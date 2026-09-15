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
    /// interaction: builds one handle per <see cref="SoundboardControl"/> over the
    /// soundboard.glb body, slides faders / rotates knobs from driver state each
    /// frame, and exposes tap-target colliders (on a dedicated collision layer) so
    /// World3D can raycast clicks and drags. Mirrors the ComputerTerminal3D pattern:
    /// a hidden sibling node that is revealed when the diegetic view opens.
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

        private const float SlotCenterY = 0.38f;
        private const float FaderTravel = 0.16f;
        private const float KnobTurnDeg = 90f;
        private const float CoverZ = -0.09f;
        private const float ColliderZ = -0.16f;
        private const float LedY = 0.60f;

        private static readonly (SoundboardControl Control, bool IsFader, float X)[] SlotLayout =
        {
            (SoundboardControl.CallerGain, true, -0.60f),
            (SoundboardControl.CallerLowPass, false, -0.36f),
            (SoundboardControl.CallerHighPass, false, -0.12f),
            (SoundboardControl.VernGain, true, 0.12f),
            (SoundboardControl.AdsGain, true, 0.36f),
            (SoundboardControl.Master, true, 0.60f)
        };

        private readonly Dictionary<SoundboardControl, MeshInstance3D> _visuals = new();
        private readonly Dictionary<SoundboardControl, Node3D> _roots = new();
        private readonly Dictionary<SoundboardControl, MeshInstance3D> _leds = new();
        private readonly Dictionary<SoundboardControl, StandardMaterial3D> _ledMaterials = new();
        private readonly Dictionary<StaticBody3D, SoundboardControl> _bodies = new();

        private StandardMaterial3D? _normalMat;
        private StandardMaterial3D? _knobMat;
        private StandardMaterial3D? _selectedMat;
        private SoundboardMonitor? _monitor;
        private AdManager? _adManager;
        private bool _built;
        private bool _handlesVisible;

        /// <summary>Shared knob-state driver (World3D points this at the overlay's driver).</summary>
        public SoundboardMixerDriver Driver { get; private set; } = new();

        /// <summary>Currently selected control (or None).</summary>
        public SoundboardControl SelectedControl { get; private set; } = SoundboardControl.None;

        public override void _Ready()
        {
            Visible = false;
            Build();
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
        /// Returns the control a tap-target collider belongs to. Use with the
        /// colliders from <see cref="GetBodies"/> after a raycast.
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
            Build();
            SelectedControl = SoundboardControl.None;
            Driver.ResetToNeutral();
            Driver.Apply();
            Visible = true;
            SetBodiesEnabled(true);
            _handlesVisible = true;
            UpdateControls();
            UpdateLeds();
        }

        /// <summary>Hides the handles and disables their tap-targets.</summary>
        public void HideHandles()
        {
            SelectedControl = SoundboardControl.None;
            _handlesVisible = false;
            Visible = false;
            SetBodiesEnabled(false);
        }

        /// <summary>Selects a control (highlights its handle); None clears the selection.</summary>
        public void SelectControl(SoundboardControl control)
        {
            if (SelectedControl == control)
            {
                return;
            }

            SelectedControl = control;
            foreach (var pair in _visuals)
            {
                if (pair.Value == null)
                {
                    continue;
                }
                pair.Value.MaterialOverride = pair.Key == control ? _selectedMat : null;
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
            UpdateControlVisual(control);
        }

        private void Build()
        {
            if (_built)
            {
                return;
            }

            _normalMat = MakeMaterial(new Color(0.16f, 0.18f, 0.22f));
            _knobMat = MakeMaterial(new Color(0.26f, 0.24f, 0.22f));
            _selectedMat = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                EmissionEnabled = true,
                Emission = new Color(0.3f, 0.8f, 0.7f, 1f),
                EmissionEnergyMultiplier = 2.2f,
                AlbedoColor = new Color(0.35f, 0.85f, 0.75f, 1f)
            };

            var root = new Node3D { Name = "SoundboardHandles" };
            AddChild(root);

            foreach (var (control, isFader, x) in SlotLayout)
            {
                BuildControl(root, control, isFader, x);
            }

            _built = true;
        }

        private void BuildControl(Node3D parent, SoundboardControl control, bool isFader, float x)
        {
            // Background track for the slot.
            var background = new MeshInstance3D
            {
                Name = $"Slot_{control}",
                Mesh = new BoxMesh
                {
                    Size = new Vector3(isFader ? 0.12f : 0.16f, 0.46f, 0.02f),
                    Material = _normalMat
                },
                Position = new Vector3(x, SlotCenterY, CoverZ)
            };
            parent.AddChild(background);

            // Movable handle root (faders slide on Y, knobs rotate on Y).
            var handleRoot = new Node3D { Name = $"Handle_{control}", Position = new Vector3(x, SlotCenterY, CoverZ) };
            parent.AddChild(handleRoot);

            MeshInstance3D visual;
            if (isFader)
            {
                var cap = new MeshInstance3D
                {
                    Name = "Cap",
                    Mesh = new BoxMesh
                    {
                        Size = new Vector3(0.13f, 0.09f, 0.035f),
                        Material = _normalMat
                    }
                };
                handleRoot.AddChild(cap);
                visual = cap;
            }
            else
            {
                var knob = new MeshInstance3D
                {
                    Name = "Knob",
                    Mesh = new CylinderMesh
                    {
                        TopRadius = 0.045f,
                        BottomRadius = 0.045f,
                        Height = 0.06f,
                        RadialSegments = 10,
                        Material = _knobMat
                    },
                    Position = new Vector3(0f, 0.05f, 0f)
                };
                handleRoot.AddChild(knob);

                var pointer = new MeshInstance3D
                {
                    Name = "Pointer",
                    Mesh = new BoxMesh
                    {
                        Size = new Vector3(0.07f, 0.02f, 0.02f),
                        Material = _normalMat
                    },
                    Position = new Vector3(0f, 0.09f, 0f)
                };
                handleRoot.AddChild(pointer);
                visual = knob;
            }

            // Tap-target collider, in front of the cover face, isolated to HitLayer.
            var body = new StaticBody3D
            {
                Name = $"Hit_{control}",
                Position = new Vector3(0f, 0f, ColliderZ - CoverZ),
                CollisionLayer = HitLayer,
                CollisionMask = 0u
            };
            var shapeNode = new CollisionShape3D
            {
                Shape = new BoxShape3D { Size = new Vector3(isFader ? 0.2f : 0.24f, 0.56f, 0.24f) }
            };
            body.AddChild(shapeNode);
            handleRoot.AddChild(body);
            _bodies[body] = control;

            // Indicator LED above the slot.
            var ledMat = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.05f, 0.05f, 0.06f, 1f),
                EmissionEnabled = true,
                Emission = LedDim,
                EmissionEnergyMultiplier = 3f
            };
            var led = new MeshInstance3D
            {
                Name = $"Led_{control}",
                Mesh = new SphereMesh
                {
                    Radius = 0.014f,
                    Height = 0.028f,
                    RadialSegments = 8,
                    Rings = 5
                },
                Position = new Vector3(x, LedY, CoverZ + 0.01f)
            };
            led.MaterialOverride = ledMat;
            parent.AddChild(led);

            _roots[control] = handleRoot;
            _visuals[control] = visual;
            _leds[control] = led;
            _ledMaterials[control] = ledMat;
        }

        private void UpdateControls()
        {
            foreach (var (control, _, _) in SlotLayout)
            {
                UpdateControlVisual(control);
            }
        }

        private void UpdateControlVisual(SoundboardControl control)
        {
            if (!_roots.TryGetValue(control, out var root) || root == null)
            {
                return;
            }

            var value = SoundboardControlApplier.CurrentValue(Driver.State, control);
            bool isFader = control is SoundboardControl.CallerGain or SoundboardControl.VernGain
                or SoundboardControl.AdsGain or SoundboardControl.Master;
            if (isFader)
            {
                root.Position = new Vector3(root.Position.X, FaderLocalY(value), root.Position.Z);
            }
            else
            {
                root.RotationDegrees = new Vector3(0f, KnobRotationDeg(value), 0f);
            }
        }

        private void UpdateLeds()
        {
            var bands = CurrentCallerBands();
            SetLedColor(SoundboardControl.CallerGain, BandColor(bands.Gain));
            SetLedColor(SoundboardControl.CallerLowPass, BandColor(bands.LowPass));
            SetLedColor(SoundboardControl.CallerHighPass, BandColor(bands.HighPass));
            SetLedColor(SoundboardControl.VernGain, LedGreen);
            var adBreak = _adManager != null && _adManager.IsAdBreakActive;
            SetLedColor(SoundboardControl.AdsGain, adBreak ? LedGreen : LedDim);
            SetLedColor(SoundboardControl.Master, BandColor(SoundboardTargetGenerator.GetWorstBand(bands)));
        }

        private SoundboardCallerBands CurrentCallerBands()
        {
            if (_monitor != null)
            {
                return _monitor.CallerBands;
            }

            return SoundboardTargetGenerator.GetCallerBands(Driver.State, 0.5f);
        }

        private void SetLedColor(SoundboardControl control, Color color)
        {
            if (!_ledMaterials.TryGetValue(control, out var material) || material == null)
            {
                return;
            }

            var isSelected = control == SelectedControl;
            material.Emission = isSelected ? LedSelected : color;
            material.AlbedoColor = isSelected ? LedSelected : new Color(0.05f, 0.05f, 0.06f, 1f);
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

        /// <summary>Fader cap local Y for a knob value (-travel at 0 .. +travel at 1).</summary>
        public static float FaderLocalY(float value) => (value - 0.5f) * 2f * FaderTravel;

        /// <summary>Knob rotation in degrees for a knob value (full swing around neutral).</summary>
        public static float KnobRotationDeg(float value) => (value - 0.5f) * 2f * KnobTurnDeg;

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

        private static StandardMaterial3D MakeMaterial(Color color)
        {
            return new StandardMaterial3D { AlbedoColor = color };
        }
    }
}