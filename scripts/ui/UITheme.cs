using Godot;

namespace KBTV.UI
{
    /// <summary>
    /// Dark theme constants and styling utilities for KBTV UI.
    /// Provides consistent colors and styling across all UI components.
    /// </summary>
    public static class UITheme
    {
        // Dark Theme Colors
        public static readonly Color BG_DARK = new Color(0.1f, 0.1f, 0.1f);
        public static readonly Color BG_PANEL = new Color(0.15f, 0.15f, 0.15f);
        public static readonly Color BG_BORDER = new Color(0.2f, 0.2f, 0.2f);
        public static readonly Color TEXT_PRIMARY = new Color(0.9f, 0.9f, 0.9f);
        public static readonly Color TEXT_SECONDARY = new Color(0.7f, 0.7f, 0.7f);
        public static readonly Color ACCENT_GOLD = new Color(1f, 0.7f, 0f);
        public static readonly Color ACCENT_RED = new Color(0.8f, 0.2f, 0.2f);
        public static readonly Color ACCENT_GREEN = new Color(0.2f, 0.8f, 0.2f);

        // Dimensions (in pixels)
        public const float HEADER_HEIGHT = 28f;
        public const float TAB_HEIGHT = 24f;
        public const float FOOTER_HEIGHT = 160f;
        public const float BUTTON_HEIGHT = 28f;

        // Panel Widths (as fractions)
        public const float ONAIR_WIDTH = 0.35f;
        public const float TRANSCRIPT_WIDTH = 0.45f;
        public const float ADBREAK_WIDTH = 0.20f;

        /// <summary>
        /// Apply dark theme panel styling to a Panel control.
        /// </summary>
        public static void ApplyPanelStyle(Panel panel, bool isDark = true)
        {
            var styleBox = new StyleBoxFlat();
            styleBox.BgColor = isDark ? BG_PANEL : BG_DARK;
            styleBox.BorderColor = BG_BORDER;
            styleBox.BorderWidthBottom = 1;
            styleBox.BorderWidthTop = 1;
            styleBox.BorderWidthLeft = 1;
            styleBox.BorderWidthRight = 1;
            styleBox.CornerRadiusTopLeft = 2;
            styleBox.CornerRadiusTopRight = 2;
            styleBox.CornerRadiusBottomLeft = 2;
            styleBox.CornerRadiusBottomRight = 2;
            panel.AddThemeStyleboxOverride("panel", styleBox);
        }

        /// <summary>
        /// Apply dark theme button styling to a Button control.
        /// </summary>
        public static void ApplyButtonStyle(Button button)
        {
            button.AddThemeColorOverride("font_color", TEXT_PRIMARY);

            // Normal state
            var normalStyle = new StyleBoxFlat();
            normalStyle.BgColor = BG_PANEL;
            normalStyle.BorderColor = BG_BORDER;
            normalStyle.BorderWidthBottom = 1;
            normalStyle.BorderWidthTop = 1;
            normalStyle.BorderWidthLeft = 1;
            normalStyle.BorderWidthRight = 1;
            normalStyle.CornerRadiusTopLeft = 3;
            normalStyle.CornerRadiusTopRight = 3;
            normalStyle.CornerRadiusBottomLeft = 3;
            normalStyle.CornerRadiusBottomRight = 3;
            button.AddThemeStyleboxOverride("normal", normalStyle);

            // Hover state
            var hoverStyle = new StyleBoxFlat();
            hoverStyle.BgColor = BG_DARK;
            hoverStyle.BorderColor = new Color(0.3f, 0.3f, 0.3f);
            hoverStyle.BorderWidthBottom = 1;
            hoverStyle.BorderWidthTop = 1;
            hoverStyle.BorderWidthLeft = 1;
            hoverStyle.BorderWidthRight = 1;
            hoverStyle.CornerRadiusTopLeft = 3;
            hoverStyle.CornerRadiusTopRight = 3;
            hoverStyle.CornerRadiusBottomLeft = 3;
            hoverStyle.CornerRadiusBottomRight = 3;
            button.AddThemeStyleboxOverride("hover", hoverStyle);

            // Pressed state
            var pressedStyle = new StyleBoxFlat();
            pressedStyle.BgColor = BG_BORDER;
            pressedStyle.BorderColor = new Color(0.4f, 0.4f, 0.4f);
            pressedStyle.BorderWidthBottom = 1;
            pressedStyle.BorderWidthTop = 1;
            pressedStyle.BorderWidthLeft = 1;
            pressedStyle.BorderWidthRight = 1;
            pressedStyle.CornerRadiusTopLeft = 3;
            pressedStyle.CornerRadiusTopRight = 3;
            pressedStyle.CornerRadiusBottomLeft = 3;
            pressedStyle.CornerRadiusBottomRight = 3;
            button.AddThemeStyleboxOverride("pressed", pressedStyle);

            // Disabled state
            var disabledStyle = new StyleBoxFlat();
            disabledStyle.BgColor = new Color(0.08f, 0.08f, 0.08f);
            disabledStyle.BorderColor = BG_BORDER;
            disabledStyle.BorderWidthBottom = 1;
            disabledStyle.BorderWidthTop = 1;
            disabledStyle.BorderWidthLeft = 1;
            disabledStyle.BorderWidthRight = 1;
            disabledStyle.CornerRadiusTopLeft = 3;
            disabledStyle.CornerRadiusTopRight = 3;
            disabledStyle.CornerRadiusBottomLeft = 3;
            disabledStyle.CornerRadiusBottomRight = 3;
            button.AddThemeStyleboxOverride("disabled", disabledStyle);
        }

        /// <summary>
        /// Apply dark theme label styling to a Label control.
        /// </summary>
        public static void ApplyLabelStyle(Label label, bool isPrimary = true)
        {
            label.AddThemeColorOverride("font_color",
                isPrimary ? TEXT_PRIMARY : TEXT_SECONDARY);
        }

        /// <summary>
        /// Create a standard spacer control with flexible sizing.
        /// </summary>
        public static Control CreateSpacer(bool flexibleWidth = true, bool flexibleHeight = false)
        {
            var spacer = new Control();
            if (flexibleWidth)
                spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            if (flexibleHeight)
                spacer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            return spacer;
        }

        /// <summary>
        /// Create a standard horizontal layout container.
        /// </summary>
        public static HBoxContainer CreateHLayout(float spacing = 4f, bool expandFill = true)
        {
            var layout = new HBoxContainer();
            layout.AddThemeConstantOverride("separation", (int)spacing);
            if (expandFill)
            {
                layout.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                layout.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            }
            return layout;
        }

        /// <summary>
        /// Create a standard vertical layout container.
        /// </summary>
        public static VBoxContainer CreateVLayout(float spacing = 4f, bool expandFill = true)
        {
            var layout = new VBoxContainer();
            layout.AddThemeConstantOverride("separation", (int)spacing);
            if (expandFill)
            {
                layout.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                layout.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            }
            return layout;
        }

        /// <summary>
        /// Apply consistent margins to a control.
        /// </summary>
        public static void ApplyMargins(Control control, float left = 8f, float top = 8f,
                                       float right = 8f, float bottom = 8f)
        {
            control.AddThemeConstantOverride("margin_left", (int)left);
            control.AddThemeConstantOverride("margin_top", (int)top);
            control.AddThemeConstantOverride("margin_right", (int)right);
            control.AddThemeConstantOverride("margin_bottom", (int)bottom);
        }
    }
}