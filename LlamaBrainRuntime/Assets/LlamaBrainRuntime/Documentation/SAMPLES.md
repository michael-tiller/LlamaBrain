# LlamaBrain for Unity Samples

This document describes the sample scenes and assets included with the LlamaBrain for Unity package.

## 📁 Sample Structure

```
Samples/
├── GettingStarted.unity          # Basic setup and configuration
├── Shared/                       # Shared assets used across samples
│   ├── Prefabs/                  # Reusable Unity prefabs
│   ├── Settings/                 # Configuration assets
│   └── Traits/                   # Personality trait definitions
├── NpcPersonaChat/               # NPC conversation example
├── JournalWriter/                # AI-powered writing assistant
├── FlavorText/                   # Dynamic text generation
├── ThemedRewrite/                # Content rewriting with themes
├── MemoryManagement/             # Memory and conversation history management
└── ClientManagement/             # Server connection monitoring and control
```

## 🎮 Sample Scenes

### Getting Started (`GettingStarted.unity`)
**Purpose**: Basic setup and configuration demonstration

**What it shows:**
- How to configure BrainSettings
- How to set up a basic BrainServer
- How to create and configure a LlamaBrainAgent
- Basic dialogue interaction

**Key Components:**
- BrainServer prefab with settings
- LlamaBrainAgent with persona configuration
- Simple dialogue UI
- Configuration examples

**Best for**: New users learning the basics

### NPC Persona Chat (`NpcPersonaChat/`)
**Purpose**: Advanced NPC conversation system

**What it shows:**
- Complex persona configurations
- Multiple NPCs with different personalities
- Advanced dialogue systems
- Memory and conversation persistence

**Key Components:**
- Multiple LlamaBrainAgent instances
- Advanced persona profiles
- Complex dialogue UI
- Memory management

**Best for**: Game developers building NPC systems

### Journal Writer (`JournalWriter/`)
**Purpose**: AI-powered writing assistant

**What it shows:**
- Specialized persona for writing tasks
- Instruction-based interactions
- Content generation workflows
- Writing assistance features

**Key Components:**
- Writing-focused persona configuration
- Instruction-based prompt system
- Text generation and editing
- Writing workflow integration

**Best for**: Content creators and writing applications

### Flavor Text (`FlavorText/`)
**Purpose**: Dynamic text generation for games

**What it shows:**
- Procedural text generation
- Context-aware content creation
- Game integration patterns
- Dynamic storytelling

**Key Components:**
- Flavor text generation system
- Context-aware prompts
- Game event integration
- Dynamic content creation

**Best for**: Game developers needing dynamic content

### Themed Rewrite (`ThemedRewrite/`)
**Purpose**: Content rewriting with different themes/styles

**What it shows:**
- Content transformation
- Style-based persona switching
- Text rewriting workflows
- Theme-based content generation

**Key Components:**
- Multiple themed personas
- Content transformation system
- Style switching
- Rewrite workflows

**Best for**: Content creators and editors

### Memory Management (`MemoryManagement/`)
**Purpose**: Advanced memory and conversation history management demonstration

**What it shows:**
- Complete memory management system (get, clear, add, categorize memories)
- Conversation history tracking and management
- Real-time UI updates with TextMeshPro integration
- Interactive memory management controls with categories
- Memory vs conversation history concepts
- Agent initialization status monitoring
- Memory filtering by categories
- Enhanced debugging and troubleshooting tools

**Key Components:**
- Enhanced LlamaBrainAgent with comprehensive memory methods
- Advanced MemoryManagerUI component with TextMeshPro
- Memory categorization system with dropdown filtering
- Real-time display updates with initialization checks
- Custom memory input with category support
- Agent state debugging and connection monitoring
- Context menu tools for development

**Best for**: Developers building advanced AI memory systems and debugging agent behavior

### Client Management (`ClientManagement/`)
**Purpose**: Server connection monitoring and control demonstration

**What it shows:**
- Server health monitoring and status checking
- Connection management with timeout handling
- Server restart and control capabilities
- Real-time status monitoring with visual indicators
- Error handling and troubleshooting tools
- Server startup time and connection attempt tracking

**Key Components:**
- Enhanced BrainServer with client management methods
- ClientManagerUI component with TextMeshPro integration
- Real-time status monitoring and visual indicators
- Interactive server control buttons
- Configurable timeout settings
- Debug tools and context menu functionality

**Best for**: Developers needing server monitoring and connection management

## 🛠️ Shared Assets

### Prefabs (`Shared/Prefabs/`)
Reusable Unity prefabs for common functionality:

- **BrainAgent.prefab**: Pre-configured LlamaBrainAgent
- **BrainServer.prefab**: Pre-configured BrainServer
- **UI/**: Dialogue UI components and canvases

### Settings (`Shared/Settings/`)
Configuration assets for easy setup:

- **Brain Settings.asset**: Default server configuration
- **Prompt Composer Settings.asset**: Default prompt configuration

### Traits (`Shared/Traits/`)
Reusable personality trait definitions:

- **Common Traits.asset**: Basic personality traits
- **Angry.asset**: Anger-related personality traits
- **Curious.asset**: Curiosity-related traits
- **Knowledgeable.asset**: Knowledge-related traits

## 🚀 Getting Started with Samples

### 1. Start with GettingStarted.unity
Begin with the Getting Started scene to understand basic setup and configuration.

### 2. Explore Shared Assets
Review the shared prefabs, settings, and traits to understand reusable components.

### 3. Try Different Samples
Experiment with different sample scenes based on your use case:
- **Games**: Focus on NpcPersonaChat and FlavorText
- **Content Creation**: Try JournalWriter and ThemedRewrite
- **Learning**: Work through all samples in order

### 4. Customize and Extend
Use the samples as starting points for your own implementations.

## 🔧 Customizing Samples

### Modifying Personas
1. Open the sample scene
2. Select the LlamaBrainAgent GameObject
3. Modify the PersonaConfig in the inspector
4. Add or modify traits as needed

### Changing Settings
1. Select the BrainServer GameObject
2. Modify BrainSettings in the inspector
3. Adjust server configuration as needed
4. Update PromptComposerSettings for different behaviors

### Adding New Traits
1. Create new PersonaTrait assets
2. Add them to persona configurations
3. Customize trait values and descriptions

## 🎯 Best Practices

### Performance
- Use appropriate context sizes for your use case
- Limit conversation history length
- Implement response caching where appropriate
- Monitor memory usage with large conversations

### Security
- Validate all user inputs
- Implement content filtering
- Use appropriate rate limiting
- Secure your model files and server

### User Experience
- Provide loading indicators
- Implement fallback responses
- Use appropriate response lengths
- Consider voice synthesis for immersion

## 🐛 Troubleshooting Samples

### Common Issues
- **Server won't start**: Check executable path and model file
- **Slow responses**: Reduce context size or use smaller models
- **Memory issues**: Limit conversation history and implement cleanup
- **UI problems**: Check prefab references and event connections

### Getting Help
- Check the main README for detailed setup instructions
- Review the [SAFEGUARDS.md](../../../../Documentation/SAFEGUARDS.md) for security information
- Test with the provided sample configurations
- Ensure your llama.cpp server is running correctly

## 📚 Next Steps

After exploring the samples:
1. Read the main README for detailed documentation
2. Check the API documentation for advanced usage
3. Review the [SAFEGUARDS.md](../../../../Documentation/SAFEGUARDS.md) for security considerations
4. Start building your own implementation

---

**Note**: All samples require a running llama.cpp server and compatible model file. Ensure you have the appropriate setup before running the samples. 