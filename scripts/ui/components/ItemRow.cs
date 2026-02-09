#nullable enable

using Godot;
using KBTV.Core;
using KBTV.UI.Themes;
using KBTV.Data;
using KBTV.Managers;

namespace KBTV.UI.Components
{
    /// <summary>
    /// UI component for displaying an item row with quantity and use button.
    /// Shows item name, current quantity, and a button to use the item.
    /// When used, shows a 30-second timer before applying effects.
    /// </summary>
    public partial class ItemRow : HBoxContainer
    {
        private string _itemId = "";
        private string _itemName = "";
        private bool _isCoffee = false;

        // UI components
        private Label? _nameLabel;
        private Button? _useButton;
        private ProgressBar? _timerProgress;
        private Timer? _useTimer;

        // Dependencies
        private ItemManager? _itemManager;
        private VernStats? _vernStats;

        public override void _Ready()
        {
            BuildUI();
            ConnectSignals();
        }

        private void BuildUI()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            AddThemeConstantOverride("separation", 16);

            // Item name label
            _nameLabel = new Label
            {
                Text = _itemName,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsStretchRatio = 1.0f
            };
            AddChild(_nameLabel);

            // Use button (always enabled since infinite)
            _useButton = new Button
            {
                Text = "USE",
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                SizeFlagsStretchRatio = 1.0f,
                CustomMinimumSize = new Vector2(80, 40)  // Make button bigger for easier clicking
            };
            UITheme.ApplyButtonStyle(_useButton);
            AddChild(_useButton);

            // Timer progress bar (initially hidden)
            _timerProgress = new ProgressBar
            {
                Visible = false,
                MinValue = 0,
                MaxValue = 30,
                Value = 0,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsStretchRatio = 1.0f
            };
            AddChild(_timerProgress);

            // Timer node
            _useTimer = new Timer
            {
                WaitTime = 30.0f,
                OneShot = true
            };
            AddChild(_useTimer);
        }

        private void ConnectSignals()
        {
            if (_useButton != null)
            {
                _useButton.Pressed += OnUseButtonPressed;
            }

            if (_useTimer != null)
            {
                _useTimer.Timeout += OnTimerTimeout;
            }
        }

        public void SetItem(string itemId, string itemName)
        {
            _itemId = itemId;
            _itemName = itemName;
            _isCoffee = itemId == "coffee";

            UpdateDisplay();
        }

        public void SetDependencies(ItemManager itemManager, VernStats vernStats)
        {
            _itemManager = itemManager;
            _vernStats = vernStats;
        }

        private void UpdateDisplay()
        {
            if (_nameLabel != null)
            {
                _nameLabel.Text = _itemName;
            }
        }

        private void OnUseButtonPressed()
        {
            if (_useButton == null || _timerProgress == null || _useTimer == null || _itemManager == null || _vernStats == null) return;

            // Start the 30-second timer
            _useButton.Visible = false;
            _timerProgress.Visible = true;
            _timerProgress.Value = 0;

            _useTimer.Start();

            // Update progress bar in real-time
            CallDeferred("StartProgressUpdate");
        }

        private async void StartProgressUpdate()
        {
            if (_useTimer == null || _timerProgress == null) return;

            while (_useTimer.TimeLeft > 0)
            {
                _timerProgress.Value = 30.0 - _useTimer.TimeLeft;
                await ToSignal(GetTree(), "process_frame");
            }
        }

        private void OnTimerTimeout()
        {
            // Timer completed - apply the item effects
            ApplyItemEffects();

            // Reset UI
            if (_useButton != null) _useButton.Visible = true;
            if (_timerProgress != null) _timerProgress.Visible = false;
        }

        private void ApplyItemEffects()
        {
            if (_itemManager == null || _vernStats == null) return;

            // Apply replenishment effects
            if (_isCoffee)
            {
                _itemManager.UseCoffee(_vernStats);
            }
            else
            {
                _itemManager.UseCigarette(_vernStats);
            }
        }
    }
}