#nullable enable

using Godot;
using KBTV.Data;
using KBTV.UI.Themes;

namespace KBTV.UI.Components
{
    /// <summary>
    /// Stat bar for centered display (-100 to +100 range).
    /// Bar fills left (red) for negative values, right (green) for positive.
    /// Format: "PHYSICAL   [░░░░░░░░░░|███░░░░░░]  +30"
    /// </summary>
    public partial class CenteredStatBar : HBoxContainer
    {
        private const float ANIMATION_DURATION = 0.5f;

        // Child nodes
        private Label _nameLabel = null!;
        private Control _barContainer = null!;
        private ColorRect _negativeBar = null!;
        private ColorRect _positiveBar = null!;
        private ColorRect _centerLine = null!;
        private Label _valueLabel = null!;

        // State
        private Stat? _stat;
        private Tween? _currentTween;
        private float _displayValue = 0f;

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

            // Create bar container (custom drawn)
            _barContainer = new Control
            {
                CustomMinimumSize = new Vector2(150, 20),
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            AddChild(_barContainer);

            // Background
            var background = new ColorRect
            {
                Color = UIColors.VernStat.BarBackground,
                AnchorRight = 1.0f,
                AnchorBottom = 1.0f
            };
            _barContainer.AddChild(background);

            // Negative bar (left side, red) - starts at center, grows left
            _negativeBar = new ColorRect
            {
                Color = UIColors.VernStat.SpiritNegative,
                AnchorTop = 0.1f,
                AnchorBottom = 0.9f,
                AnchorLeft = 0.5f,
                AnchorRight = 0.5f
            };
            _barContainer.AddChild(_negativeBar);

            // Positive bar (right side, green) - starts at center, grows right
            _positiveBar = new ColorRect
            {
                Color = UIColors.VernStat.SpiritPositive,
                AnchorTop = 0.1f,
                AnchorBottom = 0.9f,
                AnchorLeft = 0.5f,
                AnchorRight = 0.5f
            };
            _barContainer.AddChild(_positiveBar);

            // Center line marker
            _centerLine = new ColorRect
            {
                Color = UIColors.TEXT_SECONDARY,
                AnchorTop = 0f,
                AnchorBottom = 1f,
                AnchorLeft = 0.5f,
                AnchorRight = 0.5f,
                OffsetLeft = -1,
                OffsetRight = 1
            };
            _barContainer.AddChild(_centerLine);

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

            // Initial update if stat already set
            if (_stat != null)
            {
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Bind this bar to a centered stat (-100 to +100). Subscribes to value changes for real-time updates.
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
                _displayValue = _stat?.Value ?? 0f;
                UpdateDisplay();
            }
        }

        private void OnStatValueChanged(float oldValue, float newValue)
        {
            AnimateToValue(newValue);
            UpdateValueLabel();
        }

        private void UpdateDisplay()
        {
            if (_stat == null) return;

            _nameLabel.Text = _stat.Name.ToUpper();
            _displayValue = _stat.Value;

            UpdateBarDisplay();
            UpdateValueLabel();
        }

        private void UpdateBarDisplay()
        {
            if (_stat == null || _barContainer == null) return;

            // Calculate bar positions based on value
            // Range is -100 to +100, center is 0
            float minValue = _stat.MinValue; // -100
            float maxValue = _stat.MaxValue; // +100
            float range = maxValue - minValue; // 200

            // Normalize to 0-1 range where 0.5 is center (value of 0)
            float normalized = (_displayValue - minValue) / range;

            if (_displayValue >= 0)
            {
                // Positive: show green bar from center to right
                _negativeBar.AnchorLeft = 0.5f;
                _negativeBar.AnchorRight = 0.5f;

                _positiveBar.AnchorLeft = 0.5f;
                _positiveBar.AnchorRight = normalized;
            }
            else
            {
                // Negative: show red bar from center to left
                _positiveBar.AnchorLeft = 0.5f;
                _positiveBar.AnchorRight = 0.5f;

                _negativeBar.AnchorLeft = normalized;
                _negativeBar.AnchorRight = 0.5f;
            }

            // Update colors based on value (using full -100 to +100 range)
            Color valueColor = UIColors.GetCenteredStatColor(_displayValue);
            _valueLabel.AddThemeColorOverride("font_color", valueColor);
        }

        private void UpdateValueLabel()
        {
            if (_stat == null) return;

            int value = (int)_stat.Value;
            string sign = value >= 0 ? "+" : "";
            _valueLabel.Text = $"{sign}{value}";
        }

        private void AnimateToValue(float targetValue)
        {
            // Cancel any existing tween
            _currentTween?.Kill();

            // Create new tween for smooth animation
            _currentTween = CreateTween();
            _currentTween.TweenMethod(
                Callable.From<float>(SetDisplayValue),
                _displayValue,
                targetValue,
                ANIMATION_DURATION
            ).SetEase(Tween.EaseType.Out)
             .SetTrans(Tween.TransitionType.Quad);
        }

        private void SetDisplayValue(float value)
        {
            _displayValue = value;
            UpdateBarDisplay();
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
