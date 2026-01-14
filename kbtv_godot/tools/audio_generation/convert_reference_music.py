#!/usr/bin/env python3
"""
Convert reference MP3 files to game-ready OGG clips for KBTV break transition music.

Takes MP3 files from the Reference folder, clips to 20 seconds, applies fade-in/fade-out,
and converts to OGG format for use as break transition music.

Usage:
    python convert_reference_music.py              # Convert all MP3s in Reference folder
    python convert_reference_music.py --duration 15  # Custom clip duration (default: 20s)
    python convert_reference_music.py --list       # List available tracks without converting
    python convert_reference_music.py --check-deps # Check dependencies

Requirements:
    pip install pydub
    ffmpeg must be in PATH for MP3 input and OGG export
"""

import argparse
import os
import sys
from pathlib import Path
from typing import List, Tuple


def check_dependencies():
    """Check if required packages are installed."""
    missing = []
    
    try:
        from pydub import AudioSegment
    except ImportError:
        missing.append("pydub")
    
    if missing:
        print(f"Missing dependencies: {', '.join(missing)}")
        print(f"Install with: pip install {' '.join(missing)}")
        sys.exit(1)
    
    # Check for ffmpeg
    import shutil
    if shutil.which("ffmpeg") is None:
        print("ERROR: ffmpeg not found in PATH")
        print("Install with: winget install ffmpeg")
        print("Then restart your terminal")
        sys.exit(1)
    
    return True


# Mapping from source MP3 filename to output OGG name
# This creates descriptive names that are easy to identify in Unity
TRACK_MAPPING = {
    "jazz-background-music-416542.mp3": "break_transition_jazz",
    "synthwave-retro-80s-442850.mp3": "break_transition_synthwave",
    "magic-mystery-harry-potter-music-320643.mp3": "break_transition_mystery",
    "velours-machine-334573.mp3": "break_transition_velours",
    "sakura-mellow-japan-lofi-beats-265429.mp3": "break_transition_lofi",
    "drive-in-1962-251334.mp3": "break_transition_drivein",
    "country-western-texas-background-music-361672.mp3": "break_transition_country",
    "energetic-rock-2-462282.mp3": "break_transition_rock",
    "happy-mood-463426.mp3": "break_transition_happy",
    "heavy-doom-metal-instrumental-288971.mp3": "break_transition_metal",
    "silly-silly-humor-comic-background-music-354040.mp3": "break_transition_silly",
    "the-beat-of-nature-122841.mp3": "break_transition_nature",
    "upbeat-pop-music-423118.mp3": "break_transition_pop",
}


def get_reference_folder() -> Path:
    """Get the path to the Reference folder containing source MP3s."""
    script_dir = Path(__file__).parent
    project_root = script_dir.parent.parent
    return project_root / "kbtv" / "Assets" / "Audio" / "Music" / "Reference"


def get_output_folder() -> Path:
    """Get the path to output OGG files."""
    script_dir = Path(__file__).parent
    project_root = script_dir.parent.parent
    return project_root / "kbtv" / "Assets" / "Audio" / "Music"


def list_available_tracks(reference_folder: Path) -> List[Tuple[str, str]]:
    """List all available MP3 tracks in the Reference folder.
    
    Returns:
        List of tuples (mp3_filename, output_name)
    """
    tracks = []
    
    for mp3_file in sorted(reference_folder.glob("*.mp3")):
        mp3_name = mp3_file.name
        output_name = TRACK_MAPPING.get(mp3_name, f"break_transition_{mp3_file.stem[:20]}")
        tracks.append((mp3_name, output_name))
    
    return tracks


def convert_track(input_path: Path, output_path: Path, duration_seconds: float = 20.0,
                  fade_in_ms: int = 500, fade_out_ms: int = 500):
    """Convert a single MP3 track to a game-ready OGG clip.
    
    Args:
        input_path: Path to source MP3 file
        output_path: Path to output OGG file
        duration_seconds: Duration to clip to (default: 20s)
        fade_in_ms: Fade-in duration in milliseconds (default: 500ms)
        fade_out_ms: Fade-out duration in milliseconds (default: 500ms)
    """
    from pydub import AudioSegment
    
    # Load the MP3
    audio = AudioSegment.from_mp3(str(input_path))
    
    # Calculate duration in milliseconds
    duration_ms = int(duration_seconds * 1000)
    
    # Clip to duration (from the start)
    if len(audio) > duration_ms:
        audio = audio[:duration_ms]
    
    # Apply fade-in
    if fade_in_ms > 0:
        audio = audio.fade_in(fade_in_ms)
    
    # Apply fade-out
    if fade_out_ms > 0:
        audio = audio.fade_out(fade_out_ms)
    
    # Export as OGG with good quality
    audio.export(
        str(output_path),
        format="ogg",
        codec="libvorbis",
        parameters=["-q:a", "6"]
    )


def main():
    parser = argparse.ArgumentParser(
        description="Convert reference MP3s to game-ready OGG clips for KBTV"
    )
    parser.add_argument(
        "--duration",
        type=float,
        default=20.0,
        help="Duration to clip each track to in seconds (default: 20.0)"
    )
    parser.add_argument(
        "--fade-in",
        type=int,
        default=500,
        help="Fade-in duration in milliseconds (default: 500)"
    )
    parser.add_argument(
        "--fade-out",
        type=int,
        default=500,
        help="Fade-out duration in milliseconds (default: 500)"
    )
    parser.add_argument(
        "--list",
        action="store_true",
        help="List available tracks without converting"
    )
    parser.add_argument(
        "--check-deps",
        action="store_true",
        help="Check dependencies and exit"
    )
    parser.add_argument(
        "--track",
        type=str,
        default=None,
        help="Convert only a specific track (by MP3 filename or output name)"
    )
    
    args = parser.parse_args()
    
    # Check dependencies
    check_dependencies()
    
    if args.check_deps:
        print("All dependencies are installed!")
        return
    
    reference_folder = get_reference_folder()
    output_folder = get_output_folder()
    
    if not reference_folder.exists():
        print(f"ERROR: Reference folder not found: {reference_folder}")
        print("Create the folder and add MP3 files from Pixabay or similar.")
        sys.exit(1)
    
    # Get available tracks
    tracks = list_available_tracks(reference_folder)
    
    if not tracks:
        print(f"No MP3 files found in {reference_folder}")
        sys.exit(1)
    
    if args.list:
        print(f"Available tracks in {reference_folder}:\n")
        print(f"{'#':<3} {'Source MP3':<55} {'Output Name'}")
        print("-" * 90)
        for i, (mp3_name, output_name) in enumerate(tracks, 1):
            print(f"{i:<3} {mp3_name:<55} {output_name}.ogg")
        print(f"\nTotal: {len(tracks)} tracks")
        return
    
    # Filter to specific track if requested
    if args.track:
        filtered = []
        for mp3_name, output_name in tracks:
            if args.track.lower() in mp3_name.lower() or args.track.lower() in output_name.lower():
                filtered.append((mp3_name, output_name))
        
        if not filtered:
            print(f"No tracks matching '{args.track}' found.")
            print("Use --list to see available tracks.")
            sys.exit(1)
        
        tracks = filtered
    
    # Ensure output folder exists
    output_folder.mkdir(parents=True, exist_ok=True)
    
    print(f"Converting {len(tracks)} track(s)...")
    print(f"Duration: {args.duration}s, Fade-in: {args.fade_in}ms, Fade-out: {args.fade_out}ms")
    print(f"Output directory: {output_folder}")
    print()
    
    for i, (mp3_name, output_name) in enumerate(tracks, 1):
        input_path = reference_folder / mp3_name
        output_path = output_folder / f"{output_name}.ogg"
        
        print(f"  [{i}/{len(tracks)}] {mp3_name}")
        print(f"        -> {output_name}.ogg")
        
        try:
            convert_track(
                input_path=input_path,
                output_path=output_path,
                duration_seconds=args.duration,
                fade_in_ms=args.fade_in,
                fade_out_ms=args.fade_out
            )
            print(f"        Done!")
        except Exception as e:
            print(f"        ERROR: {e}")
    
    print()
    print("Conversion complete!")
    print()
    print("To use in Unity:")
    print("1. Open Unity and let it import the new audio files")
    print("2. Run KBTV > Setup Game Scene to auto-configure")
    print("3. Or manually assign clips to TransitionMusicConfig ScriptableObject")


if __name__ == "__main__":
    main()
