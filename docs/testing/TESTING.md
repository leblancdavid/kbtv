# Testing Guide

## Overview

KBTV uses **GoDotTest** as its primary testing framework for unit and integration tests. This guide covers setting up tests, writing test patterns, running the test suite, and collecting code coverage.

## Installation

### Godot Setup

**Godot Installation Path:** `D:\Software\Godot\Godot_v4.5.1-stable_mono_win64.exe`

Set the `GODOT` environment variable to run tests from command line:
```bash
set GODOT=D:\Software\Godot\Godot_v4.5.1-stable_mono_win64.exe
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
│   │   └── CallerRepositoryTests.cs
│   └── screening/
│       └── ScreeningControllerTests.cs
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
# Run all tests
godot --run-tests --quit-on-finish

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

# Run Godot tests
godot --run-tests --quit-on-finish
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
# Windows
report-tests.bat [godot_path]

# Linux/macOS
./report-tests.sh --godot [godot_path]
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

### GitHub Actions Example

```yaml
name: Tests

on: [push, pull_request]

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
        run: godot --run-tests --quit-on-finish

      - name: Coverage
        run: ./report-tests.sh --godot godot
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

## Test Categories

### Unit Tests
- `tests/unit/core/` - Core system tests
- `tests/unit/callers/` - Caller entity and repository tests
- `tests/unit/screening/` - Screening controller tests

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

## Test Results

| Test Suite | Passed | Failed | Total | Status |
|------------|--------|--------|-------|--------|
| ResultTests | 20 | 0 | 20 | ✓ All passing |
| ServiceRegistryIntegrationTests | 10 | 0 | 10 | ✓ All passing |
| CallerFlowIntegrationTests | 6 | 2 | 8 | 75% |
| CallerRepositoryTests | 29 | 2 | 31 | 94% |
| CallerTests | 31 | 4 | 35 | 89% |
| ServiceRegistryTests | 16 | 2 | 18 | 89% |
| EventAggregatorTests | 11 | 2 | 13 | 85% |
| ScreeningControllerTests | 12 | 3 | 15 | 80% |
| **Total** | **135** | **15** | **150** | **90%** |

### Known Failing Tests

The following tests have known issues and need fixes:

1. `ScreeningControllerTests::Approve_NoSession_ReturnsFailure` - Approve logic returns success instead of failure
2. `ScreeningControllerTests::Reject_NoSession_ReturnsFailure` - Reject logic returns success instead of failure
3. `ScreeningControllerTests::IsActive_AfterComplete_ReturnsFalse` - State transition issue
4. `EventAggregatorTests::Unsubscribe_TEvent_RemovesSpecificHandler` - Unsubscribe behavior
5. `EventAggregatorTests::Publish_AfterUnsubscribe_NoHandlerCalled` - Event publishing after unsubscribe
6. `ServiceRegistryTests::RegisterFactory_RegistersFactory` - Factory registration logic
7. `CallerRepositoryTests::ApproveScreening_FullHoldQueue_ReturnsFailure` - Hold queue limit check
8. `CallerTests::UpdateWaitTime_IncomingState_IncreasesWaitTime` - Wait time update logic
9. `CallerTests::CalculateShowImpact_*` - Impact calculation multipliers

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
