# LlamaBrain for Unity

A Unity package that provides seamless integration with the LlamaBrain core library, enabling AI-powered NPCs, dialogue systems, and intelligent game interactions in Unity projects.

## 🎯 Overview

LlamaBrain for Unity brings the power of local AI to Unity games and applications through:

- **AI-Powered NPCs**: Intelligent characters with persistent personalities and memory
- **Dynamic Dialogue Systems**: Natural conversations that adapt to player interactions
- **Easy Unity Integration**: Simple setup with ScriptableObject-based configuration
- **Real-time AI Processing**: Local llama.cpp server integration for privacy and performance
- **Comprehensive UI**: Built-in dialogue interface and management tools

## 🏗️ Architecture

### Core Components

#### BrainServer (`BrainServer.cs`)
- Manages the llama.cpp server process
- Handles server startup, monitoring, and shutdown
- Provides connection management for multiple clients
- Automatic error recovery and restart capabilities

#### BrainAgent (`BrainAgent.cs`)
- Represents individual AI characters/NPCs
- Manages persona profiles and memory
- Handles conversation state and context
- Provides easy-to-use dialogue methods

#### Configuration System
- **BrainSettings**: Server and LLM configuration
- **PersonaConfig**: Character personality and behavior settings
- **PromptComposerSettings**: Custom prompt building rules

#### UI Components
- **DialoguePanelController**: Main dialogue interface
- **DialogueMessage**: Individual message display
- **BrainCanvas**: Complete dialogue UI system

## 📦 Installation

### Requirements
- Unity 2022.3 LTS or higher
- .NET Standard 2.1 support
- llama.cpp server executable
- Compatible GGUF model file

### Setup
1. Import the LlamaBrainForUnity package into your Unity project
2. Ensure the LlamaBrain.dll is in the Plugins folder
3. Configure your BrainSettings asset
4. Set up your llama.cpp server and model

## 🚀 Quick Start

### 1. Configure Brain Settings

Create a BrainSettings asset:
1. Right-click in Project window → Create → LlamaBrain → BrainSettings
2. Configure the server settings:
   - **Executable Path**: Path to your llama.cpp server
   - **Model Path**: Path to your GGUF model file
   - **Port**: Server port (default: 5000)
   - **Context Size**: Model context window size

### 2. Create a Persona

Create a PersonaConfig asset:
1. Right-click → Create → LlamaBrain → PersonaConfig
2. Define the character:
   - **Name**: Character name
   - **Description**: Character background and personality
   - **Traits**: Key personality traits and behaviors
   - **Memory**: Persistent memory settings

### 3. Set Up an NPC

```csharp
using LlamaBrain.Unity.Runtime.Core;
using LlamaBrain.Unity.Runtime.Demo;

public class MyNPC : MonoBehaviour
{
    [SerializeField] private BrainSettings brainSettings;
    [SerializeField] private PersonaConfig personaConfig;
    
    private BrainAgent brainAgent;
    
    void Start()
    {
        // Initialize the brain agent
        brainAgent = new BrainAgent(brainSettings, personaConfig);
    }
    
    public async void StartConversation(string playerMessage)
    {
        // Get AI response
        string response = await brainAgent.SendMessageAsync(playerMessage);
        Debug.Log($"NPC: {response}");
    }
    
    void OnDestroy()
    {
        brainAgent?.Dispose();
    }
}
```

### 4. Use the Built-in UI

```csharp
using LlamaBrain.Unity.Runtime.Demo.UI;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private DialoguePanelController dialoguePanel;
    [SerializeField] private BrainAgent brainAgent;
    
    public void ShowDialogue()
    {
        dialoguePanel.ShowDialogue(brainAgent);
    }
}
```

## 🔧 Configuration

### Brain Settings

| Setting | Description | Default |
|---------|-------------|---------|
| **Executable Path** | Path to llama.cpp server | - |
| **Model Path** | Path to GGUF model file | - |
| **Port** | Server port number | 5000 |
| **Context Size** | Model context window | 2048 |
| **Max Tokens** | Maximum response length | 64 |
| **Temperature** | Response randomness | 0.7 |
| **Top P** | Nucleus sampling | 0.9 |
| **Top K** | Top-k sampling | 40 |
| **Repeat Penalty** | Repetition control | 1.1 |

### Persona Configuration

| Setting | Description |
|---------|-------------|
| **Name** | Character name |
| **Description** | Character background |
| **Traits** | Personality traits dictionary |
| **Memory Enabled** | Enable persistent memory |
| **Memory Capacity** | Maximum memory entries |

## 🎮 Usage Examples

### Basic NPC Interaction

```csharp
public class SimpleNPC : MonoBehaviour
{
    [SerializeField] private BrainSettings settings;
    [SerializeField] private PersonaConfig persona;
    
    private BrainAgent agent;
    
    async void Start()
    {
        agent = new BrainAgent(settings, persona);
        await agent.InitializeAsync();
    }
    
    public async void TalkToPlayer(string playerInput)
    {
        string response = await agent.SendMessageAsync(playerInput);
        // Handle the response (display in UI, play audio, etc.)
        DisplayResponse(response);
    }
}
```

### Advanced Dialogue System

```csharp
public class AdvancedDialogue : MonoBehaviour
{
    [SerializeField] private BrainAgent agent;
    [SerializeField] private DialoguePanelController ui;
    
    private DialogueSession session;
    
    async void Start()
    {
        session = new DialogueSession(agent);
        await session.StartAsync();
        
        // Set up UI callbacks
        ui.OnMessageSent += async (message) => {
            var response = await session.SendMessageAsync(message);
            ui.AddMessage(response, MessageType.NPC);
        };
    }
}
```

### Multiple NPCs

```csharp
public class NPCManager : MonoBehaviour
{
    [SerializeField] private BrainSettings settings;
    [SerializeField] private PersonaConfig[] personas;
    
    private Dictionary<string, BrainAgent> npcs = new();
    
    async void Start()
    {
        foreach (var persona in personas)
        {
            var agent = new BrainAgent(settings, persona);
            await agent.InitializeAsync();
            npcs[persona.Name] = agent;
        }
    }
    
    public async Task<string> TalkToNPC(string npcName, string message)
    {
        if (npcs.TryGetValue(npcName, out var agent))
        {
            return await agent.SendMessageAsync(message);
        }
        return "NPC not found";
    }
}
```

## 🎨 UI Components

### Dialogue Panel

The built-in dialogue panel provides:
- Message history display
- Input field for player messages
- Send button with keyboard shortcuts
- Message type indicators (Player/NPC)
- Auto-scrolling conversation view

### Custom UI Integration

```csharp
public class CustomDialogueUI : MonoBehaviour
{
    [SerializeField] private BrainAgent agent;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Text responseText;
    
    public async void SendMessage()
    {
        string message = inputField.text;
        if (string.IsNullOrEmpty(message)) return;
        
        inputField.text = "";
        responseText.text = "Thinking...";
        
        string response = await agent.SendMessageAsync(message);
        responseText.text = response;
    }
}
```

## 📁 Project Structure

```
LlamaBrainForUnity/
├── Data/
│   ├── Prefab/              # Unity prefabs
│   │   ├── BrainAgent.prefab
│   │   ├── BrainServer.prefab
│   │   └── UI/              # UI prefabs
│   │       ├── BrainCanvas.prefab
│   │       ├── DialogueMessage.prefab
│   │       └── ...
│   ├── Scenes/              # Sample scenes
│   └── Settings/            # Configuration assets
│       ├── Brain Settings.asset
│       ├── PersonaConfig.asset
│       └── ...
├── Plugins/                 # Core library
│   └── LlamaBrain.dll
└── Source/
    ├── Editor/              # Editor scripts
    ├── Runtime/             # Runtime scripts
    │   ├── Core/            # Core components
    │   └── Demo/            # Example implementations
    └── Tests/               # Unit tests
```

## 🔄 Integration

### With LlamaBrain Core

This Unity package is built on top of the LlamaBrain core library and provides:
- Unity-specific wrappers and utilities
- ScriptableObject-based configuration
- Unity UI integration
- Editor tools and inspectors

### With Other Unity Systems

LlamaBrain for Unity can integrate with:
- **Unity UI**: Built-in UI system integration
- **TextMeshPro**: Rich text support
- **Audio Systems**: Voice synthesis integration
- **Animation**: Character animation triggers
- **Save Systems**: Persistent conversation data

## 🧪 Testing

The package includes comprehensive tests:

### Edit Mode Tests
- Configuration validation
- Persona profile tests
- Process configuration tests
- Prompt composer tests

### Play Mode Tests
- API client functionality
- Brain server management
- Integration testing

Run tests through Unity's Test Runner window.

## 🎯 Best Practices

### Performance
- Use appropriate context sizes for your use case
- Implement response caching for common queries
- Monitor memory usage with large conversations
- Consider using smaller models for real-time applications

### Security
- Validate all user inputs before sending to AI
- Implement content filtering for AI responses
- Use appropriate rate limiting
- Secure your model files and server

### User Experience
- Provide loading indicators during AI processing
- Implement fallback responses for errors
- Use appropriate response lengths for your UI
- Consider voice synthesis for immersive experiences

## 🐛 Troubleshooting

### Common Issues

**Server Won't Start**
- Check executable path and permissions
- Verify model file exists and is valid
- Check port availability
- Review server logs for errors

**Slow Responses**
- Reduce context size
- Use smaller models
- Check system resources
- Implement response caching

**Memory Issues**
- Limit conversation history
- Implement memory cleanup
- Monitor file sizes
- Use appropriate memory settings

### Debug Mode

Enable debug logging:
```csharp
LlamaBrain.Utilities.Logger.LogLevel = LogLevel.Debug;
```

## 📚 Additional Resources

- [LlamaBrain Core Documentation](../LlamaBrain/README.md)
- [Security Safeguards](../LlamaBrain/Source/Core/SAFEGUARDS.md)
- [Unity Documentation](https://docs.unity3d.com/)
- [llama.cpp Documentation](https://github.com/ggerganov/llama.cpp)

## 📄 License

[Add your license information here]

## 🆘 Support

For issues, questions, or contributions:
- Check the troubleshooting section
- Review existing issues
- Create a new issue with detailed information
- Include Unity version and error logs

---

**Note**: This package requires the LlamaBrain core library and a running llama.cpp server. Ensure you have the appropriate llama.cpp executable and compatible model file before use. 