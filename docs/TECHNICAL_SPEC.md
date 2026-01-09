# KBTV - Technical Specification

## Getting Started

### Quick Setup (Recommended)
1. Open Unity with the project (`kbtv/kbtv`)
2. Open `Assets/Scenes/SampleScene.unity`
3. From menu: **KBTV > Setup Game Scene**
4. Press **Play**

The setup utility auto-creates missing assets and configures `GameBootstrap` with all required references.

### Manual Setup
If you prefer manual configuration:
1. Create a VernStats asset: **Assets > Create > KBTV > Vern Stats**
2. Create an empty GameObject named `GameBootstrap`
3. Add the `GameBootstrap` component
4. Assign fields in Inspector (VernStats, Topics, Items, Event modifiers)
5. Press Play

## Architecture Overview

- **Pattern**: Singleton managers with event-driven communication
- **Namespaces**: `KBTV.Core`, `KBTV.Data`, `KBTV.Managers`, `KBTV.Callers`, `KBTV.UI`, `KBTV.Audio`
- **Bootstrap**: `GameBootstrap` creates all managers at runtime via reflection injection
- **Data**: ScriptableObjects for configuration (Topics, Items, Events, VernStats)

### File Structure
```
Assets/Scripts/
├── Runtime/
│   ├── Core/           # GamePhase, GameStateManager, GameBootstrap, SingletonMonoBehaviour<T>
│   ├── Data/           # Stat, VernStats, StatModifier, Item, ItemSlot
│   ├── Managers/       # TimeManager, LiveShowManager, ListenerManager, ItemManager
│   ├── Callers/        # Caller, CallerQueue, CallerGenerator, CallerScreeningManager, Topic
│   ├── UI/             # LiveShowUIManager, BasePanel, panels, components (see UI System)
│   └── Audio/          # AudioManager
└── Editor/
    └── GameSetup.cs    # One-click scene setup utility
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
  - **Events**:
    - `OnCallerAdded` - New caller joined incoming queue
    - `OnCallerRemoved` - Caller removed (rejected)
    - `OnCallerApproved` - Caller moved to on-hold (important for UI state updates)
    - `OnCallerDisconnected` - Caller hung up (patience ran out)
    - `OnCallerOnAir` - Caller went live
    - `OnCallerCompleted` - Call ended normally
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
| `BasePanel.cs` | Abstract base class for panels with singleton event subscriptions |
| `StatBarUI.cs` | Reusable stat bar component (label + fill bar + value text) |
| `HeaderBarUI.cs` | Top bar: Night #, phase, clock, remaining time, blinking LIVE indicator |
| `VernStatsPanel.cs` | Left panel: 7 stat bars + show quality display |
| `CallerCardUI.cs` | Reusable caller info card (detailed/compact/on-air variants) |
| `ScreeningPanel.cs` | Current screening caller display + approve/reject buttons |
| `OnAirPanel.cs` | On-air caller display + end call button |
| `CallerQueuePanel.cs` | Incoming and on-hold caller queue lists |
| `CallerQueueEntry.cs` | Single entry in caller queue list |
| `LiveShowUIManager.cs` | Main controller, creates Canvas hierarchy, coordinates all panels |

### UITheme Utilities

`UITheme.cs` provides helper methods for consistent UI creation:

| Method | Description |
|--------|-------------|
| `CreatePanel()` | Create styled panel with background |
| `CreateText()` | Create TextMeshPro text element |
| `CreateButton()` | Create styled button with label |
| `CreateProgressBar()` | Create fill bar with background |
| `CreateDivider()` | Create horizontal divider line |
| `AddVerticalLayout()` | Add VerticalLayoutGroup with standard settings |
| `AddHorizontalLayout()` | Add HorizontalLayoutGroup with standard settings |
| `AddLayoutElement()` | Add LayoutElement for size control |
| `GetPatienceColor()` | Get green/yellow/red color based on 0-1 value |
| `GetBlinkAlpha()` | Get alpha for blinking animation |

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
- `TimeManager.OnTick` - Clock updates

**CallerQueue events** (panels must subscribe to all relevant events):
- `OnCallerAdded`, `OnCallerRemoved`, `OnCallerDisconnected` - Queue list updates
- `OnCallerApproved` - On-hold list changed (critical for button state)
- `OnCallerOnAir`, `OnCallerCompleted` - On-air state changes

**Important**: When a UI element's state depends on data (e.g., button `interactable` based on queue count), ensure the panel subscribes to ALL events that can change that data. Missing event subscriptions cause UI to become stale.

## Audio System
**Files**: `Audio/AudioManager.cs`

The audio system is centralized through `AudioManager`, which subscribes to game events and plays appropriate sounds automatically.

### AudioManager

- **Singleton**: `AudioManager.Instance`
- **Audio Sources**: Creates 3 child AudioSource components:
  - `SFX_Source` - One-shot sound effects
  - `Music_Source` - Background music (looping)
  - `Ambience_Source` - Ambient sounds (looping)
- **Volume Controls**: Master, SFX, Music, Ambience (0-1 range)

### SFX Types

| Category | SFX Types |
|----------|-----------|
| **Phase Transitions** | `ShowStart`, `ShowEnd` |
| **Caller Events** | `CallerIncoming`, `CallerApproved`, `CallerRejected`, `CallerOnAir`, `CallerComplete`, `CallerDisconnect` |
| **UI Feedback** | `ButtonClick`, `ItemUsed`, `ItemEmpty` |
| **Alerts** | `LowStat`, `HighListeners` |
| **Ambience** | `PhoneRing`, `StaticBurst` |

### Event Subscriptions

AudioManager auto-plays sounds by subscribing to:
- `GameStateManager.OnPhaseChanged` - ShowStart/ShowEnd
- `CallerQueue.OnCallerAdded/OnAir/Completed/Disconnected` - Caller event sounds
- `ListenerManager.OnPeakReached` - HighListeners alert
- `ItemManager.OnItemUsed` - ItemUsed sound

### Public API

| Method | Description |
|--------|-------------|
| `PlaySFX(SFXType)` | Play a sound effect by type |
| `PlaySFX(SFXType, float pitchVariation)` | Play with random pitch offset |
| `PlayCallerDecision(bool approved)` | Approved or rejected sound |
| `PlayButtonClick()` | Generic button click |
| `PlayItemEmpty()` | Item empty error |
| `PlayStaticBurst()` | Radio static burst |

### UI Integration

UI panels call AudioManager directly for immediate feedback:
- `ScreeningPanel` - Approve/reject button clicks
- `OnAirPanel` - End call/put on air button clicks
- `ItemPanel` - Item use button clicks, empty item errors

## Key Patterns

### SingletonMonoBehaviour<T>

All singleton managers inherit from `SingletonMonoBehaviour<T>` (`Core/SingletonMonoBehaviour.cs`):

```csharp
public class MyManager : SingletonMonoBehaviour<MyManager>
{
    protected override void OnSingletonAwake() 
    {
        // Init code here instead of Awake()
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy(); // Important! Clears Instance
        // Unsubscribe from events
    }
}
```

Benefits:
- Eliminates duplicate singleton boilerplate
- Automatically handles duplicate destruction
- Clears `Instance` on destroy to prevent stale references

### BasePanel

UI panels that subscribe to singleton events inherit from `BasePanel` (`UI/BasePanel.cs`):

```csharp
public class MyPanel : BasePanel
{
    protected override bool DoSubscribe() 
    {
        // Subscribe to singletons, return true if successful
        var manager = SomeManager.Instance;
        if (manager == null) return false;
        manager.OnSomeEvent += HandleEvent;
        return true;
    }
    
    protected override void DoUnsubscribe() 
    {
        // Unsubscribe from events
    }
    
    protected override void UpdateDisplay() 
    {
        // Update visual state
    }
}
```

Benefits:
- Handles late-binding automatically (retries subscription in Update if Start failed)
- Standardizes subscribe/unsubscribe lifecycle
- Calls `UpdateDisplay()` after successful subscription

### Other Patterns

- **Events**: Loose coupling via C# events (`OnPhaseChanged`, `OnStatsChanged`, `OnCallerCompleted`, etc.)
- **SerializeField**: Private fields exposed to Inspector for configuration
- **RequireComponent**: Enforces component dependencies (e.g., LiveShowManager requires GameStateManager)

## Common Issues & Debugging

### UI Button Not Responding
1. **Check `interactable` state** - Button may be disabled based on game state
2. **Check event subscriptions** - Panel may not be subscribed to events that update button state
3. **Check raycast blocking** - Text/images above button may have `raycastTarget = true`
4. **Check button size** - LayoutGroups may give button zero dimensions

### ScriptableObject State Persisting Between Play Sessions
- ScriptableObjects (like VernStats) persist runtime changes in Editor
- Always reinitialize state in `Initialize()` methods, don't check for null
- Example: `_mood = new Stat("Mood", initialValue)` not `if (_mood == null) _mood = ...`

### Singleton Not Available in Start()
- Singletons may not be created yet when `Start()` runs
- **Solution**: Extend `BasePanel` which handles late-binding automatically
- If not using BasePanel: check for null in `Update()` and subscribe when available
- Call `UpdateDisplay()` immediately after late-subscribing

### Stat Bars Not Updating Visually
- `Image.Type.Filled` requires a sprite assigned to work
- Alternative: manually scale `RectTransform.sizeDelta.x` based on normalized value

## Testing

### Test Structure

Tests are located in `Assets/Scripts/Tests/Editor/` and use Unity's Edit Mode testing with NUnit.

```
Assets/Scripts/Tests/
└── Editor/
    ├── KBTV.Tests.Editor.asmdef  # Test assembly definition
    ├── StatTests.cs              # Tests for Stat class
    ├── VernStatsTests.cs         # Tests for VernStats ScriptableObject
    └── CallerTests.cs            # Tests for Caller class
```

### Running Tests

**In Unity Editor**:
1. Open `Window > General > Test Runner`
2. Select **EditMode** tab
3. Click **Run All** or click individual tests

**Command Line**:
```bash
Unity.exe -projectPath kbtv/kbtv -batchmode -runTests -testPlatform editmode -testResults Results.xml
```

### Test Coverage

| Class | File | Coverage |
|-------|------|----------|
| `Stat` | `StatTests.cs` | Constructor, SetValue, Modify, Reset, Normalized, IsEmpty/IsFull, events |
| `VernStats` | `VernStatsTests.cs` | Initialize, CalculateShowQuality, ApplyDecay, event firing |
| `Caller` | `CallerTests.cs` | Constructor, UpdateWaitTime, SetState, CalculateShowImpact, events |

### Writing Tests

**Edit Mode tests** (for pure logic classes):
```csharp
using NUnit.Framework;
using KBTV.Data;

public class MyTests
{
    [Test]
    public void MyMethod_DoesExpectedThing()
    {
        var stat = new Stat("Test", 50f);
        stat.Modify(10f);
        Assert.AreEqual(60f, stat.Value);
    }
}
```

**Testing ScriptableObjects**:
```csharp
[SetUp]
public void SetUp()
{
    _stats = ScriptableObject.CreateInstance<VernStats>();
}

[TearDown]
public void TearDown()
{
    Object.DestroyImmediate(_stats);
}
```

### Best Practices

- Test one behavior per test method
- Use descriptive test names: `MethodName_Condition_ExpectedResult`
- Create helper methods for repetitive object creation
- Always clean up ScriptableObjects in `TearDown`
- Test both success and failure cases
- Test event firing with callback counters

## Performance Targets
<!-- TBD -->

## Platform Requirements
<!-- TBD -->
