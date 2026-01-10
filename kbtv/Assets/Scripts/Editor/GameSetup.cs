using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;
using KBTV;
using KBTV.Data;
using KBTV.Callers;
using KBTV.Dialogue;

/// <summary>
/// Editor utility to set up the game scene with all required components and assets.
/// Run via menu: KBTV > Setup Game Scene
/// 
/// Dialogue templates are loaded from JSON files in Assets/Data/Dialogue/.
/// See docs/CONVERSATION_DESIGN.md for JSON format documentation.
/// </summary>
public static class GameSetup
{
    private const string DATA_PATH = "Assets/Data";
    private const string VERN_STATS_PATH = "Assets/Data/DefaultVernStats.asset";
    private const string DIALOGUE_PATH = "Assets/Data/Dialogue";
    private const string DIALOGUE_ASSETS_PATH = "Assets/Data/Dialogue/Assets";
    private const string VERN_DIALOGUE_PATH = "Assets/Data/Dialogue/Assets/VernDialogue.asset";

    // Topic folders to scan for caller dialogue JSON files
    // TODO: Add Cryptids, Ghosts, Conspiracies folders when migrated to JSON
    private static readonly string[] DIALOGUE_TOPIC_FOLDERS = { "UFO", "Generic" };

    [MenuItem("KBTV/Setup Game Scene")]
    public static void SetupGameScene()
    {
        Debug.Log("GameSetup: Starting scene setup...");

        // Ensure Data folder exists
        EnsureDirectoryExists(DATA_PATH);

        // Create or find VernStats
        VernStats vernStats = GetOrCreateVernStats();

        // Create dialogue templates from JSON
        VernDialogueTemplate vernDialogue = GetOrCreateVernDialogue();
        List<CallerDialogueTemplate> callerDialogues = GetOrCreateCallerDialogues();

        // Find all topics
        Topic[] topics = FindAllAssets<Topic>("Assets/Data/Topics");

        // Find caller modifiers
        StatModifier goodCaller = AssetDatabase.LoadAssetAtPath<StatModifier>("Assets/Data/Events/GoodCaller.asset");
        StatModifier badCaller = AssetDatabase.LoadAssetAtPath<StatModifier>("Assets/Data/Events/BadCaller.asset");
        StatModifier greatCaller = AssetDatabase.LoadAssetAtPath<StatModifier>("Assets/Data/Events/GreatCaller.asset");

        // Find all items
        StatModifier[] items = FindAllAssets<StatModifier>("Assets/Data/Items");

        // Find or create GameBootstrap in scene
        GameBootstrap bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
        if (bootstrap == null)
        {
            GameObject bootstrapObj = new GameObject("GameBootstrap");
            bootstrap = bootstrapObj.AddComponent<GameBootstrap>();
            Debug.Log("GameSetup: Created GameBootstrap GameObject");
        }

        // Configure GameBootstrap via SerializedObject (proper way to set serialized fields)
        SerializedObject serializedBootstrap = new SerializedObject(bootstrap);

        // Set VernStats
        serializedBootstrap.FindProperty("_vernStatsAsset").objectReferenceValue = vernStats;

        // Set tonight's topic (first available, prefer UFOs)
        Topic tonightsTopic = AssetDatabase.LoadAssetAtPath<Topic>("Assets/Data/Topics/UFOs.asset");
        if (tonightsTopic == null && topics.Length > 0)
            tonightsTopic = topics[0];
        serializedBootstrap.FindProperty("_tonightsTopic").objectReferenceValue = tonightsTopic;

        // Set available topics
        SerializedProperty topicsArray = serializedBootstrap.FindProperty("_availableTopics");
        topicsArray.arraySize = topics.Length;
        for (int i = 0; i < topics.Length; i++)
        {
            topicsArray.GetArrayElementAtIndex(i).objectReferenceValue = topics[i];
        }

        // Set caller modifiers
        serializedBootstrap.FindProperty("_goodCallerModifier").objectReferenceValue = goodCaller;
        serializedBootstrap.FindProperty("_badCallerModifier").objectReferenceValue = badCaller;
        serializedBootstrap.FindProperty("_greatCallerModifier").objectReferenceValue = greatCaller;

        // Set available items
        SerializedProperty itemsArray = serializedBootstrap.FindProperty("_availableItems");
        itemsArray.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            itemsArray.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        // Set dialogue templates
        serializedBootstrap.FindProperty("_vernDialogue").objectReferenceValue = vernDialogue;

        SerializedProperty callerDialoguesArray = serializedBootstrap.FindProperty("_callerDialogues");
        callerDialoguesArray.arraySize = callerDialogues.Count;
        for (int i = 0; i < callerDialogues.Count; i++)
        {
            callerDialoguesArray.GetArrayElementAtIndex(i).objectReferenceValue = callerDialogues[i];
        }

        // Enable UI
        serializedBootstrap.FindProperty("_enableLiveShowUI").boolValue = true;
        serializedBootstrap.FindProperty("_enableDebugUI").boolValue = false;
        serializedBootstrap.FindProperty("_enableAudio").boolValue = true;

        // Disable auto-start so PreShow is shown
        serializedBootstrap.FindProperty("_autoStartLiveShow").boolValue = false;

        // Set reasonable show duration for testing (2 minutes)
        serializedBootstrap.FindProperty("_showDuration").floatValue = 120f;

        // Apply changes
        serializedBootstrap.ApplyModifiedProperties();

        // Mark scene dirty so it can be saved
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"GameSetup: Complete! Configured with:");
        Debug.Log($"  - VernStats: {vernStats.name}");
        Debug.Log($"  - Tonight's Topic: {(tonightsTopic != null ? tonightsTopic.name : "None")}");
        Debug.Log($"  - Available Topics: {topics.Length}");
        Debug.Log($"  - Items: {items.Length}");
        Debug.Log($"  - Vern Dialogue: {(vernDialogue != null ? vernDialogue.name : "None")}");
        Debug.Log($"  - Caller Dialogues: {callerDialogues.Count}");
        Debug.Log($"  - Show Duration: 120 seconds");
        Debug.Log("GameSetup: Press Play to start the game!");

        // Select the bootstrap object so user can see it
        Selection.activeGameObject = bootstrap.gameObject;
    }

    [MenuItem("KBTV/Create Missing Assets")]
    public static void CreateMissingAssets()
    {
        EnsureDirectoryExists(DATA_PATH);
        GetOrCreateVernStats();
        Debug.Log("GameSetup: All required assets created!");
    }

    [MenuItem("KBTV/Reload Dialogue From JSON")]
    public static void ReloadDialogueFromJson()
    {
        Debug.Log("GameSetup: Reloading dialogue templates from JSON...");

        // Delete existing dialogue assets to force regeneration
        DeleteExistingDialogueAssets();

        // Regenerate from JSON
        VernDialogueTemplate vernDialogue = GetOrCreateVernDialogue();
        List<CallerDialogueTemplate> callerDialogues = GetOrCreateCallerDialogues();

        Debug.Log($"GameSetup: Reloaded {(vernDialogue != null ? 1 : 0)} Vern template and {callerDialogues.Count} caller templates from JSON.");
    }

    private static void DeleteExistingDialogueAssets()
    {
        EnsureDirectoryExists(DIALOGUE_ASSETS_PATH);

        // Find and delete all existing dialogue ScriptableObjects in the assets folder
        string[] guids = AssetDatabase.FindAssets("t:VernDialogueTemplate t:CallerDialogueTemplate", new[] { DIALOGUE_ASSETS_PATH });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.DeleteAsset(path);
        }

        AssetDatabase.Refresh();
    }

    private static VernStats GetOrCreateVernStats()
    {
        // Try to load existing
        VernStats vernStats = AssetDatabase.LoadAssetAtPath<VernStats>(VERN_STATS_PATH);
        
        if (vernStats == null)
        {
            // Create new VernStats asset
            vernStats = ScriptableObject.CreateInstance<VernStats>();
            
            AssetDatabase.CreateAsset(vernStats, VERN_STATS_PATH);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"GameSetup: Created VernStats at {VERN_STATS_PATH}");
        }
        else
        {
            Debug.Log($"GameSetup: Found existing VernStats at {VERN_STATS_PATH}");
        }

        return vernStats;
    }

    private static T[] FindAllAssets<T>(string folder) where T : Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
        T[] assets = new T[guids.Length];
        
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            assets[i] = AssetDatabase.LoadAssetAtPath<T>(path);
        }
        
        return assets;
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = Path.GetFileName(path);
            
            // Ensure parent exists first
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureDirectoryExists(parent);
            }
            
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    private static VernDialogueTemplate GetOrCreateVernDialogue()
    {
        EnsureDirectoryExists(DIALOGUE_ASSETS_PATH);

        // Try to load existing
        VernDialogueTemplate vernDialogue = AssetDatabase.LoadAssetAtPath<VernDialogueTemplate>(VERN_DIALOGUE_PATH);

        if (vernDialogue == null)
        {
            // Create new VernDialogue asset from JSON
            vernDialogue = ScriptableObject.CreateInstance<VernDialogueTemplate>();

            // Load from JSON
            string jsonPath = DialogueLoader.GetVernDialoguePath();
            var data = DialogueLoader.LoadVernDialogueJson(jsonPath);
            
            if (data != null)
            {
                DialogueLoader.PopulateVernTemplate(vernDialogue, data);
                Debug.Log($"GameSetup: Loaded Vern dialogue from {jsonPath}");
            }
            else
            {
                Debug.LogWarning($"GameSetup: Could not load Vern dialogue from {jsonPath}, using empty template");
            }

            AssetDatabase.CreateAsset(vernDialogue, VERN_DIALOGUE_PATH);
            AssetDatabase.SaveAssets();

            Debug.Log($"GameSetup: Created VernDialogue at {VERN_DIALOGUE_PATH}");
        }
        else
        {
            Debug.Log($"GameSetup: Found existing VernDialogue at {VERN_DIALOGUE_PATH}");
        }

        return vernDialogue;
    }

    private static List<CallerDialogueTemplate> GetOrCreateCallerDialogues()
    {
        EnsureDirectoryExists(DIALOGUE_ASSETS_PATH);

        var templates = new List<CallerDialogueTemplate>();

        // Build a dictionary of topics for lookup
        var topicLookup = BuildTopicLookup();

        // Scan each topic folder for JSON files
        foreach (string topicFolder in DIALOGUE_TOPIC_FOLDERS)
        {
            string folderPath = $"{DIALOGUE_PATH}/{topicFolder}";
            
            if (!Directory.Exists(folderPath))
            {
                Debug.LogWarning($"GameSetup: Dialogue folder not found: {folderPath}");
                continue;
            }

            string[] jsonFiles = Directory.GetFiles(folderPath, "*.json");
            
            foreach (string jsonFile in jsonFiles)
            {
                var template = GetOrCreateCallerTemplateFromJson(jsonFile, topicFolder, topicLookup);
                if (template != null)
                {
                    templates.Add(template);
                }
            }
        }

        // Sort by priority (highest first) so ConversationGenerator picks best matches
        templates.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        Debug.Log($"GameSetup: {templates.Count} CallerDialogue templates ready");
        return templates;
    }

    private static Dictionary<string, Topic> BuildTopicLookup()
    {
        var lookup = new Dictionary<string, Topic>();
        
        Topic[] topics = FindAllAssets<Topic>("Assets/Data/Topics");
        foreach (var topic in topics)
        {
            if (topic != null && !string.IsNullOrEmpty(topic.TopicId))
            {
                lookup[topic.TopicId] = topic;
            }
        }

        return lookup;
    }

    private static CallerDialogueTemplate GetOrCreateCallerTemplateFromJson(
        string jsonPath, 
        string topicFolder, 
        Dictionary<string, Topic> topicLookup)
    {
        // Generate asset name from JSON path
        string assetName = DialogueLoader.GetAssetNameFromJsonPath(jsonPath, topicFolder);
        string assetPath = $"{DIALOGUE_ASSETS_PATH}/{assetName}.asset";

        // Try to load existing
        CallerDialogueTemplate template = AssetDatabase.LoadAssetAtPath<CallerDialogueTemplate>(assetPath);

        if (template == null)
        {
            // Load JSON data
            var data = DialogueLoader.LoadCallerDialogueJson(jsonPath);
            if (data == null)
            {
                Debug.LogError($"GameSetup: Failed to load caller dialogue from {jsonPath}");
                return null;
            }

            // Resolve topic
            Topic topic = null;
            if (!string.IsNullOrEmpty(data.topicId) && topicLookup.ContainsKey(data.topicId))
            {
                topic = topicLookup[data.topicId];
            }

            // Create and populate template
            template = ScriptableObject.CreateInstance<CallerDialogueTemplate>();
            DialogueLoader.PopulateCallerTemplate(template, data, topic);

            AssetDatabase.CreateAsset(template, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"GameSetup: Created {assetName} from {Path.GetFileName(jsonPath)}");
        }

        return template;
    }
}
