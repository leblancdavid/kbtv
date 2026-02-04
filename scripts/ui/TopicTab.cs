using System;
using Godot;
using KBTV.Core;
using KBTV.UI.Themes;
using KBTV.UI.Components;

namespace KBTV.UI
{
    /// <summary>
    /// TOPIC tab displaying topic experience, XP progress, and freshness meters.
    /// Shows progression for all four topics: UFOs, Ghosts, Cryptids, Conspiracies.
    /// </summary>
    [GlobalClass]
    public partial class TopicTab : Control, IDependent
    {
        private GridContainer _topicsGrid = null!;
        private VBoxContainer _recentGains = null!;

        // Topic progress panels
        private TopicProgressPanel[] _topicPanels = new TopicProgressPanel[6];

        public override void _Notification(int what) => this.Notify(what);

        public override void _Ready()
        {
            InitializeComponents();
        }

        public void OnResolved()
        {
            CreateTopicPanels();
            // Removed UpdateDisplay() call - panels now update themselves via TopicXP events
        }

        private void InitializeComponents()
        {
            var mainVBox = new VBoxContainer();
            mainVBox.Name = "MainVBox";
            mainVBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            AddChild(mainVBox);

            // Header
            var header = new Label();
            header.Name = "HeaderLabel";
            header.Text = "TOPIC EXPERIENCE & XP";
            header.HorizontalAlignment = HorizontalAlignment.Center;
            header.AddThemeFontSizeOverride("font_size", 16);
            mainVBox.AddChild(header);

            // Divider
            var divider = new Label();
            divider.Name = "Divider";
            divider.Text = "═══════════════════════════════════════════════";
            divider.HorizontalAlignment = HorizontalAlignment.Center;
            mainVBox.AddChild(divider);

            // Scroll container for topics grid
            var scrollContainer = new ScrollContainer();
            scrollContainer.Name = "TopicsScroll";
            scrollContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            scrollContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
            mainVBox.AddChild(scrollContainer);

            // Topics grid (2x2 layout, expands to fill scroll container)
            _topicsGrid = new GridContainer();
            _topicsGrid.Name = "TopicsGrid";
            _topicsGrid.Columns = 2;
            _topicsGrid.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _topicsGrid.SizeFlagsVertical = SizeFlags.ExpandFill;
            _topicsGrid.Set("theme_override_constants/h_separation", 20);
            _topicsGrid.Set("theme_override_constants/v_separation", 10);
            scrollContainer.AddChild(_topicsGrid);

            // Recent gains section
            var recentHeader = new Label();
            recentHeader.Name = "RecentHeader";
            recentHeader.Text = "\nRECENT GAINS (Last Show):";
            recentHeader.AddThemeFontSizeOverride("font_size", 14);
            mainVBox.AddChild(recentHeader);

            _recentGains = new VBoxContainer();
            _recentGains.Name = "RecentGains";
            mainVBox.AddChild(_recentGains);

            // Add placeholder recent gains
            var placeholderGain = new Label();
            placeholderGain.Text = "• No recent gains";
            _recentGains.AddChild(placeholderGain);
        }

        private void CreateTopicPanels()
        {
            string[] topicNames = { "UFOs", "Ghosts", "Cryptids", "Conspiracies", "Aliens", "Time Travel" };

            for (int i = 0; i < topicNames.Length; i++)
            {
                var panel = new TopicProgressPanel();
                panel.Name = $"{topicNames[i]}Panel";
                panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                panel.SizeFlagsVertical = SizeFlags.ExpandFill;
                panel.SetTopic(topicNames[i]);
                _topicsGrid.AddChild(panel);
                _topicPanels[i] = panel;
            }
        }
    }
}