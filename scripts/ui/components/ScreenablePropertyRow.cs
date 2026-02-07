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
    /// Shows placeholder for hidden properties and typewriter reveal effect.
    /// Format: "PROPERTY_NAME: value" with name in cyan and value in white.
    /// </summary>
    public partial class ScreenablePropertyRow : HBoxContainer
    {
        // Placeholder text for unrevealed values
        private const string PlaceholderText = "...";

        // Child node
        private RichTextLabel _contentLabel = null!;

        // State
        private ScreenableProperty? _property;
        private RevelationState _lastState = RevelationState.Hidden;

        // Cached full text for typewriter effect
        private string _fullText = "";
        private string _nameText = "";  // e.g., "SUMMARY: "
        private string _valueText = ""; // e.g., "Some value here"

        // Audio service for reveal sound
        private IUIAudioService? _audioService;

        public override void _Ready()
        {
            // Create RichTextLabel for multi-color text support
            _contentLabel = new RichTextLabel
            {
                BbcodeEnabled = true,
                FitContent = true,
                ScrollActive = false,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.Word
            };
            AddChild(_contentLabel);

            // Create a monospace system font for console-like appearance
            var monoFont = new SystemFont();
            monoFont.FontNames = new string[] { "Consolas", "Courier New", "Liberation Mono", "monospace" };

            // Apply font
            _contentLabel.AddThemeFontOverride("normal_font", monoFont);

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
            UpdateDisplay();
        }

        /// <summary>
        /// Set the property this row displays.
        /// </summary>
        public void SetProperty(ScreenableProperty property)
        {
            if (_contentLabel == null)
            {
                Log.Error("ScreenablePropertyRow.SetProperty called before _Ready() - nodes not initialized");
                return;
            }

            _property = property;
            _lastState = property.State;

            // Cache the full text components
            _nameText = $"{property.DisplayName.ToUpper()}: ";
            _valueText = property.DisplayValue;
            _fullText = _nameText + _valueText;

            // Initial display based on state
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
        }

        private void UpdateDisplay()
        {
            if (_property == null || _contentLabel == null) return;

            switch (_property.State)
            {
                case RevelationState.Hidden:
                    // Show placeholder for unrevealed properties
                    _contentLabel.Text = PlaceholderText;
                    SetPlaceholderColor(UIColors.Placeholder.Text);
                    break;

                case RevelationState.Revealing:
                    // Typewriter effect: reveal chars based on progress
                    UpdateTypewriterDisplay();
                    break;

                case RevelationState.Revealed:
                    // Show full value with colors
                    _contentLabel.Text = FormatFullText();
                    break;
            }
        }

        private void UpdateTypewriterDisplay()
        {
            if (_property == null) return;

            float progress = _property.Progress;
            int totalLength = _fullText.Length;
            
            // Calculate how many characters to reveal
            int revealedChars = (int)(progress * totalLength);
            revealedChars = Math.Clamp(revealedChars, 0, totalLength);

            if (revealedChars == 0)
            {
                // Nothing revealed yet, show placeholder
                _contentLabel.Text = PlaceholderText;
                SetPlaceholderColor(UIColors.Placeholder.Revealing);
                return;
            }

            // Build the display with partial reveal
            string revealed = _fullText.Substring(0, revealedChars);
            
            // Determine how much of the name vs value is revealed
            int nameLength = _nameText.Length;
            
            string bbcode;
            if (revealedChars <= nameLength)
            {
                // Still revealing the name portion
                string partialName = revealed;
                bbcode = FormatName(partialName) + FormatPlaceholder(PlaceholderText);
            }
            else
            {
                // Name fully revealed, now revealing value
                string partialValue = revealed.Substring(nameLength);
                bool valueComplete = revealedChars >= totalLength;
                
                if (valueComplete)
                {
                    bbcode = FormatName(_nameText) + FormatValue(partialValue);
                }
                else
                {
                    bbcode = FormatName(_nameText) + FormatValue(partialValue) + FormatPlaceholder(PlaceholderText);
                }
            }

            _contentLabel.Text = bbcode;
        }

        /// <summary>
        /// Format the full revealed text with BBCode colors.
        /// </summary>
        private string FormatFullText()
        {
            return FormatName(_nameText) + FormatValue(_valueText);
        }

        /// <summary>
        /// Format name portion with cyan color.
        /// </summary>
        private string FormatName(string text)
        {
            string colorHex = UIColors.Property.Name.ToHtml(false);
            return $"[color=#{colorHex}]{EscapeBBCode(text)}[/color]";
        }

        /// <summary>
        /// Format value portion with primary text color.
        /// </summary>
        private string FormatValue(string text)
        {
            string colorHex = UIColors.Property.Value.ToHtml(false);
            return $"[color=#{colorHex}]{EscapeBBCode(text)}[/color]";
        }

        /// <summary>
        /// Format placeholder text with muted color.
        /// </summary>
        private string FormatPlaceholder(string text)
        {
            string colorHex = UIColors.Placeholder.Revealing.ToHtml(false);
            return $"[color=#{colorHex}]{text}[/color]";
        }

        /// <summary>
        /// Set the content label to show plain text in placeholder color.
        /// Used for hidden state where we don't need BBCode.
        /// </summary>
        private void SetPlaceholderColor(Color color)
        {
            _contentLabel.AddThemeColorOverride("default_color", color);
        }

        /// <summary>
        /// Escape special BBCode characters in text.
        /// </summary>
        private string EscapeBBCode(string text)
        {
            return text.Replace("[", "[lb]").Replace("]", "[rb]");
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
