# AsyncBroadcastLoop Architecture

## Overview

The AsyncBroadcastLoop is KBTV's core broadcast execution system that manages the radio show flow through asynchronous, event-driven coordination. It replaces pull-based polling with a more robust, scalable architecture that runs broadcast items in the background without blocking the main thread.

## Key Benefits

- **Background Async Execution** - Broadcast runs without blocking the main thread
- **Event-Driven Communication** - Components subscribe to events instead of constant polling
- **Executable-Based Architecture** - Each broadcast item is a self-contained executable
- **Cancellation Token Support** - Clean interruption handling for breaks and show ending
- **No Polling Overhead** - UI components react to events instead of `_Process()` polling
- **Scalable Design** - Easy to add new broadcast executables and event types

## Core Components

### AsyncBroadcastLoop
**File**: `scripts/dialogue/AsyncBroadcastLoop.cs`  
**Type**: Autoload Node

The main coordinator that runs an async execution loop:
```csharp
public partial class AsyncBroadcastLoop : Node
{
    public async Task StartBroadcastAsync(float showDuration = 600.0f);
    public void StopBroadcast();
    public void InterruptBroadcast(BroadcastInterruptionReason reason);
    public bool IsInAdBreak();
}
```

**Responsibilities:**
- Start/stop the async broadcast execution loop
- Request next executable from BroadcastStateManager
- Handle interruptions via cancellation tokens
- Manage executable lifecycle (add/remove child nodes)
- Publish events for UI coordination

### BroadcastStateManager
**File**: `scripts/dialogue/BroadcastStateManager.cs`  
**Type**: Plain class (internal to AsyncBroadcastLoop)

Manages broadcast state and creates executable instances:
```csharp
public class BroadcastStateManager
{
    public BroadcastExecutable? StartShow();
    public BroadcastExecutable? GetNextExecutable();
    public void UpdateStateAfterExecution(BroadcastExecutable executable);
    public AsyncBroadcastState CurrentState { get; }
}
```

**Responsibilities:**
- Track current broadcast state (ShowStarting, Conversation, AdBreak, etc.)
- Factory method for creating appropriate executables
- State transitions between different broadcast phases
- Handle interruptions and state recovery

### BroadcastTimer
**File**: `scripts/dialogue/BroadcastTimer.cs`  
**Type**: Node (child of AsyncBroadcastLoop)

Manages timing for breaks and show duration:
```csharp
public partial class BroadcastTimer : Node
{
    public void StartShow(float showDuration);
    public void ScheduleBreakWarnings(float breakTimeFromNow);
    public void StartAdBreak();
    public void StopShow();
}
```

**Responsibilities:**
- Track show remaining time
- Schedule and fire break warnings (20s, 10s, 5s, 0s)
- Publish `BroadcastTimingEvent` for time-based notifications
- Handle ad break timing and transitions

## Broadcast Executables

### BroadcastExecutable (Base Class)
**File**: `scripts/dialogue/BroadcastExecutable.cs`

Abstract base class for all broadcast content:
```csharp
public abstract partial class BroadcastExecutable : Node
{
    public string Id { get; }
    public BroadcastItemType Type { get; }
    public bool RequiresAwait { get; }
    
    public abstract Task ExecuteAsync(CancellationToken cancellationToken);
    public virtual void Initialize();
    public virtual void Cleanup();
}
```

### Concrete Executable Types

#### MusicExecutable
**Purpose**: Background music, show intros, outros, return bumpers
- Loads and plays audio files from `assets/audio/bumpers/` and voice directories
- Handles music transitions and mood-based selections
- Supports looped playback for background music

#### DialogueExecutable
**Purpose**: Vern and caller dialogue with synchronized audio
- Handles both Vern and caller dialogue items
- Manages speaker identification and conversation context
- Integrates with ArcRepository for conversation data
- Supports character-specific audio loading

#### TransitionExecutable
**Purpose**: Between-callers transitions, dead air filler
- Handles mood-based transition lines between callers
- Supports variable timing based on content type
- Manages dead air filler when no callers available

#### AdExecutable
**Purpose**: Commercial breaks with sponsor information
- Loads ad audio from `assets/audio/ads/`
- Displays sponsor information and ad text
- Integrates with revenue tracking system
- Supports sequential ad playback

## Event System

### Core Events

| Event Type | Purpose | Data |
|-------------|---------|-------|
| `BroadcastItemStartedEvent` | New item begins playback | BroadcastItem with duration |
| `BroadcastEvent` | Item completed/interrupted/started | EventType + ItemId + Item |
| `BroadcastInterruptionEvent` | Break/show ending interruptions | InterruptionReason |
| `BroadcastTimingEvent` | Show timing notifications | TimingType + TimeRemaining |
| `AudioCompletedEvent` | Audio playback finished | LineId + Speaker |

### Event Flow Example

```
1. Live show starts → BroadcastCoordinator.OnLiveShowStarted()
2. AsyncBroadcastLoop.StartBroadcastAsync() begins background execution
3. BroadcastStateManager.StartShow() → first executable
4. AsyncBroadcastLoop executes → publishes BroadcastItemStartedEvent
5. UI components subscribe → update displays
6. Executable completes → publishes BroadcastEvent (Completed)
7. AsyncBroadcastLoop requests next executable → repeat
8. Break interruption → BroadcastInterruptionEvent → AdBreak executable
9. Show ending → BroadcastStateManager handles show termination
```

## State Management

### AsyncBroadcastState Enum
```csharp
public enum AsyncBroadcastState
{
    Idle,
    ShowStarting,
    IntroMusic,
    ShowOpening,
    Conversation,
    BetweenCallers,
    DeadAir,
    AdBreak,
    ShowClosing,
    ShowEnding
}
```

### State Transitions
```
ShowStarting → ShowOpening → Conversation → BetweenCallers → Conversation
                 ↓                    ↓
           (break starting)      (show ending)
                 ↓                    ↓
           AdBreak           ShowEnding
                 ↓                    ↓
           BreakReturn      ShowEnding
```

## Integration Patterns

### Starting the Broadcast
```csharp
// In BroadcastCoordinator.OnLiveShowStarted()
public void OnLiveShowStarted()
{
    _isBroadcastActive = true;
    var showDuration = ServiceRegistry.Instance.TimeManager?.ShowDuration ?? 600.0f;
    
    // Start async broadcast loop in background
    _ = Task.Run(async () => {
        await _asyncLoop.StartBroadcastAsync(showDuration);
    });
}
```

### UI Event Subscription
```csharp
// In LiveShowPanel.InitializeWithServices()
private void InitializeWithServices()
{
    var eventBus = ServiceRegistry.Instance.EventBus;
    eventBus.Subscribe<BroadcastEvent>(HandleBroadcastEvent);
    eventBus.Subscribe<BroadcastItemStartedEvent>(HandleBroadcastItemStarted);
}

private void HandleBroadcastItemStarted(BroadcastItemStartedEvent @event)
{
    DisplayCurrentItem(@event.Item);
}
```

### Executable Creation
```csharp
// In BroadcastStateManager.CreateExecutable()
private BroadcastExecutable CreateExecutable(BroadcastItem item)
{
    return item.Type switch
    {
        BroadcastItemType.Music => new MusicExecutable(item),
        BroadcastItemType.VernLine or BroadcastItemType.CallerLine => new DialogueExecutable(item),
        BroadcastItemType.Ad => new AdExecutable(item),
        BroadcastItemType.Transition => new TransitionExecutable(item),
        _ => throw new NotSupportedException($"Unsupported broadcast item type: {item.Type}")
    };
}
```

## Performance Considerations

### Async Execution
- **Background Tasks**: All broadcast execution runs in separate tasks
- **Cancellation Support**: Clean interruption without race conditions
- **Memory Management**: Proper cleanup of executable resources
- **Error Handling**: Graceful recovery from execution failures

### Event Communication
- **No Polling**: UI components never poll for broadcast state
- **Loose Coupling**: Components communicate only through events
- **Event Cleanup**: Proper unsubscribe in `_ExitTree()` to prevent memory leaks
- **Thread Safety**: Events may fire from background threads

### Resource Management
- **Audio Streams**: Proper loading and unloading of audio resources
- **Node Lifecycle**: Executables are added/removed as children
- **Cancellation Tokens**: Timely disposal to prevent memory leaks

## Testing Guidelines

### Unit Tests
- **Async Methods**: Test with proper async/await patterns
- **Cancellation**: Verify cancellation tokens work correctly
- **Event Publishing**: Mock EventBus for event verification
- **State Transitions**: Verify BroadcastStateManager state changes

### Integration Tests
- **Full Flow**: Test complete broadcast from start to end
- **UI Integration**: Verify UI components receive and display events
- **Break Handling**: Test interruption and recovery scenarios
- **Audio Coordination**: Verify audio playback and completion events

## Comparison with Previous System

| Feature | Old System | AsyncBroadcastLoop |
|----------|-------------|-------------------|
| **Execution Model** | Pull-based polling | Background async execution |
| **UI Updates** | Constant polling in `_Process()` | Event-driven reactions |
| **State Management** | Manual state tracking | Coordinated state machine |
| **Error Handling** | Manual error checking | Structured exception handling |
| **Scalability** | Limited to main thread | Background task execution |
| **Maintainability** | Complex polling logic | Clean event-driven architecture |

## Files

```
scripts/dialogue/
├── AsyncBroadcastLoop.cs          # Main async coordinator (Autoload)
├── BroadcastStateManager.cs        # State management and factory
├── BroadcastTimer.cs              # Timing and break scheduling
├── BroadcastItem.cs              # Broadcast item data structure
├── BroadcastExecutable.cs          # Base class for executables
├── executables/
│   ├── MusicExecutable.cs         # Music and bumper handling
│   ├── DialogueExecutable.cs       # Dialogue with audio
│   ├── TransitionExecutable.cs     # Between-callers content
│   └── AdExecutable.cs           # Commercial breaks
├── BroadcastCoordinator.cs         # Legacy compatibility wrapper
└── events/
    ├── BroadcastEvent.cs           # Core broadcast events
    ├── BroadcastItemStartedEvent.cs # Item start events
    └── BroadcastInterruptionEvent.cs # Interruption events
```

This architecture provides a robust, scalable foundation for KBTV's broadcast system that can easily accommodate new features and content types while maintaining clean separation of concerns and event-driven communication patterns.