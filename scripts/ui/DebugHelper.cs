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
                Log.Error("DebugHelper: ICallerRepository not available");
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
        }

        private void AutoStartShow()
        {
            if (_gameState != null)
            {
                _gameState.StartLiveShow();
            }
        }

        // Debug methods that can be called from Godot editor or console

        public void StartShow()
        {
            if (_gameState != null)
            {
                _gameState.StartLiveShow();
            }
        }

        public void SpawnCaller()
        {
            if (_callerGenerator != null)
            {
                var caller = _callerGenerator.SpawnCaller();
                if (caller == null)
                {
                    Log.Error("DebugHelper: Failed to spawn caller (queue full?)");
                }
            }
        }

        public void ApproveCaller()
        {
            if (_repository != null && _repository.IsScreening)
            {
                var result = _repository.ApproveScreening();
                if (!result.IsSuccess)
                {
                    Log.Error($"DebugHelper: Failed to approve caller - {result.ErrorCode}: {result.ErrorMessage}");
                }
            }
        }

        public void RejectCaller()
        {
            if (_repository != null && _repository.IsScreening)
            {
                var result = _repository.RejectScreening();
                if (!result.IsSuccess)
                {
                    Log.Error($"DebugHelper: Failed to reject caller - {result.ErrorCode}: {result.ErrorMessage}");
                }
            }
        }

        public void EndCall()
        {
            if (_repository != null && _repository.IsOnAir)
            {
                var result = _repository.EndOnAir();
            }
        }

        public void PutNextOnAir()
        {
            if (_repository != null && _repository.HasOnHoldCallers)
            {
                var result = _repository.PutOnAir();
            }
        }

        public void ShowGameState()
        {
            if (_gameState != null)
            {
                Log.Debug($"Phase: {_gameState.CurrentPhase}, Night: {_gameState.CurrentNight}");
            }

            if (_repository != null)
            {
                Log.Debug($"Callers - Incoming: {_repository.IncomingCallers.Count}, Hold: {_repository.OnHoldCallers.Count}, Screening: {_repository.IsScreening}, OnAir: {_repository.IsOnAir}");
            }
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