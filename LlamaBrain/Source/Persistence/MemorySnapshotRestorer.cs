using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;
using LlamaBrain.Persistence.Dtos;

namespace LlamaBrain.Persistence
{
  /// <summary>
  /// Restores AuthoritativeMemorySystem state from a PersonaMemorySnapshot.
  /// Uses internal raw insertion APIs for deterministic reconstruction.
  /// Must be in the LlamaBrain assembly to access internal methods.
  /// </summary>
  public static class MemorySnapshotRestorer
  {
    /// <summary>
    /// Restores memory system state from a snapshot.
    /// Clears existing state and restores all entries with their original metadata.
    /// Entries are restored in SequenceNumber order for determinism.
    /// </summary>
    /// <param name="memory">The memory system to restore into</param>
    /// <param name="snapshot">The snapshot to restore from</param>
    public static void RestoreSnapshot(AuthoritativeMemorySystem memory, PersonaMemorySnapshot snapshot)
    {
      if (memory == null) throw new ArgumentNullException(nameof(memory));
      if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

      // Clear existing state
      memory.ClearAll();

      // Restore canonical facts in sequence order
      foreach (var dto in snapshot.CanonicalFacts.OrderBy(f => f.SequenceNumber))
      {
        var entry = FromDto(dto);
        memory.InsertCanonicalFactRaw(entry);
      }

      // Restore world state in sequence order
      foreach (var dto in snapshot.WorldState.OrderBy(s => s.SequenceNumber))
      {
        var entry = FromDto(dto);
        memory.InsertWorldStateRaw(dto.Key, entry);
      }

      // Restore episodic memories in sequence order
      foreach (var dto in snapshot.EpisodicMemories.OrderBy(e => e.SequenceNumber))
      {
        var entry = FromDto(dto);
        memory.InsertEpisodicRaw(entry);
      }

      // Restore beliefs in sequence order
      foreach (var dto in snapshot.Beliefs.OrderBy(b => b.SequenceNumber))
      {
        var entry = FromDto(dto);
        memory.InsertBeliefRaw(dto.Key, entry);
      }

      // Restore sequence counter
      memory.SetNextSequenceNumberRaw(snapshot.NextSequenceNumber);
    }

    /// <summary>
    /// Converts a CanonicalFactDto back to a CanonicalFact.
    /// </summary>
    public static CanonicalFact FromDto(CanonicalFactDto dto)
    {
      var fact = new CanonicalFact(dto.Fact, dto.Domain)
      {
        Id = dto.Id,
        Category = dto.Category,
        CreatedAtTicks = dto.CreatedAtTicks,
        SequenceNumber = dto.SequenceNumber,
        Source = (MutationSource)dto.Source
      };

      if (dto.ContradictionKeywords != null)
      {
        fact.ContradictionKeywords = new List<string>(dto.ContradictionKeywords);
      }

      return fact;
    }

    /// <summary>
    /// Converts a WorldStateDto back to a WorldStateEntry.
    /// </summary>
    public static WorldStateEntry FromDto(WorldStateDto dto)
    {
      var entry = new WorldStateEntry(dto.Key, dto.Value)
      {
        Id = dto.Id,
        Category = dto.Category,
        CreatedAtTicks = dto.CreatedAtTicks,
        SequenceNumber = dto.SequenceNumber,
        Source = (MutationSource)dto.Source
      };

      // Set ModifiedAt using reflection (private setter)
      SetModifiedAt(entry, new DateTime(dto.ModifiedAtTicks, DateTimeKind.Utc));

      // Set ModificationCount using reflection (private setter)
      SetModificationCount(entry, dto.ModificationCount);

      return entry;
    }

    /// <summary>
    /// Converts an EpisodicMemoryDto back to an EpisodicMemoryEntry.
    /// </summary>
    public static EpisodicMemoryEntry FromDto(EpisodicMemoryDto dto)
    {
      return new EpisodicMemoryEntry(dto.Description, (EpisodeType)dto.EpisodeType)
      {
        Id = dto.Id,
        Participant = dto.Participant,
        Category = dto.Category,
        Significance = dto.Significance,
        Strength = dto.Strength,
        GameTime = dto.GameTime,
        CreatedAtTicks = dto.CreatedAtTicks,
        SequenceNumber = dto.SequenceNumber,
        Source = (MutationSource)dto.Source
      };
    }

    /// <summary>
    /// Converts a BeliefDto back to a BeliefMemoryEntry.
    /// </summary>
    public static BeliefMemoryEntry FromDto(BeliefDto dto)
    {
      var entry = new BeliefMemoryEntry(dto.Subject, dto.BeliefContent, (BeliefType)dto.BeliefType)
      {
        Id = dto.Id,
        Category = dto.Category,
        Confidence = dto.Confidence,
        Sentiment = dto.Sentiment,
        Evidence = dto.Evidence,
        CreatedAtTicks = dto.CreatedAtTicks,
        SequenceNumber = dto.SequenceNumber,
        Source = (MutationSource)dto.Source
      };

      // Set IsContradicted using reflection (private setter)
      if (dto.IsContradicted)
      {
        SetIsContradicted(entry, true);
      }

      return entry;
    }

    #region Private Helpers for Setting Private Properties

    private static void SetModifiedAt(WorldStateEntry entry, DateTime value)
    {
      var property = typeof(WorldStateEntry).GetProperty("ModifiedAt", BindingFlags.Public | BindingFlags.Instance);
      var setter = property?.GetSetMethod(nonPublic: true);
      setter?.Invoke(entry, new object[] { value });
    }

    private static void SetModificationCount(WorldStateEntry entry, int value)
    {
      var property = typeof(WorldStateEntry).GetProperty("ModificationCount", BindingFlags.Public | BindingFlags.Instance);
      var setter = property?.GetSetMethod(nonPublic: true);
      setter?.Invoke(entry, new object[] { value });
    }

    private static void SetIsContradicted(BeliefMemoryEntry entry, bool value)
    {
      var property = typeof(BeliefMemoryEntry).GetProperty("IsContradicted", BindingFlags.Public | BindingFlags.Instance);
      var setter = property?.GetSetMethod(nonPublic: true);
      setter?.Invoke(entry, new object[] { value });
    }

    #endregion
  }
}
