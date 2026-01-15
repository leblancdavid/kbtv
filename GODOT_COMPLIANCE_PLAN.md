# Godot Compliance Plan for KBTV

This document outlines the remaining tasks to achieve full Godot compliance in the KBTV project. The project has core functionality (autoloads, signals, UI) but can be improved for better maintainability, performance, and Godot idiomatic practices.

## Current Status
- ✅ Autoloads for all managers
- ✅ Signals for phase changes
- ✅ Functional UI with tabs
- ✅ Clean C# compilation
- ✅ Basic save/load

## Remaining Tasks

### 1. Scene-Based UI (High Priority)
**Goal**: Convert programmatic UI creation to reusable .tscn scenes for visual editing and reusability.

**Tasks**:
- Create `/scenes/ui/LiveShowHeader.tscn`: Header with ON AIR, time, listeners.
- Create `/scenes/ui/CallerPanel.tscn`: Scrollable caller list.
- Create `/scenes/ui/ScreeningPanel.tscn`: Caller details + buttons.
- Create `/scenes/ui/TabContainerUI.tscn`: Tab layout.
- Refactor UIManagerBootstrap to instance scenes instead of AddChild() hierarchies.
- Test UI loading and interactions.

**Benefits**: 50% code reduction in UI classes, visual editing, easier collaboration.

**Effort**: 3-5 days.

### 2. Resource Optimization (Medium Priority)
**Goal**: Use Godot Resources (.tres files) for all data, with UIDs for stability.

**Tasks**:
- Convert `Topic.cs`, `ConversationArc.cs` to Resources with [Export] properties.
- Save as .tres files in `/assets/dialogue/`.
- Update loading code to use `ResourceLoader.Load()`.
- Enable UIDs in Godot 4.4+ for all resources/scripts.

**Benefits**: Better serialization, editor integration, no path breakage.

**Effort**: 2-3 days.

### 3. Signal Expansion (Medium Priority)
**Goal**: Replace all C# events with Godot signals for unified communication.

**Tasks**:
- Convert VernStats.OnStatsChanged to [Signal] and EmitSignal().
- Convert CallerQueue events (e.g., CallerAdded) to signals.
- Update all event handlers to use Connect() with Callable.
- Emit signals from managers for UI updates.

**Benefits**: Decoupled architecture, easier debugging.

**Effort**: 2-3 days.

### 4. Performance Optimization (Low Priority)
**Goal**: Improve runtime performance and responsiveness.

**Tasks**:
- Cache all node references in _Ready().
- Replace _Process polling with signal-based updates.
- Profile with Godot's debugger and optimize bottlenecks.
- Use async resource loading where applicable.

**Benefits**: Smoother gameplay, better on low-end hardware.

**Effort**: 1-2 days.

### 5. Code Cleanup and Documentation (Low Priority)
**Goal**: Polish code and docs for maintainability.

**Tasks**:
- Break long methods into smaller functions.
- Update AGENTS.md with final Godot practices.
- Add Godot-specific comments.
- Ensure consistent naming (PascalCase classes, snake_case methods).

**Benefits**: Easier future development.

**Effort**: 1 day.

## Implementation Strategy
- **Phase 1**: Scene-Based UI (start here for biggest impact).
- **Phase 2**: Resources and Signals.
- **Phase 3**: Performance and Cleanup.
- **Testing**: Full game loop after each phase.
- **Timeline**: 1-2 weeks total.

## Next Steps
1. Start with creating LiveShowHeader.tscn.
2. Update UIManagerBootstrap to use the scene.
3. Test and iterate.

This plan will make KBTV fully Godot-compliant and professional-grade.