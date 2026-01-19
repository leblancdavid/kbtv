using Godot;
using KBTV.Core;
using KBTV.Callers;

namespace KBTV.UI
{
    /// <summary>
    /// Handles player input for game controls.
    /// Manages caller screening and show management actions.
    /// </summary>
    public partial class InputHandler : Node
    {
        private GameStateManager _gameState;
        private ICallerRepository _repository;

        // Input action names (defined in Godot project settings)
        private const string ACTION_SCREEN_ACCEPT = "screen_accept";
        private const string ACTION_SCREEN_REJECT = "screen_reject";
        private const string ACTION_END_CALL = "end_call";
        private const string ACTION_START_BREAK = "start_break";

        public override void _Ready()
        {
            CallDeferred(nameof(InitializeDeferred));
        }

        private void InitializeDeferred()
        {
            if (!Core.ServiceRegistry.IsInitialized)
            {
                GD.PrintErr("InputHandler: ServiceRegistry not initialized, retrying...");
                CallDeferred(nameof(InitializeDeferred));
                return;
            }

            _gameState = Core.ServiceRegistry.Instance.GameStateManager;
            _repository = Core.ServiceRegistry.Instance.CallerRepository;

            if (_repository == null)
            {
                GD.PrintErr("InputHandler: ICallerRepository not available");
                return;
            }
        }

        public override void _Input(InputEvent @event)
        {
            // Only handle input during live shows
            if (_gameState == null || _gameState.CurrentPhase != GamePhase.LiveShow)
                return;

            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                HandleKeyInput(keyEvent.Keycode);
            }
        }

        private void HandleKeyInput(Key keycode)
        {
            if (_repository == null)
            {
                return;
            }

            switch (keycode)
            {
                case Key.Y:
                    if (_repository.IsScreening)
                    {
                        var result = _repository.ApproveScreening();
                        if (result.IsSuccess)
                        {
                            ShowFeedback("Caller approved!");
                        }
                        else
                        {
                            GD.PrintErr($"InputHandler: Failed to approve caller - {result.ErrorCode}: {result.ErrorMessage}");
                        }
                    }
                    break;

                case Key.N:
                    if (_repository.IsScreening)
                    {
                        var result = _repository.RejectScreening();
                        if (result.IsSuccess)
                        {
                            ShowFeedback("Caller rejected!");
                        }
                        else
                        {
                            GD.PrintErr($"InputHandler: Failed to reject caller - {result.ErrorCode}: {result.ErrorMessage}");
                        }
                    }
                    break;

                case Key.E:
                    if (_repository.IsOnAir)
                    {
                        var result = _repository.EndOnAir();
                        if (result.IsSuccess)
                        {
                            ShowFeedback("Call ended!");
                            if (_repository.HasOnHoldCallers)
                            {
                                var nextResult = _repository.PutOnAir();
                                if (nextResult.IsSuccess)
                                {
                                    var nextCaller = nextResult.Value;
                                    ShowFeedback($"{nextCaller.Name} is now on air!");
                                }
                            }
                        }
                        else
                        {
                            GD.PrintErr($"InputHandler: Failed to end call - {result.ErrorCode}: {result.ErrorMessage}");
                        }
                    }
                    break;

                case Key.S:
                    if (!_repository.IsScreening && _repository.HasIncomingCallers)
                    {
                        var result = _repository.StartScreeningNext();
                        if (result.IsSuccess)
                        {
                            var caller = result.Value;
                            ShowFeedback($"Now screening: {caller.Name}");
                        }
                        else
                        {
                            GD.PrintErr($"InputHandler: Failed to start screening - {result.ErrorCode}: {result.ErrorMessage}");
                        }
                    }
                    break;

                case Key.Space:
                    if (!_repository.IsOnAir && _repository.HasOnHoldCallers)
                    {
                        var result = _repository.PutOnAir();
                        if (result.IsSuccess)
                        {
                            var caller = result.Value;
                            ShowFeedback($"{caller.Name} is now on air!");
                        }
                        else
                        {
                            GD.PrintErr($"InputHandler: Failed to put caller on air - {result.ErrorCode}: {result.ErrorMessage}");
                        }
                    }
                    break;

                case Key.P:
                    goto case Key.Space;
            }
        }

        private void ShowFeedback(string message)
        {
        }

        // Optional: Add controller/gamepad support
        public override void _Process(double delta)
        {
            // Handle any continuous input processing here
            // (e.g., hold buttons for faster actions)
        }
    }
}