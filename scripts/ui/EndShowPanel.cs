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
		[Export] private Button EndShowButton = null!;

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

			// Subscribe to AdManager event
			ServiceRegistry.Instance.AdManager.LastSegmentStarted += ShowPanel;

			_endShowButton.Pressed += OnEndShowPressed;

			// Hide initially
			Visible = false;
		}

		private void ShowPanel()
		{
			// Hide ad break panel and show this one
			var adBreakPanel = GetParent().GetNode<Control>("AdBreakPanel");
			if (adBreakPanel != null)
			{
				adBreakPanel.Visible = false;
			}
			Visible = true;
		}

		private void OnEndShowPressed()
		{
			_endShowButton.Disabled = true;
			_endShowButton.Text = "ENDING...";

			// Queue the show end
			_coordinator.QueueShowEnd();
		}

		public override void _ExitTree()
		{
			if (ServiceRegistry.IsInitialized && ServiceRegistry.Instance.AdManager != null)
			{
				ServiceRegistry.Instance.AdManager.LastSegmentStarted -= ShowPanel;
			}
		}
	}
}
