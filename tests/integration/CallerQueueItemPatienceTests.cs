using System.Collections.Generic;
using System.Linq;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Screening;
using KBTV.UI.Themes;

namespace KBTV.Tests.Integration
{
    public class CallerQueueItemPatienceTests : KBTVTestClass
    {
        public CallerQueueItemPatienceTests(Node testScene) : base(testScene) { }

        private CallerRepository _repository = null!;
        private ScreeningController _controller = null!;
        private List<string> _eventLog = null!;

        [Setup]
        public void Setup()
        {
            _repository = new CallerRepository();
            _controller = new ScreeningController();
            _eventLog = new List<string>();

            _repository.Subscribe(new TestCallerRepositoryObserver(_eventLog));
        }

        [Test]
        public void IncomingCaller_Patience_DepletesOverWaitTime()
        {
            var caller = CreateTestCaller("Alice", patience: 30f);
            _repository.AddCaller(caller);

            AssertThat(_repository.IncomingCallers.Count == 1);
            var incomingCaller = _repository.IncomingCallers[0];
            AssertThat(incomingCaller.WaitTime == 0f);

            incomingCaller.UpdateWaitTime(5f);
            AssertThat(incomingCaller.WaitTime == 5f);
            AssertThat(incomingCaller.Patience - incomingCaller.WaitTime == 25f);

            incomingCaller.UpdateWaitTime(10f);
            AssertThat(incomingCaller.WaitTime == 15f);
            AssertThat(incomingCaller.Patience - incomingCaller.WaitTime == 15f);
        }

        [Test]
        public void IncomingCaller_PatienceRatio_CalculatesCorrectly()
        {
            var caller = CreateTestCaller("Bob", patience: 30f);
            _repository.AddCaller(caller);

            var repositoryCaller = _repository.IncomingCallers[0];
            repositoryCaller.UpdateWaitTime(10f);

            float remainingPatience = caller.Patience - caller.WaitTime;
            float patienceRatio = Mathf.Clamp(remainingPatience / caller.Patience, 0f, 1f);
            AssertThat(patienceRatio == Mathf.Clamp(20f / 30f, 0f, 1f));

            repositoryCaller.UpdateWaitTime(20f);
            remainingPatience = caller.Patience - caller.WaitTime;
            patienceRatio = Mathf.Clamp(remainingPatience / caller.Patience, 0f, 1f);
            AssertThat(patienceRatio == Mathf.Clamp(10f / 30f, 0f, 1f));
        }

        [Test]
        public void ScreeningCaller_Patience_DepletesAtHalfRate()
        {
            var caller = CreateTestCaller("Charlie", patience: 30f);
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);

            _controller.Start(caller);

            var initialScreeningPatience = caller.ScreeningPatience;
            AssertThat(initialScreeningPatience == 30f);

            _controller.Update(10f);
            AssertThat(caller.ScreeningPatience < initialScreeningPatience);

            float expectedDepletion = 10f * 0.5f;
            AssertThat(Mathf.Abs(caller.ScreeningPatience - (initialScreeningPatience - expectedDepletion)) < 0.01f);
        }

        [Test]
        public void IncomingCaller_DisconnectsWhenPatienceRunsOut()
        {
            var caller = CreateTestCaller("David", patience: 10f);
            _repository.AddCaller(caller);

            var repositoryCaller = _repository.IncomingCallers[0];
            AssertThat(repositoryCaller.State == CallerState.Incoming);

            repositoryCaller.UpdateWaitTime(5f);
            AssertThat(repositoryCaller.State == CallerState.Incoming);
            AssertThat(!_eventLog.Contains("CallerRemoved: David"));

            var disconnected = repositoryCaller.UpdateWaitTime(6f);
            AssertThat(disconnected);
            AssertThat(repositoryCaller.State == CallerState.Disconnected);
            AssertThat(_eventLog.Contains("CallerRemoved: David"));
        }

        [Test]
        public void ScreeningCaller_DisconnectsWhenScreeningPatienceRunsOut()
        {
            var caller = CreateTestCaller("Eve", patience: 10f);
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);

            _controller.Start(caller);

            AssertThat(caller.State == CallerState.Screening);
            AssertThat(!_eventLog.Contains("CallerRemoved: Eve"));

            for (int i = 0; i < 25; i++)
            {
                _controller.Update(1f);
            }

            AssertThat(caller.State == CallerState.Disconnected);
            AssertThat(_eventLog.Contains("CallerRemoved: Eve"));
        }

        [Test]
        public void OnHoldCaller_WaitTime_DoesNotAccumulate()
        {
            var caller = CreateTestCaller("Frank", patience: 30f);
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);
            _repository.ApproveScreening();

            AssertThat(caller.State == CallerState.OnHold);
            var initialWaitTime = caller.WaitTime;

            caller.UpdateWaitTime(10f);
            AssertThat(caller.WaitTime == initialWaitTime);
        }

        [Test]
        public void OnAirCaller_WaitTime_DoesNotAccumulate()
        {
            var caller = CreateTestCaller("Grace", patience: 30f);
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);
            _repository.ApproveScreening();
            _repository.PutOnAir();

            AssertThat(caller.State == CallerState.OnAir);
            var initialWaitTime = caller.WaitTime;

            caller.UpdateWaitTime(10f);
            AssertThat(caller.WaitTime == initialWaitTime);
        }

        [Test]
        public void MultipleCallers_Patience_UpdatesIndependently()
        {
            var caller1 = CreateTestCaller("Caller1", patience: 20f);
            var caller2 = CreateTestCaller("Caller2", patience: 30f);
            var caller3 = CreateTestCaller("Caller3", patience: 40f);

            _repository.AddCaller(caller1);
            _repository.AddCaller(caller2);
            _repository.AddCaller(caller3);

            var callers = _repository.IncomingCallers.ToList();
            AssertThat(callers.Count == 3);

            foreach (var caller in callers)
            {
                caller.UpdateWaitTime(5f);
            }

            foreach (var caller in callers)
            {
                AssertThat(caller.WaitTime == 5f);
            }

            callers[0].UpdateWaitTime(10f);

            AssertThat(callers[0].WaitTime == 15f);
            AssertThat(callers[1].WaitTime == 5f);
            AssertThat(callers[2].WaitTime == 5f);
        }

        [Test]
        public void PatienceColor_GreenWhenHigh()
        {
            var caller = CreateTestCaller("Test", patience: 30f);
            _repository.AddCaller(caller);

            caller.UpdateWaitTime(5f);
            float remainingPatience = caller.Patience - caller.WaitTime;
            float patienceRatio = remainingPatience / caller.Patience;

            AssertThat(patienceRatio > 0.66f);

            var color = UIColors.GetPatienceColor(patienceRatio);
            AssertThat(color == UIColors.Patience.High);
        }

        [Test]
        public void PatienceColor_YellowWhenMedium()
        {
            var caller = CreateTestCaller("Test", patience: 30f);
            _repository.AddCaller(caller);

            caller.UpdateWaitTime(15f);
            float remainingPatience = caller.Patience - caller.WaitTime;
            float patienceRatio = remainingPatience / caller.Patience;

            AssertThat(patienceRatio > 0.33f && patienceRatio <= 0.66f);

            var color = UIColors.GetPatienceColor(patienceRatio);
            AssertThat(color == UIColors.Patience.Medium);
        }

        [Test]
        public void PatienceColor_RedWhenLow()
        {
            var caller = CreateTestCaller("Test", patience: 30f);
            _repository.AddCaller(caller);

            caller.UpdateWaitTime(25f);
            float remainingPatience = caller.Patience - caller.WaitTime;
            float patienceRatio = remainingPatience / caller.Patience;

            AssertThat(patienceRatio <= 0.33f);

            var color = UIColors.GetPatienceColor(patienceRatio);
            AssertThat(color == UIColors.Patience.Low);
        }

        [Test]
        public void PatienceColor_clampsToZero()
        {
            var caller = CreateTestCaller("Test", patience: 10f);
            _repository.AddCaller(caller);

            caller.UpdateWaitTime(15f);
            float remainingPatience = caller.Patience - caller.WaitTime;
            AssertThat(remainingPatience < 0);

            float patienceRatio = Mathf.Clamp(remainingPatience / caller.Patience, 0f, 1f);
            AssertThat(patienceRatio == 0f);

            var color = UIColors.GetPatienceColor(patienceRatio);
            AssertThat(color == UIColors.Patience.Low);
        }

        [Test]
        public void WaitTime_IncreasesWithEachUpdate()
        {
            var caller = CreateTestCaller("Test", patience: 30f);
            _repository.AddCaller(caller);

            var repositoryCaller = _repository.IncomingCallers[0];
            AssertThat(repositoryCaller.WaitTime == 0f);

            repositoryCaller.UpdateWaitTime(1f);
            AssertThat(repositoryCaller.WaitTime == 1f);

            repositoryCaller.UpdateWaitTime(2f);
            AssertThat(repositoryCaller.WaitTime == 3f);

            repositoryCaller.UpdateWaitTime(5f);
            AssertThat(repositoryCaller.WaitTime == 8f);
        }

        [Test]
        public void PatienceRatio_UpdatesCorrectlyAsWaitTimeIncreases()
        {
            var caller = CreateTestCaller("Test", patience: 30f);
            _repository.AddCaller(caller);

            var repositoryCaller = _repository.IncomingCallers[0];

            repositoryCaller.UpdateWaitTime(0f);
            float ratio1 = (caller.Patience - caller.WaitTime) / caller.Patience;
            AssertThat(Mathf.Abs(ratio1 - 1.0f) < 0.01f);

            repositoryCaller.UpdateWaitTime(10f);
            float ratio2 = (caller.Patience - caller.WaitTime) / caller.Patience;
            AssertThat(Mathf.Abs(ratio2 - (20f / 30f)) < 0.01f);

            repositoryCaller.UpdateWaitTime(20f);
            float ratio3 = (caller.Patience - caller.WaitTime) / caller.Patience;
            AssertThat(Mathf.Abs(ratio3 - (10f / 30f)) < 0.01f);
        }

        [Test]
        public void ScreeningPatience_DrainsAtHalfRateOfNormalPatience()
        {
            var normalCaller = CreateTestCaller("Normal", patience: 20f);
            var screeningCaller = CreateTestCaller("Screening", patience: 20f);

            _repository.AddCaller(normalCaller);
            _repository.AddCaller(screeningCaller);

            _repository.StartScreening(screeningCaller);

            normalCaller.UpdateWaitTime(10f);
            screeningCaller.UpdateWaitTime(10f);

            AssertThat(normalCaller.Patience - normalCaller.WaitTime == 10f);
            AssertThat(screeningCaller.ScreeningPatience == 15f);
        }

        private Caller CreateTestCaller(string name = "Test Caller", float patience = 30f)
        {
            return new Caller(
                name,
                "555-0123",
                "Test Location",
                "Ghosts",
                "Ghosts",
                "Test Reason",
                CallerLegitimacy.Credible,
                CallerPhoneQuality.Good,
                CallerEmotionalState.Calm,
                CallerCurseRisk.Low,
                CallerBeliefLevel.Curious,
                CallerEvidenceLevel.None,
                CallerCoherence.Coherent,
                CallerUrgency.Low,
                "personality",
                "arc",
                "summary",
                patience,
                0.8f
            );
        }

        private class TestCallerRepositoryObserver : ICallerRepositoryObserver
        {
            private readonly List<string> _eventLog;

            public TestCallerRepositoryObserver(List<string> eventLog)
            {
                _eventLog = eventLog;
            }

            public void OnCallerAdded(Caller caller) =>
                _eventLog.Add($"CallerAdded: {caller.Name}");

            public void OnCallerRemoved(Caller caller) =>
                _eventLog.Add($"CallerRemoved: {caller.Name}");

            public void OnCallerStateChanged(Caller caller, CallerState oldState, CallerState newState) =>
                _eventLog.Add($"StateChanged: {caller.Name}:{oldState}->{newState}");

            public void OnScreeningStarted(Caller caller) =>
                _eventLog.Add($"ScreeningStarted: {caller.Name}");

            public void OnScreeningEnded(Caller caller, bool approved)
            {
                _eventLog.Add($"ScreeningEnded: {caller.Name}");
                _eventLog.Add(approved ? $"ScreeningApproved: {caller.Name}" : $"ScreeningRejected: {caller.Name}");
            }

            public void OnCallerOnAir(Caller caller) =>
                _eventLog.Add($"OnAir: {caller.Name}");

            public void OnCallerOnAirEnded(Caller caller) =>
                _eventLog.Add($"OnAirEnded: {caller.Name}");
        }
    }
}
