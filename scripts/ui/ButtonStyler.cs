#nullable enable

using Godot;
using KBTV.UI.Themes;

namespace KBTV.UI
{
    /// <summary>
    /// Helper class for applying consistent button styles.
    /// </summary>
    public static class ButtonStyler
    {
        public static void StyleApprove(Button button, bool enabled)
        {
            var color = enabled ? UIColors.Button.Approve : UIColors.BG_DISABLED;
            ApplyStyle(button, color, UIColors.Button.ApproveText);
        }

        public static void StyleReject(Button button, bool enabled)
        {
            var color = enabled ? UIColors.Button.Reject : UIColors.BG_DISABLED;
            ApplyStyle(button, color, UIColors.Button.RejectText);
        }

        private static void ApplyStyle(Button button, Color bgColor, Color textColor)
        {
            var style = new StyleBoxFlat
            {
                BgColor = bgColor,
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
                ContentMarginLeft = 20,
                ContentMarginRight = 20,
                ContentMarginTop = 12,
                ContentMarginBottom = 12
            };
            button.AddThemeStyleboxOverride("normal", style);
            button.AddThemeColorOverride("font_color", textColor);
        }
    }
}
