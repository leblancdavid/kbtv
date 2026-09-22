using Godot;
using KBTV.Callers;
using KBTV.Screening;
using KBTV.UI.Themes;

namespace KBTV.UI
{
    /// <summary>
    /// Shared text-based patience bar used by both the screening panel and the
    /// evidence decryption dialog so the display and timing match exactly:
    /// remaining = ScreeningPatience - ElapsedTime, ratio against ScreeningPatience.
    /// </summary>
    public static class PatienceDisplay
    {
        public const int BarWidth = 14;

        public static float Remaining(Caller caller, ScreeningProgress? progress) =>
            Mathf.Max(caller.ScreeningPatience - (progress?.ElapsedTime ?? 0f), 0f);

        public static float Ratio(Caller caller, ScreeningProgress? progress)
        {
            if (caller.ScreeningPatience <= 0f)
            {
                return 0f;
            }
            return Mathf.Clamp(Remaining(caller, progress) / caller.ScreeningPatience, 0f, 1f);
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
