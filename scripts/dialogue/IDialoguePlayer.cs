#nullable enable

using System.Threading.Tasks;
using KBTV.Core;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Interface for dialogue players that handle audio playback and completion events.
    /// Enables event-driven conversation progression instead of timer-based systems.
    /// </summary>
    public interface IDialoguePlayer
    {
        /// <summary>
        /// Play a dialogue line asynchronously.
        /// Returns a Task that completes when audio playback starts.
        /// The player will fire events when playback completes.
        /// </summary>
        /// <param name="line">The dialogue line to play</param>
        /// <returns>Task that completes when playback begins</returns>
        Task PlayLineAsync(BroadcastLine line);

        /// <summary>
        /// Stop current playback if any.
        /// </summary>
        void Stop();

        /// <summary>
        /// Check if player is currently playing audio.
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Event fired when a dialogue line completes playback.
        /// Subscribers should advance the conversation.
        /// </summary>
        event System.Action<AudioCompletedEvent>? LineCompleted;
    }
}