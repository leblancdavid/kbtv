#!/usr/bin/env python3
"""
Script to expand conversation arc JSON files from 7 to 12 mood variants.
Adds 5 new moods: exhausted, depressed, angry, frustrated, obsessive, manic.
"""

import json
import os
import glob

# New moods to add
NEW_MOODS = ['exhausted', 'depressed', 'angry', 'frustrated', 'obsessive', 'manic']

# Template modifications for each new mood
MOOD_TEMPLATES = {
    'exhausted': {
        'text_suffix': " *yawn*",
        'voice_suffix': " *yawn*",
        'personality': 'drained, minimal effort'
    },
    'depressed': {
        'text_suffix': "",
        'voice_suffix': "",
        'personality': 'hopeless, disinterested'
    },
    'angry': {
        'text_suffix': "!",
        'voice_suffix': "!",
        'personality': 'hostile, confrontational'
    },
    'frustrated': {
        'text_suffix': " Ugh,",
        'voice_suffix': " Ugh,",
        'personality': 'short-tempered, impatient'
    },
    'obsessive': {
        'text_suffix': " You know what this means?",
        'voice_suffix': " You know what this means?",
        'personality': 'hyper-focused, conspiracy-prone'
    },
    'manic': {
        'text_suffix': "!!!",
        'voice_suffix': "!!!",
        'personality': 'over-excited, erratic'
    }
}

def expand_vern_lines(vern_lines, arc_id):
    """Expand Vern lines from 7 to 12 mood variants."""
    if len(vern_lines) != 7:
        print(f"Warning: Expected 7 mood variants, got {len(vern_lines)}")
        return vern_lines

    # Map existing moods to their indices
    existing_moods = [line['mood'] for line in vern_lines]

    # Find neutral variant as base for new moods
    neutral_idx = existing_moods.index('neutral') if 'neutral' in existing_moods else 0
    base_line = vern_lines[neutral_idx]

    expanded_lines = list(vern_lines)  # Copy existing

    for mood in NEW_MOODS:
        new_line = base_line.copy()

        # Update ID
        new_line['id'] = new_line['id'].replace('_neutral_', f'_{mood}_')

        # Update mood
        new_line['mood'] = mood

        # Modify text based on mood template
        template = MOOD_TEMPLATES[mood]
        if mood == 'exhausted':
            new_line['text'] = f"*yawn* {base_line['text'].lower()}"
            new_line['voiceText'] = f"*yawn* {base_line['voiceText'].lower()}"
        elif mood == 'depressed':
            new_line['text'] = base_line['text'].replace('?', '...').replace('!', '.')
            new_line['voiceText'] = base_line['voiceText'].replace('?', '...').replace('!', '.')
        elif mood == 'angry':
            new_line['text'] = base_line['text'].upper().replace('?', '?!').replace('.', '!')
            new_line['voiceText'] = base_line['voiceText'].upper().replace('?', '?!').replace('.', '!')
        elif mood == 'frustrated':
            new_line['text'] = f"Ugh, {base_line['text'].lower()}"
            new_line['voiceText'] = f"Ugh, {base_line['voiceText'].lower()}"
        elif mood == 'obsessive':
            new_line['text'] = f"{base_line['text']} You know what this means?"
            new_line['voiceText'] = f"{base_line['voiceText']} You know what this means?"
        elif mood == 'manic':
            new_line['text'] = f"{base_line['text'].upper()}!!!"
            new_line['voiceText'] = f"{base_line['voiceText'].upper()}!!!"

        expanded_lines.append(new_line)

    return expanded_lines

def process_arc_file(filepath):
    """Process a single arc JSON file."""
    print(f"Processing {filepath}")

    with open(filepath, 'r', encoding='utf-8') as f:
        data = json.load(f)

    modified = False

    for arc_line in data.get('arcLines', []):
        if arc_line.get('speaker') == 'vern':
            current_lines = arc_line.get('lines', [])
            if len(current_lines) == 7:  # Only expand if exactly 7 variants
                arc_line['lines'] = expand_vern_lines(current_lines, data.get('arcId', 'unknown'))
                modified = True

    if modified:
        # Write back to file
        with open(filepath, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
        print(f"  [EXPANDED] {filepath}")
    else:
        print(f"  [SKIPPED] No changes needed for {filepath}")

def main():
    """Main function."""
    # Find all arc JSON files
    arc_files = glob.glob('assets/dialogue/arcs/**/*.json')

    print(f"Found {len(arc_files)} arc files to process")

    for filepath in arc_files:
        try:
            process_arc_file(filepath)
        except Exception as e:
            print(f"Error processing {filepath}: {e}")

    print("Done!")

if __name__ == '__main__':
    main()