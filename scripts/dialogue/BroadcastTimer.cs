#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using KBTV.Core;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Types of broadcast timing events.
    /// </summary>
    public enum BroadcastTimingEventType
    {
        Break20Seconds,
        Break10Seconds,
        Break5Seconds,
        Break0Seconds,
        ShowEnd,
        AdBreakStart,
        AdBreakEnd
    }

    /// <summary>
    /// Broadcast timing event containing context information.
    /// </summary>
    public class BroadcastTimingEvent : GameEvent
    {
        public BroadcastTimingEventType Type { get; }
        public object? Context { get; }

        public BroadcastTimingEvent(BroadcastTimingEventType type, object? context = null)
        {
            Type = type;
            Context = context;
            Source = "BroadcastTimer";
        }
    }

    /// <summary>
    /// Dedicated timing service for broadcast events.
    /// Fires specific timing events for breaks, show end, and ad breaks.
    /// Replaces reliance on AdManager for timing.
    /// </summary>
    [GlobalClass]
    public partial class BroadcastTimer : Node
    {
        private readonly Dictionary<BroadcastTimingEventType, Timer> _timers = new();
        private readonly EventBus _eventBus;
        private bool _isShowActive = false;

        public BroadcastTimer()
        {
            _eventBus = ServiceRegistry.Instance.EventBus;
        }

        public override void _Ready()
        {
            CreateTimers();
        }

        /// <summary>
        /// Create and configure all timing timers.
        /// </summary>
        private void CreateTimers()
        {
            // Break warning timers
            CreateTimer(BroadcastTimingEventType.Break20Seconds, 20.0f, false);
            CreateTimer(BroadcastTimingEventType.Break10Seconds, 10.0f, false);
            CreateTimer(BroadcastTimingEventType.Break5Seconds, 5.0f, false);
            CreateTimer(BroadcastTimingEventType.Break0Seconds, 0.0f, false);
            
            // Show end timer
            CreateTimer(BroadcastTimingEventType.ShowEnd, 600.0f, false); // 10 minutes default
            
            // Ad break timers (configured as needed)
            CreateTimer(BroadcastTimingEventType.AdBreakStart, 0.0f, false);
            CreateTimer(BroadcastTimingEventType.AdBreakEnd, 0.0f, false);
        }

        /// <summary>
        /// Create a timer for a specific timing event.
        /// </summary>
        private void CreateTimer(BroadcastTimingEventType eventType, float waitTime, bool oneShot = true)
        {
            var timer = new Timer
            {
                WaitTime = waitTime,
                OneShot = oneShot,
                Autostart = false
            };
            
            timer.Timeout += () => OnTimerTimeout(eventType);
            AddChild(timer);
            
            _timers[eventType] = timer;
        }

        /// <summary>
        /// Handle timer timeout events.
        /// </summary>
        private void OnTimerTimeout(BroadcastTimingEventType eventType)
        {
            if (!_isShowActive && eventType != BroadcastTimingEventType.ShowEnd)
                return;

            var timingEvent = new BroadcastTimingEvent(eventType);
            _eventBus.Publish(timingEvent);
        }

        /// <summary>
        /// Start show timing - enables all timing events.
        /// </summary>
        public void StartShow(float showDuration = 600.0f)
        {
            _isShowActive = true;
            
            // Configure show end timer
            var showEndTimer = _timers[BroadcastTimingEventType.ShowEnd];
            showEndTimer.WaitTime = showDuration;
            showEndTimer.Start();
            
            GD.Print($"BroadcastTimer: Started show with {showDuration}s duration");
        }

        /// <summary>
        /// Stop show timing - disables all timing events.
        /// </summary>
        public void StopShow()
        {
            _isShowActive = false;
            
            // Stop all timers
            foreach (var timer in _timers.Values)
            {
                timer.Stop();
            }
            
            GD.Print("BroadcastTimer: Stopped show timing");
        }

        /// <summary>
        /// Schedule break warning timers.
        /// </summary>
        public void ScheduleBreakWarnings(float breakTime)
        {
            if (!_isShowActive) return;

            var time = Time.GetDatetimeDictFromSystem();
            var currentSeconds = (float)time["second"] + (float)time["minute"] * 60 + (float)time["hour"] * 3600;
            
            // Calculate relative times for break warnings
            var breakTimeSeconds = currentSeconds + breakTime;
            
            ScheduleBreakWarning(BroadcastTimingEventType.Break20Seconds, breakTimeSeconds - 20);
            ScheduleBreakWarning(BroadcastTimingEventType.Break10Seconds, breakTimeSeconds - 10);
            ScheduleBreakWarning(BroadcastTimingEventType.Break5Seconds, breakTimeSeconds - 5);
            ScheduleBreakWarning(BroadcastTimingEventType.Break0Seconds, breakTimeSeconds);
            
            GD.Print($"BroadcastTimer: Scheduled break warnings for {breakTime}s from now");
        }

        /// <summary>
        /// Schedule an individual break warning timer.
        /// </summary>
        private void ScheduleBreakWarning(BroadcastTimingEventType eventType, float targetTime)
        {
            var time = Time.GetDatetimeDictFromSystem();
            var currentSeconds = (float)time["second"] + (float)time["minute"] * 60 + (float)time["hour"] * 3600;
            
            var waitTime = targetTime - currentSeconds;
            if (waitTime > 0)
            {
                var timer = _timers[eventType];
                timer.WaitTime = waitTime;
                timer.Start();
            }
        }

        /// <summary>
        /// Start ad break timing.
        /// </summary>
        public void StartAdBreak(float duration = 30.0f)
        {
            var startEvent = new BroadcastTimingEvent(BroadcastTimingEventType.AdBreakStart);
            _eventBus.Publish(startEvent);
            
            // Schedule ad break end
            var endTimer = _timers[BroadcastTimingEventType.AdBreakEnd];
            endTimer.WaitTime = duration;
            endTimer.Start();
            
            GD.Print($"BroadcastTimer: Started ad break with {duration}s duration");
        }

        /// <summary>
        /// Stop ad break timing.
        /// </summary>
        public void StopAdBreak()
        {
            var endTimer = _timers[BroadcastTimingEventType.AdBreakEnd];
            endTimer.Stop();
            
            var endEvent = new BroadcastTimingEvent(BroadcastTimingEventType.AdBreakEnd);
            _eventBus.Publish(endEvent);
            
            GD.Print("BroadcastTimer: Stopped ad break timing");
        }

        /// <summary>
        /// Get remaining time until a specific timing event.
        /// </summary>
        public float GetTimeUntil(BroadcastTimingEventType eventType)
        {
            if (_timers.TryGetValue(eventType, out var timer))
            {
                return timer.TimeLeft;
            }
            return 0f;
        }

        /// <summary>
        /// Check if a timer is currently active.
        /// </summary>
        public bool IsTimerActive(BroadcastTimingEventType eventType)
        {
            if (_timers.TryGetValue(eventType, out var timer))
            {
                return timer.TimeLeft > 0;
            }
            return false;
        }
    }
}