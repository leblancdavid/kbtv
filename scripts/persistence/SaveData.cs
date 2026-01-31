using System;
using System.Collections.Generic;

namespace KBTV.Persistence
{
    /// <summary>
    /// Root container for all persistent game data.
    /// Serialized to JSON and saved to disk.
    /// </summary>
    public class SaveData
    {
        /// <summary>
        /// Save format version for migration support.
        /// Increment when making breaking changes to save structure.
        /// </summary>
        public int Version = 1;

        /// <summary>
        /// ISO 8601 timestamp of when the save was created.
        /// </summary>
        public string LastSaveTime;

        // ─────────────────────────────────────────────────────────────
        // Progress
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Current night number (1-based).
        /// </summary>
        public int CurrentNight = 1;

        /// <summary>
        /// Player's current money balance.
        /// </summary>
        public int Money = 500;

        /// <summary>
        /// Show duration in minutes (1-20, default 10).
        /// </summary>
        public int ShowDurationMinutes = 10;

        /// <summary>
        /// Whether broadcast audio is disabled (uses 4-second timeouts instead).
        /// </summary>
        public bool DisableBroadcastAudio = false;

        // ─────────────────────────────────────────────────────────────
        // Equipment
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Equipment levels keyed by EquipmentType name (e.g., "PhoneLine" -> 2).
        /// All equipment starts at level 1.
        /// </summary>
        public System.Collections.Generic.Dictionary<string, int> EquipmentLevels;

        // ─────────────────────────────────────────────────────────────
        // Inventory
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Item quantities keyed by item ID (e.g., "coffee" -> 5).
        /// </summary>
        public System.Collections.Generic.Dictionary<string, int> ItemQuantities;

        // ─────────────────────────────────────────────────────────────
        // Lifetime Stats
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Total number of callers screened across all shows.
        /// </summary>
        public int TotalCallersScreened = 0;

        /// <summary>
        /// Total number of shows completed.
        /// </summary>
        public int TotalShowsCompleted = 0;

        /// <summary>
        /// Highest peak listener count ever achieved.
        /// </summary>
        public int PeakListenersAllTime = 0;

        // ─────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new save with default starting values.
        /// </summary>
        public static SaveData CreateNew()
        {
            var save = new SaveData
            {
                Version = 2,
                LastSaveTime = DateTime.UtcNow.ToString("o"),
                CurrentNight = 1,
                Money = 500,
                ShowDurationMinutes = 10,
                DisableBroadcastAudio = false,
                EquipmentLevels = new System.Collections.Generic.Dictionary<string, int>(),
                ItemQuantities = new System.Collections.Generic.Dictionary<string, int>(),
                TotalCallersScreened = 0,
                TotalShowsCompleted = 0,
                PeakListenersAllTime = 0
            };

            // Initialize default equipment levels
            save.EquipmentLevels["PhoneLine"] = 1;
            save.EquipmentLevels["Broadcast"] = 1;

            // Initialize default item quantities
            save.ItemQuantities["coffee"] = 3;
            save.ItemQuantities["water"] = 3;
            save.ItemQuantities["sandwich"] = 3;
            save.ItemQuantities["whiskey"] = 3;
            save.ItemQuantities["cigarette"] = 3;

            return save;
        }
    }
}