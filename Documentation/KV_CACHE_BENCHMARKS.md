# KV Cache Performance Benchmarks

This document describes the performance benchmarks for LlamaBrain's KV cache optimization system (Feature 27).

## Overview

KV (Key-Value) cache optimization reduces LLM inference latency by caching the computed key-value pairs for static prompt content. When the same prefix is used across requests, the inference engine can reuse cached computations instead of re-evaluating all tokens.

### Performance Targets

| Metric | Target | Description |
|--------|--------|-------------|
| **Cache Hit Latency** | < 200ms | Response time when static prefix is cached |
| **Cache Miss Latency** | < 1.5s | Response time with full prefill (2k+ token context) |
| **Cache Hit Rate** | > 80% | Percentage of requests that hit the cache |
| **Token Cache Efficiency** | > 80% | Percentage of static prefix tokens successfully cached |

## Benchmark Test Suite

### Unit Tests (`KvCacheTests.cs`)

Location: `LlamaBrain/LlamaBrain.Tests/Performance/KvCacheTests.cs`

These tests verify the cache metrics tracking system without requiring a running LLM server.

#### CacheEfficiencyMetrics Tests

| Test | Description |
|------|-------------|
| `CacheEfficiencyMetrics_Default_AllZeros` | Verifies metrics initialize to zero |
| `CacheEfficiencyMetrics_RecordRequest_CacheHit_IncrementsCounters` | Records cache hits correctly |
| `CacheEfficiencyMetrics_RecordRequest_CacheMiss_IncrementsCounters` | Records cache misses correctly |
| `CacheEfficiencyMetrics_CacheHitRate_CalculatesCorrectly` | Hit rate: 3 hits, 1 miss = 75% |
| `CacheEfficiencyMetrics_TokenCacheRate_CalculatesCorrectly` | Token rate: 300/400 = 75% |
| `CacheEfficiencyMetrics_StaticPrefixCacheEfficiency_CalculatesCorrectly` | Prefix efficiency: 90/100 = 90% |
| `CacheEfficiencyMetrics_Reset_ClearsAllCounters` | Reset clears all metrics |
| `CacheEfficiencyMetrics_RecordFromCompletionMetrics_Works` | Integration with CompletionMetrics |
| `CacheEfficiencyMetrics_RecordCacheHit_ExplicitMethod` | Explicit hit/miss recording |
| `CacheEfficiencyMetrics_ToString_ContainsRelevantInfo` | String representation |

#### StructuredPipelineMetrics Cache Tests

| Test | Description |
|------|-------------|
| `StructuredPipelineMetrics_RecordCacheHit_IncrementsCounter` | Pipeline cache hit tracking |
| `StructuredPipelineMetrics_RecordCacheMiss_IncrementsCounter` | Pipeline cache miss tracking |
| `StructuredPipelineMetrics_CacheHitRate_CalculatesCorrectly` | 2 hits, 2 misses = 50% |
| `StructuredPipelineMetrics_RecordCacheResult_RecordsHitWhenCached` | Auto-detection of cache hits |
| `StructuredPipelineMetrics_RecordCacheResult_RecordsMissWhenNotCached` | Auto-detection of cache misses |
| `StructuredPipelineMetrics_Reset_ClearsCacheCounters` | Reset functionality |
| `StructuredPipelineMetrics_ToString_IncludesCacheInfo` | String representation |

#### DialogueInteraction Cache Tests

| Test | Description |
|------|-------------|
| `DialogueInteraction_DefaultCacheFields_AreZero` | Default field values |
| `DialogueInteraction_CacheEfficiency_CalculatesCorrectly` | 80/100 = 80% efficiency |
| `DialogueInteraction_CacheEfficiency_ZeroWhenNoPrefixTokens` | Edge case handling |
| `DialogueInteraction_KvCachingEnabled_CanBeSet` | Enable flag functionality |

#### Thread Safety Tests

| Test | Description |
|------|-------------|
| `CacheEfficiencyMetrics_ConcurrentAccess_MaintainsConsistency` | 100 concurrent requests maintain accuracy |
| `StructuredPipelineMetrics_ConcurrentCacheRecording_MaintainsConsistency` | Pipeline metrics thread safety |

### Static Prefix Tests (`StaticPrefixTests.cs`)

Location: `LlamaBrain/LlamaBrain.Tests/Inference/StaticPrefixTests.cs`

These tests verify the static prefix enforcement and determinism guarantees.

#### Key Tests

| Test | What It Proves |
|------|----------------|
| `AssembleWithCacheInfo_WithDefaultBoundary_SplitsAtFacts` | Default boundary splits after canonical facts |
| `AssembleWithCacheInfo_WithAggressiveBoundary_IncludesWorldStateInPrefix` | Aggressive boundary includes world state |
| `AssembleWithCacheInfo_IdenticalInputs_ProduceIdenticalStaticPrefix` | Static prefix is byte-stable |
| `AssembleWithCacheInfo_DifferentPlayerInput_SameStaticPrefix` | Different inputs share same prefix |
| `AssembleWithCacheInfo_DifferentDialogueHistory_SameStaticPrefix` | Different history shares same prefix |
| `AssembleWithCacheInfo_DialogueHistory_AlwaysInDynamicSuffix` | Dialogue never in static prefix |
| `AssembleWithCacheInfo_PlayerInput_AlwaysInDynamicSuffix` | Player input never in static prefix |

### PlayMode Performance Tests (`KvCachePerformanceTests.cs`)

Location: `LlamaBrainRuntime/Assets/LlamaBrainRuntime/Tests/PlayMode/KvCachePerformanceTests.cs`

These tests require a running llama.cpp server and measure real-world cache performance.

#### Test Descriptions

##### 1. `PlayMode_KvCache_SameStaticPrefix_ReducesPrefillTime`

**Purpose**: Verifies that cached requests have lower prefill time than uncached requests.

**Test Pattern**:
1. First request (cold cache) - measures baseline prefill time
2. Second request with same static prefix - should have much lower prefill time
3. Verify prefill time reduction indicates cache hit

**Expected Result**: Second request shows >50% reduction in prefill time when first request took >100ms.

##### 2. `PlayMode_KvCache_DifferentStaticPrefix_InvalidatesCache`

**Purpose**: Verifies that changing the static prefix invalidates the cache.

**Test Pattern**:
1. Request with prefix A (primes cache)
2. Request with prefix B (different prefix, misses cache)
3. Request with prefix A again (cache evicted, full prefill)

**Expected Result**: Documents cache eviction behavior on prefix change.

##### 3. `PlayMode_KvCache_MultiTurnConversation_MaintainsCacheEfficiency`

**Purpose**: Simulates typical gameplay pattern with multiple dialogue turns.

**Test Pattern**:
1. Five dialogue turns with same NPC (same static prefix)
2. Growing dialogue history in dynamic suffix
3. Measure prefill times across all turns

**Expected Result**: Subsequent turns should not be significantly slower than first turn (within 1.5x).

##### 4. `PlayMode_KvCache_CacheDisabled_NoPrefillImprovement`

**Purpose**: Control test to confirm cache behavior is tied to the `cachePrompt` flag.

**Test Pattern**:
1. Request with `cachePrompt=false`
2. Second request with `cachePrompt=false`
3. Compare prefill times

**Expected Result**: Both requests have similar prefill times (no cache benefit).

##### 5. `PlayMode_KvCache_EnabledVsDisabled_MeasurableImprovement`

**Purpose**: Definitive comparison of cache enabled vs disabled performance.

**Test Pattern**:
1. Phase 1: Multiple requests with `cachePrompt=true` (warmup + measure)
2. Phase 2: Multiple requests with `cachePrompt=false` (warmup + measure)
3. Compare average prefill times

**Expected Result**: Cache enabled shows measurable improvement over cache disabled.

## Running the Benchmarks

### Unit Tests (No Server Required)

```bash
# Run all KV cache unit tests
dotnet test --filter "Category=KvCache"

# Run only performance category tests
dotnet test --filter "Category=Performance"

# Run with detailed output
dotnet test --filter "Category=KvCache" -v n
```

### PlayMode Tests (Server Required)

PlayMode tests require a real llama.cpp server. They are marked with `Category=ExternalIntegration`.

#### Prerequisites

1. **llama-server executable**: `Backend/llama-server.exe`
2. **Model file**: `Backend/model/qwen2.5-3b-instruct-abliterated-sft-q4_k_m.gguf`

#### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `LLAMABRAIN_EXECUTABLE_PATH` | `Backend/llama-server.exe` | Path to llama-server |
| `LLAMABRAIN_MODEL_PATH` | `Backend/model/*.gguf` | Path to model file |
| `LLAMABRAIN_PORT` | Auto-assigned | Server port |
| `LLAMABRAIN_SERVER_TIMEOUT_SECONDS` | `60` | Server startup timeout |
| `LLAMABRAIN_SKIP_EXTERNAL_TESTS` | `false` | Set to `1` to skip tests |

#### Running in Unity

1. Open the Unity project
2. Open Test Runner (Window > General > Test Runner)
3. Select PlayMode tab
4. Filter by `KvCachePerformanceTests`
5. Run tests (server starts automatically)

## Metrics Reference

### CacheEfficiencyMetrics

```csharp
public class CacheEfficiencyMetrics
{
    // Counters
    public int TotalRequests { get; }
    public int CacheHitRequests { get; }
    public int CacheMissRequests { get; }
    public long TotalCachedTokens { get; }
    public long TotalPromptTokens { get; }
    public long TotalStaticPrefixTokens { get; }

    // Calculated Rates (0-100%)
    public double CacheHitRate { get; }          // CacheHitRequests / TotalRequests
    public double TokenCacheRate { get; }        // TotalCachedTokens / TotalPromptTokens
    public double StaticPrefixCacheEfficiency { get; } // TotalCachedTokens / TotalStaticPrefixTokens

    // Methods
    void RecordRequest(int promptTokens, int cachedTokens, int staticPrefixTokens = 0);
    void RecordRequest(CompletionMetrics metrics, int staticPrefixTokens = 0);
    void RecordCacheHit();
    void RecordCacheMiss();
    void Reset();
}
```

### Key Metrics Explained

| Metric | Formula | Interpretation |
|--------|---------|----------------|
| **Cache Hit Rate** | `CacheHitRequests / TotalRequests * 100` | % of requests that benefited from cache |
| **Token Cache Rate** | `TotalCachedTokens / TotalPromptTokens * 100` | % of total tokens that were cached |
| **Static Prefix Efficiency** | `TotalCachedTokens / TotalStaticPrefixTokens * 100` | How well static prefix is being cached |

### Interpreting Results

| Cache Hit Rate | Interpretation |
|----------------|----------------|
| > 90% | Excellent - optimal cache utilization |
| 80-90% | Good - meeting target |
| 60-80% | Acceptable - room for improvement |
| < 60% | Poor - investigate cache invalidation patterns |

| Token Cache Rate | Interpretation |
|------------------|----------------|
| > 80% | Excellent - most tokens cached |
| 60-80% | Good - significant caching benefit |
| 40-60% | Moderate - consider larger static prefix |
| < 40% | Low - review prompt structure |

## Static Prefix Boundaries

The static prefix boundary determines where cacheable content ends and per-request content begins.

```csharp
public enum StaticPrefixBoundary
{
    AfterSystemPrompt = 0,    // Most conservative
    AfterCanonicalFacts = 1,  // Recommended default
    AfterWorldState = 2,      // More aggressive
    AfterConstraints = 3      // Most aggressive
}
```

### Boundary Trade-offs

| Boundary | Cached Content | Use Case |
|----------|----------------|----------|
| `AfterSystemPrompt` | System prompt only | Maximum safety, minimum cache |
| `AfterCanonicalFacts` | + Canonical facts | Recommended for most games |
| `AfterWorldState` | + World state | Stable world state per session |
| `AfterConstraints` | + Constraints | When constraints rarely change |

### Prompt Structure

```
┌─────────────────────────────────────────────────────────┐
│                    STATIC PREFIX                        │
│  (Cacheable - same across requests to same NPC)         │
├─────────────────────────────────────────────────────────┤
│  System Prompt                                          │
│  "You are Gareth, a gruff blacksmith..."                │
├─────────────────────────────────────────────────────────┤
│  Canonical Facts                                        │
│  - The village of Thornwood sits at the edge of...      │
│  - The local tavern is called The Rusted Nail...        │
├─────────────────────────────────────────────────────────┤
│                    DYNAMIC SUFFIX                       │
│  (Per-request - changes with each interaction)          │
├─────────────────────────────────────────────────────────┤
│  World State (may change)                               │
│  Episodic Memories (varies by recency)                  │
│  Dialogue History (grows with conversation)             │
│  Player Input (unique per request)                      │
└─────────────────────────────────────────────────────────┘
```

## Three-Layer Cake Architecture

For optimal cache efficiency, structure prompts into three volatility layers:

| Layer | Content | Volatility | Cache Strategy |
|-------|---------|------------|----------------|
| **1. Bedrock** | System Prompt + JSON Schema + World Rules | Zero | `n_keep` protected. Never re-evaluates. |
| **2. State** | Inventory, Location, Time, Nearby NPCs | Low | Invalidate on change. Re-eval when player moves/acts. |
| **3. Chat** | Dialogue History + Current Command | High | Rolling buffer. Appends and shifts oldest. |

### Context Shift Protection (`n_keep`)

**Critical**: When the context window fills (e.g., 4096 tokens), llama.cpp performs a **context shift**, discarding the oldest tokens to make room. Without protection, this evicts your static prefix—destroying cache efficiency.

**Solution**: Set `n_keep` to the token count of Layer 1 (Bedrock).

```
Context Window (4096 tokens):
┌──────────────────────────────────────────────────────────────────┐
│ [0...n_keep]     │ [n_keep+1...n_past]        │ [new tokens]     │
│ BEDROCK          │ STATE + CHAT               │ GENERATION       │
│ (protected)      │ (evictable)                │ (computed)       │
└──────────────────────────────────────────────────────────────────┘
        ▲                    ▲
        │                    └── Context shift evicts from HERE
        └── n_keep protects everything before this point
```

**Implementation** (pending):
```csharp
// Calculate static prefix token count
var cacheInfo = assembler.AssembleWithCacheInfo(workingMemory);
int nKeep = cacheInfo.EstimatedStaticTokens;

// Pass to llama-server (requires API extension)
var response = await client.SendPromptWithMetricsAsync(
    cacheInfo.FullPrompt,
    maxTokens: 50,
    cachePrompt: true,
    nKeep: nKeep  // Protect static prefix from context shift
);
```

**Status**: ✅ Implemented. `n_keep` parameter exposed in `IApiClient` and `CompletionRequest`. `BrainAgent.SendMessageAsync` dynamically calculates `nKeep` from static prefix when KV caching is enabled. See ROADMAP Feature 27.2.1.

### RedRoom Debug Visualization (Planned)

Visual cache block indicators for debugging:

| Color | Meaning |
|-------|---------|
| **Green** | Static Prefix (Frozen/Cached) |
| **Yellow** | Context (Retained) |
| **Red** | New Tokens (Computed this frame) |

If the Green block turns Red during a conversation, `n_keep` logic has failed.

## Configuration

### KvCacheConfig Presets

```csharp
// Caching disabled (legacy behavior)
var config = KvCacheConfig.Disabled();

// Recommended default (AfterCanonicalFacts)
var config = KvCacheConfig.Default();

// Aggressive caching (AfterWorldState)
var config = KvCacheConfig.Aggressive();
```

### Custom Configuration

```csharp
var config = new KvCacheConfig
{
    EnableCaching = true,
    Boundary = StaticPrefixBoundary.AfterCanonicalFacts,
    TrackMetrics = true,
    ValidatePrefixStability = true // Debug builds only
};
```

## Usage Example

```csharp
// Configure cache-aware prompt assembly
var assemblerConfig = new PromptAssemblerConfig
{
    KvCacheConfig = KvCacheConfig.Default()
};

var assembler = new PromptAssembler(assemblerConfig);

// Assemble with cache info
var cacheInfo = assembler.AssembleWithCacheInfo(
    workingMemory,
    npcName: "Gareth");

// Log cache metrics
Console.WriteLine($"Static prefix: {cacheInfo.EstimatedStaticTokens} tokens");
Console.WriteLine($"Dynamic suffix: {cacheInfo.EstimatedDynamicTokens} tokens");

// Send with cache enabled
var response = await apiClient.SendPromptWithMetricsAsync(
    cacheInfo.FullPrompt,
    maxTokens: 50,
    cachePrompt: true);

// Track cache efficiency
metrics.RecordRequest(
    promptTokens: response.PromptTokenCount,
    cachedTokens: response.CachedTokenCount,
    staticPrefixTokens: cacheInfo.EstimatedStaticTokens);

Console.WriteLine($"Prefill time: {response.PrefillTimeMs}ms");
Console.WriteLine($"Cache hit rate: {metrics.CacheHitRate:F1}%");
```

## Troubleshooting

### Low Cache Hit Rate

**Symptoms**: Cache hit rate below 60%

**Possible Causes**:
1. **Dynamic content in static prefix**: Timestamps, random elements, or shuffled content
2. **Frequent NPC switching**: Each NPC has different static prefix
3. **Small context window**: Cache evicted due to context size limits

**Solutions**:
- Review prompt assembly for dynamic content before static prefix boundary
- Group interactions by NPC to maximize cache reuse
- Increase context window size on llama-server

### High Cache Miss on Same NPC

**Symptoms**: Same NPC, repeated interactions, but cache misses

**Possible Causes**:
1. **Non-deterministic facts ordering**: Facts assembled in different order
2. **System prompt variations**: Dynamic elements in system prompt
3. **Cache eviction**: Context window too small for workload

**Solutions**:
- Verify deterministic ordering in `AssembleWithCacheInfo()` tests
- Review system prompt for any dynamic content
- Monitor cache eviction patterns

### Minimal Latency Improvement

**Symptoms**: Cache hits but similar latency to misses

**Possible Causes**:
1. **Small static prefix**: Not enough tokens to benefit from caching
2. **Fast hardware**: Prefill time already low
3. **Network latency**: Network dominates response time

**Solutions**:
- Expand static prefix (use more aggressive boundary)
- For fast hardware, focus on other optimizations
- Profile to identify actual bottleneck

## Test Results Summary

### Expected Benchmark Results

Based on typical hardware with a 3B parameter model:

| Scenario | Cold Cache | Warm Cache | Improvement |
|----------|------------|------------|-------------|
| Simple dialogue (500 tokens) | ~150ms | ~50ms | ~67% |
| Medium context (1000 tokens) | ~400ms | ~80ms | ~80% |
| Large context (2000 tokens) | ~1000ms | ~120ms | ~88% |

### Factors Affecting Results

- **Model size**: Larger models have longer prefill times
- **Hardware**: GPU vs CPU, VRAM availability
- **Context size**: Larger contexts benefit more from caching
- **Static prefix ratio**: Higher ratio = more benefit

---

**Last Updated**: January 2026
**Feature Version**: 27.0 (Feature 27: Smart KV Cache Management)
**Test Count**: 47 tests (22 static prefix + 20 cache metrics + 5 PlayMode)
