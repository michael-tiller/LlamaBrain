# LlamaBrain for Unity - Quick Start Guide

🚀 **Get AI-powered NPCs and dialogue systems running in Unity in minutes!**

LlamaBrain for Unity brings local AI to your Unity projects with intelligent NPCs, dynamic dialogue systems, and AI-powered game interactions.

## 📋 Prerequisites

Before you start, make sure you have:

- **Unity 6000.0.58f2 LTS**
- **TextMeshPro package** (installed via Package Manager)
- **UniTask**
- **Newtonsoft.Json**
- **llama.cpp server executable** (download from [llama.cpp releases](https://github.com/ggerganov/llama.cpp/releases))
- **GGUF model file** (e.g., [Qwen2.5-3B-Instruct-abliterated-SFT-Q4_K_M-GGUF](https://huggingface.co/Triangle104/Qwen2.5-3B-Instruct-abliterated-SFT-Q4_K_M-GGUF))

## ⚡ Quick Setup (5 Minutes)

### 1. Import the Package
1. Open your Unity project
2. Go to **Window → Package Manager**
3. Click **+ → Add package from disk**
4. Select the `package.json` file from this folder
5. Import all samples when prompted

### 2. Configure Brain Settings
1. Right-click in Project window → **Create → LlamaBrain → BrainSettings**
2. Configure the settings:
   - **Executable Path**: Path to your llama.cpp server (e.g., `C:\llama\server.exe`)
   - **Model Path**: Path to your GGUF model file (e.g., `C:\models\qwen2.5-3b-instruct-abliterated-sft-q4_k_m.gguf`)
   - **Port**: `5000` (default)
   - **Context Size**: `2048` (adjust based on your model)

### 3. Create Your First NPC
1. Right-click → **Create → LlamaBrain → PersonaConfig**
2. Set the character details:
   - **Name**: "Wise Sage"
   - **Description**: "A knowledgeable and friendly guide who helps adventurers"
   - **System Prompt**: "You are a wise sage who provides helpful advice to travelers. Be friendly and informative."

### 4. Set Up the Scene
1. Open the **GettingStarted** sample scene
2. Select the **BrainServer** GameObject
3. Assign your **BrainSettings** asset to the Settings field
4. Select the **LlamaBrainAgent** GameObject
5. Assign your **PersonaConfig** asset to the Persona Config field

### 5. Test It!
1. Press **Play** in Unity
2. Type a message in the dialogue input
3. Press **Send** to see your AI NPC respond!

## 🎮 Basic Usage

### Simple NPC Script
```csharp
using LlamaBrain.Runtime.Core;

public class SimpleNPC : MonoBehaviour
{
    [SerializeField] private BrainSettings brainSettings;
    [SerializeField] private PersonaConfig personaConfig;
    
    private LlamaBrainAgent brainAgent;
    private BrainServer brainServer;
    
    async void Start()
    {
        // Set up server
        var serverObj = new GameObject("BrainServer");
        brainServer = serverObj.AddComponent<BrainServer>();
        brainServer.Settings = brainSettings;
        
        // Set up agent
        brainAgent = GetComponent<LlamaBrainAgent>();
        brainAgent.PersonaConfig = personaConfig;
        
        // Initialize
        var client = brainServer.CreateClient();
        var memory = new PersonaMemoryFileStore();
        brainAgent.Initialize(client, memory);
    }
    
    public async void TalkToPlayer(string message)
    {
        string response = await brainAgent.SendPlayerInputAsync(message);
        Debug.Log($"NPC: {response}");
    }
}
```

### Using the Built-in UI
```csharp
using LlamaBrain.Runtime.Demo.UI;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private DialoguePanelController dialoguePanel;
    [SerializeField] private LlamaBrainAgent brainAgent;
    
    void Start()
    {
        dialoguePanel.onPlayerMessageSubmitted.AddListener(async (message) => {
            string response = await brainAgent.SendPlayerInputAsync(message);
            dialoguePanel.AddNpcMessage(response);
        });
    }
}
```

## 🎯 Common Use Cases

### 🎮 Game NPCs
- **Multiple NPCs**: Create different PersonaConfig assets for each character
- **Memory**: NPCs remember conversations across sessions
- **Personality Traits**: Add traits like "Wise", "Friendly", "Mysterious"

### 📝 Content Generation
- **Flavor Text**: Generate dynamic descriptions for game events
- **Story Writing**: AI-powered narrative generation
- **Character Dialogue**: Dynamic conversations based on context

### 🔧 Utility Applications
- **Debugging Assistant**: AI help for development
- **Documentation Writer**: Generate code comments and docs
- **Testing Helper**: Automated test scenario generation

## 🛠️ Configuration Tips

### Performance Settings
- **Context Size**: Smaller = faster, larger = more memory
- **Max Tokens**: Limit response length for real-time applications
- **Temperature**: Lower = more consistent, higher = more creative

### Memory Management
- **Memory Enabled**: Turn on for persistent conversations
- **Max Memory Items**: Limit to prevent memory bloat
- **Memory Categories**: Organize memories by type
- **Automatic Memory Decay**: Enable in LlamaBrainAgent inspector to fade old memories over time
- **Canonical Facts**: Initialize immutable world truths for NPCs (see Usage Guide)

### Security
- **Input Validation**: Always validate user inputs
- **Content Filtering**: Implement response filtering
- **Rate Limiting**: Prevent abuse in multiplayer scenarios

## 🚨 Troubleshooting

### Server Won't Start
- ✅ Check executable path and permissions
- ✅ Verify model file exists and is valid
- ✅ Ensure port 5000 is available
- ✅ Check server logs in Console

### Slow Responses
- ✅ Reduce context size (try 1024 or 512)
- ✅ Use smaller models (3B instead of 7B+)
- ✅ Check system resources (CPU/RAM)
- ✅ Implement response caching

### Memory Issues
- ✅ Limit conversation history length
- ✅ Clear memory periodically
- ✅ Monitor file sizes in memory store
- ✅ Use appropriate memory settings

## 🚀 Advanced Features

LlamaBrain includes powerful advanced features for production-ready NPCs:

- **Expectancy Engine (Phase 1)**: Rule-based constraint system for deterministic NPC behavior
  - Create rules that prevent NPCs from breaking character
  - Context-aware constraints based on triggers, scenes, and interactions
  - See [Usage Guide](Documentation/USAGE_GUIDE.md#phase-1-expectancy-engine-usage) for details

- **Structured Memory System (Phase 2)**: Advanced memory management with authority levels
  - Canonical facts (immutable world truths)
  - World state tracking
  - Episodic memory with automatic decay
  - Beliefs and relationships
  - See [Usage Guide](Documentation/USAGE_GUIDE.md#phase-2-structured-memory-system-usage) for details

## 📚 Next Steps

1. **Explore Samples**: Try all the sample scenes to see different use cases
2. **Read Unity Documentation**: Check [README.md](../README.md) for detailed documentation
3. **Learn Advanced Features**: See [Usage Guide](USAGE_GUIDE.md) for Expectancy Engine and Structured Memory System
4. **Join Community**: Get help on [Discord](https://discord.gg/9ruBad4nrN)
5. **Watch Tutorial**: [Quick Start Video](https://youtu.be/1EtU6qu7O5Q)

## 🎁 Sample Scenes Included

- **GettingStarted**: Basic setup and configuration
- **NpcPersonaChat**: Advanced NPC conversations
- **JournalWriter**: AI writing assistant
- **FlavorText**: Dynamic text generation
- **ThemedRewrite**: Content style transformation
- **MemoryManagement**: Advanced memory systems
- **ClientManagement**: Server monitoring tools

## 🔗 Resources

- [Unity Documentation](../README.md)
- [Discord Community](https://discord.gg/9ruBad4nrN)
- [Video Tutorial](https://youtu.be/1EtU6qu7O5Q)
- [Unity API](http://metagrue.com/docs/llamabrainruntimeapi/)
- [Core API](http://metagrue.com/docs/llamabrainapi/)

---

**Need help?** Check the troubleshooting section, join our Discord, or create an issue with detailed information about your setup and error logs.

**Happy coding! 🎮✨**

