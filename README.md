# LlamaBrain

[![CI](https://github.com/michael-tiller/llamabrain/actions/workflows/ci-cd.yml/badge.svg?branch=main&event=push)](https://github.com/michael-tiller/llamabrain/actions/workflows/ci-cd.yml)
[![C#](https://img.shields.io/badge/C%23-239120?logo=c-sharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.1-512BD4?logo=dotnet&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Deterministic Neuro-Symbolic Authoritative State Management**

LlamaBrain™ is a production-ready architecture that enforces a strict validation boundary between untrusted LLM outputs and your authoritative game state. The model is treated as a stateless generator—continuity emerges from deterministic state reconstruction, not from trusting the AI's memory.

[![Deterministic Reconstruction + Validation Boundary](https://img.youtube.com/vi/RT2v9199gfM/0.jpg)](https://www.youtube.com/watch?v=RT2v9199gfM)

## The Core Innovation

**The Model is Untrusted.** LLMs are stochastic generators that hallucinate. LlamaBrain enforces a strict validation boundary: all LLM outputs are validated against constraints and canonical facts before any memory mutations occur. The model has no direct access to game state—it's a pure, stateless generator. Continuity emerges from deterministic state reconstruction, not from trusting the AI's memory.

**The Validation Gate** prevents hallucinations from corrupting authoritative state. Every output is checked against:
- Constraint sets from the expectancy engine
- Immutable canonical facts (world truths that cannot be modified)
- Authority hierarchy (canonical > world state > episodic > beliefs)

Only validated outputs can trigger memory mutations. Invalid outputs trigger retries with stricter constraints, never corrupting state.

## 🧪 Proof: RedRoom Demo & Deterministic Gate/Fallback

**See it in action:** The **RedRoom** testing suite is a complete, runnable demonstration of LlamaBrain's deterministic architecture and validation gates. It serves as both a "Hello World" example and a comprehensive testing framework.

**What RedRoom Demonstrates:**
- ✅ **Complete 9-Component Architecture**: All architectural components working together in a single, runnable example
- ✅ **Deterministic State Reconstruction**: Byte-level determinism in state serialization and prompt assembly
- ✅ **Validation Gate & Fallback**: Invalid outputs trigger retries with stricter constraints—never corrupting state
- ✅ **Production-Ready Components**: Uses the same components as production games, not simplified examples
- ✅ **Comprehensive Testing**: Multiple trigger zones, metrics collection, and adversarial testing capabilities

**Location:** `LlamaBrainRuntime/Assets/LlamaBrainRuntime/Runtime/RedRoom/`

The RedRoom demo proves the architecture works in practice, not just theory. It's the fastest way to understand LlamaBrain's deterministic guarantees and see the validation boundary in action.

LlamaBrain consists of two main components:

- **LlamaBrain Core** - A robust .NET Standard 2.1 library implementing the determinism boundary architecture
- **LlamaBrain Runtime** - A Unity package providing seamless integration with Unity projects

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

4. **See the Proof: RedRoom Demo** ⭐
   - **Start here**: RedRoom is the complete, runnable demonstration of LlamaBrain's deterministic architecture
   - Open the RedRoom scene in Unity to see all 9 architectural components working together
   - Test the validation gate and fallback system with multiple trigger zones
   - Collect metrics and verify deterministic behavior
   - See [RedRoom README](LlamaBrainRuntime/Assets/LlamaBrainRuntime/Runtime/RedRoom/README.md) for complete documentation

### For .NET Developers
1. **Install the Core Library**
   ```xml
   <PackageReference Include="LlamaBrain" Version="0.3.0-rc.1" />
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
- **[Core README](LlamaBrain/README.md)** - Complete library documentation and usage guide
- **[Security Guide](Documentation/SAFEGUARDS.md)** - Security measures, safeguards, and best practices
- **[API Documentation](https://metagrue.com/docs/llamabrainapi/)** - Complete API reference for LlamaBrain Core
- **[Runtime API Documentation](https://metagrue.com/docs/llamabrainruntimeapi/)** - Complete API reference for LlamaBrain Runtime (Unity)
- **[ROADMAP.md](Documentation/ROADMAP.md)** - Development roadmap with comprehensive planning and milestone definitions
- **[STATUS.md](Documentation/STATUS.md)** - Current milestone status and high-level project overview
- **[CONTRIBUTING.md](CONTRIBUTING.md)** - Guidelines for contributing to the project
- **[CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)** - Community standards and code of conduct

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

[![Tests](https://github.com/michael-tiller/llamabrain/actions/workflows/ci-cd.yml/badge.svg?branch=main&event=push)](https://github.com/michael-tiller/llamabrain/actions/workflows/ci-cd.yml)

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

**Note**: LlamaBrain™ is MIT-licensed. The name "LlamaBrain" and any associated logos are trademarks. The MIT License grants rights to use, modify, and distribute the software, but does not grant rights to use the LlamaBrain trademark, name, or logo. See [TRADEMARKS.md](TRADEMARKS.md) for trademark usage policy.

## 📈 Roadmap
**See [ROADMAP.md](Documentation/ROADMAP.md) for detailed implementation plan and progress tracking.**

### Current Status

**Core Architecture**: Complete and production-ready. The determinism boundary, validation gate, and authoritative memory system are fully implemented and tested.

**Current Focus**: Structured Output Integration (Feature 13) and cross-session determinism (Feature 14, 16)

**See [STATUS.md](Documentation/STATUS.md) for milestone progress.**

---

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
