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
    /// Load caller dialogue data from a JSON file.
    /// </summary>
    public static CallerDialogueData LoadCallerDialogueJson(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            Debug.LogError($"DialogueLoader: Caller dialogue JSON not found at {jsonPath}");
            return null;
        }

        try
        {
            string jsonContent = File.ReadAllText(jsonPath);
            var data = JsonUtility.FromJson<CallerDialogueData>(jsonContent);
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"DialogueLoader: Failed to parse caller dialogue JSON at {jsonPath}: {e.Message}");
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
    }

    /// <summary>
    /// Populate a CallerDialogueTemplate ScriptableObject from loaded JSON data.
    /// </summary>
    public static void PopulateCallerTemplate(CallerDialogueTemplate template, CallerDialogueData data, Topic topic)
    {
        if (template == null || data == null) return;

        template.Topic = topic;
        template.Legitimacy = ParseLegitimacy(data.legitimacy);
        template.Length = ParseLength(data.length);
        template.Priority = data.priority;

        template.IntroLines = ConvertLines(data.introLines);
        template.DetailLines = ConvertLines(data.detailLines);
        template.DefenseLines = ConvertLines(data.defenseLines);
        template.AcceptanceLines = ConvertLines(data.acceptanceLines);
        template.ExtraDetailLines = ConvertLines(data.extraDetailLines);
        template.ExtraDefenseLines = ConvertLines(data.extraDefenseLines);
        template.ConclusionLines = ConvertLines(data.conclusionLines);
    }

    /// <summary>
    /// Get the path to a Vern dialogue JSON file.
    /// </summary>
    public static string GetVernDialoguePath()
    {
        return $"{DIALOGUE_JSON_PATH}/Vern/VernDialogue.json";
    }

    /// <summary>
    /// Get all JSON files in a topic folder.
    /// </summary>
    public static string[] GetCallerDialogueFiles(string topicFolder)
    {
        string folderPath = $"{DIALOGUE_JSON_PATH}/{topicFolder}";
        if (!Directory.Exists(folderPath))
        {
            return Array.Empty<string>();
        }

        return Directory.GetFiles(folderPath, "*.json");
    }

    /// <summary>
    /// Get all topic folders that have JSON files.
    /// </summary>
    public static string[] GetDialogueTopicFolders()
    {
        if (!Directory.Exists(DIALOGUE_JSON_PATH))
        {
            return Array.Empty<string>();
        }

        var folders = Directory.GetDirectories(DIALOGUE_JSON_PATH);
        var result = new System.Collections.Generic.List<string>();

        foreach (var folder in folders)
        {
            string folderName = Path.GetFileName(folder);
            // Skip Vern folder - it's not a topic
            if (folderName == "Vern") continue;

            // Only include folders that have JSON files
            if (Directory.GetFiles(folder, "*.json").Length > 0)
            {
                result.Add(folderName);
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Extract asset name from JSON file path.
    /// E.g., "Assets/Data/Dialogue/UFO/Fake_Prankster.json" -> "UFO_Fake_Prankster"
    /// </summary>
    public static string GetAssetNameFromJsonPath(string jsonPath, string topicFolder)
    {
        string fileName = Path.GetFileNameWithoutExtension(jsonPath);
        return $"{topicFolder}_{fileName}";
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

    /// <summary>
    /// Parse a length string to ConversationLength enum.
    /// </summary>
    private static ConversationLength ParseLength(string lengthString)
    {
        if (string.IsNullOrEmpty(lengthString))
            return ConversationLength.Standard;

        if (Enum.TryParse<ConversationLength>(lengthString, true, out var length))
            return length;

        Debug.LogWarning($"DialogueLoader: Unknown length '{lengthString}', defaulting to Standard");
        return ConversationLength.Standard;
    }
}
