using System.Collections.Generic;
using KBTV.Callers;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Interface for the arc repository service.
    /// Provides access to conversation arcs for runtime use.
    /// </summary>
    public interface IArcRepository
    {
        /// <summary>
        /// All loaded conversation arcs.
        /// </summary>
        Godot.Collections.Array<ConversationArc> Arcs { get; }

        /// <summary>
        /// Initialize the repository by parsing all arc JSON files.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Find all arcs matching the given topic and legitimacy.
        /// </summary>
        List<ConversationArc> FindMatchingArcs(string topic, CallerLegitimacy legitimacy);

        /// <summary>
        /// Get a random arc matching the given legitimacy.
        /// Returns null if no matching arcs found.
        /// </summary>
        ConversationArc? GetRandomArc(CallerLegitimacy legitimacy);

        /// <summary>
        /// Get a random arc matching the specified topic and legitimacy.
        /// Returns null if no matching arc found.
        /// </summary>
        ConversationArc? GetRandomArcForTopic(string topicId, CallerLegitimacy legitimacy);

        /// <summary>
        /// Get a random arc from a different topic than specified.
        /// Used for generating off-topic callers.
        /// </summary>
        ConversationArc? GetRandomArcForDifferentTopic(string excludeTopicId, CallerLegitimacy legitimacy);

        /// <summary>
        /// Find all topic-switcher arcs matching a caller who lied about their topic.
        /// </summary>
        List<ConversationArc> FindTopicSwitcherArcs(string claimedTopic, string actualTopic, CallerLegitimacy legitimacy);

        /// <summary>
        /// Get a random topic-switcher arc for a caller who lied about their topic.
        /// Returns null if no matching switcher arc found.
        /// </summary>
        ConversationArc? GetRandomTopicSwitcherArc(string claimedTopic, string actualTopic, CallerLegitimacy legitimacy);

        /// <summary>
        /// Add an arc to the repository (used by editor tools).
        /// </summary>
        void AddArc(ConversationArc arc);

        /// <summary>
        /// Clear all arcs from the repository.
        /// </summary>
        void Clear();
    }
}
