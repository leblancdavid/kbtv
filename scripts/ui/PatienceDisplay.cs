using Godot;
using KBTV.Callers;
using KBTV.UI.Themes;

namespace KBTV.UI
{
    /// <summary>
    /// Shared text-based patience bar used by both the screening panel and the
    /// evidence decryption dialog so the display and timing match exactly.
    /// <see cref="Caller.ScreeningPatience"/> is already drained in real time by
    /// <c>CallerMonitor</c> (0.5x rate), so it is the single source of truth -
    /// do NOT subtract <c>ScreeningProgress.ElapsedTime</c> on top of it or the
    /// bar expires ~3x early.
    /// </summary>
    public static class PatienceDisplay
    {
        public const int BarWidth = 14;

        public static float Remaining(Caller caller) =>
            Mathf.Max(caller.ScreeningPatience, 0f);

        public static float Ratio(Caller caller)
        {
            if (caller.Patience <= 0f)
            {
                return 0f;
            }
            return Mathf.Clamp(caller.ScreeningPatience / caller.Patience, 0f, 1f);
        }

        public static string Bar(float ratio, int width = BarWidth)
        {
            int filled = Mathf.RoundToInt(Mathf.Clamp(ratio, 0f, 1f) * width);
            return "[" + new string('|', filled) + new string('.', width - filled) + "]";
        }

        public static string Text(float ratio) =>
            $"{Bar(ratio)} {(int)(ratio * 100f)}%";

        public static Color ColorFor(float ratio) => UIColors.GetPatienceColor(ratio);
    }
}
