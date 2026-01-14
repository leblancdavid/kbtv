#!/usr/bin/env python3
"""
Download all Piper TTS voices required by KBTV.

Run this once before using generate_audio.py.

Usage:
    python download_voices.py
"""

import json
import sys
import urllib.request
import tarfile
from pathlib import Path

# Voice models directory (relative to this script)
VOICES_DIR = Path(__file__).parent / "voices"

# Piper voice model URLs from GitHub releases
# Format: voice_name -> (onnx_url, config_url)
VOICE_MODELS = {
    "en_US-ryan-medium": {
        "onnx": "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/ryan/medium/en_US-ryan-medium.onnx",
        "config": "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/ryan/medium/en_US-ryan-medium.onnx.json",
    },
    "en_US-lessac-medium": {
        "onnx": "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/medium/en_US-lessac-medium.onnx",
        "config": "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/medium/en_US-lessac-medium.onnx.json",
    },
    "en_US-amy-medium": {
        "onnx": "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/amy/medium/en_US-amy-medium.onnx",
        "config": "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/amy/medium/en_US-amy-medium.onnx.json",
    },
    "en_US-ryan-low": {
        "onnx": "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/ryan/low/en_US-ryan-low.onnx",
        "config": "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/ryan/low/en_US-ryan-low.onnx.json",
    },
    "en_US-libritts-high": {
        "onnx": "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/libritts/high/en_US-libritts-high.onnx",
        "config": "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/libritts/high/en_US-libritts-high.onnx.json",
    },
}


def download_file(url: str, dest: Path) -> bool:
    """Download a file from URL to destination."""
    try:
        print(f"    Downloading {dest.name}...")
        urllib.request.urlretrieve(url, dest)
        return True
    except Exception as e:
        print(f"    Error: {e}")
        return False


def download_voice(name: str, urls: dict) -> bool:
    """Download a voice model (onnx + config)."""
    print(f"  {name}")
    
    voice_dir = VOICES_DIR
    voice_dir.mkdir(parents=True, exist_ok=True)
    
    onnx_path = voice_dir / f"{name}.onnx"
    config_path = voice_dir / f"{name}.onnx.json"
    
    # Skip if already downloaded
    if onnx_path.exists() and config_path.exists():
        print(f"    Already exists, skipping")
        return True
    
    # Download ONNX model
    if not onnx_path.exists():
        if not download_file(urls["onnx"], onnx_path):
            return False
    
    # Download config
    if not config_path.exists():
        if not download_file(urls["config"], config_path):
            return False
    
    print(f"    OK")
    return True


def main():
    print("KBTV Piper Voice Downloader")
    print("=" * 40)
    print()
    print(f"Downloading {len(VOICE_MODELS)} voice models to:")
    print(f"  {VOICES_DIR}")
    print()
    print("(This may take a few minutes - models are ~60-100MB each)")
    print()
    
    success = 0
    failed = 0
    
    for name, urls in VOICE_MODELS.items():
        if download_voice(name, urls):
            success += 1
        else:
            failed += 1
    
    print()
    print("=" * 40)
    print(f"Complete: {success} succeeded, {failed} failed")
    
    if failed > 0:
        print()
        print("Some voices failed to download. Check your internet connection")
        print("and try again.")
        return 1
    
    print()
    print("All voices downloaded!")
    print()
    print("IMPORTANT: Update generate_audio.py to use model paths like:")
    print(f'  --model "{VOICES_DIR / "en_US-ryan-medium.onnx"}"')
    print()
    print("Or update config.json to use full paths to the .onnx files.")
    return 0


if __name__ == '__main__':
    sys.exit(main())
