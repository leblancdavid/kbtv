#!/usr/bin/env python3
"""
ElevenLabs Voice Cloning Integration for Vern Tell
Upload reference audio and generate voice cloned audio for KBTV
"""

import os
import requests
import json
from pathlib import Path

class ElevenLabsVoiceCloner:
    def __init__(self, api_key=None):
        """
        Initialize ElevenLabs integration

        Args:
            api_key: Your ElevenLabs API key (get from https://elevenlabs.io/app/profile)
        """
        # Load API key from config file, or use provided key, or environment variable
        if api_key:
            self.api_key = api_key
        elif os.getenv('ELEVENLABS_API_KEY'):
            self.api_key = os.getenv('ELEVENLABS_API_KEY')
        else:
            # Try to load from config file
            config_path = os.path.join(os.path.dirname(__file__), 'elevenlabs_config.json')
            try:
                with open(config_path, 'r') as f:
                    config = json.load(f)
                    self.api_key = config.get('elevenlabs_api_key')
                if not self.api_key:
                    raise ValueError("No API key found in config file")
            except (FileNotFoundError, json.JSONDecodeError, KeyError):
                raise ValueError("Could not load API key from config file. Please create elevenlabs_config.json or set ELEVENLABS_API_KEY environment variable")
        self.base_url = "https://api.elevenlabs.io/v1"
        self.voice_id = None  # Will be set after uploading reference audio or loaded from file

        # Try to load existing voice ID from file
        voice_id_path = os.path.join(os.path.dirname(__file__), 'voice_id.txt')
        try:
            with open(voice_id_path, 'r') as f:
                self.voice_id = f.read().strip()
                print(f"Loaded voice ID from file: {self.voice_id}")
        except FileNotFoundError:
            print("No voice_id.txt file found. Voice ID will be set after uploading reference audio.")

        if not self.api_key:
            raise ValueError("API key required. Set ELEVENLABS_API_KEY environment variable or pass to constructor")

    def upload_voice_reference(self, audio_file_path, voice_name="Vern Tell - Art Bell Inspired"):
        """
        Upload reference audio to create a custom voice clone

        Args:
            audio_file_path: Path to reference audio file
            voice_name: Name for the cloned voice

        Returns:
            voice_id: ID of the created voice
        """
        if not os.path.exists(audio_file_path):
            raise FileNotFoundError(f"Reference audio file not found: {audio_file_path}")

        url = f"{self.base_url}/voices/add"

        # Prepare multipart form data
        files = {
            'files': (os.path.basename(audio_file_path), open(audio_file_path, 'rb'), 'audio/wav')
        }

        data = {
            'name': voice_name,
            'description': 'Art Bell-inspired voice for Vern Tell in KBTV radio game',
            'labels': json.dumps({
                'accent': 'american',
                'age': 'middle-aged',
                'gender': 'male',
                'style': 'radio-host'
            })
        }

        headers = {
            'xi-api-key': self.api_key
        }

        print(f"Uploading voice reference: {audio_file_path}")
        print(f"Voice name: {voice_name}")

        response = requests.post(url, files=files, data=data, headers=headers)

        if response.status_code == 200:
            result = response.json()
            self.voice_id = result['voice_id']
            print(f"Voice uploaded successfully! Voice ID: {self.voice_id}")
            return self.voice_id
        else:
            print(f"Upload failed: {response.status_code}")
            print(f"Response: {response.text}")
            return None

    def generate_audio(self, text, output_path=None, voice_id=None, model="eleven_flash_v2",
                      stability=0.5, similarity_boost=0.8, style=0.5):
        """
        Generate audio using the cloned voice or voice archetype

        Args:
            text: Text to convert to speech
            output_path: Where to save the audio file
            voice_id: Voice ID (for cloned voices) or voice archetype name
            model: TTS model to use
            stability: Voice stability (0-1)
            similarity_boost: Similarity to reference (0-1)
            style: Style exaggeration (0-1)

        Returns:
            output_path: Path to the generated audio file
        """
        # Male voice pool (13 voices) - diverse everyday male voices
        male_voice_pool = [
            "CwhRBWXzGAHq8TQ4Fs17",  # Roger - Laid-Back, Casual
            "IKne3meq5aSn9XLyUdCD",  # Charlie - Deep, Australian Male
            "JBFqnCBsd6RMkjVDRZzb",  # George - Warm Storyteller
            "N2lVS1w4EtoT3dr4eOWO",  # Callum - Husky Trickster
            "SAz9YHcvj6GT2YYXdXww",  # River - Relaxed, Neutral
            "bIHbv24MWmeRgasZH58o",  # Will - Relaxed Optimist
            "cjVigY5qzO86Huf0OWal",  # Eric - Smooth, Trustworthy
            "iP95p4xoKVk53GoZ742B",  # Chris - Charming, Down-to-Earth
            "nPczCjzI2devNBz1zQrb",  # Brian - Deep, Resonant
            "pNInz6obpgDQGcFmaJgB",  # Adam - Dominant, Firm
            "SOYHLrjzK2X1ezoPC6cr",  # Harry - Fierce Warrior
            "TX3LPaxmHKxFdv7VOQHJ",  # Liam - Energetic, Social
            "onwK4e9ZLuTAKqWW03F9",  # Daniel - Steady Broadcaster
        ]

        # Female voice pool (6 voices) - diverse everyday female voices
        female_voice_pool = [
            "FGY2WhTYpPnrIDTdsKH5",  # Laura - Enthusiast, Quirky
            "cgSgspJ2msm6clMCkdW9",  # Jessica - Playful, Bright
            "hpp4J3VqNfWAUOO0d1Us",  # Bella - Professional, Bright
            "pFZP5JQG7iQjIQuC4Bku",  # Lily - Velvety British Female
            "Xb7hH8MSUJpSbSDYk0k2",  # Alice - Clear, Engaging Educator
            "XrExE9yKIg1WjnnlVkGX",  # Matilda - Knowledgeable, Professional
        ]

        # Voice settings overrides for special effects
        # These modify the base voice to sound older or have regional accents
        voice_settings_override = {
            # Older-sounding male voices (lower stability = more robotic/elderly)
            "nPczCjzI2devNBz1zQrb": {"stability": 0.2, "style": 0.1},  # Brian as older male
            "JBFqnCBsd6RMkjVDRZzb": {"stability": 0.25, "style": 0.15},  # George as older storyteller
            "onwK4e9ZLuTAKqWW03F9": {"stability": 0.2, "style": 0.1},  # Daniel as older

            # Southern/country-sounding male voices (warmer, more relaxed)
            "CwhRBWXzGAHq8TQ4Fs17": {"stability": 0.6, "style": 0.3},  # Roger southern-fried
            "IKne3meq5aSn9XLyUdCD": {"stability": 0.5, "style": 0.4},  # Charlie country
            "bIHbv24MWmeRgasZH58o": {"stability": 0.55, "style": 0.35},  # Will southern
            "iP95p4xoKVk53GoZ742B": {"stability": 0.5, "style": 0.3},  # Chris southern
        }

        # Mapping for voice index selection (for cycling through pool)
        # Use arc_id hash to consistently select voice per arc
        def get_caller_voice_from_pool(arc_id, gender="male"):
            """Get a voice from the pool based on arc_id for consistency"""
            import hashlib
            # Create a hash from arc_id to get consistent voice per caller
            hash_val = int(hashlib.md5(arc_id.encode()).hexdigest(), 16)
            pool = male_voice_pool if gender == "male" else female_voice_pool
            return pool[hash_val % len(pool)]

        # Check if requesting a caller voice from pool
        if voice_id and voice_id.startswith("caller_pool_"):
            # Format: caller_pool_gender_hash (e.g., caller_pool_male_123)
            parts = voice_id.replace("caller_pool_", "").split("_")
            gender = parts[0] if len(parts) > 0 else "male"
            try:
                arc_hash = int(parts[1]) if len(parts) > 1 else 0
                voice_id = get_caller_voice_from_pool(f"arc_{arc_hash}", gender)
            except (ValueError, IndexError):
                voice_id = male_voice_pool[0]  # Default fallback

        # Legacy archetype mapping (kept for backwards compatibility)
        archetype_to_voice_id = {
            "default_male": "pNInz6obpgDRGcneiaZ6",   # Adam
            "default_female": "21m00Tcm4TlvDq8ikWAM",  # Rachel
            "gruff": "29vD33N1CtxCmqQRPOHJ",           # Drew (deeper)
            "nervous": "AZnzlk1XvdvUeBnXmlld",        # Dani
            "enthusiastic": "EXAVITQu4vr4xnSDxMaL",    # Bella
            "conspiracy": "ErXwobaYiN019PkySvjV",     # Antoni
            "elderly_male": "yoZ6DzaCZdzDDFgqxjZ",    # Fin
            "elderly_female": "cfV7xL7fn2s8LNcNjNWD"  # Eleanor
        }

        # Check if voice_id is an archetype (string in our mapping) vs cloned voice ID
        archetype_names = set(archetype_to_voice_id.keys())
        if voice_id and voice_id in archetype_names:  # It's an archetype
            voice_id = archetype_to_voice_id.get(voice_id, "21m00Tcm4TlvDq8ikWAM")  # Default to Rachel

        # Use stored voice ID if no voice_id provided
        voice_id = voice_id or self.voice_id
        if not voice_id:
            raise ValueError("No voice ID available. Upload reference audio first.")

        url = f"{self.base_url}/text-to-speech/{voice_id}"

        headers = {
            "Accept": "audio/mpeg",
            "Content-Type": "application/json",
            "xi-api-key": self.api_key
        }

        data = {
            "text": text,
            "model_id": model,
            "voice_settings": {
                "stability": 0.5,        # Voice stability (0-1)
                "similarity_boost": 0.8, # How similar to reference (0-1)
                "style": 0.5,           # Style exaggeration (0-1)
                "use_speaker_boost": True
            }
        }

        print(f"Generating audio for text: '{text[:50]}...'")

        response = requests.post(url, json=data, headers=headers)

        if response.status_code == 200:
            # Auto-generate output path if not provided
            if not output_path:
                output_path = f"vern_audio_{hash(text)}.mp3"

            # Save the audio file
            with open(output_path, 'wb') as f:
                f.write(response.content)

            print(f"Audio generated: {output_path}")
            return output_path
        else:
            print(f"Generation failed: {response.status_code}")
            print(f"Response: {response.text}")
            return None

    def test_basic_generation(self, test_text="Good evening, truth-seekers. You're tuned to KBTV, Beyond the Veil AM."):
        """
        Test basic voice generation with a pre-existing ElevenLabs voice
        """
        print("Testing basic voice generation...")
        # Use a pre-existing voice to test API functionality (free tier voice)
        output_path = self.generate_audio(test_text, "vern_voice_test.mp3", voice_id="EXAVITQu4vr4xnSDxMaL")  # Bella voice (free tier)
        if output_path:
            print(f"Listen to: {output_path}")
            print("This uses a pre-existing voice. Voice cloning requires a paid plan.")
        return output_path

    def test_voice_clone(self, test_text="Good evening, truth-seekers. You're tuned to KBTV, Beyond the Veil AM."):
        """
        Test the voice clone with a sample text (only if voice cloning is available)
        """
        if not self.voice_id:
            print("Voice cloning not available with current API key.")
            print("Testing basic generation instead...")
            return self.test_basic_generation(test_text)

        print("Testing voice clone...")
        output_path = self.generate_audio(test_text, "vern_voice_test.mp3")
        if output_path:
            print(f"Listen to: {output_path}")
            print("Does this sound like the Art Bell-inspired Vern Tell voice?")
        return output_path

def main():
    """
    Main function to set up and test ElevenLabs voice cloning
    """
    print("ElevenLabs Voice Cloning Setup for Vern Tell")
    print("=" * 55)

    # Initialize ElevenLabs client (API key is hardcoded in the class)
    try:
        cloner = ElevenLabsVoiceCloner()  # Will use hardcoded key
        print("ElevenLabs client initialized")
    except Exception as e:
        print(f"Initialization failed: {e}")
        return

    # Check if we already have a voice uploaded
    if not cloner.voice_id:
        print("\nStep 1: Upload Voice Reference")
        reference_file = "../../assets/audio/voice_references/vern_reference_001_final.wav"

        # Use the winning optimized reference (test_1)
        optimized_reference = "../../assets/audio/voice_references/vern_reference_bright_01.wav"

        if os.path.exists(optimized_reference):
            voice_id = cloner.upload_voice_reference(optimized_reference, "Vern Tell - Final Optimized")
            if voice_id:
                # Save voice ID for future use
                voice_id_path = os.path.join(os.path.dirname(__file__), 'voice_id.txt')
                with open(voice_id_path, 'w') as f:
                    f.write(voice_id)
                print(f"Optimized voice uploaded! Voice ID: {voice_id}")
                print("Testing final optimized cloned voice...")
                test_file = cloner.test_voice_clone()
            else:
                print("Voice upload failed")
        else:
            print(f"Optimized reference file not found: {optimized_reference}")
            print("Make sure you have the optimized reference audio ready")
            return

    # Test voice generation (try cloning first, fallback to basic)
    print("\nStep 2: Test Voice Generation")
    if cloner.voice_id:
        print("Voice clone available, testing cloned voice...")
        test_file = cloner.test_voice_clone()
    else:
        print("Attempting voice cloning...")
        # Try to upload reference again with new permissions
        reference_file = "../../assets/audio/voice_references/vern_reference_001_final.wav"
        if os.path.exists(reference_file):
            voice_id = cloner.upload_voice_reference(reference_file)
            if voice_id:
                # Save voice ID for future use
                voice_id_path = os.path.join(os.path.dirname(__file__), 'voice_id.txt')
                with open(voice_id_path, 'w') as f:
                    f.write(voice_id)
                print(f"Voice uploaded and saved! Voice ID: {voice_id}")
                print(f"Testing cloned voice...")
                test_file = cloner.test_voice_clone()
            else:
                print("Voice cloning failed, falling back to basic generation...")
                test_file = cloner.test_basic_generation()
        else:
            print("Reference file not found, using basic generation...")
            test_file = cloner.test_basic_generation()

    if test_file:
        print("\nSetup Complete!")
        print("Listen to the test file to verify API functionality")
        print("Note: Voice cloning requires a paid ElevenLabs plan")
        print("For now, we can use pre-existing voices or upgrade to enable cloning")
    else:
        print("\nTest failed")

if __name__ == "__main__":
    main()