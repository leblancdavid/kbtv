# KBTV - Voice & Audio Production Plan

## Overview

This document outlines the strategy for producing voice audio for KBTV's dialogue system, including Vern's broadcasts, caller conversations, and filler content.

**Approach**: Pre-generated audio using Piper TTS (offline, free) with runtime effects applied via Unity Audio Mixer.

## Audio Scope

### Content Volume

| Category | Description | Estimated Lines |
|----------|-------------|-----------------|
| **Conversation Arcs** | 80 arcs × 5 moods × 2 belief paths | ~6,400-9,600 lines |
| **Vern Broadcasts** | Opening, closing, between-callers, dead air filler | ~100 templates |
| **Total** | All voiced content | ~7,000-10,000 lines |

### Character Voices Needed

| Character | Description | Voice Style |
|-----------|-------------|-------------|
| **Vern Tell** | Skeptical radio host | Gravelly, late-night radio DJ, dry wit |
| **Callers** | Various paranormal witnesses | Diverse voices, filtered through phone effect |

## Production Approach

### Chosen: Pre-Generated with Piper TTS

All audio is generated offline during development using Piper TTS, then imported into Unity as static audio files.

**Why Piper?**
- Fully offline and free (no API costs)
- Fast generation (real-time or faster on CPU)
- Multiple voice models for variety
- Active development (OHF-Voice/piper1-gpl)
- Quality is good enough, especially with post-processing effects

**Why Pre-Generated?**
- Simpler Unity integration (just load AudioClips)
- No runtime dependencies
- Consistent audio quality
- Can fine-tune individual lines
- No `{callerName}` substitution needed (use generic greetings)

### Generation Workflow

```
1. Dialogue JSON files (arcs, broadcasts)
        ↓
2. Python script extracts all lines
        ↓
3. Piper generates WAV for each line
        ↓
4. Normalize volume only (no effects)
        ↓
5. Convert to OGG, organize by folder
        ↓
6. Import to Unity Assets/Audio/Voice/
        ↓
7. Unity Audio Mixer applies effects at runtime
```

**Note**: Audio effects (phone filter, radio compression, static) are applied at runtime in Unity, not baked into the files. This enables the equipment upgrade system to dynamically improve audio quality.

## Voice Profiles

### Vern Tell

| Attribute | Value |
|-----------|-------|
| **Piper Voice** | `en_US-ryan-medium` (deeper, authoritative) |
| **Fallback** | `en_US-lessac-medium` |
| **Style** | Late-night radio host, Art Bell inspired |
| **Delivery** | Measured, occasionally sardonic, professional |

**Mood Variations:**

| Mood | Speed | Pitch | Notes |
|------|-------|-------|-------|
| Tired | 0.85x | -5% | Slower, lower energy |
| Grumpy | 1.0x | 0% | Clipped, shorter pauses |
| Neutral | 1.0x | 0% | Standard delivery |
| Engaged | 1.05x | +3% | Slightly warmer |
| Excited | 1.15x | +5% | Faster, more energy |

### Caller Voice Archetypes

| Archetype | Piper Voice | Speed | Pitch | Notes |
|-----------|-------------|-------|-------|-------|
| **Default Male** | `en_US-lessac-medium` | 1.0x | 0% | Clear, neutral |
| **Default Female** | `en_US-amy-medium` | 1.0x | 0% | Clear, neutral |
| **Gruff/Older** | `en_US-ryan-low` | 0.9x | -5% | Deeper, slower |
| **Nervous/Young** | `en_US-libritts-high` | 1.1x | +5% | Higher, faster |
| **Enthusiastic** | `en_US-lessac-medium` | 1.2x | +3% | Excited delivery |
| **Conspiracy Theorist** | `en_US-ryan-medium` | 1.15x | 0% | Intense, rapid |

All caller audio receives phone filter via Unity Audio Mixer at runtime.

## Runtime Audio Effects (Unity Audio Mixer)

Audio effects are applied at runtime via Unity's Audio Mixer system. This allows the equipment upgrade system to dynamically improve audio quality as the player progresses.

### Audio Mixer Structure

```
MasterMixer
├── VernGroup
│   ├── High-Pass Filter (80Hz)
│   ├── Compressor
│   ├── EQ (mid-boost, adjustable by equipment level)
│   └── Distortion (adjustable, starts higher at low levels)
│
├── CallerGroup
│   ├── Low-Pass Filter (adjustable: 3.4kHz → 8kHz+)
│   ├── High-Pass Filter (adjustable: 300Hz → 80Hz)
│   ├── Distortion (adjustable: high → minimal)
│   └── Static/Noise send (adjustable volume)
│
└── MusicGroup / SFXGroup
```

### Equipment Upgrade System

**TODO**: Implement equipment upgrades that improve broadcast quality.

The station starts with rough, low-quality equipment. Players can purchase upgrades to improve audio clarity:

| Equipment Level | Phone Line Quality | Radio Broadcast Quality |
|-----------------|-------------------|------------------------|
| **Level 1 (Starting)** | Heavy static, narrow band (300Hz-3.4kHz), distortion | Muffled, background hum, occasional crackle |
| **Level 2** | Less static, slightly wider band | Clearer, reduced hum |
| **Level 3** | Minimal static, wider band | Professional radio sound |
| **Level 4 (Max)** | Crystal clear phone line | Broadcast-quality, warm presence |

### Exposed Mixer Parameters

These parameters are controlled by the equipment level:

| Parameter | Level 1 | Level 4 | Description |
|-----------|---------|---------|-------------|
| `CallerLowPassCutoff` | 3400 Hz | 12000 Hz | Higher = clearer callers |
| `CallerHighPassCutoff` | 300 Hz | 80 Hz | Lower = fuller sound |
| `CallerDistortion` | 0.15 | 0.0 | Lower = cleaner sound |
| `StaticVolume` | -10 dB | -80 dB | Lower = less noise |
| `VernDistortion` | 0.08 | 0.0 | Broadcast clarity |
| `VernMidBoost` | 2 dB | 4 dB | Radio presence |

## Audio Technical Specs

### Format

| Property | Value |
|----------|-------|
| **Source Format** | WAV (from Piper) |
| **Output Format** | OGG Vorbis (Unity preferred) |
| **Sample Rate** | 22.05 kHz (Piper default) or 44.1 kHz |
| **Channels** | Mono |
| **OGG Quality** | 0.5-0.7 |

### Effects Chain

Effects are applied at runtime via Unity Audio Mixer (see "Runtime Audio Effects" section above).

**Vern (Radio Broadcast) - VernGroup:**
1. High-pass filter (80Hz) - remove rumble
2. Compressor (threshold -20dB, ratio 3:1)
3. Mid-boost EQ (2-4kHz) - radio presence (adjustable by equipment)
4. Light distortion (adjustable by equipment, starts rough)

**Callers (Phone Line) - CallerGroup:**
1. Band-pass filter (300Hz-3.4kHz at Level 1, widens with upgrades)
2. Distortion/saturation (high at Level 1, reduces with upgrades)
3. Compression (threshold -15dB, ratio 4:1)
4. Static/noise layer (loud at Level 1, fades with upgrades)

## Tools & Dependencies

### Required Software

| Tool | Purpose | Installation |
|------|---------|--------------|
| **Python 3.9+** | Script runtime | System install |
| **Piper TTS** | Voice synthesis | `pip install piper-tts` |
| **pydub** | Audio normalization | `pip install pydub` |
| **ffmpeg** | Audio conversion | System install (add to PATH) |

### Piper Voice Models

Download voice models from [Piper Voices](https://github.com/rhasspy/piper/blob/master/VOICES.md) or use the auto-download feature.

```bash
# List available voices
piper --list-voices

# Generate sample
echo "Welcome to Beyond the Veil AM" | piper --model en_US-ryan-medium --output_file test.wav
```

### Project Scripts Location

```
Tools/
├── AudioGeneration/
│   ├── generate_audio.py      # Main batch generation script
│   ├── download_voices.py     # Download required Piper voice models
│   ├── config.json            # Voice mappings and settings
│   └── temp/                  # Temporary files (gitignored)
```

See [TOOLS.md](TOOLS.md) for detailed usage instructions.

## File Organization

### Actual Directory Structure

```
Assets/Audio/Voice/
├── Vern/
│   └── Broadcast/
│       ├── Opening/           # vern_opening_001.ogg, etc.
│       ├── Closing/           # vern_closing_001.ogg, etc.
│       ├── BetweenCallers/    # vern_betweencallers_001.ogg, etc.
│       ├── DeadAirFiller/     # vern_deadairfiller_001.ogg, etc.
│       ├── Introduction/      # vern_introduction_001.ogg, etc.
│       ├── SignOff/           # vern_signoff_001.ogg, etc.
│       └── ... (other categories)
└── Callers/
    ├── UFOs/
    │   ├── ufo_credible_dashcam/
    │   │   ├── Neutral/       # ufo_credible_dashcam_neutral_001_vern.ogg, etc.
    │   │   ├── Tired/
    │   │   ├── Grumpy/
    │   │   ├── Engaged/
    │   │   └── Excited/
    │   ├── ufos_fake_prankster/
    │   │   └── ... (mood folders)
    │   └── ... (other arcs)
    ├── Cryptids/
    ├── Conspiracies/
    └── Ghosts/
```

Note: Caller conversation clips are organized by `Topic/ArcId/Mood/` with each mood containing all lines for that variant.

## Naming Convention

Audio files use a specific naming pattern that matches the Addressable address format:

**Conversation lines:**
```
{arcId}_{mood}_{lineIndex:D3}_{speaker}.ogg

Examples:
ufo_credible_dashcam_neutral_001_vern.ogg
ufo_credible_dashcam_neutral_002_caller.ogg
ufos_fake_prankster_tired_001_vern.ogg
```

Note: The `arcId` comes from the arc JSON file and may or may not include a topic prefix. The arcId is used as-is without modification.

**Broadcast lines:**
```
vern_{category}_{index:D3}.ogg

Examples:
vern_opening_001.ogg
vern_deadairfiller_012.ogg
vern_betweencallers_003.ogg
```

## Implementation Plan

### Phase 1: Setup & Prototype
- [x] Install Piper TTS and dependencies
- [x] Test voice models, select best for Vern and callers
- [x] Create basic generation script (normalize only, no effects)
- [x] Create voice download helper script
- [x] Generate all 950 voice lines (Vern broadcasts + caller conversations)
- [x] Validate audio quality and timing

### Phase 2: Unity Audio Integration
- [x] Install Addressables package for async clip loading
- [x] Create VoiceAudioService for clip loading/caching via Addressables
- [x] Add Id field to DialogueTemplate for broadcast line matching
- [x] Add IDs to VernDialogue.json broadcast entries
- [x] Update AudioManager with speaker-based mixer routing
- [x] Update ConversationManager to trigger voice playback
- [x] Configure Addressables groups for voice audio folders
- [x] Add VoiceAudioService instantiation to GameBootstrap
- [x] Create Audio Mixer with VernGroup and CallerGroup (basic routing only)
- [ ] Assign mixer groups to AudioManager in scene (Inspector)
- [ ] Update ConversationPanel with dynamic typewriter speed (synced to audio duration)

### Phase 3: Audio Effects & Polish (Future)
- [ ] Add effect components to mixer (filters, distortion, compression)
- [ ] Expose parameters for equipment upgrade control
- [ ] Create AudioQualityController to map equipment level to mixer params
- [ ] Test equipment upgrade progression (Level 1 -> 4)
- [ ] Listen through and identify problem lines
- [ ] Regenerate or manually edit issues
- [ ] Fine-tune timing/pacing
- [ ] Optimize file sizes
- [ ] Test full conversation flow with audio

## Unity Integration Architecture

### Overview

Voice audio is loaded via Unity Addressables and played through the AudioManager when dialogue lines are displayed. The typewriter text effect speed is dynamically adjusted to match the audio clip duration.

### System Components

```
VoiceAudioService          AudioManager              ConversationManager
      │                         │                           │
      │ LoadClipAsync()         │                           │
      │<────────────────────────│                           │
      │                         │   OnLineDisplayed         │
      │                         │<──────────────────────────│
      │   GetClip(lineId)       │                           │
      │<────────────────────────│                           │
      │                         │                           │
      │   AudioClip             │                           │
      │────────────────────────>│                           │
      │                         │   PlayVoiceClip(clip)     │
      │                         │───────────────────────────>
      │                         │                           │
      │                         │              ConversationPanel
      │                         │                     │
      │                         │ SetLineDuration()   │
      │                         │────────────────────>│
      │                         │                     │
      │                         │      (typewriter syncs to audio)
```

### VoiceAudioService

Location: `Assets/Scripts/Runtime/Audio/VoiceAudioService.cs`

Responsibilities:
- Load clips via Addressables by address
- Cache clips for current conversation in memory
- Preload all clips for an arc when conversation starts
- Unload clips when conversation ends (memory management)
- Fallback gracefully when clip not found (log warning, return null)

Key Methods:
```csharp
// Preload all clips for a conversation arc (call on conversation start)
Task PreloadConversationAsync(string arcId, string topic, VernMood mood, int lineCount);

// Get a cached conversation clip
AudioClip GetConversationClip(string arcId, string topic, VernMood mood, int lineIndex, Speaker speaker);

// Get a broadcast clip by ID (e.g., "vern_opening_001")
Task<AudioClip> GetBroadcastClipAsync(string clipId);

// Unload cached clips (call on conversation end)
void UnloadCurrentConversation();
```

### Audio File Address Format

Addressables uses simplified addresses (filename without extension). The address is set by the `VoiceAudioSetup` editor script.

**Conversation clips:**
```
Address: {arcId}_{mood}_{lineIndex:D3}_{speaker}
Example: ufo_credible_dashcam_neutral_001_vern
File:    Assets/Audio/Voice/Callers/UFOs/ufo_credible_dashcam/Neutral/ufo_credible_dashcam_neutral_001_vern.ogg
```

Note: The address does NOT include topic prefix. The arcId from the JSON is used directly.

**Broadcast clips:**
```
Address: vern_{category}_{index:D3}
Example: vern_opening_001
File:    Assets/Audio/Voice/Vern/Broadcast/Opening/vern_opening_001.ogg
```

### Typewriter Synchronization

When a dialogue line is displayed:

1. `ConversationManager` fires `OnLineDisplayed` event
2. `VoiceAudioService` retrieves the cached clip
3. `AudioManager.PlayVoiceClip(clip, speaker)` starts playback
4. `ConversationPanel.SetLineDuration(clip.length)` calculates dynamic typing speed:
   ```csharp
   float charsPerSecond = text.Length / audioDuration;
   ```
5. Typewriter effect runs at calculated speed, finishing when audio ends

### Missing Clip Handling

If a clip is not found:
- **Development**: Log warning with expected address
- **Runtime**: Continue silently, use default typewriter speed (40 chars/sec)
- **No blocking**: Game continues without audio for that line

### Memory Management

To avoid loading all 950+ clips at once:

1. **Preload on conversation start**: Load all clips for the current arc (~10-20 clips)
2. **Cache in Dictionary**: Keep clips in memory during conversation
3. **Unload on conversation end**: Release clips when conversation completes
4. **Broadcast clips**: Load on-demand, cache for session duration

### Addressables Configuration

The voice audio system uses Unity Addressables for efficient async loading. Configuration is automated via editor scripts.

#### Automated Setup (Recommended)

Run from Unity menu: **KBTV > Setup Voice Audio > Configure Addressables Only**

This will:
1. Create/find Addressables settings
2. Create a "VoiceAudio" group
3. Mark all `.ogg` files in `Assets/Audio/Voice/Vern/Broadcast/` and `Assets/Audio/Voice/Callers/` as Addressable
4. Set each file's address to its filename (without extension)

#### Manual Verification

After running the setup, verify in **Window > Asset Management > Addressables > Groups**:
- A "VoiceAudio" group should exist
- It should contain ~950 audio clip entries
- Each entry's address should match its filename (e.g., `ufo_credible_dashcam_neutral_001_vern`)

#### Building for Release

For standalone builds, you must build the Addressables catalog:
1. Open **Window > Asset Management > Addressables > Groups**
2. Click **Build > New Build > Default Build Script**

Note: In the Unity Editor, Addressables work without building if using "Use Asset Database" play mode.

### Audio Mixer Structure (Phase 2A - Basic)

Initial implementation uses basic routing only. Effects added in Phase 3.

```
KBTVMixer
├── Master
│   ├── VernGroup       ← Vern voice clips
│   ├── CallerGroup     ← Caller voice clips
│   ├── SFXGroup        ← Sound effects (existing)
│   └── MusicGroup      ← Background music (existing)
```

AudioManager changes:
```csharp
[SerializeField] private AudioMixerGroup _vernMixerGroup;
[SerializeField] private AudioMixerGroup _callerMixerGroup;

public void PlayVoiceClip(AudioClip clip, Speaker speaker)
{
    _voiceSource.outputAudioMixerGroup = speaker == Speaker.Vern 
        ? _vernMixerGroup 
        : _callerMixerGroup;
    _voiceSource.clip = clip;
    _voiceSource.Play();
}
```

## Dialogue Considerations

### Generic Greetings

Since we're pre-generating audio, we cannot dynamically insert caller names. Update dialogue to use generic greetings:

**Instead of:**
> "Hi Vern, this is {callerName} from {callerLocation}."

**Use:**
> "Hey, first-time caller here."
> "Long-time listener, first-time caller."
> "Yeah, hi, thanks for taking my call."
> "Vern! Big fan, man."

### Mood-Specific Wording

Consider writing slightly different text for extreme moods:

- **Tired Vern**: Shorter sentences, trailing off
- **Excited Vern**: More interjections, enthusiastic words

This complements the audio speed/pitch adjustments.

## Future Upgrades

### Equipment Upgrade Integration

The equipment upgrade system will be implemented as a gameplay feature:

1. **EquipmentManager** - Tracks current equipment level (1-4)
2. **AudioQualityController** - Reads equipment level and adjusts Audio Mixer parameters
3. **Shop/Upgrade UI** - Player purchases upgrades between shows
4. **Persistence** - Equipment level saved with game progress

This creates a tangible sense of progression as the station improves from a rough AM operation to a professional broadcast.

### Voice Quality Upgrades

If higher voice quality is needed later:

1. **ElevenLabs for Vern** - Generate Vern's lines with premium AI voice for more iconic delivery
2. **Custom Piper voice** - Train a custom voice model on specific voice samples
3. **Voice actor recording** - Replace TTS with human recordings for key lines

The file naming and Unity integration will remain the same regardless of voice source.

## References

- [Piper TTS (OHF-Voice)](https://github.com/OHF-Voice/piper1-gpl) - Voice synthesis tool
- [Piper Voice Models](https://github.com/rhasspy/piper/blob/master/VOICES.md) - Available voices
- [AUDIO_DESIGN.md](AUDIO_DESIGN.md) - Overall audio direction
- [CONVERSATION_DESIGN.md](CONVERSATION_DESIGN.md) - Dialogue system overview
- [CONVERSATION_ARC_SCHEMA.md](CONVERSATION_ARC_SCHEMA.md) - Arc JSON structure
- [DEAD_AIR_FILLER.md](DEAD_AIR_FILLER.md) - Broadcast flow and filler system
