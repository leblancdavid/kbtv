#nullable enable

using Godot;
using KBTV.Data;
using KBTV.UI.Themes;

namespace KBTV.UI.Components
{
    /// <summary>
    /// Display component for VIBE score and current mood type.
    /// Shows centered bar (-100 to +100) with gradient coloring and mood text.
    /// Format: "VIBE  [░░░░░░░████████████░░░░]  +25   FOCUSED"
    /// </summary>
    public partial class VibeDisplay : HBoxContainer
    {
        private const float ANIMATION_DURATION = 0.5f;

        // Child nodes
        private Label _vibeLabel = null!;
        private Control _barContainer = null!;
        private ColorRect _negativeBar = null!;
        private ColorRect _positiveBar = null!;
        private ColorRect _centerLine = null!;
        private Label _valueLabel = null!;
        private Label _moodLabel = null!;

        // State
        private VernStats? _vernStats;
        private Tween? _currentTween;
        private float _displayValue = 0f;

        public override void _Ready()
        {
            // Create VIBE label
            _vibeLabel = new Label
            {
                Text = "VIBE",
                CustomMinimumSize = new Vector2(50, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            _vibeLabel.AddThemeColorOverride("font_color", UIColors.Property.Name);
            AddChild(_vibeLabel);

            // Create bar container (custom drawn)
            _barContainer = new Control
            {
                CustomMinimumSize = new Vector2(200, 24),
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
                Color = UIColors.VernStat.VibeNegative,
                AnchorTop = 0.1f,
                AnchorBottom = 0.9f,
                AnchorLeft = 0.5f,
                AnchorRight = 0.5f
            };
            _barContainer.AddChild(_negativeBar);

            // Positive bar (right side, green) - starts at center, grows right
            _positiveBar = new ColorRect
            {
                Color = UIColors.VernStat.VibePositive,
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
                CustomMinimumSize = new Vector2(50, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            _valueLabel.AddThemeColorOverride("font_color", UIColors.TEXT_PRIMARY);
            AddChild(_valueLabel);

            // Create mood label
            _moodLabel = new Label
            {
                CustomMinimumSize = new Vector2(100, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _moodLabel.AddThemeColorOverride("font_color", UIColors.TEXT_PRIMARY);
            AddChild(_moodLabel);

            // Apply monospace font to all labels
            var monoFont = new SystemFont();
            monoFont.FontNames = new string[] { "Consolas", "Courier New", "Liberation Mono", "monospace" };
            _vibeLabel.AddThemeFontOverride("font", monoFont);
            _valueLabel.AddThemeFontOverride("font", monoFont);
            _moodLabel.AddThemeFontOverride("font", monoFont);

            // Initial update if stats already set
            if (_vernStats != null)
            {
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Bind this display to VernStats. Subscribes to VIBE and mood changes.
        /// </summary>
        public void SetVernStats(VernStats stats)
        {
            // Unsubscribe from previous stats
            if (_vernStats != null)
            {
                _vernStats.VibeChanged -= OnVibeChanged;
                _vernStats.MoodTypeChanged -= OnMoodTypeChanged;
            }

            _vernStats = stats;

            // Subscribe to new stats
            if (_vernStats != null)
            {
                _vernStats.VibeChanged += OnVibeChanged;
                _vernStats.MoodTypeChanged += OnMoodTypeChanged;
            }

            // Update display if ready
            if (_vibeLabel != null)
            {
                _displayValue = _vernStats?.CalculateVIBE() ?? 0f;
                UpdateDisplay();
            }
        }

        private void OnVibeChanged(float newVibe)
        {
            AnimateToValue(newVibe);
            UpdateValueLabel(newVibe);
        }

        private void OnMoodTypeChanged(VernMoodType newMood)
        {
            UpdateMoodLabel(newMood);
        }

        private void UpdateDisplay()
        {
            if (_vernStats == null) return;

            _displayValue = _vernStats.CalculateVIBE();

            UpdateBarDisplay();
            UpdateValueLabel(_displayValue);
            UpdateMoodLabel(_vernStats.CurrentMoodType);
        }

        private void UpdateBarDisplay()
        {
            if (_barContainer == null) return;

            // VIBE range is -100 to +100, center is 0
            // Normalize to 0-1 range where 0.5 is center (value of 0)
            float normalized = (_displayValue + 100f) / 200f;

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

            // Update value label color based on VIBE
            Color valueColor = UIColors.GetVibeColor(_displayValue);
            _valueLabel.AddThemeColorOverride("font_color", valueColor);
        }

        private void UpdateValueLabel(float vibe)
        {
            int value = (int)vibe;
            string sign = value >= 0 ? "+" : "";
            _valueLabel.Text = $"{sign}{value}";

            // Update color
            Color valueColor = UIColors.GetVibeColor(vibe);
            _valueLabel.AddThemeColorOverride("font_color", valueColor);
        }

        private void UpdateMoodLabel(VernMoodType mood)
        {
            _moodLabel.Text = mood.ToString().ToUpper();

            // Color the mood based on positive/negative connotation
            Color moodColor = mood switch
            {
                VernMoodType.Energized => UIColors.VernStat.High,
                VernMoodType.Amused => UIColors.VernStat.High,
                VernMoodType.Focused => UIColors.VernStat.High,
                VernMoodType.Obsessive => UIColors.VernStat.High,
                VernMoodType.Manic => UIColors.VernStat.High,
                VernMoodType.Neutral => UIColors.TEXT_PRIMARY,
                VernMoodType.Gruff => UIColors.VernStat.Medium,
                VernMoodType.Frustrated => UIColors.VernStat.Medium,
                VernMoodType.Tired => UIColors.VernStat.Low,
                VernMoodType.Irritated => UIColors.VernStat.Low,
                VernMoodType.Exhausted => UIColors.VernStat.Low,
                VernMoodType.Depressed => UIColors.VernStat.Low,
                VernMoodType.Angry => UIColors.VernStat.Low,
                _ => UIColors.TEXT_PRIMARY
            };
            _moodLabel.AddThemeColorOverride("font_color", moodColor);
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
            if (_vernStats != null)
            {
                _vernStats.VibeChanged -= OnVibeChanged;
                _vernStats.MoodTypeChanged -= OnMoodTypeChanged;
            }

            _currentTween?.Kill();
        }
    }
}
