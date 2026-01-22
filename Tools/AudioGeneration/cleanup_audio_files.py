#!/usr/bin/env python3
"""
Clean up Vern broadcast audio files for consistent naming and numbering
"""

import os
import shutil
from pathlib import Path
from collections import defaultdict

def cleanup_double_suffixes():
    """Remove double mood suffixes from filenames"""
    base_dir = Path("../../assets/audio/voice/Vern/Broadcast")

    cleaned_count = 0

    for mood_dir in base_dir.iterdir():
        if not mood_dir.is_dir():
            continue

        mood = mood_dir.name
        print(f"Processing mood: {mood}")

        for file_path in mood_dir.glob("*.mp3"):
            filename = file_path.name

            # Check for double mood suffixes
            double_suffix_pattern = f"_{mood}_{mood}.mp3"
            if double_suffix_pattern in filename:
                # Remove the duplicate suffix
                new_filename = filename.replace(double_suffix_pattern, f"_{mood}.mp3")
                new_path = file_path.parent / new_filename

                print(f"  {filename} -> {new_filename}")
                shutil.move(str(file_path), str(new_path))

                # Also move the .import file if it exists
                import_file = file_path.with_suffix('.mp3.import')
                if import_file.exists():
                    new_import_file = new_path.with_suffix('.mp3.import')
                    shutil.move(str(import_file), str(new_import_file))

                cleaned_count += 1

    print(f"Cleaned {cleaned_count} double suffix files")
    return cleaned_count

def renumber_opening_files():
    """Renumber opening files to be consistent across moods (001, 002, 003, etc.)"""
    base_dir = Path("../../assets/audio/voice/Vern/Broadcast")

    renumbered_count = 0

    for mood_dir in base_dir.iterdir():
        if not mood_dir.is_dir():
            continue

        mood = mood_dir.name
        print(f"Renumbering opening files for mood: {mood}")

        # Find all opening files for this mood
        opening_files = list(mood_dir.glob("vern_opening_*_*.mp3"))
        opening_files.sort()  # Sort to ensure consistent ordering

        if not opening_files:
            print(f"  No opening files found for {mood}")
            continue

        # Renumber them sequentially starting from 01
        for i, file_path in enumerate(opening_files, 1):
            filename = file_path.name

            # Extract the current number and mood suffix
            # Pattern: vern_opening_{number}_{mood}.mp3
            parts = filename.split('_')
            if len(parts) >= 4 and parts[0] == 'vern' and parts[1] == 'opening':
                new_number = f"{i:02d}"
                new_filename = f"vern_opening_{new_number}_{mood}.mp3"
                new_path = file_path.parent / new_filename

                print(f"  {filename} -> {new_filename}")
                shutil.move(str(file_path), str(new_path))

                # Also move the .import file if it exists
                import_file = file_path.with_suffix('.mp3.import')
                if import_file.exists():
                    new_import_file = new_path.with_suffix('.mp3.import')
                    shutil.move(str(import_file), str(new_import_file))

                renumbered_count += 1

    print(f"Renumbered {renumbered_count} opening files")
    return renumbered_count

def main():
    print("=== Vern Broadcast Audio File Cleanup ===\n")

    print("1. Cleaning up double mood suffixes...")
    cleaned = cleanup_double_suffixes()

    print("\n2. Renumbering opening files for consistency...")
    renumbered = renumber_opening_files()

    print("\n=== Cleanup Complete ===")
    print(f"Files cleaned: {cleaned}")
    print(f"Files renumbered: {renumbered}")
    print(f"Total files modified: {cleaned + renumbered}")

if __name__ == "__main__":
    main()