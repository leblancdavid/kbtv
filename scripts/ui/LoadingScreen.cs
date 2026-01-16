using System.Threading.Tasks;
using Godot;
using KBTV.Core;

namespace KBTV.UI
{
    public partial class LoadingScreen : CanvasLayer
    {
        [Export] public string GameScenePath = "res://scenes/Game.tscn";

        private Label _titleLabel;
        private Label _statusLabel;
        private ProgressBar _progressBar;
        private Panel _background;
        private float _animationTimer;
        private string[] _loadingMessages = {
            "Initializing broadcast equipment...",
            "Connecting to caller lines...",
            "Warming up the microphone...",
            "Calibrating VIBE meters...",
            "Syncing with satellite...",
            "Checking for paranormal activity...",
            "Tuning frequency...",
            "Loading conspiracy theories..."
        };
        private int _messageIndex;
        private bool _transitionStarted;
        private float _totalLoadingTime;
        private const float MIN_LOADING_TIME = 1.5f;
        private const float MAX_LOADING_TIME = 10.0f;
        private bool _timeoutTriggered;

        public override void _Ready()
        {
            GD.Print("LoadingScreen: _Ready - blocking until ready");
            _totalLoadingTime = 0f;
            _timeoutTriggered = false;
            CreateLoadingUI();

            _messageIndex = 0;
            _animationTimer = 0f;
            _transitionStarted = false;

            var registry = ServiceRegistry.Instance;
            if (registry != null)
            {
                GD.Print($"LoadingScreen: Found ServiceRegistry, registered={registry.RegisteredCount} services");
                registry.Connect("AllServicesReady", Callable.From(OnAllServicesReady));

                if (ServiceRegistry.IsInitialized)
                {
                    GD.Print("LoadingScreen: Services already ready");
                    _totalLoadingTime = MIN_LOADING_TIME;
                    CallDeferred(nameof(StartFadeTransition));
                }
            }
            else
            {
                GD.PrintErr("LoadingScreen: ServiceRegistry not found!");
                CallDeferred(nameof(WaitForServicesRegistry));
            }
        }

        private void WaitForServicesRegistry()
        {
            var registry = ServiceRegistry.Instance;
            if (registry != null)
            {
                registry.Connect("AllServicesReady", Callable.From(OnAllServicesReady));
                if (ServiceRegistry.IsInitialized)
                {
                    _totalLoadingTime = MIN_LOADING_TIME;
                    StartFadeTransition();
                }
            }
            else
            {
                CallDeferred(nameof(WaitForServicesRegistry));
            }
        }

        public override void _Process(double delta)
        {
            if (!IsInstanceValid(this) || _statusLabel == null || _progressBar == null)
                return;

            float deltaF = (float)delta;
            _totalLoadingTime += deltaF;

            if (!_transitionStarted)
            {
                if (_totalLoadingTime >= MAX_LOADING_TIME && !_timeoutTriggered)
                {
                    _timeoutTriggered = true;
                    var registry = ServiceRegistry.Instance;
                    GD.PrintErr($"LoadingScreen: Timeout reached ({MAX_LOADING_TIME}s), registered={registry?.RegisteredCount ?? 0} services. Proceeding with available services.");
                    StartFadeTransition();
                    return;
                }

                _animationTimer += deltaF;
                if (_animationTimer >= 0.6f)
                {
                    _animationTimer = 0f;
                    _messageIndex = (_messageIndex + 1) % _loadingMessages.Length;
                    _statusLabel.Text = _loadingMessages[_messageIndex];
                }

                if (_progressBar != null)
                {
                    var registry = ServiceRegistry.Instance;
                    if (registry != null)
                    {
                        float progress = registry.InitializationProgress;
                        _progressBar.Value = progress * 100f;
                    }
                }
            }
        }

        private void OnAllServicesReady()
        {
            if (_transitionStarted) return;
            var registry = ServiceRegistry.Instance;
            GD.Print($"LoadingScreen: All services ready after {_totalLoadingTime:F2}s ({registry?.RegisteredCount ?? 0} services)");
            StartFadeTransition();
        }

        private void StartFadeTransition()
        {
            if (_transitionStarted) return;
            _transitionStarted = true;
            GD.Print("LoadingScreen: Starting fade transition");

            CallDeferred(nameof(PerformTransition));
        }

        private async void PerformTransition()
        {
            if (!IsInstanceValid(this) || GetParent() == null)
            {
                GD.PrintErr("LoadingScreen: Node already disposed, aborting transition");
                return;
            }

            var transitionManager = ServiceRegistry.Instance?.GlobalTransitionManager;
            if (transitionManager == null)
            {
                GD.PrintErr("LoadingScreen: GlobalTransitionManager not available, using fallback");
                var tree = GetTree();
                if (tree != null)
                {
                    await ToSignal(tree.CreateTimer(0.5f), "timeout");
                }
                LoadGameScene();
                return;
            }

            await transitionManager.FadeToBlack(0.4f);
            LoadGameScene();
            return;
        }

        private void LoadGameScene()
        {
            _transitionStarted = false;
            GD.Print("LoadingScreen: Loading game scene");
            var tree = GetTree();
            if (tree != null)
            {
                tree.ChangeSceneToFile(GameScenePath);
            }
            else
            {
                GD.PrintErr("LoadingScreen: GetTree() is null during LoadGameScene!");
            }
        }

        private void CreateLoadingUI()
        {
            Layer = 200;

            _background = new Panel();
            _background.Name = "Background";
            _background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _background.Modulate = new Color(0.12f, 0.12f, 0.16f, 1f);
            AddChild(_background);

            var container = new VBoxContainer();
            container.Name = "Container";
            container.SetAnchorsPreset(Control.LayoutPreset.Center);
            container.CustomMinimumSize = new Vector2(400, 300);
            container.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            container.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            container.AddThemeConstantOverride("separation", 20);
            _background.AddChild(container);

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

            _progressBar = new ProgressBar();
            _progressBar.Name = "ProgressBar";
            _progressBar.CustomMinimumSize = new Vector2(300, 20);
            _progressBar.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            _progressBar.Value = 0f;
            _progressBar.ShowPercentage = false;

            var fillStyle = new StyleBoxFlat();
            fillStyle.BgColor = UITheme.ACCENT_GOLD;
            fillStyle.CornerRadiusTopLeft = 3;
            fillStyle.CornerRadiusTopRight = 3;
            fillStyle.CornerRadiusBottomLeft = 3;
            fillStyle.CornerRadiusBottomRight = 3;
            _progressBar.AddThemeStyleboxOverride("fill", fillStyle);

            var bgStyle = new StyleBoxFlat();
            bgStyle.BgColor = UITheme.BG_PANEL;
            bgStyle.BorderColor = UITheme.BG_BORDER;
            bgStyle.BorderWidthBottom = 1;
            bgStyle.BorderWidthTop = 1;
            bgStyle.BorderWidthLeft = 1;
            bgStyle.BorderWidthRight = 1;
            bgStyle.CornerRadiusTopLeft = 3;
            bgStyle.CornerRadiusTopRight = 3;
            bgStyle.CornerRadiusBottomLeft = 3;
            bgStyle.CornerRadiusBottomRight = 3;
            _progressBar.AddThemeStyleboxOverride("background", bgStyle);

            container.AddChild(_progressBar);

            _statusLabel = new Label();
            _statusLabel.Name = "StatusLabel";
            _statusLabel.Text = _loadingMessages[0];
            _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _statusLabel.AddThemeColorOverride("font_color", UITheme.TEXT_PRIMARY);
            _statusLabel.AddThemeFontSizeOverride("font_size", 12);
            container.AddChild(_statusLabel);

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
