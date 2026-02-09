using System;
using Godot;
using KBTV.Core;

namespace KBTV.UI
{
    /// <summary>
    /// Singleton manager for modal dialogs.
    /// Handles modal lifecycle, layering, and provides centralized modal access.
    /// </summary>
    public partial class ModalManager : Node
    {
        private static ModalManager? _instance;
        public static ModalManager Instance => _instance ?? throw new InvalidOperationException("ModalManager not initialized");

        private Control? _modalRoot;
        private Control? _currentModal;

        public override void _Ready()
        {
            _instance = this;

            // Create modal root container
            _modalRoot = new Control
            {
                Name = "ModalRoot",
                Size = GetViewport().GetVisibleRect().Size,
                MouseFilter = Control.MouseFilterEnum.Stop
            };
            AddChild(_modalRoot);

            // Connect to viewport size changes
            GetViewport().SizeChanged += OnViewportSizeChanged;
        }

        public void ShowEvidenceModal()
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
            modal.ModalClosed += OnModalClosed;
            _currentModal = modal;
            _modalRoot?.AddChild(modal);

            // Center the modal
            CenterModal(modal);
        }

        private void OnModalClosed()
        {
            if (_currentModal != null)
            {
                _currentModal.QueueFree();
                _currentModal = null;
            }
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
            if (_modalRoot != null)
            {
                _modalRoot.Size = GetViewport().GetVisibleRect().Size;
            }

            if (_currentModal != null)
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