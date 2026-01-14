using System;
using Godot;

namespace KBTV.Data
{
    /// <summary>
    /// A single stat modification entry.
    /// </summary>
    public partial class StatModification : Resource
    {
        [Export] public StatType StatType;
        [Export] public float Amount;

        public StatModification() {}

        public StatModification(StatType type, float amount)
        {
            StatType = type;
            Amount = amount;
        }
    }

    /// <summary>
    /// Defines an item or event that can modify Vern's stats.
    /// Examples: Coffee (+Energy, -Thirst), Bad Caller (-Belief, -Mood)
    /// </summary>
    public partial class StatModifier : Resource
    {
        [Export] private string _displayName;
        [Export(PropertyHint.MultilineText)] private string _description;
        [Export] private Godot.Collections.Array<StatModification> _modifications = new Godot.Collections.Array<StatModification>();

        public string DisplayName => _displayName;
        public string Description => _description;
        public Godot.Collections.Array<StatModification> Modifications => _modifications;

        /// <summary>
        /// Apply all modifications to the given VernStats.
        /// </summary>
        public void Apply(VernStats stats)
        {
            if (stats == null)
            {
                GD.PrintErr($"StatModifier '{_displayName}': Cannot apply to null VernStats");
                return;
            }

            foreach (var mod in _modifications)
            {
                Stat targetStat = GetStat(stats, mod.StatType);
                if (targetStat != null)
                {
                    targetStat.Modify(mod.Amount);
                }
            }
        }

        private Stat GetStat(VernStats stats, StatType type)
        {
            return type switch
            {
                // Dependencies
                StatType.Caffeine => stats.Caffeine,
                StatType.Nicotine => stats.Nicotine,

                // Physical
                StatType.Energy => stats.Energy,
                StatType.Satiety => stats.Satiety,

                // Emotional
                StatType.Spirit => stats.Spirit,

                // Cognitive
                StatType.Alertness => stats.Alertness,
                StatType.Discernment => stats.Discernment,
                StatType.Focus => stats.Focus,
                StatType.Patience => stats.Patience,

                // Long-Term
                StatType.Skepticism => stats.Skepticism,

                _ => null
            };
        }
    }
}