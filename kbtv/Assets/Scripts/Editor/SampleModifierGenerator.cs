using UnityEngine;
using UnityEditor;
using KBTV.Data;

namespace KBTV.Editor
{
    /// <summary>
    /// Editor utility to generate sample StatModifier assets for testing.
    /// Access via: Tools > KBTV > Generate Sample Modifiers
    /// </summary>
    public static class SampleModifierGenerator
    {
        private const string ITEMS_PATH = "Assets/Data/Items";
        private const string EVENTS_PATH = "Assets/Data/Events";

        [MenuItem("Tools/KBTV/Generate Sample Modifiers")]
        public static void GenerateAllModifiers()
        {
            EnsureDirectoriesExist();

            // Items (things player gives to Vern)
            CreateModifier(ITEMS_PATH, "Coffee", "A hot cup of joe to keep Vern going",
                new StatModification(StatType.Energy, 15f),
                new StatModification(StatType.Thirst, -10f));

            CreateModifier(ITEMS_PATH, "Sandwich", "A filling meal to stave off hunger",
                new StatModification(StatType.Hunger, -25f),
                new StatModification(StatType.Energy, 5f));

            CreateModifier(ITEMS_PATH, "Water", "Basic hydration",
                new StatModification(StatType.Thirst, -20f));

            CreateModifier(ITEMS_PATH, "Cigarette", "Stress relief for the chain smoker",
                new StatModification(StatType.Patience, 10f),
                new StatModification(StatType.Energy, -5f));

            CreateModifier(ITEMS_PATH, "Whiskey", "Liquid courage - relaxing but impairing",
                new StatModification(StatType.Mood, 15f),
                new StatModification(StatType.Patience, 10f),
                new StatModification(StatType.Energy, -10f),
                new StatModification(StatType.Susceptibility, 10f));

            // Events (things that happen during the show)
            CreateModifier(EVENTS_PATH, "GoodCaller", "A compelling caller with interesting information",
                new StatModification(StatType.Belief, 10f),
                new StatModification(StatType.Mood, 5f));

            CreateModifier(EVENTS_PATH, "BadCaller", "An off-topic or crazy caller that wastes time",
                new StatModification(StatType.Belief, -15f),
                new StatModification(StatType.Mood, -10f),
                new StatModification(StatType.Patience, -10f));

            CreateModifier(EVENTS_PATH, "GreatCaller", "A caller with undeniable evidence",
                new StatModification(StatType.Belief, 20f),
                new StatModification(StatType.Mood, 10f),
                new StatModification(StatType.Energy, 5f));

            CreateModifier(EVENTS_PATH, "Evidence", "Compelling paranormal evidence surfaces",
                new StatModification(StatType.Belief, 20f),
                new StatModification(StatType.Susceptibility, 5f));

            CreateModifier(EVENTS_PATH, "Debunked", "Evidence gets debunked live on air",
                new StatModification(StatType.Belief, -25f),
                new StatModification(StatType.Mood, -15f));

            CreateModifier(EVENTS_PATH, "TechnicalDifficulties", "Equipment malfunction during broadcast",
                new StatModification(StatType.Mood, -10f),
                new StatModification(StatType.Patience, -15f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("KBTV: Sample modifiers generated successfully!");
            EditorUtility.DisplayDialog("KBTV", 
                "Sample modifiers created!\n\n" +
                $"Items: {ITEMS_PATH}\n" +
                $"Events: {EVENTS_PATH}", 
                "OK");
        }

        private static void EnsureDirectoriesExist()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");

            if (!AssetDatabase.IsValidFolder(ITEMS_PATH))
                AssetDatabase.CreateFolder("Assets/Data", "Items");

            if (!AssetDatabase.IsValidFolder(EVENTS_PATH))
                AssetDatabase.CreateFolder("Assets/Data", "Events");
        }

        private static void CreateModifier(string path, string name, string description, 
            params StatModification[] modifications)
        {
            string assetPath = $"{path}/{name}.asset";

            // Check if already exists
            StatModifier existing = AssetDatabase.LoadAssetAtPath<StatModifier>(assetPath);
            if (existing != null)
            {
                Debug.Log($"Modifier already exists: {assetPath}");
                return;
            }

            // Create new asset
            StatModifier modifier = ScriptableObject.CreateInstance<StatModifier>();

            // Use SerializedObject to set private fields
            SerializedObject serialized = new SerializedObject(modifier);
            serialized.FindProperty("_displayName").stringValue = name;
            serialized.FindProperty("_description").stringValue = description;

            SerializedProperty modsArray = serialized.FindProperty("_modifications");
            modsArray.arraySize = modifications.Length;

            for (int i = 0; i < modifications.Length; i++)
            {
                SerializedProperty element = modsArray.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("StatType").enumValueIndex = (int)modifications[i].StatType;
                element.FindPropertyRelative("Amount").floatValue = modifications[i].Amount;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(modifier, assetPath);
            Debug.Log($"Created modifier: {assetPath}");
        }
    }
}
