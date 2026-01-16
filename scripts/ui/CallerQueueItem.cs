#nullable enable

using System;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.UI.Components;
using KBTV.UI.Themes;

namespace KBTV.UI
{
    public partial class CallerQueueItem : Panel, ICallerListItem
    {
        [ExportGroup("Node References")]
        [Export]
        private Label _nameLabel = null!;

        [Export]
        private ProgressBar _statusIndicator = null!;

        private Caller? _caller;
        private ICallerRepository? _repository;

        public override void _Ready()
        {
            CallDeferred(nameof(InitializeDeferred));
        }

        private void InitializeDeferred()
        {
            if (!Core.ServiceRegistry.IsInitialized)
            {
                GD.PrintErr("CallerQueueItem: ServiceRegistry not initialized, retrying...");
                CallDeferred(nameof(InitializeDeferred));
                return;
            }

            _repository = Core.ServiceRegistry.Instance.CallerRepository;

            GuiInput += OnGuiInput;

            var events = Core.ServiceRegistry.Instance.EventAggregator;
            events?.Subscribe<Core.Events.Queue.CallerStateChanged>(this, OnCallerStateChanged);
            events?.Subscribe<Core.Events.Screening.ScreeningStarted>(this, OnScreeningStarted);

            UpdateVisualSelection();
        }

        public void SetCaller(Caller? caller)
        {
            _caller = caller;
            ApplyCallerName(caller?.Name ?? "");
            UpdateStatusIndicator();
            UpdateVisualSelection();
        }

        public void SetSelected(bool selected)
        {
            UpdateVisualSelection();
        }

        private void ApplyCallerName(string name)
        {
            if (_nameLabel != null)
            {
                _nameLabel.Text = name;
            }
        }

        public override void _Process(double delta)
        {
            if (_caller != null)
            {
                UpdateStatusIndicator();
            }
        }

        private void OnGuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent &&
                mouseEvent.Pressed &&
                mouseEvent.ButtonIndex == MouseButton.Left)
            {
                OnItemClicked();
            }
        }

        private void OnItemClicked()
        {
            if (_caller == null || _repository == null)
            {
                return;
            }

            if (_repository.CurrentScreening == _caller)
            {
                return;
            }

            var success = _repository.StartScreening(_caller);
            if (success.IsSuccess)
            {
                GD.Print($"CallerQueueItem: Started screening {_caller.Name}");
            }
            else
            {
                GD.PrintErr($"CallerQueueItem: Failed to start screening - {success.ErrorMessage}");
            }
        }

        private void OnCallerStateChanged(Core.Events.Queue.CallerStateChanged evt)
        {
            if (evt.Caller == _caller)
            {
                UpdateVisualSelection();
            }
        }

        private void OnScreeningStarted(Core.Events.Screening.ScreeningStarted evt)
        {
            if (evt.Caller == _caller)
            {
                UpdateVisualSelection();
            }
        }

        private void UpdateVisualSelection()
        {
            if (_caller == null || _statusIndicator == null)
            {
                return;
            }

            bool isScreening = _repository?.CurrentScreening == _caller;

            var style = new StyleBoxFlat
            {
                CornerRadiusTopLeft = 4,
                CornerRadiusTopRight = 4,
                CornerRadiusBottomRight = 4,
                CornerRadiusBottomLeft = 4,
                BgColor = isScreening ? UIColors.Screening.Selected : UIColors.Screening.Default
            };

            AddThemeStyleboxOverride("panel", style);
            AddThemeStyleboxOverride("panel_pressed", style);
            QueueRedraw();

            if (_nameLabel != null)
            {
                _nameLabel.AddThemeColorOverride("font_color",
                    isScreening ? UIColors.Screening.SelectedText : UIColors.Screening.DefaultText);
            }
        }

        private void UpdateStatusIndicator()
        {
            if (_caller == null || _statusIndicator == null)
            {
                return;
            }

            float remainingPatience = _caller.Patience - _caller.WaitTime;
            float patienceRatio = Mathf.Clamp(remainingPatience / _caller.Patience, 0f, 1f);

            _statusIndicator.Value = patienceRatio;
            _statusIndicator.AddThemeColorOverride("fill", UIColors.GetPatienceColor(patienceRatio));
        }

        public override void _ExitTree()
        {
            var events = Core.ServiceRegistry.Instance?.EventAggregator;
            events?.Unsubscribe(this);
        }
    }
}
