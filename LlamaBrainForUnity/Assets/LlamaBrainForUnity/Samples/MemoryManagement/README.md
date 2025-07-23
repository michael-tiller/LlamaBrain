# Memory Management Sample

This sample demonstrates the memory management features of LlamaBrain for Unity, showing how to work with AI agent memories and conversation history.

## 🧠 Features Demonstrated

### Memory Management
- **Get Memories**: Retrieve all stored memories as a list
- **Clear Memories**: Remove all memories for an agent
- **Add Custom Memory**: Manually add memory entries
- **Memory Count**: Track the number of stored memories
- **Memory Status**: Check if agent has any memories

### Conversation History
- **Get Conversation History**: Retrieve full conversation history
- **Get Recent History**: Get specific number of recent dialogue entries
- **Clear History**: Remove all conversation history
- **History Count**: Track conversation history length
- **History Status**: Check if agent has conversation history

### UI Features
- **Real-time Updates**: Auto-refresh memory and history displays
- **Interactive Controls**: Buttons to clear memories and history
- **Custom Memory Input**: Add custom memory entries
- **Recent History Viewer**: View recent conversation entries
- **Configurable Settings**: Adjust update intervals and display options

## 🎮 How to Use

### Setup
1. Open the `MemoryManagement.unity` scene
2. Configure the BrainServer with your llama.cpp settings
3. Assign a PersonaConfig to the UnityBrainAgent
4. Set up the UI references in the MemoryManagerUI component

### Basic Usage
1. **Start a Conversation**: Use the dialogue system to chat with the AI
2. **View Memories**: Watch the memory count and list update automatically
3. **Add Custom Memories**: Use the input field to add specific memories
4. **Clear Data**: Use the clear buttons to reset memories or history
5. **View Recent History**: Use the "Show Recent" button to see recent conversations

### Advanced Usage
- **Memory Persistence**: Memories are stored in-memory during the session
- **History Management**: Conversation history is maintained separately from memories
- **Custom Memory Types**: Add different types of information as memories
- **Memory Analysis**: Use the memory count and content for debugging

## 🔧 Implementation Details

### UnityBrainAgent Extensions
The sample adds these methods to UnityBrainAgent:

```csharp
// Memory Management
public IReadOnlyList<string> GetMemories()
public void ClearMemories()
public int MemoryCount { get; }
public bool HasMemories { get; }

// Conversation History
public IReadOnlyList<string> GetConversationHistory()
public IReadOnlyList<DialogueEntry> GetRecentHistory(int count)
public int ConversationHistoryCount { get; }
public bool HasConversationHistory { get; }
```

### MemoryManagerUI Component
A complete UI component that provides:
- Real-time memory and history display
- Interactive controls for memory management
- Configurable update intervals
- Error handling and validation

## 📊 Memory vs History

### Memories
- **Purpose**: Long-term information the AI should remember
- **Content**: Important facts, preferences, relationships
- **Persistence**: Stored separately from conversation
- **Usage**: Included in AI prompts for context

### Conversation History
- **Purpose**: Recent dialogue for conversation flow
- **Content**: Recent exchanges between player and AI
- **Persistence**: Part of the dialogue session
- **Usage**: Provides conversation context

## 🎯 Best Practices

### Memory Management
- **Keep memories concise**: Short, important facts work best
- **Use meaningful content**: Add information the AI should remember
- **Regular cleanup**: Clear old memories when no longer relevant
- **Memory limits**: The system automatically limits to 32 memories

### Conversation History
- **Monitor history length**: Long histories can impact performance
- **Clear when needed**: Reset history for new conversation topics
- **Use recent history**: Focus on recent exchanges for context
- **History analysis**: Use history for debugging and improvement

### UI Design
- **Real-time updates**: Keep displays current with auto-refresh
- **Clear controls**: Make it obvious what each button does
- **Status indicators**: Show when data is available
- **Error handling**: Provide feedback for operations

## 🔍 Debugging

### Common Issues
- **No memories showing**: Check if memory is enabled in PersonaConfig
- **UI not updating**: Verify auto-update is enabled and interval is set
- **Memory not persisting**: Memories are session-only (not file-based)
- **History too long**: Use clear functions to reset when needed

### Debug Information
- **Console logs**: Check for memory and history operations
- **UI counters**: Monitor memory and history counts
- **Content display**: Review actual memory and history content
- **Performance**: Watch for memory usage with large histories

## 🚀 Next Steps

### Enhancements
- **Persistent Storage**: Implement file-based memory persistence
- **Memory Categories**: Organize memories by type or importance
- **Memory Search**: Add search functionality for memories
- **Memory Export**: Save memories to external files
- **Memory Import**: Load memories from external sources

### Integration
- **Game Events**: Trigger memory updates from game events
- **Player Actions**: Add memories based on player behavior
- **NPC Interactions**: Store information about other NPCs
- **World State**: Remember important world events

## 📚 Related Documentation
- [Main README](../README.md) - General LlamaBrain for Unity documentation
- [SAMPLES.md](../SAMPLES.md) - Overview of all samples
- [API Documentation](../../Documenation/api/) - Detailed API reference
- [Troubleshooting](../../TROUBLESHOOTING.md) - Common issues and solutions 