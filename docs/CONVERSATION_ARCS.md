# KBTV - Arc-Based Conversation System

## Overview

The arc-based system replaces the line-pool approach with **pre-scripted conversation arcs** - complete dialogues where every line is authored together as a cohesive narrative. This ensures natural flow where Vern's questions reference the caller's claims and caller responses build on previous details.

Arc selection is based on caller attributes (legitimacy, topic). Vern's current **mood** determines which variant plays, and his **discernment** determines whether he challenges or believes the caller.

## Core Concepts

### Conversation Arcs

Pre-written complete conversations where all lines are authored together. Unlike random line assembly, arcs ensure:
- Vern's follow-ups reference specific caller claims
- Caller responses acknowledge Vern's reactions
- Story details remain consistent throughout
- Natural conversational rhythm

### Mood System

Vern has 5 mood states that affect his delivery style. Each arc includes variants for all moods.

| Mood | Vern's Delivery | Caller Reaction (Subtle) |
|------|-----------------|--------------------------|
| Tired | Brief, yawns, low energy | More direct, gets to point |
| Grumpy | Impatient, short, irritable | Defensive, nervous |
| Neutral | Professional, balanced | Normal delivery |
| Engaged | Interested, good follow-ups | Opens up, adds details |
| Excited | High energy, enthusiastic | Feeds off energy |

**Note:** Mood affects *tone and delivery*, not the core story. The same caller tells the same story - just with different energy levels from both parties.

### Discernment System

Vern's ability to detect whether a caller is legitimate or fake. Determines whether Vern takes the **Skeptical** or **Believing** path during the belief branch.

```
correctReadChance = Discernment + LegitimacyModifier
```

#### Legitimacy Modifiers

| Legitimacy | Modifier | Reasoning |
|------------|----------|-----------|
| Compelling | +20% | Convincing delivery, easy to believe |
| Credible | +10% | Solid evidence, leans believable |
| Questionable | +0% | Ambiguous, hardest to read |
| Fake | +15% | Often has obvious tells, easier to spot |

#### Examples

| Discernment | Caller Type | Calculation | Correct Read Chance |
|-------------|-------------|-------------|---------------------|
| 40% | Compelling | 40 + 20 | 60% Vern believes |
| 40% | Fake | 40 + 15 | 55% Vern is skeptical |
| 80% | Questionable | 80 + 0 | 80% correct read |
| 20% | Credible | 20 + 10 | 30% correct read |

#### Gameplay Implications

- **Low discernment Vern**: Gets fooled by fakes, misses real sightings
- **High discernment Vern**: Accurately validates credible callers, dismisses fakes
- **Questionable callers**: Always a coin flip - maintains tension regardless of stats

## Arc Structure

Each arc follows a 4-phase structure with variable line counts based on legitimacy:

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

### Line Counts by Legitimacy

| Legitimacy | Total | Intro | Development | Belief Branch | Conclusion |
|------------|-------|-------|-------------|---------------|------------|
| Fake | 6 | 2 | 2 | 0 | 2 |
| Questionable | 8 | 2 | 2 | 2 | 2 |
| Credible | 10 | 2 | 4 | 2 | 2 |
| Compelling | 12 | 2 | 4 | 4 | 2 |

**Why no belief branch for Fake callers?** Fake callers are typically dismissed quickly. Vern doesn't invest in challenging or validating them - he just moves on.

## Arc Selection Flow

```
1. Get caller's legitimacy and topic from CallerProfile
2. Find all arcs matching legitimacy + topic
3. Select random arc from matches
4. Get Vern's current mood → select moodVariant
5. Calculate belief path:
   correctReadChance = Discernment + LegitimacyModifier
   if (random < correctReadChance):
     beliefPath = legitimacy is Credible/Compelling ? "Believing" : "Skeptical"
   else:
     beliefPath = opposite (Vern misreads)
6. Assemble lines: intro + development + beliefBranch[beliefPath] + conclusion
7. Apply template substitution ({callerName}, {callerLocation})
8. Return as Conversation
```

## Template Substitution

| Placeholder | Source | Example |
|-------------|--------|---------|
| `{callerName}` | CallerProfile.Name | "Dale" |
| `{callerLocation}` | CallerProfile.Location | "Roswell, NM" |
| `{topic}` | Topic.DisplayName | "UFO sightings" |

## Directory Structure

```
Assets/Data/Dialogue/
├── Arcs/                          # Arc-based conversations
│   ├── UFOs/
│   │   ├── Fake/
│   │   │   ├── prankster.json
│   │   │   ├── vague.json
│   │   │   └── ... (5 arcs)
│   │   ├── Questionable/
│   │   │   └── ... (5 arcs)
│   │   ├── Credible/
│   │   │   ├── dashcam_trucker.json
│   │   │   └── ... (5 arcs)
│   │   └── Compelling/
│   │       └── ... (5 arcs)
│   ├── Cryptids/
│   │   ├── Fake/
│   │   ├── Questionable/
│   │   ├── Credible/
│   │   └── Compelling/
│   ├── Conspiracies/
│   │   └── ...
│   └── Ghosts/
│       └── ...
└── Vern/                          # Broadcast lines (opening, closing, filler)
    └── VernDialogue.json
```

## Content Volume

| Metric | Count |
|--------|-------|
| Topics | 4 (UFOs, Cryptids, Conspiracies, Ghosts) |
| Legitimacy levels | 4 (Fake, Questionable, Credible, Compelling) |
| Arcs per legitimacy x topic | 5 |
| **Total arcs** | 80 |
| Mood variants per arc | 5 |
| **Total mood variants** | 400 |
| Belief branches per variant | 2 |
| **Total unique paths** | 800 |

## Implementation Status

### Completed

- [x] ConversationArc.cs data model
- [x] ArcJsonData.cs for JSON deserialization
- [x] DialogueSubstitution.cs utility
- [x] ArcRepository.cs for runtime storage
- [x] MoodCalculator.cs for Vern mood calculation
- [x] DiscernmentCalculator.cs for belief path selection
- [x] ArcConversationGenerator.cs for conversation generation
- [x] Updated ConversationManager.cs to use arc system
- [x] Updated GameBootstrap.cs to use ArcRepository
- [x] Updated GameSetup.cs to load arc JSON files
- [x] Removed legacy CallerDialogueTemplate and ConversationGenerator
- [x] Removed legacy JSON files and ScriptableObjects

## Adding New Arcs

To add a new conversation arc:

1. Create a JSON file in `Assets/Data/Dialogue/Arcs/{Topic}/{Legitimacy}/`
2. Follow the schema in [CONVERSATION_ARC_SCHEMA.md](CONVERSATION_ARC_SCHEMA.md)
3. Include all 5 mood variants (Tired, Grumpy, Neutral, Engaged, Excited)
4. Include both belief branches (Skeptical, Believing) for Questionable+ legitimacy
5. Run **KBTV > Setup Game Scene** to reload arcs
