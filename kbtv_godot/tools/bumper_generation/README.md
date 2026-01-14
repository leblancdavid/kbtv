# Station Bumper Generation Tools

Scripts for generating KBTV radio station bumper audio.

## Overview

KBTV uses two types of station bumpers:

| Type | Duration | When Played | Count |
|------|----------|-------------|-------|
| **Intro** | 8-15 sec | Show opening (before Vern speaks) | 3 variations |
| **Return** | 3-5 sec | After ad breaks | 3 variations |

Each bumper is generated using Suno AI and features varied musical styles.

### Intro Bumpers

| ID | Style | Description |
|----|-------|-------------|
| `intro_01` | Retro Synth/Theremin | Classic Coast to Coast AM vibe |
| `intro_02` | Rock/Guitar | Energetic power chord driven |
| `intro_03` | Ambient/Atmospheric | Sparse and mysterious |

### Return Bumpers

| ID | Style | Description |
|----|-------|-------------|
| `return_01` | Synth Sting | Quick synth sweep + "KBTV" |
| `return_02` | Rock Hit | Power chord + "We're back!" |
| `return_03` | Minimal/Clean | Simple melody + whispered tagline |

## Setup

```bash
# No additional Python dependencies required (uses stdlib only)

# Install ffmpeg for audio processing
# Windows: choco install ffmpeg
# Mac: brew install ffmpeg
# Linux: sudo apt install ffmpeg
```

## Workflow

### 1. Get Prompts

```bash
python generate_bumpers.py prompts
```

This prints ready-to-paste prompts for Suno.

### 2. Generate in Suno

1. Go to [suno.ai](https://suno.ai)
2. Paste the style prompt and lyrics
3. Generate until you get a good take
4. Download as MP3

### 3. Save with Correct Naming

Save downloaded files to the `downloads/` folder:

```
downloads/
  intro_01_v1.mp3
  intro_02_v1.mp3
  intro_03_v1.mp3
  return_01_v1.mp3
  return_02_v1.mp3
  return_03_v1.mp3
```

Use `_v2`, `_v3` suffixes for additional variations.

### 4. Process and Convert

```bash
python generate_bumpers.py process
```

This normalizes audio and converts to OGG in the Unity folder.

### 5. Assign in Unity

1. Open Unity
2. Run: `KBTV > Create Bumper Configs` (if not already done)
3. Run: `KBTV > Assign Bumper Audio`

## Check Status

```bash
python generate_bumpers.py status
```

Shows which bumpers have audio generated.

## Folder Structure

```
Tools/BumperGeneration/
├── bumpers/                  # Bumper script JSON files
│   ├── intro_01.json
│   ├── intro_02.json
│   ├── intro_03.json
│   ├── return_01.json
│   ├── return_02.json
│   └── return_03.json
├── downloads/                # Downloaded audio from Suno
├── generate_bumpers.py       # Suno workflow script
└── README.md

kbtv/Assets/Audio/Bumpers/    # Unity audio output
├── Intro/
│   ├── intro_01_v1.ogg
│   ├── intro_02_v1.ogg
│   └── intro_03_v1.ogg
└── Return/
    ├── return_01_v1.ogg
    ├── return_02_v1.ogg
    └── return_03_v1.ogg
```

## Bumper JSON Schema

```json
{
  "id": "intro_01",
  "type": "intro",
  "style_name": "Retro Synth/Theremin",
  "duration_target": 12,
  "suno_prompt": "style prompt for Suno...",
  "lyrics": "Lyrics to sing...",
  "notes": "Production notes..."
}
```

## Tips for Suno Generation

- **Intro bumpers**: Aim for 10-15 seconds. Suno tends to generate longer, so you may need to trim.
- **Return bumpers**: These are tricky to get short. Try adding "very short, 4 seconds" to the prompt.
- **Multiple takes**: Generate 2-3 versions and pick the best one.
- **Instrumental versions**: If vocals don't work well, try "instrumental" in the style prompt.
