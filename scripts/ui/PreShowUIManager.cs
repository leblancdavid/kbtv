using System.Collections.Generic;
using Godot;
using KBTV.Ads;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Data;

namespace KBTV.UI
{
	public partial class PreShowUIManager : Node
	{
		private VBoxContainer contentContainer;
		private OptionButton _topicSelector;
		private Label _topicDescription;
		private Button _startShowButton;
		private Label _errorLabel;
		private AdConfigPanel adConfigPanel;
		private List<Topic> _availableTopics;
		private Label _revenueEstimateLabel;
		private Label _adTimeEstimateLabel;

		private int _breaksPerShow = AdConstants.DEFAULT_BREAKS_PER_SHOW;
		private int _slotsPerBreak = AdConstants.DEFAULT_SLOTS_PER_BREAK;
		private int _showDurationMinutes = 10;

		// Show duration controls
		private Button _decreaseDurationButton;
		private Label _durationLabel;
		private Button _increaseDurationButton;

		public override void _Ready()
		{
			base._Ready();
			ServiceRegistry.Instance.RegisterSelf<PreShowUIManager>(this);
			LoadTopics();

			if (ServiceRegistry.IsInitialized)
			{
				CallDeferred(nameof(DelayedRegister));
			}
			else
			{
				CallDeferred(nameof(RetryInitialization));
			}
		}

		private void RetryInitialization()
		{
			if (ServiceRegistry.IsInitialized)
			{
				DelayedRegister();
			}
			else
			{
				CallDeferred(nameof(RetryInitialization));
			}
		}

		private void DelayedRegister()
		{
			CreatePreShowUI();
		}

		private void CompleteInitialization()
		{
			LoadFromSave();
			UpdateUI();
		}

		private void LoadFromSave()
		{
			if (ServiceRegistry.Instance?.SaveManager != null)
			{
				var save = ServiceRegistry.Instance.SaveManager.CurrentSave;
				if (save.ShowDurationMinutes >= 1 && save.ShowDurationMinutes <= 20)
				{
					_showDurationMinutes = save.ShowDurationMinutes;
				}
			}
		}

		private void LoadTopics()
		{
			_availableTopics = TopicLoader.LoadAllTopics();
			if (_availableTopics.Count == 0)
			{
				GD.PrintErr("PreShowUIManager: No topics available!");
			}
		}

		private void CreatePreShowUI()
		{
			CallDeferred(nameof(RegisterWithUIManager));
		}

		private void RegisterWithUIManager()
		{
			var uiManager = ServiceRegistry.Instance?.UIManager;
			if (uiManager != null)
			{
				var canvasLayer = new CanvasLayer();
				canvasLayer.Name = "PreShowCanvasLayer";
				canvasLayer.Layer = 10;
				canvasLayer.Visible = true;
				AddChild(canvasLayer);

				uiManager.RegisterPreShowLayer(canvasLayer);

				var preShowContainer = new CenterContainer();
				preShowContainer.Name = "PreShowContainer";
				preShowContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
				canvasLayer.AddChild(preShowContainer);

				SetupPreShowUI(preShowContainer);
				CompleteInitialization();
			}
			else
			{
				GD.PrintErr("PreShowUIManager: UIManager not available");
			}
		}

		private void SetupPreShowUI(CenterContainer container)
		{
			contentContainer = new VBoxContainer();
			contentContainer.Name = "PreShowContent";
			contentContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			contentContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			contentContainer.CustomMinimumSize = new Vector2(700, 600);
			container.AddChild(contentContainer);

			var title = CreateTitle();
			var topicSelector = new TopicSelector(_availableTopics);
			_topicSelector = topicSelector.SelectorButton;
			_topicDescription = topicSelector.TopicDescription;
			adConfigPanel = new AdConfigPanel();

			// Set initial values from PreShowUIManager state
			adConfigPanel.SetShowDuration(_showDurationMinutes);
			adConfigPanel.SetBreaksPerShow(_breaksPerShow);
			adConfigPanel.SetSlotsPerBreak(_slotsPerBreak);

			// Connect ad config button events
			adConfigPanel.DecreaseBreaksButton.Pressed += OnBreaksDecreasePressed;
			adConfigPanel.IncreaseBreaksButton.Pressed += OnBreaksIncreasePressed;
			adConfigPanel.DecreaseSlotsButton.Pressed += OnSlotsDecreasePressed;
			adConfigPanel.IncreaseSlotsButton.Pressed += OnSlotsIncreasePressed;
			adConfigPanel.DecreaseDurationButton.Pressed += OnDurationDecreasePressed;
			adConfigPanel.IncreaseDurationButton.Pressed += OnDurationIncreasePressed;
			var startButtonContainer = CreateStartButton();
			_errorLabel = CreateErrorDisplay();

			contentContainer.AddChild(title);
			contentContainer.AddChild(UITheme.CreateSpacer(false, false));
			contentContainer.AddChild(topicSelector);
			contentContainer.AddChild(UITheme.CreateSpacer(false, false));
			contentContainer.AddChild(adConfigPanel);
			contentContainer.AddChild(UITheme.CreateSpacer(false, false));
			contentContainer.AddChild(startButtonContainer);
			contentContainer.AddChild(_errorLabel);

			if (_availableTopics.Count > 0)
			{
				_topicSelector.Select(0);
				OnTopicSelected(0);
			}

			// Force layout update to ensure proper positioning
			CallDeferred(nameof(UpdateLayout));
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
			if (_breaksPerShow > 0)
			{
				_breaksPerShow--;
				adConfigPanel.SetBreaksPerShow(_breaksPerShow);
				UpdateSave();
			}
		}

		private void OnBreaksIncreasePressed()
		{
			if (_breaksPerShow < AdConstants.MAX_BREAKS_PER_SHOW)
			{
				_breaksPerShow++;
				adConfigPanel.SetBreaksPerShow(_breaksPerShow);
			}
		}

		private void OnSlotsDecreasePressed()
		{
			if (_slotsPerBreak > 1)
			{
				_slotsPerBreak--;
				adConfigPanel.SetSlotsPerBreak(_slotsPerBreak);
			}
		}

		private void OnSlotsIncreasePressed()
		{
			if (_slotsPerBreak < AdConstants.MAX_SLOTS_PER_BREAK)
			{
				_slotsPerBreak++;
				adConfigPanel.SetSlotsPerBreak(_slotsPerBreak);
			}
		}

		private void OnDurationDecreasePressed()
		{
			if (_showDurationMinutes > 5)
			{
				_showDurationMinutes--;
				adConfigPanel.SetShowDuration(_showDurationMinutes);
				UpdateSave();
			}
		}

		private void OnDurationIncreasePressed()
		{
			if (_showDurationMinutes < 30)
			{
				_showDurationMinutes++;
				adConfigPanel.SetShowDuration(_showDurationMinutes);
				UpdateSave();
			}
		}



		private void UpdateSave()
		{
			if (ServiceRegistry.Instance?.SaveManager != null)
			{
				ServiceRegistry.Instance.SaveManager.CurrentSave.ShowDurationMinutes = _showDurationMinutes;
				ServiceRegistry.Instance.SaveManager.MarkDirty();
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
				ServiceRegistry.Instance.GameStateManager.SetSelectedTopic(selectedTopic);
				_topicDescription.Text = selectedTopic.Description;
				_startShowButton.Disabled = false;
				_errorLabel.Text = "";
			}
		}

		private void OnStartShowPressed()
		{
			if (ServiceRegistry.Instance.GameStateManager.CanStartLiveShow())
			{
				// Set the ad schedule
				var adSchedule = new AdSchedule(_breaksPerShow, _slotsPerBreak);
				ServiceRegistry.Instance.GameStateManager.SetAdSchedule(adSchedule);

				// Set the show duration
				ServiceRegistry.Instance.TimeManager.SetShowDuration(_showDurationMinutes * 60f);

				ServiceRegistry.Instance.GameStateManager.StartLiveShow();
			}
			else
			{
				_errorLabel.Text = "PLEASE SELECT A TOPIC FIRST";
				GD.PrintErr("PreShowUIManager: Cannot start show - no topic selected");
			}
		}

		private void UpdateUI()
		{
			var gameState = ServiceRegistry.Instance.GameStateManager;
			if (gameState != null && _startShowButton != null)
			{
				_startShowButton.Disabled = !gameState.CanStartLiveShow();
			}
		}




	}
}
