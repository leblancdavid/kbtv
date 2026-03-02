using Godot;
using KBTV.Core;
using KBTV.Managers;
using KBTV.Persistence;
using KBTV.UI;

namespace KBTV.UI
{
    public partial class PreShowUpgradesPanel : Control, IDependent
    {
        public override void _Notification(int what) => this.Notify(what);

        private VBoxContainer _contentContainer = null!;
        private Label _stationReachLabel = null!;
        private ScrollContainer _citiesScrollContainer = null!;
        private VBoxContainer _citiesContainer = null!;
        private Label _moneyLabel = null!;

        private SaveManager? _saveManager;

        public override void _Ready()
        {
            GD.Print("[PreShowUpgradesPanel] _Ready called");
            CreateUpgradesPanel();
            RefreshData();
            GD.Print("[PreShowUpgradesPanel] _Ready complete");
        }

        public void OnResolved()
        {
            GD.Print("[PreShowUpgradesPanel] OnResolved called");
            _saveManager = DependencyInjection.Get<SaveManager>(this);
            RefreshData();
        }

        private void CreateUpgradesPanel()
        {
            // Set this control to fill the parent container
            AnchorLeft = 0;
            AnchorTop = 0;
            AnchorRight = 1;
            AnchorBottom = 1;
            OffsetLeft = 0;
            OffsetTop = 0;
            OffsetRight = 0;
            OffsetBottom = 0;

            // Create content container directly
            _contentContainer = new VBoxContainer();
            _contentContainer.Name = "UpgradesContent";
            _contentContainer.AnchorLeft = 0;
            _contentContainer.AnchorTop = 0;
            _contentContainer.AnchorRight = 1;
            _contentContainer.AnchorBottom = 1;
            _contentContainer.OffsetLeft = UITheme.MARGIN_SMALL;
            _contentContainer.OffsetTop = UITheme.MARGIN_SMALL;
            _contentContainer.OffsetRight = -UITheme.MARGIN_SMALL;
            _contentContainer.OffsetBottom = -UITheme.MARGIN_SMALL;
            _contentContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            _contentContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _contentContainer.AddThemeConstantOverride("separation", UITheme.SPACING_SMALL);
            AddChild(_contentContainer);

            var title = CreateTitle();
            title.SizeFlagsStretchRatio = 0;
            _contentContainer.AddChild(title);

            var spacer1 = UITheme.CreateSpacer(false, true);
            spacer1.SizeFlagsStretchRatio = 0.4f;
            _contentContainer.AddChild(spacer1);

            var reachContainer = CreateStationReachDisplay();
            reachContainer.SizeFlagsStretchRatio = 0;
            _contentContainer.AddChild(reachContainer);

            var sectionTitle = new Label();
            sectionTitle.Text = "CITIES";
            sectionTitle.HorizontalAlignment = HorizontalAlignment.Center;
            sectionTitle.AddThemeColorOverride("font_color", UITheme.ACCENT_GOLD);
            sectionTitle.AddThemeFontSizeOverride("font_size", UITheme.FONT_SMALL);
            _contentContainer.AddChild(sectionTitle);

            _citiesScrollContainer = new ScrollContainer();
            _citiesScrollContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _citiesScrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _citiesScrollContainer.CustomMinimumSize = new Vector2(0, UITheme.Scale(140));
            _contentContainer.AddChild(_citiesScrollContainer);

            _citiesContainer = new VBoxContainer();
            _citiesContainer.Name = "CitiesContainer";
            _citiesContainer.AddThemeConstantOverride("separation", UITheme.SPACING_MEDIUM);
            _citiesScrollContainer.AddChild(_citiesContainer);

            var spacer2 = UITheme.CreateSpacer(false, true);
            spacer2.SizeFlagsStretchRatio = 0.4f;
            _contentContainer.AddChild(spacer2);

            _moneyLabel = new Label();
            _moneyLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _moneyLabel.AddThemeColorOverride("font_color", UITheme.ACCENT_GREEN);
            _moneyLabel.AddThemeFontSizeOverride("font_size", UITheme.FONT_SMALL);
            _moneyLabel.SizeFlagsStretchRatio = 0;
            _contentContainer.AddChild(_moneyLabel);

            UpdateLayout();
        }

        private Control CreateTitle()
        {
            var title = new Label();
            title.Text = "STATION UPGRADES";
            title.HorizontalAlignment = HorizontalAlignment.Center;
            title.AddThemeColorOverride("font_color", UITheme.ACCENT_GOLD);
            title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            title.CustomMinimumSize = new Vector2(0, UITheme.Scale(36));
            title.AddThemeFontSizeOverride("font_size", UITheme.FONT_SMALL);
            return title;
        }

        private Control CreateStationReachDisplay()
        {
            var container = new VBoxContainer();
            container.AddThemeConstantOverride("separation", UITheme.SPACING_SMALL);

            var label = new Label();
            label.Text = "STATION REACH";
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.AddThemeColorOverride("font_color", UITheme.ACCENT_GOLD);
            container.AddChild(label);

            _stationReachLabel = new Label();
            _stationReachLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _stationReachLabel.AddThemeFontSizeOverride("font_size", UITheme.FONT_BASE);
            _stationReachLabel.AddThemeColorOverride("font_color", Colors.White);
            container.AddChild(_stationReachLabel);

            var sublabel = new Label();
            sublabel.Text = "Maximum Listeners";
            sublabel.HorizontalAlignment = HorizontalAlignment.Center;
            sublabel.AddThemeFontSizeOverride("font_size", UITheme.FONT_TINY);
            sublabel.AddThemeColorOverride("font_color", Colors.Gray);
            container.AddChild(sublabel);

            return container;
        }

        private void UpdateLayout()
        {
            if (_contentContainer != null)
            {
                _contentContainer.QueueSort();
                _contentContainer.QueueRedraw();
            }
        }

        public void RefreshData()
        {
            if (_saveManager?.CurrentSave == null) return;

            var save = _saveManager.CurrentSave;

            _stationReachLabel.Text = $"{save.StationReach:N0}";

            int money = save.Money;
            _moneyLabel.Text = $"${money:N0}";

            RebuildCityCards(save);
        }

        private void RebuildCityCards(SaveData save)
        {
            foreach (Node child in _citiesContainer.GetChildren())
            {
                child.QueueFree();
            }

            if (save.Cities == null) return;

            foreach (var city in save.Cities)
            {
                var card = new CityUpgradeCard();
                card.SetCityData(city, 5);
                card.UpgradeClicked += OnCityUpgraded;
                card.UnlockClicked += OnCityUnlocked;
                _citiesContainer.AddChild(card);
            }
        }

        private void OnCityUpgraded()
        {
            RefreshStationReach();
            RefreshData();
        }

        private void OnCityUnlocked()
        {
            RefreshStationReach();
            RefreshData();
        }

        private void RefreshStationReach()
        {
            try
            {
                var listenerManager = DependencyInjection.Get<ListenerManager>(this);
                listenerManager?.RefreshStationReach();
            }
            catch (System.Exception e)
            {
                Log.Warning($"PreShowUpgradesPanel: Could not refresh station reach - {e.Message}");
            }
        }
    }
}
