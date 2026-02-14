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
        /// Probability arrays for each belief level (0-25).
        /// Format: [Common, Uncommon, Rare, VeryRare, OneOfAKind]
        /// Belief level = sum of all topic levels (0-6 each)
        /// </summary>
        private static readonly float[][] _levelProbabilities = new float[][]
        {
            // Belief Level 0: Only basic evidence
            new float[] { 0.80f, 0.15f, 0.05f, 0.00f, 0.00f },
            // Level 1
            new float[] { 0.75f, 0.18f, 0.07f, 0.00f, 0.00f },
            // Level 2
            new float[] { 0.70f, 0.20f, 0.10f, 0.00f, 0.00f },
            // Level 3: Introduce VeryRare
            new float[] { 0.65f, 0.22f, 0.12f, 0.01f, 0.00f },
            // Level 4
            new float[] { 0.60f, 0.25f, 0.14f, 0.01f, 0.00f },
            // Level 5
            new float[] { 0.55f, 0.25f, 0.18f, 0.02f, 0.00f },
            // Level 6
            new float[] { 0.50f, 0.25f, 0.20f, 0.05f, 0.00f },
            // Level 7
            new float[] { 0.45f, 0.25f, 0.22f, 0.08f, 0.00f },
            // Level 8
            new float[] { 0.40f, 0.25f, 0.24f, 0.11f, 0.00f },
            // Level 9
            new float[] { 0.35f, 0.25f, 0.28f, 0.12f, 0.00f },
            // Level 10
            new float[] { 0.30f, 0.25f, 0.30f, 0.15f, 0.00f },
            // Level 11: Introduce OneOfAKind
            new float[] { 0.25f, 0.25f, 0.30f, 0.18f, 0.02f },
            // Level 12
            new float[] { 0.22f, 0.25f, 0.30f, 0.20f, 0.03f },
            // Level 13
            new float[] { 0.20f, 0.25f, 0.30f, 0.22f, 0.03f },
            // Level 14
            new float[] { 0.18f, 0.25f, 0.30f, 0.24f, 0.03f },
            // Level 15
            new float[] { 0.15f, 0.25f, 0.30f, 0.26f, 0.04f },
            // Level 16
            new float[] { 0.12f, 0.25f, 0.30f, 0.28f, 0.05f },
            // Level 17
            new float[] { 0.10f, 0.25f, 0.30f, 0.30f, 0.05f },
            // Level 18
            new float[] { 0.08f, 0.25f, 0.30f, 0.32f, 0.05f },
            // Level 19
            new float[] { 0.06f, 0.25f, 0.30f, 0.34f, 0.05f },
            // Level 20
            new float[] { 0.05f, 0.25f, 0.30f, 0.35f, 0.05f },
            // Level 21
            new float[] { 0.04f, 0.25f, 0.30f, 0.36f, 0.05f },
            // Level 22
            new float[] { 0.03f, 0.25f, 0.30f, 0.37f, 0.05f },
            // Level 23
            new float[] { 0.02f, 0.25f, 0.30f, 0.38f, 0.05f },
            // Level 24
            new float[] { 0.01f, 0.25f, 0.30f, 0.39f, 0.05f },
            // Level 25: Peak difficulty
            new float[] { 0.01f, 0.20f, 0.30f, 0.40f, 0.09f }
        };

        /// <summary>
        /// Rolls for evidence quality based on belief level.
        /// </summary>
        /// <param name="beliefLevel">The current belief level (0-25+, clamped to 25)</param>
        /// <returns>The rolled evidence tier</returns>
        public static EvidenceTier RollQuality(int beliefLevel)
        {
            // Clamp belief level to valid range (0-25, levels above 25 use level 25 probabilities)
            int levelIndex = Math.Clamp(beliefLevel, 0, _levelProbabilities.Length - 1);

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