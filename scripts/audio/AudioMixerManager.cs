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
        private int _callerMuffleLowPassIndex = -1; // Additional low-pass for muffling

        // Static effect indices
        private int _staticLowPassIndex = -1;
        private int _staticHighPassIndex = -1;
        private int _staticDistortionIndex = -1;
        private int _staticMuffleLowPassIndex = -1; // Low-pass for muffling when player exits

        // SFX effect indices (for UI sounds, bleep, and ads)
        private int _sfxMuffleLowPassIndex = -1;
        private int _sfxDistortionIndex = -1;
        private int _sfxCompressorIndex = -1;

        // Music effect indices
        private int _musicMuffleLowPassIndex = -1;

        // Current equipment level
        private int _currentPhoneLineLevel = 1;
        private int _currentBroadcastLevel = 1;

        // Last soundboard knob position, re-applied after equipment level changes.
        private SoundboardKnobState? _soundboardState;

        // Ideal caller knob positions the DSP grades against (null = no caller).
        private SoundboardCallerTargets? _callerTargets;

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

        // Audio normalization compressor/limiter settings (tunable)
        private const float VERN_COMPRESSOR_THRESHOLD = -20f;
        private const float VERN_COMPRESSOR_RATIO = 3f;
        private const float VERN_COMPRESSOR_ATTACK_MS = 10f;
        private const float VERN_COMPRESSOR_RELEASE_MS = 100f;
        private const float VERN_COMPRESSOR_GAIN = 2f; // makeup gain

        private const float CALLER_COMPRESSOR_THRESHOLD = -18f;
        private const float CALLER_COMPRESSOR_RATIO = 4f;
        private const float CALLER_COMPRESSOR_ATTACK_MS = 15f;
        private const float CALLER_COMPRESSOR_RELEASE_MS = 150f;
        private const float CALLER_COMPRESSOR_GAIN = 3f; // makeup gain

        // Caller amplify baseline (the radio-like phone book). The soundboard gain
        // knob pulls this down below-target; above-target is scored as compression.
        private const float CallerBaseAmplifyDb = 8f;

        // Full "tighten" compressor recipe when the channel is pushed above target.
        private const float CallerCompressThresholdMaxDb = -28f;
        private const float CallerCompressRatioMax = 12f;
        private const float VernCompressThresholdMaxDb = -30f;
        private const float VernCompressRatioMax = 10f;
        private const float AdsCompressThresholdMaxDb = -28f;
        private const float AdsCompressRatioMax = 10f;

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

            // Add compressor for volume normalization
            var vernCompressor = new AudioEffectCompressor();
            vernCompressor.Threshold = VERN_COMPRESSOR_THRESHOLD;
            vernCompressor.Gain = VERN_COMPRESSOR_GAIN;
            vernCompressor.Ratio = VERN_COMPRESSOR_RATIO;
            vernCompressor.AttackUs = VERN_COMPRESSOR_ATTACK_MS * 1000; // Convert ms to microseconds
            vernCompressor.ReleaseMs = VERN_COMPRESSOR_RELEASE_MS;
            AudioServer.AddBusEffect(_vernBusIndex, vernCompressor);
            _vernCompressorIndex = AudioServer.GetBusEffectCount(_vernBusIndex) - 1;

            // Vern distortion for above-target gain scoring (Drive 0 = transparent).
            var vernDistortion = new AudioEffectDistortion();
            vernDistortion.Mode = AudioEffectDistortion.ModeEnum.Overdrive;
            vernDistortion.PreGain = 1f;
            vernDistortion.Drive = 0f;
            AudioServer.AddBusEffect(_vernBusIndex, vernDistortion);
            _vernDistortionIndex = AudioServer.GetBusEffectCount(_vernBusIndex) - 1;

            // No EQ on Vern
            _vernEqIndex = -1;
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

            // Add compressor for volume normalization
            var callerCompressor = new AudioEffectCompressor();
            callerCompressor.Threshold = CALLER_COMPRESSOR_THRESHOLD;
            callerCompressor.Gain = CALLER_COMPRESSOR_GAIN;
            callerCompressor.Ratio = CALLER_COMPRESSOR_RATIO;
            callerCompressor.AttackUs = CALLER_COMPRESSOR_ATTACK_MS * 1000; // Convert ms to microseconds
            callerCompressor.ReleaseMs = CALLER_COMPRESSOR_RELEASE_MS;
            AudioServer.AddBusEffect(_callerBusIndex, callerCompressor);
            _callerCompressorIndex = AudioServer.GetBusEffectCount(_callerBusIndex) - 1;

            // Add a low-pass filter for muffling when player is outside
            var muffleLowPass = new AudioEffectLowPassFilter();
            muffleLowPass.CutoffHz = 20000f; // Initially transparent (very high)
            muffleLowPass.Resonance = 1.0f;
            AudioServer.AddBusEffect(_callerBusIndex, muffleLowPass);
            _callerMuffleLowPassIndex = AudioServer.GetBusEffectCount(_callerBusIndex) - 1;
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
            // SFX bus handles UI sounds, bleep effects, and ad spots.
            // Add a low-pass filter for muffling when player is outside
            var muffleLowPass = new AudioEffectLowPassFilter();
            muffleLowPass.CutoffHz = 20000f; // Initially transparent (very high)
            muffleLowPass.Resonance = 1.0f;
            AudioServer.AddBusEffect(_sfxBusIndex, muffleLowPass);
            _sfxMuffleLowPassIndex = AudioServer.GetBusEffectCount(_sfxBusIndex) - 1;

            // Distortion + compressor for above-target ad gain scoring
            // (Drive 0 / gentle compressor = transparent at rest).
            var distortion = new AudioEffectDistortion();
            distortion.Mode = AudioEffectDistortion.ModeEnum.Overdrive;
            distortion.PreGain = 1f;
            distortion.Drive = 0f;
            AudioServer.AddBusEffect(_sfxBusIndex, distortion);
            _sfxDistortionIndex = AudioServer.GetBusEffectCount(_sfxBusIndex) - 1;

            var compressor = new AudioEffectCompressor();
            compressor.Threshold = -12f;
            compressor.Ratio = 2f;
            compressor.AttackUs = 10f;
            compressor.ReleaseMs = 150f;
            AudioServer.AddBusEffect(_sfxBusIndex, compressor);
            _sfxCompressorIndex = AudioServer.GetBusEffectCount(_sfxBusIndex) - 1;
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

            // Re-apply the soundboard knob offsets on top of the new presets so
            // equipment upgrades don't wipe knob positions.
            if (_soundboardState != null)
            {
                ApplySoundboard(_soundboardState);
            }

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
        }

private void ApplyVernEffects(int level)
        {
            if (_vernBusIndex < 0) return;

            // Vern is always clean - no distortion, minimal EQ
            // No reverb applied to Vern
        }

        /// <summary>
        /// Applies soundboard knob deltas on top of the equipment presets.
        /// Passing a state stores it so <see cref="UpdateAudioQuality"/> re-applies
        /// it after equipment upgrades. No-op when no buses exist (e.g. headless tests).
        /// </summary>
        public void ApplySoundboard(SoundboardKnobState state)
        {
            if (state == null)
            {
                return;
            }

            _soundboardState = state;
            var preset = GetCallerPresetInfo(_currentPhoneLineLevel);
            var settings = SoundboardMixerDriver.ComputeEffectSettings(state, preset, _callerTargets);

            SetCallerLowPass(settings.CallerLowPassHz);
            SetCallerHighPass(settings.CallerHighPassHz);
            SetCallerDistortion(settings.CallerDrive);
            SetCallerCompression(settings.CallerCompression);
            SetCallerAmplify(settings.CallerAmplifyDb);
            SetCallerMuffle(settings.CallerMuffleHz);

            SetVernDrive(settings.VernDrive);
            SetVernCompression(settings.VernCompression);
            SetAdsDrive(settings.AdsDrive);
            SetAdsCompression(settings.AdsCompression);

            // Gain knobs pulled low muffle their channel (rather than cutting it).
            SetVernMuffle(settings.VernMuffleHz);
            SetAdsMuffle(settings.AdsMuffleHz);

            // Output faders move the strip; faders never raise the bus above 0 dB
            // (excess above neutral is already scored as drive + compression).
            SetBusVolumeDb(_callerBusIndex, settings.CallerLevelDb);
            SetBusVolumeDb(_vernBusIndex, settings.VernLevelDb);
            SetBusVolumeDb(_sfxBusIndex, settings.AdsLevelDb);

            SetBusVolumeDb(_musicBusIndex, settings.MusicFaderDb);
            SetBusVolumeDb(_masterBusIndex, settings.MasterFaderDb);
        }

        /// <summary>
        /// Sets the ideal caller knob positions and re-grades the DSP stack against
        /// them when a knob state is already applied (e.g. a caller came on air).
        /// </summary>
        public void SetCallerTargets(SoundboardCallerTargets? targets)
        {
            _callerTargets = targets;
            if (_soundboardState != null)
            {
                ApplySoundboard(_soundboardState);
            }
        }

        private SoundboardPresetInfo GetCallerPresetInfo(int level)
        {
            int presetIndex = Mathf.Clamp(level - 1, 0, CallerPresets.Length - 1);
            var preset = CallerPresets[presetIndex];
            return new SoundboardPresetInfo(preset.lowPass, preset.highPass, preset.distortion);
        }

        private void SetCallerLowPass(float cutoffHz)
        {
            if (_callerBusIndex < 0 || _callerLowPassIndex < 0)
            {
                return;
            }

            if (AudioServer.GetBusEffect(_callerBusIndex, _callerLowPassIndex) is AudioEffectLowPassFilter lowPass)
            {
                lowPass.CutoffHz = cutoffHz;
            }
        }

        private void SetCallerHighPass(float cutoffHz)
        {
            if (_callerBusIndex < 0 || _callerHighPassIndex < 0)
            {
                return;
            }

            if (AudioServer.GetBusEffect(_callerBusIndex, _callerHighPassIndex) is AudioEffectHighPassFilter highPass)
            {
                highPass.CutoffHz = cutoffHz;
            }
        }

        private void SetCallerDistortion(float drive)
        {
            if (_callerBusIndex < 0 || _callerDistortionIndex < 0)
            {
                return;
            }

            if (AudioServer.GetBusEffect(_callerBusIndex, _callerDistortionIndex) is AudioEffectDistortion distortion)
            {
                distortion.Drive = drive;
            }
        }

        private void SetCallerAmplify(float offsetDb)
        {
            if (_callerBusIndex < 0 || _callerAmplifyIndex < 0)
            {
                return;
            }

            if (AudioServer.GetBusEffect(_callerBusIndex, _callerAmplifyIndex) is AudioEffectAmplify amplify)
            {
                // Baseline stays at the radio-like phone book; the soundboard knob
                // only attenuates from it (never goes louder than the base).
                amplify.VolumeDb = CallerBaseAmplifyDb + offsetDb;
            }
        }

        private void SetCallerMuffle(float cutoffHz)
        {
            if (_callerBusIndex < 0 || _callerMuffleLowPassIndex < 0)
            {
                return;
            }

            if (AudioServer.GetBusEffect(_callerBusIndex, _callerMuffleLowPassIndex) is AudioEffectLowPassFilter lowPass)
            {
                lowPass.CutoffHz = cutoffHz;
            }
        }

        /// <summary>
        /// Scrubs an effect compressor toward its "tighten" recipe: deeper
        /// compression (0..1) lowers the threshold and raises the ratio.
        /// </summary>
        private static void TuneCompressor(AudioEffectCompressor compressor, float depth,
            float baseThresholdDb, float maxThresholdDb, float baseRatio, float maxRatio)
        {
            compressor.Threshold = Mathf.Lerp(baseThresholdDb, maxThresholdDb, depth);
            compressor.Ratio = Mathf.Lerp(baseRatio, maxRatio, depth);
        }

        private void SetCallerCompression(float depth)
        {
            if (_callerBusIndex < 0 || _callerCompressorIndex < 0)
            {
                return;
            }

            if (AudioServer.GetBusEffect(_callerBusIndex, _callerCompressorIndex) is AudioEffectCompressor compressor)
            {
                TuneCompressor(compressor, depth,
                    CALLER_COMPRESSOR_THRESHOLD, CallerCompressThresholdMaxDb,
                    CALLER_COMPRESSOR_RATIO, CallerCompressRatioMax);
            }
        }

        private void SetVernDrive(float drive)
        {
            if (_vernBusIndex < 0 || _vernDistortionIndex < 0)
            {
                return;
            }

            if (AudioServer.GetBusEffect(_vernBusIndex, _vernDistortionIndex) is AudioEffectDistortion distortion)
            {
                distortion.Drive = drive;
            }
        }

        private void SetVernCompression(float depth)
        {
            if (_vernBusIndex < 0 || _vernCompressorIndex < 0)
            {
                return;
            }

            if (AudioServer.GetBusEffect(_vernBusIndex, _vernCompressorIndex) is AudioEffectCompressor compressor)
            {
                TuneCompressor(compressor, depth,
                    VERN_COMPRESSOR_THRESHOLD, VernCompressThresholdMaxDb,
                    VERN_COMPRESSOR_RATIO, VernCompressRatioMax);
            }
        }

        private void SetAdsDrive(float drive)
        {
            if (_sfxBusIndex < 0 || _sfxDistortionIndex < 0)
            {
                return;
            }

            if (AudioServer.GetBusEffect(_sfxBusIndex, _sfxDistortionIndex) is AudioEffectDistortion distortion)
            {
                distortion.Drive = drive;
            }
        }

        private void SetAdsCompression(float depth)
        {
            if (_sfxBusIndex < 0 || _sfxCompressorIndex < 0)
            {
                return;
            }

            if (AudioServer.GetBusEffect(_sfxBusIndex, _sfxCompressorIndex) is AudioEffectCompressor compressor)
            {
                TuneCompressor(compressor, depth, -12f, AdsCompressThresholdMaxDb, 2f, AdsCompressRatioMax);
            }
        }

        private void SetVernMuffle(float cutoffHz)
        {
            if (_vernBusIndex < 0 || _vernMuffleLowPassIndex < 0)
            {
                return;
            }

            if (AudioServer.GetBusEffect(_vernBusIndex, _vernMuffleLowPassIndex) is AudioEffectLowPassFilter lowPass)
            {
                lowPass.CutoffHz = cutoffHz;
            }
        }

        private void SetAdsMuffle(float cutoffHz)
        {
            if (_sfxBusIndex < 0 || _sfxMuffleLowPassIndex < 0)
            {
                return;
            }

            if (AudioServer.GetBusEffect(_sfxBusIndex, _sfxMuffleLowPassIndex) is AudioEffectLowPassFilter lowPass)
            {
                lowPass.CutoffHz = cutoffHz;
            }
        }

        private void SetBusVolumeDb(int busIndex, float volumeDb)
        {
            if (busIndex < 0 || busIndex >= AudioServer.BusCount)
            {
                return;
            }

            AudioServer.SetBusVolumeDb(busIndex, volumeDb);
        }

        /// <summary>
        /// Current loudest sample of the Vern bus, in dB (pre-fader, so it reflects
        /// the dry voice level rather than the board's strip volume). Returns -80 dB
        /// when the bus is missing.
        /// </summary>
        public float GetVernBusPeakDb() => GetBusPeakDb(_vernBusIndex);

        /// <summary>
        /// Current loudest sample of the Caller bus, in dB (pre-fader, so it reflects
        /// the dry phone-line voice level rather than the board's strip volume).
        /// Returns -80 dB when the bus is missing.
        /// </summary>
        public float GetCallerBusPeakDb() => GetBusPeakDb(_callerBusIndex);

        /// <summary>
        /// Current loudest sample of the Ads/SFX bus, in dB. Ads routes onto the SFX
        /// bus, so this reflects live commercial audio (silent outside ad breaks).
        /// Returns -80 dB when the bus is missing.
        /// </summary>
        public float GetAdsBusPeakDb() => GetBusPeakDb(_sfxBusIndex);

        private float GetBusPeakDb(int busIndex)
        {
            if (busIndex < 0 || busIndex >= AudioServer.BusCount)
            {
                return -80f;
            }

            float peak = float.NegativeInfinity;
            int channels = AudioServer.GetBusChannels(busIndex);
            for (int channel = 0; channel < channels; channel++)
            {
                float level = Mathf.Max(
                    AudioServer.GetBusPeakVolumeLeftDb(busIndex, channel),
                    AudioServer.GetBusPeakVolumeRightDb(busIndex, channel));
                if (level > peak)
                {
                    peak = level;
                }
            }

            return float.IsNegativeInfinity(peak) ? -80f : peak;
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
        /// Sets muffle filters independently for Vern and other audio buses.
        /// </summary>
        /// <param name="muffleVern">Whether to muffle Vern's voice</param>
        /// <param name="muffleOther">Whether to muffle all other broadcast audio (Caller, Static, Music, SFX)</param>
        public void SetMuffled(bool muffleVern, bool muffleOther)
        {
            float vernCutoff = muffleVern ? 200f : 20000f;
            float otherCutoff = muffleOther ? 200f : 20000f;

            // Update Vern muffle filter
            if (_vernMuffleLowPassIndex >= 0 && _vernBusIndex >= 0)
            {
                var effect = AudioServer.GetBusEffect(_vernBusIndex, _vernMuffleLowPassIndex);
                if (effect is AudioEffectLowPassFilter lowPass)
                {
                    lowPass.CutoffHz = vernCutoff;
                }
            }

            // Update Caller muffle filter
            if (_callerMuffleLowPassIndex >= 0 && _callerBusIndex >= 0)
            {
                var effect = AudioServer.GetBusEffect(_callerBusIndex, _callerMuffleLowPassIndex);
                if (effect is AudioEffectLowPassFilter lowPass)
                {
                    lowPass.CutoffHz = otherCutoff;
                }
            }

            // Update Static muffle filter
            if (_staticMuffleLowPassIndex >= 0 && _staticBusIndex >= 0)
            {
                var effect = AudioServer.GetBusEffect(_staticBusIndex, _staticMuffleLowPassIndex);
                if (effect is AudioEffectLowPassFilter lowPass)
                {
                    lowPass.CutoffHz = otherCutoff;
                }
            }

            // Update Music muffle filter
            if (_musicMuffleLowPassIndex >= 0 && _musicBusIndex >= 0)
            {
                var effect = AudioServer.GetBusEffect(_musicBusIndex, _musicMuffleLowPassIndex);
                if (effect is AudioEffectLowPassFilter lowPass)
                {
                    lowPass.CutoffHz = otherCutoff;
                }
            }

            // Update SFX muffle filter
            if (_sfxMuffleLowPassIndex >= 0 && _sfxBusIndex >= 0)
            {
                var effect = AudioServer.GetBusEffect(_sfxBusIndex, _sfxMuffleLowPassIndex);
                if (effect is AudioEffectLowPassFilter lowPass)
                {
                    lowPass.CutoffHz = otherCutoff;
                }
            }

            GD.Print($"AudioMixerManager: SetMuffled(Vern={muffleVern}, Other={muffleOther}) - Vern={vernCutoff}Hz, Other={otherCutoff}Hz");
        }

        public override void _ExitTree()
        {
            // Stop all audio players
            if (_vernPlayer != null)
            {
                _vernPlayer.Stop();
                _vernPlayer.QueueFree();
            }
            if (_callerPlayer != null)
            {
                _callerPlayer.Stop();
                _callerPlayer.QueueFree();
            }
            if (_musicPlayer != null)
            {
                _musicPlayer.Stop();
                _musicPlayer.QueueFree();
            }
            if (_staticController != null)
            {
                _staticController.QueueFree();
            }

            // Remove custom audio buses that were added
            // Remove in reverse order (highest index first)
            RemoveBusIfValid(_sfxBusIndex);
            RemoveBusIfValid(_musicBusIndex);
            RemoveBusIfValid(_staticBusIndex);
            RemoveBusIfValid(_callerBusIndex);
            RemoveBusIfValid(_vernBusIndex);

            GD.Print("AudioMixerManager: Cleanup complete");
        }

        private void RemoveBusIfValid(int busIndex)
        {
            if (busIndex >= 0 && busIndex < AudioServer.BusCount)
            {
                try
                {
                    AudioServer.RemoveBus(busIndex);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"AudioMixerManager: Failed to remove bus {busIndex}: {ex.Message}");
                }
            }
        }

    }
}