#nullable enable

using System;
using Godot;
using KBTV.Callers;

namespace KBTV.Audio
{
    /// <summary>
    /// Controls phone static noise playback during caller audio.
    /// Volume is based on caller's phone quality - worse quality = more static.
    /// Uses Finished signal for seamless looping.
    /// </summary>
    public partial class StaticNoiseController : Node
    {
        private AudioStreamPlayer _staticPlayer = null!;
        private AudioStream? _staticStream;
        
        private float _currentVolume = 0.10f;
        private bool _shouldBePlaying = false;

        // Base static volume (10%)
        private const float BASE_STATIC_VOLUME = 0.10f;

        // Phone quality multipliers (linear: 100% → 50%)
        // Terrible quality = most static, Good quality = least static
        private static readonly float[] PhoneQualityMultipliers = 
        {
            1.0f,   // Terrible: 100% static (rotary phone, bad signal)
            0.83f,  // Poor: 83% static (old cordless, cheap prepaid)
            0.67f,  // Average: 67% static (standard landline)
            0.5f    // Good: 50% static (modern smartphone, clear VOIP)
        };

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
            
            // Set default volume (Average quality)
            SetPhoneQuality(CallerPhoneQuality.Average);
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
        /// Sets the caller's phone quality for static volume adjustment.
        /// Worse quality = more static, better quality = less static.
        /// </summary>
        public void SetPhoneQuality(CallerPhoneQuality quality)
        {
            int index = (int)quality;  // Enum order: Terrible=0, Poor=1, Average=2, Good=3
            float multiplier = PhoneQualityMultipliers[index];
            _currentVolume = BASE_STATIC_VOLUME * multiplier;
            
            // Apply immediately if player exists
            if (_staticPlayer != null)
            {
                _staticPlayer.VolumeDb = LinearToDb(_currentVolume);
            }
            
            GD.Print($"StaticNoiseController: PhoneQuality={quality}, Static volume={(_currentVolume * 100f):F1}%");
        }

        /// <summary>
        /// Starts playing static noise.
        /// </summary>
        public void StartStatic()
        {
            GD.Print($"StartStatic CALLED - currentVolume={_currentVolume}");
            if (_staticPlayer != null && _staticStream != null)
            {
                _shouldBePlaying = true;
                _staticPlayer.Stream = _staticStream;
                if (!_staticPlayer.Playing)
                {
                    _staticPlayer.Play();
                    GD.Print($"StaticNoiseController: Started static - Volume: {(_currentVolume * 100f):F1}%, dB: {_staticPlayer.VolumeDb:F2}");
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
