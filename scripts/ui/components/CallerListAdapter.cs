using Godot;
using KBTV.Callers;
using KBTV.UI.Themes;

namespace KBTV.UI.Components
{
    /// <summary>
    /// List adapter for displaying caller items in a ReactiveListPanel.
    /// </summary>
    public class CallerListAdapter : IListAdapter<Caller>
    {
        private readonly PackedScene? _itemScene;
        private readonly ICallerRepository _repository;

        public CallerListAdapter(ICallerRepository repository)
        {
            _repository = repository;
            _itemScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/CallerQueueItem.tscn");
        }

        public Control CreateItem(Caller caller)
        {
            if (_itemScene != null)
            {
                var node = _itemScene.Instantiate();
                if (node is Control control)
                {
                    ConfigureItem(control, caller);
                    return control;
                }
            }

            return CreateFallbackItem(caller);
        }

        public void UpdateItem(Control item, Caller caller)
        {
            ConfigureItem(item, caller);
        }

        public void DestroyItem(Control item)
        {
            item.QueueFree();
        }

        private void ConfigureItem(Control item, Caller caller)
        {
            if (item is ICallerListItem listItem)
            {
                listItem.SetCaller(caller);
                // Ensure the item can process updates
                if (item is Node node)
                {
                    node.ProcessMode = Godot.Node.ProcessModeEnum.Inherit;
                }
                return;
            }

            var nameLabel = item.GetNodeOrNull<Label>("HBoxContainer/NameLabel");
            if (nameLabel != null)
            {
                nameLabel.Text = caller?.Name ?? "";
            }

            var statusIndicator = item.GetNodeOrNull<ProgressBar>("HBoxContainer/StatusIndicator");
            if (statusIndicator != null && caller != null)
            {
                float remainingPatience = caller.Patience - caller.WaitTime;
                float patienceRatio = Mathf.Clamp(remainingPatience / caller.Patience, 0f, 1f);
                statusIndicator.Value = patienceRatio;
                var fillStyle = new StyleBoxFlat { BgColor = UIColors.GetPatienceColor(patienceRatio) };
                statusIndicator.AddThemeStyleboxOverride("fill", fillStyle);
                statusIndicator.QueueRedraw();
            }

            var styleBox = new StyleBoxFlat();
            styleBox.CornerRadiusTopLeft = 4;
            styleBox.CornerRadiusTopRight = 4;
            styleBox.CornerRadiusBottomRight = 4;
            styleBox.CornerRadiusBottomLeft = 4;

            var repository = _repository;
            bool isScreening = repository?.CurrentScreening == caller;
            styleBox.BgColor = isScreening ? UIColors.Screening.Selected : UIColors.Screening.Default;

            item.AddThemeStyleboxOverride("panel", styleBox);
            item.AddThemeStyleboxOverride("panel_pressed", styleBox);
            item.QueueRedraw();
        }

        private Control CreateFallbackItem(Caller caller)
        {
            var panel = new Panel
            {
                CustomMinimumSize = new Vector2(0, 40),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };

            var styleBox = new StyleBoxFlat
            {
                BgColor = UIColors.Screening.Default,
                CornerRadiusTopLeft = 4,
                CornerRadiusTopRight = 4,
                CornerRadiusBottomRight = 4,
                CornerRadiusBottomLeft = 4
            };
            panel.AddThemeStyleboxOverride("panel", styleBox);

            var hbox = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                Alignment = HBoxContainer.AlignmentMode.Center
            };
            hbox.AddThemeConstantOverride("separation", 8);
            panel.AddChild(hbox);

            var nameLabel = new Label
            {
                Text = caller?.Name ?? "",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center
            };
            nameLabel.AddThemeColorOverride("font_color", UIColors.Screening.DefaultText);
            hbox.AddChild(nameLabel);

            var spacer = new Control
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            hbox.AddChild(spacer);

            var statusIndicator = new ProgressBar
            {
                CustomMinimumSize = new Vector2(50, 20),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
                MaxValue = 1.0,
                Value = 1.0,
                ShowPercentage = false
            };
            statusIndicator.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = UIColors.Patience.High });
            hbox.AddChild(statusIndicator);

            return panel;
        }
    }

    /// <summary>
    /// Interface for caller list items to support adapter pattern.
    /// </summary>
    public interface ICallerListItem
    {
        void SetCaller(Caller caller);
        void SetSelected(bool selected);
    }
}
