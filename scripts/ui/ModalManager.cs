using System;
using Godot;
using KBTV.Core;
using KBTV.Callers;

namespace KBTV.UI
{
    /// <summary>
    /// Singleton manager for modal dialogs.
    /// Handles modal lifecycle, layering, and provides centralized modal access.
    /// Evidence modals are hosted inside the CRT terminal's content host when the
    /// terminal is visible (so the dialog renders on the monitor over the screening
    /// UI), falling back to a fullscreen CanvasLayer otherwise.
    /// </summary>
    public partial class ModalManager : Node
    {
        private static ModalManager? _instance;
        public static ModalManager Instance => _instance ?? throw new InvalidOperationException("ModalManager not initialized");

        public event Action? ModalOpened;
        public event Action? ModalClosed;

        private CanvasLayer? _modalRoot;
        private Control? _currentModal;

        private Control? _modalHost;
        private Func<bool>? _hostVisibleCheck;

        public override void _Ready()
        {
            _instance = this;

            // Create modal root container as CanvasLayer to ensure it's above all UI
            _modalRoot = new CanvasLayer
            {
                Name = "ModalRoot",
                Layer = 150  // Above UI layers (100) but below loading screens (200)
            };
            AddChild(_modalRoot);

            // Connect to viewport size changes
            GetViewport().SizeChanged += OnViewportSizeChanged;
        }

        /// <summary>
        /// Registers the control (CRT content host) that evidence modals should be
        /// parented to when the terminal is visible, plus a predicate reporting
        /// whether that host is currently on screen.
        /// </summary>
        public void SetModalHost(Control? host, Func<bool>? isHostVisible)
        {
            _modalHost = host;
            _hostVisibleCheck = isHostVisible;
        }

        private bool IsHostUsable() =>
            _modalHost != null &&
            GodotObject.IsInstanceValid(_modalHost) &&
            (_hostVisibleCheck == null || _hostVisibleCheck());

        public void ShowEvidenceModal(Caller? caller = null)
        {
            if (_currentModal != null)
            {
                GD.PrintErr("ModalManager: Another modal is already open");
                return;
            }

            // Load and instantiate evidence modal
            var modalScene = GD.Load<PackedScene>("res://scenes/ui/EvidenceModal.tscn");
            if (modalScene == null)
            {
                GD.PrintErr("ModalManager: Could not load EvidenceModal scene");
                return;
            }

            var modal = modalScene.Instantiate<EvidenceModal>();
            modal.Initialize(caller);
            modal.ModalClosed += OnModalClosed;
            _currentModal = modal;

            if (IsHostUsable())
            {
                // Render inside the CRT viewport, above the screening UI.
                modal.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                _modalHost!.AddChild(modal);
            }
            else
            {
                _modalRoot?.AddChild(modal);

                // Center the modal
                CenterModal(modal);
            }

            // Fire modal opened event
            ModalOpened?.Invoke();
        }

        /// <summary>
        /// Force-close an open evidence modal, treating it as an aborted decryption
        /// (evidence opportunity lost). Used when the CRT is dismissed mid-game.
        /// </summary>
        public void AbortEvidenceModal()
        {
            if (_currentModal is EvidenceModal modal)
            {
                modal.Abort();
            }
        }

        public override void _Input(InputEvent @event)
        {
            // Keyboard input is routed through the main viewport: controls hosted
            // inside the CRT SubViewport do not receive window key events directly.
            if (@event is InputEventKey key &&
                _currentModal is EvidenceModal modal &&
                modal.IsCrtHosted())
            {
                if (modal.HandleKey(key))
                {
                    GetViewport().SetInputAsHandled();
                }
            }
        }

        private void OnModalClosed()
        {
            GD.Print("ModalManager: OnModalClosed called");
            if (_currentModal != null)
            {
                GD.Print($"ModalManager: Closing modal {_currentModal.Name}");
                _currentModal.QueueFree();
                _currentModal = null;
                GD.Print("ModalManager: Modal closed successfully");
            }
            else
            {
                GD.PrintErr("ModalManager: OnModalClosed called but _currentModal is null");
            }

            // Fire modal closed event
            ModalClosed?.Invoke();
        }

        private void CenterModal(Control modal)
        {
            if (_modalRoot == null || modal == null)
                return;

            var viewportSize = GetViewport().GetVisibleRect().Size;
            modal.Position = (viewportSize - modal.Size) / 2;
        }

        private void OnViewportSizeChanged()
        {
            // CanvasLayer handles viewport sizing automatically
            // Just re-center any open modal (hosted modals use layout anchors)
            if (_currentModal != null && _currentModal.GetParent() == _modalRoot)
            {
                CenterModal(_currentModal);
            }
        }

        public override void _ExitTree()
        {
            GetViewport().SizeChanged -= OnViewportSizeChanged;
            _instance = null;
        }
    }
}
