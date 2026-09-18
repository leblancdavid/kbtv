using System;
using Godot;
using KBTV.Core;

namespace KBTV.UI
{
    /// <summary>
    /// Manages the main live show UI system.
    /// Autoload that creates and manages the live show layout.
    /// </summary>
    [GlobalClass]
    public partial class CallerScreenerManager : Node, IDependent
    {
        private CanvasLayer _canvas;
        private EventBus? _eventBus;
        private GameStateManager? _gameStateManager;
        private ColorRect? _background;
        private CallerTab? _callerTab;
        private Control? _liveShowFooter;
private CanvasLayer? _transcriptCanvas;
        private TranscriptOverlay? _transcriptOverlay;
        private CanvasLayer? _topOverlayCanvas;
        private TopStateOverlay? _topOverlay;

        public bool IsOpen { get; private set; }
        public event Action? Opened;
        public event Action? Closed;

        public override void _Notification(int what) => this.Notify(what);

        public override void _Ready()
        {
        }

        public void OnResolved()
        {
            GD.Print("CallerScreenerManager: OnResolved");
            CreateUI();
            RegisterWithUIManager();
            SubscribeToEvents();
            SubscribeToPhaseChanges();
        }

        private void CreateUI()
        {
            _canvas = new CanvasLayer();
            _canvas.Name = "CanvasLayer";
            _canvas.Layer = 100;
            AddChild(_canvas);

            _background = new ColorRect();
            _background.Name = "Background";
            _background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _background.MouseFilter = Control.MouseFilterEnum.Ignore;
            _background.Color = new Color(0.05f, 0.05f, 0.05f, 1f);
            _canvas.AddChild(_background);

            var mainLayout = new VBoxContainer();
            mainLayout.Name = "MainLayout";
            mainLayout.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _canvas.AddChild(mainLayout);

            var callerScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/CallerTab.tscn");
            if (callerScene != null)
            {
                _callerTab = callerScene.Instantiate<CallerTab>();
                _callerTab.Name = "LiveShowCallerPanel";
                _callerTab.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                _callerTab.SizeFlagsStretchRatio = 3;
                mainLayout.AddChild(_callerTab);
            }
            else
            {
                Log.Error("CallerScreenerManager: Failed to load CallerTab.tscn");
            }

            var footerScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/LiveShowFooter.tscn");
            if (footerScene != null)
            {
                _liveShowFooter = footerScene.Instantiate<Control>();
                _liveShowFooter.Name = "LiveShowFooter";
                _liveShowFooter.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                _liveShowFooter.SizeFlagsStretchRatio = 1;
                _liveShowFooter.CustomMinimumSize = Vector2.Zero;
                mainLayout.AddChild(_liveShowFooter);
            }
            else
            {
                Log.Error("CallerScreenerManager: Failed to load LiveShowFooter.tscn");
            }

            _transcriptCanvas = new CanvasLayer
            {
                Name = "TranscriptCanvasLayer",
                Layer = 101
            };
            AddChild(_transcriptCanvas);

// Transcript overlay is independent from the caller screener canvas.
            _transcriptOverlay = new TranscriptOverlay();
            _transcriptCanvas.AddChild(_transcriptOverlay);
            _transcriptCanvas.Hide();

            // Top HUD overlay (time / breaks / status feed / listeners / money).
            _topOverlayCanvas = new CanvasLayer
            {
                Name = "TopOverlayCanvasLayer",
                Layer = 103
            };
            AddChild(_topOverlayCanvas);

            _topOverlay = new TopStateOverlay();
            _topOverlayCanvas.AddChild(_topOverlay);
            _topOverlayCanvas.Hide();

            _canvas.Hide();
            IsOpen = false;
        }



        private void InitializeTabs()
        {
        }

        private void RegisterWithUIManager()
        {
            var uiManager = DependencyInjection.Get<IUIManager>(this);
            if (uiManager == null)
            {
                Log.Error("CallerScreenerManager: UIManager is null - cannot register LiveShow layer!");
                return;
            }

            uiManager.RegisterLiveShowLayer(_canvas);
        }

        public void ShowCallersTab()
        {
            if (_canvas != null)
            {
                _canvas.Show();
                if (_background != null)
                {
                    _background.Show();
                }

                if (_callerTab != null)
                {
                    _callerTab.Show();
                }

                if (_liveShowFooter != null)
                {
                    _liveShowFooter.Show();
                }

                IsOpen = true;
                Opened?.Invoke();
                GetTree()?.CallGroup("player", "SetMovementLocked", true);
            }
        }

        public void Hide()
        {
            if (_canvas != null)
            {
                _canvas.Hide();

                if (_callerTab != null)
                {
                    _callerTab.Hide();
                }

                if (_liveShowFooter != null)
                {
                    _liveShowFooter.Hide();
                }

                IsOpen = false;
                Closed?.Invoke();
                GetTree()?.CallGroup("player", "SetMovementLocked", false);
            }
        }

        public void Show()
        {
            if (IsOpen)
            {
                return;
            }

            ShowCallersTab();
        }

        private void SubscribeToEvents()
        {
            _eventBus = DependencyInjection.Get<EventBus>(this);
            if (_eventBus == null)
            {
                Log.Error("CallerScreenerManager: EventBus not available");
                return;
            }

            GD.Print("CallerScreenerManager: Subscribed to ScreeningRequestedEvent");
            _eventBus.Subscribe<ScreeningRequestedEvent>(HandleScreeningRequested);
        }

private void SubscribeToPhaseChanges()
        {
            _gameStateManager = DependencyInjection.Get<GameStateManager>(this);
            if (_gameStateManager == null)
            {
                Log.Error("CallerScreenerManager: GameStateManager not available");
                return;
            }

            _gameStateManager.OnPhaseChanged += HandlePhaseChanged;
            UpdateHudVisibility(_gameStateManager.CurrentPhase);
        }

        private void HandlePhaseChanged(GamePhase oldPhase, GamePhase newPhase)
        {
            UpdateHudVisibility(newPhase);
        }

        private void UpdateHudVisibility(GamePhase phase)
        {
            bool visible = phase == GamePhase.LiveShow;

            if (_transcriptCanvas != null)
            {
                if (visible) _transcriptCanvas.Show();
                else _transcriptCanvas.Hide();
            }

            if (_topOverlayCanvas != null)
            {
                if (visible) _topOverlayCanvas.Show();
                else _topOverlayCanvas.Hide();
            }
        }

        private void HandleScreeningRequested(ScreeningRequestedEvent @event)
        {
            GD.Print("CallerScreenerManager: ScreeningRequestedEvent received");
            Show();
        }

        public override void _ExitTree()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<ScreeningRequestedEvent>(HandleScreeningRequested);
            }

            if (_gameStateManager != null)
            {
                _gameStateManager.OnPhaseChanged -= HandlePhaseChanged;
            }
        }
    }
}
