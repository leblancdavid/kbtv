using Godot;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Template for Vern's broadcast dialogue lines.
    /// Contains show opening/closing, between-caller transitions, dead air filler, and error handling.
    /// Note: Caller conversations are now handled by the arc-based system (see ArcRepository).
    /// </summary>
    public partial class VernDialogueTemplate : Resource
    {
        [Export] private Godot.Collections.Array<DialogueTemplate> _showOpeningLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _showClosingLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _betweenCallersLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _deadAirFillerLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _droppedCallerLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _breakTransitionLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _offTopicRemarkLines = new Godot.Collections.Array<DialogueTemplate>();

        public Godot.Collections.Array<DialogueTemplate> ShowOpeningLines => _showOpeningLines;
        public Godot.Collections.Array<DialogueTemplate> ShowClosingLines => _showClosingLines;
        public Godot.Collections.Array<DialogueTemplate> BetweenCallersLines => _betweenCallersLines;
        public Godot.Collections.Array<DialogueTemplate> DeadAirFillerLines => _deadAirFillerLines;
        public Godot.Collections.Array<DialogueTemplate> DroppedCallerLines => _droppedCallerLines;
        public Godot.Collections.Array<DialogueTemplate> BreakTransitionLines => _breakTransitionLines;
        public Godot.Collections.Array<DialogueTemplate> OffTopicRemarkLines => _offTopicRemarkLines;

        /// <summary>
        /// Get a show opening line.
        /// </summary>
        public DialogueTemplate GetShowOpening() => DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(_showOpeningLines));

        /// <summary>
        /// Get a show closing line.
        /// </summary>
        public DialogueTemplate GetShowClosing() => DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(_showClosingLines));

        /// <summary>
        /// Get a between-callers transition line.
        /// </summary>
        public DialogueTemplate GetBetweenCallers() => DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(_betweenCallersLines));

        /// <summary>
        /// Get a dead air filler line.
        /// </summary>
        public DialogueTemplate GetDeadAirFiller() => DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(_deadAirFillerLines));

        /// <summary>
        /// Get a dropped caller line (when caller unexpectedly disconnects).
        /// </summary>
        public DialogueTemplate GetDroppedCaller() => DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(_droppedCallerLines));

        /// <summary>
        /// Get a break transition line (when going to ad break).
        /// </summary>
        public DialogueTemplate GetBreakTransition() => DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(_breakTransitionLines));

        /// <summary>
        /// Get an off-topic remark line (when caller goes off-topic).
        /// </summary>
        public DialogueTemplate GetOffTopicRemark() => DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(_offTopicRemarkLines));
    }
}