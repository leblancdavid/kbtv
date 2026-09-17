#nullable enable

using Godot;
using KBTV.Audio;
using KBTV.Monitors;

namespace KBTV.UI
{
    /// <summary>
    /// Minimal drain-status readout shown while the diegetic 3D soundboard view is
    /// open. The mixer itself is now the 3D board (handles + LEDs built by
    /// World3D.Soundboard3D); this tiny 2D label keeps the OFF AIR / GRACE / DRAIN /
    /// MIXED PERFECT status legible. Mouse-transparent so the board raycast wins.
    /// </summary>
    public partial class SoundboardOverlay : CanvasLayer
    {
        private Label _drainLabel = null!;
        private SoundboardMonitor? _monitor;
        private bool _built;

        /// <summary>The knob-state driver shared with World3D / the monitor.</summary>
        public SoundboardMixerDriver Driver { get; } = new();

        /// <summary>Used by World3D to connect the monitor (drain status).</summary>
        public void SetMonitor(SoundboardMonitor monitor) => _monitor = monitor;

        public override void _Ready()
        {
            Layer = 122;
            Visible = false;
            Driver.Attach(GetNodeOrNull<AudioMixerManager>("/root/AudioMixerManager"));
            BuildUi();
        }

        public override void _Process(double delta)
        {
            if (!_built || !Visible)
            {
                return;
            }

            UpdateDrainLabel();
        }

        public void ShowSoundboard()
        {
            Driver.Apply();
            Visible = true;
        }

        public void HideSoundboard()
        {
            Visible = false;
        }

        private void BuildUi()
        {
            if (_built)
            {
                return;
            }

            var root = new PanelContainer
            {
                Name = "SoundboardStatusPanel",
                MouseFilter = Control.MouseFilterEnum.Ignore
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
            root.AddThemeStyleboxOverride("panel", style);
            root.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
            root.Position = new Vector2(-160f, 14f);
            root.Size = new Vector2(320f, 28f);
            AddChild(root);

            var margin = new MarginContainer();
            UITheme.ApplyMargins(margin, 10f, 4f, 10f, 4f);
            root.AddChild(margin);

            _drainLabel = new Label
            {
                Text = "OFF AIR",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            UITheme.ApplyLabelStyle(_drainLabel, isPrimary: false);
            _drainLabel.AddThemeFontSizeOverride("font_size", UITheme.FONT_TINY);
            margin.AddChild(_drainLabel);

            _built = true;
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