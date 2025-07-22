# LlamaBrain Core Library

A robust, secure, and feature-rich .NET Standard 2.1 library for integrating with llama.cpp servers. LlamaBrain provides a comprehensive solution for AI-powered applications with built-in safety measures, persona management, and process control.

## 🎯 Overview

LlamaBrain is designed to be a production-ready library for AI integration, featuring:

- **Secure API Client**: Rate-limited, validated, and sanitized HTTP communication
- **Persona Management**: Persistent character profiles with memory systems
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
- **PersonaProfile**: Character definitions with traits and behaviors
- **PersonaMemoryStore**: Persistent memory system for conversations
- **PersonaProfileManager**: File-based profile management with safeguards

#### Process Management
- **ServerManager**: Safe llama.cpp server process control
- **ProcessConfig**: Configuration for server startup
- **LlmConfig**: LLM generation parameters

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

### Persona Management

```csharp
using LlamaBrain.Persona;

// Create persona manager
var manager = new PersonaProfileManager("C:/Personas");

// Create a new persona
var profile = PersonaProfile.Create("wizard", "Gandalf the Grey");
profile.Description = "A wise and powerful wizard";
profile.Traits.Add("wise", "Very knowledgeable about magic and history");
profile.Traits.Add("helpful", "Always willing to guide others");

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
    Port = 8080,
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
await server.StartServerAsync();

// Use server...

// Stop server
await server.StopServerAsync();
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
| `Port` | 1-65535 | 8080 | Server port |
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

See [SAFEGUARDS.md](Source/Core/SAFEGUARDS.md) for detailed information.

## 📁 Project Structure

```
LlamaBrain/
├── Source/
│   ├── Core/                    # Core API and server management
│   │   ├── ApiClient.cs        # HTTP client for llama.cpp
│   │   ├── ApiContracts.cs     # Request/response models
│   │   ├── ClientManager.cs    # Client lifecycle management
│   │   ├── DialogueSession.cs  # Conversation management
│   │   ├── LlmConfig.cs        # LLM parameters
│   │   ├── ProcessConfig.cs    # Server configuration
│   │   ├── PromptComposer.cs   # Prompt building utilities
│   │   ├── ServerManager.cs    # Process management
│   │   └── SAFEGUARDS.md       # Security documentation
│   ├── Persona/                # Character and memory system
│   │   ├── PersonaMemoryFileStore.cs
│   │   ├── PersonaMemoryStore.cs
│   │   ├── PersonaProfile.cs
│   │   └── PersonaProfileManager.cs
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

## 📝 Logging

LlamaBrain includes a structured logging system:

```csharp
using LlamaBrain.Utilities;

Logger.LogInfo("Starting LlamaBrain client");
Logger.LogWarning("Rate limit approaching");
Logger.LogError("Failed to connect to server", exception);
```

## 📄 License

[Add your license information here]

## 🆘 Support

For issues, questions, or contributions:
- Check the [SAFEGUARDS.md](Source/Core/SAFEGUARDS.md) for security information
- Review existing issues and discussions
- Create a new issue with detailed information

---

**Note**: This library requires a running llama.cpp server. Ensure you have the appropriate llama.cpp executable and compatible model file before use. 