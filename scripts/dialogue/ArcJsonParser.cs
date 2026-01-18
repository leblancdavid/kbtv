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
                var topic = data.GetValueOrDefault("topic", "").AsString();
                var legitimacyStr = data.GetValueOrDefault("legitimacy", "Questionable").AsString();
                var claimedTopic = data.GetValueOrDefault("claimedTopic", "").AsString();
                var screeningSummary = data.GetValueOrDefault("screeningSummary", "").AsString();
                var callerPersonality = data.GetValueOrDefault("callerPersonality", "").AsString();

                var legitimacy = ParseLegitimacy(legitimacyStr);
                var arc = new ConversationArc(
                    arcId,
                    topic,
                    legitimacy,
                    claimedTopic
                );

                var dialogueVariant = data.GetValueOrDefault("dialogue", new Godot.Collections.Dictionary());
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
            var textVariantsVariant = data.GetValueOrDefault("textVariants", new Godot.Collections.Dictionary());
            
            Godot.Collections.Dictionary<string, string> textVariants = null;
            if (textVariantsVariant.VariantType != Variant.Type.Nil)
            {
                var variants = textVariantsVariant.As<Godot.Collections.Array>();
                if (variants != null && variants.Count > 0)
                {
                    textVariants = new Godot.Collections.Dictionary<string, string>();
                    foreach (var variantItem in variants)
                    {
                        var variantDict = variantItem.As<Godot.Collections.Dictionary>();
                        if (variantDict != null)
                        {
                            var mood = variantDict.GetValueOrDefault("mood", "").AsString();
                            var variantText = variantDict.GetValueOrDefault("text", "").AsString();
                            if (!string.IsNullOrEmpty(mood))
                            {
                                textVariants[mood] = variantText;
                            }
                        }
                    }
                }
            }

            if (speaker == Speaker.Vern && textVariants != null && textVariants.Count > 0)
            {
                return ArcDialogueLine.CreateVernLine(textVariants, arcLineIndex, section);
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
    }
}
