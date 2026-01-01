# LlamaBrain Implementation Roadmap

**Goal**: Implement the complete "Continuity Emerges from Deterministic State Reconstruction Around a Stateless Generator" architectural pattern.

**Last Updated**: December 31, 2025

---

## Quick Links

- **Status**: See `STATUS.md` for current phase completion and next tasks
- **Determinism Contract**: See `DETERMINISM_CONTRACT.md` for explicit boundary statement and contract decisions
- **Phase 10 Test Gaps**: See `PHASE10_PROOF_GAPS.md` for detailed test backlog

---

## Progress Overview

| Phase | Status | Priority |
|-------|--------|----------|
| Phase 1: Determinism Layer | ✅ Complete | CRITICAL |
| Phase 2: Structured Memory System | ✅ Complete | HIGH |
| Phase 3: State Snapshot & Context Retrieval | ✅ Complete | HIGH |
| Phase 4: Ephemeral Working Memory | ✅ Complete | MEDIUM |
| Phase 5: Output Validation System | ✅ Complete | CRITICAL |
| Phase 6: Controlled Memory Mutation | ✅ Complete | HIGH |
| Phase 7: Enhanced Fallback System | ✅ Complete | MEDIUM |
| Phase 8: RedRoom Integration | 🚧 In Progress | MEDIUM |
| Phase 9: Documentation | 🚧 In Progress | MEDIUM |
| Phase 10: Deterministic Proof Gap Testing | 📋 Planned | HIGH |
| Phase 11: RAG-Based Memory Retrieval | 📋 Planned | MEDIUM |
| Phase 12: Dedicated Structured Output | 📋 Planned | HIGH |
| Phase 13: Structured Output Integration | 📋 Planned | HIGH |

---

## ✅ Phase 1: Determinism Layer & Expectancy Engine

**Priority**: CRITICAL - Foundation for all downstream work  
**Status**: ✅ Complete  
**Dependencies**: None

### Definition of Done

#### 1.1 Expectancy Engine
- [x] Created `LlamaBrain/Source/Core/Expectancy/ExpectancyEvaluator.cs` (engine-agnostic)
- [x] Created `LlamaBrainRuntime/Runtime/Core/Expectancy/ExpectancyEngine.cs` (Unity wrapper)
- [x] Generates constraints based on interaction context
- [x] Outputs ConstraintSet for prompt construction and validation

#### 1.2 Rule System
- [x] Created `ExpectancyRuleAsset.cs` (ScriptableObject)
- [x] Rule types: Permission, Prohibition, Requirement
- [x] Severity levels: Soft, Hard, Critical
- [x] Condition system with combine modes (All/Any)
- [x] Created `NpcExpectancyConfig.cs` for NPC-specific rules

#### 1.3 Integration
- [x] Modified `LlamaBrainAgent.cs` with ExpectancyConfig field
- [x] Evaluates rules before prompt composition
- [x] Injects constraints via `ConstraintSet.ToPromptInjection()`
- [x] Added `SendPlayerInputWithContextAsync()` for context-aware triggers

#### 1.4 Testing
- [x] Unit tests for ExpectancyEvaluator (16 tests)
- [x] Unit tests for Constraint and ConstraintSet (29 tests)
- [x] Unit tests for InteractionContext (10 tests)
- [x] All 50 tests passing in `LlamaBrain.Tests/Expectancy/`

**Estimated Effort**: Complete (2-3 weeks estimated, completed)

---

## ✅ Phase 2: Structured Memory System

**Priority**: HIGH - Required for proper state management  
**Status**: ✅ Complete  
**Dependencies**: None

### Definition of Done

#### 2.1 Memory Type Hierarchy
- [x] Created `MemoryTypes/` folder structure
- [x] `MemoryAuthority.cs` - Authority levels (Canonical > WorldState > Episodic > Belief)
- [x] `MemoryEntry.cs` - Base class with MutationResult
- [x] `CanonicalFact.cs` - Immutable world truths
- [x] `WorldState.cs` - Mutable game state
- [x] `EpisodicMemory.cs` - Conversation history with decay
- [x] `BeliefMemory.cs` - NPC opinions/relationships

#### 2.2 Authoritative Memory System
- [x] Created `AuthoritativeMemorySystem.cs`
- [x] Manages all memory types with authority boundaries
- [x] Enforces: Canonical facts cannot be overridden or modified
- [x] Validates mutation source authority before allowing changes
- [x] Detects belief contradictions against canonical facts
- [x] Supports episodic memory decay and pruning
- [x] Provides unified `GetAllMemoriesForPrompt()` for prompt injection

#### 2.3 Migration
- [x] Refactored `PersonaMemoryStore.cs` to use AuthoritativeMemorySystem
- [x] `UseAuthoritativeSystem` flag for backward compatibility
- [x] New structured API: `AddCanonicalFact`, `SetWorldState`, `AddDialogue`, `SetBelief`, `SetRelationship`
- [x] `ApplyDecay()` and `ApplyDecayAll()` for memory decay
- [x] Integrated memory decay in `LlamaBrainAgent` with automatic periodic decay
- [x] Integrated canonical facts initialization

#### 2.4 Testing
- [x] `MemoryTypesTests.cs` (~25 tests)
- [x] `AuthoritativeMemorySystemTests.cs` (~20 tests)
- [x] `PersonaMemoryStoreTests.cs` (~20 tests)
- [x] All ~65 tests in `LlamaBrain.Tests/Memory/`

**Estimated Effort**: Complete (2-3 weeks estimated, completed)

---

## ✅ Phase 3: State Snapshot & Context Retrieval

**Priority**: HIGH - Enables retry logic and bounded context  
**Status**: ✅ Complete  
**Dependencies**: Phase 2 (Structured Memory)

### Definition of Done

#### 3.1 State Snapshot System
- [x] Created `StateSnapshot.cs` (engine-agnostic)
- [x] Immutable snapshot of all context at inference time
- [x] `ForRetry()` method for creating retry snapshots with merged constraints
- [x] `GetAllMemoryForPrompt()` for formatted memory injection
- [x] `StateSnapshotBuilder` with fluent API
- [x] Created `UnityStateSnapshotBuilder.cs` (Unity wrapper)

#### 3.2 Context Retrieval Layer
- [x] Created `ContextRetrievalLayer.cs` (engine-agnostic)
- [x] Retrieves relevant context from AuthoritativeMemorySystem
- [x] `ContextRetrievalConfig` for configurable limits and weighting
- [x] Recency/relevance/significance scoring for episodic memories
- [x] Confidence-based filtering for beliefs
- [x] Topic-based filtering for all memory types
- [x] `RetrievedContext.ApplyTo()` for easy snapshot building

#### 3.3 Inference Result & Retry Policy
- [x] Created `InferenceResult.cs` (engine-agnostic)
- [x] `InferenceResult` for single attempt results
- [x] `InferenceResultWithRetries` for aggregated results
- [x] `ConstraintViolation` for violation details
- [x] `ValidationOutcome` enum
- [x] Created `RetryPolicy.cs` with configurable max retries and constraint escalation
- [x] Created `ResponseValidator.cs` for validating responses against ConstraintSet

#### 3.4 Integration
- [x] Modified `LlamaBrainAgent.cs` with `SendWithSnapshotAsync()` method
- [x] Added `LastSnapshot` and `LastInferenceResult` properties
- [x] Full retry loop with constraint escalation and time limit

#### 3.5 Testing
- [x] `StateSnapshotTests.cs` (15 tests)
- [x] `InferenceResultTests.cs` (12 tests)
- [x] `RetryPolicyTests.cs` (12 tests)
- [x] `ResponseValidatorTests.cs` (15 tests)
- [x] `ContextRetrievalLayerTests.cs` (15 tests)
- [x] All ~69 tests in `LlamaBrain.Tests/Inference/`

**Estimated Effort**: Complete (2-3 weeks estimated, completed)

---

## ✅ Phase 4: Ephemeral Working Memory

**Priority**: MEDIUM - Improves prompt quality and token efficiency  
**Status**: ✅ Complete  
**Dependencies**: Phase 3 (State Snapshot)

### Definition of Done

#### 4.1 Working Memory Component
- [x] Created `EphemeralWorkingMemory.cs` (engine-agnostic)
- [x] Short-lived memory for current inference
- [x] `WorkingMemoryConfig` for configurable bounds
- [x] Preset configurations: Default, Minimal, Expanded
- [x] Explicit bounding (exchanges, memories, beliefs, characters)
- [x] Character-based truncation with priority to canonical facts and world state
- [x] `IDisposable` implementation for cleanup after inference

#### 4.2 Prompt Assembler
- [x] Created `PromptAssembler.cs` (engine-agnostic)
- [x] `PromptAssemblerConfig` with token limits and format strings
- [x] Preset configurations: Default, SmallContext, LargeContext
- [x] `AssembleFromSnapshot()` creates EphemeralWorkingMemory internally
- [x] Token estimation: `EstimateTokens()`, `EstimateCharacters()`
- [x] `AssembledPrompt` result with full prompt text, counts, truncation flag, and section breakdown
- [x] Created `PromptAssemblerSettings.cs` (Unity ScriptableObject)

#### 4.3 Integration
- [x] Modified `LlamaBrainAgent.cs` with `promptAssemblerSettings` field
- [x] Updated `SendWithSnapshotAsync()` to use PromptAssembler
- [x] Proper WorkingMemory disposal in finally block after each attempt

#### 4.4 Testing
- [x] `EphemeralWorkingMemoryTests.cs` (28 tests)
- [x] `PromptAssemblerTests.cs` (39 tests)
- [x] All ~67 tests in `LlamaBrain.Tests/Inference/`

**Estimated Effort**: Complete (1-2 weeks estimated, completed)

---

## ✅ Phase 5: Output Validation System

**Priority**: CRITICAL - Core of the architectural pattern  
**Status**: ✅ Complete  
**Dependencies**: Phase 1 (Determinism Layer)

### Definition of Done

#### 5.1 Output Parser
- [x] Created `OutputParser.cs` (engine-agnostic)
- [x] Parses LLM output into structured format
- [x] Extracts: dialogue text, proposed mutations, world intents
- [x] Handles malformed outputs gracefully
- [x] Returns parsing errors for retry
- [x] Configurable via `OutputParserConfig` with presets

#### 5.2 Validation Gate
- [x] Created `ValidationGate.cs` (engine-agnostic)
- [x] Validates parsed output against constraints from expectancy engine
- [x] Checks canonical fact contradictions
- [x] Validates knowledge boundaries
- [x] Validates proposed mutations (blocks canonical fact mutations)
- [x] Custom rule support via `ValidationRule` base class
- [x] `PatternValidationRule` for regex-based rules
- [x] Configurable via `ValidationGateConfig` with presets

#### 5.3 Validation Rules
- [x] Created `ValidationRuleAsset.cs` (Unity ScriptableObject)
- [x] Supports Prohibition, Requirement, and Custom rule types
- [x] Context conditions: scene filter, NPC ID filter, trigger reason filter
- [x] Pattern matching with regex support
- [x] Created `ValidationRuleSetAsset` for grouping rules
- [x] Created `ValidationPipeline.cs` (Unity MonoBehaviour)

#### 5.4 Integration
- [x] Modified `LlamaBrainAgent.cs` with outputParser and validationGate fields
- [x] Updated `SendWithSnapshotAsync()` to use full validation pipeline
- [x] Critical failures skip retry and use fallback
- [x] Non-critical failures trigger retry with constraint escalation

#### 5.5 Testing
- [x] `OutputParserTests.cs` (~20 tests)
- [x] `ValidationGateTests.cs` (~20 tests)
- [x] `ParsedOutputTests.cs` (~20 tests)
- [x] All ~60 tests in `LlamaBrain.Tests/Validation/`

**Estimated Effort**: Complete (2-3 weeks estimated, completed)

---

## ✅ Phase 6: Controlled Memory Mutation

**Priority**: HIGH - Ensures memory integrity  
**Status**: ✅ Complete  
**Dependencies**: Phase 2 (Structured Memory), Phase 5 (Validation)

### Definition of Done

#### 6.1 Mutation Controller
- [x] Created `MemoryMutationController.cs` (engine-agnostic)
- [x] Only validated outputs can trigger mutations
- [x] Mutation types: AppendEpisodic, TransformBelief, TransformRelationship, EmitWorldIntent
- [x] Enforces: Cannot override canonical facts (blocked with statistics tracking)
- [x] Logs all mutation attempts with configurable logging
- [x] `MutationExecutionResult` for individual mutation results
- [x] `MutationBatchResult` for aggregated batch results
- [x] `MutationStatistics` for tracking success/failure rates
- [x] Event-based world intent delivery via `OnWorldIntentEmitted`

#### 6.2 World Intent System
- [x] Created `WorldIntentDispatcher.cs` (Unity MonoBehaviour)
- [x] Singleton pattern for global access
- [x] General `OnAnyIntent` Unity event for all intents
- [x] Intent-specific handlers via `IntentHandlerConfig`
- [x] Code-based handler registration with `RegisterHandler()`
- [x] Intent history tracking with configurable size
- [x] Query methods: `GetIntentsFromNpc()`, `GetIntentsByType()`
- [x] Automatic hook to `MemoryMutationController` via `HookToController()`

#### 6.3 Integration
- [x] Modified `LlamaBrainAgent.cs` with `mutationController` field
- [x] Mutations automatically executed after successful validation
- [x] Modified `NpcDialogueTrigger.cs` with mutation statistics access methods

#### 6.4 Testing
- [x] `MemoryMutationControllerTests.cs` (41 tests, all passing)
- [x] Tests cover all mutation types, authority checking, statistics, and error handling

**Estimated Effort**: Complete (1-2 weeks estimated, completed)

---

## ✅ Phase 7: Enhanced Fallback System

**Priority**: MEDIUM - Improves reliability  
**Status**: ✅ Complete  
**Dependencies**: Phase 3 (Retry Logic)

### Definition of Done

#### 7.1 Fallback Hierarchy
- [x] Created `AuthorControlledFallback.cs` (Unity component)
- [x] Generic safe responses with configurable list
- [x] Context-aware fallbacks (based on TriggerReason)
- [x] Emergency fallbacks (always work as last resort)
- [x] Logs failure reasons for debugging
- [x] Fallback statistics tracking

#### 7.2 Integration
- [x] Modified `LlamaBrainAgent.cs` with `fallbackSystem` field
- [x] Integrated with `SendWithSnapshotAsync()` retry system
- [x] Uses author fallback only after max retry attempts are exhausted
- [x] Updated `GenerateFallbackResponse()` to use new fallback system
- [x] Modified `NpcDialogueTrigger.cs` with fallback statistics access

#### 7.3 Testing
- [x] Created `IFallbackSystem` interface in base project
- [x] Created `FallbackSystem` engine-agnostic implementation
- [x] Created `FallbackSystemTests.cs` with 28 comprehensive tests (all passing)

**Estimated Effort**: Complete (2-3 days estimated, completed)

---

## 🚧 Phase 8: RedRoom Integration

**Priority**: MEDIUM - Enables testing of new architecture  
**Status**: 🚧 In Progress (70% Complete)  
**Dependencies**: All previous phases

### Definition of Done

#### 8.1 Metrics Enhancement
- [x] Updated `DialogueMetrics.cs` with validation pass/fail tracking
- [x] Added retry metrics per interaction
- [x] Added constraint violation tracking
- [x] Added fallback usage tracking
- [x] Added architectural pattern metrics
- [x] Added `PopulateArchitecturalMetrics()` method

#### 8.2 Collector Enhancement
- [x] Updated `DialogueMetricsCollector.cs` with overloaded `RecordInteraction()` method
- [x] Updated CSV export to include all new metrics
- [x] Updated JSON export to include all new metrics
- [x] Added `ExportValidationStatistics()` method
- [x] Added `ExportConstraintViolations()` method
- [x] Enhanced `GetSessionSummary()` with architectural pattern metrics
- [x] Updated `NpcDialogueTrigger.cs` with automatic metrics recording

#### 8.3 Documentation
- [x] Updated `RedRoom/README.md` with architectural pattern section
- [x] Documented 9-component pipeline with component table
- [x] Added Expectancy Engine section
- [x] Added Validation System section
- [x] Added Retry & Fallback Flow section
- [x] Added Memory Mutation System section
- [x] Added World Intent System section
- [x] Added Architecture Troubleshooting section
- [x] Added debugging tools code examples

#### 8.4 Testing Overlays (Red Room UI Components)
**Note**: These overlays can be toggled on/off in the Red Room scene, eliminating the need for separate test scenes. Memory overlay is highest priority.

- [x] **Memory Mutation Overlay** (high priority - can definitely be overlay) - **IMPLEMENTED**
  - [x] Real-time memory state viewer panel (`MemoryMutationOverlay.cs`)
    - [x] Display canonical facts (read-only, highlighted/protected indicator)
    - [x] Display world state (mutable, with change indicators)
    - [x] Display episodic memories (with significance scores, decay status, recency)
    - [x] Display beliefs (with confidence scores, relationship status)
  - [x] Mutation execution tracker
    - [x] Show approved mutations (green) vs rejected mutations (red)
    - [x] Display mutation type, target, and result
    - [x] Show authority hierarchy violations (canonical fact protection attempts)
    - [x] Mutation statistics (success rate, blocked attempts)
  - [ ] Memory change history (future enhancement)
    - [ ] Before/after snapshots for each interaction
    - [ ] Diff view showing what changed (added/removed/modified)
  - [x] Integration with RedRoomCanvas
    - [x] Toggle via hotkey (F2)
    - [x] Auto-refresh on interaction events (0.5s interval)
    - [x] Panel positioning (right side panel)
  - [x] Auto-setup helper (`MemoryMutationOverlaySetup.cs`)
    - [x] Programmatic UI generation at runtime
    - [x] Configurable styling and layout

- [ ] **Validation Gate Overlay** (medium priority)
  - [ ] Real-time validation results display
    - [ ] Gate pass/fail status with visual indicator (green/red)
    - [ ] Failure reasons list (expandable, grouped by severity)
    - [ ] Violating text snippets highlighted
  - [ ] Constraint evaluation status
    - [ ] Active constraints (permissions, prohibitions, requirements)
    - [ ] Which constraints were checked and their results
    - [ ] Constraint severity indicators
  - [ ] Retry attempt visualization
    - [ ] Current attempt number / max attempts
    - [ ] Constraint escalation status (none, add prohibition, harden, full)
    - [ ] Retry history (previous attempts and their failures)
  - [ ] Integration with RedRoomCanvas
    - [ ] Toggle via hotkey (e.g., F3) or UI button
    - [ ] Auto-refresh on validation events

- [ ] **Constraint Demonstration Overlay** (lower priority - can be combined with Validation overlay)
  - [ ] Active constraints list
    - [ ] Grouped by type (Permission, Prohibition, Requirement)
    - [ ] Severity indicators (Critical, Hard, Soft)
    - [ ] Rule source (which ExpectancyRule triggered it)
  - [ ] Rule evaluation results
    - [ ] Which rules matched the current InteractionContext
    - [ ] Why rules matched (trigger reason, NPC ID, scene, tags)
  - [ ] Integration with RedRoomCanvas
    - [ ] Toggle via hotkey (e.g., F4) or UI button
    - [ ] Can be combined into Validation overlay as tabs/sections

- [x] **Overlay System Infrastructure** (partial)
  - [x] Extend `RedRoomCanvas` with input managed overlay panels (F2 toggle implemented)
  - [x] Auto-refresh event system (polling-based, 0.5s interval)
  - [ ] Overlay persistence (remember which overlays are open between sessions)

#### 8.5 Integration Testing
- [x] **FullPipelineIntegrationTests** (base library) - Complete
  - [x] 8 tests in `LlamaBrain.Tests/Integration/FullPipelineIntegrationTests.cs`
  - [x] Tests full 9-component pipeline from InteractionContext through memory mutation
  - [x] Validates end-to-end flow, retry logic, constraint escalation, and fallback system
- [ ] **Unity PlayMode Integration Tests** - Pending
  - [ ] Full stack integration tests with real server
  - [ ] Location: `LlamaBrainRuntime/Tests/PlayMode/`
  - [ ] Estimated: 20-30 integration/system tests

**Estimated Effort**: 3-4 days remaining (remaining overlays, sample scenes, and Unity PlayMode tests)

---

## 🚧 Phase 9: Documentation & Polish

**Priority**: MEDIUM - Enables adoption and understanding
**Status**: 🚧 In Progress (90% Complete)
**Dependencies**: All previous phases

### Definition of Done

#### 9.1 Architecture Documentation
- [x] Created `ARCHITECTURE.md`
- [x] Full explanation of architectural pattern (all 9 components)
- [x] Component interaction diagrams (references architectural_diagram.png)
- [x] Code examples for each layer
- [x] Best practices guide

#### 9.2 Main README Updates
- [x] Updated completion percentage
- [x] Added architecture pattern overview
- [x] Updated use cases with new capabilities

#### 9.3 Unity Package Documentation
- [x] Setup guide for determinism layer (in `USAGE_GUIDE.md`)
- [x] Validation rule creation tutorial (in `USAGE_GUIDE.md`)
- [x] Memory system migration guide (comprehensive guide in `USAGE_GUIDE.md`)
- [x] Troubleshooting for new components (in `TROUBLESHOOTING.md` and `RedRoom/README.md`)

#### 9.4 API Documentation
- [x] XML documentation comments for all public APIs (100% coverage)
- [x] Doxygen output generated (zero missing member warnings)
- [x] Code examples in documentation

#### 9.5 Tutorial Content
- [ ] "Setting Up Deterministic NPCs"
- [ ] "Creating Custom Validation Rules"
- [ ] "Understanding Memory Authority"
- [ ] "Debugging Validation Failures"

#### 9.6 Few-Shot Prompt Priming
- [x] **Integration with Prompt Assembly**
  - [x] Add `FewShotExample` class to `EphemeralWorkingMemory.cs` for input-output demonstration pairs
  - [x] Add few-shot configuration to `WorkingMemoryConfig` (FewShotExamples, MaxFewShotExamples, AlwaysIncludeFewShot)
  - [x] Extend `PromptAssembler` to inject few-shot examples section into prompt
  - [x] Add `GetFormattedFewShotExamples()` to `EphemeralWorkingMemory` for formatted output
  - [x] Add `FallbackToFewShotConverter` utility to convert fallback responses to few-shot examples
  - [x] Support converting fallback configs by trigger reason to few-shot examples
  - [x] Ensure deterministic ordering of examples (uses list order, byte-stable)
  - [x] Add `FewShotHeader` and `IncludeFewShotExamples` config to `PromptAssemblerConfig`
  - [x] Track few-shot count in `WorkingMemoryStats`
- [x] **Testing**
  - [x] Unit tests for `FewShotExample` class (4 tests)
  - [x] Unit tests for `WorkingMemoryConfig` few-shot settings (1 test)
  - [x] Unit tests for `EphemeralWorkingMemory` few-shot handling (8 tests)
  - [x] Unit tests for `FallbackToFewShotConverter` (17 tests)
  - [x] All 30 few-shot tests passing
- [ ] **Documentation**
  - [ ] Update `ARCHITECTURE.md` with few-shot prompting component
  - [ ] Add examples to `USAGE_GUIDE.md` showing how to use few-shot examples
  - [ ] Document best practices for few-shot example selection

**Estimated Effort**: 2-3 days remaining (tutorial content + few-shot documentation)

---

## 📋 Phase 10: Deterministic Proof Gap Testing

**Priority**: HIGH - Required for v1.0 release  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Phase 1-7 (All core components must be implemented)

**Note**: See `PHASE10_PROOF_GAPS.md` for detailed test backlog with file targets and acceptance criteria.

### Definition of Done

- [ ] ContextRetrievalLayer selection behavior tests (20-25 tests)
- [ ] PromptAssembler hard bounds & truncation priority tests (25-30 tests)
- [ ] OutputParser mutation extraction & malformed handling tests (20-25 tests)
- [ ] ValidationGate ordering & gate execution tests (30-35 tests)
- [ ] MemoryMutationController authority enforcement tests (30-35 tests)
- [ ] WorldIntentDispatcher pure dispatcher behavior tests (20-25 PlayMode tests)
- [ ] Full pipeline determinism & policy integration tests (15-20 tests)
- [ ] All Critical Implementation Requirements met (see `PHASE10_PROOF_GAPS.md`)

**Estimated Effort**: 9-13 days total
- Phase 10.1-10.5 (Unit tests): 5-7 days
- Phase 10.6 (PlayMode tests): 2-3 days
- Phase 10.7 (Integration tests): 2-3 days

**Versioning Strategy**: Phase 10 is required for v1.0 release. Pre-1.0 releases (rc/preview) can ship without Phase 10 complete, but must not claim architecture is "deterministically proven" until Phase 10 is complete.

---

## 📋 Phase 11: RAG-Based Memory Retrieval & Memory Proving

**Priority**: MEDIUM - Enhancement to existing retrieval system  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Phase 3 (Context Retrieval Layer), Phase 10 (Deterministic Proof Gap Testing)

### Overview

Enhance the `ContextRetrievalLayer` to use Retrieval-Augmented Generation (RAG) techniques instead of simple keyword matching. This will improve semantic relevance of retrieved memories by using embeddings and vector similarity search. Additionally, implement the repetition recognition feature to prove that retrieval influences generation through deterministic recognition of repeated locations, topics, and conversations.

**Current State**: The `ContextRetrievalLayer` uses keyword overlap matching (see `CalculateRelevance()` method). A comment notes: "Simple keyword matching - could be enhanced with embeddings."

**Memory Proving Work**: This phase includes implementing the deterministic repetition recognition system described in `MEMORY_TODO.md` to prove that retrieval influences generation. The system recognizes both location repetition (e.g., NPC gets tired of same tunnel) and topic/conversation repetition (e.g., NPC gets tired of player obsessively talking about manta rays). This serves as a concrete proof-of-concept for the RAG system's effectiveness.

### Definition of Done

#### 11.1 Embedding Generation System
- [ ] Create `IEmbeddingProvider` interface for embedding generation
- [ ] Implement embedding provider using local model (e.g., via llama.cpp embedding endpoint)
- [ ] Implement embedding provider using external API (e.g., OpenAI, HuggingFace)
- [ ] Support configurable embedding dimensions
- [ ] Add embedding caching to avoid redundant computations
- [ ] Create `EmbeddingConfig` for provider selection and settings

#### 11.2 Vector Storage & Indexing
- [ ] Create `MemoryVectorStore` interface for vector storage
- [ ] Implement in-memory vector store (for small memory sets)
- [ ] Implement persistent vector store (optional, for large memory sets)
- [ ] Index episodic memories with embeddings
- [ ] Index beliefs with embeddings
- [ ] Index canonical facts with embeddings (optional, for semantic search)
- [ ] Support incremental updates (add/remove/update vectors)

#### 11.3 Semantic Retrieval
- [ ] Replace keyword-based `CalculateRelevance()` with vector similarity search
- [ ] Implement cosine similarity for relevance scoring
- [ ] Support hybrid retrieval (combine vector similarity with recency/significance weights)
- [ ] Add configurable similarity threshold
- [ ] Maintain backward compatibility with keyword fallback option
- [ ] Update `ContextRetrievalConfig` with embedding-related settings

#### 11.4 Integration
- [ ] Modify `ContextRetrievalLayer` to use embedding-based retrieval
- [ ] Add embedding generation on memory mutation (episodic, beliefs)
- [ ] Add batch embedding generation for existing memories (migration path)
- [ ] Update `AuthoritativeMemorySystem` to trigger embedding updates
- [ ] Add embedding statistics to memory statistics

#### 11.5 Performance & Optimization
- [ ] Benchmark embedding generation latency
- [ ] Benchmark vector search performance vs keyword search
- [ ] Implement async embedding generation to avoid blocking
- [ ] Add configurable batch size for embedding operations
- [ ] Optimize vector search for large memory sets (consider approximate nearest neighbor)

#### 11.6 Testing
- [ ] Unit tests for `IEmbeddingProvider` implementations
- [ ] Unit tests for `MemoryVectorStore` implementations
- [ ] Unit tests for semantic retrieval scoring
- [ ] Integration tests comparing RAG vs keyword retrieval quality
- [ ] Performance tests for embedding generation and vector search
- [ ] All tests in `LlamaBrain.Tests/Retrieval/` passing

#### 11.7 Memory Proving: Repetition Recognition
- [ ] Implement `RecognitionResult` DTO with `RecognitionType` enum (Location, Topic, Conversation)
- [ ] Add `EnteredLocation` episodic memory type with `LocationId` tracking (Tier A)
- [ ] Add `RepeatedTopic` episodic memory type with topic tracking (Tier B):
  - Extract topics from player input using semantic similarity (RAG) or keyword matching
  - Normalize topic text for matching
  - Track `RepeatCount`, `TopicText`, `LastMentionTick`
- [ ] Create deterministic recognition query system:
  - **Location recognition**: Query episodic memory for existing `LocationId` entries
  - **Topic recognition**: Query episodic memory for similar topics using RAG semantic similarity
  - Track `RepeatCount`, `LastOccurrenceTick`, `EvidenceSummary` for both types
  - Return `RecognitionResult` with recognition status and type
- [ ] Implement prompt constraint injection for recognized repetitions:
  - **Location**: Inject `RECOGNITION` block with familiarity cue constraint
  - **Topic**: Inject `RECOGNITION` block with topic fatigue/redirect constraint
  - Hard constraint format varies by recognition type
- [ ] Add memory mutation tracking:
  - Append repetition events (`EnteredLocation` or `RepeatedTopic`, dedupe per tick)
  - Append `SpokeLine` with `RecognitionUsed=true/false` and `RecognitionType`
  - Update `RepeatCount` for recognized items
- [ ] Create validation rules to verify recognition cues in output (location and topic)
- [ ] Integration tests proving retrieval influences generation:
  - **Location**: Two entries into same `LocationId` → `Recognized=true`, `RepeatCount=2`
  - **Topic**: Player mentions "manta rays" three times → `Recognized=true`, `RepeatCount=3`, `Type=Topic`
  - Recognition prompts include appropriate `RECOGNITION` blocks
  - Validator confirms recognition cues present in output
  - Determinism: identical memory state + identical entry → identical result

#### 11.8 Documentation
- [ ] Update `ARCHITECTURE.md` with RAG retrieval section
- [ ] Document embedding provider configuration
- [ ] Document vector store options and trade-offs
- [ ] Add examples showing RAG vs keyword retrieval differences
- [ ] Update `USAGE_GUIDE.md` with RAG setup instructions
- [ ] Document repetition recognition system (location and topic) and memory proving approach

### Technical Considerations

**Embedding Models**:
- Local: Use llama.cpp embedding endpoint (consistent with inference server)
- External: Support OpenAI `text-embedding-3-small` or similar
- Dimensions: Typically 384-1536 depending on model

**Vector Storage Options**:
- **In-Memory**: Simple `Dictionary<string, float[]>` for small sets (<1000 memories)
- **Persistent**: Consider lightweight vector DB (e.g., FAISS, Qdrant) for large sets
- **Hybrid**: Start with in-memory, add persistent option later

**Backward Compatibility**:
- Maintain keyword-based retrieval as fallback option
- Allow configuration to switch between RAG and keyword modes
- Graceful degradation if embedding generation fails

**Performance Targets**:
- Embedding generation: <100ms per memory (async)
- Vector search: <10ms for typical memory sets (<1000 items)
- Overall retrieval latency: <150ms (including embedding generation if needed)

### Estimated Effort

**Total**: 3-4 weeks
- Phase 11.1-11.2 (Embedding & Storage): 1 week
- Phase 11.3-11.4 (Retrieval & Integration): 1 week
- Phase 11.5-11.6 (Optimization & Testing): 3-5 days
- Phase 11.7 (Memory Proving): 3-5 days
- Phase 11.8 (Documentation): 2-3 days

### Success Criteria

- [ ] RAG retrieval retrieves semantically relevant memories that keyword matching misses
- [ ] Performance impact is acceptable (<200ms additional latency)
- [ ] Backward compatibility maintained (keyword mode still available)
- [ ] All existing tests pass with RAG enabled
- [ ] Repetition recognition system implemented and tested (location and topic)
- [ ] Memory proving tests demonstrate retrieval influences generation:
  - Location recognition: Recognition queries return deterministic results for repeated locations
  - Topic recognition: Recognition queries return deterministic results for repeated topics/conversations
  - Recognition cues appear in generated output when repetition recognized
  - Validation confirms recognition cues are present (both location and topic types)
- [ ] Documentation updated with RAG usage examples and memory proving approach

**See also**: [MEMORY_TODO.md](MEMORY_TODO.md) for detailed implementation plan of the repetition recognition system.

---

## 📋 Phase 12: Dedicated Structured Output

**Priority**: HIGH - Improves reliability and determinism of output parsing  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Phase 5 (Output Validation System), Phase 10 (Deterministic Proof Gap Testing)

### Overview

Replace regex-based text parsing with LLM-native structured output formats. Current `OutputParser` uses regex patterns to extract dialogue, mutations, and world intents from free-form text. Switching to dedicated structured output (JSON mode, function calling, schema-based outputs) will eliminate parsing errors and improve determinism.

**Current State**: `OutputParser` uses regex patterns to extract structured data from free-form LLM responses. This is error-prone and can fail on malformed outputs.

### Definition of Done

#### 12.1 Structured Output Provider Interface
- [ ] Create `IStructuredOutputProvider` interface for structured output generation
- [ ] Support multiple output formats:
  - JSON mode (force JSON responses)
  - Function calling / tool use
  - Schema-based structured output (OpenAI structured outputs, Anthropic tool use)
- [ ] Create `StructuredOutputConfig` for format selection and schema definition
- [ ] Support schema validation before sending to LLM

#### 12.2 JSON Schema Definition
- [ ] Define JSON schema for `ParsedOutput` structure:
  - `dialogueText` (string, required)
  - `proposedMutations` (array of mutation objects)
  - `worldIntents` (array of intent objects)
- [ ] Create schema builder API for dynamic schema generation
- [ ] Support schema versioning for backward compatibility
- [ ] Generate schema from `ParsedOutput` class structure

#### 12.3 LLM Integration
- [ ] Extend `ApiClient` to support structured output requests
- [ ] Implement JSON mode for llama.cpp (if supported)
- [ ] Implement function calling for compatible models
- [ ] Add structured output parameters to prompt requests
- [ ] Handle structured output errors gracefully

#### 12.4 Output Parser Refactoring
- [ ] Refactor `OutputParser` to use structured output when available
- [ ] Maintain backward compatibility with regex parsing as fallback
- [ ] Add `ParseStructuredOutput()` method for JSON/structured parsing
- [ ] Update `OutputParserConfig` with structured output options
- [ ] Remove or deprecate regex-based extraction (keep as fallback)

#### 12.5 Testing
- [ ] Unit tests for `IStructuredOutputProvider` implementations
- [ ] Unit tests for JSON/structured parsing
- [ ] Integration tests comparing structured vs regex parsing reliability
- [ ] Tests for schema validation
- [ ] Tests for fallback to regex when structured output fails
- [ ] All tests in `LlamaBrain.Tests/Validation/` passing

#### 12.6 Documentation
- [ ] Update `ARCHITECTURE.md` with structured output section
- [ ] Document supported structured output formats
- [ ] Document schema definition and versioning
- [ ] Update `USAGE_GUIDE.md` with structured output setup
- [ ] Add examples showing structured vs regex parsing differences

### Technical Considerations

**Supported Formats**:
- **JSON Mode**: Force LLM to respond with valid JSON matching schema
- **Function Calling**: Use tool/function calling APIs (OpenAI, Anthropic)
- **Schema-Based**: Use provider-specific structured output APIs

**Backward Compatibility**:
- Maintain regex parsing as fallback when structured output unavailable
- Allow configuration to force regex mode for testing/compatibility
- Graceful degradation if structured output fails

**Schema Definition**:
- Use JSON Schema standard for validation
- Generate schema from C# classes using reflection
- Support schema evolution (versioning)

**Performance Targets**:
- Structured output parsing: <5ms (vs ~10-20ms for regex)
- Schema validation: <1ms
- Overall parsing latency: <10ms

### Estimated Effort

**Total**: 2-3 weeks
- Phase 12.1-12.2 (Interfaces & Schema): 1 week
- Phase 12.3-12.4 (Integration & Refactoring): 1 week
- Phase 12.5-12.6 (Testing & Docs): 3-5 days

### Success Criteria

- [ ] Structured output parsing eliminates regex parsing errors
- [ ] 100% success rate on valid structured outputs (vs ~95% with regex)
- [ ] Backward compatibility maintained (regex fallback works)
- [ ] All existing tests pass with structured output enabled
- [ ] Performance improvement over regex parsing

---

## 📋 Phase 13: Structured Output Integration

**Priority**: HIGH - Completes structured output migration  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Phase 12 (Dedicated Structured Output)

### Overview

Complete integration of structured output throughout the validation pipeline, mutation extraction, and ensure full compatibility with existing systems. This phase ensures structured outputs are used consistently and all edge cases are handled.

### Definition of Done

#### 13.1 Validation Pipeline Integration
- [ ] Update `ValidationGate` to work with structured outputs
- [ ] Ensure constraint validation works with structured format
- [ ] Update canonical fact validation for structured mutations
- [ ] Integrate structured output with retry logic
- [ ] Handle structured output validation failures gracefully

#### 13.2 Mutation Extraction Enhancement
- [ ] Update `MemoryMutationController` to parse structured mutations
- [ ] Support all mutation types in structured format:
  - `AppendEpisodic` with full schema
  - `TransformBelief` with confidence/sentiment
  - `TransformRelationship` with relationship data
  - `EmitWorldIntent` with intent parameters
- [ ] Validate mutation schemas before execution
- [ ] Improve mutation extraction reliability (target 100% success)

#### 13.3 World Intent Integration
- [ ] Update `WorldIntentDispatcher` to handle structured intents
- [ ] Parse structured intent parameters correctly
- [ ] Validate intent schemas before dispatch
- [ ] Support complex intent parameters (nested objects, arrays)

#### 13.4 Error Handling & Fallback
- [ ] Comprehensive error handling for malformed structured outputs
- [ ] Automatic fallback to regex parsing on structured output failure
- [ ] Logging and metrics for structured output success/failure rates
- [ ] User-friendly error messages for structured output issues

#### 13.5 Migration & Compatibility
- [ ] Migration path for existing prompts to structured output
- [ ] Configuration to enable/disable structured output per NPC
- [ ] A/B testing support (structured vs regex parsing)
- [ ] Backward compatibility tests for all existing functionality

#### 13.6 Testing
- [ ] Integration tests for full pipeline with structured output
- [ ] Tests for all mutation types in structured format
- [ ] Tests for validation gate with structured outputs
- [ ] Tests for error handling and fallback scenarios
- [ ] Performance tests comparing structured vs regex end-to-end
- [ ] All tests in `LlamaBrain.Tests/Integration/` passing

#### 13.7 Documentation
- [ ] Update `ARCHITECTURE.md` with structured output integration details
- [ ] Document migration guide from regex to structured output
- [ ] Update `USAGE_GUIDE.md` with structured output best practices
- [ ] Troubleshooting guide for structured output issues

### Technical Considerations

**Schema Evolution**:
- Support multiple schema versions simultaneously
- Automatic schema migration for backward compatibility
- Schema validation before LLM request

**Error Recovery**:
- Detect malformed structured output early
- Automatic fallback to regex parsing
- Retry with stricter schema constraints

**Performance**:
- Structured output should be faster than regex parsing
- Cache schema definitions to avoid regeneration
- Optimize JSON parsing for large responses

### Estimated Effort

**Total**: 1-2 weeks
- Phase 13.1-13.3 (Pipeline Integration): 1 week
- Phase 13.4-13.7 (Error Handling, Testing, Docs): 3-5 days

### Success Criteria

- [ ] All mutation types work correctly with structured output
- [ ] Validation pipeline fully integrated with structured outputs
- [ ] Error handling robust with automatic fallback
- [ ] 100% backward compatibility maintained
- [ ] All existing tests pass with structured output enabled
- [ ] Performance equal or better than regex parsing

---

## 🎯 Milestones

### Milestone 1: Core Architecture (Phases 1-3) ✅
**Target**: Weeks 1-3  
**Status**: ✅ Complete
- [x] Determinism layer functional
- [x] Structured memory system working
- [x] State snapshot and retry logic operational

### Milestone 2: Validation & Control (Phases 4-6) ✅
**Target**: Weeks 4-6  
**Status**: ✅ Complete
- [x] Output validation gate functional
- [x] Working memory system operational
- [x] Memory mutation control enforced

### Milestone 3: Integration & Polish (Phases 7-9) 🚧
**Target**: Weeks 7-8  
**Status**: 🚧 Partial
- [x] Fallback system complete
- [ ] RedRoom fully integrated (60% complete)
- [ ] Documentation comprehensive (85% complete)

### Milestone 4: Production Ready ❌
**Target**: Week 9+  
**Status**: ❌ Not Started
- [ ] All tests passing (Phase 10 complete)
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
- [x] Memory decay algorithm - Significance-weighted decay with configurable rate
- [x] World intent schema - `WorldIntent` with IntentType, Target, Parameters, Priority, SourceText
- [x] Constraint escalation strategy - Configurable via `ConstraintEscalation` modes

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

**Next Review**: After Phase 8.4 completion (sample scenes)
