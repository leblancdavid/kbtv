using Godot;
using KBTV.Core;
using KBTV.Callers;

namespace KBTV.UI
{
    /// <summary>
    /// Debug/testing utilities for the game.
    /// Provides methods to manually trigger game events for testing.
    /// </summary>
    public partial class DebugHelper : Node
    {
        private GameStateManager _gameState;
        private CallerQueue _callerQueue;
        private CallerGenerator _callerGenerator;

        public override void _Ready()
        {
            _gameState = GameStateManager.Instance;
            _callerQueue = CallerQueue.Instance;
            _callerGenerator = CallerGenerator.Instance;

            // Reduced debug output to prevent TextEdit overflow issues
            // GD.Print("DebugHelper: Ready for testing commands");
            // GD.Print("Available test commands:");
            // GD.Print("- start_show: Start live show");
            // GD.Print("- spawn_caller: Manually spawn a caller");
            // GD.Print("- approve_caller: Approve current screening caller");
            // GD.Print("- reject_caller: Reject current screening caller");
            // GD.Print("- end_call: End current on-air call");
            // GD.Print("- put_on_air: Put next caller on air");
        }

        // Debug methods that can be called from Godot editor or console

        public void StartShow()
        {
            if (_gameState != null)
            {
                GD.Print("DebugHelper: Starting live show");
                _gameState.StartLiveShow();
            }
        }

        public void SpawnCaller()
        {
            if (_callerGenerator != null)
            {
                var caller = _callerGenerator.SpawnCaller();
                if (caller != null)
                {
                    GD.Print($"DebugHelper: Spawned caller {caller.Name}");
                }
                else
                {
                    GD.Print("DebugHelper: Failed to spawn caller (queue full?)");
                }
            }
        }

        public void ApproveCaller()
        {
            if (_callerQueue != null && _callerQueue.IsScreening)
            {
                if (_callerQueue.ApproveCurrentCaller())
                {
                    GD.Print("DebugHelper: Approved caller");
                }
                else
                {
                    GD.Print("DebugHelper: Failed to approve caller");
                }
            }
            else
            {
                GD.Print("DebugHelper: No caller currently being screened");
            }
        }

        public void RejectCaller()
        {
            if (_callerQueue != null && _callerQueue.IsScreening)
            {
                if (_callerQueue.RejectCurrentCaller())
                {
                    GD.Print("DebugHelper: Rejected caller");
                }
                else
                {
                    GD.Print("DebugHelper: Failed to reject caller");
                }
            }
            else
            {
                GD.Print("DebugHelper: No caller currently being screened");
            }
        }

        public void EndCall()
        {
            if (_callerQueue != null && _callerQueue.IsOnAir)
            {
                var caller = _callerQueue.EndCurrentCall();
                if (caller != null)
                {
                    GD.Print($"DebugHelper: Ended call with {caller.Name}");
                }
            }
            else
            {
                GD.Print("DebugHelper: No caller currently on air");
            }
        }

        public void PutNextOnAir()
        {
            if (_callerQueue != null && _callerQueue.HasOnHoldCallers)
            {
                var caller = _callerQueue.PutNextCallerOnAir();
                if (caller != null)
                {
                    GD.Print($"DebugHelper: Put {caller.Name} on air");
                }
            }
            else
            {
                GD.Print("DebugHelper: No callers on hold");
            }
        }

        public void ShowGameState()
        {
            GD.Print("=== GAME STATE DEBUG ===");
            if (_gameState != null)
            {
                GD.Print($"Phase: {_gameState.CurrentPhase}");
                GD.Print($"Night: {_gameState.CurrentNight}");
            }

            if (_callerQueue != null)
            {
                GD.Print($"Incoming callers: {_callerQueue.IncomingCallers.Count}");
                GD.Print($"On hold callers: {_callerQueue.OnHoldCallers.Count}");
                GD.Print($"Is screening: {_callerQueue.IsScreening}");
                GD.Print($"Is on air: {_callerQueue.IsOnAir}");

                if (_callerQueue.IsScreening)
                    GD.Print($"Screening: {_callerQueue.CurrentScreening?.Name ?? "null"}");

                if (_callerQueue.IsOnAir)
                    GD.Print($"On air: {_callerQueue.OnAirCaller?.Name ?? "null"}");
            }

            GD.Print("======================");
        }

        // Simple keyboard shortcuts for testing (can be removed in production)
        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.F12)
            {
                ShowGameState();
            }
        }
    }
}