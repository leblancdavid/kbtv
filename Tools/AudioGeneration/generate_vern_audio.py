#!/usr/bin/env python3
"""
Generate all Vern broadcast audio from JSON dialog files.
Generates audio for: openings, closings, dead-air-fillers, break-transitions,
return-from-breaks, dropped-callers, off-topic-remarks, caller-cursed, between-callers
"""

import os
import json
import time
from elevenlabs_setup import ElevenLabsVoiceCloner

# Mood to voice settings mapping
MOOD_SETTINGS = {
    'neutral': {'stability': 0.5, 'similarity_boost': 0.8, 'style': 0.5},
    'tired': {'stability': 0.3, 'similarity_boost': 0.7, 'style': 0.3},
    'energized': {'stability': 0.7, 'similarity_boost': 0.9, 'style': 0.8},
    'irritated': {'stability': 0.6, 'similarity_boost': 0.8, 'style': 0.4},
    'amused': {'stability': 0.6, 'similarity_boost': 0.8, 'style': 0.7},
    'focused': {'stability': 0.8, 'similarity_boost': 0.8, 'style': 0.3},
    'gruff': {'stability': 0.7, 'similarity_boost': 0.7, 'style': 0.2},
    'exhausted': {'stability': 0.25, 'similarity_boost': 0.65, 'style': 0.25},
    'depressed': {'stability': 0.35, 'similarity_boost': 0.7, 'style': 0.2},
    'angry': {'stability': 0.65, 'similarity_boost': 0.85, 'style': 0.6},
    'frustrated': {'stability': 0.4, 'similarity_boost': 0.75, 'style': 0.35},
    'obsessive': {'stability': 0.75, 'similarity_boost': 0.85, 'style': 0.5},
    'manic': {'stability': 0.45, 'similarity_boost': 0.9, 'style': 0.9}
}

# Map JSON line types to output folder/file prefix
LINE_TYPE_MAPPING = {
    'openings': 'opening',
    'closings': 'closing',
    'dead-air-fillers': 'deadair',
    'break-transitions': 'break',
    'return-from-breaks': 'return',
    'dropped-callers': 'dropped',
    'off-topic-remarks': 'offtopic',
    'caller-cursed': 'cursed',
    'between-callers': 'betweencallers'
}

def get_output_filename(line_type, line_id, mood):
    """Generate output filename based on line type and mood"""
    prefix = LINE_TYPE_MAPPING.get(line_type, line_type)
    # Extract number from line_id (e.g., opening_ufos_1 -> 1)
    parts = line_id.split('_')
    num = parts[-1] if parts else '1'
    return f"{prefix}_{mood}_{num}.mp3"

def generate_vern_audio(force_regenerate=False, verbose=False):
    """Generate all Vern broadcast audio"""
    
    print("Generating Vern broadcast audio...")
    print("=" * 50)
    
    # Initialize ElevenLabs
    cloner = ElevenLabsVoiceCloner()
    if not cloner.voice_id:
        print("ERROR: No Vern voice ID available. Run elevenlabs_setup.py first.")
        return
    
    # Base directory for Vern audio
    vern_dir = os.path.join("..", "..", "assets", "dialogue", "vern")
    output_dir = os.path.join("..", "..", "assets", "audio", "voice", "Vern", "Broadcast")
    os.makedirs(output_dir, exist_ok=True)
    
    total_generated = 0
    total_skipped = 0
    
    # Process each JSON file
    for filename in os.listdir(vern_dir):
        if not filename.endswith('.json'):
            continue
            
        filepath = os.path.join(vern_dir, filename)
        
        with open(filepath, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        line_type = data.get('lineType', filename.replace('.json', ''))
        lines = data.get('lines', [])
        
        print(f"\nProcessing {filename}: {len(lines)} lines")
        
        # Group lines by mood for counting
        mood_counts = {}
        
        for line in lines:
            line_id = line.get('id', '')
            text = line.get('voiceText', line.get('text', ''))
            mood = line.get('mood', 'neutral')
            
            if not text:
                print(f"  SKIPPING: {line_id} - no text")
                continue
            
            # Track mood counts
            mood_counts[mood] = mood_counts.get(mood, 0) + 1
            
            # Generate output filename
            output_filename = get_output_filename(line_type, line_id, mood)
            output_path = os.path.join(output_dir, output_filename)
            
            # Check if file exists
            if os.path.exists(output_path) and not force_regenerate:
                if verbose:
                    print(f"  SKIPPING: {output_filename} (exists)")
                total_skipped += 1
                continue
            
            # Get voice settings for mood
            voice_settings = MOOD_SETTINGS.get(mood, MOOD_SETTINGS['neutral'])
            
            try:
                result_path = cloner.generate_audio(
                    text=text,
                    output_path=output_path,
                    voice_id=cloner.voice_id,
                    stability=voice_settings['stability'],
                    similarity_boost=voice_settings['similarity_boost'],
                    style=voice_settings['style']
                )
                print(f"  GENERATED: {output_filename}")
                total_generated += 1
                
            except Exception as e:
                print(f"  ERROR: {line_id}: {e}")
            
            # Rate limiting
            time.sleep(1.5)
        
        print(f"  Moods: {mood_counts}")
    
    print(f"\n{'=' * 50}")
    print(f"Completed: {total_generated} generated, {total_skipped} skipped")
    print(f"Audio saved to: {output_dir}")

if __name__ == "__main__":
    import argparse
    
    parser = argparse.ArgumentParser(description='Generate all Vern broadcast audio')
    parser.add_argument('--force', action='store_true', help='Regenerate existing files')
    parser.add_argument('--verbose', action='store_true', help='Verbose output')
    
    args = parser.parse_args()
    
    generate_vern_audio(args.force, args.verbose)
