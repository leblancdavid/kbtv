import os
import json
import time
from elevenlabs_setup import ElevenLabsVoiceCloner

def get_topic_from_arc_id(arc_id):
    """Map arc_id to topic folder"""
    if arc_id.startswith("ufos") or "ufos_" in arc_id:
        return "UFOs"
    if arc_id.startswith("ghosts") or "ghosts_" in arc_id:
        return "Ghosts"
    if arc_id.startswith("cryptids") or "cryptids_" in arc_id or "cryptid_" in arc_id:
        return "Cryptids"
    if arc_id.startswith("conspiracies") or "conspiracies_" in arc_id:
        return "Conspiracies"
    return "UFOs"  # Default

def get_arc_folder_name(arc_id):
    """Get the actual folder name used for this arc"""
    folder_name_map = {
        'conspiracies_credible_govt_contractor': 'govt_contractor',
        'ghosts_credible_old_house': 'old_house',
        'cryptids_credible_forest_hiker': 'forest_hiker',
        'cryptid_credible_claims_ufos': 'claims_ufos',
        'ufos_credible_dashcam': 'dashcam_trucker',
        'conspiracies_compelling_whistleblower': 'whistleblower',
        'conspiracies_questionable_patterns': 'patterns',
        'conspiracies_fake_tinfoil': 'tinfoil',
        'ghosts_compelling_investigator': 'investigator',
        'ghosts_fake_halloween': 'halloween',
        'ghosts_questionable_footsteps': 'footsteps',
        'cryptids_compelling_biologist': 'biologist',
        'cryptids_fake_costume': 'costume',
        'cryptids_questionable_shadow': 'shadow',
        'ufos_compelling_pilot': 'pilot',
        'ufos_fake_prankster': 'prankster',
        'ufos_questionable_lights': 'lights'
    }

    return folder_name_map.get(arc_id, arc_id.split('_')[-1])

def get_caller_archetype(arc_id):
    """Get appropriate caller voice archetype based on arc characteristics"""
    # This is a simplified mapping - in practice you'd use the callerPersonality from JSON
    if "compelling" in arc_id:
        return "enthusiastic"  # Credible, compelling callers
    elif "credible" in arc_id:
        return "default_male"  # Standard credible witnesses
    elif "questionable" in arc_id:
        return "nervous"  # Hesitant, questionable claims
    elif "fake" in arc_id:
        return "conspiracy"  # Intense conspiracy theorists
    else:
        return "default_male"  # Default

def generate_arc_audio(arc_id, force_regenerate=False, verbose=False, speaker_filter='both'):
    """Generate audio for a specific conversation arc"""
    print(f"Generating audio for arc: {arc_id} (speaker filter: {speaker_filter})")

    # Initialize ElevenLabs
    cloner = ElevenLabsVoiceCloner()

    # Load arc JSON
    topic = get_topic_from_arc_id(arc_id)
    arcs_dir = os.path.join("..", "..", "assets", "dialogue", "arcs", topic)
    json_file = os.path.join(arcs_dir, f"{get_arc_folder_name(arc_id)}.json")

    if not os.path.exists(json_file):
        print(f"ERROR: JSON file not found: {json_file}")
        return

    with open(json_file, 'r', encoding='utf-8') as f:
        arc_data = json.load(f)

    lines = arc_data.get('lines', [])
    print(f"Found {len(lines)} dialogue lines")

    generated_count = 0
    skipped_count = 0

    for line in lines:
        line_id = line.get('id', '')
        speaker = line.get('speaker', '').lower()
        text = line.get('voiceText', line.get('text', ''))
        mood = line.get('mood', '')

        if not line_id or not text:
            print(f"Skipping invalid line: {line}")
            continue

        # Apply speaker filter
        if speaker_filter != 'both' and speaker != speaker_filter:
            if verbose:
                print(f"SKIPPING: {line_id} (speaker filter: {speaker_filter})")
            continue

        # Determine output directory based on speaker
        if speaker == 'vern':
            output_base = os.path.join("..", "..", "assets", "audio", "voice", "Vern", "ConversationArcs", topic)
        elif speaker == 'caller':
            output_base = os.path.join("..", "..", "assets", "audio", "voice", "Callers", topic)
        else:
            print(f"WARNING: Unknown speaker '{speaker}' for line {line_id}, skipping")
            continue

        arc_folder = get_arc_folder_name(arc_id)
        output_dir = os.path.join(output_base, arc_folder)
        os.makedirs(output_dir, exist_ok=True)

        # Determine output path
        output_path = os.path.join(output_dir, f"{line_id}.mp3")

        # Check if file exists
        if os.path.exists(output_path) and not force_regenerate:
            if verbose:
                print(f"SKIPPING: {line_id} (already exists)")
            skipped_count += 1
            continue

        # Determine voice parameters
        if speaker == 'vern':
            # Use cloned Vern voice with mood adjustments
            voice_id = cloner.voice_id  # Art Bell clone
            if not voice_id:
                print(f"ERROR: No Vern voice ID available")
                continue

            # Mood-based voice settings
            stability = 0.5
            similarity_boost = 0.8
            style = 0.5

            if mood == 'tired':
                stability = 0.3  # Less stable for tired
                style = 0.3
            elif mood == 'energized':
                stability = 0.7
                style = 0.8  # More expressive
            elif mood == 'irritated':
                stability = 0.6
                style = 0.4
            elif mood == 'amused':
                stability = 0.6
                style = 0.7
            elif mood == 'focused':
                stability = 0.8
                style = 0.3
            elif mood == 'gruff':
                stability = 0.7
                style = 0.2

            # Generate with mood settings
            try:
                result_path = cloner.generate_audio(
                    text=text,
                    output_path=output_path,
                    voice_id=voice_id,
                    stability=stability,
                    similarity_boost=similarity_boost,
                    style=style
                )
                print(f"GENERATED: {line_id}")
                generated_count += 1

            except Exception as e:
                print(f"ERROR generating {line_id}: {e}")

        elif speaker == 'caller':
            # Use caller archetype
            archetype = get_caller_archetype(arc_id)
            try:
                result_path = cloner.generate_audio(
                    text=text,
                    output_path=output_path,
                    voice_id=archetype
                )
                print(f"GENERATED: {line_id}")
                generated_count += 1

            except Exception as e:
                print(f"ERROR generating {line_id}: {e}")

        # Rate limiting - be gentle with API
        time.sleep(2)  # 2 second delay between requests

    print(f"\nCompleted: {generated_count} generated, {skipped_count} skipped")

if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description='Generate audio for a conversation arc')
    parser.add_argument('arc_id', help='Arc ID to generate audio for (e.g., conspiracies_credible_govt_contractor)')
    parser.add_argument('--force', action='store_true', help='Regenerate existing files')
    parser.add_argument('--verbose', action='store_true', help='Verbose output')
    parser.add_argument('--speaker', choices=['vern', 'caller', 'both'], default='both',
                        help='Which speakers to generate audio for (default: both)')

    args = parser.parse_args()

    generate_arc_audio(args.arc_id, args.force, args.verbose, args.speaker)