using System;
using System.Collections.Generic;
using UnityEngine;
using KBTV.Core;
using KBTV.Data;

namespace KBTV.Managers
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

    /// <summary>
    /// Manages Vern's item inventory and handles item usage during shows.
    /// </summary>
    public class ItemManager : MonoBehaviour
    {
        public static ItemManager Instance { get; private set; }

        [Header("Available Items")]
        [Tooltip("All items available in the game (Item or StatModifier assets)")]
        [SerializeField] private StatModifier[] _availableItems;

        [Header("Starting Inventory")]
        [Tooltip("How many of each item Vern starts with")]
        [SerializeField] private int _startingQuantity = 3;

        // Runtime inventory
        private Dictionary<string, ItemSlot> _inventory = new Dictionary<string, ItemSlot>();
        private List<ItemSlot> _itemSlots = new List<ItemSlot>();

        public IReadOnlyList<ItemSlot> ItemSlots => _itemSlots;

        /// <summary>
        /// Fired when an item is used. (Modifier, remaining quantity)
        /// </summary>
        public event Action<StatModifier, int> OnItemUsed;

        /// <summary>
        /// Fired when inventory changes (item added, removed, or quantity changed).
        /// </summary>
        public event Action OnInventoryChanged;

        /// <summary>
        /// Fired when an item's cooldown state changes.
        /// </summary>
        public event Action<StatModifier> OnCooldownChanged;

        private GameStateManager _gameState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            _gameState = GameStateManager.Instance;

            if (_gameState != null)
            {
                _gameState.OnPhaseChanged += HandlePhaseChanged;
            }

            InitializeInventory();
        }

        private void OnDestroy()
        {
            if (_gameState != null)
            {
                _gameState.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void Update()
        {
            UpdateCooldowns(Time.deltaTime);
        }

        private void InitializeInventory()
        {
            _inventory.Clear();
            _itemSlots.Clear();

            if (_availableItems == null) return;

            int hotkeyCounter = 1;
            foreach (var modifier in _availableItems)
            {
                if (modifier == null) continue;

                var slot = new ItemSlot(modifier, _startingQuantity);
                
                // Auto-assign hotkeys if not set
                if (slot.Hotkey == 0 && slot.ItemData != null)
                {
                    // Item has no hotkey assigned
                }
                
                _inventory[slot.ItemId] = slot;
                _itemSlots.Add(slot);

                Debug.Log($"ItemManager: Added {_startingQuantity}x {modifier.DisplayName}");
            }

            OnInventoryChanged?.Invoke();
        }

        private void HandlePhaseChanged(GamePhase oldPhase, GamePhase newPhase)
        {
            if (newPhase == GamePhase.PreShow)
            {
                // Reset cooldowns at the start of each night
                ResetAllCooldowns();
            }
        }

        private void UpdateCooldowns(float deltaTime)
        {
            foreach (var slot in _itemSlots)
            {
                if (slot.CooldownRemaining > 0f)
                {
                    slot.CooldownRemaining -= deltaTime;
                    if (slot.CooldownRemaining <= 0f)
                    {
                        slot.CooldownRemaining = 0f;
                        OnCooldownChanged?.Invoke(slot.Modifier);
                    }
                }
            }
        }

        /// <summary>
        /// Try to use an item by ID.
        /// </summary>
        public bool UseItem(string itemId)
        {
            if (!_inventory.TryGetValue(itemId, out ItemSlot slot))
            {
                Debug.LogWarning($"ItemManager: Item '{itemId}' not found in inventory");
                return false;
            }

            return UseItem(slot);
        }

        /// <summary>
        /// Try to use an item from a slot.
        /// </summary>
        public bool UseItem(ItemSlot slot)
        {
            if (slot == null || slot.Modifier == null) return false;

            // Check if usable during current phase
            if (_gameState != null && _gameState.IsLive && !slot.UsableDuringShow)
            {
                Debug.Log($"ItemManager: {slot.DisplayName} cannot be used during live show");
                return false;
            }

            // Check cooldown
            if (slot.IsOnCooldown)
            {
                Debug.Log($"ItemManager: {slot.DisplayName} is on cooldown ({slot.CooldownRemaining:F1}s)");
                return false;
            }

            // Check stock
            if (!slot.HasStock)
            {
                Debug.Log($"ItemManager: No {slot.DisplayName} remaining");
                return false;
            }

            // Apply the item effects
            if (_gameState != null && _gameState.VernStats != null)
            {
                slot.Modifier.Apply(_gameState.VernStats);
            }

            // Consume the item
            slot.Quantity--;

            // Start cooldown
            if (slot.Cooldown > 0f)
            {
                slot.CooldownRemaining = slot.Cooldown;
                OnCooldownChanged?.Invoke(slot.Modifier);
            }

            Debug.Log($"ItemManager: Used {slot.DisplayName}. {slot.Quantity} remaining.");
            
            OnItemUsed?.Invoke(slot.Modifier, slot.Quantity);
            OnInventoryChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// Try to use an item by hotkey number (1-9).
        /// </summary>
        public bool UseItemByHotkey(int hotkey)
        {
            foreach (var slot in _itemSlots)
            {
                if (slot.Hotkey == hotkey)
                {
                    return UseItem(slot);
                }
            }
            return false;
        }

        /// <summary>
        /// Try to use an item by index in the item list (for keyboard shortcuts 1-9).
        /// </summary>
        public bool UseItemByIndex(int index)
        {
            if (index >= 0 && index < _itemSlots.Count)
            {
                return UseItem(_itemSlots[index]);
            }
            return false;
        }

        /// <summary>
        /// Add items to inventory.
        /// </summary>
        public void AddItem(string itemId, int quantity = 1)
        {
            if (_inventory.TryGetValue(itemId, out ItemSlot slot))
            {
                slot.Quantity += quantity;
                Debug.Log($"ItemManager: Added {quantity}x {slot.DisplayName}. Now have {slot.Quantity}.");
                OnInventoryChanged?.Invoke();
            }
        }

        /// <summary>
        /// Get the slot for a specific item.
        /// </summary>
        public ItemSlot GetSlot(string itemId)
        {
            _inventory.TryGetValue(itemId, out ItemSlot slot);
            return slot;
        }

        /// <summary>
        /// Reset all item cooldowns.
        /// </summary>
        public void ResetAllCooldowns()
        {
            foreach (var slot in _itemSlots)
            {
                if (slot.CooldownRemaining > 0f)
                {
                    slot.CooldownRemaining = 0f;
                    OnCooldownChanged?.Invoke(slot.Modifier);
                }
            }
        }

        /// <summary>
        /// Refill all items to starting quantity.
        /// </summary>
        public void RefillInventory()
        {
            foreach (var slot in _itemSlots)
            {
                slot.Quantity = _startingQuantity;
            }
            OnInventoryChanged?.Invoke();
            Debug.Log("ItemManager: Inventory refilled");
        }
    }
}
