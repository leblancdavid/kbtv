using System;
using System.Collections.Generic;
using Godot;
using KBTV.Callers;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Utility class for parsing conversation arc JSON data.
    /// Used by both runtime (ArcRepository) and editor (DialogueLoader) code.
    /// </summary>
    public static class ArcJsonParser
    {
        public static ConversationArc? Parse(string jsonText)
        {
            try
            {
                var json = new Json();
                var error = json.Parse(jsonText);
                if (error != Error.Ok)
                {
                    return null;
                }

                var data = json.Data.As<Godot.Collections.Dictionary>();
                if (data == null)
                {
                    return null;
                }

                var arcId = data.GetValueOrDefault("arcId", "").AsString();

                var topicStr = data.GetValueOrDefault("topic", "").AsString();
                var legitimacyStr = data.GetValueOrDefault("legitimacy", "Questionable").AsString();
                var callerGender = data.GetValueOrDefault("callerGender", "male").AsString();
                var claimedTopicStr = data.GetValueOrDefault("claimedTopic", "").AsString();
                var screeningSummary = data.GetValueOrDefault("screeningSummary", "").AsString();
                var callerPersonality = data.GetValueOrDefault("callerPersonality", "").AsString();

                var legitimacy = ParseLegitimacy(legitimacyStr);
                var topic = ParseTopic(topicStr);
                var claimedTopic = ParseTopic(claimedTopicStr);

                if (topic == null)
                {
                    GD.PrintErr($"[ArcJsonParser] Invalid topic '{topicStr}' in arc {arcId}");
                    return null;
                }

                var arc = new ConversationArc(
                    arcId,
                    topic.Value,
                    legitimacy,
                    callerGender,
                    claimedTopic
                );

                var dialogueVariant = data.GetValueOrDefault("lines", new Godot.Collections.Dictionary());
                if (dialogueVariant.VariantType != Variant.Type.Nil)
                {
                    var dialogueList = ConvertDialogue(dialogueVariant.As<Godot.Collections.Array>());
                    arc.SetDialogue(dialogueList);
                }

                arc.SetScreeningSummary(screeningSummary);
                arc.SetCallerPersonality(callerPersonality);

                return arc;
            }
            catch
            {
                return null;
            }
        }

        private static List<ArcDialogueLine> ConvertDialogue(Godot.Collections.Array dialogueArray)
        {
            var dialogue = new List<ArcDialogueLine>();
            int lineIndex = 0;
            int totalLines = dialogueArray.Count;

            foreach (var item in dialogueArray)
            {
                var lineDict = item.As<Godot.Collections.Dictionary>();
                if (lineDict == null)
                {
                    lineIndex++;
                    continue;
                }

                var section = DetermineSection(lineIndex, totalLines);
                var line = ConvertLine(lineDict, lineIndex, section);
                dialogue.Add(line);
                lineIndex++;
            }

            return dialogue;
        }

        private static ArcDialogueLine ConvertLine(Godot.Collections.Dictionary data, int arcLineIndex, ArcSection section)
        {
            var speakerStr = data.GetValueOrDefault("speaker", "Caller").AsString();
            var speaker = string.Equals(speakerStr, "Vern", StringComparison.OrdinalIgnoreCase)
                ? Speaker.Vern
                : Speaker.Caller;

            var text = data.GetValueOrDefault("text", "").AsString();
            var mood = data.GetValueOrDefault("mood", "").AsString();

            // Check for flattened JSON format (new format with individual speaker/mood lines)
            if (!string.IsNullOrEmpty(speakerStr) && !string.IsNullOrEmpty(mood))
            {
                // This is the flattened format - create textVariants from the single line
                var textVariants = new Godot.Collections.Dictionary<string, string>();
                textVariants[mood.ToLowerInvariant()] = text;

                if (speaker == Speaker.Vern)
                {
                    return ArcDialogueLine.CreateVernLine(textVariants, arcLineIndex, section);
                }
            }

            // Fallback: check for old nested textVariants format
            var textVariantsVariant = data.GetValueOrDefault("textVariants", new Godot.Collections.Dictionary());

            Godot.Collections.Dictionary<string, string> legacyTextVariants = null;
            if (textVariantsVariant.VariantType != Variant.Type.Nil)
            {
                var variants = textVariantsVariant.As<Godot.Collections.Array>();
                if (variants != null && variants.Count > 0)
                {
                    legacyTextVariants = new Godot.Collections.Dictionary<string, string>();
                    foreach (var variantItem in variants)
                    {
                        var variantDict = variantItem.As<Godot.Collections.Dictionary>();
                        if (variantDict != null)
                        {
                            var variantMood = variantDict.GetValueOrDefault("mood", "").AsString();
                            var variantText = variantDict.GetValueOrDefault("text", "").AsString();
                            if (!string.IsNullOrEmpty(variantMood))
                            {
                                legacyTextVariants[variantMood] = variantText;
                            }
                        }
                    }
                }
            }

            if (speaker == Speaker.Vern && legacyTextVariants != null && legacyTextVariants.Count > 0)
            {
                return ArcDialogueLine.CreateVernLine(legacyTextVariants, arcLineIndex, section);
            }

            return new ArcDialogueLine(speaker, text, arcLineIndex, section);
        }

        private static ArcSection DetermineSection(int lineIndex, int totalLines)
        {
            if (lineIndex == 0)
                return ArcSection.Intro;
            if (lineIndex >= totalLines - 1)
                return ArcSection.Conclusion;
            return ArcSection.Development;
        }

        public static CallerLegitimacy ParseLegitimacy(string legitimacyString)
        {
            if (string.IsNullOrEmpty(legitimacyString))
                return CallerLegitimacy.Questionable;

            if (Enum.TryParse<CallerLegitimacy>(legitimacyString, true, out var legitimacy))
                return legitimacy;

            return CallerLegitimacy.Questionable;
        }

        public static ShowTopic? ParseTopic(string topicString)
        {
            return ShowTopicExtensions.ParseTopic(topicString);
        }
    }
}
