#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Managers;
using KBTV.Audio;
using KBTV.Data;

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
    public class BroadcastStateManager
    {
        private readonly ICallerRepository _callerRepository;
        private readonly IArcRepository _arcRepository;
        private readonly VernDialogueTemplate _vernDialogue;
        private readonly EventBus _eventBus;
        private readonly ListenerManager _listenerManager;
        private readonly IBroadcastAudioService _audioService;
        private AsyncBroadcastState _currentState = AsyncBroadcastState.Idle;
        private readonly Queue<BroadcastExecutable> _pendingExecutables = new();
        private bool _isShowActive = false;
        private bool _hasPlayedVernOpening = false;

        public AsyncBroadcastState CurrentState => _currentState;
        public bool IsShowActive => _isShowActive;

        public BroadcastStateManager(
            ICallerRepository callerRepository,
            IArcRepository arcRepository,
            VernDialogueTemplate vernDialogue,
            EventBus eventBus,
            ListenerManager listenerManager,
            IBroadcastAudioService audioService)
        {
            _callerRepository = callerRepository;
            _arcRepository = arcRepository;
            _vernDialogue = vernDialogue;
            _eventBus = eventBus;
            _listenerManager = listenerManager;
            _audioService = audioService;

            // Subscribe to timing events
            _eventBus.Subscribe<BroadcastTimingEvent>(HandleTimingEvent);
        }

        public void UpdateStateAfterExecution(BroadcastExecutable executable)
        {
            switch (_currentState)
            {
                case AsyncBroadcastState.ShowStarting:
                    _currentState = AsyncBroadcastState.IntroMusic;
                    break;
                case AsyncBroadcastState.IntroMusic:
                    _currentState = AsyncBroadcastState.Conversation;
                    break;
                case AsyncBroadcastState.Conversation:
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
                    
                    // Handle caller conversation completion
                    if (executable.Type == BroadcastItemType.Conversation)
                    {
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
                        if (_callerRepository.OnHoldCallers.Count > 0)
                        {
                            _currentState = AsyncBroadcastState.Conversation;
                        }
                        else if (ShouldPlayDeadAir())
                        {
                            _currentState = AsyncBroadcastState.DeadAir;
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
                case AsyncBroadcastState.AdBreak:
                    _currentState = AsyncBroadcastState.BreakReturn;
                    break;
                case AsyncBroadcastState.BreakReturn:
                    _currentState = AsyncBroadcastState.Conversation;
                    break;
                case AsyncBroadcastState.ShowEnding:
                    _isShowActive = false;
                    _currentState = AsyncBroadcastState.Idle;
                    break;
            }
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

        private BroadcastExecutable CreateShowStartingExecutable() => 
            new TransitionExecutable("show_start", "Show is starting...", 3.0f, _eventBus, _audioService, null);

        private BroadcastExecutable CreateIntroMusicExecutable() => 
            new MusicExecutable("intro_music", "Intro music", "res://assets/audio/music/intro_music.wav", 4.0f, _eventBus, _audioService);

        private BroadcastExecutable CreateConversationExecutable()
        {
            // Play Vern opening once at startup (PRIORITY: Always check first)
            if (!_hasPlayedVernOpening)
            {
                var opening = _vernDialogue.GetShowOpening();
                if (opening != null)
                {
                    var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{opening.Id}.mp3";
                    return new DialogueExecutable("vern_fallback", opening.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.ShowOpening);
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
                        return new DialogueExecutable($"dialogue_{onAirCaller.Id}", onAirCaller, arc, _eventBus, _audioService);
                    }
                }
            }

            // If no on-air caller but there are incoming callers, wait for screening to complete
            if (_callerRepository.IncomingCallers.Count > 0)
            {
                return null;
            }

            // Final fallback
            return new DialogueExecutable("vern_fallback", "Welcome to the show.", "Vern", _eventBus, _audioService, lineType: VernLineType.Fallback);
        }

        private BroadcastExecutable CreateBetweenCallersExecutable()
        {
            var betweenCallers = _vernDialogue.GetBetweenCallers();
            if (betweenCallers != null)
            {
                var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{betweenCallers.Id}.mp3";
                return new DialogueExecutable("between_callers", betweenCallers.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.BetweenCallers);
            }
            
            // Fallback
            return new DialogueExecutable("between_callers", "Moving to our next caller...", "Vern", _eventBus, _audioService, lineType: VernLineType.BetweenCallers);
        }

        private BroadcastExecutable CreateDeadAirExecutable()
        {
            var filler = _vernDialogue.GetDeadAirFiller();
            var text = filler?.Text ?? "Dead air filler";
            var audioPath = filler != null ? $"res://assets/audio/voice/Vern/Broadcast/{filler.Id}.mp3" : null;
            return new DialogueExecutable("dead_air", text, "Vern", _eventBus, _audioService, audioPath, VernLineType.DeadAirFiller);
        }

        private BroadcastExecutable CreateReturnFromBreakExecutable() => 
            new TransitionExecutable("break_return", "Returning from break", 3.0f, _eventBus, _audioService, "res://assets/audio/bumpers/return.wav");

        private BroadcastExecutable CreateShowClosingExecutable() => 
            new MusicExecutable("show_closing", "Show closing music", "res://assets/audio/music/outro_music.wav", 5.0f, _eventBus, _audioService);

        private BroadcastExecutable CreateDroppedCallerExecutable()
        {
            var droppedCaller = _vernDialogue.GetDroppedCaller();
            if (droppedCaller != null)
            {
                var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{droppedCaller.Id}.mp3";
                var executable = new DialogueExecutable("dropped_caller", droppedCaller.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.DroppedCaller);
                // Reset state to Conversation after creating executable
                _currentState = AsyncBroadcastState.Conversation;
                return executable;
            }
            
            // Fallback
            var fallbackExecutable = new DialogueExecutable("dropped_caller", "Looks like we lost that caller...", "Vern", _eventBus, _audioService, lineType: VernLineType.DroppedCaller);
            _currentState = AsyncBroadcastState.Conversation;
            return fallbackExecutable;
        }

        /// <summary>
        /// Start the show and return the initial executable.
        /// </summary>
        public BroadcastExecutable? StartShow()
        {
            _isShowActive = true;
            _currentState = AsyncBroadcastState.ShowStarting;
            _hasPlayedVernOpening = false;
            return GetNextExecutable();
        }

        /// <summary>
        /// Get the next executable based on current state.
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
                case AsyncBroadcastState.AdBreak:
                    return null; // Handled by external logic
                case AsyncBroadcastState.BreakReturn:
                    return CreateReturnFromBreakExecutable();
                case AsyncBroadcastState.DroppedCaller:
                    return CreateDroppedCallerExecutable();
                case AsyncBroadcastState.ShowClosing:
                    return CreateShowClosingExecutable();
                case AsyncBroadcastState.ShowEnding:
                    return null; // Show is ending
                default:
                    return null;
            }
        }

        /// <summary>
        /// Set the current broadcast state (used by executables).
        /// </summary>
        public void SetState(AsyncBroadcastState state)
        {
            _currentState = state;
        }

        /// <summary>
        /// Handle timing events from the broadcast timer.
        /// </summary>
        private void HandleTimingEvent(BroadcastTimingEvent timingEvent)
        {
            switch (timingEvent.Type)
            {
                case BroadcastTimingEventType.ShowEnd:
                    _currentState = AsyncBroadcastState.ShowEnding;
                    break;
                case BroadcastTimingEventType.Break20Seconds:
                case BroadcastTimingEventType.Break10Seconds:
                case BroadcastTimingEventType.Break5Seconds:
                case BroadcastTimingEventType.Break0Seconds:
                    // Break warnings - could add logic here if needed
                    break;
                case BroadcastTimingEventType.AdBreakStart:
                    _currentState = AsyncBroadcastState.AdBreak;
                    break;
                case BroadcastTimingEventType.AdBreakEnd:
                    _currentState = AsyncBroadcastState.BreakReturn;
                    break;
            }
        }
    }
}