# AGENTS.md - KBTV Unity Project Guidelines

This document provides guidelines for AI agents working on the KBTV Unity project.

## AI Agent Best Practices

**Avoid large responses**: When making edits that involve lots of text content (dialogue, large code blocks, etc.), break them into smaller chunks. Large JSON responses can cause parsing errors on the client side. Aim for edits under 100 lines when possible.

## Session Continuity

Use `SESSION_LOG.md` to maintain context across sessions.

**At session start:**
1. Read `SESSION_LOG.md` to understand previous context
2. Offer to continue previous work if status is "In Progress" or "Blocked"

**During work:**
- **Update the session log FIRST** before creating other files, so work isn't lost if the session is interrupted
- Update the log automatically as tasks progress
- Track files modified, work done, and next steps
- Update status: `In Progress`, `Completed`, or `Blocked`

**When starting a new task:**
- Move "Current Session" content to "Previous Session"
- Start fresh with the new task details

**Fields to maintain:**
| Field | Purpose |
|-------|---------|
| Task | What we're trying to accomplish |
| Status | In Progress / Completed / Blocked |
| Files Modified | Quick reference to changed files |
| Work Done | Avoid repeating completed work |
| Next Steps | Know exactly where to pick up |
| Branch | Git context |
| Related Docs | Docs referenced during work |
| Blockers | Issues encountered |

## Documentation References

Project documentation is located in the `docs/` folder. **Read these documents for context before starting work:**

| Document | Description |
|----------|-------------|
| [GAME_DESIGN.md](docs/design/GAME_DESIGN.md) | Game design document - core mechanics, characters, game loop, and features |
| [TECHNICAL_SPEC.md](docs/technical/TECHNICAL_SPEC.md) | Architecture, systems design, and technical requirements |
| [ART_STYLE.md](docs/art/ART_STYLE.md) | Visual direction, color palette, and asset guidelines |
| [AUDIO_DESIGN.md](docs/audio/AUDIO_DESIGN.md) | Sound design, music, and audio technical specs |
| [ROADMAP.md](docs/design/ROADMAP.md) | Development milestones and feature backlog |
| [CI_CD_SETUP.md](docs/technical/CI_CD_SETUP.md) | GitHub Actions build/release setup and Unity license activation |
| [UI_DESIGN.md](docs/ui/UI_DESIGN.md) | UI design document - layout, panels, typography, and interaction design |
| [UI_IMPLEMENTATION.md](docs/ui/UI_IMPLEMENTATION.md) | UI implementation pattern - uGUI Canvas, phase-based UI, creating panels |
| [SCREENING_DESIGN.md](docs/ui/SCREENING_DESIGN.md) | Screening system design - information gathering game, property revelations, patience system |
| [CONVERSATION_DESIGN.md](docs/ui/CONVERSATION_DESIGN.md) | Dialogue system overview - arc-based conversations, mood, discernment |
| [CONVERSATION_ARCS.md](docs/ui/CONVERSATION_ARCS.md) | Arc system design - mood variants, belief branches, selection flow |
| [CONVERSATION_ARC_SCHEMA.md](docs/ui/CONVERSATION_ARC_SCHEMA.md) | JSON schema and full example arc with all 5 mood variants |
| [DEAD_AIR_FILLER.md](docs/ui/DEAD_AIR_FILLER.md) | Broadcast flow - show opening/closing, between-callers, and dead air filler |
| [VOICE_AUDIO.md](docs/audio/VOICE_AUDIO.md) | Voice audio production plan - TTS vs pre-generated, file organization, workflow |
| [TOOLS.md](docs/tools/TOOLS.md) | Python scripts and development tools - audio generation, setup instructions |
| [SAVE_SYSTEM.md](docs/technical/SAVE_SYSTEM.md) | Save/load system - JSON persistence, SaveData structure, auto-save triggers |
| [ECONOMY_SYSTEM.md](docs/systems/ECONOMY_SYSTEM.md) | Money tracking, income calculation, transactions |
| [STATION_EQUIPMENT.md](docs/systems/STATION_EQUIPMENT.md) | Equipment upgrades - Phone Line, Broadcast, audio quality progression |
| [AD_SYSTEM.md](docs/systems/AD_SYSTEM.md) | Ad system (Phase 1 implemented) - ad breaks, sponsors, revenue generation |
| [ECONOMY_PLAN.md](docs/systems/ECONOMY_PLAN.md) | Economy expansion plan - income sources, expenses, penalties, progression |
| [TOPIC_EXPERIENCE.md](docs/design/TOPIC_EXPERIENCE.md) | Topic leveling system - XP, freshness, bonuses, guest booking |
| [SPECIAL_EVENTS.md](docs/design/SPECIAL_EVENTS.md) | In-broadcast events - urgency, rewards, penalties, cross-topic events |
| [EVIDENCE_SYSTEM.md](docs/systems/EVIDENCE_SYSTEM.md) | Evidence collection - types, tiers, cabinet, set bonuses |
| [TOOLS_EQUIPMENT.md](docs/systems/TOOLS_EQUIPMENT.md) | Investigation tools - camera, EMF reader, audio recorder, upgrades |
| [VERN_STATS.md](docs/systems/VERN_STATS.md) | Vern's stats system - dependencies, VIBE, mood types, sigmoid functions |
| [SESSION_LOG.md](SESSION_LOG.md) | Session continuity - current task, work done, next steps for recovery |

When adding new documentation (technical specs, feature plans, art guidelines, etc.), place them in the `docs/` folder and add a reference here.

## Project Overview
- **Engine**: Unity 6000.3.1f1 (Unity 6)
- **Template**: 2D Game with Universal Render Pipeline (URP)
- **Language**: C# (Unity MonoBehaviour architecture)
- **Solution**: kbtv/kbtv.sln

## Quick Start

To run the game:
1. Open Unity with the project (`kbtv/kbtv`)
2. Open `Assets/Scenes/SampleScene.unity`
3. From menu: **KBTV > Setup Game Scene**
4. Press **Play**

The `GameSetup` utility (`Assets/Scripts/Editor/GameSetup.cs`) auto-creates missing assets and configures the scene.

## Input System

KBTV uses Unity's **Input System package (v1.17.0)** for all input handling. The project has been fully migrated from the deprecated Input Manager.

### Configuration
- **Active Input Handler**: Input System Package (ProjectSettings > Player > Active Input Handling)
- **UI Input Module**: `InputSystemUIInputModule` (created by GameBootstrap)
- **Input Actions**: Create `Assets/Input/KBTVInputActions.inputactions` in Unity Editor when adding controller/gamepad support

### Usage Guidelines
- **Never use legacy APIs**: Avoid `UnityEngine.Input.*`, `Input.GetKey()`, etc.
- **UI Interactions**: All handled automatically by InputSystemUIInputModule
- **Future Input Features**: Use Input Actions asset for any new input requirements
- **Controller Support**: Input Actions are pre-configured for gamepad/controller input

### Migration Notes
- Legacy `InputManager.asset` has been removed
- All UI components work with Input System automatically
- EventSystem uses `InputSystemUIInputModule` for proper input handling

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

### Script Compilation Check
Check C# scripts for compilation errors without doing a full build:

**PowerShell:**
```powershell
.\check_compilation.ps1
```

**Direct Unity invocation:**
```bash
Unity.exe -projectPath kbtv/kbtv -batchmode -quit -executeMethod KBTV.Editor.CompilationCheck.Check
```

**Exit codes:**
- `0` = Success (no compilation errors)
- `1` = Compilation errors found
- `2` = Unity API error

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

#### Unity Meta Files (IMPORTANT)
Unity generates `.meta` files for every asset and folder. These files contain GUIDs that Unity uses to track references between assets.

**Always commit `.meta` files together with their assets:**
- When adding a new `.cs` file, also stage its `.cs.meta` file
- When creating a new folder, also stage its `.meta` file
- When deleting assets, also delete the corresponding `.meta` files
- Never commit an asset without its `.meta` file (causes broken references)

**Example workflow:**
```bash
# After creating new files, check for untracked meta files
git status

# Stage both the file and its meta
git add Assets/Scripts/MyNewScript.cs Assets/Scripts/MyNewScript.cs.meta

# Or stage all changes in a directory (includes meta files)
git add Assets/Scripts/NewFeature/
```

**Why this matters:**
- Missing `.meta` files cause Unity to regenerate GUIDs, breaking prefab/scene references
- Different GUIDs on different machines cause merge conflicts and broken references
- The `.meta` file stores import settings (texture compression, script execution order, etc.)

## Unity Editor Configuration
- **Serialization Mode**: Force Binary (see EditorSettings.asset)
- **Line Endings**: OS Native
- **Behavior Mode**: 2D (set in EditorSettings)
