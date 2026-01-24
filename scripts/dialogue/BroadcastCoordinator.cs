#nullable enable

using System;
using Godot;
using KBTV.Callers;
using KBTV.Core;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Broadcast states for compatibility and new system.
    /// </summary>
    public enum BroadcastState
    {
        Idle,
        IntroMusic,
        ShowStarting,
        ShowOpening,
        Conversation,
        BetweenCallers,
        AdBreak,
        Break,
        BreakGracePeriod,
        BreakTransition,
        ReturnFromBreak,
        DeadAir,
        DeadAirFiller,
        OffTopicRemark,
        ShowClosing,
        ShowEnding,
        ShowEndingQueue,
        ShowEndingPending,
        ShowEndingTransition
    }

    /// <summary>
    /// Simplified broadcast coordinator using the new generic BroadcastEvent system.
    /// Replaces complex state management with clean event-driven flow.
    /// </summary>
    [GlobalClass]
    public partial class BroadcastCoordinator : Node, IBroadcastCoordinator
    {
    private ICallerRepository _repository = null!;
    private BroadcastStateMachine _stateMachine = null!;
    private BroadcastItemRegistry _itemRegistry = null!;
    private BroadcastItemExecutor _itemExecutor = null!;
    private AdBreakCoordinator _adBreakCoordinator = null!;
    private string? _currentItemId = null;
    private BroadcastItem? _currentItem = null;

        // Legacy interface compatibility - return Idle for now
        public BroadcastState CurrentState => BroadcastState.Idle;

        public override void _Ready()
        {
            InitializeWithServices();
        }

        private void InitializeWithServices()
        {
            _repository = ServiceRegistry.Instance.CallerRepository;
            _itemRegistry = new BroadcastItemRegistry();

            // Load Vern dialogue template
            var vernDialogueLoader = new VernDialogueLoader();
            vernDialogueLoader.LoadDialogue();
            _itemRegistry.SetVernDialogueTemplate(vernDialogueLoader.VernDialogue);

            // Initialize AdBreakCoordinator
            var adManager = ServiceRegistry.Instance.AdManager;
            _adBreakCoordinator = new AdBreakCoordinator(adManager, vernDialogueLoader.VernDialogue);

            _stateMachine = new BroadcastStateMachine(_repository, _itemRegistry);
            _itemExecutor = new BroadcastItemExecutor(itemId => {
                var completedEvent = new BroadcastEvent(BroadcastEventType.Completed, itemId, _currentItem);
                ServiceRegistry.Instance.EventBus.Publish(completedEvent);
            }, this, _adBreakCoordinator);

            var eventBus = ServiceRegistry.Instance.EventBus;
            eventBus.Subscribe<BroadcastEvent>(HandleBroadcastEvent);
            eventBus.Subscribe<BroadcastInterruptionEvent>(HandleBroadcastInterruption);

            // Subscribe to AdManager events for break interruptions
            var adManagerInstance = ServiceRegistry.Instance.AdManager;
            if (adManagerInstance != null)
            {
                adManagerInstance.OnBreakGracePeriod += OnBreakGracePeriod;
                adManagerInstance.OnBreakImminent += OnBreakImminent;
            }

            ServiceRegistry.Instance.RegisterSelf<BroadcastCoordinator>(this);
        }

        public void OnLiveShowStarted()
        {
            GD.Print("BroadcastCoordinator: Starting live show broadcast");
            var firstItem = _stateMachine.StartShow();

            if (firstItem != null)
            {
                ExecuteBroadcastItem(firstItem);
            }
            else
            {
                GD.PrintErr("BroadcastCoordinator: No initial broadcast item available");
            }
        }

        private void ExecuteBroadcastItem(BroadcastItem item)
        {
            GD.Print($"BroadcastCoordinator: Executing broadcast item - {item.Id} ({item.Type})");

            // Publish Started event
            var startedEvent = new BroadcastEvent(BroadcastEventType.Started, item.Id, item);
            ServiceRegistry.Instance.EventBus.Publish(startedEvent);

            // Track the currently executing item
            _currentItemId = item.Id;
            _currentItem = item;

            // Execute the item (play music, display text, etc.)
            _itemExecutor.ExecuteItem(item);
        }

        private void HandleBroadcastEvent(BroadcastEvent @event)
        {
            GD.Print($"BroadcastCoordinator: Handling broadcast event - {@event.Type} for {@event.ItemId}");

            if (@event.Type == BroadcastEventType.Completed || @event.Type == BroadcastEventType.Interrupted)
            {
                // Validate that the completed item ID matches the currently executing item
                if (_currentItemId != null && @event.ItemId != _currentItemId)
                {
                    GD.PrintErr($"BroadcastCoordinator: Ignoring {@event.Type} event for '{@event.ItemId}' - expected '{_currentItemId}'");
                    return;
                }

                // Validate that the completed item ID corresponds to a known broadcast item
                if (string.IsNullOrEmpty(@event.ItemId))
                {
                    GD.PrintErr("BroadcastCoordinator: Received completion event with null/empty ItemId");
                    return;
                }

                // Check if this looks like an audio file path (indicating cross-system contamination)
                if (@event.ItemId.Contains(".mp3") || @event.ItemId.Contains(".wav") || @event.ItemId.Contains("/") || @event.ItemId.Contains("\\"))
                {
                    GD.PrintErr($"BroadcastCoordinator: Ignoring completion event for audio file path '{@event.ItemId}' - likely cross-system contamination");
                    return;
                }

                var nextItem = _stateMachine.HandleEvent(@event);
                if (nextItem != null)
                {
                    ExecuteBroadcastItem(nextItem);
                }
                else
                {
                    GD.Print("BroadcastCoordinator: No next broadcast item, show may be ending");
                }

                // Check if this was a break transition completion
                if (@event.Item != null && @event.Item.Type == BroadcastItemType.Transition && 
                    @event.ItemId.StartsWith("break_transition"))
                {
                    GD.Print("BroadcastCoordinator: Break transition completed, notifying AdManager");
                    OnBreakTransitionCompleted?.Invoke();
                }

                // Clear current item tracking after processing
                _currentItemId = null;
                _currentItem = null;
            }
        }

        private void HandleBroadcastInterruption(BroadcastInterruptionEvent @event)
        {
            GD.Print($"BroadcastCoordinator: Handling broadcast interruption - {@event.Reason}");

            var nextItem = _stateMachine.HandleInterruption(@event);
            if (nextItem != null)
            {
                ExecuteBroadcastItem(nextItem);
            }

            // Clear current item tracking on interruption
            _currentItemId = null;
        }

        // Legacy interface methods for compatibility
        public void OnCallerPutOnAir(Caller caller)
        {
            GD.Print($"BroadcastCoordinator.OnCallerPutOnAir: {caller.Name} - transitioning to conversation state");
        }

        public void OnCallerOnAir(Caller caller)
        {
            OnCallerPutOnAir(caller);
        }

        public void OnCallerOnAirEnded(Caller caller)
        {
            GD.Print($"BroadcastCoordinator.OnCallerOnAirEnded: {caller.Name}");
        }

        // Legacy interface compatibility
        public void ResetFillerCycleCount() { }
        public BroadcastState GetNextStateAfterOffTopic() => BroadcastState.Idle;

        // Missing legacy properties/methods
        public string? CurrentAdSponsor => null;
        public bool IsOutroMusicQueued => false;
        public event Action? OnBreakTransitionCompleted;
        public event Action? OnTransitionLineAvailable;

        public BroadcastLine GetNextLine() => BroadcastLine.None(); // Legacy compatibility
        public void QueueShowEnd() { } // Legacy compatibility

        // AdManager event handlers
        private void OnBreakGracePeriod(float timeUntilBreak)
        {
            GD.Print($"BroadcastCoordinator: Break grace period started ({timeUntilBreak:F1}s until break)");
            var interruptionEvent = new BroadcastInterruptionEvent(BroadcastInterruptionReason.BreakStarting);
            HandleBroadcastInterruption(interruptionEvent);
        }

        private void OnBreakImminent(float timeUntilBreak)
        {
            GD.Print($"BroadcastCoordinator: Break imminent ({timeUntilBreak:F1}s until break)");
            // Don't trigger interruption if break is actually starting (timeUntilBreak <= 0)
            if (timeUntilBreak <= 0)
            {
                GD.Print("BroadcastCoordinator: Break is starting now, no interruption needed");
                return;
            }
            // For imminent warning, interrupt immediately with transition
            var interruptionEvent = new BroadcastInterruptionEvent(BroadcastInterruptionReason.BreakImminent);
            HandleBroadcastInterruption(interruptionEvent);
        }

        // IBroadcastCoordinator interface
        public void OnAdBreakStarted()
        {
            GD.Print("BroadcastCoordinator.OnAdBreakStarted: Starting ad break");
            // Initialize the AdBreakCoordinator for this break
            _adBreakCoordinator.OnAdBreakStarted();
            // Note: The actual interruption logic is now handled by the event-driven system
            // This method remains for interface compatibility
        }

        public void OnAdBreakEnded()
        {
            GD.Print("BroadcastCoordinator.OnAdBreakEnded: Ending ad break");
            // Notify the AdBreakCoordinator that the break has ended
            _adBreakCoordinator.OnAdBreakEnded();
            var interruptionEvent = new BroadcastInterruptionEvent(BroadcastInterruptionReason.BreakEnding);
            HandleBroadcastInterruption(interruptionEvent);
        }
    }
}