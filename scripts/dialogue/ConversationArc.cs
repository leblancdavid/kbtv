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
        [Export] private ShowTopic _topic;
        [Export] private ShowTopic _claimedTopic;
        [Export] private bool _hasClaimedTopic;
        [Export] private CallerLegitimacy _legitimacy;
        [Export] private string _callerGender;
        [Export] private Godot.Collections.Array<ArcDialogueLine> _dialogue = new Godot.Collections.Array<ArcDialogueLine>();
        [Export] private string _callerPersonality;
        [Export] private string _screeningSummary;

        public string ArcId => _arcId;
        public ShowTopic Topic => _topic;
        public ShowTopic? ClaimedTopic => _hasClaimedTopic ? _claimedTopic : null;
        public CallerLegitimacy Legitimacy => _legitimacy;
        public string CallerGender => _callerGender;
        public string CallerPersonality => _callerPersonality;
        public string ScreeningSummary => _screeningSummary;

        /// <summary>
        /// The topic as a string for debugging/display purposes.
        /// </summary>
        public string TopicName => _topic.ToTopicName();

        /// <summary>
        /// The claimed topic as a string for debugging/display purposes.
        /// </summary>
        public string ClaimedTopicName => _hasClaimedTopic ? _claimedTopic.ToTopicName() : "";

        /// <summary>
        /// True if this is a topic-switcher arc (has a claimed topic different from actual topic).
        /// </summary>
        public bool IsTopicSwitcher => _hasClaimedTopic;

        public ConversationArc(string arcId, ShowTopic topic, CallerLegitimacy legitimacy, string callerGender = "male", ShowTopic? claimedTopic = null)
        {
            _arcId = arcId;
            _topic = topic;
            _callerGender = callerGender;
            _claimedTopic = claimedTopic ?? ShowTopic.Ghosts;
            _hasClaimedTopic = claimedTopic.HasValue;
            _legitimacy = legitimacy;
        }

        /// <summary>
        /// The dialogue lines for this arc. First line is Vern's intro, last is Vern's conclusion.
        /// </summary>
        public Godot.Collections.Array<ArcDialogueLine> Dialogue => _dialogue;

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
        /// Get the dialogue lines. In the new schema, mood selection happens at playback time.
        /// </summary>
        /// <param name="mood">The current Vern mood type (ignored in new schema)</param>
        /// <returns>List of ArcDialogueLine</returns>
        public List<ArcDialogueLine> GetDialogueForMood(VernMoodType mood)
        {
            if (_dialogue == null)
                return new List<ArcDialogueLine>();

            // In the new schema, each line already has the appropriate audio id
            // Mood selection happens in BroadcastStateManager
            return _dialogue.ToList();
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
        public bool Matches(ShowTopic topic, CallerLegitimacy legitimacy)
        {
            bool legitimacyMatch = _legitimacy == legitimacy;
            bool topicMatch = _topic == topic;
            return legitimacyMatch && topicMatch;
        }

        /// <summary>
        /// Check if this arc matches as a topic-switcher arc.
        /// Used when a caller lied about their topic during screening.
        /// </summary>
        public bool MatchesTopicSwitcher(ShowTopic claimedTopic, ShowTopic actualTopic, CallerLegitimacy legitimacy)
        {
            if (!IsTopicSwitcher) return false;

            bool legitimacyMatch = _legitimacy == legitimacy;
            bool claimedMatch = _claimedTopic == claimedTopic;
            bool actualMatch = _topic == actualTopic;

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
        [Export] private Godot.Collections.Dictionary<string, string> _audioIds = new Godot.Collections.Dictionary<string, string>();
        [Export] private int _arcLineIndex = -1;
        [Export] private ArcSection _section = ArcSection.Intro;
        [Export] private string _audioId = "";

        public Speaker Speaker => _speaker;
        public string Text => _text;
        public string AudioId => _audioId;
        public Godot.Collections.Dictionary<string, string> TextVariants => _textVariants;
        public Godot.Collections.Dictionary<string, string> AudioIds => _audioIds;

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
        public ArcDialogueLine(Speaker speaker, string text, string audioId = "", int arcLineIndex = -1, ArcSection section = ArcSection.Intro)
        {
            _speaker = speaker;
            _text = text;
            _audioId = audioId;
            _arcLineIndex = arcLineIndex;
            _section = section;
        }

        /// <summary>
        /// Create a Vern line with a specific text selected from variants.
        /// Preserves the original variants dictionary for reference.
        /// </summary>
        public ArcDialogueLine(Speaker speaker, string text, Godot.Collections.Dictionary<string, string> textVariants,
            Godot.Collections.Dictionary<string, string> audioIds = null,
            int arcLineIndex = -1, ArcSection section = ArcSection.Intro)
        {
            _speaker = speaker;
            _text = text;
            _textVariants = textVariants ?? new Godot.Collections.Dictionary<string, string>();
            _audioIds = audioIds ?? new Godot.Collections.Dictionary<string, string>();
            _arcLineIndex = arcLineIndex;
            _section = section;
        }

        /// <summary>
        /// Create a line with specified speaker and audio id.
        /// </summary>
        public static ArcDialogueLine CreateLine(Speaker speaker, string text, string audioId, int arcLineIndex = -1, ArcSection section = ArcSection.Intro)
        {
            var line = new ArcDialogueLine
            {
                _speaker = speaker,
                _text = text,
                _audioId = audioId,
                _arcLineIndex = arcLineIndex,
                _section = section
            };
            return line;
        }

        /// <summary>
        /// Create a Vern line with audio id.
        /// </summary>
        public static ArcDialogueLine CreateVernLine(string text, string audioId, int arcLineIndex = -1, ArcSection section = ArcSection.Intro)
        {
            return CreateLine(Speaker.Vern, text, audioId, arcLineIndex, section);
        }

        /// <summary>
        /// Create a Vern line with mood variants and audio IDs.
        /// </summary>
        public static ArcDialogueLine CreateVernLineWithVariants(
            Godot.Collections.Dictionary<string, string> textVariants,
            Godot.Collections.Dictionary<string, string> audioIds,
            string defaultText = "",
            string defaultAudioId = "",
            int arcLineIndex = -1,
            ArcSection section = ArcSection.Intro)
        {
            var line = new ArcDialogueLine
            {
                _speaker = Speaker.Vern,
                _text = defaultText,
                _audioId = defaultAudioId,
                _textVariants = textVariants ?? new Godot.Collections.Dictionary<string, string>(),
                _audioIds = audioIds ?? new Godot.Collections.Dictionary<string, string>(),
                _arcLineIndex = arcLineIndex,
                _section = section
            };
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