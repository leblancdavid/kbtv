#nullable enable

using Godot;
using KBTV.Core;
using KBTV.Data;
using KBTV.UI.Components;
using KBTV.UI.Themes;

namespace KBTV.UI
{
    /// <summary>
    /// Main Vern stat view displaying all of Vern's stats in a two-column layout.
    /// </summary>
    public partial class VernStatView : Control, IDependent
    {
        private VernStats? _vernStats;
        private ScrollContainer? _scrollContainer;
        private VBoxContainer? _contentContainer;

        private VBoxContainer? _statsColumn;
        private VibeDisplay? _vibeDisplay;
        private StatBar? _caffeineBar;
        private StatBar? _nicotineBar;
        private CenteredStatBar? _physicalBar;
        private CenteredStatBar? _emotionalBar;
        private CenteredStatBar? _mentalBar;

        public override void _Notification(int what) => this.Notify(what);

        public override void _Ready()
        {
            // UI will be built after OnResolved when we have access to VernStats
        }

        public void OnResolved()
        {
            var gameStateManager = DependencyInjection.Get<IGameStateManager>(this);
            if (gameStateManager == null)
            {
                Log.Error("VernStatView: GameStateManager is null - cannot get VernStats!");
                return;
            }

            _vernStats = gameStateManager.VernStats;
            if (_vernStats == null)
            {
                Log.Error("VernStatView: VernStats is null!");
                return;
            }

            BuildUI();
        }

        private void BuildUI()
        {
            // Parent (_vernRightPanel) is already anchored to right half of screen.
            // No extra right-half container needed here — just add padding + content directly.
            var paddingContainer = new MarginContainer();
            UITheme.ApplyMargins(paddingContainer, 16, 56, UITheme.MARGIN_SMALL, UITheme.MARGIN_SMALL);
            paddingContainer.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(paddingContainer);

            _statsColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            _statsColumn.AddThemeConstantOverride("separation", UITheme.SPACING_MEDIUM);
            paddingContainer.AddChild(_statsColumn);

            CreateVibeDisplay(_statsColumn);
            CreateStatsColumn(_statsColumn);
        }

        private void CreateVibeDisplay(VBoxContainer parent)
        {
            if (_vernStats == null) return;

            _vibeDisplay = new VibeDisplay();
            parent.AddChild(_vibeDisplay);
            _vibeDisplay.SetVernStats(_vernStats);
        }

        private void CreateStatsColumn(VBoxContainer parent)
        {
            if (_vernStats == null) return;

            _caffeineBar = new StatBar();
            parent.AddChild(_caffeineBar);
            _caffeineBar.SetStat(_vernStats.Caffeine);

            _nicotineBar = new StatBar();
            parent.AddChild(_nicotineBar);
            _nicotineBar.SetStat(_vernStats.Nicotine);

            _physicalBar = new CenteredStatBar();
            parent.AddChild(_physicalBar);
            _physicalBar.SetStat(_vernStats.Physical);

            _emotionalBar = new CenteredStatBar();
            parent.AddChild(_emotionalBar);
            _emotionalBar.SetStat(_vernStats.Emotional);

            _mentalBar = new CenteredStatBar();
            parent.AddChild(_mentalBar);
            _mentalBar.SetStat(_vernStats.Mental);
        }

        public override void _Process(double delta)
        {
            UpdateModifiers();
        }

        private void UpdateModifiers()
        {
            if (_vernStats == null)
            {
                return;
            }

            UpdateDependencyModifiers();
            UpdateCoreModifiers();
        }

        private void UpdateDependencyModifiers()
        {
            if (_caffeineBar == null || _nicotineBar == null)
            {
                return;
            }

            float dependencyCriticalMultiplier = _vernStats.IsMentalCritical
                ? _vernStats.LowMentalDependencyMultiplier
                : 1f;

            float caffeineModifier = _vernStats.GetCaffeineDecayModifier() * dependencyCriticalMultiplier;
            float nicotineModifier = _vernStats.GetNicotineDecayModifier() * dependencyCriticalMultiplier;

            UpdateDependencyModifier(_caffeineBar, _vernStats.CaffeineDecayRate, caffeineModifier);
            UpdateDependencyModifier(_nicotineBar, _vernStats.NicotineDecayRate, nicotineModifier);
        }

        private void UpdateCoreModifiers()
        {
            if (_physicalBar == null || _emotionalBar == null || _mentalBar == null)
            {
                return;
            }

            bool hasCaffeineWithdrawal = _vernStats.IsCaffeineDepleted;
            bool hasNicotineWithdrawal = _vernStats.IsNicotineDepleted;

            float physicalDecay = hasCaffeineWithdrawal ? _vernStats.PhysicalDecayRate : 0f;
            float emotionalDecay = hasNicotineWithdrawal ? _vernStats.EmotionalDecayRate : 0f;

            float mentalDecay = 0f;
            if (hasCaffeineWithdrawal)
            {
                mentalDecay += _vernStats.MentalDecayRate;
            }
            if (hasNicotineWithdrawal)
            {
                mentalDecay += _vernStats.MentalDecayRate;
            }

            float physicalMultiplier = _vernStats.IsEmotionalCritical ? _vernStats.LowStatDecayMultiplier : 1f;
            float emotionalMultiplier = _vernStats.IsPhysicalCritical ? _vernStats.LowStatDecayMultiplier : 1f;
            float mentalMultiplier = 1f;

            if (_vernStats.IsPhysicalCritical && mentalDecay > 0f)
            {
                mentalMultiplier = _vernStats.LowStatDecayMultiplier;
            }

            if (_vernStats.IsEmotionalCritical && mentalDecay > 0f)
            {
                mentalMultiplier = _vernStats.LowStatDecayMultiplier;
            }

            physicalDecay *= physicalMultiplier;
            emotionalDecay *= emotionalMultiplier;
            mentalDecay *= mentalMultiplier;

            ApplyCoreModifier(_physicalBar, physicalDecay, physicalMultiplier);
            ApplyCoreModifier(_emotionalBar, emotionalDecay, emotionalMultiplier);
            ApplyCoreModifier(_mentalBar, mentalDecay, mentalMultiplier);
        }

        private void ApplyCoreModifier(CenteredStatBar bar, float decayRate, float multiplier)
        {
            if (bar == null)
            {
                return;
            }

            string? decayText = BuildDecayRateText(decayRate);
            string? percentText = BuildPercentModifierText(multiplier);

            if (string.IsNullOrWhiteSpace(decayText) && string.IsNullOrWhiteSpace(percentText))
            {
                bar.SetModifier(null, UIColors.Status.ModifierNeutral);
                return;
            }

            var color = GetModifierColor(multiplier);
            if (string.IsNullOrWhiteSpace(decayText))
            {
                bar.SetModifier(percentText, color);
            }
            else if (string.IsNullOrWhiteSpace(percentText))
            {
                bar.SetModifier(decayText, color);
            }
            else
            {
                bar.SetModifier($"{percentText} {decayText}", color);
            }
        }

        private void UpdateDependencyModifier(StatBar bar, float baseDecayRate, float modifier)
        {
            string? percentText = BuildPercentModifierText(modifier);
            if (percentText == null)
            {
                bar.SetModifier(null, UIColors.Status.ModifierNeutral);
                return;
            }

            var effectiveRate = baseDecayRate * modifier;
            var decayText = BuildDecayRateText(effectiveRate);
            var color = GetModifierColor(modifier);

            if (string.IsNullOrWhiteSpace(decayText))
            {
                bar.SetModifier(percentText, color);
                return;
            }

            bar.SetModifier($"{percentText} {decayText}", color);
        }

        private static string? BuildPercentModifierText(float modifier)
        {
            if (Mathf.IsEqualApprox(modifier, 1f))
            {
                return null;
            }

            var percentDelta = (modifier - 1f) * 100f;
            var rounded = Mathf.RoundToInt(percentDelta);
            if (rounded == 0)
            {
                return null;
            }

            var sign = rounded > 0 ? "+" : "";
            return $"{sign}{rounded}%";
        }

        private static string? BuildDecayRateText(float decayRate)
        {
            if (decayRate <= 0f)
            {
                return null;
            }

            return $"-{decayRate:F1}/min";
        }

        private static Color GetModifierColor(float modifier)
        {
            if (modifier <= 0.75f)
            {
                return UIColors.Status.ModifierBuff;
            }
            if (modifier <= 1.0f)
            {
                return UIColors.Status.ModifierNeutral;
            }
            return UIColors.Status.ModifierDebuff;
        }
    }
}
