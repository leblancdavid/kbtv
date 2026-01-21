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
		private AudioStreamPlayer _transitionMusicPlayer = null!;

		private BroadcastCoordinator _coordinator = null!;

		public override void _Ready()
		{
			// Initialize audio player for transition music
			_transitionMusicPlayer = new AudioStreamPlayer();
			AddChild(_transitionMusicPlayer);

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

			// Start with button disabled until T-20s
			EndShowButton.Disabled = true;
			EndShowButton.Pressed += OnEndShowPressed;
		}

		public override void _Process(double delta)
		{
			// Enable button when we're in the last 20 seconds
			if (EndShowButton != null && EndShowButton.Disabled)
			{
				var timeManager = ServiceRegistry.Instance.TimeManager;
				if (timeManager != null && timeManager.RemainingTime <= 20f)
				{
					EndShowButton.Disabled = false;
				}
			}
		}


		private void OnEndShowPressed()
		{
			// Play transition music as audio cue (same as ad breaks)
			var silentStream = GetSilentAudioFile();
			if (silentStream != null)
			{
				_transitionMusicPlayer.Stream = silentStream;
				_transitionMusicPlayer.Play();
			}
			else
			{
				GD.PrintErr("EndShowPanel: Failed to load silent audio file for transition music");
			}

			EndShowButton.Disabled = true;
			EndShowButton.Text = "ENDING...";

			// Queue the show end
			_coordinator.QueueShowEnd();
		}

		private AudioStream? GetSilentAudioFile()
		{
			var audioStream = GD.Load<AudioStream>("res://assets/audio/silence_4sec.wav");
			if (audioStream == null)
			{
				GD.PrintErr("EndShowPanel.GetSilentAudioFile: Failed to load silent audio file");
				return null;
			}
			return audioStream;
		}


	}
}
