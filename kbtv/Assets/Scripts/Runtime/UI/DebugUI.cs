using UnityEngine;
using KBTV.Core;
using KBTV.Data;
using KBTV.Managers;

namespace KBTV.UI
{
    /// <summary>
    /// Debug UI for testing core systems.
    /// Displays Vern's stats, game phase, and provides controls.
    /// Uses Unity's IMGUI for quick prototyping.
    /// </summary>
    public class DebugUI : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _showUI = true;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F1;

        private GameStateManager _gameState;
        private TimeManager _timeManager;
        private VernStats _stats;

        private void Start()
        {
            _gameState = GameStateManager.Instance;
            _timeManager = TimeManager.Instance;

            if (_gameState != null)
            {
                _stats = _gameState.VernStats;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                _showUI = !_showUI;
            }
        }

        private void OnGUI()
        {
            if (!_showUI) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 500));
            GUILayout.BeginVertical("box");

            DrawHeader();
            DrawPhaseInfo();
            DrawTimeInfo();
            DrawStats();
            DrawControls();

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawHeader()
        {
            GUILayout.Label("<size=16><b>KBTV Debug Console</b></size>");
            GUILayout.Label($"Press {_toggleKey} to toggle");
            GUILayout.Space(10);
        }

        private void DrawPhaseInfo()
        {
            if (_gameState == null)
            {
                GUILayout.Label("<color=red>GameStateManager not found!</color>");
                return;
            }

            string phaseColor = _gameState.CurrentPhase switch
            {
                GamePhase.PreShow => "yellow",
                GamePhase.LiveShow => "red",
                GamePhase.PostShow => "green",
                _ => "white"
            };

            GUILayout.Label($"<b>Night:</b> {_gameState.CurrentNight}");
            GUILayout.Label($"<b>Phase:</b> <color={phaseColor}>{_gameState.CurrentPhase}</color>");
            GUILayout.Space(5);
        }

        private void DrawTimeInfo()
        {
            if (_timeManager == null)
            {
                GUILayout.Label("<color=red>TimeManager not found!</color>");
                return;
            }

            string runningStatus = _timeManager.IsRunning ? "<color=lime>LIVE</color>" : "<color=gray>PAUSED</color>";
            
            GUILayout.Label($"<b>Status:</b> {runningStatus}");
            GUILayout.Label($"<b>Time:</b> {_timeManager.CurrentTimeFormatted}");
            GUILayout.Label($"<b>Remaining:</b> {_timeManager.RemainingTimeFormatted}");

            // Progress bar
            GUILayout.BeginHorizontal();
            GUILayout.Label("Progress:", GUILayout.Width(60));
            float progress = _timeManager.Progress;
            GUILayout.HorizontalSlider(progress, 0f, 1f, GUILayout.Width(150));
            GUILayout.Label($"{(progress * 100):F0}%", GUILayout.Width(40));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
        }

        private void DrawStats()
        {
            GUILayout.Label("<b>--- Vern's Stats ---</b>");

            if (_stats == null)
            {
                GUILayout.Label("<color=red>VernStats not initialized!</color>");
                return;
            }

            DrawStatBar("Mood", _stats.Mood, Color.magenta);
            DrawStatBar("Energy", _stats.Energy, Color.yellow);
            DrawStatBar("Hunger", _stats.Hunger, new Color(1f, 0.5f, 0f)); // Orange
            DrawStatBar("Thirst", _stats.Thirst, Color.cyan);
            DrawStatBar("Patience", _stats.Patience, Color.gray);
            DrawStatBar("Susceptibility", _stats.Susceptibility, new Color(0.5f, 0f, 0.5f)); // Purple

            GUILayout.Space(5);
            DrawStatBar("BELIEF", _stats.Belief, Color.green);

            GUILayout.Space(5);
            float quality = _stats.CalculateShowQuality();
            GUILayout.Label($"<b>Show Quality:</b> {(quality * 100):F0}%");

            GUILayout.Space(10);
        }

        private void DrawStatBar(string name, Stat stat, Color color)
        {
            if (stat == null) return;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{name}:", GUILayout.Width(100));

            // Create a colored style for the bar
            GUI.color = color;
            GUILayout.HorizontalSlider(stat.Value, stat.MinValue, stat.MaxValue, GUILayout.Width(120));
            GUI.color = Color.white;

            GUILayout.Label($"{stat.Value:F0}", GUILayout.Width(35));
            GUILayout.EndHorizontal();
        }

        private void DrawControls()
        {
            GUILayout.Label("<b>--- Controls ---</b>");

            if (_gameState == null) return;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Advance Phase"))
            {
                _gameState.AdvancePhase();
            }

            if (GUILayout.Button("New Night"))
            {
                _gameState.StartNewNight();
            }
            GUILayout.EndHorizontal();

            // Test stat modifications
            GUILayout.Space(5);
            GUILayout.Label("<b>Test Modifications:</b>");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+10 Energy"))
            {
                _stats?.Energy.Modify(10f);
            }
            if (GUILayout.Button("-10 Energy"))
            {
                _stats?.Energy.Modify(-10f);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+10 Belief"))
            {
                _stats?.Belief.Modify(10f);
            }
            if (GUILayout.Button("-10 Belief"))
            {
                _stats?.Belief.Modify(-10f);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-20 Hunger"))
            {
                _stats?.Hunger.Modify(-20f);
            }
            if (GUILayout.Button("-20 Thirst"))
            {
                _stats?.Thirst.Modify(-20f);
            }
            GUILayout.EndHorizontal();
        }
    }
}
