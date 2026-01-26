using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Dialogue;
using KBTV.Data;
using System.Collections.Generic;
using System;

namespace KBTV.Tests.Unit.Callers
{
    public class CallerGeneratorTests : KBTVTestClass
    {
        public CallerGeneratorTests(Node testScene) : base(testScene) { }

        private CallerGenerator _generator = null!;
        private MockCallerRepository _mockCallerRepository = null!;
        private MockGameStateManager _mockGameStateManager = null!;
        private MockArcRepository _mockArcRepository = null!;

        [Setup]
        public void Setup()
        {
            _mockCallerRepository = new MockCallerRepository();
            _mockGameStateManager = new MockGameStateManager();
            _mockArcRepository = new MockArcRepository();
            _generator = new CallerGenerator(_mockCallerRepository, _mockGameStateManager, _mockArcRepository);
            _generator._Ready();
        }

        [Test]
        public void StartGenerating_SetsIsGeneratingToTrue()
        {
            _generator.StartGenerating();
            AssertThat(_generator.GetType().GetField("_isGenerating", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_generator) is true);
        }

        [Test]
        public void StopGenerating_SetsIsGeneratingToFalse()
        {
            _generator.StartGenerating();
            _generator.StopGenerating();
            AssertThat(_generator.GetType().GetField("_isGenerating", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_generator) is false);
        }

        [Test]
        public void GenerateTestCaller_HasValidName()
        {
            var caller = _generator.GenerateTestCaller();

            AssertThat(!string.IsNullOrEmpty(caller.Name));
        }

        [Test]
        public void GenerateTestCaller_HasValidPhoneNumber()
        {
            var caller = _generator.GenerateTestCaller();

            AssertThat(!string.IsNullOrEmpty(caller.PhoneNumber));
            AssertThat(caller.PhoneNumber.Contains("-"));
        }

        [Test]
        public void GenerateTestCaller_HasValidLocation()
        {
            var caller = _generator.GenerateTestCaller();

            AssertThat(!string.IsNullOrEmpty(caller.Location));
        }

        [Test]
        public void GenerateTestCaller_HasValidTopic()
        {
            var caller = _generator.GenerateTestCaller();

            AssertThat(!string.IsNullOrEmpty(caller.ClaimedTopic));
        }

        [Test]
        public void GenerateTestCaller_HasValidCallReason()
        {
            var caller = _generator.GenerateTestCaller();

            AssertThat(!string.IsNullOrEmpty(caller.CallReason));
        }

        [Test]
        public void GenerateTestCaller_HasPositivePatience()
        {
            var caller = _generator.GenerateTestCaller();

            AssertThat(caller.Patience > 0f);
        }

        [Test]
        public void GenerateTestCaller_HasValidLegitimacy()
        {
            var caller = _generator.GenerateTestCaller();

            AssertThat(caller.Legitimacy >= CallerLegitimacy.Fake);
            AssertThat(caller.Legitimacy <= CallerLegitimacy.Compelling);
        }

        [Test]
        public void GenerateTestCaller_HasUniqueId()
        {
            var caller1 = _generator.GenerateTestCaller();
            var caller2 = _generator.GenerateTestCaller();

            AssertThat(caller1.Id != caller2.Id);
        }

        [Test]
        public void GenerateTestCaller_InInitialState()
        {
            var caller = _generator.GenerateTestCaller();

            AssertThat(caller.State == CallerState.Incoming);
        }
    }

    // Mock implementations for unit tests
    public class MockGameStateManager : IGameStateManager
    {
        public GamePhase CurrentPhase { get; set; } = GamePhase.PreShow;
        public int CurrentNight { get; set; } = 1;
        public Topic SelectedTopic { get; set; } = null!;
        public VernStats VernStats { get; set; } = new VernStats();
        public bool IsLive => CurrentPhase == GamePhase.LiveShow;

        public event Action<GamePhase, GamePhase> OnPhaseChanged = delegate { };
        public event Action<int> OnNightStarted = delegate { };
        public event Action<int> NightStarted = delegate { };

        public void InitializeGame() { }
        public void AdvancePhase() { }
        public void StartLiveShow() { }
        public void SetPhase(GamePhase phase) { CurrentPhase = phase; }
        public void SetSelectedTopic(Topic topic) { SelectedTopic = topic; }
        public bool CanStartLiveShow() => true;
        public void StartNewNight() { }
    }

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
