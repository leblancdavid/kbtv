# KBTV - Economy System

## Overview

The economy system tracks player money, handles transactions, and calculates income. Money is earned from shows (via ads) and spent on items, equipment upgrades, and station improvements.

## Core Concepts

| Concept | Description |
|---------|-------------|
| **Money** | Primary currency, integer value |
| **Income** | Money earned at end of each show |
| **Transactions** | Purchases that deduct money |
| **Stipend** | Temporary per-show income until ad system is implemented |

## Architecture

### Components

| Component | Description |
|-----------|-------------|
| `EconomyManager` | Singleton tracking money, handling transactions |
| `IncomeCalculator` | Calculates post-show income (stub until ads) |

## EconomyManager API

### Properties

```csharp
// Current money balance
int CurrentMoney { get; }

// Check if player can afford an amount
bool CanAfford(int amount);
```

### Methods

```csharp
// Add money (income, rewards, debug)
void AddMoney(int amount, string reason = null);

// Spend money (returns false if insufficient funds)
bool SpendMoney(int amount, string reason = null);

// Set money directly (for save/load)
void SetMoney(int amount);
```

### Events

```csharp
// Fired when money changes
event Action<int, int> OnMoneyChanged;  // (oldAmount, newAmount)

// Fired on successful purchase
event Action<int, string> OnPurchase;   // (amount, reason)

// Fired when purchase fails (insufficient funds)
event Action<int> OnPurchaseFailed;     // (attemptedAmount)
```

## Income System

### Current Implementation (Stub)

Until the ad system is implemented, income is a simple per-show stipend:

```csharp
public class IncomeCalculator
{
    private const int BASE_STIPEND = 100;
    
    public int CalculateShowIncome(int peakListeners, float showQuality)
    {
        // Stub: flat stipend until ads are implemented
        return BASE_STIPEND;
    }
}
```

### Future Implementation (With Ads)

When the ad system is complete, income will be based on:

| Factor | Description |
|--------|-------------|
| **Ad Revenue** | Number of ads played * ad value |
| **Listener Bonus** | Higher listeners = more valuable ads |
| **Show Quality** | Quality multiplier on ad effectiveness |
| **Sponsor Deals** | Fixed payments from recurring sponsors |

See [AD_SYSTEM.md](AD_SYSTEM.md) for planned ad mechanics.

## Starting Values

| Value | Amount | Notes |
|-------|--------|-------|
| Starting Money | $500 | Enough for a few upgrades or items |
| Per-Show Stipend | $100 | Temporary until ads |
| New Game Items | 3 each | Coffee, Water, Sandwich, Whiskey, Cigarette |

## Spending Categories

### Equipment Upgrades (PostShow)

| Upgrade | Cost Range |
|---------|------------|
| Phone Line (L1→L4) | $300 → $800 → $2000 |
| Broadcast (L1→L4) | $300 → $800 → $2000 |

See [EQUIPMENT_SYSTEM.md](EQUIPMENT_SYSTEM.md) for details.

### Items (PreShow Shop - Future)

| Item | Cost | Effect |
|------|------|--------|
| Coffee | $10 | +Energy, +Mood |
| Water | $5 | +Thirst |
| Sandwich | $15 | +Hunger, +Energy |
| Whiskey | $20 | +Mood, +Susceptibility, -Energy |
| Cigarette | $8 | +Mood, -Patience |

### Station Upgrades (Future)

| Upgrade | Cost | Effect |
|---------|------|--------|
| Larger Queue | $500 | Hold more callers |
| Better Screening | $750 | More caller info visible |
| Longer Shows | $1000 | Extended broadcast time |

## Transaction Flow

### Purchase Flow

```
1. Player initiates purchase (UI button)
2. Check CanAfford(cost)
   - If false: Fire OnPurchaseFailed, show UI feedback, abort
3. SpendMoney(cost, "Equipment: Phone Line L2")
4. Apply purchase effect (upgrade equipment, add item, etc.)
5. Fire OnMoneyChanged
6. SaveManager.MarkDirty() for auto-save
```

### Income Flow

```
1. LiveShow ends → GameStateManager transitions to PostShow
2. IncomeCalculator.CalculateShowIncome() returns amount
3. EconomyManager.AddMoney(amount, "Show Income")
4. PostShow UI displays income breakdown
5. SaveManager.Save() auto-triggers
```

## UI Integration

### Money Display

- Show current money in header bar (all phases)
- Format: `$1,250` with thousands separator
- Flash green on income, red on purchase

### PostShow Income Screen (Future)

```
+----------------------------------+
|        SHOW COMPLETE             |
|                                  |
|  Callers Screened: 8             |
|  Peak Listeners: 523             |
|  Show Quality: 72%               |
|                                  |
|  ─────────────────────────────   |
|  Base Stipend:        $100       |
|  (Ad revenue coming soon!)       |
|  ─────────────────────────────   |
|  TOTAL INCOME:        $100       |
|                                  |
|  Balance: $600 → $700            |
|                                  |
|  [Continue to Upgrades]          |
+----------------------------------+
```

### Insufficient Funds Feedback

When player tries to purchase something they can't afford:
- Play error sound (`SFXType.ItemEmpty`)
- Flash money display red
- Show tooltip: "Insufficient funds - need $X more"

## Persistence

Money is saved as part of `SaveData`:

```csharp
public class SaveData
{
    public int Money = 500;
    // ...
}
```

`EconomyManager` implements `ISaveable`:

```csharp
public void OnBeforeSave(SaveData data)
{
    data.Money = _currentMoney;
}

public void OnAfterLoad(SaveData data)
{
    _currentMoney = data.Money;
}
```

## Debug Tools

| Command | Effect |
|---------|--------|
| `EconomyManager.Instance.AddMoney(1000)` | Add $1000 |
| `EconomyManager.Instance.SetMoney(0)` | Set to $0 |
| Debug UI slider | Adjust money in real-time |

## Balance Considerations

### Early Game (Nights 1-5)

- Stipend: $100/show
- After 5 shows: $500 earned + $500 starting = $1000
- Can afford: 1-2 equipment upgrades OR stock up on items

### Mid Game (Nights 6-15)

- With ads (future): ~$200-400/show depending on performance
- Should unlock most L2-L3 upgrades

### Late Game (Nights 15+)

- High-value ads, sponsor deals
- All equipment maxed, focus on station expansion

## Future Considerations

- **Debt system**: Borrow money with interest
- **Expenses**: Equipment maintenance costs
- **Investments**: Spend money for passive income
- **Achievements**: Bonus money for milestones

## References

- [AD_SYSTEM.md](AD_SYSTEM.md) - Ad-based income (planned)
- [EQUIPMENT_SYSTEM.md](EQUIPMENT_SYSTEM.md) - Equipment purchases
- [SAVE_SYSTEM.md](SAVE_SYSTEM.md) - Persistence
- [GAME_DESIGN.md](GAME_DESIGN.md) - Overall design
