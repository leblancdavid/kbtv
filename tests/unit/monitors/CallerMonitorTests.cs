using System.Collections.Generic;
using System.Linq;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Monitors;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Monitors
{
    public class CallerMonitorTests : KBTVTestClass
    {
        public CallerMonitorTests(Node testScene) : base(testScene) { }

        private CallerRepository _repository = null!;
        private MockArcRepository _mockArcRepository = null!;
        private List<string> _eventLog = null!;

        [Setup]
        public void Setup()
        {
            _mockArcRepository = new MockArcRepository();
            _repository = new CallerRepository(_mockArcRepository);
            _eventLog = new List<string>();
            _repository.Subscribe(new TestCallerRepositoryObserver(_eventLog));
        }

        [Test]
        public void Monitor_DoesNotThrowWithNullRepository()
        {
            var monitor = new CallerMonitor();

            monitor._Ready();

            monitor._Process(0.016f);
        }

        [Test]
        public void Monitor_DoesNotThrowWithNoCallers()
        {
            var monitor = new CallerMonitor();
            monitor._Ready();

            monitor._Process(0.016f);

            AssertThat(_eventLog.Count == 0);
        }

        [Test]
        public void Monitor_DoesNotThrowWithCallers()
        {
            var monitor = new CallerMonitor();
            monitor._Ready();

            var caller = CreateTestCaller();
            _repository.AddCaller(caller);

            monitor._Process(0.016f);

            AssertThat(caller.WaitTime >= 0f);
        }

        [Test]
        public void Monitor_UpdatesCallerWaitTime()
        {
            var monitor = new CallerMonitor();
            monitor._Ready();

            var caller = CreateTestCaller();
            _repository.AddCaller(caller);

            AssertThat(caller.WaitTime == 0f);

            monitor._Process(1.0f);

            AssertThat(caller.WaitTime == 1.0f);
        }

        [Test]
        public void Monitor_UpdatesScreeningCallerPatience()
        {
            var monitor = new CallerMonitor();
            monitor._Ready();

            var caller = CreateTestCaller();
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);

            var initialPatience = caller.ScreeningPatience;

            monitor._Process(2.0f);

            AssertThat(caller.ScreeningPatience < initialPatience);
        }

        [Test]
        public void Monitor_MultipleCallers_UpdatesAll()
        {
            var monitor = new CallerMonitor();
            monitor._Ready();

            var caller1 = CreateTestCaller("Caller1");
            var caller2 = CreateTestCaller("Caller2");
            _repository.AddCaller(caller1);
            _repository.AddCaller(caller2);

            monitor._Process(1.0f);

            AssertThat(caller1.WaitTime == 1.0f);
            AssertThat(caller2.WaitTime == 1.0f);
        }

        [Test]
        public void Monitor_CallerDisconnectsWhenPatienceRunsOut()
        {
            var monitor = new CallerMonitor();
            monitor._Ready();

            var caller = CreateTestCaller("Test", patience: 2f);
            _repository.AddCaller(caller);

            AssertThat(caller.State == CallerState.Incoming);
            AssertThat(!_eventLog.Contains("CallerRemoved: Test"));

            for (int i = 0; i < 5; i++)
            {
                monitor._Process(1.0f);
            }

            AssertThat(_eventLog.Contains("CallerRemoved: Test"));
        }

        private Caller CreateTestCaller(string name = "Test Caller", float patience = 30f)
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
                patience,
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
                _eventLog.Add($"CallerAdded: {caller.Name}");

            public void OnCallerRemoved(Caller caller) =>
                _eventLog.Add($"CallerRemoved: {caller.Name}");

            public void OnCallerStateChanged(Caller caller, CallerState oldState, CallerState newState) =>
                _eventLog.Add($"StateChanged: {caller.Name}:{oldState}->{newState}");

            public void OnScreeningStarted(Caller caller) =>
                _eventLog.Add($"ScreeningStarted: {caller.Name}");

            public void OnScreeningEnded(Caller caller, bool approved)
            {
                _eventLog.Add($"ScreeningEnded: {caller.Name}");
                _eventLog.Add(approved ? $"ScreeningApproved: {caller.Name}" : $"ScreeningRejected: {caller.Name}");
            }

            public void OnCallerOnAir(Caller caller) =>
                _eventLog.Add($"OnAir: {caller.Name}");

            public void OnCallerOnAirEnded(Caller caller) =>
                _eventLog.Add($"OnAirEnded: {caller.Name}");
        }
    }

    // Mock implementation for unit tests
    public class MockArcRepository : IArcRepository
    {
        public Godot.Collections.Array<ConversationArc> Arcs => new();

        public void Initialize() { }

        public List<ConversationArc> FindMatchingArcs(ShowTopic topic, CallerLegitimacy legitimacy) => new();

        public ConversationArc? GetRandomArc(CallerLegitimacy legitimacy) => null;

        public ConversationArc? GetRandomArcForTopic(ShowTopic topic, CallerLegitimacy legitimacy) => null;

        public ConversationArc? GetRandomArcForDifferentTopic(ShowTopic excludeTopic, CallerLegitimacy legitimacy) => null;

        public List<ConversationArc> FindTopicSwitcherArcs(ShowTopic claimedTopic, ShowTopic actualTopic, CallerLegitimacy legitimacy) => new();

        public ConversationArc? GetRandomTopicSwitcherArc(ShowTopic claimedTopic, ShowTopic actualTopic, CallerLegitimacy legitimacy) => null;

        public void AddArc(ConversationArc arc) { }

        public void Clear() { }
    }
}
