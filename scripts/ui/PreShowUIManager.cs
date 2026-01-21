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
		public static PreShowUIManager Instance => (PreShowUIManager)((SceneTree)Engine.GetMainLoop()).Root.GetNode("/root/PreShowUIManager");
		private OptionButton _topicSelector;
		private Button _startShowButton;
		private Label _errorLabel;
		private Label _topicDescription;
		private List<Topic> _availableTopics;

		// Ad configuration controls
		private Button _decreaseBreaksButton;
		private Label _breaksCountLabel;
		private Button _increaseBreaksButton;
		private Button _decreaseSlotsButton;
		private Label _slotsCountLabel;
		private Button _increaseSlotsButton;
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
			SubscribeToEvents();
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
			var contentContainer = new VBoxContainer();
			contentContainer.Name = "PreShowContent";
			contentContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
			contentContainer.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
			contentContainer.CustomMinimumSize = new Vector2(700, 600);
			container.AddChild(contentContainer);

			var title = CreateTitle();
			var topicSelection = CreateTopicSelection();
			var topicDescription = CreateTopicDescription();
			var adConfig = CreateAdConfiguration();
			var startButton = CreateStartButton();
			var errorDisplay = CreateErrorDisplay();

			contentContainer.AddChild(title);
			contentContainer.AddChild(UITheme.CreateSpacer(false, false));
			contentContainer.AddChild(topicSelection);
			contentContainer.AddChild(UITheme.CreateSpacer(false, false));
			contentContainer.AddChild(topicDescription);
			contentContainer.AddChild(UITheme.CreateSpacer(false, false));
			contentContainer.AddChild(adConfig);
			contentContainer.AddChild(UITheme.CreateSpacer(false, false));
			contentContainer.AddChild(startButton);
			contentContainer.AddChild(errorDisplay);

			if (_availableTopics.Count > 0)
			{
				_topicSelector.Select(0);
				OnTopicSelected(0);
			}

			UpdateAdConfigLabels();
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

		private Control CreateTopicSelection()
		{
			var container = new VBoxContainer();
			container.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			container.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			container.CustomMinimumSize = new Vector2(0, 120);

			var label = new Label();
			label.Text = "SELECT TOPIC FOR TONIGHT'S SHOW";
			label.HorizontalAlignment = HorizontalAlignment.Center;
			label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			label.AddThemeColorOverride("font_color", UITheme.TEXT_PRIMARY);
			container.AddChild(label);

			_topicSelector = new OptionButton();
			_topicSelector.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
			_topicSelector.CustomMinimumSize = new Vector2(300, 40);
			_topicSelector.ItemSelected += OnTopicSelected;
			UITheme.ApplyButtonStyle(_topicSelector);
			container.AddChild(_topicSelector);

			foreach (var topic in _availableTopics)
			{
				_topicSelector.AddItem(topic.DisplayName);
			}

			return container;
		}

		private Control CreateTopicDescription()
		{
			_topicDescription = new Label();
			_topicDescription.HorizontalAlignment = HorizontalAlignment.Center;
			_topicDescription.VerticalAlignment = VerticalAlignment.Center;
			_topicDescription.AutowrapMode = TextServer.AutowrapMode.Word;
			_topicDescription.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			_topicDescription.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			_topicDescription.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
			_topicDescription.CustomMinimumSize = new Vector2(0, 80);
			return _topicDescription;
		}

		private Control CreateAdConfiguration()
		{
			var container = new VBoxContainer();
			container.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			container.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			container.CustomMinimumSize = new Vector2(0, 180);

			// Header
			var header = new Label();
			header.Text = "AD BREAK CONFIGURATION & SHOW SETTINGS";
			header.HorizontalAlignment = HorizontalAlignment.Center;
			header.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			header.AddThemeColorOverride("font_color", UITheme.TEXT_PRIMARY);
			container.AddChild(header);

			container.AddChild(UITheme.CreateSpacer(false, false));

			// Show duration row
			var durationRow = CreateAdConfigRow("SHOW DURATION (MIN)", out _decreaseDurationButton, out _durationLabel, out _increaseDurationButton);
			container.AddChild(durationRow);

			container.AddChild(UITheme.CreateSpacer(false, false));

			// Breaks per show row
			var breaksRow = CreateAdConfigRow("BREAKS PER SHOW", out _decreaseBreaksButton, out _breaksCountLabel, out _increaseBreaksButton);
			container.AddChild(breaksRow);

			// Connect breaks buttons
			_decreaseBreaksButton.Pressed += OnBreaksDecreasePressed;
			_increaseBreaksButton.Pressed += OnBreaksIncreasePressed;

			container.AddChild(UITheme.CreateSpacer(false, false));

			// Slots per break row
			var slotsRow = CreateAdConfigRow("SLOTS PER BREAK", out _decreaseSlotsButton, out _slotsCountLabel, out _increaseSlotsButton);
			container.AddChild(slotsRow);

			// Connect slots buttons
			_decreaseSlotsButton.Pressed += OnSlotsDecreasePressed;
			_increaseSlotsButton.Pressed += OnSlotsIncreasePressed;

			// Connect duration buttons
			_decreaseDurationButton.Pressed += OnDurationDecreasePressed;
			_increaseDurationButton.Pressed += OnDurationIncreasePressed;

			container.AddChild(UITheme.CreateSpacer(false, false));

			// Estimates row
			var estimatesContainer = new HBoxContainer();
			estimatesContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

			var revenueLabel = new Label();
			revenueLabel.Text = "Est. Revenue: ";
			revenueLabel.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
			estimatesContainer.AddChild(revenueLabel);

			_revenueEstimateLabel = new Label();
			_revenueEstimateLabel.Text = "$12 - $24";
			_revenueEstimateLabel.AddThemeColorOverride("font_color", UITheme.ACCENT_GREEN);
			estimatesContainer.AddChild(_revenueEstimateLabel);

			var spacer = new Control();
			spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			estimatesContainer.AddChild(spacer);

			var timeLabel = new Label();
			timeLabel.Text = "Ad Time: ";
			timeLabel.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
			estimatesContainer.AddChild(timeLabel);

			_adTimeEstimateLabel = new Label();
			_adTimeEstimateLabel.Text = "~1:24";
			_adTimeEstimateLabel.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
			estimatesContainer.AddChild(_adTimeEstimateLabel);

			container.AddChild(estimatesContainer);

			return container;
		}

		private HBoxContainer CreateAdConfigRow(string labelText, out Button decreaseButton, out Label countLabel, out Button increaseButton)
		{
			var row = new HBoxContainer();
			row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

			// Left spacer
			var leftSpacer = new Control();
			leftSpacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			row.AddChild(leftSpacer);

			// Label
			var label = new Label();
			label.Text = labelText;
			label.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
			row.AddChild(label);

			row.AddChild(UITheme.CreateSpacer(true, false));

			// Decrease button
			decreaseButton = new Button();
			decreaseButton.Text = "<";
			decreaseButton.CustomMinimumSize = new Vector2(40, 30);
			UITheme.ApplyButtonStyle(decreaseButton);
			row.AddChild(decreaseButton);

			row.AddChild(UITheme.CreateSpacer(true, false));

			// Count label
			countLabel = new Label();
			countLabel.Text = "2";
			countLabel.CustomMinimumSize = new Vector2(60, 0);
			countLabel.HorizontalAlignment = HorizontalAlignment.Center;
			row.AddChild(countLabel);

			row.AddChild(UITheme.CreateSpacer(true, false));

			// Increase button
			increaseButton = new Button();
			increaseButton.Text = ">";
			increaseButton.CustomMinimumSize = new Vector2(40, 30);
			UITheme.ApplyButtonStyle(increaseButton);
			row.AddChild(increaseButton);

			// Right spacer
			var rightSpacer = new Control();
			rightSpacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			row.AddChild(rightSpacer);

			return row;
		}

		private void OnBreaksDecreasePressed()
		{
			if (_breaksPerShow > 0) _breaksPerShow--;
			UpdateAdConfigLabels();
		}

		private void OnBreaksIncreasePressed()
		{
			if (_breaksPerShow < AdConstants.MAX_BREAKS_PER_SHOW) _breaksPerShow++;
			UpdateAdConfigLabels();
		}

		private void OnSlotsDecreasePressed()
		{
			if (_slotsPerBreak > 1) _slotsPerBreak--;
			UpdateAdConfigLabels();
		}

		private void OnSlotsIncreasePressed()
		{
			if (_slotsPerBreak < AdConstants.MAX_SLOTS_PER_BREAK) _slotsPerBreak++;
			UpdateAdConfigLabels();
		}

		private void OnDurationDecreasePressed()
		{
			if (_showDurationMinutes > 1)
			{
				_showDurationMinutes--;
				UpdateAdConfigLabels();
				UpdateSave();
			}
		}

		private void OnDurationIncreasePressed()
		{
			if (_showDurationMinutes < 20)
			{
				_showDurationMinutes++;
				UpdateAdConfigLabels();
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

		private void UpdateAdConfigLabels()
		{
			if (_breaksCountLabel != null) _breaksCountLabel.Text = _breaksPerShow.ToString();
			if (_slotsCountLabel != null) _slotsCountLabel.Text = _slotsPerBreak.ToString();
			if (_durationLabel != null) _durationLabel.Text = _showDurationMinutes.ToString();

			// Calculate estimates
			int totalSlots = _breaksPerShow * _slotsPerBreak;
			float minRevenue = totalSlots * 50 * AdData.GetRevenueRate(AdType.LocalBusiness);
			float maxRevenue = totalSlots * 300 * AdData.GetRevenueRate(AdType.PremiumSponsor);

			if (_revenueEstimateLabel != null)
			{
				_revenueEstimateLabel.Text = $"${minRevenue:F0} - ${maxRevenue:F0}";
			}

			float adTime = _breaksPerShow * (AdConstants.BREAK_JINGLE_DURATION + (_slotsPerBreak * AdConstants.AD_SLOT_DURATION) + AdConstants.RETURN_JINGLE_DURATION);
			int minutes = (int)(adTime / 60f);
			int seconds = (int)(adTime % 60f);

			if (_adTimeEstimateLabel != null)
			{
				_adTimeEstimateLabel.Text = $"~{minutes}:{seconds:D2}";
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

		private Control CreateErrorDisplay()
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

		private void SubscribeToEvents()
		{
			var gameState = ServiceRegistry.Instance.GameStateManager;

		}


	}
}
