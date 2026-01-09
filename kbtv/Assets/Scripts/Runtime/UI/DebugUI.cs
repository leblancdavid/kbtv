using UnityEngine;
using KBTV.Core;
using KBTV.Data;
using KBTV.Managers;
using KBTV.Callers;

namespace KBTV.UI
{
    /// <summary>
    /// Debug UI for testing core systems.
    /// Displays Vern's stats, game phase, callers, and provides controls.
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
        private CallerQueue _callerQueue;
        private CallerGenerator _callerGenerator;
        private CallerScreeningManager _screeningManager;

        private Vector2 _callerScrollPos;

        private void Start()
        {
            _gameState = GameStateManager.Instance;
            _timeManager = TimeManager.Instance;
            _callerQueue = CallerQueue.Instance;
            _callerGenerator = CallerGenerator.Instance;
            _screeningManager = CallerScreeningManager.Instance;

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

            // Left panel - Stats and Controls
            GUILayout.BeginArea(new Rect(10, 10, 300, 550));
            GUILayout.BeginVertical("box");

            DrawHeader();
            DrawPhaseInfo();
            DrawTimeInfo();
            DrawStats();
            DrawControls();

            GUILayout.EndVertical();
            GUILayout.EndArea();

            // Right panel - Callers
            GUILayout.BeginArea(new Rect(320, 10, 350, 550));
            GUILayout.BeginVertical("box");

            DrawCallerPanel();

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

        private void DrawCallerPanel()
        {
            GUILayout.Label("<size=16><b>Caller Management</b></size>");
            GUILayout.Space(5);

            if (_callerQueue == null)
            {
                GUILayout.Label("<color=yellow>CallerQueue not found - add CallerQueue component</color>");
                return;
            }

            // Queue status
            GUILayout.Label($"<b>Incoming:</b> {_callerQueue.IncomingCallers.Count} | <b>On Hold:</b> {_callerQueue.OnHoldCallers.Count}");

            // Caller controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Caller"))
            {
                _callerGenerator?.SpawnCaller();
            }
            if (GUILayout.Button("Clear All"))
            {
                _callerQueue.ClearAll();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Current screening
            DrawCurrentScreening();

            // On-air caller
            DrawOnAirCaller();

            // Incoming queue
            DrawIncomingQueue();

            // On-hold queue
            DrawOnHoldQueue();
        }

        private void DrawCurrentScreening()
        {
            GUILayout.Label("<b>--- Screening ---</b>");

            Caller screening = _callerQueue.CurrentScreening;
            if (screening == null)
            {
                if (_callerQueue.HasIncomingCallers)
                {
                    if (GUILayout.Button("Screen Next Caller"))
                    {
                        _callerQueue.StartScreeningNext();
                    }
                }
                else
                {
                    GUILayout.Label("<color=gray>No caller being screened</color>");
                }
            }
            else
            {
                GUILayout.Label($"<color=cyan>{screening.Name}</color>");
                GUILayout.Label($"Phone: {screening.PhoneNumber}");
                GUILayout.Label($"From: {screening.Location}");
                GUILayout.Label($"Topic: {screening.ClaimedTopic}");
                GUILayout.Label($"Reason: {screening.CallReason}");
                GUILayout.Label($"Wait: {screening.WaitTime:F1}s / {screening.Patience:F0}s");

                GUILayout.BeginHorizontal();
                GUI.color = Color.green;
                if (GUILayout.Button("APPROVE"))
                {
                    _callerQueue.ApproveCurrentCaller();
                }
                GUI.color = Color.red;
                if (GUILayout.Button("REJECT"))
                {
                    _callerQueue.RejectCurrentCaller();
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);
        }

        private void DrawOnAirCaller()
        {
            GUILayout.Label("<b>--- ON AIR ---</b>");

            Caller onAir = _callerQueue.OnAirCaller;
            if (onAir == null)
            {
                if (_callerQueue.HasOnHoldCallers)
                {
                    if (GUILayout.Button("Put Next On Air"))
                    {
                        _callerQueue.PutNextCallerOnAir();
                    }
                }
                else
                {
                    GUILayout.Label("<color=gray>No one on air</color>");
                }
            }
            else
            {
                string legitimacyColor = onAir.Legitimacy switch
                {
                    CallerLegitimacy.Fake => "red",
                    CallerLegitimacy.Questionable => "yellow",
                    CallerLegitimacy.Credible => "white",
                    CallerLegitimacy.Compelling => "lime",
                    _ => "white"
                };

                GUILayout.Label($"<color=lime><b>{onAir.Name}</b></color> - LIVE");
                GUILayout.Label($"Actual Topic: {onAir.ActualTopic}");
                GUILayout.Label($"Legitimacy: <color={legitimacyColor}>{onAir.Legitimacy}</color>");

                if (onAir.IsLyingAboutTopic)
                {
                    GUILayout.Label("<color=red>LIAR - Wrong topic!</color>");
                }

                if (GUILayout.Button("End Call"))
                {
                    _callerQueue.EndCurrentCall();
                }
            }

            GUILayout.Space(5);
        }

        private void DrawIncomingQueue()
        {
            GUILayout.Label($"<b>--- Incoming ({_callerQueue.IncomingCallers.Count}) ---</b>");

            if (_callerQueue.IncomingCallers.Count == 0)
            {
                GUILayout.Label("<color=gray>No incoming calls</color>");
                return;
            }

            _callerScrollPos = GUILayout.BeginScrollView(_callerScrollPos, GUILayout.Height(80));
            foreach (var caller in _callerQueue.IncomingCallers)
            {
                float patiencePercent = 1f - (caller.WaitTime / caller.Patience);
                string patienceColor = patiencePercent > 0.5f ? "white" : patiencePercent > 0.25f ? "yellow" : "red";
                GUILayout.Label($"<color={patienceColor}>{caller.Name}</color> - {caller.ClaimedTopic} ({caller.WaitTime:F0}s)");
            }
            GUILayout.EndScrollView();
        }

        private void DrawOnHoldQueue()
        {
            GUILayout.Label($"<b>--- On Hold ({_callerQueue.OnHoldCallers.Count}) ---</b>");

            if (_callerQueue.OnHoldCallers.Count == 0)
            {
                GUILayout.Label("<color=gray>No callers on hold</color>");
                return;
            }

            foreach (var caller in _callerQueue.OnHoldCallers)
            {
                GUILayout.Label($"{caller.Name} - {caller.ClaimedTopic}");
            }
        }
    }
}
