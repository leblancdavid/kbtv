using System;

namespace KBTV.Data
{
    /// <summary>
    /// Defines which stat to target with a modifier.
    /// See docs/systems/VERN_STATS.md for complete stat documentation.
    /// </summary>
    public enum StatType
    {
        // Dependencies (0-100)
        Caffeine,
        Nicotine,

        // Core Stats (-100 to +100)
        Physical,
        Emotional,
        Mental
    }
}
