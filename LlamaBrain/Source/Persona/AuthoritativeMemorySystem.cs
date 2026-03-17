using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using LlamaBrain.Core.Audit;
using LlamaBrain.Persona.MemoryTypes;

namespace LlamaBrain.Persona
{
  /// <summary>
  /// Manages all memory types with authority boundaries.
  /// Enforces that higher-authority memories cannot be overridden by lower-authority sources.
  /// </summary>
  public class AuthoritativeMemorySystem
  {
    private readonly Dictionary<string, CanonicalFact> _canonicalFacts = new Dictionary<string, CanonicalFact>();
    private readonly Dictionary<string, WorldStateEntry> _worldState = new Dictionary<string, WorldStateEntry>();
    private readonly List<EpisodicMemoryEntry> _episodicMemories = new List<EpisodicMemoryEntry>();
    private readonly Dictionary<string, BeliefMemoryEntry> _beliefs = new Dictionary<string, BeliefMemoryEntry>();

    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;

    /// <summary>
    /// Monotonic counter for assigning deterministic SequenceNumbers to memory entries.
    /// Persisted and restored on load to ensure ordering is preserved across sessions.
    /// </summary>
    private long _nextSequenceNumber = 1;

    /// <summary>
    /// Gets or sets the next sequence number to be assigned.
    /// Used for persistence/restore. After loading, this should be set to max(SequenceNumber) + 1.
    /// </summary>
    public long NextSequenceNumber
    {
      get => _nextSequenceNumber;
      set => _nextSequenceNumber = value;
    }

    /// <summary>
    /// Maximum number of episodic memories to retain.
    /// </summary>
    public int MaxEpisodicMemories { get; set; } = 100;

    /// <summary>
    /// Decay factor applied to episodic memories per access cycle.
    /// </summary>
    public float EpisodicDecayRate { get; set; } = 0.05f;

    /// <summary>
    /// Optional logging callback.
    /// </summary>
    public Action<string>? OnLog { get; set; }

    /// <summary>
    /// Creates a new authoritative memory system with default providers (system clock and GUID generator).
    /// </summary>
    public AuthoritativeMemorySystem()
      : this(new SystemClock(), new GuidIdGenerator())
    {
    }

    /// <summary>
    /// Creates a new authoritative memory system with custom providers.
    /// Use this constructor to inject deterministic providers for testing.
    /// </summary>
    /// <param name="clock">Clock provider for deterministic time generation</param>
    /// <param name="idGenerator">ID generator for deterministic ID generation</param>
    public AuthoritativeMemorySystem(IClock clock, IIdGenerator idGenerator)
    {
      _clock = clock ?? throw new ArgumentNullException(nameof(clock));
      _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
    }

    #region Canonical Facts (Immutable)

    /// <summary>
    /// Adds a canonical fact. Can only be done by Designer source.
    /// Once added, canonical facts cannot be modified or removed.
    /// </summary>
    /// <param name="id">The unique identifier for this canonical fact</param>
    /// <param name="fact">The immutable fact content</param>
    /// <param name="domain">Optional domain/category for organizing facts (e.g., "world", "character", "lore")</param>
    /// <returns>A mutation result indicating success or failure. Fails if a fact with the same ID already exists.</returns>
    public MutationResult AddCanonicalFact(string id, string fact, string? domain = null)
    {
      if (_canonicalFacts.ContainsKey(id))
      {
        return MutationResult.Failed($"Canonical fact '{id}' already exists and cannot be modified.");
      }

      var entry = CanonicalFact.Create(id, fact, domain);
      
      // Set deterministic timestamp using injected clock
      entry.CreatedAtTicks = _clock.UtcNowTicks;
      
      // Assign monotonic sequence number for deterministic ordering
      entry.SequenceNumber = _nextSequenceNumber++;
      
      _canonicalFacts[id] = entry;
      OnLog?.Invoke($"[Memory] Added canonical fact: {id} = '{fact}' (seq={entry.SequenceNumber})");

      return MutationResult.Succeeded(entry);
    }

    /// <summary>
    /// Gets a canonical fact by ID.
    /// Marks the fact as accessed, updating its LastAccessedAt timestamp.
    /// </summary>
    /// <param name="id">The unique identifier of the canonical fact</param>
    /// <returns>The canonical fact if found, or null if not found</returns>
    public CanonicalFact? GetCanonicalFact(string id)
    {
      _canonicalFacts.TryGetValue(id, out var fact);
      fact?.MarkAccessed();
      return fact;
    }

    /// <summary>
    /// Gets all canonical facts, optionally filtered by domain.
    /// </summary>
    /// <param name="domain">Optional domain filter. If provided, only facts with matching domain are returned.</param>
    /// <returns>An enumerable collection of canonical facts, optionally filtered by domain</returns>
    public IEnumerable<CanonicalFact> GetCanonicalFacts(string? domain = null)
    {
      var facts = _canonicalFacts.Values.AsEnumerable();
      if (domain != null)
      {
        facts = facts.Where(f => f.Domain == domain);
      }
      return facts;
    }

    /// <summary>
    /// Checks if a fact ID corresponds to a canonical fact.
    /// </summary>
    /// <param name="factId">The fact ID to check</param>
    /// <returns>True if this is a canonical fact ID</returns>
    public bool IsCanonicalFact(string factId)
    {
      return _canonicalFacts.ContainsKey(factId);
    }

    /// <summary>
    /// Checks if a statement contradicts any canonical fact.
    /// Uses pattern matching to detect negation patterns that indicate contradiction.
    /// </summary>
    /// <param name="statement">The statement to check for contradictions</param>
    /// <param name="contradictedFact">If a contradiction is found, contains the contradicted canonical fact; otherwise null</param>
    /// <returns>True if the statement contradicts any canonical fact, false otherwise</returns>
    public bool ContradictesCanonicalFact(string statement, out CanonicalFact? contradictedFact)
    {
      // Simple check: see if any canonical fact is directly contradicted
      // In a real implementation, this would use semantic analysis
      foreach (var fact in _canonicalFacts.Values)
      {
        // Basic contradiction detection (negation patterns)
        var lowerStatement = statement.ToLowerInvariant();
        var lowerFact = fact.Fact.ToLowerInvariant();

        // Check for various negation patterns
        // Pattern 1: "not {fact}" or "isn't {fact}" or "never {fact}"
        if (lowerStatement.Contains($"not {lowerFact}") ||
            lowerStatement.Contains($"isn't {lowerFact}") ||
            lowerStatement.Contains($"never {lowerFact}"))
        {
          contradictedFact = fact;
          return true;
        }

        // Pattern 2: "{fact} is not" or "{fact} isn't" (fact with negation appended)
        if (lowerStatement.Contains($"{lowerFact} is not") ||
            lowerStatement.Contains($"{lowerFact} isn't"))
        {
          contradictedFact = fact;
          return true;
        }

        // Pattern 3: Check if statement is fact with "not" inserted (e.g., "X is Y" vs "X is not Y")
        // Replace " is " with " is not " or " isn't " in the fact and check if it matches statement
        var factWithNot = lowerFact.Replace(" is ", " is not ");
        var factWithIsnt = lowerFact.Replace(" is ", " isn't ");
        if (lowerStatement == factWithNot || lowerStatement == factWithIsnt)
        {
          contradictedFact = fact;
          return true;
        }
      }

      contradictedFact = null;
      return false;
    }

    #endregion

    #region World State (Validated Mutations)

    /// <summary>
    /// Sets or updates world state. Requires GameSystem or Designer authority.
    /// World state represents mutable game state that can change but requires validation.
    /// </summary>
    /// <param name="key">The state key (e.g., "door_castle_main", "player_gold")</param>
    /// <param name="value">The state value</param>
    /// <param name="source">The source requesting the mutation. Must be GameSystem or Designer authority.</param>
    /// <returns>A mutation result indicating success or failure. Fails if source lacks sufficient authority.</returns>
    public MutationResult SetWorldState(string key, string value, MutationSource source)
    {
      if (source < MutationSource.GameSystem)
      {
        return MutationResult.Failed($"Source '{source}' lacks authority to modify world state.");
      }

      if (_worldState.TryGetValue(key, out var existing))
      {
        var result = existing.UpdateValue(value, source);
        if (result.Success)
        {
          OnLog?.Invoke($"[Memory] Updated world state: {key} = '{value}'");
        }
        return result;
      }

      var entry = new WorldStateEntry(key, value) { Source = source };
      
      // Set deterministic ID and timestamp using injected providers
      entry.Id = _idGenerator.GenerateId();
      entry.CreatedAtTicks = _clock.UtcNowTicks;
      
      // Assign monotonic sequence number for deterministic ordering
      entry.SequenceNumber = _nextSequenceNumber++;
      
      // Set ModifiedAt using reflection (private setter)
      var modifiedAtProperty = typeof(WorldStateEntry).GetProperty("ModifiedAt", BindingFlags.Public | BindingFlags.Instance);
      var setMethod = modifiedAtProperty?.GetSetMethod(nonPublic: true);
      if (setMethod != null)
      {
        setMethod.Invoke(entry, new object[] { new DateTime(_clock.UtcNowTicks, DateTimeKind.Utc) });
      }
      
      _worldState[key] = entry;
      OnLog?.Invoke($"[Memory] Added world state: {key} = '{value}' (seq={entry.SequenceNumber})");

      return MutationResult.Succeeded(entry);
    }

    /// <summary>
    /// Gets world state by key.
    /// Marks the state entry as accessed, updating its LastAccessedAt timestamp.
    /// </summary>
    /// <param name="key">The state key to retrieve</param>
    /// <returns>The world state entry if found, or null if not found</returns>
    public WorldStateEntry? GetWorldState(string key)
    {
      _worldState.TryGetValue(key, out var state);
      state?.MarkAccessed();
      return state;
    }

    /// <summary>
    /// Gets all world state entries.
    /// </summary>
    /// <returns>An enumerable collection of all world state entries</returns>
    public IEnumerable<WorldStateEntry> GetAllWorldState()
    {
      return _worldState.Values;
    }

    #endregion

    #region Episodic Memory (Append-Only with Decay)

    /// <summary>
    /// Adds an episodic memory. Requires ValidatedOutput or higher authority.
    /// Episodic memories represent conversation and event history that may decay over time.
    /// Old memories are automatically pruned if the count exceeds MaxEpisodicMemories.
    /// </summary>
    /// <param name="entry">The episodic memory entry to add</param>
    /// <param name="source">The source requesting the mutation. Must be ValidatedOutput or higher authority.</param>
    /// <returns>A mutation result indicating success or failure. Fails if source lacks sufficient authority.</returns>
    public MutationResult AddEpisodicMemory(EpisodicMemoryEntry entry, MutationSource source)
    {
      if (source < MutationSource.ValidatedOutput)
      {
        return MutationResult.Failed($"Source '{source}' lacks authority to add episodic memories.");
      }

      entry.Source = source;
      
      // Set deterministic ID and timestamp using injected providers
      // Always use injected ID generator for determinism, unless ID was explicitly set to a non-GUID value
      // (Default property initializer creates a GUID, which we replace for determinism)
      if (string.IsNullOrEmpty(entry.Id) || IsDefaultGuid(entry.Id))
      {
        entry.Id = _idGenerator.GenerateId();
      }
      // Always use injected clock for determinism
      // (Default property initializer uses system clock which is non-deterministic)
      // Tests that need specific timestamps should use a fixed clock or set timestamps after creation
      entry.CreatedAtTicks = _clock.UtcNowTicks;
      
      // Assign monotonic sequence number for deterministic ordering
      entry.SequenceNumber = _nextSequenceNumber++;
      _episodicMemories.Add(entry);
      OnLog?.Invoke($"[Memory] Added episodic memory: '{entry.Description}' (seq={entry.SequenceNumber})");

      // Prune old memories if over limit
      PruneEpisodicMemories();

      return MutationResult.Succeeded(entry);
    }

    /// <summary>
    /// Adds a dialogue exchange as an episodic memory.
    /// Creates an episodic memory entry from dialogue and adds it to the memory system.
    /// </summary>
    /// <param name="speaker">The speaker in the dialogue exchange</param>
    /// <param name="content">The dialogue content</param>
    /// <param name="significance">Emotional significance (0.0 = neutral, 1.0 = highly significant). Higher significance memories decay slower.</param>
    /// <param name="source">The source requesting the mutation (defaults to ValidatedOutput)</param>
    /// <returns>A mutation result indicating success or failure</returns>
    public MutationResult AddDialogue(string speaker, string content, float significance = 0.5f, MutationSource source = MutationSource.ValidatedOutput)
    {
      var entry = EpisodicMemoryEntry.FromDialogue(speaker, content, significance);
      return AddEpisodicMemory(entry, source);
    }

    /// <summary>
    /// Gets active episodic memories (strength > threshold).
    /// Returns memories ordered by creation date (most recent first).
    /// </summary>
    /// <param name="strengthThreshold">Minimum strength required for a memory to be considered active (default: 0.1)</param>
    /// <returns>An enumerable collection of active episodic memories, ordered by creation date descending</returns>
    public IEnumerable<EpisodicMemoryEntry> GetActiveEpisodicMemories(float strengthThreshold = 0.1f)
    {
      return _episodicMemories
        .Where(m => m.Strength > strengthThreshold)
        .OrderByDescending(m => m.CreatedAt);
    }

    /// <summary>
    /// Gets recent episodic memories.
    /// Returns only active memories (strength > 0.1) ordered by creation date (most recent first).
    /// </summary>
    /// <param name="count">Maximum number of recent memories to return</param>
    /// <returns>An enumerable collection of recent active episodic memories, ordered by creation date descending</returns>
    public IEnumerable<EpisodicMemoryEntry> GetRecentMemories(int count)
    {
      return _episodicMemories
        .Where(m => m.IsActive)
        .OrderByDescending(m => m.CreatedAt)
        .Take(count);
    }

    /// <summary>
    /// Applies decay to all episodic memories.
    /// </summary>
    public void ApplyEpisodicDecay()
    {
      foreach (var memory in _episodicMemories)
      {
        memory.ApplyDecay(EpisodicDecayRate);
      }

      // Remove completely decayed memories
      _episodicMemories.RemoveAll(m => m.Strength <= 0);
    }

    private void PruneEpisodicMemories()
    {
      if (_episodicMemories.Count <= MaxEpisodicMemories) return;

      // Remove lowest strength memories first
      var toRemove = _episodicMemories
        .OrderBy(m => m.Strength)
        .ThenBy(m => m.Significance)
        .Take(_episodicMemories.Count - MaxEpisodicMemories)
        .ToList();

      foreach (var memory in toRemove)
      {
        _episodicMemories.Remove(memory);
      }
    }

    #endregion

    #region Beliefs (Mutable, Can Be Wrong)

    /// <summary>
    /// Adds or updates a belief. Requires ValidatedOutput or higher authority.
    /// Automatically checks if the belief contradicts any canonical fact and marks it as contradicted if so.
    /// </summary>
    /// <param name="id">The unique identifier for this belief</param>
    /// <param name="entry">The belief memory entry to add or update</param>
    /// <param name="source">The source requesting the mutation. Must be ValidatedOutput or higher authority.</param>
    /// <returns>A mutation result indicating success or failure. Fails if source lacks sufficient authority.</returns>
    public MutationResult SetBelief(string id, BeliefMemoryEntry entry, MutationSource source)
    {
      if (source < MutationSource.ValidatedOutput)
      {
        return MutationResult.Failed($"Source '{source}' lacks authority to modify beliefs.");
      }

      // Check if this belief contradicts a canonical fact
      if (ContradictesCanonicalFact(entry.BeliefContent, out var contradicted))
      {
        entry.MarkContradicted($"Contradicts canonical fact: {contradicted!.Fact}");
        OnLog?.Invoke($"[Memory] Belief contradicts canonical fact: '{entry.BeliefContent}' vs '{contradicted.Fact}'");
      }

      entry.Source = source;
      
      // Set deterministic ID and timestamp using injected providers
      // Only override if ID looks auto-generated (8 hex chars) or is empty
      // This allows explicit IDs to be preserved (e.g., when belief ID matches the key)
      if (string.IsNullOrEmpty(entry.Id) || (entry.Id.Length == 8 && entry.Id.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))))
      {
        // Looks like auto-generated GUID - replace with deterministic ID
        entry.Id = _idGenerator.GenerateId();
      }
      // Always set timestamp from clock for determinism (even if entry had a timestamp)
      entry.CreatedAtTicks = _clock.UtcNowTicks;
      
      // Assign monotonic sequence number for deterministic ordering (only if new entry)
      if (entry.SequenceNumber == 0)
      {
        entry.SequenceNumber = _nextSequenceNumber++;
      }
      _beliefs[id] = entry;
      OnLog?.Invoke($"[Memory] Set belief: {id} = '{entry.BeliefContent}' (seq={entry.SequenceNumber})");

      return MutationResult.Succeeded(entry);
    }

    /// <summary>
    /// Gets a belief by ID.
    /// Marks the belief as accessed, updating its LastAccessedAt timestamp.
    /// </summary>
    /// <param name="id">The unique identifier of the belief</param>
    /// <returns>The belief entry if found, or null if not found</returns>
    public BeliefMemoryEntry? GetBelief(string id)
    {
      _beliefs.TryGetValue(id, out var belief);
      belief?.MarkAccessed();
      return belief;
    }

    /// <summary>
    /// Gets all beliefs about a subject.
    /// Performs case-insensitive matching on the subject field.
    /// </summary>
    /// <param name="subject">The subject to search for (case-insensitive)</param>
    /// <returns>An enumerable collection of beliefs matching the subject</returns>
    public IEnumerable<BeliefMemoryEntry> GetBeliefsAbout(string subject)
    {
      return _beliefs.Values.Where(b =>
        b.Subject.Equals(subject, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all non-contradicted beliefs.
    /// Returns only beliefs that have not been marked as contradicted by canonical facts.
    /// </summary>
    /// <returns>An enumerable collection of active (non-contradicted) beliefs</returns>
    public IEnumerable<BeliefMemoryEntry> GetActiveBeliefs()
    {
      return _beliefs.Values.Where(b => !b.IsContradicted);
    }

    /// <summary>
    /// Gets all beliefs, including contradicted ones.
    /// </summary>
    /// <returns>An enumerable collection of all beliefs</returns>
    public IEnumerable<BeliefMemoryEntry> GetAllBeliefs()
    {
      return _beliefs.Values;
    }

    #endregion

    #region Unified Access

    /// <summary>
    /// Gets all memories formatted for prompt injection.
    /// Respects authority ordering (canonical > world > episodic > beliefs).
    /// Memories are formatted with prefixes indicating their type: [Fact], [State], [Memory], or [Uncertain] for contradicted beliefs.
    /// </summary>
    /// <param name="maxEpisodic">Maximum number of episodic memories to include (default: 10)</param>
    /// <param name="includeContradictedBeliefs">Whether to include beliefs that have been contradicted by canonical facts (default: false)</param>
    /// <returns>An enumerable collection of formatted memory strings ready for prompt injection</returns>
    public IEnumerable<string> GetAllMemoriesForPrompt(int maxEpisodic = 10, bool includeContradictedBeliefs = false)
    {
      var memories = new List<string>();

      // Canonical facts first (always included)
      foreach (var fact in _canonicalFacts.Values)
      {
        memories.Add($"[Fact] {fact.Content}");
      }

      // World state
      foreach (var state in _worldState.Values)
      {
        memories.Add($"[State] {state.Content}");
      }

      // Recent episodic memories
      foreach (var episode in GetRecentMemories(maxEpisodic))
      {
        memories.Add($"[Memory] {episode.Content}");
      }

      // Active beliefs
      var beliefs = includeContradictedBeliefs ? _beliefs.Values : GetActiveBeliefs();
      foreach (var belief in beliefs)
      {
        var prefix = belief.IsContradicted ? "[Uncertain] " : "";
        memories.Add($"{prefix}{belief.Content}");
      }

      return memories;
    }

    /// <summary>
    /// Validates whether a proposed memory mutation is allowed.
    /// Checks if the source has sufficient authority to modify the target memory type.
    /// </summary>
    /// <param name="targetAuthority">The authority level of the target memory type to modify</param>
    /// <param name="source">The source requesting the mutation</param>
    /// <returns>True if the source has sufficient authority to modify the target memory type, false otherwise</returns>
    public bool ValidateMutation(MemoryAuthority targetAuthority, MutationSource source)
    {
      return (int)source >= (int)targetAuthority;
    }

    /// <summary>
    /// Clears all memories. Use with caution.
    /// </summary>
    public void ClearAll()
    {
      _canonicalFacts.Clear();
      _worldState.Clear();
      _episodicMemories.Clear();
      _beliefs.Clear();
      _nextSequenceNumber = 1;
      OnLog?.Invoke("[Memory] All memories cleared");
    }

    #endregion

    #region Snapshot Helpers (For Persistence)

    /// <summary>
    /// Gets all world state entries with their keys.
    /// Used by persistence layer for snapshotting.
    /// </summary>
    /// <returns>An enumerable of key-value pairs for world state</returns>
    public IEnumerable<KeyValuePair<string, WorldStateEntry>> GetWorldStateEntries()
    {
      return _worldState;
    }

    /// <summary>
    /// Gets all beliefs with their dictionary keys.
    /// Used by persistence layer for snapshotting.
    /// </summary>
    /// <returns>An enumerable of key-value pairs for beliefs</returns>
    public IEnumerable<KeyValuePair<string, BeliefMemoryEntry>> GetBeliefEntries()
    {
      return _beliefs;
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Recalculates the next sequence number from existing memories.
    /// Call this after loading/deserializing memories to ensure the counter is correct.
    /// Sets NextSequenceNumber = max(all SequenceNumbers) + 1.
    /// </summary>
    public void RecalculateNextSequenceNumber()
    {
      long maxSeq = 0;

      foreach (var memory in _episodicMemories)
      {
        if (memory.SequenceNumber > maxSeq)
          maxSeq = memory.SequenceNumber;
      }

      foreach (var belief in _beliefs.Values)
      {
        if (belief.SequenceNumber > maxSeq)
          maxSeq = belief.SequenceNumber;
      }

      _nextSequenceNumber = maxSeq + 1;
      OnLog?.Invoke($"[Memory] Recalculated next sequence number: {_nextSequenceNumber}");
    }

    /// <summary>
    /// Gets memory statistics for the entire memory system.
    /// Provides counts of all memory types including active vs total counts where applicable.
    /// </summary>
    /// <returns>A MemoryStatistics object containing counts of all memory types</returns>
    public MemoryStatistics GetStatistics()
    {
      return new MemoryStatistics
      {
        CanonicalFactCount = _canonicalFacts.Count,
        WorldStateCount = _worldState.Count,
        EpisodicMemoryCount = _episodicMemories.Count,
        ActiveEpisodicCount = _episodicMemories.Count(m => m.IsActive),
        BeliefCount = _beliefs.Count,
        ActiveBeliefCount = _beliefs.Values.Count(b => !b.IsContradicted)
      };
    }

    /// <summary>
    /// Computes a deterministic SHA256 hash of the entire memory system state.
    /// Used for audit recording and drift detection during replay.
    /// </summary>
    /// <remarks>
    /// The hash includes:
    /// - All canonical facts (ordered by SequenceNumber)
    /// - All world state entries (ordered by SequenceNumber)
    /// - All episodic memories (ordered by SequenceNumber)
    /// - All beliefs (ordered by SequenceNumber)
    /// - The NextSequenceNumber (ordering metadata)
    ///
    /// The hash explicitly EXCLUDES:
    /// - LastAccessedAt timestamps (non-deterministic, changes on access)
    ///
    /// Ordering by SequenceNumber ensures deterministic hash regardless of
    /// dictionary iteration order or insertion order.
    /// </remarks>
    /// <returns>Base64-encoded SHA256 hash of the memory state.</returns>
    public string ComputeStateHash()
    {
      var sb = new StringBuilder();

      // Header with sequence number for ordering guarantee
      sb.AppendLine($"NextSequenceNumber:{_nextSequenceNumber}");

      // Canonical facts (highest authority, ordered by sequence number)
      foreach (var fact in _canonicalFacts.Values.OrderBy(f => f.SequenceNumber))
      {
        sb.AppendLine($"F|{fact.Id}|{fact.Content}|{fact.Domain ?? ""}|{fact.CreatedAtTicks}|{fact.SequenceNumber}");
      }

      // World state (ordered by sequence number)
      foreach (var entry in _worldState.Values.OrderBy(e => e.SequenceNumber))
      {
        sb.AppendLine($"W|{entry.Key}|{entry.Value}|{(int)entry.Source}|{entry.CreatedAtTicks}|{entry.SequenceNumber}");
      }

      // Episodic memories (ordered by sequence number)
      foreach (var memory in _episodicMemories.OrderBy(m => m.SequenceNumber))
      {
        sb.AppendLine($"E|{memory.Id}|{memory.Content}|{memory.Strength:F6}|{memory.IsActive}|{memory.CreatedAtTicks}|{memory.SequenceNumber}");
      }

      // Beliefs (ordered by sequence number)
      foreach (var belief in _beliefs.Values.OrderBy(b => b.SequenceNumber))
      {
        sb.AppendLine($"B|{belief.Id}|{belief.Content}|{belief.Confidence:F6}|{belief.IsContradicted}|{belief.CreatedAtTicks}|{belief.SequenceNumber}");
      }

      return AuditHasher.ComputeSha256(sb.ToString());
    }

    #endregion

    #region Raw Insertion APIs (Test-Only, for Deterministic Reconstruction)

    /// <summary>
    /// Raw insertion of canonical fact without generating metadata.
    /// Test-only API for deterministic reconstruction from serialized state.
    /// The entry must have all fields (Id, CreatedAtTicks, SequenceNumber) already set.
    /// </summary>
    internal void InsertCanonicalFactRaw(CanonicalFact entry)
    {
      if (entry == null) throw new ArgumentNullException(nameof(entry));
      if (string.IsNullOrEmpty(entry.Id)) throw new ArgumentException("Entry must have Id set", nameof(entry));
      
      if (_canonicalFacts.ContainsKey(entry.Id))
      {
        throw new InvalidOperationException($"Canonical fact '{entry.Id}' already exists");
      }
      
      _canonicalFacts[entry.Id] = entry;
    }

    /// <summary>
    /// Raw insertion of world state without generating metadata.
    /// Test-only API for deterministic reconstruction from serialized state.
    /// The entry must have all fields (Id, CreatedAtTicks, SequenceNumber, ModifiedAt) already set.
    /// </summary>
    internal void InsertWorldStateRaw(string key, WorldStateEntry entry)
    {
      if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty", nameof(key));
      if (entry == null) throw new ArgumentNullException(nameof(entry));
      if (string.IsNullOrEmpty(entry.Id)) throw new ArgumentException("Entry must have Id set", nameof(entry));
      
      _worldState[key] = entry;
    }

    /// <summary>
    /// Raw insertion of episodic memory without generating metadata.
    /// Test-only API for deterministic reconstruction from serialized state.
    /// The entry must have all fields (Id, CreatedAtTicks, SequenceNumber) already set.
    /// </summary>
    internal void InsertEpisodicRaw(EpisodicMemoryEntry entry)
    {
      if (entry == null) throw new ArgumentNullException(nameof(entry));
      if (string.IsNullOrEmpty(entry.Id)) throw new ArgumentException("Entry must have Id set", nameof(entry));
      
      _episodicMemories.Add(entry);
    }

    /// <summary>
    /// Raw insertion of belief without generating metadata.
    /// Test-only API for deterministic reconstruction from serialized state.
    /// The entry must have all fields (Id, CreatedAtTicks, SequenceNumber) already set.
    /// </summary>
    internal void InsertBeliefRaw(string key, BeliefMemoryEntry entry)
    {
      if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty", nameof(key));
      if (entry == null) throw new ArgumentNullException(nameof(entry));
      if (string.IsNullOrEmpty(entry.Id)) throw new ArgumentException("Entry must have Id set", nameof(entry));
      
      _beliefs[key] = entry;
    }

    /// <summary>
    /// Sets the next sequence number directly without validation.
    /// Test-only API for deterministic reconstruction from serialized state.
    /// </summary>
    internal void SetNextSequenceNumberRaw(long nextSeq)
    {
      _nextSequenceNumber = nextSeq;
    }

    /// <summary>
    /// Checks if an ID looks like a default GUID (8 hex characters).
    /// Used to determine if we should replace it with a deterministic ID.
    /// </summary>
    private static bool IsDefaultGuid(string id)
    {
      if (string.IsNullOrEmpty(id) || id.Length != 8)
        return false;
      
      // Check if all characters are hex digits (default GUID pattern)
      foreach (char c in id)
      {
        if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
          return false;
      }
      return true;
    }

    #endregion
  }

  /// <summary>
  /// Statistics about the memory system.
  /// Provides counts of all memory types including active vs total counts where applicable.
  /// </summary>
  public class MemoryStatistics
  {
    /// <summary>
    /// Total number of canonical facts (immutable truths) in the memory system.
    /// </summary>
    public int CanonicalFactCount { get; set; }

    /// <summary>
    /// Total number of world state entries in the memory system.
    /// </summary>
    public int WorldStateCount { get; set; }

    /// <summary>
    /// Total number of episodic memories (including decayed ones) in the memory system.
    /// </summary>
    public int EpisodicMemoryCount { get; set; }

    /// <summary>
    /// Number of active episodic memories (strength > 0.1) in the memory system.
    /// </summary>
    public int ActiveEpisodicCount { get; set; }

    /// <summary>
    /// Total number of beliefs (including contradicted ones) in the memory system.
    /// </summary>
    public int BeliefCount { get; set; }

    /// <summary>
    /// Number of active beliefs (non-contradicted) in the memory system.
    /// </summary>
    public int ActiveBeliefCount { get; set; }

    /// <summary>
    /// Returns a string representation of the memory statistics.
    /// </summary>
    /// <returns>A formatted string showing counts of different memory types</returns>
    public override string ToString()
    {
      return $"Memory Stats: {CanonicalFactCount} facts, {WorldStateCount} state, " +
             $"{ActiveEpisodicCount}/{EpisodicMemoryCount} episodes, {ActiveBeliefCount}/{BeliefCount} beliefs";
    }
  }
}
