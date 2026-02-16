using System;
using Godot;
using KBTV.Core;
using KBTV.Data;
using KBTV.Callers;
using KBTV.Persistence;

namespace KBTV.Managers
{
    /// <summary>
    /// Tracks listener count during live shows.
    /// Listener count fluctuates based on VIBE (sigmoid curve) and caller quality.
    /// See docs/VERN_STATS.md for VIBE documentation.
    /// </summary>
   	public partial class ListenerManager : Node
   	{
        [Signal] public delegate void ListenersChangedEventHandler(int oldCount, int newCount);
		[Signal] public delegate void PeakReachedEventHandler(int newPeak);
        [Export] private int _baseListeners = 1000;
        [Export] private int _listenerVariance = 200;
        [Export] private int _greatCallerBonus = 150;
        [Export] private int _goodCallerBonus = 50;
        [Export] private int _badCallerPenalty = 100;
        [Export] private int _disconnectPenalty = 25;

        // Periodic update settings
        [Export] private float _updateIntervalSeconds = 1.5f;
        [Export] private float _noisePercent = 0.03f;
        [Export] private float _driftPercent = 0.2f;
        [Export] private float _spikeChance = 0.08f;
        [Export] private float _spikePercentMin = 0.01f;
        [Export] private float _spikePercentMax = 0.03f;
        [Export] private float _vibeInfluencePercent = 0.15f;

        // Runtime state
        private int _currentListeners;
        private int _peakListeners;
        private int _startingListeners;
        private float _timeSinceLastUpdate;
        private int _stationReach;

        public int CurrentListeners => _currentListeners;
        public int PeakListeners => _peakListeners;
        public int StartingListeners => _startingListeners;
        public int MaxListeners => _stationReach;

        /// <summary>
        /// Listener change since show started. Can be negative.
        /// </summary>
        public int ListenerChange => _currentListeners - _startingListeners;

        private readonly IGameStateManager _gameState;
        private readonly ITimeManager _timeManager;
        private readonly ICallerRepository _repository;
        private readonly SaveManager _saveManager;

        private bool _initialized;

        public ListenerManager(IGameStateManager gameState, ITimeManager timeManager, ICallerRepository repository, SaveManager saveManager)
        {
            _gameState = gameState;
            _timeManager = timeManager;
            _repository = repository;
            _saveManager = saveManager;
        }

        public override void _Ready()
        {
            // RegisterSelf removed - now using dependency injection
            CompleteInitialization();
        }

        /// <summary>
        /// Initialize the ListenerManager with service dependencies.
        /// </summary>
        public void Initialize()
        {
            if (!_initialized)
            {
                CompleteInitialization();
            }
        }

        private void CompleteInitialization()
        {
            if (_gameState == null || _timeManager == null || _repository == null)
            {
                Log.Error("ListenerManager: Required services not available - check autoload order");
                return;
            }

            _gameState.OnPhaseChanged += HandlePhaseChanged;
            _timeManager.OnTick += HandleTick;
            _initialized = true;
        }

        public override void _ExitTree()
        {
            if (_gameState != null)
            {
                _gameState.OnPhaseChanged -= HandlePhaseChanged;
            }

            if (_timeManager != null)
            {
                _timeManager.OnTick -= HandleTick;
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
            // Calculate station reach from save data
            RefreshStationReach();

            // Calculate starting listeners with some variance
            int variance = (int)GD.RandRange(-_listenerVariance, _listenerVariance + 1);
            _startingListeners = Mathf.Max(0, _baseListeners + variance);
            _currentListeners = _startingListeners;
            _peakListeners = _startingListeners;
            _timeSinceLastUpdate = 0f;

            EmitSignal("ListenersChanged", 0, _currentListeners);
        }

        private void HandleTick(float deltaTime)
        {
            if (_gameState == null || !_gameState.IsLive) return;

            VernStats stats = _gameState.VernStats;
            if (stats == null) return;

            _timeSinceLastUpdate += deltaTime;

            if (_timeSinceLastUpdate >= _updateIntervalSeconds)
            {
                _timeSinceLastUpdate = 0f;
                UpdateListenersPeriodic(stats);
            }
        }

        private void UpdateListenersPeriodic(VernStats stats)
        {
            // Get VIBE (-100 to +100)
            float vibe = stats.CalculateVIBE();

            // Calculate min/max bounds - min is 0, max is station reach
            int minListeners = 0;
            int maxListeners = _stationReach;

            // VIBE directly influences target: +100 VIBE pushes toward max, -100 toward min
            float normalizedVibe = vibe / 100f; // -1 to +1
            float vibeTargetOffset = normalizedVibe * _vibeInfluencePercent * _baseListeners;
            int vibeBasedTarget = _currentListeners + Mathf.RoundToInt(vibeTargetOffset * _updateIntervalSeconds * 2f);
            vibeBasedTarget = Mathf.Clamp(vibeBasedTarget, minListeners, maxListeners);

            // Add percentage-based noise
            float noiseRange = _noisePercent * _currentListeners;
            int noise = (int)GD.RandRange(-noiseRange, noiseRange);
            int noisyTarget = vibeBasedTarget + noise;

            // Drift toward target (increased for more responsiveness)
            float driftAmount = (noisyTarget - _currentListeners) * _driftPercent;
            int newListeners = _currentListeners + Mathf.RoundToInt(driftAmount);

            // Random spike (small chance, smaller magnitude)
            if (GD.Randf() < _spikeChance)
            {
                float spikePercent = (float)GD.RandRange(_spikePercentMin, _spikePercentMax);
                int spikeAmount = (int)(_currentListeners * spikePercent);
                // 50% chance positive or negative spike
                if (GD.Randf() < 0.5f)
                    newListeners += spikeAmount;
                else
                    newListeners -= spikeAmount;
            }

            // Final clamp
            newListeners = Mathf.Clamp(newListeners, minListeners, maxListeners);

            if (newListeners != _currentListeners)
            {
                ModifyListeners(newListeners - _currentListeners);
            }
        }

        /// <summary>
        /// Modify listener count by the given amount.
        /// </summary>
        public void ModifyListeners(int amount)
        {
            int minListeners = 0;
            int maxListeners = _stationReach;

            int oldCount = _currentListeners;
            _currentListeners = Mathf.Clamp(_currentListeners + amount, minListeners, maxListeners);

            if (_currentListeners != oldCount)
            {
                EmitSignal("ListenersChanged", oldCount, _currentListeners);

                if (_currentListeners > _peakListeners)
                {
                    _peakListeners = _currentListeners;
                    EmitSignal("PeakReached", _peakListeners);
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
        /// Get a formatted listener count with max (e.g., "1,234 / 1,500")
        /// </summary>
        public string GetFormattedListenersWithMax()
        {
            return $"{FormatListenerCount(_currentListeners)} / {FormatListenerCount(_stationReach)}";
        }

        /// <summary>
        /// Get a formatted change string (e.g., "+123" or "-45")
        /// </summary>
        public string GetFormattedChange()
        {
            int change = ListenerChange;
            if (change == 0) return "0";
            
            string sign = change >= 0 ? "+" : "-";
            string formattedNumber = FormatListenerCount(Mathf.Abs(change));
            return $"{sign}{formattedNumber}";
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

        /// <summary>
        /// Recalculate station reach from SaveData cities.
        /// Call this when cities are unlocked or upgraded.
        /// </summary>
        public void RefreshStationReach()
        {
            if (_saveManager == null)
            {
                _stationReach = 750; // Default fallback
                return;
            }

            var save = _saveManager.CurrentSave;
            if (save.Cities == null || save.Cities.Count == 0)
            {
                _stationReach = 750;
                return;
            }

            int totalReach = 0;
            foreach (var city in save.Cities)
            {
                if (city.IsUnlocked)
                {
                    // Formula: (level * 250) + 500
                    totalReach += (city.AntennaLevel * 250) + 500;
                }
            }

            _stationReach = totalReach;
            save.StationReach = totalReach;

            Log.Debug($"ListenerManager: Station reach updated to {_stationReach}");
        }

        /// <summary>
        /// Get the current station reach (max listeners).
        /// </summary>
        public int GetStationReach() => _stationReach;

        private void HandleCallerCompleted(Caller caller)
        {
            if (_gameState == null || !_gameState.IsLive) return;

            // Get current topic for impact calculation
            // TODO: Add CallerScreeningManager when ported
            string topicId = ""; // Placeholder
            float impact = caller.CalculateShowImpact(topicId);

            int listenerChange = 0;

            if (impact >= 15f)
            {
                listenerChange = _greatCallerBonus;
            }
            else if (impact >= 5f)
            {
                listenerChange = _goodCallerBonus;
            }
            else if (impact < 0f)
            {
                listenerChange = -_badCallerPenalty;
            }

            if (listenerChange != 0)
            {
                ModifyListeners(listenerChange);
            }
        }

        private void HandleCallerDisconnected(Caller caller)
        {
            if (_gameState == null || !_gameState.IsLive) return;

            // Only penalize for callers who hung up while waiting (Incoming/Screening)
            // OnHold and OnAir callers shouldn't disconnect via patience timer
            if (caller.State == CallerState.Incoming || caller.State == CallerState.Screening)
            {
                ModifyListeners(-_disconnectPenalty);
            }
        }
    }
}
