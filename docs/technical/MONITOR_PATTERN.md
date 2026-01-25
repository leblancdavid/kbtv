# Monitor Pattern

Domain-specific monitors that handle state updates in the game loop.

## Overview

Monitors are Godot Nodes that run in the scene tree's `_Process()` loop, updating their domain's state values each frame and triggering state-driven side effects. Each monitor handles ONE domain (Callers, VernStats, etc.).

## Pattern Structure

```
scripts/monitors/
├── DomainMonitor.cs         # Abstract base class
├── CallerMonitor.cs         # Handles caller patience/wait time
└── VernStatsMonitor.cs      # Handles Vern's stat decay
```

## Base Class

```csharp
public abstract partial class DomainMonitor : Node
{
    protected ICallerRepository? _repository;

    public override void _Ready()
    {
        if (ServiceRegistry.IsInitialized)
        {
            _repository = ServiceRegistry.Instance.CallerRepository;
        }
    }

    public override void _Process(double delta)
    {
        if (_repository == null) return;
        OnUpdate((float)delta);
    }

    protected abstract void OnUpdate(float deltaTime);
}
```

## Responsibilities

Monitors are responsible for:

| Responsibility | Examples |
|----------------|----------|
| **State updates** | Wait time accumulation, stat decay, patience drain |
| **Side effects** | Disconnection when patience runs out, stat depletion events |

Monitors should NOT handle:

| Excluded | Reason |
|----------|--------|
| UI updates | UI polls or observes state changes |
| Persistence | SaveManager handles save/load |
| Business logic | Screening decisions, approvals stay in their domains |

## Creating a New Monitor

### 1. Create the monitor class

```csharp
// scripts/monitors/ScreeningMonitor.cs
public partial class ScreeningMonitor : DomainMonitor
{
    protected override void OnUpdate(float deltaTime)
    {
        var controller = ServiceRegistry.Instance.ScreeningController;
        if (controller?.IsActive == true)
        {
            controller.Update(deltaTime);
        }
    }
}
```

### 2. Add to the scene

Add the monitor as a child node in `scenes/Game.tscn`:

```tscn
[gd_scene load_steps=3 format=3]

[ext_resource type="Script" uid="uid://ciyhpovkok5te" path="res://scripts/Main.cs" id="1"]
[ext_resource type="Script" uid="uid://b3ihb2axyu6ad" path="res://scripts/monitors/CallerMonitor.cs" id="2"]

[node name="Main" type="Node2D"]
script = ExtResource("1")

[node name="CallerMonitor" type="Node" parent="."]
script = ExtResource("2")
```

### 3. Add unit tests

```csharp
// tests/unit/monitors/ScreeningMonitorTests.cs
public class ScreeningMonitorTests : KBTVTestClass
{
    [Test]
    public void Monitor_UpdatesScreening_WhenActive()
    {
        var monitor = new ScreeningMonitor();
        monitor._Ready();
        monitor._Process(0.016f);
        // Assert screening controller was updated
    }
}
```

## Scene Setup

Monitors are added to `scenes/Game.tscn`:

```tscn
[gd_scene load_steps=4 format=3]

[ext_resource type="Script" uid="uid://ciyhpovkok5te" path="res://scripts/Main.cs" id="1"]
[ext_resource type="Script" uid="uid://b3ihb2axyu6ad" path="res://scripts/monitors/CallerMonitor.cs" id="2"]
[ext_resource type="Script" uid="uid://do111yirun4wa" path="res://scripts/monitors/VernStatsMonitor.cs" id="3"]

[node name="Main" type="Node2D"]
script = ExtResource("1")

[node name="CallerMonitor" type="Node" parent="."]
script = ExtResource("2")

[node name="VernStatsMonitor" type="Node" parent="."]
script = ExtResource("3")

[node name="Camera2D" type="Camera2D" parent="."]
position = Vector2(960, 540)
```

## Existing Monitors

### CallerMonitor

**Domain:** Callers

**State Updates:**
- Incoming callers: Accumulate wait time each frame
- Screening caller: Drain screening patience at 50% rate
- OnHold/OnAir callers: No wait time accumulation

**Side Effects:**
- Triggers `OnDisconnected` event when patience runs out
- Repository observers notified of state changes

### VernStatsMonitor

**Domain:** Vern's stats

**State Updates:**
- Dependencies (caffeine, nicotine) decay at configured rates
- Physical stats (energy, satiety) decay each frame
- Cognitive stats (alertness, focus, patience) decay

**Side Effects:**
- `VernStats` emits `StatsChanged`, `VibeChanged`, `MoodTypeChanged`
- Low dependency levels affect other stat multipliers (see VERN_STATS.md)

### ScreeningMonitor

**Domain:** Caller screening

**State Updates:**
- Active screening caller: Accumulate revelations each frame
- Property revelations progress toward screening completion
- Patience drain during active screening

**Side Effects:**
- `ScreeningController` updates progress and revelation state
- Triggers screening completion when all properties revealed
- UI updates screening progress bars and buttons

## Monitoring Async Systems

### AsyncBroadcastLoop Monitoring

While the Monitor Pattern focuses on domain-specific state updates, async systems like `AsyncBroadcastLoop` require a different approach:

**Key Differences:**
- **Background execution** - AsyncBroadcastLoop runs in background threads/tasks
- **Event-driven** - State changes communicated via events, not direct observation
- **Cancellation tokens** - Monitors need to handle interruption scenarios
- **Non-deterministic timing** - Frame-based timing doesn't apply to async operations

### Monitoring Guidelines for Async Systems

#### 1. Event Subscription Pattern
Instead of polling state, subscribe to system events:

```csharp
// In monitor _Ready()
public override void _Ready()
{
    base._Ready();
    
    var eventBus = ServiceRegistry.Instance.EventBus;
    eventBus.Subscribe<BroadcastEvent>(HandleBroadcastEvent);
    eventBus.Subscribe<BroadcastItemStartedEvent>(HandleItemStarted);
}

// Handle event-driven state changes
private void HandleBroadcastEvent(BroadcastEvent @event)
{
    // Update monitor state based on event
    _broadcastActive = @event.Type != BroadcastEventType.ShowEnded;
}
```

#### 2. State Tracking via Events
Track async system state through event aggregation:

```csharp
public class BroadcastMonitor : DomainMonitor
{
    private bool _isBroadcastActive = false;
    private BroadcastItemType _currentItemType = BroadcastItemType.Music;
    
    protected override void OnUpdate(float deltaTime)
    {
        // Handle timeout-based monitoring if needed
        if (_isBroadcastActive)
        {
            // Any frame-based logic for broadcast state
        }
    }
    
    private void HandleBroadcastItemStarted(BroadcastItemStartedEvent @event)
    {
        _currentItemType = @event.Item.Type;
        // React to new item starting
    }
    
    private void HandleBroadcastEnded(BroadcastEvent @event)
    {
        _isBroadcastActive = false;
        // React to broadcast ending
    }
}
```

#### 3. Async Operation Coordination
Coordinate with async systems for complex scenarios:

```csharp
// Example: Monitor that tracks broadcast health
protected override void OnUpdate(float deltaTime)
{
    var asyncLoop = ServiceRegistry.Instance.AsyncBroadcastLoop;
    if (asyncLoop?.IsRunning == true)
    {
        // Check broadcast health via event history or state
        _timeSinceLastEvent += deltaTime;
        
        if (_timeSinceLastEvent > HEALTH_CHECK_THRESHOLD)
        {
            GD.PrintWarn("BroadcastLoop health check: No events received recently");
            _timeSinceLastEvent = 0;
        }
    }
}
```

### Best Practices for Async Monitoring

| Practice | Description | Example |
|-----------|-------------|---------|
| **Event over Polling** | Subscribe to events instead of checking state | `Subscribe<BroadcastEvent>()` vs `asyncLoop.IsRunning` |
| **Thread Safety** | Assume events from background threads | Use proper synchronization if sharing state |
| **Graceful Degradation** | Handle async system not being available | Check `ServiceRegistry.Instance.AsyncBroadcastLoop` for null |
| **Event Cleanup** | Unsubscribe from events in `_ExitTree()` | Prevent memory leaks with proper cleanup |

### Integration with Existing Monitors

Async monitoring can complement existing domain monitors:

```csharp
public class ConversationMonitor : DomainMonitor
{
    // Traditional domain monitoring
    protected override void OnUpdate(float deltaTime)
    {
        // Monitor caller patience, Vern stats, etc.
    }
    
    // Async system monitoring via events
    public override void _Ready()
    {
        base._Ready();
        var eventBus = ServiceRegistry.Instance.EventBus;
        eventBus.Subscribe<BroadcastEvent>(HandleBroadcastState);
    }
    
    private void HandleBroadcastState(BroadcastEvent @event)
    {
        // Coordinate between domain state and broadcast state
        switch (@event.Type)
        {
            case BroadcastItemType.Conversation:
                // Conversation-specific monitoring logic
                break;
            case BroadcastItemType.Ad:
                // Ad break-specific monitoring
                break;
        }
    }
}
```

## Testing Guidelines

Each monitor should have:

1. **Null repository test:** Monitor doesn't throw when repository is null
2. **Empty state test:** Monitor handles empty state gracefully
3. **Update test:** Verify state is updated with delta time
4. **Side effect test:** Verify side effects (events, state changes) occur correctly
5. **Multiple entities test:** Monitor handles multiple entities correctly
6. **Async integration test:** Verify event subscription and handling works correctly

## Performance Considerations

- Monitors run every frame - keep update logic minimal
- Use `ServiceRegistry` for service access, avoid scene tree lookups
- Consider `ShouldUpdate` property if monitor needs to be enabled/disabled
- **Async events** - Minimize work in event handlers to avoid blocking background threads
- **Event subscription overhead** - Only subscribe to necessary events, unsubscribe in cleanup
