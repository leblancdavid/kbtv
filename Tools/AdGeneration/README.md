# Ad Generation Tools

Scripts for generating KBTV radio ad audio.

## Overview

KBTV has 7 thematic ads across 3 tiers:
- **LocalBusiness** (2): big_earls_auto, pizza_palace
- **RegionalBrand** (3): tinfoil_plus, bigfoot_repellent, night_vision_warehouse
- **NationalSponsor** (2): area_51_tours, ghost_b_gone

Each ad is a complete 15-20 second radio spot with music baked in, generated using Suno.

## Setup

```bash
# Install Python dependencies
pip install -r requirements.txt

# Install ffmpeg
# Windows: choco install ffmpeg
# Mac: brew install ffmpeg
# Linux: sudo apt install ffmpeg
```

## Workflow

1. **Get prompts:**
   ```bash
   python generate_ads.py prompts
   ```

2. **Generate in Suno:**
   - Go to [suno.ai](https://suno.ai)
   - Paste the style prompt and lyrics
   - Generate until you get a good take
   - Download as MP3

3. **Save with correct naming:**
   ```
   downloads/big_earls_auto_v1.mp3
   downloads/pizza_palace_v1.mp3
   ...
   ```

4. **Process and convert:**
   ```bash
   python generate_ads.py process
   ```
   This normalizes audio and converts to OGG in the Unity folder.

5. **Assign in Unity:**
   - Open Unity
   - Run: `KBTV > Setup Ad Audio`

## Check Status

```bash
python generate_ads.py status
```

## Folder Structure

```
Tools/AdGeneration/
├── ads/                    # Ad script JSON files
│   ├── big_earls_auto.json
│   ├── pizza_palace.json
│   └── ...
├── downloads/              # Downloaded audio from Suno
├── generate_ads.py         # Suno jingle workflow
├── requirements.txt
└── README.md

kbtv/Assets/Audio/Ads/      # Unity audio output
├── big_earls_auto/
│   └── big_earls_auto_v1.ogg
├── pizza_palace/
│   └── pizza_palace_v1.ogg
└── ...
```

## Ad JSON Schema

```json
{
  "id": "big_earls_auto",
  "advertiser": "Big Earl's Auto",
  "type": "LocalBusiness",
  "tagline": "...",
  "generation_method": "suno",
  "duration_target": 18,
  "style": "country jingle...",
  "lyrics": "...",
  "suno_prompt": "...",
  "notes": "..."
}
```
