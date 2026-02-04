using Godot;
using KBTV.UI.Themes;

namespace KBTV.UI.Components
{
    /// <summary>
    /// Displays progress for a single topic: experience level, belief tier, and freshness.
    /// </summary>
    [GlobalClass]
    public partial class TopicProgressPanel : PanelContainer
    {
        private string _topicName = "";
        private Label _topicLabel = null!;
        private ProgressBar _xpBar = null!;
        private Label _xpLabel = null!;
        private Label _beliefLabel = null!;
        private ProgressBar _freshnessBar = null!;
        private Label _freshnessLabel = null!;

        public override void _Ready()
        {
            CreateUI();
        }

        private void CreateUI()
        {
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
            marginContainer.AddThemeConstantOverride("margin_left", 8);
            marginContainer.AddThemeConstantOverride("margin_right", 8);
            marginContainer.AddThemeConstantOverride("margin_top", 6);
            marginContainer.AddThemeConstantOverride("margin_bottom", 6);
            AddChild(marginContainer);

            var vbox = new VBoxContainer();
            marginContainer.AddChild(vbox);

            // Topic header
            _topicLabel = new Label();
            _topicLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _topicLabel.AddThemeFontSizeOverride("font_size", 14);
            _topicLabel.AddThemeColorOverride("font_color", UIColors.TEXT_PRIMARY);
            vbox.AddChild(_topicLabel);

            // XP progress
            var xpHBox = new HBoxContainer();
            xpHBox.AddThemeConstantOverride("separation", 4);
            vbox.AddChild(xpHBox);

            _xpLabel = new Label();
            _xpLabel.Text = "Level 1: 0/100 XP";
            _xpLabel.CustomMinimumSize = new Vector2(120, 0);
            xpHBox.AddChild(_xpLabel);

            _xpBar = new ProgressBar();
            _xpBar.CustomMinimumSize = new Vector2(80, 16);
            _xpBar.MaxValue = 100;
            _xpBar.Value = 0;
            _xpBar.ShowPercentage = false;
            xpHBox.AddChild(_xpBar);

            // Belief tier
            _beliefLabel = new Label();
            _beliefLabel.Text = "Belief: SKEPTIC (+0%)";
            _beliefLabel.AddThemeColorOverride("font_color", UIColors.TEXT_SECONDARY);
            vbox.AddChild(_beliefLabel);

            // Freshness meter
            var freshHBox = new HBoxContainer();
            freshHBox.AddThemeConstantOverride("separation", 4);
            vbox.AddChild(freshHBox);

            _freshnessLabel = new Label();
            _freshnessLabel.Text = "Freshness: 100%";
            _freshnessLabel.CustomMinimumSize = new Vector2(100, 0);
            freshHBox.AddChild(_freshnessLabel);

            _freshnessBar = new ProgressBar();
            _freshnessBar.CustomMinimumSize = new Vector2(60, 12);
            _freshnessBar.MaxValue = 100;
            _freshnessBar.Value = 100;
            _freshnessBar.ShowPercentage = false;
            freshHBox.AddChild(_freshnessBar);
        }

        public void SetTopic(string topicName)
        {
            _topicName = topicName;
            _topicLabel.Text = $"─ {_topicName} ─";
        }

        public void UpdateDisplay()
        {
            // TODO: Connect to actual topic data
            // For now, show placeholder data based on topic name

            switch (_topicName)
            {
                case "UFOs":
                    SetXP(3, 245, 300);
                    SetBelief("INTERESTED", 10);
                    SetFreshness(85);
                    break;
                case "Ghosts":
                    SetXP(1, 0, 100);
                    SetBelief("SKEPTIC", 0);
                    SetFreshness(100);
                    break;
                case "Cryptids":
                    SetXP(4, 600, 600);
                    SetBelief("BELIEVER", 15);
                    SetFreshness(60);
                    break;
                case "Conspiracies":
                    SetXP(2, 120, 300);
                    SetBelief("CURIOUS", 5);
                    SetFreshness(95);
                    break;
            }
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
            _freshnessBar.Value = percentage;
        }
    }
}