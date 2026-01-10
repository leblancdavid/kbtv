using System.Collections.Generic;
using UnityEngine;
using KBTV.Callers;
using KBTV.Data;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Generates conversations from pre-scripted conversation arcs.
    /// Uses mood variants and belief branches for dynamic playback.
    /// </summary>
    public class ArcConversationGenerator
    {
        private readonly ArcRepository _arcRepository;
        private readonly VernStats _vernStats;

        /// <summary>
        /// The last arc that was used to generate a conversation.
        /// Useful for debugging and testing.
        /// </summary>
        public ConversationArc LastUsedArc { get; private set; }

        /// <summary>
        /// The mood that was calculated for the last conversation.
        /// </summary>
        public VernMood LastMood { get; private set; }

        /// <summary>
        /// The belief path that was taken for the last conversation.
        /// </summary>
        public BeliefPath LastBeliefPath { get; private set; }

        public ArcConversationGenerator(ArcRepository arcRepository, VernStats vernStats)
        {
            _arcRepository = arcRepository;
            _vernStats = vernStats;
        }

        /// <summary>
        /// Generate a conversation for the given caller using the arc system.
        /// </summary>
        /// <param name="caller">The caller to generate conversation for</param>
        /// <param name="currentTopic">The current show topic (optional)</param>
        /// <returns>A Conversation ready for playback, or null if no matching arc found</returns>
        public Conversation Generate(Caller caller, Topic currentTopic = null)
        {
            ConversationArc arc = null;

            // If caller lied about their topic, try to find a topic-switcher arc first
            if (caller.IsLyingAboutTopic)
            {
                arc = _arcRepository?.GetRandomTopicSwitcherArc(
                    caller.ClaimedTopic, 
                    caller.ActualTopic, 
                    caller.Legitimacy);
                
                if (arc != null)
                {
                    Debug.Log($"ArcConversationGenerator: Using topic-switcher arc '{arc.ArcId}' " +
                              $"(claimed: {caller.ClaimedTopic} -> actual: {caller.ActualTopic})");
                }
            }

            // Fall back to regular arc selection using actual topic
            if (arc == null)
            {
                string topicName = caller.ActualTopic ?? currentTopic?.TopicId ?? "";
                arc = _arcRepository?.GetRandomArc(topicName, caller.Legitimacy);
                
                if (arc == null)
                {
                    Debug.LogWarning($"ArcConversationGenerator: No arc found for topic '{topicName}', " +
                                     $"legitimacy '{caller.Legitimacy}'. Falling back to null.");
                    return null;
                }
            }

            LastUsedArc = arc;

            // Calculate Vern's current mood
            VernMood mood = MoodCalculator.CalculateMood(_vernStats);
            LastMood = mood;

            // Get the mood variant
            var moodVariant = arc.GetMoodVariant(mood);
            if (moodVariant == null)
            {
                Debug.LogError($"ArcConversationGenerator: Arc '{arc.ArcId}' has no mood variant for {mood}");
                return null;
            }

            // Determine belief path using discernment
            BeliefPath beliefPath = DiscernmentCalculator.DetermineBeliefPath(_vernStats, caller.Legitimacy);
            LastBeliefPath = beliefPath;

            // Assemble the conversation
            return AssembleConversation(caller, currentTopic, arc, moodVariant, beliefPath);
        }

        /// <summary>
        /// Generate a conversation with specific mood and belief path (for testing/debugging).
        /// </summary>
        public Conversation Generate(Caller caller, Topic currentTopic, VernMood mood, BeliefPath beliefPath)
        {
            ConversationArc arc = null;

            // If caller lied about their topic, try to find a topic-switcher arc first
            if (caller.IsLyingAboutTopic)
            {
                arc = _arcRepository?.GetRandomTopicSwitcherArc(
                    caller.ClaimedTopic, 
                    caller.ActualTopic, 
                    caller.Legitimacy);
            }

            // Fall back to regular arc selection using actual topic
            if (arc == null)
            {
                string topicName = caller.ActualTopic ?? currentTopic?.TopicId ?? "";
                arc = _arcRepository?.GetRandomArc(topicName, caller.Legitimacy);
                if (arc == null) return null;
            }

            LastUsedArc = arc;
            LastMood = mood;
            LastBeliefPath = beliefPath;

            var moodVariant = arc.GetMoodVariant(mood);
            if (moodVariant == null) return null;

            return AssembleConversation(caller, currentTopic, arc, moodVariant, beliefPath);
        }

        private Conversation AssembleConversation(
            Caller caller,
            Topic currentTopic,
            ConversationArc arc,
            ArcMoodVariant moodVariant,
            BeliefPath beliefPath)
        {
            var conversation = new Conversation(caller);
            string topicDisplayName = currentTopic?.DisplayName ?? caller.ClaimedTopic ?? "the paranormal";

            // Get all lines for this path
            var arcLines = moodVariant.AssembleLines(beliefPath);

            // Convert arc lines to dialogue lines with proper phases
            int lineIndex = 0;
            int totalLines = arcLines.Count;

            foreach (var arcLine in arcLines)
            {
                // Determine phase based on position
                ConversationPhase phase = DeterminePhase(lineIndex, totalLines, arc.Legitimacy);

                // Determine tone based on mood and speaker
                DialogueTone tone = DetermineTone(arcLine.Speaker, LastMood, beliefPath, phase);

                // Apply substitutions and create dialogue line
                var dialogueLine = DialogueSubstitution.ApplyToArcLine(
                    arcLine, caller, topicDisplayName, phase, tone);

                conversation.AddLine(dialogueLine.Speaker, dialogueLine.Text, dialogueLine.Tone, dialogueLine.Phase);
                lineIndex++;
            }

            return conversation;
        }

        /// <summary>
        /// Determine the conversation phase based on line position.
        /// </summary>
        private ConversationPhase DeterminePhase(int lineIndex, int totalLines, CallerLegitimacy legitimacy)
        {
            // Intro is always first 2 lines
            if (lineIndex < 2)
                return ConversationPhase.Intro;

            // Conclusion is always last 2 lines
            if (lineIndex >= totalLines - 2)
                return ConversationPhase.Resolution;

            // Development and belief branch depend on legitimacy
            int developmentEnd = legitimacy switch
            {
                CallerLegitimacy.Fake => 4,           // 2 intro + 2 development
                CallerLegitimacy.Questionable => 4,   // 2 intro + 2 development
                CallerLegitimacy.Credible => 6,       // 2 intro + 4 development
                CallerLegitimacy.Compelling => 6,     // 2 intro + 4 development
                _ => 4
            };

            if (lineIndex < developmentEnd)
                return ConversationPhase.Probe;

            // Remaining lines before conclusion are challenge/belief
            return ConversationPhase.Challenge;
        }

        /// <summary>
        /// Determine the tone for a line based on mood and context.
        /// </summary>
        private DialogueTone DetermineTone(Speaker speaker, VernMood mood, BeliefPath beliefPath, ConversationPhase phase)
        {
            if (speaker == Speaker.Vern)
            {
                // Vern's tone based on mood
                return mood switch
                {
                    VernMood.Tired => DialogueTone.Neutral,
                    VernMood.Grumpy => DialogueTone.Annoyed,
                    VernMood.Neutral => DialogueTone.Neutral,
                    VernMood.Engaged => DialogueTone.Excited,
                    VernMood.Excited => DialogueTone.Excited,
                    _ => DialogueTone.Neutral
                };
            }
            else
            {
                // Caller's tone based on phase and belief path
                if (phase == ConversationPhase.Challenge)
                {
                    return beliefPath == BeliefPath.Skeptical 
                        ? DialogueTone.Nervous 
                        : DialogueTone.Excited;
                }
                return DialogueTone.Neutral;
            }
        }
    }
}
