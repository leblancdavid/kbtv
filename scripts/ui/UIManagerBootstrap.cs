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
	public partial class UIManagerBootstrap : Node
	{
		public static UIManagerBootstrap Instance => (UIManagerBootstrap)((SceneTree)Engine.GetMainLoop()).Root.GetNode("/root/UIManagerBootstrap");
		private TabContainer _tabContainer;
		private List<TabDefinition> _tabs;

		// Canvas and managers
		private VBoxContainer _rootContainer;
		private Control _mainContent;
		private LiveShowHeader _liveShowHeader;
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



        public override void _Ready()
        {
            GD.Print("UIManagerBootstrap._Ready called");
            base._Ready();

            // Create our own CanvasLayer and container programmatically
            var canvasLayer = new CanvasLayer();
            canvasLayer.Name = "LiveShowCanvasLayer";
            canvasLayer.Layer = 5; // Lower layer for LiveShow
            AddChild(canvasLayer);
            GD.Print("UIManagerBootstrap: CanvasLayer created");

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

            // Create tabs list
            _tabs = new List<TabDefinition>
            {
                new TabDefinition { Name = "CALLERS", PopulateContent = PopulateCallersContent },
                new TabDefinition { Name = "ITEMS", PopulateContent = PopulateItemsContent },
                new TabDefinition { Name = "STATS", PopulateContent = PopulateStatsContent }
            };

            CreateUI();

            // Get manager references
            _gameState = GameStateManager.Instance;
            _timeManager = TimeManager.Instance;
            _listenerManager = ListenerManager.Instance;
            _callerQueue = CallerQueue.Instance;

            SubscribeToEvents();
            RefreshAllDisplays();
            GD.Print("UIManagerBootstrap._Ready completed");

            // Pre-populate STATS content will be called after tab controller initialization
        }

		public override void _ExitTree()
		{
			UnsubscribeFromEvents();
		}

		private void InitializeTabController()
		{
			GD.Print($"InitializeTabController called. _tabs: {_tabs?.Count}, _mainContent: {_mainContent}, _tabContainer: {_tabContainer}");
			if (_tabs != null && _mainContent != null && _tabContainer != null)
			{
				// TabContainer already loaded from scene, populate with tabs
				for (int i = 0; i < _tabs.Count; i++)
				{
					var tabContent = CreateTabContent(_tabs[i]);
					_tabContainer.AddChild(tabContent);
					_tabContainer.SetTabTitle(i, _tabs[i].Name);
				}

				GD.Print("TabContainer initialized successfully");

				// Refresh initial tab
				RefreshCurrentTab();
			}
			else
			{
				GD.PrintErr($"InitializeTabController failed: _tabs null: {_tabs == null}, _mainContent null: {_mainContent == null}, _tabContainer null: {_tabContainer == null}");
			}
		}

		private Control CreateTabContent(TabDefinition tab)
		{
			var scrollContainer = new ScrollContainer();
			scrollContainer.Name = $"{tab.Name}Scroll";
			scrollContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			scrollContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			scrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

			var contentArea = new Control();
			contentArea.Name = "Content";
			contentArea.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			contentArea.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

			scrollContainer.AddChild(contentArea);

			// Populate content
			tab.PopulateContent?.Invoke(contentArea);

			return scrollContainer;
		}

		private void TryRefreshStatsTab()
		{
			// GD.Print("TryRefreshStatsTab called");
			if (_tabContainer != null)
			{
				RefreshTabContent(2);
				// GD.Print("Stats tab refreshed successfully");
			}
			else
			{
				GD.PrintErr("TabContainer null in TryRefreshStatsTab");
			}
		}

		private void RefreshTabContent(int tabIndex)
		{
			if (_tabContainer == null || tabIndex < 0 || tabIndex >= _tabs.Count) return;

			var scrollContainer = _tabContainer.GetChild(tabIndex) as ScrollContainer;
			if (scrollContainer != null)
			{
				var contentArea = scrollContainer.GetChild(0) as Control;
				if (contentArea != null)
				{
					// Clear existing content
					foreach (var child in contentArea.GetChildren())
					{
						contentArea.RemoveChild(child);
						child.QueueFree();
					}

					// Repopulate
					_tabs[tabIndex].PopulateContent?.Invoke(contentArea);
				}
			}
		}

		private void RefreshCurrentTab()
		{
			if (_tabContainer != null)
			{
				RefreshTabContent(_tabContainer.CurrentTab);
			}
		}

        private void CreateUI()
        {
            try
            {
                // GD.Print("UIManagerBootstrap: CreateUI started");

                CreateFullLayoutUI();
                // GD.Print("UIManagerBootstrap: CreateUI completed successfully");
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
                // GD.Print("UIManagerBootstrap: CreateFullLayoutUI started");

                // Create and add UI components to the root container
                var headerBar = CreateHeaderBar();
                var mainContent = CreateMainContent();
                var footer = CreateFooter();

                _rootContainer.AddChild(headerBar);
                _rootContainer.AddChild(mainContent);
                _rootContainer.AddChild(footer);

                // Initialize tab controller immediately now that UI is created
                InitializeTabController();

                // GD.Print("UIManagerBootstrap: Full UI created successfully");
            }
            catch (Exception e)
            {
                GD.PrintErr($"UIManagerBootstrap: CreateFullLayoutUI failed with exception: {e.Message}");
                throw; // Re-throw to prevent silent failures
            }
        }



		private Control CreateMainContent()
		{
			// Load the scene-based tab container
			var tabContainerScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/TabContainerUI.tscn");
			if (tabContainerScene != null)
			{
				var mainContent = tabContainerScene.Instantiate<TabContainer>();
				mainContent.Name = "MainContent";
				// Store references
				_mainContent = mainContent;
				_tabContainer = mainContent;
				return mainContent;
			}
			else
			{
				// Fallback to programmatic creation
				GD.PrintErr("Failed to load TabContainerUI.tscn, using fallback");
				return CreateMainContentFallback();
			}
		}

		private Control CreateMainContentFallback()
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
			// Load the scene-based header
			var headerScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/LiveShowHeader.tscn");
			if (headerScene != null)
			{
				var header = headerScene.Instantiate<Control>();
				header.Name = "LiveShowHeader";
				header.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
				header.CustomMinimumSize = new Vector2(0, UITheme.HEADER_HEIGHT);

				// Store reference for updates
				_liveShowHeader = header as LiveShowHeader;

				return header;
			}
			else
			{
				// Fallback to programmatic creation
				GD.PrintErr("Failed to load LiveShowHeader.tscn, using fallback");
				return CreateHeaderBarFallback();
			}
		}

		private Control CreateHeaderBarFallback()
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
			liveLabel.HorizontalAlignment = HorizontalAlignment.Center;
			liveLabel.VerticalAlignment = VerticalAlignment.Center;
			liveLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.2f, 0.2f));
			liveContainer.AddChild(liveLabel);

			_liveIndicator = liveContainer;
			parent.AddChild(liveContainer);
		}

		private void CreateClockDisplay(Control parent)
		{
			_clockText = new Label();
			_clockText.Name = "ClockDisplay";
			_clockText.Text = "12:00 AM (45:00)";
			_clockText.HorizontalAlignment = HorizontalAlignment.Center;
			_clockText.VerticalAlignment = VerticalAlignment.Center;
			parent.AddChild(_clockText);
		}

		private void CreateListenerDisplay(Control parent)
		{
			_listenerCount = new Label();
			_listenerCount.Name = "ListenerDisplay";
			_listenerCount.Text = "1,234 (+123)";
			_listenerCount.HorizontalAlignment = HorizontalAlignment.Center;
			_listenerCount.VerticalAlignment = VerticalAlignment.Center;
			parent.AddChild(_listenerCount);
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
			GD.Print("PopulateCallersContent called");

			if (_callerQueue == null)
			{
				var errorLabel = new Label();
				errorLabel.Text = "CallerQueue not available";
				contentArea.AddChild(errorLabel);
				return;
			}

			var mainContainer = new HBoxContainer();
			mainContainer.Name = "CallersMainContainer";
			mainContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			mainContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

			// Left panel: Incoming callers (25% width)
			var incomingPanel = CreateCallerPanelScene("INCOMING CALLERS", _callerQueue.IncomingCallers, new Color(1f, 0.7f, 0f), new Color(0.8f, 0.8f, 0.8f));
			if (incomingPanel != null)
			{
				incomingPanel.SizeFlagsStretchRatio = 1; // 25%
				mainContainer.AddChild(incomingPanel);
			}

			// Middle panel: Screening controls (50% width)
			var screeningPanel = CreateScreeningPanelScene();
			screeningPanel.SizeFlagsStretchRatio = 2; // 50%
			mainContainer.AddChild(screeningPanel);

			// Right panel: On-hold callers (25% width)
			var onHoldPanel = CreateCallerPanelScene("ON HOLD", _callerQueue.OnHoldCallers, new Color(0f, 0.7f, 1f), new Color(0.6f, 0.6f, 0.6f));
			if (onHoldPanel != null)
			{
				onHoldPanel.SizeFlagsStretchRatio = 1; // 25%
				mainContainer.AddChild(onHoldPanel);
			}

			GD.Print($"Created mainContainer with {mainContainer.GetChildCount()} children");

			contentArea.AddChild(mainContainer);
		}

		private Control CreateCallerPanelScene(string headerText, System.Collections.Generic.IReadOnlyList<Caller> callers, Color headerColor, Color itemColor)
		{
			var scene = ResourceLoader.Load<PackedScene>("res://scenes/ui/CallerPanel.tscn");
			if (scene != null)
			{
				var panel = scene.Instantiate<CallerPanel>();
				if (panel != null)
				{
					panel.SetHeader(headerText, headerColor);
					panel.SetCallers(callers, itemColor);
					return panel;
				}
			}
			// Fallback to programmatic
			return CreateCallerPanelFallback(headerText, callers, headerColor, itemColor);
		}

		private Control CreateCallerPanelFallback(string headerText, System.Collections.Generic.IReadOnlyList<Caller> callers, Color headerColor, Color itemColor)
		{
			return CreateCallerPanel("FallbackPanel", headerText, callers, headerColor, itemColor);
		}

		private Control CreateCallerPanel(string name, string headerText, System.Collections.Generic.IReadOnlyList<Caller> callers, Color headerColor, Color itemColor)
		{
			GD.Print($"Creating caller panel {name} with {callers.Count} callers");

			var panel = new Panel();
			panel.Name = name;
			UITheme.ApplyPanelStyle(panel);
			panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

			var layout = new VBoxContainer();
			layout.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			layout.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			panel.AddChild(layout);

			// Header
			var header = new Label();
			header.Text = headerText;
			header.AddThemeColorOverride("font_color", headerColor);
			header.HorizontalAlignment = HorizontalAlignment.Center;
			layout.AddChild(header);

			// Scrollable list
			var scroll = new ScrollContainer();
			scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			layout.AddChild(scroll);

			var listContainer = new VBoxContainer();
			listContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			listContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			scroll.AddChild(listContainer);

			// Add caller items
			if (callers != null && callers.Count > 0)
			{
				foreach (var caller in callers)
				{
					var callerLabel = new Label();
					callerLabel.Text = $"{caller.Name} - {caller.Location}";
					callerLabel.AddThemeColorOverride("font_color", itemColor);
					listContainer.AddChild(callerLabel);
				}
			}
			else
			{
				var emptyLabel = new Label();
				emptyLabel.Text = "None";
				emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
				emptyLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
				listContainer.AddChild(emptyLabel);
			}

			return panel;
		}

		private Control CreateScreeningPanelScene()
		{
			// Use programmatic creation to avoid scene loading issues with UIDs
			return CreateScreeningPanelFallback();
		}

		private Control CreateScreeningPanelFallback()
		{
			return CreateScreeningPanel();
		}

		private Control CreateScreeningPanel()
		{
			var panel = new Panel();
			panel.Name = "ScreeningPanel";
			UITheme.ApplyPanelStyle(panel);
			panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

			var layout = new VBoxContainer();
			layout.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			layout.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			layout.AddThemeConstantOverride("separation", 10);
			panel.AddChild(layout);

			// Header
			var header = new Label();
			header.Text = "SCREENING";
			header.AddThemeColorOverride("font_color", new Color(0f, 1f, 0f));
			header.HorizontalAlignment = HorizontalAlignment.Center;
			layout.AddChild(header);

			// Current caller info
			if (_callerQueue.IsScreening)
			{
				var caller = _callerQueue.CurrentScreening;
				var callerLabel = new Label();
				callerLabel.Text = $"{caller.Name}\n{caller.Location}\nTopic: {caller.ClaimedTopic}";
				callerLabel.HorizontalAlignment = HorizontalAlignment.Center;
				callerLabel.AutowrapMode = TextServer.AutowrapMode.Word;
				layout.AddChild(callerLabel);
		}

			// Spacer
			var spacer = new Control();
			spacer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			layout.AddChild(spacer);

			// Buttons container
			var buttonsContainer = new HBoxContainer();
			buttonsContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			buttonsContainer.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
			buttonsContainer.AddThemeConstantOverride("separation", 10);
			layout.AddChild(buttonsContainer);

			// Approve button
			var approveButton = new Button();
			approveButton.Text = "APPROVE";
			approveButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			approveButton.AddThemeColorOverride("font_color", new Color(0f, 0.8f, 0f));
			approveButton.Pressed += OnApprovePressed;
			buttonsContainer.AddChild(approveButton);

			// Reject button
			var rejectButton = new Button();
			rejectButton.Text = "REJECT";
			rejectButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			rejectButton.AddThemeColorOverride("font_color", new Color(0.8f, 0.2f, 0.2f));
			rejectButton.Pressed += OnRejectPressed;
			buttonsContainer.AddChild(rejectButton);

			return panel;
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
				_gameState.Connect("PhaseChanged", Callable.From<GamePhase, GamePhase>(HandlePhaseChanged));
			}

			if (_timeManager != null)
			{
				_timeManager.Connect("Tick", Callable.From<float>(HandleTimeTick));
			}

			if (_listenerManager != null)
			{
				_listenerManager.Connect("ListenersChanged", Callable.From<int, int>(HandleListenersChanged));
			}

			if (_economyManager != null)
			{
				_economyManager.Connect("MoneyChanged", Callable.From<int, int>(HandleMoneyChanged));
			}

			var vernStats = GameStateManager.Instance?.VernStats;
			if (vernStats != null)
			{
				vernStats.Connect("StatsChanged", Callable.From(HandleStatsChanged));
			}

			if (_callerQueue != null)
			{
				_callerQueue.Connect("CallerAdded", Callable.From<Caller>(HandleCallerQueueChanged));
				_callerQueue.Connect("CallerRemoved", Callable.From<Caller>(HandleCallerQueueChanged));
				_callerQueue.Connect("CallerOnAir", Callable.From<Caller>(HandleCallerQueueChanged));
				_callerQueue.Connect("CallerCompleted", Callable.From<Caller>(HandleCallerQueueChanged));
			}
		}

		private void UnsubscribeFromEvents()
		{
			if (_gameState != null)
			{
				_gameState.Disconnect("PhaseChanged", Callable.From<GamePhase, GamePhase>(HandlePhaseChanged));
			}

			if (_timeManager != null)
			{
				_timeManager.Disconnect("Tick", Callable.From<float>(HandleTimeTick));
			}

			if (_listenerManager != null)
			{
				_listenerManager.Disconnect("ListenersChanged", Callable.From<int, int>(HandleListenersChanged));
			}

			if (_economyManager != null)
			{
				_economyManager.Disconnect("MoneyChanged", Callable.From<int, int>(HandleMoneyChanged));
			}

			var vernStats = GameStateManager.Instance?.VernStats;
			if (vernStats != null)
			{
				vernStats.Disconnect("StatsChanged", Callable.From(HandleStatsChanged));
			}

			if (_callerQueue != null)
			{
				_callerQueue.Disconnect("CallerAdded", Callable.From<Caller>(HandleCallerQueueChanged));
				_callerQueue.Disconnect("CallerRemoved", Callable.From<Caller>(HandleCallerQueueChanged));
				_callerQueue.Disconnect("CallerOnAir", Callable.From<Caller>(HandleCallerQueueChanged));
				_callerQueue.Disconnect("CallerCompleted", Callable.From<Caller>(HandleCallerQueueChanged));
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
			RefreshCurrentTab();
		}

		private void HandleCallerQueueChanged(Caller caller = null)
		{
			// Refresh CALLERS tab if it's currently visible
			RefreshTabContent(0); // CALLERS is index 0
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
			if (_liveShowHeader != null)
			{
				_liveShowHeader.RefreshClock();
			}
			else if (_clockText != null)
			{
				// Fallback
				var currentTime = TimeManager.Instance?.CurrentTimeFormatted ?? "12:00 AM";
				var remaining = TimeManager.Instance?.RemainingTimeFormatted ?? "45:00";
				_clockText.Text = $"{currentTime} ({remaining})";
			}
		}

		private void UpdateListenerDisplay()
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

		// Button handlers for caller screening
		private void OnApprovePressed()
		{
			if (_callerQueue != null && _callerQueue.ApproveCurrentCaller())
			{
				// Refresh the callers tab
				RefreshTabContent(0);
			}
		}

		private void OnRejectPressed()
		{
			if (_callerQueue != null && _callerQueue.RejectCurrentCaller())
			{
				// Refresh the callers tab
				RefreshTabContent(0);
			}
		}
	}
}
