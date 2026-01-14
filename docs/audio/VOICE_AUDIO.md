# KBTV - Voice & Audio Production Plan

## Overview

This document outlines the strategy for producing voice audio for KBTV's dialogue system, including Vern's broadcasts, caller conversations, and filler content.

**Approach**: Pre-generated audio using Piper TTS (offline, free) with runtime effects applied via Unity Audio Mixer.

## Audio Scope

### Content Volume

| Category | Description | Estimated Lines |
|----------|-------------|-----------------|
| **Conversation Arcs** | 18 arcs × 1 dialogue × 2 belief paths | ~216 lines |
| **Vern Audio per Arc** | 7 tones × dialogue lines | ~1,500 lines |
| **Total Vern Audio** | All arcs × 7 tones | ~1,500 lines |
| **Caller Audio** | 1 version per arc | ~216 lines |
| **Vern Broadcasts** | Opening, closing, between-callers, dead air filler | ~100 templates |

**Total unique audio files to generate: ~1,800 files**

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

**Mood Variations (7 VernMoodType variations):**

| VernMoodType | DialogueTone | Speed | Pitch | Notes |
|--------------|--------------|-------|-------|-------|
| Tired | Dismissive | 0.85x | -5% | Slower, trailing off, low energy |
| Energized | Excited | 1.15x | +5% | Fast, enthusiastic, engaged |
| Irritated | Annoyed | 0.95x | 0% | Short, clipped, impatient |
| Amused | Believing | 1.05x | +3% | Warm, playful, amused |
| Gruff | Dismissive | 0.90x | -3% | Brusk, direct, no-nonsense |
| Focused | Skeptical | 1.0x | 0% | Analytical, probing, questioning |
| Neutral | Neutral | 1.0x | 0% | Professional, balanced delivery |

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
| `CallerLowPassCutoff` | 2200 Hz | 10000 Hz | Higher = clearer callers |
| `CallerHighPassCutoff` | 500 Hz | 100 Hz | Lower = fuller sound |
| `CallerLowPassResonance` | 2.5 | 1.0 | Higher = nasal phone quality |
| `CallerNasalBoost` | +4 dB | 0 dB | ParamEQ @ 1800Hz for phone honk |
| `CallerDistortion` | 0.12 | 0.0 | Lower = cleaner sound |
| `CallerVolume` | +6 dB | +6 dB | Compensates for filter loss |
| `VernDistortion` | 0.01 | 0.0 | Broadcast clarity |
| `VernMidBoost` | 1 dB | 2 dB | Radio presence |
| `VernVolume` | +12 dB | +12 dB | Compensates for compressor |

**Note**: Static volume is controlled directly via `StaticNoiseController.SetBaseVolume()` rather than through the mixer. Static only plays while callers are speaking, not during the entire call.

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

See [TOOLS.md](../tools/TOOLS.md) for detailed usage instructions.

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
    │   │   ├── Tired/         # ufo_credible_dashcam_tired_001_vern.ogg, etc.
    │   │   ├── Energized/
    │   │   ├── Irritated/
    │   │   ├── Amused/
    │   │   ├── Gruff/
    │   │   ├── Focused/
    │   │   ├── Neutral/
    │   │   └── Caller/        # Caller lines (same for all moods)
    │   ├── ufos_fake_prankster/
    │   │   └── ... (7 mood folders + Caller)
    │   └── ... (other arcs)
    ├── Cryptids/
    ├── Conspiracies/
    └── Ghosts/
```

**Note:** Caller conversation clips are organized by `Topic/ArcId/{Mood}/` for Vern lines, and `Topic/ArcId/Caller/` for caller lines. Each mood folder contains all Vern lines for that arc in that mood tone.

## Naming Convention

Audio files use a specific naming pattern that matches the Addressable address format:

**Vern conversation lines (7 tones per arc):**
```
{arcId}_{mood}_{lineIndex:D3}_{speaker}.ogg

Examples:
ufo_credible_dashcam_tired_001_vern.ogg
ufo_credible_dashcam_energized_001_vern.ogg
ufo_credible_dashcam_irritated_001_vern.ogg
ufo_credible_dashcam_amused_001_vern.ogg
ufo_credible_dashcam_gruff_001_vern.ogg
ufo_credible_dashcam_focused_001_vern.ogg
ufo_credible_dashcam_neutral_001_vern.ogg
```

**Caller conversation lines (1 version per arc, placed in Caller folder):**
```
{arcId}_neutral_{lineIndex:D3}_{speaker}.ogg

Examples:
ufo_credible_dashcam_neutral_002_caller.ogg
```

**Belief branch lines:**
```
{arcId}_{mood}_{beliefTag}_{lineIndex:D3}_{speaker}.ogg

Examples:
ufo_credible_dashcam_tired_skep_009_vern.ogg  (skeptical branch, tired tone)
ufo_credible_dashcam_energized_beli_011_vern.ogg (believing branch, energized tone)
```

The `beliefTag` is:
- `skep` for Skeptical belief branch lines (first 4 chars of "Skeptical")
- `beli` for Believing belief branch lines (first 4 chars of "Believing")

Note: The `arcId` comes from the arc JSON file and may or may not include a topic prefix. The arcId is used as-is without modification.

### Line Index and Belief Paths

The `lineIndex` in audio filenames is based on the **arc JSON structure**, not the runtime conversation order. Lines are indexed sequentially across all sections including BOTH belief paths.

**IMPORTANT**: The generation order is: Intro → Development → Conclusion → Skeptical → Believing

This ordering ensures conclusion lines are numbered before belief branches, which makes the sequential numbering work correctly even though belief branches are mutually exclusive at runtime:

```
Intro lines:       001, 002
Development lines: 003, 004, 005, 006
Conclusion lines:  007, 008                 (processed BEFORE belief branches)
Skeptical lines:   009, 010  (belief path) - uses "_skep_" prefix
Believing lines:   011, 012  (belief path) - uses "_beli_" prefix
```

The Python audio generator (`Tools/AudioGeneration/generate_audio.py`) and C# parser (`ArcJsonParser.cs`) must use the same section ordering to ensure audio addresses match file names.

At runtime, only one belief path is used per conversation, but the audio file indices remain fixed. Each `DialogueLine` tracks:
- `ArcLineIndex`: The original arc position (0-based index, becomes 1-based in filenames)
- `Section`: Which arc section the line belongs to (Intro, Development, Skeptical, Believing, Conclusion)

The `Section` property is critical for belief branch audio lookup - it determines whether the `_skep_` or `_beli_` prefix is added to the audio address.

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
- [x] Create AudioQualityController to map equipment level to mixer params
- [x] Create StaticNoiseController for phone static layer
- [x] Wire StaticNoiseController to caller on-air/off-air events
- [ ] Add effect components to mixer (filters, distortion, compression)
- [ ] Expose parameters for equipment upgrade control
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
- Preload all clips for an arc when conversation starts (using section info for correct paths)
- Unload clips when conversation ends (memory management)
- Fallback gracefully when clip not found (log warning, return null)

Key Methods:
```csharp
// Preload all clips for a conversation arc (call on conversation start)
// Uses dialogue set and mood type to determine tone and get section info for each line
Task PreloadConversationAsync(string arcId, string topic, VernMoodType moodType, ArcDialogueSet dialogue);

// Get a cached conversation clip (section determines belief path prefix)
AudioClip GetConversationClip(int lineIndex, Speaker speaker, ArcSection section);

// Get a conversation clip, loading on-demand if not cached
Task<AudioClip> GetConversationClipAsync(int lineIndex, Speaker speaker, ArcSection section);

// Get a broadcast clip by ID (e.g., "vern_opening_001")
Task<AudioClip> GetBroadcastClipAsync(string clipId);

// Unload cached clips (call on conversation end)
void UnloadCurrentConversation();
```

The `ArcSection` parameter is essential for belief branch lines - it determines whether the audio address includes `_skep_` or `_beli_` prefix.

### Audio File Address Format

Addressables uses simplified addresses (filename without extension). The address is set by the `VoiceAudioSetup` editor script.

**Vern conversation clips (7 mood types):**
```
Address: {arcId}_{mood}_{lineIndex:D3}_{speaker}
Example: ufo_credible_dashcam_tired_001_vern
File:    Assets/Audio/Voice/Callers/UFOs/ufo_credible_dashcam/Tired/ufo_credible_dashcam_tired_001_vern.ogg

Vern mood types: tired, energized, irritated, amused, gruff, focused, neutral
```

**Caller conversation clips (1 version per arc):**
```
Address: {arcId}_neutral_{lineIndex:D3}_{speaker}
Example: ufo_credible_dashcam_neutral_002_caller
File:    Assets/Audio/Voice/Callers/UFOs/ufo_credible_dashcam/Caller/ufo_credible_dashcam_neutral_002_caller.ogg
```

**Belief branch clips (skeptical/believing sections):**
```
Address: {arcId}_{mood}_{beliefTag}_{lineIndex:D3}_{speaker}
Example: ufo_credible_dashcam_tired_skep_009_vern
File:    Assets/Audio/Voice/Callers/UFOs/ufo_credible_dashcam/Tired/ufo_credible_dashcam_tired_skep_009_vern.ogg
```

The `beliefTag` is `skep` or `beli` depending on the arc section (Skeptical or Believing).

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

### Phase 3: Audio Mixer Effects Setup (Unity Editor)

Follow these steps to add audio effects that respond to equipment upgrades.

#### Step 1: Open the Audio Mixer

1. In Unity, go to **Window > Audio > Audio Mixer**
2. Open `Assets/Audio/KBTVMixer.mixer`

#### Step 2: Add Effects to CallerGroup

Select **CallerGroup** in the mixer hierarchy, then add these effects in the Inspector:

| Effect | Property | Initial Value | Notes |
|--------|----------|---------------|-------|
| **Lowpass Simple** | Cutoff freq | 2200 Hz | Expose as `CallerLowPassCutoff` |
| **Lowpass Simple** | Resonance | 2.5 | Expose as `CallerLowPassResonance` |
| **Highpass Simple** | Cutoff freq | 500 Hz | Expose as `CallerHighPassCutoff` |
| **ParamEQ** | Center freq | 1800 Hz | Nasal phone frequency (fixed) |
| **ParamEQ** | Octave range | 1.0 | Width of boost (fixed) |
| **ParamEQ** | Gain | +4 dB | Expose as `CallerNasalBoost` |
| **Distortion** | Distortion | 0.12 | Expose as `CallerDistortion` |

To expose a parameter:
1. Right-click on the parameter name (e.g., "Cutoff freq")
2. Select "Expose [parameter name] to script"
3. In the **Exposed Parameters** list (top-right of mixer window), rename to match the expected name

**Expected Exposed Parameters for CallerGroup:**
- `CallerVolume` (group volume)
- `CallerLowPassCutoff`
- `CallerLowPassResonance` (for old landline phone quality)
- `CallerHighPassCutoff`
- `CallerNasalBoost` (ParamEQ gain at 1800Hz for phone honk)
- `CallerDistortion`

#### Step 3: Static Noise Setup

The static noise is controlled via `StaticNoiseController`, which has its own AudioSource that routes through CallerGroup. The volume is controlled directly by `AudioQualityController.ApplyPhoneLineLevel()` via `StaticNoiseController.SetBaseVolume()` - no mixer parameter needed.

The StaticNoiseController component:
- Creates its own AudioSource that routes through CallerGroup
- Receives phone line filter effects automatically
- Has its base volume adjusted by equipment level (Level 1 = 0.8, Level 4 = 0.05)

#### Step 4: Add Effects to VernGroup

Select **VernGroup** in the mixer hierarchy:

| Effect | Property | Initial Value | Notes |
|--------|----------|---------------|-------|
| **Highpass Simple** | Cutoff freq | 80 Hz | Removes rumble |
| **Compressor** | Threshold | -20 dB | Radio compression |
| **ParamEQ** | Center freq | 3000 Hz | Mid-presence boost |
| **ParamEQ** | Gain | 2 dB | Expose as `VernMidBoost` |
| **Distortion** | Distortion | 0.08 | Expose as `VernDistortion` |

**Expected Exposed Parameters for VernGroup:**
- `VernVolume` (group volume - +12dB to compensate for compressor)
- `VernDistortion`
- `VernMidBoost`

#### Step 5: Verify Exposed Parameters

Open the **Exposed Parameters** panel (click the dropdown in top-right of Audio Mixer window):

| Parameter Name | Expected Range | Description |
|----------------|----------------|-------------|
| `CallerVolume` | 0 to +6 dB | Volume boost for filter compensation |
| `CallerLowPassCutoff` | 2200-10000 Hz | Higher = clearer callers |
| `CallerLowPassResonance` | 1.0-2.5 | Higher = nasal old phone quality |
| `CallerHighPassCutoff` | 100-500 Hz | Lower = fuller sound |
| `CallerNasalBoost` | 0-4 dB | ParamEQ @ 1800Hz, phone honk |
| `CallerDistortion` | 0.0-0.12 | Lower = cleaner |
| `VernVolume` | 0 to +12 dB | Volume boost for compressor compensation |
| `VernDistortion` | 0.0-0.01 | Lower = cleaner |
| `VernMidBoost` | 1-2 dB | Higher = more radio presence |

**Note**: Static volume is controlled via `StaticNoiseController.SetBaseVolume()`, not a mixer parameter. Static only plays while callers are speaking.

#### Step 6: Test with AudioQualityController

1. Run **KBTV > Setup Game Scene** to ensure AudioQualityController is configured
2. Enter Play mode
3. Use the context menu on AudioQualityController:
   - Right-click > "Preview Level 1 (Both)" - Should sound rough/lo-fi
   - Right-click > "Preview Level 4 (Both)" - Should sound clear/professional
4. Check Console for warnings about missing exposed parameters

#### Troubleshooting

| Issue | Solution |
|-------|----------|
| "Could not set mixer parameter 'X'" | Parameter not exposed or wrong name |
| No audio effect change | Check effects are on correct group |
| Distortion too harsh | Reduce distortion value (0.05-0.1) |
| Static too quiet at Level 1 | Adjust `_baseVolume` in StaticNoiseController Inspector |
| Static not playing | Check `phone_static_loop.ogg` is assigned to StaticNoiseController |

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
- [CONVERSATION_DESIGN.md](../ui/CONVERSATION_DESIGN.md) - Dialogue system overview
- [CONVERSATION_ARC_SCHEMA.md](../ui/CONVERSATION_ARC_SCHEMA.md) - Arc JSON structure
- [DEAD_AIR_FILLER.md](../ui/DEAD_AIR_FILLER.md) - Broadcast flow and filler system
