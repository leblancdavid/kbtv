#nullable enable

using System;
using Godot;

namespace KBTV.Audio
{
    /// <summary>
    /// Controls phone static noise playback during caller audio.
    /// Volume is based on equipment level - worse equipment = more static.
    /// </summary>
    public partial class StaticNoiseController : Node
    {
        private AudioStreamPlayer _staticPlayer = null!;
        private AudioStream? _staticStream;
        
        private float _currentVolume = 0.8f;
        private int _equipmentLevel = 1;

        // Static volume by equipment level (1 = loud, 4 = quiet)
        // Increased volumes for more audible static
        private static readonly float[] StaticVolumes = { 1.5f, 1.0f, 0.6f, 0.2f };

        public override void _Ready()
        {
            SetupStaticPlayer();
        }

        private void SetupStaticPlayer()
        {
            _staticPlayer = new AudioStreamPlayer();
            _staticPlayer.Name = "StaticPlayer";
            AddChild(_staticPlayer);

            // Load the static audio file
            _staticStream = GD.Load<AudioStream>("res://assets/audio/sfx/phone_static_loop.ogg");
            if (_staticStream != null)
            {
                _staticPlayer.Stream = _staticStream;
            }
            else
            {
                GD.PrintErr("StaticNoiseController: Failed to load phone_static_loop.ogg");
            }
        }

        /// <summary>
        /// Sets the equipment level and adjusts static volume accordingly.
        /// </summary>
        public void SetEquipmentLevel(int level)
        {
            _equipmentLevel = Mathf.Clamp(level, 1, 4);
            UpdateStaticVolume();
        }

        private void UpdateStaticVolume()
        {
            int arrayIndex = Mathf.Clamp(_equipmentLevel - 1, 0, StaticVolumes.Length - 1);
            _currentVolume = StaticVolumes[arrayIndex];
            
            if (_staticPlayer != null)
            {
                _staticPlayer.VolumeDb = LinearToDb(_currentVolume);
            }
        }

        /// <summary>
        /// Starts playing static noise.
        /// </summary>
        public void StartStatic()
        {
            if (_staticPlayer != null && _staticStream != null)
            {
                _staticPlayer.Stream = _staticStream;
                if (!_staticPlayer.Playing)
                {
                    _staticPlayer.Play();
                    GD.Print($"StaticNoiseController: Started static at volume {_currentVolume}");
                }
            }
            else
            {
                GD.PrintErr($"StaticNoiseController: Failed to start static - player: {_staticPlayer != null}, stream: {_staticStream != null}");
            }
        }

        /// <summary>
        /// Stops playing static noise.
        /// </summary>
        public void StopStatic()
        {
            if (_staticPlayer != null && _staticPlayer.Playing)
            {
                _staticPlayer.Stop();
                GD.Print("StaticNoiseController: Stopped static");
            }
        }

        /// <summary>
        /// Sets the static volume directly (0.0 - 1.0).
        /// </summary>
        public void SetVolume(float volume)
        {
            _currentVolume = Mathf.Clamp(volume, 0f, 1f);
            if (_staticPlayer != null)
            {
                _staticPlayer.VolumeDb = LinearToDb(_currentVolume);
            }
        }

        /// <summary>
        /// Gets the current static volume (0.0 - 1.0).
        /// </summary>
        public float GetVolume() => _currentVolume;

        /// <summary>
        /// Checks if static is currently playing.
        /// </summary>
        public bool IsPlaying => _staticPlayer?.Playing ?? false;

        private static float LinearToDb(float linear)
        {
            if (linear <= 0f)
                return -80f;
            return 20f * Mathf.Log(linear) / Mathf.Log(10f);
        }
    }
}
