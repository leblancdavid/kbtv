using System.Collections.Generic;
using Godot;
using KBTV.Ads;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Data;
using KBTV.Managers;
using KBTV.Persistence;
using KBTV.UI;

namespace KBTV.UI
{
 	public partial class PreShowUIManager : CanvasLayer, IDependent
 	{
 		public override void _Notification(int what) => this.Notify(what);

		// Tab system
		private Button _showTabButton = null!;
		private Button _upgradesTabButton = null!;
		private Control _showPanelContainer = null!;
		private Control _upgradesPanelContainer = null!;
		private PreShowShowPanel? _showPanel;
		private PreShowUpgradesPanel? _upgradesPanel;

		// Legacy fields (kept for compatibility but not used)
		private VBoxContainer contentContainer;
		private OptionButton _topicSelector;
		private Label _topicDescription;
		private Button _startShowButton;
		private Label _errorLabel;
		private AdConfigPanel adConfigPanel;
		private List<Topic> _availableTopics;
		private Label _revenueEstimateLabel;
		private Label _adTimeEstimateLabel;

		private CheckBox _disableAudioCheckBox;

		private int _breaksPerShow = AdConstants.DEFAULT_BREAKS_PER_SHOW;
		private int _slotsPerBreak = AdConstants.DEFAULT_SLOTS_PER_BREAK;
		private int _showDurationMinutes = 10;

		private bool _disableBroadcastAudio = true;

		// Show duration controls
		private Button _decreaseDurationButton;
		private Label _durationLabel;
		private Button _increaseDurationButton;

 		public override void _Ready()
 		{
 			CreateTabSystem();
 			ConnectToGameStateManager();
 		}

 		public void OnResolved()
 		{
 			RegisterWithUIManager();
			InitializePanels();
 		}

		private void InitializePanels()
		{
			GD.Print("[PreShowUIManager] Creating panels...");
			
			// Create the Show panel
			_showPanel = new PreShowShowPanel();
			_showPanelContainer.AddChild(_showPanel);
			GD.Print("[PreShowUIManager] Show panel created");
			
			// Create the Upgrades panel
			_upgradesPanel = new PreShowUpgradesPanel();
			_upgradesPanelContainer.AddChild(_upgradesPanel);
			GD.Print("[PreShowUIManager] Upgrades panel created");
			
			// Initially show the Show tab
			ShowTab("show");
			GD.Print("[PreShowUIManager] Initial tab set");
		}

		private void CreateTabSystem()
		{
			// Create main container
			var mainContainer = new MarginContainer();
			mainContainer.AnchorLeft = 0;
			mainContainer.AnchorTop = 0;
			mainContainer.AnchorRight = 1;
			mainContainer.AnchorBottom = 1;
			mainContainer.OffsetLeft = 0;
			mainContainer.OffsetTop = 0;
			mainContainer.OffsetRight = 0;
			mainContainer.OffsetBottom = 0;
			AddChild(mainContainer);

			// Create vertical split for tabs and content
			var vbox = new VBoxContainer();
			vbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			vbox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			vbox.AddThemeConstantOverride("separation", UITheme.SPACING_SMALL);
			mainContainer.AddChild(vbox);

			// Create tab button container at the top
			var tabButtonContainer = new HBoxContainer();
			tabButtonContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
			tabButtonContainer.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
			tabButtonContainer.AddThemeConstantOverride("separation", UITheme.SPACING_SMALL);
			vbox.AddChild(tabButtonContainer);

			_showTabButton = new Button();
			_showTabButton.Text = "SHOW";
			_showTabButton.AddThemeFontSizeOverride("font_size", UITheme.FONT_TINY);
			_showTabButton.CustomMinimumSize = new Vector2(UITheme.ScaleInt(110), UITheme.BUTTON_HEIGHT);
			_showTabButton.Pressed += () => ShowTab("show");
			UITheme.ApplyButtonStyle(_showTabButton);
			tabButtonContainer.AddChild(_showTabButton);

			_upgradesTabButton = new Button();
			_upgradesTabButton.Text = "UPGRADES";
			_upgradesTabButton.AddThemeFontSizeOverride("font_size", UITheme.FONT_TINY);
			_upgradesTabButton.CustomMinimumSize = new Vector2(UITheme.ScaleInt(110), UITheme.BUTTON_HEIGHT);
			_upgradesTabButton.Pressed += () => ShowTab("upgrades");
			UITheme.ApplyButtonStyle(_upgradesTabButton);
			tabButtonContainer.AddChild(_upgradesTabButton);

			// Create content container that fills remaining space
			var contentContainer = new Control();
			contentContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			contentContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			vbox.AddChild(contentContainer);

			// Show panel container - fills content area
			_showPanelContainer = new Control();
			_showPanelContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			_showPanelContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			_showPanelContainer.Visible = true;
			contentContainer.AddChild(_showPanelContainer);

			// Upgrades panel container - fills content area
			_upgradesPanelContainer = new Control();
			_upgradesPanelContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			_upgradesPanelContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			_upgradesPanelContainer.Visible = false;
			contentContainer.AddChild(_upgradesPanelContainer);
		}

		private void ShowTab(string tabName)
		{
			GD.Print($"[PreShowUIManager] Switching to tab: {tabName}");
			
			if (tabName == "show")
			{
				_showPanelContainer.Visible = true;
				_upgradesPanelContainer.Visible = false;
				_showTabButton.AddThemeColorOverride("font_color", UITheme.ACCENT_GOLD);
				_upgradesTabButton.AddThemeColorOverride("font_color", Colors.Gray);
				_showPanel?.RefreshData();
				GD.Print("[PreShowUIManager] Show tab active");
			}
			else
			{
				_showPanelContainer.Visible = false;
				_upgradesPanelContainer.Visible = true;
				_showTabButton.AddThemeColorOverride("font_color", Colors.Gray);
				_upgradesTabButton.AddThemeColorOverride("font_color", UITheme.ACCENT_GOLD);
				_upgradesPanel?.RefreshData();
				GD.Print("[PreShowUIManager] Upgrades tab active");
			}
		}

 		private void CompleteInitialization()
		{
			LoadFromSave();
			UpdateUI();
		}

		private void ConnectToGameStateManager()
		{
			DeferredConnect();
		}

		private void DeferredConnect()
		{
			var gameStateManager = DependencyInjection.Get<GameStateManager>(this);
			if (gameStateManager != null)
			{
				gameStateManager.Connect("PhaseChanged", Callable.From<int, int>(OnPhaseChanged));
				UpdateUI();
			}
			else
			{
				Log.Error("PreShowUIManager: GameStateManager not available");
			}
		}

		private void OnPhaseChanged(int oldPhaseInt, int newPhaseInt)
		{
			UpdateUI();
		}

		private void LoadTopics()
		{
			_availableTopics = KBTV.Data.TopicLoader.LoadAllTopics() ?? new List<Topic>();
		}

		private void CreatePreShowUI()
		{
			var container = new MarginContainer();
			container.AnchorLeft = 0;
			container.AnchorTop = 0;
			container.AnchorRight = 1;
			container.AnchorBottom = 1;
			container.OffsetLeft = 0;
			container.OffsetTop = 0;
			container.OffsetRight = 0;
			container.OffsetBottom = 0;
			container.AddThemeConstantOverride("margin_top", 100);
			container.AddThemeConstantOverride("margin_bottom", 100);
			container.AddThemeConstantOverride("margin_left", 0);
			container.AddThemeConstantOverride("margin_right", 0);
			AddChild(container);

			LoadTopics();
			SetupPreShowUI(container);
			CompleteInitialization();
		}

		private void RegisterWithUIManager()
		{
			var uiManager = DependencyInjection.Get<IUIManager>(this);
			if (uiManager == null)
			{
				Log.Error("PreShowUIManager: UIManager not available - cannot register PreShow layer!");
				return;
			}

			uiManager.RegisterPreShowLayer(this);
			Log.Debug("PreShowUIManager: Registered with UIManager as PreShow layer");
		}

		private void LoadFromSave()
		{
			var saveManager = DependencyInjection.Get<SaveManager>(this);
			if (saveManager != null)
			{
				var save = saveManager.CurrentSave;
				if (save.ShowDurationMinutes >= 1 && save.ShowDurationMinutes <= 20)
				{
					_showDurationMinutes = save.ShowDurationMinutes;
				}
				_disableBroadcastAudio = save.DisableBroadcastAudio;
			}
		}

		private void SetupPreShowUI(Container container)
		{
			var scrollContainer = new ScrollContainer();
			scrollContainer.Name = "PreShowScroll";
			scrollContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			scrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			scrollContainer.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
			scrollContainer.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
			container.AddChild(scrollContainer);

			contentContainer = new VBoxContainer();
			contentContainer.Name = "PreShowContent";
			contentContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
			contentContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			contentContainer.AddThemeConstantOverride("separation", UITheme.SPACING_SMALL);
			scrollContainer.AddChild(contentContainer);

			var title = CreateTitle();
			title.SizeFlagsStretchRatio = 0;
			contentContainer.AddChild(title);

			var spacer1 = UITheme.CreateSpacer(false, true);
			spacer1.SizeFlagsStretchRatio = 2;
			contentContainer.AddChild(spacer1);

			var topicSelector = new TopicSelector(_availableTopics);
			if (topicSelector != null && topicSelector.SelectorButton != null)
			{
				_topicSelector = topicSelector.SelectorButton;
				_topicDescription = topicSelector.TopicDescription;
				_topicSelector.ItemSelected += OnTopicSelected;
			}
			contentContainer.AddChild(topicSelector);
			topicSelector.SizeFlagsStretchRatio = 0;
			adConfigPanel = new AdConfigPanel();
			adConfigPanel.SizeFlagsStretchRatio = 0;
			contentContainer.AddChild(adConfigPanel);

			// Add audio disable toggle
			var audioToggleContainer = CreateAudioToggle();
			audioToggleContainer.SizeFlagsStretchRatio = 0;
			contentContainer.AddChild(audioToggleContainer);

			// Connect incrementor/decrement button events
			adConfigPanel.DecreaseDurationButton.Pressed += OnDurationDecreasePressed;
			adConfigPanel.IncreaseDurationButton.Pressed += OnDurationIncreasePressed;
			adConfigPanel.DecreaseBreaksButton.Pressed += OnBreaksDecreasePressed;
			adConfigPanel.IncreaseBreaksButton.Pressed += OnBreaksIncreasePressed;
			adConfigPanel.DecreaseSlotsButton.Pressed += OnSlotsDecreasePressed;
			adConfigPanel.IncreaseSlotsButton.Pressed += OnSlotsIncreasePressed;

			var spacer3 = UITheme.CreateSpacer(false, true);
			spacer3.SizeFlagsStretchRatio = 2;
			contentContainer.AddChild(spacer3);

			var startButtonContainer = CreateStartButton();
			startButtonContainer.SizeFlagsStretchRatio = 0;
			contentContainer.AddChild(startButtonContainer);

			_errorLabel = CreateErrorDisplay();
			_errorLabel.SizeFlagsStretchRatio = 0;
			contentContainer.AddChild(_errorLabel);

			if (_availableTopics.Count > 0 && _topicSelector != null && _topicSelector.GetItemCount() > 0)
			{
				_topicSelector.Select(0);
				OnTopicSelected(0);
			}

			// Force layout update to ensure proper positioning
			UpdateLayout();
		}

		private void UpdateLayout()
		{
			if (contentContainer != null)
			{
				contentContainer.QueueSort();
				contentContainer.QueueRedraw();
			}
		}

		private Control CreateTitle()
		{
			var title = new Label();
			title.Text = "KBTV - PRE-SHOW SETUP";
			title.HorizontalAlignment = HorizontalAlignment.Center;
			title.AddThemeColorOverride("font_color", UITheme.ACCENT_GOLD);
			title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			title.CustomMinimumSize = new Vector2(0, 60);
			return title;
		}







		private void OnBreaksDecreasePressed()
		{
			try
			{
				if (_breaksPerShow > 0)
				{
					_breaksPerShow--;
					adConfigPanel.SetBreaksPerShow(_breaksPerShow);
					UpdateSave();
				}
			}
			catch (System.Exception e)
			{
				Log.Error($"Error in OnBreaksDecreasePressed: {e}");
			}
		}

		private void OnBreaksIncreasePressed()
		{
			try
			{
				if (_breaksPerShow < AdConstants.MAX_BREAKS_PER_SHOW)
				{
					_breaksPerShow++;
					adConfigPanel.SetBreaksPerShow(_breaksPerShow);
				}
			}
			catch (System.Exception e)
			{
				Log.Error($"Error in OnBreaksIncreasePressed: {e}");
			}
		}

		private void OnSlotsDecreasePressed()
		{
			try
			{
				if (_slotsPerBreak > 1)
				{
					_slotsPerBreak--;
					adConfigPanel.SetSlotsPerBreak(_slotsPerBreak);
				}
			}
			catch (System.Exception e)
			{
				Log.Error($"Error in OnSlotsDecreasePressed: {e}");
			}
		}

		private void OnSlotsIncreasePressed()
		{
			try
			{
				if (_slotsPerBreak < AdConstants.MAX_SLOTS_PER_BREAK)
				{
					_slotsPerBreak++;
					adConfigPanel.SetSlotsPerBreak(_slotsPerBreak);
				}
			}
			catch (System.Exception e)
			{
				Log.Error($"Error in OnSlotsIncreasePressed: {e}");
			}
		}

		private void OnDurationDecreasePressed()
		{
			try
			{
				if (_showDurationMinutes > 1)
				{
					_showDurationMinutes--;
					adConfigPanel.SetShowDuration(_showDurationMinutes);
					UpdateSave();
				}
			}
			catch (System.Exception e)
			{
				Log.Error($"Error in OnDurationDecreasePressed: {e}");
			}
		}

		private void OnDurationIncreasePressed()
		{
			try
			{
				if (_showDurationMinutes < 30)
				{
					_showDurationMinutes++;
					adConfigPanel.SetShowDuration(_showDurationMinutes);
					UpdateSave();
				}
			}
			catch (System.Exception e)
			{
				Log.Error($"Error in OnDurationIncreasePressed: {e}");
			}
		}



		private void OnDisableAudioToggled(bool pressed)
		{
			_disableBroadcastAudio = pressed;
			UpdateSave();
		}



		private void UpdateSave()
		{
			try
			{
				var saveManager = DependencyInjection.Get<SaveManager>(this);
				if (saveManager != null)
				{
					saveManager.CurrentSave.ShowDurationMinutes = _showDurationMinutes;
					saveManager.CurrentSave.DisableBroadcastAudio = _disableBroadcastAudio;
					saveManager.MarkDirty();
				}
			}
			catch (System.Exception e)
			{
				Log.Error($"Error in UpdateSave: {e}");
			}
		}



		private Control CreateStartButton()
		{
			var buttonContainer = new HBoxContainer();
			buttonContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			buttonContainer.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
			buttonContainer.CustomMinimumSize = new Vector2(0, UITheme.Scale(36));

			var leftSpacer = new Control();
			leftSpacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			buttonContainer.AddChild(leftSpacer);

			_startShowButton = new Button();
			_startShowButton.Text = "START LIVE SHOW";
			_startShowButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
			_startShowButton.CustomMinimumSize = new Vector2(UITheme.ScaleInt(180), UITheme.Scale(36));
			_startShowButton.Disabled = true;
			_startShowButton.Pressed += OnStartShowPressed;
			UITheme.ApplyButtonStyle(_startShowButton);
			buttonContainer.AddChild(_startShowButton);

			var rightSpacer = new Control();
			rightSpacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			buttonContainer.AddChild(rightSpacer);

			return buttonContainer;
		}

		private Control CreateAudioToggle()
		{
			var container = new HBoxContainer();
			container.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			container.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
			container.CustomMinimumSize = new Vector2(0, UITheme.Scale(36));
			container.AddThemeConstantOverride("separation", UITheme.SPACING_SMALL);

			var label = new Label();
			label.Text = "Disable Broadcast Audio";
			label.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
			label.VerticalAlignment = VerticalAlignment.Center;
			container.AddChild(label);

			_disableAudioCheckBox = new CheckBox();
			_disableAudioCheckBox.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
			_disableAudioCheckBox.ButtonPressed = _disableBroadcastAudio;
			_disableAudioCheckBox.Toggled += OnDisableAudioToggled;
			container.AddChild(_disableAudioCheckBox);

			return container;
		}

		private Label CreateErrorDisplay()
		{
			_errorLabel = new Label();
			_errorLabel.Text = "";
			_errorLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_errorLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			_errorLabel.AddThemeColorOverride("font_color", UITheme.ACCENT_RED);
			_errorLabel.CustomMinimumSize = new Vector2(0, UITheme.Scale(24));
			return _errorLabel;
		}

		private void OnTopicSelected(long index)
		{
			if (index >= 0 && index < _availableTopics.Count)
			{
				var selectedTopic = _availableTopics[(int)index];
				var gameStateManager = DependencyInjection.Get<GameStateManager>(this);
				if (gameStateManager != null)
				{
					gameStateManager.SetSelectedTopic(selectedTopic);
				}
				if (_topicDescription != null)
				{
					_topicDescription.Text = selectedTopic.Description;
				}
				if (_startShowButton != null)
				{
					_startShowButton.Disabled = false;
				}
				if (_errorLabel != null)
				{
					_errorLabel.Text = "";
				}
			}
		}

		private void OnStartShowPressed()
		{
			var gameStateManager = DependencyInjection.Get<GameStateManager>(this);
			var timeManager = DependencyInjection.Get<TimeManager>(this);
			
			if (gameStateManager == null || timeManager == null)
			{
				// Defer execution until services are available
				CallDeferred(nameof(DeferredStartShow));
				return;
			}
			
			ExecuteStartShow(gameStateManager, timeManager);
		}

		private void DeferredStartShow()
		{
			var gameStateManager = DependencyInjection.Get<GameStateManager>(this);
			var timeManager = DependencyInjection.Get<TimeManager>(this);
			
			if (gameStateManager != null && timeManager != null)
			{
				ExecuteStartShow(gameStateManager, timeManager);
			}
			else
			{
				_errorLabel.Text = "SYSTEM INITIALIZING... PLEASE WAIT";
				Log.Error("PreShowUIManager: Services still not available after deferral");
			}
		}

		private void ExecuteStartShow(GameStateManager gameStateManager, TimeManager timeManager)
		{
			if (gameStateManager.CanStartLiveShow())
			{
				// Set the ad schedule
				var adSchedule = new AdSchedule(_breaksPerShow, _slotsPerBreak);
				gameStateManager.SetAdSchedule(adSchedule);

				// Set the show duration
				if (timeManager != null)
				{
					timeManager.SetShowDuration(_showDurationMinutes * 60f);
				}

				gameStateManager.StartLiveShow();
			}
			else
			{
				_errorLabel.Text = "PLEASE SELECT A TOPIC FIRST";
				Log.Error("PreShowUIManager: Cannot start show - no topic selected");
			}
		}

		private void UpdateUI()
		{
			var gameState = DependencyInjection.Get<GameStateManager>(this);
			if (gameState != null && _startShowButton != null)
			{
				_startShowButton.Disabled = !gameState.CanStartLiveShow();
			}
		}

		public override void _ExitTree()
		{
			// Disconnect Godot signals
			var gameStateManager = DependencyInjection.Get<GameStateManager>(this);
			if (gameStateManager != null)
			{
				gameStateManager.Disconnect("PhaseChanged", Callable.From<int, int>(OnPhaseChanged));
			}
			
			// Disconnect C# events
			if (_topicSelector != null)
			{
				_topicSelector.ItemSelected -= OnTopicSelected;
			}
			
			if (adConfigPanel != null)
			{
				adConfigPanel.DecreaseDurationButton.Pressed -= OnDurationDecreasePressed;
				adConfigPanel.IncreaseDurationButton.Pressed -= OnDurationIncreasePressed;
				adConfigPanel.DecreaseBreaksButton.Pressed -= OnBreaksDecreasePressed;
				adConfigPanel.IncreaseBreaksButton.Pressed -= OnBreaksIncreasePressed;
				adConfigPanel.DecreaseSlotsButton.Pressed -= OnSlotsDecreasePressed;
				adConfigPanel.IncreaseSlotsButton.Pressed -= OnSlotsIncreasePressed;
			}
			
			if (_startShowButton != null)
			{
				_startShowButton.Pressed -= OnStartShowPressed;
			}
			
			if (_disableAudioCheckBox != null)
			{
				_disableAudioCheckBox.Toggled -= OnDisableAudioToggled;
			}
			
			base._ExitTree();


	}
}
}
