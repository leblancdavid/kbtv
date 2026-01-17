using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Screening;

namespace KBTV.Tests.Unit.Screening
{
    public class ScreeningControllerEventsTests : KBTVTestClass
    {
        public ScreeningControllerEventsTests(Node testScene) : base(testScene) { }

        private ScreeningController _controller = null!;
        private List<(ScreeningPhase phase, int callOrder)> _phaseChanges = null!;
        private List<(ScreeningProgress progress, int callOrder)> _progressUpdates = null!;
        private int _callOrder = 0;

        [Setup]
        public void Setup()
        {
            _controller = new ScreeningController();
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
        public void Approve_EmitsPhaseChangedEvent()
        {
            var caller = CreateTestCaller();
            _controller.Start(caller);
            _phaseChanges.Clear();

            _controller.Approve();

            AssertThat(_phaseChanges.Count == 1);
            AssertThat(_phaseChanges[0].phase == ScreeningPhase.Completed);
        }

        [Test]
        public void Reject_EmitsPhaseChangedEvent()
        {
            var caller = CreateTestCaller();
            _controller.Start(caller);
            _phaseChanges.Clear();

            _controller.Reject();

            AssertThat(_phaseChanges.Count == 1);
            AssertThat(_phaseChanges[0].phase == ScreeningPhase.Completed);
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
            var caller = CreateTestCaller();
            _controller.Start(caller);

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

            _controller.Update(2f);

            AssertThat(_phaseChanges.Count == 1);
            AssertThat(_phaseChanges[0].phase == ScreeningPhase.Completed);
        }

        [Test]
        public void ProgressUpdated_ContainsCorrectProperties()
        {
            var caller = CreateTestCaller(patience: 30f);
            _controller.Start(caller);

            var progress = _controller.Progress;

            AssertThat(progress.PropertiesRevealed >= 0);
            AssertThat(progress.TotalProperties == caller.Revelations.Length);
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
                "arc",
                "summary",
                patience,
                0.8f
            );
        }
    }
}
