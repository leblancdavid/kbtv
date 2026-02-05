using Godot;
using KBTV.Data;
using System.Linq;
using KBTV.Callers;

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
        [Export] private Godot.Collections.Array<DialogueTemplate> _introductionLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _showClosingLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _betweenCallersLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _deadAirFillerLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _droppedCallerLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _breakTransitionLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _returnFromBreakLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _offTopicRemarkLines = new Godot.Collections.Array<DialogueTemplate>();

        public Godot.Collections.Array<DialogueTemplate> ShowOpeningLines => _showOpeningLines;
        public Godot.Collections.Array<DialogueTemplate> IntroductionLines => _introductionLines;
        public Godot.Collections.Array<DialogueTemplate> ShowClosingLines => _showClosingLines;
        public Godot.Collections.Array<DialogueTemplate> BetweenCallersLines => _betweenCallersLines;
        public Godot.Collections.Array<DialogueTemplate> DeadAirFillerLines => _deadAirFillerLines;
        public Godot.Collections.Array<DialogueTemplate> DroppedCallerLines => _droppedCallerLines;
        public Godot.Collections.Array<DialogueTemplate> BreakTransitionLines => _breakTransitionLines;
        public Godot.Collections.Array<DialogueTemplate> ReturnFromBreakLines => _returnFromBreakLines;
        public Godot.Collections.Array<DialogueTemplate> OffTopicRemarkLines => _offTopicRemarkLines;

        public void SetShowOpeningLines(DialogueTemplate[] lines) => _showOpeningLines = new Godot.Collections.Array<DialogueTemplate>(lines);
        public void SetIntroductionLines(DialogueTemplate[] lines) => _introductionLines = new Godot.Collections.Array<DialogueTemplate>(lines);
        public void SetShowClosingLines(DialogueTemplate[] lines) => _showClosingLines = new Godot.Collections.Array<DialogueTemplate>(lines);
        public void SetBetweenCallersLines(DialogueTemplate[] lines) => _betweenCallersLines = new Godot.Collections.Array<DialogueTemplate>(lines);
        public void SetDeadAirFillerLines(DialogueTemplate[] lines) => _deadAirFillerLines = new Godot.Collections.Array<DialogueTemplate>(lines);
        public void SetDroppedCallerLines(DialogueTemplate[] lines) => _droppedCallerLines = new Godot.Collections.Array<DialogueTemplate>(lines);
        public void SetBreakTransitionLines(DialogueTemplate[] lines) => _breakTransitionLines = new Godot.Collections.Array<DialogueTemplate>(lines);
        public void SetReturnFromBreakLines(DialogueTemplate[] lines) => _returnFromBreakLines = new Godot.Collections.Array<DialogueTemplate>(lines);
        public void SetOffTopicRemarkLines(DialogueTemplate[] lines) => _offTopicRemarkLines = new Godot.Collections.Array<DialogueTemplate>(lines);

        /// <summary>
        /// Get a show opening line for the specified topic.
        /// </summary>
        public DialogueTemplate GetShowOpening(ShowTopic topic)
        {
            var topicString = topic.ToTopicName().ToLower();

            var topicLines = System.Linq.Enumerable.Where(_showOpeningLines, line => line.Topic == topicString);

            if (!topicLines.Any())
            {
                // Fallback to mood-based selection
                return GetShowOpeningFallback(VernMoodType.Neutral);
            }

            return DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(topicLines));
        }

        /// <summary>
        /// Get a show opening line for the specified mood (fallback).
        /// </summary>
        public DialogueTemplate GetShowOpeningFallback(VernMoodType mood)
        {
            var moodString = mood.ToString().ToLower();

            var moodLines = System.Linq.Enumerable.Where(_showOpeningLines, line => line.Mood == moodString);

            if (!moodLines.Any())
            {
                moodLines = System.Linq.Enumerable.Where(_showOpeningLines, line => line.Mood == "neutral");
            }

            if (!moodLines.Any())
            {
                moodLines = _showOpeningLines;
            }

            return DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(moodLines));
        }

        /// <summary>
        /// Get a show opening line.
        /// </summary>
        public DialogueTemplate GetShowOpening() => GetShowOpening(ShowTopic.Ghosts); // Default fallback

        /// <summary>
        /// Get an introduction line for a caller.
        /// </summary>
        public DialogueTemplate GetIntroduction() => DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(_introductionLines));

        /// <summary>
        /// Get a show closing line for the specified topic.
        /// </summary>
        public DialogueTemplate GetShowClosing(ShowTopic topic)
        {
            var topicString = topic.ToTopicName().ToLower();

            var topicLines = System.Linq.Enumerable.Where(_showClosingLines, line => line.Topic == topicString);

            if (!topicLines.Any())
            {
                // Fallback to mood-based selection
                return GetShowClosingFallback(VernMoodType.Neutral);
            }

            return DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(topicLines));
        }

        /// <summary>
        /// Get a show closing line.
        /// </summary>
        public DialogueTemplate GetShowClosing() => GetShowClosing(ShowTopic.Ghosts); // Default fallback

        /// <summary>
        /// Get a show closing line for the specified mood (fallback).
        /// </summary>
        public DialogueTemplate GetShowClosingFallback(VernMoodType mood)
        {
            var moodString = mood.ToString().ToLower();

            var moodLines = System.Linq.Enumerable.Where(_showClosingLines, line => line.Mood == moodString);

            if (!moodLines.Any())
            {
                moodLines = System.Linq.Enumerable.Where(_showClosingLines, line => line.Mood == "neutral");
            }

            if (!moodLines.Any())
            {
                moodLines = _showClosingLines;
            }

            return DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(moodLines));
        }

        /// <summary>
        /// Get a between-callers transition line.
        /// </summary>
        public DialogueTemplate GetBetweenCallers() => GetBetweenCallers(VernMoodType.Neutral);

        /// <summary>
        /// Get a between-callers transition line for the specified mood.
        /// </summary>
        public DialogueTemplate GetBetweenCallers(VernMoodType mood)
        {
            var moodString = mood.ToString().ToLower();

            var moodLines = System.Linq.Enumerable.Where(_betweenCallersLines, line => line.Mood == moodString);

            if (!moodLines.Any())
            {
                moodLines = System.Linq.Enumerable.Where(_betweenCallersLines, line => line.Mood == "neutral");
            }

            if (!moodLines.Any())
            {
                moodLines = _betweenCallersLines;
            }

            return DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(moodLines));
        }

        /// <summary>
        /// Get a dead air filler line for the specified topic.
        /// </summary>
        public DialogueTemplate GetDeadAirFiller(ShowTopic topic)
        {
            var topicString = topic.ToTopicName().ToLower();

            var topicLines = System.Linq.Enumerable.Where(_deadAirFillerLines, line => line.Topic == topicString);

            if (!topicLines.Any())
            {
                // Fallback to random selection
                return DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(_deadAirFillerLines));
            }

            return DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(topicLines));
        }

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
        /// Get a return from break line for the specified topic.
        /// </summary>
        public DialogueTemplate GetReturnFromBreak(ShowTopic topic)
        {
            var topicString = topic.ToTopicName().ToLower();

            var topicLines = System.Linq.Enumerable.Where(_returnFromBreakLines, line => line.Topic == topicString);

            if (!topicLines.Any())
            {
                // Fallback to mood-based selection
                return GetReturnFromBreak(VernMoodType.Neutral);
            }

            return DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(topicLines));
        }

        /// <summary>
        /// Get a return from break line.
        /// </summary>
        public DialogueTemplate GetReturnFromBreak() => GetReturnFromBreak(VernMoodType.Neutral);

        /// <summary>
        /// Get a return from break line for the specified mood.
        /// </summary>
        public DialogueTemplate GetReturnFromBreak(VernMoodType mood)
        {
            var moodString = mood.ToString().ToLower();

            var moodLines = System.Linq.Enumerable.Where(_returnFromBreakLines, line => line.Mood == moodString);

            if (!moodLines.Any())
            {
                moodLines = System.Linq.Enumerable.Where(_returnFromBreakLines, line => line.Mood == "neutral");
            }

            if (!moodLines.Any())
            {
                moodLines = _returnFromBreakLines;
            }

            return DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(moodLines));
        }

        /// <summary>
        /// Get an off-topic remark line.
        /// </summary>
        public DialogueTemplate GetOffTopicRemark() => GetOffTopicRemark(VernMoodType.Neutral);

        /// <summary>
        /// Get an off-topic remark line for the specified mood.
        /// </summary>
        public DialogueTemplate GetOffTopicRemark(VernMoodType mood)
        {
            var moodString = mood.ToString().ToLower();

            // Filter lines by mood, fallback to neutral if no matches
            var moodLines = System.Linq.Enumerable.Where(_offTopicRemarkLines, line => line.Mood == moodString);

            if (!moodLines.Any())
            {
                moodLines = System.Linq.Enumerable.Where(_offTopicRemarkLines, line => line.Mood == "neutral");
            }

            if (!moodLines.Any())
            {
                moodLines = _offTopicRemarkLines;
            }

            return DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(moodLines));
        }
    }
}