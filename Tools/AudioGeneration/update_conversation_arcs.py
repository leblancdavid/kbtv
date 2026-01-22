#!/usr/bin/env python3
"""
Update Conversation Arcs: Add Gender & Voice Text Fields
Process all conversation JSON files to add callerGender and voiceText fields
"""

import os
import json
import glob
from pathlib import Path

def get_caller_gender_from_personality(personality):
    """
    Infer gender from caller personality or use defaults
    """
    # Based on existing personalities, assign genders
    gender_hints = {
        'female': ['investigator', 'biologist', 'old_house'],
        'male': ['pilot', 'contractor', 'hiker', 'controller', 'trucker', 'conspiracist', 'whistleblower']
    }

    personality_lower = personality.lower()
    for gender, hints in gender_hints.items():
        if any(hint in personality_lower for hint in hints):
            return gender

    # Default to male for most caller types
    return 'male'

def add_voice_text(text, caller_name="caller", location="out there"):
    """
    Convert text with placeholders to voice text
    """
    voice_text = text
    voice_text = voice_text.replace("{callerName}", caller_name)
    voice_text = voice_text.replace("{location}", location)
    voice_text = voice_text.replace("{callerLocation}", location)
    return voice_text

def update_conversation_arc(file_path):
    """
    Update a single conversation arc file
    """
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            data = json.load(f)

        # Skip if already has callerGender
        if 'callerGender' in data:
            print(f"[SKIP] {file_path} - Already updated")
            return False

        # Add callerGender based on personality or arc ID
        personality = data.get('callerPersonality', '')
        gender = get_caller_gender_from_personality(personality)

        # Special cases based on arc ID
        arc_id = data.get('arcId', '').lower()
        if 'investigator' in arc_id or 'biologist' in arc_id:
            gender = 'female'
        elif 'pilot' in arc_id or 'controller' in arc_id or 'contractor' in arc_id:
            gender = 'male'

        data['callerGender'] = gender

        # Add voiceText to Vern lines that have placeholders
        for dialogue_item in data.get('dialogue', []):
            if dialogue_item.get('speaker') == 'Vern':
                text_variants = dialogue_item.get('textVariants', [])
                for variant in text_variants:
                    text = variant.get('text', '')
                    if '{' in text:  # Has placeholders
                        variant['voiceText'] = add_voice_text(text)

        # Write back to file
        with open(file_path, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)

        print(f"[OK] {file_path} - Updated (gender: {gender})")
        return True

    except Exception as e:
        print(f"[ERROR] {file_path} - Error: {e}")
        return False

def main():
    """
    Update all conversation arc files
    """
    print("Updating Conversation Arcs: Adding Gender & Voice Text")
    print("=" * 60)

    # Find all conversation arc files
    arc_pattern = "../../assets/dialogue/arcs/**/*.json"
    arc_files = glob.glob(arc_pattern, recursive=True)

    print(f"Found {len(arc_files)} conversation arc files")

    updated_count = 0
    for file_path in sorted(arc_files):
        if update_conversation_arc(file_path):
            updated_count += 1

    print(f"\nComplete! Updated {updated_count} conversation arc files")
    print("\nSummary of changes:")
    print("- Added 'callerGender' field (male/female) to all arcs")
    print("- Added 'voiceText' field to Vern lines with placeholders")
    print("- Voice text replaces {callerName} -> 'caller', {location} -> 'out there'")

if __name__ == "__main__":
    main()