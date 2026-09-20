#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using KBTV.Ads;
using KBTV.Audio;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Dialogue;
using KBTV.Monitors;

namespace KBTV.World3D
{
    /// <summary>
    /// Interactive 3D helper for the control-room soundboard. Owns the mixer driver
    /// interaction against the GLB's real parts (regenerated so the chassis is static
    /// and the fader caps / knobs / lamps / broadcast buttons exist as named nodes):
    /// slides caps along glTF-local Z, spins knobs around glTF-local Y, drives
    /// per-channel lamp emission from driver + monitor state each frame, and presses
    /// the four blocky broadcast buttons (Music/Delay/Ads/Drop): Ads queues the next
    /// break, Music starts the looping channel-3 break bed, Drop hangs up the on-air
    /// caller, and every press publishes a SoundboardButtonPressedEvent (curse-QTE).
    /// Lamp faces show game-state looks through the pure SoundboardButtonState
    /// resolver (incl. the breathing white-yellow Flashing state + glow quads);
    /// cap bodies get the same subtle hover brighten the knobs/faders use. Also
    /// exposes tap-target colliders on a dedicated collision layer so World3D can
    /// raycast clicks and drags. Parented at identity under the SoundBoard GLB
    /// node, so part transforms share the board's local frame.
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
        private ICallerRepository? _callerRepository;
        private AsyncBroadcastLoop? _broadcastLoop;
        private EventBus? _eventBus;
        private bool _servicesResolved;
        private bool _eventsSubscribed;

        /// <summary>Driven button parts: cap node + rest transform.</summary>
        private readonly Dictionary<SoundboardButton, Node3D> _buttonParts = new();
        private readonly Dictionary<SoundboardButton, Vector3> _buttonRestPositions = new();
        private readonly Dictionary<string, StandardMaterial3D> _buttonLampMaterials = new();
        private readonly Dictionary<StaticBody3D, SoundboardButton> _buttonBodies = new();
        private readonly Dictionary<SoundboardButton, (Color Color, float Energy)?> _buttonLights = new();

        /// <summary>Button cap hover highlight (same subtle brighten as knobs/faders).</summary>
        private readonly Dictionary<SoundboardButton, MeshInstance3D> _buttonPartMeshes = new();
        private readonly Dictionary<SoundboardButton, Material?> _buttonOriginalOverrides = new();
        private readonly Dictionary<SoundboardButton, StandardMaterial3D> _buttonHighlightMaterials = new();

        /// <summary>White-yellow glow quad per button, visible only while its lamp is flashing.</summary>
        private readonly Dictionary<SoundboardButton, MeshInstance3D> _buttonGlows = new();
        private readonly Dictionary<SoundboardButton, StandardMaterial3D> _buttonGlowMaterials = new();
        private readonly Dictionary<SoundboardButton, Vector3> _buttonGlowOffsets = new();
        private const float ButtonGlowSize = 0.12f;
        private const float ButtonGlowLift = 0.012f;

        private SoundboardButton _hoveredButton = SoundboardButton.None;
        private SoundboardButton _pressedButton = SoundboardButton.None;
        private float _pressTimer;

        /// <summary>Brief bright flash on a button's lamp face right after it is pressed.</summary>
        private SoundboardButton _flashButton = SoundboardButton.None;
        private float _flashTimer;
        private const float ButtonFlashSeconds = 0.18f;

        private IBroadcastAudioService? _audioService;

        /// <summary>Live info screen rendered onto the GLB's ScreenFace panel.</summary>
        private MeshInstance3D? _screenFace;
        private SubViewport? _screenViewport;
        private Label? _screenLabel;
        private StandardMaterial3D? _screenMaterial;
        private float _screenRefreshTimer;

        /// <summary>Mirrors the live-curse window so the Delay/Drop lamps can pulse while it is open.</summary>
        private bool _curseActive;
        private float _curseTimer;
        private const float CurseWindowSeconds = 20f;

        /// <summary>How long a pressed button cap stays down before it pops back.</summary>
        private const float ButtonPressHoldSeconds = 0.14f;

        /// <summary>Idle emission of a broadcast button's lamp face (a faint warm glow under the label).</summary>
        private static readonly Color ButtonIdleEmission = new(0.3f, 0.09f, 0.09f);
        private static readonly Color ButtonLampAlbedo = new(0.456f, 0.397f, 0.258f);

        /// <summary>White-yellow core of the demanding-press flash (grows/shrinks with the pulse).</summary>
        private static readonly Color ButtonFlashWarm = new(1f, 0.93f, 0.55f);
        private static readonly Color ButtonFlashHot = new(1f, 0.99f, 0.88f);

        /// <summary>One persistent halo ring per driven control (always visible while handles are shown).</summary>
        private readonly Dictionary<SoundboardControl, MeshInstance3D> _halos = new();
        private readonly Dictionary<SoundboardControl, StandardMaterial3D> _haloMaterials = new();
        private readonly Dictionary<SoundboardControl, MeshInstance3D> _faderGlows = new();
        private readonly Dictionary<SoundboardControl, StandardMaterial3D> _faderGlowMaterials = new();
        private bool _built;
        private bool _handlesVisible;
        private SoundboardGlow.SpeakingChannel _speakingChannel = SoundboardGlow.SpeakingChannel.None;
        private float _speakingChannelHold;

        /// <summary>Eased 0..1 glow per channel, following that channel's live bus peak (size = loudness).</summary>
        private float _callerGlow;
        private float _vernGlow;
        private float _adsGlow;

        /// <summary>Small lift so the halo clears the board face while the knob/fader body occludes its center.</summary>
        private const float HaloLift = 0.008f;

        /// <summary>Side of a knob halo ring quad at FULL loudness (the max; silent channels shrink to MinRingFraction of this).</summary>
        private const float HaloSize = 0.07f;

        private const float FaderGlowSize = 0.035f;
        private const float FaderGlowLift = 0.004f;

        /// <summary>Centre alpha of a resting halo ring (the radial/rect gradient fades the edges to transparent).</summary>
        private const float HaloAlpha = 0.55f;
        private const float FaderGlowAlpha = 0.35f;

        /// <summary>Per-second smoothing rate for the per-channel glow following the live bus peaks.</summary>
        private const float GlowResponsePerSecond = 8f;
        private const float SpeakingChannelHoldSeconds = 0.35f;

        /// <summary>Shared knob-state driver (World3D points this at the overlay's driver).</summary>
        public SoundboardMixerDriver Driver { get; private set; } = new();

        /// <summary>Currently selected control (or None).</summary>
        public SoundboardControl SelectedControl { get; private set; } = SoundboardControl.None;

        /// <summary>Currently hovered control (or None). Drives the part highlight + lamp brighten.</summary>
        public SoundboardControl HoveredControl { get; private set; } = SoundboardControl.None;

        public override void _Ready()
        {
            BuildHalos();
            ResolveServices();
            ResolveMixer();
        }

        public override void _Process(double delta)
        {
            ResolveServices();

            if (!_built || !_handlesVisible)
            {
                return;
            }

            UpdateControls();
            UpdateLeds();
            UpdateSpeakingState(delta);
            UpdateHalos();
            UpdateButtons(delta);
            UpdateScreen(delta);
        }

        /// <summary>Creates one persistent halo ring per driven control.</summary>
        private void BuildHalos()
        {
            foreach (var slot in SoundboardPhysicalLayout.Slots)
            {
                if (_halos.ContainsKey(slot.Control) || slot.Kind == ControlKind.Fader)
                {
                    continue;
                }

                var material = new StandardMaterial3D
                {
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    CullMode = BaseMaterial3D.CullModeEnum.Disabled,
                    AlbedoTexture = MakeHaloGradient(),
                    AlbedoColor = new Color(LedSelected.R, LedSelected.G, LedSelected.B, HaloAlpha)
                };

                var ring = new MeshInstance3D
                {
                    Name = $"Halo_{slot.Control}",
                    Mesh = new QuadMesh { Size = BaseHaloSize(slot.Control) },
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

        /// <summary>Halo quad size at full loudness for a control.</summary>
        private static Vector2 BaseHaloSize(SoundboardControl control) =>
            new Vector2(HaloSize, HaloSize);

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
            BuildButtons();
            BuildScreen();
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
            ClearButtonState();
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
            ClearButtonState();
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

        /// <summary>
        /// Locates the four broadcast button caps + lamp faces, captures their rest
        /// transforms, gives each lamp face its own driven material, and builds the
        /// HitLayer tap colliders.
        /// </summary>
        private void BuildButtons()
        {
            if (_board == null)
            {
                return;
            }

            foreach (var slot in SoundboardPhysicalLayout.ButtonSlots)
            {
                var cap = _board.FindChild(slot.PartName, true, false) as Node3D;
                if (cap == null)
                {
                    WarnMissing(slot.PartName);
                    continue;
                }

                _buttonParts[slot.Button] = cap;
                _buttonRestPositions[slot.Button] = cap.Position;

                var capMesh = FindButtonBodyMesh(cap, slot.LampName);
                if (capMesh != null)
                {
                    _buttonPartMeshes[slot.Button] = capMesh;
                    _buttonOriginalOverrides[slot.Button] = capMesh.MaterialOverride;
                    _buttonHighlightMaterials[slot.Button] = MakeHighlightMaterial(GetPartBaseColor(capMesh));
                }

                var lamp = _board.FindChild(slot.LampName, true, false) as MeshInstance3D;
                if (lamp == null)
                {
                    WarnMissing(slot.LampName);
                }
                else
                {
                    var material = new StandardMaterial3D
                    {
                        AlbedoColor = ButtonLampAlbedo,
                        EmissionEnabled = true,
                        Emission = ButtonIdleEmission,
                        EmissionEnergyMultiplier = 1f
                    };
                    lamp.MaterialOverride = material;
                    _buttonLampMaterials[slot.LampName] = material;
                }

                // The flash glow pool rides just above the lamp face (which is
                // parented to the cap, so the cap's rest frame locates it in the
                // board frame — both sit under the identity-parented helper).
                var lampOffset = lamp != null
                    ? lamp.Position + new Vector3(0f, ButtonGlowLift, 0f)
                    : new Vector3(0f, ButtonGlowLift, 0f);
                _buttonGlowOffsets[slot.Button] = lampOffset;
                var glowMaterial = new StandardMaterial3D
                {
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    CullMode = BaseMaterial3D.CullModeEnum.Disabled,
                    AlbedoTexture = MakeHaloGradient(),
                    AlbedoColor = new Color(ButtonFlashWarm.R, ButtonFlashWarm.G, ButtonFlashWarm.B, HaloAlpha)
                };
                var glow = new MeshInstance3D
                {
                    Name = $"BtnGlow_{slot.Button}",
                    Mesh = new QuadMesh { Size = new Vector2(ButtonGlowSize, ButtonGlowSize) },
                    MaterialOverride = glowMaterial,
                    RotationDegrees = new Vector3(-90f, 0f, 0f),
                    Position = cap.Position + lampOffset,
                    Visible = false
                };
                AddChild(glow);
                _buttonGlows[slot.Button] = glow;
                _buttonGlowMaterials[slot.Button] = glowMaterial;

                var body = new StaticBody3D
                {
                    Name = $"Hit_Button_{slot.Button}",
                    Position = cap.Position,
                    CollisionLayer = HitLayer,
                    CollisionMask = 0u
                };
                body.AddChild(new CollisionShape3D
                {
                    Shape = new BoxShape3D { Size = new Vector3(0.16f, 0.04f, 0.14f) }
                });
                AddChild(body);
                _buttonBodies[body] = slot.Button;
            }
        }

        /// <summary>Returns the button a tap-target collider belongs to (or None).</summary>
        public SoundboardButton ButtonFromBody(StaticBody3D body)
        {
            if (body != null && _buttonBodies.TryGetValue(body, out var button))
            {
                return button;
            }
            return SoundboardButton.None;
        }

        /// <summary>All button tap-target colliders (for raycast distinguishes / tests).</summary>
        public IEnumerable<StaticBody3D> ButtonBodies => _buttonBodies.Keys;

        /// <summary>
        /// Sets the hovered broadcast button (None clears). Applies the same subtle
        /// part brighten the knobs/faders use to the cap body — the lamp face keeps
        /// showing its game-state look unchanged.
        /// </summary>
        public void SetButtonHover(SoundboardButton button)
        {
            if (!_built || _hoveredButton == button)
            {
                return;
            }

            if (_hoveredButton != SoundboardButton.None)
            {
                SetButtonHighlight(_hoveredButton, false);
            }
            _hoveredButton = button;
            if (button != SoundboardButton.None)
            {
                SetButtonHighlight(button, true);
            }
        }

        private void SetButtonHighlight(SoundboardButton button, bool on)
        {
            if (!_buttonPartMeshes.TryGetValue(button, out var mesh) || mesh == null)
            {
                return;
            }

            if (on)
            {
                if (_buttonHighlightMaterials.TryGetValue(button, out var highlight) && highlight != null)
                {
                    mesh.MaterialOverride = highlight;
                }
            }
            else if (_buttonOriginalOverrides.TryGetValue(button, out var original))
            {
                mesh.MaterialOverride = original;
            }
        }

        /// <summary>
        /// Presses a broadcast button: sinks the cap briefly, then runs its action.
        /// Ads queues the next break, Music starts the looping break bed on the
        /// channel-3 strip and Drop hangs up the on-air caller; every press also
        /// publishes a <see cref="SoundboardButtonPressedEvent"/> so the curse-QTE
        /// owner (LiveShowFooter) can react to Delay and to a board Drop.
        /// </summary>
        public void TapButton(SoundboardButton button)
        {
            if (!_built || button == SoundboardButton.None)
            {
                return;
            }

            ResolveServices();
            _pressedButton = button;
            _pressTimer = ButtonPressHoldSeconds;
            _flashButton = button;
            _flashTimer = ButtonFlashSeconds;
            if (_buttonParts.TryGetValue(button, out var cap) && cap != null &&
                _buttonRestPositions.TryGetValue(button, out var rest))
            {
                cap.Position = new Vector3(rest.X, rest.Y - SoundboardPhysicalLayout.ButtonPressDepth, rest.Z);
            }

            InvokeButtonAction(button);
        }

        /// <summary>
        /// Overrides a button's lamp-face emission (the hook for queued/flash states).
        /// Call <see cref="ClearButtonLight"/> to return it to the idle glow.
        /// </summary>
        public void SetButtonLight(SoundboardButton button, Color color, float energy = 1f)
        {
            _buttonLights[button] = (color, energy);
        }

        /// <summary>Returns a button's lamp face to its game-state-driven look.</summary>
        public void ClearButtonLight(SoundboardButton button)
        {
            _buttonLights.Remove(button);
        }

        private void ClearButtonState()
        {
            if (_hoveredButton != SoundboardButton.None && _built)
            {
                SetButtonHighlight(_hoveredButton, false);
            }
            _hoveredButton = SoundboardButton.None;
            _flashButton = SoundboardButton.None;
            _flashTimer = 0f;
            if (_pressedButton != SoundboardButton.None && _built)
            {
                RestoreButtonCap(_pressedButton);
            }
            _pressedButton = SoundboardButton.None;
            _pressTimer = 0f;
            foreach (var glow in _buttonGlows.Values)
            {
                if (glow != null)
                {
                    glow.Visible = false;
                }
            }
        }

        private void RestoreButtonCap(SoundboardButton button)
        {
            if (_buttonParts.TryGetValue(button, out var cap) && cap != null &&
                _buttonRestPositions.TryGetValue(button, out var rest))
            {
                cap.Position = rest;
            }
        }

        /// <summary>Animates the pressed cap back up and refreshes every button's lamp face.</summary>
        private void UpdateButtons(double delta)
        {
            var dt = Mathf.Clamp((float)delta, 0f, 1f);

            if (_pressedButton != SoundboardButton.None)
            {
                _pressTimer -= dt;
                if (_pressTimer <= 0f)
                {
                    RestoreButtonCap(_pressedButton);
                    _pressedButton = SoundboardButton.None;
                }
            }

            if (_flashButton != SoundboardButton.None)
            {
                _flashTimer -= dt;
                if (_flashTimer <= 0f)
                {
                    _flashButton = SoundboardButton.None;
                }
            }

            if (_curseActive)
            {
                _curseTimer -= dt;
                if (_curseTimer <= 0f)
                {
                    _curseActive = false;
                }
            }

            var callerOnAir = _callerRepository?.OnAirCaller != null;
            var adActive = _adManager != null && _adManager.IsAdBreakActive;
            var adQueued = _adManager != null && _adManager.IsQueued;
            var adEnabled = _adManager != null && _adManager.IsQueueButtonEnabled();
            var adWindow = _adManager != null && _adManager.IsInBreakWindow;
            var breakDue = _adManager != null && _adManager.IsActive && !_adManager.IsAdBreakActive &&
                           !_adManager.IsQueued && _adManager.BreaksRemaining > 0 &&
                           _adManager.TimeUntilNextBreak <= 0f;
            var bedPlaying = _audioService is BroadcastAudioService service && service.IsBreakMusicPlaying;
            var pulse = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(Time.GetTicksMsec() / 1000f * 12.566f), 2f);
            var facts = new ButtonStateFacts(
                AdBreakActive: adActive,
                AdQueued: adQueued,
                QueueEnabled: adEnabled,
                InBreakWindow: adWindow,
                BreakDue: breakDue,
                MusicBedPlaying: bedPlaying,
                CallerOnAir: callerOnAir,
                CurseActive: _curseActive);

            foreach (var slot in SoundboardPhysicalLayout.ButtonSlots)
            {
                if (!_buttonLampMaterials.TryGetValue(slot.LampName, out var material) || material == null)
                {
                    continue;
                }

                if (_buttonLights.TryGetValue(slot.Button, out var forced) && forced.HasValue)
                {
                    material.Emission = forced.Value.Color;
                    material.EmissionEnergyMultiplier = forced.Value.Energy;
                    continue;
                }

                var look = SoundboardButtonState.Resolve(slot.Button, in facts, _flashButton == slot.Button);
                ApplyLampLook(material, look, pulse);
                UpdateButtonGlow(slot.Button, look == ButtonLampLook.Flashing, pulse);
            }
        }

        /// <summary>
        /// Breathing white-yellow glow pool over a button whose lamp is flashing
        /// (demands a press). Same light language as the knob/fader halos: a radial
        /// gradient quad that grows/shrinks and brightens with the pulse.
        /// </summary>
        private void UpdateButtonGlow(SoundboardButton button, bool flashing, float pulse)
        {
            if (!_buttonGlows.TryGetValue(button, out var glow) || glow == null ||
                !_buttonGlowMaterials.TryGetValue(button, out var material) || material == null)
            {
                return;
            }

            if (!flashing || !_handlesVisible)
            {
                glow.Visible = false;
                return;
            }

            if (_buttonParts.TryGetValue(button, out var cap) && cap != null &&
                _buttonGlowOffsets.TryGetValue(button, out var offset))
            {
                glow.Position = cap.Position + offset;
            }

            var scale = Mathf.Lerp(0.75f, 1.3f, pulse);
            ((QuadMesh)glow.Mesh!).Size = new Vector2(ButtonGlowSize, ButtonGlowSize) * scale;
            var intensity = Mathf.Lerp(0.45f, 1f, pulse);
            material.AlbedoColor = new Color(
                ButtonFlashWarm.R,
                ButtonFlashWarm.G * intensity,
                ButtonFlashWarm.B * intensity,
                HaloAlpha * Mathf.Lerp(0.6f, 1f, pulse));
            glow.Visible = true;
        }

        /// <summary>Maps a resolved <see cref="ButtonLampLook"/> to the lamp-face emission.</summary>
        private static void ApplyLampLook(StandardMaterial3D material, ButtonLampLook look, float pulse)
        {
            switch (look)
            {
                case ButtonLampLook.PressFlash:
                    material.Emission = LedSelected;
                    material.EmissionEnergyMultiplier = 4f;
                    break;
                case ButtonLampLook.Ready:
                    material.Emission = SoundboardTargetGenerator.RampGreen;
                    material.EmissionEnergyMultiplier = 2.2f;
                    break;
                case ButtonLampLook.Queued:
                    material.Emission = SoundboardTargetGenerator.RampYellow;
                    material.EmissionEnergyMultiplier = 2.6f;
                    break;
                case ButtonLampLook.Active:
                    material.Emission = SoundboardTargetGenerator.RampRed;
                    material.EmissionEnergyMultiplier = Mathf.Lerp(1.5f, 3.2f, pulse);
                    break;
                case ButtonLampLook.Flashing:
                    material.Emission = ButtonFlashHot.Lerp(ButtonFlashWarm, pulse);
                    material.EmissionEnergyMultiplier = Mathf.Lerp(1.3f, 3.4f, pulse);
                    break;
                case ButtonLampLook.Urgent:
                    material.Emission = SoundboardTargetGenerator.RampRed;
                    material.EmissionEnergyMultiplier = Mathf.Lerp(1.5f, 3.6f, pulse);
                    break;
                case ButtonLampLook.Off:
                    material.Emission = new Color(0.10f, 0.045f, 0.045f);
                    material.EmissionEnergyMultiplier = 0.5f;
                    break;
                default:
                    material.Emission = ButtonIdleEmission;
                    material.EmissionEnergyMultiplier = 1f;
                    break;
            }
        }

        private void InvokeButtonAction(SoundboardButton button)
        {
            switch (button)
            {
                case SoundboardButton.Ads:
                    _adManager?.QueueBreak();
                    break;
                case SoundboardButton.Drop:
                    DropOnAirCaller();
                    break;
                case SoundboardButton.Music:
                    QueueBreakMusic();
                    break;
                case SoundboardButton.Delay:
                    // LiveShowFooter owns the curse window; it resolves on the event
                    // below (drops the caller and clears the QTE penalty).
                    break;
            }

            // Every press is published so curse-QTE consumers (LiveShowFooter) can
            // react to Delay AND to a board Drop hanging up a cursing caller.
            _eventBus?.Publish(new SoundboardButtonPressedEvent(button));
        }

        /// <summary>
        /// Starts (idempotently) the looping ad-break music bed on the channel-3
        /// strip. Only meaningful inside an open break window — the Music button
        /// flashes then; the bed stays silent until the player fades the strip up.
        /// </summary>
        private void QueueBreakMusic()
        {
            if (_adManager == null || !_adManager.IsInBreakWindow)
            {
                return;
            }

            if (_audioService is BroadcastAudioService audio)
            {
                audio.PlayBreakTransitionMusic();
            }
        }

        /// <summary>
        /// Builds the live info screen: a hidden SubViewport rendered onto the GLB's
        /// ScreenFace panel so the board shows caller / mix / ad state diegetically
        /// (replacing the old 2D SoundboardOverlay readout).
        /// </summary>
        private void BuildScreen()
        {
            if (_board == null)
            {
                return;
            }

            _screenFace = _board.FindChild("ScreenFace", true, false) as MeshInstance3D;
            if (_screenFace == null)
            {
                WarnMissing("ScreenFace");
                return;
            }

            _screenViewport = new SubViewport
            {
                Name = "ScreenContent",
                Size = new Vector2I(256, 128),
                TransparentBg = false,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always
            };
            AddChild(_screenViewport);

            _screenLabel = new Label
            {
                Position = new Vector2(8f, 6f),
                Size = new Vector2(240f, 116f),
                Text = "STANDBY",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            _screenLabel.AddThemeFontSizeOverride("font_size", 17);
            _screenLabel.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.62f));
            _screenViewport.AddChild(_screenLabel);

            // NOTE: the SoundBoard instance carries a yaw-180 (Round 14b), so the
            // panel UV may read mirrored/upside-down in-game. If so, flip the label
            // inside the viewport (no model change). Verify in editor.
            _screenMaterial = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                AlbedoTexture = _screenViewport.GetTexture()
            };
            _screenFace.MaterialOverride = _screenMaterial;
        }

        private void UpdateScreen(double delta)
        {
            if (_screenLabel == null)
            {
                return;
            }

            _screenRefreshTimer -= Mathf.Clamp((float)delta, 0f, 1f);
            if (_screenRefreshTimer > 0f)
            {
                return;
            }

            _screenRefreshTimer = 0.2f;
            _screenLabel.Text = ComposeScreenText();
        }

        private string ComposeScreenText()
        {
            var caller = _callerRepository?.OnAirCaller;
            var sb = new System.Text.StringBuilder();

            sb.Append(caller != null ? $"CALLER {caller.Name}" : "CALLER --");
            sb.Append('\n');

            if (_monitor == null || !_monitor.IsCallerOnAir)
            {
                sb.Append("STATUS OFF AIR");
            }
            else if (_monitor.GraceRemaining > 0f)
            {
                sb.Append($"GRACE {_monitor.GraceRemaining:0}s");
            }
            else if (_monitor.IsDraining)
            {
                sb.Append($"DRAIN -{_monitor.CurrentDrainRate:0.0}/s");
            }
            else
            {
                sb.Append("MIXED PERFECT");
            }
            sb.Append('\n');

            if (_adManager != null && _adManager.IsAdBreakActive)
            {
                sb.Append("AD BREAK LIVE");
            }
            else if (_adManager != null && _adManager.IsQueued)
            {
                sb.Append("AD QUEUED");
            }
            else if (_adManager != null && _adManager.IsInBreakWindow)
            {
                var bedPlaying = _audioService is BroadcastAudioService service && service.IsBreakMusicPlaying;
                sb.Append(bedPlaying ? "WINDOW / BED UP" : "AD WINDOW OPEN");
            }
            else
            {
                sb.Append($"ADS LEFT {_adManager?.BreaksRemaining ?? 0}");
            }

            if (_curseActive)
            {
                sb.Append('\n');
                sb.Append("!! CURSED: DELAY/DROP");
            }

            return sb.ToString();
        }

        private void DropOnAirCaller()
        {
            var caller = _callerRepository?.OnAirCaller;
            if (caller != null && _broadcastLoop != null)
            {
                _broadcastLoop.InterruptBroadcast(BroadcastInterruptionReason.CallerDropped, caller.Id);
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

        /// <summary>
        /// The cap's own body mesh: the first mesh under the cap that is not the
        /// lamp face or the printed label (both are parented to the cap too).
        /// </summary>
        private static MeshInstance3D? FindButtonBodyMesh(Node3D cap, string lampName)
        {
            if (cap is MeshInstance3D direct && direct.Name != lampName)
            {
                return direct;
            }

            foreach (var node in cap.FindChildren("*", recursive: true, owned: false))
            {
                if (node is MeshInstance3D mesh &&
                    mesh.Name != lampName &&
                    !mesh.Name.ToString().StartsWith("BtnLamp") &&
                    !mesh.Name.ToString().StartsWith("BtnLabel"))
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

                foreach (var slot in SoundboardPhysicalLayout.Slots)
                {
                    if (slot.Kind != ControlKind.Fader || slot.Control != SoundboardControl.CallerLevel ||
                        slot.LampName != lampName || _faderGlows.ContainsKey(slot.Control))
                    {
                        continue;
                    }

                    var glowMaterial = new StandardMaterial3D
                    {
                        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
                        AlbedoTexture = MakeHaloGradient(),
                        AlbedoColor = new Color(LedSelected.R, LedSelected.G, LedSelected.B, FaderGlowAlpha)
                    };
                    var glow = new MeshInstance3D
                    {
                        Name = $"FaderGlow_{slot.Control}",
                        Mesh = new QuadMesh { Size = new Vector2(FaderGlowSize, FaderGlowSize) },
                        MaterialOverride = glowMaterial,
                        RotationDegrees = new Vector3(-90f, 0f, 0f),
                        Position = new Vector3(0f, FaderGlowLift, 0f),
                        Visible = false
                    };
                    lamp.AddChild(glow);
                    _faderGlows[slot.Control] = glow;
                    _faderGlowMaterials[slot.Control] = glowMaterial;
                }
            }
        }

        private static IEnumerable<string> AllLampNames()
        {
            var seen = new HashSet<string>();
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
            SetLampColor("Lamp_1", BandColor(worst));
            SetLampColor("Lamp_0", SoundboardTargetGenerator.RampGreen);
            var adBreak = _adManager != null && _adManager.IsAdBreakActive;
            SetLampColor("Lamp_2", adBreak ? SoundboardTargetGenerator.RampGreen : LedDim);
            SetLampColor("Lamp_3L", SoundboardTargetGenerator.RampGreen);
            SetLampColor("Lamp_3R", SoundboardTargetGenerator.RampGreen);
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
        /// baseline up to its full base size as the channel's bus peak rises). The
        /// CallerLevel fader uses a separate radial glow at its channel lamp.
        /// Hover does not scale or brighten the glow; hover feedback is the part
        /// highlight and the channel-lamp brighten only.
        /// </summary>
        private void UpdateHalos()
        {
            if (!_handlesVisible || (_halos.Count == 0 && _faderGlows.Count == 0))
            {
                foreach (var ring in _halos.Values)
                {
                    ring.Visible = false;
                }
                foreach (var glow in _faderGlows.Values)
                {
                    glow.Visible = false;
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
                var glowLevel = ControlGlow(slot.Control);
                var haloColor = colored
                    ? GetSpeakingHaloColor(slot.Control, _speakingChannel, glowLevel, HaloAlpha)
                    : GetIdleHaloColor(slot.Control, glowLevel, HaloAlpha);

                material.AlbedoColor = haloColor;

                var baseSize = BaseHaloSize(slot.Control);
                var glowScale = SoundboardGlow.SizeScaleFromGlow(glowLevel);
                ((QuadMesh)ring.Mesh!).Size = baseSize * glowScale;

                ring.Position = part.Position + new Vector3(0f, HaloLift, 0f);
                ring.Visible = true;
            }

            UpdateFaderGlows();
        }

        private void UpdateFaderGlows()
        {
            foreach (var slot in SoundboardPhysicalLayout.Slots)
            {
                if (slot.Kind != ControlKind.Fader ||
                    !_faderGlows.TryGetValue(slot.Control, out var glow) || glow == null ||
                    !_faderGlowMaterials.TryGetValue(slot.Control, out var material) || material == null)
                {
                    continue;
                }

                var channel = SoundboardGlow.ChannelOf(slot.Control);
                var colored = channel != SoundboardGlow.SpeakingChannel.None &&
                              channel == _speakingChannel;
                var glowLevel = ControlGlow(slot.Control);
                var haloColor = colored
                    ? GetSpeakingHaloColor(slot.Control, _speakingChannel, glowLevel, FaderGlowAlpha)
                    : GetIdleHaloColor(slot.Control, glowLevel, FaderGlowAlpha);
                material.AlbedoColor = haloColor;

                var glowScale = SoundboardGlow.SizeScaleFromGlow(glowLevel);
                ((QuadMesh)glow.Mesh!).Size = new Vector2(FaderGlowSize, FaderGlowSize) * glowScale;
                glow.Visible = true;
            }
        }

        private static bool IsCallerControl(SoundboardControl control) =>
            SoundboardGlow.ChannelOf(control) == SoundboardGlow.SpeakingChannel.Caller;

        private Color GetSpeakingHaloColor(SoundboardControl control, SoundboardGlow.SpeakingChannel channel, float glowLevel, float alpha)
        {
            var statusColor = channel == SoundboardGlow.SpeakingChannel.Caller
                ? SoundboardTargetGenerator.ColorForError(ControlError(control))
                : channel == SoundboardGlow.SpeakingChannel.Vern
                    ? SoundboardTargetGenerator.RampGreen
                    : LedSelected;
            var dimFactor = SpeakingChannelDimFactor();
            var luminance = Mathf.Lerp(0.35f, 1f, Mathf.Clamp(glowLevel, 0f, 1f)) * dimFactor;
            return new Color(
                statusColor.R * luminance,
                statusColor.G * luminance,
                statusColor.B * luminance,
                alpha * dimFactor
            );
        }

        private float SpeakingChannelDimFactor()
        {
            if (_speakingChannel == SoundboardGlow.SpeakingChannel.None || _speakingChannelHold <= 0f)
            {
                return 1f;
            }

            float progress = Mathf.Clamp(_speakingChannelHold / SpeakingChannelHoldSeconds, 0f, 1f);
            return Mathf.Lerp(1f, 0.65f, progress);
        }

        private static Color GetIdleHaloColor(SoundboardControl control, float glowLevel) =>
            GetIdleHaloColor(control, glowLevel, HaloAlpha);

        private static Color GetIdleHaloColor(SoundboardControl control, float glowLevel, float alpha)
        {
            var intensity = Mathf.Lerp(0.3f, 1f, Mathf.Clamp(glowLevel, 0f, 1f));
            return new Color(
                LedSelected.R * intensity,
                LedSelected.G * intensity,
                LedSelected.B * intensity,
                alpha
            );
        }

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
            foreach (var body in _buttonBodies.Keys)
            {
                if (body == null)
                {
                    continue;
                }
                body.CollisionLayer = enabled ? HitLayer : 0u;
                body.CollisionMask = 0u;
            }
        }

        /// <summary>
        /// Resolves the board's system services lazily. The 3D world's scene tree
        /// is ready BEFORE <c>Main._Ready()</c> runs <c>ServiceProviderRoot.Initialize()</c>,
        /// so resolving once in _Ready would pin every service to null and silently
        /// no-op every button. Retried from _Process/TapButton until every service
        /// resolves; event subscriptions happen exactly once afterwards.
        /// </summary>
        private void ResolveServices()
        {
            if (_servicesResolved)
            {
                return;
            }

            if (_adManager == null) DependencyInjection.TryGet<AdManager>(this, out _adManager);
            if (_callerRepository == null) DependencyInjection.TryGet<ICallerRepository>(this, out _callerRepository);
            if (_broadcastLoop == null) DependencyInjection.TryGet<AsyncBroadcastLoop>(this, out _broadcastLoop);
            if (_audioService == null) DependencyInjection.TryGet<IBroadcastAudioService>(this, out _audioService);
            if (_eventBus == null) DependencyInjection.TryGet<EventBus>(this, out _eventBus);

            if (_adManager == null || _callerRepository == null || _broadcastLoop == null ||
                _audioService == null || _eventBus == null)
            {
                return;
            }

            _servicesResolved = true;
            if (!_eventsSubscribed)
            {
                _eventsSubscribed = true;
                _eventBus.Subscribe<BroadcastInterruptionEvent>(OnBroadcastInterruption);
                _eventBus.Subscribe<CursingTimerCompletedEvent>(OnCursingTimerCompleted);
            }
        }

        private void OnBroadcastInterruption(BroadcastInterruptionEvent e)
        {
            if (e.Reason == BroadcastInterruptionReason.CallerCursed)
            {
                _curseActive = true;
                _curseTimer = CurseWindowSeconds;
            }
        }

        private void OnCursingTimerCompleted(CursingTimerCompletedEvent e)
        {
            _curseActive = false;
            _curseTimer = 0f;
        }

        public override void _ExitTree()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<BroadcastInterruptionEvent>(OnBroadcastInterruption);
                _eventBus.Unsubscribe<CursingTimerCompletedEvent>(OnCursingTimerCompleted);
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
            var detectedChannel = SoundboardGlow.ChooseSpeakingChannel(callerPeak, vernPeak);

            if (detectedChannel != SoundboardGlow.SpeakingChannel.None)
            {
                _speakingChannel = detectedChannel;
                _speakingChannelHold = 0f;
            }
            else if (_speakingChannel != SoundboardGlow.SpeakingChannel.None)
            {
                _speakingChannelHold += Mathf.Clamp((float)delta, 0f, 1f);
                if (_speakingChannelHold >= SpeakingChannelHoldSeconds)
                {
                    _speakingChannel = SoundboardGlow.SpeakingChannel.None;
                    _speakingChannelHold = 0f;
                }
            }

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
