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
        private Control? _liveShowFooter;
        private Control? _vernStatView;
        private Control? _vernViewContainer;
        private SubViewport? _vernViewport;
        private Camera2D? _vernCamera;
        private TextureRect? _vernViewportTexture;
        private Control? _vernTranscriptPanel;
        private ColorRect? _vernBackdrop;
        private Control? _vernRightPanel;
        private ColorRect? _vernRightOverlay;

        private static readonly float VernCameraZoomScale = 1.15f;
        private static readonly Vector2I VernGridPosition = new Vector2I(5, 2);

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
                _callerTab.BackRequested += OnBackRequested;
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

            var vernScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/VernStatView.tscn");
            if (vernScene != null)
            {
                EnsureVernViewport();

                _vernViewContainer = new Control();
                _vernViewContainer.Name = "VernStatContainer";
                _vernViewContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);

                _vernBackdrop = new ColorRect
                {
                    Name = "VernBackdrop",
                    Color = new Color(0.02f, 0.02f, 0.02f, 1f)
                };
                _vernBackdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                _vernBackdrop.MouseFilter = Control.MouseFilterEnum.Ignore;
                _vernViewContainer.AddChild(_vernBackdrop);

                // Viewport texture MUST be added before the right panel so stats draw on top
                _vernViewportTexture = new TextureRect
                {
                    Name = "VernViewportTexture",
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered
                };
                _vernViewportTexture.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                _vernViewportTexture.MouseFilter = Control.MouseFilterEnum.Ignore;
                if (_vernViewport != null)
                {
                    _vernViewportTexture.Texture = _vernViewport.GetTexture();
                }
                _vernViewContainer.AddChild(_vernViewportTexture);

                _vernRightPanel = new Control
                {
                    Name = "VernRightPanel",
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    SizeFlagsVertical = Control.SizeFlags.ExpandFill
                };
                _vernRightPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                _vernRightPanel.AnchorLeft = 0.5f;
                _vernViewContainer.AddChild(_vernRightPanel);

                _vernRightOverlay = new ColorRect
                {
                    Name = "VernRightOverlay",
                    Color = new Color(0f, 0f, 0f, 0.7f)
                };
                _vernRightOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                _vernRightOverlay.AnchorLeft = 0.5f;
                _vernRightOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
                _vernRightPanel.AddChild(_vernRightOverlay);

                _vernStatView = vernScene.Instantiate<Control>();
                _vernStatView.Name = "VernStatView";
                _vernStatView.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                _vernRightPanel.AddChild(_vernStatView);

                var transcriptScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/LiveShowPanel.tscn");
                if (transcriptScene != null)
                {
                    _vernTranscriptPanel = transcriptScene.Instantiate<Control>();
                    _vernTranscriptPanel.Name = "VernTranscriptPanel";
                    _vernViewContainer.AddChild(_vernTranscriptPanel);
                }
                else
                {
                    Log.Error("CallerScreenerManager: Failed to load LiveShowPanel.tscn");
                }

                var forwardButton = new Button
                {
                    Name = "ForwardButton",
                    Text = "->",
                    CustomMinimumSize = new Vector2(24, 18)
                };
                forwardButton.SetAnchorsPreset(Control.LayoutPreset.TopRight);
                forwardButton.OffsetLeft = -36;
                forwardButton.OffsetTop = 6;
                forwardButton.OffsetRight = -12;
                forwardButton.OffsetBottom = 24;
                UITheme.ApplyButtonStyle(forwardButton);
                forwardButton.Pressed += OnForwardRequested;
                _vernViewContainer.AddChild(forwardButton);

                _vernViewContainer.Hide();
                _canvas.AddChild(_vernViewContainer);
            }
            else
            {
                Log.Error("CallerScreenerManager: Failed to load VernStatView.tscn");
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
                if (_background != null)
                {
                    _background.Show();
                }
                if (_vernViewContainer != null)
                {
                    _vernViewContainer.Hide();
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
                if (_vernViewContainer != null)
                {
                    _vernViewContainer.Hide();
                }

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

        private void ShowVernStatView()
        {
            if (_canvas == null)
            {
                return;
            }

            _canvas.Show();
            if (_background != null)
            {
                _background.Hide();
            }
            if (_callerTab != null)
            {
                _callerTab.Hide();
            }

            if (_liveShowFooter != null)
            {
                _liveShowFooter.Hide();
            }

            EnsureVernViewport();
            UpdateVernViewportSize();
            UpdateVernCameraZoom();
            UpdateVernCameraTarget();
            UpdateVernTranscriptLayout();

            if (_vernViewContainer != null)
            {
                _vernViewContainer.Show();
            }

            IsOpen = true;
            Opened?.Invoke();
            GetTree()?.CallGroup("player", "SetMovementLocked", true);
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

        private void OnBackRequested()
        {
            ShowVernStatView();
        }

        private void OnForwardRequested()
        {
            ShowCallersTab();
        }

        private void EnsureVernViewport()
        {
            if (_vernViewport != null)
            {
                return;
            }

            _vernViewport = new SubViewport
            {
                Name = "VernSubViewport",
                TransparentBg = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always
            };

            var rootViewport = GetViewport();
            if (rootViewport != null)
            {
                _vernViewport.World2D = rootViewport.World2D;
            }

            _vernCamera = new Camera2D
            {
                Name = "VernCamera"
            };

            AddChild(_vernViewport);
            _vernViewport.AddChild(_vernCamera);
            _vernCamera.CallDeferred("make_current");
        }

        private void UpdateVernViewportSize()
        {
            if (_vernViewport == null)
            {
                return;
            }

            var rootViewport = GetViewport();
            if (rootViewport == null)
            {
                return;
            }

            var size = rootViewport.GetVisibleRect().Size;
            _vernViewContainer.CustomMinimumSize = size;
            _vernViewport.Size = new Vector2I((int)size.X, (int)size.Y);
        }

        private void UpdateVernTranscriptLayout()
        {
            if (_vernTranscriptPanel == null)
            {
                return;
            }

            var rootViewport = GetViewport();
            if (rootViewport == null)
            {
                return;
            }

            var size = rootViewport.GetVisibleRect().Size;
            var panelWidth = size.X * 0.6f;
            var panelHeight = size.Y * 0.25f;
            var left = (size.X - panelWidth) * 0.5f;
            var top = size.Y - panelHeight;

            _vernTranscriptPanel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
            _vernTranscriptPanel.OffsetLeft = left;
            _vernTranscriptPanel.OffsetTop = top;
            _vernTranscriptPanel.OffsetRight = left + panelWidth;
            _vernTranscriptPanel.OffsetBottom = top + panelHeight;
        }

        private void UpdateVernCameraTarget()
        {
            if (_vernCamera == null)
            {
                return;
            }

            var worldRoom = GetTree()?.Root?.GetNodeOrNull<global::WorldRoom>("Main/World/WorldRoom");
            if (worldRoom == null)
            {
                return;
            }

            var target = worldRoom.StudioGridToWorld(VernGridPosition);
            var viewportSize = GetViewport()?.GetVisibleRect().Size ?? Vector2.Zero;
            var zoom = _vernCamera.Zoom;
            var worldWidth = zoom.X > 0 ? viewportSize.X / zoom.X : 0f;
            var offsetX = worldWidth * 0.25f;
            // Shift camera RIGHT so Vern appears at 25% from the left edge
            _vernCamera.GlobalPosition = new Vector2(target.X + offsetX, target.Y);
        }

        private void UpdateVernCameraZoom()
        {
            if (_vernCamera == null)
            {
                return;
            }

            var rootViewport = GetViewport();
            if (rootViewport == null)
            {
                return;
            }

            var worldRoom = GetTree()?.Root?.GetNodeOrNull<global::WorldRoom>("Main/World/WorldRoom");
            if (worldRoom == null)
            {
                return;
            }

            var studioBounds = worldRoom.GetStudioBounds();
            if (studioBounds.Size.X <= 0f || studioBounds.Size.Y <= 0f)
            {
                return;
            }

            var viewportSize = rootViewport.GetVisibleRect().Size;
            var zoomX = viewportSize.X / studioBounds.Size.X;
            var zoomY = viewportSize.Y / studioBounds.Size.Y;
            var baseZoom = Mathf.Min(zoomX, zoomY);
            var zoom = baseZoom * VernCameraZoomScale;
            _vernCamera.Zoom = new Vector2(zoom, zoom);
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
                _callerTab.BackRequested -= OnBackRequested;
            }
        }
    }
}
