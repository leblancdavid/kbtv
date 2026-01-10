using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using KBTV;
using KBTV.Callers;
using KBTV.Data;
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
    private const string ARC_REPOSITORY_PATH = "Assets/Data/Dialogue/Assets/ArcRepository.asset";
    private const string ARCS_PATH = "Assets/Data/Dialogue/Arcs";

    // Topic folders to scan for arc JSON files
    private static readonly string[] ARC_TOPIC_FOLDERS = { "UFOs", "Ghosts", "Cryptids", "Conspiracies" };
    
    // TopicIds that have arc coverage (used to filter available topics)
    // Maps folder name to TopicId for matching
    private static readonly Dictionary<string, string> TOPIC_FOLDER_TO_ID = new Dictionary<string, string>
    {
        { "UFOs", "ufos" },
        { "Cryptids", "cryptids" },
        { "Ghosts", "ghosts" },
        { "Conspiracies", "government" }
    };
    
    // Legitimacy subfolders within each topic
    private static readonly string[] LEGITIMACY_FOLDERS = { "Compelling", "Credible", "Questionable", "Fake" };

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
        
        // Create arc repository
        ArcRepository arcRepository = GetOrCreateArcRepository();

        // Find all topics
        Topic[] allTopics = FindAllAssets<Topic>("Assets/Data/Topics");
        
        // Filter to only include topics that have arc coverage
        var supportedTopicIds = new HashSet<string>(TOPIC_FOLDER_TO_ID.Values, System.StringComparer.OrdinalIgnoreCase);
        Topic[] topics = allTopics.Where(t => supportedTopicIds.Contains(t.TopicId)).ToArray();
        
        Debug.Log($"GameSetup: Found {allTopics.Length} topics, {topics.Length} have arc coverage");
        foreach (var excluded in allTopics.Where(t => !supportedTopicIds.Contains(t.TopicId)))
        {
            Debug.Log($"GameSetup: Excluding topic '{excluded.name}' (topicId: {excluded.TopicId}) - no arc coverage");
        }

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

        // Set arc repository
        serializedBootstrap.FindProperty("_arcRepository").objectReferenceValue = arcRepository;

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
        Debug.Log($"  - Arc Repository: {(arcRepository != null ? arcRepository.name : "None")} ({(arcRepository != null ? arcRepository.Arcs.Count : 0)} arcs)");
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
        Debug.Log("GameSetup: Reloading dialogue from JSON...");

        // Delete existing dialogue assets to force regeneration
        DeleteExistingDialogueAssets();

        // Regenerate from JSON
        VernDialogueTemplate vernDialogue = GetOrCreateVernDialogue();
        ArcRepository arcRepository = GetOrCreateArcRepository();

        Debug.Log($"GameSetup: Reloaded Vern template and arc repository ({arcRepository?.Arcs.Count ?? 0} arcs) from JSON.");
    }

    private static void DeleteExistingDialogueAssets()
    {
        EnsureDirectoryExists(DIALOGUE_ASSETS_PATH);

        // Find and delete all existing VernDialogueTemplate assets
        string[] guids = AssetDatabase.FindAssets("t:VernDialogueTemplate", new[] { DIALOGUE_ASSETS_PATH });
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

    [MenuItem("KBTV/Create Arc Repository")]
    public static void CreateArcRepositoryMenu()
    {
        var arcRepository = GetOrCreateArcRepository();
        if (arcRepository != null)
        {
            Selection.activeObject = arcRepository;
            Debug.Log($"GameSetup: Arc repository ready with {arcRepository.Arcs.Count} arcs");
        }
    }

    private static ArcRepository GetOrCreateArcRepository()
    {
        EnsureDirectoryExists(DIALOGUE_ASSETS_PATH);
        EnsureDirectoryExists(ARCS_PATH);

        // Try to load existing
        ArcRepository arcRepository = AssetDatabase.LoadAssetAtPath<ArcRepository>(ARC_REPOSITORY_PATH);

        if (arcRepository == null)
        {
            // Create new ArcRepository asset
            arcRepository = ScriptableObject.CreateInstance<ArcRepository>();
            AssetDatabase.CreateAsset(arcRepository, ARC_REPOSITORY_PATH);
            Debug.Log($"GameSetup: Created ArcRepository at {ARC_REPOSITORY_PATH}");
        }
        else
        {
            Debug.Log($"GameSetup: Found existing ArcRepository at {ARC_REPOSITORY_PATH}");
        }

        // Populate with arc JSON files
        PopulateArcRepository(arcRepository);

        AssetDatabase.SaveAssets();
        return arcRepository;
    }

    private static void PopulateArcRepository(ArcRepository arcRepository)
    {
        // Get the serialized object to modify the _arcJsonFiles list
        SerializedObject serializedRepo = new SerializedObject(arcRepository);
        SerializedProperty jsonFilesProperty = serializedRepo.FindProperty("_arcJsonFiles");

        // Clear existing
        jsonFilesProperty.ClearArray();

        int arcCount = 0;

        // Scan each topic folder for arc JSON files
        foreach (string topicFolder in ARC_TOPIC_FOLDERS)
        {
            foreach (string legitimacyFolder in LEGITIMACY_FOLDERS)
            {
                string folderPath = $"{ARCS_PATH}/{topicFolder}/{legitimacyFolder}";
                
                if (!Directory.Exists(folderPath))
                {
                    // Create the folder structure if it doesn't exist
                    EnsureDirectoryExists(folderPath);
                    continue;
                }

                string[] jsonFiles = Directory.GetFiles(folderPath, "*.json");
                
                foreach (string jsonFile in jsonFiles)
                {
                    // Convert to Unity asset path
                    string unityPath = jsonFile.Replace("\\", "/");
                    
                    // Load the TextAsset
                    TextAsset jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(unityPath);
                    if (jsonAsset != null)
                    {
                        // Add to the array
                        jsonFilesProperty.InsertArrayElementAtIndex(jsonFilesProperty.arraySize);
                        jsonFilesProperty.GetArrayElementAtIndex(jsonFilesProperty.arraySize - 1).objectReferenceValue = jsonAsset;
                        arcCount++;
                        Debug.Log($"GameSetup: Added arc JSON: {Path.GetFileName(jsonFile)}");
                    }
                    else
                    {
                        Debug.LogWarning($"GameSetup: Could not load arc JSON as TextAsset: {unityPath}");
                    }
                }
            }
        }

        serializedRepo.ApplyModifiedProperties();
        
        // Force re-initialization
        arcRepository.Clear();

        Debug.Log($"GameSetup: Populated ArcRepository with {arcCount} arc JSON files");
    }
}
