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
    public partial class LiveShowHeader : Control
    {
        private Label _onAirLabel = null!;
        private Label _listenersLabel = null!;
        private Label _moneyLabel = null!;

        private ICallerRepository _repository = null!;
        private ListenerManager _listenerManager = null!;
        private EconomyManager _economyManager = null!;

        private bool _previousIsOnAir;
        private int _previousListeners;
        private int _previousMoney;

        public override void _Ready()
        {
            CallDeferred(nameof(InitializeDeferred));
        }

        private void InitializeDeferred()
        {
            if (!Core.ServiceRegistry.IsInitialized)
            {
                GD.PrintErr("LiveShowHeader: ServiceRegistry not initialized, retrying...");
                CallDeferred(nameof(InitializeDeferred));
                return;
            }

            _onAirLabel = GetNode<Label>("HBoxContainer/OnAirLabel");
            _listenersLabel = GetNode<Label>("HBoxContainer/ListenersLabel");
            _moneyLabel = GetNode<Label>("HBoxContainer/MoneyLabel");

            _repository = Core.ServiceRegistry.Instance.CallerRepository;
            _listenerManager = Core.ServiceRegistry.Instance.ListenerManager;
            _economyManager = Core.ServiceRegistry.Instance.EconomyManager;

            TrackStateForRefresh();
            UpdateOnAirStatus();
            UpdateListeners();
            UpdateMoney();
        }

        private void TrackStateForRefresh()
        {
            _previousIsOnAir = _repository.IsOnAir;
            _previousListeners = _listenerManager?.CurrentListeners ?? 0;
            _previousMoney = _economyManager?.CurrentMoney ?? 0;
        }

        public override void _Process(double delta)
        {
            if (_repository == null) return;

            var isOnAir = _repository.IsOnAir;
            var listeners = _listenerManager?.CurrentListeners ?? 0;
            var money = _economyManager?.CurrentMoney ?? 0;

            if (isOnAir != _previousIsOnAir ||
                listeners != _previousListeners ||
                money != _previousMoney)
            {
                UpdateOnAirStatus();
                UpdateListeners();
                UpdateMoney();

                _previousIsOnAir = isOnAir;
                _previousListeners = listeners;
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

        private void UpdateMoney()
        {
            if (_economyManager != null && _moneyLabel != null)
            {
                _moneyLabel.Text = $"Money: ${_economyManager.CurrentMoney}";
            }
        }

        public override void _ExitTree()
        {
            GD.Print("LiveShowHeader: Cleanup complete");
        }
    }
}
