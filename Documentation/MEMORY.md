# LlamaBrain Memory System

**"Authoritative Memory with Deterministic Ordering"**

This document provides comprehensive documentation of the LlamaBrain memory system, which maintains authoritative, structured memory that LLMs cannot corrupt. The memory system enforces strict authority boundaries and provides deterministic ordering for reproducible behavior.

## Overview

The LlamaBrain memory system is **Component 3** of the nine-component architecture. It serves as the authoritative source of truth for all NPC knowledge, ensuring that:

1. **Immutable truths cannot be corrupted** - Canonical facts are protected from any modification
2. **Memory has clear authority hierarchy** - Higher authority memories cannot be overridden by lower authority sources
3. **Deterministic ordering** - All memories are assigned sequence numbers for byte-stable retrieval
4. **Automatic decay and pruning** - Episodic memories decay over time and are pruned when limits are exceeded
5. **Context-aware retrieval** - Intelligent retrieval based on recency, relevance, and significance

The memory system is **external to the LLM** - the LLM has no direct access to modify memory. All changes must pass through the validation gate (Component 7) and mutation controller (Component 8).

## Architecture Integration

The memory system integrates with the broader LlamaBrain architecture:

```
Interaction Context (Component 1)
    ↓
Expectancy Engine (Component 2) → Generates constraints
    ↓
Memory System (Component 3) → Retrieves relevant context
    ↓
State Snapshot (Component 4) → Captures immutable context
    ↓
Ephemeral Working Memory (Component 5) → Creates bounded prompt
    ↓
LLM Inference (Component 6) → Generates untrusted output
    ↓
Validation Gate (Component 7) → Validates output
    ↓
Mutation Controller (Component 8) → Executes validated mutations
    ↓
Memory System (Component 3) → Updates memory
```

**Key Principle**: The LLM never directly modifies memory. All mutations must be:
1. Proposed in LLM output
2. Validated by the validation gate
3. Executed by the mutation controller
4. Applied with proper authority checks

## Memory Types and Authority Hierarchy

The memory system maintains four distinct memory types, each with a specific authority level:

### 1. Canonical Facts (Highest Authority - 100)

**Purpose**: Immutable world truths defined by game designers.

**Properties**:
- **Immutable**: Cannot be modified or removed once created
- **Cannot be contradicted**: Any attempt to contradict canonical facts is blocked
- **Designer-only creation**: Only `MutationSource.Designer` can create canonical facts
- **Always included**: Canonical facts are always included in context retrieval (unless explicitly filtered)

**Examples**:
- "The king's name is Arthur"
- "Magic exists in this world"
- "Dragons breathe fire"
- "The capital city is Camelot"

**Implementation**:
```csharp
var memorySystem = memoryStore.GetOrCreateSystem("wizard_001");

// Add canonical fact (immutable)
var result = memorySystem.AddCanonicalFact(
    "king_name", 
    "The king is named Arthur", 
    "world_lore"
);

// Attempting to modify will fail
var modifyResult = memorySystem.AddCanonicalFact("king_name", "The king is named Bob", "world_lore");
// modifyResult.Success == false
```

**Authority**: `MemoryAuthority.Canonical` (100)  
**Required Source**: `MutationSource.Designer` (100)

### 2. World State (High Authority - 75)

**Purpose**: Mutable game state that can change but requires validation.

**Properties**:
- **Mutable**: Can be updated by game systems
- **Requires GameSystem authority**: Only `MutationSource.GameSystem` or `MutationSource.Designer` can modify
- **Tracked modifications**: Maintains modification count and timestamp
- **Always included**: World state is always included in context retrieval

**Examples**:
- "door_castle_main: open"
- "player_gold: 150"
- "CurrentWeather: Stormy"
- "quest_dragon_slayer: active"

**Implementation**:
```csharp
// Set world state (requires GameSystem authority)
var result = memorySystem.SetWorldState(
    "door_castle_main", 
    "open", 
    MutationSource.GameSystem
);

// Update world state
var updateResult = memorySystem.SetWorldState(
    "door_castle_main", 
    "closed", 
    MutationSource.GameSystem
);

// Attempting to modify with ValidatedOutput will fail
var invalidResult = memorySystem.SetWorldState(
    "door_castle_main", 
    "locked", 
    MutationSource.ValidatedOutput
);
// invalidResult.Success == false
```

**Authority**: `MemoryAuthority.WorldState` (75)  
**Required Source**: `MutationSource.GameSystem` (75) or higher

### 3. Episodic Memory (Medium Authority - 50)

**Purpose**: Conversation history and event memories that may decay over time.

**Properties**:
- **Append-only**: Can be added but not arbitrarily modified
- **Automatic decay**: Memories decay over time based on significance
- **Significance-based retention**: Higher significance memories decay slower
- **Automatic pruning**: Old memories are pruned when count exceeds `MaxEpisodicMemories`
- **Retrieved by relevance**: Only relevant memories are included in prompts

**Episode Types**:
- `Dialogue` - Conversation exchange with player
- `Observation` - Observation of player action
- `Thought` - Internal thought or reaction
- `Event` - Significant event that occurred
- `LearnedInfo` - Information learned from conversation

**Examples**:
- "Player said hello"
- "We discussed the weather"
- "Player gave me a gift"
- "I witnessed a battle in the town square"

**Implementation**:
```csharp
// Add episodic memory with significance
var entry = new EpisodicMemoryEntry(
    "Player saved the village", 
    EpisodeType.MajorEvent
) {
    Significance = 1.0f  // High significance = slower decay
};

var result = memorySystem.AddEpisodicMemory(entry, MutationSource.ValidatedOutput);

// Add dialogue exchange
var dialogueResult = memorySystem.AddDialogue(
    "Player", 
    "I need your help!", 
    significance: 0.8f
);

// Get active memories (strength > 0.1)
var activeMemories = memorySystem.GetActiveEpisodicMemories();

// Apply decay (typically called periodically)
memorySystem.ApplyEpisodicDecay();
```

**Decay Mechanism**:
- Memories start with `Strength = 1.0`
- Each decay cycle reduces strength by `EpisodicDecayRate * (1.0 - Significance * 0.5)`
- Higher significance memories decay slower
- Memories with `Strength <= 0` are removed
- Memories with `Strength <= 0.1` are considered inactive (not included in retrieval)

**Authority**: `MemoryAuthority.Episodic` (50)  
**Required Source**: `MutationSource.ValidatedOutput` (50) or higher

### 4. Belief Memory (Lowest Authority - 25)

**Purpose**: NPC opinions, relationships, and beliefs that can be wrong or change.

**Properties**:
- **Mutable**: Can be updated or contradicted
- **Can be wrong**: Beliefs may be incorrect
- **Confidence-based**: Each belief has a confidence level (0.0 to 1.0)
- **Contradiction detection**: Automatically marked as contradicted if they conflict with canonical facts
- **Filtered by confidence**: Low-confidence beliefs may be excluded from retrieval
- **Sentiment tracking**: Relationship beliefs track sentiment (-1.0 to 1.0)

**Belief Types**:
- `Opinion` - Opinion about a person/entity
- `Relationship` - Relationship status with someone
- `Belief` - Belief about a fact (may be wrong)
- `Assumption` - Assumption made by the NPC
- `Preference` - Preference or like/dislike

**Examples**:
- "I think the player is trustworthy" (Opinion, confidence: 0.8)
- "I believe the treasure is in the cave" (Belief, confidence: 0.6)
- "The merchant is my friend" (Relationship, sentiment: 0.7)
- "I dislike spicy food" (Preference, sentiment: -0.3)

**Implementation**:
```csharp
// Create opinion belief
var opinion = BeliefMemoryEntry.CreateOpinion(
    "player", 
    "is a hero", 
    sentiment: 0.9f, 
    confidence: 0.95f
);
memorySystem.SetBelief("hero_opinion", opinion, MutationSource.ValidatedOutput);

// Create relationship belief
var relationship = BeliefMemoryEntry.CreateRelationship(
    "merchant_001", 
    "is a trusted business partner", 
    sentiment: 0.8f
);
memorySystem.SetBelief("rel_merchant_001", relationship, MutationSource.ValidatedOutput);

// Create fact belief (may be wrong)
var factBelief = BeliefMemoryEntry.CreateBelief(
    "treasure_location", 
    "the treasure is in the cave", 
    confidence: 0.6f,
    evidence: "I heard rumors"
);
memorySystem.SetBelief("treasure_belief", factBelief, MutationSource.ValidatedOutput);

// Get beliefs about a subject
var playerBeliefs = memorySystem.GetBeliefsAbout("player");

// Get active (non-contradicted) beliefs
var activeBeliefs = memorySystem.GetActiveBeliefs();
```

**Contradiction Detection**:
- When a belief is set, the system checks if it contradicts any canonical facts
- Contradicted beliefs are automatically marked with `IsContradicted = true`
- Contradicted beliefs have their confidence reduced to max(0.2, original confidence)
- Contradicted beliefs are excluded from retrieval by default (unless `IncludeContradictedBeliefs = true`)

**Authority**: `MemoryAuthority.Belief` (25)  
**Required Source**: `MutationSource.ValidatedOutput` (50) or higher

## Authority Enforcement

The memory system enforces strict authority boundaries to prevent corruption:

### Authority Hierarchy

```
MemoryAuthority.Canonical (100)     ← Highest authority
MemoryAuthority.WorldState (75)
MemoryAuthority.Episodic (50)
MemoryAuthority.Belief (25)         ← Lowest authority
```

### Mutation Source Hierarchy

```
MutationSource.Designer (100)        ← Highest authority
MutationSource.GameSystem (75)
MutationSource.ValidatedOutput (50)
MutationSource.LlmSuggestion (25)   ← Lowest authority (not used for mutations)
```

### Authority Rules

1. **Canonical Facts**:
   - Can only be created by `MutationSource.Designer`
   - Cannot be modified by any source
   - Cannot be removed
   - Any attempt to contradict canonical facts is blocked

2. **World State**:
   - Can be modified by `MutationSource.GameSystem` or `MutationSource.Designer`
   - Cannot be modified by `MutationSource.ValidatedOutput` or lower

3. **Episodic Memory**:
   - Can be added by `MutationSource.ValidatedOutput` or higher
   - Cannot be modified after creation (append-only)
   - Can decay and be pruned automatically

4. **Beliefs**:
   - Can be created/updated by `MutationSource.ValidatedOutput` or higher
   - Can be contradicted by canonical facts
   - Can be wrong (that's their purpose)

### Validation Logic

```csharp
public bool ValidateMutation(MemoryAuthority targetAuthority, MutationSource source)
{
    return (int)source >= (int)targetAuthority;
}
```

**Example**:
- Attempting to modify world state (75) with `ValidatedOutput` (50) → **FAILS**
- Attempting to add episodic memory (50) with `ValidatedOutput` (50) → **SUCCEEDS**
- Attempting to modify canonical fact (100) with `GameSystem` (75) → **FAILS**

## Deterministic Ordering

The memory system ensures deterministic ordering for reproducible behavior:

### Sequence Numbers

Every memory entry is assigned a **monotonically increasing sequence number** at insertion time:

```csharp
// Sequence numbers are assigned automatically
entry.SequenceNumber = _nextSequenceNumber++;
```

**Properties**:
- **Monotonic**: Always increases, never decreases
- **Persistent**: Restored on load to preserve ordering
- **Tie-breaker**: Used as final sort key for deterministic ordering
- **Per-system**: Each `AuthoritativeMemorySystem` has its own counter

### Deterministic Sorting

Memory retrieval uses **strict total order sorting** to ensure byte-stable results:

**Episodic Memories**:
1. Primary: Score (descending) - based on recency, relevance, significance
2. Secondary: `CreatedAtTicks` (descending) - more recent first
3. Tertiary: `Id` (ordinal ascending) - stable string comparison
4. Final: `SequenceNumber` (ascending) - tie-breaker

**Beliefs**:
1. Primary: Score (descending) - based on relevance and confidence
2. Secondary: `Confidence` (descending) - higher confidence first
3. Tertiary: `Id` (ordinal ascending) - stable string comparison
4. Final: `SequenceNumber` (ascending) - tie-breaker

**Result**: Same memory state + same query = identical retrieval order (byte-stable)

### Sequence Number Management

```csharp
// After loading memories from persistence, recalculate the counter
memorySystem.RecalculateNextSequenceNumber();

// This ensures new memories get sequence numbers higher than existing ones
// Prevents sequence number collisions
```

## Memory Lifecycle

### Creation

Memories are created through the `AuthoritativeMemorySystem` API:

```csharp
// Canonical facts (Designer only)
memorySystem.AddCanonicalFact("fact_id", "The king is Arthur", "world_lore");

// World state (GameSystem or Designer)
memorySystem.SetWorldState("door_1", "open", MutationSource.GameSystem);

// Episodic memory (ValidatedOutput or higher)
var entry = new EpisodicMemoryEntry("Player said hello", EpisodeType.Dialogue);
memorySystem.AddEpisodicMemory(entry, MutationSource.ValidatedOutput);

// Beliefs (ValidatedOutput or higher)
var belief = BeliefMemoryEntry.CreateOpinion("player", "is friendly", 0.7f, 0.8f);
memorySystem.SetBelief("player_opinion", belief, MutationSource.ValidatedOutput);
```

### Decay

Episodic memories decay over time:

```csharp
// Apply decay to all episodic memories
memorySystem.ApplyEpisodicDecay();

// Decay formula:
// adjustedDecay = decayRate * (1.0 - Significance * 0.5)
// Strength = max(0, Strength - adjustedDecay)
```

**Decay Factors**:
- `EpisodicDecayRate`: Default 0.05 (5% per cycle)
- `Significance`: Higher significance = slower decay
- Memories with `Strength <= 0` are removed
- Memories with `Strength <= 0.1` are inactive (not retrieved)

### Pruning

Episodic memories are automatically pruned when count exceeds limit:

```csharp
// Pruning happens automatically when adding memories
// Removes lowest strength memories first
// Then removes by lowest significance
```

**Pruning Strategy**:
1. If `Count <= MaxEpisodicMemories`: No pruning
2. If `Count > MaxEpisodicMemories`:
   - Sort by `Strength` (ascending), then `Significance` (ascending)
   - Remove the lowest `(Count - MaxEpisodicMemories)` memories

### Access Tracking

All memory entries track access:

```csharp
// Accessing a memory updates LastAccessedAt
var fact = memorySystem.GetCanonicalFact("king_name");
// fact.LastAccessedAt is now updated

// This can be used for:
// - Memory analytics
// - Debugging
// - Future LRU-based pruning (if needed)
```

## Context Retrieval Layer

The `ContextRetrievalLayer` intelligently retrieves relevant memories for prompt assembly:

### Purpose

- **Filter memories** by relevance to current interaction
- **Score and rank** memories by recency, relevance, and significance
- **Respect limits** to prevent token bloat
- **Ensure determinism** through strict total order sorting

### Configuration

```csharp
var config = new ContextRetrievalConfig
{
    MaxCanonicalFacts = 0,           // 0 = unlimited
    MaxWorldState = 0,               // 0 = unlimited
    MaxEpisodicMemories = 15,        // Top 15 episodic memories
    MaxBeliefs = 10,                 // Top 10 beliefs
    MinEpisodicStrength = 0.1f,      // Only active memories
    MinBeliefConfidence = 0.3f,       // Only confident beliefs
    IncludeContradictedBeliefs = false,
    RecencyWeight = 0.4f,            // 40% weight on recency
    RelevanceWeight = 0.4f,          // 40% weight on relevance
    SignificanceWeight = 0.2f        // 20% weight on significance
};
```

### Retrieval Process

```csharp
var retrievalLayer = new ContextRetrievalLayer(memorySystem, config);

// Retrieve context for an interaction
var retrievedContext = retrievalLayer.RetrieveContext(
    playerInput: "Tell me about the king",
    topics: new[] { "royalty", "history" }
);

// RetrievedContext contains:
// - CanonicalFacts: All relevant canonical facts
// - WorldState: All relevant world state
// - EpisodicMemories: Top N scored episodic memories
// - Beliefs: Top N scored beliefs
```

### Scoring Algorithm

**Episodic Memory Score**:
```
score = (RecencyWeight * Strength) + 
        (RelevanceWeight * RelevanceScore) + 
        (SignificanceWeight * Significance)
```

**Belief Score**:
```
score = (0.6 * RelevanceScore) + (0.4 * Confidence)
// Penalized by 0.5x if contradicted
```

**Relevance Score**:
- Keyword overlap with player input
- Topic matching
- Simple keyword matching (can be enhanced with embeddings in future)

### Deterministic Retrieval

Retrieval uses strict total order sorting to ensure byte-stable results:

1. **Score memories** using the scoring algorithm
2. **Sort with strict total order**:
   - Primary: Score (descending)
   - Secondary: CreatedAtTicks (descending) or Confidence (descending)
   - Tertiary: Id (ordinal ascending)
   - Final: SequenceNumber (ascending)
3. **Take top N** memories
4. **Return in deterministic order**

**Result**: Same memory state + same input = identical retrieval (byte-stable)

## Memory Mutation Controller

The `MemoryMutationController` executes validated mutations from the validation gate:

### Purpose

- **Execute validated mutations** from `GateResult.ApprovedMutations`
- **Enforce authority boundaries** (prevent canonical fact modifications)
- **Track statistics** for debugging and monitoring
- **Emit world intents** for game system integration

### Mutation Types

1. **AppendEpisodic**: Add conversation/event to episodic memory
2. **TransformBelief**: Update or create NPC belief/opinion
3. **TransformRelationship**: Update relationship with another entity
4. **EmitWorldIntent**: Dispatch world-affecting intent to game systems

### Execution Flow

```csharp
var mutationController = new MemoryMutationController();

// Execute all approved mutations from validation gate
var batchResult = mutationController.ExecuteMutations(
    gateResult,           // From validation gate
    memorySystem,        // Target memory system
    npcId: "wizard_001"  // Optional NPC ID
);

// Check results
if (batchResult.AllSucceeded)
{
    Debug.Log($"All {batchResult.TotalAttempted} mutations succeeded");
}
else
{
    foreach (var failure in batchResult.Failures)
    {
        Debug.Log($"Failed: {failure.ErrorMessage}");
    }
}

// Handle world intents
foreach (var intent in batchResult.EmittedIntents)
{
    WorldIntentDispatcher.Instance.DispatchIntent(intent);
}
```

### Authority Enforcement

The mutation controller validates mutations before execution:

```csharp
// Pre-flight validation
var error = mutationController.ValidateMutation(mutation, memorySystem);
if (error != null)
{
    // Mutation would fail - don't execute
    Debug.Log($"Validation failed: {error}");
}
```

**Checks**:
- Canonical fact protection (blocks attempts to modify)
- Required fields (content, target, etc.)
- Authority validation (handled by memory system)

### Statistics

The mutation controller tracks statistics:

```csharp
var stats = mutationController.Statistics;
Debug.Log($"Success rate: {stats.SuccessRate}%");
Debug.Log($"Episodic appended: {stats.EpisodicAppended}");
Debug.Log($"Beliefs transformed: {stats.BeliefsTransformed}");
Debug.Log($"Canonical mutation attempts: {stats.CanonicalMutationAttempts}");
```

## PersonaMemoryStore API

The `PersonaMemoryStore` provides a convenient high-level API:

### Purpose

- **Multi-persona management**: Manages memory systems for multiple NPCs
- **Convenience methods**: Simplified API for common operations
- **Structured memory API**: Direct access to all memory types

### Usage

```csharp
var memoryStore = new PersonaMemoryStore();

// Get or create memory system for an NPC
var memorySystem = memoryStore.GetOrCreateSystem("wizard_001");

// Convenience methods
memoryStore.AddMemory("wizard_001", "Player said hello");
var memories = memoryStore.GetMemory("wizard_001");

// Structured memory API
memoryStore.AddCanonicalFact("wizard_001", "king_name", "The king is Arthur", "world_lore");
memoryStore.SetWorldState("wizard_001", "door_1", "open", MutationSource.GameSystem);
memoryStore.AddDialogue("wizard_001", "Player", "Hello!", significance: 0.5f);
memoryStore.SetBelief("wizard_001", "player_opinion", "player", "is friendly", confidence: 0.8f);

// Batch operations
var results = memoryStore.AddCanonicalFactToAll("magic_exists", "Magic is real", "world_lore");
// Adds to all existing personas

// Statistics
var stats = memoryStore.GetStatistics("wizard_001");
Debug.Log($"Memory stats: {stats}");
```

## Integration with Architecture

### Component 3: External Authoritative Memory System

The memory system is **Component 3** of the nine-component architecture:

1. **Interaction Context** (Component 1) → Provides trigger and player input
2. **Expectancy Engine** (Component 2) → Generates constraints
3. **Memory System** (Component 3) → **Retrieves relevant context**
4. **State Snapshot** (Component 4) → Captures immutable context
5. **Ephemeral Working Memory** (Component 5) → Creates bounded prompt
6. **LLM Inference** (Component 6) → Generates untrusted output
7. **Validation Gate** (Component 7) → Validates output
8. **Mutation Controller** (Component 8) → **Executes validated mutations**
9. **Fallback System** (Component 9) → Provides safe responses

### Memory Flow

```
1. Interaction triggers memory retrieval
   ↓
2. ContextRetrievalLayer retrieves relevant memories
   ↓
3. Memories included in StateSnapshot
   ↓
4. EphemeralWorkingMemory creates bounded prompt
   ↓
5. LLM generates output (may propose mutations)
   ↓
6. ValidationGate validates output
   ↓
7. If valid, MutationController executes mutations
   ↓
8. Memory system updated with new memories
```

### Key Integration Points

1. **Context Retrieval**: `ContextRetrievalLayer` retrieves memories for `StateSnapshot`
2. **Mutation Execution**: `MemoryMutationController` executes validated mutations
3. **Authority Enforcement**: Memory system enforces authority boundaries
4. **Deterministic Ordering**: Memory system ensures byte-stable retrieval

## Best Practices

### 1. Initialize Canonical Facts Early

Set up immutable world truths before any AI interactions:

```csharp
// In NPC initialization
memoryStore.AddCanonicalFact("npc_001", "king_name", "The king is named Arthur", "world_lore");
memoryStore.AddCanonicalFact("npc_001", "magic_exists", "Magic is real in this world", "world_lore");
```

### 2. Use Appropriate Memory Types

- **Canonical Facts**: Immutable world truths (designer-only)
- **World State**: Mutable game state (game system authority)
- **Episodic Memory**: Conversation/event history (validated outputs)
- **Beliefs**: NPC opinions/relationships (can be wrong)

### 3. Set Significance for Episodic Memory

Important events should have higher significance for better retention:

```csharp
// Major event - high significance
var majorEvent = new EpisodicMemoryEntry("Player saved the village", EpisodeType.Event)
{
    Significance = 1.0f  // Will decay very slowly
};
memorySystem.AddEpisodicMemory(majorEvent, MutationSource.ValidatedOutput);

// Casual conversation - low significance
memorySystem.AddDialogue("Player", "Hello", significance: 0.3f);
```

### 4. Configure Retrieval Limits

Balance context richness with token efficiency:

```csharp
var config = new ContextRetrievalConfig
{
    MaxEpisodicMemories = 15,  // Enough for context, not too many tokens
    MaxBeliefs = 10,           // Top beliefs only
    MinBeliefConfidence = 0.3f  // Filter low-confidence beliefs
};
```

### 5. Monitor Memory Statistics

Track memory usage and mutation success:

```csharp
var stats = memorySystem.GetStatistics();
Debug.Log($"Memory stats: {stats}");
// Output: "Memory Stats: 5 facts, 12 state, 45/100 episodes, 8/10 beliefs"

var mutationStats = mutationController.Statistics;
Debug.Log($"Mutation stats: {mutationStats}");
// Output: "Mutations: 150/160 (93.8%), Episodic: 120, Beliefs: 30, ..."
```

### 6. Apply Decay Periodically

Decay episodic memories to simulate forgetting:

```csharp
// In game loop or periodic update
memorySystem.ApplyEpisodicDecay();

// Or for all personas
memoryStore.ApplyDecayAll();
```

### 7. Handle Contradicted Beliefs

Contradicted beliefs are automatically marked, but you may want to handle them:

```csharp
var beliefs = memorySystem.GetAllBeliefs();
foreach (var belief in beliefs)
{
    if (belief.IsContradicted)
    {
        Debug.Log($"Contradicted belief: {belief.BeliefContent}");
        // May want to remove or update contradicted beliefs
    }
}
```

### 8. Use Sequence Numbers for Persistence

When persisting memories, preserve sequence numbers:

```csharp
// After loading memories
memorySystem.RecalculateNextSequenceNumber();

// This ensures new memories get correct sequence numbers
// Prevents collisions and maintains ordering
```

## Troubleshooting

### Memory Not Updating

**Symptoms**: Mutations appear to succeed but memory doesn't change.

**Solutions**:
- Verify `GateResult.Passed` is true
- Check `LastMutationBatchResult` for execution results
- Ensure mutations are not targeting canonical facts
- Check authority levels (source must have sufficient authority)

### Contradiction Detection Not Working

**Symptoms**: Beliefs contradict canonical facts but aren't marked.

**Solutions**:
- Contradiction detection uses simple pattern matching
- May not catch all contradictions (semantic analysis planned for future)
- Check `IsContradicted` flag manually if needed
- Review contradiction patterns in `ContradictesCanonicalFact()`

### Memory Retrieval Inconsistent

**Symptoms**: Different memories retrieved for same input.

**Solutions**:
- Ensure deterministic sorting is used (strict total order)
- Check that sequence numbers are preserved across sessions
- Verify `RecalculateNextSequenceNumber()` is called after loading
- Check for floating-point precision issues in scoring

### Episodic Memories Decaying Too Fast

**Symptoms**: Important memories disappear quickly.

**Solutions**:
- Increase `Significance` for important memories (0.8-1.0)
- Reduce `EpisodicDecayRate` (default: 0.05)
- Increase `MaxEpisodicMemories` to retain more memories
- Check that `ApplyEpisodicDecay()` isn't called too frequently

### Token Budget Exceeded

**Symptoms**: Prompts are too long due to memory context.

**Solutions**:
- Reduce `MaxEpisodicMemories` in `ContextRetrievalConfig`
- Reduce `MaxBeliefs` in `ContextRetrievalConfig`
- Increase `MinEpisodicStrength` to filter weak memories
- Increase `MinBeliefConfidence` to filter uncertain beliefs
- Use topic filtering to retrieve only relevant memories

## Hybrid Retrieval with Semantic Search (RAG)

The memory system supports optional hybrid retrieval combining keyword matching with semantic vector similarity:

### Overview

When configured with `IEmbeddingProvider` and `IMemoryVectorStore`, the `ContextRetrievalLayer` can use:

- **Keyword matching**: Traditional word overlap scoring
- **Semantic similarity**: Vector embedding cosine similarity
- **Hybrid scoring**: Configurable weighted combination of both

### Components

**IEmbeddingProvider**: Generates embeddings for text
```csharp
public interface IEmbeddingProvider
{
    Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
    Task<float[]?[]> GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken ct = default);
    int EmbeddingDimension { get; }
    bool IsAvailable { get; }
}
```

**IMemoryVectorStore**: Stores and searches memory embeddings
```csharp
public interface IMemoryVectorStore
{
    void Upsert(string memoryId, string? npcId, MemoryVectorType type, float[] embedding, long sequenceNumber);
    bool Remove(string memoryId);
    IReadOnlyList<VectorSearchResult> FindSimilar(float[] queryEmbedding, int k, string? npcId, ...);
    VectorStoreSnapshot CreateSnapshot();
    void RestoreFromSnapshot(VectorStoreSnapshot snapshot);
}
```

**InMemoryVectorStore**: Default in-memory implementation using Dictionary storage with brute-force cosine similarity search. Suitable for <1000 entries.

### Configuration

```csharp
var embeddingConfig = new EmbeddingConfig
{
    ProviderType = EmbeddingProviderType.LlamaCpp,  // Provider selection
    EnableSemanticRetrieval = true,
    KeywordWeight = 0.3f,       // 30% keyword matching
    SemanticWeight = 0.7f,       // 70% semantic similarity
    MinSemanticSimilarity = 0.3f,
    SemanticCandidateLimit = 50,
    EmbeddingDimension = 768,   // nomic-embed-text uses 768
    LlamaCppBaseUrl = "http://localhost:8081",  // Embedding server URL
    LlamaCppModelName = "nomic-embed-text",
    RequestTimeoutSeconds = 30
};

// Factory methods for common configurations
EmbeddingConfig.KeywordOnly()     // Disables semantic retrieval (uses NullEmbeddingProvider)
EmbeddingConfig.Default()         // 30% keyword, 70% semantic with LlamaCpp
EmbeddingConfig.SemanticHeavy()   // 10% keyword, 90% semantic
EmbeddingConfig.Balanced()        // 50% keyword, 50% semantic

// Custom llama.cpp server configuration
EmbeddingConfig.ForLlamaCpp(
    baseUrl: "http://192.168.1.100:8081",
    modelName: "nomic-embed-text",
    embeddingDimension: 768,
    timeoutSeconds: 60
);
```

### Provider Types

The system supports multiple embedding provider types via `EmbeddingProviderType`:

| Provider | Description |
|----------|-------------|
| `Null` | Keyword-only mode (no embeddings) |
| `LlamaCpp` | Local llama.cpp server with `/v1/embeddings` endpoint |
| `OpenAI` | External OpenAI API (future) |

### LlamaCpp Embedding Server Setup

Start a dedicated embedding server (separate from the main LLM):

```bash
# Using llama.cpp server with embedding model
./llama-server -m nomic-embed-text-v1.5.f32.gguf --embedding --port 8081

# Verify endpoint works
curl http://localhost:8081/v1/embeddings -X POST \
  -H "Content-Type: application/json" \
  -d '{"input":"test","model":"nomic-embed-text"}'
```

Common embedding models:
- **nomic-embed-text**: 768 dimensions, good general-purpose
- **all-MiniLM-L6-v2**: 384 dimensions, fast and compact
- **bge-small-en-v1.5**: 384 dimensions, high quality

### Creating Providers with Factory

Use `EmbeddingProviderFactory` for provider instantiation:

```csharp
// From configuration
var config = EmbeddingConfig.Default();
var provider = EmbeddingProviderFactory.Create(config);

// Convenience methods
var nullProvider = EmbeddingProviderFactory.CreateKeywordOnly();
var llamaCppProvider = EmbeddingProviderFactory.CreateLlamaCpp(
    baseUrl: "http://localhost:8081",
    embeddingDimension: 768
);

// Safe creation with error handling
if (EmbeddingProviderFactory.TryCreate(config, out var provider, out var error))
{
    // Use provider
}
else
{
    Debug.LogWarning($"Embedding provider creation failed: {error}");
}
```

### Usage with ContextRetrievalLayer

```csharp
// Create dependencies
var memorySystem = new AuthoritativeMemorySystem();
var vectorStore = new InMemoryVectorStore(768);
var embeddingConfig = EmbeddingConfig.ForLlamaCpp("http://localhost:8081", embeddingDimension: 768);
var embeddingProvider = EmbeddingProviderFactory.Create(embeddingConfig);

// Create retrieval layer with RAG support
var config = new ContextRetrievalConfig
{
    EmbeddingConfig = embeddingConfig
};
var retrievalLayer = new ContextRetrievalLayer(
    memorySystem,
    config,
    embeddingProvider,
    vectorStore,
    npcId: "wizard_001"
);

// Retrieve context - automatically uses hybrid scoring
var context = retrievalLayer.RetrieveContext("Tell me about dragons");
```

### Shared Vector Store Architecture

All NPCs share a single `IMemoryVectorStore` with NPC-based filtering:

- **NpcId=null**: Shared entries (canonical facts, world state) accessible to all NPCs
- **NpcId="wizard_001"**: NPC-specific entries
- **Query filtering**: `FindSimilar()` with `npcId="wizard_001"` returns that NPC's entries plus all shared entries

### Persistence

Vector store data is persisted using binary format for efficiency:

```csharp
// Save
var snapshot = vectorStore.CreateSnapshot();
VectorStoreBinarySerializer.WriteToFile("vectors.bin", snapshot);

// Load
var restored = VectorStoreBinarySerializer.ReadFromFile("vectors.bin");
vectorStore.RestoreFromSnapshot(restored);
```

**Binary format**: ~60% smaller and ~10x faster than JSON for float arrays.

### Diagnostics

Use `VectorStoreDiagnostics` for debugging:

```csharp
// Get summary
var summary = VectorStoreDiagnostics.GetSummary(vectorStore);
// Output: "VectorStore: 1,234 vectors (dim=384), Episodic: 800, Belief: 200, Shared: 234"

// Export to JSON for inspection
VectorStoreDiagnostics.ExportToJsonFile(vectorStore, "debug.json");

// Validate binary file
var report = VectorStoreDiagnostics.ValidateFile("vectors.bin");
```

### Graceful Degradation

When embeddings are unavailable, the system automatically falls back to keyword-only retrieval:

- `IEmbeddingProvider.IsAvailable` returns false → keyword-only
- Embedding generation fails → keyword-only for that query
- No vector store configured → keyword-only

### Determinism

Vector search maintains deterministic ordering:
1. Similarity (descending)
2. SequenceNumber (ascending) - older entries first for ties
3. MemoryId (ordinal ascending) - final tie-breaker

## Recognition Query Service

The `RecognitionQueryService` enables NPCs to recognize repeated interactions:

### Recognition Types

- **Location**: Recognizes when entering a location previously visited
- **Topic**: Recognizes when player discusses a previously discussed topic
- **Conversation**: Recognizes repeated conversation patterns

### Usage

```csharp
var recognitionService = new RecognitionQueryService(
    memorySystem,
    vectorStore,
    embeddingProvider
);

// Location recognition (deterministic, keyword-based)
var locationResult = recognitionService.QueryLocationRecognition("npc-1", "castle");
if (locationResult.Recognized)
{
    Console.WriteLine($"Visited {locationResult.RepeatCount} times before");
}

// Topic recognition (semantic + keyword fallback)
var topicResult = await recognitionService.QueryTopicRecognitionAsync("npc-1", "dragons");
if (topicResult.Recognized)
{
    Console.WriteLine($"Topic discussed {topicResult.RepeatCount} times, similarity: {topicResult.BestMatchSimilarity}");
}
```

## Automatic Embedding Generation

When RAG is enabled, LlamaBrain can automatically generate embeddings for new memories as they're created using the `MemoryEmbeddingService`.

### Overview

The `MemoryEmbeddingService` subscribes to the `AuthoritativeMemorySystem.MemoryMutated` event and automatically:
1. Generates embeddings for new memories using the configured `IEmbeddingProvider`
2. Adds the embeddings to the `IMemoryVectorStore`
3. Updates embedding statistics for monitoring coverage

### Setup

```csharp
// Create memory system
var memorySystem = new AuthoritativeMemorySystem { NpcId = "wizard_001" };

// Configure embedding provider
var embeddingConfig = EmbeddingConfig.ForLlamaCpp("http://localhost:8081", embeddingDimension: 768);
var provider = EmbeddingProviderFactory.Create(embeddingConfig);

// Create vector store
var vectorStore = new InMemoryVectorStore(768);

// Wire up auto-embedding
var embeddingService = new MemoryEmbeddingService(
    memorySystem,
    provider,
    vectorStore,
    logger: Console.WriteLine // Optional logging
);

// Now all memory mutations automatically generate embeddings
memorySystem.AddCanonicalFact("king_name", "The king's name is Arthur");
memorySystem.AddDialogue("Player", "Hello, wizard!");
memorySystem.SetBelief("player_trust",
    BeliefMemoryEntry.CreateOpinion("Player", "is trustworthy"),
    MutationSource.ValidatedOutput);

// Embeddings are generated asynchronously and added to vector store
```

### How It Works

```
1. Memory is created/updated in AuthoritativeMemorySystem
   ↓
2. MemoryMutated event fires with memory content and metadata
   ↓
3. MemoryEmbeddingService receives the event
   ↓
4. Asynchronously generates embedding via IEmbeddingProvider
   ↓
5. If successful, upserts embedding into IMemoryVectorStore
   ↓
6. Updates embedding statistics in AuthoritativeMemorySystem
```

### Key Design Decisions

- **Non-blocking**: Memory mutations return immediately; embedding generation happens asynchronously
- **Fire-and-forget**: Uses `async void` event handler pattern for asynchronous processing
- **Graceful degradation**: If embedding fails, memory is still stored (keyword-only retrieval fallback)
- **IDisposable**: Properly unsubscribes from events when disposed

### Monitoring Embedding Coverage

Track how many memories have embeddings:

```csharp
var stats = memorySystem.GetStatistics();
Console.WriteLine($"Embedding coverage: {stats.EmbeddingCoverage:P0}");
Console.WriteLine($"{stats.EmbeddedMemories} / {stats.TotalMemoriesCreated} memories embedded");
```

**Output:**
```
Embedding coverage: 100%
4 / 4 memories embedded
```

### Memory Type Behavior

| Memory Type | NPC Scope | Event Content |
|-------------|-----------|---------------|
| Canonical Fact | Shared (null) | The fact text |
| World State | Shared (null) | `key=value` format |
| Episodic Memory | NPC-specific | Description text |
| Belief | NPC-specific | Belief content |

### Graceful Degradation

When embeddings are unavailable, the system continues to function:

- **Provider unavailable**: Event is skipped, memory still stored
- **Embedding generation fails**: Error logged, memory still stored
- **Provider throws exception**: Exception caught and logged, memory still stored
- **Service disposed**: Events no longer processed

The retrieval layer automatically falls back to keyword-only retrieval when embeddings are missing.

### Performance

- Embedding generation is asynchronous and non-blocking
- Memory mutations return immediately
- Typical embedding time: 50-100ms per memory (local llama.cpp server)
- Consider batch generation for initial data loading scenarios

### Cleanup

Dispose the service when no longer needed:

```csharp
embeddingService.Dispose();
```

This unsubscribes from the `MemoryMutated` event and cancels any pending embedding operations.

## Future Enhancements

### Planned Features

- **OpenAI Embedding Provider**: Integration with OpenAI's embedding API for hosted deployments
- **Persistent vector indices**: More efficient search for large datasets (HNSW, IVF)
- **Memory compression**: Reduce embedding storage requirements
- **Automatic embedding migration**: Batch generation for existing memories

## Summary

The LlamaBrain memory system provides:

1. **Four memory types** with clear authority hierarchy
2. **Strict authority enforcement** preventing memory corruption
3. **Deterministic ordering** for reproducible behavior
4. **Automatic decay and pruning** for memory management
5. **Intelligent context retrieval** based on relevance
6. **Mutation controller** for safe memory updates
7. **Multi-persona support** through PersonaMemoryStore
8. **Automatic embedding generation** for RAG-based retrieval
9. **Embedding coverage monitoring** for system health tracking

**Key Principle**: The LLM never directly modifies memory. All changes must pass through validation and mutation controllers, ensuring authoritative memory that cannot be corrupted by AI hallucinations.

---

## Further Reading

- [README.md](../LlamaBrain/README.md) - Main library documentation and overview
- [ARCHITECTURE.md](ARCHITECTURE.md) - Complete architectural documentation including Component 3 (Memory System)
- [PIPELINE_CONTRACT.md](PIPELINE_CONTRACT.md) - Formal pipeline contract including memory retrieval specifications
- [VALIDATION_GATING.md](VALIDATION_GATING.md) - Validation system that protects memory from corruption
- [USAGE_GUIDE.md](USAGE_GUIDE.md) - Practical examples for using the memory system
- [ROADMAP.md](ROADMAP.md) - Implementation status and future memory features

---

**Last Updated**: March 17, 2026
**Memory System Version**: 0.4.0-rc.1
