# KBTV - Development Tools

This document covers the Python scripts and tools used for KBTV development.

## Tools Overview

| Tool | Location | Purpose |
|------|----------|---------|
| **ElevenLabs Setup** | `Tools/AudioGeneration/elevenlabs_setup.py` | Voice cloning and API management |
| **Arc Audio Generator** | `Tools/AudioGeneration/generate_arc_audio.py` | Generate conversation arc audio |
| **Vern Audio Generator** | `Tools/AudioGeneration/generate_vern_audio.py` | Generate broadcast audio |
| **List Voices** | `Tools/AudioGeneration/list_voices.py` | List available ElevenLabs voices |
| **Test Voices** | `Tools/AudioGeneration/test_voices.py` | Test voice quality samples |
| **Test Voice Mods** | `Tools/AudioGeneration/test_voice_mods.py` | Test mood-based voice settings |
| **Extract Arc IDs** | `Tools/AudioGeneration/extract_arc_ids.py` | Find missing audio files |

## Audio Generation System

KBTV generates voice audio using ElevenLabs professional AI voice synthesis with custom voice cloning for Vern.

### Prerequisites

1. **Python 3.9+** - Install from python.org or Microsoft Store

2. **Install Python dependencies:**
   ```bash
   pip install requests
   ```

3. **ElevenLabs API Key** - Required for audio generation
   - Sign up at https://elevenlabs.io/
   - Get API key from https://elevenlabs.io/app/profile
   - Set as environment variable: `ELEVENLABS_API_KEY=your_key_here`
   - Or create `Tools/AudioGeneration/elevenlabs_config.json`:
   ```json
   {
     "elevenlabs_api_key": "your_api_key_here"
   }
   ```

4. **Voice Cloning** - Vern's voice cloned from Art Bell reference audio
   - Voice ID: `cD12ZqbaUeADFL4RycQC` (auto-loaded from `voice_id.txt`)

### Voice Pools

**Vern**: Single cloned voice (`cD12ZqbaUeADFL4RycQC`)

**Caller Voices**: 19-voice pool
- 13 male voices
- 6 female voices
- Each arc gets a consistent voice based on gender and arc ID

---

## Generating Arc Audio

### Basic Usage
```bash
cd Tools/AudioGeneration

# Generate specific arc (both Vern and caller)
python generate_arc_audio.py ufos_pilot

# Generate only Vern lines
python generate_arc_audio.py ufos_pilot --speaker vern

# Generate only caller lines
python generate_arc_audio.py ufos_pilot --speaker caller

# Force regenerate existing files
python generate_arc_audio.py ufos_pilot --force

# Verbose output
python generate_arc_audio.py ufos_pilot --verbose
```

### Output Location
```
assets/audio/voice/
├── Vern/ConversationArcs/{Topic}/{arc_id}/
│   └── {arc_id}_vern_{mood}_{index}.mp3
└── Callers/{Topic}/{arc_id}/
    └── {arc_id}_caller_{index}.mp3
```

### Arc JSON Requirements

Each arc JSON must include:
```json
{
  "arcId": "pilot",
  "topic": "UFOs",
  "callerGender": "male",
  "arcLines": [...]
}
```

**Required fields:**
- `arcId` - Unique arc identifier
- `topic` - UFOs, Ghosts, Cryptids, or Conspiracies
- `callerGender` - "male" or "female"

---

## Generating Vern Broadcast Audio

### Basic Usage
```bash
cd Tools/AudioGeneration

# Generate all broadcast audio
python generate_vern_audio.py

# Force regenerate
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

### Vern Dialog Files

| File | Lines | Description |
|------|-------|-------------|
| `openings.json` | 50 | Show opening lines |
| `closings.json` | 50 | Show closing lines |
| `dead-air-fillers.json` | 50 | Filler content |
| `break-transitions.json` | 35 | Break transitions |
| `return-from-breaks.json` | 50 | Return from breaks |
| `dropped-callers.json` | 35 | Dropped callers |
| `off-topic-remarks.json` | 29 | Off-topic responses |
| `caller-cursed.json` | 19 | Profanity responses |
| `between-callers.json` | 31 | Between callers |

**Important**: Each line must have a `mood` field. Some files had missing mood fields - these have been fixed.

---

## Testing Voices

### List Available Voices
```bash
python list_voices.py
```
Lists all voices available in your ElevenLabs account with IDs.

### Test Voice Qualities
```bash
python test_voices.py
```
Generates sample audio files to test different voice characteristics.

### Test Voice Modifications
```bash
python test_voice_mods.py
```
Tests different stability/style settings to find optimal mood variations.

---

## Editing Dialog Files

### Asterisk Notations

Replace asterisk notations with spoken words:

| Original | Spoken |
|----------|--------|
| `*yawn*` | `...` |
| `*grunt*` | `hmm,` |
| `*chuckle*` | `heh,` |
| `*sigh*` | `ah...` |

### Filler Words for Callers

Add filler words to caller lines to sound more natural:
- "uh", "um", "like", "you know", "I mean"

Example:
- Before: "I saw a UFO last night"
- After: "I mean, like, I saw a UFO last night, you know?"

---

## Troubleshooting

### JSON Syntax Errors

Some Vern dialog files had missing `mood` fields. These have been fixed:
- `between-callers.json`
- `break-transitions.json`
- `dropped-callers.json`
- `off-topic-remarks.json`

### Quota Exceeded

ElevenLabs has monthly credit limits. If you hit the limit:
1. Wait for monthly reset
2. Upgrade your ElevenLabs plan
3. Use `--force` to continue where you left off

### Voice ID Not Found

If you see "No voice_id.txt file found":
1. Run elevenlabs_setup.py to upload reference audio
2. Or manually create voice_id.txt with your voice ID

---

## File Organization

### Directory Structure
```
assets/audio/voice/
├── Vern/
│   ├── Broadcast/              # Show openings/closings
│   │   ├── opening_neutral_1.mp3
│   │   ├── closing_tired_2.mp3
│   │   └── ...
│   └── ConversationArcs/       # Vern conversation responses
│       ├── UFOs/pilot/
│       ├── Ghosts/investigator/
│       └── ...
└── Callers/                    # Caller conversation lines
    ├── UFOs/pilot/
    ├── Ghosts/investigator/
    └── ...
```

### Audio Naming Convention

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

## Related Documentation

- [AUDIO_GENERATION.md](AUDIO_GENERATION.md) - Detailed audio generation guide
- [VOICE_AUDIO.md](../audio/VOICE_AUDIO.md) - Voice production strategy
- [CONVERSATION_ARC_SCHEMA.md](../ui/CONVERSATION_ARC_SCHEMA.md) - Arc JSON structure
