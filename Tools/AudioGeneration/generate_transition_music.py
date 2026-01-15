#!/usr/bin/env python3
"""
Generate break transition music for KBTV.

Creates 5 variations of 20-second ambient synth music clips that play
when the player queues an ad break, cueing Vern that a break is coming.

Usage:
    python generate_transition_music.py              # Generate all 5 variations
    python generate_transition_music.py --variation 2  # Generate only variation 2
    python generate_transition_music.py --duration 20  # Custom duration (default: 20s)
    python generate_transition_music.py --check-deps   # Check dependencies

Requirements:
    pip install numpy scipy pydub
    ffmpeg must be in PATH for OGG export
"""

import argparse
import os
import sys
from pathlib import Path
from typing import Tuple, Optional

def check_dependencies():
    """Check if required packages are installed."""
    missing = []
    
    try:
        import numpy
    except ImportError:
        missing.append("numpy")
    
    try:
        import scipy
    except ImportError:
        missing.append("scipy")
    
    try:
        from pydub import AudioSegment
    except ImportError:
        missing.append("pydub")
    
    if missing:
        print(f"Missing dependencies: {', '.join(missing)}")
        print(f"Install with: pip install {' '.join(missing)}")
        sys.exit(1)
    
    return True


# Variation definitions
VARIATIONS = {
    1: {
        "name": "Mysterious Synth",
        "base_freq": 110.0,   # A2
        "harmonics": [2.0, 3.0, 4.0],  # Octave, 5th, 2 octaves
        "harmonic_levels": [0.3, 0.15, 0.08],
        "lfo_rate": 0.3,      # Slow vibrato
        "lfo_depth": 0.02,
        "filter_cutoff": 2000,
        "character": "minor",  # Eerie overtone
    },
    2: {
        "name": "Warm Ambient",
        "base_freq": 130.81,  # C3
        "harmonics": [2.0, 2.5, 3.0],  # Octave, major 3rd, 5th
        "harmonic_levels": [0.35, 0.2, 0.12],
        "lfo_rate": 0.2,      # Gentle
        "lfo_depth": 0.015,
        "filter_cutoff": 2500,
        "character": "major",
    },
    3: {
        "name": "Tension Build",
        "base_freq": 100.0,   # Low neutral
        "harmonics": [2.0, 3.0, 5.0],
        "harmonic_levels": [0.25, 0.2, 0.1],
        "lfo_rate": 0.5,
        "lfo_depth": 0.025,
        "filter_cutoff": 1800,
        "character": "tension",  # Volume envelope rises
    },
    4: {
        "name": "Late Night Jazz",
        "base_freq": 146.83,  # D3
        "harmonics": [1.25, 1.5, 1.875],  # Root + minor 3rd + minor 7th (D-F-C = Dm7 feel)
        "harmonic_levels": [0.4, 0.3, 0.2],
        "lfo_rate": 0.4,
        "lfo_depth": 0.018,
        "filter_cutoff": 2200,
        "character": "jazzy",
    },
    5: {
        "name": "Space Drift",
        "base_freq": 82.41,   # E2 (low)
        "harmonics": [2.0, 4.0, 6.0],
        "harmonic_levels": [0.3, 0.15, 0.08],
        "lfo_rate": 0.15,     # Very slow drift
        "lfo_depth": 0.03,
        "filter_cutoff": 1200,  # Heavy LP filter
        "character": "spacey",  # Echo effect
    },
}


def generate_oscillator(freq: float, duration: float, sample_rate: int, 
                        wave_type: str = "sine") -> 'numpy.ndarray':
    """Generate a single oscillator waveform."""
    import numpy as np
    
    t = np.arange(int(duration * sample_rate)) / sample_rate
    
    if wave_type == "sine":
        return np.sin(2 * np.pi * freq * t)
    elif wave_type == "triangle":
        return 2 * np.abs(2 * (freq * t - np.floor(freq * t + 0.5))) - 1
    elif wave_type == "saw":
        return 2 * (freq * t - np.floor(freq * t + 0.5))
    else:
        return np.sin(2 * np.pi * freq * t)


def apply_lowpass_filter(audio: 'numpy.ndarray', cutoff: float, 
                         sample_rate: int) -> 'numpy.ndarray':
    """Apply a lowpass filter to the audio."""
    from scipy import signal
    
    nyquist = sample_rate / 2
    normalized_cutoff = min(cutoff / nyquist, 0.99)
    b, a = signal.butter(4, normalized_cutoff, btype='low')
    return signal.filtfilt(b, a, audio)


def generate_transition_music(variation: int, duration_seconds: float = 20.0, 
                              sample_rate: int = 44100) -> 'numpy.ndarray':
    """
    Generate a single variation of transition music.
    
    Args:
        variation: Which variation to generate (1-5)
        duration_seconds: Length of audio in seconds
        sample_rate: Audio sample rate
        
    Returns:
        numpy array of audio samples (float32, -1 to 1)
    """
    import numpy as np
    
    if variation not in VARIATIONS:
        raise ValueError(f"Invalid variation {variation}, must be 1-5")
    
    var = VARIATIONS[variation]
    num_samples = int(duration_seconds * sample_rate)
    t = np.arange(num_samples) / sample_rate
    
    # 1. Generate base tone (blend of sine and triangle for warmth)
    base_sine = generate_oscillator(var["base_freq"], duration_seconds, sample_rate, "sine")
    base_tri = generate_oscillator(var["base_freq"], duration_seconds, sample_rate, "triangle")
    base = base_sine * 0.7 + base_tri * 0.3
    
    # 2. Add harmonics
    audio = base.copy()
    for i, (harmonic, level) in enumerate(zip(var["harmonics"], var["harmonic_levels"])):
        harm_freq = var["base_freq"] * harmonic
        harm_wave = generate_oscillator(harm_freq, duration_seconds, sample_rate, "sine")
        audio += harm_wave * level
    
    # 3. Apply LFO modulation (slow pitch/amplitude wobble)
    lfo = np.sin(2 * np.pi * var["lfo_rate"] * t) * var["lfo_depth"]
    
    # Pitch modulation via phase modulation approximation
    phase_mod = np.cumsum(lfo) / sample_rate * var["base_freq"] * 2 * np.pi
    audio *= (1 + 0.3 * np.sin(phase_mod))  # Amplitude modulation
    
    # 4. Apply character-specific processing
    if var["character"] == "tension":
        # Rising volume envelope
        envelope = np.linspace(0.5, 1.0, num_samples)
        audio *= envelope
    elif var["character"] == "spacey":
        # Add echo effect (simple delay)
        delay_samples = int(0.4 * sample_rate)  # 400ms delay
        echo = np.zeros_like(audio)
        echo[delay_samples:] = audio[:-delay_samples] * 0.35
        audio = audio + echo
    elif var["character"] == "minor":
        # Add dissonant overtone for eerie feel
        dissonant = generate_oscillator(var["base_freq"] * 1.06, duration_seconds, sample_rate, "sine")
        audio += dissonant * 0.05  # Very subtle
    
    # 5. Apply lowpass filter for warmth
    audio = apply_lowpass_filter(audio, var["filter_cutoff"], sample_rate)
    
    # 6. Apply overall amplitude envelope
    # Slow attack to fade in smoothly
    attack_samples = int(0.5 * sample_rate)  # 0.5s fade in
    release_samples = int(0.5 * sample_rate)  # 0.5s fade out
    
    envelope = np.ones(num_samples)
    envelope[:attack_samples] = np.linspace(0, 1, attack_samples)
    envelope[-release_samples:] = np.linspace(1, 0, release_samples)
    audio *= envelope
    
    # 7. Normalize
    max_val = np.max(np.abs(audio))
    if max_val > 0:
        audio = audio / max_val * 0.7  # Leave headroom
    
    return audio.astype(np.float32)


def save_as_ogg(audio_data: 'numpy.ndarray', output_path: str, sample_rate: int = 44100):
    """
    Save audio data as OGG file using pydub.
    
    Args:
        audio_data: numpy array of audio samples (-1.0 to 1.0)
        output_path: Path to save the OGG file
        sample_rate: Audio sample rate
    """
    import numpy as np
    from pydub import AudioSegment
    
    # Convert float32 to int16
    audio_int16 = (audio_data * 32767).astype(np.int16)
    
    # Create AudioSegment
    audio_segment = AudioSegment(
        audio_int16.tobytes(),
        frame_rate=sample_rate,
        sample_width=2,  # 16-bit
        channels=1  # Mono
    )
    
    # Export as OGG with good quality
    audio_segment.export(output_path, format="ogg", codec="libvorbis", parameters=["-q:a", "6"])


def main():
    parser = argparse.ArgumentParser(
        description="Generate break transition music for KBTV"
    )
    parser.add_argument(
        "--variation",
        type=int,
        choices=[1, 2, 3, 4, 5],
        default=None,
        help="Generate only this variation (1-5). Default: generate all."
    )
    parser.add_argument(
        "--duration",
        type=float,
        default=20.0,
        help="Duration in seconds (default: 20.0)"
    )
    parser.add_argument(
        "--sample-rate",
        type=int,
        default=44100,
        help="Sample rate in Hz (default: 44100)"
    )
    parser.add_argument(
        "--output-dir",
        type=str,
        default=None,
        help="Output directory (default: kbtv/Assets/Audio/Music/)"
    )
    parser.add_argument(
        "--check-deps",
        action="store_true",
        help="Check dependencies and exit"
    )
    
    args = parser.parse_args()
    
    # Check dependencies
    check_dependencies()
    
    if args.check_deps:
        print("All dependencies are installed!")
        return
    
    # Determine output directory
    if args.output_dir:
        output_dir = Path(args.output_dir)
    else:
        script_dir = Path(__file__).parent
        project_root = script_dir.parent.parent
        output_dir = project_root / "kbtv" / "Assets" / "Audio" / "Music"
    
    # Ensure output directory exists
    output_dir.mkdir(parents=True, exist_ok=True)
    
    # Determine which variations to generate
    if args.variation:
        variations_to_generate = [args.variation]
    else:
        variations_to_generate = list(VARIATIONS.keys())
    
    print(f"Generating {len(variations_to_generate)} transition music variation(s)...")
    print(f"Duration: {args.duration}s, Sample rate: {args.sample_rate}Hz")
    print(f"Output directory: {output_dir}")
    print()
    
    for var_num in variations_to_generate:
        var_info = VARIATIONS[var_num]
        output_path = output_dir / f"break_transition_{var_num:02d}.ogg"
        
        print(f"  [{var_num}/5] {var_info['name']}...")
        
        # Generate audio
        audio_data = generate_transition_music(
            variation=var_num,
            duration_seconds=args.duration,
            sample_rate=args.sample_rate
        )
        
        # Save as OGG
        save_as_ogg(audio_data, str(output_path), args.sample_rate)
        print(f"        Saved: {output_path.name}")
    
    print()
    print("Done!")
    print()
    print("To use in Godot:")
    print("1. Open Godot and let it import the new audio files")
    print("2. Assign the break_transition_*.ogg clips to the AudioManager's break transition array")


if __name__ == "__main__":
    main()
