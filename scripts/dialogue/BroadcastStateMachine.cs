#nullable enable

using System;
using System.Collections.Generic;
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

        public BroadcastStateMachine(
            ICallerRepository callerRepository,
            IArcRepository arcRepository,
            VernDialogueTemplate vernDialogue,
            EventBus eventBus,
            ListenerManager listenerManager,
            IBroadcastAudioService audioService,
            SceneTree sceneTree,
            AdManager adManager,
            BroadcastStateManager stateManager)
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
        }

        /// <summary>
        /// Get the next executable for the current broadcast state.
        /// </summary>
        public BroadcastExecutable? GetNextExecutable(AsyncBroadcastState currentState)
        {
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
                    return _stateManager._adBreakSequenceRunning ? null : CreateAdBreakSequenceExecutable();
                case AsyncBroadcastState.WaitingForBreak:
                    float timeUntilBreak = CalculateTimeUntilBreak();
                    return new WaitForBreakExecutable(_eventBus, _audioService, _sceneTree, timeUntilBreak);
                case AsyncBroadcastState.BreakReturnMusic:
                    return CreateReturnFromBreakMusicExecutable();
                case AsyncBroadcastState.BreakReturn:
                    return CreateReturnFromBreakExecutable();
                case AsyncBroadcastState.DroppedCaller:
                    var droppedCaller = _vernDialogue.GetDroppedCaller();
                    if (droppedCaller != null)
                    {
                        var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{droppedCaller.Id}.mp3";
                        return new DialogueExecutable("dropped_caller", droppedCaller.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.DroppedCaller, _stateManager);
                    }
                    return new DialogueExecutable("dropped_caller", "Looks like we lost that caller...", "Vern", _eventBus, _audioService, lineType: VernLineType.DroppedCaller, stateManager: _stateManager);
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
                        newState = AsyncBroadcastState.BetweenCallers;
                    }
                    // Else stay in DeadAir
                    break;
                case AsyncBroadcastState.AdBreak:
                    _stateManager._adBreakSequenceRunning = false;
                    newState = AsyncBroadcastState.BreakReturnMusic;
                    break;
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

                var endResult = _callerRepository.EndOnAir();
                if (endResult.IsSuccess)
                {
                    GD.Print($"BroadcastStateMachine: Ended on-air caller {endResult.Value.Name}");
                }

                if (ShouldPlayBetweenCallers())
                {
                    return AsyncBroadcastState.BetweenCallers;
                }
                else if (ShouldPlayDeadAir())
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
            if (_callerRepository.OnAirCaller != null || _callerRepository.OnHoldCallers.Count > 0)
                return false;

            return _stateManager._previousState == AsyncBroadcastState.ShowStarting ||
                   _stateManager._previousState == AsyncBroadcastState.BetweenCallers ||
                   _stateManager._previousState == AsyncBroadcastState.BreakReturn ||
                   _stateManager._previousState == AsyncBroadcastState.DeadAir ||
                   _stateManager._previousState == AsyncBroadcastState.Conversation;
        }

        // Executable creation methods (simplified versions)
        private BroadcastExecutable CreateShowStartingExecutable() =>
            new TransitionExecutable("show_start", "Show is starting...", 3.0f, _eventBus, _audioService, _sceneTree, null);

        private BroadcastExecutable CreateIntroMusicExecutable() =>
            new MusicExecutable("intro_music", "Intro music", "res://assets/audio/music/intro_music.wav", 4.0f, _eventBus, _audioService, _sceneTree);

        private BroadcastExecutable CreateConversationExecutable(AsyncBroadcastState currentState)
        {
            if (!_stateManager._hasPlayedVernOpening)
            {
                var opening = _vernDialogue.GetShowOpening();
                if (opening != null)
                {
                    var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{opening.Id}.mp3";
                    audioPath = ValidateAudioPath(audioPath, "CreateConversationExecutable_ShowOpening");
                    return new DialogueExecutable("vern_fallback", opening.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.ShowOpening, _stateManager);
                }
            }

            if (_stateManager._pendingBreakTransition)
            {
                var breakTransition = _vernDialogue.GetBreakTransition();
                if (breakTransition != null)
                {
                    var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{breakTransition.Id}.mp3";
                    audioPath = ValidateAudioPath(audioPath, "CreateConversationExecutable_BreakTransition");
                    return new DialogueExecutable("break_transition", breakTransition.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.BreakTransition, _stateManager);
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
                        return new DialogueExecutable($"dialogue_{onAirCaller.Id}", onAirCaller, arc, _eventBus, _audioService, _stateManager);
                    }
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
                return new DialogueExecutable("vern_fallback", "Welcome to the show.", "Vern", _eventBus, _audioService, lineType: VernLineType.Fallback, stateManager: _stateManager);
            }
        }

        private BroadcastExecutable CreateBetweenCallersExecutable()
        {
            var betweenCallers = _vernDialogue.GetBetweenCallers();
            if (betweenCallers != null)
            {
                var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{betweenCallers.Id}.mp3";
                audioPath = ValidateAudioPath(audioPath, "CreateBetweenCallersExecutable");
                return new DialogueExecutable("between_callers", betweenCallers.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.BetweenCallers, _stateManager);
            }
            return new DialogueExecutable("between_callers", "Moving to our next caller...", "Vern", _eventBus, _audioService, lineType: VernLineType.BetweenCallers, stateManager: _stateManager);
        }

        private BroadcastExecutable CreateDeadAirExecutable()
        {
            var filler = _vernDialogue.GetDeadAirFiller();
            var text = filler?.Text ?? "Dead air filler";
            var audioPath = filler != null ? $"res://assets/audio/voice/Vern/Broadcast/{filler.Id}.mp3" : null;
            audioPath = ValidateAudioPath(audioPath, "CreateDeadAirExecutable");
            return new DialogueExecutable("dead_air", text, "Vern", _eventBus, _audioService, audioPath, VernLineType.DeadAirFiller, _stateManager);
        }

        private BroadcastExecutable CreateAdBreakSequenceExecutable()
        {
            var adCount = _adManager?.CurrentBreakSlots ?? 6;
            GD.Print($"BroadcastStateMachine: Creating AdBreakSequence executable - will play {adCount} ads sequentially");
            _stateManager._adBreakSequenceRunning = true;
            return new AdBreakSequenceExecutable("ad_break_sequence", _eventBus, _listenerManager, _audioService, _sceneTree, adCount);
        }

        private BroadcastExecutable CreateReturnFromBreakMusicExecutable() =>
            new TransitionExecutable("break_return_music", "Return bumper music", 4.0f, _eventBus, _audioService, _sceneTree, GetRandomReturnBumperPath());

        private BroadcastExecutable CreateReturnFromBreakExecutable()
        {
            var returnLine = _vernDialogue.GetReturnFromBreak();
            if (returnLine != null)
            {
                var audioPath = $"res://assets/audio/voice/Vern/Broadcast/{returnLine.Id}.mp3";
                audioPath = ValidateAudioPath(audioPath, "CreateReturnFromBreakExecutable");
                return new DialogueExecutable("break_return", returnLine.Text, "Vern", _eventBus, _audioService, audioPath, VernLineType.Fallback, _stateManager);
            }
            return new DialogueExecutable("break_return", "We're back!", "Vern", _eventBus, _audioService, lineType: VernLineType.Fallback, stateManager: _stateManager);
        }

        private string? GetRandomReturnBumperPath()
        {
            var returnBumperDir = DirAccess.Open("res://assets/audio/bumpers/Return");
            if (returnBumperDir == null)
            {
                GD.Print("BroadcastStateMachine.GetRandomReturnBumperPath: Return bumper directory not found");
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
                GD.Print("BroadcastStateMachine.GetRandomReturnBumperPath: No return bumper files found");
                return null;
            }

            var random = new Random();
            var selectedFile = bumperFiles[random.Next(bumperFiles.Count)];
            var path = $"res://assets/audio/bumpers/Return/{selectedFile}";

            GD.Print($"BroadcastStateMachine: Selected return bumper: {selectedFile}");
            return path;
        }

        private string? ValidateAudioPath(string? audioPath, string context)
        {
            if (audioPath == null) return null;
            if (!FileAccess.FileExists(audioPath))
            {
                if (!_audioService.IsAudioDisabled)
                {
                    GD.Print($"BroadcastStateMachine.{context}: Audio file not found, falling back to timeout: {audioPath}");
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
                GD.Print($"BroadcastStateMachine.CalculateTimeUntilBreak: Warning - calculated negative/zero time until break ({timeUntilBreak:F1}s). Break may have already started. Using minimum wait time.");
                return MIN_WAIT_TIME;
            }

            if (timeUntilBreak > MAX_WAIT_TIME)
            {
                GD.Print($"BroadcastStateMachine.CalculateTimeUntilBreak: Warning - calculated time until break ({timeUntilBreak:F1}s) exceeds maximum ({MAX_WAIT_TIME}s). Using maximum wait time.");
                return MAX_WAIT_TIME;
            }

            GD.Print($"BroadcastStateMachine.CalculateTimeUntilBreak: Calculated {timeUntilBreak:F1}s until next break (nextBreakTime: {nextBreakTime:F1}s, currentTime: {currentTime:F1}s)");
            return timeUntilBreak;
        }
    }
}