#!/usr/bin/env python3
import re

def extract_arc_ids():
    """Extract unique arc IDs from missing_audio.txt"""
    arc_ids = set()

    with open('../../missing_audio.txt', 'r') as f:
        for line in f:
            if line.startswith('Vern/ConversationArcs/'):
                # Extract arc ID from path like:
                # Vern/ConversationArcs/Conspiracies/patterns/conspiracies_questionable_patterns_vern_amused_1.mp3
                # The arc ID is everything before '_vern_'
                parts = line.strip().split('/')
                if len(parts) >= 4:
                    filename = parts[-1]  # e.g., "conspiracies_questionable_patterns_vern_amused_1.mp3"
                    # Extract everything before '_vern_'
                    vern_index = filename.find('_vern_')
                    if vern_index > 0:
                        arc_id = filename[:vern_index]
                        arc_ids.add(arc_id)

    return sorted(list(arc_ids))

if __name__ == "__main__":
    arc_ids = extract_arc_ids()
    print(f"Found {len(arc_ids)} unique arc IDs:")
    for arc_id in arc_ids:
        print(arc_id)