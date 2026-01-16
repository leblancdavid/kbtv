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

            var events = Core.ServiceRegistry.Instance.EventAggregator;
            events?.Subscribe(this, (Core.Events.OnAir.CallerOnAir evt) => OnCallerOnAir(evt));
            events?.Subscribe(this, (Core.Events.OnAir.CallerOnAirEnded evt) => OnCallerCompleted(evt));

            if (_listenerManager != null)
            {
                _listenerManager.Connect("ListenersChanged", Callable.From<int, int>(OnListenersChanged));
            }

            if (_economyManager != null)
            {
                _economyManager.Connect("MoneyChanged", Callable.From<int, int>(OnMoneyChanged));
            }

            UpdateOnAirStatus();
            UpdateListeners();
            UpdateMoney();
        }

        private void OnCallerOnAir(Core.Events.OnAir.CallerOnAir evt)
        {
            UpdateOnAirStatus();
        }

        private void OnCallerCompleted(Core.Events.OnAir.CallerOnAirEnded evt)
        {
            UpdateOnAirStatus();
        }

        private void OnListenersChanged(int oldCount, int newCount)
        {
            UpdateListeners();
        }

        private void OnMoneyChanged(int oldAmount, int newAmount)
        {
            UpdateMoney();
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
            var events = Core.ServiceRegistry.Instance?.EventAggregator;
            events?.Unsubscribe(this);

            if (_listenerManager != null)
            {
                _listenerManager.Disconnect("ListenersChanged", Callable.From<int, int>(OnListenersChanged));
            }

            if (_economyManager != null)
            {
                _economyManager.Disconnect("MoneyChanged", Callable.From<int, int>(OnMoneyChanged));
            }
        }
    }
}
