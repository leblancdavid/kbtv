using System;
using Godot;
using KBTV.Core;
using KBTV.UI.Themes;
using KBTV.UI.Components;

namespace KBTV.UI
{
    /// <summary>
    /// TOPIC tab displaying topic experience, belief progress, and freshness meters.
    /// Shows progression for all four topics: UFOs, Ghosts, Cryptids, Conspiracies.
    /// </summary>
    [GlobalClass]
    public partial class TopicTab : Control, IDependent
    {
        private GridContainer _topicsGrid = null!;
        private VBoxContainer _recentGains = null!;

        // Topic progress panels
        private TopicProgressPanel[] _topicPanels = new TopicProgressPanel[4];

        public override void _Notification(int what) => this.Notify(what);

        public override void _Ready()
        {
            InitializeComponents();
        }

        public void OnResolved()
        {
            CreateTopicPanels();
            UpdateDisplay();
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
            header.Text = "TOPIC EXPERIENCE & BELIEF";
            header.HorizontalAlignment = HorizontalAlignment.Center;
            header.AddThemeFontSizeOverride("font_size", 16);
            mainVBox.AddChild(header);

            // Divider
            var divider = new Label();
            divider.Name = "Divider";
            divider.Text = "═══════════════════════════════════════════════";
            divider.HorizontalAlignment = HorizontalAlignment.Center;
            mainVBox.AddChild(divider);

            // Topics grid (2x2 layout)
            _topicsGrid = new GridContainer();
            _topicsGrid.Name = "TopicsGrid";
            _topicsGrid.Columns = 2;
            _topicsGrid.Set("theme_override_constants/h_separation", 20);
            _topicsGrid.Set("theme_override_constants/v_separation", 10);
            mainVBox.AddChild(_topicsGrid);

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
            string[] topicNames = { "UFOs", "Ghosts", "Cryptids", "Conspiracies" };

            for (int i = 0; i < topicNames.Length; i++)
            {
                var panel = new TopicProgressPanel();
                panel.Name = $"{topicNames[i]}Panel";
                panel.SetTopic(topicNames[i]);
                _topicsGrid.AddChild(panel);
                _topicPanels[i] = panel;
            }
        }

        private void UpdateDisplay()
        {
            // TODO: Connect to actual topic experience/belief data
            // For now, show placeholder data
            foreach (var panel in _topicPanels)
            {
                panel.UpdateDisplay();
            }
        }
    }
}