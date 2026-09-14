#nullable enable

using System;
using Godot;
using KBTV.Ads;
using KBTV.Audio;
using KBTV.Core;
using KBTV.Monitors;

namespace KBTV.UI
{
    /// <summary>
    /// Floating mixer-board overlay shown while the diegetic soundboard camera
    /// view is open (CanvasLayer 122, above the ScreenNavOverlay at 121).
    ///
    /// Owns a <see cref="SoundboardMixerDriver"/> (knob state -> AudioMixerManager)
    /// and reflects the <see cref="SoundboardMonitor"/>'s live CALLER bands plus
    /// Vern's mood-drain status. VERN is always shown GREEN (fixed channel); ADS
    /// lights toward its nominal target during an ad break and dims otherwise.
    /// </summary>
    public partial class SoundboardOverlay : CanvasLayer
    {
        private static readonly Color LedGreen = new(0.2f, 0.9f, 0.3f);
        private static readonly Color LedBlue = new(0.3f, 0.6f, 1.0f);
        private static readonly Color LedYellow = new(1.0f, 0.8f, 0.2f);
        private static readonly Color LedRed = new(0.9f, 0.25f, 0.2f);
        private static readonly Color LedDim = new(0.28f, 0.28f, 0.28f);

        private PanelContainer _root = null!;
        private Label _drainLabel = null!;
        private ColorRect _worstLed = null!;
        private Control _adsRow = null!;
        private ColorRect _adsLed = null!;
        private ColorRect[] _callerLeds = null!;

        private AudioMixerManager? _audioMixer;
        private AdManager? _adManager;
        private SoundboardMonitor? _monitor;
        private bool _built;

        /// <summary>The knob-state driver shared with World3D / the monitor.</summary>
        public SoundboardMixerDriver Driver { get; } = new();

        /// <summary>Used by World3D to connect the monitor (bands + drain status).</summary>
        public void SetMonitor(SoundboardMonitor monitor) => _monitor = monitor;

        public override void _Ready()
        {
            Layer = 122;
            Visible = false;
            _audioMixer = GetNodeOrNull<AudioMixerManager>("/root/AudioMixerManager");
            Driver.Attach(_audioMixer);
            ResolveAdManager();
            BuildUi();
        }

        public override void _Process(double delta)
        {
            if (!_built || !Visible)
            {
                return;
            }

            var bands = CurrentCallerBands();
            SetCallerLedColors(bands);
            _worstLed.Color = BandColor(SoundboardTargetGenerator.GetWorstBand(bands));
            UpdateAdsRow();
            UpdateDrainLabel();
        }

        public void ShowSoundboard()
        {
            Driver.ResetToNeutral();
            Driver.Apply();
            RefreshLeds();
            Visible = true;
        }

        public void HideSoundboard()
        {
            Visible = false;
        }

        private SoundboardCallerBands CurrentCallerBands()
        {
            if (_monitor != null)
            {
                return _monitor.CallerBands;
            }

            return SoundboardTargetGenerator.GetCallerBands(Driver.State, 0.5f);
        }

        private void RefreshLeds()
        {
            SetCallerLedColors(CurrentCallerBands());
            _worstLed.Color = BandColor(SoundboardTargetGenerator.GetWorstBand(CurrentCallerBands()));
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

        private void BuildUi()
        {
            if (_built)
            {
                return;
            }

            _root = new PanelContainer
            {
                Name = "SoundboardPanel",
                MouseFilter = Control.MouseFilterEnum.Stop
            };
            var style = new StyleBoxFlat
            {
                BgColor = UITheme.BG_PANEL,
                BorderColor = UITheme.BG_BORDER,
                CornerRadiusTopLeft = 2,
                CornerRadiusTopRight = 2,
                CornerRadiusBottomLeft = 2,
                CornerRadiusBottomRight = 2
            };
            style.BorderWidthBottom = 1;
            style.BorderWidthTop = 1;
            style.BorderWidthLeft = 1;
            style.BorderWidthRight = 1;
            _root.AddThemeStyleboxOverride("panel", style);
            _root.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
            _root.Position = new Vector2(-230f, -260f);
            _root.Size = new Vector2(460f, 252f);
            AddChild(_root);

            var margin = new MarginContainer();
            UITheme.ApplyMargins(margin, 10f, 8f, 10f, 8f);
            _root.AddChild(margin);

            var layout = new VBoxContainer();
            layout.AddThemeConstantOverride("separation", 4);
            margin.AddChild(layout);

            var header = new HBoxContainer();
            header.AddThemeConstantOverride("separation", 6);
            layout.AddChild(header);

            var title = new Label
            {
                Text = "SOUNDBOARD",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            UITheme.ApplyLabelStyle(title);
            title.AddThemeFontSizeOverride("font_size", UITheme.FONT_MEDIUM);
            header.AddChild(title);

            _drainLabel = new Label
            {
                Text = "OFF AIR",
                HorizontalAlignment = HorizontalAlignment.Right,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            UITheme.ApplyLabelStyle(_drainLabel, isPrimary: false);
            _drainLabel.AddThemeFontSizeOverride("font_size", UITheme.FONT_TINY);
            header.AddChild(_drainLabel);

            // VERN — locked green (fixed chain, not fiddled).
            BuildChannelRow(layout, "VERN", KnobKind.VernGain, KnobKind.None, KnobKind.None,
                out var vernSlider, out var vernLed, out _, out _, out _, out _);
            vernSlider.Editable = false;
            vernSlider.MouseFilter = Control.MouseFilterEnum.Ignore;
            vernLed.Color = LedGreen;

            // CALLER — live LEDs driven by the target generator.
            _callerLeds = new ColorRect[3];
            BuildChannelRow(
                layout, "CALLER",
                KnobKind.CallerGain, KnobKind.CallerLowPass, KnobKind.CallerHighPass,
                out _, out _callerLeds[0], out _, out _callerLeds[1], out _, out _callerLeds[2]);

            // ADS/BUMPER — nominal target; lit during an ad break.
            _adsRow = BuildChannelRow(layout, "ADS/BUMPER", KnobKind.AdsGain, KnobKind.None, KnobKind.None,
                out _, out _adsLed, out _, out _, out _, out _);

            BuildMasterRow(layout);

            _built = true;
        }

        private static Color BandColor(SoundboardBand band) => band switch
        {
            SoundboardBand.Green => LedGreen,
            SoundboardBand.Blue => LedBlue,
            SoundboardBand.Yellow => LedYellow,
            SoundboardBand.Red => LedRed,
            _ => LedDim
        };

        private enum KnobKind
        {
            None,
            CallerGain,
            CallerLowPass,
            CallerHighPass,
            VernGain,
            AdsGain,
            Master
        }

        private Control BuildChannelRow(
            VBoxContainer parent,
            string channelName,
            KnobKind knobA, KnobKind knobB, KnobKind knobC,
            out VSlider sliderA, out ColorRect ledA,
            out VSlider sliderB, out ColorRect ledB,
            out VSlider sliderC, out ColorRect ledC)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);
            row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            parent.AddChild(row);

            var nameLabel = new Label
            {
                Text = channelName,
                CustomMinimumSize = new Vector2(82f, 0f),
                VerticalAlignment = VerticalAlignment.Center
            };
            UITheme.ApplyLabelStyle(nameLabel);
            nameLabel.AddThemeFontSizeOverride("font_size", UITheme.FONT_TINY);
            row.AddChild(nameLabel);

            sliderA = AddKnobSlot(row, Labelling(knobA), knobA, out ledA);
            if (knobB != KnobKind.None)
            {
                sliderB = AddKnobSlot(row, Labelling(knobB), knobB, out ledB);
            }
            else
            {
                sliderB = null!;
                ledB = null!;
            }

            if (knobC != KnobKind.None)
            {
                sliderC = AddKnobSlot(row, Labelling(knobC), knobC, out ledC);
            }
            else
            {
                sliderC = null!;
                ledC = null!;
            }

            return row;
        }

        private Control BuildChannelRow(
            VBoxContainer parent,
            string channelName,
            KnobKind knobA, KnobKind knobB, KnobKind knobC,
            out VSlider sliderA, out ColorRect ledA)
        {
            return BuildChannelRow(
                parent, channelName, knobA, knobB, knobC,
                out sliderA, out ledA, out _, out _, out _, out _);
        }

        private static string Labelling(KnobKind kind) => kind switch
        {
            KnobKind.CallerGain => "GAIN",
            KnobKind.CallerLowPass => "LOW",
            KnobKind.CallerHighPass => "HIGH",
            KnobKind.VernGain => "GAIN",
            KnobKind.AdsGain => "GAIN",
            KnobKind.Master => "OUTPUT",
            _ => ""
        };

        private VSlider AddKnobSlot(HBoxContainer row, string labelText, KnobKind kind, out ColorRect led)
        {
            var slot = new VBoxContainer();
            slot.AddThemeConstantOverride("separation", 2);
            slot.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(slot);

            var label = new Label { Text = labelText, HorizontalAlignment = HorizontalAlignment.Center };
            UITheme.ApplyLabelStyle(label, isPrimary: false);
            label.AddThemeFontSizeOverride("font_size", UITheme.FONT_TINY);
            slot.AddChild(label);

            var slider = new VSlider
            {
                MinValue = 0f,
                MaxValue = 1f,
                Step = 0.01f,
                Value = 0.5f,
                CustomMinimumSize = new Vector2(0f, 54f),
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            };
            slider.ValueChanged += (double value) => OnKnobChanged(kind, (float)value);
            slot.AddChild(slider);

            led = new ColorRect
            {
                CustomMinimumSize = new Vector2(10f, 10f),
                Color = LedDim,
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            slot.AddChild(led);

            return slider;
        }

        private void BuildMasterRow(VBoxContainer parent)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);
            row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            parent.AddChild(row);

            var nameLabel = new Label
            {
                Text = "MASTER",
                CustomMinimumSize = new Vector2(82f, 0f),
                VerticalAlignment = VerticalAlignment.Center
            };
            UITheme.ApplyLabelStyle(nameLabel);
            nameLabel.AddThemeFontSizeOverride("font_size", UITheme.FONT_TINY);
            row.AddChild(nameLabel);

            var slot = new VBoxContainer();
            slot.AddThemeConstantOverride("separation", 2);
            slot.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(slot);

            var label = new Label { Text = "OUTPUT", HorizontalAlignment = HorizontalAlignment.Center };
            UITheme.ApplyLabelStyle(label, isPrimary: false);
            label.AddThemeFontSizeOverride("font_size", UITheme.FONT_TINY);
            slot.AddChild(label);

            var fader = new VSlider
            {
                MinValue = 0f,
                MaxValue = 1f,
                Step = 0.01f,
                Value = 0.5f,
                CustomMinimumSize = new Vector2(0f, 54f),
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            };
            fader.ValueChanged += (double value) => OnKnobChanged(KnobKind.Master, (float)value);
            slot.AddChild(fader);

            _worstLed = new ColorRect
            {
                CustomMinimumSize = new Vector2(10f, 10f),
                Color = LedDim,
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            slot.AddChild(_worstLed);
        }

        private void OnKnobChanged(KnobKind kind, float value)
        {
            switch (kind)
            {
                case KnobKind.CallerGain:
                    Driver.State.CallerGain = value;
                    break;
                case KnobKind.CallerLowPass:
                    Driver.State.CallerLowPass = value;
                    break;
                case KnobKind.CallerHighPass:
                    Driver.State.CallerHighPass = value;
                    break;
                case KnobKind.VernGain:
                    Driver.State.VernGain = value;
                    break;
                case KnobKind.AdsGain:
                    Driver.State.AdsGain = value;
                    break;
                case KnobKind.Master:
                    Driver.State.Fader = value;
                    break;
            }

            Driver.Apply();
            RefreshLeds();
        }

        private void SetCallerLedColors(SoundboardCallerBands bands)
        {
            if (_callerLeds[0] != null)
            {
                _callerLeds[0].Color = BandColor(bands.Gain);
                _callerLeds[1].Color = BandColor(bands.LowPass);
                _callerLeds[2].Color = BandColor(bands.HighPass);
            }
        }

        private void UpdateAdsRow()
        {
            var adBreak = _adManager != null && _adManager.IsAdBreakActive;
            _adsRow.Modulate = new Color(1f, 1f, 1f, adBreak ? 1f : 0.5f);
            _adsLed.Color = adBreak ? LedGreen : LedDim;
        }

        private void UpdateDrainLabel()
        {
            if (_monitor == null)
            {
                _drainLabel.Text = "OFF AIR";
                _drainLabel.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
                return;
            }

            if (_monitor.IsCallerOnAir && _monitor.GraceRemaining > 0f)
            {
                _drainLabel.Text = $"GRACE {Mathf.Ceil(_monitor.GraceRemaining):0}s";
                _drainLabel.AddThemeColorOverride("font_color", UITheme.ACCENT_GOLD);
            }
            else if (_monitor.IsDraining)
            {
                _drainLabel.Text = $"DRAIN -{_monitor.CurrentDrainRate:0.0}/s";
                _drainLabel.AddThemeColorOverride("font_color", UITheme.ACCENT_RED);
            }
            else if (_monitor.IsCallerOnAir)
            {
                _drainLabel.Text = "MIXED PERFECT";
                _drainLabel.AddThemeColorOverride("font_color", UITheme.ACCENT_GREEN);
            }
            else
            {
                _drainLabel.Text = "OFF AIR";
                _drainLabel.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
            }
        }
    }
}