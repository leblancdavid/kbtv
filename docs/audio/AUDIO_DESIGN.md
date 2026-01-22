# KBTV - Audio Design Document

## Audio Direction

KBTV's audio aesthetic evokes late-night AM radio from the 1990s-2000s, with a paranormal/conspiracy theme. Think Coast to Coast AM meets a local access station.

**Key Audio Characteristics:**
- Warm, slightly compressed radio sound
- Analog-style processing (subtle tape hiss, tube warmth)
- Synthesizer/theremin elements for paranormal theming
- Professional broadcast quality with indie charm

## Station Bumpers

Station bumpers are short audio clips that brand the station and create professional radio flow.

### Bumper Types

| Type | Duration | When Played | Purpose |
|------|----------|-------------|---------|
| **Intro Bumper** | 8-15 sec | Show opening (before Vern speaks) | Full station ID with music bed, call letters, tagline |
| **Return Bumper** | 3-5 sec | After ad breaks | Quick station ID sting to re-establish brand |

### Intro Bumper Content Ideas

Full station IDs with music bed and voice:
- "You're listening to KBTV... Beyond the Veil AM. Broadcasting live from the high desert, where the truth is out there... and we're bringing it to you."
- "KBTV, Beyond the Veil. Late night radio for those who question everything."
- "This is KBTV... where the signal reaches beyond the veil."

### Return Bumper Content Ideas

Quick stings (3-5 seconds):
- Musical sting + "KBTV... we're back."
- Synth sweep + "Beyond the Veil"
- Just a distinctive musical signature

### Audio File Locations

```
res://assets/audio/
  bumpers/            # Station branding audio
    Intro/            # Show opening bumpers (8-15 sec)
      intro_01_v1.ogg
      intro_02_v1.ogg
      intro_03_v1.ogg
    Return/           # Ad return bumpers (3-5 sec)
      return_*.ogg    # (Not yet implemented)
  music/              # Background and transitional music
    intro_music.wav   # 4s intro bumper placeholder
  voice/              # AI-generated voice files
    Vern/             # Host dialogue (109 files)
      ConversationArcs/  # Vern responses in conversations
      MainBroadcast/     # Show openings/closings
      Transitions/       # Between-caller banter
    Callers/         # Caller dialogue (83 files)
      Conspiracies/  # Conspiracy caller audio
      Cryptids/      # Cryptid caller audio
      Ghosts/        # Ghost caller audio
      UFOs/          # UFO caller audio
  ads/               # Commercial jingles
    {ad_id}/         # Per-ad directories (area_51_tours, etc.)
```

### Configuration

Audio is managed through Godot's resource system and C# scripts.

**AudioManager.cs** handles:
- Bumper playback with random selection
- Music transitions
- Voice line loading via res:// paths

**Current Implementation:**
- Intro bumpers: 3 versions in `res://assets/audio/bumpers/Intro/`
- Ad audio: Multiple sponsors in `res://assets/audio/ads/`
- Voice audio: AI-generated files loaded dynamically

**Future Enhancements:**
- Return bumpers for ad break transitions
- Dynamic audio quality based on equipment upgrades
- Music beds for different show segments

### Technical Implementation

Audio is managed through Godot's AudioStream system:

- **AudioManager.cs**: Handles bumper playback and random selection
- **AudioDialoguePlayer.cs**: Loads and plays voice audio via `GD.Load<AudioStream>()`
- **BroadcastCoordinator.cs**: Manages show flow with audio timing

**Show Opening Flow:**
```
LiveShow starts
  -> PlayIntroBumper() (4s music)
  -> Wait for bumper completion
  -> PlayShowOpening() (Vern's intro line)
  -> Check for callers or start dead air filler
```

**Ad Break Flow:**
```
Ad break triggered
  -> Play ad slots sequentially (4s each)
  -> Resume show (no return bumper yet)
```

**Voice Loading:**
```csharp
var path = $"res://assets/audio/voice/Callers/{topic}/{filename}.mp3";
var audioStream = GD.Load<AudioStream>(path);
```
```

## Music

### Intro Music Bumper

4-second music bumper that plays at show start before Vern's opening dialogue:
- Placeholder 44.1kHz, 16-bit mono WAV file
- Transcript shows "MUSIC" during playback
- Sets up infrastructure for ad break and outro music
- Future expansion ready for real audio content

**Location:** `Assets/Audio/Music/intro_music.wav`
**Integration:** BroadcastCoordinator handles playback timing and state transitions

### Break Transition Music

Music that plays when a break is queued, cueing Vern that ads are coming:
- Fades in when player clicks "Queue Ads"
- Plays under Vern's transition dialogue
- Fades out when break starts

**Location:** `Assets/Audio/Music/break_transition_*.ogg`
**Config:** `TransitionMusicConfig.asset`

### Background/Ambient

Future: Background ambience for control room, studio atmosphere.

## Sound Effects

### SFX Types (AudioManager)

| SFX Type | Description |
|----------|-------------|
| ShowStart | Show begins |
| ShowEnd | Show ends |
| CallerIncoming | New caller in queue |
| CallerApproved | Caller put on hold |
| CallerRejected | Caller rejected |
| CallerOnAir | Caller goes live |
| CallerComplete | Call ends successfully |
| CallerDisconnect | Caller hangs up |
| ButtonClick | UI button press |
| ItemUsed | Item consumed |
| ItemEmpty | No stock |
| LowStat | Critical stat warning |
| HighListeners | Peak listeners reached |
| PhoneRing | Phone ringing loop |
| StaticBurst | Radio static |
| BreakJingle | Going to ad break |
| ReturnJingle | (Legacy) Returning from break |
| DialogueType | Typewriter blip |
| DialogueSpeakerChange | Speaker change |

### Static Noise

Phone line static controlled by `StaticNoiseController`:
- Plays while caller is speaking
- Intensity based on caller's phone quality
- Stops when Vern speaks or call ends

## Voice/Radio

### Vern's Voice

Pre-generated TTS or recorded voice files for:
- Show opening/closing lines
- Dead air filler monologues
- Break transition lines
- Conversation responses

**Location:** `Assets/Audio/Voice/Vern/`

### Caller Voices

Pre-generated or procedural caller voices:
- Various phone quality levels (Good, Average, Poor, Terrible)
- Processed through CallerMixerGroup with phone filter

### Audio Mixer Groups

| Group | Purpose |
|-------|---------|
| Master | Overall volume |
| VernMixerGroup | Vern's voice (clean broadcast quality) |
| CallerMixerGroup | Caller voices (phone filter, static) |

## Ad Audio

Ad jingles for each sponsor:
- 15-20 second themed jingles
- Baked-in sponsor branding
- Multiple variations per ad for variety

**Location:** `Assets/Audio/Ads/{ad_id}/`
**Generation:** See `Tools/AdGeneration/` for Suno jingle workflow

## Technical Requirements

### Audio Formats

| Type | Format | Sample Rate | Notes |
|------|--------|-------------|-------|
| SFX | OGG Vorbis | 44.1 kHz | Short clips, no streaming |
| Music | OGG Vorbis | 44.1 kHz | Streaming for longer tracks |
| Voice | OGG Vorbis | 44.1 kHz | Mono for dialogue |
| Bumpers | OGG Vorbis | 44.1 kHz | Stereo for music bed |

### Volume Guidelines

| Category | Default Volume |
|----------|----------------|
| Master | 1.0 |
| SFX | 1.0 |
| Music | 0.5 |
| Ambience | 0.3 |
| Voice | 1.0 |
| Transition Music | 0.4 |

### Compression

- Use Godot's default OGG Vorbis compression
- Quality: 70% for SFX, 50% for music/voice
- Load Type: Decompress on Load for short SFX, Streaming for music
