using System;
using System.Collections.Generic;
using Godot;

namespace KBTV.UI.Controllers
{
    public partial class TabController : Node
    {
        private UIManagerBootstrap _owner;
        private Control _parent;
        private List<TabDefinition> _tabs;

        // Tab buttons and content containers (dynamically created)
        private List<Button> _tabButtons = new List<Button>();
        private List<Control> _tabContents = new List<Control>();

        private int _currentTab = 0;

        public TabController(List<TabDefinition> tabs, UIManagerBootstrap owner)
        {
            _tabs = tabs;
            _owner = owner;
        }

        public void Initialize(Control parent)
        {
            _parent = parent;
            CreateTabSection();
            // Set default active tab (CALLERS at index 0)
            SwitchTab(0);
        }

        public void SwitchTab(int tabIndex)
        {
            if (_currentTab == tabIndex || tabIndex < 0 || tabIndex >= _tabs.Count) return;

            // Reset all tab button colors
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                if (_tabButtons[i] != null)
                {
                    var styleBox = new StyleBoxFlat();
                    styleBox.BgColor = new Color(0.15f, 0.15f, 0.15f);
                    _tabButtons[i].AddThemeStyleboxOverride("normal", styleBox);
                }
            }

            // Set active tab color
            if (_tabButtons[tabIndex] != null)
            {
                var styleBox = new StyleBoxFlat();
                styleBox.BgColor = new Color(0.2f, 0.2f, 0.2f);
                _tabButtons[tabIndex].AddThemeStyleboxOverride("normal", styleBox);
            }

            // Show/hide tab content
            for (int i = 0; i < _tabContents.Count; i++)
            {
                if (_tabContents[i] != null)
                {
                    _tabContents[i].Visible = i == tabIndex;
                }
            }

            _currentTab = tabIndex;

            // Call tab selected callback if provided
            _tabs[tabIndex].OnTabSelected?.Invoke();
        }

        public void RefreshTabContent(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= _tabs.Count) return;

            var contentArea = _tabContents[tabIndex];
            if (contentArea != null)
            {
                // Clear existing content
                ClearTabContent(contentArea);

                // Populate with new content
                _tabs[tabIndex].PopulateContent?.Invoke(contentArea);
            }
        }

        public void RefreshCurrentTab()
        {
            RefreshTabContent(_currentTab);
        }

        public void RefreshAllTabs()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                RefreshTabContent(i);
            }
        }

        private void ClearTabContent(Control contentArea)
        {
            // Clear all children
            foreach (var child in contentArea.GetChildren())
            {
                contentArea.RemoveChild(child);
                child.QueueFree();
            }
        }

        private void CreateTabSection()
        {
            var tabContainer = new VBoxContainer();
            tabContainer.Name = "TabSection";
            tabContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            tabContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            tabContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

            var tabStyle = new StyleBoxFlat();
            tabStyle.BgColor = new Color(0.09f, 0.09f, 0.09f);
            var tabPanel = new Panel();
            tabPanel.AddThemeStyleboxOverride("panel", tabStyle);
            tabPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            tabContainer.AddChild(tabPanel);

            CreateTabHeader(tabContainer);
            CreateTabContent(tabContainer);

            _parent.AddChild(tabContainer);
        }

        private void CreateTabHeader(Control parent)
        {
            var headerContainer = new HBoxContainer();
            headerContainer.Name = "TabHeader";
            headerContainer.CustomMinimumSize = new Vector2(0, 36);
            headerContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

            var headerStyle = new StyleBoxFlat();
            headerStyle.BgColor = new Color(0.12f, 0.12f, 0.12f);
            var headerPanel = new Panel();
            headerPanel.AddThemeStyleboxOverride("panel", headerStyle);
            headerPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            headerContainer.AddChild(headerPanel);

            // Create tab buttons dynamically
            for (int i = 0; i < _tabs.Count; i++)
            {
                var button = CreateTabButton(headerContainer, _tabs[i].Name, i);
                _tabButtons.Add(button);
            }

            parent.AddChild(headerContainer);
        }

        private void CreateTabContent(Control parent)
        {
            var contentContainer = new Control();
            contentContainer.Name = "TabContent";
            contentContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            contentContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

            var contentStyle = new StyleBoxFlat();
            contentStyle.BgColor = new Color(0.12f, 0.12f, 0.12f);
            var contentPanel = new Panel();
            contentPanel.AddThemeStyleboxOverride("panel", contentStyle);
            contentPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            contentContainer.AddChild(contentPanel);

            // Create tab content areas dynamically
            for (int i = 0; i < _tabs.Count; i++)
            {
                var content = CreateScrollableTabPane(contentContainer, _tabs[i].Name);
                content.Visible = i == 0; // First tab active by default
                _tabContents.Add(content);
            }

            parent.AddChild(contentContainer);
        }

        private Control CreateScrollableTabPane(Control parent, string name)
        {
            // Create scroll container
            var scrollContainer = new ScrollContainer();
            scrollContainer.Name = $"{name}Scroll";
            scrollContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            scrollContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            scrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            scrollContainer.FollowFocus = true;

            // Create content area
            var contentArea = new VBoxContainer();
            contentArea.Name = "Content";
            contentArea.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            contentArea.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

            scrollContainer.AddChild(contentArea);
            parent.AddChild(scrollContainer);

            return contentArea;
        }

        private Button CreateTabButton(Control parent, string label, int tabIndex)
        {
            var button = new Button();
            button.Name = $"Tab_{label}";
            button.Text = label;
            button.CustomMinimumSize = new Vector2(80, 28);
            button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

            var buttonStyle = new StyleBoxFlat();
            buttonStyle.BgColor = new Color(0.15f, 0.15f, 0.15f);
            button.AddThemeStyleboxOverride("normal", buttonStyle);

            button.Pressed += () => SwitchTab(tabIndex);

            parent.AddChild(button);
            return button;
        }
    }
}