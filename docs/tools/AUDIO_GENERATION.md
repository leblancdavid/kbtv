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
Arc JSON files have a nested structure with `arcLines` containing groups:

```json
{
  "arcId": "pilot",
  "topic": "UFOs",
  "callerGender": "male",
  "arcLines": [
    {
      "speaker": "vern",
      "lines": ["You're on the air. What's your story?", "..."]
    },
    {
      "speaker": "caller", 
      "lines": ["Okay, so this is gonna sound crazy...", "..."]
    }
  ]
}
```

### Required Fields

| Field | Description |
|-------|-------------|
| `arcId` | Unique identifier for the arc |
| `topic` | Topic folder (UFOs, Ghosts, Cryptids, Conspiracies) |
| `callerGender` | "male" or "female" - determines voice pool |
| `arcLines` | Array of speaker groups |

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

### Command
```bash
cd Tools/AudioGeneration
python generate_arc_audio.py <arc_id> [options]
```

### Examples
```bash
# Generate specific arc
python generate_arc_audio.py ufos_pilot

# Generate only Vern lines
python generate_arc_audio.py ufos_pilot --speaker vern

# Generate only caller lines  
python generate_arc_audio.py ufos_pilot --speaker caller

# Force regenerate (overwrite existing)
python generate_arc_audio.py ufos_pilot --force
```

### Output Location
```
assets/audio/voice/
├── Vern/ConversationArcs/{Topic}/{arc_id}/
│   └── {arc_id}_vern_{mood}_{index}.mp3
└── Callers/{Topic}/{arc_id}/
    └── {arc_id}_caller_{index}.mp3
```

### Voice Selection

**Vern**: Always uses cloned voice (`cD12ZqbaUeADFL4RycQC`)

**Callers**: Uses gender-based pools from `elevenlabs_setup.py`:
- 13 male voices
- 6 female voices
- Each arc gets a consistent voice (hashed from arc ID)

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

### JSON Syntax Errors

Several Vern dialog files have known issues - missing `mood` fields:

| File | Issue |
|------|-------|
| `between-callers.json` | Line 45 missing mood |
| `break-transitions.json` | Line 69 missing mood |
| `dropped-callers.json` | Line 69 missing mood |
| `off-topic-remarks.json` | Line 33 missing mood |

**Fix**: Add `"mood": "value"` to the affected lines.

### Quota Exceeded

ElevenLabs has monthly credit limits. If you hit the limit:
1. Wait for monthly reset
2. Upgrade your ElevenLabs plan
3. Use `--force` to continue where you left off:
   ```bash
   python generate_arc_audio.py <arc_id> --force
   python generate_vern_audio.py --force
   ```

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

**Vern arc lines:**
```
{arc_id}_vern_{mood}_{index}.mp3
# Example: ufos_pilot_vern_neutral_1.mp3
```

**Caller arc lines:**
```
{arc_id}_caller_{index}.mp3
# Example: ufos_pilot_caller_1.mp3
```

**Broadcast lines:**
```
{type}_{mood}_{index}.mp3
# Example: opening_neutral_1.mp3
```

---

## Part 7: Best Practices

1. **Edit JSON first** - Fix asterisk notations and add filler words before generating audio
2. **Test voices first** - Use test_voices.py to find good caller voices
3. **Generate in batches** - Process multiple arcs without hitting rate limits
4. **Use --force selectively** - Only regenerate what you need
5. **Commit audio files** - Generated MP3s should be committed to git

---

## Related Documentation

- [VOICE_AUDIO.md](../audio/VOICE_AUDIO.md) - Voice production strategy
- [ELEVENLABS_SETUP.md](../audio/ELEVENLABS_SETUP.md) - ElevenLabs configuration
- [CONVERSATION_ARC_SCHEMA.md](../ui/CONVERSATION_ARC_SCHEMA.md) - Arc JSON structure
