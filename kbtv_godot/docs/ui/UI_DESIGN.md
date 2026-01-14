# UI Design Document

This document describes the KBTV Godot UI architecture using **Control nodes**, a flexible node-based approach for building game UI.

## Table of Contents

1. [Overview](#overview)
2. [UI Toolkit Architecture](#ui-toolkit-architecture)
3. [USS Styling](#uss-styling)
4. [Panel Creation Patterns](#panel-creation-patterns)
5. [Standard Dimensions](#standard-dimensions)
6. [Common Patterns](#common-patterns)
7. [Updating Panels](#updating-panels)

---

## Overview

The KBTV UI system uses **Godot Control nodes** with programmatic styling:

- **StyleBoxFlat** - Theme-based styling with colors, borders, and backgrounds
- **Control Hierarchy** - Node-based UI structure with anchors and sizing
- **Theme Overrides** - Runtime styling modifications
- **UIManagerBootstrap & PreShowUIManager** - Phase-based UI managers

### Key Benefits Over Unity UI

- **Native Godot Integration** - Seamless integration with Godot's scene system
- **Flexible Anchoring** - Powerful anchor-based layouts
- **Theme System** - Consistent styling across all UI elements
- **Runtime Modification** - Easy programmatic UI changes

---

## Godot UI Architecture

### Folder Structure

```
kbtv_godot/
├── scripts/
│   └── ui/
│       ├── UIManagerBootstrap.cs      ← Live show UI manager
│       ├── PreShowUIManager.cs        ← Pre-show UI manager
│       ├── UITheme.cs                 ← Dark theme constants
│       ├── UIHelpers.cs               ← Static helper methods
│       └── controllers/
│           └── TabController.cs       ← Tab system controller
```

### UI Hierarchy

```
Canvas (Control, FullRect)
├── HeaderBar (HBoxContainer, TopWide, 28px height)
│   ├── LiveIndicator (Panel + Label)
│   ├── Clock (VBoxContainer)
│   │   ├── CurrentTime (Label)
│   │   └── RemainingTime (Label)
│   ├── Spacer (Control, ExpandFill)
│   └── ListenerDisplay (VBoxContainer)
│       ├── ListenerCount (Label)
│       └── ListenerChange (Label)
├── MainContent (Control, responsive positioning)
│   └── TabSection (VBoxContainer)
│       ├── TabHeader (HBoxContainer)
│       │   ├── CallersTab (Button)
│       │   ├── ItemsTab (Button)
│       │   └── StatsTab (Button)
│       └── TabContentArea (Control)
│           ├── CallersContent (HBoxContainer)
│           │   ├── ScreenerColumn (Control, 60%)
│           │   └── QueueColumn (Control, 40%)
│           ├── ItemsContent (Control, hidden)
│           └── StatsContent (Control, hidden)
└── Footer (HBoxContainer, BottomWide, 160px height)
    ├── OnAirPanel (Panel, 35% width)
    ├── TranscriptPanel (Panel, 45% width)
    └── AdBreakPanel (Panel, 20% width)
```

---

## Godot Styling

### Theme Constants

The main theme constants are defined in: `scripts/ui/UITheme.cs`

### Color Constants

```csharp
public static class UITheme
{
    // Dark Theme Colors
    public static readonly Color BG_DARK = new Color(0.1f, 0.1f, 0.1f);
    public static readonly Color BG_PANEL = new Color(0.15f, 0.15f, 0.15f);
    public static readonly Color BG_BORDER = new Color(0.2f, 0.2f, 0.2f);
    public static readonly Color TEXT_PRIMARY = new Color(0.9f, 0.9f, 0.9f);
    public static readonly Color TEXT_SECONDARY = new Color(0.7f, 0.7f, 0.7f);
    public static readonly Color ACCENT_GOLD = new Color(1f, 0.7f, 0f);
    public static readonly Color ACCENT_RED = new Color(0.8f, 0.2f, 0.2f);
    public static readonly Color ACCENT_GREEN = new Color(0.2f, 0.8f, 0.2f);

    // Dimensions (in pixels)
    public const float HEADER_HEIGHT = 28f;
    public const float TAB_HEIGHT = 24f;
    public const float FOOTER_HEIGHT = 160f;
    public const float BUTTON_HEIGHT = 28f;

    // Panel Widths (as fractions)
    public const float ONAIR_WIDTH = 0.35f;
    public const float TRANSCRIPT_WIDTH = 0.45f;
    public const float ADBREAK_WIDTH = 0.20f;
}
```

### Flexbox Layout Example

```css
GameUI {
    width: 100%;
    height: 100%;
    background-color: var(--bg-dark);
    flex-direction: column;
}

HeaderBar {
    height: var(--header-height);
    background-color: var(--bg-panel);
    flex-direction: row;
    align-items: center;
}

MainContent {
    flex-grow: 1;
    flex-direction: row;
}

Footer {
    height: var(--footer-height);
    flex-direction: row;
}

OnAirColumn {
    width: var(--onair-width);
    min-width: 150px;
    background-color: var(--bg-panel);
}
```

---

## Panel Creation Patterns

### Creating a New Panel

```csharp
using Godot;

namespace KBTV.UI
{
    public partial class MyPanel : Panel
    {
        public Label TitleText;
        public Control ContentContainer;

        public MyPanel()
        {
            // Set panel background
            UITheme.ApplyPanelStyle(this);

            // Create header
            TitleText = new Label("My Panel");
            TitleText.AddThemeColorOverride("font_color", UITheme.ACCENT_GOLD);
            TitleText.HorizontalAlignment = HorizontalAlignment.Center;
            AddChild(TitleText);

            // Create content container
            ContentContainer = new Control();
            ContentContainer.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(ContentContainer);
        }

        public void Initialize()
        {
            // Subscribe to events, load data
        }
    }
}
```

### Adding to UIManagerBootstrap

In `UIManagerBootstrap.CreateFullLayoutUI()`:

```csharp
private void CreateCustomPanel(Control parent)
{
    var myPanel = new MyPanel();
    myPanel.Name = "MyPanel";
    myPanel.SetAnchorsPreset(LayoutPreset.FullRect);
    parent.AddChild(myPanel);
    myPanel.Initialize();
}
```

### Panel Update Methods

For animated content, add update methods called from `GameUIManager.Update()`:

```csharp
public void UpdateBlink(float time)
{
    if (indicator.style.display.value == DisplayStyle.Flex)
    {
        float alpha = (Mathf.Sin(time * 4f) + 1f) / 2f;
        indicator.style.opacity = Mathf.Lerp(0.3f, 1f, alpha);
    }
}
```

---

## Standard Dimensions

### Heights

| Element | CSS Value | Usage |
|---------|-----------|-------|
| HeaderBar | 28px | Top navigation bar |
| Tab buttons | 24px | Tab navigation |
| Footer | 140px | Bottom panel area |
| Buttons | 28px | Action buttons |
| Section headers | 14-16px | Panel section titles |

### Widths

| Element | CSS Value | Usage |
|---------|-----------|-------|
| OnAirColumn | 35% | Left footer panel |
| TranscriptColumn | 45% | Center footer panel |
| AdBreakColumn | 20% | Right footer panel |
| ScreenerColumn | 60% | Left callers tab |
| QueueColumn | 40% | Right callers tab |

### Colors

| UITheme Constant | RGB | Usage |
|------------------|-----|-------|
| BG_DARK | (0.1, 0.1, 0.1) | Root background |
| BG_PANEL | (0.15, 0.15, 0.15) | Panel backgrounds |
| BG_BORDER | (0.2, 0.2, 0.2) | Dividers, borders |
| TEXT_PRIMARY | (0.9, 0.9, 0.9) | Main text |
| ACCENT_GOLD | (1.0, 0.7, 0.0) | Headers, accents |
| ACCENT_GREEN | (0.2, 0.8, 0.2) | Success, approve |
| ACCENT_RED | (0.8, 0.2, 0.2) | Error, on-air |

---

## Common Patterns

### Creating Buttons

```csharp
var button = new Button();
button.Text = "ACTION";
button.CustomMinimumSize = new Vector2(0, UITheme.BUTTON_HEIGHT);
UITheme.ApplyButtonStyle(button);
button.Pressed += OnButtonPressed;
container.AddChild(button);
```

### Creating Labels

```csharp
var label = new Label();
label.Text = "Text";
label.AddThemeColorOverride("font_color", UITheme.TEXT_PRIMARY);
label.HorizontalAlignment = HorizontalAlignment.Left;
container.AddChild(label);
```

### Toggling Visibility

```csharp
control.Visible = isVisible;
```

### Setting Size/Anchors

```csharp
// Fixed size
control.Size = new Vector2(200, 50);

// Responsive with anchors
control.SetAnchorsPreset(LayoutPreset.FullRect);

// Minimum size for readability
control.CustomMinimumSize = new Vector2(100, 30);
```

### Layout Containers

```csharp
// Horizontal layout
var hbox = new HBoxContainer();
hbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

// Vertical layout
var vbox = new VBoxContainer();
vbox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
```

---

## Updating Panels

### Adding New UI Elements

1. **Create the panel class** in `scripts/ui/` or `scripts/ui/components/`
2. **Build the UI** in the constructor using `Control`, `Panel`, `Label`, `Button`
3. **Add styles** using `UITheme` constants and theme overrides
4. **Add to UIManagerBootstrap** in the appropriate creation method
5. **Initialize** in the same method or via deferred call

### Example: Adding a Settings Panel

1. Create `SettingsPanel.cs`:

```csharp
public partial class SettingsPanel : Panel
{
    public Button CloseButton;

    public SettingsPanel()
    {
        // Apply dark theme
        UITheme.ApplyPanelStyle(this);

        // Create layout container
        var container = new VBoxContainer();
        container.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(container);

        var title = new Label("SETTINGS");
        title.AddThemeColorOverride("font_color", UITheme.ACCENT_GOLD);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        container.AddChild(title);

        CloseButton = new Button();
        CloseButton.Text = "CLOSE";
        CloseButton.CustomMinimumSize = new Vector2(100, UITheme.BUTTON_HEIGHT);
        UITheme.ApplyButtonStyle(CloseButton);
        container.AddChild(CloseButton);

        // Hidden by default
        Visible = false;
    }
}
```

2. Add to `UIManagerBootstrap.CreateTabSection()`:

```csharp
private void CreateSettingsTab()
{
    var settingsPanel = new SettingsPanel();
    settingsPanel.Name = "SettingsPanel";
    // Add to appropriate parent container
    parent.AddChild(settingsPanel);
}
```

3. Show/hide via button clicks or menu options.

---

## Related Documentation

- [Game Design](../design/GAME_DESIGN.md) - Overall game mechanics
- [Technical Spec](../technical/TECHNICAL_SPEC.md) - Architecture overview
- [AGENTS.md](../AGENTS.md) - Development guidelines

---

## Change Log

| Date | Version | Changes |
|------|---------|---------|
| 2026-01-13 | 1.0 | Initial uGUI-based UI Design |
| 2026-01-13 | 2.0 | Migrated to UI Toolkit with USS styling |
