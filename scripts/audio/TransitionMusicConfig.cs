using System.Collections.Generic;
using Godot;

namespace KBTV.Audio
{
    /// <summary>
    /// Configuration for break transition music tracks.
    /// Allows enabling/disabling individual tracks and provides random selection from enabled tracks.
    /// </summary>
    public partial class TransitionMusicConfig : Resource
    {
        [System.Serializable]
        public partial class TrackEntry : Resource
        {
            // TODO: Audio streams when implementing audio system
            // [Export] public AudioStream Clip;
            [Export] public bool Enabled = true;
            [Export] public string DisplayName;
        }

        [Export] private Godot.Collections.Array<TrackEntry> _tracks = new Godot.Collections.Array<TrackEntry>();

        /// <summary>
        /// All tracks in the configuration.
        /// </summary>
        public Godot.Collections.Array<TrackEntry> Tracks => _tracks;

        /// <summary>
        /// Number of total tracks (enabled or disabled).
        /// </summary>
        public int TotalTrackCount => _tracks.Count;

        /// <summary>
        /// Number of enabled tracks.
        /// </summary>
        public int EnabledTrackCount
        {
            get
            {
                int count = 0;
                foreach (var track in _tracks)
                {
                    // TODO: Check if clip exists when audio is implemented
                    if (track.Enabled) count++;
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
            var enabledTracks = new List<TrackEntry>();
            foreach (var track in _tracks)
            {
                // TODO: Check if clip exists when audio is implemented
                if (track.Enabled) enabledTracks.Add(track);
            }

            if (enabledTracks.Count == 0)
            {
                return null;
            }

            int index = (int)(GD.Randi() % enabledTracks.Count);
            // TODO: Return clip when audio is implemented
            return null;
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
        /// Enable or disable a track by index.
        /// </summary>
        public void SetTrackEnabled(int index, bool enabled)
        {
            if (index >= 0 && index < _tracks.Count)
            {
                _tracks[index].Enabled = enabled;
            }
        }

        /// <summary>
        /// Enable all tracks.
        /// </summary>
        public void EnableAllTracks()
        {
            foreach (var track in _tracks)
            {
                track.Enabled = true;
            }
        }

        /// <summary>
        /// Disable all tracks.
        /// </summary>
        public void DisableAllTracks()
        {
            foreach (var track in _tracks)
            {
                track.Enabled = false;
            }
        }

        /// <summary>
        /// Add a track to the configuration.
        /// </summary>
        public void AddTrack(AudioStream clip, string displayName = null, bool enabled = true)
        {
            var entry = new TrackEntry
            {
                // Clip = clip, // TODO: When audio is implemented
                DisplayName = displayName ?? "Unknown",
                Enabled = enabled
            };
            _tracks.Add(entry);
        }

        /// <summary>
        /// Clear all tracks from the configuration.
        /// </summary>
        public void ClearTracks()
        {
            _tracks.Clear();
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