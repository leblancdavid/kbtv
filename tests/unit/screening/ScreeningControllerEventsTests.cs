using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Screening;
using KBTV.Managers;

namespace KBTV.Tests.Unit.Screening
{
    public class ScreeningControllerEventsTests : KBTVTestClass
    {
        public ScreeningControllerEventsTests(Node testScene) : base(testScene) { }

        private ScreeningController _controller = null!;
        private MockCallerRepository _mockRepository = null!;
        private List<(ScreeningPhase phase, int callOrder)> _phaseChanges = null!;
        private List<(ScreeningProgress progress, int callOrder)> _progressUpdates = null!;
        private int _callOrder = 0;

        [Setup]
        public void Setup()
        {
            _mockRepository = new MockCallerRepository();
            _controller = new ScreeningController(_mockRepository, new TopicManager());
            _phaseChanges = new List<(ScreeningPhase, int)>();
            _progressUpdates = new List<(ScreeningProgress, int)>();

            _controller.PhaseChanged += phase => {
                _phaseChanges.Add((phase, _callOrder++));
            };
            _controller.ProgressUpdated += progress => {
                _progressUpdates.Add((progress, _callOrder++));
            };
        }

        [Test]
        public void Start_EmitsPhaseChangedEvent()
        {
            var caller = CreateTestCaller();

            _controller.Start(caller);

            AssertThat(_phaseChanges.Count == 1);
            AssertThat(_phaseChanges[0].phase == ScreeningPhase.Gathering);
        }

        [Test]
        public void Approve_FailsWithoutRepository()
        {
            var caller = CreateTestCaller();
            _controller.Start(caller);

            var result = _controller.Approve();

            AssertThat(!result.IsSuccess);
            AssertThat(result.ErrorCode == "NO_REPOSITORY" || result.ErrorCode == "NO_SCREENING");
        }

        [Test]
        public void Reject_FailsWithoutRepository()
        {
            var caller = CreateTestCaller();
            _controller.Start(caller);

            var result = _controller.Reject();

            AssertThat(!result.IsSuccess);
            AssertThat(result.ErrorCode == "NO_REPOSITORY" || result.ErrorCode == "NO_SCREENING");
        }

        [Test]
        public void Update_EmitsProgressUpdatedEvent()
        {
            var caller = CreateTestCaller();
            _controller.Start(caller);
            _progressUpdates.Clear();

            _controller.Update(1f);

            AssertThat(_progressUpdates.Count >= 1);
            AssertThat(_progressUpdates[0].progress.ElapsedTime >= 0f);
        }

        [Test]
        public void MultipleUpdates_EmitMultipleProgressEvents()
        {
            var caller = CreateTestCaller();
            _controller.Start(caller);
            _progressUpdates.Clear();

            _controller.Update(1f);
            _controller.Update(1f);
            _controller.Update(1f);

            AssertThat(_progressUpdates.Count == 3);
        }

        [Test]
        public void PhaseChanged_CalledInCorrectOrder_BeforeProgressUpdated()
        {
            var caller = CreateTestCaller(patience: 30f);
            _controller.Start(caller);
            _controller.Update(0.1f); // Progress is only emitted on Update()

            // Start emits PhaseChanged, Update emits ProgressUpdated
            AssertThat(_phaseChanges.Count >= 1);
            AssertThat(_progressUpdates.Count >= 1);
            AssertThat(_phaseChanges[0].callOrder < _progressUpdates[0].callOrder);
        }

        [Test]
        public void Start_DoesNotEmitProgressUpdated()
        {
            var caller = CreateTestCaller();

            _controller.Start(caller);

            AssertThat(_progressUpdates.Count == 0);
        }

        [Test]
        public void Update_InactiveSession_DoesNotEmitProgressUpdated()
        {
            _controller.Update(1f);

            AssertThat(_progressUpdates.Count == 0);
        }

        [Test]
        public void PatienceExpired_EmitsPhaseChangedEvent()
        {
            var caller = CreateTestCaller(patience: 1f);
            _controller.Start(caller);
            _phaseChanges.Clear();

            // Force patience to be exhausted
            _controller.Update(2f);

            AssertThat(_phaseChanges.Count >= 1);
            if (_phaseChanges.Count > 0)
            {
                AssertThat(_phaseChanges[_phaseChanges.Count - 1].phase == ScreeningPhase.Completed);
            }
        }

        [Test]
        public void ProgressUpdated_ContainsCorrectProperties()
        {
            var caller = CreateTestCaller(patience: 30f);
            _controller.Start(caller);

            var progress = _controller.Progress;

            AssertThat(progress.PropertiesRevealed >= 0);
            AssertThat(progress.TotalProperties == caller.ScreenableProperties.Length);
            AssertThat(progress.MaxPatience == 30f);
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
                CallerBeliefLevel.Curious,
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
    }

    // Mock implementation for unit tests
    public class MockCallerRepository : ICallerRepository
    {
        public IReadOnlyList<Caller> IncomingCallers => new List<Caller>();
        public IReadOnlyList<Caller> OnHoldCallers => new List<Caller>();
        public Caller? CurrentScreening => null;
        public Caller? OnAirCaller => null;

        public bool HasIncomingCallers => false;
        public bool HasOnHoldCallers => false;
        public bool IsScreening => false;
        public bool IsOnAir => false;
        public bool CanAcceptMoreCallers => true;
        public bool CanPutOnHold => true;

        public Result<Caller> AddCaller(Caller caller) => Result<Caller>.Ok(caller);
        public Result<Caller> StartScreening(Caller caller) => Result<Caller>.Ok(caller);
        public Result<Caller> StartScreeningNext() => Result<Caller>.Fail("No callers");
        public Result<Caller> ApproveScreening() => Result<Caller>.Fail("No screening");
        public Result<Caller> RejectScreening() => Result<Caller>.Fail("No screening");
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
