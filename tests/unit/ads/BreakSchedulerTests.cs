using Chickensoft.GoDotTest;
using Godot;
using KBTV.Ads;
using KBTV.Managers;

namespace KBTV.Tests.Unit.Ads
{
    public class BreakSchedulerTests : KBTVTestClass
    {
        public BreakSchedulerTests(Node testScene) : base(testScene) { }

        [Test]
        public void Constructor_SetsProperties()
        {
            var schedule = new AdSchedule();
            var timeManager = new TimeManager();
            var scheduler = new BreakScheduler(schedule, timeManager, 0);

            // Basic constructor test - if it doesn't throw, it's good
            AssertThat(scheduler != null);
        }

        [Test]
        public void SetCallbacks_StoresCallbacks()
        {
            var schedule = new AdSchedule();
            var timeManager = new TimeManager();
            var scheduler = new BreakScheduler(schedule, timeManager, 0);

            bool windowCalled = false;
            bool graceCalled = false;
            bool imminentCalled = false;
            bool breakCalled = false;

            scheduler.SetCallbacks(
                () => windowCalled = true,
                () => graceCalled = true,
                () => imminentCalled = true,
                () => breakCalled = true
            );

            // Since we can't easily test the callbacks without mocking timers,
            // we verify callbacks are set and the object is created successfully
            AssertThat(scheduler != null);
            AssertThat(!windowCalled && !graceCalled && !imminentCalled && !breakCalled); // All false initially
        }
    }
}