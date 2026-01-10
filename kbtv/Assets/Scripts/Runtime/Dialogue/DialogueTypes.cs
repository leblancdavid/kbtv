using System;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Who is speaking in the conversation.
    /// </summary>
    public enum Speaker
    {
        /// <summary>The caller on the line</summary>
        Caller,
        /// <summary>Vern Tell, the host</summary>
        Vern
    }

    /// <summary>
    /// The emotional tone of a dialogue line.
    /// Affects display styling and future audio generation.
    /// </summary>
    public enum DialogueTone
    {
        /// <summary>Normal, conversational</summary>
        Neutral,
        /// <summary>Excited, enthusiastic</summary>
        Excited,
        /// <summary>Doubtful, questioning</summary>
        Skeptical,
        /// <summary>Frightened, anxious</summary>
        Scared,
        /// <summary>Uninterested, brushing off</summary>
        Dismissive,
        /// <summary>Warm, accepting</summary>
        Believing,
        /// <summary>Irritated, short-tempered</summary>
        Annoyed,
        /// <summary>Secretive, hushed</summary>
        Conspiratorial,
        /// <summary>Confused, uncertain</summary>
        Confused,
        /// <summary>Dramatic, intense</summary>
        Dramatic,
        /// <summary>Nervous, uneasy</summary>
        Nervous
    }

    /// <summary>
    /// The phase of the conversation structure.
    /// Each call follows this progression.
    /// </summary>
    public enum ConversationPhase
    {
        /// <summary>Vern introduces caller, caller makes initial claim</summary>
        Intro,
        /// <summary>Vern asks follow-up, caller elaborates</summary>
        Probe,
        /// <summary>Vern challenges or accepts, caller responds</summary>
        Challenge,
        /// <summary>Wrapping up the call</summary>
        Resolution
    }

    /// <summary>
    /// The current state of conversation playback.
    /// </summary>
    public enum ConversationState
    {
        /// <summary>Not started yet</summary>
        NotStarted,
        /// <summary>Currently playing through lines</summary>
        Playing,
        /// <summary>Paused (e.g., waiting for event)</summary>
        Paused,
        /// <summary>Finished all lines</summary>
        Completed
    }

    /// <summary>
    /// A single line of dialogue in a conversation.
    /// </summary>
    [Serializable]
    public class DialogueLine
    {
        public Speaker Speaker;
        public string Text;
        public DialogueTone Tone;
        public ConversationPhase Phase;

        /// <summary>
        /// How long to display this line (in seconds).
        /// If <= 0, will be calculated from text length.
        /// </summary>
        public float Duration;

        public DialogueLine() { }

        public DialogueLine(Speaker speaker, string text, DialogueTone tone, ConversationPhase phase, float duration = 0f)
        {
            Speaker = speaker;
            Text = text;
            Tone = tone;
            Phase = phase;
            Duration = duration;
        }

        /// <summary>
        /// Get the display duration for this line.
        /// Uses Duration if set, otherwise calculates from text length.
        /// </summary>
        public float GetDisplayDuration(float baseDelay = 1.5f, float perCharDelay = 0.04f)
        {
            if (Duration > 0f)
                return Duration;

            // Calculate based on text length: base time + time per character
            return baseDelay + (Text?.Length ?? 0) * perCharDelay;
        }

        public override string ToString()
        {
            string speakerName = Speaker == Speaker.Vern ? "VERN" : "CALLER";
            return $"[{speakerName}] ({Tone}) {Text}";
        }
    }
}
