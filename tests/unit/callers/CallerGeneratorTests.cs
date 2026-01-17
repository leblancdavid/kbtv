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
}
