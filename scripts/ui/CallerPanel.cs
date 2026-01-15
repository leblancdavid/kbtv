using Godot;
using System.Collections.Generic;
using KBTV.Callers;

namespace KBTV.UI
{
    public partial class CallerPanel : Panel
    {
        private Label _headerLabel;
        private VBoxContainer _callerList;

        public override void _Ready()
        {
            _headerLabel = GetNode<Label>("VBoxContainer/HeaderLabel");
            _callerList = GetNode<VBoxContainer>("VBoxContainer/ScrollContainer/CallerList");
        }

        public void SetHeader(string headerText, Color headerColor)
        {
            if (_headerLabel != null)
            {
                _headerLabel.Text = headerText;
                _headerLabel.AddThemeColorOverride("font_color", headerColor);
            }
        }

        public void SetCallers(IEnumerable<Caller> callers, Color itemColor)
        {
            if (_callerList == null) return;

            // Clear existing
            foreach (var child in _callerList.GetChildren())
            {
                _callerList.RemoveChild(child);
                child.QueueFree();
            }

            // Add new
            if (callers != null)
            {
                foreach (var caller in callers)
                {
                    var label = new Label();
                    label.Text = $"{caller.Name} - {caller.Location}";
                    label.AddThemeColorOverride("font_color", itemColor);
                    _callerList.AddChild(label);
                }
            }

            if (_callerList.GetChildCount() == 0)
            {
                var emptyLabel = new Label();
                emptyLabel.Text = "None";
                emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
                emptyLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
                _callerList.AddChild(emptyLabel);
            }
        }
    }
}