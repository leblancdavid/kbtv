#nullable enable

using Godot;
using System;
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
    /// Shows coffee and cigarette items with use buttons and 30-second timers.
    /// </summary>
    public partial class ItemsTab : Control, IDependent
    {
        private ItemManager? _itemManager;
        private VernStats? _vernStats;
        private SaveManager? _saveManager;

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

            _saveManager = DependencyInjection.Get<SaveManager>(this);
            if (_saveManager == null)
            {
                Log.Error("ItemsTab: SaveManager is null - cannot display evidence!");
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

            // Add evidence section
            CreateEvidenceSection(innerContainer);
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

        private void CreateEvidenceSection(VBoxContainer parent)
        {
            if (_saveManager?.CurrentSave?.CollectedEvidence == null) return;

            var evidence = _saveManager.CurrentSave.CollectedEvidence;
            if (evidence.Count == 0) return;

            // Add separator
            var separator = new HSeparator();
            separator.AddThemeConstantOverride("separation", 20);
            parent.AddChild(separator);

            // Add evidence title
            var evidenceTitleLabel = new Label
            {
                Text = "COLLECTED EVIDENCE",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            evidenceTitleLabel.AddThemeFontSizeOverride("font_size", 18);
            parent.AddChild(evidenceTitleLabel);

            // Add evidence count
            var countLabel = new Label
            {
                Text = $"{evidence.Count} piece{(evidence.Count == 1 ? "" : "s")} collected",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            countLabel.AddThemeColorOverride("font_color", UIColors.TEXT_SECONDARY);
            parent.AddChild(countLabel);

            // Add evidence items
            foreach (var evidenceItem in evidence)
            {
                CreateEvidenceRow(parent, evidenceItem);
            }
        }

        private void CreateEvidenceRow(VBoxContainer parent, EvidenceItem evidenceItem)
        {
            var evidenceContainer = new PanelContainer();
            evidenceContainer.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = UIColors.BG_PANEL,
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
                ContentMarginLeft = 12,
                ContentMarginRight = 12,
                ContentMarginTop = 8,
                ContentMarginBottom = 8
            });

            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 4);

            // Header with word and tier
            var headerHBox = new HBoxContainer();
            headerHBox.AddThemeConstantOverride("separation", 8);

            var wordLabel = new Label
            {
                Text = evidenceItem.Word,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            wordLabel.AddThemeFontSizeOverride("font_size", 16);
            wordLabel.AddThemeColorOverride("font_color", UIColors.TEXT_PRIMARY);
            headerHBox.AddChild(wordLabel);

            var tierLabel = new Label
            {
                Text = $"[{evidenceItem.Tier}]",
                HorizontalAlignment = HorizontalAlignment.Right,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            tierLabel.AddThemeColorOverride("font_color", GetTierColor(evidenceItem.Tier));
            headerHBox.AddChild(tierLabel);

            vbox.AddChild(headerHBox);

            // Description
            var descLabel = new Label
            {
                Text = evidenceItem.Description,
                HorizontalAlignment = HorizontalAlignment.Left,
                AutowrapMode = TextServer.AutowrapMode.Word
            };
            descLabel.AddThemeColorOverride("font_color", UIColors.TEXT_SECONDARY);
            descLabel.AddThemeFontSizeOverride("font_size", 12);
            vbox.AddChild(descLabel);

            // Collection date
            var dateLabel = new Label
            {
                Text = $"Collected: {FormatCollectionDate(evidenceItem.CollectionDate)}",
                HorizontalAlignment = HorizontalAlignment.Left
            };
            dateLabel.AddThemeColorOverride("font_color", UIColors.TEXT_DISABLED);
            dateLabel.AddThemeFontSizeOverride("font_size", 10);
            vbox.AddChild(dateLabel);

            evidenceContainer.AddChild(vbox);
            parent.AddChild(evidenceContainer);
        }

        private Color GetTierColor(string tier)
        {
            return tier switch
            {
                "Legendary" => UIColors.Accent.Gold,
                "Epic" => UIColors.Accent.Red,
                "Rare" => UIColors.Warning.Critical,
                "Uncommon" => UIColors.Warning.Caution,
                "Common" => UIColors.TEXT_SECONDARY,
                _ => UIColors.TEXT_SECONDARY
            };
        }

        private string FormatCollectionDate(string isoDate)
        {
            if (DateTime.TryParse(isoDate, out var date))
            {
                return date.ToLocalTime().ToString("MMM dd, yyyy HH:mm");
            }
            return isoDate;
        }
    }
}