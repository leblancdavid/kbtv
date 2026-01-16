using Chickensoft.GoDotTest;
using Godot;
using KBTV.UI;

namespace KBTV.Tests.Unit.UI
{
    public class LoadingScreenTests : KBTVTestClass
    {
        public LoadingScreenTests(Node testScene) : base(testScene) { }

        private LoadingScreen _loadingScreen = null!;

        [Setup]
        public void Setup()
        {
            _loadingScreen = new LoadingScreen();
            _loadingScreen._Ready();
        }

        [Test]
        public void Constructor_SetsDefaultGameScenePath()
        {
            var loadingScreen = new LoadingScreen();
            AssertThat(loadingScreen.GameScenePath == "res://scenes/Game.tscn");
        }

        [Test]
        public void Constructor_CreatesBackgroundPanel()
        {
            var background = _loadingScreen.GetNodeOrNull<Panel>("Background");

            AssertThat(background != null);
        }

        [Test]
        public void Constructor_CreatesTitleLabel()
        {
            var title = _loadingScreen.GetNodeOrNull<Label>("Background/Container/Title");

            AssertThat(title != null);
            AssertThat(title.Text == "KBTV RADIO");
        }

        [Test]
        public void Constructor_CreatesProgressBar()
        {
            var progressBar = _loadingScreen.GetNodeOrNull<ProgressBar>("Background/Container/ProgressBar");

            AssertThat(progressBar != null);
        }

        [Test]
        public void Constructor_CreatesStatusLabel()
        {
            var statusLabel = _loadingScreen.GetNodeOrNull<Label>("Background/Container/StatusLabel");

            AssertThat(statusLabel != null);
        }

        [Test]
        public void Constructor_HasLoadingMessages()
        {
            var field = typeof(LoadingScreen).GetField("_loadingMessages", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var messages = field?.GetValue(_loadingScreen) as string[];

            AssertThat(messages?.Length > 0);
        }

        [Test]
        public void StatusLabel_ShowsInitialMessage()
        {
            var statusLabel = _loadingScreen.GetNodeOrNull<Label>("Background/Container/StatusLabel");
            var field = typeof(LoadingScreen).GetField("_loadingMessages", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var messages = field?.GetValue(_loadingScreen) as string[];

            AssertThat(statusLabel.Text == messages?[0]);
        }
    }
}
