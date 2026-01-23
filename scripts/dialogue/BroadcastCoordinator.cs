#nullable enable

using System;
using Godot;
using KBTV.Ads;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Data;

namespace KBTV.Dialogue
{
    [GlobalClass]
    public partial class BroadcastCoordinator : Node, IBroadcastCoordinator
    {
        private ICallerRepository _repository = null!;
        private ITranscriptRepository _transcriptRepository = null!;
        private IArcRepository _arcRepository = null!;
        private VernDialogueTemplate _vernDialogue = null!;
        private VernDialogueLoader _vernLoader = null!;
        private TranscriptManager _transcriptManager = null!;
        private LineTimingManager _timingManager = null!;
        private AdBreakCoordinator _adCoordinator = null!;
        private BroadcastStateManager _stateManager = null!;

        // Public accessor for current state (used by AdManager for coordination)
        public BroadcastState CurrentState => _stateManager.CurrentState;

        public void ResetFillerCycleCount() => _stateManager.ResetFillerCycleCount();

        public BroadcastState GetNextStateAfterOffTopic() => _nextStateAfterOffTopic;

        private ConversationArc? _currentArc;
        private System.Collections.Generic.List<ArcDialogueLine>? _resolvedDialogue;
        private ConversationFlow? _currentFlow;
        private ConversationStateMachine? _stateMachine;
        private ConversationContext? _conversationContext;
        private int _arcLineIndex = -1;
        private bool _broadcastActive = false;

        private const int MaxFillerCyclesBeforeAutoAdvance = 1;



        private ControlAction _pendingControlAction = ControlAction.None;
        private BroadcastState _nextStateAfterOffTopic;
        
        // Show ending transition tracking
        private bool _closingLineDelivered = false;
        private bool _outroMusicQueued = false;
        private bool _showEndingPending = false;

        // Ad break state tracking
        private int _currentAdIndex = 0;
        private int _totalAdsInBreak = 0;
        private bool _adBreakActive = false;
        public string? CurrentAdSponsor { get; private set; }
        
        // Return from break sequence tracking
        private enum ReturnFromBreakStep { Music, Dialogue, Complete }
        private ReturnFromBreakStep _returnFromBreakStep = ReturnFromBreakStep.Music;

        private bool _breakTransitionPending = false;
        private BroadcastLine? _pendingTransitionLine = null;

        // Guard to prevent duplicate OnLineCompleted() calls from event and timing paths
        private bool _isProcessingLine = false;



        // Events for break coordination
        public event Action? OnBreakTransitionCompleted;
        public event Action? OnTransitionLineAvailable;

        public enum BroadcastState
        {
            Idle,
            IntroMusic,
            ShowOpening,
            Conversation,
            BetweenCallers,
            AdBreak,
            BreakGracePeriod,
            BreakTransition,
            ReturnFromBreak,
            DeadAirFiller,
            OffTopicRemark,
            ShowClosing,
            ShowEndingQueue,
            ShowEndingPending,
            ShowEndingTransition
        }

        public override void _Ready()
        {
            InitializeWithServices();
        }

        private void InitializeWithServices()
        {
            _repository = ServiceRegistry.Instance.CallerRepository;
            _transcriptRepository = ServiceRegistry.Instance.TranscriptRepository;
            _arcRepository = ServiceRegistry.Instance.ArcRepository;

            _vernLoader = new VernDialogueLoader();
            _vernLoader.LoadDialogue();
            _vernDialogue = _vernLoader.VernDialogue;

            _transcriptManager = new TranscriptManager(_transcriptRepository);
            _timingManager = new LineTimingManager();
            _adCoordinator = new AdBreakCoordinator(ServiceRegistry.Instance.AdManager, _vernDialogue);
            _stateManager = new BroadcastStateManager(_repository, this);

            var adManager = ServiceRegistry.Instance.AdManager;
            if (adManager != null)
            {
                adManager.OnBreakGracePeriod += OnBreakGracePeriod;
                adManager.OnBreakImminent += OnBreakImminent;
            }

            var timeManager = ServiceRegistry.Instance.TimeManager;
            if (timeManager != null)
            {
                timeManager.ShowEndingWarning += OnShowEndingWarning;
            }

            // Event-driven architecture: Subscribe to LineCompletedEvent for state advancement
            var eventBus = ServiceRegistry.Instance.EventBus;
            eventBus.Subscribe<LineCompletedEvent>(HandleLineCompleted);

            ServiceRegistry.Instance.RegisterSelf<BroadcastCoordinator>(this);
        }

        private void HandleLineCompleted(LineCompletedEvent @event)
        {
            GD.Print($"BroadcastCoordinator.HandleLineCompleted: Received - LineId={@event.LineId}");
            OnLineCompleted();
        }

        public void OnLiveShowStarted()
        {
            _broadcastActive = true;
            _transcriptRepository?.StartNewShow();
            _stateManager.SetState(BroadcastState.IntroMusic);
            this.ResetFillerCycleCount();
            _timingManager.StopLine();
            _pendingControlAction = ControlAction.None;
            _closingLineDelivered = false;

            ServiceRegistry.Instance.EventBus.Publish(new ShowStartedEvent());
        }

        public void OnLiveShowEnding()
        {
            GD.Print("BroadcastCoordinator: OnLiveShowEnding called");
            _stateManager.SetState(BroadcastState.Idle);
            _broadcastActive = false;
            _timingManager.StopLine();
            _pendingControlAction = ControlAction.None;
            _closingLineDelivered = false;
            _transcriptRepository?.ClearCurrentShow();
        }

        public void QueueShowEnd()
        {
            // Only queue the outro music when user clicks end show button
            // The actual closing dialog is triggered by timer at T-10s
            QueueOutroMusic();
        }

        public void QueueOutroMusic()
        {
            if (!_outroMusicQueued)
            {
                _outroMusicQueued = true;
                GD.Print("BroadcastCoordinator: Outro music queued for end of show");
            }
        }

        public bool IsOutroMusicQueued() => _outroMusicQueued;

        public void CheckShowEndCondition()
        {
            // At T-10s, trigger the closing dialog sequence
            // This allows the current line to complete, then ShowClosing will play
            if (CurrentState == BroadcastState.Conversation || CurrentState == BroadcastState.DeadAirFiller)
            {
                GD.Print("BroadcastCoordinator: T-10s trigger, preparing show ending transition");
                PrepareShowEndingTransition();
            }
        }

        private void PrepareShowEndingTransition()
        {
            _stateManager.SetState(BroadcastState.ShowEndingTransition);

            var vernStats = ServiceRegistry.Instance.GameStateManager?.VernStats;
            VernMoodType mood = vernStats?.CurrentMoodType ?? VernMoodType.Neutral;

            var closingTemplate = _vernDialogue.GetShowClosing(mood);
            if (closingTemplate == null)
            {
                GD.PrintErr("BroadcastCoordinator: CRITICAL ERROR - No show closing template found!");
                throw new InvalidOperationException("Show closing template is missing from VernDialogueTemplate");
            }

            // Prepare closing line for normal dialogue flow
            _pendingTransitionLine = CreateBroadcastLine(closingTemplate, Speaker.Vern);

            GD.Print($"BroadcastCoordinator: Prepared show ending transition line: {_pendingTransitionLine?.Text ?? "null"}");

            // Publish event for event-driven display system
            if (_pendingTransitionLine.HasValue)
            {
                ServiceRegistry.Instance.EventBus.Publish(new LineAvailableEvent(_pendingTransitionLine.Value));
            }

            // Keep callback for backward compatibility
            OnTransitionLineAvailable?.Invoke();
        }

        private void StartShowEndingTransition()
        {
            PrepareShowEndingTransition();
        }

        private void OnBreakGracePeriod(float timeUntilBreak)
        {
            if (CurrentState == BroadcastState.AdBreak || CurrentState == BroadcastState.BreakGracePeriod) return;

            _stateManager.SetState(BroadcastState.BreakGracePeriod);
            _breakTransitionPending = true;
            GD.Print($"BroadcastCoordinator: Entering grace period, {timeUntilBreak:F1}s until break");
        }

        private void OnBreakImminent(float timeUntilBreak)
        {
            if (CurrentState == BroadcastState.BreakGracePeriod && _timingManager.GetCurrentLine() != null)
            {
                // Interrupt current conversation line and start transition
                StopCurrentLine();
                StartBreakTransition();
            }
            // Removed: interruption of BreakTransition state
            // Let transition lines complete naturally

            GD.Print($"BroadcastCoordinator: Break imminent, {timeUntilBreak:F1}s remaining");
        }

        private void OnShowEndingWarning(float secondsRemaining)
        {
            // Always trigger closing sequence regardless of current state
            // The closing line will play after the current line completes
            _showEndingPending = true;
            _stateManager.SetState(BroadcastState.ShowEndingPending);
        }

        private void StartBreakTransition()
        {
            // Stop any currently playing line first
            if (_timingManager.GetCurrentLine() != null)
            {
                GD.Print("BroadcastCoordinator: Stopping current line before transition");
                StopCurrentLine();
                OnLineCompleted(); // Process completion of interrupted line
            }

            _stateManager.SetState(BroadcastState.BreakTransition);
            _breakTransitionPending = false;

            var transitionTemplate = _vernDialogue.GetBreakTransition();
            if (transitionTemplate == null)
            {
                GD.PrintErr("BroadcastCoordinator: CRITICAL ERROR - No break transition template found!");
                throw new InvalidOperationException("Break transition template is missing from VernDialogueTemplate");
            }

            // Prepare transition line for normal dialogue flow (don't play directly)
            _pendingTransitionLine = CreateBroadcastLine(transitionTemplate, Speaker.Vern);

            GD.Print($"BroadcastCoordinator: Prepared transition line for display: {_pendingTransitionLine?.Text ?? "null"}");

            // Immediately interrupt current line to prevent timing race
            ServiceRegistry.Instance.AudioPlayer?.Stop();

            // Publish event for event-driven display system
            if (_pendingTransitionLine.HasValue)
            {
                ServiceRegistry.Instance.EventBus.Publish(new LineAvailableEvent(_pendingTransitionLine.Value));
            }

            // Notify display that transition line is available
            OnTransitionLineAvailable?.Invoke();
            GD.Print("BroadcastCoordinator: Transition line prepared, notifying display");
        }

        private void StartReturnFromBreakSequence()
        {
            _stateManager.SetState(BroadcastState.ReturnFromBreak);
            _returnFromBreakStep = ReturnFromBreakStep.Music;

            // First step: Play return bumper music
            var musicLine = BroadcastLine.ReturnMusic();
            _pendingTransitionLine = musicLine;

            GD.Print("BroadcastCoordinator: Starting return-from-break sequence with music");

            // Publish event for event-driven display system
            ServiceRegistry.Instance.EventBus.Publish(new LineAvailableEvent(_pendingTransitionLine.Value));

            // Keep callback for backward compatibility
            OnTransitionLineAvailable?.Invoke();
        }

        private void StopCurrentLine()
        {
            if (_timingManager.GetCurrentLine() != null)
            {
                // Stop any audio playback
                var dialoguePlayer = ServiceRegistry.Instance.EventBus as IDialoguePlayer;
                if (dialoguePlayer != null)
                {
                    dialoguePlayer.Stop();
                }

                _timingManager.StopLine();
            }
        }

        private BroadcastLine CreateBroadcastLine(DialogueTemplate template, Speaker speaker)
        {
            return BroadcastLine.VernDialogue(template.Text, ConversationPhase.Probe, null, 0, template.Id);
        }

        public void OnAdBreakStarted()
        {
            GD.Print("BroadcastCoordinator.OnAdBreakStarted: Starting ad break");
            _adCoordinator.OnAdBreakStarted();

            _stateManager.SetState(BroadcastState.AdBreak);
            _timingManager.StopLine();

            GD.Print("BroadcastCoordinator: AdBreak started");

            GD.Print("BroadcastCoordinator: AdBreak started");
        }

        public void OnAdBreakEnded()
        {
            _adCoordinator.OnAdBreakEnded();

            // Start the return-from-break sequence instead of immediate transition
            StartReturnFromBreakSequence();

            _timingManager.StopLine();
            GD.Print("BroadcastCoordinator: AdBreak ended, starting return-from-break sequence");
        }

        public void OnCallerPutOnAir(Caller caller)
        {
            _currentArc = caller.Arc;
            var vernStats = ServiceRegistry.Instance.GameStateManager?.VernStats;
            VernMoodType mood = VernMoodType.Neutral;
            if (vernStats != null)
            {
                mood = vernStats.CurrentMoodType;
                _resolvedDialogue = _currentArc.GetDialogueForMood(mood);
            }
            else
            {
                _resolvedDialogue = new System.Collections.Generic.List<ArcDialogueLine>(_currentArc.Dialogue);
            }

            _currentFlow = ConversationFlow.CreateLinear(_resolvedDialogue);
            _stateMachine = new ConversationStateMachine();
            _conversationContext = new ConversationContext(mood);

            _stateMachine.ProcessEvent(ConversationEvent.Start());

            _arcLineIndex = 0;
            _stateManager.SetState(BroadcastState.Conversation);
            _timingManager.StopLine();
            _pendingControlAction = ControlAction.None;

            var startedEvent = new ConversationStartedEvent();
            ServiceRegistry.Instance.EventBus.Publish(startedEvent);
        }

        public void OnCallerOnAir(Caller caller)
        {
            OnCallerPutOnAir(caller);
        }

        public void OnCallerOnAirEnded(Caller caller)
        {
            _currentArc = null;
            _resolvedDialogue = null;
            _currentFlow = null;
            _stateMachine = null;
            _conversationContext = null;
            _arcLineIndex = -1;

            if (_repository.HasOnHoldCallers)
            {
                _stateManager.SetState(BroadcastState.BetweenCallers);
            }
            else
            {
                _stateManager.SetState(BroadcastState.DeadAirFiller);
                this.ResetFillerCycleCount();
            }

            _timingManager.StopLine();
        }

        public ControlAction GetPendingControlAction()
        {
            // Don't return EndShow during ShowClosing - let the closing line be displayed
            if (!_broadcastActive && CurrentState != BroadcastState.ShowClosing)
            {
                return ControlAction.None;
            }

            // Don't block the closing line from being displayed
            if (CurrentState == BroadcastState.ShowClosing)
            {
                return ControlAction.None;
            }

            if (_pendingControlAction != ControlAction.None)
            {
                return _pendingControlAction;
            }

            if (_timingManager.GetCurrentLine() != null)
            {
                return ControlAction.None;
            }

            return _pendingControlAction;
        }

        public void OnControlActionCompleted()
        {
            _pendingControlAction = ControlAction.None;
        }

        public BroadcastLine? GetNextDisplayLine()
        {
            // Allow closing line to display even when broadcast is no longer active
            if (!_broadcastActive && CurrentState != BroadcastState.ShowClosing && CurrentState != BroadcastState.ShowEndingPending && CurrentState != BroadcastState.ShowEndingTransition && CurrentState != BroadcastState.ShowEndingQueue)
            {
                return null;
            }

            // Priority: Check for pending transition line first
            if (_pendingTransitionLine != null)
            {
                var transitionLine = _pendingTransitionLine.Value;
                _pendingTransitionLine = null;

                // Set up transition line timing and state
                _timingManager.StartLine(transitionLine);

                // Add to transcript immediately
                _transcriptManager.AddEntry(transitionLine);

                // Publish event for event-driven display
                ServiceRegistry.Instance.EventBus.Publish(new LineAvailableEvent(transitionLine));

                return transitionLine;
            }

            var playingLine = _timingManager.GetCurrentLine();
            if (playingLine != null)
            {
                return playingLine;
            }

            if (_pendingControlAction != ControlAction.None)
            {
                return null;
            }

            var currentLine = CalculateNextLine();

            if (currentLine.Type == BroadcastLineType.None)
            {
                return null;
            }

            _timingManager.StartLine(currentLine);

            _transcriptManager.AddEntry(currentLine, _repository.OnAirCaller);

            // Publish event for event-driven display
            ServiceRegistry.Instance.EventBus.Publish(new LineAvailableEvent(currentLine));

            return currentLine;
        }

        public BroadcastLine GetNextLine()
        {
            var displayLine = GetNextDisplayLine();
            return displayLine ?? BroadcastLine.None();
        }

        private BroadcastLine CalculateNextLine()
        {
            var lineCalculator = new LineCalculator(_adCoordinator, _vernDialogue);
            return CurrentState switch
            {
                BroadcastState.IntroMusic => lineCalculator.GetMusicLine(),
                BroadcastState.ShowOpening => lineCalculator.GetShowOpeningLine(_vernDialogue),
                BroadcastState.Conversation => GetConversationLine(),
                BroadcastState.BetweenCallers => lineCalculator.GetBetweenCallersLine(_vernDialogue),
                BroadcastState.AdBreak => _adCoordinator.GetAdBreakLine(),
                BroadcastState.DeadAirFiller => lineCalculator.GetFillerLine(_vernDialogue),
                BroadcastState.OffTopicRemark => lineCalculator.GetOffTopicRemarkLine(_vernDialogue),
                BroadcastState.ShowClosing => lineCalculator.GetShowClosingLine(_vernDialogue),
                BroadcastState.BreakGracePeriod => BroadcastLine.None(), // Wait for transition
                BroadcastState.BreakTransition => BroadcastLine.None(),   // Handled by GetNextDisplayLine
                BroadcastState.ReturnFromBreak => BroadcastLine.None(),   // Handled by GetNextDisplayLine
                BroadcastState.ShowEndingQueue => BroadcastLine.None(),   // Wait for transition
                BroadcastState.ShowEndingPending => BroadcastLine.None(),  // Wait for current line to complete
                BroadcastState.ShowEndingTransition => BroadcastLine.None(), // Handled by GetNextDisplayLine
                _ => BroadcastLine.None()
            };
        }

        public void OnLineCompleted()
        {
            GD.Print($"BroadcastCoordinator.OnLineCompleted: Called - CurrentState={CurrentState}, _isProcessingLine={_isProcessingLine}");

            if (_isProcessingLine)
            {
                GD.Print($"BroadcastCoordinator.OnLineCompleted: Already processing, returning early");
                return;
            }

            _isProcessingLine = true;
            try
            {
                GD.Print($"BroadcastCoordinator.OnLineCompleted: Set _isProcessingLine=true, processing completion");

                _timingManager.StopLine();

            // Check if show ending is pending - set up closing transition when current line completes
            if (CurrentState == BroadcastState.ShowEndingPending)
            {
                _showEndingPending = false;
                PrepareShowEndingTransition();
                return;
            }

            // Check if break transition just completed - start the break
            if (CurrentState == BroadcastState.BreakTransition)
            {
                OnBreakTransitionCompleted?.Invoke();
                _stateManager.SetState(BroadcastState.AdBreak);
                _timingManager.StopLine();

                return; // Break started, don't advance normal flow
            }

            // Check if show ending transition line just completed - end the show
            if (CurrentState == BroadcastState.ShowEndingTransition)
            {
                _stateManager.SetState(BroadcastState.Idle);
                OnLiveShowEnding();
                return; // Show ending, don't advance normal flow
            }

            // Check if return-from-break sequence step completed
            if (CurrentState == BroadcastState.ReturnFromBreak)
            {
                if (_returnFromBreakStep == ReturnFromBreakStep.Music)
                {
                    // Music completed, now play Vern's return dialogue
                    _returnFromBreakStep = ReturnFromBreakStep.Dialogue;

                    var vernStats = ServiceRegistry.Instance.GameStateManager?.VernStats;
                    VernMoodType mood = vernStats?.CurrentMoodType ?? VernMoodType.Neutral;

                    var returnTemplate = _vernDialogue.GetReturnFromBreak(mood);
                    if (returnTemplate == null)
                    {
                        GD.PrintErr("BroadcastCoordinator: CRITICAL ERROR - No return from break template found!");
                        throw new InvalidOperationException("Return from break template is missing from VernDialogueTemplate");
                    }

                    _pendingTransitionLine = CreateBroadcastLine(returnTemplate, Speaker.Vern);

                    // Publish event for event-driven display system
                    ServiceRegistry.Instance.EventBus.Publish(new LineAvailableEvent(_pendingTransitionLine.Value));

                    // Keep callback for backward compatibility
                    OnTransitionLineAvailable?.Invoke();
                    return; // Don't advance normal flow yet
                }
                else if (_returnFromBreakStep == ReturnFromBreakStep.Dialogue)
                {
                    // Dialogue completed, now transition to normal flow
                    _returnFromBreakStep = ReturnFromBreakStep.Complete;

                    if (_repository.HasOnHoldCallers)
                    {
                        _stateManager.SetState(BroadcastState.BetweenCallers);
                    }
                    else
                    {
                    _stateManager.SetState(BroadcastState.DeadAirFiller);
                    this.ResetFillerCycleCount();
                    }

                    return; // Don't advance normal flow
                }
            }

            // Ad progression is handled by AdBreakCoordinator.GetAdBreakLine()
            // No manual index increment needed here

            // Check if we need to start break transition
            if (CurrentState == BroadcastState.BreakGracePeriod && _breakTransitionPending)
            {
                StartBreakTransition();
                return; // Don't advance normal flow
            }

            if (_stateMachine != null)
            {
                _stateMachine.ProcessEvent(ConversationEvent.LineCompleted());
            }

            bool conversationJustEnded = false;

            if (CurrentState == BroadcastState.Conversation &&
                _conversationContext != null &&
                _currentFlow != null &&
                _conversationContext.CurrentStepIndex >= _currentFlow.Steps.Count)
            {
                EndConversation();
                conversationJustEnded = true;
            }

            if (CurrentState == BroadcastState.DeadAirFiller)
            {
                _stateManager.IncrementFillerCycle();
            }

            if (!conversationJustEnded)
            {
                GD.Print($"BroadcastCoordinator.OnLineCompleted: Advancing state from {CurrentState}");
                _stateManager.AdvanceState();
                GD.Print($"BroadcastCoordinator.OnLineCompleted: New state after advance = {CurrentState}");
                if (CurrentState == BroadcastState.Conversation && _repository.OnAirCaller == null && _repository.HasOnHoldCallers)
                {
                    GD.Print($"BroadcastCoordinator.OnLineCompleted: Putting next caller on air");
                    TryPutNextCallerOnAir();
                }
            }

                var advancedEvent = new ConversationAdvancedEvent(null);
                ServiceRegistry.Instance.EventBus.Publish(advancedEvent);

                GD.Print($"BroadcastCoordinator.OnLineCompleted: Complete");
            }
            finally
            {
                _isProcessingLine = false;
                GD.Print($"BroadcastCoordinator.OnLineCompleted: Cleared _isProcessingLine flag in finally");
            }
        }

        private void EndConversation()
        {
            _stateMachine?.ProcessEvent(ConversationEvent.End());

            var currentCaller = _repository.OnAirCaller;
            bool wasOffTopic = currentCaller?.IsOffTopic ?? false;

            if (currentCaller != null)
            {
                _repository.EndOnAir();
                OnCallerOnAirEnded(currentCaller);
            }

            _currentFlow = null;
            _stateMachine = null;
            _conversationContext = null;
            _arcLineIndex = -1;

            if (wasOffTopic)
            {
                _stateManager.SetState(BroadcastState.OffTopicRemark);
                if (_repository.HasOnHoldCallers)
                {
                    _nextStateAfterOffTopic = BroadcastState.BetweenCallers;
                }
                else
                {
                    _nextStateAfterOffTopic = BroadcastState.DeadAirFiller;
                    this.ResetFillerCycleCount();
                }
            }
            else
            {
                if (_repository.HasOnHoldCallers)
                {
                    _stateManager.SetState(BroadcastState.BetweenCallers);
                }
                else
                {
                    _stateManager.SetState(BroadcastState.DeadAirFiller);
                    this.ResetFillerCycleCount();
                }
            }

            _timingManager.StopLine();
        }

        private void TryPutNextCallerOnAir()
        {
            var result = _repository.PutOnAir();
            if (result.IsSuccess)
            {
                OnCallerPutOnAir(result.Value);
            }
        }





















        private BroadcastLine GetConversationLine()
        {
            var caller = _repository.OnAirCaller;

            if (caller == null)
            {
                if (_repository.HasOnHoldCallers)
                {
                    return BroadcastLine.None();
                }
                else
                {
                    _stateManager.SetState(BroadcastState.DeadAirFiller);
                    this.ResetFillerCycleCount();
                    return GetFillerLine();
                }
            }

            if (_stateMachine == null || _currentFlow == null || _conversationContext == null)
            {
                _stateManager.SetState(BroadcastState.DeadAirFiller);
                return GetFillerLine();
            }

            if (!_stateMachine.CanProcessLines)
            {
                return BroadcastLine.None();
            }

            if (_conversationContext.CurrentStepIndex >= _currentFlow.Steps.Count)
            {
                return BroadcastLine.None();
            }

            var step = _currentFlow.Steps[_conversationContext.CurrentStepIndex];

            if (!step.CanExecute(_conversationContext))
            {
                _conversationContext.CurrentStepIndex++;
                return BroadcastLine.None();
            }

            if (step is DialogueStep dialogueStep)
            {
                // Calculate line index for audio file mapping (skip non-dialogue steps)
                int callerLineIndex = 0;
                for (int i = 0; i < _conversationContext.CurrentStepIndex; i++)
                {
                    if (_currentFlow.Steps[i] is DialogueStep)
                    {
                        callerLineIndex++;
                    }
                }

                var broadcastLine = dialogueStep.CreateBroadcastLine(
                    caller.Arc?.ArcId,
                    caller.Arc?.CallerGender,
                    callerLineIndex
                );
                _stateMachine.ProcessEvent(ConversationEvent.LineAvailable(broadcastLine));
                _conversationContext.CurrentStepIndex++;
                return broadcastLine;
            }
            else
            {
                _conversationContext.CurrentStepIndex++;
                return GetConversationLine();
            }
        }









        private BroadcastLine GetFillerLine()
        {
            if (_repository.HasOnHoldCallers && _stateManager.FillerCycleCount >= MaxFillerCyclesBeforeAutoAdvance)
            {
                this.ResetFillerCycleCount();
                return GetFillerLineText();
            }

            return GetFillerLineText();
        }

        private BroadcastLine GetFillerLineText()
        {
            var lineCalculator = new LineCalculator(_adCoordinator, _vernDialogue);
            return lineCalculator.GetFillerLine(_vernDialogue);
        }










    }
}
