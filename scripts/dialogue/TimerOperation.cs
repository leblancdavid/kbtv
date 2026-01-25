#nullable enable

using System;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Base record for timer operations that need to be executed on the main thread.
    /// Provides a thread-safe way to queue Godot timer manipulations from background threads.
    /// </summary>
    public abstract record TimerOperation
    {
        /// <summary>
        /// Timestamp when the operation was created (for debugging and timing analysis).
        /// </summary>
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        
        /// <summary>
        /// Optional operation identifier for debugging and tracking.
        /// </summary>
        public string? OperationId { get; init; }
    }

    /// <summary>
    /// Operation to start the show timing with specified duration.
    /// </summary>
    /// <param name="Duration">Show duration in seconds</param>
    public record StartShowOperation(float Duration) : TimerOperation;

    /// <summary>
    /// Operation to stop all show timing.
    /// </summary>
    public record StopShowOperation : TimerOperation;

    /// <summary>
    /// Operation to schedule break warning timers for a future break.
    /// </summary>
    /// <param name="BreakTimeFromNow">Time in seconds from now when the break should occur</param>
    public record ScheduleBreakWarningsOperation(float BreakTimeFromNow) : TimerOperation;

    /// <summary>
    /// Operation to start an ad break with specified duration.
    /// </summary>
    /// <param name="Duration">Ad break duration in seconds (default: 30.0f)</param>
    public record StartAdBreakOperation(float Duration = 30.0f) : TimerOperation;

    /// <summary>
    /// Operation to stop the current ad break.
    /// </summary>
    public record StopAdBreakOperation : TimerOperation;

    /// <summary>
    /// Operation to initialize the timer with dependencies.
    /// </summary>
    /// <param name="EventBus">Event bus instance for publishing timing events</param>
    public record InitializeOperation(KBTV.Core.EventBus EventBus) : TimerOperation;

    /// <summary>
    /// Operation to get remaining time until a specific timing event.
    /// Returns the result via the provided callback.
    /// </summary>
    /// <param name="EventType">Type of timing event to check</param>
    /// <param name="Callback">Callback to receive the result</param>
    public record GetTimeUntilOperation(
        BroadcastTimingEventType EventType, 
        Action<float> Callback
    ) : TimerOperation;

    /// <summary>
    /// Operation to check if a specific timer is currently active.
    /// Returns the result via the provided callback.
    /// </summary>
    /// <param name="EventType">Type of timing event to check</param>
    /// <param name="Callback">Callback to receive the result</param>
    public record IsTimerActiveOperation(
        BroadcastTimingEventType EventType, 
        Action<bool> Callback
    ) : TimerOperation;
}