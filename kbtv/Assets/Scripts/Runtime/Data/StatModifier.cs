using System;
using UnityEngine;

namespace KBTV.Data
{
    /// <summary>
    /// Defines which stat to target with a modifier.
    /// </summary>
    public enum StatType
    {
        Mood,
        Energy,
        Hunger,
        Thirst,
        Patience,
        Susceptibility,
        Belief
    }

    /// <summary>
    /// A single stat modification entry.
    /// </summary>
    [Serializable]
    public struct StatModification
    {
        public StatType StatType;
        public float Amount;

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
    [CreateAssetMenu(fileName = "NewStatModifier", menuName = "KBTV/Stat Modifier")]
    public class StatModifier : ScriptableObject
    {
        [Header("Info")]
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea] private string _description;

        [Header("Effects")]
        [SerializeField] private StatModification[] _modifications;

        public string DisplayName => _displayName;
        public string Description => _description;
        public StatModification[] Modifications => _modifications;

        /// <summary>
        /// Apply all modifications to the given VernStats.
        /// </summary>
        public void Apply(VernStats stats)
        {
            if (stats == null)
            {
                Debug.LogError($"StatModifier '{_displayName}': Cannot apply to null VernStats");
                return;
            }

            foreach (var mod in _modifications)
            {
                Stat targetStat = GetStat(stats, mod.StatType);
                if (targetStat != null)
                {
                    targetStat.Modify(mod.Amount);
                    Debug.Log($"Applied {_displayName}: {mod.StatType} {(mod.Amount >= 0 ? "+" : "")}{mod.Amount}");
                }
            }
        }

        private Stat GetStat(VernStats stats, StatType type)
        {
            return type switch
            {
                StatType.Mood => stats.Mood,
                StatType.Energy => stats.Energy,
                StatType.Hunger => stats.Hunger,
                StatType.Thirst => stats.Thirst,
                StatType.Patience => stats.Patience,
                StatType.Susceptibility => stats.Susceptibility,
                StatType.Belief => stats.Belief,
                _ => null
            };
        }
    }
}
