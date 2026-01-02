# LlamaBrain Core Library 0.2.0-rc.1

**Deterministic State Management for Stochastic AI**

A robust, secure, and feature-rich .NET Standard 2.1 library for integrating with llama.cpp servers. I engineered an authoritative memory system that prevents LLM hallucinations from corrupting game state by enforcing a strict validation gate between the model's output and the runtime database.

## 🎯 Overview

LlamaBrain solves a fundamental problem in AI-powered applications: **how to maintain deterministic, authoritative game state when using stochastic LLMs**. The library implements a production-ready architecture that treats the LLM as a pure, stateless generator while maintaining continuity through deterministic state reconstruction.

**The Core Innovation**: An authoritative memory system with a strict validation gate ensures that LLM outputs cannot corrupt game state. All outputs are validated against constraints and canonical facts before any memory mutations occur, preventing hallucinations from affecting your game's world state.

Key features include:

- **Secure API Client**: Rate-limited, validated HTTP communication with dependency injection support for testing
- **Persona Management**: Persistent character profiles with rich memory systems
- **Determinism Layer**: Expectancy engine that enforces constraint-based behavior control
- **Structured Memory System**: Authoritative memory hierarchy with canonical facts, world state, episodic memory, and beliefs
- **State Snapshots**: Immutable context capture with intelligent retrieval and automatic retry logic
- **Bounded Prompts**: Token-efficient prompt assembly using ephemeral working memory
- **Output Validation**: Multi-layer validation gate that checks outputs against constraints and canonical facts
- **Controlled Mutations**: Authority-enforced memory updates with world intent dispatching
- **Fallback System**: Context-aware fallback responses when inference fails after retries
- **Process Management**: Safe server lifecycle management with monitoring and graceful shutdown
- **Comprehensive Testing**: 92.37% code coverage with 1,531 passing tests

**Available Now (v0.2.0):**
- ✅ **Structured Output (JSON)**: Native llama.cpp JSON schema support with automatic parsing (Feature 12 - Complete)
  - `JsonSchemaBuilder` for dynamic schema generation
  - `ParseStructured()` and `ParseAuto()` methods for JSON parsing
  - Regex fallback maintained for backward compatibility
  - 56 comprehensive tests

**Coming in v0.3.0:**
- **Structured Output Integration**: Complete pipeline integration with structured outputs throughout validation and mutation systems (Feature 13)
- **Save/Load Game Integration**: Game state persistence system for preserving deterministic state across sessions, enabling cross-session determinism (Feature 16)
- **Deterministic Generation Seed**: Cross-session determinism through InteractionCount-based seeding (Feature 14)

**⚠️ Breaking Changes Notice (v0.3.0):**

v0.2.x uses **regex-based parsing** as the default for extracting dialogue, mutations, and world intents from LLM output. v0.2.0 now includes **native Structured Output** (JSON schema mode) as an option, and v0.3.0 will complete the integration throughout the pipeline.

- Custom `OutputParser` implementations may need updates for structured output support
- Validation rules that rely on regex patterns may need adjustment
- The `ParsedOutput` structure remains compatible, but parsing internals now support both regex and structured JSON

**Migration Path**: v0.2.0 → v0.3.0 includes structured output with automatic regex fallback (backward compatible). See `ROADMAP.md` for detailed migration guide.

## 🏗️ Architecture

LlamaBrain implements a nine-component architectural pattern that ensures deterministic, controlled AI behavior. The complete flow is illustrated in the architectural diagram below:

![Architectural Diagram](../Documentation/architectural_diagram.png)

*The "Continuity Emerges from Deterministic State Reconstruction Around a Stateless Generator" pattern*

### Core Components

#### API Client (`ApiClient.cs`)
- HTTP client for llama.cpp server communication
- Rate limiting (60 requests/minute)
- Input validation and sanitization
- Comprehensive error handling
- Request/response size limits
- Timeout management (30 seconds)

#### Persona System
- **PersonaProfile**: Character definitions with traits dictionary and behaviors
- **PersonaMemoryStore**: In-memory storage system for conversations
- **PersonaMemoryFileStore**: File-based persistent storage system
- **PersonaProfileManager**: File-based profile management with safeguards

#### Conversation Management
- **DialogueSession**: Structured conversation tracking with timestamps
- **PromptComposer**: Intelligent prompt building from personas and context
- **BrainAgent**: High-level interface for persona interactions

#### Process Management
- **ServerManager**: Safe llama.cpp server process control
- **ProcessConfig**: Configuration for server startup
- **LlmConfig**: LLM generation parameters

#### Determinism Layer
- **ExpectancyEvaluator**: Engine-agnostic rule evaluation system that generates constraints based on interaction context
- **Constraint System**: Permission, Prohibition, and Requirement constraints with configurable severity levels (Soft, Hard, Critical)
- **InteractionContext**: Context-aware rule evaluation supporting trigger reasons, NPC IDs, scene names, and custom tags
- **IExpectancyRule**: Extensible interface for creating custom behavior constraint rules

#### Structured Memory System
- **AuthoritativeMemorySystem**: Authority-based memory management that enforces strict boundaries between memory types
- **Memory Types**: Four distinct memory types with clear authority hierarchy:
  - `CanonicalFact`: Immutable world truths that cannot be modified by AI
  - `WorldState`: Mutable game state that can be updated by game systems
  - `EpisodicMemory`: Conversation history with automatic decay and significance-based retention
  - `BeliefMemory`: NPC opinions and relationships that can be wrong or contradicted
- **MemoryAuthority**: Enforces hierarchy (Canonical > WorldState > Episodic > Belief) to prevent unauthorized modifications
- **Memory Decay**: Automatic episodic memory decay with configurable significance-based retention rates

#### State Snapshot & Context Retrieval
- **StateSnapshot**: Immutable snapshot capturing all context at inference time for deterministic retries
- **ContextRetrievalLayer**: Intelligent context retrieval using recency, relevance, and significance scoring algorithms
- **InferenceResult**: Comprehensive result tracking with validation outcomes, token usage, and retry information
- **RetryPolicy**: Configurable retry behavior with automatic constraint escalation on failures
- **ResponseValidator**: Validates responses against constraint sets before memory mutation

#### Ephemeral Working Memory
- **EphemeralWorkingMemory**: Bounded working memory that exists only for a single inference, then discarded
- **PromptAssembler**: Token-efficient prompt assembly with configurable limits and format customization
- **WorkingMemoryConfig**: Preset configurations (Default, Minimal, Expanded) for different use cases

#### Output Validation System
- **OutputParser**: Parses LLM output into structured format, extracting dialogue, proposed mutations, and world intents
  - **Structured Output Support**: ✅ Native JSON schema parsing via `ParseStructured()` and `ParseAuto()` methods (Feature 12 - Complete)
  - **Regex Fallback**: Maintains backward compatibility with regex-based parsing when structured output is unavailable
  - **JsonSchemaBuilder**: Dynamic schema generation from C# types with pre-built schemas (ParsedOutputSchema, DialogueOnlySchema, AnalysisSchema)
- **ValidationGate**: Multi-layer validation that checks outputs against constraints, canonical facts, and knowledge boundaries
- **ParsedOutput**: Structured result container with dialogue text, proposed mutations, and world intents
- **ValidationRule**: Extensible validation rule system supporting custom validation logic

#### Controlled Memory Mutation
- **MemoryMutationController**: Authority-enforced mutation execution that prevents canonical fact overrides
- **Mutation Types**: Supports AppendEpisodic, TransformBelief, TransformRelationship, and EmitWorldIntent mutations
- **Authority Enforcement**: Automatically blocks attempts to modify canonical facts with statistics tracking
- **Mutation Statistics**: Comprehensive tracking of success/failure rates for debugging and metrics
- **Event-Based World Intents**: World intent delivery via events for seamless game system integration

#### Enhanced Fallback System
- **FallbackSystem**: Engine-agnostic fallback implementation with context-aware response selection
- **IFallbackSystem**: Interface abstraction enabling dependency injection and comprehensive testing
- **FallbackConfig**: Configurable generic, context-aware, and emergency fallback responses
- **FallbackStats**: Detailed statistics tracking for fallback usage by trigger reason and failure type

#### Testability & Dependency Injection
- **IFileSystem**: Interface abstraction for file system operations (enables testing)
- **IApiClient**: Interface abstraction for API client operations (enables testing)
- **FileSystem**: Default implementation with 100% test coverage
- **Comprehensive Test Suite**: 1,531 tests with 92.37% code coverage

#### Utilities
- **JsonUtils**: Safe JSON serialization/deserialization with validation and compression
- **PathUtils**: Secure file path operations with traversal prevention
- **ProcessUtils**: Process management and validation utilities
- **Logger**: Structured logging system

## 📦 Installation

### Requirements
- .NET Standard 2.1 or higher
- llama.cpp server executable
- Compatible GGUF model file

### Dependencies
```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

## 🚀 Quick Start

### Basic API Client Usage

```csharp
using LlamaBrain.Core;

// Create API client
var client = new ApiClient("localhost", 8080, "llama-2-7b.gguf");

// Send a prompt
var response = await client.SendPromptAsync("Hello, how are you?");

// Clean up
client.Dispose();
```

### Brain Agent Usage (Basic)

```csharp
using LlamaBrain.Core;
using LlamaBrain.Persona;

// Create a persona profile
var profile = PersonaProfile.Create("assistant", "AI Assistant");
profile.Description = "A helpful AI assistant";
profile.SetTrait("helpful", "Always tries to be helpful and informative");
profile.SetTrait("friendly", "Speaks in a friendly and approachable manner");

// Create API client
var client = new ApiClient("localhost", 8080, "llama-2-7b.gguf");

// Create brain agent
using var agent = new BrainAgent(profile, client);

// Send a message and get response
var response = await agent.SendMessageAsync("What's the weather like today?");

// Send an instruction
var instructionResponse = await agent.SendInstructionAsync("Write a short poem about coding");

// Get conversation history
var history = agent.GetConversationHistory();

// Add a memory (using structured memory system)
agent.AddMemory("User prefers technical explanations");
```

### Advanced Usage with Architectural Pattern

```csharp
using LlamaBrain.Core;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Validation;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;

// Create persona profile
var profile = PersonaProfile.Create("wizard", "Gorblaf the Grey-Green");
profile.Description = "A wise and powerful wizard";

// Create memory store with authoritative system
var memoryStore = new PersonaMemoryStore("wizard");
var memorySystem = memoryStore.GetOrCreateSystem("wizard");

// Initialize canonical facts (immutable world truths)
memoryStore.AddCanonicalFact("wizard", "magic-exists", "Magic exists in this world");
memoryStore.AddCanonicalFact("wizard", "tower-destroyed", "The ancient tower was destroyed 100 years ago");

// Set world state (mutable game state)
memoryStore.SetWorldState("wizard", "CurrentWeather", "Stormy");
memoryStore.SetWorldState("wizard", "TimeOfDay", "Evening");

// Create expectancy evaluator for constraint-based behavior
var expectancyEvaluator = new ExpectancyEvaluator();

// Create context for interaction
var interactionContext = new InteractionContext
{
    TriggerReason = TriggerReason.PlayerUtterance,
    NpcId = "wizard",
    SceneName = "TowerRuins",
    PlayerInput = "Tell me about the ancient tower"
};

// Evaluate constraints (rules would come from your expectancy configuration)
var constraints = new ConstraintSet();
// Add constraints based on your rules...

// Retrieve relevant context from memory system
var retrievalLayer = new ContextRetrievalLayer(memorySystem);
var retrievedContext = retrievalLayer.RetrieveContext(interactionContext.PlayerInput);

// Create state snapshot builder
var snapshotBuilder = new StateSnapshotBuilder();

// Build snapshot with context
var snapshot = retrievedContext.ApplyTo(snapshotBuilder
    .WithContext(interactionContext)
    .WithConstraints(constraints)
    .WithSystemPrompt(profile.SystemPrompt ?? profile.Description ?? "")
    .WithPlayerInput(interactionContext.PlayerInput))
    .Build();

// Create prompt assembler for bounded context
var assembler = new PromptAssembler(PromptAssemblerConfig.Default);
var assembledPrompt = assembler.AssembleFromSnapshot(snapshot);

// Send to LLM (via ApiClient)
var client = new ApiClient("localhost", 8080, "llama-2-7b.gguf");
var response = await client.SendPromptAsync(assembledPrompt.PromptText);

// Parse and validate output
var parser = new OutputParser();
var parsedOutput = parser.Parse(response);

// Create validation context with memory system, constraints, and snapshot
var validationContext = new ValidationContext
{
    MemorySystem = memorySystem,
    Constraints = constraints,
    Snapshot = snapshot
};

var validationGate = new ValidationGate();
var gateResult = validationGate.Validate(parsedOutput, validationContext);

if (gateResult.Passed)
{
    // Execute approved mutations
    var mutationController = new MemoryMutationController();
    var mutationResult = mutationController.ExecuteMutations(gateResult, memorySystem);
    
    // Handle world intents
    foreach (var intent in gateResult.ApprovedIntents)
    {
        // Dispatch to game systems
        Console.WriteLine($"World Intent: {intent.IntentType} - {intent.Target}");
    }
}
```

### Persona Management

```csharp
using LlamaBrain.Persona;

// Create persona manager
var manager = new PersonaProfileManager("C:/Personas");

// Create a new persona
var profile = PersonaProfile.Create("wizard", "Gorblaf the Grey-Green");
profile.Description = "A wise and powerful wizard";
profile.SetTrait("wise", "Very knowledgeable about magic and history");
profile.SetTrait("helpful", "Always willing to guide others");
profile.SystemPrompt = "You are a wise wizard who helps travelers with magical advice.";

// Save persona
manager.SaveProfile(profile);

// Load persona
var loadedProfile = manager.LoadProfile("wizard");
```

### Server Management

```csharp
using LlamaBrain.Core;

// Configure server
var config = new ProcessConfig
{
    ExecutablePath = "C:/llama-server.exe",
    Model = "C:/models/llama-2-7b.gguf",
    Port = 5000,
    ContextSize = 2048,
    LlmConfig = new LlmConfig
    {
        MaxTokens = 256,
        Temperature = 0.7f,
        TopP = 0.9f
    }
};

// Start server
var server = new ServerManager(config);
server.StartServer();

// Use server...

// Stop server
server.StopServer();
```

## 🔧 Configuration

### LLM Configuration (`LlmConfig`)

| Parameter | Range | Default | Description |
|-----------|-------|---------|-------------|
| `MaxTokens` | 1-2048 | 64 | Maximum tokens to generate |
| `Temperature` | 0.0-2.0 | 0.7 | Randomness control |
| `TopP` | 0.0-1.0 | 0.9 | Nucleus sampling |
| `TopK` | 1-100 | 40 | Top-k sampling |
| `RepeatPenalty` | 0.0-2.0 | 1.1 | Repetition penalty |

### Process Configuration (`ProcessConfig`)

| Parameter | Range | Default | Description |
|-----------|-------|---------|-------------|
| `Port` | 1-65535 | 5000 | Server port |
| `ContextSize` | 512-32768 | 2048 | Context window size |
| `ExecutablePath` | - | - | Path to llama.cpp server |
| `Model` | - | - | Path to GGUF model file |

## 🛡️ Security & Safeguards

LlamaBrain implements comprehensive security measures:

### Input Validation
- Host, port, and model validation
- Prompt length limits (10,000 characters)
- Parameter range validation
- Path traversal prevention

### Rate Limiting
- 60 requests per minute maximum
- Sliding window implementation
- Thread-safe operation

### File System Security
- Path validation and sanitization
- File size limits (1MB profiles, 5MB memory)
- Atomic file operations
- Traversal attack prevention

### Process Security
- Executable path validation
- Argument sanitization
- Working directory restrictions
- Graceful shutdown handling

See [SAFEGUARDS.md](../Documentation/SAFEGUARDS.md) for detailed information.

## 📁 Project Structure

```
LlamaBrain/
├── Source/
│   ├── Core/                    # Core API and server management
│   │   ├── ApiClient.cs        # HTTP client for llama.cpp
│   │   ├── ApiContracts.cs     # Request/response models
│   │   ├── BrainAgent.cs       # High-level persona interaction interface
│   │   ├── ClientManager.cs    # Client lifecycle management
│   │   ├── DialogueSession.cs  # Conversation management with memory
│   │   ├── LlmConfig.cs        # LLM parameters
│   │   ├── ProcessConfig.cs    # Server configuration
│   │   ├── PromptComposer.cs   # Intelligent prompt building
│   │   ├── ServerManager.cs    # Process management
│   │   ├── Expectancy/         # Determinism layer
│   │   │   ├── ExpectancyEvaluator.cs
│   │   │   ├── Constraint.cs
│   │   │   ├── ConstraintSet.cs
│   │   │   ├── InteractionContext.cs
│   │   │   └── IExpectancyRule.cs
│   │   ├── Inference/          # State snapshot & context retrieval
│   │   │   ├── StateSnapshot.cs
│   │   │   ├── ContextRetrievalLayer.cs
│   │   │   ├── InferenceResult.cs
│   │   │   ├── RetryPolicy.cs
│   │   │   ├── ResponseValidator.cs
│   │   │   ├── EphemeralWorkingMemory.cs
│   │   │   └── PromptAssembler.cs
│   │   ├── Validation/         # Output validation
│   │   │   ├── OutputParser.cs
│   │   │   ├── ParsedOutput.cs
│   │   │   └── ValidationGate.cs
│   │   ├── IApiClient.cs       # API client interface for testing
│   │   └── SAFEGUARDS.md       # Security documentation
│   ├── Persona/                # Character and memory system
│   │   ├── PersonaMemoryFileStore.cs
│   │   ├── PersonaMemoryStore.cs
│   │   ├── PersonaProfile.cs   # Character profiles with traits
│   │   ├── PersonaProfileManager.cs
│   │   ├── MemoryMutationController.cs  # Controlled memory mutation
│   │   ├── MemoryTypes/        # Structured memory system
│   │   │   ├── AuthoritativeMemorySystem.cs
│   │   │   ├── MemoryEntry.cs
│   │   │   ├── MemoryAuthority.cs
│   │   │   ├── CanonicalFact.cs
│   │   │   ├── WorldState.cs
│   │   │   ├── EpisodicMemory.cs
│   │   │   └── BeliefMemory.cs
│   └── Utilities/              # Helper utilities
│       ├── IFileSystem.cs      # File system interface for testing
│       ├── JsonUtils.cs
│       ├── Logger.cs
│       ├── PathUtils.cs
│       └── ProcessUtils.cs
```

## 🔄 Integration

### Unity Integration
LlamaBrain is designed to work seamlessly with Unity through the `LlamaBrainRuntime` package. The Unity package provides:
- Unity MonoBehaviour wrappers (LlamaBrainAgent, BrainServer)
- ScriptableObject-based configuration (BrainSettings, PersonaConfig, ExpectancyRuleAsset)
- Unity-specific state snapshot builder (UnityStateSnapshotBuilder)
- World intent dispatcher for game system integration
- Editor tools and custom inspectors

See the Unity Runtime README for integration details (located at `LlamaBrainRuntime/Assets/LlamaBrainRuntime/README.md`).

### Custom Integrations
The library is built on .NET Standard 2.1, making it compatible with:
- .NET Framework 4.6.1+
- .NET Core 2.0+
- .NET 5+
- Unity 2022.3+
- Other engines (Unreal, Godot) via the engine-agnostic core components

### Architectural Flow

The diagram above illustrates the complete inference pipeline. Here's how the components work together to ensure deterministic state management:

1. **Untrusted Observation** → **Interaction Context**: Player actions, NPC interactions, quest triggers, or time-based events create an interaction context
2. **Determinism Layer (Component 2)**: The Expectancy Engine evaluates rules based on context and generates constraints for both prompt assembly and output validation
3. **External Authoritative Memory System (Component 3)**: Structured memory store containing canonical facts (immutable), world state (mutable), episodic memory, and beliefs. **This is the authoritative source of truth that LLMs cannot corrupt.**
4. **Authoritative State Snapshot (Component 4)**: Context Retrieval Layer retrieves relevant memories and builds an immutable snapshot of all context
5. **Ephemeral Working Memory (Component 5)**: Prompt Assembler creates a bounded, token-efficient prompt from the snapshot and constraints
6. **Stateless Inference Core (Component 6)**: The LLM receives the bounded prompt and generates text output (pure function, no memory access, no authority)
7. **Output Parsing & Validation (Component 7)**: **The Critical Validation Gate** - Output Parser extracts structured data, Validation Gate checks against constraints and canonical facts. **Only validated outputs proceed to mutation.**
8. **Memory Mutation + World Effects (Component 8)**: Validated outputs trigger controlled memory mutations and world intents. **Crucially: canonical facts cannot be overridden, ensuring game state integrity.**
9. **Author-Controlled Fallback (Component 9)**: If validation fails after retries, context-aware fallback responses are used (never corrupting state)

**The Validation Gate: Your Defense Against Hallucinations**

The validation gate (Component 7) is the critical barrier between the stochastic LLM and your authoritative game state. It ensures:
- **Constraint Compliance**: All outputs must satisfy expectancy engine constraints
- **Canonical Fact Protection**: Outputs cannot contradict or override immutable canonical facts
- **Authority Enforcement**: Only authorized mutation types can modify specific memory types
- **Retry Logic**: Invalid outputs trigger retries with stricter constraints, never corrupting state

**Key Architectural Principles:**
- **Stateless LLM**: The LLM has no memory or state - it's a pure generator that receives bounded prompts
- **Deterministic State**: All context is captured in immutable StateSnapshots that can be replayed for retries
- **Bounded Context**: EphemeralWorkingMemory ensures token-efficient prompts by explicitly bounding context size
- **Validation Gate**: All outputs are validated against constraints and canonical facts before any state changes
- **Controlled Mutation**: Only validated outputs can mutate memory, with strict authority enforcement
- **Retry & Fallback**: Automatic retry with constraint escalation, graceful fallback when all retries fail

This architecture provides several key benefits:
- **Determinism**: Same input + context = same output (when constraints are met)
- **Authority**: Canonical facts cannot be overridden by AI, ensuring world consistency
- **Safety**: Multi-layer validation prevents invalid outputs from affecting game state
- **Efficiency**: Bounded prompts prevent token waste while maintaining relevant context

## 🧪 Testing

The library includes a comprehensive test suite with **92.37% code coverage** (5,100 of 5,521 lines covered) and **1,531 passing tests**:

### Test Coverage by Component

- **Expectancy Engine**: 55+ tests (ExpectancyEvaluator, Constraint, ConstraintSet, InteractionContext)
- **Structured Memory System**: 65+ tests (all memory types, AuthoritativeMemorySystem, PersonaMemoryStore)
- **Inference Pipeline**: Comprehensive coverage including:
  - ContextRetrievalLayer: 55 tests (Feature 10 determinism tests)
  - PromptAssembler: 40 tests
  - EphemeralWorkingMemory: 40 tests
  - StateSnapshot, InferenceResult, RetryPolicy, ResponseValidator: Additional tests
- **Output Validation**: 120+ tests including:
  - OutputParser: 86 tests (includes normalization contract tests)
  - ValidationGate: 17+ tests
  - ParsedOutput and related types: Additional tests
- **Memory Mutation**: 41 tests (MemoryMutationController with 100% coverage of mutation types)
- **Few-Shot Prompt Priming**: 30 tests (FewShotExample, EphemeralWorkingMemory few-shot handling, FallbackToFewShotConverter)
- **Core Integration**: Comprehensive test suites including:
  - ApiClient: 90.54% coverage with extensive HTTP, rate limiting, and error handling tests
  - ServerManager: 74.55% coverage (92.31% branch coverage) with 2,123+ lines of tests
  - BrainAgent, ClientManager, DialogueSession: Additional integration tests
- **Utilities**: 200+ tests including:
  - FileSystem: 100% line coverage (41 tests)
  - ProcessUtils: 617+ lines of comprehensive tests
  - JsonUtils, PathUtils: Additional utility tests
- **Integration Tests**: 8 tests (FullPipelineIntegrationTests covering complete 9-component pipeline)
- **Additional comprehensive test coverage across all components bringing total to 1,531 tests**

### Test Infrastructure

- **Test Framework**: NUnit with NSubstitute for mocking
- **Coverage Tool**: Coverlet for code coverage analysis
- **Coverage Reporting**: Automated PowerShell script (`analyze-coverage.ps1`) generates detailed reports
- **Test Project**: Standalone `LlamaBrain.Tests` project separate from Unity package

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Generate coverage report
.\analyze-coverage.ps1
```

See `COVERAGE_REPORT.md` for detailed coverage breakdown.

## 📚 Documentation

Comprehensive documentation is available in the `Documentation/` folder:

- **[ARCHITECTURE.md](../Documentation/ARCHITECTURE.md)** - Complete architectural documentation explaining the 9-component pattern
- **[MEMORY.md](../Documentation/MEMORY.md)** - Comprehensive memory system documentation with authority hierarchy
- **[PIPELINE_CONTRACT.md](../Documentation/PIPELINE_CONTRACT.md)** - Formal pipeline contract specification (Version 0.2.0)
- **[VALIDATION_GATING.md](../Documentation/VALIDATION_GATING.md)** - Complete validation gating system documentation
- **[USAGE_GUIDE.md](../Documentation/USAGE_GUIDE.md)** - Practical examples and best practices for using LlamaBrain
- **[ROADMAP.md](../Documentation/ROADMAP.md)** - Implementation status and future plans
- **[STATUS.md](../Documentation/STATUS.md)** - Current implementation status
- **[DETERMINISM_CONTRACT.md](../Documentation/DETERMINISM_CONTRACT.md)** - Determinism contract and boundaries
- **[SAFEGUARDS.md](../Documentation/SAFEGUARDS.md)** - Security measures and safeguards
- **[CHANGELOG.md](../Documentation/CHANGELOG.md)** - Version history and changes

## 📄 License

This asset is licensed under the Unity Asset Store Standard End User License Agreement. One license per seat is required. See: https://unity.com/legal/as-terms

## 🆘 Support

For issues, questions, or contributions:
- Check the [SAFEGUARDS.md](../Documentation/SAFEGUARDS.md) for security information
- Review existing issues and discussions
- Create a new issue with detailed information

---

**Note**: This library requires a running llama.cpp server. Ensure you have the appropriate llama.cpp executable and compatible model file before use. 