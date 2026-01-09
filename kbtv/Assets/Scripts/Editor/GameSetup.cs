using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using KBTV;
using KBTV.Data;
using KBTV.Callers;

/// <summary>
/// Editor utility to set up the game scene with all required components and assets.
/// Run via menu: KBTV > Setup Game Scene
/// </summary>
public static class GameSetup
{
    private const string DATA_PATH = "Assets/Data";
    private const string VERN_STATS_PATH = "Assets/Data/DefaultVernStats.asset";

    [MenuItem("KBTV/Setup Game Scene")]
    public static void SetupGameScene()
    {
        Debug.Log("GameSetup: Starting scene setup...");

        // Ensure Data folder exists
        EnsureDirectoryExists(DATA_PATH);

        // Create or find VernStats
        VernStats vernStats = GetOrCreateVernStats();

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

        // Enable UI
        serializedBootstrap.FindProperty("_enableLiveShowUI").boolValue = true;
        serializedBootstrap.FindProperty("_enableDebugUI").boolValue = false;
        serializedBootstrap.FindProperty("_enableAudio").boolValue = true;

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
            string parent = Path.GetDirectoryName(path);
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
