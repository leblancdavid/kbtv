using System;
using Godot;
using KBTV.Core;
// TODO: Add when ported - using KBTV.Persistence;

namespace KBTV.Economy
{
    /// <summary>
    /// Manages the player's money and transactions.
    /// </summary>
    public partial class EconomyManager : SingletonNode<EconomyManager>
    {
        [Export] private int _startingMoney = 500;

        private int _currentMoney;

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

        /// <summary>Fired when money changes. (oldAmount, newAmount)</summary>
        public event Action<int, int> OnMoneyChanged;

        /// <summary>Fired on successful purchase. (amount, reason)</summary>
        public event Action<int, string> OnPurchase;

        /// <summary>Fired when purchase fails due to insufficient funds.</summary>
        public event Action<int> OnPurchaseFailed;

        // ─────────────────────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────────────────────

        protected override void OnSingletonReady()
        {
            _currentMoney = _startingMoney;
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
                GD.Print($"[EconomyManager] Attempted to add non-positive amount: {amount}");
                return;
            }

            int oldMoney = _currentMoney;
            _currentMoney += amount;

            OnMoneyChanged?.Invoke(oldMoney, _currentMoney);

            // TODO: Add when SaveManager is ported
            // if (SaveManager.Instance != null)
            // {
            //     SaveManager.Instance.MarkDirty();
            // }
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
                GD.Print($"[EconomyManager] Attempted to spend non-positive amount: {amount}");
                return false;
            }

            if (!CanAfford(amount))
            {
                GD.Print($"[EconomyManager] Cannot afford ${amount}. Balance: ${_currentMoney}");
                OnPurchaseFailed?.Invoke(amount);
                return false;
            }

            int oldMoney = _currentMoney;
            _currentMoney -= amount;

            if (!string.IsNullOrEmpty(reason))
            {
                GD.Print($"[EconomyManager] -${amount} ({reason}). Balance: ${_currentMoney}");
            }

            OnMoneyChanged?.Invoke(oldMoney, _currentMoney);
            OnPurchase?.Invoke(amount, reason);

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
                OnMoneyChanged?.Invoke(oldMoney, _currentMoney);
            }
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