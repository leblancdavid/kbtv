#nullable enable

using System;
using System.Linq;
using Godot;
using KBTV.Callers;
using KBTV.Core;

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
        private int _arcLineIndex = -1;
        private int _fillerCycleCount = 0;
        private bool _broadcastActive = false;

        private const int MaxFillerCyclesBeforeAutoAdvance = 1;

        private BroadcastLine _currentLine;
        private bool _lineInProgress = false;
        private float _lineStartTime = 0f;
        private float _lineDuration = 0f;

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
        }

        public void OnLiveShowEnding()
        {
            _state = BroadcastState.ShowClosing;
            _broadcastActive = false;
            _lineInProgress = false;
            _transcriptRepository?.ClearCurrentShow();
        }

        public void OnCallerOnAir(Caller caller)
        {
            _currentArc = caller.Arc;
            _arcLineIndex = 0;
            _state = BroadcastState.Conversation;
            _lineInProgress = false;
        }

        public void OnCallerOnAirEnded(Caller caller)
        {
            _currentArc = null;
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

        public BroadcastLine GetNextLine()
        {
            if (!_broadcastActive && _state != BroadcastState.ShowClosing)
            {
                return BroadcastLine.None();
            }

            if (_lineInProgress)
            {
                return _currentLine;
            }

            _currentLine = CalculateNextLine();

            if (_currentLine.Type == BroadcastLineType.PutCallerOnAir && _repository.HasOnHoldCallers)
            {
                var nextCaller = _repository.OnHoldCallers[0];
                _currentArc = nextCaller.Arc;
                _arcLineIndex = 0;
                _state = BroadcastState.Conversation;

                if (_currentArc != null && _currentArc.Dialogue.Count > 0)
                {
                    var arcLine = _currentArc.Dialogue[0];
                    _currentLine = arcLine.Speaker == Speaker.Vern
                        ? BroadcastLine.VernDialogue(arcLine.Text, ConversationPhase.Intro, _currentArc.ArcId)
                        : BroadcastLine.CallerDialogue(arcLine.Text, nextCaller.Name, nextCaller.Id, ConversationPhase.Probe, _currentArc.ArcId);
                }
            }

            _lineInProgress = true;

            var timeManager = ServiceRegistry.Instance.TimeManager;
            _lineStartTime = timeManager?.ElapsedTime ?? 0f;
            _lineDuration = GetLineDuration(_currentLine);

            AddTranscriptEntry(_currentLine);

            return _currentLine;
        }

        private BroadcastLine CalculateNextLine()
        {
            return _state switch
            {
                BroadcastState.ShowOpening => GetShowOpeningLine(calculateOnly: true),
                BroadcastState.Conversation => GetConversationLine(calculateOnly: true),
                BroadcastState.BetweenCallers => GetBetweenCallersLine(calculateOnly: true),
                BroadcastState.DeadAirFiller => GetFillerLine(calculateOnly: true),
                BroadcastState.ShowClosing => GetShowClosingLine(calculateOnly: true),
                _ => BroadcastLine.None()
            };
        }

        public void OnLineCompleted()
        {
            _lineInProgress = false;

            if (_currentArc != null)
            {
                _arcLineIndex++;
            }

            if (_state == BroadcastState.DeadAirFiller)
            {
                _fillerCycleCount++;
            }

            AdvanceState();
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

        private BroadcastLine GetShowOpeningLine(bool calculateOnly = false)
        {
            var line = _vernDialogue.GetShowOpening();
            var result = line != null ? BroadcastLine.ShowOpening(line.Text) : BroadcastLine.None();

            if (!calculateOnly)
            {
                AdvanceFromShowOpening();
            }

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

        private BroadcastLine GetConversationLine(bool calculateOnly = false)
        {
            var caller = _repository.OnAirCaller;
            if (caller == null)
            {
                if (_repository.HasOnHoldCallers)
                {
                    return BroadcastLine.PutCallerOnAir(ConversationPhase.Intro);
                }
                else
                {
                    _state = BroadcastState.DeadAirFiller;
                    _fillerCycleCount = 0;
                    return GetFillerLine(calculateOnly: true);
                }
            }

            if (_currentArc == null || _arcLineIndex < 0)
            {
                _currentArc = caller.Arc;
                _arcLineIndex = 0;
            }

            if (_currentArc == null || _arcLineIndex >= _currentArc.Dialogue.Count)
            {
                _state = BroadcastState.BetweenCallers;
                return BroadcastLine.PutCallerOnAir(ConversationPhase.Resolution);
            }

            var arcLine = _currentArc.Dialogue[_arcLineIndex];

            var line = arcLine.Speaker == Speaker.Vern
                ? BroadcastLine.VernDialogue(arcLine.Text, ConversationPhase.Intro, _currentArc.ArcId)
                : BroadcastLine.CallerDialogue(arcLine.Text, caller.Name, caller.Id, ConversationPhase.Probe, _currentArc.ArcId);

            return line;
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

        private BroadcastLine GetBetweenCallersLine(bool calculateOnly = false)
        {
            var line = _vernDialogue.GetBetweenCallers();
            var result = line != null ? BroadcastLine.BetweenCallers(line.Text) : BroadcastLine.None();

            if (!calculateOnly)
            {
                AdvanceFromBetweenCallers();
            }

            return result;
        }

        private void AdvanceFromBetweenCallers()
        {
            if (_repository.HasOnHoldCallers && _fillerCycleCount >= MaxFillerCyclesBeforeAutoAdvance)
            {
                _fillerCycleCount = 0;
                return;
            }

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

        private BroadcastLine GetFillerLine(bool calculateOnly = false)
        {
            if (_repository.HasOnHoldCallers && _fillerCycleCount >= MaxFillerCyclesBeforeAutoAdvance)
            {
                _fillerCycleCount = 0;
                return BroadcastLine.PutCallerOnAir(ConversationPhase.Intro);
            }

            var line = _vernDialogue.GetDeadAirFiller();
            var result = line != null ? BroadcastLine.DeadAirFiller(line.Text) : BroadcastLine.None();

            return result;
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

        private BroadcastLine GetShowClosingLine(bool calculateOnly = false)
        {
            var line = _vernDialogue.GetShowClosing();
            var result = line != null ? BroadcastLine.ShowClosing(line.Text) : BroadcastLine.None();

            if (!calculateOnly)
            {
                _state = BroadcastState.Idle;
            }

            return result;
        }

        private void AddTranscriptEntry(BroadcastLine line)
        {
            if (line.Type == BroadcastLineType.PutCallerOnAir || line.Type == BroadcastLineType.None)
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
                var caller = _repository.GetCaller(line.SpeakerId);
                if (caller != null)
                {
                    _transcriptRepository?.AddEntry(
                        TranscriptEntry.CreateCallerLine(caller, line.Text, line.Phase, line.ArcId)
                    );
                }
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
