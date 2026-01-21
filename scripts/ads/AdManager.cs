using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KBTV.Core;
using KBTV.Data;
using KBTV.Dialogue;
using KBTV.Economy;
using KBTV.Managers;

namespace KBTV.Ads
{
    /// <summary>
    /// Autoload service that manages ad break scheduling, timing, and execution.
    /// </summary>
    [GlobalClass]
    public partial class AdManager : Node
    {
        private AdSchedule _schedule;
        private float _showDuration = AdConstants.SHOW_DURATION_SECONDS;
        private float _elapsedTime = 0f;
        private bool _isActive = false;
        private bool _breakActive = false;
        private int _currentBreakIndex = -1;
        private float _timeUntilNextBreak = 0f;
        private float _timeUntilBreakWindow = 0f;
        private bool _isInBreakWindow = false;
        private bool _isQueued = false;
        private float _queuedCountdown = 0f;
        private int _breaksPlayed = 0;

        // State tracking for one-shot event firing
        private bool _gracePeriodActive = false;
        private bool _imminentActive = false;

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

        private TimeManager _timeManager;
        private ListenerManager _listenerManager;
        private BroadcastCoordinator _coordinator;
        private AudioStreamPlayer _transitionMusicPlayer = null!;

        // Events
        public event Action<float> OnBreakWindowOpened;      // Time until break
        public event Action<float> OnBreakGracePeriod;       // Time until break
        public event Action<float> OnBreakImminent;          // Time until break
        public event Action OnBreakQueued;                   // Player queued the break
        public event Action OnBreakStarted;                  // Break audio starting
        public event Action<float> OnBreakEnded;             // Revenue generated
        public event Action OnShowEnded;                     // All breaks complete

        public AdSchedule Schedule => _schedule;
        public int BreaksRemaining => _schedule.Breaks.Count - _breaksPlayed;
        public bool IsAdBreakActive => _breakActive;
        public bool IsActive => _isActive;
        public bool IsInBreakWindow => _isInBreakWindow;
        public bool IsQueued => _isQueued;
        public float TimeUntilNextBreak => _timeUntilNextBreak;
        public float TimeUntilBreakWindow => _timeUntilBreakWindow;
        public float QueuedCountdown => _queuedCountdown;
        public int CurrentBreakSlots => _currentBreakIndex >= 0 && _currentBreakIndex < _schedule.Breaks.Count
            ? _schedule.Breaks[_currentBreakIndex].SlotsPerBreak : 0;

        public override void _Ready()
        {
            ServiceRegistry.Instance.RegisterSelf<AdManager>(this);
            _transitionMusicPlayer = new AudioStreamPlayer();
            AddChild(_transitionMusicPlayer);
        }

        private void ScheduleBreakTimers()
        {
            CancelTimers();

            if (_schedule == null || _schedule.Breaks.Count == 0) return;

            float currentTime = _timeManager?.ElapsedTime ?? 0f;
            int nextBreakIndex = _currentBreakIndex + 1;

            if (nextBreakIndex >= _schedule.Breaks.Count) return;

            float breakTime = _schedule.Breaks[nextBreakIndex].ScheduledTime;

            // Schedule window opening
            float windowDelay = breakTime - AdConstants.BREAK_WINDOW_DURATION - currentTime;
            if (windowDelay > 0)
            {
                _windowTimer = CreateBreakTimer(windowDelay, () => HandleBreakWindowOpened(breakTime - currentTime));
            }

            // Schedule grace period start
            float graceDelay = breakTime - AdConstants.BREAK_GRACE_TIME - currentTime;
            if (graceDelay > 0)
            {
                _graceTimer = CreateBreakTimer(graceDelay, () => OnBreakGracePeriod(breakTime - currentTime));
            }

            // Schedule imminent warning
            float imminentDelay = breakTime - AdConstants.BREAK_IMMINENT_TIME - currentTime;
            if (imminentDelay > 0)
            {
                _imminentTimer = CreateBreakTimer(imminentDelay, () => OnBreakImminent(breakTime - currentTime));
            }

            // Schedule break start
            float breakDelay = breakTime - currentTime;
            if (breakDelay > 0)
            {
                _breakTimer = CreateBreakTimer(breakDelay, OnBreakTimeReached);
            }
        }

        private SceneTreeTimer CreateBreakTimer(float delay, Action callback)
        {
            try
            {
                var timer = GetTree().CreateTimer(delay);
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

        private void OnBreakTimeReached()
        {
            GD.Print("AdManager: Break timer fired - starting break");
            StartBreak();
        }

        private void HandleBreakWindowOpened(float timeUntilBreak)
        {
            _isInBreakWindow = true;
            OnBreakWindowOpened?.Invoke(timeUntilBreak);
            GD.Print($"AdManager: Break window opened, {timeUntilBreak:F1}s until break");
        }

        private void FallbackBreakStart()
        {
            GD.PrintErr("AdManager: Using fallback break start mechanism");
            CallDeferred(nameof(StartBreak));
        }

        public void Initialize(AdSchedule schedule, float showDuration)
        {
            _schedule = schedule;
            _showDuration = showDuration;
            _isActive = true;

            // Schedule event-driven timers
            ScheduleBreakTimers();

            GD.Print($"AdManager: Initialized with {schedule.BreaksPerShow} breaks × {schedule.SlotsPerBreak} slots (event-driven)");
        }

        public override void _Process(double delta)
        {
            // Event-driven system - minimal processing needed
            if (!_isActive) return;

            float deltaTime = (float)delta;
            _elapsedTime += deltaTime;

            // Update queued countdown for UI display
            if (_isQueued && _breakQueueStatus == BreakQueueStatus.Queued)
            {
                // Calculate remaining time to next break (approximate for UI)
                int nextBreakIndex = Math.Max(0, _currentBreakIndex + 1);
                if (nextBreakIndex < _schedule?.Breaks.Count)
                {
                    float breakTime = _schedule.Breaks[nextBreakIndex].ScheduledTime;
                    _queuedCountdown = Math.Max(0, breakTime - _elapsedTime);
                }
            }
        }

        /// <summary>
        /// Creates a silent audio stream for placeholder transition music.
        /// </summary>
        private AudioStream CreateSilentAudioStream(float duration)
        {
            var sampleStream = new AudioStreamGenerator
            {
                MixRate = 44100
            };
            return sampleStream;
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
            var silentStream = CreateSilentAudioStream(4.0f);
            _transitionMusicPlayer.Stream = silentStream;
            _transitionMusicPlayer.Play();

            OnBreakQueued?.Invoke();
            GD.Print($"AdManager: Break queued, {_timeUntilNextBreak:F1}s until break");
        }

        /// <summary>
        /// Start the ad break at the scheduled time.
        /// </summary>
        private void StartBreak()
        {
            if (_breakActive) return;

            // Check if break transition is still in progress - delay break start
            if (_coordinator?.CurrentState == KBTV.Dialogue.BroadcastCoordinator.BroadcastState.BreakTransition)
            {
                GD.Print("AdManager: Delaying break start - transition in progress");
                return;
            }

            _breakActive = true;
            _isInBreakWindow = false;
            _currentBreakIndex++;

            if (_currentBreakIndex >= _schedule.Breaks.Count)
            {
                EndAdBreak();
                return;
            }

            var currentBreak = _schedule.Breaks[_currentBreakIndex];
            currentBreak.HasPlayed = true;

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
            _coordinator.OnAdBreakStarted();

            OnBreakStarted?.Invoke();
            GD.Print($"AdManager: Break #{_currentBreakIndex + 1} started (queued: {wasQueued})");
        }

        private void OnBreakTransitionCompleted()
        {
            GD.Print("AdManager: Received break transition completed event, starting break");
            StartBreakImmediately();
        }

        private void StartBreakImmediately()
        {
            // Force start the break now that transition is complete
            var currentBreak = _schedule.Breaks[_currentBreakIndex];
            bool wasQueued = (_breakQueueStatus == BreakQueueStatus.Queued);
            if (!wasQueued)
            {
                ApplyMoodPenalty();
            }

            // Apply listener dip
            ApplyListenerDip();

            // Notify broadcast coordinator
            _coordinator.OnAdBreakStarted();

            OnBreakStarted?.Invoke();
            GD.Print($"AdManager: Break #{_currentBreakIndex + 1} started after transition (queued: {wasQueued})");

            // Reset queue status for next break
            _breakQueueStatus = BreakQueueStatus.NotQueued;
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
                _isActive = false;
                OnShowEnded?.Invoke();
                GD.Print("AdManager: All breaks completed");
            }
            else
            {
                OnBreakEnded?.Invoke(revenue);
            }

            GD.Print($"AdManager: Break ended, revenue: ${revenue:F2}");
        }

        /// <summary>
        /// Calculate revenue for the current break based on listeners and ad types.
        /// For now, uses random ad types for variety.
        /// </summary>
        public float CalculateBreakRevenue(int listenerCount)
        {
            if (_currentBreakIndex < 0 || _currentBreakIndex >= _schedule.Breaks.Count) return 0f;

            var breakConfig = _schedule.Breaks[_currentBreakIndex];
            int totalSlots = breakConfig.SlotsPerBreak;

            // For now, use a mix of ad types based on listener count
            AdType adType = DetermineAdType(listenerCount);

            float totalRevenue = 0f;
            for (int i = 0; i < totalSlots; i++)
            {
                totalRevenue += listenerCount * AdData.GetRevenueRate(adType);
            }

            return totalRevenue;
        }

        private AdType DetermineAdType(int listenerCount)
        {
            // Simple tiered system based on listener count
            if (listenerCount >= 1000) return AdType.PremiumSponsor;
            if (listenerCount >= 500) return AdType.NationalSponsor;
            if (listenerCount >= 200) return AdType.RegionalBrand;
            return AdType.LocalBusiness;
        }

        private void AwardRevenue(float amount)
        {
            var economy = ServiceRegistry.Instance.EconomyManager;
            if (economy != null)
            {
                economy.AddMoney((int)amount, "Ad Revenue");
            }
        }

        private void ApplyMoodPenalty()
        {
            var vernStats = ServiceRegistry.Instance.GameStateManager?.VernStats;
            if (vernStats != null)
            {
                vernStats.Patience.Modify(-AdConstants.UNQUEUED_MOOD_PENALTY);
                GD.Print($"AdManager: Applied {AdConstants.UNQUEUED_MOOD_PENALTY} mood penalty (break not queued)");
            }
        }

        private void ApplyListenerDip()
        {
            var listenerMgr = ServiceRegistry.Instance.ListenerManager;
            if (listenerMgr != null)
            {
                int current = listenerMgr.CurrentListeners;
                int dip = (int)(current * AdConstants.LISTENER_DIP_PERCENTAGE);
                listenerMgr.ModifyListeners(-dip);
            }
        }

        private void RestoreListeners()
        {
            var listenerMgr = ServiceRegistry.Instance.ListenerManager;
            if (listenerMgr != null)
            {
                int current = listenerMgr.CurrentListeners;
                int restore = (int)(current * AdConstants.LISTENER_DIP_PERCENTAGE);
                listenerMgr.ModifyListeners(restore);
            }
        }

        private void UpdateBreakTimers()
        {
            int nextBreakIndex = _currentBreakIndex + 1;

            if (nextBreakIndex >= _schedule.Breaks.Count)
            {
                _timeUntilNextBreak = 0f;
                _timeUntilBreakWindow = 0f;
                _isInBreakWindow = false;
                return;
            }

            var nextBreak = _schedule.Breaks[nextBreakIndex];
            _timeUntilNextBreak = nextBreak.ScheduledTime - _elapsedTime;
            _timeUntilBreakWindow = _timeUntilNextBreak - AdConstants.BREAK_WINDOW_DURATION;
            _isInBreakWindow = false;
        }

        /// <summary>
        /// Get the next scheduled break time, or -1 if no more breaks.
        /// </summary>
        public float GetNextBreakTime()
        {
            int nextBreakIndex = _currentBreakIndex + 1;
            if (nextBreakIndex >= _schedule.Breaks.Count) return -1f;
            return _schedule.Breaks[nextBreakIndex].ScheduledTime;
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
                int seconds = (int)_timeUntilBreakWindow;
                return $"BREAK IN {seconds / 60}:{seconds % 60:D2}";
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
