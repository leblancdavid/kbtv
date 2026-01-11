using System;
using UnityEngine;
using KBTV.Callers;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Utility class for parsing conversation arc JSON data.
    /// Used by both runtime (ArcRepository) and editor (DialogueLoader) code.
    /// </summary>
    public static class ArcJsonParser
    {
        /// <summary>
        /// Parse arc JSON text into a ConversationArc.
        /// </summary>
        public static ConversationArc Parse(string jsonText)
        {
            var data = JsonUtility.FromJson<ArcJsonData>(jsonText);
            if (data == null) return null;

            var legitimacy = ParseLegitimacy(data.legitimacy);
            var arc = new ConversationArc(data.arcId, data.topic, legitimacy, data.claimedTopic);

            if (data.moodVariants != null)
            {
                AddMoodVariantIfPresent(arc, VernMood.Tired, data.moodVariants.Tired);
                AddMoodVariantIfPresent(arc, VernMood.Grumpy, data.moodVariants.Grumpy);
                AddMoodVariantIfPresent(arc, VernMood.Neutral, data.moodVariants.Neutral);
                AddMoodVariantIfPresent(arc, VernMood.Engaged, data.moodVariants.Engaged);
                AddMoodVariantIfPresent(arc, VernMood.Excited, data.moodVariants.Excited);
            }

            return arc;
        }

        /// <summary>
        /// Parse a legitimacy string to CallerLegitimacy enum.
        /// </summary>
        public static CallerLegitimacy ParseLegitimacy(string legitimacyString)
        {
            if (string.IsNullOrEmpty(legitimacyString))
                return CallerLegitimacy.Questionable;

            if (Enum.TryParse<CallerLegitimacy>(legitimacyString, true, out var legitimacy))
                return legitimacy;

            return CallerLegitimacy.Questionable;
        }

        private static void AddMoodVariantIfPresent(ConversationArc arc, VernMood mood, ArcMoodVariantData data)
        {
            if (data == null) return;
            arc.AddMoodVariant(mood, ConvertMoodVariant(data));
        }

        private static ArcMoodVariant ConvertMoodVariant(ArcMoodVariantData data)
        {
            var variant = new ArcMoodVariant();
            int lineIndex = 0; // Track sequential index across all sections

            if (data.intro != null)
            {
                foreach (var line in data.intro)
                    variant.Intro.Add(ConvertLine(line, lineIndex++));
            }

            if (data.development != null)
            {
                foreach (var line in data.development)
                    variant.Development.Add(ConvertLine(line, lineIndex++));
            }

            if (data.beliefBranch != null)
            {
                // Skeptical lines come first in the index sequence
                if (data.beliefBranch.Skeptical != null)
                {
                    foreach (var line in data.beliefBranch.Skeptical)
                        variant.BeliefBranch.Skeptical.Add(ConvertLine(line, lineIndex++));
                }
                // Believing lines come after skeptical in the index sequence
                if (data.beliefBranch.Believing != null)
                {
                    foreach (var line in data.beliefBranch.Believing)
                        variant.BeliefBranch.Believing.Add(ConvertLine(line, lineIndex++));
                }
            }

            if (data.conclusion != null)
            {
                foreach (var line in data.conclusion)
                    variant.Conclusion.Add(ConvertLine(line, lineIndex++));
            }

            return variant;
        }

        private static ArcDialogueLine ConvertLine(ArcLineData data, int arcLineIndex)
        {
            var speaker = string.Equals(data.speaker, "Vern", StringComparison.OrdinalIgnoreCase)
                ? Speaker.Vern
                : Speaker.Caller;
            return new ArcDialogueLine(speaker, data.text ?? "", arcLineIndex);
        }
    }
}
