using KBTV.Callers;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Interface for the conversation manager service.
    /// Orchestrates the broadcast flow during LiveShow phase.
    /// </summary>
    public interface IConversationManager
    {
        /// <summary>
        /// Current display information for UI polling.
        /// </summary>
        ConversationDisplayInfo DisplayInfo { get; }

        /// <summary>
        /// Current broadcast flow state.
        /// </summary>
        BroadcastFlowState CurrentState { get; }

        /// <summary>
        /// Whether a conversation is currently active.
        /// </summary>
        bool IsConversationActive { get; }

        /// <summary>
        /// Whether the broadcast flow is currently active (show in progress).
        /// </summary>
        bool IsBroadcastActive { get; }

        /// <summary>
        /// Called when LiveShow phase starts.
        /// </summary>
        void OnLiveShowStarted();

        /// <summary>
        /// Called when LiveShow phase is ending (before PostShow).
        /// </summary>
        void OnLiveShowEnding();

        /// <summary>
        /// Put a specific caller on air immediately.
        /// </summary>
        void PutCallerOnAir(Caller caller);

        /// <summary>
        /// Stop the current conversation and transition to next state.
        /// </summary>
        void EndCurrentConversation();

        /// <summary>
        /// Trigger dead air filler mode (for testing or manual activation).
        /// </summary>
        void StartDeadAirFiller();

        /// <summary>
        /// Stop dead air filler mode.
        /// </summary>
        void StopDeadAirFiller();

        /// <summary>
        /// Check if auto-advance is needed and handle it.
        /// </summary>
        void CheckAutoAdvance();
    }
}
