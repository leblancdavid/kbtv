#nullable enable

using System;
using Godot;
using KBTV.Core;
using KBTV.Persistence;

namespace KBTV.Audio
{
    /// <summary>
    /// Manages audio routing to Vern (studio) or Caller (phone line) with effects.
    /// Applies audio quality based on equipment level.
    /// </summary>
    [GlobalClass]
    public partial class AudioMixerManager : Node
    {
        private AudioStreamPlayer _vernPlayer = null!;
        private AudioStreamPlayer _callerPlayer = null!;
        private StaticNoiseController _staticController = null!;
        
        private const string VERN_BUS_NAME = "Master";
        private const string CALLER_BUS_NAME = "Master";

        public override void _Ready()
        {
            SetupAudioPlayers();
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

            UpdateAudioQuality();
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
        /// Updates audio quality based on equipment level.
        /// </summary>
        public void UpdateAudioQuality()
        {
            int level = GetPhoneLineLevel();
            ApplyCallerEffects(level);
            _staticController?.SetEquipmentLevel(level);
        }

        private void ApplyCallerEffects(int level)
        {
            var (lowPass, highPass, distortion) = GetEffectSettingsForLevel(level);
            
            // Note: Godot 4 AudioStreamPlayer doesn't have built-in effects
            // Effects are applied via the audio bus system in Godot
            // For now, we manage static noise and volume adjustments
            
            // Adjust caller volume based on equipment level
            // Lower levels need volume boost to compensate for filter loss
            float volumeBoost = level switch
            {
                1 => 6.0f,
                2 => 4.0f,
                3 => 2.0f,
                4 => 0.0f,
                _ => 0.0f
            };
            
            _callerPlayer.VolumeDb = volumeBoost;
        }

        private (float lowPassHz, float highPassHz, float distortion) GetEffectSettingsForLevel(int level)
        {
            return level switch
            {
                1 => (2200f, 500f, 0.12f),
                2 => (3500f, 350f, 0.08f),
                3 => (6000f, 200f, 0.04f),
                4 => (10000f, 100f, 0.0f),
                _ => (2200f, 500f, 0.12f)
            };
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
    }
}
