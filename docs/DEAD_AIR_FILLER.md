# Dead Air Filler Feature

## Overview

During the LiveShow phase, when no caller is on air and no callers are waiting on hold, Vern will automatically deliver "dead air filler" monologue lines to maintain broadcast atmosphere. This prevents awkward silence and keeps the show feeling alive.

## Behavior Specification

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
               │ Auto-put next     │  │ Start dead air    │
               │ caller on air     │  │ filler mode       │
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
                    │  Caller put on hold?      │
                    │  (OnCallerApproved event) │
                    └───────────────────────────┘
                               │
                          Yes  │
                               ▼
                    ┌───────────────────────────┐
                    │ Set _callerWaiting flag   │
                    │ → Finish current line     │
                    │ → Then auto-put caller    │
                    │   on air                  │
                    └───────────────────────────┘
```

### Scenarios

| Scenario | Behavior |
|----------|----------|
| Conversation ends, callers on hold | Immediately put next caller on air (no dead air) |
| Conversation ends, no callers on hold | Start dead air filler (Vern monologue) |
| Dead air playing, filler line ends, no callers | Cycle to next filler line after ~8s |
| Dead air playing, caller approved (put on hold) | Finish current filler line, then auto-put caller on air |
| Dead air playing, caller goes on air manually | Stop dead air filler immediately |

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
private bool _callerWaitingAfterFiller = false;
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
```

#### New Methods

- `StartDeadAirFiller()` - Begin filler mode
- `StopDeadAirFiller()` - End filler mode
- `DisplayNextFillerLine()` - Show next filler line from template
- `UpdateDeadAirFiller()` - Handle filler timing in Update loop
- `HandleCallerApproved()` - Flag to auto-advance when filler ends

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

- [ ] Conversation ends with callers on hold → next caller auto-goes on air
- [ ] Conversation ends with no callers → filler starts
- [ ] Filler displays with typewriter effect
- [ ] Filler cycles to new line after ~8 seconds
- [ ] Approving a caller during filler → filler finishes, then caller goes on air
- [ ] Manually putting caller on air during filler → filler stops immediately
- [ ] Phase label shows "ON AIR" during filler
- [ ] Empty state never shows when filler is playing

## Future Enhancements

- Add "between callers" transition lines when auto-advancing to next caller
- Add show opening/closing lines for phase transitions
- Add variety weighting to prevent recently-used filler lines from repeating
