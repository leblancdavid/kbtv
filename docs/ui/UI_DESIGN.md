# UI Design Document

This document describes the KBTV Godot UI architecture using **Control nodes** with theme styling, a flexible approach for building game UI.

## Table of Contents

1. [Overview](#overview)
2. [Control Node Architecture](#control-node-architecture)
3. [USS Styling](#uss-styling)
4. [Panel Creation Patterns](#panel-creation-patterns)
5. [Standard Dimensions](#standard-dimensions)
6. [Common Patterns](#common-patterns)
7. [Updating Panels](#updating-panels)

---

## Overview

The KBTV UI system uses **Godot Control nodes** with theme-based styling:

- **Theme resources** - Centralized styling with variables and overrides
- **Control node hierarchy** - Flexible UI structure with containers
- **PackedScene instantiation** - Reusable UI components
- **UIManagerBootstrap** - Central manager handling UI initialization and updates

### Key Benefits

- **Flexible layout** - Container nodes handle layout automatically
- **Clean styling separation** - Theme resources contain all visual styles
- **Visual editing** - UI can be designed in Godot editor
- **Better performance** - Native Godot UI rendering

---

## Control Node Architecture

### Folder Structure

```
scenes/
├── ui/
│   ├── UIManagerBootstrap.cs      ← Central UI manager
│   ├── TabContainerUI.tscn         ← Root UI scene
│   ├── LiveShowHeader.tscn         ← Header component
│   ├── ScreeningPanel.tscn         ← Panel components
│   ├── CallerPanel.tscn
│   └── ...
scripts/
└── ui/
    ├── UIManagerBootstrap.cs
    ├── PanelFactory.cs
    └── ...
```

### UI Hierarchy

```
GameUI (root)
├── HeaderBar (height: 28px)
│   ├── LiveIndicator
│   ├── Clock
│   ├── Remaining
│   ├── Spacer
│   └── Listeners
├── MainContent (flex-grow: 1)
│   └── LeftColumn
│       └── TabContainer
│           ├── TabButtonContainer
│           │   ├── TabCallers
│           │   ├── TabItems
│           │   └── TabStats
│           └── TabContentArea
│               ├── CallersContent (flex-direction: row)
│               │   ├── ScreenerColumn (60%)
│               │   └── QueueColumn (40%)
│               ├── ItemsContent (hidden by default)
│               └── StatsContent (hidden by default)
└── Footer (height: 140px, flex-direction: row)
    ├── OnAirColumn (35%)
    ├── TranscriptColumn (45%)
    └── AdBreakColumn (20%)
```

---

## Theme Styling

### Main Theme

The main theme is loaded from: `res://themes/default_theme.tres`

### CSS Variables

```css
:root {
    /* Colors */
    --bg-dark: #1D1D1D;
    --bg-panel: #3A3A3A;
    --bg-border: #4A4A4A;
    --text-primary: #E0E0E0;
    --text-secondary: #A0A0A0;
    --text-dim: #707070;
    --text-amber: #FFB300;
    --text-white: #FFFFFF;
    --accent-green: #44AA44;
    --accent-red: #CC3333;
    --accent-cyan: #44AAAA;
    --accent-yellow: #FFCC00;

    /* Spacing */
    --spacing-xs: 4px;
    --spacing-sm: 6px;
    --spacing-md: 8px;
    --spacing-lg: 12px;
    --spacing-xl: 16px;

    /* Font Sizes */
    --font-xs: 10px;
    --font-sm: 12px;
    --font-md: 14px;
    --font-lg: 18px;
    --font-xl: 24px;

    /* Heights */
    --header-height: 28px;
    --tab-height: 24px;
    --footer-height: 140px;
    --button-height: 28px;

    /* Panel Widths */
    --onair-width: 35%;
    --transcript-width: 45%;
    --adbreak-width: 20%;
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
    public partial class MyPanel : Control
    {
        public Label TitleText;
        public Control ContentContainer;

        public MyPanel()
        {
            // Set container styles
            SizeFlagsVertical = SizeFlags.Fill;

            // Create header
            var header = new Label { Text = "My Panel" };
            header.AddThemeColorOverride("font_color", new Color(1f, 0.7f, 0f));
            header.AddThemeFontSizeOverride("font_size", 10);
            AddChild(header);

            // Create content
            ContentContainer = new Control();
            ContentContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
            AddChild(ContentContainer);
        }

        public void _Ready()
        {
            // Subscribe to events, load data
        }
    }
}
```

### Adding to GameUIManager

In `GameUIManager.BuildMainContent()`:

```csharp
var myPanel = new MyPanel { name = "MyPanel" };
myPanel.style.flexGrow = 1;
container.Add(myPanel);
```

Then initialize in `InitializePanels()`:

```csharp
myPanel?.Initialize();
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

| CSS Variable | Hex | Usage |
|--------------|-----|-------|
| --bg-dark | #1D1D1D | Root background |
| --bg-panel | #3A3A3A | Panel backgrounds |
| --bg-border | #4A4A4A | Dividers, borders |
| --text-primary | #E0E0E0 | Main text |
| --text-amber | #FFB300 | Headers, accents |
| --accent-green | #44AA44 | Success, approve |
| --accent-red | #CC3333 | Error, on-air |
| --accent-cyan | #44AAAA | Progress bars |
| --accent-yellow | #FFCC00 | Timers |

---

## Common Patterns

### Creating Buttons

```csharp
var button = new Button { text = "ACTION" };
button.style.height = 28;
button.style.backgroundColor = new Color(0.267f, 0.667f, 0.267f);  /* --accent-green */
button.style.color = Color.white;
button.clicked += OnButtonClicked;
container.Add(button);
```

### Creating Labels

```csharp
var label = new Label("Text");
label.style.color = new Color(0.878f, 0.878f, 0.878f);  /* --text-primary */
label.style.fontSize = 14;
label.style.flexGrow = 1;
container.Add(label);
```

### Toggling Visibility

```csharp
element.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
```

### Setting Width/Height

```csharp
element.style.width = new Length(100, LengthUnit.Percent);
element.style.height = new Length(28, LengthUnit.Pixel);
```

### Flex Layout

```csharp
container.style.flexDirection = FlexDirection.Row;
container.style.alignItems = Align.Center;
container.style.justifyContent = Justify.SpaceBetween;
```

---

## Updating Panels

### Adding New UI Elements

1. **Create the panel class** in `Assets/Scripts/Runtime/UI/`
2. **Build the UI** in the constructor using `VisualElement`, `Label`, `Button`
3. **Add styles** using USS properties (or inline `style.*`)
4. **Add to GameUIManager** in `BuildMainContent()` or `BuildFooter()`
5. **Initialize** in `InitializePanels()`

### Example: Adding a Settings Panel

1. Create `SettingsPanel.cs`:

```csharp
public class SettingsPanel : VisualElement
{
    public Button CloseButton;

    public SettingsPanel()
    {
        style.flexDirection = FlexDirection.Column;
        style.backgroundColor = new Color(0.227f, 0.227f, 0.227f);
        style.paddingTop = 12;
        style.paddingBottom = 12;
        style.paddingLeft = 12;
        style.paddingRight = 12;

        var title = new Label("SETTINGS");
        title.style.color = new Color(1f, 0.7f, 0f);
        title.style.fontSize = 10;
        title.style.fontWeight = FontStyle.Bold;
        Add(title);

        CloseButton = new Button { text = "CLOSE" };
        CloseButton.style.height = 28;
        CloseButton.style.marginTop = 12;
        Add(CloseButton);
    }
}
```

2. Add to `GameUIManager.BuildMainContent()`:

```csharp
var settingsPanel = new SettingsPanel { name = "SettingsPanel" };
settingsPanel.style.display = DisplayStyle.None;  // Hidden by default
mainContent.Add(settingsPanel);
```

3. Show/hide via tab switching or button clicks.

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
