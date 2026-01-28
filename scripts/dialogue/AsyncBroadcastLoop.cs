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

        private BroadcastStateManager _stateManager = null!;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly List<CancellationTokenSource> _oldTokenSources = new();
        private Task? _broadcastTask;
        private bool _isRunning = false;
        private BroadcastExecutable? _currentExecutable;
        private readonly object _lock = new object();
        private BroadcastInterruptionReason _lastInterruptionReason;

        public bool IsRunning => _isRunning;
        public BroadcastExecutable? CurrentExecutable => _currentExecutable;

        // Provider interface implementation
        AsyncBroadcastLoop IProvide<AsyncBroadcastLoop>.Value() => this;

        /// <summary>
        /// Called when all dependencies are resolved.
        /// </summary>
        public void OnResolved()
        {
            GD.Print("AsyncBroadcastLoop: Dependencies resolved, initializing...");

            try
            {
                // Get VernDialogue from Template repository
                var vernDialogueLoader = new VernDialogueLoader();
                vernDialogueLoader.LoadDialogue();
                var vernDialogue = vernDialogueLoader.VernDialogue;

                _stateManager = new BroadcastStateManager(CallerRepository, ArcRepository, vernDialogue, EventBus, ListenerManager, DependencyInjection.Get<IBroadcastAudioService>(this), GetTree());

                // Subscribe to broadcast timing events for break warnings
                EventBus.Subscribe<BroadcastTimingEvent>(HandleBroadcastTimingEvent);

                GD.Print("AsyncBroadcastLoop: Initialization complete");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"AsyncBroadcastLoop: Error during initialization: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when node enters the scene tree and is ready.
        /// </summary>
        public void OnReady()
        {
            GD.Print("AsyncBroadcastLoop: Ready, providing service to descendants");
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
                    GD.PrintErr("AsyncBroadcastLoop: Broadcast is already running");
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
                    GD.PrintErr("AsyncBroadcastLoop: No initial executable available");
                    lock (_lock)
                    {
                        _isRunning = false;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                GD.Print("AsyncBroadcastLoop: Broadcast cancelled");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"AsyncBroadcastLoop: Error in broadcast: {ex.Message}");
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
            GD.Print("AsyncBroadcastLoop: Starting main broadcast loop");

            while (!cancellationToken.IsCancellationRequested && _stateManager.IsShowActive)
            {
                try
                {
                    // Get next executable from state manager
                    var nextExecutable = _stateManager.GetNextExecutable();
                    
                    if (nextExecutable == null)
                    {
                    // No executable available - wait a bit and try again
                    await _stateManager.DelayAsync(1.0f, cancellationToken);
                        continue;
                    }

                    // Execute the executable
                    await ExecuteExecutableAsync(nextExecutable, cancellationToken);

                    // Update state manager after execution
                    _stateManager.UpdateStateAfterExecution(nextExecutable);

                    // Small delay between executables
                    await _stateManager.DelayAsync(0.1f, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Check if this is a break interruption - continue the loop with fresh token
                    if (_lastInterruptionReason == BroadcastInterruptionReason.BreakImminent || 
                        _lastInterruptionReason == BroadcastInterruptionReason.BreakStarting)
                    {
                        GD.Print($"AsyncBroadcastLoop: Break interruption detected ({_lastInterruptionReason}) - resetting token and continuing");
                        
                        // Clean up old token
                        if (_cancellationTokenSource != null)
                        {
                            _oldTokenSources.Add(_cancellationTokenSource);
                            _cancellationTokenSource.Dispose();
                        }
                        
                        // Create fresh token for continued execution
                        _cancellationTokenSource = new CancellationTokenSource();
                        
                        GD.Print("AsyncBroadcastLoop: Fresh token created, continuing loop");
                        continue;
                    }
                    else
                    {
                        // Show ending or other interruption - stop the loop
                        GD.Print($"AsyncBroadcastLoop: Non-break interruption ({_lastInterruptionReason}) - stopping loop");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"AsyncBroadcastLoop: Error in broadcast loop: {ex.Message}");
                    // Continue with next executable even if one fails
                    await _stateManager.DelayAsync(1.0f, cancellationToken);
                }
            }

            GD.Print("AsyncBroadcastLoop: Broadcast loop ended");
        }

        /// <summary>
        /// Execute a single broadcast executable.
        /// </summary>
        private async Task ExecuteExecutableAsync(BroadcastExecutable executable, CancellationToken cancellationToken)
        {
            if (_currentExecutable != null)
            {
                _currentExecutable.Cleanup();
            }

            _currentExecutable = executable;

            GD.Print($"AsyncBroadcastLoop: Executing {executable.Type} - {executable.Id} (requires await: {executable.RequiresAwait})");

            try
            {
                // Execute the executable (async/await based on RequiresAwait flag)
                if (executable.RequiresAwait)
                {
                    GD.Print($"AsyncBroadcastLoop: Awaiting executable {executable.Id}");
                    await executable.ExecuteAsync(cancellationToken);
                    GD.Print($"AsyncBroadcastLoop: Executable {executable.Id} completed normally");
                }
                else
                {
                    // Await the background task to prevent token disposal issues
                    GD.Print($"AsyncBroadcastLoop: Running executable {executable.Id} in background");
                    await Task.Run(() => executable.ExecuteAsync(cancellationToken), cancellationToken);
                    GD.Print($"AsyncBroadcastLoop: Background executable {executable.Id} completed");
                }
            }
            catch (OperationCanceledException)
            {
                GD.Print($"AsyncBroadcastLoop: Executable {executable.Id} was interrupted - rethrowing");
                throw;
            }
            finally
            {
                // Cleanup after execution (Cleanup stays on background thread)
                executable.Cleanup();
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
                    GD.Print($"AsyncBroadcastLoop: InterruptBroadcast called but not running - ignoring {reason}");
                    return;
                }

                GD.Print($"AsyncBroadcastLoop: Interrupting broadcast - {reason} (current executable: {_currentExecutable?.Id ?? "none"})");

                _lastInterruptionReason = reason;
                _cancellationTokenSource?.Cancel();
            }
        }

        /// <summary>
        /// Internal unsafe broadcast stop method (caller must hold lock).
        /// </summary>
        private void StopBroadcastUnsafe()
        {
            if (!_isRunning) return;

            GD.Print("AsyncBroadcastLoop: Stopping broadcast");

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
                GD.PrintErr($"AsyncBroadcastLoop: Error waiting for task completion: {ex.Message}");
            }

            // Store current source for later disposal (don't dispose now)
            if (_cancellationTokenSource != null)
            {
                _oldTokenSources.Add(_cancellationTokenSource);
            }
            
            _cancellationTokenSource = null;
            _broadcastTask = null;

            GD.Print("AsyncBroadcastLoop: Broadcast stopped");
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
                    GD.PrintErr($"AsyncBroadcastLoop: Error disposing token source: {ex.Message}");
                }
            }
            _oldTokenSources.Clear();
            
            base._ExitTree();
        }
    }
}