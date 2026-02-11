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

        private bool _evidenceSectionCreated = false;
        private VBoxContainer? _innerContainer;
        
        // Evidence section UI elements for refreshing
        private Label? _totalCountLabel;
        private System.Collections.Generic.List<Label>? _tierCountLabels;

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

            // Connect to save completion event for evidence loading
            _saveManager.Connect("LoadCompleted", new Callable(this, "OnSaveLoaded"));
            _saveManager.Connect("DataChanged", new Callable(this, "OnDataChanged"));
            _saveManager.Connect("SaveCompleted", new Callable(this, "OnSaveCompleted"));
            GD.Print("ItemsTab: Connected to LoadCompleted, DataChanged, and SaveCompleted signals");

            GD.Print($"ItemsTab: Dependencies resolved - SaveManager: {_saveManager != null}, CurrentSave: {_saveManager?.CurrentSave != null}");

            // Initialize tier count labels list
            _tierCountLabels = new System.Collections.Generic.List<Label>();

            if (_saveManager?.CurrentSave?.CollectedEvidence != null)
            {
                GD.Print($"ItemsTab: Found {_saveManager.CurrentSave.CollectedEvidence.Count} pieces of evidence");
                foreach (var evidence in _saveManager.CurrentSave.CollectedEvidence)
                {
                    GD.Print($"ItemsTab: Evidence - Word: {evidence.Word}, Tier: {evidence.Tier}, Caller: {evidence.SourceCallerName}");
                }
            }
            else
            {
                GD.Print("ItemsTab: No evidence found in save data");
                if (_saveManager?.CurrentSave == null)
                {
                    GD.Print("ItemsTab: CurrentSave is null");
                }
                else
                {
                    GD.Print($"ItemsTab: CollectedEvidence is {(_saveManager.CurrentSave.CollectedEvidence != null ? $"not null, count {_saveManager.CurrentSave.CollectedEvidence.Count}" : "null")} (CurrentSave exists but no evidence list)");
                }
            }

            BuildUI();
        }

        public void OnSaveLoaded()
        {
            GD.Print("ItemsTab: Save loaded event received, checking for evidence");
            if (_saveManager?.CurrentSave?.CollectedEvidence?.Count > 0 && !_evidenceSectionCreated)
            {
                GD.Print($"ItemsTab: Evidence available ({_saveManager.CurrentSave.CollectedEvidence.Count} items) - creating section");
                if (_innerContainer != null)
                {
                    CreateEvidenceSection(_innerContainer);
                }
            }
        }

        public void OnDataChanged()
        {
            GD.Print("ItemsTab: DataChanged signal received, refreshing evidence section");
            if (_evidenceSectionCreated)
            {
                RefreshEvidenceSection();
            }
        }

        public void OnSaveCompleted()
        {
            GD.Print("ItemsTab: SaveCompleted signal received, refreshing evidence section");
            if (_evidenceSectionCreated)
            {
                RefreshEvidenceSection();
            }
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

            _innerContainer = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            _innerContainer.AddThemeConstantOverride("separation", 16);
            paddingContainer.AddChild(_innerContainer);
            _contentContainer.AddChild(paddingContainer);

            // Add title
            var titleLabel = new Label
            {
                Text = "CONSUMABLE ITEMS",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            titleLabel.AddThemeFontSizeOverride("font_size", 18);
            _innerContainer.AddChild(titleLabel);

            // Add item rows
            CreateItemRows(_innerContainer);

            // Add evidence section (will be added later if evidence loads)
            CreateEvidenceSection(_innerContainer);
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
            if (_evidenceSectionCreated || _saveManager?.CurrentSave?.CollectedEvidence == null) {
                return;
            }

            var evidence = _saveManager.CurrentSave.CollectedEvidence;

            _evidenceSectionCreated = true;
            
            // Clear and reinitialize tier count labels list
            if (_tierCountLabels == null)
            {
                _tierCountLabels = new System.Collections.Generic.List<Label>();
            }
            else
            {
                _tierCountLabels.Clear();
            }

            // Count evidence by tier
            var tierCounts = new System.Collections.Generic.Dictionary<EvidenceTier, int>();
            foreach (var tier in System.Enum.GetValues<EvidenceTier>())
            {
                tierCounts[tier] = 0;
            }
            foreach (var evidenceItem in evidence)
            {
                tierCounts[evidenceItem.Tier]++;
            }

            // Add separator
            var separator = new HSeparator();
            separator.AddThemeConstantOverride("separation", 20);
            parent.AddChild(separator);

            // Add evidence title
            var evidenceTitleLabel = new Label
            {
                Text = "EVIDENCE",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            evidenceTitleLabel.AddThemeFontSizeOverride("font_size", 18);
            parent.AddChild(evidenceTitleLabel);

            // Add total count
            _totalCountLabel = new Label
            {
                Text = $"{evidence.Count} piece{(evidence.Count == 1 ? "" : "s")} collected",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _totalCountLabel.AddThemeColorOverride("font_color", UIColors.TEXT_SECONDARY);
            parent.AddChild(_totalCountLabel);

            // Create horizontal layout for counts and button
            var evidenceHBox = new HBoxContainer();
            evidenceHBox.AddThemeConstantOverride("separation", 16);
            evidenceHBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            parent.AddChild(evidenceHBox);

            // Left side: Evidence counts
            var countsVBox = new VBoxContainer();
            countsVBox.AddThemeConstantOverride("separation", 4);
            countsVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            evidenceHBox.AddChild(countsVBox);

            // Add tier counts
            foreach (var tier in System.Enum.GetValues<EvidenceTier>())
            {
                var tierCountLabel = new Label
                {
                    Text = $"{GetTierDisplayName(tier)}: {tierCounts[tier]}",
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                tierCountLabel.AddThemeColorOverride("font_color", GetTierColor(tier));
                countsVBox.AddChild(tierCountLabel);
                _tierCountLabels?.Add(tierCountLabel);
            }

            // Right side: Process button
            var processButton = new Button
            {
                Text = "PROCESS EVIDENCE",
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                CustomMinimumSize = new Vector2(140, 40)
            };
            UITheme.ApplyButtonStyle(processButton);
            processButton.Pressed += OnProcessButtonPressed;
            evidenceHBox.AddChild(processButton);
        }

        private void OnProcessButtonPressed()
        {
            // Placeholder implementation - processing logic to be implemented later
            GD.Print("Process Evidence button pressed - processing logic not yet implemented");
        }

        private Color GetTierColor(EvidenceTier tier)
        {
            return tier switch
            {
                EvidenceTier.OneOfAKind => UIColors.Accent.Gold,
                EvidenceTier.VeryRare => UIColors.Accent.Purple,
                EvidenceTier.Rare => UIColors.Accent.Blue,
                EvidenceTier.Uncommon => UIColors.Accent.Green,
                EvidenceTier.Common => UIColors.TEXT_SECONDARY,
                _ => UIColors.TEXT_SECONDARY
            };
        }

        private string GetTierDisplayName(EvidenceTier tier)
        {
            return tier switch
            {
                EvidenceTier.OneOfAKind => "One of a Kind",
                EvidenceTier.VeryRare => "Very Rare",
                EvidenceTier.Rare => "Rare",
                EvidenceTier.Uncommon => "Uncommon",
                EvidenceTier.Common => "Common",
                _ => "Common"
            };
        }

        private void RefreshEvidenceSection()
        {
            if (!_evidenceSectionCreated || _saveManager?.CurrentSave?.CollectedEvidence == null)
            {
                return;
            }

            var evidence = _saveManager.CurrentSave.CollectedEvidence;
            
            // Update total count
            if (_totalCountLabel != null)
            {
                _totalCountLabel.Text = $"{evidence.Count} piece{(evidence.Count == 1 ? "" : "s")} collected";
            }

            // Count evidence by tier
            var tierCounts = new System.Collections.Generic.Dictionary<EvidenceTier, int>();
            foreach (var tier in System.Enum.GetValues<EvidenceTier>())
            {
                tierCounts[tier] = 0;
            }
            foreach (var evidenceItem in evidence)
            {
                tierCounts[evidenceItem.Tier]++;
            }

            // Update tier count labels
            var tiers = System.Enum.GetValues<EvidenceTier>();
            for (int i = 0; i < tiers.Length && i < _tierCountLabels?.Count; i++)
            {
                var tier = tiers[i];
                var label = _tierCountLabels[i];
                if (label != null)
                {
                    label.Text = $"{GetTierDisplayName(tier)}: {tierCounts[tier]}";
                }
            }

            GD.Print($"ItemsTab: Refreshed evidence counts - Total: {evidence.Count}");
        }
    }
}