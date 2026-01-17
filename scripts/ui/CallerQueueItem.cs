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
        private Caller? _cachedCaller;
        private Caller? _pendingCaller;
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
                CallDeferred(nameof(InitializeDeferred));
                return;
            }

            _repository = Core.ServiceRegistry.Instance.CallerRepository;

            GuiInput += OnGuiInput;

            var events = Core.ServiceRegistry.Instance.EventAggregator;
            events?.Subscribe<Core.Events.Queue.CallerStateChanged>(this, OnCallerStateChanged);
            events?.Subscribe<Core.Events.Screening.ScreeningStarted>(this, OnScreeningStarted);
            events?.Subscribe<Core.Events.Screening.ScreeningProgressUpdated>(this, OnScreeningProgressUpdated);

            ApplyPendingCallerData();
            UpdateVisualSelection();
            UpdateStatusIndicator();
        }

        private void ApplyPendingCallerData()
        {
            if (_pendingCaller != null)
            {
                _cachedCaller = _pendingCaller;
                _callerId = _pendingCaller.Id;
                _pendingCaller = null;
            }
            ApplyCallerData(_cachedCaller);
        }

        public void SetCaller(Caller? caller)
        {
            _callerId = caller?.Id;
            _cachedCaller = caller;

            if (_nameLabel == null)
            {
                _pendingCaller = caller;
                return;
            }

            ApplyCallerData(caller);
        }

        private void ApplyCallerData(Caller? caller)
        {
            if (_nameLabel != null)
            {
                _nameLabel.Text = caller?.Name ?? "";
                _nameLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
            }
            UpdateStatusIndicator();
            UpdateVisualSelection();
        }

        public void SetSelected(bool selected)
        {
            UpdateVisualSelection();
        }

        public override void _Process(double delta)
        {
        }

        private Caller? GetCurrentCaller()
        {
            return _cachedCaller;
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
                _cachedCaller = caller;
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
            if (evt.Caller != null && evt.Caller.Id == _callerId)
            {
                _cachedCaller = evt.Caller;
                UpdateVisualSelection();
                UpdateStatusIndicator();
            }
        }

        private void OnScreeningProgressUpdated(Core.Events.Screening.ScreeningProgressUpdated evt)
        {
            if (_cachedCaller != null && _cachedCaller.State == CallerState.Screening)
            {
                UpdateStatusIndicator();
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
                remainingPatience = caller.ScreeningPatience;
            }
            else
            {
                remainingPatience = caller.Patience - caller.WaitTime;
            }

            float patienceRatio = Mathf.Clamp(remainingPatience / caller.Patience, 0f, 1f);

            _statusIndicator.Value = patienceRatio;

            var fillStyle = new StyleBoxFlat { BgColor = UIColors.GetPatienceColor(patienceRatio) };
            _statusIndicator.AddThemeStyleboxOverride("fill", fillStyle);

            _statusIndicator.QueueRedraw();
            QueueRedraw();
        }

        public override void _ExitTree()
        {
            var events = Core.ServiceRegistry.Instance?.EventAggregator;
            events?.Unsubscribe(this);
        }
    }
}
