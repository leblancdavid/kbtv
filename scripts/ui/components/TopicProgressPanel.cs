using Godot;
using KBTV.UI.Themes;
using KBTV.Managers;
using KBTV.Core;
using KBTV.Data;

namespace KBTV.UI.Components
{
    /// <summary>
    /// Displays progress for a single topic: experience level, XP tier, and freshness.
    /// </summary>
    [GlobalClass]
    public partial class TopicProgressPanel : PanelContainer, IDependent
    {
        private string _topicName = "";
        private Label _topicLabel = null!;
        private Label _xpLabel = null!;
        private ProgressBar _xpBar = null!;
        private Label _levelUpLabel = null!;
        private Label _tierLabel = null!;
        private ProgressBar _freshnessBar = null!;
        private Label _freshnessLabel = null!;
        private bool _uiCreated = false;

        private TopicManager _topicManager = null!;
        private TopicXP _topicXP = null!;

        public override void _Notification(int what) => this.Notify(what);

        public void OnResolved()
        {
            _topicManager = DependencyInjection.Get<TopicManager>(this);
            
            // Subscribe to TopicXP events for real-time updates
            _topicXP = _topicManager.GetTopicXP(_topicName.ToLower());
            _topicXP.OnXPChanged += OnXPChanged;
            _topicXP.OnTierChanged += OnTierChanged;
            
            UpdateDisplay(); // Initial display
        }

        public override void _ExitTree()
        {
            // Clean up event subscriptions
            if (_topicXP != null)
            {
                _topicXP.OnXPChanged -= OnXPChanged;
                _topicXP.OnTierChanged -= OnTierChanged;
            }
        }

        private void OnXPChanged(float oldXP, float newXP)
        {
            GD.Print($"TopicProgressPanel: {_topicName} XP changed from {oldXP} to {newXP}");
            UpdateXPBar(newXP);
            UpdateLevelUpIndicator(newXP);
        }

        private void OnTierChanged(XPTier oldTier, XPTier newTier)
        {
            GD.Print($"TopicProgressPanel: {_topicName} tier changed from {oldTier} to {newTier}");
            UpdateTierText(newTier);
            UpdateXPBar(_topicXP.XP); // Update with new tier thresholds
        }

        private void UpdateXPBar(float currentXP)
        {
            var currentTier = _topicXP.CurrentTier;
            var nextTierThreshold = TopicXP.GetTierThreshold(currentTier + 1);
            
            // Update XP display: current/next format (even if over threshold)
            _xpLabel.Text = $"{currentXP:F0}/{nextTierThreshold:F0} XP";
            _xpBar.MaxValue = nextTierThreshold;
            _xpBar.Value = Mathf.Min(currentXP, nextTierThreshold); // Cap at threshold for visual
        }

        private void UpdateLevelUpIndicator(float currentXP)
        {
            var currentTier = _topicXP.CurrentTier;
            var nextTierThreshold = TopicXP.GetTierThreshold(currentTier + 1);
            var canLevelUp = currentXP >= nextTierThreshold && currentTier < XPTier.TrueBeliever;
            
            // Set progress bar color based on level-up readiness
            _xpBar.Modulate = canLevelUp ? Colors.Green : UIColors.Accent.Blue;
            
            // Show/hide level-up indicator
            _levelUpLabel.Visible = canLevelUp;
        }

        private void UpdateTierText(XPTier tier)
        {
            var tierName = TopicXP.GetTierName(tier);
            var mentalBonus = _topicXP.MentalBonus;
            SetTier(tierName, (int)(mentalBonus * 100f));
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

            // Level up ready indicator (initially hidden)
            _levelUpLabel = new Label();
            _levelUpLabel.Text = "READY TO LEVEL UP!";
            _levelUpLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _levelUpLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _levelUpLabel.SizeFlagsVertical = SizeFlags.ShrinkBegin;
            _levelUpLabel.AddThemeColorOverride("font_color", Colors.Green);
            _levelUpLabel.AddThemeFontSizeOverride("font_size", 12);
            _levelUpLabel.Visible = false;
            vbox.AddChild(_levelUpLabel);

            // XP tier
            _tierLabel = new Label();
            _tierLabel.Text = "Tier: SKEPTIC (+0%)";
            _tierLabel.HorizontalAlignment = HorizontalAlignment.Left;
            _tierLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _tierLabel.SizeFlagsVertical = SizeFlags.ShrinkBegin;
            _tierLabel.AddThemeColorOverride("font_color", UIColors.TEXT_SECONDARY);
            vbox.AddChild(_tierLabel);

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
            
            // Get real topic XP data
            var topicXP = _topicXP; // Use cached instance
            var currentXP = topicXP.XP;
            var currentTier = topicXP.CurrentTier;
            var mentalBonus = topicXP.MentalBonus;
            
            // Update all display elements
            UpdateXPBar(currentXP);
            UpdateLevelUpIndicator(currentXP);
            UpdateTierText(currentTier);
            
            // Update freshness (placeholder for now)
            var freshness = GetPlaceholderFreshness(_topicName);
            SetFreshness(freshness);
            
            GD.Print($"Topic {_topicName}: Tier {currentTier}, XP {currentXP}/{TopicXP.GetTierThreshold(currentTier + 1)}");
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

        private void SetTier(string tier, int bonusPercent)
        {
            _tierLabel.Text = $"Tier: {tier} (+{bonusPercent}%)";
        }

        private void SetFreshness(int percentage)
        {
            _freshnessLabel.Text = $"Freshness: {percentage}%";
        }
    }
}