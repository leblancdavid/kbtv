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
    public partial class TabContainerManager : Node, IDependent
    {
        private TabContainer _tabContainer;
        private CanvasLayer _canvas;

        public override void _Notification(int what) => this.Notify(what);

        public override void _Ready()
        {
        }

        public void OnResolved()
        {
            CreateUI();
            RegisterWithUIManager();
        }

        private void CreateUI()
        {
            _canvas = new CanvasLayer();
            _canvas.Layer = 100;
            AddChild(_canvas);

            var mainLayout = new VBoxContainer();
            mainLayout.Name = "MainLayout";
            mainLayout.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _canvas.AddChild(mainLayout);

            var headerScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/LiveShowHeader.tscn");
            if (headerScene != null)
            {
                var header = headerScene.Instantiate<Control>();
                header.Name = "LiveShowHeader";
                header.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
                header.CustomMinimumSize = new Vector2(0, 28);
                mainLayout.AddChild(header);
            }
            else
            {
                GD.PrintErr("TabContainerManager: Failed to load LiveShowHeader.tscn");
            }

            var tabScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/TabContainerUI.tscn");
            if (tabScene != null)
            {
                _tabContainer = tabScene.Instantiate<TabContainer>();
                _tabContainer.Name = "MainTabContainer";
                _tabContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                mainLayout.AddChild(_tabContainer);
                InitializeTabs();
            }
            else
            {
                GD.PrintErr("TabContainerManager: Failed to load TabContainerUI.tscn");
            }

            var footerScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/LiveShowFooter.tscn");
            if (footerScene != null)
            {
                var footer = footerScene.Instantiate<Control>();
                footer.Name = "LiveShowFooter";
                footer.SizeFlagsVertical = Control.SizeFlags.ShrinkEnd;
                footer.CustomMinimumSize = new Vector2(0, 200);
                mainLayout.AddChild(footer);
            }
            else
            {
                GD.PrintErr("TabContainerManager: Failed to load LiveShowFooter.tscn");
            }
        }



        private void InitializeTabs()
        {
            var callerTabScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/CallerTab.tscn");
            if (callerTabScene != null)
            {
                var callerTab = callerTabScene.Instantiate<Control>();
                _tabContainer.AddChild(callerTab);
                _tabContainer.SetTabTitle(callerTab.GetIndex(), "CALLERS");
            }
            else
            {
                GD.PrintErr("TabContainerManager: Failed to load CallerTab.tscn");
                AddPlaceholderTab("CALLERS");
            }

            AddPlaceholderTab("ITEMS");
            AddPlaceholderTab("STATS");
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
        }

        private void RegisterWithUIManager()
        {
            var uiManager = DependencyInjection.Get<IUIManager>(this);
            if (uiManager == null)
            {
                GD.PrintErr("TabContainerManager: UIManager is null - cannot register LiveShow layer!");
                return;
            }

            uiManager.RegisterLiveShowLayer(_canvas);
        }

        public override void _ExitTree()
        {
        }
    }
}