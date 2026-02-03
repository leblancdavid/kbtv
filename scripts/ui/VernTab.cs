#nullable enable

using Godot;
using KBTV.Core;
using KBTV.Data;
using KBTV.UI.Components;
using KBTV.UI.Themes;

namespace KBTV.UI
{
    /// <summary>
    /// Main VERN tab controller displaying all of Vern's stats.
    /// Layout:
    /// - VIBE/Mood at top
    /// - DEPENDENCIES: Caffeine, Nicotine (0-100 bars)
    /// - CORE STATS: Physical, Emotional, Mental (centered -100 to +100 bars)
    /// - TOPIC BELIEF: Current topic belief display (future)
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
                GD.PrintErr("VernTab: GameStateManager is null - cannot get VernStats!");
                return;
            }

            _vernStats = gameStateManager.VernStats;
            if (_vernStats == null)
            {
                GD.PrintErr("VernTab: VernStats is null!");
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
            innerContainer.AddThemeConstantOverride("separation", 20);
            paddingContainer.AddChild(innerContainer);
            _contentContainer.AddChild(paddingContainer);

            // Build sections
            CreateVibeDisplay(innerContainer);
            CreateDependenciesGroup(innerContainer);
            CreateCoreStatsGroup(innerContainer);
            // TODO: CreateTopicBeliefDisplay(innerContainer);
        }

        private void CreateVibeDisplay(VBoxContainer parent)
        {
            if (_vernStats == null) return;

            _vibeDisplay = new VibeDisplay();
            parent.AddChild(_vibeDisplay);
            _vibeDisplay.SetVernStats(_vernStats);
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
    }
}
