# KBTV - Conversation System Design

## Overview

The conversation system plays dialogue between Vern Tell (the host) and callers during the LiveShow phase. Each call is a **conversation arc** - a pre-scripted cohesive narrative where every line responds naturally to the previous.

**Arc Assignment**: Arcs are assigned to callers during generation with topic-based distribution. **90% on-topic** callers get arcs matching the show's current topic, while **10% off-topic** callers get arcs from different topics. Each caller has an `IsOffTopic` flag set automatically. Screening can detect topic deception, but off-topic callers are transparent about their actual topic.

Arc selection is based on caller legitimacy and topic matching. Vern's current **mood** and **discernment** determine which variant plays and whether he challenges or believes the caller.

## Related Documentation

| Document | Description |
|----------|-------------|
| [CONVERSATION_ARCS.md](CONVERSATION_ARCS.md) | Arc-based system design, mood system, discernment mechanics |
| [CONVERSATION_ARC_SCHEMA.md](CONVERSATION_ARC_SCHEMA.md) | JSON schema and full example arc with all 5 moods |

## Design Goals

1. **Cohesion** - Each call is a complete narrative, not random line assembly
2. **Variety** - Target: 80 arcs x 5 moods x 2 belief paths = 800 unique conversations
3. **Reactivity** - Vern's mood affects delivery; discernment affects belief path
4. **Pacing** - Natural flow with appropriate timing between lines

**Current Status**: 17 sample arcs implemented (4 topics x 4 legitimacy tiers + 1 topic-switcher arc). See [ROADMAP.md](../design/ROADMAP.md) for expansion plans.

## Quick Reference

### Legitimacy Levels

| Level | Characteristics | Line Count |
|-------|-----------------|------------|
| **Fake** | Generic claims, weak details, flustered | 6 lines |
| **Questionable** | Vague, hedging, uncertain | 8 lines |
| **Credible** | Specific details, consistent story | 10 lines |
| **Compelling** | Strong hook, evidence, confident | 12 lines |

### Mood States

| Mood | Vern's Delivery |
|------|-----------------|
| Tired | Brief, yawns, low energy |
| Grumpy | Impatient, short, irritable |
| Neutral | Professional, balanced |
| Engaged | Interested, good follow-ups |
| Excited | High energy, enthusiastic |

### Mental & Hoaxer Detection

Determines if Vern correctly identifies a hoaxer during screening. Mental stat (boosted by Topic Belief tier bonuses) affects the player's ability to detect low-legitimacy callers:

```
Detection is screening-based (Option A):
- Player rejects low-legitimacy caller = "caught hoaxer"
- Player approves low-legitimacy caller = "fooled by hoaxer"

Higher Mental provides screening hints to help identify fakes.
Topic Belief Tier bonuses:
  Tier 2 (Curious):      +5% Mental
  Tier 3 (Interested):  +10% Mental, screening hints visible
  Tier 4 (Believer):    +15% Mental
  Tier 5 (True Believer): +20% Mental
```

## Arc Structure

```
Intro (2 lines)
  └─ Vern greets caller, Caller makes initial claim

Development (2-4 lines based on legitimacy)
  └─ Caller elaborates, Vern asks follow-ups

Belief Branch (0-4 lines based on legitimacy)
  ├─ Skeptical: Vern challenges, Caller defends
  └─ Believing: Vern validates, Caller appreciates

Conclusion (2 lines)
  └─ Vern wraps up, Caller signs off
```

## Timing & Pacing

### Line Duration

```
Duration = BaseDelay + (CharacterCount * PerCharDelay)
         = 1.5s + (text.Length * 0.04s)
```

Example: "I saw a bright light over the mesa." (37 chars) = 2.98 seconds

### Speaker Transition

- 0.5 second pause between different speakers
- No pause for same speaker back-to-back

### Approximate Call Durations

| Legitimacy | Lines | Duration |
|------------|-------|----------|
| Fake | 6 | ~25 seconds |
| Questionable | 8 | ~34 seconds |
| Credible | 10 | ~42 seconds |
| Compelling | 12 | ~50 seconds |

## Directory Structure

```
Assets/Data/Dialogue/
├── Arcs/                    # Arc-based conversations
│   ├── UFOs/
│   │   ├── Fake/
│   │   ├── Questionable/
│   │   ├── Credible/
│   │   └── Compelling/
│   ├── Cryptids/
│   ├── Conspiracies/
│   └── Ghosts/
└── Vern/                    # Broadcast lines (opening, closing, filler)
    └── VernDialogue.json
```

## Template Substitution

| Placeholder | Source | Example |
|-------------|--------|---------|
| `{callerName}` | CallerProfile.Name | "Dale" |
| `{callerLocation}` | CallerProfile.Location | "Roswell, NM" |
| `{topic}` | Topic.DisplayName | "UFO sightings" |

## Implementation Status

### Completed Phases

- [x] Phase 1: Core Flow (DialogueTypes, Conversation, ConversationManager)
- [x] Phase 2: Dialogue Content (Vern templates, UFO caller templates)
- [x] Phase 3: Expand Content (Cryptids, Ghosts, Conspiracies)
- [x] Phase 4: Polish (Typewriter effect, history, audio hooks)
- [x] Phase 5: Arc-Based System (ConversationArc, ArcRepository, MoodCalculator, DiscernmentCalculator)
- [x] Phase 6: Split API Architecture (ControlAction enum, GetPendingControlAction, OnControlActionCompleted)

## Split API Architecture

The conversation system uses a **split API** to separate display lines from control signals, enabling proper caller transfer and transcript synchronization.

### API Methods

```csharp
// Consumer asks for displayable content
BroadcastLine GetNextDisplayLine(); // Returns line or null

// Consumer checks for control actions
ControlAction GetPendingControlAction(); // PutCallerOnAir, None

// Consumer reports completion
void OnControlActionCompleted(); // Clears pending action
void OnLineCompleted(); // Advances to next state
```

### Control Actions

| Action | Description | When Used |
|--------|-------------|-----------|
| `PutCallerOnAir` | Signal to put next caller on air | When queue has waiting callers |
| `None` | No control action pending | Normal display flow |

### Corrected Caller Transfer Flow

**Before (Problematic):**
```
GetNextLine() returns PutCallerOnAir (control signal as line)
↓
UI displays "PutCallerOnAir" in transcript ❌
↓
Audio tries to play non-existent line ❌
↓
Caller transfer fails ❌
```

**After (Fixed):**
```
GetPendingControlAction() returns PutCallerOnAir
↓
UI calls PutOnAir() → moves caller to on-air state
↓
UI calls OnCallerPutOnAir(caller) → sets up arc with index 0
↓
UI calls OnControlActionCompleted() → clears pending action
↓
GetNextDisplayLine() returns first arc line (index 0)
↓
Caller dialogue displays correctly ✅
```

### Event-Driven Synchronization

Conversations progress through event-driven state transitions:

**Event Flow Pattern:**
```csharp
// 1. User puts caller on air → OnCallerOnAir()
// 2. BroadcastCoordinator publishes ConversationStartedEvent
// 3. ConversationDisplay receives event → requests first line
// 4. Audio plays → fires AudioCompletedEvent when done
// 5. ConversationDisplay receives event → calls OnLineCompleted()
// 6. BroadcastCoordinator advances to next line
// 7. Loop continues until conversation ends
```

**Benefits:**
- Loose coupling between conversation logic and UI display
- Natural audio synchronization through event completion
- Predictable state transitions without timers
- Easy to extend with new event types

## Future Considerations

### Player Intervention
- "Cut the caller" button ends conversation early
- "Steer the conversation" prompts affect next line selection

### Memorable Callers
- Unique dialogue trees for specific characters
- Recurring callers with continuity
