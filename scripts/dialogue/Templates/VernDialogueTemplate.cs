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
        // Pool shares for topic-blended selection (see GetBlendedTopicLine):
        // (topic / generic "open" / "personal") on topic shows, then
        // (generic / personal) on Open-topic shows.
        private const float DeadAirTopicShare = 0.60f;
        private const float DeadAirGenericShare = 0.25f;
        private const float DeadAirPersonalShare = 0.15f;
        private const float DeadAirOpenGenericShare = 0.60f;
        private const float DeadAirOpenPersonalShare = 0.40f;

        private const float ShowLineTopicShare = 0.80f;
        private const float ShowLineGenericShare = 0.15f;
        private const float ShowLinePersonalShare = 0.05f;
        private const float ShowLineOpenGenericShare = 0.90f;
        private const float ShowLineOpenPersonalShare = 0.10f;

        private const float BreakReturnTopicShare = 0.75f;
        private const float BreakReturnGenericShare = 0.15f;
        private const float BreakReturnPersonalShare = 0.10f;
        private const float BreakReturnOpenGenericShare = 0.85f;
        private const float BreakReturnOpenPersonalShare = 0.15f;

        [Export] private Godot.Collections.Array<DialogueTemplate> _showOpeningLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _introductionLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _showClosingLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _betweenCallersLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _deadAirFillerLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _droppedCallerLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _breakTransitionLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _returnFromBreakLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _offTopicRemarkLines = new Godot.Collections.Array<DialogueTemplate>();
        [Export] private Godot.Collections.Array<DialogueTemplate> _callerCursedLines = new Godot.Collections.Array<DialogueTemplate>();

        public Godot.Collections.Array<DialogueTemplate> ShowOpeningLines => _showOpeningLines;
        public Godot.Collections.Array<DialogueTemplate> IntroductionLines => _introductionLines;
        public Godot.Collections.Array<DialogueTemplate> ShowClosingLines => _showClosingLines;
        public Godot.Collections.Array<DialogueTemplate> BetweenCallersLines => _betweenCallersLines;
        public Godot.Collections.Array<DialogueTemplate> DeadAirFillerLines => _deadAirFillerLines;
        public Godot.Collections.Array<DialogueTemplate> DroppedCallerLines => _droppedCallerLines;
        public Godot.Collections.Array<DialogueTemplate> BreakTransitionLines => _breakTransitionLines;
        public Godot.Collections.Array<DialogueTemplate> ReturnFromBreakLines => _returnFromBreakLines;
        public Godot.Collections.Array<DialogueTemplate> OffTopicRemarkLines => _offTopicRemarkLines;
        public Godot.Collections.Array<DialogueTemplate> CallerCursedLines => _callerCursedLines;

        public void SetShowOpeningLines(DialogueTemplate[] lines) => _showOpeningLines = new Godot.Collections.Array<DialogueTemplate>(lines);
        public void SetIntroductionLines(DialogueTemplate[] lines) => _introductionLines = new Godot.Collections.Array<DialogueTemplate>(lines);
        public void SetShowClosingLines(DialogueTemplate[] lines) => _showClosingLines = new Godot.Collections.Array<DialogueTemplate>(lines);
        public void SetBetweenCallersLines(DialogueTemplate[] lines) => _betweenCallersLines = new Godot.Collections.Array<DialogueTemplate>(lines);
        public void SetDeadAirFillerLines(DialogueTemplate[] lines) => _deadAirFillerLines = new Godot.Collections.Array<DialogueTemplate>(lines);
        public void SetDroppedCallerLines(DialogueTemplate[] lines) => _droppedCallerLines = new Godot.Collections.Array<DialogueTemplate>(lines);
        public void SetBreakTransitionLines(DialogueTemplate[] lines) => _breakTransitionLines = new Godot.Collections.Array<DialogueTemplate>(lines);
        public void SetReturnFromBreakLines(DialogueTemplate[] lines) => _returnFromBreakLines = new Godot.Collections.Array<DialogueTemplate>(lines);
        public void SetOffTopicRemarkLines(DialogueTemplate[] lines) => _offTopicRemarkLines = new Godot.Collections.Array<DialogueTemplate>(lines);
        public void SetCallerCursedLines(DialogueTemplate[] lines) => _callerCursedLines = new Godot.Collections.Array<DialogueTemplate>(lines);

        /// <summary>
        /// Get a show opening line for the specified topic.
        /// Topic shows mostly draw topic-specific openers, with a small blend
        /// of generic ("open") and "personal" lines for variety.
        /// </summary>
        public DialogueTemplate GetShowOpening(ShowTopic topic)
        {
            var topicString = topic.ToTopicName().ToLower();

            var blended = GetBlendedTopicLine(
                _showOpeningLines, topicString,
                ShowLineTopicShare, ShowLineGenericShare, ShowLinePersonalShare,
                ShowLineOpenGenericShare, ShowLineOpenPersonalShare);

            if (blended == null)
            {
                // Fallback to mood-based selection
                return GetShowOpeningFallback(VernMoodType.Neutral);
            }

            return blended;
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
        /// Topic shows mostly draw topic-specific closers, with a small blend
        /// of generic ("open") and "personal" lines for variety.
        /// </summary>
        public DialogueTemplate GetShowClosing(ShowTopic topic)
        {
            var topicString = topic.ToTopicName().ToLower();

            var blended = GetBlendedTopicLine(
                _showClosingLines, topicString,
                ShowLineTopicShare, ShowLineGenericShare, ShowLinePersonalShare,
                ShowLineOpenGenericShare, ShowLineOpenPersonalShare);

            if (blended == null)
            {
                // Fallback to mood-based selection
                return GetShowClosingFallback(VernMoodType.Neutral);
            }

            return blended;
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
        /// Topic shows blend pools: 60% topic-specific, 25% generic (open),
        /// 15% personal anecdotes. Open-topic shows draw only generic (60%)
        /// and personal (40%). If none of the relevant pools have lines,
        /// falls back to a random pick across all filler lines.
        /// </summary>
        public DialogueTemplate GetDeadAirFiller(ShowTopic topic)
        {
            var topicString = topic.ToTopicName().ToLower();

            var blended = GetBlendedTopicLine(
                _deadAirFillerLines, topicString,
                DeadAirTopicShare, DeadAirGenericShare, DeadAirPersonalShare,
                DeadAirOpenGenericShare, DeadAirOpenPersonalShare);

            return blended ?? DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(_deadAirFillerLines));
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
        /// Topic shows mostly draw topic-specific returns, with a blend of
        /// generic ("open") and "personal" lines for variety.
        /// </summary>
        public DialogueTemplate GetReturnFromBreak(ShowTopic topic)
        {
            var topicString = topic.ToTopicName().ToLower();

            var blended = GetBlendedTopicLine(
                _returnFromBreakLines, topicString,
                BreakReturnTopicShare, BreakReturnGenericShare, BreakReturnPersonalShare,
                BreakReturnOpenGenericShare, BreakReturnOpenPersonalShare);

            if (blended == null)
            {
                // Fallback to mood-based selection
                return GetReturnFromBreak(VernMoodType.Neutral);
            }

            return blended;
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

        /// <summary>
        /// Get a caller cursed response line.
        /// </summary>
        public DialogueTemplate GetCallerCursed() => GetCallerCursed(VernMoodType.Neutral);

        /// <summary>
        /// Get a caller cursed response line for the specified mood.
        /// </summary>
        public DialogueTemplate GetCallerCursed(VernMoodType mood)
        {
            var moodString = mood.ToString().ToLower();

            var moodLines = System.Linq.Enumerable.Where(_callerCursedLines, line => line.Mood == moodString);

            if (!moodLines.Any())
            {
                moodLines = System.Linq.Enumerable.Where(_callerCursedLines, line => line.Mood == "neutral");
            }

            if (!moodLines.Any())
            {
                moodLines = _callerCursedLines;
            }

            return DialogueUtility.GetWeightedRandom(System.Linq.Enumerable.ToArray(moodLines));
        }

        /// <summary>
        /// Weighted pick blending a line array's topic-specific pool, its generic
        /// ("open") pool, and its "personal" pool. Empty pools contribute nothing
        /// (shares renormalize). On Open-topic shows only generic and personal
        /// pools are eligible. Template weights still apply within a pool.
        /// Returns null only when every relevant pool is empty, so callers can
        /// keep their legacy fallbacks.
        /// </summary>
        private static DialogueTemplate GetBlendedTopicLine(
            Godot.Collections.Array<DialogueTemplate> lines,
            string topicString,
            float topicShare,
            float genericShare,
            float personalShare,
            float openShowGenericShare,
            float openShowPersonalShare)
        {
            var genericLines = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(lines, line => line.Topic == "open"));
            var personalLines = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(lines, line => line.Topic == "personal"));

            if (topicString == "open")
            {
                if (genericLines.Length == 0 && personalLines.Length == 0)
                {
                    return null;
                }

                return DialogueUtility.GetWeightedRandom(
                    System.Linq.Enumerable.Concat(genericLines, personalLines).ToArray(),
                    line => line.Topic == "personal"
                        ? openShowPersonalShare / personalLines.Length * line.Weight
                        : openShowGenericShare / genericLines.Length * line.Weight);
            }

            var topicLines = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(lines, line => line.Topic == topicString));

            if (topicLines.Length == 0 && genericLines.Length == 0 && personalLines.Length == 0)
            {
                return null;
            }

            return DialogueUtility.GetWeightedRandom(
                topicLines.Concat(genericLines).Concat(personalLines).ToArray(),
                line =>
                {
                    if (line.Topic == topicString) return topicShare / topicLines.Length * line.Weight;
                    if (line.Topic == "open") return genericShare / genericLines.Length * line.Weight;
                    return personalShare / personalLines.Length * line.Weight;
                });
        }
    }
}