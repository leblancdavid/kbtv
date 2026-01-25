#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using KBTV.Callers;
using KBTV.Core;

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
        ShowOpening,
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
        private AsyncBroadcastState _currentState = AsyncBroadcastState.Idle;
        private readonly Queue<BroadcastExecutable> _pendingExecutables = new();
        private bool _isShowActive = false;

        // Dependencies for executable creation
        private readonly IArcRepository _arcRepository;
        private readonly VernDialogueTemplate _vernDialogue;

        public AsyncBroadcastState CurrentState => _currentState;
        public bool IsShowActive => _isShowActive;

        public BroadcastStateManager(
            ICallerRepository callerRepository,
            IArcRepository arcRepository,
            VernDialogueTemplate vernDialogue)
        {
            _callerRepository = callerRepository;
            _arcRepository = arcRepository;
            _vernDialogue = vernDialogue;
            _eventBus = ServiceRegistry.Instance.EventBus;

            // Subscribe to timing events
            _eventBus.Subscribe<BroadcastTimingEvent>(HandleTimingEvent);
        }

        /// <summary>
        /// Start the show and return the first executable.
        /// </summary>
        public BroadcastExecutable? StartShow()
        {
            _isShowActive = true;
            _currentState = AsyncBroadcastState.ShowStarting;
            
            var firstExecutable = CreateShowStartingExecutable();
            if (firstExecutable != null)
            {
                _pendingExecutables.Enqueue(firstExecutable);
            }

            GD.Print("BroadcastStateManager: Show started");
            return firstExecutable;
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
                AsyncBroadcastState.ShowStarting => CreateShowOpeningExecutable(),
                AsyncBroadcastState.ShowOpening => CreateIntroMusicExecutable(),
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
                    _currentState = AsyncBroadcastState.ShowOpening;
                    break;
                case AsyncBroadcastState.ShowOpening:
                    _currentState = AsyncBroadcastState.IntroMusic;
                    break;
                case AsyncBroadcastState.IntroMusic:
                    _currentState = AsyncBroadcastState.Conversation;
                    break;
                case AsyncBroadcastState.Conversation:
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
            return _callerRepository.OnAirCaller != null && 
                   _callerRepository.IncomingCallers.Count > 0;
        }

        /// <summary>
        /// Check if we should play dead air filler.
        /// </summary>
        private bool ShouldPlayDeadAir()
        {
            return _callerRepository.OnAirCaller == null && 
                   _callerRepository.IncomingCallers.Count == 0;
        }

        // Executable creation methods
        private BroadcastExecutable CreateShowStartingExecutable() => 
            new TransitionExecutable("show_start", "Show is starting...", 3.0f, "res://assets/audio/music/intro_music.wav");

        private BroadcastExecutable CreateShowOpeningExecutable() => 
            new MusicExecutable("show_opening", "Show opening music", "res://assets/audio/music/intro_music.wav", 5.0f);

        private BroadcastExecutable CreateIntroMusicExecutable() => 
            new MusicExecutable("intro_music", "Intro music", "res://assets/audio/music/intro_music.wav", 4.0f);

        private BroadcastExecutable CreateConversationExecutable()
        {
            var onAirCaller = _callerRepository.OnAirCaller;
            if (onAirCaller != null)
            {
                // Convert string topic to ShowTopic enum
                var topic = ShowTopicExtensions.ParseTopic(onAirCaller.ActualTopic);
                if (topic.HasValue)
                {
                    var arc = _arcRepository.GetRandomArcForTopic(topic.Value, onAirCaller.Legitimacy);
                    if (arc != null)
                    {
                        return new DialogueExecutable($"dialogue_{onAirCaller.Id}", onAirCaller, arc);
                    }
                }
            }

            // Fallback to Vern dialogue - use introduction lines as fallback
            var vernLines = _vernDialogue.IntroductionLines;
            if (vernLines != null && vernLines.Count > 0)
            {
                var randomIndex = GD.RandRange(0, vernLines.Count - 1);
                var randomLine = vernLines[randomIndex];
                return new DialogueExecutable("vern_fallback", randomLine.Text, "Vern");
            }

            // Final fallback
            return new DialogueExecutable("vern_fallback", "Welcome to the show.", "Vern");
        }

        private BroadcastExecutable CreateBetweenCallersExecutable() => 
            new TransitionExecutable("between_callers", "Transitioning between callers", 4.0f, "res://assets/audio/bumpers/transition.wav");

        private BroadcastExecutable CreateDeadAirExecutable() => 
            new TransitionExecutable("dead_air", "Dead air filler", 8.0f, "res://assets/audio/bumpers/dead_air.wav");

        private BroadcastExecutable CreateReturnFromBreakExecutable() => 
            new TransitionExecutable("break_return", "Returning from break", 3.0f, "res://assets/audio/bumpers/return.wav");

        private BroadcastExecutable CreateShowClosingExecutable() => 
            new MusicExecutable("show_closing", "Show closing music", "res://assets/audio/music/outro_music.wav", 5.0f);

        private IEnumerable<BroadcastExecutable> CreateAdBreakExecutables()
        {
            var listenerManager = ServiceRegistry.Instance.ListenerManager;
            var listenerCount = listenerManager != null ? listenerManager.CurrentListeners : 100;
            var adCount = Math.Max(1, listenerCount / 50); // 1 ad per 50 listeners

            for (int i = 0; i < adCount; i++)
            {
                yield return AdExecutable.CreateForListenerCount($"ad_break_{i + 1}", listenerCount, i + 1);
            }
        }
    }
}