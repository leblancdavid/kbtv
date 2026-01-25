#nullable enable

using System;
using System.Threading.Tasks;
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
    /// Async broadcast coordinator using new async architecture.
    /// Replaces complex state management with clean async loop and event-driven flow.
    /// </summary>
    [GlobalClass]
    public partial class BroadcastCoordinator : Node, IBroadcastCoordinator
    {
    private AsyncBroadcastLoop _asyncLoop = null!;
    private ICallerRepository _repository = null!;
    private bool _isBroadcastActive = false;
    private bool _isOutroMusicQueued = false;
    private string _currentAdSponsor = "";

    // Legacy interface compatibility
    public BroadcastState CurrentState => GetLegacyState();
    
    // Missing properties for compatibility
    public bool IsOutroMusicQueued => _isOutroMusicQueued;
    public string CurrentAdSponsor => _currentAdSponsor;

    public event Action? OnBreakTransitionCompleted;

    public override void _Ready()
        {
            InitializeWithServices();
        }

        private void InitializeWithServices()
        {
            _repository = ServiceRegistry.Instance.CallerRepository;
            _asyncLoop = new AsyncBroadcastLoop();
            AddChild(_asyncLoop);

            // Subscribe to events
            var eventBus = ServiceRegistry.Instance.EventBus;
            eventBus.Subscribe<BroadcastTimingEvent>(HandleTimingEvent);
            eventBus.Subscribe<BroadcastEvent>(HandleBroadcastEvent);
            eventBus.Subscribe<BroadcastInterruptionEvent>(HandleBroadcastInterruption);

            // Subscribe to AdManager events for break interruptions
            var adManager = ServiceRegistry.Instance.AdManager;
            if (adManager != null)
            {
                adManager.OnBreakGracePeriod += OnBreakGracePeriod;
                adManager.OnBreakImminent += OnBreakImminent;
            }

            ServiceRegistry.Instance.RegisterSelf<BroadcastCoordinator>(this);
        }

    public void OnLiveShowStarted()
        {
            GD.Print("BroadcastCoordinator: Starting live show with async broadcast loop");
            
            _isBroadcastActive = true;
            
            // Get show duration from TimeManager
            var timeManager = ServiceRegistry.Instance.TimeManager;
            var showDuration = timeManager?.ShowDuration ?? 600.0f; // 10 minutes default
            
            // Start async broadcast loop
            _ = Task.Run(async () => {
                try
                {
                    await _asyncLoop.StartBroadcastAsync(showDuration);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"BroadcastCoordinator: Error in async broadcast: {ex.Message}");
                    _isBroadcastActive = false;
                }
            });
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
            if (!_isBroadcastActive || _asyncLoop == null)
                return BroadcastState.Idle;

            // Map AsyncBroadcastState to legacy BroadcastState
            var asyncState = _asyncLoop.IsInAdBreak() ? AsyncBroadcastState.AdBreak : AsyncBroadcastState.Conversation;
            
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
        /// Get the next broadcast line for display.
        /// Compatibility method for legacy UI components.
        /// </summary>
        public BroadcastLine GetNextLine()
        {
            // For now, return a placeholder line
            // In the future, this should integrate with the async loop
            if (_repository?.OnAirCaller != null)
            {
                return BroadcastLine.VernDialogue("Broadcast in progress...", ConversationPhase.Intro, null, 0, "vern");
            }
            return BroadcastLine.None();
        }

        /// <summary>
        /// Set the current ad sponsor for display.
        /// </summary>
        public void SetCurrentAdSponsor(string sponsor)
        {
            _currentAdSponsor = sponsor ?? "";
        }

        /// <summary>
        /// Handle caller going on air.
        /// </summary>
        public void OnCallerOnAir(Caller caller)
        {
            // This would be handled by the async loop in the future
            GD.Print($"BroadcastCoordinator: OnCallerOnAir called for {caller?.Name}");
        }

        /// <summary>
        /// Handle caller on air ending.
        /// </summary>
        public void OnCallerOnAirEnded(Caller caller)
        {
            // This would be handled by the async loop in the future  
            GD.Print($"BroadcastCoordinator: OnCallerOnAirEnded called for {caller?.Name}");
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