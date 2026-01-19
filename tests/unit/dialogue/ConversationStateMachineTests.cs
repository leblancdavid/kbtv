#nullable enable

using Chickensoft.GoDotTest;
using Godot;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Dialogue;

public class ConversationStateMachineTests : KBTVTestClass
{
    public ConversationStateMachineTests(Node testScene) : base(testScene) { }

    [Test]
    public void InitialState_IsIdle()
    {
        var stateMachine = new ConversationStateMachine();
        AssertThat(stateMachine.CurrentState == ConversationFlowState.Idle);
    }

    [Test]
    public void ProcessEvent_Start_TransitionsToWaitingForLine()
    {
        var stateMachine = new ConversationStateMachine();
        var newState = stateMachine.ProcessEvent(ConversationEvent.Start());

        AssertThat(newState == ConversationFlowState.WaitingForLine);
        AssertThat(stateMachine.CurrentState == ConversationFlowState.WaitingForLine);
    }

    [Test]
    public void ProcessEvent_LineAvailable_TransitionsToPlaying()
    {
        var stateMachine = new ConversationStateMachine();
        stateMachine.ProcessEvent(ConversationEvent.Start());

        var broadcastLine = BroadcastLine.VernDialogue("Test", ConversationPhase.Probe, "test_arc");
        var newState = stateMachine.ProcessEvent(ConversationEvent.LineAvailable(broadcastLine));

        AssertThat(newState == ConversationFlowState.Playing);
        AssertThat(stateMachine.CurrentState == ConversationFlowState.Playing);
    }

    [Test]
    public void ProcessEvent_LineCompleted_TransitionsToWaitingForLine()
    {
        var stateMachine = new ConversationStateMachine();
        stateMachine.ProcessEvent(ConversationEvent.Start());

        var broadcastLine = BroadcastLine.VernDialogue("Test", ConversationPhase.Probe, "test_arc");
        stateMachine.ProcessEvent(ConversationEvent.LineAvailable(broadcastLine));

        var newState = stateMachine.ProcessEvent(ConversationEvent.LineCompleted());

        AssertThat(newState == ConversationFlowState.WaitingForLine);
        AssertThat(stateMachine.CurrentState == ConversationFlowState.WaitingForLine);
    }

    [Test]
    public void CanProcessLines_ReturnsTrue_WhenWaitingForLine()
    {
        var stateMachine = new ConversationStateMachine();
        stateMachine.ProcessEvent(ConversationEvent.Start());

        AssertThat(stateMachine.CanProcessLines);
    }

    [Test]
    public void CanProcessLines_ReturnsFalse_WhenPlaying()
    {
        var stateMachine = new ConversationStateMachine();
        stateMachine.ProcessEvent(ConversationEvent.Start());

        var broadcastLine = BroadcastLine.VernDialogue("Test", ConversationPhase.Probe, "test_arc");
        stateMachine.ProcessEvent(ConversationEvent.LineAvailable(broadcastLine));

        AssertThat(!stateMachine.CanProcessLines);
    }

    [Test]
    public void IsActive_ReturnsTrue_WhenNotIdleOrCompleted()
    {
        var stateMachine = new ConversationStateMachine();
        stateMachine.ProcessEvent(ConversationEvent.Start());

        AssertThat(stateMachine.IsActive);
    }

    [Test]
    public void IsActive_ReturnsFalse_WhenIdle()
    {
        var stateMachine = new ConversationStateMachine();

        AssertThat(!stateMachine.IsActive);
    }

    [Test]
    public void ProcessEvent_End_TransitionsToCompleted()
    {
        var stateMachine = new ConversationStateMachine();
        var newState = stateMachine.ProcessEvent(ConversationEvent.End());

        AssertThat(newState == ConversationFlowState.Completed);
        AssertThat(stateMachine.CurrentState == ConversationFlowState.Completed);
    }
}