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

        public override void _Ready()
        {
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
                        if (!result.IsSuccess)
                        {
                            GD.PrintErr($"InputHandler: Failed to approve caller - {result.ErrorCode}: {result.ErrorMessage}");
                        }
                    }
                    break;

                case Key.N:
                    if (_repository.IsScreening)
                    {
                        var result = _repository.RejectScreening();
                        if (!result.IsSuccess)
                        {
                            GD.PrintErr($"InputHandler: Failed to reject caller - {result.ErrorCode}: {result.ErrorMessage}");
                        }
                    }
                    break;

                case Key.E:
                    if (_repository.IsOnAir)
                    {
                        var result = _repository.EndOnAir();
                        if (!result.IsSuccess)
                        {
                            GD.PrintErr($"InputHandler: Failed to end call - {result.ErrorCode}: {result.ErrorMessage}");
                        }
                    }
                    break;

                case Key.S:
                    if (!_repository.IsScreening && _repository.HasIncomingCallers)
                    {
                        var result = _repository.StartScreeningNext();
                        if (!result.IsSuccess)
                        {
                            GD.PrintErr($"InputHandler: Failed to start screening - {result.ErrorCode}: {result.ErrorMessage}");
                        }
                    }
                    break;
            }
        }

        private void ShowFeedback(string message)
        {
        }

        public override void _Process(double delta)
        {
        }
    }
}
