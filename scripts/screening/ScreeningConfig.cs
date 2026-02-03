namespace KBTV.Screening
{
    /// <summary>
    /// Centralized configuration for screening timing.
    /// Base total: 60 seconds to reveal all 11 properties.
    /// SpeedMultiplier: 1.0 = normal, >1.0 = faster, less than 1.0 = slower
    /// </summary>
    public static class ScreeningConfig
    {
        /// <summary>
        /// Speed multiplier applied to all reveal times.
        /// 1.0 = baseline (60s total), 1.5 = 50% faster (40s), 0.5 = 50% slower (120s)
        /// Equipment upgrades increase this value.
        /// </summary>
        public static float SpeedMultiplier { get; set; } = 1.0f;

        /// <summary>
        /// Base reveal durations in seconds (before multiplier).
        /// Total: 60 seconds baseline.
        /// </summary>
        public static class BaseDurations
        {
            // Tier 1 - Easy to reveal (11s total)
            // Surface-level observations, quick to assess
            public const float AudioQuality = 3f;
            public const float EmotionalState = 4f;
            public const float CurseRisk = 4f;

            // Tier 2 - Medium difficulty (27s total)
            // Requires some conversation/listening
            public const float Summary = 5f;
            public const float Personality = 5f;
            public const float BeliefLevel = 6f;
            public const float Evidence = 6f;
            public const float Urgency = 5f;

            // Tier 3 - Hard to reveal (22s total)
            // Requires extended listening and assessment
            public const float Topic = 6f;
            public const float Legitimacy = 8f;
            public const float Coherence = 8f;
        }

        /// <summary>
        /// Get effective reveal duration for a property.
        /// Higher multiplier = faster = shorter duration.
        /// </summary>
        /// <param name="propertyKey">The property identifier (e.g., "Topic", "AudioQuality").</param>
        /// <returns>Effective duration in seconds after applying multiplier.</returns>
        public static float GetRevealDuration(string propertyKey)
        {
            float baseDuration = propertyKey switch
            {
                // Tier 1 - Easy
                "AudioQuality" => BaseDurations.AudioQuality,
                "EmotionalState" => BaseDurations.EmotionalState,
                "CurseRisk" => BaseDurations.CurseRisk,

                // Tier 2 - Medium
                "Summary" => BaseDurations.Summary,
                "Personality" => BaseDurations.Personality,
                "BeliefLevel" => BaseDurations.BeliefLevel,
                "Evidence" => BaseDurations.Evidence,
                "Urgency" => BaseDurations.Urgency,

                // Tier 3 - Hard
                "Topic" => BaseDurations.Topic,
                "Legitimacy" => BaseDurations.Legitimacy,
                "Coherence" => BaseDurations.Coherence,

                // Default fallback for unknown properties
                _ => 5f
            };

            // Higher multiplier = faster = shorter duration
            // e.g., multiplier 2.0 cuts duration in half
            return baseDuration / SpeedMultiplier;
        }

        /// <summary>
        /// Get the total baseline screening time (sum of all property durations).
        /// </summary>
        /// <returns>Total baseline time in seconds (60s at multiplier 1.0).</returns>
        public static float GetTotalBaselineTime()
        {
            return (BaseDurations.AudioQuality + BaseDurations.EmotionalState + BaseDurations.CurseRisk +
                    BaseDurations.Summary + BaseDurations.Personality + BaseDurations.BeliefLevel +
                    BaseDurations.Evidence + BaseDurations.Urgency +
                    BaseDurations.Topic + BaseDurations.Legitimacy + BaseDurations.Coherence)
                   / SpeedMultiplier;
        }
    }
}
