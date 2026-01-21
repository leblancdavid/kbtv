#nullable enable

using System;
using System.Linq;
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

        private bool _breakTransitionPending = false;
        private BroadcastLine? _pendingTransitionLine = null;

        // Ad break state tracking
        private int _currentAdIndex = 0;
        private int _totalAdsInBreak = 0;
        private bool _adBreakActive = false;
        public string? CurrentAdSponsor { get; private set; }

        // Return from break sequence tracking
        private enum ReturnFromBreakStep { Music, Dialogue, Complete }
        private ReturnFromBreakStep _returnFromBreakStep = ReturnFromBreakStep.Music;

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

            ServiceRegistry.Instance.RegisterSelf<BroadcastCoordinator>(this);
        }

        public override void _Process(double delta)
        {
            if (_timingManager.UpdateProgress((float)delta))
            {
                OnLineCompleted();
            }
        }

        public void OnLiveShowStarted()
        {
            _broadcastActive = true;
            _transcriptRepository?.StartNewShow();
            _stateManager.SetState(BroadcastState.IntroMusic);
            this.ResetFillerCycleCount();
            _timingManager.StopLine();
            _pendingControlAction = ControlAction.None;

            ServiceRegistry.Instance.EventBus.Publish(new ShowStartedEvent());
        }

        public void OnLiveShowEnding()
        {
            _stateManager.SetState(BroadcastState.ShowClosing);
            _broadcastActive = false;
            _timingManager.StopLine();
            _pendingControlAction = ControlAction.None;
            _transcriptRepository?.ClearCurrentShow();
        }

        public void QueueShowEnd()
        {
            if (_broadcastActive && (CurrentState == BroadcastState.Conversation || CurrentState == BroadcastState.DeadAirFiller))
            {
                _stateManager.SetState(BroadcastState.ShowEndingQueue);
                _pendingControlAction = ControlAction.EndShow;
                GD.Print("BroadcastCoordinator: Show end queued");
            }
        }

        public void CheckShowEndCondition()
        {
            if (_pendingControlAction == ControlAction.EndShow &&
                (CurrentState == BroadcastState.ShowEndingQueue || CurrentState == BroadcastState.Conversation))
            {
                StartShowEndingTransition();
            }
        }

        private void StartShowEndingTransition()
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

            GD.Print($"BroadcastCoordinator: Prepared show closing line for display: {_pendingTransitionLine?.Text ?? "null"}");

            // Notify display that transition line is available
            OnTransitionLineAvailable?.Invoke();
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

            // Notify display that transition line is available
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
            return BroadcastLine.VernDialogue(template.Text, ConversationPhase.Probe, template.Id);
        }

        public void OnAdBreakStarted()
        {
            if (_timingManager.GetCurrentLine() != null)
            {
                StopCurrentLine();
            }

            _stateManager.SetState(BroadcastState.AdBreak);
            _timingManager.StopLine();
            _adCoordinator.OnAdBreakStarted();

            GD.Print($"BroadcastCoordinator: Starting ad break with {_totalAdsInBreak} ads");
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
            if (!_broadcastActive && CurrentState != BroadcastState.ShowClosing)
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
            if (!_broadcastActive && CurrentState != BroadcastState.ShowClosing)
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

                GD.Print($"BroadcastCoordinator: Delivering transition line to display: {transitionLine.Text}");
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

            return currentLine;
        }

        public BroadcastLine GetNextLine()
        {
            var displayLine = GetNextDisplayLine();
            return displayLine ?? BroadcastLine.None();
        }

        private BroadcastLine CalculateNextLine()
        {
            return CurrentState switch
            {
                BroadcastState.IntroMusic => GetMusicLine(),
                BroadcastState.ShowOpening => GetShowOpeningLine(),
                BroadcastState.Conversation => GetConversationLine(),
                BroadcastState.BetweenCallers => GetBetweenCallersLine(),
                BroadcastState.AdBreak => GetAdBreakLine(),
                BroadcastState.DeadAirFiller => GetFillerLine(),
                BroadcastState.OffTopicRemark => GetOffTopicRemarkLine(),
                BroadcastState.ShowClosing => GetShowClosingLine(),
                BroadcastState.BreakGracePeriod => BroadcastLine.None(), // Wait for transition
                BroadcastState.BreakTransition => BroadcastLine.None(),   // Handled by GetNextDisplayLine
                BroadcastState.ReturnFromBreak => BroadcastLine.None(),   // Handled by GetNextDisplayLine
                BroadcastState.ShowEndingQueue => BroadcastLine.None(),   // Wait for transition
                BroadcastState.ShowEndingTransition => BroadcastLine.None(), // Handled by GetNextDisplayLine
                _ => BroadcastLine.None()
            };
        }

        public void OnLineCompleted()
        {
            _timingManager.StopLine();

            // Check if break transition just completed - start the break
            if (CurrentState == BroadcastState.BreakTransition)
            {
                GD.Print("BroadcastCoordinator: Break transition completed, notifying listeners");
                OnBreakTransitionCompleted?.Invoke();
                _stateManager.SetState(BroadcastState.AdBreak);
                _timingManager.StopLine();

                return; // Break started, don't advance normal flow
            }

            // Check if show ending transition just completed - end the show
            if (CurrentState == BroadcastState.ShowEndingTransition)
            {
                GD.Print("BroadcastCoordinator: Show ending transition completed, ending show");
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
                    GD.Print($"BroadcastCoordinator: Return-from-break music completed, queuing dialogue: {_pendingTransitionLine?.Text ?? "null"}");

                    // Notify display that next line is available
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

                    GD.Print("BroadcastCoordinator: Return-from-break sequence completed, transitioning to normal flow");
                    return; // Don't advance normal flow
                }
            }

            // Check if ad break line completed - progress to next ad
            if (CurrentState == BroadcastState.AdBreak && _adCoordinator.IsAdBreakActive)
            {
                _currentAdIndex++;
                GD.Print($"BroadcastCoordinator: Completed ad {_currentAdIndex - 1}, {_totalAdsInBreak - _currentAdIndex} remaining");
                return; // Stay in AdBreak state, next call to GetNextDisplayLine will get next ad
            }

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
                _stateManager.AdvanceState();
                if (CurrentState == BroadcastState.Conversation && _repository.OnAirCaller == null && _repository.HasOnHoldCallers)
                {
                    TryPutNextCallerOnAir();
                }
            }

            var advancedEvent = new ConversationAdvancedEvent(null);
            ServiceRegistry.Instance.EventBus.Publish(advancedEvent);
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





        private BroadcastLine GetAdBreakLine()
        {
            return _adCoordinator.GetAdBreakLine();
        }

        private BroadcastLine GetMusicLine()
        {
            return BroadcastLine.Music();
        }







        private BroadcastLine GetShowOpeningLine()
        {
            var line = _vernDialogue.GetShowOpening();
            return line != null ? BroadcastLine.ShowOpening(line.Text) : BroadcastLine.None();
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
                var broadcastLine = dialogueStep.CreateBroadcastLine();
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



        private BroadcastLine GetBetweenCallersLine()
        {
            var vernStats = ServiceRegistry.Instance.GameStateManager?.VernStats;
            var currentMood = vernStats?.CurrentMoodType ?? VernMoodType.Neutral;

            var line = _vernDialogue.GetBetweenCallers(currentMood);
            if (line != null)
            {
                return BroadcastLine.BetweenCallers(line.Text);
            }
            else
            {
                return BroadcastLine.None();
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
            var line = _vernDialogue.GetDeadAirFiller();
            return line != null ? BroadcastLine.DeadAirFiller(line.Text) : BroadcastLine.None();
        }

        private BroadcastLine GetOffTopicRemarkLine()
        {
            var vernStats = ServiceRegistry.Instance.GameStateManager?.VernStats;
            var currentMood = vernStats?.CurrentMoodType ?? VernMoodType.Neutral;

            var line = _vernDialogue.GetOffTopicRemark(currentMood);
            return line != null ? BroadcastLine.OffTopicRemark(line.Text) : BroadcastLine.None();
        }



        private BroadcastLine GetShowClosingLine()
        {
            var vernStats = ServiceRegistry.Instance.GameStateManager?.VernStats;
            VernMoodType mood = vernStats?.CurrentMoodType ?? VernMoodType.Neutral;

            var line = _vernDialogue.GetShowClosing(mood);
            var result = line != null ? BroadcastLine.ShowClosing(line.Text) : BroadcastLine.None();
            _stateManager.SetState(BroadcastState.Idle);  // State changes to Idle after line
            return result;
        }




    }
}
