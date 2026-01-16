#nullable enable

using System;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Screening;
using KBTV.UI.Themes;

namespace KBTV.UI
{
    public partial class ScreeningPanel : Control
    {
        [ExportGroup("Node References")]
        [Export]
        private Label _headerLabel = null!;

        [Export]
        private Label _callerInfoLabel = null!;

        [Export]
        private Button _approveButton = null!;

        [Export]
        private Button _rejectButton = null!;

        [ExportGroup("Optional UI")]
        [Export]
        private ProgressBar? _patienceBar;

        [Export]
        private Label? _patienceLabel;

        private IScreeningController _controller = null!;

        public override void _Ready()
        {
            CallDeferred(nameof(InitializeDeferred));
        }

        private void InitializeDeferred()
        {
            if (!Core.ServiceRegistry.IsInitialized)
            {
                GD.PrintErr("ScreeningPanel: ServiceRegistry not initialized, retrying...");
                CallDeferred(nameof(InitializeDeferred));
                return;
            }

            InitializeController();
            ConnectSignals();
            ValidateNodeReferences();
        }

        private void InitializeController()
        {
            _controller = Core.ServiceRegistry.Instance.ScreeningController;
            _controller.PhaseChanged += OnPhaseChanged;
            _controller.ProgressUpdated += OnProgressUpdated;
        }

        private void ConnectSignals()
        {
            _approveButton!.Pressed += OnApprovePressed;
            _rejectButton!.Pressed += OnRejectPressed;

            var events = Core.ServiceRegistry.Instance.EventAggregator;
            if (events != null)
            {
                events.Subscribe(this, (Core.Events.Screening.ScreeningStarted evt) => OnScreeningStarted(evt));
                events.Subscribe(this, (Core.Events.Screening.ScreeningEnded evt) => OnScreeningEnded(evt));
            }
        }

        private void ValidateNodeReferences()
        {
            var missingNodes = GetMissingNodeReferences();
            if (missingNodes.Count > 0)
            {
                GD.PrintErr($"ScreeningPanel: Missing node references: {string.Join(", ", missingNodes)}");
            }
        }

        private System.Collections.Generic.List<string> GetMissingNodeReferences()
        {
            var missing = new System.Collections.Generic.List<string>();

            if (_headerLabel == null) missing.Add("VBoxContainer/HeaderLabel");
            if (_callerInfoLabel == null) missing.Add("VBoxContainer/CallerInfoScroll/CallerInfoLabel");
            if (_approveButton == null) missing.Add("VBoxContainer/HBoxContainer/ApproveButton");
            if (_rejectButton == null) missing.Add("VBoxContainer/HBoxContainer/RejectButton");

            return missing;
        }

        public void SetCaller(Caller? caller)
        {
            if (_callerInfoLabel == null)
            {
                EnsureNodesInitialized();
            }

            if (caller != null)
            {
                _callerInfoLabel!.Text = BuildCallerInfoText(caller);
            }
            else
            {
                _callerInfoLabel!.Text = "No caller\ncurrently\nscreening";
            }
        }

        private void OnApprovePressed()
        {
            var result = _controller.Approve();
            if (!result.IsSuccess)
            {
                GD.PrintErr($"ScreeningPanel: Approve failed - {result.ErrorCode}: {result.ErrorMessage}");
            }
        }

        private void OnRejectPressed()
        {
            var result = _controller.Reject();
            if (!result.IsSuccess)
            {
                GD.PrintErr($"ScreeningPanel: Reject failed - {result.ErrorCode}: {result.ErrorMessage}");
            }
        }

        private void OnPhaseChanged(ScreeningPhase phase)
        {
            UpdateFromController();
        }

        private void OnProgressUpdated(ScreeningProgress progress)
        {
            UpdatePatienceDisplay(progress);
        }

        private void OnScreeningStarted(Core.Events.Screening.ScreeningStarted evt)
        {
            SetCaller(evt.Caller);
            UpdateFromController();
        }

        private void OnScreeningEnded(Core.Events.Screening.ScreeningEnded evt)
        {
            SetCaller(null);
        }

        private void UpdateFromController()
        {
            SetCaller(_controller.CurrentCaller);
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            bool hasCaller = _controller.CurrentCaller != null;
            _approveButton!.Disabled = !hasCaller;
            _rejectButton!.Disabled = !hasCaller;

            var approveStyle = new StyleBoxFlat();
            approveStyle.BgColor = hasCaller ? UIColors.Button.Approve : UIColors.BG_DISABLED;
            approveStyle.CornerRadiusTopLeft = 3;
            approveStyle.CornerRadiusTopRight = 3;
            approveStyle.CornerRadiusBottomLeft = 3;
            approveStyle.CornerRadiusBottomRight = 3;
            _approveButton.AddThemeStyleboxOverride("normal", approveStyle);

            var rejectStyle = new StyleBoxFlat();
            rejectStyle.BgColor = hasCaller ? UIColors.Button.Reject : UIColors.BG_DISABLED;
            rejectStyle.CornerRadiusTopLeft = 3;
            rejectStyle.CornerRadiusTopRight = 3;
            rejectStyle.CornerRadiusBottomLeft = 3;
            rejectStyle.CornerRadiusBottomRight = 3;
            _rejectButton.AddThemeStyleboxOverride("normal", rejectStyle);
        }

        private void UpdatePatienceDisplay(ScreeningProgress progress)
        {
            if (_patienceBar != null)
            {
                _patienceBar.Value = progress.ProgressPercent;
                _patienceBar.AddThemeColorOverride("fill", UIColors.GetPatienceColor(progress.ProgressPercent));
            }

            if (_patienceLabel != null)
            {
                _patienceLabel.Text = $"Patience: {progress.PatienceRemaining:F1}s / {progress.MaxPatience:F1}s";
            }
        }

        private string BuildCallerInfoText(Caller caller)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"Name: {caller.Name}");
            sb.AppendLine($"Location: {caller.Location}");
            sb.AppendLine($"Topic: {caller.ClaimedTopic}");
            sb.AppendLine();

            sb.AppendLine("AUDIO & EMOTIONAL:");
            sb.AppendLine($"  Quality: {caller.PhoneQuality}");
            sb.AppendLine($"  Emotional State: {caller.EmotionalState}");
            sb.AppendLine($"  Curse Risk: {caller.CurseRisk}");
            sb.AppendLine();

            sb.AppendLine("ASSESSMENT:");
            sb.AppendLine($"  Belief Level: {caller.BeliefLevel}");
            sb.AppendLine($"  Evidence: {caller.EvidenceLevel}");
            sb.AppendLine($"  Coherence: {caller.Coherence}");
            sb.AppendLine($"  Urgency: {caller.Urgency}");
            sb.AppendLine();

            sb.AppendLine("OVERALL:");
            sb.AppendLine($"  Legitimacy: {caller.Legitimacy}");
            sb.AppendLine($"  Personality: {caller.Personality}");

            if (!string.IsNullOrEmpty(caller.ScreeningSummary))
            {
                sb.AppendLine();
                sb.AppendLine("SUMMARY:");
                sb.AppendLine(caller.ScreeningSummary);
            }

            return sb.ToString();
        }

        public void ConnectButtons(Callable approveCallable, Callable rejectCallable)
        {
            EnsureNodesInitialized();
            if (_approveButton != null)
            {
                _approveButton.Connect("pressed", approveCallable);
            }
            if (_rejectButton != null)
            {
                _rejectButton.Connect("pressed", rejectCallable);
            }
        }

        public override void _ExitTree()
        {
            _controller.PhaseChanged -= OnPhaseChanged;
            _controller.ProgressUpdated -= OnProgressUpdated;

            var events = Core.ServiceRegistry.Instance?.EventAggregator;
            events?.Unsubscribe(this);
        }

        private void EnsureNodesInitialized()
        {
            if (_headerLabel == null)
                _headerLabel = GetNode<Label>("VBoxContainer/HeaderLabel");
            if (_callerInfoLabel == null)
                _callerInfoLabel = GetNode<Label>("VBoxContainer/CallerInfoScroll/CallerInfoLabel");
            if (_approveButton == null)
                _approveButton = GetNode<Button>("VBoxContainer/HBoxContainer/ApproveButton");
            if (_rejectButton == null)
                _rejectButton = GetNode<Button>("VBoxContainer/HBoxContainer/RejectButton");
        }
    }
}
