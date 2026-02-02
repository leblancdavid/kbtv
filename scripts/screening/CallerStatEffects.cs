using System.Collections.Generic;
using KBTV.Callers;
using KBTV.Data;

namespace KBTV.Screening
{
    /// <summary>
    /// Static utility class that calculates stat effects for caller properties.
    /// Both the property type AND value determine the effect on Vern's stats.
    /// These effects are previewed during screening and applied when the caller goes on-air.
    /// </summary>
    public static class CallerStatEffects
    {
        /// <summary>
        /// Calculate stat effects for a given property and value.
        /// </summary>
        /// <param name="propertyKey">The property identifier.</param>
        /// <param name="value">The property value.</param>
        /// <returns>List of stat modifications, empty if no effects.</returns>
        public static List<StatModification> GetStatEffects(string propertyKey, object value)
        {
            if (value == null)
            {
                return new List<StatModification>();
            }

            return propertyKey switch
            {
                "EmotionalState" => GetEmotionalStateEffects((CallerEmotionalState)value),
                "CurseRisk" => GetCurseRiskEffects((CallerCurseRisk)value),
                "Coherence" => GetCoherenceEffects((CallerCoherence)value),
                "Urgency" => GetUrgencyEffects((CallerUrgency)value),
                "BeliefLevel" => GetBeliefLevelEffects((CallerBeliefLevel)value),
                "Evidence" => GetEvidenceLevelEffects((CallerEvidenceLevel)value),
                "Legitimacy" => GetLegitimacyEffects((CallerLegitimacy)value),
                "AudioQuality" => GetPhoneQualityEffects((CallerPhoneQuality)value),
                // Properties with no stat effects
                "Summary" => new List<StatModification>(),
                "Topic" => new List<StatModification>(),
                "Personality" => new List<StatModification>(),
                _ => new List<StatModification>()
            };
        }

        /// <summary>
        /// Emotional state affects Vern's patience and spirit.
        /// Calm callers are easy to deal with, angry callers drain patience.
        /// </summary>
        private static List<StatModification> GetEmotionalStateEffects(CallerEmotionalState state)
        {
            return state switch
            {
                CallerEmotionalState.Calm => new List<StatModification>
                {
                    new StatModification(StatType.Patience, 3f)
                },
                CallerEmotionalState.Anxious => new List<StatModification>
                {
                    new StatModification(StatType.Patience, -2f),
                    new StatModification(StatType.Spirit, -1f)
                },
                CallerEmotionalState.Excited => new List<StatModification>
                {
                    new StatModification(StatType.Spirit, 3f),
                    new StatModification(StatType.Energy, 2f)
                },
                CallerEmotionalState.Scared => new List<StatModification>
                {
                    new StatModification(StatType.Patience, -3f),
                    new StatModification(StatType.Spirit, -2f)
                },
                CallerEmotionalState.Angry => new List<StatModification>
                {
                    new StatModification(StatType.Patience, -5f),
                    new StatModification(StatType.Spirit, -3f)
                },
                _ => new List<StatModification>()
            };
        }

        /// <summary>
        /// Curse risk affects Vern's patience.
        /// High curse risk means Vern has to be on edge, ready for the dump button.
        /// </summary>
        private static List<StatModification> GetCurseRiskEffects(CallerCurseRisk risk)
        {
            return risk switch
            {
                CallerCurseRisk.Low => new List<StatModification>(),
                CallerCurseRisk.Medium => new List<StatModification>
                {
                    new StatModification(StatType.Patience, -2f)
                },
                CallerCurseRisk.High => new List<StatModification>
                {
                    new StatModification(StatType.Patience, -4f),
                    new StatModification(StatType.Spirit, -2f)
                },
                _ => new List<StatModification>()
            };
        }

        /// <summary>
        /// Coherence affects Vern's patience and focus.
        /// Incoherent callers are frustrating and hard to follow.
        /// </summary>
        private static List<StatModification> GetCoherenceEffects(CallerCoherence coherence)
        {
            return coherence switch
            {
                CallerCoherence.Coherent => new List<StatModification>
                {
                    new StatModification(StatType.Patience, 2f),
                    new StatModification(StatType.Focus, 1f)
                },
                CallerCoherence.Questionable => new List<StatModification>
                {
                    new StatModification(StatType.Patience, -2f)
                },
                CallerCoherence.Incoherent => new List<StatModification>
                {
                    new StatModification(StatType.Patience, -4f),
                    new StatModification(StatType.Focus, -3f)
                },
                _ => new List<StatModification>()
            };
        }

        /// <summary>
        /// Urgency affects Vern's spirit and energy.
        /// Urgent calls are more engaging but critical urgency can be stressful.
        /// </summary>
        private static List<StatModification> GetUrgencyEffects(CallerUrgency urgency)
        {
            return urgency switch
            {
                CallerUrgency.Low => new List<StatModification>(),
                CallerUrgency.Medium => new List<StatModification>
                {
                    new StatModification(StatType.Spirit, 1f)
                },
                CallerUrgency.High => new List<StatModification>
                {
                    new StatModification(StatType.Spirit, 2f),
                    new StatModification(StatType.Energy, 1f)
                },
                CallerUrgency.Critical => new List<StatModification>
                {
                    new StatModification(StatType.Spirit, 3f),
                    new StatModification(StatType.Patience, -2f)
                },
                _ => new List<StatModification>()
            };
        }

        /// <summary>
        /// Belief level affects Vern's spirit, discernment, and skepticism.
        /// Curious callers are easy, zealots are exhausting.
        /// </summary>
        private static List<StatModification> GetBeliefLevelEffects(CallerBeliefLevel belief)
        {
            return belief switch
            {
                CallerBeliefLevel.Curious => new List<StatModification>
                {
                    new StatModification(StatType.Discernment, 1f)
                },
                CallerBeliefLevel.Partial => new List<StatModification>(),
                CallerBeliefLevel.Committed => new List<StatModification>
                {
                    new StatModification(StatType.Spirit, 1f)
                },
                CallerBeliefLevel.Certain => new List<StatModification>
                {
                    new StatModification(StatType.Spirit, 2f),
                    new StatModification(StatType.Skepticism, 1f)
                },
                CallerBeliefLevel.Zealot => new List<StatModification>
                {
                    new StatModification(StatType.Patience, -3f),
                    new StatModification(StatType.Spirit, -1f)
                },
                _ => new List<StatModification>()
            };
        }

        /// <summary>
        /// Evidence level affects Vern's spirit, discernment, and skepticism.
        /// Good evidence boosts the show, no evidence increases skepticism.
        /// </summary>
        private static List<StatModification> GetEvidenceLevelEffects(CallerEvidenceLevel evidence)
        {
            return evidence switch
            {
                CallerEvidenceLevel.None => new List<StatModification>
                {
                    new StatModification(StatType.Skepticism, 2f)
                },
                CallerEvidenceLevel.Low => new List<StatModification>
                {
                    new StatModification(StatType.Skepticism, 1f)
                },
                CallerEvidenceLevel.Medium => new List<StatModification>(),
                CallerEvidenceLevel.High => new List<StatModification>
                {
                    new StatModification(StatType.Spirit, 2f),
                    new StatModification(StatType.Discernment, 1f)
                },
                CallerEvidenceLevel.Irrefutable => new List<StatModification>
                {
                    new StatModification(StatType.Spirit, 5f),
                    new StatModification(StatType.Discernment, 2f)
                },
                _ => new List<StatModification>()
            };
        }

        /// <summary>
        /// Legitimacy affects Vern's spirit, patience, and skepticism.
        /// Fake callers are demoralizing, compelling callers boost the show.
        /// </summary>
        private static List<StatModification> GetLegitimacyEffects(CallerLegitimacy legitimacy)
        {
            return legitimacy switch
            {
                CallerLegitimacy.Fake => new List<StatModification>
                {
                    new StatModification(StatType.Spirit, -5f),
                    new StatModification(StatType.Patience, -3f)
                },
                CallerLegitimacy.Questionable => new List<StatModification>
                {
                    new StatModification(StatType.Skepticism, 2f)
                },
                CallerLegitimacy.Credible => new List<StatModification>(),
                CallerLegitimacy.Compelling => new List<StatModification>
                {
                    new StatModification(StatType.Spirit, 3f),
                    new StatModification(StatType.Discernment, 2f)
                },
                _ => new List<StatModification>()
            };
        }

        /// <summary>
        /// Phone/audio quality affects Vern's patience.
        /// Bad connections are frustrating to deal with.
        /// </summary>
        private static List<StatModification> GetPhoneQualityEffects(CallerPhoneQuality quality)
        {
            return quality switch
            {
                CallerPhoneQuality.Terrible => new List<StatModification>
                {
                    new StatModification(StatType.Patience, -3f)
                },
                CallerPhoneQuality.Poor => new List<StatModification>
                {
                    new StatModification(StatType.Patience, -1f)
                },
                CallerPhoneQuality.Average => new List<StatModification>(),
                CallerPhoneQuality.Good => new List<StatModification>
                {
                    new StatModification(StatType.Patience, 1f)
                },
                _ => new List<StatModification>()
            };
        }

        /// <summary>
        /// Aggregate stat effects from multiple properties.
        /// Effects for the same stat are added together.
        /// </summary>
        /// <param name="properties">Collection of screenable properties.</param>
        /// <param name="revealedOnly">If true, only include effects from revealed properties.</param>
        /// <returns>Dictionary of stat type to total effect amount.</returns>
        public static Dictionary<StatType, float> AggregateStatEffects(
            IEnumerable<ScreenableProperty> properties,
            bool revealedOnly = true)
        {
            var totals = new Dictionary<StatType, float>();

            foreach (var prop in properties)
            {
                if (revealedOnly && !prop.IsRevealed)
                {
                    continue;
                }

                foreach (var effect in prop.StatEffects)
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
    }
}
