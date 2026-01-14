using System;
using Godot;

namespace KBTV.UI
{
    public static class UIHelpers
    {
        public static Control CreateSpacer(float flexibleWidth = 1f)
        {
            var spacer = new Control();
            spacer.Name = "Spacer";
            spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            spacer.CustomMinimumSize = new Vector2(0, 0);
            // In Godot, we use size flags for flexible spacing
            return spacer;
        }

        public static Label CreateLabel(string text, HorizontalAlignment hAlign = HorizontalAlignment.Left, VerticalAlignment vAlign = VerticalAlignment.Top)
        {
            var label = new Label();
            label.Text = text;
            label.HorizontalAlignment = hAlign;
            label.VerticalAlignment = vAlign;
            return label;
        }

        public static Button CreateButton(string text, Action onPressed = null)
        {
            var button = new Button();
            button.Text = text;
            if (onPressed != null)
            {
                button.Pressed += onPressed;
            }
            return button;
        }

        public static Panel CreatePanel(string name, Color bgColor)
        {
            var panel = new Panel();
            panel.Name = name;

            var styleBox = new StyleBoxFlat();
            styleBox.BgColor = bgColor;
            panel.AddThemeStyleboxOverride("panel", styleBox);

            return panel;
        }

        public static ScrollContainer CreateScrollContainer()
        {
            var scroll = new ScrollContainer();
            scroll.FollowFocus = true;
            return scroll;
        }
    }
}