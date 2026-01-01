# LlamaBrain Pipeline Contract

**Version**: 1.0.0  
**Last Updated**: December 31, 2025
**Status**: Contract decisions documented, implementation in progress

This document defines the formal contract for the LlamaBrain 9-component architectural pipeline. It specifies the exact flow, data contracts, error handling, and behavioral guarantees that all implementations must satisfy.

## Overview

The LlamaBrain pipeline is a deterministic, state-reconstruction architecture that treats the LLM as a pure, stateless generator. This contract ensures that all implementations maintain the critical invariants required for deterministic AI behavior in game systems.

**Core Principle**: The LLM has no memory, no authority, and no direct access to world state. All context is provided through bounded prompts, and all outputs are validated before any state changes occur.

## Pipeline Flow

The pipeline consists of 9 sequential components, executed in strict order:

```
1. Interaction Context → 2. Determinism Layer → 3. Memory Retrieval → 4. State Snapshot
                                                                           ↓
9. Fallback ← 8. Memory Mutation ← 7. Validation ← 6. Inference ← 5. Prompt Assembly
```

### Component Execution Order

1. **Untrusted Observation → Interaction Context** (Component 1)
2. **Determinism Layer (Expectancy Engine)** (Component 2)
3. **External Authoritative Memory System** (Component 3)
4. **Authoritative State Snapshot** (Component 4)
5. **Ephemeral Working Memory** (Component 5)
6. **Stateless Inference Core** (Component 6)
7. **Output Parsing & Validation** (Component 7)
8. **Memory Mutation + World Effects** (Component 8)
9. **Author-Controlled Fallback** (Component 9)

## Component Contracts

### Component 1: Interaction Context

**Purpose**: Capture and structure the trigger that initiates an AI interaction.

**Input Contract**:
- `TriggerReason`: Enum specifying why the interaction was triggered
- `NpcId`: String identifier for the NPC
- `PlayerInput`: Optional string containing player input
- `GameTime`: Float representing game time
- `SceneName`: Optional string for scene context
- `Tags`: Optional list of string tags
- `TriggerPrompt`: Optional string for few-shot examples or trigger-specific prompts

**Output Contract**:
- `InteractionContext`: Immutable object containing all trigger information

**Guarantees**:
- Context is immutable once created
- All required fields are non-null (except optional fields)
- `NpcId` must be a valid identifier

**Error Handling**:
- Invalid `NpcId` → Exception thrown immediately
- Missing required fields → Exception thrown during context creation

**Example**:
```csharp
var context = new InteractionContext
{
    TriggerReason = TriggerReason.PlayerUtterance,
    NpcId = "wizard_001",
    PlayerInput = "Tell me about the ancient tower",
    GameTime = 100f,
    SceneName = "TowerRuins",
    Tags = new List<string> { "quest_related", "lore" }
};
```

---

### Component 2: Determinism Layer (Expectancy Engine)

**Purpose**: Generate constraints based on interaction context that control both prompt assembly and output validation.

**Input Contract**:
- `InteractionContext`: From Component 1
- `IExpectancyRule[]`: Array of rules to evaluate

**Output Contract**:
- `ConstraintSet`: Collection of constraints (Permissions, Prohibitions, Requirements)
- Each constraint has:
  - `Id`: Unique identifier
  - `Type`: Permission, Prohibition, or Requirement
  - `Severity`: Soft, Hard, or Critical
  - `Description`: Human-readable description
  - `PromptInjection`: Text to inject into prompt
  - `Keywords`: List of keywords for validation

**Guarantees**:
- Rules are evaluated deterministically (same context + rules = same constraints)
- Constraints are immutable once generated
- Empty `ConstraintSet` if no rules match

**Error Handling**:
- Invalid rule → Rule is skipped, logged, execution continues
- Rule evaluation exception → Rule is skipped, logged, execution continues

**Constraint Escalation** (for retries):
- `None`: No escalation
- `AddSpecificProhibition`: Add prohibition for specific violation
- `HardenRequirements`: Increase severity of requirements
- `Full`: Combine all escalation modes

**Example**:
```csharp
var constraints = expectancyEvaluator.Evaluate(context, rules);
// constraints contains all matching rule constraints
```

---

### Component 3: External Authoritative Memory System

**Purpose**: Retrieve relevant memories from the authoritative memory system.

**Input Contract**:
- `AuthoritativeMemorySystem`: Memory system for the NPC
- `string`: Player input (for relevance calculation)
- `ContextRetrievalConfig`: Optional configuration for retrieval limits

**Output Contract**:
- `RetrievedContext`: Object containing:
  - `CanonicalFacts`: List of immutable world truths (always included)
  - `WorldState`: Dictionary of mutable game state (always included)
  - `EpisodicMemories`: List of conversation/event memories (filtered by relevance)
  - `Beliefs`: List of NPC opinions/relationships (filtered by confidence)

**Guarantees**:
- Canonical facts are ALWAYS included (no filtering)
- World state is ALWAYS included (no filtering)
- Episodic memories are filtered by:
  - Recency (more recent prioritized)
  - Relevance (keyword matching against player input)
  - Significance (higher significance retained longer)
- Beliefs are filtered by confidence threshold
- Retrieval limits prevent token bloat

**Error Handling**:
- Memory system unavailable → Empty `RetrievedContext` returned, execution continues
- Invalid memory entry → Entry is skipped, logged, execution continues

**Example**:
```csharp
var retrievalLayer = new ContextRetrievalLayer(memorySystem);
var retrievedContext = retrievalLayer.RetrieveContext(playerInput);
// retrievedContext contains filtered, relevant memories
```

---

### Component 4: Authoritative State Snapshot

**Purpose**: Create an immutable snapshot of all context at inference time for deterministic retries.

**Input Contract**:
- `InteractionContext`: From Component 1
- `ConstraintSet`: From Component 2
- `RetrievedContext`: From Component 3
- `string`: System prompt
- `string`: Player input
- `int`: Attempt number (0-based)
- `int`: Max attempts

**Output Contract**:
- `StateSnapshot`: Immutable object containing:
  - All context from Components 1-3
  - System prompt
  - Player input
  - Attempt number
  - Max attempts
  - Timestamp

**Guarantees**:
- Snapshot is immutable (cannot be modified after creation)
- Same inputs = same snapshot (deterministic)
- Snapshot can be replayed exactly for retries
- `ForRetry()` method creates new snapshot with merged constraints

**Error Handling**:
- Missing required fields → Exception thrown during snapshot creation
- Invalid attempt number → Exception thrown (must be >= 0 and < max attempts)

**Retry Support**:
```csharp
var retrySnapshot = snapshot.ForRetry(escalatedConstraints, attemptNumber: snapshot.AttemptNumber + 1);
// Creates new snapshot with merged constraints (original + escalated)
```

**Example**:
```csharp
var snapshot = new StateSnapshotBuilder()
    .WithContext(context)
    .WithConstraints(constraints)
    .Apply(retrievedContext)
    .WithSystemPrompt(profile.SystemPrompt)
    .WithPlayerInput(playerInput)
    .WithAttemptNumber(0)
    .WithMaxAttempts(3)
    .Build();
```

---

### Component 5: Ephemeral Working Memory

**Purpose**: Create a bounded, token-efficient prompt from the snapshot that exists only for the current inference.

**Input Contract**:
- `StateSnapshot`: From Component 4
- `WorkingMemoryConfig`: Configuration for bounds
- `PromptAssemblerConfig`: Configuration for prompt formatting

**Output Contract**:
- `AssembledPrompt`: Object containing:
  - `PromptText`: Final bounded prompt string
  - `WorkingMemory`: Ephemeral working memory (implements `IDisposable`)

**Guarantees**:
- Prompt is bounded according to `WorkingMemoryConfig`:
  - Max exchanges (dialogue history)
  - Max memories (episodic memories)
  - Max beliefs (beliefs)
  - Max characters (total character limit)
- Priority order for truncation:
  1. Canonical facts (always included, never truncated)
  2. World state (always included, never truncated)
  3. Recent episodic memories (by significance)
  4. High-confidence beliefs
- Working memory is disposed after inference
- Prompt assembly is deterministic (same snapshot = same prompt)

**Error Handling**:
- Invalid config → Default config used, logged, execution continues
- Truncation required → Silent truncation, statistics tracked

**Example**:
```csharp
var assembler = new PromptAssembler(PromptAssemblerConfig.Default);
var assembledPrompt = assembler.AssembleFromSnapshot(snapshot, npcName: "Wizard");
// assembledPrompt.PromptText contains bounded prompt
// assembledPrompt.WorkingMemory will be disposed after use
```

---

### Component 6: Stateless Inference Core

**Purpose**: Pure stateless text generation - the LLM has no memory, authority, or world access.

**Input Contract**:
- `string`: Bounded prompt from Component 5
- `int?`: Optional max tokens
- `float?`: Optional temperature
- `CancellationToken`: Cancellation token

**Output Contract**:
- `string`: Raw LLM response text

**Guarantees**:
- LLM is pure function (same input = same output when constraints are met)
- LLM has no memory (receives only the bounded prompt)
- LLM has no authority (cannot directly modify any state)
- LLM has no world access (no direct memory or state access)
- Response is untrusted (must be validated before use)

**Error Handling**:
- API failure → Exception thrown, retry logic handles
- Timeout → Exception thrown, retry logic handles
- Invalid response → Returned as-is, validation will catch

**Example**:
```csharp
var response = await apiClient.SendPromptAsync(
    assembledPrompt.PromptText,
    maxTokens: null,
    temperature: null,
    cancellationToken: CancellationToken.None
);
// response is untrusted - must be validated
```

---

### Component 7: Output Parsing & Validation

**Purpose**: Parse LLM output and validate it against constraints and canonical facts before any state changes.

**Input Contract**:
- `string`: Raw LLM response from Component 6
- `ValidationContext`: Contains:
  - `ConstraintSet`: From Component 2
  - `AuthoritativeMemorySystem`: For canonical fact checking
  - `StateSnapshot`: From Component 4
- `IValidationRule[]`: Optional validation rules (Global, NPC-specific, Trigger-specific)

**Output Contract**:
- `GateResult`: Object containing:
  - `Passed`: Boolean indicating if validation passed
  - `Failures`: List of validation failures (if not passed)
  - `ValidatedOutput`: Parsed output (if passed)
  - `ApprovedMutations`: List of approved memory mutations
  - `RejectedMutations`: List of rejected memory mutations
  - `ApprovedIntents`: List of approved world intents

**Validation Pipeline** (executed in order):

1. **Output Parsing**:
   - Extracts dialogue text
   - Parses proposed mutations (`[MUTATION: ...]`)
   - Extracts world intents (`[INTENT: ...]` or `[ACTION: ...]`)
   - Handles malformed outputs gracefully

2. **Validation Gates** (all must pass):
   - **Constraint Validation**: Checks against expectancy engine constraints
   - **Canonical Fact Validation**: Ensures no contradictions with immutable facts
   - **Knowledge Boundary Validation**: Prevents revealing forbidden knowledge
   - **Mutation Validation**: Validates proposed memory changes

3. **Validation Rule Levels** (all must pass):
   - **Global Rules**: World-level rules (if `ValidationPipeline` present)
   - **NPC-Specific Rules**: Rules assigned to `LlamaBrainAgent`
   - **Trigger-Specific Rules**: Rules assigned to trigger component

**Guarantees**:
- Parsing failures result in `GateResult.Passed = false`
- All validation gates must pass for `GateResult.Passed = true`
- All validation rule levels must pass for `GateResult.Passed = true`
- Failures from all levels are combined in `GateResult.Failures`
- No state changes occur if validation fails

**Error Handling**:
- Parsing error → `GateResult.Passed = false`, `ParsedOutput.Success = false`
- Validation exception → `GateResult.Passed = false`, exception logged
- Invalid mutation → Mutation added to `RejectedMutations`, validation continues

**Retry Logic**:
- If `GateResult.Passed = false` and `snapshot.CanRetry`:
  - Create retry snapshot with escalated constraints
  - Increment attempt number
  - Return to Component 4/5 for retry
- Max attempts exceeded → Proceed to Component 9 (Fallback)

**Example**:
```csharp
var parser = new OutputParser();
var parsedOutput = parser.Parse(llmResponse);

var validationContext = new ValidationContext
{
    Constraints = constraints,
    MemorySystem = memorySystem,
    Snapshot = snapshot
};

var gateResult = validationGate.Validate(parsedOutput, validationContext);

if (!gateResult.Passed)
{
    // Retry or fallback
    foreach (var failure in gateResult.Failures)
    {
        Debug.Log($"Validation failed: {failure.Reason} - {failure.Description}");
    }
}
```

---

### Component 8: Memory Mutation + World Effects

**Purpose**: Execute validated mutations and dispatch world intents, with strict authority enforcement.

**Input Contract**:
- `GateResult`: From Component 7 (must have `Passed = true`)
- `AuthoritativeMemorySystem`: Memory system for the NPC
- `string`: NPC ID

**Output Contract**:
- `MutationResult`: Object containing:
  - `TotalAttempted`: Number of mutations attempted
  - `SuccessCount`: Number of successful mutations
  - `FailureCount`: Number of failed mutations
  - `AllSucceeded`: Boolean indicating if all mutations succeeded
  - `Failures`: List of mutation failures

**Mutation Types**:
- `AppendEpisodic`: Add conversation/event to episodic memory
- `TransformBelief`: Update or create NPC belief/opinion
- `TransformRelationship`: Update relationship with another entity
- `EmitWorldIntent`: Dispatch world-affecting intent to game systems

**Guarantees**:
- Only executed if `GateResult.Passed = true`
- Canonical facts cannot be overridden (blocked with statistics)
- World state requires `GameSystem` authority
- All mutation attempts are logged
- Mutations are executed atomically (all or nothing per mutation type)

**Authority Enforcement**:
- Canonical facts: Blocked, logged, statistics tracked
- World state: Requires `MutationSource.GameSystem`
- Episodic memory: Allowed with `MutationSource.ValidatedOutput`
- Beliefs: Allowed with `MutationSource.ValidatedOutput`

**World Intents**:
- Dispatched via `WorldIntentDispatcher` (Unity component)
- Can trigger quest events, spawn items, change world state, etc.
- Intents are approved during validation (Component 7)

**Error Handling**:
- Invalid mutation → Mutation fails, logged, execution continues
- Authority violation → Mutation blocked, logged, statistics tracked
- World intent dispatch failure → Logged, execution continues

**Example**:
```csharp
if (gateResult.Passed)
{
    var mutationResult = mutationController.ExecuteMutations(
        gateResult,
        memorySystem,
        npcId
    );

    if (mutationResult.AllSucceeded)
    {
        Debug.Log($"All {mutationResult.TotalAttempted} mutations succeeded");
    }

    // Dispatch world intents
    foreach (var intent in gateResult.ApprovedIntents)
    {
        WorldIntentDispatcher.Instance.DispatchIntent(intent);
    }
}
```

---

### Component 9: Author-Controlled Fallback

**Purpose**: Provide context-aware fallback responses when all retry attempts fail.

**Input Contract**:
- `InteractionContext`: From Component 1
- `string`: Failure reason
- `FallbackConfig`: Configuration for fallback responses

**Output Contract**:
- `string`: Fallback response text

**Guarantees**:
- Always returns a non-empty string
- Never corrupts state (no mutations executed)
- Context-aware selection based on `TriggerReason`
- Statistics tracked for debugging

**Fallback Hierarchy**:
1. **Context-Aware Fallbacks**: Selected based on `TriggerReason`
   - `PlayerUtterance` → Generic conversation fallbacks
   - `ZoneTrigger` → Location-specific fallbacks
   - `QuestTrigger` → Quest-related fallbacks
   - etc.
2. **Generic Fallbacks**: Used when no context-specific fallback matches
3. **Emergency Fallbacks**: Always available as last resort

**Error Handling**:
- No fallback configured → Emergency fallback used
- Fallback selection exception → Emergency fallback used

**Example**:
```csharp
var fallbackResponse = fallbackSystem.GetFallbackResponse(
    context,
    "Validation failed after max retries"
);
// fallbackResponse is guaranteed to be non-empty
```

---

## Retry Contract

### Retry Conditions

Retries are triggered when:
1. `GateResult.Passed = false` (validation failed)
2. `snapshot.AttemptNumber < snapshot.MaxAttempts` (attempts remaining)
3. `snapshot.CanRetry = true` (retry is allowed)

### Retry Process

1. **Constraint Escalation**: Create escalated constraints based on failure
2. **Snapshot Creation**: Create new snapshot with `ForRetry()`:
   ```csharp
   var retrySnapshot = snapshot.ForRetry(escalatedConstraints, attemptNumber: snapshot.AttemptNumber + 1);
   ```
3. **Re-execution**: Return to Component 4/5 with retry snapshot
4. **Max Attempts**: If `attemptNumber >= maxAttempts`, proceed to Component 9

### Retry Guarantees

- Same snapshot inputs = same retry snapshot (deterministic)
- Constraints are merged (original + escalated)
- Attempt number is incremented
- Max attempts limit is enforced

---

## Error Handling Contract

### Error Categories

1. **Fatal Errors**: Stop pipeline execution immediately
   - Invalid `NpcId`
   - Missing required fields in `InteractionContext`
   - Memory system unavailable (if critical)

2. **Recoverable Errors**: Log and continue execution
   - Invalid rule → Rule skipped
   - Invalid memory entry → Entry skipped
   - Parsing error → Validation fails, retry triggered
   - Validation failure → Retry triggered

3. **Silent Errors**: Log only, no user-visible impact
   - Truncation required → Silent truncation
   - Invalid config → Default used

### Error Propagation

- **Component 1-4**: Fatal errors propagate immediately (exception thrown)
- **Component 5**: Recoverable errors → Default config used
- **Component 6**: API errors → Exception thrown, retry logic handles
- **Component 7**: Validation errors → `GateResult.Passed = false`, retry triggered
- **Component 8**: Mutation errors → `MutationResult` contains failures
- **Component 9**: Fallback errors → Emergency fallback used

---

## State Mutation Contract

### Mutation Authority

**Canonical Facts** (Highest Authority):
- Cannot be modified by any source
- Attempts to override are blocked and logged
- Statistics tracked for debugging

**World State** (High Authority):
- Requires `MutationSource.GameSystem` authority
- Cannot be modified by validated outputs
- Only game systems can update

**Episodic Memory** (Medium Authority):
- Can be appended by validated outputs
- Requires `MutationSource.ValidatedOutput`
- Automatic decay based on significance

**Belief Memory** (Lowest Authority):
- Can be modified by validated outputs
- Requires `MutationSource.ValidatedOutput`
- Can be wrong, can be contradicted
- Confidence-based filtering

### Mutation Execution

- Mutations are executed only if `GateResult.Passed = true`
- Mutations are executed atomically (all or nothing per mutation type)
- Failed mutations are logged and tracked in `MutationResult`
- Authority violations are blocked and logged

---

## Determinism Guarantees

### Deterministic Components

The following components are guaranteed to be deterministic:

1. **Component 2** (Determinism Layer): Same context + rules = same constraints
2. **Component 3** (Memory Retrieval): Same input + memory state = same retrieval
3. **Component 4** (State Snapshot): Same inputs = same snapshot
4. **Component 5** (Prompt Assembly): Same snapshot = same prompt
5. **Component 7** (Validation): Same input + constraints = same validation result

### Non-Deterministic Components

The following components are non-deterministic (by design):

1. **Component 6** (Inference Core): LLM generation is stochastic
   - **Mitigation**: Validation gate ensures only valid outputs proceed
   - **Future**: Feature 14 (Deterministic Generation Seed) will make this deterministic

### Deterministic Retries

- Same snapshot + same constraints = same retry snapshot
- Retry snapshots are deterministic (can be replayed exactly)
- Constraint escalation is deterministic

---

## Performance Contract

### Token Budget

- **Component 5** (Prompt Assembly): Bounded by `WorkingMemoryConfig`
  - Default: 2000 characters
  - Minimal: 1000 characters
  - Expanded: 4000 characters

### Time Limits

- **Component 6** (Inference): Configurable timeout
- **Retry Loop**: Configurable time limit (`RetryPolicy.TimeLimitSeconds`)
- **Total Pipeline**: No explicit limit (game-dependent)

### Resource Limits

- **Memory Retrieval**: Configurable limits (`ContextRetrievalConfig`)
- **Prompt Assembly**: Character-based truncation
- **Validation**: No limits (fast operation)

---

## Testing Contract

### Integration Test Requirements

All implementations must pass the following integration tests (see `FullPipelineIntegrationTests.cs`):

1. **Happy Path**: `FullPipeline_ValidInput_CompletesSuccessfully`
   - Valid input → Successful completion
   - All components execute in order
   - Validation passes
   - Mutations execute

2. **Constraint Enforcement**: `FullPipeline_WithConstraints_EnforcesConstraints`
   - Constraints are generated
   - Constraints are enforced during validation
   - Violations are caught

3. **Canonical Fact Protection**: `FullPipeline_WithCanonicalFacts_ProtectsFacts`
   - Canonical facts are protected
   - Contradictions are caught
   - Facts remain intact after failed validation

4. **Retry Logic**: `FullPipeline_InvalidOutput_TriggersRetry`
   - Invalid output triggers retry
   - Retry uses escalated constraints
   - Retry can succeed

5. **Fallback System**: `FullPipeline_MaxRetriesExceeded_UsesFallback`
   - Max retries exceeded → Fallback used
   - Fallback provides response
   - No state corruption

6. **Memory Mutation**: `FullPipeline_MemoryMutation_ExecutesApprovedMutations`
   - Approved mutations execute
   - Memory is updated
   - Mutations are tracked

7. **Context Retrieval**: `FullPipeline_ContextRetrieval_IncludesRelevantMemories`
   - Relevant memories are retrieved
   - Canonical facts included
   - World state included

8. **World Intent**: `FullPipeline_WorldIntent_DispatchesIntent`
   - World intents are parsed
   - Intents are approved
   - Intents are dispatched

---

## Version History

- **1.0.0** (December 31, 2025): Initial contract definition

---

## Further Reading

- [README.md](../LlamaBrain/README.md) - Main library documentation and overview
- [ARCHITECTURE.md](ARCHITECTURE.md) - Complete architectural documentation explaining the 9-component pattern
- [MEMORY.md](MEMORY.md) - Memory system documentation (Component 3)
- [VALIDATION_GATING.md](VALIDATION_GATING.md) - Validation system documentation (Component 7)
- [USAGE_GUIDE.md](USAGE_GUIDE.md) - Practical examples and best practices
- [ROADMAP.md](ROADMAP.md) - Implementation status and planned features
- [FullPipelineIntegrationTests.cs](../LlamaBrain/LlamaBrain.Tests/Integration/FullPipelineIntegrationTests.cs) - Integration test suite
