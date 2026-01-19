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
		private Caller? _pendingCaller;

		private bool _nodesInitialized;
		private Caller? _previousCaller;

		public override void _Ready()
		{
			EnsureNodesInitialized();
			_nodesInitialized = _headerLabel != null && _callerInfoLabel != null &&
			                   _approveButton != null && _rejectButton != null;
			InitializeController();
			if (_approveButton != null && _rejectButton != null)
			{
				ConnectSignals();
			}
			else
			{
				GD.PrintErr("ScreeningPanel: Skipping ConnectSignals() due to missing node references");
			}
			ValidateNodeReferences();
		}

		private void InitializeController()
		{
			_controller = Core.ServiceRegistry.Instance.ScreeningController;
		}

		private void ConnectSignals()
		{
			if (_approveButton == null)
			{
				GD.PrintErr("ScreeningPanel: _approveButton is null - EnsureNodesInitialized failed to find node");
				return;
			}
			if (_rejectButton == null)
			{
				GD.PrintErr("ScreeningPanel: _rejectButton is null - EnsureNodesInitialized failed to find node");
				return;
			}

			_approveButton!.Pressed += OnApprovePressed;
			_rejectButton!.Pressed += OnRejectPressed;
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

		public override void _Process(double delta)
		{
			var currentCaller = _controller.CurrentCaller;
			if (currentCaller != _previousCaller)
			{
				_previousCaller = currentCaller;
				SetCaller(currentCaller);
				UpdateButtons();
			}
			UpdatePatienceDisplay(_controller.Progress);
		}

		public void SetCaller(Caller? caller)
		{
			_pendingCaller = caller;
			if (_nodesInitialized)
			{
				_SetCallerImmediate(_pendingCaller);
			}
			else
			{
				CallDeferred(nameof(_ApplyCallerDeferred));
			}
		}

		private void _ApplyCallerDeferred()
		{
			EnsureNodesInitialized();
			_SetCallerImmediate(_pendingCaller);
		}

		private void _SetCallerImmediate(Caller? caller)
		{
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
			if (_controller.CurrentCaller == null)
			{
				GD.Print("ScreeningPanel: Approve pressed but no caller is being screened - ignoring");
				return;
			}

			var result = _controller.Approve();
			if (!result.IsSuccess)
			{
				GD.PrintErr($"ScreeningPanel: Approve failed - {result.ErrorCode}: {result.ErrorMessage}");
			}
		}

		private void OnRejectPressed()
		{
			if (_controller.CurrentCaller == null)
			{
				GD.Print("ScreeningPanel: Reject pressed but no caller is being screened - ignoring");
				return;
			}

			var result = _controller.Reject();
			if (!result.IsSuccess)
			{
				GD.PrintErr($"ScreeningPanel: Reject failed - {result.ErrorCode}: {result.ErrorMessage}");
			}
		}

		private void UpdateButtons()
		{
			bool hasCaller = _controller.CurrentCaller != null;
			_approveButton!.Disabled = !hasCaller;
			_rejectButton!.Disabled = !hasCaller;

			// Approve button style
			var approveStyle = new StyleBoxFlat();
			approveStyle.BgColor = hasCaller ? UIColors.Button.Approve : UIColors.BG_DISABLED;
			approveStyle.CornerRadiusTopLeft = 8;
			approveStyle.CornerRadiusTopRight = 8;
			approveStyle.CornerRadiusBottomLeft = 8;
			approveStyle.CornerRadiusBottomRight = 8;
			approveStyle.ContentMarginLeft = 20;
			approveStyle.ContentMarginRight = 20;
			approveStyle.ContentMarginTop = 12;
			approveStyle.ContentMarginBottom = 12;
			_approveButton.AddThemeStyleboxOverride("normal", approveStyle);
			_approveButton.AddThemeColorOverride("font_color", UIColors.Button.ApproveText);

			// Reject button style
			var rejectStyle = new StyleBoxFlat();
			rejectStyle.BgColor = hasCaller ? UIColors.Button.Reject : UIColors.BG_DISABLED;
			rejectStyle.CornerRadiusTopLeft = 8;
			rejectStyle.CornerRadiusTopRight = 8;
			rejectStyle.CornerRadiusBottomLeft = 8;
			rejectStyle.CornerRadiusBottomRight = 8;
			rejectStyle.ContentMarginLeft = 20;
			rejectStyle.ContentMarginRight = 20;
			rejectStyle.ContentMarginTop = 12;
			rejectStyle.ContentMarginBottom = 12;
			_rejectButton.AddThemeStyleboxOverride("normal", rejectStyle);
			_rejectButton.AddThemeColorOverride("font_color", UIColors.Button.RejectText);
		}

		private void UpdatePatienceDisplay(ScreeningProgress progress)
		{
			if (_patienceBar != null)
			{
				_patienceBar.Value = progress.ProgressPercent;
				var fillStyle = new StyleBoxFlat { BgColor = UIColors.GetPatienceColor(progress.ProgressPercent) };
				_patienceBar.AddThemeStyleboxOverride("fill", fillStyle);
			}

			if (_patienceLabel != null)
			{
				_patienceLabel.Text = $"Patience: {progress.PatienceRemaining:F1}s / {progress.MaxPatience:F1}s";
			}
		}

		private string BuildCallerInfoText(Caller caller)
		{
			var sb = new System.Text.StringBuilder();

			// Header row: Name | Phone | Location | Topic
			sb.Append($"Name: {caller.Name}");
			sb.Append($"  |  Phone: {caller.PhoneNumber}");
			sb.Append($"  |  Location: {caller.Location}");
			sb.AppendLine($"  |  Topic: {caller.ClaimedTopic}");

			// Divider line
			sb.AppendLine(new string('─', 60));

			// All properties as simple "Property: Value" lines
			sb.AppendLine($"Quality: {caller.PhoneQuality}");
			sb.AppendLine($"Emotional State: {caller.EmotionalState}");
			sb.AppendLine($"Curse Risk: {caller.CurseRisk}");
			sb.AppendLine($"Belief Level: {caller.BeliefLevel}");
			sb.AppendLine($"Evidence: {caller.EvidenceLevel}");
			sb.AppendLine($"Coherence: {caller.Coherence}");
			sb.AppendLine($"Urgency: {caller.Urgency}");
			sb.AppendLine($"Legitimacy: {caller.Legitimacy}");
			sb.AppendLine($"Personality: {caller.Personality}");

			if (!string.IsNullOrEmpty(caller.ScreeningSummary))
			{
				sb.AppendLine();
				sb.AppendLine(caller.ScreeningSummary);
			}

			return sb.ToString();
		}

		public void ConnectButtons(Callable approveCallable, Callable rejectCallable)
		{
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
			GD.Print("ScreeningPanel: Cleanup complete");
		}

		private void EnsureNodesInitialized()
		{
			if (_headerLabel == null)
			{
				try
				{
					_headerLabel = GetNode<Label>("VBoxContainer/HeaderLabel");
				}
				catch (Exception ex)
				{
					GD.PrintErr($"ScreeningPanel: Failed to find HeaderLabel at VBoxContainer/HeaderLabel: {ex.Message}");
				}
			}
			if (_callerInfoLabel == null)
			{
				try
				{
					_callerInfoLabel = GetNode<Label>("VBoxContainer/CallerInfoScroll/CallerInfoLabel");
				}
				catch (Exception ex)
				{
					GD.PrintErr($"ScreeningPanel: Failed to find CallerInfoLabel at VBoxContainer/CallerInfoScroll/CallerInfoLabel: {ex.Message}");
				}
			}
			if (_approveButton == null)
			{
				try
				{
					_approveButton = GetNode<Button>("VBoxContainer/HBoxContainer/ApproveButton");
				}
				catch (Exception ex)
				{
					GD.PrintErr($"ScreeningPanel: Failed to find ApproveButton at VBoxContainer/HBoxContainer/ApproveButton: {ex.Message}");
				}
			}
			if (_rejectButton == null)
			{
				try
				{
					_rejectButton = GetNode<Button>("VBoxContainer/HBoxContainer/RejectButton");
				}
				catch (Exception ex)
				{
					GD.PrintErr($"ScreeningPanel: Failed to find RejectButton at VBoxContainer/HBoxContainer/RejectButton: {ex.Message}");
				}
			}
		}
	}
}
