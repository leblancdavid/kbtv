using System;
using System.Collections.Generic;
using Godot;
using KBTV.Core;
using KBTV.Managers;

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
    /// Commands sent to BroadcastTimer from async components.
    /// </summary>
    public enum BroadcastTimerCommandType
    {
        StartShow,
        StopShow,
        ScheduleBreakWarnings,
        StartAdBreak,
        StopAdBreak
    }

    /// <summary>
    /// Command event for timer operations.
    /// </summary>
    public class BroadcastTimerCommand : GameEvent
    {
        public BroadcastTimerCommandType Type { get; }
        public object? Data { get; }

        public BroadcastTimerCommand(BroadcastTimerCommandType type, object? data = null)
        {
            Type = type;
            Data = data;
            Source = "AsyncBroadcastLoop";
        }
    }

    /// <summary>
    /// Dedicated timing service for broadcast events.
    /// Fires specific timing events for breaks, show end, and ad breaks.
    /// Replaces reliance on AdManager for timing.
    /// </summary>
[GlobalClass]
public partial class BroadcastTimer : Node, 
    IProvide<BroadcastTimer>,
    IDependent
{
    public override void _Notification(int what) => this.Notify(what);

    private readonly Dictionary<BroadcastTimingEventType, Timer> _timers = new();
    private EventBus _eventBus = null!;
    private TimeManager _timeManager = null!;
    private bool _isShowActive = false;

    // Provider interface implementation
    BroadcastTimer IProvide<BroadcastTimer>.Value() => this;

        public BroadcastTimer()
        {
            // ServiceRegistry access moved to _Ready() to avoid initialization order issues
        }

        public override void _Ready()
        {
            CreateTimers();
        }

        public void OnResolved()
        {
            _eventBus = DependencyInjection.Get<EventBus>(this);
            _timeManager = DependencyInjection.Get<TimeManager>(this);
            _eventBus.Subscribe<BroadcastTimerCommand>(HandleTimerCommand);
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
            CreateTimer(BroadcastTimingEventType.Break0Seconds, 0.0f, false); // Will be set to 0.001f for Godot compatibility
            
            // Show end timer
            CreateTimer(BroadcastTimingEventType.ShowEnd, 600.0f, false); // 10 minutes default
            
            // Ad break timers (configured as needed)
            CreateTimer(BroadcastTimingEventType.AdBreakStart, 0.0f, false); // Will be set to 0.001f for Godot compatibility
            CreateTimer(BroadcastTimingEventType.AdBreakEnd, 0.0f, false); // Will be set to 0.001f for Godot compatibility
        }

        /// <summary>
        /// Create a timer for a specific timing event.
        /// </summary>
        private void CreateTimer(BroadcastTimingEventType eventType, float waitTime, bool oneShot = true)
        {
            // Godot Timer requires WaitTime > 0.0f. For zero-wait timers, use minimum positive value.
            if (waitTime <= 0.0f)
            {
                waitTime = 0.001f; // 1ms - smallest practical timer duration
            }

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
        /// Handle broadcast timer commands from async components.
        /// </summary>
        private void HandleTimerCommand(BroadcastTimerCommand command)
        {
            switch (command.Type)
            {
                case BroadcastTimerCommandType.StartShow:
                    StartShowInternal((float)command.Data!);
                    break;
                case BroadcastTimerCommandType.StopShow:
                    StopShowInternal();
                    break;
                case BroadcastTimerCommandType.ScheduleBreakWarnings:
                    ScheduleBreakWarningsInternal((float)command.Data!);
                    break;
                case BroadcastTimerCommandType.StartAdBreak:
                    StartAdBreakInternal();
                    break;
                case BroadcastTimerCommandType.StopAdBreak:
                    StopAdBreakInternal();
                    break;
            }
        }

        /// <summary>
        /// Start show timing - enables all timing events.
        /// </summary>
        private void StartShowInternal(float showDuration = 600.0f)
        {
            _isShowActive = true;
            CallDeferred(nameof(DeferredStartShow), showDuration);
        }

        /// <summary>
        /// Deferred timer operations for show start (main thread only).
        /// </summary>
        private void DeferredStartShow(float showDuration)
        {
            // Configure show end timer
            var showEndTimer = _timers[BroadcastTimingEventType.ShowEnd];
            showEndTimer.WaitTime = showDuration;
            showEndTimer.Start();
        }

        /// <summary>
        /// Stop show timing - disables all timing events.
        /// </summary>
        private void StopShowInternal()
        {
            _isShowActive = false;
            CallDeferred(nameof(DeferredStopShow));
        }

        /// <summary>
        /// Deferred timer operations for show stop (main thread only).
        /// </summary>
        private void DeferredStopShow()
        {
            // Stop all timers
            foreach (var timer in _timers.Values)
            {
                timer.Stop();
            }
        }

        /// <summary>
        /// Schedule break warning timers.
        /// </summary>
        private void ScheduleBreakWarningsInternal(float breakTime)
        {
            CallDeferred(nameof(DeferredScheduleBreakWarnings), breakTime);
        }

        /// <summary>
        /// Deferred timer operations for break scheduling (main thread only).
        /// </summary>
        private void DeferredScheduleBreakWarnings(float breakTime)
        {
            float currentTime = _timeManager?.ElapsedTime ?? 0f;
            
            // Calculate absolute times for break warnings using game time
            float breakTimeAbsolute = currentTime + breakTime;
            
            ScheduleBreakWarning(BroadcastTimingEventType.Break20Seconds, breakTimeAbsolute - 20);
            ScheduleBreakWarning(BroadcastTimingEventType.Break10Seconds, breakTimeAbsolute - 10);
            ScheduleBreakWarning(BroadcastTimingEventType.Break5Seconds, breakTimeAbsolute - 5);
            ScheduleBreakWarning(BroadcastTimingEventType.Break0Seconds, breakTimeAbsolute);
        }

        /// <summary>
        /// Schedule an individual break warning timer.
        /// </summary>
        private void ScheduleBreakWarning(BroadcastTimingEventType eventType, float targetTime)
        {
            float currentTime = _timeManager?.ElapsedTime ?? 0f;
            
            float waitTime = targetTime - currentTime;
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
        private void StartAdBreakInternal(float duration = 30.0f)
        {
            // Publish start event immediately (no timer operations)
            var startEvent = new BroadcastTimingEvent(BroadcastTimingEventType.AdBreakStart);
            _eventBus.Publish(startEvent);
            
            // Defer timer operations
            CallDeferred(nameof(DeferredStartAdBreak), duration);
        }

        /// <summary>
        /// Deferred timer operations for ad break start (main thread only).
        /// </summary>
        private void DeferredStartAdBreak(float duration)
        {
            // Schedule ad break end
            var endTimer = _timers[BroadcastTimingEventType.AdBreakEnd];
            endTimer.WaitTime = duration;
            endTimer.Start();
        }

        /// <summary>
        /// Stop ad break timing.
        /// </summary>
        private void StopAdBreakInternal()
        {
            // Publish end event immediately
            var endEvent = new BroadcastTimingEvent(BroadcastTimingEventType.AdBreakEnd);
            _eventBus.Publish(endEvent);
            
            // Defer timer operations
            CallDeferred(nameof(DeferredStopAdBreak));
        }

        /// <summary>
        /// Deferred timer operations for ad break stop (main thread only).
        /// </summary>
        private void DeferredStopAdBreak()
        {
            var endTimer = _timers[BroadcastTimingEventType.AdBreakEnd];
            endTimer.Stop();
        }

        /// <summary>
        /// Get remaining time until a specific timing event.
        /// </summary>
        public float GetTimeUntil(BroadcastTimingEventType eventType)
        {
            if (_timers.TryGetValue(eventType, out var timer))
            {
                return (float)timer.TimeLeft;
            }
            return 0f;
        }

        /// <summary>
        /// Public interface for scheduling break warnings.
        /// Called directly by AdManager to avoid EventBus timing issues.
        /// </summary>
        public void ScheduleBreakWarnings(float breakTime)
        {
            ScheduleBreakWarningsInternal(breakTime);
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