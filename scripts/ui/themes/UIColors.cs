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

        // Stat effect indicator colors (for screening property reveals)
        public static class StatEffect
        {
            public static readonly Color Positive = new(0.2f, 0.9f, 0.2f);  // Green for buffs
            public static readonly Color Negative = new(0.9f, 0.2f, 0.2f);  // Red for debuffs
            public static readonly Color Neutral = new(0.7f, 0.7f, 0.7f);   // Gray for no effect
        }

        // Placeholder text colors (for unrevealed/hidden properties)
        public static class Placeholder
        {
            public static readonly Color Text = new(0.4f, 0.4f, 0.4f);       // Muted gray for hidden
            public static readonly Color Revealing = new(0.6f, 0.6f, 0.6f);  // Lighter when revealing
        }

        // Property display colors (for screening property rows)
        public static class Property
        {
            public static readonly Color Name = new(0f, 0.7f, 1f);           // Cyan for property names
            public static readonly Color Value = TEXT_PRIMARY;                // White for values
        }

        // Vern stat display colors
        public static class VernStat
        {
            public static readonly Color High = new(0.2f, 0.9f, 0.2f);        // Green > 66%
            public static readonly Color Medium = new(0.9f, 0.9f, 0.2f);      // Yellow 33-66%
            public static readonly Color Low = new(0.9f, 0.2f, 0.2f);         // Red < 33%
            public static readonly Color SpiritPositive = new(0.2f, 0.9f, 0.2f);  // Green for positive Spirit
            public static readonly Color SpiritNegative = new(0.9f, 0.2f, 0.2f);  // Red for negative Spirit
            public static readonly Color SpiritNeutral = new(0.5f, 0.5f, 0.5f);   // Gray for zero Spirit
            public static readonly Color CategoryHeader = new(0.6f, 0.6f, 0.6f);  // Muted for group headers
            public static readonly Color VibePositive = new(0.2f, 0.9f, 0.2f);    // Green for positive VIBE
            public static readonly Color VibeNegative = new(0.9f, 0.2f, 0.2f);    // Red for negative VIBE
            public static readonly Color VibeNeutral = new(0.9f, 0.9f, 0.2f);     // Yellow for neutral VIBE
            public static readonly Color BarBackground = new(0.2f, 0.2f, 0.2f);   // Dark background for bars
        }

        // Warning/alert colors for status panel
        public static class Warning
        {
            public static readonly Color Critical = new(0.9f, 0.2f, 0.2f);    // Red for critical warnings
            public static readonly Color Caution = new(0.9f, 0.7f, 0.2f);     // Orange/yellow for caution
            public static readonly Color Info = new(0.5f, 0.7f, 0.9f);        // Light blue for info
            public static readonly Color Good = new(0.2f, 0.9f, 0.2f);        // Green for positive status
        }

        // Status panel colors
        public static class Status
        {
            public static readonly Color SectionHeader = new(0.7f, 0.7f, 0.7f);   // Section titles
            public static readonly Color ItemText = new(0.8f, 0.8f, 0.8f);        // Normal item text
            public static readonly Color ValueText = new(0.6f, 0.8f, 0.6f);       // Values/numbers
            public static readonly Color ModifierBuff = new(0.2f, 0.9f, 0.2f);    // Positive modifiers
            public static readonly Color ModifierDebuff = new(0.9f, 0.5f, 0.2f);  // Negative modifiers
            public static readonly Color ModifierNeutral = new(0.7f, 0.7f, 0.7f); // Neutral modifiers
        }

        /// <summary>
        /// Get color for a Vern stat based on its normalized value (0-1 range).
        /// </summary>
        public static Color GetVernStatColor(float normalizedValue)
        {
            if (normalizedValue > 0.66f)
                return VernStat.High;
            if (normalizedValue > 0.33f)
                return VernStat.Medium;
            return VernStat.Low;
        }

        /// <summary>
        /// Get color for VIBE score based on value (-100 to +100 range).
        /// </summary>
        public static Color GetVibeColor(float vibeValue)
        {
            if (vibeValue > 25f)
                return VernStat.VibePositive;
            if (vibeValue > -25f)
                return VernStat.VibeNeutral;
            return VernStat.VibeNegative;
        }

        /// <summary>
        /// Get color for Spirit stat based on value (-50 to +50 range).
        /// </summary>
        public static Color GetSpiritColor(float spiritValue)
        {
            if (spiritValue > 10f)
                return VernStat.SpiritPositive;
            if (spiritValue > -10f)
                return VernStat.SpiritNeutral;
            return VernStat.SpiritNegative;
        }

        /// <summary>
        /// Get color for centered stats based on value (-100 to +100 range).
        /// Used for Physical, Emotional, Mental stats.
        /// </summary>
        public static Color GetCenteredStatColor(float value)
        {
            if (value > 25f)
                return VernStat.SpiritPositive;   // Green for positive
            if (value > -25f)
                return VernStat.SpiritNeutral;    // Gray for neutral
            return VernStat.SpiritNegative;       // Red for negative
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
