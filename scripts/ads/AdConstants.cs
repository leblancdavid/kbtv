namespace KBTV.Ads
{
    /// <summary>
    /// Constants for ad system timing and configuration.
    /// </summary>
    public static class AdConstants
    {
        // Time budget constants
        public const float SHOW_DURATION_SECONDS = 600f;  // 10 minutes
        public const float AD_SLOT_DURATION = 18f;        // Average ad length
        public const float BREAK_JINGLE_DURATION = 5f;    // Break transition jingle
        public const float RETURN_JINGLE_DURATION = 3f;   // Return from break jingle

        // Break scheduling constants
        public const int MAX_BREAKS_PER_SHOW = 4;
        public const int MAX_SLOTS_PER_BREAK = 3;
        public const int DEFAULT_BREAKS_PER_SHOW = 2;
        public const int DEFAULT_SLOTS_PER_BREAK = 2;

        // Queue button timing
        public const float BREAK_WINDOW_DURATION = 20f;   // How long before break the button enables
        public const float BREAK_GRACE_TIME = 10f;        // When grace period starts (let line finish)
        public const float BREAK_IMMINENT_TIME = 5f;      // Fallback interrupt point

        // Player feedback
        public const float UNQUEUED_MOOD_PENALTY = 15f;   // Mood penalty if player didn't queue
        public const float LISTENER_DIP_PERCENTAGE = 0.05f;  // 5% listener dip during breaks

        // Estimated timing for UI display
        public static float GetEstimatedBreakDuration(int slotsPerBreak)
        {
            return BREAK_JINGLE_DURATION +
                   (slotsPerBreak * AD_SLOT_DURATION) +
                   RETURN_JINGLE_DURATION;
        }
    }
}
