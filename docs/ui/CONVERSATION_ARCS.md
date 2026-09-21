# KBTV - Arc-Based Conversation System

## Overview

The arc-based system provides pre-scripted, narrative-driven conversations between Vern and callers. Each arc is a complete story with beginning, middle, and end - authored as cohesive dialogue rather than assembled from random lines.

**Key Changes in v2.0:**
- One conversation per arc, authored as a flat `arcLines` array
- Each Vern turn carries **13 mood variants** (one per `VernMoodType`); the runtime picks the variant matching Vern's current mood at playback time
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

### Mood Variants, Not Mood Filters

Each arc contains **one dialogue set**, but every Vern turn ships all 13 mood
variants (text + audio). Vern's mood selects which variant plays via
`ArcDialogueLine.GetAudioIdForMood` / `GetTextForMood`
(`scripts/dialogue/ConversationArc.cs`), falling back to the line default
(`neutral`) if a mood has no variant. The **caller's story remains
consistent** regardless of Vern's mood.

| Vern's Mood (example) | Delivery Style | Example |
|-------------|----------------|---------|
| tired | Flat, slow, dismissive | "Yeah... go on." |
| energized | Enthusiastic, quick, engaged | "This is incredible! Keep going!" |
| irritated | Short, sharp, impatient | "Get to the point." |
| amused | Playful, entertained | "Ha! I love this. Continue." |

The full 13-mood enum lives in `scripts/data/VernMoodType.cs` and is
mirrored in [CONVERSATION_ARC_SCHEMA.md](CONVERSATION_ARC_SCHEMA.md).

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

Each arc uses a flat `arcLines` array of speaker turns; Vern turns carry
mood-specific text + audio variants (13 each):

```
arcLines (8-12 turns, starts with Vern, strictly alternates Vern/Caller)
  └─ Vern turns: 13 lines - one per VernMoodType
  └─ Caller turns: 1 line
  └─ Ends with Vern conclusion (recommended; some legacy arcs end on a Caller line)
```

**Example arc structure:**
```
001: Vern intro (13 mood variants)
002: Caller initial claim
003: Vern response (13 mood variants)
004: Caller details
005: Vern response (13 mood variants)
006: Caller details
007: Vern conclusion (13 mood variants)
```

The conversation phases (Intro, Development, Conclusion) are assigned based on
line index at parse time (`ArcJsonParser.DetermineSection`), not from the JSON.

**Note:** The "belief branch" (Skeptical/Believing) sections from the original
design were never adopted - current arcs have no belief branching, and the
discernment mechanic above is used by screening/stats, not dialogue selection.

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
assets/dialogue/
├── arcs/                          # Arc-based conversations (auto-discovered, flat per topic)
│   ├── UFOs/                      # e.g. pilot.json, dashcam_trucker.json, topic_switch_ghost.json
│   ├── Cryptids/                  # e.g. biologist.json, claims_ufos.json
│   ├── Conspiracies/              # e.g. whistleblower.json, tinfoil.json
│   └── Ghosts/                    # e.g. old_house.json, halloween.json
└── vern/                          # Broadcast lines (openings, closings, fillers, ...)
    ├── openings.json
    ├── closings.json
    └── ... (one file per line type)
```

Topics and legitimacy levels live inside each JSON (`topic`, `legitimacy`
fields); no legitimacy subfolders. `ArcRepository` discovers every
`*.json` under `arcs/` recursively.

## Content Volume

| Metric | Count |
|--------|-------|
| Topics | 4 (UFOs, Cryptids, Conspiracies, Ghosts) |
| Legitimacy levels | 4 (Fake, Questionable, Credible, Compelling) |
| Total arcs | 73 (as of Sep 2026) |
| Total dialogue line entries | ~4000 incl. all mood variants |

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
- [x] Mood-variant JSON schema (13 variants per Vern turn, authored per line)
- [x] Runtime mood variant selection (`ArcDialogueLine.GetAudioIdForMood`)
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

1. Create `assets/dialogue/arcs/{Topic}/{descriptor}.json` following
   [CONVERSATION_ARC_SCHEMA.md](CONVERSATION_ARC_SCHEMA.md) (that doc is the
   authoritative rule set, validated by `ArcSchemaValidationTests`).
2. Generate the audio:
   `cd Tools/AudioGeneration && python generate_arc_audio.py {descriptor} --check`
   then the same command without `--check` (see
   [AUDIO_GENERATION.md](../tools/AUDIO_GENERATION.md)).
3. Open Godot once so the new `.mp3` files import.
4. Run `pwsh -NoProfile -File run-tests.ps1 -Filter ConversationArcTests` and
   `-Filter ArcSchemaValidationTests` - both must stay green.

## Audio Naming Convention

Line ids are filenames (`.mp3`) inside
`assets/audio/voice/Vern/ConversationArcs/{topicToken}/{arcId}/` or
`assets/audio/voice/Callers/{topicToken}/{arcId}/`, where `{topicToken}` is
the first `_`-separated token of the line id (see the schema doc). The topic
folder is chosen by the line id, not the arc's `topic` field - this keeps
topic-switcher arcs consistent with the generator and runtime.
