#nullable enable

using Godot;
using KBTV.Data;
using KBTV.UI.Themes;

namespace KBTV.UI.Components
{
    /// <summary>
    /// Right column panel showing Vern's current status effects.
    /// Displays:
    /// - Decay rates with modifiers
    /// - Withdrawal effects (when dependencies depleted)
    /// - Stat interactions (when stats are critical)
    /// - Warnings (only visible when active)
    /// 
    /// Updates in real-time via _Process().
    /// </summary>
    public partial class VernStatusPanel : VBoxContainer
    {
        private VernStats? _vernStats;

        // Section components
        private StatusSection _decayRatesSection = null!;
        private StatusSection _withdrawalSection = null!;
        private StatusSection _interactionsSection = null!;
        private StatusSection _warningsSection = null!;

        public override void _Ready()
        {
            // Set up container spacing
            AddThemeConstantOverride("separation", 16);

            // Create section header
            var headerLabel = new Label
            {
                Text = "─── STATUS ───────────────────────────",
                HorizontalAlignment = HorizontalAlignment.Left
            };
            headerLabel.AddThemeColorOverride("font_color", UIColors.VernStat.CategoryHeader);

            var monoFont = new SystemFont();
            monoFont.FontNames = new string[] { "Consolas", "Courier New", "Liberation Mono", "monospace" };
            headerLabel.AddThemeFontOverride("font", monoFont);
            AddChild(headerLabel);

            // Create sections
            _decayRatesSection = new StatusSection("DECAY RATES");
            AddChild(_decayRatesSection);

            _withdrawalSection = new StatusSection("WITHDRAWAL");
            AddChild(_withdrawalSection);

            _interactionsSection = new StatusSection("STAT INTERACTIONS");
            AddChild(_interactionsSection);

            _warningsSection = new StatusSection("", hideWhenEmpty: true);
            AddChild(_warningsSection);

            // Initial update if stats already set
            if (_vernStats != null)
            {
                UpdateAllSections();
            }
        }

        /// <summary>
        /// Bind this panel to VernStats for real-time updates.
        /// </summary>
        public void SetVernStats(VernStats stats)
        {
            _vernStats = stats;

            if (_decayRatesSection != null)
            {
                UpdateAllSections();
            }
        }

        public override void _Process(double delta)
        {
            // Real-time updates
            if (_vernStats != null && _decayRatesSection != null)
            {
                UpdateAllSections();
            }
        }

        private void UpdateAllSections()
        {
            if (_vernStats == null) return;

            UpdateDecayRates();
            UpdateWithdrawal();
            UpdateStatInteractions();
            UpdateWarnings();
        }

        private void UpdateDecayRates()
        {
            _decayRatesSection.ClearItems();

            // Caffeine decay rate with modifier
            float caffeineModifier = _vernStats!.GetCaffeineDecayModifier();
            float caffeineEffectiveRate = _vernStats.CaffeineDecayRate * caffeineModifier;
            Color caffeineColor = GetModifierColor(caffeineModifier);
            _decayRatesSection.AddTreeItem(
                $"Caffeine: -{caffeineEffectiveRate:F2}/min ({caffeineModifier:F2}x)",
                caffeineColor,
                isLast: false
            );

            // Nicotine decay rate with modifier
            float nicotineModifier = _vernStats.GetNicotineDecayModifier();
            float nicotineEffectiveRate = _vernStats.NicotineDecayRate * nicotineModifier;
            Color nicotineColor = GetModifierColor(nicotineModifier);
            _decayRatesSection.AddTreeItem(
                $"Nicotine: -{nicotineEffectiveRate:F2}/min ({nicotineModifier:F2}x)",
                nicotineColor,
                isLast: true
            );
        }

        private void UpdateWithdrawal()
        {
            _withdrawalSection.ClearItems();

            bool hasCaffeineWithdrawal = _vernStats!.IsCaffeineDepleted;
            bool hasNicotineWithdrawal = _vernStats.IsNicotineDepleted;

            if (!hasCaffeineWithdrawal && !hasNicotineWithdrawal)
            {
                _withdrawalSection.AddTreeItem("None (dependencies OK)", UIColors.Warning.Good, isLast: true);
                return;
            }

            // Show withdrawal effects
            if (hasCaffeineWithdrawal)
            {
                _withdrawalSection.AddTreeItem(
                    $"Physical: -{_vernStats.PhysicalDecayRate:F1}/min",
                    UIColors.Warning.Critical,
                    isLast: false
                );
            }

            if (hasNicotineWithdrawal)
            {
                _withdrawalSection.AddTreeItem(
                    $"Emotional: -{_vernStats.EmotionalDecayRate:F1}/min",
                    UIColors.Warning.Critical,
                    isLast: false
                );
            }

            // Mental decay from either/both
            float mentalDecayRate = _vernStats.MentalDecayRate;
            if (hasCaffeineWithdrawal && hasNicotineWithdrawal)
            {
                mentalDecayRate *= 2; // Both contribute
            }
            _withdrawalSection.AddTreeItem(
                $"Mental: -{mentalDecayRate:F1}/min",
                UIColors.Warning.Critical,
                isLast: true
            );
        }

        private void UpdateStatInteractions()
        {
            _interactionsSection.ClearItems();

            bool hasInteractions = false;
            bool isPhysicalCritical = _vernStats!.IsPhysicalCritical;
            bool isEmotionalCritical = _vernStats.IsEmotionalCritical;
            bool isMentalCritical = _vernStats.IsMentalCritical;

            if (isPhysicalCritical)
            {
                _interactionsSection.AddTreeItem(
                    "Physical < -25: Mental decay +50%",
                    UIColors.Status.ModifierDebuff,
                    isLast: !isEmotionalCritical && !isMentalCritical
                );
                hasInteractions = true;
            }

            if (isEmotionalCritical)
            {
                _interactionsSection.AddTreeItem(
                    "Emotional < -25: Physical decay +50%",
                    UIColors.Status.ModifierDebuff,
                    isLast: !isMentalCritical
                );
                hasInteractions = true;
            }

            if (isMentalCritical)
            {
                _interactionsSection.AddTreeItem(
                    "Mental < -25: Dependencies decay +25%",
                    UIColors.Status.ModifierDebuff,
                    isLast: true
                );
                hasInteractions = true;
            }

            if (!hasInteractions)
            {
                _interactionsSection.AddTreeItem("None active", UIColors.Status.ModifierNeutral, isLast: true);
            }
        }

        private void UpdateWarnings()
        {
            _warningsSection.ClearItems();

            // Check all warning conditions
            bool caffeineDepleted = _vernStats!.IsCaffeineDepleted;
            bool nicotineDepleted = _vernStats.IsNicotineDepleted;
            bool physicalExhausted = _vernStats.Physical.Value < -50f;
            bool emotionalDemoralized = _vernStats.Emotional.Value < -50f;
            bool mentalUnfocused = _vernStats.Mental.Value < -50f;
            bool vibeLow = _vernStats.CurrentVIBE < -25f;

            // Add warnings (only show section if there are warnings)
            if (caffeineDepleted)
            {
                _warningsSection.AddItem("\u26a0 CAFFEINE CRASH", UIColors.Warning.Critical);
            }

            if (nicotineDepleted)
            {
                _warningsSection.AddItem("\u26a0 NICOTINE WITHDRAWAL", UIColors.Warning.Critical);
            }

            if (physicalExhausted)
            {
                _warningsSection.AddItem("\u26a0 EXHAUSTED", UIColors.Warning.Critical);
            }

            if (emotionalDemoralized)
            {
                _warningsSection.AddItem("\u26a0 DEMORALIZED", UIColors.Warning.Critical);
            }

            if (mentalUnfocused)
            {
                _warningsSection.AddItem("\u26a0 UNFOCUSED", UIColors.Warning.Critical);
            }

            if (vibeLow)
            {
                _warningsSection.AddItem("\u26a0 LISTENERS LEAVING", UIColors.Warning.Caution);
            }
        }

        /// <summary>
        /// Get color for decay modifier based on value.
        /// Lower modifiers are good (slower decay), higher are bad.
        /// </summary>
        private static Color GetModifierColor(float modifier)
        {
            if (modifier <= 0.75f)
                return UIColors.Status.ModifierBuff;     // Green - slow decay
            if (modifier <= 1.0f)
                return UIColors.Status.ModifierNeutral;  // Gray - normal
            return UIColors.Status.ModifierDebuff;       // Orange - fast decay
        }
    }
}
