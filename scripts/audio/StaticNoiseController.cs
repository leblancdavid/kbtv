#nullable enable

using System;
using System.Threading.Tasks;
using Godot;

namespace KBTV.Audio
{
    /// <summary>
    /// Controls phone static noise playback during caller audio.
    /// Volume is based on equipment level - worse equipment = more static.
    /// Uses Finished signal for seamless looping.
    /// </summary>
    public partial class StaticNoiseController : Node
    {
        private AudioStreamPlayer _staticPlayer = null!;
        private AudioStream? _staticStream;
        
        private float _currentVolume = 0.20f;
        private int _equipmentLevel = 1;
        private bool _shouldBePlaying = false;

        // Static volume by equipment level (1 = loud, 4 = quiet)
        // 10% at level 1, tapering down
        private static readonly float[] StaticVolumes = { 0.10f, 0.06f, 0.03f, 0.01f };

        public override void _Ready()
        {
            SetupStaticPlayer();
        }

        private void SetupStaticPlayer()
        {
            _staticPlayer = new AudioStreamPlayer();
            _staticPlayer.Name = "StaticPlayer";
            _staticPlayer.Bus = "Static";  // Route to Static bus with light phone effects
            AddChild(_staticPlayer);
            
            // Subscribe to Finished signal for seamless looping
            _staticPlayer.Finished += OnStaticFinished;
            
            GD.Print($"SetupStaticPlayer: Static player created, assigned to Bus='{_staticPlayer.Bus}'");

            // Load the static audio file
            GD.Print("StaticNoiseController: Attempting to load phone_static_loop.ogg...");
            _staticStream = GD.Load<AudioStream>("res://assets/audio/sfx/phone_static_loop.ogg");
            if (_staticStream != null)
            {
                _staticPlayer.Stream = _staticStream;
                float length = 0f;
                if (_staticStream is AudioStreamMP3 mp3) length = (float)mp3.GetLength();
                else if (_staticStream is AudioStreamOggVorbis ogg) length = (float)ogg.GetLength();
                else if (_staticStream is Godot.AudioStreamWav wav) length = (float)wav.GetLength();
                GD.Print($"StaticNoiseController: Successfully loaded phone_static_loop.ogg - Length: {length}s");
            }
            else
            {
                GD.PrintErr("StaticNoiseController: FAILED to load phone_static_loop.ogg - File not found or corrupted!");
            }
        }

        private void OnStaticFinished()
        {
            // Restart immediately for seamless loop (if still supposed to be playing)
            if (_staticPlayer != null && _staticStream != null && _shouldBePlaying)
            {
                _staticPlayer.Stream = _staticStream;
                _staticPlayer.Play();
                GD.Print("StaticNoiseController: Static looped seamlessly");
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
            float db = LinearToDb(_currentVolume);
            
            GD.Print($"StaticNoiseController: Setting volume - Linear={_currentVolume}, dB={db:F2}");
            
            if (_staticPlayer != null)
            {
                _staticPlayer.VolumeDb = db;
                GD.Print($"StaticNoiseController: VolumeDb set to {_staticPlayer.VolumeDb:F2}");
            }
        }

        /// <summary>
        /// Starts playing static noise.
        /// </summary>
        public void StartStatic()
        {
            GD.Print($"StartStatic CALLED - equipmentLevel={_equipmentLevel}, currentVolume={_currentVolume}");
            if (_staticPlayer != null && _staticStream != null)
            {
                _shouldBePlaying = true;
                _staticPlayer.Stream = _staticStream;
                if (!_staticPlayer.Playing)
                {
                    _staticPlayer.Play();
                    GD.Print($"StaticNoiseController: Started static - Equipment Level: {_equipmentLevel}, Volume: {_currentVolume}, dB: {_staticPlayer.VolumeDb:F2}");
                    GD.Print($"StaticNoiseController: Bus='{_staticPlayer.Bus}', Playing={_staticPlayer.Playing}, VolumeDb={_staticPlayer.VolumeDb:F2}");
                }
                else
                {
                    GD.Print("StaticNoiseController: Static already playing, not restarting");
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
            _shouldBePlaying = false;
            if (_staticPlayer != null && _staticPlayer.Playing)
            {
                _staticPlayer.Stop();
                GD.Print("StaticNoiseController: Stopped static");
            }
        }

        /// <summary>
        /// Stops playing static noise instantly (no fade).
        /// </summary>
        public void StopStaticWithFade(float fadeDuration = 0.15f)
        {
            // Instant stop - no fade
            StopStatic();
            GD.Print("StaticNoiseController: Stopped static instantly (no fade)");
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
