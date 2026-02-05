using System.Collections.Generic;
using System;
using KBTV.Callers;
using KBTV.Data;
using KBTV.Dialogue;
using KBTV.Core;
using KBTV.Managers;
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
        private TopicManager _topicManager;
        private GameStateManager _gameStateManager;
        private float _totalXP;
        private float _appliedXP;

        public ConversationStatTracker(GameStateManager gameStateManager, TopicManager topicManager)
        {
            _vernStats = gameStateManager?.VernStats;
            _gameStateManager = gameStateManager;
            _topicManager = topicManager;
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
            
            // Calculate total XP from sum of ALL stat modifier amounts (including negatives)
            _totalXP = 0f;
            foreach (var amount in _totalEffects.Values)
            {
                _totalXP += amount;
            }
            _appliedXP = 0f;
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
            
            // Award XP equal to sum of ALL stat effects applied this line (including negatives)
            float xpAward = 0f;
            foreach (var (statType, totalAmount) in _totalEffects)
            {
                float incrementalAmount = totalAmount * (progress - previousProgress);
                xpAward += incrementalAmount;
            }
            
            if (xpAward != 0 && string.Equals(_currentCaller.ActualTopic, _gameStateManager?.SelectedTopic?.TopicName, StringComparison.OrdinalIgnoreCase))  // Award even negative XP
            {
                _appliedXP += xpAward;
                _topicManager.AwardXP(_currentCaller.ActualTopic, (int)Mathf.Round(xpAward));
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