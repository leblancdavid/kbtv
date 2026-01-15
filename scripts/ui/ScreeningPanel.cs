using Godot;
using KBTV.Callers;

namespace KBTV.UI
{
    public partial class ScreeningPanel : Control
	{
		private Label _headerLabel;
		private Label _callerInfoLabel;
		private Button _approveButton;
		private Button _rejectButton;

		public override void _Ready()
		{
			EnsureNodesInitialized();
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

		public void SetCaller(Caller caller)
		{
			EnsureNodesInitialized();
			if (caller != null)
			{
				_callerInfoLabel.Text = BuildCallerInfoText(caller);
			}
			else
			{
				_callerInfoLabel.Text = "No caller\ncurrently\nscreening";
			}
		}

		private string BuildCallerInfoText(Caller caller)
		{
			var sb = new System.Text.StringBuilder();

			// Basic Info
			sb.AppendLine($"Name: {caller.Name}");
			sb.AppendLine($"Location: {caller.Location}");
			sb.AppendLine($"Topic: {caller.ClaimedTopic}");
			sb.AppendLine();

			// Audio & Emotional
			sb.AppendLine("AUDIO & EMOTIONAL:");
			sb.AppendLine($"• Quality: {caller.PhoneQuality}");
			sb.AppendLine($"• Emotional State: {caller.EmotionalState}");
			sb.AppendLine($"• Curse Risk: {caller.CurseRisk}");
			sb.AppendLine();

			// Assessment
			sb.AppendLine("ASSESSMENT:");
			sb.AppendLine($"• Belief Level: {caller.BeliefLevel}");
			sb.AppendLine($"• Evidence: {caller.EvidenceLevel}");
			sb.AppendLine($"• Coherence: {caller.Coherence}");
			sb.AppendLine($"• Urgency: {caller.Urgency}");
			sb.AppendLine();

			// Overall
			sb.AppendLine("OVERALL:");
			sb.AppendLine($"• Legitimacy: {caller.Legitimacy}");
			sb.AppendLine($"• Personality: {caller.Personality}");
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
	}
}
