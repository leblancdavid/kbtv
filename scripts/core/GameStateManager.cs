using System;
using System.Threading.Tasks;
using Godot;
using KBTV.Ads;
using KBTV.Audio;
using KBTV.Data;
using KBTV.Dialogue;
using KBTV.Economy;
using KBTV.Managers;
using KBTV.Persistence;
using KBTV.Callers;
using KBTV.UI;

namespace KBTV.Core
{
	/// <summary>
	/// Manages the game state and phase transitions for nightly broadcasts.
	/// Uses AutoInject IAutoNode pattern for dependency injection.
	/// </summary>
    public partial class GameStateManager : Node, 
        IGameStateManager,
        IProvide<GameStateManager>,
        IDependent
	{
		public override void _Notification(int what) => this.Notify(what);

		[Signal] public delegate void PhaseChangedEventHandler(GamePhase oldPhase, GamePhase newPhase);
		[Signal] public delegate void NightStartedEventHandler(int nightNumber);

		public event Action<GamePhase, GamePhase> OnPhaseChanged;
		public event Action<int> OnNightStarted;

		// ═══════════════════════════════════════════════════════════════════════════════════════════════
		// DEPENDENCIES
		// ═══════════════════════════════════════════════════════════════════════════════════════════════

        private TimeManager TimeManager => DependencyInjection.Get<TimeManager>(this);
        private EconomyManager EconomyManager => DependencyInjection.Get<EconomyManager>(this);
        private SaveManager SaveManager => DependencyInjection.Get<SaveManager>(this);
        private ICallerRepository CallerRepository => DependencyInjection.Get<ICallerRepository>(this);
        private IUIManager UIManager => DependencyInjection.Get<IUIManager>(this);
        private AsyncBroadcastLoop AsyncBroadcastLoop => DependencyInjection.Get<AsyncBroadcastLoop>(this);
        private AdManager AdManager => DependencyInjection.Get<AdManager>(this);
        private CallerGenerator CallerGenerator => DependencyInjection.Get<CallerGenerator>(this);
        private IBroadcastAudioService AudioPlayer => DependencyInjection.Get<IBroadcastAudioService>(this);
        private ListenerManager ListenerManager => DependencyInjection.Get<ListenerManager>(this);
        private EventBus EventBus => DependencyInjection.Get<EventBus>(this);

		private GamePhase _currentPhase = GamePhase.Loading;
        private VernStats _vernStats;
        private int _currentNight = 1;
        private Topic _selectedTopic;
        private AdSchedule _adSchedule;
        private static int _instanceCount;
        private int _instanceId;
        private bool _gameInitialized;

		public VernStats VernStats => _vernStats;
		public GamePhase CurrentPhase => _currentPhase;
		public int CurrentNight => _currentNight;
		public Topic SelectedTopic => _selectedTopic;
		public AdSchedule AdSchedule => _adSchedule;

		/// <summary>
		/// Whether broadcast audio is disabled (uses 4-second timeouts instead).
		/// </summary>
		public bool DisableBroadcastAudio => DependencyInjection.Get<SaveManager>(this)?.CurrentSave?.DisableBroadcastAudio ?? false;

		// ═══════════════════════════════════════════════════════════════════════════════════════════════
		// PROVIDER INTERFACE IMPLEMENTATIONS
		// ═══════════════════════════════════════════════════════════════════════════════════════════════

		// ═══════════════════════════════════════════════════════════════════════════════════════════════
		// PROVIDER INTERFACE IMPLEMENTATIONS
		// ═══════════════════════════════════════════════════════════════════════════════════════════════

        GameStateManager IProvide<GameStateManager>.Value() => this;

		/// <summary>
		/// Called when node enters the scene tree and is ready.
		/// Makes services available to descendants.
		/// </summary>
		public void OnReady() => this.Provide();

		/// <summary>
		/// Called when all dependencies are resolved.
		/// </summary>
		public void OnResolved()
		{
			GD.Print("GameStateManager: Dependencies resolved, initializing...");
			_instanceId = ++_instanceCount;

			_vernStats = new VernStats();
			_adSchedule = new AdSchedule(AdConstants.DEFAULT_BREAKS_PER_SHOW, AdConstants.DEFAULT_SLOTS_PER_BREAK);
			InitializeGame();

			// Connect to TimeManager event
            TimeManager.OnShowEnded += OnShowTimerExpired;
		}

        /// <summary>
        /// Initialize the game state. VernStats is created automatically.
        /// </summary>
        public void InitializeGame()
        {
            if (_gameInitialized) return;

            if (_vernStats != null)
            {
                _vernStats.Initialize();
            }
            else
            {
                GD.PrintErr("GameStateManager: Failed to create VernStats!");
            }

            _gameInitialized = true;
        }

        /// <summary>
        /// Transition from Loading phase to PreShow phase after initialization is complete.
        /// </summary>
        public void FinishLoading()
        {
            if (_currentPhase != GamePhase.Loading)
            {
                GD.PrintErr("GameStateManager: Cannot finish loading - not in Loading phase");
                return;
            }

            GD.Print("GameStateManager: Loading complete, transitioning to PreShow");
            GamePhase oldPhase = _currentPhase;
            _currentPhase = GamePhase.PreShow;
            EmitSignal("PhaseChanged", (int)oldPhase, (int)_currentPhase);
            OnPhaseChanged?.Invoke(oldPhase, _currentPhase);
        }

		/// <summary>
		/// Transition to the next phase in the nightly cycle.
		/// </summary>
		public void AdvancePhase()
		{
			GamePhase oldPhase = _currentPhase;

			switch (_currentPhase)
			{
				case GamePhase.Loading:
					// Cannot advance from Loading - use FinishLoading() instead
					GD.PrintErr("GameStateManager: Cannot advance from Loading phase - use FinishLoading()");
					return;

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
			OnPhaseChanged?.Invoke(oldPhase, _currentPhase);
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
			OnPhaseChanged?.Invoke(oldPhase, _currentPhase);

			// Start the show clock
			TimeManager.StartClock();

			// Initialize ad manager with schedule
			AdManager.Initialize(_adSchedule, TimeManager.ShowDuration);

			// Initialize broadcast flow
			_ = Task.Run(async () => {
				try
				{
					await AsyncBroadcastLoop.StartBroadcastAsync(TimeManager.ShowDuration);
				}
				catch (Exception ex)
				{
					GD.PrintErr($"GameStateManager: Error in async broadcast: {ex.Message}");
				}
			});
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
			OnPhaseChanged?.Invoke(oldPhase, _currentPhase);
			EmitSignal("NightStarted", _currentNight);
			OnNightStarted?.Invoke(_currentNight);
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
			CallerRepository.ClearAll();
			GD.Print("GameStateManager: All callers cleared");

			// Get show performance data
			int peakListeners = ListenerManager.PeakListeners;

			float showQuality = _vernStats?.CalculateVIBE() ?? 50f;

			// Calculate and award income
			int income = IncomeCalculator.CalculateShowIncome(peakListeners, showQuality);
			EconomyManager.AddMoney(income, "Show Income");
			GD.Print($"GameStateManager: Added income {income} (quality: {showQuality:F1}, peak listeners: {peakListeners})");

			// Update save data
			if (SaveManager.CurrentSave != null)
			{
				var save = SaveManager.CurrentSave;
				save.TotalShowsCompleted++;
				if (peakListeners > save.PeakListenersAllTime)
				{
					save.PeakListenersAllTime = peakListeners;
				}
				save.CurrentNight = _currentNight;
			}

			// Reset the clock for next show
			TimeManager.ResetClock();

			// Always play outro music at show end
			GD.Print("GameStateManager: Playing outro music");
			var outroItem = new BroadcastItem("outro_music", BroadcastItemType.Music, "Outro music", "res://assets/audio/music/intro_music.wav", 4.0f);
			await AudioPlayer.PlayAudioForBroadcastItemAsync(outroItem);
			GD.Print("GameStateManager: Outro music completed");

			// Auto-save at end of show
			SaveManager.Save();
			GD.Print("GameStateManager: Game saved");

			// Show PostShow UI
			UIManager.ShowPostShowLayer();
			GD.Print("GameStateManager: PostShow UI shown");

			// Emit phase changed signal
			EmitSignal("PhaseChanged", (int)GamePhase.LiveShow, (int)GamePhase.PostShow);
		}

		/// <summary>
		/// Handle automatic show end when timer expires at T=0.
		/// Interrupts current audio, clears callers, then advances to PostShow.
		/// </summary>
		private void OnShowTimerExpired()
		{
			GD.Print("GameStateManager: OnShowTimerExpired - Timer reached 0, ending show");

			// Interrupt current audio playback
			AudioPlayer.Stop();
			GD.Print("GameStateManager: Current audio interrupted");

			// Clear all callers and stop generation
			CallerRepository.ClearAll();
			CallerGenerator.StopGenerating();
			GD.Print("GameStateManager: All callers cleared and generation stopped");

			// Advance to PostShow phase immediately
			AdvanceToPostShow();
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
			OnPhaseChanged?.Invoke(oldPhase, _currentPhase);
		}
	}
}
