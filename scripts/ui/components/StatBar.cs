#nullable enable

using Godot;
using KBTV.Data;
using KBTV.UI.Themes;

namespace KBTV.UI.Components
{
    /// <summary>
    /// Reusable stat bar component displaying a stat with label, progress bar, and value.
    /// Format: "STAT_NAME   [████████░░░░░░░░░░]  75/100"
    /// Subscribes to Stat.OnValueChanged for real-time updates with smooth animation.
    /// </summary>
    public partial class StatBar : HBoxContainer
    {
        private const float ANIMATION_DURATION = 0.5f;

        // Child nodes
        private Label _nameLabel = null!;
        private ProgressBar _bar = null!;
        private Label _valueLabel = null!;

        // State
        private Stat? _stat;
        private Tween? _currentTween;

        public override void _Ready()
        {
            // Create name label (CAPS, cyan)
            _nameLabel = new Label
            {
                CustomMinimumSize = new Vector2(100, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            _nameLabel.AddThemeColorOverride("font_color", UIColors.Property.Name);
            AddChild(_nameLabel);

            // Create progress bar
            _bar = new ProgressBar
            {
                CustomMinimumSize = new Vector2(150, 20),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MaxValue = 100,
                Value = 0,
                ShowPercentage = false
            };
            AddChild(_bar);

            // Create value label
            _valueLabel = new Label
            {
                CustomMinimumSize = new Vector2(60, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            _valueLabel.AddThemeColorOverride("font_color", UIColors.TEXT_PRIMARY);
            AddChild(_valueLabel);

            // Apply monospace font to all labels
            var monoFont = new SystemFont();
            monoFont.FontNames = new string[] { "Consolas", "Courier New", "Liberation Mono", "monospace" };
            _nameLabel.AddThemeFontOverride("font", monoFont);
            _valueLabel.AddThemeFontOverride("font", monoFont);

            // Style the progress bar background
            var bgStyle = new StyleBoxFlat
            {
                BgColor = UIColors.VernStat.BarBackground,
                CornerRadiusTopLeft = 2,
                CornerRadiusTopRight = 2,
                CornerRadiusBottomLeft = 2,
                CornerRadiusBottomRight = 2
            };
            _bar.AddThemeStyleboxOverride("background", bgStyle);

            // Initial update if stat already set
            if (_stat != null)
            {
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Bind this bar to a stat. Subscribes to value changes for real-time updates.
        /// </summary>
        public void SetStat(Stat stat)
        {
            // Unsubscribe from previous stat
            if (_stat != null)
            {
                _stat.OnValueChanged -= OnStatValueChanged;
            }

            _stat = stat;

            // Subscribe to new stat
            if (_stat != null)
            {
                _stat.OnValueChanged += OnStatValueChanged;
            }

            // Update display if ready
            if (_nameLabel != null)
            {
                UpdateDisplay();
            }
        }

        private void OnStatValueChanged(float oldValue, float newValue)
        {
            AnimateToValue(newValue);
            UpdateValueLabel();
            UpdateBarColor();
        }

        private void UpdateDisplay()
        {
            if (_stat == null) return;

            _nameLabel.Text = _stat.Name.ToUpper();
            _bar.MinValue = _stat.MinValue;
            _bar.MaxValue = _stat.MaxValue;
            _bar.Value = _stat.Value;

            UpdateValueLabel();
            UpdateBarColor();
        }

        private void UpdateValueLabel()
        {
            if (_stat == null) return;
            _valueLabel.Text = $"{(int)_stat.Value}/{(int)_stat.MaxValue}";
        }

        private void UpdateBarColor()
        {
            if (_stat == null) return;

            Color fillColor = UIColors.GetVernStatColor(_stat.Normalized);

            var fillStyle = new StyleBoxFlat
            {
                BgColor = fillColor,
                CornerRadiusTopLeft = 2,
                CornerRadiusTopRight = 2,
                CornerRadiusBottomLeft = 2,
                CornerRadiusBottomRight = 2
            };
            _bar.AddThemeStyleboxOverride("fill", fillStyle);
        }

        private void AnimateToValue(float targetValue)
        {
            // Cancel any existing tween
            _currentTween?.Kill();

            // Create new tween for smooth animation
            _currentTween = CreateTween();
            _currentTween.TweenProperty(_bar, "value", targetValue, ANIMATION_DURATION)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Quad);
        }

        public override void _ExitTree()
        {
            // Unsubscribe from stat changes
            if (_stat != null)
            {
                _stat.OnValueChanged -= OnStatValueChanged;
            }

            _currentTween?.Kill();
        }
    }
}
