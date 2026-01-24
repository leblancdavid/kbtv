#nullable enable

using KBTV.Core;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Event fired when an audio line completes playback.
    /// Used by the conversation system to advance dialogue.
    /// </summary>
    public partial class AudioCompletedEvent : GameEvent
    {
        /// <summary>
        /// Unique identifier for the completed audio line.
        /// </summary>
        public string LineId { get; }
        
        /// <summary>
        /// The speaker who completed their line.
        /// </summary>
        public Speaker Speaker { get; }
        
        public AudioCompletedEvent(string lineId, Speaker speaker)
        {
            LineId = lineId;
            Speaker = speaker;
            Source = "AudioDialoguePlayer";
        }
    }
    
    /// <summary>
    /// Event fired when a conversation advances to the next line.
    /// </summary>
    public partial class ConversationAdvancedEvent : GameEvent
    {
        /// <summary>
        /// The new current line in the conversation.
        /// </summary>
        public BroadcastLine? CurrentLine { get; }

        public ConversationAdvancedEvent(BroadcastLine? currentLine)
        {
            CurrentLine = currentLine;
            Source = "BroadcastCoordinator";
        }
    }

    /// <summary>
    /// Event fired when a conversation starts and is ready to play lines.
    /// </summary>
    public partial class ConversationStartedEvent : GameEvent
    {
        public ConversationStartedEvent()
        {
            Source = "BroadcastCoordinator";
        }
    }

    /// <summary>
    /// Event fired when the live show starts.
    /// Triggers Vern's intro dialog to begin playing.
    /// </summary>
    public partial class ShowStartedEvent : GameEvent
    {
        public ShowStartedEvent()
        {
            Source = "BroadcastCoordinator";
        }
    }

    /// <summary>
    /// Event fired when a new line becomes available for display/playback.
    /// This is the primary event for event-driven broadcast flow.
    /// </summary>
    public partial class LineAvailableEvent : GameEvent
    {
        /// <summary>
        /// The line that is now available for display.
        /// </summary>
        public BroadcastLine Line { get; }

        public LineAvailableEvent(BroadcastLine line)
        {
            Line = line;
            Source = "BroadcastCoordinator";
        }
    }

    /// <summary>
    /// Event fired when a line has completed playback.
    /// Used to advance the broadcast state machine.
    /// </summary>
    public partial class LineCompletedEvent : GameEvent
    {
        /// <summary>
        /// The line that completed.
        /// </summary>
        public BroadcastLine CompletedLine { get; }

        /// <summary>
        /// Unique identifier for the completed line.
        /// </summary>
        public string LineId { get; }

        public LineCompletedEvent(BroadcastLine completedLine)
        {
            CompletedLine = completedLine;
            LineId = completedLine.SpeakerId;
            Source = "ConversationDisplay";
        }
    }

    /// <summary>
    /// Event fired when the broadcast state changes.
    /// Used by UI and other systems to react to state transitions.
    /// </summary>
    public partial class BroadcastStateChangedEvent : GameEvent
    {
        /// <summary>
        /// The previous state before the transition.
        /// </summary>
        public BroadcastState OldState { get; }

        /// <summary>
        /// The new state after the transition.
        /// </summary>
        public BroadcastState NewState { get; }

        public BroadcastStateChangedEvent(BroadcastState oldState, BroadcastState newState)
        {
            OldState = oldState;
            NewState = newState;
            Source = "BroadcastStateManager";
        }
    }
}