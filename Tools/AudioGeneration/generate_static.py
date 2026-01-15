#!/usr/bin/env python3
"""
Generate phone static loop audio for KBTV.

Creates a loopable phone line static/noise effect using synthesized noise.
The output is designed to be subtle background noise that plays during
caller conversations and is controlled by the equipment upgrade level.

Usage:
    python generate_static.py [--duration SECONDS] [--output PATH]

Requirements:
    pip install numpy scipy pydub
    ffmpeg must be in PATH for OGG export
"""

import argparse
import os
import sys
from pathlib import Path

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


def generate_phone_static(duration_seconds: float = 5.0, sample_rate: int = 22050) -> 'numpy.ndarray':
    """
    Generate phone line static noise.
    
    Creates a mix of:
    - Band-limited white noise (phone line hiss)
    - Occasional crackle bursts
    - Low frequency hum (60Hz power line)
    
    Args:
        duration_seconds: Length of audio in seconds
        sample_rate: Audio sample rate
        
    Returns:
        numpy array of audio samples
    """
    import numpy as np
    from scipy import signal
    
    num_samples = int(duration_seconds * sample_rate)
    
    # 1. Base white noise (phone line hiss)
    white_noise = np.random.randn(num_samples)
    
    # Apply bandpass filter (300Hz - 3400Hz, phone bandwidth)
    nyquist = sample_rate / 2
    low = 300 / nyquist
    high = 3400 / nyquist
    b, a = signal.butter(4, [low, high], btype='band')
    phone_hiss = signal.filtfilt(b, a, white_noise)
    
    # 2. Occasional crackle bursts
    crackle = np.zeros(num_samples)
    num_crackles = int(duration_seconds * 2)  # ~2 crackles per second
    
    for _ in range(num_crackles):
        pos = np.random.randint(0, num_samples - 100)
        length = np.random.randint(10, 100)
        amplitude = np.random.uniform(0.3, 0.8)
        crackle[pos:pos+length] = np.random.randn(length) * amplitude * np.exp(-np.arange(length) / 20)
    
    # 3. 60Hz power line hum (very subtle)
    t = np.arange(num_samples) / sample_rate
    hum = 0.02 * np.sin(2 * np.pi * 60 * t)
    
    # Mix together
    mixed = phone_hiss * 0.4 + crackle * 0.3 + hum
    
    # Normalize to prevent clipping
    mixed = mixed / np.max(np.abs(mixed)) * 0.8
    
    # Apply gentle fade at start/end for seamless looping
    fade_samples = int(0.1 * sample_rate)  # 100ms fade
    fade_in = np.linspace(0, 1, fade_samples)
    fade_out = np.linspace(1, 0, fade_samples)
    
    mixed[:fade_samples] *= fade_in
    mixed[-fade_samples:] *= fade_out
    
    return mixed.astype(np.float32)


def save_as_ogg(audio_data: 'numpy.ndarray', output_path: str, sample_rate: int = 22050):
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
    
    # Export as OGG
    audio_segment.export(output_path, format="ogg", codec="libvorbis", parameters=["-q:a", "5"])
    

def main():
    parser = argparse.ArgumentParser(
        description="Generate phone static loop audio for KBTV"
    )
    parser.add_argument(
        "--duration", 
        type=float, 
        default=5.0,
        help="Duration in seconds (default: 5.0)"
    )
    parser.add_argument(
        "--output",
        type=str,
        default=None,
        help="Output path (default: Assets/Audio/SFX/phone_static_loop.ogg)"
    )
    parser.add_argument(
        "--sample-rate",
        type=int,
        default=22050,
        help="Sample rate in Hz (default: 22050)"
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
    
    # Determine output path
    if args.output:
        output_path = Path(args.output)
    else:
        # Default to Unity Assets folder
        script_dir = Path(__file__).parent
        project_root = script_dir.parent.parent
        output_path = project_root / "kbtv" / "Assets" / "Audio" / "SFX" / "phone_static_loop.ogg"
    
    # Ensure output directory exists
    output_path.parent.mkdir(parents=True, exist_ok=True)
    
    print(f"Generating {args.duration}s phone static at {args.sample_rate}Hz...")
    
    # Generate audio
    audio_data = generate_phone_static(
        duration_seconds=args.duration,
        sample_rate=args.sample_rate
    )
    
    print(f"Saving to: {output_path}")
    
    # Save as OGG
    save_as_ogg(audio_data, str(output_path), args.sample_rate)
    
    print("Done!")
    print(f"\nTo use in Godot:")
    print(f"1. Import {output_path.name} into your project")
    print(f"2. Assign to the static noise controller")


if __name__ == "__main__":
    main()
