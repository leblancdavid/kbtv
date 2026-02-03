using System.Collections.Generic;
using KBTV.Callers;
using KBTV.Data;

namespace KBTV.Screening
{
    /// <summary>
    /// Static utility class that calculates stat effects for caller properties.
    /// Both the property type AND value determine the effect on Vern's stats.
    /// These effects are previewed during screening and applied when the caller goes on-air.
    /// 
    /// Stats Mapping (v2 - Three-Stat System):
    /// - Physical: Energy, stamina, reaction time (range: -5 to +5)
    /// - Emotional: Mood, morale, patience, passion (range: -5 to +5)
    /// - Mental: Discernment, focus, patience (cognitive) (range: -5 to +5)
    /// 
    /// Design Principles:
    /// - Balanced risk/reward: Most properties have both positive and negative extremes
    /// - Full Physical involvement: Callers physically drain/energize Vern
    /// - Extreme values reach ±5, typical values ±1 to ±3
    /// - Neutral/baseline values have no effect
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
                "Personality" => GetPersonalityEffects((string)value),
                // Properties with no stat effects
                "Summary" => new List<StatModification>(),
                "Topic" => new List<StatModification>(),
                _ => new List<StatModification>()
            };
        }

        /// <summary>
        /// Emotional state affects all three stats.
        /// Calm callers are easy to deal with, angry callers are exhausting confrontations.
        /// </summary>
        private static List<StatModification> GetEmotionalStateEffects(CallerEmotionalState state)
        {
            return state switch
            {
                // Calm: Easy, relaxing conversation (+7 total)
                CallerEmotionalState.Calm => new List<StatModification>
                {
                    new StatModification(StatType.Physical, 2f),
                    new StatModification(StatType.Emotional, 3f),
                    new StatModification(StatType.Mental, 2f)
                },
                // Anxious: Exhausting to manage their nerves (-6 total)
                CallerEmotionalState.Anxious => new List<StatModification>
                {
                    new StatModification(StatType.Physical, -2f),
                    new StatModification(StatType.Emotional, -3f),
                    new StatModification(StatType.Mental, -1f)
                },
                // Excited: Energizing but chaotic (+5 total)
                CallerEmotionalState.Excited => new List<StatModification>
                {
                    new StatModification(StatType.Physical, 3f),
                    new StatModification(StatType.Emotional, 4f),
                    new StatModification(StatType.Mental, -2f)
                },
                // Scared: Draining to comfort, but sharpens focus (-4 total)
                CallerEmotionalState.Scared => new List<StatModification>
                {
                    new StatModification(StatType.Physical, -3f),
                    new StatModification(StatType.Emotional, -3f),
                    new StatModification(StatType.Mental, 2f)
                },
                // Angry: Exhausting confrontation (-11 total)
                CallerEmotionalState.Angry => new List<StatModification>
                {
                    new StatModification(StatType.Physical, -3f),
                    new StatModification(StatType.Emotional, -5f),
                    new StatModification(StatType.Mental, -3f)
                },
                _ => new List<StatModification>()
            };
        }

        /// <summary>
        /// Curse risk affects all three stats.
        /// Low risk is relaxing, high risk requires constant vigilance.
        /// </summary>
        private static List<StatModification> GetCurseRiskEffects(CallerCurseRisk risk)
        {
            return risk switch
            {
                // Low: Relaxed, no worry about dump button (+4 total)
                CallerCurseRisk.Low => new List<StatModification>
                {
                    new StatModification(StatType.Physical, 1f),
                    new StatModification(StatType.Emotional, 2f),
                    new StatModification(StatType.Mental, 1f)
                },
                // Medium: On edge, watching for profanity (-3 total)
                CallerCurseRisk.Medium => new List<StatModification>
                {
                    new StatModification(StatType.Emotional, -1f),
                    new StatModification(StatType.Mental, -2f)
                },
                // High: Constant vigilance, stressful (-8 total)
                CallerCurseRisk.High => new List<StatModification>
                {
                    new StatModification(StatType.Physical, -2f),
                    new StatModification(StatType.Emotional, -3f),
                    new StatModification(StatType.Mental, -3f)
                },
                _ => new List<StatModification>()
            };
        }

        /// <summary>
        /// Coherence affects all three stats, especially Mental.
        /// Coherent callers are satisfying, incoherent ones are exhausting to parse.
        /// </summary>
        private static List<StatModification> GetCoherenceEffects(CallerCoherence coherence)
        {
            return coherence switch
            {
                // Coherent: Smooth, satisfying conversation (+8 total)
                CallerCoherence.Coherent => new List<StatModification>
                {
                    new StatModification(StatType.Physical, 2f),
                    new StatModification(StatType.Emotional, 2f),
                    new StatModification(StatType.Mental, 4f)
                },
                // Questionable: Slight effort to follow (-3 total)
                CallerCoherence.Questionable => new List<StatModification>
                {
                    new StatModification(StatType.Emotional, -1f),
                    new StatModification(StatType.Mental, -2f)
                },
                // Incoherent: Exhausting, frustrating to parse (-10 total)
                CallerCoherence.Incoherent => new List<StatModification>
                {
                    new StatModification(StatType.Physical, -2f),
                    new StatModification(StatType.Emotional, -3f),
                    new StatModification(StatType.Mental, -5f)
                },
                _ => new List<StatModification>()
            };
        }

        /// <summary>
        /// Urgency has trade-offs: engaging but tiring at high levels.
        /// Critical urgency is stressful chaos.
        /// </summary>
        private static List<StatModification> GetUrgencyEffects(CallerUrgency urgency)
        {
            return urgency switch
            {
                // Low: Relaxed but less engaging (+2 total)
                CallerUrgency.Low => new List<StatModification>
                {
                    new StatModification(StatType.Physical, 2f),
                    new StatModification(StatType.Emotional, -1f),
                    new StatModification(StatType.Mental, 1f)
                },
                // Medium: Good baseline energy (+1 total)
                CallerUrgency.Medium => new List<StatModification>
                {
                    new StatModification(StatType.Emotional, 1f)
                },
                // High: Exciting but tiring, sharpens focus (+3 total)
                CallerUrgency.High => new List<StatModification>
                {
                    new StatModification(StatType.Physical, -2f),
                    new StatModification(StatType.Emotional, 3f),
                    new StatModification(StatType.Mental, 2f)
                },
                // Critical: Adrenaline rush, stressful chaos (-3 total)
                CallerUrgency.Critical => new List<StatModification>
                {
                    new StatModification(StatType.Physical, -3f),
                    new StatModification(StatType.Emotional, 2f),
                    new StatModification(StatType.Mental, -2f)
                },
                _ => new List<StatModification>()
            };
        }

        /// <summary>
        /// Belief level ranges from curious (easy) to zealot (exhausting).
        /// Moderate belief is engaging, extreme belief is draining.
        /// </summary>
        private static List<StatModification> GetBeliefLevelEffects(CallerBeliefLevel belief)
        {
            return belief switch
            {
                // Curious: Easy, open-minded conversation (+6 total)
                CallerBeliefLevel.Curious => new List<StatModification>
                {
                    new StatModification(StatType.Physical, 2f),
                    new StatModification(StatType.Emotional, 2f),
                    new StatModification(StatType.Mental, 2f)
                },
                // Partial: Neutral baseline (0 total)
                CallerBeliefLevel.Partial => new List<StatModification>(),
                // Committed: Passionate but manageable (+2 total)
                CallerBeliefLevel.Committed => new List<StatModification>
                {
                    new StatModification(StatType.Emotional, 2f)
                },
                // Certain: Intense but tiresome certainty (0 total, but trade-off)
                CallerBeliefLevel.Certain => new List<StatModification>
                {
                    new StatModification(StatType.Physical, -1f),
                    new StatModification(StatType.Emotional, 3f),
                    new StatModification(StatType.Mental, -2f)
                },
                // Zealot: Exhausting, unhinged (-11 total)
                CallerBeliefLevel.Zealot => new List<StatModification>
                {
                    new StatModification(StatType.Physical, -3f),
                    new StatModification(StatType.Emotional, -4f),
                    new StatModification(StatType.Mental, -4f)
                },
                _ => new List<StatModification>()
            };
        }

        /// <summary>
        /// Evidence level affects show quality and Vern's mood.
        /// Good evidence is exciting and vindicating, no evidence is demoralizing.
        /// </summary>
        private static List<StatModification> GetEvidenceLevelEffects(CallerEvidenceLevel evidence)
        {
            return evidence switch
            {
                // None: Wasted time, demoralizing (-6 total)
                CallerEvidenceLevel.None => new List<StatModification>
                {
                    new StatModification(StatType.Physical, -2f),
                    new StatModification(StatType.Emotional, -3f),
                    new StatModification(StatType.Mental, -1f)
                },
                // Low: Disappointing (-3 total)
                CallerEvidenceLevel.Low => new List<StatModification>
                {
                    new StatModification(StatType.Physical, -1f),
                    new StatModification(StatType.Emotional, -2f)
                },
                // Medium: Standard call (0 total)
                CallerEvidenceLevel.Medium => new List<StatModification>(),
                // High: Exciting content (+7 total)
                CallerEvidenceLevel.High => new List<StatModification>
                {
                    new StatModification(StatType.Physical, 2f),
                    new StatModification(StatType.Emotional, 3f),
                    new StatModification(StatType.Mental, 2f)
                },
                // Irrefutable: Great radio, vindicating (+11 total)
                CallerEvidenceLevel.Irrefutable => new List<StatModification>
                {
                    new StatModification(StatType.Physical, 3f),
                    new StatModification(StatType.Emotional, 5f),
                    new StatModification(StatType.Mental, 3f)
                },
                _ => new List<StatModification>()
            };
        }

        /// <summary>
        /// Legitimacy heavily affects Emotional and Mental.
        /// Fake callers are embarrassing and demoralizing, compelling callers boost the show.
        /// </summary>
        private static List<StatModification> GetLegitimacyEffects(CallerLegitimacy legitimacy)
        {
            return legitimacy switch
            {
                // Fake: Wasted time, embarrassing (-11 total)
                CallerLegitimacy.Fake => new List<StatModification>
                {
                    new StatModification(StatType.Physical, -2f),
                    new StatModification(StatType.Emotional, -5f),
                    new StatModification(StatType.Mental, -4f)
                },
                // Questionable: Doubt creeps in (-5 total)
                CallerLegitimacy.Questionable => new List<StatModification>
                {
                    new StatModification(StatType.Physical, -1f),
                    new StatModification(StatType.Emotional, -2f),
                    new StatModification(StatType.Mental, -2f)
                },
                // Credible: Solid caller (+2 total)
                CallerLegitimacy.Credible => new List<StatModification>
                {
                    new StatModification(StatType.Emotional, 1f),
                    new StatModification(StatType.Mental, 1f)
                },
                // Compelling: Great content (+9 total)
                CallerLegitimacy.Compelling => new List<StatModification>
                {
                    new StatModification(StatType.Physical, 2f),
                    new StatModification(StatType.Emotional, 4f),
                    new StatModification(StatType.Mental, 3f)
                },
                _ => new List<StatModification>()
            };
        }

        /// <summary>
        /// Audio quality affects all stats - bad connections are frustrating and exhausting.
        /// Good audio is pleasant and easy.
        /// </summary>
        private static List<StatModification> GetPhoneQualityEffects(CallerPhoneQuality quality)
        {
            return quality switch
            {
                // Terrible: Straining to hear, frustrating (-8 total)
                CallerPhoneQuality.Terrible => new List<StatModification>
                {
                    new StatModification(StatType.Physical, -2f),
                    new StatModification(StatType.Emotional, -3f),
                    new StatModification(StatType.Mental, -3f)
                },
                // Poor: Slight irritation (-4 total)
                CallerPhoneQuality.Poor => new List<StatModification>
                {
                    new StatModification(StatType.Physical, -1f),
                    new StatModification(StatType.Emotional, -2f),
                    new StatModification(StatType.Mental, -1f)
                },
                // Average: Standard baseline (0 total)
                CallerPhoneQuality.Average => new List<StatModification>(),
                // Good: Clear and pleasant (+6 total)
                CallerPhoneQuality.Good => new List<StatModification>
                {
                    new StatModification(StatType.Physical, 2f),
                    new StatModification(StatType.Emotional, 2f),
                    new StatModification(StatType.Mental, 2f)
                },
                _ => new List<StatModification>()
            };
        }

        /// <summary>
        /// Personality effects are looked up from the PersonalityStatEffects class.
        /// Each of the 36 personalities has unique stat combinations.
        /// </summary>
        private static List<StatModification> GetPersonalityEffects(string personalityName)
        {
            return PersonalityStatEffects.GetEffects(personalityName);
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
