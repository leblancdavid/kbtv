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

    public enum BroadcastInterruptionReason
    {
        ShowEnding,
        BreakStarting,
        BreakEnding,
        UserAction,
        Error
    }
}