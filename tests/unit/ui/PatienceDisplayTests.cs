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
        public void Remaining_ReflectsDrainedScreeningPatience()
        {
            var caller = CreateCaller(60f);
            caller.SetState(CallerState.Screening);

            // Screening drains at 0.5x, so 30s of updates removes 15 patience.
            caller.UpdateWaitTime(30f);

            AssertAreEqual(45f, PatienceDisplay.Remaining(caller), "remaining");
            AssertAreEqual(0.75f, PatienceDisplay.Ratio(caller), "ratio");
        }

        [Test]
        public void Remaining_NotDoubleCountedByElapsedTime()
        {
            // Regression: the old formula subtracted the session's ElapsedTime
            // on top of the already-drained patience, expiring the evidence
            // dialog at ~1/3 of the real window.
            var caller = CreateCaller(60f);
            caller.SetState(CallerState.Screening);
            caller.UpdateWaitTime(30f);

            var progress = new ScreeningProgress(0, 0, 45f, 60f, 30f);
            AssertAreEqual(caller.ScreeningPatience, PatienceDisplay.Remaining(caller), "remaining");
            AssertThat(progress.ElapsedTime > 0f, "elapsed time must not affect the display");
        }

        [Test]
        public void Remaining_ClampedAtZero()
        {
            var caller = CreateCaller(10f);
            caller.SetState(CallerState.Screening);
            caller.UpdateWaitTime(60f);

            AssertAreEqual(0f, PatienceDisplay.Remaining(caller), "remaining");
            AssertAreEqual(0f, PatienceDisplay.Ratio(caller), "ratio");
        }

        [Test]
        public void FreshCaller_ShowsFullPatience()
        {
            var caller = CreateCaller(60f);

            AssertAreEqual(60f, PatienceDisplay.Remaining(caller), "remaining");
            AssertAreEqual(1f, PatienceDisplay.Ratio(caller), "ratio");
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
