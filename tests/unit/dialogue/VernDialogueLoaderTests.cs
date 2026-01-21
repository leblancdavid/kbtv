using Chickensoft.GoDotTest;
using Godot;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Dialogue;

public class VernDialogueLoaderTests : KBTVTestClass
{
    public VernDialogueLoaderTests(Node testScene) : base(testScene) { }

    [Test]
    public void LoadDialogue_LoadsTemplates()
    {
        var loader = new VernDialogueLoader();
        loader.LoadDialogue();

        var dialogue = loader.VernDialogue;
        AssertThat(dialogue).IsNotNull();
        AssertThat(dialogue.GetShowOpeningLines()).IsNotNull();
        AssertThat(dialogue.GetBetweenCallersLines()).IsNotNull();
    }
}