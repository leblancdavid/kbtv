using Godot;
using KBTV.Core;

namespace KBTV.UI
{
	/// <summary>
	/// Post-show summary panel displayed after the broadcast ends.
	/// Shows placeholder stats and allows player to continue to the next night.
	/// </summary>
	public partial class PostShowPanel : Control
	{
		private Label _incomeLabel = null!;
		private Label _callersLabel = null!;
		private Label _listenersLabel = null!;
		private Button _continueButton = null!;

		public override void _Ready()
		{
			_incomeLabel = GetNode<Label>("VBoxContainer/StatsContainer/IncomeLabel");
			_callersLabel = GetNode<Label>("VBoxContainer/StatsContainer/CallersLabel");
			_listenersLabel = GetNode<Label>("VBoxContainer/StatsContainer/ListenersLabel");
			_continueButton = GetNode<Button>("VBoxContainer/ContinueButton");

			if (_continueButton == null)
			{
				GD.PrintErr("PostShowPanel: ContinueButton not found");
				return;
			}

			_continueButton.Pressed += OnContinuePressed;

			UpdateStats();
		}

		private void UpdateStats()
		{
			var economyManager = ServiceRegistry.Instance.EconomyManager;
			var listenerManager = ServiceRegistry.Instance.ListenerManager;

			int income = economyManager?.CurrentMoney ?? 0;
			int peakListeners = listenerManager?.PeakListeners ?? 0;

			if (_incomeLabel != null)
			{
				_incomeLabel.Text = $"Income: ${income}";
			}

			if (_callersLabel != null)
			{
				_callersLabel.Text = $"Show Complete!";
			}

			if (_listenersLabel != null)
			{
				_listenersLabel.Text = $"Peak Listeners: {peakListeners}";
			}
		}

		private void OnContinuePressed()
		{
			// Advance to next night (returns to PreShow)
			ServiceRegistry.Instance.GameStateManager?.AdvancePhase();
		}
	}
}
