using System.Collections.Generic;
using Godot;

namespace KBTV.Upgrades
{
    /// <summary>
    /// Resource defining all equipment upgrades and their costs.
    /// Create via Godot editor as a Resource.
    /// </summary>
    public partial class EquipmentConfig : Resource
    {
        [Export] private Godot.Collections.Array<EquipmentUpgrade> _phoneLineUpgrades = new Godot.Collections.Array<EquipmentUpgrade>();
        [Export] private Godot.Collections.Array<EquipmentUpgrade> _broadcastUpgrades = new Godot.Collections.Array<EquipmentUpgrade>();
        [Export] private Godot.Collections.Array<EquipmentUpgrade> _antennaUpgrades = new Godot.Collections.Array<EquipmentUpgrade>();

        /// <summary>
        /// Get all upgrades for an equipment type.
        /// </summary>
        public Godot.Collections.Array<EquipmentUpgrade> GetUpgrades(EquipmentType type)
        {
            return type switch
            {
                EquipmentType.PhoneLine => _phoneLineUpgrades,
                EquipmentType.Broadcast => _broadcastUpgrades,
                EquipmentType.Antenna => _antennaUpgrades,
                _ => new Godot.Collections.Array<EquipmentUpgrade>()
            };
        }

        /// <summary>
        /// Get a specific upgrade by type and level.
        /// </summary>
        public EquipmentUpgrade GetUpgrade(EquipmentType type, int level)
        {
            var upgrades = GetUpgrades(type);
            foreach (var upgrade in upgrades)
            {
                if (upgrade.Level == level)
                    return upgrade;
            }
            return null;
        }

        /// <summary>
        /// Get the next available upgrade after the current level.
        /// </summary>
        public EquipmentUpgrade GetNextUpgrade(EquipmentType type, int currentLevel)
        {
            return GetUpgrade(type, currentLevel + 1);
        }

        /// <summary>
        /// Get the maximum level for an equipment type.
        /// </summary>
        public int GetMaxLevel(EquipmentType type)
        {
            var upgrades = GetUpgrades(type);
            int max = 1;
            foreach (var upgrade in upgrades)
            {
                if (upgrade.Level > max) max = upgrade.Level;
            }
            return max;
        }

        /// <summary>
        /// Get the max listeners for a city based on antenna level.
        /// Formula: (level * 250) + 500
        /// Level 1: 750, Level 2: 1000, Level 3: 1250, Level 4: 1500, Level 5: 1750
        /// </summary>
        public int GetMaxListenersForLevel(int antennaLevel)
        {
            return (antennaLevel * 250) + 500;
        }

        /// <summary>
        /// Get the upgrade cost for the next antenna level.
        /// Returns 0 if already at max level.
        /// </summary>
        public int GetAntennaUpgradeCost(int currentLevel)
        {
            if (currentLevel >= GetMaxLevel(EquipmentType.Antenna))
                return 0;

            var upgrade = GetNextUpgrade(EquipmentType.Antenna, currentLevel);
            return upgrade?.Cost ?? 0;
        }

        /// <summary>
        /// Create default upgrades if none exist.
        /// Called from editor script or on validate.
        /// </summary>
        public void InitializeDefaults()
        {
            if (_phoneLineUpgrades.Count == 0)
            {
                _phoneLineUpgrades.Add(new EquipmentUpgrade("Stock Phone Lines", "Heavy static, narrow bandwidth, noticeable distortion.", 1, 0));
                _phoneLineUpgrades.Add(new EquipmentUpgrade("Basic Phone Lines", "Reduced static, slightly wider bandwidth.", 2, 300));
                _phoneLineUpgrades.Add(new EquipmentUpgrade("Professional Lines", "Minimal static, clear voice transmission.", 3, 800));
                _phoneLineUpgrades.Add(new EquipmentUpgrade("Broadcast Quality", "Crystal clear phone audio, full bandwidth.", 4, 2000));
            }

            if (_broadcastUpgrades.Count == 0)
            {
                _broadcastUpgrades.Add(new EquipmentUpgrade("Stock Broadcast", "Muffled sound, slight distortion, weak presence.", 1, 0));
                _broadcastUpgrades.Add(new EquipmentUpgrade("Basic Broadcast", "Clearer signal, reduced distortion.", 2, 300));
                _broadcastUpgrades.Add(new EquipmentUpgrade("Professional", "Clean radio sound, good presence.", 3, 800));
                _broadcastUpgrades.Add(new EquipmentUpgrade("Studio Quality", "Warm, professional broadcast quality.", 4, 2000));
            }

            if (_antennaUpgrades.Count == 0)
            {
                _antennaUpgrades.Add(new EquipmentUpgrade("Basic Antenna", "Standard antenna, limited range.", 1, 0));
                _antennaUpgrades.Add(new EquipmentUpgrade("Enhanced Antenna", "Improved signal strength.", 2, 300));
                _antennaUpgrades.Add(new EquipmentUpgrade("High-Gain Antenna", "Extended broadcast range.", 3, 600));
                _antennaUpgrades.Add(new EquipmentUpgrade("Professional Array", "Professional-grade antenna system.", 4, 1200));
                _antennaUpgrades.Add(new EquipmentUpgrade("Broadcast Tower", "Full broadcast tower with maximum range.", 5, 2500));
            }
        }
    }
}