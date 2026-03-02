using Godot;
using KBTV.UI.Themes;
using KBTV.Core;
using KBTV.Items;
using KBTV.Persistence;
using KBTV.Managers;
using KBTV.Data;
using KBTV.UI.Components;

namespace KBTV.UI
{
    /// <summary>
    /// ITEMS tab displaying consumable items for Vern's stat replenishment.
    /// Shows coffee and cigarette items with use buttons.
    /// </summary>
    public partial class ItemsTab : Control, IDependent
    {
        private ItemManager? _itemManager;
        private VernStats? _vernStats;
        private SaveManager? _saveManager;

        private ScrollContainer? _scrollContainer;
        private VBoxContainer? _contentContainer;

        private ItemRow? _coffeeRow;
        private ItemRow? _cigaretteRow;

        public override void _Notification(int what) => this.Notify(what);

        public override void _Ready()
        {
        }

        public void OnResolved()
        {
            _itemManager = DependencyInjection.Get<ItemManager>(this);
            if (_itemManager == null)
            {
                Log.Error("ItemsTab: ItemManager is null - cannot display items!");
                return;
            }

            var gameStateManager = DependencyInjection.Get<IGameStateManager>(this);
            if (gameStateManager == null)
            {
                Log.Error("ItemsTab: GameStateManager is null - cannot get VernStats!");
                return;
            }

            _vernStats = gameStateManager.VernStats;
            if (_vernStats == null)
            {
                Log.Error("ItemsTab: VernStats is null!");
                return;
            }

            BuildUI();
        }

        private void BuildUI()
        {
            _scrollContainer = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                VerticalScrollMode = ScrollContainer.ScrollMode.Auto
            };
            _scrollContainer.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(_scrollContainer);

            _contentContainer = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            _contentContainer.AddThemeConstantOverride("separation", UITheme.SPACING_MEDIUM);
            _scrollContainer.AddChild(_contentContainer);

            var paddingContainer = new MarginContainer();
            UITheme.ApplyMargins(paddingContainer, UITheme.MARGIN_MEDIUM, UITheme.MARGIN_SMALL, UITheme.MARGIN_MEDIUM, UITheme.MARGIN_SMALL);
            paddingContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;

            var innerContainer = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            innerContainer.AddThemeConstantOverride("separation", UITheme.SPACING_MEDIUM);
            paddingContainer.AddChild(innerContainer);
            _contentContainer.AddChild(paddingContainer);

            var titleLabel = new Label
            {
                Text = "CONSUMABLE ITEMS",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            titleLabel.AddThemeFontSizeOverride("font_size", UITheme.FONT_BASE);
            innerContainer.AddChild(titleLabel);

            CreateItemRows(innerContainer);
        }

        private void CreateItemRows(VBoxContainer parent)
        {
            if (_itemManager == null || _vernStats == null) return;

            _coffeeRow = new ItemRow();
            _coffeeRow.SetItem("coffee", "COFFEE");
            _coffeeRow.SetDependencies(_itemManager, _vernStats);
            parent.AddChild(_coffeeRow);

            _cigaretteRow = new ItemRow();
            _cigaretteRow.SetItem("cigarette", "CIGARETTE");
            _cigaretteRow.SetDependencies(_itemManager, _vernStats);
            parent.AddChild(_cigaretteRow);
        }
    }
}
