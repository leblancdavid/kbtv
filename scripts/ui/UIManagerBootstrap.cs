using System;
using System.Collections.Generic;
using Godot;
using KBTV.Core;
using KBTV.Managers;
using KBTV.Callers;
using KBTV.Dialogue;
using KBTV.Ads;
using KBTV.Audio;
using KBTV.Economy;
using KBTV.Data;
using KBTV.UI.Controllers;

namespace KBTV.UI
{
    public partial class UIManagerBootstrap : SingletonNode<UIManagerBootstrap>
    {
        private TabController _tabController;

        // Canvas and managers
        private VBoxContainer _rootContainer;
        private Control _mainContent;
        private CallerQueue _callerQueue;
        private EconomyManager _economyManager;
        private ListenerManager _listenerManager;
        private GameStateManager _gameState;
        private TimeManager _timeManager;

        // UI element references
        private Label _clockText;
        private Label _remainingText;
        private Label _listenerCount;
        private Label _listenerChange;
        private Control _liveIndicator;
        private Label _moneyText;
        private Label energyLabel;
        private Label spiritLabel;
        private Label patienceLabel;

        protected override void OnSingletonReady()
        {
            GD.Print("UIManagerBootstrap.OnSingletonReady called");

            // Create our own CanvasLayer and container programmatically
            var canvasLayer = new CanvasLayer();
            canvasLayer.Name = "LiveShowCanvasLayer";
            canvasLayer.Layer = 5; // Lower layer for LiveShow
            AddChild(canvasLayer);

            // Register with UIManager
            var uiManager = GetNode<UIManager>("/root/Main/UIManager");
            if (uiManager != null)
            {
                uiManager.RegisterLiveShowLayer(canvasLayer);
            }
            else
            {
                GD.PrintErr("UIManagerBootstrap: Could not find UIManager node");
            }

            _rootContainer = new VBoxContainer();
            _rootContainer.Name = "LiveShowContainer";
            _rootContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _rootContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _rootContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            canvasLayer.AddChild(_rootContainer);

            GD.Print("UIManagerBootstrap: Created CanvasLayer and container programmatically");

            // Create tab controller FIRST, before UI creation
            var tabs = new List<TabDefinition> {
                new TabDefinition {
                    Name = "CALLERS",
                    PopulateContent = PopulateCallersContent
                },
                new TabDefinition {
                    Name = "ITEMS",
                    PopulateContent = PopulateItemsContent
                },
                new TabDefinition {
                    Name = "STATS",
                    PopulateContent = PopulateStatsContent
                }
            };

            _tabController = new TabController(tabs, this);
            AddChild(_tabController);
            GD.Print("Tab controller created in OnSingletonReady");

            GD.Print("UIManagerBootstrap: Creating UI within container...");
            CreateUI();
            GD.Print("UIManagerBootstrap: UI creation complete");
        }

        public override void _Ready()
        {
            // Call base._Ready() first to initialize singleton and call OnSingletonReady()
            base._Ready();

            GD.Print($"UIManagerBootstrap._Ready called. _tabController: {_tabController}");

            // Get manager references (UI creation happens in OnSingletonReady)
            _gameState = GameStateManager.Instance;
            _timeManager = TimeManager.Instance;
            _listenerManager = ListenerManager.Instance;
            _economyManager = EconomyManager.Instance;
            _callerQueue = CallerQueue.Instance;

            SubscribeToEvents();
            RefreshAllDisplays();

            // Pre-populate STATS content will be called after tab controller initialization
        }

        public override void _ExitTree()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeTabController()
        {
            GD.Print($"InitializeTabController called. _tabController: {_tabController}, _mainContent: {_mainContent}");
            if (_tabController != null && _mainContent != null)
            {
                // TabController will create its own TabSection as a child of _mainContent
                _tabController.Initialize(_mainContent);
                GD.Print("Tab controller initialized successfully");

                // Now that tab controller is initialized, refresh the stats tab
                TryRefreshStatsTab();
            }
            else
            {
                GD.PrintErr($"_tabController or _mainContent is null during initialization. _tabController: {_tabController}, _mainContent: {_mainContent}");
            }
        }

        private void TryRefreshStatsTab()
        {
            GD.Print($"TryRefreshStatsTab called. _tabController: {_tabController}");
            if (_tabController != null)
            {
                _tabController.RefreshTabContent(2);
                GD.Print("Stats tab pre-populated successfully");
            }
            else
            {
                GD.PrintErr("Tab controller still null in TryRefreshStatsTab");
                // Debug: Check if tab controller was ever created
                GD.Print("Checking all children of UIManager for TabController...");
                foreach (var child in GetChildren())
                {
                    GD.Print($"  Child: {child.Name} ({child.GetType().Name})");
                }
            }
        }

        private void CreateUI()
        {
            try
            {
                GD.Print("UIManagerBootstrap: CreateUI started");

                CreateFullLayoutUI();
                GD.Print("UIManagerBootstrap: CreateUI completed successfully");
            }
            catch (Exception e)
            {
                GD.PrintErr($"UIManagerBootstrap: CreateUI failed with exception: {e.Message}");
                throw; // Re-throw to prevent silent failures
            }
        }



        private void CreateFullLayoutUI()
        {
            try
            {
                GD.Print("UIManagerBootstrap: CreateFullLayoutUI started");

                // Create and add UI components to the root container
                var headerBar = CreateHeaderBar();
                var mainContent = CreateMainContent();
                var footer = CreateFooter();

                _rootContainer.AddChild(headerBar);
                _rootContainer.AddChild(mainContent);
                _rootContainer.AddChild(footer);

                // Initialize tab controller immediately now that UI is created
                InitializeTabController();

                GD.Print("UIManagerBootstrap: Full UI created successfully");
            }
            catch (Exception e)
            {
                GD.PrintErr($"UIManagerBootstrap: CreateFullLayoutUI failed with exception: {e.Message}");
                throw; // Re-throw to prevent silent failures
            }
        }



        private Control CreateMainContent()
        {
            var mainContent = new Control();
            mainContent.Name = "MainContent";
            // Fill available space - containers handle the layout
            mainContent.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            mainContent.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

            // Store reference for tab controller initialization
            _mainContent = mainContent;
            return mainContent;
        }

        private Control CreateHeaderBar()
        {
            var headerContainer = new HBoxContainer();
            headerContainer.Name = "HeaderBar";
            headerContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            headerContainer.CustomMinimumSize = new Vector2(0, UITheme.HEADER_HEIGHT);

            var headerPanel = new Panel();
            UITheme.ApplyPanelStyle(headerPanel);
            headerPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            headerContainer.AddChild(headerPanel);

            // Add flexible spacers between elements to distribute evenly
            var spacer1 = UIHelpers.CreateSpacer();
            headerContainer.AddChild(spacer1);

            CreateLiveIndicator(headerContainer);

            var spacer2 = UIHelpers.CreateSpacer();
            headerContainer.AddChild(spacer2);

            CreateClockDisplay(headerContainer);

            var spacer3 = UIHelpers.CreateSpacer();
            headerContainer.AddChild(spacer3);

            CreateListenerDisplay(headerContainer);

            var spacer4 = UIHelpers.CreateSpacer();
            headerContainer.AddChild(spacer4);

            return headerContainer;
        }

        private void CreateLiveIndicator(Control parent)
        {
            var liveContainer = new HBoxContainer();
            liveContainer.Name = "LiveContainer";
            liveContainer.CustomMinimumSize = new Vector2(60, 24);

            // Red dot
            var dotPanel = new Panel();
            dotPanel.CustomMinimumSize = new Vector2(8, 8);
            var dotStyle = new StyleBoxFlat();
            dotStyle.BgColor = new Color(0.8f, 0.2f, 0.2f);
            dotPanel.AddThemeStyleboxOverride("panel", dotStyle);
            liveContainer.AddChild(dotPanel);

            // "ON AIR" text
            var liveLabel = new Label();
            liveLabel.Text = "ON AIR";
            liveLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.2f, 0.2f));
            liveLabel.VerticalAlignment = VerticalAlignment.Center;
            liveContainer.AddChild(liveLabel);

            _liveIndicator = liveContainer;
            parent.AddChild(liveContainer);
        }

        private void CreateClockDisplay(Control parent)
        {
            var clockContainer = new VBoxContainer();
            clockContainer.Name = "ClockDisplay";

            _clockText = new Label();
            _clockText.Text = "12:00 AM";
            _clockText.HorizontalAlignment = HorizontalAlignment.Center;
            clockContainer.AddChild(_clockText);

            _remainingText = new Label();
            _remainingText.Text = "45:00";
            _remainingText.HorizontalAlignment = HorizontalAlignment.Center;
            _remainingText.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
            clockContainer.AddChild(_remainingText);

            parent.AddChild(clockContainer);
        }

        private void CreateListenerDisplay(Control parent)
        {
            var listenerContainer = new VBoxContainer();
            listenerContainer.Name = "ListenerDisplay";

            _listenerCount = new Label();
            _listenerCount.Text = "1,234";
            _listenerCount.HorizontalAlignment = HorizontalAlignment.Center;
            listenerContainer.AddChild(_listenerCount);

            _listenerChange = new Label();
            _listenerChange.Text = "+123";
            _listenerChange.HorizontalAlignment = HorizontalAlignment.Center;
            _listenerChange.AddThemeColorOverride("font_color", new Color(0f, 0.8f, 0f));
            listenerContainer.AddChild(_listenerChange);

            parent.AddChild(listenerContainer);
        }

        private Control CreateFooter()
        {
            var footerContainer = new HBoxContainer();
            footerContainer.Name = "Footer";
            footerContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            footerContainer.CustomMinimumSize = new Vector2(0, UITheme.FOOTER_HEIGHT);

            var footerPanel = new Panel();
            UITheme.ApplyPanelStyle(footerPanel, false); // Dark background
            footerPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            footerContainer.AddChild(footerPanel);

            // Add flexible spacers between panels to distribute evenly
            var spacer1 = UIHelpers.CreateSpacer();
            footerContainer.AddChild(spacer1);

            CreateOnAirPanel(footerContainer);

            var spacer2 = UIHelpers.CreateSpacer();
            footerContainer.AddChild(spacer2);

            CreateTranscriptPanel(footerContainer);

            var spacer3 = UIHelpers.CreateSpacer();
            footerContainer.AddChild(spacer3);

            CreateAdBreakPanel(footerContainer);

            var spacer4 = UIHelpers.CreateSpacer();
            footerContainer.AddChild(spacer4);

            CreateMoneyDisplay(footerContainer);

            var spacer5 = UIHelpers.CreateSpacer();
            footerContainer.AddChild(spacer5);

            return footerContainer;
        }

        private void CreateOnAirPanel(Control parent)
        {
            var onAirPanel = new Panel();
            onAirPanel.Name = "OnAirPanel";
            onAirPanel.CustomMinimumSize = new Vector2(180, 120);

            var onAirStyle = new StyleBoxFlat();
            onAirStyle.BgColor = new Color(0.12f, 0.12f, 0.12f);
            onAirPanel.AddThemeStyleboxOverride("panel", onAirStyle);

            // Header
            var headerLabel = new Label();
            headerLabel.Name = "Header";
            headerLabel.Text = "ON AIR";
            headerLabel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
            headerLabel.Size = new Vector2(180, 18);
            headerLabel.HorizontalAlignment = HorizontalAlignment.Center;
            headerLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.2f, 0.2f));
            onAirPanel.AddChild(headerLabel);

            // Caller name
            var callerLabel = new Label();
            callerLabel.Name = "CallerName";
            callerLabel.Text = "No caller";
            callerLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
            callerLabel.Size = new Vector2(172, 40); // Account for padding
            callerLabel.HorizontalAlignment = HorizontalAlignment.Center;
            callerLabel.VerticalAlignment = VerticalAlignment.Center;
            callerLabel.AddThemeColorOverride("font_color", new Color(1f, 0.7f, 0f));
            onAirPanel.AddChild(callerLabel);

            // TODO: Add End Call button when button system is implemented

            parent.AddChild(onAirPanel);
        }

        private void CreateTranscriptPanel(Control parent)
        {
            var transcriptPanel = new Panel();
            transcriptPanel.Name = "TranscriptPanel";
            transcriptPanel.CustomMinimumSize = new Vector2(300, 120);

            var transcriptStyle = new StyleBoxFlat();
            transcriptStyle.BgColor = new Color(0.12f, 0.12f, 0.12f);
            transcriptPanel.AddThemeStyleboxOverride("panel", transcriptStyle);

            // Header
            var headerLabel = new Label();
            headerLabel.Name = "Header";
            headerLabel.Text = "TRANSCRIPT";
            headerLabel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
            headerLabel.Size = new Vector2(300, 18);
            headerLabel.HorizontalAlignment = HorizontalAlignment.Center;
            headerLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
            transcriptPanel.AddChild(headerLabel);

            // TODO: Add transcript scroll area when scrolling is implemented

            parent.AddChild(transcriptPanel);
        }

        private void CreateAdBreakPanel(Control parent)
        {
            var adBreakPanel = new Panel();
            adBreakPanel.Name = "AdBreakPanel";
            adBreakPanel.CustomMinimumSize = new Vector2(180, 120);

            var adBreakStyle = new StyleBoxFlat();
            adBreakStyle.BgColor = new Color(0.12f, 0.12f, 0.12f);
            adBreakPanel.AddThemeStyleboxOverride("panel", adBreakStyle);

            // Header
            var headerLabel = new Label();
            headerLabel.Name = "Header";
            headerLabel.Text = "AD BREAK";
            headerLabel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
            headerLabel.Size = new Vector2(180, 18);
            headerLabel.HorizontalAlignment = HorizontalAlignment.Center;
            headerLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
            adBreakPanel.AddChild(headerLabel);

            // TODO: Add ad break controls when button system is implemented

            parent.AddChild(adBreakPanel);
        }

        private void CreateMoneyDisplay(Control parent)
        {
            var moneyPanel = new Panel();
            moneyPanel.Name = "MoneyPanel";
            moneyPanel.CustomMinimumSize = new Vector2(120, 120);

            var moneyStyle = new StyleBoxFlat();
            moneyStyle.BgColor = new Color(0.12f, 0.12f, 0.12f);
            moneyPanel.AddThemeStyleboxOverride("panel", moneyStyle);

            // Header
            var headerLabel = new Label();
            headerLabel.Name = "Header";
            headerLabel.Text = "MONEY";
            headerLabel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
            headerLabel.Size = new Vector2(120, 18);
            headerLabel.HorizontalAlignment = HorizontalAlignment.Center;
            headerLabel.AddThemeColorOverride("font_color", new Color(0f, 0.8f, 0f));
            moneyPanel.AddChild(headerLabel);

            // Money amount
            _moneyText = new Label();
            _moneyText.Name = "MoneyAmount";
            _moneyText.Text = "$500";
            _moneyText.SetAnchorsPreset(Control.LayoutPreset.Center);
            _moneyText.Size = new Vector2(112, 40);
            _moneyText.HorizontalAlignment = HorizontalAlignment.Center;
            _moneyText.VerticalAlignment = VerticalAlignment.Center;
            _moneyText.AddThemeColorOverride("font_color", new Color(0f, 0.8f, 0f));
            moneyPanel.AddChild(_moneyText);

            parent.AddChild(moneyPanel);
        }

        // Content population methods for tabs
        private void PopulateCallersContent(Control contentArea)
        {
            if (_callerQueue == null)
            {
                var errorLabel = new Label();
                errorLabel.Text = "CallerQueue not available";
                contentArea.AddChild(errorLabel);
                return;
            }

            var container = new VBoxContainer();
            container.Name = "CallersContainer";

            // Incoming callers
            if (_callerQueue.HasIncomingCallers)
            {
                var incomingHeader = new Label();
                incomingHeader.Text = "INCOMING CALLERS:";
                incomingHeader.AddThemeColorOverride("font_color", new Color(1f, 0.7f, 0f));
                container.AddChild(incomingHeader);

                foreach (var caller in _callerQueue.IncomingCallers)
                {
                    var callerLabel = new Label();
                    callerLabel.Text = $"{caller.Name} - {caller.Location}";
                    callerLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
                    container.AddChild(callerLabel);
                }
            }

            // Current screening
            if (_callerQueue.IsScreening)
            {
                var screeningHeader = new Label();
                screeningHeader.Text = "\nCURRENTLY SCREENING:";
                screeningHeader.AddThemeColorOverride("font_color", new Color(0f, 1f, 0f));
                container.AddChild(screeningHeader);

                var screeningLabel = new Label();
                screeningLabel.Text = $"{_callerQueue.CurrentScreening.Name} - {_callerQueue.CurrentScreening.Location}";
                container.AddChild(screeningLabel);
            }

            // On hold callers
            if (_callerQueue.HasOnHoldCallers)
            {
                var holdHeader = new Label();
                holdHeader.Text = "\nON HOLD:";
                holdHeader.AddThemeColorOverride("font_color", new Color(0f, 0.7f, 1f));
                container.AddChild(holdHeader);

                foreach (var caller in _callerQueue.OnHoldCallers)
                {
                    var callerLabel = new Label();
                    callerLabel.Text = $"{caller.Name} - {caller.Location}";
                    callerLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
                    container.AddChild(callerLabel);
                }
            }

            // On air caller
            if (_callerQueue.IsOnAir)
            {
                var airHeader = new Label();
                airHeader.Text = "\nON AIR:";
                airHeader.AddThemeColorOverride("font_color", new Color(1f, 0.2f, 0.2f));
                container.AddChild(airHeader);

                var airLabel = new Label();
                airLabel.Text = $"{_callerQueue.OnAirCaller.Name} - {_callerQueue.OnAirCaller.Location}";
                container.AddChild(airLabel);
            }

            // Empty state
            if (!_callerQueue.HasIncomingCallers && !_callerQueue.IsScreening && !_callerQueue.HasOnHoldCallers && !_callerQueue.IsOnAir)
            {
                var emptyLabel = new Label();
                emptyLabel.Text = "No callers waiting";
                emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
                container.AddChild(emptyLabel);
            }

            contentArea.AddChild(container);
        }

        private void PopulateItemsContent(Control contentArea)
        {
            // TODO: Implement when ItemManager is ported
            var placeholderLabel = new Label();
            placeholderLabel.Text = "ITEMS TAB\n(Not yet implemented)";
            placeholderLabel.HorizontalAlignment = HorizontalAlignment.Center;
            placeholderLabel.VerticalAlignment = VerticalAlignment.Center;
            contentArea.AddChild(placeholderLabel);
        }

        private void PopulateStatsContent(Control contentArea)
        {
            var vernStats = GameStateManager.Instance?.VernStats;
            if (vernStats == null)
            {
                var errorLabel = new Label();
                errorLabel.Text = "VernStats not available";
                contentArea.AddChild(errorLabel);
                return;
            }

            var statsContainer = new VBoxContainer();
            statsContainer.Name = "StatsContainer";

            // VIBE
            var vibeLabel = new Label();
            vibeLabel.Text = $"VIBE: {vernStats.CalculateVIBE():F1}";
            statsContainer.AddChild(vibeLabel);

            // ENERGY
            var energyLabel = new Label();
            energyLabel.Text = $"ENERGY: {vernStats.Energy.Value:F0}";
            statsContainer.AddChild(energyLabel);

            // SPIRIT
            var spiritLabel = new Label();
            spiritLabel.Text = $"SPIRIT: {vernStats.Spirit.Value:F1}";
            statsContainer.AddChild(spiritLabel);

            // PATIENCE
            var patienceLabel = new Label();
            patienceLabel.Text = $"PATIENCE: {vernStats.Patience.Value:F0}";
            statsContainer.AddChild(patienceLabel);

            // Money
            var moneyLabel = new Label();
            moneyLabel.Text = $"MONEY: ${(_economyManager?.CurrentMoney ?? 0)}";
            statsContainer.AddChild(moneyLabel);

            // Listeners
            var listenersLabel = new Label();
            listenersLabel.Text = $"LISTENERS: {_listenerManager?.CurrentListeners ?? 0}";
            statsContainer.AddChild(listenersLabel);

            contentArea.AddChild(statsContainer);
        }

        private void SubscribeToEvents()
        {
            if (_gameState != null)
            {
                _gameState.OnPhaseChanged += HandlePhaseChanged;
            }

            if (_timeManager != null)
            {
                _timeManager.OnTick += HandleTimeTick;
            }

            if (_listenerManager != null)
            {
                _listenerManager.OnListenersChanged += HandleListenersChanged;
            }

            if (_economyManager != null)
            {
                _economyManager.OnMoneyChanged += HandleMoneyChanged;
            }

            var vernStats = GameStateManager.Instance?.VernStats;
            if (vernStats != null)
            {
                vernStats.OnStatsChanged += HandleStatsChanged;
            }

            if (_callerQueue != null)
            {
                _callerQueue.OnCallerAdded += HandleCallerQueueChanged;
                _callerQueue.OnCallerRemoved += HandleCallerQueueChanged;
                _callerQueue.OnCallerOnAir += HandleCallerQueueChanged;
                _callerQueue.OnCallerCompleted += HandleCallerQueueChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_gameState != null)
            {
                _gameState.OnPhaseChanged -= HandlePhaseChanged;
            }

            if (_timeManager != null)
            {
                _timeManager.OnTick -= HandleTimeTick;
            }

            if (_listenerManager != null)
            {
                _listenerManager.OnListenersChanged -= HandleListenersChanged;
            }

            if (_economyManager != null)
            {
                _economyManager.OnMoneyChanged -= HandleMoneyChanged;
            }

            var vernStats = GameStateManager.Instance?.VernStats;
            if (vernStats != null)
            {
                vernStats.OnStatsChanged -= HandleStatsChanged;
            }

            if (_callerQueue != null)
            {
                _callerQueue.OnCallerAdded -= HandleCallerQueueChanged;
                _callerQueue.OnCallerRemoved -= HandleCallerQueueChanged;
                _callerQueue.OnCallerOnAir -= HandleCallerQueueChanged;
                _callerQueue.OnCallerCompleted -= HandleCallerQueueChanged;
            }
        }

        private void HandlePhaseChanged(GamePhase oldPhase, GamePhase newPhase)
        {
            RefreshAllDisplays();
        }

        private void HandleTimeTick(float deltaTime)
        {
            UpdateClockDisplay();
        }

        private void HandleListenersChanged(int oldCount, int newCount)
        {
            UpdateListenerDisplay();
        }

        private void HandleMoneyChanged(int oldAmount, int newAmount)
        {
            UpdateMoneyDisplay();
        }

        private void HandleStatsChanged()
        {
            // Refresh STATS tab if it's currently visible
            _tabController.RefreshCurrentTab();
        }

        private void HandleCallerQueueChanged(Caller caller = null)
        {
            // Refresh CALLERS tab if it's currently visible
            _tabController.RefreshTabContent(0); // CALLERS is index 0
        }

        private void RefreshAllDisplays()
        {
            UpdateClockDisplay();
            UpdateListenerDisplay();
            UpdateMoneyDisplay();
            UpdateLiveIndicator();
        }

        private void UpdateClockDisplay()
        {
            if (_clockText == null || _remainingText == null) return;

            // TODO: Implement proper time display
            _clockText.Text = "12:00 AM";
            _remainingText.Text = "45:00";
        }

        private void UpdateListenerDisplay()
        {
            if (_listenerCount == null || _listenerChange == null || _listenerManager == null) return;

            _listenerCount.Text = _listenerManager.GetFormattedListeners();
            _listenerChange.Text = _listenerManager.GetFormattedChange();

            // Color based on change
            var change = _listenerManager.ListenerChange;
            var color = change >= 0 ? new Color(0f, 0.8f, 0f) : new Color(0.8f, 0.2f, 0.2f);
            _listenerChange.AddThemeColorOverride("font_color", color);
        }

        private void UpdateMoneyDisplay()
        {
            if (_moneyText == null || _economyManager == null) return;

            _moneyText.Text = $"${_economyManager.CurrentMoney}";
        }

        private void UpdateLiveIndicator()
        {
            // Update live indicator based on game phase
            if (_liveIndicator != null)
            {
                bool isLive = GameStateManager.Instance?.CurrentPhase == GamePhase.LiveShow;
                _liveIndicator.Visible = isLive;
            }
        }
    }
}