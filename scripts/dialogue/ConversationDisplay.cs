#nullable enable

using System;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Monitors;

namespace KBTV.Dialogue
{
    public partial class ConversationDisplay : DomainMonitor, ICallerRepositoryObserver
    {
        private BroadcastCoordinator _coordinator = null!;
        private IDialoguePlayer _audioPlayer = null!;

        private ConversationDisplayInfo _displayInfo = new();
        private BroadcastLine _currentLine;

        public ConversationDisplayInfo DisplayInfo => _displayInfo;

        public override void _Ready()
        {
            base._Ready();

            if (!ServiceRegistry.IsInitialized)
            {
                CallDeferred(nameof(RetryInitialization));
                return;
            }

            InitializeWithServices();
        }

        public override void _ExitTree()
        {
            var eventBus = ServiceRegistry.Instance?.EventBus;
            if (eventBus != null)
            {
                eventBus.Unsubscribe<ShowStartedEvent>(HandleShowStarted);
                eventBus.Unsubscribe<ConversationStartedEvent>(HandleConversationStarted);
                eventBus.Unsubscribe<AudioCompletedEvent>(HandleAudioCompleted);
            }

            _repository?.Unsubscribe(this);

            base._ExitTree();
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

            _audioPlayer = ServiceRegistry.Instance.AudioPlayer;
            if (_audioPlayer != null)
            {
                _audioPlayer.LineCompleted += OnAudioLineCompleted;
            }

            var eventBus = ServiceRegistry.Instance.EventBus;
            eventBus.Subscribe<ShowStartedEvent>(HandleShowStarted);
            eventBus.Subscribe<ConversationStartedEvent>(HandleConversationStarted);
            eventBus.Subscribe<AudioCompletedEvent>(HandleAudioCompleted);
            eventBus.Subscribe<ConversationAdvancedEvent>(HandleConversationAdvanced);

            // Subscribe to transition line notifications
            if (_coordinator != null)
            {
                _coordinator.OnTransitionLineAvailable += OnTransitionLineAvailable;
            }
        }

        private void HandleShowStarted(ShowStartedEvent @event)
        {
            TryGetNextLine();
        }

        private void HandleConversationStarted(ConversationStartedEvent @event)
        {
            TryGetNextLine();
        }

        private void HandleAudioCompleted(AudioCompletedEvent @event)
        {
            _coordinator.OnLineCompleted();
        }

        private void OnTransitionLineAvailable()
        {
            TryGetNextLine();
        }

        private void HandleConversationAdvanced(ConversationAdvancedEvent @event)
        {
            TryGetNextLine();
        }

        public override void OnEvent(GameEvent gameEvent)
        {
            if (gameEvent is ShowStartedEvent showEvent)
            {
                HandleShowStarted(showEvent);
            }
            else if (gameEvent is ConversationStartedEvent startedEvent)
            {
                HandleConversationStarted(startedEvent);
            }
            else if (gameEvent is AudioCompletedEvent audioEvent)
            {
                HandleAudioCompleted(audioEvent);
            }
            else if (gameEvent is ConversationAdvancedEvent advancedEvent)
            {
                HandleConversationAdvanced(advancedEvent);
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            // Event-driven conversation system doesn't use timer updates
            // All progression is handled via events in OnEvent()
        }

        private void TryGetNextLine()
        {
            var line = _coordinator.GetNextDisplayLine();

            if (line == null)
            {
                _displayInfo = ConversationDisplayInfo.CreateIdle();
                return;
            }

            StartLine(line.Value);
        }

        private void StartLine(BroadcastLine line)
        {
            GD.Print($"ConversationDisplay: Starting line - Type: {line.Type}, Text: {line.Text}");

            // Check if this is an interrupting line that should stop current audio
            bool shouldInterrupt = IsInterruptingLine(line);

            if (shouldInterrupt && _audioPlayer != null && _audioPlayer.IsPlaying)
            {
                GD.Print("ConversationDisplay: Interrupting current audio for new line");
                _audioPlayer.Stop();

                // Fire completion event for the interrupted line to maintain state consistency
                OnAudioLineCompleted(new AudioCompletedEvent(_currentLine.SpeakerId,
                    _currentLine.Speaker == "Vern" ? Speaker.Vern : Speaker.Caller));
            }

            _currentLine = line;
            _displayInfo = CreateDisplayInfo(line);

            if (_audioPlayer != null)
            {
                GD.Print("ConversationDisplay: Playing line through audio player");
                _audioPlayer.PlayLineAsync(line);
            }
            else
            {
                GD.PrintErr("ConversationDisplay: No audio player available, skipping line");
                OnAudioLineCompleted(new AudioCompletedEvent(line.SpeakerId, line.Speaker == "Vern" ? Speaker.Vern : Speaker.Caller));
            }
        }

        private bool IsInterruptingLine(BroadcastLine line)
        {
            // For now, transition lines are interrupting
            // Future: Add more interruption conditions (emergency broadcasts, etc.)
            if (_coordinator == null) return false;

            return line.Type == BroadcastLineType.VernDialogue &&
                   _coordinator.CurrentState == BroadcastCoordinator.BroadcastState.BreakTransition;
        }

        private void OnAudioLineCompleted(AudioCompletedEvent audioEvent)
        {
            _displayInfo.Progress = 1f;
            _displayInfo.ElapsedLineTime = 0;

            // Only notify coordinator of completion if this wasn't an interruption
            // Interrupted lines don't advance the conversation state
            bool wasInterrupted = IsInterruptingLine(_currentLine);
            if (!wasInterrupted)
            {
                if (_coordinator != null)
                {
                    _coordinator.OnLineCompleted();
                }
                else
                {
                    GD.PrintErr("ConversationDisplay: _coordinator is null, cannot call OnLineCompleted");
                }
            }
            else
            {
                GD.Print("ConversationDisplay: Line was interrupted, not advancing conversation state");
            }
        }



        private static ConversationDisplayInfo CreateDisplayInfo(BroadcastLine line)
        {
            return line.Type switch
            {
                BroadcastLineType.Music => ConversationDisplayInfo.CreateMusic(line.Text),
                BroadcastLineType.ShowOpening => ConversationDisplayInfo.CreateBroadcastLine(
                    line.Speaker, line.SpeakerId, line.Text, line.Phase, BroadcastFlowState.ShowOpening),
                BroadcastLineType.BetweenCallers => ConversationDisplayInfo.CreateBroadcastLine(
                    line.Speaker, line.SpeakerId, line.Text, line.Phase, BroadcastFlowState.BetweenCallers),
                BroadcastLineType.DeadAirFiller => ConversationDisplayInfo.CreateDeadAir(line.Text),
                BroadcastLineType.ShowClosing => ConversationDisplayInfo.CreateBroadcastLine(
                    line.Speaker, line.SpeakerId, line.Text, line.Phase, BroadcastFlowState.ShowClosing),
                BroadcastLineType.AdBreak => ConversationDisplayInfo.CreateAdBreak(line.Text),
                BroadcastLineType.Ad => ConversationDisplayInfo.CreateAd(line.Text),
                _ => ConversationDisplayInfo.CreateBroadcastLine(
                    line.Speaker, line.SpeakerId, line.Text, line.Phase, BroadcastFlowState.Conversation)
            };
        }



        public void OnCallerOnAir(Caller caller)
        {
            _coordinator.OnCallerOnAir(caller);
        }

        public void OnCallerOnAirEnded(Caller caller)
        {
            _coordinator.OnCallerOnAirEnded(caller);
        }

        public void OnCallerAdded(Caller caller) { }
        public void OnCallerRemoved(Caller caller) { }
        public void OnCallerStateChanged(Caller caller, CallerState oldState, CallerState newState) { }
        public void OnScreeningStarted(Caller caller) { }
        public void OnScreeningEnded(Caller caller, bool approved) { }
    }
}
