using System.Collections.Generic;
using System.Linq;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Screening;

namespace KBTV.Tests.Integration
{
    public class CallerFlowIntegrationTests : KBTVTestClass
    {
        public CallerFlowIntegrationTests(Node testScene) : base(testScene) { }

        private CallerRepository _repository = null!;
        private ScreeningController _controller = null!;
        private List<string> _eventLog = null!;
        private readonly object _eventSubscriber = new();

        [Setup]
        public void Setup()
        {
            _repository = new CallerRepository();
            _controller = new ScreeningController();
            _eventLog = new List<string>();

            ServiceRegistry.Instance.EventAggregator.Subscribe(_eventSubscriber, (Core.Events.Queue.CallerAdded evt) => _eventLog.Add($"CallerAdded: {evt.Caller.Name}"));
            ServiceRegistry.Instance.EventAggregator.Subscribe(_eventSubscriber, (Core.Events.Queue.CallerRemoved evt) => _eventLog.Add($"CallerRemoved: {evt.Caller.Name}"));
            ServiceRegistry.Instance.EventAggregator.Subscribe(_eventSubscriber, (Core.Events.Screening.ScreeningStarted evt) => _eventLog.Add($"ScreeningStarted: {evt.Caller.Name}"));
            ServiceRegistry.Instance.EventAggregator.Subscribe(_eventSubscriber, (Core.Events.Screening.ScreeningApproved evt) => _eventLog.Add($"ScreeningApproved: {evt.Caller.Name}"));
            ServiceRegistry.Instance.EventAggregator.Subscribe(_eventSubscriber, (Core.Events.Screening.ScreeningRejected evt) => _eventLog.Add($"ScreeningRejected: {evt.Caller.Name}"));
        }

        [Test]
        public void FullFlow_AddCaller_Approve_PutOnAir_EndCall()
        {
            var caller = CreateTestCaller("Alice");

            var addResult = _repository.AddCaller(caller);
            AssertThat(addResult.IsSuccess);
            AssertThat(_repository.HasIncomingCallers);

            var startResult = _repository.StartScreeningNext();
            AssertThat(startResult.IsSuccess);
            AssertThat(_repository.IsScreening);

            var approveResult = _repository.ApproveScreening();
            AssertThat(approveResult.IsSuccess);
            AssertThat(_repository.HasOnHoldCallers);
            AssertThat(!_repository.IsScreening);

            var onAirResult = _repository.PutOnAir();
            AssertThat(onAirResult.IsSuccess);
            AssertThat(_repository.IsOnAir);
            AssertThat(_repository.OnAirCaller == caller);

            var endResult = _repository.EndOnAir();
            AssertThat(endResult.IsSuccess);
            AssertThat(!_repository.IsOnAir);

            AssertThat(_eventLog.Contains("CallerAdded: Alice"));
            AssertThat(_eventLog.Contains("ScreeningApproved: Alice"));
        }

        [Test]
        public void FullFlow_AddCaller_Reject_RemovesCaller()
        {
            var caller = CreateTestCaller("Bob");

            _repository.AddCaller(caller);
            _repository.StartScreeningNext();

            var rejectResult = _repository.RejectScreening();
            AssertThat(rejectResult.IsSuccess);
            AssertThat(_repository.IncomingCallers.Count == 0);

            AssertThat(_eventLog.Contains("CallerAdded: Bob"));
            AssertThat(_eventLog.Contains("ScreeningRejected: Bob"));
        }

        [Test]
        public void MultipleCallers_QueuedInOrder()
        {
            var caller1 = CreateTestCaller("Caller1");
            var caller2 = CreateTestCaller("Caller2");
            var caller3 = CreateTestCaller("Caller3");

            _repository.AddCaller(caller1);
            _repository.AddCaller(caller2);
            _repository.AddCaller(caller3);

            AssertThat(_repository.IncomingCallers.Count == 3);
            AssertThat(_repository.HasIncomingCallers);

            var firstScreening = _repository.StartScreeningNext();
            AssertThat(firstScreening.IsSuccess);

            _repository.ApproveScreening();

            var secondScreening = _repository.StartScreeningNext();
            AssertThat(secondScreening.IsSuccess);

            _repository.ApproveScreening();

            var thirdScreening = _repository.StartScreeningNext();
            AssertThat(thirdScreening.IsSuccess);

            AssertThat(_repository.IncomingCallers.Count == 0);
        }

        [Test]
        public void ScreeningController_Integration()
        {
            var caller = CreateTestCaller("Charlie");
            _repository.AddCaller(caller);

            var startScreening = _repository.StartScreening(caller);
            AssertThat(startScreening.IsSuccess);

            _controller.Start(caller);
            AssertThat(_controller.IsActive);
            AssertThat(_controller.CurrentCaller == caller);

            for (int i = 0; i < 10; i++)
            {
                _controller.Update(1f);
            }

            AssertThat(_controller.Progress.ElapsedTime > 0);
        }

        [Test]
        public void ScreeningPatience_DepletesOverTime()
        {
            var caller = CreateTestCaller("David");
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);

            _controller.Start(caller);
            var initialPatience = _controller.Progress.PatienceRemaining;

            _controller.Update(10f);

            AssertThat(_controller.Progress.PatienceRemaining < initialPatience);
        }

        [Test]
        public void StateTransitions_EmitEvents()
        {
            var caller = CreateTestCaller("Frank");

            _repository.AddCaller(caller);
            AssertThat(_eventLog.Contains("CallerAdded: Frank"));

            _repository.StartScreeningNext();
            AssertThat(_eventLog.Contains("ScreeningStarted: Frank"));

            _repository.ApproveScreening();
            AssertThat(_eventLog.Contains("ScreeningApproved: Frank"));

            _repository.PutOnAir();

            _repository.EndOnAir();
        }

        [Test]
        public void Repository_ClearAll_RemovesCallers()
        {
            for (int i = 0; i < 5; i++)
            {
                _repository.AddCaller(CreateTestCaller($"Caller{i}"));
            }

            AssertThat(_repository.IncomingCallers.Count == 5);

            _repository.ClearAll();

            AssertThat(_repository.IncomingCallers.Count == 0);
            AssertThat(_repository.OnHoldCallers.Count == 0);
            AssertThat(_repository.CurrentScreening == null);
            AssertThat(_repository.OnAirCaller == null);
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
                "arc",
                "summary",
                30f,
                0.8f
            );
        }
    }
}
