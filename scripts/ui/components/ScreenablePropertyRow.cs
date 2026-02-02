using System;
using Godot;
using KBTV.Callers;
using KBTV.Screening;
using KBTV.UI.Themes;

namespace KBTV.UI.Components
{
    /// <summary>
    /// UI component that displays a single screenable property with reveal animation.
    /// Shows scrambled Matrix-style text for hidden properties, typewriter reveal effect,
    /// and colored stat effect indicators when fully revealed.
    /// </summary>
    public partial class ScreenablePropertyRow : HBoxContainer
    {
        // Characters used for Matrix-style scramble effect
        private static readonly string ScrambleChars = 
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*<>[]{}|";

        // Child nodes
        private Label _nameLabel = null!;
        private Label _valueLabel = null!;
        private HBoxContainer _statEffectsContainer = null!;

        // State
        private ScreenableProperty? _property;
        private Random _rng = new();
        private RevelationState _lastState = RevelationState.Hidden;
        private bool _statEffectsBuilt = false;

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

            // Apply initial styling
            UpdateValueLabelColor();
        }

        /// <summary>
        /// Set the property this row displays.
        /// </summary>
        public void SetProperty(ScreenableProperty property)
        {
            _property = property;
            _lastState = property.State;
            _statEffectsBuilt = false;

            // Set the property name (never scrambled)
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
                    // Show scrambled text (updates every frame for animation)
                    _valueLabel.Text = GenerateScrambledText(_property.DisplayValue.Length);
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
            int totalLength = _property.DisplayValue.Length;
            int revealedChars = (int)(progress * totalLength);

            // Clamp to valid range
            revealedChars = Math.Clamp(revealedChars, 0, totalLength);

            // Build the display: revealed portion + scrambled remainder
            string revealed = _property.DisplayValue.Substring(0, revealedChars);
            int remainingLength = totalLength - revealedChars;

            if (remainingLength > 0)
            {
                string scrambled = GenerateScrambledText(remainingLength);
                _valueLabel.Text = revealed + scrambled;
            }
            else
            {
                _valueLabel.Text = revealed;
            }
        }

        private string GenerateScrambledText(int length)
        {
            if (length <= 0) return "";

            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = ScrambleChars[_rng.Next(ScrambleChars.Length)];
            }
            return new string(chars);
        }

        private void UpdateValueLabelColor()
        {
            if (_property == null) return;

            Color color = _property.State switch
            {
                RevelationState.Hidden => UIColors.Scramble.Text,
                RevelationState.Revealing => UIColors.Scramble.Revealing,
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
