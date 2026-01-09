using System;
using UnityEngine;

namespace KBTV.Data
{
    /// <summary>
    /// Tracks item quantities for a single item type.
    /// </summary>
    [Serializable]
    public class ItemSlot
    {
        public StatModifier Modifier;
        public Item ItemData; // May be null if using plain StatModifier
        public int Quantity;
        public float CooldownRemaining;

        public bool IsOnCooldown => CooldownRemaining > 0f;
        public bool HasStock => Quantity > 0;
        public bool CanUse => HasStock && !IsOnCooldown;

        public string ItemId => ItemData != null ? ItemData.ItemId : Modifier.name;
        public string DisplayName => Modifier.DisplayName;
        public string ShortName => ItemData != null ? ItemData.ShortName : Modifier.DisplayName;
        public int Hotkey => ItemData != null ? ItemData.Hotkey : 0;
        public float Cooldown => ItemData != null ? ItemData.Cooldown : 0f;
        public bool UsableDuringShow => ItemData == null || ItemData.UsableDuringShow;

        public ItemSlot(StatModifier modifier, int quantity = 0)
        {
            Modifier = modifier;
            ItemData = modifier as Item;
            Quantity = quantity;
            CooldownRemaining = 0f;
        }
    }
}
