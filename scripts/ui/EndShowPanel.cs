using Godot;
using KBTV.Core;
using KBTV.Dialogue;
using KBTV.Managers;
using KBTV.Audio;

namespace KBTV.UI
{
	/// <summary>
	/// Panel that appears after the last ad break, allowing the host to end the show.
	/// </summary>
	public partial class EndShowPanel : Control, IDependent
	{
		private Button EndShowButton;
		private BroadcastCoordinator _coordinator = null!;
		private TimeManager? _timeManager;
		private IBroadcastAudioService _broadcastAudioService = null!;

		public override void _Notification(int what) => this.Notify(what);


		public void OnResolved()
		{
			_coordinator = DependencyInjection.Get<BroadcastCoordinator>(this);
			_timeManager = DependencyInjection.Get<TimeManager>(this);
			_broadcastAudioService = DependencyInjection.Get<IBroadcastAudioService>(this);
			if (_coordinator == null)
			{
				GD.PrintErr("EndShowPanel: BroadcastCoordinator not available");
				return;
			}

			// Hide initially
			Visible = false;

			EndShowButton = GetNode<Button>("VBoxContainer/EndShowButton");
			if (EndShowButton == null)
			{
				GD.PrintErr("EndShowPanel: EndShowButton not found");
				return;
			}

			// Start with button disabled until T-20s
			EndShowButton.Disabled = true;
			EndShowButton.Pressed += OnEndShowPressed;
		}

		public override void _Process(double delta)
		{
			// Enable button when we're in the last 20 seconds
			if (EndShowButton != null && EndShowButton.Disabled)
			{
				if (_timeManager != null && _timeManager.RemainingTime <= 20f)
				{
					EndShowButton.Disabled = false;
				}
			}
		}


		private void OnEndShowPressed()
		{
			// Play transition music as audio cue (same as ad breaks)
			_ = _broadcastAudioService.PlaySilentAudioAsync();

			EndShowButton.Disabled = true;
			EndShowButton.Text = "ENDING...";

			// Queue the show end
			_coordinator.QueueShowEnd();
		}


	}
}
