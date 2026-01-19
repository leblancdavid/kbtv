#nullable enable

using System;
using System.Linq;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Data;

namespace KBTV.Dialogue
{
    [GlobalClass]
    public partial class BroadcastCoordinator : Node
    {
        private ICallerRepository _repository = null!;
        private ITranscriptRepository _transcriptRepository = null!;
        private IArcRepository _arcRepository = null!;
        private VernDialogueTemplate _vernDialogue = null!;

        private BroadcastState _state = BroadcastState.Idle;
        private ConversationArc? _currentArc;
        private System.Collections.Generic.List<ArcDialogueLine>? _resolvedDialogue;
        private ConversationFlow? _currentFlow;
        private ConversationStateMachine? _stateMachine;
        private ConversationContext? _conversationContext;
        private int _arcLineIndex = -1;
        private int _fillerCycleCount = 0;
        private bool _broadcastActive = false;

        private const int MaxFillerCyclesBeforeAutoAdvance = 1;

        private BroadcastLine _currentLine;
        private bool _lineInProgress = false;
        private float _lineStartTime = 0f;
        private float _lineDuration = 0f;

        private ControlAction _pendingControlAction = ControlAction.None;

        private enum BroadcastState
        {
            Idle,
            ShowOpening,
            Conversation,
            BetweenCallers,
            DeadAirFiller,
            ShowClosing
        }

        public override void _Ready()
        {
            if (!ServiceRegistry.IsInitialized)
            {
                CallDeferred(nameof(RetryInitialization));
                return;
            }

            InitializeWithServices();
        }

        private void RetryInitialization()
        {
            if (ServiceRegistry.IsInitialized)
            {
                InitializeWithServices();
            }
            else
            {
                CallDeferred(nameof(RetryInitialization));
            }
        }

        private void InitializeWithServices()
        {
            _repository = ServiceRegistry.Instance.CallerRepository;
            _transcriptRepository = ServiceRegistry.Instance.TranscriptRepository;
            _arcRepository = ServiceRegistry.Instance.ArcRepository;
            _vernDialogue = LoadVernDialogue();

            ServiceRegistry.Instance.RegisterSelf<BroadcastCoordinator>(this);
        }

        public override void _Process(double delta)
        {
            if (!_lineInProgress)
            {
                return;
            }

            var timeManager = ServiceRegistry.Instance.TimeManager;
            if (timeManager == null)
            {
                return;
            }

            float elapsed = timeManager.ElapsedTime - _lineStartTime;
            if (elapsed >= _lineDuration)
            {
                OnLineCompleted();
            }
        }

        public void OnLiveShowStarted()
        {
            _broadcastActive = true;
            _transcriptRepository?.StartNewShow();
            _state = BroadcastState.ShowOpening;
            _fillerCycleCount = 0;
            _lineInProgress = false;
            _pendingControlAction = ControlAction.None;
        }

        public void OnLiveShowEnding()
        {
            _state = BroadcastState.ShowClosing;
            _broadcastActive = false;
            _lineInProgress = false;
            _pendingControlAction = ControlAction.None;
            _transcriptRepository?.ClearCurrentShow();
        }

        public void OnCallerPutOnAir(Caller caller)
        {
            GD.Print($"BroadcastCoordinator: Starting conversation for caller: {caller.Name}");

            _currentArc = caller.Arc;
            var vernStats = ServiceRegistry.Instance.GameStateManager?.VernStats;
            VernMoodType mood = VernMoodType.Neutral;
            if (vernStats != null)
            {
                mood = vernStats.CurrentMoodType;
                _resolvedDialogue = _currentArc.GetDialogueForMood(mood);
                GD.Print($"BroadcastCoordinator: Resolved dialogue for mood {mood}, {(_resolvedDialogue?.Count ?? 0)} lines");
            }
            else
            {
                _resolvedDialogue = new System.Collections.Generic.List<ArcDialogueLine>(_currentArc.Dialogue);
                GD.Print("BroadcastCoordinator: No VernStats available, using raw dialogue");
            }

            // Initialize functional conversation system
            _currentFlow = ConversationFlow.CreateLinear(_resolvedDialogue);
            GD.Print($"BroadcastCoordinator: Created conversation flow with {_currentFlow.Steps.Count} steps");

            _stateMachine = new ConversationStateMachine();
            _conversationContext = new ConversationContext(mood);

            _stateMachine.ProcessEvent(ConversationEvent.Start());
            GD.Print($"BroadcastCoordinator: State machine started in state: {_stateMachine.CurrentState}");

            _arcLineIndex = 0;
            _state = BroadcastState.Conversation;
            _lineInProgress = false;
            _pendingControlAction = ControlAction.None;

            GD.Print("BroadcastCoordinator: Conversation initialization complete");

            // Auto-start conversation by publishing event
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
                _state = BroadcastState.BetweenCallers;
            }
            else
            {
                _state = BroadcastState.DeadAirFiller;
                _fillerCycleCount = 0;
            }

            _lineInProgress = false;
        }

        public ControlAction GetPendingControlAction()
        {
            if (!_broadcastActive && _state != BroadcastState.ShowClosing)
            {
                return ControlAction.None;
            }

            if (_pendingControlAction != ControlAction.None)
            {
                return _pendingControlAction;
            }

            if (_lineInProgress)
            {
                return ControlAction.None;
            }

            CheckForControlAction();
            return _pendingControlAction;
        }

        private void CheckForControlAction()
        {
            if (_state == BroadcastState.Conversation && _repository.OnAirCaller == null && _repository.HasOnHoldCallers)
            {
                _pendingControlAction = ControlAction.PutCallerOnAir;
            }
            else if (_state == BroadcastState.DeadAirFiller && _fillerCycleCount >= MaxFillerCyclesBeforeAutoAdvance && _repository.HasOnHoldCallers)
            {
                _pendingControlAction = ControlAction.PutCallerOnAir;
                _fillerCycleCount = 0;
            }
        }

        public void OnControlActionCompleted()
        {
            _pendingControlAction = ControlAction.None;
        }

        public BroadcastLine? GetNextDisplayLine()
        {
            if (!_broadcastActive && _state != BroadcastState.ShowClosing)
            {
                return null;
            }

            if (_lineInProgress)
            {
                return _currentLine;
            }

            if (_pendingControlAction != ControlAction.None)
            {
                return null;
            }

            _currentLine = CalculateNextLine();

            if (_currentLine.Type == BroadcastLineType.None)
            {
                return null;
            }

            _lineInProgress = true;

            var timeManager = ServiceRegistry.Instance.TimeManager;
            _lineStartTime = timeManager?.ElapsedTime ?? 0f;
            _lineDuration = GetLineDuration(_currentLine);

            AddTranscriptEntry(_currentLine);

            return _currentLine;
        }

        public BroadcastLine GetNextLine()
        {
            var displayLine = GetNextDisplayLine();
            return displayLine ?? BroadcastLine.None();
        }

        private BroadcastLine CalculateNextLine()
        {
            return _state switch
            {
                BroadcastState.ShowOpening => GetShowOpeningLine(),
                BroadcastState.Conversation => GetConversationLine(),
                BroadcastState.BetweenCallers => GetBetweenCallersLine(),
                BroadcastState.DeadAirFiller => GetFillerLine(),
                BroadcastState.ShowClosing => GetShowClosingLine(),
                _ => BroadcastLine.None()
            };
        }

        public void OnLineCompleted()
        {
            GD.Print($"BroadcastCoordinator.OnLineCompleted called");
            _lineInProgress = false;

            // Update state machine
            if (_stateMachine != null)
            {
                _stateMachine.ProcessEvent(ConversationEvent.LineCompleted());
                GD.Print($"BroadcastCoordinator: State machine transitioned to {_stateMachine.CurrentState}");
            }

            // Publish event for conversation advancement
            var advancedEvent = new ConversationAdvancedEvent(null);
            ServiceRegistry.Instance.EventBus.Publish(advancedEvent);

            if (_state == BroadcastState.DeadAirFiller)
            {
                _fillerCycleCount++;
            }

            AdvanceState();
        }

        private void EndConversation()
        {
            GD.Print("BroadcastCoordinator: Ending conversation");
            _stateMachine?.ProcessEvent(ConversationEvent.End());

            var currentCaller = _repository.OnAirCaller;
            if (currentCaller != null)
            {
                _repository.EndOnAir();
                OnCallerOnAirEnded(currentCaller);
            }

            // Reset conversation state
            _currentFlow = null;
            _stateMachine = null;
            _conversationContext = null;
            _arcLineIndex = -1;
        }

        private void AdvanceState()
        {
            switch (_state)
            {
                case BroadcastState.ShowOpening:
                    AdvanceFromShowOpening();
                    break;
                case BroadcastState.Conversation:
                    AdvanceFromConversation();
                    break;
                case BroadcastState.BetweenCallers:
                    AdvanceFromBetweenCallers();
                    break;
                case BroadcastState.DeadAirFiller:
                    AdvanceFromFiller();
                    break;
                case BroadcastState.ShowClosing:
                    _state = BroadcastState.Idle;
                    break;
            }
        }

        private static float GetLineDuration(BroadcastLine line)
        {
            return line.Type switch
            {
                BroadcastLineType.ShowOpening => 5f,
                BroadcastLineType.VernDialogue => 4f,
                BroadcastLineType.CallerDialogue => 4f,
                BroadcastLineType.BetweenCallers => 4f,
                BroadcastLineType.DeadAirFiller => 8f,
                BroadcastLineType.ShowClosing => 5f,
                _ => 0f
            };
        }

        private BroadcastLine GetShowOpeningLine()
        {
            var line = _vernDialogue.GetShowOpening();
            var result = line != null ? BroadcastLine.ShowOpening(line.Text) : BroadcastLine.None();
            AdvanceFromShowOpening();
            return result;
        }

        private void AdvanceFromShowOpening()
        {
            if (_repository.HasOnHoldCallers)
            {
                _state = BroadcastState.Conversation;
            }
            else
            {
                _state = BroadcastState.DeadAirFiller;
                _fillerCycleCount = 0;
            }
        }

        private BroadcastLine GetConversationLine()
        {
            GD.Print("BroadcastCoordinator.GetConversationLine: Called");

            var caller = _repository.OnAirCaller;
            if (caller == null)
            {
                GD.Print("BroadcastCoordinator.GetConversationLine: No caller on air");
                if (_repository.HasOnHoldCallers)
                {
                    GD.Print("BroadcastCoordinator.GetConversationLine: Has on-hold callers, returning None");
                    return BroadcastLine.None();  // Allow control action polling instead of filler loop

                }
                else
                {
                    _state = BroadcastState.DeadAirFiller;
                    _fillerCycleCount = 0;
                    return GetFillerLine();
                }
            }

            // Use functional conversation system
            GD.Print("BroadcastCoordinator.GetConversationLine: Using functional conversation system");

            if (_stateMachine == null || _currentFlow == null || _conversationContext == null)
            {
                GD.Print("BroadcastCoordinator.GetConversationLine: Conversation system not initialized");
                _state = BroadcastState.DeadAirFiller;
                return GetFillerLine();
            }

            GD.Print($"BroadcastCoordinator.GetConversationLine: StateMachine state: {_stateMachine.CurrentState}, CanProcessLines: {_stateMachine.CanProcessLines}");

            if (!_stateMachine.CanProcessLines)
            {
                GD.Print($"BroadcastCoordinator.GetConversationLine: Cannot process lines in state {_stateMachine.CurrentState}");
                return BroadcastLine.None();
            }

            // Get next step from conversation flow
            GD.Print($"BroadcastCoordinator.GetConversationLine: CurrentStepIndex: {_conversationContext.CurrentStepIndex}, TotalSteps: {_currentFlow.Steps.Count}");

            if (_conversationContext.CurrentStepIndex >= _currentFlow.Steps.Count)
            {
                GD.Print("BroadcastCoordinator.GetConversationLine: Conversation flow completed");
                EndConversation();
                return BroadcastLine.None();
            }

            var step = _currentFlow.Steps[_conversationContext.CurrentStepIndex];
            GD.Print($"BroadcastCoordinator.GetConversationLine: Processing step {_conversationContext.CurrentStepIndex} of type {step.Type}");

            if (!step.CanExecute(_conversationContext))
            {
                GD.Print($"BroadcastCoordinator.GetConversationLine: Step {_conversationContext.CurrentStepIndex} cannot execute");
                return BroadcastLine.None();
            }

            // Process the step
            if (step is DialogueStep dialogueStep)
            {
                var broadcastLine = dialogueStep.CreateBroadcastLine();
                GD.Print($"BroadcastCoordinator.GetConversationLine: Created broadcast line: {broadcastLine.Speaker} - {broadcastLine.Text}");

                _stateMachine.ProcessEvent(ConversationEvent.LineAvailable(broadcastLine));
                GD.Print($"BroadcastCoordinator.GetConversationLine: State machine processed LineAvailable, new state: {_stateMachine.CurrentState}");

                _conversationContext.CurrentStepIndex++;
                GD.Print($"BroadcastCoordinator.GetConversationLine: Returning dialogue line: {broadcastLine.Speaker} - {broadcastLine.Text}");
                return broadcastLine;
            }
            else
            {
                // Skip non-dialogue steps
                GD.Print($"BroadcastCoordinator.GetConversationLine: Skipping non-dialogue step {step.Type}");
                _conversationContext.CurrentStepIndex++;
                return GetConversationLine(); // Recurse
            }
        }

        private void AdvanceFromConversation()
        {
            if (_repository.OnAirCaller == null)
            {
                if (_repository.HasOnHoldCallers)
                {
                    _state = BroadcastState.BetweenCallers;
                }
                else
                {
                    _state = BroadcastState.DeadAirFiller;
                    _fillerCycleCount = 0;
                }
            }
        }

        private BroadcastLine GetBetweenCallersLine()
        {
            var line = _vernDialogue.GetBetweenCallers();
            var result = line != null ? BroadcastLine.BetweenCallers(line.Text) : BroadcastLine.None();
            AdvanceFromBetweenCallers();
            return result;
        }

        private void AdvanceFromBetweenCallers()
        {
            if (_repository.HasOnHoldCallers)
            {
                _state = BroadcastState.Conversation;
            }
            else
            {
                _state = BroadcastState.DeadAirFiller;
                _fillerCycleCount = 0;
            }
        }

        private BroadcastLine GetFillerLine()
        {
            if (_repository.HasOnHoldCallers && _fillerCycleCount >= MaxFillerCyclesBeforeAutoAdvance)
            {
                _fillerCycleCount = 0;
                return GetFillerLineText();
            }

            return GetFillerLineText();
        }

        private BroadcastLine GetFillerLineText()
        {
            var line = _vernDialogue.GetDeadAirFiller();
            return line != null ? BroadcastLine.DeadAirFiller(line.Text) : BroadcastLine.None();
        }

        private void AdvanceFromFiller()
        {
            if (_repository.HasOnHoldCallers)
            {
                _state = BroadcastState.Conversation;
            }
            else
            {
                _fillerCycleCount = 0;
            }
        }

        private BroadcastLine GetShowClosingLine()
        {
            var line = _vernDialogue.GetShowClosing();
            var result = line != null ? BroadcastLine.ShowClosing(line.Text) : BroadcastLine.None();
            _state = BroadcastState.Idle;
            return result;
        }

        private void AddTranscriptEntry(BroadcastLine line)
        {
            if (line.Type == BroadcastLineType.None)
            {
                return;
            }

            if (line.Type == BroadcastLineType.VernDialogue || 
                line.Type == BroadcastLineType.DeadAirFiller ||
                line.Type == BroadcastLineType.BetweenCallers ||
                line.Type == BroadcastLineType.ShowOpening ||
                line.Type == BroadcastLineType.ShowClosing)
            {
                _transcriptRepository?.AddEntry(
                    TranscriptEntry.CreateVernLine(line.Text, line.Phase, line.ArcId)
                );
            }
            else if (line.Type == BroadcastLineType.CallerDialogue)
            {
                _transcriptRepository?.AddEntry(
                    new TranscriptEntry(Speaker.Caller, line.Text, line.Phase, line.ArcId, line.Speaker)
                );
                GD.Print($"BroadcastCoordinator: Added caller transcript entry for {line.Speaker}: {line.Text}");
            }
            else if (line.Type == BroadcastLineType.VernDialogue)
            {
                _transcriptRepository?.AddEntry(
                    new TranscriptEntry(Speaker.Vern, line.Text, line.Phase, line.ArcId, "Vern")
                );
                GD.Print($"BroadcastCoordinator: Added Vern transcript entry: {line.Text}");
            }
        }

        private static VernDialogueTemplate LoadVernDialogue()
        {
            var jsonPath = "res://assets/dialogue/vern/VernDialogue.json";
            var file = Godot.FileAccess.FileExists(jsonPath) ? Godot.FileAccess.Open(jsonPath, Godot.FileAccess.ModeFlags.Read) : null;
            if (file == null)
            {
                return new VernDialogueTemplate();
            }

            try
            {
                var jsonText = file.GetAsText();
                file.Close();

                var json = new Json();
                var error = json.Parse(jsonText);
                if (error != Error.Ok)
                {
                    return new VernDialogueTemplate();
                }

                var result = new VernDialogueTemplate();
                var data = json.Data.As<Godot.Collections.Dictionary>();
                if (data == null)
                {
                    return result;
                }

                result.SetShowOpeningLines(ParseDialogueArray(data, "showOpeningLines"));
                result.SetIntroductionLines(ParseDialogueArray(data, "introductionLines"));
                result.SetShowClosingLines(ParseDialogueArray(data, "showClosingLines"));
                result.SetBetweenCallersLines(ParseDialogueArray(data, "betweenCallersLines"));
                result.SetDeadAirFillerLines(ParseDialogueArray(data, "deadAirFillerLines"));
                result.SetDroppedCallerLines(ParseDialogueArray(data, "droppedCallerLines"));
                result.SetBreakTransitionLines(ParseDialogueArray(data, "breakTransitionLines"));
                result.SetOffTopicRemarkLines(ParseDialogueArray(data, "offTopicRemarkLines"));

                return result;
            }
            catch
            {
                return new VernDialogueTemplate();
            }
        }

        private static DialogueTemplate[] ParseDialogueArray(Godot.Collections.Dictionary data, string key)
        {
            if (!data.ContainsKey(key))
            {
                return Array.Empty<DialogueTemplate>();
            }

            var array = data[key].As<Godot.Collections.Array>();
            if (array == null || array.Count == 0)
            {
                return Array.Empty<DialogueTemplate>();
            }

            return array.Select(item => {
                var itemDict = item.As<Godot.Collections.Dictionary>();
                if (itemDict == null) return null;

                var id = itemDict.ContainsKey("id") ? itemDict["id"].AsString() : "";
                var text = itemDict.ContainsKey("text") ? itemDict["text"].AsString() : "";
                var weight = itemDict.ContainsKey("weight") ? itemDict["weight"].AsSingle() : 1f;

                return new DialogueTemplate(id, text, weight);
            }).Where(x => x != null).ToArray()!;
        }
    }
}
