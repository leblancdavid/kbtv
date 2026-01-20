using System;
using System.Collections.Generic;
using Godot;

namespace KBTV.Ads
{
    /// <summary>
    /// Configuration for a single ad break, including timing and ad slots.
    /// </summary>
    [Serializable]
    public partial class AdBreakConfig
    {
        [Export] public int BreakIndex { get; set; }
        [Export] public float ScheduledTime { get; set; }
        [Export] public int SlotsPerBreak { get; set; }
        [Export] public bool WasQueued { get; set; }
        [Export] public bool HasPlayed { get; set; }

        public AdBreakConfig() { }

        public AdBreakConfig(int breakIndex, float scheduledTime, int slotsPerBreak)
        {
            BreakIndex = breakIndex;
            ScheduledTime = scheduledTime;
            SlotsPerBreak = slotsPerBreak;
            WasQueued = false;
            HasPlayed = false;
        }

        /// <summary>
        /// Calculate the total duration of this break in seconds.
        /// Includes break jingle, all ad slots, and return jingle.
        /// </summary>
        public float GetTotalDuration()
        {
            return AdConstants.BREAK_JINGLE_DURATION +
                   (SlotsPerBreak * AdConstants.AD_SLOT_DURATION) +
                   AdConstants.RETURN_JINGLE_DURATION;
        }
    }
}
