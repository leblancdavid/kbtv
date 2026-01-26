# Service Dependencies and Autoload Order

This document defines the service dependency hierarchy and autoload order for the KBTV project. Services are loaded in specific dependency levels to ensure all required dependencies are available during initialization.

## Service Architecture Overview

KBTV uses a **hybrid service registration approach**:

1. **Core Services (Plain Classes)**: Created by ServiceRegistry in `RegisterCoreServices()`
2. **Autoload Services**: Register themselves in `_Ready()` using `RegisterSelf()`

**Key Principle**: Services must only access dependencies that are loaded before them in the autoload order. No deferred initialization is used.

## Core Services (Created by ServiceRegistry)

These services are created first and available to all autoload services:

- `CallerRepository` - Manages caller data and state
- `ScreeningController` - Handles caller screening workflow  
- `ArcRepository` - Stores conversation arcs
- `EventBus` - Global event bus for inter-system communication
- `AudioDialoguePlayer` - Handles dialogue audio playback
- `AdManager` - Manages advertising system

## Autoload Service Dependency Levels

### Level 0: Foundation Services
**Must load first** - No dependencies except ServiceRegistry

```
1. ServiceRegistry
```

### Level 1: Core Managers
**Simple services** - Depend only on ServiceRegistry and core services

```
2. SaveManager        - Persists game state
3. EconomyManager     - Manages money/economy
4. GlobalTransitionManager - Handles fade transitions
5. TranscriptRepository - Stores broadcast transcripts
```

### Level 2: Game State & Timing
**Core game systems** - Depend on Level 0-1 services

```
6. TimeManager       - Manages show timing and persistence
7. GameStateManager  - Controls game phases and state
```

### Level 3: UI Management
**UI systems** - Depend on core state systems

```
8. UIManager         - Main UI orchestrator
9. PostShowUIManager - Post-show UI layer
10. TabContainerManager - Tab container management
```

### Level 4: Game Systems
**Game logic systems** - Depend on UI and state systems

```
11. ListenerManager  - Manages audience size
12. PreShowUIManager - Pre-show UI layer
```

### Level 5: Content Generation
**Content creation systems** - Depend on game systems

```
13. CallerGenerator - Spawns incoming callers
```

### Level 6: Broadcast System
**Final layer** - Depends on all other systems

```
14. AsyncBroadcastLoop - Async broadcast execution
15. BroadcastCoordinator - Legacy broadcast coordination
```

## Service Dependency Details

### GameStateManager Dependencies
- **TimeManager** - Connects to ShowEnded signal
- **AdManager** - For live show initialization
- **BroadcastCoordinator** - Notifies of live show start
- **CallerRepository** - Clears callers on show end
- **ListenerManager** - Gets peak listeners on show end
- **EconomyManager** - Adds income on show end
- **SaveManager** - Saves game on show end
- **UIManager** - Shows post-show layer

### TimeManager Dependencies
- **SaveManager** - Registers as saveable

### UIManager Dependencies
- **GameStateManager** - Listens to PhaseChanged signal
- **GlobalTransitionManager** - For layer transitions

### ListenerManager Dependencies
- **CallerRepository** - Gets caller data for listener calculations
- **GameStateManager** - Connects to PhaseChanged, gets VernStats
- **TimeManager** - Connects to Tick signal

### PreShowUIManager Dependencies
- **UIManager** - Registers PreShow layer
- **GameStateManager** - Sets topic, checks CanStartLiveShow
- **SaveManager** - Loads/saves show duration
- **TimeManager** - Sets show duration

### CallerGenerator Dependencies
- **CallerRepository** - Adds generated callers
- **GameStateManager** - Gets selected topic, connects to PhaseChanged
- **ArcRepository** - Gets arcs for selected topic

### AsyncBroadcastLoop Dependencies
- **CallerRepository** - For broadcast state management
- **ArcRepository** - For broadcast state management
- **EventBus** - For publishing events

### BroadcastCoordinator Dependencies
- **AsyncBroadcastLoop** - For broadcast execution
- **CallerRepository** - For coordination
- **EventBus** - For event subscriptions
- **AdManager** - For break event subscriptions
- **TimeManager** - Gets show duration

## Adding New Services

When adding a new service to the project:

1. **Identify Dependencies**: List all services the new service requires
2. **Determine Level**: Place the service at the appropriate dependency level
3. **Update Autoload Order**: Add the service to `project.godot` in the correct position
4. **Update This Document**: Add the service to the dependency details section

### Service Addition Checklist

- [ ] Service uses `[GlobalClass]` attribute for autoload
- [ ] Service calls `ServiceRegistry.Instance.RegisterSelf<T>(this)` in `_Ready()`
- [ ] All dependencies are loaded before the new service in autoload order
- [ ] Updated `project.godot` autoload section
- [ ] Updated this documentation
- [ ] Test that all services initialize without errors

## Initialization Rules

### Direct Initialization Only
Services MUST access their dependencies directly in `_Ready()`:

```csharp
public override void _Ready()
{
    ServiceRegistry.Instance.RegisterSelf<YourService>(this);
    
    // Direct access - dependencies are guaranteed to be loaded
    _dependency = ServiceRegistry.Instance.YourDependency;
    if (_dependency == null)
    {
        GD.PrintErr("YourService: Required dependency not available");
        return;
    }
    
    // Initialize service
}
```

### No Deferred Initialization
**Do NOT use `CallDeferred()` for service dependency access**. The autoload order ensures all dependencies are available when `_Ready()` is called.

### Error Handling
Services should check for null dependencies and log errors:

```csharp
if (ServiceRegistry.Instance.YourDependency == null)
{
    GD.PrintErr("YourService: Required service YourDependency not available - check autoload order");
    return;
}
```

## Troubleshooting

### Service Not Found Errors
If you see "Service not found" errors:

1. Check the autoload order in `project.godot`
2. Verify the service is properly registered with `RegisterSelf()`
3. Ensure all dependencies are loaded before the service
4. Check that core services are created in `ServiceRegistry.RegisterCoreServices()`

### Circular Dependencies
The current dependency structure has **no circular dependencies**. If you encounter circular dependencies during development:

1. Refactor to remove the circular dependency
2. Consider using events via EventBus for loose coupling
3. Split the service into smaller, more focused services

## File References

- `project.godot` - Autoload configuration
- `scripts/core/ServiceRegistry.cs` - Core service registration
- `docs/technical/SERVICE_DEPENDENCIES.md` - This document