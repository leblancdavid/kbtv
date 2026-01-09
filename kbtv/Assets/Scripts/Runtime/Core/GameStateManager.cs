using System;
using UnityEngine;
using KBTV.Data;

namespace KBTV.Core
{
    /// <summary>
    /// Manages the game state and phase transitions for nightly broadcasts.
    /// Singleton pattern for easy access across the game.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private VernStats _vernStats;

        [Header("State")]
        [SerializeField] private GamePhase _currentPhase = GamePhase.PreShow;
        [SerializeField] private int _currentNight = 1;

        public GamePhase CurrentPhase => _currentPhase;
        public int CurrentNight => _currentNight;
        public VernStats VernStats => _vernStats;

        /// <summary>
        /// Fired when the game phase changes.
        /// </summary>
        public event Action<GamePhase, GamePhase> OnPhaseChanged; // oldPhase, newPhase

        /// <summary>
        /// Fired when a new night begins.
        /// </summary>
        public event Action<int> OnNightStarted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeGame();
        }

        private void InitializeGame()
        {
            if (_vernStats != null)
            {
                _vernStats.Initialize();
            }
            else
            {
                Debug.LogError("GameStateManager: VernStats not assigned!");
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
                    break;

                case GamePhase.PostShow:
                    // Start a new night
                    StartNewNight();
                    return;
            }

            Debug.Log($"Phase changed: {oldPhase} -> {_currentPhase}");
            OnPhaseChanged?.Invoke(oldPhase, _currentPhase);
        }

        /// <summary>
        /// Directly set the game phase (useful for testing/debugging).
        /// </summary>
        public void SetPhase(GamePhase phase)
        {
            if (_currentPhase == phase) return;

            GamePhase oldPhase = _currentPhase;
            _currentPhase = phase;

            Debug.Log($"Phase set: {oldPhase} -> {_currentPhase}");
            OnPhaseChanged?.Invoke(oldPhase, _currentPhase);
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

            Debug.Log($"Starting Night {_currentNight}");
            OnPhaseChanged?.Invoke(oldPhase, _currentPhase);
            OnNightStarted?.Invoke(_currentNight);
        }

        /// <summary>
        /// Check if we're currently in the live broadcast phase.
        /// </summary>
        public bool IsLive => _currentPhase == GamePhase.LiveShow;
    }
}
