# Determinism Contract

**Last Updated**: December 31, 2025  
**Status**: Contract decisions documented, implementation in progress

## Determinism Boundary

**Explicit Statement of Scope:**

The LlamaBrain architecture provides **deterministic state reconstruction** around a **stateless LLM generator**. The determinism guarantee applies to the following boundaries:

- ✅ **Deterministic Reconstruction**: Given identical inputs (StateSnapshot, constraints, memory state), the system will produce identical prompt assembly, validation results, and memory mutations.
- ❌ **Deterministic LLM Generation**: NOT guaranteed. LLM output is non-deterministic by nature. Tests use mocked `IApiClient` to provide deterministic responses for verification.
- ✅ **Deterministic Prompt Assembly**: Must be byte-stable given identical snapshot, config, and comparer rules. Same inputs produce identical prompt text.
- ✅ **Deterministic Validation**: Same ParsedOutput + ValidationContext = same GateResult.
- ✅ **Deterministic Memory Mutation**: Same GateResult + MemorySystem initial state = same mutation results.

**Test Contract**: All deterministic behavior tests use mocked `IApiClient` to eliminate LLM non-determinism. The contract is: same prompt → same mocked output (controlled).

---

## Contract Decisions (Resolved Conflicts)

The following contract decisions ensure non-contradictory behavior across the system:

### 1. ContextRetrievalLayer Tie-Breaker

**Decision**: Selection uses a single total-order sort with chained keys. Determinism guarantee requires a strict total order.

**Implementation Requirements**:
- Ordering must be implemented via a comparator chain that forms a strict total order, including `SequenceNumber` as the final unique key.
- Since every element has a unique final key (`SequenceNumber`), there are no ties. The ordering keys must form a strict total order.
- **Id comparisons MUST use ordinal ordering** (`StringComparer.Ordinal`) to ensure determinism across machines/locales.
- **CreatedAtTicks**: Must be `DateTimeOffset.UtcNow.UtcTicks` captured at insertion and persisted; never derived from local time or recomputed.

**Sorting Order**:
- **Episodic memories**: `score desc, CreatedAtTicks desc, Id asc (ordinal), SequenceNumber asc`
- **Beliefs**: `score desc, Confidence desc, Id asc (ordinal), SequenceNumber asc`

**Rationale**: Ensures deterministic ordering for identical scores. SequenceNumber provides total order even when timestamps/identifiers collide. With SequenceNumber as the final unique key, stability is irrelevant—the requirement is a strict total order.

**Implementation Files**:
- `LlamaBrain/Source/Core/Inference/ContextRetrievalLayer.cs`
- `LlamaBrain/Source/Persona/AuthoritativeMemorySystem.cs` (SequenceNumber assignment)

**Test Files**:
- `LlamaBrain.Tests/Inference/ContextRetrievalLayerTests.cs`

---

### 2. WorkingMemoryConfig Hard Bounds

**Decision**: "Hard bounds" apply to optional sections (dialogue, episodic memories, beliefs). Mandatory sections (canonical facts, world state when `AlwaysInclude=true`) bypass character limits.

**Contract**:
- `MaxContextCharacters` is a soft cap for optional sections.
- Mandatory sections are never truncated.
- **Note**: Assembled prompt may exceed `MaxContextCharacters` when mandatory sections exceed it; this is expected behavior, not a bug.

**Truncation Priority** (when character limit exceeded):
1. Dialogue history (lowest priority - truncated first)
2. Episodic memories (truncated second)
3. Beliefs (truncated third)
4. Canonical facts (never truncated - highest priority)
5. World state (never truncated when `AlwaysIncludeWorldState=true`)

**Section Ordering and Separators** (byte-stability):
- Exact section ordering is canonical and deterministic.
- Exact separators (newline counts) are part of the deterministic surface.
- Empty optional sections are omitted entirely (or included as headers with no content—pick one and lock it).
- Header tokens and newline layout are part of the deterministic surface.

**Implementation Files**:
- `LlamaBrain/Source/Core/Inference/EphemeralWorkingMemory.cs`
- `LlamaBrain/Source/Core/Inference/PromptAssembler.cs`
- `LlamaBrain/Source/Core/Inference/WorkingMemoryConfig.cs`

**Test Files**:
- `LlamaBrain.Tests/Inference/EphemeralWorkingMemoryTests.cs`
- `LlamaBrain.Tests/Inference/PromptAssemblerTests.cs`

---

### 3. ValidationGate Intent Approval

**Decision**: Intents are only approved when `GateResult.Passed=true`. When the gate fails, `ApprovedIntents` is empty.

**Contract**:
- Failed gates do not dispatch intents.
- Intent approval is gated by overall gate pass status.
- Dispatcher only consumes `ApprovedIntents` or controller-emitted intents derived from approved intents.

**Pipeline Policy** (defense in depth):
- Dispatcher consumes intents **only** from `MutationBatchResult.EmittedIntents` (or `GateResult.ApprovedIntents` if still carried), never directly from `ParsedOutput.Intents`.
- Integration test required: Inject intents into `ParsedOutput` on a failing gate and assert **zero dispatches**.

**Implementation Files**:
- `LlamaBrain/Source/Core/Validation/ValidationGate.cs`
- `LlamaBrainRuntime/Runtime/Core/WorldIntentDispatcher.cs`

**Test Files**:
- `LlamaBrain.Tests/Validation/ValidationGateTests.cs`
- `LlamaBrain.Tests/Integration/DeterministicPipelineTests.cs` (Phase 10.7)

---

### 4. OutputParser Extraction

**Decision**: Normalization and extraction stages are pinned with explicit ordering.

**Contract**:
- **If `ExtractStructuredData=false`**: `NormalizeWhitespace(raw)` and return.
- **If `ExtractStructuredData=true`**: `Extract(raw)` → `NormalizeWhitespace(dialogue)`.

**Normalization Rules** (byte-stable):
- CRLF → LF
- Trim trailing whitespace per line
- Collapse 3+ consecutive blank lines to 2
- **Blank line definition**: a line that is empty after trimming trailing whitespace
- **Trailing newline**: `NormalizeWhitespace` does NOT add a newline. It preserves an existing trailing newline if present in input (or always strips—pick one and lock it).
- **Leading blank lines**: Preserved (or stripped—pick one and lock it).
- **BOM handling**: Strips `\uFEFF` (BOM) if present at start. Otherwise "same prompt, different first char" bug will occur.
- **Raw mode semantics**: Raw mode is semantically lossless except for whitespace normalization rules. Whitespace normalization is allowed even in raw mode.

**Extraction Rules**:
- When `ExtractStructuredData=false`, the parser must not remove, rewrite, or interpret any structured markers or JSON fences.
- Only `NormalizeWhitespace` is applied (whitespace normalization is still allowed).
- When `ExtractStructuredData=true`, extraction happens BEFORE normalization (order matters).

**Implementation Files**:
- `LlamaBrain/Source/Core/Validation/OutputParser.cs`

**Test Files**:
- `LlamaBrain.Tests/Validation/OutputParserTests.cs`

---

### 5. Pipeline Determinism

**Decision**: Tests use mocked `IApiClient` for deterministic output, not real LLM determinism.

**Contract**:
- Same prompt → same mocked output (controlled).
- Real LLM determinism is NOT part of the architecture guarantee.
- Determinism proof applies to reconstruction pipeline only.

**Implementation Files**:
- All test files using `IApiClient` mocks

**Test Files**:
- `LlamaBrain.Tests/Integration/FullPipelineIntegrationTests.cs`
- `LlamaBrain.Tests/Integration/DeterministicPipelineTests.cs` (Phase 10.7)

---

## Traceability Matrix

| Contract Decision | Implementation File(s) | Test File(s) | Test Names/Patterns |
|-------------------|----------------------|--------------|---------------------|
| ContextRetrievalLayer Tie-Breaker | `ContextRetrievalLayer.cs`<br>`AuthoritativeMemorySystem.cs` | `ContextRetrievalLayerTests.cs` | `*_TieBreaker_*`<br>`*_DeterministicOrdering_*`<br>`*_SequenceNumber_*` |
| WorkingMemoryConfig Hard Bounds | `EphemeralWorkingMemory.cs`<br>`PromptAssembler.cs`<br>`WorkingMemoryConfig.cs` | `EphemeralWorkingMemoryTests.cs`<br>`PromptAssemblerTests.cs` | `*_HardBounds_*`<br>`*_TruncationPriority_*`<br>`*_MandatorySections_*` |
| ValidationGate Intent Approval | `ValidationGate.cs`<br>`WorldIntentDispatcher.cs` | `ValidationGateTests.cs`<br>`DeterministicPipelineTests.cs` | `*_IntentApproval_*`<br>`*_GatePassStatus_*` |
| OutputParser Extraction | `OutputParser.cs` | `OutputParserTests.cs` | `*_ExtractStructuredData_*`<br>`*_NormalizeWhitespace_*`<br>`*_ExtractionOrder_*` |
| Pipeline Determinism | All pipeline components | `FullPipelineIntegrationTests.cs`<br>`DeterministicPipelineTests.cs` | `*_Deterministic_*`<br>`*_SameInput_*` |

---

## Invariants

### Memory Authority Invariants

1. **Canonical facts cannot be modified** by any mutation type (AppendEpisodic, TransformBelief, TransformRelationship).
2. **Canonical facts cannot be deleted** or overridden by beliefs.
3. **World state mutations** require `MutationSource.GameSystem` authority (not `ValidatedOutput`).
4. **Memory mutations** from validated output use `MutationSource.ValidatedOutput` authority.

### Validation Invariants

1. **Gate execution order** is deterministic: Constraint → Canonical → Knowledge → Mutation → Custom.
2. **Gate failures accumulate** (all gates execute even if earlier gates fail).
3. **Critical failures** skip retry and use fallback immediately.
4. **Mutation validation** happens even if dialogue validation passes.

### Prompt Assembly Invariants

1. **Same StateSnapshot + same constraints = same prompt assembly** (byte-stable).
2. **Mandatory sections** (canonical facts, world state when `AlwaysInclude=true`) are never truncated.
3. **Character limits** apply only to optional sections.
4. **Truncation order** is deterministic: Dialogue → Episodic → Beliefs.
5. **Time-dependent computations** use `SnapshotTimeUtcTicks` captured in `StateSnapshot`. No `DateTime.UtcNow` or `Time.time` during scoring, decay, or pruning.
6. **Score outputs** are rounded before ordering, or ordering uses deterministic tiebreak even for "nearly equal" floats (see Score Computation section).

---

## Comparers and Normalization

### String Comparison

- **Id comparisons**: MUST use `StringComparer.Ordinal` (not culture-sensitive).
- **Rationale**: Ensures determinism across machines/locales.
- **Forbidden**: `ToLower()`, `string.Format` without invariant culture, any culture-sensitive comparisons or formatting.

### Score Computation

- **Time source**: All time-dependent computations use `SnapshotTimeUtcTicks` captured in `StateSnapshot`. No `DateTime.UtcNow` or `Time.time` during scoring, decay, or pruning.
- **Floating-point determinism**: Score outputs are rounded before ordering, or ordering uses deterministic tiebreak even for "nearly equal" floats. Two viable patterns:
  - Convert to `int scoreQ = (int)Math.Round(score * 1_000_000)` and sort by `scoreQ`.
  - Or keep `double` but require that any comparisons use total-order keys afterward and tests include near-equal cases.
- **Forbidden**: Using `DateTime.UtcNow` or `Time.time` during scoring/decay instead of snapshot time.

### Timestamp Comparison

- **CreatedAt comparisons**: Use `CreatedAtTicks` (DateTimeOffset.UtcTicks) for deterministic timestamp comparison.
- **Source**: `CreatedAtTicks = DateTimeOffset.UtcNow.UtcTicks` captured at insertion and persisted; never derived from local time or recomputed.
- **Rationale**: Avoids DateTime comparison issues across timezones and ensures determinism across reloads.

### Sequence Number

- **SequenceNumber**: Monotonic increasing counter assigned at insertion time.
- **Direction**: Lower SequenceNumber means earlier insertion and sorts earlier when all higher-priority keys tie.
- **Rationale**: Provides total order even when timestamps/identifiers are equal.

**Persistence Semantics** (CRITICAL for deterministic reconstruction):
- `SequenceNumber` is persisted as part of each memory entry.
- The memory system counter is persisted as `NextSequenceNumber`.
- **On load**: `NextSequenceNumber = max(SequenceNumber) + 1` (never reassign based on enumeration order).
- **On merge**: Preserve existing SequenceNumbers; new imported entries get new SequenceNumbers assigned in a deterministic order based on `(CreatedAtTicks, Id ordinal, Source)`.
- **Deterministic reconstruction breaks if**: SequenceNumber is not serialized, reassigned on load using enumeration order, or merged non-deterministically.

---

## Verification Handles

### Coverage Metrics
- **Command**: `dotnet test --collect:"XPlat code coverage" --settings coverlet.runsettings`
- **Artifact**: `LlamaBrain/coverage/coverage-analysis.csv`
- **Report**: `LlamaBrain/COVERAGE_REPORT.md`
- **Current**: 92.37% line coverage (5,100 of 5,521 lines), ~95% branch coverage

### Test Counts
- **Command**: `dotnet test --list-tests`
- **Artifact**: Test output
- **Current**: 853+ tests (all passing)
- **FullPipelineIntegrationTests**: 8 tests (see `LlamaBrain.Tests/Integration/FullPipelineIntegrationTests.cs`)

### Doxygen Documentation
- **Command**: `doxygen Documentation/doxygen/llamabrain.Doxyfile`
- **Artifact**: `Documentation/doxygen/html/`
- **Verification**: Zero missing member warnings in Doxygen output

---

## Implementation Requirements

These requirements must be implemented in code (not just tested). Tests will fail if these are not correctly implemented.

### 1. Strict Total Order Sort Algorithm

- **Requirement**: Ordering must be implemented via a comparator chain that forms a strict total order, including `SequenceNumber` as the final unique key.
- **Implementation shape**: Use LINQ `OrderBy().ThenBy()` for all sorting operations (implementation preference, not stability requirement).
- **Reason**: Since every element has a unique final key (`SequenceNumber`), there are no ties. Stability is irrelevant—the requirement is a strict total order. LINQ `OrderBy/ThenBy` provides a clear implementation shape.
- **Location**: `ContextRetrievalLayer.cs` - all memory sorting operations.

### 2. Deterministic Total Order Key with Persistence

- **Requirement**: Every episodic memory and belief entry must have `SequenceNumber` assigned at insertion time from a monotonic counter owned by the memory system.
- **Persistence**: `SequenceNumber` is persisted as part of each memory entry. The memory system counter is persisted as `NextSequenceNumber`.
- **On load**: `NextSequenceNumber = max(SequenceNumber) + 1` (never reassign based on enumeration order).
- **On merge**: Preserve existing SequenceNumbers; new imported entries get new SequenceNumbers assigned in a deterministic order based on `(CreatedAtTicks, Id ordinal, Source)`.
- **Location**: 
  - `AuthoritativeMemorySystem.cs` - add SequenceNumber field, monotonic counter, and persistence logic.
  - `ContextRetrievalLayer.cs` - implemented as secondary sort keys in a single total-order sort.

### 3. Ordinal String Comparison

- **Requirement**: Id comparisons MUST use `StringComparer.Ordinal`.
- **Location**: `ContextRetrievalLayer.cs` - `RetrieveEpisodicMemories()` and `RetrieveBeliefs()`.

### 4. OutputParser Normalization Contract

- **Requirement**: Normalization must be centralized and deterministic. Stage is pinned (see Contract Decision #4).
- **Location**: `OutputParser.cs` - normalization logic.

### 5. WorldIntentDispatcher Singleton Lifecycle

- **Requirement**: 
  - `Awake()` enforces singleton by: If `Instance != null && Instance != this`: disable duplicate and `Destroy(gameObject)` (end-of-frame). Else set `Instance = this`.
  - `OnDestroy()` clears Instance only if `Instance == this`.
- **Lifetime Model** (MUST be pinned):
  - **Option A**: Dispatcher is scene-local; no `DontDestroyOnLoad`; scenes must contain exactly one.
  - **Option B**: Dispatcher is global; uses `DontDestroyOnLoad`; duplicates destroyed.
  - **Pick one and state it explicitly**. Otherwise tests will pass in one setup and fail in another.
- **Location**: `WorldIntentDispatcher.cs` - `Awake()` and `OnDestroy()` methods.

---

---

## Forbidden in Deterministic Path

The following operations are explicitly banned in any code path that contributes to deterministic reconstruction:

- **Dictionary/HashSet enumeration without sorting**: Enumerating `Dictionary<TKey,TValue>` or `HashSet<T>` without sorting first.
- **Guid.ToString() without pinned format**: Using `Guid.ToString()` without pinned format/case (use `"D"` format and uppercase/lowercase policy).
- **Culture-sensitive operations**: Using any culture-sensitive comparisons or formatting (`ToLower()`, `string.Format` without invariant culture).
- **Local time usage**: Using local time (`DateTime.Now`) anywhere in snapshot/memory/scoring.
- **Random without seed**: Using `Random` without seed captured in snapshot (or just ban random entirely in deterministic path).
- **"Now" in scoring**: Using `DateTime.UtcNow` or `Time.time` during scoring, decay, or pruning (must use `SnapshotTimeUtcTicks`).

---

## Phase 10 Additional Test Gaps (High Leverage)

These tests catch common determinism leaks:

### A) No-Now Enforcement Tests
- **Test**: Run the same retrieval/assembly twice with the same snapshot but different wall clock time and assert identical output.
- **Catches**: Accidental `UtcNow` usage in scoring/decay.

### B) Dictionary Order Tripwire Test
- **Test**: Store memories in a dictionary in deliberately shuffled insertion order, then verify output ordering is identical across runs.
- **Forces**: Sorting, not reliance on enumeration order.

### C) Near-Equal Floating Score Tests
- **Test**: Two items whose score differs at ~1e-12, verify deterministic ordering via tie-breakers or quantization rule.
- **Catches**: Floating-point drift issues.

### D) Serialization Round-Trip Determinism
- **Test**: Serialize memory system, reload, run retrieval/assembly, assert identical results including ordering.
- **Catches**: SequenceNumber persistence bugs immediately.

**Location**: Add to `LlamaBrain.Tests/Integration/DeterministicPipelineTests.cs` (Phase 10.7)

---

**Next Review**: After Phase 10 completion (deterministic proof gap testing)
