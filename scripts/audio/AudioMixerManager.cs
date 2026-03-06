#nullable enable

using System;
using Godot;
using KBTV.Core;
using KBTV.Persistence;

namespace KBTV.Audio
{
    /// <summary>
    /// Manages audio routing to Vern (studio) or Caller (phone line) with effects.
    /// Creates and configures audio buses programmatically based on equipment level.
    /// </summary>
    [GlobalClass]
    public partial class AudioMixerManager : Node
    {
        private AudioStreamPlayer _vernPlayer = null!;
        private AudioStreamPlayer _callerPlayer = null!;
        private AudioStreamPlayer _musicPlayer = null!;
        private StaticNoiseController _staticController = null!;

        // Bus indices
        private int _masterBusIndex = 0;
        private int _vernBusIndex = -1;
        private int _callerBusIndex = -1;
        private int _staticBusIndex = -1;
        private int _musicBusIndex = -1;
        private int _sfxBusIndex = -1;

        // Effect indices within buses

        // Vern effect indices
        private int _vernReverbIndex = -1;
        private int _vernHighPassIndex = -1;
        private int _vernMuffleLowPassIndex = -1; // Low-pass for muffling when player exits
        private int _vernCompressorIndex = -1;
        private int _vernEqIndex = -1;
        private int _vernDistortionIndex = -1;

        // Caller effect indices
        private int _callerLowPassIndex = -1;
        private int _callerHighPassIndex = -1;
        private int _callerDistortionIndex = -1;
        private int _callerAmplifyIndex = -1;
        private int _callerCompressorIndex = -1;
        private int _callerEqIndex = -1;
        private int _callerChorusIndex = -1;
        private int _callerReverbIndex = -1;
        private int _callerMuffleLowPassIndex = -1; // Additional low-pass for muffling

        // Static effect indices
        private int _staticLowPassIndex = -1;
        private int _staticHighPassIndex = -1;
        private int _staticDistortionIndex = -1;
        private int _staticMuffleLowPassIndex = -1; // Low-pass for muffling when player exits

        // SFX effect indices (for UI sounds and bleep)
        private int _sfxMuffleLowPassIndex = -1;

        // Music effect indices
        private int _musicMuffleLowPassIndex = -1;

        // Current equipment level
        private int _currentPhoneLineLevel = 1;
        private int _currentBroadcastLevel = 1;

        // Effect presets for each equipment level - CALLERS
        // Format: (lowPassHz, highPassHz, distortion, resonance)
        // Balanced phone effect
        private static readonly (float lowPass, float highPass, float distortion, float resonance)[] CallerPresets =
        {
            (600f, 400f, 0.40f, 3.0f),   // Level 1: Bad phone - balanced
            (800f, 350f, 0.35f, 3.0f),   // Level 2: Improved
            (1200f, 300f, 0.30f, 3.0f),   // Level 3: Better
            (2500f, 200f, 0.20f, 3.0f)    // Level 4: Clear
        };

        // Vern broadcast presets - VERN (should be clean)
        private static readonly (float eqGain, float distortion)[] VernPresets =
        {
            (1.0f, 0.0f),   // Level 1: Clean - no distortion
            (1.0f, 0.0f),   // Level 2: Clean
            (1.0f, 0.0f),   // Level 3: Clean
            (1.0f, 0.0f)    // Level 4: Clean - broadcast quality
        };

        public override void _Ready()
        {
            SetupAudioBuses();
            SetupAudioPlayers();
            UpdateAudioQuality();
            GD.Print("AudioMixerManager: Initialized with equipment levels - Phone: " + GetPhoneLineLevel() + ", Broadcast: " + GetBroadcastLevel());
        }

        private void SetupAudioBuses()
        {
            // Create Vern bus
            _vernBusIndex = AudioServer.BusCount;
            AudioServer.AddBus(_vernBusIndex);
            AudioServer.SetBusName(_vernBusIndex, "Vern");
            ConfigureVernBus();
            GD.Print($"AudioMixerManager: Created Vern bus at index {_vernBusIndex}");

            // Create Caller bus
            _callerBusIndex = AudioServer.BusCount;
            AudioServer.AddBus(_callerBusIndex);
            AudioServer.SetBusName(_callerBusIndex, "Caller");
            GD.Print($"AudioMixerManager: Creating Caller bus at index {_callerBusIndex}");
            ConfigureCallerBus();
            GD.Print($"AudioMixerManager: Finished configuring Caller bus");

            // Create Static bus
            _staticBusIndex = AudioServer.BusCount;
            AudioServer.AddBus(_staticBusIndex);
            AudioServer.SetBusName(_staticBusIndex, "Static");
            ConfigureStaticBus();
            GD.Print($"AudioMixerManager: Created Static bus at index {_staticBusIndex}");

            // Create Music bus
            _musicBusIndex = AudioServer.BusCount;
            AudioServer.AddBus(_musicBusIndex);
            AudioServer.SetBusName(_musicBusIndex, "Music");
            ConfigureMusicBus();
            GD.Print($"AudioMixerManager: Created Music bus at index {_musicBusIndex}");

            // Create SFX bus
            _sfxBusIndex = AudioServer.BusCount;
            AudioServer.AddBus(_sfxBusIndex);
            AudioServer.SetBusName(_sfxBusIndex, "SFX");
            ConfigureSFXBus();
            GD.Print($"AudioMixerManager: Created SFX bus at index {_sfxBusIndex}");

            GD.Print($"AudioMixerManager: All buses created - Master: {_masterBusIndex}, Vern: {_vernBusIndex}, Caller: {_callerBusIndex}, Static: {_staticBusIndex}, Music: {_musicBusIndex}, SFX: {_sfxBusIndex}");

            // Verify all buses exist
            for (int i = 0; i < AudioServer.BusCount; i++)
            {
                string name = AudioServer.GetBusName(i);
                int effectCount = AudioServer.GetBusEffectCount(i);
                GD.Print($"Bus[{i}]: '{name}', Effects: {effectCount}");
            }
        }



        private void ConfigureVernBus()
        {
            // Vern should be clean - minimal processing
            // Just a highpass to remove rumble, nothing else

            // Add reverb to Vern bus
            var verReverb = new AudioEffectReverb();
            verReverb.PredelayFeedback = 0.01f;
            verReverb.PredelayMsec = 0.0f;
            verReverb.RoomSize = 0.01f;
            verReverb.Spread = 0.1f;
            verReverb.Hipass = 0f;
            verReverb.Damping = 0.95f;
            verReverb.Dry = 0.99f;
            verReverb.Wet = 0.01f;
            AudioServer.AddBusEffect(_vernBusIndex, verReverb);
            _vernReverbIndex = AudioServer.GetBusEffectCount(_vernBusIndex) - 1;

            var highPass = new AudioEffectHighPassFilter();
            highPass.CutoffHz = 80f;
            AudioServer.AddBusEffect(_vernBusIndex, highPass);
            _vernHighPassIndex = 0;

            // Add a low-pass filter for muffling when player is outside
            var muffleLowPass = new AudioEffectLowPassFilter();
            muffleLowPass.CutoffHz = 20000f; // Initially transparent (very high)
            muffleLowPass.Resonance = 1.0f;
            AudioServer.AddBusEffect(_vernBusIndex, muffleLowPass);
            _vernMuffleLowPassIndex = AudioServer.GetBusEffectCount(_vernBusIndex) - 1;

            // No compressor - makes Vern sound too processed
            // No EQ - keep Vern natural
            // No distortion - keep Vern clean
            _vernCompressorIndex = -1;
            _vernEqIndex = -1;
            _vernDistortionIndex = -1;
        }

        private void ConfigureCallerBus()
        {
            GD.Print($"ConfigureCallerBus: Adding effects to bus index {_callerBusIndex}");

            // Add LowPass filter (index 0) - simulates phone bandwidth
            // Start with Level 1 settings: 600Hz, resonance 3.0
            var lowPass = new AudioEffectLowPassFilter();
            lowPass.CutoffHz = 600f;
            lowPass.Resonance = 3.0f;
            AudioServer.AddBusEffect(_callerBusIndex, lowPass);
            _callerLowPassIndex = 0;
            GD.Print("AudioMixerManager: Added LowPass to Caller bus");

            // Add HighPass filter (index 1) - removes low frequencies
            var highPass = new AudioEffectHighPassFilter();
            highPass.CutoffHz = 400f;  // Level 1: 400Hz
            AudioServer.AddBusEffect(_callerBusIndex, highPass);
            _callerHighPassIndex = 1;
            GD.Print("AudioMixerManager: Added HighPass to Caller bus");

            // Add Distortion (index 2) - phone line character
            var distortion = new AudioEffectDistortion();
            distortion.Mode = AudioEffectDistortion.ModeEnum.Overdrive;
            distortion.PreGain = 1f;
            distortion.Drive = 0.40f;  // Level 1: 0.40
            AudioServer.AddBusEffect(_callerBusIndex, distortion);
            _callerDistortionIndex = 2;
            GD.Print("AudioMixerManager: Added Distortion to Caller bus");

            // Add Amplify (index 3) - boost caller voice above static
            var amplify = new AudioEffectAmplify();
            amplify.VolumeDb = 8f;  // Boost by 8dB
            AudioServer.AddBusEffect(_callerBusIndex, amplify);
            _callerAmplifyIndex = 3;
            GD.Print("AudioMixerManager: Added Amplify to Caller bus");

            // Add EQ (index 4) - subtle telephone presence at 1-2kHz
            var eq = new AudioEffectEQ();
            eq.SetBandGainDb(3, 2f);   // ~1kHz - subtle boost
            eq.SetBandGainDb(4, 2f);   // ~2kHz - subtle boost
            AudioServer.AddBusEffect(_callerBusIndex, eq);
            _callerEqIndex = 4;
            GD.Print("AudioMixerManager: Added EQ to Caller bus");

            // Add reverb to caller bus
            var callReverb = new AudioEffectReverb();
            callReverb.PredelayFeedback = 0.05f;
            callReverb.PredelayMsec = 0.0f;
            callReverb.RoomSize = 0.03f;
            callReverb.Spread = 0.2f;
            callReverb.Hipass = 0f;
            callReverb.Damping = 0.9f;
            callReverb.Dry = 0.95f;
            callReverb.Wet = 0.05f;
            AudioServer.AddBusEffect(_callerBusIndex, callReverb);
            _callerReverbIndex = AudioServer.GetBusEffectCount(_callerBusIndex) - 1;

            // Add a low-pass filter for muffling when player is outside
            var muffleLowPass = new AudioEffectLowPassFilter();
            muffleLowPass.CutoffHz = 20000f; // Initially transparent (very high)
            muffleLowPass.Resonance = 1.0f;
            AudioServer.AddBusEffect(_callerBusIndex, muffleLowPass);
            _callerMuffleLowPassIndex = AudioServer.GetBusEffectCount(_callerBusIndex) - 1;

            GD.Print("AudioMixerManager: Added Chorus to Caller bus");
        }

        private void ConfigureMusicBus()
        {
            // Add Compressor for consistent music volume
            var compressor = new AudioEffectCompressor();
            compressor.Threshold = -18f;
            compressor.Ratio = 2f;
            compressor.AttackUs = 10f;
            compressor.ReleaseMs = 200f;
            AudioServer.AddBusEffect(_musicBusIndex, compressor);

            // Add EQ for music enhancement
            var eq = new AudioEffectEQ();
            // Slight bass boost, slight treble cut for warmth
            eq.SetBandGainDb(0, 1f);   // Bass
            eq.SetBandGainDb(1, 0.5f);
            eq.SetBandGainDb(2, 0f);
            eq.SetBandGainDb(3, -0.5f);  // Slight treble cut
            eq.SetBandGainDb(4, -1f);    // More treble cut
            AudioServer.AddBusEffect(_musicBusIndex, eq);

            // Add a low-pass filter for muffling when player is outside
            var muffleLowPass = new AudioEffectLowPassFilter();
            muffleLowPass.CutoffHz = 20000f; // Initially transparent (very high)
            muffleLowPass.Resonance = 1.0f;
            AudioServer.AddBusEffect(_musicBusIndex, muffleLowPass);
            _musicMuffleLowPassIndex = AudioServer.GetBusEffectCount(_musicBusIndex) - 1;
        }

        /// <summary>
        /// Sets the bus used for broadcast audio. The BroadcastAudioService will
        /// route its players to this bus.
        /// </summary>


        private void ConfigureStaticBus()
        {
            // Add LowPass filter (index 0) - wider than caller to keep static audible
            var lowPass = new AudioEffectLowPassFilter();
            lowPass.CutoffHz = 3000f;  // Much wider than caller (600Hz)
            lowPass.Resonance = 2.0f;   // Lower resonance to avoid spikes
            AudioServer.AddBusEffect(_staticBusIndex, lowPass);
            _staticLowPassIndex = 0;

            // Add HighPass filter (index 1) - remove rumble but keep body
            var highPass = new AudioEffectHighPassFilter();
            highPass.CutoffHz = 150f;   // Lower than caller (400Hz)
            AudioServer.AddBusEffect(_staticBusIndex, highPass);
            _staticHighPassIndex = 1;

            // Add light Distortion (index 2) - subtle phone line character
            var distortion = new AudioEffectDistortion();
            distortion.Mode = AudioEffectDistortion.ModeEnum.Overdrive;
            distortion.PreGain = 0.8f;
            distortion.Drive = 0.15f;   // Light distortion
            AudioServer.AddBusEffect(_staticBusIndex, distortion);
            _staticDistortionIndex = 2;

            // Add a low-pass filter for muffling when player is outside
            var muffleLowPass = new AudioEffectLowPassFilter();
            muffleLowPass.CutoffHz = 20000f; // Initially transparent (very high)
            muffleLowPass.Resonance = 1.0f;
            AudioServer.AddBusEffect(_staticBusIndex, muffleLowPass);
            _staticMuffleLowPassIndex = AudioServer.GetBusEffectCount(_staticBusIndex) - 1;
        }

        private void ConfigureSFXBus()
        {
            // SFX bus handles UI sounds and bleep effects
            // Add a low-pass filter for muffling when player is outside
            var muffleLowPass = new AudioEffectLowPassFilter();
            muffleLowPass.CutoffHz = 20000f; // Initially transparent (very high)
            muffleLowPass.Resonance = 1.0f;
            AudioServer.AddBusEffect(_sfxBusIndex, muffleLowPass);
            _sfxMuffleLowPassIndex = AudioServer.GetBusEffectCount(_sfxBusIndex) - 1;
        }

        private void SetupAudioPlayers()
        {
            _vernPlayer = new AudioStreamPlayer();
            _vernPlayer.Name = "VernPlayer";
            AddChild(_vernPlayer);

            _callerPlayer = new AudioStreamPlayer();
            _callerPlayer.Name = "CallerPlayer";
            AddChild(_callerPlayer);

            _musicPlayer = new AudioStreamPlayer();
            _musicPlayer.Name = "MusicPlayer";
            _musicPlayer.Bus = AudioServer.GetBusName(_musicBusIndex);
            AddChild(_musicPlayer);

            _staticController = new StaticNoiseController();
            AddChild(_staticController);

            // Route players to their respective buses
            if (_vernBusIndex >= 0)
            {
                _vernPlayer.Bus = AudioServer.GetBusName(_vernBusIndex);
            }
            if (_callerBusIndex >= 0)
            {
                _callerPlayer.Bus = AudioServer.GetBusName(_callerBusIndex);
            }
        }

        /// <summary>
        /// Gets the current phone line equipment level (1-4).
        /// </summary>
        public int GetPhoneLineLevel()
        {
            var saveManager = ServiceRegistry.Instance?.Get<SaveManager>();
            if (saveManager?.CurrentSave?.EquipmentLevels != null)
            {
                if (saveManager.CurrentSave.EquipmentLevels.TryGetValue("PhoneLine", out int level))
                {
                    return Mathf.Clamp(level, 1, 4);
                }
            }
            return 1;
        }

        /// <summary>
        /// Gets the current broadcast equipment level (1-4).
        /// </summary>
        public int GetBroadcastLevel()
        {
            var saveManager = ServiceRegistry.Instance?.Get<SaveManager>();
            if (saveManager?.CurrentSave?.EquipmentLevels != null)
            {
                if (saveManager.CurrentSave.EquipmentLevels.TryGetValue("Broadcast", out int level))
                {
                    return Mathf.Clamp(level, 1, 4);
                }
            }
            return 1;
        }

        /// <summary>
        /// Updates audio quality based on equipment levels.
        /// </summary>
        public void UpdateAudioQuality()
        {
            _currentPhoneLineLevel = GetPhoneLineLevel();
            _currentBroadcastLevel = GetBroadcastLevel();

            ApplyCallerEffects(_currentPhoneLineLevel);
            ApplyVernEffects(_currentBroadcastLevel);
            // Static volume is now controlled by caller's PhoneQuality, not equipment level

            GD.Print($"AudioMixerManager: Updated quality - PhoneLine: {_currentPhoneLineLevel}, Broadcast: {_currentBroadcastLevel}");
        }

        private void ApplyCallerEffects(int level)
        {
            if (_callerBusIndex < 0) return;

            int presetIndex = Mathf.Clamp(level - 1, 0, CallerPresets.Length - 1);
            var preset = CallerPresets[presetIndex];

            // Update LowPass filter
            if (_callerLowPassIndex >= 0)
            {
                var effect = AudioServer.GetBusEffect(_callerBusIndex, _callerLowPassIndex);
                if (effect is AudioEffectLowPassFilter lowPass)
                {
                    lowPass.CutoffHz = preset.lowPass;
                    lowPass.Resonance = preset.resonance;
                }
            }

            // Update HighPass filter
            if (_callerHighPassIndex >= 0)
            {
                var effect = AudioServer.GetBusEffect(_callerBusIndex, _callerHighPassIndex);
                if (effect is AudioEffectHighPassFilter highPass)
                {
                    highPass.CutoffHz = preset.highPass;
                }
            }

            // Update reverb wet mix based on phone line level
            if (_callerReverbIndex >= 0)
            {
                var effect = AudioServer.GetBusEffect(_callerBusIndex, _callerReverbIndex);
                if (effect is AudioEffectReverb rev)
                {
                    rev.Wet = Math.Clamp(0.08f + (level - 1) * 0.02f, 0.08f, 0.15f);
                }
            }
        }

        private void ApplyVernEffects(int level)
        {
            if (_vernBusIndex < 0) return;

            // Vern is always clean - no distortion, minimal EQ
            // Update reverb wet mix based on broadcast level
            if (_vernReverbIndex >= 0)
            {
                var effect = AudioServer.GetBusEffect(_vernBusIndex, _vernReverbIndex);
                if (effect is AudioEffectReverb rev)
                {
                    // increase wet mix with level
                    rev.Wet = Math.Clamp(0.1f + (level - 1) * 0.05f, 0.1f, 0.3f);
                }
            }

            // No distortion for Vern - disabled
        }

        /// <summary>
        /// Gets the Vern audio player for direct playback.
        /// </summary>
        public AudioStreamPlayer GetVernPlayer() => _vernPlayer;

        /// <summary>
        /// Gets the Caller audio player for direct playback.
        /// </summary>
        public AudioStreamPlayer GetCallerPlayer() => _callerPlayer;

        /// <summary>
        /// Plays audio through the Vern player (studio quality).
        /// </summary>
        public void PlayVern(AudioStream stream)
        {
            if (_vernPlayer == null) return;

            _vernPlayer.Stream = stream;
            _vernPlayer.Play();
        }

        /// <summary>
        /// Plays audio through the Caller player (phone line quality with static).
        /// </summary>
        public void PlayCaller(AudioStream stream)
        {
            if (_callerPlayer == null) return;

            _callerPlayer.Stream = stream;
            _callerPlayer.Play();

            // Start static when caller is speaking
            _staticController?.StartStatic();
        }



        /// <summary>
        /// Stops Caller playback and static.
        /// </summary>
        public void StopCaller()
        {
            _callerPlayer?.Stop();
            _staticController?.StopStatic();
        }

        /// <summary>
        /// Gets the Vern audio player for direct playback.
        /// </summary>
        public AudioStreamPlayer GetMusicPlayer() => _musicPlayer;

        /// <summary>
        /// Plays audio through the Music player (background music).
        /// </summary>
        public void PlayMusic(AudioStream stream, bool loop = true)
        {
            if (_musicPlayer == null) return;

            _musicPlayer.Stream = stream;
            _musicPlayer.Play();
        }



        /// <summary>
        /// Checks if Vern is currently playing.
        /// </summary>
        public bool IsVernPlaying => _vernPlayer?.Playing ?? false;

        /// <summary>
        /// Checks if Caller is currently playing.
        /// </summary>
        public bool IsCallerPlaying => _callerPlayer?.Playing ?? false;

        /// <summary>
        /// Checks if Music is currently playing.
        /// </summary>
        public bool IsMusicPlaying => _musicPlayer?.Playing ?? false;

        /// <summary>
        /// Gets the StaticNoiseController for external control.
        /// </summary>
        public StaticNoiseController GetStaticController() => _staticController;

        /// <summary>
        /// Gets a description of the current audio quality.
        /// </summary>
        public string GetQualityDescription()
        {
            string phoneQuality = _currentPhoneLineLevel switch
            {
                1 => "Poor - Heavy static, narrow band",
                2 => "Fair - Some static, wider band",
                3 => "Good - Minimal static, clear",
                4 => "Excellent - Crystal clear",
                _ => "Unknown"
            };

            string broadcastQuality = _currentBroadcastLevel switch
            {
                1 => "Muffled, some hum",
                2 => "Clearer",
                3 => "Professional",
                4 => "Broadcast-quality",
                _ => "Unknown"
            };

            return $"Phone: {phoneQuality}\nBroadcast: {broadcastQuality}";
        }

        /// <summary>
        /// Sets the muffled state for broadcast audio. When outside the control room,
        /// a low-pass filter is applied to Vern, Caller, Static, Music, and SFX buses
        /// to simulate hearing the broadcast from another room.
        /// </summary>
        public void SetMuffled(bool outside)
        {
            float cutoff = outside ? 200f : 20000f;

            // Update Vern muffle filter
            if (_vernMuffleLowPassIndex >= 0 && _vernBusIndex >= 0)
            {
                var effect = AudioServer.GetBusEffect(_vernBusIndex, _vernMuffleLowPassIndex);
                if (effect is AudioEffectLowPassFilter lowPass)
                {
                    lowPass.CutoffHz = cutoff;
                }
            }

            // Update Caller muffle filter
            if (_callerMuffleLowPassIndex >= 0 && _callerBusIndex >= 0)
            {
                var effect = AudioServer.GetBusEffect(_callerBusIndex, _callerMuffleLowPassIndex);
                if (effect is AudioEffectLowPassFilter lowPass)
                {
                    lowPass.CutoffHz = cutoff;
                }
            }

            // Update Static muffle filter
            if (_staticMuffleLowPassIndex >= 0 && _staticBusIndex >= 0)
            {
                var effect = AudioServer.GetBusEffect(_staticBusIndex, _staticMuffleLowPassIndex);
                if (effect is AudioEffectLowPassFilter lowPass)
                {
                    lowPass.CutoffHz = cutoff;
                }
            }

            // Update Music muffle filter
            if (_musicMuffleLowPassIndex >= 0 && _musicBusIndex >= 0)
            {
                var effect = AudioServer.GetBusEffect(_musicBusIndex, _musicMuffleLowPassIndex);
                if (effect is AudioEffectLowPassFilter lowPass)
                {
                    lowPass.CutoffHz = cutoff;
                }
            }

            // Update SFX muffle filter
            if (_sfxMuffleLowPassIndex >= 0 && _sfxBusIndex >= 0)
            {
                var effect = AudioServer.GetBusEffect(_sfxBusIndex, _sfxMuffleLowPassIndex);
                if (effect is AudioEffectLowPassFilter lowPass)
                {
                    lowPass.CutoffHz = cutoff;
                }
            }

            GD.Print($"AudioMixerManager: SetMuffled({outside}) - cutoff = {cutoff}Hz");
        }

    }
}