using System;
using Godot;
using KBTV.Audio;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Screening;
using KBTV.UI.Themes;

namespace KBTV.UI.Components
{
    /// <summary>
    /// UI component that displays a single screenable property with reveal animation.
    /// Shows placeholder blocks for hidden properties and typewriter reveal effect.
    /// </summary>
    public partial class ScreenablePropertyRow : HBoxContainer
    {
        // Placeholder text for unrevealed values
        private const string PlaceholderText = "...";

        // Child nodes
        private Label _nameLabel = null!;
        private Label _valueLabel = null!;

        // State
        private ScreenableProperty? _property;
        private RevelationState _lastState = RevelationState.Hidden;

        // Audio service for reveal sound
        private IUIAudioService? _audioService;

        public override void _Ready()
        {
            // Create child nodes programmatically
            _nameLabel = new Label
            {
                SizeFlagsHorizontal = SizeFlags.Fill,
                CustomMinimumSize = new Vector2(120, 0)
            };
            AddChild(_nameLabel);

            _valueLabel = new Label
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.Word
            };
            AddChild(_valueLabel);

            // Create a monospace system font for console-like appearance
            var monoFont = new SystemFont();
            monoFont.FontNames = new string[] { "Consolas", "Courier New", "Liberation Mono", "monospace" };

            // Apply to both labels for consistent terminal look
            _nameLabel.AddThemeFontOverride("font", monoFont);
            _valueLabel.AddThemeFontOverride("font", monoFont);

            // Try to get audio service for reveal sounds
            try
            {
                _audioService = DependencyInjection.Get<IUIAudioService>(this);
            }
            catch
            {
                // Audio service not available - sounds will be silently skipped
            }

            // Apply initial styling
            UpdateValueLabelColor();
        }

        /// <summary>
        /// Set the property this row displays.
        /// </summary>
        public void SetProperty(ScreenableProperty property)
        {
            if (_nameLabel == null || _valueLabel == null)
            {
                GD.PrintErr("ScreenablePropertyRow.SetProperty called before _Ready() - nodes not initialized");
                return;
            }

            _property = property;
            _lastState = property.State;

            // Set the property name (never hidden)
            _nameLabel.Text = $"{property.DisplayName}:";

            // Initial value display based on state
            UpdateDisplay();
        }

        /// <summary>
        /// Update the animation each frame.
        /// Call this from the parent panel's _Process.
        /// </summary>
        public void UpdateAnimation(float delta)
        {
            if (_property == null) return;

            // Check for state transitions
            if (_property.State != _lastState)
            {
                OnStateChanged(_lastState, _property.State);
                _lastState = _property.State;
            }

            // Update display based on current state
            UpdateDisplay();
        }

        private void OnStateChanged(RevelationState oldState, RevelationState newState)
        {
            if (newState == RevelationState.Revealed)
            {
                // Play reveal sound with slight pitch variation for variety
                _audioService?.PlaySfx(UISfx.PropertyReveal, 0.1f);
            }

            UpdateValueLabelColor();
        }

        private void UpdateDisplay()
        {
            if (_property == null) return;

            switch (_property.State)
            {
                case RevelationState.Hidden:
                    // Show placeholder for unrevealed properties
                    _valueLabel.Text = PlaceholderText;
                    break;

                case RevelationState.Revealing:
                    // Typewriter effect: reveal chars based on progress
                    UpdateTypewriterDisplay();
                    break;

                case RevelationState.Revealed:
                    // Show full value
                    _valueLabel.Text = _property.DisplayValue;
                    break;
            }
        }

        private void UpdateTypewriterDisplay()
        {
            if (_property == null) return;

            float progress = _property.Progress;
            string actualValue = _property.DisplayValue;
            int totalLength = actualValue.Length;
            
            // Calculate how many actual characters to reveal
            int revealedChars = (int)(progress * totalLength);
            revealedChars = Math.Clamp(revealedChars, 0, totalLength);

            // Build the display: revealed portion + "..." if not complete
            string revealed = actualValue.Substring(0, revealedChars);
            
            if (revealedChars < totalLength)
            {
                _valueLabel.Text = revealed + PlaceholderText;
            }
            else
            {
                _valueLabel.Text = revealed;
            }
        }

        private void UpdateValueLabelColor()
        {
            if (_property == null) return;

            Color color = _property.State switch
            {
                RevelationState.Hidden => UIColors.Placeholder.Text,
                RevelationState.Revealing => UIColors.Placeholder.Revealing,
                RevelationState.Revealed => UIColors.TEXT_PRIMARY,
                _ => UIColors.TEXT_PRIMARY
            };

            _valueLabel.AddThemeColorOverride("font_color", color);
        }

        /// <summary>
        /// Get the current property being displayed.
        /// </summary>
        public ScreenableProperty? Property => _property;

        /// <summary>
        /// Check if this property has been fully revealed.
        /// </summary>
        public bool IsRevealed => _property?.IsRevealed ?? false;
    }
}
