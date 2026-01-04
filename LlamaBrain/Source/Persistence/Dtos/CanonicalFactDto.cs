using System;
using System.Collections.Generic;

namespace LlamaBrain.Persistence.Dtos
{
  /// <summary>
  /// DTO for serializing CanonicalFact memory entries.
  /// </summary>
  [Serializable]
  public sealed class CanonicalFactDto
  {
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// The fact content.
    /// </summary>
    public string Fact { get; set; } = "";

    /// <summary>
    /// Optional domain/category for the fact.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Optional category/tag.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Keywords that indicate potential contradictions.
    /// </summary>
    public List<string>? ContradictionKeywords { get; set; }

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
