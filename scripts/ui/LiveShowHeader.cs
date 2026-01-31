using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Economy;
using KBTV.Managers;

namespace KBTV.UI
{
    /// <summary>
    /// Header component for the liveshow UI.
    /// Displays on-air status, listener count, and money balance.
    /// </summary>
    public partial class LiveShowHeader : Control, IDependent
    {
    private Label _onAirLabel = null!;
    private Label _listenersLabel = null!;
    private Label _timerLabel = null!;
    private Label _moneyLabel = null!;

    private ICallerRepository _repository = null!;
    private ListenerManager _listenerManager = null!;
    private EconomyManager _economyManager = null!;
    private TimeManager _timeManager = null!;

    public override void _Notification(int what) => this.Notify(what);

    private bool _previousIsOnAir;
    private int _previousListeners;
    private string _previousTimerText;
    private int _previousMoney;

        public override void _Ready()
        {
            _onAirLabel = GetNode<Label>("HBoxContainer/OnAirLabel");
            _listenersLabel = GetNode<Label>("HBoxContainer/ListenersLabel");
            _timerLabel = GetNode<Label>("HBoxContainer/TimerLabel");
            _moneyLabel = GetNode<Label>("HBoxContainer/MoneyLabel");

            // Dependencies resolved in OnResolved()
        }

        /// <summary>
        /// Called when all dependencies are resolved.
        /// </summary>
        public void OnResolved()
        {
            // Get dependencies via DI
            _repository = DependencyInjection.Get<ICallerRepository>(this);
            _listenerManager = DependencyInjection.Get<ListenerManager>(this);
            _economyManager = DependencyInjection.Get<EconomyManager>(this);
            _timeManager = DependencyInjection.Get<TimeManager>(this);

            TrackStateForRefresh();
            UpdateOnAirStatus();
            UpdateListeners();
            UpdateMoney();
        }

        private void TrackStateForRefresh()
        {
            _previousIsOnAir = _repository.IsOnAir;
            _previousListeners = _listenerManager?.CurrentListeners ?? 0;
            _previousTimerText = _timeManager?.RemainingTimeFormatted ?? "--:--";
            _previousMoney = _economyManager?.CurrentMoney ?? 0;
        }

        public override void _Process(double delta)
        {
            if (_repository == null) return;

            var isOnAir = _repository.IsOnAir;
            var listeners = _listenerManager?.CurrentListeners ?? 0;
            var timerText = _timeManager?.RemainingTimeFormatted ?? "--:--";
            var money = _economyManager?.CurrentMoney ?? 0;

            if (isOnAir != _previousIsOnAir ||
                listeners != _previousListeners ||
                timerText != _previousTimerText ||
                money != _previousMoney)
            {
                UpdateOnAirStatus();
                UpdateListeners();
                UpdateTimer();
                UpdateMoney();

                _previousIsOnAir = isOnAir;
                _previousListeners = listeners;
                _previousTimerText = timerText;
                _previousMoney = money;
            }
        }

        private void UpdateOnAirStatus()
        {
            if (_onAirLabel != null)
            {
                bool isOnAir = _repository.IsOnAir;
                _onAirLabel.Text = "ON-AIR";
                _onAirLabel.AddThemeColorOverride("font_color", isOnAir ? Colors.Green : Colors.Red);
            }
        }

        private void UpdateListeners()
        {
            if (_listenerManager != null && _listenersLabel != null)
            {
                _listenersLabel.Text = $"Listeners: {_listenerManager.GetFormattedListeners()}";
            }
        }

        private void UpdateTimer()
        {
            if (_timeManager != null && _timerLabel != null)
            {
                var remainingText = _timeManager.RemainingTimeFormatted;
                _timerLabel.Text = remainingText;

                // Color coding based on time remaining
                var remainingSeconds = _timeManager.RemainingTime;
                if (remainingSeconds <= 30f)
                {
                    _timerLabel.AddThemeColorOverride("font_color", Colors.Red);
                }
                else if (remainingSeconds <= 60f)
                {
                    _timerLabel.AddThemeColorOverride("font_color", Colors.Yellow);
                }
                else
                {
                    _timerLabel.AddThemeColorOverride("font_color", Colors.White);
                }
            }
        }

        private void UpdateMoney()
        {
            if (_economyManager != null && _moneyLabel != null)
            {
                _moneyLabel.Text = $"Money: ${_economyManager.CurrentMoney}";
            }
        }

        public override void _ExitTree()
        {
        }
    }
}
