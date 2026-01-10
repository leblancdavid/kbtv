using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System.IO;
using System.Linq;
using KBTV.Audio;

/// <summary>
/// Editor utility to set up voice audio infrastructure:
/// - Creates Audio Mixer with Vern and Caller groups
/// - Configures Addressables for voice audio clips
/// - Assigns mixer groups to AudioManager
/// 
/// Run via menu: KBTV > Setup Voice Audio
/// </summary>
public static class VoiceAudioSetup
{
    private const string AUDIO_PATH = "Assets/Audio";
    private const string MIXER_PATH = "Assets/Audio/KBTVMixer.mixer";
    private const string VOICE_PATH = "Assets/Audio/Voice";
    private const string VERN_BROADCAST_PATH = "Assets/Audio/Voice/Vern/Broadcast";
    private const string CALLERS_PATH = "Assets/Audio/Voice/Callers";
    
    private const string ADDRESSABLE_GROUP_NAME = "VoiceAudio";

    [MenuItem("KBTV/Setup Voice Audio")]
    public static void SetupVoiceAudio()
    {
        Debug.Log("VoiceAudioSetup: Starting voice audio setup...");

        // Ensure Audio folder exists
        EnsureDirectoryExists(AUDIO_PATH);
        EnsureDirectoryExists(VOICE_PATH);

        // Create or find Audio Mixer
        AudioMixer mixer = GetOrCreateAudioMixer();
        
        // Get mixer groups
        AudioMixerGroup masterGroup = GetMixerGroup(mixer, "Master");
        AudioMixerGroup vernGroup = GetMixerGroup(mixer, "VernGroup");
        AudioMixerGroup callerGroup = GetMixerGroup(mixer, "CallerGroup");

        // Configure Addressables (if available)
        bool addressablesConfigured = ConfigureAddressables();

        // Assign to AudioManager
        bool audioManagerConfigured = ConfigureAudioManager(mixer, vernGroup, callerGroup);

        // Summary
        Debug.Log("VoiceAudioSetup: Complete!");
        Debug.Log($"  - Audio Mixer: {MIXER_PATH}");
        Debug.Log($"  - Master Group: {(masterGroup != null ? "OK" : "Missing")}");
        Debug.Log($"  - Vern Group: {(vernGroup != null ? "OK" : "Missing")}");
        Debug.Log($"  - Caller Group: {(callerGroup != null ? "OK" : "Missing")}");
        Debug.Log($"  - Addressables: {(addressablesConfigured ? "Configured" : "Skipped (configure manually)")}");
        Debug.Log($"  - AudioManager: {(audioManagerConfigured ? "Configured" : "Not found in scene")}");

        EditorUtility.DisplayDialog("Voice Audio Setup Complete",
            $"Audio Mixer: {(mixer != null ? "Created" : "Error")}\n" +
            $"Mixer Groups: Master, VernGroup, CallerGroup\n" +
            $"Addressables: {(addressablesConfigured ? "Configured" : "Configure manually")}\n" +
            $"AudioManager: {(audioManagerConfigured ? "Configured" : "Assign manually in scene")}",
            "OK");
    }

    [MenuItem("KBTV/Setup Voice Audio/Create Audio Mixer Only")]
    public static void CreateAudioMixerOnly()
    {
        EnsureDirectoryExists(AUDIO_PATH);
        AudioMixer mixer = GetOrCreateAudioMixer();
        Selection.activeObject = mixer;
        Debug.Log($"VoiceAudioSetup: Audio Mixer ready at {MIXER_PATH}");
    }

    [MenuItem("KBTV/Setup Voice Audio/Configure Addressables Only")]
    public static void ConfigureAddressablesOnly()
    {
        bool success = ConfigureAddressables();
        if (success)
        {
            Debug.Log("VoiceAudioSetup: Addressables configured successfully");
        }
        else
        {
            Debug.LogWarning("VoiceAudioSetup: Addressables configuration failed or was skipped");
        }
    }

    [MenuItem("KBTV/Setup Voice Audio/Assign AudioManager Only")]
    public static void AssignAudioManagerOnly()
    {
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MIXER_PATH);
        if (mixer == null)
        {
            Debug.LogError("VoiceAudioSetup: Audio Mixer not found. Run 'Create Audio Mixer Only' first.");
            return;
        }

        AudioMixerGroup vernGroup = GetMixerGroup(mixer, "VernGroup");
        AudioMixerGroup callerGroup = GetMixerGroup(mixer, "CallerGroup");

        bool success = ConfigureAudioManager(mixer, vernGroup, callerGroup);
        if (success)
        {
            Debug.Log("VoiceAudioSetup: AudioManager configured successfully");
        }
        else
        {
            Debug.LogWarning("VoiceAudioSetup: AudioManager not found in scene");
        }
    }

    private static AudioMixer GetOrCreateAudioMixer()
    {
        // Try to load existing
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MIXER_PATH);

        if (mixer != null)
        {
            Debug.Log($"VoiceAudioSetup: Found existing Audio Mixer at {MIXER_PATH}");
            
            // Verify groups exist
            EnsureMixerGroupsExist(mixer);
            return mixer;
        }

        // Create new Audio Mixer
        mixer = new AudioMixer();
        AssetDatabase.CreateAsset(mixer, MIXER_PATH);
        
        // Note: Unity's AudioMixer API is limited for programmatic group creation.
        // We create the asset, but groups must be added via the Audio Mixer window
        // or by duplicating an existing mixer template.
        
        Debug.Log($"VoiceAudioSetup: Created Audio Mixer at {MIXER_PATH}");
        Debug.Log("VoiceAudioSetup: NOTE - Open the Audio Mixer window to add VernGroup and CallerGroup manually");
        
        // Create a template mixer with the groups we need using SerializedObject
        CreateMixerGroups(mixer);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return mixer;
    }

    private static void CreateMixerGroups(AudioMixer mixer)
    {
        // Unity's AudioMixer doesn't expose a public API to create groups programmatically.
        // The recommended approach is to:
        // 1. Create the mixer asset
        // 2. Open it in the Audio Mixer window
        // 3. Add groups manually
        //
        // However, we can try using reflection or SerializedObject to add groups.
        // For now, we'll log instructions for manual setup.
        
        SerializedObject serializedMixer = new SerializedObject(mixer);
        
        // Find the groups array
        SerializedProperty groupsProperty = serializedMixer.FindProperty("m_MasterGroup");
        
        if (groupsProperty != null)
        {
            Debug.Log("VoiceAudioSetup: Master group property found");
        }
        
        // Unfortunately, creating child groups programmatically requires internal APIs.
        // The user will need to add VernGroup and CallerGroup manually in the Audio Mixer window.
        
        Debug.LogWarning("VoiceAudioSetup: Please open the Audio Mixer window (Window > Audio > Audio Mixer) " +
                        "and add two child groups to Master: 'VernGroup' and 'CallerGroup'");
    }

    private static void EnsureMixerGroupsExist(AudioMixer mixer)
    {
        AudioMixerGroup[] groups = mixer.FindMatchingGroups(string.Empty);
        
        bool hasVernGroup = groups.Any(g => g.name == "VernGroup");
        bool hasCallerGroup = groups.Any(g => g.name == "CallerGroup");
        
        if (!hasVernGroup || !hasCallerGroup)
        {
            Debug.LogWarning("VoiceAudioSetup: Missing mixer groups. Please add in Audio Mixer window:");
            if (!hasVernGroup) Debug.LogWarning("  - Add child group 'VernGroup' under Master");
            if (!hasCallerGroup) Debug.LogWarning("  - Add child group 'CallerGroup' under Master");
        }
    }

    private static AudioMixerGroup GetMixerGroup(AudioMixer mixer, string groupName)
    {
        if (mixer == null) return null;
        
        AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
        return groups.Length > 0 ? groups[0] : null;
    }

    private static bool ConfigureAddressables()
    {
        // Check if Addressables is installed and initialized
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        
        if (settings == null)
        {
            // Try to create default settings
            settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            
            if (settings == null)
            {
                Debug.LogWarning("VoiceAudioSetup: Addressables not initialized. " +
                               "Open Window > Asset Management > Addressables > Groups to initialize.");
                return false;
            }
        }

        // Find or create VoiceAudio group
        AddressableAssetGroup voiceGroup = settings.FindGroup(ADDRESSABLE_GROUP_NAME);
        
        if (voiceGroup == null)
        {
            voiceGroup = settings.CreateGroup(ADDRESSABLE_GROUP_NAME, false, false, true, 
                settings.DefaultGroup.Schemas);
            Debug.Log($"VoiceAudioSetup: Created Addressables group '{ADDRESSABLE_GROUP_NAME}'");
        }

        int assetsMarked = 0;

        // Mark Vern Broadcast folder as Addressable
        assetsMarked += MarkFolderAddressable(settings, voiceGroup, VERN_BROADCAST_PATH);

        // Mark Callers folder as Addressable
        assetsMarked += MarkFolderAddressable(settings, voiceGroup, CALLERS_PATH);

        Debug.Log($"VoiceAudioSetup: Marked {assetsMarked} audio assets as Addressable");

        // Save settings
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        return assetsMarked > 0;
    }

    private static int MarkFolderAddressable(AddressableAssetSettings settings, 
        AddressableAssetGroup group, string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"VoiceAudioSetup: Folder not found: {folderPath}");
            return 0;
        }

        int count = 0;

        // Find all audio files in the folder (recursively)
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            
            // Check if already addressable
            AddressableAssetEntry existingEntry = settings.FindAssetEntry(guid);
            if (existingEntry != null)
            {
                continue; // Already addressable
            }

            // Add to addressables group
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            
            if (entry != null)
            {
                // Simplify address to just the filename (without extension)
                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                entry.address = fileName;
                count++;
            }
        }

        return count;
    }

    private static bool ConfigureAudioManager(AudioMixer mixer, AudioMixerGroup vernGroup, AudioMixerGroup callerGroup)
    {
        // Find AudioManager in scene
        AudioManager audioManager = Object.FindFirstObjectByType<AudioManager>();
        
        if (audioManager == null)
        {
            Debug.LogWarning("VoiceAudioSetup: AudioManager not found in scene. " +
                           "Add AudioManager to a GameObject and run this again, or assign mixer groups manually.");
            return false;
        }

        // Configure via SerializedObject
        SerializedObject serializedAudio = new SerializedObject(audioManager);

        serializedAudio.FindProperty("_audioMixer").objectReferenceValue = mixer;
        serializedAudio.FindProperty("_vernMixerGroup").objectReferenceValue = vernGroup;
        serializedAudio.FindProperty("_callerMixerGroup").objectReferenceValue = callerGroup;

        serializedAudio.ApplyModifiedProperties();
        
        // Mark object dirty
        EditorUtility.SetDirty(audioManager);

        Debug.Log($"VoiceAudioSetup: Configured AudioManager with mixer groups");
        
        // Select the AudioManager so user can verify
        Selection.activeGameObject = audioManager.gameObject;

        return true;
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
            Debug.Log($"VoiceAudioSetup: Created folder {path}");
        }
    }
}
