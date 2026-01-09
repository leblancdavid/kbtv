# KBTV - Technical Specification

## Architecture Overview

- **Pattern**: Singleton managers with event-driven communication
- **Namespaces**: `KBTV.Core`, `KBTV.Data`, `KBTV.Managers`, `KBTV.Callers`, `KBTV.UI`
- **Bootstrap**: `GameBootstrap` creates all managers at runtime via reflection injection
- **Data**: ScriptableObjects for configuration (Topics, Items, Events, VernStats)

### File Structure
```
Assets/Scripts/Runtime/
├── Core/           # GamePhase, GameStateManager, GameBootstrap
├── Data/           # Stat, VernStats, StatModifier, Item
├── Managers/       # TimeManager, LiveShowManager, ListenerManager, ItemManager
├── Callers/        # Caller, CallerQueue, CallerGenerator, CallerScreeningManager, Topic
└── UI/             # LiveShowUIManager, panels, components (see UI System)
```

## Core Systems

### Stats System
**Files**: `Data/Stat.cs`, `Data/VernStats.cs`, `Data/StatModifier.cs`

- `VernStats` (ScriptableObject) contains 7 stats (0-100 range):
  - **Mood** - Affected by events, interactions, items
  - **Energy** - Drops over time, restored by caffeine/items
  - **Hunger/Thirst** - Basic needs, increase over time
  - **Patience** - Tolerance for waiting/bad callers
  - **Susceptibility** - How much evidence impacts Vern's beliefs
  - **Belief** - Core meter, correlates to show quality
- `StatModifier` ScriptableObjects apply stat changes (items/events call `Apply(VernStats)`)
- `CalculateShowQuality()` returns 0-1: Belief (40%) + Mood (25%) + Energy (20%) + Patience (15%), penalized by high Hunger/Thirst
- `ApplyDecay(deltaTime, multiplier)` degrades stats during live shows

### Phase/Time System
**Files**: `Core/GamePhase.cs`, `Core/GameStateManager.cs`, `Managers/TimeManager.cs`, `Managers/LiveShowManager.cs`

- **3 Phases**: `PreShow` → `LiveShow` → `PostShow` → (new night, back to PreShow)
- `GameStateManager` (Singleton): Controls `CurrentPhase`, `CurrentNight`, holds `VernStats` reference
  - `AdvancePhase()` transitions phases, `StartNewNight()` increments night counter
  - Events: `OnPhaseChanged`, `OnNightStarted`
- `TimeManager` (Singleton): In-game clock for live shows
  - Default 300s real-time = 4 hours in-game (10 PM - 2 AM)
  - `StartClock()`, `PauseClock()`, `ResetClock()`
  - Events: `OnTick`, `OnShowEnded`
- `LiveShowManager`: Glue component connecting GameStateManager + TimeManager
  - Starts clock when phase changes to LiveShow
  - Applies stat decay on every tick
  - Advances to PostShow when show ends

### Listener System
**Files**: `Managers/ListenerManager.cs`

- `ListenerManager` (Singleton): Tracks audience size during live shows
  - **Starting Listeners**: Base count with random variance at show start
  - **Quality-Based Growth**: Listeners increase/decrease based on show quality vs threshold
  - **Caller Impact**: Great callers add listeners, bad callers cost listeners
  - **Disconnect Penalty**: Callers hanging up loses some listeners
- Configurable settings:
  - `_baseListeners` (1000) - Starting audience
  - `_qualityGrowthRate` (5/sec) - How fast listeners change
  - `_qualityThreshold` (0.5) - Quality level where growth is neutral
  - `_greatCallerBonus` (+150), `_goodCallerBonus` (+50), `_badCallerPenalty` (-100)
- Events: `OnListenersChanged`, `OnPeakReached`
- Properties: `CurrentListeners`, `PeakListeners`, `ListenerChange`

### Item System
**Files**: `Data/Item.cs`, `Managers/ItemManager.cs`, `UI/ItemPanel.cs`

- `Item` (ScriptableObject, extends StatModifier): Consumable items with additional settings
  - `ItemId` - Unique identifier
  - `Cost` - Purchase price (for PreShow shop)
  - `Cooldown` - Seconds before item can be used again
  - `UsableDuringShow` - Whether item works during live show
  - `Hotkey` - Keyboard shortcut (1-9)
  - `ShortName` - Abbreviated name for UI
- `ItemManager` (Singleton): Manages inventory and item usage
  - `ItemSlot` - Tracks quantity and cooldown per item
  - `UseItem(itemId)` / `UseItemByIndex(index)` - Consume and apply item
  - Works with both `Item` and plain `StatModifier` assets
  - Events: `OnItemUsed`, `OnInventoryChanged`, `OnCooldownChanged`
- `ItemPanel` (UI): Displays items with use buttons
  - Shows hotkey indicator, name, quantity, cooldown timer
  - Keyboard shortcuts 1-5 to use items by index
  - Buttons disabled when out of stock or on cooldown

### Caller Screening System
**Files**: `Callers/Caller.cs`, `Callers/CallerQueue.cs`, `Callers/CallerGenerator.cs`, `Callers/CallerScreeningManager.cs`

- **Caller Lifecycle**: `Incoming` → `Screening` → `OnHold` → `OnAir` → `Completed` (or `Rejected`/`Disconnected`)
- **Legitimacy Levels**: `Fake`, `Questionable`, `Credible`, `Compelling`
- `CallerGenerator` (Singleton): Spawns random callers during LiveShow
  - Configurable spawn intervals, patience, legitimacy distribution
  - 70% on-topic when topic is set, fake callers have 50% shorter patience
- `CallerQueue` (Singleton): Manages caller queues
  - Max 10 incoming, max 3 on-hold
  - `UpdateCallerPatience()` ticks wait times, disconnects impatient callers
- `CallerScreeningManager` (Singleton): Validates callers against Topic rules
  - `ScreenCurrentCaller()` returns pass/fail based on topic ScreeningRules
  - Applies StatModifiers on call completion based on impact

### Topics System
**Files**: `Callers/Topic.cs`

- `Topic` (ScriptableObject): Defines show topics with screening rules
  - `ScreeningRules[]` - Array of rules callers must pass
  - `DeceptionRate` - Chance callers lie about their topic
  - `QualityMultiplier` - Bonus for on-topic callers
- `ScreeningRule` types:
  - `TopicMustMatch` - Caller's claimed topic must match
  - `LocationRequired` / `LocationBanned` - Geographic filters
  - `AreaCodeRequired` - Phone number filter
  - `MinimumLegitimacy` - Minimum credibility threshold

## Data Assets

Located in `Assets/Data/`:

| Type | Assets |
|------|--------|
| **Topics** | UFOs, Cryptids, GovernmentConspiracies, GhostsAndHauntings, AncientMysteries, TimeTravel, MenInBlack, OpenLines |
| **Items** | Coffee, Water, Sandwich, Whiskey, Cigarette |
| **Events** | GoodCaller, BadCaller, GreatCaller, Evidence, Debunked, TechnicalDifficulties |

## UI System
**Files**: `UI/LiveShowUIManager.cs`, `UI/UITheme.cs`, and panel/component scripts

The Live Show UI is **runtime-generated uGUI** (no prefabs). All UI elements are created via code in `LiveShowUIManager.CreateUI()`.

### Architecture

| Script | Purpose |
|--------|---------|
| `UITheme.cs` | Colors, fonts, styling constants; helper methods for creating styled UI elements |
| `StatBarUI.cs` | Reusable stat bar component (label + fill bar + value text) |
| `HeaderBarUI.cs` | Top bar: Night #, phase, clock, remaining time, blinking LIVE indicator |
| `VernStatsPanel.cs` | Left panel: 7 stat bars + show quality display |
| `CallerCardUI.cs` | Reusable caller info card (detailed/compact/on-air variants) |
| `ScreeningPanel.cs` | Current screening caller display + approve/reject buttons |
| `OnAirPanel.cs` | On-air caller display + end call button |
| `CallerQueuePanel.cs` | Incoming and on-hold caller queue lists |
| `LiveShowUIManager.cs` | Main controller, creates Canvas hierarchy, coordinates all panels |

### Theme

- **Background**: Charcoal `#1a1a1a`
- **Panel**: Dark gray `#2d2d2d`
- **Primary Text**: Terminal green `#33ff33`
- **Secondary Text**: Amber `#ffaa00`
- **Accent**: Red `#ff3333` (LIVE indicator, reject button)

### Layout

```
┌─────────────────────────────────────────────────────────┐
│ HeaderBar (Night, Phase, Clock, LIVE)                   │
├─────────────────┬───────────────────────────────────────┤
│ VernStatsPanel  │  ScreeningPanel  │  OnAirPanel       │
│ (7 stat bars)   │  (current caller)│  (active call)    │
│                 ├───────────────────────────────────────┤
│ Show Quality    │  CallerQueuePanel                     │
│                 │  (Incoming | On-Hold queues)          │
└─────────────────┴───────────────────────────────────────┘
```

### Event Subscriptions

Panels subscribe to game events for real-time updates:
- `GameStateManager.OnPhaseChanged` - Phase transitions
- `VernStats.OnStatsChanged` - Stat bar updates
- `CallerQueue.OnQueueChanged` - Queue list updates
- `TimeManager.OnTick` - Clock updates
- `CallerScreeningManager.OnScreeningStarted/Completed` - Caller card updates

## Key Patterns

- **Events**: Loose coupling via C# events (`OnPhaseChanged`, `OnStatsChanged`, `OnCallerCompleted`, etc.)
- **Singletons**: Managers accessed via static `Instance` property
- **SerializeField**: Private fields exposed to Inspector for configuration
- **RequireComponent**: Enforces component dependencies (e.g., LiveShowManager requires GameStateManager)

## Performance Targets
<!-- TBD -->

## Platform Requirements
<!-- TBD -->
