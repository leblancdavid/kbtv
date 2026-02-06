#nullable enable

using System;
using KBTV.Core;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Generic broadcast event system replacing the complex multi-event architecture.
    /// All broadcast activities (music, lines, ads, transitions) use this unified event pattern.
    /// </summary>
    public enum BroadcastEventType
    {
        /// <summary>Broadcast item begins playing/execution</summary>
        Started,

        /// <summary>Broadcast item completed successfully</summary>
        Completed,

        /// <summary>Broadcast item was interrupted (break, show end, error)</summary>
        Interrupted
    }

    /// <summary>
    /// Generic broadcast event for all broadcast activities.
    /// Replaces LineAvailableEvent, AudioCompletedEvent, LineCompletedEvent, etc.
    /// </summary>
    public class BroadcastEvent : GameEvent
    {
        public BroadcastEventType Type { get; }
        public string ItemId { get; }
        public BroadcastItem? Item { get; }
        public object? Context { get; }

        public BroadcastEvent(BroadcastEventType type, string itemId, BroadcastItem? item = null, object? context = null)
        {
            Type = type;
            ItemId = itemId;
            Item = item;
            Context = context;
        }

        public override string ToString() =>
            $"BroadcastEvent({Type}, ItemId={ItemId}, ItemType={Item?.Type.ToString() ?? "null"})";
    }

    /// <summary>
    /// Broadcast interruption event for handling breaks, show ending, etc.
    /// </summary>
    public class BroadcastInterruptionEvent : GameEvent
    {
        public BroadcastInterruptionReason Reason { get; }
        public object? Context { get; }

        public BroadcastInterruptionEvent(BroadcastInterruptionReason reason, object? context = null)
        {
            Reason = reason;
            Context = context;
        }
    }

    /// <summary>
    /// Event fired when broadcast state changes (conversation, ad break, etc.).
    /// Used by UI to update display based on broadcast phase.
    /// </summary>
    public class BroadcastStateChangedEvent : GameEvent
    {
        public AsyncBroadcastState NewState { get; }
        public AsyncBroadcastState PreviousState { get; }

        public BroadcastStateChangedEvent(AsyncBroadcastState newState, AsyncBroadcastState previousState)
        {
            NewState = newState;
            PreviousState = previousState;
        }
    }

    public enum BroadcastInterruptionReason
    {
        ShowEnding,
        BreakStarting,
        BreakEnding,
        BreakImminent,
        BreakGracePeriod,
        UserAction,
        CallerDropped,
        Error
    }

    /// <summary>
    /// Event fired when a broadcast item starts playback with duration information.
    /// Used by UI to sync typewriter effects to actual audio duration.
    /// </summary>
    public partial class BroadcastItemStartedEvent : GameEvent
    {
        /// <summary>
        /// The broadcast item that started playing.
        /// </summary>
        public BroadcastItem Item { get; }

        /// <summary>
        /// Duration in seconds for typewriter synchronization.
        /// </summary>
        public float Duration { get; }

        /// <summary>
        /// Actual audio length if available (for precise sync).
        /// </summary>
        public float AudioLength { get; }

public BroadcastItemStartedEvent(BroadcastItem item, float duration, float audioLength = 0f)
        {
            Item = item;
            Duration = duration;
            AudioLength = audioLength;
            Source = "BroadcastItemExecutor";
        }
    }

    /// <summary>
    /// Event fired when audio playback completes.
    /// Used for dialogue progression and conversation flow.
    /// </summary>
    public class AudioCompletedEvent : GameEvent
    {
        /// <summary>
        /// ID of the dialogue line that completed.
        /// </summary>
        public string LineId { get; }

        /// <summary>
        /// Speaker who completed the line.
        /// </summary>
        public Speaker Speaker { get; }

        public AudioCompletedEvent(string lineId, Speaker speaker)
        {
            LineId = lineId;
            Speaker = speaker;
            Source = "AudioDialoguePlayer";
        }
    }
}