using Godot;
using KBTV.Core;
using KBTV.Managers;
using KBTV.Economy;
using KBTV.UI;

namespace KBTV.UI
{
    /// <summary>
    /// Manages UI display updates and synchronization with game state.
    /// Handles clock, listeners, money, live indicators, and event-driven updates.
    /// </summary>
    public partial class DisplayManager
    {
        private readonly UIManagerBootstrap _uiManager;

        // Cached references to UI elements
        private Label _clockText;
        private Label _listenerCount;
        private Label _moneyText;
        private Control _liveIndicator;
        private LiveShowHeader _liveShowHeader;

        // Cached references to managers
        private GameStateManager _gameState;
        private TimeManager _timeManager;
        private ListenerManager _listenerManager;
        private EconomyManager _economyManager;

        public DisplayManager(UIManagerBootstrap uiManager)
        {
            _uiManager = uiManager;
            InitializeReferences();
        }

        private void InitializeReferences()
        {
            // Get UI element references from UIManagerBootstrap
            _clockText = _uiManager.GetClockText();
            _listenerCount = _uiManager.GetListenerCount();
            _moneyText = _uiManager.GetMoneyText();
            _liveIndicator = _uiManager.GetLiveIndicator();
            _liveShowHeader = _uiManager.GetLiveShowHeader();

            // Get manager references
            _gameState = GameStateManager.Instance;
            _timeManager = TimeManager.Instance;
            _listenerManager = ListenerManager.Instance;
            _economyManager = EconomyManager.Instance;
        }

        /// <summary>
        /// Refresh all display elements.
        /// </summary>
        public void RefreshAllDisplays()
        {
            UpdateClockDisplay();
            UpdateListenerDisplay();
            UpdateMoneyDisplay();
            UpdateLiveIndicator();
        }

        /// <summary>
        /// Update the clock display.
        /// </summary>
        public void UpdateClockDisplay()
        {
            if (_liveShowHeader != null)
            {
                _liveShowHeader.RefreshClock();
            }
            else if (_clockText != null)
            {
                // Fallback
                var currentTime = _timeManager?.CurrentTimeFormatted ?? "12:00 AM";
                var remaining = _timeManager?.RemainingTimeFormatted ?? "45:00";
                _clockText.Text = $"{currentTime} ({remaining})";
            }
        }

        /// <summary>
        /// Update the listener display.
        /// </summary>
        public void UpdateListenerDisplay()
        {
            if (_liveShowHeader != null)
            {
                _liveShowHeader.RefreshListeners();
            }
            else if (_listenerCount != null && _listenerManager != null)
            {
                // Fallback
                var count = _listenerManager.GetFormattedListeners();
                var change = _listenerManager.GetFormattedChange();
                var changeValue = _listenerManager.ListenerChange;
                var color = changeValue >= 0 ? new Color(0f, 0.8f, 0f) : new Color(0.8f, 0.2f, 0.2f);

                _listenerCount.Text = $"{count} ({change})";
                _listenerCount.AddThemeColorOverride("font_color", color);
            }
        }

        /// <summary>
        /// Update the money display.
        /// </summary>
        public void UpdateMoneyDisplay()
        {
            if (_moneyText == null || _economyManager == null) return;

            _moneyText.Text = $"${_economyManager.CurrentMoney}";
        }

        /// <summary>
        /// Update the live indicator based on game phase.
        /// </summary>
        public void UpdateLiveIndicator()
        {
            if (_liveIndicator != null)
            {
                bool isLive = _gameState?.CurrentPhase == GamePhase.LiveShow;
                _liveIndicator.Visible = isLive;
            }
        }

        /// <summary>
        /// Handle time tick updates.
        /// </summary>
        public void HandleTimeTick(float deltaTime)
        {
            UpdateClockDisplay();
        }

        /// <summary>
        /// Handle listener count changes.
        /// </summary>
        public void HandleListenersChanged(int oldCount, int newCount)
        {
            UpdateListenerDisplay();
        }

        /// <summary>
        /// Handle money changes.
        /// </summary>
        public void HandleMoneyChanged(int oldAmount, int newAmount)
        {
            UpdateMoneyDisplay();
        }

        /// <summary>
        /// Handle game phase changes.
        /// </summary>
        public void HandlePhaseChanged(GamePhase oldPhase, GamePhase newPhase)
        {
            RefreshAllDisplays();
        }

        /// <summary>
        /// Handle stats changes.
        /// </summary>
        public void HandleStatsChanged()
        {
            // Refresh STATS tab if it's currently visible
            _uiManager.RefreshCurrentTab();
        }
    }
}