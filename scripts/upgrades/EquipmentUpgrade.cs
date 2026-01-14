using System;
using Godot;

namespace KBTV.Upgrades
{
    /// <summary>
    /// Data for a single equipment upgrade level.
    /// </summary>
    [Serializable]
    public partial class EquipmentUpgrade : Resource
    {
        [Export] public string Name;
        [Export(PropertyHint.MultilineText)] public string Description;
        [Export(PropertyHint.Range, "1,4")] public int Level;
        [Export] public int Cost;

        /// <summary>
        /// Creates a default upgrade.
        /// </summary>
        public EquipmentUpgrade()
        {
            Name = "Upgrade";
            Description = "";
            Level = 1;
            Cost = 0;
        }

        /// <summary>
        /// Creates an upgrade with specified values.
        /// </summary>
        public EquipmentUpgrade(string name, string description, int level, int cost)
        {
            Name = name;
            Description = description;
            Level = level;
            Cost = cost;
        }
    }
}