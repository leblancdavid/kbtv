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
        private static int _instanceCount = 0;

        public ConversationDisplayInfo DisplayInfo => _displayInfo;

        public override void _Ready()
        {
            _instanceCount++;
            GD.Print($"ConversationDisplay #{_instanceCount} initialized: {GetPath()}, instance={GetInstanceId()}");

            base._Ready(); // Initialize DomainMonitor

            if (!ServiceRegistry.IsInitialized)
            {
                CallDeferred(nameof(RetryInitialization));
                return;
            }

            InitializeWithServices();
        }

        public override void _ExitTree()
        {
            // Unsubscribe from events to prevent memory leaks
            var eventBus = ServiceRegistry.Instance?.EventBus;
            if (eventBus != null)
            {
                eventBus.Unsubscribe<ConversationStartedEvent>(HandleConversationStarted);
                eventBus.Unsubscribe<AudioCompletedEvent>(HandleAudioCompleted);
            }

            // Unsubscribe from repository
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

            // Subscribe to event bus for conversation events
            var eventBus = ServiceRegistry.Instance.EventBus;
            eventBus.Subscribe<ConversationStartedEvent>(HandleConversationStarted);
            eventBus.Subscribe<AudioCompletedEvent>(HandleAudioCompleted);
            eventBus.Subscribe<ConversationAdvancedEvent>(HandleConversationAdvanced);
        }

        private void HandleConversationStarted(ConversationStartedEvent @event)
        {
            GD.Print("ConversationDisplay: Received ConversationStartedEvent, requesting first line");
            // Conversation started, get the first line
            TryGetNextLine();
        }

        private void HandleAudioCompleted(AudioCompletedEvent @event)
        {
            GD.Print($"ConversationDisplay: Received AudioCompletedEvent for {@event.LineId}, advancing conversation");
            // Audio completed, advance the conversation
            _coordinator.OnLineCompleted();
        }

        private void HandleConversationAdvanced(ConversationAdvancedEvent @event)
        {
            GD.Print("ConversationDisplay.HandleConversationAdvanced: Received ConversationAdvancedEvent, requesting next line");
            // Conversation advanced, get next line if available
            TryGetNextLine();
        }

        // Keep OnEvent for DomainMonitor compatibility, but delegate to handlers
        public override void OnEvent(GameEvent gameEvent)
        {
            GD.Print($"ConversationDisplay.OnEvent: Received {gameEvent.GetType().Name} from {gameEvent.Source}");

            if (gameEvent is ConversationStartedEvent startedEvent)
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
            else
            {
                GD.Print($"ConversationDisplay: Unhandled event type: {gameEvent.GetType().Name}");
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            // Event-driven conversation system doesn't use timer updates
            // All progression is handled via events in OnEvent()
        }

        private void TryGetNextLine()
        {
            GD.Print("ConversationDisplay.TryGetNextLine: Requesting next line from coordinator");
            var line = _coordinator.GetNextDisplayLine();

            if (line == null)
            {
                GD.Print("ConversationDisplay.TryGetNextLine: No line available, setting idle");
                _displayInfo = ConversationDisplayInfo.CreateIdle();
                return;
            }

            GD.Print($"ConversationDisplay.TryGetNextLine: Got line: {line.Value.Speaker} - {line.Value.Text}");
            StartLine(line.Value);
        }

        private async void StartLine(BroadcastLine line)
        {
            _currentLine = line;

            _displayInfo = CreateDisplayInfo(line);
            // Note: Duration will be determined by actual audio length

            GD.Print($"ConversationDisplay: Starting audio playback for line - {line.Speaker}: {line.Text}");

            if (_audioPlayer != null)
            {
                await _audioPlayer.PlayLineAsync(line);
            }
            else
            {
                GD.PrintErr("ConversationDisplay: No audio player available, skipping line");
                OnAudioLineCompleted(new AudioCompletedEvent(line.SpeakerId, line.Speaker == "Vern" ? Speaker.Vern : Speaker.Caller));
            }
        }

        private void OnAudioLineCompleted(AudioCompletedEvent audioEvent)
        {
            GD.Print($"ConversationDisplay: Audio completed for line {audioEvent.LineId}, advancing conversation");

            // Update display info to show completion
            _displayInfo.Progress = 1f;
            _displayInfo.ElapsedLineTime = 0; // Audio duration not tracked yet

            // Advance the conversation
            _coordinator.OnLineCompleted();
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



        public void OnCallerOnAir(Caller caller)
        {
            GD.Print($"ConversationDisplay: Caller going on air: {caller.Name}");
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
