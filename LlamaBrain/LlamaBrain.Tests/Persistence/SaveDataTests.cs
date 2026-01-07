using System;
using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json;
using LlamaBrain.Persistence;
using LlamaBrain.Persistence.Dtos;

namespace LlamaBrain.Tests.Persistence
{
  /// <summary>
  /// Tests for SaveData serialization and initialization.
  /// </summary>
  [TestFixture]
  public class SaveDataTests
  {
    #region Constant Tests

    [Test]
    public void CurrentVersion_IsOne()
    {
      // Assert
      Assert.That(SaveData.CurrentVersion, Is.EqualTo(1));
    }

    #endregion

    #region Default Value Tests

    [Test]
    public void Constructor_DefaultValues_VersionIsCurrentVersion()
    {
      // Act
      var saveData = new SaveData();

      // Assert
      Assert.That(saveData.Version, Is.EqualTo(SaveData.CurrentVersion));
    }

    [Test]
    public void Constructor_DefaultValues_SavedAtUtcTicksIsZero()
    {
      // Act
      var saveData = new SaveData();

      // Assert
      Assert.That(saveData.SavedAtUtcTicks, Is.EqualTo(0L));
    }

    [Test]
    public void Constructor_DefaultValues_PersonaMemoriesIsEmptyDictionary()
    {
      // Act
      var saveData = new SaveData();

      // Assert
      Assert.That(saveData.PersonaMemories, Is.Not.Null);
      Assert.That(saveData.PersonaMemories, Is.Empty);
    }

    [Test]
    public void Constructor_DefaultValues_ConversationHistoriesIsEmptyDictionary()
    {
      // Act
      var saveData = new SaveData();

      // Assert
      Assert.That(saveData.ConversationHistories, Is.Not.Null);
      Assert.That(saveData.ConversationHistories, Is.Empty);
    }

    [Test]
    public void Constructor_DefaultValues_MetadataIsEmptyDictionary()
    {
      // Act
      var saveData = new SaveData();

      // Assert
      Assert.That(saveData.Metadata, Is.Not.Null);
      Assert.That(saveData.Metadata, Is.Empty);
    }

    #endregion

    #region CreateNew Tests

    [Test]
    public void CreateNew_SetsVersionToCurrentVersion()
    {
      // Act
      var saveData = SaveData.CreateNew();

      // Assert
      Assert.That(saveData.Version, Is.EqualTo(SaveData.CurrentVersion));
    }

    [Test]
    public void CreateNew_SetsSavedAtUtcTicksToNonZero()
    {
      // Arrange
      var beforeTicks = DateTimeOffset.UtcNow.UtcTicks;

      // Act
      var saveData = SaveData.CreateNew();

      // Assert
      var afterTicks = DateTimeOffset.UtcNow.UtcTicks;
      Assert.That(saveData.SavedAtUtcTicks, Is.GreaterThanOrEqualTo(beforeTicks));
      Assert.That(saveData.SavedAtUtcTicks, Is.LessThanOrEqualTo(afterTicks));
    }

    [Test]
    public void CreateNew_InitializesEmptyCollections()
    {
      // Act
      var saveData = SaveData.CreateNew();

      // Assert
      Assert.That(saveData.PersonaMemories, Is.Not.Null);
      Assert.That(saveData.PersonaMemories, Is.Empty);
      Assert.That(saveData.ConversationHistories, Is.Not.Null);
      Assert.That(saveData.ConversationHistories, Is.Empty);
      Assert.That(saveData.Metadata, Is.Not.Null);
      Assert.That(saveData.Metadata, Is.Empty);
    }

    #endregion

    #region Property Tests

    [Test]
    public void Version_SetAndGet_ReturnsSetValue()
    {
      // Arrange
      var saveData = new SaveData();

      // Act
      saveData.Version = 2;

      // Assert
      Assert.That(saveData.Version, Is.EqualTo(2));
    }

    [Test]
    public void SavedAtUtcTicks_SetAndGet_ReturnsSetValue()
    {
      // Arrange
      var saveData = new SaveData();
      var expectedTicks = 638000000000000000L;

      // Act
      saveData.SavedAtUtcTicks = expectedTicks;

      // Assert
      Assert.That(saveData.SavedAtUtcTicks, Is.EqualTo(expectedTicks));
    }

    [Test]
    public void PersonaMemories_AddEntry_PersistsEntry()
    {
      // Arrange
      var saveData = new SaveData();
      var memory = new PersonaMemorySnapshot { PersonaId = "guard_01" };

      // Act
      saveData.PersonaMemories["guard_01"] = memory;

      // Assert
      Assert.That(saveData.PersonaMemories.ContainsKey("guard_01"), Is.True);
      Assert.That(saveData.PersonaMemories["guard_01"].PersonaId, Is.EqualTo("guard_01"));
    }

    [Test]
    public void ConversationHistories_AddEntry_PersistsEntry()
    {
      // Arrange
      var saveData = new SaveData();
      var history = new ConversationHistorySnapshot { PersonaId = "merchant_01" };

      // Act
      saveData.ConversationHistories["merchant_01"] = history;

      // Assert
      Assert.That(saveData.ConversationHistories.ContainsKey("merchant_01"), Is.True);
      Assert.That(saveData.ConversationHistories["merchant_01"].PersonaId, Is.EqualTo("merchant_01"));
    }

    [Test]
    public void Metadata_AddEntry_PersistsEntry()
    {
      // Arrange
      var saveData = new SaveData();

      // Act
      saveData.Metadata["game_mode"] = "story";

      // Assert
      Assert.That(saveData.Metadata.ContainsKey("game_mode"), Is.True);
      Assert.That(saveData.Metadata["game_mode"], Is.EqualTo("story"));
    }

    #endregion

    #region Serialization Tests

    [Test]
    public void JsonRoundTrip_EmptySaveData_PreservesDefaults()
    {
      // Arrange
      var original = new SaveData();

      // Act
      var json = JsonConvert.SerializeObject(original);
      var restored = JsonConvert.DeserializeObject<SaveData>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.Version, Is.EqualTo(SaveData.CurrentVersion));
      Assert.That(restored.SavedAtUtcTicks, Is.EqualTo(0L));
      Assert.That(restored.PersonaMemories, Is.Not.Null);
      Assert.That(restored.ConversationHistories, Is.Not.Null);
      Assert.That(restored.Metadata, Is.Not.Null);
    }

    [Test]
    public void JsonRoundTrip_WithAllFields_PreservesValues()
    {
      // Arrange
      var original = new SaveData
      {
        Version = 1,
        SavedAtUtcTicks = 638000000000000000L,
        PersonaMemories = new Dictionary<string, PersonaMemorySnapshot>
        {
          ["guard_01"] = new PersonaMemorySnapshot
          {
            PersonaId = "guard_01",
            NextSequenceNumber = 42
          }
        },
        ConversationHistories = new Dictionary<string, ConversationHistorySnapshot>
        {
          ["guard_01"] = new ConversationHistorySnapshot
          {
            PersonaId = "guard_01",
            Entries = new List<DialogueEntryDto>
            {
              new DialogueEntryDto { Speaker = "Player", Text = "Hello", TimestampTicks = 100L }
            }
          }
        },
        Metadata = new Dictionary<string, string>
        {
          ["difficulty"] = "hard",
          ["chapter"] = "3"
        }
      };

      // Act
      var json = JsonConvert.SerializeObject(original);
      var restored = JsonConvert.DeserializeObject<SaveData>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.Version, Is.EqualTo(1));
      Assert.That(restored.SavedAtUtcTicks, Is.EqualTo(638000000000000000L));
      Assert.That(restored.PersonaMemories["guard_01"].NextSequenceNumber, Is.EqualTo(42));
      Assert.That(restored.ConversationHistories["guard_01"].Entries[0].Text, Is.EqualTo("Hello"));
      Assert.That(restored.Metadata["difficulty"], Is.EqualTo("hard"));
      Assert.That(restored.Metadata["chapter"], Is.EqualTo("3"));
    }

    [Test]
    public void JsonRoundTrip_WithMultiplePersonas_PreservesAll()
    {
      // Arrange
      var original = new SaveData
      {
        SavedAtUtcTicks = 12345L,
        PersonaMemories = new Dictionary<string, PersonaMemorySnapshot>
        {
          ["npc_1"] = new PersonaMemorySnapshot { PersonaId = "npc_1", NextSequenceNumber = 10 },
          ["npc_2"] = new PersonaMemorySnapshot { PersonaId = "npc_2", NextSequenceNumber = 20 },
          ["npc_3"] = new PersonaMemorySnapshot { PersonaId = "npc_3", NextSequenceNumber = 30 }
        }
      };

      // Act
      var json = JsonConvert.SerializeObject(original);
      var restored = JsonConvert.DeserializeObject<SaveData>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.PersonaMemories.Count, Is.EqualTo(3));
      Assert.That(restored.PersonaMemories["npc_1"].NextSequenceNumber, Is.EqualTo(10));
      Assert.That(restored.PersonaMemories["npc_2"].NextSequenceNumber, Is.EqualTo(20));
      Assert.That(restored.PersonaMemories["npc_3"].NextSequenceNumber, Is.EqualTo(30));
    }

    [Test]
    public void JsonDeserialization_MissingFields_UsesDefaults()
    {
      // Arrange
      var json = "{}";

      // Act
      var restored = JsonConvert.DeserializeObject<SaveData>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.Version, Is.EqualTo(0).Or.EqualTo(SaveData.CurrentVersion));
      Assert.That(restored.SavedAtUtcTicks, Is.EqualTo(0L));
    }

    [Test]
    public void JsonDeserialization_PartialFields_PreservesProvidedValues()
    {
      // Arrange
      var json = "{\"Version\":2,\"SavedAtUtcTicks\":999}";

      // Act
      var restored = JsonConvert.DeserializeObject<SaveData>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.Version, Is.EqualTo(2));
      Assert.That(restored.SavedAtUtcTicks, Is.EqualTo(999L));
    }

    [Test]
    public void JsonRoundTrip_SpecialCharactersInMetadata_PreservesValues()
    {
      // Arrange
      var original = new SaveData
      {
        Metadata = new Dictionary<string, string>
        {
          ["player_name"] = "Hero \"The Brave\"",
          ["location"] = "Forest\nClearing",
          ["notes"] = "Special chars: <>&'\""
        }
      };

      // Act
      var json = JsonConvert.SerializeObject(original);
      var restored = JsonConvert.DeserializeObject<SaveData>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.Metadata["player_name"], Is.EqualTo("Hero \"The Brave\""));
      Assert.That(restored.Metadata["location"], Is.EqualTo("Forest\nClearing"));
      Assert.That(restored.Metadata["notes"], Is.EqualTo("Special chars: <>&'\""));
    }

    #endregion

    #region Edge Case Tests

    [Test]
    public void SavedAtUtcTicks_MaxValue_HandlesCorrectly()
    {
      // Arrange
      var saveData = new SaveData { SavedAtUtcTicks = long.MaxValue };

      // Act
      var json = JsonConvert.SerializeObject(saveData);
      var restored = JsonConvert.DeserializeObject<SaveData>(json);

      // Assert
      Assert.That(restored!.SavedAtUtcTicks, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void Version_ZeroValue_HandlesCorrectly()
    {
      // Arrange
      var saveData = new SaveData { Version = 0 };

      // Act
      var json = JsonConvert.SerializeObject(saveData);
      var restored = JsonConvert.DeserializeObject<SaveData>(json);

      // Assert
      Assert.That(restored!.Version, Is.EqualTo(0));
    }

    [Test]
    public void PersonaMemories_LargeDictionary_HandlesCorrectly()
    {
      // Arrange
      var saveData = new SaveData();
      for (int i = 0; i < 100; i++)
      {
        var id = $"npc_{i:D3}";
        saveData.PersonaMemories[id] = new PersonaMemorySnapshot
        {
          PersonaId = id,
          NextSequenceNumber = i
        };
      }

      // Act
      var json = JsonConvert.SerializeObject(saveData);
      var restored = JsonConvert.DeserializeObject<SaveData>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.PersonaMemories.Count, Is.EqualTo(100));
      Assert.That(restored.PersonaMemories["npc_000"].NextSequenceNumber, Is.EqualTo(0));
      Assert.That(restored.PersonaMemories["npc_099"].NextSequenceNumber, Is.EqualTo(99));
    }

    [Test]
    public void Metadata_UnicodeKeys_HandlesCorrectly()
    {
      // Arrange
      var original = new SaveData
      {
        Metadata = new Dictionary<string, string>
        {
          ["日本語キー"] = "Japanese value",
          ["emoji_key"] = "Value with emoji: 🎮"
        }
      };

      // Act
      var json = JsonConvert.SerializeObject(original);
      var restored = JsonConvert.DeserializeObject<SaveData>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.Metadata["日本語キー"], Is.EqualTo("Japanese value"));
      Assert.That(restored.Metadata["emoji_key"], Is.EqualTo("Value with emoji: 🎮"));
    }

    [Test]
    public void ConversationHistories_EmptyEntries_PreservesCorrectly()
    {
      // Arrange
      var original = new SaveData
      {
        ConversationHistories = new Dictionary<string, ConversationHistorySnapshot>
        {
          ["silent_npc"] = new ConversationHistorySnapshot
          {
            PersonaId = "silent_npc",
            Entries = new List<DialogueEntryDto>()
          }
        }
      };

      // Act
      var json = JsonConvert.SerializeObject(original);
      var restored = JsonConvert.DeserializeObject<SaveData>(json);

      // Assert
      Assert.That(restored, Is.Not.Null);
      Assert.That(restored!.ConversationHistories["silent_npc"].Entries, Is.Empty);
    }

    #endregion
  }
}
