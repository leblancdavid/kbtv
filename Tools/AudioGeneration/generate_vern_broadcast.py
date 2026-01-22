#!/usr/bin/env python3
"""
Generate Vern Broadcast Audio using ElevenLabs
Creates audio for show openings, closings, between-callers, etc.
"""

import os
import json
import argparse
import time
from pathlib import Path
from elevenlabs_setup import ElevenLabsVoiceCloner

def load_vern_dialogue():
    """Load the Vern dialogue from JSON"""
    dialogue_path = "../../assets/dialogue/vern/VernDialogue.json"

    if not os.path.exists(dialogue_path):
        raise FileNotFoundError(f"Vern dialogue file not found: {dialogue_path}")

    with open(dialogue_path, 'r', encoding='utf-8') as f:
        dialogue_data = json.load(f)

    return dialogue_data

def extract_broadcast_lines(dialogue_data):
    """Extract broadcast lines (not conversation arcs)"""
    broadcast_lines = []

    # Categories that are broadcast (not conversation-specific)
    broadcast_categories = [
        'showOpeningLines',
        'showClosingLines',
        'betweenCallersLines',
        'deadAirFillerLines',
        'droppedCallerLines',
        'breakTransitionLines',
        'returnFromBreakLines',
        'offTopicRemarkLines'
    ]

    for category in broadcast_categories:
        if category in dialogue_data:
            for line in dialogue_data[category]:
                # Handle different line formats
                if isinstance(line, dict) and 'text' in line:
                    broadcast_lines.append({
                        'id': line.get('id', f"{category}_{len(broadcast_lines)}"),
                        'text': line['text'],
                        'voiceText': line.get('voiceText'),  # May be None
                        'category': category,
                        'mood': line.get('mood', 'neutral'),
                        'weight': line.get('weight', 1.0)
                    })

    print(f"Found {len(broadcast_lines)} Vern broadcast lines")
    return broadcast_lines

def generate_broadcast_audio(lines, cloner, output_dir="../../assets/audio/voice/Vern/Broadcast", force_regenerate=False):
    """Generate broadcast audio using ElevenLabs"""

    generated_count = 0
    failed_count = 0

    print(f"Starting broadcast generation of {len(lines)} lines...")
    print(f"Output directory: {output_dir}")

    for i, line in enumerate(lines):
        line_id = line['id']
        # Use voiceText if available, otherwise use text
        text = line.get('voiceText') or line['text']
        mood = line['mood']

        # Create output path: {mood}/{line_id}_{mood}.mp3
        output_path = os.path.join(output_dir, mood, f"{line_id}_{mood}.mp3")

        # Check if file exists
        if os.path.exists(output_path) and not force_regenerate:
            print(f"[{i+1}/{len(lines)}] Skipping: {line_id}_{mood}.mp3 (already exists)")
            continue

        try:
            print(f"[{i+1}/{len(lines)}] Generating: {line_id}_{mood}.mp3")
            print(f"   Text: '{text[:50]}...'")

            # Adjust voice settings based on mood
            voice_settings = get_mood_voice_settings(mood)

            # Generate audio
            result = cloner.generate_audio(
                text=text,
                output_path=output_path,
                model="eleven_flash_v2"
            )

            if result:
                generated_count += 1
                print(f"   Saved: {output_path}")
            else:
                failed_count += 1
                print(f"   Failed: {line_id}_{mood}.mp3")

            # Rate limiting
            time.sleep(0.5)

        except Exception as e:
            print(f"   Error generating {line_id}_{mood}.mp3: {e}")
            failed_count += 1

    print("\nBroadcast generation complete!")
    print(f"Generated: {generated_count} files")
    print(f"Failed: {failed_count} files")
    print(f"Output directory: {output_dir}")

    return generated_count, failed_count

def get_mood_voice_settings(mood):
    """Get voice settings optimized for different moods"""
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

def main():
    parser = argparse.ArgumentParser(description="Generate Vern broadcast audio")
    parser.add_argument('--force', '-f', action='store_true', help='Force regeneration of all files')
    parser.add_argument('--verbose', '-v', action='store_true', help='Show detailed output')

    args = parser.parse_args()

    # Load dialogue
    dialogue_data = load_vern_dialogue()
    broadcast_lines = extract_broadcast_lines(dialogue_data)

    # Initialize ElevenLabs
    cloner = ElevenLabsVoiceCloner()

    # Generate audio
    generated, failed = generate_broadcast_audio(broadcast_lines, cloner, force_regenerate=args.force)

    print(f"\nResult: {generated} generated, {failed} failed")

if __name__ == "__main__":
    main()