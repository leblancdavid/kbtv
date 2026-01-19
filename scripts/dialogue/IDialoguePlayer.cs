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
        /// Play a dialogue line. Returns immediately; playback completion is handled via events.
        /// The player will fire LineCompleted event when playback finishes.
        /// </summary>
        /// <param name="line">The dialogue line to play</param>
        void PlayLineAsync(BroadcastLine line);

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