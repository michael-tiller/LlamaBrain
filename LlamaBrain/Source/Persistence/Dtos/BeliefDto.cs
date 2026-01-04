using System;

namespace LlamaBrain.Persistence.Dtos
{
  /// <summary>
  /// DTO for serializing BeliefMemoryEntry memory entries.
  /// </summary>
  [Serializable]
  public sealed class BeliefDto
  {
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// The dictionary key used to store this belief in the memory system.
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// The subject of the belief (who/what it's about).
    /// </summary>
    public string Subject { get; set; } = "";

    /// <summary>
    /// The belief content.
    /// </summary>
    public string BeliefContent { get; set; } = "";

    /// <summary>
    /// The belief type as an integer (BeliefType enum value).
    /// </summary>
    public int BeliefType { get; set; }

    /// <summary>
    /// Optional category/tag.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Confidence level (0.0 to 1.0).
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Sentiment value (-1.0 to 1.0).
    /// </summary>
    public float Sentiment { get; set; }

    /// <summary>
    /// Whether this belief has been contradicted.
    /// </summary>
    public bool IsContradicted { get; set; }

    /// <summary>
    /// Evidence or reason for this belief.
    /// </summary>
    public string? Evidence { get; set; }

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
