#nullable enable

using System;
using System.Linq;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Managers;
using KBTV.Screening;
using KBTV.UI.Components;
using KBTV.UI.Themes;

namespace KBTV.UI
{
    public partial class CallerTab : Control, ICallerActions
    {
        public event Action? CloseRequested;
        public event Action? BackRequested;
        [ExportGroup("Node References")]
        [Export]
        private VBoxContainer? _incomingPanel;

        [Export]
        private Control? _screeningPanel;

        [Export]
        private VBoxContainer? _onHoldPanel;

        private ICallerRepository _repository = null!;
        private IScreeningController _screeningController = null!;
        private CallerTabManager _tabManager = null!;
        private CallerListAdapter _incomingAdapter = null!;
        private ReactiveListPanel<Caller>? _reactiveListPanel;
        private Button? _closeButton;
        private Button? _backButton;
        private Label? _showTimerLabel;
        private TimeManager _timeManager = null!;

        private string? _previousScreeningCallerId;
        private int _previousIncomingCount;
        private int _previousOnHoldCount;
        private string _previousTimerText = "--:--";

        public override void _Ready()
        {
            InitializeServices();
            InitializeNodeReferences();
            CreateTabManager();
            PopulateTabContent();

            TrackStateForRefresh();
            RefreshTabContent(); // Ensure initial visibility is set correctly
            UpdateShowTimer();
        }

        private void InitializeNodeReferences()
        {
            _incomingPanel = GetNode<VBoxContainer>("HBoxContainer/IncomingScroll/IncomingMargin/IncomingList");
            _screeningPanel = GetNode<Control>("HBoxContainer/ScreeningContainer");
            _onHoldPanel = GetNode<VBoxContainer>("HBoxContainer/OnHoldScroll/OnHoldMargin/OnHoldList");

        }

        private void InitializeServices()
        {
            _repository = DependencyInjection.Get<ICallerRepository>(this);
            _screeningController = DependencyInjection.Get<IScreeningController>(this);
            _incomingAdapter = new CallerListAdapter(_repository);
            _timeManager = DependencyInjection.Get<TimeManager>(this);
        }

        private void CreateTabManager()
        {
            _tabManager = new CallerTabManager(_repository, _screeningController, this);
        }

        private void PopulateTabContent()
        {
            CreateIncomingPanel();
            CreateScreeningPanel();
            CreateOnHoldPanel();

        }

        private void CreateIncomingPanel()
        {
            if (_incomingPanel == null)
            {
                Log.Error("CallerTab.CreateIncomingPanel: _incomingPanel is null - node not found in scene");
                return;
            }

            if (_reactiveListPanel == null)
            {
                foreach (var child in _incomingPanel.GetChildren().ToList())
                {
                    _incomingPanel.RemoveChild(child);
                    child.QueueFree();
                }

                if (_backButton != null)
                {
                    _backButton.Pressed -= OnBackPressed;
                    _backButton.QueueFree();
                    _backButton = null;
                }

                var topRow = new HBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill
                };
                topRow.AddThemeConstantOverride("separation", UITheme.SPACING_SMALL);

                _backButton = new Button
                {
                    Text = "<-",
                    CustomMinimumSize = new Vector2(24, 18),
                    SizeFlagsHorizontal = SizeFlags.ShrinkBegin
                };
                _backButton.AddThemeFontSizeOverride("font_size", 9);
                UITheme.ApplyButtonStyle(_backButton);
                _backButton.Pressed += OnBackPressed;
                topRow.AddChild(_backButton);

                _showTimerLabel = new Label
                {
                    Text = _timeManager?.RemainingTimeFormatted ?? "--:--",
                    HorizontalAlignment = HorizontalAlignment.Left,
                    SizeFlagsHorizontal = SizeFlags.ExpandFill
                };
                _showTimerLabel.AddThemeFontSizeOverride("font_size", 9);
                _showTimerLabel.AddThemeFontOverride("font", UITheme.MonoFont);
                topRow.AddChild(_showTimerLabel);

                _incomingPanel.AddChild(topRow);

                var header = new Label
                {
                    Text = "INCOMING CALLERS",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    CustomMinimumSize = new Vector2(0, UITheme.BUTTON_HEIGHT)
                };
                header.AddThemeFontSizeOverride("font_size", 10);
                header.AddThemeConstantOverride("margin_left", 4);
                header.AddThemeConstantOverride("margin_right", 4);
                header.AddThemeColorOverride("font_color", UIColors.Queue.Incoming);
                _incomingPanel.AddChild(header);

                var spacer = new Control
                {
                    CustomMinimumSize = new Vector2(0, UITheme.SPACING_SMALL),
                    SizeFlagsVertical = SizeFlags.ShrinkEnd
                };
                _incomingPanel.AddChild(spacer);

                _reactiveListPanel = new ReactiveListPanel<Caller>
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    SizeFlagsVertical = SizeFlags.ExpandFill
                };
                _reactiveListPanel.SetAdapter(_incomingAdapter);

                _incomingPanel.AddChild(_reactiveListPanel);
            }

            UpdateIncomingPanelData();
        }

        private void UpdateIncomingPanelData()
        {
            if (_reactiveListPanel == null)
            {
                return;
            }

            var incomingCallers = _repository.IncomingCallers.ToList();
            _reactiveListPanel.SetData(incomingCallers);
        }

        private void CreateScreeningPanel()
        {
            if (_screeningPanel == null)
            {
                Log.Error("CallerTab.CreateScreeningPanel: _screeningPanel is null - node not found in scene");
                return;
            }
            _tabManager.CreateScreeningPanel(_screeningPanel);
        }

        private void UpdateScreeningPanel()
        {
            if (_screeningPanel == null)
            {
                Log.Error("CallerTab.UpdateScreeningPanel: _screeningPanel is null - node not found in scene");
                return;
            }
            _tabManager.UpdateScreeningPanelContent();
        }

        private void CreateOnHoldPanel()
        {
            if (_onHoldPanel == null)
            {
                Log.Error("CallerTab.CreateOnHoldPanel: _onHoldPanel is null - node not found in scene");
                return;
            }

            foreach (var child in _onHoldPanel.GetChildren().ToList())
            {
                _onHoldPanel.RemoveChild(child);
                child.QueueFree();
            }

            if (_closeButton != null)
            {
                _closeButton.Pressed -= OnClosePressed;
                _closeButton.QueueFree();
                _closeButton = null;
            }

            _closeButton = new Button
            {
                Text = "X",
                CustomMinimumSize = new Vector2(24, 18),
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd
            };
            _closeButton.AddThemeFontSizeOverride("font_size", 9);
            UITheme.ApplyButtonStyle(_closeButton);
            _closeButton.Pressed += OnClosePressed;
            _onHoldPanel.AddChild(_closeButton);

            var header = new Label
            {
                Text = "ON HOLD",
                HorizontalAlignment = HorizontalAlignment.Center,
                CustomMinimumSize = new Vector2(0, UITheme.BUTTON_HEIGHT)
            };
                header.AddThemeFontSizeOverride("font_size", 10);
                header.AddThemeConstantOverride("margin_left", 4);
                header.AddThemeConstantOverride("margin_right", 4);
            header.AddThemeColorOverride("font_color", UIColors.Queue.OnHold);
            _onHoldPanel.AddChild(header);

                var spacer = new Control
                {
                    CustomMinimumSize = new Vector2(0, UITheme.SPACING_SMALL),
                    SizeFlagsVertical = SizeFlags.ShrinkEnd
                };
            _onHoldPanel.AddChild(spacer);

            var listContainer = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            listContainer.AddThemeConstantOverride("separation", UITheme.SPACING_SMALL);
            _onHoldPanel.AddChild(listContainer);

            if (_repository.OnHoldCallers.Count > 0)
            {
                foreach (var caller in _repository.OnHoldCallers)
                {
                    var callerLabel = new Label
                    {
                        Text = $"• {caller.Name} - {caller.Location}"
                    };
                    callerLabel.AddThemeFontSizeOverride("font_size", 9);
                    callerLabel.AddThemeColorOverride("font_color", UIColors.TEXT_SECONDARY);
                    listContainer.AddChild(callerLabel);
                }
            }
            else
            {
                var emptyLabel = new Label
                {
                    Text = "None",
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                emptyLabel.AddThemeFontSizeOverride("font_size", 9);
                emptyLabel.AddThemeColorOverride("font_color", UIColors.TEXT_DISABLED);
                listContainer.AddChild(emptyLabel);
            }
        }

        private void OnClosePressed()
        {
            CloseRequested?.Invoke();
        }

        private void OnBackPressed()
        {
            BackRequested?.Invoke();
        }

        private void TrackStateForRefresh()
        {
            _previousScreeningCallerId = _repository.CurrentScreening?.Id;
            _previousIncomingCount = _repository.IncomingCallers.Count;
            _previousOnHoldCount = _repository.OnHoldCallers.Count;
            _previousTimerText = _timeManager?.RemainingTimeFormatted ?? "--:--";
        }

        public override void _Process(double delta)
        {
            if (_repository == null) return;

            var screeningCallerId = _repository.CurrentScreening?.Id;
            var incomingCount = _repository.IncomingCallers.Count;
            var onHoldCount = _repository.OnHoldCallers.Count;
            var timerText = _timeManager?.RemainingTimeFormatted ?? "--:--";

            if (screeningCallerId != _previousScreeningCallerId ||
                incomingCount != _previousIncomingCount ||
                onHoldCount != _previousOnHoldCount ||
                timerText != _previousTimerText)
            {
                RefreshTabContent();
                UpdateShowTimer();
                _previousScreeningCallerId = screeningCallerId;
                _previousIncomingCount = incomingCount;
                _previousOnHoldCount = onHoldCount;
                _previousTimerText = timerText;
            }
        }

        private void RefreshTabContent()
        {
            UpdateIncomingPanelData();
            UpdateScreeningPanel();
            CreateOnHoldPanel();
            
            // Keep screening panel visible to preserve layout width
            if (_screeningPanel != null)
            {
                _screeningPanel.Visible = true;
            }
        }

        private void UpdateShowTimer()
        {
            if (_timeManager == null || _showTimerLabel == null)
            {
                return;
            }

            var remainingText = _timeManager.RemainingTimeFormatted ?? "--:--";
            _showTimerLabel.Text = remainingText;

            var remainingSeconds = _timeManager.RemainingTime;
            if (remainingSeconds <= 30f)
            {
                _showTimerLabel.AddThemeColorOverride("font_color", Colors.Red);
            }
            else if (remainingSeconds <= 60f)
            {
                _showTimerLabel.AddThemeColorOverride("font_color", Colors.Yellow);
            }
            else
            {
                _showTimerLabel.AddThemeColorOverride("font_color", Colors.White);
            }
        }

        public void OnApproveCaller()
        {
            if (_screeningController.CurrentCaller == null)
            {
                return;
            }

            var result = _screeningController.Approve();
            if (!result.IsSuccess)
            {
                Log.Error($"CallerTab: Failed to approve caller: {result.ErrorCode}: {result.ErrorMessage}");
            }
        }

        public void OnRejectCaller()
        {
            if (_screeningController.CurrentCaller == null)
            {
                return;
            }

            var result = _screeningController.Reject();
            if (!result.IsSuccess)
            {
                Log.Error($"CallerTab: Failed to reject caller: {result.ErrorCode}: {result.ErrorMessage}");
            }
        }

        public override void _ExitTree()
        {
            if (_closeButton != null)
            {
                _closeButton.Pressed -= OnClosePressed;
            }

            if (_backButton != null)
            {
                _backButton.Pressed -= OnBackPressed;
            }
        }
    }
}
