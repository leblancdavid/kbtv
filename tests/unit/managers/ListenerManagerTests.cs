using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Data;
using KBTV.Managers;
using System.Collections.Generic;
using System;

namespace KBTV.Tests.Unit.Managers
{
    public class ListenerManagerTests : KBTVTestClass
    {
        public ListenerManagerTests(Node testScene) : base(testScene) { }

        private ListenerManager _listenerManager = null!;
        private MockGameStateManager _mockGameStateManager = null!;
        private MockTimeManager _mockTimeManager = null!;
        private MockCallerRepository _mockCallerRepository = null!;

        [Setup]
        public void Setup()
        {
            _mockGameStateManager = new MockGameStateManager();
            _mockTimeManager = new MockTimeManager();
            _mockCallerRepository = new MockCallerRepository();
            _listenerManager = new ListenerManager(_mockGameStateManager, _mockTimeManager, _mockCallerRepository);
            _listenerManager._Ready();
        }

        [Test]
        public void Constructor_InitializesCurrentListenersToZero()
        {
            AssertThat(_listenerManager.CurrentListeners == 0);
        }

        [Test]
        public void Constructor_InitializesPeakListenersToZero()
        {
            AssertThat(_listenerManager.PeakListeners == 0);
        }

        [Test]
        public void Constructor_InitializesStartingListenersToZero()
        {
            AssertThat(_listenerManager.StartingListeners == 0);
        }

        [Test]
        public void ModifyListeners_PositiveValue_IncreasesListeners()
        {
            int initial = _listenerManager.CurrentListeners;

            _listenerManager.ModifyListeners(100);

            AssertThat(_listenerManager.CurrentListeners > initial);
        }

        [Test]
        public void ModifyListeners_NegativeValue_DecreasesListeners()
        {
            _listenerManager.ModifyListeners(500);
            int initial = _listenerManager.CurrentListeners;

            _listenerManager.ModifyListeners(-100);

            AssertThat(_listenerManager.CurrentListeners < initial);
        }

        [Test]
        public void ModifyListeners_AtMinLimit_ClampsToMin()
        {
            for (int i = 0; i < 50; i++)
            {
                _listenerManager.ModifyListeners(-100);
            }

            AssertThat(_listenerManager.CurrentListeners >= 100);
        }

        [Test]
        public void ModifyListeners_AtMaxLimit_ClampsToMax()
        {
            for (int i = 0; i < 50; i++)
            {
                _listenerManager.ModifyListeners(100);
            }

            AssertThat(_listenerManager.CurrentListeners <= 3000);
        }

        [Test]
        public void ModifyListeners_UpdatesPeakIfExceeded()
        {
            _listenerManager.ModifyListeners(1000);
            int current = _listenerManager.CurrentListeners;

            _listenerManager.ModifyListeners(100);

            AssertThat(_listenerManager.PeakListeners >= current);
        }

        [Test]
        public void ModifyListeners_DoesNotUpdatePeakIfNotExceeded()
        {
            _listenerManager.ModifyListeners(1000);
            int peak = _listenerManager.PeakListeners;

            _listenerManager.ModifyListeners(-50);
            _listenerManager.ModifyListeners(25);

            AssertThat(_listenerManager.PeakListeners == peak);
        }

        [Test]
        public void ListenerChange_StartsAtZero()
        {
            AssertThat(_listenerManager.ListenerChange == 0);
        }

        [Test]
        public void ListenerChange_ReflectsNetChange()
        {
            _listenerManager.ModifyListeners(100);

            AssertThat(_listenerManager.ListenerChange > 0);
        }

        [Test]
        public void GetFormattedListeners_WithThousands_FormatsAsK()
        {
            _listenerManager.ModifyListeners(15000);
            string formatted = _listenerManager.GetFormattedListeners();

            AssertThat(formatted == "15.0K");
        }

        [Test]
        public void GetFormattedListeners_WithMillions_FormatsAsM()
        {
            _listenerManager.ModifyListeners(1500000);
            string formatted = _listenerManager.GetFormattedListeners();

            AssertThat(formatted == "1.5M");
        }

        [Test]
        public void GetFormattedListeners_WithSmallNumbers_FormatsWithCommas()
        {
            _listenerManager.ModifyListeners(500);
            string formatted = _listenerManager.GetFormattedListeners();

            AssertThat(!string.IsNullOrEmpty(formatted));
        }

        [Test]
        public void GetFormattedChange_PositiveChange_ShowsPlusSign()
        {
            _listenerManager.ModifyListeners(100);
            string formatted = _listenerManager.GetFormattedChange();

            AssertThat(formatted.StartsWith("+"));
        }

        [Test]
        public void GetFormattedChange_NegativeChange_ShowsMinusSign()
        {
            _listenerManager.ModifyListeners(-500);
            string formatted = _listenerManager.GetFormattedChange();

            AssertThat(formatted.StartsWith("-"));
        }

        [Test]
        public void GetFormattedListeners_NegativeListeners_FormatsCorrectly()
        {
            _listenerManager.ModifyListeners(-1000);
            string formatted = _listenerManager.GetFormattedListeners();

            AssertThat(formatted == "-1,000");
        }

        [Test]
        public void ModifyListeners_ExcessiveNegative_ClampsToMinimum()
        {
            _listenerManager.ModifyListeners(500);
            int beforeClamp = _listenerManager.CurrentListeners;

            _listenerManager.ModifyListeners(-600);

            AssertThat(_listenerManager.CurrentListeners <= beforeClamp);
            AssertThat(_listenerManager.CurrentListeners >= 100);
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

    public class MockTimeManager : ITimeManager
    {
        public float ElapsedTime { get; set; } = 0f;
        public float ShowDuration { get; set; } = 600f;
        public float Progress { get; set; } = 0f;
        public bool IsRunning { get; set; } = false;
        public string CurrentTimeFormatted { get; set; } = "00:00";
        public float CurrentHour { get; set; } = 0f;
        public float RemainingTime { get; set; } = 600f;
        public string RemainingTimeFormatted { get; set; } = "10:00";

        public event Action<float> OnTick = delegate { };
        public event Action OnShowEnded = delegate { };
        public event Action<float> OnShowEndingWarning = delegate { };
        public event Action<bool> OnRunningChanged = delegate { };

        public void StartClock() { }
        public void PauseClock() { }
        public void ResetClock() { }
        public void EndShow() { }
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
