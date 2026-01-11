using System;
using System.Collections.Generic;
using UnityEngine;
using KBTV.Callers;

namespace KBTV.Dialogue
{
    /// <summary>
    /// A pre-scripted conversation arc between Vern and a caller.
    /// Contains mood variants and belief branches for dynamic playback.
    /// </summary>
    [Serializable]
    public class ConversationArc
    {
        [SerializeField] private string _arcId;
        [SerializeField] private string _topic;
        [SerializeField] private string _claimedTopic;  // For topic-switcher arcs: what the caller claimed
        [SerializeField] private CallerLegitimacy _legitimacy;
        [SerializeField] private Dictionary<VernMood, ArcMoodVariant> _moodVariants;

        public string ArcId => _arcId;
        public string Topic => _topic;
        public string ClaimedTopic => _claimedTopic;
        public CallerLegitimacy Legitimacy => _legitimacy;

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
            _moodVariants = new Dictionary<VernMood, ArcMoodVariant>();
        }

        /// <summary>
        /// Add a mood variant to this arc.
        /// </summary>
        public void AddMoodVariant(VernMood mood, ArcMoodVariant variant)
        {
            _moodVariants[mood] = variant;
        }

        /// <summary>
        /// Get the mood variant for the specified mood.
        /// Falls back to Neutral if the requested mood isn't available.
        /// </summary>
        public ArcMoodVariant GetMoodVariant(VernMood mood)
        {
            if (_moodVariants.TryGetValue(mood, out var variant))
                return variant;

            // Fallback to Neutral
            if (_moodVariants.TryGetValue(VernMood.Neutral, out var neutralVariant))
                return neutralVariant;

            // Return first available if Neutral not found
            foreach (var v in _moodVariants.Values)
                return v;

            return null;
        }

        /// <summary>
        /// Check if this arc has all required mood variants.
        /// </summary>
        public bool HasAllMoodVariants()
        {
            return _moodVariants.ContainsKey(VernMood.Tired) &&
                   _moodVariants.ContainsKey(VernMood.Grumpy) &&
                   _moodVariants.ContainsKey(VernMood.Neutral) &&
                   _moodVariants.ContainsKey(VernMood.Engaged) &&
                   _moodVariants.ContainsKey(VernMood.Excited);
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
    /// A mood-specific variant of a conversation arc.
    /// Contains all phases with the dialogue tailored to Vern's current mood.
    /// </summary>
    [Serializable]
    public class ArcMoodVariant
    {
        [SerializeField] private List<ArcDialogueLine> _intro;
        [SerializeField] private List<ArcDialogueLine> _development;
        [SerializeField] private ArcBeliefBranch _beliefBranch;
        [SerializeField] private List<ArcDialogueLine> _conclusion;

        public List<ArcDialogueLine> Intro => _intro;
        public List<ArcDialogueLine> Development => _development;
        public ArcBeliefBranch BeliefBranch => _beliefBranch;
        public List<ArcDialogueLine> Conclusion => _conclusion;

        public ArcMoodVariant()
        {
            _intro = new List<ArcDialogueLine>();
            _development = new List<ArcDialogueLine>();
            _beliefBranch = new ArcBeliefBranch();
            _conclusion = new List<ArcDialogueLine>();
        }

        /// <summary>
        /// Assemble all lines for this variant with the specified belief path.
        /// </summary>
        public List<ArcDialogueLine> AssembleLines(BeliefPath beliefPath)
        {
            var lines = new List<ArcDialogueLine>();
            
            lines.AddRange(_intro);
            lines.AddRange(_development);
            
            var branchLines = _beliefBranch?.GetLines(beliefPath);
            if (branchLines != null)
                lines.AddRange(branchLines);
            
            lines.AddRange(_conclusion);
            
            return lines;
        }

        /// <summary>
        /// Get the total number of arc lines across all sections (including BOTH belief paths).
        /// Used for audio preloading to ensure all possible audio files are loaded.
        /// </summary>
        public int GetTotalArcLineCount()
        {
            int count = 0;
            count += _intro?.Count ?? 0;
            count += _development?.Count ?? 0;
            count += _beliefBranch?.Skeptical?.Count ?? 0;
            count += _beliefBranch?.Believing?.Count ?? 0;
            count += _conclusion?.Count ?? 0;
            return count;
        }
    }

    /// <summary>
    /// The belief branch containing both Skeptical and Believing paths.
    /// </summary>
    [Serializable]
    public class ArcBeliefBranch
    {
        [SerializeField] private List<ArcDialogueLine> _skeptical;
        [SerializeField] private List<ArcDialogueLine> _believing;

        public List<ArcDialogueLine> Skeptical => _skeptical;
        public List<ArcDialogueLine> Believing => _believing;

        public ArcBeliefBranch()
        {
            _skeptical = new List<ArcDialogueLine>();
            _believing = new List<ArcDialogueLine>();
        }

        public List<ArcDialogueLine> GetLines(BeliefPath path)
        {
            return path == BeliefPath.Skeptical ? _skeptical : _believing;
        }
    }

    /// <summary>
    /// A single line of dialogue in an arc.
    /// Simpler than DialogueLine - just speaker and text.
    /// </summary>
    [Serializable]
    public class ArcDialogueLine
    {
        [SerializeField] private Speaker _speaker;
        [SerializeField] private string _text;
        [SerializeField] private int _arcLineIndex = -1;

        public Speaker Speaker => _speaker;
        public string Text => _text;
        
        /// <summary>
        /// The original 0-based index of this line within the arc JSON.
        /// Used for audio file lookup. -1 means not set.
        /// </summary>
        public int ArcLineIndex
        {
            get => _arcLineIndex;
            set => _arcLineIndex = value;
        }

        public ArcDialogueLine() { }

        public ArcDialogueLine(Speaker speaker, string text, int arcLineIndex = -1)
        {
            _speaker = speaker;
            _text = text;
            _arcLineIndex = arcLineIndex;
        }

        /// <summary>
        /// Convert to a full DialogueLine with phase and tone information.
        /// </summary>
        public DialogueLine ToDialogueLine(ConversationPhase phase, DialogueTone tone = DialogueTone.Neutral)
        {
            return new DialogueLine(_speaker, _text, tone, phase, 0f, _arcLineIndex);
        }
    }
}
