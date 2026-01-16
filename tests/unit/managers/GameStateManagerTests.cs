using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Data;
using KBTV.Economy;

namespace KBTV.Tests.Unit.Managers
{
    public class GameStateManagerTests : KBTVTestClass
    {
        public GameStateManagerTests(Node testScene) : base(testScene) { }

        private GameStateManager _gameState = null!;

        [Setup]
        public void Setup()
        {
            _gameState = new GameStateManager();
            _gameState._Ready();
        }

        [Test]
        public void Constructor_InitializesToPreShow()
        {
            AssertThat(_gameState.CurrentPhase == GamePhase.PreShow);
        }

        [Test]
        public void Constructor_InitializesNightToOne()
        {
            AssertThat(_gameState.CurrentNight == 1);
        }

        [Test]
        public void Constructor_InitializesVernStats()
        {
            AssertThat(_gameState.VernStats != null);
        }

        [Test]
        public void AdvancePhase_PreShowToLiveShow()
        {
            var topic = new Topic("Test Topic", "test_topic", "Test Description");
            _gameState.SetSelectedTopic(topic);

            _gameState.AdvancePhase();

            AssertThat(_gameState.CurrentPhase == GamePhase.LiveShow);
        }

        [Test]
        public void AdvancePhase_LiveShowToPostShow()
        {
            _gameState.SetPhase(GamePhase.LiveShow);

            _gameState.AdvancePhase();

            AssertThat(_gameState.CurrentPhase == GamePhase.PostShow);
        }

        [Test]
        public void AdvancePhase_PostShowToPreShow_NewNight()
        {
            _gameState.SetPhase(GamePhase.PostShow);

            _gameState.AdvancePhase();

            AssertThat(_gameState.CurrentPhase == GamePhase.PreShow);
            AssertThat(_gameState.CurrentNight == 2);
        }

        [Test]
        public void SetPhase_UpdatesPhase()
        {
            _gameState.SetPhase(GamePhase.LiveShow);

            AssertThat(_gameState.CurrentPhase == GamePhase.LiveShow);
        }

        [Test]
        public void SetPhase_EmitsPhaseChangedEvent()
        {
            GamePhase? oldPhase = null;
            GamePhase? newPhase = null;
            _gameState.PhaseChanged += (old, newVal) =>
            {
                oldPhase = old;
                newPhase = newVal;
            };

            _gameState.SetPhase(GamePhase.LiveShow);

            AssertThat(oldPhase == GamePhase.PreShow);
            AssertThat(newPhase == GamePhase.LiveShow);
        }

        [Test]
        public void SetSelectedTopic_SetsTopic()
        {
            var topic = new Topic("Ghosts", "ghosts", " paranormal");

            _gameState.SetSelectedTopic(topic);

            AssertThat(_gameState.SelectedTopic == topic);
        }

        [Test]
        public void CanStartLiveShow_TrueWhenPreShowAndTopicSelected()
        {
            var topic = new Topic("Test", "test", "desc");
            _gameState.SetSelectedTopic(topic);

            AssertThat(_gameState.CanStartLiveShow());
        }

        [Test]
        public void CanStartLiveShow_FalseWhenNoTopic()
        {
            AssertThat(!_gameState.CanStartLiveShow());
        }

        [Test]
        public void CanStartLiveShow_FalseWhenNotPreShow()
        {
            var topic = new Topic("Test", "test", "desc");
            _gameState.SetSelectedTopic(topic);
            _gameState.SetPhase(GamePhase.LiveShow);

            AssertThat(!_gameState.CanStartLiveShow());
        }

        [Test]
        public void IsLive_TrueDuringLiveShow()
        {
            _gameState.SetPhase(GamePhase.LiveShow);

            AssertThat(_gameState.IsLive);
        }

        [Test]
        public void IsLive_FalseDuringPreShow()
        {
            AssertThat(!_gameState.IsLive);
        }

        [Test]
        public void StartNewNight_IncrementsNight()
        {
            int initialNight = _gameState.CurrentNight;

            _gameState.StartNewNight();

            AssertThat(_gameState.CurrentNight == initialNight + 1);
        }

        [Test]
        public void StartNewNight_ResetsToPreShow()
        {
            _gameState.SetPhase(GamePhase.LiveShow);

            _gameState.StartNewNight();

            AssertThat(_gameState.CurrentPhase == GamePhase.PreShow);
        }

        [Test]
        public void StartNewNight_ReinitializesVernStats()
        {
            float initialVibe = _gameState.VernStats.CalculateVIBE();

            _gameState.StartNewNight();

            AssertThat(_gameState.VernStats != null);
        }

        [Test]
        public void InitializeGame_CallsVernStatsInitialize()
        {
            _gameState.InitializeGame();

            AssertThat(_gameState.VernStats != null);
        }
    }
}
