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
    public partial class PreShowShowPanel : Control, IDependent
    {
        public override void _Notification(int what) => this.Notify(what);

        private VBoxContainer _contentContainer = null!;
        private OptionButton _topicSelector = null!;
        private Label _topicDescription = null!;
        private Button _startShowButton = null!;
        private Label _errorLabel = null!;
        private AdConfigPanel _adConfigPanel = null!;
        private CheckBox _disableAudioCheckBox = null!;

        private List<Topic> _availableTopics = null!;
        private int _breaksPerShow = AdConstants.DEFAULT_BREAKS_PER_SHOW;
        private int _slotsPerBreak = AdConstants.DEFAULT_SLOTS_PER_BREAK;
        private int _showDurationMinutes = 10;
        private bool _disableBroadcastAudio = true;

        public Button StartShowButton => _startShowButton;

        private GameStateManager? _gameStateManager;
        private TimeManager? _timeManager;
        private SaveManager? _saveManager;

        public override void _Ready()
        {
            GD.Print("[PreShowShowPanel] _Ready called");
            _availableTopics = new List<Topic>();
            CreateShowPanel();
            LoadTopics();
            LoadFromSave();
            SetupUI();
            ConnectSignals();
            GD.Print("[PreShowShowPanel] _Ready complete");
        }

        public void OnResolved()
        {
            GD.Print("[PreShowShowPanel] OnResolved called");
            _gameStateManager = DependencyInjection.Get<GameStateManager>(this);
            _timeManager = DependencyInjection.Get<TimeManager>(this);
            _saveManager = DependencyInjection.Get<SaveManager>(this);
            
            GD.Print($"[PreShowShowPanel] Dependencies resolved, topics count: {_availableTopics.Count}");
            
            // Set the default topic (index 0) now that dependencies are ready
            if (_availableTopics.Count > 0)
            {
                GD.Print("[PreShowShowPanel] Calling OnTopicSelected(0)");
                OnTopicSelected(0);
            }
            
            RefreshData();
            GD.Print("[PreShowShowPanel] Calling UpdateUI");
            UpdateUI();
        }

        private void CreateShowPanel()
        {
            // Set this control to fill the parent container
            AnchorLeft = 0;
            AnchorTop = 0;
            AnchorRight = 1;
            AnchorBottom = 1;
            OffsetLeft = 0;
            OffsetTop = 0;
            OffsetRight = 0;
            OffsetBottom = 0;

            // Create content container directly
            _contentContainer = new VBoxContainer();
            _contentContainer.Name = "ShowContent";
            _contentContainer.AnchorLeft = 0;
            _contentContainer.AnchorTop = 0;
            _contentContainer.AnchorRight = 1;
            _contentContainer.AnchorBottom = 1;
            _contentContainer.OffsetLeft = UITheme.MARGIN_SMALL;
            _contentContainer.OffsetTop = UITheme.MARGIN_SMALL;
            _contentContainer.OffsetRight = -UITheme.MARGIN_SMALL;
            _contentContainer.OffsetBottom = -UITheme.MARGIN_SMALL;
            _contentContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            _contentContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _contentContainer.AddThemeConstantOverride("separation", UITheme.SPACING_SMALL);
            AddChild(_contentContainer);
        }

        private void LoadTopics()
        {
            _availableTopics = TopicLoader.LoadAllTopics() ?? new List<Topic>();
        }

        private void LoadFromSave()
        {
            if (_saveManager?.CurrentSave != null)
            {
                var save = _saveManager.CurrentSave;
                if (save.ShowDurationMinutes >= 1 && save.ShowDurationMinutes <= 20)
                {
                    _showDurationMinutes = save.ShowDurationMinutes;
                }
                _disableBroadcastAudio = save.DisableBroadcastAudio;
            }
        }

        private void SetupUI()
        {
            var title = CreateTitle();
            title.SizeFlagsStretchRatio = 0;
            _contentContainer.AddChild(title);

            var spacer1 = UITheme.CreateSpacer(false, true);
            spacer1.SizeFlagsStretchRatio = 0.4f;
            _contentContainer.AddChild(spacer1);

            var topicSelector = new TopicSelector(_availableTopics);
            if (topicSelector != null && topicSelector.SelectorButton != null)
            {
                _topicSelector = topicSelector.SelectorButton;
                _topicDescription = topicSelector.TopicDescription;
            _topicDescription.AddThemeFontSizeOverride("font_size", UITheme.FONT_TINY);
            _topicDescription.CustomMinimumSize = new Vector2(0, UITheme.Scale(24));
            }
            _contentContainer.AddChild(topicSelector);
            topicSelector.SizeFlagsStretchRatio = 0;

            _adConfigPanel = new AdConfigPanel();
            _adConfigPanel.SizeFlagsStretchRatio = 0;
            _contentContainer.AddChild(_adConfigPanel);

            var audioToggleContainer = CreateAudioToggle();
            audioToggleContainer.SizeFlagsStretchRatio = 0;
            _contentContainer.AddChild(audioToggleContainer);

            var spacer3 = UITheme.CreateSpacer(false, true);
            spacer3.SizeFlagsStretchRatio = 0.4f;
            _contentContainer.AddChild(spacer3);

            var startButtonContainer = CreateStartButton();
            startButtonContainer.SizeFlagsStretchRatio = 0;
            _contentContainer.AddChild(startButtonContainer);

            _errorLabel = CreateErrorDisplay();
            _errorLabel.SizeFlagsStretchRatio = 0;
            _contentContainer.AddChild(_errorLabel);

            if (_availableTopics.Count > 0 && _topicSelector != null && _topicSelector.GetItemCount() > 0)
            {
                _topicSelector.Select(0);
                OnTopicSelected(0);
            }

            UpdateLayout();
        }

        private void UpdateLayout()
        {
            if (_contentContainer != null)
            {
                _contentContainer.QueueSort();
                _contentContainer.QueueRedraw();
            }
        }

        private Control CreateTitle()
        {
            var title = new Label();
            title.Text = "KBTV - PRE-SHOW SETUP";
            title.HorizontalAlignment = HorizontalAlignment.Center;
            title.AddThemeColorOverride("font_color", UITheme.ACCENT_GOLD);
            title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            title.CustomMinimumSize = new Vector2(0, UITheme.Scale(36));
            title.AddThemeFontSizeOverride("font_size", UITheme.FONT_SMALL);
            return title;
        }

        private void ConnectSignals()
        {
            if (_topicSelector != null)
            {
                _topicSelector.ItemSelected += OnTopicSelected;
            }

            if (_adConfigPanel != null)
            {
                _adConfigPanel.DecreaseDurationButton.Pressed += OnDurationDecreasePressed;
                _adConfigPanel.IncreaseDurationButton.Pressed += OnDurationIncreasePressed;
                _adConfigPanel.DecreaseBreaksButton.Pressed += OnBreaksDecreasePressed;
                _adConfigPanel.IncreaseBreaksButton.Pressed += OnBreaksIncreasePressed;
                _adConfigPanel.DecreaseSlotsButton.Pressed += OnSlotsDecreasePressed;
                _adConfigPanel.IncreaseSlotsButton.Pressed += OnSlotsIncreasePressed;
            }
        }

        private void OnBreaksDecreasePressed()
        {
            if (_breaksPerShow > 0)
            {
                _breaksPerShow--;
                _adConfigPanel.SetBreaksPerShow(_breaksPerShow);
                UpdateSave();
            }
        }

        private void OnBreaksIncreasePressed()
        {
            if (_breaksPerShow < AdConstants.MAX_BREAKS_PER_SHOW)
            {
                _breaksPerShow++;
                _adConfigPanel.SetBreaksPerShow(_breaksPerShow);
            }
        }

        private void OnSlotsDecreasePressed()
        {
            if (_slotsPerBreak > 1)
            {
                _slotsPerBreak--;
                _adConfigPanel.SetSlotsPerBreak(_slotsPerBreak);
            }
        }

        private void OnSlotsIncreasePressed()
        {
            if (_slotsPerBreak < AdConstants.MAX_SLOTS_PER_BREAK)
            {
                _slotsPerBreak++;
                _adConfigPanel.SetSlotsPerBreak(_slotsPerBreak);
            }
        }

        private void OnDurationDecreasePressed()
        {
            if (_showDurationMinutes > 1)
            {
                _showDurationMinutes--;
                _adConfigPanel.SetShowDuration(_showDurationMinutes);
                UpdateSave();
            }
        }

        private void OnDurationIncreasePressed()
        {
            if (_showDurationMinutes < 30)
            {
                _showDurationMinutes++;
                _adConfigPanel.SetShowDuration(_showDurationMinutes);
                UpdateSave();
            }
        }

        private void UpdateSave()
        {
            if (_saveManager?.CurrentSave != null)
            {
                _saveManager.CurrentSave.ShowDurationMinutes = _showDurationMinutes;
                _saveManager.CurrentSave.DisableBroadcastAudio = _disableBroadcastAudio;
                _saveManager.MarkDirty();
            }
        }

        private Control CreateStartButton()
        {
            var buttonContainer = new HBoxContainer();
            buttonContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            buttonContainer.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            buttonContainer.CustomMinimumSize = new Vector2(0, UITheme.Scale(28));

            var leftSpacer = new Control();
            leftSpacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            buttonContainer.AddChild(leftSpacer);

            _startShowButton = new Button();
            _startShowButton.Text = "START LIVE SHOW";
            _startShowButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            _startShowButton.CustomMinimumSize = new Vector2(UITheme.ScaleInt(150), UITheme.Scale(26));
            _startShowButton.AddThemeFontSizeOverride("font_size", UITheme.FONT_TINY);
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
            container.CustomMinimumSize = new Vector2(0, UITheme.Scale(30));
            container.AddThemeConstantOverride("separation", UITheme.SPACING_SMALL);

            var label = new Label();
            label.Text = "Disable Broadcast Audio";
            label.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.AddThemeFontSizeOverride("font_size", UITheme.FONT_TINY);
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

        private void OnDisableAudioToggled(bool pressed)
        {
            _disableBroadcastAudio = pressed;
            UpdateSave();
        }

        private void OnTopicSelected(long index)
        {
            GD.Print($"[PreShowShowPanel] OnTopicSelected called with index: {index}");
            GD.Print($"  _availableTopics.Count: {_availableTopics.Count}");
            GD.Print($"  _gameStateManager: {_gameStateManager}");
            
            if (index >= 0 && index < _availableTopics.Count)
            {
                var selectedTopic = _availableTopics[(int)index];
                GD.Print($"  Selected topic: index {(int)index}");


                if (_gameStateManager != null)
                {
                    _gameStateManager.SetSelectedTopic(selectedTopic);
                    GD.Print($"  Topic set on GameStateManager");
                }
                else
                {
                    GD.Print($"  WARNING: _gameStateManager is null!");
                }
                if (_topicDescription != null)
                {
                    _topicDescription.Text = selectedTopic.Description;
                }
                if (_startShowButton != null)
                {
                    GD.Print($"  Enabling start button");
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
            GD.Print("[PreShowShowPanel] Start button pressed");
            GD.Print($"  _gameStateManager: {_gameStateManager}");
            GD.Print($"  _timeManager: {_timeManager}");
            GD.Print($"  CanStartLiveShow: {_gameStateManager?.CanStartLiveShow()}");

            if (_gameStateManager == null || _timeManager == null)
            {
                GD.Print("[PreShowShowPanel] Dependencies null, returning");
                return;
            }

            if (_gameStateManager.CanStartLiveShow())
            {
                GD.Print("[PreShowShowPanel] Starting live show...");
                var adSchedule = new AdSchedule(_breaksPerShow, _slotsPerBreak);
                _gameStateManager.SetAdSchedule(adSchedule);

                _timeManager.SetShowDuration(_showDurationMinutes * 60f);

                _gameStateManager.StartLiveShow();
                GD.Print("[PreShowShowPanel] Live show started");
            }
            else
            {
                GD.Print("[PreShowShowPanel] Cannot start show - no topic selected?");
                if (_errorLabel != null)
                {
                    _errorLabel.Text = "PLEASE SELECT A TOPIC FIRST";
                }
            }
        }

        private void UpdateUI()
        {
            GD.Print("[PreShowShowPanel] UpdateUI called");
            GD.Print($"  _gameStateManager: {_gameStateManager}");
            GD.Print($"  _startShowButton: {_startShowButton}");
            
            // Enable button if topic is selected, regardless of current phase
            // (phase will be PreShow when show actually starts)
            if (_gameStateManager != null && _startShowButton != null)
            {
                bool canStart = _gameStateManager.CanStartLiveShow();
                
                // If CanStartLiveShow is false due to phase, check if topic is selected
                // This handles the case where UI is created during Loading phase
                if (!canStart && _gameStateManager.SelectedTopic != null)
                {
                    GD.Print($"  Phase not ready but topic selected - enabling button");
                    _startShowButton.Disabled = false;
                }
                else
                {
                    _startShowButton.Disabled = !canStart;
                }
                GD.Print($"  CanStartLiveShow: {canStart}, SelectedTopic: {_gameStateManager.SelectedTopic != null}, Button disabled: {_startShowButton.Disabled}");
            }
            else
            {
                GD.Print("[PreShowShowPanel] Cannot update button - dependencies null");
                // Enable button anyway if dependencies aren't ready yet
                if (_startShowButton != null)
                {
                    _startShowButton.Disabled = false;
                }
            }
        }

        public void RefreshData()
        {
            LoadFromSave();
            if (_adConfigPanel != null)
            {
                _adConfigPanel.SetShowDuration(_showDurationMinutes);
                _adConfigPanel.SetBreaksPerShow(_breaksPerShow);
                _adConfigPanel.SetSlotsPerBreak(_slotsPerBreak);
            }
            if (_disableAudioCheckBox != null)
            {
                _disableAudioCheckBox.ButtonPressed = _disableBroadcastAudio;
            }
            UpdateUI();
        }

        public override void _ExitTree()
        {
            if (_topicSelector != null)
            {
                _topicSelector.ItemSelected -= OnTopicSelected;
            }

            if (_adConfigPanel != null)
            {
                _adConfigPanel.DecreaseDurationButton.Pressed -= OnDurationDecreasePressed;
                _adConfigPanel.IncreaseDurationButton.Pressed -= OnDurationIncreasePressed;
                _adConfigPanel.DecreaseBreaksButton.Pressed -= OnBreaksDecreasePressed;
                _adConfigPanel.IncreaseBreaksButton.Pressed -= OnBreaksIncreasePressed;
                _adConfigPanel.DecreaseSlotsButton.Pressed -= OnSlotsDecreasePressed;
                _adConfigPanel.IncreaseSlotsButton.Pressed -= OnSlotsIncreasePressed;
            }

            if (_startShowButton != null)
            {
                _startShowButton.Pressed -= OnStartShowPressed;
            }

            if (_disableAudioCheckBox != null)
            {
                _disableAudioCheckBox.Toggled -= OnDisableAudioToggled;
            }
        }
    }
}
