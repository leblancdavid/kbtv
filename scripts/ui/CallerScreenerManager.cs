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
        private ColorRect? _background;
        private CallerTab? _callerTab;

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
                _callerTab.CloseRequested += OnCloseRequested;
                mainLayout.AddChild(_callerTab);
            }
            else
            {
                Log.Error("CallerScreenerManager: Failed to load CallerTab.tscn");
            }

            var footerScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/LiveShowFooter.tscn");
            if (footerScene != null)
            {
                var footer = footerScene.Instantiate<Control>();
                footer.Name = "LiveShowFooter";
                footer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                footer.SizeFlagsStretchRatio = 1;
                footer.CustomMinimumSize = Vector2.Zero;
                mainLayout.AddChild(footer);
            }
            else
            {
                Log.Error("CallerScreenerManager: Failed to load LiveShowFooter.tscn");
            }

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

        private void HandleScreeningRequested(ScreeningRequestedEvent @event)
        {
            GD.Print("CallerScreenerManager: ScreeningRequestedEvent received");
            Show();
        }

        private void OnCloseRequested()
        {
            Hide();
        }

        public override void _ExitTree()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<ScreeningRequestedEvent>(HandleScreeningRequested);
            }

            if (_callerTab != null)
            {
                _callerTab.CloseRequested -= OnCloseRequested;
            }
        }
    }
}
