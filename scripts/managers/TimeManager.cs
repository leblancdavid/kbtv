using System;
using Godot;
using KBTV.Core;

namespace KBTV.Managers
{
    /// <summary>
    /// Manages the in-game clock for the radio show.
    /// Handles time progression during live broadcasts.
    /// </summary>
 	public partial class TimeManager : Node
     {
		[Signal] public delegate void TickEventHandler(float delta);
		[Signal] public delegate void ShowEndedEventHandler();
		[Signal] public delegate void RunningChangedEventHandler(bool isRunning);
        [Export] private float _showDurationSeconds = 600f; // 10 minutes real-time
        [Export] private float _showDurationHours = 4f;
        [Export] private int _showStartHour = 22;

        // Runtime state (not serialized - always starts fresh)
        private float _elapsedTime = 0f;
        private bool _isRunning = false;

        public float ElapsedTime => _elapsedTime;
        public float ShowDuration => _showDurationSeconds;
        public float Progress => Mathf.Clamp(_elapsedTime / _showDurationSeconds, 0f, 1f);
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Current in-game time as a formatted string (e.g., "10:45 PM").
        /// </summary>
        public string CurrentTimeFormatted => GetFormattedTime();

        /// <summary>
        /// Current in-game hour (0-23).
        /// </summary>
        public float CurrentHour => _showStartHour + (_showDurationHours * Progress);



        public override void _Ready()
        {
            // Ensure clean state on startup
            _elapsedTime = 0f;
            _isRunning = false;
        }

        public override void _Process(double delta)
        {
            if (!_isRunning) return;

            float deltaTime = (float)delta;
            _elapsedTime += deltaTime;

            EmitSignal("Tick", deltaTime);

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
            EmitSignal("RunningChanged", true);
        }

        /// <summary>
        /// Pause the show clock.
        /// </summary>
        public void PauseClock()
        {
            if (!_isRunning) return;

            _isRunning = false;
            EmitSignal("RunningChanged", false);
        }

        /// <summary>
        /// Reset the clock for a new show.
        /// </summary>
        public void ResetClock()
        {
            _elapsedTime = 0f;
            _isRunning = false;
        }

        /// <summary>
        /// End the show (called when time runs out or manually).
        /// </summary>
        public void EndShow()
        {
            _isRunning = false;
            EmitSignal("ShowEnded");
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