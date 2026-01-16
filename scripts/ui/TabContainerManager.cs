using Godot;
using KBTV.Core;

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
            ServiceRegistry.Instance.RegisterSelf<TabContainerManager>(this);

            _canvas = new CanvasLayer();
            _canvas.Layer = 100;
            AddChild(_canvas);

            var background = new Panel();
            background.Name = "Background";
            background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            background.Modulate = new Color(0, 0, 0, 0.8f);
            _canvas.AddChild(background);

            // Create main layout container
            var mainLayout = new VBoxContainer();
            mainLayout.Name = "MainLayout";
            mainLayout.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _canvas.AddChild(mainLayout);

            // Load and instantiate Header
            var headerScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/LiveShowHeader.tscn");
            if (headerScene != null)
            {
                var header = headerScene.Instantiate<Control>();
                header.Name = "LiveShowHeader";
                header.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
                header.CustomMinimumSize = new Vector2(0, 28); // Fixed header height
                mainLayout.AddChild(header);
                GD.Print("TabContainerManager: Header loaded successfully");
            }
            else
            {
                GD.PrintErr("TabContainerManager: Failed to load LiveShowHeader.tscn");
            }

            // Load and instantiate TabContainer scene
            var tabScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/TabContainerUI.tscn");
            if (tabScene != null)
            {
                _tabContainer = tabScene.Instantiate<TabContainer>();
                _tabContainer.Name = "MainTabContainer";
                _tabContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                mainLayout.AddChild(_tabContainer);

                // Initialize tabs
                InitializeTabs();
                GD.Print("TabContainerManager: TabContainer loaded successfully");
            }
            else
            {
                GD.PrintErr("TabContainerManager: Failed to load TabContainerUI.tscn");
            }

            // Load and instantiate Footer
            var footerScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/LiveShowFooter.tscn");
            if (footerScene != null)
            {
                var footer = footerScene.Instantiate<Control>();
                footer.Name = "LiveShowFooter";
                footer.SizeFlagsVertical = Control.SizeFlags.ShrinkEnd;
                footer.CustomMinimumSize = new Vector2(0, 140); // Fixed footer height
                mainLayout.AddChild(footer);
                GD.Print("TabContainerManager: Footer loaded successfully");
            }
            else
            {
                GD.PrintErr("TabContainerManager: Failed to load LiveShowFooter.tscn");
            }

            GD.Print("TabContainerManager: UI system initialized successfully");

            // Register with UIManager (deferred to ensure it's ready)
            CallDeferred(nameof(RegisterWithUIManager));
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

        private void RegisterWithUIManager()
        {
            GD.Print("TabContainerManager: Registering LiveShow layer with UIManager");

            var uiManager = ServiceRegistry.Instance?.UIManager;
            if (uiManager == null)
            {
                GD.PrintErr("TabContainerManager: UIManager is null - cannot register LiveShow layer!");
                return;
            }

            uiManager.RegisterLiveShowLayer(_canvas);
            GD.Print("TabContainerManager: LiveShow layer registered successfully");
        }

        public override void _ExitTree()
        {
            GD.Print("TabContainerManager: Cleaning up");
        }
    }
}