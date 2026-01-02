# Contributing to LlamaBrain

Thank you for your interest in contributing to LlamaBrain! This document provides guidelines and instructions for contributing to the project.

## 🚀 Getting Started

### Prerequisites

**For Core Library Development:**
- **.NET SDK**: .NET Standard 2.1 or higher (compatible with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+)
- **IDE**: Visual Studio, Visual Studio Code, or Rider (recommended)
- **Testing**: NUnit test framework (included via project dependencies)

**For Unity Runtime Development:**
- **Unity**: Unity 2022.3 LTS or higher
- **Unity Test Framework**: Included via Unity Test Runner
- **Core Library**: Must build the core library first (see Unity Development section)

### Unity Dependencies

**Required Unity Packages:**

1. **UniTask** (via Unity Package Manager - Git URL)
   - **Purpose**: High-performance async/await library for Unity
   - **Installation**: Package Manager → Add package from git URL → `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
   - **Usage**: Used throughout LlamaBrain for async operations (`SendPlayerInputAsync`, `SendWithSnapshotAsync`, etc.)
   - **License**: MIT License

2. **TextMeshPro (TMP)** (via Unity Package Manager)
   - **Purpose**: Advanced text rendering for UI components
   - **Installation**: Package Manager → Search "TextMeshPro" → Install
   - **Usage**: All dialogue UI components, RedRoom overlays, and debug displays
   - **License**: Unity package (distributed by Unity)

3. **SaveGameFree** (manual import - UnityPackage)
   - **Purpose**: Game state persistence (required for Feature 16: Save/Load Game Integration)
   - **Installation**: Download from [GitHub Releases](https://github.com/BayatGames/SaveGameFree/releases) → Import `.unitypackage`
   - **Version**: 2.5.0+ (use GitHub version, not Asset Store version)
   - **Usage**: Persisting game state, NPC memory, and InteractionCount across sessions
   - **License**: MIT License (GitHub version)

4. **Starter Assets - Third Person** (via Unity Asset Store)
   - **Purpose**: Character controller for NPC movement and interaction
   - **Installation**: Download from [Unity Asset Store](https://assetstore.unity.com/packages/essentials/starter-assets-thirdperson-updates-in-new-charactercontroller-pa-196526)
   - **Usage**: NPC movement, character controller functionality, and player interaction mechanics
   - **License**: Standard Asset Store EULA

**Note**: See [THIRD_PARTY_PACKAGES.md](LlamaBrainRuntime/Assets/LlamaBrainRuntime/Documentation/THIRD_PARTY_PACKAGES.md) for detailed installation instructions and usage information.

### Building the Project

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd LlamaBrain
   ```

2. **Restore dependencies**
   ```bash
   cd LlamaBrain
   dotnet restore
   ```

3. **Build the project**
   ```bash
   dotnet build
   ```

   Or build in Release mode:
   ```bash
   dotnet build -c Release
   ```

### Project Structure

```
LlamaBrain/
├── LlamaBrain/              # Core .NET library (netstandard2.1)
│   ├── Source/              # Source code
│   │   ├── Core/           # API client, server management, inference pipeline
│   │   ├── Persona/        # Character and memory system
│   │   └── Utilities/      # Helper utilities
│   └── LlamaBrain.csproj   # Main project file
├── LlamaBrain.Tests/        # Core library test project (NUnit)
│   └── LlamaBrain.Tests.csproj
├── LlamaBrainRuntime/       # Unity package
│   └── Assets/LlamaBrainRuntime/
│       ├── Runtime/        # Unity runtime scripts
│       ├── Editor/         # Unity editor scripts
│       ├── Samples/        # Example scenes and assets
│       └── Tests/          # Unity Test Runner tests
│           ├── EditMode/   # Editor-time tests
│           └── PlayMode/   # Runtime tests
└── Documentation/           # Comprehensive documentation
```

## 🧪 Testing

LlamaBrain has a comprehensive test suite with **92.37% code coverage** and **1,531 passing tests**. All contributions should maintain or improve this coverage.

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests in a specific project
cd LlamaBrain.Tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal

# Run a specific test class or method
dotnet test --filter "FullyQualifiedName~ContextRetrievalLayerTests"
```

### Code Coverage

```bash
# Run tests with coverage collection
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Generate coverage report (if analyze-coverage.ps1 exists)
.\analyze-coverage.ps1
```

### Test Infrastructure

- **Test Framework**: NUnit
- **Mocking**: NSubstitute
- **Coverage Tool**: Coverlet
- **Coverage Reports**: PowerShell script for detailed analysis

### Test Requirements

- ✅ All new features must include unit tests
- ✅ Bug fixes should include regression tests
- ✅ Tests must pass before submitting a PR
- ✅ Maintain or improve code coverage (currently 88.96%)
- ✅ Follow existing test patterns and naming conventions
- ✅ Integration tests required for pipeline changes
- ✅ Unity PlayMode tests for Unity runtime changes

## 🎮 Unity Development

### Unity Prerequisites

- **Unity Version**: Unity 2022.3 LTS or higher
- **Unity Test Framework**: Included via Unity Test Runner (EditMode and PlayMode tests)
- **Assembly Definitions**: Unity uses `.asmdef` files for assembly management
- **ScriptableObjects**: Used extensively for configuration (BrainSettings, PersonaConfig, ExpectancyRuleAsset, etc.)

### Setting Up Unity Development

1. **Open Unity Project**
   ```bash
   # Open the LlamaBrainRuntime folder as a Unity project
   # In Unity Hub: Add > Select LlamaBrainRuntime folder
   ```

2. **Verify Core Library Reference**
   - The Unity package references the core `LlamaBrain.dll` (built from `LlamaBrain/`)
   - Ensure the core library is built before testing Unity components
   ```bash
   cd LlamaBrain
   dotnet build -c Release
   ```

3. **Unity Package Structure**
   ```
   LlamaBrainRuntime/Assets/LlamaBrainRuntime/
   ├── Runtime/              # Runtime scripts (MonoBehaviours, ScriptableObjects)
   │   ├── Core/            # Core Unity components (BrainServer, LlamaBrainAgent)
   │   ├── Demo/            # Demo components and UI
   │   └── RedRoom/         # In-game LLM testing suite
   ├── Editor/              # Editor scripts and custom inspectors
   ├── Samples/             # Example scenes and assets
   └── Tests/               # Unity Test Runner tests
       ├── EditMode/       # Editor-time tests
       └── PlayMode/       # Runtime tests
   ```

### Unity Testing

LlamaBrain includes comprehensive Unity tests using Unity Test Runner:

#### Running Unity Tests

1. **Via Unity Test Runner Window**
   - Open: `Window > General > Test Runner`
   - Select `EditMode` or `PlayMode` tab
   - Click `Run All` or run specific test classes

2. **Via Command Line** (for CI/CD)
   ```bash
   # Run EditMode tests
   Unity -runTests -batchmode -projectPath ./LlamaBrainRuntime -testResults ./test-results.xml -testPlatform EditMode

   # Run PlayMode tests
   Unity -runTests -batchmode -projectPath ./LlamaBrainRuntime -testResults ./test-results.xml -testPlatform PlayMode
   ```

#### Test Organization

- **EditMode Tests** (`Tests/EditMode/`): Test ScriptableObjects, configuration, and editor-time functionality
  - `BrainSettingsTests.cs`: Configuration validation
  - No runtime dependencies required

- **PlayMode Tests** (`Tests/PlayMode/`): Test runtime components and full pipeline
  - `BrainAgentIntegrationTests.cs`: LlamaBrainAgent component tests
  - `BrainServerTests.cs`: Server management tests
  - `FullPipelinePlayModeTests.cs`: Complete 9-component pipeline tests
  - `MemoryMutationPlayModeTests.cs`: Memory mutation system tests
  - `FewShotAndFallbackPlayModeTests.cs`: Fallback system tests

#### Unity Test Requirements

- ✅ All new Unity components must include Unity tests
- ✅ ScriptableObject assets should have EditMode tests
- ✅ MonoBehaviour components should have PlayMode tests
- ✅ Tests must pass in both EditMode and PlayMode (as appropriate)
- ✅ Use Unity Test Runner assertions (`Assert.AreEqual`, `Assert.IsNotNull`, etc.)

### Unity Code Organization

#### Runtime Components

- **MonoBehaviours**: Place in `Runtime/Core/` or appropriate subdirectory
  - Examples: `BrainServer.cs`, `LlamaBrainAgent.cs`
  - Follow Unity naming: PascalCase class names matching file names

- **ScriptableObjects**: Place in `Runtime/Core/` or configuration-specific folders
  - Examples: `BrainSettings.cs`, `PersonaConfig.cs`, `ExpectancyRuleAsset.cs`
  - Use `[CreateAssetMenu]` attribute for Unity menu creation

- **Editor Scripts**: Place in `Editor/` folder
  - Custom inspectors, property drawers, menu items
  - Use `[CustomEditor]` and `[CustomPropertyDrawer]` attributes

#### Unity-Specific Guidelines

- **Serialization**: Use `[SerializeField]` for private fields exposed in Inspector
- **Inspector Organization**: Use `[Header]`, `[Tooltip]`, and `[Range]` attributes
- **Assembly Definitions**: Maintain `.asmdef` files for proper assembly isolation
- **ScriptableObject Patterns**: Follow existing patterns for configuration assets
- **MonoBehaviour Lifecycle**: Respect Unity's lifecycle (Awake, Start, OnDestroy, etc.)
- **Coroutines**: Use for async operations that need Unity-specific timing
- **Events**: Use UnityEvents or C# events for component communication

### Unity Contribution Workflow

1. **Build Core Library First**
   ```bash
   cd LlamaBrain
   dotnet build -c Release
   ```

2. **Create Unity Branch**
   ```bash
   git checkout -b unity/your-feature-name
   ```

3. **Make Unity Changes**
   - Add/Modify MonoBehaviour components
   - Create/Update ScriptableObject assets
   - Add Editor tools if needed
   - Update Unity samples/scenes

4. **Test in Unity**
   - Run EditMode tests for configuration changes
   - Run PlayMode tests for runtime components
   - Test in sample scenes
   - Verify Inspector behavior

5. **Update Unity Documentation**
   - Update [Unity Runtime README](LlamaBrainRuntime/Assets/LlamaBrainRuntime/README.md)
   - Update [Unity USAGE_GUIDE](LlamaBrainRuntime/Assets/LlamaBrainRuntime/Documentation/USAGE_GUIDE.md)
   - Add/Update sample scenes if adding new features

### Unity-Specific Areas for Contribution

- **RedRoom Testing Suite**: In-game LLM testing framework improvements
- **UI Components**: Enhanced dialogue interfaces and interaction systems
- **Editor Tools**: Custom inspectors, property drawers, and workflow improvements
- **Sample Scenes**: Additional examples demonstrating features
- **Unity Integration**: Better integration with Unity systems (Animation, Audio, etc.)
- **Performance**: Unity-specific optimizations and profiling

## 📝 Code Style & Standards

### General Guidelines

- **C# Standards**: Follow standard C# conventions and best practices
- **Nullable Reference Types**: The project uses `nullable enable` - handle nullability appropriately
- **Documentation**: Add XML documentation comments for public APIs
- **Naming**: Use clear, descriptive names following C# conventions
- **Error Handling**: Use appropriate exception types and include meaningful error messages

### Code Formatting

LlamaBrain uses standard C# formatting. Before committing:

1. **Run dotnet format**:
   ```bash
   dotnet format LlamaBrain/LlamaBrain.sln
   ```

2. **IDE Settings**: Use default C# formatting in your IDE:
   - Visual Studio: Default C# formatting
   - VS Code: C# extension with default settings
   - Rider: Default formatting

3. **XML Documentation**: All public APIs must have XML documentation:
   ```csharp
   /// <summary>
   /// Validates LLM output against constraints and canonical facts.
   /// </summary>
   /// <param name="output">The parsed LLM output to validate</param>
   /// <returns>Validation result with pass/fail status</returns>
   public ValidationResult Validate(ParsedOutput output) { ... }
   ```

4. **Code Style Checks**: The project enforces:
   - Consistent indentation (spaces, not tabs)
   - Proper brace placement
   - Naming conventions (PascalCase for public, camelCase for private)
   - No unused using statements

### Architecture Principles

LlamaBrain follows a **9-component architectural pattern** for deterministic state management:

1. **Stateless LLM**: The LLM is a pure generator with no memory access
2. **Deterministic State**: All context captured in immutable StateSnapshots
3. **Bounded Context**: EphemeralWorkingMemory ensures token-efficient prompts
4. **Validation Gate**: All outputs validated before state changes
5. **Controlled Mutation**: Only validated outputs can mutate memory
6. **Authority Enforcement**: Canonical facts cannot be overridden

When contributing, ensure your changes align with these principles. See [ARCHITECTURE.md](Documentation/ARCHITECTURE.md) for detailed information.

### Code Organization

- **Core Components**: Place in `LlamaBrain/Source/Core/`
- **Persona/Memory**: Place in `LlamaBrain/Source/Persona/`
- **Utilities**: Place in `LlamaBrain/Source/Utilities/`
- **Tests**: Mirror source structure in `LlamaBrain.Tests/`

## 🌿 Branching Strategy

LlamaBrain uses a simple branching model:

- **`main`**: Stable, release-ready code. All code must be merged via Pull Request.
- **`feature/name`**: New features and enhancements
- **`fix/name`**: Bug fixes
- **`docs/name`**: Documentation-only changes
- **`test/name`**: Test additions or improvements

### Branch Rules

- **No direct commits to `main`**: All changes must go through Pull Requests
- **Keep branches focused**: One feature/fix per branch
- **Update from main regularly**: Rebase or merge `main` into your branch to stay current
- **Delete branches after merge**: Clean up merged branches to keep the repo tidy

## 🔄 Development Workflow

### 1. Create a Branch

```bash
git checkout -b feature/your-feature-name
# or
git checkout -b fix/your-bug-fix
# or
git checkout -b docs/update-readme
```

### 2. Make Your Changes

- Write clean, well-documented code
- Add tests for new functionality
- Update documentation if needed
- Ensure all tests pass

### 3. Commit Your Changes

Use clear, descriptive commit messages following conventional commit format:

**Commit Message Format:**
```
type(scope): description

[optional body]

[optional footer]
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `test`: Test additions or changes
- `refactor`: Code refactoring (no behavior change)
- `chore`: Maintenance tasks (dependencies, build, etc.)

**Examples:**
```bash
git commit -m "feat(core): Add structured output support"
git commit -m "fix(validation): Correct constraint escalation logic"
git commit -m "docs(readme): Update Unity version requirements"
git commit -m "test(inference): Add snapshot determinism tests"
git commit -m "refactor(memory): Simplify memory retrieval logic"
git commit -m "chore(deps): Update Newtonsoft.Json to 13.0.4"
```

**Best Practices:**
- Use present tense ("Add" not "Added")
- Keep first line under 72 characters
- Reference issues: `fix(validation): Correct constraint escalation (#123)`
- Be specific: "Add validation" not "Update code"

### 4. Run Tests

```bash
# Ensure all tests pass
dotnet test

# Check code coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### 5. Update Documentation

If your changes affect:
- **API**: Update XML documentation comments
- **Architecture**: Update [ARCHITECTURE.md](Documentation/ARCHITECTURE.md)
- **Usage**: Update [USAGE_GUIDE.md](Documentation/USAGE_GUIDE.md)
- **Features**: Update [STATUS.md](Documentation/STATUS.md) or [ROADMAP.md](Documentation/ROADMAP.md)

### 6. Submit a Pull Request

1. Push your branch to the repository
2. Create a Pull Request with:
   - Clear description of changes
   - Reference to related issues (if any)
   - Test results and coverage information
   - Any breaking changes or migration notes

## 🐛 Reporting Issues

When reporting bugs or requesting features:

1. **Check Existing Issues**: Search for similar issues first
2. **Use Clear Titles**: Descriptive titles help others find and understand issues
3. **Provide Context**: Include:
   - Platform and .NET version
   - Steps to reproduce
   - Expected vs. actual behavior
   - Error messages and logs
   - Configuration details

## 📚 Documentation

LlamaBrain has extensive documentation. When contributing:

- **Code Comments**: Add XML documentation for public APIs
- **Architecture Docs**: Update [ARCHITECTURE.md](Documentation/ARCHITECTURE.md) for architectural changes
- **Usage Guides**: Update [USAGE_GUIDE.md](Documentation/USAGE_GUIDE.md) for new features
- **Status Tracking**: Update [STATUS.md](Documentation/STATUS.md) for feature completion

## 🎯 Areas for Contribution

### High Priority

- **Feature 12 & 13: Structured Output (JSON)**: Replace regex parser with LLM-native structured output
- **Feature 16: Save/Load Game Integration**: Game state persistence system

### Core Library

- Additional integration tests
- Performance benchmarks
- Edge case coverage
- API improvements

### Unity Runtime

- **RedRoom Testing Suite**: Enhancements to in-game LLM testing framework
- **UI Components**: Improved dialogue interfaces and interaction systems
- **Editor Tools**: Custom inspectors, property drawers, and workflow improvements
- **Sample Scenes**: Additional examples demonstrating features
- **Unity Integration**: Better integration with Unity systems (Animation, Audio, Input, etc.)
- **Performance**: Unity-specific optimizations and profiling
- **Unity Tests**: Additional EditMode and PlayMode tests

### Documentation

- Tutorial content and examples
- API reference improvements
- Integration guides for other engines (Unreal, Godot)
- Unity-specific tutorials and guides

See [ROADMAP.md](Documentation/ROADMAP.md) and [STATUS.md](Documentation/STATUS.md) for detailed feature status and priorities.

## 🔒 Security

LlamaBrain implements comprehensive security measures. When contributing:

- **Review [SAFEGUARDS.md](Documentation/SAFEGUARDS.md)**: Understand security requirements
- **Input Validation**: Always validate and sanitize inputs
- **Path Security**: Use `PathUtils` for file operations
- **Rate Limiting**: Respect rate limits in API clients

### Security Reporting Path

**DO NOT create public GitHub issues for security vulnerabilities.**

Security issues must be reported privately:

1. **Preferred Method**: [GitHub Security Advisory](https://github.com/michael-tiller/llamabrain/security/advisories)
   - Go to the Security tab
   - Click "Report a vulnerability"
   - Fill out the security advisory form

2. **Alternative Methods**:
   - **Email**: [contact@michaeltiller.com](mailto:contact@michaeltiller.com)
   - **Discord**: Contact maintainers privately on the [LlamaBrain Discord](https://discord.gg/9ruBad4nrN)

3. **Response Timeline**:
   - Initial response: Within 48 hours
   - Status update: Within 7 days
   - Resolution: Depends on severity

See [SECURITY.md](SECURITY.md) for detailed security reporting procedures.

### No Secrets in Issues

**Never paste secrets, API keys, tokens, or credentials in GitHub issues or PRs.**

- Use placeholder values: `[REDACTED]` or `your-api-key-here`
- If you accidentally commit secrets:
  1. Rotate the secret immediately
  2. Remove from git history (if possible)
  3. Contact maintainers privately
- Issues containing secrets may be automatically closed
- Use environment variables or configuration files (not committed) for secrets

## 📄 License

By contributing to LlamaBrain, you agree that your contributions will be licensed under the MIT License. See [LICENSE.md](LICENSE.md) for details.

## 🆘 Getting Help

- **Documentation**: Check the [Documentation](Documentation/) folder
- **Discord**: Join the [LlamaBrain Discord](https://discord.gg/9ruBad4nrN) for discussions
- **Issues**: Open an issue for bugs or feature requests

## 🙏 Thank You!

Your contributions help make LlamaBrain better for everyone. We appreciate your time and effort!
