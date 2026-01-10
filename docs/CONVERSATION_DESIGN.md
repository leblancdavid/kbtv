# KBTV - Conversation System Design

## Overview

The conversation system generates and plays back dynamic dialogue between Vern Tell (the host) and callers during the LiveShow phase. Conversations are procedurally generated based on caller attributes, Vern's current stats, and the night's topic.

## Design Goals

1. **Variety** - Callers feel distinct based on legitimacy and topic
2. **Reactivity** - Vern's responses adapt to his mood, energy, belief, etc.
3. **Pacing** - Conversations flow naturally with appropriate timing
4. **Extensibility** - Easy to add new dialogue via ScriptableObject templates

## Conversation Structure

Each call follows a 4-phase structure (8 total lines):

### Phase 1: Intro (2 lines)

| Speaker | Purpose | Example |
|---------|---------|---------|
| Vern | Introduces caller to audience | "We've got Dale on the line from Roswell. What's on your mind tonight?" |
| Caller | Makes initial claim/statement | "Vern, you're not gonna believe this, but I saw something in the desert last week." |

### Phase 2: Probe (2 lines)

| Speaker | Purpose | Example |
|---------|---------|---------|
| Vern | Asks follow-up question | "Alright, walk me through it. What exactly did you see?" |
| Caller | Elaborates on their story | "It was around 2 AM, this bright light hovering over the mesa. Silent. Then it just... vanished." |

### Phase 3: Challenge (2 lines)

| Speaker | Purpose | Example (Skeptical) | Example (Believing) |
|---------|---------|---------------------|---------------------|
| Vern | Challenges or accepts | "Now hold on, could've been a helicopter or a drone..." | "See, THIS is what I'm talking about, folks." |
| Caller | Defends or expands | "No way, I know what a helicopter sounds like." | "I knew you'd understand, Vern. You're the only one who gets it." |

### Phase 4: Resolution (2 lines)

| Speaker | Purpose | Example |
|---------|---------|---------|
| Vern | Wraps up the call | "Appreciate the call, Dale. Keep your eyes on the skies." |
| Caller | Signs off | "Thanks Vern. I'll keep you posted if it comes back." |

## Vern's Response Types

Vern's Challenge phase response is selected based on his stats and the caller's legitimacy:

| Response Type | Trigger Conditions | Tone |
|---------------|-------------------|------|
| **Believing** | High Belief + Credible/Compelling caller | Excited, validating |
| **Skeptical** | Low Belief OR Questionable caller | Doubtful, probing |
| **Dismissive** | Low Belief + Fake caller | Uninterested, cutting |
| **Annoyed** | Low Patience + Low Mood | Impatient, short |
| **Tired** | Low Energy (< 20%) | Exhausted, brief |

### Stat Thresholds

```
Energy < 20%                 -> Tired responses
Patience < 30% + Mood < 40%  -> Annoyed responses
Belief > 50% + Credible+     -> Believing responses
Belief < 30% OR Fake caller  -> Skeptical/Dismissive
```

## Caller Dialogue Variation

Callers have different dialogue pools based on **Legitimacy**. Within each legitimacy level, lines have **tonal variety** (serious, dramatic, comedic, nervous) baked in to create natural variation without requiring separate template assets per tone.

### Legitimacy Levels

| Level | Characteristics | Tones Used |
|-------|-----------------|------------|
| **Fake** | Generic claims, weak details, flustered when challenged | Nervous, Neutral |
| **Questionable** | Vague, hedging language, uncertain | Nervous, Confused |
| **Credible** | Specific details, consistent story, calm under pressure | Neutral, Excited |
| **Compelling** | Strong hook, evidence references, confident | Dramatic, Conspiratorial |

### Tonal Variety Approach

Rather than creating separate templates for each tone, we mix tones within each legitimacy level's line pool. Each `DialogueTemplate` has a `Tone` field from `DialogueTone` enum:

- `Neutral` - Normal, conversational
- `Excited` - Enthusiastic, energetic  
- `Nervous` - Anxious, uncertain
- `Dramatic` - Intense, urgent
- `Conspiratorial` - Secretive, hushed
- `Confused` - Uncertain, questioning

This gives natural variety - a Credible caller might be earnestly excited in one call, calmly matter-of-fact in another.

## Topic-Specific Dialogue

Templates can be scoped to specific topics for flavor:

| Topic | Caller Vocabulary | Vern References |
|-------|------------------|-----------------|
| UFOs | "craft", "lights", "beings", "abduction" | Area 51, Roswell, black helicopters |
| Cryptids | "creature", "tracks", "howling", "sighting" | Bigfoot, Mothman, local legends |
| Ghosts | "presence", "cold spots", "voices", "orbs" | EMF readings, spirit communication |
| Conspiracies | "they", "cover-up", "documents", "agenda" | Deep state, false flags |

## Text Substitution Placeholders

Templates support these placeholders:

| Placeholder | Replaced With | Example |
|-------------|--------------|---------|
| `{callerName}` | Caller.Name | "Dale" |
| `{location}` | Caller.Location | "Roswell, NM" |
| `{topic}` | Topic.DisplayName or Caller.ClaimedTopic | "UFO sightings" |
| `{reason}` | Caller.CallReason | "I saw something strange" |

## Timing & Pacing

### Line Duration Calculation

```
Duration = BaseDelay + (CharacterCount * PerCharDelay)
         = 1.5s + (text.Length * 0.04s)
```

Example: "I saw a bright light over the mesa." (37 chars)
-> 1.5 + (37 * 0.04) = 2.98 seconds

### Speaker Transition Delay

- 0.5 second pause between different speakers
- Same speaker back-to-back: no pause

### Approximate Call Duration

- Average line: ~60 chars = 3.9 seconds
- 8 lines * 3.9s = ~31 seconds
- Plus transitions: ~34-38 seconds per call

## Template Asset Structure

### VernDialogueTemplate (ScriptableObject)

```
VernDialogueTemplate
├── Introduction Lines[]
├── Probing Lines[]
├── Skeptical Lines[]
├── Dismissive Lines[]
├── Believing Lines[]
├── Tired Lines[]
├── Annoyed Lines[]
└── SignOff Lines[]
```

### CallerDialogueTemplate (ScriptableObject)

```
CallerDialogueTemplate
├── Matching Criteria
│   ├── Topic (optional, null = any)
│   ├── Legitimacy (required)
│   └── Priority (for multi-match)
├── Intro Lines[]
├── Detail Lines[]
├── Defense Lines[]
├── Acceptance Lines[]
└── Conclusion Lines[]
```

## JSON-Based Dialogue System

Dialogue content is stored in JSON files and loaded into ScriptableObjects during Editor setup. This separation makes it easy to edit dialogue without modifying C# code.

### Directory Structure

```
Assets/Data/Dialogue/
├── Vern/
│   └── VernDialogue.json       # All of Vern's response lines
├── UFO/
│   ├── Fake_Prankster.json     # Obvious joke callers (Short)
│   ├── Fake_Vague.json         # Can't provide details (Short)
│   ├── Fake_Generic.json       # Generic fake caller (Short)
│   ├── Questionable_Sleepy.json    # 3AM sighting, half asleep (Standard)
│   ├── Questionable_Distance.json  # Saw lights from highway (Standard)
│   ├── Questionable_Generic.json   # Generic questionable (Standard)
│   ├── Credible_Trucker.json   # Long-haul driver, dash cam (Extended)
│   ├── Credible_Family.json    # Whole family saw it (Extended)
│   ├── Credible_Generic.json   # Generic credible (Extended)
│   ├── Compelling_Military.json    # Retired pilot, radar contact (Long)
│   ├── Compelling_Repeated.json    # 10 years of sightings (Long)
│   └── Compelling_Generic.json     # Generic compelling (Long)
├── Generic/
│   ├── Fake.json               # Fallback for any topic
│   ├── Questionable.json
│   ├── Credible.json
│   └── Compelling.json
├── Cryptids/                   # TODO: Migrate from inline code
├── Ghosts/                     # TODO: Migrate from inline code
└── Conspiracies/               # TODO: Migrate from inline code
```

### Conversation Lengths

Templates specify a `length` field that determines conversation structure:

| Length | Lines | Structure | Use Case |
|--------|-------|-----------|----------|
| Short | 6 | Intro(2) → Probe(2) → Resolution(2) | Fake callers, quick dismissals |
| Standard | 8 | Intro(2) → Probe(2) → Challenge(2) → Resolution(2) | Most callers |
| Extended | 10 | Standard + ExtraProbe(2) | Credible callers with more detail |
| Long | 12 | Extended + ExtraChallenge(2) | Compelling callers, full story |

### Priority System

When multiple templates match the same Topic+Legitimacy:
- Higher `priority` value = preferred template
- Priority 2 = specific archetypes (Prankster, Trucker, Military)
- Priority 1 = generic fallback for that topic
- Priority 0 = global generic fallback

### JSON Format: Vern Dialogue

```json
{
  "introductionLines": [
    { "text": "Alright night owls, we've got {callerName}...", "tone": "Neutral", "weight": 1.0 }
  ],
  "probingLines": [...],
  "extraProbingLines": [...],
  "skepticalLines": [...],
  "dismissiveLines": [...],
  "believingLines": [...],
  "tiredLines": [...],
  "annoyedLines": [...],
  "engagingLines": [...],
  "cutOffLines": [...],
  "signOffLines": [...]
}
```

### JSON Format: Caller Dialogue

```json
{
  "topicId": "UFOs",
  "legitimacy": "Credible",
  "length": "Extended",
  "priority": 2,
  "introLines": [
    { "text": "Vern, my whole family saw it...", "tone": "Nervous", "weight": 1.0 }
  ],
  "detailLines": [...],
  "defenseLines": [...],
  "acceptanceLines": [...],
  "extraDetailLines": [...],
  "extraDefenseLines": [...],
  "conclusionLines": [...]
}
```

### Adding New Templates

1. Create a new JSON file in the appropriate topic folder
2. Set `topicId` to match the Topic asset's TopicId field
3. Set `legitimacy` to one of: "Fake", "Questionable", "Credible", "Compelling"
4. Set `length` to one of: "Short", "Standard", "Extended", "Long"
5. Set `priority` (higher = preferred when multiple templates match)
6. Fill in dialogue arrays
7. Run **KBTV > Setup Game Scene** or **KBTV > Reload Dialogue From JSON**

### Reloading Dialogue

Use **KBTV > Reload Dialogue From JSON** menu to regenerate ScriptableObject assets from JSON files. This:
1. Deletes existing dialogue assets in `Assets/Data/Dialogue/Assets/`
2. Scans JSON folders and creates fresh ScriptableObjects
3. Useful after editing JSON files

### Valid Tone Values

- `Neutral` - Normal, conversational
- `Excited` - Enthusiastic, energetic
- `Nervous` - Anxious, uncertain
- `Dramatic` - Intense, urgent
- `Conspiratorial` - Secretive, hushed
- `Confused` - Uncertain, questioning
- `Skeptical` - Doubtful
- `Dismissive` - Uninterested
- `Annoyed` - Impatient
- `Believing` - Accepting, validating

---

## Dialogue Content (UFO Topic)

**Note:** This section is now for reference only. Actual dialogue is stored in JSON files in `Assets/Data/Dialogue/`. See the JSON files for current content.

### Vern Dialogue (VernDialogue.asset)

#### Introduction Lines
| Text | Tone |
|------|------|
| "We've got {callerName} on the line from {location}. What've you got for us tonight?" | Neutral |
| "{callerName}, you're on the air. Go ahead." | Neutral |
| "Next up, {callerName} calling in from {location}. Talk to me." | Neutral |

#### Probing Lines
| Text | Tone |
|------|------|
| "Alright, walk me through this. What exactly did you see?" | Neutral |
| "And when did this happen? Give me the details." | Neutral |
| "Hold on, slow down. Start from the beginning." | Neutral |

#### Skeptical Lines
| Text | Tone |
|------|------|
| "Now hold on a second, that could've been anything up there." | Skeptical |
| "I gotta be honest with you, I'm not buying it." | Skeptical |
| "You sure it wasn't just a plane? A satellite?" | Skeptical |

#### Dismissive Lines
| Text | Tone |
|------|------|
| "Yeah, okay. Thanks for calling in." | Dismissive |
| "Right. We're gonna move on to the next caller." | Dismissive |
| "Uh-huh. Appreciate it." | Dismissive |

#### Believing Lines
| Text | Tone |
|------|------|
| "See, THIS is what I'm talking about, folks." | Excited |
| "Now that's fascinating. I believe you." | Believing |
| "This is exactly the kind of call I live for." | Excited |

#### Tired Lines
| Text | Tone |
|------|------|
| "Mm-hmm. Go on." | Neutral |
| "Alright. What else?" | Neutral |
| "Yeah... okay..." | Neutral |

#### Annoyed Lines
| Text | Tone |
|------|------|
| "Get to the point, caller." | Annoyed |
| "We don't have all night here." | Annoyed |
| "Is there a point to this story?" | Annoyed |

#### SignOff Lines
| Text | Tone |
|------|------|
| "Appreciate the call. Keep watching the skies." | Neutral |
| "Thanks for sharing that with us tonight." | Neutral |
| "Stay vigilant out there, {callerName}." | Neutral |

---

### UFO Caller Dialogue - Fake (UFO_Fake.asset)

**Matching:** Topic=UFOs, Legitimacy=Fake, Priority=0

#### Intro Lines
| Text | Tone |
|------|------|
| "Yeah so I totally saw a UFO last night, it was crazy." | Neutral |
| "Dude, Vern, you're not gonna believe this..." | Excited |
| "Uh, hi, first time caller... I saw some lights?" | Nervous |

#### Detail Lines
| Text | Tone |
|------|------|
| "It was like... really bright. And flying around." | Neutral |
| "It did a bunch of loops and stuff. Very alien-like." | Excited |
| "I didn't get a video but trust me it was wild." | Nervous |

#### Defense Lines
| Text | Tone |
|------|------|
| "I mean, I know what I saw, man." | Nervous |
| "Why would I lie about this?" | Nervous |
| "My buddy saw it too, he's just not here right now." | Nervous |

#### Acceptance Lines
| Text | Tone |
|------|------|
| "Uh, yeah, exactly what I was thinking." | Nervous |
| "See? Someone gets it." | Excited |
| "That's... yeah, that." | Confused |

#### Conclusion Lines
| Text | Tone |
|------|------|
| "Cool, thanks Vern." | Neutral |
| "Peace out, keep it real." | Neutral |
| "Alright, later." | Neutral |

---

### UFO Caller Dialogue - Questionable (UFO_Questionable.asset)

**Matching:** Topic=UFOs, Legitimacy=Questionable, Priority=0

#### Intro Lines
| Text | Tone |
|------|------|
| "I think I might have seen something strange in the sky..." | Nervous |
| "I'm not sure what it was, but I had to call." | Nervous |
| "This might sound crazy, but hear me out..." | Nervous |

#### Detail Lines
| Text | Tone |
|------|------|
| "It could have been a plane, but the lights were blinking weird." | Confused |
| "It hovered for a bit, then just... moved off." | Neutral |
| "I was half asleep, but I'm pretty sure I saw something." | Confused |

#### Defense Lines
| Text | Tone |
|------|------|
| "I'm not saying it was aliens, but it wasn't normal." | Neutral |
| "Look, I'm just telling you what I saw." | Nervous |
| "I know how this sounds, okay?" | Nervous |

#### Acceptance Lines
| Text | Tone |
|------|------|
| "That's what I thought too." | Neutral |
| "Yeah, maybe you're right." | Neutral |
| "I hadn't considered that." | Confused |

#### Conclusion Lines
| Text | Tone |
|------|------|
| "Thanks for hearing me out, Vern." | Neutral |
| "I just needed to tell someone." | Nervous |
| "Okay, thanks. I feel better now." | Neutral |

---

### UFO Caller Dialogue - Credible (UFO_Credible.asset)

**Matching:** Topic=UFOs, Legitimacy=Credible, Priority=0

#### Intro Lines
| Text | Tone |
|------|------|
| "Vern, I've been listening for years and I finally have my own sighting to report." | Neutral |
| "I never thought I'd be making this call, but here I am." | Neutral |
| "So I was out stargazing Tuesday night when something appeared that wasn't on any chart." | Excited |

#### Detail Lines
| Text | Tone |
|------|------|
| "Three amber lights in a triangle formation, completely silent, hovering for about two minutes before shooting off." | Neutral |
| "It moved against the wind, no sound, no blinking lights like a plane would have." | Neutral |
| "I timed it - ninety seconds of hovering, then acceleration that would flatten any pilot." | Excited |

#### Defense Lines
| Text | Tone |
|------|------|
| "I've worked at the airport for fifteen years. I know aircraft. This wasn't one." | Neutral |
| "I'm an amateur astronomer. I know what satellites look like. This was different." | Neutral |
| "My whole family saw it. We're not all crazy, Vern." | Neutral |

#### Acceptance Lines
| Text | Tone |
|------|------|
| "I knew you'd understand. Nobody else believes me." | Excited |
| "Thank god. My wife thinks I've lost it." | Nervous |
| "That's exactly what I was hoping to hear." | Excited |

#### Conclusion Lines
| Text | Tone |
|------|------|
| "Thanks Vern. I'll send you the photos if I can get them developed." | Neutral |
| "Keep doing what you're doing. People need to hear this stuff." | Neutral |
| "I'll keep watching and call back if it happens again." | Excited |

---

### UFO Caller Dialogue - Compelling (UFO_Compelling.asset)

**Matching:** Topic=UFOs, Legitimacy=Compelling, Priority=0

#### Intro Lines
| Text | Tone |
|------|------|
| "Vern, I'm a retired Air Force pilot, and what I saw last week has kept me up every night since." | Dramatic |
| "I have documentation, Vern. Radar logs. Witness statements. This is real." | Dramatic |
| "I was told never to speak of this. But after thirty years, I can't stay silent anymore." | Conspiratorial |

#### Detail Lines
| Text | Tone |
|------|------|
| "Radar contact at 40,000 feet, no transponder, performed maneuvers that would kill any human pilot. Command told us to forget it happened." | Dramatic |
| "We tracked it for eighteen minutes. It descended from 80,000 feet to sea level in under four seconds. Nothing we have can do that." | Dramatic |
| "The craft was disc-shaped, metallic, approximately 40 feet in diameter. I was close enough to see the seams." | Neutral |

#### Defense Lines
| Text | Tone |
|------|------|
| "They can discredit me all they want. I have my flight logs, I have witnesses. This happened." | Dramatic |
| "I've got nothing to gain from this and everything to lose. Why would I make it up?" | Neutral |
| "You think I want to be called crazy? I'm risking my pension for this." | Dramatic |

#### Acceptance Lines
| Text | Tone |
|------|------|
| "That's exactly right. They don't want us talking about this." | Conspiratorial |
| "Finally, someone who understands what's at stake here." | Dramatic |
| "You're one of the few who gets it, Vern." | Neutral |

#### Conclusion Lines
| Text | Tone |
|------|------|
| "Keep doing what you're doing, Vern. The truth needs to get out." | Dramatic |
| "I've said my piece. Do with it what you will." | Neutral |
| "Stay safe, Vern. They're listening." | Conspiratorial |

---

## Implementation Status

### Phase 1: Core Flow (Complete)

- [x] DialogueTypes (Speaker, Tone, Phase, State, DialogueLine)
- [x] Conversation (line container, playback state, events)
- [x] ConversationManager (timing, CallerQueue integration)
- [x] ConversationGenerator (procedural generation with fallbacks)
- [x] ConversationPanel (UI display)

### Phase 2: Dialogue Content (Complete)

- [x] Design dialogue content for Vern (all response types)
- [x] Design dialogue content for UFO callers (all legitimacy levels)
- [x] Update GameSetup.cs to create dialogue template assets programmatically
- [x] Update GameBootstrap.cs to wire dialogue templates to ConversationManager
- [x] Create VernDialogue.asset via GameSetup
- [x] Create UFO caller templates (Fake, Questionable, Credible, Compelling) via GameSetup

### Phase 3: Expand Content (Complete)

- [x] Add Cryptids topic templates
- [x] Add Ghosts topic templates  
- [x] Add Conspiracies topic templates
- [x] Add generic fallback templates (Topic=null)

### Phase 4: Polish (Complete)

- [x] Typewriter text effect
- [x] Dialogue history / transcript (shows last 3 lines with speaker prefix)
- [x] Audio hooks for future TTS/voice acting (dialogue blips, speaker change SFX, voice AudioSource)
- [x] Response indicators (animated thinking dots between speaker turns)

## Future Considerations

### Dynamic Length

- Some callers could have longer/shorter calls
- Add optional extra probe/challenge exchanges for compelling callers

### Player Intervention

- "Cut the caller" button ends conversation early
- "Steer the conversation" prompts that affect next line selection

### Memorable Callers

- Certain callers could have unique dialogue trees
- Recurring callers with continuity
