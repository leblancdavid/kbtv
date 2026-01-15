using Godot;
using KBTV.Callers;
using KBTV.Managers;
using KBTV.Economy;

namespace KBTV.UI
{
    /// <summary>
    /// Header component for the liveshow UI.
    /// Displays on-air status, listener count, and money balance.
    /// </summary>
    public partial class LiveShowHeader : Control
    {
        private Label _onAirLabel;
        private Label _listenersLabel;
        private Label _moneyLabel;

        private CallerQueue _callerQueue;
        private ListenerManager _listenerManager;
        private EconomyManager _economyManager;

        public override void _Ready()
        {
            // Get UI references
            _onAirLabel = GetNode<Label>("HBoxContainer/OnAirLabel");
            _listenersLabel = GetNode<Label>("HBoxContainer/ListenersLabel");
            _moneyLabel = GetNode<Label>("HBoxContainer/MoneyLabel");

            // Get manager references
            _callerQueue = CallerQueue.Instance;
            _listenerManager = ListenerManager.Instance;
            _economyManager = EconomyManager.Instance;

            // Connect to signals
            if (_callerQueue != null)
            {
                _callerQueue.Connect("CallerOnAir", Callable.From<Caller>(OnCallerOnAir));
                _callerQueue.Connect("CallerCompleted", Callable.From<Caller>(OnCallerCompleted));
            }

            if (_listenerManager != null)
            {
                _listenerManager.Connect("ListenersChanged", Callable.From<int, int>(OnListenersChanged));
            }

            if (_economyManager != null)
            {
                _economyManager.Connect("MoneyChanged", Callable.From<int, int>(OnMoneyChanged));
            }

            // Initial updates
            UpdateOnAirStatus();
            UpdateListeners();
            UpdateMoney();
        }

        private void OnCallerOnAir(Caller caller)
        {
            UpdateOnAirStatus();
        }

        private void OnCallerCompleted(Caller caller)
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
            if (_callerQueue != null && _onAirLabel != null)
            {
                bool isOnAir = _callerQueue.IsOnAir;
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
            // Clean up signal connections
            if (_callerQueue != null)
            {
                _callerQueue.Disconnect("CallerOnAir", Callable.From<Caller>(OnCallerOnAir));
                _callerQueue.Disconnect("CallerCompleted", Callable.From<Caller>(OnCallerCompleted));
            }

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