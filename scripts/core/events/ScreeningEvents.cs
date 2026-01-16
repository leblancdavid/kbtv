using KBTV.Callers;

namespace KBTV.Core.Events.Screening
{
    /// <summary>
    /// Events related to the screening process.
    /// </summary>
    public class ScreeningStarted
    {
        public required Caller Caller { get; init; }
    }

    public class ScreeningEnded
    {
        public required Caller Caller { get; init; }
    }

    public class ScreeningApproved
    {
        public required Caller Caller { get; init; }
    }

    public class ScreeningRejected
    {
        public required Caller Caller { get; init; }
    }

    public class ScreeningPatienceUpdated
    {
        public required float RemainingPatience { get; init; }
        public required float MaxPatience { get; init; }
    }

    public class ScreeningPropertyRevealed
    {
        public required string PropertyName { get; init; }
        public required object Value { get; init; }
    }

    public class ScreeningProgressUpdated
    {
        public required int PropertiesRevealed { get; init; }
        public required int TotalProperties { get; init; }
        public required float PatienceRemaining { get; init; }
        public required float ElapsedTime { get; init; }
    }
}
