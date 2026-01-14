namespace KBTV.Ads
{
    /// <summary>
    /// Tiers of advertisers, unlocked by reaching peak listener milestones.
    /// Higher tiers pay more per listener.
    /// </summary>
    public enum AdType
    {
        /// <summary>
        /// Local businesses - available from the start.
        /// Rate: $0.02 per listener per slot.
        /// </summary>
        LocalBusiness,

        /// <summary>
        /// Regional brands - unlocked at 200 peak listeners.
        /// Rate: $0.04 per listener per slot.
        /// </summary>
        RegionalBrand,

        /// <summary>
        /// National sponsors - unlocked at 500 peak listeners.
        /// Rate: $0.06 per listener per slot.
        /// </summary>
        NationalSponsor,

        /// <summary>
        /// Premium sponsors - unlocked at 1000 peak listeners.
        /// Rate: $0.10 per listener per slot.
        /// </summary>
        PremiumSponsor
    }
}