# LlamaBrain Core Library 0.3.0-rc.1

**Deterministic State Management for Stochastic AI**

A .NET Standard 2.1 library for integrating with llama.cpp servers. LlamaBrain implements an authoritative memory system that prevents LLM hallucinations from corrupting game state by enforcing a strict validation gate between model output and runtime database.

## The Problem

LLMs are stochastic - they can hallucinate, contradict themselves, and generate outputs that would break game logic. Traditional approaches either accept this chaos or severely limit what AI can do.

## The Solution

LlamaBrain treats the LLM as a **pure, stateless generator** while maintaining continuity through **deterministic state reconstruction**. An authoritative memory system with a strict validation gate ensures that LLM outputs cannot corrupt game state.

![Architectural Diagram](../Documentation/architectural_diagram.png)

## Key Features

- **Authoritative Memory**: Four-tier memory hierarchy (Canonical Facts > World State > Episodic > Beliefs) with strict authority enforcement
- **Validation Gate**: Multi-layer validation checks outputs against constraints and canonical facts before any state mutation
- **Determinism Layer**: Expectancy engine generates constraints based on interaction context
- **Structured Output**: Native llama.cpp JSON schema support with automatic parsing
- **Fallback System**: Context-aware fallback responses when inference fails
- **Cross-Session Determinism**: Seed-based reproducibility with InteractionCount tracking

## Installation

### Requirements
- .NET Standard 2.1 or higher
- llama.cpp server executable
- Compatible GGUF model file

### Dependencies
```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

## Quick Start

```csharp
using LlamaBrain.Core;
using LlamaBrain.Persona;

// Create a persona
var profile = PersonaProfile.Create("wizard", "Gorblaf the Grey-Green");
profile.Description = "A wise and powerful wizard";
profile.SetTrait("wise", "Very knowledgeable about magic and history");

// Create API client and brain agent
var client = new ApiClient("localhost", 8080, "model.gguf");
using var agent = new BrainAgent(profile, client);

// Send a message
var response = await agent.SendMessageAsync("Tell me about the ancient tower");
```

For advanced usage with the full architectural pattern (expectancy constraints, state snapshots, validation gates, memory mutations), see **[USAGE_GUIDE.md](../Documentation/USAGE_GUIDE.md)**.

## Configuration

### LLM Parameters (`LlmConfig`)

| Parameter | Range | Default | Description |
|-----------|-------|---------|-------------|
| `MaxTokens` | 1-2048 | 64 | Maximum tokens to generate |
| `Temperature` | 0.0-2.0 | 0.7 | Randomness control |
| `TopP` | 0.0-1.0 | 0.9 | Nucleus sampling |
| `TopK` | 1-100 | 40 | Top-k sampling |
| `RepeatPenalty` | 0.0-2.0 | 1.1 | Repetition penalty |

### Server Configuration (`ProcessConfig`)

| Parameter | Range | Default | Description |
|-----------|-------|---------|-------------|
| `Port` | 1-65535 | 5000 | Server port |
| `ContextSize` | 512-32768 | 2048 | Context window size |
| `ExecutablePath` | - | - | Path to llama.cpp server |
| `Model` | - | - | Path to GGUF model file |

## Security

LlamaBrain implements comprehensive security measures including input validation, rate limiting (60 req/min), path traversal prevention, and atomic file operations. See **[SAFEGUARDS.md](../Documentation/SAFEGUARDS.md)** for details.

## Testing

See **[COVERAGE_REPORT.md](./COVERAGE_REPORT.md)** for current test metrics and coverage.

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat code coverage" --settings coverlet.runsettings
```

## Documentation

- **[ARCHITECTURE.md](../Documentation/ARCHITECTURE.md)** - Complete 9-component architectural pattern
- **[MEMORY.md](../Documentation/MEMORY.md)** - Memory system with authority hierarchy
- **[PIPELINE_CONTRACT.md](../Documentation/PIPELINE_CONTRACT.md)** - Formal pipeline contract
- **[VALIDATION_GATING.md](../Documentation/VALIDATION_GATING.md)** - Validation gating system
- **[USAGE_GUIDE.md](../Documentation/USAGE_GUIDE.md)** - Practical examples and best practices
- **[DETERMINISM_CONTRACT.md](../Documentation/DETERMINISM_CONTRACT.md)** - Determinism boundaries
- **[ROADMAP.md](../Documentation/ROADMAP.md)** - Feature roadmap and status
- **[STATUS.md](../Documentation/STATUS.md)** - Current implementation status

## License

MIT License - see [LICENSE](LICENSE) for details.

---

**Note**: This library requires a running llama.cpp server. Ensure you have the appropriate llama.cpp executable and compatible model file before use.
