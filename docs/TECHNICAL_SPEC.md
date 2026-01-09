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
├── Data/           # Stat, VernStats, StatModifier
├── Managers/       # TimeManager, LiveShowManager
├── Callers/        # Caller, CallerQueue, CallerGenerator, CallerScreeningManager, Topic
└── UI/             # DebugUI
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

## Key Patterns

- **Events**: Loose coupling via C# events (`OnPhaseChanged`, `OnStatsChanged`, `OnCallerCompleted`, etc.)
- **Singletons**: Managers accessed via static `Instance` property
- **SerializeField**: Private fields exposed to Inspector for configuration
- **RequireComponent**: Enforces component dependencies (e.g., LiveShowManager requires GameStateManager)

## Performance Targets
<!-- TBD -->

## Platform Requirements
<!-- TBD -->
