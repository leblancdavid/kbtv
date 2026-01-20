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

        private TimeManager _timeManager;
        private ListenerManager _listenerManager;
        private BroadcastCoordinator _coordinator;

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

        public override void _Ready()
        {
            ServiceRegistry.Instance.RegisterSelf<AdManager>(this);
        }

        public void Initialize(AdSchedule schedule, float showDuration)
        {
            _schedule = schedule;
            _showDuration = showDuration;
            _schedule.GenerateBreakSchedule(showDuration);

            _timeManager = ServiceRegistry.Instance.TimeManager;
            _listenerManager = ServiceRegistry.Instance.ListenerManager;
            _coordinator = ServiceRegistry.Instance.BroadcastCoordinator;

            _isActive = true;
            _elapsedTime = 0f;
            _breaksPlayed = 0;
            _currentBreakIndex = -1;
            _breakActive = false;
            _isQueued = false;

            UpdateBreakTimers();
            GD.Print($"AdManager: Initialized with {schedule.BreaksPerShow} breaks × {schedule.SlotsPerBreak} slots");
        }

        public override void _Process(double delta)
        {
            if (!_isActive || _breakActive) return;

            float deltaTime = (float)delta;
            _elapsedTime += deltaTime;
            _timeUntilNextBreak -= deltaTime;
            _timeUntilBreakWindow -= deltaTime;

            // Check if we're entering break window
            if (!_isInBreakWindow && _timeUntilBreakWindow <= 0 && _currentBreakIndex < _schedule.Breaks.Count - 1)
            {
                _isInBreakWindow = true;
                OnBreakWindowOpened?.Invoke(_timeUntilNextBreak);
            }

            // Check for break start
            if (_timeUntilNextBreak <= 0)
            {
                StartBreak();
            }
            else if (_isQueued)
            {
                _queuedCountdown = _timeUntilNextBreak;

                // Grace period (T-10s)
                if (_timeUntilNextBreak <= AdConstants.BREAK_GRACE_TIME && _timeUntilNextBreak > AdConstants.BREAK_IMMINENT_TIME)
                {
                    OnBreakGracePeriod?.Invoke(_timeUntilNextBreak);
                }

                // Imminent (T-5s) - fallback only
                if (_timeUntilNextBreak <= AdConstants.BREAK_IMMINENT_TIME)
                {
                    OnBreakImminent?.Invoke(_timeUntilNextBreak);
                }
            }
        }

        /// <summary>
        /// Player clicks "Queue Ads" button - queues the next break.
        /// </summary>
        public void QueueBreak()
        {
            if (!_isActive || _isInBreakWindow == false || _isQueued) return;

            _isQueued = true;
            _queuedCountdown = _timeUntilNextBreak;

            if (_currentBreakIndex >= 0 && _currentBreakIndex < _schedule.Breaks.Count)
            {
                _schedule.Breaks[_currentBreakIndex].WasQueued = true;
            }

            OnBreakQueued?.Invoke();
            GD.Print($"AdManager: Break queued, {_timeUntilNextBreak:F1}s until break");
        }

        /// <summary>
        /// Start the ad break at the scheduled time.
        /// </summary>
        private void StartBreak()
        {
            if (_breakActive) return;

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
            bool wasQueued = currentBreak.WasQueued;
            if (!wasQueued)
            {
                ApplyMoodPenalty();
            }

            // Apply listener dip
            ApplyListenerDip();

            // Notify broadcast coordinator
            _coordinator.OnAdBreakStarted();

            OnBreakStarted?.Invoke();
            GD.Print($"AdManager: Break #{_currentBreakIndex + 1} started (queued: {wasQueued})");
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
            if (_isInBreakWindow) return "QUEUE ADS";
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
