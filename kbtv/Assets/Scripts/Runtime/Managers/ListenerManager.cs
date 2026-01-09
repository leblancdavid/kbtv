using System;
using UnityEngine;
using KBTV.Core;
using KBTV.Data;
using KBTV.Callers;

namespace KBTV.Managers
{
    /// <summary>
    /// Tracks listener count during live shows.
    /// Listener count fluctuates based on show quality and caller quality.
    /// </summary>
    public class ListenerManager : MonoBehaviour
    {
        public static ListenerManager Instance { get; private set; }

        [Header("Starting Listeners")]
        [Tooltip("Base listener count at the start of each night")]
        [SerializeField] private int _baseListeners = 1000;

        [Tooltip("Random variance added to base listeners (±)")]
        [SerializeField] private int _listenerVariance = 200;

        [Header("Growth Settings")]
        [Tooltip("How quickly listeners change based on show quality (per second)")]
        [SerializeField] private float _qualityGrowthRate = 5f;

        [Tooltip("Quality threshold - above this grows listeners, below shrinks")]
        [SerializeField] private float _qualityThreshold = 0.5f;

        [Tooltip("Maximum multiplier on base listeners")]
        [SerializeField] private float _maxListenerMultiplier = 3f;

        [Tooltip("Minimum multiplier on base listeners (can't go below this)")]
        [SerializeField] private float _minListenerMultiplier = 0.1f;

        [Header("Caller Impact")]
        [Tooltip("Listeners gained from a great caller")]
        [SerializeField] private int _greatCallerBonus = 150;

        [Tooltip("Listeners gained from a good caller")]
        [SerializeField] private int _goodCallerBonus = 50;

        [Tooltip("Listeners lost from a bad caller")]
        [SerializeField] private int _badCallerPenalty = 100;

        [Tooltip("Listeners lost when a caller disconnects (hung up on)")]
        [SerializeField] private int _disconnectPenalty = 25;

        // Runtime state
        private int _currentListeners;
        private int _peakListeners;
        private int _startingListeners;
        private float _accumulatedGrowth;

        public int CurrentListeners => _currentListeners;
        public int PeakListeners => _peakListeners;
        public int StartingListeners => _startingListeners;

        /// <summary>
        /// Listener change since show started. Can be negative.
        /// </summary>
        public int ListenerChange => _currentListeners - _startingListeners;

        /// <summary>
        /// Fired when listener count changes. (oldCount, newCount)
        /// </summary>
        public event Action<int, int> OnListenersChanged;

        /// <summary>
        /// Fired when a new peak is reached.
        /// </summary>
        public event Action<int> OnPeakReached;

        private GameStateManager _gameState;
        private TimeManager _timeManager;
        private CallerQueue _callerQueue;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            _gameState = GameStateManager.Instance;
            _timeManager = TimeManager.Instance;
            _callerQueue = CallerQueue.Instance;

            if (_gameState != null)
            {
                _gameState.OnPhaseChanged += HandlePhaseChanged;
            }

            if (_timeManager != null)
            {
                _timeManager.OnTick += HandleTick;
            }

            if (_callerQueue != null)
            {
                _callerQueue.OnCallerCompleted += HandleCallerCompleted;
                _callerQueue.OnCallerDisconnected += HandleCallerDisconnected;
            }
        }

        private void OnDestroy()
        {
            if (_gameState != null)
            {
                _gameState.OnPhaseChanged -= HandlePhaseChanged;
            }

            if (_timeManager != null)
            {
                _timeManager.OnTick -= HandleTick;
            }

            if (_callerQueue != null)
            {
                _callerQueue.OnCallerCompleted -= HandleCallerCompleted;
                _callerQueue.OnCallerDisconnected -= HandleCallerDisconnected;
            }
        }

        private void HandlePhaseChanged(GamePhase oldPhase, GamePhase newPhase)
        {
            if (newPhase == GamePhase.LiveShow)
            {
                InitializeListeners();
            }
        }

        private void InitializeListeners()
        {
            // Calculate starting listeners with some variance
            int variance = UnityEngine.Random.Range(-_listenerVariance, _listenerVariance + 1);
            _startingListeners = Mathf.Max(100, _baseListeners + variance);
            _currentListeners = _startingListeners;
            _peakListeners = _startingListeners;
            _accumulatedGrowth = 0f;

            Debug.Log($"ListenerManager: Show starting with {_currentListeners} listeners");
            OnListenersChanged?.Invoke(0, _currentListeners);
        }

        private void HandleTick(float deltaTime)
        {
            if (_gameState == null || !_gameState.IsLive) return;

            VernStats stats = _gameState.VernStats;
            if (stats == null) return;

            float showQuality = stats.CalculateShowQuality();

            // Calculate growth/decay based on quality vs threshold
            float qualityDelta = showQuality - _qualityThreshold;
            float growthAmount = qualityDelta * _qualityGrowthRate * deltaTime;

            // Accumulate fractional growth
            _accumulatedGrowth += growthAmount;

            // Apply whole number changes
            int wholeChange = Mathf.FloorToInt(_accumulatedGrowth);
            if (wholeChange != 0)
            {
                _accumulatedGrowth -= wholeChange;
                ModifyListeners(wholeChange);
            }
        }

        private void HandleCallerCompleted(Caller caller)
        {
            if (_gameState == null || !_gameState.IsLive) return;

            // Get current topic for impact calculation
            string topicId = CallerScreeningManager.Instance?.CurrentTopic?.TopicId ?? "";
            float impact = caller.CalculateShowImpact(topicId);

            int listenerChange = 0;

            if (impact >= 15f)
            {
                listenerChange = _greatCallerBonus;
                Debug.Log($"ListenerManager: Great caller! +{listenerChange} listeners");
            }
            else if (impact >= 5f)
            {
                listenerChange = _goodCallerBonus;
                Debug.Log($"ListenerManager: Good caller. +{listenerChange} listeners");
            }
            else if (impact < 0f)
            {
                listenerChange = -_badCallerPenalty;
                Debug.Log($"ListenerManager: Bad caller! {listenerChange} listeners");
            }

            if (listenerChange != 0)
            {
                ModifyListeners(listenerChange);
            }
        }

        private void HandleCallerDisconnected(Caller caller)
        {
            if (_gameState == null || !_gameState.IsLive) return;

            // Losing callers looks bad
            ModifyListeners(-_disconnectPenalty);
            Debug.Log($"ListenerManager: Caller disconnected. -{_disconnectPenalty} listeners");
        }

        /// <summary>
        /// Modify listener count by the given amount.
        /// </summary>
        public void ModifyListeners(int amount)
        {
            int minListeners = Mathf.RoundToInt(_baseListeners * _minListenerMultiplier);
            int maxListeners = Mathf.RoundToInt(_baseListeners * _maxListenerMultiplier);

            int oldCount = _currentListeners;
            _currentListeners = Mathf.Clamp(_currentListeners + amount, minListeners, maxListeners);

            if (_currentListeners != oldCount)
            {
                OnListenersChanged?.Invoke(oldCount, _currentListeners);

                if (_currentListeners > _peakListeners)
                {
                    _peakListeners = _currentListeners;
                    OnPeakReached?.Invoke(_peakListeners);
                    Debug.Log($"ListenerManager: New peak! {_peakListeners} listeners");
                }
            }
        }

        /// <summary>
        /// Get a formatted listener count string (e.g., "1,234" or "12.3K")
        /// </summary>
        public string GetFormattedListeners()
        {
            return FormatListenerCount(_currentListeners);
        }

        /// <summary>
        /// Get a formatted change string (e.g., "+123" or "-45")
        /// </summary>
        public string GetFormattedChange()
        {
            int change = ListenerChange;
            string sign = change >= 0 ? "+" : "";
            return $"{sign}{FormatListenerCount(change)}";
        }

        private static string FormatListenerCount(int count)
        {
            int absCount = Mathf.Abs(count);
            string sign = count < 0 ? "-" : "";

            if (absCount >= 1000000)
            {
                return $"{sign}{absCount / 1000000f:F1}M";
            }
            else if (absCount >= 10000)
            {
                return $"{sign}{absCount / 1000f:F1}K";
            }
            else
            {
                return $"{sign}{absCount:N0}";
            }
        }
    }
}
