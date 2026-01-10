using System;
using UnityEngine;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Vern's response type based on his current state and the caller.
    /// </summary>
    public enum VernResponseType
    {
        /// <summary>Standard introduction of caller</summary>
        Introduction,
        /// <summary>Asking for more details</summary>
        Probing,
        /// <summary>Follow-up questions for extended conversations</summary>
        ExtraProbing,
        /// <summary>Skeptical, challenging the caller</summary>
        Skeptical,
        /// <summary>Dismissive, not interested</summary>
        Dismissive,
        /// <summary>Interested, believing</summary>
        Believing,
        /// <summary>Tired, low energy response</summary>
        Tired,
        /// <summary>Annoyed, impatient</summary>
        Annoyed,
        /// <summary>Playful engagement with obvious fake callers</summary>
        Engaging,
        /// <summary>Abrupt cut-off when ending a call early</summary>
        CutOff,
        /// <summary>Wrapping up the call</summary>
        SignOff
    }

    /// <summary>
    /// A collection of Vern's response templates for a specific response type.
    /// </summary>
    [Serializable]
    public class VernResponseSet
    {
        [Tooltip("What type of response these are")]
        public VernResponseType ResponseType;

        [Tooltip("Template lines for this response type")]
        public DialogueTemplate[] Templates;

        /// <summary>
        /// Get a random template from this set, weighted by Weight values.
        /// </summary>
        public DialogueTemplate GetRandomTemplate()
        {
            if (Templates == null || Templates.Length == 0)
                return null;

            float totalWeight = 0f;
            foreach (var template in Templates)
            {
                totalWeight += template.Weight;
            }

            float random = UnityEngine.Random.Range(0f, totalWeight);
            float current = 0f;

            foreach (var template in Templates)
            {
                current += template.Weight;
                if (random <= current)
                    return template;
            }

            return Templates[Templates.Length - 1];
        }
    }

    /// <summary>
    /// Template for Vern's dialogue responses.
    /// Vern's responses are driven by his stats and the caller's legitimacy.
    /// </summary>
    [CreateAssetMenu(fileName = "VernDialogue", menuName = "KBTV/Dialogue/Vern Dialogue Template")]
    public class VernDialogueTemplate : ScriptableObject
    {
        [Header("Introduction Lines")]
        [Tooltip("How Vern introduces callers to the audience")]
        public DialogueTemplate[] IntroductionLines;

        [Header("Probing Lines")]
        [Tooltip("Questions Vern asks to get more details")]
        public DialogueTemplate[] ProbingLines;

        [Header("Extra Probing Lines")]
        [Tooltip("Follow-up questions for extended conversations")]
        public DialogueTemplate[] ExtraProbingLines;

        [Header("Skeptical Lines")]
        [Tooltip("Responses when Vern doubts the caller")]
        public DialogueTemplate[] SkepticalLines;

        [Header("Dismissive Lines")]
        [Tooltip("Responses when Vern is uninterested or annoyed")]
        public DialogueTemplate[] DismissiveLines;

        [Header("Believing Lines")]
        [Tooltip("Responses when Vern finds the caller credible")]
        public DialogueTemplate[] BelievingLines;

        [Header("Tired Lines")]
        [Tooltip("Responses when Vern is low on energy")]
        public DialogueTemplate[] TiredLines;

        [Header("Annoyed Lines")]
        [Tooltip("Responses when Vern is irritated")]
        public DialogueTemplate[] AnnoyedLines;

        [Header("Engaging Lines")]
        [Tooltip("Playful responses when Vern humors obvious fake callers")]
        public DialogueTemplate[] EngagingLines;

        [Header("Cut-Off Lines")]
        [Tooltip("Abrupt endings when Vern cuts a caller short")]
        public DialogueTemplate[] CutOffLines;

        [Header("Sign-Off Lines")]
        [Tooltip("How Vern ends calls")]
        public DialogueTemplate[] SignOffLines;

        [Header("Show Opening Lines")]
        [Tooltip("Lines Vern says when the show goes live")]
        public DialogueTemplate[] ShowOpeningLines;

        [Header("Show Closing Lines")]
        [Tooltip("Lines Vern says when the show ends")]
        public DialogueTemplate[] ShowClosingLines;

        [Header("Between Callers Lines")]
        [Tooltip("Transition lines when moving to the next caller")]
        public DialogueTemplate[] BetweenCallersLines;

        [Header("Dead Air Filler Lines")]
        [Tooltip("Lines Vern says when there are no callers waiting")]
        public DialogueTemplate[] DeadAirFillerLines;

        /// <summary>
        /// Get a random template for the specified response type.
        /// </summary>
        public DialogueTemplate GetResponse(VernResponseType responseType)
        {
            DialogueTemplate[] templates = responseType switch
            {
                VernResponseType.Introduction => IntroductionLines,
                VernResponseType.Probing => ProbingLines,
                VernResponseType.ExtraProbing => ExtraProbingLines,
                VernResponseType.Skeptical => SkepticalLines,
                VernResponseType.Dismissive => DismissiveLines,
                VernResponseType.Believing => BelievingLines,
                VernResponseType.Tired => TiredLines,
                VernResponseType.Annoyed => AnnoyedLines,
                VernResponseType.Engaging => EngagingLines,
                VernResponseType.CutOff => CutOffLines,
                VernResponseType.SignOff => SignOffLines,
                _ => null
            };

            return GetRandomWeighted(templates);
        }

        /// <summary>
        /// Get an introduction line.
        /// </summary>
        public DialogueTemplate GetIntroduction() => GetRandomWeighted(IntroductionLines);

        /// <summary>
        /// Get a probing question.
        /// </summary>
        public DialogueTemplate GetProbingQuestion() => GetRandomWeighted(ProbingLines);

        /// <summary>
        /// Get an extra probing question for extended conversations.
        /// </summary>
        public DialogueTemplate GetExtraProbingQuestion() => GetRandomWeighted(ExtraProbingLines);

        /// <summary>
        /// Get a skeptical response.
        /// </summary>
        public DialogueTemplate GetSkepticalResponse() => GetRandomWeighted(SkepticalLines);

        /// <summary>
        /// Get a dismissive response.
        /// </summary>
        public DialogueTemplate GetDismissiveResponse() => GetRandomWeighted(DismissiveLines);

        /// <summary>
        /// Get a believing response.
        /// </summary>
        public DialogueTemplate GetBelievingResponse() => GetRandomWeighted(BelievingLines);

        /// <summary>
        /// Get a tired response.
        /// </summary>
        public DialogueTemplate GetTiredResponse() => GetRandomWeighted(TiredLines);

        /// <summary>
        /// Get an annoyed response.
        /// </summary>
        public DialogueTemplate GetAnnoyedResponse() => GetRandomWeighted(AnnoyedLines);

        /// <summary>
        /// Get an engaging response (for humoring fake callers).
        /// </summary>
        public DialogueTemplate GetEngagingResponse() => GetRandomWeighted(EngagingLines);

        /// <summary>
        /// Get a cut-off response (for ending calls abruptly).
        /// </summary>
        public DialogueTemplate GetCutOffResponse() => GetRandomWeighted(CutOffLines);

        /// <summary>
        /// Get a sign-off line.
        /// </summary>
        public DialogueTemplate GetSignOff() => GetRandomWeighted(SignOffLines);

        /// <summary>
        /// Get a show opening line.
        /// </summary>
        public DialogueTemplate GetShowOpening() => GetRandomWeighted(ShowOpeningLines);

        /// <summary>
        /// Get a show closing line.
        /// </summary>
        public DialogueTemplate GetShowClosing() => GetRandomWeighted(ShowClosingLines);

        /// <summary>
        /// Get a between-callers transition line.
        /// </summary>
        public DialogueTemplate GetBetweenCallers() => GetRandomWeighted(BetweenCallersLines);

        /// <summary>
        /// Get a dead air filler line.
        /// </summary>
        public DialogueTemplate GetDeadAirFiller() => GetRandomWeighted(DeadAirFillerLines);

        private DialogueTemplate GetRandomWeighted(DialogueTemplate[] templates)
        {
            if (templates == null || templates.Length == 0)
                return null;

            float totalWeight = 0f;
            foreach (var template in templates)
            {
                totalWeight += template.Weight;
            }

            float random = UnityEngine.Random.Range(0f, totalWeight);
            float current = 0f;

            foreach (var template in templates)
            {
                current += template.Weight;
                if (random <= current)
                    return template;
            }

            return templates[templates.Length - 1];
        }
    }
}
