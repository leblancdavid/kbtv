# UI Implementation Pattern

## Overview

KBTV uses a **scene-based UI system** in Godot 4.x, with programmatic instantiation and scene-based panels. Panels are defined as reusable `.tscn` scenes and managed through `UIManager.cs`.

## Why Scene-Based UI

- **Modular Design**: Each UI panel is a self-contained scene that can be edited in the Godot editor
- **Visual Editing**: Use Godot's scene editor for precise layout and styling
- **Reusability**: Scenes can be instantiated multiple times with different configurations
- **Maintainability**: Separation of concerns between UI structure (scenes) and logic (scripts)
- **Performance**: Godot's optimized scene instantiation and node management

## UI Architecture

### Current File Structure

| File | Lines | Responsibility |
|------|-------|----------------|
| `UIManager.cs` | ~400 | Main UI orchestrator for live show |
| `PanelFactory.cs` | ~250 | Factory for creating UI panel scenes |
| `ScreeningPanel.cs` | ~50 | Screening panel logic |
| `CallerPanel.cs` | ~50 | Caller list panel logic |
| `LiveShowHeader.cs` | ~100 | Header panel logic |

### UI Manager Structure

| Manager | Purpose | Active Phase |
|---------|---------|--------------|
| `UIManager` | Live show UI (tabs, panels, header, footer) | LiveShow only |

### Scene Configuration

The main UI uses a TabContainer scene (`TabContainerUI.tscn`) that contains:
- Header panel for show info and controls
- Tab container with three tabs: Callers, Items, Stats
- Footer panels for status displays

### Phase-Based Visibility

```csharp
// In UIManager.cs
public override void _Ready()
{
    // Initialize only when entering live show phase
    if (_gameState.CurrentPhase == GamePhase.LiveShow)
    {
        InitializeUI();
    }
}
```

## Using PanelFactory

The `PanelFactory` class handles programmatic creation of UI panels:

```csharp
// Create screening panel with caller data
var screeningPanel = _panelFactory.CreateScreeningPanelScene();

// Create caller list panel
var callerPanel = _panelFactory.CreateCallerPanelScene("INCOMING CALLERS", callers, headerColor, itemColor);

// Add to scene tree
mainContainer.AddChild(screeningPanel);
```

## Scene-Based Panel Creation

Panels are created using Godot's PackedScene system:

```csharp
// Load and instantiate scene
var scene = ResourceLoader.Load<PackedScene>("res://scenes/ui/ScreeningPanel.tscn");
var panel = scene.Instantiate<ScreeningPanel>();

// Configure panel
panel.SetCaller(caller);
panel.ConnectButtons(approveCallable, rejectCallable);
```

## Creating New UI Components

### Pattern 1: Scene-Based Panel

Create a `.tscn` file in `scenes/ui/` with the desired layout, then create a corresponding script:

```csharp
// MyPanel.cs
public partial class MyPanel : Panel
{
    public void Initialize(string title, Color backgroundColor)
    {
        // Configure panel appearance and content
        var titleLabel = GetNode<Label>("TitleLabel");
        titleLabel.Text = title;

        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = backgroundColor;
        AddThemeStyleboxOverride("panel", styleBox);
    }
}
```

### Pattern 2: Programmatic Fallback

For dynamic content, create panels programmatically as fallback:

```csharp
private Control CreateMyPanelFallback(string title, Color color)
{
    var panel = new Panel();
    panel.Name = "MyPanel";

    var layout = new VBoxContainer();
    layout.AddThemeConstantOverride("separation", 10);
    panel.AddChild(layout);

    var titleLabel = new Label();
    titleLabel.Text = title;
    titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
    layout.AddChild(titleLabel);

    return panel;
}
```

### Pattern 3: Signal Connections

Use Godot's signal system for event handling:

```csharp
private void ConnectSignals()
{
    if (_callerQueue != null)
    {
        _callerQueue.CallerQueueChanged += OnCallerQueueChanged;
    }
}

private void DisconnectSignals()
{
    if (_callerQueue != null)
    {
        _callerQueue.CallerQueueChanged -= OnCallerQueueChanged;
    }
}

private void OnCallerQueueChanged()
{
    // Update UI display
    RefreshTabContent(0); // Refresh callers tab
}
```

### Pattern 4: Deferred Call for UI Initialization

When setting properties on newly instantiated nodes, `_Ready()` may not have run yet. Use `CallDeferred()` to schedule operations after the node is fully initialized:

```csharp
public void SetCaller(Caller caller)
{
    _caller = caller;
    // Defer name assignment until after _Ready() runs
    CallDeferred(nameof(_ApplyCallerName), caller?.Name ?? "");
}

private void _ApplyCallerName(string name)
{
    if (_nameLabel != null)
    {
        _nameLabel.Text = name;
    }
    UpdateStatusIndicator();
}
```

**Why this pattern is needed:**
- `Instantiate()` creates a node but doesn't guarantee `_Ready()` has run
- Calling `SetCaller()` immediately after `Instantiate()` may find child node references as `null`
- Godot defers `CallDeferred()` calls until after `_Ready()` completes
- This ensures `_nameLabel` and other `@onready` nodes are available

**When to use:**
- Setting text on Labels in UI items created dynamically
- Configuring UI elements immediately after scene instantiation
- Any scenario where data binding happens before `_Ready()`

## Theme and Styling

Use Godot's theme system for consistent styling:

```csharp
// Apply theme colors
var styleBox = new StyleBoxFlat();
styleBox.BgColor = new Color(0.15f, 0.15f, 0.15f); // Dark background
panel.AddThemeStyleboxOverride("panel", styleBox);

// Theme colors
label.AddThemeColorOverride("font_color", new Color(0f, 1f, 0f)); // Green text
```

## Color Palette

```csharp
// KBTV Color Palette
new Color(1f, 0.7f, 0f)      // Gold - primary accent
new Color(0f, 0.8f, 0f)      // Green - success/money
new Color(0.8f, 0.2f, 0.2f)  // Red - error/alert
new Color(0.6f, 0.6f, 0.6f)  // Gray - secondary text
new Color(0.15f, 0.15f, 0.15f) // Dark bg
```

## File Structure

```
scripts/ui/
├── UIManager.cs              # Main UI orchestrator
├── PanelFactory.cs           # Panel creation factory
├── ScreeningPanel.cs         # Screening panel logic
├── CallerPanel.cs            # Caller list panel logic
├── LiveShowHeader.cs         # Header panel logic
├── LiveShowFooter.cs         # Footer panel logic
├── PreShowUIManager.cs       # Pre-show UI
├── TabContainerManager.cs    # Tab system manager
├── CallerTabManager.cs       # Caller-specific tab management
├── InputHandler.cs           # Input processing
├── DebugHelper.cs            # Debug tools
├── UITheme.cs                # Theme and styling
├── UIHelpers.cs              # Static helper methods
├── themes/
│   └── UIColors.cs           # Centralized color definitions
├── components/
│   ├── ReactiveListPanel.cs  # Differential UI updates
│   ├── CallerListAdapter.cs  # Caller list adapter
│   └── IListAdapter.cs       # List adapter interface
└── controllers/
    └── TabDefinition.cs      # Tab definition

scenes/ui/
├── TabContainerUI.tscn       # Main tab container scene
├── ScreeningPanel.tscn       # Screening panel scene
├── CallerPanel.tscn          # Caller list panel scene
├── CallerQueueItem.tscn      # Caller queue item scene
├── CallerTab.tscn            # Caller tab scene
├── LiveShowHeader.tscn       # Header panel scene
└── LiveShowFooter.tscn       # Footer panel scene
```

## Integration with Game State

The UI is managed by the UIManager which is created in the main scene:

```csharp
// In main scene (scenes/Main.tscn), UIManager is a child node
// It initializes when the game enters LiveShow phase
if (gameState.CurrentPhase == GamePhase.LiveShow)
{
    uiManager.ShowLiveUI();
}
```

## Common Issues

### UI Not Visible
1. Check Control.visible is true
2. Verify node is added to scene tree
3. Ensure parent containers have proper size flags
4. Check theme overrides aren't hiding elements

### UI Not Filling Screen
Use `SizeFlagsExpandFill` on containers and set `SizeFlagsStretchRatio` for proportional sizing.

### Buttons Not Clickable
1. Ensure button is visible and enabled
2. Check signal connections are properly established
3. Verify no overlapping controls blocking input

### Text Not Displaying
1. Check Label.text is set after node enters scene tree
2. Verify font and theme settings
3. Ensure container has sufficient size

### Scenes Not Loading
1. Check scene file paths are correct (`res://scenes/ui/...`)
2. Verify scenes exist and are not corrupted
3. Use GD.Print to debug ResourceLoader.Load calls

## Future Improvements

Consider enhancing the theme system:
- Create custom theme resources for consistent styling
- Add theme switching for different visual modes
- Implement responsive design for various screen sizes
