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
        # Conspiracies
        'conspiracies_credible_govt_contractor': 'govt_contractor',
        'conspiracies_compelling_whistleblower': 'whistleblower',
        'conspiracies_questionable_patterns': 'patterns',
        'conspiracies_fake_tinfoil': 'tinfoil',
        # Ghosts
        'ghosts_credible_old_house': 'old_house',
        'ghosts_compelling_investigator': 'investigator',
        'ghosts_fake_halloween': 'halloween',
        'ghosts_questionable_footsteps': 'footsteps',
        # Cryptids
        'cryptids_credible_forest_hiker': 'forest_hiker',
        'cryptids_compelling_biologist': 'biologist',
        'cryptids_fake_costume': 'costume',
        'cryptids_questionable_shadow': 'shadow',
        'cryptids_credible_claims_ufos': 'claims_ufos',
        # UFOs - legacy mappings
        'ufos_credible_dashcam_trucker': 'dashcam_trucker',
        'ufos_compelling_pilot': 'pilot',
        'ufos_fake_prankster': 'prankster',
        'ufos_questionable_lights': 'lights',
        # UFOs - simple names (use as-is)
        'cowboy_witness': 'cowboy_witness',
        'pilot_friend': 'pilot_friend',
        'truck_driver': 'truck_driver',
        'hiking_couple': 'hiking_couple',
        'gov_contractor': 'gov_contractor',
        'grain_silo_worker': 'grain_silo_worker',
        'rural_teacher': 'rural_teacher',
        'amateur_astronomer': 'amateur_astronomer',
        'telephone_technician': 'telephone_technician',
        'security_footage': 'security_footage',
        'party_confession': 'party_confession',
        'atmospheric_scientist': 'atmospheric_scientist',
        'construction_worker': 'construction_worker',
        'star_gazer': 'star_gazer',
        'topic_switch_cryptid': 'topic_switch_cryptid',
        'military_physicist': 'military_physicist',
        'electrical_phenomenon': 'electrical_phenomenon',
        'railroad_worker': 'railroad_worker',
        'newspaper_photo': 'newspaper_photo',
        'lake_reflection': 'lake_reflection',
        'movie_hoax': 'movie_hoax',
        'dream_abduction': 'dream_abduction',
        'rancher_encounter': 'rancher_encounter',
        'drone_sighting': 'drone_sighting',
        'camping_prank': 'camping_prank',
        'air_traffic_contact': 'air_traffic_contact',
        'business_traveler': 'business_traveler',
        'plane_confusion': 'plane_confusion',
        'topic_switch_ghost': 'topic_switch_ghost',
        'physicist_observation': 'physicist_observation',
        'radar_operator': 'radar_operator',
        'night_watchman': 'night_watchman',
        'chemist_witness': 'chemist_witness',
        'buddy_story': 'buddy_story',
        'neighbor_lights': 'neighbor_lights',
        'bonfire_joke': 'bonfire_joke',
        'night_picnic': 'night_picnic',
        'ufo_cryptid_switch': 'ufo_cryptid_switch',
    }

    # Check if mapped
    if arc_id in folder_name_map:
        return folder_name_map[arc_id]

    # Handle ufo_ prefix - if arc_id is like "ufo_skywriter", use as-is
    if arc_id.startswith('ufo_'):
        return arc_id

    # Default: use last part after splitting
    return arc_id.split('_')[-1]

def get_caller_voice_for_arc(arc_id, line_index):
    """
    Get a caller voice that cycles through the 20-voice pool based on arc_id.
    This ensures each arc gets a consistent voice for all caller lines,
    but different arcs get different voices for variety.
    """
    return f"caller_pool_{arc_id}_{line_index}"

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

    # Get caller gender for voice selection
    caller_gender = arc_data.get('callerGender', 'male').lower()
    if caller_gender not in ['male', 'female']:
        caller_gender = 'male'  # Default to male
    
    print(f"Caller gender: {caller_gender}")

    lines = arc_data.get('arcLines', [])
    print(f"Found {len(lines)} dialogue groups")
    
    # Flatten the nested structure: each group has speaker + lines array
    flat_lines = []
    for group in lines:
        speaker = group.get('speaker', '').lower()
        speaker_lines = group.get('lines', [])
        for line_entry in speaker_lines:
            flat_line = line_entry.copy()
            flat_line['speaker'] = speaker
            flat_lines.append(flat_line)
    
    print(f"Found {len(flat_lines)} total dialogue lines after flattening")

    generated_count = 0
    skipped_count = 0

    for line in flat_lines:
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
            # Use caller voice from gender-specific pool (consistent per arc)
            # Use hash of arc_id only - same voice for all caller lines in an arc
            import hashlib
            arc_hash = int(hashlib.md5(arc_id.encode()).hexdigest(), 16) % 1000
            caller_voice = f"caller_pool_{caller_gender}_{arc_hash}"
            
            # Get voice settings override if available (for older/southern effects)
            voice_settings_override = {
                "nPczCjzI2devNBz1zQrb": {"stability": 0.2, "style": 0.1},
                "JBFqnCBsd6RMkjVDRZzb": {"stability": 0.25, "style": 0.15},
                "onwK4e9ZLuTAKqWW03F9": {"stability": 0.2, "style": 0.1},
                "CwhRBWXzGAHq8TQ4Fs17": {"stability": 0.6, "style": 0.3},
                "IKne3meq5aSn9XLyUdCD": {"stability": 0.5, "style": 0.4},
                "bIHbv24MWmeRgasZH58o": {"stability": 0.55, "style": 0.35},
                "iP95p4xoKVk53GoZ742B": {"stability": 0.5, "style": 0.3},
            }
            
            # Apply overrides if this voice has special settings
            override = voice_settings_override.get(caller_voice, {})
            stability = override.get("stability", 0.5)
            style = override.get("style", 0.5)
            
            try:
                result_path = cloner.generate_audio(
                    text=text,
                    output_path=output_path,
                    voice_id=caller_voice,
                    stability=stability,
                    similarity_boost=0.8,
                    style=style
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