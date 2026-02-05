using System;
using Godot;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Who is speaking in the conversation.
    /// </summary>
    public enum Speaker
    {
        /// <summary>Bumper music is playing</summary>
        Music,
        /// <summary>The caller on the line</summary>
        Caller,
        /// <summary>Vern Tell, the host</summary>
        Vern,
        /// <summary>System messages (ad breaks, etc.)</summary>
        System
    }

    /// <summary>
    /// The phase of the conversation structure.
    /// Each call follows this progression.
    /// </summary>
    public enum ConversationPhase
    {
        /// <summary>Vern introduces caller, caller makes initial claim</summary>
        Intro,
        /// <summary>Vern asks follow-up, caller elaborates</summary>
        Probe,
        /// <summary>Vern challenges or accepts, caller responds</summary>
        Challenge,
        /// <summary>Wrapping up the call</summary>
        Resolution
    }

    /// <summary>
    /// The section of a conversation arc where a dialogue line originates.
    /// Used for audio file lookup.
    /// </summary>
    public enum ArcSection
    {
        /// <summary>Opening section - Vern introduces caller</summary>
        Intro,
        /// <summary>Middle section - Caller elaborates on their story</summary>
        Development,
        /// <summary>Closing section - Wrapping up the call</summary>
        Conclusion
    }

    /// <summary>
    /// The current state of conversation playback.
    /// </summary>
    public enum ConversationState
    {
        /// <summary>Not started yet</summary>
        NotStarted,
        /// <summary>Currently playing through lines</summary>
        Playing,
        /// <summary>Paused (e.g., waiting for event)</summary>
        Paused,
        /// <summary>Finished all lines</summary>
        Completed
    }

    /// <summary>
    /// A template for a single dialogue line with selection weight.
    /// Used for Vern's broadcast lines (show opening, filler, etc.).
    /// </summary>
    [Serializable]
    public partial class DialogueTemplate : Resource
    {
    /// <summary>Unique identifier for this template (e.g., "vern_opening_001"). Used for audio file lookup.</summary>
    [Export] public string Id;

    /// <summary>The dialogue text (supports placeholders like {callerName}).</summary>
    [Export] public string Text;

    /// <summary>Selection weight for random picking (higher = more likely).</summary>
    [Export] public float Weight = 1f;

    /// <summary>Vern's mood when this line should be used (optional - for mood-based selection).</summary>
    [Export] public string Mood;

    /// <summary>The show topic when this line should be used (optional - for topic-based selection).</summary>
    [Export] public string Topic;

        public DialogueTemplate() { }

        public DialogueTemplate(string text, float weight = 1f)
        {
            Text = text;
            Weight = weight;
        }

        public DialogueTemplate(string id, string text, float weight = 1f, string mood = "", string topic = "")
        {
            Id = id;
            Text = text;
            Weight = weight;
            Mood = mood;
            Topic = topic;
        }
    }

    /// <summary>
    /// A single line of dialogue in a conversation.
    /// </summary>
    [Serializable]
    public partial class DialogueLine : Resource
    {
        [Export] public Speaker Speaker;
        [Export] public string Text;
        [Export] public ConversationPhase Phase;
    }

    /// <summary>
    /// Utility methods for dialogue systems.
    /// </summary>
    public static class DialogueUtility
    {
        /// <summary>
        /// Select a random item from an array using weighted probability.
        /// Items with higher weights are more likely to be selected.
        /// </summary>
        /// <typeparam name="T">The type of items in the array.</typeparam>
        /// <param name="items">Array of items to select from.</param>
        /// <param name="weightSelector">Function to extract the weight from each item.</param>
        /// <returns>A randomly selected item, or default if array is null/empty.</returns>
        public static T GetWeightedRandom<T>(T[] items, Func<T, float> weightSelector)
        {
            if (items == null || items.Length == 0)
                return default;

            float totalWeight = 0f;
            foreach (var item in items)
            {
                totalWeight += weightSelector(item);
            }

            float random = GD.Randf() * totalWeight;
            float current = 0f;

            foreach (var item in items)
            {
                current += weightSelector(item);
                if (random <= current)
                    return item;
            }

            return items[items.Length - 1];
        }

        /// <summary>
        /// Get a weighted random DialogueTemplate from an array.
        /// Convenience overload for the common case.
        /// </summary>
        public static DialogueTemplate GetWeightedRandom(DialogueTemplate[] templates)
        {
            return GetWeightedRandom(templates, t => t.Weight);
        }
    }
}