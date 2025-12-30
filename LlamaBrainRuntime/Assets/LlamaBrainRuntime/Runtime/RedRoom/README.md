# RedRoom - In-Game LLM Testing Suite

RedRoom is a comprehensive Unity testing framework for evaluating LLM-powered NPC dialogue systems in real-time gameplay scenarios. It provides a complete environment for testing multiple conversational scenarios, collecting performance metrics, and analyzing LLM response quality.

## Overview

RedRoom enables you to:
- **Test LLM dialogue** with NPCs in a simulated game environment
- **Create multiple test scenarios** using trigger zones with different conversational seeds
- **Collect detailed metrics** on response times, token usage, and performance
- **Export data** for analysis in CSV and JSON formats
- **Track interactions** across multiple test sessions

## System Architecture

### Core Components

#### 1. **NPC System**
- **`NpcFollowerExample`**: NPC that follows the player and uses LLM for dialogue generation
- **`NpcAgentExample`**: Wraps `LlamaBrainAgent` to provide dialogue capabilities
- NPCs automatically generate dialogue when entering trigger zones

#### 2. **Interaction System**
- **`NpcDialogueTrigger`**: Defines trigger zones with unique prompts/conversational seeds
  - Each trigger has its own `promptText` that seeds the conversation
  - Tracks `PromptCount` (how many times player interacted)
  - Automatically generates dialogue when NPC enters the zone
- **`NpcTriggerCollider`**: Collider component that detects when NPCs enter/exit triggers

#### 3. **Player Interaction**
- **`RedRoomPlayerRaycast`**: Raycasts from camera to detect NPCs
  - Configured via LayerMask (typically "Npc" layer)
  - Fires events when NPCs are detected
- **`RedRoomCanvas`**: Manages UI and player input
  - Shows visual indicator when dialogue is ready
  - Handles E key press to open/close dialogue panel
  - Displays generated dialogue text

#### 4. **Metrics Collection**
- **`DialogueMetricsCollector`**: Singleton component that collects all interaction data
  - Automatically records metrics when dialogue is generated
  - Exports to CSV and JSON formats
  - Provides session summaries and statistics
- **`DialogueMetrics`**: Data structures for storing interaction data

## Setup Instructions

### 1. Scene Setup

#### Create the NPC
1. Create a GameObject for your NPC
2. Add components:
   - `NpcFollowerExample` (handles following behavior)
   - `NpcAgentExample` (LLM dialogue wrapper)
   - `LlamaBrainAgent` (core LLM agent)
   - `NpcTriggerCollider` (for trigger detection)
   - `CapsuleCollider` (for raycast detection)
   - `CharacterController` or `Rigidbody` (for movement)
3. Set the GameObject's **Layer** to **"Npc"**
4. Configure `NpcAgentExample`:
   - Assign a `BrainSettings` asset
   - The agent will auto-initialize on Start

#### Create Dialogue Triggers
1. Create empty GameObjects for each trigger zone
2. Add `NpcDialogueTrigger` component to each
3. Configure each trigger:
   - **Prompt Text**: The conversational seed/prompt for this trigger
   - **Fallback Text**: Optional fallback if LLM fails
   - Each trigger should have a unique prompt to test different scenarios
4. Add a Collider (BoxCollider, SphereCollider, etc.) set as a **Trigger**
5. Add `NpcTriggerCollider` component to the NPC (not the trigger zone)

#### Setup Player Interaction
1. Create a GameObject for the player camera
2. Add `RedRoomPlayerRaycast` component:
   - Set **Layer Mask** to "Npc" layer
   - Set **Raycast Distance** (default: 100)
   - Wire up events in Inspector:
     - `OnRaycastHit` → `RedRoomCanvas.OnRaycastHit`
     - `OnRaycastMiss` → `RedRoomCanvas.OnRaycastMiss`
3. Create a Canvas GameObject
4. Add `RedRoomCanvas` component
5. Configure UI references:
   - **Conversation Panel**: GameObject that shows/hides dialogue
   - **Text**: TextMeshProUGUI component for dialogue text
   - **Player Raycast Hit Indicator**: GameObject that shows when dialogue is ready

#### Setup Metrics Collection
1. Create a GameObject (or use existing manager)
2. Add `DialogueMetricsCollector` component
3. Configure:
   - **Auto Export On Quit**: Enable to export when application closes
   - **Export Directory**: Folder name for exports (default: "DialogueMetrics")
   - **Enable Rolling Files**: Enable automatic file rollover (recommended)
   - **Max Interactions Per File**: Rollover after N interactions (0 = disabled, default: 1000)
   - **Max File Size MB**: Rollover when file exceeds size (0 = disabled, default: 10 MB)
   - **Max Time Per File Minutes**: Rollover after N minutes (0 = disabled, default: 60)
   - **Max Files To Keep**: Delete oldest files when exceeded (0 = keep all, default: 50)
4. The component persists across scenes automatically

### 2. Layer Configuration

Ensure your NPCs are on the **"Npc"** layer:
1. Edit → Project Settings → Tags and Layers
2. Create/assign "Npc" layer
3. Set NPC GameObjects to this layer
4. Configure `RedRoomPlayerRaycast` LayerMask to include "Npc"

### 3. Event Wiring

In the Unity Inspector, wire up the events:

**RedRoomPlayerRaycast:**
- `OnRaycastHit` → Drag `RedRoomCanvas` → Select `OnRaycastHit(RaycastHit)`
- `OnRaycastMiss` → Drag `RedRoomCanvas` → Select `OnRaycastMiss()`

## Usage Guide

### Testing Workflow

1. **Start the game** - NPCs will initialize and start following
2. **Move NPC into trigger zone** - Dialogue generation starts automatically
3. **Look at NPC** - Visual indicator appears when dialogue is ready
4. **Press E** - Opens dialogue panel showing generated text
5. **Press E again or Escape** - Closes dialogue panel
6. **Move to different trigger** - Test different conversational seeds

### Multiple Test Scenarios

Create 3+ trigger zones with different prompts:
- **Trigger 1**: "Tell me about your adventures"
- **Trigger 2**: "What do you think about this place?"
- **Trigger 3**: "Do you have any advice for me?"

Each trigger tests a different conversational context and LLM response pattern.

### Metrics Collection

Metrics are automatically collected when dialogue is generated. Each interaction records:

**Performance Metrics:**
- TTFT (Time To First Token)
- Prefill Time
- Decode Time
- Total Time
- Tokens Per Second

**Token Metrics:**
- Prompt Token Count
- Generated Token Count
- Cached Token Count

**Quality Metrics:**
- Response Length
- Truncation Status
- Trigger Information
- Prompt Count

### Exporting Data

**Automatic Export:**
- Enabled by default on application quit
- Saves to: `Application.persistentDataPath/DialogueMetrics/`

**Rolling File System:**
The metrics collector uses a rolling file system to prevent files from growing too large:
- **Automatic Rollover**: Files automatically roll to new files when thresholds are met
- **Multiple Rollover Triggers**:
  - **Interaction Count**: Rollover after N interactions (e.g., 1000)
  - **File Size**: Rollover when file exceeds size limit (e.g., 10 MB)
  - **Time Limit**: Rollover after time period (e.g., 60 minutes)
- **File Naming**: Rolled files include part numbers: `DialogueMetrics_{SessionId}_{Timestamp}_part001.csv`
- **Automatic Cleanup**: Oldest files are automatically deleted when exceeding the file limit

**Manual Export:**
- Right-click `DialogueMetricsCollector` component → "Export Current Session"
- Or call: `DialogueMetricsCollector.Instance.ExportCurrentSession()`

**Export Formats:**
- **CSV**: For spreadsheet analysis (Excel, Google Sheets)
- **JSON**: For programmatic analysis (Python, JavaScript)

**File Naming:**
```
# Single file (no rollover)
DialogueMetrics_{SessionId}_{Timestamp}.csv
DialogueMetrics_{SessionId}_{Timestamp}.json

# Rolled files (with part numbers)
DialogueMetrics_{SessionId}_{Timestamp}_part001.csv
DialogueMetrics_{SessionId}_{Timestamp}_part002.csv
...
```

### Viewing Session Summary

Right-click `DialogueMetricsCollector` component → "Print Session Summary"

Shows:
- Total interactions
- Average response times
- Average token usage
- Tokens per second
- Truncation rate
- Per-trigger statistics

## Component Reference

### NpcDialogueTrigger

**Properties:**
- `PromptText`: The conversational seed/prompt for this trigger
- `FallbackText`: List of fallback responses if LLM fails
- `ConversationText`: The generated dialogue (read-only)
- `PromptCount`: Number of times player interacted (read-only)
- `CurrentAgent`: The NPC currently in this trigger (read-only)

**Methods:**
- `TriggerConversation(NpcAgentExample)`: Manually trigger dialogue generation
- `IncrementPromptCount()`: Increment interaction counter
- `ResetPromptCount()`: Reset counter to zero
- `GetTriggerName()`: Get the GameObject name
- `GetPromptText()`: Get the prompt text

**Events:**
- `onConversationTextGenerated`: Fires when dialogue is ready

### RedRoomCanvas

**Properties:**
- `Instance`: Singleton instance (static)

**Methods:**
- `OnRaycastHit(RaycastHit)`: Called when player looks at NPC
- `OnRaycastMiss()`: Called when player looks away
- `ContinueConversation(string)`: Display dialogue text
- `EndConversation()`: Close dialogue panel

### DialogueMetricsCollector

**Properties:**
- `Instance`: Singleton instance (static)

**Configuration:**
- `EnableRollingFiles`: Enable/disable rolling file system
- `MaxInteractionsPerFile`: Rollover threshold by interaction count (0 = disabled)
- `MaxFileSizeMB`: Rollover threshold by file size in MB (0 = disabled)
- `MaxTimePerFileMinutes`: Rollover threshold by time in minutes (0 = disabled)
- `MaxFilesToKeep`: Maximum files to retain, oldest deleted when exceeded (0 = keep all)

**Methods:**
- `StartNewSession()`: Begin a new metrics session (resets file index)
- `RecordInteraction(...)`: Record an interaction (called automatically, checks rollover)
- `ExportCurrentSession()`: Export current session to files
- `GetSessionSummary()`: Get formatted summary string

**Context Menu:**
- "Export Current Session": Export metrics to files
- "Print Session Summary": Print summary to console

**Rolling File Behavior:**
- Automatically rolls to new file when any threshold is met
- Files are numbered sequentially: `_part001`, `_part002`, etc.
- Old files are automatically cleaned up based on `MaxFilesToKeep` setting

### RedRoomPlayerRaycast

**Properties:**
- `LayerMask`: Which layers to raycast against
- `RaycastDistance`: Maximum raycast distance
- `PreviousHitObject`: Last object hit by raycast

**Events:**
- `OnRaycastHit`: Fired when NPC is detected
- `OnRaycastMiss`: Fired when raycast misses

## Data Analysis

### CSV Format

The CSV export includes all interaction data in a spreadsheet-friendly format:

```csv
InteractionId,Timestamp,TriggerId,TriggerName,PromptText,PromptCount,NpcName,
ResponseText,ResponseLength,TtftMs,PrefillTimeMs,DecodeTimeMs,TotalTimeMs,
PromptTokenCount,GeneratedTokenCount,CachedTokenCount,TokensPerSecond,WasTruncated
```

### JSON Format

The JSON export includes full session metadata:

```json
{
  "SessionId": "...",
  "SessionStart": "2024-01-15 14:30:22.123",
  "SessionEnd": "2024-01-15 15:45:10.456",
  "TotalInteractions": 42,
  "Interactions": [...]
}
```

### Analysis Tips

1. **Compare Triggers**: Group by `TriggerName` to compare different prompts
2. **Performance Trends**: Track `TokensPerSecond` over time
3. **Truncation Rate**: Count `WasTruncated=true` to identify token limit issues
4. **Response Quality**: Analyze `ResponseLength` and `GeneratedTokenCount`
5. **Cache Efficiency**: Compare `CachedTokenCount` vs `PromptTokenCount`

## Troubleshooting

### Indicator Not Showing

**Problem**: Visual indicator doesn't appear when looking at NPC

**Solutions:**
1. Check NPC is on "Npc" layer
2. Verify `RedRoomPlayerRaycast` LayerMask includes "Npc"
3. Ensure events are wired correctly
4. Check dialogue has been generated (look for logs)
5. Verify `RedRoomCanvas` has indicator GameObject assigned

### Dialogue Not Generating

**Problem**: No dialogue appears when NPC enters trigger

**Solutions:**
1. Check `NpcAgentExample` is initialized (check logs)
2. Verify `BrainSettings` is assigned
3. Ensure trigger has `promptText` set
4. Check NPC has `NpcTriggerCollider` component
5. Verify trigger collider is set as "Is Trigger"

### Metrics Not Recording

**Problem**: No metrics in exports

**Solutions:**
1. Ensure `DialogueMetricsCollector` component exists in scene
2. Check `NpcDialogueTrigger` is calling metrics collection
3. Verify `LlamaBrainAgent.LastMetrics` is being set
4. Check console for error messages

### Raycast Not Working

**Problem**: Can't detect NPCs with mouse

**Solutions:**
1. Verify camera has `RedRoomPlayerRaycast` component
2. Check LayerMask configuration
3. Ensure NPC has collider (not just trigger collider)
4. Check raycast distance is sufficient
5. Verify NPC is on correct layer

## Best Practices

1. **Multiple Triggers**: Create 3+ triggers with distinct prompts for comprehensive testing
2. **Consistent Setup**: Use prefabs for NPCs and triggers to ensure consistency
3. **Layer Organization**: Keep NPCs on dedicated "Npc" layer
4. **Regular Exports**: Export metrics regularly during testing sessions
5. **Session Management**: Start new sessions for different test scenarios
6. **Fallback Text**: Always provide fallback text for reliability
7. **Performance Monitoring**: Watch for low tokens/second (indicates performance issues)

## Example Scene Setup

```
Scene
├── Player
│   └── Main Camera (RedRoomPlayerRaycast)
├── NPC
│   ├── NpcFollowerExample
│   ├── NpcAgentExample
│   ├── LlamaBrainAgent
│   ├── NpcTriggerCollider
│   └── CapsuleCollider (Layer: Npc)
├── TriggerZone1 (NpcDialogueTrigger + Collider)
├── TriggerZone2 (NpcDialogueTrigger + Collider)
├── TriggerZone3 (NpcDialogueTrigger + Collider)
├── Canvas (RedRoomCanvas)
│   ├── ConversationPanel
│   ├── DialogueText (TextMeshProUGUI)
│   └── Indicator (GameObject)
└── MetricsManager (DialogueMetricsCollector)
```

## Advanced Usage

### Custom Metrics

Extend `DialogueInteraction` class to add custom metrics fields, then update `DialogueMetricsCollector` to record them.

### Multiple NPCs

The system supports multiple NPCs. Each NPC can interact with any trigger, and metrics are tracked per interaction.

### Session Management

Call `DialogueMetricsCollector.Instance.StartNewSession()` to begin a new test session. Previous session is automatically exported.

### Programmatic Access

Access metrics programmatically:
```csharp
var collector = DialogueMetricsCollector.Instance;
var summary = collector.GetSessionSummary();
var session = collector.currentSession; // Access raw data
```

## Support

For issues or questions:
1. Check console logs for detailed debug information
2. Review component Inspector settings
3. Verify all events are properly wired
4. Check that all required components are present

---

**RedRoom** - Comprehensive LLM Testing for Unity Games

