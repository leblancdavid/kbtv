using System;
using Godot;
using KBTV.Core;

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
    /// Examples: Coffee (+Physical, Caffeine → 100), Bad Caller (-Emotional, -Mental)
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
                Log.Error($"StatModifier '{_displayName}': Cannot apply to null VernStats");
                return;
            }

            foreach (var mod in _modifications)
            {
                Stat? targetStat = GetStat(stats, mod.StatType);
                if (targetStat != null)
                {
                    targetStat.Modify(mod.Amount);
                }
            }
        }

        private Stat? GetStat(VernStats stats, StatType type)
        {
            return type switch
            {
                // Dependencies
                StatType.Caffeine => stats.Caffeine,
                StatType.Nicotine => stats.Nicotine,

                // Core Stats
                StatType.Physical => stats.Physical,
                StatType.Emotional => stats.Emotional,
                StatType.Mental => stats.Mental,

                _ => null
            };
        }
    }
}
