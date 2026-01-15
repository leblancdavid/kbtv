using Godot;
using System;
using KBTV.Callers;

namespace KBTV.UI
{
    /// <summary>
    /// Interface for caller actions
    /// </summary>
    public interface ICallerActions
    {
        void OnApproveCaller();
        void OnRejectCaller();
    }

    /// <summary>
    /// Interface for caller tab management
    /// </summary>
    public interface ICallerTabManager
    {
        void CreateIncomingPanel(Control panel);
        void CreateScreeningPanel(Control panel);
        void CreateOnHoldPanel(Control panel);
        void RefreshContent();
        void Cleanup();
    }

    /// <summary>
    /// Manages caller UI panels within a tab.
    /// Self-contained component that populates individual panel areas.
    /// </summary>
    public partial class CallerTabManager : ICallerTabManager
    {
        private readonly CallerQueue _callerQueue;
        private readonly ICallerActions _callerActions;

        public CallerTabManager(CallerQueue callerQueue, ICallerActions callerActions)
        {
            _callerQueue = callerQueue ?? throw new ArgumentNullException(nameof(callerQueue));
            _callerActions = callerActions ?? throw new ArgumentNullException(nameof(callerActions));
        }

        /// <summary>
        /// Create the incoming callers panel
        /// </summary>
        public void CreateIncomingPanel(Control panel)
        {
            GD.Print("CallerTabManager: Creating incoming callers panel");
            PopulateCallerPanel(panel, "INCOMING CALLERS", _callerQueue.IncomingCallers, new Color(1f, 0.7f, 0f), new Color(0.8f, 0.8f, 0.8f));
        }

        /// <summary>
        /// Create the screening panel
        /// </summary>
        public void CreateScreeningPanel(Control panel)
        {
            GD.Print("CallerTabManager: Creating screening panel");

            try
            {
                string scenePath = "res://scenes/ui/ScreeningPanel.tscn";
                var scene = ResourceLoader.Load<PackedScene>(scenePath);
                GD.Print($"CallerTabManager: Screening scene loaded: {scene != null}");

                if (scene != null)
                {
                    var screeningPanel = scene.Instantiate<ScreeningPanel>();
                    GD.Print($"CallerTabManager: Screening panel instantiated: {screeningPanel != null}");

                    if (screeningPanel != null)
                    {
                        // Configure for proper containment within the tab
                        screeningPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                        screeningPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                        screeningPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                        screeningPanel.CustomMinimumSize = Vector2.Zero;
                        GD.Print("CallerTabManager: Configured screening panel for full rect containment");

                        // Set caller info
                        if (_callerQueue.IsScreening)
                        {
                            screeningPanel.SetCaller(_callerQueue.CurrentScreening);
                        }
                        else
                        {
                            screeningPanel.SetCaller(null);
                        }

                        // Connect buttons
                        screeningPanel.ConnectButtons(
                            Callable.From(() => _callerActions.OnApproveCaller()),
                            Callable.From(() => _callerActions.OnRejectCaller())
                        );

                        panel.AddChild(screeningPanel);
                        GD.Print("CallerTabManager: Screening panel creation successful");
                    }
                }
                else
                {
                    GD.PrintErr("CallerTabManager: Failed to load screening panel scene");
                    panel.AddChild(CreateErrorPanel("Failed to load screening panel"));
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"CallerTabManager.CreateScreeningPanel failed: {ex.Message}");
                panel.AddChild(CreateErrorPanel("Failed to create screening panel"));
            }
        }

        /// <summary>
        /// Create the on-hold callers panel
        /// </summary>
        public void CreateOnHoldPanel(Control panel)
        {
            GD.Print("CallerTabManager: Creating on-hold callers panel");
            PopulateCallerPanel(panel, "ON HOLD", _callerQueue.OnHoldCallers, new Color(0f, 0.7f, 1f), new Color(0.6f, 0.6f, 0.6f));
        }

        /// <summary>
        /// Refresh the caller tab content (called by CallerTab when needed)
        /// </summary>
        public void RefreshContent()
        {
            // This method is now handled by CallerTab directly
            GD.Print("CallerTabManager: RefreshContent called (handled by CallerTab)");
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Cleanup()
        {
            GD.Print("CallerTabManager: Cleanup completed");
        }

        /// <summary>
        /// Populate a caller list panel with header and scrollable content
        /// </summary>
        private void PopulateCallerPanel(Control panel, string headerText, System.Collections.Generic.IReadOnlyList<Caller> callers, Color headerColor, Color itemColor)
        {
            try
            {
                GD.Print($"CallerTabManager: Populating caller panel '{headerText}' with {callers?.Count ?? 0} callers");

                // Clear existing children first
                foreach (var child in panel.GetChildren())
                {
                    panel.RemoveChild(child);
                    child.QueueFree();
                }

                // Create header
                var header = new Label();
                header.Text = headerText;
                header.AddThemeColorOverride("font_color", headerColor);
                header.HorizontalAlignment = HorizontalAlignment.Center;
                panel.AddChild(header);

                // Create scrollable list
                var scroll = new ScrollContainer();
                scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                panel.AddChild(scroll);

                var listContainer = new VBoxContainer();
                listContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                listContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                scroll.AddChild(listContainer);

                // Add caller items
                if (callers != null && callers.Count > 0)
                {
                    GD.Print($"CallerTabManager: Adding {callers.Count} callers to '{headerText}'");
                    foreach (var caller in callers)
                    {
                        var callerLabel = new Label();
                        callerLabel.Text = $"• {caller.Name} - {caller.Location}";
                        callerLabel.AddThemeColorOverride("font_color", itemColor);
                        listContainer.AddChild(callerLabel);
                        GD.Print($"CallerTabManager: Added caller {caller.Name}");
                    }
                }
                else
                {
                    var emptyLabel = new Label();
                    emptyLabel.Text = "None";
                    emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
                    emptyLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
                    listContainer.AddChild(emptyLabel);
                    GD.Print($"CallerTabManager: No callers for '{headerText}', showing 'None'");
                }

                GD.Print($"CallerTabManager: Successfully populated '{headerText}' panel with {panel.GetChildCount()} children");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"CallerTabManager.PopulateCallerPanel failed: {ex.Message}");
                panel.AddChild(CreateErrorPanel($"Failed to create {headerText.ToLower()} panel"));
            }
        }

        private Control CreateErrorPanel(string message)
        {
            var panel = new Panel();
            panel.Name = "ErrorPanel";
            var label = new Label();
            label.Text = message;
            label.HorizontalAlignment = HorizontalAlignment.Center;
            panel.AddChild(label);
            return panel;
        }
    }
}