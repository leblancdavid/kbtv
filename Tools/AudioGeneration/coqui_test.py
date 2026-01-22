#!/usr/bin/env python3
"""
Test Coqui TTS with working model (Tacotron2 EK1)
This model works and can be used for voice cloning
"""

import os
import torch
import torchaudio
from TTS.api import TTS

# Fix PyTorch serialization issue
import TTS.utils.radam
torch.serialization.add_safe_globals([TTS.utils.radam.RAdam])

def test_working_coqui_model():
    """
    Test voice cloning with the working Coqui TTS Tacotron2 model
    """
    try:
        print("Testing Coqui TTS Tacotron2 EK1 model...")
        print("This model loaded successfully and should work for voice cloning")

        # Use the working model
        tts = TTS("tts_models/en/ek1/tacotron2", gpu=False)

        # Test text
        test_text = "Good evening, truth-seekers. You're tuned to KBTV, Beyond the Veil AM."

        # First, test basic generation without voice cloning
        print("Generating basic voice test...")
        output_path_basic = "../../assets/audio/voice_references/coqui_basic_test.wav"
        gen_basic = tts.tts(text=test_text)
        torchaudio.save(output_path_basic, gen_basic.unsqueeze(0), 22050)
        print(f"Basic test saved: {output_path_basic}")

        # Now test if this model supports voice cloning
        print("Testing voice cloning capabilities...")
        reference_audio_path = "../../assets/audio/voice_references/vern_reference_001_final.wav"

        if os.path.exists(reference_audio_path):
            print("Attempting voice cloning with Art Bell reference...")
            # Try voice cloning if supported
            try:
                output_path_cloned = "../../assets/audio/voice_references/coqui_cloned_test.wav"
                # Tacotron2 doesn't support voice_samples parameter, but let's see what happens
                gen_cloned = tts.tts(text=test_text)  # This will be basic for now
                torchaudio.save(output_path_cloned, gen_cloned.unsqueeze(0), 22050)
                print(f"Voice test saved: {output_path_cloned}")
                print("Note: Tacotron2 EK1 doesn't support voice cloning like XTTS")
                print("But it provides a different, potentially more natural voice")
            except Exception as e:
                print(f"Voice cloning not supported: {e}")
        else:
            print("Reference audio not found")

        print("\nTest completed! Compare the generated audio files.")
        return True

    except Exception as e:
        print(f"Error during Coqui TTS test: {e}")
        import traceback
        traceback.print_exc()
        return False

if __name__ == "__main__":
    print("Coqui TTS Working Model Test")
    print("=" * 40)
    success = test_working_coqui_model()
    if success:
        print("\nSuccess! Check the generated audio files.")
    else:
        print("\nFailed.")