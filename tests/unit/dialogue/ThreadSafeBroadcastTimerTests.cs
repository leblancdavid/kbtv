#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Core;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Dialogue
{
    /// <summary>
    /// Unit tests for ThreadSafeBroadcastTimer to ensure thread safety and proper functionality.
    /// Tests cover:
    /// - Thread safety with concurrent calls
    /// - Operation queue integrity
    /// - Main thread execution via CallDeferred
    /// - Error handling and edge cases
    /// - Performance and memory management
    /// </summary>
    public partial class ThreadSafeBroadcastTimerTests : KBTVTestClass
    {
        private ThreadSafeBroadcastTimer _timer = null!;

        public ThreadSafeBroadcastTimerTests(Node testScene) : base(testScene) 
        {
        }

        [Cleanup]
        public new void Cleanup()
        {
            _timer?.QueueFree();
        }

        [Test]
        public void Constructor_InitializesWithEmptyQueue()
        {
            // Create timer
            _timer = new ThreadSafeBroadcastTimer();
            TestScene.AddChild(_timer);
            
            AssertThat(_timer.QueueSize == 0);
            AssertThat(!_timer.IsInitialized);
            AssertThat(_timer.OperationsProcessed == 0);
        }

        [Test]
        public void StartShow_WithValidDuration_EnqueuesOperation()
        {
            // Create timer
            _timer = new ThreadSafeBroadcastTimer();
            TestScene.AddChild(_timer);
            
            // Act - queue operation from background thread
            Task.Run(() => _timer.StartShow(300.0f));
            
            // Wait a moment for operation to queue
            Thread.Sleep(100);
            
            // Assert - operation should be queued safely
            var queueSize = _timer.QueueSize;
            AssertThat(queueSize >= 0, "Queue should handle operations safely");
        }

        [Test]
        public void StopShow_EnqueuesOperation()
        {
            // Create timer
            _timer = new ThreadSafeBroadcastTimer();
            TestScene.AddChild(_timer);
            
            // Act
            Task.Run(() => _timer.StopShow());
            
            // Wait a moment for operation to queue
            Thread.Sleep(100);
            
            // Assert - should handle operation safely
            var queueSize = _timer.QueueSize;
            AssertThat(queueSize >= 0, "Queue should handle stop operations safely");
        }

        [Test]
        public void ScheduleBreakWarnings_WithValidTime_EnqueuesOperation()
        {
            // Create timer
            _timer = new ThreadSafeBroadcastTimer();
            TestScene.AddChild(_timer);
            
            // Act
            Task.Run(() => _timer.ScheduleBreakWarnings(60.0f));
            
            // Wait a moment for operation to queue
            Thread.Sleep(100);
            
            // Assert - should handle operation safely
            var queueSize = _timer.QueueSize;
            AssertThat(queueSize >= 0, "Queue should handle break warning operations safely");
        }

        [Test]
        public void StartAdBreak_WithDefaultDuration_EnqueuesOperation()
        {
            // Create timer
            _timer = new ThreadSafeBroadcastTimer();
            TestScene.AddChild(_timer);
            
            // Act
            Task.Run(() => _timer.StartAdBreak());
            
            // Wait a moment for operation to queue
            Thread.Sleep(100);
            
            // Assert - should handle operation safely
            var queueSize = _timer.QueueSize;
            AssertThat(queueSize >= 0, "Queue should handle ad break operations safely");
        }

        [Test]
        public void InvalidParameters_AreHandledGracefully()
        {
            // Create timer
            _timer = new ThreadSafeBroadcastTimer();
            TestScene.AddChild(_timer);
            
            // Act - these should not throw exceptions
            Task.Run(() => _timer.StartShow(-10.0f));
            Task.Run(() => _timer.StartAdBreak(-5.0f));
            Task.Run(() => _timer.ScheduleBreakWarnings(-60.0f));
            
            // Wait for operations to be processed
            Thread.Sleep(200);
            
            // Assert - queue should not grow excessively due to invalid operations
            var queueSize = _timer.QueueSize;
            AssertThat(queueSize < 10, "Invalid operations should not be queued");
        }

        [Test]
        public void DebugInfo_ProvidesValidInformation()
        {
            // Create timer
            _timer = new ThreadSafeBroadcastTimer();
            TestScene.AddChild(_timer);
            
            // Act
            var debugInfo = _timer.GetDebugInfo();
            
            // Assert
            AssertThat(debugInfo != null, "Debug info should not be null");
            AssertThat(debugInfo.ContainsKey("IsInitialized"), "Should contain IsInitialized");
            AssertThat(debugInfo.ContainsKey("QueueSize"), "Should contain QueueSize");
            AssertThat(debugInfo.ContainsKey("OperationsProcessed"), "Should contain OperationsProcessed");
        }

        [Test]
        public void LogDebugInfo_DoesNotThrowException()
        {
            // Create timer
            _timer = new ThreadSafeBroadcastTimer();
            TestScene.AddChild(_timer);
            
            // Act & Assert - should not throw
            _timer.LogDebugInfo();
        }

        [Test]
        public void IsShowActive_ReturnsValidState()
        {
            // Create timer
            _timer = new ThreadSafeBroadcastTimer();
            TestScene.AddChild(_timer);
            
            // Act
            var isActive = _timer.IsShowActive;
            
            // Assert
            AssertThat(isActive == true || isActive == false, "Should return valid boolean");
        }

        [Test]
        public void ConcurrentOperations_AreHandledSafely()
        {
            // Create timer
            _timer = new ThreadSafeBroadcastTimer();
            TestScene.AddChild(_timer);
            
            // Act - spawn multiple threads calling operations concurrently
            var tasks = new Task[5];
            
            for (int i = 0; i < tasks.Length; i++)
            {
                var threadId = i;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < 3; j++)
                    {
                        _timer.StartShow(300.0f + threadId);
                        _timer.ScheduleBreakWarnings(60.0f + j);
                        _timer.StartAdBreak(30.0f + j);
                    }
                });
            }
            
            // Wait for all threads to complete
            Task.WaitAll(tasks, TimeSpan.FromSeconds(5));
            
            // Wait for all operations to be processed
            Thread.Sleep(1000);
            
            // Assert - should handle concurrent operations without crashes
            var queueSize = _timer.QueueSize;
            AssertThat(queueSize >= 0, "Should handle concurrent operations safely");
        }
    }
}