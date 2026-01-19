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
        private BroadcastState _nextStateAfterOffTopic;  // Tracks where to go after off-topic remark

        private enum BroadcastState
        {
            Idle,
            ShowOpening,
            Conversation,
            BetweenCallers,
            DeadAirFiller,
            OffTopicRemark,
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

            ServiceRegistry.Instance.EventBus.Publish(new ShowStartedEvent());
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
            _state = BroadcastState.Conversation;
            _lineInProgress = false;
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
                GD.Print($"[BroadcastCoordinator] Conversation ended, transitioning to BetweenCallers state ({_repository.OnHoldCallers.Count} callers waiting)");
                _state = BroadcastState.BetweenCallers;
            }
            else
            {
                GD.Print($"[BroadcastCoordinator] Conversation ended, no callers waiting, transitioning to DeadAirFiller state");
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
        }

        public void OnControlActionCompleted()
        {
            _pendingControlAction = ControlAction.None;
        }

        public BroadcastLine? GetNextDisplayLine()
        {
            if (!_broadcastActive && _state != BroadcastState.ShowClosing)
            {
                GD.Print($"[BroadcastCoordinator] GetNextDisplayLine: broadcast not active, state={_state}, returning null");
                return null;
            }

            if (_lineInProgress)
            {
                GD.Print($"[BroadcastCoordinator] GetNextDisplayLine: line in progress, returning current line type={_currentLine.Type}");
                return _currentLine;
            }

            if (_pendingControlAction != ControlAction.None)
            {
                GD.Print($"[BroadcastCoordinator] GetNextDisplayLine: pending control action={_pendingControlAction}, returning null");
                return null;
            }

            _currentLine = CalculateNextLine();
            GD.Print($"[BroadcastCoordinator] GetNextDisplayLine: state={_state}, calculated line type={_currentLine.Type}, text='{_currentLine.Text}'");

            if (_currentLine.Type == BroadcastLineType.None)
            {
                GD.Print($"[BroadcastCoordinator] GetNextDisplayLine: line type is None, returning null");
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
                BroadcastState.OffTopicRemark => GetOffTopicRemarkLine(),
                BroadcastState.ShowClosing => GetShowClosingLine(),
                _ => BroadcastLine.None()
            };
        }

        public void OnLineCompleted()
        {
            _lineInProgress = false;

            GD.Print($"[BroadcastCoordinator] OnLineCompleted called: state={_state}, _conversationContext={_conversationContext != null}");

            if (_stateMachine != null)
            {
                _stateMachine.ProcessEvent(ConversationEvent.LineCompleted());
            }

            // Check if conversation has reached its end after this line completed
            bool conversationJustEnded = false;

            if (_state == BroadcastState.Conversation &&
                _conversationContext != null &&
                _currentFlow != null &&
                _conversationContext.CurrentStepIndex >= _currentFlow.Steps.Count)
            {
                EndConversation();
                conversationJustEnded = true;
                GD.Print($"[BroadcastCoordinator] OnLineCompleted: conversation ended, state is now {_state}");
            }

            if (_state == BroadcastState.DeadAirFiller)
            {
                _fillerCycleCount++;
            }

            GD.Print($"[BroadcastCoordinator] OnLineCompleted: state={_state}, conversationJustEnded={conversationJustEnded}");
            if (!conversationJustEnded)
            {
                AdvanceState();
                GD.Print($"[BroadcastCoordinator] OnLineCompleted: after AdvanceState, state={_state}");
                if (_state == BroadcastState.Conversation && _repository.OnAirCaller == null && _repository.HasOnHoldCallers)
                {
                    GD.Print($"[BroadcastCoordinator] OnLineCompleted: putting next caller on air, { _repository.OnHoldCallers.Count} waiting");
                    TryPutNextCallerOnAir();
                    GD.Print($"[BroadcastCoordinator] OnLineCompleted: after TryPutNextCallerOnAir, OnAirCaller={_repository.OnAirCaller?.Name ?? "null"}");
                }
            }

            var advancedEvent = new ConversationAdvancedEvent(null);
            GD.Print($"[BroadcastCoordinator] OnLineCompleted: publishing ConversationAdvancedEvent, state={_state}");
            ServiceRegistry.Instance.EventBus.Publish(advancedEvent);
            GD.Print($"[BroadcastCoordinator] OnLineCompleted: event published");
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
                _state = BroadcastState.OffTopicRemark;
                if (_repository.HasOnHoldCallers)
                {
                    _nextStateAfterOffTopic = BroadcastState.BetweenCallers;
                }
                else
                {
                    _nextStateAfterOffTopic = BroadcastState.DeadAirFiller;
                    _fillerCycleCount = 0;
                }
            }
            else
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

            _lineInProgress = false;
        }

        private void TryPutNextCallerOnAir()
        {
            var result = _repository.PutOnAir();
            if (result.IsSuccess)
            {
                OnCallerPutOnAir(result.Value);
            }
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
                case BroadcastState.OffTopicRemark:
                    AdvanceFromOffTopicRemark();
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
                BroadcastLineType.Music => 5f,
                BroadcastLineType.ShowOpening => 5f,
                BroadcastLineType.VernDialogue => 4f,
                BroadcastLineType.CallerDialogue => 4f,
                BroadcastLineType.BetweenCallers => 4f,
                BroadcastLineType.DeadAirFiller => 8f,
                BroadcastLineType.ShowClosing => 5f,
                _ => 4f
            };
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
            var caller = _repository.OnAirCaller;
            if (caller == null)
            {
                if (_repository.HasOnHoldCallers)
                {
                    return BroadcastLine.None();
                }
                else
                {
                    _state = BroadcastState.DeadAirFiller;
                    _fillerCycleCount = 0;
                    return GetFillerLine();
                }
            }

            if (_stateMachine == null || _currentFlow == null || _conversationContext == null)
            {
                _state = BroadcastState.DeadAirFiller;
                return GetFillerLine();
            }

            if (!_stateMachine.CanProcessLines)
            {
                return BroadcastLine.None();
            }

            var step = _currentFlow.Steps[_conversationContext.CurrentStepIndex];

            if (!step.CanExecute(_conversationContext))
            {
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
            var vernStats = ServiceRegistry.Instance.GameStateManager?.VernStats;
            GD.Print($"[BroadcastCoordinator] VernStats available: {vernStats != null}");

            var currentMood = vernStats?.CurrentMoodType ?? VernMoodType.Neutral;
            GD.Print($"[BroadcastCoordinator] Getting between-callers line for mood: {currentMood} (int value: {(int)currentMood})");

            var line = _vernDialogue.GetBetweenCallers(currentMood);
            if (line != null)
            {
                GD.Print($"[BroadcastCoordinator] Selected between-callers line: '{line.Text}' (ID: {line.Id})");
                return BroadcastLine.BetweenCallers(line.Text);
            }
            else
            {
                GD.Print($"[BroadcastCoordinator] No between-callers line found for mood {currentMood}, returning None");
                return BroadcastLine.None();
            }
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

        private void AdvanceFromOffTopicRemark()
        {
            _state = _nextStateAfterOffTopic;
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

        private BroadcastLine GetOffTopicRemarkLine()
        {
            var vernStats = ServiceRegistry.Instance.GameStateManager?.VernStats;
            var currentMood = vernStats?.CurrentMoodType ?? VernMoodType.Neutral;

            var line = _vernDialogue.GetOffTopicRemark(currentMood);
            return line != null ? BroadcastLine.OffTopicRemark(line.Text) : BroadcastLine.None();
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

            if (line.Type == BroadcastLineType.Music)
            {
                _transcriptRepository?.AddEntry(TranscriptEntry.CreateMusicLine());
            }
            else if (line.Type == BroadcastLineType.VernDialogue ||
                line.Type == BroadcastLineType.DeadAirFiller ||
                line.Type == BroadcastLineType.BetweenCallers ||
                line.Type == BroadcastLineType.OffTopicRemark ||
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
                var mood = itemDict.ContainsKey("mood") ? itemDict["mood"].AsString() : "";

                return new DialogueTemplate(id, text, weight, mood);
            }).Where(x => x != null).ToArray()!;
        }
    }
}
