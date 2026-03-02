using Godot;
using KBTV.Core;

namespace KBTV.UI
{
    /// <summary>
    /// Manages the main live show UI system.
    /// Autoload that creates and manages the live show layout.
    /// </summary>
    [GlobalClass]
    public partial class TabContainerManager : Node, IDependent
    {
        private CanvasLayer _canvas;
        private EventBus? _eventBus;

        public override void _Notification(int what) => this.Notify(what);

        public override void _Ready()
        {
        }

        public void OnResolved()
        {
            GD.Print("TabContainerManager: OnResolved");
            CreateUI();
            RegisterWithUIManager();
            SubscribeToEvents();
        }

        private void CreateUI()
        {
            _canvas = new CanvasLayer();
            _canvas.Layer = 100;
            AddChild(_canvas);

            var mainLayout = new VBoxContainer();
            mainLayout.Name = "MainLayout";
            mainLayout.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _canvas.AddChild(mainLayout);

            var callerScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/CallerTab.tscn");
            if (callerScene != null)
            {
                var callerPanel = callerScene.Instantiate<Control>();
                callerPanel.Name = "LiveShowCallerPanel";
                callerPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                mainLayout.AddChild(callerPanel);
            }
            else
            {
                Log.Error("TabContainerManager: Failed to load CallerTab.tscn");
            }

            var footerScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/LiveShowFooter.tscn");
            if (footerScene != null)
            {
                var footer = footerScene.Instantiate<Control>();
                footer.Name = "LiveShowFooter";
                footer.SizeFlagsVertical = Control.SizeFlags.ShrinkEnd;
                footer.CustomMinimumSize = new Vector2(0, 64);
                mainLayout.AddChild(footer);
            }
            else
            {
                Log.Error("TabContainerManager: Failed to load LiveShowFooter.tscn");
            }
        }



        private void InitializeTabs()
        {
        }

        private void RegisterWithUIManager()
        {
            var uiManager = DependencyInjection.Get<IUIManager>(this);
            if (uiManager == null)
            {
                Log.Error("TabContainerManager: UIManager is null - cannot register LiveShow layer!");
                return;
            }

            uiManager.RegisterLiveShowLayer(_canvas);
        }

        public void ShowCallersTab()
        {
            if (_canvas != null)
            {
                _canvas.Show();
            }
        }

        private void OnClosePressed()
        {
            if (_canvas != null)
            {
                _canvas.Hide();
            }
        }

        private void SubscribeToEvents()
        {
            _eventBus = DependencyInjection.Get<EventBus>(this);
            if (_eventBus == null)
            {
                Log.Error("TabContainerManager: EventBus not available");
                return;
            }

            GD.Print("TabContainerManager: Subscribed to ScreeningRequestedEvent");
            _eventBus.Subscribe<ScreeningRequestedEvent>(HandleScreeningRequested);
        }

        private void HandleScreeningRequested(ScreeningRequestedEvent @event)
        {
            GD.Print("TabContainerManager: ScreeningRequestedEvent received");
            ShowCallersTab();
        }

        public override void _ExitTree()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<ScreeningRequestedEvent>(HandleScreeningRequested);
            }
        }
    }
}
