using System;
using System.Collections.Generic;

namespace KBTV.Items
{
    /// <summary>
    /// Evidence quality tiers based on collection difficulty and topic progression.
    /// </summary>
    public enum EvidenceTier
    {
        Common,
        Uncommon,
        Rare,
        VeryRare,
        OneOfAKind
    }

    /// <summary>
    /// Manages loot table probabilities for evidence quality based on topic level.
    /// </summary>
    public static class EvidenceLootTable
    {
        /// <summary>
        /// Probability arrays for each topic level (1-7).
        /// Format: [Common, Uncommon, Rare, VeryRare, OneOfAKind]
        /// </summary>
        private static readonly float[][] _levelProbabilities = new float[][]
        {
            // Level 1: Mostly common, occasionally uncommon, rarely rare
            new float[] { 0.70f, 0.20f, 0.10f, 0.00f, 0.00f },
            // Level 2
            new float[] { 0.60f, 0.25f, 0.15f, 0.00f, 0.00f },
            // Level 3
            new float[] { 0.50f, 0.30f, 0.20f, 0.00f, 0.00f },
            // Level 4
            new float[] { 0.35f, 0.30f, 0.30f, 0.05f, 0.00f },
            // Level 5
            new float[] { 0.25f, 0.25f, 0.35f, 0.15f, 0.00f },
            // Level 6
            new float[] { 0.15f, 0.20f, 0.35f, 0.25f, 0.05f },
            // Level 7: Rare happens often, occasionally purple/gold, rarely golden/common
            new float[] { 0.05f, 0.15f, 0.40f, 0.30f, 0.10f }
        };

        /// <summary>
        /// Rolls for evidence quality based on topic level.
        /// </summary>
        /// <param name="topicLevel">The current topic level (1-7, clamped if out of range)</param>
        /// <returns>The rolled evidence tier</returns>
        public static EvidenceTier RollQuality(int topicLevel)
        {
            // Clamp topic level to valid range
            int levelIndex = Math.Clamp(topicLevel - 1, 0, _levelProbabilities.Length - 1);

            float[] probabilities = _levelProbabilities[levelIndex];
            float roll = Random.Shared.NextSingle(); // 0.0 to 1.0

            float cumulative = 0f;
            for (int i = 0; i < probabilities.Length; i++)
            {
                cumulative += probabilities[i];
                if (roll <= cumulative)
                {
                    return (EvidenceTier)i;
                }
            }

            // Fallback (should not reach here if probabilities sum to 1.0)
            return EvidenceTier.Common;
        }

        /// <summary>
        /// Gets the probability array for a given topic level for debugging/testing.
        /// </summary>
        public static float[] GetProbabilities(int topicLevel)
        {
            int levelIndex = Math.Clamp(topicLevel - 1, 0, _levelProbabilities.Length - 1);
            return _levelProbabilities[levelIndex];
        }
    }
}