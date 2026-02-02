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
    /// Shows placeholder blocks for hidden properties, typewriter reveal effect,
    /// and colored stat effect indicators when fully revealed.
    /// </summary>
    public partial class ScreenablePropertyRow : HBoxContainer
    {
        // Placeholder character for unrevealed text (block character for "censored" feel)
        private const char PlaceholderChar = '█';
        private const int PlaceholderLength = 10;

        // Child nodes
        private Label _nameLabel = null!;
        private Label _valueLabel = null!;
        private HBoxContainer _statEffectsContainer = null!;

        // State
        private ScreenableProperty? _property;
        private RevelationState _lastState = RevelationState.Hidden;
        private bool _statEffectsBuilt = false;

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
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            AddChild(_valueLabel);

            _statEffectsContainer = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd
            };
            _statEffectsContainer.AddThemeConstantOverride("separation", 4);
            AddChild(_statEffectsContainer);

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
            _statEffectsBuilt = false;

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
            if (newState == RevelationState.Revealed && !_statEffectsBuilt)
            {
                // Play reveal sound with slight pitch variation for variety
                _audioService?.PlaySfx(UISfx.PropertyReveal, 0.1f);

                BuildStatEffectIndicators();
                _statEffectsBuilt = true;
            }

            UpdateValueLabelColor();
        }

        private void UpdateDisplay()
        {
            if (_property == null) return;

            switch (_property.State)
            {
                case RevelationState.Hidden:
                    // Show fixed-length placeholder blocks
                    _valueLabel.Text = new string(PlaceholderChar, PlaceholderLength);
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

            // Build the display: revealed portion + placeholder remainder
            string revealed = actualValue.Substring(0, revealedChars);
            int placeholderCount = PlaceholderLength - revealedChars;
            
            if (placeholderCount > 0)
            {
                string placeholder = new string(PlaceholderChar, placeholderCount);
                _valueLabel.Text = revealed + placeholder;
            }
            else
            {
                // All characters revealed (or value is longer than placeholder length)
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

        private void BuildStatEffectIndicators()
        {
            if (_property == null) return;

            // Clear existing indicators
            foreach (var child in _statEffectsContainer.GetChildren())
            {
                child.QueueFree();
            }

            // Get stat effect displays from the property
            var displays = _property.GetStatEffectDisplays();

            foreach (var display in displays)
            {
                var label = new Label
                {
                    Text = display.Text
                };

                // Apply color based on positive/negative
                Color effectColor = display.IsPositive 
                    ? UIColors.StatEffect.Positive 
                    : UIColors.StatEffect.Negative;
                label.AddThemeColorOverride("font_color", effectColor);

                _statEffectsContainer.AddChild(label);
            }
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
