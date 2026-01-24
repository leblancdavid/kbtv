#!/usr/bin/env python3
"""
Generate audio for break transitions from VernDialogue.json
Only generates the break transition lines needed for ad breaks
"""

import os
import json
import time
from elevenlabs_setup import ElevenLabsVoiceCloner

def get_mood_voice_settings(mood):
    """Get voice settings based on mood"""
    base_settings = {
        'stability': 0.5,
        'similarity_boost': 0.8,
        'style': 0.5
    }

    mood_settings = {
        'neutral': {'stability': 0.5, 'similarity_boost': 0.8, 'style': 0.5},
        'tired': {'stability': 0.3, 'similarity_boost': 0.7, 'style': 0.3},
        'energized': {'stability': 0.7, 'similarity_boost': 0.9, 'style': 0.8},
        'irritated': {'stability': 0.6, 'similarity_boost': 0.8, 'style': 0.4},
        'amused': {'stability': 0.6, 'similarity_boost': 0.8, 'style': 0.7},
        'focused': {'stability': 0.8, 'similarity_boost': 0.8, 'style': 0.3},
        'gruff': {'stability': 0.7, 'similarity_boost': 0.7, 'style': 0.2}
    }

    return mood_settings.get(mood, base_settings)

def generate_break_audio(force_regenerate=False, verbose=False):
    """Generate audio for break transitions only"""

    print("Generating break transition audio...")
    print("=" * 50)

    # Initialize ElevenLabs
    cloner = ElevenLabsVoiceCloner()
    if not cloner.voice_id:
        print("ERROR: No Vern voice ID available. Run elevenlabs_setup.py first.")
        return

    # Load VernDialogue.json
    vern_dialogue_path = os.path.join("..", "..", "assets", "dialogue", "vern", "VernDialogue.json")
    if not os.path.exists(vern_dialogue_path):
        print(f"ERROR: VernDialogue.json not found at {vern_dialogue_path}")
        return

    with open(vern_dialogue_path, 'r', encoding='utf-8') as f:
        vern_data = json.load(f)

    break_transitions = vern_data.get('breakTransitions', [])
    print(f"Found {len(break_transitions)} break transitions")

    generated_count = 0
    skipped_count = 0

    # Output directory for break audio
    output_base = os.path.join("..", "..", "assets", "audio", "voice", "Vern", "Broadcast")
    os.makedirs(output_base, exist_ok=True)

    for transition in break_transitions:
        line_id = transition.get('id', '')
        text = transition.get('voiceText', transition.get('text', ''))
        mood = transition.get('mood', 'neutral')

        if not line_id or not text:
            print(f"Skipping invalid transition: {transition}")
            continue

        # Output path
        output_path = os.path.join(output_base, f"{line_id}.mp3")

        # Check if file exists
        if os.path.exists(output_path) and not force_regenerate:
            if verbose:
                print(f"SKIPPING: {line_id} (already exists)")
            skipped_count += 1
            continue

        # Get mood-based voice settings
        voice_settings = get_mood_voice_settings(mood)

        try:
            result_path = cloner.generate_audio(
                text=text,
                output_path=output_path,
                voice_id=cloner.voice_id,
                stability=voice_settings['stability'],
                similarity_boost=voice_settings['similarity_boost'],
                style=voice_settings['style']
            )
            print(f"GENERATED: {line_id} (mood: {mood})")
            generated_count += 1

        except Exception as e:
            print(f"ERROR generating {line_id}: {e}")

        # Rate limiting - be gentle with API
        time.sleep(2)  # 2 second delay between requests

    print(f"\nCompleted: {generated_count} generated, {skipped_count} skipped")
    print(f"Break transition audio saved to: {output_base}")

if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description='Generate audio for break transitions')
    parser.add_argument('--force', action='store_true', help='Regenerate existing files')
    parser.add_argument('--verbose', action='store_true', help='Verbose output')

    args = parser.parse_args()

    generate_break_audio(args.force, args.verbose)