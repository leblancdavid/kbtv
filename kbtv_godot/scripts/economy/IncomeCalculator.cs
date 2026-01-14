using Godot;

namespace KBTV.Economy
{
    /// <summary>
    /// Calculates post-show income.
    /// Currently a stub with a fixed stipend - will integrate with Ad System later.
    /// </summary>
    public static class IncomeCalculator
    {
        /// <summary>
        /// Base stipend per show until ads are implemented.
        /// </summary>
        private const int BASE_STIPEND = 100;

        /// <summary>
        /// Calculate income for a completed show.
        /// </summary>
        /// <param name="peakListeners">Peak listener count during the show</param>
        /// <param name="showQuality">Show quality percentage (0-100)</param>
        /// <returns>Total income for the show</returns>
        public static int CalculateShowIncome(int peakListeners, float showQuality)
        {
            // TODO: Replace with Ad System revenue calculation
            // For now, just return a flat stipend

            int income = BASE_STIPEND;

            return income;
        }

        /// <summary>
        /// Get a breakdown of income sources for UI display.
        /// </summary>
        public static IncomeBreakdown GetIncomeBreakdown(int peakListeners, float showQuality)
        {
            return new IncomeBreakdown
            {
                BaseStipend = BASE_STIPEND,
                AdRevenue = 0, // Not implemented yet
                BonusIncome = 0,
                Total = BASE_STIPEND
            };
        }
    }

    /// <summary>
    /// Breakdown of income sources for UI display.
    /// </summary>
    public struct IncomeBreakdown
    {
        public int BaseStipend;
        public int AdRevenue;
        public int BonusIncome;
        public int Total;
    }
}