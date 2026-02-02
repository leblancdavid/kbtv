using System.Collections.Generic;
using System.Linq;
using Godot;
using KBTV.Callers;
using KBTV.Data;
using KBTV.Screening;
using KBTV.UI.Themes;

namespace KBTV.UI.Components
{
    /// <summary>
    /// UI component that displays an aggregated summary of all stat effects from revealed properties.
    /// Shows the total predicted impact on Vern if this caller goes on-air.
    /// Updates dynamically as more properties are revealed during screening.
    /// </summary>
    public partial class StatSummaryPanel : PanelContainer
    {
        // Child nodes
        private Label _titleLabel = null!;
        private HBoxContainer _statsContainer = null!;
        private Label _noDataLabel = null!;

        // State
        private ScreenableProperty[]? _properties;
        private Dictionary<StatType, float> _lastDisplayedTotals = new();

        public override void _Ready()
        {
            // Set up panel styling
            var panelStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.1f, 0.1f, 0.12f, 0.9f),
                BorderColor = new Color(0.3f, 0.5f, 0.3f, 0.8f),
                BorderWidthLeft = 1,
                BorderWidthRight = 1,
                BorderWidthTop = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 4,
                CornerRadiusTopRight = 4,
                CornerRadiusBottomLeft = 4,
                CornerRadiusBottomRight = 4,
                ContentMarginLeft = 8,
                ContentMarginRight = 8,
                ContentMarginTop = 4,
                ContentMarginBottom = 4
            };
            AddThemeStyleboxOverride("panel", panelStyle);

            // Create inner VBox layout
            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 4);
            AddChild(vbox);

            // Title row
            _titleLabel = new Label
            {
                Text = "Predicted Impact on Vern:",
                HorizontalAlignment = HorizontalAlignment.Left
            };
            _titleLabel.AddThemeColorOverride("font_color", UIColors.TEXT_SECONDARY);
            _titleLabel.AddThemeFontSizeOverride("font_size", 12);
            vbox.AddChild(_titleLabel);

            // Stats container (horizontal)
            _statsContainer = new HBoxContainer();
            _statsContainer.AddThemeConstantOverride("separation", 12);
            vbox.AddChild(_statsContainer);

            // No data label (shown when nothing is revealed)
            _noDataLabel = new Label
            {
                Text = "Screening...",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _noDataLabel.AddThemeColorOverride("font_color", UIColors.Scramble.Text);
            _statsContainer.AddChild(_noDataLabel);
        }

        /// <summary>
        /// Set the properties to aggregate stats from.
        /// </summary>
        public void SetProperties(ScreenableProperty[]? properties)
        {
            _properties = properties;
            _lastDisplayedTotals.Clear();
            UpdateDisplay();
        }

        /// <summary>
        /// Update the display each frame. Call from parent's _Process.
        /// </summary>
        public void UpdateDisplay()
        {
            if (_properties == null || _properties.Length == 0)
            {
                ShowNoData("No caller data");
                return;
            }

            // Calculate totals from all revealed properties
            var totals = CalculateRevealedStatTotals();

            // Check if anything changed
            if (TotalsMatch(totals, _lastDisplayedTotals))
            {
                return; // No update needed
            }

            // Update the display
            _lastDisplayedTotals = new Dictionary<StatType, float>(totals);
            RebuildStatLabels(totals);
        }

        /// <summary>
        /// Calculate total stat effects from all revealed properties.
        /// </summary>
        private Dictionary<StatType, float> CalculateRevealedStatTotals()
        {
            var totals = new Dictionary<StatType, float>();

            foreach (var property in _properties!)
            {
                if (!property.IsRevealed) continue;

                foreach (var effect in property.StatEffects)
                {
                    if (!totals.ContainsKey(effect.StatType))
                    {
                        totals[effect.StatType] = 0f;
                    }
                    totals[effect.StatType] += effect.Amount;
                }
            }

            return totals;
        }

        /// <summary>
        /// Check if two totals dictionaries are equivalent.
        /// </summary>
        private bool TotalsMatch(Dictionary<StatType, float> a, Dictionary<StatType, float> b)
        {
            if (a.Count != b.Count) return false;

            foreach (var (key, value) in a)
            {
                if (!b.TryGetValue(key, out var bValue) || !Mathf.IsEqualApprox(value, bValue))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Rebuild the stat labels to show current totals.
        /// </summary>
        private void RebuildStatLabels(Dictionary<StatType, float> totals)
        {
            // Clear existing labels
            foreach (var child in _statsContainer.GetChildren())
            {
                child.QueueFree();
            }

            if (totals.Count == 0)
            {
                ShowNoData("Screening...");
                return;
            }

            // Sort by stat type for consistent display
            var sortedStats = totals.OrderBy(kv => kv.Key).ToList();

            foreach (var (statType, amount) in sortedStats)
            {
                // Skip zero effects
                if (Mathf.IsZeroApprox(amount)) continue;

                var label = CreateStatLabel(statType, amount);
                _statsContainer.AddChild(label);
            }

            // If all effects are zero, show neutral message
            if (_statsContainer.GetChildCount() == 0)
            {
                ShowNoData("Neutral impact");
            }
        }

        /// <summary>
        /// Create a label for a single stat effect.
        /// </summary>
        private Label CreateStatLabel(StatType statType, float amount)
        {
            var code = GetStatCode(statType);
            var fullName = GetStatFullName(statType);
            var arrow = amount >= 0 ? "↑" : "↓";
            var absAmount = Mathf.Abs(amount);

            // Format: "Patience +5" or "Spirit -3"
            var signText = amount >= 0 ? "+" : "";
            var text = $"{fullName}: {signText}{amount:F0}";

            var label = new Label { Text = text };

            // Color based on positive/negative
            var color = amount >= 0 ? UIColors.StatEffect.Positive : UIColors.StatEffect.Negative;
            label.AddThemeColorOverride("font_color", color);

            // Tooltip with full stat name
            label.TooltipText = $"{fullName}: {signText}{amount:F1}";

            return label;
        }

        /// <summary>
        /// Show the no-data message.
        /// </summary>
        private void ShowNoData(string message)
        {
            foreach (var child in _statsContainer.GetChildren())
            {
                child.QueueFree();
            }

            _noDataLabel = new Label
            {
                Text = message,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _noDataLabel.AddThemeColorOverride("font_color", UIColors.Scramble.Text);
            _statsContainer.AddChild(_noDataLabel);
        }

        /// <summary>
        /// Get short code for a stat type.
        /// </summary>
        private static string GetStatCode(StatType statType)
        {
            return statType switch
            {
                StatType.Patience => "P",
                StatType.Spirit => "S",
                StatType.Energy => "E",
                StatType.Focus => "F",
                StatType.Discernment => "D",
                StatType.Skepticism => "Sk",
                StatType.Alertness => "A",
                StatType.Caffeine => "Ca",
                StatType.Nicotine => "N",
                StatType.Satiety => "Sa",
                _ => "?"
            };
        }

        /// <summary>
        /// Get full name for a stat type.
        /// </summary>
        private static string GetStatFullName(StatType statType)
        {
            return statType switch
            {
                StatType.Patience => "Patience",
                StatType.Spirit => "Spirit",
                StatType.Energy => "Energy",
                StatType.Focus => "Focus",
                StatType.Discernment => "Discernment",
                StatType.Skepticism => "Skepticism",
                StatType.Alertness => "Alertness",
                StatType.Caffeine => "Caffeine",
                StatType.Nicotine => "Nicotine",
                StatType.Satiety => "Satiety",
                _ => "Unknown"
            };
        }
    }
}
