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
    /// Only populates broadcast flow lines (show opening/closing, between callers, dead air filler).
    /// Caller conversation lines are handled by the arc-based system.
    /// </summary>
    public static void PopulateVernTemplate(VernDialogueTemplate template, VernDialogueData data)
    {
        if (template == null || data == null) return;

        // Broadcast flow lines
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
    /// Uses the shared ArcJsonParser for consistency with runtime loading.
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
            return ArcJsonParser.Parse(jsonContent);
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

    #endregion
}
