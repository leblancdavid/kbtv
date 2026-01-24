#nullable enable

using System;
using KBTV.Dialogue;

namespace KBTV.Dialogue
{
    /// <summary>
    /// A dialogue step that plays a specific line of dialogue.
    /// </summary>
    public class DialogueStep : ConversationStep
    {
        public ArcDialogueLine DialogueLine { get; }

        public DialogueStep(ArcDialogueLine dialogueLine)
        {
            DialogueLine = dialogueLine;
        }

        public override ConversationStepType Type => ConversationStepType.Dialogue;

        public override bool CanExecute(ConversationContext context)
        {
            // Dialogue steps can always execute
            return true;
        }

        public BroadcastLine CreateBroadcastLine(string? arcId = null, string? callerGender = null, int lineIndex = 0, string? mood = null)
        {
            // Construct the line ID for audio file lookup
            // Format: {arcId}_{speaker}_{mood}_{lineIndex}
            var actualMood = mood ?? "neutral"; // Default to neutral if not provided
            var speakerStr = DialogueLine.Speaker == Speaker.Vern ? "vern" : "caller";
            var constructedId = $"{arcId}_{speakerStr}_{actualMood}_{lineIndex + 1}"; // +1 because lineIndex is 0-based

            // Convert ArcDialogueLine to BroadcastLine based on speaker
            return DialogueLine.Speaker == Speaker.Vern
                ? BroadcastLine.VernDialogue(DialogueLine.Text, ConversationPhase.Probe, arcId, lineIndex, constructedId)
                : BroadcastLine.CallerDialogue(DialogueLine.Text, "Caller", "caller", ConversationPhase.Probe, arcId, callerGender, lineIndex);
        }
    }

    /// <summary>
    /// A branching step that executes different steps based on conditions.
    /// </summary>
    public class BranchStep : ConversationStep
    {
        public Func<ConversationContext, bool> Condition { get; }
        public ConversationStep TrueStep { get; }
        public ConversationStep FalseStep { get; }

        public BranchStep(Func<ConversationContext, bool> condition, ConversationStep trueStep, ConversationStep falseStep)
        {
            Condition = condition;
            TrueStep = trueStep;
            FalseStep = falseStep;
        }

        public override ConversationStepType Type => ConversationStepType.Branch;

        public override bool CanExecute(ConversationContext context)
        {
            // Branch steps can always execute - they decide what to do next
            return true;
        }

        public ConversationStep Evaluate(ConversationContext context)
        {
            return Condition(context) ? TrueStep : FalseStep;
        }
    }

    /// <summary>
    /// An action step that performs some operation without dialogue.
    /// </summary>
    public class ActionStep : ConversationStep
    {
        public Action<ConversationContext> Action { get; }

        public ActionStep(Action<ConversationContext> action)
        {
            Action = action;
        }

        public override ConversationStepType Type => ConversationStepType.Action;

        public override bool CanExecute(ConversationContext context)
        {
            return true;
        }

        public void Execute(ConversationContext context)
        {
            Action(context);
        }
    }
}