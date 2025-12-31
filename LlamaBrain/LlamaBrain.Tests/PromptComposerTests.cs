using System;
using System.Collections.Generic;
using NUnit.Framework;
using LlamaBrain.Core;
using LlamaBrain.Persona;

namespace LlamaBrain.Tests
{
  /// <summary>
  /// Tests for the PromptComposer functionality.
  /// </summary>
  public class PromptComposerTests
  {
    #region ComposeWithSettings Tests

    [Test]
    public void Compose_WithDefaultSettings_ReturnsExpectedFormat()
    {
      // Arrange
      var personaName = "TestNPC";
      var description = "A test NPC";
      var memory = new List<string> { "Memory 1", "Memory 2" };
      var dialogueHistory = new List<string> { "Player: Hello", "NPC: Hi there!" };
      var playerName = "Player";
      var playerInput = "How are you?";

      // Act
      var result = PromptComposer.ComposeWithSettings(personaName, description, memory, dialogueHistory, playerName, playerInput, null);

      // Assert
      Assert.That(result, Contains.Substring("NPC: TestNPC"));
      Assert.That(result, Contains.Substring("Description: A test NPC"));
      Assert.That(result, Contains.Substring("NPC Memory:"));
      Assert.That(result, Contains.Substring("- Memory 1"));
      Assert.That(result, Contains.Substring("- Memory 2"));
      Assert.That(result, Contains.Substring("Dialogue so far:"));
      Assert.That(result, Contains.Substring("Player: Hello"));
      Assert.That(result, Contains.Substring("NPC: Hi there!"));
      Assert.That(result, Contains.Substring("Player: Player"));
      Assert.That(result, Contains.Substring("Player says: How are you?"));
      Assert.That(result, Contains.Substring("NPC responds:"));
    }

    [Test]
    public void Compose_WithCustomSettings_ReturnsExpectedFormat()
    {
      // Arrange
      var settingsDict = new Dictionary<string, object>
      {
        ["npcTemplate"] = "NPC: {personaName}",
        ["descriptionTemplate"] = "{description}",
        ["memoryHeaderTemplate"] = "Memory:",
        ["memoryItemTemplate"] = "- {memory}",
        ["dialogueHeaderTemplate"] = "History:",
        ["playerTemplate"] = "Player: {playerName}",
        ["playerInputTemplate"] = "> {playerInput}",
        ["responsePromptTemplate"] = "{personaName}:",
        ["includeEmptyMemory"] = false,
        ["includeEmptyDialogue"] = false,
        ["maxMemoryItems"] = 10,
        ["maxDialogueLines"] = 10,
        ["sectionSeparator"] = "\n",
        ["compactDialogueFormat"] = true,
        ["dialogueLinePrefix"] = "> ",
        ["playerName"] = "Adventurer"
      };

      var personaName = "TestNPC";
      var description = "A test NPC";
      var memory = new List<string> { "Memory 1" };
      var dialogueHistory = new List<string> { "Player: Hello", "NPC: Hi!" };
      var playerName = "Player";
      var playerInput = "How are you?";

      // Act
      var result = PromptComposer.ComposeWithSettings(personaName, description, memory, dialogueHistory, playerName, playerInput, settingsDict);

      // Assert
      Assert.That(result, Contains.Substring("NPC: TestNPC"));
      Assert.That(result, Contains.Substring("A test NPC")); // Compact format doesn't have "Description:" prefix
      Assert.That(result, Contains.Substring("Memory:"));
      Assert.That(result, Contains.Substring("History:"));
      Assert.That(result, Contains.Substring("> How are you?")); // Compact format uses ">" prefix
      Assert.That(result, Contains.Substring("TestNPC:")); // Compact format uses persona name with colon
    }

    [Test]
    public void Compose_WithDetailedSettings_ReturnsExpectedFormat()
    {
      // Arrange
      var settingsDict = new Dictionary<string, object>
      {
        ["npcTemplate"] = "Character: {personaName}",
        ["descriptionTemplate"] = "Background: {description}",
        ["memoryHeaderTemplate"] = "Character Memory (Recent Events):",
        ["memoryItemTemplate"] = "• {memory}",
        ["dialogueHeaderTemplate"] = "Conversation History:",
        ["playerTemplate"] = "Player Character: {playerName}",
        ["playerInputTemplate"] = "Player says: \"{playerInput}\"",
        ["responsePromptTemplate"] = "How does {personaName} respond?",
        ["includeEmptyMemory"] = false,
        ["includeEmptyDialogue"] = false,
        ["maxMemoryItems"] = 10,
        ["maxDialogueLines"] = 10,
        ["sectionSeparator"] = "\n",
        ["compactDialogueFormat"] = false,
        ["dialogueLinePrefix"] = "",
        ["playerName"] = "Adventurer"
      };

      var personaName = "TestNPC";
      var description = "A test NPC";
      var memory = new List<string> { "Memory 1" };
      var dialogueHistory = new List<string> { "Player: Hello", "NPC: Hi!" };
      var playerName = "Player";
      var playerInput = "How are you?";

      // Act
      var result = PromptComposer.ComposeWithSettings(personaName, description, memory, dialogueHistory, playerName, playerInput, settingsDict);

      // Assert
      Assert.That(result, Contains.Substring("Character: TestNPC"));
      Assert.That(result, Contains.Substring("Background: A test NPC"));
      Assert.That(result, Contains.Substring("Character Memory (Recent Events):"));
      Assert.That(result, Contains.Substring("• Memory 1"));
      Assert.That(result, Contains.Substring("Conversation History:"));
      Assert.That(result, Contains.Substring("Player Character: Adventurer")); // Should use player name from settings
      Assert.That(result, Contains.Substring("Player says: \"How are you?\""));
      Assert.That(result, Contains.Substring("How does TestNPC respond?"));
    }

    [Test]
    public void Compose_WithEmptyMemoryAndDialogue_RespectsIncludeEmptySettings()
    {
      // Arrange
      var settingsDict = new Dictionary<string, object>
      {
        ["npcTemplate"] = "Character: {personaName}",
        ["descriptionTemplate"] = "Background: {description}",
        ["memoryHeaderTemplate"] = "Character Memory (Recent Events):",
        ["memoryItemTemplate"] = "• {memory}",
        ["dialogueHeaderTemplate"] = "Conversation History:",
        ["playerTemplate"] = "Player Character: {playerName}",
        ["playerInputTemplate"] = "Player says: \"{playerInput}\"",
        ["responsePromptTemplate"] = "How does {personaName} respond?",
        ["includeEmptyMemory"] = true,
        ["includeEmptyDialogue"] = true,
        ["maxMemoryItems"] = 10,
        ["maxDialogueLines"] = 10,
        ["sectionSeparator"] = "\n",
        ["compactDialogueFormat"] = false,
        ["dialogueLinePrefix"] = "",
        ["playerName"] = "Adventurer"
      };

      var personaName = "TestNPC";
      var description = "A test NPC";
      var memory = new List<string>();
      var dialogueHistory = new List<string>();
      var playerName = "Player";
      var playerInput = "Hello";

      // Act
      var result = PromptComposer.ComposeWithSettings(personaName, description, memory, dialogueHistory, playerName, playerInput, settingsDict);

      // Assert
      Assert.That(result, Contains.Substring("Character Memory (Recent Events):"));
      Assert.That(result, Contains.Substring("Conversation History:"));
    }

    [Test]
    public void Compose_WithMemoryLimit_RespectsMaxMemoryItems()
    {
      // Arrange
      var settingsDict = new Dictionary<string, object>
      {
        ["npcTemplate"] = "NPC: {personaName}",
        ["descriptionTemplate"] = "Description: {description}",
        ["memoryHeaderTemplate"] = "NPC Memory:",
        ["memoryItemTemplate"] = "- {memory}",
        ["dialogueHeaderTemplate"] = "Dialogue so far:",
        ["playerTemplate"] = "Player: {playerName}",
        ["playerInputTemplate"] = "Player says: {playerInput}",
        ["responsePromptTemplate"] = "NPC responds:",
        ["includeEmptyMemory"] = false,
        ["includeEmptyDialogue"] = false,
        ["maxMemoryItems"] = 2,
        ["maxDialogueLines"] = 10,
        ["sectionSeparator"] = "\n",
        ["compactDialogueFormat"] = false,
        ["dialogueLinePrefix"] = "",
        ["playerName"] = "Player"
      };

      var personaName = "TestNPC";
      var description = "A test NPC";
      var memory = new List<string> { "Memory 1", "Memory 2", "Memory 3", "Memory 4" };
      var dialogueHistory = new List<string>();
      var playerName = "Player";
      var playerInput = "Hello";

      // Act
      var result = PromptComposer.ComposeWithSettings(personaName, description, memory, dialogueHistory, playerName, playerInput, settingsDict);

      // Assert
      Assert.That(result, Contains.Substring("- Memory 3")); // Should include last 2 memories
      Assert.That(result, Contains.Substring("- Memory 4"));
      Assert.That(result, Does.Not.Contain("- Memory 1")); // Should not include first 2 memories
      Assert.That(result, Does.Not.Contain("- Memory 2"));
    }

    [Test]
    public void Compose_WithDialogueLimit_RespectsMaxDialogueLines()
    {
      // Arrange
      var settingsDict = new Dictionary<string, object>
      {
        ["npcTemplate"] = "NPC: {personaName}",
        ["descriptionTemplate"] = "Description: {description}",
        ["memoryHeaderTemplate"] = "NPC Memory:",
        ["memoryItemTemplate"] = "- {memory}",
        ["dialogueHeaderTemplate"] = "Dialogue so far:",
        ["playerTemplate"] = "Player: {playerName}",
        ["playerInputTemplate"] = "Player says: {playerInput}",
        ["responsePromptTemplate"] = "NPC responds:",
        ["includeEmptyMemory"] = false,
        ["includeEmptyDialogue"] = false,
        ["maxMemoryItems"] = 10,
        ["maxDialogueLines"] = 2,
        ["sectionSeparator"] = "\n",
        ["compactDialogueFormat"] = false,
        ["dialogueLinePrefix"] = "",
        ["playerName"] = "Player"
      };

      var personaName = "TestNPC";
      var description = "A test NPC";
      var memory = new List<string>();
      var dialogueHistory = new List<string> { "Player: Hello", "NPC: Hi!", "Player: How are you?", "NPC: Good!" };
      var playerName = "Player";
      var playerInput = "Hello";

      // Act
      var result = PromptComposer.ComposeWithSettings(personaName, description, memory, dialogueHistory, playerName, playerInput, settingsDict);

      // Assert
      Assert.That(result, Contains.Substring("Player: How are you?")); // Should include last 2 dialogue lines
      Assert.That(result, Contains.Substring("NPC: Good!"));
      Assert.That(result, Does.Not.Contain("Player: Hello")); // Should not include first 2 dialogue lines
      Assert.That(result, Does.Not.Contain("NPC: Hi!"));
    }

    [Test]
    public void Compose_WithPlayerNameFromSettings_UsesSettingsPlayerName()
    {
      // Arrange
      var settingsDict = new Dictionary<string, object>
      {
        ["npcTemplate"] = "NPC: {personaName}",
        ["descriptionTemplate"] = "Description: {description}",
        ["memoryHeaderTemplate"] = "NPC Memory:",
        ["memoryItemTemplate"] = "- {memory}",
        ["dialogueHeaderTemplate"] = "Dialogue so far:",
        ["playerTemplate"] = "Player: {playerName}",
        ["playerInputTemplate"] = "Player says: {playerInput}",
        ["responsePromptTemplate"] = "NPC responds:",
        ["includeEmptyMemory"] = false,
        ["includeEmptyDialogue"] = false,
        ["maxMemoryItems"] = 10,
        ["maxDialogueLines"] = 10,
        ["sectionSeparator"] = "\n",
        ["compactDialogueFormat"] = false,
        ["dialogueLinePrefix"] = "",
        ["playerName"] = "CustomPlayer"
      };

      var personaName = "TestNPC";
      var description = "A test NPC";
      var memory = new List<string>();
      var dialogueHistory = new List<string>();
      var playerName = ""; // Empty player name parameter
      var playerInput = "Hello";

      // Act
      var result = PromptComposer.ComposeWithSettings(personaName, description, memory, dialogueHistory, playerName, playerInput, settingsDict);

      // Assert
      Assert.That(result, Contains.Substring("Player: CustomPlayer")); // Should use player name from settings
    }

    [Test]
    public void Compose_WithPlayerNameFromSettings_OverridesParameterPlayerName()
    {
      // Arrange
      var settingsDict = new Dictionary<string, object>
      {
        ["npcTemplate"] = "NPC: {personaName}",
        ["descriptionTemplate"] = "Description: {description}",
        ["memoryHeaderTemplate"] = "NPC Memory:",
        ["memoryItemTemplate"] = "- {memory}",
        ["dialogueHeaderTemplate"] = "Dialogue so far:",
        ["playerTemplate"] = "Player: {playerName}",
        ["playerInputTemplate"] = "Player says: {playerInput}",
        ["responsePromptTemplate"] = "NPC responds:",
        ["includeEmptyMemory"] = false,
        ["includeEmptyDialogue"] = false,
        ["maxMemoryItems"] = 10,
        ["maxDialogueLines"] = 10,
        ["sectionSeparator"] = "\n",
        ["compactDialogueFormat"] = false,
        ["dialogueLinePrefix"] = "",
        ["playerName"] = "SettingsPlayer"
      };

      var personaName = "TestNPC";
      var description = "A test NPC";
      var memory = new List<string>();
      var dialogueHistory = new List<string>();
      var playerName = "ParameterPlayer"; // This should be overridden by settings
      var playerInput = "Hello";

      // Act
      var result = PromptComposer.ComposeWithSettings(personaName, description, memory, dialogueHistory, playerName, playerInput, settingsDict);

      // Assert
      Assert.That(result, Contains.Substring("Player: SettingsPlayer")); // Should use player name from settings
      Assert.That(result, Does.Not.Contain("Player: ParameterPlayer")); // Should not use parameter player name
    }

    [Test]
    public void Compose_WithEmptyDescription_OmitsDescription()
    {
      // Arrange
      var personaName = "TestNPC";
      var description = ""; // Empty description
      var memory = new List<string>();
      var dialogueHistory = new List<string>();
      var playerName = "Player";
      var playerInput = "Hello";

      // Act
      var result = PromptComposer.ComposeWithSettings(personaName, description, memory, dialogueHistory, playerName, playerInput, null);

      // Assert
      Assert.That(result, Does.Not.Contain("Description:"));
    }

    [Test]
    public void Compose_WithWrongTypeSetting_UsesDefault()
    {
      // Arrange
      var settingsDict = new Dictionary<string, object>
      {
        ["maxMemoryItems"] = "not a number", // Wrong type
        ["compactDialogueFormat"] = "not a bool" // Wrong type
      };

      var personaName = "TestNPC";
      var description = "A test NPC";
      var memory = new List<string> { "Memory 1", "Memory 2", "Memory 3", "Memory 4", "Memory 5" };
      var dialogueHistory = new List<string>();
      var playerName = "Player";
      var playerInput = "Hello";

      // Act
      var result = PromptComposer.ComposeWithSettings(personaName, description, memory, dialogueHistory, playerName, playerInput, settingsDict);

      // Assert - Should use default maxMemoryItems (10), so all memories should be included
      Assert.That(result, Contains.Substring("- Memory 1"));
      Assert.That(result, Contains.Substring("- Memory 5"));
    }

    [Test]
    public void Compose_WithCompactDialogueFormat_UsesPrefix()
    {
      // Arrange
      var settingsDict = new Dictionary<string, object>
      {
        ["compactDialogueFormat"] = true,
        ["dialogueLinePrefix"] = "> "
      };

      var personaName = "TestNPC";
      var description = "A test NPC";
      var memory = new List<string>();
      var dialogueHistory = new List<string> { "Player: Hello", "NPC: Hi!" };
      var playerName = "Player";
      var playerInput = "Hello";

      // Act
      var result = PromptComposer.ComposeWithSettings(personaName, description, memory, dialogueHistory, playerName, playerInput, settingsDict);

      // Assert
      Assert.That(result, Contains.Substring("> Player: Hello"));
      Assert.That(result, Contains.Substring("> NPC: Hi!"));
    }

    #endregion

    #region ComposePrompt Tests

    [Test]
    public void ComposePrompt_WithFullProfile_ReturnsCompletePrompt()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile
      {
        Name = "TestNPC",
        Description = "A test character",
        SystemPrompt = "You are a helpful assistant",
        Background = "Born in a small village",
        Traits = new Dictionary<string, string> { { "Kindness", "High" }, { "Intelligence", "Medium" } }
      };
      var session = new DialogueSession("test-persona");
      session.AppendPlayer("Hello");
      session.AppendNpc("Hi there!");
      var userInput = "How are you?";

      // Act
      var result = composer.ComposePrompt(profile, session, userInput);

      // Assert
      Assert.That(result, Contains.Substring("System: You are a helpful assistant"));
      Assert.That(result, Contains.Substring("You are TestNPC, A test character"));
      Assert.That(result, Contains.Substring("Your personality traits:"));
      Assert.That(result, Contains.Substring("- Kindness: High"));
      Assert.That(result, Contains.Substring("- Intelligence: Medium"));
      Assert.That(result, Contains.Substring("Background: Born in a small village"));
      Assert.That(result, Contains.Substring("Conversation history:"));
      Assert.That(result, Contains.Substring("Player: Hello"));
      Assert.That(result, Contains.Substring("NPC: Hi there!"));
      Assert.That(result, Contains.Substring("Player: How are you?"));
      Assert.That(result, Contains.Substring("TestNPC:"));
    }

    [Test]
    public void ComposePrompt_WithNullProfile_ThrowsArgumentNullException()
    {
      // Arrange
      var composer = new PromptComposer();
      var session = new DialogueSession("test-persona");

      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => composer.ComposePrompt(null!, session, "test"));
    }

    [Test]
    public void ComposePrompt_WithNullSession_ThrowsArgumentNullException()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "Test" };

      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => composer.ComposePrompt(profile, null!, "test"));
    }

    [Test]
    public void ComposePrompt_WithNullUserInput_ThrowsArgumentException()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "Test" };
      var session = new DialogueSession("test-persona");

      // Act & Assert
      Assert.Throws<ArgumentException>(() => composer.ComposePrompt(profile, session, null!));
    }

    [Test]
    public void ComposePrompt_WithEmptyUserInput_ThrowsArgumentException()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "Test" };
      var session = new DialogueSession("test-persona");

      // Act & Assert
      Assert.Throws<ArgumentException>(() => composer.ComposePrompt(profile, session, ""));
      Assert.Throws<ArgumentException>(() => composer.ComposePrompt(profile, session, "   "));
    }

    [Test]
    public void ComposePrompt_WithoutSystemPrompt_OmitsSystemPrompt()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "TestNPC", Description = "A test" };
      var session = new DialogueSession("test-persona");

      // Act
      var result = composer.ComposePrompt(profile, session, "Hello");

      // Assert
      Assert.That(result, Does.Not.Contain("System:"));
    }

    [Test]
    public void ComposePrompt_WithoutDescription_OmitsDescription()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "TestNPC" };
      var session = new DialogueSession("test-persona");

      // Act
      var result = composer.ComposePrompt(profile, session, "Hello");

      // Assert
      Assert.That(result, Does.Not.Contain("You are TestNPC,"));
    }

    [Test]
    public void ComposePrompt_WithIncludeTraitsFalse_OmitsTraits()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile
      {
        Name = "TestNPC",
        Traits = new Dictionary<string, string> { { "Kindness", "High" } }
      };
      var session = new DialogueSession("test-persona");

      // Act
      var result = composer.ComposePrompt(profile, session, "Hello", includeTraits: false);

      // Assert
      Assert.That(result, Does.Not.Contain("Your personality traits:"));
      Assert.That(result, Does.Not.Contain("- Kindness: High"));
    }

    [Test]
    public void ComposePrompt_WithEmptyTraits_OmitsTraits()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile
      {
        Name = "TestNPC",
        Traits = new Dictionary<string, string>()
      };
      var session = new DialogueSession("test-persona");

      // Act
      var result = composer.ComposePrompt(profile, session, "Hello", includeTraits: true);

      // Assert
      Assert.That(result, Does.Not.Contain("Your personality traits:"));
    }

    [Test]
    public void ComposePrompt_WithLongHistory_TruncatesToMaxEntries()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "TestNPC" };
      var session = new DialogueSession("test-persona");
      
      // Add more than MaxHistoryEntries (10) entries
      for (int i = 1; i <= 15; i++)
      {
        session.AppendPlayer($"Message {i}");
        session.AppendNpc($"Response {i}");
      }
      // This creates 30 entries, but we should only see the last 10

      // Act
      var result = composer.ComposePrompt(profile, session, "Hello");

      // Assert - Should only include last 10 entries (Messages 6-15, Responses 6-15)
      // Since we have 15 pairs (30 entries total), we should see the last 10 pairs
      // Verify the last entries are present
      Assert.That(result, Contains.Substring("Message 11")); // Should include last 10 entries
      Assert.That(result, Contains.Substring("Response 15"));
      // Verify early messages are not present (check for exact pattern to avoid matching "Message 11", "Message 12", etc.)
      Assert.That(result, Does.Not.Contain("Message 1\n"));
      Assert.That(result, Does.Not.Contain("Message 1 "));
      Assert.That(result, Does.Not.Contain("Message 5\n"));
    }

    [Test]
    public void ComposePrompt_WithLongPrompt_TruncatesToMaxContextLength()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile
      {
        Name = "TestNPC",
        Description = new string('A', 10000) // Very long description
      };
      var session = new DialogueSession("test-persona");

      // Act
      var result = composer.ComposePrompt(profile, session, "Hello");

      // Assert
      Assert.That(result.Length, Is.LessThanOrEqualTo(8003)); // MaxContextLength + "..."
      Assert.That(result, Does.EndWith("..."));
    }

    #endregion

    #region ComposeSimplePrompt Tests

    [Test]
    public void ComposeSimplePrompt_WithFullProfile_ReturnsCompletePrompt()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile
      {
        Name = "TestNPC",
        Description = "A test character",
        SystemPrompt = "You are helpful",
        Traits = new Dictionary<string, string> { { "Kindness", "High" } }
      };

      // Act
      var result = composer.ComposeSimplePrompt(profile, "Hello");

      // Assert
      Assert.That(result, Contains.Substring("System: You are helpful"));
      Assert.That(result, Contains.Substring("You are TestNPC, A test character"));
      Assert.That(result, Contains.Substring("Your personality traits:"));
      Assert.That(result, Contains.Substring("Player: Hello"));
      Assert.That(result, Contains.Substring("TestNPC:"));
    }

    [Test]
    public void ComposeSimplePrompt_WithNullProfile_ThrowsArgumentNullException()
    {
      // Arrange
      var composer = new PromptComposer();

      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => composer.ComposeSimplePrompt(null!, "test"));
    }

    [Test]
    public void ComposeSimplePrompt_WithNullUserInput_ThrowsArgumentException()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "Test" };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => composer.ComposeSimplePrompt(profile, null!));
      Assert.Throws<ArgumentException>(() => composer.ComposeSimplePrompt(profile, ""));
      Assert.Throws<ArgumentException>(() => composer.ComposeSimplePrompt(profile, "   "));
    }

    [Test]
    public void ComposeSimplePrompt_WithIncludeTraitsFalse_OmitsTraits()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile
      {
        Name = "TestNPC",
        Traits = new Dictionary<string, string> { { "Kindness", "High" } }
      };

      // Act
      var result = composer.ComposeSimplePrompt(profile, "Hello", includeTraits: false);

      // Assert
      Assert.That(result, Does.Not.Contain("Your personality traits:"));
    }

    [Test]
    public void ComposeSimplePrompt_WithLongPrompt_TruncatesToMaxContextLength()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile
      {
        Name = "TestNPC",
        Description = new string('A', 10000)
      };

      // Act
      var result = composer.ComposeSimplePrompt(profile, "Hello");

      // Assert
      Assert.That(result.Length, Is.LessThanOrEqualTo(8003));
      Assert.That(result, Does.EndWith("..."));
    }

    #endregion

    #region ComposeInstructionPrompt Tests

    [Test]
    public void ComposeInstructionPrompt_WithFullProfile_ReturnsCompletePrompt()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile
      {
        Name = "TestNPC",
        Description = "A test character",
        SystemPrompt = "You are helpful",
        Traits = new Dictionary<string, string> { { "Kindness", "High" } }
      };

      // Act
      var result = composer.ComposeInstructionPrompt(profile, "Do something", "Some context");

      // Assert
      Assert.That(result, Contains.Substring("System: You are helpful"));
      Assert.That(result, Contains.Substring("You are TestNPC, A test character"));
      Assert.That(result, Contains.Substring("Your personality traits:"));
      Assert.That(result, Contains.Substring("Instruction: Do something"));
      Assert.That(result, Contains.Substring("Context: Some context"));
      Assert.That(result, Contains.Substring("TestNPC:"));
    }

    [Test]
    public void ComposeInstructionPrompt_WithNullProfile_ThrowsArgumentNullException()
    {
      // Arrange
      var composer = new PromptComposer();

      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => composer.ComposeInstructionPrompt(null!, "test", null));
    }

    [Test]
    public void ComposeInstructionPrompt_WithNullInstruction_ThrowsArgumentException()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "Test" };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => composer.ComposeInstructionPrompt(profile, null!, null));
      Assert.Throws<ArgumentException>(() => composer.ComposeInstructionPrompt(profile, "", null));
      Assert.Throws<ArgumentException>(() => composer.ComposeInstructionPrompt(profile, "   ", null));
    }

    [Test]
    public void ComposeInstructionPrompt_WithoutContext_OmitsContext()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "TestNPC" };

      // Act
      var result = composer.ComposeInstructionPrompt(profile, "Do something", null);

      // Assert
      Assert.That(result, Does.Not.Contain("Context:"));
    }

    [Test]
    public void ComposeInstructionPrompt_WithEmptyContext_OmitsContext()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "TestNPC" };

      // Act
      var result = composer.ComposeInstructionPrompt(profile, "Do something", "");

      // Assert
      Assert.That(result, Does.Not.Contain("Context:"));
    }

    [Test]
    public void ComposeInstructionPrompt_WithLongPrompt_TruncatesToMaxContextLength()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile
      {
        Name = "TestNPC",
        Description = new string('A', 10000)
      };

      // Act
      var result = composer.ComposeInstructionPrompt(profile, "Do something", null);

      // Assert
      Assert.That(result.Length, Is.LessThanOrEqualTo(8003));
      Assert.That(result, Does.EndWith("..."));
    }

    #endregion

    #region ComposeStructuredJsonPrompt Tests

    [Test]
    public void ComposeStructuredJsonPrompt_WithFullProfile_ReturnsCompletePrompt()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile
      {
        Name = "TestNPC",
        Description = "A test character",
        SystemPrompt = "You are helpful",
        Traits = new Dictionary<string, string> { { "Kindness", "High" } }
      };
      var jsonSchema = "{\"type\": \"object\", \"properties\": {\"response\": {\"type\": \"string\"}}}";

      // Act
      var result = composer.ComposeStructuredJsonPrompt(profile, "Do something", jsonSchema, "Some context");

      // Assert
      Assert.That(result, Contains.Substring("IMPORTANT: You must respond with valid JSON only"));
      Assert.That(result, Contains.Substring(jsonSchema));
      Assert.That(result, Contains.Substring("System: You are helpful"));
      Assert.That(result, Contains.Substring("You are TestNPC, A test character"));
      Assert.That(result, Contains.Substring("Task: Do something"));
      Assert.That(result, Contains.Substring("Context: Some context"));
      Assert.That(result, Contains.Substring("Respond with JSON only:"));
    }

    [Test]
    public void ComposeStructuredJsonPrompt_WithNullProfile_ThrowsArgumentNullException()
    {
      // Arrange
      var composer = new PromptComposer();

      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => composer.ComposeStructuredJsonPrompt(null!, "test", "schema", null));
    }

    [Test]
    public void ComposeStructuredJsonPrompt_WithNullInstruction_ThrowsArgumentException()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "Test" };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => composer.ComposeStructuredJsonPrompt(profile, null!, "schema", null));
      Assert.Throws<ArgumentException>(() => composer.ComposeStructuredJsonPrompt(profile, "", "schema", null));
    }

    [Test]
    public void ComposeStructuredJsonPrompt_WithNullJsonSchema_ThrowsArgumentException()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "Test" };

      // Act & Assert
      Assert.Throws<ArgumentException>(() => composer.ComposeStructuredJsonPrompt(profile, "test", null!, null));
      Assert.Throws<ArgumentException>(() => composer.ComposeStructuredJsonPrompt(profile, "test", "", null));
    }

    [Test]
    public void ComposeStructuredJsonPrompt_WithoutContext_OmitsContext()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "TestNPC" };

      // Act
      var result = composer.ComposeStructuredJsonPrompt(profile, "Do something", "schema", null);

      // Assert
      Assert.That(result, Does.Not.Contain("Context:"));
    }

    [Test]
    public void ComposeStructuredJsonPrompt_WithLongPrompt_TruncatesToMaxContextLength()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile
      {
        Name = "TestNPC",
        Description = new string('A', 10000)
      };

      // Act
      var result = composer.ComposeStructuredJsonPrompt(profile, "Do something", "schema", null);

      // Assert
      Assert.That(result.Length, Is.LessThanOrEqualTo(8003));
      Assert.That(result, Does.EndWith("..."));
    }

    #endregion

    #region ComposeStructuredJsonConversationPrompt Tests

    [Test]
    public void ComposeStructuredJsonConversationPrompt_WithFullProfile_ReturnsCompletePrompt()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile
      {
        Name = "TestNPC",
        Description = "A test character",
        SystemPrompt = "You are helpful",
        Background = "Born in a village",
        Traits = new Dictionary<string, string> { { "Kindness", "High" } }
      };
      var session = new DialogueSession("test-persona");
      session.AppendPlayer("Hello");
      session.AppendNpc("Hi!");
      var jsonSchema = "{\"type\": \"object\"}";

      // Act
      var result = composer.ComposeStructuredJsonConversationPrompt(profile, session, "How are you?", jsonSchema);

      // Assert
      Assert.That(result, Contains.Substring("IMPORTANT: You must respond with valid JSON only"));
      Assert.That(result, Contains.Substring(jsonSchema));
      Assert.That(result, Contains.Substring("System: You are helpful"));
      Assert.That(result, Contains.Substring("You are TestNPC, A test character"));
      Assert.That(result, Contains.Substring("Background: Born in a village"));
      Assert.That(result, Contains.Substring("Conversation history:"));
      Assert.That(result, Contains.Substring("Player: Hello"));
      Assert.That(result, Contains.Substring("Player: How are you?"));
      Assert.That(result, Contains.Substring("Respond with JSON only:"));
    }

    [Test]
    public void ComposeStructuredJsonConversationPrompt_WithNullProfile_ThrowsArgumentNullException()
    {
      // Arrange
      var composer = new PromptComposer();
      var session = new DialogueSession("test-persona");

      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => composer.ComposeStructuredJsonConversationPrompt(null!, session, "test", "schema"));
    }

    [Test]
    public void ComposeStructuredJsonConversationPrompt_WithNullSession_ThrowsArgumentNullException()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "Test" };

      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => composer.ComposeStructuredJsonConversationPrompt(profile, null!, "test", "schema"));
    }

    [Test]
    public void ComposeStructuredJsonConversationPrompt_WithNullUserInput_ThrowsArgumentException()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "Test" };
      var session = new DialogueSession("test-persona");

      // Act & Assert
      Assert.Throws<ArgumentException>(() => composer.ComposeStructuredJsonConversationPrompt(profile, session, null!, "schema"));
      Assert.Throws<ArgumentException>(() => composer.ComposeStructuredJsonConversationPrompt(profile, session, "", "schema"));
    }

    [Test]
    public void ComposeStructuredJsonConversationPrompt_WithNullJsonSchema_ThrowsArgumentException()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "Test" };
      var session = new DialogueSession("test-persona");

      // Act & Assert
      Assert.Throws<ArgumentException>(() => composer.ComposeStructuredJsonConversationPrompt(profile, session, "test", null!));
      Assert.Throws<ArgumentException>(() => composer.ComposeStructuredJsonConversationPrompt(profile, session, "test", ""));
    }

    [Test]
    public void ComposeStructuredJsonConversationPrompt_WithLongHistory_TruncatesToMaxEntries()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "TestNPC" };
      var session = new DialogueSession("test-persona");
      
      for (int i = 1; i <= 15; i++)
      {
        session.AppendPlayer($"Message {i}");
      }

      // Act
      var result = composer.ComposeStructuredJsonConversationPrompt(profile, session, "Hello", "schema");

      // Assert - Should only include last 10 entries (Messages 6-15)
      Assert.That(result, Contains.Substring("Message 6")); // Last 10 entries
      Assert.That(result, Contains.Substring("Message 15"));
      // Verify early messages are not present (check for exact pattern to avoid matching "Message 11", "Message 12", etc.)
      Assert.That(result, Does.Not.Contain("Player: Message 1\n"));
      Assert.That(result, Does.Not.Contain("Player: Message 1 "));
      Assert.That(result, Does.Not.Contain("Player: Message 5\n"));
    }

    [Test]
    public void ComposeStructuredJsonConversationPrompt_WithLongPrompt_TruncatesToMaxContextLength()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile
      {
        Name = "TestNPC",
        Description = new string('A', 10000)
      };
      var session = new DialogueSession("test-persona");

      // Act
      var result = composer.ComposeStructuredJsonConversationPrompt(profile, session, "Hello", "schema");

      // Assert
      Assert.That(result.Length, Is.LessThanOrEqualTo(8003));
      Assert.That(result, Does.EndWith("..."));
    }

    #endregion

    #region ComposePromptWithPrefix Tests

    [Test]
    public void ComposePromptWithPrefix_WithFullProfile_ReturnsPrefixAndSuffix()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile
      {
        Name = "TestNPC",
        Description = "A test character",
        SystemPrompt = "You are helpful",
        Background = "Born in a village",
        Traits = new Dictionary<string, string> { { "Kindness", "High" } }
      };
      var session = new DialogueSession("test-persona");
      session.AppendPlayer("Hello");
      session.AppendNpc("Hi!");

      // Act
      var (prefix, suffix) = composer.ComposePromptWithPrefix(profile, session, "How are you?");

      // Assert
      Assert.That(prefix, Contains.Substring("System: You are helpful"));
      Assert.That(prefix, Contains.Substring("You are TestNPC, A test character"));
      Assert.That(prefix, Contains.Substring("Your personality traits:"));
      Assert.That(prefix, Contains.Substring("Background: Born in a village"));
      Assert.That(prefix, Does.Not.Contain("Conversation history:"));
      Assert.That(prefix, Does.Not.Contain("Player:"));
      
      Assert.That(suffix, Contains.Substring("Conversation history:"));
      Assert.That(suffix, Contains.Substring("Player: Hello"));
      Assert.That(suffix, Contains.Substring("Player: How are you?"));
      Assert.That(suffix, Contains.Substring("TestNPC:"));
      Assert.That(suffix, Does.Not.Contain("System:"));
    }

    [Test]
    public void ComposePromptWithPrefix_WithNullProfile_ThrowsArgumentNullException()
    {
      // Arrange
      var composer = new PromptComposer();
      var session = new DialogueSession("test-persona");

      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => composer.ComposePromptWithPrefix(null!, session, "test"));
    }

    [Test]
    public void ComposePromptWithPrefix_WithNullSession_ThrowsArgumentNullException()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "Test" };

      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => composer.ComposePromptWithPrefix(profile, null!, "test"));
    }

    [Test]
    public void ComposePromptWithPrefix_WithNullUserInput_ThrowsArgumentException()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "Test" };
      var session = new DialogueSession("test-persona");

      // Act & Assert
      Assert.Throws<ArgumentException>(() => composer.ComposePromptWithPrefix(profile, session, null!));
      Assert.Throws<ArgumentException>(() => composer.ComposePromptWithPrefix(profile, session, ""));
    }

    [Test]
    public void ComposePromptWithPrefix_WithIncludeTraitsFalse_OmitsTraitsFromPrefix()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile
      {
        Name = "TestNPC",
        Traits = new Dictionary<string, string> { { "Kindness", "High" } }
      };
      var session = new DialogueSession("test-persona");

      // Act
      var (prefix, suffix) = composer.ComposePromptWithPrefix(profile, session, "Hello", includeTraits: false);

      // Assert
      Assert.That(prefix, Does.Not.Contain("Your personality traits:"));
    }

    [Test]
    public void ComposePromptWithPrefix_WithLongHistory_TruncatesToMaxEntries()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "TestNPC" };
      var session = new DialogueSession("test-persona");
      
      for (int i = 1; i <= 15; i++)
      {
        session.AppendPlayer($"Message {i}");
      }

      // Act
      var (prefix, suffix) = composer.ComposePromptWithPrefix(profile, session, "Hello");

      // Assert - Should only include last 10 entries (Messages 6-15)
      Assert.That(suffix, Contains.Substring("Message 6")); // Last 10 entries
      Assert.That(suffix, Contains.Substring("Message 15"));
      // Verify early messages are not present (check for exact pattern to avoid matching "Message 11", "Message 12", etc.)
      Assert.That(suffix, Does.Not.Contain("Message 1\n"));
      Assert.That(suffix, Does.Not.Contain("Message 1 "));
      Assert.That(suffix, Does.Not.Contain("Message 5\n"));
    }

    [Test]
    public void ComposePromptWithPrefix_WithLongSuffix_TruncatesSuffix()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile { Name = "TestNPC" };
      var session = new DialogueSession("test-persona");
      var longInput = new string('A', 10000);

      // Act
      var (prefix, suffix) = composer.ComposePromptWithPrefix(profile, session, longInput);

      // Assert
      var totalLength = prefix.Length + suffix.Length;
      Assert.That(totalLength, Is.LessThanOrEqualTo(8003)); // MaxContextLength + "..."
      if (suffix.Length > 0 && suffix.Length < longInput.Length)
      {
        Assert.That(suffix, Does.EndWith("..."));
      }
    }

    [Test]
    public void ComposePromptWithPrefix_WithLongPrefix_TruncatesPrefix()
    {
      // Arrange
      var composer = new PromptComposer();
      var profile = new PersonaProfile
      {
        Name = "TestNPC",
        Description = new string('A', 10000) // Very long description
      };
      var session = new DialogueSession("test-persona");

      // Act
      var (prefix, suffix) = composer.ComposePromptWithPrefix(profile, session, "Hello");

      // Assert
      var totalLength = prefix.Length + suffix.Length;
      Assert.That(totalLength, Is.LessThanOrEqualTo(8003)); // MaxContextLength + "..."
      if (prefix.Length >= 8000)
      {
        Assert.That(prefix, Does.EndWith("..."));
        Assert.That(suffix, Is.EqualTo(string.Empty));
      }
    }

    #endregion
  }
}

