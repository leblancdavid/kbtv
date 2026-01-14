using System.Collections.Generic;
using Godot;
using KBTV.Callers;

namespace KBTV.Data
{
    /// <summary>
    /// Utility class for loading Topic resources from the Godot project.
    /// Handles topic loading with fallback to sample topics if none are found.
    /// </summary>
    public static class TopicLoader
    {
        /// <summary>
        /// Load all available topics from project resources.
        /// Falls back to sample topics if no resources are found.
        /// </summary>
        public static List<Topic> LoadAllTopics()
        {
            var topics = new List<Topic>();

            // TODO: Load topics from Godot resources
            // For now, create sample topics as fallback
            topics.Add(CreateSampleTopic("UFO Sightings"));
            topics.Add(CreateSampleTopic("Government Conspiracies"));
            topics.Add(CreateSampleTopic("Paranormal Activity"));
            topics.Add(CreateSampleTopic("Ancient Mysteries"));

            GD.Print($"TopicLoader: Loaded {topics.Count} topics");
            return topics;
        }

        /// <summary>
        /// Create a sample topic for fallback/demo purposes.
        /// </summary>
        private static Topic CreateSampleTopic(string name)
        {
            var topicId = name.ToLower().Replace(" ", "_");
            var topic = new Topic(name, topicId, $"Discuss {name.ToLower()} and related phenomena");
            topic.SetProperties(0.1f, 0.2f, 1f);

            // Add sample keywords
            var keywords = new Godot.Collections.Array<string>();
            keywords.Add(name.ToLower());
            keywords.Add("mystery");
            keywords.Add("unexplained");
            topic.SetKeywords(keywords);

            // Add sample screening rules
            var rules = new Godot.Collections.Array<ScreeningRule>();
            var rule = new ScreeningRule(
                $"Caller must be calling about {name.ToLower()}",
                ScreeningRuleType.TopicMustMatch,
                topicId,
                true
            );
            rules.Add(rule);
            topic.SetRules(rules);

            return topic;
        }

        /// <summary>
        /// Get a topic by ID.
        /// </summary>
        public static Topic GetTopicById(string topicId)
        {
            var topics = LoadAllTopics();
            return topics.Find(t => t.TopicId == topicId);
        }

        /// <summary>
        /// Validate that topics are available.
        /// </summary>
        public static bool HasTopicsAvailable()
        {
            return LoadAllTopics().Count > 0;
        }
    }
}