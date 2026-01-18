#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using KBTV.Callers;
using KBTV.Core;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Orchestrates the broadcast flow during LiveShow phase.
    /// Manages show opening, conversations, transitions, dead air filler, and show closing.
    /// </summary>
    [GlobalClass]
    public partial class ConversationManager : Node, IConversationManager
    {
        private ICallerRepository _repository = null!;
        private VernDialogueTemplate _vernDialogue = null!;

        private ConversationDisplayInfo _displayInfo = new();
        private BroadcastFlowState _state = BroadcastFlowState.Idle;
        private bool _broadcastActive = false;

        private DialogueTemplate? _currentFillerLine;

        private float _lineTimer = 0f;
        private float _lineDuration = 0f;

        private float _deadAirTimer = 0f;
        private const float DeadAirCycleInterval = 8f;
        private const float BaseDelay = 1.5f;
        private const float PerCharDelay = 0.04f;

        public ConversationDisplayInfo DisplayInfo => _displayInfo;
        public BroadcastFlowState CurrentState => _state;
        public bool IsConversationActive => _state == BroadcastFlowState.Conversation;
        public bool IsBroadcastActive => _broadcastActive;

        public override void _Ready()
        {
            if (!ServiceRegistry.IsInitialized)
            {
                GD.PrintErr("ConversationManager: ServiceRegistry not initialized, retrying...");
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
            _vernDialogue = LoadVernDialogue();

            ServiceRegistry.Instance.RegisterSelf<IConversationManager>(this);
            ServiceRegistry.Instance.RegisterSelf<ConversationManager>(this);

            GD.Print("ConversationManager: Initialized");
        }

        private static VernDialogueTemplate LoadVernDialogue()
        {
            var jsonPath = "res://assets/dialogue/vern/VernDialogue.json";
            var file = Godot.FileAccess.FileExists(jsonPath) ? Godot.FileAccess.Open(jsonPath, Godot.FileAccess.ModeFlags.Read) : null;
            if (file == null)
            {
                GD.PrintErr($"ConversationManager: Could not load dialogue file at {jsonPath}");
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
                    GD.PrintErr($"ConversationManager: Failed to parse JSON: {error}");
                    return new VernDialogueTemplate();
                }

                var result = new VernDialogueTemplate();
                var data = json.Data.As<Godot.Collections.Dictionary>();
                if (data == null)
                {
                    GD.PrintErr("ConversationManager: Invalid JSON structure");
                    return result;
                }

                result.SetShowOpeningLines(ParseDialogueArray(data, "showOpeningLines"));
                result.SetShowClosingLines(ParseDialogueArray(data, "showClosingLines"));
                result.SetBetweenCallersLines(ParseDialogueArray(data, "betweenCallersLines"));
                result.SetDeadAirFillerLines(ParseDialogueArray(data, "deadAirFillerLines"));
                result.SetDroppedCallerLines(ParseDialogueArray(data, "droppedCallerLines"));
                result.SetBreakTransitionLines(ParseDialogueArray(data, "breakTransitionLines"));
                result.SetOffTopicRemarkLines(ParseDialogueArray(data, "offTopicRemarkLines"));

                return result;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"ConversationManager: Error loading dialogue: {ex.Message}");
                return new VernDialogueTemplate();
            }
        }

        private static DialogueTemplate[] ParseDialogueArray(Godot.Collections.Dictionary data, string key)
        {
            if (!data.ContainsKey(key))
            {
                GD.PrintErr($"ConversationManager: Missing key '{key}' in dialogue JSON");
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

                var id = itemDict.GetValueOrDefault("id", "").AsString();
                var text = itemDict.GetValueOrDefault("text", "").AsString();
                var weight = itemDict.GetValueOrDefault("weight", 1f).AsSingle();

                return new DialogueTemplate(id, text, weight);
            }).Where(x => x != null).ToArray()!;
        }

        public override void _Process(double delta)
        {
            float dt = (float)delta;

            switch (_state)
            {
                case BroadcastFlowState.DeadAirFiller:
                    UpdateDeadAirFiller(dt);
                    break;
            }
        }

        public void OnLiveShowStarted()
        {
            if (_broadcastActive)
            {
                GD.Print("ConversationManager: Broadcast already active");
                return;
            }

            _broadcastActive = true;
            PlayShowOpening();
            GD.Print("ConversationManager: LiveShow started");
        }

        public void OnLiveShowEnding()
        {
            if (!_broadcastActive)
            {
                return;
            }

            StopDeadAirFiller();
            PlayShowClosing();
            _broadcastActive = false;
            GD.Print("ConversationManager: LiveShow ending");
        }

        public void PutCallerOnAir(Caller caller)
        {
            if (_broadcastActive && _state == BroadcastFlowState.DeadAirFiller)
            {
                StopDeadAirFiller();
            }
        }

        public void EndCurrentConversation()
        {
            if (_state == BroadcastFlowState.DeadAirFiller)
            {
                StopDeadAirFiller();
            }

            if (_repository.HasOnHoldCallers)
            {
                PlayBetweenCallers();
            }
            else
            {
                StartDeadAirFiller();
            }
        }

        public void StartDeadAirFiller()
        {
            if (_state == BroadcastFlowState.DeadAirFiller)
            {
                return;
            }

            _state = BroadcastFlowState.DeadAirFiller;
            _deadAirTimer = 0f;

            var line = _vernDialogue.GetDeadAirFiller();
            if (line != null)
            {
                _currentFillerLine = line;
                _lineTimer = 0f;
                _lineDuration = CalculateLineDuration(line.Text);

                _displayInfo = ConversationDisplayInfo.CreateDeadAir(line.Text);
                _displayInfo.CurrentLineDuration = _lineDuration;
            }

            GD.Print("ConversationManager: Started dead air filler");
        }

        public void StopDeadAirFiller()
        {
            if (_state != BroadcastFlowState.DeadAirFiller)
            {
                return;
            }

            _state = BroadcastFlowState.Idle;
            _currentFillerLine = null;
            _displayInfo = ConversationDisplayInfo.CreateIdle();

            GD.Print("ConversationManager: Stopped dead air filler");
        }

        public void CheckAutoAdvance()
        {
            if (!_broadcastActive)
            {
                return;
            }

            if (_state == BroadcastFlowState.Idle && _repository.HasOnHoldCallers)
            {
                var result = _repository.PutOnAir();
                if (result.IsSuccess && result.Value != null)
                {
                    GD.Print("ConversationManager: Auto-advanced to on-hold caller");
                }
            }
        }

        private void PlayShowOpening()
        {
            _state = BroadcastFlowState.ShowOpening;

            var line = _vernDialogue.GetShowOpening();
            if (line != null)
            {
                _lineTimer = 0f;
                _lineDuration = CalculateLineDuration(line.Text);

                _displayInfo = ConversationDisplayInfo.CreateBroadcastLine("Vern", "VERN", line.Text, ConversationPhase.Intro);
                _displayInfo.CurrentLineDuration = _lineDuration;
            }
        }

        private void PlayBetweenCallers()
        {
            _state = BroadcastFlowState.BetweenCallers;

            var line = _vernDialogue.GetBetweenCallers();
            if (line != null)
            {
                _lineTimer = 0f;
                _lineDuration = CalculateLineDuration(line.Text);

                _displayInfo = ConversationDisplayInfo.CreateBroadcastLine("Vern", "VERN", line.Text, ConversationPhase.Resolution);
                _displayInfo.CurrentLineDuration = _lineDuration;
            }
        }

        private void PlayShowClosing()
        {
            _state = BroadcastFlowState.ShowClosing;

            var line = _vernDialogue.GetShowClosing();
            if (line != null)
            {
                _lineTimer = 0f;
                _lineDuration = CalculateLineDuration(line.Text);

                _displayInfo = ConversationDisplayInfo.CreateBroadcastLine("Vern", "VERN", line.Text, ConversationPhase.Resolution);
                _displayInfo.CurrentLineDuration = _lineDuration;
            }
        }

        private void UpdateDeadAirFiller(float dt)
        {
            _lineTimer += dt;
            _deadAirTimer += dt;

            if (_currentFillerLine != null)
            {
                _displayInfo.Progress = _lineDuration > 0 ? _lineTimer / _lineDuration : 0f;
                _displayInfo.ElapsedLineTime = _lineTimer;
            }

            if (_lineTimer >= _lineDuration)
            {
                if (_repository.HasOnHoldCallers)
                {
                    StopDeadAirFiller();
                    var result = _repository.PutOnAir();
                    if (result.IsSuccess)
                    {
                        GD.Print("ConversationManager: Auto-advanced from filler to caller");
                    }
                }
                else if (_deadAirTimer >= DeadAirCycleInterval)
                {
                    PlayNextFillerLine();
                }
            }
        }

        private void PlayNextFillerLine()
        {
            var line = _vernDialogue.GetDeadAirFiller();
            if (line != null)
            {
                _currentFillerLine = line;
                _lineTimer = 0f;
                _lineDuration = CalculateLineDuration(line.Text);
                _deadAirTimer = 0f;

                _displayInfo = ConversationDisplayInfo.CreateDeadAir(line.Text);
                _displayInfo.CurrentLineDuration = _lineDuration;
            }
        }

        private float CalculateLineDuration(string text)
        {
            return BaseDelay + (text.Length * PerCharDelay);
        }

        public void OnCallerOnAir(Caller caller)
        {
            if (_state == BroadcastFlowState.DeadAirFiller)
            {
                StopDeadAirFiller();
            }
        }

        public void OnCallerOnAirEnded(Caller caller)
        {
            EndCurrentConversation();
        }

        public void OnCallerAdded(Caller caller) { }
        public void OnCallerRemoved(Caller caller) { }
        public void OnCallerStateChanged(Caller caller, CallerState oldState, CallerState newState) { }
        public void OnScreeningStarted(Caller caller) { }
        public void OnScreeningEnded(Caller caller, bool approved) { }
    }
}
