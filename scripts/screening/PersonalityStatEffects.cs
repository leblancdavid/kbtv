using System.Collections.Generic;
using KBTV.Data;

namespace KBTV.Screening
{
    /// <summary>
    /// Static utility class that maps caller personalities to unique stat effects.
    /// Each of the 36 personalities has a distinct combination of Physical, Emotional, and Mental effects.
    /// 
    /// Personality Categories:
    /// - Positive (12): Total effect +5 to +6, generally boost all stats
    /// - Negative (12): Total effect -6 to -8, generally drain all stats
    /// - Neutral (12): Total effect -2 to +4, mixed effects with trade-offs
    /// </summary>
    public static class PersonalityStatEffects
    {
        /// <summary>
        /// Dictionary mapping personality names to their stat effects.
        /// </summary>
        private static readonly Dictionary<string, List<StatModification>> PersonalityEffects = new()
        {
            // ═══════════════════════════════════════════════════════════════════════════════
            // POSITIVE PERSONALITIES (12) - Generally boost stats, total +5 to +6
            // ═══════════════════════════════════════════════════════════════════════════════
            
            ["Matter-of-fact reporter"] = new()
            {
                new StatModification(StatType.Physical, 2f),
                new StatModification(StatType.Emotional, 1f),
                new StatModification(StatType.Mental, 3f)
            },
            
            ["Academic researcher"] = new()
            {
                new StatModification(StatType.Emotional, 2f),
                new StatModification(StatType.Mental, 3f)
            },
            
            ["Local history buff"] = new()
            {
                new StatModification(StatType.Physical, 2f),
                new StatModification(StatType.Emotional, 3f),
                new StatModification(StatType.Mental, 1f)
            },
            
            ["Frequent listener"] = new()
            {
                new StatModification(StatType.Physical, 2f),
                new StatModification(StatType.Emotional, 3f)
            },
            
            ["Genuinely frightened"] = new()
            {
                new StatModification(StatType.Emotional, 3f),
                new StatModification(StatType.Mental, 2f)
            },
            
            ["True believer"] = new()
            {
                new StatModification(StatType.Physical, 1f),
                new StatModification(StatType.Emotional, 3f),
                new StatModification(StatType.Mental, 2f)
            },
            
            ["Retired professional"] = new()
            {
                new StatModification(StatType.Physical, 2f),
                new StatModification(StatType.Emotional, 1f),
                new StatModification(StatType.Mental, 3f)
            },
            
            ["Careful observer"] = new()
            {
                new StatModification(StatType.Physical, 2f),
                new StatModification(StatType.Emotional, 1f),
                new StatModification(StatType.Mental, 3f)
            },
            
            ["Soft-spoken witness"] = new()
            {
                new StatModification(StatType.Physical, 2f),
                new StatModification(StatType.Emotional, 3f)
            },
            
            ["Articulate storyteller"] = new()
            {
                new StatModification(StatType.Physical, 2f),
                new StatModification(StatType.Emotional, 3f),
                new StatModification(StatType.Mental, 1f)
            },
            
            ["Patient explainer"] = new()
            {
                new StatModification(StatType.Physical, 3f),
                new StatModification(StatType.Emotional, 1f),
                new StatModification(StatType.Mental, 2f)
            },
            
            ["Earnest truth-seeker"] = new()
            {
                new StatModification(StatType.Emotional, 3f),
                new StatModification(StatType.Mental, 3f)
            },

            // ═══════════════════════════════════════════════════════════════════════════════
            // NEGATIVE PERSONALITIES (12) - Generally drain stats, total -6 to -8
            // ═══════════════════════════════════════════════════════════════════════════════
            
            ["Attention seeker"] = new()
            {
                new StatModification(StatType.Physical, -2f),
                new StatModification(StatType.Emotional, -3f),
                new StatModification(StatType.Mental, -1f)
            },
            
            ["Conspiracy theorist"] = new()
            {
                new StatModification(StatType.Physical, -1f),
                new StatModification(StatType.Emotional, -2f),
                new StatModification(StatType.Mental, -3f)
            },
            
            ["Rambling storyteller"] = new()
            {
                new StatModification(StatType.Physical, -3f),
                new StatModification(StatType.Emotional, -1f),
                new StatModification(StatType.Mental, -2f)
            },
            
            ["Joker type"] = new()
            {
                new StatModification(StatType.Physical, -1f),
                new StatModification(StatType.Emotional, -3f),
                new StatModification(StatType.Mental, -2f)
            },
            
            ["Monotone delivery"] = new()
            {
                new StatModification(StatType.Physical, -3f),
                new StatModification(StatType.Emotional, -2f),
                new StatModification(StatType.Mental, -1f)
            },
            
            ["Skeptical witness"] = new()
            {
                new StatModification(StatType.Physical, -1f),
                new StatModification(StatType.Emotional, -3f),
                new StatModification(StatType.Mental, -2f)
            },
            
            ["Know-it-all"] = new()
            {
                new StatModification(StatType.Physical, -1f),
                new StatModification(StatType.Emotional, -3f),
                new StatModification(StatType.Mental, -2f)
            },
            
            ["Chronic interrupter"] = new()
            {
                new StatModification(StatType.Physical, -2f),
                new StatModification(StatType.Emotional, -3f),
                new StatModification(StatType.Mental, -3f)
            },
            
            ["Drama queen"] = new()
            {
                new StatModification(StatType.Physical, -3f),
                new StatModification(StatType.Emotional, -3f)
            },
            
            ["Mumbling caller"] = new()
            {
                new StatModification(StatType.Physical, -2f),
                new StatModification(StatType.Emotional, -1f),
                new StatModification(StatType.Mental, -3f)
            },
            
            ["Easily distracted"] = new()
            {
                new StatModification(StatType.Physical, -2f),
                new StatModification(StatType.Emotional, -1f),
                new StatModification(StatType.Mental, -3f)
            },
            
            ["Defensive storyteller"] = new()
            {
                new StatModification(StatType.Physical, -1f),
                new StatModification(StatType.Emotional, -3f),
                new StatModification(StatType.Mental, -2f)
            },

            // ═══════════════════════════════════════════════════════════════════════════════
            // NEUTRAL PERSONALITIES (12) - Mixed effects, total -2 to +4
            // ═══════════════════════════════════════════════════════════════════════════════
            
            ["Nervous but sincere"] = new()
            {
                new StatModification(StatType.Physical, -1f),
                new StatModification(StatType.Emotional, 2f),
                new StatModification(StatType.Mental, 1f)
            },
            
            ["Overly enthusiastic"] = new()
            {
                new StatModification(StatType.Physical, 2f),
                new StatModification(StatType.Emotional, 2f),
                new StatModification(StatType.Mental, -2f)
            },
            
            ["First-time caller"] = new()
            {
                new StatModification(StatType.Physical, -2f),
                new StatModification(StatType.Emotional, 2f)
            },
            
            ["Desperate for answers"] = new()
            {
                new StatModification(StatType.Physical, -2f),
                new StatModification(StatType.Emotional, 2f),
                new StatModification(StatType.Mental, 1f)
            },
            
            ["Reluctant witness"] = new()
            {
                new StatModification(StatType.Physical, 2f),
                new StatModification(StatType.Emotional, -1f),
                new StatModification(StatType.Mental, 2f)
            },
            
            ["Excitable narrator"] = new()
            {
                new StatModification(StatType.Physical, 2f),
                new StatModification(StatType.Emotional, 2f),
                new StatModification(StatType.Mental, -2f)
            },
            
            ["Quiet observer"] = new()
            {
                new StatModification(StatType.Physical, 2f),
                new StatModification(StatType.Mental, 2f)
            },
            
            ["Chatty neighbor"] = new()
            {
                new StatModification(StatType.Physical, -2f),
                new StatModification(StatType.Emotional, 2f),
                new StatModification(StatType.Mental, -2f)
            },
            
            ["Late-night insomniac"] = new()
            {
                new StatModification(StatType.Physical, -2f),
                new StatModification(StatType.Mental, 2f)
            },
            
            ["Curious skeptic"] = new()
            {
                new StatModification(StatType.Emotional, -2f),
                new StatModification(StatType.Mental, 3f)
            },
            
            ["Nostalgic elder"] = new()
            {
                new StatModification(StatType.Physical, -2f),
                new StatModification(StatType.Emotional, 3f)
            },
            
            ["Breathless reporter"] = new()
            {
                new StatModification(StatType.Physical, 2f),
                new StatModification(StatType.Emotional, 2f),
                new StatModification(StatType.Mental, -2f)
            }
        };

        /// <summary>
        /// Get stat effects for a given personality name.
        /// </summary>
        /// <param name="personalityName">The personality name (e.g., "Matter-of-fact reporter").</param>
        /// <returns>List of stat modifications, empty if personality not found.</returns>
        public static List<StatModification> GetEffects(string personalityName)
        {
            if (string.IsNullOrEmpty(personalityName))
            {
                return new List<StatModification>();
            }

            if (PersonalityEffects.TryGetValue(personalityName, out var effects))
            {
                // Return a copy to prevent external modification
                return new List<StatModification>(effects);
            }

            return new List<StatModification>();
        }

        /// <summary>
        /// Check if a personality has defined effects.
        /// </summary>
        /// <param name="personalityName">The personality name to check.</param>
        /// <returns>True if the personality has defined effects.</returns>
        public static bool HasEffects(string personalityName)
        {
            return !string.IsNullOrEmpty(personalityName) && PersonalityEffects.ContainsKey(personalityName);
        }

        /// <summary>
        /// Get all personality names with defined effects.
        /// </summary>
        /// <returns>Collection of personality names.</returns>
        public static IEnumerable<string> GetAllPersonalityNames()
        {
            return PersonalityEffects.Keys;
        }
    }
}
