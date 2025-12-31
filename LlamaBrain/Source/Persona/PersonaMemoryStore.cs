using System;
using System.Collections.Generic;
using System.Linq;
using LlamaBrain.Persona.MemoryTypes;

namespace LlamaBrain.Persona
{
  /// <summary>
  /// Store for persona memory using the AuthoritativeMemorySystem.
  /// Provides convenience methods that map to structured memory types.
  /// </summary>
  public class PersonaMemoryStore
  {
    /// <summary>
    /// The authoritative memory systems, keyed by persona ID.
    /// </summary>
    private readonly Dictionary<string, AuthoritativeMemorySystem> _memorySystems = new Dictionary<string, AuthoritativeMemorySystem>();

    /// <summary>
    /// Maximum episodic memories per persona.
    /// </summary>
    public int MaxEpisodicMemories { get; set; } = 100;

    /// <summary>
    /// Gets or creates the authoritative memory system for a persona.
    /// If no system exists for the given persona ID, a new one is created with the current MaxEpisodicMemories setting.
    /// </summary>
    /// <param name="personaId">The unique identifier of the persona</param>
    /// <returns>The authoritative memory system for the persona, creating a new one if necessary</returns>
    public AuthoritativeMemorySystem GetOrCreateSystem(string personaId)
    {
      if (!_memorySystems.TryGetValue(personaId, out var system))
      {
        system = new AuthoritativeMemorySystem
        {
          MaxEpisodicMemories = MaxEpisodicMemories
        };
        _memorySystems[personaId] = system;
      }
      return system;
    }

    /// <summary>
    /// Gets the authoritative memory system for a persona (if exists).
    /// Returns null if no memory system has been created for the given persona ID.
    /// </summary>
    /// <param name="personaId">The unique identifier of the persona</param>
    /// <returns>The authoritative memory system for the persona, or null if not found</returns>
    public AuthoritativeMemorySystem? GetSystem(string personaId)
    {
      _memorySystems.TryGetValue(personaId, out var system);
      return system;
    }

    #region Convenience API (Maps to Structured Memory)

    /// <summary>
    /// Add a memory to the store.
    /// Maps to episodic memory with default significance.
    /// </summary>
    /// <param name="personaId">The ID of the persona</param>
    /// <param name="message">The message to add</param>
    public void AddMemory(string personaId, string message)
    {
      var system = GetOrCreateSystem(personaId);
      var entry = new EpisodicMemoryEntry(message, EpisodeType.LearnedInfo);
      system.AddEpisodicMemory(entry, MutationSource.ValidatedOutput);
    }

    /// <summary>
    /// Add a memory to the store using a PersonaProfile.
    /// </summary>
    /// <param name="profile">The persona profile</param>
    /// <param name="message">The message to add</param>
    /// <exception cref="ArgumentException">Thrown when profile is null or has an invalid PersonaId</exception>
    public void AddMemory(PersonaProfile profile, string message)
    {
      if (profile == null)
        throw new ArgumentException("PersonaProfile cannot be null", nameof(profile));

      if (string.IsNullOrEmpty(profile.PersonaId))
        throw new ArgumentException("PersonaProfile must have a valid PersonaId", nameof(profile));

      AddMemory(profile.PersonaId, message);
    }

    /// <summary>
    /// Get the memory for a persona.
    /// Returns all memory types formatted for prompts.
    /// </summary>
    /// <param name="personaId">The ID of the persona</param>
    /// <returns>The memory for the persona</returns>
    public IReadOnlyList<string> GetMemory(string personaId)
    {
      var system = GetSystem(personaId);
      if (system == null) return Array.Empty<string>();

      return system.GetAllMemoriesForPrompt(maxEpisodic: 20).ToList();
    }

    /// <summary>
    /// Get only episodic memories for a persona.
    /// Returns raw episodic memory content (without formatting prefixes).
    /// </summary>
    /// <param name="personaId">The ID of the persona</param>
    /// <param name="maxCount">Maximum number of recent memories to return (default: 20)</param>
    /// <returns>Raw episodic memory content</returns>
    public IReadOnlyList<string> GetEpisodicMemories(string personaId, int maxCount = 20)
    {
      var system = GetSystem(personaId);
      if (system == null) return Array.Empty<string>();

      return system.GetRecentMemories(maxCount).Select(m => m.Content).ToList();
    }

    /// <summary>
    /// Get only episodic memories for a persona using a PersonaProfile.
    /// Returns raw episodic memory content (without formatting prefixes).
    /// </summary>
    /// <param name="profile">The persona profile</param>
    /// <param name="maxCount">Maximum number of recent memories to return (default: 20)</param>
    /// <returns>Raw episodic memory content</returns>
    /// <exception cref="ArgumentException">Thrown when profile is null or has an invalid PersonaId</exception>
    public IReadOnlyList<string> GetEpisodicMemories(PersonaProfile profile, int maxCount = 20)
    {
      if (profile == null)
        throw new ArgumentException("PersonaProfile cannot be null", nameof(profile));

      if (string.IsNullOrEmpty(profile.PersonaId))
        throw new ArgumentException("PersonaProfile must have a valid PersonaId", nameof(profile));

      return GetEpisodicMemories(profile.PersonaId, maxCount);
    }

    /// <summary>
    /// Get the memory for a persona using a PersonaProfile.
    /// </summary>
    /// <param name="profile">The persona profile</param>
    /// <returns>The memory for the persona</returns>
    /// <exception cref="ArgumentException">Thrown when profile is null or has an invalid PersonaId</exception>
    public IReadOnlyList<string> GetMemory(PersonaProfile profile)
    {
      if (profile == null)
        throw new ArgumentException("PersonaProfile cannot be null", nameof(profile));

      if (string.IsNullOrEmpty(profile.PersonaId))
        throw new ArgumentException("PersonaProfile must have a valid PersonaId", nameof(profile));

      return GetMemory(profile.PersonaId);
    }

    /// <summary>
    /// Clears the memory of the persona.
    /// </summary>
    /// <param name="personaId">The ID of the persona.</param>
    public void ClearMemory(string personaId)
    {
      var system = GetSystem(personaId);
      system?.ClearAll();
    }

    /// <summary>
    /// Clears the memory of the persona using a PersonaProfile.
    /// </summary>
    /// <param name="profile">The persona profile.</param>
    /// <exception cref="ArgumentException">Thrown when profile is null or has an invalid PersonaId</exception>
    public void ClearMemory(PersonaProfile profile)
    {
      if (profile == null)
        throw new ArgumentException("PersonaProfile cannot be null", nameof(profile));

      if (string.IsNullOrEmpty(profile.PersonaId))
        throw new ArgumentException("PersonaProfile must have a valid PersonaId", nameof(profile));

      ClearMemory(profile.PersonaId);
    }

    #endregion

    #region Structured Memory API

    /// <summary>
    /// Adds a canonical fact (immutable truth) to the persona's memory system.
    /// Canonical facts are immutable and cannot be modified or removed once added.
    /// </summary>
    /// <param name="personaId">The unique identifier of the persona</param>
    /// <param name="factId">The unique identifier for this canonical fact</param>
    /// <param name="fact">The immutable fact content</param>
    /// <param name="domain">Optional domain/category for organizing facts (e.g., "world", "character", "lore")</param>
    /// <returns>A mutation result indicating success or failure with details</returns>
    public MutationResult AddCanonicalFact(string personaId, string factId, string fact, string? domain = null)
    {
      var system = GetOrCreateSystem(personaId);
      return system.AddCanonicalFact(factId, fact, domain);
    }

    /// <summary>
    /// Adds a canonical fact to all existing personas (world-level facts).
    /// Useful for immutable world truths that all NPCs should know.
    /// </summary>
    /// <param name="factId">The unique identifier for this canonical fact</param>
    /// <param name="fact">The immutable fact content</param>
    /// <param name="domain">Optional domain/category for organizing facts (e.g., "world", "character", "lore")</param>
    /// <returns>Dictionary mapping persona IDs to their mutation results (success/failure)</returns>
    public Dictionary<string, MutationResult> AddCanonicalFactToAll(string factId, string fact, string? domain = null)
    {
      var results = new Dictionary<string, MutationResult>();
      
      foreach (var personaId in _memorySystems.Keys.ToList())
      {
        var result = AddCanonicalFact(personaId, factId, fact, domain);
        results[personaId] = result;
      }
      
      return results;
    }

    /// <summary>
    /// Adds a canonical fact to specific personas.
    /// </summary>
    /// <param name="personaIds">The list of persona IDs to add the fact to</param>
    /// <param name="factId">The unique identifier for this canonical fact</param>
    /// <param name="fact">The immutable fact content</param>
    /// <param name="domain">Optional domain/category for organizing facts (e.g., "world", "character", "lore")</param>
    /// <returns>Dictionary mapping persona IDs to their mutation results (success/failure)</returns>
    public Dictionary<string, MutationResult> AddCanonicalFactToPersonas(IEnumerable<string> personaIds, string factId, string fact, string? domain = null)
    {
      var results = new Dictionary<string, MutationResult>();
      
      foreach (var personaId in personaIds)
      {
        var result = AddCanonicalFact(personaId, factId, fact, domain);
        results[personaId] = result;
      }
      
      return results;
    }

    /// <summary>
    /// Sets or updates world state for the persona.
    /// World state represents mutable game state that requires validation to change.
    /// </summary>
    /// <param name="personaId">The unique identifier of the persona</param>
    /// <param name="key">The state key (e.g., "door_castle_main", "player_gold")</param>
    /// <param name="value">The state value</param>
    /// <param name="source">The source of the mutation request (defaults to GameSystem)</param>
    /// <returns>A mutation result indicating success or failure with details</returns>
    public MutationResult SetWorldState(string personaId, string key, string value, MutationSource source = MutationSource.GameSystem)
    {
      var system = GetOrCreateSystem(personaId);
      return system.SetWorldState(key, value, source);
    }

    /// <summary>
    /// Adds a dialogue exchange as episodic memory for the persona.
    /// Episodic memories represent conversation and event history that may decay over time.
    /// </summary>
    /// <param name="personaId">The unique identifier of the persona</param>
    /// <param name="speaker">The speaker in the dialogue exchange</param>
    /// <param name="content">The dialogue content</param>
    /// <param name="significance">Emotional significance (0.0 = neutral, 1.0 = highly significant). Higher significance memories decay slower.</param>
    /// <returns>A mutation result indicating success or failure with details</returns>
    public MutationResult AddDialogue(string personaId, string speaker, string content, float significance = 0.5f)
    {
      var system = GetOrCreateSystem(personaId);
      return system.AddDialogue(speaker, content, significance);
    }

    /// <summary>
    /// Sets or updates a belief for the persona.
    /// Beliefs are mutable and can be wrong; they may change based on interactions and can be contradicted by canonical facts.
    /// </summary>
    /// <param name="personaId">The unique identifier of the persona</param>
    /// <param name="beliefId">The unique identifier for this belief</param>
    /// <param name="subject">Who or what the belief is about</param>
    /// <param name="belief">The belief content</param>
    /// <param name="confidence">Confidence level in this belief (0.0 = uncertain, 1.0 = certain)</param>
    /// <returns>A mutation result indicating success or failure with details</returns>
    public MutationResult SetBelief(string personaId, string beliefId, string subject, string belief, float confidence = 0.5f)
    {
      var system = GetOrCreateSystem(personaId);
      var entry = BeliefMemoryEntry.CreateBelief(subject, belief, confidence);
      return system.SetBelief(beliefId, entry, MutationSource.ValidatedOutput);
    }

    /// <summary>
    /// Sets a relationship belief about someone for the persona.
    /// Relationship beliefs track the persona's perception of relationships with other entities.
    /// </summary>
    /// <param name="personaId">The unique identifier of the persona</param>
    /// <param name="targetId">The unique identifier of the target entity</param>
    /// <param name="relationship">The relationship description</param>
    /// <param name="sentiment">Sentiment value (-1.0 = very negative, 0 = neutral, 1.0 = very positive)</param>
    /// <returns>A mutation result indicating success or failure with details</returns>
    public MutationResult SetRelationship(string personaId, string targetId, string relationship, float sentiment = 0f)
    {
      var system = GetOrCreateSystem(personaId);
      var entry = BeliefMemoryEntry.CreateRelationship(targetId, relationship, sentiment);
      return system.SetBelief($"rel_{targetId}", entry, MutationSource.ValidatedOutput);
    }

    /// <summary>
    /// Gets memory statistics for a persona.
    /// Returns null if no memory system exists for the given persona ID.
    /// </summary>
    /// <param name="personaId">The unique identifier of the persona</param>
    /// <returns>Memory statistics including counts of all memory types, or null if persona not found</returns>
    public MemoryStatistics? GetStatistics(string personaId)
    {
      return GetSystem(personaId)?.GetStatistics();
    }

    /// <summary>
    /// Applies decay to episodic memories for a specific persona.
    /// Decay reduces memory strength over time, with more significant memories decaying slower.
    /// </summary>
    /// <param name="personaId">The unique identifier of the persona</param>
    public void ApplyDecay(string personaId)
    {
      GetSystem(personaId)?.ApplyEpisodicDecay();
    }

    /// <summary>
    /// Applies decay to all personas' episodic memories.
    /// </summary>
    public void ApplyDecayAll()
    {
      foreach (var system in _memorySystems.Values)
      {
        system.ApplyEpisodicDecay();
      }
    }

    #endregion
  }
}
