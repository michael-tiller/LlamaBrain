# Changelog

All notable changes to LlamaBrain will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0-rc.3] (Unreleased)

#### Fixed
- Fixes an issue with the CI pipeline not firing properly.

## [0.3.0-rc.2] 2026-01-07

### Unity Runtime

#### Added
- **Voice System Integration (Features 31-32)** 🚧
  - **Feature 31: Whisper Speech-to-Text Integration** - Voice input for NPCs
    - **NpcVoiceInput** - Microphone-based voice input with Whisper.unity integration
    - Speech-to-text transcription for player dialogue
    - Voice activity detection and silence detection
  - **Feature 32: Piper Text-to-Speech Integration** - Voice output for NPCs
    - **NpcVoiceOutput** - Text-to-speech output system with Piper.unity integration
    - Natural voice synthesis for NPC responses
    - Per-NPC voice model configuration
  - **NpcVoiceController** - Central voice input/output management coordinator
  - **NpcSpeechConfig** - ScriptableObject configuration for voice settings
  - Integration with LlamaBrainAgent for voice-enabled NPCs
  - Files Added:
    - `Runtime/Core/Voice/NpcVoiceController.cs`
    - `Runtime/Core/Voice/NpcVoiceInput.cs`
    - `Runtime/Core/Voice/NpcVoiceOutput.cs`
    - `Runtime/Core/Voice/NpcSpeechConfig.cs`
- **Game State Management UI (Feature 16 Extension)** ✅ **COMPLETE**
  - **RedRoomGameController** (15 lines) - Singleton game state management
  - **MainMenu** (26 lines) - Main menu with continue/new game/load game, dynamic button states
  - **LoadGameMenu** (133 lines) - Save browser with delete confirmation, empty state handling
  - **LoadGameScrollViewElement** (56 lines) - Individual save slot with selection feedback
  - **PausePanel** (83 lines) - Pause menu with save/quit, ESC key support
  - Full integration with LlamaBrainSaveManager for all save/load operations
  - Confirmation dialogs via Unity UI SetActive pattern
  - Scene transition support
  - **Total**: ~298 lines of production UI code
  - Files Added:
    - `Runtime/RedRoom/RedRoomGameController.cs`
    - `Runtime/RedRoom/UI/MainMenu.cs`
    - `Runtime/RedRoom/UI/LoadGameMenu.cs`
    - `Runtime/RedRoom/UI/LoadGameScrollViewElement.cs`
    - `Runtime/RedRoom/UI/PausePanel.cs`
- **Prefab Organization** ✅
  - Reorganized RedRoom prefabs into UI subfolder
  - Added new prefabs: LlamaBrainSaveManager, WhisperManager
  - New UI prefabs: Panel_MainMenu, Panel_LoadGame, Panel_Pause, Element_SaveGameEntry

#### Changed
- **Unity Package Updates**
  - Updated package.json with new voice system dependencies
  - Updated manifest.json and packages-lock.json with required packages
  - Updated assembly definition to include voice system
  - Removed unused assembly references from `Assembly-CSharp.LlamaBrain.Runtime.asmdef`
- **WorldIntentDispatcher Event Parameters** - Changed `WorldIntentEvent` parameter type from `Dictionary<string, string>` to `Dictionary<string, object>` to support complex parameter types (nested objects, arrays) matching `WorldIntent.Parameters`
- **NpcSpeechConfig Custom Model Fallback** - Added fallback to default model (`en_US-lessac-high`) when Custom preset is selected but `CustomModelPath` is null or empty, with editor validation warning
- **PausePanel Method Rename** - Renamed `ConfirmQuit()` to `ConfirmRestart()` for clarity and updated prefab reference
- **Third-Party Package Documentation** - Enhanced `THIRD_PARTY_PACKAGES.md` and `CONTRIBUTING.md` with:
  - Clarified that Starter Assets – Third Person requires URP version only for Unity 6 LTS compatibility
  - Added required dependencies documentation (URP project, New Input System, Cinemachine)
  - Added installation notes for UniTask namespace resolution issues
  - Updated package dependencies summary table with compatibility notes
- **RedRoom Scene Updates**
  - Enhanced RedRoom.unity scene with new UI systems
  - Integrated save/load functionality
  - Added voice input/output support
- **Configuration Assets**
  - Added PromptAssemblerSettings asset for scene-specific configuration
  - Added new expectancy and validation rule assets
  - Enhanced dialogue panel controller for voice integration
- **Project Settings**
  - Updated AudioManager, EditorBuildSettings, GraphicsSettings
  - Added MultiplayerManager asset
  - Updated ProjectVersion.txt
  - Updated URP settings (ShaderGraphSettings, URPProjectSettings)

#### Fixed
- UI prefab references and organization
- Save/load system integration with RedRoom demo
- Voice system integration with dialogue triggers
- **InteractionCount Increment Bug** - Fixed `InteractionCount` only incrementing when `storeConversationHistory` was enabled, breaking determinism when history storage was disabled. Now increments on any successful interaction regardless of history storage settings.
- **FileSystemSaveSystem File Size Check** - Fixed file size validation to use UTF-8 byte count instead of character count, ensuring accurate size limits for multi-byte characters
- **NpcVoiceOutput Audio Amplification** - Fixed division by zero error when processing audio with near-zero amplitude by adding epsilon check before amplification
- **NpcDialogueTrigger Cancellation Token** - Fixed `ObjectDisposedException` when accessing disposed cancellation token by adding proper exception handling
- **LoadGameMenu Empty Slot Display** - Fixed empty slot text not hiding when slots are populated and added null check for emptySlotText
- **PausePanel Save Method** - Added null check for `LlamaBrainSaveManager.Instance` to prevent errors when save manager is unavailable

### Core Library

#### Added
- **Structured Output Enhancements (Feature 12 Completion)** ✅
  - **Schema Versioning System**
    - Added `SchemaVersion.cs` with version parsing, comparison, and migration support
    - Version format: `major.minor.patch` with compatibility checking
    - Automatic version detection from JSON schemas
    - Migration support for backward compatibility
    - 49 tests in `SchemaVersionTests.cs` covering version parsing, comparison, compatibility, and migration
  - **Complex Intent Parameters**
    - Added `IntentParameters.cs` with typed parameter classes:
      - `GiveItemParameters` - Item name, quantity, target entity
      - `MoveToParameters` - Destination coordinates, speed
      - `InteractParameters` - Target entity, interaction type
    - Added `IntentParameterExtensions` for safe extraction from `Dictionary<string, object>`
    - Support for `GetArray<T>()` and `GetNested()` for complex parameter types
    - Enhanced `GetArray<T>()` to handle JArray, JToken, IList, IEnumerable, and ArrayList from JSON deserialization
    - Added `ConvertElement<T>()` helper for robust type conversion with JToken support
    - 18 tests passing in `ComplexIntentParametersTests.cs` (expanded with collection type tests)
  - **Relationship Authority Validation**
    - Added `RelationshipAuthorityValidator.cs` for owner-based and confidence threshold validation
    - Validates relationship entry authority before inclusion in context
    - Supports owner-based filtering and confidence-based filtering
    - 27 tests passing in `RelationshipAuthorityTests.cs`
  - **Files Added**:
    - `Source/Core/StructuredOutput/SchemaVersion.cs`
    - `Source/Core/StructuredOutput/IntentParameters.cs`
    - `Source/Core/StructuredOutput/RelationshipAuthorityValidator.cs`
  - **Files Modified**:
    - `Source/Core/StructuredOutput/JsonSchemaBuilder.cs` - Added version support
    - `Source/Core/StructuredOutput/StructuredSchemaValidator.cs` - Enhanced validation
    - `Source/Core/StructuredOutput/StructuredDialoguePipeline.cs` - Complex parameter support
- **Structured Input Enhancements (Feature 23 Completion)** ✅
  - **Relationship Entry Schema**
    - Added `RelationshipEntry.cs` with full schema:
      - `sourceEntity`, `targetEntity` - Entity identifiers
      - `affinity`, `trust`, `familiarity` - Relationship metrics
      - `history` - Relationship history array
      - `tags` - Relationship tags array
    - Integrated into `ContextSection.Relationships` property
    - 21 tests passing in `RelationshipEntryTests.cs`
  - **Partial Context Builder**
    - Added `PartialContextBuilder.cs` with fluent API for incremental context construction
    - Methods: `WithCanonicalFacts()`, `WithBeliefs()`, `WithRelationships()`, `WithDialogue()`
    - All `ContextSection` properties nullable with `NullValueHandling.Ignore`
    - Enables selective context inclusion for token efficiency
    - 18 tests passing in `PartialContextTests.cs`
  - **Validation Requirements**
    - Added `ValidationRequirements` to `ConstraintSection`:
      - `minResponseLength`, `maxResponseLength` - Response length constraints
      - `requiredKeywords` - Required keywords array
      - `forbiddenKeywords` - Forbidden keywords array
    - Integrated into constraint validation pipeline
    - 13 tests passing in `ValidationRequirementsTests.cs`
  - **Authority Boundaries**
    - Added `Authority` field to constraints with source tracking:
      - `system` - System-level constraints
      - `designer` - Designer-defined constraints
      - `npc` - NPC-specific constraints
      - `player` - Player-influenced constraints
    - Enables authority-based constraint filtering
  - **Dialogue Metadata**
    - Added `DialogueMetadata` to `StructuredDialogueEntry`:
      - `emotion` - Emotional state
      - `location` - Conversation location
      - `trigger` - Trigger identifier
      - `turnNumber` - Turn sequence number
    - Added `Timestamp` (float?) to dialogue entries
    - 14 tests passing in `DialogueMetadataTests.cs`
  - **Files Added**:
    - `Source/Core/StructuredInput/PartialContextBuilder.cs`
    - `Source/Core/StructuredInput/Schemas/RelationshipEntry.cs`
  - **Files Modified**:
    - `Source/Core/StructuredInput/Schemas/ConstraintSection.cs` - Added ValidationRequirements and Authority
    - `Source/Core/StructuredInput/Schemas/ContextSection.cs` - Added Relationships property
    - `Source/Core/StructuredInput/Schemas/DialogueSection.cs` - Added Timestamp and Metadata
    - `Source/Core/StructuredInput/Schemas/ContextJsonSchema.cs` - Updated schema structure
- **Seed-Based Determinism Documentation (Feature 14)** ✅
  - **Comprehensive Documentation**
    - Added comprehensive seed-based determinism section to `DETERMINISM_CONTRACT.md`:
      - Double-lock system explanation (Context Locking + Entropy Locking)
      - Seed parameter flow through the system
      - Seed behavior by value (null, 0, 1+, -1)
      - Retry behavior (same seed for all retry attempts)
      - Per-interaction vs per-attempt seed semantics
      - Hardware determinism limitations and guarantees
      - Cross-device reproducibility expectations
      - Backward compatibility and migration guide
    - Updated `ARCHITECTURE.md` with seed usage logging information:
      - `[LlamaBrainAgent] Using deterministic seed: 5 (InteractionCount)`
      - `[LlamaBrainAgent] No seed provided, using non-deterministic mode`
    - Updated `DEVELOPMENT_LOG.md` marking deferred items as complete:
      - Schema versioning (Feature 12.3)
      - Complex intent parameters (Feature 13.3)
      - Relationship entries (Feature 23.2)
      - Partial context support (Feature 23.5)
      - Validation requirements (Feature 23.2)
      - Authority boundaries (Feature 23.2)
      - Dialogue metadata (Feature 23.2)
  - **Integration Testing**
    - Added `BrainAgentSeedIntegrationTests.cs` for seed integration testing
    - Tests verify seed extraction from `InteractionContext` and propagation to API client
- **Feature 16: Save/Load Game Integration - COMPLETE** ✅
  - **Engine-Agnostic Persistence Abstraction** (`LlamaBrain/Source/Persistence/`)
    - `ISaveSystem` interface for engine-agnostic save/load operations
    - `SaveData` top-level container with versioning and timestamps
    - `PersonaMemorySnapshot` for complete memory state serialization
    - `ConversationHistorySnapshot` for dialogue history persistence
    - `SaveSlotInfo` for slot metadata (name, timestamp, version, persona count)
    - `SaveResult` for operation results with success/failure handling
  - **Memory Snapshot System**
    - `MemorySnapshotBuilder` creates snapshots from `AuthoritativeMemorySystem` using public APIs
    - `MemorySnapshotRestorer` restores memory state using internal `InsertXxxRaw` APIs for deterministic reconstruction
    - Preserves all determinism-critical fields: `Id`, `SequenceNumber`, `CreatedAtTicks`, ordinals
    - All enums serialized as `int` for stability
  - **Immutable DTO Layer** (`LlamaBrain/Source/Persistence/Dtos/`)
    - `CanonicalFactDto` - Canonical fact serialization
    - `WorldStateDto` - World state entry serialization
    - `EpisodicMemoryDto` - Episodic memory with significance/decay
    - `BeliefDto` - Belief memory with confidence/sentiment
    - `DialogueEntryDto` - Dialogue history entries
  - **FileSystem Implementation** (`FileSystemSaveSystem.cs`)
    - Uses existing `IFileSystem` abstraction for testability
    - Atomic writes (temp-file-then-move pattern)
    - Slot name sanitization for security
    - 5MB file size limit protection
    - JSON serialization via Newtonsoft.Json
  - **Test Coverage**: 33 new tests
    - `MemorySnapshotTests.cs`: Round-trip determinism, DTO conversion, restore correctness (18 tests)
    - `FileSystemSaveSystemTests.cs`: Save/load operations, sanitization, error handling (15 tests)
  - **AuthoritativeMemorySystem Enhancements**
    - Added `GetWorldStateEntries()` helper for snapshot building
    - Added `GetBeliefEntries()` helper for belief dictionary access
  - **Files Added**:
    - `Source/Persistence/ISaveSystem.cs`
    - `Source/Persistence/SaveData.cs`
    - `Source/Persistence/PersonaMemorySnapshot.cs`
    - `Source/Persistence/ConversationHistorySnapshot.cs`
    - `Source/Persistence/SaveSlotInfo.cs`
    - `Source/Persistence/SaveResult.cs`
    - `Source/Persistence/FileSystemSaveSystem.cs`
    - `Source/Persistence/MemorySnapshotBuilder.cs`
    - `Source/Persistence/MemorySnapshotRestorer.cs`
    - `Source/Persistence/Dtos/CanonicalFactDto.cs`
    - `Source/Persistence/Dtos/WorldStateDto.cs`
    - `Source/Persistence/Dtos/EpisodicMemoryDto.cs`
    - `Source/Persistence/Dtos/BeliefDto.cs`
    - `Source/Persistence/Dtos/DialogueEntryDto.cs`
  - **Documentation**: Updated STATUS.md, ROADMAP.md
- **Test Coverage Improvements** ✅
  - **Persistence Test Suite Expansion**
    - Added `ConversationHistorySnapshotTests.cs` (340 lines) - Comprehensive tests for conversation history serialization
    - Added `DialogueEntryDtoTests.cs` (263 lines) - DTO serialization and validation tests
    - Added `SaveDataTests.cs` (462 lines) - Save data container and versioning tests
  - **Integration Test Enhancements**
    - Expanded `StructuredPipelineIntegrationTests.cs` (+500 lines) - Additional structured pipeline integration scenarios
  - **Utility Test Expansion**
    - Enhanced `FileSystemTests.cs` (+125 lines) - Additional file system operation coverage
    - Enhanced `ProcessUtilsTests.cs` (+273 lines) - Comprehensive process utility test coverage
  - **Structured Output Test Expansion**
    - Expanded `JsonSchemaBuilderTests.cs` (+509 lines) - Additional JSON schema generation and validation tests
  - **Coverage Report Updates**
    - Updated `COVERAGE_REPORT.md` with latest coverage statistics (91.37% line coverage, 84.94% branch coverage)
    - Updated `coverage-analysis.csv` with latest metrics
  - **Total**: ~2,601 lines of new test code across 9 files

#### Fixed
- **Test Count Corrections** - Fixed incorrect test counts in changelog documentation (was showing 88 for all tests, now shows accurate counts: 18, 27, 21, 18, 13, 14)
- **Nullable Reference Warnings** - Fixed nullable reference warnings in test files by adding null-forgiving operators for nullable properties in ContextSerializer, DialogueMetadata, and StructuredContextProvider tests
- **IntentParameters GetArray Enhancement** - Enhanced `GetArray<T>()` method to handle JArray, JToken, IList, IEnumerable, and ArrayList from JSON deserialization, improving compatibility with various JSON deserializers

### Unity Runtime

#### Added
- **Feature 16: Unity Save/Load Integration** ✅
  - **SaveGameFree Implementation** (`LlamaBrainRuntime/.../Runtime/Persistence/`)
    - `SaveGameFreeSaveSystem` wraps SaveGameFree plugin, implements `ISaveSystem`
    - Stores saves at `LlamaBrain/Saves/{slotName}` using SaveGameFree's native serialization
    - Maintains metadata file for slot listing and ordering
  - **LlamaBrainSaveManager MonoBehaviour**
    - Central save/load manager for games
    - Auto-registers all `LlamaBrainAgent` instances in scene (configurable)
    - Named save slots support (e.g., "autosave", "slot1")
    - `SaveToSlot()` / `LoadFromSlot()` methods
    - `QuickSave()` / `QuickLoad()` convenience methods
    - Events: `OnSaveComplete`, `OnLoadComplete`
  - **LlamaBrainAgent Integration**
    - Added `CreateSaveData()` method for agent state serialization
    - Added `RestoreFromSaveData()` method for agent state restoration
    - Preserves memory state, conversation history, and deterministic metadata
  - **Files Added**:
    - `Runtime/Persistence/SaveGameFreeSaveSystem.cs`
    - `Runtime/Persistence/LlamaBrainSaveManager.cs`

## [0.3.0-rc.1] 2026-01-03

### Unity Runtime

#### Added
- **Feature 14: Cross-Session Determinism Proof Tests - COMPLETE** ✅
  - **PlayMode Tests** (`ExternalIntegrationPlayModeTests.cs`)
    - `PlayMode_CrossSession_SameSeedSamePrompt_ProducesIdenticalOutput` - Core proof: same seed + prompt = identical output
    - `PlayMode_CrossSession_DifferentSeeds_ProduceDifferentOutputs` - Sanity check: different seeds differ
    - `PlayMode_CrossSession_InteractionCountAsSeed_ProducesDeterministicSequence` - Game replay proof
    - `PlayMode_CrossSession_TemperatureZero_ProducesDeterministicOutput` - Greedy decoding proof
    - `PlayMode_CrossSession_StructuredOutput_ProducesIdenticalJson` - JSON output determinism
  - **Server Wait Improvements**
    - Wait for `/health` endpoint to return 200 OK (not just any HTTP response)
    - Properly handles 503 "Loading model" status during model initialization
    - Added error validation to prevent false positives from error responses
  - **Documentation**: Updated ARCHITECTURE.md with Feature 14 completion status and proof test references

### Core Library

#### Fixed
- **Governance Plane Determinism: Dictionary Enumeration Order Leak**
  - `ContextRetrievalLayer.RetrieveCanonicalFacts()` now sorts by `Id` (ordinal) for deterministic ordering
  - `ContextRetrievalLayer.RetrieveWorldState()` now sorts by `Key` (ordinal) for deterministic ordering
  - Fixes byte-stable prompt assembly when dictionary insertion order varies
  - **Files Changed**: `Source/Core/Inference/ContextRetrievalLayer.cs`
  - **Tests Fixed**: `GovernancePlaneDeterminismTests.cs` - rewrote to properly verify serialization stability

#### Changed
- **Clarified Determinism Claims: LLM Output is NOT Mathematically Deterministic**
  - Renamed `CrossSessionDeterminismPlayModeTests.cs` → `ExternalIntegrationPlayModeTests.cs`
  - Renamed test class `CrossSessionDeterminismPlayModeTests` → `ExternalIntegrationPlayModeTests`
  - Rewrote test descriptions: "proof" → "reproducibility smoke test"
  - Updated ARCHITECTURE.md to clarify:
    - **What IS proven**: Pipeline determinism (prompt assembly, validation, mutation) - `GovernancePlaneDeterminismTests.cs`
    - **What is NOT proven**: LLM generation (seeded sampling is best-effort, not guaranteed)
  - Removed "pure function" claims for LLM output
  - Added explicit limitations: GPU/CPU paths, thread scheduling, llama.cpp version, CUDA version, etc.
  - **Rationale**: Seeded sampling provides best-effort reproducibility under controlled conditions, but is not mathematically deterministic due to hardware/runtime variations
  - Added "Determinism Boundary: Known Limitation" section to ARCHITECTURE.md near the top, with table showing what is/isn't deterministic and why LLM determinism is not the goal

#### Added
- **Feature 14.1: Seed Parameter Support - COMPLETE** ✅
  - **API Layer Seed Support**
    - Added `seed` field to `CompletionRequest` in `ApiContracts.cs`
    - Updated `IApiClient` interface with `seed` parameter on all 4 methods:
      - `SendPromptAsync`
      - `SendPromptWithMetricsAsync`
      - `SendStructuredPromptAsync`
      - `SendStructuredPromptWithMetricsAsync`
    - Updated `ApiClient` implementation to include seed in request JSON
    - Seed semantics: `null` = omit (server default), `-1` = random, `0+` = deterministic
  - **Test Coverage**
    - Added `ApiClientSeedTests.cs` with 10 unit tests covering:
      - Seed inclusion in all API methods
      - Null seed omission from requests
      - Negative seed (-1) pass-through
      - Zero seed handling
      - Seed parameter ordering with other parameters
  - **Files Changed**:
    - `Source/Core/ApiContracts.cs` - Added `seed` field
    - `Source/Core/IApiClient.cs` - Added `seed` parameter to all methods
    - `Source/Core/ApiClient.cs` - Implemented seed parameter support
  - **Files Added**:
    - `LlamaBrain.Tests/Core/ApiClientSeedTests.cs` - Seed parameter unit tests
  - **Documentation**: Updated `ROADMAP.md` with 14.1 completion status

- **Feature 23: Structured Input/Context - COMPLETE** ✅
  - **Structured Context Provider Infrastructure**
    - Added `IStructuredContextProvider` interface for structured context generation
    - Added `LlamaCppStructuredContextProvider` singleton implementation
    - Added `StructuredContextFormat` enum with None, JsonContext, and FunctionCalling options
    - Added `StructuredContextConfig` for configuring context injection behavior
    - Static presets: Default, TextOnly, Strict
  - **JSON Schema DTOs for Context**
    - Added `ContextJsonSchema` root DTO with versioning support
    - Added `ContextSection` for memories (canonical facts, world state, episodic, beliefs)
    - Added `ConstraintSection` for prohibitions, requirements, permissions
    - Added `DialogueSection` with history and player input
    - Added helper DTOs: `WorldStateEntry`, `EpisodicMemoryEntry`, `BeliefEntry`, `StructuredDialogueEntry`
  - **Context Serialization**
    - Added `ContextSerializer` with deterministic JSON serialization
    - Supports compact JSON mode for token efficiency
    - XML-style delimiters (`<context_json>`) for prompt injection
    - Schema version tracking for forward compatibility
  - **PromptAssembler Integration**
    - Added `StructuredContextConfig` property to `PromptAssemblerConfig`
    - Added `UseStructuredContext` computed property
    - Added `AssembleStructuredPrompt()` method for structured context assembly
    - Automatic fallback to text assembly on failure (when configured)
    - Hybrid mode: structured JSON context + text system prompt
  - **Test Coverage**: ~246 new tests across 8 test files
    - `ContextSerializerTests.cs`: Serialization, determinism, and round-trip tests (23 tests)
    - `StructuredContextProviderTests.cs`: Provider, config, and context building tests (24 tests)
    - `PromptAssemblerStructuredTests.cs`: Integration and fallback behavior tests (35 tests)
    - `FunctionCallDispatcherTests.cs`: Dispatch, registration, case-insensitivity, error handling (51 tests)
    - `FunctionCallExecutorTests.cs`: Execution, factory methods, integration tests (30 tests)
    - `BuiltInContextFunctionsTests.cs`: All 6 built-in functions, argument parsing (37 tests)
    - `FunctionCallTests.cs`: Model tests, argument extraction (30 tests)
    - `FunctionCallResultTests.cs`: Result factory methods, serialization (16 tests)
  - **Files Added**:
    - `Source/Core/StructuredInput/StructuredContextFormat.cs`
    - `Source/Core/StructuredInput/StructuredContextConfig.cs`
    - `Source/Core/StructuredInput/IStructuredContextProvider.cs`
    - `Source/Core/StructuredInput/LlamaCppStructuredContextProvider.cs`
    - `Source/Core/StructuredInput/ContextSerializer.cs`
    - `Source/Core/StructuredInput/Schemas/ContextJsonSchema.cs`
    - `Source/Core/StructuredInput/Schemas/ContextSection.cs`
    - `Source/Core/StructuredInput/Schemas/ConstraintSection.cs`
    - `Source/Core/StructuredInput/Schemas/DialogueSection.cs`
  - **Function Calling Dispatch System** ✅
    - Added `FunctionCallDispatcher` with command table pattern for function call routing
    - Added `FunctionCall` and `FunctionCallResult` DTOs for function call requests/results
    - Added `FunctionCallExecutor` for pipeline integration
    - Extended `ParsedOutput` to include `FunctionCalls` property
    - Extended JSON schema to include `functionCalls` array
    - Added `BuiltInContextFunctions` with 6 built-in context access functions:
      - `get_memories(limit, minSignificance)` - Get episodic memories
      - `get_beliefs(limit, minConfidence)` - Get NPC beliefs
      - `get_constraints()` - Get current constraints
      - `get_dialogue_history(limit)` - Get recent dialogue
      - `get_world_state(keys)` - Get world state entries
      - `get_canonical_facts()` - Get canonical facts
    - Supports custom game function registration (e.g., PlayNpcFaceAnimation, StartWalking)
    - Self-contained interpretation from LLM JSON output (no native LLM function calling required)
    - Works with any LLM that outputs structured JSON
    - Files Added:
      - `Source/Core/FunctionCalling/FunctionCall.cs`
      - `Source/Core/FunctionCalling/FunctionCallResult.cs`
      - `Source/Core/FunctionCalling/FunctionCallDispatcher.cs`
      - `Source/Core/FunctionCalling/FunctionCallExecutor.cs`
      - `Source/Core/FunctionCalling/BuiltInContextFunctions.cs`
    - Tests Added:
      - `LlamaBrain.Tests/FunctionCalling/FunctionCallDispatcherTests.cs`
      - `LlamaBrain.Tests/FunctionCalling/FunctionCallExecutorTests.cs`
      - `LlamaBrain.Tests/FunctionCalling/BuiltInContextFunctionsTests.cs`
      - `LlamaBrain.Tests/FunctionCalling/FunctionCallTests.cs`
      - `LlamaBrain.Tests/FunctionCalling/FunctionCallResultTests.cs`
  - **Deferred Items**: Native LLM function calling APIs (OpenAI, Anthropic) - not needed, we parse JSON ourselves

### Unity Runtime

#### Added
- **Unity Function Call Integration** ✅
  - **FunctionCallController** (MonoBehaviour singleton)
    - Manages core `FunctionCallDispatcher` instance
    - Registers functions from ScriptableObject configs
    - Provides UnityEvents for function call results
    - Programmatic registration/unregistration support
    - Scene-local singleton pattern (like `WorldIntentDispatcher`)
  - **FunctionCallConfigAsset** (ScriptableObject)
    - Designer-friendly function configuration
    - Function name, description, parameter schema
    - Enable/disable flags, priority/ordering
    - Similar structure to `ExpectancyRuleAsset`
  - **NpcFunctionCallConfig** (MonoBehaviour component)
    - Per-NPC function configuration
    - References NPC-specific `FunctionCallConfigAsset` list
    - Overrides/extends global functions
    - Attached to NPC GameObjects (like `NpcExpectancyConfig`)
  - **FunctionCallEvents** (Unity Event Types)
    - `FunctionCallEvent` - UnityEvent for individual function calls
    - `FunctionCallResultEvent` - UnityEvent for results by function name
    - `FunctionCallResultsEvent` - UnityEvent for batch results
  - **LlamaBrainAgent Integration**
    - Added `NpcFunctionCallConfig?` field (like `ExpectancyConfig`)
    - Auto-detects `NpcFunctionCallConfig` component during initialization
    - Registers NPC-specific functions with `FunctionCallController`
    - Executes function calls after parsing output in `SendWithSnapshotAsync()`
    - Stores results in `LastFunctionCallResults` property
    - Added `HookFunctionCallController()` method (like `HookIntentDispatcher()`)
  - **Files Added**:
    - `LlamaBrainRuntime/.../FunctionCalling/FunctionCallEvents.cs`
    - `LlamaBrainRuntime/.../FunctionCalling/FunctionCallConfigAsset.cs`
    - `LlamaBrainRuntime/.../FunctionCalling/FunctionCallController.cs`
    - `LlamaBrainRuntime/.../FunctionCalling/NpcFunctionCallConfig.cs`
  - **Features**:
    - Inspector-based function handlers via UnityEvents
    - Code-based function handlers via C# Action delegates
    - Global functions from `FunctionCallController` + NPC-specific functions from `NpcFunctionCallConfig`
    - NPC-specific functions take precedence over global functions
    - Results automatically sent to Unity via UnityEvents
    - Results stored in `LlamaBrainAgent.LastFunctionCallResults` for debugging/metrics

### Project Infrastructure

#### Added
- **Simple Project Governance** ✅
  - **Public Surface Definition**
    - Condensed `SCOPE.md` to 1-page format covering what LlamaBrain is (and is not), supported Unity versions, supported providers, and deterministic guarantees
  - **Governance Structure**
    - `GOVERNANCE.md` with BDFL model and delegation framework
    - `CODEOWNERS` file covering critical directories (Validation, Inference, AuthoritativeMemorySystem, Unity Core)
    - Maintainer criteria and permissions clearly defined
  - **Contribution Guidelines**
    - Enhanced `CONTRIBUTING.md` with complete sections for:
      - Branching strategy (feature/fix/docs/test branches)
      - Test requirements (92.37% coverage, 1,531 tests)
      - Code formatting (dotnet format)
      - Commit conventions (Conventional Commits format)
      - Security reporting path (GitHub Security Advisory)
      - "No secrets in issues" policy
  - **Release Process**
    - `RELEASE_CHECKLIST.md` with SemVer guidelines
    - `CHANGELOG.md` following Keep a Changelog format
    - CI/CD package validation step (validates Unity package.json consistency)
  - **Documentation & Proof**
    - Updated `README.md` to prominently feature RedRoom demo as proof of deterministic architecture
    - Added dedicated "🧪 Proof: RedRoom Demo & Deterministic Gate/Fallback" section
    - Highlights RedRoom as complete, runnable demonstration of all 9 architectural components
- **Formal Attribution & Trademark Policy** ✅
  - **Citation Support**
    - Added `CITATION.cff` for GitHub citation tooling and academic attribution
    - Enables "Cite this repository" feature on GitHub
  - **Developer Certificate of Origin (DCO)**
    - Added `DCO.md` explaining DCO requirements and sign-off process
    - All commits must include "Signed-off-by" line
    - CI workflow enforces DCO check on all pull requests
    - Updated `CONTRIBUTING.md` with DCO requirements and attribution guidelines
  - **Trademark Usage Policy**
    - Added `TRADEMARKS.md` with comprehensive trademark usage policy
    - Defines allowed uses, prohibited uses, and fork branding requirements
    - Clarifies separation between MIT License (code) and trademark rights
    - Updated `README.md` with trademark clarification note
  - **Attribution & Credit**
    - Added `CONTRIBUTORS` file for tracking non-trivial contributions
    - Added `THIRD_PARTY_NOTICES.md` for third-party code attribution
    - Updated `CONTRIBUTING.md` with attribution section covering:
      - DCO sign-off requirements
      - SPDX license header requirements for new files
      - Copyright and attribution guidelines
      - Third-party attribution requirements
  - **SPDX License Headers**
    - Added SPDX license identifiers to key public API files (`ApiClient.cs`, `BrainAgent.cs`, `IApiClient.cs`)
    - New files must include `SPDX-License-Identifier: MIT` headers
    - Makes license scanning and compliance easier

### Documentation

#### Changed
- **README Files**: Comprehensive rewrite of root README.md and LlamaBrain/README.md for improved clarity
  - Simplified architecture explanation focusing on the core problem/solution
  - Streamlined feature descriptions
  - Updated test counts (2,068 tests, up from 1,758)
- **Doxygen Configuration**: Extended input file coverage
  - Added SECURITY.md, DCO.md, THIRD_PARTY_NOTICES.md, TRADEMARKS.md to core Doxygen config
  - Added RedRoom README.md for Unity documentation generation
- **XML Documentation**: Improved documentation coverage in `StructuredSchemaValidator.cs`
  - Added missing XML doc comments for methods and parameters

## [0.2.0] - 2026-01-02

### Core Library

#### Added
- **Feature 12: Dedicated Structured Output - COMPLETE** ✅
- **Feature 13: Structured Output Integration - COMPLETE** ✅
  - **Native llama.cpp Structured Output Support**
    - Added `StructuredOutputFormat` enum with JsonSchema, Grammar, ResponseFormat, and None options
    - Added `StructuredOutputConfig` for configuring structured output behavior
    - Added `IStructuredOutputProvider` interface and `LlamaCppStructuredOutputProvider` implementation
    - Extended `CompletionRequest` with `json_schema`, `grammar`, and `response_format` parameters
    - Extended `IApiClient` with `SendStructuredPromptAsync` and `SendStructuredPromptWithMetricsAsync` methods
    - Implemented `ApiClient.SendStructuredPromptWithMetricsAsync` for native llama.cpp structured output
  - **JSON Schema Generation**
    - Added `JsonSchemaBuilder` for generating JSON schemas from C# types
    - Pre-built schemas: `ParsedOutputSchema`, `DialogueOnlySchema`, `AnalysisSchema`
    - Schema validation with `ValidateSchema()` method
    - Dynamic schema generation from C# types via `BuildFromType<T>()`
  - **Structured Output Parsing**
    - Added `OutputParser.ParseStructured()` for direct JSON parsing of structured responses
    - Added `OutputParser.ParseAuto()` for automatic detection of structured vs free-form responses
    - Added `OutputParserConfig.NativeStructured` preset for native structured output mode
    - Added `StructuredDialogueResponse`, `StructuredMutation`, `StructuredIntent` DTOs for JSON deserialization
  - **BrainAgent Integration**
    - Added `SendNativeStructuredMessageAsync()` for native structured output with custom schema
    - Added `SendNativeDialogueAsync()` for dialogue with built-in ParsedOutput schema
    - Added `SendNativeStructuredInstructionAsync()` for instructions with structured output
    - Generic versions with `<T>` for automatic deserialization to custom types
  - **Test Coverage**: 56 new tests across 3 test files
    - `JsonSchemaBuilderTests.cs`: Schema generation and validation tests
    - `StructuredOutputProviderTests.cs`: Provider and configuration tests
    - `OutputParserStructuredTests.cs`: Structured parsing tests
  - **Documentation**: Updated `ARCHITECTURE.md` with Feature 12 section

- **Feature 13: Structured Output Integration - COMPLETE** ✅
  - **StructuredDialoguePipeline**: Complete orchestration layer for structured output processing
    - Unified pipeline orchestrating LLM request → parsing → validation → mutation execution
    - Automatic retry logic with constraint escalation via `StateSnapshot.ForRetry()`
    - Automatic fallback to regex parsing on structured output failure
    - Comprehensive metrics tracking for success rates and performance
    - Full integration with `ValidationGate` and `MemoryMutationController`
    - Three configuration modes: Default (structured + fallback), StructuredOnly, RegexOnly
  - **StructuredSchemaValidator**: Pre-execution schema validation
    - Validates mutation schemas (type, content, target, confidence requirements)
    - Validates intent schemas (intentType, priority, parameters)
    - Filters invalid mutations/intents before execution
    - Supports both `StructuredMutation`/`ProposedMutation` and `StructuredIntent`/`WorldIntent` types
    - Optional logging callbacks for invalid items
  - **StructuredPipelineConfig**: Configurable pipeline modes
    - `Default`: Structured output with automatic regex fallback (recommended for production)
    - `StructuredOnly`: Native structured output only, no fallback
    - `RegexOnly`: Legacy regex parsing only (for backward compatibility)
    - Configurable schema validation flags (`ValidateMutationSchemas`, `ValidateIntentSchemas`)
    - Configurable retry limits (`MaxRetries`)
  - **StructuredPipelineMetrics**: Performance and success tracking
    - Tracks structured output success/failure rates
    - Tracks fallback usage rates
    - Tracks validation failure counts
    - Tracks mutation and intent execution counts
    - Tracks retry attempt counts
    - Calculated success rates (structured, overall, fallback)
    - Reset capability for session-based tracking
  - **StructuredPipelineResult**: Unified result reporting
    - Success/failure status with detailed error messages
    - Parse mode tracking (Structured, Regex, Fallback)
    - Complete validation and mutation results
    - Retry count tracking
    - Convenience properties for common queries (MutationsExecuted, IntentsEmitted, ValidationPassed)
  - **Validation Pipeline Integration**
    - Full integration with `ValidationGate` for constraint and canonical fact validation
    - Pre-validation schema checking before reaching validation gate
    - Retry logic with constraint escalation on validation failures
    - Handles structured output validation failures gracefully
  - **Mutation Extraction Enhancement**
    - Pipeline integration with `MemoryMutationController.ExecuteMutations`
    - Mutations parsed via `OutputParser.ParseStructured()` before reaching controller
    - Support for all mutation types in structured format (AppendEpisodic, TransformBelief, TransformRelationship, EmitWorldIntent)
    - Schema validation integrated in pipeline via `ValidateMutationSchemas` config
  - **World Intent Integration**
    - `WorldIntentDispatcher` handles structured intents via event-based hookup
    - Parse structured intent parameters correctly (flat Dictionary<string, string>)
    - Validate intent schemas before dispatch via `ValidateIntentSchemas` config
  - **Error Handling & Fallback**
    - Comprehensive error handling for malformed structured outputs
    - Automatic fallback to regex parsing on structured output failure
    - Logging and metrics for structured output success/failure rates
    - User-friendly error messages via `StructuredPipelineResult.ErrorMessage`
  - **Migration & Compatibility**
    - Configuration to enable/disable structured output per pipeline (`StructuredPipelineConfig`)
    - Support for regex-only mode (`StructuredPipelineConfig.RegexOnly`)
    - A/B testing support via configurable modes (Structured, Regex, Fallback)
    - 100% backward compatibility maintained (all existing tests pass)
  - **Performance**: Sub-millisecond parsing for all paths (~0.01ms structured, ~0.00ms regex)
  - **Test Coverage**: Comprehensive integration tests covering all pipeline modes, fallback scenarios, retry logic, and schema validation
  - **Documentation**: Updated `ARCHITECTURE.md` with complete Feature 13 section including pipeline flow, configuration, and usage examples

#### Changed
- **`StructuredPipelineMetrics` thread-safety**
  - Refactored all metric properties to use `Interlocked` operations for thread-safe concurrent access
  - Changed property setters to private backing fields with `Interlocked.Increment`, `Interlocked.Add`, and `Interlocked.Exchange`
  - Properties now use expression-bodied getters returning backing field values
  - Ensures safe concurrent metric tracking in multi-threaded scenarios

### Project Infrastructure

#### Added
- **GitHub Issue Templates**
  - Added bug report template (`.github/ISSUE_TEMPLATE/bug_report.md`) with comprehensive bug reporting fields
  - Added feature request template (`.github/ISSUE_TEMPLATE/feature_request.md`) with use case and implementation guidance
  - Added issue template configuration (`.github/ISSUE_TEMPLATE/config.yml`) with security advisory and community links
- **Pull Request Template** (`.github/PULL_REQUEST_TEMPLATE.md`)
  - Comprehensive PR template with architecture compliance checklist
  - Security considerations section
  - Code quality and testing checklists
  - Documentation update requirements
- **Dependabot Configuration** (`.github/dependabot.yml`)
  - Automated dependency updates for NuGet packages (monthly schedule)
  - Automated dependency updates for GitHub Actions (monthly schedule)
  - Grouped updates for patch and minor versions
  - Configurable PR limits and reviewers
- **Security Policy** (`.github/SECURITY.md` and `SECURITY.md`)
  - GitHub security policy with vulnerability reporting guidelines
  - Root-level security policy with detailed reporting procedures
  - Discord community contact information for security issues
- **Code of Conduct** (`CODE_OF_CONDUCT.md`)
  - Contributor Covenant Code of Conduct (version 2.1)
  - Community standards and enforcement guidelines
  - Reporting procedures and enforcement escalation

#### Changed
- **`.gitignore`**
  - Added `claude.md` to ignore patterns
- **`CONTRIBUTING.md`**
  - Removed completed test coverage item from roadmap section

### Documentation

#### Changed
- **`ROADMAP.md`**: Updated with latest feature status and progress
- **`STATUS.md`**: Updated with current implementation status
- **`USAGE_GUIDE.md`**: Updated usage documentation
- **Doxygen Configuration**: Updated doxygen configs for documentation generation

#### Fixed
- **`.github/SECURITY.md`**: Updated GitHub username placeholder to actual repository owner
- **`USAGE_GUIDE.md`**: Fixed heading formatting issue (removed empty braces)
- **`COVERAGE_REPORT.md`**: Updated to reflect current ApiClient coverage status (66.74%)

## [0.2.0-rc.2] - 2026-01-01

### Core Library

#### Added
- **Phase 10: Deterministic Proof Gap Testing - COMPLETE** ✅
  - **WorldIntentDispatcher Singleton Lifecycle (Requirement #5)** - Complete implementation and testing
    - Implemented scene-local singleton pattern with duplicate detection and destruction
    - Added `IsDuplicateInstance` property for test verification
    - Comprehensive PlayMode test suite: 30 tests covering singleton lifecycle, intent dispatch, handler execution, history tracking, statistics, integration, and edge cases
    - All 5 critical requirements now complete (Requirements #1-5)
  - **Pipeline Proof Gaps - COMPLETE** ✅
    - **Double-hook safety**: Made `HookToController()` idempotent to prevent double-subscription
      - Added `HashSet<MemoryMutationController>` to track hooked controllers
      - `HookToController()` now checks if already hooked before subscribing
      - Added test `Integration_HookToController_DoubleHook_DoesNotDoubleSubscribe` to verify idempotency
    - **Policy boundary proof**: Added integration tests proving validation failure produces zero dispatches
      - Added test-only `WorldIntentEmitsForTests` counter to `MemoryMutationController` for emission tracking
      - Added positive control test `Integration_PolicyBoundary_PassingValidation_ProducesDispatches` verifying passing validation produces 1 emission and 1 dispatch
      - Added negative control test `Integration_PolicyBoundary_FailingValidation_ProducesZeroDispatches` proving failing validation produces 0 emissions and 0 dispatches
      - Tests verify both controller emission count and dispatcher count to prove real coupling
  - **Total Phase 10 Test Coverage**: 353 tests (exceeds original estimate of 150-180)
    - ContextRetrievalLayer: 55 tests
    - PromptAssembler/EphemeralWorkingMemory: 80 tests
    - OutputParser: 86 tests
    - ValidationGate: 44 tests (17 basic + 27 detailed)
    - MemoryMutationController: 41 tests
    - WorldIntentDispatcher: 30 tests (28 original + 2 pipeline proof gap tests)
    - Full Pipeline Integration: 25 tests (17 DeterministicPipelineTests + 8 FullPipelineIntegrationTests)
  - **Determinism Proof Status**: Now defensible at byte level for serialized state and prompt text assembly
  - All 7 minimal proof suite tests complete
  - All critical implementation requirements met
  - See `VERIFICATION_REPORT.md` for detailed completion status
- **Snapshot-time driven context retrieval** (`StateSnapshot`, `ContextRetrievalLayer`)
  - Added `SnapshotTimeUtcTicks` property to `StateSnapshot` for deterministic time-based operations
  - Added `WithSnapshotTimeUtcTicks()` method to `StateSnapshotBuilder` for explicit snapshot time control
  - Added `RetrieveContext(StateSnapshot snapshot)` overload to `ContextRetrievalLayer` for snapshot-based retrieval
  - Added `RetrieveContext(string playerInput, long snapshotTimeUtcTicks)` overload for explicit time-based retrieval
  - Updated recency scoring to use exponential decay formula: `recency = 0.5 ^ (elapsedTime / halfLife)` based on snapshot time
  - Ensures deterministic behavior by eliminating `UtcNow`/`Time.time` calls during scoring, decay, and pruning operations
- **Comprehensive regression tests for memory serialization** (`DeterministicPipelineTests`)
  - Added extensive test suite for `Esc()`, `Unesc()`, and `SplitEscaped()` methods (1,000+ lines)
  - Tests cover all edge cases: null handling, empty strings, pipes, backslashes, newlines, carriage returns, mixed special characters
  - Validates byte-stable round-trips and correct handling of escape sequences
  - Tests verify that `SplitEscaped` preserves escape sequences for `Unesc()` to decode correctly
- **Byte-level prompt text determinism tests (Test D)** (`DeterministicPipelineTests`)
  - Added 6 comprehensive tests for WorkingMemory hard-bounds behavior byte-level verification
  - `PromptAssembly_ByteLevelDeterminism_SameInputs_IdenticalOutput` - Verifies same inputs produce identical byte-level prompt text across multiple runs
  - `PromptAssembly_NewlineCounts_Deterministic` - Verifies exact newline counts match deterministically
  - `PromptAssembly_EmptySections_DeterministicOmission` - Verifies empty section handling produces identical byte-level output
  - `PromptAssembly_SeparatorPlacement_Deterministic` - Verifies exact separator positions match deterministically
  - `PromptAssembly_MandatorySections_BypassCharacterCaps` - Verifies mandatory sections bypass character limits deterministically
  - `PromptAssembly_TruncationPriority_Deterministic` - Verifies truncation priority order (Dialogue → Episodic → Beliefs) produces deterministic output
  - Completes Test D requirement for byte-level prompt text verification

#### Fixed
- **DeterministicPipelineTests compilation errors**
  - Changed `MemoryStateSerializer.Unesc()` method access modifier from `private` to `internal` to allow test methods to access it
  - Fixed 16 compilation errors (CS0122) related to inaccessible method
- **SplitEscaped test correction**
  - Fixed `SplitEscaped_HandlesTrailingBackslash` test by correcting input string from `"abc\\|def"` to `"abc\\\\|def"` to properly represent a literal backslash before delimiter
  - Updated expected value to match the actual behavior of `SplitEscaped` method which preserves escape sequences
- **Context retrieval determinism**
  - Fixed context retrieval to use snapshot time instead of current system time for recency calculations
  - Updated test `NoNow_ContextRetrievalUsesSnapshotTime_NotCurrentTime` to verify identical results with same snapshot time despite wall-clock changes
  - Ensures deterministic ranking of episodic memories based on snapshot time rather than access time
- **Nullable reference warnings in DeterministicPipelineTests**
  - Fixed CS8604 warnings by adding null-coalescing operators (`?? string.Empty`) for nullable `PlayerInput` property
  - Updated 11 occurrences across `DeterministicPipelineTests.cs` where `context.PlayerInput` (nullable `string?`) was passed to methods expecting non-nullable `string`
  - Ensures type safety while maintaining test behavior (PlayerInput is always set in test contexts)

#### Changed
- **MemoryEntry documentation**
  - Added documentation clarifying that `LastAccessedAt` is not included in deterministic serialization as it changes on access
  - Added note that `MarkAccessed()` uses system time and is not part of deterministic state
- **Verification Report status updates**
  - Updated `VERIFICATION_REPORT.md` to reflect current test coverage status
  - Marked Issue #3 (PipelineOrder test) as FIXED - test now uses real orchestrator with component execution order verification
  - Updated Test E (OutputParser Normalization) status from PARTIAL to DONE - all normalization points verified (21+ tests)
  - Updated Test D (WorkingMemory Hard-Bounds) status from PARTIAL to DONE - byte-level prompt text comparison tests added (6 new tests)
  - Overall status: 7/7 minimal proof suite tests complete, 0/7 partial - **ALL TESTS COMPLETE**

#### Known Issues / Caveats
- **CanonicalFact.Source serialization mismatch**
  - `CanonicalFact.Source` property is preserved in `ReconstructFromSource()` but is not serialized/restored in `SerializeState()` and `ReconstructFromSerialized()`
  - If downstream code depends on `CanonicalFact.Source`, this creates a silent reconstruction mismatch between source-based and serialized-based reconstruction
  - **Impact**: Low - Source is primarily used for authority enforcement during creation, not typically accessed after creation
  - **Workaround**: Use `ReconstructFromSource()` if Source preservation is required
- **Redundant escape handling in Unesc()**
  - The special case `if (s == "\\\\0") return "\\0"` in `Unesc()` is redundant under current `Esc()` logic
  - This does not break functionality but represents dead code that could be removed in a future cleanup
- **Reflection-based determinism proof fragility**
  - The determinism proof tests depend on reflection to access private fields (`_episodicMemories`, `_beliefs`) for byte-level state comparison
  - Field name changes will break the determinism proof tests
  - **Impact**: Medium - Refactoring field names requires updating test reflection code
  - **Mitigation**: This is an acceptable trade-off for integration-proof test suites, but developers should be aware that field renames require test updates
- **Determinism proof status**
  - The determinism proof is now defensible at the byte level for all serialized state, including:
    - Previously missing belief-key dimension (now properly serialized/restored)
    - Float fidelity (using hex bit pattern representation)
    - Complete state reconstruction matching
    - Byte-level prompt text determinism (Test D complete - 6 new tests verify prompt assembly produces identical byte-level output)
  - **Phase 10 is now COMPLETE** - All 7 minimal proof suite tests verified, all 5 critical requirements implemented, 351 tests total


## [0.2.0-rc.1] - 2026-01-01

### Core Library

#### Added

#### Integration Testing Infrastructure
- **Full Pipeline Integration Tests** (`LlamaBrain.Tests/Integration/FullPipelineIntegrationTests.cs`)
  - Comprehensive integration tests for the complete 9-component architectural pipeline
  - Tests all components working together from InteractionContext through validated output and memory mutation
  - Validates end-to-end flow including expectancy evaluation, context retrieval, prompt assembly, validation, and mutation execution
  - Tests retry logic, constraint escalation, and fallback system integration
  - Validates memory authority enforcement and canonical fact protection
- **Comprehensive ServerManager Test Suite** (`LlamaBrain.Tests/Core/ServerManagerTests.cs`)
  - 2,123+ lines of comprehensive tests for server lifecycle management
  - Tests process startup, shutdown, monitoring, and error handling
  - Validates configuration validation, path resolution, and argument building
  - Tests event handling (OnServerOutput, OnServerError)
  - Validates timeout handling and graceful shutdown procedures
  - Coverage improved from 32.81% to 74.55% (92.31% branch coverage)
- **ProcessUtils Test Suite** (`LlamaBrain.Tests/Utilities/ProcessUtilsTests.cs`)
  - 617+ lines of comprehensive tests for process utility functions
  - Tests process detection, validation, and management utilities
  - Validates error handling and edge cases
- **Enhanced ApiClient Test Suite** (`LlamaBrain.Tests/ApiClientTests.cs`)
  - Major expansion with 1,605+ additional lines of tests
  - Comprehensive coverage of HTTP communication, rate limiting, error handling
  - Tests timeout management, request validation, and response parsing
  - Coverage improved from 36.36% to 90.54%

#### Determinism Layer & Expectancy Engine
- **Expectancy Engine Core** (`ExpectancyEvaluator.cs`)
  - Engine-agnostic rule evaluation system
  - Support for code-based and asset-based rules
  - Priority-based rule evaluation
  - Logging callback system for debug output
- **Constraint System** (`Constraint.cs`, `ConstraintSet.cs`)
  - Permission, Prohibition, and Requirement constraint types
  - Soft, Hard, and Critical severity levels
  - Prompt injection support via `ToPromptInjection()` method
  - Constraint combination and management
- **Interaction Context** (`InteractionContext.cs`)
  - Context-aware rule evaluation
  - Support for trigger reasons, NPC IDs, scene names, and tags
  - Interaction count tracking
  - Condition combination modes (All/Any)

#### Structured Memory System
- **Memory Type Hierarchy** (`MemoryTypes/`)
  - `MemoryEntry` base class with mutation results
  - `MemoryAuthority` enum (Canonical > WorldState > Episodic > Belief)
  - `CanonicalFact` - Immutable world truths (Designer-only authority)
  - `WorldState` - Mutable game state (GameSystem+ authority)
  - `EpisodicMemory` - Conversation history with decay and significance
  - `BeliefMemory` - NPC opinions/relationships (can be wrong, can be contradicted)
- **Authoritative Memory System** (`AuthoritativeMemorySystem.cs`)
  - Authority boundary enforcement
  - Canonical fact protection (cannot be overridden or modified)
  - Mutation source validation
  - Belief contradiction detection against canonical facts
  - Episodic memory decay and pruning
  - Unified memory retrieval for prompt injection
  - Memory statistics and insights
- **Enhanced PersonaMemoryStore**
  - Integration with AuthoritativeMemorySystem
  - New structured API: `AddCanonicalFact`, `SetWorldState`, `AddDialogue`, `SetBelief`, `SetRelationship`
  - Memory decay support (`ApplyDecay()`, `ApplyDecayAll()`)
  - World-level canonical facts (`AddCanonicalFactToAll()`, `AddCanonicalFactToPersonas()`)
  - Backward compatibility flag (`UseAuthoritativeSystem`)
  - Memory statistics reporting

#### State Snapshot & Context Retrieval System
- **State Snapshot System** (`Core/Inference/`)
  - `StateSnapshot` - Immutable snapshot of all context at inference time
  - `StateSnapshotBuilder` - Fluent builder pattern for snapshot creation
  - `ForRetry()` method for creating retry snapshots with merged constraints
  - `GetAllMemoryForPrompt()` for formatted memory injection with type prefixes
  - Metadata support for debugging and logging
- **Context Retrieval Layer** (`Core/Inference/ContextRetrievalLayer.cs`)
  - `ContextRetrievalLayer` - Retrieves relevant context from AuthoritativeMemorySystem
  - `ContextRetrievalConfig` - Configurable limits and weighting parameters
  - Recency/relevance/significance scoring algorithm for episodic memories
  - Confidence-based filtering for beliefs
  - Topic-based filtering for all memory types
  - `RetrievedContext` container with `ApplyTo()` for snapshot building
- **Inference Result System** (`Core/Inference/InferenceResult.cs`)
  - `InferenceResult` - Single attempt result with validation outcome
  - `InferenceResultWithRetries` - Aggregated results across all attempts
  - `ConstraintViolation` - Detailed violation information
  - `ValidationOutcome` enum (Valid, ProhibitionViolated, RequirementNotMet, InvalidFormat)
  - `TokenUsage` for tracking prompt and completion tokens
- **Retry Policy** (`Core/Inference/RetryPolicy.cs`)
  - `RetryPolicy` - Configurable retry behavior (default: 2 retries = 3 total attempts)
  - `ConstraintEscalation` modes (None, AddSpecificProhibition, HardenRequirements, Full)
  - `GenerateRetryConstraints()` for automatic constraint escalation on failure
  - `GenerateRetryFeedback()` for retry prompt injection
  - Preset policies: Default, NoRetry, Aggressive
  - Time limit enforcement (`MaxTotalTimeMs`)
- **Response Validator** (`Core/Inference/ResponseValidator.cs`)
  - `ResponseValidator` - Validates responses against ConstraintSet
  - Pattern extraction from constraint descriptions (quoted strings, keywords)
  - Keyword and regex matching for prohibition/requirement checking
  - `ValidationResult` with violations list and outcome

#### Ephemeral Working Memory System
- **Working Memory Component** (`Core/Inference/EphemeralWorkingMemory.cs`)
  - `EphemeralWorkingMemory` - Short-lived memory for single inference
  - `WorkingMemoryConfig` - Configurable bounds (exchanges, memories, beliefs, characters)
  - Preset configurations: Default, Minimal, Expanded
  - Explicit bounding (last 5 exchanges, 5 memories, 3 beliefs by default)
  - Character-based truncation with priority to canonical facts and world state
  - `GetFormattedContext()` and `GetFormattedDialogue()` for prompt building
  - `GetStats()` and `WorkingMemoryStats` for debugging/metrics
  - `IDisposable` implementation for cleanup after inference
  - **Few-Shot Prompt Priming Support**
    - `FewShotExample` class for input-output demonstration pairs
    - `WorkingMemoryConfig` few-shot configuration (FewShotExamples, MaxFewShotExamples, AlwaysIncludeFewShot)
    - `GetFormattedFewShotExamples()` for formatted few-shot section injection
    - Deterministic ordering of examples (byte-stable)
    - Few-shot count tracking in `WorkingMemoryStats`
- **Prompt Assembler** (`Core/Inference/PromptAssembler.cs`)
  - `PromptAssembler` - Builds bounded, constrained prompts from snapshots
  - `PromptAssemblerConfig` - Token limits and format string configuration
  - Preset configurations: Default, SmallContext, LargeContext
  - `AssembleFromSnapshot()` - Creates working memory internally
  - `AssembleFromWorkingMemory()` - For pre-built working memory
  - `AssembleMinimal()` - For testing/simple use cases
  - Token estimation: `EstimateTokens()`, `EstimateCharacters()`
  - `AssembledPrompt` result with character/token counts and breakdown
  - `PromptSectionBreakdown` - Detailed character usage by section
  - **Few-Shot Integration**
    - `FewShotHeader` and `IncludeFewShotExamples` configuration options
    - Automatic few-shot section injection into prompts
    - Few-shot character count tracking in section breakdown
- **Few-Shot Conversion Utilities** (`Core/FallbackSystem.cs`)
  - `FallbackToFewShotConverter` - Utility for converting fallback responses to few-shot examples
  - `ConvertFallbacksToFewShot()` - Converts fallback lists to few-shot examples
  - `ConvertConfigToFewShot()` - Converts fallback configs by trigger reason to few-shot examples
  - Support for deterministic ordering and configurable max examples

#### Testing Infrastructure
- **Comprehensive Test Suite** (`LlamaBrain.Tests/`)
  - Standalone test project separate from Unity package
  - 85.54% overall code coverage (4,715 of 5,512 lines covered)
  - 39 files with 80%+ coverage, 0 files with 0% coverage
  - NUnit test framework with NSubstitute for mocking
  - Coverlet integration for code coverage analysis
- **Expectancy Tests** (`LlamaBrain.Tests/Expectancy/`)
  - 16 tests for ExpectancyEvaluator
  - 29 tests for Constraint and ConstraintSet
  - 10 tests for InteractionContext
  - All 50+ tests passing
- **Memory Tests** (`LlamaBrain.Tests/Memory/`)
  - ~25 tests for all memory types
  - ~20 tests for AuthoritativeMemorySystem authority enforcement
  - ~20 tests for PersonaMemoryStore backward compatibility and new API
  - All ~65 tests passing
- **Inference Tests** (`LlamaBrain.Tests/Inference/`)
  - ~15 tests for StateSnapshot and StateSnapshotBuilder
  - ~12 tests for InferenceResult and InferenceResultWithRetries
  - ~12 tests for RetryPolicy and constraint escalation
  - ~15 tests for ResponseValidator
  - ~15 tests for ContextRetrievalLayer
  - ~20 tests for EphemeralWorkingMemory and WorkingMemoryConfig
  - ~25 tests for PromptAssembler and PromptAssemblerConfig
  - All ~95+ tests passing
- **Validation Tests** (`LlamaBrain.Tests/Validation/`)
  - ~20 tests for OutputParser
  - ~20 tests for ValidationGate
  - ~20 tests for ParsedOutput, ProposedMutation, WorldIntent, GateResult
  - All ~60 tests passing
- **Core Component Tests**
  - 54 tests for PromptComposer (100% line and branch coverage)
  - 42 tests for LlmConfig (91.07% line coverage, 100% branch coverage)
  - 41 tests for FileSystem (100% line coverage)
  - Tests for ApiClient, BrainAgent, ClientManager, DialogueSession, ServerManager
  - Tests for PersonaProfileManager, PersonaMemoryFileStore, PersonaProfile
  - Tests for ProcessConfig, ApiContracts
- **Utility Tests** (`LlamaBrain.Tests/Utilities/`)
  - FileSystemTests - Comprehensive file operation testing
  - JsonUtilsTests - JSON serialization/deserialization testing
  - PathUtilsTests - Path manipulation testing
  - ProcessUtilsTests - Process management testing
- **Metrics Tests** (`LlamaBrain.Tests/Metrics/`)
  - DialogueInteractionTests - Comprehensive metrics collection testing
- **Coverage Reporting**
  - PowerShell script (`analyze-coverage.ps1`) for coverage analysis
  - Automated coverage report generation (`COVERAGE_REPORT.md`)
  - CSV export for coverage data analysis
  - Detailed file-by-file coverage breakdown with complexity metrics

#### Output Validation System
- **Output Parser** (`Core/Validation/OutputParser.cs`)
  - Parses LLM output into structured format
  - Extracts dialogue text, proposed mutations, world intents
  - Handles malformed outputs (stage directions, speaker labels, truncation)
  - Meta-text detection to reject explanatory/instructional responses
  - Configurable via `OutputParserConfig` with presets (Default, Structured, Minimal)
- **Parsed Output Types** (`Core/Validation/ParsedOutput.cs`)
  - `ParsedOutput` - Structured result container with dialogue, mutations, intents
  - `ProposedMutation` - Memory mutation proposals (AppendEpisodic, TransformBelief, TransformRelationship, EmitWorldIntent)
  - `WorldIntent` - NPC world-affecting desires with parameters and priority
  - `MutationType` enum for categorizing mutations
- **Validation Gate** (`Core/Validation/ValidationGate.cs`)
  - Validates parsed output against constraints from expectancy engine
  - Checks canonical fact contradictions against AuthoritativeMemorySystem
  - Validates knowledge boundaries (forbidden knowledge topics)
  - Validates proposed mutations (blocks canonical fact modifications)
  - Custom rule support via `ValidationRule` base class
  - `PatternValidationRule` for regex-based validation rules
  - `ValidationContext` for passing memory system and constraints
  - `GateResult` with failures, approved/rejected mutations, critical failure detection
  - `ValidationFailure` with reason, description, violating text, severity
  - Configurable via `ValidationGateConfig` with presets (Default, Minimal)

#### Controlled Memory Mutation
- **MemoryMutationController** (`Persona/MemoryMutationController.cs`)
  - Only validated outputs can trigger mutations
  - Mutation types: AppendEpisodic, TransformBelief, TransformRelationship, EmitWorldIntent
  - Enforces: Cannot override canonical facts (blocked with statistics tracking)
  - Logs all mutation attempts with configurable logging
  - `MutationExecutionResult` for individual mutation results
  - `MutationBatchResult` for aggregated batch results
  - `MutationStatistics` for tracking success/failure rates
  - Event-based world intent delivery via `OnWorldIntentEmitted`

#### Testability & Dependency Injection
- **IFileSystem Interface** (`Utilities/IFileSystem.cs`)
  - Interface abstraction for file system operations
  - Enables dependency injection and unit testing
  - Methods for directory creation, file operations, path manipulation
  - `IFileInfo` interface for file metadata
- **FileSystem Implementation** (`Utilities/FileSystem.cs`)
  - Default implementation using System.IO
  - 100% test coverage with comprehensive test suite
  - Wrapper for FileInfo to implement IFileInfo interface
- **IApiClient Interface** (`Core/IApiClient.cs`)
  - Interface abstraction for API client operations
  - Enables dependency injection and unit testing
  - Async prompt sending with configurable parameters
  - Enhanced with `SendPromptWithMetricsAsync` method with full parameter support
  - Improved documentation and method signatures

#### Testing Infrastructure
- **Comprehensive Test Suite** (`LlamaBrain.Tests/`)
  - Standalone test project separate from Unity package
  - 92.37% overall code coverage (5,100 of 5,521 lines covered) 🎉
  - 40 files with 80%+ coverage, 0 files with 0% coverage
  - 0 files with < 50% coverage (all files above 50%)
  - NUnit test framework with NSubstitute for mocking
  - Coverlet integration for code coverage analysis
- **Integration Tests** (`LlamaBrain.Tests/Integration/`)
  - FullPipelineIntegrationTests - Complete 9-component pipeline integration tests
  - Tests all components working together from InteractionContext through memory mutation
  - Validates end-to-end flow, retry logic, constraint escalation, and fallback system
- **Expectancy Tests** (`LlamaBrain.Tests/Expectancy/`)
  - 16 tests for ExpectancyEvaluator
  - 29 tests for Constraint and ConstraintSet
  - 10 tests for InteractionContext
  - All 50+ tests passing
- **Memory Tests** (`LlamaBrain.Tests/Memory/`)
  - ~25 tests for all memory types
  - ~20 tests for AuthoritativeMemorySystem authority enforcement
  - ~20 tests for PersonaMemoryStore backward compatibility and new API
  - All ~65 tests passing
- **Inference Tests** (`LlamaBrain.Tests/Inference/`)
  - ~15 tests for StateSnapshot and StateSnapshotBuilder
  - ~12 tests for InferenceResult and InferenceResultWithRetries
  - ~12 tests for RetryPolicy and constraint escalation
  - ~15 tests for ResponseValidator
  - ~15 tests for ContextRetrievalLayer
  - ~28 tests for EphemeralWorkingMemory and WorkingMemoryConfig (including few-shot tests)
  - ~39 tests for PromptAssembler and PromptAssemblerConfig
  - All ~95+ tests passing
- **Few-Shot Prompt Priming Tests**
  - 4 tests for `FewShotExample` class
  - 1 test for `WorkingMemoryConfig` few-shot settings
  - 8 tests for `EphemeralWorkingMemory` few-shot handling
  - 17 tests for `FallbackToFewShotConverter`
  - All 30 few-shot tests passing
- **Validation Tests** (`LlamaBrain.Tests/Validation/`)
  - ~20 tests for OutputParser
  - ~20 tests for ValidationGate
  - ~20 tests for ParsedOutput, ProposedMutation, WorldIntent, GateResult
  - All ~60 tests passing
- **Core Component Tests**
  - 54 tests for PromptComposer (100% line and branch coverage)
  - 42 tests for LlmConfig (91.07% line coverage, 100% branch coverage)
  - 41 tests for FileSystem (100% line coverage)
  - Comprehensive ApiClient tests (coverage improved from 36.36% to 90.54%)
  - Comprehensive ServerManager tests (coverage improved from 32.81% to 74.55%, 92.31% branch coverage)
  - Tests for BrainAgent, ClientManager, DialogueSession
  - Tests for PersonaProfileManager, PersonaMemoryFileStore, PersonaProfile
  - Tests for ProcessConfig, ApiContracts
- **Utility Tests** (`LlamaBrain.Tests/Utilities/`)
  - FileSystemTests - Comprehensive file operation testing
  - JsonUtilsTests - JSON serialization/deserialization testing
  - PathUtilsTests - Path manipulation testing
  - ProcessUtilsTests - Comprehensive process management testing (617+ lines)
- **Metrics Tests** (`LlamaBrain.Tests/Metrics/`)
  - DialogueInteractionTests - Comprehensive metrics collection testing
- **Coverage Reporting**
  - PowerShell script (`analyze-coverage.ps1`) for coverage analysis
  - Automated coverage report generation (`COVERAGE_REPORT.md`)
  - CSV export for coverage data analysis
  - Detailed file-by-file coverage breakdown with complexity metrics
  - Coverage tracking: 92.37% overall (up from 81.26%)

#### Project Organization
- Added `LlamaBrain.Tests` project to solution
- Moved tests from Unity package to core library
- Improved project structure and separation of concerns

#### Changed
- **PersonaMemoryStore** refactored to use AuthoritativeMemorySystem internally
  - Added world-level canonical facts support
  - Now uses IFileSystem interface for file operations (enables testing)
- **PromptComposer** now supports constraint injection
  - Achieved 100% line and branch coverage through comprehensive testing
  - Minor improvements to prompt composition logic
- **DialogueSession** updated to work with new memory system
- **ApiClient** significantly enhanced
  - Improved error handling and validation
  - Enhanced timeout management and request validation
  - Better HTTP client lifecycle management
  - Test coverage improved from 36.36% to 90.54%
- **IApiClient Interface** enhanced
  - Added `SendPromptWithMetricsAsync` method with full parameter support
  - Improved documentation and method signatures
  - Better support for dependency injection and testing
- **ServerManager** major improvements
  - Enhanced process lifecycle management
  - Improved error handling and validation
  - Better path resolution and argument building
  - Enhanced event handling and monitoring
  - Test coverage improved from 32.81% to 74.55% (92.31% branch coverage)
- **ClientManager** minor improvements
- **ProcessConfig** parameter validation enhancements
- **DialogueMetrics** enhanced with additional tracking fields
- **Test Coverage** significantly improved
  - Overall coverage increased from 81.26% to 92.37% (5,100 of 5,521 lines covered)
  - 40 files now have 80%+ coverage (up from previous count)
  - 0 files with 0% coverage (all files have test coverage)
  - 0 files with < 50% coverage (all files above 50%)
  - FileSystem (100%), PromptComposer (100%), LlmConfig (91.07%), ApiClient (90.54%) now have excellent coverage
  - ServerManager (74.55%) and ProcessUtils significantly improved
- Removed obsolete documentation files (moved to Documentation folder)
  - `CHANGELOG.md` from root (moved to Documentation/)
  - `LLAMABRAIN.md` (consolidated into README)
  - `SAFEGUARDS.md` from Source/Core (moved to Documentation/)
  - `RED_ROOM_THREAT_MODEL.md` from Documentation (consolidated)
- Removed test files from Unity package (moved to core library)
  - `ApiContractsTests.cs`, `PersonaProfileTests.cs`, `ProcessConfigTests.cs`, `PromptComposerTests.cs` from EditMode
  - `ApiClientTests.cs` from PlayMode
- Removed `DialogueMetrics.cs` from Unity Runtime (moved to Core library)

### Managed Host (Unity Runtime)

#### Added

#### Determinism Layer & Expectancy Engine
- **Unity Integration** (`Runtime/Core/Expectancy/ExpectancyEngine.cs`, `ExpectancyRuleAsset.cs`, `NpcExpectancyConfig.cs`)
  - Unity MonoBehaviour wrapper with singleton pattern
  - ScriptableObject-based rule assets
  - NPC-specific expectancy configuration
  - Auto-detection of expectancy configs on LlamaBrainAgent
- **Agent Integration**
  - `LlamaBrainAgent` now supports expectancy evaluation
  - Context-aware input methods (`SendPlayerInputWithContextAsync`)
  - Automatic constraint injection into prompts
  - Last constraints tracking for debugging

#### RedRoom Testing Infrastructure
- **Dialogue Interaction System**
  - `NpcDialogueTrigger` - Collision-based dialogue trigger system with ITrigger interface
  - `NpcTriggerCollider` - Collider component for trigger zones
  - Support for prompt text, fallback responses, and conversation tracking
  - Prompt count tracking per trigger
  - UnityEvent integration for conversation text generation
- **Player Interaction System**
  - `RedRoomPlayerRaycast` - Camera-based raycast for NPC detection
  - Configurable layer mask and raycast distance
  - Event-driven hit/miss detection
  - Visual indicator system for NPC targeting
- **UI Management**
  - `RedRoomCanvas` - Singleton UI manager for conversation display
  - Conversation panel management with keyboard controls (E to interact, Escape to close)
  - Integration with raycast system for seamless NPC interaction
  - `ExampleBillboardUI` - Billboard-style UI component for NPCs
- **Metrics Collection System**
  - `DialogueMetricsCollector` - Singleton metrics collection system
  - `DialogueMetrics` - Comprehensive interaction metrics tracking
  - Rolling file system to prevent files from growing too large
  - Configurable limits: interactions per file, file size (MB), time per file
  - Automatic file cleanup with configurable retention policy
  - CSV and JSON export formats for analysis
  - Session management with unique session IDs
  - Auto-export on application quit
- **NPC AI System**
  - `NpcFollowerExample` - NavMesh-based NPC follower with dialogue integration
  - Refactored component hierarchy for better organization
  - Optimized trigger lookups (removed FindObjectsOfType calls)
  - Integration with dialogue trigger system
- **RedRoom Integration** (Feature 8)
  - Enhanced `DialogueMetricsCollector` with architectural pattern metrics tracking
  - Added validation pass/fail tracking, retry metrics per interaction, constraint violation tracking, and fallback usage tracking to metrics system
  - Updated CSV and JSON export formats to include all new architectural metrics
  - Enhanced session summary with architectural pattern statistics
  - `NpcDialogueTrigger` now supports trigger-specific expectancy rules
  - Context-aware interaction tracking improved
- **Memory Mutation Overlay** (`Runtime/RedRoom/UI/MemoryMutationOverlay.cs`)
  - Real-time memory state viewer panel with toggleable sections for all memory types
  - Displays canonical facts with read-only protection indicators, world state with change indicators, episodic memories with significance scores and decay status, and beliefs with confidence scores
  - Mutation execution tracker showing approved vs rejected mutations with color coding
  - Mutation statistics display including success rate, blocked attempts, and authority violations
  - Integrated with RedRoomCanvas with F2 hotkey toggle and auto-refresh on interaction events (0.5s polling interval)
  - Configurable section toggles and display limits for performance optimization
- **Memory Mutation Overlay Setup** (`Runtime/RedRoom/UI/MemoryMutationOverlaySetup.cs`)
  - Auto-setup helper component for programmatic UI generation at runtime
  - Configurable styling and layout
  - Prefab-based instantiation system
  - Automatic reference wiring
- **Validation Gate Overlay** (`Runtime/RedRoom/UI/ValidationGateOverlay.cs`)
  - Real-time validation results display with visual pass/fail indicators
  - Failure reasons grouped by severity (Critical, Hard, Soft) with violating text snippets
  - Constraint evaluation status showing active constraints grouped by type with severity indicators and rule source attribution
  - Retry attempt visualization with current attempt number, max attempts progress bar, constraint escalation status, and retry history
  - Integrated with RedRoomCanvas with F3 hotkey toggle and auto-refresh on validation events (0.3s polling interval)
  - Constraint demonstration features integrated into this unified overlay

#### Memory System Integration
- **Memory Decay Integration**
  - Automatic periodic memory decay in `LlamaBrainAgent`
  - Configurable decay interval (default: 300 seconds)
  - Inspector controls for enabling/disabling auto-decay
  - More significant memories decay slower
- **Canonical Facts Initialization**
  - `InitializeCanonicalFact()` method on `LlamaBrainAgent` for individual NPCs
  - World-level canonical facts via `AddCanonicalFactToAll()` for shared world truths
  - Selective canonical facts via `AddCanonicalFactToPersonas()` for specific NPC groups

#### State Snapshot & Context Retrieval
- **Unity Integration** (`Runtime/Core/Inference/UnityStateSnapshotBuilder.cs`)
  - Unity-specific builder with Time.time and scene integration
  - `BuildForNpcDialogue()` for player interactions
  - `BuildForZoneTrigger()` for trigger interactions with trigger-specific rules
  - `BuildWithExplicitContext()` for custom use cases
  - `BuildMinimal()` for testing and simple scenarios
- **LlamaBrainAgent Enhancement**
  - New `SendWithSnapshotAsync()` method using snapshot-based inference pipeline
  - `LastSnapshot` property for debugging state at inference time
  - `LastInferenceResult` property for debugging retry attempts
  - Full retry loop with constraint escalation and time limit enforcement
  - Token usage tracking across attempts

#### Ephemeral Working Memory
- **Unity Integration** (`Runtime/Core/Inference/PromptAssemblerSettings.cs`)
  - ScriptableObject for editor-configurable prompt assembly settings
  - Token limits, working memory limits, content inclusion toggles
  - Customizable format strings
  - `ToConfig()` and `ToWorkingMemoryConfig()` converters
- **LlamaBrainAgent Updates**
  - `promptAssemblerSettings` field for optional ScriptableObject configuration
  - `LastAssembledPrompt` property for debugging/metrics
  - Updated `SendWithSnapshotAsync()` to use PromptAssembler
  - Proper WorkingMemory disposal after each inference attempt

#### Output Validation
- **Unity Integration** (`Runtime/Core/Validation/`)
  - `ValidationRuleAsset` - ScriptableObject for designer-created rules
  - `ValidationRuleSetAsset` - Groups of rules for easy assignment
  - `ValidationPipeline` - MonoBehaviour combining parser and gate
  - Support for context conditions: scene filter, NPC ID filter, trigger reason filter
- **LlamaBrainAgent Integration**
  - `outputParser` and `validationGate` fields
  - `LastParsedOutput` and `LastGateResult` properties for debugging
  - Updated `SendWithSnapshotAsync()` to use full validation pipeline
  - Critical failures skip retry and use fallback
  - Non-critical failures trigger retry with constraint escalation

#### Enhanced Fallback System
- **AuthorControlledFallback** (`Runtime/Core/AuthorControlledFallback.cs`)
  - Generic safe responses with configurable fallback lists
  - Context-aware fallbacks based on TriggerReason (PlayerUtterance, ZoneTrigger, TimeTrigger, QuestTrigger, NpcInteraction, WorldEvent, Custom)
  - Emergency fallbacks as absolute last resort
  - Comprehensive failure reason logging and tracking
  - Fallback statistics tracking (total fallbacks, by trigger reason, by failure reason, emergency fallbacks)
  - `FallbackConfig` for customizable fallback responses
  - `FallbackStats` for metrics and debugging
- **LlamaBrainAgent Integration**
  - Automatic fallback activation after all retry attempts are exhausted
  - `BuildFailureReason()` method for detailed failure logging
  - `FallbackStats` property for accessing fallback usage statistics
  - Updated `GenerateFallbackResponse()` to use new AuthorControlledFallback system
  - Fallback responses are treated as successful for dialogue history storage
- **NpcDialogueTrigger Enhancement**
  - `GetFallbackStats()` method for accessing fallback statistics from associated NPC agents
  - Automatic fallback integration through LlamaBrainAgent

#### World Intent System
- **WorldIntentDispatcher** (`Runtime/Core/WorldIntentDispatcher.cs`)
  - Unity MonoBehaviour component for dispatching world intents to game systems
  - Singleton pattern for global access
  - UnityEvent integration for designer-friendly intent handling
  - Code-based handler registration for programmatic intent handling
  - Intent history tracking with configurable size limits
  - Support for intent-specific handlers and wildcard handlers
  - Automatic hooking to MemoryMutationController for intent emission
  - Debug logging and statistics tracking
  - `WorldIntentRecord` for history tracking with NPC ID and game time
- **DialogueMetricsExtensions** (`Runtime/RedRoom/Interaction/DialogueMetricsExtensions.cs`)
  - Extension methods bridging Unity Runtime types to Core metrics interfaces
  - `FromMetrics()` for creating DialogueInteraction from Unity types
  - `PopulateArchitecturalMetrics()` for populating metrics from LlamaBrainAgent state
- **Memory Mutation Integration**
  - `LlamaBrainAgent` integration with MemoryMutationController
  - `LastMutationBatchResult` property for debugging/metrics
  - `MutationStats` property for statistics access
  - `HookIntentDispatcher()` and `UnhookIntentDispatcher()` methods
  - Mutations automatically executed after successful validation

#### Changed
- **LlamaBrainAgent** significantly refactored and enhanced
  - Code simplification and optimization (595 lines refactored)
  - Enhanced with expectancy evaluation support
  - Added `ExpectancyConfig` field with auto-detection
  - Added `LastConstraints` property for debugging
  - Enhanced prompt composition with constraint injection
  - Enhanced with automatic memory decay and canonical facts initialization
  - Improved error handling and state management
- **BrainServer** major improvements
  - Enhanced server lifecycle management (245 lines changed)
  - Improved error handling and monitoring
  - Better integration with ServerManager
  - Enhanced event handling and logging
- **NpcDialogueTrigger** updated to support expectancy rules and trigger-specific rule sets
  - Added `GetMutationStats()` method for accessing mutation statistics
  - Added `GetLastMutationBatchResult()` method for debugging
- **DialogueMetricsCollector** enhanced with rolling file system and improved export capabilities
- **RedRoomCanvas** improved with better conversation management and keyboard controls
- **RedRoomPlayerRaycast** optimized for better NPC detection
- **NpcFollowerExample** refactored component hierarchy for better organization
- **BrainSettings** and **PersonaConfig** minor enhancements

#### Fixed
- Performance optimizations in RedRoom interaction system
- Improved trigger lookup efficiency (removed FindObjectsOfType calls)
- Fixed component hierarchy in NpcFollowerExample
- Documentation link fixes

### Documentation
- **New ARCHITECTURE.md** - Comprehensive architectural documentation
  - Complete explanation of the 9-component architectural pattern
  - Detailed component descriptions with code examples
  - Complete flow examples showing all components working together
  - Best practices and troubleshooting guides
  - Implementation status and test coverage information
- **Major README.md Updates** (326 lines changed)
  - Enhanced architecture overview with component descriptions
  - Updated feature list and capabilities
  - Improved installation and usage instructions
  - Better code examples and integration guides
- **Updated ROADMAP.md** (818 lines changed)
  - Comprehensive tracking of implementation phases
  - Updated status for all components
  - Detailed progress information
- **Updated USAGE_GUIDE.md** (251 lines changed)
  - Enhanced Unity integration examples
  - Updated usage patterns for all phases
  - Better code examples and best practices
- **Updated COVERAGE_REPORT.md** (165 lines changed)
  - Updated coverage statistics (92.37% overall coverage)
  - Detailed file-by-file coverage breakdown
  - Coverage improvement tracking and analysis
- Updated SAFEGUARDS.md with latest security information
- Enhanced code documentation with XML comments
- Added coverage analysis PowerShell script for automated reporting
- Reorganized documentation structure (moved to Documentation/ folder)
- **Updated ROADMAP.md** - Added Feature 14: Deterministic Generation Seed
  - Comprehensive plan for cross-session determinism via InteractionCount seed strategy
  - Detailed implementation plan with seed parameter support, integration with InteractionContext, and cross-session determinism testing
  - Documents the "double-lock system" (context locking + entropy locking) for complete determinism
  - Includes proof strategy and architectural significance
- **Updated ARCHITECTURE.md** - Added "Planned Features" section
  - Feature 11: RAG-Based Memory Retrieval & Memory Proving
  - Feature 12: Dedicated Structured Output
  - Feature 13: Structured Output Integration
  - Feature 14: Deterministic Generation Seed
  - Each feature includes overview, key components, architectural impact, and roadmap links
- **Terminology Update** - Changed "Phase" to "Feature" throughout documentation
  - Updated ROADMAP.md, STATUS.md, PHASE10_PROOF_GAPS.md, and ARCHITECTURE.md
  - Table headers now use "Features" (plural) for consistency
  - All feature references updated from "Phase X" to "Feature X"
  - Maintains backward compatibility with existing documentation structure
- **New MEMORY.md** - Comprehensive memory system documentation
  - Complete documentation of Component 3: External Authoritative Memory System
  - Detailed explanation of memory authority hierarchy (Canonical > WorldState > Episodic > Belief)
  - Memory type descriptions with use cases and authority boundaries
  - Deterministic ordering and sequence number system
  - Memory decay, pruning, and retrieval algorithms
  - Integration with the broader LlamaBrain architecture
  - Code examples and best practices
- **New PIPELINE_CONTRACT.md** - Formal pipeline contract specification
  - Version 1.0.0 contract defining the 9-component architectural pipeline
  - Exact flow, data contracts, and behavioral guarantees
  - Component execution order and data flow specifications
  - Error handling and retry logic contracts
  - State reconstruction and determinism guarantees
  - Implementation requirements and validation criteria
- **New VALIDATION_GATING.md** - Comprehensive validation system documentation
  - Complete documentation of Component 7: Validation Gating System
  - Five sequential validation gates (Output Parsing, Constraint Validation, Canonical Fact Validation, Knowledge Boundary Validation, Mutation Validation)
  - Detailed gate descriptions with implementation details
  - Failure handling and retry logic
  - Integration with expectancy engine and memory system
  - Code examples and validation patterns
- **New DETERMINISM_CONTRACT.md** - Explicit determinism boundary statement
  - Determinism guarantees and limitations
  - Hardware determinism documentation
  - Contract decisions and boundary definitions
- **New SAFEGUARDS.md** - Security and safety documentation
  - Threat mitigation strategies
  - Best practices for secure implementation
  - Security safeguards and input validation
- **New PHASE10_PROOF_GAPS.md** - Deterministic proof gap testing documentation
  - Test backlog and acceptance criteria
  - Feature 10 implementation requirements
- **New MEMORY_TODO.md** - Memory system enhancement roadmap
  - RAG-based retrieval planning
  - Memory proving system design
- **Feature 8: RedRoom Integration**
  - Enhanced metrics collection system with validation pass/fail tracking, retry metrics per interaction, constraint violation tracking, and fallback usage tracking
  - Updated CSV and JSON export formats to include architectural pattern metrics
  - Enhanced session summary with comprehensive architectural metrics
  - Implemented Memory Mutation Overlay with real-time memory state viewing accessible via F2 hotkey
  - Implemented Validation Gate Overlay with real-time validation results and constraint evaluation accessible via F3 hotkey
  - Integrated constraint demonstration features into Validation Gate Overlay for unified debugging experience
  - Built overlay system infrastructure with input-managed panels and auto-refresh event system
  - Added full pipeline integration tests (8 tests) and Unity PlayMode integration tests (73+ tests)
  - Updated RedRoom README with architectural pattern documentation and troubleshooting guides
  - Some overlay fixes and improvements pending
- **Feature 9: Documentation & Polish**
  - Created comprehensive architecture documentation suite covering all aspects of the LlamaBrain system. ARCHITECTURE.md provides complete explanation of the 9-component architectural pattern with code examples and best practices. DETERMINISM_CONTRACT.md establishes explicit boundary statements and determinism guarantees including hardware determinism limitations. PIPELINE_CONTRACT.md defines formal pipeline specification with exact data contracts and behavioral guarantees. MEMORY.md documents the memory system architecture including authority hierarchy and memory types. VALIDATION_GATING.md describes the validation system with detailed gate descriptions and failure handling. SAFEGUARDS.md covers security documentation and threat mitigation strategies. PHASE10_PROOF_GAPS.md provides proof gap testing documentation with test backlog and acceptance criteria. MEMORY_TODO.md outlines the memory enhancement roadmap including RAG-based retrieval planning.
  - Created status tracking documentation to monitor project progress. STATUS.md tracks current implementation status with feature completion percentages and test coverage reporting. ROADMAP.md provides comprehensive feature roadmap with detailed implementation plans and execution order recommendations for all features.
  - Enhanced Unity package documentation with complete integration guides. USAGE_GUIDE.md (Unity Runtime) contains comprehensive component-by-component usage guide with four step-by-step tutorials covering deterministic NPC setup, validation rule creation, memory authority understanding, and validation failure debugging. USAGE_GUIDE.md (Core Library) provides core library usage examples and API patterns. QUICK_START.md enables rapid Unity integration. TROUBLESHOOTING.md addresses common issues and solutions. SAMPLES.md provides code samples. STRUCTURED_OUTPUT.md documents structured output features. RED_ROOM_THREAT_MODEL.md covers RedRoom security threat model.
  - Added four comprehensive tutorials to USAGE_GUIDE.md providing step-by-step guidance: "Setting Up Deterministic NPCs" walks through creating NPCs with deterministic behavior (6 steps), "Creating Custom Validation Rules" explains rule creation and application at multiple levels (8 steps), "Understanding Memory Authority" teaches the memory authority hierarchy and usage patterns (7 steps), and "Debugging Validation Failures" provides debugging tools and techniques including reusable debug helper scripts (9 steps).
  - Updated main README.md with enhanced architecture overview, current completion status, and improved feature descriptions to help users understand the system capabilities.
  - Completed API documentation with XML comments achieving 100% coverage of all public APIs and generated Doxygen output with zero missing member warnings, ensuring comprehensive API reference documentation.

## [0.1.0] - 2025-07-24

### Core Library

#### Added
- Initial release of LlamaBrain core library
- Core AI integration with llama.cpp server (ApiClient)
- PersonaConfig system for character personalities (core data structures)
- Memory management with persistent storage (PersonaMemoryStore)
- Basic prompt composition (PromptComposer)
- Dialogue session management
- Performance metrics tracking (CompletionMetrics)

#### Technical
- .NET Standard 2.1 compatibility
- Comprehensive unit tests (NUnit and NSubstitute)

### Managed Host (Unity Runtime)

#### Added
- Initial Unity integration release
- UnityBrainAgent component for AI-powered NPCs
- BrainServer component for server management (Unity MonoBehaviour)
- Built-in UI components for dialogue systems
- Editor tools and custom inspectors
- Complete documentation and troubleshooting guides

#### Features
- **AI-Powered NPCs**: Intelligent characters with persistent personalities and memory
- **Dynamic Dialogue Systems**: Natural conversations that adapt to player interactions
- **Easy Unity Integration**: Simple setup with ScriptableObject-based configuration
- **Real-time AI Processing**: Local llama.cpp server integration for privacy and performance
- **Memory Management**: Persistent conversation history and character memory
- **Trait System**: Reusable personality traits for character customization
- **Security Safeguards**: Multi-layered security measures and input validation
- **Performance Optimization**: Rate limiting, caching, and resource management

#### Technical
- Unity 6000.0.58f2 LTS
- Assembly definitions for proper package structure
- TextMeshPro integration for rich text support
- EditMode and PlayMode tests for integration testing

#### Documentation
- Complete README with setup instructions
- Sample scene documentation
- Troubleshooting guide
- Security documentation
- API documentation
- Best practices guide

#### Samples
- Getting Started scene
- NPC Persona Chat example
- Journal Writer assistant
- Flavor Text generation
- Themed Rewrite system
- Memory Management demonstration
- Client Management tools

---

## Version History

### Current Version
- **0.2.0-rc.2**: Feature 10 Complete - Deterministic Proof Gap Testing (Phase 10 COMPLETE), WorldIntentDispatcher Singleton Lifecycle (Requirement #5), Pipeline Proof Gaps (double-hook safety, policy boundary proof), Snapshot-time driven context retrieval for deterministic behavior, Comprehensive regression tests for memory serialization (1,000+ lines), Byte-level prompt text determinism tests (Test D complete), 353 total tests (exceeds original estimate), All 7 minimal proof suite tests complete, All 5 critical requirements implemented, Determinism proof now defensible at byte level for serialized state and prompt text assembly

### Previous Versions
- **0.2.0-rc.1**: Features 1-9 Complete - Determinism Layer (Expectancy Engine), Structured Memory System, State Snapshots & Context Retrieval, Ephemeral Working Memory (with Few-Shot Prompt Priming), Output Validation, Controlled Memory Mutation (MemoryMutationController & World Intent Dispatcher), Enhanced Fallback System, RedRoom Integration (with Memory Mutation Overlay and Validation Gate Overlay), Documentation & Polish (comprehensive documentation suite with 4 tutorials), Comprehensive Testing Infrastructure (92.37% coverage with integration tests), Testability Improvements (IFileSystem, IApiClient interfaces), Major Test Coverage Improvements (ApiClient 90.54%, ServerManager 74.55%), Full Pipeline Integration Tests, and Complete Documentation Suite (ARCHITECTURE.md, DETERMINISM_CONTRACT.md, PIPELINE_CONTRACT.md, MEMORY.md, VALIDATION_GATING.md, SAFEGUARDS.md, USAGE_GUIDE.md with tutorials, STATUS.md, ROADMAP.md, and more)
- **0.1.0**: Initial Unity integration

