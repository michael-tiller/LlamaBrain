# Default Memory Categories

This directory contains default memory categories for LlamaBrain for Unity that can be used as shared settings across different projects and samples.

## 📁 Files

### Memory Category Manager
- **Default Memory Categories.asset** - A complete MemoryCategoryManager with all default categories pre-configured

### Individual Memory Categories
- **PlayerInfo.asset** - Player character information and background
- **Preferences.asset** - Player likes, dislikes, and choices
- **WorldInfo.asset** - World locations and environment information
- **WorldEvents.asset** - Important world events and happenings
- **Relationships.asset** - NPC relationships and character connections
- **Quests.asset** - Quest information and objectives
- **Custom.asset** - Custom memory entries

## 🎯 Category Details

### PlayerInfo
- **Color**: Blue (#3366FF)
- **Priority**: 10 (Highest)
- **Importance**: 0.9
- **Max Entries**: 15
- **Auto Extract**: Yes
- **Keywords**: player, character, name, background, origin, family, history
- **Patterns**: Extracts player names, character backgrounds, and personal details

### Preferences
- **Color**: Yellow (FFCC33)
- **Priority**: 8
- **Importance**: 0.7
- **Max Entries**: 12
- **Auto Extract**: Yes
- **Keywords**: like, dislike, prefer, favorite, hate, enjoy, want, choose
- **Patterns**: Extracts player preferences, likes, and dislikes

### WorldInfo
- **Color**: Green (#4DCC4D)
- **Priority**: 6
- **Importance**: 0.6
- **Max Entries**: 20
- **Auto Extract**: Yes
- **Keywords**: world, location, place, city, town, village, area, region, environment
- **Patterns**: Extracts world locations, place names, and environmental information

### WorldEvents
- **Color**: Red (CC4D4D)
- **Priority**: 7
- **Importance**: 0.8
- **Max Entries**: 15
- **Auto Extract**: Yes
- **Keywords**: event, happened, occurred, incident, attack, celebration, festival, disaster, war, peace
- **Patterns**: Extracts world events, incidents, and important happenings

### Relationships
- **Color**: Purple (CC33CC)
- **Priority**: 9
- **Importance**: 0.8
- **Max Entries**: 12
- **Auto Extract**: Yes
- **Keywords**: friend, enemy, ally, relationship, family, companion, partner, rival, lover
- **Patterns**: Extracts relationship information with NPCs and other characters

### Quests
- **Color**: Orange (FF9900)
- **Priority**: 5
- **Importance**: 0.9
- **Max Entries**: 10
- **Auto Extract**: Yes
- **Keywords**: quest, mission, objective, task, goal, assignment, challenge
- **Patterns**: Extracts quest information, objectives, and progress

### Custom
- **Color**: Gray (#999999)
- **Priority**: 1 (Lowest)
- **Importance**: 0.5
- **Max Entries**: 8
- **Auto Extract**: No (Manual only)
- **Keywords**: None
- **Patterns**: None
- **Purpose**: For manually added memories that don't fit other categories

## 🚀 Usage

### Using the Complete Manager
1. Assign `Default Memory Categories.asset` to your MemoryCategoryManager field
2. All categories will be automatically available for memory extraction and organization

### Using Individual Categories
1. Create your own MemoryCategoryManager
2. Drag individual category assets into the manager's memoryCategories list
3. Customize the categories as needed for your project

### Integration with LlamaBrainAgent
The memory categories will automatically be used by the LlamaBrainAgent for:
- Automatic memory extraction from conversations
- Memory organization and filtering
- Memory persistence across sessions
- Memory importance scoring

## 🔧 Customization

### Modifying Categories
- Open any category asset in the Unity Inspector
- Adjust colors, priorities, importance, and max entries
- Add or remove trigger keywords and extraction patterns
- Enable/disable auto-extraction as needed

### Creating New Categories
1. Right-click in the Project window
2. Select Create > LlamaBrain > Memory Category
3. Configure the new category with appropriate settings
4. Add it to your MemoryCategoryManager

### Priority System
Categories are processed in priority order (highest first):
- PlayerInfo (10) - Most important, processed first
- Relationships (9) - High priority for character interactions
- Preferences (8) - Important for personalization
- WorldEvents (7) - Significant world happenings
- WorldInfo (6) - General world information
- Quests (5) - Quest-related information
- Custom (1) - Manual entries only

## 📊 Memory Limits

Each category has a maximum entry limit to prevent memory overflow:
- PlayerInfo: 15 entries
- Preferences: 12 entries
- WorldInfo: 20 entries (largest for world details)
- WorldEvents: 15 entries
- Relationships: 12 entries
- Quests: 10 entries
- Custom: 8 entries

## 🎨 Color Coding

Each category has a distinct color for easy visual identification:
- **Blue**: Player information
- **Yellow**: Preferences and choices
- **Green**: World and location info
- **Red**: World events and incidents
- **Purple**: Relationships and connections
- **Orange**: Quests and objectives
- **Gray**: Custom entries

## 🔍 Extraction Patterns

The categories use regex patterns to automatically extract information:
- Patterns are case-insensitive
- Use capture groups `([^,]+)` to extract content
- Multiple patterns can be defined per category
- Invalid patterns are logged as warnings

## 📝 Best Practices

1. **Use the complete manager** for quick setup
2. **Customize categories** based on your game's needs
3. **Adjust priorities** based on what's most important in your game
4. **Monitor memory usage** and adjust max entries as needed
5. **Test extraction patterns** to ensure they work with your dialogue
6. **Use the Custom category** for one-off memories that don't fit elsewhere

## 🔗 Related Documentation
- [Troubleshooting](TROUBLESHOOTING.md) 