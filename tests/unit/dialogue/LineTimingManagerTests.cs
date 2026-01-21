using Chickensoft.GoDotTest;
using Godot;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Dialogue;

public class LineTimingManagerTests : KBTVTestClass
{
    public LineTimingManagerTests(Node testScene) : base(testScene) { }

    [Test]
    public void StartLine_SetsCurrentLine()
    {
        var manager = new LineTimingManager();
        var line = BroadcastLine.VernDialogue("Test", ConversationPhase.Intro);
        manager.StartLine(line);

        var current = manager.GetCurrentLine();
        AssertNotNull(current);
        AssertAreEqual("Test", current.Value.Text);
    }

    [Test]
    public void UpdateProgress_LineCompletes_ReturnsTrue()
    {
        var manager = new LineTimingManager();
        var line = BroadcastLine.VernDialogue("Test", ConversationPhase.Intro);
        manager.StartLine(line);

        // Simulate time passing (4 seconds for VernDialogue)
        var completed = manager.UpdateProgress(4.1f);
        AssertThat(completed);
    }

    [Test]
    public void StopLine_ClearsCurrentLine()
    {
        var manager = new LineTimingManager();
        var line = BroadcastLine.VernDialogue("Test", ConversationPhase.Intro);
        manager.StartLine(line);
        manager.StopLine();

        var current = manager.GetCurrentLine();
        AssertNull(current);
    }
}