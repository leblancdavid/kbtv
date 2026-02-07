#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Dedicated state machine for managing broadcast state transitions.
    /// Handles the logic for determining next executables and updating state after execution.
    /// </summary>
    public class BroadcastStateMachine
    {
        private readonly ICallerRepository _callerRepository;
        private readonly IArcRepository _arcRepository;
        private readonly VernDialogueTemplate _vernDialogue;
        private readonly EventBus _eventBus;
        private readonly ListenerManager _listenerManager;
        private readonly IBroadcastAudioService _audioService;
        private readonly SceneTree _sceneTree;
        private readonly AdManager _adManager;
        private readonly BroadcastStateManager _stateManager; // Reference to parent for state access
        private readonly TimeManager _timeManager;
        private readonly IGameStateManager _gameStateManager;
        private readonly DeadAirManager _deadAirManager;
        private readonly ConversationStatTracker _statTracker;

        // Off-topic remark tracking
        private bool _pendingOffTopicRemark = false;

        public BroadcastStateMachine(
            ICallerRepository callerRepository,
            IArcRepository arcRepository,
            VernDialogueTemplate vernDialogue,
            EventBus eventBus,
            ListenerManager listenerManager,
            IBroadcastAudioService audioService,
            SceneTree sceneTree,
            AdManager adManager,
            BroadcastStateManager stateManager,
            TimeManager timeManager,
            IGameStateManager gameStateManager,
            DeadAirManager deadAirManager,
            ConversationStatTracker statTracker)
        {
            _callerRepository = callerRepository;
            _arcRepository = arcRepository;
            _vernDialogue = vernDialogue;
            _eventBus = eventBus;
            _listenerManager = listenerManager;
            _audioService = audioService;
            _sceneTree = sceneTree;
            _adManager = adManager;
            _stateManager = stateManager;
            _timeManager = timeManager;
            _gameStateManager = gameStateManager;
            _deadAirManager = deadAirManager;
            _statTracker = statTracker;
        }

        /// <summary>
        /// Get the next executable for the current broadcast state.
        /// </summary>
        public BroadcastExecutable? GetNextExecutable(AsyncBroadcastState currentState)
        {
            
            // Global pending transition checks - these take priority over current state
            if (_stateManager._pendingShowEndingTransition)
            {
                _stateManager.SetState(AsyncBroadcastState.Conversation);
                return CreateConversationExecutable(AsyncBroadcastState.Conversation);
            }
            if (_stateManager._pendingBreakTransition)
            {
                if (currentState == AsyncBroadcastState.AdBreak || 
                    currentState == AsyncBroadcastState.WaitingForBreak ||
                    currentState == AsyncBroadcastState.BreakReturnMusic ||
                    currentState == AsyncBroadcastState.BreakReturn)
                {
                    // Queue the transition for after current break completes
                }
                else
                {
                    _stateManager.SetState(AsyncBroadcastState.Conversation);
                    return CreateConversationExecutable(AsyncBroadcastState.Conversation);
                }
            }

            switch (currentState)
            {
                case AsyncBroadcastState.Idle:
                    return null;
                case AsyncBroadcastState.ShowStarting:
                    return CreateShowStartingExecutable();
                case AsyncBroadcastState.IntroMusic:
                    return CreateIntroMusicExecutable();
                case AsyncBroadcastState.Conversation:
                    return CreateConversationExecutable(currentState);
                case AsyncBroadcastState.BetweenCallers:
                    return CreateBetweenCallersExecutable();
                case AsyncBroadcastState.DeadAir:
                    return CreateDeadAirExecutable();
                 case AsyncBroadcastState.AdBreak:
                        if (!_stateManager.AdBreakInitialized)
                        {
                            int adCount = _adManager?.CurrentBreakSlots ?? 1;
                            int totalAds = Math.Max(adCount, 1);
                            var order = new List<int>();
                            for (int i = 0; i < totalAds; i++) order.Add(i);
                            order = order.OrderBy(x => GD.Randi()).ToList();
                            _stateManager.SetAdBreakState(totalAds, order);
                            _stateManager.SetAdBreakInitialized(true);
                            // Start the break when entering AdBreak state
                            _adManager?.StartBreak();
                        }
                       if (_stateManager.CurrentAdIndex >= _stateManager.TotalAdsForBreak)
                       {
                           // All ads complete, transition to break return music
                           _stateManager.SetState(AsyncBroadcastState.BreakReturnMusic);
                           return CreateReturnFromBreakMusicExecutable();
                       }
                       else
                       {
                           int adSlot = _stateManager.AdOrder[_stateManager.CurrentAdIndex];
                           return CreateAdExecutable(adSlot);
                       }
                case AsyncBroadcastState.WaitingForBreak:
                    float timeUntilBreak = CalculateTimeUntilBreak();
                    return new WaitForBreakExecutable(_eventBus, _audioService, _sceneTree, _callerRepository, timeUntilBreak);
                case AsyncBroadcastState.WaitingForShowEnd:
                    float timeUntilShowEnd = CalculateTimeUntilShowEnd();
                    return new WaitForBreakExecutable(_eventBus, _audioService, _sceneTree, _callerRepository, timeUntilShowEnd, "Show ending...");
                case AsyncBroadcastState.BreakReturnMusic:
                    return CreateReturnFromBreakMusicExecutable();
                case AsyncBroadcastState.BreakReturn:
                    return CreateReturnFromBreakExecutable();
                case AsyncBroadcastState.DroppedCaller:
                    var droppedCaller = _vernDialogue.GetDroppedCaller();
                    if (droppedCaller != null)
                    {
                        var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{droppedCaller.Id}.mp3";
                          return new DialogueExecutable("dropped_caller", droppedCaller.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.DroppedCaller, _stateManager, _statTracker);
                    }
                      return new DialogueExecutable("dropped_caller", "Looks like we lost that caller...", "Vern", _eventBus, _audioService, lineType: VernLineType.DroppedCaller, stateManager: _stateManager, statTracker: _statTracker);
                 case AsyncBroadcastState.CallerCursed:
                     var cursedCaller = _vernDialogue.GetCallerCursed();
                     if (cursedCaller != null)
                     {
                         var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{cursedCaller.Id}.mp3";
                           return new DialogueExecutable("caller_cursed", cursedCaller.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.CallerCursed, _stateManager, _statTracker);
                     }
                       return new DialogueExecutable("caller_cursed", "You can't use that kind of language on my show!", "Vern", _eventBus, _audioService, lineType: VernLineType.CallerCursed, stateManager: _stateManager, statTracker: _statTracker);
                  case AsyncBroadcastState.CursingDelay:
                      return new CursingDelayExecutable(_eventBus, _audioService, _sceneTree);
                   case AsyncBroadcastState.ShowClosing:
                  case AsyncBroadcastState.ShowEnding:
                      return null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Update state after executable execution.
        /// </summary>
        public AsyncBroadcastState UpdateStateAfterExecution(AsyncBroadcastState currentState, BroadcastExecutable executable)
        {
            AsyncBroadcastState newState = currentState;

            switch (currentState)
            {
                case AsyncBroadcastState.ShowStarting:
                    newState = AsyncBroadcastState.IntroMusic;
                    break;
                case AsyncBroadcastState.IntroMusic:
                    newState = AsyncBroadcastState.Conversation;
                    break;
                case AsyncBroadcastState.Conversation:
                    newState = HandleConversationStateTransition(executable);
                    break;
                case AsyncBroadcastState.BetweenCallers:
                    newState = AsyncBroadcastState.Conversation;
                    break;
                case AsyncBroadcastState.DeadAir:
                    if (ShouldPlayBetweenCallers())
                    {
                        // Go directly to Conversation - no "between callers" transition needed
                        // since we're coming from dead air, not from a previous caller
                        newState = AsyncBroadcastState.Conversation;
                    }
                    // Else stay in DeadAir
                    break;
                case AsyncBroadcastState.DroppedCaller:
                    // After dropped caller line plays, check if there are more callers
                    if (ShouldPlayBetweenCallers())
                    {
                        newState = AsyncBroadcastState.BetweenCallers;
                    }
                    else
                    {
                        newState = AsyncBroadcastState.DeadAir;
                    }
                    break;
                 case AsyncBroadcastState.CallerCursed:
                     // After cursed caller response, check if there are more callers
                     if (ShouldPlayBetweenCallers())
                     {
                         newState = AsyncBroadcastState.BetweenCallers;
                     }
                     else
                     {
                         newState = AsyncBroadcastState.DeadAir;
                     }
                     break;
                 case AsyncBroadcastState.CursingDelay:
                     // After cursing delay completes, transition to Vern's cursing response
                     newState = AsyncBroadcastState.CallerCursed;
                     break;
                 case AsyncBroadcastState.AdBreak:
                     _stateManager.IncrementAdIndex();
                     if (_stateManager.CurrentAdIndex >= _stateManager.TotalAdsForBreak)
                     {
                         // All ads complete, transition to break return music
                         newState = AsyncBroadcastState.BreakReturnMusic;
                     }
                     // Else stay in AdBreak for next ad
                     break;
                 case AsyncBroadcastState.BreakReturnMusic:
                     newState = AsyncBroadcastState.BreakReturn;
                     break;
                   case AsyncBroadcastState.BreakReturn:
                       // Check if return dialogue completed and determine next state based on callers
                       if (executable.Type == BroadcastItemType.VernLine &&
                           executable is DialogueExecutable returnExecutable &&
                           returnExecutable.LineType == VernLineType.ReturnFromBreak)
                       {
                           if (_callerRepository.OnHoldCallers.Count > 0)
                           {
                               newState = AsyncBroadcastState.Conversation;
                           }
                           else
                           {
                               newState = AsyncBroadcastState.DeadAir;
                           }
                       }
                       else
                       {
                           newState = AsyncBroadcastState.Conversation;  // Fallback for other executables
                       }
                       break;
                  case AsyncBroadcastState.WaitingForBreak:
                      newState = AsyncBroadcastState.AdBreak;
                      break;
                   case AsyncBroadcastState.WaitingForShowEnd:
                       newState = AsyncBroadcastState.ShowEnding;
                       break;
                   case AsyncBroadcastState.ShowEnding:
                       newState = AsyncBroadcastState.Idle;
                       break;
            }

            // Handle dead air manager notifications
            if (currentState == AsyncBroadcastState.DeadAir && newState != AsyncBroadcastState.DeadAir)
            {
                // Transitioning FROM DeadAir to another state
                _deadAirManager?.OnDeadAirEnded();
            }
            else if (currentState != AsyncBroadcastState.DeadAir && newState == AsyncBroadcastState.DeadAir)
            {
                // Transitioning TO DeadAir from another state
                _deadAirManager?.OnDeadAirStarted();
            }

            return newState;
        }

        private AsyncBroadcastState HandleConversationStateTransition(BroadcastExecutable executable)
        {
            // Handle break transition completion
            if (executable.Type == BroadcastItemType.VernLine &&
                executable is DialogueExecutable breakTransitionExecutable &&
                breakTransitionExecutable.LineType == VernLineType.BreakTransition)
            {
                _stateManager._pendingBreakTransition = false;
                return AsyncBroadcastState.WaitingForBreak;
            }

            // Handle show closing completion
            if (executable.Type == BroadcastItemType.VernLine &&
                executable is DialogueExecutable showClosingExecutable &&
                showClosingExecutable.LineType == VernLineType.ShowClosing)
            {
                _stateManager._pendingShowEndingTransition = false;
                _stateManager._showClosingStarted = false;
                return AsyncBroadcastState.WaitingForShowEnd;
            }

            if (executable.Type == BroadcastItemType.VernLine &&
                executable is DialogueExecutable vernExecutable &&
                vernExecutable.LineType == VernLineType.ShowOpening)
            {
                _stateManager._hasPlayedVernOpening = true;
                if (_callerRepository.OnHoldCallers.Count > 0)
                {
                    return AsyncBroadcastState.Conversation;
                }
                else
                {
                    return AsyncBroadcastState.DeadAir;
                }
            }

            if (executable.Type == BroadcastItemType.VernLine &&
                executable is DialogueExecutable returnExecutable &&
                returnExecutable.LineType == VernLineType.ReturnFromBreak)
            {
                if (_callerRepository.OnHoldCallers.Count > 0)
                {
                    return AsyncBroadcastState.Conversation;
                }
                else
                {
                    return AsyncBroadcastState.DeadAir;
                }
            }

            // Handle PutOnAir completion
            if (executable.Type == BroadcastItemType.PutOnAir)
            {
                return AsyncBroadcastState.Conversation;
            }

            // Handle caller conversation completion
            if (executable.Type == BroadcastItemType.Conversation)
            {
                
                if (_stateManager._pendingBreakTransition)
                {
                    return AsyncBroadcastState.Conversation; // Stay for break transition
                }

                 // Handle pending caller drop
                 if (_stateManager._pendingCallerDropped)
                 {
                     _stateManager._pendingCallerDropped = false;
                     return AsyncBroadcastState.DroppedCaller;
                 }

                 // Handle pending caller cursed (must check before normal completion logic)
                 if (_stateManager._pendingCallerCursed)
                 {
                     _stateManager._pendingCallerCursed = false;
                     return AsyncBroadcastState.CursingDelay;
                 }
 
                 var endResult = _callerRepository.EndOnAir();
                
                Caller? caller = null;
                if (endResult.IsSuccess)
                {
                    
                     // NOTE: Stat effects are now applied gradually during conversation via ConversationStatTracker
                     // Only apply penalties here (they still apply at conversation end)
                     var vernStats = _gameStateManager?.VernStats;
                     if (vernStats != null)
                     {
                        
                         // Apply penalties only (stat effects already applied gradually)
                         caller = endResult.Value;
                         if (caller.IsOffTopic)
                         {
                             vernStats.ApplyOffTopicPenalty();
                             _pendingOffTopicRemark = true;
                         }
                         
                         if (caller.Legitimacy == CallerLegitimacy.Fake)
                             vernStats.ApplyHoaxerFooledPenalty();
                     }
                 }
                               
                var shouldPlayBetween = ShouldPlayBetweenCallers();
                var shouldPlayDead = ShouldPlayDeadAir();
                
                // If we have a pending off-topic remark, stay in conversation state to play it
                if (_pendingOffTopicRemark)
                {
                    return AsyncBroadcastState.Conversation;
                }
                
                if (shouldPlayBetween)
                {
                    return AsyncBroadcastState.BetweenCallers;
                }
                else if (shouldPlayDead)
                {
                    return AsyncBroadcastState.DeadAir;
                }
                else
                {
                    return AsyncBroadcastState.Conversation;
                }
            }

            // Fallback
            if (ShouldPlayBetweenCallers())
            {
                return AsyncBroadcastState.BetweenCallers;
            }
            else if (ShouldPlayDeadAir())
            {
                return AsyncBroadcastState.DeadAir;
            }

            return AsyncBroadcastState.Conversation;
        }

        private bool ShouldPlayBetweenCallers() => _callerRepository.OnHoldCallers.Count > 0;

        private bool ShouldPlayDeadAir()
        {
            // Don't play dead air if we have an active caller or callers waiting
            if (_callerRepository.OnAirCaller != null || _callerRepository.OnHoldCallers.Count > 0)
                return false;

            // Don't play dead air if show ending is pending - prioritize closing dialogue
            if (_stateManager._pendingShowEndingTransition)
                return false;

            // If no callers and no pending transitions, we should play dead air filler
            return true;
        }

        // Executable creation methods (simplified versions)
        private BroadcastExecutable CreateShowStartingExecutable() =>
            new TransitionExecutable("show_start", "Show is starting...", 3.0f, _eventBus, _audioService, _sceneTree, null);

        private BroadcastExecutable CreateIntroMusicExecutable() =>
            new MusicExecutable("intro_music", "Intro music", "res://assets/audio/music/intro_music.wav", 4.0f, _eventBus, _audioService, _sceneTree);


        private BroadcastExecutable CreateConversationExecutable(AsyncBroadcastState currentState)
        {
            
            // Check for pending off-topic remark first
            if (_pendingOffTopicRemark)
            {
                _pendingOffTopicRemark = false;
                
                // Get mood-appropriate remark
                var vernStats = _gameStateManager?.VernStats;
                var mood = vernStats?.CurrentMoodType ?? VernMoodType.Neutral;
                var remark = _vernDialogue.GetOffTopicRemark(mood);
                if (remark != null)
                {
                    var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{remark.Id}.mp3";
                    audioPath = ValidateAudioPath(audioPath, "CreateConversationExecutable_OffTopicRemark");
                    return new DialogueExecutable("off_topic_remark", remark.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.OffTopicRemark, _stateManager, _statTracker);
                }
                else
                {
                }
            }
            
            if (!_stateManager._hasPlayedVernOpening)
            {
                var topic = _gameStateManager?.SelectedTopic?.TopicValue ?? ShowTopic.Ghosts; // Fallback to Ghosts if no topic
                var opening = _vernDialogue.GetShowOpening(topic);
                if (opening != null)
                {
                    var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{opening.Id}.mp3";
                    audioPath = ValidateAudioPath(audioPath, "CreateConversationExecutable_ShowOpening");
                    return new DialogueExecutable("vern_fallback", opening.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.ShowOpening, _stateManager, _statTracker);
                }
            }

             if (_stateManager._pendingBreakTransition && !_stateManager._pendingShowEndingTransition)
             {
                 var breakTransition = _vernDialogue.GetBreakTransition();
                 if (breakTransition != null)
                 {
                     var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{breakTransition.Id}.mp3";
                     audioPath = ValidateAudioPath(audioPath, "CreateConversationExecutable_BreakTransition");
                      return new DialogueExecutable("break_transition", breakTransition.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.BreakTransition, _stateManager, _statTracker);
                 }
             }

            if (_stateManager._pendingShowEndingTransition)
            {
                var topic = _gameStateManager?.SelectedTopic?.TopicValue ?? ShowTopic.Ghosts; // Fallback to Ghosts if no topic
                var closing = _vernDialogue.GetShowClosing(topic);
                if (closing != null)
                {
                    _stateManager._showClosingStarted = true;  // Track that closing has started
                    var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{closing.Id}.mp3";
                    audioPath = ValidateAudioPath(audioPath, "CreateConversationExecutable_ShowClosing");
                     return new DialogueExecutable("show_closing", closing.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.ShowClosing, _stateManager, _statTracker);
                }
                else
                {
                }
            }

            var onAirCaller = _callerRepository.OnAirCaller;
            if (onAirCaller == null)
            {
                if (_callerRepository.OnHoldCallers.Count > 0 && !_callerRepository.IsOnAir)
                {
                    return new PutOnAirExecutable(_eventBus, _callerRepository, _stateManager);
                }
            }

            if (onAirCaller != null)
            {
                var topic = ShowTopicExtensions.ParseTopic(onAirCaller.ActualTopic);
                if (topic.HasValue)
                {
                    var arc = _arcRepository.GetRandomArcForTopic(topic.Value, onAirCaller.Legitimacy);
                    if (arc != null)
                    {
                        return new DialogueExecutable($"dialogue_{onAirCaller.Id}", onAirCaller, arc, _eventBus, _audioService, _stateManager, _statTracker);
                    }
                    else
                    {
                    }
                }
                else
                {
                }
            }

            if (_callerRepository.IncomingCallers.Count > 0)
            {
                return null;
            }


            if (ShouldPlayDeadAir())
            {
                return CreateDeadAirExecutable();
            }
            else
            {
                 return new DialogueExecutable("vern_fallback", "Welcome to the show.", "Vern", _eventBus, _audioService, lineType: VernLineType.Fallback, stateManager: _stateManager, statTracker: _statTracker);
            }
        }

        private BroadcastExecutable CreateBetweenCallersExecutable()
        {
            var betweenCallers = _vernDialogue.GetBetweenCallers();
            if (betweenCallers != null)
            {
                var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{betweenCallers.Id}.mp3";
                audioPath = ValidateAudioPath(audioPath, "CreateBetweenCallersExecutable");
                 return new DialogueExecutable("between_callers", betweenCallers.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.BetweenCallers, _stateManager, _statTracker);
            }
             return new DialogueExecutable("between_callers", "Moving to our next caller...", "Vern", _eventBus, _audioService, lineType: VernLineType.BetweenCallers, stateManager: _stateManager, statTracker: _statTracker);
        }

        private BroadcastExecutable CreateDeadAirExecutable()
        {
            var topic = _gameStateManager?.SelectedTopic?.TopicValue ?? ShowTopic.Ghosts; // Fallback to Ghosts if no topic
            var filler = _vernDialogue.GetDeadAirFiller(topic);
            var text = filler?.Text ?? "Dead air filler";
            var audioPath = filler != null ? $"res://assets/audio/voice/Vern/Broadcast/{filler.Id}.mp3" : null;
            audioPath = ValidateAudioPath(audioPath, "CreateDeadAirExecutable");
             return new DialogueExecutable("dead_air", text, "Vern", _eventBus, _audioService, audioPath, VernLineType.DeadAirFiller, _stateManager, _statTracker);
        }

        private BroadcastExecutable CreateAdExecutable(int adSlot)
        {
            var listenerCount = _listenerManager?.CurrentListeners ?? 100;
            return AdExecutable.CreateForListenerCount($"ad_{adSlot}", listenerCount, adSlot, _eventBus, _listenerManager, _audioService, _sceneTree);
        }


        private BroadcastExecutable CreateReturnFromBreakMusicExecutable() =>
            new TransitionExecutable("break_return_music", "Return bumper music", 4.0f, _eventBus, _audioService, _sceneTree, GetRandomReturnBumperPath());

        private BroadcastExecutable CreateReturnFromBreakExecutable()
        {
            var topic = _gameStateManager?.SelectedTopic?.TopicValue ?? ShowTopic.Ghosts; // Fallback to Ghosts if no topic
            var returnLine = _vernDialogue.GetReturnFromBreak(topic);
            if (returnLine != null)
            {
                var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{returnLine.Id}.mp3";
                audioPath = ValidateAudioPath(audioPath, "CreateReturnFromBreakExecutable");
                 return new DialogueExecutable("break_return", returnLine.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.ReturnFromBreak, _stateManager, _statTracker);
            }
             return new DialogueExecutable("break_return", "We're back!", "Vern", _eventBus, _audioService, lineType: VernLineType.ReturnFromBreak, stateManager: _stateManager, statTracker: _statTracker);
        }

        private string? GetRandomReturnBumperPath()
        {
            var returnBumperDir = DirAccess.Open("res://assets/audio/bumpers/Return");
            if (returnBumperDir == null)
            {
                return null;
            }

            var bumperFiles = new System.Collections.Generic.List<string>();
            returnBumperDir.ListDirBegin();
            string fileName = returnBumperDir.GetNext();
            while (fileName != "")
            {
                if (!fileName.StartsWith(".") && (fileName.EndsWith(".ogg") || fileName.EndsWith(".wav") || fileName.EndsWith(".mp3")))
                {
                    bumperFiles.Add(fileName);
                }
                fileName = returnBumperDir.GetNext();
            }
            returnBumperDir.ListDirEnd();

            if (bumperFiles.Count == 0)
            {
                return null;
            }

            var random = new Random();
            var selectedFile = bumperFiles[random.Next(bumperFiles.Count)];
            var path = $"res://assets/audio/bumpers/Return/{selectedFile}";

            return path;
        }

        private string? ValidateAudioPath(string? audioPath, string context)
        {
            if (audioPath == null) return null;
            if (!FileAccess.FileExists(audioPath))
            {
                if (!_audioService.IsAudioDisabled)
                {
                }
                return null;
            }
            return audioPath;
        }

        /// <summary>
        /// Calculate the time until the next break starts, with safety validation.
        /// </summary>
        private float CalculateTimeUntilBreak()
        {
            float nextBreakTime = _adManager.GetNextBreakTime();
            float currentTime = _stateManager.ElapsedTime;
            float timeUntilBreak = nextBreakTime - currentTime;

            // Validate the calculation
            const float MAX_WAIT_TIME = 20.0f; // Should never wait more than 20s for a break
            const float MIN_WAIT_TIME = 0.1f;  // Must wait at least a tiny bit

            if (timeUntilBreak <= 0)
            {
                return MIN_WAIT_TIME;
            }

            if (timeUntilBreak > MAX_WAIT_TIME)
            {
                return MAX_WAIT_TIME;
            }

            return timeUntilBreak;
        }

        /// <summary>
        /// Calculate the time until show ends, with safety validation.
        /// </summary>
        private float CalculateTimeUntilShowEnd()
        {
            float showDuration = _timeManager?.ShowDuration ?? 600.0f;
            float currentTime = _stateManager.ElapsedTime;
            float timeUntilShowEnd = showDuration - currentTime;

            // Validate the calculation
            const float MAX_WAIT_TIME = 30.0f; // Should never wait more than 30s for show end
            const float MIN_WAIT_TIME = 0.1f;  // Must wait at least a tiny bit

            if (timeUntilShowEnd <= 0)
            {
                return MIN_WAIT_TIME;
            }

            if (timeUntilShowEnd > MAX_WAIT_TIME)
            {
                return MAX_WAIT_TIME;
            }

            return timeUntilShowEnd;
        }


    }
}