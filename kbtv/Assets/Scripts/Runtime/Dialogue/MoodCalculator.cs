using KBTV.Data;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Calculates Vern's current mood state based on his stats.
    /// Used to select the appropriate mood variant for conversation arcs.
    /// </summary>
    public static class MoodCalculator
    {
        /// <summary>
        /// Determine Vern's mood state based on his current stats.
        /// </summary>
        public static VernMood CalculateMood(VernStats stats)
        {
            if (stats == null)
                return VernMood.Neutral;

            float energy = stats.Energy?.Normalized ?? 0.5f;
            float mood = stats.Mood?.Normalized ?? 0.5f;
            float patience = stats.Patience?.Normalized ?? 0.5f;

            // Tired: Low energy takes priority
            if (energy < 0.25f)
            {
                return VernMood.Tired;
            }

            // Grumpy: Low mood + low patience
            if (mood < 0.35f && patience < 0.4f)
            {
                return VernMood.Grumpy;
            }

            // Excited: High mood + high energy
            if (mood > 0.75f && energy > 0.6f)
            {
                return VernMood.Excited;
            }

            // Engaged: Good mood and decent energy
            if (mood > 0.55f && energy > 0.4f)
            {
                return VernMood.Engaged;
            }

            // Default: Neutral
            return VernMood.Neutral;
        }

        /// <summary>
        /// Get a human-readable description of the mood.
        /// </summary>
        public static string GetMoodDescription(VernMood mood)
        {
            return mood switch
            {
                VernMood.Tired => "Vern is tired and low on energy",
                VernMood.Grumpy => "Vern is in a bad mood and impatient",
                VernMood.Neutral => "Vern is professional and balanced",
                VernMood.Engaged => "Vern is interested and attentive",
                VernMood.Excited => "Vern is energized and enthusiastic",
                _ => "Vern's mood is unknown"
            };
        }
    }
}
