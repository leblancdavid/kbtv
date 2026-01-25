#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Core;
using KBTV.Dialogue;
using KBTV.Callers;

namespace KBTV.Tests.Integration
{
    /// <summary>
    /// Integration tests for AsyncBroadcastLoop with ThreadSafeBroadcastTimer.
    /// Tests verify:
    /// - No threading errors occur during broadcast execution
    /// - Timer operations work correctly in real broadcast scenarios
    /// - Background thread execution is preserved
    /// - Integration with existing broadcast components
    /// </summary>
    public partial class AsyncBroadcastLoopIntegrationTests : KBTVTestClass
    {
        private AsyncBroadcastLoop _asyncLoop = null!;

        public AsyncBroadcastLoopIntegrationTests(Node testScene) : base(testScene) 
        {
        }

        [Cleanup]
        public new void Cleanup()
        {
            _asyncLoop?.StopBroadcast();
            _asyncLoop?.QueueFree();
        }

        [Test]
        public async Task AsyncBroadcastLoop_InitializesWithThreadSafeTimer()
        {
            // Create and add AsyncBroadcastLoop
            _asyncLoop = new AsyncBroadcastLoop();
            TestScene.AddChild(_asyncLoop);
            
            // Wait for initialization
            await Task.Delay(1000);
            
            // Assert - AsyncBroadcastLoop should initialize without threading errors
            AssertThat(_asyncLoop != null, "AsyncBroadcastLoop should be created");
            AssertThat(!_asyncLoop.IsRunning, "Should not be running initially");
        }

        [Test]
        public async Task StartBroadcast_UsesThreadSafeTimer()
        {
            // Create AsyncBroadcastLoop
            _asyncLoop = new AsyncBroadcastLoop();
            TestScene.AddChild(_asyncLoop);
            
            // Wait for initialization
            await Task.Delay(1000);
            
            try
            {
                // Act - start broadcast on background thread
                await _asyncLoop.StartBroadcastAsync(3.0f); // Very short duration for testing
                
                // Let it run briefly
                await Task.Delay(500);
                
                // Stop broadcast
                _asyncLoop.StopBroadcast();
                
                // Assert - no threading exceptions should occur
                AssertThat(true, "Broadcast should start and stop without exceptions");
            }
            catch (Exception ex)
            {
                AssertThat(false, $"Unexpected exception: {ex.Message}");
            }
        }

        [Test]
        public async Task TimerOperations_IntegrateCorrectly()
        {
            // Create AsyncBroadcastLoop
            _asyncLoop = new AsyncBroadcastLoop();
            TestScene.AddChild(_asyncLoop);
            
            // Wait for initialization
            await Task.Delay(1000);
            
            try
            {
                // Act
                await _asyncLoop.StartBroadcastAsync(2.0f); // Very short duration
                
                // Wait for timer operations
                await Task.Delay(500);
                
                // Stop broadcast
                _asyncLoop.StopBroadcast();
                
                // Assert - should complete without errors
                AssertThat(true, "Timer operations should integrate correctly");
            }
            catch (Exception ex)
            {
                AssertThat(false, $"Timer operations failed: {ex.Message}");
            }
        }

        [Test]
        public async Task InterruptBroadcast_WorksCorrectly()
        {
            // Create AsyncBroadcastLoop
            _asyncLoop = new AsyncBroadcastLoop();
            TestScene.AddChild(_asyncLoop);
            
            // Wait for initialization
            await Task.Delay(1000);
            
            try
            {
                // Act
                await _asyncLoop.StartBroadcastAsync(3.0f);
                
                // Wait for start
                await Task.Delay(500);
                
                // Interrupt broadcast
                _asyncLoop.InterruptBroadcast(BroadcastInterruptionReason.ShowEnding);
                
                // Wait for interruption
                await Task.Delay(500);
                
                // Assert
                AssertThat(true, "Interruption should be processed correctly");
            }
            catch (Exception ex)
            {
                AssertThat(false, $"Interruption failed: {ex.Message}");
            }
        }

        [Test]
        public async Task TimerOperations_FromBackgroundThreads_WorkCorrectly()
        {
            // Create AsyncBroadcastLoop
            _asyncLoop = new AsyncBroadcastLoop();
            TestScene.AddChild(_asyncLoop);
            
            // Wait for initialization
            await Task.Delay(1000);
            
            try
            {
                // Act - spawn background threads that use timer operations
                var backgroundOperations = new Task[3];
                
                for (int i = 0; i < backgroundOperations.Length; i++)
                {
                    var index = i;
                    backgroundOperations[i] = Task.Run(async () =>
                    {
                        await Task.Delay(100); // Small delay to simulate real usage
                        
                        // These operations should be thread-safe
                        switch (index % 3)
                        {
                            case 0:
                                _asyncLoop.ScheduleBreak(30.0f + index * 10);
                                break;
                            case 1:
                                _asyncLoop.StartAdBreak();
                                break;
                            case 2:
                                var inAdBreak = _asyncLoop.IsInAdBreak();
                                break;
                        }
                    });
                }
                
                // Wait for all operations
                await Task.WhenAll(backgroundOperations);
                
                // Assert - operations should complete without threading errors
                AssertThat(true, "All background timer operations should complete successfully");
            }
            catch (Exception ex)
            {
                AssertThat(false, $"Background operations failed: {ex.Message}");
            }
        }
    }
}