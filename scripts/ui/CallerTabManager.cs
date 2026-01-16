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
        void OnScreeningPanelCleared();
    }

    /// <summary>
    /// Manages caller UI panels within a tab.
    /// Self-contained component that populates individual panel areas.
    /// </summary>
    public partial class CallerTabManager : ICallerTabManager
    {
        private readonly CallerQueue _callerQueue;
        private readonly ICallerActions _callerActions;
        private ScreeningPanel _screeningPanel;
        private Control _screeningPanelContainer;

        public CallerTabManager(CallerQueue callerQueue, ICallerActions callerActions)
        {
            _callerQueue = callerQueue ?? throw new ArgumentNullException(nameof(callerQueue));
            _callerActions = callerActions ?? throw new ArgumentNullException(nameof(callerActions));
            _callerQueue.ScreeningChanged += OnScreeningChanged;
        }

        /// <summary>
        /// Create the incoming callers panel
        /// </summary>
        public void CreateIncomingPanel(Control panel)
        {
            GD.Print("CallerTabManager: Creating incoming callers panel");
            PopulateScrollableCallerPanel(panel, "INCOMING CALLERS", _callerQueue.IncomingCallers, new Color(1f, 0.7f, 0f), new Color(0.8f, 0.8f, 0.8f));
        }

        /// <summary>
        /// Create or update the screening panel
        /// </summary>
        public void CreateScreeningPanel(Control panel)
        {
            GD.Print("CallerTabManager: Setting up screening panel");

            _screeningPanelContainer = panel;

            if (_screeningPanel == null)
            {
                CreateScreeningPanelInstance(panel);
            }
            else
            {
                UpdateScreeningPanelContent();
            }
        }

        private void CreateScreeningPanelInstance(Control panel)
        {
            try
            {
                string scenePath = "res://scenes/ui/ScreeningPanel.tscn";
                var scene = ResourceLoader.Load<PackedScene>(scenePath);
                GD.Print($"CallerTabManager: Screening scene loaded: {scene != null}");

                if (scene != null)
                {
                    _screeningPanel = scene.Instantiate<ScreeningPanel>();
                    GD.Print($"CallerTabManager: Screening panel instantiated: {_screeningPanel != null}");

                    if (_screeningPanel != null)
                    {
                        _screeningPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                        _screeningPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                        _screeningPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                        _screeningPanel.CustomMinimumSize = Vector2.Zero;
                        GD.Print("CallerTabManager: Configured screening panel for full rect containment");

                        _screeningPanel.ConnectButtons(
                            Callable.From(() => _callerActions.OnApproveCaller()),
                            Callable.From(() => _callerActions.OnRejectCaller())
                        );

                        panel.AddChild(_screeningPanel);
                        UpdateScreeningPanelContent();
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

        private void UpdateScreeningPanelContent()
        {
            if (_screeningPanel == null) return;

            if (_callerQueue.IsScreening)
            {
                _screeningPanel.SetCaller(_callerQueue.CurrentScreening);
            }
            else
            {
                _screeningPanel.SetCaller(null);
            }
        }

        private void OnScreeningChanged()
        {
            UpdateScreeningPanelContent();
        }

        /// <summary>
        /// Create the on-hold callers panel
        /// </summary>
        public void CreateOnHoldPanel(Control panel)
        {
            GD.Print("CallerTabManager: Creating on-hold callers panel");
            PopulateScrollableCallerPanel(panel, "ON HOLD", _callerQueue.OnHoldCallers, new Color(0f, 0.7f, 1f), new Color(0.6f, 0.6f, 0.6f));
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
            _callerQueue.ScreeningChanged -= OnScreeningChanged;
            if (_screeningPanel != null)
            {
                _screeningPanel.QueueFree();
                _screeningPanel = null;
            }
            _screeningPanelContainer = null;
            GD.Print("CallerTabManager: Cleanup completed");
        }

        /// <summary>
        /// Called when the screening panel is cleared/destroyed by CallerTab
        /// </summary>
        public void OnScreeningPanelCleared()
        {
            _screeningPanel = null;
        }

        /// <summary>
        /// Populate a caller list panel that is already inside a ScrollContainer
        /// </summary>
        private void PopulateScrollableCallerPanel(Control panel, string headerText, System.Collections.Generic.IReadOnlyList<Caller> callers, Color headerColor, Color itemColor)
        {
            try
            {
                GD.Print($"CallerTabManager: Populating scrollable caller panel '{headerText}' with {callers?.Count ?? 0} callers");

                // Clear existing children first
                foreach (var child in panel.GetChildren())
                {
                    panel.RemoveChild(child);
                    child.QueueFree();
                }

                // Create root container to manage layout
                var rootContainer = new VBoxContainer();
                rootContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                rootContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                rootContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                panel.AddChild(rootContainer);

                // Create header
                var header = new Label();
                header.Text = headerText;
                header.AddThemeColorOverride("font_color", headerColor);
                header.HorizontalAlignment = HorizontalAlignment.Center;
                header.SizeFlagsVertical = 0; // Don't expand vertically
                header.CustomMinimumSize = new Vector2(0, 24); // Minimum height for header
                rootContainer.AddChild(header);

                // Add spacing between header and list
                var spacer = new Control();
                spacer.SizeFlagsVertical = 0;
                spacer.CustomMinimumSize = new Vector2(0, 16);
                rootContainer.AddChild(spacer);

                // Create list container directly (no inner scroll since panel is already in ScrollContainer)
                var listContainer = new VBoxContainer();
                listContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                listContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                listContainer.AddThemeConstantOverride("separation", 4); // 4px between caller items
                rootContainer.AddChild(listContainer);

                // Add caller items
                if (callers != null && callers.Count > 0)
                {
                    GD.Print($"CallerTabManager: Adding {callers.Count} callers to scrollable '{headerText}'");
                    foreach (var caller in callers)
                    {
                        if (caller == null)
                        {
                            GD.PrintErr("CallerTabManager: Null caller in list, skipping");
                            continue;
                        }

                        if (headerText == "INCOMING CALLERS")
                        {
                            // Use interactive CallerQueueItem for incoming callers
                            var itemScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/CallerQueueItem.tscn");
                            if (itemScene != null)
                            {
                                var node = itemScene.Instantiate();
                                if (node is CallerQueueItem callerItem)
                                {
                                    callerItem.SetCaller(caller);
                                    listContainer.AddChild(callerItem);
                                    GD.Print($"CallerTabManager: Added interactive caller item {caller.Name}");
                                }
                                else
                                {
                                    GD.PrintErr($"CallerTabManager: Instantiated node is not CallerQueueItem, got {node?.GetType()}");
                                    // Fallback to simple label
                                    var callerLabel = new Label();
                                    callerLabel.Text = $"• {caller.Name} - {caller.Location}";
                                    callerLabel.AddThemeColorOverride("font_color", itemColor);
                                    listContainer.AddChild(callerLabel);
                                }
                            }
                            else
                            {
                                GD.PrintErr("CallerTabManager: Failed to load CallerQueueItem scene");
                                // Fallback to simple label
                                var callerLabel = new Label();
                                callerLabel.Text = $"• {caller.Name} - {caller.Location}";
                                callerLabel.AddThemeColorOverride("font_color", itemColor);
                                listContainer.AddChild(callerLabel);
                            }
                        }
                        else
                        {
                            // Use simple label for other panels (on-hold, etc.)
                            var callerLabel = new Label();
                            callerLabel.Text = $"• {caller.Name} - {caller.Location}";
                            callerLabel.AddThemeColorOverride("font_color", itemColor);
                            listContainer.AddChild(callerLabel);
                            GD.Print($"CallerTabManager: Added caller {caller.Name}");
                        }
                    }
                }
                else
                {
                    var emptyLabel = new Label();
                    emptyLabel.Text = "None";
                    emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
                    emptyLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
                    listContainer.AddChild(emptyLabel);
                    GD.Print($"CallerTabManager: No callers for scrollable '{headerText}', showing 'None'");
                }

                GD.Print($"CallerTabManager: Successfully populated scrollable '{headerText}' panel with {panel.GetChildCount()} children");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"CallerTabManager.PopulateScrollableCallerPanel failed: {ex.Message}");
                panel.AddChild(CreateErrorPanel($"Failed to create scrollable {headerText.ToLower()} panel"));
            }
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
                        if (caller == null)
                        {
                            GD.PrintErr("CallerTabManager: Null caller in list, skipping");
                            continue;
                        }

                        if (headerText == "INCOMING CALLERS")
                        {
                            // Use interactive CallerQueueItem for incoming callers
                            var itemScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/CallerQueueItem.tscn");
                            if (itemScene != null)
                            {
                                var node = itemScene.Instantiate();
                                if (node is CallerQueueItem callerItem)
                                {
                                    callerItem.SetCaller(caller);
                                    listContainer.AddChild(callerItem);
                                    GD.Print($"CallerTabManager: Added interactive caller item {caller.Name}");
                                }
                                else
                                {
                                    GD.PrintErr($"CallerTabManager: Instantiated node is not CallerQueueItem, got {node?.GetType()}");
                                    // Fallback to simple label
                                    var callerLabel = new Label();
                                    callerLabel.Text = $"• {caller.Name} - {caller.Location}";
                                    callerLabel.AddThemeColorOverride("font_color", itemColor);
                                    listContainer.AddChild(callerLabel);
                                }
                            }
                            else
                            {
                                GD.PrintErr("CallerTabManager: Failed to load CallerQueueItem scene");
                                // Fallback to simple label
                                var callerLabel = new Label();
                                callerLabel.Text = $"• {caller.Name} - {caller.Location}";
                                callerLabel.AddThemeColorOverride("font_color", itemColor);
                                listContainer.AddChild(callerLabel);
                            }
                        }
                        else
                        {
                            // Use simple label for other panels (on-hold, etc.)
                            var callerLabel = new Label();
                            callerLabel.Text = $"• {caller.Name} - {caller.Location}";
                            callerLabel.AddThemeColorOverride("font_color", itemColor);
                            listContainer.AddChild(callerLabel);
                            GD.Print($"CallerTabManager: Added caller {caller.Name}");
                        }
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