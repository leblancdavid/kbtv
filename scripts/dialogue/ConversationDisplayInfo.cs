#nullable enable

using System;
using Godot;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Read-only snapshot of the current conversation state for UI polling.
    /// UI components read these values to update their display.
    /// </summary>
    [Serializable]
    public partial class ConversationDisplayInfo : Resource
    {
        [Export] public string SpeakerName = string.Empty;
        [Export] public string SpeakerIcon = string.Empty;
        [Export] public string Text = string.Empty;
        [Export] public ConversationPhase Phase = ConversationPhase.Intro;
        [Export] public float Progress = 0f;
        [Export] public bool IsTyping = false;
        [Export] public bool IsConversationActive = false;
        [Export] public string? CurrentArcId;
        [Export] public BroadcastFlowState FlowState = BroadcastFlowState.Idle;
        [Export] public float CurrentLineDuration = 0f;
        [Export] public float ElapsedLineTime = 0f;
        [Export] public int CurrentLineIndex = 0;
        [Export] public int TotalLines = 0;

        public ConversationDisplayInfo() { }

        public ConversationDisplayInfo Copy()
        {
            return new ConversationDisplayInfo
            {
                SpeakerName = SpeakerName,
                SpeakerIcon = SpeakerIcon,
                Text = Text,
                Phase = Phase,
                Progress = Progress,
                IsTyping = IsTyping,
                IsConversationActive = IsConversationActive,
                CurrentArcId = CurrentArcId,
                FlowState = FlowState,
                CurrentLineDuration = CurrentLineDuration,
                ElapsedLineTime = ElapsedLineTime,
                CurrentLineIndex = CurrentLineIndex,
                TotalLines = TotalLines
            };
        }

        public bool HasChanged(ConversationDisplayInfo other)
        {
            return SpeakerName != other.SpeakerName ||
                   SpeakerIcon != other.SpeakerIcon ||
                   Text != other.Text ||
                   Phase != other.Phase ||
                   IsTyping != other.IsTyping ||
                   IsConversationActive != other.IsConversationActive ||
                   FlowState != other.FlowState ||
                   CurrentArcId != other.CurrentArcId;
        }

        public static ConversationDisplayInfo CreateIdle()
        {
            return new ConversationDisplayInfo
            {
                SpeakerName = string.Empty,
                Text = string.Empty,
                FlowState = BroadcastFlowState.Idle,
                IsTyping = false,
                IsConversationActive = false
            };
        }

        public static ConversationDisplayInfo CreateDeadAir(string text)
        {
            return new ConversationDisplayInfo
            {
                SpeakerName = "Vern",
                SpeakerIcon = "VERN",
                Text = text,
                Phase = ConversationPhase.Intro,
                FlowState = BroadcastFlowState.DeadAirFiller,
                IsTyping = true,
                IsConversationActive = false
            };
        }

        public static ConversationDisplayInfo CreateBroadcastLine(string speaker, string icon, string text, ConversationPhase phase)
        {
            return new ConversationDisplayInfo
            {
                SpeakerName = speaker,
                SpeakerIcon = icon,
                Text = text,
                Phase = phase,
                FlowState = BroadcastFlowState.ShowOpening,
                IsTyping = true,
                IsConversationActive = false
            };
        }

        public static ConversationDisplayInfo CreateConversationLine(
            string speaker,
            string icon,
            string text,
            ConversationPhase phase,
            string arcId,
            int lineIndex,
            int totalLines,
            float duration,
            float elapsed)
        {
            return new ConversationDisplayInfo
            {
                SpeakerName = speaker,
                SpeakerIcon = icon,
                Text = text,
                Phase = phase,
                CurrentArcId = arcId,
                CurrentLineIndex = lineIndex,
                TotalLines = totalLines,
                CurrentLineDuration = duration,
                ElapsedLineTime = elapsed,
                Progress = duration > 0 ? elapsed / duration : 0f,
                FlowState = BroadcastFlowState.Conversation,
                IsTyping = elapsed < duration,
                IsConversationActive = true
            };
        }
    }
}
