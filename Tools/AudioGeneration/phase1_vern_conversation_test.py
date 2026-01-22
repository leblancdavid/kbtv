#!/usr/bin/env python3
"""
Phase 1: Generate Vern Lines from One Conversation Arc
Test the cloned Vern voice in conversation context
"""

import os
import json
import time
from elevenlabs_setup import ElevenLabsVoiceCloner

def load_api_key():
    """Load ElevenLabs API key"""
    config_path = os.path.join(os.path.dirname(__file__), 'elevenlabs_config.json')
    try:
        with open(config_path, 'r') as f:
            config = json.load(f)
            return config.get('elevenlabs_api_key')
    except (FileNotFoundError, json.JSONDecodeError, KeyError):
        return None

def load_conversation_arc(arc_path):
    """Load a conversation arc JSON file"""
    try:
        with open(arc_path, 'r', encoding='utf-8') as f:
            return json.load(f)
    except Exception as e:
        print(f"Error loading {arc_path}: {e}")
        return None

def extract_vern_lines_from_arc(arc_data):
    """Extract Vern dialogue lines with mood variants"""
    vern_lines = []
    arc_id = arc_data.get('arcId', 'unknown')

    for i, dialogue_item in enumerate(arc_data.get('dialogue', [])):
        if dialogue_item.get('speaker') == 'Vern':
            text_variants = dialogue_item.get('textVariants', [])

            for j, variant in enumerate(text_variants):
                mood = variant.get('mood', 'neutral')
                # Use voiceText if available, otherwise text
                text = variant.get('voiceText', variant.get('text', ''))

                if text:  # Only if text exists
                    vern_lines.append({
                        'arc_id': arc_id,
                        'line_index': i,
                        'variant_index': j,
                        'mood': mood,
                        'text': text,
                        'id': f"vern_{arc_id}_{mood}_{i:03d}_{j:03d}"
                    })

    return vern_lines

def generate_vern_audio_batch(vern_lines, cloner, output_dir="../../assets/audio/voice/Vern/ConversationArcs"):
    """Generate audio for Vern lines using cloned voice"""
    # Create output directory structure
    os.makedirs(output_dir, exist_ok=True)

    generated_count = 0
    failed_count = 0

    print(f"Starting Vern audio generation for {len(vern_lines)} lines...")

    for i, line in enumerate(vern_lines):
        arc_id = line['arc_id']
        mood = line['mood']
        text = line['text']
        line_id = line['id']

        # Create mood subdirectory
        mood_dir = os.path.join(output_dir, arc_id, mood)
        os.makedirs(mood_dir, exist_ok=True)

        # Create filename
        filename = f"{line_id}.mp3"
        output_path = os.path.join(mood_dir, filename)

        # Skip if file already exists
        if os.path.exists(output_path):
            print(f"[{i+1}/{len(vern_lines)}] {line_id} - Already exists")
            continue

        try:
            print(f"[{i+1}/{len(vern_lines)}] Generating: {line_id}")
            print(f"   Mood: {mood}")
            print(f"   Text: '{text[:60]}...'")

            # Generate audio using cloned Vern voice
            result = cloner.generate_audio(
                text=text,
                output_path=output_path,
                model="eleven_flash_v2"
            )

            if result:
                generated_count += 1
                print(f"   Generated: {filename}")
            else:
                failed_count += 1
                print(f"   Failed: {line_id}")

            # Rate limiting to avoid API limits (longer delay for stability)
            print("   Waiting 1 second for API rate limiting...")
            time.sleep(1)  # 1 second between requests

        except Exception as e:
            print(f"   Error generating {line_id}: {e}")
            failed_count += 1

    print("\nGeneration complete!")
    print(f"Generated: {generated_count} files")
    print(f"Failed: {failed_count} files")
    print(f"Output directory: {output_dir}")

    return generated_count, failed_count

def main():
    """Phase 1: Generate Vern lines from one conversation arc"""
    print("Phase 1: Vern Lines from Conversation Arc")
    print("=" * 45)

    # Load API key
    api_key = load_api_key()
    if not api_key:
        print("No API key found!")
        print("Please check elevenlabs_config.json")
        return

    # Initialize ElevenLabs client
    try:
        cloner = ElevenLabsVoiceCloner(api_key)

        # Load existing voice ID
        voice_id_path = os.path.join(os.path.dirname(__file__), 'voice_id.txt')
        try:
            with open(voice_id_path, 'r') as f:
                cloner.voice_id = f.read().strip()
            print(f"Loaded cloned Vern voice ID: {cloner.voice_id}")
        except FileNotFoundError:
            print("No voice ID file found!")
            print("Please run elevenlabs_setup.py first to create the cloned voice")
            return

    except Exception as e:
        print(f"Error initializing ElevenLabs: {e}")
        return

    # Load the test conversation arc
    arc_path = "../../assets/dialogue/arcs/UFOs/Compelling/pilot.json"
    print(f"\nLoading conversation arc: {arc_path}")

    arc_data = load_conversation_arc(arc_path)
    if not arc_data:
        print("Failed to load conversation arc")
        return

    # Extract Vern lines
    vern_lines = extract_vern_lines_from_arc(arc_data)
    print(f"Found {len(vern_lines)} Vern dialogue lines in {arc_data.get('arcId', 'unknown')}")

    if not vern_lines:
        print("No Vern lines found in this arc")
        return

    # Confirm generation
    print(f"\nAbout to generate {len(vern_lines)} Vern audio files")
    print("This will use your cloned Vern voice")
    print("Files will be organized by mood in the conversation arc folder")

    try:
        response = input("\nContinue with generation? (y/N): ").lower().strip()
        if response != 'y':
            print("Generation cancelled")
            return
    except EOFError:
        # Handle case where input is not available (e.g., non-interactive)
        print("Non-interactive mode detected, proceeding with generation...")

    # Generate Vern audio
    generated, failed = generate_vern_audio_batch(vern_lines, cloner)

    if generated > 0:
        print("\nPhase 1 Complete!")
        print(f"Successfully generated {generated} Vern dialogue lines")
        print("Listen to the audio files to verify voice quality")

        arc_id = arc_data.get('arcId', 'unknown')
        print(f"\nAudio files created in:")
        print(f"assets/audio/voice/Vern/ConversationArcs/{arc_id}/")
        print("\nSubfolders by mood: neutral, tired, energized, irritated, gruff, amused, focused")
    else:
        print("\nNo files were generated")
        if failed > 0:
            print(f"{failed} files failed - check API connection and voice ID")

if __name__ == "__main__":
    main()