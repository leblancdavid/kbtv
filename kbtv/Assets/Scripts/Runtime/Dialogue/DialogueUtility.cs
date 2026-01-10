using System;
using UnityEngine;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Utility methods for dialogue systems.
    /// </summary>
    public static class DialogueUtility
    {
        /// <summary>
        /// Select a random item from an array using weighted probability.
        /// Items with higher weights are more likely to be selected.
        /// </summary>
        /// <typeparam name="T">The type of items in the array.</typeparam>
        /// <param name="items">Array of items to select from.</param>
        /// <param name="weightSelector">Function to extract the weight from each item.</param>
        /// <returns>A randomly selected item, or default if array is null/empty.</returns>
        public static T GetWeightedRandom<T>(T[] items, Func<T, float> weightSelector)
        {
            if (items == null || items.Length == 0)
                return default;

            float totalWeight = 0f;
            foreach (var item in items)
            {
                totalWeight += weightSelector(item);
            }

            float random = UnityEngine.Random.Range(0f, totalWeight);
            float current = 0f;

            foreach (var item in items)
            {
                current += weightSelector(item);
                if (random <= current)
                    return item;
            }

            return items[items.Length - 1];
        }

        /// <summary>
        /// Get a weighted random DialogueTemplate from an array.
        /// Convenience overload for the common case.
        /// </summary>
        public static DialogueTemplate GetWeightedRandom(DialogueTemplate[] templates)
        {
            return GetWeightedRandom(templates, t => t.Weight);
        }
    }
}
