using System;

namespace LlamaBrain.Persistence.Dtos
{
  /// <summary>
  /// DTO for serializing WorldStateEntry memory entries.
  /// </summary>
  [Serializable]
  public sealed class WorldStateDto
  {
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// The state key (e.g., "door_castle_main").
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// The current value of this state.
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>
    /// Optional category/tag.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// When this memory was created (UTC ticks).
    /// </summary>
    public long CreatedAtTicks { get; set; }

    /// <summary>
    /// When this state was last modified (UTC ticks).
    /// </summary>
    public long ModifiedAtTicks { get; set; }

    /// <summary>
    /// Number of times this state has been modified.
    /// </summary>
    public int ModificationCount { get; set; }

    /// <summary>
    /// Monotonic sequence number for deterministic ordering.
    /// </summary>
    public long SequenceNumber { get; set; }

    /// <summary>
    /// The mutation source as an integer (MutationSource enum value).
    /// </summary>
    public int Source { get; set; }
  }
}
