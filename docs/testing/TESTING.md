# Testing Guide

## Overview

KBTV uses **GoDotTest** as its primary testing framework for unit and integration tests. This guide covers setting up tests, writing test patterns, running the test suite, and collecting code coverage.

## Installation

### Godot Setup

**Godot Installation Path:** `C:\Program Files\Godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe`

Set the `GODOT` environment variable to run tests from command line:
```bash
set GODOT=C:\Program Files\Godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe
```

Or run tests directly:
```bash
"C:\Program Files\Godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe" --run-tests --quit-on-finish
```

### NuGet Packages

The following packages are configured in `KBTV.csproj`:

- **Chickensoft.GoDotTest** (v2.0.28) - Test runner for Godot
- **LightMock.Generator** (v1.2.3) - Compile-time mocking
- **Chickensoft.GodotTestDriver** (v3.0.0) - UI/integration test drivers
- **coverlet.collector** (v6.0.2) - Coverage collection

Packages are only included in non-Release builds to avoid test code in exported games.

## Test Directory Structure

```
tests/
├── unit/
│   ├── core/
│   │   ├── ResultTests.cs
│   │   ├── ServiceRegistryTests.cs
│   │   └── EventAggregatorTests.cs
│   ├── callers/
│   │   ├── CallerTests.cs
│   │   ├── CallerRepositoryTests.cs
│   │   └── CallerGeneratorTests.cs
│   ├── screening/
│   │   └── ScreeningControllerTests.cs
│   ├── managers/
│   │   ├── GameStateManagerTests.cs
│   │   ├── TimeManagerTests.cs
│   │   ├── ListenerManagerTests.cs
│   │   └── EconomyManagerTests.cs
│   ├── data/
│   │   ├── VernStatsTests.cs
│   │   ├── StatTests.cs
│   │   └── IncomeCalculatorTests.cs
│   ├── monitors/
│   │   ├── ScreeningMonitorTests.cs
│   │   └── CallerMonitorTests.cs
│   └── ui/
│       ├── ServiceAwareComponentTests.cs
│       ├── ButtonStylerTests.cs
│       ├── ReactiveListPanelTests.cs
│       ├── GlobalTransitionManagerTests.cs
│       ├── LoadingScreenTests.cs
│       └── AudioDialoguePlayerTests.cs
├── integration/
│   ├── ServiceRegistryIntegrationTests.cs
│   └── CallerFlowIntegrationTests.cs
└── test/
    ├── Tests.tscn          (test runner scene)
    └── Tests.cs            (test runner script)
```

## Running Tests

### From Godot Editor

1. Open Godot Editor
2. Press `F5` or click Play to run the game
3. Tests run automatically if `--run-tests` is passed

### From Command Line

```bash
# Run all tests (using GODOT env var)
godot --run-tests --quit-on-finish

# Run all tests (using full path)
"C:\Program Files\Godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe" --run-tests --quit-on-finish

# Run specific test suite
godot --run-tests=ResultTests --quit-on-finish

# Run single test method
godot --run-tests=ResultTests.Ok_CreatesSuccessfulResult --quit-on-finish

# Run with coverage (for report generation)
godot --run-tests --coverage --quit-on-finish
```

### From VS Code

**Launch configurations** (`.vscode/launch.json`):

| Configuration | Description |
|--------------|-------------|
| `Play` | Debug the game normally |
| `Debug Tests` | Run all tests in debugger |
| `Debug Current Test` | Run tests in current file |
| `Debug Tests with Coverage` | Run tests and collect coverage |

**Tasks** (`.vscode/tasks.json`):

| Task | Description |
|------|-------------|
| `build` | Build the project |
| `build-release` | Build release configuration |
| `test` | Run .NET tests |

### Build & Run Tests

```bash
# Build project
dotnet build

# Run Godot tests (using full path)
"C:\Program Files\Godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe" --run-tests --quit-on-finish
```

## Writing Tests

### Basic Test Structure

```csharp
using Chickensoft.GoDotTest;
using Godot;

namespace KBTV.Tests.Unit.Core
{
    [TestClass]
    public class ResultTests : TestClass
    {
        public ResultTests(Node testScene) : base(testScene) { }

        [Test]
        public void Ok_CreatesSuccessfulResult()
        {
            var result = Result<int>.Ok(42);
            AssertThat(result.IsSuccess);
            AssertThat(result.Value == 42);
        }
    }
}
```

### Test Lifecycle Attributes

| Attribute | Description |
|-----------|-------------|
| `[Setup]` | Run before each test |
| `[Cleanup]` | Run after each test |
| `[SetupAll]` | Run once before all tests |
| `[CleanupAll]` | Run once after all tests |
| `[Test]` | Test method |
| `[Failure]` | Run when any test fails |

### Assertions

GoDotTest provides `AssertThat()` with various conditions:

```csharp
AssertThat(condition);
AssertThat(value == expected);
AssertThat(list.Contains(item));
AssertThat(!result.IsFailure);
AssertThrows<InvalidOperationException>(() => { });
```

### Using Mocks

```csharp
using LightMock;
using LightMock.Generator;

public class MyServiceTests : TestClass
{
    private readonly IMyService _mockService;
    private readonly MockContext<IMyService> _context;

    public MyServiceTests(Node testScene) : base(testScene)
    {
        _context = new MockContext<IMyService>();
        _mockService = _context.Object;
    }

    [Test]
    public void Method_CallsDependency()
    {
        _context.Setup(m => m.DoSomething(It.IsAny<int>())).Returns(true);

        var result = _mockService.DoSomething(42);

        AssertThat(result);
        _context.Verify(m => m.DoSomething(42), Times.Once);
    }
}
```

### Integration Tests with UI

```csharp
using Chickensoft.GoDotTest;
using Chickensoft.GodotTestDriver;
using Godot;

public class ScreeningPanelTests : TestClass
{
    public ScreeningPanelTests(Node testScene) : base(testScene) { }

    [Test]
    public void Panel_DisplaysCallerInfo()
    {
        var driver = new ScreeningPanelDriver();
        var caller = CreateTestCaller();

        driver.SetCaller(caller);

        AssertThat(driver.InfoLabel.Text.Contains(caller.Name));
    }
}
```

## Development Workflow Requirements

### Before Making Changes

1. **Run existing tests to establish a baseline:**
   ```bash
   "C:\Program Files\Godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe" --run-tests --quit-on-finish
   ```

2. **Note any pre-existing failures** in your commit message or task notes

3. **Identify tests that cover the code being modified**:
   - Tests in the same directory as your change
   - Tests that exercise the changed code path
   - Integration tests for affected components

### After Making Changes

1. **Build the project first:**
   ```bash
   dotnet build
   ```

2. **Run tests related to your changes:**
   ```bash
   "C:\Program Files\Godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe" --run-tests --quit-on-finish
   ```

3. **Evaluate test results:**
   - If tests fail, determine the cause:
     - **Bug in implementation**: Fix the implementation
     - **Test expectations are incorrect**: Update the test
     - **Test is outdated**: Update or document the issue
   - All tests related to your changes should pass
   - Document any intentional test skips or known issues

### When to Add New Tests

| Change Type | Test Action |
|-------------|-------------|
| **New feature** | Add unit tests before or after implementation |
| **New method** | Add test covering the method's logic and edge cases |
| **Bug fix** | Add regression test to prevent future bugs |
| **Refactor** | Verify existing tests pass; add tests for new behavior |
| **UI component** | Add UI integration test for the component |
| **Data model** | Add tests for validation and transformations |

### Test Selection Guidelines

- **Unit tests** (`tests/unit/`): Test individual methods and classes in isolation
- **Integration tests** (`tests/integration/`): Test interactions between components
- **Related tests**: Tests in the same directory as your change, or tests that exercise the changed code path

### Coverage Requirement

- New code should maintain **>= 80%** coverage
- Check coverage with:
  ```bash
  "C:\Program Files\Godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe" --run-tests --coverage --quit-on-finish
  ```

## Test Maintenance

### Updating Tests

When modifying production code:

1. **Run existing tests** to identify failures:
   ```bash
   "C:\Program Files\Godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe" --run-tests --quit-on-finish
   ```

2. **Fix failing tests** that expose bugs in your changes

3. **Update tests** that reflect new expected behavior

4. **Add new tests** for new functionality

### Test Quality Guidelines

- **AAA Pattern**: Arrange, Act, Assert
- **Isolated Tests**: Each test should be independent
- **Descriptive Names**: `MethodName_Scenario_ExpectedResult`
- **Fast Tests**: Tests should complete in milliseconds
- **No External Dependencies**: Mock or stub external services

### Test File Naming Convention

```
tests/unit/[domain]/[Component]Tests.cs
tests/integration/[Feature]Tests.cs
```

Examples:
- `tests/unit/callers/CallerTests.cs`
- `tests/unit/core/ResultTests.cs`
- `tests/integration/CallerFlowIntegrationTests.cs`

### Test Templates

#### Unit Test Template

```csharp
using Chickensoft.GoDotTest;
using Godot;
using KBTV.[Domain];

namespace KBTV.Tests.Unit.[Domain]
{
    public class [Component]Tests : KBTVTestClass
    {
        public [Component]Tests(Node testScene) : base(testScene) { }

        [Setup]
        public void Setup()
        {
            // Common setup for all tests
        }

        [Test]
        public void [MethodName]_[Condition]_[ExpectedResult]()
        {
            // Arrange
            var input = CreateTestInput();

            // Act
            var result = SystemUnderTest.Process(input);

            // Assert
            AssertThat(result.IsSuccess);
            AssertThat(result.Value == expectedValue);
        }

        private [ReturnType] CreateTestInput()
        {
            // Helper method for creating test inputs
        }
    }
}
```

#### Integration Test Template

```csharp
using Chickensoft.GoDotTest;
using Godot;
using KBTV.[Domain1];
using KBTV.[Domain2];

namespace KBTV.Tests.Integration
{
    public class [Feature]IntegrationTests : KBTVTestClass
    {
        public [Feature]IntegrationTests(Node testScene) : base(testScene) { }

        private [Service1] _service1 = null!;
        private [Service2] _service2 = null!;

        [Setup]
        public void Setup()
        {
            _service1 = new [Service1]();
            _service2 = new [Service2]();
        }

        [Test]
        public void [Scenario]_[ExpectedBehavior]()
        {
            // Setup dependencies
            var input = CreateTestInput();

            // Execute workflow
            _service1.Process(input);
            var result = _service2.Complete();

            // Verify end state
            AssertThat(result.IsSuccess);
        }
    }
}
```

#### Mocked Service Test Template

```csharp
using Chickensoft.GoDotTest;
using LightMock;
using LightMock.Generator;
using Godot;
using KBTV.[Domain];

namespace KBTV.Tests.Unit.[Domain]
{
    public class [Component]WithMocksTests : KBTVTestClass
    {
        private readonly I[Dependency] _mockDependency;
        private readonly MockContext<I[Dependency]> _context;

        public [Component]WithMocksTests(Node testScene) : base(testScene)
        {
            _context = new MockContext<I[Dependency]>();
            _mockDependency = _context.Object;
        }

        [Test]
        public void [MethodName]_[Condition]_[ExpectedResult]()
        {
            // Arrange
            _context.Setup(m => m.[Method](It.IsAny<[ParamType]()))
                .Returns([expectedValue]);

            var systemUnderTest = new [Component](_mockDependency);

            // Act
            var result = systemUnderTest.[Method]([testValue]);

            // Assert
            AssertThat(result.[Property] == [expectedValue]);
            _context.Verify(m => m.[Method]([expectedParam]), Times.Once);
        }
    }
}
```

## Test Fixtures

### Creating Test Callers

```csharp
private Caller CreateTestCaller(
    string name = "Test Caller",
    CallerLegitimacy legitimacy = CallerLegitimacy.Credible,
    float patience = 30f)
{
    return new Caller(
        name,
        "555-0123",
        "Test Location",
        "Ghosts",
        "Ghosts",
        "Test Reason",
        legitimacy,
        CallerPhoneQuality.Good,
        CallerEmotionalState.Calm,
        CallerCurseRisk.Low,
        CallerBeliefLevel.Curious,
        CallerEvidenceLevel.None,
        CallerCoherence.Coherent,
        CallerUrgency.Low,
        "personality",
        "arc",
        "summary",
        patience,
        0.8f
    );
}
```

### Test Observers

```csharp
public class TestCallerRepositoryObserver : ICallerRepositoryObserver
{
    public List<Caller> AddedCallers = new();
    public List<Caller> RemovedCallers = new();

    public void OnCallerAdded(Caller caller) => AddedCallers.Add(caller);
    public void OnCallerRemoved(Caller caller) => RemovedCallers.Add(caller);
}
```

## Code Coverage

### Running with Coverage

```bash
# Windows (using full path)
"C:\Program Files\Godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe" --run-tests --coverage --quit-on-finish

# Or using report-tests.bat
report-tests.bat --godot "C:\Program Files\Godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe"

# Linux/macOS
./report-tests.sh --godot /path/to/godot
```

### Coverage Configuration

Coverage is configured to exclude:
- Test files (`test/**`, `tests/**`)
- Generated files (`Godot.SourceGenerators/**`)
- Microsoft test SDK files

### Coverage Thresholds

Minimum coverage target: **80%**

### Coverage Reports

Reports are generated in `coverage/` directory:
- `coverage.xml` - OpenCover format
- `coverage/report/index.html` - HTML report (requires reportgenerator)

## Best Practices

1. **AAA Pattern**: Arrange, Act, Assert
2. **Isolated Tests**: Each test should be independent
3. **Descriptive Names**: `MethodName_Scenario_ExpectedBehavior`
4. **Fast Tests**: Tests should run in milliseconds
5. **No External Dependencies**: Mock or stub external services
6. **Test Fixtures**: Use helper methods for common setup

## CI/CD Integration

### CI/CD Enforcement Rules

| Requirement | Enforcement |
|-------------|-------------|
| All tests must pass | Block merges on failure |
| Coverage >= 80% | Block merges if below threshold |
| Build must succeed | Block merges on error |

### GitHub Actions Example

```yaml
name: Tests

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Godot
        uses: barrel-db/godot-action@master
        with:
          godot-version: 4.5.1
          dotnet-version: 8.0

      - name: Build
        run: dotnet build

      - name: Run Tests
        run: |
          $godot = "C:\Program Files\Godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe"
          & $godot --run-tests --quit-on-finish

      - name: Coverage
        run: |
          $godot = "C:\Program Files\Godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe"
          ./report-tests.bat --godot $godot

      - name: Check Coverage
        run: |
          COVERAGE=$(grep -oP '(?<=coveredby=").*?(?=")' coverage/coverage.xml 2>/dev/null | head -1 || echo "0")
          if [ "$COVERAGE" -lt 80 ]; then
            echo "Coverage is $COVERAGE%, which is below 80% threshold"
            exit 1
          fi
          echo "Coverage: $COVERAGE% (meets 80% threshold)"
```

## Debugging Tests

### VS Code Debugging

1. Set breakpoints in test code
2. Select `Debug Tests` or `Debug Current Test` configuration
3. Press `F5` to start debugging

### Common Issues

**Tests not discovered:**
- Ensure test class extends `TestClass`
- Ensure test methods have `[Test]` attribute
- Check build output for errors

**NullReference in tests:**
- Tests run in Godot environment, not pure .NET
- Some services require ServiceRegistry initialization

**Coverage not collected:**
- Ensure `--coverage` flag is passed
- Check that coverlet is installed
- Verify DLL path is correct

### Test Categories

### Unit Tests
- `tests/unit/core/` - Core system tests (Result, ServiceRegistry, EventAggregator)
- `tests/unit/callers/` - Caller entity and repository tests
- `tests/unit/screening/` - Screening controller tests
- `tests/unit/managers/` - Game manager tests (GameState, Time, Listener, Economy)
- `tests/unit/data/` - Data component tests (VernStats, Stat, IncomeCalculator)
- `tests/unit/ui/` - UI component tests (GlobalTransitionManager, LoadingScreen)

### Integration Tests
- `tests/integration/` - Cross-component tests
- Full caller flow tests
- Service registry integration tests

## Mocking Reference

### LightMock.Generator Syntax

```csharp
// Setup
context.Setup(m => m.Method(It.IsAny<T>())).Returns(value);

// Verify
context.Verify(m => m.Method(It.IsAny<T>()), Times.Once);

// Capture
context.Setup(m => m.Method(It.IsAny<T>()))
       .Callback<T>(value => captured = value);
```

### Matchers

```csharp
It.IsAny<T>()           // Any value
It.Is<T>(x => x > 0)    // Condition
It.IsInRange(1, 10)     // Range
It.IsRegex("[a-z]+")    // Regex
```

## Performance

- Tests should complete in milliseconds
- Avoid waiting for real-time in tests
- Use deterministic random seeds when needed
- Clean up resources in `[Cleanup]`

## Current Test Status

**Last run:** January 2026
**Result: 396 passed, 10 failed** (~2.5% failure rate)

### Test Summary

| Category | Passed | Failed | Total | Coverage |
|----------|--------|--------|-------|----------|
| Unit Tests | ~370 | 10 | ~380 | 78% |
| Integration Tests | ~26 | 0 | ~26 | 65% |
| **Total** | **396** | **10** | **406** | **77%** |

### Known Failing Tests

| Test Suite | Failing Tests | Issue Type | Status |
|------------|---------------|------------|--------|
| ServiceRegistryIntegrationTests | 6 | Services not registered in test context | Fixed - null-safe assertions |
| ScreeningControllerEventsTests | 4 | Repository dependency in unit tests | Fixed - test expectations updated |

### Recent Test Improvements

- **Added ServiceAwareComponentTests** - Base class initialization pattern tests
- **Added ButtonStylerTests** - UI button styling helper tests
- **Added AudioDialoguePlayerTests** - Audio playback synchronization tests
- **Added ReactiveListPanelTests** - UI list management tests
- **Added ScreeningMonitorTests** - Domain monitor pattern tests
- **Added ConversationManagerTests** - Extended with VernDialogueTemplate tests
- **Added StatTests** - Comprehensive coverage of Stat class (value, normalize, events)
- **Added ArcRepositoryTests** - Dialogue arc repository tests
- **Added SaveDataTests** - Persistence data structure tests
- **Added SaveManagerTests** - Save/load functionality tests
- **Added IncomeCalculatorTests** - Economy calculation tests

### Notes

- Some tests have assertions that don't match current implementation behavior
- Tests requiring Godot editor to run (GoDotTest framework)
- New tests added for dialogue and persistence systems
- Coverage reporting configured via coverlet

### Test Maintenance Notes

When addressing test failures:

1. **For assertion mismatches**: Determine whether the test expectation or implementation is incorrect
2. **For missing data/setup**: Create necessary test fixtures or mock dependencies
3. **Document known issues**: Note any intentional test skips or known limitations

## Coverage Target

| Category | Target |
|----------|--------|
| Unit Tests | 80% |
| Integration Tests | 70% |
| **Overall** | **80%** |

## Resources

- [GoDotTest Documentation](https://github.com/chickensoft-games/GoDotTest)
- [LightMock.Generator](https://github.com/anton-yashin/LightMock.Generator)
- [GodotTestDriver](https://github.com/chickensoft-games/GodotTestDriver)
- [coverlet](https://github.com/coverlet-coverage/coverlet)
