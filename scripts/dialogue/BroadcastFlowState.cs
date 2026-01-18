namespace KBTV.Dialogue
{
    /// <summary>
    /// Represents the current state of the broadcast flow.
    /// Used by ConversationManager to orchestrate show transitions.
    /// </summary>
    public enum BroadcastFlowState
    {
        /// <summary>Not in a broadcast or waiting for trigger</summary>
        Idle,

        /// <summary>Playing Vern's show opening line</summary>
        ShowOpening,

        /// <summary>Playing conversation arc with a caller</summary>
        Conversation,

        /// <summary>Playing transition between callers</summary>
        BetweenCallers,

        /// <summary>Playing dead air filler monologue (no callers)</summary>
        DeadAirFiller,

        /// <summary>Playing Vern's show closing line</summary>
        ShowClosing
    }

    /// <summary>
    /// Helper extension methods for BroadcastFlowState.
    /// </summary>
    public static class BroadcastFlowStateExtensions
    {
        /// <summary>
        /// Check if the state represents an active broadcast activity.
        /// </summary>
        public static bool IsActiveBroadcast(this BroadcastFlowState state)
        {
            return state == BroadcastFlowState.ShowOpening ||
                   state == BroadcastFlowState.Conversation ||
                   state == BroadcastFlowState.BetweenCallers ||
                   state == BroadcastFlowState.DeadAirFiller;
        }

        /// <summary>
        /// Check if the state involves a live conversation with a caller.
        /// </summary>
        public static bool HasActiveConversation(this BroadcastFlowState state)
        {
            return state == BroadcastFlowState.Conversation;
        }

        /// <summary>
        /// Check if the state is a transitional state (not a conversation or filler).
        /// </summary>
        public static bool IsTransition(this BroadcastFlowState state)
        {
            return state == BroadcastFlowState.ShowOpening ||
                   state == BroadcastFlowState.BetweenCallers ||
                   state == BroadcastFlowState.ShowClosing;
        }

        /// <summary>
        /// Get a human-readable name for the state.
        /// </summary>
        public static string GetDisplayName(this BroadcastFlowState state)
        {
            return state switch
            {
                BroadcastFlowState.Idle => "Idle",
                BroadcastFlowState.ShowOpening => "Show Opening",
                BroadcastFlowState.Conversation => "On Air",
                BroadcastFlowState.BetweenCallers => "Transition",
                BroadcastFlowState.DeadAirFiller => "Dead Air",
                BroadcastFlowState.ShowClosing => "Show Closing",
                _ => "Unknown"
            };
        }
    }
}
