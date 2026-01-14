namespace KBTV.Data
{
    /// <summary>
    /// Vern's current mood type, used to select dialog variants.
    /// Determined by VernStats.CalculateMoodType().
    /// Priority order: Tired → Energetized → Irritated → Amused → Gruff → Focused → Neutral
    /// See docs/VERN_STATS.md for full documentation.
    /// </summary>
    public enum VernMoodType
    {
        /// <summary>Energy < 30 - Slow, flat, misses cues</summary>
        Tired,

        /// <summary>Caffeine > 60 AND Energy > 60 - Enthusiastic, quick-witted</summary>
        Energized,

        /// <summary>Spirit < -10 OR Patience < 40 - Snarky, dismissive</summary>
        Irritated,

        /// <summary>Spirit > 20 AND positive interaction - Laughing, playful</summary>
        Amused,

        /// <summary>Recent bad caller OR Spirit < 0 - Grumpy, reluctant</summary>
        Gruff,

        /// <summary>Alertness > 60 AND Discernment > 50 - Analytical, digging into claims</summary>
        Focused,

        /// <summary>Default state - Professional, balanced</summary>
        Neutral
    }
}