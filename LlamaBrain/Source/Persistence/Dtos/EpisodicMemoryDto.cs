using System;

namespace LlamaBrain.Persistence.Dtos
{
  /// <summary>
  /// DTO for serializing EpisodicMemoryEntry memory entries.
  /// </summary>
  [Serializable]
  public sealed class EpisodicMemoryDto
  {
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// The description of what happened.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// The episode type as an integer (EpisodeType enum value).
    /// </summary>
    public int EpisodeType { get; set; }

    /// <summary>
    /// Who was involved (e.g., "Player").
    /// </summary>
    public string? Participant { get; set; }

    /// <summary>
    /// Optional category/tag.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Emotional significance (0.0 to 1.0).
    /// </summary>
    public float Significance { get; set; }

    /// <summary>
    /// Current strength of this memory (1.0 = fresh, decays toward 0).
    /// </summary>
    public float Strength { get; set; }

    /// <summary>
    /// Game time when this episode occurred.
    /// </summary>
    public float GameTime { get; set; }

    /// <summary>
    /// When this memory was created (UTC ticks).
    /// </summary>
    public long CreatedAtTicks { get; set; }

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
