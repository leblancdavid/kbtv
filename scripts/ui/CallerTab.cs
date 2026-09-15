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
        private Label? _currentCallerNameLabel;
        private Label? _currentCallerPhoneLabel;

        private string? _previousScreeningCallerId;
        private int _previousIncomingCount;
        private int _previousOnHoldCount;

        public override void _Ready()
        {
            InitializeServices();
            InitializeNodeReferences();
            CreateTabManager();
            PopulateTabContent();

            TrackStateForRefresh();
            RefreshTabContent(); // Ensure initial visibility is set correctly
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

                var header = CreateSectionHeader("INCOMING CALLERS");
                _incomingPanel.AddChild(header);

                _incomingPanel.AddChild(CreateDivider());

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

            var header = CreateSectionHeader("ON HOLD");
            _onHoldPanel.AddChild(header);

            _onHoldPanel.AddChild(CreateDivider());

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
                        Text = $"  {caller.Name}  {caller.PhoneNumber}"
                    };
                    callerLabel.AddThemeFontSizeOverride("font_size", 12);
                    callerLabel.AddThemeFontOverride("font", UITheme.MonoFont);
                    callerLabel.AddThemeColorOverride("font_color", UIColors.Screening.DefaultText);
                    listContainer.AddChild(callerLabel);
                }
            }
            else
            {
                var emptyLabel = new Label
                {
                    Text = "  NONE",
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                emptyLabel.AddThemeFontSizeOverride("font_size", 12);
                emptyLabel.AddThemeFontOverride("font", UITheme.MonoFont);
                emptyLabel.AddThemeColorOverride("font_color", UIColors.Screening.DimText);
                listContainer.AddChild(emptyLabel);
            }

            _onHoldPanel.AddChild(CreateDivider());

            var currentCallerHeader = CreateSectionHeader("CURRENT CALLER");
            _onHoldPanel.AddChild(currentCallerHeader);

            _currentCallerNameLabel = new Label
            {
                Text = "Name: ???"
            };
            _currentCallerNameLabel.AddThemeFontSizeOverride("font_size", 12);
            _currentCallerNameLabel.AddThemeFontOverride("font", UITheme.MonoFont);
            _currentCallerNameLabel.AddThemeColorOverride("font_color", UIColors.Screening.HeaderText);
            _onHoldPanel.AddChild(_currentCallerNameLabel);

            _currentCallerPhoneLabel = new Label
            {
                Text = "Phone: ---"
            };
            _currentCallerPhoneLabel.AddThemeFontSizeOverride("font_size", 12);
            _currentCallerPhoneLabel.AddThemeFontOverride("font", UITheme.MonoFont);
            _currentCallerPhoneLabel.AddThemeColorOverride("font_color", UIColors.Screening.HeaderText);
            _onHoldPanel.AddChild(_currentCallerPhoneLabel);

            UpdateCurrentCallerDisplay();
        }

        private Label CreateSectionHeader(string title)
        {
            var header = new Label
            {
                Text = title,
                HorizontalAlignment = HorizontalAlignment.Center,
                CustomMinimumSize = new Vector2(0, 16)
            };
            header.AddThemeFontSizeOverride("font_size", 13);
            header.AddThemeFontOverride("font", UITheme.MonoFont);
            header.AddThemeColorOverride("font_color", UIColors.Screening.HeaderText);
            return header;
        }

        private Label CreateDivider()
        {
            var divider = new Label
            {
                Text = string.Concat(Enumerable.Repeat("=", 30)),
                HorizontalAlignment = HorizontalAlignment.Center,
                CustomMinimumSize = new Vector2(0, 12)
            };
            divider.AddThemeFontSizeOverride("font_size", 12);
            divider.AddThemeFontOverride("font", UITheme.MonoFont);
            divider.AddThemeColorOverride("font_color", UIColors.Screening.DimText);
            return divider;
        }

        private void UpdateCurrentCallerDisplay()
        {
            if (_currentCallerNameLabel == null || _currentCallerPhoneLabel == null)
            {
                return;
            }

            var caller = _repository.CurrentScreening;
            if (caller == null)
            {
                _currentCallerNameLabel.Text = "Name: N/A";
                _currentCallerPhoneLabel.Text = "Phone: N/A";
                return;
            }

            var nameProperty = caller.ScreenableProperties
                .FirstOrDefault(p => p.PropertyKey == "Name");
            bool nameRevealed = nameProperty != null && nameProperty.IsRevealed;

            _currentCallerNameLabel.Text = $"Name: {(nameRevealed ? caller.Name : "???")}";
            _currentCallerPhoneLabel.Text = $"Phone: {caller.PhoneNumber}";
        }

        private void TrackStateForRefresh()
        {
            _previousScreeningCallerId = _repository.CurrentScreening?.Id;
            _previousIncomingCount = _repository.IncomingCallers.Count;
            _previousOnHoldCount = _repository.OnHoldCallers.Count;
        }

        public override void _Process(double delta)
        {
            if (_repository == null) return;

            var screeningCallerId = _repository.CurrentScreening?.Id;
            var incomingCount = _repository.IncomingCallers.Count;
            var onHoldCount = _repository.OnHoldCallers.Count;

            if (screeningCallerId != _previousScreeningCallerId ||
                incomingCount != _previousIncomingCount ||
                onHoldCount != _previousOnHoldCount)
            {
                RefreshTabContent();
                _previousScreeningCallerId = screeningCallerId;
                _previousIncomingCount = incomingCount;
                _previousOnHoldCount = onHoldCount;
            }

            UpdateCurrentCallerDisplay();
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
    }
}
