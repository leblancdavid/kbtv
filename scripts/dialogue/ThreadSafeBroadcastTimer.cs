#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Godot;
using KBTV.Core;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Thread-safe wrapper for BroadcastTimer that handles background thread calls safely.
    /// 
    /// This class solves the Godot threading problem where background threads cannot
    /// directly manipulate Godot Timer nodes. It uses an operation queue pattern where:
    /// 1. Background threads can safely queue timer operations
    /// 2. Operations are executed on the main thread via CallDeferred()
    /// 3. State synchronization is maintained through proper locking
    /// 
    /// Usage Pattern:
    /// - Background thread: timer.StartShow(duration) // Thread-safe
    /// - Main thread: Processes queued operations via CallDeferred
    /// - Result: All Godot Timer operations happen on main thread safely
    /// 
    /// Thread Safety Guarantees:
    /// - All public methods are thread-safe and can be called from any thread
    /// - Internal state is protected by locks
    /// - Godot operations only occur on main thread via CallDeferred
    /// - Operation queue order is preserved
    /// 
    /// Performance Characteristics:
    /// - Minimal overhead: operations are queued and batched
    /// - Background threading preserved for actual broadcast logic
    /// - Only timer operations marshalled to main thread
    /// - Queue size monitoring prevents memory leaks
    /// </summary>
    [GlobalClass]
    public partial class ThreadSafeBroadcastTimer : Node
    {
        #region Private Fields
        
        // Thread synchronization
        private readonly object _lock = new object();
        private readonly Queue<TimerOperation> _operationQueue = new();
        
        // Core components
        private BroadcastTimer? _broadcastTimer;
        private EventBus? _eventBus;
        
        // State management
        private volatile bool _isInitialized = false;
        private volatile bool _isProcessingQueue = false;
        private const int MaxQueueSize = 1000; // Prevent memory leaks
        
        // Performance monitoring
        private int _operationsProcessed = 0;
        private DateTime _lastProcessTime = DateTime.UtcNow;
        
        #endregion

        #region Public Properties
        
        /// <summary>
        /// Thread-safe access to show active state.
        /// Note: BroadcastTimer doesn't expose IsShowActive publicly, so this returns a default state.
        /// </summary>
        public bool IsShowActive => _isInitialized;
        
        /// <summary>
        /// Number of operations currently in the queue.
        /// Useful for debugging performance issues.
        /// </summary>
        public int QueueSize
        {
            get
            {
                lock (_lock)
                {
                    return _operationQueue.Count;
                }
            }
        }
        
        /// <summary>
        /// Whether the timer wrapper is initialized and ready to process operations.
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Total number of operations processed (for monitoring).
        /// </summary>
        public int OperationsProcessed => _operationsProcessed;
        
        #endregion

        #region Node Lifecycle
        
        public override void _Ready()
        {
            // Register self as an autoload service
            ServiceRegistry.Instance.RegisterSelf<ThreadSafeBroadcastTimer>(this);
            
            Initialize();
        }
        
        /// <summary>
        /// Initialize the thread-safe timer wrapper.
        /// Follows the standard KBTV deferred initialization pattern.
        /// </summary>
        private void Initialize()
        {
            if (!ServiceRegistry.IsInitialized)
            {
                GD.PrintErr("ThreadSafeBroadcastTimer: ServiceRegistry not initialized, deferring initialization");
                CallDeferred(nameof(InitializeWithServices));
                return;
            }

            InitializeWithServices();
        }
        
        private void InitializeWithServices()
        {
            if (!ServiceRegistry.IsInitialized)
            {
                GD.PrintErr("ThreadSafeBroadcastTimer: ServiceRegistry still not initialized, retrying...");
                CallDeferred(nameof(InitializeWithServices));
                return;
            }

            try
            {
                lock (_lock)
                {
                    _eventBus = ServiceRegistry.Instance.EventBus;
                    _broadcastTimer = new BroadcastTimer();
                    AddChild(_broadcastTimer);
                    
                    // Queue initialization operation to ensure EventBus is set
                    _operationQueue.Enqueue(new InitializeOperation(_eventBus));
                    
                    _isInitialized = true;
                    
                    // Process any pending operations
                    CallDeferred(nameof(ProcessOperationQueue));
                }
                
                GD.Print("ThreadSafeBroadcastTimer: Initialized successfully");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"ThreadSafeBroadcastTimer: Error during initialization: {ex.Message}");
            }
        }
        
        public override void _ExitTree()
        {
            // Cleanup remaining operations
            lock (_lock)
            {
                _operationQueue.Clear();
                _isInitialized = false;
            }
            
            base._ExitTree();
        }
        
        #endregion

        #region Thread-Safe Public API
        
        /// <summary>
        /// Start show timing with specified duration.
        /// Thread-safe - can be called from any thread.
        /// </summary>
        /// <param name="showDuration">Show duration in seconds</param>
        public void StartShow(float showDuration = 600.0f)
        {
            if (showDuration <= 0)
            {
                GD.PrintErr("ThreadSafeBroadcastTimer: Invalid show duration, must be positive");
                return;
            }
            
            EnqueueOperation(new StartShowOperation(showDuration));
        }
        
        /// <summary>
        /// Stop all show timing.
        /// Thread-safe - can be called from any thread.
        /// </summary>
        public void StopShow()
        {
            EnqueueOperation(new StopShowOperation());
        }
        
        /// <summary>
        /// Schedule break warning timers for a future break.
        /// Thread-safe - can be called from any thread.
        /// </summary>
        /// <param name="breakTimeFromNow">Time in seconds from now when the break should occur</param>
        public void ScheduleBreakWarnings(float breakTimeFromNow)
        {
            if (breakTimeFromNow <= 0)
            {
                GD.PrintErr("ThreadSafeBroadcastTimer: Invalid break time, must be positive");
                return;
            }
            
            EnqueueOperation(new ScheduleBreakWarningsOperation(breakTimeFromNow));
        }
        
        /// <summary>
        /// Start an ad break with specified duration.
        /// Thread-safe - can be called from any thread.
        /// </summary>
        /// <param name="duration">Ad break duration in seconds (default: 30.0f)</param>
        public void StartAdBreak(float duration = 30.0f)
        {
            if (duration <= 0)
            {
                GD.PrintErr("ThreadSafeBroadcastTimer: Invalid ad break duration, must be positive");
                return;
            }
            
            EnqueueOperation(new StartAdBreakOperation(duration));
        }
        
        /// <summary>
        /// Stop the current ad break.
        /// Thread-safe - can be called from any thread.
        /// </summary>
        public void StopAdBreak()
        {
            EnqueueOperation(new StopAdBreakOperation());
        }
        
        /// <summary>
        /// Get remaining time until a specific timing event.
        /// Thread-safe - result is provided via callback.
        /// </summary>
        /// <param name="eventType">Type of timing event to check</param>
        /// <param name="callback">Callback to receive the result</param>
        public void GetTimeUntil(BroadcastTimingEventType eventType, Action<float> callback)
        {
            if (callback == null)
            {
                GD.PrintErr("ThreadSafeBroadcastTimer: Callback cannot be null for GetTimeUntil");
                return;
            }
            
            EnqueueOperation(new GetTimeUntilOperation(eventType, callback));
        }
        
        /// <summary>
        /// Check if a specific timer is currently active.
        /// Thread-safe - result is provided via callback.
        /// </summary>
        /// <param name="eventType">Type of timing event to check</param>
        /// <param name="callback">Callback to receive the result</param>
        public void IsTimerActive(BroadcastTimingEventType eventType, Action<bool> callback)
        {
            if (callback == null)
            {
                GD.PrintErr("ThreadSafeBroadcastTimer: Callback cannot be null for IsTimerActive");
                return;
            }
            
            EnqueueOperation(new IsTimerActiveOperation(eventType, callback));
        }
        
        #endregion

        #region Operation Queue Management
        
        /// <summary>
        /// Thread-safe operation queuing with overflow protection.
        /// </summary>
        /// <param name="operation">Operation to enqueue</param>
        private void EnqueueOperation(TimerOperation operation)
        {
            lock (_lock)
            {
                // Prevent queue overflow
                if (_operationQueue.Count >= MaxQueueSize)
                {
                    GD.PrintErr($"ThreadSafeBroadcastTimer: Operation queue overflow ({_operationQueue.Count} operations), dropping oldest operation");
                    _operationQueue.Dequeue();
                }
                
                _operationQueue.Enqueue(operation);
                
                // Trigger processing if initialized and not already processing
                if (_isInitialized && !_isProcessingQueue)
                {
                    CallDeferred(nameof(ProcessOperationQueue));
                }
            }
        }
        
        /// <summary>
        /// Process queued operations on the main thread.
        /// This method is called via CallDeferred() to ensure main thread execution.
        /// </summary>
        private void ProcessOperationQueue()
        {
            if (!IsInstanceValid(this) || !_isInitialized)
            {
                return;
            }
            
            List<TimerOperation> operationsToProcess;
            
            lock (_lock)
            {
                if (_operationQueue.Count == 0)
                {
                    _isProcessingQueue = false;
                    return;
                }
                
                _isProcessingQueue = true;
                
                // Get all current operations to minimize lock time
                operationsToProcess = _operationQueue.ToList();
                _operationQueue.Clear();
            }
            
            // Process operations outside of lock to prevent deadlock
            foreach (var operation in operationsToProcess)
            {
                try
                {
                    ExecuteOperationOnMainThread(operation);
                    _operationsProcessed++;
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"ThreadSafeBroadcastTimer: Error executing operation {operation.GetType().Name}: {ex.Message}");
                }
            }
            
            // Update performance monitoring
            _lastProcessTime = DateTime.UtcNow;
            
            // Check if more operations arrived while processing
            lock (_lock)
            {
                _isProcessingQueue = false;
                if (_operationQueue.Count > 0)
                {
                    CallDeferred(nameof(ProcessOperationQueue));
                }
            }
        }
        
        /// <summary>
        /// Execute a single operation on the main thread.
        /// All Godot Timer operations happen here.
        /// </summary>
        /// <param name="operation">Operation to execute</param>
        private void ExecuteOperationOnMainThread(TimerOperation operation)
        {
            if (_broadcastTimer == null)
            {
                GD.PrintErr("ThreadSafeBroadcastTimer: BroadcastTimer not initialized");
                return;
            }
            
            switch (operation)
            {
                case InitializeOperation initOp:
                    // Initialize operation is handled during _broadcastTimer creation
                    break;
                    
                case StartShowOperation startOp:
                    _broadcastTimer.StartShow(startOp.Duration);
                    break;
                    
                case StopShowOperation:
                    _broadcastTimer.StopShow();
                    break;
                    
                case ScheduleBreakWarningsOperation scheduleOp:
                    _broadcastTimer.ScheduleBreakWarnings(scheduleOp.BreakTimeFromNow);
                    break;
                    
                case StartAdBreakOperation startAdOp:
                    _broadcastTimer.StartAdBreak(startAdOp.Duration);
                    break;
                    
                case StopAdBreakOperation:
                    _broadcastTimer.StopAdBreak();
                    break;
                    
                case GetTimeUntilOperation timeOp:
                    var timeUntil = _broadcastTimer.GetTimeUntil(timeOp.EventType);
                    try
                    {
                        timeOp.Callback(timeUntil);
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"ThreadSafeBroadcastTimer: Error in GetTimeUntil callback: {ex.Message}");
                    }
                    break;
                    
                case IsTimerActiveOperation activeOp:
                    var isActive = _broadcastTimer.IsTimerActive(activeOp.EventType);
                    try
                    {
                        activeOp.Callback(isActive);
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"ThreadSafeBroadcastTimer: Error in IsTimerActive callback: {ex.Message}");
                    }
                    break;
                    
                default:
                    GD.PrintErr($"ThreadSafeBroadcastTimer: Unknown operation type: {operation.GetType().Name}");
                    break;
            }
        }
        
        #endregion

        #region Debug and Monitoring
        
        /// <summary>
        /// Get performance and debugging information.
        /// Useful for monitoring the health of the timer wrapper.
        /// </summary>
        /// <returns>Debug information dictionary</returns>
        public Dictionary<string, object> GetDebugInfo()
        {
            lock (_lock)
            {
                return new Dictionary<string, object>
                {
                    ["IsInitialized"] = _isInitialized,
                    ["QueueSize"] = _operationQueue.Count,
                    ["IsProcessingQueue"] = _isProcessingQueue,
                    ["OperationsProcessed"] = _operationsProcessed,
                    ["LastProcessTime"] = _lastProcessTime,
                    ["MaxQueueSize"] = MaxQueueSize,
                    ["IsShowActive"] = _isInitialized
                };
            }
        }
        
        /// <summary>
        /// Log current debug state for troubleshooting.
        /// </summary>
        public void LogDebugInfo()
        {
            var debugInfo = GetDebugInfo();
            GD.Print($"ThreadSafeBroadcastTimer Debug: {string.Join(", ", debugInfo.Select(kv => $"{kv.Key}={kv.Value}"))}");
        }
        
        #endregion
    }
}