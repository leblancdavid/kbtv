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

        public override void _Ready()
        {
            GD.Print("LoadingScreen: _Ready - blocking until ready");
            _totalLoadingTime = 0f;
            CreateLoadingUI();

            _messageIndex = 0;
            _animationTimer = 0f;
            _transitionStarted = false;

            var registry = ServiceRegistry.Instance;
            if (registry != null)
            {
                GD.Print($"LoadingScreen: Found ServiceRegistry, registered={registry.RegisteredCount}/{registry.ExpectedCount}");
                registry.Connect("AllServicesReady", Callable.From(OnAllServicesReady));

                if (ServiceRegistry.IsInitialized && registry.RegisteredCount >= registry.ExpectedCount)
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
                if (registry.RegisteredCount >= registry.ExpectedCount)
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
            float deltaF = (float)delta;
            _totalLoadingTime += deltaF;

            if (!_transitionStarted)
            {
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
            GD.Print($"LoadingScreen: All services ready after {_totalLoadingTime:F2}s");
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
            var transitionManager = ServiceRegistry.Instance?.GlobalTransitionManager;
            if (transitionManager == null)
            {
                GD.PrintErr("LoadingScreen: GlobalTransitionManager not available, using fallback");
                await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
                LoadGameScene();
                return;
            }

            await transitionManager.FadeToBlack(0.4f);
            LoadGameScene();
            await ToSignal(GetTree(), "process_frame");
            await transitionManager.FadeFromBlack(0.4f);

            QueueFree();
        }

        private void LoadGameScene()
        {
            _transitionStarted = false;
            GD.Print("LoadingScreen: Loading game scene");
            GetTree().ChangeSceneToFile(GameScenePath);
        }

        private void CreateLoadingUI()
        {
            Layer = 200;

            _background = new Panel();
            _background.Name = "Background";
            _background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _background.Modulate = new Color(0.05f, 0.05f, 0.08f, 1f);
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
            loadingLabel.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
            loadingLabel.AddThemeFontSizeOverride("font_size", 14);
            container.AddChild(loadingLabel);

            _progressBar = new ProgressBar();
            _progressBar.Name = "ProgressBar";
            _progressBar.CustomMinimumSize = new Vector2(300, 20);
            _progressBar.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            _progressBar.Value = 0f;
            _progressBar.ShowPercentage = false;
            container.AddChild(_progressBar);

            _statusLabel = new Label();
            _statusLabel.Name = "StatusLabel";
            _statusLabel.Text = _loadingMessages[0];
            _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _statusLabel.AddThemeColorOverride("font_color", UITheme.TEXT_SECONDARY);
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
