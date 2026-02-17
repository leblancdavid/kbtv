#!/usr/bin/env python3
"""
Test script to generate audio samples for all 20 caller voices
"""

import os
import sys
sys.path.insert(0, os.path.dirname(__file__))

from elevenlabs_setup import ElevenLabsVoiceCloner

# Sample caller line to test
SAMPLE_TEXT = "Uh, yeah, so I've been driving trucks for twenty-two years, Vern."

# Updated 20 voice pool with correct IDs
CALLER_VOICE_POOL = [
    ("CwhRBWXzGAHq8TQ4Fs17", "Roger - Laid-Back, Casual"),
    ("FGY2WhTYpPnrIDTdsKH5", "Laura - Enthusiast, Quirky"),
    ("IKne3meq5aSn9XLyUdCD", "Charlie - Deep, Australian Male"),
    ("JBFqnCBsd6RMkjVDRZzb", "George - Warm Storyteller"),
    ("N2lVS1w4EtoT3dr4eOWO", "Callum - Husky Trickster"),
    ("SAz9YHcvj6GT2YYXdXww", "River - Relaxed, Neutral"),
    ("bIHbv24MWmeRgasZH58o", "Will - Relaxed Optimist"),
    ("cgSgspJ2msm6clMCkdW9", "Jessica - Playful, Bright"),
    ("cjVigY5qzO86Huf0OWal", "Eric - Smooth, Trustworthy"),
    ("hpp4J3VqNfWAUOO0d1Us", "Bella - Professional, Bright"),
    ("iP95p4xoKVk53GoZ742B", "Chris - Charming, Down-to-Earth"),
    ("nPczCjzI2devNBz1zQrb", "Brian - Deep, Resonant"),
    ("pFZP5JQG7iQjIQuC4Bku", "Lily - Velvety British Female"),
    ("pNInz6obpgDQGcFmaJgB", "Adam - Dominant, Firm"),
    ("pqHfZKP75CvOlQylNhV4", "Bill - Wise, Mature"),
    ("SOYHLrjzK2X1ezoPC6cr", "Harry - Fierce Warrior"),
    ("TX3LPaxmHKxFdv7VOQHJ", "Liam - Energetic, Social"),
    ("Xb7hH8MSUJpSbSDYk0k2", "Alice - Clear, Engaging Educator"),
    ("XrExE9yKIg1WjnnlVkGX", "Matilda - Knowledgeable, Professional"),
    ("onwK4e9ZLuTAKqWW03F9", "Daniel - Steady Broadcaster"),
]

def test_all_voices():
    """Generate sample audio for all 20 caller voices"""
    print("Testing all 20 caller voices...")
    print(f"Sample text: {SAMPLE_TEXT}")
    print("=" * 60)
    
    # Initialize ElevenLabs
    try:
        cloner = ElevenLabsVoiceCloner()
        print(f"Using Vern's voice ID: {cloner.voice_id}")
    except Exception as e:
        print(f"Error initializing ElevenLabs: {e}")
        return
    
    # Create output directory
    output_dir = os.path.join("..", "..", "assets", "audio", "voice", "Testing", "voice_samples")
    os.makedirs(output_dir, exist_ok=True)
    
    success_count = 0
    fail_count = 0
    
    for voice_id, description in CALLER_VOICE_POOL:
        print(f"\nTesting: {description}")
        print(f"Voice ID: {voice_id}")
        
        # Generate filename from description
        filename = description.split(" - ")[0].lower().replace(" ", "_")
        output_path = os.path.join(output_dir, f"sample_{filename}.mp3")
        
        try:
            result = cloner.generate_audio(
                text=SAMPLE_TEXT,
                output_path=output_path,
                voice_id=voice_id,
                stability=0.5,
                similarity_boost=0.8,
                style=0.5
            )
            
            if result and os.path.exists(output_path):
                print(f"[OK] SUCCESS: {output_path}")
                success_count += 1
            else:
                print(f"[X] FAILED: No output file created")
                fail_count += 1
                
        except Exception as e:
            print(f"[X] ERROR: {e}")
            fail_count += 1
        
        # Rate limiting
        import time
        time.sleep(2)
    
    print("\n" + "=" * 60)
    print(f"RESULTS: {success_count} succeeded, {fail_count} failed")
    print(f"Output directory: {output_dir}")
    
    return success_count, fail_count

if __name__ == "__main__":
    test_all_voices()
