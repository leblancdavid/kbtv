using System;
using Godot;
using KBTV.Data;
using KBTV.Managers;
using KBTV.Economy;
using KBTV.Persistence;
using KBTV.Callers;

namespace KBTV.Core
{
	/// <summary>
	/// Manages the game state and phase transitions for nightly broadcasts.
	/// </summary>
	public partial class GameStateManager : SingletonNode<GameStateManager>
	{
		private VernStats _vernStats;
		private GamePhase _currentPhase = GamePhase.PreShow;
		private int _currentNight = 1;
		private Topic _selectedTopic;

		public VernStats VernStats => _vernStats;
		public GamePhase CurrentPhase => _currentPhase;
		public int CurrentNight => _currentNight;
		public Topic SelectedTopic => _selectedTopic;

		/// <summary>
		/// Fired when the game phase changes.
		/// </summary>
		public event Action<GamePhase, GamePhase> OnPhaseChanged; // oldPhase, newPhase

		/// <summary>
		/// Fired when a new night begins.
		/// </summary>
		public event Action<int> OnNightStarted;

    protected override void OnSingletonReady()
    {
        // GD.Print("GameStateManager: OnSingletonReady called - singleton now available");

        // Create VernStats dynamically
        _vernStats = new VernStats();
        InitializeGame();

        // GD.Print($"GameStateManager: Initialized with phase {_currentPhase}");
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

			OnPhaseChanged?.Invoke(oldPhase, _currentPhase);
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
        OnPhaseChanged?.Invoke(oldPhase, _currentPhase);
        // TODO: Initialize live show systems with selected topic
    }

		/// <summary>
		/// Directly set the game phase (useful for testing/debugging).
		/// </summary>
		public void SetPhase(GamePhase phase)
		{
			var oldPhase = _currentPhase;
			_currentPhase = phase;
			OnPhaseChanged?.Invoke(oldPhase, phase);
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

			OnPhaseChanged?.Invoke(oldPhase, _currentPhase);
			OnNightStarted?.Invoke(_currentNight);
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
			// Get show performance data
			int peakListeners = ListenerManager.Instance?.PeakListeners ?? 0;

			float showQuality = _vernStats?.CalculateVIBE() ?? 50f;

			// Calculate and award income
			int income = IncomeCalculator.CalculateShowIncome(peakListeners, showQuality);
			if (EconomyManager.Instance != null)
			{
				EconomyManager.Instance.AddMoney(income, "Show Income");
			}

			// Update save data
			if (SaveManager.Instance?.CurrentSave != null)
			{
				var save = SaveManager.Instance.CurrentSave;
				save.TotalShowsCompleted++;
				if (peakListeners > save.PeakListenersAllTime)
				{
					save.PeakListenersAllTime = peakListeners;
				}
				save.CurrentNight = _currentNight;
			}

			// Auto-save at end of show
			if (SaveManager.Instance != null)
			{
				SaveManager.Instance.Save();
			}
		}
	}
}
