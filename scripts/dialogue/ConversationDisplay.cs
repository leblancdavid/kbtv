#nullable enable

using System;
using Godot;
using KBTV.Audio;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Monitors;

namespace KBTV.Dialogue
{
    public partial class ConversationDisplay : DomainMonitor, ICallerRepositoryObserver, IDependent
    {
        private BroadcastCoordinator _coordinator = null!;
        private IBroadcastAudioService _audioPlayer = null!;

        public override void _Notification(int what) => this.Notify(what);

        private ConversationDisplayInfo _displayInfo = new();

        public ConversationDisplayInfo DisplayInfo => _displayInfo;

        public override void _Ready()
        {
            GD.Print("ConversationDisplay: Ready, waiting for dependencies...");
            // Dependencies resolved in OnResolved(), don't call base._Ready()
        }

        /// <summary>
        /// Called when all dependencies are resolved.
        /// </summary>
        public override void OnResolved()
        {
            GD.Print("ConversationDisplay: Dependencies resolved, initializing...");

            // Get dependencies via DI
            _coordinator = DependencyInjection.Get<BroadcastCoordinator>(this);
            _repository = DependencyInjection.Get<ICallerRepository>(this);
            _audioPlayer = DependencyInjection.Get<IBroadcastAudioService>(this);
            var eventBus = DependencyInjection.Get<EventBus>(this);

            // Subscribe to events
            _repository.Subscribe(this);
            eventBus.Subscribe<BroadcastEvent>(HandleBroadcastEvent);

            GD.Print("ConversationDisplay: Initialization complete");
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