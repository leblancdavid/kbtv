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
			CreatePreShowUI();
			ConnectToGameStateManager();
		}

		public void OnResolved()
		{
			RegisterWithUIManager();
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
			contentContainer = new VBoxContainer();
			contentContainer.Name = "PreShowContent";
			contentContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
			contentContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			contentContainer.AddThemeConstantOverride("separation", 20);
			container.AddChild(contentContainer);

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
			buttonContainer.CustomMinimumSize = new Vector2(0, 60);

			var leftSpacer = new Control();
			leftSpacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			buttonContainer.AddChild(leftSpacer);

			_startShowButton = new Button();
			_startShowButton.Text = "START LIVE SHOW";
			_startShowButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
			_startShowButton.CustomMinimumSize = new Vector2(250, 50);
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
			container.CustomMinimumSize = new Vector2(0, 50);
			container.AddThemeConstantOverride("separation", 10);

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
			_errorLabel.CustomMinimumSize = new Vector2(0, 40);
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




	}
}
