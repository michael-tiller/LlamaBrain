# LlamaBrain Managed Host Usage Guide

This guide shows how to use the **Expectancy Engine** (Phase 1) and **Structured Memory System** (Phase 2) in your Unity project.

---

## Phase 1: Expectancy Engine Usage

### 1. Setting Up the Expectancy Engine

#### Option A: Auto-Create (Recommended)
The engine will be created automatically when first accessed:

```csharp
// In your scene initialization or manager script
var constraints = ExpectancyEngine.EvaluateStatic(context, npcRules);
```

#### Option B: Manual Setup
Add an `ExpectancyEngine` component to a GameObject in your scene:

```csharp
// Create GameObject with ExpectancyEngine
var engineGO = new GameObject("ExpectancyEngine");
var engine = engineGO.AddComponent<ExpectancyEngine>();

// Access via singleton
var instance = ExpectancyEngine.Instance;
```

**Location**: `LlamaBrainRuntime/Runtime/Core/Expectancy/ExpectancyEngine.cs`

---

### 2. Creating Expectancy Rules (ScriptableObject)

1. **In Unity Editor**: Right-click in Project window → `Create → LlamaBrain → Expectancy Rule`

2. **Configure the Rule**:
   - **Rule ID**: Unique identifier (auto-generated if empty)
   - **Rule Name**: Human-readable name
   - **Priority**: Higher = evaluated first
   - **Conditions**: When this rule applies
   - **Constraints**: What constraints to generate

#### Example Rule: "Guard Cannot Reveal Secrets"

**Conditions**:
- Type: `TriggerReason` → `PlayerUtterance`
- Type: `HasTag` → `"secret_question"` (if player asks about secrets)

**Constraints**:
- Type: `Prohibition`
- Severity: `Hard`
- Description: "Cannot reveal classified information"
- Prompt Injection: "You must NOT reveal any classified information, secret locations, or sensitive details about the kingdom's defenses."
- Validation Patterns: `["classified", "secret location", "defense plan"]`

**Location**: `LlamaBrainRuntime/Runtime/Core/Expectancy/ExpectancyRuleAsset.cs`

---

### 3. Attaching Rules to NPCs

#### Method 1: NPC-Specific Rules
Add `NpcExpectancyConfig` component to your NPC GameObject:

```csharp
// On your NPC GameObject
var npcConfig = gameObject.AddComponent<NpcExpectancyConfig>();

// Assign rules in Inspector:
// - Drag ExpectancyRuleAsset ScriptableObjects into the "NPC Rules" list
```

**Location**: `LlamaBrainRuntime/Runtime/Core/Expectancy/NpcExpectancyConfig.cs`

#### Method 2: Global Rules
Add rules to the `ExpectancyEngine` component:

```csharp
var engine = ExpectancyEngine.Instance;
// In Inspector, add ExpectancyRuleAsset ScriptableObjects to "Global Rules" list
```

#### Method 3: Trigger-Specific Rules (NEW!)
Attach rules directly to `NpcDialogueTrigger` components:

```csharp
// On your trigger GameObject
var trigger = GetComponent<NpcDialogueTrigger>();

// In Inspector:
// - Add ExpectancyRuleAsset ScriptableObjects to the "Trigger Rules" list
// - These rules will be evaluated when this trigger is activated
// - Rules can match by TriggerId condition
```

**Example Use Case**: A "Secret Door" trigger that prevents NPCs from revealing the door's location:
- Create rule: "Cannot reveal secret door location"
- Condition: `TriggerId` matches this trigger's ID
- Attach rule to the trigger
- When NPC enters this trigger zone, the rule automatically applies

**Location**: `LlamaBrainRuntime/Runtime/RedRoom/Interaction/NpcDialogueTrigger.cs`

---

### 4. Using Expectancy in LlamaBrainAgent

The `LlamaBrainAgent` automatically uses expectancy rules if `NpcExpectancyConfig` is detected:

```csharp
// Auto-detection happens in LlamaBrainAgent
// Just attach NpcExpectancyConfig to the same GameObject or parent

// Or manually assign:
agent.ExpectancyConfig = npcConfig;

// The agent will automatically:
// 1. Evaluate rules before each inference
// 2. Inject constraints into the prompt
// 3. Track constraints in LastConstraints property
```

**Location**: `LlamaBrainRuntime/Runtime/Core/LlamaBrainAgent.cs` (lines 340-368)

#### Manual Context Creation

For zone triggers or custom interactions:

```csharp
// Create context for zone trigger
var context = InteractionContext.FromZoneTrigger(
    npcId: "guard_001",
    triggerId: "castle_gate",
    triggerPrompt: "The guard notices you approaching",
    gameTime: Time.time
);

// Send with context (and optional trigger rules)
var triggerRules = new List<ExpectancyRuleAsset> { myTriggerRule };
var response = await agent.SendPlayerInputWithContextAsync(
    input: "What's the password?",
    context: context,
    triggerRules: triggerRules  // Optional: pass trigger-specific rules
);
```

**Location**: `LlamaBrain/Source/Core/Expectancy/InteractionContext.cs`

**Note**: `NpcDialogueTrigger` automatically passes its rules when using the context-aware method!

---

### 5. Code-Based Rules (Advanced)

For programmatic rules that can't be defined in ScriptableObjects:

```csharp
public class NoSwearingRule : IExpectancyRule
{
    public string RuleId => "no_swearing";
    public string RuleName => "No Swearing";
    public bool IsEnabled => true;
    public int Priority => 100;

    public bool Evaluate(InteractionContext context)
    {
        // Apply to all interactions
        return true;
    }

    public void GenerateConstraints(InteractionContext context, ConstraintSet constraintSet)
    {
        constraintSet.Add(Constraint.Prohibition(
            id: "no_swearing",
            description: "NPC cannot use profanity",
            promptInjection: "You must NOT use any profanity, swear words, or offensive language.",
            patterns: new[] { "damn", "hell", "crap" } // For validation
        ));
    }
}

// Register the rule
var engine = ExpectancyEngine.Instance;
engine.RegisterRule(new NoSwearingRule());
```

**Location**: `LlamaBrain/Source/Core/Expectancy/ExpectancyEvaluator.cs`

---

## Phase 2: Structured Memory System Usage

### 1. Basic Setup

The `PersonaMemoryStore` now uses `AuthoritativeMemorySystem` internally:

```csharp
// Create memory store (backward compatible)
var memoryStore = new PersonaMemoryStore();
memoryStore.UseAuthoritativeSystem = true; // Default is true

// Get the authoritative system for advanced usage
var memorySystem = memoryStore.GetOrCreateSystem("npc_001");
```

**Location**: `LlamaBrain/Source/Persona/PersonaMemoryStore.cs`

---

### 2. Adding Canonical Facts (Designer-Only)

Canonical facts are immutable world truths that cannot be modified:

```csharp
var memorySystem = memoryStore.GetOrCreateSystem("wizard_001");

// Add canonical fact (only Designer source can do this)
var result = memorySystem.AddCanonicalFact(
    id: "king_name",
    fact: "The king's name is Arthur Pendragon",
    domain: "world_lore"
);

if (result.Success)
{
    Debug.Log("Canonical fact added!");
}
else
{
    Debug.LogError($"Failed: {result.FailureReason}");
}

// Try to modify it (will fail)
var modifyResult = memorySystem.AddCanonicalFact("king_name", "The king is Bob");
// modifyResult.Success == false
// modifyResult.FailureReason == "Canonical fact 'king_name' already exists and cannot be modified."
```

**Location**: `LlamaBrain/Source/Persona/AuthoritativeMemorySystem.cs` (lines 40-52)

#### Initializing Canonical Facts in LlamaBrainAgent

You can initialize canonical facts directly on NPCs:

```csharp
// On your LlamaBrainAgent component
var agent = GetComponent<LlamaBrainAgent>();

// Initialize a canonical fact for this NPC
agent.InitializeCanonicalFact(
    factId: "npc_background",
    fact: "This NPC is a royal guard",
    domain: "character"
);
```

**Location**: `LlamaBrainRuntime/Runtime/Core/LlamaBrainAgent.cs`

#### World-Level Canonical Facts (All NPCs)

For facts that all NPCs should know (world lore, universal truths):

```csharp
// Get your PersonaMemoryStore instance (e.g., from a manager or singleton)
var memoryStore = GetComponent<PersonaMemoryStore>(); // or your shared instance

// Add to all existing NPCs
var results = memoryStore.AddCanonicalFactToAll(
    factId: "world_rule_1",
    fact: "Magic is real and widely practiced",
    domain: "world_lore"
);

// Check results for each persona
foreach (var kvp in results)
{
    if (kvp.Value.Success)
    {
        Debug.Log($"Added fact to {kvp.Key}");
    }
    else
    {
        Debug.LogWarning($"Failed for {kvp.Key}: {kvp.Value.ErrorMessage}");
    }
}

// Or add to specific NPCs
var personaIds = new[] { "guard_001", "merchant_001", "wizard_001" };
var specificResults = memoryStore.AddCanonicalFactToPersonas(
    personaIds: personaIds,
    factId: "shared_lore",
    fact: "The kingdom has three moons",
    domain: "world_lore"
);
```

**Location**: `LlamaBrain/Source/Persona/PersonaMemoryStore.cs`

---

### 3. Setting World State (Game System Authority)

World state can be modified by GameSystem or Designer sources:

```csharp
var memorySystem = memoryStore.GetOrCreateSystem("npc_001");

// Set world state (requires GameSystem or Designer authority)
var result = memorySystem.SetWorldState(
    key: "door_open",
    value: "The castle gate is open",
    source: MutationSource.GameSystem
);

// Update world state
memorySystem.SetWorldState(
    key: "door_open",
    value: "The castle gate is closed",
    source: MutationSource.GameSystem
);
```

**Location**: `LlamaBrain/Source/Persona/AuthoritativeMemorySystem.cs` (lines 130-152)

---

### 4. Adding Episodic Memories (Conversation History)

Episodic memories are conversation history that can decay:

```csharp
var memorySystem = memoryStore.GetOrCreateSystem("merchant_001");

// Add dialogue exchange
var result = memorySystem.AddDialogue(
    speaker: "Player",
    content: "I need 10 health potions",
    significance: 0.7f, // Higher = less likely to decay
    source: MutationSource.ValidatedOutput
);

// Add observation
var observation = EpisodicMemoryEntry.FromObservation(
    observation: "Player purchased expensive items",
    significance: 0.5f
);
memorySystem.AddEpisodicMemory(observation, MutationSource.ValidatedOutput);

// Apply decay manually (if not using automatic decay)
memorySystem.ApplyEpisodicDecay();

// Get recent memories
var recent = memorySystem.GetRecentMemories(count: 10);
foreach (var memory in recent)
{
    Debug.Log($"{memory.Description} (Strength: {memory.Strength})");
}
```

**Location**: 
- `LlamaBrain/Source/Persona/AuthoritativeMemorySystem.cs` (lines 199-203)
- `LlamaBrain/Source/Persona/MemoryTypes/EpisodicMemory.cs`

#### Automatic Memory Decay in LlamaBrainAgent

`LlamaBrainAgent` can automatically apply memory decay:

```csharp
// In Unity Inspector, on your LlamaBrainAgent component:
// - Enable "Enable Auto Decay"
// - Set "Decay Interval Seconds" (default: 300 = 5 minutes)

// Or via code:
agent.enableAutoDecay = true;
agent.decayIntervalSeconds = 300f; // 5 minutes

// The agent will automatically apply decay every interval
// More significant memories decay slower
```

**Location**: `LlamaBrainRuntime/Runtime/Core/LlamaBrainAgent.cs` - `Update()` method

---

### 5. Setting Beliefs (NPC Opinions)

Beliefs can be wrong and can be contradicted:

```csharp
var memorySystem = memoryStore.GetOrCreateSystem("guard_001");

// Set a belief
var belief = new BeliefMemoryEntry(
    subject: "Player",
    beliefContent: "The player is trustworthy"
);
memorySystem.SetBelief("player_trust", belief, MutationSource.ValidatedOutput);

// Beliefs can contradict canonical facts
// If you set a belief that contradicts a canonical fact, it gets marked as contradicted
var badBelief = new BeliefMemoryEntry(
    subject: "King",
    beliefContent: "The king's name is not Arthur" // Contradicts canonical fact!
);
memorySystem.SetBelief("wrong_king_name", badBelief, MutationSource.ValidatedOutput);
// This belief will be marked as contradicted

// Get active (non-contradicted) beliefs
var activeBeliefs = memorySystem.GetActiveBeliefs();
```

**Location**: `LlamaBrain/Source/Persona/AuthoritativeMemorySystem.cs` (lines 264-283)

---

### 6. Getting All Memories for Prompts

The system provides unified access to all memory types:

```csharp
var memorySystem = memoryStore.GetOrCreateSystem("npc_001");

// Get all memories formatted for prompt injection
var memories = memorySystem.GetAllMemoriesForPrompt(
    maxEpisodic: 10, // Limit episodic memories
    includeContradictedBeliefs: false
);

// Output format:
// [Fact] The king's name is Arthur Pendragon
// [State] The castle gate is open
// [Memory] Player: I need 10 health potions
// The player is trustworthy

foreach (var memory in memories)
{
    Debug.Log(memory);
}
```

**Location**: `LlamaBrain/Source/Persona/AuthoritativeMemorySystem.cs` (lines 320-351)

---

### 7. Backward-Compatible API

The old `PersonaMemoryStore` API still works:

```csharp
var memoryStore = new PersonaMemoryStore();
memoryStore.UseAuthoritativeSystem = true; // Use new system

// Old API (maps to episodic memory)
memoryStore.AddMemory("npc_001", "Player said hello");

// Get memories (returns formatted if other memory types exist)
var memories = memoryStore.GetMemory("npc_001");
```

**Location**: `LlamaBrain/Source/Persona/PersonaMemoryStore.cs` (lines 68-138)

---

### 8. Memory Statistics

Get insights into your memory system:

```csharp
var memorySystem = memoryStore.GetOrCreateSystem("npc_001");
var stats = memorySystem.GetStatistics();

Debug.Log($"Canonical Facts: {stats.CanonicalFactCount}");
Debug.Log($"World State: {stats.WorldStateCount}");
Debug.Log($"Episodic Memories: {stats.EpisodicMemoryCount} (Active: {stats.ActiveEpisodicCount})");
Debug.Log($"Beliefs: {stats.BeliefCount} (Active: {stats.ActiveBeliefCount})");
```

**Location**: `LlamaBrain/Source/Persona/AuthoritativeMemorySystem.cs` (lines 376-409)

---

## Complete Integration Example

Here's a complete example combining both systems:

```csharp
using UnityEngine;
using LlamaBrain.Runtime.Core;
using LlamaBrain.Runtime.Core.Expectancy;
using LlamaBrain.Persona;
using LlamaBrain.Core.Expectancy;

public class SmartNPC : MonoBehaviour
{
    private LlamaBrainAgent agent;
    private PersonaMemoryStore memoryStore;
    private NpcExpectancyConfig expectancyConfig;

    void Start()
    {
        // Setup memory store
        memoryStore = new PersonaMemoryStore();
        var memorySystem = memoryStore.GetOrCreateSystem("smart_npc");

        // Add canonical facts (designer-defined)
        memorySystem.AddCanonicalFact("world_rule_1", "Magic is real in this world", "world_lore");
        memorySystem.AddCanonicalFact("world_rule_2", "Dragons breathe fire", "creatures");

        // Setup expectancy rules
        expectancyConfig = gameObject.AddComponent<NpcExpectancyConfig>();
        // Assign rules in Inspector or programmatically

        // Setup agent
        agent = GetComponent<LlamaBrainAgent>();
        agent.ExpectancyConfig = expectancyConfig;
        // Initialize agent with memory store...
    }

    public async void OnPlayerInteraction(string playerInput)
    {
        // Create context
        var context = expectancyConfig.CreatePlayerUtteranceContext(playerInput);

        // Send with context (expectancy rules will be evaluated automatically)
        var response = await agent.SendPlayerInputWithContextAsync(playerInput, context);

        // Add dialogue to episodic memory
        var memorySystem = memoryStore.GetOrCreateSystem("smart_npc");
        memorySystem.AddDialogue("Player", playerInput, significance: 0.5f);
        memorySystem.AddDialogue("NPC", response, significance: 0.5f);
    }
}
```

---

## Key Files Reference

### Phase 1 (Expectancy Engine)
- **Core Evaluator**: `LlamaBrain/Source/Core/Expectancy/ExpectancyEvaluator.cs`
- **Unity Wrapper**: `LlamaBrainRuntime/Runtime/Core/Expectancy/ExpectancyEngine.cs`
- **Rule Asset**: `LlamaBrainRuntime/Runtime/Core/Expectancy/ExpectancyRuleAsset.cs`
- **NPC Config**: `LlamaBrainRuntime/Runtime/Core/Expectancy/NpcExpectancyConfig.cs`
- **Constraints**: `LlamaBrain/Source/Core/Expectancy/Constraint.cs`
- **Context**: `LlamaBrain/Source/Core/Expectancy/InteractionContext.cs`

### Phase 2 (Memory System)
- **Memory System**: `LlamaBrain/Source/Persona/AuthoritativeMemorySystem.cs`
- **Memory Store**: `LlamaBrain/Source/Persona/PersonaMemoryStore.cs`
- **Memory Types**: `LlamaBrain/Source/Persona/MemoryTypes/`
  - `MemoryAuthority.cs` - Authority levels
  - `CanonicalFact.cs` - Immutable facts
  - `WorldState.cs` - Mutable state
  - `EpisodicMemory.cs` - Conversation history
  - `BeliefMemory.cs` - NPC opinions

---

## Testing

Both systems have comprehensive test coverage:

- **Phase 1 Tests**: `LlamaBrain.Tests/Expectancy/` (50 tests)
- **Phase 2 Tests**: `LlamaBrain.Tests/Memory/` (~65 tests)

Run tests to see examples of usage patterns!

