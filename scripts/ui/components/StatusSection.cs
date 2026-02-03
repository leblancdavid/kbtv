#nullable enable

using Godot;
using KBTV.UI.Themes;

namespace KBTV.UI.Components
{
    /// <summary>
    /// Reusable component for status panel sections.
    /// Displays a header and a list of items with optional colors.
    /// Format:
    ///   SECTION TITLE
    ///   |- Item 1: value
    ///   |- Item 2: value
    /// </summary>
    public partial class StatusSection : VBoxContainer
    {
        private Label _headerLabel = null!;
        private VBoxContainer _itemsContainer = null!;
        private string _title = "";
        private bool _hideWhenEmpty = false;

        /// <summary>
        /// Create a new status section with the specified title.
        /// </summary>
        public StatusSection(string title, bool hideWhenEmpty = false)
        {
            _title = title;
            _hideWhenEmpty = hideWhenEmpty;
        }

        // Parameterless constructor for Godot
        public StatusSection() : this("STATUS") { }

        public override void _Ready()
        {
            // Set up container spacing
            AddThemeConstantOverride("separation", 4);

            // Create header label
            _headerLabel = new Label
            {
                Text = _title,
                HorizontalAlignment = HorizontalAlignment.Left,
                CustomMinimumSize = new Vector2(0, 18)
            };
            _headerLabel.AddThemeColorOverride("font_color", UIColors.Status.SectionHeader);

            // Apply monospace font
            var monoFont = new SystemFont();
            monoFont.FontNames = new string[] { "Consolas", "Courier New", "Liberation Mono", "monospace" };
            _headerLabel.AddThemeFontOverride("font", monoFont);
            _headerLabel.AddThemeFontSizeOverride("font_size", 13);

            AddChild(_headerLabel);

            // Create items container
            _itemsContainer = new VBoxContainer();
            _itemsContainer.AddThemeConstantOverride("separation", 2);
            AddChild(_itemsContainer);

            UpdateVisibility();
        }

        /// <summary>
        /// Set the section title.
        /// </summary>
        public void SetTitle(string title)
        {
            _title = title;
            if (_headerLabel != null)
            {
                _headerLabel.Text = title;
            }
        }

        /// <summary>
        /// Clear all items from the section.
        /// </summary>
        public void ClearItems()
        {
            if (_itemsContainer == null) return;

            foreach (var child in _itemsContainer.GetChildren())
            {
                child.QueueFree();
            }

            UpdateVisibility();
        }

        /// <summary>
        /// Add an item to the section with optional color.
        /// </summary>
        public void AddItem(string text, Color? color = null)
        {
            if (_itemsContainer == null) return;

            var itemLabel = new Label
            {
                Text = $"  {text}",
                HorizontalAlignment = HorizontalAlignment.Left
            };
            itemLabel.AddThemeColorOverride("font_color", color ?? UIColors.Status.ItemText);

            // Apply monospace font
            var monoFont = new SystemFont();
            monoFont.FontNames = new string[] { "Consolas", "Courier New", "Liberation Mono", "monospace" };
            itemLabel.AddThemeFontOverride("font", monoFont);
            itemLabel.AddThemeFontSizeOverride("font_size", 12);

            _itemsContainer.AddChild(itemLabel);
            UpdateVisibility();
        }

        /// <summary>
        /// Add an item with a tree-style prefix (for nested display).
        /// </summary>
        public void AddTreeItem(string text, Color? color = null, bool isLast = false)
        {
            string prefix = isLast ? "  \u2514\u2500 " : "  \u251c\u2500 "; // └─ or ├─
            AddItem($"{prefix}{text}", color);
        }

        /// <summary>
        /// Get the number of items in this section.
        /// </summary>
        public int ItemCount => _itemsContainer?.GetChildCount() ?? 0;

        private void UpdateVisibility()
        {
            if (_hideWhenEmpty)
            {
                Visible = ItemCount > 0;
            }
        }

        /// <summary>
        /// Set whether to hide the section when empty.
        /// </summary>
        public void SetHideWhenEmpty(bool hide)
        {
            _hideWhenEmpty = hide;
            UpdateVisibility();
        }
    }
}
