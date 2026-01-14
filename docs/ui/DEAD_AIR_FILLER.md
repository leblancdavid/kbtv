# Broadcast Flow & Dead Air Filler

## Overview

The broadcast flow system manages Vern's dialogue during transitions in the LiveShow phase:

1. **Show Opening** - Vern's intro line when LiveShow begins
2. **Dead Air Filler** - Vern's monologue when no caller is on air
3. **Between Callers** - Transition lines when auto-advancing to next caller
4. **Show Closing** - Vern's outro line when LiveShow ends

These features maintain broadcast atmosphere, prevent awkward silence, and make the show feel alive.

## Broadcast Flow

### Show Opening
When the game transitions to LiveShow phase, Vern delivers an opening line before anything else happens. After the opening completes, dead air filler begins if no caller is on air.

### Show Closing
When leaving LiveShow phase, Vern delivers a closing line to wrap up the show.

### Between Callers
When a conversation ends and there are callers on hold, Vern delivers a transition line before auto-advancing to the next caller. This creates a natural flow between calls.

## Dead Air Filler

### Behavior Specification

### State Transitions

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         CONVERSATION ENDS                                │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │    Callers on hold?           │
                    └───────────────────────────────┘
                           │                │
                      Yes  │                │  No
                           ▼                ▼
               ┌───────────────────┐  ┌───────────────────┐
               │ Play between-     │  │ Start dead air    │
               │ callers line,     │  │ filler mode       │
               │ then put on air   │  │                   │
               └───────────────────┘  └───────────────────┘
                                              │
                    ┌─────────────────────────┤
                    │                         │
                    ▼                         ▼
         ┌───────────────────┐    ┌───────────────────────────┐
         │ Display filler    │    │ Timer expires (~8s)       │
         │ line with         │    │ → Cycle to next           │
         │ typewriter effect │    │   filler line             │
         └───────────────────┘    └───────────────────────────┘
                    │                         │
                    └──────────┬──────────────┘
                               │
                               ▼
                    ┌───────────────────────────┐
                    │ Filler line ends AND      │
                    │ callers on hold?          │
                    └───────────────────────────┘
                               │
                          Yes  │
                               ▼
                    ┌───────────────────────────┐
                    │ Stop filler, put next     │
                    │ caller on air             │
                    └───────────────────────────┘
```

### Auto-Advance Logic

When Vern finishes speaking (filler line, broadcast line, or show opening), the system automatically checks for on-hold callers:

1. **Show opening ends** → Check `CallerQueue.HasOnHoldCallers` → Put caller on air OR start filler
2. **Filler line ends** → Check `CallerQueue.HasOnHoldCallers` → Put caller on air OR continue filler cycle
3. **Conversation ends** → Check `CallerQueue.HasOnHoldCallers` → Play between-callers transition OR start filler
4. **Caller approved while nothing playing** → Put caller on air immediately

This eliminates the need for manual "Put On Air" actions - callers flow on air automatically when Vern is ready.

### Scenarios

| Scenario | Behavior |
|----------|----------|
| LiveShow starts | Show opening plays, then check for on-hold callers or start filler |
| Show opening ends, callers on hold | Put next caller on air automatically |
| Show opening ends, no callers on hold | Start dead air filler (Vern monologue) |
| Conversation ends, callers on hold | Between-callers transition, then next caller on air |
| Conversation ends, no callers on hold | Start dead air filler |
| Filler line ends, callers on hold | Stop filler, put next caller on air automatically |
| Filler line ends, no callers on hold | Wait for cycle interval (~8s), then show next filler line |
| Caller approved while nothing playing | Put caller on air immediately |
| Caller goes on air during filler | Stop filler immediately |
| LiveShow ends | Show closing plays |

### Timing

- **Filler line duration**: Calculated same as conversation lines (`baseDelay + text.Length * perCharDelay`)
- **Cycle interval**: ~8 seconds between filler lines (configurable via `_deadAirCycleInterval`)
- **Transition delay**: When caller becomes available, wait for current filler line to finish naturally

## Implementation Details

### Files Modified

| File | Changes |
|------|---------|
| `CallerQueue.cs` | No changes needed - `OnCallerApproved` event already exists |
| `ConversationManager.cs` | Add dead air filler state, methods, and auto-advance logic |
| `ConversationPanel.cs` | Subscribe to filler events, display Vern monologue mode |

### ConversationManager.cs Changes

#### New Fields

```csharp
// Dead air filler state
private bool _isPlayingDeadAirFiller = false;
private float _deadAirFillerTimer = 0f;
private float _currentFillerLineDuration = 0f;
private DialogueLine _currentFillerLine = null;

[Header("Dead Air Settings")]
[Tooltip("Time between filler lines when no callers available")]
[SerializeField] private float _deadAirCycleInterval = 8f;
```

#### New Properties

```csharp
public bool IsPlayingDeadAirFiller => _isPlayingDeadAirFiller;
public DialogueLine CurrentFillerLine => _currentFillerLine;
```

#### New Events

```csharp
public event Action<DialogueLine> OnFillerLineDisplayed;
public event Action OnFillerStopped;
public event Action<DialogueLine> OnBroadcastLineDisplayed;
public event Action OnBroadcastLineCompleted;
```

#### New Methods

**Dead Air Filler:**
- `StartDeadAirFiller()` - Begin filler mode
- `StopDeadAirFiller()` - End filler mode
- `DisplayNextFillerLine()` - Show next filler line from template
- `UpdateDeadAirFiller()` - Handle filler timing; auto-advance callers when line ends
- `HandleCallerApproved()` - Put caller on air immediately if nothing playing

**Broadcast Flow:**
- `PlayBroadcastLine(template, onComplete)` - Play a broadcast line with callback
- `UpdateBroadcastLine()` - Handle broadcast line timing
- `CompleteBroadcastLine()` - Complete broadcast line and invoke callback
- `CancelBroadcastLine()` - Cancel broadcast line without invoking callback
- `PlayShowOpening(onComplete)` - Play show opening line
- `PlayShowClosing(onComplete)` - Play show closing line
- `PlayBetweenCallers(onComplete)` - Play between-callers transition

#### Modified Methods

- `Start()` - Subscribe to `OnCallerApproved`
- `OnDestroy()` - Unsubscribe from `OnCallerApproved`
- `Update()` - Add filler update logic
- `HandleConversationCompleted()` - Check queue and start filler or auto-advance
- `HandleCallerOnAir()` - Stop filler when caller goes on air

### ConversationPanel.cs Changes

#### New Event Subscriptions

- `OnFillerLineDisplayed` - Display filler line with typewriter effect
- `OnFillerStopped` - Return to empty state if no conversation
- `OnBroadcastLineDisplayed` - Display broadcast line (opening/closing/between)
- `OnBroadcastLineCompleted` - Update display after broadcast line ends

#### Modified Methods

- `DoSubscribe()` / `DoUnsubscribe()` - Add filler event handlers
- `UpdateDisplay()` - Show conversation container during filler
- `GetCallerName()` - Handle filler mode (no caller)

## UI Display

When dead air filler is playing:

| Element | Display |
|---------|---------|
| Speaker Icon | Green (Vern color) |
| Speaker Label | "VERN" |
| Phase Label | "ON AIR" |
| Dialogue Text | Filler line with typewriter effect |
| History | Empty (cleared for filler mode) |
| Progress Bar | Shows progress through current filler line |

## Testing Checklist

### Broadcast Flow
- [ ] LiveShow starts → Show opening plays first
- [ ] Show opening ends, caller on hold → Caller goes on air automatically
- [ ] Show opening ends, no callers → Dead air filler starts
- [ ] Conversation ends with callers on hold → Between-callers transition plays
- [ ] Between-callers ends → Next caller goes on air
- [ ] LiveShow ends → Show closing plays

### Dead Air Filler
- [ ] Conversation ends with no callers → filler starts
- [ ] Filler displays with typewriter effect
- [ ] Filler cycles to new line after ~8 seconds (if no callers)
- [ ] Filler line ends with caller on hold → Caller goes on air automatically
- [ ] Caller approved while nothing playing → Caller goes on air immediately
- [ ] Caller goes on air → filler stops immediately
- [ ] Phase label shows "ON AIR" during filler
- [ ] Empty state never shows when filler is playing

## Future Enhancements

- Add variety weighting to prevent recently-used filler lines from repeating
- Add configurable timing for opening/closing/between-callers display duration
- Add special opening lines for topic-specific shows
