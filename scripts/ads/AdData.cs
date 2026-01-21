using Godot;

namespace KBTV.Ads
{
    /// <summary>
    /// Resource defining a single advertisement.
    /// Create instances via Godot editor as a Resource.
    /// </summary>
    public partial class AdData : Resource
    {
        [Export] public string Id;
        [Export] public string AdvertiserName;
        [Export] public string Tagline;
        [Export] public AdType Type = AdType.LocalBusiness;

        // TODO: Audio streams when implementing audio system
        // [Export] private Godot.Collections.Array<AudioStream> _audioVariations = new Godot.Collections.Array<AudioStream>();
        [Export(PropertyHint.Range, "10,30")] public float FallbackDuration = 18f;

        /// <summary>
        /// Whether this ad has any audio clips assigned.
        /// </summary>
        public bool HasAudio => false; // TODO: Implement when audio system is added

        /// <summary>
        /// Gets a random audio variation for playback.
        /// Returns null if no audio is assigned.
        /// </summary>
        public AudioStream GetRandomVariation()
        {
            if (!HasAudio) return null;

            // TODO: Implement when audio system is added
            return null;
        }

        /// <summary>
        /// Gets the actual duration - from first audio clip if available, otherwise the FallbackDuration field.
        /// </summary>
        public float ActualDuration => FallbackDuration; // TODO: Use audio length when implemented

        /// <summary>
        /// Revenue rate per listener for this ad type.
        /// </summary>
        public float RevenuePerListener => GetRevenueRate(Type);

        /// <summary>
        /// Peak listener threshold required to unlock this ad type.
        /// </summary>
        public int UnlockThreshold => GetUnlockThreshold(Type);

        /// <summary>
        /// Gets the revenue rate for an ad type.
        /// </summary>
        public static float GetRevenueRate(AdType type)
        {
            return type switch
            {
                AdType.LocalBusiness => 0.02f,
                AdType.RegionalBrand => 0.05f,
                AdType.NationalSponsor => 0.08f,
                AdType.PremiumSponsor => 0.12f,
                _ => 0.02f
            };
        }

        public static string GetAdTypeDisplayName(AdType type)
        {
            return type switch
            {
                AdType.LocalBusiness => "Local Business",
                AdType.RegionalBrand => "Regional Brand",
                AdType.NationalSponsor => "National Sponsor",
                AdType.PremiumSponsor => "Premium Sponsor",
                _ => "Advertisement"
            };
        }

        /// <summary>
        /// Gets the peak listener unlock threshold for an ad type.
        /// Currently all ads are unlocked at start for testing/early game variety.
        /// </summary>
        public static int GetUnlockThreshold(AdType type)
        {
            // All ads available from start for variety
            // TODO: Restore tiered unlocks when more LocalBusiness ads exist
            return 0;

            /* Original tiered thresholds:
            return type switch
            {
                AdType.LocalBusiness => 0,
                AdType.RegionalBrand => 200,
                AdType.NationalSponsor => 500,
                AdType.PremiumSponsor => 1000,
                _ => 0
            };
            */
        }
    }
}