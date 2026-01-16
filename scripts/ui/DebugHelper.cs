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
        private ICallerRepository _repository;
        private CallerGenerator _callerGenerator;

        public override void _Ready()
        {
            if (ServiceRegistry.Instance == null)
            {
                GD.PrintErr("DebugHelper: ServiceRegistry not available");
            }
            else
            {
                _repository = ServiceRegistry.Instance.CallerRepository;
            }

            if (_repository == null)
            {
                GD.PrintErr("DebugHelper: ICallerRepository not available");
            }

            _gameState = ServiceRegistry.Instance.GameStateManager;
            _callerGenerator = ServiceRegistry.Instance.CallerGenerator;

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
            if (_repository != null && _repository.IsScreening)
            {
                var result = _repository.ApproveScreening();
                if (result.IsSuccess)
                {
                    GD.Print("DebugHelper: Approved caller");
                }
                else
                {
                    GD.Print($"DebugHelper: Failed to approve caller - {result.ErrorCode}: {result.ErrorMessage}");
                }
            }
            else
            {
                GD.Print("DebugHelper: No caller currently being screened");
            }
        }

        public void RejectCaller()
        {
            if (_repository != null && _repository.IsScreening)
            {
                var result = _repository.RejectScreening();
                if (result.IsSuccess)
                {
                    GD.Print("DebugHelper: Rejected caller");
                }
                else
                {
                    GD.Print($"DebugHelper: Failed to reject caller - {result.ErrorCode}: {result.ErrorMessage}");
                }
            }
            else
            {
                GD.Print("DebugHelper: No caller currently being screened");
            }
        }

        public void EndCall()
        {
            if (_repository != null && _repository.IsOnAir)
            {
                var result = _repository.EndOnAir();
                if (result.IsSuccess)
                {
                    var caller = result.Value;
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
            if (_repository != null && _repository.HasOnHoldCallers)
            {
                var result = _repository.PutOnAir();
                if (result.IsSuccess)
                {
                    var caller = result.Value;
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

            if (_repository != null)
            {
                GD.Print($"Incoming callers: {_repository.IncomingCallers.Count}");
                GD.Print($"On hold callers: {_repository.OnHoldCallers.Count}");
                GD.Print($"Is screening: {_repository.IsScreening}");
                GD.Print($"Is on air: {_repository.IsOnAir}");

                if (_repository.IsScreening)
                    GD.Print($"Screening: {_repository.CurrentScreening?.Name ?? "null"}");

                if (_repository.IsOnAir)
                    GD.Print($"On air: {_repository.OnAirCaller?.Name ?? "null"}");
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