#nullable enable

using System;
using Godot;
using KBTV.Callers;
using KBTV.Core;

namespace KBTV.Dialogue
{
    public partial class ConversationDisplay : Node, ICallerRepositoryObserver
    {
        private BroadcastCoordinator _coordinator = null!;
        private ICallerRepository _repository = null!;

        private ConversationDisplayInfo _displayInfo = new();
        private BroadcastLine _currentLine;
        private float _lineTimer = 0f;
        private float _lineDuration = 0f;
        private bool _isPlaying = false;
        private bool _waitingForCaller = false;

        public ConversationDisplayInfo DisplayInfo => _displayInfo;

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
            _coordinator = ServiceRegistry.Instance.BroadcastCoordinator;
            _repository = ServiceRegistry.Instance.CallerRepository;
            _repository.Subscribe(this);

            GD.Print("ConversationDisplay: Initialized");
        }

        public override void _Process(double delta)
        {
            float dt = (float)delta;

            if (_waitingForCaller)
            {
                return;
            }

            if (!_isPlaying)
            {
                TryGetNextLine();
                return;
            }

            _lineTimer += dt;

            if (_lineDuration > 0)
            {
                _displayInfo.Progress = Mathf.Min(1f, _lineTimer / _lineDuration);
                _displayInfo.ElapsedLineTime = _lineTimer;
            }

            if (_lineTimer >= _lineDuration)
            {
                EndCurrentLine();
            }
        }

        private void TryGetNextLine()
        {
            var line = _coordinator.GetNextLine();

            switch (line.Type)
            {
                case BroadcastLineType.PutCallerOnAir:
                    if (!_waitingForCaller)
                    {
                        _waitingForCaller = true;
                        var result = _repository.PutOnAir();
                        if (result.IsSuccess)
                        {
                            GD.Print("ConversationDisplay: Put caller on air");
                        }
                        else
                        {
                            _waitingForCaller = false;
                            GD.PrintErr($"ConversationDisplay: Failed to put caller on air: {result.ErrorCode}");
                        }
                    }
                    break;

                case BroadcastLineType.None:
                    _displayInfo = ConversationDisplayInfo.CreateIdle();
                    break;

                default:
                    StartLine(line);
                    break;
            }
        }

        private void StartLine(BroadcastLine line)
        {
            _currentLine = line;
            _lineTimer = 0f;
            _lineDuration = CalculateLineDuration(line.Text);
            _isPlaying = true;
            _waitingForCaller = false;

            _displayInfo = CreateDisplayInfo(line);
            _displayInfo.CurrentLineDuration = _lineDuration;

            GD.Print($"ConversationDisplay: Started line - {line.Speaker}");
        }

        private void EndCurrentLine()
        {
            _isPlaying = false;
            _displayInfo.Progress = 1f;
            _displayInfo.ElapsedLineTime = _lineDuration;

            _coordinator.OnLineCompleted();

            GD.Print($"ConversationDisplay: Ended line - {_currentLine.Speaker}");
        }

        private static ConversationDisplayInfo CreateDisplayInfo(BroadcastLine line)
        {
            return line.Type switch
            {
                BroadcastLineType.ShowOpening => ConversationDisplayInfo.CreateBroadcastLine(
                    line.Speaker, line.SpeakerId, line.Text, line.Phase, BroadcastFlowState.ShowOpening),
                BroadcastLineType.BetweenCallers => ConversationDisplayInfo.CreateBroadcastLine(
                    line.Speaker, line.SpeakerId, line.Text, line.Phase, BroadcastFlowState.BetweenCallers),
                BroadcastLineType.DeadAirFiller => ConversationDisplayInfo.CreateDeadAir(line.Text),
                BroadcastLineType.ShowClosing => ConversationDisplayInfo.CreateBroadcastLine(
                    line.Speaker, line.SpeakerId, line.Text, line.Phase, BroadcastFlowState.ShowClosing),
                _ => ConversationDisplayInfo.CreateBroadcastLine(
                    line.Speaker, line.SpeakerId, line.Text, line.Phase, BroadcastFlowState.Conversation)
            };
        }

        private static float CalculateLineDuration(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            return 4f;
        }

        public void OnCallerOnAir(Caller caller)
        {
            _waitingForCaller = false;
            _coordinator.OnCallerOnAir(caller);
            _isPlaying = false;
        }

        public void OnCallerOnAirEnded(Caller caller)
        {
            _waitingForCaller = false;
            _coordinator.OnCallerOnAirEnded(caller);
        }

        public void OnCallerAdded(Caller caller) { }
        public void OnCallerRemoved(Caller caller) { }
        public void OnCallerStateChanged(Caller caller, CallerState oldState, CallerState newState) { }
        public void OnScreeningStarted(Caller caller) { }
        public void OnScreeningEnded(Caller caller, bool approved) { }

        public override void _ExitTree()
        {
            _repository?.Unsubscribe(this);
        }
    }
}
