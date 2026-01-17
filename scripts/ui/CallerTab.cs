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

        private string? _previousScreeningCallerId;
        private int _previousIncomingCount;
        private int _previousOnHoldCount;

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
            PopulateTabContent();

            TrackStateForRefresh();

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

            if (_reactiveListPanel == null)
            {
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

            GD.Print($"CallerTab: Updated incoming panel with {incomingCallers.Count} callers");
        }

        private void CreateScreeningPanel()
        {
            if (_screeningPanel == null)
            {
                GD.PrintErr("CallerTab.CreateScreeningPanel: _screeningPanel is null - node not found in scene");
                return;
            }
            _tabManager.CreateScreeningPanel(_screeningPanel);
        }

        private void UpdateScreeningPanel()
        {
            if (_screeningPanel == null)
            {
                GD.PrintErr("CallerTab.UpdateScreeningPanel: _screeningPanel is null - node not found in scene");
                return;
            }
            _tabManager.UpdateScreeningPanelContent();
        }

        private void CreateOnHoldPanel()
        {
            if (_onHoldPanel == null)
            {
                GD.PrintErr("CallerTab.CreateOnHoldPanel: _onHoldPanel is null - node not found in scene");
                return;
            }

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
        }

        private void RefreshTabContent()
        {
            UpdateIncomingPanelData();
            UpdateScreeningPanel();
            CreateOnHoldPanel();
        }

        public void OnApproveCaller()
        {
            GD.Print("CallerTab: Approve button pressed");

            if (_screeningController.CurrentCaller == null)
            {
                GD.Print("CallerTab: Approve pressed but no caller is being screened - ignoring");
                return;
            }

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

            if (_screeningController.CurrentCaller == null)
            {
                GD.Print("CallerTab: Reject pressed but no caller is being screened - ignoring");
                return;
            }

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
            GD.Print("CallerTab: Cleanup complete");
        }
    }
}
