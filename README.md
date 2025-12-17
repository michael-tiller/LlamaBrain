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
└── LlamaBrainForUnity/           # Unity package
    ├── LLAMABRAINFORUNITY.md     # Unity package documentation
    ├── SAMPLES.md                # Sample scenes guide
    ├── TROUBLESHOOTING.md        # Troubleshooting guide
    ├── Runtime/                  # Unity runtime scripts
    ├── Editor/                   # Unity editor scripts
    ├── Samples/                  # Example scenes and assets
    └── Tests/                    # Unit tests
```

## 🚀 Quick Start

### For Unity Developers
1. **Import the Unity Package**
   - Import `LlamaBrainForUnity` into your Unity project
   - See [LlamaBrain for Unity README](LlamaBrainForUnity/LLAMABRAINFORUNITY.md) for detailed setup

2. **Set Up Your Server**
   - Download llama.cpp server executable
   - Obtain a compatible GGUF model file
   - Configure BrainSettings in Unity

3. **Create Your First NPC**
   - Create a PersonaConfig asset
   - Add UnityBrainAgent to a GameObject
   - Start building AI-powered characters

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

## 📚 Documentation

### Core Library
- **[Core README](LlamaBrain/LLAMABRAIN.md)** - Complete library documentation
- **[Security Guide](LlamaBrain/Source/Core/SAFEGUARDS.md)** - Security measures and safeguards

### Unity Package
- **[Unity README](LlamaBrainForUnity/Assets/LlamaBrainForUnity/LLAMABRAINFORUNITY.md)** - Unity integration guide
- **[Samples Guide](LlamaBrainForUnity/Assets/LlamaBrainForUnity/SAMPLES.md)** - Sample scenes and examples
- **[Troubleshooting](LlamaBrainForUnity/Assets/LlamaBrainForUnity/TROUBLESHOOTING.md)** - Common issues and solutions
- **[Tests Documentation](LlamaBrainForUnity/Assets/LlamaBrainForUnity/Tests/TESTS.md)** - Testing documentation

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

See [SAFEGUARDS.md](LlamaBrain/Source/Core/SAFEGUARDS.md) for detailed security information.

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
1. Download the LlamaBrainForUnity package
2. Import into your Unity project
3. Follow the [Unity README](LlamaBrainForUnity/Assets/LlamaBrainForUnity/LLAMABRAINFORUNITY.md) for setup

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

This repository is provided privately for code review and evaluation purposes only.

All rights reserved. No part of this codebase may be copied, redistributed, or used in any product or service without explicit written permission from Michael Tiller.

## 📈 Roadmap

### Planned Features
- **Enhanced Memory Systems**: More sophisticated memory management
- **Multi-Modal Support**: Image and audio integration
- **Advanced Prompting**: More sophisticated prompt composition
- **Performance Optimization**: Improved response times
- **Additional Platforms**: Support for more .NET platforms
- **Voice Integration**: Text-to-speech and speech-to-text
- **Animation Integration**: Character animation triggers
- **Save System Integration**: Persistent conversation storage
- **Multi-Player Support**: Shared AI experiences

---

**Note**: LlamaBrain requires a running llama.cpp server. Ensure you have the appropriate llama.cpp executable and compatible model file before use.

For detailed information, see the specific README files for each component:
- [Core Library](LlamaBrain/LLAMABRAIN.md)
- [Unity Package](LlamaBrainForUnity/Assets/LlamaBrainForUnity/LLAMABRAINFORUNITY.md) 
