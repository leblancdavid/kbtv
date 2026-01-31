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
        WaitingForBreak  // New state: waiting for T0 after break transition
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
        private VernDialogueTemplate _vernDialogue;
        private AsyncBroadcastState _currentState = AsyncBroadcastState.Idle;
        private readonly Queue<BroadcastExecutable> _pendingExecutables = new();
        private bool _isShowActive = false;
        public bool _hasPlayedVernOpening = false;
        public bool _pendingBreakTransition = false;
        public bool _adBreakSequenceRunning = false;
        public AsyncBroadcastState _previousState = AsyncBroadcastState.Idle;

        public AsyncBroadcastState CurrentState => _currentState;
        public float ElapsedTime => _timeManager?.ElapsedTime ?? 0f;
        public bool IsShowActive => _isShowActive;
        public bool PendingBreakTransition => _pendingBreakTransition;
        public SceneTree SceneTree => _sceneTree;

        public BroadcastExecutable? GetNextExecutable()
        {
            return _stateMachine.GetNextExecutable(_currentState);
        }

        public void UpdateStateAfterExecution(BroadcastExecutable executable)
        {
            var previousState = _currentState;
            _currentState = _stateMachine.UpdateStateAfterExecution(_currentState, executable);
            
            if (_currentState != previousState)
            {
                PublishStateChangedEvent(previousState);
                _previousState = previousState;
            }
        }
        /// Start the broadcast show and return the first executable.
        /// </summary>
        public BroadcastExecutable? StartShow()
        {
            if (_currentState != AsyncBroadcastState.Idle)
            {
                GD.PrintErr($"BroadcastStateManager: Cannot start show - current state is {_currentState}, expected Idle");
                return null;
            }

            _isShowActive = true;
            _currentState = AsyncBroadcastState.ShowStarting;
            
            GD.Print("BroadcastStateManager: Show started, returning first executable");
            return GetNextExecutable();
        }

        /// <summary>
        /// Set the broadcast state directly (for special cases).
        /// </summary>
        public void SetState(AsyncBroadcastState newState)
        {
            var previousState = _currentState;
            _currentState = newState;
            
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
            var previousState = _currentState;
            
            switch (timingEvent.Type)
            {
                case BroadcastTimingEventType.ShowEnd:
                    _currentState = AsyncBroadcastState.ShowEnding;
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
                     // T0 reached - fire interruption to complete WaitForBreakExecutable, then transition to AdBreak
                     _eventBus.Publish(new BroadcastInterruptionEvent(BroadcastInterruptionReason.BreakStarting));
                     _currentState = AsyncBroadcastState.AdBreak;
                     break;
                case BroadcastTimingEventType.AdBreakStart:
                    _currentState = AsyncBroadcastState.AdBreak;
                    break;
                 case BroadcastTimingEventType.AdBreakEnd:
                     _currentState = AsyncBroadcastState.BreakReturnMusic;
                     break;
             }
             
             if (_currentState != previousState)
             {
                 PublishStateChangedEvent(previousState);
                 _previousState = previousState;
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
                // Drop the on-air caller when break starts
                var onAirCaller = _callerRepository.OnAirCaller;
                if (onAirCaller != null)
                {
                    GD.Print($"BroadcastStateManager: Dropping on-air caller '{onAirCaller.Name}' due to ad break");
                    _callerRepository.SetCallerState(onAirCaller, CallerState.Disconnected);
                    _callerRepository.RemoveCaller(onAirCaller);
                }
            }
            
            if (_currentState != previousState)
            {
                PublishStateChangedEvent(previousState);
            }
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
                this
            );

            // Subscribe to timing events
            _eventBus.Subscribe<BroadcastTimingEvent>(HandleTimingEvent);
            // Subscribe to interruption events for break handling
            _eventBus.Subscribe<BroadcastInterruptionEvent>(HandleInterruptionEvent);
        }
    }
}