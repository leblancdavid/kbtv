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

        private BroadcastState _state = BroadcastState.Idle;

        // Public accessor for current state (used by AdManager for coordination)
        public BroadcastState CurrentState => _state;
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
        private BroadcastState _nextStateAfterOffTopic;

        private bool _breakTransitionPending = false;
        private BroadcastLine? _pendingTransitionLine = null;

        // Ad break state tracking
        private int _currentAdIndex = 0;
        private int _totalAdsInBreak = 0;
        private bool _adBreakActive = false;
        public string? CurrentAdSponsor { get; private set; }

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
            DeadAirFiller,
            OffTopicRemark,
            ShowClosing
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
            _vernDialogue = LoadVernDialogue();

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
            _state = BroadcastState.IntroMusic;
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

        private void OnBreakGracePeriod(float timeUntilBreak)
        {
            if (_state == BroadcastState.AdBreak || _state == BroadcastState.BreakGracePeriod) return;

            _state = BroadcastState.BreakGracePeriod;
            _breakTransitionPending = true;
            GD.Print($"BroadcastCoordinator: Entering grace period, {timeUntilBreak:F1}s until break");
        }

        private void OnBreakImminent(float timeUntilBreak)
        {
            if (_state == BroadcastState.BreakGracePeriod && _lineInProgress)
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
            if (_lineInProgress)
            {
                GD.Print("BroadcastCoordinator: Stopping current line before transition");
                StopCurrentLine();
                OnLineCompleted(); // Process completion of interrupted line
            }

            _state = BroadcastState.BreakTransition;
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

        private void StopCurrentLine()
        {
            if (_lineInProgress)
            {
                // Stop any audio playback
                var dialoguePlayer = ServiceRegistry.Instance.EventBus as IDialoguePlayer;
                if (dialoguePlayer != null)
                {
                    dialoguePlayer.Stop();
                }

                _lineInProgress = false;
                _lineStartTime = 0f;
                _lineDuration = 0f;
            }
        }

        private BroadcastLine CreateBroadcastLine(DialogueTemplate template, Speaker speaker)
        {
            return BroadcastLine.VernDialogue(template.Text, ConversationPhase.Probe, template.Id);
        }

        public void OnAdBreakStarted()
        {
            if (_lineInProgress)
            {
                StopCurrentLine();
            }

            _state = BroadcastState.AdBreak;
            _lineInProgress = false;
            _currentAdIndex = 0;
            _totalAdsInBreak = ServiceRegistry.Instance.AdManager?.CurrentBreakSlots ?? 0;
            _adBreakActive = true;
            CurrentAdSponsor = null;

            GD.Print($"BroadcastCoordinator: Starting ad break with {_totalAdsInBreak} ads");
        }

        public void OnAdBreakEnded()
        {
            CurrentAdSponsor = null;

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
            GD.Print("BroadcastCoordinator: Exiting AdBreak state");
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

            // Priority: Check for pending transition line first
            if (_pendingTransitionLine != null)
            {
                var transitionLine = _pendingTransitionLine.Value;
                _pendingTransitionLine = null;

                // Set up transition line timing and state
                _currentLine = transitionLine;
                _lineInProgress = true;
                _lineStartTime = ServiceRegistry.Instance.TimeManager?.ElapsedTime ?? 0f;
                _lineDuration = GetLineDuration(transitionLine);

                // Add to transcript immediately
                AddTranscriptEntry(transitionLine);

                GD.Print($"BroadcastCoordinator: Delivering transition line to display: {transitionLine.Text}");
                return transitionLine;
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
                _ => BroadcastLine.None()
            };
        }

        public void OnLineCompleted()
        {
            _lineInProgress = false;

            // Check if break transition just completed - start the break
            if (_state == BroadcastState.BreakTransition)
            {
                GD.Print("BroadcastCoordinator: Break transition completed, notifying listeners");
                OnBreakTransitionCompleted?.Invoke();
                _state = BroadcastState.AdBreak;
                _lineInProgress = false;

                return; // Break started, don't advance normal flow
            }

            // Check if ad break line completed - progress to next ad
            if (_state == BroadcastState.AdBreak && _adBreakActive)
            {
                _currentAdIndex++;
                GD.Print($"BroadcastCoordinator: Completed ad {_currentAdIndex - 1}, {_totalAdsInBreak - _currentAdIndex} remaining");
                return; // Stay in AdBreak state, next call to GetNextDisplayLine will get next ad
            }

            // Check if we need to start break transition
            if (_state == BroadcastState.BreakGracePeriod && _breakTransitionPending)
            {
                StartBreakTransition();
                return; // Don't advance normal flow
            }

            if (_stateMachine != null)
            {
                _stateMachine.ProcessEvent(ConversationEvent.LineCompleted());
            }

            bool conversationJustEnded = false;

            if (_state == BroadcastState.Conversation &&
                _conversationContext != null &&
                _currentFlow != null &&
                _conversationContext.CurrentStepIndex >= _currentFlow.Steps.Count)
            {
                EndConversation();
                conversationJustEnded = true;
            }

            if (_state == BroadcastState.DeadAirFiller)
            {
                _fillerCycleCount++;
            }

            if (!conversationJustEnded)
            {
                AdvanceState();
                if (_state == BroadcastState.Conversation && _repository.OnAirCaller == null && _repository.HasOnHoldCallers)
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
                case BroadcastState.IntroMusic:
                    AdvanceFromIntroMusic();
                    break;
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
                BroadcastLineType.Music => 4f,
                BroadcastLineType.ShowOpening => 5f,
                BroadcastLineType.VernDialogue => 4f,
                BroadcastLineType.CallerDialogue => 4f,
                BroadcastLineType.BetweenCallers => 4f,
                BroadcastLineType.DeadAirFiller => 8f,
                BroadcastLineType.ShowClosing => 5f,
                BroadcastLineType.AdBreak => 2f, // Brief display for "AD BREAK" header
                BroadcastLineType.Ad => 4f,      // 4-second placeholder ads
                _ => 4f
            };
        }

        private BroadcastLine GetMusicLine()
        {
            return BroadcastLine.Music();
        }

        private BroadcastLine GetAdBreakMusicLine()
        {
            return BroadcastLine.Music();
        }

        private BroadcastLine GetAdBreakLine()
        {
            if (!_adBreakActive)
            {
                CurrentAdSponsor = null;
                return BroadcastLine.None();
            }

            if (_currentAdIndex < _totalAdsInBreak)
            {
                var adManager = ServiceRegistry.Instance.AdManager;
                var adType = DetermineAdType(adManager?.CurrentListeners ?? 100);
                var sponsorName = AdData.GetAdTypeDisplayName(adType);

                CurrentAdSponsor = sponsorName;

                _transcriptRepository?.AddEntry(new TranscriptEntry(
                    Speaker.System,
                    $"Ad sponsored by {sponsorName}",
                    ConversationPhase.Intro,
                    "system"
                ));

                string adText = _totalAdsInBreak > 1 ? $"AD BREAK ({_currentAdIndex + 1})" : "AD BREAK";
                return BroadcastLine.Ad(adText);
            }
            else
            {
                _adBreakActive = false;
                CurrentAdSponsor = null;
                ServiceRegistry.Instance.AdManager?.EndCurrentBreak();
                return BroadcastLine.None();
            }
        }

        private AdType DetermineAdType(int listenerCount)
        {
            // Simple tiered system based on listener count
            if (listenerCount >= 1000) return AdType.PremiumSponsor;
            if (listenerCount >= 500) return AdType.NationalSponsor;
            if (listenerCount >= 200) return AdType.RegionalBrand;
            return AdType.LocalBusiness;
        }

        private BroadcastLine GetRandomAdLine()
        {
            // For now, return placeholder ad line with custom text
            string adText = _totalAdsInBreak > 1 ? $"AD BREAK ({_currentAdIndex})" : "AD BREAK";
            return BroadcastLine.Ad(adText);
        }

        private BroadcastLine GetShowOpeningLine()
        {
            var line = _vernDialogue.GetShowOpening();
            return line != null ? BroadcastLine.ShowOpening(line.Text) : BroadcastLine.None();
        }

        private void AdvanceFromIntroMusic()
        {
            _state = BroadcastState.ShowOpening;
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
            else if (line.Type == BroadcastLineType.AdBreak || line.Type == BroadcastLineType.Ad)
            {
                _transcriptRepository?.AddEntry(
                    new TranscriptEntry(Speaker.System, line.Text, ConversationPhase.Intro, "system")
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
