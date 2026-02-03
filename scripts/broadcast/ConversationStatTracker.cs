using System.Collections.Generic;
using KBTV.Callers;
using KBTV.Data;
using KBTV.Dialogue;
using KBTV.Core;
using Godot;

namespace KBTV.Broadcast
{
    /// <summary>
    /// Tracks and applies caller stat effects gradually during conversations.
    /// Effects are applied proportionally after each completed line.
    /// </summary>
    public class ConversationStatTracker
    {
        private Dictionary<StatType, float> _totalEffects;
        private int _totalLines;
        private int _completedLines;
        private Caller _currentCaller;
        private VernStats? _vernStats;

        public ConversationStatTracker(GameStateManager gameStateManager)
        {
            _vernStats = gameStateManager?.VernStats;
        }

        /// <summary>
        /// Initialize tracking for a new conversation.
        /// </summary>
        public void StartConversation(Caller caller, ConversationArc arc)
        {
            _currentCaller = caller;
            _totalEffects = caller.GetTotalStatEffects();
            _totalLines = arc.Dialogue.Count;
            _completedLines = 0;
        }

        /// <summary>
        /// Apply effects for a completed line.
        /// </summary>
        public void OnLineCompleted()
        {
            if (_completedLines >= _totalLines) return;

            _completedLines++;
            float progress = (float)_completedLines / _totalLines;
            float previousProgress = (float)(_completedLines - 1) / _totalLines;

            // Apply incremental effects for this line
            foreach (var (statType, totalAmount) in _totalEffects)
            {
                float incrementalAmount = totalAmount * (progress - previousProgress);
                if (incrementalAmount != 0)
                {
                    ApplyStatEffect(statType, incrementalAmount);
                }
            }
        }

        /// <summary>
        /// Handle conversation interruption (no action needed - effects already applied).
        /// </summary>
        public void InterruptConversation()
        {
            // Effects up to _completedLines are already applied
        }

        /// <summary>
        /// Ensure all effects are applied when conversation completes naturally.
        /// </summary>
        public void CompleteConversation()
        {
            // Apply any remaining effects due to rounding
            float finalProgress = (float)_completedLines / _totalLines;
            if (finalProgress < 1.0f)
            {
                foreach (var (statType, totalAmount) in _totalEffects)
                {
                    float appliedAmount = totalAmount * finalProgress;
                    float remainingAmount = totalAmount - appliedAmount;
                    if (remainingAmount != 0)
                    {
                        ApplyStatEffect(statType, remainingAmount);
                    }
                }
            }
        }

        private void ApplyStatEffect(StatType statType, float amount)
        {
            if (_vernStats == null)
            {
                GD.PrintErr($"ConversationStatTracker.ApplyStatEffect: VernStats is null - cannot apply stat effect {statType}: {amount}");
                return;
            }

            var effects = new Dictionary<StatType, float> { [statType] = amount };
            _vernStats.ApplyCallerEffects(effects);
        }
    }
}