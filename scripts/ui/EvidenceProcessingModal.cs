using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KBTV.Core;
using KBTV.Items;
using KBTV.UI.Components;
using KBTV.UI.Themes;

namespace KBTV.UI
{
    public partial class EvidenceProcessingModal : Control, IDependent
    {
        private IEvidenceAnalyzer? _analyzer;
        private IEvidenceCabinet? _cabinet;
        private IEvidenceWebsite? _website;
        private ModalManager? _modalManager;

        private PanelContainer? _panel;
        private VBoxContainer? _contentContainer;
        private Label? _titleLabel;
        private ScrollContainer? _scrollContainer;
        private VBoxContainer? _evidenceListContainer;
        private Button? _closeButton;

        private List<IdentifiedEvidence> _displayedEvidence = new();

        public override void _Notification(int what) => this.Notify(what);

        public override void _Ready()
        {
            BuildUI();
            Visible = false;
        }

        public void OnResolved()
        {
            _analyzer = DependencyInjection.Get<IEvidenceAnalyzer>(this);
            _cabinet = DependencyInjection.Get<IEvidenceCabinet>(this);
            _website = DependencyInjection.Get<IEvidenceWebsite>(this);
            _modalManager = DependencyInjection.Get<ModalManager>(this);
        }

        private void BuildUI()
        {
            _panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                CustomMinimumSize = new Vector2(500, 400)
            };
            _panel.SetAnchorsPreset(Control.LayoutPreset.Center);

            var style = new StyleBoxFlat
            {
                BgColor = UIColors.BG_PANEL,
                CornerRadiusTopLeft = 12,
                CornerRadiusTopRight = 12,
                CornerRadiusBottomLeft = 12,
                CornerRadiusBottomRight = 12,
                ContentMarginLeft = 20,
                ContentMarginRight = 20,
                ContentMarginTop = 20,
                ContentMarginBottom = 20
            };
            _panel.AddThemeStyleboxOverride("panel", style);
            AddChild(_panel);

            _contentContainer = new VBoxContainer();
            _contentContainer.AddThemeConstantOverride("separation", 16);
            _panel.AddChild(_contentContainer);

            _titleLabel = new Label
            {
                Text = "EVIDENCE PROCESSING",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _titleLabel.AddThemeFontSizeOverride("font_size", 20);
            _contentContainer.AddChild(_titleLabel);

            _scrollContainer = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
            };
            _contentContainer.AddChild(_scrollContainer);

            _evidenceListContainer = new VBoxContainer();
            _evidenceListContainer.AddThemeConstantOverride("separation", 8);
            _scrollContainer.AddChild(_evidenceListContainer);

            _closeButton = new Button
            {
                Text = "CLOSE",
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                CustomMinimumSize = new Vector2(100, 36)
            };
            UITheme.ApplyButtonStyle(_closeButton);
            _closeButton.Pressed += OnClosePressed;
            _contentContainer.AddChild(_closeButton);
        }

        public void ShowProcessing(List<IdentifiedEvidence> evidence)
        {
            if (_evidenceListContainer == null) return;

            _displayedEvidence = evidence;
            foreach (var child in _evidenceListContainer.GetChildren())
            {
                child.QueueFree();
            }

            foreach (var item in evidence)
            {
                var itemRow = CreateEvidenceRow(item);
                _evidenceListContainer.AddChild(itemRow);
            }

            Visible = true;
        }

        private Control CreateEvidenceRow(IdentifiedEvidence evidence)
        {
            var container = new PanelContainer();
            var style = new StyleBoxFlat
            {
                BgColor = UIColors.BG_DARK,
                CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6,
                CornerRadiusBottomLeft = 6,
                CornerRadiusBottomRight = 6,
                ContentMarginLeft = 12,
                ContentMarginRight = 12,
                ContentMarginTop = 8,
                ContentMarginBottom = 8
            };
            container.AddThemeStyleboxOverride("panel", style);

            var hbox = new HBoxContainer();
            hbox.AddThemeConstantOverride("separation", 12);
            container.AddChild(hbox);

            var infoBox = new VBoxContainer();
            infoBox.AddThemeConstantOverride("separation", 2);
            infoBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            hbox.AddChild(infoBox);

            var nameLabel = new Label
            {
                Text = evidence.Word,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            nameLabel.AddThemeFontSizeOverride("font_size", 14);
            infoBox.AddChild(nameLabel);

            var tierLabel = new Label
            {
                Text = $"{GetTierName(evidence.Tier)} - {evidence.BonusDescription}",
                HorizontalAlignment = HorizontalAlignment.Left
            };
            tierLabel.AddThemeColorOverride("font_color", GetTierColor(evidence.Tier));
            infoBox.AddChild(tierLabel);

            if (evidence.Status == EvidenceStatus.Processing)
            {
                var timeLabel = new Label
                {
                    Text = $"{(evidence.AnalysisTimeSeconds - (evidence.AnalysisProgress * evidence.AnalysisTimeSeconds)):F0}s remaining",
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                timeLabel.AddThemeColorOverride("font_color", UIColors.Patience.Medium);
                hbox.AddChild(timeLabel);
            }
            else if (evidence.Status == EvidenceStatus.Identified)
            {
                var buttonBox = new HBoxContainer();
                buttonBox.AddThemeConstantOverride("separation", 8);
                hbox.AddChild(buttonBox);

                var cabinetButton = new Button
                {
                    Text = "CABINET",
                    CustomMinimumSize = new Vector2(80, 28)
                };
                UITheme.ApplyButtonStyle(cabinetButton);
                cabinetButton.Pressed += () => OnMoveToCabinet(evidence);
                buttonBox.AddChild(cabinetButton);

                var websiteButton = new Button
                {
                    Text = "WEBSITE",
                    CustomMinimumSize = new Vector2(80, 28)
                };
                UITheme.ApplyButtonStyle(websiteButton);
                websiteButton.Pressed += () => OnMoveToWebsite(evidence);
                buttonBox.AddChild(websiteButton);

                var sellButton = new Button
                {
                    Text = $"SELL ${evidence.SellPrice}",
                    CustomMinimumSize = new Vector2(100, 28)
                };
                UITheme.ApplyButtonStyle(sellButton);
                sellButton.Pressed += () => OnSellEvidence(evidence);
                buttonBox.AddChild(sellButton);
            }

            return container;
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

        private Color GetTierColor(EvidenceTier tier)
        {
            return tier switch
            {
                EvidenceTier.Common => UIColors.TEXT_SECONDARY,
                EvidenceTier.Uncommon => UIColors.Accent.Green,
                EvidenceTier.Rare => UIColors.Accent.Blue,
                EvidenceTier.VeryRare => UIColors.Accent.Purple,
                EvidenceTier.OneOfAKind => UIColors.Accent.Gold,
                _ => UIColors.TEXT_SECONDARY
            };
        }

        private void OnMoveToCabinet(IdentifiedEvidence evidence)
        {
            _cabinet?.MoveToCabinet(evidence);
            RefreshDisplay();
        }

        private void OnMoveToWebsite(IdentifiedEvidence evidence)
        {
            _website?.MoveToWebsite(evidence);
            RefreshDisplay();
        }

        private void OnSellEvidence(IdentifiedEvidence evidence)
        {
            _analyzer?.SellEvidence(evidence);
            RefreshDisplay();
        }

        private void OnClosePressed()
        {
            Visible = false;
        }

        public void RefreshDisplay()
        {
            if (_displayedEvidence.Count > 0)
            {
                ShowProcessing(_displayedEvidence);
            }
        }

        public override void _Process(double delta)
        {
            if (Visible && _displayedEvidence.Count > 0)
            {
                _analyzer?.Update();
                var processingCount = _displayedEvidence.Count(e => e.Status == EvidenceStatus.Processing);
                if (processingCount == 0)
                {
                    RefreshDisplay();
                }
            }
        }
    }
}
