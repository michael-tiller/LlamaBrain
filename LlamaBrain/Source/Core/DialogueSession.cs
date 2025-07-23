using System;
using System.Collections.Generic;
using LlamaBrain.Persona;

namespace LlamaBrain.Core
{
  /// <summary>
  /// Tracks the conversation history between participants, typically a player and an NPC.
  /// </summary>
  public class DialogueSession
  {
    /// <summary>
    /// The ID of the persona.
    /// </summary>
    public string PersonaId { get; }

    /// <summary>
    /// The dialogue history.
    /// </summary>
    private readonly List<DialogueEntry> _history = new List<DialogueEntry>();

    /// <summary>
    /// The memory store for the persona.
    /// </summary>
    private readonly PersonaMemoryStore? _memoryStore;

    /// <summary>
    /// Maximum number of history entries to keep in memory.
    /// </summary>
    private const int MaxHistoryEntries = 50;

    /// <summary>
    /// Initializes a new instance of the DialogueSession class.
    /// </summary>
    /// <param name="personaId">The ID of the persona.</param>
    /// <param name="memoryStore">Optional memory store for the persona.</param>
    public DialogueSession(string personaId, PersonaMemoryStore? memoryStore = null)
    {
      if (string.IsNullOrWhiteSpace(personaId))
        throw new ArgumentException("PersonaId cannot be null or empty", nameof(personaId));

      PersonaId = personaId;
      _memoryStore = memoryStore;
    }

    /// <summary>
    /// Appends a message to the dialogue history with a generic speaker.
    /// </summary>
    /// <param name="speaker">The speaker of the message.</param>
    /// <param name="text">The message text.</param>
    /// <param name="timestamp">Optional timestamp for the message.</param>
    public void Append(string speaker, string text, DateTime? timestamp = null)
    {
      if (string.IsNullOrWhiteSpace(speaker))
        throw new ArgumentException("Speaker cannot be null or empty", nameof(speaker));

      if (string.IsNullOrWhiteSpace(text))
        throw new ArgumentException("Text cannot be null or empty", nameof(text));

      var entry = new DialogueEntry
      {
        Speaker = speaker,
        Text = text,
        Timestamp = timestamp ?? DateTime.Now
      };

      _history.Add(entry);

      // Keep history within limits
      if (_history.Count > MaxHistoryEntries)
      {
        _history.RemoveAt(0);
      }
    }

    /// <summary>
    /// Appends a player message to the dialogue history.
    /// </summary>
    /// <param name="input">The player message.</param>
    /// <param name="timestamp">Optional timestamp for the message.</param>
    public void AppendPlayer(string input, DateTime? timestamp = null)
    {
      Append("Player", input, timestamp);
    }

    /// <summary>
    /// Appends an NPC message to the dialogue history.
    /// </summary>
    /// <param name="response">The NPC message.</param>
    /// <param name="timestamp">Optional timestamp for the message.</param>
    public void AppendNpc(string response, DateTime? timestamp = null)
    {
      Append("NPC", response, timestamp);
    }

    /// <summary>
    /// Gets the dialogue history as formatted strings.
    /// </summary>
    /// <returns>The dialogue history as formatted strings.</returns>
    public IReadOnlyList<string> GetHistory()
    {
      var formattedHistory = new List<string>();
      foreach (var entry in _history)
      {
        formattedHistory.Add($"{entry.Speaker}: {entry.Text}");
      }
      return formattedHistory;
    }

    /// <summary>
    /// Gets the dialogue history as structured entries.
    /// </summary>
    /// <returns>The dialogue history as structured entries.</returns>
    public IReadOnlyList<DialogueEntry> GetHistoryEntries()
    {
      return _history.AsReadOnly();
    }

    /// <summary>
    /// Gets the recent dialogue history (last N entries).
    /// </summary>
    /// <param name="count">The number of recent entries to retrieve.</param>
    /// <returns>The recent dialogue history.</returns>
    public IReadOnlyList<DialogueEntry> GetRecentHistory(int count)
    {
      if (count <= 0)
        return Array.Empty<DialogueEntry>();

      var startIndex = Math.Max(0, _history.Count - count);
      return _history.GetRange(startIndex, _history.Count - startIndex).AsReadOnly();
    }

    /// <summary>
    /// Gets the memory for the persona.
    /// </summary>
    /// <returns>The memory for the persona, or empty list if no memory store.</returns>
    public IReadOnlyList<string> GetMemory()
    {
      return _memoryStore?.GetMemory(PersonaId) ?? Array.Empty<string>();
    }

    /// <summary>
    /// Clears the dialogue history.
    /// </summary>
    public void Clear()
    {
      _history.Clear();
    }

    /// <summary>
    /// Clears the memory for the persona.
    /// </summary>
    public void ClearMemory()
    {
      _memoryStore?.ClearMemory(PersonaId);
    }

    /// <summary>
    /// Gets the total number of dialogue entries.
    /// </summary>
    /// <returns>The total number of dialogue entries.</returns>
    public int GetEntryCount()
    {
      return _history.Count;
    }

    /// <summary>
    /// Gets the last dialogue entry.
    /// </summary>
    /// <returns>The last dialogue entry, or null if no entries exist.</returns>
    public DialogueEntry? GetLastEntry()
    {
      return _history.Count > 0 ? _history[_history.Count - 1] : null;
    }

    /// <summary>
    /// Gets the last message from a specific speaker.
    /// </summary>
    /// <param name="speaker">The speaker to search for.</param>
    /// <returns>The last message from the speaker, or null if not found.</returns>
    public DialogueEntry? GetLastEntryFromSpeaker(string speaker)
    {
      if (string.IsNullOrWhiteSpace(speaker))
        return null;

      for (int i = _history.Count - 1; i >= 0; i--)
      {
        if (_history[i].Speaker.Equals(speaker, StringComparison.OrdinalIgnoreCase))
        {
          return _history[i];
        }
      }
      return null;
    }
  }

  /// <summary>
  /// Represents a single dialogue entry with speaker, text, and timestamp.
  /// </summary>
  public class DialogueEntry
  {
    /// <summary>
    /// The speaker of the message.
    /// </summary>
    public string Speaker { get; set; } = string.Empty;

    /// <summary>
    /// The text of the message.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when the message was added.
    /// </summary>
    public DateTime Timestamp { get; set; }
  }
}