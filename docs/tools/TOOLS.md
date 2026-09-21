# KBTV - Development Tools

This document covers the Python scripts and tools used for KBTV development.

## Tools Overview

| Tool | Location | Purpose |
|------|----------|---------|
| **ElevenLabs Setup** | `Tools/AudioGeneration/elevenlabs_setup.py` | Voice cloning and API management |
| **Arc Audio Generator** | `Tools/AudioGeneration/generate_arc_audio.py` | Generate/verify conversation arc audio (JSON-driven, `--check`, `--all`) |
| **Vern Audio Generator** | `Tools/AudioGeneration/generate_vern_audio.py` | Generate broadcast audio |
| **List Voices** | `Tools/AudioGeneration/list_voices.py` | List available ElevenLabs voices |
| **Test Voices** | `Tools/AudioGeneration/test_voices.py` | Test voice quality samples |
| **Test Voice Mods** | `Tools/AudioGeneration/test_voice_mods.py` | Test mood-based voice settings |

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

The arc audio rules (paths, ids, moods) are defined in
[CONVERSATION_ARC_SCHEMA.md](../ui/CONVERSATION_ARC_SCHEMA.md) and enforced
by `ArcSchemaValidationTests` + `ConversationArcTests`. The generator reads
everything from the arc JSON - no manual arc registration anywhere.

### Basic Usage
```bash
cd Tools/AudioGeneration

# Report missing audio first (no API calls; exit 1 if gaps)
python generate_arc_audio.py pilot --check

# Generate specific arc (both speakers; existing files are skipped)
python generate_arc_audio.py pilot

# Generate only Vern lines
python generate_arc_audio.py pilot --speaker vern

# Generate only caller lines
python generate_arc_audio.py pilot --speaker caller

# Audit every arc
python generate_arc_audio.py --all --check

# Force regenerate existing files
python generate_arc_audio.py pilot --force

# Verbose output
python generate_arc_audio.py pilot --verbose
```

### Output Location
```
assets/audio/voice/
├── Vern/ConversationArcs/{topicToken}/{arcId}/{line_id}.mp3
└── Callers/{topicToken}/{arcId}/{line_id}.mp3
```
`{topicToken}` comes from the first token of the line id (see schema doc).

### Arc JSON Requirements

Each arc JSON must include:
```json
{
  "arcId": "pilot",
  "topic": "UFOs",
  "legitimacy": "Compelling",
  "callerGender": "male",
  "arcLines": [...]
}
```

**Required fields:**
- `arcId` - Unique; equals filename; equals audio folder name
- `topic` - UFOs, Ghosts, Cryptids, or Conspiracies
- `legitimacy` - Fake, Questionable, Credible, or Compelling
- `callerGender` - "male" or "female"
- Vern turns carry all 13 mood variants; caller turns exactly one line

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
assets/audio/voice/Vern/Broadcast/{line id}.mp3
```
Filenames are the `id` fields from the vern JSON files verbatim, e.g.
`opening_ufos_1.mp3`, `betweencallers_neutral_2.mp3`, `break_gruff_1.mp3`.

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
| `between-callers.json` | 34 | Between callers |

**Important**: Each line must have a `mood` and `voiceText` field.

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

### Missing Audio (silent dialogue lines)

Run the coverage check - it reports every JSON line without an mp3, no API
calls:

```bash
python generate_arc_audio.py --all --check
```

Or via tests: `run-tests.ps1 -Filter ConversationArcTests`.

### Quota Exceeded

ElevenLabs has monthly credit limits. If you hit the limit:
1. Wait for monthly reset (or upgrade)
2. Re-run the same command WITHOUT `--force` - existing files are skipped,
   so generation resumes where it stopped

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
│   ├── Broadcast/              # {id}.mp3 from assets/dialogue/vern/*.json
│   │   ├── opening_ufos_1.mp3
│   │   ├── betweencallers_neutral_2.mp3
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

Files are named by their JSON line `id` verbatim (plus `.mp3`). Full id
pattern, mood list, and the line-id-prefix folder routing rule are defined in
[CONVERSATION_ARC_SCHEMA.md](../ui/CONVERSATION_ARC_SCHEMA.md).

---

## Related Documentation

- [AUDIO_GENERATION.md](AUDIO_GENERATION.md) - Detailed audio generation guide
- [VOICE_AUDIO.md](../audio/VOICE_AUDIO.md) - Voice production strategy
- [CONVERSATION_ARC_SCHEMA.md](../ui/CONVERSATION_ARC_SCHEMA.md) - Arc JSON structure
