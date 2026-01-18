using System;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Dialogue
{
    public class ConversationManagerTests : KBTVTestClass
    {
        public ConversationManagerTests(Node testScene) : base(testScene) { }

        [Test]
        public void BroadcastFlowState_IsActiveBroadcast_ReturnsCorrectValues()
        {
            AssertThat(!BroadcastFlowState.Idle.IsActiveBroadcast());
            AssertThat(BroadcastFlowState.ShowOpening.IsActiveBroadcast());
            AssertThat(BroadcastFlowState.Conversation.IsActiveBroadcast());
            AssertThat(BroadcastFlowState.BetweenCallers.IsActiveBroadcast());
            AssertThat(BroadcastFlowState.DeadAirFiller.IsActiveBroadcast());
            AssertThat(!BroadcastFlowState.ShowClosing.IsActiveBroadcast());
        }

        [Test]
        public void BroadcastFlowState_HasActiveConversation_ReturnsCorrectValues()
        {
            AssertThat(!BroadcastFlowState.Idle.HasActiveConversation());
            AssertThat(!BroadcastFlowState.ShowOpening.HasActiveConversation());
            AssertThat(BroadcastFlowState.Conversation.HasActiveConversation());
            AssertThat(!BroadcastFlowState.BetweenCallers.HasActiveConversation());
            AssertThat(!BroadcastFlowState.DeadAirFiller.HasActiveConversation());
            AssertThat(!BroadcastFlowState.ShowClosing.HasActiveConversation());
        }

        [Test]
        public void BroadcastFlowState_IsTransition_ReturnsCorrectValues()
        {
            AssertThat(!BroadcastFlowState.Idle.IsTransition());
            AssertThat(BroadcastFlowState.ShowOpening.IsTransition());
            AssertThat(!BroadcastFlowState.Conversation.IsTransition());
            AssertThat(BroadcastFlowState.BetweenCallers.IsTransition());
            AssertThat(!BroadcastFlowState.DeadAirFiller.IsTransition());
            AssertThat(BroadcastFlowState.ShowClosing.IsTransition());
        }

        [Test]
        public void BroadcastFlowState_GetDisplayName_ReturnsCorrectNames()
        {
            AssertThat(BroadcastFlowState.Idle.GetDisplayName() == "Idle");
            AssertThat(BroadcastFlowState.ShowOpening.GetDisplayName() == "Show Opening");
            AssertThat(BroadcastFlowState.Conversation.GetDisplayName() == "On Air");
            AssertThat(BroadcastFlowState.BetweenCallers.GetDisplayName() == "Transition");
            AssertThat(BroadcastFlowState.DeadAirFiller.GetDisplayName() == "Dead Air");
            AssertThat(BroadcastFlowState.ShowClosing.GetDisplayName() == "Show Closing");
        }

        [Test]
        public void ConversationDisplayInfo_CreateIdle_CreatesEmptyInfo()
        {
            var info = ConversationDisplayInfo.CreateIdle();

            AssertThat(info.SpeakerName.Length == 0);
            AssertThat(info.Text.Length == 0);
            AssertThat(info.FlowState == BroadcastFlowState.Idle);
            AssertThat(!info.IsTyping);
            AssertThat(!info.IsConversationActive);
        }

        [Test]
        public void ConversationDisplayInfo_CreateDeadAir_CreatesVernInfo()
        {
            var info = ConversationDisplayInfo.CreateDeadAir("Test filler");

            AssertThat(info.SpeakerName == "Vern");
            AssertThat(info.SpeakerIcon == "VERN");
            AssertThat(info.Text == "Test filler");
            AssertThat(info.FlowState == BroadcastFlowState.DeadAirFiller);
            AssertThat(info.IsTyping);
            AssertThat(!info.IsConversationActive);
        }

        [Test]
        public void ConversationDisplayInfo_CreateBroadcastLine_CreatesCorrectInfo()
        {
            var info = ConversationDisplayInfo.CreateBroadcastLine("Vern", "VERN", "Welcome", ConversationPhase.Intro);

            AssertThat(info.SpeakerName == "Vern");
            AssertThat(info.SpeakerIcon == "VERN");
            AssertThat(info.Text == "Welcome");
            AssertThat(info.Phase == ConversationPhase.Intro);
            AssertThat(info.FlowState == BroadcastFlowState.ShowOpening);
            AssertThat(info.IsTyping);
        }

        [Test]
        public void ConversationDisplayInfo_CreateConversationLine_CreatesCorrectInfo()
        {
            var info = ConversationDisplayInfo.CreateConversationLine(
                "TestCaller", "CALLER", "Hello there",
                ConversationPhase.Intro, "test_arc", 0, 4, 3.0f, 1.5f);

            AssertThat(info.SpeakerName == "TestCaller");
            AssertThat(info.SpeakerIcon == "CALLER");
            AssertThat(info.Text == "Hello there");
            AssertThat(info.Phase == ConversationPhase.Intro);
            AssertThat(info.CurrentArcId == "test_arc");
            AssertThat(info.CurrentLineIndex == 0);
            AssertThat(info.TotalLines == 4);
            AssertThat(info.CurrentLineDuration == 3.0f);
            AssertThat(info.ElapsedLineTime == 1.5f);
            AssertThat(info.Progress == 0.5f);
            AssertThat(info.IsConversationActive);
        }

        [Test]
        public void ConversationDisplayInfo_HasChanged_DetectsChanges()
        {
            var info1 = ConversationDisplayInfo.CreateBroadcastLine("A", "ICON", "Text1", ConversationPhase.Intro);
            var info2 = ConversationDisplayInfo.CreateBroadcastLine("A", "ICON", "Text1", ConversationPhase.Intro);
            var info3 = ConversationDisplayInfo.CreateBroadcastLine("B", "ICON", "Text1", ConversationPhase.Intro);

            AssertThat(!info1.HasChanged(info2));
            AssertThat(info1.HasChanged(info3));
        }

        [Test]
        public void ConversationDisplayInfo_Copy_CreatesIndependentCopy()
        {
            var original = ConversationDisplayInfo.CreateConversationLine(
                "Caller", "ICON", "Text", ConversationPhase.Probe, "arc_001", 1, 5, 2.5f, 1.0f);

            var copy = original.Copy();

            AssertThat(copy != original);
            AssertThat(copy.SpeakerName == original.SpeakerName);
            AssertThat(copy.Text == original.Text);
            AssertThat(copy.Phase == original.Phase);
        }

        [Test]
        public void ConversationPhase_AllPhasesExist()
        {
            var phases = Enum.GetValues(typeof(ConversationPhase));
            AssertThat(phases.Length == 4);

            AssertThat((ConversationPhase)phases.GetValue(0)! == ConversationPhase.Intro);
            AssertThat((ConversationPhase)phases.GetValue(1)! == ConversationPhase.Probe);
            AssertThat((ConversationPhase)phases.GetValue(2)! == ConversationPhase.Challenge);
            AssertThat((ConversationPhase)phases.GetValue(3)! == ConversationPhase.Resolution);
        }
    }
}
