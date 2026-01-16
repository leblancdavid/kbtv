using Chickensoft.GoDotTest;
using Godot;
using KBTV.Managers;

namespace KBTV.Tests.Unit.Managers
{
    public class TimeManagerTests : KBTVTestClass
    {
        public TimeManagerTests(Node testScene) : base(testScene) { }

        private TimeManager _timeManager = null!;

        [Setup]
        public void Setup()
        {
            _timeManager = new TimeManager();
            _timeManager._Ready();
        }

        [Test]
        public void Constructor_InitializesElapsedTimeToZero()
        {
            AssertThat(_timeManager.ElapsedTime == 0f);
        }

        [Test]
        public void Constructor_InitializesIsRunningToFalse()
        {
            AssertThat(!_timeManager.IsRunning);
        }

        [Test]
        public void StartClock_SetsIsRunningToTrue()
        {
            _timeManager.StartClock();

            AssertThat(_timeManager.IsRunning);
        }

        [Test]
        public void StartClock_DoesNotStartWhenAlreadyRunning()
        {
            _timeManager.StartClock();
            bool firstCallRunning = _timeManager.IsRunning;

            _timeManager.StartClock();

            AssertThat(_timeManager.IsRunning == firstCallRunning);
        }

        [Test]
        public void PauseClock_SetsIsRunningToFalse()
        {
            _timeManager.StartClock();
            AssertThat(_timeManager.IsRunning);

            _timeManager.PauseClock();

            AssertThat(!_timeManager.IsRunning);
        }

        [Test]
        public void PauseClock_DoesNothingWhenNotRunning()
        {
            bool initialRunning = _timeManager.IsRunning;

            _timeManager.PauseClock();

            AssertThat(_timeManager.IsRunning == initialRunning);
        }

        [Test]
        public void ResetClock_ResetsElapsedTime()
        {
            _timeManager.StartClock();
            _timeManager._Process(1.0);

            _timeManager.ResetClock();

            AssertThat(_timeManager.ElapsedTime == 0f);
            AssertThat(!_timeManager.IsRunning);
        }

        [Test]
        public void ResetClock_StopsClock()
        {
            _timeManager.StartClock();

            _timeManager.ResetClock();

            AssertThat(!_timeManager.IsRunning);
        }

        [Test]
        public void EndShow_StopsClock()
        {
            _timeManager.StartClock();

            _timeManager.EndShow();

            AssertThat(!_timeManager.IsRunning);
        }

        [Test]
        public void Progress_StartsAtZero()
        {
            AssertThat(_timeManager.Progress == 0f);
        }

        [Test]
        public void Progress_IncreasesWithTime()
        {
            _timeManager.StartClock();
            _timeManager._Process(1.0);

            AssertThat(_timeManager.Progress > 0f);
        }

        [Test]
        public void Progress_ClampedToOne()
        {
            _timeManager.StartClock();

            for (int i = 0; i < 100; i++)
            {
                _timeManager._Process(10.0);
            }

            AssertThat(_timeManager.Progress <= 1f);
        }

        [Test]
        public void RemainingTime_PositiveWhenTimeRemaining()
        {
            AssertThat(_timeManager.RemainingTime > 0f);
        }

        [Test]
        public void RemainingTime_ZeroWhenShowEnded()
        {
            _timeManager.StartClock();

            for (int i = 0; i < 100; i++)
            {
                _timeManager._Process(10.0);
            }

            AssertThat(_timeManager.RemainingTime == 0f);
        }

        [Test]
        public void CurrentHour_WithinShowStartRange()
        {
            float hour = _timeManager.CurrentHour;
            AssertThat(hour >= 22f);
            AssertThat(hour <= 23f);
        }

        [Test]
        public void CurrentTimeFormatted_HasValidFormat()
        {
            string formatted = _timeManager.CurrentTimeFormatted;
            AssertThat(!string.IsNullOrEmpty(formatted));
            AssertThat(formatted.Contains(":"));
        }

        [Test]
        public void RemainingTimeFormatted_HasValidFormat()
        {
            string formatted = _timeManager.RemainingTimeFormatted;
            AssertThat(!string.IsNullOrEmpty(formatted));
        }
    }
}
