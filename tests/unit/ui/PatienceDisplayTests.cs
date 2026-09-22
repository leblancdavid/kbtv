#nullable enable

using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Screening;
using KBTV.UI;
using KBTV.UI.Themes;

namespace KBTV.Tests.Unit.UI
{
    public class PatienceDisplayTests : KBTVTestClass
    {
        public PatienceDisplayTests(Node testScene) : base(testScene) { }

        protected override bool FailOnRecordedFailures => true;

        [Test]
        public void Text_FullRatio_AllBars()
        {
            AssertAreEqual("[||||||||||||||] 100%", PatienceDisplay.Text(1f));
        }

        [Test]
        public void Text_HalfRatio_HalfBars()
        {
            AssertAreEqual("[|||||||.......] 50%", PatienceDisplay.Text(0.5f));
        }

        [Test]
        public void Text_ZeroRatio_AllDots()
        {
            AssertAreEqual("[..............] 0%", PatienceDisplay.Text(0f));
        }

        [Test]
        public void Remaining_SubtractsElapsedTime()
        {
            var caller = CreateCaller(60f);
            var progress = new ScreeningProgress(0, 0, 45f, 60f, 15f);

            AssertAreEqual(45f, PatienceDisplay.Remaining(caller, progress), "remaining");
            AssertAreEqual(0.75f, PatienceDisplay.Ratio(caller, progress), "ratio");
        }

        [Test]
        public void Remaining_ClampedAtZero()
        {
            var caller = CreateCaller(10f);
            var progress = new ScreeningProgress(0, 0, 0f, 10f, 30f);

            AssertAreEqual(0f, PatienceDisplay.Remaining(caller, progress), "remaining");
            AssertAreEqual(0f, PatienceDisplay.Ratio(caller, progress), "ratio");
        }

        [Test]
        public void NullProgress_ShowsFullPatience()
        {
            var caller = CreateCaller(60f);

            AssertAreEqual(60f, PatienceDisplay.Remaining(caller, null), "remaining");
            AssertAreEqual(1f, PatienceDisplay.Ratio(caller, null), "ratio");
        }

        [Test]
        public void ColorFor_MatchesThresholds()
        {
            AssertAreEqual(UIColors.Patience.High, PatienceDisplay.ColorFor(0.9f));
            AssertAreEqual(UIColors.Patience.Medium, PatienceDisplay.ColorFor(0.5f));
            AssertAreEqual(UIColors.Patience.Low, PatienceDisplay.ColorFor(0.1f));
        }

        private static Caller CreateCaller(float patience)
        {
            return new Caller(
                name: "Test Caller",
                phoneNumber: "555-0123",
                location: "Test City",
                claimedTopic: "Test Topic",
                actualTopic: "Test Topic",
                callReason: "Test Reason",
                legitimacy: CallerLegitimacy.Credible,
                phoneQuality: CallerPhoneQuality.Good,
                emotionalState: CallerEmotionalState.Calm,
                curseRisk: CallerCurseRisk.Low,
                evidenceLevel: CallerEvidenceLevel.None,
                coherence: CallerCoherence.Coherent,
                urgency: CallerUrgency.Medium,
                personality: "Test",
                personalityEffect: null,
                claimedArc: null,
                actualArc: null,
                screeningSummary: "Test",
                patience: patience,
                quality: 1.0f
            );
        }
    }
}
