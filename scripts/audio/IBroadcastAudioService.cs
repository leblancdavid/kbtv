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
        /// Plays audio from the specified path for a maximum duration asynchronously.
        /// Completes when either the duration expires or playback finishes (whichever comes first).
        /// </summary>
        Task PlayAudioForDurationAsync(string audioPath, float maxDuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Plays audio from the specified path for a maximum duration asynchronously.
        /// Completes when either the duration expires or playback finishes (whichever comes first).
        /// </summary>
        /// <param name="immediateStop">If true, stops playback immediately when duration expires. If false, uses deferred stop for thread safety.</param>
        Task PlayAudioForDurationAsync(string audioPath, float maxDuration, bool immediateStop, CancellationToken cancellationToken = default);

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
        /// Gets the duration of an audio stream in seconds.
        /// </summary>
        float GetAudioDuration(AudioStream audioStream);

        /// <summary>
        /// Event fired when a broadcast item audio completes playback.
        /// Subscribers should advance the conversation or broadcast flow.
        /// </summary>
        event System.Action<AudioCompletedEvent>? LineCompleted;
    }
}