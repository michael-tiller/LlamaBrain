# LlamaBrain Implementation Roadmap

**Goal**: Implement the complete "Continuity Emerges from Deterministic State Reconstruction Around a Stateless Generator" architectural pattern.

**Current Status**: ~97% Complete (Phase 1-7 Complete; Phase 8-9 In Progress; Core Integration Tests: Complete; Coverage: 68.36% line, ~55% branch)

---

## Progress Overview

Phase 1: Determinism Layer                  [x] 100%
Phase 2: Structured Memory System           [x] 100%
Phase 3: State Snapshot & Context Retrieval [x] 100%
Phase 4: Ephemeral Working Memory           [x] 100%
Phase 5: Output Validation System           [x] 100%
Phase 6: Controlled Memory Mutation         [x] 100%
Phase 7: Enhanced Fallback System           [x] 100%
Phase 8: RedRoom Integration                [ ] 75%
Phase 8.5: Missing Test Coverage           [ ] 75% (+ DialogueInteraction complete)
Phase 9: Documentation                      [ ] 30%

---

## ✅ Completed (Foundation Phase)

### Core LLM Integration
- [x] Stateless inference core (LlamaBrainAgent)
- [x] ApiClient with llama.cpp integration
- [x] Basic prompt composition (PromptComposer)
- [x] Performance metrics tracking (CompletionMetrics)

### Testing Infrastructure (RedRoom)
- [x] NpcDialogueTrigger system
- [x] RedRoomPlayerRaycast for NPC detection
- [x] Visual indicator system
- [x] Player interaction (E key press)
- [x] DialogueMetricsCollector
- [x] Rolling file system for data export
- [x] CSV/JSON export for analysis
- [x] NpcFollowerExample with NavMesh pathfinding
- [x] Refactored component hierarchy (NpcFollowerExample as head component)
- [x] Optimized trigger lookups (removed FindObjectsOfType calls)

### Basic Memory System
- [x] PersonaMemoryStore (basic implementation)
- [x] PersonaProfile with traits
- [x] Conversation history tracking

---

## 🚧 In Progress

### Phase 8: RedRoom Integration
**Status**: 75% Complete
**Priority**: MEDIUM - Enables testing of new architecture
- ✅ Metrics Enhancement (8.1) - Complete
- ✅ Collector Enhancement (8.2) - Complete
- ✅ Documentation (8.3) - Complete
- [ ] Sample Scenes (8.4) - Pending
- [ ] Integration Testing - See Phase 8.5.6

### Phase 8.5: Missing Test Coverage
**Status**: 30% Complete (Core tests complete, additional coverage needed)
**Priority**: MEDIUM - Improve test coverage for remaining components
**Current Coverage**: 61.2% line coverage, 49.1% branch coverage (592 tests passing)

#### Completed Test Suites
- [x] All Inference Pipeline Tests (136 tests) - ✅ COMPLETE
- [x] All Core Integration Tests (179 tests) - ✅ COMPLETE
  - BrainAgentTests (42 tests)
  - ClientManagerTests (20 tests)
  - ServerManagerTests (34 tests)
  - DialogueSessionTests (39 tests)
  - DialogueEntryTests (15 tests)
  - CompletionMetricsTests (29 tests)

#### Remaining Test Coverage Gaps
- [ ] **PromptComposer** (~20% coverage) - Additional edge case tests needed
  - Complex prompt composition scenarios
  - Template variable substitution edge cases
  - Error handling for malformed templates
  - ~15-20 additional tests estimated
- [ ] **ResponseValidator** (~33% coverage) - More comprehensive validation tests needed
  - Additional constraint validation scenarios
  - Edge cases in pattern matching
  - Complex validation rule combinations
  - ~10-15 additional tests estimated
- [ ] **ProcessConfig** - Configuration validation tests
  - Invalid configuration handling
  - Default value validation
  - Configuration serialization/deserialization
  - ~5-10 tests estimated
- [ ] **PersonaProfileManager** - Profile management tests
  - Profile CRUD operations
  - Profile persistence
  - Profile validation
  - ~10-15 tests estimated

**Estimated Effort**: 2-3 days to reach 70%+ coverage

### Phase 9: Documentation & Polish
**Status**: 30% Complete
**Priority**: MEDIUM - Enables adoption and understanding

---

## ✅ Phase 1: Determinism Layer & Expectancy Engine (COMPLETE)

**Priority**: CRITICAL - Foundation for all downstream work

**Status**: 100% Complete

### Components Built

#### 1.1 Expectancy Engine (COMPLETE)
- [x] Created `LlamaBrain/Source/Core/Expectancy/ExpectancyEvaluator.cs`
  - Engine-agnostic core evaluator (can be used by Unreal/Godot)
  - Generates constraints based on interaction context
  - Outputs ConstraintSet for prompt construction and validation
- [x] Created `LlamaBrainRuntime/Runtime/Core/Expectancy/ExpectancyEngine.cs`
  - Unity MonoBehaviour wrapper with singleton pattern
  - Supports global rules (ScriptableObject) + code-based rules
  - Debug logging support

#### 1.2 Rule System (COMPLETE)
- [x] Created `LlamaBrainRuntime/Runtime/Core/Expectancy/ExpectancyRuleAsset.cs` (ScriptableObject)
  - Rule types: Permission, Prohibition, Requirement
  - Severity levels: Soft, Hard, Critical
  - Condition system: TriggerReason, NpcId, TriggerId, SceneName, HasTag, InteractionCount
  - Condition combine modes: All (AND), Any (OR)
  - Editor-friendly with `[CreateAssetMenu]`
- [x] Created `LlamaBrainRuntime/Runtime/Core/Expectancy/NpcExpectancyConfig.cs`
  - NPC-specific rules component
  - Auto-creates InteractionContext with Unity time/scene

#### 1.3 Integration (COMPLETE)
- [x] Modified `LlamaBrainRuntime/Runtime/Core/LlamaBrainAgent.cs`
  - Added `ExpectancyConfig` field with auto-detection
  - Evaluates rules before prompt composition
  - Injects constraints via `ConstraintSet.ToPromptInjection()`
  - Added `SendPlayerInputWithContextAsync()` for context-aware triggers
  - Tracks `LastConstraints` for debugging/metrics

#### 1.4 Testing (COMPLETE)
- [x] Unit tests for ExpectancyEvaluator (16 tests)
- [x] Unit tests for Constraint and ConstraintSet (29 tests)
- [x] Unit tests for InteractionContext (10 tests)
- [x] All 50 tests passing in `LlamaBrain.Tests/Expectancy/`

---

## ✅ Phase 2: Structured Memory System (COMPLETE)

**Priority**: HIGH - Required for proper state management

**Status**: 100% Complete

**Dependencies**: None

### Components Built

#### 2.1 Memory Type Hierarchy (COMPLETE)
- [x] Created `LlamaBrain/Source/Persona/MemoryTypes/` folder
- [x] `MemoryAuthority.cs` - Authority levels (Canonical > WorldState > Episodic > Belief)
- [x] `MemoryEntry.cs` - Base class with MutationResult
- [x] `CanonicalFact.cs` - Immutable world truths (Designer-only)
- [x] `WorldState.cs` - Mutable game state (GameSystem+ authority)
- [x] `EpisodicMemory.cs` - Conversation history with decay and significance
- [x] `BeliefMemory.cs` - NPC opinions/relationships (can be wrong, can be contradicted)

#### 2.2 Authoritative Memory System (COMPLETE)
- [x] Created `LlamaBrain/Source/Persona/AuthoritativeMemorySystem.cs`
  - Manages all memory types with authority boundaries
  - Enforces: Canonical facts cannot be overridden or modified
  - Validates mutation source authority before allowing changes
  - Detects belief contradictions against canonical facts
  - Supports episodic memory decay and pruning
  - Provides unified `GetAllMemoriesForPrompt()` for prompt injection

#### 2.3 Migration (COMPLETE)
- [x] Refactored `LlamaBrain/Source/Persona/PersonaMemoryStore.cs`
  - Uses AuthoritativeMemorySystem internally
  - `UseAuthoritativeSystem` flag for backward compatibility
  - New structured API: `AddCanonicalFact`, `SetWorldState`, `AddDialogue`, `SetBelief`, `SetRelationship`
  - `ApplyDecay()` and `ApplyDecayAll()` for memory decay
  - `AddCanonicalFactToAll()` and `AddCanonicalFactToPersonas()` for world-level facts
  - `GetStatistics()` for memory system insights
- [x] Integrated memory decay in `LlamaBrainAgent`
  - Automatic periodic decay via `Update()` method
  - Configurable via inspector (enableAutoDecay, decayIntervalSeconds)
- [x] Integrated canonical facts initialization
  - `InitializeCanonicalFact()` method on `LlamaBrainAgent`
  - World-level facts support via `PersonaMemoryStore`

#### 2.4 Testing (COMPLETE)
- [x] `MemoryTypesTests.cs` - Tests for all memory types (~25 tests)
- [x] `AuthoritativeMemorySystemTests.cs` - Authority enforcement tests (~20 tests)
- [x] `PersonaMemoryStoreTests.cs` - Backward compatibility + new API (~20 tests)
- [x] All ~65 tests in `LlamaBrain.Tests/Memory/`

---

## ✅ Phase 3: State Snapshot & Context Retrieval (COMPLETE)

**Priority**: HIGH - Enables retry logic and bounded context

**Status**: 100% Complete

**Dependencies**: Phase 2 (Structured Memory)

### Components Built

#### 3.1 State Snapshot System (COMPLETE) - Base Project
- [x] Created `LlamaBrain/Source/Core/Inference/StateSnapshot.cs` (engine-agnostic)
  - Immutable snapshot of all context at inference time
  - Components: retrieved memory, world state, constraints, history, metadata
  - `ForRetry()` method for creating retry snapshots with merged constraints
  - `GetAllMemoryForPrompt()` for formatted memory injection
  - `StateSnapshotBuilder` with fluent API
#### 3.1.1 Unity Wrapper
- [x] Created `LlamaBrainRuntime/Runtime/Core/Inference/UnityStateSnapshotBuilder.cs`
  - Unity-specific builder with Time.time and scene integration
  - `BuildForNpcDialogue()` for player interactions
  - `BuildForZoneTrigger()` for trigger interactions
  - `BuildWithExplicitContext()` for custom use cases

#### 3.2 Context Retrieval Layer (COMPLETE) - Base Project
- [x] Created `LlamaBrain/Source/Core/Inference/ContextRetrievalLayer.cs` (engine-agnostic)
  - Retrieves relevant context from AuthoritativeMemorySystem
  - `ContextRetrievalConfig` for configurable limits and weighting
  - Recency/relevance/significance scoring for episodic memories
  - Confidence-based filtering for beliefs
  - Topic-based filtering for all memory types
  - `RetrievedContext.ApplyTo()` for easy snapshot building
- [x] Created scoring algorithm
  - Configurable weights: RecencyWeight, RelevanceWeight, SignificanceWeight
  - Keyword matching for relevance (can be extended with embeddings)
  - Contraicted belief exclusion by default

#### 3.3 Inference Result & Retry Policy (COMPLETE) - Base Project
- [x] Created `LlamaBrain/Source/Core/Inference/InferenceResult.cs` (engine-agnostic)
  - `InferenceResult` for single attempt results
  - `InferenceResultWithRetries` for aggregated results across attempts
  - `ConstraintViolation` for violation details
  - `ValidationOutcome` enum (Valid, ProhibitionViolated, RequirementNotMet, InvalidFormat)
  - `TokenUsage` for tracking prompt/completion tokens
- [x] Created `LlamaBrain/Source/Core/Inference/RetryPolicy.cs` (engine-agnostic)
  - Configurable max retries (default: 2 = 3 total attempts)
  - `ConstraintEscalation` modes (None, AddSpecificProhibition, HardenRequirements, Full)
  - `GenerateRetryConstraints()` for automatic constraint escalation
  - `GenerateRetryFeedback()` for retry prompt injection
  - Preset policies: Default, NoRetry, Aggressive
- [x] Created `LlamaBrain/Source/Core/Inference/ResponseValidator.cs` (engine-agnostic)
  - Validates responses against ConstraintSet
  - Pattern extraction from constraint descriptions
  - Keyword and regex matching
  - `ValidationResult` with violations and outcome

#### 3.4 Integration (COMPLETE) - Unity Layer
- [x] Modified `LlamaBrainRuntime/Runtime/Core/LlamaBrainAgent.cs`
  - Added `SendWithSnapshotAsync()` method using new inference pipeline
  - Added `LastSnapshot` and `LastInferenceResult` properties for debugging
  - Private `ResponseValidator` and `RetryPolicy` fields
  - `BuildStateSnapshot()` method for snapshot creation
  - `BuildPromptFromSnapshot()` for prompt construction
  - Full retry loop with constraint escalation and time limit

#### 3.5 Testing (COMPLETE) - Base Project
- [x] `LlamaBrain.Tests/Inference/StateSnapshotTests.cs` - Snapshot creation, retry, and memory formatting (15 tests)
- [x] `LlamaBrain.Tests/Inference/InferenceResultTests.cs` - Result creation and aggregation (12 tests)
- [x] `LlamaBrain.Tests/Inference/RetryPolicyTests.cs` - Policy configuration and constraint generation (12 tests)
- [x] `LlamaBrain.Tests/Inference/ResponseValidatorTests.cs` - Validation logic (15 tests)
- [x] `LlamaBrain.Tests/Inference/ContextRetrievalLayerTests.cs` - Retrieval and filtering (15 tests)
- [x] All ~69 tests in `LlamaBrain.Tests/Inference/` - **ALL TEST FILES CREATED**

---

## ✅ Phase 4: Ephemeral Working Memory (COMPLETE)

**Priority**: MEDIUM - Improves prompt quality and token efficiency

**Status**: 100% Complete

**Dependencies**: Phase 3 (State Snapshot)

### Components Built

#### 4.1 Working Memory Component (COMPLETE) - Base Project
- [x] Created `LlamaBrain/Source/Core/Inference/EphemeralWorkingMemory.cs` (engine-agnostic)
  - Short-lived memory for current inference
  - `WorkingMemoryConfig` for configurable bounds (exchanges, memories, beliefs, characters)
  - Preset configurations: Default, Minimal, Expanded
  - Explicit bounding (e.g., last 5 exchanges, 5 memories, 3 beliefs)
  - Character-based truncation with priority to canonical facts and world state
  - `GetFormattedContext()` and `GetFormattedDialogue()` for prompt building
  - `GetStats()` for debugging/metrics
  - `IDisposable` implementation for cleanup after inference
  - Assembled from StateSnapshot, discarded after inference

#### 4.2 Prompt Assembler (COMPLETE) - Base Project
- [x] Created `LlamaBrain/Source/Core/Inference/PromptAssembler.cs` (engine-agnostic)
  - `PromptAssemblerConfig` with token limits and format strings
  - Preset configurations: Default, SmallContext, LargeContext
  - `AssembleFromSnapshot()` creates EphemeralWorkingMemory internally
  - `AssembleFromWorkingMemory()` for pre-built working memory
  - `AssembleMinimal()` for testing/simple use cases
  - Token estimation: `EstimateTokens()`, `EstimateCharacters()`
  - `AssembledPrompt` result with:
    - Full prompt text
    - Character and token counts
    - `WasTruncated` flag
    - `PromptSectionBreakdown` (system, context, constraints, dialogue, etc.)
    - Reference to WorkingMemory for disposal
  - Configurable section formats (system prompt, context header, NPC prompt, etc.)
#### 4.2.1 Unity Wrapper
- [x] Created `LlamaBrainRuntime/Runtime/Core/Inference/PromptAssemblerSettings.cs` (Unity ScriptableObject)
  - Inspector-editable configuration
  - Token limits, working memory limits, content inclusion toggles
  - Format strings for customization
  - `ToConfig()` and `ToWorkingMemoryConfig()` converters

#### 4.3 Integration (COMPLETE) - Unity Layer
- [x] Modified `LlamaBrainRuntime/Runtime/Core/LlamaBrainAgent.cs`
  - Added `promptAssemblerSettings` field (optional ScriptableObject)
  - Added `promptAssembler` private field
  - Added `LastAssembledPrompt` property for debugging/metrics
  - Updated `SendWithSnapshotAsync()` to use PromptAssembler
  - Proper WorkingMemory disposal in finally block after each attempt
  - Retry feedback automatically included when applicable

#### 4.4 Testing (COMPLETE) - Base Project
- [x] `LlamaBrain.Tests/Inference/EphemeralWorkingMemoryTests.cs` - Bounds, truncation, disposal (28 tests)
- [x] `LlamaBrain.Tests/Inference/PromptAssemblerTests.cs` - Assembly, config, estimation (39 tests)
- [x] All ~67 tests in `LlamaBrain.Tests/Inference/` - **ALL TEST FILES CREATED**

---

## ✅ Phase 5: Output Validation System (COMPLETE)

**Priority**: CRITICAL - Core of the architectural pattern

**Status**: 100% Complete

**Dependencies**: Phase 1 (Determinism Layer)

### Components Built

#### 5.1 Output Parser (COMPLETE) - Base Project
- [x] Created `LlamaBrain/Source/Core/Validation/OutputParser.cs`
  - Parses LLM output into structured format
  - Extracts: dialogue text, proposed mutations, world intents
  - Handles malformed outputs gracefully (stage directions, speaker labels, truncation)
  - Returns parsing errors for retry
  - Configurable via `OutputParserConfig` with presets (Default, Structured, Minimal)
- [x] Created `LlamaBrain/Source/Core/Validation/ParsedOutput.cs`
  - `ParsedOutput` - structured result with dialogue, mutations, intents
  - `ProposedMutation` - memory mutation proposals with types (AppendEpisodic, TransformBelief, TransformRelationship, EmitWorldIntent)
  - `WorldIntent` - NPC world-affecting desires with parameters and priority

#### 5.2 Validation Gate (COMPLETE) - Base Project
- [x] Created `LlamaBrain/Source/Core/Validation/ValidationGate.cs`
  - Validates parsed output against constraints from expectancy engine
  - Checks canonical fact contradictions against AuthoritativeMemorySystem
  - Validates knowledge boundaries (forbidden knowledge topics)
  - Validates proposed mutations (blocks canonical fact mutations)
  - Custom rule support via `ValidationRule` base class
  - `PatternValidationRule` for regex-based rules
  - `ValidationContext` for passing memory system and constraints
  - `GateResult` with failures, approved/rejected mutations, critical failure detection
  - Configurable via `ValidationGateConfig` with presets (Default, Minimal)

#### 5.3 Validation Rules (COMPLETE) - Unity Layer
- [x] Created `LlamaBrainRuntime/Runtime/Core/Validation/ValidationRuleAsset.cs`
  - ScriptableObject for designer-created rules
  - Supports Prohibition, Requirement, and Custom rule types
  - Context conditions: scene filter, NPC ID filter, trigger reason filter
  - Pattern matching with regex support and case-insensitivity option
- [x] Created `ValidationRuleSetAsset` for grouping rules
- [x] Created `LlamaBrainRuntime/Runtime/Core/Validation/ValidationPipeline.cs`
  - Unity MonoBehaviour for complete validation pipeline
  - Combines OutputParser and ValidationGate
  - Global rules via ScriptableObject rule sets
  - Forbidden knowledge configuration
  - Events for validation pass/fail

#### 5.4 Integration (COMPLETE) - Unity Layer
- [x] Modified `LlamaBrainRuntime/Runtime/Core/LlamaBrainAgent.cs`
  - Added `outputParser` and `validationGate` fields
  - Added `LastParsedOutput` and `LastGateResult` properties for debugging
  - Updated `SendWithSnapshotAsync()` to use full validation pipeline
  - Parses output with OutputParser, validates with ValidationGate
  - Critical failures skip retry and use fallback
  - Non-critical failures trigger retry with constraint escalation

#### 5.5 Testing (COMPLETE) - Base Project
- [x] `LlamaBrain.Tests/Validation/OutputParserTests.cs` - ~20 tests
  - Basic parsing, single-line enforcement, direction removal
  - Speaker label removal, truncation handling, meta-text detection
  - Structured data extraction, configuration, fragment detection
- [x] `LlamaBrain.Tests/Validation/ValidationGateTests.cs` - ~20 tests
  - Basic validation, constraint validation, canonical fact validation
  - Knowledge boundary validation, mutation validation
  - Custom rules, critical failures, world intents
- [x] `LlamaBrain.Tests/Validation/ParsedOutputTests.cs` - ~20 tests
  - ParsedOutput, ProposedMutation, WorldIntent, ValidationFailure, GateResult

---

## ✅ Core Test Coverage Complete

**Status**: ✅ Complete - All critical test suites implemented

**Summary**: All inference pipeline tests (136 tests) and core integration tests (179 tests) are complete, totaling 315 core tests. See Phase 8.5 for remaining test coverage gaps.

---

## ✅ Phase 6: Controlled Memory Mutation (COMPLETE)

**Priority**: HIGH - Ensures memory integrity

**Status**: 100% Complete

**Dependencies**: Phase 2 (Structured Memory), Phase 5 (Validation), **Inference Tests (above)**

### Components Built

#### 6.1 Mutation Controller (COMPLETE)
- [x] Created `LlamaBrain/Source/Persona/MemoryMutationController.cs`
  - Only validated outputs can trigger mutations
  - Mutation types: AppendEpisodic, TransformBelief, TransformRelationship, EmitWorldIntent
  - Enforces: Cannot override canonical facts (blocked with statistics tracking)
  - Logs all mutation attempts with configurable logging
  - `MutationExecutionResult` for individual mutation results
  - `MutationBatchResult` for aggregated batch results
  - `MutationStatistics` for tracking success/failure rates
  - Event-based world intent delivery via `OnWorldIntentEmitted`

#### 6.2 World Intent System (COMPLETE)
- [x] Created `LlamaBrainRuntime/Runtime/Core/WorldIntentDispatcher.cs`
  - Unity MonoBehaviour for dispatching world intents to game systems
  - Singleton pattern for global access
  - General `OnAnyIntent` Unity event for all intents
  - Intent-specific handlers via `IntentHandlerConfig`
  - Code-based handler registration with `RegisterHandler()`
  - Intent history tracking with configurable size
  - Query methods: `GetIntentsFromNpc()`, `GetIntentsByType()`
  - Automatic hook to `MemoryMutationController` via `HookToController()`

#### 6.3 Integration (COMPLETE)
- [x] Modified `LlamaBrainRuntime/Runtime/Core/LlamaBrainAgent.cs`
  - Added `mutationController` field with automatic initialization
  - Added `LastMutationBatchResult` property for debugging/metrics
  - Added `MutationStats` property for statistics access
  - Added `HookIntentDispatcher()` and `UnhookIntentDispatcher()` methods
  - Mutations automatically executed after successful validation in `SendWithSnapshotAsync()`
- [x] Modified `LlamaBrainRuntime/Runtime/RedRoom/Interaction/NpcDialogueTrigger.cs`
  - Added `GetMutationStats()` method for accessing mutation statistics
  - Added `GetLastMutationBatchResult()` method for debugging
  - Mutations flow through LlamaBrainAgent automatically

#### 6.4 Testing (COMPLETE)
- [x] Created `LlamaBrain.Tests/Memory/MemoryMutationControllerTests.cs` (41 tests, all passing)
  - Constructor and configuration tests
  - Gate result validation tests
  - AppendEpisodic mutation tests (success, empty content, statistics)
  - TransformBelief mutation tests (success, no target, empty content, canonical fact protection)
  - TransformRelationship mutation tests (success, no target, statistics)
  - EmitWorldIntent tests (success, no intent type, parameters, NPC ID)
  - Batch execution tests (multiple mutations, intents, mixed success/failure)
  - ValidateMutation pre-flight check tests
  - Statistics and reset tests
  - Logging tests (enabled/disabled)
  - Exception handling tests
  - Result class tests (MutationExecutionResult, MutationBatchResult, MutationStatistics)

**Completed**: December 2024

---

## ✅ Phase 7: Enhanced Fallback System (COMPLETE)

**Priority**: MEDIUM - Improves reliability

**Status**: 100% Complete

**Dependencies**: Phase 3 (Retry Logic)

### Components Built

#### 7.1 Fallback Hierarchy (COMPLETE)
- [x] Created `LlamaBrainRuntime/Runtime/Core/AuthorControlledFallback.cs`
  - Generic safe responses with configurable list
  - Context-aware fallbacks (based on TriggerReason: PlayerUtterance, ZoneTrigger, TimeTrigger, QuestTrigger, NpcInteraction, WorldEvent, Custom)
  - Emergency fallbacks (always work as last resort)
  - Logs failure reasons for debugging
  - Fallback statistics tracking (total fallbacks, by trigger reason, by failure reason, emergency fallbacks)

#### 7.2 Integration (COMPLETE)
- [x] Modified `LlamaBrainRuntime/Runtime/Core/LlamaBrainAgent.cs`
  - Added `fallbackSystem` field and initialization
  - Integrated with `SendWithSnapshotAsync()` retry system
  - Uses author fallback only after max retry attempts are exhausted
  - Updated `GenerateFallbackResponse()` to use new fallback system
  - Added `BuildFailureReason()` method for detailed failure logging
  - Added `FallbackStats` property for metrics access
- [x] Modified `LlamaBrainRuntime/Runtime/RedRoom/Interaction/NpcDialogueTrigger.cs`
  - Added `GetFallbackStats()` method for accessing fallback statistics
  - Fallback automatically used through LlamaBrainAgent integration

#### 7.3 Testing (COMPLETE)
- [x] Test fallback selection logic
- [x] Test context-aware fallback selection
- [x] Test emergency fallback reliability
- [x] Verify statistics tracking (failure reasons, trigger reasons, emergency fallbacks)
- [x] Created `IFallbackSystem` interface in base project
- [x] Created `FallbackSystem` engine-agnostic implementation
- [x] Created `FallbackSystemTests.cs` with 28 comprehensive tests (all passing)

**Estimated Effort**: Complete (2-3 days estimated, completed)

---

## 📋 Phase 8: RedRoom Integration

**Priority**: MEDIUM - Enables testing of new architecture

**Status**: 60% Complete (Basic infrastructure exists, Metrics enhanced)

**Dependencies**: All previous phases

### Components to Update

#### 8.1 Metrics Enhancement (COMPLETE)
- [x] Updated `LlamaBrainRuntime/Runtime/RedRoom/Interaction/DialogueMetrics.cs`
  - Added `ValidationPassed` and `ValidationFailureCount` for validation pass/fail tracking
  - Added `RetryCount` and `TotalAttempts` for retry metrics per interaction
  - Added `ConstraintViolationTypes` (comma-separated list) for violation type tracking
  - Added `FallbackUsed` and `FallbackTriggerReason` for fallback usage tracking
  - Added `ConstraintCount`, `ProhibitionCount`, `RequirementCount`, `PermissionCount` for determinism layer metrics
  - Added `HasCriticalFailure` for critical failure detection
  - Added `PopulateArchitecturalMetrics()` method to extract data from LlamaBrainAgent

#### 8.2 Collector Enhancement (COMPLETE)
- [x] Updated `LlamaBrainRuntime/Runtime/RedRoom/Interaction/DialogueMetricsCollector.cs`
  - Added overloaded `RecordInteraction()` method that accepts LlamaBrainAgent for architectural data
  - Updated CSV export to include all new validation, retry, fallback, and constraint metrics
  - Updated JSON export to include all new metrics
  - Added `ExportValidationStatistics()` method to export validation-specific statistics to separate CSV
  - Added `ExportConstraintViolations()` method to export constraint violation details to separate CSV
  - Enhanced `GetSessionSummary()` to include architectural pattern metrics (validation pass rate, retry averages, fallback usage rate, critical failures)
  - Automatic export of validation statistics and constraint violations when session is exported
- [x] Updated `NpcDialogueTrigger.cs`
  - Added automatic metrics recording after dialogue generation
  - Passes LlamaBrainAgent to collector for architectural metrics population

#### 8.3 Documentation (COMPLETE)
- [x] Update `LlamaBrainRuntime/Runtime/RedRoom/README.md`
  - Added "Architectural Pattern: Determinism Layer" section with diagram reference
  - Documented 9-component pipeline with component table
  - Added Expectancy Engine (Component 2) section with constraint types/severity
  - Added Validation System (Component 7) section with failure reasons and GateResult API
  - Added custom validation rules examples (PatternValidationRule)
  - Added Retry & Fallback Flow section
  - Added Memory Mutation System (Component 8) section with mutation types and authority hierarchy
  - Added World Intent System section with dispatcher usage and handlers
  - Added Architecture Troubleshooting section for validation, mutation, and intent issues
  - Added debugging tools code examples

#### 8.4 Sample Scenes
- [ ] Create validation test scene
- [ ] Create constraint demonstration scene
- [ ] Create memory mutation test scene

**Estimated Effort**: 4-5 days (includes sample scenes and documentation)

---

## 📋 Phase 8.5: Missing Test Coverage

**Priority**: MEDIUM - Improve test coverage for remaining components

**Status**: 75% Complete (ResponseValidator + MemoryMutation + Utilities + ProcessConfig + DialogueInteraction complete)

**Dependencies**: Phase 1-5, 7 (Implementation complete)

### Current Test Coverage Status
- **Line Coverage**: ~70% (estimated after DialogueInteraction tests)
- **Branch Coverage**: ~55% (estimated)
- **Total Tests**: 853 tests (all passing)
- **Core Integration Tests**: ✅ Complete (179 tests)
- **Inference Pipeline Tests**: ✅ Complete (164 tests - including 28 new ResponseValidator tests)
- **Memory Mutation Tests**: ✅ Complete (41 tests)
- **Utility Tests**: ✅ Complete (162 tests - JsonUtils + PathUtils + ProcessUtils)
- **ProcessConfig Tests**: ✅ Complete (6 tests - 100% line coverage)
- **DialogueInteraction Tests**: ✅ Complete (35 tests - ~85% coverage)

### Completed Test Suites

#### Inference Pipeline Tests (164 tests) - ✅ COMPLETE
- [x] `StateSnapshotTests.cs` (15 tests)
- [x] `InferenceResultTests.cs` (12 tests)
- [x] `RetryPolicyTests.cs` (12 tests)
- [x] `ResponseValidatorTests.cs` (50 tests)
- [x] `ContextRetrievalLayerTests.cs` (15 tests)
- [x] `EphemeralWorkingMemoryTests.cs` (28 tests)
- [x] `PromptAssemblerTests.cs` (39 tests)

#### Core Integration Tests (179 tests) - ✅ COMPLETE
- [x] `BrainAgentTests.cs` (42 tests)
- [x] `ClientManagerTests.cs` (20 tests)
- [x] `ServerManagerTests.cs` (34 tests)
- [x] `DialogueSessionTests.cs` (39 tests)
- [x] `DialogueEntryTests.cs` (15 tests)
- [x] `CompletionMetricsTests.cs` (29 tests)

#### Memory Mutation Tests (41 tests) - ✅ COMPLETE
- [x] `MemoryMutationControllerTests.cs` (41 tests)
  - Mutation execution (episodic, belief, relationship, world intent)
  - Authority checking and canonical fact protection
  - Statistics tracking
  - Error handling and edge cases

#### Utility Tests (162 tests) - ✅ COMPLETE
- [x] `JsonUtilsTests.cs` (56 tests)
  - Serialization/deserialization with all options
  - Validation, sanitization, compression, pretty-printing
  - Statistics and schema validation
- [x] `PathUtilsTests.cs` (49 tests)
  - Path combination and validation
  - Security checks (traversal, invalid chars)
  - Safe filename generation
- [x] `ProcessUtilsTests.cs` (57 tests)
  - Process running detection and enumeration
  - Process info retrieval with memory stats
  - Process configuration validation
  - Async wait operations (start/exit)
  - Input validation and edge cases

### Remaining Test Coverage Gaps

#### 8.5.1 PromptComposer Tests
**Current Coverage**: ~20%
**Priority**: MEDIUM
- [ ] Complex prompt composition scenarios
- [ ] Template variable substitution edge cases
- [ ] Error handling for malformed templates
- [ ] Multi-line prompt handling
- [ ] Special character escaping
- **Estimated**: 15-20 additional tests

#### 8.5.2 ResponseValidator Tests (COMPLETE)
**Current Coverage**: ~85%
**Priority**: MEDIUM
- [x] Additional constraint validation scenarios
- [x] Edge cases in pattern matching (word boundary regex, special chars, unicode, long patterns)
- [x] Complex validation rule combinations (multiple requirements, overlapping patterns)
- [x] Performance with large constraint sets (100KB+ responses, many constraints)
- [x] Validation error message accuracy
- [x] Pattern extraction edge cases (quotes, keywords after about/mention/reveal)
- [x] Null/empty constraint handling
- **Added**: 28 new tests (15 → 50 total in ResponseValidatorTests)

#### 8.5.3 ProcessConfig Tests (COMPLETE)
**Current Coverage**: 100% line coverage (13 lines, all covered)
**Priority**: COMPLETE
- [x] Default value validation (6 tests)
- [x] Custom values handling
- [x] Null value handling
- [x] LlmConfig integration
- [x] Edge case values (empty strings, zero, max values)
- [x] Large value handling
- **Status**: ✅ Complete - All 13 lines covered with 6 comprehensive tests

#### 8.5.4 PersonaProfileManager Tests
**Current Coverage**: 0% (195 lines, 0 covered)
**Priority**: MEDIUM-HIGH
- [ ] Profile CRUD operations
- [ ] Profile persistence (file I/O - requires mocking)
- [ ] Profile validation
- [ ] Profile loading/saving
- **Estimated**: 10-15 tests
- **Coverage Impact**: +3.6% overall coverage if fully covered

#### 8.5.5 Utility Tests (COMPLETE)
**Current Coverage**: ~85% (All utility tests complete)
**Priority**: COMPLETE
- [x] `JsonUtils` - JSON serialization edge cases (56 tests)
  - Serialize/Deserialize with formatting
  - IsValidJson, SafeDeserialize
  - TruncateJson, ValidateJsonSchema
  - SanitizeJson, CompressJson, PrettyPrintJson
  - GetJsonStatistics
- [x] `PathUtils` - Path manipulation utilities (49 tests)
  - CombinePath, ValidateAndSanitizePath
  - ContainsPathTraversal, ContainsInvalidCharacters
  - IsPathWithinDirectory, GetDirectoryDepth
  - CreateSafeFilename, IsPathSafe, GetRelativePath
- [x] `ProcessUtils` - Process validation utilities (57 tests)
  - IsProcessRunning, GetRunningProcesses, KillProcess
  - GetProcessInfo, ProcessInfo class
  - WaitForProcessStartAsync, WaitForProcessExitAsync
  - ValidateProcessConfig, input validation edge cases
- **Added**: 162 new tests total

#### 8.5.6 Additional Zero-Coverage Files
**Current Coverage**: Partial (DialogueInteraction complete)
**Priority**: MEDIUM-HIGH
- [x] **DialogueInteraction.cs** (0% → ~85% - 132 lines) ✅ **COMPLETE**
  - 35 tests covering all functionality
  - Property initialization, `FromMetrics()`, `PopulateArchitecturalMetrics()`
  - DialogueMetricsCollection tests (add, end session, properties)
  - Edge cases and serialization attribute verification
  - **Coverage Impact**: +2.4% overall coverage
- [ ] **PersonaMemoryFileStore.cs** (0% - 154 lines)
  - File I/O operations (requires mocking)
  - Memory persistence and loading
  - **Coverage Impact**: +2.8% overall coverage
  - **Estimated**: 10-15 tests

#### 8.5.7 Integration & System Testing
**Current Coverage**: Not Started
**Priority**: MEDIUM
- [ ] End-to-end tests with full architectural pattern
  - Full inference pipeline from input to validated output
  - Memory mutation flow integration
  - Retry and fallback system integration
- [ ] Performance benchmarks with new layers
  - Measure overhead from validation/retry layers
  - Memory system performance under load
  - Prompt assembly performance
- [ ] Validate metrics accuracy
  - Verify metrics collection correctness
  - Test metrics export functionality
  - Validate statistical calculations
- [ ] Test all failure modes
  - Network failures
  - Server unavailability
  - Invalid responses
  - Timeout scenarios
  - Memory system failures
- **Estimated**: 20-30 integration/system tests

### Coverage Goals
- **Target Line Coverage**: 70%+ (currently 68.36% - very close!)
- **Target Branch Coverage**: 60%+ (currently ~55%)
- **Focus Areas**: 
  - **Easy Wins**: DialogueInteraction (+2.4%), PersonaMemoryFileStore (+2.8%) = +5.2% total
  - **Medium Priority**: PromptComposer (20% → 80%+), PersonaProfileManager (0% → 80%+)
  - Integration testing for full pipeline validation

**Estimated Effort**: 1-2 days for easy wins (DialogueInteraction, PersonaMemoryFileStore), 2-3 days for PromptComposer/PersonaProfileManager, 2-3 days for integration/system testing

---

## 📋 Phase 9: Documentation & Polish

**Priority**: MEDIUM - Enables adoption and understanding

**Status**: 30% Complete (Basic docs exist, needs expansion)

**Dependencies**: All previous phases

### Documentation to Create/Update

#### 9.1 Architecture Documentation
- [ ] Create `LlamaBrain/ARCHITECTURE.md`
  - Full explanation of architectural pattern
  - Component interaction diagrams
  - Code examples for each layer
  - Best practices guide

#### 9.2 Main README Updates
- [ ] Update `README.md`
  - Move architectural features from "Planned" to "Current"
  - Update completion percentage
  - Add architecture pattern overview
  - Update use cases with new capabilities

#### 9.3 Unity Package Documentation
- [ ] Update `LlamaBrainRuntime/Assets/LlamaBrainRuntime/LLAMABRAIN.md`
  - Setup guide for determinism layer
  - Validation rule creation tutorial
  - Memory system migration guide
  - Troubleshooting for new components

#### 9.4 API Documentation
- [ ] Generate XML documentation comments for all new components
- [ ] Create API reference documentation
- [ ] Add code examples to documentation

#### 9.5 Tutorial Content
- [ ] Write tutorial: "Setting Up Deterministic NPCs"
- [ ] Write tutorial: "Creating Custom Validation Rules"
- [ ] Write tutorial: "Understanding Memory Authority"
- [ ] Write tutorial: "Debugging Validation Failures"

**Estimated Effort**: 5-6 days

---

## 📊 Success Metrics

### Technical Metrics
- [ ] Validation pass rate > 95%
- [ ] Fallback usage rate < 5%
- [ ] Average retries per interaction < 1.5
- [ ] Canonical fact violation attempts = 0
- [ ] Memory mutation authorization accuracy = 100%

### Testing Metrics (via RedRoom)
- [ ] 1000+ test interactions across 10+ scenarios
- [ ] Comprehensive metrics export for analysis
- [ ] Performance benchmarks showing <10% overhead from new layers
- [ ] Determinism verification: Same input + context = Same output

### Documentation Metrics
- [ ] 100% of public APIs documented
- [ ] 5+ complete tutorials
- [ ] Architecture diagram matches implementation
- [ ] Zero unanswered questions in documentation review

---

## 🎯 Milestones

### Milestone 1: Core Architecture (Phases 1-3)
**Target**: Weeks 1-3
**Status**: ✅ Complete
- [x] Determinism layer functional
- [x] Structured memory system working
- [x] State snapshot and retry logic operational

### Milestone 2: Validation & Control (Phases 4-6)
**Target**: Weeks 4-6
**Status**: ✅ Complete
- [x] Output validation gate functional
- [x] Working memory system operational
- [x] Memory mutation control enforced (Phase 6 complete)

### Milestone 3: Integration & Polish (Phases 7-9)
**Target**: Weeks 7-8
**Status**: 🚧 Partial (Implementation: 50%, Tests: 0%)
- [x] Fallback system complete (implementation complete, tests pending)
- [ ] RedRoom fully integrated (60% complete)
- [ ] Documentation comprehensive (30% complete)

### Milestone 4: Production Ready
**Target**: Week 9+
**Status**: ❌ Not Started
- [ ] All tests passing (many test files missing)
- [ ] Performance benchmarks met
- [ ] Documentation complete
- [ ] Ready for external use

---

## 🔄 Iteration Strategy

Each phase follows this pattern:
1. **Build** - Implement core components
2. **Test** - Unit and integration tests
3. **Integrate** - Connect to existing system
4. **Validate** - RedRoom testing with metrics
5. **Document** - Update relevant documentation
6. **Review** - Code review and architecture validation

---

## 📝 Notes

### Design Decisions Made
- [x] Validation rule syntax/format - ScriptableObject-based rules with conditions
- [x] Memory decay algorithm for episodic memory - Significance-weighted decay with configurable rate
- [x] World intent schema - `WorldIntent` with IntentType, Target, Parameters, Priority, SourceText
- [x] Constraint escalation strategy for retries - Configurable via `ConstraintEscalation` modes (None, AddSpecificProhibition, HardenRequirements, Full)

### Risks & Mitigations
- **Risk**: Performance overhead from new layers
  - **Mitigation**: Benchmark after each phase, optimize hot paths
- **Risk**: Backward compatibility breaking
  - **Mitigation**: Maintain compatibility layer for existing PersonaMemoryStore
- **Risk**: Complexity overwhelming users
  - **Mitigation**: Provide sensible defaults, comprehensive tutorials

### Future Considerations (Beyond This Roadmap)
- Multi-agent conversations with shared memory
- Voice integration with validation
- Visual novel integration
- Multiplayer shared world state
- Advanced analytics dashboard for metrics

---

**Last Updated**: December 30, 2025

**Next Review**: After Phase 8 completion (Phase 6 now complete)

**Recent Updates**:
- ✅ **Phase 6: Controlled Memory Mutation COMPLETE** (December 2024)
  - Created `MemoryMutationController.cs` - engine-agnostic mutation execution with statistics tracking
  - Created `WorldIntentDispatcher.cs` - Unity component for dispatching world intents to game systems
  - Integrated mutation execution in `LlamaBrainAgent.cs` after successful validation
  - Added mutation statistics and batch result access in `NpcDialogueTrigger.cs`
  - Created 41 comprehensive tests in `MemoryMutationControllerTests.cs` (all passing)
- ✅ BrainAgentTests.cs complete (42 tests, all passing)
- ✅ Fixed memory retrieval bug: GetMemories() now returns raw content without formatting prefixes
- ✅ ClientManagerTests.cs complete (20 tests, all passing) - covers client creation, connection management, error handling
- ✅ CompletionMetricsTests.cs complete (29 tests, all passing) - covers metrics collection, property access, TokensPerSecond calculation, edge cases
- ✅ DialogueSessionTests.cs complete (39 tests, all passing) - covers session creation, entry management, history tracking, memory integration
- ✅ DialogueEntryTests.cs complete (15 tests, all passing) - covers entry creation, property access, timestamp handling, edge cases
- ✅ ServerManagerTests.cs complete (34 tests, all passing) - covers server lifecycle management, validation, status checks, error handling, events
- ✅ Test coverage report generated: 61.2% line coverage, 49.1% branch coverage (592 tests passing)
- ✅ Phase 8.5 created to track remaining test coverage gaps (PromptComposer, ResponseValidator, ProcessConfig, PersonaProfileManager)

