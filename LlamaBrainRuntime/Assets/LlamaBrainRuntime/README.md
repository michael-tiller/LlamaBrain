# LlamaBrain Unity Runtime 0.3.0-rc.1

Unity integration for the LlamaBrain core library, enabling AI-powered NPCs with deterministic state management.

## Overview

LlamaBrain for Unity provides:
- **AI-Powered NPCs**: Intelligent characters with persistent personalities and memory
- **Voice Integration**: Speech-to-text (Whisper) and text-to-speech (Piper) for natural conversations
- **ScriptableObject Configuration**: Designer-friendly setup via BrainSettings, PersonaConfig, ExpectancyRuleAsset
- **Validation Pipeline**: Constraint-based validation with retry logic and fallback
- **Local AI Processing**: llama.cpp server integration for privacy and performance

## Requirements

- Unity 6000.0.58f2 LTS
- **UniTask**: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
- **TextMeshPro** (via Package Manager)
- llama.cpp server executable + GGUF model

### Voice System (Optional)
- **whisper.unity**: Speech-to-text + Whisper model (ggml-base.en recommended)
- **uPiper**: Text-to-speech + Unity Sentis + ONNX voice model

See [THIRD_PARTY_PACKAGES.md](Documentation/THIRD_PARTY_PACKAGES.md) for detailed setup.

## Quick Start

> **New to LlamaBrain?** See the [Quick Start Guide](Documentation/QUICK_START.md) for a 5-minute setup tutorial.

### 1. Configure Brain Settings
Right-click in Project → Create → LlamaBrain → BrainSettings
- Set executable path, model path, port, context size

### 2. Create a Persona
Right-click → Create → LlamaBrain → PersonaConfig
- Define name, description, traits, system prompt

### 3. Set Up an NPC

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

    void Start()
    {
        InitializeAsync().Forget();
    }

    private async UniTaskVoid InitializeAsync()
    {
        // Set up server
        brainServer = FindObjectOfType<BrainServer>();
        if (brainServer == null)
        {
            var serverObj = new GameObject("BrainServer");
            brainServer = serverObj.AddComponent<BrainServer>();
            brainServer.Settings = brainSettings;
        }

        // Initialize agent
        brainAgent = GetComponent<LlamaBrainAgent>();
        brainAgent.PersonaConfig = personaConfig;
        brainAgent.Initialize(brainServer.CreateClient(), new PersonaMemoryFileStore());
    }

    public void TalkTo(string message)
    {
        TalkToAsync(message).Forget();
    }

    private async UniTaskVoid TalkToAsync(string message)
    {
        string response = await brainAgent.SendPlayerInputAsync(message);
        Debug.Log($"NPC: {response}");
    }
}
```

## Core Components

| Component | Description |
|-----------|-------------|
| **BrainServer** | Manages llama.cpp server lifecycle |
| **LlamaBrainAgent** | MonoBehaviour for AI characters |
| **BrainSettings** | ScriptableObject for server config |
| **PersonaConfig** | ScriptableObject for character definition |
| **PersonaTrait** | Reusable personality trait assets |
| **ExpectancyRuleAsset** | Designer-created behavior constraints |
| **ValidationPipeline** | Complete validation pipeline component |
| **WorldIntentDispatcher** | Routes world intents to game systems |
| **NpcVoiceController** | Coordinates voice input/output |
| **NpcVoiceInput** | Whisper STT with VAD |
| **NpcVoiceOutput** | Piper TTS with caching |
| **NpcSpeechConfig** | Per-NPC voice settings |

## Configuration

### Brain Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Executable Path | Path to llama.cpp server | - |
| Model Path | Path to GGUF model | - |
| Port | Server port | 5000 |
| Context Size | Context window | 2048 |
| Max Tokens | Response length | 64 |
| Temperature | Randomness | 0.7 |

### Persona Config

| Setting | Description |
|---------|-------------|
| Name | Character name |
| Description | Background and personality |
| Trait Assignments | Personality traits |
| System Prompt | AI behavior instructions |

## Voice System

LlamaBrain includes local voice integration for natural NPC conversations:

### Speech-to-Text (Whisper)
- **Package**: whisper.unity with Silero VAD
- **Features**: VAD-gated batch transcription, stereo-to-mono conversion, artifact filtering
- **Model**: ggml-base.en (GPU-accelerated via Vulkan)

### Text-to-Speech (Piper)
- **Package**: uPiper with Unity Sentis
- **Features**: Async generation, LRU audio caching, spatial audio, per-NPC voices
- **Models**: ONNX voice models (e.g., en_US-lessac-medium)

### Setup
1. Add `NpcVoiceController` to your NPC GameObject
2. Create `NpcSpeechConfig` asset (Create → LlamaBrain → NpcSpeechConfig)
3. Assign voice model and configure settings
4. Voice integrates automatically with `LlamaBrainAgent`

See [USAGE_GUIDE.md](Documentation/USAGE_GUIDE.md) for detailed voice setup.

## Project Structure

```
LlamaBrainForUnity/
├── Runtime/
│   ├── LlamaBrain.dll          # Core library
│   ├── Core/                   # BrainServer, LlamaBrainAgent, etc.
│   │   └── Voice/              # NpcVoiceController, NpcVoiceInput, NpcVoiceOutput
│   └── Demo/                   # Example UI components
├── Editor/                     # Custom inspectors
├── Samples/                    # Example scenes and assets
└── Tests/                      # Edit/Play mode tests
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Server won't start | Check executable path, model exists, port available |
| Slow responses | Reduce context size, use smaller model |
| Memory issues | Limit conversation history, cleanup old files |

## Development Status

All 9 architectural components implemented. See **[STATUS.md](../../Documentation/STATUS.md)** for feature status and **[COVERAGE_REPORT.md](../../LlamaBrain/COVERAGE_REPORT.md)** for test metrics.

## Resources

- [Quick Start Video](https://youtu.be/1EtU6qu7O5Q)
- [LlamaBrain Discord](https://discord.gg/9ruBad4nrN)
- [llama.cpp](https://github.com/ggerganov/llama.cpp)
- [UniTask](https://github.com/Cysharp/UniTask)

## License

MIT License - see [LICENSE.md](../../../LICENSE.md)
