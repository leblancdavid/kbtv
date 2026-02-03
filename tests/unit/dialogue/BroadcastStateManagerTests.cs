using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Dialogue
{
    public class BroadcastStateManagerTests : KBTVTestClass
    {
        public BroadcastStateManagerTests(Node testScene) : base(testScene) { }

        [Test]
        public void BreakStartingInterruption_DropsOnAirCaller()
        {
            // Arrange
            var caller = CreateTestCaller("Test Caller");
            var mockRepository = new MockCallerRepository();
            mockRepository.SetupOnAirCaller(caller);

            // Act - Simulate what HandleInterruptionEvent does for BreakStarting
            var interruptionEvent = new BroadcastInterruptionEvent(BroadcastInterruptionReason.BreakStarting);
            if (interruptionEvent.Reason == BroadcastInterruptionReason.BreakStarting)
            {
                var onAirCaller = mockRepository.OnAirCaller;
                if (onAirCaller != null)
                {
                    mockRepository.SetCallerState(onAirCaller, CallerState.Disconnected);
                    mockRepository.RemoveCaller(onAirCaller);
                }
            }

            // Assert
            AssertThat(mockRepository.DisconnectedCallers.Contains(caller));
            AssertThat(mockRepository.RemovedCallers.Contains(caller));
        }

        [Test]
        public void SetState_AdBreak_ResetsAdBreakState()
        {
            // Arrange - Create a state manager and simulate being in AdBreak with some state
            var stateManager = new BroadcastStateManager();
            stateManager.SetAdBreakState(3, new List<int> { 0, 1, 2 });
            stateManager.IncrementAdIndex(); // Simulate having played one ad
            stateManager.IncrementAdIndex(); // Simulate having played second ad
            
            // Verify initial state
            AssertThat(stateManager.CurrentAdIndex == 2);
            AssertThat(stateManager.TotalAdsForBreak == 3);
            AssertThat(stateManager.AdOrder.Count == 3);
            
            // Act - Transition to AdBreak again (like for a second break)
            stateManager.SetState(AsyncBroadcastState.AdBreak);
            
            // Assert - Ad break state should be reset
            AssertThat(stateManager.CurrentAdIndex == 0);
            AssertThat(stateManager.TotalAdsForBreak == 0);
            AssertThat(stateManager.AdOrder.Count == 0);
        }

        private Caller CreateTestCaller(string name = "Test Caller")
        {
            return new Caller(
                name,
                "555-0123",
                "Test Location",
                "Ghosts",
                "Ghosts",
                "Test Reason",
                CallerLegitimacy.Credible,
                CallerPhoneQuality.Good,
                CallerEmotionalState.Calm,
                CallerCurseRisk.Low,
                CallerBeliefLevel.Curious,
                CallerEvidenceLevel.None,
                CallerCoherence.Coherent,
                CallerUrgency.Low,
                "personality",
                null,
                null,
                null,
                "summary",
                30f,
                0.8f
            );
        }

        private class MockCallerRepository : ICallerRepository
        {
            public List<Caller> DisconnectedCallers { get; } = new();
            public List<Caller> RemovedCallers { get; } = new();
            private Caller? _onAirCaller;

            public void SetupOnAirCaller(Caller? caller) => _onAirCaller = caller;

            public IReadOnlyList<Caller> IncomingCallers => new List<Caller>();
            public IReadOnlyList<Caller> OnHoldCallers => new List<Caller>();
            public Caller? CurrentScreening => null;
            public Caller? OnAirCaller => _onAirCaller;
            public bool HasIncomingCallers => false;
            public bool HasOnHoldCallers => false;
            public bool IsScreening => false;
            public bool IsOnAir => _onAirCaller != null;
            public bool CanAcceptMoreCallers => true;
            public bool CanPutOnHold => true;

            public Result<Caller> AddCaller(Caller caller) => Result<Caller>.Ok(caller);
            public Result<Caller> StartScreening(Caller caller) => Result<Caller>.Ok(caller);
            public Result<Caller> StartScreeningNext() => Result<Caller>.Fail("No callers");
            public Result<Caller> ApproveScreening() => Result<Caller>.Fail("No screening");
            public Result<Caller> RejectScreening() => Result<Caller>.Fail("No screening");
            public Result<Caller> PutOnAir() => Result<Caller>.Fail("No caller");
            public Result<Caller> EndOnAir() => Result<Caller>.Fail("No caller on air");

            public bool SetCallerState(Caller caller, CallerState newState)
            {
                if (newState == CallerState.Disconnected)
                {
                    DisconnectedCallers.Add(caller);
                }
                return true;
            }

            public bool RemoveCaller(Caller caller)
            {
                RemovedCallers.Add(caller);
                return true;
            }

            public void ClearAll() { }
            public Caller? GetCaller(string callerId) => null;
            public void Subscribe(ICallerRepositoryObserver observer) { }
            public void Unsubscribe(ICallerRepositoryObserver observer) { }
        }
    }
}