#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Managers;
using KBTV.Audio;
using KBTV.Data;
using KBTV.Dialogue;
using KBTV.Ads;
using KBTV.Monitors;
using KBTV.Broadcast;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Broadcast state for the async broadcast system.
    /// </summary>
    public enum AsyncBroadcastState
    {
        Idle,
        ShowStarting,
        IntroMusic,
        Conversation,
        BetweenCallers,
        AdBreak,
        BreakReturnMusic,
        BreakReturn,
        DeadAir,
        DroppedCaller,
        ShowClosing,
        ShowEnding,
        WaitingForBreak,  // New state: waiting for T0 after break transition
        WaitingForShowEnd  // New state: waiting for show end after closing dialogue
    }

    /// <summary>
    /// State manager for determining which executable to deliver next.
    /// Listens to timing events and manages state transitions between show phases.
    /// Handles interruption logic for breaks, show ending, etc.
    /// </summary>
    public partial class BroadcastStateManager : Node, 
        IProvide<BroadcastStateManager>,
        IDependent
    {
        public override void _Notification(int what) => this.Notify(what);
        // ═══════════════════════════════════════════════════════════════════════════════════════════════
        // DEPENDENCIES
        // ═══════════════════════════════════════════════════════════════════════════════════════════════

        private ICallerRepository _callerRepository => DependencyInjection.Get<ICallerRepository>(this);
        private IArcRepository _arcRepository => DependencyInjection.Get<IArcRepository>(this);
        private BroadcastStateMachine _stateMachine;
        private EventBus _eventBus => DependencyInjection.Get<EventBus>(this);
        private ListenerManager _listenerManager => DependencyInjection.Get<ListenerManager>(this);
        private IBroadcastAudioService _audioService => DependencyInjection.Get<IBroadcastAudioService>(this);
        private TimeManager _timeManager => DependencyInjection.Get<TimeManager>(this);
        private SceneTree _sceneTree => GetTree();
        private AdManager _adManager => DependencyInjection.Get<AdManager>(this);
        private IGameStateManager _gameStateManager => DependencyInjection.Get<IGameStateManager>(this);
        private DeadAirManager _deadAirManager => DependencyInjection.Get<DeadAirManager>(this);
        private ConversationStatTracker _statTracker => DependencyInjection.Get<ConversationStatTracker>(this);
        private VernDialogueTemplate _vernDialogue;
        private AsyncBroadcastState _currentState = AsyncBroadcastState.Idle;
        private readonly Queue<BroadcastExecutable> _pendingExecutables = new();
        private bool _isShowActive = false;
        public bool _hasPlayedVernOpening = false;
        public bool _pendingBreakTransition = false;
        public bool _pendingShowEndingTransition = false;
        public bool _showClosingStarted = false;
        public bool _adBreakSequenceRunning = false;
        public AsyncBroadcastState _previousState = AsyncBroadcastState.Idle;
        
        // Ad break tracking for sequential execution
        private int _currentAdIndex = 0;
        private int _totalAdsForBreak = 0;
        private List<int> _adOrder = new();
        private bool _adBreakInitialized = false;

        // Public accessors for BroadcastStateMachine
        public int CurrentAdIndex => _currentAdIndex;
        public int TotalAdsForBreak => _totalAdsForBreak;
        public List<int> AdOrder => _adOrder;
        public bool AdBreakInitialized => _adBreakInitialized;

        // Methods for BroadcastStateMachine to modify ad state
        public void IncrementAdIndex() => _currentAdIndex++;
        public void ResetAdBreakState()
        {
            _currentAdIndex = 0;
            _totalAdsForBreak = 0;
            _adOrder.Clear();
            _adBreakInitialized = false;
        }
        public void SetAdBreakState(int totalAds, List<int> order)
        {
            _totalAdsForBreak = totalAds;
            _adOrder = order;
            _currentAdIndex = 0;
        }
        public void SetAdBreakInitialized(bool value) => _adBreakInitialized = value;

        public AsyncBroadcastState CurrentState => _currentState;
        public float ElapsedTime => _timeManager?.ElapsedTime ?? 0f;
        public bool IsShowActive => _isShowActive;
        public bool PendingBreakTransition => _pendingBreakTransition;
        public bool PendingShowEndingTransition => _pendingShowEndingTransition;
        public SceneTree SceneTree => _sceneTree;

        public BroadcastExecutable? GetNextExecutable()
        {
            return _stateMachine.GetNextExecutable(_currentState);
        }

        public void UpdateStateAfterExecution(BroadcastExecutable executable)
        {
            var newState = _stateMachine.UpdateStateAfterExecution(_currentState, executable);
            SetState(newState);
        }
        /// <summary>
        /// Set the broadcast state directly (for special cases).
        /// </summary>
        public void SetState(AsyncBroadcastState newState)
        {
            var previousState = _currentState;
            _currentState = newState;
            
            // Reset ad break state when transitioning TO AdBreak
            if (newState == AsyncBroadcastState.AdBreak && previousState != AsyncBroadcastState.AdBreak)
            {
                ResetAdBreakState();
                GD.Print("BroadcastStateManager: Reset ad break state for new break");
            }
            
            // Reset ad break sequence flag if transitioning out of AdBreak
            ResetAdBreakSequenceFlag();
            
            if (_currentState != previousState)
            {
                PublishStateChangedEvent(previousState);
                _previousState = previousState;
            }
        }

        /// <summary>
        /// Non-blocking delay using Godot timers with cancellation support.
        /// </summary>
        public async Task DelayAsync(float seconds, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);

            var timer = _sceneTree.CreateTimer(seconds);
            var tcs = new TaskCompletionSource<bool>();

            void OnTimeout()
            {
                tcs.TrySetResult(true);
            }

            timer.Timeout += OnTimeout;

            using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());

            try
            {
                await tcs.Task;
            }
            finally
            {
                timer.Timeout -= OnTimeout;
            }
        }

        /// <summary>
        /// Handle timing events from the broadcast timer.
        /// </summary>
        private void HandleTimingEvent(BroadcastTimingEvent timingEvent)
        {
            switch (timingEvent.Type)
            {
                case BroadcastTimingEventType.ShowEnd:
                    _eventBus.Publish(new BroadcastInterruptionEvent(BroadcastInterruptionReason.ShowEnding));
                    SetState(AsyncBroadcastState.ShowEnding);
                    break;
                case BroadcastTimingEventType.ShowEnd20Seconds:
                    GD.Print($"BroadcastStateManager: T-20s fired - setting pending show ending transition (current state: {_currentState})");
                    _pendingShowEndingTransition = true;
                    break;
                case BroadcastTimingEventType.ShowEnd10Seconds:
                    GD.Print($"BroadcastStateManager: T-10s fired - checking if closing started (current state: {_currentState}, closing started: {_showClosingStarted})");
                    if (!_showClosingStarted)
                    {
                        GD.Print("BroadcastStateManager: T-10s - closing not started, publishing interruption to force it");
                        _eventBus.Publish(new BroadcastInterruptionEvent(BroadcastInterruptionReason.ShowEnding));
                    }
                    else
                    {
                        GD.Print("BroadcastStateManager: T-10s - closing already started, no interruption needed");
                    }
                    break;
                case BroadcastTimingEventType.Break20Seconds:
                    // Just opens break window - no broadcast action needed
                    break;
                case BroadcastTimingEventType.Break10Seconds:
                    // Set pending break transition for grace period - will be handled in GetNextExecutable
                    _pendingBreakTransition = true;
                    break;
                case BroadcastTimingEventType.Break5Seconds:
                    // T5 timing handled by interruption events now - no direct state change
                    GD.Print($"BroadcastStateManager: T5 timing event received, current state: {_currentState}");
                    break;
                 case BroadcastTimingEventType.Break0Seconds:
                     // T0 event removed - caller dropping now handled by WaitForBreakExecutable completion
                     // AdManager.StartBreak() moved to AdBreak state initialization
                     break;
                 case BroadcastTimingEventType.AdBreakStart:
                     // AdBreakStart event removed - state transitions handled by BroadcastStateMachine after WaitForBreak
                     break;
                 case BroadcastTimingEventType.AdBreakEnd:
                     SetState(AsyncBroadcastState.BreakReturnMusic);
                     break;
            }
        }
 
         /// <summary>
        /// Handle interruption events for break transitions.
        /// </summary>
        private void HandleInterruptionEvent(BroadcastInterruptionEvent interruptionEvent)
        {
            GD.Print($"BroadcastStateManager: Received interruption event: {interruptionEvent.Reason}");
            
            var previousState = _currentState;
            
            if (interruptionEvent.Reason == BroadcastInterruptionReason.BreakImminent)
            {
                GD.Print("BroadcastStateManager: Break imminent - setting pending break transition");
                _pendingBreakTransition = true;  // Ensure Vern transition plays before ads
                // IMPORTANT: Do NOT change state during BreakImminent - preserve current state
                // This allows break transition to have priority over any other executable
                GD.Print($"BroadcastStateManager: Pending break transition set, preserving state {_currentState}");
            }
            else if (interruptionEvent.Reason == BroadcastInterruptionReason.BreakStarting)
            {
                // Additional safety fallback - drop caller if not already done
                var onAirCallerFallback = _callerRepository.OnAirCaller;
                if (onAirCallerFallback != null)
                {
                    GD.Print($"BroadcastStateManager: FALLBACK caller drop for '{onAirCallerFallback.Name}' via BreakStarting interruption");
                    _callerRepository.SetCallerState(onAirCallerFallback, CallerState.Disconnected);
                    _callerRepository.RemoveCaller(onAirCallerFallback);
                }
                else
                {
                    GD.Print("BroadcastStateManager: No on-air caller to drop via BreakStarting interruption (already handled)");
                }
            }
            else if (interruptionEvent.Reason == BroadcastInterruptionReason.ShowEnding)
            {
                GD.Print($"BroadcastStateManager: Show ending interruption received (current state: {_currentState}) - global queuing will handle transition");
                // Global queuing in BroadcastStateMachine.GetNextExecutable() will handle state transition
            }
            
            if (_currentState != previousState)
            {
                PublishStateChangedEvent(previousState);
            }
        }

        /// <summary>
        /// Start the broadcast show and return the first executable.
        /// </summary>
        public BroadcastExecutable? StartShow()
        {
            if (_currentState != AsyncBroadcastState.Idle)
            {
                GD.PrintErr($"BroadcastStateManager: Cannot start show - current state is {_currentState}, expected Idle");
                return null;
            }

            // Reset flags for new show
            _pendingBreakTransition = false;
            _pendingShowEndingTransition = false;
            _showClosingStarted = false;
            _hasPlayedVernOpening = false;
            _adBreakSequenceRunning = false;
            _currentAdIndex = 0;
            _totalAdsForBreak = 0;
            _adOrder.Clear();

            _isShowActive = true;
            _currentState = AsyncBroadcastState.ShowStarting;
            
            GD.Print("BroadcastStateManager: Show started, returning first executable");
            return GetNextExecutable();
        }

        /// <summary>
        /// Publish state change event for UI updates.
        /// </summary>
        private void PublishStateChangedEvent(AsyncBroadcastState previousState)
        {
            var stateChangedEvent = new BroadcastStateChangedEvent(_currentState, previousState);
            _eventBus.Publish(stateChangedEvent);
            GD.Print($"BroadcastStateManager: Published state change from {previousState} to {_currentState}");
        }

        /// <summary>
        /// Reset the ad break sequence running flag when transitioning out of AdBreak state.
        /// This ensures the sequence can run again for the next break.
        /// </summary>
        private void ResetAdBreakSequenceFlag()
        {
            if (_currentState != AsyncBroadcastState.AdBreak && _adBreakSequenceRunning)
            {
                GD.Print("BroadcastStateManager: Resetting _adBreakSequenceRunning flag (transitioned out of AdBreak state)");
                _adBreakSequenceRunning = false;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════════════════════
        // PROVIDER INTERFACE IMPLEMENTATIONS
        // ═══════════════════════════════════════════════════════════════════════════════════════════════

        BroadcastStateManager IProvide<BroadcastStateManager>.Value() => this;

        /// <summary>
        /// Called when node enters the scene tree and is ready.
        /// Makes services available to descendants.
        /// </summary>
        public void OnReady() => this.Provide();

        /// <summary>
        /// Called when all dependencies are resolved.
        /// Subscribe to events now that dependencies are available.
        /// </summary>
        public void OnResolved()
        {
            // Load VernDialogueTemplate
            var vernDialogueLoader = new VernDialogueLoader();
            vernDialogueLoader.LoadDialogue();
            _vernDialogue = vernDialogueLoader.VernDialogue;

            // Create state machine
            _stateMachine = new BroadcastStateMachine(
                _callerRepository,
                _arcRepository,
                _vernDialogue,
                _eventBus,
                _listenerManager,
                _audioService,
                _sceneTree,
                _adManager,
                this,
                _timeManager,
                _gameStateManager,
                _deadAirManager,
                _statTracker
            );

            // Subscribe to timing events
            _eventBus.Subscribe<BroadcastTimingEvent>(HandleTimingEvent);
            // Subscribe to interruption events for break handling
            _eventBus.Subscribe<BroadcastInterruptionEvent>(HandleInterruptionEvent);
        }
    }
}