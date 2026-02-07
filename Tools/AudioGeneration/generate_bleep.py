#!/usr/bin/env python3
"""
Generate a custom bleep sound for the cursing feature.
Creates a higher-pitched bleep (800Hz → 1200Hz frequency sweep)
lasting approximately 1.5 seconds.
"""

import os
from pydub import AudioSegment
from pydub.generators import Sine

def generate_bleep_sound():
    """Generate a custom bleep sound for cursing."""

    print("Generating custom bleep sound...")

    # Create a frequency sweep from 800Hz to 1200Hz over 1.5 seconds
    # Using multiple segments for the sweep effect
    duration_ms = 1500  # 1.5 seconds total

    # Create segments with gradually increasing frequency
    frequencies = [800, 900, 1000, 1100, 1200]
    segment_duration = duration_ms // len(frequencies)

    bleep_segments = []
    for freq in frequencies:
        segment = Sine(freq).to_audio_segment(duration=segment_duration)
        bleep_segments.append(segment)

    # Combine all segments
    bleep = bleep_segments[0]
    for segment in bleep_segments[1:]:
        bleep = bleep + segment

    # Apply fade in/out for smoother sound
    bleep = bleep.fade_in(50).fade_out(150)

    # Normalize to reasonable volume (-20dBFS to avoid clipping)
    bleep = bleep.normalize(headroom=20.0)

    # Output path
    output_dir = os.path.join("..", "..", "assets", "audio")
    os.makedirs(output_dir, exist_ok=True)
    output_path = os.path.join(output_dir, "bleep.wav")

    # Export as WAV (Godot-friendly format)
    bleep.export(output_path, format="wav", parameters=["-acodec", "pcm_s16le", "-ar", "44100", "-ac", "1"])

    file_size = os.path.getsize(output_path)
    print(f"Generated bleep sound: {output_path}")
    print(f"Duration: {len(bleep)/1000:.1f} seconds")
    print(f"File size: {file_size} bytes")

    return output_path

if __name__ == "__main__":
    try:
        output_file = generate_bleep_sound()
        print(f"\nBleep sound generated successfully: {output_file}")
        print("The new bleep will play during cursing events with a rising pitch sweep.")
    except Exception as e:
        print(f"Error generating bleep sound: {e}")
        import traceback
        traceback.print_exc()