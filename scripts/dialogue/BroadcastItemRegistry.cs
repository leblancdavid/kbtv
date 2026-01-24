#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using KBTV.Data;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Registry for managing broadcast items.
    /// Loads items from JSON config and provides lookup by ID.
    /// </summary>
    public class BroadcastItemRegistry
    {
        private readonly Dictionary<string, BroadcastItem> _items = new();
        private VernDialogueTemplate? _vernDialogue;

        public BroadcastItemRegistry()
        {
            LoadDefaultItems();
        }

        public void SetVernDialogueTemplate(VernDialogueTemplate vernDialogue)
        {
            _vernDialogue = vernDialogue;
        }

        public BroadcastItem? GetItem(string id)
        {
            return _items.TryGetValue(id, out var item) ? item : null;
        }

        public BroadcastItem? GetNextDeadAirFiller()
        {
            // Dead air fillers now use dynamic selection from VernDialogueTemplate
            return GetVernItem("dead_air_filler", VernMoodType.Neutral);
        }

        public IEnumerable<BroadcastItem> GetAllItems()
        {
            return _items.Values;
        }

        public BroadcastItem? GetVernItem(string type, VernMoodType mood)
        {
            if (_vernDialogue == null)
            {
                GD.PrintErr("BroadcastItemRegistry: VernDialogueTemplate not set");
                return null;
            }

            DialogueTemplate dialogueTemplate;
            string audioPath;

            switch (type)
            {
                case "opening":
                    dialogueTemplate = _vernDialogue.GetShowOpening();
                    audioPath = $"res://assets/audio/voice/Vern/Broadcast/{dialogueTemplate.Id}.mp3";
                    return new BroadcastItem(
                        dialogueTemplate.Id,
                        BroadcastItemType.VernLine,
                        dialogueTemplate.Text,
                        audioPath
                    );

                case "closing":
                    dialogueTemplate = _vernDialogue.GetShowClosing(mood);
                    audioPath = $"res://assets/audio/voice/Vern/Broadcast/{dialogueTemplate.Id}.mp3";
                    return new BroadcastItem(
                        dialogueTemplate.Id,
                        BroadcastItemType.VernLine,
                        dialogueTemplate.Text,
                        audioPath
                    );

                case "between_callers":
                    dialogueTemplate = _vernDialogue.GetBetweenCallers(mood);
                    audioPath = $"res://assets/audio/voice/Vern/Broadcast/{dialogueTemplate.Id}.mp3";
                    return new BroadcastItem(
                        dialogueTemplate.Id,
                        BroadcastItemType.Transition,
                        dialogueTemplate.Text,
                        audioPath
                    );

                case "dead_air_filler":
                    dialogueTemplate = _vernDialogue.GetDeadAirFiller();
                    audioPath = $"res://assets/audio/voice/Vern/Broadcast/{dialogueTemplate.Id}.mp3";
                    return new BroadcastItem(
                        dialogueTemplate.Id,
                        BroadcastItemType.DeadAir,
                        dialogueTemplate.Text,
                        audioPath
                    );

                case "break_transition":
                    dialogueTemplate = _vernDialogue.GetBreakTransition();
                    audioPath = $"res://assets/audio/voice/Vern/Broadcast/{dialogueTemplate.Id}.mp3";
                    return new BroadcastItem(
                        dialogueTemplate.Id,
                        BroadcastItemType.Transition,
                        dialogueTemplate.Text,
                        audioPath
                    );

                case "return_from_break":
                    dialogueTemplate = _vernDialogue.GetReturnFromBreak(mood);
                    audioPath = $"res://assets/audio/voice/Vern/Broadcast/{dialogueTemplate.Id}.mp3";
                    return new BroadcastItem(
                        dialogueTemplate.Id,
                        BroadcastItemType.Transition,
                        dialogueTemplate.Text,
                        audioPath
                    );

                case "off_topic_remark":
                    dialogueTemplate = _vernDialogue.GetOffTopicRemark(mood);
                    audioPath = $"res://assets/audio/voice/Vern/Broadcast/{dialogueTemplate.Id}.mp3";
                    return new BroadcastItem(
                        dialogueTemplate.Id,
                        BroadcastItemType.VernLine,
                        dialogueTemplate.Text,
                        audioPath
                    );

                default:
                    GD.PrintErr($"BroadcastItemRegistry: Unknown Vern item type '{type}'");
                    return null;
            }
        }

        private void LoadDefaultItems()
        {
            // Music items
            _items["music_intro"] = new BroadcastItem(
                "music_intro",
                BroadcastItemType.Music,
                "Intro Music",
                "res://assets/audio/music/intro_music.wav"
            );

            _items["music_outro"] = new BroadcastItem(
                "music_outro",
                BroadcastItemType.Music,
                "Outro Music",
                "res://assets/audio/bumpers/outro.mp3"
            );

            _items["break_return_music"] = new BroadcastItem(
                "break_return_music",
                BroadcastItemType.Music,
                "Return from Break Music",
                "res://assets/audio/bumpers/return.mp3"
            );



            // Transition items
            _items["between_callers"] = new BroadcastItem(
                "between_callers",
                BroadcastItemType.Transition,
                "Moving to the next caller...",
                duration: 2.0f
            );



            _items["break_start"] = new BroadcastItem(
                "break_start",
                BroadcastItemType.Transition,
                "We'll be right back after these messages.",
                duration: 1.5f
            );

            _items["ad_break"] = new BroadcastItem(
                "ad_break",
                BroadcastItemType.Ad,
                "[Commercial Break]",
                duration: 30.0f  // Placeholder duration
            );



            // Placeholder for conversation items (will be replaced with dynamic content)
            _items["conversation_placeholder"] = new BroadcastItem(
                "conversation_placeholder",
                BroadcastItemType.VernLine,
                "Tell me more about your experience...",
                null  // No audio - uses 4-second timer fallback
            );
        }

        /// <summary>
        /// Load additional items from JSON config (future enhancement).
        /// </summary>
        public void LoadFromJson(string jsonPath)
        {
            // TODO: Implement JSON loading for custom broadcast items
            GD.Print($"BroadcastItemRegistry: TODO - Load items from {jsonPath}");
        }
    }
}