using Godot;
using KBTV.Core;
using KBTV.Callers;
using KBTV.Managers;

namespace KBTV.UI
{
    /// <summary>
    /// Debug/testing utilities for the game.
    /// Provides methods to manually trigger game events for testing.
    /// </summary>
    public partial class DebugHelper : Node, IDependent
    {
        private GameStateManager _gameState;
        private ICallerRepository _repository;
        private CallerGenerator _callerGenerator;

        public override void _Notification(int what) => this.Notify(what);

        public override void _Ready()
        {
            // Initialization moved to OnResolved
        }

        public void OnResolved()
        {
            _repository = DependencyInjection.Get<ICallerRepository>(this);
            _gameState = DependencyInjection.Get<GameStateManager>(this);
            _callerGenerator = DependencyInjection.Get<CallerGenerator>(this);

            if (_repository == null)
            {
                GD.PrintErr("DebugHelper: ICallerRepository not available");
            }

            // Auto-start live show for testing after 2 seconds
            CallDeferred(nameof(StartAutoShowTimer));
        }

        private void StartAutoShowTimer()
        {
            var timer = new Timer();
            timer.WaitTime = 2.0f; // 2 seconds
            timer.OneShot = true;
            timer.Timeout += () => AutoStartShow();
            AddChild(timer);
            timer.Start();
            GD.Print("DebugHelper: Auto-start timer set for 2 seconds");
        }

        private void AutoStartShow()
        {
            if (_gameState != null && _gameState.CurrentPhase == GamePhase.PreShow)
            {
                GD.Print("DebugHelper: Auto-starting live show for testing");
                _gameState.StartLiveShow();
            }
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