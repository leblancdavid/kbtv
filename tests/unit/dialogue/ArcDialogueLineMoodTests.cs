using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Dialogue
{
    public class ArcDialogueLineMoodTests : KBTVTestClass
    {
        public ArcDialogueLineMoodTests(Node testScene) : base(testScene) { }

        protected override bool FailOnRecordedFailures => true;

        private static ArcDialogueLine VernLineWithVariants()
        {
            var texts = new Godot.Collections.Dictionary<string, string>
            {
                ["neutral"] = "You're on the air.",
                ["tired"] = "Alright, you're on.",
            };
            var audioIds = new Godot.Collections.Dictionary<string, string>
            {
                ["neutral"] = "ufos_credible_demo_vern_neutral_1",
                ["tired"] = "ufos_credible_demo_vern_tired_1",
            };
            return ArcDialogueLine.CreateVernLineWithVariants(
                texts, audioIds, "You're on the air.", "ufos_credible_demo_vern_neutral_1");
        }

        [Test]
        public void GetAudioIdForMood_ReturnsVariantWhenPresent()
        {
            AssertAreEqual("ufos_credible_demo_vern_tired_1", VernLineWithVariants().GetAudioIdForMood("tired"));
        }

        [Test]
        public void GetAudioIdForMood_FallsBackToDefault_WhenMoodMissing()
        {
            var line = VernLineWithVariants();
            AssertAreEqual("ufos_credible_demo_vern_neutral_1", line.GetAudioIdForMood("manic"));
            AssertAreEqual("ufos_credible_demo_vern_neutral_1", line.GetAudioIdForMood(null));
            AssertAreEqual("ufos_credible_demo_vern_neutral_1", line.GetAudioIdForMood(""));
        }

        [Test]
        public void GetTextForMood_ReturnsVariantWithFallback()
        {
            var line = VernLineWithVariants();
            AssertAreEqual("Alright, you're on.", line.GetTextForMood("tired"));
            AssertAreEqual("You're on the air.", line.GetTextForMood("angry"));
        }

        [Test]
        public void CallerLine_UsesDefaultRegardlessOfMood()
        {
            var line = ArcDialogueLine.CreateLine(Speaker.Caller, "I saw lights.", "ufos_credible_demo_caller_1");
            AssertAreEqual("ufos_credible_demo_caller_1", line.GetAudioIdForMood("tired"));
            AssertAreEqual("I saw lights.", line.GetTextForMood("tired"));
        }
    }
}
