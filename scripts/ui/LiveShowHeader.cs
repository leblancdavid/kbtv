using Godot;
using KBTV.Core;
using KBTV.Managers;

namespace KBTV.UI
{
    public partial class LiveShowHeader : Panel
    {
        private Label _onAirLabel;
        private Label _clockLabel;
        private Label _listenerLabel;

        public override void _Ready()
        {
            _onAirLabel = GetNode<Label>("HBoxContainer/OnAirContainer/OnAirLabel");
            _clockLabel = GetNode<Label>("HBoxContainer/ClockLabel");
            _listenerLabel = GetNode<Label>("HBoxContainer/ListenerLabel");

            // Connect to managers
            var gameState = GameStateManager.Instance;
            if (gameState != null)
            {
                gameState.Connect("PhaseChanged", Callable.From<GamePhase, GamePhase>(OnPhaseChanged));
                UpdateOnAirVisibility(gameState.CurrentPhase);
            }

            var timeManager = TimeManager.Instance;
            if (timeManager != null)
            {
                // Connect to time updates (will be signal later)
                // For now, update manually
                UpdateClock();
            }

            var listenerManager = ListenerManager.Instance;
            if (listenerManager != null)
            {
                // Connect to listener updates
                UpdateListeners();
            }
        }

        private void OnPhaseChanged(GamePhase oldPhase, GamePhase newPhase)
        {
            UpdateOnAirVisibility(newPhase);
        }

        private void UpdateOnAirVisibility(GamePhase phase)
        {
            var onAirContainer = GetNode<Control>("HBoxContainer/OnAirContainer");
            onAirContainer.Visible = phase == GamePhase.LiveShow;
        }

        private void UpdateClock()
        {
            var timeManager = TimeManager.Instance;
            if (timeManager != null && _clockLabel != null)
            {
                var current = timeManager.CurrentTimeFormatted ?? "12:00 AM";
                var remaining = timeManager.RemainingTimeFormatted ?? "45:00";
                _clockLabel.Text = $"{current} ({remaining})";
            }
        }

        private void UpdateListeners()
        {
            var listenerManager = ListenerManager.Instance;
            if (listenerManager != null && _listenerLabel != null)
            {
                var count = listenerManager.GetFormattedListeners();
                var change = listenerManager.GetFormattedChange();
                var color = listenerManager.ListenerChange >= 0 ? new Color(0f, 0.8f, 0f) : new Color(0.8f, 0.2f, 0.2f);
                _listenerLabel.Text = $"{count} ({change})";
                _listenerLabel.AddThemeColorOverride("font_color", color);
            }
        }

        // Call these from UIManagerBootstrap for updates
        public void RefreshClock()
        {
            UpdateClock();
        }

        public void RefreshListeners()
        {
            UpdateListeners();
        }
    }
}