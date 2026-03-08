using System;
using Godot;
using KBTV.Ads;
using KBTV.Core;
using KBTV.Economy;
using KBTV.Managers;
using KBTV.Dialogue;
using KBTV.Audio;

namespace KBTV.Ads
{
    /// <summary>
    /// Manages advertisement breaks during the radio show.
    /// Handles timing, player queuing, mood penalties, and revenue calculation.
    /// Converted to AutoInject Provider pattern.
    /// </summary>
    public partial class AdManager : Node,
        IProvide<AdManager>,
        IDependent
    {
        public override void _Notification(int what) => this.Notify(what);

        private TimeManager TimeManager => DependencyInjection.Get<TimeManager>(this);

        private ListenerManager ListenerManager => DependencyInjection.Get<ListenerManager>(this);

        private EconomyManager EconomyManager => DependencyInjection.Get<EconomyManager>(this);

        private GameStateManager GameStateManager => DependencyInjection.Get<GameStateManager>(this);

        private IBroadcastAudioService BroadcastAudioService => DependencyInjection.Get<IBroadcastAudioService>(this);

        private AdSchedule _schedule;
        private float _showDuration = 0f;
        private float _timeUntilNextBreak = 0f;
        private float _timeUntilBreakWindow = 0f;
        private bool _isInBreakWindow = false;
        private bool _isQueued = false;
        private float _queuedCountdown = 0f;
        private int _breaksPlayed = 0;
        private bool _isActive = false;
        private int _currentBreakIndex = -1;
        private bool _breakActive = false;
        private bool _isLastSegment = false;
        private bool _breakTransitionCompleted = false;

        // Clean break queue status
        private enum BreakQueueStatus
        {
            NotQueued,
            Queued
        }
        private BreakQueueStatus _breakQueueStatus = BreakQueueStatus.NotQueued;

        // Modular components (restored from refactoring)
        private BreakScheduler _breakScheduler;
        private BreakLogic _breakLogic;
        private RevenueCalculator _revenueCalculator;

        // Events
        public event Action<float> OnBreakWindowOpened;      // Time until break
        public event Action<float> OnBreakGracePeriod;       // Time until break
        public event Action<float> OnBreakImminent;          // Time until break
        public event Action OnBreakQueued;                   // Player queued the break
        public event Action OnBreakStarted;                  // Break audio starting
        public event Action<float> OnBreakEnded;             // Revenue generated
        public event Action OnInitialized;
        public event Action OnShowEnded;                     // All breaks complete
        public event Action LastSegmentStarted;              // After last ad break
        public event Action OnBreakReady;                    // Break started and transition completed

        public AdSchedule Schedule => _schedule;
        public int BreaksRemaining => _schedule != null ? _schedule.Breaks.Count - _breaksPlayed : 0;
        public bool IsAdBreakActive => _breakActive;
        public bool IsActive => _isActive;
        public bool IsInBreakWindow => _isInBreakWindow;
        public bool IsQueued => _isQueued;
        public bool IsLastSegment => _isLastSegment;
        public float TimeUntilNextBreak => _timeUntilNextBreak;
        public float TimeUntilBreakWindow => _timeUntilBreakWindow;
        public float QueuedCountdown => _queuedCountdown;
        public int CurrentBreakSlots
        {
            get
            {
                return _schedule != null && _currentBreakIndex >= 0 && _currentBreakIndex < _schedule.Breaks.Count
                    ? _schedule.Breaks[_currentBreakIndex].SlotsPerBreak : 0;
            }
        }
        public int CurrentListeners => ListenerManager.CurrentListeners;
        public bool IsInitialized => _schedule != null;

        private EventBus _eventBus = null!;

        // Provider interface implementation
        AdManager IProvide<AdManager>.Value() => this;

        /// <summary>
        /// Called when all dependencies are resolved.
        /// </summary>
        public void OnResolved()
        {
            // Initialize modular components
            _breakLogic = new BreakLogic(GameStateManager, ListenerManager);
            _revenueCalculator = new RevenueCalculator(EconomyManager);

            // Subscribe to broadcast timing events
            _eventBus = DependencyInjection.Get<EventBus>(this);
            _eventBus.Subscribe<BroadcastTimingEvent>(HandleBroadcastTimingEvent);
        }

        /// <summary>
        /// Called when node enters the scene tree and is ready.
        /// </summary>
        public void OnReady()
        {
            Log.Debug("AdManager: Ready, providing service to descendants");
            this.Provide();
        }

        public void Initialize(AdSchedule schedule, float showDuration)
        {
            if (schedule == null)
            {
                Log.Error("AdManager.Initialize() - schedule is null!");
                return;
            }

            // Reset state for new show
            _currentBreakIndex = -1;
            _breaksPlayed = 0;
            _breakActive = false;
            _isInBreakWindow = false;
            _isQueued = false;
            _queuedCountdown = 0f;
            _breakQueueStatus = BreakQueueStatus.NotQueued;
            _isLastSegment = false;
            _breakTransitionCompleted = false;

            _schedule = schedule;
            _showDuration = showDuration;
            _isActive = true;
            
            // Initialize BreakScheduler with required parameters
            _breakScheduler = new BreakScheduler(schedule, TimeManager, _currentBreakIndex);

            // Generate break schedule if not already done
            if (_schedule.Breaks.Count == 0)
            {
                _schedule.GenerateBreakSchedule(showDuration);
                Log.Debug($"AdManager: Generated break schedule with {_schedule.Breaks.Count} breaks");
            }

            // Validate break count matches expected BreaksPerShow
            if (_schedule.Breaks.Count != _schedule.BreaksPerShow)
            {
                Log.Error($"AdManager: Schedule break count mismatch! Expected {_schedule.BreaksPerShow}, got {_schedule.Breaks.Count}. Using generated count.");
            }

            Log.Debug($"AdManager: Initialized with {_schedule.BreaksPerShow} breaks requested, {_schedule.Breaks.Count} breaks scheduled, {schedule.SlotsPerBreak} slots per break (event-driven)");

            // Schedule event-driven timers
            ScheduleBreakTimers();

            // Set initial countdown values for immediate UI display
            UpdateCountdownValues();

            OnInitialized?.Invoke();
        }

        private void ScheduleBreakTimers()
        {
            if (_schedule == null || _schedule.Breaks.Count == 0)
            {
                Log.Debug($"AdManager: ScheduleBreakTimers - 0 breaks in schedule - schedule: {_schedule}, breaks: {_schedule?.Breaks.Count ?? 0}");
                return;
            }

            int nextBreakIndex = _currentBreakIndex + 1;
            if (nextBreakIndex >= _schedule.Breaks.Count)
            {
                Log.Debug($"AdManager: ScheduleBreakTimers - All breaks completed (current: {_currentBreakIndex}, total: {_schedule.Breaks.Count})");
                return;
            }

            // Calculate time until next break using game time
            float nextBreakTime = _schedule.Breaks[nextBreakIndex].ScheduledTime;
            float currentTime = TimeManager?.ElapsedTime ?? 0f;
            float timeUntilBreak = nextBreakTime - currentTime;

            Log.Debug($"AdManager: ScheduleBreakTimers - Next break #{nextBreakIndex + 1} at show time {nextBreakTime:F1}s (current: {currentTime:F1}s, wait: {timeUntilBreak:F1}s)");

            if (timeUntilBreak > 0)
            {
                // Schedule timing events directly with BroadcastTimer to avoid EventBus timing issues
                var broadcastTimer = DependencyInjection.Get<BroadcastTimer>(this);
                broadcastTimer.ScheduleBreakWarnings(timeUntilBreak);
                Log.Debug($"AdManager: Scheduled break warnings for {timeUntilBreak:F1}s from now");
            }
            else
            {
                Log.Warning($"AdManager: ScheduleBreakTimers - Next break time {nextBreakTime:F1} is in the past (current: {currentTime:F1}) - break will be triggered immediately!");
            }
        }

        /// <summary>
        /// Handle broadcast timing events to integrate with AsyncBroadcastLoop.
        /// </summary>
        private void HandleBroadcastTimingEvent(BroadcastTimingEvent timingEvent)
        {
            switch (timingEvent.Type)
            {
                case BroadcastTimingEventType.Break20Seconds:
                    // Only open break window if there are remaining breaks
                    if (_schedule != null && _currentBreakIndex + 1 < _schedule.Breaks.Count)
                    {
                        _isInBreakWindow = true;
                        UpdateCountdownValues();
                        OnBreakWindowOpened?.Invoke(_timeUntilNextBreak);
                        Log.Debug($"AdManager: Break window opened (T-20s). Next break index: {_currentBreakIndex + 1}, total breaks: {_schedule.Breaks.Count}");
                    }
                    else
                    {
                        Log.Debug("AdManager: T-20s event but no remaining breaks. Skipping window open.");
                    }
                    break;
                case BroadcastTimingEventType.Break0Seconds:
                    // Break starts now - StartBreak() moved to AdBreak state initialization
                    // _currentBreakIndex++ moved to HandleBroadcastTimingEvent for immediate update
                    int oldIndex = _currentBreakIndex;
                    
                    // Guard against incrementing beyond scheduled breaks
                    if (oldIndex + 1 >= _schedule?.Breaks.Count)
                    {
                        Log.Warning($"AdManager: T-0s event but already at or beyond last break (index={oldIndex}, total={_schedule?.Breaks.Count ?? 0}). Not incrementing.");
                        break;
                    }
                    
                    _currentBreakIndex++;
                    Log.Debug($"AdManager: T-0s event - incrementing break index from {oldIndex} to {_currentBreakIndex} (total scheduled: {_schedule?.Breaks.Count ?? 0})");
                    break;
            }
        }

        public void StartBreak()
        {
            if (_breakActive)
            {
                Log.Warning($"AdManager.StartBreak: Called but break already active (index {_currentBreakIndex})");
                return;
            }

            Log.Debug($"AdManager.StartBreak: Starting break at index {_currentBreakIndex}, total breaks: {_schedule?.Breaks.Count ?? 0}");

            // Validate break index before proceeding
            if (_currentBreakIndex < 0 || _currentBreakIndex >= (_schedule?.Breaks.Count ?? 0))
            {
                Log.Error($"AdManager.StartBreak: Invalid break index {_currentBreakIndex} (total: {_schedule?.Breaks.Count ?? 0}). Ending break immediately.");
                EndAdBreak();
                return;
            }

            // Stop the transition music when break starts
            if (BroadcastAudioService is BroadcastAudioService bas)
            {
                bas.StopBreakTransitionMusic();
            }

            // Breaks always start on schedule - transitions are best-effort and can be interrupted
            _breakActive = true;
            _isInBreakWindow = false;  // Break window closes when break starts
            // _currentBreakIndex++ moved to HandleBroadcastTimingEvent for immediate update

            // Update break scheduler with new index
            _breakScheduler.UpdateCurrentBreakIndex(_currentBreakIndex);

            var currentBreak = _schedule.Breaks[_currentBreakIndex];
            int slotsInBreak = currentBreak.SlotsPerBreak;
            currentBreak.HasPlayed = true;

            Log.Debug($"AdManager.StartBreak: Break {_currentBreakIndex + 1}/{_schedule.Breaks.Count} has {slotsInBreak} ad slots");

            // Apply mood penalty if not queued
            bool wasQueued = (_breakQueueStatus == BreakQueueStatus.Queued);
            if (!wasQueued)
            {
                ApplyMoodPenalty();
                Log.Debug("AdManager.StartBreak: Applied unqueued mood penalty");
            }

            // Reset queue status for next break
            _breakQueueStatus = BreakQueueStatus.NotQueued;

            // Apply listener dip
            ApplyListenerDip();

            OnBreakStarted?.Invoke();
        }

        private void OnBreakTransitionCompleted()
        {
            // Check if break time has already arrived - if so, proceed to ads
            if (_breakActive)
            {
                OnBreakReady?.Invoke();
            }
        }



        /// <summary>
        /// Called by BroadcastCoordinator when all ads in current break are complete
        /// </summary>
        public void EndCurrentBreak()
        {
            if (_breakActive)
            {
                EndAdBreak();
            }
        }

        /// <summary>
        /// End the current ad break and calculate revenue.
        /// </summary>
        public void EndAdBreak()
        {
            if (!_breakActive) 
            {
                Log.Warning($"AdManager.EndAdBreak: Called but no break active (breaksPlayed: {_breaksPlayed})");
                return;
            }

            _breakActive = false;
            _breaksPlayed++;

            Log.Debug($"AdManager.EndAdBreak: Completed break {_breaksPlayed}/{_schedule.Breaks.Count}");

            // Reset transition completed flag for next break
            _breakTransitionCompleted = false;
 
            // Calculate and award revenue
            int currentListeners = ListenerManager?.CurrentListeners ?? 0;
            float revenue = CalculateBreakRevenue(currentListeners);
            AwardRevenue(revenue);

            Log.Debug($"AdManager.EndAdBreak: Awarded ${revenue:F2} revenue from {currentListeners} listeners");

            // Restore listeners
            RestoreListeners();

            // Reset queue state
            _isQueued = false;
            _queuedCountdown = 0f;

            // Reset break window for next break
            _isInBreakWindow = false;

            // Update timers for next break
            UpdateBreakTimers();

            if (_breaksPlayed >= _schedule.Breaks.Count)
            {
                Log.Debug($"AdManager.EndAdBreak: All {_schedule.Breaks.Count} breaks completed! Setting last segment. breaksPlayed={_breaksPlayed}, count={_schedule.Breaks.Count}");
                _isLastSegment = true;
                LastSegmentStarted?.Invoke();
                _isActive = false;
                OnShowEnded?.Invoke();
            }
            else
            {
                Log.Debug($"AdManager.EndAdBreak: {_schedule.Breaks.Count - _breaksPlayed} breaks remaining");
            }

            OnBreakEnded?.Invoke(revenue);
        }

        public override void _Process(double delta)
        {
            if (!_isActive || _schedule == null)
            {
                return;
            }

            UpdateCountdownValues();

            if (_isQueued)
            {
                _queuedCountdown = Mathf.Max(0f, _timeUntilNextBreak);
            }
        }

        /// <summary>
        /// Player clicks "Queue Ads" button - queues the next break.
        /// </summary>
        public void QueueBreak()
        {
            if (!_isActive || _isInBreakWindow == false || _breakQueueStatus == BreakQueueStatus.Queued) return;

            _isQueued = true;
            _queuedCountdown = _timeUntilNextBreak;
            _breakQueueStatus = BreakQueueStatus.Queued;

            // Play random transition music as audio cue to Vern
            if (BroadcastAudioService is BroadcastAudioService bas)
            {
                bas.PlayBreakTransitionMusic();
            }

            OnBreakQueued?.Invoke();
        }

        /// <summary>
        /// Update countdown values for UI display.
        /// </summary>
        private void UpdateCountdownValues()
        {
            if (_breakScheduler != null)
            {
                float currentTime = TimeManager?.ElapsedTime ?? 0f;
                float nextBreakTime = _breakScheduler.GetNextBreakTime();
                _timeUntilNextBreak = nextBreakTime > 0 ? nextBreakTime - currentTime : 0f;
                _timeUntilBreakWindow = nextBreakTime > 0 ? (nextBreakTime - AdConstants.BREAK_WINDOW_DURATION) - currentTime : 0f;
            }
        }

        private void ApplyMoodPenalty()
        {
            _breakLogic.ApplyUnqueuedPenalty();
        }

        private void ApplyListenerDip()
        {
            _breakLogic.ApplyListenerDip();
        }

        private void RestoreListeners()
        {
            _breakLogic.RestoreListeners();
        }

        private float CalculateBreakRevenue(int currentListeners)
        {
            if (_currentBreakIndex < 0 || _currentBreakIndex >= _schedule.Breaks.Count) return 0f;
            var breakConfig = _schedule.Breaks[_currentBreakIndex];
            return _revenueCalculator.CalculateBreakRevenue(currentListeners, breakConfig);
        }

        private void AwardRevenue(float revenue)
        {
            _revenueCalculator.AwardRevenue(revenue);
        }

        private void UpdateBreakTimers()
        {
            // Schedule timers for the next break
            ScheduleBreakTimers();
        }

        /// <summary>
        /// Get the next scheduled break time, or -1 if no more breaks.
        /// </summary>
        public float GetNextBreakTime()
        {
            return _breakScheduler?.GetNextBreakTime() ?? -1f;
        }

        /// <summary>
        /// Get the state of the queue button for UI display.
        /// </summary>
        public string GetQueueButtonText()
        {
            if (!_isActive) return "NO BREAKS";
            if (_breakActive) return "ON BREAK";
            if (_isQueued) return $"QUEUED {_queuedCountdown:F1}";
            if (_isInBreakWindow) return "QUEUE AD-BREAK";
            if (_timeUntilBreakWindow > 0)
            {
                return _breakScheduler?.GetQueueButtonText(_timeUntilBreakWindow, _timeUntilNextBreak, _isQueued) ?? "BREAK SOON";
            }
            return "BREAK SOON";
        }

        /// <summary>
        /// Check if the queue button should be enabled.
        /// </summary>
        public bool IsQueueButtonEnabled()
        {
            return _isActive && !_breakActive && _isInBreakWindow && !_isQueued;
        }

        public override void _ExitTree()
        {
            // Unsubscribe from events to prevent memory leaks
            _eventBus?.Unsubscribe<BroadcastTimingEvent>(HandleBroadcastTimingEvent);
            
            base._ExitTree();
        }
    }
}
