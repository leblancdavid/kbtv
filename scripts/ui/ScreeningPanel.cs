using Godot;
using KBTV.Callers;

namespace KBTV.UI
{
	public partial class ScreeningPanel : Panel
	{
		private Label _headerLabel;
		private Label _callerInfoLabel;
		private Button _approveButton;
		private Button _rejectButton;

		public override void _Ready()
		{
			_headerLabel = GetNode<Label>("VBoxContainer/HeaderLabel");
			_callerInfoLabel = GetNode<Label>("VBoxContainer/CallerInfoLabel");
			_approveButton = GetNode<Button>("VBoxContainer/HBoxContainer/ApproveButton");
			_rejectButton = GetNode<Button>("VBoxContainer/HBoxContainer/RejectButton");
		}

		public void SetCaller(Caller caller)
		{
			if (caller != null)
			{
				_callerInfoLabel.Text = $"{caller.Name}\n{caller.Location}\nTopic: {caller.ClaimedTopic}";
			}
			else
			{
				_callerInfoLabel.Text = "No caller\ncurrently\nscreening";
			}
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
	}
}
