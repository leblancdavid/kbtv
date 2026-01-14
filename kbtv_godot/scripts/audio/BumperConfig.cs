using System.Collections.Generic;
using System.Linq;
using Godot;

namespace KBTV.Audio
{
    /// <summary>
    /// Configuration for station bumper audio clips.
    /// Used for both intro bumpers (show opening) and return bumpers (after ad breaks).
    /// Allows enabling/disabling individual clips and provides random selection from enabled clips.
    /// </summary>
    public partial class BumperConfig : Resource
    {
        [System.Serializable]
        public partial class BumperEntry : Resource
        {
            // TODO: Audio streams when implementing audio system
            // [Export] public AudioStream Clip;
            [Export] public bool Enabled = true;
            [Export] public string DisplayName;
        }

        [Export] private Godot.Collections.Array<BumperEntry> _bumpers = new Godot.Collections.Array<BumperEntry>();

        /// <summary>
        /// All bumpers in the configuration.
        /// </summary>
        public Godot.Collections.Array<BumperEntry> Bumpers => _bumpers;

        /// <summary>
        /// Number of total bumpers (enabled or disabled).
        /// </summary>
        public int TotalBumperCount => _bumpers.Count;

        /// <summary>
        /// Number of enabled bumpers.
        /// </summary>
        public int EnabledBumperCount
        {
            get
            {
                int count = 0;
                foreach (var bumper in _bumpers)
                {
                    // TODO: Check if clip exists when audio is implemented
                    if (bumper.Enabled) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Get a random enabled clip.
        /// Returns null if no enabled clips are available.
        /// </summary>
        public AudioStream GetRandomEnabledClip()
        {
            var enabledBumpers = new List<BumperEntry>();
            foreach (var bumper in _bumpers)
            {
                // TODO: Check if clip exists when audio is implemented
                if (bumper.Enabled) enabledBumpers.Add(bumper);
            }

            if (enabledBumpers.Count == 0)
            {
                return null;
            }

            int index = (int)(GD.Randi() % enabledBumpers.Count);
            // TODO: Return clip when audio is implemented
            return null;
        }

        /// <summary>
        /// Get the average duration of all enabled clips.
        /// Returns 0 if no enabled clips are available.
        /// </summary>
        public float GetAverageEnabledDuration()
        {
            // TODO: Calculate average duration when audio is implemented
            return 3.0f; // Placeholder duration
        }

        /// <summary>
        /// Get all enabled clips.
        /// </summary>
        public AudioStream[] GetAllEnabledClips()
        {
            // TODO: Return clips when audio is implemented
            return new AudioStream[0];
        }

        /// <summary>
        /// Enable or disable a bumper by index.
        /// </summary>
        public void SetBumperEnabled(int index, bool enabled)
        {
            if (index >= 0 && index < _bumpers.Count)
            {
                _bumpers[index].Enabled = enabled;
            }
        }

        /// <summary>
        /// Enable all bumpers.
        /// </summary>
        public void EnableAllBumpers()
        {
            foreach (var bumper in _bumpers)
            {
                bumper.Enabled = true;
            }
        }

        /// <summary>
        /// Disable all bumpers.
        /// </summary>
        public void DisableAllBumpers()
        {
            foreach (var bumper in _bumpers)
            {
                bumper.Enabled = false;
            }
        }

        /// <summary>
        /// Add a bumper to the configuration.
        /// </summary>
        public void AddBumper(AudioStream clip, string displayName = null, bool enabled = true)
        {
            var entry = new BumperEntry
            {
                // Clip = clip, // TODO: When audio is implemented
                DisplayName = displayName ?? "Unknown",
                Enabled = enabled
            };
            _bumpers.Add(entry);
        }

        /// <summary>
        /// Clear all bumpers from the configuration.
        /// </summary>
        public void ClearBumpers()
        {
            _bumpers.Clear();
        }

        /// <summary>
        /// Check if a clip is already in the configuration.
        /// </summary>
        public bool ContainsClip(AudioStream clip)
        {
            // TODO: Implement when audio is added
            return false;
        }
    }
}