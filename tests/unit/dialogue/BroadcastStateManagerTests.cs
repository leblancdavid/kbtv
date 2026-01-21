using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Dialogue;

public class BroadcastStateManagerTests : KBTVTestClass
{
    public BroadcastStateManagerTests(Node testScene) : base(testScene) { }

    [Test]
    public void SetState_UpdatesCurrentState()
    {
        var repo = new CallerRepository();
        var coordinator = new BroadcastCoordinator();
        var manager = new BroadcastStateManager(repo, coordinator);

        manager.SetState(BroadcastCoordinator.BroadcastState.Conversation);
        AssertThat(manager.CurrentState).IsEqualTo(BroadcastCoordinator.BroadcastState.Conversation);
    }

    [Test]
    public void ResetFillerCycleCount_SetsToZero()
    {
        var repo = new CallerRepository();
        var coordinator = new BroadcastCoordinator();
        var manager = new BroadcastStateManager(repo, coordinator);

        manager.IncrementFillerCycle();
        AssertThat(manager.FillerCycleCount).IsEqualTo(1);

        manager.ResetFillerCycleCount();
        AssertThat(manager.FillerCycleCount).IsEqualTo(0);
    }

    [Test]
    public void AdvanceFromIntroMusic_SetsShowOpening()
    {
        var repo = new CallerRepository();
        var coordinator = new BroadcastCoordinator();
        var manager = new BroadcastStateManager(repo, coordinator);

        manager.SetState(BroadcastCoordinator.BroadcastState.IntroMusic);
        manager.AdvanceState();

        AssertThat(manager.CurrentState).IsEqualTo(BroadcastCoordinator.BroadcastState.ShowOpening);
    }
}