#!/usr/bin/env python3
"""
Tortoise TTS Voice Cloning for Vern Tell
High-quality voice cloning using Tortoise TTS with Art Bell reference
"""

import os
import torch
import torchaudio
import tortoise
from tortoise.api import TextToSpeech
from tortoise.utils.audio import load_audio, load_voice

def test_tortoise_voice_cloning():
    """
    Test voice cloning with Tortoise TTS using Art Bell reference audio
    """
    try:
        print("Initializing Tortoise TTS for voice cloning...")

        # Initialize TextToSpeech
        tts = TextToSpeech(use_deepspeed=False, kv_cache=True, half=False)

        # Load reference audio
        reference_audio_path = "../../assets/audio/voice_references/vern_reference_001_final.wav"

        if not os.path.exists(reference_audio_path):
            print(f"Reference audio not found: {reference_audio_path}")
            return False

        print("Generating voice test with voice cloning...")

        # Test text
        test_text = "Good evening, truth-seekers. You're tuned to KBTV, Beyond the Veil AM."

        # Generate with voice cloning using tts.tts_with_preset
        output_path = "../../assets/audio/voice_references/vern_tortoise_test.wav"

        # Load voice samples
        voice_samples, conditioning_latents = load_voice(reference_audio_path)

        # Use voice cloning with custom voice
        gen = tts.tts_with_preset(
            text=test_text,
            voice_samples=voice_samples,
            conditioning_latents=conditioning_latents,
            preset="fast"
        )

        torchaudio.save(output_path, gen.squeeze(0).cpu(), 24000)

        print(f"Voice test generated: {output_path}")
        print("Listen to the test file to evaluate voice cloning quality")
        print("Tortoise TTS should provide much better voice cloning than Tacotron2")

        return True

    except Exception as e:
        print(f"Error during Tortoise voice cloning: {e}")
        import traceback
        traceback.print_exc()
        return False

        print(f"Loading reference audio: {reference_audio_path}")

        print("Generating voice test with voice cloning...")

        # Test text
        test_text = "Good evening, truth-seekers. You're tuned to KBTV, Beyond the Veil AM."

        # Generate with voice cloning using tortoise.do_tts
        output_path = "../../assets/audio/voice_references/vern_tortoise_test.wav"

        # Use voice cloning - provide custom voice samples
        gen = tortoise.do_tts(
            text=test_text,
            voice_samples=[reference_audio_path],  # Pass file path
            preset="fast"
        )

        # Save the generated audio
        torchaudio.save(output_path, gen.squeeze(0).cpu(), 24000)

        print(f"Voice test generated: {output_path}")
        print("Listen to the test file to evaluate voice cloning quality")
        print("Tortoise TTS should provide much better voice cloning than Tacotron2")

        return True

    except Exception as e:
        print(f"Error during Tortoise voice cloning: {e}")
        import traceback
        traceback.print_exc()
        return False

def test_tortoise_basic():
    """
    Test basic Tortoise TTS without voice cloning
    """
    try:
        print("Testing basic Tortoise TTS...")

        # Initialize TextToSpeech
        tts = TextToSpeech(use_deepspeed=False, kv_cache=True, half=False)  # Disable half precision for compatibility

        test_text = "Hello, this is a test of Tortoise TTS voice synthesis."
        output_path = "../../assets/audio/voice_references/tortoise_basic_test.wav"

        # Use tts.tts_with_preset for basic generation
        gen = tts.tts_with_preset(
            text=test_text,
            preset="fast"
        )

        torchaudio.save(output_path, gen.squeeze(0).cpu(), 24000)

        print(f"Basic test generated: {output_path}")
        return True

    except Exception as e:
        print(f"Error during basic Tortoise test: {e}")
        import traceback
        traceback.print_exc()
        return False

if __name__ == "__main__":
    print("Vern Tell - Tortoise TTS Voice Cloning Test")
    print("=" * 50)

    # Test basic functionality first
    print("Testing basic Tortoise TTS...")
    if test_tortoise_basic():
        print("\nBasic Tortoise TTS working")

        # Now test voice cloning
        print("\nTesting voice cloning...")
        if test_tortoise_voice_cloning():
            print("\nVoice cloning test completed!")
            print("Listen to the generated files to compare quality.")
        else:
            print("\nVoice cloning failed")
    else:
        print("\nBasic Tortoise TTS failed")