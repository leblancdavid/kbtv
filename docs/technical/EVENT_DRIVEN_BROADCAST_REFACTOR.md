# Event-Driven Broadcast System Refactor Plan

**Date**: 2026-01-21  
**Status**: In Progress  
**Version**: 1.0

## Executive Summary

**Goal**: Eliminate polling loops and race conditions by converting the broadcast system to pure event-driven architecture consistent with the EventBus pattern used throughout KBTV.

**Scope**: Major architectural refactor affecting ConversationDisplay, BroadcastCoordinator, BroadcastStateManager, and related UI components.

**Approach**: Incremental implementation with documented progress tracking and rollback points.

## Current Architecture Analysis

### Pull-Based Polling (Current Issues)
- `ConversationDisplay._Process()` calls `GetNextLine()` every frame (60fps polling)
- Race conditions between audio completion and polling timing
- Inconsistent with EventBus pattern used elsewhere
- Potential for missed state transitions

### Event-Driven Components (Working)
- Audio completion: `AudioCompletedEvent` → `ConversationDisplay.HandleAudioCompleted()`
- Show lifecycle: `ShowStartedEvent`, `ConversationStartedEvent`
- Ad coordination: Event-based break coordination

## Target Event-Driven Architecture

### Core Flow
```
1. BroadcastCoordinator publishes LineAvailableEvent(line)
2. ConversationDisplay receives → plays audio
3. Audio finishes → AudioCompletedEvent → ConversationDisplay publishes LineCompletedEvent
4. BroadcastStateManager receives → advances state → publishes StateChangedEvent
5. ConversationDisplay receives → requests next line via event
6. Loop continues...
```

### Event Definitions
```csharp
// New events to add
public class LineAvailableEvent : GameEvent {
    public BroadcastLine Line { get; }
}

public class LineCompletedEvent : GameEvent {
    public BroadcastLine CompletedLine { get; }
    public string SpeakerId { get; }
}

public class BroadcastStateChangedEvent : GameEvent {
    public BroadcastCoordinator.BroadcastState OldState { get; }
    public BroadcastCoordinator.BroadcastState NewState { get; }
}
```

## Implementation Plan

### Phase 1: Core Infrastructure (Foundation)

**Goal**: Establish event publishing/subscription infrastructure without breaking existing functionality.

**Status**: PENDING

**Tasks:**
1. [ ] Add new event classes to `ConversationEvents.cs`
2. [ ] Add event publishing to `BroadcastCoordinator` (LineAvailableEvent, StateChangedEvent)
3. [ ] Add event subscription setup in `ConversationDisplay`
4. [ ] Add `LineCompletedEvent` publishing in `ConversationDisplay.HandleAudioCompleted()`
5. [ ] Add event subscription in `BroadcastStateManager` (optional - keep existing OnLineCompleted() API for now)

**Files Modified:**
- `scripts/dialogue/ConversationEvents.cs` - Add new event classes
- `scripts/dialogue/BroadcastCoordinator.cs` - Add event publishing
- `scripts/dialogue/ConversationDisplay.cs` - Add event subscriptions
- `scripts/dialogue/BroadcastStateManager.cs` - Add LineCompletedEvent subscription

**Verification:**
- [ ] All events publish correctly
- [ ] Existing polling still works (backward compatibility)
- [ ] Console logs show event flow

---

### Phase 2: Remove Polling Dependencies

**Goal**: Gradually eliminate polling loops, starting with ConversationDisplay.

**Status**: PENDING

**Tasks:**
1. [ ] Add `LineAvailableEvent` and `StateChangedEvent` handlers in ConversationDisplay
2. [ ] Modify handlers to update display state instead of polling
3. [ ] Add logic to request next line via events when appropriate
4. [ ] Test that event-driven updates work alongside polling

**Files Modified:**
- `scripts/dialogue/ConversationDisplay.cs` - Add event handlers, modify display logic

**Verification:**
- [ ] Event-driven updates work correctly
- [ ] Polling still functional as fallback
- [ ] No visual glitches or timing issues

---

### Phase 3: State Manager Event-Driven

**Goal**: Make BroadcastStateManager respond to events instead of direct method calls.

**Status**: PENDING

**Tasks:**
1. [ ] Modify `BroadcastStateManager` to subscribe to `LineCompletedEvent`
2. [ ] Update state advancement logic to be event-driven
3. [ ] Remove direct `OnLineCompleted()` calls from BroadcastCoordinator
4. [ ] Ensure state changes publish `StateChangedEvent`

**Files Modified:**
- `scripts/dialogue/BroadcastStateManager.cs` - Event subscriptions and logic
- `scripts/dialogue/BroadcastCoordinator.cs` - Remove OnLineCompleted() API calls

**Verification:**
- [ ] State advances on LineCompletedEvent
- [ ] StateChangedEvent publishes correctly
- [ ] No state progression breaks

---

### Phase 4: Remove Polling API

**Goal**: Eliminate GetNextLine() polling completely.

**Status**: PENDING

**Tasks:**
1. [ ] Remove `ConversationDisplay._Process()` polling
2. [ ] Remove `BroadcastCoordinator.GetNextLine()` method
3. [ ] Update any remaining callers to use events
4. [ ] Ensure all line availability goes through events

**Files Modified:**
- `scripts/dialogue/ConversationDisplay.cs` - Remove _Process polling
- `scripts/dialogue/BroadcastCoordinator.cs` - Remove GetNextLine API
- `scripts/ui/LiveShowPanel.cs` - Update to event-driven approach

**Verification:**
- [ ] No polling loops remain
- [ ] All line display driven by events
- [ ] UI updates work correctly

---

### Phase 5: UI Integration Cleanup

**Goal**: Ensure all UI components work with event-driven system.

**Status**: PENDING

**Tasks:**
1. [ ] Update LiveShowPanel to work with event-driven line availability
2. [ ] Remove any remaining polling dependencies
3. [ ] Optimize event subscriptions/unsubscriptions
4. [ ] Performance testing and cleanup

**Files Modified:**
- `scripts/ui/LiveShowPanel.cs` - Event-driven updates
- Various UI components as needed

**Verification:**
- [ ] All UI updates work
- [ ] No performance issues
- [ ] Clean event subscriptions

---

## State Machine Flow (Unchanged)

The state machine logic remains the same - only the triggering mechanism changes from polling to events.

```
ShowOpening (4s) → AdvanceFromShowOpening()
    ├─ Has callers: Conversation
    └─ No callers: DeadAirFiller

Conversation (4s per line) → AdvanceFromConversation() 
    └─ When conversation ends: BetweenCallers or DeadAirFiller

BetweenCallers (4s) → AdvanceFromBetweenCallers()
    ├─ Has callers: Conversation  
    └─ No callers: DeadAirFiller

DeadAirFiller (8s) → AdvanceFromFiller()
    ├─ Has callers: Conversation
    └─ No callers: Stay in DeadAirFiller (cycle)
```

## Risk Assessment

### High Risk Areas
1. **UI Display Timing**: Event-driven updates must maintain visual continuity
2. **State Transition Logic**: Complex branching logic must work correctly
3. **Audio Synchronization**: Line display must sync with audio playback

### Mitigation Strategies
1. **Incremental Rollback**: Each phase maintains backward compatibility
2. **Comprehensive Testing**: Unit tests for each event flow
3. **Feature Flags**: Ability to switch between polling/event-driven modes

---

## Technical Debt / Deferred Work

### Refactor ConversationDisplay to Remove Polling Calls

**Issue**: Currently `ConversationDisplay` has multiple code paths that call `TryGetNextLine()` which bypass the event-driven architecture:

- `HandleShowStarted()` calls `TryGetNextLine()`
- `HandleConversationStarted()` calls `TryGetNextLine()`
- `HandleConversationAdvanced()` calls `TryGetNextLine()`

These create race conditions where the same line gets started twice (once via polling, once via events).

**Solution (Deferred)**:
- Remove `TryGetNextLine()` calls from all event handlers
- Rely solely on `LineAvailableEvent` for starting lines
- Use `OnTransitionLineAvailable()` only for explicit transition lines
- Move all line-fetching logic to event-driven flow through `BroadcastCoordinator`

**Status**: Deferred to after Phase 1 verification

---

## Session Log

| Date | Session | Phase | Work Completed | Status |
|------|---------|-------|----------------|--------|
| 2026-01-21 | 1 | Planning | Created this document | Complete |
| 2026-01-21 | 2 | Phase 1 | Added LineAvailableEvent, LineCompletedEvent, BroadcastStateChangedEvent classes; Updated AudioDialoguePlayer to always use silent audio; Updated ConversationDisplay to publish LineCompletedEvent and subscribe to new events; Updated BroadcastCoordinator to publish LineAvailableEvent and subscribe to LineCompletedEvent; Updated BroadcastStateManager to publish BroadcastStateChangedEvent | Complete |
| 2026-01-21 | 3 | Phase 1 | Implemented hybrid audio + timer fallback for consistent 4s pacing | Complete |
| 2026-01-21 | 5 | Phase 1 | Fixed transition line system - removed polling, made all transitions event-driven with proper timer cleanup | Complete |

## Implementation Details

### Hybrid Audio + Timer Fallback

To ensure consistent 4-second timing regardless of audio file status, the system uses a hybrid approach:

```csharp
// PlayLineAsync() flow in AudioDialoguePlayer.cs:
1. Try to load audio stream from silent_4sec.wav
2. If loaded successfully:
   - CURRENTLY FORCED TO USE TIMER: Audio file not providing proper timing
   - Use SceneTreeTimer for 4 seconds (temporary fix)
3. If load fails:
   - Log error: "Audio failed to load for {SpeakerId}, using 4s timer fallback"
   - Create SceneTreeTimer for 4 seconds
   - On timeout: Fire OnAudioFinished()

// TODO: Fix audio file timing issue
// The silent_4sec.wav file loads but AudioStreamPlayer.Finished
// fires immediately instead of after 4 seconds. Need to:
// - Verify audio file is valid 4-second WAV
// - Re-import or replace audio file
// - Then remove forced timer fallback
```

**Current Status**: Audio path is temporarily disabled - always uses timer fallback. Multiple timer issue fixed, ads now use timer fallback too.

### Transition Line System (Event-Driven)

**Problem Fixed**: Transition lines (break transitions, return from break, show endings) were using polling via `TryGetNextLine()`, causing race conditions and timer conflicts.

**Solution Implemented**:

**ConversationDisplay.cs:**
- Added `IsTransitionLine()` method to identify transition lines
- Modified `HandleLineAvailable()` to properly clean up timers for transition lines
- Changed `OnTransitionLineAvailable()` to not use polling (handled by event system)

**BroadcastCoordinator.cs:**
- Added `LineAvailableEvent` publishing for all transition lines (6 locations)
- Kept `OnTransitionLineAvailable` callbacks for backward compatibility
- Transition lines now flow through the same event-driven system as regular lines

**Result**: Transition lines start cleanly, previous timers are cancelled, no null `_currentLineId` issues, proper state advancement.

### Timer Cleanup Improvements

**AudioDialoguePlayer.cs:**
- Added `_currentTimer` tracking to prevent multiple timers
- Enhanced `Stop()` method to cancel active timers
- Proper timer reference cleanup in `OnTimerTimeout()`
- Ads now use timer fallback for consistent timing

**Benefits (when audio is fixed):**
- **Audio works**: Uses real audio timing when available
- **Fallback safe**: 4s timer ensures consistent pacing if audio fails
- **Debuggable**: Clear logging shows which path is taken
- **Future-proof**: When real voice audio is added, system uses it automatically

### Files Modified

| File | Changes |
|------|---------|
| `scripts/dialogue/AudioDialoguePlayer.cs` | Added StartTimerFallback() method, hybrid audio + timer approach |

### Expected Console Output

**When audio loads successfully:**
```
AudioDialoguePlayer.PlayLineAsync: Starting - SpeakerId=vern_opening_001
AudioDialoguePlayer.LoadAudioForLine: Using 4-second silent audio
AudioDialoguePlayer.GetSilentAudioFile: Loaded silent audio successfully
AudioDialoguePlayer.PlayLineAsync: Playing audio for vern_opening_001
[4 seconds pass - audio plays silently]
AudioDialoguePlayer.OnAudioFinished: Audio completed
```

**When audio fails (uses timer fallback):**
```
AudioDialoguePlayer.PlayLineAsync: Starting - SpeakerId=vern_opening_001
AudioDialoguePlayer.LoadAudioForLine: Using 4-second silent audio
AudioDialoguePlayer.GetSilentAudioFile: Failed to load - returning null
AudioDialoguePlayer.PlayLineAsync: Audio failed to load for vern_opening_001, using 4s timer fallback
[4 seconds pass - timer counts down]
AudioDialoguePlayer: Timer fallback completed after 4s
AudioDialoguePlayer.OnAudioFinished: Audio completed
```

## Audio File Issue (To Fix Later)

**Problem**: The silent audio file (`assets/audio/silence_4sec.wav`) loads successfully but `AudioStreamPlayer.Finished` event fires immediately instead of after 4 seconds. This breaks the audio timing path.

**Current Status**: Timer fallback implemented as workaround. All dialogue (including ads) uses 4-second SceneTreeTimer for consistent pacing.

**Investigation Steps** (Deferred):
1. Check file size: `ls -la assets/audio/silence_4sec.wav` (should be ~344KB for 4s 44.1kHz WAV mono)
2. Verify import file: `cat assets/audio/silence_4sec.wav.import`
3. Test in Godot editor - can you play it manually?
4. Check if AudioStreamPlayer.Playing property is true after Play()
5. Create new silent WAV file using external audio tool if needed

**Once Fixed**:
- Remove forced timer fallback in AudioDialoguePlayer.PlayLineAsync()
- Restore audio playback path
- Lines will use real audio timing when available

## Next Steps

1. **Test timing**: Verify lines advance at consistent 4-second pace
2. **Remove Guard Flag**: Once confirmed working, remove the _isProcessingLine guard flag
3. **Continue Phase 2**: Begin removing polling dependencies from ConversationDisplay
