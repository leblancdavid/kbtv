using UnityEngine;
using KBTV.Callers;
using KBTV.Data;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Calculates Vern's discernment outcomes - whether he correctly reads a caller.
    /// Uses the formula: correctReadChance = Discernment + LegitimacyModifier
    /// </summary>
    public static class DiscernmentCalculator
    {
        // Legitimacy modifiers as defined in CONVERSATION_ARCS.md
        private const float COMPELLING_MODIFIER = 0.20f;  // Easy to believe
        private const float CREDIBLE_MODIFIER = 0.10f;    // Solid evidence
        private const float QUESTIONABLE_MODIFIER = 0.0f; // Ambiguous
        private const float FAKE_MODIFIER = 0.15f;        // Often obvious tells

        /// <summary>
        /// Get the legitimacy modifier for discernment calculation.
        /// </summary>
        public static float GetLegitimacyModifier(CallerLegitimacy legitimacy)
        {
            return legitimacy switch
            {
                CallerLegitimacy.Compelling => COMPELLING_MODIFIER,
                CallerLegitimacy.Credible => CREDIBLE_MODIFIER,
                CallerLegitimacy.Questionable => QUESTIONABLE_MODIFIER,
                CallerLegitimacy.Fake => FAKE_MODIFIER,
                _ => 0f
            };
        }

        /// <summary>
        /// Calculate the chance of Vern correctly reading the caller.
        /// </summary>
        /// <param name="discernment">Vern's discernment stat (0-1 normalized)</param>
        /// <param name="legitimacy">The caller's actual legitimacy</param>
        /// <returns>Probability of correct read (0-1)</returns>
        public static float CalculateCorrectReadChance(float discernment, CallerLegitimacy legitimacy)
        {
            float modifier = GetLegitimacyModifier(legitimacy);
            return Mathf.Clamp01(discernment + modifier);
        }

        /// <summary>
        /// Determine which belief path Vern takes based on discernment roll.
        /// </summary>
        /// <param name="discernment">Vern's discernment stat (0-1 normalized)</param>
        /// <param name="legitimacy">The caller's actual legitimacy</param>
        /// <returns>The belief path Vern will take</returns>
        public static BeliefPath DetermineBeliefPath(float discernment, CallerLegitimacy legitimacy)
        {
            float correctReadChance = CalculateCorrectReadChance(discernment, legitimacy);
            bool correctRead = Random.value < correctReadChance;

            // Determine correct path based on legitimacy
            bool callerIsLegit = legitimacy == CallerLegitimacy.Credible || 
                                 legitimacy == CallerLegitimacy.Compelling;

            if (correctRead)
            {
                // Vern reads the caller correctly
                return callerIsLegit ? BeliefPath.Believing : BeliefPath.Skeptical;
            }
            else
            {
                // Vern misreads the caller
                return callerIsLegit ? BeliefPath.Skeptical : BeliefPath.Believing;
            }
        }

        /// <summary>
        /// Determine belief path using VernStats (gets discernment from Susceptibility stat).
        /// Lower susceptibility = higher discernment.
        /// </summary>
        public static BeliefPath DetermineBeliefPath(VernStats stats, CallerLegitimacy legitimacy)
        {
            // Discernment is inverse of susceptibility
            // High susceptibility = low discernment (easily fooled)
            // Low susceptibility = high discernment (hard to fool)
            float discernment = stats?.Susceptibility != null 
                ? 1f - stats.Susceptibility.Normalized 
                : 0.5f;

            return DetermineBeliefPath(discernment, legitimacy);
        }
    }
}
