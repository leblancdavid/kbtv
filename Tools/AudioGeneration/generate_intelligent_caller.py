#!/usr/bin/env python3
"""
Generate Intelligent Caller Audio: Personality + Legitimacy + Gender
Test with UFO pilot arc using sophisticated voice selection

Usage:
  python generate_intelligent_caller.py              # Skip existing files
  python generate_intelligent_caller.py --force      # Regenerate all files
  python generate_intelligent_caller.py --verbose    # Detailed output
"""

import os
import json
import glob
import random
import argparse
import time
from elevenlabs_setup import ElevenLabsVoiceCloner

# Personality to voice archetype mapping
personality_voice_map = {
    "cold_factual": {
        "primary": ["default_male", "default_female"],
        "secondary": ["conspiracy", "enthusiastic"],
        "style": "calm_professional"
    },
    "gruff_experienced": {
        "primary": ["gruff", "elderly_male", "elderly_female"],
        "secondary": ["default_male", "default_female"],
        "style": "rough_weathered"
    },
    "nervous_hesitant": {
        "primary": ["nervous"],
        "secondary": ["default_male", "default_female"],
        "style": "anxious_uncertain"
    },
    "charismatic_storyteller": {
        "primary": ["enthusiastic", "conspiracy"],
        "secondary": ["nervous", "default_male"],
        "style": "engaging_theatrical"
    },
    "emotional_wreck": {
        "primary": ["nervous", "default_female"],
        "secondary": ["gruff", "elderly_female"],
        "style": "distressed_emotional"
    },
    "enthusiastic_convert": {
        "primary": ["enthusiastic"],
        "secondary": ["nervous", "conspiracy"],
        "style": "excited_convinced"
    }
}

# Topic-specific voice pools
caller_archetype_mapping = {
    "UFOs": ["default_male", "enthusiastic", "nervous", "elderly_male"],
    "Cryptids": ["gruff", "default_male", "nervous", "elderly_male", "elderly_female"],
    "Conspiracies": ["conspiracy", "nervous", "default_male", "elderly_male"],
    "Ghosts": ["default_female", "nervous", "gruff", "elderly_female"]
}

def load_api_key():
    """Load ElevenLabs API key"""
    config_path = os.path.join(os.path.dirname(__file__), 'elevenlabs_config.json')
    try:
        with open(config_path, 'r') as f:
            config = json.load(f)
            return config.get('elevenlabs_api_key')
    except (FileNotFoundError, json.JSONDecodeError, KeyError):
        return None

def extract_caller_characteristics(arc_data):
    """Extract personality, legitimacy, gender, and topic"""
    return {
        'personality': arc_data.get('callerPersonality'),
        'legitimacy': arc_data.get('legitimacy'),
        'gender': arc_data.get('callerGender', 'male'),
        'topic': arc_data.get('topic')
    }

def select_optimal_voice(personality, legitimacy, gender, topic):
    """
    Intelligent voice selection based on personality, legitimacy, gender, and topic
    """
    # Get personality preferences
    personality_prefs = personality_voice_map.get(personality, {})
    primary_voices = personality_prefs.get("primary", [])
    secondary_voices = personality_prefs.get("secondary", [])

    # Filter by gender
    primary_gendered = [v for v in primary_voices if gender in v or v in ["nervous", "gruff", "conspiracy", "enthusiastic"]]
    secondary_gendered = [v for v in secondary_voices if gender in v or v in ["nervous", "gruff", "conspiracy", "enthusiastic"]]

    # Topic-specific filtering
    topic_voices = caller_archetype_mapping.get(topic, [])
    available_voices = primary_gendered + secondary_gendered
    topic_filtered = [v for v in available_voices if v in topic_voices]

    # Select voice with personality priority
    if topic_filtered:
        selected_voice = random.choice(topic_filtered)
    elif available_voices:
        selected_voice = random.choice(available_voices)
    else:
        # Ultimate fallback
        selected_voice = f"default_{gender}"

    print(f"Voice Selection: {personality} + {legitimacy} + {gender} + {topic} = {selected_voice}")
    return selected_voice

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
    """Extract caller dialogue lines"""
    caller_lines = []
    arc_id = arc_data.get('arcId', 'unknown')

    for i, dialogue_item in enumerate(arc_data.get('dialogue', [])):
        if dialogue_item.get('speaker') == 'Caller':
            caller_lines.append({
                'arc_id': arc_id,
                'line_index': i,
                'text': dialogue_item.get('text', ''),
                'speaker': 'Caller'
            })

    return caller_lines

def generate_caller_sample_audio():
    """
    Generate sample caller audio using intelligent voice selection
    """
    print("Generating Intelligent Caller Audio Sample")
    print("=" * 55)

    # Load API key
    api_key = load_api_key()
    if not api_key:
        print("No API key found")
        return

    # Initialize ElevenLabs client
    try:
        cloner = ElevenLabsVoiceCloner(api_key)

        # Load existing voice ID
        voice_id_path = os.path.join(os.path.dirname(__file__), 'voice_id.txt')
        try:
            with open(voice_id_path, 'r') as f:
                cloner.voice_id = f.read().strip()
            print("ElevenLabs client initialized")
            print(f"Loaded voice ID: {cloner.voice_id}")
        except FileNotFoundError:
            print("No voice ID file found. Please run elevenlabs_setup.py first to create a voice clone.")
            return
    except Exception as e:
        print(f"Initialization failed: {e}")
        return

    # Load pilot arc
    arc_path = "../../assets/dialogue/arcs/UFOs/Compelling/pilot.json"
    print(f"\nLoading conversation arc: {arc_path}")

    arc_data = json.load(open(arc_path, 'r', encoding='utf-8'))
    if not arc_data:
        print("Failed to load conversation arc")
        return

    # Extract caller characteristics
    characteristics = extract_caller_characteristics(arc_data)
    print(f"Caller Characteristics:")
    print(f"   Personality: {characteristics['personality']}")
    print(f"   Legitimacy: {characteristics['legitimacy']}")
    print(f"   Gender: {characteristics['gender']}")
    print(f"   Topic: {characteristics['topic']}")

    # Select optimal voice
    voice_archetype = select_optimal_voice(
        characteristics['personality'],
        characteristics['legitimacy'],
        characteristics['gender'],
        characteristics['topic']
    )

    # Extract caller lines (first 2 for testing)
    caller_lines = extract_caller_lines(arc_data)[:2]
    print(f"\nFound {len(caller_lines)} caller lines (using first 2 for testing)")

    # Create output directory
    arc_id = arc_data.get('arcId', 'unknown')
    topic = arc_data.get('topic', 'Unknown')
    output_dir = f"../../assets/audio/voice/Callers/{topic}/{arc_id}"
    os.makedirs(output_dir, exist_ok=True)

    # Generate audio for each caller line
    generated_count = 0
    print("\nGenerating caller audio...")
    for line in caller_lines:
        line_index = line['line_index']
        text = line['text']

        # Create filename
        filename = f"caller_{line_index:03d}.mp3"
        output_path = os.path.join(output_dir, filename)

        # Skip if exists
        if os.path.exists(output_path):
            print(f"  {filename} - Already exists")
            continue

        print(f"  Generating: {filename}")
        print(f"   Voice: {voice_archetype}")
        print(f"   Text: '{text[:60]}...'")

        # Debug: Print what we're sending
        print(f"   DEBUG: Sending voice_archetype='{voice_archetype}' to generate_audio")

        # Generate audio
        result = cloner.generate_audio(
            text=text,
            output_path=output_path,
            voice_id=voice_archetype,  # Explicitly pass voice archetype
            model="eleven_flash_v2"
        )

        if result:
            generated_count += 1
            print(f"   Generated: {filename}")
        else:
            print(f"   Failed: {filename}")

        # Rate limiting
        import time
        time.sleep(1)

    print("\nGeneration Complete!")
    print(f"Generated: {generated_count} caller audio files")
    print(f"Voice Archetype: {voice_archetype}")
    print(f"Characteristics: {characteristics['personality']} {characteristics['gender']} ({characteristics['legitimacy']} {characteristics['topic']})")
    print(f"\nAudio files saved to: {output_dir}")

    if generated_count > 0:
        print("\nListen to the generated files to evaluate:")
        print(f"   - Does the voice match a {characteristics['personality']} {characteristics['gender']} caller?")
        print(f"   - Is the delivery appropriate for a {characteristics['legitimacy']} {characteristics['topic']} witness?")
        print("   - How does it contrast with Vern's hosting voice?")
if __name__ == "__main__":
    generate_caller_sample_audio()