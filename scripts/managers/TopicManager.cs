using System;
using System.Collections.Generic;
using KBTV.Data;

namespace KBTV.Managers
{
    /// <summary>
    /// Manages topic progression data (experience and belief levels).
    /// Provides access to TopicBelief instances for each topic.
    /// </summary>
    public class TopicManager
    {
        private readonly Dictionary<string, TopicBelief> _topicBeliefs = new();

        public TopicManager()
        {
            InitializeTopics();
        }

        private void InitializeTopics()
        {
            // Initialize belief data for each topic
            var topics = new[] { "UFOs", "Ghosts", "Cryptids", "Conspiracies", "Aliens", "Time Travel" };

            foreach (var topicName in topics)
            {
                var topicId = topicName.ToLower().Replace(" ", "_");
                var belief = new TopicBelief(topicId, topicName, GetInitialBelief(topicName));
                _topicBeliefs[topicId] = belief;
            }
        }

        private float GetInitialBelief(string topicName)
        {
            // Placeholder initial belief values (could be loaded from save data)
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

        public TopicBelief GetTopicBelief(string topicId)
        {
            if (_topicBeliefs.TryGetValue(topicId.ToLower(), out var belief))
            {
                return belief;
            }

            // Fallback for unknown topics
            var fallback = new TopicBelief(topicId.ToLower(), topicId, 0f);
            _topicBeliefs[topicId.ToLower()] = fallback;
            return fallback;
        }

        public IEnumerable<TopicBelief> GetAllTopicBeliefs()
        {
            return _topicBeliefs.Values;
        }

        /// <summary>
        /// Award belief points for a topic after screening a caller.
        /// </summary>
        public void AwardBeliefPoints(string topicName, int points)
        {
            var topicId = topicName.ToLower();
            var belief = GetTopicBelief(topicId);

            if (points > 0)
            {
                belief.ApplyGoodCaller(points);
            }
            else
            {
                belief.ApplyBadCaller(points);
            }

            Godot.GD.Print($"TopicManager: Awarded {points} belief points to {topicName} (now {belief.Belief:F0})");
        }
    }
}