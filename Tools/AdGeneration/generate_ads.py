#!/usr/bin/env python3
"""
generate_ads.py - Suno Ad Generation Helper

Since Suno doesn't have a public API, this script:
1. Reads ad JSON files from the ads/ folder
2. Outputs ready-to-paste prompts for Suno web UI
3. Processes downloaded files (normalize, convert to OGG)
4. Organizes output into the correct Unity folder structure

Usage:
    python generate_ads.py prompts          # Print Suno prompts for all jingle ads
    python generate_ads.py prompts <id>     # Print prompt for specific ad
    python generate_ads.py process          # Process downloaded MP3s to OGG
    python generate_ads.py status           # Show which ads have audio
"""

import json
import os
import sys
import subprocess
from pathlib import Path

# Paths
SCRIPT_DIR = Path(__file__).parent
ADS_DIR = SCRIPT_DIR / "ads"
DOWNLOADS_DIR = SCRIPT_DIR / "downloads"
OUTPUT_DIR = SCRIPT_DIR.parent.parent / "kbtv" / "Assets" / "Audio" / "Ads"

# Audio settings
TARGET_SAMPLE_RATE = 44100
TARGET_CHANNELS = 2
TARGET_FORMAT = "ogg"
NORMALIZE_LUFS = -16  # Broadcast standard


def load_ad(ad_id: str) -> dict:
    """Load a single ad JSON file."""
    path = ADS_DIR / f"{ad_id}.json"
    if not path.exists():
        raise FileNotFoundError(f"Ad not found: {ad_id}")
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def load_all_ads() -> list:
    """Load all ad JSON files."""
    ads = []
    for path in sorted(ADS_DIR.glob("*.json")):
        with open(path, "r", encoding="utf-8") as f:
            ads.append(json.load(f))
    return ads


def get_suno_ads() -> list:
    """Get only ads that use Suno generation."""
    return [ad for ad in load_all_ads() if ad.get("generation_method") == "suno"]


def print_suno_prompt(ad: dict):
    """Print a formatted Suno prompt for an ad."""
    print("=" * 60)
    print(f"AD: {ad['advertiser']} ({ad['id']})")
    print(f"Type: {ad['type']}")
    print("=" * 60)
    print()
    print("STYLE PROMPT (paste in Suno 'Style' field):")
    print("-" * 40)
    print(ad["suno_prompt"])
    print()
    print("LYRICS (paste in Suno 'Lyrics' field):")
    print("-" * 40)
    print(ad["lyrics"])
    print()
    print(f"Target duration: {ad['duration_target']} seconds")
    print()
    print("SAVE AS:")
    print(f"  {ad['id']}_v1.mp3")
    print(f"  (or _v2, _v3 for variations)")
    print()


def cmd_prompts(ad_id: str = None):
    """Print Suno prompts for jingle ads."""
    if ad_id:
        ad = load_ad(ad_id)
        if ad.get("generation_method") != "suno":
            print(f"Error: {ad_id} is not a Suno jingle (uses {ad.get('generation_method')})")
            return
        print_suno_prompt(ad)
    else:
        ads = get_suno_ads()
        print(f"Found {len(ads)} Suno jingle ads\n")
        for ad in ads:
            print_suno_prompt(ad)


def check_ffmpeg() -> bool:
    """Check if ffmpeg is available."""
    try:
        subprocess.run(["ffmpeg", "-version"], capture_output=True, check=True)
        return True
    except (subprocess.CalledProcessError, FileNotFoundError):
        return False


def process_audio_file(input_path: Path, output_path: Path):
    """Convert and normalize an audio file using ffmpeg."""
    # Ensure output directory exists
    output_path.parent.mkdir(parents=True, exist_ok=True)
    
    # ffmpeg command: normalize to -16 LUFS, convert to OGG
    # -vn: strip any video/image streams (like album art)
    # -map_metadata -1: strip all metadata to avoid issues
    cmd = [
        "ffmpeg", "-y",
        "-i", str(input_path),
        "-vn",  # No video - strip album art
        "-map_metadata", "-1",  # Strip all metadata
        "-af", f"loudnorm=I={NORMALIZE_LUFS}:TP=-1.5:LRA=11",
        "-ar", str(TARGET_SAMPLE_RATE),
        "-ac", str(TARGET_CHANNELS),
        "-c:a", "libvorbis",
        "-q:a", "6",  # Quality level (0-10, 6 is good)
        str(output_path)
    ]
    
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        print(f"  Error: {result.stderr}")
        return False
    return True


def cmd_process():
    """Process downloaded MP3s to OGG format."""
    if not check_ffmpeg():
        print("Error: ffmpeg not found. Install ffmpeg first.")
        print("  Windows: choco install ffmpeg")
        print("  Mac: brew install ffmpeg")
        print("  Linux: sudo apt install ffmpeg")
        return
    
    # Create downloads folder if it doesn't exist
    DOWNLOADS_DIR.mkdir(exist_ok=True)
    
    # Find all MP3/WAV files in downloads
    audio_files = list(DOWNLOADS_DIR.glob("*.mp3")) + list(DOWNLOADS_DIR.glob("*.wav"))
    
    if not audio_files:
        print(f"No audio files found in {DOWNLOADS_DIR}")
        print()
        print("Download your Suno generations and save them as:")
        print("  downloads/big_earls_auto_v1.mp3")
        print("  downloads/pizza_palace_v1.mp3")
        print("  etc.")
        return
    
    print(f"Found {len(audio_files)} audio files to process")
    print(f"Output directory: {OUTPUT_DIR}")
    print()
    
    # Load all ad IDs for validation
    valid_ids = {ad["id"] for ad in load_all_ads()}
    
    processed = 0
    for audio_file in audio_files:
        # Parse filename: {ad_id}_v{n}.mp3
        name = audio_file.stem  # e.g., "big_earls_auto_v1"
        
        # Extract ad_id (everything before _v{n})
        parts = name.rsplit("_v", 1)
        if len(parts) != 2:
            print(f"  Skipping {audio_file.name} (invalid format, expected {'{ad_id}'}_v{'{n}'}.mp3)")
            continue
        
        ad_id = parts[0]
        if ad_id not in valid_ids:
            print(f"  Skipping {audio_file.name} (unknown ad_id: {ad_id})")
            continue
        
        # Output path: Assets/Audio/Ads/{ad_id}/{ad_id}_v{n}.ogg
        output_path = OUTPUT_DIR / ad_id / f"{name}.{TARGET_FORMAT}"
        
        print(f"  Processing: {audio_file.name} -> {ad_id}/{name}.{TARGET_FORMAT}")
        
        if process_audio_file(audio_file, output_path):
            processed += 1
    
    print()
    print(f"Processed {processed}/{len(audio_files)} files")
    print()
    print("Next steps:")
    print("  1. Open Godot")
    print("  2. Import the generated audio files")


def cmd_status():
    """Show which ads have audio files."""
    ads = load_all_ads()
    
    print("Ad Audio Status")
    print("=" * 60)
    
    for ad in ads:
        ad_id = ad["id"]
        ad_dir = OUTPUT_DIR / ad_id
        
        if ad_dir.exists():
            ogg_files = list(ad_dir.glob("*.ogg"))
            if ogg_files:
                files = ", ".join(f.name for f in ogg_files)
                print(f"  [OK] {ad_id}: {len(ogg_files)} file(s) - {files}")
            else:
                print(f"  [--] {ad_id}: folder exists but no OGG files")
        else:
            method = ad.get("generation_method", "unknown")
            print(f"  [  ] {ad_id}: no audio ({method})")
    
    print()
    print(f"Output directory: {OUTPUT_DIR}")


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return
    
    command = sys.argv[1].lower()
    
    if command == "prompts":
        ad_id = sys.argv[2] if len(sys.argv) > 2 else None
        cmd_prompts(ad_id)
    elif command == "process":
        cmd_process()
    elif command == "status":
        cmd_status()
    else:
        print(f"Unknown command: {command}")
        print(__doc__)


if __name__ == "__main__":
    main()
