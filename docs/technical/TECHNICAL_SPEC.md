# KBTV - Technical Specification

## Getting Started

### Quick Setup (Recommended)
1. Open Godot 4.x
2. Import the project by selecting `project.godot`
3. Open the main scene (`scenes/Main.tscn`)
4. Press **F5** or click Play

The project is pre-configured with all necessary scenes and scripts.

### Scene Structure
The main scene (`scenes/Main.tscn`) contains:
- GameStateManager - Controls game phases
- TimeManager - Handles show timing
- ListenerManager - Tracks audience size
- EconomyManager - Manages money
- SaveManager - Handles persistence
- CallerGenerator - Spawns incoming callers
- UIManager - Main UI orchestrator
- VernStats - Resource with host character stats

## Architecture Overview

- **Pattern**: AutoInject dependency injection with event-driven communication
- **Namespaces**: `KBTV.Core`, `KBTV.Data`, `KBTV.Managers`, `KBTV.Callers`, `KBTV.Dialogue`, `KBTV.UI`, `KBTV.Audio`
- **Bootstrap**: Main scene (`scenes/Main.tscn`) creates ServiceProviderRoot with all managers at startup
- **Data**: Godot Resources for configuration (Topics, Items, VernStats)

### File Structure
```
scripts/
├── core/           # Core patterns (ServiceRegistry, EventAggregator, GameStateManager, GamePhase)
├── managers/       # Game managers (TimeManager, ListenerManager)
├── callers/        # Caller domain (Caller, CallerQueue, CallerGenerator, CallerRepository, Topic)
├── screening/      # Screening workflow (ScreeningController, ScreeningSession)
├── ui/             # UI components and managers (UIManager, panels, components)
├── economy/        # Money system (EconomyManager, IncomeCalculator)
├── persistence/    # Save/load (SaveManager, SaveData, ISaveable)
├── data/           # Data models (VernStats, Stat, StatModifier, StatType)
├── dialogue/       # Conversation arcs (ConversationManager, ArcRepository, ArcJsonParser)
├── audio/          # Audio configs (AudioManager, BumperConfig, TransitionMusicConfig)
├── ads/            # Ad system (AdData, AdType)
├── upgrades/       # Equipment upgrades (EquipmentConfig, EquipmentUpgrade, EquipmentType)
└── patterns/       # Result<T> type pattern

scenes/
├── Main.tscn              # Main game scene
└── ui/                    # UI panel scenes
    ├── TabContainerUI.tscn
    ├── ScreeningPanel.tscn
    ├── CallerPanel.tscn
    ├── CallerQueueItem.tscn
    ├── CallerTab.tscn
    ├── LiveShowHeader.tscn
    └── LiveShowFooter.tscn

assets/
├── dialogue/              # JSON conversation arc files
├── stats/                 # VernStats resource files
├── topics/                # Topic definition files
└── items/                 # Item definition files
```

## Core Systems

### Stats System
**Files**: `scripts/data/Stat.cs`, `scripts/data/VernStats.cs`, `scripts/data/StatModifier.cs`

Vern's stats system tracks his physical, emotional, and cognitive state during broadcasts. These stats drive the VIBE metric, which determines listener growth and show quality. The system uses sigmoid curves for smooth, natural-feeling transitions.

- **Dependencies (Decay-Only)**: Caffeine, Nicotine (0-100) - must be maintained, cause withdrawal
- **Physical Capacity**: Energy, Satiety (0-100) - capacity to perform
- **Emotional State**: Spirit (-50 to +50) - universal mood modifier
- **Cognitive Performance**: Alertness, Discernment, Focus (0-100) - performance quality
- **Long-Term**: Skepticism (0-100), Topic Affinity (-50 to +50 per topic)

The combination of all stats affects VIBE (Vibrancy, Interest, Broadcast Entertainment), which drives listener growth.

- `VernStats` (Resource) contains all stat tracking with decay rules and VIBE calculations
- `Stat` (individual stat with clamping and events)
- `StatModifier` Resources apply stat changes (items/events call `Apply(VernStats)`)
- `CalculateVibe()` returns composite metric using sigmoid functions for smooth transitions
- `ApplyDecay(deltaTime, multiplier)` degrades stats during live shows using configurable decay rates

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
**Files**: `scripts/data/Item.cs`, `scripts/managers/ItemManager.cs`, `scripts/ui/ItemPanel.cs`

- `Item` (Resource, extends StatModifier): Consumable items with additional settings
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
**Files**: `scripts/callers/Topic.cs`

- `Topic` (Resource): Defines show topics with screening rules
  - `ScreeningRules[]` - Array of rules callers must pass
  - `DeceptionRate` - Chance callers lie about their topic
  - `QualityMultiplier` - Bonus for on-topic callers
- `ScreeningRule` types:
  - `TopicMustMatch` - Caller's claimed topic must match
  - `LocationRequired` / `LocationBanned` - Geographic filters
  - `AreaCodeRequired` - Phone number filter
  - `MinimumLegitimacy` - Minimum credibility threshold

### Dialogue System
**Files**: `Dialogue/AsyncBroadcastLoop.cs`, `Dialogue/BroadcastStateManager.cs`, `Dialogue/BroadcastTimer.cs`, `Dialogue/ArcRepository.cs`, `Dialogue/ArcJsonParser.cs`, `Dialogue/BroadcastItem.cs`, `Dialogue/BroadcastExecutable.cs`, `Dialogue/VernStateCalculator.cs`, `Dialogue/Templates/VernDialogueTemplate.cs`

The dialogue system uses **async event-driven broadcast architecture** - executable-based broadcast items that run in the background with coordinated state management.

#### Core Concepts
- **AsyncBroadcastLoop**: Background coordinator that runs async execution loop
- **Broadcast Executables**: Self-contained items (music, dialogue, ads, transitions)
- **Event-Driven Communication**: Components subscribe to events instead of polling
- **State Management**: Coordinated state tracking through BroadcastStateManager
- **Cancellation Tokens**: Clean interruption handling for breaks and show ending

#### Key Classes
| Class | Description |
|-------|-------------|
| `AsyncBroadcastLoop` | Background coordinator running async execution loop |
| `BroadcastStateManager` | State management and executable factory |
| `BroadcastTimer` | Handles timing for breaks, show end, and ad breaks |
| `BroadcastItem` | Data structure for executable broadcast items |
| `BroadcastExecutable` | Base class for all broadcast executable types |
| `MusicExecutable` | Handles background music, intros, outros |
| `DialogueExecutable` | Manages Vern/caller dialogue with audio |
| `TransitionExecutable` | Between-callers and dead air filler content |
| `AdExecutable` | Commercial breaks with sponsor information |
| `ArcRepository` | Resource holding arc JSON files, provides arc selection |
| `ArcJsonParser` | Static utility for parsing arc JSON into ConversationArc objects |
| `VernStateCalculator` | Static utility for mood and discernment calculation |
| `VernDialogueTemplate` | Resource for Vern's broadcast lines (opening, filler, signoff) |

#### Mood Calculation
`VernStateCalculator.CalculateMood(VernStats)` returns `VernMood` based on priority order:
- **Tired**: Energy < 30
- **Energized**: Caffeine > 60 AND Energy > 60
- **Irritated**: Spirit < -10 OR Patience < 40
- **Amused**: Spirit > 20 AND LastCallerPositive
- **Gruff**: RecentBadCaller OR Spirit < 0
- **Focused**: Alertness > 60 AND Discernment > 50
- **Neutral**: Default state

#### Discernment Calculation
`VernStateCalculator.DetermineBeliefPath(discernment, legitimacy)` determines if Vern correctly reads the caller:
- Higher discernment = more likely to be Skeptical of Fake callers, Believing of Compelling callers
- Legitimacy modifies threshold (Compelling callers are easier to believe)

#### Async Broadcast Flow
- **Background Execution**: AsyncBroadcastLoop runs in Task without blocking main thread
- **Event Coordination**: BroadcastStateManager coordinates state and executables
- **Interrupt Handling**: Clean cancellation for breaks and show ending
- **Audio Synchronization**: Each executable handles its own audio playback
- **No Polling**: UI components react to events instead of constant polling

#### Event-Driven Communication
Components communicate through these events:
- `BroadcastItemStartedEvent` - New item begins with duration info
- `BroadcastEvent` - Item completed/interrupted/started
- `BroadcastInterruptionEvent` - Break/show ending interruptions
- `AudioCompletedEvent` - Audio playback finished
- `BroadcastTimingEvent` - Show timing (show end, break warnings)

#### Template Substitution
`DialogueSubstitution.Substitute(text, caller)` replaces placeholders:
- `{callerName}` - Caller's display name
- `{callerLocation}` - Caller's location
- `{topic}` - Current show topic

#### Events
- `ConversationStartedEvent` - New conversation began (event-driven)
- `AudioCompletedEvent` - Audio line finished playing
- `ConversationAdvancedEvent` - Advanced to next conversation line
- `ShowEndingWarning` - T-10s warning before show end
- `OnTransitionLineAvailable` - Transition line ready (ad breaks/show ending)
- `ConversationManager.OnBroadcastStateChanged` - Show open/close/filler state changes

## Data Assets

Located in `assets/` or defined as Resources in the editor:

| Type | Assets |
|------|--------|
| **Topics** | UFOs, Cryptids, GovernmentConspiracies, GhostsAndHauntings, AncientMysteries, TimeTravel, MenInBlack, OpenLines |
| **Items** | Coffee, Water, Sandwich, Whiskey, Cigarette |
| **Events** | GoodCaller, BadCaller, GreatCaller, Evidence, Debunked, TechnicalDifficulties |

## UI System
**Files**: `scripts/ui/UIManager.cs`, `scripts/ui/UITheme.cs`, and panel/component scripts

The Live Show UI uses scene-based panels instantiated at runtime.

### Architecture

| Script | Purpose |
|--------|---------|
| `UITheme.cs` | Colors, fonts, styling constants; helper methods for creating styled UI elements |
| `BasePanel.cs` | Abstract base class for panels with singleton event subscriptions |
| `PreShowUIManager.cs` | PreShow phase controller: topic selection + Start Show button |
| `TopicSelectionPanel.cs` | Grid of topic buttons with selection state and description |
| `LiveShowUIManager.cs` | LiveShow phase controller, creates Canvas hierarchy, coordinates all panels |
| `StatBarUI.cs` | Reusable stat bar component (label + fill bar + value text) |
| `HeaderBarUI.cs` | Top bar: Night #, phase, clock, remaining time, blinking LIVE indicator |
| `VernStatsPanel.cs` | Left panel: 7 stat bars + show quality display |
| `ItemPanel.cs` | Item buttons with hotkeys, quantities, cooldowns |
| `CallerCardUI.cs` | Reusable caller info card (detailed/compact/on-air variants) |
| `ScreeningPanel.cs` | Current screening caller display + approve/reject buttons |
| `OnAirPanel.cs` | On-air caller display + end call button |
| `CallerQueuePanel.cs` | Incoming and on-hold caller queue lists |
| `CallerQueueEntry.cs` | Single entry in caller queue list |
| `LiveShowPanel.cs` | Event-driven dialogue display with typewriter effect, speaker identification, and progress tracking |
| `ConversationDisplay.cs` | Event-driven conversation display component for UI panels |

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
┌─────────────────────────────────────────────────────────────────────┐
│ HeaderBar (Night, Phase, Clock, LIVE indicator, Listeners)          │
├───────────────┬─────────────────────────────────────────────────────┤
│ VernStats     │  ScreeningPanel        │  OnAirPanel               │
│ (7 stat bars) │  (current caller)      │  (active call)            │
│               ├────────────────────────┴─────────────────────────── │
│ Show Quality  │  CallerQueuePanel      │  ConversationPanel        │
│               │  (Incoming | On-Hold)  │  (dialogue + history)     │
├───────────────┼─────────────────────────────────────────────────────┤
│ ItemPanel     │                                                     │
│ (consumables) │                                                     │
└───────────────┴─────────────────────────────────────────────────────┘
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
**Files**: `Audio/AudioManager.cs`, `Audio/VoiceAudioService.cs`

The audio system is centralized through `AudioManager`, which subscribes to game events and plays appropriate sounds automatically. Voice audio is loaded asynchronously via `VoiceAudioService` using Godot ResourceLoader.

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

### VoiceAudioService

`VoiceAudioService` handles asynchronous loading and caching of voice audio clips via Godot ResourceLoader.

| Method | Description |
|--------|-------------|
| `PreloadConversationAsync()` | Load all clips for an arc on conversation start |
| `GetConversationClip()` | Get cached clip for a dialogue line |
| `GetBroadcastClipAsync()` | Load broadcast clip by ID |
| `UnloadCurrentConversation()` | Release cached clips on conversation end |

See [VOICE_AUDIO.md](VOICE_AUDIO.md) for detailed integration architecture.

## Key Patterns

### AutoInject Dependency Injection

KBTV uses Chickensoft AutoInject for dependency injection. Services register themselves using the `IAutoNode` mixin and dependencies are resolved automatically.

```csharp
// Services register themselves
[Meta(typeof(IAutoNode))]
public partial class GameStateManager : Node, IProvide<GameStateManager>
{
    public override void _Notification(int what) => this.Notify(what);
    
    GameStateManager IProvide<GameStateManager>.Value() => this;
    
    public void OnReady() => this.Provide();  // Make services available
}

// Components consume dependencies
[Meta(typeof(IAutoNode))]
public partial class MyComponent : Node, IDependent
{
    [Dependency] private GameStateManager GameState => DependOn<GameStateManager>();
}
```

**Key Classes:**
- `ServiceProviderRoot` - Root service provider for the scene tree
- All services implement `IAutoNode` with `IProvide<T>` interfaces

See [AUTOINJECT_PATTERN.md](AUTOINJECT_PATTERN.md) for complete documentation.

### BasePanel

UI panels that subscribe to singleton events inherit from `BasePanel` (`ui/BasePanel.cs`):

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

### AsyncBroadcastLoop Architecture

KBTV uses an **async event-driven AsyncBroadcastLoop** to manage the radio broadcast flow. This replaces pull-based polling with a more robust, scalable architecture.

**Key Benefits:**
- **Background async execution** - Broadcast runs without blocking main thread
- **Event-driven communication** - Components subscribe to events instead of polling
- **Executable-based architecture** - Each broadcast item is self-contained
- **Cancellation token support** - Clean interruption handling for breaks and show ending
- **No polling overhead** - UI components react to events instead of constant polling

**Core API:**
```csharp
// Start the broadcast loop (async)
public async Task StartBroadcastAsync(float showDuration = 600.0f);

// Stop the broadcast loop
public void StopBroadcast();

// Handle interruption from external events (breaks, show ending, etc.)
public void InterruptBroadcast(BroadcastInterruptionReason reason);

// Schedule a break at a specific time
public void ScheduleBreak(float breakTimeFromNow);

// Force start an ad break immediately
public void StartAdBreak();

// Check if currently in an ad break
public bool IsInAdBreak();
```

**Broadcast Events:**
Components subscribe to these events for updates:
- `BroadcastItemStartedEvent` - New item begins with duration info
- `BroadcastEvent` - Item completed/interrupted/started
- `BroadcastInterruptionEvent` - Break/show ending interruptions
- `AudioCompletedEvent` - Audio playback finished
- `BroadcastTimingEvent` - Show timing (show end, break warnings)

**Executable Types:**
| Type | Description |
|------|-------------|
| `MusicExecutable` | Background music, intros, outros |
| `DialogueExecutable` | Vern/caller dialogue with audio |
| `TransitionExecutable` | Between-callers, dead air filler |
| `AdExecutable` | Commercial breaks with sponsor info |

**Broadcast States:**
The AsyncBroadcastLoop uses `BroadcastStateManager` to track broadcast state:
```
ShowStarting → ShowOpening → Conversation → BetweenCallers → Conversation
                 ↓                    ↓
           (break starting)      (show ending)
                 ↓                    ↓
           AdBreak           ShowEnding
                 ↓                    ↓
           BreakReturn      ShowEnding
```

**Event-Driven Show Ending:**
The show ending follows the same event-driven pattern as ad breaks:
```
T-10s: TimeManager emits ShowEndingWarning event
    ↓
BroadcastCoordinator.OnShowEndingWarning()
    ↓
Sets _pendingTransitionLine with closing template
Fires OnTransitionLineAvailable
    ↓
ConversationDisplay.OnTransitionLineAvailable()
    ↓
Calls TryGetNextLine() → gets closing line
    ↓
Closing line displays
    ↓
OnLineCompleted() detects ShowEndingTransition
    ↓
OnLiveShowEnding() → state = ShowClosing
    ↓
T=0: TimeManager.EndShow() → ProcessEndOfShow()
```

### Event-Driven Conversation System

Conversations progress through event-driven state transitions synchronized with audio playback:

**Event Flow Pattern:**
```csharp
// 1. User puts caller on air → OnCallerOnAir()
// 2. BroadcastCoordinator publishes ConversationStartedEvent
// 3. ConversationDisplay receives event → requests first line
// 4. Audio plays → fires AudioCompletedEvent when done
// 5. ConversationDisplay receives event → calls OnLineCompleted()
// 6. BroadcastCoordinator advances to next line
// 7. Loop continues until conversation ends
```

**Event Types:**
- `ConversationStartedEvent` - Signals conversation initialization
- `AudioCompletedEvent` - Fired when audio line finishes playing
- `ConversationAdvancedEvent` - Signals advancement to next line

**Event Subscription Pattern:**
```csharp
// In ConversationDisplay.InitializeWithServices()
var eventBus = ServiceRegistry.Instance.EventBus;
eventBus.Subscribe<ConversationStartedEvent>(HandleConversationStarted);
eventBus.Subscribe<AudioCompletedEvent>(HandleAudioCompleted);
eventBus.Subscribe<ConversationAdvancedEvent>(HandleConversationAdvanced);
```

**Benefits:**
- Loose coupling between conversation logic and UI display
- Natural audio synchronization through event completion
- Predictable state transitions without timers
- Easy to extend with new event types

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

### Resource State Persisting Between Play Sessions
- Resources (like VernStats) persist runtime changes in Editor
- Always reinitialize state in `Initialize()` methods, don't check for null
- Example: `_mood = new Stat("Mood", initialValue)` not `if (_mood == null) _mood = ...`

### Singleton Not Available in Start()
- Singletons may not be created yet when `Start()` runs
- **Solution**: Use `ServiceRegistry.Instance.HasService<T>()` to check, then subscribe
- Or check for null and subscribe in `Update()` until available
- Call `UpdateDisplay()` immediately after late-subscribing

### Stat Bars Not Updating Visually
- `Image.Type.Filled` requires a sprite assigned to work
- Alternative: manually scale `RectTransform.sizeDelta.x` based on normalized value

## Testing

### Test Structure

Tests are located in `tests/` directory and use GdUnit4 testing framework.

```
tests/
├── unit/
│   ├── core/              # ServiceRegistry, EventAggregator, Result tests
│   ├── callers/           # Caller, CallerRepository tests
│   ├── screening/         # ScreeningController tests
│   └── ui/                # UI panel tests
└── integration/           # Cross-system tests
```

### Running Tests

**In Godot Editor**:
1. Open Godot Editor
2. Go to `Project > Tools > Run Tests`
3. Or use command line

**Command Line**:
```bash
godot --script addons/gdUnit4/bin/GdUnit4Cmd.gd --quit
```

### Test Coverage

| Class | File | Coverage |
|-------|------|----------|
| `Stat` | `tests/unit/core/StatTests.cs` | Constructor, SetValue, Modify, Reset, Normalized, IsEmpty/IsFull, events |
| `VernStats` | `tests/unit/core/VernStatsTests.cs` | Initialize, CalculateShowQuality, ApplyDecay, event firing |
| `Caller` | `tests/unit/callers/CallerTests.cs` | Constructor, UpdateWaitTime, SetState, CalculateShowImpact, events |
| `ServiceRegistry` | `tests/unit/core/ServiceRegistryTests.cs` | Service registration, retrieval, lifecycle |
| `EventAggregator` | `tests/unit/core/EventAggregatorTests.cs` | Subscribe, publish, weak reference handling |

### Writing Tests

**Unit tests**:
```csharp
using Godot;
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

**Testing Resources**:
```csharp
[Test]
public void VernStats_Initialize_SetsDefaults()
{
    var stats = new VernStats();
    stats.Initialize();
    Assert.AreEqual(50f, stats.Mood.Value);
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
