#nullable enable

using Godot;
using KBTV.Core;
using KBTV.Data;
using KBTV.UI.Components;
using KBTV.UI.Themes;

namespace KBTV.UI
{
    /// <summary>
    /// Main VERN tab controller displaying all of Vern's stats in a two-column layout.
    /// 
    /// Layout:
    /// ┌─────────────────────────────────────────────────────────────────────────────┐
    /// │  VIBE  [░░░░░░░████████████░░░░]  +25   FOCUSED     (full width header)     │
    /// ├─────────────────────────────────┬───────────────────────────────────────────┤
    /// │  LEFT COLUMN (50%)              │  RIGHT COLUMN (50%)                       │
    /// │                                 │                                           │
    /// │  ─── DEPENDENCIES ───           │  ─── STATUS ───                           │
    /// │  CAFFEINE  [████████░░░░] 80    │  DECAY RATES                              │
    /// │  NICOTINE  [████░░░░░░░░] 40    │  ├─ Caffeine: -3.75/min (0.75x)           │
    /// │                                 │  └─ Nicotine: -4.00/min (1.00x)           │
    /// │  ─── CORE STATS ───             │                                           │
    /// │  PHYSICAL  [░░░░░|██░░░] +30    │  WITHDRAWAL                               │
    /// │  EMOTIONAL [░░░██|░░░░░] -20    │  └─ None (dependencies OK)                │
    /// │  MENTAL    [░░░░░|█░░░░] +15    │                                           │
    /// │                                 │  STAT INTERACTIONS                        │
    /// │                                 │  └─ None active                           │
    /// │                                 │                                           │
    /// │                                 │  ⚠ CAFFEINE CRASH                         │
    /// │                                 │  ⚠ LISTENERS LEAVING                      │
    /// └─────────────────────────────────┴───────────────────────────────────────────┘
    /// 
    /// Implements IDependent to get VernStats from GameStateManager via DI.
    /// </summary>
    public partial class VernTab : Control, IDependent
    {
        private VernStats? _vernStats;
        private ScrollContainer? _scrollContainer;
        private VBoxContainer? _contentContainer;

        // Stat display components
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
            // Get VernStats from GameStateManager via DI
            var gameStateManager = DependencyInjection.Get<IGameStateManager>(this);
            if (gameStateManager == null)
            {
                Log.Error("VernTab: GameStateManager is null - cannot get VernStats!");
                return;
            }

            _vernStats = gameStateManager.VernStats;
            if (_vernStats == null)
            {
                Log.Error("VernTab: VernStats is null!");
                return;
            }

            BuildUI();
        }

        private void BuildUI()
        {
            // Create scroll container for the entire tab
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

            // Add padding container
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

            // Build sections
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

            // Create horizontal container for two columns (50/50 split)
            var columnsContainer = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            columnsContainer.AddThemeConstantOverride("separation", 24);
            parent.AddChild(columnsContainer);

            // Left column - Stats
            var leftColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsStretchRatio = 1.0f
            };
            leftColumn.AddThemeConstantOverride("separation", 20);
            columnsContainer.AddChild(leftColumn);

            // Right column - Status panel
            var rightColumn = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsStretchRatio = 1.0f
            };
            columnsContainer.AddChild(rightColumn);

            // Build left column content
            CreateDependenciesGroup(leftColumn);
            CreateCoreStatsGroup(leftColumn);

            // Build right column content
            CreateStatusPanel(rightColumn);
        }

        private void CreateDependenciesGroup(VBoxContainer parent)
        {
            if (_vernStats == null) return;

            _dependenciesGroup = new StatGroup("DEPENDENCIES");
            parent.AddChild(_dependenciesGroup);

            // Caffeine (0-100 bar)
            var caffeineBar = new StatBar();
            _dependenciesGroup.AddStatBar(caffeineBar);
            caffeineBar.SetStat(_vernStats.Caffeine);

            // Nicotine (0-100 bar)
            var nicotineBar = new StatBar();
            _dependenciesGroup.AddStatBar(nicotineBar);
            nicotineBar.SetStat(_vernStats.Nicotine);
        }

        private void CreateCoreStatsGroup(VBoxContainer parent)
        {
            if (_vernStats == null) return;

            _coreStatsGroup = new StatGroup("CORE STATS");
            parent.AddChild(_coreStatsGroup);

            // Physical (-100 to +100, centered bar)
            var physicalBar = new CenteredStatBar();
            _coreStatsGroup.AddCenteredStatBar(physicalBar);
            physicalBar.SetStat(_vernStats.Physical);

            // Emotional (-100 to +100, centered bar)
            var emotionalBar = new CenteredStatBar();
            _coreStatsGroup.AddCenteredStatBar(emotionalBar);
            emotionalBar.SetStat(_vernStats.Emotional);

            // Mental (-100 to +100, centered bar)
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
