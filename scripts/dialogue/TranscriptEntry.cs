#nullable enable

using System;
using Godot;
using KBTV.Callers;

namespace KBTV.Dialogue
{
    /// <summary>
    /// A single entry in the broadcast transcript.
    /// Records who spoke, what was said, the conversation phase, and when it occurred.
    /// </summary>
    [Serializable]
    public partial class TranscriptEntry : Resource
    {
        [Export] public Speaker Speaker;
        [Export] public string Text;
        [Export] public ConversationPhase Phase;
        [Export] public float Timestamp;
        [Export] public string? ArcId;
        [Export] public string? SpeakerName;
        [Export] public string? CallerId;

        public TranscriptEntry() { Text = string.Empty; }

        public TranscriptEntry(Speaker speaker, string text, ConversationPhase phase, float timestamp, string? arcId = null, string? speakerName = null, string? callerId = null)
        {
            Speaker = speaker;
            Text = text;
            Phase = phase;
            Timestamp = timestamp;
            ArcId = arcId;
            SpeakerName = speakerName;
            CallerId = callerId;
        }

        public static TranscriptEntry CreateVernLine(string text, ConversationPhase phase, float timestamp, string? arcId = null)
        {
            return new TranscriptEntry(Speaker.Vern, text, phase, timestamp, arcId, "Vern");
        }

        public static TranscriptEntry CreateCallerLine(Caller caller, string text, ConversationPhase phase, float timestamp, string? arcId = null)
        {
            return new TranscriptEntry(Speaker.Caller, text, phase, timestamp, arcId, caller.Name, caller.Id);
        }

        public string GetDisplayText()
        {
            var speakerLabel = SpeakerName ?? (Speaker == Speaker.Vern ? "Vern" : "Caller");
            return $"[{Timestamp:F1}s] {speakerLabel}: {Text}";
        }
    }
}
