# UI Implementation Pattern

## Overview

KBTV uses a **programmatic Godot Control** approach for all UI. This provides native Godot integration with flexible anchoring and theming systems.

## Why Godot Controls

- Native Godot integration with scene system
- Powerful anchor-based layouts for responsive design
- Theme system for consistent styling
- No external dependencies or complex configurations
- Easy debugging through scene tree inspection
- Runtime UI modification capabilities

## UI Architecture

### Current File Structure

| File | Lines | Responsibility |
|------|-------|----------------|
| `UIManagerBootstrap.cs` | ~1500 | Live show UI manager with all functionality |
| `PreShowUIManager.cs` | ~200 | Pre-show UI for topic selection |
| `UITheme.cs` | ~50 | Dark theme constants and styling utilities |
| `UIHelpers.cs` | ~280 | Static helper methods for UI creation |
| `TopicLoader.cs` | ~50 | Topic resource loading and management |

### Two UI Managers

| Manager | Purpose | Active Phase |
|---------|---------|--------------|
| `PreShowUIManager` | Topic selection, show preparation | PreShow only |
| `UIManagerBootstrap` | Live show UI (callers, stats, ads, transcript) | LiveShow only |

### Canvas Configuration

```csharp
_canvas = new Control();
_canvas.Name = "Canvas";
_canvas.SetAnchorsPreset(LayoutPreset.FullRect);
AddChild(_canvas);

// Godot handles responsive scaling through anchor system
// No additional CanvasScaler needed
```

### Phase-Based Visibility

```csharp
private void HandlePhaseChanged(GamePhase oldPhase, GamePhase newPhase)
{
    _canvas.Visible = (newPhase == TargetPhase);
}
```

## Using UIHelpers

```csharp
// Create a text element
var text = UIHelpers.CreateText("Hello", 14, Color.white, TextAnchor.MiddleLeft);

// Create a button with click handler
var button = UIHelpers.CreateButton("Click Me", 12, Color.gray, Color.white, () => { /* handler */ });

// Create a horizontal layout container
var hLayout = UIHelpers.CreateHLayout(padding: 8, spacing: 4);

// Create a scrollable list
var (container, content) = UIHelpers.CreateScrollList(minHeight: 100);

// Create a spacer
var spacer = UIHelpers.CreateSpacer(flexibleWidth: 1);
```

## Using UIPanelBuilder (Fluent API)

```csharp
var panel = UIPanelBuilder.Create("MyPanel")
    .WithBackground(new Color(0.1f, 0.1f, 0.1f))
    .WithMinHeight(120)
    .WithHLayout(padding: 8, spacing: 4)
    .AddHeader("PANEL TITLE", fontSize: 12)
    .AddButton("Action", onClick: HandleAction, fontSize: 14)
    .AddSpacer(flexibleWidth: 1)
    .Build();
```

## Creating New UI Components

### Pattern 1: Header Element (using UIPanelBuilder)

```csharp
private void CreateMyElement(Transform parent)
{
    var element = UIPanelBuilder.Create("MyElement")
        .WithMinWidth(100)
        .WithHLayout(4, 4)
        .Build();
    element.transform.SetParent(parent, false);
    
    UIHelpers.AddTextToParent(element.transform, "Label", 12, Color.gray);
}
```

### Pattern 2: Footer Panel

```csharp
private void CreateMyFooterPanel(Transform parent)
{
    var panel = UIPanelBuilder.Create("MyPanel")
        .WithBackground(new Color(0.12f, 0.12f, 0.12f))
        .WithMinHeight(140)
        .WithPreferredHeight(140)
        .WithHLayout(8, 8)
        .Build();
    panel.transform.SetParent(parent, false);
}
```

### Pattern 3: Event Handler

```csharp
private void SubscribeToEvents()
{
    if (_someManager != null)
    {
        _someManager.OnSomeEvent += OnSomeEvent;
    }
}

private void UnsubscribeFromEvents()
{
    if (_someManager != null)
    {
        _someManager.OnSomeEvent -= OnSomeEvent;
    }
}

private void OnSomeEvent(SomeType value)
{
    // Update display
}
```

## Font

Use Unity's legacy runtime font for compatibility:

```csharp
text.font = UIHelpers.DefaultFont; // Uses Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
```

## Color Constants

```csharp
// KBTV Color Palette
new Color(1f, 0.7f, 0f)      // Gold - primary accent
new Color(0.2f, 0.8f, 0.2f)  // Green - success/money
new Color(0.8f, 0.2f, 0.2f)  // Red - error/alert
new Color(0.267f, 0.667f, 0.267f)  // Green - positive change
new Color(0.439f, 0.439f, 0.439f)  // Gray - secondary text
new Color(0.05f, 0.05f, 0.08f)     // Dark bg
```

## File Structure

```
Assets/Scripts/Runtime/UI/
├── UIManagerBootstrap.cs     # Main orchestrator (partial)
├── UIHeader.cs              # Header creation (partial)
├── UITabs.cs                # Tab system (partial)
├── UIFooter.cs              # Footer panels (partial)
├── UIEvents.cs              # Event subscriptions (partial)
├── UIDisplays.cs            # Display updates (partial)
├── UIHelpers.cs             # Static helper methods
├── UIPanelBuilder.cs        # Fluent panel builder
└── PreShowUIManager.cs      # Pre-show UI
```

## Integration with GameBootstrap

The UI managers are automatically created by `GameBootstrap.cs`:

```csharp
// Create PreShow UI
if (PreShowUIManager.Instance == null)
{
    GameObject preShowUIObj = new GameObject("PreShowUIManager");
    preShowUIObj.AddComponent<PreShowUIManager>();
}

// Create Live Show UI (only if enabled)
if (_enableLiveShowUI && UIManagerBootstrap.Instance == null)
{
    GameObject uiObj = new GameObject("UIManager");
    uiObj.AddComponent<UIManagerBootstrap>();
}
```

UI creation is controlled by the `_enableLiveShowUI` boolean field in the GameBootstrap inspector.

## Common Issues

### UI Not Visible
1. Check Canvas sortingOrder
2. Verify Canvas.enabled is true
3. Ensure CanvasScaler reference resolution matches target
4. Check if parent RectTransform has valid anchors

### UI Not Filling Screen
Set `childForceExpandHeight = false` in VerticalLayoutGroup and use `flexibleHeight = 1` on the element that should expand.

### Buttons Not Clickable
1. Ensure Canvas has GraphicRaycaster
2. Check button.targetGraphic is set
3. Verify button.interactable is true

### Text Looks Blurry
Use CanvasScaler with appropriate reference resolution (1920x1080 recommended)

### UI Not Displaying
1. Check Unity Console for debug messages from UIManagerBootstrap
2. Verify canvas test panel appears (red semi-transparent panel)
3. Use debug context menu options on UIManager GameObject:
   - `Debug/Test Font Loading` - Tests font loading
   - `Debug/Create Minimal UI Test` - Creates simple test UI
4. Check for exceptions in UI creation (now properly logged)

## Future Improvements

Consider migrating to TextMeshPro for better text rendering:
- Install TextMeshPro package
- Replace `Text` with `TMPro.TextMeshProUGUI`
- Update font references to TMP fonts
