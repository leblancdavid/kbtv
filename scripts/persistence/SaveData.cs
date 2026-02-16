using System;
using System.Collections.Generic;
using KBTV.Data;
using KBTV.Items;

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
        public bool DisableBroadcastAudio = true;

        // ─────────────────────────────────────────────────────────────
        // Station Reach
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Total maximum listener capacity across all cities.
        /// </summary>
        public int StationReach = 500;

        /// <summary>
        /// City data for station expansion.
        /// </summary>
        [Serializable]
        public class CityData
        {
            public string CityId;
            public string CityName;
            public int AntennaLevel = 1;
            public bool IsUnlocked;
            public int UnlockCost;
        }

        /// <summary>
        /// List of all cities (some locked, some unlocked).
        /// </summary>
        public List<CityData> Cities = new List<CityData>();

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
        // Topic XP
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Topic experience data for persistence.
        /// </summary>
        [Serializable]
        public class TopicXPData
        {
            public string TopicId;
            public float XP;
            public int HighestLevelReached;
        }

        /// <summary>
        /// XP progression data for all topics.
        /// Empty for new games (topics start at 0 XP).
        /// </summary>
        public List<TopicXPData> TopicXPs;

        // ─────────────────────────────────────────────────────────────
        // Evidence Collection
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Legacy: Collection of raw evidence items (migrated to EvidenceSystemData).
        /// </summary>
        public List<EvidenceItem> CollectedEvidence = new List<EvidenceItem>();

        /// <summary>
        /// New evidence system with identification, cabinet, and website.
        /// </summary>
        public EvidenceSystemData EvidenceSystem = new EvidenceSystemData();

        /// <summary>
        /// Creates a new save with default starting values.
        /// </summary>
        public static SaveData CreateNew()
        {
            var save = new SaveData
            {
                Version = 6,
                LastSaveTime = DateTime.UtcNow.ToString("o"),
                CurrentNight = 1,
                Money = 500,
                ShowDurationMinutes = 10,
                DisableBroadcastAudio = true,
                StationReach = 750,  // Hometown at level 1: (1 * 250) + 500 = 750
                EquipmentLevels = new System.Collections.Generic.Dictionary<string, int>(),
                ItemQuantities = new System.Collections.Generic.Dictionary<string, int>(),
                TotalCallersScreened = 0,
                TotalShowsCompleted = 0,
                PeakListenersAllTime = 0,
                TopicXPs = new List<TopicXPData>(),
                CollectedEvidence = new List<EvidenceItem>(),
                Cities = new List<CityData>
                {
                    new CityData { CityId = "hometown", CityName = "Hometown", AntennaLevel = 1, IsUnlocked = true, UnlockCost = 0 },
                    new CityData { CityId = "downtown", CityName = "Downtown", AntennaLevel = 1, IsUnlocked = false, UnlockCost = 500 },
                    new CityData { CityId = "suburbs", CityName = "Suburbs", AntennaLevel = 1, IsUnlocked = false, UnlockCost = 1000 },
                    new CityData { CityId = "industrial", CityName = "Industrial District", AntennaLevel = 1, IsUnlocked = false, UnlockCost = 2000 },
                    new CityData { CityId = "mountains", CityName = "Mountains", AntennaLevel = 1, IsUnlocked = false, UnlockCost = 0 }
                }
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