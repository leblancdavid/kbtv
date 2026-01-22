#!/usr/bin/env python3
"""
XTTS Voice Cloning Setup for Vern Tell
Test script for voice cloning with Art Bell-inspired reference audio
"""

import os
import sys
from pathlib import Path

def setup_xtts_voice_cloning():
    """
    Set up XTTS voice cloning for Vern Tell using Art Bell reference audio
    """
    try:
        from TTS.api import TTS
        print("TTS library available")

        # Check for reference audio (multiple samples for better quality)
        reference_paths = [
            "../../assets/audio/voice_references/vern_reference_001_final.wav",
            "../../assets/audio/voice_references/vern_reference_002.wav",
            "../../assets/audio/voice_references/vern_reference_003.wav",
            "../../assets/audio/voice_references/vern_reference_004.wav",
            "../../assets/audio/voice_references/vern_reference_005.wav"
        ]

        reference_audio = None
        for path in reference_paths:
            if os.path.exists(path):
                reference_audio = path
                print(f"Found reference audio: {path}")
                break

        if not reference_audio:
            print("No reference audio found. Please create vern_reference_001.wav first.")
            print("See docs/audio/VOICE_REFERENCE_GUIDE.md for instructions.")
            return False

        # Initialize TTS model (try Tacotron2 first as fallback)
        print("Loading TTS model...")
        # Set environment variable to auto-accept license for non-commercial use
        os.environ["COQUI_TOS_AGREED"] = "1"

        try:
            # Try XTTS v2 first
            tts = TTS("tts_models/multilingual/multi-dataset/xtts_v2", gpu=False)
            print("XTTS v2 model loaded")
        except Exception as e:
            print(f"XTTS failed ({e}), trying Tacotron2...")
            # Fallback to simpler model
            tts = TTS("tts_models/en/ljspeech/tacotron2-DDC_ph", gpu=False)
            print("Tacotron2 model loaded (fallback)")

        # Test voice cloning with reference samples
        test_text = "Good evening, truth-seekers. You're tuned to KBTV, Beyond the Veil AM. I'm your host, Vern Tell, and we've got a big show for you tonight."

        output_path = "../../assets/audio/voice_references/vern_voice_test.wav"

        # Check if model supports voice cloning and multiple languages
        if "xtts" in str(type(tts)).lower():
            print("Generating voice test with 5 reference samples (XTTS)...")
            # Use all 5 reference samples for XTTS
            reference_samples = [
                "../../assets/audio/voice_references/vern_reference_001_final.wav",
                "../../assets/audio/voice_references/vern_reference_002.wav",
                "../../assets/audio/voice_references/vern_reference_003.wav",
                "../../assets/audio/voice_references/vern_reference_004.wav",
                "../../assets/audio/voice_references/vern_reference_005.wav"
            ]

            tts.tts_to_file(
                text=test_text,
                file_path=output_path,
                speaker_wav=reference_samples,
                language="en"
            )
        else:
            print("Generating voice test with single reference sample (Tacotron2)...")
            # Tacotron2 doesn't support voice cloning, just test basic TTS
            tts.tts_to_file(
                text=test_text,
                file_path=output_path
            )

        print(f"Voice test generated: {output_path}")
        print("Listen to the test file to evaluate voice quality")
        print("If it sounds good, proceed with full audio generation")

        return True

    except ImportError as e:
        print(f"Missing dependency: {e}")
        print("Install with: pip install TTS torch torchaudio")
        return False
    except Exception as e:
        print(f"Error during voice cloning setup: {e}")
        return False

def test_pretrained_voice():
    """
    Test with pre-trained XTTS voices as fallback
    """
    try:
        from TTS.api import TTS

        print("Testing pre-trained XTTS voices...")

        # List available speakers (this might take time)
        tts = TTS("tts_models/multilingual/multi-dataset/xtts_v2")
        speakers = tts.speakers
        print(f"Available speakers: {len(speakers)} found")

        # Test a few promising speakers
        test_speakers = ["Claribel Dervox", "Daisy Studious", "Anna Nagle"]
        test_text = "This is a test of the voice synthesis system."

        for speaker in test_speakers:
            if speaker in speakers:
                output_path = f"assets/audio/voice_references/test_{speaker.lower().replace(' ', '_')}.wav"
                print(f"Testing {speaker}...")
                tts.tts_to_file(
                    text=test_text,
                    file_path=output_path,
                    speaker=speaker,
                    language="en"
                )
                print(f"Generated: {output_path}")

        print("Listen to the test files and choose the best match for Vern")
        return True

    except Exception as e:
        print(f"Error testing pre-trained voices: {e}")
        return False

if __name__ == "__main__":
    print("Vern Tell Voice Setup")
    print("=" * 40)

    if len(sys.argv) > 1 and sys.argv[1] == "--pretrained":
        print("Testing pre-trained voices...")
        test_pretrained_voice()
    else:
        print("Setting up voice cloning with reference audio...")
        success = setup_xtts_voice_cloning()

        if not success:
            print("\nAlternative: Test pre-trained voices")
            print("Run: python voice_setup.py --pretrained")