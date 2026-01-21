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
        AssertAreEqual(BroadcastCoordinator.BroadcastState.Conversation, manager.CurrentState);
    }

    [Test]
    public void ResetFillerCycleCount_SetsToZero()
    {
        var repo = new CallerRepository();
        var coordinator = new BroadcastCoordinator();
        var manager = new BroadcastStateManager(repo, coordinator);

        manager.IncrementFillerCycle();
        AssertAreEqual(1, manager.FillerCycleCount);

        manager.ResetFillerCycleCount();
        AssertAreEqual(0, manager.FillerCycleCount);
    }

    [Test]
    public void AdvanceFromIntroMusic_SetsShowOpening()
    {
        var repo = new CallerRepository();
        var coordinator = new BroadcastCoordinator();
        var manager = new BroadcastStateManager(repo, coordinator);

        manager.SetState(BroadcastCoordinator.BroadcastState.IntroMusic);
        manager.AdvanceState();

        AssertAreEqual(BroadcastCoordinator.BroadcastState.ShowOpening, manager.CurrentState);
    }
}