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

        private static readonly Color LedGreen = new(0.2f, 0.9f, 0.3f);
        private static readonly Color LedBlue = new(0.3f, 0.6f, 1.0f);
        private static readonly Color LedYellow = new(1.0f, 0.8f, 0.2f);
        private static readonly Color LedRed = new(0.9f, 0.25f, 0.2f);
        private static readonly Color LedDim = new(0.28f, 0.28f, 0.28f);
        private static readonly Color LedSelected = new(0.9f, 0.95f, 1.0f);

        private readonly Dictionary<SoundboardControl, Node3D> _parts = new();
        private readonly Dictionary<SoundboardControl, Vector3> _restPositions = new();
        private readonly Dictionary<string, MeshInstance3D> _lamps = new();
        private readonly Dictionary<string, StandardMaterial3D> _lampMaterials = new();
        private readonly Dictionary<StaticBody3D, SoundboardControl> _bodies = new();
        private readonly HashSet<string> _warnedMissing = new();

        private Node3D? _board;
        private SoundboardMonitor? _monitor;
        private AdManager? _adManager;
        private MeshInstance3D? _halo;
        private StandardMaterial3D? _haloMaterial;
        private StandardMaterial3D? _hoverMaterial;
        private Node3D? _hoverPart;
        private bool _built;
        private bool _handlesVisible;

        /// <summary>Height of the hover halo above the board face (glTF-local +Y).</summary>
        private const float HaloLift = 0.02f;

        /// <summary>How far the halo sits toward the operator (board-local -Z) of the part centre.</summary>
        private const float HaloTowardCameraZ = 0.045f;

        /// <summary>Side of the square hover halo quad.</summary>
        private const float HaloSize = 0.24f;

        /// <summary>Shared knob-state driver (World3D points this at the overlay's driver).</summary>
        public SoundboardMixerDriver Driver { get; private set; } = new();

        /// <summary>Currently selected control (or None).</summary>
        public SoundboardControl SelectedControl { get; private set; } = SoundboardControl.None;

        /// <summary>Currently hovered control (or None). Drives the halo + lamp brighten.</summary>
        public SoundboardControl HoveredControl { get; private set; } = SoundboardControl.None;

        public override void _Ready()
        {
            BuildHoverVisuals();
            ResolveAdManager();
        }

        public override void _Process(double delta)
        {
            if (!_built || !_handlesVisible)
            {
                return;
            }

            UpdateControls();
            UpdateLeds();
            UpdateHoverVisual();
        }

        /// <summary>Creates the shared halo quad + hover highlight material.</summary>
        private void BuildHoverVisuals()
        {
            _hoverMaterial = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                AlbedoColor = new Color(0.45f, 0.78f, 1f),
                EmissionEnabled = true,
                Emission = new Color(0.35f, 0.7f, 1f),
                EmissionEnergyMultiplier = 0.9f
            };

            _haloMaterial = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                NoDepthTest = true,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled,
                AlbedoTexture = MakeHaloGradient(),
                AlbedoColor = new Color(0.3f, 1f, 0.55f)
            };

            _halo = new MeshInstance3D
            {
                Name = "HoverHalo",
                Mesh = new QuadMesh { Size = new Vector2(HaloSize, HaloSize) },
                MaterialOverride = _haloMaterial,
                RotationDegrees = new Vector3(-90f, 0f, 0f),
                Visible = false
            };
            AddChild(_halo);
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
            BuildLamps();
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

        /// <summary>Shows the handles and returns them to neutral.</summary>
        public void ShowHandles()
        {
            if (!_built)
            {
                return;
            }
            SelectedControl = SoundboardControl.None;
            HoveredControl = SoundboardControl.None;
            Driver.ResetToNeutral();
            Driver.Apply();
            SetBodiesEnabled(true);
            _handlesVisible = true;
            UpdateControls();
            UpdateLeds();
            UpdateHoverVisual();
        }

        /// <summary>Hides the handles and disables their tap-targets.</summary>
        public void HideHandles()
        {
            SelectedControl = SoundboardControl.None;
            HoveredControl = SoundboardControl.None;
            _handlesVisible = false;
            SetBodiesEnabled(false);
            UpdateHoverVisual();
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

        /// <summary>Sets the hovered control (None clears). Drives the halo + lamp/highlight.</summary>
        public void SetHover(SoundboardControl control)
        {
            if (HoveredControl == control)
            {
                return;
            }

            HoveredControl = control;
            if (_built)
            {
                UpdateLeds();
                UpdateHoverVisual();
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
                // Track the full fader travel (0.22 deep) / knob extent (0.12–0.18) so
                // the whole slot is a tap target, not just the cap geometry.
                Shape = new BoxShape3D
                {
                    Size = new Vector3(isFader ? 0.08f : 0.11f, 0.06f, isFader ? 0.22f : 0.14f)
                }
            };
            if (!isFader && control == SoundboardControl.Master)
            {
                shape.Shape = new BoxShape3D { Size = new Vector3(0.2f, 0.08f, 0.18f) };
            }
            body.AddChild(shape);
            AddChild(body);
            _bodies[body] = control;
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
            SetLampColor("Lamp_7", LedGreen);
            var adBreak = _adManager != null && _adManager.IsAdBreakActive;
            SetLampColor("Lamp_5", adBreak ? LedGreen : LedDim);
            SetLampColor("Lamp_3", LedGreen);
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
        /// Places the color-coded halo over the hovered part and applies the shared
        /// hover highlight to its mesh each frame.
        /// </summary>
        private void UpdateHoverVisual()
        {
            Node3D? part = null;
            var showHalo = _handlesVisible &&
                           HoveredControl != SoundboardControl.None &&
                           _parts.TryGetValue(HoveredControl, out part) && part != null;

            if (showHalo)
            {
                _halo!.Position = part!.Position + new Vector3(0f, HaloLift, HaloTowardCameraZ);
                _haloMaterial!.AlbedoColor = BandColor(HoveredControlBand());
                _halo.Visible = true;
            }

            if (_hoverPart != part)
            {
                if (_hoverPart is MeshInstance3D previous)
                {
                    previous.MaterialOverride = null;
                }
                _hoverPart = showHalo ? part : null;
                if (_hoverPart is MeshInstance3D current)
                {
                    current.MaterialOverride = _hoverMaterial;
                }
            }

            if (!showHalo && _halo != null)
            {
                _halo.Visible = false;
            }
        }

        /// <summary>
        /// Band of the hovered control. Caller knobs use the live caller bands (from the
        /// monitor when attached), all others compare their current value to neutral.
        /// </summary>
        private SoundboardBand HoveredControlBand()
        {
            var bands = CurrentCallerBands();
            switch (HoveredControl)
            {
                case SoundboardControl.CallerGain:
                    return bands.Gain;
                case SoundboardControl.CallerLowPass:
                    return bands.LowPass;
                case SoundboardControl.CallerHighPass:
                    return bands.HighPass;
                default:
                    return SoundboardTargetGenerator.GetBand(
                        SoundboardControlApplier.CurrentValue(Driver.State, HoveredControl),
                        SoundboardKnobState.NeutralValue);
            }
        }

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

        private static Color BandColor(SoundboardBand band) => band switch
        {
            SoundboardBand.Green => LedGreen,
            SoundboardBand.Blue => LedBlue,
            SoundboardBand.Yellow => LedYellow,
            SoundboardBand.Red => LedRed,
            _ => LedDim
        };
    }
}