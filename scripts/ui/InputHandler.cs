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
        private CallerQueue _callerQueue;

        // Input action names (defined in Godot project settings)
        private const string ACTION_SCREEN_ACCEPT = "screen_accept";
        private const string ACTION_SCREEN_REJECT = "screen_reject";
        private const string ACTION_END_CALL = "end_call";
        private const string ACTION_START_BREAK = "start_break";

        public override void _Ready()
        {
            _gameState = GameStateManager.Instance;
            _callerQueue = CallerQueue.Instance;

            GD.Print("InputHandler: Initialized input handling");
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
            switch (keycode)
            {
                case Key.Y: // Accept caller (Y key)
                    if (_callerQueue.IsScreening)
                    {
                        if (_callerQueue.ApproveCurrentCaller())
                        {
                            GD.Print("InputHandler: Approved caller");
                            ShowFeedback("Caller approved!");
                        }
                    }
                    break;

                case Key.N: // Reject caller (N key)
                    if (_callerQueue.IsScreening)
                    {
                        if (_callerQueue.RejectCurrentCaller())
                        {
                            GD.Print("InputHandler: Rejected caller");
                            ShowFeedback("Caller rejected!");
                        }
                    }
                    break;

                case Key.E: // End current call (E key)
                    if (_callerQueue.IsOnAir)
                    {
                        var completed = _callerQueue.EndCurrentCall();
                        if (completed != null)
                        {
                            GD.Print("InputHandler: Ended call");
                            ShowFeedback("Call ended!");

                            // Auto-put next caller on air if available
                            if (_callerQueue.HasOnHoldCallers)
                            {
                                var nextCaller = _callerQueue.PutNextCallerOnAir();
                                if (nextCaller != null)
                                {
                                    GD.Print("InputHandler: Put next caller on air");
                                    ShowFeedback($"{nextCaller.Name} is now on air!");
                                }
                            }
                        }
                    }
                    break;

                case Key.S: // Start screening next caller (S key)
                    if (!_callerQueue.IsScreening && _callerQueue.HasIncomingCallers)
                    {
                        var caller = _callerQueue.StartScreeningNext();
                        if (caller != null)
                        {
                            GD.Print($"InputHandler: Started screening {caller.Name}");
                            ShowFeedback($"Now screening: {caller.Name}");
                        }
                    }
                    break;

                case Key.Space: // Put next caller on air (Space key)
                    if (!_callerQueue.IsOnAir && _callerQueue.HasOnHoldCallers)
                    {
                        var caller = _callerQueue.PutNextCallerOnAir();
                        if (caller != null)
                        {
                            GD.Print($"InputHandler: Put {caller.Name} on air");
                            ShowFeedback($"{caller.Name} is now on air!");
                        }
                    }
                    break;
            }
        }

        private void ShowFeedback(string message)
        {
            // TODO: Implement visual feedback system
            // For now, just log to console
            GD.Print($"FEEDBACK: {message}");
        }

        // Optional: Add controller/gamepad support
        public override void _Process(double delta)
        {
            // Handle any continuous input processing here
            // (e.g., hold buttons for faster actions)
        }
    }
}