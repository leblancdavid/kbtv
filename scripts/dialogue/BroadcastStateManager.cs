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
        private readonly EventBus _eventBus;
        private readonly ListenerManager _listenerManager;
        private AsyncBroadcastState _currentState = AsyncBroadcastState.Idle;
        private readonly Queue<BroadcastExecutable> _pendingExecutables = new();
        private bool _isShowActive = false;
        private bool _hasPlayedVernOpening = false;

        // Dependencies for executable creation
        private readonly IArcRepository _arcRepository;
        private readonly VernDialogueTemplate _vernDialogue;
        private readonly IBroadcastAudioService _audioService;

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

        /// <summary>
        /// Start the show and return the first executable.
        /// </summary>
        public BroadcastExecutable? StartShow()
        {
            _isShowActive = true;
            _hasPlayedVernOpening = false;
            _currentState = AsyncBroadcastState.ShowStarting;
            
            GD.Print("BroadcastStateManager: Show started");
            return CreateShowStartingExecutable();
        }

        /// <summary>
        /// Get the next executable to execute.
        /// </summary>
        public BroadcastExecutable? GetNextExecutable()
        {
            // If there are pending executables, return the first one
            if (_pendingExecutables.Count > 0)
            {
                return _pendingExecutables.Dequeue();
            }

            // Determine next executable based on current state
            return CreateNextExecutableForState();
        }

        /// <summary>
        /// Handle timing events that affect state transitions.
        /// </summary>
        private void HandleTimingEvent(BroadcastTimingEvent timingEvent)
        {
            switch (timingEvent.Type)
            {
                case BroadcastTimingEventType.ShowEnd:
                    HandleShowEnding();
                    break;
                case BroadcastTimingEventType.Break0Seconds:
                    HandleBreakStarting();
                    break;
                case BroadcastTimingEventType.AdBreakEnd:
                    HandleBreakEnding();
                    break;
            }
        }

        /// <summary>
        /// Handle show ending.
        /// </summary>
        private void HandleShowEnding()
        {
            if (!_isShowActive) return;

            _currentState = AsyncBroadcastState.ShowEnding;
            
            // Clear pending executables and add show closing sequence
            _pendingExecutables.Clear();
            
            var closingExecutable = CreateShowClosingExecutable();
            if (closingExecutable != null)
            {
                _pendingExecutables.Enqueue(closingExecutable);
            }

            GD.Print("BroadcastStateManager: Show ending triggered");
        }

        /// <summary>
        /// Handle break starting.
        /// </summary>
        private void HandleBreakStarting()
        {
            if (!_isShowActive) return;

            _currentState = AsyncBroadcastState.AdBreak;
            
            // Clear pending executables and add ad break sequence
            _pendingExecutables.Clear();
            
            var adBreakExecutables = CreateAdBreakExecutables();
            foreach (var executable in adBreakExecutables)
            {
                _pendingExecutables.Enqueue(executable);
            }

            GD.Print("BroadcastStateManager: Ad break starting");
        }

        /// <summary>
        /// Handle break ending.
        /// </summary>
        private void HandleBreakEnding()
        {
            if (!_isShowActive) return;

            _currentState = AsyncBroadcastState.BreakReturn;
            
            // Add return from break executable
            var returnExecutable = CreateReturnFromBreakExecutable();
            if (returnExecutable != null)
            {
                _pendingExecutables.Enqueue(returnExecutable);
            }

            GD.Print("BroadcastStateManager: Break ending, transitioning back to show");
        }

        /// <summary>
        /// Create the next executable based on current state.
        /// </summary>
        private BroadcastExecutable? CreateNextExecutableForState()
        {
            return _currentState switch
            {
                AsyncBroadcastState.ShowStarting => CreateIntroMusicExecutable(),
                AsyncBroadcastState.IntroMusic => CreateIntroMusicExecutable(),
                AsyncBroadcastState.Conversation => CreateConversationExecutable(),
                AsyncBroadcastState.BetweenCallers => CreateBetweenCallersExecutable(),
                AsyncBroadcastState.DeadAir => CreateDeadAirExecutable(),
                AsyncBroadcastState.BreakReturn => CreateConversationExecutable(),
                AsyncBroadcastState.ShowEnding => null, // Show is ending
                _ => null
            };
        }

        /// <summary>
        /// Update state after an executable completes.
        /// </summary>
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
                    if (executable.Type == BroadcastItemType.CallerLine)
                    {
                        // End the current caller's session
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
                            _currentState = AsyncBroadcastState.BetweenCallers;
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
                // Auto-advance from on-hold queue if available
                if (_callerRepository.OnHoldCallers.Count > 0)
                {
                    var putOnAirResult = _callerRepository.PutOnAir();
                    if (putOnAirResult.IsSuccess)
                    {
                        onAirCaller = putOnAirResult.Value;
                        GD.Print($"BroadcastStateManager: Auto-advanced caller {onAirCaller.Name} to on-air");
                    }
                    else
                    {
                        GD.PrintErr($"BroadcastStateManager: Failed to put caller on air: {putOnAirResult.ErrorMessage}");
                    }
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

        private IEnumerable<BroadcastExecutable> CreateAdBreakExecutables()
        {
            var listenerCount = _listenerManager != null ? _listenerManager.CurrentListeners : 100;
            var adCount = Math.Max(1, listenerCount / 50); // 1 ad per 50 listeners

            for (int i = 0; i < adCount; i++)
            {
                yield return AdExecutable.CreateForListenerCount($"ad_break_{i + 1}", listenerCount, i + 1, _eventBus, _listenerManager, _audioService);
            }
        }
    }
}