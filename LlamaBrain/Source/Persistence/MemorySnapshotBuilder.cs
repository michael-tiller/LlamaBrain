using System;
using System.Collections.Generic;
using System.Linq;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;
using LlamaBrain.Persistence.Dtos;

namespace LlamaBrain.Persistence
{
  /// <summary>
  /// Builds PersonaMemorySnapshot from AuthoritativeMemorySystem state.
  /// Uses public APIs only - can be used from any assembly.
  /// </summary>
  public static class MemorySnapshotBuilder
  {
    /// <summary>
    /// Creates a snapshot of the memory system's current state.
    /// All entries are sorted by SequenceNumber for deterministic serialization.
    /// </summary>
    /// <param name="memory">The memory system to snapshot</param>
    /// <param name="personaId">The persona ID for this snapshot</param>
    /// <param name="interactionCount">Optional interaction count for deterministic seed restoration (Feature 14)</param>
    /// <returns>A complete snapshot of the memory state</returns>
    public static PersonaMemorySnapshot CreateSnapshot(AuthoritativeMemorySystem memory, string personaId, int interactionCount = 0)
    {
      if (memory == null) throw new ArgumentNullException(nameof(memory));
      if (string.IsNullOrEmpty(personaId)) throw new ArgumentException("Persona ID cannot be null or empty", nameof(personaId));

      return new PersonaMemorySnapshot
      {
        PersonaId = personaId,
        NextSequenceNumber = memory.NextSequenceNumber,
        InteractionCount = interactionCount,
        CanonicalFacts = memory.GetCanonicalFacts()
            .OrderBy(f => f.SequenceNumber)
            .Select(ToDto)
            .ToList(),
        WorldState = memory.GetWorldStateEntries()
            .OrderBy(kvp => kvp.Value.SequenceNumber)
            .Select(kvp => ToDto(kvp.Key, kvp.Value))
            .ToList(),
        EpisodicMemories = memory.GetActiveEpisodicMemories(0f) // Get all, including weak ones
            .OrderBy(e => e.SequenceNumber)
            .Select(ToDto)
            .ToList(),
        Beliefs = memory.GetBeliefEntries()
            .OrderBy(kvp => kvp.Value.SequenceNumber)
            .Select(kvp => ToDto(kvp.Key, kvp.Value))
            .ToList()
      };
    }

    /// <summary>
    /// Converts a CanonicalFact to its DTO form.
    /// </summary>
    public static CanonicalFactDto ToDto(CanonicalFact fact)
    {
      return new CanonicalFactDto
      {
        Id = fact.Id,
        Fact = fact.Fact,
        Domain = fact.Domain,
        Category = fact.Category,
        ContradictionKeywords = fact.ContradictionKeywords != null
            ? new List<string>(fact.ContradictionKeywords)
            : null,
        CreatedAtTicks = fact.CreatedAtTicks,
        SequenceNumber = fact.SequenceNumber,
        Source = (int)fact.Source
      };
    }

    /// <summary>
    /// Converts a WorldStateEntry to its DTO form.
    /// </summary>
    public static WorldStateDto ToDto(string key, WorldStateEntry entry)
    {
      return new WorldStateDto
      {
        Id = entry.Id,
        Key = key,
        Value = entry.Value,
        Category = entry.Category,
        CreatedAtTicks = entry.CreatedAtTicks,
        ModifiedAtTicks = entry.ModifiedAt.ToUniversalTime().Ticks,
        ModificationCount = entry.ModificationCount,
        SequenceNumber = entry.SequenceNumber,
        Source = (int)entry.Source
      };
    }

    /// <summary>
    /// Converts an EpisodicMemoryEntry to its DTO form.
    /// </summary>
    public static EpisodicMemoryDto ToDto(EpisodicMemoryEntry entry)
    {
      return new EpisodicMemoryDto
      {
        Id = entry.Id,
        Description = entry.Description,
        EpisodeType = (int)entry.EpisodeType,
        Participant = entry.Participant,
        Category = entry.Category,
        Significance = entry.Significance,
        Strength = entry.Strength,
        GameTime = entry.GameTime,
        CreatedAtTicks = entry.CreatedAtTicks,
        SequenceNumber = entry.SequenceNumber,
        Source = (int)entry.Source
      };
    }

    /// <summary>
    /// Converts a BeliefMemoryEntry to its DTO form.
    /// </summary>
    public static BeliefDto ToDto(string key, BeliefMemoryEntry entry)
    {
      return new BeliefDto
      {
        Id = entry.Id,
        Key = key,
        Subject = entry.Subject,
        BeliefContent = entry.BeliefContent,
        BeliefType = (int)entry.BeliefType,
        Category = entry.Category,
        Confidence = entry.Confidence,
        Sentiment = entry.Sentiment,
        IsContradicted = entry.IsContradicted,
        Evidence = entry.Evidence,
        CreatedAtTicks = entry.CreatedAtTicks,
        SequenceNumber = entry.SequenceNumber,
        Source = (int)entry.Source
      };
    }
  }
}
