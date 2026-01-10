using UnityEngine;
using KBTV.Core;
using KBTV.Data;
using KBTV.Managers;
using KBTV.Callers;
using KBTV.UI;
using KBTV.Audio;
using KBTV.Dialogue;

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
        public static GameBootstrap Instance { get; private set; }

        /// <summary>
        /// All available topics for the game. Used by PreShow UI for topic selection.
        /// </summary>
        public Topic[] AvailableTopics => _availableTopics;

        /// <summary>
        /// Static helper to get available topics from anywhere.
        /// </summary>
        public static Topic[] GetAvailableTopics() => Instance?._availableTopics ?? System.Array.Empty<Topic>();

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

        [Header("Dialogue")]
        [Tooltip("Vern's dialogue template")]
        [SerializeField] private VernDialogueTemplate _vernDialogue;

        [Tooltip("Repository of conversation arcs")]
        [SerializeField] private ArcRepository _arcRepository;

        [Header("UI")]
        [SerializeField] private bool _enableLiveShowUI = true;
        [SerializeField] private bool _enableDebugUI = false;

        [Header("Audio")]
        [SerializeField] private bool _enableAudio = true;

        [Header("Debug")]
        [Tooltip("Automatically start the live show when the game begins (skip PreShow)")]
        [SerializeField] private bool _autoStartLiveShow = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            SetupManagers();
        }

        private void Start()
        {
            // Auto-advance to LiveShow for testing (after all managers have initialized in their Start())
            if (_autoStartLiveShow && GameStateManager.Instance != null)
            {
                // Use a small delay to ensure all managers have finished their Start() methods
                Invoke(nameof(StartLiveShow), 0.1f);
            }
        }

        private void StartLiveShow()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentPhase == GamePhase.PreShow)
            {
                GameStateManager.Instance.AdvancePhase();
            }
        }

        private void SetupManagers()
        {
            // Create GameStateManager
            if (GameStateManager.Instance == null)
            {
                GameObject gameManagerObj = new GameObject("GameStateManager");
                GameStateManager gameState = gameManagerObj.AddComponent<GameStateManager>();
                
                // Use reflection to set the VernStats since it's serialized
                // NOTE: This must happen BEFORE Initialize() is called
                var field = typeof(GameStateManager).GetField("_vernStats", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(gameState, _vernStatsAsset);

                // Now initialize the game (VernStats is set)
                gameState.InitializeGame();

                // Add LiveShowManager
                gameManagerObj.AddComponent<LiveShowManager>();
            }
            else
            {
                // GameStateManager already exists - make sure it has VernStats and is initialized
                if (GameStateManager.Instance.VernStats == null)
                {
                    var field = typeof(GameStateManager).GetField("_vernStats", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    field?.SetValue(GameStateManager.Instance, _vernStatsAsset);
                }
                GameStateManager.Instance.InitializeGame();
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

            // Create ConversationManager
            if (ConversationManager.Instance == null)
            {
                GameObject conversationObj = new GameObject("ConversationManager");
                ConversationManager conversationManager = conversationObj.AddComponent<ConversationManager>();

                // Set current topic and VernStats via reflection
                var topicField = typeof(ConversationManager).GetField("_currentTopic",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                topicField?.SetValue(conversationManager, _tonightsTopic);

                var statsField = typeof(ConversationManager).GetField("_vernStats",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                statsField?.SetValue(conversationManager, _vernStatsAsset);

                // Set Vern dialogue template via reflection
                var vernTemplateField = typeof(ConversationManager).GetField("_vernTemplate",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                vernTemplateField?.SetValue(conversationManager, _vernDialogue);

                // Set arc repository via reflection
                if (_arcRepository != null)
                {
                    var arcRepoField = typeof(ConversationManager).GetField("_arcRepository",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    arcRepoField?.SetValue(conversationManager, _arcRepository);
                }
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

                if (PreShowUIManager.Instance == null)
                {
                    GameObject preShowUIObj = new GameObject("PreShowUI");
                    preShowUIObj.AddComponent<PreShowUIManager>();
                }
            }

            // Create Debug UI
            if (_enableDebugUI)
            {
                GameObject debugUIObj = new GameObject("DebugUI");
                debugUIObj.AddComponent<DebugUI>();
            }
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
