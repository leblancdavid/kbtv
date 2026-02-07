using System;
using System.Linq;
using Godot;
using KBTV.Dialogue;
using KBTV.Callers;
using KBTV.Core;

namespace KBTV.Testing
{
    public partial class TopicDialogueTest : Node
    {
        public override void _Ready()
        {
            Log.Debug("=== Topic-Based Dialogue Test ===");

            // Test topic enum
            Log.Debug("Available topics:");
            foreach (ShowTopic topic in Enum.GetValues(typeof(ShowTopic)))
            {
                Log.Debug($"  - {topic.ToTopicName()}");
            }

            // Test dialogue loading
            var loader = new VernDialogueLoader();
            loader.LoadDialogue();
            var template = loader.VernDialogue;

            if (template == null)
            {
                Log.Debug("ERROR: Failed to load dialogue template");
                return;
            }

            Log.Debug("Dialogue template loaded successfully");

            // Test topic-based selection for each topic
            foreach (ShowTopic topic in Enum.GetValues(typeof(ShowTopic)))
            {
                Log.Debug($"\n=== Testing {topic.ToTopicName()} topic ===");

                // Test each line type
                TestLineType(template, "Opening", topic, () => template.GetShowOpening(topic));
                TestLineType(template, "Closing", topic, () => template.GetShowClosing(topic));
                TestLineType(template, "Dead Air Filler", topic, () => template.GetDeadAirFiller(topic));
                TestLineType(template, "Return from Break", topic, () => template.GetReturnFromBreak(topic));
            }

            Log.Debug("\n=== Test Complete ===");
        }

        private void TestLineType(VernDialogueTemplate template, string lineTypeName, ShowTopic topic, Func<DialogueTemplate> selector)
        {
            try
            {
                var line = selector();
                if (line != null)
                {
                    Log.Debug($"  {lineTypeName}: {line.Id} (Topic: {line.Topic}, Mood: {line.Mood})");
                    Log.Debug($"    Text: {line.Text}");
                }
                else
                {
                    Log.Debug($"  {lineTypeName}: NULL (no line found for {topic.ToTopicName()})");
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"  {lineTypeName}: ERROR - {ex.Message}");
            }
        }
    }
}