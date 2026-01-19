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
}