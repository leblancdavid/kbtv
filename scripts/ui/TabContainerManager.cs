using Godot;

namespace KBTV.UI
{
    /// <summary>
    /// Manages the main tab container UI system.
    /// Autoload that creates and manages the TabContainer scene and its tabs.
    /// Follows Godot's component-based architecture with self-managing tab components.
    /// </summary>
    [GlobalClass]
    public partial class TabContainerManager : Node
    {
        private TabContainer _tabContainer;
        private CanvasLayer _canvas;

        public override void _Ready()
        {
            GD.Print("TabContainerManager: Initializing UI system");

            // Create canvas layer for UI
            _canvas = new CanvasLayer();
            _canvas.Layer = 100; // Above other UI layers
            AddChild(_canvas);

            // Add full-screen background to block lower layers
            var background = new Panel();
            background.Name = "Background";
            background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            background.Modulate = new Color(0, 0, 0, 0.8f); // Semi-transparent background
            _canvas.AddChild(background);

            // Load and instantiate TabContainer scene
            var tabScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/TabContainerUI.tscn");
            if (tabScene != null)
            {
                _tabContainer = tabScene.Instantiate<TabContainer>();
                _tabContainer.Name = "MainTabContainer";
                _canvas.AddChild(_tabContainer);

                // Initialize tabs
                InitializeTabs();
                GD.Print("TabContainerManager: UI system initialized successfully");
            }
            else
            {
                GD.PrintErr("TabContainerManager: Failed to load TabContainerUI.tscn");
            }
        }

        private void InitializeTabs()
        {
            GD.Print("TabContainerManager: Initializing tabs");

            // Add CallerTab (self-contained component)
            var callerTabScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/CallerTab.tscn");
            if (callerTabScene != null)
            {
                var callerTab = callerTabScene.Instantiate<Control>();
                _tabContainer.AddChild(callerTab);
                _tabContainer.SetTabTitle(callerTab.GetIndex(), "CALLERS");
                GD.Print("TabContainerManager: CallerTab added successfully");
            }
            else
            {
                GD.PrintErr("TabContainerManager: Failed to load CallerTab.tscn");
                AddPlaceholderTab("CALLERS");
            }

            // Add placeholder tabs for future implementation
            AddPlaceholderTab("ITEMS");
            AddPlaceholderTab("STATS");

            GD.Print($"TabContainerManager: Initialized {_tabContainer.GetChildCount()} tabs");
        }

        private void AddPlaceholderTab(string title)
        {
            var placeholder = new Label();
            placeholder.Name = $"{title}Placeholder";
            placeholder.Text = $"{title} TAB\n(Not implemented yet)";
            placeholder.HorizontalAlignment = HorizontalAlignment.Center;
            placeholder.VerticalAlignment = VerticalAlignment.Center;

            _tabContainer.AddChild(placeholder);
            _tabContainer.SetTabTitle(placeholder.GetIndex(), title);

            GD.Print($"TabContainerManager: Added placeholder tab for {title}");
        }

        public override void _ExitTree()
        {
            GD.Print("TabContainerManager: Cleaning up");
        }
    }
}