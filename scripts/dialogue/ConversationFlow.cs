#nullable enable

using System;
using System.Collections.Generic;
using KBTV.Dialogue;
using KBTV.Data;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Declarative representation of a conversation flow.
    /// Defines the sequence of dialogue steps and their execution conditions.
    /// </summary>
    public class ConversationFlow
    {
        public IReadOnlyList<ConversationStep> Steps { get; }

        public ConversationFlow(IEnumerable<ConversationStep> steps)
        {
            Steps = new List<ConversationStep>(steps);
        }

        /// <summary>
        /// Creates a simple linear conversation flow from dialogue lines.
        /// </summary>
        public static ConversationFlow CreateLinear(IEnumerable<ArcDialogueLine> lines)
        {
            var steps = new List<ConversationStep>();
            foreach (var line in lines)
            {
                steps.Add(ConversationStep.CreateDialogue(line));
            }
            return new ConversationFlow(steps);
        }
    }

    /// <summary>
    /// Represents a single step in a conversation flow.
    /// Can be dialogue, branching decisions, or other conversation elements.
    /// </summary>
    public abstract class ConversationStep
    {
        public abstract ConversationStepType Type { get; }
        public abstract bool CanExecute(ConversationContext context);

        /// <summary>
        /// Creates a dialogue step from an arc dialogue line.
        /// </summary>
        public static ConversationStep CreateDialogue(ArcDialogueLine line)
        {
            return new DialogueStep(line);
        }

        /// <summary>
        /// Creates a conditional branching step.
        /// </summary>
        public static ConversationStep CreateBranch(
            Func<ConversationContext, bool> condition,
            ConversationStep trueStep,
            ConversationStep falseStep)
        {
            return new BranchStep(condition, trueStep, falseStep);
        }
    }

    /// <summary>
    /// Types of conversation steps.
    /// </summary>
    public enum ConversationStepType
    {
        Dialogue,
        Branch,
        Action
    }

    /// <summary>
    /// Context information available during conversation execution.
    /// </summary>
    public class ConversationContext
    {
        public int CurrentStepIndex { get; set; }
        public VernMoodType CurrentMood { get; set; }
        public Speaker LastSpeaker { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public IReadOnlyDictionary<string, object> Variables { get; }

        public ConversationContext(VernMoodType mood, IReadOnlyDictionary<string, object>? variables = null)
        {
            CurrentMood = mood;
            Variables = variables ?? new Dictionary<string, object>();
        }
    }
}