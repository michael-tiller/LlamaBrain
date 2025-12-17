# LlamaBrain – Local NPC AI Runtime (Overview)

Engine-agnostic NPC / dialogue runtime built on top of a local `llama.cpp` server.

**Status**: experimental / prototype 

## What This Is / Is Not

This repo:

- Describes the architecture and core concepts behind LlamaBrain.
- Documents key implementation classes and data structures used in the private runtime.

It does **not** include:

- The full production implementation of LlamaBrain.
- Any Unity-specific integration code.
- The `llama.cpp` binary or models.

The full implementation is private and can be shared with hiring teams on request.

## Goals

LlamaBrain is designed to:

- Run **entirely on a local machine** using a `llama.cpp` server.
- Provide NPC dialogue generation with persona-based behavior.
- Be **engine-agnostic**:
  - No Unity types in the core.
  - Integrates via thin adapters in any engine.
- Own the **lifecycle of the LLM process** (start, health-check, restart, shutdown).
- Return raw text responses from the LLM with conversation history management.

## Conceptual Model

LlamaBrain works with a small set of core concepts:

- **Persona**  
  An NPC with identity, personality traits, background, and a system prompt. Represented by `PersonaProfile`.

- **Dialogue Session**  
  Tracks conversation history between a player and a persona. Maintains a list of dialogue entries with speaker, text, and timestamps.

- **Memory Store**  
  Persistent memory for personas, allowing them to remember past interactions across sessions.

- **Prompt**  
  Text representation built from persona profile, dialogue history, and current user input. Composed by `PromptComposer`.

- **Response**  
  Raw text output from the LLM. No validation, constraint, or structured output system is currently implemented.

## Architecture

LlamaBrain is split into three layers:

1. **Process / Transport Layer**
   - `ServerManager`: Starts and supervises the local `llama.cpp` server process.
   - `ApiClient`: Communicates with `llama.cpp` over HTTP.
   - `ClientManager`: Manages API client lifecycle and health checks.
   - Performs health checks and process management.

2. **Core Runtime (Engine-Agnostic)**
   - `BrainAgent`: High-level agent for managing persona interactions.
   - `PersonaProfile`: Stores persona identity, traits, background, and system prompts.
   - `DialogueSession`: Tracks conversation history between player and persona.
   - `PromptComposer`: Builds prompts from persona profiles and dialogue history.
   - `PersonaMemoryStore`: Manages persistent memory for personas.
   - Returns raw text responses from the LLM.

3. **Host Adapters (Unity, etc.)**
   - Translate engine/game events into persona interactions.
   - Display dialogue text in game UI.
   - Hook into host lifecycle to start/stop the process layer.

## Local llama.cpp Management

LlamaBrain assumes a local `llama.cpp` server running on the same machine as the game or editor.

**Configuration**

- Binary path (e.g., `llama-server`).
- Model path(s).
- Host/port.
- Context window, max tokens, and sampling parameters.
- Startup mode: `Manual` (external) or `Managed` (LlamaBrain spawns it).

**Startup**

- On host startup (e.g., editor boot or game launch):
  - If in `Managed` mode:
    - Check whether the server is already running.
    - If not, spawn `llama.cpp` with configured arguments.
  - Wait for readiness via a health endpoint or test call.

**Health Checks**

- Periodic pings or lightweight prompts to:
  - Confirm the server is alive.
  - Detect timeouts or crashes.
- Configurable retry and backoff.

**Restart and Shutdown**

- On failure:
  - Attempt automatic restart up to N times.
  - Log failures via the host logging framework.
- On host shutdown:
  - Gracefully stop the `llama.cpp` process (or kill as last resort).

## Example Flow – Room Entry Dialogue

A typical integration looks like this:

1. The player enters a room containing an NPC.
2. The game publishes a `PlayerEnteredRoom` event.
3. A host-specific adapter handles the event and:
   - Creates or retrieves a `PersonaProfile` for the NPC.
   - Creates a `BrainAgent` with the profile and an `ApiClient`.
   - Optionally loads persona memory from `PersonaMemoryStore`.
4. The adapter calls `BrainAgent.SendMessageAsync("Hello")`:
   - `BrainAgent` uses `PromptComposer` to build a prompt from:
     - Persona profile (name, description, traits, background, system prompt).
     - Dialogue history from `DialogueSession`.
     - Current user input.
   - `BrainAgent` sends the prompt to `llama.cpp` via `ApiClient`.
   - Receives raw text response.
   - Appends the exchange to `DialogueSession`.
5. The host adapter:
   - Displays the response text in the dialogue UI.
   - Optionally stores important information in persona memory.

## Engine Integration (Unity Example)

LlamaBrain is engine-agnostic. A Unity integration typically:

- Registers LlamaBrain services in the DI container (e.g., VContainer).
- Subscribes to game events (player enters room, talks to NPC, quest updates).
- Creates `PersonaProfile` instances for NPCs.
- Creates `BrainAgent` instances with `ApiClient` and `PersonaMemoryStore`.
- Calls `BrainAgent.SendMessageAsync()` and waits for the text response.
- Displays the response text in:
  - Dialogue UI.
  - Subtitle / bark systems.
- Optionally extracts game state changes from the response text (manual parsing required).

The core LlamaBrain runtime never references Unity types; only the adapter layer knows about scenes, GameObjects, or MonoBehaviours.

## Status

**Implemented**

- Managed `llama.cpp` process lifecycle (`ServerManager`: start, health checks, shutdown).
- HTTP client for `llama.cpp` API (`ApiClient`, `ClientManager`).
- Persona system (`PersonaProfile`, `PersonaProfileManager`).
- Dialogue session tracking (`DialogueSession`).
- Prompt composition (`PromptComposer`).
- Memory store for personas (`PersonaMemoryStore`, `PersonaMemoryFileStore`).
- High-level agent interface (`BrainAgent`).
- Structured JSON output support (via prompt engineering, not validation).
- Engine-agnostic core (no Unity dependencies).

**Not Implemented**

- Output validation or constraint system.
- Verb/intent extraction or structured output validation.
- Expectation rules or safety filters.
- Conversation context DTOs.
- Automatic game state extraction from responses.

## License

The documentation in this overview repo is released under the MIT License.

The full LlamaBrain implementation is private and not covered by this license.

See `LICENSE` for details.

## Samples

The `Samples/` directory contains working C# examples demonstrating various aspects of LlamaBrain:

- **BasicApiClientExample.cs**  
  Direct API client usage - the simplest way to interact with llama.cpp. Shows basic prompt/response patterns.

- **PersonaAgentExample.cs**  
  Creating personas with personality traits and having conversations. Demonstrates `BrainAgent` and `PersonaProfile` usage.

- **ServerManagementExample.cs**  
  Managing the llama.cpp server process lifecycle. Shows how to start, monitor, validate, and stop the server programmatically.

- **PersonaMemoryExample.cs**  
  Persistent memory storage across game sessions. Demonstrates how personas can remember past interactions using `PersonaMemoryFileStore`.

- **PersonaProfileManagerExample.cs**  
  Saving and loading persona profiles from disk. Useful for creating NPCs in a game editor and loading them at runtime.

- **CompleteIntegrationExample.cs**  
  Full integration workflow combining all components: server management, persona creation, dialogue sessions, and memory persistence.

All samples use the actual implementation classes and can be used as reference code for integration.

## Repository Layout

```text
README.md                  # This overview
LICENSE                    # MIT license
Samples/                   # Working C# code examples
