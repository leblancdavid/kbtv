# ThreadSafeBroadcastTimer - Godot Thread Safety Pattern

## Overview

The **ThreadSafeBroadcastTimer** is a thread-safe wrapper that solves the critical Godot threading problem where background threads cannot safely manipulate Godot nodes (particularly Timers). This pattern provides a robust solution for maintaining async broadcast performance while ensuring thread safety.

## Problem: Godot Threading Limitations

### The Issue
Godot enforces strict threading rules:
- **Main Thread Only**: Godot nodes (including Timer) can only be modified from the main thread
- **Background Access Violation**: Accessing node properties from background threads throws exceptions
- **Async Broadcast Loop**: The AsyncBroadcastLoop runs on background threads for performance

### Error Example
```
E 0:00:02:611 Caller thread can't call this function in this node (/root/AsyncBroadcastLoop/@Node@6/@Timer@11). 
Use call_deferred() or call_thread_group() instead.
```

### Root Cause Analysis
```csharp
// Background thread execution (AsyncBroadcastLoop.StartBroadcastAsync)
_ = Task.Run(async () => {
    await _asyncLoop.StartBroadcastAsync(showDuration); // Background thread
});

// Inside AsyncBroadcastLoop - still on background thread
_broadcastTimer.StartShow(showDuration); // ❌ Timer.WaitTime modification on background thread
```

## Solution: ThreadSafeBroadcastTimer Pattern

### Architecture Overview
The ThreadSafeBroadcastTimer uses an **Operation Queue Pattern** with **CallDeferred** marshalling:

```
Background Thread                    Main Thread
┌─────────────────┐                ┌─────────────────┐
│ Timer API Call   │ ── enqueue ─► │ CallDeferred()  │
│ (Thread-Safe)   │                │ Process Queue   │
└─────────────────┘                └─────────────────┘
        │                                   │
        └───────── Event/Callback ──────────┘
```

### Key Components

#### 1. TimerOperation Records
```csharp
public abstract record TimerOperation
{
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public string? OperationId { get; init; }
}

public record StartShowOperation(float Duration) : TimerOperation;
public record StopShowOperation : TimerOperation;
public record ScheduleBreakWarningsOperation(float BreakTimeFromNow) : TimerOperation;
```

**Benefits:**
- Immutable operation definitions
- Compile-time type safety
- Built-in debugging information (timestamps, operation IDs)
- Extensible for new timer operations

#### 2. ThreadSafeBroadcastTimer Wrapper
```csharp
[GlobalClass]
public partial class ThreadSafeBroadcastTimer : Node
{
    private readonly object _lock = new object();
    private readonly Queue<TimerOperation> _operationQueue = new();
    private BroadcastTimer? _broadcastTimer;
    private volatile bool _isInitialized = false;
}
```

**Core Features:**
- **Thread-safe API** - All methods can be called from any thread
- **Operation queue** - Serialized execution on main thread
- **Overflow protection** - Prevents memory leaks
- **Performance monitoring** - Debug information and metrics
- **Error handling** - Graceful degradation and logging

#### 3. Operation Queue Processing
```csharp
// Thread-safe operation queuing
public void StartShow(float showDuration)
{
    EnqueueOperation(new StartShowOperation(showDuration));
}

// Main thread execution via CallDeferred
private void ProcessOperationQueue()
{
    var operationsToProcess = GetQueuedOperations();
    foreach (var operation in operationsToProcess)
    {
        ExecuteOperationOnMainThread(operation);
    }
}
```

## Implementation Details

### Thread Safety Guarantees

#### 1. Lock-Based Synchronization
```csharp
private void EnqueueOperation(TimerOperation operation)
{
    lock (_lock)
    {
        if (_operationQueue.Count >= MaxQueueSize)
        {
            _operationQueue.Dequeue(); // Overflow protection
        }
        _operationQueue.Enqueue(operation);
        
        if (_isInitialized && !_isProcessingQueue)
        {
            CallDeferred(nameof(ProcessOperationQueue));
        }
    }
}
```

#### 2. CallDeferred Main Thread Marshalling
```csharp
private void ExecuteOperationOnMainThread(TimerOperation operation)
{
    // This runs on main thread - safe to modify Godot nodes
    switch (operation)
    {
        case StartShowOperation startOp:
            _broadcastTimer!.StartShow(startOp.Duration); // ✅ Safe: Main thread
            break;
        case StopShowOperation:
            _broadcastTimer!.StopShow(); // ✅ Safe: Main thread
            break;
    }
}
```

#### 3. State Synchronization
```csharp
public bool IsShowActive
{
    get
    {
        lock (_lock)
        {
            return _broadcastTimer?.IsShowActive ?? false;
        }
    }
}
```

### Performance Characteristics

#### Minimal Overhead
- **Operation Queue**: O(1) enqueue/dequeue operations
- **Lock Duration**: Microseconds - only for queue access
- **Main Thread Processing**: Batched operations minimize context switching

#### Memory Management
```csharp
// Overflow protection prevents memory leaks
private const int MaxQueueSize = 1000;

// Queue size monitoring
public int QueueSize => _operationQueue.Count;

// Performance metrics
public int OperationsProcessed => _operationsProcessed;
```

#### Debug Information
```csharp
public Dictionary<string, object> GetDebugInfo()
{
    return new Dictionary<string, object>
    {
        ["IsInitialized"] = _isInitialized,
        ["QueueSize"] = _operationQueue.Count,
        ["IsProcessingQueue"] = _operationsProcessed,
        ["LastProcessTime"] = _lastProcessTime
    };
}
```

## Usage Patterns

### Basic Usage
```csharp
// From any thread (background or main)
_timer.StartShow(600.0f);           // Thread-safe
_timer.ScheduleBreakWarnings(300.0f); // Thread-safe
_timer.StartAdBreak(30.0f);         // Thread-safe
_timer.StopShow();                  // Thread-safe
```

### Callback-Based Operations
```csharp
// Get state information safely
_timer.GetTimeUntil(BroadcastTimingEventType.ShowEnd, (timeUntil) => {
    GD.Print($"Time until show end: {timeUntil}s");
});

_timer.IsTimerActive(BroadcastTimingEventType.AdBreakEnd, (isActive) => {
    GD.Print($"Ad break timer active: {isActive}");
});
```

### Error Handling
```csharp
// Invalid parameters are handled gracefully
_timer.StartShow(-10.0f); // Logs error, doesn't queue operation
_timer.StartAdBreak(0.0f); // Logs error, doesn't queue operation

// Null callbacks are rejected
_timer.GetTimeUntil(BroadcastTimingEventType.ShowEnd, null!); // Logs error
```

### Monitoring and Debugging
```csharp
// Check timer health
var debugInfo = _timer.GetDebugInfo();
GD.Print($"Queue size: {debugInfo["QueueSize"]}");
GD.Print($"Operations processed: {debugInfo["OperationsProcessed"]}");

// Log current state
_timer.LogDebugInfo();
```

## Integration with AsyncBroadcastLoop

### Migration Steps

#### Before (Problematic)
```csharp
// AsyncBroadcastLoop.cs
private BroadcastTimer _broadcastTimer = null!;

public async Task StartBroadcastAsync(float showDuration = 600.0f)
{
    _broadcastTimer.StartShow(showDuration); // ❌ Thread safety violation
}

private void InitializeWithServices()
{
    _broadcastTimer = new BroadcastTimer();
    AddChild(_broadcastTimer);
}
```

#### After (Thread-Safe)
```csharp
// AsyncBroadcastLoop.cs
private ThreadSafeBroadcastTimer _broadcastTimer = null!;

public async Task StartBroadcastAsync(float showDuration = 600.0f)
{
    _broadcastTimer.StartShow(showDuration); // ✅ Thread-safe
}

private void InitializeWithServices()
{
    _broadcastTimer = new ThreadSafeBroadcastTimer();
    AddChild(_broadcastTimer);
}
```

### Compatibility
- **API Preservation**: Same method signatures as original BroadcastTimer
- **Behavior Preservation**: Identical timing and event behavior
- **Performance**: Same broadcast performance with thread safety

## Testing Strategy

### Unit Tests
```csharp
[Test]
public void StartShow_QueuesOperationAndProcessesIt()
{
    // Test thread safety with concurrent calls
    Task.Run(() => _timer.StartShow(300.0f));
    WaitUntil(() => _timer.QueueSize == 0, TimeSpan.FromSeconds(5));
    AssertThat(_timer.OperationsProcessed >= 1);
}

[Test]
public void ConcurrentOperations_AreProcessedSafely()
{
    // Test with multiple threads and operations
    const int threadCount = 10;
    const int operationsPerThread = 5;
    // ... spawn threads and verify safe processing
}
```

### Integration Tests
```csharp
[Test]
public async Task StartBroadcastAsync_WithThreadSafeTimer_DoesNotThrowThreadingErrors()
{
    // Test full broadcast execution without threading errors
    await _asyncLoop.StartBroadcastAsync(60.0f);
    // Verify no exceptions and proper broadcast flow
}
```

### Performance Tests
```csharp
[Test]
public async Task LongRunningBroadcast_WithTimerOperations_StaysStable()
{
    // Monitor operation processing over extended periods
    await _asyncLoop.StartBroadcastAsync(60.0f);
    // Verify stable performance and no memory leaks
}
```

## Benefits and Trade-offs

### Benefits

#### 1. **Thread Safety**
- Eliminates all Godot threading violations
- Robust synchronization and state management
- Graceful error handling and recovery

#### 2. **Performance Preservation**
- Background threading maintained for broadcast logic
- Minimal overhead for timer operations
- Batched processing reduces context switching

#### 3. **Maintainability**
- Clean separation of concerns
- Well-documented patterns
- Extensive error handling and logging

#### 4. **Reusability**
- Pattern can be applied to other Godot node operations
- Generic operation framework
- Consistent with existing KBTV patterns

#### 5. **Monitoring and Debugging**
- Built-in performance metrics
- Comprehensive debugging information
- Real-time health monitoring

### Trade-offs

#### 1. **Complexity**
- Additional layer of abstraction
- More code to maintain
- Requires understanding of threading patterns

#### 2. **Memory Usage**
- Operation queue consumes memory
- Needs overflow protection
- Monitoring required for large-scale usage

#### 3. **Latency**
- Small delay for CallDeferred processing
- Queue processing overhead
- Not suitable for real-time critical operations

## Best Practices

### When to Use This Pattern

#### ✅ Good Use Cases
- Background threads need to modify Godot nodes
- Batch operations that can tolerate small delays
- Timer-based operations and scheduled events
- Long-running background processes

#### ❌ Avoid This Pattern
- Real-time critical operations requiring immediate execution
- High-frequency operations (>1000 ops/second)
- Simple one-time main thread operations
- Operations requiring synchronous responses

### Implementation Guidelines

#### 1. **Operation Design**
```csharp
// ✅ Good: Parameterized operations
public record StartShowOperation(float Duration) : TimerOperation;

// ❌ Avoid: Operations with complex side effects
public record ComplexOperation(Action Callback) : TimerOperation;
```

#### 2. **Error Handling**
```csharp
// ✅ Good: Validate parameters before queuing
if (showDuration <= 0)
{
    GD.PrintErr("Invalid duration, must be positive");
    return;
}

// ✅ Good: Handle exceptions in main thread execution
try
{
    ExecuteOperationOnMainThread(operation);
}
catch (Exception ex)
{
    GD.PrintErr($"Error executing operation: {ex.Message}");
}
```

#### 3. **Performance Monitoring**
```csharp
// Monitor queue health regularly
if (_timer.QueueSize > 500)
{
    GD.Print($"Warning: High queue size: {_timer.QueueSize}");
}

// Log performance metrics
_timer.LogDebugInfo();
```

#### 4. **Memory Management**
```csharp
// Set reasonable queue limits
private const int MaxQueueSize = 1000;

// Clear queue on cleanup
public override void _ExitTree()
{
    lock (_lock)
    {
        _operationQueue.Clear();
    }
}
```

## Extension and Customization

### Adding New Operations

#### 1. Define Operation Type
```csharp
public record CustomTimerOperation(string Parameter, int Value) : TimerOperation;
```

#### 2. Add Public Method
```csharp
public void CustomOperation(string parameter, int value)
{
    EnqueueOperation(new CustomTimerOperation(parameter, value));
}
```

#### 3. Implement Execution Logic
```csharp
case CustomTimerOperation customOp:
    _broadcastTimer!.CustomMethod(customOp.Parameter, customOp.Value);
    break;
```

### Generic Framework Extension
```csharp
// Generic wrapper for other Godot nodes
public class ThreadSafeNodeWrapper<T> : Node where T : Node
{
    private readonly Queue<NodeOperation<T>> _operationQueue = new();
    
    public void EnqueueOperation(NodeOperation<T> operation)
    {
        lock (_lock)
        {
            _operationQueue.Enqueue(operation);
            CallDeferred(nameof(ProcessOperationQueue));
        }
    }
}
```

### Performance Optimization
```csharp
// Batch similar operations
private Dictionary<Type, List<TimerOperation>> GroupOperationsByType(List<TimerOperation> operations)
{
    return operations
        .GroupBy(op => op.GetType())
        .ToDictionary(g => g.Key, g => g.ToList());
}

// Process batches for efficiency
foreach (var batch in operationBatches)
{
    ProcessOperationBatch(batch.Key, batch.Value);
}
```

## Conclusion

The ThreadSafeBroadcastTimer pattern provides a robust, performant solution to Godot's threading limitations while preserving the async broadcast architecture. The operation queue approach offers:

- **Thread Safety**: Eliminates all threading violations
- **Performance**: Maintains background threading benefits
- **Maintainability**: Clean, documented, extensible code
- **Reliability**: Comprehensive error handling and monitoring
- **Scalability**: Pattern can be extended to other Godot operations

This implementation follows established KBTV patterns (locks, CallDeferred, EventBus) and provides a foundation for handling future thread-safety challenges as the project grows. The pattern balances complexity with safety and performance, making it an ideal solution for the AsyncBroadcastLoop threading issues.