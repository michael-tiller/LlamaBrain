using System;
using System.Collections.Generic;
using LlamaBrain.Persistence.Dtos;

namespace LlamaBrain.Persistence
{
  /// <summary>
  /// Snapshot of a single persona's complete memory state.
  /// Preserves all determinism-critical fields: SequenceNumber, CreatedAtTicks, etc.
  /// </summary>
  [Serializable]
  public sealed class PersonaMemorySnapshot
  {
    /// <summary>
    /// The persona ID this snapshot belongs to.
    /// </summary>
    public string PersonaId { get; set; } = "";

    /// <summary>
    /// Next sequence number to restore deterministic ordering.
    /// </summary>
    public long NextSequenceNumber { get; set; }

    /// <summary>
    /// Canonical facts (immutable truths).
    /// </summary>
    public List<CanonicalFactDto> CanonicalFacts { get; set; }
        = new List<CanonicalFactDto>();

    /// <summary>
    /// World state entries.
    /// </summary>
    public List<WorldStateDto> WorldState { get; set; }
        = new List<WorldStateDto>();

    /// <summary>
    /// Episodic memories.
    /// </summary>
    public List<EpisodicMemoryDto> EpisodicMemories { get; set; }
        = new List<EpisodicMemoryDto>();

    /// <summary>
    /// Beliefs.
    /// </summary>
    public List<BeliefDto> Beliefs { get; set; }
        = new List<BeliefDto>();
  }
}
