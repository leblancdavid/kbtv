using UnityEngine;
using KBTV.Core;
using KBTV.Data;
using KBTV.Managers;
using KBTV.UI;

namespace KBTV
{
    /// <summary>
    /// Bootstrapper that creates and initializes all core game systems.
    /// Add this to an empty GameObject in your scene to set up the game.
    /// 
    /// Required setup in Unity:
    /// 1. Create a VernStats ScriptableObject: Assets > Create > KBTV > Vern Stats
    /// 2. Assign it to this component's VernStats field
    /// 3. Press Play to test the systems
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private VernStats _vernStatsAsset;

        [Header("Show Settings")]
        [Tooltip("Duration of live show in real-time seconds (default: 60 for testing)")]
        [SerializeField] private float _showDuration = 60f;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugUI = true;

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
