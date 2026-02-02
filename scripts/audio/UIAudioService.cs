#nullable enable

using System.Collections.Generic;
using Godot;
using KBTV.Core;

namespace KBTV.Audio
{
    /// <summary>
    /// Enumeration of available UI sound effects.
    /// </summary>
    public enum UISfx
    {
        PropertyReveal,
        ButtonClick,
        CallerApproved,
        CallerRejected,
        ScreeningComplete,
        Warning,
        Error
    }

    /// <summary>
    /// Interface for the UI audio service.
    /// </summary>
    public interface IUIAudioService
    {
        /// <summary>
        /// Play a UI sound effect.
        /// </summary>
        /// <param name="sfx">The sound effect to play</param>
        /// <param name="pitchVariation">Random pitch variation (0 = none, 0.1 = ±10%)</param>
        void PlaySfx(UISfx sfx, float pitchVariation = 0f);

        /// <summary>
        /// Play a UI sound effect by name.
        /// </summary>
        /// <param name="sfxName">The name of the sound effect</param>
        /// <param name="pitchVariation">Random pitch variation (0 = none, 0.1 = ±10%)</param>
        void PlaySfx(string sfxName, float pitchVariation = 0f);

        /// <summary>
        /// Check if a sound effect is available.
        /// </summary>
        bool HasSfx(UISfx sfx);
    }

    /// <summary>
    /// Service for playing UI sound effects like button clicks, property reveals, etc.
    /// Uses a simple AudioStreamPlayer with preloaded sounds for responsive playback.
    /// </summary>
    public partial class UIAudioService : Node, IUIAudioService
    {
        // Sound effect paths - update these when audio files are added
        private static readonly Dictionary<UISfx, string> SfxPaths = new()
        {
            { UISfx.PropertyReveal, "res://assets/audio/sfx/property_reveal.ogg" },
            { UISfx.ButtonClick, "res://assets/audio/sfx/button_click.ogg" },
            { UISfx.CallerApproved, "res://assets/audio/sfx/caller_approved.ogg" },
            { UISfx.CallerRejected, "res://assets/audio/sfx/caller_rejected.ogg" },
            { UISfx.ScreeningComplete, "res://assets/audio/sfx/screening_complete.ogg" },
            { UISfx.Warning, "res://assets/audio/sfx/warning.ogg" },
            { UISfx.Error, "res://assets/audio/sfx/error.ogg" }
        };

        // Cached audio streams
        private readonly Dictionary<string, AudioStream> _sfxCache = new();
        
        // Audio player pool for overlapping sounds
        private const int PlayerPoolSize = 3;
        private readonly List<AudioStreamPlayer> _playerPool = new();
        private int _nextPlayerIndex = 0;

        // Random for pitch variation
        private readonly System.Random _rng = new();

        /// <summary>
        /// Called when the node is added to the scene tree.
        /// </summary>
        public void OnReady() => this.Provide();

        /// <summary>
        /// Called when dependencies are resolved.
        /// </summary>
        public void OnResolved()
        {
            // No dependencies needed for UIAudioService
        }

        public override void _Ready()
        {
            // Create player pool
            for (int i = 0; i < PlayerPoolSize; i++)
            {
                var player = new AudioStreamPlayer();
                player.Bus = "SFX"; // Use SFX audio bus if available
                AddChild(player);
                _playerPool.Add(player);
            }

            // Preload all available sound effects
            PreloadSfx();
        }

        /// <summary>
        /// Preload all configured sound effects that exist.
        /// </summary>
        private void PreloadSfx()
        {
            foreach (var (sfx, path) in SfxPaths)
            {
                var stream = GD.Load<AudioStream>(path);
                if (stream != null)
                {
                    _sfxCache[path] = stream;
                    GD.Print($"UIAudioService: Loaded {sfx} from {path}");
                }
                // Silently skip missing files - they can be added later
            }

            GD.Print($"UIAudioService: Preloaded {_sfxCache.Count}/{SfxPaths.Count} sound effects");
        }

        /// <summary>
        /// Play a UI sound effect by enum.
        /// </summary>
        public void PlaySfx(UISfx sfx, float pitchVariation = 0f)
        {
            if (SfxPaths.TryGetValue(sfx, out var path))
            {
                PlaySfxInternal(path, pitchVariation);
            }
        }

        /// <summary>
        /// Play a UI sound effect by name (for custom sounds).
        /// </summary>
        public void PlaySfx(string sfxName, float pitchVariation = 0f)
        {
            // Try to construct path from name
            var path = $"res://assets/audio/sfx/{sfxName}.ogg";
            PlaySfxInternal(path, pitchVariation);
        }

        /// <summary>
        /// Check if a sound effect is available.
        /// </summary>
        public bool HasSfx(UISfx sfx)
        {
            if (SfxPaths.TryGetValue(sfx, out var path))
            {
                return _sfxCache.ContainsKey(path);
            }
            return false;
        }

        /// <summary>
        /// Internal method to play a sound effect by path.
        /// </summary>
        private void PlaySfxInternal(string path, float pitchVariation)
        {
            // Try to get from cache
            if (!_sfxCache.TryGetValue(path, out var stream))
            {
                // Try to load it
                stream = GD.Load<AudioStream>(path);
                if (stream == null)
                {
                    // Sound not available - silently return
                    return;
                }
                _sfxCache[path] = stream;
            }

            // Get next available player (round-robin)
            var player = _playerPool[_nextPlayerIndex];
            _nextPlayerIndex = (_nextPlayerIndex + 1) % PlayerPoolSize;

            // Stop if already playing
            if (player.Playing)
            {
                player.Stop();
            }

            // Set stream and pitch
            player.Stream = stream;
            
            // Apply pitch variation
            if (pitchVariation > 0f)
            {
                float variation = (float)(_rng.NextDouble() * 2 - 1) * pitchVariation;
                player.PitchScale = 1f + variation;
            }
            else
            {
                player.PitchScale = 1f;
            }

            player.Play();
        }
    }
}
