using System;
using Godot;
using KBTV.Core;
// TODO: Add when ported - using KBTV.Persistence;

namespace KBTV.Economy
{
    /// <summary>
    /// Manages the player's money and transactions.
    /// </summary>
  	public partial class EconomyManager : Node
  	{
		[Signal] public delegate void MoneyChangedEventHandler(int oldAmount, int newAmount);
		[Signal] public delegate void PurchaseEventHandler(int amount, string reason);
		[Signal] public delegate void PurchaseFailedEventHandler(int amount);
        [Export] private int _startingMoney = 500;

        private int _currentMoney;
        private bool _initialized;

        // ─────────────────────────────────────────────────────────────
        // Properties
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Current money balance.
        /// </summary>
        public int CurrentMoney => _currentMoney;

        // ─────────────────────────────────────────────────────────────
        // Events
        // ─────────────────────────────────────────────────────────────



        // ─────────────────────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────────────────────

        public override void _Ready()
        {
            _currentMoney = _startingMoney;
            // Removed RegisterSelf - now registered in ServiceProviderRoot
        }

        /// <summary>
        /// Initialize the EconomyManager with starting money.
        /// </summary>
        public void Initialize()
        {
            if (!_initialized)
            {
                _currentMoney = _startingMoney;
                _initialized = true;
            }
        }

        // TODO: Add when SaveManager is ported
        // private void Start()
        // {
        //     // Register with SaveManager
        //     if (SaveManager.Instance != null)
        //     {
        //         SaveManager.Instance.RegisterSaveable(this);
        //     }
        // }

        // public override void _ExitTree()
        // {
        //     if (SaveManager.Instance != null)
        //     {
        //         SaveManager.Instance.UnregisterSaveable(this);
        //     }
        // }

        // ─────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Check if player can afford an amount.
        /// </summary>
        public bool CanAfford(int amount)
        {
            return _currentMoney >= amount;
        }

        /// <summary>
        /// Add money to the player's balance.
        /// </summary>
        /// <param name="amount">Amount to add (must be positive)</param>
        /// <param name="reason">Optional reason for logging</param>
        public void AddMoney(int amount, string reason = null)
        {
            if (amount <= 0)
            {
                return;
            }

            int oldMoney = _currentMoney;
            _currentMoney += amount;

            EmitSignal("MoneyChanged", oldMoney, _currentMoney);
        }

        /// <summary>
        /// Spend money from the player's balance.
        /// </summary>
        /// <param name="amount">Amount to spend</param>
        /// <param name="reason">Optional reason for logging</param>
        /// <returns>True if successful, false if insufficient funds</returns>
        public bool SpendMoney(int amount, string reason = null)
        {
            if (amount <= 0)
            {
                return false;
            }

            if (!CanAfford(amount))
            {
                EmitSignal("PurchaseFailed", amount);
                return false;
            }

            int oldMoney = _currentMoney;
            _currentMoney -= amount;

            EmitSignal("MoneyChanged", oldMoney, _currentMoney);
            EmitSignal("Purchase", amount, reason);

            // TODO: Add when SaveManager is ported
            // if (SaveManager.Instance != null)
            // {
            //     SaveManager.Instance.MarkDirty();
            // }

            return true;
        }

        /// <summary>
        /// Set money directly (for save/load).
        /// </summary>
        public void SetMoney(int amount)
        {
            int oldMoney = _currentMoney;
            _currentMoney = Mathf.Max(0, amount);

            if (oldMoney != _currentMoney)
            {
                EmitSignal("MoneyChanged", oldMoney, _currentMoney);
            }
        }

        /// <summary>
        /// Apply FCC fine for violating broadcast standards (cursing on air).
        /// </summary>
        /// <param name="amount">Fine amount to deduct</param>
        /// <returns>True if fine was applied, false if insufficient funds</returns>
        public bool ApplyFCCFine(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            int oldMoney = _currentMoney;
            _currentMoney = Mathf.Max(0, _currentMoney - amount);

            EmitSignal("MoneyChanged", oldMoney, _currentMoney);
            EmitSignal("Purchase", amount, $"FCC Fine - Profanity Violation");

            return true; // Fine is always applied, even if it puts balance negative
        }

        // ─────────────────────────────────────────────────────────────
        // ISaveable (TODO: Implement when SaveManager is ported)
        // ─────────────────────────────────────────────────────────────

        // public void OnBeforeSave(SaveData data)
        // {
        //     data.Money = _currentMoney;
        // }

        // public void OnAfterLoad(SaveData data)
        // {
        //     SetMoney(data.Money);
        //     GD.Print($"[EconomyManager] Loaded money: ${_currentMoney}");
        // }
    }
}