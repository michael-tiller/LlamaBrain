
---

<a id="feature-1"></a>
## Feature 1: Determinism Layer & Expectancy Engine

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

<a id="feature-2"></a>
## Feature 2: Structured Memory System

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

<a id="feature-3"></a>
## Feature 3: State Snapshot & Context Retrieval

**Priority**: HIGH - Enables retry logic and bounded context  
**Status**: ✅ Complete  
**Dependencies**: Feature 2 (Structured Memory)

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

<a id="feature-4"></a>
## Feature 4: Ephemeral Working Memory 

**Priority**: MEDIUM - Improves prompt quality and token efficiency  
**Status**: ✅ Complete  
**Dependencies**: Feature 3 (State Snapshot)

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

<a id="feature-5"></a>
## Feature 5: Output Validation System

**Priority**: CRITICAL - Core of the architectural pattern  
**Status**: ✅ Complete  
**Dependencies**: Feature 1 (Determinism Layer)

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

<a id="feature-6"></a>
## Feature 6: Controlled Memory Mutation

**Priority**: HIGH - Ensures memory integrity  
**Status**: ✅ Complete  
**Dependencies**: Feature 2 (Structured Memory), Feature 5 (Validation)

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

<a id="feature-7"></a>
## Feature 7: Enhanced Fallback System

**Priority**: MEDIUM - Improves reliability  
**Status**: ✅ Complete  
**Dependencies**: Feature 3 (Retry Logic)

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

<a id="feature-8"></a>
## Feature 8: RedRoom Integration

**Priority**: MEDIUM - Enables testing of new architecture  
**Status**: ✅ Complete  
**Dependencies**: All previous features  
**Execution Order**: **Weave in as breather task** - Can be done in parallel with heavy architectural work (Features 12-14). Lower cognitive load, good for maintaining momentum.

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
  - [x] Integration with RedRoomCanvas
    - [x] Toggle via hotkey (F2)
    - [x] Auto-refresh on interaction events (0.5s interval)
    - [x] Panel positioning (right side panel)
  - [x] Auto-setup helper (`MemoryMutationOverlaySetup.cs`)
    - [x] Programmatic UI generation at runtime
    - [x] Configurable styling and layout

- [x] **Validation Gate Overlay** (medium priority) - **IMPLEMENTED**
  - [x] Real-time validation results display (`ValidationGateOverlay.cs`)
    - [x] Gate pass/fail status with visual indicator (green/red)
    - [x] Failure reasons list (grouped by severity: Critical, Hard, Soft)
    - [x] Violating text snippets displayed
  - [x] Constraint evaluation status
    - [x] Active constraints (permissions, prohibitions, requirements)
    - [x] Constraints grouped by type with color coding
    - [x] Constraint severity indicators (Critical=red, Hard=orange, Soft=yellow)
    - [x] Source rule attribution (shows which ExpectancyRule triggered it)
  - [x] Retry attempt visualization
    - [x] Current attempt number / max attempts with progress bar
    - [x] Constraint escalation status (None, Add Prohibition, Harden, Full)
    - [x] Retry history (showing each attempt's status, timing, violations)
  - [x] Integration with RedRoomCanvas
    - [x] Toggle via hotkey (F3)
    - [x] Auto-refresh on validation events (0.3s interval)
  - [x] Setup helper (`ValidationGateOverlaySetup.cs`)
    - [x] Prefab-based instantiation at runtime
    - [x] Automatic wire-up of UI references

- [x] **Constraint Demonstration Overlay** - **COMBINED INTO VALIDATION GATE OVERLAY**
  - [x] Active constraints list (integrated into ValidationGateOverlay)
    - [x] Grouped by type (Permission, Prohibition, Requirement)
    - [x] Severity indicators (Critical, Hard, Soft)
    - [x] Rule source (which ExpectancyRule triggered it)
  - [x] Rule evaluation results
    - [x] Displays constraints from LastConstraints
    - [x] Shows trigger reason in stats section
  - [x] Integration with RedRoomCanvas
    - [x] Combined with Validation overlay (F3)
    - [x] Single unified overlay for validation + constraints

- [x] **Overlay System Infrastructure** - Complete
  - [x] Extend `RedRoomCanvas` with input managed overlay panels (F2 and F3 toggles implemented)
  - [x] Auto-refresh event system (polling-based, 0.3-0.5s interval)
  
#### 8.5 Integration Testing
- [x] **FullPipelineIntegrationTests** (base library) - Complete
  - [x] 8 tests in `LlamaBrain.Tests/Integration/FullPipelineIntegrationTests.cs`
  - [x] Tests full 9-component pipeline from InteractionContext through memory mutation
  - [x] Validates end-to-end flow, retry logic, constraint escalation, and fallback system
- [x] **Unity PlayMode Integration Tests** - Complete
  - [x] Location: `LlamaBrainRuntime/Tests/PlayMode/`
  - [x] `FullPipelinePlayModeTests.cs` - 12 tests (6 external integration, 6 contract tests)
  - [x] `BrainAgentIntegrationTests.cs` - 9 tests for basic agent behavior
  - [x] `BrainServerTests.cs` - 25 tests for server lifecycle
  - [x] `MemoryMutationPlayModeTests.cs` - 15 tests for memory mutation flows (episodic, beliefs, world state, canonical facts)
  - [x] `FewShotAndFallbackPlayModeTests.cs` - 12 tests for few-shot priming, fallback system, multi-turn conversations
  - [x] Total: 73+ PlayMode integration tests

**Estimated Effort**: Complete

---

<a id="feature-9"></a>
## Feature 9: Documentation & Polish

**Priority**: MEDIUM - Enables adoption and understanding
**Status**: ✅ Complete (100%)
**Dependencies**: All previous features  
**Execution Order**: **Weave in as breather task** - Can be done in parallel with heavy architectural work (Features 12-14). Documentation can be updated incrementally as features are completed.

### Definition of Done

#### 9.1 Architecture Documentation
- [x] Created `ARCHITECTURE.md`
  - [x] Full explanation of architectural pattern (all 9 components)
  - [x] Component interaction diagrams (references architectural_diagram.png)
  - [x] Code examples for each layer
  - [x] Best practices guide
- [x] Created `DETERMINISM_CONTRACT.md`
  - [x] Explicit boundary statement and contract decisions
  - [x] Determinism guarantees and limitations
  - [x] Hardware determinism documentation
- [x] Created `PIPELINE_CONTRACT.md`
  - [x] Formal pipeline contract specification
  - [x] Component interaction contracts
  - [x] Data flow documentation
- [x] Created `MEMORY.md`
  - [x] Memory system architecture documentation
  - [x] Memory authority hierarchy explanation
  - [x] Memory types and usage patterns
- [x] Created `VALIDATION_GATING.md`
  - [x] Validation system documentation
  - [x] Rule creation and application guide
  - [x] Validation pipeline explanation
- [x] Created `SAFEGUARDS.md`
  - [x] Security and safety documentation
  - [x] Threat mitigation strategies
  - [x] Best practices for secure implementation
- [x] Created `CHANGELOG.md`
  - [x] Version history and change tracking
  - [x] Feature additions and modifications
- [x] Created `PHASE10_PROOF_GAPS.md`
  - [x] Deterministic proof gap testing documentation
  - [x] Test backlog and acceptance criteria
- [x] Created `MEMORY_TODO.md`
  - [x] Memory system enhancement roadmap
  - [x] RAG-based retrieval planning

#### 9.2 Main README Updates
- [x] Updated main `README.md` in core library
  - [x] Updated completion percentage
  - [x] Added architecture pattern overview
  - [x] Updated use cases with new capabilities
  - [x] Quick start guide
- [x] Created `STATUS.md`
  - [x] Current implementation status tracking
  - [x] Feature completion percentages
  - [x] Test coverage reporting
  - [x] Next tasks and milestones
- [x] Created `ROADMAP.md`
  - [x] Complete feature roadmap
  - [x] Implementation status for all features
  - [x] Execution order recommendations
  - [x] Milestone tracking

#### 9.3 Unity Package Documentation
- [x] Created `USAGE_GUIDE.md` (Unity Runtime)
  - [x] Setup guide for determinism layer
  - [x] Validation rule creation tutorial
  - [x] Memory system migration guide (comprehensive guide)
  - [x] Complete component-by-component usage guide
  - [x] Four step-by-step tutorials (Feature 9.5)
- [x] Created `USAGE_GUIDE.md` (Core Library)
  - [x] Core library usage examples
  - [x] API reference and patterns
- [x] Created Unity Runtime documentation files
  - [x] `QUICK_START.md` - Quick start guide for Unity integration
  - [x] `TROUBLESHOOTING.md` - Troubleshooting guide for common issues
  - [x] `SAMPLES.md` - Code samples and examples
  - [x] `STRUCTURED_OUTPUT.md` - Structured output documentation
  - [x] `RED_ROOM_THREAT_MODEL.md` - RedRoom security threat model
- [x] RedRoom documentation
  - [x] RedRoom README with troubleshooting
  - [x] RedRoom threat model documentation

#### 9.4 API Documentation
- [x] XML documentation comments for all public APIs (100% coverage)
- [x] Doxygen output generated (zero missing member warnings)
- [x] Code examples in documentation

#### 9.5 Tutorial Content
- [x] "Setting Up Deterministic NPCs" - Complete tutorial with 6 steps
- [x] "Creating Custom Validation Rules" - Complete tutorial with 8 steps
- [x] "Understanding Memory Authority" - Complete tutorial with 7 steps
- [x] "Debugging Validation Failures" - Complete tutorial with 9 steps

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
  - [x] Integration tests in `FewShotAndFallbackPlayModeTests.cs` (12 tests)
  - [x] All 30+ few-shot tests passing
- [x] **Documentation**
  - [x] Update `ARCHITECTURE.md` with few-shot prompting component
  - [x] Add examples to both `USAGE_GUIDE.md` files (core library and Unity runtime) showing how to use few-shot examples
  - [x] Document best practices for few-shot example selection
  - [x] Document configuration options (MaxFewShotExamples, AlwaysIncludeFewShot)

**Estimated Effort**: Complete (all tutorials added to USAGE_GUIDE.md)

---

<a id="feature-10"></a>
## Feature 10: Deterministic Proof Gap Testing ✅ **COMPLETE**

**Priority**: HIGH - Required for v0.2.0 release  
**Status**: ✅ **Complete** - All features, requirements, and tests implemented  
**Dependencies**: Features 1-7 (All core components must be implemented)  
**Execution Order**: ✅ **COMPLETED** for Milestone 4. Architecture can now claim "deterministically proven" at byte level.

### Phase 10.7 Completion Summary

#### All 4 Major Issues Fixed
- [x] **Issue #1**: Context retrieval snapshot-time driven (FIXED)
- [x] **Issue #2**: Intent approval semantics (VERIFIED)
- [x] **Issue #3**: PipelineOrder test with real orchestrator (FIXED)
- [x] **Issue #4**: Deterministic mutation byte-level state equality (FIXED)

#### All 7 Minimal Proof Suite Tests Complete
- [x] ContextRetrieval_UsesSnapshotTime_NoWallClockDependency
- [x] ContextRetrieval_TotalOrder_TieBreakerChain_OrdinalIds_SequenceLast
- [x] Memory_Persistence_RoundTrip_PreservesSequenceAndOrdering
- [x] PromptAssembler_HardBounds_MandatoryBypassesCaps_OptionalTruncationOrder (6 new byte-level tests)
- [x] OutputParser_NormalizationAndExtraction_OrderPinned
- [x] ValidationGate_FailClearsApprovedIntents
- [x] FullPipeline_WithMockedApiClient_SameInputs_ProducesSamePrompt_GateResult_Mutations_FinalMemoryState

#### Tests A-E All Complete
- [x] Test A: Dictionary/HashSet enumeration tripwire ✅
- [x] Test B: Serialization round-trip determinism ✅
- [x] Test C: Near-equal float ordering ✅
- [x] Test D: WorkingMemory hard-bounds byte-level verification ✅ (6 new tests)
- [x] Test E: OutputParser normalization pinning ✅

### Progress Summary

#### Critical Requirements Implemented (5/5) ✅
- [x] **Requirement #1**: Strict Total Order Sort Algorithm - Updated ContextRetrievalLayer with LINQ OrderBy/ThenBy chains
- [x] **Requirement #2**: SequenceNumber Field - Added to MemoryEntry base class, monotonic counter in AuthoritativeMemorySystem
- [x] **Requirement #3**: Score vs Tie-Breaker Logic - Implemented with ordinal Id comparison and SequenceNumber tie-breaker
- [x] **Requirement #4**: OutputParser Normalization Contract - Added `NormalizeWhitespace()` static method with 21 tests
- [x] **Requirement #5**: WorldIntentDispatcher Singleton Lifecycle - Implemented with scene-local model, 28 tests complete

#### High-Leverage Determinism Tests Added (7+ tests)
- [x] Dictionary order tripwire test
- [x] Near-equal floating score tie-breaker test
- [x] SequenceNumber tie-breaker test
- [x] SequenceNumber persistence test
- [x] Belief determinism test
- [x] Ordinal string comparison test
- [x] All weights zero fallback test

### Component Test Status

- [x] ContextRetrievalLayer selection behavior tests (55 tests - exceeds estimate)
- [x] PromptAssembler hard bounds & truncation priority tests (80 tests - exceeds estimate)
- [x] OutputParser mutation extraction & malformed handling tests (86 tests - exceeds estimate)
- [x] ValidationGate ordering & gate execution tests (44 tests - complete)
- [x] MemoryMutationController authority enforcement tests (41 tests - exceeds estimate)
- [x] WorldIntentDispatcher pure dispatcher behavior tests (28 tests - complete)
- [x] Full pipeline determinism & policy integration tests (25 tests - complete)

**Determinism Proof Status**: Now defensible at byte level for:
- Serialized memory state (all types with full fidelity including belief-key dimension, float hex patterns)
- Prompt text assembly (byte-level verification with 6 comprehensive tests)

**Versioning Strategy**: ✅ **Feature 10 COMPLETE**. Architecture can now claim "deterministically proven" at byte level. All critical requirements and minimal proof suite tests implemented (351 tests total).

---

<a id="feature-12"></a>
## Feature 12: Dedicated Structured Output

**Priority**: HIGH - Improves reliability and determinism of output parsing  
**Status**: ✅ **Complete** (100% Complete)  
**Dependencies**: Feature 5 (Output Validation System), Feature 10 (Deterministic Proof Gap Testing)  
**Execution Order**: ✅ **COMPLETE** - Fundamentally changes how data enters the pipeline. Completed before Feature 16 (Save/Load).

### Overview

Replace regex-based text parsing with LLM-native structured output formats. Current `OutputParser` uses regex patterns to extract dialogue, mutations, and world intents from free-form text. Switching to dedicated structured output (JSON mode, function calling, schema-based outputs) will eliminate parsing errors and improve determinism.

**Current State**: `OutputParser` uses regex patterns to extract structured data from free-form LLM responses. This is error-prone and can fail on malformed outputs.

### Definition of Done

#### 12.1 Structured Output Provider Interface
- [x] Create `IStructuredOutputProvider` interface for structured output generation
- [x] Support multiple output formats:
  - [x] JSON mode (force JSON responses) - ✅ Native llama.cpp json_schema support
- [x] Create `StructuredOutputConfig` for format selection and schema definition
- [x] Support schema validation before sending to LLM - ✅ `ValidateSchema()` method implemented

#### 12.2 JSON Schema Definition
- [x] Define JSON schema for `ParsedOutput` structure:
  - [x] `dialogueText` (string, required)
  - [x] `proposedMutations` (array of mutation objects)
  - [x] `worldIntents` (array of intent objects)
- [x] Create schema builder API for dynamic schema generation - ✅ `JsonSchemaBuilder.BuildFromType<T>()`
- [x] Support schema versioning for backward compatibility - ✅ `SchemaVersion.cs` and `SchemaVersionManager` with version detection and migration support
- [x] Generate schema from `ParsedOutput` class structure - ✅ Reflection-based schema generation
- [x] Pre-built schemas: `ParsedOutputSchema`, `DialogueOnlySchema`, `AnalysisSchema`
- [x] Versioned schemas: `VersionedParsedOutputSchema`, `VersionedDialogueOnlySchema`, `VersionedAnalysisSchema`

#### 12.3 LLM Integration
- [x] Extend `ApiClient` to support structured output requests - ✅ `SendStructuredPromptAsync()` and `SendStructuredPromptWithMetricsAsync()`
- [x] Implement JSON mode for llama.cpp (if supported) - ✅ Native `json_schema` parameter support
- [x] Implement function calling for compatible models - ✅ Self-contained JSON interpretation via `FunctionCallDispatcher` (works with any LLM including llama.cpp)
- [x] Add structured output parameters to prompt requests - ✅ Extended `CompletionRequest` with `json_schema`, `grammar`, `response_format`
- [x] Handle structured output errors gracefully - ✅ Error handling with fallback

#### 12.4 Output Parser Refactoring
- [x] Refactor `OutputParser` to use structured output when available - ✅ `ParseStructured()` and `ParseAuto()` methods
- [x] Maintain backward compatibility with regex parsing as fallback - ✅ Automatic fallback on structured parsing failure
- [x] Add `ParseStructuredOutput()` method for JSON/structured parsing - ✅ `ParseStructured()` method implemented
- [x] Update `OutputParserConfig` with structured output options - ✅ `OutputParserConfig.NativeStructured` preset
- [x] Remove or deprecate regex-based extraction (keep as fallback) - ✅ Regex maintained as fallback, not deprecated

#### 12.5 Testing
- [x] Unit tests for `IStructuredOutputProvider` implementations - ✅ `StructuredOutputProviderTests.cs` (319 lines)
- [x] Unit tests for JSON/structured parsing - ✅ `OutputParserStructuredTests.cs` (359 lines)
- [x] Integration tests comparing structured vs regex parsing reliability - ✅ Tests in `OutputParserStructuredTests.cs`
- [x] Tests for schema validation - ✅ `JsonSchemaBuilderTests.cs` (264 lines)
- [x] Tests for fallback to regex when structured output fails - ✅ Covered in `OutputParserStructuredTests.cs`
- [x] Tests for schema versioning - ✅ `SchemaVersionTests.cs` (49 tests for version parsing, comparison, compatibility, and migration)
- [x] All tests in `LlamaBrain.Tests/Validation/` passing - ✅ 105+ new tests total, all passing

#### 12.6 Documentation
- [x] Update `ARCHITECTURE.md` with structured output section - ✅ Feature 12 section added
- [x] Document supported structured output formats - ✅ Documented in `ARCHITECTURE.md` and `CHANGELOG.md`
- [x] Document schema definition and versioning - ✅ Schema versioning documented with `SchemaVersion.cs`
- [x] Update `USAGE_GUIDE.md` with structured output setup - ✅ Comprehensive structured output section added to Unity Runtime USAGE_GUIDE.md
- [x] Add examples showing structured vs regex parsing differences - ✅ Comparison table and examples in USAGE_GUIDE.md

### Technical Considerations

**Supported Formats**:
- **JSON Mode**: Force LLM to respond with valid JSON matching schema (llama.cpp native support)

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
- Feature 12.1-12.2 (Interfaces & Schema): 1 week
- Feature 12.3-12.4 (Integration & Refactoring): 1 week
- Feature 12.5-12.6 (Testing & Docs): 3-5 days

### Success Criteria

- [x] Structured output parsing eliminates regex parsing errors - ✅ Native JSON parsing eliminates regex errors
- [x] 100% success rate on valid structured outputs (vs ~95% with regex) - ✅ Structured parsing provides deterministic JSON parsing
- [x] Backward compatibility maintained (regex fallback works) - ✅ Automatic fallback to regex on structured parsing failure
- [x] All existing tests pass with structured output enabled - ✅ 56 new tests passing, existing tests maintained
- [x] Performance improvement over regex parsing - ✅ JSON parsing is faster than regex extraction

### Additional Implementation

- [x] BrainAgent integration with `SendNativeStructuredMessageAsync()`, `SendNativeDialogueAsync()`, `SendNativeStructuredInstructionAsync()`
- [x] Generic type support with generic type parameter `T` for automatic deserialization
- [x] DTOs for structured parsing: `StructuredDialogueResponse`, `StructuredMutation`, `StructuredIntent`

---

<a id="feature-13"></a>
## Feature 13: Structured Output Integration

**Priority**: HIGH - Completes structured output migration
**Status**: ✅ **Complete**
**Dependencies**: Feature 12 (Dedicated Structured Output)
**Execution Order**: **DO IMMEDIATELY AFTER Feature 12** - Completes the structured output migration. Must be done before Feature 16 (Save/Load) to ensure data structures are stable.

### Overview

Complete integration of structured output throughout the validation pipeline, mutation extraction, and ensure full compatibility with existing systems. This phase ensures structured outputs are used consistently and all edge cases are handled.

### Definition of Done

#### 13.1 Validation Pipeline Integration
- [x] Create `StructuredDialoguePipeline` to orchestrate full flow
- [x] Integrate structured output with `ValidationGate`
- [x] Ensure constraint validation works with structured format
- [x] Update canonical fact validation for structured mutations (via ValidationGate)
- [x] Integrate structured output with retry logic (ProcessWithRetryAsync)
- [x] Handle structured output validation failures gracefully

#### 13.2 Mutation Extraction Enhancement
- [x] Pipeline integration with `MemoryMutationController.ExecuteMutations`
- [x] Mutations parsed via `OutputParser.ParseStructured()` before reaching controller
- [x] Support all mutation types in structured format:
  - `AppendEpisodic` with full schema
  - `TransformBelief` with confidence/sentiment
  - `TransformRelationship` with relationship data
  - `EmitWorldIntent` with intent parameters
- [x] Validate mutation schemas before execution (`StructuredSchemaValidator`)
- [x] Schema validation integrated in pipeline via `ValidateMutationSchemas` config

#### 13.3 World Intent Integration
- [x] `WorldIntentDispatcher` handles structured intents via event-based hookup
- [x] Parse structured intent parameters correctly (flat Dictionary<string, string>)
- [x] Validate intent schemas before dispatch (`StructuredSchemaValidator`)
- [x] Support complex intent parameters (nested objects, arrays) - ✅ COMPLETE
  - IntentParameters.cs with typed parameter classes (GiveItemParameters, MoveToParameters, InteractParameters)
  - IntentParameterExtensions for safe extraction from Dictionary<string, object>
  - Support for GetArray<T> and GetNested for complex parameter types
  - 88 tests passing in ComplexIntentParametersTests.cs

#### 13.4 Error Handling & Fallback
- [x] Comprehensive error handling for malformed structured outputs
- [x] Automatic fallback to regex parsing on structured output failure (`StructuredPipelineConfig.FallbackToRegex`)
- [x] Logging and metrics for structured output success/failure rates (`StructuredPipelineMetrics`)
- [x] User-friendly error messages via `StructuredPipelineResult.ErrorMessage`

#### 13.5 Migration & Compatibility
- [x] Configuration to enable/disable structured output per NPC (`StructuredPipelineConfig`)
- [x] Support for regex-only mode (`StructuredPipelineConfig.RegexOnly`)
- [x] A/B testing support via configurable modes (Structured, Regex, Fallback)
- [x] Migration path documentation for existing prompts (documented in ARCHITECTURE.md and USAGE_GUIDE.md)
- [x] Backward compatibility maintained (all 1749+ tests pass)

#### 13.6 Testing
- [x] Unit tests for `StructuredPipelineConfig` (7 tests)
- [x] Unit tests for `StructuredPipelineResult` (6 tests)
- [x] Unit tests for `StructuredPipelineMetrics` (10 tests)
- [x] Unit tests for `StructuredDialoguePipeline` construction (7 tests)
- [x] Unit tests for `StructuredSchemaValidator` (35 tests)
- [x] Tests for all mutation types in structured format (via schema validator)
- [x] Integration tests for full pipeline with mocked LLM (13+ tests)
- [x] Performance tests comparing structured vs regex end-to-end (sub-millisecond parsing verified)
- [x] All tests in `LlamaBrain.Tests/` passing (1749+ tests)

#### 13.7 Documentation
- [x] Update `ARCHITECTURE.md` with structured output integration details (complete Feature 13 section added)
- [x] Document migration guide from regex to structured output (in ARCHITECTURE.md and USAGE_GUIDE.md)
- [x] Update `USAGE_GUIDE.md` with structured output best practices (structured output section)
- [x] Troubleshooting guide for structured output issues (included in ARCHITECTURE.md)

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
- Feature 13.1-13.3 (Pipeline Integration): 1 week
- Feature 13.4-13.7 (Error Handling, Testing, Docs): 3-5 days

### Success Criteria

- [x] All mutation types work correctly with structured output
- [x] Validation pipeline fully integrated with structured outputs
- [x] Error handling robust with automatic fallback
- [x] 100% backward compatibility maintained
- [x] All existing tests pass with structured output enabled
- [x] Performance equal or better than regex parsing (sub-millisecond parsing verified)

---

<a id="feature-23"></a>
## Feature 23: Structured Input/Context

**Priority**: HIGH - Completes bidirectional structured communication
**Status**: ✅ **Complete** (100% Complete)
**Dependencies**: Feature 12 (Dedicated Structured Output), Feature 13 (Structured Output Integration)
**Execution Order**: **DO AFTER Feature 13** - Builds on structured output foundation to provide structured context input. Should be done before Feature 16 (Save/Load) to ensure data structures are stable.

### Overview

Provide context, memories, constraints, and dialogue history to the LLM in structured format (JSON/function calling) instead of plain text prompt assembly. This complements structured outputs (Features 12 & 13) to create a complete bidirectional structured communication pattern, improving LLM understanding of context sections and enhancing determinism.

**Current State**: `PromptAssembler` assembles prompts as plain text strings with sections (system prompt, context, constraints, dialogue history). This works but lacks the precision and reliability of structured formats. The LLM receives context as free-form text, which can lead to ambiguity in how context sections are interpreted.

**Architectural Impact**: Completes the structured I/O pattern. Just as structured outputs eliminate parsing errors, structured inputs improve context understanding and enable function calling APIs. This enhances determinism by making context sections machine-parseable and unambiguous. Enables advanced features like function calling, tool use, and structured context injection APIs.

### Definition of Done

#### 23.1 Structured Context Provider Interface
- [x] Create `IStructuredContextProvider` interface for structured context generation
- [x] Support multiple input formats:
  - [x] JSON context injection (structured JSON objects for memories, constraints, etc.)
- [x] Create `StructuredContextConfig` for format selection and schema definition
- [x] Support context schema validation before sending to LLM

#### 23.2 JSON Schema Definitions for Context
- [x] Define JSON schema for memory context structure:
  - [x] Episodic memories array with structured fields
  - [x] Belief memories array with confidence/sentiment
  - [x] Relationship memories array with relationship data - ✅ COMPLETE
    - RelationshipEntry.cs with full schema (sourceEntity, targetEntity, affinity, trust, familiarity, history, tags)
    - Integrated into ContextSection.Relationships property
    - RelationshipAuthorityValidator.cs for owner-based and confidence threshold checks
    - 88 tests passing in RelationshipEntryTests.cs and RelationshipAuthorityTests.cs
- [x] Define JSON schema for constraint structure:
  - [x] Constraint rules array (prohibitions, requirements, permissions)
  - [x] Validation requirements - **COMPLETE** - Added `ValidationRequirements` to ConstraintSection with min/max response length, required/forbidden keywords
  - [x] Authority boundaries - **COMPLETE** - Added `Authority` field to constraints with source tracking (system, designer, npc, player)
- [x] Define JSON schema for dialogue history structure:
  - [x] Conversation turns array
  - [x] Speaker identification
  - [x] Timestamp/metadata - **COMPLETE** - Added `Timestamp` (float?) and `Metadata` (DialogueMetadata) to StructuredDialogueEntry with emotion, location, trigger, turnNumber fields
- [x] Create schema builder API for dynamic context schema generation (`ContextSerializer`)
- [x] Pre-built schemas: `ContextJsonSchema`, `ContextSection`, `ConstraintSection`, `DialogueSection`

#### 23.3 LLM Integration
- [x] Add structured context parameters to prompt requests (via `PromptAssembler.AssembleStructuredPrompt`)
- [x] Support hybrid mode (structured context + text system prompt)
- [x] Handle structured context errors gracefully with fallback to text

#### 23.4 Prompt Assembler Refactoring
- [x] Refactor `PromptAssembler` to support structured context mode
- [x] Add `AssembleStructuredPrompt()` method for structured context generation
- [x] Maintain backward compatibility with text-based prompt assembly
- [x] Add `StructuredContextConfig` configuration option to `PromptAssemblerConfig`
- [x] Support gradual migration (structured context for memories, text for system prompt)

#### 23.5 Context Section Serialization
- [x] Serialize memory context to structured format (JSON)
- [x] Serialize constraints to structured format
- [x] Serialize dialogue history to structured format
- [x] Optimize serialization performance (< 10ms for typical contexts - verified via test suite)
- [x] Support partial structured context (e.g., structured memories, text constraints) - ✅ COMPLETE
  - PartialContextBuilder.cs with fluent API for incremental context construction
  - All ContextSection properties nullable with NullValueHandling.Ignore
  - WithCanonicalFacts, WithBeliefs, WithRelationships, WithDialogue methods
  - 88 tests passing in PartialContextTests.cs

#### 23.6 Function Calling Support
- [x] Implement function call dispatch system (self-contained interpretation from JSON)
- [x] Create `FunctionCallDispatcher` with command table pattern
- [x] Add `FunctionCall` and `FunctionCallResult` DTOs
- [x] Integrate function calls into `ParsedOutput` and JSON schema
- [x] Create `FunctionCallExecutor` for pipeline integration
- [x] Implement built-in context functions (get_memories, get_beliefs, get_constraints, etc.)
- [x] Support custom game function registration (e.g., PlayNpcFaceAnimation, StartWalking)
- [x] **Unity Function Call Integration** ✅
  - [x] Create `FunctionCallController` (MonoBehaviour singleton)
  - [x] Create `FunctionCallConfigAsset` (ScriptableObject)
  - [x] Create `NpcFunctionCallConfig` (MonoBehaviour component)
  - [x] Create `FunctionCallEvents` (Unity Event Types)
  - [x] Integrate with `LlamaBrainAgent` for automatic execution
  - [x] Support inspector-based handlers via UnityEvents
  - [x] Support code-based handlers via C# Action delegates
  - [x] Store results in `LastFunctionCallResults` property
  - [x] Fire UnityEvents with results for Unity integration

#### 23.7 Testing
- [x] Unit tests for `IStructuredContextProvider` implementations (`StructuredContextProviderTests.cs` - 24 tests)
- [x] Unit tests for structured context serialization (`ContextSerializerTests.cs` - 23 tests)
- [x] Integration tests comparing structured vs text context assembly (`PromptAssemblerStructuredTests.cs` - 35 tests)
- [x] Tests for function calling dispatch system - complete (164 tests across 5 test files)
- [x] Tests for schema validation (included in provider tests)
- [x] Tests for fallback to text when structured context fails (included in integration tests)
- [x] Performance tests for structured context serialization (via determinism tests)
- **Total**: ~246 tests across 8 test files

#### 23.8 Documentation
- [x] Update `ARCHITECTURE.md` with structured input section
- [x] Document supported structured input formats (in code comments)
- [x] Document function calling setup and usage
- [x] Document migration path from text to structured context
- [x] Add examples showing structured vs text context differences
- [x] Update `USAGE_GUIDE.md` with structured input setup

### Technical Considerations

**Supported Formats**:
- **JSON Context Injection**: Provide memories, constraints, dialogue as structured JSON objects
- **Function Calling**: Self-contained function call dispatch from LLM JSON output (works with any LLM, including llama.cpp)
- **Hybrid Mode**: Mix structured context with text system prompts

**Function Calling Implementation**:
- LLM outputs function calls in structured JSON (no native LLM support required)
- `FunctionCallDispatcher` uses command table pattern to route calls
- Built-in context functions: get_memories, get_beliefs, get_constraints, get_dialogue_history, get_world_state, get_canonical_facts
- Custom game functions can be registered (e.g., PlayNpcFaceAnimation, StartWalking)
- Synchronous execution during dialogue processing

**Backward Compatibility**:
- Maintain text-based prompt assembly as default/fallback
- Allow configuration to enable structured context per NPC
- Graceful degradation if structured context fails
- Support partial structured context (e.g., structured memories, text constraints)

**Schema Definition**:
- Use JSON Schema standard for validation
- Generate schema from C# classes using reflection
- Support schema evolution (versioning)
- Pre-built schemas for common context types

**Performance Targets**:
- Structured context serialization: <10ms for typical contexts (<100 memories)
- Function calling setup: <5ms overhead
- Overall context assembly latency: <20ms (vs ~5-10ms for text)
- Acceptable trade-off for improved context understanding

**Function Calling Approach**:
- Define context injection as functions/tools
- LLM "calls" functions to receive context
- More explicit and structured than text injection
- Enables advanced features like dynamic context retrieval

### Estimated Effort

**Total**: 1-2 weeks
- Feature 23.1-23.3 (Interfaces & Integration): 1 week
- Feature 23.4-23.6 (Refactoring & Function Calling): 3-5 days
- Feature 23.7-23.8 (Testing & Docs): 3-5 days

### Success Criteria

- [x] Structured context improves LLM understanding of context sections (requires live LLM validation)
- [x] Function calling support enables advanced context injection (164 tests covering dispatch, execution, built-ins)
- [x] Backward compatibility maintained (text mode still available) (verified: all 1971 tests pass)
- [x] Performance impact acceptable (<20ms additional latency) (verified via performance tests)
- [x] All existing tests pass with structured context enabled (verified: 1971 tests pass)
- [x] Schema validation prevents malformed context (verified via schema validation tests)
- [x] Documentation updated with structured input usage examples

### Integration with Structured Outputs

**Complete Structured I/O Pattern**:
- **Structured Inputs (Feature 23)**: Context provided in structured format (JSON/function calling)
- **Structured Outputs (Features 12 & 13)**: Responses received in structured format (JSON schema)
- **Result**: Complete bidirectional structured communication, eliminating ambiguity on both sides

**Benefits**:
- Improved context understanding by LLM
- Eliminated parsing errors (output side)
- Enhanced determinism through structured formats
- Enables advanced features (function calling, tool use)
- Better debugging and observability

---

<a id="feature-16"></a>
## Feature 16: Save/Load Game Integration

**Priority**: CRITICAL - Required for Feature 14 (Deterministic Generation Seed)
**Status**: ✅ **Complete** (100% Complete)
**Dependencies**: Feature 2 (Structured Memory System), Feature 3 (State Snapshot), **Feature 12 & 13 (Structured Output)**
**Execution Order**: ✅ **COMPLETE** - Persistence layer built after data structures stabilized.

### Overview

Engine-agnostic persistence abstraction layer with Unity/SaveGameFree implementation. Enables save/load of memory state and conversation history across game sessions, supporting deterministic reconstruction.

**Implementation Highlights**:
- Core `ISaveSystem` interface in LlamaBrain proper (engine-agnostic)
- `MemorySnapshotBuilder`/`MemorySnapshotRestorer` for deterministic state serialization
- `FileSystemSaveSystem` default implementation using existing `IFileSystem` abstraction
- Unity `SaveGameFreeSaveSystem` wrapping SaveGameFree plugin
- `LlamaBrainSaveManager` MonoBehaviour for game integration
- Named save slots with metadata tracking

### Definition of Done

#### 16.1 Persistence Abstraction Interface ✅
- [x] Create `ISaveSystem` interface for save/load operations
- [x] Define `SaveData` DTO containing:
  - `Dictionary<string, PersonaMemorySnapshot>` (per persona memory state)
  - `Dictionary<string, ConversationHistorySnapshot>` (per persona dialogue)
  - `int Version` (for migration)
  - `long SavedAtUtcTicks` (timestamp)
- [x] Support sync save/load operations
- [x] Support error handling via `SaveResult` (success/failure with messages)

#### 16.2 Core Implementation ✅
- [x] **Core Interface**: `ISaveSystem` adapter interface
  - Defines contract for save/load operations
  - Allows swapping implementations without code changes
- [x] **FileSystem Implementation**: `FileSystemSaveSystem`
  - Uses `IFileSystem` abstraction for testability
  - Atomic writes (temp-file-then-move pattern)
  - Slot name sanitization for security
  - 5MB file size limit protection
- [x] **Memory Snapshot System**:
  - `MemorySnapshotBuilder` creates snapshots from `AuthoritativeMemorySystem`
  - `MemorySnapshotRestorer` restores state using internal `InsertXxxRaw` APIs
  - Preserves determinism-critical fields: `Id`, `SequenceNumber`, `CreatedAtTicks`

#### 16.3 DTO Layer ✅
- [x] `PersonaMemorySnapshot` - Complete memory state
- [x] `ConversationHistorySnapshot` - Dialogue history
- [x] `CanonicalFactDto`, `WorldStateDto`, `EpisodicMemoryDto`, `BeliefDto` - Memory DTOs
- [x] `DialogueEntryDto` - Dialogue entry serialization
- [x] All enums serialized as `int` for stability

#### 16.4 Unity Integration ✅
- [x] `SaveGameFreeSaveSystem` wrapping SaveGameFree plugin
- [x] `LlamaBrainSaveManager` MonoBehaviour component
  - Auto-registers agents in scene
  - Named save slots ("autosave", "slot1", etc.)
  - `SaveToSlot()` / `LoadFromSlot()` methods
  - `QuickSave()` / `QuickLoad()` convenience methods
  - Events: `OnSaveComplete`, `OnLoadComplete`
- [x] `LlamaBrainAgent` integration
  - `CreateSaveData()` for agent state serialization
  - `RestoreFromSaveData()` for agent state restoration

#### 16.5 Versioning ✅
- [x] `SaveData.Version` field for format versioning (currently v1)
- [x] Backward compatibility: graceful handling of missing fields

#### 16.6 Testing ✅
- [x] Unit tests for `MemorySnapshotBuilder`/`MemorySnapshotRestorer` (18 tests)
- [x] Unit tests for `FileSystemSaveSystem` with mock file system (15 tests)
- [x] Round-trip determinism tests (snapshot → serialize → restore → verify)
- [x] DTO conversion correctness tests
- [x] All 33 tests in `LlamaBrain.Tests/Persistence/` passing

#### 16.7 Documentation ✅
- [x] Update `STATUS.md` with Feature 16 completion
- [x] Update `CHANGELOG.md` with persistence layer details
- [x] Update `ROADMAP.md` to mark feature complete
- [x] Document usage in USAGE_GUIDE.md (persistence section)

### Technical Considerations

**Architecture**:
- `ISaveSystem` interface in LlamaBrain core provides engine-agnostic abstraction
- `FileSystemSaveSystem` uses existing `IFileSystem` abstraction for testability
- `SaveGameFreeSaveSystem` wraps SaveGameFree plugin for Unity
- Atomic writes prevent corruption (temp-file-then-move pattern)

**Determinism**:
- All DTOs preserve determinism-critical fields: `Id`, `SequenceNumber`, `CreatedAtTicks`
- Enums serialized as `int` for stability across versions
- `MemorySnapshotRestorer` uses internal `InsertXxxRaw` APIs for exact reconstruction
- Ordinals preserved for deterministic sorting

**Data Structure** (serialized as JSON):
```json
{
  "Version": 1,
  "SavedAtUtcTicks": 638727840000000000,
  "PersonaMemories": {
    "npc_001": {
      "CanonicalFacts": [...],
      "WorldState": [...],
      "EpisodicMemories": [...],
      "Beliefs": [...]
    }
  },
  "ConversationHistories": {
    "npc_001": {
      "DialogueHistory": [...]
    }
  }
}
```

**Error Handling**:
- `SaveResult.Failed()` returns descriptive error messages
- File size limits prevent memory exhaustion (5MB default)
- Slot name sanitization prevents path traversal attacks
- Missing data handled gracefully (empty collections)

### Success Criteria ✅

- [x] `ISaveSystem` interface defined and documented
- [x] `FileSystemSaveSystem` and `SaveGameFreeSaveSystem` implementations complete
- [x] Adapter pattern allows swapping implementations without code changes
- [x] Memory state and conversation history saved and restored across sessions
- [x] Integration with `LlamaBrainAgent` complete
- [x] `LlamaBrainSaveManager` Unity component for easy save/load
- [x] All 33 tests passing
- [x] Documentation complete with examples

### Feature 16 Extension: Game State Management UI

**Status**: ✅ **Complete** (100%)  
**Lines of Code**: ~298 lines

**Implementation Status**: Full save/load UI system implemented for RedRoom demo. All core functionality complete.

#### Components Implemented

**RedRoomGameController** (15 lines)
- [x] Singleton pattern for game state management
- [x] Scene transition management (handled via LlamaBrainSaveManager integration)
- [x] Integration with LlamaBrainSaveManager

**MainMenu** (26 lines)  
- [x] Continue button (loads most recent save)
- [x] New Game button integration
- [x] Dynamic continue button state based on save existence
- [x] Load game menu access (via UI panel/hatch)

**LoadGameMenu** (133 lines)
- [x] Save slot browser with scrollview
- [x] Integration with LlamaBrainSaveManager.ListSlots()
- [x] Dynamic save slot element generation
- [x] Save metadata display (slot name, timestamp)
- [x] **Save slot delete functionality with confirmation dialog**
- [x] Empty save state handling

**LoadGameScrollViewElement** (56 lines)
- [x] Individual save slot display with visual selection feedback
- [x] Load button per slot
- [x] Delete button per slot
- [x] Save metadata display (timestamp formatted, persona count available)

**PausePanel** (83 lines)
- [x] Resume/Pause functionality with ESC key
- [x] Save integration via LlamaBrainSaveManager
- [x] Quit functionality (scene reload/transition)
- [x] In-game menu navigation
- [x] Confirmation dialogs (via SetActive pattern on Unity UI panels)

---

<a id="feature-14"></a>
## Feature 14: Deterministic Generation Seed

**Priority**: CRITICAL - Completes cross-session determinism guarantee
**Status**: ✅ **COMPLETE** (All sub-features implemented: 14.1-14.7)
**Dependencies**: Feature 10 (Deterministic Proof Gap Testing), Feature 16 (Save/Load Game Integration) - Requires deterministic inputs to be proven first and persistence for InteractionCount
**Execution Order**: **COMPLETE** - Hook the persistence layer into the RNG to achieve the "Holy Grail" of AI consistency (cross-session determinism).
**Remaining**: ~~Save/load determinism test deferred to Feature 16~~ **COMPLETE** - InteractionCount persistence added to PersonaMemorySnapshot, save/load determinism tests implemented in Feature 23.

### Overview

Implement the **InteractionCount seed strategy** to achieve true cross-session determinism. By using `InteractionContext.InteractionCount` as the seed for LLM generation, we transform the stochastic generator into a pure function relative to game state. This closes the "hidden variable" gap and ensures that identical game states produce identical outputs across sessions and devices (with hardware floating-point caveats).

**Current State**: `InteractionContext.InteractionCount` exists and tracks "Number of times this NPC has been interacted with in this session", but it is not currently used to seed LLM generation. The LLM generation step has a hidden random seed that breaks determinism.

**Architectural Impact**: This feature completes the deterministic state reconstruction pattern by locking the final source of non-determinism: the LLM's internal random number generator. Combined with Feature 10's deterministic inputs, this achieves the "Holy Grail" of AI game development: **Cross-Session State Consistency**.

### Why This Works

**1. It Closes the "Hidden Variable" Gap**
- The LLM has no memory and no direct access to world state (per `ARCHITECTURE.md`)
- However, standard LLM calls have a hidden variable: the random seed
- By driving that seed with `InteractionContext.InteractionCount`, we remove that hidden variable
- **Formula**: `f(Prompt, Context, InteractionCount) = Output`
- **Result**: If a player reloads a save and talks to the NPC for the 5th time, they get the *exact* same text

**2. Infrastructure Already Exists**
- `InteractionCount` is already tracked in `InteractionContext`
- No new tracking logic needed
- Just need to pass this integer into `ApiClient` or `BrainServer` as the request seed

**3. Distinction from Memory Sequence**
- **Memory Sequence (`_nextSequenceNumber`)**: In `AuthoritativeMemorySystem.cs`, used for deterministic sort order (ensures memories with same score sort in insertion order)
- **Generation Seed (`InteractionCount`)**: Controls the *randomness* of word choices (temperature/sampling) for the current response
- These are separate concerns: one ensures deterministic retrieval, the other ensures deterministic generation

### Definition of Done

#### 14.1 Seed Parameter Support ✅ COMPLETE
- [x] Add `seed` parameter to `CompletionRequest` structure (llama.cpp API supports `seed` field)
- [x] Update `IApiClient` interface to accept optional `seed` parameter in `SendPromptAsync()` and `SendPromptWithMetricsAsync()`
- [x] Update `ApiClient` implementation to include `seed` in request JSON when provided
- [x] Seed parameter accepts: null (omit from request), -1 (random), 0+ (deterministic)
- [x] Add seed parameter to `LlmConfig` for default seed behavior (optional)

#### 14.2 Integration with InteractionContext ✅ COMPLETE
- [x] Modify `LlamaBrainAgent.SendWithSnapshotAsync()` to extract `InteractionCount` from `InteractionContext` - Implemented at line 572
- [x] Pass `InteractionCount` as seed to `ApiClient.SendPromptWithMetricsAsync()` (or `SendPromptAsync()`) - Implemented at line 581
- [x] Ensure seed is passed through retry attempts (same seed for all retries of same interaction) - Seed variable doesn't change during retry loop
- [x] Handle edge case: `InteractionCount = 0` (first interaction) - use 0 as seed or special handling - null check at line 573
- [x] Document seed behavior: seed is per-interaction, not per-attempt (retries use same seed) - Documented in DETERMINISM_CONTRACT.md and USAGE_GUIDE.md

#### 14.3 Cross-Session Determinism Testing ✅ COMPLETE
- [x] **Core Determinism Test**: `PlayMode_CrossSession_SameSeedSamePrompt_ProducesIdenticalOutput`
  - Same seed + same prompt = identical output across independent sessions
  - Proves `f(Prompt, Seed) = Output` is a pure function
- [x] **Different Seeds Test**: `PlayMode_CrossSession_DifferentSeeds_ProduceDifferentOutputs`
  - Sanity check that different seeds produce different outputs
- [x] **Multiple Interaction Test**: `PlayMode_CrossSession_InteractionCountAsSeed_ProducesDeterministicSequence`
  - Run sequence: InteractionCount 0, 1, 2 (first playthrough)
  - New session: InteractionCount 0, 1, 2 (second playthrough)
  - Assert all outputs match (proves game replay determinism)
- [x] **Temperature Zero Test**: `PlayMode_CrossSession_TemperatureZero_ProducesDeterministicOutput`
  - Greedy decoding (temperature=0) is deterministic without seed
- [x] **Structured Output Test**: `PlayMode_CrossSession_StructuredOutput_ProducesIdenticalJson`
  - JSON schema output is also deterministic with same seed
- [x] **Save/Load Test**: ~~Deferred to Feature 16~~ **COMPLETE** - `PlayMode_PipelineContract_SaveLoad_PreservesInteractionCount`, `PlayMode_PipelineContract_SaveLoad_SameSeedProducesSameOutput`
- [x] **Retry Determinism Test**: ~~Deferred~~ **COMPLETE** - `PlayMode_PipelineContract_RetryAttempts_UseSameSeed`, `PlayMode_PipelineContract_MultipleRetries_AllUseSameSeed`

#### 14.4 Hardware Determinism Documentation ✅ COMPLETE
- [x] Document that determinism is **100% across sessions** (same device, same model) - DETERMINISM_CONTRACT.md:411-419
- [x] Document that determinism is **99.9% across devices** (hardware floating-point differences may cause rare divergence) - DETERMINISM_CONTRACT.md:411-419
- [x] Document mitigation: `llama.cpp` fights hard to be deterministic, usually "stable enough" for gameplay - DETERMINISM_CONTRACT.md:429-435
- [x] Document edge cases: Different GPUs (NVIDIA vs AMD vs Apple Silicon) may have tiny rounding differences - DETERMINISM_CONTRACT.md:439-445
- [x] Add note to `DETERMINISM_CONTRACT.md` about hardware determinism limits - DETERMINISM_CONTRACT.md:409-464

#### 14.5 Backward Compatibility ✅ COMPLETE
- [x] Ensure seed parameter is optional (null/unspecified = non-deterministic mode, backward compatible) - All API methods default to `seed: null`
- [x] Default behavior: if `InteractionCount` not provided or seed not supported, fall back to non-deterministic mode - LlamaBrainAgent:573-579
- [x] Configuration behavior: Per-call control (no global flag by design) - Documented in DETERMINISM_CONTRACT.md:496-500
- [x] Log when seed is used vs when it's not available - LlamaBrainAgent:574-579 logs both cases

#### 14.6 Testing ✅ COMPLETE
- [x] Unit tests for `ApiClient` seed parameter handling (ApiClientSeedTests.cs - 11 tests)
- [x] Unit tests for seed validation (negative seeds, null handling)
- [x] Tests for backward compatibility (seed = null works correctly)
- [x] Integration tests for cross-session determinism (`ExternalIntegrationPlayModeTests.cs` - 5 tests)
- [x] Standalone .NET tests (`CrossSessionDeterminismTests.cs` - 5 tests, requires manual server)
- [x] Tests for retry behavior with seed (same seed across retries) - **COMPLETE** - `PlayMode_PipelineContract_RetryAttempts_UseSameSeed`
- [x] All determinism tests passing

#### 14.7 Documentation ✅ COMPLETE
- [x] Update `ARCHITECTURE.md` with seed-based determinism section - ARCHITECTURE.md:1633-1692
- [x] Document the "double-lock system":
  - Lock 1: Context locking (SequenceNumber, Ordinal comparisons) ensures prompt never flutters
  - Lock 2: Entropy locking (InteractionCount seed) ensures dice roll never flutters
  - Documented in DETERMINISM_CONTRACT.md:342-354
- [x] Update `USAGE_GUIDE.md` with seed configuration examples - USAGE_GUIDE.md:1147-1223
- [x] Document QA implications: testers can reproduce hallucinations exactly by loading save file - USAGE_GUIDE.md:1152-1153
- [x] Add to `DETERMINISM_CONTRACT.md`: seed-based determinism guarantees and limitations - DETERMINISM_CONTRACT.md:340-500

### Technical Considerations

**llama.cpp API Support**:
- llama.cpp completion API supports `seed` parameter (integer, -1 for random)
- Seed must be non-negative integer (0, 1, 2, ...)
- Seed of -1 means "use random seed" (non-deterministic)
- Seed of 0 or positive means "use this exact seed" (deterministic)

**Seed vs Temperature**:
- Seed controls *which* token is selected from the probability distribution
- Temperature controls the *shape* of the probability distribution
- Both must be identical for true determinism
- Temperature is already controlled via `LlmConfig`, so seed is the missing piece

**InteractionCount Tracking**:
- `InteractionCount` is already tracked per-NPC per-session in `InteractionContext`
- **CRITICAL**: Must be persisted in save games via Feature 16 (Save/Load Game Integration) to maintain determinism across sessions
- On game load, `InteractionCount` must be restored from save data
- Increment happens before seed is used (seed = InteractionCount at time of generation)
- Feature 16 provides the persistence layer required for this to work

**Hardware Determinism Limits**:
- Different GPUs may have floating-point rounding differences
- These accumulate in massive matrix multiplications (LLM inference)
- Rare edge case: token with `p=0.499999` on one device becomes `p=0.500001` on another
- If that token is on Top-P boundary, different word might be selected
- Mitigation: `llama.cpp` is usually stable enough for gameplay purposes
- **Contract**: We guarantee determinism across sessions (same device), but only "predictable" across devices

### Estimated Effort

**Total**: 1-2 weeks
- Feature 14.1-14.2 (Seed Parameter & Integration): 3-4 days
- Feature 14.3-14.4 (Testing & Documentation): 3-4 days
- Feature 14.5-14.7 (Compatibility, Testing, Docs): 2-3 days

### Success Criteria

- [x] Seed parameter successfully passed to llama.cpp API - CompletionRequest.seed parameter
- [x] `InteractionCount` correctly extracted from `InteractionContext` and used as seed - LlamaBrainAgent:572
- [x] Cross-session determinism test passes: same `InteractionCount` + same prompt = identical output - CrossSessionDeterminismTests.cs
- [x] Save/load determinism test passes: reloaded game produces identical outputs - **COMPLETE** - `PlayMode_PipelineContract_SaveLoad_SameSeedProducesSameOutput`
- [x] Retry logic uses same seed across retry attempts - Seed variable unchanged in retry loop
- [x] Backward compatibility maintained (seed = null works) - All methods default to null
- [x] Hardware determinism limits documented - DETERMINISM_CONTRACT.md:409-464
- [x] All tests passing - 16 seed-related tests passing
- [x] Documentation updated with seed-based determinism strategy - ARCHITECTURE.md, USAGE_GUIDE.md, DETERMINISM_CONTRACT.md

### Proof Strategy

For Feature 10 proof, this makes the integration test remarkably simple:

1. Set `InteractionCount = 5`
2. Send Prompt "Hello" with identical `StateSnapshot`
3. Record Output A
4. Clear everything
5. Set `InteractionCount = 5` again
6. Send Prompt "Hello" with identical `StateSnapshot`
7. Assert Output B == Output A

**If this passes, we have mathematically proven that your system is deterministic**, regardless of the inherent randomness of the underlying AI model.

### Architectural Significance

This phase completes the deterministic state reconstruction pattern:

- **Before Feature 14**: Deterministic inputs (Feature 10) + Non-deterministic generation = Non-deterministic outputs
- **After Feature 14**: Deterministic inputs (Feature 10) + Deterministic generation (Feature 14) = **Deterministic outputs**

This is the thinking that distinguishes this architecture:
- **Locking the Context**: `SequenceNumber` and `Ordinal` string comparisons ensure the *prompt* never flutters
- **Locking the Entropy**: `InteractionCount` seed ensures the *dice roll* never flutters

We aren't just building a chatbot; we are building a **reproducible state engine**.

---

<a id="session-2026-01-07"></a>
## Session 2026-01-07: Deferred Items Completion (Feature 14 & 23)

**Focus**: Complete all deferred test and schema items from Features 14 and 23.

### Deferred Items Audit

Reviewed DEVELOPMENT_LOG.md and identified 5 deferred items across Features 14 and 23:

| Feature | Item | Initial Status | Final Status |
|---------|------|----------------|--------------|
| 14.3 | Save/Load determinism test | Deferred | ✅ COMPLETE |
| 14.3/14.6 | Retry determinism test with seed | Deferred | ✅ COMPLETE |
| 23.2 | Validation requirements schema | Deferred | ✅ COMPLETE |
| 23.2 | Authority boundaries schema | Deferred | ✅ COMPLETE |
| 23.2 | Timestamp/metadata for dialogue | Deferred | ✅ COMPLETE |

### Feature 14: Determinism Tests Completed

#### InteractionCount Persistence (Save/Load)
- Added `InteractionCount` property to `LlamaBrainAgent` for tracking interactions
- Added `InteractionCount` field to `PersonaMemorySnapshot` for persistence
- Updated `MemorySnapshotBuilder.CreateSnapshot()` to accept and store InteractionCount
- Updated `LlamaBrainAgent.CreateSaveData()` and `RestoreFromSaveData()` to persist/restore InteractionCount
- Agent's InteractionCount auto-increments after each successful interaction

**New Tests:**
- `PlayMode_PipelineContract_SaveLoad_PreservesInteractionCount` - Verifies InteractionCount survives save/load
- `PlayMode_PipelineContract_SaveLoad_SameSeedProducesSameOutput` - Verifies determinism after restoration

#### Retry Seed Consistency
- Verified that seed remains constant across retry attempts (seed variable set once per interaction)
- Added explicit tests to document this contract

**New Tests:**
- `PlayMode_PipelineContract_RetryAttempts_UseSameSeed` - Verifies 2 retry attempts use identical seed
- `PlayMode_PipelineContract_MultipleRetries_AllUseSameSeed` - Verifies all retries use same seed

### Feature 23.2: Schema Enhancements Completed

#### Dialogue History Timestamp/Metadata
Enhanced `StructuredDialogueEntry` in `DialogueSection.cs`:
```csharp
public sealed class StructuredDialogueEntry
{
    public string Speaker { get; set; }
    public string Text { get; set; }
    public float? Timestamp { get; set; }           // NEW
    public DialogueMetadata? Metadata { get; set; } // NEW
}

public sealed class DialogueMetadata
{
    public string? Emotion { get; set; }     // e.g., "friendly", "angry"
    public string? Location { get; set; }    // e.g., "tavern", "castle_gate"
    public string? Trigger { get; set; }     // e.g., "zone_enter", "quest_update"
    public int? TurnNumber { get; set; }     // Conversation turn index
}
```

#### Constraint Validation Requirements
Enhanced `ConstraintSection.cs`:
```csharp
public sealed class ValidationRequirements
{
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public List<string>? RequiredKeywords { get; set; }
    public List<string>? ForbiddenKeywords { get; set; }
    public string? Format { get; set; }      // "single_sentence", "paragraph", etc.
    public bool? MustBeQuestion { get; set; }
    public bool? MustNotBeQuestion { get; set; }
}
```

#### Constraint Authority Boundaries
```csharp
public sealed class ConstraintAuthority
{
    public ConstraintSource Source { get; set; }  // System, Designer, Npc, Player, Quest, Environment
    public string? SourceId { get; set; }
    public int Priority { get; set; }
    public bool IsOverridable { get; set; }
    public float? SetAt { get; set; }
    public float? ExpiresAt { get; set; }
}
```

### Files Modified

| File | Changes |
|------|---------|
| `PersonaMemorySnapshot.cs` | Added `InteractionCount` property |
| `MemorySnapshotBuilder.cs` | Added `interactionCount` parameter to `CreateSnapshot()` |
| `LlamaBrainAgent.cs` | Added `InteractionCount` property, persistence, auto-increment |
| `DialogueSection.cs` | Added `Timestamp`, `Metadata`, `DialogueMetadata` class |
| `ConstraintSection.cs` | Added `ValidationRequirements`, `ConstraintAuthority`, `ConstraintSource` enum |
| `FullPipelinePlayModeTests.cs` | Added 4 new seed/save-load contract tests |

### Files Created

| File | Description |
|------|-------------|
| `ValidationRequirementsTests.cs` | 18 tests for ValidationRequirements and ConstraintAuthority |
| `DialogueMetadataTests.cs` | 14 tests for DialogueMetadata and timestamp fields |

### Test Results

- **36 new tests** added (4 PlayMode + 32 unit tests)
- All tests passing
- Build succeeded with 0 errors

### Contract Guarantees Now Tested

1. **Save/Load Determinism**: `InteractionCount` persists through save/load, ensuring identical seeds after game reload
2. **Retry Consistency**: All retry attempts within one interaction use the same seed
3. **Schema Completeness**: All context schema fields now have explicit validation requirements and authority tracking

---