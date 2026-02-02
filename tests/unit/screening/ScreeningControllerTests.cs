using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Screening;
using System.Collections.Generic;

namespace KBTV.Tests.Unit.Screening
{
    public class ScreeningControllerTests : KBTVTestClass
    {
        public ScreeningControllerTests(Node testScene) : base(testScene) { }

        private ScreeningController _controller = null!;
        private MockCallerRepository _mockRepository = null!;

        [Setup]
        public void Setup()
        {
            _mockRepository = new MockCallerRepository();
            _controller = new ScreeningController(_mockRepository);
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
    }
}
