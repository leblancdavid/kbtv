using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;

namespace KBTV.Tests.Unit.Callers
{
    public class CallerTests : KBTVTestClass
    {
        public CallerTests(Node testScene) : base(testScene) { }

        private Caller CreateTestCaller(
            string name = "Test Caller",
            CallerLegitimacy legitimacy = CallerLegitimacy.Credible,
            CallerPhoneQuality phoneQuality = CallerPhoneQuality.Good,
            float patience = 30f,
            float quality = 0.8f)
        {
            return new Caller(
                name,
                "555-0123",
                "Test Location",
                "Ghosts",
                "Ghosts",
                "Test Reason",
                legitimacy,
                phoneQuality,
                CallerEmotionalState.Calm,
                CallerCurseRisk.Low,
                CallerBeliefLevel.Curious,
                CallerEvidenceLevel.None,
                CallerCoherence.Coherent,
                CallerUrgency.Low,
                "nervous_hiker",
                "test_arc",
                "Test summary",
                patience,
                quality
            );
        }

        [Test]
        public void Constructor_SetsPropertiesCorrectly()
        {
            var caller = CreateTestCaller("John Doe");

            AssertThat(caller.Name == "John Doe");
            AssertThat(caller.PhoneNumber == "555-0123");
            AssertThat(caller.Location == "Test Location");
            AssertThat(caller.ClaimedTopic == "Ghosts");
            AssertThat(caller.ActualTopic == "Ghosts");
            AssertThat(caller.CallReason == "Test Reason");
            AssertThat(caller.Legitimacy == CallerLegitimacy.Credible);
            AssertThat(caller.PhoneQuality == CallerPhoneQuality.Good);
            AssertThat(caller.Patience == 30f);
            AssertThat(caller.Quality == 0.8f);
        }

        [Test]
        public void Constructor_InitializesState_ToIncoming()
        {
            var caller = CreateTestCaller();
            AssertThat(caller.State == CallerState.Incoming);
        }

        [Test]
        public void Constructor_InitializesWaitTime_ToZero()
        {
            var caller = CreateTestCaller();
            AssertThat(caller.WaitTime == 0f);
        }

        [Test]
        public void Constructor_GeneratesUniqueId()
        {
            var caller1 = CreateTestCaller();
            var caller2 = CreateTestCaller();

            AssertThat(caller1.Id != caller2.Id);
        }

        [Test]
        public void Constructor_InitializesRevelations()
        {
            var caller = CreateTestCaller();
            AssertThat(caller.Revelations != null);
            AssertThat(caller.Revelations.Length == 11);
        }

        [Test]
        public void PhoneQualityModifier_Terrible_ReturnsNegative2()
        {
            var caller = CreateTestCaller(phoneQuality: CallerPhoneQuality.Terrible);
            AssertThat(caller.PhoneQualityModifier == -2);
        }

        [Test]
        public void PhoneQualityModifier_Poor_ReturnsNegative1()
        {
            var caller = CreateTestCaller(phoneQuality: CallerPhoneQuality.Poor);
            AssertThat(caller.PhoneQualityModifier == -1);
        }

        [Test]
        public void PhoneQualityModifier_Average_ReturnsZero()
        {
            var caller = CreateTestCaller(phoneQuality: CallerPhoneQuality.Average);
            AssertThat(caller.PhoneQualityModifier == 0);
        }

        [Test]
        public void PhoneQualityModifier_Good_ReturnsPositive1()
        {
            var caller = CreateTestCaller(phoneQuality: CallerPhoneQuality.Good);
            AssertThat(caller.PhoneQualityModifier == 1);
        }

        [Test]
        public void IsLyingAboutTopic_SameTopics_ReturnsFalse()
        {
            var caller = CreateTestCaller();
            AssertThat(!caller.IsLyingAboutTopic);
        }

        [Test]
        public void IsLyingAboutTopic_DifferentTopics_ReturnsTrue()
        {
            var caller = new Caller(
                "Test", "555-0123", "Location",
                "Ghosts", "Aliens", "Reason",
                CallerLegitimacy.Credible, CallerPhoneQuality.Good,
                CallerEmotionalState.Calm, CallerCurseRisk.Low,
                CallerBeliefLevel.Curious, CallerEvidenceLevel.None,
                CallerCoherence.Coherent, CallerUrgency.Low,
                "personality", "arc", "summary", 30f, 0.8f
            );
            AssertThat(caller.IsLyingAboutTopic);
        }

        [Test]
        public void SetOffTopic_UpdatesIsOffTopic()
        {
            var caller = CreateTestCaller();
            AssertThat(!caller.IsOffTopic);

            caller.SetOffTopic(true);
            AssertThat(caller.IsOffTopic);

            caller.SetOffTopic(false);
            AssertThat(!caller.IsOffTopic);
        }

        [Test]
        public void GetRevelation_ExistingProperty_ReturnsRevelation()
        {
            var caller = CreateTestCaller();
            var revelation = caller.GetRevelation("AudioQuality");

            AssertThat(revelation != null);
            AssertThat(revelation!.PropertyKey == "AudioQuality");
        }

        [Test]
        public void GetRevelation_NonExistentProperty_ReturnsNull()
        {
            var caller = CreateTestCaller();
            var revelation = caller.GetRevelation("NonExistent");

            AssertThat(revelation == null);
        }

        [Test]
        public void GetRevealedProperties_InitiallyEmpty()
        {
            var caller = CreateTestCaller();
            var revealed = caller.GetRevealedProperties();

            AssertThat(revealed.Count == 0);
        }

        [Test]
        public void GetNextRevelation_InitiallyReturnsFirstRevelation()
        {
            var caller = CreateTestCaller();
            var next = caller.GetNextRevelation();

            AssertThat(next != null);
            AssertThat(next!.PropertyKey == caller.Revelations[0].PropertyKey);
        }

        [Test]
        public void GetNextRevelation_AllRevealed_ReturnsNull()
        {
            var caller = CreateTestCaller();
            foreach (var rev in caller.Revelations)
            {
                rev.State = RevelationState.Revealed;
            }

            var next = caller.GetNextRevelation();
            AssertThat(next == null);
        }

        [Test]
        public void ResetRevelations_AllHidden()
        {
            var caller = CreateTestCaller();
            caller.GetRevelation("AudioQuality")!.State = RevelationState.Revealed;
            caller.GetRevelation("EmotionalState")!.State = RevelationState.Revealed;

            caller.ResetRevelations();

            foreach (var rev in caller.Revelations)
            {
                AssertThat(rev.State == RevelationState.Hidden);
                AssertThat(rev.ElapsedTime == 0f);
            }
        }

        [Test]
        public void SetState_SameState_DoesNotTriggerEvent()
        {
            var caller = CreateTestCaller();
            int eventCount = 0;
            caller.OnStateChanged += (_, _) => eventCount++;

            caller.SetState(CallerState.Incoming);
            AssertThat(eventCount == 0);
        }

        [Test]
        public void SetState_DifferentState_TriggersEvent()
        {
            var caller = CreateTestCaller();
            CallerState? oldState = null;
            CallerState? newState = null;
            caller.OnStateChanged += (old, newVal) =>
            {
                oldState = old;
                newState = newVal;
            };

            caller.SetState(CallerState.Screening);

            AssertThat(oldState == CallerState.Incoming);
            AssertThat(newState == CallerState.Screening);
        }

        [Test]
        public void UpdateWaitTime_IncomingState_TracksWaitTime()
        {
            var caller = CreateTestCaller(patience: 30f);
            var disconnected = caller.UpdateWaitTime(5f);

            AssertThat(!disconnected);
            AssertThat(caller.WaitTime == 5f);
        }

        [Test]
        public void UpdateWaitTime_PatienceExceeded_ReturnsTrue()
        {
            var caller = CreateTestCaller(patience: 30f);
            var disconnected = caller.UpdateWaitTime(35f);
            AssertThat(disconnected);
            AssertThat(caller.State == CallerState.Disconnected);
        }

        [Test]
        public void UpdateWaitTime_PatienceNotExceeded_ReturnsFalse()
        {
            var caller = CreateTestCaller(patience: 30f);
            var disconnected = caller.UpdateWaitTime(25f);
            AssertThat(!disconnected);
        }

        [Test]
        public void UpdateWaitTime_ScreeningState_DrainsScreeningPatience()
        {
            var caller = CreateTestCaller(patience: 30f);
            caller.SetState(CallerState.Screening);
            float initialScreeningPatience = caller.ScreeningPatience;

            caller.UpdateWaitTime(10f);

            AssertThat(caller.ScreeningPatience < initialScreeningPatience);
        }

        [Test]
        public void UpdateWaitTime_CompletedState_DoesNotUpdate()
        {
            var caller = CreateTestCaller(patience: 30f);
            caller.SetState(CallerState.Completed);

            var disconnected = caller.UpdateWaitTime(100f);

            AssertThat(!disconnected);
            AssertThat(caller.WaitTime == 0f);
        }

        [Test]
        public void UpdateWaitTime_RejectedState_DoesNotUpdate()
        {
            var caller = CreateTestCaller(patience: 30f);
            caller.SetState(CallerState.Rejected);

            var disconnected = caller.UpdateWaitTime(100f);

            AssertThat(!disconnected);
        }

        [Test]
        public void UpdateWaitTime_DisconnectedState_DoesNotUpdate()
        {
            var caller = CreateTestCaller(patience: 30f);
            caller.SetState(CallerState.Disconnected);

            var disconnected = caller.UpdateWaitTime(100f);

            AssertThat(!disconnected);
        }

        [Test]
        public void UpdateWaitTime_OnAirState_DoesNotUpdate()
        {
            var caller = CreateTestCaller(patience: 30f);
            caller.SetState(CallerState.OnAir);

            var disconnected = caller.UpdateWaitTime(100f);

            AssertThat(!disconnected);
        }

        [Test]
        public void UpdateWaitTime_OnHoldState_DoesNotUpdate()
        {
            var caller = CreateTestCaller(patience: 30f);
            caller.SetState(CallerState.OnHold);

            var disconnected = caller.UpdateWaitTime(100f);

            AssertThat(!disconnected);
        }

        [Test]
        public void UpdateWaitTime_ScreeningPatienceDepleted_Disconnects()
        {
            var caller = CreateTestCaller(patience: 30f);
            caller.SetState(CallerState.Screening);

            bool disconnected = false;
            caller.OnDisconnected += () => disconnected = true;

            caller.UpdateWaitTime(100f);

            AssertThat(disconnected);
            AssertThat(caller.State == CallerState.Disconnected);
        }

        [Test]
        public void UpdateRevelations_UpdatesAllRevelations()
        {
            var caller = CreateTestCaller();
            var audioRev = caller.GetRevelation("AudioQuality");
            AssertThat(audioRev != null);

            caller.UpdateRevelations(1f);

            AssertThat(audioRev!.ElapsedTime == 1f);
        }

        [Test]
        public void GetScreeningInfo_ContainsExpectedInfo()
        {
            var caller = CreateTestCaller("John Doe");
            var info = caller.GetScreeningInfo();

            AssertThat(info.Contains("John Doe"));
            AssertThat(info.Contains("555-0123"));
            AssertThat(info.Contains("Test Location"));
            AssertThat(info.Contains("Ghosts"));
            AssertThat(info.Contains("Test Reason"));
        }

        [Test]
        public void CalculateShowImpact_MatchingTopic_ReturnsHigherImpact()
        {
            var caller = CreateTestCaller(quality: 1f);
            var impact = caller.CalculateShowImpact("Ghosts");

            AssertThat(impact == 1.5f);
        }

        [Test]
        public void CalculateShowImpact_DifferentTopic_ReturnsLowerImpact()
        {
            var caller = CreateTestCaller(quality: 1f);
            var impact = caller.CalculateShowImpact("Aliens");

            AssertThat(impact == 0.5f);
        }

        [Test]
        public void CalculateShowImpact_FakeLegitimacy_ReturnsNegativeImpact()
        {
            var caller = CreateTestCaller(legitimacy: CallerLegitimacy.Fake, quality: 1f);
            var impact = caller.CalculateShowImpact("Ghosts");

            AssertThat(impact < 0f);
        }

        [Test]
        public void CalculateShowImpact_QuestionableLegitimacy_ReturnsReducedImpact()
        {
            var caller = CreateTestCaller(legitimacy: CallerLegitimacy.Questionable, quality: 1f);
            var impact = caller.CalculateShowImpact("Ghosts");

            AssertThat(impact > 0f);
            AssertThat(impact < 1f);
        }

        [Test]
        public void CalculateShowImpact_CompellingLegitimacy_ReturnsBonusImpact()
        {
            var caller = CreateTestCaller(legitimacy: CallerLegitimacy.Compelling, quality: 1f);
            var impact = caller.CalculateShowImpact("Ghosts");

            AssertThat(impact > 1f);
        }
    }
}
