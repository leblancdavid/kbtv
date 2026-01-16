# Testing Guide

## Overview

KBTV uses **GdUnit4** as its primary testing framework for unit and integration tests. This guide covers setting up tests, writing test patterns, and running the test suite.

## Installing GdUnit4

1. Open Godot 4.x
2. Go to **AssetLib** (or **Editor > Manage Export Templates**)
3. Search for "GdUnit4"
4. Click **Download** and then **Import**
5. Restart Godot if prompted

Alternatively, you can clone the repository:
```bash
git clone https://github.com/godot-gdunit-labs/gdunit4.git
```

## Test Directory Structure

```
tests/
├── unit/
│   ├── callers/
│   │   ├── CallerRepositoryTests.cs
│   │   └── CallerTests.cs
│   ├── screening/
│   │   └── ScreeningControllerTests.cs
│   ├── ui/
│   │   ├── ScreeningPanelTests.cs
│   │   └── ReactiveListPanelTests.cs
│   └── core/
│       ├── ServiceRegistryTests.cs
│       ├── EventAggregatorTests.cs
│       └── ResultTests.cs
├── integration/
│   └── CallerFlowIntegrationTests.cs
└── fixtures/
    ├── scenes/
    │   └── ScreeningPanel.tscn
    └── CallerFixture.cs
```

## Running Tests

### From Editor
1. Open Godot Editor
2. Go to **GdUnit4** menu (in top toolbar)
3. Click **Run Tests** or press `F6`

### From Command Line
```bash
godot --script addons/gdUnit4/bin/GdUnit4Cmd.gd --quit
```

### Running Specific Tests
```bash
# Run a single test file
godot --script addons/gdUnit4/bin/GdUnit4Cmd.gd --test="tests/unit/callers/CallerRepositoryTests.cs"

# Run tests matching a pattern
godot --script addons/gdUnit4/bin/GdUnit4Cmd.gd --filter="AddCaller"
```

## Writing Tests

### Basic Test Structure

```csharp
using Godot;
using KBTV.Callers;
using KBTV.Core;

[Test]
public void Repository_AddCaller_NotifiesObservers()
{
    // Arrange
    var repository = new CallerRepository();
    var observer = new TestCallerObserver();
    repository.Subscribe(observer);
    var caller = CreateTestCaller();
    
    // Act
    var result = repository.AddCaller(caller);
    
    // Assert
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(observer.AddedCallers, Contains.Item(caller));
}

private Caller CreateTestCaller()
{
    return new Caller(
        "Test Caller",
        "555-0123",
        "Test Location",
        "Ghosts",
        "Ghosts",
        "Test Reason",
        CallerLegitimacy.Credible,
        CallerPhoneQuality.Good,
        CallerEmotionalState.Calm,
        CallerCurseRisk.Low,
        CallerBeliefLevel.Curious,
        CallerEvidenceLevel.None,
        CallerCoherence.Coherent,
        CallerUrgency.Low,
        "nervous_hiker",
        "test_arc",
        "Test summary",
        30f,
        0.8f
    );
}
```

### Testing with Godot Nodes

```csharp
[Test]
public void ScreeningPanel_SetCaller_UpdatesInfoLabel()
{
    // Arrange
    var panel = new ScreeningPanel();
    var caller = CreateTestCaller();
    
    // Act
    panel.SetCaller(caller);
    
    // Assert
    Assert.That(panel.GetCallerInfoText(), Does.Contain(caller.Name));
    Assert.That(panel.GetCallerInfoText(), Does.Contain(caller.Location));
}
```

### Using Fixtures

```csharp
[Test]
public void Caller_Patience_ExpiresAfterTimeout([Values(20f, 30f, 40f)] float patience)
{
    // Arrange
    var caller = CreateTestCallerWithPatience(patience);
    
    // Act
    bool disconnected = caller.UpdateWaitTime(patience + 1f);
    
    // Assert
    Assert.That(disconnected, Is.True);
    Assert.That(caller.State, Is.EqualTo(CallerState.Disconnected));
}
```

## Test Coverage Target

- **Unit Tests**: 80% coverage minimum
- **Required**: All new features must have corresponding tests
- **Integration Tests**: Critical paths must be covered

## Best Practices

1. **AAA Pattern**: Arrange, Act, Assert
2. **Isolated Tests**: Each test should be independent
3. **Descriptive Names**: `MethodName_Scenario_ExpectedBehavior`
4. **Fast Tests**: Tests should run in milliseconds
5. **No External Dependencies**: Mock or stub external services

## CI/CD Integration

Tests run automatically on:
- Pull requests to main/develop branches
- Commits to feature branches (optional)

Example GitHub Actions workflow:
```yaml
name: Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: barrel-db/godot-action@master
        with:
          godot-version: 4.5
      - name: Run Tests
        run: |
          godot --script addons/gdUnit4/bin/GdUnit4Cmd.gd --quit
```

## Mocking

For mocking dependencies, use the built-in patterns or create test doubles:

```csharp
public class MockCallerRepository : ICallerRepository
{
    public List<Caller> AddedCallers { get; } = new();
    
    public Result<Caller> AddCaller(Caller caller)
    {
        AddedCallers.Add(caller);
        return Result<Caller>.Ok(caller);
    }
    
    // Implement other interface members...
}
```
