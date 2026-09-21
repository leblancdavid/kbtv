# KBTV - Audio Generation Guide

This document covers how to generate voice audio for KBTV using the ElevenLabs API.

## Overview

KBTV uses ElevenLabs AI voice synthesis with:
- **Vern**: Custom voice clone (ID: `cD12ZqbaUeADFL4RycQC`) 
- **Callers**: Pool of 19 diverse voices (13 male, 6 female)

## Prerequisites

### 1. Python & Dependencies
```bash
pip install requests
```

### 2. ElevenLabs API Key
Create `Tools/AudioGeneration/elevenlabs_config.json`:
```json
{
  "elevenlabs_api_key": "your_api_key_here"
}
```

Or set environment variable: `ELEVENLABS_API_KEY=your_key_here`

### 3. Voice Setup
- Vern's voice clone ID is stored in `voice_id.txt`
- Caller voice pools are defined in `elevenlabs_setup.py`

---

## Part 1: Arc JSON Files

### Location
```
assets/dialogue/arcs/{Topic}/{arc_id}.json
```

### Structure

Arc JSON files nest speaker turns, and each turn lists line objects with
`id` / `text` / `voiceText` / `mood`. Vern turns carry all 13 mood variants;
caller turns carry exactly one line:

```json
{
  "arcId": "pilot",
  "topic": "UFOs",
  "legitimacy": "Compelling",
  "callerGender": "male",
  "arcLines": [
    {
      "speaker": "vern",
      "lines": [
        { "id": "ufos_compelling_pilot_vern_neutral_1", "mood": "neutral",
          "text": "You're on the air. You said you have professional experience in aviation?",
          "voiceText": "You're on the air. You said you have professional experience in aviation?" },
        { "id": "ufos_compelling_pilot_vern_tired_1", "mood": "tired", "text": "...", "voiceText": "..." }
      ]
    },
    {
      "speaker": "caller",
      "lines": [
        { "id": "ufos_compelling_pilot_caller_1", "mood": "neutral", "text": "...", "voiceText": "..." }
      ]
    }
  ]
}
```

**The full authoring rules (line id pattern, folder routing, mood list,
enforcement) are in [CONVERSATION_ARC_SCHEMA.md](../ui/CONVERSATION_ARC_SCHEMA.md).**
`ArcSchemaValidationTests` fails on violations.

### Required Fields

| Field | Description |
|-------|-------------|
| `arcId` | Unique; must equal the JSON filename; used as the audio folder name |
| `topic` | Actual topic (UFOs, Ghosts, Cryptids, Conspiracies) |
| `legitimacy` | Fake / Questionable / Credible / Compelling |
| `callerGender` | "male" or "female" - determines voice pool |
| `arcLines` | Array of speaker groups (see schema doc) |

### Editing Guidelines

1. **Asterisk notations** - Replace with spoken words:
   - `*yawn*` → `...`
   - `*grunt*` → `hmm,`
   - `*chuckle*` → `heh,`
   - `*sigh*` → `ah...`

2. **Filler words for callers** - Add to make them sound natural:
   - "uh", "um", "like", "you know", "I mean"
   - Example: "I saw it" → "I mean, like, I saw it, you know?"

3. **Gender field** - Add to each arc JSON:
   ```json
   "callerGender": "male"
   ```
   or
   ```json
   "callerGender": "female"
   ```

### Vern Dialog Files

Location: `assets/dialogue/vern/`

| File | Description |
|------|-------------|
| `openings.json` | Show opening lines (50 lines) |
| `closings.json` | Show closing lines (50 lines) |
| `dead-air-fillers.json` | Filler content between callers |
| `break-transitions.json` | Transition to commercial breaks |
| `return-from-breaks.json` | Coming back from breaks |
| `dropped-callers.json` | When callers disconnect |
| `off-topic-remarks.json` | Responses to off-topic callers |
| `caller-cursed.json` | When callers use profanity |
| `between-callers.json` | Transition between callers |

**Important**: Each line must have a `mood` field:
```json
{
  "id": "opening_ufos_1",
  "text": "Good evening, truth-seekers!",
  "voiceText": "Good evening, truth-seekers!",
  "mood": "neutral",
  "topic": "ufos"
}
```

---

## Part 2: Generating Arc Audio

The generator is JSON-driven: you point it at the arc's `arcId` (its JSON
filename) and it reads all path/naming rules from the JSON itself - the same
rules the runtime uses (see
[CONVERSATION_ARC_SCHEMA.md](../ui/CONVERSATION_ARC_SCHEMA.md)). No
hardcoded arc list.

### Command
```bash
cd Tools/AudioGeneration
python generate_arc_audio.py <arcId|path-to-arc.json> [options]
python generate_arc_audio.py --all [options]
```

### Examples
```bash
# Report missing audio only (no API calls, exit 1 if gaps) - do this first
python generate_arc_audio.py haunted_motel --check

# Generate a Ghosts arc (existing files are skipped automatically)
python generate_arc_audio.py haunted_motel

# Audit every arc under assets/dialogue/arcs/
python generate_arc_audio.py --all --check

# Generate only Vern lines (or --speaker caller)
python generate_arc_audio.py haunted_motel --speaker vern

# Force regenerate (overwrites existing; may change caller voice, see below)
python generate_arc_audio.py haunted_motel --force
```

### Output Location
```
assets/audio/voice/
├── Vern/ConversationArcs/{topicToken}/{arcId}/{line_id}.mp3
└── Callers/{topicToken}/{arcId}/{line_id}.mp3
```
`{topicToken}` is the first token of each line id (`ufo|ufos|ghosts|cryptid|cryptids|conspiracies`),
NOT the arc's `topic` field - this is what keeps topic-switcher arcs consistent.

### Voice Selection

**Vern**: Always uses cloned voice (`cD12ZqbaUeADFL4RycQC`), with mood-based
settings from `MOOD_SETTINGS` in `generate_vern_audio.py` (all 13 moods).

**Callers**: Uses gender-based pools from `elevenlabs_setup.py`:
- 13 male voices
- 6 female voices
- Each arc gets a consistent voice, hashed from the arc's `arcId`.
  (Legacy arcs were generated from a hash of the older full-id argument;
  regenerating one with `--force` may pick a different pool voice. Only
  force-regenerate single arcs deliberately.)

---

## Part 3: Generating Vern Broadcast Audio

### Command
```bash
cd Tools/AudioGeneration
python generate_vern_audio.py [options]
```

### Examples
```bash
# Generate all Vern broadcast audio
python generate_vern_audio.py

# Force regenerate existing files
python generate_vern_audio.py --force

# Verbose output
python generate_vern_audio.py --verbose
```

### Output Location
```
assets/audio/voice/Vern/Broadcast/
├── opening_{mood}_{index}.mp3
├── closing_{mood}_{index}.mp3
├── deadair_{mood}_{index}.mp3
├── break_{mood}_{index}.mp3
├── return_{mood}_{index}.mp3
├── dropped_{mood}_{index}.mp3
├── offtopic_{mood}_{index}.mp3
├── cursed_{mood}_{index}.mp3
└── betweencallers_{mood}_{index}.mp3
```

### Mood Settings

| Mood | Stability | Similarity | Style |
|------|-----------|------------|-------|
| neutral | 0.5 | 0.8 | 0.5 |
| tired | 0.3 | 0.7 | 0.3 |
| energized | 0.7 | 0.9 | 0.8 |
| irritated | 0.6 | 0.8 | 0.4 |
| amused | 0.6 | 0.8 | 0.7 |
| focused | 0.8 | 0.8 | 0.3 |
| gruff | 0.7 | 0.7 | 0.2 |
| exhausted | 0.25 | 0.65 | 0.25 |
| depressed | 0.35 | 0.7 | 0.2 |
| angry | 0.65 | 0.85 | 0.6 |
| frustrated | 0.4 | 0.75 | 0.35 |
| obsessive | 0.75 | 0.85 | 0.5 |
| manic | 0.45 | 0.9 | 0.9 |

---

## Part 4: Testing Voices

### List Available Voices
```bash
python list_voices.py
```
Lists all voices available in your ElevenLabs account.

### Test Voice Qualities
```bash
python test_voices.py
```
Generates sample audio for testing different voices.

### Test Voice Modifications
```bash
python test_voice_mods.py
```
Tests different stability/style settings for mood variations.

---

## Part 5: Troubleshooting

### Coverage Gaps (silent lines in game)

A dialogue line whose mp3 is missing plays as ~4 seconds of silence at
runtime (no crash). Find gaps without spending API credits:

```bash
python generate_arc_audio.py --all --check   # exit 1 lists MISSING files
```

Or via the test suite: `pwsh -NoProfile -File run-tests.ps1 -Filter ConversationArcTests`.

### Quota Exceeded

ElevenLabs has monthly credit limits. If you hit the limit:
1. Wait for monthly reset (or upgrade)
2. Re-run the SAME command without `--force` - existing files are skipped,
   so generation resumes exactly where it stopped.

### Voice ID Not Found

If you see "No voice_id.txt file found":
1. Run elevenlabs_setup.py to upload reference audio
2. Or manually create voice_id.txt with your voice ID

---

## Part 6: File Organization

### Arc Audio Structure
```
assets/audio/voice/
├── Vern/
│   ├── Broadcast/              # Openings, closings, breaks
│   └── ConversationArcs/       # Vern responses per arc
│       ├── UFOs/
│       │   ├── pilot/
│       │   ├── lights/
│       │   └── dashcam_trucker/
│       ├── Ghosts/
│       ├── Cryptids/
│       └── Conspiracies/
└── Callers/
    ├── UFOs/
    ├── Ghosts/
    ├── Cryptids/
    └── Conspiracies/
```

### Naming Convention

Filenames are the arc JSON's line `id` values verbatim plus `.mp3`:

```
Vern arc line:   {topicToken}_{legitimacy}_{descriptor}_vern_{mood}_{seq}.mp3
Caller arc line: {topicToken}_{legitimacy}_{descriptor}_caller_{seq}.mp3
Broadcast line:  {id from assets/dialogue/vern/*.json}.mp3
```

The authoritative pattern (and folder rules) lives in
[CONVERSATION_ARC_SCHEMA.md](../ui/CONVERSATION_ARC_SCHEMA.md).

---

## Part 7: Best Practices

1. **Edit JSON first** - Fix asterisk notations and add filler words before generating audio
2. **Test voices first** - Use test_voices.py to find good caller voices
3. **Generate in batches** - Process multiple arcs without hitting rate limits
4. **Use --force selectively** - Only regenerate what you need
5. **Don't commit audio** - mp3 files are gitignored by design; each machine
   regenerates locally from the JSON (the repo carries the dialogue, not the audio)

---

## Related Documentation

- [VOICE_AUDIO.md](../audio/VOICE_AUDIO.md) - Voice production strategy
- [ELEVENLABS_SETUP.md](../audio/ELEVENLABS_SETUP.md) - ElevenLabs configuration
- [CONVERSATION_ARC_SCHEMA.md](../ui/CONVERSATION_ARC_SCHEMA.md) - Arc JSON structure
