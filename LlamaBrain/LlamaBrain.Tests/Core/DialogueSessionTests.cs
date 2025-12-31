using System;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core;
using LlamaBrain.Persona;

namespace LlamaBrain.Tests.Core
{
  /// <summary>
  /// Tests for DialogueSession conversation tracking
  /// </summary>
  [TestFixture]
  public class DialogueSessionTests
  {
    private PersonaMemoryStore _memoryStore = null!;

    [SetUp]
    public void SetUp()
    {
      _memoryStore = new PersonaMemoryStore();
    }

    #region Constructor Tests

    [Test]
    public void DialogueSession_Constructor_WithValidPersonaId_CreatesCorrectly()
    {
      // Arrange & Act
      var session = new DialogueSession("test-persona");

      // Assert
      Assert.IsNotNull(session);
      Assert.AreEqual("test-persona", session.PersonaId);
      Assert.AreEqual(0, session.GetEntryCount());
    }

    [Test]
    public void DialogueSession_Constructor_WithMemoryStore_CreatesCorrectly()
    {
      // Arrange & Act
      var session = new DialogueSession("test-persona", _memoryStore);

      // Assert
      Assert.IsNotNull(session);
      Assert.AreEqual("test-persona", session.PersonaId);
    }

    [Test]
    public void DialogueSession_Constructor_WithNullPersonaId_ThrowsArgumentException()
    {
      // Arrange, Act & Assert
      Assert.Throws<ArgumentException>(() => new DialogueSession(null!));
    }

    [Test]
    public void DialogueSession_Constructor_WithEmptyPersonaId_ThrowsArgumentException()
    {
      // Arrange, Act & Assert
      Assert.Throws<ArgumentException>(() => new DialogueSession(string.Empty));
    }

    [Test]
    public void DialogueSession_Constructor_WithWhitespacePersonaId_ThrowsArgumentException()
    {
      // Arrange, Act & Assert
      Assert.Throws<ArgumentException>(() => new DialogueSession("   "));
    }

    #endregion

    #region Append Tests

    [Test]
    public void Append_WithValidParameters_AddsEntry()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      var timestamp = DateTime.Now;

      // Act
      session.Append("Player", "Hello", timestamp);

      // Assert
      Assert.AreEqual(1, session.GetEntryCount());
      var entry = session.GetLastEntry();
      Assert.IsNotNull(entry);
      Assert.AreEqual("Player", entry!.Speaker);
      Assert.AreEqual("Hello", entry.Text);
      Assert.AreEqual(timestamp, entry.Timestamp);
    }

    [Test]
    public void Append_WithoutTimestamp_UsesCurrentTime()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      var beforeAppend = DateTime.Now;

      // Act
      session.Append("Player", "Hello");
      var afterAppend = DateTime.Now;

      // Assert
      var entry = session.GetLastEntry();
      Assert.IsNotNull(entry);
      Assert.GreaterOrEqual(entry!.Timestamp, beforeAppend);
      Assert.LessOrEqual(entry.Timestamp, afterAppend);
    }

    [Test]
    public void Append_WithNullSpeaker_ThrowsArgumentException()
    {
      // Arrange
      var session = new DialogueSession("test-persona");

      // Act & Assert
      Assert.Throws<ArgumentException>(() => session.Append(null!, "Hello"));
    }

    [Test]
    public void Append_WithEmptySpeaker_ThrowsArgumentException()
    {
      // Arrange
      var session = new DialogueSession("test-persona");

      // Act & Assert
      Assert.Throws<ArgumentException>(() => session.Append(string.Empty, "Hello"));
    }

    [Test]
    public void Append_WithNullText_ThrowsArgumentException()
    {
      // Arrange
      var session = new DialogueSession("test-persona");

      // Act & Assert
      Assert.Throws<ArgumentException>(() => session.Append("Player", null!));
    }

    [Test]
    public void Append_WithEmptyText_ThrowsArgumentException()
    {
      // Arrange
      var session = new DialogueSession("test-persona");

      // Act & Assert
      Assert.Throws<ArgumentException>(() => session.Append("Player", string.Empty));
    }

    [Test]
    public void AppendPlayer_AddsPlayerEntry()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      var timestamp = DateTime.Now;

      // Act
      session.AppendPlayer("Hello there", timestamp);

      // Assert
      Assert.AreEqual(1, session.GetEntryCount());
      var entry = session.GetLastEntry();
      Assert.IsNotNull(entry);
      Assert.AreEqual("Player", entry!.Speaker);
      Assert.AreEqual("Hello there", entry.Text);
      Assert.AreEqual(timestamp, entry.Timestamp);
    }

    [Test]
    public void AppendNpc_AddsNpcEntry()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      var timestamp = DateTime.Now;

      // Act
      session.AppendNpc("How can I help you?", timestamp);

      // Assert
      Assert.AreEqual(1, session.GetEntryCount());
      var entry = session.GetLastEntry();
      Assert.IsNotNull(entry);
      Assert.AreEqual("NPC", entry!.Speaker);
      Assert.AreEqual("How can I help you?", entry.Text);
      Assert.AreEqual(timestamp, entry.Timestamp);
    }

    [Test]
    public void Append_MultipleEntries_MaintainsOrder()
    {
      // Arrange
      var session = new DialogueSession("test-persona");

      // Act
      session.AppendPlayer("First");
      session.AppendNpc("Second");
      session.AppendPlayer("Third");

      // Assert
      Assert.AreEqual(3, session.GetEntryCount());
      var entries = session.GetHistoryEntries().ToList();
      Assert.AreEqual("First", entries[0].Text);
      Assert.AreEqual("Second", entries[1].Text);
      Assert.AreEqual("Third", entries[2].Text);
    }

    #endregion

    #region History Retrieval Tests

    [Test]
    public void GetHistory_WithNoEntries_ReturnsEmptyList()
    {
      // Arrange
      var session = new DialogueSession("test-persona");

      // Act
      var history = session.GetHistory();

      // Assert
      Assert.IsNotNull(history);
      Assert.AreEqual(0, history.Count);
    }

    [Test]
    public void GetHistory_WithEntries_ReturnsFormattedStrings()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      session.AppendPlayer("Hello");
      session.AppendNpc("Hi there");

      // Act
      var history = session.GetHistory().ToList();

      // Assert
      Assert.AreEqual(2, history.Count);
      Assert.AreEqual("Player: Hello", history[0]);
      Assert.AreEqual("NPC: Hi there", history[1]);
    }

    [Test]
    public void GetHistoryEntries_WithNoEntries_ReturnsEmptyList()
    {
      // Arrange
      var session = new DialogueSession("test-persona");

      // Act
      var entries = session.GetHistoryEntries();

      // Assert
      Assert.IsNotNull(entries);
      Assert.AreEqual(0, entries.Count);
    }

    [Test]
    public void GetHistoryEntries_WithEntries_ReturnsStructuredEntries()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      var timestamp = DateTime.Now;
      session.Append("Player", "Hello", timestamp);

      // Act
      var entries = session.GetHistoryEntries().ToList();

      // Assert
      Assert.AreEqual(1, entries.Count);
      Assert.AreEqual("Player", entries[0].Speaker);
      Assert.AreEqual("Hello", entries[0].Text);
      Assert.AreEqual(timestamp, entries[0].Timestamp);
    }

    [Test]
    public void GetRecentHistory_WithZeroCount_ReturnsEmptyList()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      session.AppendPlayer("Hello");
      session.AppendNpc("Hi");

      // Act
      var recent = session.GetRecentHistory(0);

      // Assert
      Assert.IsNotNull(recent);
      Assert.AreEqual(0, recent.Count);
    }

    [Test]
    public void GetRecentHistory_WithNegativeCount_ReturnsEmptyList()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      session.AppendPlayer("Hello");
      session.AppendNpc("Hi");

      // Act
      var recent = session.GetRecentHistory(-1);

      // Assert
      Assert.IsNotNull(recent);
      Assert.AreEqual(0, recent.Count);
    }

    [Test]
    public void GetRecentHistory_WithCountLessThanTotal_ReturnsLastNEntries()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      session.AppendPlayer("First");
      session.AppendNpc("Second");
      session.AppendPlayer("Third");
      session.AppendNpc("Fourth");

      // Act
      var recent = session.GetRecentHistory(2).ToList();

      // Assert
      Assert.AreEqual(2, recent.Count);
      Assert.AreEqual("Third", recent[0].Text);
      Assert.AreEqual("Fourth", recent[1].Text);
    }

    [Test]
    public void GetRecentHistory_WithCountGreaterThanTotal_ReturnsAllEntries()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      session.AppendPlayer("First");
      session.AppendNpc("Second");

      // Act
      var recent = session.GetRecentHistory(10).ToList();

      // Assert
      Assert.AreEqual(2, recent.Count);
      Assert.AreEqual("First", recent[0].Text);
      Assert.AreEqual("Second", recent[1].Text);
    }

    #endregion

    #region Entry Count and Retrieval Tests

    [Test]
    public void GetEntryCount_WithNoEntries_ReturnsZero()
    {
      // Arrange
      var session = new DialogueSession("test-persona");

      // Act
      var count = session.GetEntryCount();

      // Assert
      Assert.AreEqual(0, count);
    }

    [Test]
    public void GetEntryCount_WithEntries_ReturnsCorrectCount()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      session.AppendPlayer("One");
      session.AppendNpc("Two");
      session.AppendPlayer("Three");

      // Act
      var count = session.GetEntryCount();

      // Assert
      Assert.AreEqual(3, count);
    }

    [Test]
    public void GetLastEntry_WithNoEntries_ReturnsNull()
    {
      // Arrange
      var session = new DialogueSession("test-persona");

      // Act
      var entry = session.GetLastEntry();

      // Assert
      Assert.IsNull(entry);
    }

    [Test]
    public void GetLastEntry_WithEntries_ReturnsMostRecentEntry()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      session.AppendPlayer("First");
      session.AppendNpc("Second");
      session.AppendPlayer("Third");

      // Act
      var entry = session.GetLastEntry();

      // Assert
      Assert.IsNotNull(entry);
      Assert.AreEqual("Player", entry!.Speaker);
      Assert.AreEqual("Third", entry.Text);
    }

    [Test]
    public void GetLastEntryFromSpeaker_WithNoEntries_ReturnsNull()
    {
      // Arrange
      var session = new DialogueSession("test-persona");

      // Act
      var entry = session.GetLastEntryFromSpeaker("Player");

      // Assert
      Assert.IsNull(entry);
    }

    [Test]
    public void GetLastEntryFromSpeaker_WithMatchingSpeaker_ReturnsLastEntry()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      session.AppendPlayer("First player");
      session.AppendNpc("NPC response");
      session.AppendPlayer("Second player");
      session.AppendNpc("Another NPC");

      // Act
      var entry = session.GetLastEntryFromSpeaker("Player");

      // Assert
      Assert.IsNotNull(entry);
      Assert.AreEqual("Player", entry!.Speaker);
      Assert.AreEqual("Second player", entry.Text);
    }

    [Test]
    public void GetLastEntryFromSpeaker_WithCaseInsensitiveMatch_ReturnsLastEntry()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      session.Append("Player", "First");
      session.Append("player", "Second");
      session.Append("PLAYER", "Third");

      // Act
      var entry = session.GetLastEntryFromSpeaker("player");

      // Assert
      Assert.IsNotNull(entry);
      Assert.AreEqual("PLAYER", entry!.Speaker); // Original case preserved
      Assert.AreEqual("Third", entry.Text);
    }

    [Test]
    public void GetLastEntryFromSpeaker_WithNullSpeaker_ReturnsNull()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      session.AppendPlayer("Hello");

      // Act
      var entry = session.GetLastEntryFromSpeaker(null!);

      // Assert
      Assert.IsNull(entry);
    }

    [Test]
    public void GetLastEntryFromSpeaker_WithEmptySpeaker_ReturnsNull()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      session.AppendPlayer("Hello");

      // Act
      var entry = session.GetLastEntryFromSpeaker(string.Empty);

      // Assert
      Assert.IsNull(entry);
    }

    [Test]
    public void GetLastEntryFromSpeaker_WithNonExistentSpeaker_ReturnsNull()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      session.AppendPlayer("Hello");
      session.AppendNpc("Hi");

      // Act
      var entry = session.GetLastEntryFromSpeaker("Unknown");

      // Assert
      Assert.IsNull(entry);
    }

    #endregion

    #region History Limit Tests

    [Test]
    public void Append_ExceedingMaxHistoryEntries_RemovesOldestEntries()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      const int maxEntries = 50;

      // Act - Add more than max entries
      for (int i = 0; i < maxEntries + 10; i++)
      {
        session.AppendPlayer($"Message {i}");
      }

      // Assert
      Assert.AreEqual(maxEntries, session.GetEntryCount());
      var entries = session.GetHistoryEntries().ToList();
      // First entry should be "Message 10" (oldest entries removed)
      Assert.AreEqual("Message 10", entries[0].Text);
      // Last entry should be "Message 59"
      Assert.AreEqual($"Message {maxEntries + 9}", entries[maxEntries - 1].Text);
    }

    #endregion

    #region Clear Tests

    [Test]
    public void Clear_WithEntries_RemovesAllEntries()
    {
      // Arrange
      var session = new DialogueSession("test-persona");
      session.AppendPlayer("Hello");
      session.AppendNpc("Hi");
      session.AppendPlayer("How are you?");

      // Act
      session.Clear();

      // Assert
      Assert.AreEqual(0, session.GetEntryCount());
      Assert.IsNull(session.GetLastEntry());
      Assert.AreEqual(0, session.GetHistory().Count);
    }

    [Test]
    public void Clear_WithNoEntries_DoesNothing()
    {
      // Arrange
      var session = new DialogueSession("test-persona");

      // Act
      session.Clear();

      // Assert
      Assert.AreEqual(0, session.GetEntryCount());
    }

    #endregion

    #region Memory Integration Tests

    [Test]
    public void GetMemory_WithoutMemoryStore_ReturnsEmptyList()
    {
      // Arrange
      var session = new DialogueSession("test-persona");

      // Act
      var memory = session.GetMemory();

      // Assert
      Assert.IsNotNull(memory);
      Assert.AreEqual(0, memory.Count);
    }

    [Test]
    public void GetMemory_WithMemoryStore_ReturnsMemoryFromStore()
    {
      // Arrange
      var session = new DialogueSession("test-persona", _memoryStore);
      _memoryStore.AddMemory("test-persona", "Remember this fact");

      // Act
      var memory = session.GetMemory();

      // Assert
      Assert.IsNotNull(memory);
      Assert.Greater(memory.Count, 0);
    }

    [Test]
    public void ClearMemory_WithoutMemoryStore_DoesNotThrow()
    {
      // Arrange
      var session = new DialogueSession("test-persona");

      // Act & Assert
      Assert.DoesNotThrow(() => session.ClearMemory());
    }

    [Test]
    public void ClearMemory_WithMemoryStore_ClearsMemory()
    {
      // Arrange
      var session = new DialogueSession("test-persona", _memoryStore);
      _memoryStore.AddMemory("test-persona", "Remember this fact");

      // Act
      session.ClearMemory();

      // Assert
      var memory = session.GetMemory();
      Assert.AreEqual(0, memory.Count);
    }

    #endregion
  }
}

