using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KBTV.Callers;
using KBTV.Data;

namespace KBTV.Dialogue
{
    /// <summary>
    /// A pre-scripted conversation arc between Vern and a caller.
    /// Contains a flat dialogue array where Vern's lines have mood-specific text variants.
    /// First line is always Vern's intro, last line is always Vern's conclusion.
    /// </summary>
    public partial class ConversationArc : Resource
    {
        [Export] private string _arcId;
        [Export] private string _topic;
        [Export] private string _claimedTopic;
        [Export] private CallerLegitimacy _legitimacy;
        [Export] private Godot.Collections.Array<ArcDialogueLine> _dialogue = new Godot.Collections.Array<ArcDialogueLine>();
        [Export] private string _callerPersonality;
        [Export] private string _screeningSummary;

        public string ArcId => _arcId;
        public string Topic => _topic;
        public string ClaimedTopic => _claimedTopic;
        public CallerLegitimacy Legitimacy => _legitimacy;
        public string CallerPersonality => _callerPersonality;
        public string ScreeningSummary => _screeningSummary;

        /// <summary>
        /// The dialogue lines for this arc. First line is Vern's intro, last is Vern's conclusion.
        /// </summary>
        public Godot.Collections.Array<ArcDialogueLine> Dialogue => _dialogue;

        /// <summary>
        /// True if this is a topic-switcher arc (has a claimed topic different from actual topic).
        /// </summary>
        public bool IsTopicSwitcher => !string.IsNullOrEmpty(_claimedTopic);

        public ConversationArc(string arcId, string topic, CallerLegitimacy legitimacy, string claimedTopic = null)
        {
            _arcId = arcId;
            _topic = topic;
            _claimedTopic = claimedTopic;
            _legitimacy = legitimacy;
        }

        /// <summary>
        /// Set the screening summary for this arc.
        /// </summary>
        public void SetScreeningSummary(string summary)
        {
            _screeningSummary = summary;
        }

        /// <summary>
        /// Set the caller personality for this arc.
        /// </summary>
        public void SetCallerPersonality(string personality)
        {
            _callerPersonality = personality;
        }

        /// <summary>
        /// Set the dialogue lines for this arc.
        /// </summary>
        public void SetDialogue(List<ArcDialogueLine> dialogue)
        {
            _dialogue.Clear();
            if (dialogue != null)
            {
                foreach (var line in dialogue)
                {
                    _dialogue.Add(line);
                }
            }
        }

        /// <summary>
        /// Check if this arc has valid dialogue.
        /// </summary>
        public bool HasDialogue()
        {
            return _dialogue != null && _dialogue.Count > 0;
        }

        /// <summary>
        /// Get the dialogue lines for a specific Vern mood type.
        /// For Vern lines, selects the appropriate text variant based on mood.
        /// For Caller lines, returns the single text.
        /// </summary>
        /// <param name="mood">The current Vern mood type</param>
        /// <returns>List of ArcDialogueLine with Vern's text selected for the mood</returns>
        public List<ArcDialogueLine> GetDialogueForMood(VernMoodType mood)
        {
            if (_dialogue == null)
                return new List<ArcDialogueLine>();

            string moodKey = mood.ToString().ToLowerInvariant();

            return _dialogue.Select(line =>
            {
                // For Vern lines, select the text variant for this mood
                if (line.Speaker == Speaker.Vern && line.TextVariants != null && line.TextVariants.Count > 0)
                {
                    if (line.TextVariants.TryGetValue(moodKey, out string moodText))
                    {
                        return new ArcDialogueLine(line.Speaker, moodText, line.TextVariants, line.ArcLineIndex, line.Section);
                    }
                    // Fallback to first available variant if mood not found
                    var firstVariant = line.TextVariants.First();
                    return new ArcDialogueLine(line.Speaker, firstVariant.Value, line.TextVariants, line.ArcLineIndex, line.Section);
                }

                // Caller lines - return as-is
                if (line.Speaker == Speaker.Vern)
                {
                    GD.Print($"[ConversationArc] Vern line {line.ArcLineIndex} has no TextVariants or empty TextVariants");
                }
                return line;
            }).ToList();
        }

        /// <summary>
        /// Get all Vern lines in this arc (for audio preloading).
        /// Returns all mood variants for each Vern line.
        /// </summary>
        public List<ArcDialogueLine> GetAllVernLines()
        {
            return _dialogue?.Where(line => line.Speaker == Speaker.Vern).ToList() ?? new List<ArcDialogueLine>();
        }

        /// <summary>
        /// Get all Caller lines in this arc.
        /// </summary>
        public List<ArcDialogueLine> GetAllCallerLines()
        {
            return _dialogue?.Where(line => line.Speaker == Speaker.Caller).ToList() ?? new List<ArcDialogueLine>();
        }

        /// <summary>
        /// Check if this arc matches the given topic and legitimacy.
        /// </summary>
        public bool Matches(string topic, CallerLegitimacy legitimacy)
        {
            bool legitimacyMatch = _legitimacy == legitimacy;
            bool topicMatch = string.IsNullOrEmpty(_topic) ||
                               string.Equals(_topic, topic, StringComparison.OrdinalIgnoreCase);
            return legitimacyMatch && topicMatch;
        }

        /// <summary>
        /// Check if this arc matches as a topic-switcher arc.
        /// Used when a caller lied about their topic during screening.
        /// </summary>
        public bool MatchesTopicSwitcher(string claimedTopic, string actualTopic, CallerLegitimacy legitimacy)
        {
            if (!IsTopicSwitcher) return false;

            bool legitimacyMatch = _legitimacy == legitimacy;
            bool claimedMatch = string.Equals(_claimedTopic, claimedTopic, StringComparison.OrdinalIgnoreCase);
            bool actualMatch = string.Equals(_topic, actualTopic, StringComparison.OrdinalIgnoreCase);

            return legitimacyMatch && claimedMatch && actualMatch;
        }
    }

    /// <summary>
    /// A single line of dialogue in an arc.
    /// For Vern: contains TextVariants with mood-specific text.
    /// For Caller: contains single Text field.
    /// </summary>
    public partial class ArcDialogueLine : Resource
    {
        [Export] private Speaker _speaker;
        [Export] private string _text;
        [Export] private Godot.Collections.Dictionary<string, string> _textVariants = new Godot.Collections.Dictionary<string, string>();
        [Export] private int _arcLineIndex = -1;
        [Export] private ArcSection _section = ArcSection.Intro;

        public Speaker Speaker => _speaker;
        public string Text => _text;

        /// <summary>
        /// Mood-specific text variants for Vern's lines.
        /// Keys: "neutral", "tired", "energized", "irritated", "gruff", "amused", "focused"
        /// For Caller lines, this dictionary is empty.
        /// </summary>
        public Godot.Collections.Dictionary<string, string> TextVariants => _textVariants;

        /// <summary>
        /// The original 0-based index of this line within the arc JSON.
        /// Used for audio file lookup. -1 means not set.
        /// </summary>
        public int ArcLineIndex
        {
            get => _arcLineIndex;
            set => _arcLineIndex = value;
        }

        /// <summary>
        /// The arc section this line belongs to (Intro, Development, Conclusion).
        /// </summary>
        public ArcSection Section
        {
            get => _section;
            set => _section = value;
        }

        public ArcDialogueLine() { }

        /// <summary>
        /// Create a caller line (single text, no variants).
        /// </summary>
        public ArcDialogueLine(Speaker speaker, string text, int arcLineIndex = -1, ArcSection section = ArcSection.Intro)
        {
            _speaker = speaker;
            _text = text;
            _arcLineIndex = arcLineIndex;
            _section = section;
        }

        /// <summary>
        /// Create a Vern line with a specific text selected from variants.
        /// Preserves the original variants dictionary for reference.
        /// </summary>
        public ArcDialogueLine(Speaker speaker, string text, Godot.Collections.Dictionary<string, string> textVariants,
            int arcLineIndex = -1, ArcSection section = ArcSection.Intro)
        {
            _speaker = speaker;
            _text = text;
            _textVariants = textVariants ?? new Godot.Collections.Dictionary<string, string>();
            _arcLineIndex = arcLineIndex;
            _section = section;
        }

        /// <summary>
        /// Create a Vern line with mood variants.
        /// </summary>
        public static ArcDialogueLine CreateVernLine(Godot.Collections.Dictionary<string, string> textVariants, int arcLineIndex = -1, ArcSection section = ArcSection.Intro)
        {
            var line = new ArcDialogueLine
            {
                _speaker = Speaker.Vern,
                _textVariants = textVariants,
                _arcLineIndex = arcLineIndex,
                _section = section
            };
            // Set Text to the neutral variant as a fallback/default
            if (textVariants.TryGetValue("neutral", out string neutralText))
            {
                line._text = neutralText;
            }
            else if (textVariants.Count > 0)
            {
                // Get first variant as fallback
                foreach (var kvp in textVariants)
                {
                    line._text = kvp.Value;
                    break;
                }
            }
            return line;
        }

        /// <summary>
        /// Convert to a full DialogueLine.
        /// </summary>
        public DialogueLine ToDialogueLine(ConversationPhase phase)
        {
            return new DialogueLine { Speaker = _speaker, Text = _text, Phase = phase };
        }

        /// <summary>
        /// Convert to a full DialogueLine with specified text (for mood variants).
        /// </summary>
        public DialogueLine ToDialogueLine(string actualText, ConversationPhase phase)
        {
            return new DialogueLine { Speaker = _speaker, Text = actualText, Phase = phase };
        }
    }
}