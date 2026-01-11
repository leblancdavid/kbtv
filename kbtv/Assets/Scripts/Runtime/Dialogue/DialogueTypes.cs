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
    /// Vern's current mood state. Affects dialogue tone and delivery.
    /// Each conversation arc has variants for all 5 moods.
    /// </summary>
    public enum VernMood
    {
        /// <summary>Brief, yawns, low energy</summary>
        Tired,
        /// <summary>Impatient, short, irritable</summary>
        Grumpy,
        /// <summary>Professional, balanced</summary>
        Neutral,
        /// <summary>Interested, good follow-ups</summary>
        Engaged,
        /// <summary>High energy, enthusiastic</summary>
        Excited
    }

    /// <summary>
    /// The belief path taken during the belief branch of a conversation arc.
    /// Determined by Vern's discernment and caller legitimacy.
    /// </summary>
    public enum BeliefPath
    {
        /// <summary>Vern challenges the caller, caller defends</summary>
        Skeptical,
        /// <summary>Vern validates the caller, caller appreciates</summary>
        Believing
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
    /// The section of a conversation arc where a dialogue line originates.
    /// Used for audio file lookup (belief branch sections have different naming).
    /// </summary>
    public enum ArcSection
    {
        /// <summary>Opening section - Vern introduces caller</summary>
        Intro,
        /// <summary>Middle section - Caller elaborates on their story</summary>
        Development,
        /// <summary>Belief branch - Vern is skeptical</summary>
        Skeptical,
        /// <summary>Belief branch - Vern believes the caller</summary>
        Believing,
        /// <summary>Closing section - Wrapping up the call</summary>
        Conclusion
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
    /// A template for a single dialogue line with selection weight.
    /// Used for Vern's broadcast lines (show opening, filler, etc.).
    /// </summary>
    [Serializable]
    public class DialogueTemplate
    {
        /// <summary>Unique identifier for this template (e.g., "vern_opening_001"). Used for audio file lookup.</summary>
        public string Id;

        /// <summary>The dialogue text (supports placeholders like {callerName}).</summary>
        public string Text;
        
        /// <summary>The emotional tone of this line.</summary>
        public DialogueTone Tone;
        
        /// <summary>Selection weight for random picking (higher = more likely).</summary>
        public float Weight = 1f;

        public DialogueTemplate() { }

        public DialogueTemplate(string text, DialogueTone tone = DialogueTone.Neutral, float weight = 1f)
        {
            Text = text;
            Tone = tone;
            Weight = weight;
        }

        public DialogueTemplate(string id, string text, DialogueTone tone = DialogueTone.Neutral, float weight = 1f)
        {
            Id = id;
            Text = text;
            Tone = tone;
            Weight = weight;
        }
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

        /// <summary>
        /// The original 0-based index of this line within the arc JSON.
        /// Used for audio file lookup (maps to {lineIndex+1:D3} in audio filenames).
        /// -1 indicates no arc line index (e.g., for broadcast lines).
        /// </summary>
        public int ArcLineIndex = -1;

        /// <summary>
        /// The arc section this line belongs to (Intro, Development, Skeptical, Believing, Conclusion).
        /// Used for audio file lookup - belief branch lines have different naming convention.
        /// </summary>
        public ArcSection Section = ArcSection.Intro;

        public DialogueLine() { }

        public DialogueLine(Speaker speaker, string text, DialogueTone tone, ConversationPhase phase, float duration = 0f, int arcLineIndex = -1, ArcSection section = ArcSection.Intro)
        {
            Speaker = speaker;
            Text = text;
            Tone = tone;
            Phase = phase;
            Duration = duration;
            ArcLineIndex = arcLineIndex;
            Section = section;
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
