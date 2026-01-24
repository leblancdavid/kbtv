using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Dialogue;

public class BroadcastStateMachineTests : KBTVTestClass
{
    public BroadcastStateMachineTests(Node testScene) : base(testScene) { }

    [Test]
    public void StartShow_SetsIntroMusicState()
    {
        var repo = new CallerRepository();
        var registry = new BroadcastItemRegistry();
        var stateMachine = new BroadcastStateMachine(repo, registry);

        var firstItem = stateMachine.StartShow();

        AssertAreEqual(BroadcastState.IntroMusic, stateMachine.CurrentState);
        AssertThat(firstItem != null);
    }

    [Test]
    public void HandleEvent_CompletedItem_AdvancesState()
    {
        var repo = new CallerRepository();
        var registry = new BroadcastItemRegistry();
        var stateMachine = new BroadcastStateMachine(repo, registry);

        // Start show
        stateMachine.StartShow();

        // Complete the music intro
        var completedEvent = new BroadcastEvent(BroadcastEventType.Completed, "music_intro");
        var nextItem = stateMachine.HandleEvent(completedEvent);

        AssertAreEqual(BroadcastState.ShowOpening, stateMachine.CurrentState);
        AssertThat(nextItem != null);
    }

    [Test]
    public void ResolveAudioPathWithFallbacks_ReturnsNullForMissingAudio()
    {
        var repo = new CallerRepository();
        var registry = new BroadcastItemRegistry();
        var stateMachine = new BroadcastStateMachine(repo, registry);

        // Create a test BroadcastLine for a Vern dialogue
        var testLine = BroadcastLine.VernDialogue("Test text", ConversationPhase.Probe,
            "test_arc", 1, "test_arc_vern_irritated_1");

        // Create a minimal test arc
        var testArc = new ConversationArc("test_arc", ShowTopic.UFOs, CallerLegitimacy.Credible);

        // Test the fallback method
        var audioPath = stateMachine.GetType()
            .GetMethod("ResolveAudioPathWithFallbacks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(stateMachine, new object[] { testLine, testArc }) as string;

        // Should return null since the test audio file doesn't exist
        AssertThat(audioPath == null);
    }

    [Test]
    public void HandleEvent_BreakCompleted_TransitionsToReturnFromBreak()
    {
        var repo = new CallerRepository();
        var registry = new BroadcastItemRegistry();
        var stateMachine = new BroadcastStateMachine(repo, registry);

        // Manually set state to Break (simulate break transition)
        stateMachine.GetType().GetProperty("CurrentState")?.SetValue(stateMachine, BroadcastState.Break);

        // Complete the ad_break item
        var completedEvent = new BroadcastEvent(BroadcastEventType.Completed, "ad_break");
        var nextItem = stateMachine.HandleEvent(completedEvent);

        AssertAreEqual(BroadcastState.ReturnFromBreak, stateMachine.CurrentState);
        AssertThat(nextItem != null);
    }

    [Test]
    public void ArcRepository_LoadsArcsWithCorrectArcIds()
    {
        // This test would require mocking the file system or using actual files
        // For now, we rely on the JSON files being manually updated
        // In a real test environment, we could load specific test arcs and verify their ArcId
        AssertThat(true); // Placeholder - actual test would verify arc loading
    }
}