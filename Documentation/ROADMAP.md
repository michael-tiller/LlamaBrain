# LlamaBrain Implementation Roadmap

**Goal**: Implement the complete "Continuity Emerges from Deterministic State Reconstruction Around a Stateless Generator" architectural pattern.

**Last Updated**: January 7, 2026

---

## Quick Links

- **Status**: See `STATUS.md` for current feature completion and next tasks
- **Determinism Contract**: See `DETERMINISM_CONTRACT.md` for explicit boundary statement and contract decisions
- **Feature 10 Test Gaps**: See `PHASE10_PROOF_GAPS.md` for detailed test backlog

---

## Progress Overview

| Features | Status | Priority |
|---------|--------|----------|
| [Feature 1: Determinism Layer](DEVELOPMENT_LOG.md#feature-1) | ✅ Complete | CRITICAL |
| [Feature 2: Structured Memory System](DEVELOPMENT_LOG.md#feature-2) | ✅ Complete | HIGH |
| [Feature 3: State Snapshot & Context Retrieval](DEVELOPMENT_LOG.md#feature-3) | ✅ Complete | HIGH |
| [Feature 4: Ephemeral Working Memory](DEVELOPMENT_LOG.md#feature-4) | ✅ Complete | MEDIUM |
| [Feature 5: Output Validation System](DEVELOPMENT_LOG.md#feature-5) | ✅ Complete | CRITICAL |
| [Feature 6: Controlled Memory Mutation](DEVELOPMENT_LOG.md#feature-6) | ✅ Complete | HIGH |
| [Feature 7: Enhanced Fallback System](DEVELOPMENT_LOG.md#feature-7) | ✅ Complete | MEDIUM |
| [Feature 8: RedRoom Integration](DEVELOPMENT_LOG.md#feature-8) | ✅ Complete | MEDIUM |
| [Feature 9: Documentation](DEVELOPMENT_LOG.md#feature-9) | ✅ Complete | MEDIUM |
| [Feature 10: Deterministic Proof Gap Testing](DEVELOPMENT_LOG.md#feature-10) | ✅ Complete | HIGH |
| [Feature 11: RAG-Based Memory Retrieval](#feature-11) | 📋 Planned | MEDIUM |
| [Feature 12: Dedicated Structured Output](DEVELOPMENT_LOG.md#feature-12) | ✅ Complete | HIGH |
| [Feature 13: Structured Output Integration](DEVELOPMENT_LOG.md#feature-13) | ✅ Complete | HIGH |
| [Feature 14: Deterministic Generation Seed](DEVELOPMENT_LOG.md#feature-14) | ✅ Complete | CRITICAL |
| [Feature 15: Multiple NPC Support](#feature-15) | 📋 Planned | MEDIUM |
| [Feature 16: Save/Load Game Integration](DEVELOPMENT_LOG.md#feature-16) | ✅ Complete | 100% |
| [Feature 17: Token Cost Tracking & Analytics](#feature-17) | 📋 Planned | MEDIUM |
| [Feature 18: Concurrent Request Handling & Thread Safety](#feature-18) | 📋 Planned | MEDIUM |
| [Feature 19: Health Check & Resilience](#feature-19) | 📋 Planned | MEDIUM |
| [Feature 20: Memory Change History Visualization](#feature-20) | 📋 Planned | LOW |
| [Feature 21: Sidecar Host](#feature-21) | 📋 Planned | MEDIUM |
| [Feature 22: Unreal Engine Support](#feature-22) | 📋 Planned | MEDIUM |
| [Feature 23: Structured Input/Context](DEVELOPMENT_LOG.md#feature-23) | ✅ Complete | HIGH |
| [Feature 24: "I've seen this" Recognition](#feature-24) | 📋 Planned | MEDIUM |
| [Feature 25: NLP Belief Contradiction Detection](#feature-25) | 📋 Planned | MEDIUM |
| [Feature 26: Narrative Consolidation](#feature-26) | 📋 Planned | MEDIUM |
| [Feature 27: Smart KV Cache Management](DEVELOPMENT_LOG.md#feature-27) | ✅ Complete | CRITICAL |
| [Feature 28: "Black Box" Audit Recorder](DEVELOPMENT_LOG.md#feature-28) | ✅ Complete | CRITICAL |
| [Feature 29: Prompt A/B Testing & Hot Reload](#feature-29) | 📋 Planned | MEDIUM |
| [Feature 30: Unity Repackaging & Distribution](#feature-30) | 📋 Planned | MEDIUM |
| [Feature 31: Whisper Speech-to-Text Integration](#feature-31) | 🚧 In Progress (~70%) | MEDIUM |
| [Feature 32: Piper Text-to-Speech Integration](#feature-32) | 🚧 In Progress (~65%) | MEDIUM |

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

### Phase 2: Persistence Layer ✅ COMPLETE
2. **Feature 16 (Save/Load Game Integration)** - ✅ **COMPLETE**
   - Engine-agnostic `ISaveSystem` interface in LlamaBrain core
   - Unity `SaveGameFreeSaveSystem` and `LlamaBrainSaveManager`
   - Memory snapshot builder/restorer for deterministic reconstruction
   - 33 tests passing

### Phase 3: Determinism Completion
3. **Feature 14 (Deterministic Generation Seed)** - **DO THIS THIRD**
   - Hook the persistence layer into the RNG to achieve cross-session determinism
   - Uses persisted `InteractionCount` from Feature 16
   - **Rationale**: This is the "Holy Grail" of AI consistency, but requires persistence to work

### Phase 4: Proof & Validation
4. **Feature 10 (Deterministic Proof Gap Testing)** - ✅ **COMPLETE**
   - All features, requirements, and tests implemented (351 tests total)
   - **Rationale**: Architecture can now claim "deterministically proven" at byte level. Required for v0.2.0.

### Phase 5: Production Performance & Operations (Critical for Production)
5. **Feature 27 (Smart KV Cache Management)** - ✅ **COMPLETE**
   - Static prefix policy enforced at `AfterCanonicalFacts` boundary
   - Cache-aware prompt assembly with `AssembleWithCacheInfo()`
   - Thread-safe metrics tracking (hit/miss rates, efficiency)
   - 42 tests passing (22 prefix enforcement + 20 cache metrics)
   - **Rationale**: Latency critical - difference between playable and unplayable game

6. **Feature 28 ("Black Box" Audit Recorder)** - ✅ **COMPLETE**
   - Production support tool leveraging determinism for bug reproduction
   - Ring buffer recording, debug package export/import, replay engine
   - Drift detection with step-through debugging
   - Unity integration: AuditRecorderBridge, RedRoomReplayController, ReplayProgressUI
   - 277 tests passing
   - **Rationale**: Ops critical - turns "He said/She said" into reproducible tickets

7. **Feature 29 (Prompt A/B Testing & Hot Reload)** - **DO NEXT**
   - Developer experience enhancement for rapid iteration
   - Enables live tuning of prompts and settings
   - **Rationale**: Developer experience - accelerates design iteration cycle

**Note**: Features 27 and 28 are COMPLETE and ready for production deployment. Feature 29 improves developer experience and can be done in parallel with voice features (31-32).

### Post-Milestone 5: Enhanced Features
8. **Milestone 6 Features (11, 15, 17, 18, 19, 24, 25, 26)** - **Only after Milestone 5 complete**
   - Feature 11: RAG-Based Memory Retrieval
   - Feature 15: Multiple NPC Support
   - Feature 17: Token Cost Tracking & Analytics
   - Feature 18: Concurrent Request Handling & Thread Safety
   - Feature 19: Health Check & Resilience
   - Feature 24: "I've seen this" Recognition
   - Feature 25: NLP Belief Contradiction Detection *(depends on Feature 11 embeddings)*
   - Feature 26: Narrative Consolidation
   - **Rationale**: These are enhancements that build on a stable foundation

**Key Principle**: Build the foundation (structured output) before building on top of it (persistence, determinism). Don't build persistence for data structures that will change.

**Note**: Milestone 4 (v0.2.0) is complete and ready for open source release. The execution order above applies to Milestone 5 (v0.3.0).

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

<a id="feature-25"></a>
## Feature 25: NLP Belief Contradiction Detection

**Priority**: MEDIUM - Fixes known gap in memory authority enforcement
**Status**: 📋 Planned (0% Complete)
**Dependencies**: Feature 2 (Structured Memory System), Feature 11 (RAG-Based Memory Retrieval - for embeddings)
**Execution Order**: **Milestone 6** - Enhances memory integrity through semantic analysis

### Overview

The current belief contradiction detection uses simple string matching, which fails to detect semantic contradictions. For example, if a canonical fact states "The king's name is Arthur" and the LLM generates a belief "The king is not named Arthur", the current system won't flag this as a contradiction because it only looks for exact string matches.

**The Problem**:
- `AuthoritativeMemorySystem.SetBelief()` has a basic contradiction check
- Current implementation uses simple string matching: `belief.Content.Contains(fact.Fact)`
- Test `GetActiveBeliefs_ExcludesContradicted` admits: *"Mark b1 as contradicted manually (since our simple check won't catch it)"*
- Semantic contradictions slip through, violating the "canonical facts are immutable truth" contract

**The Gap**:
- String matching: "The sky is blue" vs "The sky is green" - NOT detected as contradiction
- Negation patterns: "The king is Arthur" vs "The king is not Arthur" - NOT detected
- Paraphrasing: "Arthur rules the kingdom" vs "The kingdom has no ruler" - NOT detected

**The Solution**:
Implement NLP-based semantic contradiction detection using embeddings (from Feature 11) or a dedicated contradiction classifier. When a belief is added, check semantic similarity with canonical facts and flag contradictions based on meaning, not string matching.

**Use Cases**:
- Prevent NPCs from forming beliefs that contradict world lore
- Catch LLM hallucinations that contradict established facts
- Maintain narrative consistency across long play sessions
- Enable designers to trust that canonical facts are truly immutable

### Definition of Done

#### 25.1 Semantic Similarity Engine
- [ ] Create `SemanticContradictionDetector` service
- [ ] Integration with embedding model (from Feature 11's RAG infrastructure)
- [ ] Compute semantic similarity between belief content and canonical facts
- [ ] Configurable similarity threshold for contradiction detection (default: 0.8)
- [ ] Support for negation detection patterns (not, never, no longer, etc.)

#### 25.2 Contradiction Detection Algorithm
- [ ] Embedding-based approach: Compare belief embedding with fact embeddings
- [ ] Negation-aware: Detect when belief negates a fact even with high similarity
- [ ] Entailment classification: Use NLI (Natural Language Inference) model for precise detection
- [ ] Hybrid approach: Fast string matching + semantic fallback for edge cases
- [ ] Deterministic results: Same belief + facts = same contradiction decision

#### 25.3 Memory System Integration
- [ ] Update `AuthoritativeMemorySystem.SetBelief()` to use semantic detection
- [ ] Update `ValidationGate` to use semantic contradiction check (Gate 2)
- [ ] Add `ContradictionReason` field to explain why belief was flagged
- [ ] Support batch checking for multiple beliefs against all canonical facts
- [ ] Maintain backward compatibility with simple string matching (fallback mode)

#### 25.4 Configuration
- [ ] `ContradictionDetectionMode`: `Simple` (current), `Semantic`, `Hybrid`
- [ ] `SimilarityThreshold`: Configurable threshold for semantic detection (0.0-1.0)
- [ ] `EnableNegationDetection`: Toggle negation pattern matching
- [ ] `EnableEntailmentClassification`: Toggle NLI model usage
- [ ] Per-persona configuration overrides

#### 25.5 Testing
- [ ] Unit tests for `SemanticContradictionDetector`
- [ ] Test case: "The sky is blue" vs "The sky is green" → CONTRADICTION
- [ ] Test case: "The king is Arthur" vs "The king is not Arthur" → CONTRADICTION
- [ ] Test case: "Arthur rules" vs "The kingdom has no ruler" → CONTRADICTION
- [ ] Test case: "Arthur is wise" vs "Arthur is brave" → NOT contradiction
- [ ] Integration tests with `AuthoritativeMemorySystem`
- [ ] Integration tests with `ValidationGate`
- [ ] Determinism tests: Same inputs produce identical results
- [ ] Performance tests: Detection latency < 50ms

#### 25.6 Documentation
- [ ] Update `MEMORY.md` with semantic contradiction detection section
- [ ] Update `ARCHITECTURE.md` Claims-to-Tests mapping
- [ ] Document configuration options and tuning guidance
- [ ] Add examples of detected vs. missed contradictions
- [ ] Troubleshooting guide for false positives/negatives

### Technical Considerations

**Embedding Model**:
- Reuse embedding infrastructure from Feature 11 (RAG)
- Recommended: `all-MiniLM-L6-v2` (fast, good quality)
- Alternative: `text-embedding-ada-002` (OpenAI, higher quality)
- Local model preferred for offline/determinism requirements

**Detection Strategies**:

| Strategy | Pros | Cons |
|----------|------|------|
| **Embedding Similarity** | Fast, works well for paraphrasing | May miss negations |
| **Negation Patterns** | Catches "not", "never", etc. | Brittle, language-specific |
| **NLI Model** | Most accurate for entailment | Slower, requires separate model |
| **Hybrid** | Best of all approaches | Most complex to implement |

**Recommended Approach**: Start with Embedding Similarity + Negation Patterns (25.1 + 25.2), add NLI in future iteration if needed.

**Performance Requirements**:
- Detection latency: < 50ms per belief
- Batch detection: < 200ms for 10 beliefs against 100 facts
- Memory: Embeddings cached for all canonical facts (updated on fact addition)

**Determinism Requirements**:
- Embedding model must produce deterministic outputs (or use fixed seed)
- Contradiction decision must be reproducible for save/load compatibility
- Document any non-deterministic components and their impact

### Estimated Effort

**Total**: 1-2 weeks
- Feature 25.1 (Semantic Similarity Engine): 2-3 days
- Feature 25.2 (Contradiction Algorithm): 2-3 days
- Feature 25.3 (Memory System Integration): 2 days
- Feature 25.4-25.5 (Configuration & Testing): 2-3 days
- Feature 25.6 (Documentation): 1 day

### Success Criteria

- [ ] Semantic contradiction detection implemented and integrated
- [ ] Test case "The king is Arthur" vs "The king is not Arthur" passes
- [ ] Test case "The sky is blue" vs "The sky is green" passes
- [ ] `ValidationGate` uses semantic detection for Gate 2 (canonical contradiction check)
- [ ] Detection latency < 50ms per belief
- [ ] Configuration options documented and working
- [ ] All tests passing with deterministic guarantees
- [ ] Documentation complete with examples

**Note**: This feature directly addresses a known gap documented in ARCHITECTURE.md's "Claims to Tests Mapping" section. The current string-matching approach is acknowledged in test code as insufficient. Feature 25 closes this gap and enables the system to truly enforce canonical fact immutability.

---

<a id="feature-26"></a>
## Feature 26: Narrative Consolidation

**Priority**: MEDIUM - Transforms memory from FIFO buffer to knowledge graph  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Feature 2 (Structured Memory System), Feature 3 (Context Retrieval)  
**Execution Order**: **Post-Milestone 5** - Enhances long-term memory retention through compression rather than forgetting

### Overview

The current memory system uses decay and pruning to manage episodic memories, which results in **forgetting** older memories. However, real long-term memory involves **compression** rather than deletion. If a player plays for 100 hours, the NPC should remember a summary of the beginning, not forget it entirely.

**The Issue**: 
- `EphemeralWorkingMemory` truncates older memories based on character limits
- `AuthoritativeMemorySystem` supports decay, which causes forgetting
- **Decay = Forgetting**: Old episodic memories are lost when they decay below threshold

**The Gap**: 
- Real long-term memory involves compression, not deletion
- Players expect NPCs to remember the summary of early interactions even after 100 hours of gameplay
- Current system loses information permanently through decay/pruning

**The Solution**: 
A background consolidation job that compresses multiple episodic memories into summary memories or updates canonical facts. This transforms "Memory" from a FIFO buffer into a true knowledge graph where information is preserved in compressed form.

**Use Cases**:
- Long play sessions (100+ hours) where NPCs should remember early interactions in summary form
- Preserve narrative continuity across extended gameplay
- Transform episodic memories into persistent knowledge
- Reduce memory storage while maintaining narrative coherence

### Definition of Done

#### 26.1 Summary Memory Type
- [ ] Create `SummaryMemory` class extending `MemoryEntry`
- [ ] Support for compressed episodic memory summaries
- [ ] Track source episodic memories (references to original entries)
- [ ] Maintain temporal ordering (summary covers time range)
- [ ] Support significance weighting (important summaries retained longer)
- [ ] Integration with `AuthoritativeMemorySystem`

#### 26.2 Consolidation Algorithm
- [ ] Create `MemoryConsolidationService` for background consolidation
- [ ] Algorithm to select episodic memories for consolidation (e.g., oldest 10 entries)
- [ ] LLM-based summarization of selected memories into single summary
- [ ] Extract canonical facts from episodic memories (promote to `CanonicalFact` if appropriate)
- [ ] Preserve key details while compressing narrative
- [ ] Maintain determinism (same input memories → same summary)

#### 26.3 Consolidation Triggers
- [ ] Sleep/save trigger: Consolidate on game save or NPC sleep
- [ ] Threshold-based trigger: Consolidate when episodic memory count exceeds threshold
- [ ] Time-based trigger: Consolidate episodic memories older than X hours
- [ ] Manual trigger: API for explicit consolidation requests
- [ ] Configurable consolidation policies per persona

#### 26.4 Memory Lifecycle
- [ ] After consolidation: Remove original episodic memories (or mark as consolidated)
- [ ] Add summary memory to episodic memory list (or separate summary store)
- [ ] Update canonical facts if consolidation extracts immutable truths
- [ ] Preserve sequence numbers for deterministic ordering
- [ ] Maintain memory authority hierarchy (summaries respect authority rules)

#### 26.5 Context Retrieval Integration
- [ ] Update `ContextRetrievalLayer` to include summary memories in retrieval
- [ ] Score summaries based on relevance and temporal coverage
- [ ] Prefer detailed episodic memories over summaries when both available
- [ ] Include summaries when detailed memories are unavailable
- [ ] Support querying summaries for specific time periods

#### 26.6 Testing
- [ ] Unit tests for `MemoryConsolidationService`
- [ ] Unit tests for `SummaryMemory` type
- [ ] Integration tests: Verify consolidation preserves key information
- [ ] Integration tests: Verify consolidation reduces memory count
- [ ] Integration tests: Verify summaries appear in context retrieval
- [ ] Determinism tests: Verify same memories → same summary (byte-stable)
- [ ] All tests in `LlamaBrain.Tests/MemoryConsolidation/` passing

#### 26.7 Documentation
- [ ] Update `MEMORY.md` with narrative consolidation section
- [ ] Document consolidation algorithms and policies
- [ ] Update `ARCHITECTURE.md` with consolidation flow
- [ ] Document configuration options for consolidation triggers
- [ ] Add examples showing consolidation in action
- [ ] Troubleshooting guide for consolidation issues

### Technical Considerations

**Consolidation Strategy**:
- **Batch Size**: Consolidate 10 episodic memories at a time (configurable)
- **Selection Criteria**: Oldest memories first, or lowest significance first
- **Summarization**: Use LLM to generate concise summary preserving key narrative elements
- **Fact Extraction**: Identify immutable truths that should become canonical facts
- **Determinism**: Same input memories must produce identical summary (requires deterministic LLM seed)

**Memory Storage**:
- Summaries stored as `SummaryMemory` entries in episodic memory list
- Or separate `SummaryMemoryStore` for better organization
- Track which episodic memories were consolidated into each summary
- Preserve temporal ordering (summaries have time range)

**Performance**:
- Consolidation runs as background job (async, non-blocking)
- Triggered on save/sleep to avoid impacting gameplay
- Configurable batch size to balance memory reduction vs. processing time
- Cache consolidation results to avoid re-consolidating same memories

**Determinism Requirements**:
- Consolidation must be deterministic (same memories → same summary)
- Requires deterministic LLM seed (Feature 14) for consistent summarization
- Summary generation must be byte-stable for save/load compatibility

**Integration Points**:
- `AuthoritativeMemorySystem`: Add summary memory support
- `ContextRetrievalLayer`: Include summaries in retrieval
- `EphemeralWorkingMemory`: Summaries included in prompt assembly
- `MemoryMutationController`: Handle summary memory creation

### Estimated Effort

**Total**: 2-3 weeks
- Feature 26.1-26.2 (Summary Memory & Consolidation Algorithm): 1 week
- Feature 26.3-26.4 (Triggers & Lifecycle): 3-4 days
- Feature 26.5-26.6 (Integration & Testing): 3-4 days
- Feature 26.7 (Documentation): 2-3 days

### Success Criteria

- [ ] Summary memory type implemented and integrated with memory system
- [ ] Consolidation service compresses episodic memories into summaries
- [ ] Consolidation triggers working (sleep/save, threshold, time-based)
- [ ] Summaries included in context retrieval and prompt assembly
- [ ] Memory count reduced while preserving narrative information
- [ ] Consolidation is deterministic (same memories → same summary)
- [ ] All tests passing with deterministic guarantees
- [ ] Documentation complete with examples and configuration guide

**Note**: This feature transforms the memory system from a FIFO buffer (where old memories are forgotten) into a true knowledge graph (where old memories are compressed into summaries). This enables NPCs to maintain narrative continuity across extended gameplay sessions without losing early interaction context.

---


<a id="feature-29"></a>
## Feature 29: Prompt A/B Testing & Hot Reload

**Priority**: MEDIUM - Developer experience enhancement  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Feature 23 (Structured Input/Context), Feature 16 (Save/Load Game Integration)  
**Execution Order**: **Milestone 5** - Developer experience feature for rapid iteration

### Overview

Tuning the `SystemPrompt` or `Temperature` requires rebuilding or restarting the game. This slows down iteration for narrative designers who need to tweak NPC personality traits or generation parameters. A "Hot Reload" capability for `PersonaConfig` and `BrainSettings` allows changes to be applied immediately without restarting, enabling rapid A/B testing of prompt variations.

**The Problem**:
- Tuning `SystemPrompt` or `Temperature` requires rebuilding or restarting
- Narrative designers need rapid iteration on personality traits
- A/B testing prompt variations is slow and cumbersome
- No way to see changes immediately in running game

**The Solution**:
Implement hot reload capability for `PersonaConfig` and `BrainSettings` that allows changes to be applied immediately while the game is running. This enables rapid iteration and A/B testing of prompt variations.

**Use Cases**:
- Narrative designers tweaking "Grumpiness" trait in real-time
- A/B testing different `SystemPrompt` variations
- Adjusting `MaxTokens` or `Temperature` during gameplay
- Rapid iteration on personality configurations
- Live tuning of generation parameters

### Definition of Done

#### 29.1 Hot Reload Infrastructure
- [ ] Create `ConfigHotReloadService` for managing config changes
- [ ] Implement file watcher for `PersonaConfig` and `BrainSettings` files
- [ ] Support for both ScriptableObject (Unity) and JSON (standalone) configs
- [ ] Add validation for config changes (prevent invalid states)
- [ ] Implement safe reload with rollback on validation failure

#### 29.2 PersonaConfig Hot Reload
- [ ] Support hot reload of `PersonaConfig` changes
- [ ] Apply changes to `SystemPrompt`, personality traits, memory settings
- [ ] Preserve runtime state (don't reset memory or interaction count)
- [ ] Validate changes before applying (prevent breaking changes)
- [ ] Notify components of config changes (event system)

#### 29.3 BrainSettings Hot Reload
- [ ] Support hot reload of `BrainSettings` changes
- [ ] Apply changes to `Temperature`, `MaxTokens`, `TopP`, etc.
- [ ] Apply changes immediately to next interaction
- [ ] Validate parameter ranges (prevent invalid values)
- [ ] Support per-NPC settings override

#### 29.4 A/B Testing Support
- [ ] Implement A/B testing framework for prompt variations
- [ ] Support multiple `SystemPrompt` variants with traffic splitting
- [ ] Track metrics per variant (response quality, latency, etc.)
- [ ] Support gradual rollout (10% variant A, 90% variant B)
- [ ] Export A/B test results for analysis

#### 29.5 Integration & Testing
- [ ] Unit tests for `ConfigHotReloadService`
- [ ] Unit tests for config validation
- [ ] Integration tests: Verify hot reload applies changes correctly
- [ ] Integration tests: Verify A/B testing framework
- [ ] Performance tests: Verify hot reload doesn't impact gameplay
- [ ] All tests in `LlamaBrain.Tests/Config/HotReloadTests.cs` passing

#### 29.6 Documentation
- [ ] Update `ARCHITECTURE.md` with hot reload section
- [ ] Document hot reload workflow and best practices
- [ ] Update `USAGE_GUIDE.md` with A/B testing examples
- [ ] Document config file formats and validation rules
- [ ] Add troubleshooting guide for hot reload issues

### Technical Considerations

**Hot Reload Strategy**:
- **File Watcher**: Monitor config files for changes (Unity: AssetDatabase, Standalone: FileSystemWatcher)
- **Validation**: Validate changes before applying (prevent breaking states)
- **Rollback**: Revert to previous config if validation fails
- **State Preservation**: Don't reset runtime state (memory, interaction count)

**Config Change Application**:
- **PersonaConfig**: Apply to next interaction (don't affect current interaction)
- **BrainSettings**: Apply immediately to next API call
- **Event System**: Notify components of config changes
- **Thread Safety**: Ensure thread-safe config updates

**A/B Testing Framework**:
- **Variant Selection**: Random or deterministic (seed-based) selection
- **Traffic Splitting**: Configurable percentages (10% A, 90% B)
- **Metrics Tracking**: Track response quality, latency, user satisfaction per variant
- **Results Export**: JSON/CSV export for analysis

**Performance**:
- **Hot Reload**: < 50ms for config file change detection
- **Config Application**: < 10ms for applying changes
- **A/B Testing**: Negligible overhead (< 1ms per interaction)

**Integration Points**:
- `PersonaConfig`: Hot reload support
- `BrainSettings`: Hot reload support
- `BrainAgent`: Apply config changes to next interaction
- `RedRoom`: Visualize A/B test results

### Estimated Effort

**Total**: 1-2 weeks
- Feature 29.1-29.2 (Hot Reload Infrastructure & PersonaConfig): 4-5 days
- Feature 29.3-29.4 (BrainSettings & A/B Testing): 3-4 days
- Feature 29.5-29.6 (Integration & Documentation): 2-3 days

### Success Criteria

- [ ] Hot reload applies `PersonaConfig` changes without restart
- [ ] Hot reload applies `BrainSettings` changes without restart
- [ ] A/B testing framework supports multiple prompt variants
- [ ] Config validation prevents invalid states
- [ ] Hot reload performance meets targets (< 50ms detection, < 10ms application)
- [ ] Runtime state preserved during hot reload
- [ ] All tests passing with hot reload functionality
- [ ] Documentation complete with A/B testing guide

**Note**: This feature significantly improves developer experience by enabling rapid iteration on prompt tuning and personality configuration. Narrative designers can tweak traits and see changes immediately, accelerating the design iteration cycle.

---

<a id="feature-30"></a>
## Feature 30: Unity Repackaging & Distribution

**Priority**: MEDIUM - Improves Unity package distribution and developer experience  
**Status**: 📋 Planned (0% Complete)  
**Dependencies**: Feature 8 (RedRoom Integration), Feature 23 (Structured Input/Context)  
**Execution Order**: **Milestone 8** - Platform expansion and distribution improvements

### Overview

Improve Unity package distribution, versioning, and integration experience. The current Unity package (`LlamaBrainRuntime`) is distributed as a local package, but production-ready distribution requires automated packaging, version management, Git-based UPM support, and streamlined integration workflows.

**The Problem**:
- Unity runtime (`LlamaBrainRuntime`) is currently embedded in the main repository
- Manual package building and versioning is error-prone
- No automated package validation or consistency checks
- Limited distribution options (local package only)
- No Git-based UPM support for easy version management
- Package dependencies and compatibility not automatically validated
- No automated release packaging workflow
- Monorepo structure complicates Unity Package Manager distribution

**The Solution**:
Split the Unity runtime into its own dedicated repository and implement comprehensive Unity package repackaging system with automated build, validation, versioning, and distribution support. Enable Git-based UPM distribution, automated package validation, and streamlined release workflows.

**Use Cases**:
- Automated Unity package building from CI/CD
- Git-based UPM distribution for easy version management
- Automated package validation and consistency checks
- Streamlined release packaging workflows
- Better dependency management and compatibility validation
- Support for Unity Package Manager registry distribution

### Definition of Done

#### 30.0 Repository Migration (Do First)
- [ ] Create new dedicated repository for Unity runtime (e.g., `llamabrain-unity`)
- [ ] Migrate `LlamaBrainRuntime` codebase to new repository
- [ ] Set up repository structure following Unity Package Manager conventions
- [ ] Configure Git repository with proper `.gitignore` for Unity projects
- [ ] Set up CI/CD pipeline for the new repository
- [ ] Update main repository to reference Unity package via Git UPM
- [ ] Migrate Unity-specific documentation to new repository
- [ ] Update all cross-repository references and links
- [ ] Test repository migration (verify package can be installed from new repo)
- [ ] Document repository structure and migration rationale

#### 30.1 Automated Package Building
- [ ] Create package build script (PowerShell/Bash) for automated building
- [ ] Support for building from CI/CD pipelines
- [ ] Automated version injection from Git tags or version files
- [ ] Package structure validation (required files, correct paths)
- [ ] Automated package.json generation with correct dependencies
- [ ] Support for both development and release builds

#### 30.2 Package Validation
- [ ] Automated validation of package.json consistency
- [ ] Validate package structure (required directories, files)
- [ ] Validate Unity version compatibility
- [ ] Validate dependency versions and compatibility
- [ ] Check for missing or broken references
- [ ] Validate sample scenes and assets

#### 30.3 Git-Based UPM Support
- [ ] Support Git URL-based package installation
- [ ] Support version tags (e.g., `#v0.3.0`)
- [ ] Support branch-based installation (e.g., `#main`, `#develop`)
- [ ] Document Git UPM installation workflow
- [ ] Test Git UPM installation in clean Unity projects
- [ ] Support for private repository access (SSH/HTTPS)

#### 30.4 Version Management
- [ ] Automated version bumping from Git tags
- [ ] Semantic versioning support (major.minor.patch)
- [ ] Pre-release version support (e.g., `0.3.0-rc.1`)
- [ ] Version consistency across package.json, AssemblyInfo, and documentation
- [ ] Automated changelog generation from Git commits
- [ ] Support for version metadata (build number, commit hash)

#### 30.5 Distribution Workflow
- [ ] Automated release package creation
- [ ] Support for Unity Package Manager registry distribution
- [ ] Support for local package distribution (`.tgz` files)
- [ ] Automated release notes generation
- [ ] Package signing and integrity verification (optional)
- [ ] Support for multiple Unity version targets

#### 30.6 Documentation & Integration
- [ ] Update installation documentation with Git UPM instructions
- [ ] Document package build and release workflow
- [ ] Create developer guide for package maintenance
- [ ] Update CI/CD documentation with package build steps
- [ ] Document version management and release process
- [ ] Add troubleshooting guide for package installation issues

#### 30.7 Testing
- [ ] Unit tests for package build scripts
- [ ] Integration tests: Verify package builds correctly
- [ ] Integration tests: Verify Git UPM installation works
- [ ] Integration tests: Verify package validation catches errors
- [ ] Test package installation in clean Unity projects
- [ ] Test package upgrade scenarios (version migration)
- [ ] All tests in `LlamaBrain.Tests/Packaging/` passing

### Technical Considerations

**Repository Migration**:
- **Rationale**: Unity Package Manager works best with dedicated repositories for packages
- **Separation**: Core library (main repo) vs Unity runtime (dedicated repo)
- **Benefits**: Independent versioning, cleaner Git history, easier UPM distribution
- **Dependencies**: Unity package will reference core library (NuGet, Git submodule, or DLL)
- **Migration Strategy**: 
  - Create new repository with Unity package structure
  - Migrate `LlamaBrainRuntime` codebase
  - Update main repository to use Git UPM reference
  - Set up CI/CD for both repositories
  - Update documentation and cross-references

**Package Structure**:
- **Required Files**: `package.json`, `README.md`, `LICENSE.md`, core runtime files
- **Optional Files**: Samples, documentation, editor tools
- **Directory Structure**: Follow Unity Package Manager conventions
- **Asset Organization**: Proper folder structure for Unity import

**Version Management**:
- **Source of Truth**: Git tags for release versions
- **Version Format**: Semantic versioning (e.g., `0.3.0`, `0.3.0-rc.1`)
- **Consistency**: package.json, AssemblyInfo, documentation must match
- **Automation**: CI/CD automatically bumps versions from Git tags

**Git UPM Support**:
- **URL Format**: `https://github.com/user/repo.git#v0.3.0` or `git@github.com:user/repo.git#v0.3.0`
- **Version Tags**: Support for Git tags (e.g., `#v0.3.0`)
- **Branches**: Support for branch-based installation (e.g., `#main`)
- **Authentication**: Support for private repositories (SSH keys, personal access tokens)

**Package Validation**:
- **Structure Validation**: Check required files and directories exist
- **Dependency Validation**: Verify dependency versions are compatible
- **Unity Version**: Verify Unity version compatibility
- **Reference Validation**: Check for broken script references
- **Sample Validation**: Verify sample scenes load correctly

**Distribution Options**:
- **Git UPM**: Primary distribution method (Git URL-based)
- **Local Package**: `.tgz` file distribution for offline installation
- **Unity Registry**: Optional registry distribution (requires Unity account)
- **Asset Store**: Future consideration (not in scope for this feature)

**CI/CD Integration**:
- **Build Trigger**: On Git tag creation or manual trigger
- **Package Build**: Automated package building from source
- **Validation**: Automated package validation before release
- **Artifact**: Package artifact uploaded to release or artifact storage
- **Notification**: Optional notification on build completion

**Repository Structure**:
- **Main Repository**: Core LlamaBrain library (`.NET Standard 2.1`)
- **Unity Repository**: Dedicated repository for Unity runtime package
- **Separation**: Clean separation enables independent versioning and distribution
- **Dependencies**: Unity package references core library via NuGet or Git submodule
- **CI/CD**: Separate CI/CD pipelines for each repository

**Integration Points**:
- `package.json`: Unity package manifest (in Unity repository)
- `LlamaBrain.csproj`: Core library project file (in main repository)
- Unity runtime package: Complete package structure in dedicated repository
- CI/CD pipelines: Automated build and release workflows for both repositories
- Git repositories: Version tags and release management for both repositories
- Cross-repository references: Documentation and dependency management

### Estimated Effort

**Total**: 2-3 weeks
- Feature 30.0 (Repository Migration): 3-5 days
- Feature 30.1-30.2 (Package Building & Validation): 3-4 days
- Feature 30.3-30.4 (Git UPM & Version Management): 2-3 days
- Feature 30.5-30.6 (Distribution & Documentation): 2-3 days
- Feature 30.7 (Testing): 1-2 days

### Success Criteria

- [ ] Unity runtime successfully migrated to dedicated repository
- [ ] New repository structure follows Unity Package Manager conventions
- [ ] Main repository references Unity package via Git UPM
- [ ] Automated package building from CI/CD works correctly
- [ ] Package validation catches common errors and inconsistencies
- [ ] Git UPM installation works in clean Unity projects
- [ ] Version management is automated and consistent across repositories
- [ ] Release packaging workflow is streamlined and documented
- [ ] Package installation and upgrade scenarios work correctly
- [ ] All tests passing with package build and validation
- [ ] Documentation complete with installation and maintenance guides
- [ ] Cross-repository dependencies properly managed

**Note**: This feature improves the developer experience for both LlamaBrain maintainers and users. Splitting the Unity runtime into its own repository enables proper Git-based UPM distribution, independent versioning, and cleaner separation of concerns. Automated packaging reduces manual errors, Git UPM support enables easy version management, and streamlined workflows accelerate the release process. This complements Feature 21 (Sidecar Host) and Feature 22 (Unreal Engine Support) by ensuring Unity integration is production-ready with proper distribution infrastructure.

---

<a id="feature-31"></a>
## Feature 31: Whisper Speech-to-Text Integration

**Priority**: MEDIUM - Enhances player experience with voice input  
**Status**: 🚧 In Progress (~70% Complete)  
**Dependencies**: Unity Audio System, whisper.unity package

**Implementation Status**: Core components fully implemented (NpcVoiceInput: 338 lines, NpcVoiceController: 440 lines). Whisper integration complete with VAD, streaming transcription, events. Missing: platform testing, confidence thresholds, comprehensive docs.

### Overview

Integrate [whisper.unity](https://github.com/Macoron/whisper.unity) for local speech-to-text (STT) conversion, enabling players to speak to NPCs instead of typing. This complements Feature 32 (Chatterbox TTS) to create a complete voice conversation loop.

**Architecture Alignment**: 
- Local execution aligns with LlamaBrain's local-first approach
- Native Unity integration (C# package) - no external services required
- Text output feeds into existing `SendPlayerInputAsync()` pipeline
- Maintains deterministic validation boundary (STT output is validated like typed text)

### Definition of Done

#### 31.1 Package Integration
- [x] Add whisper.unity package to Unity project (via Git UPM or manual installation)
- [x] Configure WhisperManager in scene with appropriate model (tiny/small/medium)
- [ ] Set up GPU acceleration (Vulkan/Metal) if available  
- [ ] Configure model weights in StreamingAssets folder
- [ ] Test microphone permissions on target platforms (Windows, macOS, Linux, iOS, Android)

#### 31.2 Voice Input Component  
- [x] Create `NpcVoiceInput.cs` for voice input handling
- [x] Implement VAD-based always-listening pattern with silence detection
- [x] Add visual feedback via UnityEvents (OnListeningStarted/Stopped)
- [x] Implement recording start/stop with WhisperManager API
- [x] Handle transcription results via OnTranscriptionComplete event
- [x] Add toggle support (alwaysListening, enableTextFallback)

#### 31.3 Integration with LlamaBrain Pipeline
- [x] Transcribed text flows through existing `SendPlayerInputAsync()` method (via NpcVoiceController)
- [x] No changes required to core LlamaBrain validation or inference pipeline
- [x] Voice input treated identically to typed text (same validation, same constraints)
- [ ] Add transcription confidence threshold (filter low-confidence transcriptions)
- [x] Implement fallback to text input if STT fails or returns empty result

#### 31.4 Error Handling & Validation
- [ ] Handle microphone permission denials gracefully
- [ ] Handle transcription failures with user feedback
- [ ] Validate transcription quality (minimum length, confidence scores)
- [ ] Filter out nonsensical or very short transcriptions
- [ ] Add retry mechanism for failed transcriptions
- [ ] Log transcription attempts for debugging

#### 31.5 Platform Support
- [ ] Test on Windows (x86_64, optional Vulkan)
- [ ] Test on macOS (Intel and ARM, optional Metal)
- [ ] Test on Linux (x86_64, optional Vulkan)
- [ ] Handle platform-specific microphone permission requests

#### 31.6 Performance & Optimization
- [ ] Benchmark transcription latency (target: <500ms for real-time feel)
- [ ] Optimize model selection (tiny for speed, small/medium for accuracy)
- [ ] Implement transcription caching for repeated phrases (optional)
- [ ] Monitor memory usage with different model sizes
- [ ] Profile GPU vs CPU performance on target hardware

#### 31.7 Testing
- [ ] Unit tests for VoiceInputComponent transcription flow
- [ ] Integration tests with LlamaBrainAgent (voice → text → LLM → response)
- [ ] Test multilingual transcription (English, German, Spanish, etc.)
- [ ] Test with various audio quality levels (background noise, quiet speech)
- [ ] Test transcription accuracy with game-specific terminology
- [ ] RedRoom integration test with voice input scenarios

#### 31.8 Documentation
- [ ] Update USAGE_GUIDE.md with voice input setup instructions
- [ ] Document model selection tradeoffs (speed vs accuracy)
- [ ] Add voice input examples to samples
- [ ] Document microphone permission requirements per platform
- [ ] Update ARCHITECTURE.md with voice input flow diagram

### Integration Points

- **DialoguePanelController.cs**: Add voice input toggle and recording controls
- **LlamaBrainAgent.cs**: No changes required (uses existing `SendPlayerInputAsync()`)
- **WhisperManager**: Unity component from whisper.unity package
- **Unity Audio System**: Microphone input handling

### Estimated Effort

**Total**: 2-3 weeks
- Feature 31.1-31.2 (Package Integration & Component): 3-4 days
- Feature 31.3-31.4 (Pipeline Integration & Validation): 2-3 days
- Feature 31.5-31.6 (Platform Support & Performance): 3-4 days
- Feature 31.7-31.8 (Testing & Documentation): 2-3 days

### Success Criteria

- [ ] Players can speak to NPCs using microphone input
- [ ] Transcribed text flows seamlessly into existing dialogue pipeline
- [ ] Voice input works on all target platforms with proper permissions
- [ ] Transcription latency is acceptable for real-time conversation (<500ms)
- [ ] Fallback to text input works when STT fails
- [ ] Documentation complete with setup and troubleshooting guides
- [ ] All tests passing with voice input integration

**Note**: This feature enables natural voice conversations with NPCs. Whisper.unity provides local, privacy-preserving STT that aligns with LlamaBrain's architecture. The integration is non-invasive - transcribed text simply feeds into the existing validated text pipeline, maintaining all determinism and validation guarantees. This complements Feature 32 (Piper TTS) to create a complete voice conversation loop: Player speaks → Whisper STT → LlamaBrain → Piper TTS → NPC speaks.

---

<a id="feature-32"></a>
## Feature 32: Piper Text-to-Speech Integration

**Priority**: MEDIUM - Enhances NPC dialogue with voice output  
**Status**: 🚧 In Progress (~65% Complete)  
**Dependencies**: Unity Sentis, piper.unity (uPiper) package, .onnx voice models

**Implementation Status**: Core components fully implemented (NpcVoiceOutput: 498 lines, NpcSpeechConfig: 81 lines). Unity Sentis integration complete with phonemization (Japanese/English), async audio generation, events. Missing: audio caching, editor preview, platform testing, comprehensive docs.

### Overview

Integrate [piper.unity](https://github.com/Macoron/piper.unity) for local text-to-speech (TTS) conversion, enabling NPCs to speak their dialogue responses. This complements Feature 31 (Whisper STT) to create a complete voice conversation loop.

**Architecture Alignment**:
- Native Unity integration (C# package) - no external services required
- Local execution aligns with LlamaBrain's local-first approach
- Unity Sentis-based inference for high-performance TTS
- Multiple voice models per NPC enable unique voices
- Audio generation happens after text validation (maintains deterministic boundary)

### Definition of Done

#### 32.1 Package Integration
- [x] Add piper.unity (uPiper) package to Unity project (via Git UPM or manual installation)
- [x] Configure Unity Sentis for ONNX model inference (InferenceAudioGenerator)
- [ ] Set up PiperManager component in scene
- [ ] Download and configure .onnx voice models (e.g., `en_US-lessac-medium.onnx`)
- [ ] Place voice models in Assets folder (Unity Sentis auto-converts to model assets)
- [ ] Test TTS generation with sample text

#### 32.2 Unity Integration
- [x] Create `NpcVoiceOutput.cs` for TTS handling
- [x] Integrate uPiper API for audio generation (InferenceAudioGenerator, PhonemeEncoder)
- [x] Implement async audio generation with Unity Sentis
- [ ] Add audio caching system (cache generated audio by text + voice model hash)
- [x] Integrate with Unity AudioSource for playback
- [x] Add TTS enable/disable via NpcVoiceController component

#### 32.3 Voice Management
- [x] Create `NpcSpeechConfig` ScriptableObject for NPC voice configuration
- [x] Store voice model references per NPC (modelPath, pitch, rate, volume)
- [x] Add speech config field to NpcVoiceController component
- [x] Implement voice model validation and loading (LoadVoiceModelAsync)
- [ ] Support multiple voice models per NPC (different languages, styles)
- [ ] Add voice preview functionality in Unity editor
- [ ] Document voice model selection and acquisition (Hugging Face models)

#### 32.4 Integration with LlamaBrain Pipeline
- [x] Hook TTS generation after LlamaBrainAgent response via NpcVoiceController
- [x] Generate audio for validated responses (integrated via event-driven architecture)
- [x] Pass validated text to TTS service (maintains validation boundary)
- [x] Play audio via AudioSource component
- [x] Handle TTS generation failures (OnSpeakingFailed event, fallback to text)
- [x] Add TTS generation timeout via CancellationToken

#### 32.5 Text Processing & SSML Support
- [x] Clean text before TTS generation (phonemization pipeline handles this)
- [ ] Handle SSML tags if supported by voice models
- [x] Process punctuation for natural speech pauses (phonemization handles this)
- [x] Handle multilingual text (Japanese and English phonemizers implemented)
- [ ] Validate text length limits for TTS generation

#### 32.6 Audio Playback & Synchronization
- [ ] Integrate with Unity AudioSource for spatial audio (3D positioning)
- [ ] Synchronize audio playback with dialogue text display
- [ ] Add audio volume controls per NPC
- [ ] Implement audio fade-in/fade-out for smooth transitions
- [ ] Handle audio interruption (new dialogue cancels previous audio)
- [ ] Support subtitle display alongside audio

#### 32.7 Error Handling & Validation
- [ ] Validate voice model is loaded before generation
- [ ] Handle Unity Sentis inference failures gracefully
- [ ] Validate generated audio format and duration
- [ ] Verify audio matches text length (sanity check)
- [ ] Add retry mechanism for failed TTS generation
- [ ] Handle out-of-memory errors for large text inputs
- [ ] Log TTS generation attempts and failures

#### 32.8 Performance & Optimization
- [ ] Benchmark TTS generation latency (target: <500ms for real-time feel)
- [ ] Implement audio caching (cache by text + voice model hash)
- [ ] Optimize Unity Sentis inference settings
- [ ] Profile memory usage with different voice models
- [ ] Test with multiple concurrent TTS requests
- [ ] Optimize voice model loading (preload vs on-demand)
- [ ] Test performance on target platforms (Windows x86-64, others if supported)

#### 32.9 Testing
- [ ] Unit tests for PiperTTSComponent audio generation
- [ ] Integration tests with LlamaBrainAgent (text → TTS → audio playback)
- [ ] Test different voice models per NPC
- [ ] Test multilingual TTS (switch models based on language)
- [ ] Test audio caching (same text generates once, plays from cache)
- [ ] Test Unity Sentis inference failures (fallback to text-only)
- [ ] Test with various text lengths (short barks, long dialogue)
- [ ] RedRoom integration test with TTS-enabled NPCs

#### 32.10 Documentation
- [ ] Update USAGE_GUIDE.md with TTS setup instructions
- [ ] Document piper.unity package installation
- [ ] Document voice model acquisition and setup (Hugging Face models)
- [ ] Document voice profile creation and model assignment
- [ ] Add TTS examples to samples
- [ ] Document Unity Sentis requirements and configuration
- [ ] Update ARCHITECTURE.md with TTS integration flow
- [ ] Add troubleshooting guide for common TTS issues (model loading, Sentis errors)

### Integration Points

- **LlamaBrainAgent.cs**: Add TTS generation hook after `SendWithSnapshotAsync()` validation
- **PersonaConfig.cs**: Add optional VoiceProfile field
- **PiperTTSComponent.cs**: New Unity component for TTS generation (or extend LlamaBrainAgent)
- **PiperManager**: Unity component from piper.unity package
- **Unity Sentis**: ONNX model inference engine
- **Unity AudioSource**: Audio playback component

### Estimated Effort

**Total**: 2-3 weeks
- Feature 32.1-32.2 (Package Integration & Unity Integration): 2-3 days
- Feature 32.3-32.4 (Voice Management & Pipeline Integration): 2-3 days
- Feature 32.5-32.6 (Text Processing & Audio Playback): 2-3 days
- Feature 32.7-32.8 (Error Handling & Performance): 2-3 days
- Feature 32.9-32.10 (Testing & Documentation): 2-3 days

### Success Criteria

- [ ] NPCs can speak their dialogue responses using TTS
- [ ] Each NPC can use different voice models for unique voices
- [ ] TTS generation happens after text validation (maintains deterministic boundary)
- [ ] Audio playback is synchronized with text display
- [ ] Multilingual TTS works (switch models based on language)
- [ ] Audio caching reduces redundant generation
- [ ] Unity Sentis inference failures gracefully fall back to text-only
- [ ] Documentation complete with setup and troubleshooting guides
- [ ] All tests passing with TTS integration

**Note**: This feature brings NPCs to life with natural voice output. Piper.unity provides high-quality, local TTS with native Unity integration - perfect for seamless NPC voices. The Unity-native architecture eliminates external service dependencies and keeps everything local. TTS generation happens after text validation, maintaining LlamaBrain's deterministic validation boundary. This complements Feature 31 (Whisper STT) to create a complete voice conversation loop: Player speaks → Whisper STT → LlamaBrain → Piper TTS → NPC speaks.

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
