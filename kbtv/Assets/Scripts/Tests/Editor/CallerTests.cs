using NUnit.Framework;
using KBTV.Callers;

namespace KBTV.Tests
{
    /// <summary>
    /// Unit tests for the Caller class.
    /// </summary>
    public class CallerTests
    {
        private Caller CreateTestCaller(
            CallerLegitimacy legitimacy = CallerLegitimacy.Credible,
            float patience = 30f,
            float quality = 0.5f,
            string claimedTopic = "UFOs",
            string actualTopic = "UFOs")
        {
            return new Caller(
                name: "Test Caller",
                phoneNumber: "555-1234",
                location: "Anytown, USA",
                claimedTopic: claimedTopic,
                actualTopic: actualTopic,
                callReason: "Saw something strange",
                legitimacy: legitimacy,
                patience: patience,
                quality: quality
            );
        }

        [Test]
        public void Constructor_SetsInitialState()
        {
            var caller = CreateTestCaller();

            Assert.AreEqual(CallerState.Incoming, caller.State);
            Assert.AreEqual(0f, caller.WaitTime);
            Assert.IsNotNull(caller.Id);
            Assert.IsNotEmpty(caller.Id);
        }

        [Test]
        public void Constructor_SetsProperties()
        {
            var caller = CreateTestCaller(
                legitimacy: CallerLegitimacy.Compelling,
                patience: 45f,
                quality: 0.8f
            );

            Assert.AreEqual("Test Caller", caller.Name);
            Assert.AreEqual("555-1234", caller.PhoneNumber);
            Assert.AreEqual("Anytown, USA", caller.Location);
            Assert.AreEqual(CallerLegitimacy.Compelling, caller.Legitimacy);
            Assert.AreEqual(45f, caller.Patience);
            Assert.AreEqual(0.8f, caller.Quality);
        }

        [Test]
        public void IsLyingAboutTopic_ReturnsTrueWhenTopicsDiffer()
        {
            var caller = CreateTestCaller(claimedTopic: "UFOs", actualTopic: "Bigfoot");
            Assert.IsTrue(caller.IsLyingAboutTopic);
        }

        [Test]
        public void IsLyingAboutTopic_ReturnsFalseWhenTopicsMatch()
        {
            var caller = CreateTestCaller(claimedTopic: "UFOs", actualTopic: "UFOs");
            Assert.IsFalse(caller.IsLyingAboutTopic);
        }

        [Test]
        public void UpdateWaitTime_IncreasesWaitTime()
        {
            var caller = CreateTestCaller(patience: 60f);

            caller.UpdateWaitTime(5f);

            Assert.AreEqual(5f, caller.WaitTime);
        }

        [Test]
        public void UpdateWaitTime_AccumulatesTime()
        {
            var caller = CreateTestCaller(patience: 60f);

            caller.UpdateWaitTime(5f);
            caller.UpdateWaitTime(3f);

            Assert.AreEqual(8f, caller.WaitTime);
        }

        [Test]
        public void UpdateWaitTime_ReturnsTrueWhenPatienceExceeded()
        {
            var caller = CreateTestCaller(patience: 10f);

            bool disconnected = caller.UpdateWaitTime(15f);

            Assert.IsTrue(disconnected);
        }

        [Test]
        public void UpdateWaitTime_ReturnsFalseWhenPatienceNotExceeded()
        {
            var caller = CreateTestCaller(patience: 30f);

            bool disconnected = caller.UpdateWaitTime(5f);

            Assert.IsFalse(disconnected);
        }

        [Test]
        public void UpdateWaitTime_SetsStateToDisconnectedWhenPatienceExceeded()
        {
            var caller = CreateTestCaller(patience: 10f);

            caller.UpdateWaitTime(15f);

            Assert.AreEqual(CallerState.Disconnected, caller.State);
        }

        [Test]
        public void UpdateWaitTime_FiresOnDisconnectedWhenPatienceExceeded()
        {
            var caller = CreateTestCaller(patience: 10f);
            int callCount = 0;
            caller.OnDisconnected += () => callCount++;

            caller.UpdateWaitTime(15f);

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void UpdateWaitTime_DoesNotUpdateWhenCompleted()
        {
            var caller = CreateTestCaller(patience: 30f);
            caller.SetState(CallerState.Completed);

            bool result = caller.UpdateWaitTime(5f);

            Assert.IsFalse(result);
            Assert.AreEqual(0f, caller.WaitTime);
        }

        [Test]
        public void UpdateWaitTime_DoesNotUpdateWhenRejected()
        {
            var caller = CreateTestCaller(patience: 30f);
            caller.SetState(CallerState.Rejected);

            caller.UpdateWaitTime(5f);

            Assert.AreEqual(0f, caller.WaitTime);
        }

        [Test]
        public void UpdateWaitTime_DoesNotUpdateWhenOnAir()
        {
            var caller = CreateTestCaller(patience: 30f);
            caller.SetState(CallerState.OnAir);

            caller.UpdateWaitTime(5f);

            Assert.AreEqual(0f, caller.WaitTime);
        }

        [Test]
        public void UpdateWaitTime_DoesNotUpdateWhenAlreadyDisconnected()
        {
            var caller = CreateTestCaller(patience: 30f);
            caller.SetState(CallerState.Disconnected);

            caller.UpdateWaitTime(5f);

            Assert.AreEqual(0f, caller.WaitTime);
        }

        [Test]
        public void SetState_ChangesState()
        {
            var caller = CreateTestCaller();

            caller.SetState(CallerState.Screening);

            Assert.AreEqual(CallerState.Screening, caller.State);
        }

        [Test]
        public void SetState_FiresOnStateChanged()
        {
            var caller = CreateTestCaller();
            CallerState capturedOldState = CallerState.Completed;
            CallerState capturedNewState = CallerState.Completed;
            int callCount = 0;

            caller.OnStateChanged += (oldState, newState) =>
            {
                capturedOldState = oldState;
                capturedNewState = newState;
                callCount++;
            };

            caller.SetState(CallerState.OnHold);

            Assert.AreEqual(1, callCount);
            Assert.AreEqual(CallerState.Incoming, capturedOldState);
            Assert.AreEqual(CallerState.OnHold, capturedNewState);
        }

        [Test]
        public void SetState_DoesNotFireWhenStateUnchanged()
        {
            var caller = CreateTestCaller();
            int callCount = 0;
            caller.OnStateChanged += (old, newState) => callCount++;

            caller.SetState(CallerState.Incoming); // Same as initial state

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void CalculateShowImpact_ReturnsNegativeForFakeLegitimacy()
        {
            var caller = CreateTestCaller(legitimacy: CallerLegitimacy.Fake, quality: 0.5f);

            float impact = caller.CalculateShowImpact("UFOs");

            Assert.Less(impact, 0f);
        }

        [Test]
        public void CalculateShowImpact_ReturnsPositiveForCredibleLegitimacy()
        {
            var caller = CreateTestCaller(legitimacy: CallerLegitimacy.Credible, quality: 0.5f);

            float impact = caller.CalculateShowImpact("UFOs");

            Assert.Greater(impact, 0f);
        }

        [Test]
        public void CalculateShowImpact_HigherForCompellingThanCredible()
        {
            var credibleCaller = CreateTestCaller(legitimacy: CallerLegitimacy.Credible, quality: 0.5f);
            var compellingCaller = CreateTestCaller(legitimacy: CallerLegitimacy.Compelling, quality: 0.5f);

            float credibleImpact = credibleCaller.CalculateShowImpact("UFOs");
            float compellingImpact = compellingCaller.CalculateShowImpact("UFOs");

            Assert.Greater(compellingImpact, credibleImpact);
        }

        [Test]
        public void CalculateShowImpact_BonusForMatchingTopic()
        {
            var caller = CreateTestCaller(actualTopic: "UFOs", quality: 0.5f);

            float matchingImpact = caller.CalculateShowImpact("UFOs");
            float nonMatchingImpact = caller.CalculateShowImpact("Bigfoot");

            Assert.Greater(matchingImpact, nonMatchingImpact);
        }

        [Test]
        public void CalculateShowImpact_MatchingTopicGives1_5xBonus()
        {
            var caller = CreateTestCaller(
                actualTopic: "UFOs",
                quality: 1f,
                legitimacy: CallerLegitimacy.Credible
            );

            float matchingImpact = caller.CalculateShowImpact("UFOs");
            float nonMatchingImpact = caller.CalculateShowImpact("Bigfoot");

            // Matching: 1.0 * 1.5 * 1.0 = 1.5
            // Non-matching: 1.0 * 0.5 * 1.0 = 0.5
            Assert.AreEqual(1.5f, matchingImpact, 0.001f);
            Assert.AreEqual(0.5f, nonMatchingImpact, 0.001f);
        }

        [Test]
        public void CalculateShowImpact_QuestionableLegitimacyGives0_25xMultiplier()
        {
            var caller = CreateTestCaller(
                legitimacy: CallerLegitimacy.Questionable,
                quality: 1f,
                actualTopic: "UFOs"
            );

            float impact = caller.CalculateShowImpact("UFOs");

            // 1.0 * 1.5 (matching) * 0.25 (questionable) = 0.375
            Assert.AreEqual(0.375f, impact, 0.001f);
        }

        [Test]
        public void GetScreeningInfo_ContainsAllFields()
        {
            var caller = CreateTestCaller();

            string info = caller.GetScreeningInfo();

            Assert.That(info, Does.Contain("Test Caller"));
            Assert.That(info, Does.Contain("555-1234"));
            Assert.That(info, Does.Contain("Anytown, USA"));
            Assert.That(info, Does.Contain("UFOs"));
            Assert.That(info, Does.Contain("Saw something strange"));
        }

        [Test]
        public void Id_IsUnique()
        {
            var caller1 = CreateTestCaller();
            var caller2 = CreateTestCaller();

            Assert.AreNotEqual(caller1.Id, caller2.Id);
        }
    }
}
