using System;
using Godot;
using KBTV.Ads;
using KBTV.Core;
using KBTV.Managers;
using KBTV.Dialogue;

namespace KBTV.Ads
{
    /// <summary>
    /// Manages advertisement breaks during the radio show.
    /// Handles timing, player queuing, mood penalties, and revenue calculation.
    /// </summary>
    public partial class AdManager : Node
    {
        // Dependencies
        private AdSchedule _schedule;
        private TimeManager _timeManager;
        private ListenerManager _listenerManager;
        private BroadcastCoordinator _coordinator;
        private AudioStreamPlayer _transitionMusicPlayer = null!;

        // Modular components
        private BreakScheduler _breakScheduler = null!;
        private BreakLogic _breakLogic = null!;
        private RevenueCalculator _revenueCalculator = null!;

        // State fields
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

        // Clean break queue status
        private enum BreakQueueStatus
        {
            NotQueued,
            Queued
        }
        private BreakQueueStatus _breakQueueStatus = BreakQueueStatus.NotQueued;

        // Event-driven timers (replaces polling)
        private SceneTreeTimer _windowTimer;
        private SceneTreeTimer _graceTimer;
        private SceneTreeTimer _imminentTimer;
        private SceneTreeTimer _breakTimer;

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
        public int CurrentBreakSlots => _schedule != null && _currentBreakIndex >= 0 && _currentBreakIndex < _schedule.Breaks.Count
            ? _schedule.Breaks[_currentBreakIndex].SlotsPerBreak : 0;
        public int CurrentListeners => _listenerManager?.CurrentListeners ?? 100;
        public bool IsInitialized => _schedule != null;

        public override void _Ready()
        {
            ServiceRegistry.Instance.RegisterSelf<AdManager>(this);
            _transitionMusicPlayer = new AudioStreamPlayer();
            AddChild(_transitionMusicPlayer);
        }

        public void Initialize(AdSchedule schedule, float showDuration)
        {
            if (schedule == null)
            {
                GD.PrintErr("AdManager.Initialize() - schedule is null!");
                return;
            }

            _timeManager = ServiceRegistry.Instance.TimeManager;
            _listenerManager = ServiceRegistry.Instance.ListenerManager;
            _coordinator = ServiceRegistry.Instance.BroadcastCoordinator;

            // Initialize modular components
            _breakScheduler = new BreakScheduler(schedule, _timeManager, _currentBreakIndex);
            _breakScheduler.SetCallbacks(OnWindowTimerFired, OnGraceTimerFired, OnImminentTimerFired, OnBreakTimerFired);
            _breakLogic = new BreakLogic();
            _revenueCalculator = new RevenueCalculator();

            // Subscribe to coordinator events for break coordination
            if (_coordinator != null)
            {
                _coordinator.OnBreakTransitionCompleted += OnBreakTransitionCompleted;
            }

            _schedule = schedule;
            _showDuration = showDuration;
            _isActive = true;

            // Generate break schedule if not already done
            if (_schedule.Breaks.Count == 0)
            {
                _schedule.GenerateBreakSchedule(showDuration);
                GD.Print($"AdManager: Generated break schedule with {_schedule.Breaks.Count} breaks");
            }

            // Schedule event-driven timers
            ScheduleBreakTimers();

            // Set initial countdown values for immediate UI display
            UpdateCountdownValues();

            GD.Print($"AdManager: Initialized with {schedule.BreaksPerShow} breaks × {schedule.SlotsPerBreak} slots (event-driven)");
            OnInitialized?.Invoke();
        }

        private void ScheduleBreakTimers()
        {
            CancelTimers();

            if (_schedule == null || _schedule.Breaks.Count == 0)
            {
                GD.Print($"AdManager: Initialized with 0 breaks (show will end immediately) - schedule: {_schedule}, breaks: {_schedule?.Breaks.Count ?? 0}");
                return;
            }

            int nextBreakIndex = _currentBreakIndex + 1;
            if (nextBreakIndex >= _schedule.Breaks.Count)
            {
                GD.Print($"AdManager: All breaks completed - current: {_currentBreakIndex}, total: {_schedule.Breaks.Count}");
                return;
            }

            _breakScheduler.ScheduleBreakTimers(this);
        }

        private SceneTreeTimer CreateBreakTimer(float delay, Action callback)
        {
            var tree = GetTree();
            if (tree == null)
            {
                GD.PrintErr($"AdManager: Cannot create timer - node not in scene tree (delay: {delay})");
                return null;
            }

            try
            {
                var timer = tree.CreateTimer(delay);
                timer.Timeout += () =>
                {
                    try
                    {
                        callback();
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"AdManager: Timer callback failed: {ex.Message}");
                        FallbackBreakStart();
                    }
                };
                GD.Print($"AdManager: Created timer with {delay:F1}s delay");
                return timer;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"AdManager: Failed to create timer (delay: {delay}): {ex.Message}");
                FallbackBreakStart();
                return null;
            }
        }

        private void CancelTimers()
        {
            // SceneTreeTimer doesn't have Dispose method, just set to null
            _windowTimer = null;
            _graceTimer = null;
            _imminentTimer = null;
            _breakTimer = null;
        }

        private void OnWindowTimerFired()
        {
            _isInBreakWindow = true;
            UpdateCountdownValues();
            GD.Print($"AdManager: Break window opened");
            OnBreakWindowOpened?.Invoke(_timeUntilNextBreak);
        }

        private void OnGraceTimerFired()
        {
            UpdateCountdownValues();
            GD.Print($"AdManager: Grace period started");
            OnBreakGracePeriod?.Invoke(_timeUntilNextBreak);
        }

        private void OnImminentTimerFired()
        {
            UpdateCountdownValues();
            GD.Print($"AdManager: Imminent warning");
            OnBreakImminent?.Invoke(_timeUntilNextBreak);
        }

        private void OnBreakTimerFired()
        {
            UpdateCountdownValues();
            GD.Print($"AdManager: Break starting now");
            OnBreakTimeReached();
        }

        private void OnBreakTimeReached()
        {
            GD.Print("AdManager: Break timer fired - starting break");
            StartBreak();
        }

        private void FallbackBreakStart()
        {
            GD.PrintErr("AdManager: Using fallback break start mechanism");
            CallDeferred(nameof(StartBreak));
        }

        /// <summary>
        /// Updates countdown values for UI display (called by timer events)
        /// </summary>
        private void UpdateCountdownValues()
        {
            if (_schedule == null || _schedule.Breaks.Count == 0) return;

            int nextBreakIndex = Math.Max(0, _currentBreakIndex + 1);
            if (nextBreakIndex >= _schedule.Breaks.Count) return;

            float currentTime = _timeManager?.ElapsedTime ?? 0f;
            float nextBreakTime = _schedule.Breaks[nextBreakIndex].ScheduledTime;
            _timeUntilNextBreak = Math.Max(0, nextBreakTime - currentTime);
            _timeUntilBreakWindow = Math.Max(0, _timeUntilNextBreak - AdConstants.BREAK_WINDOW_DURATION);

            // Update queued countdown for UI display
            if (_isQueued && _breakQueueStatus == BreakQueueStatus.Queued)
            {
                _queuedCountdown = _timeUntilNextBreak;
            }

            GD.Print($"AdManager: Countdown updated - Break in {_timeUntilNextBreak:F1}s, Window in {_timeUntilBreakWindow:F1}s");
        }

        private void StartBreak()
        {
            GD.Print($"AdManager.StartBreak: Called, _breakActive={_breakActive}");
            if (_breakActive)
            {
                GD.Print("AdManager.StartBreak: Break already active, returning");
                return;
            }

            GD.Print("AdManager.StartBreak: Starting break...");
            // Breaks always start on schedule - transitions are best-effort and can be interrupted
            _breakActive = true;
            _isInBreakWindow = false;  // Break window closes when break starts
            _currentBreakIndex++;

            GD.Print($"AdManager.StartBreak: _currentBreakIndex now {_currentBreakIndex}, _schedule.Breaks.Count = {_schedule.Breaks.Count}");

            if (_currentBreakIndex >= _schedule.Breaks.Count)
            {
                GD.Print("AdManager.StartBreak: No more breaks in schedule, ending");
                EndAdBreak();
                return;
            }

            var currentBreak = _schedule.Breaks[_currentBreakIndex];
            int slotsInBreak = currentBreak.SlotsPerBreak;
            currentBreak.HasPlayed = true;

            GD.Print($"AdManager.StartBreak: Current break has {slotsInBreak} slots");

            // Apply mood penalty if not queued
            bool wasQueued = (_breakQueueStatus == BreakQueueStatus.Queued);
            if (!wasQueued)
            {
                ApplyMoodPenalty();
            }

            // Reset queue status for next break
            _breakQueueStatus = BreakQueueStatus.NotQueued;

            // Apply listener dip
            ApplyListenerDip();

            // Notify broadcast coordinator
            GD.Print("AdManager.StartBreak: Notifying broadcast coordinator");
            _coordinator.OnAdBreakStarted();

            OnBreakStarted?.Invoke();
            GD.Print($"AdManager: Break #{_currentBreakIndex + 1} started (queued: {wasQueued})");
        }

        private void OnBreakTransitionCompleted()
        {
            GD.Print("AdManager: Received break transition completed event, starting break");
            StartBreak();
        }



        /// <summary>
        /// Called by BroadcastCoordinator when all ads in current break are complete
        /// </summary>
        public void EndCurrentBreak()
        {
            if (_breakActive)
            {
                GD.Print("AdManager: Ending current break via coordinator signal");
                EndAdBreak();
            }
        }

        /// <summary>
        /// End the current ad break and calculate revenue.
        /// </summary>
        public void EndAdBreak()
        {
            if (!_breakActive) return;

            _breakActive = false;
            _breaksPlayed++;

            // Calculate and award revenue
            int currentListeners = _listenerManager?.CurrentListeners ?? 0;
            float revenue = CalculateBreakRevenue(currentListeners);
            AwardRevenue(revenue);

            // Restore listeners
            RestoreListeners();

            // Notify broadcast coordinator
            _coordinator.OnAdBreakEnded();

            // Reset queue state
            _isQueued = false;
            _queuedCountdown = 0f;

            // Update timers for next break
            UpdateBreakTimers();

            if (_breaksPlayed >= _schedule.Breaks.Count)
            {
                _isLastSegment = true;
                LastSegmentStarted?.Invoke();
                _isActive = false;
                OnShowEnded?.Invoke();
                GD.Print("AdManager: All breaks completed - last segment started");
            }

            OnBreakEnded?.Invoke(revenue);
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

            // Play transition music as audio cue to Vern
            var silentStream = GetSilentAudioFile();
            if (silentStream != null)
            {
                _transitionMusicPlayer.Stream = silentStream;
                _transitionMusicPlayer.Play();
            }
            else
            {
                GD.PrintErr("AdManager: Failed to load silent audio file for transition music");
            }

            OnBreakQueued?.Invoke();
            GD.Print($"AdManager: Break queued, {_timeUntilNextBreak:F1}s until break");
        }

        /// <summary>
        /// Loads the 4-second silent WAV file for timing-critical scenarios.
        /// </summary>
        private AudioStream? GetSilentAudioFile()
        {
            var audioStream = GD.Load<AudioStream>("res://assets/audio/silence_4sec.wav");
            if (audioStream == null)
            {
                GD.PrintErr("AdManager.GetSilentAudioFile: Failed to load silent audio file");
                return null;
            }
            return audioStream;
        }

        /// <summary>
        /// Creates a placeholder audio stream for transition music with flexible duration.
        /// </summary>


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
    }
}