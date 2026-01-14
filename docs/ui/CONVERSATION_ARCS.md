# KBTV - Arc-Based Conversation System

## Overview

The arc-based system provides pre-scripted, narrative-driven conversations between Vern and callers. Each arc is a complete story with beginning, middle, and end - authored as cohesive dialogue rather than assembled from random lines.

**Key Changes in v2.0:**
- Single dialogue set per arc (no mood variants in JSON)
- Tone applied programmatically based on Vern's current mood
- Caller personality drives storytelling style
- Medium-to-long format with richer character development

## Core Concepts

### Conversation Arcs

Pre-written complete conversations where all lines are authored together. Unlike random line assembly, arcs ensure:
- Consistent caller backstory and motivation
- Natural conversational flow with proper escalation
- Emotional beats that build and resolve
- Vern's questions reference specific caller claims
- Caller responses acknowledge Vern's reactions

### Single Dialogue, Multiple Tones

Each arc contains **one dialogue set**. Vern's mood affects **delivery style**, not content:

| Vern's Mood | Delivery Style | Example |
|-------------|----------------|---------|
| Tired | Flat, slow, dismissive | "Yeah... go on." |
| Energized | Enthusiastic, quick, engaged | "This is incredible! Keep going!" |
| Irritated | Short, sharp, impatient | "Get to the point." |
| Amused | Playful, entertained | "Ha! I love this. Continue." |
| Gruff | Direct, no-nonsense | "What do you want?" |
| Focused | Analytical, probing | "Walk me through exactly what happened." |
| Neutral | Professional, balanced | Standard delivery |

The **caller's story remains consistent** regardless of Vern's mood - only Vern's delivery changes.

### Caller Personalities

Each arc specifies a caller personality type that shapes the storytelling:

| Personality | How They Sound | Example Opener |
|-------------|----------------|----------------|
| Gruff Experienced | Straight-talking professional | "Listen, I've been doing this twenty years..." |
| Nervous Hesitant | Anxious, second-guessing | "I don't know if you'll believe me, but..." |
| Enthusiastic Convert | Was skeptical, now believer | "I used to laugh at this stuff, but then..." |
| Cold Factual | Detached, clinical | "Timeline: 3:47 AM, coordinates confirmed..." |
| Emotional Wreck | Fearful, urgent, personal stakes | "I'm scared to go back home..." |
| Charismatic Storyteller | Pacing, dramatic | "So there I was, when suddenly..." |

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

Each arc uses a flat dialogue array with mood-specific text variants for Vern's lines:

```
Dialogue Array (8-10 lines, odd number, starts and ends with Vern)
  └─ Lines alternate: Vern, Caller, Vern, Caller...
  └─ Vern lines have 7 text variants (one per mood)
  └─ Caller lines have single text
```

**Example arc structure:**
```
001: Vern intro (7 mood variants)
002: Caller initial claim
003: Vern response (7 mood variants)
004: Caller details
005: Vern response (7 mood variants)
006: Caller details
007: Vern response (7 mood variants)
008: Caller closing
009: Vern conclusion (7 mood variants)
```

The conversation phases (Intro, Probe, Challenge, Resolution) are assigned based on line index at runtime, not from the JSON structure.
  └─ Vern wraps up, Caller signs off
```

### Line Counts by Legitimacy

| Legitimacy | Total | Intro | Development | Belief Branch | Conclusion |
|------------|-------|-------|-------------|---------------|------------|
| Fake | 8 | 2 | 4 | 0 | 2 |
| Questionable | 10 | 2 | 4 | 2 | 2 |
| Credible | 12 | 2 | 4 | 4 | 2 |
| Compelling | 14 | 2 | 6 | 4 | 2 |

**Note:** Fake callers are dismissed quickly - no belief branch investment.

## Arc Selection Flow

```
1. Get caller's legitimacy and topic from CallerProfile
2. Find all arcs matching legitimacy + topic
3. Select random arc from matches
4. Get Vern's current mood → will determine delivery tone at runtime
5. Calculate belief path:
   correctReadChance = Discernment + LegitimacyModifier
   if (random < correctReadChance):
     beliefPath = legitimacy is Credible/Compelling ? "Believing" : "Skeptical"
   else:
     beliefPath = opposite (Vern misreads)
6. Select text variants based on Vern's mood for Vern's lines (caller lines use single text)
7. Apply template substitution ({callerName}, {callerLocation})
8. Return as Conversation
```

## Caller Story Elements

Each arc should include relevant elements based on topic:

| Element | UFO | Cryptid | Ghost | Conspiracy |
|---------|-----|---------|-------|------------|
| Occupation/lifestyle | Trucker, pilot, farmer | Hiker, park ranger, rancher | Historian, night watchman | Engineer, contractor, researcher |
| Discovery circumstances | Late night drive, airfield | Hiking trail, wildlife camera | Old building, family home | Work site, government facility |
| Emotional journey | Fear → Wonder | Unease → Fascination | Dread → Acceptance | Skepticism → Alarm |
| Evidence mentioned | Dashcam, photos, witnesses | Footage, tracks, physical proof | Recordings, historical records | Documents, recordings, expert testimony |
| Why call now | Reviewed footage, need to share | Just happened, shaken | Anniversary, events broke trust | Finally safe, others coming forward |
| Skepticism defense | "22 years trucking, I know aircraft" | "I know wildlife, this wasn't natural" | "My whole family felt it" | "I have documentation" |

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
│   │   │   └── prankster.json
│   │   ├── Questionable/
│   │   │   └── lights.json
│   │   ├── Credible/
│   │   │   └── dashcam_trucker.json
│   │   └── Compelling/
│   │       └── pilot.json
│   ├── Cryptids/
│   │   ├── Fake/
│   │   │   └── costume.json
│   │   ├── Questionable/
│   │   │   └── shadow.json
│   │   ├── Credible/
│   │   │   └── forest_hiker.json
│   │   └── Compelling/
│   │       └── biologist.json
│   ├── Conspiracies/
│   │   ├── Fake/
│   │   │   └── tinfoil.json
│   │   ├── Questionable/
│   │   │   └── patterns.json
│   │   ├── Credible/
│   │   │   └── govt_contractor.json
│   │   └── Compelling/
│   │       └── whistleblower.json
│   └── Ghosts/
│       ├── Fake/
│       │   └── halloween.json
│       ├── Questionable/
│       │   └── footsteps.json
│       ├── Credible/
│       │   └── old_house.json
│       └── Compelling/
│           └── investigator.json
└── Vern/                          # Broadcast lines (opening, closing, filler)
    └── VernDialogue.json
```

## Content Volume

| Metric | Count |
|--------|-------|
| Topics | 4 (UFOs, Cryptids, Conspiracies, Ghosts) |
| Legitimacy levels | 4 (Fake, Questionable, Credible, Compelling) |
| Arcs per topic × legitimacy | 4-5 |
| Total arcs | 18 |
| Avg lines per arc | ~12 |
| Total dialogue lines | ~216 |

**Compared to v1.0:** ~75% less JSON content (216 lines vs ~900 lines) while maintaining full gameplay variety.

## Writing Guidelines

### Arc Content Requirements

1. **Backstory** - Why this caller? What's their life like?
2. **Specific details** - Dates, times, locations, sensory descriptions
3. **Emotional journey** - How did this experience affect them?
4. **Evidence** - What proof do they mention?
5. **Personal stakes** - Why does this matter to them?
6. **Skepticism defense** - How would they counter doubters?

### Voice Consistency

- Caller personality should remain consistent throughout the arc
- Vern adapts his tone based on current mood (handled at runtime)
- Don't mix personality types within a single arc

### Writing Style

- Mix of personalities across arcs for variety
- Some arcs are more humorous, others more serious
- Let the caller's personality shape their delivery
- Vern responds to the energy the caller brings

## Implementation Status

### Completed

- [x] ConversationArc.cs data model
- [x] ArcJsonData.cs for JSON deserialization
- [x] DialogueSubstitution.cs utility
- [x] ArcRepository.cs for runtime storage
- [x] VernStateCalculator.cs for mood type and tone mapping
- [x] ArcConversationGenerator.cs for conversation generation
- [x] Updated ConversationManager.cs to use arc system
- [x] Single dialogue set architecture (no mood variants in JSON)
- [x] Runtime tone application based on Vern's mood
- [x] Removed legacy CallerDialogueTemplate and ConversationGenerator

### Arc Content (In Progress)

The following arcs need to be rewritten with the new longer, richer format:

| Topic | Status |
|-------|--------|
| UFOs | 4 of 4 done |
| Cryptids | 4 of 4 done |
| Conspiracies | 4 of 4 done |
| Ghosts | 4 of 4 done |

## Adding New Arcs

To add a new conversation arc:

1. Create a JSON file in `Assets/Data/Dialogue/Arcs/{Topic}/{Legitimacy}/`
2. Follow the schema in [CONVERSATION_ARC_SCHEMA.md](CONVERSATION_ARC_SCHEMA.md)
3. Include a complete dialogue with Vern and Caller lines (odd number, starts/ends with Vern)
4. Set appropriate `callerPersonality` for the story
5. Run **KBTV > Setup Game Scene** to reload arcs

## Audio Naming Convention

Audio files are organized by mood and use underscores in filenames:

```
{arcId}/{mood}/{arcId}_{mood}_{lineIndex:000}_{speaker}.ogg
```

Example:
- `ufos_credible_dashcam/energized/ufos_credible_dashcam_energized_001_vern.ogg`
- `ufos_credible_dashcam/tired/ufos_credible_dashcam_tired_001_vern.ogg`
- `ufos_credible_dashcam/neutral/ufos_credible_dashcam_neutral_001_vern.ogg`

Caller lines are placed in a `caller/` folder:
- `ufos_credible_dashcam/caller/ufos_credible_dashcam_caller_002.ogg`

Mood folders: `neutral`, `tired`, `energized`, `irritated`, `gruff`, `amused`, `focused`
