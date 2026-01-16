#nullable enable

using KBTV.Callers;

namespace KBTV.Core.Events.OnAir
{
    /// <summary>
    /// Events related to on-air caller management.
    /// </summary>
    public class CallerOnAir
    {
        public required Caller Caller { get; init; }
    }

    public class CallerOnAirEnded
    {
        public required Caller Caller { get; init; }
    }

    public class OnAirSlotChanged
    {
        public Caller? PreviousCaller { get; init; }
        public Caller? NewCaller { get; init; }
    }

    public class OnAirConversationStarted
    {
        public required Caller Caller { get; init; }
        public required string Topic { get; init; }
    }

    public class OnAirConversationEnded
    {
        public required Caller Caller { get; init; }
        public required float Duration { get; init; }
    }
}
