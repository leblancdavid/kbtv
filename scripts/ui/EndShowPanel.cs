using Godot;
using KBTV.Core;
using KBTV.Dialogue;

namespace KBTV.UI
{
	/// <summary>
	/// Panel that appears after the last ad break, allowing the host to end the show.
	/// </summary>
	public partial class EndShowPanel : Control
	{
		private Button EndShowButton;

		private BroadcastCoordinator _coordinator = null!;

		public override void _Ready()
		{
			CallDeferred(nameof(InitializeDeferred));
		}

		private void InitializeDeferred()
		{
			if (!ServiceRegistry.IsInitialized)
			{
				CallDeferred(nameof(InitializeDeferred));
				return;
			}

			_coordinator = ServiceRegistry.Instance.BroadcastCoordinator;
			if (_coordinator == null)
			{
				CallDeferred(nameof(InitializeDeferred));
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

			EndShowButton.Pressed += OnEndShowPressed;
		}



		private void OnEndShowPressed()
		{
			EndShowButton.Disabled = true;
			EndShowButton.Text = "ENDING...";

			// Queue the show end
			_coordinator.QueueShowEnd();
		}


	}
}
