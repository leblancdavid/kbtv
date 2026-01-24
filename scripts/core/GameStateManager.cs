using System;
using Godot;
using KBTV.Ads;
using KBTV.Data;
using KBTV.Dialogue;
using KBTV.Economy;
using KBTV.Managers;
using KBTV.Persistence;
using KBTV.Callers;
using static KBTV.Dialogue.BroadcastCoordinator;

namespace KBTV.Core
{
	/// <summary>
	/// Manages the game state and phase transitions for nightly broadcasts.
	/// </summary>
	public partial class GameStateManager : Node
	{
		[Signal] public delegate void PhaseChangedEventHandler(GamePhase oldPhase, GamePhase newPhase);
	[Signal] public delegate void NightStartedEventHandler(int nightNumber);

	// ═══════════════════════════════════════════════════════════════════════════════════════════════
		// FIELDS
		// ═══════════════════════════════════════════════════════════════════════════════

		private GamePhase _currentPhase = GamePhase.PreShow;
		private VernStats _vernStats;
		private int _currentNight = 1;
		private Topic _selectedTopic;
		private AdSchedule _adSchedule;
		private static int _instanceCount;
		private int _instanceId;

		public VernStats VernStats => _vernStats;
		public GamePhase CurrentPhase => _currentPhase;
		public int CurrentNight => _currentNight;
		public Topic SelectedTopic => _selectedTopic;
		public AdSchedule AdSchedule => _adSchedule;

		public override void _Ready()
		{
			_instanceId = ++_instanceCount;
			GD.Print($"GameStateManager: _Ready called (instance #{_instanceId})");
			ServiceRegistry.Instance.RegisterSelf<GameStateManager>(this);

			_vernStats = new VernStats();
			_adSchedule = new AdSchedule(AdConstants.DEFAULT_BREAKS_PER_SHOW, AdConstants.DEFAULT_SLOTS_PER_BREAK);
			InitializeGame();

			// Connect to timer expiration for automatic end-of-show
			var timeManager = ServiceRegistry.Instance.TimeManager;
			if (timeManager != null)
			{
				timeManager.ShowEnded += OnShowTimerExpired;
			}
		}

		/// <summary>
		/// Initialize the game state. VernStats is created automatically.
		/// </summary>
		public void InitializeGame()
		{
			if (_vernStats != null)
			{
				_vernStats.Initialize();
			}
			else
			{
				GD.PrintErr("GameStateManager: Failed to create VernStats!");
			}
		}

		/// <summary>
		/// Transition to the next phase in the nightly cycle.
		/// </summary>
		public void AdvancePhase()
		{
			GamePhase oldPhase = _currentPhase;

			switch (_currentPhase)
			{
				case GamePhase.PreShow:
					_currentPhase = GamePhase.LiveShow;
					break;

				case GamePhase.LiveShow:
					_currentPhase = GamePhase.PostShow;
					ProcessEndOfShow();
					return; // ProcessEndOfShow handles phase changed signal after bumper

				case GamePhase.PostShow:
					// Start a new night
					StartNewNight();
					return;
			}

			EmitSignal("PhaseChanged", (int)oldPhase, (int)_currentPhase);
		}

		/// <summary>
		/// Start the live show phase if a topic is selected.
		/// </summary>
		public void StartLiveShow()
		{
			GD.Print("DEBUG: GameStateManager.StartLiveShow called");
			if (!CanStartLiveShow())
			{
				GD.PrintErr("GameStateManager: Cannot start live show - invalid state or no topic selected");
				return;
			}

			// GD.Print($"GameStateManager: Starting live show with topic '{_selectedTopic.DisplayName}'");
			GamePhase oldPhase = _currentPhase;
			_currentPhase = GamePhase.LiveShow;
			EmitSignal("PhaseChanged", (int)oldPhase, (int)_currentPhase);

			// Start the show clock
			ServiceRegistry.Instance.TimeManager?.StartClock();

			// Initialize ad manager with schedule
			var adManager = ServiceRegistry.Instance.AdManager;
			var timeManager = ServiceRegistry.Instance.TimeManager;
			if (adManager != null && timeManager != null)
			{
				adManager.Initialize(_adSchedule, timeManager.ShowDuration);
			}
			else
			{
				GD.PrintErr("GameStateManager: AdManager or TimeManager not available");
			}

			// Initialize broadcast flow
			var coordinator = ServiceRegistry.Instance.BroadcastCoordinator;
			GD.Print($"DEBUG: GameStateManager checking BroadcastCoordinator: {coordinator != null}");
			if (coordinator != null)
			{
				GD.Print("DEBUG: GameStateManager calling OnLiveShowStarted");
				coordinator.OnLiveShowStarted();
			}
			else
			{
				GD.PrintErr("DEBUG: BroadcastCoordinator is null - not registered in ServiceRegistry");
			}
		}

		/// <summary>
		/// Directly set the game phase (useful for testing/debugging).
		/// </summary>
		public void SetPhase(GamePhase phase)
		{
			var oldPhase = _currentPhase;
			_currentPhase = phase;
			EmitSignal("PhaseChanged", (int)oldPhase, (int)phase);
		}

    	/// <summary>
    	/// Set the selected topic for the current show.
    	/// </summary>
    	public void SetSelectedTopic(Topic topic)
    	{
    		_selectedTopic = topic;
    		// GD.Print($"GameStateManager: Topic selected - {topic?.DisplayName ?? "None"}");
    	}

		/// <summary>
		/// Set the ad schedule for the current show.
		/// </summary>
		public void SetAdSchedule(AdSchedule schedule)
		{
			_adSchedule = schedule;
		}

		/// <summary>
		/// Check if the game is ready to start live show.
		/// </summary>
		public bool CanStartLiveShow()
		{
			return _currentPhase == GamePhase.PreShow && _selectedTopic != null;
		}



		/// <summary>
		/// Start a new night, resetting stats and returning to PreShow.
		/// </summary>
		public void StartNewNight()
		{
			_currentNight++;

			GamePhase oldPhase = _currentPhase;
			_currentPhase = GamePhase.PreShow;

			// Re-initialize Vern's stats for the new night
			if (_vernStats != null)
			{
				_vernStats.Initialize();
			}

			EmitSignal("PhaseChanged", (int)oldPhase, (int)_currentPhase);
			EmitSignal("NightStarted", _currentNight);
		}

		/// <summary>
		/// Check if we're currently in the live broadcast phase.
		/// </summary>
		public bool IsLive => _currentPhase == GamePhase.LiveShow;

		/// <summary>
		/// Process end-of-show logic: calculate income, update stats, save game.
		/// Called at T=0 when show time expires.
		/// </summary>
		private async void ProcessEndOfShow()
		{
			GD.Print("GameStateManager: ProcessEndOfShow started (T=0)");

			// The closing dialog was already triggered at T-10s by OnShowEndingWarning event
			// and should have completed by now. We just need to:
			// 1. Clear all callers
			// 2. Calculate income and update save data
			// 3. Play outro music if queued
			// 4. Show PostShow UI

			// Clear all callers (incoming, on-hold, on-air)
			ServiceRegistry.Instance.CallerRepository?.ClearAll();
			GD.Print("GameStateManager: All callers cleared");

			// Get show performance data
			int peakListeners = ServiceRegistry.Instance.ListenerManager?.PeakListeners ?? 0;

			float showQuality = _vernStats?.CalculateVIBE() ?? 50f;

			// Calculate and award income
			int income = IncomeCalculator.CalculateShowIncome(peakListeners, showQuality);
			if (ServiceRegistry.Instance.EconomyManager != null)
			{
				ServiceRegistry.Instance.EconomyManager.AddMoney(income, "Show Income");
				GD.Print($"GameStateManager: Added income {income} (quality: {showQuality:F1}, peak listeners: {peakListeners})");
			}

			// Update save data
			if (ServiceRegistry.Instance.SaveManager?.CurrentSave != null)
			{
				var save = ServiceRegistry.Instance.SaveManager.CurrentSave;
				save.TotalShowsCompleted++;
				if (peakListeners > save.PeakListenersAllTime)
				{
					save.PeakListenersAllTime = peakListeners;
				}
				save.CurrentNight = _currentNight;
			}

			// Reset the clock for next show
			ServiceRegistry.Instance.TimeManager?.ResetClock();

			// Play outro music if it was queued by user clicking "End Show" button
			var broadcastCoordinator = ServiceRegistry.Instance.BroadcastCoordinator;
			if (broadcastCoordinator != null && broadcastCoordinator.IsOutroMusicQueued)
			{
				GD.Print("GameStateManager: Playing queued outro music");
				var audioPlayer = ServiceRegistry.Instance.EventBus as IDialoguePlayer;
				if (audioPlayer != null)
				{
					var bumperLine = BroadcastLine.Music();
					audioPlayer.PlayLineAsync(bumperLine);

					// Wait for bumper to complete (4 seconds for music lines)
					await ToSignal(GetTree().CreateTimer(4f), "timeout");
					GD.Print("GameStateManager: Outro music completed");
				}
			}

			// Auto-save at end of show
			if (ServiceRegistry.Instance.SaveManager != null)
			{
				ServiceRegistry.Instance.SaveManager.Save();
				GD.Print("GameStateManager: Game saved");
			}

			// Show PostShow UI
			ServiceRegistry.Instance.UIManager?.ShowPostShowLayer();
			GD.Print("GameStateManager: PostShow UI shown");

			// Emit phase changed signal
			EmitSignal("PhaseChanged", (int)GamePhase.LiveShow, (int)GamePhase.PostShow);
		}

		/// <summary>
		/// Handle automatic show end when timer expires at T=0.
		/// Interrupts current audio, clears callers, plays outro music, then advances to PostShow.
		/// </summary>
		private async void OnShowTimerExpired()
		{
			GD.Print("GameStateManager: OnShowTimerExpired - Timer reached 0, ending show");

			// Interrupt current audio playback
			var audioPlayer = ServiceRegistry.Instance.AudioPlayer;
			if (audioPlayer != null)
			{
				audioPlayer.Stop();
				GD.Print("GameStateManager: Current audio interrupted");
			}

			// Clear all callers and stop generation
			ServiceRegistry.Instance.CallerRepository?.ClearAll();
			ServiceRegistry.Instance.CallerGenerator?.StopGenerating();
			GD.Print("GameStateManager: All callers cleared and generation stopped");

			// Play outro bumper music
			var dialoguePlayer = ServiceRegistry.Instance.EventBus as IDialoguePlayer;
			if (dialoguePlayer != null)
			{
				var outroLine = BroadcastLine.OutroMusic();
				GD.Print("GameStateManager: Playing outro music");

				// Subscribe to completion event
				void OnOutroCompleted(AudioCompletedEvent @event)
				{
					if (@event.LineId == outroLine.SpeakerId)
					{
						GD.Print("GameStateManager: Outro music completed, advancing to PostShow");
						ServiceRegistry.Instance.EventBus.Unsubscribe<AudioCompletedEvent>(OnOutroCompleted);
						AdvanceToPostShow();
					}
				}

				ServiceRegistry.Instance.EventBus.Subscribe<AudioCompletedEvent>(OnOutroCompleted);
				dialoguePlayer.PlayLineAsync(outroLine);
			}
			else
			{
				// Fallback if no dialogue player
				GD.Print("GameStateManager: No dialogue player available, advancing immediately");
				await ToSignal(GetTree().CreateTimer(4f), "timeout");
				AdvanceToPostShow();
			}
		}

		/// <summary>
		/// Advance to PostShow phase after outro music completion.
		/// </summary>
		private void AdvanceToPostShow()
		{
			GamePhase oldPhase = _currentPhase;
			_currentPhase = GamePhase.PostShow;
			ProcessEndOfShow();
			EmitSignal("PhaseChanged", (int)oldPhase, (int)_currentPhase);
		}
	}
}
