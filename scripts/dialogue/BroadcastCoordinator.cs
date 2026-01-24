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
            _stateMachine = new BroadcastStateMachine(_repository, _itemRegistry);
            _itemExecutor = new BroadcastItemExecutor(itemId => {
                var completedEvent = new BroadcastEvent(BroadcastEventType.Completed, itemId);
                ServiceRegistry.Instance.EventBus.Publish(completedEvent);
            }, this);

            var eventBus = ServiceRegistry.Instance.EventBus;
            eventBus.Subscribe<BroadcastEvent>(HandleBroadcastEvent);
            eventBus.Subscribe<BroadcastInterruptionEvent>(HandleBroadcastInterruption);

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

            // Execute the item (play music, display text, etc.)
            _itemExecutor.ExecuteItem(item);
        }

        private void HandleBroadcastEvent(BroadcastEvent @event)
        {
            GD.Print($"BroadcastCoordinator: Handling broadcast event - {@event.Type} for {@event.ItemId}");

            if (@event.Type == BroadcastEventType.Completed || @event.Type == BroadcastEventType.Interrupted)
            {
                var nextItem = _stateMachine.HandleEvent(@event);
                if (nextItem != null)
                {
                    ExecuteBroadcastItem(nextItem);
                }
                else
                {
                    GD.Print("BroadcastCoordinator: No next broadcast item, show may be ending");
                }
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

        // IBroadcastCoordinator interface
        public void OnAdBreakStarted()
        {
            GD.Print("BroadcastCoordinator.OnAdBreakStarted: Starting ad break");
            var interruptionEvent = new BroadcastInterruptionEvent(BroadcastInterruptionReason.BreakStarting);
            HandleBroadcastInterruption(interruptionEvent);
        }

        public void OnAdBreakEnded()
        {
            GD.Print("BroadcastCoordinator.OnAdBreakEnded: Ending ad break");
            var interruptionEvent = new BroadcastInterruptionEvent(BroadcastInterruptionReason.BreakEnding);
            HandleBroadcastInterruption(interruptionEvent);
        }
    }
}