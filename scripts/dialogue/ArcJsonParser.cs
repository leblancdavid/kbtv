using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using KBTV.Callers;

namespace KBTV.Dialogue
{
    /// <summary>
    /// JSON-serializable data for a conversation arc file.
    /// Used by JsonUtility to deserialize arc JSON files.
    /// </summary>
    [Serializable]
    public class ArcJsonData
    {
        public string arcId;
        public string topic;
        public string claimedTopic;
        public string legitimacy;
        public string screeningSummary;
        public ArcDialogueLineData[] dialogue;
        public string callerPersonality;
        public string arcNotes;
    }

    /// <summary>
    /// JSON data for a single dialogue line.
    /// For Vern: has textVariants with mood-specific text.
    /// For Caller: has single text field.
    /// </summary>
    [Serializable]
    public class ArcDialogueLineData
    {
        public string speaker;
        public string text;
        public TextVariantData[] textVariants;
    }

    /// <summary>
    /// Serializable text variant data.
    /// </summary>
    [Serializable]
    public class TextVariantData
    {
        public string mood;
        public string text;
    }

    /// <summary>
    /// Utility class for parsing conversation arc JSON data.
    /// Used by both runtime (ArcRepository) and editor (DialogueLoader) code.
    /// </summary>
    public static class ArcJsonParser
    {
        /// <summary>
        /// Parse arc JSON text into a ConversationArc.
        /// </summary>
        public static ConversationArc? Parse(string jsonText)
        {
            try
            {
                var jsonParse = Json.ParseString(jsonText);
                if (jsonParse.Equals(null))
                {
                    GD.PrintErr("ArcJsonParser: Failed to parse JSON");
                    return null;
                }

                var data = JsonSerializer.Deserialize<ArcJsonData>(jsonText);
                if (data == null)
                {
                    GD.PrintErr("ArcJsonParser: Failed to deserialize JSON data");
                    return null;
                }

                var legitimacy = ParseLegitimacy(data.legitimacy ?? "Questionable");
                var arc = new ConversationArc(
                    data.arcId ?? "",
                    data.topic ?? "",
                    legitimacy,
                    data.claimedTopic ?? ""
                );

                if (data.dialogue != null && data.dialogue.Length > 0)
                {
                    arc.SetDialogue(ConvertDialogue(data.dialogue));
                }

                arc.SetScreeningSummary(data.screeningSummary ?? "");
                arc.SetCallerPersonality(data.callerPersonality ?? "");

                GD.Print($"ArcJsonParser: Successfully parsed arc '{arc.ArcId}'");
                return arc;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"ArcJsonParser: Error parsing arc JSON: {ex.Message}");
                return null;
            }
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

        /// <summary>
        /// Convert flat dialogue array to List of ArcDialogueLine.
        /// </summary>
        private static List<ArcDialogueLine> ConvertDialogue(ArcDialogueLineData[] dialogueArray)
        {
            var dialogue = new List<ArcDialogueLine>();
            int lineIndex = 0;

            foreach (var lineData in dialogueArray)
            {
                var section = DetermineSection(lineIndex, dialogueArray.Length);
                dialogue.Add(ConvertLine(lineData, lineIndex, section));
                lineIndex++;
            }

            return dialogue;
        }

        /// <summary>
        /// Determine the section (Intro, Development, Conclusion) based on line position.
        /// </summary>
        private static ArcSection DetermineSection(int lineIndex, int totalLines)
        {
            // First line is always intro
            if (lineIndex == 0)
                return ArcSection.Intro;

            // Last line is always conclusion
            if (lineIndex >= totalLines - 1)
                return ArcSection.Conclusion;

            // Everything in between is development
            return ArcSection.Development;
        }

        /// <summary>
        /// Convert JSON line data to ArcDialogueLine.
        /// For Vern: creates line with textVariants.
        /// For Caller: creates line with single text.
        /// </summary>
        private static ArcDialogueLine ConvertLine(ArcDialogueLineData data, int arcLineIndex, ArcSection section)
        {
            var speaker = string.Equals(data.speaker, "Vern", StringComparison.OrdinalIgnoreCase)
                ? Speaker.Vern
                : Speaker.Caller;

            // Convert TextVariantData[] to Dictionary<string, string>
            Godot.Collections.Dictionary<string, string> textVariants = null;
            if (data.textVariants != null && data.textVariants.Length > 0)
            {
                textVariants = new Godot.Collections.Dictionary<string, string>();
                foreach (var variant in data.textVariants)
                {
                    if (!string.IsNullOrEmpty(variant.mood) && variant.text != null)
                    {
                        textVariants[variant.mood] = variant.text;
                    }
                }
            }

            if (speaker == Speaker.Vern && textVariants != null && textVariants.Count > 0)
            {
                // Vern line - create with textVariants
                return ArcDialogueLine.CreateVernLine(textVariants, arcLineIndex, section);
            }

            // Caller line or Vern without variants - use single text
            return new ArcDialogueLine(speaker, data.text ?? "", arcLineIndex, section);
        }
    }
}