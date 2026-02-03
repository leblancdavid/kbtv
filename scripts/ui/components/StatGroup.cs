#nullable enable

using Godot;
using KBTV.UI.Themes;

namespace KBTV.UI.Components
{
    /// <summary>
    /// Category container for grouping related stats with a header separator.
    /// Format: "─── CATEGORY NAME ────────────────────────────────"
    /// Contains child StatBar/SpiritBar components.
    /// </summary>
    public partial class StatGroup : VBoxContainer
    {
        private const int HEADER_LINE_LENGTH = 50;

        private Label _headerLabel = null!;
        private VBoxContainer _statsContainer = null!;
        private string _categoryName = "";

        /// <summary>
        /// Create a new stat group with the specified category name.
        /// </summary>
        public StatGroup(string categoryName)
        {
            _categoryName = categoryName;
        }

        // Parameterless constructor for Godot
        public StatGroup() : this("CATEGORY") { }

        public override void _Ready()
        {
            // Set up container spacing
            AddThemeConstantOverride("separation", 8);

            // Create header label with separator styling
            _headerLabel = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                CustomMinimumSize = new Vector2(0, 20)
            };
            _headerLabel.AddThemeColorOverride("font_color", UIColors.VernStat.CategoryHeader);
            AddChild(_headerLabel);

            // Apply monospace font for consistent dashes
            var monoFont = new SystemFont();
            monoFont.FontNames = new string[] { "Consolas", "Courier New", "Liberation Mono", "monospace" };
            _headerLabel.AddThemeFontOverride("font", monoFont);

            // Set header text
            UpdateHeaderText();

            // Create stats container
            _statsContainer = new VBoxContainer();
            _statsContainer.AddThemeConstantOverride("separation", 6);
            AddChild(_statsContainer);
        }

        private void UpdateHeaderText()
        {
            // Format: "─── CATEGORY NAME ────────────────────────────────"
            string prefix = "─── ";
            string suffix = " ";
            int remainingDashes = HEADER_LINE_LENGTH - prefix.Length - _categoryName.Length - suffix.Length;
            remainingDashes = Mathf.Max(remainingDashes, 3); // Minimum 3 dashes at end

            _headerLabel.Text = $"{prefix}{_categoryName}{suffix}{new string('─', remainingDashes)}";
        }

        /// <summary>
        /// Add a StatBar to this group.
        /// </summary>
        public void AddStatBar(StatBar statBar)
        {
            _statsContainer.AddChild(statBar);
        }

        /// <summary>
        /// Add a SpiritBar to this group.
        /// </summary>
        public void AddSpiritBar(SpiritBar spiritBar)
        {
            _statsContainer.AddChild(spiritBar);
        }

        /// <summary>
        /// Add a CenteredStatBar to this group.
        /// </summary>
        public void AddCenteredStatBar(CenteredStatBar centeredStatBar)
        {
            _statsContainer.AddChild(centeredStatBar);
        }

        /// <summary>
        /// Add any Control as a child stat display.
        /// </summary>
        public void AddStatDisplay(Control display)
        {
            _statsContainer.AddChild(display);
        }

        /// <summary>
        /// Get the stats container for direct child manipulation if needed.
        /// </summary>
        public VBoxContainer StatsContainer => _statsContainer;
    }
}
