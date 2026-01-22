#!/usr/bin/env python3
"""
Generate Caller Audio from Conversation Arcs
Use ElevenLabs to generate caller dialogue audio based on gender and topic

Usage:
   python generate_caller_audio.py                     # Skip existing files
   python generate_caller_audio.py --force             # Regenerate all files
   python generate_caller_audio.py --verbose           # Detailed output
   python generate_caller_audio.py --arc ufos_compelling_pilot  # Specific arc
   python generate_caller_audio.py --resume            # Continue from saved progress
"""

import os
import json
import glob
import random
import argparse
import time
import pickle
from pathlib import Path
from elevenlabs_setup import ElevenLabsVoiceCloner

# Voice archetype mapping by topic
caller_archetype_mapping = {
    "UFOs": ["default_male", "enthusiastic", "nervous", "elderly_male"],
    "Cryptids": ["gruff", "default_male", "nervous", "elderly_male", "elderly_female"],
    "Conspiracies": ["conspiracy", "nervous", "default_male", "elderly_male"],
    "Ghosts": ["default_female", "nervous", "gruff", "elderly_female"]
}

def load_api_key():
    """
    Load ElevenLabs API key from config file or environment
    """
    # Try environment variable first
    api_key = os.getenv('ELEVENLABS_API_KEY')
    if api_key:
        return api_key

    # Try config file
    config_path = os.path.join(os.path.dirname(__file__), 'elevenlabs_config.json')
    try:
        with open(config_path, 'r') as f:
            config = json.load(f)
            return config.get('elevenlabs_api_key')
    except (FileNotFoundError, json.JSONDecodeError, KeyError):
        return None

def select_caller_voice(topic, gender):
    """
    Select appropriate voice for caller based on topic and gender
    """
    topic_voices = caller_archetype_mapping.get(topic, ["default_male"])

    # Filter by gender
    gender_voices = [v for v in topic_voices if gender in v]

    # If no gender-specific voices, use defaults
    if not gender_voices:
        if gender == "male":
            gender_voices = [v for v in topic_voices if "male" in v or not any(g in v for g in ["male", "female"])]

    # Random selection for variety
    return random.choice(gender_voices)

def should_generate_file(output_path, force_regenerate=False, verbose=False):
    """
    Determine if file should be generated based on existence and force flag

    Returns: (should_generate, reason)
    """
    if force_regenerate:
        if verbose:
            print(f"  Force regenerating: {os.path.basename(output_path)}")
        return True, "forced_regeneration"

    if os.path.exists(output_path):
        if verbose:
            print(f"  Skipping existing: {os.path.basename(output_path)}")
        return False, "existing_file"

    return True, "new_file"

def extract_caller_lines(arc_data):
    """
    Extract all caller dialogue lines from conversation arc
    """
    caller_lines = []
    arc_id = arc_data.get('arcId', 'unknown')
    topic = arc_data.get('topic', 'Unknown')
    gender = arc_data.get('callerGender', 'male')

    for i, dialogue_item in enumerate(arc_data.get('dialogue', [])):
        if dialogue_item.get('speaker') == 'Caller':
            # Caller lines are simple text (no mood variants)
            text = dialogue_item.get('text', '')

            caller_lines.append({
                'arc_id': arc_id,
                'topic': topic,
                'gender': gender,
                'line_index': i,
                'text': text,
                'voice_archetype': select_caller_voice(topic, gender)
            })

    return caller_lines

def load_progress(progress_file):
    """Load progress from pickle file"""
    if os.path.exists(progress_file):
        try:
            with open(progress_file, 'rb') as f:
                return pickle.load(f)
        except Exception as e:
            print(f"Warning: Could not load progress file {progress_file}: {e}")
    return None

def save_progress(progress_file, completed_indices):
    """Save progress to pickle file"""
    try:
        with open(progress_file, 'wb') as f:
            pickle.dump(completed_indices, f)
    except Exception as e:
        print(f"Warning: Could not save progress file {progress_file}: {e}")

def generate_caller_audio_batch(caller_lines, cloner, output_dir="../../assets/audio/voice/Callers", force_regenerate=False, verbose=False, resume=False):
    """
    Generate audio for all caller lines with smart skipping and progress saving
    """
    progress_file = os.path.join(os.path.dirname(__file__), 'caller_generation_progress.pkl')

    # Load previous progress if resuming
    completed_indices = set()
    if resume:
        loaded_progress = load_progress(progress_file)
        if loaded_progress:
            completed_indices = loaded_progress
            print(f"Resuming from previous progress: {len(completed_indices)} files already completed")

    # Create output directory structure
    os.makedirs(output_dir, exist_ok=True)

    # Create topic subdirectories
    topics = set(line['topic'] for line in caller_lines)
    for topic in topics:
        os.makedirs(os.path.join(output_dir, topic), exist_ok=True)

    # Track generation statistics
    stats = {
        'generated_new': 0,
        'skipped_existing': 0,
        'regenerated_forced': 0,
        'failed': 0
    }

    print(f"Starting caller audio generation of {len(caller_lines)} lines...")
    if force_regenerate:
        print("Force regeneration enabled - all files will be regenerated")
    else:
        print("Smart skipping enabled - existing files will be skipped (use --force to override)")

    for i, line in enumerate(caller_lines):
        # Skip if already completed in previous run
        if i in completed_indices and not force_regenerate:
            if verbose:
                print(f"[{i+1:2d}/{len(caller_lines)}] Already completed: {line['arc_id']}")
            stats['skipped_existing'] += 1
            continue

        arc_id = line['arc_id']
        topic = line['topic']
        gender = line['gender']
        text = line['text']
        voice_archetype = line['voice_archetype']
        line_index = line['line_index']

        # Create filename
        safe_arc_id = arc_id.replace('/', '_').replace('\\', '_')
        filename = f"{safe_arc_id}_{gender}_{line_index}.mp3"
        output_path = os.path.join(output_dir, topic, filename)

        # Check if we should generate this file
        should_generate, reason = should_generate_file(output_path, force_regenerate, verbose)

        if not should_generate:
            stats['skipped_existing'] += 1
            completed_indices.add(i)  # Mark as completed
            if not verbose:
                print(f"[{i+1:2d}/{len(caller_lines)}] Skipping: {filename}")
            continue

        try:
            print(f"[{i+1:2d}/{len(caller_lines)}] Generating: {filename}")
            if verbose:
                print(f"   Voice: {voice_archetype}")
                print(f"   Text: '{text[:50]}...'")

            # Generate audio
            result = cloner.generate_audio(
                text=text,
                output_path=output_path,
                voice_id=voice_archetype  # Use voice archetype
            )

            if result:
                if reason == "forced_regeneration":
                    stats['regenerated_forced'] += 1
                else:
                    stats['generated_new'] += 1
                print(f"   Saved: {filename}")

                # Save progress after successful generation
                completed_indices.add(i)
                if len(completed_indices) % 10 == 0:  # Save every 10 files
                    save_progress(progress_file, completed_indices)

            else:
                stats['failed'] += 1
                print(f"   Failed: {filename}")

            # Rate limiting to avoid API limits
            time.sleep(0.5)  # 500ms between requests

        except Exception as e:
            print(f"   Error generating {filename}: {e}")
            stats['failed'] += 1

    # Save final progress
    save_progress(progress_file, completed_indices)

    # Print final statistics
    print("\nGeneration complete!")
    print("Summary:")
    print(f"   New files generated: {stats['generated_new']}")
    print(f"   Existing files skipped: {stats['skipped_existing']}")
    print(f"   Files force-regenerated: {stats['regenerated_forced']}")
    print(f"   Failed generations: {stats['failed']}")
    print(f"   Output directory: {output_dir}")
    print(f"   Progress saved to: {progress_file}")

    if not force_regenerate and stats['skipped_existing'] > 0:
        print("\nUse --force to regenerate all files including existing ones.")
        print("Use --resume to continue from last saved progress.")

    return stats['generated_new'] + stats['regenerated_forced'], stats['failed']

def main():
    """
    Main function for caller audio generation
    """
    parser = argparse.ArgumentParser(description="Generate caller dialogue audio")
    parser.add_argument('--force', '-f', action='store_true',
                        help='Force regeneration of all files (ignore existing)')
    parser.add_argument('--verbose', '-v', action='store_true',
                        help='Show detailed progress for each file')
    parser.add_argument('--arc', type=str,
                        help='Generate audio for specific conversation arc (e.g., "ufos_compelling_pilot")')
    parser.add_argument('--resume', '-r', action='store_true',
                        help='Resume from last saved progress (skips already completed files)')

    args = parser.parse_args()

    print("Caller Audio Generation from Conversation Arcs")
    print("=" * 55)

    if args.force:
        print("Force regeneration mode: All files will be regenerated")
    else:
        print("Smart skipping mode: Existing files will be skipped (use --force to override)")

    if args.verbose:
        print("Verbose output enabled")

    # Check for API key
    api_key = load_api_key()
    if not api_key:
        print("No API key found!")
        print("Please create elevenlabs_config.json with your API key or set ELEVENLABS_API_KEY environment variable")
        print("Example config file:")
        print('{"elevenlabs_api_key": "your_api_key_here"}')
        return

    try:
        # Initialize ElevenLabs client
        cloner = ElevenLabsVoiceCloner(api_key)
        print("ElevenLabs client initialized")

        # Load all conversation arcs
        print("\nLoading conversation arcs...")
        arc_pattern = "../../assets/dialogue/arcs/**/*.json"
        arc_files = glob.glob(arc_pattern, recursive=True)

        all_caller_lines = []
        for arc_file in arc_files:
            try:
                with open(arc_file, 'r', encoding='utf-8') as f:
                    arc_data = json.load(f)

                # Filter by specific arc if requested
                if args.arc and args.arc not in arc_data.get('arcId', ''):
                    continue

                caller_lines = extract_caller_lines(arc_data)
                all_caller_lines.extend(caller_lines)

                if args.verbose or not args.arc:
                    print(f"Loaded {len(caller_lines)} caller lines from {os.path.basename(arc_file)}")

            except Exception as e:
                print(f"Error loading {arc_file}: {e}")

        print(f"\nTotal caller lines to generate: {len(all_caller_lines)}")

        # Generate all caller audio
        generated, failed = generate_caller_audio_batch(all_caller_lines, cloner, output_dir="../../assets/audio/voice/Callers", force_regenerate=args.force, verbose=args.verbose, resume=args.resume)

        if generated > 0:
            print("\nSuccess! Caller audio generation complete!")
            print(f"Generated {generated} caller audio files")
            print("Files organized by topic in assets/audio/voice/Callers/")
        if failed > 0:
            print(f"\nNote: {failed} files failed to generate. You may need to retry them.")

    except Exception as e:
        print(f"Error: {e}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    main()