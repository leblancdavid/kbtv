using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Screening;
using KBTV.Managers;
using KBTV.Persistence;
using KBTV.Data;
using System.Collections.Generic;
using System;

namespace KBTV.Tests.Unit.Screening
{
    public class ScreeningControllerTests : KBTVTestClass
    {
        public ScreeningControllerTests(Node testScene) : base(testScene) { }

        private ScreeningController _controller = null!;
        private MockCallerRepository _mockRepository = null!;
        private SaveManager _saveManager = null!;
        private MockGameStateManager _mockGameStateManager = null!;

        [Setup]
        public void Setup()
        {
            _mockRepository = new MockCallerRepository();
            _saveManager = new SaveManager();
            _mockGameStateManager = new MockGameStateManager();
            _controller = new ScreeningController(_mockRepository, new TopicManager(), _saveManager, _mockGameStateManager);
        }

        private Caller CreateTestCaller(float patience = 30f)
        {
            return new Caller(
                "Test Caller",
                "555-0123",
                "Test Location",
                "Ghosts",
                "Ghosts",
                "Test Reason",
                CallerLegitimacy.Credible,
                CallerPhoneQuality.Good,
                CallerEmotionalState.Calm,
                CallerCurseRisk.Low,
                CallerEvidenceLevel.None,
                CallerCoherence.Coherent,
                CallerUrgency.Low,
                "personality",
                null,
                null,
                null,
                "summary",
                patience,
                0.8f
            );
        }

        [Test]
        public void Constructor_InitializesToIdle()
        {
            AssertThat(_controller.Phase == ScreeningPhase.Idle);
            AssertThat(!_controller.IsActive);
            AssertThat(_controller.CurrentCaller == null);
        }

        [Test]
        public void Start_NullCaller_DoesNotStart()
        {
            _controller.Start(null!);

            AssertThat(_controller.Phase == ScreeningPhase.Idle);
            AssertThat(!_controller.IsActive);
        }

        [Test]
        public void Start_ValidCaller_StartsScreening()
        {
            var caller = CreateTestCaller();
            _controller.Start(caller);

            AssertThat(_controller.IsActive);
            AssertThat(_controller.CurrentCaller == caller);
            AssertThat(_controller.Phase == ScreeningPhase.Gathering);
        }

        [Test]
        public void Start_CallerResetRevelations()
        {
            var caller = CreateTestCaller();
            var initialRevealed = caller.GetRevealedProperties().Count;

            _controller.Start(caller);

            AssertThat(initialRevealed == 0);
            AssertThat(caller.GetRevealedProperties().Count == 0);
        }

        [Test]
        public void Start_TriggersPhaseChangedEvent()
        {
            ScreeningPhase? triggeredPhase = null;
            _controller.PhaseChanged += phase => triggeredPhase = phase;

            var caller = CreateTestCaller();
            _controller.Start(caller);

            AssertThat(triggeredPhase == ScreeningPhase.Gathering);
        }

        [Test]
        public void Approve_NoSession_ReturnsFailure()
        {
            var result = _controller.Approve();

            AssertThat(result.IsFailure);
            AssertThat(result.ErrorCode == "NO_SESSION");
        }

        [Test]
        public void Update_NoSession_DoesNotUpdate()
        {
            _controller.Update(1f);

            AssertThat(_controller.Phase == ScreeningPhase.Idle);
        }

        [Test]
        public void Update_WithSession_UpdatesProgress()
        {
            var caller = CreateTestCaller(patience: 30f);
            _controller.Start(caller);

            _controller.Update(1f);

            var progress = _controller.Progress;
            AssertThat(progress.ElapsedTime >= 0f);
        }

        [Test]
        public void Update_TriggersProgressUpdatedEvent()
        {
            var caller = CreateTestCaller();
            _controller.Start(caller);

            ScreeningProgress triggeredProgress = default;
            _controller.ProgressUpdated += progress => triggeredProgress = progress;

            _controller.Update(1f);

            AssertThat(triggeredProgress != null);
        }

        [Test]
        public void Phase_ReturnsCurrentPhase()
        {
            AssertThat(_controller.Phase == ScreeningPhase.Idle);

            var caller = CreateTestCaller();
            _controller.Start(caller);

            AssertThat(_controller.Phase == ScreeningPhase.Gathering);
        }

        [Test]
        public void Progress_IdleSession_ReturnsZeroProgress()
        {
            var progress = _controller.Progress;

            AssertThat(progress.PropertiesRevealed == 0);
            AssertThat(progress.TotalProperties == 0);
            AssertThat(progress.PatienceRemaining == 0f);
            AssertThat(progress.ElapsedTime == 0f);
        }

        [Test]
        public void Progress_ActiveSession_ReturnsValidProgress()
        {
            var caller = CreateTestCaller(patience: 30f);
            _controller.Start(caller);

            var progress = _controller.Progress;

            AssertThat(progress.PropertiesRevealed >= 0);
            AssertThat(progress.TotalProperties == caller.ScreenableProperties.Length);
            AssertThat(progress.MaxPatience == 30f);
        }

        [Test]
        public void Reject_NoSession_ReturnsFailure()
        {
            var result = _controller.Reject();

            AssertThat(result.IsFailure);
            AssertThat(result.ErrorCode == "NO_SESSION");
        }

        [Test]
        public void IsActive_AfterStart_ReturnsTrue()
        {
            var caller = CreateTestCaller();
            _controller.Start(caller);

            AssertThat(_controller.IsActive);
        }

        [Test]
        public void IsActive_AfterPhaseChange_ReturnsCorrectValue()
        {
            var caller = CreateTestCaller();
            _controller.Start(caller);

            AssertThat(_controller.IsActive);
            AssertThat(_controller.Phase == ScreeningPhase.Gathering);
        }

        [Test]
        public void CurrentCaller_ReturnsCorrectCaller()
        {
            var caller = CreateTestCaller();
            _controller.Start(caller);

            AssertThat(_controller.CurrentCaller == caller);
        }

        [Test]
        public void CurrentCaller_NoSession_ReturnsNull()
        {
            AssertThat(_controller.CurrentCaller == null);
        }

        [Test]
        public void Approve_HoldQueueFull_PreservesSessionForRetry()
        {
            var controller = new ScreeningController(
                new MockApproveFailingRepository(),
                new TopicManager(),
                _saveManager,
                _mockGameStateManager);
            var caller = CreateTestCaller();
            controller.Start(caller);

            var result = controller.Approve();

            AssertThat(result.IsFailure);
            AssertThat(result.ErrorCode == "HOLD_QUEUE_FULL");
            AssertThat(controller.CurrentCaller == caller);
            AssertThat(controller.IsActive);
            AssertThat(controller.Phase == ScreeningPhase.Gathering);
        }

        [Test]
        public void Approve_HoldQueueFull_RejectStillWorks()
        {
            var controller = new ScreeningController(
                new MockApproveFailingRepository(),
                new TopicManager(),
                _saveManager,
                _mockGameStateManager);
            var caller = CreateTestCaller();
            controller.Start(caller);

            controller.Approve();

            var reject = controller.Reject();

            AssertThat(reject.IsSuccess);
            AssertThat(controller.CurrentCaller == null);
            AssertThat(controller.Phase == ScreeningPhase.Completed);
        }
    }

    public class MockApproveFailingRepository : ICallerRepository
    {
        public IReadOnlyList<Caller> IncomingCallers => new List<Caller>();
        public IReadOnlyList<Caller> OnHoldCallers => new List<Caller>();
        public Caller? CurrentScreening => null;
        public Caller? OnAirCaller => null;

        public bool HasIncomingCallers => false;
        public bool HasOnHoldCallers => false;
        public bool IsScreening => true;
        public bool IsOnAir => false;
        public bool CanAcceptMoreCallers => true;
        public bool CanPutOnHold => false;

        public Result<Caller> AddCaller(Caller caller) => Result<Caller>.Ok(caller);
        public Result<Caller> StartScreening(Caller caller) => Result<Caller>.Ok(caller);
        public Result<Caller> StartScreeningNext() => Result<Caller>.Fail("No callers");
        public Result<Caller> ApproveScreening() => Result<Caller>.Fail("On-hold queue is full", "HOLD_QUEUE_FULL");
        public Result<Caller> RejectScreening() => Result<Caller>.Ok(null!);
        public Result<Caller> PutOnAir() => Result<Caller>.Fail("No caller");
        public Result<Caller> EndOnAir() => Result<Caller>.Fail("No caller on air");

        public bool SetCallerState(Caller caller, CallerState newState) => true;
        public bool RemoveCaller(Caller caller) => true;
        public void ClearAll() { }
        public Caller? GetCaller(string callerId) => null;

        public void Subscribe(ICallerRepositoryObserver observer) { }
        public void Unsubscribe(ICallerRepositoryObserver observer) { }
    }
}
