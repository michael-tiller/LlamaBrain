# Memory Management Sample

This sample demonstrates the advanced memory management features of LlamaBrain for Unity, showing how to work with AI agent memories, conversation history, and memory categorization.

## 🧠 Features Demonstrated

### Memory Management
- **Get Memories**: Retrieve all stored memories as a list
- **Get Memories by Category**: Filter memories by specific categories
- **Clear Memories**: Remove all memories for an agent
- **Add Custom Memory**: Manually add memory entries
- **Add Categorized Memory**: Add memories with specific categories
- **Memory Count**: Track the number of stored memories
- **Memory Status**: Check if agent has any memories
- **Memory Categorization**: Organize memories by type (PlayerInfo, Preferences, WorldInfo, etc.)

### Conversation History
- **Get Conversation History**: Retrieve full conversation history
- **Get Recent History**: Get specific number of recent dialogue entries
- **Clear History**: Remove all conversation history
- **History Count**: Track conversation history length
- **History Status**: Check if agent has conversation history

### UI Features
- **TextMeshPro Integration**: Modern UI with TMP components
- **Real-time Updates**: Auto-refresh memory and history displays
- **Interactive Controls**: Buttons to clear memories and history
- **Custom Memory Input**: Add custom memory entries with category support
- **Memory Category Dropdown**: Filter memories by category
- **Recent History Viewer**: View recent conversation entries
- **Configurable Settings**: Adjust update intervals and display options
- **Initialization Monitoring**: Real-time agent status tracking
- **Debug Tools**: Context menu tools for development

## 🎮 How to Use

### Setup
1. Open the `MemoryManagement.unity` scene
2. Configure the BrainServer with your llama.cpp settings
3. Assign a PersonaConfig to the LlamaBrainAgent
4. Set up the UI references in the MemoryManagerUI component
5. Ensure TextMeshPro is installed for UI components

### Basic Usage
1. **Start a Conversation**: Use the dialogue system to chat with the AI
2. **View Memories**: Watch the memory count and categorized list update automatically
3. **Add Custom Memories**: Use the input field to add specific memories (with category support)
4. **Filter Memories**: Use the category dropdown to filter memories by type
5. **Clear Data**: Use the clear buttons to reset memories or history
6. **View Recent History**: Use the "Show Recent" button to see recent conversations
7. **Monitor Status**: Watch the initialization status and connection information

### Advanced Usage
- **Memory Persistence**: Memories are stored in-memory during the session
- **Memory Categorization**: Organize memories by type using the category system
- **History Management**: Conversation history is maintained separately from memories
- **Custom Memory Types**: Add different types of information as memories with categories
- **Memory Analysis**: Use the memory count, categories, and content for debugging
- **Agent Monitoring**: Track agent initialization and connection status in real-time
- **Debug Tools**: Use context menu tools for troubleshooting and development

## 🔧 Implementation Details

### LlamaBrainAgent Extensions     
The sample adds these methods to LlamaBrainAgent:

```csharp
// Memory Management
public IReadOnlyList<string> GetMemories()
public IReadOnlyList<string> GetMemoriesByCategory(string category)
public void ClearMemories()
public void AddSpecificMemory(string category, string content)
public int MemoryCount { get; }
public bool HasMemories { get; }

// Conversation History
public IReadOnlyList<string> GetConversationHistory()
public IReadOnlyList<DialogueEntry> GetRecentHistory(int count)
public int ConversationHistoryCount { get; }
public bool HasConversationHistory { get; }

// Agent Status
public bool IsInitialized { get; }
public string ConnectionStatus { get; }
public void DebugAgentState()
public string GetSetupInstructions()
```

### MemoryManagerUI Component
An advanced UI component that provides:
- TextMeshPro integration for modern UI
- Real-time memory and history display with categorization
- Interactive controls for memory management
- Memory filtering by categories with dropdown
- Agent initialization monitoring and status display
- Configurable update intervals with initialization checks
- Error handling and validation
- Debug tools via context menu
- Async initialization waiting

## 📊 Memory vs History

### Memories
- **Purpose**: Long-term information the AI should remember
- **Content**: Important facts, preferences, relationships, categorized by type
- **Persistence**: Stored separately from conversation
- **Usage**: Included in AI prompts for context
- **Categories**: PlayerInfo, Preferences, WorldInfo, WorldEvents, Relationships, Quests, Custom

### Conversation History
- **Purpose**: Recent dialogue for conversation flow
- **Content**: Recent exchanges between player and AI
- **Persistence**: Part of the dialogue session
- **Usage**: Provides conversation context

## 🎯 Best Practices

### Memory Management
- **Keep memories concise**: Short, important facts work best
- **Use meaningful content**: Add information the AI should remember
- **Use categories**: Organize memories by type for better management
- **Regular cleanup**: Clear old memories when no longer relevant
- **Memory limits**: The system automatically limits to 32 memories
- **Category format**: Use "Category: Content" format when adding custom memories

### Conversation History
- **Monitor history length**: Long histories can impact performance
- **Clear when needed**: Reset history for new conversation topics
- **Use recent history**: Focus on recent exchanges for context
- **History analysis**: Use history for debugging and improvement

### UI Design
- **Real-time updates**: Keep displays current with auto-refresh
- **Clear controls**: Make it obvious what each button does
- **Status indicators**: Show when data is available and agent status
- **Error handling**: Provide feedback for operations
- **TextMeshPro**: Use modern UI components for better appearance
- **Initialization monitoring**: Show agent connection status
- **Debug tools**: Provide context menu tools for development

## 🔍 Debugging

### Common Issues
- **No memories showing**: Check if memory is enabled in PersonaConfig
- **UI not updating**: Verify auto-update is enabled and agent is initialized
- **Agent not initialized**: Check BrainServer configuration and connection
- **Memory not persisting**: Memories are session-only (not file-based)
- **History too long**: Use clear functions to reset when needed
- **Category filtering not working**: Ensure memory category manager is configured
- **TextMeshPro errors**: Verify TMP components are properly assigned

### Debug Information
- **Console logs**: Check for memory and history operations
- **UI counters**: Monitor memory and history counts
- **Content display**: Review actual memory and history content
- **Performance**: Watch for memory usage with large histories
- **Agent status**: Monitor initialization and connection status
- **Context menu tools**: Use "Debug UI State" and "Force Find Agent" tools
- **Category information**: Check memory categorization and filtering

## 🚀 Next Steps

### Enhancements
- **Persistent Storage**: Implement file-based memory persistence
- **Advanced Categories**: Add more memory categories and subcategories
- **Memory Search**: Add search functionality for memories
- **Memory Export**: Save memories to external files
- **Memory Import**: Load memories from external sources
- **Memory Analytics**: Add memory usage statistics and insights
- **Memory Prioritization**: Implement memory importance scoring

### Integration
- **Game Events**: Trigger memory updates from game events
- **Player Actions**: Add memories based on player behavior
- **NPC Interactions**: Store information about other NPCs
- **World State**: Remember important world events
- **Quest System**: Integrate with quest progress and completion
- **Dialogue System**: Automatic memory creation from conversations
- **Save System**: Integrate with game save/load functionality

## 🆕 New Features in This Version

### TextMeshPro Integration
- **Modern UI**: All text components now use TextMeshPro for better appearance
- **Better Typography**: Improved text rendering and font support
- **Rich Text**: Support for rich text formatting in memory displays
- **Scalable UI**: Better UI scaling across different resolutions

### Memory Categorization
- **Category System**: Organize memories by type (PlayerInfo, Preferences, WorldInfo, etc.)
- **Category Dropdown**: Filter memories by category using a dropdown menu
- **Categorized Input**: Add memories with specific categories using "Category: Content" format
- **Category Display**: Visual category indicators in memory lists

### Agent Monitoring
- **Initialization Tracking**: Real-time monitoring of agent initialization status
- **Connection Status**: Display current connection status and issues
- **Async Initialization**: Automatic waiting for agent to be ready
- **Status Display**: Show setup instructions and troubleshooting info

### Debug Tools
- **Context Menu Tools**: Right-click context menu for debugging
- **Debug UI State**: Comprehensive state information display
- **Force Find Agent**: Manual agent discovery tool
- **Agent State Debug**: Detailed agent status information

## 📋 Requirements

### Unity Requirements
- **Unity 2021.3 or later**: For TextMeshPro integration
- **TextMeshPro Package**: Must be installed via Package Manager
- **Cysharp.Threading.Tasks**: For async operations (included with package)

### Setup Requirements
- **BrainServer**: Properly configured with llama.cpp
- **PersonaConfig**: Assigned to LlamaBrainAgent
- **Memory Category Manager**: Optional but recommended for categorization
- **UI References**: All TMP components properly assigned

## 📚 Related Documentation
- [Main README](../LLAMABRAIN.md) - General LlamaBrain for Unity documentation
- [SAMPLES.md](../SAMPLES.md) - Overview of all samples
- [Troubleshooting](../../TROUBLESHOOTING.md) - Common issues and solutions 