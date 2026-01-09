using UnityEngine;

namespace KBTV.Data
{
    /// <summary>
    /// Represents a consumable item that Vern can use during the show.
    /// Items apply stat modifications and have optional cooldowns.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "KBTV/Item")]
    public class Item : StatModifier
    {
        [Header("Item Settings")]
        [Tooltip("Unique identifier for this item")]
        [SerializeField] private string _itemId;

        [Tooltip("Cost to purchase this item (for PreShow shop)")]
        [SerializeField] private int _cost = 10;

        [Tooltip("Cooldown in seconds before item can be used again (0 = no cooldown)")]
        [SerializeField] private float _cooldown = 0f;

        [Tooltip("Can this item be used during live show?")]
        [SerializeField] private bool _usableDuringShow = true;

        [Tooltip("Keyboard shortcut (1-9) for quick use")]
        [SerializeField] [Range(0, 9)] private int _hotkey = 0;

        [Header("Display")]
        [Tooltip("Short name for UI (max ~8 chars)")]
        [SerializeField] private string _shortName;

        [Tooltip("Icon color for UI representation")]
        [SerializeField] private Color _iconColor = Color.white;

        public string ItemId => string.IsNullOrEmpty(_itemId) ? name : _itemId;
        public int Cost => _cost;
        public float Cooldown => _cooldown;
        public bool UsableDuringShow => _usableDuringShow;
        public int Hotkey => _hotkey;
        public string ShortName => string.IsNullOrEmpty(_shortName) ? DisplayName : _shortName;
        public Color IconColor => _iconColor;

        /// <summary>
        /// Get a brief summary of item effects for tooltip.
        /// </summary>
        public string GetEffectsSummary()
        {
            if (Modifications == null || Modifications.Length == 0)
                return "No effects";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var mod in Modifications)
            {
                if (sb.Length > 0) sb.Append(", ");
                string sign = mod.Amount >= 0 ? "+" : "";
                sb.Append($"{mod.StatType} {sign}{mod.Amount:0}");
            }
            return sb.ToString();
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_itemId))
            {
                _itemId = name;
            }
            if (string.IsNullOrEmpty(_shortName))
            {
                _shortName = DisplayName;
            }
        }
    }
}
