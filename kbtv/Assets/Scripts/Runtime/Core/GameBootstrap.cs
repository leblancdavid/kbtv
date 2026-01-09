using UnityEngine;
using KBTV.Core;
using KBTV.Data;
using KBTV.Managers;
using KBTV.Callers;
using KBTV.UI;
using KBTV.Audio;

namespace KBTV
{
    /// <summary>
    /// Bootstrapper that creates and initializes all core game systems.
    /// Add this to an empty GameObject in your scene to set up the game.
    /// 
    /// Required setup in Unity:
    /// 1. Create a VernStats ScriptableObject: Assets > Create > KBTV > Vern Stats
    /// 2. Assign it to this component's VernStats field
    /// 3. Optionally assign a Topic for tonight's show
    /// 4. Press Play to test the systems
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private VernStats _vernStatsAsset;
        [SerializeField] private Topic _tonightsTopic;

        [Header("Show Settings")]
        [Tooltip("Duration of live show in real-time seconds (default: 60 for testing)")]
        [SerializeField] private float _showDuration = 60f;

        [Header("Caller Settings")]
        [Tooltip("All available topics for caller generation")]
        [SerializeField] private Topic[] _availableTopics;

        [Header("Stat Modifiers for Callers")]
        [SerializeField] private StatModifier _goodCallerModifier;
        [SerializeField] private StatModifier _badCallerModifier;
        [SerializeField] private StatModifier _greatCallerModifier;

        [Header("Items")]
        [Tooltip("Available items for Vern to use during shows (StatModifier or Item assets)")]
        [SerializeField] private StatModifier[] _availableItems;

        [Header("UI")]
        [SerializeField] private bool _enableLiveShowUI = true;
        [SerializeField] private bool _enableDebugUI = false;

        [Header("Audio")]
        [SerializeField] private bool _enableAudio = true;

        private void Awake()
        {
            SetupManagers();
        }

        private void SetupManagers()
        {
            // Create GameStateManager
            if (GameStateManager.Instance == null)
            {
                GameObject gameManagerObj = new GameObject("GameStateManager");
                GameStateManager gameState = gameManagerObj.AddComponent<GameStateManager>();
                
                // Use reflection to set the VernStats since it's serialized
                var field = typeof(GameStateManager).GetField("_vernStats", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(gameState, _vernStatsAsset);

                // Add LiveShowManager
                gameManagerObj.AddComponent<LiveShowManager>();
            }

            // Create TimeManager
            if (TimeManager.Instance == null)
            {
                GameObject timeManagerObj = new GameObject("TimeManager");
                TimeManager timeManager = timeManagerObj.AddComponent<TimeManager>();
                
                // Set show duration
                var durationField = typeof(TimeManager).GetField("_showDurationSeconds",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                durationField?.SetValue(timeManager, _showDuration);
            }

            // Create CallerQueue
            if (CallerQueue.Instance == null)
            {
                GameObject callerQueueObj = new GameObject("CallerQueue");
                callerQueueObj.AddComponent<CallerQueue>();
            }

            // Create CallerGenerator
            if (CallerGenerator.Instance == null)
            {
                GameObject callerGenObj = new GameObject("CallerGenerator");
                CallerGenerator callerGen = callerGenObj.AddComponent<CallerGenerator>();

                // Set available topics
                if (_availableTopics != null && _availableTopics.Length > 0)
                {
                    var topicsField = typeof(CallerGenerator).GetField("_availableTopics",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    topicsField?.SetValue(callerGen, _availableTopics);
                }
            }

            // Create CallerScreeningManager
            if (CallerScreeningManager.Instance == null)
            {
                GameObject screeningObj = new GameObject("CallerScreeningManager");
                CallerScreeningManager screening = screeningObj.AddComponent<CallerScreeningManager>();

                // Set topic and modifiers via reflection
                var topicField = typeof(CallerScreeningManager).GetField("_currentTopic",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                topicField?.SetValue(screening, _tonightsTopic);

                var goodField = typeof(CallerScreeningManager).GetField("_goodCallerModifier",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                goodField?.SetValue(screening, _goodCallerModifier);

                var badField = typeof(CallerScreeningManager).GetField("_badCallerModifier",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                badField?.SetValue(screening, _badCallerModifier);

                var greatField = typeof(CallerScreeningManager).GetField("_greatCallerModifier",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                greatField?.SetValue(screening, _greatCallerModifier);
            }

            // Create ListenerManager
            if (ListenerManager.Instance == null)
            {
                GameObject listenerObj = new GameObject("ListenerManager");
                listenerObj.AddComponent<ListenerManager>();
            }

            // Create ItemManager
            if (ItemManager.Instance == null)
            {
                GameObject itemManagerObj = new GameObject("ItemManager");
                ItemManager itemManager = itemManagerObj.AddComponent<ItemManager>();

                // Set available items via reflection
                if (_availableItems != null && _availableItems.Length > 0)
                {
                    var itemsField = typeof(ItemManager).GetField("_availableItems",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    itemsField?.SetValue(itemManager, _availableItems);
                }
            }

            // Create AudioManager
            if (_enableAudio && AudioManager.Instance == null)
            {
                GameObject audioObj = new GameObject("AudioManager");
                audioObj.AddComponent<AudioManager>();
            }

            // Create Live Show UI
            if (_enableLiveShowUI)
            {
                if (LiveShowUIManager.Instance == null)
                {
                    GameObject liveShowUIObj = new GameObject("LiveShowUI");
                    liveShowUIObj.AddComponent<LiveShowUIManager>();
                }
            }

            // Create Debug UI
            if (_enableDebugUI)
            {
                GameObject debugUIObj = new GameObject("DebugUI");
                debugUIObj.AddComponent<DebugUI>();
            }

            Debug.Log("GameBootstrap: All systems initialized");
        }

        private void OnValidate()
        {
            if (_vernStatsAsset == null)
            {
                Debug.LogWarning("GameBootstrap: VernStats asset not assigned! Create one via Assets > Create > KBTV > Vern Stats");
            }
        }
    }
}
