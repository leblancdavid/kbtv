#nullable enable

using System;
using System.Linq;
using Godot;
using KBTV.Callers;
using KBTV.Core;
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
        private readonly CallerListAdapter _incomingAdapter = new();
        private ReactiveListPanel<Caller>? _reactiveListPanel;

        public override void _Ready()
        {
            GD.Print("CallerTab: Initializing with Service Registry pattern");

            CallDeferred(nameof(InitializeDeferred));
        }

        private void InitializeDeferred()
        {
            if (!ServiceRegistry.IsInitialized)
            {
                GD.PrintErr("CallerTab: ServiceRegistry not initialized, retrying...");
                CallDeferred(nameof(InitializeDeferred));
                return;
            }

            InitializeServices();
            InitializeNodeReferences();
            CreateTabManager();
            SubscribeToEvents();
            PopulateTabContent();

            GD.Print("CallerTab: Initialization complete");
        }

        private void InitializeNodeReferences()
        {
            _incomingPanel = GetNode<VBoxContainer>("HBoxContainer/IncomingScroll/IncomingList");
            _screeningPanel = GetNode<Control>("HBoxContainer/ScreeningContainer");
            _onHoldPanel = GetNode<VBoxContainer>("HBoxContainer/OnHoldScroll/OnHoldList");
        }

        private void InitializeServices()
        {
            _repository = ServiceRegistry.Instance.CallerRepository;
            _screeningController = ServiceRegistry.Instance.ScreeningController;
        }

        private void CreateTabManager()
        {
            _tabManager = new CallerTabManager(_repository, _screeningController, this);
        }

        private void SubscribeToEvents()
        {
            var events = ServiceRegistry.Instance.EventAggregator;

            events?.Subscribe(this, (Core.Events.Queue.CallerAdded evt) => OnCallerAdded(evt));
            events?.Subscribe(this, (Core.Events.Queue.CallerRemoved evt) => OnCallerRemoved(evt));
            events?.Subscribe(this, (Core.Events.Queue.CallerStateChanged evt) => OnCallerStateChanged(evt));
            events?.Subscribe(this, (Core.Events.Screening.ScreeningStarted evt) => OnScreeningStarted(evt));
            events?.Subscribe(this, (Core.Events.Screening.ScreeningEnded evt) => OnScreeningEnded(evt));
            events?.Subscribe(this, (Core.Events.Screening.ScreeningApproved evt) => OnScreeningApproved(evt));
            events?.Subscribe(this, (Core.Events.Screening.ScreeningRejected evt) => OnScreeningRejected(evt));
        }

        private void PopulateTabContent()
        {
            GD.Print("CallerTab: Populating tab content");

            GD.Print($"CallerTab: Incoming callers: {_repository.IncomingCallers.Count}, " +
                     $"On-hold: {_repository.OnHoldCallers.Count}, " +
                     $"IsScreening: {_repository.IsScreening}");

            CreateIncomingPanel();
            CreateScreeningPanel();
            CreateOnHoldPanel();
        }

        private void CreateIncomingPanel()
        {
            if (_incomingPanel == null)
            {
                GD.PrintErr("CallerTab.CreateIncomingPanel: _incomingPanel is null - node not found in scene");
                return;
            }

            // Only create the panel structure once
            if (_reactiveListPanel == null)
            {
                // Clear existing children but keep the container
                foreach (var child in _incomingPanel.GetChildren().ToList())
                {
                    _incomingPanel.RemoveChild(child);
                    child.QueueFree();
                }

                var header = new Label
                {
                    Text = "INCOMING CALLERS",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    CustomMinimumSize = new Vector2(0, 24)
                };
                header.AddThemeColorOverride("font_color", UIColors.Queue.Incoming);
                _incomingPanel.AddChild(header);

                var spacer = new Control
                {
                    CustomMinimumSize = new Vector2(0, 16),
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

                GD.Print("CallerTab: Created persistent incoming panel");
            }

            // Always update the data
            UpdateIncomingPanelData();
        }

        private void UpdateIncomingPanelData()
        {
            if (_reactiveListPanel == null)
            {
                return;
            }

            var incomingCallers = _repository.IncomingCallers.ToList();
            var screeningCaller = _repository.CurrentScreening;
            var allCallers = incomingCallers.ToList();
            if (screeningCaller != null && !allCallers.Contains(screeningCaller))
            {
                allCallers.Insert(0, screeningCaller);
            }
            _reactiveListPanel.SetData(allCallers);

            GD.Print($"CallerTab: Updated incoming panel with {allCallers.Count} callers (incoming: {incomingCallers.Count}, screening: {screeningCaller?.Name ?? "none"})");
        }

        private void CreateScreeningPanel()
        {
            if (_screeningPanel == null)
            {
                GD.PrintErr("CallerTab.CreateScreeningPanel: _screeningPanel is null - node not found in scene");
                return;
            }
            ClearPanel(_screeningPanel, true);

            var screeningScene = GD.Load<PackedScene>("res://scenes/ui/ScreeningPanel.tscn");
            if (screeningScene != null)
            {
                var screeningInstance = screeningScene.Instantiate();
                _screeningPanel.AddChild(screeningInstance);
                GD.Print("CallerTab: Created ScreeningPanel");
            }
            else
            {
                GD.PrintErr("CallerTab: Failed to load ScreeningPanel.tscn");
            }
        }

        private void CreateOnHoldPanel()
        {
            if (_onHoldPanel == null)
            {
                GD.PrintErr("CallerTab.CreateOnHoldPanel: _onHoldPanel is null - node not found in scene");
                return;
            }

            // Clear existing children
            foreach (var child in _onHoldPanel.GetChildren().ToList())
            {
                _onHoldPanel.RemoveChild(child);
                child.QueueFree();
            }

            var header = new Label
            {
                Text = "ON HOLD",
                HorizontalAlignment = HorizontalAlignment.Center,
                CustomMinimumSize = new Vector2(0, 24)
            };
            header.AddThemeColorOverride("font_color", UIColors.Queue.OnHold);
            _onHoldPanel.AddChild(header);

            var spacer = new Control
            {
                CustomMinimumSize = new Vector2(0, 16),
                SizeFlagsVertical = SizeFlags.ShrinkEnd
            };
            _onHoldPanel.AddChild(spacer);

            var listContainer = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            listContainer.AddThemeConstantOverride("separation", 4);
            _onHoldPanel.AddChild(listContainer);

            if (_repository.OnHoldCallers.Count > 0)
            {
                foreach (var caller in _repository.OnHoldCallers)
                {
                    var callerLabel = new Label
                    {
                        Text = $"• {caller.Name} - {caller.Location}"
                    };
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
                emptyLabel.AddThemeColorOverride("font_color", UIColors.TEXT_DISABLED);
                listContainer.AddChild(emptyLabel);
            }

            GD.Print($"CallerTab: Created on-hold panel with {_repository.OnHoldCallers.Count} callers");
        }

        private void OnCallerAdded(Core.Events.Queue.CallerAdded evt)
        {
            GD.Print($"CallerTab: Caller added - {evt.Caller?.Name}");
            RefreshTabContent();
        }

        private void OnCallerRemoved(Core.Events.Queue.CallerRemoved evt)
        {
            GD.Print($"CallerTab: Caller removed - {evt.Caller?.Name}");
            RefreshTabContent();
        }

        private void OnCallerStateChanged(Core.Events.Queue.CallerStateChanged evt)
        {
            RefreshTabContent();
        }

        private void OnScreeningStarted(Core.Events.Screening.ScreeningStarted evt)
        {
            GD.Print($"CallerTab: Screening started - {evt.Caller?.Name}");
            RefreshTabContent();
        }

        private void OnScreeningEnded(Core.Events.Screening.ScreeningEnded evt)
        {
            GD.Print($"CallerTab: Screening ended - {evt.Caller?.Name}");
            RefreshTabContent();
        }

        private void OnScreeningApproved(Core.Events.Screening.ScreeningApproved evt)
        {
            GD.Print($"CallerTab: Caller approved - {evt.Caller?.Name}");
            RefreshTabContent();
        }

        private void OnScreeningRejected(Core.Events.Screening.ScreeningRejected evt)
        {
            GD.Print($"CallerTab: Caller rejected - {evt.Caller?.Name}");
            RefreshTabContent();
        }

        private void RefreshTabContent()
        {
            // Update incoming panel data without recreating the panel
            UpdateIncomingPanelData();
            
            // Still need to recreate screening and on-hold panels as they change more dynamically
            CreateScreeningPanel();
            CreateOnHoldPanel();
        }

        private void ClearPanel(Control? panel, bool isScreeningPanel = false)
        {
            if (panel == null)
            {
                GD.PrintErr("CallerTab.ClearPanel: panel is null");
                return;
            }

            foreach (var child in panel.GetChildren().ToList())
            {
                panel.RemoveChild(child);
                child.QueueFree();
            }
        }

        public void OnApproveCaller()
        {
            GD.Print("CallerTab: Approve button pressed");
            var result = _screeningController.Approve();
            if (result.IsSuccess)
            {
                GD.Print("CallerTab: Caller approved successfully");
            }
            else
            {
                GD.PrintErr($"CallerTab: Failed to approve caller: {result.ErrorCode}: {result.ErrorMessage}");
            }
        }

        public void OnRejectCaller()
        {
            GD.Print("CallerTab: Reject button pressed");
            var result = _screeningController.Reject();
            if (result.IsSuccess)
            {
                GD.Print("CallerTab: Caller rejected successfully");
            }
            else
            {
                GD.PrintErr($"CallerTab: Failed to reject caller: {result.ErrorCode}: {result.ErrorMessage}");
            }
        }

        public override void _ExitTree()
        {
            var events = ServiceRegistry.Instance?.EventAggregator;
            events?.Unsubscribe(this);

            GD.Print("CallerTab: Cleanup complete");
        }
    }
}
