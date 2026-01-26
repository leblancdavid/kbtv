#nullable enable

using System;
using System.Threading.Tasks;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Ads;
using KBTV.Managers;

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
    /// Async broadcast coordinator using new async architecture.
    /// Replaces complex state management with clean async loop and event-driven flow.
    /// Converted to AutoInject Provider pattern.
    /// </summary>
    public partial class BroadcastCoordinator : Node, IBroadcastCoordinator,
        IProvide<BroadcastCoordinator>,
        IDependent
    {
        public override void _Notification(int what) => this.Notify(what);

        private AsyncBroadcastLoop AsyncBroadcastLoop => DependencyInjection.Get<AsyncBroadcastLoop>(this);

        private ICallerRepository CallerRepository => DependencyInjection.Get<ICallerRepository>(this);

        private AdManager AdManager => DependencyInjection.Get<AdManager>(this);

        private TimeManager TimeManager => DependencyInjection.Get<TimeManager>(this);

        private EventBus EventBus => DependencyInjection.Get<EventBus>(this);

        private bool _isBroadcastActive = false;
        private bool _isOutroMusicQueued = false;
        private string _currentAdSponsor = "";
        private readonly object _lock = new object();

        // Missing field - AsyncBroadcastLoop dependency
        private AsyncBroadcastLoop _asyncLoop;

        // Legacy interface compatibility
        public BroadcastState CurrentState => GetLegacyState();

        // Missing properties for compatibility
        public bool IsOutroMusicQueued => _isOutroMusicQueued;
        public string CurrentAdSponsor => _currentAdSponsor;

        // Provider interface implementation
        BroadcastCoordinator IProvide<BroadcastCoordinator>.Value() => this;

        public event Action? OnBreakTransitionCompleted;

        /// <summary>
        /// Called when all dependencies are resolved.
        /// </summary>
        public void OnResolved()
        {
            GD.Print("BroadcastCoordinator: Dependencies resolved, initializing...");

            // Initialize field from dependency property
            _asyncLoop = AsyncBroadcastLoop;

            // Subscribe to AdManager events for break interruptions
            AdManager.OnBreakGracePeriod += OnBreakGracePeriod;
            AdManager.OnBreakImminent += OnBreakImminent;

            // Subscribe to events
            EventBus.Subscribe<BroadcastTimingEvent>(HandleTimingEvent);
            EventBus.Subscribe<BroadcastEvent>(HandleBroadcastEvent);
            EventBus.Subscribe<BroadcastInterruptionEvent>(HandleBroadcastInterruption);

            GD.Print("BroadcastCoordinator: Initialization complete");
        }

        /// <summary>
        /// Called when node enters the scene tree and is ready.
        /// </summary>
        public void OnReady()
        {
            GD.Print("BroadcastCoordinator: Ready, providing service to descendants");
            this.Provide();
        }

    public void OnLiveShowStarted()
        {
            lock (_lock)
            {
                if (_isBroadcastActive)
                {
                    GD.Print("BroadcastCoordinator: Live show already active, ignoring start request");
                    return;
                }

                GD.Print("BroadcastCoordinator: Starting live show with async broadcast loop");
                
                _isBroadcastActive = true;
                
                // Get show duration from TimeManager
                var showDuration = TimeManager.ShowDuration;
                
                // Start async broadcast loop
                _ = Task.Run(async () => {
                    try
                    {
                        await _asyncLoop.StartBroadcastAsync(showDuration);
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"BroadcastCoordinator: Error in async broadcast: {ex.Message}");
                        lock (_lock)
                        {
                            _isBroadcastActive = false;
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Handle timing events from BroadcastTimer.
        /// </summary>
        private void HandleTimingEvent(BroadcastTimingEvent timingEvent)
        {
            GD.Print($"BroadcastCoordinator: Handling timing event - {timingEvent.Type}");

            switch (timingEvent.Type)
            {
                case BroadcastTimingEventType.ShowEnd:
                    HandleShowEnding();
                    break;
                case BroadcastTimingEventType.AdBreakStart:
                    OnAdBreakStarted();
                    break;
                case BroadcastTimingEventType.AdBreakEnd:
                    OnAdBreakEnded();
                    break;
            }
        }

        /// <summary>
        /// Handle broadcast events from async loop.
        /// </summary>
        private void HandleBroadcastEvent(BroadcastEvent broadcastEvent)
        {
            GD.Print($"BroadcastCoordinator: Handling broadcast event - {broadcastEvent.Type} for {broadcastEvent.ItemId}");

            // Legacy: Notify AdManager of transitions for break handling
            if (broadcastEvent.Item?.Type == BroadcastItemType.Transition && 
                broadcastEvent.ItemId.StartsWith("break_transition"))
            {
                OnBreakTransitionCompleted?.Invoke();
            }
        }

        /// <summary>
        /// Handle broadcast interruption events.
        /// </summary>
        private void HandleBroadcastInterruption(BroadcastInterruptionEvent interruptionEvent)
        {
            GD.Print($"BroadcastCoordinator: Handling interruption - {interruptionEvent.Reason}");

            switch (interruptionEvent.Reason)
            {
                case BroadcastInterruptionReason.BreakStarting:
                    // Break interruption handled by timing events
                    break;
                case BroadcastInterruptionReason.ShowEnding:
                    _isBroadcastActive = false;
                    break;
            }
        }

        /// <summary>
        /// Legacy interface implementation for AdManager integration.
        /// </summary>
        public void OnAdBreakStarted()
        {
            GD.Print("BroadcastCoordinator: Ad break started (legacy interface)");
            // Handled by async loop via timing events
        }

        /// <summary>
        /// Legacy interface implementation for AdManager integration.
        /// </summary>
        public void OnAdBreakEnded()
        {
            GD.Print("BroadcastCoordinator: Ad break ended (legacy interface)");
            // Handled by async loop via timing events
        }

        /// <summary>
        /// Handle show ending from TimeManager.
        /// </summary>
        private void HandleShowEnding()
        {
            GD.Print("BroadcastCoordinator: Show ending triggered");
            _asyncLoop.InterruptBroadcast(BroadcastInterruptionReason.ShowEnding);
        }

        /// <summary>
        /// Handle break grace period from AdManager.
        /// </summary>
        private void OnBreakGracePeriod(float secondsRemaining)
        {
            GD.Print($"BroadcastCoordinator: Break grace period - {secondsRemaining}s remaining");
            // Async loop handles this via timing events
        }

        /// <summary>
        /// Handle break imminent from AdManager.
        /// </summary>
        private void OnBreakImminent(float secondsRemaining)
        {
            GD.Print($"BroadcastCoordinator: Break imminent - {secondsRemaining}s remaining");
            // Schedule break in async loop
            _asyncLoop.ScheduleBreak(secondsRemaining);
        }

        /// <summary>
        /// Map async state to legacy BroadcastState.
        /// </summary>
        private BroadcastState GetLegacyState()
        {
            if (!_isBroadcastActive)
                return BroadcastState.Idle;

            // Map AsyncBroadcastState to legacy BroadcastState
            var asyncState = AsyncBroadcastLoop.IsInAdBreak() ? AsyncBroadcastState.AdBreak : AsyncBroadcastState.Conversation;

            return asyncState switch
            {
                AsyncBroadcastState.Idle => BroadcastState.Idle,
                AsyncBroadcastState.ShowStarting => BroadcastState.ShowStarting,
                AsyncBroadcastState.IntroMusic => BroadcastState.IntroMusic,
                AsyncBroadcastState.ShowOpening => BroadcastState.ShowOpening,
                AsyncBroadcastState.Conversation => BroadcastState.Conversation,
                AsyncBroadcastState.BetweenCallers => BroadcastState.BetweenCallers,
                AsyncBroadcastState.AdBreak => BroadcastState.AdBreak,
                AsyncBroadcastState.DeadAir => BroadcastState.DeadAirFiller,
                AsyncBroadcastState.ShowClosing => BroadcastState.ShowClosing,
                AsyncBroadcastState.ShowEnding => BroadcastState.ShowEnding,
                _ => BroadcastState.Idle
            };
        }

        /// <summary>
        /// Queue the show end for outro music playback.
        /// </summary>
        public void QueueShowEnd()
        {
            GD.Print("BroadcastCoordinator: QueueShowEnd called - setting outro music queued flag");
            _isOutroMusicQueued = true;
        }

        

        /// <summary>
        /// Set the current ad sponsor for display.
        /// </summary>
        public void SetCurrentAdSponsor(string sponsor)
        {
            _currentAdSponsor = sponsor ?? "";
        }

        

        /// <summary>
        /// Clean up resources.
        /// </summary>
        public override void _ExitTree()
        {
            _asyncLoop?.StopBroadcast();
            base._ExitTree();
        }
    }
}