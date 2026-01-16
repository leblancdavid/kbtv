using KBTV.Callers;

namespace KBTV.Core.Events.Queue
{
    /// <summary>
    /// Events related to the caller queue (incoming, on-hold).
    /// </summary>
    public class CallerAdded
    {
        public required Caller Caller { get; init; }
    }

    public class CallerRemoved
    {
        public required Caller Caller { get; init; }
    }

    public class CallerStateChanged
    {
        public required Caller Caller { get; init; }
        public required CallerState OldState { get; init; }
        public required CallerState NewState { get; init; }
    }

    public class QueueCapacityChanged
    {
        public required int CurrentCount { get; init; }
        public required int MaxCapacity { get; init; }
    }

    public class IncomingCallerQueued
    {
        public required Caller Caller { get; init; }
    }

    public class OnHoldCallerQueued
    {
        public required Caller Caller { get; init; }
    }
}
