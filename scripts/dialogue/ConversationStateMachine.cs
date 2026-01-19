#nullable enable

using System;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Pure functional state machine for conversation state management.
    /// All state transitions are deterministic functions of current state and events.
    /// </summary>
    public class ConversationStateMachine
    {
        public ConversationFlowState CurrentState { get; private set; }

        public ConversationStateMachine()
        {
            CurrentState = ConversationFlowState.Idle;
        }

        /// <summary>
        /// Processes an event and returns the new state.
        /// Pure function - no side effects, deterministic transitions.
        /// </summary>
        public ConversationFlowState ProcessEvent(ConversationEvent @event)
        {
            var newState = Transition(CurrentState, @event);
            CurrentState = newState;
            return newState;
        }

        /// <summary>
        /// Pure transition function defining all state changes.
        /// </summary>
        private static ConversationFlowState Transition(ConversationFlowState currentState, ConversationEvent @event)
        {
            return (currentState, @event.Type) switch
            {
                // Idle transitions
                (ConversationFlowState.Idle, ConversationEventType.Start) => ConversationFlowState.WaitingForLine,
                (ConversationFlowState.Idle, ConversationEventType.ConversationEnd) => ConversationFlowState.Idle,

                // Line processing
                (ConversationFlowState.WaitingForLine, ConversationEventType.LineAvailable) => ConversationFlowState.Playing,
                (ConversationFlowState.Playing, ConversationEventType.LineCompleted) => ConversationFlowState.WaitingForLine,
                (ConversationFlowState.Playing, ConversationEventType.Interrupted) => ConversationFlowState.Idle,

                // Branching
                (ConversationFlowState.WaitingForLine, ConversationEventType.Branch) => ConversationFlowState.EvaluatingBranch,
                (ConversationFlowState.EvaluatingBranch, ConversationEventType.BranchResult) => ConversationFlowState.WaitingForLine,

                // End conditions
                (_, ConversationEventType.ConversationEnd) => ConversationFlowState.Completed,
                (ConversationFlowState.Completed, _) => ConversationFlowState.Completed,

                // Default: stay in current state
                _ => currentState
            };
        }

        /// <summary>
        /// Checks if the current state allows line processing.
        /// </summary>
        public bool CanProcessLines => CurrentState == ConversationFlowState.WaitingForLine;

        /// <summary>
        /// Checks if the conversation is active.
        /// </summary>
        public bool IsActive => CurrentState != ConversationFlowState.Idle && CurrentState != ConversationFlowState.Completed;

        /// <summary>
        /// Resets the state machine to idle.
        /// </summary>
        public void Reset()
        {
            CurrentState = ConversationFlowState.Idle;
        }
    }

    /// <summary>
    /// States in the conversation state machine.
    /// </summary>
    public enum ConversationFlowState
    {
        Idle,
        WaitingForLine,
        Playing,
        EvaluatingBranch,
        Completed
    }

    /// <summary>
    /// Events that can occur in conversations.
    /// </summary>
    public class ConversationEvent
    {
        public ConversationEventType Type { get; }
        public object? Data { get; }

        private ConversationEvent(ConversationEventType type, object? data = null)
        {
            Type = type;
            Data = data;
        }

        public static ConversationEvent Start() => new ConversationEvent(ConversationEventType.Start);
        public static ConversationEvent End() => new ConversationEvent(ConversationEventType.ConversationEnd);
        public static ConversationEvent LineAvailable(BroadcastLine line) => new ConversationEvent(ConversationEventType.LineAvailable, line);
        public static ConversationEvent LineCompleted() => new ConversationEvent(ConversationEventType.LineCompleted);
        public static ConversationEvent Interrupted() => new ConversationEvent(ConversationEventType.Interrupted);
        public static ConversationEvent Branch() => new ConversationEvent(ConversationEventType.Branch);
        public static ConversationEvent BranchResult(bool condition) => new ConversationEvent(ConversationEventType.BranchResult, condition);
    }

    /// <summary>
    /// Types of conversation events.
    /// </summary>
    public enum ConversationEventType
    {
        Start,
        ConversationEnd,
        LineAvailable,
        LineCompleted,
        Interrupted,
        Branch,
        BranchResult
    }
}