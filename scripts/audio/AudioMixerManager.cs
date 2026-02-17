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
        private int _musicBusIndex = -1;

        // Effect indices within buses
        private int _callerLowPassIndex = -1;
        private int _callerHighPassIndex = -1;
        private int _callerDistortionIndex = -1;
        private int _vernHighPassIndex = -1;
        private int _vernCompressorIndex = -1;
        private int _vernEqIndex = -1;
        private int _vernDistortionIndex = -1;

        // Current equipment level
        private int _currentPhoneLineLevel = 1;
        private int _currentBroadcastLevel = 1;

        // Effect presets for each equipment level - CALLERS
        // Format: (lowPassHz, highPassHz, distortion, resonance)
        // Bandpass effect: aggressive lowpass + highpass = old phone sound
        private static readonly (float lowPass, float highPass, float distortion, float resonance)[] CallerPresets = 
        {
            (800f, 400f, 0.30f, 4.0f),   // Level 1: Very bad phone - bandpass 400-800Hz, harsh distortion
            (1500f, 350f, 0.20f, 3.5f),   // Level 2: Bad phone - bandpass 350-1500Hz
            (3000f, 200f, 0.10f, 2.5f),   // Level 3: Decent phone - bandpass 200-3000Hz
            (10000f, 100f, 0.0f, 1.0f)    // Level 4: Clear phone - almost full range
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
        }

        private void SetupAudioBuses()
        {
            // Create Vern bus
            _vernBusIndex = AudioServer.BusCount;
            AudioServer.AddBus(_vernBusIndex);
            AudioServer.SetBusName(_vernBusIndex, "Vern");
            ConfigureVernBus();

            // Create Caller bus
            _callerBusIndex = AudioServer.BusCount;
            AudioServer.AddBus(_callerBusIndex);
            AudioServer.SetBusName(_callerBusIndex, "Caller");
            ConfigureCallerBus();

            // Create Music bus
            _musicBusIndex = AudioServer.BusCount;
            AudioServer.AddBus(_musicBusIndex);
            AudioServer.SetBusName(_musicBusIndex, "Music");
            ConfigureMusicBus();

            GD.Print($"AudioMixerManager: Created buses - Master: {_masterBusIndex}, Vern: {_vernBusIndex}, Caller: {_callerBusIndex}, Music: {_musicBusIndex}");
        }

        private void ConfigureVernBus()
        {
            // Add HighPass filter (index 0) - remove rumble
            var highPass = new AudioEffectHighPassFilter();
            highPass.CutoffHz = 80f;
            AudioServer.AddBusEffect(_vernBusIndex, highPass);
            _vernHighPassIndex = 0;

            // Add Compressor (index 1) - radio compression for consistent volume
            var compressor = new AudioEffectCompressor();
            compressor.Threshold = -20f;
            compressor.Ratio = 3f;
            compressor.AttackUs = 50f;
            compressor.ReleaseMs = 100f;
            AudioServer.AddBusEffect(_vernBusIndex, compressor);
            _vernCompressorIndex = 1;

            // Add EQ for slight mid-boost (index 2) - subtle radio presence
            var eq = new AudioEffectEQ();
            eq.SetBandGainDb(3, 1f);  // ~1kHz - slight presence
            AudioServer.AddBusEffect(_vernBusIndex, eq);
            _vernEqIndex = 2;

            // No distortion for Vern - keep it clean
            _vernDistortionIndex = -1;
        }

        private void ConfigureCallerBus()
        {
            // Add LowPass filter (index 0) - simulates phone bandwidth
            var lowPass = new AudioEffectLowPassFilter();
            lowPass.CutoffHz = 2200f;  // Start at level 1
            lowPass.Resonance = 2.5f;
            AudioServer.AddBusEffect(_callerBusIndex, lowPass);
            _callerLowPassIndex = 0;

            // Add HighPass filter (index 1) - removes rumble
            var highPass = new AudioEffectHighPassFilter();
            highPass.CutoffHz = 500f;  // Start at level 1
            AudioServer.AddBusEffect(_callerBusIndex, highPass);
            _callerHighPassIndex = 1;

            // Add Distortion (index 2) - phone line character
            var distortion = new AudioEffectDistortion();
            distortion.Mode = AudioEffectDistortion.ModeEnum.Overdrive;
            distortion.PreGain = 1f;
            distortion.Drive = 0.12f;  // Start at level 1
            AudioServer.AddBusEffect(_callerBusIndex, distortion);
            _callerDistortionIndex = 2;
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
            _staticController?.SetEquipmentLevel(_currentPhoneLineLevel);
            
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

            // Update Distortion
            if (_callerDistortionIndex >= 0)
            {
                var effect = AudioServer.GetBusEffect(_callerBusIndex, _callerDistortionIndex);
                if (effect is AudioEffectDistortion distortion)
                {
                    distortion.Drive = preset.distortion;
                }
            }
        }

        private void ApplyVernEffects(int level)
        {
            if (_vernBusIndex < 0) return;

            // Vern is always clean - no distortion, minimal EQ
            // Update EQ - just band 3 for slight presence
            if (_vernEqIndex >= 0)
            {
                var effect = AudioServer.GetBusEffect(_vernBusIndex, _vernEqIndex);
                if (effect is AudioEffectEQ eq)
                {
                    eq.SetBandGainDb(3, 1f);  // ~1kHz - slight presence
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
        /// Stops Vern playback.
        /// </summary>
        public void StopVern()
        {
            _vernPlayer?.Stop();
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
        /// Stops Music playback.
        /// </summary>
        public void StopMusic()
        {
            _musicPlayer?.Stop();
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
    }
}
