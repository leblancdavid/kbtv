#nullable enable

namespace KBTV.Core
{
    /// <summary>
    /// Base class for all game events in the event-driven architecture.
    /// Events are used for communication between systems in a decoupled manner.
    /// </summary>
    public abstract partial class GameEvent
    {
        /// <summary>
        /// Timestamp when the event was created.
        /// </summary>
        public double Timestamp { get; } = Godot.Time.GetTicksMsec() / 1000.0;
        
        /// <summary>
        /// Optional source identifier for debugging.
        /// </summary>
        public string? Source { get; set; }
    }
}