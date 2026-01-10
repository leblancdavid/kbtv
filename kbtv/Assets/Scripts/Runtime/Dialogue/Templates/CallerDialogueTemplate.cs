using System;
using UnityEngine;
using KBTV.Callers;

namespace KBTV.Dialogue
{
    /// <summary>
    /// A template line that can be used to generate dialogue.
    /// Supports text substitution placeholders like {callerName}, {location}, {topic}.
    /// </summary>
    [Serializable]
    public class DialogueTemplate
    {
        [TextArea(2, 4)]
        [Tooltip("The dialogue text. Use placeholders: {callerName}, {location}, {topic}, {reason}")]
        public string Text;

        [Tooltip("The emotional tone of this line")]
        public DialogueTone Tone = DialogueTone.Neutral;

        [Tooltip("Relative weight for random selection (higher = more likely)")]
        [Range(0.1f, 3f)]
        public float Weight = 1f;

        /// <summary>
        /// Apply substitutions to the template text.
        /// </summary>
        public string ApplySubstitutions(Caller caller, string topicName = null)
        {
            if (string.IsNullOrEmpty(Text))
                return Text;

            string result = Text;
            
            if (caller != null)
            {
                result = result.Replace("{callerName}", caller.Name ?? "caller");
                result = result.Replace("{location}", caller.Location ?? "somewhere");
                result = result.Replace("{reason}", caller.CallReason ?? "something strange");
                result = result.Replace("{topic}", topicName ?? caller.ClaimedTopic ?? "the paranormal");
            }

            return result;
        }
    }

    /// <summary>
    /// Template for generating caller dialogue based on topic and legitimacy.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCallerDialogue", menuName = "KBTV/Dialogue/Caller Dialogue Template")]
    public class CallerDialogueTemplate : ScriptableObject
    {
        [Header("Matching Criteria")]
        [Tooltip("Topic this template is for (null = any topic)")]
        public Topic Topic;

        [Tooltip("Legitimacy level this template is for")]
        public CallerLegitimacy Legitimacy = CallerLegitimacy.Credible;

        [Tooltip("Priority when multiple templates match (higher = preferred)")]
        public int Priority = 0;

        [Header("Conversation Settings")]
        [Tooltip("How long this conversation should be")]
        public ConversationLength Length = ConversationLength.Standard;

        [Header("Intro Phase - Initial claim/statement")]
        [Tooltip("What the caller says when first introduced")]
        public DialogueTemplate[] IntroLines;

        [Header("Probe Phase - Elaboration")]
        [Tooltip("How the caller elaborates when asked for details")]
        public DialogueTemplate[] DetailLines;

        [Header("Challenge Phase - Defense/Reaction")]
        [Tooltip("How the caller responds to skepticism")]
        public DialogueTemplate[] DefenseLines;

        [Tooltip("How the caller responds to belief/acceptance")]
        public DialogueTemplate[] AcceptanceLines;

        [Header("Extended Content (for Extended/Long conversations)")]
        [Tooltip("Additional story details for extended conversations")]
        public DialogueTemplate[] ExtraDetailLines;

        [Tooltip("Additional defense/elaboration for long conversations")]
        public DialogueTemplate[] ExtraDefenseLines;

        [Header("Resolution Phase - Conclusion")]
        [Tooltip("How the caller wraps up the call")]
        public DialogueTemplate[] ConclusionLines;

        /// <summary>
        /// Get a random intro line.
        /// </summary>
        public DialogueTemplate GetIntroLine() => DialogueUtility.GetWeightedRandom(IntroLines);

        /// <summary>
        /// Get a random detail/elaboration line.
        /// </summary>
        public DialogueTemplate GetDetailLine() => DialogueUtility.GetWeightedRandom(DetailLines);

        /// <summary>
        /// Get a random defense line (for when Vern is skeptical).
        /// </summary>
        public DialogueTemplate GetDefenseLine() => DialogueUtility.GetWeightedRandom(DefenseLines);

        /// <summary>
        /// Get a random acceptance response line (for when Vern believes).
        /// </summary>
        public DialogueTemplate GetAcceptanceLine() => DialogueUtility.GetWeightedRandom(AcceptanceLines);

        /// <summary>
        /// Get a random conclusion line.
        /// </summary>
        public DialogueTemplate GetConclusionLine() => DialogueUtility.GetWeightedRandom(ConclusionLines);

        /// <summary>
        /// Get a random extra detail line (for extended conversations).
        /// </summary>
        public DialogueTemplate GetExtraDetailLine() => DialogueUtility.GetWeightedRandom(ExtraDetailLines);

        /// <summary>
        /// Get a random extra defense line (for long conversations).
        /// </summary>
        public DialogueTemplate GetExtraDefenseLine() => DialogueUtility.GetWeightedRandom(ExtraDefenseLines);

        /// <summary>
        /// Check if this template matches the given criteria.
        /// </summary>
        public bool Matches(Topic topic, CallerLegitimacy legitimacy)
        {
            // Legitimacy must match
            if (Legitimacy != legitimacy)
                return false;

            // If no topic specified, matches any
            if (Topic == null)
                return true;

            // Topic must match if specified
            return topic != null && Topic.TopicId == topic.TopicId;
        }
    }
}
