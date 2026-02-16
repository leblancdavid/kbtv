using Godot;
using KBTV.Core;
using KBTV.Persistence;
using KBTV.UI;

namespace KBTV.UI
{
    public partial class CityUpgradeCard : VBoxContainer, IDependent
    {
        public override void _Notification(int what) => this.Notify(what);

        private Label _cityNameLabel = null!;
        private Label _antennaLevelLabel = null!;
        private ProgressBar _levelProgressBar = null!;
        private Label _maxListenersLabel = null!;
        private Button _upgradeButton = null!;
        private Label _costLabel = null!;

        private SaveData.CityData? _cityData;
        private int _maxLevel = 5;
        private int _upgradeCost = 0;
        private SaveManager? _saveManager;

        public event System.Action? UpgradeClicked;
        public event System.Action? UnlockClicked;

        public override void _Ready()
        {
            SetupUI();
        }

        public void OnResolved()
        {
            _saveManager = DependencyInjection.Get<SaveManager>(this);
            RefreshDisplay();
        }

        private void SetupUI()
        {
            Name = "CityUpgradeCard";
            AddThemeConstantOverride("separation", 10);

            var headerContainer = new HBoxContainer();
            AddChild(headerContainer);

            _cityNameLabel = new Label();
            _cityNameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _cityNameLabel.AddThemeColorOverride("font_color", UITheme.ACCENT_GOLD);
            headerContainer.AddChild(_cityNameLabel);

            _antennaLevelLabel = new Label();
            _antennaLevelLabel.HorizontalAlignment = HorizontalAlignment.Right;
            headerContainer.AddChild(_antennaLevelLabel);

            _levelProgressBar = new ProgressBar();
            _levelProgressBar.CustomMinimumSize = new Vector2(0, 20);
            _levelProgressBar.ShowPercentage = false;
            AddChild(_levelProgressBar);

            _maxListenersLabel = new Label();
            _maxListenersLabel.AddThemeFontSizeOverride("font_size", 14);
            AddChild(_maxListenersLabel);

            var buttonContainer = new HBoxContainer();
            buttonContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            AddChild(buttonContainer);

            _upgradeButton = new Button();
            _upgradeButton.CustomMinimumSize = new Vector2(120, 36);
            _upgradeButton.Text = "Upgrade";
            _upgradeButton.Pressed += OnUpgradeButtonPressed;
            UITheme.ApplyButtonStyle(_upgradeButton);
            buttonContainer.AddChild(_upgradeButton);

            _costLabel = new Label();
            _costLabel.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            _costLabel.VerticalAlignment = VerticalAlignment.Center;
            buttonContainer.AddChild(_costLabel);
        }

        public void SetCityData(SaveData.CityData cityData, int maxLevel)
        {
            _cityData = cityData;
            _maxLevel = maxLevel;
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (_cityData == null) return;

            _cityNameLabel.Text = _cityData.CityName;

            if (_cityData.IsUnlocked)
            {
                _antennaLevelLabel.Text = $"Antenna Lv {_cityData.AntennaLevel}/{_maxLevel}";
                
                float progress = (float)_cityData.AntennaLevel / _maxLevel;
                _levelProgressBar.Value = progress * 100;
                
                int maxListeners = (_cityData.AntennaLevel * 250) + 500;
                _maxListenersLabel.Text = $"Max Listeners: {maxListeners:N0}";

                if (_cityData.AntennaLevel >= _maxLevel)
                {
                    _upgradeButton.Text = "MAX";
                    _upgradeButton.Disabled = true;
                    _costLabel.Text = "";
                }
                else
                {
                    _upgradeCost = GetUpgradeCost(_cityData.AntennaLevel);
                    _upgradeButton.Text = "Upgrade";
                    _upgradeButton.Disabled = false;
                    _costLabel.Text = $"${_upgradeCost:N0}";
                }
            }
            else
            {
                _antennaLevelLabel.Text = "LOCKED";
                _levelProgressBar.Value = 0;
                _maxListenersLabel.Text = "";
                
                _upgradeButton.Text = "Unlock";
                _upgradeButton.Disabled = _cityData.UnlockCost <= 0;
                _costLabel.Text = _cityData.UnlockCost > 0 ? $"${_cityData.UnlockCost:N0}" : "";
            }
        }

        private int GetUpgradeCost(int currentLevel)
        {
            return currentLevel switch
            {
                1 => 300,
                2 => 600,
                3 => 1200,
                4 => 2500,
                _ => 0
            };
        }

        private void OnUpgradeButtonPressed()
        {
            if (_cityData == null || _saveManager?.CurrentSave == null) return;

            if (_cityData.IsUnlocked)
            {
                if (_cityData.AntennaLevel < _maxLevel)
                {
                    int cost = GetUpgradeCost(_cityData.AntennaLevel);
                    if (_saveManager.CurrentSave.Money >= cost)
                    {
                        _saveManager.CurrentSave.Money -= cost;
                        _cityData.AntennaLevel++;
                        _saveManager.MarkDirty();
                        UpgradeClicked?.Invoke();
                        RefreshDisplay();
                    }
                }
            }
            else
            {
                if (_cityData.UnlockCost > 0 && _saveManager.CurrentSave.Money >= _cityData.UnlockCost)
                {
                    _saveManager.CurrentSave.Money -= _cityData.UnlockCost;
                    _cityData.IsUnlocked = true;
                    _saveManager.MarkDirty();
                    UnlockClicked?.Invoke();
                    RefreshDisplay();
                }
            }
        }

        public void Refresh()
        {
            RefreshDisplay();
        }
    }
}
