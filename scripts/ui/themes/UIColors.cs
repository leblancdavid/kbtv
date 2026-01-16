using Godot;
using KBTV.Callers;

namespace KBTV.UI.Themes
{
    /// <summary>
    /// Centralized semantic color definitions for UI components.
    /// Provides consistent colors across all UI elements.
    /// </summary>
    public static class UIColors
    {
        // Base dark theme colors
        public static readonly Color BG_DARK = new(0.1f, 0.1f, 0.1f);
        public static readonly Color BG_PANEL = new(0.15f, 0.15f, 0.15f);
        public static readonly Color BG_BORDER = new(0.2f, 0.2f, 0.2f);
        public static readonly Color BG_HOVER = new(0.2f, 0.2f, 0.2f);
        public static readonly Color BG_DISABLED = new(0.08f, 0.08f, 0.08f);

        public static readonly Color TEXT_PRIMARY = new(0.9f, 0.9f, 0.9f);
        public static readonly Color TEXT_SECONDARY = new(0.7f, 0.7f, 0.7f);
        public static readonly Color TEXT_DISABLED = new(0.5f, 0.5f, 0.5f);

        // Screening UI colors
        public static class Screening
        {
            public static readonly Color Background = new(0.15f, 0.15f, 0.15f);
            public static readonly Color Selected = new(0.2f, 0.5f, 0.2f);
            public static readonly Color Default = new(0.2f, 0.2f, 0.2f);
            public static readonly Color SelectedText = new(0.9f, 0.9f, 0.9f);
            public static readonly Color DefaultText = new(0.7f, 0.7f, 0.7f);
            public static readonly Color HeaderText = new(0f, 1f, 0f);
        }

        // Patience indicator colors
        public static class Patience
        {
            public static readonly Color High = new(0f, 1f, 0f);     // > 66%
            public static readonly Color Medium = new(1f, 1f, 0f);   // 33-66%
            public static readonly Color Low = new(1f, 0f, 0f);      // < 33%
            public static readonly Color Critical = new(1f, 0f, 0f);
        }

        // Queue state colors
        public static class Queue
        {
            public static readonly Color Incoming = new(1f, 0.7f, 0f);
            public static readonly Color OnHold = new(0f, 0.7f, 1f);
            public static readonly Color OnAir = new(0.2f, 0.8f, 0.2f);
            public static readonly Color Rejected = new(0.8f, 0.2f, 0.2f);
            public static readonly Color Screening = new(0.2f, 0.5f, 0.2f);
        }

        // Button colors
        public static class Button
        {
            public static readonly Color Approve = new(0f, 0.8f, 0f);
            public static readonly Color Reject = new(0.8f, 0.2f, 0.2f);
            public static readonly Color ApproveText = new(0.9f, 0.9f, 0.9f);
            public static readonly Color RejectText = new(0.9f, 0.9f, 0.9f);
        }

        // Panel accent colors
        public static class Accent
        {
            public static readonly Color Gold = new(1f, 0.7f, 0f);
            public static readonly Color Red = new(0.8f, 0.2f, 0.2f);
            public static readonly Color Green = new(0.2f, 0.8f, 0.2f);
            public static readonly Color Blue = new(0.2f, 0.4f, 0.8f);
        }

        // Legend colors for reference
        public static Color GetQueueStateColor(CallerState state)
        {
            return state switch
            {
                CallerState.Incoming => Queue.Incoming,
                CallerState.Screening => Queue.Screening,
                CallerState.OnHold => Queue.OnHold,
                CallerState.OnAir => Queue.OnAir,
                CallerState.Completed => Accent.Green,
                CallerState.Rejected => Queue.Rejected,
                CallerState.Disconnected => Accent.Red,
                _ => TEXT_SECONDARY
            };
        }

        public static Color GetPatienceColor(float patienceRatio)
        {
            if (patienceRatio > 0.66f)
                return Patience.High;
            if (patienceRatio > 0.33f)
                return Patience.Medium;
            return Patience.Low;
        }
    }
}
