using System.Reflection;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Dialogue
{
    public class BroadcastTimerTests : KBTVTestClass
    {
        public BroadcastTimerTests(Node testScene) : base(testScene) { }

        [Test]
        public void CreateTimers_CreatesOnlyOneShotTimers()
        {
            var broadcastTimer = new BroadcastTimer();
            typeof(BroadcastTimer)
                .GetMethod("CreateTimers", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(broadcastTimer, null);

            var timerCount = 0;
            foreach (var child in broadcastTimer.GetChildren())
            {
                if (child is Timer timer)
                {
                    timerCount++;
                    AssertThat(timer.OneShot, $"Broadcast timer '{timer.Name}' should not repeat after firing.");
                }
            }

            AssertThat(timerCount > 0, "BroadcastTimer should create timing Timer children.");
            broadcastTimer.Free();
        }
    }
}
