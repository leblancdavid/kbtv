using System;
using UnityEngine;

namespace KBTV.Managers
{
    /// <summary>
    /// Manages the in-game clock for the radio show.
    /// Handles time progression during live broadcasts.
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Header("Show Timing")]
        [Tooltip("Duration of the live show in real-time seconds")]
        [SerializeField] private float _showDurationSeconds = 300f; // 5 minutes real-time

        [Tooltip("In-game hours the show runs (e.g., 10 PM to 2 AM = 4 hours)")]
        [SerializeField] private float _showDurationHours = 4f;

        [Tooltip("Show start time in 24-hour format (e.g., 22 = 10 PM)")]
        [SerializeField] private int _showStartHour = 22;

        [Header("State")]
        [SerializeField] private float _elapsedTime = 0f;
        [SerializeField] private bool _isRunning = false;

        public float ElapsedTime => _elapsedTime;
        public float ShowDuration => _showDurationSeconds;
        public float Progress => Mathf.Clamp01(_elapsedTime / _showDurationSeconds);
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Current in-game time as a formatted string (e.g., "10:45 PM").
        /// </summary>
        public string CurrentTimeFormatted => GetFormattedTime();

        /// <summary>
        /// Current in-game hour (0-23).
        /// </summary>
        public float CurrentHour => _showStartHour + (_showDurationHours * Progress);

        /// <summary>
        /// Fired every frame while the clock is running. Passes delta time.
        /// </summary>
        public event Action<float> OnTick;

        /// <summary>
        /// Fired when the show time runs out.
        /// </summary>
        public event Action OnShowEnded;

        /// <summary>
        /// Fired when time starts or stops.
        /// </summary>
        public event Action<bool> OnRunningChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (!_isRunning) return;

            float deltaTime = Time.deltaTime;
            _elapsedTime += deltaTime;

            OnTick?.Invoke(deltaTime);

            if (_elapsedTime >= _showDurationSeconds)
            {
                EndShow();
            }
        }

        /// <summary>
        /// Start the show clock.
        /// </summary>
        public void StartClock()
        {
            if (_isRunning) return;

            _isRunning = true;
            Debug.Log("TimeManager: Clock started");
            OnRunningChanged?.Invoke(true);
        }

        /// <summary>
        /// Pause the show clock.
        /// </summary>
        public void PauseClock()
        {
            if (!_isRunning) return;

            _isRunning = false;
            Debug.Log("TimeManager: Clock paused");
            OnRunningChanged?.Invoke(false);
        }

        /// <summary>
        /// Reset the clock for a new show.
        /// </summary>
        public void ResetClock()
        {
            _elapsedTime = 0f;
            _isRunning = false;
            Debug.Log("TimeManager: Clock reset");
        }

        /// <summary>
        /// End the show (called when time runs out or manually).
        /// </summary>
        public void EndShow()
        {
            _isRunning = false;
            Debug.Log("TimeManager: Show ended");
            OnShowEnded?.Invoke();
        }

        private string GetFormattedTime()
        {
            float hour = CurrentHour;
            
            // Handle day wrap (e.g., 22 + 4 = 26 -> 2 AM)
            while (hour >= 24f) hour -= 24f;

            int hourInt = Mathf.FloorToInt(hour);
            int minutes = Mathf.FloorToInt((hour - hourInt) * 60f);

            bool isPM = hourInt >= 12;
            int displayHour = hourInt % 12;
            if (displayHour == 0) displayHour = 12;

            string ampm = isPM ? "PM" : "AM";
            return $"{displayHour}:{minutes:D2} {ampm}";
        }

        /// <summary>
        /// Get remaining time in seconds.
        /// </summary>
        public float RemainingTime => Mathf.Max(0f, _showDurationSeconds - _elapsedTime);

        /// <summary>
        /// Get remaining time as formatted string (MM:SS).
        /// </summary>
        public string RemainingTimeFormatted
        {
            get
            {
                float remaining = RemainingTime;
                int minutes = Mathf.FloorToInt(remaining / 60f);
                int seconds = Mathf.FloorToInt(remaining % 60f);
                return $"{minutes:D2}:{seconds:D2}";
            }
        }
    }
}
