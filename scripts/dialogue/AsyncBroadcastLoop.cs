#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using KBTV.Core;
using KBTV.Callers;
using KBTV.Managers;
using KBTV.Audio;
using KBTV.Ads;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Main background coordinator running async loop.
    /// Requests executables → executing → awaiting → repeat.
    /// Handles cancellation tokens for interruptions.
    /// Publishes events for UI updates.
    /// Converted to AutoInject Provider pattern.
    /// </summary>
    public partial class AsyncBroadcastLoop : Node,
        IProvide<AsyncBroadcastLoop>,
        IDependent
    {
        public override void _Notification(int what) => this.Notify(what);

        private ICallerRepository CallerRepository => DependencyInjection.Get<ICallerRepository>(this);

        private IArcRepository ArcRepository => DependencyInjection.Get<IArcRepository>(this);

        private EventBus EventBus => DependencyInjection.Get<EventBus>(this);

        private ListenerManager ListenerManager => DependencyInjection.Get<ListenerManager>(this);

        private BroadcastStateManager _stateManager => DependencyInjection.Get<BroadcastStateManager>(this);
        private AdManager AdManager => DependencyInjection.Get<AdManager>(this);
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly List<CancellationTokenSource> _oldTokenSources = new();
        private Task? _broadcastTask;
        private bool _isRunning = false;
        private BroadcastExecutable? _currentExecutable;
        private readonly object _lock = new object();
        private BroadcastInterruptionReason _lastInterruptionReason;
        private bool _isOutroMusicQueued = false;
        private string _currentAdSponsor = "";
        private readonly List<CancellationTokenSource> _tokenPool = new();
        private readonly object _tokenPoolLock = new();
        private readonly List<GameEvent> _eventBatch = new();
        private bool _eventBatchTimerActive = false;

        public bool IsRunning => _isRunning;
        public BroadcastExecutable? CurrentExecutable => _currentExecutable;
        public AsyncBroadcastState CurrentState => _stateManager.CurrentState;
        public bool IsOutroMusicQueued => _isOutroMusicQueued;
        public string CurrentAdSponsor => _currentAdSponsor;

        // Provider interface implementation
        AsyncBroadcastLoop IProvide<AsyncBroadcastLoop>.Value() => this;

        public BroadcastStateManager StateManager => _stateManager;

        /// <summary>
        /// Called when all dependencies are resolved.
        /// </summary>
        public void OnResolved()
        {
            KBTV.Core.Logger.Debug("AsyncBroadcastLoop: Dependencies resolved, initializing...");

            try
            {
                // Subscribe to broadcast timing events for break warnings
                EventBus.Subscribe<BroadcastTimingEvent>(HandleBroadcastTimingEvent);

                // Subscribe to state changed events to synchronize break ending
                EventBus.Subscribe<BroadcastStateChangedEvent>(HandleStateChangedEvent);

                // Subscribe to AdManager events for break interruptions
                AdManager.OnBreakGracePeriod += OnBreakGracePeriod;
                AdManager.OnBreakImminent += OnBreakImminent;

                KBTV.Core.Logger.Debug("AsyncBroadcastLoop: Initialization complete");
            }
            catch (Exception ex)
            {

            }
        }
        private void PublishImmediate(GameEvent @event)
        {
            EventBus.Publish(@event);
        }

        /// <summary>
        /// Add event to batch for delayed publishing (for performance optimization).
        /// </summary>
        private void PublishBatched(GameEvent @event)
        {
            lock (_eventBatch)
            {
                _eventBatch.Add(@event);
                
                if (!_eventBatchTimerActive)
                {
                    _eventBatchTimerActive = true;
                    // Schedule batch publishing on next frame
                    CallDeferred(nameof(PublishBatched));
                }
            }
        }

        /// <summary>
        /// Get a CancellationTokenSource from the pool or create new one.
        /// </summary>
        private CancellationTokenSource GetTokenSource()
        {
            lock (_tokenPoolLock)
            {
                if (_tokenPool.Count > 0)
                {
                    var token = _tokenPool[_tokenPool.Count - 1];
                    _tokenPool.RemoveAt(_tokenPool.Count - 1);
                    return token;
                }
            }
            return new CancellationTokenSource();
        }

        /// <summary>
        /// Return a CancellationTokenSource to the pool for reuse.
        /// </summary>
        private void ReturnTokenSource(CancellationTokenSource tokenSource)
        {
            if (tokenSource == null) return;

            // Reset the token source if possible
            try
            {
                if (!tokenSource.IsCancellationRequested)
                {
                    // Can't reset a non-cancelled token, dispose and create new
                    tokenSource.Dispose();
                    return;
                }
                // Token was cancelled, dispose it
                tokenSource.Dispose();
            }
            catch
            {
                // Dispose failed, just ignore
            }
        }

        /// <summary>
        /// Called when node enters the scene tree and is ready.
        /// </summary>
        public void OnReady()
        {
            KBTV.Core.Logger.Debug("AsyncBroadcastLoop: Ready, providing service to descendants");
            this.Provide();
        }

        /// <summary>
        /// Start the broadcast loop.
        /// </summary>
        public async Task StartBroadcastAsync(float showDuration = 600.0f)
        {
            lock (_lock)
            {
                if (_isRunning)
                {
                    KBTV.Core.Logger.Error("AsyncBroadcastLoop: Broadcast is already running");
                    return;
                }

                // Cancel and store old token for later disposal (don't dispose immediately)
                var oldToken = _cancellationTokenSource;
                _cancellationTokenSource = new CancellationTokenSource();
                
                // Store old token for later disposal
                if (oldToken != null)
                {
                    _oldTokenSources.Add(oldToken);
                    oldToken.Cancel();
                }
            }

            try
            {
                _isRunning = true;
                EventBus.Publish(new BroadcastTimerCommand(BroadcastTimerCommandType.StartShow, showDuration));

                // Get the first executable and start the loop
                var firstExecutable = _stateManager.StartShow();
                if (firstExecutable != null)
                {
                    await ExecuteExecutableAsync(firstExecutable, _cancellationTokenSource.Token);
                    // Update state after first executable
                    _stateManager.UpdateStateAfterExecution(firstExecutable);
                    await RunBroadcastLoopAsync(_cancellationTokenSource.Token);
                }
                else
                {
                    KBTV.Core.Logger.Error("AsyncBroadcastLoop: No initial executable available");
                    lock (_lock)
                    {
                        _isRunning = false;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                KBTV.Core.Logger.Debug("AsyncBroadcastLoop: Broadcast cancelled");
            }
            catch (Exception ex)
            {
                KBTV.Core.Logger.Error($"AsyncBroadcastLoop: Error in broadcast: {ex.Message}");
            }
            finally
            {
                lock (_lock)
                {
                    StopBroadcastUnsafe();
                }
            }
        }

        /// <summary>
        /// Main broadcast execution loop.
        /// </summary>
        private async Task RunBroadcastLoopAsync(CancellationToken cancellationToken)
        {
            KBTV.Core.Logger.Debug("AsyncBroadcastLoop: Starting main broadcast loop");

            while (!_cancellationTokenSource.Token.IsCancellationRequested && _stateManager.IsShowActive)
            {
                try
                {
                    KBTV.Core.Logger.Debug($"AsyncBroadcastLoop: Loop check - token cancelled: {_cancellationTokenSource.Token.IsCancellationRequested}, show active: {_stateManager.IsShowActive}");
                    
                    // Get next executable from state manager
                    var nextExecutable = _stateManager.GetNextExecutable();
                    
                    if (nextExecutable == null)
                    {
                        // No executable available - wait a bit and try again
                        await _stateManager.DelayAsync(1.0f, _cancellationTokenSource.Token);
                        continue;
                    }

                    // Execute the executable
                    await ExecuteExecutableAsync(nextExecutable, _cancellationTokenSource.Token);

                    // Update state manager after execution
                    _stateManager.UpdateStateAfterExecution(nextExecutable);

                    // Small delay between executables
                    await _stateManager.DelayAsync(0.1f, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // Check if this is a break interruption - continue the loop with fresh token
                    if (_lastInterruptionReason == BroadcastInterruptionReason.BreakImminent || 
                        _lastInterruptionReason == BroadcastInterruptionReason.BreakStarting)
                    {
                        KBTV.Core.Logger.Debug($"AsyncBroadcastLoop: Break interruption detected ({_lastInterruptionReason}) - resetting token and continuing");
                        
                        // Clean up old token
                        if (_cancellationTokenSource != null)
                        {
                            _oldTokenSources.Add(_cancellationTokenSource);
                            _cancellationTokenSource.Dispose();
                        }
                        
                        // Create fresh token for continued execution
                        _cancellationTokenSource = new CancellationTokenSource();
                        
                        KBTV.Core.Logger.Debug("AsyncBroadcastLoop: Fresh token created, continuing loop");
                        continue;
                    }
                    else if (_lastInterruptionReason == BroadcastInterruptionReason.ShowEnding)
                    {
                        // Check if we're in a break transition state - allow transition to complete
                        if (_stateManager.CurrentState == AsyncBroadcastState.AdBreak && 
                            _stateManager.PendingBreakTransition)
                        {
                            KBTV.Core.Logger.Debug("AsyncBroadcastLoop: Show ending during break transition - allowing transition to complete");
                            continue;
                        }
                        else
                        {
                            // Normal show ending
                            KBTV.Core.Logger.Debug($"AsyncBroadcastLoop: Non-break interruption ({_lastInterruptionReason}) - stopping loop");
                            break;
                        }
                    }
                    else
                    {
                        // Other interruption types - stop the loop
                        KBTV.Core.Logger.Debug($"AsyncBroadcastLoop: Non-break interruption ({_lastInterruptionReason}) - stopping loop");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    KBTV.Core.Logger.Error($"AsyncBroadcastLoop: Error in broadcast loop: {ex.Message}");
                    // Continue with next executable even if one fails
                    await _stateManager.DelayAsync(1.0f, _cancellationTokenSource.Token);
                }
            }

            KBTV.Core.Logger.Debug($"AsyncBroadcastLoop: Broadcast loop ended - token cancelled: {_cancellationTokenSource?.Token.IsCancellationRequested}, show active: {_stateManager.IsShowActive}");
        }

        /// <summary>
        /// Execute a single broadcast executable with comprehensive error handling.
        /// </summary>
        private async Task ExecuteExecutableAsync(BroadcastExecutable executable, CancellationToken cancellationToken)
        {
            if (_currentExecutable != null)
            {
                try
                {
                    _currentExecutable.Cleanup();
                }
                catch (Exception ex)
                {
                    KBTV.Core.Logger.Error($"AsyncBroadcastLoop: Error cleaning up previous executable: {ex.Message}");
                    // Continue anyway
                }
            }

            _currentExecutable = executable;

            KBTV.Core.Logger.Debug($"AsyncBroadcastLoop: Executing {executable.Type} - {executable.Id} (requires await: {executable.RequiresAwait})");

            try
            {
                // Execute with timeout to prevent hanging
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                
                // Execute the executable (async/await based on RequiresAwait flag)
                Task executionTask;
                if (executable.RequiresAwait)
                {
                    KBTV.Core.Logger.Debug($"AsyncBroadcastLoop: Awaiting executable {executable.Id}");
                    executionTask = executable.ExecuteAsync(cancellationToken);
                }
                else
                {
                    // Await the background task to prevent token disposal issues
                    KBTV.Core.Logger.Debug($"AsyncBroadcastLoop: Running executable {executable.Id} in background");
                    executionTask = Task.Run(() => executable.ExecuteAsync(cancellationToken), cancellationToken);
                }

                var completedTask = await Task.WhenAny(executionTask, timeoutTask).ConfigureAwait(false);
                
                if (completedTask == timeoutTask)
                {
                    KBTV.Core.Logger.Error($"AsyncBroadcastLoop: Executable {executable.Id} timed out after 30 seconds");
                    // Don't throw - allow graceful degradation
                }
                else
                {
                    KBTV.Core.Logger.Debug($"AsyncBroadcastLoop: Executable {executable.Id} completed normally");
                }
            }
            catch (OperationCanceledException)
            {
                KBTV.Core.Logger.Debug($"AsyncBroadcastLoop: Executable {executable.Id} was interrupted - rethrowing");
                throw;
            }
            catch (Exception ex)
            {
                KBTV.Core.Logger.Error($"AsyncBroadcastLoop: Error executing {executable.Id}: {ex.Message}");
                // Don't rethrow - allow graceful degradation, continue with next executable
            }
            finally
            {
                // Cleanup after execution (Cleanup stays on background thread)
                try
                {
                    executable.Cleanup();
                }
                catch (Exception ex)
                {
                    KBTV.Core.Logger.Error($"AsyncBroadcastLoop: Error cleaning up executable {executable.Id}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handle interruption from external events (breaks, show ending, etc.).
        /// </summary>
        public void InterruptBroadcast(BroadcastInterruptionReason reason)
        {
            lock (_lock)
            {
                if (!_isRunning) 
                {
                    KBTV.Core.Logger.Debug($"AsyncBroadcastLoop: InterruptBroadcast called but not running - ignoring {reason}");
                    return;
                }

                KBTV.Core.Logger.Debug($"AsyncBroadcastLoop: Interrupting broadcast - {reason} (current executable: {_currentExecutable?.Id ?? "none"})");

                _lastInterruptionReason = reason;
                
                // Publish interruption event for UI and other components
                EventBus.Publish(new BroadcastInterruptionEvent(reason));
                
                _cancellationTokenSource?.Cancel();
            }
        }

        /// <summary>
        /// Internal unsafe broadcast stop method (caller must hold lock).
        /// </summary>
        private void StopBroadcastUnsafe()
        {
            if (!_isRunning) return;

            KBTV.Core.Logger.Debug("AsyncBroadcastLoop: Stopping broadcast");

            _isRunning = false;
            _cancellationTokenSource?.Cancel();
            EventBus.Publish(new BroadcastTimerCommand(BroadcastTimerCommandType.StopShow));

            // Cleanup current executable
            if (_currentExecutable != null)
            {
                _currentExecutable.Cleanup();
                _currentExecutable = null;
            }

            // Wait for task to complete
            try
            {
                _broadcastTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                KBTV.Core.Logger.Error($"AsyncBroadcastLoop: Error waiting for task completion: {ex.Message}");
            }

            // Store current source for later disposal (don't dispose now)
            if (_cancellationTokenSource != null)
            {
                _oldTokenSources.Add(_cancellationTokenSource);
            }
            
            _cancellationTokenSource = null;
            _broadcastTask = null;

            KBTV.Core.Logger.Debug("AsyncBroadcastLoop: Broadcast stopped");
        }

        /// <summary>
        /// Handle broadcast timing events - now primarily handled by BroadcastStateManager.
        /// AsyncBroadcastLoop may still need to respond to some events.
        /// </summary>
        private void HandleBroadcastTimingEvent(BroadcastTimingEvent timingEvent)
        {
            // Most timing event handling moved to BroadcastStateManager
            // This method kept for future extensibility if needed
            switch (timingEvent.Type)
            {
                case BroadcastTimingEventType.Break10Seconds:
                case BroadcastTimingEventType.Break5Seconds:
                    // Handled by BroadcastStateManager via pending transition or interruption
                    break;
            }
        }

        /// <summary>
        /// Stop the broadcast loop.
        /// </summary>
        public void StopBroadcast()
        {
            lock (_lock)
            {
                StopBroadcastUnsafe();
            }
        }

        /// <summary>
        /// Schedule a break at a specific time.
        /// </summary>
        public void ScheduleBreak(float breakTimeFromNow)
        {
            EventBus.Publish(new BroadcastTimerCommand(BroadcastTimerCommandType.ScheduleBreakWarnings, breakTimeFromNow));
        }

        /// <summary>
        /// Force start an ad break immediately.
        /// </summary>
        public void StartAdBreak()
        {
            EventBus.Publish(new BroadcastTimerCommand(BroadcastTimerCommandType.StartAdBreak));
        }

        /// <summary>
        /// Check if the broadcast is currently in an ad break.
        /// </summary>
        public bool IsInAdBreak()
        {
            return _stateManager.CurrentState == AsyncBroadcastState.AdBreak;
        }

        /// <summary>
        /// Queue the show end for outro music playback.
        /// </summary>
        public void QueueShowEnd()
        {
            KBTV.Core.Logger.Debug("AsyncBroadcastLoop: QueueShowEnd called - setting outro music queued flag");
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
        /// Handle break grace period from AdManager.
        /// </summary>
        private void OnBreakGracePeriod(float secondsRemaining)
        {
            KBTV.Core.Logger.Debug($"AsyncBroadcastLoop: Break grace period - {secondsRemaining}s remaining");
            // Async loop handles this via timing events
        }

        /// <summary>
        /// Handle break imminent from AdManager.
        /// </summary>
        private void OnBreakImminent(float secondsRemaining)
        {
            KBTV.Core.Logger.Debug($"AsyncBroadcastLoop: Break imminent - {secondsRemaining}s remaining");
            // Schedule break in async loop
            ScheduleBreak(secondsRemaining);
        }

        /// <summary>
        /// Handle state changed events to synchronize break ending.
        /// </summary>
        private void HandleStateChangedEvent(BroadcastStateChangedEvent @event)
        {
            KBTV.Core.Logger.Debug($"AsyncBroadcastLoop: State changed from {@event.PreviousState} to {@event.NewState}");

            // End the break when transitioning to BreakReturn
            if (@event.NewState == AsyncBroadcastState.BreakReturn)
            {
                AdManager.EndCurrentBreak();
                KBTV.Core.Logger.Debug("AsyncBroadcastLoop: Break ended via state transition to BreakReturn");
            }
        }

        public override void _ExitTree()
        {
            lock (_lock)
            {
                StopBroadcastUnsafe();
            }
            
            // Dispose all stored token sources
            foreach (var tokenSource in _oldTokenSources)
            {
                try
                {
                    tokenSource.Dispose();
                }
                catch (Exception ex)
                {
                    KBTV.Core.Logger.Error($"AsyncBroadcastLoop: Error disposing token source: {ex.Message}");
                }
            }
            _oldTokenSources.Clear();
            
            base._ExitTree();
        }
    }
}