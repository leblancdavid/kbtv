#!/usr/bin/env python3
"""
Test different optimized reference audio samples with ElevenLabs
Find the best one for Vern Tell's voice cloning
"""

import os
import json
from elevenlabs_setup import ElevenLabsVoiceCloner

def test_optimized_references():
    """
    Test all optimized reference samples to find the best one
    """
    print("Testing Optimized Reference Samples for Vern Tell")
    print("=" * 60)

    # Load API key
    with open('elevenlabs_config.json', 'r') as f:
        config = json.load(f)
        api_key = config.get('elevenlabs_api_key')

    if not api_key:
        print("No API key found")
        return

    # Initialize cloner
    cloner = ElevenLabsVoiceCloner(api_key)
    print("ElevenLabs client initialized")

    # List of optimized reference samples to test
    reference_files = [
        "../../assets/audio/voice_references/vern_reference_bright_01.wav",
        "../../assets/audio/voice_references/vern_reference_bright_02.wav",
        "../../assets/audio/voice_references/vern_reference_bright_03.wav",
        "../../assets/audio/voice_references/vern_reference_bright_04.wav"
    ]

    test_text = "Good evening, truth-seekers. You're tuned to KBTV, Beyond the Veil AM. I'm your host, Vern Tell."

    print(f"\nTesting with text: '{test_text[:60]}...'\n")

    # Test each reference sample
    for i, ref_file in enumerate(reference_files, 1):
        if not os.path.exists(ref_file):
            print(f"Reference file {ref_file} not found, skipping...")
            continue

        print(f"\nTesting Reference {i}/4: {os.path.basename(ref_file)}")

        # Upload voice reference
        voice_id = cloner.upload_voice_reference(ref_file, f"Vern Tell - Optimized {i}")
        if voice_id:
            # Generate test audio
            output_file = f"vern_optimized_test_{i}.mp3"
            result = cloner.generate_audio(test_text, output_file)

            if result:
                print(f"Generated: {output_file}")
                print("Listen to compare brightness and clarity")
            else:
                print("Generation failed")
        else:
            print("Voice upload failed")

        print("-" * 40)

    print("\nTest Complete!")
    print("Compare the generated files:")
    print("- vern_optimized_test_1.mp3 (gentle optimization)")
    print("- vern_optimized_test_2.mp3 (moderate optimization)")
    print("- vern_optimized_test_3.mp3 (aggressive optimization)")
    print("- vern_optimized_test_4.mp3 (balanced optimization)")
    print("\nChoose the one with the best balance of brightness and natural sound!")

if __name__ == "__main__":
    test_optimized_references()