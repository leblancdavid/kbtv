# KBTV - Voice & Audio Production Plan

## Overview

This document outlines the strategy for producing voice audio for KBTV's dialogue system, including Vern's broadcasts, caller conversations, and filler content.

**Approach**: Pre-generated audio using ElevenLabs API (high-quality AI voices) with runtime effects applied via Godot audio bus system.

## Audio Scope

### Content Volume

| Category | Description | Current Files |
|----------|-------------|---------------|
| **Vern Conversation Arcs** | 73 arcs x vern turns x 13 mood variants | ~2900 vern arc files |
| **Caller Conversation Arcs** | 73 arcs, one caller file per caller turn | ~360 caller files |
| **Vern Broadcast Audio** | Openings/closings/fillers/breaks etc. from `assets/dialogue/vern/*.json` | ~550 files |

Audio is regenerated locally from the dialogue JSON (mp3s are gitignored);
run `python generate_arc_audio.py --all --check` to see current coverage.

### Character Voices Needed

| Character | Description | Voice Style |
|-----------|-------------|-------------|
| **Vern Tell** | Skeptical radio host | Gravelly, late-night radio DJ, dry wit |
| **Callers** | Various paranormal witnesses | Diverse voices, filtered through phone effect |

## Production Approach

### Chosen: Pre-Generated with ElevenLabs API

All audio is generated using ElevenLabs' professional-grade AI voice synthesis with custom voice cloning for Vern, then imported into Godot as AudioStream resources.

**Why ElevenLabs?**
- Professional broadcast-quality voices
- Extensive voice library with personality diversity
- Emotion and style control via API parameters
- Vern voice cloned from Art Bell reference audio (authentic gravelly radio host)
- Superior quality to offline TTS engines
- Built-in voice archetypes for diverse caller voices

**Why Pre-Generated?**
- Simpler Godot integration (just load AudioStream resources)
- No runtime API dependencies
- Consistent audio quality across all lines
- Can fine-tune individual lines for natural delivery
- Cost-effective (generate once, use forever)

### Generation Workflow

```
1. Dialogue JSON files (conversation arcs + broadcast content)
        ↓
2. ElevenLabs voice setup (clone Vern from Art Bell audio)
        ↓
3. Python generation script (generate_arc_audio.py)
        ├── Speaker filtering (--speaker vern|caller|both)
        ├── Mood-based voice parameters for Vern
        └── Voice archetypes for callers
        ↓
4. Smart file skipping (--force to regenerate existing)
        ↓
5. Automatic organization by speaker type:
        ├── Vern/ConversationArcs/{topic}/{arc_id}/
        └── Callers/{topic}/{arc_id}/
        ↓
6. Import to Godot project
        ↓
7. Godot audio bus system applies runtime effects

**Note**: Audio effects (phone filter, radio compression, static) are applied at runtime in Godot, not baked into the files. This enables the equipment upgrade system to dynamically improve audio quality.

## Voice Profiles

### Vern Tell

| Attribute | Value |
|-----------|-------|
| **Voice Source** | ElevenLabs voice clone from Art Bell reference audio |
| **Voice ID** | Custom cloned voice (cD12ZqbaUeADFL4RycQC) |
| **Style** | Late-night radio host, Art Bell inspired |
| **Delivery** | Measured, occasionally sardonic, professional |

**Mood Variations (all 13 `VernMoodType` values):**

Delivery parameters live in `MOOD_SETTINGS`
(`Tools/AudioGeneration/generate_vern_audio.py`) and apply to both broadcast
lines and arc lines. The seven originals:

| VernMoodType | DialogueTone | Notes |
|--------------|--------------|-------|
| Tired | Dismissive | Slower, trailing off, low energy |
| Energized | Excited | Fast, enthusiastic, engaged |
| Irritated | Annoyed | Short, clipped, impatient |
| Amused | Believing | Warm, playful, amused |
| Gruff | Dismissive | Brusk, direct, no-nonsense |
| Focused | Skeptical | Analytical, probing, questioning |
| Neutral | Neutral | Professional, balanced delivery |

Added with VIBE/stats expansion: Exhausted, Depressed, Angry, Frustrated,
Obsessive, Manic (`scripts/data/VernMoodType.cs`).

### Caller Voice Pools

Current implementation uses gender pools defined in
`Tools/AudioGeneration/elevenlabs_setup.py`: 13 male + 6 female ElevenLabs
voices. Each arc is hashed from its `arcId` to one pool voice, so all caller
lines in an arc share one voice while different arcs differ. Voice-settings
overrides in the same file make some voices read older/southern.

All caller audio receives phone filter via Godot Audio Buses at runtime.

## Runtime Audio Effects (Godot Audio Buses)

Audio effects are applied at runtime via Godot's Audio Bus system. This allows the equipment upgrade system to dynamically improve audio quality as the player progresses.

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
| **Source** | ElevenLabs API (`eleven_flash_v2`) |
| **Format** | MP3 |
| **Channels** | Mono |

### Effects Chain

Effects are applied at runtime via Godot Audio Buses (see "Runtime Audio Effects" section above).

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
| **requests** | ElevenLabs API calls | `pip install requests` |

### Generation Scripts

```
Tools/AudioGeneration/
├── generate_arc_audio.py    # Per-arc audio from assets/dialogue/arcs/**/*.json (JSON-driven, --check/--all)
├── generate_vern_audio.py   # Broadcast audio from assets/dialogue/vern/*.json
├── generate_break_audio.py  # Break transition lines
├── elevenlabs_setup.py      # Vern clone + caller voice pools (pools defined here)
├── elevenlabs_config.json   # API key (gitignored)
└── voice_id.txt             # Vern clone ID (gitignored)
```

The Piper-era tooling described in earlier revisions of this doc has been
replaced entirely by the ElevenLabs scripts above.
See [AUDIO_GENERATION.md](../tools/AUDIO_GENERATION.md) for usage.

## File Organization

```
assets/audio/voice/
├── Vern/
│   ├── Broadcast/                  # {line id from assets/dialogue/vern/*.json}.mp3
│   │   └── opening_ufos_1.mp3, betweencallers_neutral_2.mp3, ...
│   └── ConversationArcs/
│       └── {topicToken}/{arcId}/   # topicToken from the line id prefix
│           └── {line id}.mp3
└── Callers/
    └── {topicToken}/{arcId}/
        └── {line id}.mp3
```

## Naming Convention

Filenames are the **line `id` fields from the arc JSON verbatim** plus `.mp3`.
Line ids encode topic token, legitimacy, descriptor, speaker, mood, and turn
sequence - see [CONVERSATION_ARC_SCHEMA.md](../ui/CONVERSATION_ARC_SCHEMA.md)
for the authoritative pattern, mood list, and folder routing rules (the audio
folder topic comes from the **line id prefix**, which is what allows
topic-switcher arcs to resolve consistently between generator and runtime).

Broadcast lines use their `id` field directly, e.g. `opening_ufos_1.mp3`,
`betweencallers_neutral_2.mp3`.

(The 7-tone/mood-folder/belief-tag naming scheme described in earlier
revisions of this document predates the current 13-mood schema and no longer
applies. There are ~100 leftover files in `Vern/Broadcast/` from that era -
`opening_neutral_*`, `closing_amused_*`, `deadair_*`, `return_neutral_*` -
which are unreferenced by any JSON and safe to delete.)

## Implementation Plan

### Phase 1: Setup & Prototype
- [x] Install Piper TTS and dependencies
- [x] Test voice models, select best for Vern and callers
- [x] Create basic generation script (normalize only, no effects)
- [x] Integrate automatic voice model downloading
- [x] Generate all 950 voice lines (Vern broadcasts + caller conversations)
- [x] Validate audio quality and timing

### Phase 2: Audio Integration (Godot Migration Pending)

**Note**: Audio loading uses Godot's ResourceLoader system.

- [x] Create VoiceAudioService for clip loading/caching via ResourceLoader
- [x] Add Id field to DialogueTemplate for broadcast line matching
- [x] Add IDs to VernDialogue.json broadcast entries
- [x] Update AudioManager with speaker-based mixer routing
- [x] Update ConversationManager to trigger voice playback
- [x] Replace Addressables with Godot ResourceLoader paths (see above)
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

## Godot Integration Architecture

### Overview

Voice audio is loaded per line at playback time. The flow is:

```
DialogueExecutable (arc)  BroadcastStateMachine (single lines)
        │                         │
        │ build res:// path       │ resolve id -> res://assets/audio/voice/
        │ via mood + line id      │   Vern/Broadcast/{id}.mp3
        ▼                         ▼
   BroadcastAudioService.PlayAudioAsync(path)  ->  AudioStreamPlayer on
   Vern/Caller bus (speaker detected from path)
```

`DialogueExecutable` reads `VernStats.CurrentMoodType`, picks the mood
variant of each line (`ArcDialogueLine.GetAudioIdForMood`/`GetTextForMood`,
neutral fallback), and resolves the folder from the line-id topic token via
`ArcAudioTopics.GetTopicFolder` - identical rules to the generator.

### Clip Loading

- Conversation clips: path built per line as described above (no preload
  layer); Godot's `ResourceLoader` caches each `res://` path after first load.
- Broadcast clips: loaded by id from the `assets/dialogue/vern/*.json` files.
- The legacy Unity-era `VoiceAudioService` / Addressables abstraction was
  replaced by `BroadcastAudioService` during the Godot migration; the
  belief-branch address scheme predates the current schema and no longer
  applies.

### Missing Clip Handling

If a clip is not found:
- **Development**: Log warning with expected address
- **Runtime**: Continue silently, use default typewriter speed (40 chars/sec)
- **No blocking**: Game continues without audio for that line

### Memory Management

With ~4000 clips total, loading everything at once is impossible:

1. **Load per line**: each line's clip is `GD.Load`ed when playback reaches it
2. **Godot resource cache**: keeps loaded streams in memory per session
3. **Future option**: LRU eviction or `Resource.Unload` per conversation if
   memory becomes a constraint (not currently an issue for show-length runs)

### Godot ResourceLoader Configuration

The voice audio system uses Godot's ResourceLoader for efficient async loading and caching.

#### Audio File Organization

Voice audio is loaded dynamically via `GD.Load<AudioStream>()`:

```csharp
// Broadcast audio
var broadcastStream = GD.Load<AudioStream>($"res://assets/audio/voice/Vern/Broadcast/{line.Id}.mp3");

// Arc conversation audio
var arcStream = GD.Load<AudioStream>($"res://assets/audio/voice/Vern/ConversationArcs/{topic}/{line.Id}.mp3");

// Caller audio
var callerStream = GD.Load<AudioStream>($"res://assets/audio/voice/Callers/{topic}/{arcId}_{gender}_{lineIndex}.mp3");
```

#### Import Settings

All voice MP3 files are imported with:
- **Load Type**: Decompress on Load (for short dialogue clips)
- **Sample Rate**: 44.1 kHz
- **Format**: Mono (for dialogue efficiency)
- **Compression**: OGG Vorbis with quality setting

#### Runtime Loading

- **Broadcast clips**: Load on-demand, cache per session
- **Arc clips**: Preload conversation audio when entering dialogue
- **Caller clips**: Load dynamically during calls
- **Memory management**: Release cached clips when conversations end

Note: In Godot Editor, resources load directly from the project.

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

### Phase 3: Audio Bus Effects Setup (Godot Editor)

Follow these steps to add audio effects that respond to equipment upgrades.

#### Step 1: Open the Audio Mixer

1. In Godot, go to **Audio > Audio Buses**
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

The file naming and Godot integration will remain the same regardless of voice source.

## References

- [Piper TTS (OHF-Voice)](https://github.com/OHF-Voice/piper1-gpl) - Voice synthesis tool
- [Piper Voice Models](https://github.com/rhasspy/piper/blob/master/VOICES.md) - Available voices
- [AUDIO_DESIGN.md](AUDIO_DESIGN.md) - Overall audio direction
- [CONVERSATION_DESIGN.md](../ui/CONVERSATION_DESIGN.md) - Dialogue system overview
- [CONVERSATION_ARC_SCHEMA.md](../ui/CONVERSATION_ARC_SCHEMA.md) - Arc JSON structure
- [DEAD_AIR_FILLER.md](../ui/DEAD_AIR_FILLER.md) - Broadcast flow and filler system
