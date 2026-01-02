# LlamaBrain Managed Host 0.2.0-rc.1

A Unity package that provides seamless integration with the LlamaBrain core library, enabling AI-powered NPCs, dialogue systems, and intelligent game interactions in Unity projects.

## 🎯 Overview

LlamaBrain for Unity brings the power of local AI to Unity games and applications through:

- **AI-Powered NPCs**: Intelligent characters with persistent personalities and memory
- **Dynamic Dialogue Systems**: Natural conversations that adapt to player interactions
- **Determinism Layer**: Expectancy engine with constraint-based behavior control via ScriptableObject rules
- **Structured Memory System**: Authoritative memory with canonical facts, world state, episodic memory, and beliefs
- **State Snapshot & Context Retrieval**: Immutable state snapshots with intelligent context retrieval and retry logic
- **Ephemeral Working Memory**: Bounded working memory for efficient, token-aware prompt assembly
- **Output Validation System**: Constraint-based validation with retry logic and fallback
- **Enhanced Fallback System**: Context-aware fallback responses when inference fails after retries
- **Easy Unity Integration**: Simple setup with ScriptableObject-based configuration
- **Real-time AI Processing**: Local llama.cpp server integration for privacy and performance
- **Basic UI Components**: Simple dialogue interface for testing and prototyping

## 🏗️ Architecture

### Core Components

#### BrainServer (`BrainServer.cs`)
- Manages the llama.cpp server process
- Handles server startup, monitoring, and shutdown
- Provides connection management for multiple clients
- Automatic error recovery and restart capabilities

#### LlamaBrainAgent (`LlamaBrainAgent.cs`)
- Represents individual AI characters/NPCs as Unity MonoBehaviour components
- Manages persona profiles and memory
- Handles conversation state and context
- Provides dialogue methods using `SendPlayerInputAsync()` and `SendWithSnapshotAsync()`
- Integrates with Unity's component system
- Supports expectancy evaluation and constraint injection
- Full validation pipeline with retry logic, constraint escalation, and automatic fallback system

#### Determinism Layer (Component 2)
- **ExpectancyEngine**: Unity MonoBehaviour wrapper with singleton pattern
- **ExpectancyRuleAsset**: ScriptableObject-based rule assets for designers
- **NpcExpectancyConfig**: NPC-specific expectancy configuration
- **ConstraintSet**: Permission, Prohibition, and Requirement constraints

#### Structured Memory System (Component 3)
- **AuthoritativeMemorySystem**: Authority-based memory management
- **Memory Types**: CanonicalFact, WorldState, EpisodicMemory, BeliefMemory
- **Memory Decay**: Automatic episodic memory decay with configurable intervals

#### State Snapshot & Context Retrieval (Component 4)
- **UnityStateSnapshotBuilder**: Unity-specific snapshot builder
- **ContextRetrievalLayer**: Intelligent context retrieval with scoring
- **RetryPolicy**: Configurable retry behavior with constraint escalation
- **ResponseValidator**: Response validation against constraints

#### Ephemeral Working Memory (Component 5)
- **PromptAssemblerSettings**: ScriptableObject for prompt assembly configuration
- **EphemeralWorkingMemory**: Bounded working memory for inference
- **PromptAssembler**: Token-efficient prompt assembly

#### Stateless Inference Core (Component 6)
- **LLM Integration**: Pure stateless inference via llama.cpp server
- **ApiClient**: HTTP client for LLM communication
- **No Memory Access**: LLM has no direct access to memory or world state
- **Bounded Prompts**: Receives only ephemeral, bounded prompt context

#### Output Validation System (Component 7)
- **ValidationRuleAsset**: ScriptableObject-based validation rules
- **ValidationRuleSetAsset**: Groups of validation rules
- **ValidationPipeline**: Complete validation pipeline MonoBehaviour
- **OutputParser**: Parses LLM output into structured format
- **ValidationGate**: Validates output against constraints and canonical facts

#### Memory Mutation & World Effects (Component 8)
- **MemoryMutationController**: Controlled memory mutation execution
- **Mutation Types**: AppendEpisodic, TransformBelief, TransformRelationship, EmitWorldIntent
- **Authority Enforcement**: Cannot override canonical facts
- **WorldIntentDispatcher**: Unity component for dispatching world intents to game systems
- **Explicit Writes**: All memory writes are explicit and validated

#### Enhanced Fallback System (Component 9)
- **AuthorControlledFallback**: Context-aware fallback responses when inference fails
- **FallbackConfig**: Configurable generic, context-aware, and emergency fallbacks
- **FallbackStats**: Comprehensive statistics tracking for fallback usage
- **Automatic Integration**: Seamlessly integrated with retry system in LlamaBrainAgent

#### Configuration System
- **BrainSettings**: Server and LLM configuration
- **PersonaConfig**: Character personality and behavior settings
- **PromptComposerSettings**: Custom prompt building rules
- **PersonaTrait**: Reusable personality trait definitions

#### UI Components
- **DialoguePanelController**: Basic dialogue interface
- **DialogueMessage**: Individual message display component
- **NpcChatCanvas**: Complete dialogue UI prefab

## 📦 Installation

### Requirements
- Unity 2022.3 LTS or higher
- .NET Standard 2.1 support
- **UniTask** (via Unity Package Manager - Git URL: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`)
- **TextMeshPro (TMP)** (via Unity Package Manager)
- **SaveGameFree** (manual import - download from [GitHub Releases](https://github.com/BayatGames/SaveGameFree/releases))
- **Starter Assets - Third Person** (via Unity Asset Store - [download here](https://assetstore.unity.com/packages/essentials/starter-assets-thirdperson-updates-in-new-charactercontroller-pa-196526))
- llama.cpp server executable
- Compatible GGUF model file

**See**: [THIRD_PARTY_PACKAGES.md](Documentation/THIRD_PARTY_PACKAGES.md) for detailed installation instructions.

### Setup
1. Import the LlamaBrainForUnity package into your Unity project
2. Ensure the LlamaBrain.dll is in the Runtime folder
3. Configure your BrainSettings asset
4. Set up your llama.cpp server and model

## 🚀 Quick Start

> **New to LlamaBrain?** Check out the [Quick Start Guide](Documentation/QUICK_START.md) for a step-by-step 5-minute setup tutorial!

### 1. Configure Brain Settings

Create a BrainSettings asset:
1. Right-click in Project window → Create → LlamaBrain → BrainSettings
2. Configure the server settings:
   - **Executable Path**: Path to your llama.cpp server
   - **Model Path**: Path to your GGUF model file
   - **Port**: Server port (default: 5000)
   - **Context Size**: Model context window size

### 2. Create Trait Assets (Optional but Recommended)

Create reusable trait definitions:
1. Right-click → Create → LlamaBrain → Persona Trait
2. Define the trait:
   - **Display Name**: Trait name (e.g., "Wise", "Helpful", "Friendly")
   - **Category**: Organization category (e.g., "Personality", "Skills")
   - **Description**: What this trait means
   - **Default Value**: Default description for this trait
   - **Include in Prompts**: Whether to include in AI prompts

### 3. Create a Persona

Create a PersonaConfig asset:
1. Right-click → Create → LlamaBrain → PersonaConfig
2. Define the character:
   - **Name**: Character name
   - **Description**: Character background and personality
   - **Trait Assignments**: Add traits from your trait assets
   - **System Prompt**: AI behavior instructions
   - **Background**: Character backstory
   - **Memory**: Persistent memory settings

### 4. Set Up an NPC

```csharp
using LlamaBrain.Runtime.Core;
using LlamaBrain.Core;
using LlamaBrain.Persona;

public class MyNPC : MonoBehaviour
{
    [SerializeField] private BrainSettings brainSettings;
    [SerializeField] private PersonaConfig personaConfig;
    
    private LlamaBrainAgent brainAgent;
    private BrainServer brainServer;
    private ApiClient client;
    private PersonaMemoryStore memoryProvider;
    
    async void Start()
    {
        // Set up the brain server
        brainServer = FindObjectOfType<BrainServer>();
        if (brainServer == null)
        {
            var serverObj = new GameObject("BrainServer");
            brainServer = serverObj.AddComponent<BrainServer>();
            brainServer.Settings = brainSettings;
        }
        
        // Initialize the brain agent
        brainAgent = GetComponent<LlamaBrainAgent>();
        brainAgent.PersonaConfig = personaConfig;
        
        // Create client and memory provider
        client = brainServer.CreateClient();
        memoryProvider = new PersonaMemoryFileStore();
        
        // Initialize the agent
        brainAgent.Initialize(client, memoryProvider);
    }
    
    public async void StartConversation(string playerMessage)
    {
        // Get AI response
        string response = await brainAgent.SendPlayerInputAsync(playerMessage);
        Debug.Log($"NPC: {response}");
    }
    
    void OnDestroy()
    {
        // LlamaBrainAgent is a MonoBehaviour, so it's automatically cleaned up
    }
}
```

### 5. Use the Built-in UI

```csharp
using LlamaBrain.Runtime.Demo.UI;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private DialoguePanelController dialoguePanel;
    [SerializeField] private LlamaBrainAgent brainAgent;
    
    void Start()
    {
        // Set up the dialogue panel to handle player messages
        dialoguePanel.onPlayerMessageSubmitted.AddListener(async (message) => {
            string response = await brainAgent.SendPlayerInputAsync(message);
            dialoguePanel.AddNpcMessage(response);
        });
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
| **System Prompt** | AI behavior instructions |
| **Background** | Character backstory |
| **Trait Assignments** | Assigned personality traits |
| **Memory Enabled** | Enable persistent memory |
| **Custom Metadata** | Additional key-value pairs |

### Trait System

The trait system allows you to create reusable personality traits:

| Trait Setting | Description |
|---------------|-------------|
| **Display Name** | Trait name (e.g., "Wise", "Helpful") |
| **Category** | Organization category |
| **Description** | What this trait means |
| **Default Value** | Default description for the trait |
| **Include in Prompts** | Whether to include in AI prompts |
| **Display Order** | Order in lists (lower = first) |

### Prompt Composer Settings

The PromptComposerSettings asset controls how prompts are built and formatted:

| Setting | Description | Default |
|---------|-------------|---------|
| **NPC Template** | Template for NPC section | `NPC: {personaName}` |
| **Description Template** | Template for description section | `{description}` |
| **Memory Header Template** | Template for memory section header | `Memory:` |
| **Memory Item Template** | Template for individual memory items | `- {memory}` |
| **Dialogue Header Template** | Template for dialogue history header | `History:` |
| **Player Template** | Template for player section | `Player: {playerName}` |
| **Player Input Template** | Template for player input | `> {playerInput}` |
| **Response Prompt Template** | Template for NPC response prompt | `{personaName}:` |
| **Player Name** | Default player name | `Adventurer` |
| **Include Empty Memory** | Include memory section even if empty | `false` |
| **Include Empty Dialogue** | Include dialogue section even if empty | `false` |
| **Max Memory Items** | Maximum memory items to include | `5` |
| **Max Dialogue Lines** | Maximum dialogue history lines | `10` |
| **Section Separator** | Separator between sections | `\n` |
| **Compact Dialogue Format** | Use compact dialogue format | `true` |
| **Dialogue Line Prefix** | Prefix for dialogue lines | `""` |
| **Include Personality Traits** | Include personality traits in prompts | `true` |

> **Note**: The **Include Personality Traits** setting controls whether personality traits are included in prompts sent to the LLM. When enabled, traits are included for context but the AI is instructed not to mention them in responses. When disabled, traits are completely excluded from prompts.

## 🎮 Usage Examples

### Basic NPC Interaction

```csharp
public class SimpleNPC : MonoBehaviour
{
    [SerializeField] private BrainSettings settings;
    [SerializeField] private PersonaConfig persona;
    
    private LlamaBrainAgent agent;
    private BrainServer server;
    private ApiClient client;
    private PersonaMemoryStore memory;
    
    async void Start()
    {
        // Set up server
        var serverObj = new GameObject("BrainServer");
        server = serverObj.AddComponent<BrainServer>();
        server.Settings = settings;
        
        // Set up agent
        agent = GetComponent<LlamaBrainAgent>();
        agent.PersonaConfig = persona;
        
        // Initialize
        client = server.CreateClient();
        memory = new PersonaMemoryFileStore();
        agent.Initialize(client, memory);
    }
    
    public async void TalkToPlayer(string playerInput)
    {
        string response = await agent.SendPlayerInputAsync(playerInput);
        // Handle the response (display in UI, play audio, etc.)
        DisplayResponse(response);
    }
    
    private void DisplayResponse(string response)
    {
        Debug.Log($"NPC Response: {response}");
        // Add your UI display logic here
    }
}
```

### Advanced Dialogue System

```csharp
public class AdvancedDialogue : MonoBehaviour
{
    [SerializeField] private LlamaBrainAgent agent;
    [SerializeField] private DialoguePanelController ui;
    
    void Start()
    {
        // Set up UI callbacks
        ui.onPlayerMessageSubmitted.AddListener(async (message) => {
            var response = await agent.SendPlayerInputAsync(message);
            ui.AddNpcMessage(response);
        });
    }
    
    public void ClearConversation()
    {
        agent.ClearDialogueHistory();
        // Clear UI messages here
    }
}
```

### Multiple NPCs

```csharp
public class NPCManager : MonoBehaviour
{
    [SerializeField] private BrainSettings settings;
    [SerializeField] private PersonaConfig[] personas;
    
    private Dictionary<string, LlamaBrainAgent> npcs = new();
    private BrainServer server;
    private ApiClient client;
    private PersonaMemoryStore memory;
    
    async void Start()
    {
        // Set up shared server
        var serverObj = new GameObject("BrainServer");
        server = serverObj.AddComponent<BrainServer>();
        server.Settings = settings;
        
        client = server.CreateClient();
        memory = new PersonaMemoryFileStore();
        
        // Create NPCs
        foreach (var persona in personas)
        {
            var agent = GetComponent<LlamaBrainAgent>();
            agent.PersonaConfig = persona;
            agent.Initialize(client, memory);
            npcs[persona.Name] = agent;
        }
    }
    
    public async Task<string> TalkToNPC(string npcName, string message)
    {
        if (npcs.TryGetValue(npcName, out var agent))
        {
            return await agent.SendPlayerInputAsync(message);
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
- Send button
- Message type indicators (Player/NPC)
- Basic conversation view

### Custom UI Integration

```csharp
public class CustomDialogueUI : MonoBehaviour
{
    [SerializeField] private LlamaBrainAgent agent;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Text responseText;
    
    public async void SendMessage()
    {
        string message = inputField.text;
        if (string.IsNullOrEmpty(message)) return;
        
        inputField.text = "";
        responseText.text = "Thinking...";
        
        string response = await agent.SendPlayerInputAsync(message);
        responseText.text = response;
    }
}
```

## 📁 Project Structure

```
LlamaBrainForUnity/
├── Runtime/                 # Runtime scripts and DLL
│   ├── LlamaBrain.dll      # Core library
│   ├── Core/               # Core components
│   │   ├── LlamaBrainAgent.cs
│   │   ├── BrainServer.cs
│   │   ├── BrainSettings.cs
│   │   ├── PersonaConfig.cs
│   │   ├── PersonaTrait.cs
│   │   └── PromptComposerSettings.cs
│   └── Demo/               # Example implementations
│       ├── UI/             # UI components
│       │   ├── DialoguePanelController.cs
│       │   └── DialogueMessage.cs
│       └── NpcAgentExample.cs
├── Editor/                 # Editor scripts
│   ├── BrainSettingsEditor.cs
│   ├── PersonaConfigEditor.cs
│   └── PromptComposerSettingsEditor.cs
├── Samples/                # Sample scenes and assets
│   ├── Shared/             # Shared assets
│   │   ├── Prefabs/        # Unity prefabs
│   │   │   ├── BrainAgent.prefab
│   │   │   ├── BrainServer.prefab
│   │   │   └── UI/         # UI prefabs
│   │   └── Settings/       # Configuration assets
│   └── [Sample Scenes]/    # Example scenes
└── Tests/                  # Unit tests
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

### Building
- Include the LlamaBrain.DLL, your model, and your backend server in your build.
- It is helpful to keep a folder of deployables in the same structure as is specified in your config.
- I highly recommend automating this process!

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

## 🏗️ Development (Architectural Pattern Implementation)

### Phase Status

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

**Feature 8: RedRoom Integration** 🚧 99% Complete
- Validation metrics and export ✅
- Architectural pattern testing ✅
- End-to-end validation scenarios ✅
- Testing overlays ✅ (Memory Mutation Overlay, Validation Gate Overlay - minor fixes needed)
- Unity PlayMode integration tests ✅ (73+ tests complete)
- Full Pipeline Integration Tests ✅ (8 tests complete)

**Feature 9: Documentation** ✅ 100% Complete
- Architecture documentation with diagrams ✅
- Setup tutorials for new components ✅
- API reference for all layers ✅ (100% XML documentation, zero missing member warnings)
- Few-shot prompt priming ✅ Complete (30 tests, full integration)
- Tutorial content ✅ Complete (4 comprehensive step-by-step tutorials)

**Feature 10: Deterministic Proof Gap Testing** ✅ Complete (100% Complete)
- ✅ All 5 Critical Requirements implemented (strict total order sorting, SequenceNumber field, tie-breaker logic, OutputParser normalization, WorldIntentDispatcher singleton lifecycle)
- ✅ 7 high-leverage determinism tests added
- ✅ ContextRetrievalLayer: 55 tests complete (exceeds estimate)
- ✅ PromptAssembler: 40 tests complete
- ✅ EphemeralWorkingMemory: 40 tests complete
- ✅ OutputParser: 86 tests complete (includes normalization contract)
- ✅ ValidationGate: 44 tests complete (17 basic + 27 detailed tests)
- ✅ MemoryMutationController: 41 tests complete
- ✅ WorldIntentDispatcher: 28 tests complete (Requirement #5 singleton lifecycle)
- ✅ Full Pipeline deterministic tests: 25 tests complete (17 DeterministicPipelineTests + 8 FullPipelineIntegrationTests)
- **Total: 351 tests** (exceeds original estimate of 150-180)
- **Determinism proof now defensible at byte level** for serialized state and prompt text assembly

### Future Features (Post-Architecture)

- **Multi-Modal Support**: Image and audio integration
- **Performance Optimization**: Layer-specific optimizations
- **Voice Integration**: Text-to-speech and speech-to-text
- **Animation Integration**: Character animation triggers
- **Multi-Player Support**: Shared world state with validation
- **Advanced Analytics**: Validation and constraint visualization

For detailed status information, see the [STATUS.md](../../Documentation/STATUS.md) file in the Documentation directory.

## 📚 Additional Resources

- [Unity Documentation](https://docs.unity3d.com/)
- [llama.cpp](https://github.com/ggerganov/llama.cpp)
- [UniTask](https://github.com/Cysharp/UniTask)
- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/)
- [Stability Zephyr 3b Model](https://huggingface.co/stabilityai/stablelm-zephyr-3b)
- [LlamaBrain Discord](https://discord.gg/9ruBad4nrN)
- [Quick Start Guide](https://youtu.be/1EtU6qu7O5Q)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE.md](../../../LICENSE.md) file for details.

## 🆘 Support

For issues, questions, or contributions:
- Check the troubleshooting section
- Review existing issues
- Create a new issue with detailed information
- Include Unity version and error logs

---

**Note**: This package requires the LlamaBrain core library and a running llama.cpp server. Ensure you have the appropriate llama.cpp executable and compatible model file before use. 

