# KBTV - AutoInject Dependency Injection Pattern

## Overview

KBTV uses Chickensoft AutoInject for dependency injection, providing better testability, cleaner code, and Godot best practices. This replaces the previous Service Registry approach with a tree-scoped, automatic resolution system.

## Core Concepts

### Service Registration
Services register themselves using the `IAutoNode` mixin:

```csharp
[Meta(typeof(IAutoNode))]
public partial class GameStateManager : Node,
    IProvide<GameStateManager>, IProvide<TimeManager>, // Services this node provides
    IDependent  // If this node consumes dependencies
{
    public override void _Notification(int what) => this.Notify(what);
    
    // Dependencies this node consumes
    [Dependency] private IService Service => DependOn<IService>();
    
    // Services this node provides
    GameStateManager IProvide<GameStateManager>.Value() => this;
    TimeManager IProvide<TimeManager>.Value() => _timeManager;
    
    public void OnReady() => this.Provide();  // Make services available
    public void OnResolved() { /* Called when dependencies are ready */ }
}
```

### Key Principles
- **Use `IAutoNode` mixin**: Applies IAutoOn, IAutoConnect, IProvider, IDependent
- **Providers call `this.Provide()` in `OnReady()`**: Makes services available to descendants
- **Dependents use `[Dependency]` + `DependOn<T>()`**: For lazy dependency resolution
- **ServiceProviderRoot as root provider**: Provides all core services to the scene tree
- **Dependencies resolved before first frame**: `OnResolved()` called before `_Process()`

### Service Architecture
- **ServiceProviderRoot**: Pure provider, instantiates and provides all core services
- **Manager nodes**: Both providers and dependents (GameStateManager, EconomyManager, etc.)
- **UI Components**: Dependents that consume services via `[Dependency]`

### Benefits over Service Registry
- ✅ **Tree-scoped dependencies**: Services available to subtree, overridable by descendants
- ✅ **Automatic resolution**: No manual registration required
- ✅ **Test-friendly**: Easy to fake dependencies with `FakeDependency()`
- ✅ **Guaranteed timing**: Dependencies available before frame processing
- ✅ **Clean separation**: Providers vs consumers clearly defined

### Required Packages
- Chickensoft.GodotNodeInterfaces
- Chickensoft.Introspection
- Chickensoft.Introspection.Generator
- Chickensoft.AutoInject
- Chickensoft.AutoInject.Analyzers

### Files
- `scripts/core/ServiceProviderRoot.cs` - Root service provider
- All service nodes use `IAutoNode` pattern with `[Meta(typeof(IAutoNode))]`
- `scenes/Main.tscn` - ServiceProviderRoot added as child node

## Migration from Service Registry
The project has been fully migrated to AutoInject pattern. Legacy Service Registry code has been removed in favor of this approach.