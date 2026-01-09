using UnityEngine;
using UnityEditor;
using KBTV.Callers;

namespace KBTV.Editor
{
    /// <summary>
    /// Editor utility to generate sample Topic assets for testing.
    /// Access via: Tools > KBTV > Generate Sample Topics
    /// </summary>
    public static class SampleTopicGenerator
    {
        private const string TOPICS_PATH = "Assets/Data/Topics";

        [MenuItem("Tools/KBTV/Generate Sample Topics")]
        public static void GenerateAllTopics()
        {
            EnsureDirectoriesExist();

            // UFOs / Aliens
            CreateTopic(
                "UFOs",
                "ufos",
                "Unidentified Flying Objects, alien spacecraft, close encounters",
                new[] { "ufo", "alien", "spacecraft", "flying saucer", "close encounter", "abduction" },
                0.25f,
                1.2f
            );

            // Government Conspiracies
            CreateTopic(
                "Government Conspiracies",
                "government",
                "Secret government programs, cover-ups, shadow agencies",
                new[] { "government", "cia", "fbi", "cover-up", "classified", "black ops", "deep state" },
                0.3f,
                1.0f
            );

            // Cryptids
            CreateTopic(
                "Cryptids",
                "cryptids",
                "Bigfoot, Loch Ness Monster, Chupacabra, and other mysterious creatures",
                new[] { "bigfoot", "sasquatch", "loch ness", "cryptid", "creature", "monster", "chupacabra" },
                0.2f,
                1.1f
            );

            // Ghosts & Hauntings
            CreateTopic(
                "Ghosts & Hauntings",
                "ghosts",
                "Paranormal activity, haunted locations, spirit encounters",
                new[] { "ghost", "haunting", "spirit", "paranormal", "poltergeist", "séance", "apparition" },
                0.15f,
                1.0f
            );

            // Ancient Mysteries
            CreateTopic(
                "Ancient Mysteries",
                "ancient",
                "Ancient aliens, lost civilizations, mysterious artifacts",
                new[] { "ancient", "pyramid", "atlantis", "artifact", "civilization", "megalith", "nazca" },
                0.2f,
                1.15f
            );

            // Time Travel
            CreateTopic(
                "Time Travel",
                "timetravel",
                "Time travelers, temporal anomalies, future predictions",
                new[] { "time travel", "future", "temporal", "chronology", "prediction", "mandela effect" },
                0.35f,
                1.3f
            );

            // Men in Black
            CreateTopic(
                "Men in Black",
                "mib",
                "Mysterious agents, witness intimidation, cover-up operations",
                new[] { "men in black", "mib", "agent", "silencer", "witness", "intimidation" },
                0.4f,
                1.25f
            );

            // Open Lines
            CreateTopic(
                "Open Lines",
                "open",
                "Any paranormal topic - callers choose their subject",
                new[] { "any", "open", "general", "paranormal", "strange", "weird", "unexplained" },
                0.1f,
                0.9f
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("KBTV: Sample topics generated successfully!");
            EditorUtility.DisplayDialog("KBTV", 
                $"Sample topics created in:\n{TOPICS_PATH}", 
                "OK");
        }

        private static void EnsureDirectoriesExist()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");

            if (!AssetDatabase.IsValidFolder(TOPICS_PATH))
                AssetDatabase.CreateFolder("Assets/Data", "Topics");
        }

        private static void CreateTopic(string displayName, string topicId, string description,
            string[] keywords, float deceptionRate, float qualityMultiplier)
        {
            string assetPath = $"{TOPICS_PATH}/{displayName.Replace(" ", "").Replace("&", "And")}.asset";

            // Check if already exists
            Topic existing = AssetDatabase.LoadAssetAtPath<Topic>(assetPath);
            if (existing != null)
            {
                Debug.Log($"Topic already exists: {assetPath}");
                return;
            }

            // Create new asset
            Topic topic = ScriptableObject.CreateInstance<Topic>();

            // Use SerializedObject to set private fields
            SerializedObject serialized = new SerializedObject(topic);
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_topicId").stringValue = topicId;
            serialized.FindProperty("_description").stringValue = description;
            serialized.FindProperty("_deceptionRate").floatValue = deceptionRate;
            serialized.FindProperty("_qualityMultiplier").floatValue = qualityMultiplier;

            // Set keywords
            SerializedProperty keywordsArray = serialized.FindProperty("_keywords");
            keywordsArray.arraySize = keywords.Length;
            for (int i = 0; i < keywords.Length; i++)
            {
                keywordsArray.GetArrayElementAtIndex(i).stringValue = keywords[i];
            }

            // Add a default rule: topic must match
            SerializedProperty rulesArray = serialized.FindProperty("_rules");
            rulesArray.arraySize = 1;
            SerializedProperty rule = rulesArray.GetArrayElementAtIndex(0);
            rule.FindPropertyRelative("_description").stringValue = $"Caller must be calling about {displayName}";
            rule.FindPropertyRelative("_ruleType").enumValueIndex = (int)ScreeningRuleType.TopicMustMatch;
            rule.FindPropertyRelative("_requiredValue").stringValue = topicId;
            rule.FindPropertyRelative("_isRequired").boolValue = true;

            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(topic, assetPath);
            Debug.Log($"Created topic: {assetPath}");
        }
    }
}
