# KBTV - Development Tools

This document covers the Python scripts and tools used for KBTV development.

## Tools Overview

| Tool | Location | Purpose |
|------|----------|---------|
| Audio Generation | `Tools/AudioGeneration/` | Generate voice audio from dialogue JSON using Piper TTS |

## Audio Generation

Generates voice audio files for all dialogue (Vern broadcasts, caller conversations) using Piper TTS.

### Location

```
Tools/AudioGeneration/
├── generate_audio.py     # Main generation script
├── download_voices.py    # Download required Piper voice models
├── config.json           # Voice mappings and audio settings
├── voices/               # Downloaded voice models (gitignored)
└── temp/                 # Temporary files (gitignored)
```

### Prerequisites

1. **Python 3.9+** - Install from python.org or Microsoft Store

2. **Install Python dependencies:**
   ```bash
   pip install piper-tts pydub
   ```

3. **ffmpeg** - Required for OGG conversion
   - Windows: Download from https://ffmpeg.org/download.html and add to PATH
   - Or use: `winget install ffmpeg`

4. **Download Piper voice models** (required before first run):
   ```bash
   cd Tools/AudioGeneration
   python download_voices.py
   ```
   This downloads all 5 voice models to `Tools/AudioGeneration/voices/` (~500MB total).
   The generate_audio.py script automatically uses these local models.

### Usage

```bash
cd Tools/AudioGeneration

# Generate all audio (arcs + Vern broadcasts)
python generate_audio.py

# Generate only conversation arcs
python generate_audio.py --arcs-only

# Generate only Vern broadcast audio
python generate_audio.py --vern-only

# Generate a specific arc
python generate_audio.py --arc ufo_credible_dashcam

# Dry run - list lines without generating
python generate_audio.py --dry-run

# Force regenerate all (ignore cache)
python generate_audio.py --force

# Verbose output
python generate_audio.py --verbose

# Limit to first N lines (for testing)
python generate_audio.py --limit 10

# Check if dependencies are installed
python generate_audio.py --check-deps
```

### Configuration

Edit `config.json` to customize:

- **paths** - Input/output directories
- **audio** - Sample rate, format, quality settings
- **voices.vern** - Vern's voice model and mood adjustments
- **voices.callers** - Caller voice archetypes
- **voices.caller_archetype_mapping** - Which caller voices to use per topic
- **text_processing** - Placeholder replacements, stage direction removal

### Voice Models

The project uses these Piper TTS voices:

| Voice | Model | Used For |
|-------|-------|----------|
| Vern | `en_US-ryan-medium` | Radio host, authoritative |
| Default Male | `en_US-lessac-medium` | Standard male callers |
| Default Female | `en_US-amy-medium` | Standard female callers |
| Gruff | `en_US-ryan-low` | Older, deeper callers |
| Nervous | `en_US-libritts-high` | Younger, higher-pitched callers |

### Output

Generated audio is saved to:
```
kbtv/Assets/Audio/Voice/
├── Vern/Broadcast/
│   ├── Opening/
│   ├── Closing/
│   ├── BetweenCallers/
│   └── DeadAirFiller/
└── Callers/
    ├── UFOs/{arc_id}/{mood}/
    ├── Cryptids/{arc_id}/{mood}/
    ├── Conspiracies/{arc_id}/{mood}/
    └── Ghosts/{arc_id}/{mood}/
```

Files are named: `{arc_id}_{mood}_{index}_{speaker}.ogg`

### Incremental Generation

The script maintains a manifest at `Assets/Data/Dialogue/voice_manifest.json` to track generated files. Only new or changed lines are regenerated on subsequent runs. Use `--force` to regenerate everything.

### Troubleshooting

**"Unable to find voice" error:**
Run `python download_voices.py` to download the required voice models.

**"ffmpeg not found" error:**
Install ffmpeg and ensure it's in your PATH:
```bash
winget install ffmpeg
# Then restart your terminal
```

**"piper not found" error:**
Install piper-tts:
```bash
pip install piper-tts
```

**Slow generation:**
- Use `--limit 10` for testing
- Piper runs on CPU; generation is roughly real-time

## Adding New Tools

When adding new Python tools:

1. Create a folder under `Tools/` (e.g., `Tools/MyTool/`)
2. Include a `requirements.txt` if there are dependencies
3. Add documentation to this file
4. Reference the tool in `AGENTS.md` if AI agents should use it
