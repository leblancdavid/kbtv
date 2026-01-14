# KBTV Developer Guide

This guide explains how to work with and extend the KBTV codebase. Whether you're adding new features, fixing bugs, or understanding the architecture, this document will help you navigate the project.

## 🏗️ Architecture Overview

### Core Principles

1. **Singleton Pattern**: All major systems use `SingletonNode<T>` for global access
2. **Event-Driven**: Systems communicate via C# events and Godot signals
3. **Resource-Based**: Game data stored as Godot Resources (`.tres` files)
4. **Modular UI**: Control-based UI with reusable components
5. **Separation of Concerns**: Each system has a single responsibility

### System Categories

- **Core**: Fundamental game systems (GameState, Time)
- **Managers**: Game logic managers (Listeners, Economy)
- **UI**: User interface systems and controls
- **Callers**: Caller generation and management
- **Data**: Game data structures and stats
- **Economy**: Money and upgrade systems
- **Persistence**: Save/load functionality

## 🚀 Adding New Features

### 1. Creating a New Manager

```csharp
// 1. Create new script in appropriate directory
// scripts/managers/NewManager.cs

using Godot;
using KBTV.Core;

namespace KBTV.Managers
{
    public partial class NewManager : SingletonNode<NewManager>
    {
        // Export properties for editor configuration
        [Export] private float _someSetting = 1.0f;

        // Properties
        public bool IsActive { get; private set; }

        // Events
        public event Action OnActivated;
        public event Action OnDeactivated;

        protected override void OnSingletonReady()
        {
            // Initialize your manager
            GD.Print("NewManager initialized");
        }

        public void Activate()
        {
            IsActive = true;
            OnActivated?.Invoke();
        }

        public void Deactivate()
        {
            IsActive = false;
            OnDeactivated?.Invoke();
        }
    }
}
```

```csharp
// 2. Add to main scene (scenes/Main.tscn)
[node name="NewManager" type="Node" parent="."]
script = ExtResource("path_to_newmanager_script")
someSetting = 2.0
```

```csharp
// 3. Access from other systems
var newManager = NewManager.Instance;
newManager.Activate();
```

### 2. Adding UI Components

```csharp
// 1. Create UI component
// scripts/ui/components/NewUIComponent.cs

using Godot;

namespace KBTV.UI.Components
{
    public partial class NewUIComponent : Control
    {
        [Export] private string _title = "New Component";

        private Label _titleLabel;
        private Button _actionButton;

        public override void _Ready()
        {
            _titleLabel = GetNode<Label>("TitleLabel");
            _actionButton = GetNode<Button>("ActionButton");

            _titleLabel.Text = _title;
            _actionButton.Pressed += OnActionPressed;
        }

        private void OnActionPressed()
        {
            GD.Print("Action button pressed!");
        }
    }
}
```

```csharp
// 2. Add to UIManagerBootstrap
public partial class UIManagerBootstrap : SingletonNode<UIManagerBootstrap>
{
    private void CreateNewUIComponent(Control parent)
    {
        var component = new NewUIComponent();
        component.Name = "NewUIComponent";
        parent.AddChild(component);
    }
}
```

### 3. Adding New Stats

```csharp
// 1. Add to StatType enum
public enum StatType
{
    // ... existing stats ...
    NewStat,
    AnotherStat
}

// 2. Add to VernStats class
public partial class VernStats : Resource
{
    [Export] private Stat _newStat = new Stat(50f, 0f, 100f);

    public Stat NewStat => _newStat;

    // Update CalculateVIBE if needed
    public float CalculateVIBE()
    {
        // Include new stat in calculation
        float vibe = /* existing calculation */;
        vibe += (_newStat.Normalized - 0.5f) * 20f; // Example modifier
        return vibe;
    }
}
```

### 4. Adding Dialogue Content

```csharp
// 1. Create dialogue JSON file
// assets/dialogue/new_arc.json
{
    "arcId": "new_arc_001",
    "topic": "NewTopic",
    "legitimacy": "Credible",
    "dialogue": [
        {
            "speaker": "Vern",
            "text": "Hello caller, what's your story?",
            "textVariants": {
                "neutral": "Hello caller, what's your story?",
                "gruff": "Alright, what's this about?",
                "amused": "Well hello there, what have you got for us?"
            }
        }
    ]
}
```

```csharp
// 2. Add to ArcRepository
[Export] private Godot.Collections.Array<string> _arcJsonFilePaths = new Godot.Collections.Array<string> {
    "res://assets/dialogue/new_arc.json"
};
```

## 🔧 Modifying Existing Systems

### Changing Caller Generation

```csharp
// Modify CallerGenerator.cs
private Caller GenerateRandomCaller()
{
    // Add new caller attributes
    CallerLegitimacy legitimacy = DetermineRandomLegitimacy();

    // Add new personality types
    string[] personalities = { "Normal", "Shy", "Aggressive", "Chatty" };
    string personality = personalities[GD.Randi() % personalities.Length];

    return new Caller(name, phoneNumber, location,
        claimedTopic, actualTopic, reason,
        legitimacy, phoneQuality, emotionalState, curseRisk,
        beliefLevel, evidenceLevel, coherence, urgency,
        personality, arcId, screeningSummary, patience, quality);
}
```

### Adding UI Themes

```csharp
// Create theme in Godot editor or programmatically
var theme = new Theme();

// Style buttons
var buttonStyle = new StyleBoxFlat();
buttonStyle.BgColor = new Color(0.2f, 0.2f, 0.2f);
theme.SetStylebox("normal", "Button", buttonStyle);

// Apply to UI elements
myButton.Theme = theme;
```

### Modifying Save Data

```csharp
// 1. Add to SaveData class
public class SaveData
{
    public int NewFeatureData = 0;
}

// 2. Implement ISaveable in your manager
public partial class NewManager : SingletonNode<NewManager>, ISaveable
{
    public void OnBeforeSave(SaveData data)
    {
        data.NewFeatureData = _someValue;
    }

    public void OnAfterLoad(SaveData data)
    {
        _someValue = data.NewFeatureData;
    }
}

// 3. Register with SaveManager
protected override void OnSingletonReady()
{
    SaveManager.Instance?.RegisterSaveable(this);
}
```

## 🧪 Testing and Debugging

### Using DebugHelper

```csharp
// In Godot editor, call these methods on the DebugHelper node
var debugHelper = GetNode<DebugHelper>("/root/Main/DebugHelper");

// Test game flow
debugHelper.StartShow();
debugHelper.SpawnCaller();
debugHelper.ShowGameState();
```

### Adding Debug Logging

```csharp
// Add debug logging to any system
public partial class MySystem : SingletonNode<MySystem>
{
    private void SomeMethod()
    {
        GD.Print($"MySystem: SomeMethod called with parameter: {param}");

        #if DEBUG
        // Additional debug-only code
        GD.PrintRich($"[color=yellow]Debug: {debugInfo}[/color]");
        #endif
    }
}
```

### Performance Profiling

```csharp
// Use Godot's built-in profiler
public override void _Process(double delta)
{
    using (var profiler = new Godot.Profiling.Profiler())
    {
        // Profile expensive operations
        DoExpensiveOperation();
    }
}
```

## 🎨 UI Development

### Layout Best Practices

```csharp
// Use containers for responsive layout
var container = new HBoxContainer();
container.SizeFlagsHorizontal = SizeFlags.ExpandFill;

// Add spacers for distribution
var spacer = new Control();
spacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
container.AddChild(spacer);

// Use anchors for precise positioning
var panel = new Panel();
panel.SetAnchorsPreset(LayoutPreset.TopWide);
panel.Size = new Vector2(0, 50);
```

### Styling Guidelines

```csharp
// Consistent color scheme
private static class UIColors
{
    public static readonly Color Primary = new Color(0.2f, 0.2f, 0.2f);
    public static readonly Color Secondary = new Color(0.15f, 0.15f, 0.15f);
    public static readonly Color Accent = new Color(0f, 0.8f, 0f);
    public static readonly Color Warning = new Color(0.8f, 0.2f, 0.2f);
}

// Apply consistent styling
private void StyleButton(Button button)
{
    var style = new StyleBoxFlat();
    style.BgColor = UIColors.Primary;
    button.AddThemeStyleboxOverride("normal", style);
}
```

## 🔄 Event System

### Creating Custom Events

```csharp
public partial class MySystem : SingletonNode<MySystem>
{
    // Define events
    public event Action<int> OnValueChanged;
    public event Action<string> OnMessageReceived;

    // Trigger events
    private void UpdateValue(int newValue)
    {
        _currentValue = newValue;
        OnValueChanged?.Invoke(newValue);
    }

    private void ReceiveMessage(string message)
    {
        OnMessageReceived?.Invoke(message);
    }
}
```

### Subscribing to Events

```csharp
public partial class AnotherSystem : SingletonNode<AnotherSystem>
{
    protected override void OnSingletonReady()
    {
        // Subscribe to events
        MySystem.Instance.OnValueChanged += HandleValueChanged;
        MySystem.Instance.OnMessageReceived += HandleMessageReceived;
    }

    public override void _ExitTree()
    {
        // Always unsubscribe
        MySystem.Instance.OnValueChanged -= HandleValueChanged;
        MySystem.Instance.OnMessageReceived -= HandleMessageReceived;
    }

    private void HandleValueChanged(int newValue)
    {
        GD.Print($"Value changed to: {newValue}");
    }

    private void HandleMessageReceived(string message)
    {
        GD.Print($"Message received: {message}");
    }
}
```

## 📊 Performance Optimization

### Object Pooling

```csharp
// For frequently created/destroyed objects like UI elements
public class ObjectPool<T> where T : Node
{
    private List<T> _pool = new List<T>();
    private PackedScene _scene;

    public ObjectPool(PackedScene scene)
    {
        _scene = scene;
    }

    public T Get()
    {
        if (_pool.Count > 0)
        {
            var obj = _pool[_pool.Count - 1];
            _pool.RemoveAt(_pool.Count - 1);
            return obj;
        }
        return _scene.Instantiate<T>();
    }

    public void Return(T obj)
    {
        obj.GetParent().RemoveChild(obj);
        _pool.Add(obj);
    }
}
```

### Efficient Updates

```csharp
// Only update when necessary
public partial class OptimizedSystem : SingletonNode<OptimizedSystem>
{
    private bool _needsUpdate = false;
    private float _updateTimer = 0f;

    public override void _Process(double delta)
    {
        // Batch updates to reduce frequency
        _updateTimer += (float)delta;
        if (_updateTimer >= 0.1f) // Update 10 times per second
        {
            _updateTimer = 0f;
            if (_needsUpdate)
            {
                PerformUpdate();
                _needsUpdate = false;
            }
        }
    }

    public void MarkForUpdate()
    {
        _needsUpdate = true;
    }
}
```

## 🚀 Deployment

### Export Configuration

1. **Project → Export**: Open export dialog
2. **Add Preset**: Choose target platform (Windows, Linux, macOS)
3. **Configure Options**:
   - **Runnable**: Enable for executable
   - **Debug**: Disable for release builds
   - **Export Path**: Set output directory

### Build Optimization

```csharp
// Use release builds for better performance
// Project → Export → Advanced Options → Export As Release
```

### Platform-Specific Considerations

```csharp
// Handle platform differences
public override void _Ready()
{
    if (OS.GetName() == "Windows")
    {
        // Windows-specific code
    }
    else if (OS.GetName() == "Linux")
    {
        // Linux-specific code
    }
}
```

## 🐛 Troubleshooting

### Common Issues

**"Type not found" errors**: Ensure all scripts are properly saved and compiled
**UI not appearing**: Check Canvas setup and node hierarchy
**Events not firing**: Verify event subscription and unsubscription
**Performance issues**: Use Godot's profiler to identify bottlenecks

### Debug Tips

1. **Use GD.Print() liberally** for debugging
2. **Check the Godot debugger** for runtime errors
3. **Use DebugHelper** for testing game systems
4. **Profile with Godot's built-in profiler**

### Getting Help

1. **Check existing code** for similar implementations
2. **Review API documentation** in `API_DOCUMENTATION.md`
3. **Test with DebugHelper** before implementing
4. **Use Godot's documentation** for UI and system questions

## 📝 Best Practices

### Code Style
- Use PascalCase for public members, camelCase for private
- Add XML documentation comments to public methods
- Use meaningful variable names
- Keep methods focused on single responsibilities

### Error Handling
```csharp
try
{
    // Risky operation
    DoSomethingDangerous();
}
catch (System.Exception e)
{
    GD.PrintErr($"Error in {nameof(MyMethod)}: {e.Message}");
    // Handle gracefully
}
```

### Resource Management
```csharp
// Properly dispose of resources
public override void _ExitTree()
{
    // Unsubscribe from events
    // Dispose of unmanaged resources
    // Clean up references
}
```

This guide should help you understand and extend the KBTV codebase. Remember to test thoroughly and follow the established patterns!