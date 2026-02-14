using System;
using System.Collections.Generic;
using KBTV.Data;
using KBTV.Persistence;
using KBTV.Core;

namespace KBTV.Managers
{
    /// <summary>
    /// Manages topic progression data (experience and XP levels).
    /// Provides access to TopicXP instances for each topic.
    /// </summary>
    public class TopicManager : ISaveable
    {
        private readonly Dictionary<string, TopicXP> _topicXPs = new();

        public TopicManager()
        {
            InitializeTopics();
        }

        private void InitializeTopics()
        {
            // Initialize XP data for each topic (all start at 0 XP)
            var topics = new[] { "UFOs", "Ghosts", "Cryptids", "Conspiracies", "Aliens", "Time Travel" };

            foreach (var topicName in topics)
            {
                var topicId = topicName.ToLower().Replace(" ", "_");
                var belief = new TopicXP(topicId, topicName, 0f); // Start at 0 XP
                _topicXPs[topicId] = belief;
            }
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
        /// Get the total belief level (sum of all topic levels).
        /// </summary>
        public int GetTotalBeliefLevel()
        {
            int total = 0;
            foreach (var topicXP in _topicXPs.Values)
            {
                total += topicXP.CurrentLevel;
            }
            return total;
        }

        // ─────────────────────────────────────────────────────────────
        // ISaveable Implementation
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Called before saving - serialize topic XP data to save file.
        /// </summary>
        public void OnBeforeSave(SaveData data)
        {
            data.TopicXPs = new List<SaveData.TopicXPData>();
            foreach (var topicXP in _topicXPs.Values)
            {
                data.TopicXPs.Add(new SaveData.TopicXPData
                {
                    TopicId = topicXP.TopicId,
                    XP = topicXP.XP,
                    HighestLevelReached = topicXP.HighestLevelReached
                });
            }
        }

        /// <summary>
        /// Called after loading - restore topic XP data from save file.
        /// </summary>
        public void OnAfterLoad(SaveData data)
        {
            if (data.TopicXPs != null)
            {
                foreach (var savedXP in data.TopicXPs)
                {
                    var topicXP = GetTopicXP(savedXP.TopicId);
                    topicXP.SetXP(savedXP.XP);
                    // HighestTierReached is automatically updated by SetXP
                }
            }
            // Topics not in save data remain at 0 XP (already initialized)
        }
    }
}