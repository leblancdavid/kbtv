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

## Testing Guidelines

Each monitor should have:

1. **Null repository test:** Monitor doesn't throw when repository is null
2. **Empty state test:** Monitor handles empty state gracefully
3. **Update test:** Verify state is updated with delta time
4. **Side effect test:** Verify side effects (events, state changes) occur correctly
5. **Multiple entities test:** Monitor handles multiple entities correctly

## Performance Considerations

- Monitors run every frame - keep update logic minimal
- Use `ServiceRegistry` for service access, avoid scene tree lookups
- Consider `ShouldUpdate` property if monitor needs to be enabled/disabled
