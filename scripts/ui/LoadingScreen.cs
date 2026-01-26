using Godot;
using KBTV.Core;

namespace KBTV.UI
{
    public partial class LoadingScreen : CanvasLayer
    {
        private Label _titleLabel;
        private Panel _background;

        public override void _Ready()
        {
            GD.Print("LoadingScreen: Initializing loading screen");
            CreateLoadingUI();
            ConnectToGameStateManager();
            GD.Print("LoadingScreen: UI created");
        }

        private void ConnectToGameStateManager()
        {
            // Wait for ServiceRegistry to be initialized
            CallDeferred(nameof(DeferredConnect));
        }

        private void DeferredConnect()
        {
            var gameStateManager = DependencyInjection.Get<GameStateManager>(this);
            if (gameStateManager != null)
            {
                gameStateManager.Connect("PhaseChanged", Callable.From<int, int>(OnPhaseChanged));
                UpdateVisibility(gameStateManager.CurrentPhase);
                GD.Print("LoadingScreen: Connected to GameStateManager");
            }
            else
            {
                GD.PrintErr("LoadingScreen: GameStateManager not available");
            }
        }

        private void OnPhaseChanged(int oldPhaseInt, int newPhaseInt)
        {
            var newPhase = (GamePhase)newPhaseInt;
            UpdateVisibility(newPhase);
        }

        private void UpdateVisibility(GamePhase phase)
        {
            Visible = phase == GamePhase.Loading;
            GD.Print($"LoadingScreen: Visibility set to {Visible} for phase {phase}");
        }

        private void CreateLoadingUI()
        {
            Layer = 200;

            _background = new Panel();
            _background.Name = "Background";
            _background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            var backgroundStyle = new StyleBoxFlat();
            backgroundStyle.BgColor = new Color(0.15f, 0.15f, 0.15f);
            _background.AddThemeStyleboxOverride("panel", backgroundStyle);
            AddChild(_background);

            var containerWrapper = new CenterContainer();
            containerWrapper.Name = "ContainerWrapper";
            containerWrapper.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _background.AddChild(containerWrapper);

            var container = new VBoxContainer();
            container.Name = "Container";
            container.CustomMinimumSize = new Vector2(400, 200);
            container.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            container.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            container.Alignment = BoxContainer.AlignmentMode.Center;
            container.AddThemeConstantOverride("separation", 20);
            containerWrapper.AddChild(container);

            _titleLabel = new Label();
            _titleLabel.Name = "Title";
            _titleLabel.Text = "KBTV RADIO";
            _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _titleLabel.AddThemeColorOverride("font_color", UITheme.ACCENT_GOLD);
            _titleLabel.AddThemeFontSizeOverride("font_size", 32);
            _titleLabel.CustomMinimumSize = new Vector2(0, 50);
            container.AddChild(_titleLabel);

            var loadingLabel = new Label();
            loadingLabel.Name = "LoadingLabel";
            loadingLabel.Text = "INITIALIZING...";
            loadingLabel.HorizontalAlignment = HorizontalAlignment.Center;
            loadingLabel.AddThemeColorOverride("font_color", UITheme.TEXT_PRIMARY);
            loadingLabel.AddThemeFontSizeOverride("font_size", 14);
            container.AddChild(loadingLabel);

            var versionLabel = new Label();
            versionLabel.Name = "VersionLabel";
            versionLabel.Text = "v1.0.0";
            versionLabel.HorizontalAlignment = HorizontalAlignment.Center;
            versionLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.4f));
            versionLabel.AddThemeFontSizeOverride("font_size", 10);
            container.AddChild(versionLabel);

            GD.Print("LoadingScreen: UI created");
        }
    }
}
