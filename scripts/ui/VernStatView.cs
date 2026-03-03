#nullable enable

using Godot;
using KBTV.Core;
using KBTV.Data;
using KBTV.UI.Components;
using KBTV.UI.Themes;

namespace KBTV.UI
{
    /// <summary>
    /// Main Vern stat view displaying all of Vern's stats in a two-column layout.
    /// </summary>
    public partial class VernStatView : Control, IDependent
    {
        private VernStats? _vernStats;
        private ScrollContainer? _scrollContainer;
        private VBoxContainer? _contentContainer;

        private VibeDisplay? _vibeDisplay;
        private StatGroup? _dependenciesGroup;
        private StatGroup? _coreStatsGroup;
        private VernStatusPanel? _statusPanel;

        public override void _Notification(int what) => this.Notify(what);

        public override void _Ready()
        {
            // UI will be built after OnResolved when we have access to VernStats
        }

        public void OnResolved()
        {
            var gameStateManager = DependencyInjection.Get<IGameStateManager>(this);
            if (gameStateManager == null)
            {
                Log.Error("VernStatView: GameStateManager is null - cannot get VernStats!");
                return;
            }

            _vernStats = gameStateManager.VernStats;
            if (_vernStats == null)
            {
                Log.Error("VernStatView: VernStats is null!");
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

            CreateVibeDisplay(innerContainer);
            CreateTwoColumnLayout(innerContainer);
        }

        private void CreateVibeDisplay(VBoxContainer parent)
        {
            if (_vernStats == null) return;

            _vibeDisplay = new VibeDisplay();
            parent.AddChild(_vibeDisplay);
            _vibeDisplay.SetVernStats(_vernStats);
        }

        private void CreateTwoColumnLayout(VBoxContainer parent)
        {
            if (_vernStats == null) return;

            var columnsContainer = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            columnsContainer.AddThemeConstantOverride("separation", UITheme.SPACING_MEDIUM);
            parent.AddChild(columnsContainer);

            var leftColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsStretchRatio = 1.0f
            };
            leftColumn.AddThemeConstantOverride("separation", UITheme.SPACING_MEDIUM);
            columnsContainer.AddChild(leftColumn);

            var rightColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsStretchRatio = 1.0f
            };
            columnsContainer.AddChild(rightColumn);

            CreateDependenciesGroup(leftColumn);
            CreateCoreStatsGroup(leftColumn);
            CreateStatusPanel(rightColumn);
        }

        private void CreateDependenciesGroup(VBoxContainer parent)
        {
            if (_vernStats == null) return;

            _dependenciesGroup = new StatGroup("DEPENDENCIES");
            parent.AddChild(_dependenciesGroup);

            var caffeineBar = new StatBar();
            _dependenciesGroup.AddStatBar(caffeineBar);
            caffeineBar.SetStat(_vernStats.Caffeine);

            var nicotineBar = new StatBar();
            _dependenciesGroup.AddStatBar(nicotineBar);
            nicotineBar.SetStat(_vernStats.Nicotine);
        }

        private void CreateCoreStatsGroup(VBoxContainer parent)
        {
            if (_vernStats == null) return;

            _coreStatsGroup = new StatGroup("CORE STATS");
            parent.AddChild(_coreStatsGroup);

            var physicalBar = new CenteredStatBar();
            _coreStatsGroup.AddCenteredStatBar(physicalBar);
            physicalBar.SetStat(_vernStats.Physical);

            var emotionalBar = new CenteredStatBar();
            _coreStatsGroup.AddCenteredStatBar(emotionalBar);
            emotionalBar.SetStat(_vernStats.Emotional);

            var mentalBar = new CenteredStatBar();
            _coreStatsGroup.AddCenteredStatBar(mentalBar);
            mentalBar.SetStat(_vernStats.Mental);
        }

        private void CreateStatusPanel(VBoxContainer parent)
        {
            if (_vernStats == null) return;

            _statusPanel = new VernStatusPanel();
            parent.AddChild(_statusPanel);
            _statusPanel.SetVernStats(_vernStats);
        }
    }
}
