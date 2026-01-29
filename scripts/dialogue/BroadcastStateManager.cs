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
        WaitingForT0,
        AdBreak,
        BreakReturn,
        DeadAir,
        DroppedCaller,
        ShowClosing,
        ShowEnding
    }

    /// <summary>
    /// State manager for determining which executable to deliver next.
    /// Listens to timing events and manages state transitions between show phases.
    /// Handles interruption logic for breaks, show ending, etc.
    /// </summary>
    public partial class BroadcastStateManager : GodotObject
    {
        private readonly ICallerRepository _callerRepository;
        private readonly IArcRepository _arcRepository;
        private readonly VernDialogueTemplate _vernDialogue;
        private readonly EventBus _eventBus;
        private readonly ListenerManager _listenerManager;
        private readonly IBroadcastAudioService _audioService;
        private readonly TimeManager _timeManager;
        private readonly SceneTree _sceneTree;
        private readonly AdManager _adManager;
        private AsyncBroadcastState _currentState = AsyncBroadcastState.Idle;
        private readonly Queue<BroadcastExecutable> _pendingExecutables = new();
        private bool _isShowActive = false;
        private bool _hasPlayedVernOpening = false;
        private bool _pendingBreakTransition = false;
        private float _t0AbsoluteTime = 0f; // When T0 should occur

        public AsyncBroadcastState CurrentState => _currentState;
        public bool IsShowActive => _isShowActive;
        public bool PendingBreakTransition => _pendingBreakTransition;
        public SceneTree SceneTree => _sceneTree;

        public BroadcastStateManager(
            ICallerRepository callerRepository,
            IArcRepository arcRepository,
            VernDialogueTemplate vernDialogue,
            EventBus eventBus,
            ListenerManager listenerManager,
            IBroadcastAudioService audioService,
            TimeManager timeManager,
            SceneTree sceneTree,
            AdManager adManager)
        {
            _callerRepository = callerRepository;
            _arcRepository = arcRepository;
            _vernDialogue = vernDialogue;
            _eventBus = eventBus;
            _listenerManager = listenerManager;
            _audioService = audioService;
            _timeManager = timeManager;
            _sceneTree = sceneTree;
            _adManager = adManager;

            // Subscribe to timing events
            _eventBus.Subscribe<BroadcastTimingEvent>(HandleTimingEvent);
            // Subscribe to interruption events for break handling
            _eventBus.Subscribe<BroadcastInterruptionEvent>(HandleInterruptionEvent);
        }

        /// <summary>
        /// Get the next executable for the current broadcast state.
        /// </summary>
        public BroadcastExecutable? GetNextExecutable()
        {
            switch (_currentState)
            {
                case AsyncBroadcastState.Idle:
                    return null;
                case AsyncBroadcastState.ShowStarting:
                    return CreateShowStartingExecutable();
                case AsyncBroadcastState.IntroMusic:
                    return CreateIntroMusicExecutable();
                case AsyncBroadcastState.Conversation:
                    return CreateConversationExecutable();
                case AsyncBroadcastState.BetweenCallers:
                    return CreateBetweenCallersExecutable();
                case AsyncBroadcastState.DeadAir:
                    return CreateDeadAirExecutable();
                case AsyncBroadcastState.WaitingForT0:
                    return CreateT0WaitExecutable();
                case AsyncBroadcastState.AdBreak:
                    return CreateAdBreakSequenceExecutable(); // Use sequence executable for all 6 ads
                case AsyncBroadcastState.BreakReturn:
                    return CreateReturnFromBreakExecutable();
                case AsyncBroadcastState.DroppedCaller:
                    var droppedCaller = _vernDialogue.GetDroppedCaller();
                    if (droppedCaller != null)
                    {
                        var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{droppedCaller.Id}.mp3";
                        return new DialogueExecutable("dropped_caller", droppedCaller.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.DroppedCaller, this);
                    }
                    
                    // Fallback
                    return new DialogueExecutable("dropped_caller", "Looks like we lost that caller...", "Vern", _eventBus, _audioService, lineType: VernLineType.DroppedCaller, stateManager: this);
                case AsyncBroadcastState.ShowClosing:
                case AsyncBroadcastState.ShowEnding:
                    // Handled in UpdateStateAfterExecution
                    return null;
                default:
                    return null;
            }
        }

        public void UpdateStateAfterExecution(BroadcastExecutable executable)
        {
            var previousState = _currentState;
            
            switch (_currentState)
            {
                case AsyncBroadcastState.ShowStarting:
                    _currentState = AsyncBroadcastState.IntroMusic;
                    break;
                case AsyncBroadcastState.IntroMusic:
                    _currentState = AsyncBroadcastState.Conversation;
                    break;
                case AsyncBroadcastState.Conversation:
                    // Handle break transition completion
                    if (executable.Type == BroadcastItemType.VernLine && 
                        executable is DialogueExecutable breakTransitionExecutable &&
                        breakTransitionExecutable.LineType == VernLineType.BreakTransition)
                    {
                        // Calculate and store T0 absolute time (current elapsed + 5 seconds)
                        _t0AbsoluteTime = _timeManager.ElapsedTime + 5f;
                        _currentState = AsyncBroadcastState.WaitingForT0;
                        _pendingBreakTransition = false;  // Reset the flag
                        GD.Print($"BroadcastStateManager: Break transition completed, T0 will occur at {_t0AbsoluteTime:F1}s (in 5s)");
                        break;
                    }

                    if (executable.Type == BroadcastItemType.VernLine && 
                        executable is DialogueExecutable vernExecutable &&
                        vernExecutable.LineType == VernLineType.ShowOpening)
                    {
                        _hasPlayedVernOpening = true;
                        // Transition based on caller availability
                        if (_callerRepository.OnHoldCallers.Count > 0)
                        {
                            _currentState = AsyncBroadcastState.Conversation;  // Advance to next caller
                        }
                        else
                        {
                            _currentState = AsyncBroadcastState.DeadAir;  // No callers ready, play filler
                        }
                        break;
                    }
                    
                    // Handle PutOnAir completion - caller just put on air, start conversation
                    if (executable.Type == BroadcastItemType.PutOnAir)
                    {
                        // Stay in Conversation state to begin the caller's dialogue
                        break;
                    }
                    
                    // Handle caller conversation completion (including interrupted conversations)
                    if (executable.Type == BroadcastItemType.Conversation)
                    {
                        // Check if this was an interrupted conversation (we have pending break transition)
                        if (_pendingBreakTransition)
                        {
                            GD.Print("BroadcastStateManager: Conversation interrupted with pending break transition - staying in Conversation state");
                            // Stay in Conversation state so break transition can play next
                            break;
                        }
                        
                        // End the current caller's session ONLY after entire conversation
                        var endResult = _callerRepository.EndOnAir();
                        if (endResult.IsSuccess)
                        {
                            GD.Print($"BroadcastStateManager: Ended on-air caller {endResult.Value.Name}");
                        }
                        else
                        {
                            GD.PrintErr($"BroadcastStateManager: Failed to end on-air caller: {endResult.ErrorMessage}");
                        }
                        
                        // Check if more callers are queued
                        if (ShouldPlayBetweenCallers())
                        {
                            _currentState = AsyncBroadcastState.BetweenCallers;
                        }
                        else if (ShouldPlayDeadAir())
                        {
                            _currentState = AsyncBroadcastState.DeadAir;
                        }
                        else
                        {
                            _currentState = AsyncBroadcastState.Conversation;
                        }
                        // Otherwise stay in Conversation state (will auto-advance next time)
                        break;
                    }
                    
                    // Fallback for other conversation types
                    if (ShouldPlayBetweenCallers())
                    {
                        _currentState = AsyncBroadcastState.BetweenCallers;
                    }
                    else if (ShouldPlayDeadAir())
                    {
                        _currentState = AsyncBroadcastState.DeadAir;
                    }
                    // Otherwise stay in Conversation state
                    break;
                case AsyncBroadcastState.BetweenCallers:
                case AsyncBroadcastState.DeadAir:
                    _currentState = AsyncBroadcastState.Conversation;
                    break;
                case AsyncBroadcastState.WaitingForT0:
                    // Stay in WaitingForT0 state until T0 event fires
                    // This creates the timing gap between break transition and ads
                    break;
                case AsyncBroadcastState.AdBreak:
                    // AdBreakSequenceExecutable completed - move to break return
                    _currentState = AsyncBroadcastState.BreakReturn;
                    break;
                case AsyncBroadcastState.BreakReturn:
                    _currentState = AsyncBroadcastState.Conversation;
                    break;
                case AsyncBroadcastState.DroppedCaller:
                    // DroppedCaller executable completed - return to conversation
                    _currentState = AsyncBroadcastState.Conversation;
                    break;
                case AsyncBroadcastState.ShowClosing:
                    _isShowActive = false;
                    _currentState = AsyncBroadcastState.Idle;
                    break;
                case AsyncBroadcastState.ShowEnding:
                    _isShowActive = false;
                    _currentState = AsyncBroadcastState.Idle;
                    break;
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
            }
        }

        private BroadcastExecutable CreateShowStartingExecutable() => 
            new TransitionExecutable("show_start", "Show is starting...", 3.0f, _eventBus, _audioService, _sceneTree, null);

        private BroadcastExecutable CreateIntroMusicExecutable() => 
            new MusicExecutable("intro_music", "Intro music", "res://assets/audio/music/intro_music.wav", 4.0f, _eventBus, _audioService, _sceneTree);

        private BroadcastExecutable CreateConversationExecutable()
        {
            // Play Vern opening once at startup (PRIORITY: Always check first)
            if (!_hasPlayedVernOpening)
            {
                var opening = _vernDialogue.GetShowOpening();
                if (opening != null)
                {
                    var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{opening.Id}.mp3";
                    return new DialogueExecutable("vern_fallback", opening.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.ShowOpening, this);
                }
            }

            // Check for pending break transition (HIGHEST PRIORITY after opening)
            if (_pendingBreakTransition)
            {
                var breakTransition = _vernDialogue.GetBreakTransition();
                if (breakTransition != null)
                {
                    var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{breakTransition.Id}.mp3";
                    return new DialogueExecutable("break_transition", breakTransition.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.BreakTransition, this);
                }
            }

            // THEN handle caller advancement
            var onAirCaller = _callerRepository.OnAirCaller;
            if (onAirCaller == null)
            {
                // Check if we should put a caller on air
                if (_callerRepository.OnHoldCallers.Count > 0 && !_callerRepository.IsOnAir)
                {
                    // Return PutOnAirExecutable to handle the logic
                    return new PutOnAirExecutable(_eventBus, _callerRepository, this);
                }
            }

            if (onAirCaller != null)
            {
                // Convert string topic to ShowTopic enum
                var topic = ShowTopicExtensions.ParseTopic(onAirCaller.ActualTopic);
                if (topic.HasValue)
                {
                    var arc = _arcRepository.GetRandomArcForTopic(topic.Value, onAirCaller.Legitimacy);
                    if (arc != null)
                    {
                        return new DialogueExecutable($"dialogue_{onAirCaller.Id}", onAirCaller, arc, _eventBus, _audioService, this);
                    }
                }
            }

            // If no on-air caller but there are incoming callers, wait for screening to complete
            if (_callerRepository.IncomingCallers.Count > 0)
            {
                return null;
            }

            // Final fallback
            return new DialogueExecutable("vern_fallback", "Welcome to the show.", "Vern", _eventBus, _audioService, lineType: VernLineType.Fallback, stateManager: this);
        }

        private BroadcastExecutable CreateBetweenCallersExecutable()
        {
            var betweenCallers = _vernDialogue.GetBetweenCallers();
            if (betweenCallers != null)
            {
                var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{betweenCallers.Id}.mp3";
                return new DialogueExecutable("between_callers", betweenCallers.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.BetweenCallers, this);
            }
            
            // Fallback
            return new DialogueExecutable("between_callers", "Moving to our next caller...", "Vern", _eventBus, _audioService, lineType: VernLineType.BetweenCallers, stateManager: this);
        }

        private BroadcastExecutable CreateDeadAirExecutable()
        {
            var filler = _vernDialogue.GetDeadAirFiller();
            var text = filler?.Text ?? "Dead air filler";
            var audioPath = filler != null ? $"res://assets/audio/voice/Vern/Broadcast/{filler.Id}.mp3" : null;
            return new DialogueExecutable("dead_air", text, "Vern", _eventBus, _audioService, audioPath, VernLineType.DeadAirFiller, this);
        }

        private BroadcastExecutable CreateAdBreakExecutable()
        {
            // Placeholder: Create a single commercial break with timeout
            // TODO: Replace with actual ad selection logic from AdManager
            var listenerCount = _listenerManager?.CurrentListeners ?? 100;
            return AdExecutable.CreateForListenerCount("placeholder_ad", listenerCount, 1, _eventBus, _listenerManager, _audioService, _sceneTree);
        }

        private BroadcastExecutable CreateReturnFromBreakExecutable() => 
            new TransitionExecutable("break_return", "Returning from break", 3.0f, _eventBus, _audioService, _sceneTree, "res://assets/audio/bumpers/return.wav");

        private BroadcastExecutable CreateT0WaitExecutable()
        {
            // Calculate remaining time until T0
            float currentTime = _timeManager.ElapsedTime;
            float timeUntilT0 = Math.Max(0f, _t0AbsoluteTime - currentTime);
            
            GD.Print($"BroadcastStateManager: Creating T0 wait executable - current time: {currentTime:F1}s, T0 at: {_t0AbsoluteTime:F1}s, waiting: {timeUntilT0:F1}s");
            
            // Create a dynamic wait executable that waits exactly until T0
            return new TransitionExecutable("t0_wait", "Waiting for break...", timeUntilT0, _eventBus, _audioService, _sceneTree, null);
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

        private BroadcastExecutable CreateAdBreakSequenceExecutable()
        {
            // Get current break slots from AdManager
            var adCount = _adManager?.CurrentBreakSlots ?? 6; // Fallback to 6 if AdManager not available
            
            GD.Print($"BroadcastStateManager: Creating AdBreakSequence executable - will play {adCount} ads sequentially");
            return new AdBreakSequenceExecutable("ad_break_sequence", _eventBus, _listenerManager, _audioService, _sceneTree, adCount);
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
                    // Move from WaitingForT0 to AdBreak state when T0 occurs
                    if (_currentState == AsyncBroadcastState.WaitingForT0)
                    {
                        _currentState = AsyncBroadcastState.AdBreak;
                        GD.Print($"BroadcastStateManager: T0 reached at {_timeManager.ElapsedTime:F1}s, moving from WaitingForT0 to AdBreak");
                    }
                    else
                    {
                        _currentState = AsyncBroadcastState.AdBreak;
                    }
                    break;
                case BroadcastTimingEventType.AdBreakStart:
                    _currentState = AsyncBroadcastState.AdBreak;
                    break;
                case BroadcastTimingEventType.AdBreakEnd:
                    _currentState = AsyncBroadcastState.BreakReturn;
                    break;
            }
            
            if (_currentState != previousState)
            {
                PublishStateChangedEvent(previousState);
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

        /// <summary>
        /// Check if we should play a between-callers transition.
        /// </summary>
        private bool ShouldPlayBetweenCallers()
        {
            return _callerRepository.OnHoldCallers.Count > 0;
        }

        /// <summary>
        /// Check if we should play dead air filler.
        /// </summary>
        private bool ShouldPlayDeadAir()
        {
            return _callerRepository.OnAirCaller == null && 
                   _callerRepository.OnHoldCallers.Count == 0;
        }

        /// <summary>
        /// Get the time remaining until T0 (for countdown display during break waiting).
        /// Returns 0 if not in WaitingForT0 state.
        /// </summary>
        public float GetTimeUntilT0()
        {
            if (_currentState != AsyncBroadcastState.WaitingForT0)
            {
                return 0f;
            }

            float currentTime = _timeManager.ElapsedTime;
            return Math.Max(0f, _t0AbsoluteTime - currentTime);
        }
    }
}