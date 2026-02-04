using Godot;
using KBTV.UI.Themes;
using KBTV.Managers;
using KBTV.Core;

namespace KBTV.UI.Components
{
    /// <summary>
    /// Displays progress for a single topic: experience level, belief tier, and freshness.
    /// </summary>
    [GlobalClass]
    public partial class TopicProgressPanel : PanelContainer, IDependent
    {
        private string _topicName = "";
        private Label _topicLabel = null!;
        private ProgressBar _xpBar = null!;
        private Label _xpLabel = null!;
        private Label _beliefLabel = null!;
        private ProgressBar _freshnessBar = null!;
        private Label _freshnessLabel = null!;
        private bool _uiCreated = false;

        public override void _Notification(int what) => this.Notify(what);

        private TopicManager _topicManager = null!;

        public override void _Ready()
        {
            if (!_uiCreated)
            {
                CreateUI();
                _uiCreated = true;
            }
        }

        public void OnResolved()
        {
            _topicManager = DependencyInjection.Get<TopicManager>(this);
        }

        private void CreateUI()
        {
            GD.Print($"CreateUI called for {_topicName}");
            
            var panel = new Panel();
            var styleBox = new StyleBoxFlat();
            styleBox.BgColor = UIColors.VernStat.BarBackground;
            styleBox.CornerRadiusTopLeft = 4;
            styleBox.CornerRadiusTopRight = 4;
            styleBox.CornerRadiusBottomLeft = 4;
            styleBox.CornerRadiusBottomRight = 4;
            panel.AddThemeStyleboxOverride("panel", styleBox);
            AddThemeStyleboxOverride("panel", styleBox);

            var marginContainer = new MarginContainer();
            marginContainer.AddThemeConstantOverride("margin_left", 16);
            marginContainer.AddThemeConstantOverride("margin_right", 16);
            marginContainer.AddThemeConstantOverride("margin_top", 12);
            marginContainer.AddThemeConstantOverride("margin_bottom", 12);
            AddChild(marginContainer);

            var vbox = new VBoxContainer();
            vbox.SizeFlagsVertical = SizeFlags.ExpandFill;
            vbox.AddThemeConstantOverride("separation", 8);
            marginContainer.AddChild(vbox);

            // Topic header
            _topicLabel = new Label();
            _topicLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _topicLabel.AddThemeFontSizeOverride("font_size", 14);
            _topicLabel.AddThemeColorOverride("font_color", UIColors.TEXT_PRIMARY);
            vbox.AddChild(_topicLabel);

            // XP progress (emphasized section)
            _xpLabel = new Label();
            _xpLabel.Text = "Level 1: 0/100 XP";
            _xpLabel.HorizontalAlignment = HorizontalAlignment.Left;
            _xpLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _xpLabel.SizeFlagsVertical = SizeFlags.ShrinkBegin;
            _xpLabel.AutowrapMode = TextServer.AutowrapMode.Off;
            _xpLabel.CustomMinimumSize = new Vector2(200, 0);
            _xpLabel.AddThemeColorOverride("font_color", UIColors.TEXT_PRIMARY);
            vbox.AddChild(_xpLabel);

            _xpBar = new ProgressBar();
            _xpBar.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _xpBar.CustomMinimumSize = new Vector2(0, 24); // Taller XP bar
            _xpBar.MaxValue = 100;
            _xpBar.Value = 0;
            _xpBar.ShowPercentage = false;
            vbox.AddChild(_xpBar);

            // Belief tier
            _beliefLabel = new Label();
            _beliefLabel.Text = "Belief: SKEPTIC (+0%)";
            _beliefLabel.HorizontalAlignment = HorizontalAlignment.Left;
            _beliefLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _beliefLabel.SizeFlagsVertical = SizeFlags.ShrinkBegin;
            _beliefLabel.AddThemeColorOverride("font_color", UIColors.TEXT_SECONDARY);
            vbox.AddChild(_beliefLabel);

            // Freshness percentage (text only)
            _freshnessLabel = new Label();
            _freshnessLabel.Text = "Freshness: 100%";
            _freshnessLabel.HorizontalAlignment = HorizontalAlignment.Left;
            _freshnessLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _freshnessLabel.SizeFlagsVertical = SizeFlags.ShrinkBegin;
            _freshnessLabel.AddThemeColorOverride("font_color", UIColors.TEXT_SECONDARY);
            vbox.AddChild(_freshnessLabel);
        }

        public void SetTopic(string topicName)
        {
            GD.Print($"SetTopic called for {topicName}");
            
            // Ensure UI is created before setting data
            if (_topicLabel == null && !_uiCreated)
            {
                CreateUI();
                _uiCreated = true;
            }

            _topicName = topicName;
            _topicLabel.Text = $"─ {_topicName} ─";
        }

        public void UpdateDisplay()
        {
            GD.Print($"UpdateDisplay called for {_topicName}");
            
            // Get real topic belief data
            var topicBelief = _topicManager.GetTopicBelief(_topicName.ToLower());
            var belief = topicBelief.Belief;
            var tier = topicBelief.CurrentTier;
            var mentalBonus = topicBelief.MentalBonus;

            // For now, use placeholder freshness (could be added to TopicBelief later)
            var freshness = GetPlaceholderFreshness(_topicName);

            // Calculate level and XP progress (simplified - no real TopicExperience yet)
            var level = GetLevelForBelief(belief);
            var currentXp = belief - GetXpThresholdForLevel(level - 1);
            var maxXp = GetXpThresholdForLevel(level) - GetXpThresholdForLevel(level - 1);

            GD.Print($"Topic {_topicName}: level {level}, xp {currentXp}/{maxXp}, belief {belief}");
            
            SetXP(level, (int)currentXp, (int)maxXp);
            GD.Print($"XP label set for {_topicName}: {_xpLabel.Text}");
            SetBelief(topicBelief.CurrentTierName, (int)(mentalBonus * 100f));
            SetFreshness(freshness);
        }

        private int GetLevelForBelief(float belief)
        {
            // Simplified level calculation (could be based on TopicExperience)
            if (belief >= 1000) return 5;
            if (belief >= 600) return 4;
            if (belief >= 300) return 3;
            if (belief >= 100) return 2;
            return 1;
        }

        private float GetXpThresholdForLevel(int level)
        {
            return level switch
            {
                0 => 0,
                1 => 100,
                2 => 300,
                3 => 600,
                4 => 1000,
                _ => 1000
            };
        }

        private int GetPlaceholderFreshness(string topicName)
        {
            // Placeholder freshness values
            return topicName switch
            {
                "UFOs" => 85,
                "Ghosts" => 100,
                "Cryptids" => 60,
                "Conspiracies" => 95,
                "Aliens" => 75,
                "Time Travel" => 90,
                _ => 100
            };
        }

        private void SetXP(int level, int current, int max)
        {
            _xpLabel.Text = $"Level {level}: {current}/{max} XP";
            _xpBar.MaxValue = max;
            _xpBar.Value = current;
        }

        private void SetBelief(string tier, int bonusPercent)
        {
            _beliefLabel.Text = $"Belief: {tier} (+{bonusPercent}%)";
        }

        private void SetFreshness(int percentage)
        {
            _freshnessLabel.Text = $"Freshness: {percentage}%";
        }
    }
}