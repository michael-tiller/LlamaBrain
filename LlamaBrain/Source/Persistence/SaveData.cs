using System;
using System.Collections.Generic;

namespace LlamaBrain.Persistence
{
  /// <summary>
  /// Top-level save data container. Immutable record for serialization.
  /// Contains all persona memories and conversation histories.
  /// </summary>
  [Serializable]
  public sealed class SaveData
  {
    /// <summary>
    /// Current save data schema version.
    /// </summary>
    public const int CurrentVersion = 1;

    /// <summary>
    /// Schema version for forward/backward compatibility.
    /// </summary>
    public int Version { get; set; } = CurrentVersion;

    /// <summary>
    /// When this save was created (UTC ticks for determinism).
    /// </summary>
    public long SavedAtUtcTicks { get; set; }

    /// <summary>
    /// Memory state for all personas, keyed by persona ID.
    /// </summary>
    public Dictionary<string, PersonaMemorySnapshot> PersonaMemories { get; set; }
        = new Dictionary<string, PersonaMemorySnapshot>();

    /// <summary>
    /// Conversation history per persona.
    /// </summary>
    public Dictionary<string, ConversationHistorySnapshot> ConversationHistories { get; set; }
        = new Dictionary<string, ConversationHistorySnapshot>();

    /// <summary>
    /// Extensible metadata for future additions.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; }
        = new Dictionary<string, string>();

    /// <summary>
    /// Creates a new SaveData with the current timestamp.
    /// </summary>
    /// <returns>A new SaveData instance with SavedAtUtcTicks set to now</returns>
    public static SaveData CreateNew() => new SaveData
    {
      SavedAtUtcTicks = DateTimeOffset.UtcNow.UtcTicks
    };
  }
}
