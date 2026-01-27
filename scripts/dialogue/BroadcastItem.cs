#nullable enable

using System;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Defines a broadcast item that can be played/executed during the show.
    /// </summary>
    public class BroadcastItem
    {
        public string Id { get; }
        public BroadcastItemType Type { get; }
        public string Text { get; }
        public string? AudioPath { get; }
        public float Duration { get; }  // For non-audio items or fallback
        public object? Metadata { get; }  // Additional data (mood, speaker, etc.)

        public BroadcastItem(string id, BroadcastItemType type, string text,
                           string? audioPath = null, float duration = 0, object? metadata = null)
        {
            Id = id;
            Type = type;
            Text = text;
            AudioPath = audioPath;
            Duration = duration;
            Metadata = metadata;
        }
    }

    /// <summary>
    /// Types of broadcast items that can be executed.
    /// </summary>
    public enum BroadcastItemType
    {
        Music,          // Background music, transitions
        VernLine,        // Vern's dialogue lines
        CallerLine,      // Caller dialogue lines
        Conversation,   // Full conversation arcs (Vern + Caller dialogue)
        Ad,             // Commercial advertisements
        DeadAir,        // Filler content when nothing else to play
        Transition,     // State transition indicators
        PutOnAir        // Putting a caller on air
    }
}