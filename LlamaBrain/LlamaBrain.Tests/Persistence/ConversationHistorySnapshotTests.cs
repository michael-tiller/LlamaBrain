using System;
using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json;
using LlamaBrain.Persistence;
using LlamaBrain.Persistence.Dtos;

namespace LlamaBrain.Tests.Persistence
{
  /// <summary>
  /// Tests for ConversationHistorySnapshot serialization and initialization.
  /// </summary>
  [TestFixture]
  public class ConversationHistorySnapshotTests
  {
    #region Default Value Tests

    [Test]
    public void Constructor_DefaultValues_PersonaIdIsEmptyString()
    {
      // Act
      var snapshot = new ConversationHistorySnapshot();

      // Assert
      Assert.That(snapshot.PersonaId, Is.EqualTo(""));
    }

    [Test]
    public void Constructor_DefaultValues_EntriesIsEmptyList()
    {
      // Act
      var snapshot = new ConversationHistorySnapshot();

      // Assert
      Assert.That(snapshot.Entries, Is.Not.Null);
      Assert.That(snapshot.Entries, Is.Empty);
    }

    #endregion

    #region Property Tests

    [Test]
    public void PersonaId_SetAndGet_ReturnsSetValue()
    {
      // Arrange
      var snapshot = new ConversationHistorySnapshot();

      // Act
      snapshot.PersonaId = "npc_guard_01";

      // Assert
      Assert.That(snapshot.PersonaId, Is.EqualTo("npc_guard_01"));
    }

    [Test]
    public void Entries_SetAndGet_ReturnsSetValue()
    {
      // Arrange
      var snapshot = new ConversationHistorySnapshot();
      var entries = new List<DialogueEntryDto>
      {
        new DialogueEntryDto { Speaker = "Player", Text = "Hello", TimestampTicks = 100L }
      };

      // Act
      snapshot.Entries = entries;

      // Assert
      Assert.That(snapshot.Entries, Is.SameAs(entries));
    }

    [Test]
    public void Entries_AddToList_PersistsEntry()
    {
      // Arrange
      var snapshot = new ConversationHistorySnapshot();
      var entry = new DialogueEntryDto { Speaker = "NPC", Text = "Greetings!", TimestampTicks = 200L };

      // Act
      snapshot.Entries.Add(entry);

      // Assert
      Assert.That(snapshot.Entries.Count, Is.EqualTo(1));
      Assert.That(snapshot.Entries[0], Is.SameAs(entry));
    }

    #endregion

    #region Serialization Tests

    [Test]
    public void JsonRoundTrip_EmptySnapshot_PreservesDefaults()
    {
      // Arrange
      var original = new ConversationHistorySnapshot();

      // Act
      var json = JsonConvert.SerializeObject(original);
      var restored = JsonConvert.DeserializeObject<ConversationHistorySnapshot>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.PersonaId, Is.EqualTo(""));
      Assert.That(restored.Entries, Is.Not.Null);
      Assert.That(restored.Entries, Is.Empty);
    }

    [Test]
    public void JsonRoundTrip_WithPersonaId_PreservesValue()
    {
      // Arrange
      var original = new ConversationHistorySnapshot
      {
        PersonaId = "merchant_blacksmith"
      };

      // Act
      var json = JsonConvert.SerializeObject(original);
      var restored = JsonConvert.DeserializeObject<ConversationHistorySnapshot>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.PersonaId, Is.EqualTo("merchant_blacksmith"));
    }

    [Test]
    public void JsonRoundTrip_WithSingleEntry_PreservesEntry()
    {
      // Arrange
      var original = new ConversationHistorySnapshot
      {
        PersonaId = "guard_01",
        Entries = new List<DialogueEntryDto>
        {
          new DialogueEntryDto
          {
            Speaker = "Player",
            Text = "What news?",
            TimestampTicks = 638000000000000000L
          }
        }
      };

      // Act
      var json = JsonConvert.SerializeObject(original);
      var restored = JsonConvert.DeserializeObject<ConversationHistorySnapshot>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.Entries.Count, Is.EqualTo(1));
      Assert.That(restored.Entries[0].Speaker, Is.EqualTo("Player"));
      Assert.That(restored.Entries[0].Text, Is.EqualTo("What news?"));
      Assert.That(restored.Entries[0].TimestampTicks, Is.EqualTo(638000000000000000L));
    }

    [Test]
    public void JsonRoundTrip_WithMultipleEntries_PreservesOrder()
    {
      // Arrange
      var original = new ConversationHistorySnapshot
      {
        PersonaId = "tavern_keeper",
        Entries = new List<DialogueEntryDto>
        {
          new DialogueEntryDto { Speaker = "Player", Text = "Hello!", TimestampTicks = 100L },
          new DialogueEntryDto { Speaker = "NPC", Text = "Welcome!", TimestampTicks = 200L },
          new DialogueEntryDto { Speaker = "Player", Text = "What's for sale?", TimestampTicks = 300L }
        }
      };

      // Act
      var json = JsonConvert.SerializeObject(original);
      var restored = JsonConvert.DeserializeObject<ConversationHistorySnapshot>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.Entries.Count, Is.EqualTo(3));
      Assert.That(restored.Entries[0].Text, Is.EqualTo("Hello!"));
      Assert.That(restored.Entries[1].Text, Is.EqualTo("Welcome!"));
      Assert.That(restored.Entries[2].Text, Is.EqualTo("What's for sale?"));
    }

    [Test]
    public void JsonDeserialization_MissingFields_UsesDefaults()
    {
      // Arrange
      var json = "{}";

      // Act
      var restored = JsonConvert.DeserializeObject<ConversationHistorySnapshot>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.PersonaId, Is.Null.Or.EqualTo(""));
      Assert.That(restored.Entries, Is.Null.Or.Empty);
    }

    [Test]
    public void JsonDeserialization_PartialFields_PreservesProvidedValues()
    {
      // Arrange
      var json = "{\"PersonaId\":\"wizard_01\"}";

      // Act
      var restored = JsonConvert.DeserializeObject<ConversationHistorySnapshot>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.PersonaId, Is.EqualTo("wizard_01"));
    }

    [Test]
    public void JsonRoundTrip_SpecialCharactersInPersonaId_PreservesValue()
    {
      // Arrange
      var original = new ConversationHistorySnapshot
      {
        PersonaId = "npc_guard_01_area-2.sub"
      };

      // Act
      var json = JsonConvert.SerializeObject(original);
      var restored = JsonConvert.DeserializeObject<ConversationHistorySnapshot>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.PersonaId, Is.EqualTo("npc_guard_01_area-2.sub"));
    }

    #endregion

    #region Edge Case Tests

    [Test]
    public void Entries_LargeList_HandlesCorrectly()
    {
      // Arrange
      var snapshot = new ConversationHistorySnapshot { PersonaId = "test_npc" };
      for (int i = 0; i < 1000; i++)
      {
        snapshot.Entries.Add(new DialogueEntryDto
        {
          Speaker = i % 2 == 0 ? "Player" : "NPC",
          Text = $"Message {i}",
          TimestampTicks = i * 1000L
        });
      }

      // Act
      var json = JsonConvert.SerializeObject(snapshot);
      var restored = JsonConvert.DeserializeObject<ConversationHistorySnapshot>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.Entries.Count, Is.EqualTo(1000));
      Assert.That(restored.Entries[0].Text, Is.EqualTo("Message 0"));
      Assert.That(restored.Entries[999].Text, Is.EqualTo("Message 999"));
    }

    [Test]
    public void Entries_WithUnicodeContent_PreservesCorrectly()
    {
      // Arrange
      var original = new ConversationHistorySnapshot
      {
        PersonaId = "merchant_foreign",
        Entries = new List<DialogueEntryDto>
        {
          new DialogueEntryDto
          {
            Speaker = "NPC",
            Text = "Bienvenue! ¡Hola! 你好! こんにちは!",
            TimestampTicks = 12345L
          }
        }
      };

      // Act
      var json = JsonConvert.SerializeObject(original);
      var restored = JsonConvert.DeserializeObject<ConversationHistorySnapshot>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.Entries[0].Text, Is.EqualTo("Bienvenue! ¡Hola! 你好! こんにちは!"));
    }

    [Test]
    public void Entries_EmptyStringsInDialogue_PreservesCorrectly()
    {
      // Arrange
      var original = new ConversationHistorySnapshot
      {
        PersonaId = "",
        Entries = new List<DialogueEntryDto>
        {
          new DialogueEntryDto { Speaker = "", Text = "", TimestampTicks = 0L }
        }
      };

      // Act
      var json = JsonConvert.SerializeObject(original);
      var restored = JsonConvert.DeserializeObject<ConversationHistorySnapshot>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.PersonaId, Is.EqualTo(""));
      Assert.That(restored.Entries.Count, Is.EqualTo(1));
      Assert.That(restored.Entries[0].Speaker, Is.EqualTo(""));
      Assert.That(restored.Entries[0].Text, Is.EqualTo(""));
    }

    [Test]
    public void JsonRoundTrip_ChronologicalOrder_IsPreserved()
    {
      // Arrange - entries with specific timestamps
      var original = new ConversationHistorySnapshot
      {
        PersonaId = "time_test",
        Entries = new List<DialogueEntryDto>
        {
          new DialogueEntryDto { Speaker = "A", Text = "First", TimestampTicks = 1000L },
          new DialogueEntryDto { Speaker = "B", Text = "Second", TimestampTicks = 2000L },
          new DialogueEntryDto { Speaker = "A", Text = "Third", TimestampTicks = 3000L }
        }
      };

      // Act
      var json = JsonConvert.SerializeObject(original);
      var restored = JsonConvert.DeserializeObject<ConversationHistorySnapshot>(json);

      // Assert - verify chronological order is maintained
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.Entries[0].TimestampTicks, Is.LessThan(restored.Entries[1].TimestampTicks));
      Assert.That(restored.Entries[1].TimestampTicks, Is.LessThan(restored.Entries[2].TimestampTicks));
    }

    #endregion
  }
}
