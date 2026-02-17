#!/usr/bin/env python3
"""
Test voice settings to make voices sound older or have regional accents
"""

import os
import sys
sys.path.insert(0, os.path.dirname(__file__))

from elevenlabs_setup import ElevenLabsVoiceCloner

SAMPLE_TEXT = "Uh, yeah, so I've been driving trucks for twenty-two years, Vern."

def test_voice_modifications():
    print("Testing voice modifications to create older/southern sounds...")
    print("=" * 60)
    
    cloner = ElevenLabsVoiceCloner()
    
    output_dir = os.path.join("..", "..", "assets", "audio", "voice", "Testing", "voice_mods")
    os.makedirs(output_dir, exist_ok=True)
    
    # Test 1: Make a voice sound OLDER
    # Lower stability = more robotic/older sound
    # Lower style = less expressive
    # We can also try slower speech by adjusting model settings
    
    older_settings = [
        {"name": "brian_deep_old", "voice_id": "nPczCjzI2devNBz1zQrb", "stability": 0.2, "style": 0.1, "description": "Brian with OLDER settings (low stability)"},
        {"name": "brian_old_slow", "voice_id": "nPczCjzI2devNBz1zQrb", "stability": 0.3, "style": 0.2, "description": "Brian with elderly settings"},
        {"name": "bill_mature", "voice_id": "pqHfZKP75CvOlQylNhV4", "stability": 0.25, "style": 0.15, "description": "Bill as older male"},
        {"name": "daniel_old", "voice_id": "onwK4e9ZLuTAKqWW03F9", "stability": 0.2, "style": 0.1, "description": "Daniel with elderly settings"},
        {"name": "george_older", "voice_id": "JBFqnCBsd6RMkjVDRZzb", "stability": 0.25, "style": 0.15, "description": "George as older storyteller"},
    ]
    
    # Test 2: Try to create a southern-like sound
    # Using warmer, more relaxed settings
    southern_settings = [
        {"name": "roger_southern", "voice_id": "CwhRBWXzGAHq8TQ4Fs17", "stability": 0.6, "style": 0.3, "description": "Roger with southern-fried settings"},
        {"name": "charlie_country", "voice_id": "IKne3meq5aSn9XLyUdCD", "stability": 0.5, "style": 0.4, "description": "Charlie with country settings"},
        {"name": "will_southern", "voice_id": "bIHbv24MWmeRgasZH58o", "stability": 0.55, "style": 0.35, "description": "Will with southern settings"},
        {"name": "chris_southern", "voice_id": "iP95p4xoKVk53GoZ742B", "stability": 0.5, "style": 0.3, "description": "Chris with southern settings"},
    ]
    
    all_tests = older_settings + southern_settings
    
    for test in all_tests:
        print(f"\nTesting: {test['description']}")
        
        output_path = os.path.join(output_dir, f"{test['name']}.mp3")
        
        try:
            result = cloner.generate_audio(
                text=SAMPLE_TEXT,
                output_path=output_path,
                voice_id=test['voice_id'],
                stability=test['stability'],
                similarity_boost=0.8,
                style=test['style']
            )
            
            if result and os.path.exists(output_path):
                print(f"  [OK] Generated: {test['name']}.mp3")
            else:
                print(f"  [X] Failed")
                
        except Exception as e:
            print(f"  [X] Error: {e}")
        
        import time
        time.sleep(2)
    
    print("\n" + "=" * 60)
    print("Check the output folder and listen to these samples:")
    print(output_dir)

if __name__ == "__main__":
    test_voice_modifications()
