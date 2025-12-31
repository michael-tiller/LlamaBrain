# LlamaBrain Core Library 0.2.0-rc.1

A robust, secure, and feature-rich .NET Standard 2.1 library for integrating with llama.cpp servers. LlamaBrain provides a comprehensive solution for AI-powered applications with built-in safety measures, persona management, and process control.

## 🎯 Overview

LlamaBrain is designed to be a production-ready library for AI integration, featuring:

- **Secure API Client**: Rate-limited, validated, and sanitized HTTP communication
- **Persona Management**: Persistent character profiles with memory systems
- **Determinism Layer**: Expectancy engine with constraint-based behavior control
- **Structured Memory System**: Authoritative memory with canonical facts, world state, episodic memory, and beliefs
- **State Snapshot & Context Retrieval**: Immutable state snapshots with intelligent context retrieval
- **Ephemeral Working Memory**: Bounded working memory for efficient prompt assembly
- **Output Validation System**: Comprehensive validation gate with constraint checking and mutation validation
- **Enhanced Fallback System**: Context-aware fallback responses when inference fails after retries
- **Process Management**: Safe server startup, monitoring, and shutdown
- **Comprehensive Safeguards**: Multi-layered security and stability measures
- **Unity Integration Ready**: Designed to work seamlessly with Unity projects

## 🏗️ Architecture

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

#### Determinism Layer (Phase 1)
- **ExpectancyEvaluator**: Engine-agnostic rule evaluation system
- **Constraint System**: Permission, Prohibition, and Requirement constraints with severity levels
- **InteractionContext**: Context-aware rule evaluation with trigger reasons, NPC IDs, and scene names
- **IExpectancyRule**: Extensible rule interface for custom behavior constraints

#### Structured Memory System (Phase 2)
- **AuthoritativeMemorySystem**: Authority-based memory management with boundary enforcement
- **Memory Types**: CanonicalFact (immutable), WorldState (mutable), EpisodicMemory (conversation history), BeliefMemory (NPC opinions)
- **MemoryAuthority**: Hierarchy enforcement (Canonical > WorldState > Episodic > Belief)
- **Memory Decay**: Automatic episodic memory decay with significance-based retention

#### State Snapshot & Context Retrieval (Phase 3)
- **StateSnapshot**: Immutable snapshot of all context at inference time
- **ContextRetrievalLayer**: Intelligent context retrieval with recency/relevance/significance scoring
- **InferenceResult**: Result tracking with validation outcomes and token usage
- **RetryPolicy**: Configurable retry behavior with constraint escalation
- **ResponseValidator**: Response validation against constraint sets

#### Ephemeral Working Memory (Phase 4)
- **EphemeralWorkingMemory**: Bounded working memory for single inference
- **PromptAssembler**: Token-efficient prompt assembly with configurable limits
- **WorkingMemoryConfig**: Preset configurations for different use cases

#### Output Validation System (Phase 5)
- **OutputParser**: Parses LLM output into structured format (dialogue, mutations, world intents)
- **ValidationGate**: Validates parsed output against constraints and canonical facts
- **ParsedOutput**: Structured result with dialogue, proposed mutations, and world intents
- **ValidationRule**: Extensible validation rule system

#### Enhanced Fallback System (Phase 7)
- **AuthorControlledFallback**: Context-aware fallback responses when inference fails
- **FallbackConfig**: Configurable generic, context-aware, and emergency fallbacks
- **FallbackStats**: Comprehensive statistics tracking for fallback usage
- **Automatic Integration**: Seamlessly integrated with retry system in LlamaBrainAgent

#### Utilities
- **JsonUtils**: Safe JSON serialization/deserialization
- **PathUtils**: Secure file path operations
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

### Brain Agent Usage

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

// Add a memory
agent.AddMemory("User prefers technical explanations");
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
│   │   ├── Expectancy/         # Determinism layer (Phase 1)
│   │   │   ├── ExpectancyEvaluator.cs
│   │   │   ├── Constraint.cs
│   │   │   ├── ConstraintSet.cs
│   │   │   ├── InteractionContext.cs
│   │   │   └── IExpectancyRule.cs
│   │   ├── Inference/          # State snapshot & context (Phase 3-4)
│   │   │   ├── StateSnapshot.cs
│   │   │   ├── ContextRetrievalLayer.cs
│   │   │   ├── InferenceResult.cs
│   │   │   ├── RetryPolicy.cs
│   │   │   ├── ResponseValidator.cs
│   │   │   ├── EphemeralWorkingMemory.cs
│   │   │   └── PromptAssembler.cs
│   │   ├── Validation/         # Output validation (Phase 5)
│   │   │   ├── OutputParser.cs
│   │   │   ├── ParsedOutput.cs
│   │   │   └── ValidationGate.cs
│   │   └── SAFEGUARDS.md       # Security documentation
│   ├── Persona/                # Character and memory system
│   │   ├── PersonaMemoryFileStore.cs
│   │   ├── PersonaMemoryStore.cs
│   │   ├── PersonaProfile.cs   # Character profiles with traits
│   │   ├── PersonaProfileManager.cs
│   │   ├── MemoryTypes/        # Structured memory (Phase 2)
│   │   │   ├── AuthoritativeMemorySystem.cs
│   │   │   ├── MemoryEntry.cs
│   │   │   ├── MemoryAuthority.cs
│   │   │   ├── CanonicalFact.cs
│   │   │   ├── WorldState.cs
│   │   │   ├── EpisodicMemory.cs
│   │   │   └── BeliefMemory.cs
│   └── Utilities/              # Helper utilities
│       ├── JsonUtils.cs
│       ├── Logger.cs
│       ├── PathUtils.cs
│       └── ProcessUtils.cs
```

## 🔄 Integration

### Unity Integration
LlamaBrain is designed to work seamlessly with Unity through the `LlamaBrainForUnity` package. See the Unity project README for integration details.

### Custom Integrations
The library is built on .NET Standard 2.1, making it compatible with:
- .NET Framework 4.6.1+
- .NET Core 2.0+
- .NET 5+
- Unity 2022.3+

## 🧪 Testing

The library includes comprehensive unit tests covering:
- API client functionality
- Persona management
- Process control
- Security safeguards
- Error handling

Run tests using your preferred .NET test runner.

## 📄 License

This asset is licensed under the Unity Asset Store Standard End User License Agreement. One license per seat is required. See: https://unity.com/legal/as-terms

## 🆘 Support

For issues, questions, or contributions:
- Check the [SAFEGUARDS.md](../Documentation/SAFEGUARDS.md) for security information
- Review existing issues and discussions
- Create a new issue with detailed information

---

**Note**: This library requires a running llama.cpp server. Ensure you have the appropriate llama.cpp executable and compatible model file before use. 