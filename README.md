# LlamaBrain

A comprehensive AI integration solution for Unity and .NET applications, providing secure, feature-rich integration with llama.cpp servers.

## 🎯 Overview

LlamaBrain consists of two main components:

- **LlamaBrain Core Library** - A robust .NET Standard 2.1 library for AI integration
- **LlamaBrain for Unity** - A Unity package providing seamless integration with Unity projects

## 🏗️ Project Structure

```
LlamaBrain/
├── README.md                     # This file - Project overview
├── LlamaBrain/                   # Core .NET library
│   ├── LLAMABRAIN.md             # Core library documentation
│   ├── Source/                   # Source code
│   │   ├── Core/                 # API client, server management, etc.
│   │   ├── Persona/              # Character and memory system
│   │   └── Utilities/            # Helper utilities
│   └── Source/Core/SAFEGUARDS.md # Security documentation
└── LlamaBrainRuntime/            # Unity package
    ├── Assets/LlamaBrainRuntime/
    │   ├── Runtime/              # Unity runtime scripts
    │   │   ├── Core/             # Core Unity components
    │   │   ├── Demo/             # Demo components and UI
    │   │   └── RedRoom/          # In-game LLM testing suite
    │   │       ├── AI/           # NPC follower and AI components
    │   │       ├── Interaction/   # Dialogue triggers and metrics
    │   │       ├── UI/           # Player interaction UI
    │   │       └── README.md     # RedRoom documentation
    │   ├── Editor/               # Unity editor scripts
    │   ├── Samples/              # Example scenes and assets
    │   └── Tests/                # Unit tests
    └── Documentation/            # Additional documentation
```

## 🚀 Quick Start

### For Unity Developers
1. **Import the Unity Package**
   - Import `LlamaBrainRuntime` into your Unity project
   - See the Unity package documentation for detailed setup

2. **Set Up Your Server**
   - Download llama.cpp server executable
   - Obtain a compatible GGUF model file
   - Configure BrainSettings in Unity

3. **Create Your First NPC**
   - Create a PersonaConfig asset
   - Add LlamaBrainAgent to a GameObject
   - Start building AI-powered characters

4. **Test LLM Performance (Optional)**
   - Use the RedRoom testing suite for comprehensive LLM evaluation
   - Set up multiple trigger zones with different prompts
   - Collect and analyze metrics automatically
   - See the RedRoom testing suite documentation for details

### For .NET Developers
1. **Install the Core Library**
   ```xml
   <PackageReference Include="LlamaBrain" Version="0.2.01" />
   ```

2. **Basic Usage**
   ```csharp
   using LlamaBrain.Core;
   using LlamaBrain.Persona;
   
   // Create API client
   var client = new ApiClient("localhost", 8080, "llama-2-7b.gguf");
   
   // Create persona
   var profile = PersonaProfile.Create("assistant", "AI Assistant");
   
   // Create brain agent
   using var agent = new BrainAgent(profile, client);
   
   // Send message
   var response = await agent.SendMessageAsync("Hello!");
   ```

## 🎮 Key Features

### Core Library
- **Secure API Client**: Rate-limited, validated HTTP communication
- **Persona Management**: Character profiles with persistent memory
- **Process Management**: Safe server startup and monitoring
- **Comprehensive Safeguards**: Multi-layered security measures
- **Conversation Tracking**: Structured dialogue management

### Unity Integration
- **ScriptableObject Configuration**: Easy setup in Unity Inspector
- **MonoBehaviour Components**: Seamless Unity integration
- **Built-in UI Components**: Ready-to-use dialogue interfaces
- **Editor Tools**: Custom inspectors and configuration tools
- **Sample Scenes**: Complete examples for different use cases
- **RedRoom Testing Suite**: Comprehensive in-game LLM testing framework
  - Multiple trigger zones for testing different conversational seeds
  - Real-time metrics collection and export
  - Rolling file system for data management
  - NPC follower system with LLM dialogue
  - Player interaction system with visual feedback

## 📚 Documentation

### Core Library
- **[Core README](LlamaBrain/README.md)** - Complete library documentation
- **[Security Guide](Documentation/SAFEGUARDS.md)** - Security measures and safeguards

### Unity Package
Unity package documentation is available in the LlamaBrainRuntime project.

## 🛠️ Requirements

### Core Library
- .NET Standard 2.1 or higher
- llama.cpp server executable
- Compatible GGUF model file

### Unity Package
- Unity 2022.3 LTS or higher
- .NET Standard 2.1 support
- Same server requirements as core library

### Dependencies
```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

## 🔧 Configuration

### Server Setup
1. Download llama.cpp server for your platform
2. Obtain a GGUF model file (e.g., llama-2-7b.gguf)
3. Configure server settings in your application

### Basic Configuration
```csharp
// Core library
var config = new ProcessConfig
{
    ExecutablePath = "C:/llama-server.exe",
    Model = "C:/models/llama-2-7b.gguf",
    Port = 5000,
    ContextSize = 2048
};

// Unity package
// Use BrainSettings ScriptableObject in Inspector
```

## 🎯 Use Cases

### Game Development
- **NPCs with Memory**: Characters that remember player interactions
- **Dynamic Dialogue**: Context-aware conversations
- **Procedural Content**: AI-generated quests and stories
- **Character Personalities**: Unique AI personalities for each NPC
- **LLM Testing & QA**: Comprehensive testing framework for evaluating LLM performance in-game
  - Test multiple conversational scenarios simultaneously
  - Collect detailed performance metrics (response times, token usage)
  - Export data for analysis and comparison
  - Validate LLM behavior across different prompts and contexts

### Content Creation
- **Writing Assistants**: AI-powered writing tools
- **Content Generation**: Dynamic text and story creation
- **Style Transfer**: Rewriting content in different styles
- **Interactive Stories**: Choose-your-own-adventure experiences

### Business Applications
- **Customer Service**: AI chatbots with personality
- **Training Simulations**: Interactive training scenarios
- **Data Analysis**: Natural language data exploration
- **Documentation**: AI-powered help systems

## 🛡️ Security & Safety

LlamaBrain implements comprehensive security measures:

- **Input Validation**: All inputs validated and sanitized
- **Rate Limiting**: 60 requests per minute maximum
- **File System Security**: Path traversal prevention
- **Process Security**: Safe server execution
- **Resource Limits**: File size and memory constraints

See [SAFEGUARDS.md](Documentation/SAFEGUARDS.md) for detailed security information.

## 🧪 Testing

### Core Library
- Comprehensive unit tests
- Security validation tests
- Performance benchmarks
- Integration tests

### Unity Package
- EditMode tests for configuration
- PlayMode tests for runtime functionality
- Sample scene validation
- UI component tests
- **RedRoom Testing Suite**: In-game LLM testing framework
  - Automated metrics collection
  - Rolling file system for data management
  - Multiple test scenarios with trigger zones
  - CSV/JSON export for analysis

## 🔄 Integration

### Unity Integration
LlamaBrain is designed to work seamlessly with Unity:
- ScriptableObject-based configuration
- MonoBehaviour components
- Built-in UI prefabs
- Editor tools and inspectors

### Custom Integrations
The core library supports:
- .NET Framework 4.6.1+
- .NET Core 2.0+
- .NET 5+
- Any .NET Standard 2.1 compatible platform

## 📦 Installation

### Quick Start
If you have questions you can review the video quick start on [Youtube].(https://youtu.be/1EtU6qu7O5Q)

### Unity Package
1. Download the LlamaBrainRuntime package
2. Import into your Unity project
3. Follow the Unity package documentation for setup
4. For LLM testing, see the RedRoom testing suite documentation

### Core Library
```bash
# NuGet (when available)
dotnet add package LlamaBrain

# Or include the source directly
# Copy LlamaBrain/Source/ to your project
```

## 🆘 Support

### Getting Help
1. **Check Documentation**: Start with the relevant README
2. **Review Samples**: Explore the provided examples
3. **Troubleshooting**: Check the troubleshooting guide
4. **Security**: Review the safeguards documentation
5. **Discord**: You can always join the [LlamaBrain Discord](https://discord.gg/9ruBad4nrN) for deep questions.

### Reporting Issues
When reporting issues, include:
- Platform and version information
- Error messages and logs
- Steps to reproduce
- Configuration details

## 📄 License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## 📈 Roadmap

**See [ROADMAP.md](Documentation/ROADMAP.md) for detailed implementation plan and progress tracking.**

### Current Status: ~98% Complete

**Current Phase**: Phase 8 - RedRoom Integration 🚧 99% Complete | Phase 10 - Deterministic Proof Gap Testing 🚧 ~65% Complete

**Next Priority**: Phase 8.4 - Testing Overlay fixes & Phase 10.4 - ValidationGate detailed determinism tests

### Current Features (v0.2+)
- ✅ **RedRoom Testing Suite**: Comprehensive in-game LLM testing framework
- ✅ **Metrics Collection**: Detailed performance and quality metrics with rolling files
- ✅ **Multiple Test Scenarios**: Support for testing multiple conversational seeds
- ✅ **NPC Follower System**: AI-powered NPCs with LLM dialogue
- ✅ **Stateless LLM Core**: Clean separation of inference from state
- ✅ **Expectancy Engine**: Constraint-based behavior control for NPCs
  - Engine-agnostic core (works with Unity, Unreal, Godot)
  - Rule types: Prohibition, Requirement, Permission
  - ScriptableObject-based declarative rules
  - Context-aware constraint evaluation
  - 50 unit tests passing
- ✅ **Structured Memory System**: Authority-based memory management
  - Four memory types: Canonical Facts, World State, Episodic, Beliefs
  - Authority hierarchy prevents unauthorized modifications
  - Episodic memory with decay and significance
  - Belief contradiction detection against canonical facts
  - ~65 unit tests passing

### In Development (Architectural Pattern Implementation)

**Phase 1: Determinism Layer** ✅ Complete
- Expectancy Engine for constraint generation
- Rule-based control over LLM behavior
- Per-NPC and global constraint configuration
- Full test coverage (50 tests)

**Phase 2: Structured Memory System** ✅ Complete
- Canonical Facts (immutable, Designer-only)
- World State (validated mutations, GameSystem+ authority)
- Episodic Memory (decay-enabled, significance-weighted)
- Belief/Relationship Memory (can be wrong, contradiction detection)
- Full test coverage (~65 tests)

**Phase 3: State Snapshot & Retry Logic** ✅ Complete
- Authoritative state snapshots before inference
- Context retrieval with relevance weighting
- Retry logic with stricter constraints (max 3 attempts)
- Full test coverage (~69 tests)

**Phase 4: Ephemeral Working Memory** ✅ Complete
- Bounded working memory for current inference
- Token-aware prompt assembly
- Explicit memory lifecycle management
- Full test coverage

**Phase 5: Output Validation System** ✅ Complete
- Output parser for structured extraction
- Validation gate with constraint checking
- Automatic retry on validation failure
- Full test coverage (60+ tests)

**Phase 6: Controlled Memory Mutation** ✅ Complete
- Validated outputs only for memory writes
- Canonical fact protection enforcement
- World intent emission system
- Full test coverage (41 tests)

**Phase 7: Enhanced Fallback System** ✅ Complete
- Author-controlled fallback hierarchy
- Context-aware emergency responses
- Failure reason logging
- Full test coverage

**Phase 8: RedRoom Integration** 🚧 99% Complete
- Validation metrics and export ✅
- Architectural pattern testing ✅
- End-to-end validation scenarios ✅
- Testing overlays ✅ (Memory Mutation Overlay, Validation Gate Overlay - minor fixes needed)
- Unity PlayMode integration tests ✅ (73+ tests complete)
- Full Pipeline Integration Tests ✅ (8 tests complete)

**Phase 9: Documentation** ✅ 100% Complete
- Architecture documentation with diagrams ✅
- Setup tutorials for new components ✅
- API reference for all layers ✅ (100% XML documentation, zero missing member warnings)
- Few-shot prompt priming ✅ Complete (30 tests, full integration)
- Tutorial content ✅ Complete (4 comprehensive step-by-step tutorials)

**Phase 10: Deterministic Proof Gap Testing** 🚧 In Progress (~65% Complete)
- ✅ Critical Requirements 1-4 implemented (strict total order sorting, SequenceNumber field, tie-breaker logic, OutputParser normalization)
- ✅ 7 high-leverage determinism tests added
- ✅ ContextRetrievalLayer: 55 tests complete (exceeds estimate)
- ✅ PromptAssembler: 40 tests complete
- ✅ EphemeralWorkingMemory: 40 tests complete
- ✅ OutputParser: 86 tests complete (includes normalization contract)
- ✅ MemoryMutationController: 41 tests complete
- 🚧 ValidationGate: 17 tests (Feature 10.4 detailed tests pending)
- ⏳ WorldIntentDispatcher: 0 tests (not started)
- ⏳ Full Pipeline deterministic tests: 0 tests (not started)
- See [PHASE10_PROOF_GAPS.md](Documentation/PHASE10_PROOF_GAPS.md) for detailed test backlog

For detailed status information, see the [STATUS.md](Documentation/STATUS.md) file.

### Future Features (Post-Architecture)
- **Multi-Modal Support**: Image and audio integration
- **Performance Optimization**: Layer-specific optimizations
- **Voice Integration**: Text-to-speech and speech-to-text
- **Animation Integration**: Character animation triggers
- **Multi-Player Support**: Shared world state with validation
- **Advanced Analytics**: Validation and constraint visualization

---

**Note**: LlamaBrain requires a running llama.cpp server. Ensure you have the appropriate llama.cpp executable and compatible model file before use.

For detailed information, see the specific README files for each component:
- [Core Library](LlamaBrain/README.md) 
