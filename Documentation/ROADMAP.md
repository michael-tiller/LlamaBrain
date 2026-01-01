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
| Feature 1: Determinism Layer | ✅ Complete | CRITICAL |
| Feature 2: Structured Memory System | ✅ Complete | HIGH |
| Feature 3: State Snapshot & Context Retrieval | ✅ Complete | HIGH |
| Feature 4: Ephemeral Working Memory | ✅ Complete | MEDIUM |
| Feature 5: Output Validation System | ✅ Complete | CRITICAL |
| Feature 6: Controlled Memory Mutation | ✅ Complete | HIGH |
| Feature 7: Enhanced Fallback System | ✅ Complete | MEDIUM |
| Feature 8: RedRoom Integration | 🚧 In Progress (99%) | MEDIUM |
| Feature 9: Documentation | ✅ Complete | MEDIUM |
| Feature 10: Deterministic Proof Gap Testing | 🚧 In Progress | HIGH |
| Feature 11: RAG-Based Memory Retrieval | 📋 Planned | MEDIUM |
| Feature 12: Dedicated Structured Output | 📋 Planned | HIGH |
| Feature 13: Structured Output Integration | 📋 Planned | HIGH |
| Feature 14: Deterministic Generation Seed | 📋 Planned | CRITICAL |
| Feature 15: Multiple NPC Support | 📋 Planned | MEDIUM |
| Feature 16: Save/Load Game Integration | 📋 Planned | CRITICAL |
| Feature 17: Token Cost Tracking & Analytics | 📋 Planned | MEDIUM |
| Feature 18: Concurrent Request Handling & Thread Safety | 📋 Planned | MEDIUM |
| Feature 19: Health Check & Resilience | 📋 Planned | MEDIUM |

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
4. **Feature 10 (Deterministic Proof Gap Testing)** - **Must be completed for Milestone 5**
   - Should be ongoing throughout all phases
   - Critical tests can be written as features are implemented
   - **Must be finished** before Milestone 5 is considered complete
   - **Rationale**: Proves determinism claims, required for v1.0. Cannot claim "deterministically proven" architecture without this.

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

## ✅ Feature 1: Determinism Layer & Expectancy Engine

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

## ✅ Feature 2: Structured Memory System

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

## ✅ Feature 3: State Snapshot & Context Retrieval

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

## ✅ Feature 4: Ephemeral Working Memory

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

## ✅ Feature 5: Output Validation System

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

## ✅ Feature 6: Controlled Memory Mutation

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

## ✅ Feature 7: Enhanced Fallback System

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

## 🚧 Feature 8: RedRoom Integration

**Priority**: MEDIUM - Enables testing of new architecture  
**Status**: 🚧 In Progress (99% Complete - overlay fixes needed)  
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

- [x] **Overlay System Infrastructure** - Complete (fixes needed)
  - [x] Extend `RedRoomCanvas` with input managed overlay panels (F2 + F3 toggles implemented)
  - [x] Auto-refresh event system (polling-based, 0.3-0.5s interval)
  - [ ] **Overlay fixes needed** - Bug fixes and improvements required

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

**Estimated Effort**: ~1 day remaining (overlay fixes and improvements)

---

## ✅ Feature 9: Documentation & Polish

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

## 🚧 Feature 10: Deterministic Proof Gap Testing

**Priority**: HIGH - Required for v1.0 release  
**Status**: 🚧 In Progress (~25% Complete)  
**Dependencies**: Features 1-7 (All core components must be implemented)  
**Execution Order**: **MUST BE COMPLETED** for Milestone 4. Should be ongoing throughout all phases, but must be finished before Milestone 4 is considered complete. Cannot claim "deterministically proven" architecture without this.

**Note**: See `PHASE10_PROOF_GAPS.md` for detailed test backlog with file targets and acceptance criteria.

### Progress Summary

#### Critical Requirements Implemented (4/5)
- [x] **Requirement #1**: Strict Total Order Sort Algorithm - Updated ContextRetrievalLayer with LINQ OrderBy/ThenBy chains
- [x] **Requirement #2**: SequenceNumber Field - Added to MemoryEntry base class, monotonic counter in AuthoritativeMemorySystem
- [x] **Requirement #3**: Score vs Tie-Breaker Logic - Implemented with ordinal Id comparison and SequenceNumber tie-breaker
- [x] **Requirement #4**: OutputParser Normalization Contract - Added `NormalizeWhitespace()` static method with 21 tests
- [ ] **Requirement #5**: WorldIntentDispatcher Singleton Lifecycle - Pending

#### High-Leverage Determinism Tests Added (7 tests)
- [x] Dictionary order tripwire test
- [x] Near-equal floating score tie-breaker test
- [x] SequenceNumber tie-breaker test
- [x] SequenceNumber persistence test
- [x] Belief determinism test
- [x] Ordinal string comparison test
- [x] All weights zero fallback test

### Definition of Done

- [ ] ContextRetrievalLayer selection behavior tests (7/20-25 tests complete)
- [ ] PromptAssembler hard bounds & truncation priority tests (0/25-30 tests)
- [x] OutputParser mutation extraction & malformed handling tests (21/20-25 tests)
- [ ] ValidationGate ordering & gate execution tests (0/30-35 tests)
- [ ] MemoryMutationController authority enforcement tests (0/30-35 tests)
- [ ] WorldIntentDispatcher pure dispatcher behavior tests (0/20-25 PlayMode tests)
- [ ] Full pipeline determinism & policy integration tests (0/15-20 tests)
- [ ] All Critical Implementation Requirements met (4/5 complete, see `PHASE10_PROOF_GAPS.md`)

**Estimated Effort**: 9-13 days total
- Feature 10.1-10.5 (Unit tests): 5-7 days
- Feature 10.6 (PlayMode tests): 2-3 days
- Feature 10.7 (Integration tests): 2-3 days

**Versioning Strategy**: Feature 10 is required for v1.0 release. Pre-1.0 releases (rc/preview) can ship without Feature 10 complete, but must not claim architecture is "deterministically proven" until Feature 10 is complete.

---

## 📋 Feature 11: RAG-Based Memory Retrieval & Memory Proving

**Priority**: MEDIUM - Enhancement to existing retrieval system  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Feature 3 (Context Retrieval Layer), Phase 10 (Deterministic Proof Gap Testing)

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
- Feature 11.1-11.2 (Embedding & Storage): 1 week
- Feature 11.3-11.4 (Retrieval & Integration): 1 week
- Feature 11.5-11.6 (Optimization & Testing): 3-5 days
- Feature 11.7 (Memory Proving): 3-5 days
- Feature 11.8 (Documentation): 2-3 days

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

## 📋 Feature 12: Dedicated Structured Output

**Priority**: HIGH - Improves reliability and determinism of output parsing  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Feature 5 (Output Validation System), Feature 10 (Deterministic Proof Gap Testing)  
**Execution Order**: **DO THIS FIRST** - Fundamentally changes how data enters the pipeline. Must be completed before Feature 16 (Save/Load) to avoid rework.

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
- Feature 12.1-12.2 (Interfaces & Schema): 1 week
- Feature 12.3-12.4 (Integration & Refactoring): 1 week
- Feature 12.5-12.6 (Testing & Docs): 3-5 days

### Success Criteria

- [ ] Structured output parsing eliminates regex parsing errors
- [ ] 100% success rate on valid structured outputs (vs ~95% with regex)
- [ ] Backward compatibility maintained (regex fallback works)
- [ ] All existing tests pass with structured output enabled
- [ ] Performance improvement over regex parsing

---

## 📋 Feature 13: Structured Output Integration

**Priority**: HIGH - Completes structured output migration  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Feature 12 (Dedicated Structured Output)  
**Execution Order**: **DO IMMEDIATELY AFTER Feature 12** - Completes the structured output migration. Must be done before Feature 16 (Save/Load) to ensure data structures are stable.

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
- Feature 13.1-13.3 (Pipeline Integration): 1 week
- Feature 13.4-13.7 (Error Handling, Testing, Docs): 3-5 days

### Success Criteria

- [ ] All mutation types work correctly with structured output
- [ ] Validation pipeline fully integrated with structured outputs
- [ ] Error handling robust with automatic fallback
- [ ] 100% backward compatibility maintained
- [ ] All existing tests pass with structured output enabled
- [ ] Performance equal or better than regex parsing

---

## 📋 Feature 14: Deterministic Generation Seed

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

## 📋 Feature 15: Multiple NPC Support

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

## 📋 Feature 16: Save/Load Game Integration

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

### Future Enhancements (Post-v1.0)

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

## 📋 Feature 17: Token Cost Tracking & Analytics

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

## 📋 Feature 18: Concurrent Request Handling & Thread Safety

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

## 📋 Feature 19: Health Check & Resilience

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

## 📋 Feature 20: Memory Change History Visualization

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

**Note**: This is an aspirational future enhancement. It is not required for Milestone 4 or v1.0 release. It can be implemented post-Milestone 4 as a quality-of-life improvement for developers using RedRoom.

---

## 🎯 Milestones

### Milestone 1: Core Architecture (Features 1-3) ✅
**Target**: Weeks 1-3  
**Status**: ✅ Complete
- [x] Determinism layer functional
- [x] Structured memory system working
- [x] State snapshot and retry logic operational

### Milestone 2: Validation & Control (Features 4-6) ✅
**Target**: Weeks 4-6  
**Status**: ✅ Complete
- [x] Output validation gate functional
- [x] Working memory system operational
- [x] Memory mutation control enforced

### Milestone 3: Integration & Polish (Features 7-9) 🚧
**Target**: Weeks 7-8  
**Status**: 🚧 Partial
- [x] Fallback system complete
- [x] RedRoom fully integrated (99% complete - overlay fixes needed)
- [x] Documentation comprehensive (100% complete)

### Milestone 4: v0.2.0 - The Foundation Update (Features 1-9, 10 partial) ✅
**Target**: v0.2.0-rc.1  
**Status**: ✅ Complete (Ready for Open Source Release)

**What's Included in v0.2.0:**
- ✅ Feature 1: Determinism Layer & Expectancy Engine
- ✅ Feature 2: Structured Memory System
- ✅ Feature 3: State Snapshot & Context Retrieval
- ✅ Feature 4: Ephemeral Working Memory
- ✅ Feature 5: Output Validation System
- ✅ Feature 6: Controlled Memory Mutation
- ✅ Feature 7: Enhanced Fallback System
- ✅ Feature 8: RedRoom Integration (99% - overlay fixes in progress)
- ✅ Feature 9: Documentation (100% complete)
- 🚧 Feature 10: Deterministic Proof Gap Testing (~65% complete - ongoing)

**Completion Criteria**:
- [x] Core architecture complete (Features 1-6)
- [x] Integration & polish complete (Features 7-9)
- [x] Comprehensive documentation
- [x] 92.37% test coverage with 1,531 passing tests
- [x] Ready for v0.2.0-rc.1 open source release

**Note**: v0.2.0 uses **regex-based parsing** for output extraction. v0.3.0 will introduce LLM-native Structured Output (JSON), which may require updates to custom parser logic. See "Breaking Changes" section below.

### Milestone 5: v0.3.0 - The Production Update (Features 12, 13, 14, 16, 10 completion) 🚧
**Target**: v0.3.0  
**Status**: 🚧 Planned  
**Prerequisite**: **Milestone 4 (v0.2.0) must be released** before starting Milestone 5 features.

**Recommended Execution Order** (see "Recommended Execution Order" section above):
1. **Feature 12 & 13** (Structured Output) - Do first
2. **Feature 16** (Save/Load) - Do second
3. **Feature 14** (Deterministic Seed) - Do third
4. **Feature 10** (Proof Gap Testing) - **Must be completed** (ongoing throughout, but finish before Milestone 5 complete)

**Status**:
- Feature 10: Deterministic Proof Gap Testing - 🚧 ~65% Complete (**MUST BE COMPLETED** for Milestone 5)
- Feature 12: Dedicated Structured Output - 📋 Planned (HIGH priority - **DO FIRST**)
- Feature 13: Structured Output Integration - 📋 Planned (HIGH priority - **DO IMMEDIATELY AFTER 12**)
- Feature 16: Save/Load Game Integration - 📋 Planned (CRITICAL priority - **DO SECOND**, after 12 & 13)
- Feature 14: Deterministic Generation Seed - 📋 Planned (CRITICAL priority - **DO THIRD**, after 16)

**Completion Criteria**:
- [ ] **Feature 10 complete** - All deterministic proof gap tests passing (REQUIRED)
- [ ] Feature 12 & 13 complete - Structured Output migration
- [ ] Feature 16 complete - Save/Load persistence
- [ ] Feature 14 complete - Cross-session determinism
- [ ] All tests passing
- [ ] Performance benchmarks met
- [ ] Documentation updated with breaking changes
- [ ] Ready for v0.3.0 release

**Note**: Feature 10 is **required** for Milestone 5 completion. The architecture cannot claim to be "deterministically proven" without completing Feature 10's test suite.

### Milestone 6: Enhanced Features (Features 11, 15, 17, 18, 19, 20) 📋
**Target**: Post-v0.3.0  
**Status**: 📋 Planned  
**Prerequisite**: **Milestone 5 (v0.3.0) must be complete** before starting Milestone 6 features. These are enhancements that build on a stable foundation.
- Feature 11: RAG-Based Memory Retrieval & Memory Proving - 📋 Planned (MEDIUM priority)
- Feature 15: Multiple NPC Support - 📋 Planned (MEDIUM priority)
- Feature 17: Token Cost Tracking & Analytics - 📋 Planned (MEDIUM priority)
- Feature 18: Concurrent Request Handling & Thread Safety - 📋 Planned (MEDIUM priority)
- Feature 19: Health Check & Resilience - 📋 Planned (MEDIUM priority)
- Feature 20: Memory Change History Visualization - 📋 Planned (LOW priority - aspirational)
- [ ] RAG-based retrieval with embeddings
- [ ] Vector storage and indexing
- [ ] Memory proving through repetition recognition
- [ ] Multi-NPC conversation support
- [ ] Shared memory systems
- [ ] NPC-to-NPC interaction capabilities
- [ ] Token usage tracking and cost analytics
- [ ] Thread-safe concurrent request handling
- [ ] Health monitoring and automatic recovery
- [ ] Circuit breaker and graceful degradation
- [ ] Memory change history (before/after snapshots, diff view) - Future enhancement

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
