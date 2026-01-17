#nullable enable

using System;
using System.Linq;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.UI.Components;
using KBTV.UI.Themes;

namespace KBTV.UI
{
    public partial class CallerQueueItem : Panel, ICallerListItem
    {
        private Label _nameLabel = null!;
        private ProgressBar _statusIndicator = null!;

        private string? _callerId;
        private ICallerRepository? _repository;

        public override void _Ready()
        {
            try
            {
                _nameLabel = GetNode<Label>("HBoxContainer/NameLabel");
                _statusIndicator = GetNode<ProgressBar>("HBoxContainer/StatusIndicator");
            }
            catch (Exception ex)
            {
                GD.PrintErr("[CallerQueueItem] ERROR getting node references: " + ex.Message);
            }
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
            _callerId = caller?.Id;
            CallDeferred(nameof(_ApplyCallerNameDeferred), caller?.Name ?? "");
        }

        private void _ApplyCallerNameDeferred(string name)
        {
            if (_nameLabel != null)
            {
                _nameLabel.Text = name;
                _nameLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
            }
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
                _nameLabel.Modulate = new Color(0.7f, 0.7f, 0.7f);
            }
        }

        public override void _Process(double delta)
        {
            UpdateStatusIndicator();
            
            // Additional debug logging to verify _Process is called
            if (Engine.GetProcessFrames() % 120 == 0) // ~2 seconds at 60 FPS
            {
                GD.Print($"CallerQueueItem._Process: Processing frame {Engine.GetProcessFrames()} for {GetCurrentCaller()?.Name ?? "null"}");
            }
        }

        private Caller? GetCurrentCaller()
        {
            if (_callerId == null || _repository == null)
            {
                return null;
            }

            var incomingCallers = _repository.IncomingCallers.ToList();
            foreach (var caller in incomingCallers)
            {
                if (caller.Id == _callerId)
                {
                    return caller;
                }
            }

            var onHoldCallers = _repository.OnHoldCallers.ToList();
            foreach (var caller in onHoldCallers)
            {
                if (caller.Id == _callerId)
                {
                    return caller;
                }
            }

            var currentScreening = _repository.CurrentScreening;
            if (currentScreening != null && currentScreening.Id == _callerId)
            {
                return currentScreening;
            }

            return null;
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
            var caller = GetCurrentCaller();
            if (caller == null || _repository == null)
            {
                return;
            }

            if (_repository.CurrentScreening == caller)
            {
                return;
            }

            var success = _repository.StartScreening(caller);
            if (success.IsSuccess)
            {
                GD.Print($"CallerQueueItem: Started screening {caller.Name}");
            }
            else
            {
                GD.PrintErr($"CallerQueueItem: Failed to start screening - {success.ErrorMessage}");
            }
        }

        private void OnCallerStateChanged(Core.Events.Queue.CallerStateChanged evt)
        {
            var currentCaller = GetCurrentCaller();
            if (evt.Caller != null && evt.Caller.Id == _callerId)
            {
                UpdateVisualSelection();
            }
        }

        private void OnScreeningStarted(Core.Events.Screening.ScreeningStarted evt)
        {
            var currentCaller = GetCurrentCaller();
            if (evt.Caller != null && evt.Caller.Id == _callerId)
            {
                UpdateVisualSelection();
            }
        }

        private void UpdateVisualSelection()
        {
            var caller = GetCurrentCaller();
            if (caller == null || _statusIndicator == null)
            {
                return;
            }

            bool isScreening = _repository?.CurrentScreening == caller;

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
            var caller = GetCurrentCaller();
            if (caller == null || _statusIndicator == null)
            {
                return;
            }

            float remainingPatience;
            if (caller.State == CallerState.Screening)
            {
                // During screening, show remaining screening patience
                remainingPatience = caller.ScreeningPatience;
            }
            else
            {
                // For incoming/on-hold callers, show patience minus wait time
                remainingPatience = caller.Patience - caller.WaitTime;
            }
            
            float patienceRatio = Mathf.Clamp(remainingPatience / caller.Patience, 0f, 1f);

            _statusIndicator.Value = patienceRatio;
            
            var fillStyle = new StyleBoxFlat { BgColor = UIColors.GetPatienceColor(patienceRatio) };
            _statusIndicator.AddThemeStyleboxOverride("fill", fillStyle);

            // Force UI redraw
            _statusIndicator.QueueRedraw();
            QueueRedraw();

            // Debug logging every second (more frequent for debugging)
            if (Engine.GetProcessFrames() % 60 == 0) // ~1 second at 60 FPS
            {
                GD.Print($"CallerQueueItem: {caller.Name} - State: {caller.State}, Patience: {remainingPatience:F1}/{caller.Patience:F1}, Ratio: {patienceRatio:F2}, Color: {fillStyle.BgColor}");
            }
        }

        public override void _ExitTree()
        {
            var events = Core.ServiceRegistry.Instance?.EventAggregator;
            events?.Unsubscribe(this);
        }
    }
}
