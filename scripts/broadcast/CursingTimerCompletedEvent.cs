#nullable enable

using KBTV.Core;

namespace KBTV.Core
{
    /// <summary>
    /// Event published when the cursing timer completes (either by button press or timeout).
    /// </summary>
    public partial class CursingTimerCompletedEvent : GameEvent
    {
        /// <summary>
        /// Whether the player successfully clicked the DELAY button before timeout.
        /// </summary>
        public bool WasSuccessful { get; }

        /// <summary>
        /// Creates a new cursing timer completion event.
        /// </summary>
        /// <param name="wasSuccessful">True if player clicked DELAY, false if timer expired.</param>
        public CursingTimerCompletedEvent(bool wasSuccessful)
        {
            WasSuccessful = wasSuccessful;
        }
    }
}