# KBTV Audio Generation Tools

## 🎙️ Voice Cloning with ElevenLabs

This directory contains tools for generating high-quality voice audio for Vern Tell using ElevenLabs voice cloning.

## 📁 Files Overview

- **`elevenlabs_setup.py`** - Initial setup and voice cloning test
- **`generate_vern_audio.py`** - Batch generate all Vern dialogue audio
- **`voice_setup.py`** - Legacy Coqui TTS setup (deprecated)
- **`coqui_test.py`** - Coqui TTS testing (deprecated)
- **`tortoise_test.py`** - Tortoise TTS testing (deprecated)

## 🚀 Quick Start

### 1. Prerequisites
- ElevenLabs account (https://elevenlabs.io/)
- API key set as environment variable: `ELEVENLABS_API_KEY`

### 2. Voice Setup
```bash
python elevenlabs_setup.py
```
- Uploads reference audio
- Creates voice clone
- Tests voice quality

### 3. Generate All Audio
```bash
python generate_vern_audio.py
```
- Generates all ~220 Vern dialogue lines
- Organizes by mood categories
- Saves to game assets directory

## 🎯 Voice Reference Audio

Located in `../../assets/audio/voice_references/`:
- `vern_reference_001_final.wav` - Primary reference (3s)
- `vern_reference_002.wav` through `vern_reference_005.wav` - Additional samples

These were extracted from Art Bell Coast-to-Coast AM audio with transformative processing.

## 📊 Output Structure

Generated audio is saved to `../../assets/audio/voice/Vern/Broadcast/`:

```
Vern/Broadcast/
├── neutral/
│   ├── vern_opening_001.mp3
│   └── ...
├── energized/
│   ├── vern_opening_001.mp3
│   └── ...
├── tired/
├── irritated/
├── amused/
├── focused/
└── gruff/
```

## 🎭 Mood-Based Voice Settings

Different voice settings are automatically applied based on mood:
- **neutral:** Balanced, professional
- **energized:** More dynamic, higher similarity
- **tired:** More stable, less expressive
- **irritated:** More expressive variation
- **amused:** Balanced expressiveness
- **focused:** More controlled delivery
- **gruff:** Balanced character

## ⚡ API Usage & Limits

- **Free tier:** 10,000 characters/month
- **Rate limiting:** 0.5s between requests (built-in)
- **File format:** MP3 (high quality)
- **Cost:** ~$0.30 per 1,000 characters (if exceeding free tier)

## 🛠️ Troubleshooting

### API Key Issues
```bash
# Set environment variable
set ELEVENLABS_API_KEY=your_api_key_here

# Or edit the script directly
# Find: api_key = os.getenv('ELEVENLABS_API_KEY')
# Replace with: api_key = "your_api_key_here"
```

### Voice Quality Issues
- Check reference audio quality
- Adjust voice settings in `get_mood_voice_settings()`
- Try different ElevenLabs models

### Rate Limiting
- Script includes automatic delays
- If hitting limits, increase sleep time

## 🔄 Legacy Tools

The other TTS scripts (Coqui, Tortoise) are kept for reference but had compatibility issues. ElevenLabs provides superior results with simpler setup.

---

**🎉 Ready to give Vern Tell his authentic Art Bell-inspired voice!**