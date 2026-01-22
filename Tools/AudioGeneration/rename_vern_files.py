#!/usr/bin/env python3
"""
Rename Vern broadcast files for consistent mood suffix naming
"""

import os
import shutil
from pathlib import Path

def rename_vern_broadcast_files():
    """Rename all Vern broadcast files to include mood suffixes consistently"""

    base_dir = Path("../../assets/audio/voice/Vern/Broadcast")

    renamed_count = 0

    # Process each mood directory
    for mood_dir in base_dir.iterdir():
        if not mood_dir.is_dir():
            continue

        mood = mood_dir.name
        print(f"Processing mood: {mood}")

        for file_path in mood_dir.glob("*.mp3"):
            filename = file_path.name

            # Skip files that already have mood suffixes
            if f"_{mood}.mp3" in filename:
                continue

            # Handle different file types
            if filename.startswith("vern_opening_"):
                # vern_opening_001.mp3 -> vern_opening_001_{mood}.mp3
                parts = filename.split("_")
                if len(parts) >= 3 and parts[2].endswith(".mp3"):
                    number = parts[2].replace(".mp3", "")
                    new_filename = f"vern_opening_{number}_{mood}.mp3"
                    new_path = file_path.parent / new_filename

                    print(f"  {filename} -> {new_filename}")
                    shutil.move(str(file_path), str(new_path))
                    renamed_count += 1

            elif filename.startswith("vern_closing_"):
                # vern_closing_001_gruff.mp3 -> already has mood suffix, skip
                continue

            elif filename.startswith("vern_betweencallers_"):
                # vern_betweencallers_neutral.mp3 -> already has mood suffix, skip
                continue

            elif filename.startswith("vern_return_"):
                # vern_return_neutral.mp3 -> already has mood suffix, skip
                continue

    print(f"Renamed {renamed_count} files")
    return renamed_count

if __name__ == "__main__":
    rename_vern_broadcast_files()