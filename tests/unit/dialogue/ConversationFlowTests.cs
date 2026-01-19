#nullable enable

using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Data;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Dialogue;

public class ConversationFlowTests : KBTVTestClass
{
    public ConversationFlowTests(Node testScene) : base(testScene) { }

    [Test]
    public void CreateLinear_CreatesFlowWithDialogueSteps()
    {
        var dialogueLines = new List<ArcDialogueLine>
        {
            new ArcDialogueLine(Speaker.Vern, "Line 1"),
            new ArcDialogueLine(Speaker.Caller, "Line 2"),
            new ArcDialogueLine(Speaker.Vern, "Line 3")
        };

        var flow = ConversationFlow.CreateLinear(dialogueLines);

        AssertThat(flow.Steps != null);
        AssertThat(flow.Steps.Count == 3);

        AssertThat(flow.Steps[0] is DialogueStep);
        AssertThat(flow.Steps[1] is DialogueStep);
        AssertThat(flow.Steps[2] is DialogueStep);
    }

    [Test]
    public void DialogueStep_CanExecute_ReturnsTrue()
    {
        var dialogueLine = new ArcDialogueLine(Speaker.Vern, "Test line");
        var step = new DialogueStep(dialogueLine);
        var context = new ConversationContext(VernMoodType.Neutral);

        AssertThat(step.CanExecute(context));
    }

    [Test]
    public void DialogueStep_Type_IsDialogue()
    {
        var dialogueLine = new ArcDialogueLine(Speaker.Vern, "Test line");
        var step = new DialogueStep(dialogueLine);

        AssertThat(step.Type == ConversationStepType.Dialogue);
    }

    [Test]
    public void BranchStep_CanExecute_ReturnsTrue()
    {
        var trueStep = new DialogueStep(new ArcDialogueLine(Speaker.Vern, "True"));
        var falseStep = new DialogueStep(new ArcDialogueLine(Speaker.Vern, "False"));
        var step = new BranchStep(_ => true, trueStep, falseStep);
        var context = new ConversationContext(VernMoodType.Neutral);

        AssertThat(step.CanExecute(context));
    }

    [Test]
    public void BranchStep_Evaluate_ReturnsCorrectStep()
    {
        var trueStep = new DialogueStep(new ArcDialogueLine(Speaker.Vern, "True"));
        var falseStep = new DialogueStep(new ArcDialogueLine(Speaker.Vern, "False"));

        var trueBranch = new BranchStep(_ => true, trueStep, falseStep);
        var falseBranch = new BranchStep(_ => false, trueStep, falseStep);

        var context = new ConversationContext(VernMoodType.Neutral);

        AssertThat(trueBranch.Evaluate(context) == trueStep);
        AssertThat(falseBranch.Evaluate(context) == falseStep);
    }

    [Test]
    public void ConversationContext_InitializesCorrectly()
    {
        var context = new ConversationContext(VernMoodType.Irritated);

        AssertThat(context.CurrentMood == VernMoodType.Irritated);
        AssertThat(context.CurrentStepIndex == 0);
        AssertThat(context.LastSpeaker == Speaker.Caller); // Default
        AssertThat(context.Variables != null);
        AssertThat(context.Variables.Count == 0);
    }
}