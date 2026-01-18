# Broadcast Flow & Dead Air Filler

## Overview

The broadcast flow system manages Vern's dialogue during transitions in the LiveShow phase using the **pull-based BroadcastCoordinator** pattern:

1. **Show Opening** - Vern's intro line when LiveShow begins
2. **Dead Air Filler** - Vern's monologue when no caller is on air (8s per cycle)
3. **Between Callers** - Transition lines when auto-advancing to next caller
4. **Show Closing** - Vern's outro line when LiveShow ends

These features maintain broadcast atmosphere, prevent awkward silence, and make the show feel alive.

## BroadcastCoordinator Pattern

The `BroadcastCoordinator` is a **pull-based** service that manages the entire broadcast flow:

```csharp
// Consumer asks "what's next?" - coordinator decides
BroadcastLine GetNextLine();

// Consumer calls this when line finishes playing
void OnLineCompleted();

// UI calls these when state changes
void OnCallerOnAir(Caller caller);
void OnCallerOnAirEnded(Caller caller);
void OnLiveShowStarted();
void OnLiveShowEnding();
```

### State Machine

```
ShowOpening → Conversation → BetweenCallers → Conversation
                 ↓                    ↓
           (no callers)        (no callers)
                 ↓                    ↓
           DeadAirFiller      DeadAirFiller
                 ↓                    ↓
           (1 cycle)           (1 cycle)
                 ↓                    ↓
           Conversation        Conversation
```

## Dead Air Filler

### Behavior Specification

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
                           │                │
                           └────────┬───────┘
                                    │
                                    ▼
                         ┌───────────────────┐
                         │ Filler line ends  │
                         │ (8s cycle)        │
                         └───────────────────┘
                                    │
                           ┌────────┴────────┐
                           │                 │
                           ▼                 ▼
                ┌───────────────────┐  ┌───────────────────┐
                │ Callers on hold?  │  │ Continue filler   │
                └───────────────────┘  │ (next line)       │
                           │          └───────────────────┘
                      Yes  │
                           ▼
                ┌───────────────────┐
                │ Stop filler, put  │
                │ next caller on air│
                └───────────────────┘
```

### Key Timing Rules

| Phase | Duration |
|-------|----------|
| Show Opening | 5 seconds |
| Vern Dialogue | 4 seconds |
| Caller Dialogue | 4 seconds |
| Between Callers | 4 seconds |
| Dead Air Filler | 8 seconds per line |
| Show Closing | 5 seconds |

### Auto-Advance Rules

1. **Filler ends + callers on hold** → Put next caller on air after 1 filler cycle
2. **Between-callers ends + callers on hold** → Put next caller on air
3. **No callers ever** → Filler cycles forever
4. **Arc dialogue exhausted** → Between-callers transition → auto-advance

## Scenarios

| Scenario | Behavior |
|----------|----------|
| LiveShow starts | Show opening plays (5s), then check for on-hold callers or start filler |
| Show opening ends, callers on hold | First caller goes on air automatically |
| Show opening ends, no callers on hold | Dead air filler starts (8s cycle) |
| Conversation ends, callers on hold | Between-callers transition (4s), then next caller on air |
| Conversation ends, no callers on hold | Start dead air filler |
| Filler line ends, callers on hold | Stop filler, put next caller on air automatically |
| Filler line ends, no callers on hold | Wait for cycle interval (8s), then show next filler line |
| LiveShow ends | Show closing plays (5s) |

## Arc-Caller Association

**Critical:** Each caller has their conversation arc assigned during generation:

1. **Caller generation** → Arc selected by legitimacy, stored as `caller.ActualArc` and `caller.ClaimedArc`
2. **Screening approval** → Uses `caller.ActualArc` for the conversation
3. **Put on air** → Arc is retrieved: `coordinator.OnCallerOnAir(caller)` → uses `caller.ActualArc`

This ensures:
- Arcs are known upfront, enabling screening preview
- Deception possible: claimed arc ≠ actual arc (30% chance)
- Consistent conversation regardless of when caller goes on air

## Implementation Details

### Files

| File | Purpose |
|------|---------|
| `scripts/dialogue/BroadcastCoordinator.cs` | Main pull-based coordinator service |
| `scripts/dialogue/BroadcastLineType.cs` | Enum: ShowOpening, VernDialogue, CallerDialogue, BetweenCallers, DeadAirFiller, ShowClosing, PutCallerOnAir, None |
| `scripts/dialogue/BroadcastLine.cs` | Struct with Text, Speaker, Type, Phase, ArcId |
| `scripts/dialogue/ConversationDisplay.cs` | UI display that polls GetNextLine() |
| `scripts/dialogue/ArcRepository.cs` | Auto-discovers arc files from `assets/dialogue/arcs/` |
| `scripts/dialogue/ArcJsonParser.cs` | Parses arc JSON files using Godot's JSON class |
| `scripts/callers/Caller.cs` | Stores Arc references: `ActualArc`, `ClaimedArc`, `Arc` properties |

### BroadcastCoordinator API

```csharp
public partial class BroadcastCoordinator : Node
{
    // Pull-based API
    public BroadcastLine GetNextLine();
    public void OnLineCompleted();
    
    // State change callbacks (called by UI)
    public void OnLiveShowStarted();
    public void OnLiveShowEnding();
    public void OnCallerOnAir(Caller caller);
    public void OnCallerOnAirEnded(Caller caller);
    
    // Line duration (seconds)
    private static float GetLineDuration(BroadcastLine line)
    {
        return line.Type switch
        {
            BroadcastLineType.ShowOpening => 5f,
            BroadcastLineType.VernDialogue => 4f,
            BroadcastLineType.CallerDialogue => 4f,
            BroadcastLineType.BetweenCallers => 4f,
            BroadcastLineType.DeadAirFiller => 8f,
            BroadcastLineType.ShowClosing => 5f,
            _ => 0f
        };
    }
}
```

### Auto-Discovery of Arc Files

`ArcRepository.Initialize()` automatically discovers arc JSON files:

```csharp
private Godot.Collections.Array<string> DiscoverArcFiles()
{
    var foundFiles = new Godot.Collections.Array<string>();
    var searchDir = "res://assets/dialogue/arcs";
    
    // Recursively find all .json files
    DiscoverArcFilesRecursive(searchDir, foundFiles);
    return foundFiles;
}
```

Arc files are organized by topic and legitimacy:
```
assets/dialogue/arcs/
├── UFOs/
│   ├── Questionable/lights.json
│   ├── Fake/prankster.json
│   └── Credible/dashcam_trucker.json
├── Ghosts/
│   ├── Questionable/footsteps.json
│   └── ...
└── ...
```

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
- [ ] Filler cycles to new line after 8 seconds (if no callers)
- [ ] Filler line ends with caller on hold → Caller goes on air automatically
- [ ] Caller approved while nothing playing → Caller goes on air immediately
- [ ] Caller goes on air → filler stops immediately
- [ ] Phase label shows "DEAD AIR" during filler

### Arc Loading
- [ ] Arcs auto-discovered from `assets/dialogue/arcs/`
- [ ] Arc loaded when caller is approved (screening)
- [ ] Arc retrieved when caller goes on air
- [ ] Arc dialogue lines display correctly in transcript
