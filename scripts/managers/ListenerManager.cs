using System;
using Godot;
using KBTV.Core;
using KBTV.Data;
using KBTV.Callers;

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

		public static ListenerManager Instance => (ListenerManager)((SceneTree)Engine.GetMainLoop()).Root.GetNode("/root/ListenerManager");
        [Export] private int _baseListeners = 1000;
        [Export] private int _listenerVariance = 200;
        [Export] private float _baseGrowthRate = 2f;
        [Export] private float _maxGrowthMultiplier = 12f;
        [Export] private float _minGrowthMultiplier = -12f;
        [Export] private float _maxListenerMultiplier = 3f;
        [Export] private float _minListenerMultiplier = 0.1f;
        [Export] private int _greatCallerBonus = 150;
        [Export] private int _goodCallerBonus = 50;
        [Export] private int _badCallerPenalty = 100;
        [Export] private int _disconnectPenalty = 25;

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



        private GameStateManager _gameState;
        private TimeManager _timeManager;
        private ICallerRepository _repository;

        public override void _Ready()
        {
            if (ServiceRegistry.Instance == null)
            {
                GD.PrintErr("ListenerManager: ServiceRegistry not available");
            }
            else
            {
                _repository = ServiceRegistry.Instance.CallerRepository;
            }

            if (_repository == null)
            {
                GD.PrintErr("ListenerManager: ICallerRepository not available");
            }

            _gameState = GameStateManager.Instance;
            _timeManager = TimeManager.Instance;

			if (_gameState != null)
			{
				_gameState.Connect("PhaseChanged", Callable.From<int, int>(HandlePhaseChanged));
			}

            if (_timeManager != null)
            {
                _timeManager.Connect("Tick", Callable.From<float>(HandleTick));
            }

            if (_repository != null)
            {
                _repository.Subscribe(_repositoryObserver);
            }
        }

        private readonly ICallerRepositoryObserver _repositoryObserver = new ListenerRepositoryObserver();

        public override void _ExitTree()
        {
            if (_gameState != null)
            {
                _gameState.Disconnect("PhaseChanged", Callable.From<int, int>(HandlePhaseChanged));
            }

            if (_timeManager != null)
            {
                _timeManager.Disconnect("Tick", Callable.From<float>(HandleTick));
            }

            if (_repository != null)
            {
                _repository.Unsubscribe(_repositoryObserver);
            }
        }

        private void HandlePhaseChanged(int oldPhaseInt, int newPhaseInt)
        {
            GamePhase oldPhase = (GamePhase)oldPhaseInt;
            GamePhase newPhase = (GamePhase)newPhaseInt;

            if (newPhase == GamePhase.LiveShow)
            {
                InitializeListeners();
            }
        }

        private void InitializeListeners()
        {
            // Calculate starting listeners with some variance
            int variance = (int)GD.RandRange(-_listenerVariance, _listenerVariance + 1);
            _startingListeners = Mathf.Max(100, _baseListeners + variance);
            _currentListeners = _startingListeners;
            _peakListeners = _startingListeners;
            _accumulatedGrowth = 0f;

            EmitSignal("ListenersChanged", 0, _currentListeners);
        }

        private void HandleTick(float deltaTime)
        {
            if (_gameState == null || !_gameState.IsLive) return;

            VernStats stats = _gameState.VernStats;
            if (stats == null) return;

            // Get VIBE (-100 to +100)
            float vibe = stats.CalculateVIBE();

            // Calculate growth rate using sigmoid curve
            // Sigmoid: modifier = 1.0 + (vibe/100 * 0.8) + (vibe/100)^2 * 0.4
            float normalizedVibe = vibe / 100f;
            float modifier = 1.0f + (normalizedVibe * 0.8f) + (normalizedVibe * normalizedVibe * 0.4f);

            // Map modifier to growth rate
            // modifier 0.2 (at -100 VIBE) -> -12x multiplier
            // modifier 1.0 (at 0 VIBE) -> 1x multiplier (base rate)
            // modifier 1.6 (at +100 VIBE) -> +12x multiplier
            float growthRate;
            if (modifier >= 1f)
            {
                // Positive VIBE: grow at rate proportional to modifier
                growthRate = _baseGrowthRate * (1f + (modifier - 1f) * (_maxGrowthMultiplier - 1f));
            }
            else
            {
                // Negative VIBE: lose listeners proportional to inverse modifier
                float inverseModifier = 1f - modifier;  // 0.8 at -100 VIBE
                growthRate = _baseGrowthRate * -inverseModifier * (_minGrowthMultiplier * -1f);
            }

            // Apply growth
            float growthAmount = growthRate * deltaTime;

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
            else
            {
                GD.Print($"ListenerManager: Unexpected disconnect - Caller '{caller.Name}' state was {caller.State}");
            }
        }
    }

    internal class ListenerRepositoryObserver : ICallerRepositoryObserver
    {
        public void OnCallerAdded(Caller caller) { }
        public void OnCallerRemoved(Caller caller) { }
        public void OnCallerStateChanged(Caller caller, CallerState oldState, CallerState newState) { }
    }
}