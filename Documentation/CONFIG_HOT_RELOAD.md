# Configuration Hot Reload & A/B Testing

**Feature 29: Prompt A/B Testing & Hot Reload**

This document provides comprehensive documentation for LlamaBrain's configuration hot reload system and A/B testing framework.

## Table of Contents

- [Overview](#overview)
- [Hot Reload System](#hot-reload-system)
  - [PersonaConfig Hot Reload](#personaconfig-hot-reload)
  - [BrainSettings Hot Reload](#brainsettings-hot-reload)
  - [State Preservation](#state-preservation)
- [A/B Testing Framework](#ab-testing-framework)
  - [Variant Configuration](#variant-configuration)
  - [Deterministic Selection](#deterministic-selection)
  - [Metrics Tracking](#metrics-tracking)
  - [Export Capabilities](#export-capabilities)
- [Performance Characteristics](#performance-characteristics)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

---

## Overview

The hot reload system enables narrative designers and developers to modify NPC behavior and LLM parameters **at runtime** without restarting the game. The A/B testing framework provides deterministic variant selection for controlled experimentation.

**Key Features:**
- ✅ **Runtime Configuration**: Modify PersonaConfig and BrainSettings during play mode
- ✅ **State Preservation**: Maintains InteractionCount, memory, dialogue history
- ✅ **Validation & Rollback**: Validates changes before applying, rolls back on failure
- ✅ **A/B Testing**: Deterministic prompt variant selection with metrics
- ✅ **Performance**: < 10ms validation, < 1ms variant selection
- ✅ **Thread-Safe**: Safe for concurrent operations

---

## Hot Reload System

### PersonaConfig Hot Reload

**What Can Be Hot-Reloaded:**
- System Prompt
- Name, Description, Background
- Trait assignments
- Metadata
- **Prompt Variants (A/B Testing)**

**What Is Preserved:**
- InteractionCount (critical for determinism)
- Episodic memory (PersonaMemoryStore)
- Dialogue history (DialogueSession)
- Current conversation context

**How It Works:**

1. **Detection**: `UnityEditorConfigWatcher` monitors `AssetDatabase.importedAssets`
2. **Debouncing**: 100ms window to batch rapid changes
3. **Validation**: `ConfigValidator.ValidatePersonaProfile()` checks new config
4. **Application**: `LlamaBrainAgent.ReloadPersonaConfig()` updates runtime profile
5. **Rollback**: On validation failure, keeps previous profile unchanged

**Unity Menu Integration:**
```
LlamaBrain/Hot Reload/Enable   - Enable hot reload (default in Editor)
LlamaBrain/Hot Reload/Disable  - Disable hot reload
LlamaBrain/Hot Reload/Show Statistics - Display reload stats
LlamaBrain/Hot Reload/Reset Statistics - Reset counters
```

**Example:**
```csharp
// Subscribe to reload events
agent.OnPersonaConfigReloaded += (agent, oldProfile, newProfile) =>
{
    Debug.Log($"Reloaded: {oldProfile.Name} → {newProfile.Name}");
    Debug.Log($"InteractionCount preserved: {agent.InteractionCount}");
};

// Manually trigger reload
bool success = agent.ReloadPersonaConfig();
if (!success)
{
    Debug.LogError("Reload failed - check validation errors in console");
}
```

**Validation Rules:**
- Name must not be empty
- PersonaId must not be empty
- SystemPrompt should be present (warning if missing)
- Variant traffic percentages must sum to 100% (if variants configured)

### BrainSettings Hot Reload

**Two-Tier Configuration:**

| Parameter | Hot-Reloadable | Requires Restart |
|-----------|----------------|------------------|
| Temperature | ✅ Yes | |
| MaxTokens | ✅ Yes | |
| TopP | ✅ Yes | |
| TopK | ✅ Yes | |
| RepeatPenalty | ✅ Yes | |
| StopSequences | ✅ Yes | |
| GPU Layers | | ❌ Yes |
| Model Path | | ❌ Yes |
| Context Size | | ❌ Yes |
| Batch Size | | ❌ Yes |

**How It Works:**

1. **Detection**: `ConfigHotReloadManager` detects BrainSettings changes
2. **Validation**: `LlmConfig.Validate()` checks LLM parameters
3. **Server Config Check**: `BrainServer.HasServerConfigChanged()` detects restart-required changes
4. **Warning**: Logs warning if server-level params changed
5. **Broadcast**: Updates all registered agents via `agent.UpdateLlmConfig()`

**Example:**
```csharp
// Subscribe to BrainSettings reload events
brainServer.OnBrainSettingsReloaded += (llmConfig) =>
{
    Debug.Log($"LLM Config updated: Temp={llmConfig.Temperature}, MaxTokens={llmConfig.MaxTokens}");
};

// Manually trigger reload
bool success = brainServer.ReloadBrainSettings();
if (!success)
{
    Debug.LogError("BrainSettings reload failed");
}
```

**Restart-Required Warning:**
```
[HotReload] Server-level settings changed (GPU layers, model path, context size, or batch size).
Full restart required to apply these changes.
[HotReload] LLM generation parameters (Temperature, MaxTokens, etc.) will be applied immediately.
```

### State Preservation

**Critical Guarantees:**

1. **InteractionCount Preserved**:
   ```csharp
   var countBefore = agent.InteractionCount;
   agent.ReloadPersonaConfig();
   Assert.AreEqual(countBefore, agent.InteractionCount); // Always true
   ```

2. **Memory Preserved**:
   ```csharp
   // Episodic memories remain intact
   var memoryBefore = agent.GetMemoryStore().GetEpisodicMemories();
   agent.ReloadPersonaConfig();
   var memoryAfter = agent.GetMemoryStore().GetEpisodicMemories();
   Assert.AreEqual(memoryBefore.Count, memoryAfter.Count);
   ```

3. **Dialogue History Preserved**:
   ```csharp
   // Conversation context maintained
   var historyBefore = agent.GetConversationHistory();
   agent.ReloadPersonaConfig();
   var historyAfter = agent.GetConversationHistory();
   Assert.AreEqual(historyBefore.Count, historyAfter.Count);
   ```

---

## A/B Testing Framework

### Variant Configuration

**Unity Inspector Setup:**

1. Open PersonaConfig in Inspector
2. Expand "A/B Testing (Optional)" section
3. Add variants to "System Prompt Variants" list:

```
Variant 0:
  Variant Name: "Control"
  System Prompt: "You are a friendly wizard who helps travelers."
  Traffic Percentage: 50
  Is Active: ✓

Variant 1:
  Variant Name: "Experimental"
  System Prompt: "You are a mysterious wizard who speaks in riddles."
  Traffic Percentage: 50
  Is Active: ✓
```

**Validation:**
- Active variant traffic must sum to 100%
- Variant names must be unique
- At least one variant should be active

**Example - Code Configuration:**
```csharp
// Alternatively, configure variants in code (for testing)
personaConfig.SystemPromptVariants = new List<PromptVariantConfig>
{
    new PromptVariantConfig
    {
        VariantName = "Control",
        SystemPrompt = "You are a friendly wizard.",
        TrafficPercentage = 50f,
        IsActive = true
    },
    new PromptVariantConfig
    {
        VariantName = "Experimental",
        SystemPrompt = "You are a mysterious wizard.",
        TrafficPercentage = 50f,
        IsActive = true
    }
};
```

### Deterministic Selection

**How It Works:**

1. **Hash Calculation**: `HashCode.Combine(InteractionCount, PersonaId)`
2. **Bucket Assignment**: `hash % 100` → 0-99 bucket
3. **Cumulative Distribution**: Select variant based on traffic percentages

**Determinism Contract:**
```csharp
// Same InteractionCount + PersonaId → Same Variant
agent.InteractionCount = 42;
var variant1 = agent.SelectSystemPromptVariant();

agent.InteractionCount = 42; // Reset to same value
var variant2 = agent.SelectSystemPromptVariant();

Assert.AreEqual(variant1, variant2); // Always true
```

**Traffic Distribution:**
```csharp
// 50/50 split example:
// Bucket 0-49 → Variant A (50%)
// Bucket 50-99 → Variant B (50%)

// 10/90 split example:
// Bucket 0-9 → Experimental (10%)
// Bucket 10-99 → Control (90%)
```

**Gradual Rollout Example:**
```csharp
// Week 1: 10% experimental
Experimental: 10% traffic, Active
Control: 90% traffic, Active

// Week 2: 25% experimental (if metrics look good)
Experimental: 25% traffic, Active
Control: 75% traffic, Active

// Week 3: 50% experimental
Experimental: 50% traffic, Active
Control: 50% traffic, Active

// Week 4: Disable underperforming variant
Experimental: 100% traffic, Active (winning variant)
Control: 0% traffic, Inactive
```

### Metrics Tracking

**Automatically Tracked:**
- **SelectionCount**: How many times variant was selected
- **SuccessCount**: Successful interactions (validation passed)
- **ValidationFailureCount**: Failed validations
- **FallbackCount**: Fallback responses triggered
- **AvgLatencyMs**: Average response latency
- **AvgTokensGenerated**: Average tokens per response

**Access Metrics:**
```csharp
// Get metrics from agent
var metrics = agent.GetVariantMetrics();
foreach (var kvp in metrics)
{
    var variantName = kvp.Key;
    var variantMetrics = kvp.Value;

    Debug.Log($"{variantName}:");
    Debug.Log($"  Selections: {variantMetrics.SelectionCount}");
    Debug.Log($"  Success Rate: {variantMetrics.SuccessCount / (float)variantMetrics.SelectionCount:P2}");
    Debug.Log($"  Avg Latency: {variantMetrics.AvgLatencyMs:F2}ms");
    Debug.Log($"  Avg Tokens: {variantMetrics.AvgTokensGenerated:F2}");
}
```

**Aggregate Across All Agents:**
```csharp
// BrainServer aggregates metrics from all agents
var report = brainServer.GenerateABTestReport("WizardPersonalityTest");
report.Complete();

Debug.Log($"Total Interactions: {report.GetTotalInteractions()}");
foreach (var variantName in report.GetAllVariantNames())
{
    var successRate = report.GetSuccessRate(variantName);
    Debug.Log($"{variantName} Success Rate: {successRate:P2}");
}
```

### Export Capabilities

**JSON Export** (for analysis tools):
```csharp
var report = brainServer.GenerateABTestReport("MyTest");
report.Complete();

var json = report.ExportToJson();
File.WriteAllText("abtest_results.json", json);

// Output format:
{
  "testName": "MyTest",
  "startTime": "2026-01-13T10:30:00.000Z",
  "endTime": "2026-01-13T12:45:00.000Z",
  "durationSeconds": 8100.0,
  "totalInteractions": 1000,
  "variants": {
    "Control": {
      "selectionCount": 500,
      "successCount": 475,
      "validationFailureCount": 25,
      "fallbackCount": 5,
      "avgLatencyMs": 125.5,
      "avgTokensGenerated": 24.3,
      "successRate": 0.95
    },
    "Experimental": {
      "selectionCount": 500,
      "successCount": 480,
      "validationFailureCount": 20,
      "fallbackCount": 3,
      "avgLatencyMs": 130.2,
      "avgTokensGenerated": 25.1,
      "successRate": 0.96
    }
  }
}
```

**CSV Export** (for spreadsheets):
```csharp
var csv = report.ExportToCsv();
File.WriteAllText("abtest_results.csv", csv);

// Output format:
VariantName,SelectionCount,SuccessCount,ValidationFailureCount,FallbackCount,AvgLatencyMs,AvgTokensGenerated,SuccessRate
Control,500,475,25,5,125.50,24.30,0.9500
Experimental,500,480,20,3,130.20,25.10,0.9600
```

**Human-Readable Summary**:
```csharp
var summary = report.GetSummary();
Debug.Log(summary);

// Output:
A/B Test Report: WizardPersonalityTest
Started: 2026-01-13 10:30:00 UTC
Ended: 2026-01-13 12:45:00 UTC
Duration: 8100.0 seconds
Total Interactions: 1000

Variant Performance:
  Control:
    Selections: 500
    Success Rate: 95.00%
    Avg Latency: 125.50ms
    Avg Tokens: 24.30
    Validation Failures: 25
    Fallbacks: 5
  Experimental:
    Selections: 500
    Success Rate: 96.00%
    Avg Latency: 130.20ms
    Avg Tokens: 25.10
    Validation Failures: 20
    Fallbacks: 3
```

---

## Performance Characteristics

**Measured Performance** (from test suite):

| Operation | Target | Actual | Status |
|-----------|--------|--------|--------|
| Config Validation | < 10ms | < 1ms | ✅ Exceeds |
| Variant Selection | < 1ms | ~0.001ms | ✅ Exceeds |
| Metrics Recording | < 0.01ms | < 0.001ms | ✅ Exceeds |
| JSON Export (5 variants) | < 50ms | ~20ms | ✅ Exceeds |
| CSV Export (5 variants) | < 50ms | ~15ms | ✅ Exceeds |
| Metrics Aggregation (10 variants) | < 10ms | < 5ms | ✅ Exceeds |

**Scalability Verified:**
- ✅ 10,000 selections: deterministic, stable performance
- ✅ 100,000 metrics recordings: accurate tracking
- ✅ 200,000 total interactions (20 variants × 10,000): handles well
- ✅ Concurrent operations: thread-safe

**Memory Overhead:**
- Variant Manager: ~1KB per agent (negligible)
- Metrics Tracking: ~200 bytes per variant (negligible)
- Hot Reload: Zero overhead when disabled

---

## Best Practices

### Hot Reload

**DO:**
- ✅ Test config changes in Editor before committing
- ✅ Subscribe to `OnPersonaConfigReloaded` for custom logic
- ✅ Monitor hot reload statistics via menu
- ✅ Use validation to catch errors early

**DON'T:**
- ❌ Rely on hot reload in production builds (Editor-only by default)
- ❌ Make breaking changes to PersonaId (breaks memory lookups)
- ❌ Expect server-level changes to apply without restart

### A/B Testing

**DO:**
- ✅ Start with small traffic splits (10/90) for new variants
- ✅ Run tests long enough for statistical significance (1000+ interactions)
- ✅ Compare success rates, latency, and token usage
- ✅ Use descriptive variant names ("FriendlyWizard", "MysteriousWizard")
- ✅ Export and analyze results regularly

**DON'T:**
- ❌ Change traffic percentages mid-test (invalidates results)
- ❌ Compare variants with < 100 selections each (insufficient data)
- ❌ Forget to finalize reports before exporting
- ❌ Test too many variants simultaneously (splits traffic too thin)

**Sample Size Guidelines:**
- Minimum: 100 selections per variant
- Recommended: 500+ selections per variant
- Statistical significance: 1000+ selections per variant

**Variant Naming:**
- Good: "Control", "Experimental", "FriendlyTone", "FormalTone"
- Bad: "V1", "V2", "Test", "New" (not descriptive)

---

## Troubleshooting

### Hot Reload Not Working

**Symptom**: Config changes don't apply in play mode

**Solutions:**
1. Check if hot reload is enabled: `LlamaBrain/Hot Reload/Show Statistics`
2. Verify changes are saved to asset: `Ctrl+S` in Unity
3. Check console for validation errors
4. Manually trigger: `agent.ReloadPersonaConfig()`

**Debug:**
```csharp
// Enable verbose logging
Debug.Log($"Hot Reload Enabled: {ConfigHotReloadManager.IsEnabled}");
Debug.Log($"Total Reloads: {ConfigHotReloadManager.TotalReloads}");
Debug.Log($"Failed Reloads: {ConfigHotReloadManager.FailedReloads}");
```

### Variant Selection Not Deterministic

**Symptom**: Same InteractionCount produces different variants

**Causes:**
1. PersonaId changing between selections
2. Variants modified between selections
3. Traffic percentages don't sum to 100%

**Solutions:**
```csharp
// Verify determinism
var count = agent.InteractionCount;
var variant1 = agent.SelectSystemPromptVariant();
agent.TestSetInteractionCount(count); // Reset
var variant2 = agent.SelectSystemPromptVariant();
Debug.Assert(variant1 == variant2, "Selection should be deterministic");
```

### Variant Traffic Not Matching Percentages

**Symptom**: 50/50 split producing 60/40 distribution

**Causes:**
1. Insufficient sample size (< 100 selections)
2. Hash distribution bias (rare with `HashCode.Combine`)

**Solutions:**
- Increase sample size to 1000+ selections
- Verify with stress test: `HotReloadStressTests.VariantSelection_ThousandsOfSelections_Deterministic`

### Metrics Export Fails

**Symptom**: Export throws exception or produces empty output

**Solutions:**
1. Call `report.Complete()` before exporting
2. Ensure at least one variant has selections
3. Check file write permissions

```csharp
// Safe export
try
{
    var report = brainServer.GenerateABTestReport("Test");
    report.Complete();

    if (report.GetTotalInteractions() == 0)
    {
        Debug.LogWarning("No interactions recorded for report");
        return;
    }

    var json = report.ExportToJson();
    File.WriteAllText("report.json", json);
}
catch (Exception ex)
{
    Debug.LogError($"Export failed: {ex.Message}");
}
```

---

## See Also

- [ARCHITECTURE.md](ARCHITECTURE.md) - Component 10: Configuration Hot Reload & A/B Testing
- [USAGE_GUIDE.md](USAGE_GUIDE.md) - Hot reload workflow examples
- [ROADMAP.md](ROADMAP.md) - Feature 29 implementation status

---

**Last Updated**: January 13, 2026
**Version**: 0.3.0
