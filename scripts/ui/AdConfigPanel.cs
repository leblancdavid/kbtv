using Godot;
using KBTV.UI;

namespace KBTV.UI
{
    /// <summary>
    /// UI panel for configuring ad breaks and show settings.
    /// Extracted from PreShowUIManager to improve maintainability.
    /// </summary>
    public partial class AdConfigPanel : Control
    {
        // UI Controls
        private Button _decreaseDurationButton;
        private Label _durationLabel;
        private Button _increaseDurationButton;
        private Button _decreaseBreaksButton;
        private Label _breaksCountLabel;
        private Button _increaseBreaksButton;
        private Button _decreaseSlotsButton;
        private Label _slotsCountLabel;
        private Button _increaseSlotsButton;
        private Label _revenueEstimateLabel;
        private Label _adTimeEstimateLabel;

        // Configuration values
        private int _showDurationMinutes = 10;
        private int _breaksPerShow = 2;
        private int _slotsPerBreak = 2;

        public AdConfigPanel()
        {
            // Set proper size flags for container layout
            this.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            this.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            this.CustomMinimumSize = new Vector2(0, 150);

            CreateUI();
        }

        private void CreateUI()
        {
            var container = new VBoxContainer();
            container.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            container.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            container.CustomMinimumSize = new Vector2(0, 150);
            AddChild(container);

            // Header
            var header = new Label();
            header.Text = "AD BREAK CONFIGURATION & SHOW SETTINGS";
            header.HorizontalAlignment = HorizontalAlignment.Center;
            header.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            header.AddThemeColorOverride("font_color", UITheme.TEXT_PRIMARY);
            container.AddChild(header);

            container.AddChild(UITheme.CreateSpacer(false, false));

            // Show duration row
            var durationRow = CreateAdConfigRow("SHOW DURATION (MIN)", out _decreaseDurationButton, out _durationLabel, out _increaseDurationButton);
            container.AddChild(durationRow);

            container.AddChild(UITheme.CreateSpacer(false, false));

            // Breaks per show row
            var breaksRow = CreateAdConfigRow("BREAKS PER SHOW", out _decreaseBreaksButton, out _breaksCountLabel, out _increaseBreaksButton);
            container.AddChild(breaksRow);

            container.AddChild(UITheme.CreateSpacer(false, false));

            // Slots per break row
            var slotsRow = CreateAdConfigRow("SLOTS PER BREAK", out _decreaseSlotsButton, out _slotsCountLabel, out _increaseSlotsButton);
            container.AddChild(slotsRow);

            container.AddChild(UITheme.CreateSpacer(false, false));

            // Estimates row
            var estimatesContainer = new HBoxContainer();
            estimatesContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

            var revenueLabel = new Label();
            revenueLabel.Text = "Est. Revenue: ";
            revenueLabel.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
            estimatesContainer.AddChild(revenueLabel);

            _revenueEstimateLabel = new Label();
            _revenueEstimateLabel.Text = "$12 - $24";
            _revenueEstimateLabel.AddThemeColorOverride("font_color", UITheme.ACCENT_GREEN);
            estimatesContainer.AddChild(_revenueEstimateLabel);

            var spacer = new Control();
            spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            estimatesContainer.AddChild(spacer);

            var timeLabel = new Label();
            timeLabel.Text = "Ad Time: ";
            timeLabel.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
            estimatesContainer.AddChild(timeLabel);

            _adTimeEstimateLabel = new Label();
            _adTimeEstimateLabel.Text = "~1:24";
            _adTimeEstimateLabel.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
            estimatesContainer.AddChild(_adTimeEstimateLabel);

            container.AddChild(estimatesContainer);

            // Initialize labels
            UpdateLabels();
            
            // Force layout update
            QueueRedraw();
        }

        private HBoxContainer CreateAdConfigRow(string labelText, out Button decreaseButton, out Label countLabel, out Button increaseButton)
        {
            var row = new HBoxContainer();
            row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.CustomMinimumSize = new Vector2(0, UITheme.BUTTON_HEIGHT + 4);
            row.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

            // Left spacer
            var leftSpacer = new Control();
            leftSpacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(leftSpacer);

            // Label
            var label = new Label();
            label.Text = labelText;
            label.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
            row.AddChild(label);

            row.AddChild(UITheme.CreateSpacer(true, false));

            // Decrease button
            decreaseButton = new Button();
            decreaseButton.Text = "<";
            decreaseButton.CustomMinimumSize = new Vector2(40, 28);
            decreaseButton.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            decreaseButton.MouseFilter = Control.MouseFilterEnum.Pass;
            decreaseButton.FocusMode = Control.FocusModeEnum.All;
            UITheme.ApplyButtonStyle(decreaseButton);
            row.AddChild(decreaseButton);

            row.AddChild(UITheme.CreateSpacer(true, false));

            // Count label
            countLabel = new Label();
            countLabel.HorizontalAlignment = HorizontalAlignment.Center;
            countLabel.CustomMinimumSize = new Vector2(60, 0);
            countLabel.AddThemeColorOverride("font_color", UITheme.ACCENT_GREEN);
            row.AddChild(countLabel);

            row.AddChild(UITheme.CreateSpacer(true, false));

            // Increase button
            increaseButton = new Button();
            increaseButton.Text = ">";
            increaseButton.CustomMinimumSize = new Vector2(40, 28);
            increaseButton.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            increaseButton.MouseFilter = Control.MouseFilterEnum.Pass;
            increaseButton.FocusMode = Control.FocusModeEnum.All;
            UITheme.ApplyButtonStyle(increaseButton);
            row.AddChild(increaseButton);

            // Right spacer
            var rightSpacer = new Control();
            rightSpacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(rightSpacer);

            return row;
        }

        private void UpdateLabels()
        {
            _durationLabel.Text = _showDurationMinutes.ToString();
            _breaksCountLabel.Text = _breaksPerShow.ToString();
            _slotsCountLabel.Text = _slotsPerBreak.ToString();

            // Update estimates
            int totalSlots = _breaksPerShow * _slotsPerBreak;
            int minRevenue = totalSlots * 2; // Local business rate
            int maxRevenue = totalSlots * 10; // Premium sponsor rate
            _revenueEstimateLabel.Text = $"${minRevenue} - ${maxRevenue}";

            int totalAdTime = totalSlots * 18; // 18 seconds per slot
            int minutes = totalAdTime / 60;
            int seconds = totalAdTime % 60;
            _adTimeEstimateLabel.Text = $"~{minutes}:{seconds:D2}";
        }

        // Configuration accessors
        public int ShowDurationMinutes => _showDurationMinutes;
        public int BreaksPerShow => _breaksPerShow;
        public int SlotsPerBreak => _slotsPerBreak;

        // Button event handlers (to be connected by parent)
        public Button DecreaseDurationButton => _decreaseDurationButton;
        public Button IncreaseDurationButton => _increaseDurationButton;
        public Button DecreaseBreaksButton => _decreaseBreaksButton;
        public Button IncreaseBreaksButton => _increaseBreaksButton;
        public Button DecreaseSlotsButton => _decreaseSlotsButton;
        public Button IncreaseSlotsButton => _increaseSlotsButton;

        // Configuration methods
        public void SetShowDuration(int minutes)
        {
            _showDurationMinutes = Mathf.Clamp(minutes, 5, 30);
            UpdateLabels();
        }

        public void SetBreaksPerShow(int breaks)
        {
            _breaksPerShow = Mathf.Clamp(breaks, 0, 10);
            UpdateLabels();
        }

        public void SetSlotsPerBreak(int slots)
        {
            _slotsPerBreak = Mathf.Clamp(slots, 1, 3);
            UpdateLabels();
        }
    }
}