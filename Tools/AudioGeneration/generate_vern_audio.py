#!/usr/bin/env python3
"""
Generate Vern Dialogue Audio from Conversation Arcs
Use ElevenLabs to generate Vern audio based on mood variants

Usage:
  python generate_vern_audio.py              # Skip existing files
  python generate_vern_audio.py --force      # Regenerate all files
  python generate_vern_audio.py --verbose    # Detailed output
  python generate_vern_audio.py --force --verbose  # Force + verbose
"""

import os
import json
import glob
import argparse
import time
from elevenlabs_setup import ElevenLabsVoiceCloner

def load_vern_dialogue():
    """
    Load the cleaned Vern dialogue from JSON
    """
    dialogue_path = "../../assets/dialogue/vern/VernDialogue.json"

    if not os.path.exists(dialogue_path):
        raise FileNotFoundError(f"Vern dialogue file not found: {dialogue_path}")

    with open(dialogue_path, 'r', encoding='utf-8') as f:
        dialogue_data = json.load(f)

    return dialogue_data

def extract_vern_lines(dialogue_data):
    """
    Extract all Vern dialogue lines from the JSON structure
    """
    vern_lines = []

    # Extract from different categories
    categories = [
        'introductionLines',
        'showOpeningLines',
        'showClosingLines',
        'betweenCallersLines',
        'deadAirFillerLines',
        'droppedCallerLines',
        'breakTransitionLines',
        'returnFromBreakLines',
        'offTopicRemarkLines'
    ]

    for category in categories:
        if category in dialogue_data:
            for line in dialogue_data[category]:
                # Handle different line formats
                if isinstance(line, dict) and 'text' in line:
                    vern_lines.append({
                        'id': line.get('id', f"{category}_{len(vern_lines)}"),
                        'text': line['text'],
                        'category': category,
                        'mood': line.get('mood', 'neutral'),
                        'weight': line.get('weight', 1.0)
                    })
                elif isinstance(line, dict) and 'mood' in line and 'text' in line:
                    # Handle mood-based lines (like showClosingLines)
                    vern_lines.append({
                        'id': line.get('id', f"{category}_{len(vern_lines)}"),
                        'text': line['text'],
                        'voiceText': line.get('voiceText'),  # May be None
                        'category': category,
                        'mood': line['mood'],
                        'weight': 1.0
                    })

    print(f"Found {len(vern_lines)} Vern dialogue lines")
    return vern_lines

def generate_audio_batch(vern_lines, cloner, output_dir="../../assets/audio/voice/Vern/ConversationArcs", force_regenerate=False, verbose=False):
    """
    Generate audio for all Vern dialogue lines
    """
    # Create output directory structure
    os.makedirs(output_dir, exist_ok=True)

    # Create subdirectories for moods
    moods = ['neutral', 'tired', 'energized', 'irritated', 'amused', 'focused', 'gruff']
    for mood in moods:
        os.makedirs(os.path.join(output_dir, mood), exist_ok=True)

    generated_count = 0
    failed_count = 0

    print(f"Starting batch generation of {len(vern_lines)} lines...")
    print(f"Output directory: {output_dir}")

    for i, line in enumerate(vern_lines):
        line_id = line['id']
        # Use voiceText if available, otherwise use text
        text = line.get('voiceText') or line['text']
        mood = line['mood']
        category = line['category']

        # Create filename
        safe_id = line_id.replace('/', '_').replace('\\', '_')
        output_path = os.path.join(output_dir, mood, f"{safe_id}.mp3")

        # Skip if file already exists (uncomment to force regeneration)
        # if os.path.exists(output_path):
        #     print(f"[{i+1}/{len(vern_lines)}] {line_id} - Already exists")
        #     continue

        try:
            print(f"[{i+1}/{len(vern_lines)}] Generating: {line_id}")
            print(f"   Text: '{text[:50]}...'")
            print(f"   Mood: {mood}, Category: {category}")

            # Adjust voice settings based on mood
            voice_settings = get_mood_voice_settings(mood)

            # Generate audio
            generated_path = cloner.generate_audio(
                text=text,
                output_path=output_path,
                model="eleven_flash_v2"
            )

            if generated_path:
                generated_count += 1
                print(f"   Saved: {generated_path}")
            else:
                failed_count += 1
                print(f"   Failed: {line_id}")

            # Rate limiting to avoid API limits
            time.sleep(0.5)  # 500ms between requests

        except Exception as e:
            print(f"   Error generating {line_id}: {e}")
            failed_count += 1

    print("\nBatch generation complete!")
    print(f"Generated: {generated_count} files")
    print(f"Failed: {failed_count} files")
    print(f"Output directory: {output_dir}")

    return generated_count, failed_count

def get_mood_voice_settings(mood):
    """
    Get voice settings optimized for different moods
    """
    base_settings = {
        "stability": 0.5,
        "similarity_boost": 0.8,
        "style": 0.5,
        "use_speaker_boost": True
    }

    # Adjust settings based on mood
    mood_adjustments = {
        "neutral": {"stability": 0.6, "style": 0.4},
        "tired": {"stability": 0.7, "style": 0.2},  # More stable, less expressive
        "energized": {"stability": 0.4, "style": 0.8, "similarity_boost": 0.9},  # More dynamic
        "irritated": {"stability": 0.5, "style": 0.7},  # More expressive
        "amused": {"stability": 0.5, "style": 0.6},  # Balanced
        "focused": {"stability": 0.7, "style": 0.3},  # More controlled
        "gruff": {"stability": 0.6, "style": 0.4}  # Balanced
    }

    if mood in mood_adjustments:
        base_settings.update(mood_adjustments[mood])

    return base_settings

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

def load_voice_id():
    """
    Load the voice ID from a saved file
    """
    voice_id_path = os.path.join(os.path.dirname(__file__), 'voice_id.txt')
    try:
        with open(voice_id_path, 'r') as f:
            return f.read().strip()
    except FileNotFoundError:
        return None

def main():
    """
    Main function for batch Vern audio generation
    """
    parser = argparse.ArgumentParser(description="Generate Vern dialogue audio")
    parser.add_argument('--force', '-f', action='store_true',
                       help='Force regeneration of all files (ignore existing)')
    parser.add_argument('--verbose', '-v', action='store_true',
                       help='Show detailed progress for each file')
    parser.add_argument('--arc', type=str,
                       help='Generate audio for specific conversation arc (e.g., "ufos_compelling_pilot")')

    args = parser.parse_args()

    print("Vern Tell - ElevenLabs Batch Audio Generation")
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

        # Load existing voice ID
        voice_id_path = os.path.join(os.path.dirname(__file__), 'voice_id.txt')
        try:
            with open(voice_id_path, 'r') as f:
                cloner.voice_id = f.read().strip()
            print(f"Loaded cloned Vern voice ID: {cloner.voice_id}")
        except FileNotFoundError:
            print("No voice ID file found!")
            print("Please run elevenlabs_setup.py first to create a voice clone")
            return

        # Load dialogue data
        print("\nLoading Vern dialogue...")
        dialogue_data = load_vern_dialogue()
        vern_lines = extract_vern_lines(dialogue_data)

        if args.arc:
            # Filter to specific arc
            vern_lines = [line for line in vern_lines if args.arc in line['id']]
            if not vern_lines:
                print(f"No Vern lines found for arc: {args.arc}")
                return
            print(f"Filtered to {len(vern_lines)} lines for arc: {args.arc}")

        # Generate Vern audio
        generated, failed = generate_audio_batch(vern_lines, cloner, force_regenerate=args.force, verbose=args.verbose)

        if generated > 0:
            print("\nSuccess! Vern Tell voice audio is ready!")
            print(f"Generated {generated} Vern dialogue lines")
        if failed > 0:
            print(f"\nNote: {failed} files failed to generate. You may need to retry them.")

    except Exception as e:
        print(f"Error: {e}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    main()