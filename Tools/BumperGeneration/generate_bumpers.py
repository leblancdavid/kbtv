#!/usr/bin/env python3
"""
generate_bumpers.py - Suno Station Bumper Generation Helper

Since Suno doesn't have a public API, this script:
1. Reads bumper JSON files from the bumpers/ folder
2. Outputs ready-to-paste prompts for Suno web UI
3. Processes downloaded files (normalize, convert to OGG)
4. Organizes output into the correct Unity folder structure

Usage:
    python generate_bumpers.py prompts          # Print Suno prompts for all bumpers
    python generate_bumpers.py prompts <id>     # Print prompt for specific bumper
    python generate_bumpers.py process          # Process downloaded MP3s to OGG
    python generate_bumpers.py status           # Show which bumpers have audio
"""

import json
import os
import sys
import subprocess
from pathlib import Path
from typing import Optional

# Paths
SCRIPT_DIR = Path(__file__).parent
BUMPERS_DIR = SCRIPT_DIR / "bumpers"
DOWNLOADS_DIR = SCRIPT_DIR / "downloads"
OUTPUT_BASE = SCRIPT_DIR.parent.parent / "kbtv" / "Assets" / "Audio" / "Bumpers"
OUTPUT_INTRO = OUTPUT_BASE / "Intro"
OUTPUT_RETURN = OUTPUT_BASE / "Return"

# Audio settings
TARGET_SAMPLE_RATE = 44100
TARGET_CHANNELS = 2
TARGET_FORMAT = "ogg"
NORMALIZE_LUFS = -16  # Broadcast standard


def load_bumper(bumper_id: str) -> dict:
    """Load a single bumper JSON file."""
    path = BUMPERS_DIR / f"{bumper_id}.json"
    if not path.exists():
        raise FileNotFoundError(f"Bumper not found: {bumper_id}")
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def load_all_bumpers() -> list:
    """Load all bumper JSON files."""
    bumpers = []
    for path in sorted(BUMPERS_DIR.glob("*.json")):
        with open(path, "r", encoding="utf-8") as f:
            bumpers.append(json.load(f))
    return bumpers


def get_intro_bumpers() -> list:
    """Get only intro bumpers."""
    return [b for b in load_all_bumpers() if b.get("type") == "intro"]


def get_return_bumpers() -> list:
    """Get only return bumpers."""
    return [b for b in load_all_bumpers() if b.get("type") == "return"]


def print_suno_prompt(bumper: dict):
    """Print a formatted Suno prompt for a bumper."""
    bumper_type = bumper.get("type", "unknown").upper()
    print("=" * 60)
    print(f"BUMPER: {bumper['id']} ({bumper_type})")
    print(f"Style: {bumper.get('style_name', 'N/A')}")
    print("=" * 60)
    print()
    print("STYLE PROMPT (paste in Suno 'Style' field):")
    print("-" * 40)
    print(bumper["suno_prompt"])
    print()
    print("LYRICS (paste in Suno 'Lyrics' field):")
    print("-" * 40)
    print(bumper["lyrics"])
    print()
    print(f"Target duration: {bumper['duration_target']} seconds")
    if bumper.get("notes"):
        print(f"Notes: {bumper['notes']}")
    print()
    print("SAVE AS:")
    print(f"  {bumper['id']}_v1.mp3")
    print(f"  (or _v2, _v3 for variations)")
    print()


def cmd_prompts(bumper_id: Optional[str] = None):
    """Print Suno prompts for bumpers."""
    if bumper_id:
        bumper = load_bumper(bumper_id)
        print_suno_prompt(bumper)
    else:
        bumpers = load_all_bumpers()
        intro_count = len([b for b in bumpers if b.get("type") == "intro"])
        return_count = len([b for b in bumpers if b.get("type") == "return"])
        print(f"Found {len(bumpers)} bumpers ({intro_count} intro, {return_count} return)\n")
        
        print("\n" + "=" * 60)
        print("INTRO BUMPERS (8-15 seconds)")
        print("=" * 60 + "\n")
        for bumper in get_intro_bumpers():
            print_suno_prompt(bumper)
        
        print("\n" + "=" * 60)
        print("RETURN BUMPERS (3-5 seconds)")
        print("=" * 60 + "\n")
        for bumper in get_return_bumpers():
            print_suno_prompt(bumper)


def check_ffmpeg() -> bool:
    """Check if ffmpeg is available."""
    try:
        subprocess.run(
            ["ffmpeg", "-version"],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            check=True
        )
        return True
    except (subprocess.CalledProcessError, FileNotFoundError):
        return False


def process_audio(input_path: Path, output_path: Path) -> bool:
    """Process audio file: normalize and convert to OGG for Unity.
    
    Uses intermediate WAV to ensure clean OGG encoding without truncation issues.
    """
    try:
        # Ensure output directory exists
        output_path.parent.mkdir(parents=True, exist_ok=True)
        
        # Create temp WAV path
        temp_wav = output_path.with_suffix('.tmp.wav')
        
        # Step 1: Convert to clean WAV (strips metadata, album art, normalizes)
        wav_cmd = [
            "ffmpeg", "-y",
            "-i", str(input_path),
            "-vn",  # No video - strip album art
            "-map_metadata", "-1",  # Strip all metadata
            "-af", f"volume=0.9,aresample=resampler=soxr",  # Simple volume + high-quality resample
            "-ar", str(TARGET_SAMPLE_RATE),
            "-ac", str(TARGET_CHANNELS),
            "-c:a", "pcm_s16le",
            str(temp_wav)
        ]
        
        result = subprocess.run(
            wav_cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            check=True
        )
        
        # Step 2: Convert WAV to OGG with clean encoding
        ogg_cmd = [
            "ffmpeg", "-y",
            "-i", str(temp_wav),
            "-c:a", "libvorbis",
            "-q:a", "6",
            str(output_path)
        ]
        
        result = subprocess.run(
            ogg_cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            check=True
        )
        
        # Clean up temp file
        if temp_wav.exists():
            temp_wav.unlink()
        
        return True
    except subprocess.CalledProcessError as e:
        print(f"Error processing {input_path.name}: {e.stderr.decode()}")
        # Clean up temp file on error
        temp_wav = output_path.with_suffix('.tmp.wav')
        if temp_wav.exists():
            temp_wav.unlink()
        return False


def get_output_dir(bumper_type: str) -> Path:
    """Get the output directory for a bumper type."""
    if bumper_type == "intro":
        return OUTPUT_INTRO
    elif bumper_type == "return":
        return OUTPUT_RETURN
    else:
        raise ValueError(f"Unknown bumper type: {bumper_type}")


def cmd_process():
    """Process downloaded MP3s to OGG format."""
    if not check_ffmpeg():
        print("Error: ffmpeg not found. Please install ffmpeg.")
        print("  Windows: choco install ffmpeg")
        print("  Mac: brew install ffmpeg")
        print("  Linux: sudo apt install ffmpeg")
        return
    
    # Ensure downloads directory exists
    DOWNLOADS_DIR.mkdir(exist_ok=True)
    
    # Find all MP3 files in downloads
    mp3_files = list(DOWNLOADS_DIR.glob("*.mp3"))
    
    if not mp3_files:
        print("No MP3 files found in downloads/")
        print("Download bumper audio from Suno and save as:")
        print("  intro_01_v1.mp3, intro_02_v1.mp3, etc.")
        print("  return_01_v1.mp3, return_02_v1.mp3, etc.")
        return
    
    # Load bumper metadata to determine type
    bumpers_by_id = {}
    for bumper in load_all_bumpers():
        bumpers_by_id[bumper["id"]] = bumper
    
    processed = 0
    failed = 0
    
    for mp3_path in mp3_files:
        # Parse filename: {id}_v{n}.mp3
        name = mp3_path.stem  # e.g., "intro_01_v1"
        parts = name.rsplit("_v", 1)
        if len(parts) != 2:
            print(f"Skipping {mp3_path.name}: invalid filename format (expected {{id}}_v{{n}}.mp3)")
            continue
        
        bumper_id = parts[0]  # e.g., "intro_01"
        
        # Look up bumper type
        if bumper_id not in bumpers_by_id:
            print(f"Skipping {mp3_path.name}: no matching bumper JSON for '{bumper_id}'")
            continue
        
        bumper = bumpers_by_id[bumper_id]
        bumper_type = bumper.get("type", "intro")
        
        # Determine output path
        output_dir = get_output_dir(bumper_type)
        output_path = output_dir / f"{name}.ogg"
        
        print(f"Processing: {mp3_path.name} -> {output_path.relative_to(OUTPUT_BASE)}")
        
        if process_audio(mp3_path, output_path):
            processed += 1
        else:
            failed += 1
    
    print()
    print(f"Processed: {processed} files")
    if failed:
        print(f"Failed: {failed} files")
    
    if processed > 0:
        print()
        print("Next steps:")
        print("  1. Open Godot")
        print("  2. Import the generated audio files")


def cmd_status():
    """Show status of bumper audio files."""
    bumpers = load_all_bumpers()
    
    print("BUMPER STATUS")
    print("=" * 60)
    
    # Check intro bumpers
    print("\nINTRO BUMPERS (8-15 sec):")
    print("-" * 40)
    for bumper in get_intro_bumpers():
        bumper_id = bumper["id"]
        output_dir = OUTPUT_INTRO
        ogg_files = list(output_dir.glob(f"{bumper_id}_v*.ogg"))
        mp3_files = list(DOWNLOADS_DIR.glob(f"{bumper_id}_v*.mp3"))
        
        if ogg_files:
            status = f"[OK] {len(ogg_files)} variation(s)"
        elif mp3_files:
            status = f"[PENDING] {len(mp3_files)} MP3(s) to process"
        else:
            status = "[MISSING] No audio"
        
        style = bumper.get("style_name", "")
        print(f"  {bumper_id}: {status} ({style})")
    
    # Check return bumpers
    print("\nRETURN BUMPERS (3-5 sec):")
    print("-" * 40)
    for bumper in get_return_bumpers():
        bumper_id = bumper["id"]
        output_dir = OUTPUT_RETURN
        ogg_files = list(output_dir.glob(f"{bumper_id}_v*.ogg"))
        mp3_files = list(DOWNLOADS_DIR.glob(f"{bumper_id}_v*.mp3"))
        
        if ogg_files:
            status = f"[OK] {len(ogg_files)} variation(s)"
        elif mp3_files:
            status = f"[PENDING] {len(mp3_files)} MP3(s) to process"
        else:
            status = "[MISSING] No audio"
        
        style = bumper.get("style_name", "")
        print(f"  {bumper_id}: {status} ({style})")
    
    print()


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return
    
    command = sys.argv[1].lower()
    
    if command == "prompts":
        bumper_id = sys.argv[2] if len(sys.argv) > 2 else None
        cmd_prompts(bumper_id)
    elif command == "process":
        cmd_process()
    elif command == "status":
        cmd_status()
    else:
        print(f"Unknown command: {command}")
        print(__doc__)


if __name__ == "__main__":
    main()
