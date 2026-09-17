#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using KBTV.Ads;
using KBTV.Audio;
using KBTV.Core;
using KBTV.Monitors;

namespace KBTV.World3D
{
    /// <summary>
    /// Interactive 3D helper for the control-room soundboard. Owns the mixer driver
    /// interaction against the GLB's real parts (regenerated so the chassis is static
    /// and the fader caps / knobs / lamps exist as named nodes): slides caps along
    /// glTF-local Z, spins knobs around glTF-local Y, and drives per-channel lamp
    /// emission from driver + monitor state each frame. Also exposes tap-target
    /// colliders on a dedicated collision layer so World3D can raycast clicks and
    /// drags. Parented at identity under the SoundBoard GLB node, so part transforms
    /// share the board's local frame.
    /// </summary>
    public partial class Soundboard3D : Node3D
    {
        /// <summary>Collision layer the handle tap-targets live on (player/world never collide with it).</summary>
        public const uint HitLayer = 1u << 20;

        private static readonly Color LedDim = new(0.28f, 0.28f, 0.28f);
        private static readonly Color LedSelected = new(0.9f, 0.95f, 1f);

        private readonly Dictionary<SoundboardControl, Node3D> _parts = new();
        private readonly Dictionary<SoundboardControl, Vector3> _restPositions = new();
        private readonly Dictionary<string, MeshInstance3D> _lamps = new();
        private readonly Dictionary<string, StandardMaterial3D> _lampMaterials = new();
        private readonly Dictionary<StaticBody3D, SoundboardControl> _bodies = new();
        private readonly Dictionary<SoundboardControl, StaticBody3D> _bodiesByControl = new();
        private readonly HashSet<string> _warnedMissing = new();

        /// <summary>Driven part bodies used for the subtle hover highlight (one mesh per control).</summary>
        private readonly Dictionary<SoundboardControl, MeshInstance3D> _partMeshes = new();
        private readonly Dictionary<SoundboardControl, Material?> _partOriginalOverrides = new();
        private readonly Dictionary<SoundboardControl, StandardMaterial3D> _partHighlightMaterials = new();

        private Node3D? _board;
        private SoundboardMonitor? _monitor;
        private AudioMixerManager? _mixer;
        private AdManager? _adManager;

        /// <summary>One persistent halo ring per driven control (always visible while handles are shown).</summary>
        private readonly Dictionary<SoundboardControl, MeshInstance3D> _halos = new();
        private readonly Dictionary<SoundboardControl, StandardMaterial3D> _haloMaterials = new();
        private bool _built;
        private bool _handlesVisible;
        private SoundboardGlow.SpeakingChannel _speakingChannel = SoundboardGlow.SpeakingChannel.None;

        /// <summary>Eased 0..1 glow per channel, following that channel's live bus peak (size = loudness).</summary>
        private float _callerGlow;
        private float _vernGlow;
        private float _adsGlow;

        /// <summary>Small lift so the halo clears the board face while the knob/fader body occludes its center.</summary>
        private const float HaloLift = 0.008f;

        /// <summary>Side of a knob halo ring quad at FULL loudness (the max; silent channels shrink to MinRingFraction of this).</summary>
        private const float HaloSize = 0.09f;

        /// <summary>Knob-halo quad for the master knob (its body is much larger than a channel knob).</summary>
        private const float MasterHaloSize = 0.15f;

        /// <summary>Fader-halo quad at FULL loudness: narrow across the slot (X), elongated along the slider stroke (Z).</summary>
        private static readonly Vector2 FaderHaloSize = new(0.045f, 0.13f);

        /// <summary>Centre alpha of a resting halo ring (the radial/rect gradient fades the edges to transparent).</summary>
        private const float HaloAlpha = 0.55f;

        /// <summary>Per-second smoothing rate for the per-channel glow following the live bus peaks.</summary>
        private const float GlowResponsePerSecond = 8f;

        /// <summary>Shared knob-state driver (World3D points this at the overlay's driver).</summary>
        public SoundboardMixerDriver Driver { get; private set; } = new();

        /// <summary>Currently selected control (or None).</summary>
        public SoundboardControl SelectedControl { get; private set; } = SoundboardControl.None;

        /// <summary>Currently hovered control (or None). Drives the halo + lamp brighten.</summary>
        public SoundboardControl HoveredControl { get; private set; } = SoundboardControl.None;

        public override void _Ready()
        {
            BuildHalos();
            ResolveAdManager();
            ResolveMixer();
        }

        public override void _Process(double delta)
        {
            if (!_built || !_handlesVisible)
            {
                return;
            }

            UpdateControls();
            UpdateLeds();
            UpdateSpeakingState(delta);
            UpdateHalos();
        }

        /// <summary>Creates one persistent halo ring per driven control.</summary>
        private void BuildHalos()
        {
            foreach (var slot in SoundboardPhysicalLayout.Slots)
            {
                if (_halos.ContainsKey(slot.Control))
                {
                    continue;
                }

                var isFader = slot.Kind == ControlKind.Fader;
                var material = new StandardMaterial3D
                {
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    CullMode = BaseMaterial3D.CullModeEnum.Disabled,
                    AlbedoTexture = isFader ? RectHaloTexture : MakeHaloGradient(),
                    AlbedoColor = new Color(LedSelected.R, LedSelected.G, LedSelected.B, HaloAlpha)
                };

                var ring = new MeshInstance3D
                {
                    Name = $"Halo_{slot.Control}",
                    Mesh = new QuadMesh { Size = BaseHaloSize(slot.Control, isFader) },
                    MaterialOverride = material,
                    RotationDegrees = new Vector3(-90f, 0f, 0f),
                    Visible = false
                };
                AddChild(ring);

                _halos[slot.Control] = ring;
                _haloMaterials[slot.Control] = material;
            }
        }

        private static GradientTexture2D MakeHaloGradient()
        {
            var gradient = new Gradient();
            gradient.SetColor(0, new Color(1f, 1f, 1f, 1f));
            gradient.SetColor(1, new Color(1f, 1f, 1f, 0f));
            return new GradientTexture2D
            {
                Gradient = gradient,
                Width = 256,
                Height = 256,
                Fill = GradientTexture2D.FillEnum.Radial,
                FillFrom = new Vector2(0.5f, 0.5f),
                FillTo = new Vector2(1f, 0.5f)
            };
        }

        /// <summary>
        /// Soft rounded-rectangle glow texture for fader halos (the radial knob
        /// gradient can't read as a slider, so faders get a rectangular falloff
        /// with rounded corners). White core fading to transparent at the edges.
        /// </summary>
        private static readonly ImageTexture RectHaloTexture = MakeRectHaloTexture();

        private static ImageTexture MakeRectHaloTexture()
        {
            const int res = 96;
            var image = Image.CreateEmpty(res, res, false, Image.Format.Rgba8);
            float half = res / 2f;
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float px = Mathf.Abs(x + 0.5f - half) / half;
                    float py = Mathf.Abs(y + 0.5f - half) / half;
                    float alpha = Mathf.Clamp(1f - Mathf.Max(px, py), 0f, 1f);
                    alpha = Mathf.Pow(alpha, 1.6f);
                    image.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            return ImageTexture.CreateFromImage(image);
        }

        /// <summary>Halo quad size at full loudness for a control.</summary>
        private static Vector2 BaseHaloSize(SoundboardControl control, bool isFader)
        {
            if (isFader)
            {
                return FaderHaloSize;
            }
            float side = control == SoundboardControl.Master ? MasterHaloSize : HaloSize;
            return new Vector2(side, side);
        }

        /// <summary>Points this board at a shared driver (set before the view opens).</summary>
        public void AttachDriver(SoundboardMixerDriver driver)
        {
            if (driver != null)
            {
                Driver = driver;
            }
        }

        /// <summary>Connects the monitor that supplies live CALLER bands + drain state.</summary>
        public void SetMonitor(SoundboardMonitor monitor) => _monitor = monitor;

        /// <summary>
        /// Binds the board to the SoundBoard GLB node: locates the named part nodes
        /// (and idle lamps), captures their rest transforms, and builds the HitLayer
        /// tap-target colliders anchored to each part.
        /// </summary>
        public void AttachBoard(Node3D board)
        {
            if (_board != null || board == null)
            {
                return;
            }

            _board = board;
            BuildSlots();
            BuildPartHighlights();
            BuildLamps();
            NormalizeUnusedParts();
            _built = true;
        }

        /// <summary>
        /// Returns the control a tap-target collider belongs to. Use with the
        /// colliders from <see cref="Bodies"/> after a raycast.
        /// </summary>
        public SoundboardControl ControlFromBody(StaticBody3D body)
        {
            if (body != null && _bodies.TryGetValue(body, out var control))
            {
                return control;
            }
            return SoundboardControl.None;
        }

        /// <summary>All tap-target colliders (for raycast distinguishes / tests).</summary>
        public IEnumerable<StaticBody3D> Bodies => _bodies.Keys;

        /// <summary>Shows the handles and re-pushes the persisted mix.</summary>
        public void ShowHandles()
        {
            if (!_built)
            {
                return;
            }
            ClearHoverHighlight();
            SelectedControl = SoundboardControl.None;
            HoveredControl = SoundboardControl.None;
            Driver.Apply();
            SetBodiesEnabled(true);
            _handlesVisible = true;
            UpdateControls();
            UpdateLeds();
            UpdateHalos();
        }

        /// <summary>Hides the handles and disables their tap-targets.</summary>
        public void HideHandles()
        {
            ClearHoverHighlight();
            SelectedControl = SoundboardControl.None;
            HoveredControl = SoundboardControl.None;
            _handlesVisible = false;
            SetBodiesEnabled(false);
            UpdateHalos();
        }

        /// <summary>Selects a control (highlights its channel lamp); None clears the selection.</summary>
        public void SelectControl(SoundboardControl control)
        {
            if (SelectedControl == control)
            {
                return;
            }

            SelectedControl = control;
            if (_built)
            {
                UpdateLeds();
            }
        }

        /// <summary>Sets the hovered control (None clears). Drives the part highlight + lamp.</summary>
        public void SetHover(SoundboardControl control)
        {
            if (HoveredControl == control)
            {
                return;
            }

            if (HoveredControl != SoundboardControl.None)
            {
                SetPartHighlight(HoveredControl, false);
            }
            HoveredControl = control;
            if (control != SoundboardControl.None)
            {
                SetPartHighlight(control, true);
            }

            if (_built)
            {
                UpdateLeds();
                UpdateHalos();
            }
        }

        private void ClearHoverHighlight()
        {
            if (HoveredControl != SoundboardControl.None)
            {
                SetPartHighlight(HoveredControl, false);
            }
        }

        /// <summary>Sets a control value from normalized input and applies it to the mix.</summary>
        public void SetControlValue(SoundboardControl control, float value, bool apply = true)
        {
            if (control == SoundboardControl.None)
            {
                return;
            }

            SoundboardControlApplier.Apply(Driver.State, control, value);
            if (apply)
            {
                Driver.Apply();
            }
            if (_built)
            {
                var slot = SoundboardPhysicalLayout.SlotFor(control);
                if (_parts.TryGetValue(control, out var part) && part != null)
                {
                    UpdateControlVisual(control, part, slot.Kind);
                }
            }
        }

        private void BuildSlots()
        {
            if (_board == null)
            {
                return;
            }

            foreach (var slot in SoundboardPhysicalLayout.Slots)
            {
                var part = _board.FindChild(slot.PartName, true, false) as Node3D;
                if (part == null)
                {
                    WarnMissing(slot.PartName);
                    continue;
                }

                _parts[slot.Control] = part;
                _restPositions[slot.Control] = part.Position;
                BuildCollider(slot.Control, slot.Kind, part.Position);
            }
        }

        /// <summary>
        /// Parks every cosmetic (driven-slot-adjacent + unused-column) knob and
        /// fader cap on the board face at the resting defaults so the whole mixer
        /// reads uniformly: all knobs at 12 o'clock and all unused fader caps at
        /// the bottom (0%). Driven slots are skipped — they are positioned from
        /// driver state in <see cref="UpdateControls"/>. Runs once at attach; the
        /// parked parts have no slots so nothing ever moves them afterwards.
        /// </summary>
        private void NormalizeUnusedParts()
        {
            if (_board == null)
            {
                return;
            }

            var driven = new HashSet<string>(SoundboardPhysicalLayout.Slots.Length);
            foreach (var slot in SoundboardPhysicalLayout.Slots)
            {
                driven.Add(slot.PartName);
            }

            foreach (var node in _board.FindChildren("Knob_*", recursive: true, owned: false))
            {
                if (node is Node3D knob && !driven.Contains(node.Name))
                {
                    knob.RotationDegrees = new Vector3(0f, SoundboardPhysicalLayout.KnobRestOffsetDeg, 0f);
                }
            }

            foreach (var node in _board.FindChildren("FaderCap_*", recursive: true, owned: false))
            {
                if (node is Node3D cap && !driven.Contains(node.Name))
                {
                    var pos = cap.Position;
                    pos.Z = SoundboardPhysicalLayout.FaderLocalZ(0f);
                    cap.Position = pos;
                }
            }
        }

        /// <summary>
        /// Locates each driven part's body mesh and pre-builds its subtle hover
        /// highlight material (derived from the part's own flat albedo). Hovering
        /// applies the override; leaving restores the captured original.
        /// </summary>
        private void BuildPartHighlights()
        {
            foreach (var slot in SoundboardPhysicalLayout.Slots)
            {
                if (!_parts.TryGetValue(slot.Control, out var part) || part == null)
                {
                    continue;
                }

                var mesh = FindPartBodyMesh(part);
                if (mesh == null)
                {
                    WarnMissing($"{slot.PartName} body mesh");
                    continue;
                }

                _partMeshes[slot.Control] = mesh;
                _partOriginalOverrides[slot.Control] = mesh.MaterialOverride;
                _partHighlightMaterials[slot.Control] = MakeHighlightMaterial(GetPartBaseColor(mesh));
            }
        }

        /// <summary>First visible mesh under a driven part (the part node or a mesh child).</summary>
        private static MeshInstance3D? FindPartBodyMesh(Node3D part)
        {
            if (part is MeshInstance3D direct)
            {
                return direct;
            }

            foreach (var node in part.FindChildren("*", recursive: true, owned: false))
            {
                if (node is MeshInstance3D mesh)
                {
                    return mesh;
                }
            }
            return null;
        }

        /// <summary>Base albedo of a driven part's body (knobs/caps are flat-coloured, untextured).</summary>
        private static Color GetPartBaseColor(MeshInstance3D mesh)
        {
            if (mesh.GetActiveMaterial(0) is StandardMaterial3D active && active.AlbedoTexture == null)
            {
                return active.AlbedoColor;
            }
            return Colors.White;
        }

        /// <summary>
        /// Subtle hover highlight: the part's albedo pulled toward white (a little
        /// lit-up lift) plus a faint matching emission. Enough to say "this one"
        /// without changing the glow pool underneath.
        /// </summary>
        private static StandardMaterial3D MakeHighlightMaterial(Color baseColor)
        {
            var lit = baseColor.Lerp(Colors.White, 0.22f);
            return new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel,
                AlbedoColor = lit,
                EmissionEnabled = true,
                Emission = lit * 0.45f,
                EmissionEnergyMultiplier = 0.6f
            };
        }

        private void SetPartHighlight(SoundboardControl control, bool on)
        {
            if (!_partMeshes.TryGetValue(control, out var mesh) || mesh == null)
            {
                return;
            }

            if (on)
            {
                if (_partHighlightMaterials.TryGetValue(control, out var highlight) && highlight != null)
                {
                    mesh.MaterialOverride = highlight;
                }
            }
            else if (_partOriginalOverrides.TryGetValue(control, out var original))
            {
                mesh.MaterialOverride = original;
            }
        }

        private void BuildCollider(SoundboardControl control, ControlKind kind, Vector3 partPos)
        {
            var isFader = kind == ControlKind.Fader;
            var body = new StaticBody3D
            {
                Name = $"Hit_{control}",
                Position = partPos,
                CollisionLayer = HitLayer,
                CollisionMask = 0u
            };
            var shape = new CollisionShape3D
            {
                // Slim tap targets so hover only activates over the actual handle.
                // The halo is centred on the part, so the collider centre == the
                // knob/fader centre under the cursor; the capsule bodies occlude the
                // halo centre, leaving a soft glow ring under each handle.
                // Knob depth (Z) must stay under the 0.065 row pitch so adjacent
                // hitboxes never overlap under the oblique camera.
                Shape = new BoxShape3D
                {
                    Size = new Vector3(isFader ? 0.055f : 0.075f, 0.05f, isFader ? 0.04f : 0.05f)
                }
            };
            if (!isFader && control == SoundboardControl.Master)
            {
                shape.Shape = new BoxShape3D { Size = new Vector3(0.17f, 0.07f, 0.15f) };
            }
            body.AddChild(shape);
            AddChild(body);
            _bodies[body] = control;
            _bodiesByControl[control] = body;
        }

        private void BuildLamps()
        {
            if (_board == null)
            {
                return;
            }

            foreach (var lampName in AllLampNames())
            {
                var lamp = _board.FindChild(lampName, true, false) as MeshInstance3D;
                if (lamp == null)
                {
                    WarnMissing(lampName);
                    continue;
                }

                var material = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.05f, 0.05f, 0.06f, 1f),
                    EmissionEnabled = true,
                    Emission = LedDim,
                    EmissionEnergyMultiplier = 3f
                };
                lamp.MaterialOverride = material;
                _lamps[lampName] = lamp;
                _lampMaterials[lampName] = material;
            }
        }

        private static IEnumerable<string> AllLampNames()
        {
            var seen = new HashSet<string>(SoundboardPhysicalLayout.IdleLamps);
            foreach (var lampName in SoundboardPhysicalLayout.IdleLamps)
            {
                yield return lampName;
            }
            foreach (var slot in SoundboardPhysicalLayout.Slots)
            {
                if (seen.Add(slot.LampName))
                {
                    yield return slot.LampName;
                }
            }
        }

        private void WarnMissing(string what)
        {
            if (_warnedMissing.Add(what))
            {
                GD.PushWarning($"Soundboard3D: missing board part '{what}' (stroke GLB out of date?)");
            }
        }

        private void UpdateControls()
        {
            foreach (var slot in SoundboardPhysicalLayout.Slots)
            {
                if (_parts.TryGetValue(slot.Control, out var part) && part != null)
                {
                    UpdateControlVisual(slot.Control, part, slot.Kind);
                }
            }
        }

        private void UpdateControlVisual(SoundboardControl control, Node3D part, ControlKind kind)
        {
            var value = SoundboardControlApplier.CurrentValue(Driver.State, control);
            if (kind == ControlKind.Fader)
            {
                var rest = _restPositions[control];
                part.Position = new Vector3(rest.X, rest.Y, SoundboardPhysicalLayout.FaderLocalZ(value));
                if (_bodiesByControl.TryGetValue(control, out var body) && body != null)
                {
                    body.Position = part.Position;
                }
            }
            else
            {
                part.RotationDegrees = new Vector3(0f, SoundboardPhysicalLayout.KnobRotationDeg(value), 0f);
            }
        }

        private void UpdateLeds()
        {
            var bands = CurrentCallerBands();
            var worst = SoundboardTargetGenerator.GetWorstBand(bands);
            SetLampColor("Lamp_6", BandColor(worst));
            SetLampColor("Lamp_7", SoundboardTargetGenerator.RampGreen);
            var adBreak = _adManager != null && _adManager.IsAdBreakActive;
            SetLampColor("Lamp_5", adBreak ? SoundboardTargetGenerator.RampGreen : LedDim);
            SetLampColor("Lamp_3", SoundboardTargetGenerator.RampGreen);
            foreach (var idle in SoundboardPhysicalLayout.IdleLamps)
            {
                SetLampColor(idle, LedDim);
            }
        }

        private SoundboardCallerBands CurrentCallerBands()
        {
            if (_monitor != null)
            {
                return _monitor.CallerBands;
            }

            return SoundboardTargetGenerator.GetCallerBands(Driver.State, 0.5f);
        }

        private void SetLampColor(string lampName, Color color)
        {
            if (!_lampMaterials.TryGetValue(lampName, out var material) || material == null)
            {
                return;
            }

            var isHighlighted = IsLampHighlighted(lampName);
            material.Emission = isHighlighted ? LedSelected : color;
            material.AlbedoColor = isHighlighted ? LedSelected : new Color(0.05f, 0.05f, 0.06f, 1f);
        }

        private bool IsLampHighlighted(string lampName)
        {
            if (lampName == string.Empty)
            {
                return false;
            }

            if (SelectedControl != SoundboardControl.None &&
                SoundboardPhysicalLayout.SlotFor(SelectedControl).LampName == lampName)
            {
                return true;
            }

            return HoveredControl != SoundboardControl.None &&
                   SoundboardPhysicalLayout.SlotFor(HoveredControl).LampName == lampName;
        }

        /// <summary>
        /// Updates every control's halo ring each frame while the handles are
        /// visible. Each ring is anchored at its control part's position (+ a tiny
        /// lift) so it stays centred under the handle from any camera angle, and
        /// depth-tested so the knob/fader body hides its centre — a soft ring of
        /// light pooling under each part. Colour shows the control's mix status:
        /// the continuous blue→cyan→green→yellow→red error ramp while its own
        /// channel is the one talking, a constant white idle ring otherwise. SIZE
        /// encodes live loudness (each channel's glow grows from the MinRingFraction
        /// baseline up to its full base size as the channel's bus peak rises, and
        /// fader rings are stretched into a rectangle along the slider stroke).
        /// Hover never touches the glow — the hovered part highlights itself.
        /// </summary>
        private void UpdateHalos()
        {
            if (!_handlesVisible || _halos.Count == 0)
            {
                foreach (var ring in _halos.Values)
                {
                    ring.Visible = false;
                }
                return;
            }

            foreach (var slot in SoundboardPhysicalLayout.Slots)
            {
                if (!_halos.TryGetValue(slot.Control, out var ring) || ring == null ||
                    !_haloMaterials.TryGetValue(slot.Control, out var material) || material == null ||
                    !_parts.TryGetValue(slot.Control, out var part) || part == null)
                {
                    continue;
                }

                var channel = SoundboardGlow.ChannelOf(slot.Control);
                var colored = channel != SoundboardGlow.SpeakingChannel.None &&
                              channel == _speakingChannel;

                material.AlbedoColor = colored
                    ? new Color(SoundboardTargetGenerator.ColorForError(ControlError(slot.Control)), HaloAlpha)
                    : new Color(LedSelected.R, LedSelected.G, LedSelected.B, HaloAlpha);

                var isFader = slot.Kind == ControlKind.Fader;
                var baseSize = BaseHaloSize(slot.Control, isFader);
                var glowScale = SoundboardGlow.SizeScaleFromGlow(ControlGlow(slot.Control));
                ((QuadMesh)ring.Mesh!).Size = baseSize * glowScale;

                ring.Position = part.Position + new Vector3(0f, HaloLift, 0f);
                ring.Visible = true;
            }
        }

        private static bool IsCallerControl(SoundboardControl control) =>
            SoundboardGlow.ChannelOf(control) == SoundboardGlow.SpeakingChannel.Caller;

        private float ControlError(SoundboardControl control)
        {
            if (control == SoundboardControl.None)
            {
                return 0f;
            }

            var isCaller = IsCallerControl(control);
            var speakingVolume = _monitor != null && isCaller
                ? (_monitor.CallerSpeakingVolume ?? 0.5f)
                : 0.5f;
            var seed = _monitor != null && isCaller
                ? (_monitor.CallerSoundboardSeed ?? 0)
                : 0;
            return SoundboardTargetGenerator.GetControlError(Driver.State, control, speakingVolume, seed);
        }

        /// <summary>
        /// Eased 0..1 glow for a control, following its own channel's live bus peak
        /// (caller rings breathe with the caller's voice, Vern's with Vern, Ads with
        /// the SFX bus during breaks). Master has no channel of its own → 0 (always
        /// the quiet baseline size).
        /// </summary>
        private float ControlGlow(SoundboardControl control) => control switch
        {
            SoundboardControl.CallerGain or SoundboardControl.CallerLowPass or
            SoundboardControl.CallerHighPass or SoundboardControl.CallerLevel => _callerGlow,
            SoundboardControl.VernGain or SoundboardControl.VernLevel => _vernGlow,
            SoundboardControl.AdsGain or SoundboardControl.AdsLevel => _adsGlow,
            _ => 0f
        };

        private void SetBodiesEnabled(bool enabled)
        {
            foreach (var body in _bodies.Keys)
            {
                if (body == null)
                {
                    continue;
                }
                body.CollisionLayer = enabled ? HitLayer : 0u;
                body.CollisionMask = 0u;
            }
        }

        private void ResolveAdManager()
        {
            try
            {
                _adManager = DependencyInjection.Get<AdManager>(this);
            }
            catch (InvalidOperationException)
            {
                _adManager = null;
            }
        }

        private void ResolveMixer()
        {
            _mixer = GetNodeOrNull<AudioMixerManager>("/root/AudioMixerManager");
        }

        /// <summary>
        /// Samples the live Vern/Caller/Ads bus peaks each frame, picks which channel
        /// is speaking, and eases each channel's glow toward its own peak so the halo
        /// rings breathe with the audio (louder channel = bigger rings). Runs only
        /// while the board is on screen (guarded by the _built check in _Process).
        /// </summary>
        private void UpdateSpeakingState(double delta)
        {
            float callerPeak = _mixer?.GetCallerBusPeakDb() ?? -80f;
            float vernPeak = _mixer?.GetVernBusPeakDb() ?? -80f;
            float adsPeak = _mixer?.GetAdsBusPeakDb() ?? -80f;
            _speakingChannel = SoundboardGlow.ChooseSpeakingChannel(callerPeak, vernPeak);

            float ease = 1f - Mathf.Exp(-GlowResponsePerSecond * (float)delta);
            _callerGlow = Mathf.Lerp(_callerGlow, SoundboardGlow.GlowFromPeakDb(callerPeak), ease);
            _vernGlow = Mathf.Lerp(_vernGlow, SoundboardGlow.GlowFromPeakDb(vernPeak), ease);
            _adsGlow = Mathf.Lerp(_adsGlow, SoundboardGlow.GlowFromPeakDb(adsPeak), ease);
        }

        private static Color BandColor(SoundboardBand band) => band switch
        {
            SoundboardBand.Green => SoundboardTargetGenerator.RampGreen,
            SoundboardBand.Cyan => SoundboardTargetGenerator.RampCyan,
            SoundboardBand.Blue => SoundboardTargetGenerator.RampBlue,
            SoundboardBand.Yellow => SoundboardTargetGenerator.RampYellow,
            SoundboardBand.Red => SoundboardTargetGenerator.RampRed,
            _ => LedDim
        };
    }
}
