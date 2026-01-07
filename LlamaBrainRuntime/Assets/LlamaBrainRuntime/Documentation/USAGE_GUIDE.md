# LlamaBrain Unity Integration Guide

This guide shows how to use all nine components of the LlamaBrain architecture in your Unity project. The system ensures deterministic, controlled AI behavior while maintaining the flexibility and creativity of Large Language Models.

**Last Updated**: January 6, 2026

---

## Quick Start

Get up and running with LlamaBrain in 5 minutes:

### 1. Basic NPC Setup (Components 1, 2, 6, 7, 9)

**Minimal Setup**:
1. Create a GameObject for your NPC
2. Add `LlamaBrainAgent` component
3. Assign a `PersonaConfig` (or create one: `Create → LlamaBrain → Persona Config`)
4. Configure `BrainSettings` with your llama.cpp server details
5. Call `agent.SendPlayerInputAsync("Hello!")` to start a conversation

**That's it!** The agent handles:
- **Component 1**: Captures interaction context automatically
- **Component 2**: Evaluates expectancy rules (if configured)
- **Component 6**: Sends prompt to LLM server
- **Component 7**: Validates output automatically
- **Component 9**: Uses fallback if validation fails

### 2. Add Memory System (Component 3)

**Enable Persistent Memory**:
1. The `LlamaBrainAgent` automatically creates a `PersonaMemoryStore`
2. Initialize canonical facts (immutable world truths):
   ```csharp
   agent.InitializeCanonicalFact("king_name", "The king is named Arthur", "world_lore");
   ```
3. Memory is automatically used in prompts - no additional setup needed

### 3. Add Validation Rules (Component 7)

**World-Level Rules** (apply to all NPCs):
1. Create a GameObject named "ValidationManager"
2. Add `ValidationPipeline` component
3. Create validation rules: `Create → LlamaBrain → Validation Rule`
4. Create a rule set: `Create → LlamaBrain → Validation Rule Set`
5. Assign rule set to `ValidationPipeline`'s "Global Rules" list

**NPC-Specific Rules**:
1. Select your NPC GameObject
2. In `LlamaBrainAgent` component, find "Validation Rules" section
3. Assign a `ValidationRuleSetAsset` to "NPC Validation Rules"

**Trigger-Specific Rules**:
1. Select your trigger GameObject (with `NpcDialogueTrigger`)
2. Assign a `ValidationRuleSetAsset` to "Trigger Validation Rules"

### 4. Add Expectancy Rules (Component 2)

**Control NPC Behavior**:
1. Create expectancy rules: `Create → LlamaBrain → Expectancy Rule`
2. Add `NpcExpectancyConfig` component to NPC
3. Assign rules to "NPC Rules" list
4. Rules generate constraints that control both prompts and validation

### 5. Complete Example

```csharp
using UnityEngine;
using LlamaBrain.Runtime.Core;

public class SimpleNPC : MonoBehaviour
{
    private LlamaBrainAgent agent;
    
    void Start()
    {
        agent = GetComponent<LlamaBrainAgent>();
        
        // Initialize canonical fact (Component 3)
        agent.InitializeCanonicalFact("world_rule", "Magic is real", "lore");
    }
    
    public async void TalkToPlayer(string playerInput)
    {
        // Components 1-9 all work together automatically:
        // 1. Interaction context created
        // 2. Expectancy rules evaluated (if configured)
        // 3. Memory retrieved
        // 4. State snapshot built
        // 5. Prompt assembled
        // 6. LLM generates response
        // 7. Output validated
        // 8. Memory mutated (if validated)
        // 9. Fallback used (if validation fails)
        var response = await agent.SendPlayerInputAsync(playerInput);
        Debug.Log($"NPC says: {response}");
    }
}
```

### Required Global Singletons

You need to place these singleton components in your scene for full functionality:

#### 1. ValidationPipeline (Component 7) - **Required for Global Validation Rules**

**Purpose**: Applies global validation rules to all NPCs

**Setup**:
1. Create GameObject named "ValidationManager" (or any name)
2. Add `ValidationPipeline` component
3. Assign `ValidationRuleSetAsset` instances to "Global Rules" list
4. Configure "Forbidden Knowledge" list (optional)

**When Required**: 
- ✅ **Required** if you want global validation rules (e.g., "no swearing", "no spoilers")
- ❌ **Optional** if you only use NPC-specific or trigger-specific rules

**Location**: `LlamaBrainRuntime/Runtime/Core/Validation/ValidationPipeline.cs`

#### 2. WorldIntentDispatcher (Component 8) - **Required for World Intents**

**Purpose**: Dispatches world intents from NPCs to game systems

**Setup**:
1. Create GameObject named "WorldIntentManager" (or any name)
2. Add `WorldIntentDispatcher` component
3. Configure intent handlers in Inspector (optional - can also register via code)

**When Required**:
- ✅ **Required** if NPCs emit world intents (e.g., "follow_player", "give_item", "start_quest")
- ❌ **Optional** if NPCs don't need to affect game world

**Location**: `LlamaBrainRuntime/Runtime/Core/WorldIntentDispatcher.cs`

#### 3. ExpectancyEngine (Component 2) - **Optional (Auto-Creates)**

**Purpose**: Evaluates global expectancy rules for behavior constraints

**Setup**:
1. Create GameObject named "ExpectancyEngine" (or any name)
2. Add `ExpectancyEngine` component
3. Assign global `ExpectancyRuleAsset` instances (optional)

**When Required**:
- ⚠️ **Optional** - Can auto-create if not present, but better to have in scene for global rules
- ✅ **Recommended** if you have global expectancy rules that apply to all NPCs

**Location**: `LlamaBrainRuntime/Runtime/Core/Expectancy/ExpectancyEngine.cs`

#### RedRoom Singletons (Testing Only)

These are only needed for RedRoom testing framework:

- **DialogueMetricsCollector**: Collects metrics for testing (RedRoom only)
- **RedRoomCanvas**: UI management for RedRoom (RedRoom only)

### Component Overview

| Component | Name | What It Does | Unity Integration | Singleton Required? |
|-----------|------|--------------|-------------------|---------------------|
| 1 | Interaction Context | Captures trigger information | Automatic via `LlamaBrainAgent` | ❌ No |
| 2 | Determinism Layer | Generates behavior constraints | `ExpectancyEngine`, `NpcExpectancyConfig` | ⚠️ Optional |
| 3 | Authoritative Memory | Stores world facts & NPC memories | `PersonaMemoryStore`, `AuthoritativeMemorySystem` | ❌ No |
| 4 | State Snapshot | Immutable context for retries | `StateSnapshotBuilder` (automatic) | ❌ No |
| 5 | Ephemeral Working Memory | Bounded prompt assembly | `PromptAssembler` (automatic) | ❌ No |
| 6 | Stateless Inference | LLM text generation | `ApiClient`, `BrainServer` | ❌ No |
| 7 | Output Validation | Validates LLM output | `ValidationPipeline`, `ValidationGate` | ✅ Yes (for global rules) |
| 8 | Memory Mutation | Executes validated changes | `MemoryMutationController` (automatic) | ✅ Yes (for world intents) |
| 9 | Fallback System | Safe responses on failure | `AuthorControlledFallback` (automatic) | ❌ No |

**Most components work automatically** - you only need to configure:
- **Component 2**: Expectancy rules (optional, for behavior control)
- **Component 3**: Memory initialization (optional, for persistent NPCs)
- **Component 7**: Validation rules (optional, for content filtering)

**Required Singletons Summary**:
- ✅ **ValidationPipeline** - Required if using global validation rules
- ✅ **WorldIntentDispatcher** - Required if NPCs emit world intents
- ⚠️ **ExpectancyEngine** - Optional (auto-creates, but recommended for global rules)

---

## Component 1: Interaction Context

**Purpose**: Capture and structure the trigger that initiates an AI interaction.

**Unity Integration**: Automatically handled by `LlamaBrainAgent` - you rarely need to create contexts manually.

### Automatic Context Creation

When you call `agent.SendPlayerInputAsync()`, the agent automatically creates an `InteractionContext`:

```csharp
// Automatic - context created internally
var response = await agent.SendPlayerInputAsync("Hello!");
```

### Manual Context Creation (Advanced)

For trigger zones or custom interactions, you can create contexts explicitly:

```csharp
// Create context for a zone trigger
var context = InteractionContext.FromZoneTrigger(
    npcId: "guard_001",
    triggerId: "castle_gate_zone",
    triggerPrompt: "Welcome to the castle",
    gameTime: Time.time
);
context.SceneName = "CastleCourtyard";

// Send with context
var response = await agent.SendPlayerInputWithContextAsync("What is this place?", context);
```

### Trigger Reasons

Contexts support different trigger types:
- `PlayerUtterance` - Player speaks to NPC
- `ZoneTrigger` - NPC enters a trigger zone
- `TimeTrigger` - Time-based event
- `QuestTrigger` - Quest state change
- `NpcInteraction` - NPC-to-NPC interaction
- `WorldEvent` - Game world event
- `Custom` - Custom trigger type

**Location**: `LlamaBrain/Source/Core/Expectancy/InteractionContext.cs`

---

## Component 2: Determinism Layer (Expectancy Engine)

### Setting Up the Expectancy Engine

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

### Creating Expectancy Rules (ScriptableObject)

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

### Attaching Rules to NPCs

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

#### Method 3: Trigger-Specific Rules
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

## Component 3: External Authoritative Memory System

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

### 7. Memory System Migration Guide

This guide helps you migrate from the old simple memory API to the new structured authoritative memory system.

#### Overview of Changes

**Old System (Pre-Component 3):**
- Single flat memory list
- All memories treated the same
- No authority boundaries
- No protection against AI hallucinations

**New System (Component 3 - Authoritative Memory System):**
- Four distinct memory types with authority hierarchy
- Canonical facts (immutable, designer-only)
- World state (mutable, game system authority)
- Episodic memory (conversation history with decay)
- Beliefs (NPC opinions, can be wrong)
- Authority enforcement prevents unauthorized modifications

#### Migration Strategy

**Option 1: Gradual Migration (Recommended)**
The old API still works and automatically maps to the new system:

```csharp
// Old code continues to work
var memoryStore = new PersonaMemoryStore();
memoryStore.UseAuthoritativeSystem = true; // Default is true

// Old API (maps to episodic memory)
memoryStore.AddMemory("npc_001", "Player said hello");

// Get memories (returns formatted if other memory types exist)
var memories = memoryStore.GetMemory("npc_001");
```

**Option 2: Full Migration to Structured API**
Migrate to the new structured API for better control:

```csharp
var memoryStore = new PersonaMemoryStore();
var memorySystem = memoryStore.GetOrCreateSystem("npc_001");

// OLD: Simple memory addition
// memoryStore.AddMemory("npc_001", "Player said hello");

// NEW: Structured episodic memory
var entry = new EpisodicMemoryEntry("Player said hello", EpisodeType.LearnedInfo);
memorySystem.AddEpisodicMemory(entry, MutationSource.ValidatedOutput);
```

#### API Mapping Reference

| Old API | New API | Notes |
|---------|---------|-------|
| `AddMemory(personaId, message)` | `AddEpisodicMemory(entry, source)` | Maps to episodic memory with default significance |
| `GetMemory(personaId)` | `GetAllMemoriesForPrompt()` | Returns all memory types formatted |
| N/A | `AddCanonicalFact(id, fact, domain)` | **NEW** - Immutable world truths |
| N/A | `SetWorldState(key, value, source)` | **NEW** - Mutable game state |
| N/A | `AddDialogue(speaker, content, significance)` | **NEW** - Conversation history |
| N/A | `SetBelief(id, entry, source)` | **NEW** - NPC opinions/relationships |

#### Step-by-Step Migration

**Step 1: Identify Memory Types in Your Code**

Review your existing code and categorize memories:

```csharp
// Example: Categorize your existing memories
// - World truths that never change → Canonical Facts
// - Game state that changes → World State  
// - Conversation history → Episodic Memory
// - NPC opinions/relationships → Beliefs
```

**Step 2: Migrate Canonical Facts**

```csharp
// OLD: No way to store immutable facts
// (Had to rely on system prompts or hardcoded values)

// NEW: Store immutable world truths
memoryStore.AddCanonicalFact("npc_001", "king_name", "The king is named Arthur", "world_lore");
memoryStore.AddCanonicalFact("npc_001", "magic_exists", "Magic is real in this world", "world_lore");

// These facts cannot be modified by AI outputs
```

**Step 3: Migrate World State**

```csharp
// OLD: Mixed with other memories
// memoryStore.AddMemory("npc_001", "Current weather: Stormy");

// NEW: Separate world state
memoryStore.SetWorldState("npc_001", "CurrentWeather", "Stormy", MutationSource.GameSystem);
memoryStore.SetWorldState("npc_001", "TimeOfDay", "Evening", MutationSource.GameSystem);

// World state can be updated by game systems
```

**Step 4: Migrate Conversation History**

```csharp
// OLD: Simple memory addition
// memoryStore.AddMemory("npc_001", "Player: Hello\nNPC: Hi there!");

// NEW: Structured dialogue with significance
memoryStore.AddDialogue("npc_001", "Player", "Hello", significance: 0.5f);
memoryStore.AddDialogue("npc_001", "NPC", "Hi there!", significance: 0.5f);

// Significance affects memory retention (higher = more likely to be remembered)
```

**Step 5: Migrate NPC Opinions/Relationships**

```csharp
// OLD: Stored as simple memories
// memoryStore.AddMemory("npc_001", "NPC thinks player is trustworthy");

// NEW: Structured beliefs with confidence
var belief = BeliefMemoryEntry.CreateOpinion("player", "is trustworthy", sentiment: 0.8f, confidence: 0.9f);
memoryStore.SetBelief("npc_001", "trust_player", belief);

// Beliefs can be wrong and contradicted
```

**Step 6: Update Memory Retrieval**

```csharp
// OLD: Get all memories as flat list
// var memories = memoryStore.GetMemory("npc_001");

// NEW: Get specific memory types or all formatted
var memorySystem = memoryStore.GetOrCreateSystem("npc_001");

// Get all memories formatted for prompts
var allMemories = memorySystem.GetAllMemoriesForPrompt(maxEpisodic: 20);

// Or get specific types
var canonicalFacts = memorySystem.GetCanonicalFacts();
var worldState = memorySystem.GetWorldState();
var episodicMemories = memorySystem.GetRecentMemories(maxCount: 10);
var beliefs = memorySystem.GetBeliefs(minConfidence: 0.5f);
```

#### Common Migration Scenarios

**Scenario 1: Initializing NPC Knowledge**

```csharp
// OLD
var memoryStore = new PersonaMemoryStore();
memoryStore.AddMemory("wizard", "The king is named Arthur");
memoryStore.AddMemory("wizard", "Magic exists in this world");

// NEW
var memoryStore = new PersonaMemoryStore();
memoryStore.AddCanonicalFact("wizard", "king_name", "The king is named Arthur", "world_lore");
memoryStore.AddCanonicalFact("wizard", "magic_exists", "Magic exists in this world", "world_lore");
```

**Scenario 2: Tracking Game State**

```csharp
// OLD
memoryStore.AddMemory("npc_001", $"Door status: {doorIsOpen}");
memoryStore.AddMemory("npc_001", $"Player gold: {playerGold}");

// NEW
memoryStore.SetWorldState("npc_001", "door_castle_main", doorIsOpen ? "open" : "closed", MutationSource.GameSystem);
memoryStore.SetWorldState("npc_001", "player_gold", playerGold.ToString(), MutationSource.GameSystem);
```

**Scenario 3: Conversation History**

```csharp
// OLD
memoryStore.AddMemory("npc_001", $"Player: {playerMessage}\nNPC: {npcResponse}");

// NEW
memoryStore.AddDialogue("npc_001", "Player", playerMessage, significance: 0.5f);
memoryStore.AddDialogue("npc_001", "NPC", npcResponse, significance: 0.5f);
```

**Scenario 4: NPC Relationships**

```csharp
// OLD
memoryStore.AddMemory("npc_001", "NPC likes the player");
memoryStore.AddMemory("npc_001", "NPC thinks player is a hero");

// NEW
var relationship = BeliefMemoryEntry.CreateOpinion("player", "is a friend", sentiment: 0.8f, confidence: 0.9f);
memoryStore.SetBelief("npc_001", "rel_player", relationship);

var opinion = BeliefMemoryEntry.CreateBelief("player", "is a hero", confidence: 0.7f);
memoryStore.SetBelief("npc_001", "opinion_hero", opinion);
```

#### Breaking Changes

**None!** The old API is fully backward compatible. However, for best results:

1. **Use structured API for new code** - Better control and type safety
2. **Migrate canonical facts** - Protect world truths from AI hallucinations
3. **Separate world state** - Clear distinction between mutable and immutable data
4. **Use significance for episodic memory** - Better memory retention control

#### Best Practices After Migration

1. **Initialize Canonical Facts Early**
   ```csharp
   // In your NPC initialization
   memoryStore.AddCanonicalFact("npc_001", "world_rule_1", "Magic is real", "world_lore");
   memoryStore.AddCanonicalFact("npc_001", "world_rule_2", "Dragons exist", "creatures");
   ```

2. **Update World State from Game Systems**
   ```csharp
   // When game state changes
   memoryStore.SetWorldState("npc_001", "quest_completed", "true", MutationSource.GameSystem);
   ```

3. **Use Significance for Important Memories**
   ```csharp
   // Important events get higher significance
   memoryStore.AddDialogue("npc_001", "Player", "I saved your life!", significance: 1.0f);
   
   // Casual conversation gets lower significance
   memoryStore.AddDialogue("npc_001", "Player", "Hello", significance: 0.3f);
   ```

4. **Track Belief Confidence**
   ```csharp
   // High confidence for direct observations
   var belief = BeliefMemoryEntry.CreateOpinion("player", "is skilled", sentiment: 0.9f, confidence: 0.95f);
   
   // Lower confidence for rumors
   var rumor = BeliefMemoryEntry.CreateBelief("player", "is a spy", confidence: 0.3f);
   ```

#### Memory Decay (New Feature)

The new system includes automatic memory decay:

```csharp
// Enable automatic decay in LlamaBrainAgent
agent.enableAutoDecay = true;
agent.decayIntervalSeconds = 300f; // Decay every 5 minutes

// Or manually trigger decay
memorySystem.ApplyDecay();
memoryStore.ApplyDecayAll(); // All personas
```

**Location**: `LlamaBrain/Source/Persona/PersonaMemoryStore.cs`

---

### 8. Memory Statistics

---

## Component 4: Authoritative State Snapshot

**Purpose**: Create an immutable snapshot of all context at inference time for deterministic retries.

**Unity Integration**: Automatically handled by `LlamaBrainAgent` - no manual configuration needed.

The state snapshot captures:
- Interaction context (Component 1)
- Constraints from expectancy engine (Component 2)
- Retrieved memories (Component 3)
- Dialogue history
- System prompt
- Attempt number for retry logic

**Accessing Snapshots** (for debugging):
```csharp
var agent = GetComponent<LlamaBrainAgent>();
var snapshot = agent.LastSnapshot;
if (snapshot != null)
{
    Debug.Log($"Snapshot ID: {snapshot.SnapshotId}");
    Debug.Log($"Attempt: {snapshot.AttemptNumber}/{snapshot.MaxAttempts}");
}
```

**Location**: `LlamaBrain/Source/Core/Inference/StateSnapshot.cs`

---

## Component 5: Ephemeral Working Memory

**Purpose**: Create a bounded, token-efficient prompt from the snapshot.

**Unity Integration**: Automatically handled by `LlamaBrainAgent` - configurable via `PromptAssemblerSettings`.

### Configuring Prompt Assembly

1. Create `PromptAssemblerSettings` asset: `Create → LlamaBrain → Prompt Assembler Settings`
2. Configure bounds:
   - `MaxPromptTokens`: Maximum tokens for prompt (default: 2048)
   - `ReserveResponseTokens`: Tokens reserved for response (default: 256)
   - `CharsPerToken`: Character-to-token ratio (default: 4.0)
3. Assign to `LlamaBrainAgent`'s "Prompt Assembly" section

### Few-Shot Prompt Priming

Few-shot examples guide the LLM's response style, format, and tone by providing example input-output pairs. Configure them via `WorkingMemoryConfig` when assembling prompts.

#### Basic Configuration

```csharp
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Expectancy;

// Create few-shot examples
var examples = new List<FewShotExample>
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
};

// Configure working memory with examples
var workingMemoryConfig = new WorkingMemoryConfig
{
    MaxFewShotExamples = 3,
    AlwaysIncludeFewShot = true,  // Include even when dialogue history exists
    FewShotExamples = examples
};

// Use when assembling prompts (via PromptAssembler)
var promptAssembler = new PromptAssembler();
var assembledPrompt = promptAssembler.AssembleFromSnapshot(
    snapshot,
    npcName: "Gundren",
    workingMemoryConfig: workingMemoryConfig
);
```

#### Unity Integration

In Unity, configure few-shot examples programmatically by creating a custom `WorkingMemoryConfig` and passing it to the prompt assembly process. Since `LlamaBrainAgent` uses `PromptAssemblerSettings`, you can extend it or configure examples at runtime:

```csharp
using UnityEngine;
using LlamaBrain.Runtime.Core;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Expectancy;
using System.Collections.Generic;

public class NPCWithFewShot : MonoBehaviour
{
    private LlamaBrainAgent agent;
    private List<FewShotExample> fewShotExamples;

    void Start()
    {
        agent = GetComponent<LlamaBrainAgent>();
        
        // Configure few-shot examples for this NPC
        fewShotExamples = new List<FewShotExample>
        {
            new FewShotExample(
                "Hello!",
                "Greetings, traveler. What brings you to my forge today?"
            ),
            new FewShotExample(
                "Can you repair my sword?",
                "Aye, I can take a look. What needs fixing?"
            ),
            new FewShotExample(
                "How much will it cost?",
                "Depends on the damage. Show me the blade and I'll give you a fair price."
            )
        };
    }

    // Note: LlamaBrainAgent uses PromptAssemblerSettings internally
    // For full control, you may need to extend the agent or configure
    // WorkingMemoryConfig through PromptAssemblerSettings if supported
}
```

#### Converting Fallbacks to Few-Shot Examples

Reuse your fallback responses as few-shot examples:

```csharp
using LlamaBrain.Core.Fallback;

// Your fallback responses
var fallbacks = new List<string>
{
    "Hmm, let me think on that.",
    "I'm not sure I understand.",
    "*scratches head* Run that by me again?"
};

// Convert to few-shot examples
var examples = FallbackToFewShotConverter.ConvertFallbacksToFewShot(
    fallbacks,
    maxExamples: 3,
    triggerReason: TriggerReason.PlayerUtterance
);

// Use in WorkingMemoryConfig
var workingMemoryConfig = new WorkingMemoryConfig
{
    FewShotExamples = examples,
    MaxFewShotExamples = 3
};
```

#### Configuration Options

- **`MaxFewShotExamples`**: Maximum number of examples to include (default: 3)
- **`AlwaysIncludeFewShot`**: If `true`, includes examples even when dialogue history exists. If `false`, examples are only included when dialogue history is empty (default: `false`)
- **`FewShotExamples`**: The list of example input-output pairs

**Best Practices**:
- Use 2-5 examples for best results
- Place most important examples last (recency bias)
- Match examples to your NPC's voice and tone
- Use examples to demonstrate expected response length and format

**Location**: 
- `LlamaBrain/Source/Core/Inference/EphemeralWorkingMemory.cs`
- `LlamaBrain/Source/Core/Inference/PromptAssembler.cs`

---

## Component 6: Stateless Inference Core

**Purpose**: Pure stateless text generation - the LLM has no memory, authority, or world access.

**Unity Integration**: Automatically handled by `LlamaBrainAgent` via `ApiClient` and `BrainServer`.

### Server Configuration

1. Configure `BrainSettings` asset with:
   - Server host and port
   - Model name
   - LLM parameters (temperature, top_p, etc.)
2. Assign to `LlamaBrainAgent`'s "Persona Configuration" section

### Server Management

Use `BrainServer` component to manage llama.cpp server lifecycle:

```csharp
var server = GetComponent<BrainServer>();
server.StartServer(); // Starts llama.cpp server
// ... use agent ...
server.StopServer(); // Stops server
```

**Location**: 
- `LlamaBrain/Source/Core/ApiClient.cs`
- `LlamaBrainRuntime/Runtime/Core/BrainServer.cs`

---

## Component 7: Output Parsing & Validation

The validation rules system allows you to control what NPCs can and cannot say through pattern-based validation. Rules can be assigned at three levels: **world/global**, **NPC-specific**, and **trigger-specific**.

### 1. Creating Validation Rules

#### Creating a Validation Rule Asset

1. **In Unity Editor**: Right-click in Project window → `Create → LlamaBrain → Validation Rule`

2. **Configure the Rule**:
   - **Rule ID**: Unique identifier (auto-generated from name if empty)
   - **Description**: Human-readable description
   - **Rule Type**: `Prohibition` (must NOT contain) or `Requirement` (must contain)
   - **Severity**: `Soft` (warning), `Hard` (retry), or `Critical` (immediate fallback)
   - **Pattern**: Regex pattern to match (e.g., `\bswear\b` for "swear" word)
   - **Case Insensitive**: Whether pattern matching is case-insensitive
   - **Additional Patterns**: Optional list of additional patterns (OR logic)
   - **Context Filters**: Optional filters for scene, NPC ID, or trigger reason

**Location**: `LlamaBrainRuntime/Runtime/Core/Validation/ValidationRuleAsset.cs`

#### Example Rule: "No Swearing"

**Configuration**:
- **Rule Type**: `Prohibition`
- **Severity**: `Hard`
- **Pattern**: `\b(swear|curse|profanity)\b`
- **Case Insensitive**: `true`
- **Description**: "NPCs should not use profanity"

#### Example Rule: "Must Greet Player"

**Configuration**:
- **Rule Type**: `Requirement`
- **Severity**: `Soft`
- **Pattern**: `\b(hello|hi|greetings|welcome)\b`
- **Case Insensitive**: `true`
- **Description**: "NPCs should greet the player"

### 2. Creating Validation Rule Sets

A **Validation Rule Set** is a collection of validation rules that can be assigned to different levels:

1. **In Unity Editor**: Right-click in Project window → `Create → LlamaBrain → Validation Rule Set`

2. **Add Rules**: Drag `ValidationRuleAsset` instances into the "Rules" list

3. **Enable/Disable**: Toggle the "Enabled" checkbox to enable or disable the entire set

**Location**: `LlamaBrainRuntime/Runtime/Core/Validation/ValidationRuleAsset.cs` (ValidationRuleSetAsset class

### 3. World-Level (Global) Validation Rules

Global rules apply to **all NPCs** in the scene. These are typically rules like "no swearing", "no spoilers", or "no breaking character".

#### Setting Up ValidationPipeline

1. **Create ValidationPipeline Component**:
   - Create a GameObject in your scene (e.g., "ValidationManager")
   - Add `ValidationPipeline` component

2. **Assign Global Rule Sets**:
   - In the Inspector, find the "Global Rules" section
   - Drag `ValidationRuleSetAsset` instances into the list
   - These rules will be applied to all NPCs automatically

3. **Configure Forbidden Knowledge** (Optional):
   - Add topics to the "Forbidden Knowledge" list
   - NPCs will be prevented from revealing knowledge about these topics

**Example Setup**:
```csharp
// ValidationPipeline is a singleton - access it via:
var pipeline = ValidationPipeline.Instance;

// Rules are automatically loaded and applied to all NPCs
// No code needed - just configure in Inspector
```

**Location**: `LlamaBrainRuntime/Runtime/Core/Validation/ValidationPipeline.cs`

### 4. NPC-Specific Validation Rules

NPC-specific rules apply only to a **specific NPC**. These are useful for character-specific constraints.

#### Assigning Rules to an NPC

1. **Select the NPC GameObject** with `LlamaBrainAgent` component

2. **In Inspector**, find the "Validation Rules" section

3. **Assign NPC Validation Rules**:
   - Drag a `ValidationRuleSetAsset` into the "NPC Validation Rules" field
   - These rules are loaded when the agent initializes

**Example**: A guard NPC might have rules preventing them from revealing secrets:
- Create `ValidationRuleSetAsset` named "GuardNoSecrets"
- Add rules like "Cannot mention classified information"
- Assign to the guard's `LlamaBrainAgent` component

**Location**: `LlamaBrainRuntime/Runtime/Core/LlamaBrainAgent.cs` (npcValidationRules field)

### 5. Trigger-Specific Validation Rules

Trigger-specific rules apply only when an NPC enters a **specific trigger zone**. These are useful for location-based or context-based constraints.

#### Assigning Rules to a Trigger

1. **Select the Trigger GameObject** with `NpcDialogueTrigger` component

2. **In Inspector**, find the "Validation Rules" section

3. **Assign Trigger Validation Rules**:
   - Drag a `ValidationRuleSetAsset` into the "Trigger Validation Rules" field
   - These rules are loaded when the trigger activates
   - Rules are automatically cleared after the interaction completes

**Example**: A "No Magic Zone" trigger:
- Create `ValidationRuleSetAsset` named "NoMagicZone"
- Add rule: "Cannot mention magic" (Prohibition, Hard severity)
- Assign to the trigger's `NpcDialogueTrigger` component
- When NPCs enter this zone, they cannot mention magic

**Location**: `LlamaBrainRuntime/Runtime/RedRoom/Interaction/NpcDialogueTrigger.cs` (triggerValidationRules field)

### 6. Rule Priority and Execution Order

Validation rules are checked in this order:

1. **Global Rules** (from `ValidationPipeline`) - checked first
2. **NPC-Specific Rules** - checked second
3. **Trigger-Specific Rules** - checked third

**All three levels must pass** for validation to succeed. If any level fails, the validation fails and failures from all levels are combined in the result.

### 7. Context Filtering

Validation rules can be filtered by context to apply only in specific situations:

- **Scene Filter**: Only apply in specific scenes
- **NPC ID Filter**: Only apply to specific NPCs
- **Trigger Reason Filter**: Only apply for specific trigger reasons (PlayerUtterance, ZoneTrigger, etc.)

**Example**: A rule that only applies in the "Castle" scene:
- Set "Scene Filter" to `["Castle"]`
- Rule will only be checked when `InteractionContext.SceneName == "Castle"`

### 8. Accessing Validation Results

You can access validation results for debugging:

```csharp
// From LlamaBrainAgent
var agent = GetComponent<LlamaBrainAgent>();
var gateResult = agent.LastGateResult;

if (gateResult != null)
{
    if (gateResult.Passed)
    {
        Debug.Log("Validation passed!");
    }
    else
    {
        foreach (var failure in gateResult.Failures)
        {
            Debug.LogWarning($"[{failure.Severity}] {failure.Reason}: {failure.Description}");
        }
    }
}

// From ValidationPipeline
var pipeline = ValidationPipeline.Instance;
var lastResult = pipeline.LastGateResult;
var lastParsed = pipeline.LastParsedOutput;
```

### 9. Best Practices

1. **Use Global Rules for Universal Constraints**: Rules that apply to all NPCs (no swearing, no spoilers)
2. **Use NPC-Specific Rules for Character Constraints**: Rules specific to a character's role or personality
3. **Use Trigger-Specific Rules for Location Constraints**: Rules that apply only in specific areas or contexts
4. **Start with Soft Severity**: Use `Soft` severity for warnings, `Hard` for retries, `Critical` only for game-breaking issues
5. **Test Rules Thoroughly**: Validation rules can block valid responses - test with various inputs
6. **Use Context Filters**: Narrow rules to specific contexts to avoid false positives

### 10. Troubleshooting

**Problem**: Validation always fails
- **Solution**: Check rule patterns - they might be too broad or matching unintended text
- **Solution**: Check severity - `Critical` severity causes immediate fallback without retry

**Problem**: Rules not applying
- **Solution**: Verify `ValidationPipeline` exists in scene (for global rules)
- **Solution**: Check rule set is enabled
- **Solution**: Verify context filters match (scene, NPC ID, trigger reason)

**Problem**: NPC-specific rules not working
- **Solution**: Ensure rules are assigned to `LlamaBrainAgent` component
- **Solution**: Check agent is initialized (rules load during initialization)

**Problem**: Trigger rules not clearing
- **Solution**: Rules should clear automatically after interaction - check `ClearTriggerValidationRules()` is being called

---

## Component 8: Memory Mutation + World Effects

**Purpose**: Execute validated mutations and dispatch world intents, with strict authority enforcement.

**Unity Integration**: Automatically handled by `LlamaBrainAgent` - mutations execute after validation passes.

### Automatic Mutation Execution

When validation passes, mutations are automatically executed:
- **AppendEpisodic**: Conversation history added to episodic memory
- **TransformBelief**: NPC beliefs updated
- **TransformRelationship**: Relationships with entities updated
- **EmitWorldIntent**: World intents dispatched to game systems

### Accessing Mutation Results

```csharp
var agent = GetComponent<LlamaBrainAgent>();
var mutationResult = agent.LastMutationBatchResult;

if (mutationResult != null)
{
    Debug.Log($"Mutations: {mutationResult.SuccessCount}/{mutationResult.TotalAttempted} succeeded");
    foreach (var failure in mutationResult.Failures)
    {
        Debug.LogWarning($"Failed: {failure.ErrorMessage}");
    }
}
```

### World Intent System

NPCs can emit "intents" (desires) that game systems respond to:

1. **Setup WorldIntentDispatcher**:
   - Create GameObject named "WorldIntentManager"
   - Add `WorldIntentDispatcher` component
   - Configure intent handlers in Inspector

2. **Intents are automatically dispatched** when validation passes

**Example Intent Handler**:
```csharp
var dispatcher = WorldIntentDispatcher.Instance;
dispatcher.RegisterHandler("follow_player", (intent, npcId) => {
    var npc = FindNpc(npcId);
    npc.StartFollowing(intent.Target);
});
```

**Location**: 
- `LlamaBrain/Source/Persona/MemoryMutationController.cs`
- `LlamaBrainRuntime/Runtime/Core/WorldIntentDispatcher.cs`

---

## Component 9: Author-Controlled Fallback

**Purpose**: Provide context-aware fallback responses when all retry attempts fail.

**Unity Integration**: Automatically handled by `LlamaBrainAgent` - configurable via `FallbackConfigAsset`.

### Configuring Fallbacks

1. Create `FallbackConfigAsset`: `Create → LlamaBrain → Fallback Config`
2. Configure fallback lists:
   - **Generic Fallbacks**: Used for any trigger reason
   - **Context Fallbacks**: Specific to trigger reasons (PlayerUtterance, ZoneTrigger, etc.)
   - **Emergency Fallbacks**: Last resort when all else fails
3. Assign to `LlamaBrainAgent`'s "Fallback System" section

### Accessing Fallback Statistics

```csharp
var agent = GetComponent<LlamaBrainAgent>();
var stats = agent.FallbackStats;

Debug.Log($"Fallback usage: {stats.FallbackUsageCount}");
Debug.Log($"Validation pass rate: {stats.ValidationPassRate}%");
Debug.Log($"Most common failure: {stats.MostCommonFailureReason}");
```

**Location**: 
- `LlamaBrain/Source/Core/FallbackSystem.cs`
- `LlamaBrainRuntime/Runtime/Core/AuthorControlledFallback.cs`

---

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

## Voice System Integration (Features 31-32)

**Purpose**: Enable voice-based interactions with NPCs through microphone input and text-to-speech output.

**Features**:
- **Feature 31**: Whisper.unity integration for speech-to-text (voice input)
- **Feature 32**: Piper.unity integration for text-to-speech (voice output)

### Overview

The voice system provides a complete solution for spoken dialogue with NPCs. Player speech is transcribed to text via Whisper, processed through the standard LlamaBrain pipeline, and NPC responses are converted to speech via TTS.

**Architecture Flow**:
```
Player Speaks → Microphone → NpcVoiceInput → Whisper (STT) → 
LlamaBrainAgent (Standard Pipeline) → NpcVoiceOutput → TTS → Audio Playback
```

### Components

#### 1. NpcVoiceController

Central component managing voice input/output coordination.

**Setup**:
```csharp
// On NPC GameObject
public class VoiceNPC : MonoBehaviour
{
    [SerializeField] private NpcSpeechConfig speechConfig;
    private NpcVoiceController voiceController;
    private LlamaBrainAgent agent;
    
    void Start()
    {
        agent = GetComponent<LlamaBrainAgent>();
        voiceController = gameObject.AddComponent<NpcVoiceController>();
        voiceController.Initialize(agent, speechConfig);
        
        // Hook up events
        voiceController.OnTranscriptionReceived += HandleTranscription;
        voiceController.OnSpeechStarted += () => Debug.Log("NPC speaking...");
        voiceController.OnSpeechCompleted += () => Debug.Log("NPC finished speaking");
    }
    
    private async void HandleTranscription(string transcribedText)
    {
        Debug.Log($"Player said: {transcribedText}");
        // Agent processes and responds automatically
    }
    
    public void ToggleListening()
    {
        if (voiceController.IsListening)
            voiceController.StopListening();
        else
            voiceController.StartListening();
    }
}
```

**Key Methods**:
- `Initialize(LlamaBrainAgent, NpcSpeechConfig)` - Set up voice controller
- `StartListening()` - Begin recording microphone input
- `StopListening()` - Stop recording and transcribe
- `Speak(string text)` - Convert text to speech and play

**Events**:
- `OnTranscriptionReceived(string)` - Fired when speech is transcribed
- `OnSpeechStarted()` - Fired when TTS begins playback
- `OnSpeechCompleted()` - Fired when TTS finishes playback
- `OnListeningStarted()` - Fired when microphone starts recording
- `OnListeningStopped()` - Fired when microphone stops recording

#### 2. NpcVoiceInput

Handles microphone input and speech-to-text transcription.

**Key Features**:
- Microphone device selection
- Whisper model integration (local or API)
- Voice activity detection (VAD)
- Automatic silence detection
- Audio preprocessing (noise reduction, normalization)

**Configuration** (via NpcSpeechConfig):
```csharp
// Voice Input Settings
inputSettings.microphoneDevice = null; // null = default device
inputSettings.whisperModelPath = "path/to/whisper-model.bin";
inputSettings.useWhisperAPI = false; // Use local Whisper model
inputSettings.whisperAPIKey = ""; // If using API
inputSettings.silenceThreshold = 0.01f; // Audio level threshold
inputSettings.silenceDuration = 2.0f; // Seconds of silence before auto-stop
inputSettings.maxRecordingDuration = 30.0f; // Maximum recording length
```

#### 3. NpcVoiceOutput

Handles text-to-speech conversion and audio playback.

**Key Features**:
- Multiple TTS provider support (local, API-based)
- Voice parameter configuration (pitch, speed, volume)
- Audio caching for repeated phrases
- Integration with Unity AudioSource

**Configuration** (via NpcSpeechConfig):
```csharp
// Voice Output Settings
outputSettings.ttsProvider = TTSProvider.Local; // or TTSProvider.API
outputSettings.voiceId = "en-US-Standard-A"; // Voice selection
outputSettings.pitch = 1.0f; // Voice pitch multiplier
outputSettings.speed = 1.0f; // Speech speed multiplier
outputSettings.volume = 1.0f; // Audio volume
outputSettings.useAudioCache = true; // Cache common phrases
```

#### 4. NpcSpeechConfig

ScriptableObject for voice system configuration.

**Creation**:
1. Right-click in Project window
2. `Create → LlamaBrain → Voice → NPC Speech Config`
3. Configure in Inspector
4. Assign to NpcVoiceController

**Settings**:
- **Input Settings**: Microphone, Whisper, VAD
- **Output Settings**: TTS provider, voice, audio parameters
- **Integration Settings**: Agent reference, audio source
- **Debug Settings**: Logging, visualization

### Example: Complete Voice-Enabled NPC

```csharp
using UnityEngine;
using LlamaBrain.Runtime.Core;
using LlamaBrain.Runtime.Core.Voice;

public class CompleteVoiceNPC : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private LlamaBrainAgent agent;
    [SerializeField] private NpcSpeechConfig speechConfig;
    
    [Header("UI")]
    [SerializeField] private GameObject listeningIndicator;
    [SerializeField] private GameObject speakingIndicator;
    
    private NpcVoiceController voiceController;
    
    void Start()
    {
        // Initialize voice controller
        voiceController = gameObject.AddComponent<NpcVoiceController>();
        voiceController.Initialize(agent, speechConfig);
        
        // Hook up events for UI feedback
        voiceController.OnListeningStarted += () => 
        {
            listeningIndicator.SetActive(true);
            Debug.Log("Listening to player...");
        };
        
        voiceController.OnListeningStopped += () => 
        {
            listeningIndicator.SetActive(false);
            Debug.Log("Processing speech...");
        };
        
        voiceController.OnTranscriptionReceived += (text) =>
        {
            Debug.Log($"Transcribed: {text}");
            // Agent automatically processes through full pipeline:
            // 1. Create interaction context
            // 2. Evaluate expectancy rules
            // 3. Retrieve memory context
            // 4. Assemble prompt
            // 5. Query LLM
            // 6. Validate output
            // 7. Mutate memory
            // 8. Return response (or fallback)
        };
        
        voiceController.OnSpeechStarted += () => 
        {
            speakingIndicator.SetActive(true);
            Debug.Log("NPC speaking...");
        };
        
        voiceController.OnSpeechCompleted += () => 
        {
            speakingIndicator.SetActive(false);
            Debug.Log("NPC finished speaking");
        };
    }
    
    void Update()
    {
        // Toggle listening with Space key
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (voiceController.IsListening)
                voiceController.StopListening();
            else
                voiceController.StartListening();
        }
        
        // Manual stop with Escape
        if (Input.GetKeyDown(KeyCode.Escape) && voiceController.IsListening)
        {
            voiceController.StopListening();
        }
    }
    
    // Programmatic speech (e.g., for cutscenes)
    public void SayGreeting()
    {
        voiceController.Speak("Hello traveler! How can I help you today?");
    }
}
```

### Integration with LlamaBrainAgent

The voice system integrates seamlessly with the existing LlamaBrain pipeline:

```csharp
// In NpcVoiceController (simplified internal flow)
private async void ProcessTranscription(string transcribedText)
{
    // 1. Transcription received from Whisper
    OnTranscriptionReceived?.Invoke(transcribedText);
    
    // 2. Send to LlamaBrainAgent (goes through full pipeline)
    var response = await agent.SendPlayerInputAsync(transcribedText);
    
    // 3. Convert response to speech
    if (!string.IsNullOrEmpty(response))
    {
        await Speak(response);
    }
}
```

**Key Points**:
- Voice input is converted to text **before** entering the pipeline
- Standard validation, memory, and fallback systems apply
- Voice output is generated **after** validation succeeds
- All architectural guarantees maintained (determinism, validation, authority)

### Whisper Integration

**Local Whisper** (Recommended for Privacy):
```csharp
// Configure for local Whisper model
speechConfig.inputSettings.useWhisperAPI = false;
speechConfig.inputSettings.whisperModelPath = "Assets/Models/whisper-base.bin";
speechConfig.inputSettings.whisperLanguage = "en"; // or "auto" for detection
```

**Whisper API** (Cloud-based):
```csharp
// Configure for Whisper API
speechConfig.inputSettings.useWhisperAPI = true;
speechConfig.inputSettings.whisperAPIKey = "your-api-key";
speechConfig.inputSettings.whisperAPIEndpoint = "https://api.openai.com/v1/audio/transcriptions";
```

### TTS Provider Options

**1. Local TTS** (Platform-dependent):
```csharp
outputSettings.ttsProvider = TTSProvider.Local;
outputSettings.voiceId = "Microsoft David Desktop"; // Windows
// or "com.apple.speech.synthesis.voice.Alex" // macOS
```

**2. Cloud TTS** (Google, Azure, AWS):
```csharp
outputSettings.ttsProvider = TTSProvider.GoogleCloudTTS;
outputSettings.voiceId = "en-US-Standard-B";
outputSettings.apiKey = "your-google-cloud-api-key";
```

### Performance Considerations

**Audio Processing**:
- Whisper transcription: ~1-3 seconds (local, depends on audio length)
- TTS generation: ~0.5-2 seconds (depends on provider and text length)
- Total voice round-trip: ~2-6 seconds + LLM processing time

**Optimization Tips**:
1. Use local Whisper model for faster transcription
2. Cache common TTS phrases in NpcSpeechConfig
3. Pre-generate TTS for frequently used responses
4. Use smaller Whisper models (base, small) for real-time conversations
5. Consider streaming TTS for longer responses

### Troubleshooting

**Microphone Not Detecting**:
- Check `Microphone.devices` array for available devices
- Verify microphone permissions (Windows/macOS security settings)
- Test with `speechConfig.inputSettings.microphoneDevice = null` (default)

**Transcription Quality Issues**:
- Increase `silenceThreshold` if background noise is detected
- Use larger Whisper model (medium, large) for better accuracy
- Set correct `whisperLanguage` instead of "auto"

**TTS Not Playing**:
- Verify AudioSource component is attached
- Check audio output device settings
- Ensure TTS provider credentials are valid
- Test with simple phrase: `voiceController.Speak("Test")`

**Voice/Agent Integration Issues**:
- Ensure LlamaBrainAgent is properly initialized before voice controller
- Check that agent's PersonaConfig and BrainSettings are configured
- Verify agent is responding to text input before adding voice

### Files Reference

**Voice System**:
- `Runtime/Core/Voice/NpcVoiceController.cs` - Main controller
- `Runtime/Core/Voice/NpcVoiceInput.cs` - Microphone + Whisper
- `Runtime/Core/Voice/NpcVoiceOutput.cs` - TTS + Audio
- `Runtime/Core/Voice/NpcSpeechConfig.cs` - Configuration asset

**Integration**:
- Voice system works with all existing LlamaBrainAgent features
- Compatible with validation rules, expectancy constraints, memory mutations
- Maintains deterministic pipeline guarantees

---

## Save/Load UI System (Feature 16 Extension)

**Purpose**: Provide complete user interface for game state management including main menu, save/load screens, and pause menu.

### Overview

The save/load UI system provides a complete, production-ready interface for managing game state persistence. It integrates with LlamaBrainSaveManager (Feature 16) to provide seamless save/load functionality with a user-friendly UI.

### Key Components

#### RedRoomGameController
Main game controller managing scene transitions and game state.

#### MainMenu
Main menu UI panel with new game and load game options.
- Prefab: `Prefabs/UI/Panel_MainMenu.prefab`

#### LoadGameMenu
Save slot browser showing all available saves with metadata.
- Prefab: `Prefabs/UI/Panel_LoadGame.prefab`

#### LoadGameScrollViewElement
Individual save slot entry displaying metadata and load button.
- Prefab: `Prefabs/UI/Element_SaveGameEntry.prefab`

#### PausePanel
In-game pause menu with save, load, and quit options.
- Prefab: `Prefabs/UI/Panel_Pause.prefab`

### Basic Usage

```csharp
using UnityEngine;
using LlamaBrain.Runtime.Persistence;

public class GameStateExample : MonoBehaviour
{
    [SerializeField] private LlamaBrainSaveManager saveManager;
    
    // Quick save (F5)
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            QuickSave();
        }
        
        if (Input.GetKeyDown(KeyCode.F9))
        {
            QuickLoad();
        }
    }
    
    private async void QuickSave()
    {
        var result = await saveManager.QuickSave();
        if (result.Success)
        {
            Debug.Log("Quick save successful");
        }
    }
    
    private async void QuickLoad()
    {
        var result = await saveManager.QuickLoad();
        if (result.Success)
        {
            Debug.Log("Quick load successful");
        }
    }
    
    // Named save slot
    private async void SaveToSlot(string slotName)
    {
        var result = await saveManager.SaveToSlot(slotName);
        if (result.Success)
        {
            Debug.Log($"Saved to slot: {slotName}");
        }
    }
    
    // Load from slot
    private async void LoadFromSlot(string slotName)
    {
        var result = await saveManager.LoadFromSlot(slotName);
        if (result.Success)
        {
            Debug.Log($"Loaded from slot: {slotName}");
        }
    }
}
```

### Files Reference

**UI System**:
- `Runtime/RedRoom/RedRoomGameController.cs`
- `Runtime/RedRoom/UI/MainMenu.cs`
- `Runtime/RedRoom/UI/LoadGameMenu.cs`
- `Runtime/RedRoom/UI/LoadGameScrollViewElement.cs`
- `Runtime/RedRoom/UI/PausePanel.cs`

**See**: RedRoom sample scene for complete working example

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

### Component 1: Interaction Context
- **Core**: `LlamaBrain/Source/Core/Expectancy/InteractionContext.cs`
- **Unity Integration**: Automatic via `LlamaBrainAgent.SendPlayerInputAsync()`

### Component 2: Determinism Layer (Expectancy Engine)
- **Core Evaluator**: `LlamaBrain/Source/Core/Expectancy/ExpectancyEvaluator.cs`
- **Unity Wrapper**: `LlamaBrainRuntime/Runtime/Core/Expectancy/ExpectancyEngine.cs`
- **Rule Asset**: `LlamaBrainRuntime/Runtime/Core/Expectancy/ExpectancyRuleAsset.cs`
- **NPC Config**: `LlamaBrainRuntime/Runtime/Core/Expectancy/NpcExpectancyConfig.cs`
- **Constraints**: `LlamaBrain/Source/Core/Expectancy/Constraint.cs`

### Component 3: External Authoritative Memory System
- **Memory System**: `LlamaBrain/Source/Persona/AuthoritativeMemorySystem.cs`
- **Memory Store**: `LlamaBrain/Source/Persona/PersonaMemoryStore.cs`
- **Memory Types**: `LlamaBrain/Source/Persona/MemoryTypes/`
  - `MemoryAuthority.cs` - Authority levels
  - `CanonicalFact.cs` - Immutable facts
  - `WorldState.cs` - Mutable state
  - `EpisodicMemory.cs` - Conversation history
  - `BeliefMemory.cs` - NPC opinions

### Component 4: Authoritative State Snapshot
- **Core**: `LlamaBrain/Source/Core/Inference/StateSnapshot.cs`
- **Builder**: `LlamaBrain/Source/Core/Inference/StateSnapshotBuilder.cs`
- **Context Retrieval**: `LlamaBrain/Source/Core/Inference/ContextRetrievalLayer.cs`
- **Unity Integration**: Automatic via `LlamaBrainAgent`

### Component 5: Ephemeral Working Memory
- **Core**: `LlamaBrain/Source/Core/Inference/EphemeralWorkingMemory.cs`
- **Prompt Assembler**: `LlamaBrain/Source/Core/Inference/PromptAssembler.cs`
- **Config**: `LlamaBrain/Source/Core/Inference/WorkingMemoryConfig.cs`
- **Unity Settings**: `LlamaBrainRuntime/Runtime/Core/Inference/PromptAssemblerSettings.cs`

### Component 6: Stateless Inference Core
- **API Client**: `LlamaBrain/Source/Core/ApiClient.cs`
- **Server Management**: `LlamaBrainRuntime/Runtime/Core/BrainServer.cs`
- **Unity Integration**: Automatic via `LlamaBrainAgent`

### Component 7: Output Parsing & Validation
- **Validation Rule Asset**: `LlamaBrainRuntime/Runtime/Core/Validation/ValidationRuleAsset.cs`
- **Validation Pipeline**: `LlamaBrainRuntime/Runtime/Core/Validation/ValidationPipeline.cs`
- **Core Validation Gate**: `LlamaBrain/Source/Core/Validation/ValidationGate.cs`
- **Core Output Parser**: `LlamaBrain/Source/Core/Validation/OutputParser.cs`
- **Response Validator**: `LlamaBrain/Source/Core/Inference/ResponseValidator.cs`

### Component 8: Memory Mutation + World Effects
- **Mutation Controller**: `LlamaBrain/Source/Persona/MemoryMutationController.cs`
- **World Intent Dispatcher**: `LlamaBrainRuntime/Runtime/Core/WorldIntentDispatcher.cs`
- **Unity Integration**: Automatic via `LlamaBrainAgent`

### Component 9: Author-Controlled Fallback
- **Core**: `LlamaBrain/Source/Core/FallbackSystem.cs`
- **Unity Wrapper**: `LlamaBrainRuntime/Runtime/Core/AuthorControlledFallback.cs`
- **Config Asset**: `LlamaBrainRuntime/Runtime/Core/FallbackConfigAsset.cs`

---

## Testing

All components have comprehensive test coverage:

- **Component 1-2 Tests**: `LlamaBrain.Tests/Expectancy/` (50+ tests)
- **Component 3 Tests**: `LlamaBrain.Tests/Memory/` (65+ tests)
- **Component 4-5 Tests**: `LlamaBrain.Tests/Inference/` (80+ tests)
- **Component 7 Tests**: `LlamaBrain.Tests/Validation/` (60+ tests)
- **Integration Tests**: `LlamaBrain.Tests/Integration/` (8+ tests)

Run tests to see examples of usage patterns!

---

## Tutorials

This section provides step-by-step tutorials for common tasks. These tutorials walk you through complete workflows from start to finish.

### Tutorial 1: Setting Up Deterministic NPCs

This tutorial shows you how to create an NPC that behaves consistently and predictably using LlamaBrain's determinism layer.

#### Step 1: Create Your NPC GameObject

1. In Unity, create a new GameObject (right-click in Hierarchy → `Create Empty`)
2. Name it something like "GuardNPC" or "MerchantNPC"
3. Add the `LlamaBrainAgent` component (Add Component → LlamaBrain → LlamaBrain Agent)

#### Step 2: Configure the Brain Server

1. Create a `BrainSettings` asset:
   - Right-click in Project window → `Create → LlamaBrain → Brain Settings`
   - Name it "MyBrainSettings"
2. Configure the settings:
   - **Server Host**: `localhost` (or your llama.cpp server address)
   - **Server Port**: `8080` (or your server port)
   - **Model Name**: The name of your model (e.g., "llama-2-7b-chat")
   - **Temperature**: `0.7` (lower = more deterministic)
   - **Top P**: `0.9`
   - **Max Tokens**: `256`
3. Assign to your NPC:
   - Select your NPC GameObject
   - In the `LlamaBrainAgent` component, find "Persona Configuration"
   - Drag "MyBrainSettings" into the "Brain Settings" field

#### Step 3: Create a Persona Config

1. Create a `PersonaConfig` asset:
   - Right-click in Project window → `Create → LlamaBrain → Persona Config`
   - Name it "GuardPersona" (or appropriate name)
2. Configure the persona:
   - **NPC Name**: "Guard" (or your NPC's name)
   - **System Prompt**: Write a system prompt describing the NPC's personality, role, and behavior
   
   Example system prompt:
   ```
   You are a royal guard stationed at the castle gate. You are professional, 
   loyal to the crown, and take your duty seriously. You speak formally but 
   not coldly. You protect the castle and its secrets.
   ```
3. Assign to your NPC:
   - Select your NPC GameObject
   - In the `LlamaBrainAgent` component, drag "GuardPersona" into the "Persona Config" field

#### Step 4: Add Expectancy Rules (Optional but Recommended)

Expectancy rules control NPC behavior deterministically:

1. Create an `ExpectancyRuleAsset`:
   - Right-click in Project window → `Create → LlamaBrain → Expectancy Rule`
   - Name it "GuardNoSecrets"
2. Configure the rule:
   - **Rule Name**: "Guard Cannot Reveal Secrets"
   - **Priority**: `100` (higher = evaluated first)
   - **Conditions**: 
     - Add condition: Type = `TriggerReason`, Value = `PlayerUtterance`
   - **Constraints**:
     - Add constraint: Type = `Prohibition`, Severity = `Hard`
     - **Prompt Injection**: "You must NOT reveal any classified information, secret locations, or sensitive details about the kingdom's defenses."
     - **Validation Patterns**: `["classified", "secret location", "defense plan"]`
3. Create an `NpcExpectancyConfig`:
   - Select your NPC GameObject
   - Add Component → LlamaBrain → NPC Expectancy Config
   - Drag "GuardNoSecrets" into the "NPC Rules" list

#### Step 5: Initialize Canonical Facts (World Truths)

Canonical facts are immutable world truths that NPCs cannot contradict:

```csharp
using UnityEngine;
using LlamaBrain.Runtime.Core;

public class GuardNPC : MonoBehaviour
{
    private LlamaBrainAgent agent;
    
    void Start()
    {
        agent = GetComponent<LlamaBrainAgent>();
        
        // Initialize canonical facts (immutable world truths)
        agent.InitializeCanonicalFact(
            factId: "king_name",
            fact: "The king's name is Arthur Pendragon",
            domain: "world_lore"
        );
        
        agent.InitializeCanonicalFact(
            factId: "castle_location",
            fact: "The castle is located in the capital city",
            domain: "world_lore"
        );
    }
    
    public async void TalkToPlayer(string playerInput)
    {
        var response = await agent.SendPlayerInputAsync(playerInput);
        Debug.Log($"Guard says: {response}");
        // Display response in your UI
    }
}
```

#### Step 6: Test Your NPC

1. Create a simple test script or use Unity's Play mode
2. Call `TalkToPlayer()` with various inputs
3. Verify the NPC responds consistently and doesn't contradict canonical facts

**What You've Achieved:**
- ✅ NPC with deterministic behavior
- ✅ Protected world truths (canonical facts)
- ✅ Behavior constraints (expectancy rules)
- ✅ Consistent responses based on persona

**Next Steps:**
- Add validation rules to filter content (see Tutorial 2)
- Set up memory system for persistent conversations (see Component 3 section)
- Configure fallback responses for error handling

---

### Tutorial 2: Creating Custom Validation Rules

This tutorial shows you how to create validation rules that control what NPCs can and cannot say.

#### Step 1: Understand Validation Rule Types

Validation rules come in two types:
- **Prohibition**: NPC must NOT contain the pattern (e.g., no swearing)
- **Requirement**: NPC must contain the pattern (e.g., must greet player)

#### Step 2: Create a "No Swearing" Rule

1. Create a `ValidationRuleAsset`:
   - Right-click in Project window → `Create → LlamaBrain → Validation Rule`
   - Name it "NoSwearing"
2. Configure the rule:
   - **Rule ID**: Auto-generated (or set manually)
   - **Description**: "NPCs should not use profanity"
   - **Rule Type**: `Prohibition`
   - **Severity**: `Hard` (causes retry if violated)
   - **Pattern**: `\b(swear|curse|profanity|damn|hell)\b`
   - **Case Insensitive**: `true`
   - **Additional Patterns**: (optional) Add more patterns like `\b(crap|darn)\b`

#### Step 3: Create a Validation Rule Set

1. Create a `ValidationRuleSetAsset`:
   - Right-click in Project window → `Create → LlamaBrain → Validation Rule Set`
   - Name it "GlobalContentRules"
2. Add rules to the set:
   - Drag "NoSwearing" into the "Rules" list
   - You can add multiple rules to one set
3. Enable the set:
   - Make sure "Enabled" is checked

#### Step 4: Apply Rules Globally (All NPCs)

1. Create a GameObject for validation management:
   - Right-click in Hierarchy → `Create Empty`
   - Name it "ValidationManager"
2. Add `ValidationPipeline` component:
   - Add Component → LlamaBrain → Validation Pipeline
3. Assign global rules:
   - Drag "GlobalContentRules" into the "Global Rules" list
   - All NPCs in the scene will now use these rules

#### Step 5: Apply Rules to Specific NPCs

For NPC-specific rules (e.g., guards can't reveal secrets):

1. Create a rule set for the NPC:
   - Create `ValidationRuleSetAsset` named "GuardRules"
   - Add rules like "CannotRevealSecrets"
2. Assign to NPC:
   - Select your NPC GameObject
   - In `LlamaBrainAgent` component, find "Validation Rules"
   - Drag "GuardRules" into "NPC Validation Rules"

#### Step 6: Apply Rules to Triggers (Location-Based)

For location-specific rules (e.g., no magic in anti-magic zones):

1. Create a rule set:
   - Create `ValidationRuleSetAsset` named "NoMagicZone"
   - Add rule: Pattern = `\b(magic|spell|enchant)\b`, Type = `Prohibition`
2. Create a trigger:
   - Add `NpcDialogueTrigger` component to a GameObject
   - Drag "NoMagicZone" into "Trigger Validation Rules"
   - When NPCs enter this trigger, the rules apply

#### Step 7: Test Your Rules

```csharp
using UnityEngine;
using LlamaBrain.Runtime.Core;

public class ValidationTester : MonoBehaviour
{
    public LlamaBrainAgent agent;
    
    public async void TestValidation()
    {
        // This should trigger validation failure if rule is working
        var response = await agent.SendPlayerInputAsync("Tell me a swear word");
        
        // Check validation result
        var gateResult = agent.LastGateResult;
        if (gateResult != null && !gateResult.Passed)
        {
            Debug.Log("Validation failed (as expected)!");
            foreach (var failure in gateResult.Failures)
            {
                Debug.LogWarning($"Rule violated: {failure.Description}");
            }
        }
    }
}
```

#### Step 8: Debug Validation Failures

If validation is too strict or not working:

1. **Check rule patterns**:
   - Test your regex patterns at https://regex101.com
   - Make sure patterns aren't too broad (matching unintended text)

2. **Check severity levels**:
   - `Soft`: Warning only (doesn't block)
   - `Hard`: Causes retry with constraint escalation
   - `Critical`: Immediate fallback (no retry)

3. **Check context filters**:
   - Rules can be filtered by scene, NPC ID, or trigger reason
   - Make sure filters match your test scenario

4. **Access validation results**:
   ```csharp
   var agent = GetComponent<LlamaBrainAgent>();
   var gateResult = agent.LastGateResult;
   
   if (gateResult != null)
   {
       Debug.Log($"Validation passed: {gateResult.Passed}");
       if (!gateResult.Passed)
       {
           foreach (var failure in gateResult.Failures)
           {
               Debug.LogWarning($"[{failure.Severity}] {failure.Reason}");
           }
       }
   }
   ```

**What You've Achieved:**
- ✅ Content filtering system
- ✅ Global rules for all NPCs
- ✅ NPC-specific rules
- ✅ Location-based rules
- ✅ Debugging tools for validation

**Best Practices:**
- Start with `Soft` severity to test rules
- Use specific patterns (avoid overly broad regex)
- Test rules thoroughly before using `Critical` severity
- Use context filters to narrow rule application

---

### Tutorial 3: Understanding Memory Authority

This tutorial explains LlamaBrain's memory authority system and how to use it effectively.

#### Step 1: Understand Memory Types and Authority

LlamaBrain has four memory types with different authority levels:

1. **Canonical Facts** (Highest Authority)
   - Immutable world truths
   - Cannot be modified by AI
   - Set by designers/game systems only
   - Example: "The king's name is Arthur"

2. **World State** (High Authority)
   - Mutable game state
   - Can be updated by game systems
   - Example: "The castle gate is open"

3. **Episodic Memory** (Medium Authority)
   - Conversation history
   - Can be added by validated AI output
   - Can decay over time
   - Example: "Player said: Hello"

4. **Beliefs** (Lowest Authority)
   - NPC opinions and relationships
   - Can be wrong or contradicted
   - Can be updated by validated AI output
   - Example: "The player is trustworthy"

#### Step 2: Initialize Canonical Facts

Canonical facts protect world truths from AI hallucinations:

```csharp
using UnityEngine;
using LlamaBrain.Runtime.Core;

public class MemoryAuthorityExample : MonoBehaviour
{
    private LlamaBrainAgent agent;
    
    void Start()
    {
        agent = GetComponent<LlamaBrainAgent>();
        
        // Add canonical facts (immutable)
        agent.InitializeCanonicalFact(
            factId: "world_rule_1",
            fact: "Magic is real in this world",
            domain: "world_lore"
        );
        
        agent.InitializeCanonicalFact(
            factId: "king_name",
            fact: "The king's name is Arthur Pendragon",
            domain: "world_lore"
        );
        
        // Try to modify (will fail silently)
        // This demonstrates authority protection
        var result = agent.InitializeCanonicalFact("king_name", "The king is Bob");
        // result will indicate failure - canonical facts cannot be modified
    }
}
```

#### Step 3: Set World State (Game System Authority)

World state is updated by game systems, not AI:

```csharp
// In your game system code
var memorySystem = memoryStore.GetOrCreateSystem("npc_001");

// Update world state when game events occur
memorySystem.SetWorldState(
    key: "quest_status",
    value: "The main quest has been completed",
    source: MutationSource.GameSystem
);

memorySystem.SetWorldState(
    key: "weather",
    value: "It is currently raining",
    source: MutationSource.GameSystem
);
```

#### Step 4: Understand Episodic Memory (AI Can Add)

Episodic memory stores conversation history and can be added by validated AI output:

```csharp
// Episodic memory is automatically added when validation passes
// But you can also add it manually:

var memorySystem = memoryStore.GetOrCreateSystem("npc_001");

// Add dialogue with significance (higher = less likely to decay)
memorySystem.AddDialogue(
    speaker: "Player",
    content: "I need 10 health potions",
    significance: 0.7f, // Important conversation
    source: MutationSource.ValidatedOutput
);

// Add observation
var observation = EpisodicMemoryEntry.FromObservation(
    observation: "Player purchased expensive items",
    significance: 0.5f
);
memorySystem.AddEpisodicMemory(observation, MutationSource.ValidatedOutput);
```

#### Step 5: Understand Beliefs (NPC Opinions)

Beliefs are NPC opinions that can be wrong:

```csharp
var memorySystem = memoryStore.GetOrCreateSystem("guard_001");

// Set a belief (NPC opinion)
var belief = new BeliefMemoryEntry(
    subject: "Player",
    beliefContent: "The player is trustworthy"
);
memorySystem.SetBelief("player_trust", belief, MutationSource.ValidatedOutput);

// Beliefs can contradict canonical facts
// If a belief contradicts a canonical fact, it gets marked as contradicted
var badBelief = new BeliefMemoryEntry(
    subject: "King",
    beliefContent: "The king's name is not Arthur" // Contradicts canonical fact!
);
memorySystem.SetBelief("wrong_king_name", badBelief, MutationSource.ValidatedOutput);
// This belief will be marked as contradicted and won't be used in prompts
```

#### Step 6: Verify Authority Enforcement

Test that canonical facts cannot be modified:

```csharp
using UnityEngine;
using LlamaBrain.Runtime.Core;
using LlamaBrain.Persona;

public class AuthorityTest : MonoBehaviour
{
    private LlamaBrainAgent agent;
    
    void Start()
    {
        agent = GetComponent<LlamaBrainAgent>();
        
        // Initialize canonical fact
        agent.InitializeCanonicalFact("test_fact", "The sky is blue", "test");
        
        // Try to modify via memory system (should fail)
        var memoryStore = new PersonaMemoryStore();
        var memorySystem = memoryStore.GetOrCreateSystem(agent.PersonaId);
        
        // Attempt to modify canonical fact (will be blocked)
        var result = memorySystem.AddCanonicalFact("test_fact", "The sky is red");
        if (!result.Success)
        {
            Debug.Log($"Authority protection working! Error: {result.FailureReason}");
        }
    }
}
```

#### Step 7: Use Memory Statistics

Monitor your memory system:

```csharp
var memorySystem = memoryStore.GetOrCreateSystem("npc_001");
var stats = memorySystem.GetStatistics();

Debug.Log($"Canonical Facts: {stats.CanonicalFactCount}");
Debug.Log($"World State: {stats.WorldStateCount}");
Debug.Log($"Episodic Memories: {stats.EpisodicMemoryCount}");
Debug.Log($"Beliefs: {stats.BeliefCount} (Active: {stats.ActiveBeliefCount})");
```

**Key Takeaways:**
- ✅ Canonical facts are immutable (highest authority)
- ✅ World state is game-controlled
- ✅ Episodic memory and beliefs can be added by AI (after validation)
- ✅ Authority hierarchy prevents AI from corrupting world truths
- ✅ Contradicted beliefs are automatically marked and excluded

**Common Mistakes to Avoid:**
- ❌ Don't try to modify canonical facts (they're immutable)
- ❌ Don't set world state from AI output (use GameSystem source)
- ❌ Don't forget to set significance for episodic memories (affects decay)
- ❌ Don't ignore contradicted beliefs (they're automatically handled)

---

### Tutorial 4: Debugging Validation Failures

This tutorial shows you how to diagnose and fix validation failures when NPCs fail to respond.

#### Step 1: Understand Why Validation Fails

Validation can fail for several reasons:
1. **Rule violations**: Output matches a prohibition pattern
2. **Missing requirements**: Output doesn't match a requirement pattern
3. **Canonical fact contradictions**: Output contradicts a canonical fact
4. **Constraint violations**: Output violates expectancy constraints
5. **Parsing errors**: Output is malformed and can't be parsed

#### Step 2: Access Validation Results

Get detailed information about validation failures:

```csharp
using UnityEngine;
using LlamaBrain.Runtime.Core;

public class ValidationDebugger : MonoBehaviour
{
    public LlamaBrainAgent agent;
    
    public async void TestWithDebugging()
    {
        var response = await agent.SendPlayerInputAsync("Hello!");
        
        // Check validation result
        var gateResult = agent.LastGateResult;
        if (gateResult != null)
        {
            if (gateResult.Passed)
            {
                Debug.Log("✅ Validation passed!");
            }
            else
            {
                Debug.LogError("❌ Validation failed!");
                
                // Print all failures
                foreach (var failure in gateResult.Failures)
                {
                    Debug.LogWarning($"Failure: [{failure.Severity}] {failure.Reason}");
                    Debug.LogWarning($"Description: {failure.Description}");
                    
                    // Print violating text if available
                    if (!string.IsNullOrEmpty(failure.ViolatingText))
                    {
                        Debug.LogWarning($"Violating text: {failure.ViolatingText}");
                    }
                }
            }
        }
    }
}
```

#### Step 3: Check Retry Attempts

When validation fails, the system retries with constraint escalation:

```csharp
var agent = GetComponent<LlamaBrainAgent>();
var inferenceResult = agent.LastInferenceResult;

if (inferenceResult != null)
{
    Debug.Log($"Total attempts: {inferenceResult.Attempts.Count}");
    
    foreach (var attempt in inferenceResult.Attempts)
    {
        Debug.Log($"Attempt {attempt.AttemptNumber}:");
        Debug.Log($"  - Passed: {attempt.ValidationOutcome == ValidationOutcome.Passed}");
        Debug.Log($"  - Outcome: {attempt.ValidationOutcome}");
        
        if (attempt.ValidationOutcome != ValidationOutcome.Passed)
        {
            foreach (var violation in attempt.ConstraintViolations)
            {
                Debug.LogWarning($"  - Violation: {violation.ConstraintId} - {violation.Description}");
            }
        }
    }
}
```

#### Step 4: Debug Rule Patterns

If a rule is too strict or not matching:

```csharp
// Test your regex patterns
using System.Text.RegularExpressions;

public void TestRulePattern()
{
    string pattern = @"\b(swear|curse)\b";
    string testText = "I would never swear!";
    
    var match = Regex.Match(testText, pattern, RegexOptions.IgnoreCase);
    if (match.Success)
    {
        Debug.Log($"Pattern matched: '{match.Value}'");
    }
    else
    {
        Debug.Log("Pattern did not match");
    }
}
```

#### Step 5: Check Constraint Escalation

See how constraints escalate during retries:

```csharp
var agent = GetComponent<LlamaBrainAgent>();
var snapshot = agent.LastSnapshot;

if (snapshot != null)
{
    Debug.Log($"Attempt: {snapshot.AttemptNumber}/{snapshot.MaxAttempts}");
    Debug.Log($"Constraints: {snapshot.Constraints.Count}");
    
    foreach (var constraint in snapshot.Constraints)
    {
        Debug.Log($"  - {constraint.Type}: {constraint.Description}");
        Debug.Log($"    Severity: {constraint.Severity}");
    }
}
```

#### Step 6: Debug Parsing Errors

If output can't be parsed:

```csharp
var pipeline = ValidationPipeline.Instance;
var lastParsed = pipeline.LastParsedOutput;

if (lastParsed != null)
{
    if (lastParsed.HasParsingErrors)
    {
        Debug.LogError("Parsing errors detected!");
        foreach (var error in lastParsed.ParsingErrors)
        {
            Debug.LogError($"  - {error}");
        }
    }
    else
    {
        Debug.Log($"Parsed dialogue: {lastParsed.DialogueText}");
    }
}
```

#### Step 7: Use Fallback Statistics

Monitor how often fallbacks are used:

```csharp
var agent = GetComponent<LlamaBrainAgent>();
var stats = agent.FallbackStats;

Debug.Log($"Fallback usage: {stats.FallbackUsageCount}");
Debug.Log($"Validation pass rate: {stats.ValidationPassRate}%");
Debug.Log($"Most common failure: {stats.MostCommonFailureReason}");
```

#### Step 8: Common Issues and Solutions

**Issue: Validation always fails**
- **Solution**: Check rule patterns - they might be too broad
- **Solution**: Lower rule severity from `Critical` to `Hard`
- **Solution**: Check if rules have conflicting requirements

**Issue: Rules not applying**
- **Solution**: Verify `ValidationPipeline` exists in scene (for global rules)
- **Solution**: Check rule set is enabled
- **Solution**: Verify context filters match (scene, NPC ID, trigger reason)

**Issue: NPC-specific rules not working**
- **Solution**: Ensure rules are assigned to `LlamaBrainAgent` component
- **Solution**: Check agent is initialized (rules load during initialization)

**Issue: Too many retries**
- **Solution**: Reduce `MaxRetries` in retry policy
- **Solution**: Check if constraints are too strict
- **Solution**: Consider using `Soft` severity for non-critical rules

#### Step 9: Create a Debug Helper Script

Create a reusable debug helper:

```csharp
using UnityEngine;
using LlamaBrain.Runtime.Core;

public static class ValidationDebugHelper
{
    public static void PrintValidationDetails(LlamaBrainAgent agent)
    {
        if (agent == null) return;
        
        var gateResult = agent.LastGateResult;
        var inferenceResult = agent.LastInferenceResult;
        
        Debug.Log("=== Validation Debug Info ===");
        
        if (gateResult != null)
        {
            Debug.Log($"Validation Passed: {gateResult.Passed}");
            Debug.Log($"Failure Count: {gateResult.Failures.Count}");
            
            foreach (var failure in gateResult.Failures)
            {
                Debug.LogWarning($"[{failure.Severity}] {failure.Reason}: {failure.Description}");
            }
        }
        
        if (inferenceResult != null)
        {
            Debug.Log($"Total Attempts: {inferenceResult.Attempts.Count}");
            Debug.Log($"Final Outcome: {inferenceResult.FinalOutcome}");
        }
        
        var stats = agent.FallbackStats;
        Debug.Log($"Fallback Usage: {stats.FallbackUsageCount}");
        Debug.Log($"Pass Rate: {stats.ValidationPassRate}%");
    }
}

// Usage:
ValidationDebugHelper.PrintValidationDetails(agent);
```

**What You've Achieved:**
- ✅ Understanding of validation failure reasons
- ✅ Tools to debug validation issues
- ✅ Ability to monitor retry attempts
- ✅ Knowledge of common issues and solutions
- ✅ Reusable debug helper script

**Best Practices:**
- Always check `LastGateResult` after interactions
- Monitor fallback statistics to identify patterns
- Test rule patterns before deploying
- Use appropriate severity levels (don't overuse `Critical`)
- Keep debug helpers in your project for quick diagnosis

---

## Next Steps

After completing these tutorials, you should be able to:
- ✅ Set up deterministic NPCs with proper configuration
- ✅ Create and apply validation rules at multiple levels
- ✅ Understand and use the memory authority system
- ✅ Debug validation failures effectively

For more advanced topics, see:
- Component-specific sections above for detailed API reference
- `ARCHITECTURE.md` for system design understanding
- `DETERMINISM_CONTRACT.md` for determinism guarantees

