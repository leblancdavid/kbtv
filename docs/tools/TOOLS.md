# KBTV - Development Tools

This document covers the Python scripts and tools used for KBTV development.

## Tools Overview

| Tool | Location | Purpose |
|------|----------|---------|
| Audio Generation | `Tools/AudioGeneration/` | Generate voice audio from dialogue JSON using Piper TTS |
| Static Generation | `Tools/AudioGeneration/generate_static.py` | Generate phone line static noise loop |
| Transition Music (Procedural) | `Tools/AudioGeneration/generate_transition_music.py` | Generate procedural break transition music |
| Transition Music (Convert) | `Tools/AudioGeneration/convert_reference_music.py` | Convert reference MP3s to game-ready OGG clips |
| Ad Generation | `Tools/AdGeneration/` | Generate radio ad audio (Suno jingles) |
| Bumper Generation | `Tools/BumperGeneration/` | Generate station ID bumpers (Suno) |

## Audio Generation

Generates voice audio files for all dialogue (Vern broadcasts, caller conversations) using Piper TTS.

### Location

```
Tools/AudioGeneration/
├── generate_audio.py     # Main generation script (includes automatic voice model downloading)
├── config.json           # Voice mappings and audio settings
├── voices/               # Auto-downloaded voice models (gitignored)
└── temp/                 # Temporary files (gitignored)
```

### Prerequisites

1. **Python 3.9+** - Install from python.org or Microsoft Store

2. **Install Python dependencies:**
   ```bash
   pip install piper-tts pydub requests
   ```

3. **ffmpeg** - Required for OGG conversion
   - Windows: Download from https://ffmpeg.org/download.html and add to PATH
   - Or use: `winget install ffmpeg`

4. **Voice models** - Downloaded automatically on first use
   The generate_audio.py script automatically downloads required Piper voice models from Hugging Face when first needed. Models are cached locally in `Tools/AudioGeneration/voices/` (~60MB each).

### Usage

```bash
cd Tools/AudioGeneration

# Generate all audio (arcs + Vern broadcasts) - processes 1 arc per run
python generate_audio.py --arcs-only --full-rebuild

# Continue where left off (default behavior)
python generate_audio.py --arcs-only

# Process 3 arcs at a time (faster)
python generate_audio.py --arcs-only --batch-count 3

# Generate only Vern broadcast audio
python generate_audio.py --vern-only --full-rebuild

# Dry run - list lines without generating
python generate_audio.py --arcs-only --dry-run

# Force regenerate all (ignore cache)
python generate_audio.py --arcs-only --force

# Reset arc tracking and start fresh
python generate_audio.py --arcs-only --full-rebuild --reset-batch

# Check if dependencies are installed
python generate_audio.py --check-deps
```

#### Batch Processing

The full audio generation (17 arcs × ~40 lines each) can take 30+ minutes. Batch processing handles this automatically:

| Command | Arcs | Est. Time |
|---------|------|-----------|
| `--batch-count 1` (default) | 1 arc | 1-2 min |
| `--batch-count 3` | 3 arcs | 3-5 min |
| `--batch-count 5` | 5 arcs | 5-8 min |

The script tracks progress in the manifest and resumes automatically:

```bash
# Initial run
python generate_audio.py --arcs-only --full-rebuild
# Output: Progress: 1/17 arcs (5.9%), Remaining: 16 arcs

# Continue
python generate_audio.py --arcs-only
# Output: Progress: 2/17 arcs (11.8%), Remaining: 15 arcs

# Faster - do 3 at a time
python generate_audio.py --arcs-only --batch-count 3
# Output: Progress: 5/17 arcs (29.4%), Remaining: 12 arcs
```

#### New Mood Names

The system now uses 7 mood types instead of 5 tones:

| Mood | Description |
|------|-------------|
| `tired` | Low energy, weary |
| `energized` | Enthusiastic, high energy |
| `irritated` | Short-tempered, annoyed |
| `amused` | Entertained, finding it funny |
| `gruff` | Rough, no-nonsense |
| `focused` | Analytical, attentive |
| `neutral` | Professional, balanced |

#### Output Folders

```
kbtv/assets/Audio/Voice/
├── Vern/Broadcast/
│   ├── Opening/
│   ├── Closing/
│   ├── BetweenCallers/
│   └── DeadAirFiller/
└── Callers/
    ├── UFOs/{arc_id}/
    │   ├── tired/
    │   ├── energized/
    │   ├── irritated/
    │   ├── amused/
    │   ├── gruff/
    │   ├── focused/
    │   ├── neutral/
    │   └── Caller/
    ├── Cryptids/{arc_id}/
    │   └── ... (same structure)
    ├── Conspiracies/{arc_id}/
    │   └── ... (same structure)
    └── Ghosts/{arc_id}/
        └── ... (same structure)
```

**Vern files:** `{arc_id}_{mood}_{index:03d}_vern.ogg`
**Caller files:** `{arc_id}_caller_{index:03d}_caller.ogg`

#### Audio Compression

Audio is generated as OGG Vorbis at quality 6 for Unity compatibility:
- Standard Unity-supported format
- Good compression for speech
- Silence is trimmed from beginning/end of each file

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

### Incremental Generation

The script maintains a manifest at `assets/Data/Dialogue/voice_manifest.json` to track:
- Generated files and their content hashes
- Processed arcs (for batch resume)
- Last batch timestamp

Only new or changed lines are regenerated. Use `--force` to regenerate everything.

### Troubleshooting

**"Unable to find voice" error:**
The script should automatically download missing voice models. Check your internet connection. If the error persists, voice models may need to be downloaded manually from https://huggingface.co/rhasspy/piper-voices

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

## Static Noise Generation

Generates a loopable phone line static noise effect for the equipment upgrade system.

### Location

```
Tools/AudioGeneration/generate_static.py
```

### Prerequisites

```bash
pip install numpy scipy pydub
# ffmpeg must be in PATH for OGG export
```

### Usage

```bash
cd Tools/AudioGeneration

# Generate default 5-second loop
python generate_static.py

# Custom duration
python generate_static.py --duration 10

# Custom output path
python generate_static.py --output ../../kbtv/assets/Audio/SFX/my_static.ogg

# Check dependencies
python generate_static.py --check-deps
```

### Output

Default output: `kbtv/assets/Audio/SFX/phone_static_loop.ogg`

The generated audio contains:
- Band-limited white noise (phone line hiss)
- Occasional crackle bursts
- Subtle 60Hz power line hum
- Fade edges for seamless looping

### Unity Setup

After generating:

1. The file is automatically placed in `assets/Audio/SFX/`
2. Assign `phone_static_loop.ogg` to `StaticNoiseController._staticLoopClip` in the Inspector
3. Or run **KBTV > Setup Game Scene** which will configure everything

The static volume is controlled by `AudioQualityController` via `StaticNoiseController.SetBaseVolume()` based on equipment level.

## Adding New Tools

When adding new Python tools:

1. Create a folder under `Tools/` (e.g., `Tools/MyTool/`)
2. Include a `requirements.txt` if there are dependencies
3. Add documentation to this file
4. Reference the tool in `AGENTS.md` if AI agents should use it

## Break Transition Music Generation

Generates 5 variations of ambient synth music clips that play when a break is queued, cueing Vern that a break is coming.

> **Note:** For production use, prefer `convert_reference_music.py` with royalty-free tracks from Pixabay or similar sources. The procedural generation is useful for placeholders during development.

### Location

```
Tools/AudioGeneration/generate_transition_music.py
```

### Prerequisites

```bash
pip install numpy scipy pydub
# ffmpeg must be in PATH for OGG export
```

### Usage

```bash
cd Tools/AudioGeneration

# Generate all audio (arcs + Vern broadcasts) - processes 1 arc per run
python generate_audio.py --arcs-only --full-rebuild

# Continue where left off (default behavior)
python generate_audio.py --arcs-only

# Process 3 arcs at a time (faster)
python generate_audio.py --arcs-only --batch-count 3

# Generate only Vern broadcast audio
python generate_audio.py --vern-only --full-rebuild

# Dry run - list lines without generating
python generate_audio.py --arcs-only --dry-run

# Force regenerate all (ignore cache)
python generate_audio.py --arcs-only --force

# Reset arc tracking and start fresh
python generate_audio.py --arcs-only --full-rebuild --reset-batch

# Check if dependencies are installed
python generate_audio.py --check-deps
```

#### Batch Processing

The full audio generation (17 arcs x ~40 lines each) can take 30+ minutes. Batch processing handles this automatically:

| Command | Arcs | Est. Time |
|---------|------|-----------|
| `--batch-count 1` (default) | 1 arc | 1-2 min |
| `--batch-count 3` | 3 arcs | 3-5 min |
| `--batch-count 5` | 5 arcs | 5-8 min |

The script tracks progress in the manifest and resumes automatically:

```bash
# Initial run
python generate_audio.py --arcs-only --full-rebuild
# Output: Progress: 1/17 arcs (5.9%), Remaining: 16 arcs

# Continue
python generate_audio.py --arcs-only
# Output: Progress: 2/17 arcs (11.8%), Remaining: 15 arcs

# Faster - do 3 at a time
python generate_audio.py --arcs-only --batch-count 3
# Output: Progress: 5/17 arcs (29.4%), Remaining: 12 arcs
```

#### Mood Types

The system uses 7 mood types instead of the old 5 tones:

| Mood | Description |
|------|-------------|
| `tired` | Low energy, weary |
| `energized` | Enthusiastic, high energy |
| `irritated` | Short-tempered, annoyed |
| `amused` | Entertained, finding it funny |
| `gruff` | Rough, no-nonsense |
| `focused` | Analytical, attentive |
| `neutral` | Professional, balanced |

#### Output Folders

```
kbtv/assets/Audio/Voice/
├── Vern/Broadcast/
│   ├── Opening/
│   ├── Closing/
│   ├── BetweenCallers/
│   └── DeadAirFiller/
└── Callers/
    ├── UFOs/{arc_id}/
    │   ├── tired/
    │   ├── energized/
    │   ├── irritated/
    │   ├── amused/
    │   ├── gruff/
    │   ├── focused/
    │   ├── neutral/
    │   └── Caller/
    ├── Cryptids/{arc_id}/
    │   └── ... (same structure)
    ├── Conspiracies/{arc_id}/
    │   └── ... (same structure)
    └── Ghosts/{arc_id}/
        └── ... (same structure)
```

**Vern files:** `{arc_id}_{mood}_{index:03d}_vern.ogg`
**Caller files:** `{arc_id}_caller_{index:03d}_caller.ogg`

#### Audio Compression

Audio is generated as OGG Vorbis at quality 6 for Unity compatibility:
- Standard Unity-supported format
- Good compression for speech
- Silence is trimmed from beginning/end of each file

### Output

Default output: `kbtv/assets/Audio/Music/break_transition_*.ogg`

The 5 variations are:

| # | Name | Description |
|---|------|-------------|
| 1 | Mysterious Synth | A2 base, minor feel, slow vibrato, eerie overtone |
| 2 | Warm Ambient | C3 base, major feel, gentle LFO, warmer EQ |
| 3 | Tension Build | Low neutral, rising volume envelope |
| 4 | Late Night Jazz | D3 base, Dm7 chord feel, jazzy layers |
| 5 | Space Drift | E2 base, heavy LP filter, echo effect |

Each clip has:
- 0.5s fade-in and fade-out built in
- Loopable design (plays until break starts)
- Mono audio at 44.1kHz

## Reference Music Conversion

Converts royalty-free MP3 files to game-ready OGG clips for break transition music. This is the recommended approach for production-quality music.

### Location

```
Tools/AudioGeneration/convert_reference_music.py
```

### Prerequisites

```bash
pip install pydub
# ffmpeg must be in PATH for MP3 input and OGG export
```

### Workflow

1. Download royalty-free music from [Pixabay](https://pixabay.com/music/) or similar
2. Place MP3 files in `kbtv/assets/Audio/Music/Reference/`
3. Run the conversion script
4. Open Godot and import the generated files

### Usage

```bash
cd Tools/AudioGeneration

# Convert all MP3s in Reference folder
python convert_reference_music.py

# List available tracks without converting
python convert_reference_music.py --list

# Custom clip duration (default: 20s)
python convert_reference_music.py --duration 15

# Convert a specific track
python convert_reference_music.py --track jazz

# Check dependencies
python convert_reference_music.py --check-deps
```

### Track Naming

The script maps source MP3 filenames to descriptive output names:

| Source MP3 | Output OGG |
|------------|------------|
| `jazz-background-music-*.mp3` | `break_transition_jazz.ogg` |
| `synthwave-retro-80s-*.mp3` | `break_transition_synthwave.ogg` |
| `magic-mystery-*.mp3` | `break_transition_mystery.ogg` |
| ... etc | ... |

Unknown files are named based on their first 20 characters.

### Output

Default output: `kbtv/assets/Audio/Music/break_transition_*.ogg`

Each converted clip has:
- 20 second duration (clipped from start)
- 0.5s fade-in and fade-out
- OGG Vorbis format, quality 6

### Unity Setup

After conversion:

1. The files are automatically placed in `assets/Audio/Music/`
2. Run **KBTV > Setup Game Scene** which will:
   - Create/update `TransitionMusicConfig` ScriptableObject
   - Auto-discover all `break_transition_*.ogg` files
   - Add new tracks to the config with Enabled = true
3. To enable/disable specific tracks, edit `assets/Data/TransitionMusicConfig.asset` in the Inspector

The `TransitionMusicConfig` ScriptableObject allows you to:
- Toggle individual tracks on/off
- See all available tracks at a glance
- Add new tracks by dropping AudioClips into the list

The music automatically:
- Plays with fade-in when player queues an ad break (`PlayBreakTransitionMusic()`)
- Stops with fade-out when break is imminent (`StopBreakTransitionMusic()`)
- Volume controlled by `_transitionMusicVolume` (default 0.4)

## Ad Audio Generation

Generates radio ad audio for the 7 thematic KBTV advertisers using Suno jingles.

### Location

```
Tools/AdGeneration/
├── ads/                        # Individual ad script JSON files
│   ├── big_earls_auto.json
│   ├── pizza_palace.json
│   └── ... (7 total)
├── downloads/                  # Downloaded audio from Suno
├── generate_ads.py             # Suno jingle workflow
├── requirements.txt
└── README.md
```

### Ad Types

| Method | Count | Ads |
|--------|-------|-----|
| Suno jingle | 7 | big_earls_auto, pizza_palace, tinfoil_plus, bigfoot_repellent, night_vision_warehouse, area_51_tours, ghost_b_gone |

### Prerequisites

```bash
cd Tools/AdGeneration
pip install -r requirements.txt
# Also need ffmpeg in PATH
```

### Suno Jingle Workflow

1. **Get prompts:**
   ```bash
   python generate_ads.py prompts
   ```

2. **Generate in Suno web UI**, download MP3s, save as:
   ```
   downloads/big_earls_auto_v1.mp3
   downloads/pizza_palace_v1.mp3
   ...
   ```

3. **Process and convert:**
   ```bash
   python generate_ads.py process
   ```

4. **Import in Godot:** Run the setup script

### Output

Generated audio is saved to:
```
kbtv/assets/Audio/Ads/
├── big_earls_auto/
│   └── big_earls_auto_v1.ogg
├── pizza_palace/
│   └── pizza_palace_v1.ogg
└── ...
```

The `KBTV > Setup Ad Audio` menu command auto-assigns clips to `AdData.AudioVariations`.

## Station Bumper Generation

Generates station ID bumper audio for show openings and ad break returns using Suno.

### Location

```
Tools/BumperGeneration/
├── bumpers/                    # Bumper script JSON files
│   ├── intro_01.json          # Synth/Theremin intro
│   ├── intro_02.json          # Rock/Guitar intro
│   ├── intro_03.json          # Ambient intro
│   ├── return_01.json         # Synth sting return
│   ├── return_02.json         # Rock hit return
│   └── return_03.json         # Minimal return
├── downloads/                  # Downloaded audio from Suno
├── generate_bumpers.py         # Suno workflow script
└── README.md
```

### Bumper Types

| Type | Duration | When Played | Count |
|------|----------|-------------|-------|
| **Intro** | 8-15 sec | Show opening (before Vern speaks) | 3 |
| **Return** | 3-5 sec | After ad breaks | 3 |

### Bumper Styles

| ID | Style | Description |
|----|-------|-------------|
| `intro_01` | Retro Synth/Theremin | Coast to Coast AM vibe |
| `intro_02` | Rock/Guitar | Energetic power chords |
| `intro_03` | Ambient/Atmospheric | Sparse and mysterious |
| `return_01` | Synth Sting | Quick synth sweep |
| `return_02` | Rock Hit | Power chord + "We're back!" |
| `return_03` | Minimal/Clean | Simple melody + whisper |

### Suno Workflow

1. **Get prompts:**
   ```bash
   cd Tools/BumperGeneration
   python generate_bumpers.py prompts
   ```

2. **Generate in Suno web UI**, download MP3s, save as:
   ```
   downloads/intro_01_v1.mp3
   downloads/intro_02_v1.mp3
   downloads/intro_03_v1.mp3
   downloads/return_01_v1.mp3
   downloads/return_02_v1.mp3
   downloads/return_03_v1.mp3
   ```

3. **Process and convert:**
   ```bash
   python generate_bumpers.py process
   ```

4. **Check status:**
   ```bash
   python generate_bumpers.py status
   ```

5. **Import in Godot:**
   - Run `KBTV > Create Bumper Configs` (if not already done)
   - Run `KBTV > Assign Bumper Audio`
   - Run `KBTV > Setup Game Scene` (auto-assigns configs to GameBootstrap)

### Output

Generated audio is saved to:
```
kbtv/assets/Audio/Bumpers/
├── Intro/
│   ├── intro_01_v1.ogg
│   ├── intro_02_v1.ogg
│   └── intro_03_v1.ogg
└── Return/
    ├── return_01_v1.ogg
    ├── return_02_v1.ogg
    └── return_03_v1.ogg
```

The bumper configs (`IntroBumperConfig.asset` and `ReturnBumperConfig.asset`) are created in `assets/Data/Audio/` and automatically assigned to `GameBootstrap` when running `KBTV > Setup Game Scene`. The configs are then wired to `AudioManager` at runtime.
