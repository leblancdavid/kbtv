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
        private StaticNoiseController _staticController = null!;

        // Bus indices
        private int _masterBusIndex = 0;
        private int _vernBusIndex = -1;
        private int _callerBusIndex = -1;

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

        // Effect presets for each equipment level
        // Format: (lowPassHz, highPassHz, distortion, resonance)
        private static readonly (float lowPass, float highPass, float distortion, float resonance)[] CallerPresets = 
        {
            (2200f, 500f, 0.12f, 2.5f),  // Level 1: Very muffled, lots of distortion
            (3500f, 350f, 0.08f, 2.0f),   // Level 2: Muffled, some distortion
            (6000f, 200f, 0.04f, 1.5f),   // Level 3: Near clear, minimal distortion
            (10000f, 100f, 0.0f, 1.0f)    // Level 4: Crystal clear, no distortion
        };

        // Vern broadcast presets (simpler, mostly quality improvements)
        private static readonly (float eqGain, float distortion)[] VernPresets = 
        {
            (1.0f, 0.02f),   // Level 1: Muffled, some hum
            (1.5f, 0.01f),   // Level 2: Clearer
            (2.0f, 0.005f),  // Level 3: Professional
            (2.5f, 0.0f)     // Level 4: Broadcast quality
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

            GD.Print($"AudioMixerManager: Created buses - Master: {_masterBusIndex}, Vern: {_vernBusIndex}, Caller: {_callerBusIndex}");
        }

        private void ConfigureVernBus()
        {
            // Add HighPass filter (index 0)
            var highPass = new AudioEffectHighPassFilter();
            highPass.CutoffHz = 80f;
            AudioServer.AddBusEffect(_vernBusIndex, highPass);
            _vernHighPassIndex = 0;

            // Add Compressor (index 1)
            var compressor = new AudioEffectCompressor();
            compressor.Threshold = -20f;
            compressor.Ratio = 3f;
            compressor.AttackUs = 50f;
            compressor.ReleaseMs = 100f;
            AudioServer.AddBusEffect(_vernBusIndex, compressor);
            _vernCompressorIndex = 1;

            // Add EQ for mid-boost (index 2) - EQ6 has 6 bands: 0-5
            var eq = new AudioEffectEQ();
            // Boost 1-4kHz range for radio presence (bands 2-4)
            eq.SetBandGainDb(2, 1f);   // ~500Hz
            eq.SetBandGainDb(3, 2f);  // ~1kHz - main presence
            eq.SetBandGainDb(4, 1.5f); // ~2kHz
            AudioServer.AddBusEffect(_vernBusIndex, eq);
            _vernEqIndex = 2;

            // Add subtle distortion (index 3)
            var distortion = new AudioEffectDistortion();
            distortion.Mode = AudioEffectDistortion.ModeEnum.Overdrive;
            distortion.PreGain = 1f;
            distortion.PostGain = 0.98f;
            AudioServer.AddBusEffect(_vernBusIndex, distortion);
            _vernDistortionIndex = 3;
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

        private void SetupAudioPlayers()
        {
            _vernPlayer = new AudioStreamPlayer();
            _vernPlayer.Name = "VernPlayer";
            AddChild(_vernPlayer);

            _callerPlayer = new AudioStreamPlayer();
            _callerPlayer.Name = "CallerPlayer";
            AddChild(_callerPlayer);

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

            int presetIndex = Mathf.Clamp(level - 1, 0, VernPresets.Length - 1);
            var preset = VernPresets[presetIndex];

            // Update EQ - EQ6 has bands 0-5
            if (_vernEqIndex >= 0)
            {
                var effect = AudioServer.GetBusEffect(_vernBusIndex, _vernEqIndex);
                if (effect is AudioEffectEQ eq)
                {
                    // Adjust mid-boost based on level (bands 2-4)
                    eq.SetBandGainDb(2, (float)(preset.eqGain * 0.5));
                    eq.SetBandGainDb(3, preset.eqGain);
                    eq.SetBandGainDb(4, (float)(preset.eqGain * 0.75));
                }
            }

            // Update Distortion
            if (_vernDistortionIndex >= 0)
            {
                var effect = AudioServer.GetBusEffect(_vernBusIndex, _vernDistortionIndex);
                if (effect is AudioEffectDistortion distortion)
                {
                    distortion.Drive = preset.distortion;
                }
            }
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
        /// Checks if Vern is currently playing.
        /// </summary>
        public bool IsVernPlaying => _vernPlayer?.Playing ?? false;

        /// <summary>
        /// Checks if Caller is currently playing.
        /// </summary>
        public bool IsCallerPlaying => _callerPlayer?.Playing ?? false;

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
