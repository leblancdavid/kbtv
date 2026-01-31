using System.Threading;
using System.Threading.Tasks;
using Godot;
using KBTV.Dialogue;

namespace KBTV.Audio
{
    /// <summary>
    /// Interface for broadcast audio services.
    /// Handles audio playback for the broadcast system.
    /// </summary>
    public interface IBroadcastAudioService
    {
        /// <summary>
        /// Plays audio from the specified path asynchronously.
        /// Completes when audio playback finishes.
        /// </summary>
        Task PlayAudioAsync(string audioPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Plays the specified audio stream asynchronously.
        /// Completes when audio playback finishes.
        /// </summary>
        Task PlayAudioStreamAsync(AudioStream audioStream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Plays silent audio for the specified duration asynchronously.
        /// Used for timing-critical operations like break queuing and show ending.
        /// </summary>
        Task PlaySilentAudioAsync(float duration = 4.0f);

        /// <summary>
        /// Plays audio for a broadcast item by loading it based on item type and metadata.
        /// </summary>
        Task PlayAudioForBroadcastItemAsync(BroadcastItem item);

        /// <summary>
        /// Stop current playback if any.
        /// </summary>
        void Stop();

        /// <summary>
        /// Check if player is currently playing audio.
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Check if broadcast audio is disabled.
        /// </summary>
        bool IsAudioDisabled { get; }

        /// <summary>
        /// Event fired when a broadcast item audio completes playback.
        /// Subscribers should advance the conversation or broadcast flow.
        /// </summary>
        event System.Action<AudioCompletedEvent>? LineCompleted;
    }
}