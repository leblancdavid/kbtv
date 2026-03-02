using System;
using Godot;
using KBTV.Core;
using KBTV.UI.Themes;
using KBTV.UI.Components;
using KBTV.Managers;

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
    private Label _beliefLevelLabel = null!;

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
            UpdateBeliefLevelDisplay();
            SubscribeToBeliefLevelChanges();
            // Removed UpdateDisplay() call - panels now update themselves via TopicXP events
        }

        private void SubscribeToBeliefLevelChanges()
        {
            try
            {
                var topicManager = DependencyInjection.Get<TopicManager>(this);
                foreach (var topicXP in topicManager.GetAllTopicXPs())
                {
                    topicXP.OnLevelChanged += OnTopicLevelChanged;
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"TopicTab: Failed to subscribe to belief level changes - {ex.Message}");
            }
        }

        private void OnTopicLevelChanged(int oldLevel, int newLevel)
        {
            UpdateBeliefLevelDisplay();
        }

        private void UpdateBeliefLevelDisplay()
        {
            try
            {
                var topicManager = DependencyInjection.Get<TopicManager>(this);
                int totalBeliefLevel = topicManager.GetTotalBeliefLevel();
                _beliefLevelLabel.Text = $"Total Belief Level: {totalBeliefLevel}";
            }
            catch (Exception ex)
            {
                GD.PrintErr($"TopicTab: Failed to update belief level display - {ex.Message}");
                _beliefLevelLabel.Text = "Total Belief Level: Error";
            }
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
            header.AddThemeFontSizeOverride("font_size", UITheme.FONT_SMALL);
            mainVBox.AddChild(header);

            // Divider
            var divider = new Label();
            divider.Name = "Divider";
            divider.Text = "═══════════════════════════════════════════════";
            divider.HorizontalAlignment = HorizontalAlignment.Center;
            mainVBox.AddChild(divider);

            // Total Belief Level display
            var beliefLevelLabel = new Label();
            beliefLevelLabel.Name = "BeliefLevelLabel";
            beliefLevelLabel.Text = "Total Belief Level: Calculating...";
            beliefLevelLabel.HorizontalAlignment = HorizontalAlignment.Center;
            beliefLevelLabel.AddThemeFontSizeOverride("font_size", UITheme.FONT_BASE);
            mainVBox.AddChild(beliefLevelLabel);

            // Belief level divider
            var beliefDivider = new Label();
            beliefDivider.Name = "BeliefDivider";
            beliefDivider.Text = "───────────────────────────────────────────────";
            beliefDivider.HorizontalAlignment = HorizontalAlignment.Center;
            mainVBox.AddChild(beliefDivider);

            // Store reference for updates
            _beliefLevelLabel = beliefLevelLabel;

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
            _topicsGrid.Set("theme_override_constants/h_separation", UITheme.SPACING_MEDIUM);
            _topicsGrid.Set("theme_override_constants/v_separation", UITheme.SPACING_SMALL);
            scrollContainer.AddChild(_topicsGrid);

            // Recent gains section
            var recentHeader = new Label();
            recentHeader.Name = "RecentHeader";
            recentHeader.Text = "\nRECENT GAINS (Last Show):";
            recentHeader.AddThemeFontSizeOverride("font_size", UITheme.FONT_TINY);
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
