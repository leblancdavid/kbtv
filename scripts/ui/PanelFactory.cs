#nullable enable

using Godot;
using System.Collections.Generic;
using KBTV.Callers;
using KBTV.UI;

namespace KBTV.UI
{
    /// <summary>
    /// Factory class for creating UI panels programmatically.
    /// Handles all panel instantiation and configuration with scene-based loading and programmatic fallbacks.
    /// </summary>
    public partial class PanelFactory
    {
        private readonly CallerQueue _callerQueue;
        private readonly ICallerActions _callerActions;

        public PanelFactory(CallerQueue callerQueue, ICallerActions callerActions)
        {
            _callerQueue = callerQueue;
            _callerActions = callerActions;
        }

        /// <summary>
        /// Creates the screening panel for caller approval/rejection.
        /// </summary>
        public Control CreateScreeningPanelScene()
        {
            var scene = ResourceLoader.Load<PackedScene>("res://scenes/ui/ScreeningPanel.tscn");
            if (scene != null)
            {
                var panel = scene.Instantiate<ScreeningPanel>();
                if (panel != null)
                {
                    // Set caller info
                    if (_callerQueue.IsScreening)
                    {
                        panel.SetCaller(_callerQueue.CurrentScreening);
                    }
                    else
                    {
                        panel.SetCaller(null);
                    }

                    // Connect buttons
                    panel.ConnectButtons(Callable.From(() => _callerActions.OnApproveCaller()), Callable.From(() => _callerActions.OnRejectCaller()));

                    return panel;
                }
            }
            // Fallback to programmatic
            return CreateScreeningPanelFallback();
        }

        /// <summary>
        /// Programmatic fallback for screening panel.
        /// </summary>
        private Control CreateScreeningPanelFallback()
        {
            return CreateScreeningPanel();
        }

        /// <summary>
        /// Creates the basic screening panel structure.
        /// </summary>
        private Control CreateScreeningPanel()
        {
            var panel = new Panel();
            panel.Name = "ScreeningPanel";
            UITheme.ApplyPanelStyle(panel);
            panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

            var layout = new VBoxContainer();
            layout.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            layout.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            layout.AddThemeConstantOverride("separation", 10);
            panel.AddChild(layout);

            // Header
            var header = new Label();
            header.Text = "SCREENING";
            header.AddThemeColorOverride("font_color", new Color(0f, 1f, 0f));
            header.HorizontalAlignment = HorizontalAlignment.Center;
            layout.AddChild(header);

            // Current caller info
            if (_callerQueue.IsScreening)
            {
                var caller = _callerQueue.CurrentScreening;
                var callerLabel = new Label();
                callerLabel.Text = $"{caller.Name}\n{caller.Location}\nTopic: {caller.ClaimedTopic}";
                callerLabel.HorizontalAlignment = HorizontalAlignment.Center;
                callerLabel.AutowrapMode = TextServer.AutowrapMode.Word;
                layout.AddChild(callerLabel);
            }

            // Spacer
            var spacer = new Control();
            spacer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            layout.AddChild(spacer);

            // Buttons container
            var buttonsContainer = new HBoxContainer();
            buttonsContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            buttonsContainer.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            buttonsContainer.AddThemeConstantOverride("separation", 10);
            layout.AddChild(buttonsContainer);

            // Approve button
            var approveButton = new Button();
            approveButton.Text = "APPROVE";
            approveButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            approveButton.AddThemeColorOverride("font_color", new Color(0f, 0.8f, 0f));
            approveButton.Pressed += () => _callerActions.OnApproveCaller();
            buttonsContainer.AddChild(approveButton);

            // Reject button
            var rejectButton = new Button();
            rejectButton.Text = "REJECT";
            rejectButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            rejectButton.AddThemeColorOverride("font_color", new Color(0.8f, 0.2f, 0.2f));
            rejectButton.Pressed += () => _callerActions.OnRejectCaller();
            buttonsContainer.AddChild(rejectButton);

            return panel;
        }

        /// <summary>
        /// Creates a caller panel showing a list of callers.
        /// </summary>
        public Control CreateCallerPanelScene(string headerText, IReadOnlyList<Caller> callers, Color headerColor, Color itemColor)
        {
            var scene = ResourceLoader.Load<PackedScene>("res://scenes/ui/CallerPanel.tscn");
            if (scene != null)
            {
                var panel = scene.Instantiate<CallerPanel>();
                if (panel != null)
                {
                    panel.SetHeader(headerText, headerColor);
                    panel.SetCallers(callers, itemColor);
                    return panel;
                }
            }
            // Fallback
            return CreateCallerPanelFallback(headerText, callers, headerColor, itemColor);
        }

        /// <summary>
        /// Programmatic fallback for caller panel.
        /// </summary>
        private Control CreateCallerPanelFallback(string headerText, IReadOnlyList<Caller> callers, Color headerColor, Color itemColor)
        {
            return CreateCallerPanel("FallbackPanel", headerText, callers, headerColor, itemColor);
        }

        /// <summary>
        /// Creates the caller panel structure.
        /// </summary>
        private Control CreateCallerPanel(string name, string headerText, IReadOnlyList<Caller> callers, Color headerColor, Color itemColor)
        {
            GD.Print($"Creating caller panel {name} with {callers.Count} callers");

            var panel = new Panel();
            panel.Name = name;
            UITheme.ApplyPanelStyle(panel);
            panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

            var layout = new VBoxContainer();
            layout.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            layout.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            layout.AddThemeConstantOverride("separation", 5);
            panel.AddChild(layout);

            // Header
            var header = new Label();
            header.Text = headerText;
            header.AddThemeColorOverride("font_color", headerColor);
            header.HorizontalAlignment = HorizontalAlignment.Center;
            layout.AddChild(header);

            // Scrollable list
            var scroll = new ScrollContainer();
            scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            layout.AddChild(scroll);

            var listContainer = new VBoxContainer();
            listContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            listContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            scroll.AddChild(listContainer);

            // Add caller items
            if (callers != null && callers.Count > 0)
            {
                foreach (var caller in callers)
                {
                    var callerLabel = new Label();
                    callerLabel.Text = $"{caller.Name} - {caller.Location}";
                    callerLabel.AddThemeColorOverride("font_color", itemColor);
                    listContainer.AddChild(callerLabel);
                }
            }
            else
            {
                var emptyLabel = new Label();
                emptyLabel.Text = "None";
                emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
                emptyLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
                listContainer.AddChild(emptyLabel);
            }

            return panel;
        }
    }
}