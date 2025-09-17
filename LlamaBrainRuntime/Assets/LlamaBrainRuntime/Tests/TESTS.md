# LlamaBrain Tests

This directory contains tests for the LlamaBrain integration using Unity's Test Framework (UniTest).

## Test Structure

### EditMode Tests
Tests that run in the Unity Editor without entering Play Mode:
- **BrainSettingsTests.cs** - Tests for configuration validation
- **ProcessConfigTests.cs** - Tests for process configuration
- **ApiContractsTests.cs** - Tests for API request/response serialization
- **PersonaProfileTests.cs** - Tests for persona profile system
- **PromptComposerTests.cs** - Tests for prompt composition and formatting

### PlayMode Tests
Tests that run in Play Mode (requires actual server):
- **BrainServerTests.cs** - Tests for LlamaBrainServer component
- **ApiClientTests.cs** - Tests for HTTP client communication

## Running Tests

### In Unity Editor
1. Open the **Test Runner** window (Window > General > Test Runner)
2. Select **EditMode** or **PlayMode** tab
3. Click **Run All** or run individual tests

### From Command Line
```bash
# Run EditMode tests
Unity.exe -batchmode -quit -projectPath <project-path> -runTests -testPlatform EditMode

# Run PlayMode tests
Unity.exe -batchmode -quit -projectPath <project-path> -runTests -testPlatform PlayMode
```

## Test Categories

### Unit Tests (EditMode)
- Configuration validation
- Data structure tests
- Serialization tests
- No external dependencies

### Integration Tests (PlayMode)
- Component lifecycle tests
- Server integration tests (when server is available)
- HTTP communication tests

## Test Requirements

### For EditMode Tests
- No special requirements
- Run in Unity Editor

### For PlayMode Tests
- Requires llama-server.exe to be available
- Requires model file to be available
- Tests may fail if server is not running

## Adding New Tests

1. **EditMode Tests**: Place in `Tests/EditMode/` directory
2. **PlayMode Tests**: Place in `Tests/PlayMode/` directory
3. Use `[Test]` attribute for synchronous tests
4. Use `[UnityTest]` attribute for coroutine-based tests
5. Follow naming convention: `ClassName_MethodName_ExpectedBehavior`

## Test Naming Convention

- **Test Method Names**: `MethodName_Scenario_ExpectedResult`
- **Test Class Names**: `ClassNameTests`
- **Namespaces**: `LlamaBrain.Tests.EditMode` or `LlamaBrain.Tests.PlayMode`

## Example Test Structure

```csharp
[Test]
public void MethodName_WithValidInput_ReturnsExpectedResult()
{
    // Arrange
    var input = "test";
    
    // Act
    var result = MethodUnderTest(input);
    
    // Assert
    Assert.AreEqual("expected", result);
}
``` 