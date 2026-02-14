using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KBTV.Core;
using KBTV.Items;
using KBTV.Managers;
using KBTV.Persistence;
using KBTV.UI.Components;
using KBTV.UI.Themes;

namespace KBTV.UI
{
    public partial class EvidenceTab : Control, IDependent
    {
        private IEvidenceAnalyzer? _analyzer;
        private IEvidenceCabinet? _cabinet;
        private IEvidenceWebsite? _website;
        private SaveManager? _saveManager;

        private ScrollContainer? _scrollContainer;
        private VBoxContainer? _contentContainer;

        private bool _rawSectionCreated;
        private bool _processingSectionCreated;
        private bool _identifiedSectionCreated;
        private bool _cabinetSectionCreated;
        private bool _websiteSectionCreated;

        private Label? _rawCountLabel;
        private Button? _commonCountButton;
        private Button? _uncommonCountButton;
        private Button? _rareCountButton;
        private Button? _veryRareCountButton;
        private Button? _oneOfAKindCountButton;
        private Label? _processingCountLabel;
        private Label? _identifiedCountLabel;
        private Label? _cabinetCountLabel;
        private Label? _websiteCountLabel;

        public override void _Notification(int what) => this.Notify(what);

        public override void _Ready()
        {
        }

        public override void _Process(double delta)
        {
            // Refresh processing section every frame to update timer
            RefreshProcessingSection();
        }

        public void OnResolved()
        {
            _analyzer = DependencyInjection.Get<IEvidenceAnalyzer>(this);
            _cabinet = DependencyInjection.Get<IEvidenceCabinet>(this);
            _website = DependencyInjection.Get<IEvidenceWebsite>(this);
            _saveManager = DependencyInjection.Get<SaveManager>(this);

            if (_saveManager != null)
            {
                _saveManager.Connect("DataChanged", new Callable(this, "OnDataChanged"));
                _saveManager.Connect("SaveCompleted", new Callable(this, "OnDataChanged"));
                PopulateEvidenceFromSave();
            }

            BuildUI();
            RefreshAllSections();
        }

        private void PopulateEvidenceFromSave()
        {
            if (_saveManager?.CurrentSave == null || _analyzer == null) return;

            var evidenceSystem = _saveManager.CurrentSave.EvidenceSystem;
            var collectedEvidence = evidenceSystem?.RawEvidence != null && evidenceSystem.RawEvidence.Count > 0
                ? ConvertRawEvidenceDataToItems(evidenceSystem.RawEvidence)
                : _saveManager.CurrentSave.CollectedEvidence;

            if (collectedEvidence != null)
            {
                foreach (var item in collectedEvidence)
                {
                    if (!_analyzer.GetRawEvidence().Any(e => e.Word == item.Word && e.SourceCallerName == item.SourceCallerName))
                    {
                        _analyzer.AddEvidence(item);
                    }
                }
            }
        }

        private List<EvidenceItem> ConvertRawEvidenceDataToItems(List<IdentifiedEvidenceData> dataList)
        {
            var items = new List<EvidenceItem>();
            foreach (var data in dataList)
            {
                var item = EvidenceItem.Create(data.Word, data.SourceCallerName, data.EvidenceLevel, (EvidenceTier)data.Tier);
                items.Add(item);
            }
            return items;
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
            _contentContainer.AddThemeConstantOverride("separation", 16);
            _scrollContainer.AddChild(_contentContainer);

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

            var titleLabel = new Label
            {
                Text = "EVIDENCE",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            titleLabel.AddThemeFontSizeOverride("font_size", 18);
            innerContainer.AddChild(titleLabel);

            CreateRawEvidenceSection(innerContainer);
            CreateProcessingSection(innerContainer);
            CreateIdentifiedSection(innerContainer);
            CreateCabinetSection(innerContainer);
            CreateWebsiteSection(innerContainer);
        }

        private void CreateRawEvidenceSection(VBoxContainer parent)
        {
            if (_rawSectionCreated) return;
            _rawSectionCreated = true;

            var separator = new HSeparator();
            parent.AddChild(separator);

            var headerBox = new HBoxContainer();
            headerBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            parent.AddChild(headerBox);

            var titleLabel = new Label
            {
                Text = "RAW EVIDENCE",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            titleLabel.AddThemeFontSizeOverride("font_size", 16);
            headerBox.AddChild(titleLabel);

            _rawCountLabel = new Label
            {
                Text = "0 pieces",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            _rawCountLabel.AddThemeColorOverride("font_color", UIColors.TEXT_SECONDARY);
            headerBox.AddChild(_rawCountLabel);

            var tierCountsContainer = new HBoxContainer();
            tierCountsContainer.AddThemeConstantOverride("separation", 16);
            tierCountsContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            parent.AddChild(tierCountsContainer);

            _commonCountButton = new Button { Text = "Common: 0" };
            UITheme.ApplyButtonStyle(_commonCountButton);
            _commonCountButton.Pressed += () => OnProcessTierPressed(EvidenceTier.Common);
            tierCountsContainer.AddChild(_commonCountButton);

            _uncommonCountButton = new Button { Text = "Uncommon: 0" };
            UITheme.ApplyButtonStyle(_uncommonCountButton);
            _uncommonCountButton.Pressed += () => OnProcessTierPressed(EvidenceTier.Uncommon);
            tierCountsContainer.AddChild(_uncommonCountButton);

            _rareCountButton = new Button { Text = "Rare: 0" };
            UITheme.ApplyButtonStyle(_rareCountButton);
            _rareCountButton.Pressed += () => OnProcessTierPressed(EvidenceTier.Rare);
            tierCountsContainer.AddChild(_rareCountButton);

            _veryRareCountButton = new Button { Text = "Very Rare: 0" };
            UITheme.ApplyButtonStyle(_veryRareCountButton);
            _veryRareCountButton.Pressed += () => OnProcessTierPressed(EvidenceTier.VeryRare);
            tierCountsContainer.AddChild(_veryRareCountButton);

            _oneOfAKindCountButton = new Button { Text = "One of a Kind: 0" };
            UITheme.ApplyButtonStyle(_oneOfAKindCountButton);
            _oneOfAKindCountButton.Pressed += () => OnProcessTierPressed(EvidenceTier.OneOfAKind);
            tierCountsContainer.AddChild(_oneOfAKindCountButton);
        }

        private void CreateProcessingSection(VBoxContainer parent)
        {
            if (_processingSectionCreated) return;
            _processingSectionCreated = true;

            var separator = new HSeparator();
            parent.AddChild(separator);

            var headerBox = new HBoxContainer();
            headerBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            parent.AddChild(headerBox);

            var titleLabel = new Label
            {
                Text = "PROCESSING",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            titleLabel.AddThemeFontSizeOverride("font_size", 16);
            headerBox.AddChild(titleLabel);

            _processingCountLabel = new Label
            {
                Text = "0 analyzing",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            _processingCountLabel.AddThemeColorOverride("font_color", UIColors.Patience.Medium);
            headerBox.AddChild(_processingCountLabel);

            var processingContainer = new VBoxContainer();
            processingContainer.AddThemeConstantOverride("separation", 8);
            parent.AddChild(processingContainer);

            _processingSectionCreated = true;
        }

        private void CreateIdentifiedSection(VBoxContainer parent)
        {
            if (_identifiedSectionCreated) return;
            _identifiedSectionCreated = true;

            var separator = new HSeparator();
            parent.AddChild(separator);

            var headerBox = new HBoxContainer();
            headerBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            parent.AddChild(headerBox);

            var titleLabel = new Label
            {
                Text = "IDENTIFIED EVIDENCE",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            titleLabel.AddThemeFontSizeOverride("font_size", 16);
            headerBox.AddChild(titleLabel);

            _identifiedCountLabel = new Label
            {
                Text = "0 ready",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            _identifiedCountLabel.AddThemeColorOverride("font_color", UIColors.Accent.Green);
            headerBox.AddChild(_identifiedCountLabel);

            var identifiedContainer = new VBoxContainer();
            identifiedContainer.AddThemeConstantOverride("separation", 8);
            parent.AddChild(identifiedContainer);

            _identifiedSectionCreated = true;
        }

        private void CreateCabinetSection(VBoxContainer parent)
        {
            if (_cabinetSectionCreated) return;
            _cabinetSectionCreated = true;

            var separator = new HSeparator();
            parent.AddChild(separator);

            var headerBox = new HBoxContainer();
            headerBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            parent.AddChild(headerBox);

            var titleLabel = new Label
            {
                Text = "FILE CABINET",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            titleLabel.AddThemeFontSizeOverride("font_size", 16);
            headerBox.AddChild(titleLabel);

            _cabinetCountLabel = new Label
            {
                Text = "0/5 slots",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            _cabinetCountLabel.AddThemeColorOverride("font_color", UIColors.Accent.Blue);
            headerBox.AddChild(_cabinetCountLabel);

            var bonusLabel = new Label
            {
                Text = GetCabinetBonusText(),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            bonusLabel.AddThemeColorOverride("font_color", UIColors.TEXT_SECONDARY);
            parent.AddChild(bonusLabel);

            var upgradeButton = new Button
            {
                Text = "Upgrade Cabinet (+5 slots - $300)",
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                CustomMinimumSize = new Vector2(220, 36)
            };
            UITheme.ApplyButtonStyle(upgradeButton);
            upgradeButton.Pressed += OnUpgradeCabinetPressed;
            parent.AddChild(upgradeButton);

            var cabinetContainer = new VBoxContainer();
            cabinetContainer.AddThemeConstantOverride("separation", 8);
            parent.AddChild(cabinetContainer);

            _cabinetSectionCreated = true;
        }

        private void CreateWebsiteSection(VBoxContainer parent)
        {
            if (_websiteSectionCreated) return;
            _websiteSectionCreated = true;

            var separator = new HSeparator();
            parent.AddChild(separator);

            var headerBox = new HBoxContainer();
            headerBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            parent.AddChild(headerBox);

            var titleLabel = new Label
            {
                Text = "WEBSITE",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            titleLabel.AddThemeFontSizeOverride("font_size", 16);
            headerBox.AddChild(titleLabel);

            _websiteCountLabel = new Label
            {
                Text = "0/5 slots",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            _websiteCountLabel.AddThemeColorOverride("font_color", UIColors.Accent.Purple);
            headerBox.AddChild(_websiteCountLabel);

            var incomeLabel = new Label
            {
                Text = GetWebsiteIncomeText(),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            incomeLabel.AddThemeColorOverride("font_color", UIColors.TEXT_SECONDARY);
            parent.AddChild(incomeLabel);

            var upgradeButton = new Button
            {
                Text = "Upgrade Website (+5 slots - $400)",
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                CustomMinimumSize = new Vector2(220, 36)
            };
            UITheme.ApplyButtonStyle(upgradeButton);
            upgradeButton.Pressed += OnUpgradeWebsitePressed;
            parent.AddChild(upgradeButton);

            var websiteContainer = new VBoxContainer();
            websiteContainer.AddThemeConstantOverride("separation", 8);
            parent.AddChild(websiteContainer);

            _websiteSectionCreated = true;
        }

        private void OnProcessTierPressed(EvidenceTier tier)
        {
            if (_analyzer == null) return;

            // Check if we have evidence of this tier and not currently processing
            int count = _analyzer.GetRawCountByTier(tier);
            if (count == 0 || _analyzer.IsProcessingEvidence) return;

            // Find the first evidence of this tier
            var evidence = _analyzer.GetRawEvidence().FirstOrDefault(e => e.Tier == tier);
            if (evidence == null) return;

            // Start processing
            _analyzer.StartProcessingSpecificEvidence(evidence);

            // Refresh UI
            RefreshRawSection();
            RefreshProcessingSection();
        }

        private void OnUpgradeCabinetPressed()
        {
            if (_cabinet == null) return;

            var cost = _cabinet.GetUpgradeCost();
            var economyManager = ServiceRegistry.Instance?.EconomyManager;
            if (economyManager != null && economyManager.SpendMoney(cost, "Upgrade Evidence Cabinet"))
            {
                _cabinet.Upgrade(_cabinet.Capacity / 5 + 1);
                RefreshAllSections();
            }
        }

        private void OnUpgradeWebsitePressed()
        {
            if (_website == null) return;

            var cost = _website.GetUpgradeCost();
            var economyManager = ServiceRegistry.Instance?.EconomyManager;
            if (economyManager != null && economyManager.SpendMoney(cost, "Upgrade Evidence Website"))
            {
                _website.Upgrade(_website.Capacity / 5 + 1);
                RefreshAllSections();
            }
        }

        public void OnDataChanged()
        {
            RefreshAllSections();
        }

        private void RefreshAllSections()
        {
            PopulateEvidenceFromSave();
            RefreshRawSection();
            RefreshProcessingSection();
            RefreshIdentifiedSection();
            RefreshCabinetSection();
            RefreshWebsiteSection();
        }

        private void RefreshRawSection()
        {
            if (_rawCountLabel == null || _analyzer == null) return;

            int count = _analyzer.GetRawCount();
            _rawCountLabel.Text = $"{count} piece{(count == 1 ? "" : "s")}";

            bool isProcessing = _analyzer.IsProcessingEvidence;

            if (_commonCountButton != null)
            {
                int commonCount = _analyzer.GetRawCountByTier(EvidenceTier.Common);
                _commonCountButton.Text = $"Common: {commonCount}";
                _commonCountButton.Disabled = isProcessing || commonCount == 0;
            }
            if (_uncommonCountButton != null)
            {
                int uncommonCount = _analyzer.GetRawCountByTier(EvidenceTier.Uncommon);
                _uncommonCountButton.Text = $"Uncommon: {uncommonCount}";
                _uncommonCountButton.Disabled = isProcessing || uncommonCount == 0;
            }
            if (_rareCountButton != null)
            {
                int rareCount = _analyzer.GetRawCountByTier(EvidenceTier.Rare);
                _rareCountButton.Text = $"Rare: {rareCount}";
                _rareCountButton.Disabled = isProcessing || rareCount == 0;
            }
            if (_veryRareCountButton != null)
            {
                int veryRareCount = _analyzer.GetRawCountByTier(EvidenceTier.VeryRare);
                _veryRareCountButton.Text = $"Very Rare: {veryRareCount}";
                _veryRareCountButton.Disabled = isProcessing || veryRareCount == 0;
            }
            if (_oneOfAKindCountButton != null)
            {
                int oneOfAKindCount = _analyzer.GetRawCountByTier(EvidenceTier.OneOfAKind);
                _oneOfAKindCountButton.Text = $"One of a Kind: {oneOfAKindCount}";
                _oneOfAKindCountButton.Disabled = isProcessing || oneOfAKindCount == 0;
            }
        }

        private void RefreshProcessingSection()
        {
            if (_processingCountLabel == null || _analyzer == null) return;

            var processingEvidence = _analyzer.GetProcessingEvidence();
            if (processingEvidence.Count > 0)
            {
                var evidence = processingEvidence[0]; // Only one can be processing
                float timeRemaining = evidence.AnalysisTimeSeconds - ((float)evidence.AnalysisProgress * evidence.AnalysisTimeSeconds);
                string tierName = GetTierName(evidence.Tier);
                _processingCountLabel.Text = $"Analyzing '{evidence.Word}' ({tierName}) - {timeRemaining:F0}s remaining";
            }
            else
            {
                _processingCountLabel.Text = "None processing";
            }
        }

        private void RefreshIdentifiedSection()
        {
            if (_identifiedCountLabel == null || _analyzer == null) return;

            int count = _analyzer.GetIdentifiedCount();
            _identifiedCountLabel.Text = $"{count} ready";
        }

        private void RefreshCabinetSection()
        {
            if (_cabinetCountLabel == null || _cabinet == null) return;

            _cabinetCountLabel.Text = $"{_cabinet.Used}/{_cabinet.Capacity} slots";
        }

        private void RefreshWebsiteSection()
        {
            if (_websiteCountLabel == null || _website == null) return;

            _websiteCountLabel.Text = $"{_website.Used}/{_website.Capacity} slots";
        }

        private string GetTierName(EvidenceTier tier)
        {
            return tier switch
            {
                EvidenceTier.Common => "Common",
                EvidenceTier.Uncommon => "Uncommon",
                EvidenceTier.Rare => "Rare",
                EvidenceTier.VeryRare => "Very Rare",
                EvidenceTier.OneOfAKind => "One of a Kind",
                _ => "Unknown"
            };
        }

        private string GetCabinetBonusText()
        {
            if (_cabinet == null) return "No bonuses active";

            var bonuses = new List<string>();
            float phys = _cabinet.GetPhysicalBonus();
            float emot = _cabinet.GetEmotionalBonus();
            float ment = _cabinet.GetMentalBonus();

            if (phys > 0) bonuses.Add($"Physical +{phys:F0}");
            if (emot > 0) bonuses.Add($"Emotional +{emot:F0}");
            if (ment > 0) bonuses.Add($"Mental +{ment:F0}");

            return bonuses.Count > 0 ? string.Join(" | ", bonuses) : "No bonuses active";
        }

        private string GetWebsiteIncomeText()
        {
            if (_website == null) return "No passive income";

            float income = _website.CalculatePassiveIncome();
            float multiplier = _website.CalculateListenerMultiplier();
            string incomeStr = income > 0 ? $"${income:F0}/show" : "$0/show";
            string multStr = multiplier > 1f ? $" (+{(multiplier - 1f):P0} listeners)" : "";

            return $"{incomeStr}{multStr}";
        }
    }
}
