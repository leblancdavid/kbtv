#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Registry for managing broadcast items.
    /// Loads items from JSON config and provides lookup by ID.
    /// </summary>
    public class BroadcastItemRegistry
    {
        private readonly Dictionary<string, BroadcastItem> _items = new();
        private int _fillerCycleIndex = 0;
        private readonly string[] _fillerItemIds = { "dead_air_filler", "dead_air_filler_2", "dead_air_filler_3" };

        public BroadcastItemRegistry()
        {
            LoadDefaultItems();
        }

        public BroadcastItem? GetItem(string id)
        {
            return _items.TryGetValue(id, out var item) ? item : null;
        }

        public BroadcastItem? GetNextDeadAirFiller()
        {
            var itemId = _fillerItemIds[_fillerCycleIndex % _fillerItemIds.Length];
            _fillerCycleIndex++;
            return GetItem(itemId);
        }

        public IEnumerable<BroadcastItem> GetAllItems()
        {
            return _items.Values;
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

            // Vern opening lines
            _items["vern_opening_1"] = new BroadcastItem(
                "vern_opening_1",
                BroadcastItemType.VernLine,
                "Welcome back, night owls. This is Vern Tell coming to you live from the KBTV studios.",
                "res://assets/audio/voice/Vern/Broadcast/opening_neutral_1.mp3"
            );

            // Transition items
            _items["between_callers"] = new BroadcastItem(
                "between_callers",
                BroadcastItemType.Transition,
                "Moving to the next caller...",
                duration: 2.0f
            );

            _items["dead_air_filler"] = new BroadcastItem(
                "dead_air_filler",
                BroadcastItemType.DeadAir,
                "Let me know if you've got a story to share...",
                "res://assets/audio/voice/Vern/Broadcast/deadair_1.mp3"
            );

            _items["dead_air_filler_2"] = new BroadcastItem(
                "dead_air_filler_2",
                BroadcastItemType.DeadAir,
                "We're here to listen to your stories...",
                "res://assets/audio/voice/Vern/Broadcast/deadair_2.mp3"
            );

            _items["dead_air_filler_3"] = new BroadcastItem(
                "dead_air_filler_3",
                BroadcastItemType.DeadAir,
                "Don't be shy, pick up that phone...",
                "res://assets/audio/voice/Vern/Broadcast/deadair_3.mp3"
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

            _items["show_closing"] = new BroadcastItem(
                "show_closing",
                BroadcastItemType.VernLine,
                "That's all the time we have tonight. Keep watching the skies!",
                "res://assets/audio/voice/Vern/Broadcast/closing_neutral_1.mp3"
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