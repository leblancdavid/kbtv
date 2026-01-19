#nullable enable

using System;
using Godot;
using KBTV.Callers;

namespace KBTV.Dialogue
{
    /// <summary>
    /// A single entry in the broadcast transcript.
    /// Records who spoke, what was said, the conversation phase, and arc info.
    /// </summary>
    [Serializable]
    public partial class TranscriptEntry : Resource
    {
        [Export] public Speaker Speaker;
        [Export] public string Text;
        [Export] public ConversationPhase Phase;
        [Export] public string? ArcId;
        [Export] public string? SpeakerName;

        public TranscriptEntry() { Text = string.Empty; }

        public TranscriptEntry(Speaker speaker, string text, ConversationPhase phase, string? arcId = null, string? speakerName = null)
        {
            Speaker = speaker;
            Text = text;
            Phase = phase;
            ArcId = arcId;
            SpeakerName = speakerName;
        }

        public static TranscriptEntry CreateMusicLine()
        {
            return new TranscriptEntry(Speaker.Music, "Bumper Music", ConversationPhase.Intro, null, "Music");
        }

        public static TranscriptEntry CreateVernLine(string text, ConversationPhase phase, string? arcId = null)
        {
            return new TranscriptEntry(Speaker.Vern, text, phase, arcId, "Vern");
        }

        public static TranscriptEntry CreateCallerLine(Caller caller, string text, ConversationPhase phase, string? arcId = null)
        {
            return new TranscriptEntry(Speaker.Caller, text, phase, arcId, caller.Name);
        }

        public string GetDisplayText()
        {
            switch (Speaker)
            {
                case Speaker.Music:
                    return "MUSIC";
                case Speaker.Vern:
                    return $"VERN: {Text}";
                case Speaker.Caller:
                    return $"CALLER: {Text}";
                default:
                    return "TRANSCRIPT";
            }
        }
    }
}
