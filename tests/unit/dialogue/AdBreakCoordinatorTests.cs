using Chickensoft.GoDotTest;
using Godot;
using KBTV.Ads;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Dialogue;

public class AdBreakCoordinatorTests : KBTVTestClass
{
    public AdBreakCoordinatorTests(Node testScene) : base(testScene) { }

    [Test]
    public void OnAdBreakStarted_SetsActiveState()
    {
        var adManager = new AdManager();
        var dialogue = new VernDialogueTemplate();
        var coordinator = new AdBreakCoordinator(adManager, dialogue);

        coordinator.OnAdBreakStarted();
        AssertThat(coordinator.IsAdBreakActive).IsTrue();
        AssertThat(coordinator.CurrentAdSponsor).IsEqualTo("Local Business");
    }

    [Test]
    public void GetAdBreakLine_ReturnsAdLine()
    {
        var adManager = new AdManager();
        var dialogue = new VernDialogueTemplate();
        var coordinator = new AdBreakCoordinator(adManager, dialogue);

        coordinator.OnAdBreakStarted();
        var line = coordinator.GetAdBreakLine();

        AssertThat(line.Type).IsEqualTo(BroadcastLineType.Ad);
        AssertThat(line.Text).Contains("Local Business");
    }
}