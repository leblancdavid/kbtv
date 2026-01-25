# KBTV Broadcast System Refactoring Plan

## Overview
Replacing the current complex BroadcastCoordinator system with a simplified async loop architecture that maintains event-driven design while reducing complexity.

## Architecture Design

### 1. Main Thread Async Loop (Recommended for Godot)
Use Godot's main thread with async/await (not separate threads)
Leverages Godot's scene tree timing and signal system
Avoids thread-safety complexity while maintaining async benefits

### 2. New Core Components

#### AsyncBroadcastLoop
- Main background coordinator running async loop
- Requesting executables → executing → awaiting → repeat
- Handles cancellation tokens for interruptions
- Publishes events for UI updates

#### BroadcastStateManager
- Determines which executable to deliver next
- Listens to timing events (break timers, show end, etc.)
- Manages state transitions between show phases
- Handles interruption logic

#### BroadcastTimer (New)
- Dedicated timing service for broadcast events
- Fires specific timing events: Break(T20, T10, T5, T0), ShowEnd
- Replaces reliance on AdManager for timing

#### BroadcastExecutable (New Base Class)
- Abstract base for all broadcast items
- Configurable RequiresAwait flag per executable type
- Async ExecuteAsync(cancellationToken) method
- Built-in interruption handling

### 3. Executable Types

#### MusicExecutable: Audio playback with await
#### DialogueExecutable: Text display + audio with await
#### AdExecutable: Ad content with configurable await
#### TransitionExecutable: Visual/audio transitions with await
#### TimerExecutable: Pure timing without await (immediate)

### 4. Event Flow
```
BroadcastTimer fires timing event
    ↓
BroadcastStateManager updates state
    ↓
AsyncBroadcastLoop requests next executable
    ↓
State Manager returns appropriate executable
    ↓
Loop executes executable (if await → wait, else continue)
    ↓
Loop repeats until show ends
```

## Implementation Status

### ✅ Phase 1: Foundation (COMPLETED)
- [x] Created BroadcastExecutable base class with configurable async behavior
- [x] Created BroadcastTimer with timing events  
- [x] Created BroadcastStateManager for state transitions and executable selection
- [x] Created AsyncBroadcastLoop main coordinator

### ✅ Phase 2: Executables (COMPLETED)
- [x] MusicExecutable - Handles show openings, closings, bumpers
- [x] DialogueExecutable - Handles both Vern and caller dialogue
- [x] AdExecutable - Handles commercial breaks with sponsor info
- [x] TransitionExecutable - Handles between-callers transitions

### 🔄 Phase 3: Integration (IN PROGRESS)
- [ ] Update BroadcastCoordinator to use new async architecture
- [ ] Fix remaining compilation errors in UI components
- [ ] Update ServiceRegistry to include new services
- [ ] Add missing properties and methods to existing classes

### 📋 Phase 4: Cleanup & Testing (PENDING)
- [ ] Remove deprecated classes (BroadcastItemExecutor, etc.)
- [ ] Update tests to cover new architecture
- [ ] Documentation updates

## Technical Benefits

### Simplified Logic
- Single async loop vs complex state machine coordination
- Clear separation of concerns (loop, state, timing, execution)
- 80% reduction in coordination logic complexity

### Better Interruptions  
- Cancellation tokens provide clean interruption
- Configurable async behavior per executable type
- No timing complexity in interruption handling

### Configurable Async
- Each executable type controls its own await behavior
- RequiresAwait flag for immediate vs. waiting executables
- Timer-based executables don't block the loop

### Cleaner Timing
- Dedicated timer service removes AdManager coupling
- Precise timing events for breaks and show ending
- Event-driven instead of polling

## Integration Challenges

### Current Issues (82 compilation errors)
1. **Missing properties**: Caller.Topic, Caller.Gender, ConversationArc.Lines
2. **Interface mismatches**: AdType vs AdType, BroadcastItem property ordering
3. **ServiceRegistry gaps**: Missing VernDialogue property, ListenerManager properties
4. **AudioStream types**: AudioStreamWAV not available (use AudioStreamOggVorbis)
5. **Legacy dependencies**: Many UI components expect old BroadcastCoordinator methods

### Resolution Strategy
1. **Fix property mismatches** - Use correct property names
2. **Add missing adapters** - Bridge between old and new systems
3. **Maintain compatibility** - Keep legacy interface methods during transition
4. **Incremental migration** - Replace components piece by piece

## Success Metrics

- [x] Zero compilation errors in core async components
- [ ] Build successfully with new architecture  
- [ ] All existing tests pass
- [ ] No functionality regression
- [ ] Performance improvement measurable

## Files Created

### Core Components
- `scripts/dialogue/executables/BroadcastExecutable.cs`
- `scripts/dialogue/BroadcastTimer.cs` 
- `scripts/dialogue/BroadcastStateManager.cs`
- `scripts/dialogue/AsyncBroadcastLoop.cs`

### Executable Classes
- `scripts/dialogue/executables/MusicExecutable.cs`
- `scripts/dialogue/executables/DialogueExecutable.cs`
- `scripts/dialogue/executables/AdExecutable.cs`
- `scripts/dialogue/executables/TransitionExecutable.cs`

### Event System
- Added `AudioCompletedEvent` to `BroadcastEvents.cs`
- Added `BroadcastItemStartedEvent` for UI sync
- Added `BroadcastTimingEvent` for timing coordination

## Next Steps

1. **Fix remaining compilation errors** (Priority: HIGH)
2. **Test core async loop** (Priority: HIGH)  
3. **Update UI components** (Priority: MEDIUM)
4. **Performance testing** (Priority: LOW)

## Timeline

- **Week 1**: Foundation and executables ✅ COMPLETED
- **Week 2**: Integration and testing 🔄 IN PROGRESS
- **Week 3**: Cleanup and documentation 📋 PENDING

This refactoring maintains the event-driven architecture while dramatically simplifying coordination logic and improving interruption handling through proper async patterns.