# AGENTS.md - KBTV Godot Project Guidelines

This document provides guidelines for AI agents working on the KBTV Godot project.

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
| [CI_CD_SETUP.md](docs/technical/CI_CD_SETUP.md) | Build and export setup for Godot (CI/CD not currently implemented) |
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
| [TESTING.md](docs/testing/TESTING.md) | GdUnit4 testing framework setup, patterns, and best practices |

When adding new documentation (technical specs, feature plans, art guidelines, etc.), place them in the `docs/` folder and add a reference here.

## Architecture Patterns

### Service Registry Pattern

KBTV uses a **Service Registry** (Autoload) for global service access instead of singletons:

```csharp
// Access services through ServiceRegistry
var repository = ServiceRegistry.Instance.CallerRepository;
var screeningController = ServiceRegistry.Instance.ScreeningController;
var events = ServiceRegistry.Instance.EventAggregator;
```

**Pattern Benefits:**
- Centralized service management
- Testability (can replace services with mocks)
- Lazy initialization
- Clear dependency relationships

**How Services Register:**

1. **Node-based services (autoloads/scene nodes):** Register themselves in `_Ready()`
```csharp
public override void _Ready()
{
    ServiceRegistry.Instance.RegisterSelf<MyService>(this);
    // ... rest of initialization
}
```

2. **Plain class services (created by ServiceRegistry):** Registered in `RegisterCoreServices()`
```csharp
private void RegisterCoreServices()
{
    var eventAggregator = new EventAggregator();
    RegisterSelf<IEventAggregator>(eventAggregator);

    var repository = new CallerRepository();
    RegisterSelf<ICallerRepository>(repository);

    var screeningController = new ScreeningController();
    RegisterSelf<IScreeningController>(screeningController);
}
```

**AllServicesReady Signal:**
- The registry emits `AllServicesReady` after services finish registering
- Uses **timeout-based detection** (0.5s) instead of hardcoded counts
- No need to update expected service count when adding/removing services
- Minimum 5 services required before checking timeout
```csharp
// Timeout-based detection in ServiceRegistry._Process()
if (!_allReadyEmitted && _registeredCount >= MIN_SERVICES_EXPECTED)
{
    if (Time.GetTicksMsec() / 1000.0 - _lastRegistrationTime >= REGISTRATION_TIMEOUT)
    {
        _allReadyEmitted = true;
        EmitSignal("AllServicesReady");
    }
}
```

**Autoload Order (Critical):**
1. `ServiceRegistry` - Always first, creates base services
2. `UIManager` - Needed by TabContainerManager/PreShowUIManager
3. `GameStateManager` - Needed by many services
4. `TimeManager`, `ListenerManager`, `EconomyManager` - Can depend on GameStateManager
5. `GlobalTransitionManager` - Handles fade transitions
6. `TabContainerManager`, `PreShowUIManager` - Need UIManager for layer registration

**Files:**
- `scripts/core/ServiceRegistry.cs` - Main service registry (Autoload)
- `RegisterSelf<T>()` method handles both interface and concrete type registration

**Registered Services:**
- `GameStateManager` - Controls game phases
- `TimeManager` - Handles show timing
- `ListenerManager` - Tracks audience size
- `EconomyManager` - Manages money
- `SaveManager` - Handles persistence
- `UIManager` - Main UI orchestrator
- `CallerGenerator` - Spawns incoming callers
- `CallerRepository` - Manages caller data
- `ScreeningController` - Handles caller screening
- `EventAggregator` - Pub/sub event system
- `GlobalTransitionManager` - Handles fade-to-black transitions

### Event Aggregation Pattern

Components communicate through events instead of direct coupling:

```csharp
// Subscribe to events
ServiceRegistry.Instance.EventAggregator.Subscribe<Events.Screening.ScreeningStarted>(OnScreeningStarted);

// Publish events
ServiceRegistry.Instance.EventAggregator.Publish(new Events.Screening.ScreeningStarted(caller));
```

**Event Domains:**
- `Core.Events.Screening` - Screening process events
- `Core.Events.Queue` - Queue state events
- `Core.Events.OnAir` - On-air caller events

### Result Type Pattern

Use `Result<T>` for operations that can fail:

```csharp
var result = repository.AddCaller(caller);
if (result.IsSuccess)
{
    // Handle success
}
else
{
    GD.PrintErr($"Failed: {result.ErrorCode}: {result.ErrorMessage}");
}

// Or use pattern matching
result.Switch(
    onSuccess: caller => { /* ... */ },
    onFailure: (message, code) => { GD.PrintErr($"{code}: {message}"); }
);
```

### Repository Pattern

Data access is encapsulated in repositories:

```csharp
// Define interfaces
public interface ICallerRepository
{
    Result<Caller> AddCaller(Caller caller);
    Result<Caller> StartScreening(Caller caller);
    // ...
}

// Implementations are registered in ServiceRegistry
```

### UI Component Patterns

1. **[Export] attributes** for node references instead of `GetNode<>()` with strings
2. **ReactiveListPanel** for differential UI updates (no full rebuilds)
3. **Centralized colors** in `scripts/ui/themes/UIColors.cs`

## Project Overview
- **Engine**: Godot 4.x
- **Template**: 2D Game
- **Language**: C# (primary) with some GDScript
- **Project**: project.godot

## Quick Start

To run the game:
1. Open Godot 4.x
2. Import the project by selecting the `project.godot` file
3. Open the main scene (typically `scenes/Main.tscn`)
4. Press **F5** or click the Play button

The project is set up with all necessary scenes and scripts for the KBTV radio station game.

## Development Workflow

KBTV uses gitflow for organized development. See [GITFLOW.md](GITFLOW.md) for detailed branching guidelines.

### Quick Gitflow Reference
- **Start feature**: `git flow feature start my-feature`
- **Finish feature**: `git flow feature finish my-feature`
- **Start bugfix**: `git flow bugfix start fix-issue`
- **Finish bugfix**: `git flow bugfix finish fix-issue`



## Input System

KBTV uses Godot's built-in input system with action mappings defined in the project settings.

### Configuration
- **Input Actions**: Defined in Project Settings > Input Map
- **Controller Support**: Gamepad inputs are automatically mapped

### Usage Guidelines
- Use `Input.is_action_pressed()`, `Input.is_action_just_pressed()` for input detection
- Input actions are defined in `project.godot` settings
- Controller/gamepad inputs work automatically

## Build Commands

### Godot Editor
- Open the project in Godot 4.x
- Use **Project > Export** to build for target platforms
- **Command-line build**:
  ```bash
  # Windows build
  godot --export "Windows Desktop" --output "builds/KBTV_Windows.exe"

  # Development build with debug symbols
  godot --export-debug "Windows Desktop" --output "builds/KBTV_Windows_Debug.exe"
  ```

### Export Presets
Configure export presets in **Project > Export** for different platforms (Windows, Linux, macOS, HTML5).

## UID (Unique Identifier) System

Godot uses UIDs to track script and resource references across scenes and files. These are stored in `.uid` files alongside each script.

### Handling UID Issues

**When you see UID warnings:**
```
WARNING: scene/resources/resource_format_text.cpp:444 - res://scenes/Main.tscn:14 - ext_resource, invalid UID
```

**Quick Fix:**
1. Delete all `.uid` files: `find . -name "*.uid" -delete`
2. Open Godot and let it regenerate UIDs automatically
3. Save all scenes to persist new UIDs

**Manual Fix (when Godot not available):**
1. Delete all `.uid` files to regenerate them
2. Update all scene files' ext_resource entries to use the new UIDs from the regenerated `.uid` files
3. Commit both `.uid` files and updated scene files

**Why UIDs Break:**
- Moving or renaming files changes their absolute paths
- UIDs are generated from file paths + content hashes
- File restructuring (like our migration) invalidates existing UIDs

**Prevention:**
- Always move files through Godot's FileSystem dock when possible
- If moving manually, regenerate UIDs afterward
- Commit `.uid` files along with their corresponding scripts

**UID File Management:**
- `.uid` files are auto-generated - never edit them manually
- Include them in version control for consistent references
- They ensure scene references remain valid across different machines

## Testing

### GoDotTest Testing Framework
KBTV uses **GoDotTest** for unit and integration testing. See [TESTING.md](docs/testing/TESTING.md) for detailed setup and patterns.

**Installation:**
NuGet packages are already configured in `KBTV.csproj`:
- Chickensoft.GoDotTest (v2.0.28)
- LightMock.Generator (v1.2.3)
- Chickensoft.GodotTestDriver (v3.0.0)

**Running Tests:**
- **Editor**: Run with `--run-tests` flag or use VS Code "Debug Tests" launch config
- **CLI**: `godot --run-tests --quit-on-finish`
- **With Coverage**: `godot --run-tests --coverage --quit-on-finish`

**Test Structure:**
```
tests/
├── unit/
│   ├── callers/
│   ├── screening/
│   └── core/
└── integration/
```

**Coverage Target:** 80% unit test coverage minimum

### AI Agent Testing Guidelines

When modifying code, AI agents MUST follow these testing guidelines:

#### Before Making Changes

1. **Run existing tests to establish a baseline:**
   ```bash
   godot --run-tests --quit-on-finish
   ```

2. **Note any pre-existing failures** - Document these at the start of your work

3. **Identify tests that cover the code being modified:**
   - Tests in the same directory as your change
   - Tests that exercise the changed code path
   - Integration tests for affected components

#### After Making Changes

1. **Build the project first:**
   ```bash
   dotnet build
   ```

2. **Run tests related to your changes:**
   ```bash
   godot --run-tests --quit-on-finish
   ```

3. **Evaluate test results:**
   - If tests fail, determine the cause:
     - **Bug in implementation**: Fix the implementation
     - **Test expectations are incorrect**: Update the test
     - **Test is outdated**: Update or document the issue
   - All tests related to your changes should pass
   - Document any intentional test skips or known issues

#### When to Add New Tests

| Change Type | Test Action |
|-------------|-------------|
| **New feature** | Add unit tests before or after implementation |
| **New method** | Add test covering the method's logic and edge cases |
| **Bug fix** | Add regression test that would have caught the bug |
| **Refactor** | Verify existing tests pass; add tests for new behavior |
| **UI component** | Add UI integration test for the component |

#### Coverage Requirement

- New code should maintain **>= 80%** coverage
- Check coverage with: `godot --run-tests --coverage --quit-on-finish`

#### Test File Locations

- Unit tests: `tests/unit/[domain]/[Component]Tests.cs`
- Integration tests: `tests/integration/[Feature]Tests.cs`

### Test Patterns

**Unit Test Pattern:**
```csharp
[Test]
public void MethodName_Condition_ExpectedResult()
{
    // Arrange
    var input = CreateTestInput();
    
    // Act
    var result = SystemUnderTest.Process(input);
    
    // Assert
    AssertThat(result.IsSuccess);
    AssertThat(result.Value == expectedValue);
}
```

**Integration Test Pattern:**
```csharp
[Test]
public void FullWorkflow_StartingFromState_CompletesSuccessfully()
{
    // Setup dependencies
    var repository = new CallerRepository();
    var controller = new ScreeningController();
    
    // Execute workflow
    var caller = CreateTestCaller();
    repository.AddCaller(caller);
    repository.StartScreening(caller);
    
    // Verify end state
    AssertThat(repository.IsScreening);
}
```

### CI/CD Enforcement

Tests must pass and meet coverage requirements:
- All tests must pass (failures block merges)
- Coverage must be >= 80% (failures block merges)

### Script Validation
Check GDScript syntax and basic validation:

```bash
# Basic syntax check
godot --check-only project.godot
```

### C# Compilation
For C# scripts, build the project after making changes to catch compilation errors:

- **In Godot Editor**: Project > Tools > C# > Build
- **Check Output**: Review the Output/MSBuild panel for errors
- **Frequency**: Run build after editing C# scripts to ensure they compile correctly before testing

**Important**: C# scripts must be built before running the game. Unbuilt changes may cause runtime errors or crashes.

## Code Style Guidelines

### Naming Conventions
- **Classes/Scripts**: PascalCase (e.g., `PlayerController`, `EnemyAI`)
- **Functions**: snake_case (e.g., `move_player()`, `calculate_damage()`)
- **Variables**: snake_case (e.g., `player_health`, `movement_speed`)
- **Constants**: ALL_CAPS (e.g., `MAX_HEALTH = 100`)
- **Signals**: snake_case (e.g., `player_died`, `health_changed`)

### GDScript Patterns
```gdscript
extends Node2D

@export var speed: float = 5.0
@export var jump_force: float = 10.0

@onready var sprite = $Sprite2D

func _ready():
    # Initialize when node enters scene tree
    pass

func _process(delta):
    # Handle input and updates
    handle_movement(delta)

func handle_movement(delta):
    # Movement logic here
    pass

func take_damage(amount: float):
    # Damage handling
    pass
```

### File Organization
- **Scripts**: `scripts/[Domain]/[Component].gd`
- **Scenes**: `scenes/[SceneName].tscn`
- **Resources**: `assets/[Type]/[ResourceName].tres`

### Node Architecture
- Use Godot's node system with proper inheritance
- Implement `_ready()`, `_process()`, `_physics_process()` for lifecycle
- Use signals for communication between nodes
- Group related functionality in custom node types

### Error Handling
- Use `push_error()` for critical errors
- Use `push_warning()` for warnings
- Validate node references before use
- Use assertions for debugging

### Performance
- Cache node references in `_ready()`
- Use Godot's built-in pooling for frequently instantiated scenes
- Minimize use of `get_node()` in `_process()` loops
- Use Godot's profiler for optimization

### UI Initialization Timing (Deferred Calls)
When dynamically instantiating scenes and setting properties immediately after, `_ready()` may not have run yet. Use `CallDeferred()` to safely schedule UI updates:

```csharp
public void SetData(MyData data)
{
    _data = data;
    // Defer UI updates until after _ready() completes
    CallDeferred(nameof(_ApplyDataDeferred), data.Name);
}

private void _ApplyDataDeferred(string name)
{
    if (_nameLabel != null)
    {
        _nameLabel.Text = name;
    }
}
```

See [UI_IMPLEMENTATION.md](docs/ui/UI_IMPLEMENTATION.md) - Pattern 4 for full documentation.

### Git & Collaboration
- Create descriptive commit messages explaining the "why"
- Reference ticket/issue numbers in commits
- Use gitflow branching: `feature/`, `bugfix/`, `hotfix/`, `release/`
- Follow conventional commits: `feat:`, `fix:`, `docs:`, etc.
- The `.meta` file stores import settings (texture compression, script execution order, etc.)


