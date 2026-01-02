# LlamaBrain Implementation Roadmap

**Goal**: Implement the complete "Continuity Emerges from Deterministic State Reconstruction Around a Stateless Generator" architectural pattern.

**Last Updated**: January 1, 2026

---

## Quick Links

- **Status**: See `STATUS.md` for current feature completion and next tasks
- **Determinism Contract**: See `DETERMINISM_CONTRACT.md` for explicit boundary statement and contract decisions
- **Feature 10 Test Gaps**: See `PHASE10_PROOF_GAPS.md` for detailed test backlog

---

## Progress Overview

| Features | Status | Priority |
|---------|--------|----------|
| [Feature 1: Determinism Layer](#feature-1) | ✅ Complete | CRITICAL |
| [Feature 2: Structured Memory System](#feature-2) | ✅ Complete | HIGH |
| [Feature 3: State Snapshot & Context Retrieval](#feature-3) | ✅ Complete | HIGH |
| [Feature 4: Ephemeral Working Memory](#feature-4) | ✅ Complete | MEDIUM |
| [Feature 5: Output Validation System](#feature-5) | ✅ Complete | CRITICAL |
| [Feature 6: Controlled Memory Mutation](#feature-6) | ✅ Complete | HIGH |
| [Feature 7: Enhanced Fallback System](#feature-7) | ✅ Complete | MEDIUM |
| [Feature 8: RedRoom Integration](#feature-8) | ✅ Complete | MEDIUM |
| [Feature 9: Documentation](#feature-9) | ✅ Complete | MEDIUM |
| [Feature 10: Deterministic Proof Gap Testing](#feature-10) | ✅ Complete | HIGH |
| [Feature 11: RAG-Based Memory Retrieval](#feature-11) | 📋 Planned | MEDIUM |
| [Feature 12: Dedicated Structured Output](#feature-12) | ✅ Complete | HIGH |
| [Feature 13: Structured Output Integration](#feature-13) | ✅ Complete | HIGH |
| [Feature 14: Deterministic Generation Seed](#feature-14) | 📋 Planned | CRITICAL |
| [Feature 15: Multiple NPC Support](#feature-15) | 📋 Planned | MEDIUM |
| [Feature 16: Save/Load Game Integration](#feature-16) | 📋 Planned | CRITICAL |
| [Feature 17: Token Cost Tracking & Analytics](#feature-17) | 📋 Planned | MEDIUM |
| [Feature 18: Concurrent Request Handling & Thread Safety](#feature-18) | 📋 Planned | MEDIUM |
| [Feature 19: Health Check & Resilience](#feature-19) | 📋 Planned | MEDIUM |
| [Feature 20: Memory Change History Visualization](#feature-20) | 📋 Planned | LOW |
| [Feature 21: Sidecar Host](#feature-21) | 📋 Planned | MEDIUM |
| [Feature 22: Unreal Engine Support](#feature-22) | 📋 Planned | MEDIUM |
| [Feature 23: Structured Input/Context](#feature-23) | 📋 Planned | HIGH |
| [Feature 24: "I've seen this" Recognition](#feature-24) | 📋 Planned | MEDIUM |

---

## 🎯 Recommended Execution Order

**Critical Path for Milestone 5 (v0.3.0 - The Production Update):**

The following execution order is **strongly recommended** for v0.3.0 to avoid rework and ensure architectural stability:

### Phase 1: Foundation (Do First)
1. **Feature 12 & 13 (Structured Output)** - **DO THIS FIRST**
   - Fundamentally changes how data enters the pipeline
   - Don't build persistence for data structures that are about to change
   - Complete Feature 12, then immediately do Feature 13
   - **Rationale**: Output parsing is core to the system; changing it later requires reworking everything downstream

### Phase 2: Persistence Layer
2. **Feature 16 (Save/Load Game Integration)** - **DO THIS SECOND**
   - Build persistence layer after data structures are stable
   - Persist `InteractionCount` and other deterministic state
   - **Rationale**: Must be built on stable data structures from Phase 1

### Phase 3: Determinism Completion
3. **Feature 14 (Deterministic Generation Seed)** - **DO THIS THIRD**
   - Hook the persistence layer into the RNG to achieve cross-session determinism
   - Uses persisted `InteractionCount` from Feature 16
   - **Rationale**: This is the "Holy Grail" of AI consistency, but requires persistence to work

### Phase 4: Proof & Validation
4. **Feature 10 (Deterministic Proof Gap Testing)** - ✅ **COMPLETE**
   - All features, requirements, and tests implemented (351 tests total)
   - **Rationale**: Architecture can now claim "deterministically proven" at byte level. Required for v0.2.0.

### Post-Milestone 5: Enhanced Features
5. **Milestone 6 Features (11, 15, 17, 18, 19, 20)** - **Only after Milestone 5 complete**
   - Feature 11: RAG-Based Memory Retrieval
   - Feature 15: Multiple NPC Support
   - Feature 17: Token Cost Tracking & Analytics
   - Feature 18: Concurrent Request Handling & Thread Safety
   - Feature 19: Health Check & Resilience
   - **Rationale**: These are enhancements that build on a stable foundation

**Key Principle**: Build the foundation (structured output) before building on top of it (persistence, determinism). Don't build persistence for data structures that will change.

**Note**: Milestone 4 (v0.2.0) is complete and ready for open source release. The execution order above applies to Milestone 5 (v0.3.0).

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

- [x] **Validation Gate Overlay** (medium priority) - **IMPLEMENTED (fixes needed)**
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

**Note**: See `VERIFICATION_REPORT.md` for Phase 10.7 completion status and `PHASE10_PROOF_GAPS.md` for remaining test backlog.

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

<a id="feature-11"></a>
## Feature 11: RAG-Based Memory Retrieval & Memory Proving

**Priority**: MEDIUM - Enhancement to existing retrieval system  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Feature 3 (Context Retrieval Layer), Feature 10 (Deterministic Proof Gap Testing)

### Overview

Enhance the `ContextRetrievalLayer` to use a **hybrid approach** combining Retrieval-Augmented Generation (RAG) techniques with existing keyword matching. This hybrid system will use both noun-based keyword matching (for safe, deterministic checks) and semantic inference via embeddings and vector similarity search (for improved relevance). Additionally, implement the repetition recognition feature to prove that retrieval influences generation through deterministic recognition of repeated locations, topics, and conversations.

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

#### 11.3 Hybrid Semantic Retrieval
- [ ] Implement hybrid retrieval system combining noun-based keyword matching with semantic vector similarity
- [ ] Keep existing keyword-based `CalculateRelevance()` for deterministic noun-based checks
- [ ] Add vector similarity search using cosine similarity for semantic inference
- [ ] Combine both approaches: noun-based matching (safe, deterministic) + vector similarity (semantic relevance)
- [ ] Support configurable weights for hybrid scoring (keyword vs semantic)
- [ ] Add configurable similarity threshold for semantic matching
- [ ] Update `ContextRetrievalConfig` with hybrid retrieval settings (keyword weights, semantic weights, thresholds)

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

**Hybrid Approach**:
- **Noun-based keyword matching**: Maintained for safe, deterministic checks (existing functionality preserved)
- **Semantic inference**: Added via embeddings and vector similarity for improved relevance
- **Combined scoring**: Both approaches work together, with configurable weights
- **Graceful degradation**: If embedding generation fails, system falls back to keyword-only mode
- **Configuration**: Allow tuning of hybrid weights (e.g., 70% semantic, 30% keyword) or keyword-only mode

**Performance Targets**:
- Embedding generation: <100ms per memory (async)
- Vector search: <10ms for typical memory sets (<1000 items)
- Overall retrieval latency: <150ms (including embedding generation if needed)

### Estimated Effort

**Total**: 3-4 weeks
- Feature 11.1-11.2 (Embedding & Storage): 1 week
- Feature 11.3-11.4 (Retrieval & Integration): 1 week
- Feature 11.5-11.6 (Optimization & Testing): 3-5 days
- Feature 11.7 (Memory Proving): 3-5 days
- Feature 11.8 (Documentation): 2-3 days

### Success Criteria

- [ ] Hybrid retrieval (noun-based + semantic) retrieves semantically relevant memories that keyword-only matching misses, while maintaining deterministic noun-based checks
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
  - [ ] Function calling / tool use - 📋 Optional enhancement (not required for v0.2.0)
  - [ ] Schema-based structured output (OpenAI structured outputs, Anthropic tool use) - 📋 Optional enhancement
- [x] Create `StructuredOutputConfig` for format selection and schema definition
- [x] Support schema validation before sending to LLM - ✅ `ValidateSchema()` method implemented

#### 12.2 JSON Schema Definition
- [x] Define JSON schema for `ParsedOutput` structure:
  - [x] `dialogueText` (string, required)
  - [x] `proposedMutations` (array of mutation objects)
  - [x] `worldIntents` (array of intent objects)
- [x] Create schema builder API for dynamic schema generation - ✅ `JsonSchemaBuilder.BuildFromType<T>()`
- [ ] Support schema versioning for backward compatibility - 📋 Optional enhancement
- [x] Generate schema from `ParsedOutput` class structure - ✅ Reflection-based schema generation
- [x] Pre-built schemas: `ParsedOutputSchema`, `DialogueOnlySchema`, `AnalysisSchema`

#### 12.3 LLM Integration
- [x] Extend `ApiClient` to support structured output requests - ✅ `SendStructuredPromptAsync()` and `SendStructuredPromptWithMetricsAsync()`
- [x] Implement JSON mode for llama.cpp (if supported) - ✅ Native `json_schema` parameter support
- [ ] Implement function calling for compatible models - 📋 Optional enhancement
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
- [x] All tests in `LlamaBrain.Tests/Validation/` passing - ✅ 56 new tests total, all passing

#### 12.6 Documentation
- [x] Update `ARCHITECTURE.md` with structured output section - ✅ Feature 12 section added
- [x] Document supported structured output formats - ✅ Documented in `ARCHITECTURE.md` and `CHANGELOG.md`
- [x] Document schema definition and versioning - ✅ Schema generation documented (versioning is optional enhancement)
- [ ] Update `USAGE_GUIDE.md` with structured output setup - 📋 Can be added as enhancement
- [ ] Add examples showing structured vs regex parsing differences - 📋 Can be added as enhancement

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
- [x] Support complex intent parameters (nested objects, arrays) - deferred to F23 (noted as future enhancement)

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
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Feature 12 (Dedicated Structured Output), Feature 13 (Structured Output Integration)  
**Execution Order**: **DO AFTER Feature 13** - Builds on structured output foundation to provide structured context input. Should be done before Feature 16 (Save/Load) to ensure data structures are stable.

### Overview

Provide context, memories, constraints, and dialogue history to the LLM in structured format (JSON/function calling) instead of plain text prompt assembly. This complements structured outputs (Features 12 & 13) to create a complete bidirectional structured communication pattern, improving LLM understanding of context sections and enhancing determinism.

**Current State**: `PromptAssembler` assembles prompts as plain text strings with sections (system prompt, context, constraints, dialogue history). This works but lacks the precision and reliability of structured formats. The LLM receives context as free-form text, which can lead to ambiguity in how context sections are interpreted.

**Architectural Impact**: Completes the structured I/O pattern. Just as structured outputs eliminate parsing errors, structured inputs improve context understanding and enable function calling APIs. This enhances determinism by making context sections machine-parseable and unambiguous. Enables advanced features like function calling, tool use, and structured context injection APIs.

### Definition of Done

#### 23.1 Structured Context Provider Interface
- [ ] Create `IStructuredContextProvider` interface for structured context generation
- [ ] Support multiple input formats:
  - [ ] JSON context injection (structured JSON objects for memories, constraints, etc.)
  - [ ] Function calling / tool use (OpenAI, Anthropic compatible)
  - [ ] Schema-based context (structured context APIs)
- [ ] Create `StructuredContextConfig` for format selection and schema definition
- [ ] Support context schema validation before sending to LLM

#### 23.2 JSON Schema Definitions for Context
- [ ] Define JSON schema for memory context structure:
  - [ ] Episodic memories array with structured fields
  - [ ] Belief memories array with confidence/sentiment
  - [ ] Relationship memories array with relationship data
- [ ] Define JSON schema for constraint structure:
  - [ ] Constraint rules array
  - [ ] Validation requirements
  - [ ] Authority boundaries
- [ ] Define JSON schema for dialogue history structure:
  - [ ] Conversation turns array
  - [ ] Speaker identification
  - [ ] Timestamp/metadata
- [ ] Create schema builder API for dynamic context schema generation
- [ ] Pre-built schemas: `MemoryContextSchema`, `ConstraintSchema`, `DialogueHistorySchema`

#### 23.3 LLM Integration
- [ ] Extend `ApiClient` to support structured context requests
- [ ] Implement function calling for compatible models (OpenAI, Anthropic)
- [ ] Add structured context parameters to prompt requests
- [ ] Support hybrid mode (structured context + text system prompt)
- [ ] Handle structured context errors gracefully with fallback to text

#### 23.4 Prompt Assembler Refactoring
- [ ] Refactor `PromptAssembler` to support structured context mode
- [ ] Add `AssembleStructuredContext()` method for structured context generation
- [ ] Maintain backward compatibility with text-based prompt assembly
- [ ] Add `StructuredContextMode` configuration option
- [ ] Support gradual migration (structured context for memories, text for system prompt)

#### 23.5 Context Section Serialization
- [ ] Serialize memory context to structured format (JSON/function calling)
- [ ] Serialize constraints to structured format
- [ ] Serialize dialogue history to structured format
- [ ] Optimize serialization performance (target <10ms for typical contexts)
- [ ] Support partial structured context (e.g., structured memories, text constraints)

#### 23.6 Function Calling Support
- [ ] Implement function/tool definitions for context injection
- [ ] Support OpenAI function calling format
- [ ] Support Anthropic tool use format
- [ ] Support llama.cpp function calling (if available)
- [ ] Define context injection functions (e.g., `add_memory`, `add_constraint`, `add_dialogue_turn`)

#### 23.7 Testing
- [ ] Unit tests for `IStructuredContextProvider` implementations
- [ ] Unit tests for structured context serialization
- [ ] Integration tests comparing structured vs text context assembly
- [ ] Tests for function calling context injection
- [ ] Tests for schema validation
- [ ] Tests for fallback to text when structured context fails
- [ ] Performance tests for structured context serialization

#### 23.8 Documentation
- [ ] Update `ARCHITECTURE.md` with structured input section
- [ ] Document supported structured input formats
- [ ] Document function calling setup and usage
- [ ] Document migration path from text to structured context
- [ ] Add examples showing structured vs text context differences
- [ ] Update `USAGE_GUIDE.md` with structured input setup

### Technical Considerations

**Supported Formats**:
- **JSON Context Injection**: Provide memories, constraints, dialogue as structured JSON objects
- **Function Calling**: Use tool/function calling APIs (OpenAI, Anthropic) for context injection
- **Schema-Based**: Use provider-specific structured context APIs
- **Hybrid Mode**: Mix structured context with text system prompts

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

- [ ] Structured context improves LLM understanding of context sections
- [ ] Function calling support enables advanced context injection
- [ ] Backward compatibility maintained (text mode still available)
- [ ] Performance impact acceptable (<20ms additional latency)
- [ ] All existing tests pass with structured context enabled
- [ ] Schema validation prevents malformed context
- [ ] Documentation updated with structured input usage examples

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

<a id="feature-14"></a>
## Feature 14: Deterministic Generation Seed

**Priority**: CRITICAL - Completes cross-session determinism guarantee  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Feature 10 (Deterministic Proof Gap Testing), Feature 16 (Save/Load Game Integration) - Requires deterministic inputs to be proven first and persistence for InteractionCount  
**Execution Order**: **DO THIS THIRD** (after Feature 16). Hook the persistence layer into the RNG to achieve the "Holy Grail" of AI consistency (cross-session determinism).

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

#### 14.1 Seed Parameter Support
- [ ] Add `seed` parameter to `CompletionRequest` structure (llama.cpp API supports `seed` field)
- [ ] Update `IApiClient` interface to accept optional `seed` parameter in `SendPromptAsync()` and `SendPromptWithMetricsAsync()`
- [ ] Update `ApiClient` implementation to include `seed` in request JSON when provided
- [ ] Validate seed parameter (must be non-negative integer, or null/unspecified for non-deterministic mode)
- [ ] Add seed parameter to `LlmConfig` for default seed behavior (optional)

#### 14.2 Integration with InteractionContext
- [ ] Modify `LlamaBrainAgent.SendWithSnapshotAsync()` to extract `InteractionCount` from `InteractionContext`
- [ ] Pass `InteractionCount` as seed to `ApiClient.SendPromptWithMetricsAsync()` (or `SendPromptAsync()`)
- [ ] Ensure seed is passed through retry attempts (same seed for all retries of same interaction)
- [ ] Handle edge case: `InteractionCount = 0` (first interaction) - use 0 as seed or special handling
- [ ] Document seed behavior: seed is per-interaction, not per-attempt (retries use same seed)

#### 14.3 Cross-Session Determinism Testing
- [ ] **Core Determinism Test**: 
  - Set `InteractionCount = 5`
  - Send Prompt "Hello" with identical `StateSnapshot`
  - Record Output A
  - Clear everything (new session)
  - Set `InteractionCount = 5` again
  - Send Prompt "Hello" with identical `StateSnapshot`
  - Assert Output B == Output A (byte-for-byte identical)
- [ ] **Save/Load Test**:
  - Create interaction with `InteractionCount = 3`
  - Save game state
  - Reload game state
  - Create interaction with `InteractionCount = 3` again
  - Assert identical output
- [ ] **Multiple Interaction Test**:
  - Run sequence: InteractionCount 1, 2, 3, 4, 5
  - Save state
  - Reload state
  - Run sequence: InteractionCount 1, 2, 3, 4, 5 again
  - Assert all outputs match (proves seed progression works)
- [ ] **Retry Determinism Test**:
  - Same `InteractionCount`, same prompt, same snapshot
  - Trigger retry (validation failure)
  - Assert retry uses same seed (output may differ due to constraint escalation, but seed is consistent)

#### 14.4 Hardware Determinism Documentation
- [ ] Document that determinism is **100% across sessions** (same device, same model)
- [ ] Document that determinism is **99.9% across devices** (hardware floating-point differences may cause rare divergence)
- [ ] Document mitigation: `llama.cpp` fights hard to be deterministic, usually "stable enough" for gameplay
- [ ] Document edge cases: Different GPUs (NVIDIA vs AMD vs Apple Silicon) may have tiny rounding differences
- [ ] Add note to `DETERMINISM_CONTRACT.md` about hardware determinism limits

#### 14.5 Backward Compatibility
- [ ] Ensure seed parameter is optional (null/unspecified = non-deterministic mode, backward compatible)
- [ ] Default behavior: if `InteractionCount` not provided or seed not supported, fall back to non-deterministic mode
- [ ] Add configuration flag to enable/disable seed-based determinism (for testing/debugging)
- [ ] Log when seed is used vs when it's not available

#### 14.6 Testing
- [ ] Unit tests for `ApiClient` seed parameter handling
- [ ] Unit tests for seed validation (negative seeds, null handling)
- [ ] Integration tests for cross-session determinism (Phase 14.3 tests)
- [ ] Tests for retry behavior with seed (same seed across retries)
- [ ] Tests for backward compatibility (seed = null works correctly)
- [ ] All tests in `LlamaBrain.Tests/Determinism/` passing

#### 14.7 Documentation
- [ ] Update `ARCHITECTURE.md` with seed-based determinism section
- [ ] Document the "double-lock system":
  - Lock 1: Context locking (SequenceNumber, Ordinal comparisons) ensures prompt never flutters
  - Lock 2: Entropy locking (InteractionCount seed) ensures dice roll never flutters
- [ ] Update `USAGE_GUIDE.md` with seed configuration examples
- [ ] Document QA implications: testers can reproduce hallucinations exactly by loading save file
- [ ] Add to `DETERMINISM_CONTRACT.md`: seed-based determinism guarantees and limitations

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

- [ ] Seed parameter successfully passed to llama.cpp API
- [ ] `InteractionCount` correctly extracted from `InteractionContext` and used as seed
- [ ] Cross-session determinism test passes: same `InteractionCount` + same prompt = identical output
- [ ] Save/load determinism test passes: reloaded game produces identical outputs
- [ ] Retry logic uses same seed across retry attempts
- [ ] Backward compatibility maintained (seed = null works)
- [ ] Hardware determinism limits documented
- [ ] All tests passing
- [ ] Documentation updated with seed-based determinism strategy

### Proof Strategy

For Feature 10 proof, this makes the integration test remarkably simple:

1. Set `InteractionCount = 5`
2. Send Prompt "Hello" with identical `StateSnapshot`
3. Record Output A
4. Clear everything
5. Set `InteractionCount = 5` again
6. Send Prompt "Hello" with identical `StateSnapshot`
7. Assert Output B == Output A

**If this passes, you have mathematically proven that your system is deterministic**, regardless of the inherent randomness of the underlying AI model.

### Architectural Significance

This phase completes the deterministic state reconstruction pattern:

- **Before Feature 14**: Deterministic inputs (Feature 10) + Non-deterministic generation = Non-deterministic outputs
- **After Feature 14**: Deterministic inputs (Feature 10) + Deterministic generation (Feature 14) = **Deterministic outputs**

This is the "Senior" thinking that distinguishes this architecture:
- **Locking the Context**: `SequenceNumber` and `Ordinal` string comparisons ensure the *prompt* never flutters
- **Locking the Entropy**: `InteractionCount` seed ensures the *dice roll* never flutters

You aren't just building a chatbot; you are building a **reproducible state engine**.

---

<a id="feature-15"></a>
## Feature 15: Multiple NPC Support

**Priority**: MEDIUM - Enables multi-NPC scenarios and shared memory  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Feature 2 (Structured Memory System), Feature 3 (Context Retrieval), Feature 11 (RAG-Based Memory Retrieval)

### Overview

Enable support for multiple NPCs in the same conversation context, including NPC-to-NPC interactions, shared memory systems, and coordinated behavior. This extends the single-NPC architecture to support complex multi-agent scenarios where NPCs can interact with each other, share memories, and coordinate actions.

**Current State**: The system is designed for single-NPC interactions. Each `LlamaBrainAgent` operates independently with its own memory system. Multi-NPC scenarios require coordination, shared context, and NPC-to-NPC interaction triggers.

**Use Cases**:
- Group conversations (player talks to multiple NPCs simultaneously)
- NPC-to-NPC dialogue (NPCs interact with each other)
- Shared knowledge systems (NPCs in a group share certain memories)
- Coordinated actions (multiple NPCs respond to the same world event)
- Party/faction systems (NPCs with shared goals and memories)

### Definition of Done

#### 15.1 Multi-NPC Conversation Context
- [ ] Create `MultiNpcConversationContext` class extending `InteractionContext`
- [ ] Support multiple NPC IDs in a single interaction
- [ ] Track which NPC is currently speaking/responding
- [ ] Support conversation turn management (player → NPC1 → NPC2 → player)
- [ ] Add `ConversationMode` enum (SingleNpc, MultiNpc, NpcToNpc)
- [ ] Update `StateSnapshot` to support multi-NPC context

#### 15.2 Shared Memory Systems
- [ ] Create `SharedMemoryGroup` class for managing shared memories across NPCs
- [ ] Support group-level canonical facts (shared world knowledge)
- [ ] Support group-level world state (shared game state)
- [ ] Support group-level episodic memories (shared events)
- [ ] Memory authority hierarchy: Individual NPC memories > Shared group memories
- [ ] Add `MemorySharingConfig` for configuring what memories are shared
- [ ] Update `AuthoritativeMemorySystem` to support shared memory queries

#### 15.3 NPC-to-NPC Interaction Triggers
- [ ] Add `NpcInteraction` trigger reason (already exists in enum, needs implementation)
- [ ] Create `NpcToNpcTrigger` class for triggering NPC-to-NPC interactions
- [ ] Support NPC-to-NPC dialogue generation (NPC1 speaks to NPC2)
- [ ] Validate NPC-to-NPC outputs (both NPCs must pass validation)
- [ ] Support NPC-to-NPC memory mutations (both NPCs can update shared memories)
- [ ] Add `NpcInteractionContext` for NPC-to-NPC specific context

#### 15.4 Coordinated Constraint Evaluation
- [ ] Extend `ExpectancyEvaluator` to evaluate rules for multiple NPCs
- [ ] Support group-level constraints (apply to all NPCs in group)
- [ ] Support NPC-specific constraints (apply only to individual NPC)
- [ ] Constraint conflict resolution (when NPCs have conflicting constraints)
- [ ] Update `ConstraintSet` to support multi-NPC scenarios
- [ ] Add constraint priority system (group constraints override individual)

#### 15.5 Group Memory Retrieval
- [ ] Extend `ContextRetrievalLayer` to retrieve from shared memory groups
- [ ] Support hybrid retrieval (individual NPC memories + shared group memories)
- [ ] Priority system: Individual memories > Shared memories (for relevance)
- [ ] Update `RetrievedContext` to include shared memory sources
- [ ] Support RAG-based retrieval across shared memories (when Feature 11 complete)

#### 15.6 Multi-NPC World Intent Coordination
- [ ] Extend `WorldIntentDispatcher` to handle intents from multiple NPCs
- [ ] Support intent coordination (multiple NPCs can contribute to same intent)
- [ ] Intent conflict resolution (when NPCs have conflicting intents)
- [ ] Group intent priority system
- [ ] Add `CoordinatedIntent` class for multi-NPC intents
- [ ] Update intent history to track multi-NPC intents

#### 15.7 Integration
- [ ] Update `LlamaBrainAgent` to support multi-NPC mode
- [ ] Add `MultiNpcConversationManager` Unity component
- [ ] Support switching between single-NPC and multi-NPC modes
- [ ] Update `NpcDialogueTrigger` to support multi-NPC triggers
- [ ] Add Unity events for NPC-to-NPC interactions
- [ ] Update RedRoom integration for multi-NPC testing

#### 15.8 Performance & Optimization
- [ ] Benchmark multi-NPC conversation latency
- [ ] Optimize shared memory queries (caching, indexing)
- [ ] Support async NPC-to-NPC interactions (non-blocking)
- [ ] Configurable limits on concurrent NPCs in conversation
- [ ] Memory pooling for multi-NPC contexts

#### 15.9 Testing
- [ ] Unit tests for `MultiNpcConversationContext`
- [ ] Unit tests for `SharedMemoryGroup`
- [ ] Unit tests for NPC-to-NPC interaction triggers
- [ ] Integration tests for multi-NPC conversations
- [ ] Integration tests for shared memory systems
- [ ] Performance tests for concurrent NPC interactions
- [ ] All tests in `LlamaBrain.Tests/MultiNpc/` passing

#### 15.10 Documentation
- [ ] Update `ARCHITECTURE.md` with multi-NPC architecture section
- [ ] Document shared memory system design
- [ ] Document NPC-to-NPC interaction patterns
- [ ] Update `USAGE_GUIDE.md` with multi-NPC setup examples
- [ ] Add examples for group conversations, NPC-to-NPC dialogue
- [ ] Troubleshooting guide for multi-NPC scenarios

### Technical Considerations

**Memory Sharing Strategies**:
- **Full Sharing**: All memories shared (simple but may leak information)
- **Selective Sharing**: Only specific memory types shared (canonical facts, world state)
- **Event-Based Sharing**: Memories shared when specific events occur
- **Group-Based Sharing**: NPCs in same group share memories

**Conversation Turn Management**:
- **Sequential**: NPCs respond one at a time in order
- **Parallel**: NPCs can respond simultaneously (more complex)
- **Coordinated**: NPCs coordinate responses (requires additional logic)

**Performance Considerations**:
- Multiple NPCs = multiple LLM calls (latency multiplies)
- Shared memory queries add overhead
- Constraint evaluation for multiple NPCs increases computation
- Consider batching or parallel processing for NPC-to-NPC interactions

**Backward Compatibility**:
- Single-NPC mode must remain fully functional
- Existing `LlamaBrainAgent` components should work unchanged
- Multi-NPC features should be opt-in (not required)

### Estimated Effort

**Total**: 2-3 weeks
- Feature 15.1-15.3 (Context & Triggers): 1 week
- Feature 15.4-15.6 (Coordination & Memory): 1 week
- Feature 15.7-15.10 (Integration, Testing, Docs): 3-5 days

### Success Criteria

- [ ] Multiple NPCs can participate in same conversation
- [ ] NPC-to-NPC interactions work correctly
- [ ] Shared memory systems function as designed
- [ ] Constraint evaluation works for multi-NPC scenarios
- [ ] Performance acceptable (<500ms per NPC response in group)
- [ ] Backward compatibility maintained (single-NPC mode unchanged)
- [ ] All tests passing
- [ ] Documentation complete with examples

---

<a id="feature-16"></a>
## Feature 16: Save/Load Game Integration

**Priority**: CRITICAL - Required for Feature 14 (Deterministic Generation Seed)  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Feature 2 (Structured Memory System), Feature 3 (State Snapshot), **Feature 12 & 13 (Structured Output)**  
**Execution Order**: **DO THIS SECOND** (after Features 12 & 13). Build persistence layer after data structures are stable. Don't build persistence for data structures that are about to change.

### Overview

Implement a simple, abstracted save/load system for game state persistence. This enables `InteractionCount` and other deterministic state to be preserved across game sessions, which is **required** for Feature 14 (Deterministic Generation Seed) to achieve true cross-session determinism.

**Current State**: `InteractionContext.InteractionCount` exists but is session-only. Memory system has file-based persistence (`PersonaMemoryFileStore`), but there's no unified save/load system for game state including `InteractionCount`.

**Design Philosophy**: Keep it simple and abstracted. Use an adapter interface pattern (`IGameStatePersistence`) to allow multiple persistence implementations. Primary implementation uses SaveGameFree for Unity. The adapter pattern allows swapping implementations without breaking code if needed in the future.

**Architectural Impact**: Without this feature, Feature 14 cannot achieve cross-session determinism because `InteractionCount` resets on game restart. This is a **hard dependency** for Feature 14.

### Definition of Done

#### 16.1 Persistence Abstraction Interface
- [ ] Create `IGameStatePersistence` interface for save/load operations
- [ ] Define `GameStateData` DTO containing:
  - `Dictionary<string, int> InteractionCounts` (per NPC ID)
  - `Dictionary<string, object> CustomState` (for extensibility)
  - `string Version` (for migration)
  - `DateTime SaveTimestamp`
- [ ] Support async save/load operations
- [ ] Support error handling (save failures, corrupted files)

#### 16.2 Adapter Interface & Implementations
- [ ] **Core Interface**: `IGameStatePersistence` adapter interface
  - Defines contract for save/load operations
  - Allows swapping implementations without code changes
  - Supports async operations and error handling
- [ ] **Primary Implementation**:
  - [ ] `SaveGameFreePersistence` using SaveGameFree
    - Store `InteractionCounts` and game state using SaveGameFree API
    - Use SaveGameFree's built-in serialization and file management
    - Leverage SaveGameFree features (encryption, async/await, etc.) as configured
    - Handle SaveGameFree errors gracefully

#### 16.3 Integration with InteractionContext
- [ ] Create `GameStateManager` class that wraps persistence interface
- [ ] `SaveGameState()` method:
  - Collects `InteractionCount` from all active NPCs
  - Serializes to `GameStateData`
  - Calls persistence interface
- [ ] `LoadGameState()` method:
  - Loads `GameStateData` from persistence
  - Restores `InteractionCount` to NPC contexts
  - Returns loaded state for validation
- [ ] Integration point: `LlamaBrainAgent` or `PersonaMemoryStore` tracks InteractionCount per NPC

#### 16.4 Unity Integration
- [ ] Create `GameStatePersistenceManager` Unity MonoBehaviour component
  - Uses SaveGameFree for persistence
  - Auto-save on scene unload (optional)
  - Manual save/load methods
- [ ] Integration with Unity save system (if game has one)
- [ ] Editor tools for testing save/load (optional)

#### 16.5 Migration & Versioning
- [ ] `GameStateData.Version` field for save format versioning
- [ ] Basic migration support (upgrade old saves to new format)
- [ ] Backward compatibility: handle missing fields gracefully
- [ ] Migration path from no-save to save system (default InteractionCount = 0)

#### 16.6 Testing
- [ ] Unit tests for `IGameStatePersistence` interface
- [ ] Unit tests for `SaveGameFreePersistence` (mock SaveGameFree API)
- [ ] Integration tests: Save → Load → Verify InteractionCount restored
- [ ] Integration tests: Save → Modify → Load → Verify state restored
- [ ] Edge case tests: Corrupted saves, missing files, permission errors
- [ ] All tests in `LlamaBrain.Tests/Persistence/` passing

#### 16.7 Documentation
- [ ] Update `ARCHITECTURE.md` with persistence system section
- [ ] Document `IGameStatePersistence` adapter interface pattern
- [ ] Document SaveGameFree implementation
- [ ] Update `USAGE_GUIDE.md` with save/load examples
- [ ] Document SaveGameFree integration setup and configuration
- [ ] Document migration path for existing games
- [ ] Document how to swap persistence providers (adapter pattern benefits)

### Technical Considerations

**Adapter Interface Design**:
- `IGameStatePersistence` adapter interface provides clean abstraction
- SaveGameFree implementation implements the interface
- Adapter pattern allows for future extensibility if needed
- Runtime configuration of SaveGameFree settings

**SaveGameFree Implementation**:
- Uses SaveGameFree for Unity projects
- SaveGameFree provides production-ready features:
  - Encryption support
  - Async/await support for non-blocking operations
  - Multiple serialization formats (JSON, XML, Binary)
  - Cross-platform support
  - Auto-save functionality
  - Built-in error handling

**InteractionCount Tracking**:
- Must track `InteractionCount` per NPC ID
- Must persist before game closes
- Must restore on game load
- Default to 0 if not found in save (new game)

**Data Structure** (serialized by SaveGameFree):
```json
{
  "Version": "1.0",
  "SaveTimestamp": "2025-12-31T12:00:00Z",
  "InteractionCounts": {
    "npc_001": 5,
    "npc_002": 3,
    "wizard_001": 12
  },
  "CustomState": {}
}
```

**SaveGameFree Format**:
- Uses SaveGameFree's native serialization format
- Stores `GameStateData` object directly via SaveGameFree API
- SaveGameFree handles file management, encryption, and async operations as configured
- File location and naming managed by SaveGameFree settings

**Error Handling**:
- Save failures: Log error, continue (don't crash game)
- Load failures: Return default state (InteractionCount = 0), log warning
- Corrupted saves: Attempt recovery, fall back to defaults
- Permission errors: Log, continue with in-memory only

### Estimated Effort

**Total**: 4-6 days
- Feature 16.1-16.2 (Adapter Interface & Implementations): 2-3 days
  - Core interface and simple implementations: 1-2 days
  - Optional SaveGameFree adapter: 1 day
- Feature 16.3-16.4 (Integration): 1-2 days
- Feature 16.5-16.7 (Migration, Testing, Docs): 1 day

### Success Criteria

- [ ] `IGameStatePersistence` adapter interface defined and documented
- [ ] `SaveGameFreePersistence` implementation complete
- [ ] Adapter pattern allows runtime selection of persistence provider
- [ ] `InteractionCount` successfully saved and restored across sessions
- [ ] Integration with `LlamaBrainAgent` or memory system complete
- [ ] Unity component for easy save/load integration
- [ ] All tests passing
- [ ] Documentation complete with examples (including SaveGameFree setup)
- [ ] Feature 14 can use persisted `InteractionCount` for determinism

### Future Enhancements (Post-v0.2.0)

**Note**: Many of these features are available via SaveGameFree:
- ✅ Encrypted save files (via SaveGameFree)
- ✅ Async/await support (via SaveGameFree)
- ✅ Multiple serialization formats (via SaveGameFree)
- Additional enhancements:
- Save file compression
- Multiple save slots
- Save file validation and repair tools
- Performance: Incremental saves (only changed data)

---

<a id="feature-17"></a>
## Feature 17: Token Cost Tracking & Analytics

**Priority**: MEDIUM - Production monitoring and cost management  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Feature 6 (Controlled Memory Mutation), Feature 3 (State Snapshot)

### Overview

Implement token usage tracking and cost analytics to provide visibility into API costs and usage patterns. This enables production deployments to monitor costs, identify optimization opportunities, and set budget limits.

**Current State**: `CompletionMetrics` tracks token counts per request, but there's no aggregation, cost calculation, or analytics. No visibility into cumulative costs or usage patterns.

**Use Cases**:
- Track token usage per NPC, per interaction type, per session
- Calculate API costs (if using paid APIs)
- Identify high-cost interactions for optimization
- Set budget alerts and throttling
- Generate usage reports for analysis

### Definition of Done

#### 17.1 Token Usage Tracking
- [ ] Create `TokenUsageTracker` class for aggregating token metrics
- [ ] Track per-NPC token usage (prompt tokens, completion tokens, total)
- [ ] Track per-interaction-type usage (PlayerUtterance, ZoneTrigger, etc.)
- [ ] Track per-session cumulative usage
- [ ] Track per-day/week/month usage (time-based aggregation)
- [ ] Store usage data in memory with optional persistence

#### 17.2 Cost Calculation
- [ ] Create `CostCalculator` interface for cost calculation
- [ ] Support multiple pricing models:
  - Per-token pricing (OpenAI-style)
  - Per-request pricing
  - Tiered pricing
- [ ] Create `OpenAIPricingCalculator` implementation (example)
- [ ] Create `LlamaCppPricingCalculator` (free/local, but tracks for consistency)
- [ ] Configurable pricing per model/endpoint
- [ ] Support custom pricing calculators

#### 17.3 Analytics & Reporting
- [ ] Create `UsageAnalytics` class for generating reports
- [ ] Generate usage statistics:
  - Total tokens used (prompt + completion)
  - Average tokens per interaction
  - Peak usage periods
  - Most expensive NPCs/interactions
- [ ] Export usage data to CSV/JSON
- [ ] Generate summary reports (daily, weekly, monthly)
- [ ] Identify optimization opportunities (high-cost patterns)

#### 17.4 Budget Management
- [ ] Create `BudgetManager` class for budget tracking
- [ ] Support daily/weekly/monthly budget limits
- [ ] Budget alerts (warnings when approaching limits)
- [ ] Budget throttling (pause requests when limit exceeded)
- [ ] Configurable budget reset schedules

#### 17.5 Integration
- [ ] Integrate with `ApiClient` to capture token metrics
- [ ] Integrate with `LlamaBrainAgent` for per-NPC tracking
- [ ] Add usage metrics to `CompletionMetrics` events
- [ ] Optional Unity component for runtime monitoring
- [ ] Integration with existing metrics collection (RedRoom)

#### 17.6 Testing
- [ ] Unit tests for `TokenUsageTracker`
- [ ] Unit tests for `CostCalculator` implementations
- [ ] Unit tests for `UsageAnalytics`
- [ ] Unit tests for `BudgetManager`
- [ ] Integration tests: Verify tracking across multiple interactions
- [ ] All tests in `LlamaBrain.Tests/Analytics/` passing

#### 17.7 Documentation
- [ ] Update `ARCHITECTURE.md` with analytics system section
- [ ] Document cost calculation and pricing models
- [ ] Update `USAGE_GUIDE.md` with usage tracking examples
- [ ] Document budget management setup
- [ ] Add examples for custom pricing calculators

### Technical Considerations

**Token Tracking**:
- Capture from `CompletionMetrics` (already available)
- Aggregate in-memory with optional persistence
- Support reset/clear operations for testing

**Cost Calculation**:
- Abstract via `ICostCalculator` interface
- Default implementations for common pricing models
- Easy to add custom calculators for specific APIs

**Performance**:
- Lightweight tracking (minimal overhead)
- Optional persistence (don't block on I/O)
- Efficient aggregation (use dictionaries/counters)

**Privacy**:
- No sensitive data in usage tracking
- Optional anonymization for reporting
- Configurable data retention

### Estimated Effort

**Total**: 1-2 weeks
- Feature 17.1-17.2 (Tracking & Cost Calculation): 3-4 days
- Feature 17.3-17.4 (Analytics & Budget): 2-3 days
- Feature 17.5-17.7 (Integration, Testing, Docs): 2-3 days

### Success Criteria

- [ ] Token usage tracked per NPC, interaction type, and session
- [ ] Cost calculation working for multiple pricing models
- [ ] Usage analytics and reports generated correctly
- [ ] Budget management functional (alerts and throttling)
- [ ] Integration with existing metrics system complete
- [ ] All tests passing
- [ ] Documentation complete with examples

---

<a id="feature-18"></a>
## Feature 18: Concurrent Request Handling & Thread Safety

**Priority**: MEDIUM - Required for multi-NPC scenarios and production reliability  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Feature 15 (Multiple NPC Support), Feature 3 (State Snapshot)

### Overview

Ensure thread-safe concurrent request handling for multiple NPCs interacting simultaneously. This is required for Feature 15 (Multiple NPC Support) and production deployments where multiple NPCs may process requests in parallel.

**Current State**: `ApiClient` has basic rate limiting with `SemaphoreSlim`, but there's no comprehensive thread-safety documentation or testing. Memory system may have race conditions with concurrent access.

**Use Cases**:
- Multiple NPCs responding to player simultaneously
- Parallel NPC-to-NPC interactions
- Concurrent zone triggers
- Production deployments with high concurrency

### Definition of Done

#### 18.1 Thread Safety Audit
- [ ] Audit all core components for thread safety:
  - `AuthoritativeMemorySystem` (memory mutations)
  - `ContextRetrievalLayer` (memory queries)
  - `StateSnapshotBuilder` (snapshot creation)
  - `PromptAssembler` (prompt assembly)
  - `ValidationGate` (validation)
  - `MemoryMutationController` (mutation execution)
- [ ] Document thread-safety guarantees for each component
- [ ] Identify race conditions and shared state issues
- [ ] Create thread-safety documentation

#### 18.2 Concurrent Request Queue
- [ ] Create `ConcurrentRequestQueue` for managing parallel requests
- [ ] Support priority-based queuing (high-priority interactions first)
- [ ] Support request batching (group similar requests)
- [ ] Configurable max concurrent requests
- [ ] Request timeout handling
- [ ] Deadlock prevention

#### 18.3 Memory System Thread Safety
- [ ] Ensure `AuthoritativeMemorySystem` is thread-safe:
  - Use locks for mutation operations
  - Use concurrent collections for queries
  - Prevent race conditions on SequenceNumber counter
- [ ] Ensure `PersonaMemoryStore` is thread-safe
- [ ] Support concurrent reads (no locking required)
- [ ] Serialize writes (mutations must be atomic)

#### 18.4 Resource Pooling
- [ ] Create resource pools for expensive operations:
  - HTTP client pooling (reuse connections)
  - Memory allocation pooling
  - Snapshot builder pooling
- [ ] Configurable pool sizes
- [ ] Automatic cleanup of unused resources

#### 18.5 Deadlock Prevention
- [ ] Document lock ordering rules
- [ ] Implement lock timeout mechanisms
- [ ] Add deadlock detection (optional, for debugging)
- [ ] Ensure no circular lock dependencies

#### 18.6 Testing
- [ ] Unit tests for thread-safe components
- [ ] Concurrent stress tests (multiple threads accessing same components)
- [ ] Race condition tests (verify no data corruption)
- [ ] Deadlock tests (verify no deadlocks under load)
- [ ] Performance tests (verify minimal overhead from locking)
- [ ] All tests in `LlamaBrain.Tests/Concurrency/` passing

#### 18.7 Documentation
- [ ] Update `ARCHITECTURE.md` with thread-safety section
- [ ] Document thread-safety guarantees for each component
- [ ] Document lock ordering rules
- [ ] Update `USAGE_GUIDE.md` with concurrent usage examples
- [ ] Add best practices for multi-threaded usage

### Technical Considerations

**Thread Safety Strategy**:
- **Read operations**: Use concurrent collections (no locking)
- **Write operations**: Use locks (mutex/semaphore)
- **Immutable data**: `StateSnapshot` is immutable (safe to share)
- **Shared state**: Use locks or atomic operations

**Lock Granularity**:
- Fine-grained locks (per-NPC) when possible
- Coarse-grained locks (entire system) only when necessary
- Balance between safety and performance

**Concurrent Collections**:
- Use `ConcurrentDictionary` for memory lookups
- Use `ConcurrentQueue` for request queuing
- Minimize lock contention

**Performance**:
- Lock-free reads when possible
- Minimize lock hold time
- Use reader-writer locks for read-heavy workloads

### Estimated Effort

**Total**: 1-2 weeks
- Feature 18.1-18.2 (Audit & Queue): 2-3 days
- Feature 18.3-18.4 (Memory Safety & Pooling): 2-3 days
- Feature 18.5-18.7 (Deadlock Prevention, Testing, Docs): 2-3 days

### Success Criteria

- [ ] All core components thread-safe and documented
- [ ] Concurrent request queue functional
- [ ] Memory system handles concurrent access correctly
- [ ] Resource pooling implemented
- [ ] No race conditions or deadlocks under load
- [ ] Performance acceptable (minimal overhead from locking)
- [ ] All tests passing
- [ ] Documentation complete with thread-safety guarantees

---

<a id="feature-19"></a>
## Feature 19: Health Check & Resilience

**Priority**: MEDIUM - Production reliability and fault tolerance  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Feature 6 (Controlled Memory Mutation), Feature 3 (State Snapshot)

### Overview

Implement health checks, automatic recovery, and resilience patterns to ensure the system gracefully handles failures and maintains availability in production deployments.

**Current State**: Basic error handling exists in `ApiClient`, but there's no health monitoring, automatic recovery, or circuit breaker patterns. System may fail silently or crash on LLM server unavailability.

**Use Cases**:
- Monitor LLM server health
- Automatic server restart on failure
- Graceful degradation when LLM unavailable
- Circuit breaker to prevent cascading failures
- Production monitoring and alerting

### Definition of Done

#### 19.1 Health Check System
- [ ] Create `IHealthCheck` interface for health monitoring
- [ ] Create `HealthCheckService` for aggregating health checks
- [ ] Implement health checks for:
  - LLM server connectivity (`LlamaServerHealthCheck`)
  - Memory system availability (`MemorySystemHealthCheck`)
  - API client connectivity (`ApiClientHealthCheck`)
- [ ] Support health check endpoints (HTTP/Unity events)
- [ ] Health status reporting (Healthy, Degraded, Unhealthy)

#### 19.2 Automatic Recovery
- [ ] Create `RecoveryService` for automatic failure recovery
- [ ] Automatic LLM server restart on failure
- [ ] Automatic reconnection on network failure
- [ ] Retry logic with exponential backoff
- [ ] Configurable retry limits and timeouts
- [ ] Recovery event notifications

#### 19.3 Circuit Breaker Pattern
- [ ] Create `CircuitBreaker` class for failure protection
- [ ] Three states: Closed (normal), Open (failing), Half-Open (testing)
- [ ] Automatic state transitions based on failure rates
- [ ] Configurable failure thresholds
- [ ] Fast-fail when circuit is open (don't attempt requests)
- [ ] Automatic recovery attempts (half-open state)

#### 19.4 Graceful Degradation
- [ ] Create `DegradationStrategy` for handling unavailability
- [ ] Fallback to cached responses when LLM unavailable
- [ ] Fallback to deterministic responses (no LLM)
- [ ] Reduce functionality (disable features) rather than crash
- [ ] User-friendly error messages
- [ ] Configurable degradation levels

#### 19.5 Monitoring & Alerting
- [ ] Create `HealthMonitor` for continuous monitoring
- [ ] Track health metrics over time
- [ ] Alert on health status changes (optional Unity events)
- [ ] Health dashboard (optional Unity component)
- [ ] Integration with existing metrics (RedRoom)

#### 19.6 Integration
- [ ] Integrate health checks with `ApiClient`
- [ ] Integrate circuit breaker with request pipeline
- [ ] Integrate recovery service with `BrainServer` (Unity)
- [ ] Unity component for health monitoring UI
- [ ] Integration with existing error handling

#### 19.7 Testing
- [ ] Unit tests for health check system
- [ ] Unit tests for circuit breaker
- [ ] Unit tests for recovery service
- [ ] Integration tests: Verify recovery on server failure
- [ ] Integration tests: Verify circuit breaker behavior
- [ ] Stress tests: Verify system stability under failures
- [ ] All tests in `LlamaBrain.Tests/Resilience/` passing

#### 19.8 Documentation
- [ ] Update `ARCHITECTURE.md` with resilience patterns section
- [ ] Document health check system
- [ ] Document circuit breaker configuration
- [ ] Update `USAGE_GUIDE.md` with health monitoring examples
- [ ] Document graceful degradation strategies
- [ ] Troubleshooting guide for common failure scenarios

### Technical Considerations

**Health Check Design**:
- Lightweight checks (don't impact performance)
- Async health checks (don't block)
- Cached results (avoid excessive checking)
- Configurable check intervals

**Circuit Breaker Design**:
- Fast-fail when open (immediate response)
- Automatic recovery attempts (half-open)
- Configurable thresholds (failure rate, timeout)
- Per-endpoint circuit breakers (isolate failures)

**Recovery Strategy**:
- Exponential backoff (avoid thundering herd)
- Maximum retry limits (prevent infinite loops)
- Recovery notifications (log/events)
- Manual override (admin can force recovery)

**Graceful Degradation**:
- Tiered fallbacks (best to worst)
- User transparency (inform user of degraded mode)
- Feature flags (disable non-critical features)
- Maintain core functionality

### Estimated Effort

**Total**: 1-2 weeks
- Feature 19.1-19.2 (Health Checks & Recovery): 3-4 days
- Feature 19.3-19.4 (Circuit Breaker & Degradation): 2-3 days
- Feature 19.5-19.8 (Monitoring, Integration, Testing, Docs): 2-3 days

### Success Criteria

- [ ] Health check system functional and monitoring all components
- [ ] Automatic recovery working (server restart, reconnection)
- [ ] Circuit breaker preventing cascading failures
- [ ] Graceful degradation maintaining core functionality
- [ ] Monitoring and alerting operational
- [ ] Integration with existing systems complete
- [ ] All tests passing
- [ ] Documentation complete with resilience patterns

---

<a id="feature-20"></a>
## Feature 20: Memory Change History Visualization

**Priority**: LOW - Aspirational future enhancement  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Feature 8 (RedRoom Integration), Feature 2 (Structured Memory System)  
**Execution Order**: **Post-Milestone 4** - Future enhancement for RedRoom testing tools

### Overview

Add memory change history visualization to the RedRoom Memory Mutation Overlay. This enables developers to see before/after snapshots of memory state for each interaction, making it easier to debug memory mutations and understand how NPC memories evolve over time.

**Current State**: The Memory Mutation Overlay shows current memory state and mutation execution tracking, but does not show historical changes or diffs between interactions.

**Use Cases**:
- Debug memory mutations (see what changed between interactions)
- Understand memory evolution over time
- Verify memory authority enforcement (see blocked mutations)
- Track memory decay and pruning events

### Definition of Done

#### 20.1 Memory Snapshot System
- [ ] Create `MemorySnapshot` class to capture memory state at a point in time
- [ ] Capture all memory types: canonical facts, world state, episodic memories, beliefs
- [ ] Support snapshot comparison (diff generation)
- [ ] Store snapshots per interaction (configurable retention)
- [ ] Efficient serialization for storage

#### 20.2 Diff Generation
- [ ] Create `MemoryDiff` class for representing changes
- [ ] Detect added memories (new entries)
- [ ] Detect removed memories (deleted entries)
- [ ] Detect modified memories (updated fields)
- [ ] Highlight authority violations (blocked canonical fact mutations)
- [ ] Support diff visualization (color-coded changes)

#### 20.3 History Storage
- [ ] Create `MemoryHistoryStore` for managing snapshots
- [ ] Configurable retention policy (keep last N interactions)
- [ ] Efficient storage (only store deltas, not full snapshots)
- [ ] Support clearing history
- [ ] Optional persistence (save history to file)

#### 20.4 RedRoom Integration
- [ ] Extend `MemoryMutationOverlay` with history panel
- [ ] Display interaction timeline (list of interactions with snapshots)
- [ ] Show before/after comparison view
- [ ] Diff visualization (added=green, removed=red, modified=yellow)
- [ ] Navigation between historical snapshots
- [ ] Filter by memory type (show only episodic changes, etc.)

#### 20.5 Testing
- [ ] Unit tests for `MemorySnapshot` and `MemoryDiff`
- [ ] Unit tests for `MemoryHistoryStore`
- [ ] Integration tests: Verify snapshots captured correctly
- [ ] Integration tests: Verify diff generation accuracy
- [ ] All tests in `LlamaBrain.Tests/MemoryHistory/` passing

#### 20.6 Documentation
- [ ] Update RedRoom README with memory history feature
- [ ] Document snapshot retention policies
- [ ] Add examples showing how to use history for debugging
- [ ] Document performance considerations (snapshot overhead)

### Technical Considerations

**Performance**:
- Snapshot creation should be lightweight (<10ms)
- Use delta compression (only store changes, not full state)
- Configurable retention (default: last 50 interactions)
- Optional feature (can be disabled for production)

**Storage**:
- In-memory storage (default)
- Optional file persistence for debugging sessions
- Efficient serialization (JSON or binary)

**UI Design**:
- Side-by-side before/after view
- Color-coded diff (green=added, red=removed, yellow=modified)
- Collapsible sections by memory type
- Timeline navigation (previous/next interaction)

### Estimated Effort

**Total**: 1-2 weeks
- Feature 20.1-20.2 (Snapshot & Diff): 3-4 days
- Feature 20.3-20.4 (Storage & Integration): 3-4 days
- Feature 20.5-20.6 (Testing & Docs): 2-3 days

### Success Criteria

- [ ] Memory snapshots captured correctly for each interaction
- [ ] Diff generation accurately identifies all changes
- [ ] History visualization functional in RedRoom overlay
- [ ] Performance overhead acceptable (<10ms per snapshot)
- [ ] All tests passing
- [ ] Documentation complete with examples

**Note**: This is an aspirational future enhancement. It is not required for Milestone 4 or v0.2.0 release. It can be implemented post-Milestone 4 as a quality-of-life improvement for developers using RedRoom.

---

<a id="feature-21"></a>
## Feature 21: Sidecar Host

**Priority**: MEDIUM - Enables engine-agnostic deployment  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Feature 1 (Determinism Layer), Feature 2 (Structured Memory System), Feature 3 (Context Retrieval)  
**Execution Order**: **Post-Milestone 5** - Enables deployment flexibility for any game engine

### Overview

Implement a sidecar host architecture that runs LlamaBrain as a separate process/service alongside the game engine. The sidecar manages both the LlamaBrain API and the underlying llama.cpp server process, providing a unified service for AI operations. This enables engine-agnostic deployment where the game engine communicates with LlamaBrain via IPC (Inter-Process Communication) or network protocols, rather than embedding the library directly in the engine process.

**Current State**: LlamaBrain is embedded directly in Unity via the LlamaBrainRuntime package, and llama.cpp server management is handled by Unity components. This requires Unity-specific integration and limits deployment flexibility.

**Use Cases**:
- Deploy LlamaBrain as a separate service for any game engine (Unity, Unreal, Godot, custom engines)
- Isolate AI processing from game engine process (improved stability, resource management)
- Enable shared LlamaBrain instance across multiple game instances
- Support cloud/remote deployment of LlamaBrain service
- Enable hot-reloading of LlamaBrain without restarting game engine

### Definition of Done

#### 21.1 Sidecar Host Process
- [ ] Create `SidecarHost` executable/service application
- [ ] Implement process lifecycle management (start, stop, restart)
- [ ] Support configuration via JSON config file (`appsettings.json` format) or command-line arguments
- [ ] Implement graceful shutdown handling (wait for in-flight requests, cleanup resources)
- [ ] Support Windows, Linux, and macOS platforms
- [ ] Add logging and diagnostics infrastructure (structured logging with configurable levels)
- [ ] Default HTTP port: 8080 (configurable)
- [ ] Default WebSocket port: 8081 (configurable, or same as HTTP with upgrade)
- [ ] Use ASP.NET Core/Kestrel as HTTP server framework

#### 21.1.1 Llama.cpp Server Management
- [ ] Manage llama.cpp executable lifecycle (start, stop, restart, monitor)
- [ ] Wrap llama.cpp server process as managed subprocess
- [ ] Support configuration for llama.cpp:
  - [ ] Executable path (llama-server binary location)
  - [ ] Model path(s) and model selection
  - [ ] Server host/port (default: localhost:8080, configurable)
  - [ ] Context window, max tokens, sampling parameters
  - [ ] GPU/CPU configuration
- [ ] Implement llama.cpp process monitoring:
  - [ ] Health checks (ping llama.cpp health endpoint)
  - [ ] Automatic restart on crash or timeout
  - [ ] Resource monitoring (CPU, memory usage)
  - [ ] Log aggregation from llama.cpp process
- [ ] Support startup modes:
  - [ ] **Managed mode**: Sidecar spawns and manages llama.cpp process (default)
  - [ ] **External mode**: Sidecar connects to existing llama.cpp instance
- [ ] Handle llama.cpp process lifecycle:
  - [ ] Start llama.cpp on sidecar startup (if managed mode)
  - [ ] Wait for llama.cpp readiness before accepting requests
  - [ ] Graceful shutdown: Stop llama.cpp on sidecar shutdown
  - [ ] Emergency shutdown: Kill llama.cpp if unresponsive
- [ ] Proxy/forward requests to llama.cpp:
  - [ ] Wrap llama.cpp HTTP API calls
  - [ ] Maintain compatibility with llama.cpp API endpoints
  - [ ] Add request/response transformation if needed
  - [ ] Handle llama.cpp errors and timeouts
- [ ] Configuration integration:
  - [ ] Read llama.cpp settings from sidecar config file
  - [ ] Support environment variables for llama.cpp configuration
  - [ ] Validate llama.cpp executable and model files exist

#### 21.2 Communication Protocol (JSON)
- [ ] Design JSON-based protocol for game engine ↔ sidecar communication
- [ ] Implement JSON-RPC 2.0 over HTTP/WebSocket for request/response operations
- [ ] Define JSON schema for all request and response messages
- [ ] Support request/response pattern for synchronous operations
- [ ] Support event streaming for asynchronous notifications (WebSocket)
- [ ] Implement connection management and reconnection logic
- [ ] Add protocol versioning for backward compatibility
- [ ] Ensure JSON serialization/deserialization compatibility across platforms

#### 21.2.1 Input/Output DTOs
- [ ] Define input DTOs (Data Transfer Objects) for all API operations:
  - [ ] `SendPlayerInputRequest` - Player dialogue input with context
  - [ ] `SendWithSnapshotRequest` - Context-aware interaction with state snapshot
  - [ ] `GetMemoryStateRequest` - Memory query parameters
  - [ ] `UpdateMemoryRequest` - Memory mutation operations
  - [ ] `HealthCheckRequest` - Service health check
  - [ ] `BatchRequest` - Multiple operations in single request
  - [ ] `GetLlamaServerStatusRequest` - Get llama.cpp server status
  - [ ] `RestartLlamaServerRequest` - Restart llama.cpp server
  - [ ] `GetLlamaServerConfigRequest` - Get llama.cpp configuration
  - [ ] `UpdateLlamaServerConfigRequest` - Update llama.cpp configuration
- [ ] Define output DTOs (Data Transfer Objects) for all API responses:
  - [ ] `SendPlayerInputResponse` - NPC dialogue response with metadata
  - [ ] `SendWithSnapshotResponse` - Context-aware response with state changes
  - [ ] `GetMemoryStateResponse` - Memory state data
  - [ ] `UpdateMemoryResponse` - Memory mutation results
  - [ ] `HealthCheckResponse` - Service health status
  - [ ] `BatchResponse` - Results for batch operations
  - [ ] `ErrorResponse` - Standardized error format
  - [ ] `GetLlamaServerStatusResponse` - Llama.cpp server status (running, stopped, error)
  - [ ] `RestartLlamaServerResponse` - Restart operation result
  - [ ] `GetLlamaServerConfigResponse` - Current llama.cpp configuration
  - [ ] `UpdateLlamaServerConfigResponse` - Configuration update result
- [ ] Implement JSON serialization for all DTOs using `System.Text.Json` (default .NET JSON library)
- [ ] Add validation for DTO structure and required fields (use `System.ComponentModel.DataAnnotations` or FluentValidation)
- [ ] Support DTO versioning for schema evolution (include `Version` field in request/response headers or DTOs)
- [ ] Document DTO schemas with JSON Schema definitions (generate from C# types)
- [ ] Define error response structure: `ErrorResponse` with `Code`, `Message`, `Details`, `RequestId` fields

#### 21.3 API Surface
- [ ] Expose all core LlamaBrain operations via sidecar API using JSON DTOs:
  - [ ] `SendPlayerInput()` - Accepts `SendPlayerInputRequest`, returns `SendPlayerInputResponse`
  - [ ] `SendWithSnapshot()` - Accepts `SendWithSnapshotRequest`, returns `SendWithSnapshotResponse`
  - [ ] `GetMemoryState()` - Accepts `GetMemoryStateRequest`, returns `GetMemoryStateResponse`
  - [ ] `UpdateMemory()` - Accepts `UpdateMemoryRequest`, returns `UpdateMemoryResponse`
  - [ ] `HealthCheck()` - Accepts `HealthCheckRequest`, returns `HealthCheckResponse`
  - [ ] `Batch()` - Accepts `BatchRequest`, returns `BatchResponse`
- [ ] Expose llama.cpp management operations (optional, for administration):
  - [ ] `GetLlamaServerStatus()` - Get llama.cpp server status and health
  - [ ] `RestartLlamaServer()` - Restart llama.cpp server process
  - [ ] `GetLlamaServerConfig()` - Get current llama.cpp configuration
  - [ ] `UpdateLlamaServerConfig()` - Update llama.cpp configuration (requires restart)
- [ ] Implement request queuing and rate limiting:
  - [ ] Configurable rate limits (default: 100 requests/second per client)
  - [ ] Request queue with configurable max queue size (default: 1000)
  - [ ] Per-client rate limiting (track by client ID or IP)
- [ ] Support batch operations for efficiency:
  - [ ] Max batch size: 50 operations per `BatchRequest`
  - [ ] Batch operations execute in parallel where possible
- [ ] Add authentication/authorization (optional, for remote deployment):
  - [ ] API key authentication (simple header-based: `X-API-Key`)
  - [ ] Optional JWT token support for advanced scenarios
- [ ] Ensure all API methods use JSON DTOs for input/output
- [ ] Connection timeout: 30 seconds (configurable)
- [ ] Request timeout: 60 seconds (configurable, per operation)

#### 21.4 Client SDK
- [ ] Create client SDK library for game engines to communicate with sidecar
- [ ] Implement connection management (connect, disconnect, reconnect)
- [ ] Support async/await patterns matching core library API
- [ ] Provide Unity-compatible client (if needed)
- [ ] Provide Unreal-compatible client (C++ or Blueprint)
- [ ] Add error handling and retry logic:
  - [ ] Max retries: 3 (configurable)
  - [ ] Exponential backoff: 100ms, 200ms, 400ms
  - [ ] Retry on network errors, timeouts, 5xx server errors
  - [ ] Do not retry on 4xx client errors
- [ ] Support local and remote sidecar connections:
  - [ ] Local: `http://localhost:8080` (default)
  - [ ] Remote: Configurable endpoint URL
  - [ ] Connection string format: `http://hostname:port` or `ws://hostname:port`

#### 21.5 Integration & Migration
- [ ] Create migration guide from embedded to sidecar deployment
- [ ] Maintain backward compatibility with embedded Unity integration
- [ ] Provide example implementations for different engines
- [ ] Support hybrid deployment (embedded for development, sidecar for production)
- [ ] Add configuration toggles for deployment mode

#### 21.6 Testing
- [ ] Unit tests for sidecar host process
- [ ] Unit tests for llama.cpp process management (spawn, monitor, restart, shutdown)
- [ ] Unit tests for JSON communication protocol
- [ ] Unit tests for input/output DTOs (serialization, deserialization, validation)
- [ ] Unit tests for client SDK
- [ ] Integration tests: Game engine ↔ sidecar communication via JSON
- [ ] Integration tests: Sidecar ↔ llama.cpp communication and proxying
- [ ] Integration tests: Llama.cpp process lifecycle (start, health check, restart, stop)
- [ ] JSON schema validation tests for all DTOs
- [ ] Performance tests: Latency and throughput benchmarks
- [ ] Stress tests: Concurrent requests, connection failures, llama.cpp crashes
- [ ] All tests in `LlamaBrain.Tests/Sidecar/` passing

#### 21.7 Documentation
- [ ] Document sidecar architecture and deployment patterns
- [ ] Create setup guide for sidecar host installation
- [ ] Document llama.cpp integration and management:
  - [ ] Configuration options for llama.cpp
  - [ ] Managed vs external mode setup
  - [ ] Troubleshooting llama.cpp startup issues
  - [ ] Resource requirements and recommendations
- [ ] Document JSON communication protocol specification
- [ ] Document all input/output DTO schemas with JSON Schema definitions
- [ ] Provide DTO usage examples and validation rules
- [ ] Provide client SDK usage examples
- [ ] Add troubleshooting guide for common issues (sidecar and llama.cpp)
- [ ] Document performance considerations and optimization

### Technical Considerations

**Communication Protocol**:
- **JSON-RPC 2.0 over HTTP/WebSocket**: Primary protocol for all communication
  - JSON format for all messages (human-readable, easy to debug)
  - HTTP for synchronous request/response operations (default port: 8080)
  - WebSocket for event streaming and asynchronous notifications (default port: 8081, or HTTP upgrade)
  - JSON Schema definitions for all input/output DTOs
  - Type-safe DTOs with validation on both client and server
  - JSON serialization: `System.Text.Json` (default .NET library)
  - HTTP server: ASP.NET Core/Kestrel
  - Use HTTP for all standard operations; WebSocket optional for real-time event streaming
- **Future Considerations**: gRPC or custom binary protocol may be added later for performance optimization, but JSON remains the primary protocol

**Deployment Models**:
- **Local sidecar**: Sidecar runs on same machine as game engine (IPC or localhost)
  - Manages llama.cpp process locally
  - Low latency communication
- **Remote sidecar**: Sidecar runs on separate machine/container (network)
  - Manages llama.cpp process on remote machine
  - Network latency considerations
- **Shared sidecar**: Single sidecar instance serves multiple game instances
  - Single llama.cpp process shared across clients
  - Resource-efficient for multiple game instances

**Llama.cpp Integration**:
- Sidecar wraps and manages llama.cpp server process
- Sidecar acts as proxy/gateway between game engines and llama.cpp
- Supports both managed (sidecar spawns llama.cpp) and external (connects to existing) modes
- Automatic health monitoring and restart of llama.cpp process
- Unified configuration for both sidecar and llama.cpp settings

**Performance Targets**:
- IPC latency: <5ms for local sidecar
- Network latency: <50ms for remote sidecar (same network)
- Throughput: Support 100+ concurrent requests
- Memory overhead: <100MB for sidecar process

### Estimated Effort

**Total**: 5-7 weeks
- Feature 21.1 (Host Process & Llama.cpp Management): 1-2 weeks
- Feature 21.2 (Communication Protocol & DTOs): 1 week
- Feature 21.3-21.4 (API & Client SDK): 1-2 weeks
- Feature 21.5-21.6 (Integration & Testing): 1-2 weeks
- Feature 21.7 (Documentation): 3-5 days

### Success Criteria

- [ ] Sidecar host runs as standalone process/service
- [ ] Sidecar manages llama.cpp server process (spawn, monitor, restart)
- [ ] Llama.cpp integration functional (managed and external modes)
- [ ] Game engine can communicate with sidecar via JSON protocol (JSON-RPC 2.0)
- [ ] All input/output DTOs defined and documented with JSON Schema
- [ ] All core LlamaBrain operations accessible via sidecar API using JSON DTOs
- [ ] Client SDK provides seamless integration for game engines
- [ ] JSON serialization/deserialization works correctly across all platforms
- [ ] Performance overhead acceptable (<10ms additional latency for local sidecar)
- [ ] Backward compatibility maintained (embedded Unity integration still works)
- [ ] All tests passing (including DTO validation tests, llama.cpp management tests)
- [ ] Documentation complete with JSON protocol spec, DTO schemas, llama.cpp management guide, examples, and migration guide

---

<a id="feature-22"></a>
## Feature 22: Unreal Engine Support

**Priority**: MEDIUM - Expands engine support beyond Unity  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Feature 1 (Determinism Layer), Feature 2 (Structured Memory System), Feature 3 (Context Retrieval), **Feature 21 (Sidecar Host)** - Optional but recommended  
**Execution Order**: **Post-Milestone 5** - After core architecture is stable. Can leverage Feature 21 (Sidecar Host) for easier integration.

### Overview

Create Unreal Engine integration for LlamaBrain, enabling AI-powered NPCs and dialogue systems in Unreal Engine projects. This extends LlamaBrain support beyond Unity to the second major game engine, demonstrating the engine-agnostic nature of the core library.

**Current State**: LlamaBrain has Unity integration via LlamaBrainRuntime package. Unreal Engine support requires creating Unreal-specific adapters, components, and Blueprint integration.

**Use Cases**:
- AI-powered NPCs in Unreal Engine games
- Dynamic dialogue systems with persistent memory
- Integration with Unreal's gameplay systems (Gameplay Framework, Blueprints)
- Support for both C++ and Blueprint workflows
- Compatibility with Unreal's networking and multiplayer systems

### Definition of Done

#### 22.1 Unreal Plugin Structure
- [ ] Create Unreal Engine plugin structure (`LlamaBrainRuntime` plugin)
- [ ] Set up module definitions (.Build.cs files)
- [ ] Configure plugin dependencies (Core, Engine modules)
- [ ] Support Unreal Engine 5.0+ (LTS versions: 5.0, 5.1, 5.2, 5.3, 5.4)
- [ ] Create plugin descriptor (.uplugin file)
- [ ] Set up proper include paths and module organization
- [ ] C++ standard: C++17 (minimum, C++20 if engine version supports)
- [ ] Default integration approach: Sidecar Host (Feature 21) if available, fallback to direct integration

#### 22.2 Core Integration
- [ ] Create Unreal wrapper for `BrainAgent` (UObject-based)
- [ ] Create Unreal wrapper for `ApiClient` and server management
- [ ] Integrate with Unreal's async system (use `AsyncTask` or `FTaskGraph`)
- [ ] Create Unreal-compatible configuration system (USTRUCT-based)
- [ ] Integrate with Unreal's logging system (UE_LOG macros)
- [ ] Support Unreal's garbage collection system

#### 22.3 Actor Components
- [ ] Create `ULlamaBrainAgentComponent` (UActorComponent) for NPCs
- [ ] Create `UBrainServerComponent` for server management
- [ ] Support component initialization and lifecycle
- [ ] Integrate with Unreal's tick system (if needed)
- [ ] Support component replication for multiplayer (optional)

#### 22.4 Blueprint Integration
- [ ] Expose core functionality to Blueprints (UFUNCTION with BlueprintCallable)
- [ ] Create Blueprint nodes for common operations (naming convention: `LlamaBrain_<Operation>`):
  - [ ] `LlamaBrain_SendPlayerInput` - Send player input to NPC
  - [ ] `LlamaBrain_GetNPCResponse` - Get NPC response (async callback)
  - [ ] `LlamaBrain_UpdateMemory` - Update memory state
  - [ ] `LlamaBrain_CheckServerHealth` - Check server health
  - [ ] `LlamaBrain_GetMemoryState` - Query memory state
- [ ] Create Blueprint-friendly data structures (USTRUCT):
  - [ ] `FLlamaBrainResponse` - NPC response data
  - [ ] `FLlamaBrainMemoryState` - Memory state data
  - [ ] `FLlamaBrainConfig` - Configuration data
- [ ] Support Blueprint events for async callbacks (use `DECLARE_DYNAMIC_MULTICAST_DELEGATE`)
- [ ] Create example Blueprint implementations

#### 22.5 Configuration Assets
- [ ] Create `UPersonaConfig` (UObject/DataAsset) for persona profiles
- [ ] Create `UBrainSettings` (UObject/DataAsset) for server configuration
- [ ] Support Unreal's asset system (can be created in Content Browser)
- [ ] Create custom editor widgets for configuration (optional)
- [ ] Support asset references and dependencies

#### 22.6 Memory System Integration
- [ ] Create Unreal wrappers for memory system components
- [ ] Support Unreal's save/load system integration (optional):
  - [ ] Integrate with `USaveGame` system for persistence
  - [ ] Support save/load of memory state via Blueprint or C++
  - [ ] Optional: Auto-save memory state on game save
- [ ] Create Blueprint nodes for memory queries:
  - [ ] `LlamaBrain_QueryMemory` - Query episodic memories
  - [ ] `LlamaBrain_GetBeliefs` - Get NPC beliefs
  - [ ] `LlamaBrain_GetCanonicalFacts` - Get canonical facts
- [ ] Support memory visualization in editor (optional)

#### 22.7 RedRoom Integration (Optional)
- [ ] Port RedRoom testing suite to Unreal (if applicable)
- [ ] Create Unreal-specific RedRoom UI components
- [ ] Support Unreal's widget system for RedRoom overlays
- [ ] Adapt metrics collection for Unreal

#### 22.8 Testing
- [ ] Create Unreal test framework integration
- [ ] Unit tests for Unreal components
- [ ] Integration tests: Full pipeline in Unreal
- [ ] Blueprint functionality tests
- [ ] Performance tests: Ensure acceptable overhead
- [ ] All tests in `LlamaBrainRuntime/Tests/` passing

#### 22.9 Documentation
- [ ] Create Unreal-specific README and setup guide
- [ ] Document Blueprint usage and examples
- [ ] Create tutorial: "Creating Your First AI NPC in Unreal"
- [ ] Document C++ integration patterns
- [ ] Provide migration guide from Unity (if applicable)
- [ ] Document Unreal-specific considerations and best practices

#### 22.10 Samples & Examples
- [ ] Create sample Unreal project demonstrating LlamaBrain
- [ ] Example: Simple NPC dialogue system
- [ ] Example: Blueprint-only implementation
- [ ] Example: C++ integration pattern
- [ ] Example: Multiplayer considerations (if applicable)

### Technical Considerations

**Unreal Engine Version Support**:
- Target Unreal Engine 5.0+ (LTS versions)
- Support both C++ and Blueprint workflows
- Consider Blueprint-only users (minimize C++ requirements)

**Integration Approach**:
- **Option A**: Direct integration (embed LlamaBrain Core in Unreal plugin)
  - Pros: Lower latency, simpler deployment
  - Cons: Requires .NET runtime in Unreal process (use .NET hosting APIs: `hostfxr` or embed .NET runtime)
  - Implementation: Use .NET hosting APIs to load and call .NET assemblies from C++
  - **Default**: Not recommended unless sidecar unavailable
- **Option B**: Sidecar integration (use Feature 21 Sidecar Host) - **RECOMMENDED**
  - Pros: Cleaner separation, easier to maintain, works with any engine
  - Cons: Additional latency (~5-10ms), requires sidecar process
  - Implementation: Unreal C++ HTTP client communicating with sidecar via JSON-RPC
  - **Default**: Yes, use sidecar if Feature 21 is available
- **Recommendation**: Default to Option B (Sidecar), provide Option A as fallback

**C++ vs Blueprint**:
- Core functionality in C++ for performance
- Blueprint exposure for ease of use
- Support Blueprint-only workflows where possible

**Networking Considerations**:
- Support Unreal's replication system (if needed for multiplayer):
  - [ ] Memory state replication: Server-authoritative (server manages memory, replicates to clients)
  - [ ] NPC responses: Replicate from server to clients
  - [ ] Use `Replicated` properties for memory state (if applicable)
- Consider server-authoritative memory mutations:
  - [ ] All memory mutations must occur on server
  - [ ] Clients send mutation requests to server
  - [ ] Server validates and applies mutations
- Support client-side prediction (if applicable):
  - [ ] Optional: Predict NPC responses locally for responsiveness
  - [ ] Reconcile with server response when received

### Estimated Effort

**Total**: 6-8 weeks
- Feature 22.1-22.3 (Plugin Structure & Core Integration): 1-2 weeks
- Feature 22.4-22.5 (Blueprint & Configuration): 1-2 weeks
- Feature 22.6-22.7 (Memory & RedRoom): 1 week
- Feature 22.8-22.9 (Testing & Documentation): 1-2 weeks
- Feature 22.10 (Samples): 1 week

### Success Criteria

- [ ] Unreal plugin installs and loads correctly
- [ ] Core LlamaBrain functionality accessible from Unreal
- [ ] Blueprint integration functional (can create NPCs without C++)
- [ ] Actor components work correctly in Unreal projects
- [ ] Configuration assets can be created and used
- [ ] All tests passing
- [ ] Documentation complete with Unreal-specific guides
- [ ] Sample project demonstrates key features
- [ ] Performance acceptable (no significant overhead vs Unity integration)

**Note**: Feature 21 (Sidecar Host) is recommended but not strictly required. If implemented, Unreal integration can use the sidecar for cleaner separation. If not, direct integration will be required.

---

<a id="feature-24"></a>
## Feature 24: "I've seen this" Recognition

**Priority**: MEDIUM - Deterministic repetition recognition system  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Feature 3 (Context Retrieval Layer), Feature 4 (Ephemeral Working Memory), Feature 11 (RAG-Based Memory Retrieval) - Optional but recommended for topic recognition

### Overview

Add a deterministic pre-LLM recognition step that detects repetition (locations, topics, conversations) and influences the next generated line. Examples: NPC gets tired of seeing the same tunnel repeatedly, or gets tired of the player obsessively talking about manta rays.

This feature provides concrete proof that retrieval influences generation through deterministic repetition recognition. It serves as a memory proving system that demonstrates the effectiveness of the memory retrieval pipeline.

**Current State (Proven So Far)**:
- End-to-end generation works in Unity (interaction -> LLM -> subtitle output, ~263 ms success)
- Ephemeral episodic logging works (player prompt + NPC line recorded)
- Introspection overlay is live (agent identity, inference status, memory categories)

**Not Proven Yet** (This feature will prove):
- Retrieval influences generation (later output explicitly conditioned on earlier episodic state)
- Deterministic reconstruction (byte-stable prompt assembly, deterministic validation and mutation)
- Validation/gating behavior (rules executed, failures/retries, blocked outputs)
- Deterministic fallback path (forced failure -> stable fallback selection + logging)
- Memory lifecycle (guaranteed ephemeral across scene/domain resets)

### Definition of Done

#### 24.1 Repetition Event Recording
- [ ] Implement `EnteredLocation` episodic memory type (Tier A - ship now):
  - `Kind = EnteredLocation`
  - `LocationId` (author-tagged stable ID)
  - `TimeIndex` (monotonic tick, not wall clock)
- [ ] Implement `RepeatedTopic` episodic memory type (Tier B - after Tier A):
  - `Kind = RepeatedTopic`
  - `TopicId` (extracted from player input via semantic similarity or keyword matching)
  - `TopicText` (normalized topic text for matching)
  - `TimeIndex` (monotonic tick)
- [ ] Add deduplication logic to prevent duplicate events per tick
- [ ] Integrate event recording into interaction pipeline

#### 24.2 Deterministic Recognition Query System
- [ ] Create `RecognitionResult` DTO (pure DTO, no side effects):
  ```csharp
  public readonly record struct RecognitionResult(
    bool Recognized,
    RecognitionType Type, // Location, Topic, Conversation
    string RecognizedId, // LocationId or TopicId
    int RepeatCount,
    long LastOccurrenceTick,
    string EvidenceSummary
  );
  
  public enum RecognitionType
  {
    Location,
    Topic,
    Conversation
  }
  ```
- [ ] Implement location recognition query (on interaction trigger - location entry):
  - Query episodic memory for existing `LocationId` entries
  - If `LocationId` already exists in episodic memory: `Recognized = true`
  - Track `VisitCount`, `LastVisitTick`, `EvidenceSummary`
  - Return `RecognitionResult` with recognition status
- [ ] Implement topic recognition query (on interaction trigger - player input):
  - Query episodic memory for similar topics (using RAG semantic similarity or keyword matching)
  - If similar topic found with `RepeatCount >= threshold`: `Recognized = true`
  - Track `RepeatCount`, `LastMentionTick`, `TopicText`, `EvidenceSummary`
  - Return `RecognitionResult` with recognition status
- [ ] Ensure deterministic behavior: identical memory state + identical entry event → identical `RecognitionResult`

#### 24.3 Prompt Constraint Injection
- [ ] Implement prompt constraint injection for recognized repetitions (if `Recognized = true`):
  - **Location recognition**: Inject fixed-format `RECOGNITION` block into prompt assembly
    - Hard constraint: "include a brief familiarity cue in one clause; no fourth-wall break."
    - Evidence is optional flavor, not lore
    - This is the mechanism that proves retrieval influence
  - **Topic recognition**: Inject fixed-format `RECOGNITION` block into prompt assembly
    - Hard constraint format varies by recognition type (topic fatigue/redirect)
    - Evidence is optional flavor, not lore
- [ ] Integrate with `PromptAssembler` to inject recognition blocks
- [ ] Ensure byte-stable prompt assembly (deterministic reconstruction): identical recognition result → identical prompt text bytes

#### 24.4 Memory Mutation Tracking
- [ ] After generation, append repetition events to episodic memory:
  - Append `EnteredLocation` events (dedupe per tick)
  - Append `SpokeLine` with `RecognitionUsed=true/false` and `RecognitionType`
- [ ] Update `RepeatCount` for recognized items
- [ ] Track recognition usage in memory mutation results
- [ ] Ensure memory mutation is deterministic (identical recognition result → identical memory mutation)

#### 24.5 Validation & Gating
- [ ] Create validation rules to verify recognition cues in output:
  - Location recognition: Validator confirms familiarity cue present
  - Topic recognition: Validator confirms topic fatigue/redirect cue present
- [ ] Integrate with existing validation pipeline
- [ ] Test validation failures trigger retries
- [ ] Test deterministic fallback path (forced failure -> stable fallback selection + logging)

#### 24.6 Integration & Testing
- [ ] **Domain tests for location recognition**:
  - Two entries into same `LocationId` → `Recognized=true`, `RepeatCount=2`
- [ ] **Integration tests for location recognition**:
  - Second visit prompt bytes include `RECOGNITION` block
  - Validator confirms cue present
- [ ] **Determinism tests for location recognition**:
  - Identical memory state + identical entry event → identical `RecognitionResult` + identical prompt text bytes
- [ ] **Integration tests for topic recognition**:
  - Player mentions topic multiple times → `Recognized=true`, `RepeatCount>=threshold`, `Type=Topic`
  - Recognition prompts include appropriate `RECOGNITION` blocks
  - Validator confirms recognition cues present in output
- [ ] **Determinism tests for topic recognition**:
  - Identical memory state + identical topic mention → identical result
- [ ] Unit tests for `RecognitionResult` and recognition query logic
- [ ] Unit tests for prompt constraint injection
- [ ] All tests in `LlamaBrain.Tests/Recognition/` passing

#### 24.7 RedRoom Integration
- [ ] Update introspection overlay to show `RecognitionResult`:
  - Display `Recognized`, `Type`, `RepeatCount`, `RecognizedId`
  - Show `EvidenceSummary` when available
- [ ] Update overlay to show prompt section containing the `RECOGNITION` block
- [ ] Update overlay to show validation result "recognition cue present"
- [ ] Visual feedback for recognition events in memory mutation overlay

#### 24.8 On-Screen Demo Acceptance Criteria
- [ ] **Location Recognition Demo**:
  - Overlay shows `RecognitionResult(Recognized=true, Type=Location, RepeatCount>=2, ...)`
  - Overlay shows prompt section containing the `RECOGNITION` block
  - Overlay shows validation result "recognition cue present"
  - Subtitle on second visit contains a short familiarity clause ("been here before", "feels familiar", etc.)
- [ ] **Topic Recognition Demo**:
  - Overlay shows `RecognitionResult(Recognized=true, Type=Topic, RepeatCount>=threshold, ...)`
  - Overlay shows prompt section containing the `RECOGNITION` block
  - Overlay shows validation result "recognition cue present"
  - Subtitle contains topic fatigue/redirect cue

#### 24.9 Documentation
- [ ] Update `ARCHITECTURE.md` with recognition system section
- [ ] Document `RecognitionResult` and recognition types
- [ ] Document prompt constraint injection format
- [ ] Add examples showing recognition in action
- [ ] Update `USAGE_GUIDE.md` with recognition setup instructions
- [ ] Document memory proving approach and deterministic guarantees

### Technical Considerations

**Location Recognition (Tier A)**:
- Simple deterministic check: `LocationId` exists in episodic memory
- No semantic search required
- Can be implemented immediately without RAG dependencies

**Topic Recognition (Tier B)**:
- Requires semantic similarity or keyword matching
- Can use RAG from Feature 11 if available, or fallback to keyword matching
- Topic extraction from player input needs normalization

**Determinism Requirements**:
- Recognition queries must be byte-stable (identical memory state → identical result)
- Prompt assembly must be deterministic (identical recognition result → identical prompt bytes)
- Validation must be deterministic (identical prompt → identical validation result)

**Memory Lifecycle**:
- Ephemeral memory must be guaranteed across scene/domain resets
- Recognition events should not persist beyond session/episode boundaries
- Clear separation between episodic (temporary) and persistent memory

### Estimated Effort

**Total**: 2-3 weeks
- Feature 24.1-24.2 (Event Recording & Recognition Query): 3-5 days
- Feature 24.3-24.4 (Prompt Injection & Memory Mutation): 3-5 days
- Feature 24.5-24.6 (Validation & Testing): 3-5 days
- Feature 24.7-24.8 (RedRoom Integration & Demo): 2-3 days
- Feature 24.9 (Documentation): 1-2 days

### Success Criteria

- [ ] Location recognition system implemented and tested (Tier A)
- [ ] Topic recognition system implemented and tested (Tier B)
- [ ] Recognition queries return deterministic results for repeated locations and topics
- [ ] Recognition cues appear in generated output when repetition recognized
- [ ] Validation confirms recognition cues are present (both location and topic types)
- [ ] RedRoom overlay demonstrates recognition system working end-to-end
- [ ] All tests passing with deterministic guarantees
- [ ] Documentation complete with examples and usage guide

**Note**: This feature can be implemented independently of Feature 11 (RAG-Based Memory Retrieval), but topic recognition will benefit from semantic search capabilities. Location recognition (Tier A) requires no RAG dependencies and can be shipped immediately.

---

## 🔄 Iteration Strategy

Each feature follows this pattern:
1. **Build** - Implement core components
2. **Test** - Unit and integration tests
3. **Integrate** - Connect to existing system
4. **Validate** - RedRoom testing with metrics
5. **Document** - Update relevant documentation
6. **Review** - Code review and architecture validation

---

## ⚠️ Breaking Changes

### v0.3.0: Structured Output Migration

**Note on Roadmap**: v0.2.x uses **regex-based parsing** for extracting dialogue, mutations, and world intents from LLM output. v0.3.0 will introduce **LLM-native Structured Output** (JSON mode, function calling, schema-based outputs), which may require updates to custom parser logic.

**Impact**:
- Custom `OutputParser` implementations may need updates
- Validation rules that rely on regex patterns may need adjustment
- The `ParsedOutput` structure will remain compatible, but parsing internals will change

**Migration Path**:
- v0.2.x: Regex parsing (current, stable)
- v0.3.0: Structured output with regex fallback (automatic, backward compatible)
- Future: Structured output only (deprecation of regex mode)

**See**: Features 12 & 13 in the roadmap for detailed migration guide.

---

## 📝 Notes

### Design Decisions Made
- [x] Validation rule syntax/format - ScriptableObject-based rules with conditions
- [x] Memory decay algorithm - Significance-weighted decay with configurable rate
- [x] World intent schema - `WorldIntent` with IntentType, Target, Parameters, Priority, SourceText
- [x] Constraint escalation strategy - Configurable via `ConstraintEscalation` modes

### Risks & Mitigations
- **Risk**: Performance overhead from new layers
  - **Mitigation**: Benchmark after each feature, optimize hot paths
- **Risk**: Backward compatibility breaking
  - **Mitigation**: Maintain compatibility layer for existing PersonaMemoryStore
- **Risk**: Complexity overwhelming users
  - **Mitigation**: Provide sensible defaults, comprehensive tutorials

### Future Considerations (Beyond This Roadmap)
- Voice integration with validation
- Visual novel integration
- Multiplayer shared world state
- Advanced analytics dashboard for metrics
- Multi-agent conversations with shared memory (now Feature 15 in Milestone 5)

---

## Further Reading

- [README.md](../LlamaBrain/README.md) - Main library documentation and overview
- [ARCHITECTURE.md](ARCHITECTURE.md) - Complete architectural documentation
- [MEMORY.md](MEMORY.md) - Memory system documentation
- [PIPELINE_CONTRACT.md](PIPELINE_CONTRACT.md) - Formal pipeline contract
- [VALIDATION_GATING.md](VALIDATION_GATING.md) - Validation system documentation
- [USAGE_GUIDE.md](USAGE_GUIDE.md) - Practical examples and best practices
- [STATUS.md](STATUS.md) - Current implementation status
- [DETERMINISM_CONTRACT.md](DETERMINISM_CONTRACT.md) - Determinism contract and boundaries

---

**Next Review**: After Feature 8.4 completion (overlay persistence)
