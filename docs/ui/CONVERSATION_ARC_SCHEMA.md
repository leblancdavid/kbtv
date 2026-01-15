# KBTV - Arc JSON Schema

This document defines the JSON schema for conversation arc files.

## Overview

Conversation arcs are pre-scripted dialogues between Vern and callers. Each arc contains:
- A flat dialogue array (not mood-specific - mood-specific text is embedded)
- Vern's lines have 7 unique text variants (one per mood)
- First line is always Vern's intro, last line is always Vern's conclusion
- Metadata for arc selection

## Full Arc Example

```json
{
  "arcId": "ufos_compelling_pilot",
  "topic": "UFOs",
  "legitimacy": "Compelling",
  "callerPersonality": "cold_factual",
  "arcNotes": "Air traffic controller, multiple radar confirmations, Coast Guard visual",
  "dialogue": [
     {
       "speaker": "Vern",
       "textVariants": [
         {"mood": "neutral", "text": "You're on the air. You said you wanted to talk about a UFO sighting?"},
         {"mood": "tired", "text": "Alright, you're on. What's this about a UFO? Make it quick."},
         {"mood": "energized", "text": "YOU ARE ON THE AIR! Finally, someone with a good story - tell us everything!"},
         {"mood": "irritated", "text": "You're on. Look, I've heard a lot of stories tonight - why should I believe yours?"},
         {"mood": "gruff", "text": "You're on. So what'd you see?"},
         {"mood": "amused", "text": "You're on the air! Oh, a UFO sighting? This is going to be good - tell me everything!"},
         {"mood": "focused", "text": "You're on the air. You mentioned having documentation - let's start with the facts."}
       ]
     },
    {
      "speaker": "Caller",
      "text": "Air traffic controller for fifteen years, Vern. I've tracked something that to this day I cannot explain, and I've filed three official incident reports about it."
    },
     {
       "speaker": "Vern",
       "textVariants": [
         {"mood": "neutral", "text": "Three incident reports. That's serious. Tell me what you saw."},
         {"mood": "tired", "text": "Three reports, huh? Alright, I'll listen... what's the gist?"},
         {"mood": "energized", "text": "THREE incident reports?! This is HUGE! Every detail, don't leave anything out!"},
         {"mood": "irritated", "text": "Three reports? You don't say. Let me guess... aliens, right?"},
         {"mood": "gruff", "text": "Three reports. Fine, I'll bite. What'd you see?"},
         {"mood": "amused", "text": "Three official reports! This is fantastic - I've been waiting for a case like this!"},
         {"mood": "focused", "text": "Three incident reports with documentation. That's a paper trail I can work with. Walk me through the timeline."}
       ]
     },
    {
      "speaker": "Caller",
      "text": "November 14th, 2:47 AM. Object appeared on primary radar at 40,000 feet, descending. No transponder, no flight plan. It descended to 100 feet in four seconds."
    },
     {
       "speaker": "Vern",
       "textVariants": [
         {"mood": "neutral", "text": "Four seconds. That rate of descent would destroy conventional aircraft."},
         {"mood": "tired", "text": "Four seconds... yeah, that'd wreck most planes. So what happened next?"},
         {"mood": "energized", "text": "FOUR SECONDS from 40,000 feet?! That's impossible for any known aircraft!"},
         {"mood": "irritated", "text": "Four seconds. Of course it did. Let me guess - it hovered, right?"},
         {"mood": "gruff", "text": "Four seconds. That's fast. Most planes couldn't handle that. Go on."},
         {"mood": "amused", "text": "FOUR SECONDS! The physics don't work for any normal aircraft! This is exactly why I do this show!"},
         {"mood": "focused", "text": "40,000 to 100 feet in four seconds. That's a 12,000 foot per minute descent rate. Impossible for conventional aircraft."}
       ]
     },
    {
      "speaker": "Caller",
      "text": "Correct. Then it hovered over the water for six minutes. Multiple controllers witnessed it. Coast Guard saw it visually before it accelerated straight up and vanished."
    },
     {
       "speaker": "Vern",
       "textVariants": [
         {"mood": "neutral", "text": "Six minutes hovering, multiple witnesses, Coast Guard confirmation. That's multiple independent confirmations."},
         {"mood": "tired", "text": "Six minutes... Coast Guard saw it too. Alright, that's more than one person."},
         {"mood": "energized", "text": "SIX MINUTES of hovering with Coast Guard visual confirmation?! This is TEXTBOOK material!"},
         {"mood": "irritated", "text": "Of course it hovered for six minutes. What'd it do at the end?"},
         {"mood": "gruff", "text": "Six minutes, multiple witnesses, Coast Guard confirmation. That's not nothing."},
         {"mood": "amused", "text": "Multiple controllers, Coast Guard visual, six minutes of hovering - this is as real as it gets!"},
         {"mood": "focused", "text": "Six minutes stationary over water, multiple radar confirmations, visual confirmation. That trajectory data is critical."}
       ]
     },
    {
      "speaker": "Caller",
      "text": "Someone needs to be asking questions at the highest level. I appreciate you giving me the platform, Vern."
    },
     {
       "speaker": "Vern",
       "textVariants": [
         {"mood": "neutral", "text": "Thank you for your testimony. Multiple independent confirmations make this exactly the kind of credible account that needs to be heard."},
         {"mood": "tired", "text": "Yeah, someone should look into this. Thanks for calling in."},
         {"mood": "energized", "text": "We're going to get ANSWERS! This is exactly why this show exists!"},
         {"mood": "irritated", "text": "Right, well... someone should be asking questions. Thanks for calling."},
         {"mood": "gruff", "text": "Someone should be asking questions. That's what we're here for. Thanks for coming forward."},
         {"mood": "amused", "text": "This made you question everything you thought you knew? That's exactly why I do this show! Thank you!"},
         {"mood": "focused", "text": "The highest levels need to see this data. Multiple independent confirmations create a verifiable pattern. Thank you."}
       ]
     }
  ]
}
```

## Schema Reference



### Root Object

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `arcId` | string | Yes | Unique identifier (format: `{topic}_{legitimacy}_{descriptor}`) |
| `topic` | string | Yes | One of: "UFOs", "Cryptids", "Conspiracies", "Ghosts" |
| `claimedTopic` | string | No | For topic-switcher arcs: what the caller claimed during screening |
| `legitimacy` | string | Yes | One of: "Fake", "Questionable", "Credible", "Compelling" |
| `dialogue` | array | Yes | Contains the arc dialogue with mood variants |
| `callerPersonality` | string | No | Personality type for voice direction |
| `arcNotes` | string | No | Author notes for content direction |

## Dialogue Array

The dialogue is a flat array where:
- **Index 0** is always Vern's intro
- **Index N-1** (last) is always Vern's conclusion
- Lines alternate: Vern, Caller, Vern, Caller...
- Vern lines have `textVariants` with all 7 moods
- Caller lines have single `text`

| Index | Speaker | Content |
|-------|---------|---------|
| 0 | Vern | Intro - "You're on the air" |
| 1 | Caller | Initial claim |
| 2 | Vern | Response |
| 3 | Caller | Details |
| 4 | Vern | Response |
| 5 | Caller | Details |
| 6 | Vern | Response |
| 7 | Caller | Closing thought |
| N-1 | Vern | Conclusion |

### Vern Line (textVariants)

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `speaker` | string | Yes | "Vern" |
| `textVariants` | array | Yes | Array of TextVariantData objects (7 moods) |

### TextVariantData Object

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `mood` | string | Yes | Mood identifier (neutral, tired, energized, irritated, gruff, amused, focused) |
| `text` | string | Yes | Dialogue text for this mood |

### textVariants Keys

| Key | Mood | Characteristics |
|-----|------|-----------------|
| `neutral` | Neutral | Professional, balanced, curious |
| `tired` | Tired | Low energy, dismissive, short responses |
| `energized` | Energized | High energy, enthusiastic, lots of punctuation |
| `irritated` | Irritated | Short-tempered, sarcastic, dismissive |
| `gruff` | Gruff | Gruff but engaged, reluctant interest |
| `amused` | Amused | Playful, laughing, finds it entertaining |
| `focused` | Focused | Analytical, detail-oriented, digging for facts |

### Caller Line (text)

| Field | Description |
|-------| Type | Required |------|----------|-------------|
| `speaker` | string | Yes | "Caller" |
| `text` | string | Yes | The dialogue text (supports placeholders) |

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
  "dialogue": [
     {
       "speaker": "Vern",
       "textVariants": [
         {"mood": "neutral", "text": "You're on. You said you had something about lights in the sky?"},
         {"mood": "tired", "text": "Alright, you're on. What's this about lights?"},
         {"mood": "energized", "text": "YOU ARE ON THE AIR! Tell me about these lights!"},
         ...
       ]
     },
    {
      "speaker": "Caller",
      "text": "I... I said that to get through, Vern. That's not what I saw. I was out past Miller Road checking my trail cameras, and I heard something in the trees. Big. Moving through the undergrowth."
    },
    ...
  ],
  "callerPersonality": "nervous_hesitant"
}
```

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

1. Dialogue array must be present
2. First line must be Vern (index 0)
3. Last line must be Vern (index N-1, where N is odd)
4. Lines must alternate: Vern, Caller, Vern, Caller...
5. Vern lines must have `textVariants` with all 7 moods
6. Caller lines must have `text` (no variants)
7. `arcId` must be unique across all arc files
8. All placeholders should be valid

## Content Volume

| Metric | Count |
|--------|-------|
| Topics | 4 (UFOs, Cryptids, Conspiracies, Ghosts) |
| Legitimacy levels | 4 (Fake, Questionable, Credible, Compelling) |
| Arcs per topic × legitimacy | 4-5 |
| Total arcs | 17 |
| Lines per arc (avg) | 9 |
| Vern lines per arc | 5 |
| Caller lines per arc | 4 |
| Total Vern dialogue variants | 17 arcs × 5 lines × 7 moods = 595 audio files |
| Total Caller audio files | 17 arcs × 4 lines = 68 audio files |

This system provides **unique Vern experiences for each mood** while keeping the JSON structure manageable.
