# KBTV - Conversation System Design

## Overview

The conversation system plays dialogue between Vern Tell (the host) and callers during the LiveShow phase. Each call is a **conversation arc** - a pre-scripted cohesive narrative where every line responds naturally to the previous.

Arc selection is based on caller attributes (legitimacy, topic). Vern's current **mood** and **discernment** determine which variant plays and whether he challenges or believes the caller.

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

### Discernment

Determines if Vern correctly reads the caller:

```
correctReadChance = Discernment + LegitimacyModifier

Modifiers:
  Compelling:   +20%
  Credible:     +10%
  Questionable:  +0%
  Fake:         +15%
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

## Future Considerations

### Player Intervention
- "Cut the caller" button ends conversation early
- "Steer the conversation" prompts affect next line selection

### Memorable Callers
- Unique dialogue trees for specific characters
- Recurring callers with continuity
