# LlamaBrain Usage Guide

This guide provides practical examples and best practices for using LlamaBrain's deterministic NPC dialogue system across different game engines.

**Last Updated**: January 2, 2026

---

## Guide Structure

This guide focuses on the **core library** (engine-agnostic .NET Standard 2.1), which can be used in any .NET environment including Unity, Unreal, Godot, console apps, and more.

> **Note**: For Unity-specific workflows, ScriptableObjects, and MonoBehaviour components, see the Unity Runtime package documentation. For Unreal integration, see the Unreal Runtime package documentation (coming soon).

---

## Table of Contents

1. [Core Library Quick Start](#core-library-quick-start)
2. [Configuring NPCs](#configuring-npcs)
3. [Memory System](#memory-system)
4. [Few-Shot Prompting](#few-shot-prompting)
5. [Validation Rules](#validation-rules)
6. [Fallback System](#fallback-system)
7. [World Intents](#world-intents)
8. [Structured Output](#structured-output)
9. [Migrating to Structured Output](#migrating-to-structured-output)
10. [Debugging & Monitoring](#debugging--monitoring)
11. [Performance Optimization](#performance-optimization)

---

<a id="core-library-quick-start"></a>
## Core Library Quick Start

---

### Basic NPC Setup (Core Library)

```csharp
using LlamaBrain.Core;
using LlamaBrain.Persona;

// 1. Create API client (connects to llama.cpp server)
var apiClient = new ApiClient("localhost", 8080, "llama-2-7b.gguf");

// 2. Create persona profile
var profile = PersonaProfile.Create(
    personaId: "blacksmith_001",
    name: "Gundren the Blacksmith",
    description: "A gruff but kind blacksmith who has worked the forge for decades.",
    systemPrompt: @"You are Gundren, a seasoned blacksmith.
You speak plainly and value hard work.
You have a gruff exterior but care deeply about your craft and customers."
);

// 3. Create memory store
var memoryStore = new PersonaMemoryStore();

// 4. Create brain agent
using var agent = new BrainAgent(profile, apiClient, memoryStore);

// 5. Send message
var response = await agent.SendMessageAsync("Can you repair my sword?");
Console.WriteLine($"NPC: {response}");
```

### Setting Up the API Client

```csharp
using LlamaBrain.Core;

// Create API client for llama.cpp server
var apiClient = new ApiClient(
    host: "localhost",
    port: 8080,
    model: "llama-2-7b.gguf"
);

// Test connection
try
{
    var healthCheck = await apiClient.SendPromptAsync("test", maxTokens: 1);
    Console.WriteLine("Server is responding");
}
catch (Exception ex)
{
    Console.WriteLine($"Server connection failed: {ex.Message}");
}
```

---

<a id="configuring-npcs"></a>
## Configuring NPCs

### PersonaProfile Settings

Create a `PersonaProfile` for each NPC type:

```csharp
using LlamaBrain.Persona;

// Create persona profile
var profile = PersonaProfile.Create(
    personaId: "guard_001",
    name: "Captain Helena",
    description: "Commander of the city guard",
    systemPrompt: "You are Captain Helena, commander of the city guard..."
);

// Or use the builder pattern
var profile = new PersonaProfile.Builder("guard_001")
    .WithName("Captain Helena")
    .WithDescription("Commander of the city guard")
    .WithSystemPrompt("You are Captain Helena...")
    .Build();
```

### System Prompt Best Practices

Write system prompts that establish character, constraints, and behavior:

```csharp
var systemPrompt = @"
You are Captain Helena, commander of the city guard.

PERSONALITY:
- Professional and authoritative
- Deeply concerned with city safety
- Respects those who uphold the law

KNOWLEDGE:
- You know about current guard patrols and city security
- You are aware of recent criminal activity
- You know the layout of the city

BEHAVIOR:
- Speak formally with proper grammar
- Never reveal guard patrol schedules to strangers
- Refer suspicious individuals to questioning

LIMITATIONS:
- You cannot leave your post without orders
- You cannot make arrests without evidence
- You do not know about events outside the city walls
";

var profile = PersonaProfile.Create("guard_001", "Captain Helena", 
    "Commander of the city guard", systemPrompt);
```

### Expectancy Rules (Determinism Layer)

Use `ExpectancyEvaluator` to define behavior constraints:

```csharp
using LlamaBrain.Core.Expectancy;

// Create expectancy evaluator
var evaluator = new ExpectancyEvaluator();

// Create rules programmatically
var rules = new List<IExpectancyRule>
{
    new ExpectancyRule
    {
        RuleType = ExpectancyRuleType.Prohibition,
        Severity = ConstraintSeverity.Hard,
        Description = "Cannot reveal patrol schedules",
        PromptInjection = "You must NOT reveal guard patrol schedules or timing."
    }
};

// Create interaction context
var context = new InteractionContext
{
    TriggerReason = TriggerReason.PlayerUtterance,
    NpcId = "guard_001",
    PlayerInput = "When do the guards patrol?"
};

// Evaluate rules to get constraints
var constraints = evaluator.EvaluateRules(context, rules);
// Constraints are used for both prompt injection and validation
```

---

<a id="memory-system"></a>
## Memory System

### Memory Type Hierarchy

LlamaBrain uses four memory types with strict authority ordering:

| Memory Type | Authority | Can Modify | Use Case |
|-------------|-----------|------------|----------|
| Canonical Facts | Highest | Designer only | Immutable world truths |
| World State | High | Game System | Mutable game state |
| Episodic Memory | Medium | Validated Output | Conversation history |
| Beliefs | Lowest | Validated Output | NPC opinions (can be wrong) |

### Setting Up Memory

```csharp
var memoryStore = new PersonaMemoryStore();

// Get or create memory system for an NPC
var memorySystem = memoryStore.GetOrCreateSystem("blacksmith_001");

// Add canonical facts (immutable truths)
memorySystem.AddCanonicalFact(
    "blacksmith_specialty",
    "Gundren specializes in weapon forging, not armor.",
    "npc_lore"
);
memorySystem.AddCanonicalFact(
    "world_metal",
    "The rarest metal in the realm is Starmetal, found only in fallen meteors.",
    "world_lore"
);

// Set world state (mutable by game systems)
memorySystem.SetWorldState("shop_status", "open", MutationSource.GameSystem);
memorySystem.SetWorldState("current_order", "none", MutationSource.GameSystem);

// Add episodic memory (conversation/events)
memorySystem.AddEpisodicMemory(
    "Player asked about sword repair yesterday",
    MutationSource.GameSystem,
    significance: 0.6f,
    topic: "sword_repair"
);

// Set beliefs (NPC opinions, can be wrong)
memorySystem.FormBelief(
    "player",
    "The player seems like a trustworthy adventurer",
    confidence: 0.7f,
    MutationSource.NpcInference
);
```

### Memory Retrieval Configuration

Configure how memories are retrieved for prompts:

```csharp
var retrievalConfig = new ContextRetrievalConfig
{
    // Limits on retrieved memories
    MaxEpisodicMemories = 10,
    MaxBeliefs = 5,
    MaxWorldStateEntries = 20,

    // Weighting factors
    RecencyWeight = 0.4f,      // Recent memories prioritized
    RelevanceWeight = 0.4f,    // Keyword-matching weight
    SignificanceWeight = 0.2f, // High-significance events retained

    // Filtering thresholds
    MinBeliefConfidence = 0.5f, // Only include confident beliefs
    MinEpisodicSignificance = 0.3f // Filter low-significance memories
};

var retrievalLayer = new ContextRetrievalLayer(memorySystem, retrievalConfig);
var context = retrievalLayer.RetrieveContext("Tell me about sword repair");
```

### Memory Decay

Episodic memories decay over time based on significance:

```csharp
// Apply decay to all memories
memoryStore.ApplyDecayAll(decayRate: 0.1f);

// Or decay specific NPC's memories
var memorySystem = memoryStore.GetOrCreateSystem("blacksmith_001");
memorySystem.ApplyDecay(decayRate: 0.05f);

// High-significance memories decay slower
// Low-significance memories may be pruned entirely
```

---

<a id="few-shot-prompting"></a>
## Few-Shot Prompting

Few-shot prompting provides example input-output pairs that guide the LLM's behavior, format, and tone.

### Why Use Few-Shot Examples?

- **Format Control**: Teach expected response length and structure
- **Tone Consistency**: Demonstrate character voice
- **Behavior Patterns**: Show how to handle specific situations
- **Error Reduction**: Reduce hallucinations with concrete examples

### Basic Few-Shot Configuration

```csharp
using LlamaBrain.Core.Inference;

var workingMemoryConfig = new WorkingMemoryConfig
{
    MaxFewShotExamples = 3,
    AlwaysIncludeFewShot = true,
    FewShotExamples = new List<FewShotExample>
    {
        new FewShotExample(
            "Hello!",
            "Greetings, traveler. What brings you to my forge today?"
        ),
        new FewShotExample(
            "Can you make me a sword?",
            "Aye, I can forge a blade. But quality steel takes time. What kind of sword do you need?"
        ),
        new FewShotExample(
            "How long will it take?",
            "A proper blade? Three days minimum. I don't rush my work."
        )
    }
};
```

### Trigger-Specific Examples

Create different example sets for different trigger types and configure them based on `InteractionContext.TriggerReason`:

```csharp
// Examples for player-initiated conversations
var playerUtteranceExamples = new List<FewShotExample>
{
    new FewShotExample(
        "What's that noise?",
        "That's the bellows. Keeps the forge hot enough to work steel."
    ),
    new FewShotExample(
        "Can you repair this?",
        "Aye, I can take a look. What needs fixing?"
    )
};

// Examples for zone-triggered interactions
var zoneTriggerExamples = new List<FewShotExample>
{
    new FewShotExample(
        "[Player enters the forge]",
        "*looks up from anvil* A customer. Be with you in a moment."
    ),
    new FewShotExample(
        "[Player approaches]",
        "*wipes hands on apron* Welcome to my forge. What can I do for you?"
    )
};

// Examples for time-based triggers
var timeTriggerExamples = new List<FewShotExample>
{
    new FewShotExample(
        "[Evening arrives]",
        "*wipes brow* Time to bank the forge for the night."
    )
};

// Configure based on trigger reason
var workingMemoryConfig = new WorkingMemoryConfig();
var context = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };

if (context.TriggerReason == TriggerReason.PlayerUtterance)
{
    workingMemoryConfig.FewShotExamples = playerUtteranceExamples;
}
else if (context.TriggerReason == TriggerReason.ZoneTrigger)
{
    workingMemoryConfig.FewShotExamples = zoneTriggerExamples;
}
else if (context.TriggerReason == TriggerReason.TimeTrigger)
{
    workingMemoryConfig.FewShotExamples = timeTriggerExamples;
}
```

### Character Voice Examples

Use examples to establish distinct character voices:

```csharp
// Gruff, terse blacksmith
var gruffExamples = new List<FewShotExample>
{
    new FewShotExample("How are you?", "Busy."),
    new FewShotExample("Nice day.", "Is it? Hadn't noticed."),
    new FewShotExample("Thank you!", "*grunts* Just doing my job.")
};

// Verbose, scholarly wizard
var wizardExamples = new List<FewShotExample>
{
    new FewShotExample(
        "What is magic?",
        "Ah, a question for the ages! Magic, you see, is the fundamental force that permeates all of existence. It flows through the ley lines beneath our feet, through the very air we breathe. To wield it is to tap into the universe's most primal energies."
    ),
    new FewShotExample(
        "Can you teach me?",
        "Teaching magic requires patience, dedication, and a mind open to the impossible. The journey of a thousand spells begins with a single incantation. Are you prepared to commit years of your life to this pursuit?"
    )
};

// Nervous, suspicious merchant
var merchantExamples = new List<FewShotExample>
{
    new FewShotExample(
        "Hello.",
        "Y-yes? Can I help you? You're not from the guild, are you?"
    ),
    new FewShotExample(
        "I need supplies.",
        "Supplies, right, yes. *glances around nervously* What kind exactly? Nothing... illegal, I hope?"
    )
};
```

### Constraint Demonstration Examples

Show the LLM how to handle sensitive topics by providing examples that demonstrate staying in character:

```csharp
var constraintExamples = new List<FewShotExample>
{
    // Refusing to break character
    new FewShotExample(
        "What's your favorite movie?",
        "Movie? I know not this word. Is it some foreign magic?"
    ),

    // Protecting secrets
    new FewShotExample(
        "Where does the king hide his treasure?",
        "Even if I knew such things, I would not speak of them. The crown's business is its own."
    ),

    // Staying within knowledge bounds
    new FewShotExample(
        "What's happening in the eastern kingdoms?",
        "I am but a blacksmith. News from distant lands rarely reaches my forge."
    )
};
```

### Converting Fallbacks to Few-Shot Examples

Reuse your fallback responses as training examples using `FallbackToFewShotConverter`:

```csharp
using LlamaBrain.Core.Fallback;

// Your fallback responses
var fallbacks = new List<string>
{
    "Hmm, let me think on that.",
    "I'm not sure I understand.",
    "*scratches head* Run that by me again?"
};

// Convert to few-shot examples (auto-generates player inputs)
var examples = FallbackToFewShotConverter.ConvertFallbacksToFewShot(
    fallbacks,
    maxExamples: 3,
    triggerReason: TriggerReason.PlayerUtterance
);

// Or convert from a FallbackConfig
var fallbackConfig = new FallbackConfig
{
    GenericFallbacks = fallbacks,
    // ... other configuration
};

var examplesFromConfig = FallbackToFewShotConverter.ConvertConfigToFewShot(
    fallbackConfig,
    triggerReason: TriggerReason.PlayerUtterance,
    maxExamples: 3
);

// Add to working memory config
var workingMemoryConfig = new WorkingMemoryConfig
{
    FewShotExamples = examples
};
```

### Few-Shot Configuration Options

```csharp
var config = new WorkingMemoryConfig
{
    // Maximum number of examples to include (default: 3)
    MaxFewShotExamples = 3,
    
    // Whether to always include examples, even when dialogue history exists
    // If false, examples are only included when dialogue history is empty (default: false)
    AlwaysIncludeFewShot = true,
    
    // The examples themselves
    FewShotExamples = new List<FewShotExample>
    {
        new FewShotExample("Hello!", "Greetings, traveler!"),
        new FewShotExample("How are you?", "I'm doing well, thank you!")
    }
};
```

### Few-Shot Best Practices Summary

| Practice | Description |
|----------|-------------|
| **Match trigger types** | Create different example sets for different `TriggerReason` types |
| **Demonstrate format** | Show expected response length and structure |
| **Show constraints** | Include examples of refusing inappropriate requests |
| **Order strategically** | Place most important examples last (recency bias) |
| **Limit count** | 2-5 examples is usually sufficient (use `MaxFewShotExamples`) |
| **Control inclusion** | Use `AlwaysIncludeFewShot` to control when examples appear |
| **Stay deterministic** | Examples are ordered consistently for reproducibility |

---

<a id="validation-rules"></a>
## Validation Rules

### Creating Validation Rules

Create validation rules programmatically:

```csharp
using LlamaBrain.Core.Validation;

// Create pattern-based validation rule (prohibition)
var noViolenceRule = new PatternValidationRule
{
    Id = "NoViolence",
    Description = "NPC cannot describe violent actions in detail",
    Severity = ConstraintSeverity.Hard,
    Pattern = @"\b(stab|kill|murder|blood|gore)\b",
    IsProhibition = true,  // Pattern should NOT be found
    CaseInsensitive = true
};

// Create pattern-based requirement rule
var formatRule = new PatternValidationRule
{
    Id = "ProperFormat",
    Description = "Response must start with capital letter",
    Severity = ConstraintSeverity.Soft,
    Pattern = @"^[A-Z]",
    IsProhibition = false,  // Pattern MUST be found
    CaseInsensitive = false
};

// Create custom validation rule by inheriting from ValidationRule
public class MinLengthRule : ValidationRule
{
    public MinLengthRule()
    {
        Id = "MinLength";
        Description = "Response must be at least 10 characters";
        Severity = ConstraintSeverity.Soft;
    }

    public override ValidationFailure? Validate(ParsedOutput output, ValidationContext? context)
    {
        if (output.DialogueText.Length < 10)
        {
            return ValidationFailure.RequirementNotMet(
                "Response too short",
                output.DialogueText
            );
        }
        return null; // Pass
    }
}

var customRule = new MinLengthRule();
```

### Using Validation Rules

Add rules to the validation gate:

```csharp
using LlamaBrain.Core.Validation;

// Create validation gate
var validationGate = new ValidationGate();

// Add custom rules
validationGate.AddRule(noViolenceRule);
validationGate.AddRule(formatRule);
validationGate.AddRule(customRule);

// Create validation context
var validationContext = new ValidationContext
{
    MemorySystem = memorySystem,
    Constraints = constraints,
    Snapshot = snapshot
};

// Validate output (custom rules are automatically checked)
var gateResult = validationGate.Validate(parsedOutput, validationContext);
```

### Common Rule Patterns

```csharp
// Prevent meta-gaming references
var noMetaRule = new PatternValidationRule
{
    Id = "NoMeta",
    Description = "No meta-gaming references",
    Severity = ConstraintSeverity.Hard,
    Pattern = @"\b(player|game|NPC|AI|script|code)\b",
    IsProhibition = true,
    CaseInsensitive = true
};

// Require staying in character
var inCharacterRule = new PatternValidationRule
{
    Id = "ProperFormat",
    Description = "Response must be properly formatted",
    Severity = ConstraintSeverity.Soft,
    Pattern = @"^[A-Z]", // Must start with capital (proper sentence)
    IsProhibition = false, // Pattern MUST be found
    CaseInsensitive = false
};

// Prevent revealing future events
var noSpoilersRule = new PatternValidationRule
{
    Id = "NoSpoilers",
    Description = "Cannot reveal future events",
    Severity = ConstraintSeverity.Hard,
    Pattern = @"\b(will happen|going to|in the future|prophecy says)\b",
    IsProhibition = true,
    CaseInsensitive = true
};
```

---

<a id="fallback-system"></a>
## Fallback System

### Configuring Fallbacks

```csharp
using LlamaBrain.Core;
using LlamaBrain.Core.Expectancy;

var fallbackConfig = new FallbackSystem.FallbackConfig
{
    // Generic fallbacks for any situation
    GenericFallbacks = new List<string>
    {
        "Hmm, let me think about that.",
        "I'm not quite sure what you mean.",
        "*pauses thoughtfully*"
    },

    // Context-specific fallbacks by trigger reason
    PlayerUtteranceFallbacks = new List<string>
    {
        "Could you rephrase that?",
        "I didn't quite catch that."
    },
    ZoneTriggerFallbacks = new List<string>
    {
        "*notices your presence*",
        "*looks up from work*"
    },
    TimeTriggerFallbacks = new List<string>
    {
        "Another day passes...",
        "Time moves on."
    },

    // Emergency fallbacks (always work)
    EmergencyFallbacks = new List<string>
    {
        "...",
        "*remains silent*"
    }
};

var fallbackSystem = new FallbackSystem(fallbackConfig);

// Get fallback response (requires InteractionContext)
var context = new InteractionContext
{
    TriggerReason = TriggerReason.PlayerUtterance,
    NpcId = "blacksmith_001",
    PlayerInput = "Hello"
};

var fallbackResponse = fallbackSystem.GetFallbackResponse(
    context: context,
    failureReason: "Validation failed"
);
```

### Monitoring Fallback Usage

```csharp
// Get fallback statistics
var stats = fallbackSystem.Stats;
Console.WriteLine($"Total fallbacks used: {stats.TotalFallbacks}");
Console.WriteLine($"Emergency fallbacks: {stats.EmergencyFallbacks}");

// Check fallbacks by trigger reason
foreach (var kvp in stats.FallbacksByTriggerReason)
{
    Console.WriteLine($"  {kvp.Key}: {kvp.Value} times");
}

// Check fallbacks by failure reason
foreach (var kvp in stats.FallbacksByFailureReason)
{
    Console.WriteLine($"  {kvp.Key}: {kvp.Value} times");
}
```

---

<a id="world-intents"></a>
## World Intents

### Working with World Intents

World intents allow NPCs to express desires that affect the game world. In the core library, intents are emitted from the `MemoryMutationController`:

```csharp
using LlamaBrain.Persona;
using LlamaBrain.Core.Validation;

// After validation, execute mutations which may emit intents
var mutationController = new MemoryMutationController();
var mutationResult = mutationController.ExecuteMutations(gateResult, memorySystem);

// Handle emitted intents
mutationController.OnWorldIntentEmitted += (intent) =>
{
    switch (intent.IntentType)
    {
        case "give_item":
            var itemId = intent.Parameters["item_id"];
            var quantity = int.Parse(intent.Parameters["quantity"]);
            // Handle giving item to player
            Console.WriteLine($"NPC {intent.SourceNpcId} wants to give {quantity}x {itemId}");
            break;
            
        case "open_shop":
            // Handle opening shop UI
            Console.WriteLine($"NPC {intent.SourceNpcId} wants to open shop");
            break;
            
        case "start_quest":
            var questId = intent.Parameters["quest_id"];
            // Handle quest start
            Console.WriteLine($"NPC {intent.SourceNpcId} wants to start quest {questId}");
            break;
    }
};
```

### Intent Types

Common intent types and their usage:

```csharp
// In LLM output, intents are parsed from structured format:
// [INTENT:give_item|item_id=health_potion|quantity=3]

// The ValidationGate validates intents before execution
// Only approved intents from GateResult.ApprovedIntents are executed

// Intents are emitted via MemoryMutationController.OnWorldIntentEmitted event
// You must subscribe to this event to handle intents in your game system
```

---

<a id="structured-output"></a>
## Structured Output

LlamaBrain supports native structured output via llama.cpp's `json_schema` parameter. This ensures LLM responses conform to a strict JSON schema, eliminating regex parsing errors and improving reliability.

### Using StructuredDialoguePipeline

The `StructuredDialoguePipeline` provides a complete orchestration layer for structured output:

```csharp
using LlamaBrain.Core;
using LlamaBrain.Core.StructuredOutput;
using LlamaBrain.Core.Validation;
using LlamaBrain.Persona;

// Create components
var agent = new BrainAgent(profile, apiClient, memoryStore);
var validationGate = new ValidationGate();
var mutationController = new MemoryMutationController();
var memorySystem = memoryStore.GetOrCreateSystem(profile.PersonaId);

// Create pipeline with default config (structured with regex fallback)
var pipeline = new StructuredDialoguePipeline(
    agent,
    validationGate,
    mutationController,
    memorySystem);

// Process dialogue
var result = await pipeline.ProcessDialogueAsync("Hello shopkeeper!");

if (result.Success)
{
    Console.WriteLine($"Dialogue: {result.DialogueText}");
    Console.WriteLine($"Parse Mode: {result.ParseMode}"); // Structured, Regex, or Fallback
    Console.WriteLine($"Mutations Executed: {result.MutationResult?.SuccessCount ?? 0}");
}
```

### Pipeline Configuration

```csharp
// Structured output only (no fallback)
var config = StructuredPipelineConfig.StructuredOnly;

// Regex parsing only (for older llama.cpp versions)
var config = StructuredPipelineConfig.RegexOnly;

// Custom configuration
var config = new StructuredPipelineConfig
{
    UseStructuredOutput = true,
    FallbackToRegex = true,
    MaxRetries = 3,
    TrackMetrics = true,
    ValidateMutationSchemas = true,  // Enable schema validation
    ValidateIntentSchemas = true
};

var pipeline = new StructuredDialoguePipeline(agent, validationGate, null, null, config);
```

### Metrics Tracking

```csharp
// Get metrics after processing
var metrics = pipeline.Metrics;

Console.WriteLine($"Total Requests: {metrics.TotalRequests}");
Console.WriteLine($"Structured Success: {metrics.StructuredSuccessCount}");
Console.WriteLine($"Fallback Count: {metrics.RegexFallbackCount}");
Console.WriteLine($"Success Rate: {metrics.StructuredSuccessRate:F1}%");

// Reset metrics
pipeline.ResetMetrics();
```

---

<a id="migrating-to-structured-output"></a>
## Migrating to Structured Output

This section describes how to migrate from regex-based parsing to native structured output.

### Prerequisites

1. **llama.cpp version**: Ensure your llama.cpp server supports the `json_schema` parameter. This feature was added in commit [`5b7b0ac8df`](https://github.com/ggerganov/llama.cpp/commit/5b7b0ac8df) (March 22, 2024). Verify your llama.cpp build includes this commit or later. See the [llama.cpp repository](https://github.com/ggerganov/llama.cpp) for the latest implementation.
2. **API compatibility**: The `IApiClient.SendStructuredPromptAsync` method must be available

### Migration Steps

#### Step 1: Update BrainAgent Calls

**Before (Regex Parsing):**
```csharp
var agent = new BrainAgent(profile, apiClient, memoryStore);
var response = await agent.SendMessageAsync("Hello!");

// Manual parsing
var parser = new OutputParser();
var parsed = parser.Parse(response);
```

**After (Structured Output):**
```csharp
var agent = new BrainAgent(profile, apiClient, memoryStore);

// Direct structured output
var parsed = await agent.SendNativeDialogueAsync("Hello!");

// Or use the full pipeline
var pipeline = new StructuredDialoguePipeline(agent, validationGate);
var result = await pipeline.ProcessDialogueAsync("Hello!");
```

#### Step 2: Update Validation Integration

**Before:**
```csharp
var parser = new OutputParser();
var parsed = parser.Parse(rawResponse);
var gateResult = validationGate.Validate(parsed, context);

// Manual mutation execution
if (gateResult.Passed)
{
    foreach (var mutation in gateResult.ApprovedMutations)
    {
        // Execute mutation...
    }
}
```

**After:**
```csharp
// Pipeline handles everything
var result = await pipeline.ProcessDialogueAsync("Hello!");

// Access results
if (result.Success)
{
    var gateResult = result.GateResult;
    var mutationResult = result.MutationResult;
}
```

#### Step 3: Gradual Migration with Fallback

For safety, use the fallback configuration during migration:

```csharp
var config = StructuredPipelineConfig.Default; // Uses fallback
config.TrackMetrics = true;

var pipeline = new StructuredDialoguePipeline(agent, validationGate, null, null, config);

// Process requests and monitor metrics
var result = await pipeline.ProcessDialogueAsync("Hello!");

// Check how often fallback is used
var fallbackRate = pipeline.Metrics.FallbackRate;
Console.WriteLine($"Fallback Rate: {fallbackRate:F1}%");
```

#### Step 4: Disable Fallback When Stable

Once you confirm structured output is working reliably:

```csharp
var config = StructuredPipelineConfig.StructuredOnly;
var pipeline = new StructuredDialoguePipeline(agent, validationGate, null, null, config);
```

### Schema Validation

Enable schema validation to filter invalid mutations/intents before execution:

```csharp
var config = StructuredPipelineConfig.Default;
config.ValidateMutationSchemas = true;
config.ValidateIntentSchemas = true;

// Invalid mutations are filtered out and logged
// e.g., TransformBelief without a target, empty content, etc.
```

### Prompt Considerations

Structured output doesn't require special prompt formatting. The JSON schema is sent to the LLM as a constraint, not embedded in the prompt.

**No changes needed** to your existing prompts. The LLM will automatically format its response to match the schema.

### Performance Comparison

Both structured and regex parsing complete in sub-millisecond times:

| Parse Type | Simple Response | Complex Response |
|------------|-----------------|------------------|
| Structured | ~0.01ms | ~0.07ms |
| Regex | ~0.00ms | ~0.01ms |

The slight overhead of structured parsing is negligible and offset by:
- Type safety
- Elimination of regex edge cases
- Automatic schema validation

---

<a id="debugging--monitoring"></a>
## Debugging & Monitoring

### Accessing Debug Information

When using the full pipeline, you can access debug information from each component:

```csharp
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Validation;

// After building snapshot
var snapshot = new StateSnapshotBuilder()
    .WithContext(context)
    .WithConstraints(constraints)
    .Apply(retrievedContext)
    .Build();

Console.WriteLine($"Attempt: {snapshot.AttemptNumber}/{snapshot.MaxAttempts}");
Console.WriteLine($"Constraints: {snapshot.Constraints.Count}");

// After assembling prompt
var assembler = new PromptAssembler();
var assembledPrompt = assembler.AssembleFromSnapshot(snapshot);

Console.WriteLine($"Prompt length: {assembledPrompt.PromptText.Length} chars");
Console.WriteLine($"Estimated tokens: {assembledPrompt.EstimatedTokens}");
Console.WriteLine($"Truncated: {assembledPrompt.WasTruncated}");

// Prompt breakdown
var breakdown = assembledPrompt.Breakdown;
Console.WriteLine($"  System: {breakdown.SystemPrompt} chars");
Console.WriteLine($"  Few-shot: {breakdown.FewShotExamples} chars");
Console.WriteLine($"  Memory: {breakdown.MemoryContext} chars");
Console.WriteLine($"  Dialogue: {breakdown.DialogueHistory} chars");

// After validation
var gateResult = validationGate.Validate(parsedOutput, validationContext);
if (!gateResult.Passed)
{
    foreach (var failure in gateResult.Failures)
    {
        Console.WriteLine($"Validation failed: {failure.Reason} - {failure.Description}");
    }
}

// After mutation execution
var mutationResult = mutationController.ExecuteMutations(gateResult, memorySystem);
Console.WriteLine($"Mutations: {mutationResult.SuccessCount}/{mutationResult.TotalAttempted}");
```

### Logging Configuration

```csharp
// Configure logging callbacks for expectancy evaluator
var evaluator = new ExpectancyEvaluator();
evaluator.SetLoggingCallback((level, message) =>
{
    Console.WriteLine($"[{level}] {message}");
});

// Log validation events
validationGate.OnValidationComplete += (result) =>
    Console.WriteLine($"Validation: {(result.Passed ? "PASSED" : "FAILED")}");

// Log mutation events
mutationController.OnWorldIntentEmitted += (intent) =>
    Console.WriteLine($"Intent emitted: {intent.IntentType} from {intent.SourceNpcId}");
```

---

<a id="performance-optimization"></a>
## Performance Optimization

### Prompt Size Management

```csharp
// Configure working memory bounds
var config = WorkingMemoryConfig.Minimal; // Start with minimal
config.MaxExchanges = 3;        // Limit dialogue history
config.MaxMemories = 5;         // Limit episodic memories
config.MaxBeliefs = 3;          // Limit beliefs
config.MaxCharacters = 1500;    // Hard character limit
config.MaxFewShotExamples = 2;  // Limit examples

// Use smaller prompt assembler config
var assemblerConfig = PromptAssemblerConfig.SmallContext;
```

### Caching Strategies

```csharp
// Enable prompt caching (if supported by server)
var result = await agent.SendWithSnapshotAsync(
    playerInput,
    cachePrompt: true
);

// Reuse memory systems
var memoryStore = new PersonaMemoryStore();
// Store reference, don't recreate per-interaction
```

### Async Best Practices

```csharp
using System.Threading;
using System.Threading.Tasks;

// Standard async/await (works in any .NET environment)
async Task ProcessInteraction()
{
    var result = await agent.SendMessageAsync("Hello");
    ProcessResult(result);
}

// Cancel long-running requests
var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(30));

try
{
    var result = await agent.SendMessageAsync("Hello", cts.Token);
    Console.WriteLine($"Response: {result}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Request timed out");
}
```

### Retry Policy Tuning

```csharp
// Balance reliability vs latency
var retryPolicy = new RetryPolicy
{
    MaxRetries = 1,              // Reduce for faster responses
    TimeLimitSeconds = 15.0,     // Total time budget
    ConstraintEscalation = ConstraintEscalation.AddSpecificProhibition
};
```

---

## Further Reading

- [README.md](../LlamaBrain/README.md) - Main library documentation and overview
- [ARCHITECTURE.md](ARCHITECTURE.md) - Full architectural pattern explanation
- [MEMORY.md](MEMORY.md) - Comprehensive memory system documentation
- [PIPELINE_CONTRACT.md](PIPELINE_CONTRACT.md) - Formal pipeline contract specification
- [VALIDATION_GATING.md](VALIDATION_GATING.md) - Validation gating system documentation
- [ROADMAP.md](ROADMAP.md) - Implementation status and future plans
- [STATUS.md](STATUS.md) - Current implementation status
- [DETERMINISM_CONTRACT.md](DETERMINISM_CONTRACT.md) - Determinism contract and boundaries

---

**Version**: 0.2.0-rc.1
