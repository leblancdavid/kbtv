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

                var dialogueVariant = data.GetValueOrDefault("arcLines", new Godot.Collections.Dictionary());
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
            int turnIndex = 0;
            int totalTurns = dialogueArray.Count;

            foreach (var item in dialogueArray)
            {
                var turnDict = item.As<Godot.Collections.Dictionary>();
                if (turnDict == null)
                {
                    turnIndex++;
                    continue;
                }

                var section = DetermineSection(turnIndex, totalTurns);
                var line = ConvertTurn(turnDict, turnIndex, section);
                dialogue.Add(line);
                turnIndex++;
            }

            return dialogue;
        }

        private static ArcDialogueLine ConvertTurn(Godot.Collections.Dictionary data, int turnIndex, ArcSection section)
        {
            var speakerStr = data.GetValueOrDefault("speaker", "Caller").AsString();
            var speaker = string.Equals(speakerStr, "Vern", StringComparison.OrdinalIgnoreCase)
                ? Speaker.Vern
                : Speaker.Caller;

            var linesVariant = data.GetValueOrDefault("lines", new Godot.Collections.Array());
            var lines = linesVariant.As<Godot.Collections.Array>();

            if (lines == null || lines.Count == 0)
            {
                // Fallback for empty turn
                return new ArcDialogueLine(speaker, "", null, null, turnIndex, section);
            }

            var textVariants = new Godot.Collections.Dictionary<string, string>();
            var audioIds = new Godot.Collections.Dictionary<string, string>();
            string defaultText = "";
            string defaultAudioId = "";

            foreach (var lineItem in lines)
            {
                var lineDict = lineItem.As<Godot.Collections.Dictionary>();
                if (lineDict == null) continue;

                var mood = lineDict.GetValueOrDefault("mood", "").AsString();
                var text = lineDict.GetValueOrDefault("text", "").AsString();
                var id = lineDict.GetValueOrDefault("id", "").AsString();

                if (speaker == Speaker.Vern && !string.IsNullOrEmpty(mood))
                {
                    // Vern line with mood variant
                    textVariants[mood.ToLowerInvariant()] = text;
                    audioIds[mood.ToLowerInvariant()] = id;

                    // Use neutral as default if available, otherwise first one
                    if (mood.ToLowerInvariant() == "neutral" || string.IsNullOrEmpty(defaultText))
                    {
                        defaultText = text;
                        defaultAudioId = id;
                    }
                }
                else if (speaker == Speaker.Caller)
                {
                    // Caller line - single variant
                    defaultText = text;
                    defaultAudioId = id;
                }
            }

            if (speaker == Speaker.Vern && textVariants.Count > 0)
            {
                return ArcDialogueLine.CreateVernLineWithVariants(textVariants, audioIds, defaultText, defaultAudioId, turnIndex, section);
            }
            else
            {
                // Caller line or fallback
                return ArcDialogueLine.CreateLine(speaker, defaultText, defaultAudioId, turnIndex, section);
            }
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
