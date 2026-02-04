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
        private Label? _progressLabel;

        private string? _callerId;
        private Caller? _cachedCaller;
        private Caller? _pendingCaller;
        private ICallerRepository? _repository;

        private string? _previousCallerId;
        private CallerState _previousState;
        private float _previousWaitTime;
        private float _previousScreeningPatience;
        private float _previousScreeningProgress;

        public override void _Ready()
        {
            try
            {
                _nameLabel = GetNode<Label>("HBoxContainer/NameLabel");
                _progressLabel = GetNodeOrNull<Label>("HBoxContainer/ProgressLabel");
            }
            catch (Exception ex)
            {
                GD.PrintErr("[CallerQueueItem] ERROR getting node references: " + ex.Message);
            }

            _repository = DependencyInjection.Get<ICallerRepository>(this);

            GuiInput += OnGuiInput;

            ApplyPendingCallerData();
            TrackStateForRefresh();
            UpdateVisualSelection();
            UpdateProgressLabel();
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
            UpdateVisualSelection();
        }

        public void SetSelected(bool selected)
        {
            UpdateVisualSelection();
        }

        private void TrackStateForRefresh()
        {
            if (_cachedCaller != null)
            {
                _previousCallerId = _cachedCaller.Id;
                _previousState = _cachedCaller.State;
                _previousWaitTime = _cachedCaller.WaitTime;
                _previousScreeningPatience = _cachedCaller.ScreeningPatience;
            }
        }

        public override void _Process(double delta)
        {
            if (_repository == null || _callerId == null) return;

            var currentCaller = _repository.GetCaller(_callerId);
            if (currentCaller == null)
            {
                _cachedCaller = null;
                _callerId = null;
                _previousState = CallerState.Disconnected;
                ApplyCallerData(null);
                return;
            }

            _cachedCaller = currentCaller;

            bool needsStatusUpdate = currentCaller.WaitTime != _previousWaitTime ||
                                      (currentCaller.State == CallerState.Screening &&
                                       currentCaller.ScreeningPatience != _previousScreeningPatience);

            if (needsStatusUpdate)
            {
                _previousWaitTime = currentCaller.WaitTime;
                _previousScreeningPatience = currentCaller.ScreeningPatience;
            }

            // Update screening progress display
            float currentProgress = currentCaller.GetScreeningProgress();
            if (currentProgress != _previousScreeningProgress || currentCaller.State != _previousState)
            {
                _previousScreeningProgress = currentProgress;
                UpdateProgressLabel();
            }

            bool needsFullRefresh = currentCaller.Id != _previousCallerId ||
                                    currentCaller.State != _previousState;

            if (needsFullRefresh)
            {
                _previousCallerId = currentCaller.Id;
                _previousState = currentCaller.State;
                UpdateVisualSelection();
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
            if (_repository == null || _callerId == null)
            {
                return;
            }

            var caller = _repository.GetCaller(_callerId);
            if (caller == null)
            {
                _cachedCaller = null;
                _callerId = null;
                ApplyCallerData(null);
                return;
            }

            if (_repository.CurrentScreening == caller)
            {
                return;
            }

            var success = _repository.StartScreening(caller);
            if (!success.IsSuccess)
            {
                GD.PrintErr($"CallerQueueItem: Failed to start screening - {success.ErrorMessage}");
            }
        }

        private void UpdateVisualSelection()
        {
            if (_cachedCaller == null || _callerId == null)
            {
                return;
            }

            var caller = _repository?.GetCaller(_callerId);
            if (caller == null)
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

        private void UpdateProgressLabel()
        {
            if (_progressLabel == null || _callerId == null)
            {
                return;
            }

            var caller = _repository?.GetCaller(_callerId);
            if (caller == null)
            {
                _progressLabel.Visible = false;
                return;
            }

            // Only show progress for callers that have started screening
            float progress = caller.GetScreeningProgress();
            if (progress > 0f || caller.State == CallerState.Screening)
            {
                int percent = (int)(progress * 100f);
                _progressLabel.Text = $"{percent}%";
                _progressLabel.Visible = true;
            }
            else
            {
                _progressLabel.Visible = false;
            }
        }

        public override void _ExitTree()
        {
        }
    }
}
