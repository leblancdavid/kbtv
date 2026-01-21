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
        AssertThat(coordinator.IsAdBreakActive);
        AssertAreEqual("Local Business", coordinator.CurrentAdSponsor);
    }

    [Test]
    public void GetAdBreakLine_ReturnsAdLine()
    {
        var adManager = new AdManager();
        var dialogue = new VernDialogueTemplate();
        var coordinator = new AdBreakCoordinator(adManager, dialogue);

        coordinator.OnAdBreakStarted();
        var line = coordinator.GetAdBreakLine();

        AssertAreEqual(BroadcastLineType.Ad, line.Type);
        AssertThat(line.Text.Contains("Local Business"));
    }
}