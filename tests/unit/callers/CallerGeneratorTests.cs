using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Core;

namespace KBTV.Tests.Unit.Callers
{
    public class CallerGeneratorTests : KBTVTestClass
    {
        public CallerGeneratorTests(Node testScene) : base(testScene) { }

        private CallerGenerator _generator = null!;

        [Setup]
        public void Setup()
        {
            _generator = new CallerGenerator();
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
        public void SpawnCaller_HasValidName()
        {
            var caller = _generator.SpawnCaller();

            AssertThat(!string.IsNullOrEmpty(caller.Name));
        }

        [Test]
        public void SpawnCaller_HasValidPhoneNumber()
        {
            var caller = _generator.SpawnCaller();

            AssertThat(!string.IsNullOrEmpty(caller.PhoneNumber));
            AssertThat(caller.PhoneNumber.Contains("-"));
        }

        [Test]
        public void SpawnCaller_HasValidLocation()
        {
            var caller = _generator.SpawnCaller();

            AssertThat(!string.IsNullOrEmpty(caller.Location));
        }

        [Test]
        public void SpawnCaller_HasValidTopic()
        {
            var caller = _generator.SpawnCaller();

            AssertThat(!string.IsNullOrEmpty(caller.ClaimedTopic));
        }

        [Test]
        public void SpawnCaller_HasValidCallReason()
        {
            var caller = _generator.SpawnCaller();

            AssertThat(!string.IsNullOrEmpty(caller.CallReason));
        }

        [Test]
        public void SpawnCaller_HasPositivePatience()
        {
            var caller = _generator.SpawnCaller();

            AssertThat(caller.Patience > 0f);
        }

        [Test]
        public void SpawnCaller_HasValidLegitimacy()
        {
            var caller = _generator.SpawnCaller();

            AssertThat(caller.Legitimacy >= CallerLegitimacy.Fake);
            AssertThat(caller.Legitimacy <= CallerLegitimacy.Compelling);
        }

        [Test]
        public void SpawnCaller_HasUniqueId()
        {
            var caller1 = _generator.SpawnCaller();
            var caller2 = _generator.SpawnCaller();

            AssertThat(caller1.Id != caller2.Id);
        }

        [Test]
        public void SpawnCaller_InInitialState()
        {
            var caller = _generator.SpawnCaller();

            AssertThat(caller.State == CallerState.Incoming);
        }
    }
}
