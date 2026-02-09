#nullable enable

using Godot;
using KBTV.Core;
using KBTV.Data;
using KBTV.Managers;
using KBTV.UI.Components;

namespace KBTV.UI
{
    /// <summary>
    /// ITEMS tab displaying consumable items for Vern's stat replenishment.
    /// Shows coffee and cigarette items with use buttons and 30-second timers.
    /// </summary>
    public partial class ItemsTab : Control, IDependent
    {
        private ItemManager? _itemManager;
        private VernStats? _vernStats;

        private ScrollContainer? _scrollContainer;
        private VBoxContainer? _contentContainer;

        // Item rows
        private ItemRow? _coffeeRow;
        private ItemRow? _cigaretteRow;

        public override void _Notification(int what) => this.Notify(what);

        public override void _Ready()
        {
            // UI will be built after OnResolved when we have access to dependencies
        }

        public void OnResolved()
        {
            // Get dependencies via DI
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
            // Create scroll container
            _scrollContainer = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                VerticalScrollMode = ScrollContainer.ScrollMode.Auto
            };
            _scrollContainer.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(_scrollContainer);

            // Create main content container
            _contentContainer = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            _contentContainer.AddThemeConstantOverride("separation", 16);
            _scrollContainer.AddChild(_contentContainer);

            // Add padding
            var paddingContainer = new MarginContainer();
            paddingContainer.AddThemeConstantOverride("margin_left", 16);
            paddingContainer.AddThemeConstantOverride("margin_right", 16);
            paddingContainer.AddThemeConstantOverride("margin_top", 12);
            paddingContainer.AddThemeConstantOverride("margin_bottom", 12);
            paddingContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;

            var innerContainer = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            innerContainer.AddThemeConstantOverride("separation", 16);
            paddingContainer.AddChild(innerContainer);
            _contentContainer.AddChild(paddingContainer);

            // Add title
            var titleLabel = new Label
            {
                Text = "CONSUMABLE ITEMS",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            titleLabel.AddThemeFontSizeOverride("font_size", 18);
            innerContainer.AddChild(titleLabel);

            // Add item rows
            CreateItemRows(innerContainer);
        }

        private void CreateItemRows(VBoxContainer parent)
        {
            if (_itemManager == null || _vernStats == null) return;

            // Coffee row
            _coffeeRow = new ItemRow();
            _coffeeRow.SetItem("coffee", "COFFEE");
            _coffeeRow.SetDependencies(_itemManager, _vernStats);
            parent.AddChild(_coffeeRow);

            // Cigarette row
            _cigaretteRow = new ItemRow();
            _cigaretteRow.SetItem("cigarette", "CIGARETTE");
            _cigaretteRow.SetDependencies(_itemManager, _vernStats);
            parent.AddChild(_cigaretteRow);
        }
    }
}