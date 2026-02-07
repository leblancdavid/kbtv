using Godot;
using KBTV.Core;
using KBTV.Dialogue;
using KBTV.Managers;
using KBTV.Audio;
using KBTV.UI.Themes;

namespace KBTV.UI
{
	/// <summary>
	/// Panel that appears after the last ad break, allowing the host to end the show.
	/// </summary>
	public partial class EndShowPanel : Control, IDependent
	{
		private Button EndShowButton;
		private AsyncBroadcastLoop _asyncLoop = null!;
		private TimeManager? _timeManager;
		private IBroadcastAudioService _broadcastAudioService = null!;

		public override void _Notification(int what) => this.Notify(what);


		public void OnResolved()
		{
			_asyncLoop = DependencyInjection.Get<AsyncBroadcastLoop>(this);
			_timeManager = DependencyInjection.Get<TimeManager>(this);
			_broadcastAudioService = DependencyInjection.Get<IBroadcastAudioService>(this);
			if (_asyncLoop == null)
			{
				Log.Error("EndShowPanel: AsyncBroadcastLoop not available");
				return;
			}

			// Hide initially
			Visible = false;

			EndShowButton = GetNode<Button>("VBoxContainer/EndShowButton");
			if (EndShowButton == null)
			{
				Log.Error("EndShowPanel: EndShowButton not found");
				return;
			}

			// Start with button disabled until T-20s
			EndShowButton.Disabled = true;
			EndShowButton.Pressed += OnEndShowPressed;

			// Apply initial styling and text
			UpdateButtonStyling();
			UpdateButtonText();
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

			// Update button text with timer
			UpdateButtonText();

			// Update button styling based on current state
			UpdateButtonStyling();
		}


		private void UpdateButtonText()
		{
			if (EndShowButton == null || _timeManager == null) return;

			if (EndShowButton.Text == "ENDING...")
			{
				// Keep "ENDING..." text during transition
				return;
			}

			string timeText = _timeManager.RemainingTimeFormatted;

			if (EndShowButton.Disabled)
			{
				// Show time until button enables
				EndShowButton.Text = $"WAITING\n{timeText}";
			}
			else
			{
				// Show remaining time to end show
				EndShowButton.Text = $"END SHOW\n{timeText}";
			}
		}

		private void OnEndShowPressed()
		{
			// Play transition music as audio cue (same as ad breaks)
			_ = _broadcastAudioService.PlaySilentAudioAsync();

			EndShowButton.Disabled = true;
			EndShowButton.Text = "ENDING...";

			// Update styling immediately for pressed state
			UpdateButtonStyling();


		}

		private void UpdateButtonStyling()
		{
			if (EndShowButton == null) return;

			// Determine the appropriate color based on button state
			Color bgColor, borderColor;
			if (EndShowButton.Text == "ENDING...")
			{
				// Gold when ending (pressed state)
				bgColor = UIColors.Accent.Gold;
				borderColor = UIColors.Accent.Gold;
			}
			else if (!EndShowButton.Disabled)
			{
				// Green when enabled (ready to end show)
				bgColor = UIColors.Accent.Green;
				borderColor = UIColors.Accent.Green;
			}
			else
			{
				// Red when disabled (waiting for end show window)
				bgColor = UIColors.Accent.Red;
				borderColor = UIColors.Accent.Red;
			}

			// Create StyleBoxFlat objects for each state
			var styleBoxNormal = new StyleBoxFlat();
			var styleBoxDisabled = new StyleBoxFlat();
			var styleBoxPressed = new StyleBoxFlat();

			// Normal state: full brightness
			styleBoxNormal.BgColor = bgColor;
			styleBoxNormal.BorderColor = borderColor;
			styleBoxNormal.BorderWidthTop = 2;
			styleBoxNormal.BorderWidthBottom = 2;
			styleBoxNormal.BorderWidthLeft = 2;
			styleBoxNormal.BorderWidthRight = 2;

			// Disabled state: dimmed
			styleBoxDisabled.BgColor = new Color(bgColor.R, bgColor.G, bgColor.B, 0.6f);
			styleBoxDisabled.BorderColor = new Color(borderColor.R, borderColor.G, borderColor.B, 0.6f);
			styleBoxDisabled.BorderWidthTop = 2;
			styleBoxDisabled.BorderWidthBottom = 2;
			styleBoxDisabled.BorderWidthLeft = 2;
			styleBoxDisabled.BorderWidthRight = 2;

			// Pressed state: full brightness
			styleBoxPressed.BgColor = bgColor;
			styleBoxPressed.BorderColor = borderColor;
			styleBoxPressed.BorderWidthTop = 2;
			styleBoxPressed.BorderWidthBottom = 2;
			styleBoxPressed.BorderWidthLeft = 2;
			styleBoxPressed.BorderWidthRight = 2;

			// Apply the theme overrides
			EndShowButton.AddThemeStyleboxOverride("normal", styleBoxNormal);
			EndShowButton.AddThemeStyleboxOverride("disabled", styleBoxDisabled);
			EndShowButton.AddThemeStyleboxOverride("pressed", styleBoxPressed);
		}


	}
}
