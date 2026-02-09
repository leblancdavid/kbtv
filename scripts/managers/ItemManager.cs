using Godot;
using KBTV.Data;

namespace KBTV.Managers
{
    /// <summary>
    /// Provides access to consumable item replenishment actions.
    /// Items are infinite - no quantity tracking needed.
    /// </summary>
    public partial class ItemManager : Node
    {
        /// <summary>
        /// Replenish caffeine (coffee effect).
        /// Sets caffeine to 100 and boosts Physical +10.
        /// </summary>
        public void UseCoffee(VernStats vernStats)
        {
            if (vernStats != null)
            {
                vernStats.UseCoffee();
            }
        }

        /// <summary>
        /// Replenish nicotine (cigarette effect).
        /// Sets nicotine to 100 and boosts Emotional +5.
        /// </summary>
        public void UseCigarette(VernStats vernStats)
        {
            if (vernStats != null)
            {
                vernStats.UseCigarette();
            }
        }
    }
}