# Changelog

All notable changes to LlamaBrain will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0-rc.1] - 2025-12-31 (Unreleased)

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
- **New PIPELINE_CONTRACT.md** - Formal pipeline contract specification
  - Version 1.0.0 contract defining the 9-component architectural pipeline
  - Exact flow, data contracts, and behavioral guarantees
  - Component execution order and data flow specifications
  - Error handling and retry logic contracts
  - State reconstruction and determinism guarantees
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

## [0.1.0] - 2025-7-24

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
- Unity 2022.3 LTS or higher support
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
- **0.2.0-rc.1**: Features 1-9 Complete - Determinism Layer (Expectancy Engine), Structured Memory System, State Snapshots & Context Retrieval, Ephemeral Working Memory (with Few-Shot Prompt Priming), Output Validation, Controlled Memory Mutation (MemoryMutationController & World Intent Dispatcher), Enhanced Fallback System, RedRoom Integration (with Memory Mutation Overlay and Validation Gate Overlay), Documentation & Polish (comprehensive documentation suite with 4 tutorials), Comprehensive Testing Infrastructure (92.37% coverage with integration tests), Testability Improvements (IFileSystem, IApiClient interfaces), Major Test Coverage Improvements (ApiClient 90.54%, ServerManager 74.55%), Full Pipeline Integration Tests, and Complete Documentation Suite (ARCHITECTURE.md, DETERMINISM_CONTRACT.md, PIPELINE_CONTRACT.md, MEMORY.md, VALIDATION_GATING.md, SAFEGUARDS.md, USAGE_GUIDE.md with tutorials, STATUS.md, ROADMAP.md, and more)

### Previous Versions
- **0.1.0**: Initial Unity integration
y