using System;
using System.Collections.Generic;
using KBTV.Data;

namespace KBTV.Managers
{
    /// <summary>
    /// Manages topic progression data (experience and XP levels).
    /// Provides access to TopicXP instances for each topic.
    /// </summary>
    public class TopicManager
    {
        private readonly Dictionary<string, TopicXP> _topicXPs = new();

        public TopicManager()
        {
            InitializeTopics();
        }

        private void InitializeTopics()
        {
            // Initialize XP data for each topic
            var topics = new[] { "UFOs", "Ghosts", "Cryptids", "Conspiracies", "Aliens", "Time Travel" };

            foreach (var topicName in topics)
            {
                var topicId = topicName.ToLower().Replace(" ", "_");
                var belief = new TopicXP(topicId, topicName, GetInitialXP(topicName));
                _topicXPs[topicId] = belief;
            }
        }

        private float GetInitialXP(string topicName)
        {
            // Placeholder initial XP values (could be loaded from save data)
            return topicName switch
            {
                "UFOs" => 245f,         // Level 3 (Interested)
                "Ghosts" => 0f,         // Level 1 (Skeptic)
                "Cryptids" => 650f,     // Level 4 (Believer)
                "Conspiracies" => 120f, // Level 2 (Curious)
                "Aliens" => 45f,        // Level 1 (Skeptic)
                "Time Travel" => 180f,  // Level 2 (Curious)
                _ => 0f
            };
        }

        public TopicXP GetTopicXP(string topicId)
        {
            if (_topicXPs.TryGetValue(topicId.ToLower(), out var belief))
            {
                return belief;
            }

            // Fallback for unknown topics
            var fallback = new TopicXP(topicId.ToLower(), topicId, 0f);
            _topicXPs[topicId.ToLower()] = fallback;
            return fallback;
        }

        public IEnumerable<TopicXP> GetAllTopicXPs()
        {
            return _topicXPs.Values;
        }

        /// <summary>
        /// Award XP for a topic after screening a caller.
        /// </summary>
        public void AwardXP(string topicName, int points)
        {
            var topicId = topicName.ToLower();
            var belief = GetTopicXP(topicId);

            if (points > 0)
            {
                belief.ApplyGoodCaller(points);
            }
            else
            {
                belief.ApplyBadCaller(points);
            }

            Godot.GD.Print($"TopicManager: Awarded {points} XP to {topicName} (now {belief.XP:F0})");
        }
    }
}