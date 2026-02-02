using System;

namespace KBTV.Data
{
    /// <summary>
    /// Defines which stat to target with a modifier.
    /// See docs/VERN_STATS.md for complete stat documentation.
    /// </summary>
    public enum StatType
    {
        // Dependencies
        Caffeine,
        Nicotine,

        // Physical
        Energy,
        Satiety,

        // Emotional
        Spirit,

        // Cognitive
        Alertness,
        Discernment,
        Focus,
        Patience,

        // Long-Term
        Belief
    }


}