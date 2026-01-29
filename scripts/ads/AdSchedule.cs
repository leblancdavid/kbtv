using System;
using System.Collections.Generic;
using Godot;

namespace KBTV.Ads
{
    /// <summary>
    /// Player's ad configuration for a single show.
    /// Created in PreShow and passed to AdManager when show starts.
    /// </summary>
    [Serializable]
    public partial class AdSchedule
    {
        [Export] public int BreaksPerShow { get; set; }
        [Export] public int SlotsPerBreak { get; set; }
        [Export] public List<AdBreakConfig> Breaks { get; set; } = new();

        public AdSchedule() { }

        public AdSchedule(int breaksPerShow, int slotsPerBreak)
        {
            BreaksPerShow = breaksPerShow;
            SlotsPerBreak = slotsPerBreak;
        }

        /// <summary>
        /// Calculate the total ad time for this schedule in seconds.
        /// Includes all breaks with jingles.
        /// </summary>
        public float GetTotalAdTime()
        {
            if (Breaks.Count == 0) return 0f;

            float breakDuration = AdConstants.BREAK_JINGLE_DURATION +
                                   (SlotsPerBreak * AdConstants.AD_SLOT_DURATION) +
                                   AdConstants.RETURN_JINGLE_DURATION;
            return Breaks.Count * breakDuration;
        }

        /// <summary>
        /// Get the total number of ad slots for this schedule.
        /// </summary>
        public int GetTotalSlots()
        {
            return Breaks.Count * SlotsPerBreak;
        }

        /// <summary>
        /// Calculate estimated revenue range based on listener counts.
        /// </summary>
        public (float min, float max) EstimateRevenueRange(int minListeners, int maxListeners)
        {
            int totalSlots = GetTotalSlots();
            float minRevenue = totalSlots * minListeners * AdData.GetRevenueRate(AdType.LocalBusiness);
            float maxRevenue = totalSlots * maxListeners * AdData.GetRevenueRate(AdType.PremiumSponsor);
            return (minRevenue, maxRevenue);
        }

        /// <summary>
        /// Generate break configurations for evenly spaced breaks throughout the show.
        /// </summary>
        public void GenerateBreakSchedule(float showDuration)
        {
            Breaks.Clear();

            if (BreaksPerShow <= 0) return;

            for (int i = 0; i < BreaksPerShow; i++)
            {
                // Evenly space breaks throughout the show duration
                float scheduledTime = showDuration * (i + 1) / (BreaksPerShow + 1);
                Breaks.Add(new AdBreakConfig(i, scheduledTime, SlotsPerBreak));
            }
        }
    }
}
