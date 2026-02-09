using System;
using Godot;
using KBTV.UI.Themes;
using KBTV.Screening;
using KBTV.Core;
using KBTV.Callers;

namespace KBTV.UI
{
    /// <summary>
    /// UI section for evidence collection during screening.
    /// Displays an "Examine" button that becomes enabled and flashes when evidence is available.
    /// </summary>
    public partial class EvidenceSection : Control, IDependent
    {
        [Export]
        private Button _examineButton = null!;

        private IScreeningController? _screeningController;
        private bool _evidenceAvailable;
        private float _flashTimer;

        public override void _Notification(int what) => this.Notify(what);

        public override void _Ready()
        {
            EnsureNodesInitialized();
            UpdateButtonState();
        }

        public void OnResolved()
        {
            _screeningController = DependencyInjection.Get<IScreeningController>(this);
            if (_screeningController != null)
            {
                _screeningController.EvidenceCollected += OnEvidenceCollected;
                _screeningController.ProgressUpdated += OnProgressUpdated;
                _screeningController.PhaseChanged += OnPhaseChanged;
            }

            // Subscribe to modal events to disable button during modal
            var modalManager = DependencyInjection.Get<ModalManager>(this);
            if (modalManager != null)
            {
                modalManager.ModalOpened += OnModalOpened;
                modalManager.ModalClosed += OnModalClosed;
            }
        }

        public override void _Process(double delta)
        {
            // Handle flashing animation when evidence is available
            if (_evidenceAvailable && _examineButton != null && IsInstanceValid(_examineButton) && _examineButton.Disabled == false)
            {
                _flashTimer += (float)delta;
                UpdateFlashAnimation();
            }
        }

        private void OnEvidenceCollected(Caller caller)
        {
            // Evidence has been collected, so it's no longer available
            _evidenceAvailable = false;
            UpdateButtonState();
        }

        private void OnProgressUpdated(ScreeningProgress progress)
        {
            // Check if evidence availability has changed
            bool evidenceNowAvailable = _screeningController?.IsEvidenceAvailable ?? false;
            if (evidenceNowAvailable != _evidenceAvailable)
            {
                _evidenceAvailable = evidenceNowAvailable;
                UpdateButtonState();
            }
        }

        private void OnModalOpened()
        {
            // Disable button while modal is open
            if (_examineButton != null && IsInstanceValid(_examineButton))
            {
                _examineButton.Disabled = true;
                StyleDisabledButton(); // Apply disabled styling
            }
        }

        private void OnModalClosed()
        {
            // Re-enable button if evidence is still available
            if (_evidenceAvailable)
            {
                UpdateButtonState();
            }
        }

        private void OnPhaseChanged(ScreeningPhase phase)
        {
            // Reset evidence state when screening session ends
            if (phase == ScreeningPhase.Completed || phase == ScreeningPhase.Idle)
            {
                _evidenceAvailable = false;
                _flashTimer = 0f;
                UpdateButtonState();
            }
        }

        private void UpdateButtonState()
        {
            if (_examineButton == null || !IsInstanceValid(_examineButton))
                return;

            bool shouldBeEnabled = _evidenceAvailable;
            _examineButton.Disabled = !shouldBeEnabled;

            if (shouldBeEnabled)
            {
                StyleEnabledButton();
            }
            else
            {
                StyleDisabledButton();
            }
        }

        private void StyleEnabledButton()
        {
            if (_examineButton == null || !IsInstanceValid(_examineButton))
                return;

            // Use green accent color for enabled state, with flashing effect
            var style = new StyleBoxFlat
            {
                BgColor = UIColors.Accent.Green,
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
                ContentMarginLeft = 20,
                ContentMarginRight = 20,
                ContentMarginTop = 12,
                ContentMarginBottom = 12
            };
            _examineButton.AddThemeStyleboxOverride("normal", style);
            _examineButton.AddThemeStyleboxOverride("hover", style);
            _examineButton.AddThemeColorOverride("font_color", UIColors.TEXT_PRIMARY);
            _examineButton.QueueRedraw();
        }

        private void StyleDisabledButton()
        {
            if (_examineButton == null || !IsInstanceValid(_examineButton))
                return;

            // Use disabled background color
            var style = new StyleBoxFlat
            {
                BgColor = UIColors.BG_DISABLED,
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
                ContentMarginLeft = 20,
                ContentMarginRight = 20,
                ContentMarginTop = 12,
                ContentMarginBottom = 12
            };
            _examineButton.AddThemeStyleboxOverride("normal", style);
            _examineButton.AddThemeStyleboxOverride("hover", style);
            _examineButton.AddThemeColorOverride("font_color", UIColors.TEXT_DISABLED);
            _examineButton.QueueRedraw();
        }

        private void UpdateFlashAnimation()
        {
            if (_examineButton == null || !IsInstanceValid(_examineButton))
                return;

            // Flash every 0.5 seconds between green and default disabled color
            bool isFlashPhase = Mathf.Floor(_flashTimer * 2) % 2 == 0;
            Color bgColor = isFlashPhase ? UIColors.Accent.Green : UIColors.BG_DISABLED;

            var style = new StyleBoxFlat
            {
                BgColor = bgColor,
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
                ContentMarginLeft = 20,
                ContentMarginRight = 20,
                ContentMarginTop = 12,
                ContentMarginBottom = 12
            };
            _examineButton.AddThemeStyleboxOverride("normal", style);
            _examineButton.AddThemeStyleboxOverride("hover", style);
            _examineButton.QueueRedraw();
        }

        private void OnExaminePressed()
        {
            if (_evidenceAvailable && _screeningController != null)
            {
                // Open evidence collection modal
                var modalManager = DependencyInjection.Get<ModalManager>(this);
                modalManager?.ShowEvidenceModal();
            }
        }

        private void EnsureNodesInitialized()
        {
            if (_examineButton == null)
            {
                _examineButton = GetNodeOrNull<Button>("MarginContainer/VBoxContainer/ExamineButton");
                if (_examineButton != null)
                {
                    _examineButton.Pressed += OnExaminePressed;
                }
            }
        }

        public override void _ExitTree()
        {
            if (_screeningController != null)
            {
                _screeningController.EvidenceCollected -= OnEvidenceCollected;
                _screeningController.ProgressUpdated -= OnProgressUpdated;
                _screeningController.PhaseChanged -= OnPhaseChanged;
            }

            // Unsubscribe from modal events
            var modalManager = DependencyInjection.Get<ModalManager>(this);
            if (modalManager != null)
            {
                modalManager.ModalOpened -= OnModalOpened;
                modalManager.ModalClosed -= OnModalClosed;
            }

            if (_examineButton != null)
            {
                _examineButton.Pressed -= OnExaminePressed;
            }
        }
    }
}