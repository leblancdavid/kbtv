using System;
using System.Linq;
using Godot;
using KBTV.Dialogue;
using KBTV.Callers;

namespace KBTV.Testing
{
    public partial class TopicDialogueTest : Node
    {
        public override void _Ready()
        {
            GD.Print("=== Topic-Based Dialogue Test ===");

            // Test topic enum
            GD.Print("Available topics:");
            foreach (ShowTopic topic in Enum.GetValues(typeof(ShowTopic)))
            {
                GD.Print($"  - {topic.ToTopicName()}");
            }

            // Test dialogue loading
            var loader = new VernDialogueLoader();
            loader.LoadDialogue();
            var template = loader.VernDialogue;

            if (template == null)
            {
                GD.Print("ERROR: Failed to load dialogue template");
                return;
            }

            GD.Print("Dialogue template loaded successfully");

            // Test topic-based selection for each topic
            foreach (ShowTopic topic in Enum.GetValues(typeof(ShowTopic)))
            {
                GD.Print($"\n=== Testing {topic.ToTopicName()} topic ===");

                // Test each line type
                TestLineType(template, "Opening", topic, () => template.GetShowOpening(topic));
                TestLineType(template, "Closing", topic, () => template.GetShowClosing(topic));
                TestLineType(template, "Dead Air Filler", topic, () => template.GetDeadAirFiller(topic));
                TestLineType(template, "Return from Break", topic, () => template.GetReturnFromBreak(topic));
            }

            GD.Print("\n=== Test Complete ===");
        }

        private void TestLineType(VernDialogueTemplate template, string lineTypeName, ShowTopic topic, Func<DialogueTemplate> selector)
        {
            try
            {
                var line = selector();
                if (line != null)
                {
                    GD.Print($"  {lineTypeName}: {line.Id} (Topic: {line.Topic}, Mood: {line.Mood})");
                    GD.Print($"    Text: {line.Text}");
                }
                else
                {
                    GD.Print($"  {lineTypeName}: NULL (no line found for {topic.ToTopicName()})");
                }
            }
            catch (Exception ex)
            {
                GD.Print($"  {lineTypeName}: ERROR - {ex.Message}");
            }
        }
    }
}