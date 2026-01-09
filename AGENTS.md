# AGENTS.md - KBTV Unity Project Guidelines

This document provides guidelines for AI agents working on the KBTV Unity project.

## Project Overview
- **Engine**: Unity 6000.0.28f1 (Unity 6)
- **Template**: 2D Game with Universal Render Pipeline (URP)
- **Language**: C# (Unity MonoBehaviour architecture)
- **Solution**: kbtv/kbtv.sln

## Build Commands

### Unity Editor
- Open the project in Unity Hub or directly via `kbtv/kbtv/kbtv.sln`
- Use Build Settings (Ctrl+Shift+B) to build for target platform
- **Command-line build**:
  ```bash
  # Windows build
  Unity.exe -buildTarget Win64 -buildPath Build/Windows -projectPath kbtv -quit -batchmode -executeMethod BuildScript.BuildWindows
  
  # Development build with debug symbols
  Unity.exe -buildTarget Win64 -buildPath Build/Windows -projectPath kbtv -quit -batchmode -executeMethod BuildScript.BuildWindows -developmentBuild
  ```

### Build Script Pattern
Create a `BuildScript.cs` in `Assets/Scripts/Editor/`:
```csharp
public class BuildScript {
    [MenuItem("Build/Build Windows")]
    public static void BuildWindows() {
        // Standard Unity build pipeline
    }
}
```

## Testing

### Unity Test Framework (UTF)
- Tests located in `Assets/Tests/` or `Assets/Scripts/Tests/`
- **Run all tests**: `Window > General > Test Runner > Run All`
- **Run single test**: Click the play icon next to the test method name
- **Command-line test run**:
  ```bash
  Unity.exe -projectPath kbtv/kbtv -batchmode -runTests -testPlatform editmode -testResults Results.xml
  Unity.exe -projectPath kbtv/kbtv -batchmode -runTests -testPlatform playmode -testResults Results.xml
  ```
- **Filter single test**:
  ```bash
  -testFilter "TestClassName.TestMethodName"
  ```

### Test Structure
```csharp
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

public class MyComponentTests {
    [Test]
    public void ComponentInitialization() { }
    
    [Test]
    public void ComponentBehavior_ContextResets() { }
}
```

## Code Style Guidelines

### Naming Conventions
- **Classes/Components**: PascalCase, descriptive (e.g., `PlayerController`, `EnemyAI`)
- **Methods**: PascalCase, verb-noun pattern (e.g., `MovePlayer()`, `CalculateDamage()`)
- **Variables/Fields**: camelCase (e.g., `playerHealth`, `movementSpeed`)
- **Private/Protected Fields**: camelCase with underscore prefix (e.g., `_playerHealth`)
- **Constants**: UPPER_CASE with underscores (e.g., `MAX_HEALTH = 100`)
- **Unity Events**: Prefix with `On` (e.g., `OnPlayerDeath`)

### Unity-Specific Patterns
```csharp
public class PlayerController : MonoBehaviour {
    [Header("Movement Settings")]
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _jumpForce = 10f;
    
    [SerializeField] private Rigidbody2D _rigidbody;
    
    private void Awake() {
        _rigidbody = GetComponent<Rigidbody2D>();
    }
    
    private void FixedUpdate() {
        HandleMovement();
    }
    
    public void TakeDamage(float amount) { }
}
```

### File Organization
- **Scripts**: `Assets/Scripts/[Domain]/[Component].cs`
- **Scripts/Editor**: Editor scripts in `Assets/Scripts/Editor/`
- **Scripts/Runtime**: Runtime scripts in `Assets/Scripts/Runtime/`
- **Prefabs**: `Assets/Prefabs/[Component].prefab`
- **Scenes**: `Assets/Scenes/[SceneName].unity`

### Component Architecture
- Use `[RequireComponent(typeof(ComponentType))]` for dependencies
- Implement proper lifecycle methods: `Awake()`, `Start()`, `OnEnable()`, `OnDisable()`
- Use `OnDestroy()` for cleanup (取消订阅事件)
- Place physics logic in `FixedUpdate()`, input in `Update()`

### Serialization
- Use `[SerializeField]` for private fields visible in Inspector
- Use `[Header("Group Name")]` for organization
- Use `[Range(min, max)]` for numeric values
- Use `[Tooltip("description")]` for documentation
- Avoid public fields; prefer properties with `[SerializeField]`

### Input Handling (New Input System)
- Use `InputActionAsset` for input definitions
- Reference actions via `PlayerInput` component
- Use callback methods: `OnMove(InputValue value)`
- Never poll input in `FixedUpdate()`

### Error Handling
- Use `Debug.LogError()` for critical errors
- Use `Debug.LogWarning()` for warnings
- Validate null references with `Assert.IsNotNull()`
- Wrap Unity API calls in try-catch for coroutines
- Never swallow exceptions silently

### Performance
- Cache component references in `Awake()` or `Start()`
- Use object pooling for frequently instantiated objects
- Avoid `GameObject.Find*` in Update loops
- Use `[SerializeField]` instead of `GetComponent` in Update
- Profile before optimizing; use Unity Profiler

### Git & Collaboration
- Create descriptive commit messages explaining the "why"
- Reference ticket/issue numbers in commits
- Use feature branches: `feature/[name]`, `bugfix/[name]`
- Keep Unity meta files with their assets

## Unity Editor Configuration
- **Serialization Mode**: Force Binary (see EditorSettings.asset)
- **Line Endings**: OS Native
- **Behavior Mode**: 2D (set in EditorSettings)
