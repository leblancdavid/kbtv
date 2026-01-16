using System.Collections.Generic;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Data;

namespace KBTV.UI
{
	/// <summary>
	/// Manages the pre-show UI for topic selection and show preparation.
	/// Only active during the PreShow game phase.
	/// </summary>
	public partial class PreShowUIManager : Node
	{
		public static PreShowUIManager Instance => (PreShowUIManager)((SceneTree)Engine.GetMainLoop()).Root.GetNode("/root/PreShowUIManager");
		private OptionButton _topicSelector;
		private Button _startShowButton;
		private Label _errorLabel;
		private Label _topicDescription;
		private List<Topic> _availableTopics;

		public override void _Ready()
		{
			base._Ready();
			ServiceRegistry.Instance.RegisterSelf<PreShowUIManager>(this);
			LoadTopics();
			GD.Print("PreShowUIManager: Waiting for AllServicesReady before showing UI");

			var registry = ServiceRegistry.Instance;
			if (registry != null && ServiceRegistry.IsInitialized && registry.RegisteredCount >= registry.ExpectedCount)
			{
				CallDeferred(nameof(DelayedRegister));
			}
			else
			{
				CallDeferred(nameof(SubscribeAndWait));
			}
		}

		private void SubscribeAndWait()
		{
			var registry = ServiceRegistry.Instance;
			if (registry != null)
			{
				registry.Connect("AllServicesReady", Callable.From(DelayedRegister));
			}
		}

		private void DelayedRegister()
		{
			var registry = ServiceRegistry.Instance;
			if (registry != null)
			{
				registry.Disconnect("AllServicesReady", Callable.From(DelayedRegister));
			}
			GD.Print("PreShowUIManager: All services ready, creating and registering UI");
			CreatePreShowUI();
			SubscribeToEvents();
		}

		private void CompleteInitialization()
		{
			UpdateUI();
			GD.Print("PreShowUIManager: Initialization complete");
		}

		private void LoadTopics()
		{
			_availableTopics = TopicLoader.LoadAllTopics();
			if (_availableTopics.Count == 0)
			{
				GD.PrintErr("PreShowUIManager: No topics available!");
				// Show error state
			}
			else
			{
				// GD.Print($"PreShowUIManager: Loaded {_availableTopics.Count} topics");
			}
		}

	private void CreatePreShowUI()
	{
		// Register with UIManager (deferred to ensure UIManager is ready)
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
			GD.Print("PreShowUIManager: Registered with UIManager");

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
		// Create a VBoxContainer to hold our UI elements
		var contentContainer = new VBoxContainer();
		contentContainer.Name = "PreShowContent";
		contentContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		contentContainer.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		contentContainer.CustomMinimumSize = new Vector2(700, 500);
		container.AddChild(contentContainer);

		// Create UI elements and add them to the content container
		var title = CreateTitle();
		var topicSelection = CreateTopicSelection();
		var topicDescription = CreateTopicDescription();
		var startButton = CreateStartButton();
		var errorDisplay = CreateErrorDisplay();

		// GD.Print("PreShowUIManager: Created UI elements");

		// Add elements to content container with spacing
		contentContainer.AddChild(title);
		contentContainer.AddChild(UITheme.CreateSpacer(false, false)); // Small vertical space
		contentContainer.AddChild(topicSelection);
		contentContainer.AddChild(UITheme.CreateSpacer(false, false)); // Small vertical space
		contentContainer.AddChild(topicDescription);
		contentContainer.AddChild(UITheme.CreateSpacer(false, false)); // Small vertical space
		contentContainer.AddChild(startButton);
		contentContainer.AddChild(errorDisplay);

		// Select first topic by default after UI is fully created
		if (_availableTopics.Count > 0)
		{
			_topicSelector.Select(0);
			OnTopicSelected(0);
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
		UITheme.ApplyButtonStyle(_topicSelector); // OptionButton can use button styling
		container.AddChild(_topicSelector);

		// Populate topics
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

	private Control CreateStartButton()
	{
		var buttonContainer = new HBoxContainer();
		buttonContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		buttonContainer.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		buttonContainer.CustomMinimumSize = new Vector2(0, 60);

		// Spacer to center the button
		var leftSpacer = new Control();
		leftSpacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		buttonContainer.AddChild(leftSpacer);

		_startShowButton = new Button();
		_startShowButton.Text = "START LIVE SHOW";
		_startShowButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		_startShowButton.CustomMinimumSize = new Vector2(250, 50);
		_startShowButton.Disabled = true; // Initially disabled until topic selected
		_startShowButton.Pressed += OnStartShowPressed;
		UITheme.ApplyButtonStyle(_startShowButton);
		buttonContainer.AddChild(_startShowButton);

		// Spacer to center the button
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
				ServiceRegistry.Instance.GameStateManager.StartLiveShow();
				// GD.Print("PreShowUIManager: Starting live show");
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
 			gameState.Connect("PhaseChanged", Callable.From<GamePhase, GamePhase>(HandlePhaseChanged));
 			HandlePhaseChanged(GamePhase.PreShow, gameState.CurrentPhase);
 		}

	private void HandlePhaseChanged(GamePhase oldPhase, GamePhase newPhase)
	{
		// Visibility is now handled by UIManager at the CanvasLayer level
		GD.Print($"PreShowUIManager: Phase changed to {newPhase} (visibility handled by UIManager)");
	}


	}
}
