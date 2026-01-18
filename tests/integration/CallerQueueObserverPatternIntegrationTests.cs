using System.Collections.Generic;
using System.Linq;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Core;

namespace KBTV.Tests.Integration
{
    public class CallerQueueObserverPatternIntegrationTests : KBTVTestClass
    {
        public CallerQueueObserverPatternIntegrationTests(Node testScene) : base(testScene) { }

        private CallerRepository _repository = null!;
        private TestObserver _observer = null!;

        [Setup]
        public void Setup()
        {
            _repository = new CallerRepository();
            _observer = new TestObserver();
            _repository.Subscribe(_observer);
        }

        [Test]
        public void Observer_ReceivesOnCallerAdded()
        {
            var caller = CreateTestCaller("AddedCaller");

            _repository.AddCaller(caller);

            AssertThat(_observer.AddedCallers.Contains(caller));
        }

        [Test]
        public void Observer_ReceivesOnCallerRemoved()
        {
            var caller = CreateTestCaller("RemovedCaller");
            _repository.AddCaller(caller);

            _repository.RemoveCaller(caller);

            AssertThat(_observer.RemovedCallers.Contains(caller));
        }

        [Test]
        public void Observer_ReceivesOnCallerStateChanged_OnScreening()
        {
            var caller = CreateTestCaller("ScreeningCaller");
            _repository.AddCaller(caller);

            _repository.StartScreening(caller);

            AssertThat(_observer.StateChanges.Any(sc =>
                sc.caller == caller &&
                sc.oldState == CallerState.Incoming &&
                sc.newState == CallerState.Screening));
        }

        [Test]
        public void Observer_ReceivesOnCallerStateChanged_OnApproved()
        {
            var caller = CreateTestCaller("ApprovedCaller");
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);

            _repository.ApproveScreening();

            AssertThat(_observer.StateChanges.Any(sc =>
                sc.caller == caller &&
                sc.newState == CallerState.OnHold));
        }

        [Test]
        public void Observer_ReceivesOnCallerStateChanged_OnRejected()
        {
            var caller = CreateTestCaller("RejectedCaller");
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);

            _repository.RejectScreening();

            AssertThat(_observer.RemovedCallers.Contains(caller));
        }

        [Test]
        public void Observer_ReceivesOnCallerStateChanged_OnPutOnAir()
        {
            var caller = CreateTestCaller("OnAirCaller");
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);
            _repository.ApproveScreening();

            _repository.PutOnAir();

            AssertThat(_observer.StateChanges.Any(sc =>
                sc.caller == caller &&
                sc.newState == CallerState.OnAir));
        }

        [Test]
        public void Observer_ReceivesOnCallerStateChanged_OnEndOnAir()
        {
            var caller = CreateTestCaller("EndAirCaller");
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);
            _repository.ApproveScreening();
            _repository.PutOnAir();

            _repository.EndOnAir();

            AssertThat(_observer.StateChanges.Any(sc =>
                sc.caller == caller &&
                sc.newState == CallerState.Completed));
        }

        [Test]
        public void Observer_DoesNotReceiveAfterUnsubscribe()
        {
            var caller = CreateTestCaller("UnsubscribedCaller");
            _repository.Unsubscribe(_observer);
            _observer.AddedCallers.Clear();

            _repository.AddCaller(caller);

            AssertThat(_observer.AddedCallers.Count == 0);
        }

        [Test]
        public void MultipleObservers_AllReceiveEvents()
        {
            var observer2 = new TestObserver();
            _repository.Subscribe(observer2);

            var caller = CreateTestCaller("MultiCaller");

            _repository.AddCaller(caller);

            AssertThat(_observer.AddedCallers.Contains(caller));
            AssertThat(observer2.AddedCallers.Contains(caller));
        }

        private Caller CreateTestCaller(string name)
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
                "summary",
                30f,
                0.8f
            );
        }

        private class TestObserver : ICallerRepositoryObserver
        {
            public List<Caller> AddedCallers = new();
            public List<Caller> RemovedCallers = new();
            public List<(Caller caller, CallerState oldState, CallerState newState)> StateChanges = new();
            public List<Caller> ScreeningStartedCallers = new();
            public List<(Caller caller, bool approved)> ScreeningEndedCallers = new();
            public List<Caller> OnAirCallers = new();
            public List<Caller> OnAirEndedCallers = new();

            public void OnCallerAdded(Caller caller) => AddedCallers.Add(caller);
            public void OnCallerRemoved(Caller caller) => RemovedCallers.Add(caller);
            public void OnCallerStateChanged(Caller caller, CallerState oldState, CallerState newState)
                => StateChanges.Add((caller, oldState, newState));
            public void OnScreeningStarted(Caller caller) => ScreeningStartedCallers.Add(caller);
            public void OnScreeningEnded(Caller caller, bool approved) => ScreeningEndedCallers.Add((caller, approved));
            public void OnCallerOnAir(Caller caller) => OnAirCallers.Add(caller);
            public void OnCallerOnAirEnded(Caller caller) => OnAirEndedCallers.Add(caller);
        }
    }
}
