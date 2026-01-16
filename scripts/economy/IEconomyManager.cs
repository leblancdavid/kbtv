using System;
using Godot;

namespace KBTV.Economy
{
    public interface IEconomyManager
    {
        int CurrentMoney { get; }

        event Action<int, int> MoneyChanged;
        event Action<int, string> Purchase;
        event Action<int> PurchaseFailed;

        bool CanAfford(int amount);
        void AddMoney(int amount, string reason = null);
        bool SpendMoney(int amount, string reason = null);
        void SetMoney(int amount);
    }
}
