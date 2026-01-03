# LlamaBrain Scope

## What LlamaBrain Is

**LlamaBrain** is a deterministic neuro-symbolic authoritative state management system for game AI. It enforces a strict validation boundary between untrusted LLM outputs and your authoritative game state.

**Core Philosophy:**
- **The Model is Untrusted**: LLMs are stochastic generators. LlamaBrain treats the LLM as a pure, stateless generator with no direct access to game state.
- **Continuity from Reconstruction**: All continuity emerges from deterministic state reconstruction, not from trusting the AI's memory.
- **Validation Boundary**: Every LLM output is validated against constraints and canonical facts before any memory mutations occur.
- **Authority Hierarchy**: Enforces immutable canonical facts (world truths) that cannot be modified by the LLM.

## Supported Platforms & Versions

- **.NET Standard 2.1+** (compatible with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+)
- **Unity 2022.3 LTS+** (requires .NET Standard 2.1 support, UPM-compatible)

## Supported LLM Providers

- **llama.cpp server** (primary, recommended) - Local execution via HTTP API
- **GGUF model files** - Standard format for local LLM execution

## Determinism Guarantees

LlamaBrain provides **byte-level determinism** for:
1. **State Serialization**: Memory state serializes and reconstructs deterministically at the byte level
2. **Prompt Text Assembly**: Same inputs produce identical byte-level prompt text across runs
3. **Validation Boundary**: All outputs validated before state changes (no hallucinations corrupt state)
4. **State Reconstruction**: Complete context reconstructed from authoritative memory before each inference

**What "Deterministic" Means:**
- Given the same authoritative state and inputs, the system produces identical serialized state
- Prompt assembly is byte-stable and deterministic
- Validation gates enforce consistent behavior
- Memory mutations only occur after validation passes

**What "Deterministic" Does NOT Mean:**
- LLM outputs are not deterministic (they're stochastic by nature)
- Cross-session determinism requires additional features (see Feature 14: Deterministic Generation Seed)
- Hardware-level determinism (CPU/GPU differences may affect results)

## What LlamaBrain Is NOT

1. **General-Purpose LLM Wrapper**: Specifically designed for game AI with authoritative state management, not a generic LLM SDK
2. **Cloud AI Service**: Designed for local execution. Does not provide native support for OpenAI, Anthropic, or other cloud APIs (architecture could support, but out of scope)
3. **Multi-Player Sync Solution**: Manages authoritative state for single-instance game AI. Does not provide network synchronization or distributed state management
4. **Multi-Modal AI System**: Does not support image generation, audio/voice integration, or video processing
5. **Production LLM Hosting**: Does not provide model hosting infrastructure, load balancing, auto-scaling, or model training

## Architectural Boundaries

- **Stateless LLM**: The LLM has no memory between calls—all context is reconstructed
- **Authoritative Memory**: Only validated outputs can mutate memory
- **Validation Gate**: All outputs must pass validation before state changes
- **Deterministic Reconstruction**: State is reconstructed from authoritative sources, not LLM memory

## Supported Use Cases

**In Scope:**
- Game AI: NPCs with persistent memory and deterministic behavior
- Dialogue Systems: Context-aware conversations with validation
- Character Personalities: Unique AI personalities with authoritative state
- Testing Frameworks: Comprehensive LLM testing (RedRoom demo)
- Content Generation: Procedural content with validation boundaries

**Requires Additional Work:**
- Multi-Player Games: Requires additional synchronization layer (not provided)
- Cloud-Based AI: Requires provider-specific integration (architecture supports, but not implemented)
- Production Hosting: Requires infrastructure setup (not provided)

## Getting Help

- [ARCHITECTURE.md](Documentation/ARCHITECTURE.md) - Complete architectural documentation
- [ROADMAP.md](Documentation/ROADMAP.md) - Planned features and enhancements
- [CONTRIBUTING.md](CONTRIBUTING.md) - How to contribute
- [Discord](https://discord.gg/9ruBad4nrN) - Community support
