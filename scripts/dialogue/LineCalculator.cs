using System;
using Godot;
using KBTV.Ads;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Data;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Handles calculation of broadcast lines for different states.
    /// Extracted from BroadcastCoordinator to reduce file size and improve maintainability.
    /// </summary>
    public class LineCalculator
    {
        private readonly AdBreakCoordinator _adCoordinator;
        private readonly VernDialogueTemplate _vernDialogue;

        public LineCalculator(AdBreakCoordinator adCoordinator, VernDialogueTemplate vernDialogue)
        {
            _adCoordinator = adCoordinator;
            _vernDialogue = vernDialogue;
        }

        public BroadcastLine GetMusicLine()
        {
            return BroadcastLine.Music();
        }

        public BroadcastLine GetShowOpeningLine(VernDialogueTemplate vernDialogue)
        {
            var line = vernDialogue.GetShowOpening();
            return line != null ? BroadcastLine.ShowOpening(line.Text, line.Id) : BroadcastLine.None();
        }

        public BroadcastLine GetBetweenCallersLine(VernDialogueTemplate vernDialogue)
        {
            // Get Vern's mood for between-callers line selection
            var vernStats = ServiceRegistry.Instance.GameStateManager?.VernStats;
            var mood = vernStats?.CurrentMoodType ?? VernMoodType.Neutral;

            var line = vernDialogue.GetBetweenCallers(mood);
            return line != null ? BroadcastLine.BetweenCallers(line.Text, line.Id) : BroadcastLine.None();
        }

        public BroadcastLine GetFillerLine(VernDialogueTemplate vernDialogue)
        {
            var line = vernDialogue.GetDeadAirFiller();
            return line != null ? BroadcastLine.DeadAirFiller(line.Text, line.Id) : BroadcastLine.None();
        }

        public BroadcastLine GetOffTopicRemarkLine(VernDialogueTemplate vernDialogue)
        {
            var line = vernDialogue.GetOffTopicRemark();
            return line != null ? BroadcastLine.OffTopicRemark(line.Text, line.Id) : BroadcastLine.None();
        }

        public BroadcastLine GetShowClosingLine(VernDialogueTemplate vernDialogue)
        {
            var line = vernDialogue.GetShowClosing();
            return line != null ? BroadcastLine.ShowClosing(line.Text, line.Id) : BroadcastLine.None();
        }
    }
}