using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using KBTV.Dialogue;
using KBTV.Callers;

/// <summary>
/// Editor utility for loading dialogue templates from JSON files and populating ScriptableObjects.
/// </summary>
public static class DialogueLoader
{
    private const string DIALOGUE_JSON_PATH = "Assets/Data/Dialogue";
    private const string ARCS_FOLDER = "Arcs";

    /// <summary>
    /// Load Vern dialogue data from a JSON file.
    /// </summary>
    public static VernDialogueData LoadVernDialogueJson(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            Debug.LogError($"DialogueLoader: Vern dialogue JSON not found at {jsonPath}");
            return null;
        }

        try
        {
            string jsonContent = File.ReadAllText(jsonPath);
            var data = JsonUtility.FromJson<VernDialogueData>(jsonContent);
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"DialogueLoader: Failed to parse Vern dialogue JSON at {jsonPath}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Populate a VernDialogueTemplate ScriptableObject from loaded JSON data.
    /// </summary>
    public static void PopulateVernTemplate(VernDialogueTemplate template, VernDialogueData data)
    {
        if (template == null || data == null) return;

        template.IntroductionLines = ConvertLines(data.introductionLines);
        template.ProbingLines = ConvertLines(data.probingLines);
        template.ExtraProbingLines = ConvertLines(data.extraProbingLines);
        template.SkepticalLines = ConvertLines(data.skepticalLines);
        template.DismissiveLines = ConvertLines(data.dismissiveLines);
        template.BelievingLines = ConvertLines(data.believingLines);
        template.TiredLines = ConvertLines(data.tiredLines);
        template.AnnoyedLines = ConvertLines(data.annoyedLines);
        template.EngagingLines = ConvertLines(data.engagingLines);
        template.CutOffLines = ConvertLines(data.cutOffLines);
        template.SignOffLines = ConvertLines(data.signOffLines);

        // Filler dialogue for broadcast flow
        template.ShowOpeningLines = ConvertLines(data.showOpeningLines);
        template.ShowClosingLines = ConvertLines(data.showClosingLines);
        template.BetweenCallersLines = ConvertLines(data.betweenCallersLines);
        template.DeadAirFillerLines = ConvertLines(data.deadAirFillerLines);

        // Error handling dialogue
        template.DroppedCallerLines = ConvertLines(data.droppedCallerLines);
    }

    /// <summary>
    /// Get the path to a Vern dialogue JSON file.
    /// </summary>
    public static string GetVernDialoguePath()
    {
        return $"{DIALOGUE_JSON_PATH}/Vern/VernDialogue.json";
    }

    /// <summary>
    /// Convert DialogueLineData array to DialogueTemplate array.
    /// </summary>
    private static DialogueTemplate[] ConvertLines(DialogueLineData[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            return Array.Empty<DialogueTemplate>();
        }

        var result = new DialogueTemplate[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            result[i] = new DialogueTemplate
            {
                Id = lines[i].id ?? "",
                Text = lines[i].text ?? "",
                Tone = ParseTone(lines[i].tone),
                Weight = lines[i].weight > 0 ? lines[i].weight : 1f
            };
        }

        return result;
    }

    /// <summary>
    /// Parse a tone string to DialogueTone enum.
    /// </summary>
    private static DialogueTone ParseTone(string toneString)
    {
        if (string.IsNullOrEmpty(toneString))
            return DialogueTone.Neutral;

        if (Enum.TryParse<DialogueTone>(toneString, true, out var tone))
            return tone;

        Debug.LogWarning($"DialogueLoader: Unknown tone '{toneString}', defaulting to Neutral");
        return DialogueTone.Neutral;
    }

    /// <summary>
    /// Parse a legitimacy string to CallerLegitimacy enum.
    /// </summary>
    private static CallerLegitimacy ParseLegitimacy(string legitimacyString)
    {
        if (string.IsNullOrEmpty(legitimacyString))
            return CallerLegitimacy.Questionable;

        if (Enum.TryParse<CallerLegitimacy>(legitimacyString, true, out var legitimacy))
            return legitimacy;

        Debug.LogWarning($"DialogueLoader: Unknown legitimacy '{legitimacyString}', defaulting to Questionable");
        return CallerLegitimacy.Questionable;
    }

    #region Arc Loading

    /// <summary>
    /// Get the path to the Arcs folder.
    /// </summary>
    public static string GetArcsPath()
    {
        return $"{DIALOGUE_JSON_PATH}/{ARCS_FOLDER}";
    }

    /// <summary>
    /// Load a conversation arc from a JSON file.
    /// </summary>
    public static ConversationArc LoadArcJson(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            Debug.LogError($"DialogueLoader: Arc JSON not found at {jsonPath}");
            return null;
        }

        try
        {
            string jsonContent = File.ReadAllText(jsonPath);
            var data = JsonUtility.FromJson<ArcJsonData>(jsonContent);
            return ConvertArcData(data);
        }
        catch (Exception e)
        {
            Debug.LogError($"DialogueLoader: Failed to parse arc JSON at {jsonPath}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Load all arcs from the Arcs folder structure.
    /// Structure: Arcs/{Topic}/{Legitimacy}/*.json
    /// </summary>
    public static System.Collections.Generic.List<ConversationArc> LoadAllArcs()
    {
        var arcs = new System.Collections.Generic.List<ConversationArc>();
        string arcsPath = GetArcsPath();

        if (!Directory.Exists(arcsPath))
        {
            Debug.LogWarning($"DialogueLoader: Arcs folder not found at {arcsPath}");
            return arcs;
        }

        // Iterate through topic folders
        foreach (var topicDir in Directory.GetDirectories(arcsPath))
        {
            string topicName = Path.GetFileName(topicDir);

            // Iterate through legitimacy folders
            foreach (var legitimacyDir in Directory.GetDirectories(topicDir))
            {
                // Load all JSON files in this legitimacy folder
                foreach (var jsonFile in Directory.GetFiles(legitimacyDir, "*.json"))
                {
                    var arc = LoadArcJson(jsonFile);
                    if (arc != null)
                    {
                        arcs.Add(arc);
                        Debug.Log($"DialogueLoader: Loaded arc '{arc.ArcId}' from {jsonFile}");
                    }
                }
            }
        }

        Debug.Log($"DialogueLoader: Loaded {arcs.Count} conversation arcs");
        return arcs;
    }

    /// <summary>
    /// Convert JSON data to a ConversationArc object.
    /// </summary>
    private static ConversationArc ConvertArcData(ArcJsonData data)
    {
        if (data == null) return null;

        var legitimacy = ParseLegitimacy(data.legitimacy);
        var arc = new ConversationArc(data.arcId, data.topic, legitimacy, data.claimedTopic);

        if (data.moodVariants != null)
        {
            if (data.moodVariants.Tired != null)
                arc.AddMoodVariant(VernMood.Tired, ConvertMoodVariant(data.moodVariants.Tired));
            if (data.moodVariants.Grumpy != null)
                arc.AddMoodVariant(VernMood.Grumpy, ConvertMoodVariant(data.moodVariants.Grumpy));
            if (data.moodVariants.Neutral != null)
                arc.AddMoodVariant(VernMood.Neutral, ConvertMoodVariant(data.moodVariants.Neutral));
            if (data.moodVariants.Engaged != null)
                arc.AddMoodVariant(VernMood.Engaged, ConvertMoodVariant(data.moodVariants.Engaged));
            if (data.moodVariants.Excited != null)
                arc.AddMoodVariant(VernMood.Excited, ConvertMoodVariant(data.moodVariants.Excited));
        }

        return arc;
    }

    /// <summary>
    /// Convert JSON mood variant data to ArcMoodVariant.
    /// </summary>
    private static ArcMoodVariant ConvertMoodVariant(ArcMoodVariantData data)
    {
        var variant = new ArcMoodVariant();

        if (data.intro != null)
        {
            foreach (var line in data.intro)
                variant.Intro.Add(ConvertArcLine(line));
        }

        if (data.development != null)
        {
            foreach (var line in data.development)
                variant.Development.Add(ConvertArcLine(line));
        }

        if (data.beliefBranch != null)
        {
            if (data.beliefBranch.Skeptical != null)
            {
                foreach (var line in data.beliefBranch.Skeptical)
                    variant.BeliefBranch.Skeptical.Add(ConvertArcLine(line));
            }
            if (data.beliefBranch.Believing != null)
            {
                foreach (var line in data.beliefBranch.Believing)
                    variant.BeliefBranch.Believing.Add(ConvertArcLine(line));
            }
        }

        if (data.conclusion != null)
        {
            foreach (var line in data.conclusion)
                variant.Conclusion.Add(ConvertArcLine(line));
        }

        return variant;
    }

    /// <summary>
    /// Convert JSON line data to ArcDialogueLine.
    /// </summary>
    private static ArcDialogueLine ConvertArcLine(ArcLineData data)
    {
        var speaker = string.Equals(data.speaker, "Vern", StringComparison.OrdinalIgnoreCase) 
            ? Speaker.Vern 
            : Speaker.Caller;
        return new ArcDialogueLine(speaker, data.text ?? "");
    }

    #endregion
}
