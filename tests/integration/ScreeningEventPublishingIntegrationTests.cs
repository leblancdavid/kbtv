using System.Collections.Generic;
using System.Linq;
using System;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Screening;
using KBTV.Dialogue;
using KBTV.Managers;
using KBTV.Persistence;
using KBTV.Data;

namespace KBTV.Tests.Integration
{
    public class ScreeningEventPublishingIntegrationTests : KBTVTestClass
    {
        public ScreeningEventPublishingIntegrationTests(Node testScene) : base(testScene) { }

        private CallerRepository _repository = null!;
        private ScreeningController _controller = null!;
        private SaveManager _saveManager = null!;
        private MockGameStateManager _mockGameStateManager = null!;
        private List<string> _eventLog = null!;
        private MockArcRepository _mockArcRepository = null!;
        private MockCallerRepository _mockCallerRepositoryForController = null!;

        [Setup]
        public void Setup()
        {
            _mockArcRepository = new MockArcRepository();
            _repository = new CallerRepository(_mockArcRepository);
            
            _mockCallerRepositoryForController = new MockCallerRepository();
            _saveManager = new SaveManager();
            _mockGameStateManager = new MockGameStateManager();
            _controller = new ScreeningController(_mockCallerRepositoryForController, new TopicManager(), _saveManager, _mockGameStateManager);
            
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
                null, null, null, caller.ScreeningSummary, 1f, 0.8f
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

        private class MockArcRepository : IArcRepository
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

        private class MockCallerRepository : ICallerRepository
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

        private class MockGameStateManager : IGameStateManager
        {
            public GamePhase CurrentPhase { get; set; } = GamePhase.PreShow;
            public int CurrentNight { get; set; } = 1;
            public Topic SelectedTopic { get; set; } = null!;
            public VernStats VernStats { get; set; } = new VernStats();
            public bool IsLive => CurrentPhase == GamePhase.LiveShow;

            public event Action<GamePhase, GamePhase> OnPhaseChanged = delegate { };
            public event Action<int> OnNightStarted = delegate { };

            public void InitializeGame() { }
            public void AdvancePhase() { }
            public void StartLiveShow() { }
            public void SetPhase(GamePhase phase) { CurrentPhase = phase; }
            public void SetSelectedTopic(Topic topic) { SelectedTopic = topic; }
            public bool CanStartLiveShow() => true;
            public void StartNewNight() { }
        }
    }
}
