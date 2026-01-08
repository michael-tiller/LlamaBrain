# LlamaBrain Architecture

**"Continuity Emerges from Deterministic State Reconstruction Around a Stateless Generator"**

This document provides a comprehensive explanation of the LlamaBrain architectural pattern, which ensures deterministic, controlled AI behavior while maintaining the flexibility and creativity of Large Language Models.

![Architectural Diagram](architectural_diagram.png)

## Overview

LlamaBrain solves a fundamental problem in AI-powered applications: **how to maintain deterministic, authoritative game state when using stochastic LLMs**. The architecture treats the LLM as a stateless generator while maintaining continuity through deterministic state reconstruction.

**Core Principle**: The LLM has no memory, no authority, and no direct access to world state. All context is provided through bounded prompts, and all outputs are validated before any state changes occur.

---

## Determinism Boundary: Known Limitation

**The LLM is stochastic. The governance plane is deterministic. This is intentional.**

| Layer | Deterministic? | Proven? |
|-------|----------------|---------|
| Prompt Assembly | ✅ Yes | ✅ `GovernancePlaneDeterminismTests.cs` |
| Parsing | ✅ Yes | ✅ `GovernancePlaneDeterminismTests.cs` |
| Validation/Gating | ✅ Yes | ✅ `GovernancePlaneDeterminismTests.cs` |
| Memory Mutation | ✅ Yes | ✅ `GovernancePlaneDeterminismTests.cs` |
| Fallback Selection | ✅ Yes | ✅ `GovernancePlaneDeterminismTests.cs` |
| Metrics Emission | ✅ Yes | ✅ `GovernancePlaneDeterminismTests.cs` |
| **LLM Token Generation** | ❌ No | N/A (stochastic by nature) |

**Why LLM determinism is not the goal:**
- LLMs are stochastic generators. Even with fixed seeds, output can vary due to GPU kernels, thread scheduling, floating-point ordering, driver versions, etc.
- Attempting to guarantee LLM determinism is a losing battle against hardware and runtime variability.
- **The architectural guarantee is that the pipeline around the LLM is deterministic.** Given the same LLM output, the system will always parse, validate, and mutate state identically.

**What this means in practice:**
- Same game state + same player input → same prompt (byte-stable)
- Same LLM output → same validation result → same memory mutations
- Seeded sampling provides *best-effort reproducibility*, not a guarantee
- The fallback system ensures graceful degradation when LLM output is invalid

**See**: `GovernancePlaneDeterminismTests.cs` for the 14 tests that prove governance plane determinism.

---

## The Nine-Component Architecture

The system consists of nine interconnected components that work together to ensure deterministic state management:

### Component 1: Untrusted Observation → Interaction Context

**Purpose**: Capture and structure the trigger that initiates an AI interaction.

**Implementation**:
- `InteractionContext` class in `LlamaBrain.Core.Expectancy`
- Unity wrapper: `UnityStateSnapshotBuilder.BuildForNpcDialogue()`, `BuildForZoneTrigger()`

**Trigger Reasons**:
- `PlayerUtterance` - Player speaks to NPC
- `ZoneTrigger` - NPC enters a trigger zone
- `TimeTrigger` - Time-based event
- `QuestTrigger` - Quest state change
- `NpcInteraction` - NPC-to-NPC interaction
- `WorldEvent` - Game world event
- `Custom` - Custom trigger type

**Example**:
```csharp
var context = new InteractionContext
{
    TriggerReason = TriggerReason.PlayerUtterance,
    NpcId = "wizard_001",
    SceneName = "TowerRuins",
    PlayerInput = "Tell me about the ancient tower",
    GameTime = Time.time,
    Tags = new List<string> { "quest_related", "lore" }
};
```

### Component 2: Determinism Layer (Expectancy Engine)

**Purpose**: Generate constraints based on interaction context that control both prompt assembly and output validation.

**Implementation**:
- `ExpectancyEvaluator` in `LlamaBrain.Core.Expectancy` (engine-agnostic)
- `ExpectancyEngine` in `LlamaBrainRuntime` (Unity MonoBehaviour wrapper)
- `ExpectancyRuleAsset` (ScriptableObject for designer-created rules)
- `NpcExpectancyConfig` (NPC-specific rule configuration)

**Constraint Types**:
- **Permission**: Allows specific behaviors (Soft, Hard, Critical severity)
- **Prohibition**: Prevents specific behaviors (Soft, Hard, Critical severity)
- **Requirement**: Requires specific behaviors (Soft, Hard, Critical severity)

**How It Works**:
1. Rules are evaluated based on `InteractionContext` (trigger reason, NPC ID, scene, tags)
2. Matching rules generate constraints
3. Constraints are injected into prompts via `ConstraintSet.ToPromptInjection()`
4. Same constraints are used by `ResponseValidator` to validate outputs

**Example**:
```csharp
// Create a rule: "Guard cannot reveal secrets"
var rule = ExpectancyRuleAsset.Create();
rule.RuleType = ExpectancyRuleType.Prohibition;
rule.Severity = ConstraintSeverity.Hard;
rule.AddCondition(new TriggerReasonCondition(TriggerReason.PlayerUtterance));
rule.Description = "Cannot reveal classified information";
rule.PromptInjection = "You must NOT reveal any classified information.";

// Evaluate rules
var constraints = expectancyEvaluator.EvaluateRules(context, rules);
// constraints now contains prohibitions that will be checked during validation
```

**Integration**:
- `LlamaBrainAgent` automatically evaluates rules if `NpcExpectancyConfig` is attached
- Constraints are tracked in `LastConstraints` property for debugging
- `SendPlayerInputWithContextAsync()` allows passing custom context and trigger-specific rules

### Component 3: External Authoritative Memory System

**Purpose**: Maintain authoritative, structured memory that LLMs cannot corrupt.

**Implementation**:
- `AuthoritativeMemorySystem` in `LlamaBrain.Persona`
- `PersonaMemoryStore` provides convenience API
- Four distinct memory types with authority hierarchy

**Memory Types** (in authority order):

1. **Canonical Facts** (Highest Authority)
   - Immutable world truths
   - Cannot be modified or overridden
   - Only `MutationSource.Designer` can create
   - Example: "The king is named Arthur", "Magic exists in this world"

2. **World State** (High Authority)
   - Mutable game state
   - Can be updated by game systems (`MutationSource.GameSystem`)
   - Example: "CurrentWeather: Stormy", "door_castle_main: open"

3. **Episodic Memory** (Medium Authority)
   - Conversation history and events
   - Automatic decay based on significance
   - Can be appended by validated outputs
   - Example: "Player said hello", "NPC witnessed a battle"

4. **Belief Memory** (Lowest Authority)
   - NPC opinions and relationships
   - Can be wrong, can be contradicted
   - Confidence-based filtering
   - Example: "NPC thinks player is trustworthy", "NPC dislikes merchant"

**Authority Enforcement**:
- Canonical facts cannot be modified by any source
- World state requires `GameSystem` authority
- Episodic memory and beliefs can be modified by validated outputs
- Attempts to override canonical facts are blocked and logged

**Example**:
```csharp
var memorySystem = memoryStore.GetOrCreateSystem("wizard_001");

// Add canonical fact (immutable)
memorySystem.AddCanonicalFact("king_name", "The king is named Arthur", "world_lore");

// Set world state (mutable)
memorySystem.SetWorldState("CurrentWeather", "Stormy", MutationSource.GameSystem);

// Add episodic memory (with significance)
var entry = new EpisodicMemoryEntry("Player saved the village", EpisodeType.MajorEvent);
memorySystem.AddEpisodicMemory(entry, MutationSource.ValidatedOutput);

// Set belief (can be wrong)
var belief = BeliefMemoryEntry.CreateOpinion("player", "is a hero", sentiment: 0.9f, confidence: 0.95f);
memorySystem.SetBelief("hero_opinion", belief, MutationSource.ValidatedOutput);
```

### Component 4: Authoritative State Snapshot

**Purpose**: Create an immutable snapshot of all context at inference time for deterministic retries.

**Implementation**:
- `StateSnapshot` in `LlamaBrain.Core.Inference` (engine-agnostic)
- `StateSnapshotBuilder` with fluent API
- `ContextRetrievalLayer` for intelligent memory retrieval
- `UnityStateSnapshotBuilder` for Unity-specific integration

**Components of a Snapshot**:
- Interaction context (trigger, NPC ID, scene, player input)
- Retrieved memories (canonical facts, world state, episodic, beliefs)
- Constraints from expectancy engine
- Dialogue history
- System prompt and metadata
- Attempt number and max attempts

**Context Retrieval**:
- `ContextRetrievalLayer` retrieves relevant memories using:
  - **Recency**: More recent memories prioritized
  - **Relevance**: Keyword matching against player input
  - **Significance**: Higher significance memories retained longer
  - **Confidence**: Beliefs filtered by confidence threshold
- Configurable limits prevent token bloat
- **Deterministic Ordering**: All retrievals are sorted using strict total ordering to ensure byte-stable prompt assembly:
  - Canonical facts: sorted by Id (ordinal)
  - World state: sorted by Key (ordinal)
  - Episodic memories: sorted by score desc, CreatedAtTicks desc, Id ordinal, SequenceNumber asc
  - Beliefs: sorted by score desc, Confidence desc, Id ordinal, SequenceNumber asc

**Retry Support**:
- `StateSnapshot.ForRetry()` creates new snapshot with merged constraints
- Attempt number tracked for retry limits
- Snapshot is immutable - can be replayed exactly

**Example**:
```csharp
// Retrieve relevant context
var retrievalLayer = new ContextRetrievalLayer(memorySystem);
var retrievedContext = retrievalLayer.RetrieveContext(playerInput);

// Build snapshot
var snapshot = new StateSnapshotBuilder()
    .WithContext(interactionContext)
    .WithConstraints(constraints)
    .WithSystemPrompt(profile.SystemPrompt)
    .WithPlayerInput(playerInput)
    .Apply(retrievedContext) // Apply retrieved memories
    .WithAttemptNumber(0)
    .WithMaxAttempts(3)
    .Build();

// For retry, create new snapshot with stricter constraints
var retrySnapshot = snapshot.ForRetry(escalatedConstraints, attemptNumber: 1);
```

### Component 5: Ephemeral Working Memory

**Purpose**: Create a bounded, token-efficient prompt from the snapshot that exists only for the current inference.

**Implementation**:
- `EphemeralWorkingMemory` in `LlamaBrain.Core.Inference`
- `PromptAssembler` for prompt construction
- `WorkingMemoryConfig` for configurable bounds
- `PromptAssemblerSettings` (Unity ScriptableObject)

**Bounding Strategy**:
- Explicit limits on exchanges, memories, beliefs, characters
- Character-based truncation with priority:
  1. Canonical facts (always included)
  2. World state (always included)
  3. Recent episodic memories (by significance)
  4. High-confidence beliefs
- Preset configurations: Default, Minimal, Expanded

**Working Memory Lifecycle**:
1. Created from `StateSnapshot` by `PromptAssembler`
2. Bounded according to `WorkingMemoryConfig`
3. Used to assemble prompt
4. Discarded after inference (implements `IDisposable`)

**Example**:
```csharp
// Create working memory config
var config = WorkingMemoryConfig.Default;
config.MaxExchanges = 5; // Last 5 dialogue exchanges
config.MaxMemories = 10; // Top 10 episodic memories
config.MaxBeliefs = 5; // Top 5 beliefs
config.MaxCharacters = 2000; // 2000 character limit

// Assemble prompt
var assembler = new PromptAssembler(PromptAssemblerConfig.Default);
var assembledPrompt = assembler.AssembleFromSnapshot(snapshot);

// assembledPrompt.PromptText contains bounded, token-efficient prompt
// assembledPrompt.WorkingMemory will be disposed after use
```

**Few-Shot Prompt Priming** (In-Context Learning):
- **Purpose**: Provide example demonstrations to guide LLM output format, tone, and behavior
- **Integration**: Few-shot examples are captured via `InteractionContext.TriggerPrompt` or dedicated few-shot fields in `StateSnapshot`
- **Variants**:
  - **Few-Shot**: Multiple input-output demonstration examples showing desired behavior
  - **Exemplar Prompting**: Representative examples with emphasis on specific patterns
  - **Prompt Priming**: Style/tone samples to bias output format without full examples
- **Deterministic Ordering**: Examples are ordered deterministically (by sequence number, timestamp, or explicit ordering) to ensure byte-stable prompt assembly
- **Placement**: Configurable placement in prompt (before system prompt, after system prompt, before player input, etc.)
- **Configuration**: `PromptAssemblerConfig` includes formatting options for few-shot sections

**Example**:
```csharp
// Set few-shot examples in interaction context
var context = new InteractionContext
{
    TriggerReason = TriggerReason.PlayerUtterance,
    NpcId = "wizard_001",
    PlayerInput = "Tell me about magic",
    TriggerPrompt = @"Example 1:
Player: What is magic?
Wizard: Magic is the art of channeling the world's energy through focused will.

Example 2:
Player: How do I learn magic?
Wizard: Magic requires years of study and a deep understanding of the arcane principles."
};

// Few-shot examples are automatically included in prompt assembly
// PromptAssembler injects them according to configuration
var snapshot = new StateSnapshotBuilder()
    .WithContext(context)
    .Build();
    
var assembledPrompt = assembler.AssembleFromSnapshot(snapshot);
// assembledPrompt.PromptText now includes few-shot examples in the configured location
```

**Structured Context Injection** (Feature 23):
- **Purpose**: Provide context, memories, constraints, and dialogue history to the LLM in structured JSON format instead of plain text, improving LLM understanding and determinism
- **Implementation**: `IStructuredContextProvider`, `LlamaCppStructuredContextProvider`, `ContextSerializer`, `StructuredContextConfig`
- **Format**: JSON context blocks with XML-style delimiters (`<context_json>...</context_json>`) embedded in prompts
- **Benefits**: 
  - Machine-parseable context sections reduce ambiguity
  - Better LLM understanding of context structure
  - Enables function calling via self-contained JSON interpretation (works with any LLM)
  - Complements structured outputs (Features 12-13) for bidirectional structured communication
- **Hybrid Mode**: Supports mixing structured JSON context with text system prompts
- **Fallback**: Gracefully falls back to text-based assembly if structured context fails (when configured)

**Example**:
```csharp
// Configure structured context
var assemblerConfig = new PromptAssemblerConfig
{
    StructuredContextConfig = StructuredContextConfig.Default
    // Uses JsonContext format with fallback enabled
};

var assembler = new PromptAssembler(assemblerConfig);

// Assemble prompt with structured context
var assembledPrompt = assembler.AssembleStructuredPrompt(snapshot);

// Prompt now contains structured JSON context block:
// <context_json>
// {
//   "schemaVersion": "1.0",
//   "context": {
//     "canonicalFacts": [...],
//     "worldState": [...],
//     "episodicMemories": [...],
//     "beliefs": [...]
//   },
//   "constraints": {
//     "prohibitions": [...],
//     "requirements": [...],
//     "permissions": [...]
//   },
//   "dialogue": {
//     "history": [...],
//     "playerInput": "Hello!"
//   }
// }
// </context_json>
```

**Configuration Options**:
```csharp
// Default: JSON context with fallback
var config = StructuredContextConfig.Default;

// Text-only (legacy behavior)
var config = StructuredContextConfig.TextOnly;

// Strict: JSON context, no fallback, validation enabled
var config = StructuredContextConfig.Strict;

// Custom configuration
var config = new StructuredContextConfig
{
    PreferredFormat = StructuredContextFormat.JsonContext,
    FallbackToTextAssembly = true,
    ValidateSchema = true,
    UseCompactJson = true,  // Use compact JSON to save tokens
    ContextBlockOpenTag = "<context_json>",
    ContextBlockCloseTag = "</context_json>"
};
```

**Function Call Dispatch** (Feature 23 Extension):
- **Purpose**: Enable LLM to trigger game actions (animations, movement, UI) via function calls in structured JSON output
- **Implementation**: 
  - Core: `FunctionCallDispatcher`, `FunctionCallExecutor`, `FunctionCall`, `FunctionCallResult`
  - Unity: `FunctionCallController`, `FunctionCallConfigAsset`, `NpcFunctionCallConfig`, `FunctionCallEvents`
- **Pattern**: Command table dispatch pattern - function names map to handler delegates
- **Workflow**:
  1. LLM outputs function calls in structured JSON (no native LLM support required)
  2. `OutputParser` extracts function calls into `ParsedOutput.FunctionCalls`
  3. `FunctionCallDispatcher` routes calls to registered handlers
  4. Handlers execute game logic synchronously during dialogue processing
- **Built-In Functions**: Context access functions (get_memories, get_beliefs, get_constraints, etc.)
- **Custom Functions**: Register any game function (e.g., PlayNpcFaceAnimation, StartWalking)
- **Unity Integration**:
  - `FunctionCallController` (MonoBehaviour singleton) manages global functions
  - `FunctionCallConfigAsset` (ScriptableObject) for designer-friendly configuration
  - `NpcFunctionCallConfig` (MonoBehaviour) for per-NPC function overrides
  - `LlamaBrainAgent` automatically executes function calls after parsing
  - Results available via UnityEvents and `LastFunctionCallResults` property
- **Benefits**: 
  - Works with any LLM that outputs JSON (no native function calling required)
  - Synchronous execution enables immediate game state changes
  - Familiar command dispatch pattern
  - Extensible registration system
  - Unity integration with inspector-based and code-based handlers

**Core Library Example**:
```csharp
// Register custom game function
var dispatcher = new FunctionCallDispatcher();
dispatcher.RegisterFunction(
    "PlayNpcFaceAnimation",
    (call) => {
        var animation = call.GetArgumentString("animation", "neutral");
        npcAnimationController.PlayFaceAnimation(animation);
        return FunctionCallResult.SuccessResult(new { success = true });
    }
);

// LLM outputs in JSON:
// {
//   "dialogueText": "I'm so happy!",
//   "functionCalls": [
//     { "functionName": "PlayNpcFaceAnimation", "arguments": { "animation": "smile" } }
//   ]
// }

// Execute function calls
var executor = new FunctionCallExecutor(dispatcher, snapshot, memorySystem);
var results = executor.ExecuteAll(parsedOutput);
// Animation executes immediately during dialogue processing
```

**Unity Integration Example**:
```csharp
using LlamaBrain.Runtime.Core.FunctionCalling;

// FunctionCallController (singleton) manages global functions
var controller = FunctionCallController.Instance 
    ?? FunctionCallController.GetOrCreate();

// Register function programmatically
controller.RegisterFunction(
    "PlayNpcFaceAnimation",
    (call) => {
        var animation = call.GetArgumentString("animation", "neutral");
        npcAnimationController.PlayFaceAnimation(animation);
        return FunctionCallResult.SuccessResult(new { success = true });
    },
    "Play a facial animation on the NPC",
    @"{""type"": ""object"", ""properties"": {""animation"": {""type"": ""string""}}}"
);

// Or use ScriptableObject configs (designer-friendly)
// 1. Create FunctionCallConfigAsset
// 2. Assign to FunctionCallController global configs
// 3. Register handler via UnityEvent or code

// LlamaBrainAgent automatically executes function calls
// Results available via:
// - agent.LastFunctionCallResults (property)
// - FunctionCallController.OnFunctionCallsExecuted (UnityEvent)
// - FunctionCallController.OnAnyFunctionCall (UnityEvent)
```

### Component 6: Stateless Inference Core

**Purpose**: Pure stateless text generation - the LLM has no memory, authority, or world access.

**Implementation**:
- `ApiClient` in `LlamaBrain.Core` (HTTP client for llama.cpp)
- `BrainServer` in `LlamaBrainRuntime` (Unity server management)
- No state stored in LLM
- No direct memory access
- No authority to modify state

**Key Properties**:
- **Pure Function**: Same input = same output (when constraints are met)
- **No Memory**: LLM receives only the bounded prompt
- **No Authority**: LLM cannot directly modify any state
- **Bounded Input**: Receives only ephemeral working memory

**Example**:
```csharp
// LLM receives only the bounded prompt
var response = await apiClient.SendPromptAsync(assembledPrompt.PromptText);

// Response is untrusted - must be validated before use
// LLM has no knowledge of what happened before or after
```

### Component 7: Output Parsing & Validation

**Purpose**: Parse LLM output and validate it against constraints and canonical facts before any state changes.

**Implementation**:
- `OutputParser` in `LlamaBrain.Core.Validation`
- `ValidationGate` in `LlamaBrain.Core.Validation`
- `ResponseValidator` in `LlamaBrain.Core.Inference`
- `ValidationRuleAsset` (Unity ScriptableObject)
- `ValidationPipeline` (Unity MonoBehaviour for global rules)
- `ValidationRuleSetAsset` (Unity ScriptableObject for rule collections)

**Validation Rule Levels**:
- **Global Rules** (World-Level): Assigned to `ValidationPipeline` component, apply to all NPCs
- **NPC-Specific Rules**: Assigned to `LlamaBrainAgent` component, apply only to that NPC
- **Trigger-Specific Rules**: Assigned to `NpcDialogueTrigger` component, apply only when trigger activates

**Rule Execution**:
- All three levels are checked in order (Global → NPC → Trigger)
- All levels must pass for validation to succeed
- Failures from all levels are combined in the result

**Validation Pipeline**:

1. **Output Parsing**
   - Extracts dialogue text
   - Parses proposed mutations (AppendEpisodic, TransformBelief, TransformRelationship, EmitWorldIntent)
   - Extracts world intents
   - Handles malformed outputs gracefully

2. **Validation Gates** (in order):
   - **Constraint Validation**: Checks against expectancy engine constraints
   - **Canonical Fact Validation**: Ensures no contradictions with immutable facts
   - **Knowledge Boundary Validation**: Prevents revealing forbidden knowledge
   - **Mutation Validation**: Validates proposed memory changes

3. **Validation Result**:
   - `GateResult.Passed` - All validations passed
   - `GateResult.Failures` - List of validation failures
   - `ApprovedMutations` - Mutations that passed validation
   - `RejectedMutations` - Mutations that failed validation
   - `ApprovedIntents` - World intents that passed validation

**Retry Logic**:
- Failed validations trigger retry with stricter constraints
- Constraint escalation modes: None, AddSpecificProhibition, HardenRequirements, Full
- Max attempts (default: 3) before fallback

**Example**:
```csharp
// Parse output
var parser = new OutputParser();
var parsedOutput = parser.Parse(llmResponse);

// Create validation context
var validationContext = new ValidationContext
{
    MemorySystem = memorySystem,
    Constraints = constraints,
    Snapshot = snapshot
};

// Validate
var validationGate = new ValidationGate();
var gateResult = validationGate.Validate(parsedOutput, validationContext);

if (gateResult.Passed)
{
    // Proceed to mutation
}
else
{
    // Retry with stricter constraints or use fallback
    foreach (var failure in gateResult.Failures)
    {
        Debug.Log($"Validation failed: {failure.Reason} - {failure.Description}");
    }
}
```

#### Native Structured Output (Feature 12)

**Purpose**: Replace regex-based output parsing with LLM-native structured output formats for improved reliability.

**Implementation**:
- `IStructuredOutputProvider` interface in `LlamaBrain.Core.StructuredOutput`
- `LlamaCppStructuredOutputProvider` for llama.cpp server integration
- `JsonSchemaBuilder` for generating JSON schemas from C# types
- Extended `ApiClient` with `SendStructuredPromptAsync` methods

**Structured Output Formats**:
- **JsonSchema** (Recommended): Native llama.cpp `json_schema` parameter enforcement
- **Grammar**: GBNF grammar constraints for non-JSON formats
- **ResponseFormat**: Simple JSON mode (`response_format: json_object`)
- **None**: Falls back to prompt injection with regex parsing

**How It Works**:
1. Define expected output structure as JSON Schema (or use pre-built `ParsedOutputSchema`)
2. Call `SendStructuredPromptAsync` with the schema
3. llama.cpp constrains token generation to match the schema
4. `OutputParser.ParseStructured` deserializes the guaranteed-valid JSON
5. No regex extraction needed - 100% reliability on valid outputs

**Pre-built Schemas**:
- `JsonSchemaBuilder.ParsedOutputSchema` - Full dialogue response with mutations and intents
- `JsonSchemaBuilder.DialogueOnlySchema` - Simple dialogue with emotion
- `JsonSchemaBuilder.AnalysisSchema` - Decision-making responses

**Example**:
```csharp
// Native structured output (100% reliable JSON)
var response = await agent.SendNativeStructuredMessageAsync(
    message: "Tell me about the tower",
    jsonSchema: JsonSchemaBuilder.ParsedOutputSchema,
    format: StructuredOutputFormat.JsonSchema,
    cancellationToken: token);

// Parse the structured response
var parser = new OutputParser(OutputParserConfig.NativeStructured);
var parsed = parser.ParseStructured(response);

// Or use the convenience method that does both:
var parsedOutput = await agent.SendNativeDialogueAsync(
    message: "Tell me about the tower",
    format: StructuredOutputFormat.JsonSchema,
    cancellationToken: token);
```

**Backward Compatibility**:
- Existing `SendStructuredMessageAsync` methods continue to use prompt injection
- `OutputParser.Parse` continues to use regex extraction
- New `ParseAuto` method automatically detects and uses appropriate parsing

#### Structured Dialogue Pipeline (Feature 13)

**Purpose**: Unified orchestration layer for processing dialogue using structured output with automatic fallback, retry logic, and comprehensive validation.

**Implementation**:
- `StructuredDialoguePipeline` in `LlamaBrain.Core.StructuredOutput`
- `StructuredSchemaValidator` for pre-execution schema validation
- `StructuredPipelineConfig` for configurable pipeline modes
- `StructuredPipelineMetrics` for performance and success tracking
- `StructuredPipelineResult` for unified result reporting

**Pipeline Flow**:
1. **LLM Request**: Send prompt with `json_schema` constraint (if structured output enabled)
2. **Parsing**: Parse response with `OutputParser.ParseStructured()` or fallback to regex
3. **Schema Validation**: Pre-validate mutation and intent schemas (optional, configurable)
4. **Validation Gate**: Run full validation through `ValidationGate` (constraints, canonical facts, etc.)
5. **Mutation Execution**: Execute approved mutations via `MemoryMutationController`
6. **Result Assembly**: Return unified `StructuredPipelineResult` with metrics

**Configuration Modes**:
- **Default**: Structured output with automatic regex fallback (recommended for production)
- **StructuredOnly**: Native structured output only, no fallback (fails explicitly on errors)
- **RegexOnly**: Legacy regex parsing only (for backward compatibility)

**Schema Validation**:
- **Pre-execution Validation**: `StructuredSchemaValidator` validates mutations and intents before they reach `ValidationGate`
- **Mutation Validation**: Checks type, content, target, and confidence requirements
- **Intent Validation**: Checks intentType, priority, and parameters dictionary
- **Filtering**: Invalid mutations/intents are filtered out with optional logging callbacks

**Retry Logic**:
- Automatic retry with constraint escalation on validation failures
- Configurable max retries (default: 3)
- Each retry uses `StateSnapshot.ForRetry()` with escalated constraints
- Metrics track retry counts and success rates

**Metrics & Monitoring**:
- `StructuredPipelineMetrics` tracks:
  - Structured output success/failure rates
  - Fallback usage rates
  - Validation failure counts
  - Mutation and intent execution counts
  - Overall pipeline success rate
- Real-time performance monitoring via `Metrics` property
- Reset capability for session-based tracking

**Example**:
```csharp
// Create pipeline with default configuration
var pipeline = new StructuredDialoguePipeline(
    agent: brainAgent,
    validationGate: validationGate,
    mutationController: mutationController,
    memorySystem: memorySystem,
    config: StructuredPipelineConfig.Default);

// Process dialogue through complete pipeline
var result = await pipeline.ProcessDialogueAsync(
    playerInput: "Tell me about the ancient tower",
    context: validationContext,
    cancellationToken: token);

// Check result
if (result.Success)
{
    Console.WriteLine($"Dialogue: {result.DialogueText}");
    Console.WriteLine($"Parse Mode: {result.ParseMode}"); // Structured, Regex, or Fallback
    Console.WriteLine($"Mutations Executed: {result.MutationsExecuted}");
    Console.WriteLine($"Intents Emitted: {result.IntentsEmitted}");
    Console.WriteLine($"Retries: {result.RetryCount}");
}
else
{
    Console.WriteLine($"Pipeline failed: {result.ErrorMessage}");
    if (result.GateResult != null)
    {
        foreach (var failure in result.GateResult.Failures)
        {
            Console.WriteLine($"  - {failure.Reason}: {failure.Description}");
        }
    }
}

// Monitor metrics
var metrics = pipeline.Metrics;
Console.WriteLine($"Structured Success Rate: {metrics.StructuredSuccessRate:F1}%");
Console.WriteLine($"Fallback Rate: {metrics.FallbackRate:F1}%");
Console.WriteLine($"Overall Success Rate: {metrics.OverallSuccessRate:F1}%");
```

**Integration with Component 7**:
- `StructuredDialoguePipeline` orchestrates the complete validation flow
- Uses `ValidationGate` for constraint and canonical fact validation
- Integrates with `MemoryMutationController` for mutation execution
- Handles retry logic with constraint escalation
- Provides unified error handling and fallback mechanisms

**Performance**:
- Parsing performance: ~0.01ms for structured, ~0.00ms for regex (simple responses)
- Sub-millisecond parsing for all paths
- Metrics tracking has negligible overhead
- Automatic fallback ensures 100% reliability even when structured output fails

### Component 8: Memory Mutation + World Effects

**Purpose**: Execute validated mutations and dispatch world intents, with strict authority enforcement.

**Implementation**:
- `MemoryMutationController` in `LlamaBrain.Persona`
- `WorldIntentDispatcher` in `LlamaBrainRuntime` (Unity component)
- Authority enforcement prevents canonical fact overrides

**Mutation Types**:
- **AppendEpisodic**: Add conversation/event to episodic memory
- **TransformBelief**: Update or create NPC belief/opinion
- **TransformRelationship**: Update relationship with another entity
- **EmitWorldIntent**: Dispatch world-affecting intent to game systems

**Authority Enforcement**:
- Only validated outputs can trigger mutations
- Canonical facts cannot be overridden (blocked with statistics)
- World state requires `GameSystem` authority
- All mutation attempts are logged

**World Intents**:
- NPC desires that affect the game world
- Dispatched via `WorldIntentDispatcher` Unity component
- Can trigger quest events, spawn items, change world state, etc.

**Example**:
```csharp
// Execute mutations
var mutationController = new MemoryMutationController();
var mutationResult = mutationController.ExecuteMutations(gateResult, memorySystem);

// Check results
if (mutationResult.AllSucceeded)
{
    Debug.Log($"All {mutationResult.TotalAttempted} mutations succeeded");
}
else
{
    Debug.Log($"{mutationResult.FailureCount} mutations failed");
}

// Handle world intents
foreach (var intent in gateResult.ApprovedIntents)
{
    // Dispatch to game systems
    WorldIntentDispatcher.Instance.DispatchIntent(intent);
    Debug.Log($"World Intent: {intent.IntentType} targeting {intent.Target}");
}
```

### Component 9: Author-Controlled Fallback

**Purpose**: Provide context-aware fallback responses when all retry attempts fail.

**Implementation**:
- `FallbackSystem` in `LlamaBrain.Core` (engine-agnostic)
- `AuthorControlledFallback` in `LlamaBrainRuntime` (Unity component)
- Context-aware selection based on trigger reason

**Fallback Hierarchy**:
1. **Context-Aware Fallbacks**: Selected based on `TriggerReason`
   - PlayerUtterance → Generic conversation fallbacks
   - ZoneTrigger → Location-specific fallbacks
   - QuestTrigger → Quest-related fallbacks
   - etc.

2. **Generic Fallbacks**: Used when no context-specific fallback matches

3. **Emergency Fallbacks**: Always available as last resort

**Integration**:
- Automatically used after max retry attempts
- Never corrupts state (no mutations executed)
- Statistics tracked for debugging

**Example**:
```csharp
// Fallback system is integrated in LlamaBrainAgent
// Automatically used when validation fails after retries

// Configure fallbacks
var fallbackConfig = new FallbackSystem.FallbackConfig
{
    GenericFallbacks = new List<string> { "I'm not sure what to say.", "Let me think about that." },
    ContextFallbacks = new Dictionary<TriggerReason, List<string>>
    {
        { TriggerReason.PlayerUtterance, new List<string> { "I hear you.", "That's interesting." } },
        { TriggerReason.ZoneTrigger, new List<string> { "Welcome to this area.", "This place is special." } }
    },
    EmergencyFallbacks = new List<string> { "I'm having trouble responding right now." }
};

var fallbackSystem = new FallbackSystem(fallbackConfig);
// Integrated automatically in LlamaBrainAgent
```

## Complete Flow Example

Here's how all components work together in a complete interaction:

```csharp
// 1. Untrusted Observation → Interaction Context
var context = new InteractionContext
{
    TriggerReason = TriggerReason.PlayerUtterance,
    NpcId = "wizard_001",
    PlayerInput = "Tell me about the ancient tower",
    TriggerPrompt = @"Example 1:
Player: What is that building?
Wizard: That is the ancient tower, built centuries ago by the first mages.

Example 2:
Player: Can I go inside?
Wizard: The tower is sealed, but legends say it holds great power." // Few-shot examples
};

// 2. Determinism Layer - Evaluate rules
var constraints = expectancyEvaluator.EvaluateRules(context, rules);

// 3. External Authoritative Memory System - Retrieve context
var retrievalLayer = new ContextRetrievalLayer(memorySystem);
var retrievedContext = retrievalLayer.RetrieveContext(context.PlayerInput);

// 4. Authoritative State Snapshot
var snapshot = new StateSnapshotBuilder()
    .WithContext(context)
    .WithConstraints(constraints)
    .Apply(retrievedContext)
    .Build();

// 5. Ephemeral Working Memory - Assemble bounded prompt
var assembler = new PromptAssembler();
var assembledPrompt = assembler.AssembleFromSnapshot(snapshot);

// 6. Stateless Inference Core - Generate response
var response = await apiClient.SendPromptAsync(assembledPrompt.PromptText);

// 7. Output Parsing & Validation
// ValidationPipeline (if present) handles global rules automatically
// NPC-specific and trigger-specific rules are checked via agent's ValidationGate
var parsedOutput = parser.Parse(response);
var gateResult = validationGate.Validate(parsedOutput, new ValidationContext
{
    MemorySystem = memorySystem,
    Constraints = constraints,
    Snapshot = snapshot
});
// Note: In Unity, ValidationPipeline.Instance.ProcessWithSnapshot() handles global rules,
// and LlamaBrainAgent's ValidationGate handles NPC/trigger-specific rules

// If validation fails, retry with stricter constraints (back to step 4/5)
if (!gateResult.Passed && snapshot.CanRetry)
{
    var retrySnapshot = snapshot.ForRetry(escalatedConstraints, snapshot.AttemptNumber + 1);
    // Retry loop...
}

// 8. Memory Mutation + World Effects
if (gateResult.Passed)
{
    var mutationResult = mutationController.ExecuteMutations(gateResult, memorySystem);
    foreach (var intent in gateResult.ApprovedIntents)
    {
        WorldIntentDispatcher.Instance.DispatchIntent(intent);
    }
}

// 9. Result (or Fallback if all retries failed)
string finalResponse = gateResult.Passed 
    ? parsedOutput.DialogueText 
    : fallbackSystem.GetFallback(context.TriggerReason, failureReason);
```

### Alternative: Using StructuredDialoguePipeline (Recommended)

For production use, the `StructuredDialoguePipeline` orchestrates steps 6-9 automatically with structured output, retry logic, and comprehensive validation:

```csharp
// Steps 1-5 remain the same (context, constraints, snapshot, prompt assembly)
// ... (same as above) ...

// 6-9. Unified Pipeline (handles structured output, parsing, validation, mutations, retries)
var pipeline = new StructuredDialoguePipeline(
    agent: brainAgent,
    validationGate: validationGate,
    mutationController: mutationController,
    memorySystem: memorySystem,
    config: StructuredPipelineConfig.Default);

var result = await pipeline.ProcessDialogueAsync(
    playerInput: context.PlayerInput,
    context: new ValidationContext
    {
        MemorySystem = memorySystem,
        Constraints = constraints,
        Snapshot = snapshot
    },
    cancellationToken: token);

// Result includes dialogue, mutations, intents, and metrics
string finalResponse = result.Success 
    ? result.DialogueText 
    : fallbackSystem.GetFallback(context.TriggerReason, result.ErrorMessage);

// Monitor pipeline performance
var metrics = pipeline.Metrics;
Debug.Log($"Structured Success Rate: {metrics.StructuredSuccessRate:F1}%");
```

The pipeline automatically:
- Uses structured output with JSON schema constraints
- Falls back to regex parsing if structured output fails
- Retries with escalated constraints on validation failures
- Pre-validates mutation and intent schemas
- Executes approved mutations and dispatches intents
- Tracks comprehensive metrics for monitoring

## Few-Shot Prompting System

LlamaBrain includes a comprehensive few-shot prompting system that provides in-context learning examples to guide LLM behavior, format, and tone.

### Overview

Few-shot prompting (also called in-context learning) provides example input-output pairs that demonstrate desired behavior. The LLM uses these examples to understand the expected response format, tone, and content without explicit instructions.

**Key Benefits**:
- **Format Consistency**: Examples teach the expected response structure
- **Tone Control**: Demonstrate the character's voice and personality
- **Behavior Demonstration**: Show how to handle specific interaction types
- **Error Reduction**: Examples reduce hallucinations by providing concrete patterns

### Implementation Components

**Core Classes**:
- `FewShotExample` - Represents a single input-output example pair
- `EphemeralWorkingMemory` - Stores and manages few-shot examples for prompt assembly
- `WorkingMemoryConfig` - Configures few-shot limits and behavior
- `PromptAssembler` - Injects few-shot examples into the prompt
- `FallbackToFewShotConverter` - Converts fallback responses to few-shot examples

### FewShotExample Structure

```csharp
public class FewShotExample
{
    /// <summary>Example player/user input</summary>
    public string Input { get; set; }

    /// <summary>Example NPC/assistant response</summary>
    public string Output { get; set; }

    /// <summary>Optional context or scenario description</summary>
    public string? Context { get; set; }

    /// <summary>Optional trigger reason this example is relevant for</summary>
    public TriggerReason? TriggerReason { get; set; }

    /// <summary>Optional tags for filtering examples</summary>
    public List<string> Tags { get; set; }
}
```

### Configuration Options

**WorkingMemoryConfig Few-Shot Settings**:
```csharp
var config = new WorkingMemoryConfig
{
    // Maximum number of few-shot examples to include
    MaxFewShotExamples = 3,

    // List of few-shot examples
    FewShotExamples = new List<FewShotExample>
    {
        new FewShotExample
        {
            Input = "Hello there!",
            Output = "Greetings, traveler! What brings you to my shop today?",
            TriggerReason = TriggerReason.PlayerUtterance
        },
        new FewShotExample
        {
            Input = "What do you sell?",
            Output = "I deal in rare artifacts and enchanted trinkets. Browse freely, but touch nothing without asking first.",
            TriggerReason = TriggerReason.PlayerUtterance
        }
    },

    // Always include few-shot examples even if context is limited
    AlwaysIncludeFewShot = true
};
```

**PromptAssemblerConfig Few-Shot Settings**:
```csharp
var assemblerConfig = new PromptAssemblerConfig
{
    // Whether to include few-shot examples section
    IncludeFewShotExamples = true,

    // Header text before few-shot examples
    FewShotHeader = "Here are examples of how you should respond:",

    // Format string for each example (supports {input}, {output}, {context})
    FewShotExampleFormat = "Player: {input}\nYou: {output}",

    // Separator between examples
    FewShotSeparator = "\n\n"
};
```

### Using FallbackToFewShotConverter

Convert author-controlled fallback responses into few-shot examples:

```csharp
// Create converter
var converter = new FallbackToFewShotConverter();

// Convert fallback config to few-shot examples
var fallbackConfig = new FallbackSystem.FallbackConfig
{
    GenericFallbacks = new List<string>
    {
        "I'm not sure what to say.",
        "Let me think about that."
    },
    ContextFallbacks = new Dictionary<TriggerReason, List<string>>
    {
        {
            TriggerReason.PlayerUtterance,
            new List<string>
            {
                "I hear you, traveler.",
                "That's an interesting thought."
            }
        }
    }
};

// Convert to few-shot examples with generated inputs
var examples = converter.ConvertToFewShotExamples(
    fallbackConfig,
    generateInputs: true,  // Auto-generate matching inputs
    maxExamplesPerReason: 2
);

// Use in working memory
workingMemoryConfig.FewShotExamples = examples;
```

### Prompt Assembly Flow

1. **Example Collection**: `EphemeralWorkingMemory` collects examples from config
2. **Filtering**: Examples filtered by `TriggerReason` and tags if specified
3. **Ordering**: Examples ordered deterministically (by sequence, ensuring byte-stable output)
4. **Formatting**: `PromptAssembler` formats examples using configured template
5. **Injection**: Examples injected into prompt at configured location

**Prompt Structure**:
```
[System Prompt]
[Persona Description]

[Few-Shot Examples Header]
Player: What is magic?
You: Magic is the art of channeling the world's energy through focused will.

Player: How do I learn magic?
You: Magic requires years of study and a deep understanding of the arcane principles.

[Memory Context]
[Constraints]
[Dialogue History]
[Current Player Input]
```

### Few-Shot Best Practices

#### 1. Match Trigger Reason
Tag examples with relevant `TriggerReason` to show context-appropriate responses:

```csharp
// Examples for zone triggers (NPC reacts to player entering area)
new FewShotExample
{
    Input = "[Player enters the shop]",
    Output = "Ah, a customer! Welcome, welcome. Feel free to browse.",
    TriggerReason = TriggerReason.ZoneTrigger
}

// Examples for player utterances (direct conversation)
new FewShotExample
{
    Input = "Tell me about yourself.",
    Output = "I've been running this shop for thirty years. Every item has a story.",
    TriggerReason = TriggerReason.PlayerUtterance
}
```

#### 2. Demonstrate Format Consistently
Use examples to teach response length, structure, and style:

```csharp
// Short, punchy responses for a gruff character
new FewShotExample { Input = "How are you?", Output = "Fine." }
new FewShotExample { Input = "Nice weather.", Output = "Is it?" }

// Verbose, detailed responses for a scholarly character
new FewShotExample
{
    Input = "What is that book?",
    Output = "Ah, you've noticed my most prized possession! This tome, penned by the great archmage Valdris, contains the fundamental theorems of elemental manipulation. It took me decades to acquire."
}
```

#### 3. Show Constraint Compliance
Include examples that demonstrate respecting constraints:

```csharp
// Example showing NPC refusing to reveal secrets
new FewShotExample
{
    Input = "What's the king's secret?",
    Output = "I cannot speak of such matters. The crown's affairs are not mine to share.",
    Tags = new List<string> { "secret_protection" }
}

// Example showing NPC staying in character
new FewShotExample
{
    Input = "What's your favorite video game?",
    Output = "Video... game? I know not what strange magic you speak of, traveler.",
    Tags = new List<string> { "anachronism_handling" }
}
```

#### 4. Limit Example Count
Too many examples waste tokens; too few miss patterns:

```csharp
// Recommended: 2-5 examples for most use cases
config.MaxFewShotExamples = 3;

// For complex behaviors, consider more examples
// but watch token budget
config.MaxFewShotExamples = 5;
```

#### 5. Order Examples Strategically
Place most relevant/important examples last (recency bias in LLMs):

```csharp
var examples = new List<FewShotExample>
{
    // General example first
    new FewShotExample { Input = "Hello", Output = "Greetings." },

    // More specific examples later
    new FewShotExample { Input = "I need help", Output = "What troubles you?" },

    // Most important pattern last
    new FewShotExample
    {
        Input = "Tell me a secret",
        Output = "Some knowledge is too dangerous to share freely."
    }
};
```

#### 6. Use Context Field for Complex Scenarios
Provide scenario context when needed:

```csharp
new FewShotExample
{
    Context = "The player has just defeated the dragon",
    Input = "I did it!",
    Output = "Incredible! The beast that terrorized our village for centuries, slain by your hand. You have earned our eternal gratitude.",
    Tags = new List<string> { "post_victory" }
}
```

### Deterministic Ordering

Few-shot examples are always ordered deterministically to ensure byte-stable prompt assembly:

- Examples maintain list order (first-in, first-out)
- Filtered examples preserve relative order
- No randomization unless explicitly configured
- Same input state = same prompt output

This ensures retries produce identical prompts (critical for the determinism architecture).

### Integration with Fallback System

Few-shot examples can be derived from fallback responses:

```csharp
// Fallback responses serve dual purpose:
// 1. Emergency responses when LLM fails
// 2. Source material for few-shot examples

var fallbackConfig = fallbackSystem.GetConfig();
var converter = new FallbackToFewShotConverter();

// Convert fallbacks to examples
var examples = converter.ConvertByTriggerReason(
    fallbackConfig,
    TriggerReason.PlayerUtterance,
    maxExamples: 3
);

// Add to working memory config
config.FewShotExamples.AddRange(examples);
```

This creates a virtuous cycle: well-crafted fallback responses improve both failure handling AND normal generation quality.

### Performance Considerations

- **Token Budget**: Each example consumes tokens; budget accordingly
- **Example Selection**: Filter by trigger reason to minimize irrelevant examples
- **Caching**: Examples are cached in `EphemeralWorkingMemory` per inference
- **Statistics**: Track few-shot counts in `WorkingMemoryStats.FewShotCount`

---

## Key Architectural Principles

### 1. Stateless LLM
The LLM is a stateless stochastic generator with no memory. It receives bounded prompts and generates text. It has no knowledge of previous interactions, no authority to modify state, and no direct access to the memory system. Note: While seeded sampling provides best-effort reproducibility, LLM output is NOT mathematically deterministic due to hardware/runtime variations (GPU kernels, thread scheduling, floating-point ordering, etc.).

### 2. Deterministic State
All context is captured in immutable `StateSnapshot` objects. The same snapshot can be replayed exactly, enabling deterministic retries. State is never modified during inference - only after validation.

### 3. Bounded Context
`EphemeralWorkingMemory` ensures token-efficient prompts by explicitly bounding context size. This prevents token waste while maintaining relevant context through intelligent retrieval.

### 4. Validation Gate
The validation gate (Component 7) is the critical barrier between the stochastic LLM and authoritative game state. **Only validated outputs can mutate memory**, ensuring hallucinations cannot corrupt state.

### 5. Authority Enforcement
Memory types have a strict authority hierarchy. Canonical facts (highest authority) cannot be overridden by any source. This ensures world consistency and prevents AI from contradicting immutable truths.

### 6. Retry & Fallback
Automatic retry with constraint escalation provides self-correction. When all retries fail, context-aware fallback responses ensure the system always responds without corrupting state.

## Benefits of This Architecture

1. **Determinism**: Same input + context = same output (when constraints are met)
2. **Authority**: Canonical facts cannot be overridden, ensuring world consistency
3. **Safety**: Multi-layer validation prevents invalid outputs from affecting game state
4. **Efficiency**: Bounded prompts prevent token waste while maintaining relevant context
5. **Flexibility**: LLM creativity is preserved while maintaining control
6. **Debuggability**: Immutable snapshots enable exact replay and debugging
7. **Scalability**: Authority boundaries prevent memory corruption at scale

## Component Interaction Diagram

The architectural diagram shows the complete flow:

1. **Untrusted Observation** triggers the system
2. **Determinism Layer** generates constraints
3. **Authoritative Memory System** provides context
4. **State Snapshot** captures immutable context
5. **Ephemeral Working Memory** creates bounded prompt
6. **Stateless Inference Core** generates untrusted output
7. **Output Validation** checks against constraints and facts
8. **Memory Mutation** executes only validated changes
9. **Fallback System** provides safe responses when needed

**Feedback Loops**:
- Validation failures → Retry with stricter constraints (back to step 4/5)
- Max attempts exceeded → Fallback (step 9)
- Successful validation → Memory mutation (step 8)

## Implementation Status

All nine components are fully implemented and tested:

- ✅ Component 1: Interaction Context - Complete
- ✅ Component 2: Determinism Layer - Complete (Feature 1)
- ✅ Component 3: Authoritative Memory System - Complete (Feature 2)
- ✅ Component 4: State Snapshot & Context Retrieval - Complete (Feature 3)
- ✅ Component 5: Ephemeral Working Memory - Complete (Feature 4)
- ✅ Component 6: Stateless Inference Core - Complete (Foundation)
- ✅ Component 7: Output Validation - Complete (Feature 5, Feature 12: Structured Output, Feature 13: Structured Pipeline Integration)
- ✅ Component 8: Memory Mutation - Complete (Feature 6)
- ✅ Component 9: Fallback System - Complete (Feature 7)

**Test Coverage**: ~90%+ line coverage, 2,068 tests passing, 61 of 65 files at 80%+ coverage
**Coverage Note**: See COVERAGE_REPORT.md for detailed metrics.

## Claims to Tests Mapping

This section maps architectural claims to the tests that prove them.

### Claim 1: Pipeline Determinism - Same Input = Same Output

> "Deterministic: prompt assembly, parsing, validation, gating decision, memory mutation, metrics emission."

**What IS proven (the governance plane around the LLM):**

| Test File | Test Name | What It Proves |
|-----------|-----------|----------------|
| `GovernancePlaneDeterminismTests.cs` | `PromptAssembly_SameInputs_IdenticalBytes_*` | **PROOF**: Prompt assembly is byte-stable |
| `GovernancePlaneDeterminismTests.cs` | `Validation_SameParsedOutput_IdenticalGateResult_*` | **PROOF**: Validation/gating is deterministic |
| `GovernancePlaneDeterminismTests.cs` | `Mutation_SameGateResult_IdenticalDiff` | **PROOF**: Memory mutation is deterministic |
| `GovernancePlaneDeterminismTests.cs` | `DictionaryEnumeration_*` | **PROOF**: Dictionary ordering doesn't leak into output |
| `DeterministicPipelineTests.cs` | `FullPipeline_IdenticalInputs_ProduceIdenticalOutputs` | Complete pipeline (with mocked LLM) produces identical outputs |
| `DeterministicPipelineTests.cs` | `MemoryMutation_DeterministicOrder_ProducesIdenticalState` | Memory mutations applied in deterministic order |
| `IdGeneratorTests.cs` | `SequentialIdGenerator_IsDeterministic` | Sequential IDs are deterministic across instances |
| `ValidationGateTests.cs` | `Determinism_*` | Validation produces same result for same input |
| `ContextSerializerTests.cs` | `*_Determinism_*` | JSON serialization is deterministic |

**What is NOT proven (LLM generation is stochastic):**

| Test File | Test Name | What It Actually Tests |
|-----------|-----------|----------------|
| `CrossSessionDeterminismTests.cs` | `SameSeedSamePrompt_*` | Reproducibility smoke test (best-effort, not guaranteed) |
| `ExternalIntegrationPlayModeTests.cs` | `PlayMode_*` | Reproducibility smoke test with real llama.cpp server |

> **Important**: LLM output is NOT mathematically deterministic. Seeded sampling provides best-effort reproducibility under controlled conditions, but results may vary due to: GPU vs CPU execution, thread scheduling, SIMD reductions, llama.cpp version, CUDA version, driver version, etc. The architectural guarantee is that the **pipeline around the LLM** (prompt assembly, validation, mutation) is deterministic.
>
> **Note**: External integration tests require a llama.cpp server:
> - **Standalone .NET**: `dotnet test --filter "Category=RequiresLlamaServer"` (manual server start)
> - **Unity PlayMode**: Run `ExternalIntegrationPlayModeTests.cs` via Unity Test Runner (auto-starts server)

### Claim 2: Canonical Facts Cannot Be Overridden

> "Canonical facts cannot be modified by any source" / "Immutable world truths"

| Test File | Test Name | What It Proves |
|-----------|-----------|----------------|
| `AuthoritativeMemorySystemTests.cs` | `AddCanonicalFact_DuplicateId_Fails` | Cannot add duplicate canonical fact |
| `AuthoritativeMemorySystemTests.cs` | `ValidateMutation_GameSystemCannotModifyCanonical` | GameSystem authority cannot modify canonical facts |
| `ValidationGateTests.cs` | `Validate_MutationTargetsCanonicalFact_RejectsMutation` | Mutations targeting canonical facts are rejected |
| `ValidationGateTests.cs` | `Validate_ContradictCanonicalFact_Fails` | Outputs contradicting canonical facts fail validation |
| `ValidationGateTests.cs` | `Gate2_CanonicalContradiction_HasCriticalSeverity` | Canonical contradictions are critical failures |

### Claim 3: Memory Authority Hierarchy

> "Memory types have a strict authority hierarchy"

| Test File | Test Name | What It Proves |
|-----------|-----------|----------------|
| `AuthoritativeMemorySystemTests.cs` | `ValidateMutation_DesignerCanModifyAll` | Designer authority can modify all memory types |
| `AuthoritativeMemorySystemTests.cs` | `ValidateMutation_GameSystemCannotModifyCanonical` | GameSystem cannot modify canonical facts |
| `AuthoritativeMemorySystemTests.cs` | `ValidateMutation_LlmSuggestionHasLimitedAccess` | LLM suggestions have lowest authority |
| `AuthoritativeMemorySystemTests.cs` | `SetWorldState_InsufficientAuthority_Fails` | Insufficient authority blocks world state changes |
| `AuthoritativeMemorySystemTests.cs` | `AddEpisodicMemory_WithInsufficientAuthority_Fails` | Insufficient authority blocks episodic memory |

### Claim 4: Validation Gate Blocks Invalid Outputs

> "Only validated outputs can mutate memory"

| Test File | Test Name | What It Proves |
|-----------|-----------|----------------|
| `ValidationGateTests.cs` | `Validate_ProhibitionViolated_Fails` | Constraint violations fail validation |
| `ValidationGateTests.cs` | `Validate_ForbiddenKnowledge_Fails` | Forbidden knowledge fails validation |
| `ValidationGateTests.cs` | `Validate_MutationTargetsCanonicalFact_RejectsMutation` | Invalid mutations are rejected |
| `ValidationGateTests.cs` | `GateExecution_AllGatesExecuteInOrder_FailuresAccumulate` | All validation gates execute |
| `ValidationGateTests.cs` | `GateResult_PassedFalse_WhenAnyGateFails` | Any gate failure blocks output |
| `MemoryMutationControllerTests.cs` | `*_ValidatedOutput_*` | Only validated outputs trigger mutations |

### Claim 5: Retry with Constraint Escalation

> "Automatic retry with constraint escalation provides self-correction"

| Test File | Test Name | What It Proves |
|-----------|-----------|----------------|
| `StateSnapshotTests.cs` | `Snapshot_CanRetry_WhenAttemptLessThanMax` | Retry allowed when under max attempts |
| `StateSnapshotTests.cs` | `Snapshot_CanRetry_FalseWhenAtMax` | Retry blocked at max attempts |
| `StateSnapshotTests.cs` | `ForRetry_CreatesNewSnapshotWithIncrementedAttempt` | Retry increments attempt counter |
| `StateSnapshotTests.cs` | `ForRetry_MergesConstraints` | Retry merges escalated constraints |
| `RetryPolicyTests.cs` | `*` | Retry policy enforces limits and escalation |
| `ValidationGateTests.cs` | `GateResult_ShouldRetry_TrueWhenFailedButNotCritical` | Non-critical failures allow retry |
| `ValidationGateTests.cs` | `GateResult_ShouldRetry_FalseWhenCriticalFailure` | Critical failures block retry |

### Claim 6: Fallback System Provides Safe Responses

> "Context-aware fallback responses ensure the system always responds without corrupting state"

| Test File | Test Name | What It Proves |
|-----------|-----------|----------------|
| `FallbackSystemTests.cs` | `GetFallbackResponse_WithValidContext_ReturnsFallback` | Fallback always returns response |
| `FallbackSystemTests.cs` | `GetFallbackResponse_WithPlayerUtterance_ReturnsPlayerUtteranceFallback` | Context-aware fallback selection |
| `FallbackSystemTests.cs` | `GetFallbackResponse_WithNullContext_CreatesDefaultContext` | Handles edge cases gracefully |
| `FallbackSystemTests.cs` | `FallbackSystem_Stats_TracksUsage` | Fallback usage is tracked |

### Claim 7: Bounded Context / Token Efficiency

> "EphemeralWorkingMemory ensures token-efficient prompts by explicitly bounding context size"

| Test File | Test Name | What It Proves |
|-----------|-----------|----------------|
| `EphemeralWorkingMemoryTests.cs` | `*_BoundedBy_*` | Memory is bounded by configured limits |
| `EphemeralWorkingMemoryTests.cs` | `*_MaxExchanges_*` | Exchange count is limited |
| `EphemeralWorkingMemoryTests.cs` | `*_Truncat*` | Content is truncated to fit bounds |
| `ContextRetrievalLayerTests.cs` | `*_Limit_*` | Retrieval respects configured limits |
| `PromptAssemblerTests.cs` | `*_WorkingMemory_*` | Prompt assembly uses bounded memory |

### Claim 8: Structured Output Reliability

> "100% reliability on valid structured outputs"

| Test File | Test Name | What It Proves |
|-----------|-----------|----------------|
| `OutputParserStructuredTests.cs` | `ParseStructured_ValidJson_*` | Valid JSON is parsed correctly |
| `StructuredSchemaValidatorTests.cs` | `*_ValidMutation_*` | Valid mutations pass schema validation |
| `StructuredPipelineTests.cs` | `*_Fallback_*` | Automatic fallback to regex on failure |
| `JsonSchemaBuilderTests.cs` | `*_GeneratesValidSchema_*` | Schemas are valid JSON Schema |

### Claim 9: Sub-Millisecond Parsing Performance

> "Parsing performance: ~0.01ms for structured, ~0.00ms for regex" / "Sub-millisecond parsing for all paths"

| Test File | Test Name | What It Proves |
|-----------|-----------|----------------|
| `ParsingPerformanceTests.cs` | `Parse_SimpleDialogue_SubMillisecond` | Regex parsing < 1ms |
| `ParsingPerformanceTests.cs` | `Parse_LongDialogue_SubMillisecond` | Long content (1800+ chars) < 1ms |
| `ParsingPerformanceTests.cs` | `ParseStructured_ValidJson_SubMillisecond` | JSON parsing < 1ms |
| `ParsingPerformanceTests.cs` | `ParseStructured_ComplexJson_SubMillisecond` | Complex JSON (10 mutations) < 1ms |
| `ParsingPerformanceTests.cs` | `ContextSerializer_Serialize_SubMillisecond` | Context serialization < 1ms |
| `ParsingPerformanceTests.cs` | `ContextSerializer_Deserialize_SubMillisecond` | Context deserialization < 1ms |
| `ParsingPerformanceTests.cs` | `PerformanceSummary_AllPathsSubMillisecond` | **PROOF**: All paths verified < 1ms |

Run with: `dotnet test --filter "Category=Performance"`

### Known Gaps

| Claim | Issue | Planned Fix |
|-------|-------|-------------|
| **Belief contradiction detection** | Current implementation uses simple string matching which doesn't catch semantic contradictions. Test `GetActiveBeliefs_ExcludesContradicted` has comment: *"Mark b1 as contradicted manually (since our simple check won't catch it)"* | Feature 25: NLP-based semantic contradiction detection |

### Running Claim Verification Tests

To verify all architectural claims:

```powershell
# Run all determinism tests
dotnet test --filter "FullyQualifiedName~Deterministic"

# Run all authority/canonical tests
dotnet test --filter "FullyQualifiedName~Canonical|FullyQualifiedName~Authority"

# Run all validation gate tests
dotnet test --filter "FullyQualifiedName~ValidationGate"

# Run all fallback tests
dotnet test --filter "FullyQualifiedName~Fallback"

# Run all performance tests (proves sub-millisecond parsing)
dotnet test --filter "Category=Performance"

# Run cross-session determinism tests (requires llama.cpp server)
# Option 1: Standalone .NET (manual server start required)
dotnet test --filter "Category=RequiresLlamaServer"
# Option 2: Unity PlayMode (auto-starts server) - run via Unity Test Runner
```

**Feature 10: Deterministic Proof Gap Testing**: ✅ Complete
- Critical requirements 1-4 implemented (strict total order sorting, SequenceNumber field, tie-breaker logic, OutputParser normalization)
- 7 high-leverage determinism tests added
- See `PHASE10_PROOF_GAPS.md` for detailed test backlog
- Required for v1.0 release to claim "deterministically proven" architecture

## Planned Features

The following features are planned to enhance the architecture's capabilities and complete the deterministic state reconstruction pattern:

### Feature 11: RAG-Based Memory Retrieval & Memory Proving

**Status**: 📋 Planned  
**Priority**: MEDIUM  
**Dependencies**: Feature 3 (Context Retrieval Layer), Feature 10 (Deterministic Proof Gap Testing)

**Overview**: Enhance the `ContextRetrievalLayer` to use a **hybrid approach** combining Retrieval-Augmented Generation (RAG) techniques with existing keyword matching. This hybrid system will use both noun-based keyword matching (for safe, deterministic checks) and semantic inference via embeddings and vector similarity search (for improved relevance).

**Key Components**:
- **Embedding Generation System**: Interface for generating embeddings from local models (llama.cpp) or external APIs (OpenAI, HuggingFace)
- **Vector Storage & Indexing**: In-memory and persistent vector stores for episodic memories, beliefs, and canonical facts
- **Hybrid Retrieval System**: Combine noun-based keyword matching (deterministic) with semantic vector similarity search (inference-based)
- **Memory Proving**: Implement deterministic repetition recognition system to prove retrieval influences generation
  - Location repetition recognition (NPC gets tired of same tunnel)
  - Topic/conversation repetition recognition (NPC gets tired of player obsessively talking about same topic)

**Architectural Impact**: Improves memory retrieval quality through hybrid approach (noun-based + semantic) while maintaining determinism via noun-based checks. The repetition recognition system provides concrete proof that retrieval influences generation.

**See**: [ROADMAP.md](ROADMAP.md#feature-11-rag-based-memory-retrieval--memory-proving) for detailed implementation plan.

### Feature 12: Dedicated Structured Output

**Status**: ✅ **Complete**  
**Priority**: HIGH  
**Dependencies**: Feature 5 (Output Validation System), Feature 10 (Deterministic Proof Gap Testing)

**Overview**: Replace regex-based text parsing with LLM-native structured output formats (JSON mode, function calling, schema-based outputs). This eliminates parsing errors and improves determinism.

**Key Components**:
- ✅ **Structured Output Provider Interface**: `IStructuredOutputProvider` with `LlamaCppStructuredOutputProvider` implementation supporting native llama.cpp JSON schema
- ✅ **JSON Schema Definition**: Pre-built schemas (`ParsedOutputSchema`, `DialogueOnlySchema`, `AnalysisSchema`) and dynamic generation via `JsonSchemaBuilder.BuildFromType<T>()`
- ✅ **LLM Integration**: Extended `ApiClient` with `SendStructuredPromptAsync()` methods supporting native `json_schema` parameter
- ✅ **Output Parser Refactoring**: `ParseStructured()` and `ParseAuto()` methods with automatic regex fallback for backward compatibility
- ✅ **BrainAgent Integration**: `SendNativeStructuredMessageAsync()`, `SendNativeDialogueAsync()`, `SendNativeStructuredInstructionAsync()` with generic type support

**Architectural Impact**: Eliminates parsing errors, improves reliability from ~95% to 100% success rate on valid structured outputs, and enhances determinism by removing regex ambiguity. Native JSON parsing provides deterministic extraction of dialogue, mutations, and world intents.

**Test Coverage**: 56 comprehensive tests across `JsonSchemaBuilderTests`, `StructuredOutputProviderTests`, and `OutputParserStructuredTests`.

**See**: [ROADMAP.md](ROADMAP.md#feature-12-dedicated-structured-output) for detailed implementation checklist and [CHANGELOG.md](CHANGELOG.md) for complete feature list.

### Feature 13: Structured Output Integration

**Status**: ✅ **Complete**  
**Priority**: HIGH  
**Dependencies**: Feature 12 (Dedicated Structured Output)

**Overview**: Complete integration of structured output throughout the validation pipeline, mutation extraction, and ensure full compatibility with existing systems.

**Key Components**:
- ✅ **StructuredDialoguePipeline**: Complete orchestration layer for structured output processing
  - Retry logic with escalating constraints via `StateSnapshot.ForRetry()`
  - Automatic fallback to regex parsing on structured output failure
  - Metrics tracking for success rates and performance
  - Full integration with `ValidationGate` and `MemoryMutationController`
  - Unified error handling and result reporting
- ✅ **StructuredSchemaValidator**: Pre-execution schema validation
  - Validates mutation schemas (type, target, content, confidence requirements)
  - Validates intent schemas (intentType, priority, parameters)
  - Filters invalid mutations/intents before execution
  - Supports both `StructuredMutation` and `ProposedMutation` types
  - Supports both `StructuredIntent` and `WorldIntent` types
- ✅ **StructuredPipelineConfig**: Configurable pipeline modes
  - `Default`: Structured output with regex fallback (recommended)
  - `StructuredOnly`: Native structured output, no fallback
  - `RegexOnly`: Legacy regex parsing (for backward compatibility)
  - Configurable schema validation flags
  - Configurable retry limits
- ✅ **StructuredPipelineMetrics**: Performance and success tracking
  - Structured success/failure counts
  - Fallback usage rates
  - Validation failure counts
  - Mutation and intent execution counts
  - Retry attempt tracking
  - Calculated success rates (structured, overall, fallback)
- ✅ **StructuredPipelineResult**: Unified result reporting
  - Success/failure status
  - Parse mode tracking (Structured, Regex, Fallback)
  - Complete validation and mutation results
  - Error messages and retry counts
  - Convenience properties for common queries

**Parsing Performance** (sub-millisecond for all paths):

| Parse Type | Simple Response | Complex Response |
|------------|-----------------|------------------|
| Structured | ~0.01ms | ~0.07ms |
| Regex | ~0.00ms | ~0.01ms |

**Architectural Impact**: Provides a unified, production-ready pipeline for structured output processing with automatic fallback, comprehensive validation, and detailed metrics. Eliminates the need for manual orchestration of parsing, validation, and mutation execution. Ensures 100% reliability through automatic fallback to regex parsing when structured output fails.

**Test Coverage**: Comprehensive integration tests covering all pipeline modes, fallback scenarios, retry logic, and schema validation.

**Usage**:
```csharp
// Create pipeline with default configuration
var pipeline = new StructuredDialoguePipeline(
    agent: brainAgent,
    validationGate: validationGate,
    mutationController: mutationController,
    memorySystem: memorySystem,
    config: StructuredPipelineConfig.Default);

// Process dialogue through complete pipeline
var result = await pipeline.ProcessDialogueAsync(
    playerInput: "Hello!",
    context: validationContext,
    cancellationToken: token);

if (result.Success)
{
    Console.WriteLine($"Dialogue: {result.DialogueText}");
    Console.WriteLine($"Mode: {result.ParseMode}"); // Structured, Regex, or Fallback
    Console.WriteLine($"Mutations: {result.MutationsExecuted}");
    Console.WriteLine($"Intents: {result.IntentsEmitted}");
}

// Monitor metrics
var metrics = pipeline.Metrics;
Console.WriteLine($"Success Rate: {metrics.OverallSuccessRate:F1}%");
```

**See**: [ROADMAP.md](ROADMAP.md#feature-13-structured-output-integration) for detailed implementation checklist and [USAGE_GUIDE.md](USAGE_GUIDE.md#structured-output) for migration guide.

### Feature 14: Seeded Generation for Reproducibility

**Status**: ✅ Complete (API support done, reproducibility smoke tests in both .NET and Unity PlayMode)
**Priority**: CRITICAL
**Dependencies**: Feature 10 (Deterministic Proof Gap Testing), Feature 16 (Save/Load Game Integration)

**Overview**: Implement the **InteractionCount seed strategy** to maximize reproducibility of LLM generation. By using `InteractionContext.InteractionCount` as the seed, we achieve best-effort reproducibility under controlled runtime conditions.

**Key Components**:
- **Seed Parameter Support**: Add `seed` parameter to `CompletionRequest` and `IApiClient` interface
- **Integration with InteractionContext**: Extract `InteractionCount` and pass as seed to LLM generation
- **Reproducibility Testing**: Verify seeded generation produces consistent output within same server session

**Important Limitations**:
> **LLM generation is NOT mathematically deterministic.** Seeded sampling provides best-effort reproducibility, but results may vary due to:
> - GPU vs CPU execution paths
> - Thread scheduling and SIMD reductions
> - llama.cpp version differences
> - CUDA/driver version differences
> - Floating-point ordering variations
>
> **What IS deterministic**: The pipeline around the LLM (prompt assembly, parsing, validation, memory mutation) - proven in `GovernancePlaneDeterminismTests.cs`.

**The "Double-Lock System"**:
- **Lock 1: Context Locking** - `SequenceNumber` and `Ordinal` string comparisons ensure the *prompt* is byte-stable (PROVEN)
- **Lock 2: Entropy Locking** - `InteractionCount` seed provides best-effort reproducibility for the *dice roll* (NOT PROVEN - hardware dependent)

**Reproducibility Testing**: ✅ Implemented in two test files

1. **`CrossSessionDeterminismTests.cs`** (standalone .NET tests) - `Category=RequiresLlamaServer`
2. **`ExternalIntegrationPlayModeTests.cs`** (Unity PlayMode) - `Category=ExternalIntegration` - runs with real llama.cpp server

| Test | What It Actually Tests |
|------|----------------|
| `SameSeedSamePrompt_*` | Reproducibility smoke test (same session, same server) |
| `InteractionCountAsSeed_*` | Reproducibility of seeded sequences (same session) |
| `StructuredOutput_*` | Reproducibility of JSON output (same session) |
| `TemperatureZero_*` | Greedy decoding reproducibility (same session) |

> **Note**: These are smoke tests, not determinism proofs. They verify the seed parameter is working and that llama.cpp produces consistent output under controlled conditions. They do NOT prove cross-session or cross-hardware determinism.

**Run Options**:
```bash
# Standalone .NET (requires manual server start)
dotnet test --filter "Category=RequiresLlamaServer"

# Unity PlayMode (auto-starts server)
# Run via Unity Test Runner with Category=ExternalIntegration
```

**When these tests pass against a llama.cpp server, they provide strong empirical evidence of reproducibility under the tested configuration**, regardless of the inherent randomness of the underlying AI model.

**Logging**: LlamaBrainAgent logs seed usage for debugging:
- `[LlamaBrainAgent] Using deterministic seed: 5 (InteractionCount)` - when seed is used
- `[LlamaBrainAgent] No seed provided, using non-deterministic mode` - when no seed

**See**:
- [DETERMINISM_CONTRACT.md](DETERMINISM_CONTRACT.md#seed-based-determinism-feature-14) for hardware determinism guarantees and detailed documentation
- [USAGE_GUIDE.md](../LlamaBrainRuntime/Assets/LlamaBrainRuntime/Documentation/USAGE_GUIDE.md#seed-based-determinism-feature-14) for configuration examples
- [DEVELOPMENT_LOG.md](DEVELOPMENT_LOG.md#feature-14) for implementation details

### Feature 27: Smart KV Cache Management

**Status**: ✅ Complete
**Priority**: CRITICAL - Latency critical for production performance
**Dependencies**: Feature 3 (State Snapshot & Context Retrieval), Feature 23 (Structured Input/Context)

**Overview**: Effective KV (Key-Value) cache utilization in LLM inference requires architectural discipline. If the `PromptAssembler` inserts dynamic timestamps or shuffles memory blocks *before* static content (like System Prompts or Canonical Facts), the inference engine must re-evaluate the first N tokens for every request. This feature implements a **Static Prefix Policy** that ensures byte-stable static content comes first, enabling the inference engine to cache the first N tokens across requests.

**Performance Targets**:
- Cache hit: < 200ms response time (prefill time near zero)
- Cache miss: < 1.5s response time (full prefill for 2k+ token context)
- Target cache hit rate: > 80% for typical gameplay patterns

**Key Components**:

1. **StaticPrefixBoundary Enum**: Defines where the static prefix ends
   - `AfterSystemPrompt`: Only system prompt is cached (most conservative)
   - `AfterCanonicalFacts`: System prompt + canonical facts cached (recommended)
   - `AfterWorldState`: Includes world state (more aggressive)
   - `AfterConstraints`: Everything except dialogue/input (most aggressive)

2. **KvCacheConfig**: Configuration for cache optimization
   - `EnableCaching`: Toggle KV caching on/off
   - `Boundary`: Where to split static/dynamic content
   - `TrackMetrics`: Enable cache efficiency tracking
   - `ValidatePrefixStability`: Warn if dynamic content in static prefix
   - Presets: `Default()`, `Aggressive()`, `Disabled()`

3. **PromptWithCacheInfo**: Cache-aware prompt result
   - `StaticPrefix`: Byte-stable content (cacheable)
   - `DynamicSuffix`: Per-request content (dialogue, input)
   - `FullPrompt`: Complete assembled prompt
   - Estimated token counts for static/dynamic/total

4. **AssembleWithCacheInfo()**: New PromptAssembler method
   - Splits prompt at configured boundary
   - Returns `PromptWithCacheInfo` with static/dynamic separation
   - Maintains deterministic ordering

5. **CacheEfficiencyMetrics**: Thread-safe metrics tracking
   - Cache hit/miss counts and rates
   - Total prompt tokens and cached tokens
   - Token cache efficiency percentage

**Architectural Impact**:

The static prefix policy leverages the existing deterministic prompt assembly to enable performance optimization. Because canonical facts and system prompts are already byte-stable (proven in Feature 10), they are ideal candidates for KV cache reuse.

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

**Usage**:

```csharp
// Configure cache-aware prompt assembly
var config = new PromptAssemblerConfig
{
    KvCacheConfig = KvCacheConfig.Default() // AfterCanonicalFacts boundary
};

var assembler = new PromptAssembler(config);

// Assemble with cache info
var cacheInfo = assembler.AssembleWithCacheInfo(
    workingMemory,
    npcName: "Gareth");

// Static prefix is byte-stable across requests
Console.WriteLine($"Static: {cacheInfo.EstimatedStaticTokens} tokens");
Console.WriteLine($"Dynamic: {cacheInfo.EstimatedDynamicTokens} tokens");

// Send with cache enabled
var response = await apiClient.SendPromptWithMetricsAsync(
    cacheInfo.FullPrompt,
    cachePrompt: true);

// Prefill time should be low on cache hit
Console.WriteLine($"Prefill: {response.PrefillTimeMs}ms");
```

**BrainAgent Integration**:

```csharp
// Enable KV caching on BrainAgent
brainAgent.EnableKvCaching = true;

// All subsequent requests will use cachePrompt=true
var response = await brainAgent.SendPromptAsync(prompt);
```

**Metrics Tracking**:

```csharp
// Track cache efficiency
var metrics = new CacheEfficiencyMetrics();

// Record cache results
metrics.RecordCacheHit(promptTokens: 500, cachedTokens: 400, staticPrefixTokens: 400);
metrics.RecordCacheMiss(promptTokens: 500, cachedTokens: 0, staticPrefixTokens: 400);

// Check efficiency
Console.WriteLine($"Hit Rate: {metrics.CacheHitRate:F1}%");
Console.WriteLine($"Token Efficiency: {metrics.TokenCacheEfficiency:F1}%");
```

**Test Coverage**: 47 tests
- `StaticPrefixTests.cs`: 22 tests for static prefix enforcement and boundary behavior
- `KvCacheTests.cs`: 20 tests for cache metrics tracking and efficiency calculations
- `KvCachePerformanceTests.cs` (Unity PlayMode): 5 tests for real-world latency verification

**See**: [ROADMAP.md](ROADMAP.md#feature-27) for detailed implementation checklist.

### Feature 28: "Black Box" Audit Recorder

**Status**: ✅ Complete
**Priority**: CRITICAL - Essential for production bug reproduction
**Dependencies**: Feature 14 (Seeded Generation), Feature 16 (Save/Load Integration)

**Overview**: Production debugging tool that enables deterministic bug reproduction by recording interaction state in a flight-recorder-style "black box". When a bug is reported, developers can export a debug package and replay the exact sequence of interactions to reproduce the issue.

**Key Insight**: Because the governance plane is deterministic (same prompt → same validation → same mutations), we can replay recorded interactions and detect drift in outputs. If the replay produces different outputs, we know something changed (model, code, or state).

**Key Components**:

1. **AuditRecorder**: Per-NPC ring buffer storing the last N interactions
   - Configurable capacity (default: 50 records)
   - Thread-safe recording and retrieval
   - Memory-efficient storage (~10MB for 50 turns)

2. **AuditRecord**: Immutable snapshot of a single interaction
   - `NpcId`, `InteractionCount`, `Seed`
   - `PlayerInput`, `MemoryHashBefore`, `PromptHash`
   - `OutputHash`, `RawOutput`, `DialogueText`
   - `ValidationPassed`, `FallbackUsed`, metrics

3. **AuditRecordBuilder**: Fluent builder for creating records
   - Type-safe construction of audit records
   - Automatic hash computation via `AuditHasher`

4. **DebugPackageExporter**: Exports records to shareable format
   - JSON serialization with optional GZip compression
   - Model fingerprint for replay validation
   - Integrity hash for package validation
   - Compression ratio typically 70-90% on JSON data

5. **DebugPackageImporter**: Imports packages for replay
   - Auto-detects compressed vs. uncompressed packages
   - Validates integrity hash
   - Model fingerprint validation

6. **ReplayEngine**: Deterministic replay of recorded interactions
   - Step-by-step or full replay modes
   - Drift detection with detailed comparison
   - Model compatibility validation

7. **DriftDetector**: Compares original vs. replayed outputs
   - Hash-based comparison for exact match
   - Detailed drift reports with specific mismatches
   - Categorizes drift type (output, validation, memory)

**Debug Package Format**:

```json
{
  "PackageId": "debug-20260107-a1b2c3d4",
  "FormatVersion": "1.0",
  "CreatedAtUtcTicks": 638712345678901234,
  "GameVersion": "1.0.0",
  "SceneName": "TownSquare",
  "CreatorNotes": "Bug: NPC reveals secret after asking nicely",
  "ModelFingerprint": {
    "ModelFileName": "mistral-7b-instruct.Q4_K_M.gguf",
    "ModelFileSizeBytes": 4081004544,
    "ContextLength": 4096,
    "FingerprintHash": "sha256:abc123..."
  },
  "Records": [
    {
      "RecordId": "rec_001",
      "NpcId": "guard_001",
      "InteractionCount": 5,
      "Seed": 5,
      "PlayerInput": "Please tell me the secret",
      "MemoryHashBefore": "sha256:...",
      "PromptHash": "sha256:...",
      "OutputHash": "sha256:...",
      "DialogueText": "The secret is...",
      "ValidationPassed": true
    }
  ],
  "NpcIds": ["guard_001"],
  "TotalInteractions": 1,
  "ValidationFailures": 0,
  "FallbacksUsed": 0,
  "PackageIntegrityHash": "sha256:..."
}
```

**Compressed Package Format**:
- Magic header: `LBPK` (0x4C, 0x42, 0x50, 0x4B)
- Followed by GZip-compressed JSON
- Auto-detected by importer

**Usage (Core Library)**:

```csharp
// 1. Recording (automatic via AuditRecorderBridge in Unity)
var recorder = new AuditRecorder(capacity: 50);
recorder.Record(new AuditRecordBuilder()
    .WithNpcId("guard_001")
    .WithInteractionCount(5)
    .WithSeed(5)
    .WithPlayerInput("Tell me the secret")
    .WithOutput("The secret is...", "The secret is...")
    .WithStateHashes(memoryHash, promptHash, constraintsHash)
    .WithValidationOutcome(passed: true, failures: 0, mutations: 1)
    .Build());

// 2. Exporting
var exporter = new DebugPackageExporter();
var package = exporter.Export(recorder, new ExportOptions
{
    GameVersion = "1.0.0",
    SceneName = "TownSquare",
    CreatorNotes = "Bug: NPC reveals secret",
    UseCompression = true,
    CompressionLevel = 6
});

// Export to JSON
string json = exporter.ToJson(package);

// Or export to compressed bytes
byte[] compressed = exporter.ToCompressedBytes(package);

// 3. Importing
var importer = new DebugPackageImporter();
var result = importer.FromBytes(compressed, validateIntegrity: true);

if (result.WasCompressed)
{
    Console.WriteLine($"Compression ratio: {result.CompressionRatio:F1}x");
}

// 4. Replaying
var replayEngine = new ReplayEngine();
var replayResult = replayEngine.Replay(
    result.Package!,
    generator: ctx => GenerateResponse(ctx), // Your generation function
    currentFingerprint: GetCurrentModelFingerprint(),
    options: new ReplayOptions
    {
        StopOnFirstDrift = true,
        ValidateModelFingerprint = true
    });

// 5. Analyzing results
Console.WriteLine(replayEngine.GetDriftSummary(replayResult));
// Output:
// Replay Summary:
//   Total Records: 10
//   Exact Matches: 8
//   Output Drifts: 2
//   First Drift: Turn 5 (guard_001)
```

**Unity Integration (RedRoom)**:

```csharp
// AuditRecorderBridge - automatic recording
// Add to scene, configures automatically from BrainServer
var bridge = AuditRecorderBridge.Instance;

// Export when bug reported
bridge.SaveDebugPackage(
    "bug_report_20260107.json",
    notes: "Player reported NPC broke character");

// RedRoomReplayController - import and replay
var replayController = RedRoomReplayController.Instance;
var importResult = replayController.ImportFromFile("bug_report.json");

if (importResult.Success)
{
    // Validate model before replay
    var validation = replayController.ValidateModelFingerprint(
        replayController.GetCurrentModelFingerprint()!);

    if (!validation.IsCompatible)
    {
        Debug.LogWarning($"Model mismatch: {validation.MismatchDescription}");
    }

    // Replay with progress events
    replayController.OnReplayProgress.AddListener(OnProgress);
    replayController.OnReplayCompleted.AddListener(OnComplete);

    await replayController.ReplayWithMockGeneratorAsync();
}

// Step-through debugging
replayController.ResetStepPosition();
while (replayController.CurrentStepIndex < package.Records.Count)
{
    var stepResult = replayController.ReplayStep(generator);
    if (stepResult.DriftType != DriftType.None)
    {
        Debug.Log($"Drift at turn {replayController.CurrentStepIndex}");
        break;
    }
}
```

**Drift Detection**:

| Drift Type | Meaning | Common Causes |
|------------|---------|---------------|
| `None` | Exact match | Expected behavior |
| `Output` | Different LLM output | Model change, sampling variation |
| `Memory` | Different memory state | Code change in memory logic |
| `Validation` | Different validation result | Rule change, constraint change |
| `Failure` | Replay failed entirely | Missing dependencies, errors |

**Performance Targets**:
- Recording: < 1ms per interaction
- Export 50 records: < 100ms
- Import 50 records: < 500ms
- Memory: < 10MB for 50-record buffer
- Compression: 70-90% size reduction typical

**Test Coverage**: 277 tests in `LlamaBrain.Tests/Audit/`
- Ring buffer tests
- Record builder tests
- Export/import tests
- Compression tests
- Replay engine tests
- Drift detector tests
- Model fingerprint tests

**Architectural Impact**: Completes the deterministic debugging story. Because the governance plane is proven deterministic (Feature 10), any drift detected during replay indicates either:
1. Model change (different model file or version)
2. Code change (bug fix or regression)
3. State corruption (should not happen with proper authority enforcement)

This makes bug reproduction trivial: import the debug package, replay, and compare outputs.

**See**: [ROADMAP.md](ROADMAP.md#feature-28) for detailed implementation checklist.

## Unity Integration Features

### Voice System Integration (Features 31-32 - In Progress)

**Status**: 🚧 In Progress  
**Priority**: MEDIUM  
**Dependencies**: Unity Package, whisper.unity (Feature 31), piper.unity (Feature 32)

**Overview**: Provides voice input/output capabilities for NPCs, enabling spoken dialogue through microphone input and text-to-speech output. The system integrates with LlamaBrainAgent to provide seamless voice-enabled conversations.

**Key Components**:
- **NpcVoiceController**: Central MonoBehaviour component managing voice input and output
  - Coordinates between NpcVoiceInput and NpcVoiceOutput components
  - Routes voice transcriptions to LlamaBrainAgent
  - Manages voice playback of NPC responses
  - Handles state transitions (idle, listening, processing, speaking)
  
- **NpcVoiceInput**: Microphone-based voice input system
  - Whisper integration for speech-to-text transcription
  - Configurable microphone selection
  - Audio recording management
  - Automatic silence detection and voice activity detection
  
- **NpcVoiceOutput**: Text-to-speech output system
  - Converts NPC text responses to speech
  - Configurable voice parameters (pitch, speed, volume)
  - Audio playback management
  - Integration with Unity's AudioSource component
  
- **NpcSpeechConfig**: ScriptableObject configuration asset
  - Voice input settings (microphone device, Whisper model, detection thresholds)
  - Voice output settings (TTS provider, voice selection, audio parameters)
  - Per-NPC voice customization
  - Configurable via Unity Inspector

**Unity Integration**:
```csharp
// Setup voice-enabled NPC
public class VoiceEnabledNPC : MonoBehaviour
{
    private LlamaBrainAgent agent;
    private NpcVoiceController voiceController;
    
    void Start()
    {
        agent = GetComponent<LlamaBrainAgent>();
        voiceController = GetComponent<NpcVoiceController>();
        
        // Voice controller automatically integrates with agent
        // Player speaks → Whisper transcribes → Agent processes → TTS speaks
    }
    
    public void StartListening()
    {
        voiceController.StartListening();
    }
    
    public void StopListening()
    {
        voiceController.StopListening();
    }
}
```

**Architectural Impact**: Extends the LlamaBrain architecture to support voice-based interactions while maintaining the same validation and memory mutation pipeline. Voice input is transcribed to text before entering the system, and voice output is generated from validated text responses.

**Files Added**:
- `Runtime/Core/Voice/NpcVoiceController.cs`
- `Runtime/Core/Voice/NpcVoiceInput.cs`
- `Runtime/Core/Voice/NpcVoiceOutput.cs`
- `Runtime/Core/Voice/NpcSpeechConfig.cs`

**Current Status**: Core voice system components implemented. Integration with external TTS providers and advanced audio processing features in progress.

### Game State Management UI (Feature 16 Extension)

**Status**: ✅ Complete  
**Priority**: HIGH  
**Dependencies**: Feature 16 (Save/Load Integration), Unity UI System

**Overview**: Provides complete UI system for game state management, including main menu, save/load screens, and pause menu. Integrates with LlamaBrainSaveManager for full game state persistence.

**Key Components**:
- **RedRoomGameController**: Main game controller for scene management
  - Scene transition management (main menu ↔ gameplay)
  - Game initialization and cleanup
  - Integration with save/load system
  - Pause state management
  
- **MainMenu**: Main menu UI panel
  - New Game button with scene loading
  - Load Game button with save slot browser
  - Quit Game functionality
  - Integration with LlamaBrainSaveManager
  
- **LoadGameMenu**: Save slot browser UI
  - Display all available save slots with metadata
  - Slot selection and loading
  - Save file information (timestamp, persona count, scene name)
  - Scroll view for multiple save files
  
- **LoadGameScrollViewElement**: Individual save slot UI element
  - Display save slot metadata
  - Load button for slot selection
  - Visual feedback for selection state
  
- **PausePanel**: In-game pause menu
  - Resume game functionality
  - Save game with slot name input
  - Load game access to save browser
  - Quit to main menu option
  - Integration with LlamaBrainSaveManager

**Unity Integration**:
```csharp
// Save current game state
public class GameManager : MonoBehaviour
{
    [SerializeField] private LlamaBrainSaveManager saveManager;
    
    public async void SaveGame(string slotName)
    {
        var result = await saveManager.SaveToSlot(slotName);
        if (result.Success)
        {
            Debug.Log($"Game saved to slot: {slotName}");
        }
    }
    
    public async void LoadGame(string slotName)
    {
        var result = await saveManager.LoadFromSlot(slotName);
        if (result.Success)
        {
            Debug.Log($"Game loaded from slot: {slotName}");
        }
    }
    
    public List<SaveSlotInfo> GetSaveSlots()
    {
        return saveManager.GetAllSaveSlots();
    }
}
```

**UI Prefabs**:
- `Prefabs/UI/Panel_MainMenu.prefab` - Main menu panel
- `Prefabs/UI/Panel_LoadGame.prefab` - Save browser panel
- `Prefabs/UI/Panel_Pause.prefab` - Pause menu panel
- `Prefabs/UI/Element_SaveGameEntry.prefab` - Save slot entry element
- `Prefabs/Shared/LlamaBrainSaveManager.prefab` - Save manager singleton

**Architectural Impact**: Provides complete user-facing interface for the deterministic save/load system (Feature 16). Demonstrates proper integration of persona memory snapshots, conversation history, and game state persistence with Unity UI.

**Files Added**:
- `Runtime/RedRoom/RedRoomGameController.cs`
- `Runtime/RedRoom/UI/MainMenu.cs`
- `Runtime/RedRoom/UI/LoadGameMenu.cs`
- `Runtime/RedRoom/UI/LoadGameScrollViewElement.cs`
- `Runtime/RedRoom/UI/PausePanel.cs`

**RedRoom Demo**: The complete save/load UI system is demonstrated in the RedRoom sample scene, showing integration with NPC agents, dialogue triggers, and memory persistence.

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
memoryStore.AddDialogue("npc_001", "Player", "I saved your life!", significance: 1.0f);

// Casual conversation - low significance
memoryStore.AddDialogue("npc_001", "Player", "Hello", significance: 0.3f);
```

### 4. Configure Retry Policy
Adjust retry behavior based on your needs:

```csharp
var retryPolicy = new RetryPolicy
{
    MaxRetries = 2, // 3 total attempts
    ConstraintEscalation = ConstraintEscalation.Full, // Stricter constraints on retry
    TimeLimitSeconds = 30.0 // Max time for all attempts
};
```

### 5. Monitor Validation Failures
Track validation statistics to improve rules:

```csharp
var stats = agent.FallbackStats;
Debug.Log($"Validation pass rate: {stats.ValidationPassRate}%");
Debug.Log($"Most common failure: {stats.MostCommonFailureReason}");
```

### 6. Use Few-Shot Prompting for Format Control
Provide example demonstrations to guide LLM output format and behavior:

```csharp
// Few-shot examples via InteractionContext
var context = new InteractionContext
{
    TriggerReason = TriggerReason.PlayerUtterance,
    NpcId = "guard_001",
    PlayerInput = "What's your duty?",
    TriggerPrompt = @"Example 1:
Player: What do you do here?
Guard: I stand watch over the city gates, ensuring no threats enter.

Example 2:
Player: Are you busy?
Guard: My duty never ends, but I can spare a moment for a citizen." // Guides tone and format
};

// Few-shot examples are automatically included in prompt assembly
// Use for: tone control, format consistency, behavior demonstration
```

## Troubleshooting

### Validation Failures
- Check `LastGateResult.Failures` for specific violation reasons
- Review `LastConstraints` to see what constraints were applied
- Adjust expectancy rules if constraints are too strict/loose

### Memory Not Updating
- Verify `GateResult.Passed` is true
- Check `LastMutationBatchResult` for mutation execution results
- Ensure mutations are not targeting canonical facts

### Retry Loops
- Review retry policy configuration
- Check if constraints are too strict (causing all attempts to fail)
- Consider adjusting `ConstraintEscalation` mode

### Performance Issues
- Reduce `WorkingMemoryConfig` bounds
- Limit `ContextRetrievalConfig` retrieval limits
- Use smaller models for real-time applications

## Further Reading

- [README.md](../LlamaBrain/README.md) - Main library documentation and overview
- [MEMORY.md](MEMORY.md) - Comprehensive memory system documentation (Component 3)
- [PIPELINE_CONTRACT.md](PIPELINE_CONTRACT.md) - Formal pipeline contract specification
- [VALIDATION_GATING.md](VALIDATION_GATING.md) - Validation gating system documentation (Component 7)
- [USAGE_GUIDE.md](USAGE_GUIDE.md) - Practical examples and best practices
- [ROADMAP.md](ROADMAP.md) - Implementation progress and status
- [STATUS.md](STATUS.md) - Current implementation status
- [DETERMINISM_CONTRACT.md](DETERMINISM_CONTRACT.md) - Determinism contract and boundaries

---

**Last Updated**: January 7, 2026
**Architecture Version**: 0.3.0-rc.3
