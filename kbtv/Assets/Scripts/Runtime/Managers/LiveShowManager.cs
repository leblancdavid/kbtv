using UnityEngine;
using KBTV.Core;
using KBTV.Data;

namespace KBTV.Managers
{
    /// <summary>
    /// Connects the game systems together and handles stat decay during live shows.
    /// Attach this to the same GameObject as GameStateManager.
    /// </summary>
    [RequireComponent(typeof(GameStateManager))]
    public class LiveShowManager : MonoBehaviour
    {
        [Header("Decay Settings")]
        [Tooltip("Multiplier for how fast stats decay during live show")]
        [SerializeField] private float _decayMultiplier = 1f;

        private GameStateManager _gameState;
        private TimeManager _timeManager;

        private void Awake()
        {
            _gameState = GetComponent<GameStateManager>();
        }

        private void Start()
        {
            _timeManager = TimeManager.Instance;

            if (_timeManager != null)
            {
                _timeManager.OnTick += HandleTick;
                _timeManager.OnShowEnded += HandleShowEnded;
            }

            if (_gameState != null)
            {
                _gameState.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        private void OnDestroy()
        {
            if (_timeManager != null)
            {
                _timeManager.OnTick -= HandleTick;
                _timeManager.OnShowEnded -= HandleShowEnded;
            }

            if (_gameState != null)
            {
                _gameState.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void HandleTick(float deltaTime)
        {
            // Only apply decay during live show
            if (_gameState == null || !_gameState.IsLive) return;

            VernStats stats = _gameState.VernStats;
            if (stats != null)
            {
                stats.ApplyDecay(deltaTime, _decayMultiplier);
            }
        }

        private void HandlePhaseChanged(GamePhase oldPhase, GamePhase newPhase)
        {
            if (newPhase == GamePhase.LiveShow)
            {
                StartLiveShow();
            }
            else if (oldPhase == GamePhase.LiveShow)
            {
                StopLiveShow();
            }
        }

        private void StartLiveShow()
        {
            Debug.Log("LiveShowManager: Starting live show");
            
            if (_timeManager != null)
            {
                _timeManager.ResetClock();
                _timeManager.StartClock();
            }
        }

        private void StopLiveShow()
        {
            Debug.Log("LiveShowManager: Stopping live show");
            
            if (_timeManager != null)
            {
                _timeManager.PauseClock();
            }
        }

        private void HandleShowEnded()
        {
            Debug.Log("LiveShowManager: Show time ended, transitioning to PostShow");
            
            if (_gameState != null)
            {
                _gameState.AdvancePhase();
            }
        }
    }
}
