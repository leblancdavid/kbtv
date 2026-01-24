# KBTV - Arc JSON Schema

This document defines the JSON schema for conversation arc files.

## Overview

Conversation arcs are pre-scripted dialogues between Vern and callers. Each arc contains:
- An `arcLines` array defining the sequence of speakers with nested line details
- Each speaker entry contains a `lines` array with line variants (Vern has 7 mood variants)
- First speaker is always Vern's intro, last speaker is always Vern's conclusion
- Metadata for arc selection and voice direction

## Full Arc Example

```json
{
  "arcId": "dashcam_trucker",
  "topic": "UFOs",
  "legitimacy": "Credible",
  "screeningSummary": "Trucker with dashcam footage of triangular object pacing his truck for two miles",
  "callerPersonality": "gruff_experienced",
  "arcNotes": "Trucker with dashcam evidence, skeptical but has specific details, willing to defend his experience",
  "callerGender": "male",
  "arcLines": [
    {
      "speaker": "vern",
      "lines": [
        {
          "id": "ufos_credible_dashcam_vern_neutral_1",
          "text": "You're on the air. You mentioned dashcam footage?",
          "voiceText": "You're on the air. You mentioned dashcam footage?",
          "mood": "neutral"
        },
        {
          "id": "ufos_credible_dashcam_vern_tired_1",
          "text": "Alright, you're on. Something about dashcam footage?",
          "voiceText": "Alright, you're on. Something about dashcam footage?",
          "mood": "tired"
        }
      ]
    },
    {
      "speaker": "caller",
      "lines": [
        {
          "id": "ufos_credible_dashcam_caller_1",
          "text": "Yeah, I was trucking through rural Montana last month. Had my dashcam running like always. Something started pacing my truck.",
          "voiceText": "Yeah, I was trucking through rural Montana last month. Had my dashcam running like always. Something started pacing my truck.",
          "mood": "neutral"
        }
      ]
    }
  ]
}
```

## Schema Reference

### Root Object

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `arcId` | string | Yes | Unique identifier (format: `{topic}_{legitimacy}_{descriptor}`) - used as folder name for audio files |
| `topic` | string | Yes | One of: "UFOs", "Cryptids", "Conspiracies", "Ghosts" |
| `claimedTopic` | string | No | For topic-switcher arcs: what the caller claimed during screening |
| `legitimacy` | string | Yes | One of: "Fake", "Questionable", "Credible", "Compelling" |
| `arcLines` | array | Yes | Conversation structure with nested line details |
| `callerPersonality` | string | No | Personality type for voice direction |
| `arcNotes` | string | No | Author notes for content direction |
| `screeningSummary` | string | No | Brief summary of the caller's story for screening |
| `callerGender` | string | No | "male" or "female" for voice selection |

## Arc Lines Array

The `arcLines` array defines the conversation structure:
- **Index 0** is always vern (intro)
- **Index N-1** (last) is always vern (conclusion)
- Lines alternate: vern, caller, vern, caller...

| Index | Speaker | Description |
|-------|---------|-------------|
| 0 | vern | Intro - "You're on the air" |
| 1 | caller | Initial claim |
| 2 | vern | Response |
| 3 | caller | Details |
| 4 | vern | Response |
| 5 | caller | Details |
| 6 | vern | Response |
| 7 | caller | Closing thought |
| N-1 | vern | Conclusion |

### ArcLine Object

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `speaker` | string | Yes | "vern" or "caller" |
| `lines` | array | Yes | Array of line variants (Vern: 7 moods, Caller: 1) |

### Line Object

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | string | Yes | Unique line identifier (format: `{topic}_{legitimacy}_{descriptor}_{speaker}_{mood}_{sequence}`) |
| `text` | string | Yes | Display text (may contain placeholders) |
| `voiceText` | string | Yes | Text sent to TTS (may differ from display text) |
| `mood` | string | Yes | Mood enum: neutral, tired, energized, irritated, gruff, amused, focused |

### Mood Enum Values

| Value | Mood | Characteristics |
|-------|------|-----------------|
| `neutral` | Neutral | Professional, balanced, curious |
| `tired` | Tired | Low energy, dismissive, short responses |
| `energized` | Energized | High energy, enthusiastic, lots of punctuation |
| `irritated` | Irritated | Short-tempered, sarcastic, dismissive |
| `gruff` | Gruff | Gruff but engaged, reluctant interest |
| `amused` | Amused | Playful, laughing, finds it entertaining |
| `focused` | Focused | Analytical, detail-oriented, digging for facts |

## Placeholders

| Placeholder | Replaced With |
|-------------|---------------|
| `{callerName}` | Caller's name from CallerProfile |
| `{callerLocation}` | Caller's location from CallerProfile |
| `{topic}` | Current topic display name |

## Caller Personality Types

| Personality | Description | Vern's Response Style |
|-------------|-------------|----------------------|
| `gruff_experienced` | Straight-talking professional with decades of experience | Vern respects their expertise, engages as peer |
| `nervous_hesitant` | Anxious, second-guessing, uncertain if they'll be believed | Vern is patient, draws them out gently |
| `enthusiastic_convert` | Was skeptical, now true believer, eager to share | Vern is intrigued, asks how the转变 happened |
| `cold_factual` | Detached, clinical, presenting facts and timeline | Vern engages analytically, follows the evidence |
| `emotional_wreck` | Fearful, upset, personal stakes high | Vern is empathetic but focused on getting details |
| `charismatic_storyteller` | Pacing narrative, dramatic beats, performance-oriented | Vern goes along with the energy, plays off cues |

## Topic-Switcher Arcs

Topic-switcher arcs handle callers who lied about their topic during screening.

### Example

```json
{
  "arcId": "cryptid_credible_claims_ufos",
  "claimedTopic": "UFOs",
  "topic": "Cryptids",
  "legitimacy": "Credible",
  "screeningSummary": "Caller claiming UFO sighting but actually saw something in the woods",
  "callerPersonality": "nervous_hesitant",
  "arcNotes": "Topic-switcher: claimed UFOs but actually cryptid sighting",
  "callerGender": "male",
  "arcLines": [
    {
      "speaker": "vern",
      "lines": [
        {
          "id": "cryptid_credible_claims_ufos_vern_neutral_1",
          "text": "You're on. You said you had something about lights in the sky?",
          "voiceText": "You're on. You said you had something about lights in the sky?",
          "mood": "neutral"
        }
      ]
    },
    {
      "speaker": "caller",
      "lines": [
        {
          "id": "cryptid_credible_claims_ufos_caller_1",
          "text": "I... I said that to get through, Vern. That's not what I saw. I was out past Miller Road checking my trail cameras, and I heard something in the trees. Big. Moving through the undergrowth.",
          "voiceText": "I... I said that to get through, Vern. That's not what I saw. I was out past Miller Road checking my trail cameras, and I heard something in the trees. Big. Moving through the undergrowth.",
          "mood": "neutral"
        }
      ]
    },
    {
      "speaker": "vern",
      "lines": [
        {
          "id": "cryptid_credible_claims_ufos_vern_neutral_2",
          "text": "Trail cameras? So this is about whatever you've been tracking in the woods?",
          "voiceText": "Trail cameras? So this is about whatever you've been tracking in the woods?",
          "mood": "neutral"
        }
      ]
    }
  ]
}
```

## Audio File Organization

Audio files are stored in subdirectories by topic and arcId:
- **Vern**: `Vern/ConversationArcs/{topic}/{arcId}/{line.Id}.mp3`
- **Caller**: `Callers/{topic}/{arcId}/{line.Id}.mp3`

The `arcId` in each JSON must exactly match the JSON filename (without `.json`).

## File Naming Convention

```
{descriptor}.json
```

Examples:
- `dashcam_trucker.json`
- `nervous_hiker.json`
- `government_whistleblower.json`
- `historic_house.json`

The `arcId` inside the file should follow the pattern: `{topic}_{legitimacy}_{descriptor}`

## Writing Guidelines

### Arc Content Goals

- **Medium length** - 8-10 lines total (odd number, starts and ends with Vern)
- **Caller backstory** - Occupation, lifestyle, why this matters to them
- **Specific details** - Dates, times, locations, sensory details
- **Emotional journey** - How this experience affected them
- **Evidence mentioned** - Physical proof, documentation, witnesses

### Line Guidelines

| Position | Lines | Purpose |
|----------|-------|---------|
| Intro (0) | 1 Vern | Hook the listener, introduce caller |
| Body | 6-8 alternating | Build tension, add details, establish credibility |
| Conclusion (N-1) | 1 Vern | Wrap up, call to action |

### Mood Variant Guidelines

Each Vern line needs 7 unique variants:

| Mood | Energy | Punctuation | Response Style |
|------|--------|-------------|----------------|
| Neutral | Medium | Periods | Professional, curious |
| Tired | Low | Dots, ellipses | Dismissive, short |
| Energized | High | Exclamation marks!!! | Enthusiastic, excited |
| Irritated | Medium-low | Question marks? | Sarcastic, dismissive |
| Gruff | Medium-low | Periods | Reluctant interest |
| Amused | Medium-high | Exclamation, laughter | Playful, entertained |
| Focused | Medium | Periods | Analytical, detail-focused |

## Validation Rules

1. `arcLines` array must be present
2. First arcLine must be vern (index 0)
3. Last arcLine must be vern (index N-1, where N is odd)
4. arcLines must alternate: vern, caller, vern, caller...
5. Each arcLine must have `speaker` ("vern" or "caller") and `lines` array
6. Vern arcLines must have 7 lines (one per mood)
7. Caller arcLines must have 1 line
8. All lines must have `id`, `text`, `voiceText`, and `mood` fields
9. Line IDs must follow the pattern: `{topic}_{legitimacy}_{descriptor}_{speaker}_{mood}_{sequence}`
10. `arcId` must be unique across all arc files
11. All placeholders in `text` and `voiceText` should be valid

## Content Volume

| Metric | Count |
|--------|-------|
| Topics | 4 (UFOs, Cryptids, Conspiracies, Ghosts) |
| Legitimacy levels | 4 (Fake, Questionable, Credible, Compelling) |
| Arcs per topic × legitimacy | 4-5 |
| Total arcs | 17 |
| ArcLines per arc (avg) | 9 |
| Vern arcLines per arc | 5 (each with 7 mood variants) |
| Caller arcLines per arc | 4 (each with 1 line) |
| Total line entries per arc | 5 × 7 + 4 × 1 = 39 entries |
| Total Vern dialogue variants | 17 arcs × 5 lines × 7 moods = 595 audio files |
| Total Caller audio files | 17 arcs × 4 lines = 68 audio files |

This system provides **unique Vern experiences for each mood** with nested line structure for better organization.
