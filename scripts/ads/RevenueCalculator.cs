using System;
using Godot;
using KBTV.Ads;
using KBTV.Core;
using KBTV.Managers;

namespace KBTV.Ads
{
    /// <summary>
    /// Handles calculation of ad revenue and award distribution.
    /// Extracted from AdManager to improve modularity.
    /// </summary>
    public class RevenueCalculator
    {
        public float CalculateBreakRevenue(int currentListeners, AdBreakConfig breakConfig)
        {
            int totalSlots = breakConfig.SlotsPerBreak;

            // For now, use a mix of ad types based on listener count
            AdType adType = DetermineAdType(currentListeners);

            float totalRevenue = 0f;
            for (int i = 0; i < totalSlots; i++)
            {
                totalRevenue += currentListeners * AdData.GetRevenueRate(adType);
            }

            return totalRevenue;
        }

        private AdType DetermineAdType(int listenerCount)
        {
            // Simple tiered system based on listener count
            if (listenerCount >= 1000) return AdType.PremiumSponsor;
            if (listenerCount >= 500) return AdType.NationalSponsor;
            if (listenerCount >= 200) return AdType.RegionalBrand;
            return AdType.LocalBusiness;
        }

        public void AwardRevenue(float revenue)
        {
            var economy = ServiceRegistry.Instance.EconomyManager;
            if (economy != null)
            {
                economy.AddMoney((int)revenue, "Ad Revenue");
            }
        }
    }
}