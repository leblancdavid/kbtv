using System;
using Godot;
using KBTV.Data;
using KBTV.Dialogue;
using KBTV.Economy;
using KBTV.Managers;
using KBTV.Persistence;
using KBTV.Callers;

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
    private static int _instanceCount;
    private int _instanceId;

    public VernStats VernStats => _vernStats;
    public GamePhase CurrentPhase => _currentPhase;
    public int CurrentNight => _currentNight;
    public Topic SelectedTopic => _selectedTopic;

    public override void _Ready()
    {
        _instanceId = ++_instanceCount;
        GD.Print($"GameStateManager: _Ready called (instance #{_instanceId})");
        ServiceRegistry.Instance.RegisterSelf<GameStateManager>(this);

			_vernStats = new VernStats();
			InitializeGame();
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
					break;

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

			// Initialize broadcast flow
			ServiceRegistry.Instance.BroadcastCoordinator?.OnLiveShowStarted();
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
		/// </summary>
		private void ProcessEndOfShow()
		{
			// End broadcast flow first
			ServiceRegistry.Instance.BroadcastCoordinator?.OnLiveShowEnding();

			// Get show performance data
			int peakListeners = ServiceRegistry.Instance.ListenerManager?.PeakListeners ?? 0;

			float showQuality = _vernStats?.CalculateVIBE() ?? 50f;

			// Calculate and award income
			int income = IncomeCalculator.CalculateShowIncome(peakListeners, showQuality);
			if (ServiceRegistry.Instance.EconomyManager != null)
			{
				ServiceRegistry.Instance.EconomyManager.AddMoney(income, "Show Income");
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

			// Auto-save at end of show
			if (ServiceRegistry.Instance.SaveManager != null)
			{
				ServiceRegistry.Instance.SaveManager.Save();
			}
		}
	}
}
