using System.Collections.Generic;
using System.Linq;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Screening;
using KBTV.Dialogue;

namespace KBTV.Tests.Integration
{
    public class ScreeningEventPublishingIntegrationTests : KBTVTestClass
    {
        public ScreeningEventPublishingIntegrationTests(Node testScene) : base(testScene) { }

        private CallerRepository _repository = null!;
        private ScreeningController _controller = null!;
        private List<string> _eventLog = null!;
        private MockArcRepository _mockArcRepository = null!;
        private MockCallerRepository _mockCallerRepositoryForController = null!;

        [Setup]
        public void Setup()
        {
            _mockArcRepository = new MockArcRepository();
            _repository = new CallerRepository(_mockArcRepository);
            
            _mockCallerRepositoryForController = new MockCallerRepository();
            _controller = new ScreeningController(_mockCallerRepositoryForController);
            
            _eventLog = new List<string>();

            _repository.Subscribe(new TestCallerRepositoryObserver(_eventLog));
        }

        [Test]
        public void Start_TriggersScreeningStartedObserver()
        {
            var caller = CreateTestCaller("TestCaller");
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);

            _controller.Start(caller);

            AssertThat(_eventLog.Contains("ScreeningStarted:TestCaller"));
        }

        [Test]
        public void Approve_TriggersScreeningEndedAndApprovedObservers()
        {
            var caller = CreateTestCaller("ApproveCaller");
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);

            _controller.Start(caller);
            _controller.Approve();

            AssertThat(_eventLog.Contains("ScreeningEnded:ApproveCaller"));
            AssertThat(_eventLog.Contains("ScreeningApproved:ApproveCaller"));
        }

        [Test]
        public void Reject_TriggersScreeningEndedAndRejectedObservers()
        {
            var caller = CreateTestCaller("RejectCaller");
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);

            _controller.Start(caller);
            _controller.Reject();

            AssertThat(_eventLog.Contains("ScreeningEnded:RejectCaller"));
            AssertThat(_eventLog.Contains("ScreeningRejected:RejectCaller"));
        }

        [Test]
        public void Update_TriggersProgressUpdatedEvent()
        {
            var caller = CreateTestCaller("ProgressCaller");
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);

            _controller.Start(caller);

            var initialCount = _eventLog.Count(e => e.StartsWith("Progress:"));
            _controller.Update(1f);
            var afterUpdateCount = _eventLog.Count(e => e.StartsWith("Progress:"));

            AssertThat(afterUpdateCount > initialCount);
        }

        [Test]
        public void PatienceExpired_TriggersScreeningEndedObserver()
        {
            var caller = CreateTestCaller("PatienceCaller");
            caller = new Caller(
                caller.Name, caller.PhoneNumber, caller.Location,
                caller.ClaimedTopic, caller.ActualTopic, caller.CallReason,
                caller.Legitimacy, caller.PhoneQuality, caller.EmotionalState,
                caller.CurseRisk, caller.BeliefLevel, caller.EvidenceLevel,
                caller.Coherence, caller.Urgency, caller.Personality,
                null, null, caller.ScreeningSummary, 1f, 0.8f
            );
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);

            _controller.Start(caller);
            _controller.Update(2f);

            AssertThat(_eventLog.Contains("ScreeningEnded:PatienceCaller"));
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

        private class TestCallerRepositoryObserver : ICallerRepositoryObserver
        {
            private readonly List<string> _eventLog;

            public TestCallerRepositoryObserver(List<string> eventLog)
            {
                _eventLog = eventLog;
            }

            public void OnCallerAdded(Caller caller) =>
                _eventLog.Add($"RepositoryCallerAdded:{caller.Name}");

            public void OnCallerRemoved(Caller caller) =>
                _eventLog.Add($"RepositoryCallerRemoved:{caller.Name}");

            public void OnCallerStateChanged(Caller caller, CallerState oldState, CallerState newState) =>
                _eventLog.Add($"RepositoryStateChanged:{caller.Name}:{oldState}->{newState}");

            public void OnScreeningStarted(Caller caller) =>
                _eventLog.Add($"ScreeningStarted:{caller?.Name ?? "null"}");

            public void OnScreeningEnded(Caller caller, bool approved)
            {
                _eventLog.Add($"ScreeningEnded:{caller?.Name ?? "null"}");
                if (approved)
                {
                    _eventLog.Add($"ScreeningApproved:{caller?.Name ?? "null"}");
                }
                else
                {
                    _eventLog.Add($"ScreeningRejected:{caller?.Name ?? "null"}");
                }
            }

            public void OnCallerOnAir(Caller caller) =>
                _eventLog.Add($"OnAir:{caller?.Name ?? "null"}");

            public void OnCallerOnAirEnded(Caller caller) =>
                _eventLog.Add($"OnAirEnded:{caller?.Name ?? "null"}");
        }
    }
}
