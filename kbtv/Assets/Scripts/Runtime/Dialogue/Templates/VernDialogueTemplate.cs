using UnityEngine;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Template for Vern's broadcast dialogue lines.
    /// Contains show opening/closing, between-caller transitions, dead air filler, and error handling.
    /// Note: Caller conversations are now handled by the arc-based system (see ArcRepository).
    /// </summary>
    [CreateAssetMenu(fileName = "VernDialogue", menuName = "KBTV/Dialogue/Vern Dialogue Template")]
    public class VernDialogueTemplate : ScriptableObject
    {
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

        [Header("Dropped Caller Lines")]
        [Tooltip("Lines Vern says when a caller unexpectedly disconnects")]
        public DialogueTemplate[] DroppedCallerLines;

        /// <summary>
        /// Get a show opening line.
        /// </summary>
        public DialogueTemplate GetShowOpening() => DialogueUtility.GetWeightedRandom(ShowOpeningLines);

        /// <summary>
        /// Get a show closing line.
        /// </summary>
        public DialogueTemplate GetShowClosing() => DialogueUtility.GetWeightedRandom(ShowClosingLines);

        /// <summary>
        /// Get a between-callers transition line.
        /// </summary>
        public DialogueTemplate GetBetweenCallers() => DialogueUtility.GetWeightedRandom(BetweenCallersLines);

        /// <summary>
        /// Get a dead air filler line.
        /// </summary>
        public DialogueTemplate GetDeadAirFiller() => DialogueUtility.GetWeightedRandom(DeadAirFillerLines);

        /// <summary>
        /// Get a dropped caller line (when caller unexpectedly disconnects).
        /// </summary>
        public DialogueTemplate GetDroppedCaller() => DialogueUtility.GetWeightedRandom(DroppedCallerLines);
    }
}
