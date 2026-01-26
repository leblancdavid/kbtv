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

        public ConversationDisplayInfo DisplayInfo => _displayInfo;

        public override void _Ready()
        {
            base._Ready();
            GD.Print("ConversationDisplay: Initializing with services...");
            InitializeWithServices();
            GD.Print("ConversationDisplay: Initialization complete");
        }

        private void InitializeWithServices()
        {
            if (ServiceRegistry.IsInitialized)
            {
                _coordinator = ServiceRegistry.Instance.BroadcastCoordinator;
                _repository = ServiceRegistry.Instance.CallerRepository;
                _audioPlayer = ServiceRegistry.Instance.AudioPlayer;
                var eventBus = ServiceRegistry.Instance.EventBus;

                if (_coordinator != null && _repository != null && _audioPlayer != null && eventBus != null)
                {
                    _repository.Subscribe(this);
                    eventBus.Subscribe<BroadcastEvent>(HandleBroadcastEvent);
                    ServiceRegistry.Instance.RegisterSelf<ConversationDisplay>(this);
                }
                else
                {
                    CallDeferred(nameof(RetryInitialization));
                }
            }
            else
            {
                CallDeferred(nameof(RetryInitialization));
            }
        }

        private void RetryInitialization()
        {
            InitializeWithServices();
        }

        // ICallerRepositoryObserver implementation
        public void OnCallerAdded(Caller caller) { }
        public void OnCallerRemoved(Caller caller) { }
        public void OnCallerStateChanged(Caller caller, CallerState oldState, CallerState newState) { }
        public void OnScreeningStarted(Caller caller) { }
        public void OnScreeningEnded(Caller caller, bool approved) { }
        public void OnCallerOnAir(Caller caller) { }
        public void OnCallerOnAirEnded(Caller caller) { }

        private void HandleBroadcastEvent(BroadcastEvent @event)
        {
            GD.Print($"ConversationDisplay.HandleBroadcastEvent: {@event.Type} - {@event.ItemId}");

            if (@event.Type == BroadcastEventType.Started && @event.Item != null)
            {
                // Display the broadcast item
                DisplayBroadcastItem(@event.Item);
            }
            else if (@event.Type == BroadcastEventType.Completed)
            {
                // Item completed, coordinator will handle next item
                GD.Print($"ConversationDisplay: Broadcast item completed - {@event.ItemId}");
            }
        }

        private void DisplayBroadcastItem(BroadcastItem item)
        {
            GD.Print($"ConversationDisplay: Displaying broadcast item - {item.Id} ({item.Type})");

            // Update UI based on item type
            switch (item.Type)
            {
                case BroadcastItemType.Music:
                    // Music items might not need UI display, just play audio
                    if (!string.IsNullOrEmpty(item.AudioPath))
                    {
                        PlayAudio(item.AudioPath);
                    }
                    break;

                case BroadcastItemType.VernLine:
                case BroadcastItemType.CallerLine:
                case BroadcastItemType.DeadAir:
                    // Display text and play audio
                    DisplayText(item.Text);
                    if (!string.IsNullOrEmpty(item.AudioPath))
                    {
                        PlayAudio(item.AudioPath);
                    }
                    break;

                case BroadcastItemType.Ad:
                case BroadcastItemType.Transition:
                    // Display text, may have timer
                    DisplayText(item.Text);
                    if (item.Duration > 0)
                    {
                        StartTimer(item.Duration, item.Id);
                    }
                    break;

                default:
                    GD.PrintErr($"ConversationDisplay: Unknown broadcast item type {item.Type}");
                    break;
            }
        }

        private void DisplayText(string text)
        {
            GD.Print($"ConversationDisplay: Displaying text - {text}");
            // In real implementation, update UI labels
            _displayInfo = new ConversationDisplayInfo
            {
                Text = text,
                IsTyping = false,
                IsConversationActive = true
            };
        }

        private void PlayAudio(string audioPath)
        {
            GD.Print($"ConversationDisplay: Playing audio - {audioPath}");
            // In real implementation, load and play audio
        }

        private void StartTimer(float duration, string itemId)
        {
            GD.Print($"ConversationDisplay: Starting timer {duration}s for {itemId}");
            // In real implementation, create timer and publish completion event when done
        }



        public override void OnEvent(GameEvent gameEvent)
        {
            // Handle any additional game events if needed
        }

        // DomainMonitor abstract method
        protected override void OnUpdate(float deltaTime)
        {
            // Event-driven system - no polling needed
        }
    }
}