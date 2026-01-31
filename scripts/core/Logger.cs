#nullable enable

using Godot;

namespace KBTV.Core
{
    /// <summary>
    /// Simple conditional debug logger for performance optimization.
    /// Replaces excessive GD.Print calls with conditional logging.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Enable/disable debug logging. Disabled in production for performance.
        /// </summary>
        public static bool IsDebugEnabled { get; set; } = OS.IsDebugBuild();

        /// <summary>
        /// Log debug message if debug logging is enabled.
        /// </summary>
        public static void Debug(string message)
        {
            if (IsDebugEnabled)
            {
                GD.Print(message);
            }
        }

        /// <summary>
        /// Log error message (always enabled).
        /// </summary>
        public static void Error(string message)
        {
            GD.PrintErr(message);
        }

        /// <summary>
        /// Log warning message (always enabled).
        /// </summary>
        public static void Warning(string message)
        {
            GD.PushWarning(message);
        }
    }
}