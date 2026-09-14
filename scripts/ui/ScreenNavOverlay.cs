#nullable enable

using System;
using Godot;

namespace KBTV.UI
{
    /// <summary>
    /// Global screen navigation overlay shown while a diegetic view (terminal or
    /// soundboard) is open. Renders a `<-  Back` button top-left and an `X` close
    /// button top-right above the projected screen, styled with UITheme.
    ///
    /// Owns no view logic: emits <see cref="BackRequested"/> / <see cref="CloseRequested"/>
    /// and lets the owner (World3D) decide what each does based on the current view state.
    /// </summary>
    public partial class ScreenNavOverlay : CanvasLayer
    {
        private const int LayerConstant = 121;

        private const string BackText = "<-";
        private const string CloseText = "X";

        private Button _backButton = null!;
        private Button _closeButton = null!;

        private string _backLabel = "";

        public event Action? BackRequested;
        public event Action? CloseRequested;

        public ScreenNavOverlay()
        {
            Layer = LayerConstant;
        }

        public override void _Ready()
        {
            BuildUi();
            Visible = false;
        }

        private void BuildUi()
        {
            var root = new Control { Name = "ScreenNavRoot" };
            root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            root.MouseFilter = Control.MouseFilterEnum.Ignore;
            AddChild(root);

            _backButton = new Button
            {
                Name = "BackButton",
                Text = BackText,
                MouseFilter = Control.MouseFilterEnum.Stop,
                Position = new Vector2(UITheme.MARGIN_SMALL, UITheme.MARGIN_SMALL),
                CustomMinimumSize = new Vector2(54, UITheme.BUTTON_HEIGHT)
            };
            UITheme.ApplyButtonStyle(_backButton);
            _backButton.Pressed += OnBackPressed;
            root.AddChild(_backButton);

            _closeButton = new Button
            {
                Name = "CloseButton",
                Text = CloseText,
                MouseFilter = Control.MouseFilterEnum.Stop,
                CustomMinimumSize = new Vector2(28, UITheme.BUTTON_HEIGHT)
            };
            UITheme.ApplyButtonStyle(_closeButton);
            _closeButton.Pressed += OnClosePressed;
            root.AddChild(_closeButton);
        }

        public override void _Process(double delta)
        {
            if (GetViewport() == null)
            {
                return;
            }

            var size = GetViewport().GetVisibleRect().Size;
            _closeButton.Position = new Vector2(size.X - _closeButton.Size.X - UITheme.MARGIN_SMALL, UITheme.MARGIN_SMALL);
        }

        /// <summary>
        /// Show the overlay with the given back-button label and close-button visibility.
        /// </summary>
        public void ShowOverlay(string backLabel, bool showClose)
        {
            _backLabel = backLabel;
            _backButton.Text = string.IsNullOrEmpty(backLabel) ? BackText : $"{BackText} {backLabel}";
            _backButton.Visible = !string.IsNullOrEmpty(backLabel);
            _closeButton.Visible = showClose;
            Visible = true;
        }

        public void HideOverlay()
        {
            Visible = false;
        }

        private void OnBackPressed()
        {
            BackRequested?.Invoke();
        }

        private void OnClosePressed()
        {
            CloseRequested?.Invoke();
        }
    }
}