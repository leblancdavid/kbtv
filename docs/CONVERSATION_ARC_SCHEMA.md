# KBTV - Arc JSON Schema

This document defines the JSON schema for conversation arc files.

## Full Arc Example

Below is a complete example arc for a Credible UFO caller (dashcam trucker) with all 5 mood variants.

```json
{
  "arcId": "ufo_credible_dashcam",
  "topic": "UFOs",
  "legitimacy": "Credible",
  "moodVariants": {
    "Tired": {
      "intro": [
        { "speaker": "Vern", "text": "*yawns* Alright, {callerName} from {callerLocation}. What've you got?" },
        { "speaker": "Caller", "text": "I've been driving trucks for twenty-two years. Last night I saw something." }
      ],
      "development": [
        { "speaker": "Vern", "text": "Mm-hmm. Go on." },
        { "speaker": "Caller", "text": "Triangular shape, three lights. Silent. Paced my rig for two miles." },
        { "speaker": "Vern", "text": "Two miles. Then what?" },
        { "speaker": "Caller", "text": "Accelerated and was gone. I've got dash cam footage." }
      ],
      "beliefBranch": {
        "Skeptical": [
          { "speaker": "Vern", "text": "Could've been a drone. Military runs tests out there." },
          { "speaker": "Caller", "text": "I know aircraft, Vern. Twenty-two years. This was different." }
        ],
        "Believing": [
          { "speaker": "Vern", "text": "Dash cam footage. That's what we need. Real evidence." },
          { "speaker": "Caller", "text": "I knew you'd understand. My dispatcher didn't believe me." }
        ]
      },
      "conclusion": [
        { "speaker": "Vern", "text": "Send that footage in. Appreciate the call." },
        { "speaker": "Caller", "text": "Will do. Thanks, Vern." }
      ]
    },
    "Grumpy": {
      "intro": [
        { "speaker": "Vern", "text": "{callerName}, {callerLocation}. Make it quick." },
        { "speaker": "Caller", "text": "Uh, sure. I drive trucks. Twenty-two years. Saw something last night." }
      ],
      "development": [
        { "speaker": "Vern", "text": "What kind of something?" },
        { "speaker": "Caller", "text": "Triangle. Three lights. No sound. Followed me for two miles." },
        { "speaker": "Vern", "text": "And?" },
        { "speaker": "Caller", "text": "Shot off like nothing I've ever seen. Got it on dash cam." }
      ],
      "beliefBranch": {
        "Skeptical": [
          { "speaker": "Vern", "text": "Probably military. They test stuff out there all the time." },
          { "speaker": "Caller", "text": "I've seen military aircraft. This wasn't that." }
        ],
        "Believing": [
          { "speaker": "Vern", "text": "Dash cam. Good. At least someone brings evidence." },
          { "speaker": "Caller", "text": "I figured you'd want proof. Most people just laugh." }
        ]
      },
      "conclusion": [
        { "speaker": "Vern", "text": "Send the footage. Next caller." },
        { "speaker": "Caller", "text": "Will do. Thanks." }
      ]
    },
    "Neutral": {
      "intro": [
        { "speaker": "Vern", "text": "We've got {callerName} on the line from {callerLocation}. What's on your mind tonight?" },
        { "speaker": "Caller", "text": "Vern, I've been driving trucks for twenty-two years. Last night, I saw something I can't explain." }
      ],
      "development": [
        { "speaker": "Vern", "text": "Alright, walk me through it. What exactly did you see?" },
        { "speaker": "Caller", "text": "Triangular craft, three amber lights at each point. Completely silent. It paced my rig for two miles." },
        { "speaker": "Vern", "text": "Two miles is a long time. What happened next?" },
        { "speaker": "Caller", "text": "It accelerated and was gone in maybe two seconds. I've got dash cam footage of the whole thing." }
      ],
      "beliefBranch": {
        "Skeptical": [
          { "speaker": "Vern", "text": "Could've been a drone. Military runs tests in that area." },
          { "speaker": "Caller", "text": "Vern, I know aircraft. Twenty-two years on the road. This was something else entirely." }
        ],
        "Believing": [
          { "speaker": "Vern", "text": "Dash cam footage. Now that's what I'm talking about. Real evidence." },
          { "speaker": "Caller", "text": "I knew you'd understand. My dispatcher thinks I'm crazy." }
        ]
      },
      "conclusion": [
        { "speaker": "Vern", "text": "Send that footage to the station. Appreciate the call." },
        { "speaker": "Caller", "text": "Will do, Vern. Keep up the good work." }
      ]
    },
    "Engaged": {
      "intro": [
        { "speaker": "Vern", "text": "{callerName} from {callerLocation}, you're on the air. What've you got for us?" },
        { "speaker": "Caller", "text": "Vern, I've been a trucker for twenty-two years. Last night changed everything I thought I knew." }
      ],
      "development": [
        { "speaker": "Vern", "text": "I like the sound of that. Tell me everything." },
        { "speaker": "Caller", "text": "Triangular craft. Three amber lights. Dead silent. It matched my speed for two full miles." },
        { "speaker": "Vern", "text": "Two miles! That's intentional. That's not random." },
        { "speaker": "Caller", "text": "Exactly what I thought. Then it shot off faster than anything I've ever seen. Got it all on dash cam." }
      ],
      "beliefBranch": {
        "Skeptical": [
          { "speaker": "Vern", "text": "Now hold on - military runs black projects out there. Could be experimental." },
          { "speaker": "Caller", "text": "Vern, I've seen every aircraft type there is. This broke physics." }
        ],
        "Believing": [
          { "speaker": "Vern", "text": "Dash cam footage! That's exactly what we need. Documentation!" },
          { "speaker": "Caller", "text": "I knew you'd get it. Everyone else thinks I'm losing my mind." }
        ]
      },
      "conclusion": [
        { "speaker": "Vern", "text": "Send that footage in immediately. This could be huge." },
        { "speaker": "Caller", "text": "Already uploading it. Thanks for believing me, Vern." }
      ]
    },
    "Excited": {
      "intro": [
        { "speaker": "Vern", "text": "Folks, we've got {callerName} calling in from {callerLocation}! What've you got?!" },
        { "speaker": "Caller", "text": "Vern! Twenty-two years driving trucks and last night I finally SAW one!" }
      ],
      "development": [
        { "speaker": "Vern", "text": "Tell me! Tell me everything!" },
        { "speaker": "Caller", "text": "Triangle! Three lights! Silent as death! It FOLLOWED me for two miles, Vern!" },
        { "speaker": "Vern", "text": "Two miles! It was watching you! What happened?!" },
        { "speaker": "Caller", "text": "GONE. Like a bullet. But I got it! I got it on dash cam!" }
      ],
      "beliefBranch": {
        "Skeptical": [
          { "speaker": "Vern", "text": "Wait wait wait - could've been military. They test crazy stuff out there." },
          { "speaker": "Caller", "text": "Vern, nothing human moves like that. Nothing." }
        ],
        "Believing": [
          { "speaker": "Vern", "text": "DASH CAM! You got it on CAMERA! THIS is what I'm talking about folks!" },
          { "speaker": "Caller", "text": "I KNEW you'd believe me! Everyone else thinks I'm nuts!" }
        ]
      },
      "conclusion": [
        { "speaker": "Vern", "text": "Send that footage NOW! This is incredible!" },
        { "speaker": "Caller", "text": "Sending it right now! Thanks Vern!" }
      ]
    }
  }
}
```

## Schema Reference

### Root Object

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `arcId` | string | Yes | Unique identifier (format: `{topic}_{legitimacy}_{descriptor}`) |
| `topic` | string | Yes | The actual topic: "UFOs", "Cryptids", "Conspiracies", "Ghosts" |
| `claimedTopic` | string | No | For topic-switcher arcs: what the caller claimed during screening |
| `legitimacy` | string | Yes | One of: "Fake", "Questionable", "Credible", "Compelling" |
| `moodVariants` | object | Yes | Contains variants for each mood |

## Topic-Switcher Arcs

Topic-switcher arcs handle callers who lied about their topic during screening. These arcs have both a `claimedTopic` (what they said) and `topic` (what they actually talk about).

### When They're Used

When a caller's `ClaimedTopic` differs from their `ActualTopic`, the system first looks for a matching topic-switcher arc. If none is found, it falls back to a regular arc matching the actual topic.

### Example Topic-Switcher Arc

```json
{
  "arcId": "cryptid_credible_claims_ufos",
  "claimedTopic": "UFOs",
  "topic": "Cryptids",
  "legitimacy": "Credible",
  "moodVariants": {
    "Neutral": {
      "intro": [
        { "speaker": "Vern", "text": "{callerName} from {callerLocation}, you said you saw something in the sky?" },
        { "speaker": "Caller", "text": "Well, I was out looking for lights when I heard something crash through the trees..." }
      ],
      "development": [
        { "speaker": "Vern", "text": "Hold on. That doesn't sound like a UFO." },
        { "speaker": "Caller", "text": "I know, I said UFOs to get through. But Vern, I saw a creature." },
        { "speaker": "Vern", "text": "A creature. What kind?" },
        { "speaker": "Caller", "text": "Bipedal. Seven feet tall. Covered in dark fur." }
      ]
    }
  }
}
```

### Naming Convention for Switcher Arcs

Place in the **actual topic** folder with a `claims_` prefix:

```
Assets/Data/Dialogue/Arcs/Cryptids/Credible/claims_ufos.json
```

This is a Cryptids arc where the caller claimed UFOs.

### Mood Variants Object

Must contain all 5 keys:
- `Tired`
- `Grumpy`
- `Neutral`
- `Engaged`
- `Excited`

Each mood variant contains the arc phases.

### Arc Phase Structure

| Field | Type | Line Count | Description |
|-------|------|------------|-------------|
| `intro` | DialogueLine[] | 2 | Vern greets, Caller makes claim |
| `development` | DialogueLine[] | 2-4 | Back-and-forth elaboration |
| `beliefBranch` | object | 0-4 | Skeptical and Believing paths |
| `conclusion` | DialogueLine[] | 2 | Wrap up and sign off |

### Belief Branch Object

| Field | Type | Description |
|-------|------|-------------|
| `Skeptical` | DialogueLine[] | Vern challenges, Caller defends |
| `Believing` | DialogueLine[] | Vern validates, Caller appreciates |

**Note:** For Fake callers, `beliefBranch` can be omitted or empty.

### DialogueLine Object

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `speaker` | string | Yes | "Vern" or "Caller" |
| `text` | string | Yes | The dialogue text (supports placeholders) |

### Placeholders

| Placeholder | Replaced With |
|-------------|---------------|
| `{callerName}` | Caller's name from CallerProfile |
| `{callerLocation}` | Caller's location from CallerProfile |
| `{topic}` | Current topic display name |

## File Naming Convention

```
{descriptor}.json
```

Examples:
- `dashcam_trucker.json`
- `prankster.json`
- `military_pilot.json`
- `backyard_sighting.json`

The `arcId` inside the file should follow the pattern: `{topic}_{legitimacy}_{descriptor}`

## Validation Rules

1. All 5 mood variants must be present
2. Each mood variant must have intro, development, conclusion
3. Intro and conclusion must have exactly 2 lines
4. Development line count must match legitimacy requirements
5. Line alternation: Vern speaks on odd lines (1, 3, 5...), Caller on even (2, 4, 6...)
6. `arcId` must be unique across all arc files
