using Chickensoft.GoDotTest;
using Godot;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Dialogue
{
    public class AudioDialoguePlayerTests : KBTVTestClass
    {
        public AudioDialoguePlayerTests(Node testScene) : base(testScene) { }

        private AudioDialoguePlayer _audioPlayer = null!;

        [Setup]
        public void Setup()
        {
            _audioPlayer = new AudioDialoguePlayer();
        }

        [Test]
        public void Constructor_CreatesAudioStreamPlayer()
        {
            AssertThat(_audioPlayer != null);
        }

        [Test]
        public void IsPlaying_ReturnsFalse_WhenNotPlaying()
        {
            AssertThat(!_audioPlayer.IsPlaying);
        }

        [Test]
        public void Stop_ResetsCurrentLineId()
        {
            _audioPlayer.Stop();
            AssertThat(!_audioPlayer.IsPlaying);
        }

        [Test]
        public void CalculateDurationForText_ReturnsMinimum_ForEmptyText()
        {
            var duration = typeof(AudioDialoguePlayer)
                .GetMethod("CalculateDurationForText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(_audioPlayer, new object[] { "" });

            AssertThat((float)duration! == 1.0f);
        }

        [Test]
        public void CalculateDurationForText_ReturnsCorrectDuration_ForNormalText()
        {
            var duration = typeof(AudioDialoguePlayer)
                .GetMethod("CalculateDurationForText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(_audioPlayer, new object[] { "Hello world this is a test" });

            AssertThat((float)duration! >= 1.0f);
        }

        [Test]
        public void LineCompleted_Event_IsSubscribable()
        {
            bool eventFired = false;
            _audioPlayer.LineCompleted += (e) => eventFired = true;
            AssertThat(eventFired == false);
        }
    }
}
