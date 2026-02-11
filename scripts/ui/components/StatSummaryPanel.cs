using System.Collections.Generic;
using System.Linq;
using Godot;
using KBTV.Callers;
using KBTV.Data;
using KBTV.Screening;
using KBTV.UI.Themes;
using KBTV.Core;

namespace KBTV.UI.Components
{
    /// <summary>
    /// UI component that displays an aggregated summary of all stat effects from revealed properties.
    /// Shows total predicted impact on Vern if this caller goes on-air.
    /// Updates dynamically as more properties are revealed during screening.
    /// </summary>
    public partial class StatSummaryPanel : PanelContainer, IDependent
    {
        public override void _Notification(int what) => this.Notify(what);

        // Child nodes
        private Label _titleLabel = null!;
        private HBoxContainer _statsContainer = null!;
        private Label _noDataLabel = null!;
        private Button _evidenceFoundButton = null!;
        private HBoxContainer _evidenceContainer = null!;
        
        // State
        private ScreenableProperty[]? _properties;
        private Dictionary<StatType, float> _lastDisplayedTotals = new();
        private IScreeningController? _screeningController;
        private bool _evidenceAvailable;
        private float _flashTimer;

        public override void _Ready()
        {
            // Set up panel styling
            var panelStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.1f, 0.1f, 0.12f, 0.9f),
                BorderColor = new Color(0.3f, 0.5f, 0.3f, 0.8f),
                BorderWidthLeft = 1,
                BorderWidthRight = 1,
                BorderWidthTop = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 4,
                CornerRadiusTopRight = 4,
                CornerRadiusBottomLeft = 4,
                CornerRadiusBottomRight = 4,
                ContentMarginLeft = 8,
                ContentMarginRight = 8,
                ContentMarginTop = 4,
                ContentMarginBottom = 4
            };
            AddThemeStyleboxOverride("panel", panelStyle);

            // Create inner VBox layout
            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 4);
            AddChild(vbox);

            // Title row
            _titleLabel = new Label
            {
                Text = "Predicted Impact on Vern:",
                HorizontalAlignment = HorizontalAlignment.Left
            };
            _titleLabel.AddThemeColorOverride("font_color", UIColors.TEXT_SECONDARY);
            _titleLabel.AddThemeFontSizeOverride("font_size", 12);
            vbox.AddChild(_titleLabel);

            // Stats container (horizontal)
            _statsContainer = new HBoxContainer();
            _statsContainer.AddThemeConstantOverride("separation", 12);
            vbox.AddChild(_statsContainer);

            // Evidence Found button container (right-aligned)
            var buttonContainer = new HBoxContainer();
            buttonContainer.AddThemeConstantOverride("separation", 8);
            vbox.AddChild(buttonContainer);

            // Spacer to push button to right
            var spacer = new Control();
            spacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            buttonContainer.AddChild(spacer);

            // Evidence container (label + button)
            _evidenceContainer = new HBoxContainer();
            _evidenceContainer.AddThemeConstantOverride("separation", 8); // standard spacing
            _evidenceContainer.Visible = false;
            buttonContainer.AddChild(_evidenceContainer);

            // Evidence Found label
            var evidenceLabel = new Label
            {
                Text = "Evidence Found: "
            };
            evidenceLabel.AddThemeColorOverride("font_color", UIColors.TEXT_SECONDARY);
            evidenceLabel.AddThemeFontSizeOverride("font_size", 16);
            _evidenceContainer.AddChild(evidenceLabel);

            // Evidence Found button
            _evidenceFoundButton = new Button
            {
                Text = "Examine",
                CustomMinimumSize = new Vector2(80, 32)
            };
            _evidenceFoundButton.AddThemeFontSizeOverride("font_size", 16);
            _evidenceFoundButton.Pressed += OnEvidenceFoundPressed;
            _evidenceContainer.AddChild(_evidenceFoundButton);

            // No data label (shown when nothing is revealed)
            _noDataLabel = new Label
            {
                Text = "Screening...",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _noDataLabel.AddThemeColorOverride("font_color", UIColors.Placeholder.Text);
            _statsContainer.AddChild(_noDataLabel);

            // Get screening controller for evidence button functionality
            _screeningController = DependencyInjection.Get<IScreeningController>(this);
            if (_screeningController != null)
            {
                _screeningController.EvidenceCollected += OnEvidenceCollected;
                _screeningController.ProgressUpdated += OnProgressUpdated;
            }

            // Initialize evidence button state
            _evidenceAvailable = false;
            _flashTimer = 0f;
            SetProcess(true);
        }

        public void OnResolved()
        {
            // Initialize dependencies when ready
        }

        public override void _Process(double delta)
        {
            // Handle flashing animation when evidence is available
            if (_evidenceAvailable && _evidenceFoundButton != null && IsInstanceValid(_evidenceFoundButton) && _evidenceFoundButton.Disabled == false)
            {
                _flashTimer += (float)delta;
                UpdateFlashAnimation();
            }
        }

        /// <summary>
        /// Set the properties to aggregate stats from.
        /// </summary>
        public void SetProperties(ScreenableProperty[]? properties)
        {
            _properties = properties;
            _lastDisplayedTotals.Clear();
            UpdateDisplay();
        }

        /// <summary>
        /// Update the display each frame. Call from parent's _Process.
        /// </summary>
        public void UpdateDisplay()
        {
            if (_properties == null || _properties.Length == 0)
            {
                ShowNoData("No caller data");
                return;
            }

            // Calculate totals from all revealed properties
            var totals = CalculateRevealedStatTotals();

            // Check if anything changed
            if (TotalsMatch(totals, _lastDisplayedTotals))
            {
                return; // No update needed
            }

            // Update display
            _lastDisplayedTotals = new Dictionary<StatType, float>(totals);
            RebuildStatLabels(totals);
        }

        /// <summary>
        /// Update evidence button visibility and styling based on evidence availability.
        /// </summary>
        public void UpdateEvidenceButton(bool evidenceAvailable)
        {
            if (_evidenceContainer == null || !GodotObject.IsInstanceValid(_evidenceContainer))
                return;

            _evidenceAvailable = evidenceAvailable;
            _evidenceContainer.Visible = evidenceAvailable;
            if (evidenceAvailable)
            {
                _evidenceFoundButton.Disabled = false;
                StyleEnabledButton();
            }
            else
            {
                _evidenceFoundButton.Disabled = true;
                StyleDisabledButton();
            }
        }

        /// <summary>
        /// Handle evidence found button press.
        /// </summary>
        private void OnEvidenceFoundPressed()
        {
            if (_evidenceFoundButton == null || !GodotObject.IsInstanceValid(_evidenceFoundButton))
                return;
                
            if (!IsInstanceValid(this))
                return;
                
            if (_evidenceAvailable && _screeningController != null)
            {
                // Open evidence collection modal with current caller
                var modalManager = DependencyInjection.Get<ModalManager>(this);
                var currentCaller = _screeningController.CurrentCaller;
                modalManager?.ShowEvidenceModal(currentCaller);
            }
        }

        /// <summary>
        /// Handle evidence collection event.
        /// </summary>
        private void OnEvidenceCollected(Caller caller)
        {
            // Check if still in scene tree
            if (!GodotObject.IsInstanceValid(this))
                return;
                
            // Evidence has been collected, hide button
            UpdateEvidenceButton(false);
        }

        /// <summary>
        /// Handle screening progress updates.
        /// </summary>
        private void OnProgressUpdated(ScreeningProgress progress)
        {
            // Check if still in scene tree
            if (!GodotObject.IsInstanceValid(this))
                return;
                
            // Check if evidence availability has changed
            bool evidenceNowAvailable = _screeningController?.IsEvidenceAvailable ?? false;
            UpdateEvidenceButton(evidenceNowAvailable);
        }

        /// <summary>
        /// Apply enabled styling to evidence button (green flashing).
        /// </summary>
        private void StyleEnabledButton()
        {
            if (_evidenceFoundButton == null)
                return;

            // Use green accent color for enabled state
            var style = new StyleBoxFlat
            {
                BgColor = UIColors.Accent.Green,
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
                ContentMarginLeft = 20,
                ContentMarginRight = 20,
                ContentMarginTop = 12,
                ContentMarginBottom = 12
            };
            _evidenceFoundButton.AddThemeStyleboxOverride("normal", style);
            _evidenceFoundButton.AddThemeStyleboxOverride("hover", style);
            _evidenceFoundButton.AddThemeColorOverride("font_color", UIColors.TEXT_PRIMARY);
            _evidenceFoundButton.QueueRedraw();
        }

        /// <summary>
        /// Apply disabled styling to evidence button (gray).
        /// </summary>
        private void StyleDisabledButton()
        {
            if (_evidenceFoundButton == null)
                return;

            // Use disabled background color
            var style = new StyleBoxFlat
            {
                BgColor = UIColors.BG_DISABLED,
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
                ContentMarginLeft = 20,
                ContentMarginRight = 20,
                ContentMarginTop = 12,
                ContentMarginBottom = 12
            };
            _evidenceFoundButton.AddThemeStyleboxOverride("normal", style);
            _evidenceFoundButton.AddThemeStyleboxOverride("hover", style);
            _evidenceFoundButton.AddThemeColorOverride("font_color", UIColors.TEXT_DISABLED);
            _evidenceFoundButton.QueueRedraw();
        }

        /// <summary>
        /// Update flashing animation for evidence button.
        /// </summary>
        private void UpdateFlashAnimation()
        {
            if (_evidenceFoundButton == null || !IsInstanceValid(_evidenceFoundButton))
                return;

            // Flash every 0.5 seconds between green and default disabled color
            bool isFlashPhase = Mathf.Floor(_flashTimer * 2) % 2 == 0;
            Color bgColor = isFlashPhase ? UIColors.Accent.Green : UIColors.BG_DISABLED;

            var style = new StyleBoxFlat
            {
                BgColor = bgColor,
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
                ContentMarginLeft = 20,
                ContentMarginRight = 20,
                ContentMarginTop = 12,
                ContentMarginBottom = 12
            };
            _evidenceFoundButton.AddThemeStyleboxOverride("normal", style);
            _evidenceFoundButton.AddThemeStyleboxOverride("hover", style);
            _evidenceFoundButton.QueueRedraw();
        }

        /// <summary>
        /// Calculate total stat effects from all revealed properties.
        /// </summary>
        private Dictionary<StatType, float> CalculateRevealedStatTotals()
        {
            var totals = new Dictionary<StatType, float>();

            foreach (var property in _properties!)
            {
                if (!property.IsRevealed) continue;

                foreach (var effect in property.StatEffects)
                {
                    if (!totals.ContainsKey(effect.StatType))
                    {
                        totals[effect.StatType] = 0f;
                    }
                    totals[effect.StatType] += effect.Amount;
                }
            }

            return totals;
        }

        /// <summary>
        /// Check if two totals dictionaries are equivalent.
        /// </summary>
        private bool TotalsMatch(Dictionary<StatType, float> a, Dictionary<StatType, float> b)
        {
            if (a.Count != b.Count) return false;

            foreach (var (key, value) in a)
            {
                if (!b.TryGetValue(key, out var bValue) || !Mathf.IsEqualApprox(value, bValue))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Rebuild the stat labels to show current totals.
        /// </summary>
        private void RebuildStatLabels(Dictionary<StatType, float> totals)
        {
            // Clear existing labels
            foreach (var child in _statsContainer.GetChildren())
            {
                child.QueueFree();
            }

            if (totals.Count == 0)
            {
                ShowNoData("Neutral impact");
                return;
            }

            // Sort by stat type for consistent display
            var sortedStats = totals.OrderBy(kv => kv.Key).ToList();

            foreach (var (statType, amount) in sortedStats)
            {
                // Skip zero effects
                if (Mathf.IsZeroApprox(amount)) continue;

                var label = CreateStatLabel(statType, amount);
                _statsContainer.AddChild(label);
            }

            // Add XP impact prediction
            var xpImpact = CalculateXPImpact();
            if (!Mathf.IsZeroApprox(xpImpact))
            {
                var xpLabel = CreateXPLabel(xpImpact);
                _statsContainer.AddChild(xpLabel);
            }

            // If all effects are zero, show neutral message
            if (_statsContainer.GetChildCount() == 0)
            {
                ShowNoData("Neutral impact");
            }
        }

        /// <summary>
        /// Create a label for a single stat effect.
        /// </summary>
        private Label CreateStatLabel(StatType statType, float amount)
        {
            var fullName = GetStatFullName(statType);

            // Format: "Patience +5" or "Spirit -3"
            var signText = amount >= 0 ? "+" : "";
            var text = $"{fullName}: {signText}{amount:F0}";

            var label = new Label { Text = text };

            // Color based on positive/negative
            var color = amount >= 0 ? UIColors.StatEffect.Positive : UIColors.StatEffect.Negative;
            label.AddThemeColorOverride("font_color", color);

            // Tooltip with full stat name
            label.TooltipText = $"{fullName}: {signText}{amount:F1}";

            return label;
        }

        /// <summary>
        /// Show a no-data message.
        /// </summary>
        private void ShowNoData(string message)
        {
            foreach (var child in _statsContainer.GetChildren())
            {
                child.QueueFree();
            }

            _noDataLabel = new Label
            {
                Text = message,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _noDataLabel.AddThemeColorOverride("font_color", UIColors.Placeholder.Text);
            _statsContainer.AddChild(_noDataLabel);
        }

        /// <summary>
        /// Get full display name for a stat type.
        /// </summary>
        private static string GetStatFullName(StatType statType)
        {
            return statType switch
            {
                StatType.Physical => "Physical",
                StatType.Emotional => "Emotional",
                StatType.Mental => "Mental",
                StatType.Caffeine => "Caffeine",
                StatType.Nicotine => "Nicotine",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Calculate XP impact from revealed properties.
        /// </summary>
        private float CalculateXPImpact()
        {
            if (_properties == null) return 0f;

            // Sum up all stat effects as XP impact
            return _properties
                .Where(p => p.IsRevealed)
                .SelectMany(p => p.StatEffects)
                .Sum(e => e.Amount);
        }

        /// <summary>
        /// Create a label for XP impact.
        /// </summary>
        private Label CreateXPLabel(float xpImpact)
        {
            var signText = xpImpact >= 0 ? "+" : "";
            var text = $"XP {signText}{xpImpact:F0}";

            var label = new Label { Text = text };
            var color = xpImpact >= 0 ? UIColors.StatEffect.Positive : UIColors.StatEffect.Negative;
            label.AddThemeColorOverride("font_color", color);
            label.TooltipText = $"Topic Belief: {signText}{xpImpact:F1}";

            return label;
        }

        public override void _ExitTree()
        {
            if (_screeningController != null)
            {
                _screeningController.EvidenceCollected -= OnEvidenceCollected;
                _screeningController.ProgressUpdated -= OnProgressUpdated;
            }

            if (_evidenceFoundButton != null)
            {
                _evidenceFoundButton.Pressed -= OnEvidenceFoundPressed;
            }
        }
    }
}
