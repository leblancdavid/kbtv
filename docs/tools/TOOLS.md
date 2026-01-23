# KBTV - Development Tools

This document covers the Python scripts and tools used for KBTV development.

## Tools Overview

| Tool | Location | Purpose |
|------|----------|---------|
| **Audio Generation** | `Tools/AudioGeneration/` | Generate voice audio using ElevenLabs API |
| **ElevenLabs Setup** | `Tools/AudioGeneration/elevenlabs_setup.py` | Voice cloning and API management |
| **Arc Audio Generator** | `Tools/AudioGeneration/generate_arc_audio.py` | Generate conversation arc audio |
| **Broadcast Generator** | `Tools/AudioGeneration/generate_vern_broadcast.py` | Generate show broadcast audio |
| **Arc ID Extractor** | `Tools/AudioGeneration/extract_arc_ids.py` | Utility for checking missing audio |

## Audio Generation System

Generates voice audio files for all dialogue using ElevenLabs professional AI voice synthesis with custom voice cloning for Vern.

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

### Core Tools

#### generate_arc_audio.py - Conversation Arc Audio Generator

Generates audio for conversation arcs (Vern responses + caller lines).

**Usage:**
```bash
cd Tools/AudioGeneration

# Generate specific arc
python generate_arc_audio.py conspiracies_compelling_whistleblower

# Generate only Vern lines
python generate_arc_audio.py conspiracies_compelling_whistleblower --speaker vern

# Generate only caller lines
python generate_arc_audio.py conspiracies_compelling_whistleblower --speaker caller

# Generate both (default)
python generate_arc_audio.py conspiracies_compelling_whistleblower --speaker both

# Force regenerate existing files
python generate_arc_audio.py conspiracies_compelling_whistleblower --force

# Verbose output
python generate_arc_audio.py conspiracies_compelling_whistleblower --verbose
```

**Features:**
- Automatic speaker detection (Vern vs Caller)
- Mood-based voice parameters for Vern (7 mood variations)
- Voice archetypes for callers (enthusiastic, nervous, etc.)
- Smart file skipping (only regenerates changed content)
- Automatic folder organization

#### generate_vern_broadcast.py - Broadcast Audio Generator

Generates Vern's broadcast audio (show openings, closings, between-callers, dead air filler).

**Usage:**
```bash
cd Tools/AudioGeneration
python generate_vern_broadcast.py
```

**Features:**
- Generates non-conversation broadcast content
- Uses cloned Vern voice with appropriate tone
- Organizes into Broadcast/ folder

#### elevenlabs_setup.py - Voice Management

Manages ElevenLabs API integration and voice cloning.

**Usage:**
```python
from elevenlabs_setup import ElevenLabsVoiceCloner

cloner = ElevenLabsVoiceCloner()
# Voice ID auto-loaded from voice_id.txt
audio_path = cloner.generate_audio("Hello world", voice_id="cD12ZqbaUeADFL4RycQC")
```

#### extract_arc_ids.py - Utility Script

Extracts arc IDs from missing audio files for batch processing.

**Usage:**
```bash
cd Tools/AudioGeneration
python extract_arc_ids.py
```

### File Organization

Generated audio is automatically organized:

```
assets/audio/voice/
├── Vern/
│   ├── Broadcast/              # Show openings/closings (40 files)
│   └── ConversationArcs/       # Vern conversation responses
│       ├── Conspiracies/       # conspiracies_compelling_whistleblower/
│       ├── Cryptids/           # cryptids_credible_forest_hiker/
│       └── ...
└── Callers/                    # Caller conversation lines
    ├── Conspiracies/           # conspiracies_compelling_whistleblower/
    ├── Cryptids/               # cryptids_credible_forest_hiker/
    └── ...
```

### Troubleshooting

**API Rate Limits:**
- ElevenLabs has request limits - add delays between batches if needed
- Use `--speaker` filtering to process smaller batches

**Voice Quality Issues:**
- Ensure Vern voice ID is correctly loaded (`cD12ZqbaUeADFL4RycQC`)
- Check mood parameters are applying correctly
- Verify caller archetypes are mapping to valid voices

**File Organization:**
- Script automatically creates correct folder structure
- Use `--force` to regenerate specific files if needed