using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LlamaBrain.Core.Retrieval;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;

namespace LlamaBrain.Core.Inference
{
  /// <summary>
  /// Configuration for context retrieval limits and weighting.
  /// </summary>
  public class ContextRetrievalConfig
  {
    /// <summary>
    /// Maximum number of canonical facts to include.
    /// Default: unlimited (0 = no limit).
    /// </summary>
    public int MaxCanonicalFacts { get; set; } = 0;

    /// <summary>
    /// Maximum number of world state entries to include.
    /// Default: unlimited (0 = no limit).
    /// </summary>
    public int MaxWorldState { get; set; } = 0;

    /// <summary>
    /// Maximum number of episodic memories to include.
    /// </summary>
    public int MaxEpisodicMemories { get; set; } = 15;

    /// <summary>
    /// Maximum number of beliefs to include.
    /// </summary>
    public int MaxBeliefs { get; set; } = 10;

    /// <summary>
    /// Maximum dialogue history turns to include.
    /// </summary>
    public int MaxDialogueHistory { get; set; } = 10;

    /// <summary>
    /// Minimum strength threshold for episodic memories.
    /// </summary>
    public float MinEpisodicStrength { get; set; } = 0.1f;

    /// <summary>
    /// Minimum confidence threshold for beliefs.
    /// </summary>
    public float MinBeliefConfidence { get; set; } = 0.3f;

    /// <summary>
    /// Whether to include contradicted beliefs (marked as uncertain).
    /// </summary>
    public bool IncludeContradictedBeliefs { get; set; } = false;

    /// <summary>
    /// Weight given to recency when scoring memories (0-1).
    /// Higher values favor more recent memories.
    /// </summary>
    public float RecencyWeight { get; set; } = 0.4f;

    /// <summary>
    /// Weight given to relevance when scoring memories (0-1).
    /// Higher values favor more relevant memories.
    /// </summary>
    public float RelevanceWeight { get; set; } = 0.4f;

    /// <summary>
    /// Weight given to significance when scoring memories (0-1).
    /// Higher values favor more significant memories.
    /// </summary>
    public float SignificanceWeight { get; set; } = 0.2f;

    /// <summary>
    /// Half-life for recency decay in ticks. Memories older than this
    /// relative to snapshot time will have recency score ~0.5.
    /// Default: 1 hour (36_000_000_000 ticks).
    /// </summary>
    public long RecencyHalfLifeTicks { get; set; } = TimeSpan.FromHours(1).Ticks;

    /// <summary>
    /// Configuration for embedding-based semantic retrieval (RAG).
    /// When null or EnableSemanticRetrieval=false, only keyword matching is used.
    /// </summary>
    public EmbeddingConfig? EmbeddingConfig { get; set; } = null;

    /// <summary>
    /// Creates a default configuration.
    /// </summary>
    public static ContextRetrievalConfig Default => new ContextRetrievalConfig();
  }

  /// <summary>
  /// Retrieves and filters relevant context from the memory system.
  /// Applies recency/relevance weighting and respects character knowledge boundaries.
  /// This is part of box 4 in the architecture: "Authoritative State Snapshot".
  /// Supports optional RAG (embedding-based semantic retrieval) via IEmbeddingProvider and IMemoryVectorStore.
  /// </summary>
  public class ContextRetrievalLayer
  {
    private readonly AuthoritativeMemorySystem _memorySystem;
    private readonly ContextRetrievalConfig _config;
    private readonly IEmbeddingProvider? _embeddingProvider;
    private readonly IMemoryVectorStore? _vectorStore;
    private readonly HybridRelevanceCalculator? _hybridCalculator;
    private readonly string? _npcId;

    /// <summary>
    /// Optional logging callback.
    /// </summary>
    public Action<string>? OnLog { get; set; }

    /// <summary>
    /// Creates a new context retrieval layer (keyword-only mode).
    /// </summary>
    /// <param name="memorySystem">The memory system to retrieve from.</param>
    /// <param name="config">Optional configuration (uses defaults if null).</param>
    public ContextRetrievalLayer(AuthoritativeMemorySystem memorySystem, ContextRetrievalConfig? config = null)
      : this(memorySystem, config, null, null, null)
    {
    }

    /// <summary>
    /// Creates a new context retrieval layer with optional RAG support.
    /// </summary>
    /// <param name="memorySystem">The memory system to retrieve from.</param>
    /// <param name="config">Optional configuration (uses defaults if null).</param>
    /// <param name="embeddingProvider">Optional embedding provider for semantic search.</param>
    /// <param name="vectorStore">Optional vector store for semantic search.</param>
    /// <param name="npcId">Optional NPC ID for filtering shared vector store queries.</param>
    public ContextRetrievalLayer(
      AuthoritativeMemorySystem memorySystem,
      ContextRetrievalConfig? config,
      IEmbeddingProvider? embeddingProvider,
      IMemoryVectorStore? vectorStore,
      string? npcId)
    {
      _memorySystem = memorySystem ?? throw new ArgumentNullException(nameof(memorySystem));
      _config = config ?? ContextRetrievalConfig.Default;
      _embeddingProvider = embeddingProvider;
      _vectorStore = vectorStore;
      _npcId = npcId;

      // Create hybrid calculator if semantic retrieval is enabled and dependencies are available
      if (_config.EmbeddingConfig?.EnableSemanticRetrieval == true &&
          _embeddingProvider != null &&
          _vectorStore != null)
      {
        _hybridCalculator = new HybridRelevanceCalculator(_config.EmbeddingConfig);
      }
    }

    /// <summary>
    /// Retrieves all relevant context for an interaction using snapshot time for deterministic recency calculations.
    /// This is the preferred method for deterministic behavior.
    /// </summary>
    /// <remarks>
    /// WARNING: When semantic retrieval is configured, this synchronous method will fall back to
    /// keyword-only retrieval to avoid potential deadlocks. Use <see cref="RetrieveContextAsync"/>
    /// for full semantic retrieval support.
    /// </remarks>
    /// <param name="snapshot">The state snapshot containing player input and snapshot time</param>
    /// <param name="topics">Optional topics to filter by (for relevance)</param>
    /// <returns>Retrieved context ready for snapshot building</returns>
    public RetrievedContext RetrieveContext(StateSnapshot snapshot, IEnumerable<string>? topics = null)
    {
      return RetrieveContextInternal(snapshot.PlayerInput, snapshot.SnapshotTimeUtcTicks, topics);
    }

    /// <summary>
    /// Retrieves all relevant context for an interaction using explicit snapshot time for deterministic recency calculations.
    /// This is the preferred method when you have snapshot time but not yet a full snapshot.
    /// </summary>
    /// <remarks>
    /// WARNING: When semantic retrieval is configured, this synchronous method will fall back to
    /// keyword-only retrieval to avoid potential deadlocks. Use <see cref="RetrieveContextAsync"/>
    /// for full semantic retrieval support.
    /// </remarks>
    /// <param name="playerInput">The player's input (used for relevance scoring)</param>
    /// <param name="snapshotTimeUtcTicks">The snapshot time in UTC ticks for deterministic recency calculations</param>
    /// <param name="topics">Optional topics to filter by (for relevance)</param>
    /// <returns>Retrieved context ready for snapshot building</returns>
    public RetrievedContext RetrieveContext(string playerInput, long snapshotTimeUtcTicks, IEnumerable<string>? topics = null)
    {
      return RetrieveContextInternal(playerInput, snapshotTimeUtcTicks, topics);
    }

    /// <summary>
    /// Retrieves all relevant context for an interaction.
    /// Returns a RetrievedContext containing categorized memories.
    /// NOTE: This overload uses current wall-clock time, which is NOT deterministic.
    /// Use RetrieveContext(StateSnapshot, ...) for deterministic behavior.
    /// </summary>
    /// <remarks>
    /// WARNING: When semantic retrieval is configured, this synchronous method will fall back to
    /// keyword-only retrieval to avoid potential deadlocks. Use <see cref="RetrieveContextAsync"/>
    /// for full semantic retrieval support.
    /// </remarks>
    /// <param name="playerInput">The player's input (used for relevance scoring).</param>
    /// <param name="topics">Optional topics to filter by (for relevance).</param>
    /// <returns>Retrieved context ready for snapshot building.</returns>
    public RetrievedContext RetrieveContext(string playerInput, IEnumerable<string>? topics = null)
    {
      // Use current time for backward compatibility (not deterministic)
      var snapshotTimeUtcTicks = DateTimeOffset.UtcNow.UtcTicks;
      return RetrieveContextInternal(playerInput, snapshotTimeUtcTicks, topics);
    }

    /// <summary>
    /// Internal method that retrieves context with explicit snapshot time.
    /// </summary>
    private RetrievedContext RetrieveContextInternal(string playerInput, long snapshotTimeUtcTicks, IEnumerable<string>? topics)
    {
      var topicList = topics?.ToList() ?? new List<string>();

      OnLog?.Invoke($"[ContextRetrieval] Retrieving context for input: '{Truncate(playerInput, 50)}'");

      // Pre-compute semantic scores for all memories if semantic retrieval is enabled
      Dictionary<string, float>? semanticScores = null;
      if (IsSemanticRetrievalAvailable())
      {
        semanticScores = ComputeSemanticScores(playerInput);
        if (semanticScores != null)
        {
          OnLog?.Invoke($"[ContextRetrieval] Computed {semanticScores.Count} semantic scores");
        }
      }

      var result = new RetrievedContext
      {
        CanonicalFacts = RetrieveCanonicalFacts(topicList),
        WorldState = RetrieveWorldState(topicList),
        EpisodicMemories = RetrieveEpisodicMemories(playerInput, topicList, snapshotTimeUtcTicks, semanticScores),
        Beliefs = RetrieveBeliefs(playerInput, topicList, semanticScores)
      };

      OnLog?.Invoke($"[ContextRetrieval] Retrieved: {result.CanonicalFacts.Count} facts, " +
                    $"{result.WorldState.Count} state, {result.EpisodicMemories.Count} episodes, " +
                    $"{result.Beliefs.Count} beliefs");

      return result;
    }

    /// <summary>
    /// Async version of RetrieveContext - safe for Unity.
    /// Uses explicit snapshot time for deterministic recency calculations.
    /// </summary>
    public async Task<RetrievedContext> RetrieveContextAsync(string playerInput, long snapshotTimeUtcTicks, IEnumerable<string>? topics = null)
    {
      var topicList = topics?.ToList() ?? new List<string>();

      OnLog?.Invoke($"[ContextRetrieval] Retrieving context async for input: '{Truncate(playerInput, 50)}'");

      // Pre-compute semantic scores asynchronously if semantic retrieval is enabled
      Dictionary<string, float>? semanticScores = null;
      if (IsSemanticRetrievalAvailable())
      {
        semanticScores = await ComputeSemanticScoresAsync(playerInput);
        if (semanticScores != null)
        {
          OnLog?.Invoke($"[ContextRetrieval] Computed {semanticScores.Count} semantic scores");
        }
      }

      var result = new RetrievedContext
      {
        CanonicalFacts = RetrieveCanonicalFacts(topicList),
        WorldState = RetrieveWorldState(topicList),
        EpisodicMemories = RetrieveEpisodicMemories(playerInput, topicList, snapshotTimeUtcTicks, semanticScores),
        Beliefs = RetrieveBeliefs(playerInput, topicList, semanticScores)
      };

      OnLog?.Invoke($"[ContextRetrieval] Retrieved: {result.CanonicalFacts.Count} facts, " +
                    $"{result.WorldState.Count} state, {result.EpisodicMemories.Count} episodes, " +
                    $"{result.Beliefs.Count} beliefs");

      return result;
    }

    /// <summary>
    /// Checks if semantic retrieval is available and enabled.
    /// </summary>
    private bool IsSemanticRetrievalAvailable()
    {
      return _hybridCalculator != null &&
             _embeddingProvider?.IsAvailable == true &&
             _vectorStore != null &&
             _config.EmbeddingConfig?.EnableSemanticRetrieval == true;
    }

    /// <summary>
    /// Sync path intentionally skips semantic scoring to avoid deadlocks.
    /// Returns null to trigger keyword-only fallback.
    /// Use RetrieveContextAsync for semantic retrieval support.
    /// </summary>
    private Dictionary<string, float>? ComputeSemanticScores(string playerInput)
    {
      // Intentionally skip semantic scoring in sync path to avoid deadlock.
      // Calling GetAwaiter().GetResult() on async embedding generation can deadlock
      // in Unity's single-threaded synchronization context.
      OnLog?.Invoke("[ContextRetrieval] Semantic scoring skipped in sync path (use RetrieveContextAsync for semantic retrieval)");
      return null;
    }

    /// <summary>
    /// Async version of ComputeSemanticScores - safe for Unity.
    /// </summary>
    private async Task<Dictionary<string, float>?> ComputeSemanticScoresAsync(string playerInput)
    {
      if (_vectorStore == null || _embeddingProvider == null)
        return null;

      try
      {
        var queryEmbedding = await _embeddingProvider.GenerateEmbeddingAsync(playerInput);
        if (queryEmbedding == null)
        {
          OnLog?.Invoke("[ContextRetrieval] Embedding generation failed, falling back to keyword-only");
          return null;
        }

        return ComputeSemanticScoresFromEmbedding(queryEmbedding);
      }
      catch (Exception ex)
      {
        OnLog?.Invoke($"[ContextRetrieval] Semantic retrieval failed ({ex.Message}), falling back to keyword-only");
        return null;
      }
    }

    /// <summary>
    /// Computes semantic scores from a pre-generated query embedding.
    /// </summary>
    private Dictionary<string, float>? ComputeSemanticScoresFromEmbedding(float[] queryEmbedding)
    {
      if (_vectorStore == null)
        return null;

      var candidateLimit = _config.EmbeddingConfig?.SemanticCandidateLimit ?? 50;
      var minSimilarity = _config.EmbeddingConfig?.MinSemanticSimilarity ?? 0.3f;

      var results = _vectorStore.FindSimilar(
        queryEmbedding,
        candidateLimit,
        _npcId,
        memoryType: null,
        minSimilarity: minSimilarity);

      return results.ToDictionary(r => r.MemoryId, r => r.Similarity);
    }

    /// <summary>
    /// Retrieves canonical facts, optionally filtered by domain/topics.
    /// Uses strict total order sorting for deterministic results:
    /// Order: Id ordinal ascending (canonical facts are immutable, so Id is stable).
    /// </summary>
    private List<string> RetrieveCanonicalFacts(List<string> topics)
    {
      var facts = _memorySystem.GetCanonicalFacts().ToList();

      // Filter by topics if provided
      if (topics.Count > 0)
      {
        facts = facts.Where(f => IsRelevantToTopics(f.Content, topics) ||
                                  (f.Domain != null && topics.Contains(f.Domain, StringComparer.OrdinalIgnoreCase)))
                     .ToList();
      }

      // Sort deterministically by Id before applying limit
      facts = facts.OrderBy(f => f.Id, StringComparer.Ordinal).ToList();

      // Apply limit if configured
      if (_config.MaxCanonicalFacts > 0)
      {
        facts = facts.Take(_config.MaxCanonicalFacts).ToList();
      }

      return facts.Select(f => f.Content).ToList();
    }

    /// <summary>
    /// Retrieves world state entries, optionally filtered by topics.
    /// Uses strict total order sorting for deterministic results:
    /// Order: Key ordinal ascending (world state keys are stable identifiers).
    /// </summary>
    private List<string> RetrieveWorldState(List<string> topics)
    {
      var states = _memorySystem.GetAllWorldState().ToList();

      // Filter by topics if provided
      if (topics.Count > 0)
      {
        states = states.Where(s => IsRelevantToTopics(s.Content, topics) ||
                                    IsRelevantToTopics(s.Key, topics))
                       .ToList();
      }

      // Sort deterministically by Key before applying limit
      states = states.OrderBy(s => s.Key, StringComparer.Ordinal).ToList();

      // Apply limit if configured
      if (_config.MaxWorldState > 0)
      {
        states = states.Take(_config.MaxWorldState).ToList();
      }

      return states.Select(s => s.Content).ToList();
    }

    /// <summary>
    /// Retrieves episodic memories with recency/relevance scoring.
    /// Uses strict total order sorting for deterministic results:
    /// Primary: score descending, then CreatedAtTicks descending, then Id ordinal, then SequenceNumber ascending.
    /// </summary>
    private List<string> RetrieveEpisodicMemories(string playerInput, List<string> topics, long snapshotTimeUtcTicks, Dictionary<string, float>? semanticScores)
    {
      var memories = _memorySystem.GetActiveEpisodicMemories(_config.MinEpisodicStrength).ToList();

      // Score and sort memories with strict total order for determinism
      // Order: score desc, CreatedAtTicks desc, Id ordinal asc, SequenceNumber asc
      var scoredMemories = memories
        .Select(m => new ScoredItem<EpisodicMemoryEntry>(m, ScoreEpisodicMemory(m, playerInput, topics, snapshotTimeUtcTicks, semanticScores)))
        .OrderByDescending(s => s.Score)
        .ThenByDescending(s => s.Item.CreatedAtTicks)
        .ThenBy(s => s.Item.Id, StringComparer.Ordinal)
        .ThenBy(s => s.Item.SequenceNumber)
        .Take(_config.MaxEpisodicMemories)
        .ToList();

      return scoredMemories.Select(s => s.Item.Content).ToList();
    }

    /// <summary>
    /// Retrieves beliefs with relevance scoring.
    /// Uses strict total order sorting for deterministic results:
    /// Primary: score descending, then Confidence descending, then Id ordinal, then SequenceNumber ascending.
    /// </summary>
    private List<string> RetrieveBeliefs(string playerInput, List<string> topics, Dictionary<string, float>? semanticScores)
    {
      var beliefs = _config.IncludeContradictedBeliefs
        ? _memorySystem.GetAllBeliefs()
        : _memorySystem.GetActiveBeliefs();

      var beliefList = beliefs
        .Where(b => b.Confidence >= _config.MinBeliefConfidence)
        .ToList();

      // Score and sort beliefs with strict total order for determinism
      // Order: score desc, Confidence desc, Id ordinal asc, SequenceNumber asc
      var scoredBeliefs = beliefList
        .Select(b => new ScoredItem<BeliefMemoryEntry>(b, ScoreBelief(b, playerInput, topics, semanticScores)))
        .OrderByDescending(s => s.Score)
        .ThenByDescending(s => s.Item.Confidence)
        .ThenBy(s => s.Item.Id, StringComparer.Ordinal)
        .ThenBy(s => s.Item.SequenceNumber)
        .Take(_config.MaxBeliefs)
        .ToList();

      return scoredBeliefs.Select(s => FormatBelief(s.Item)).ToList();
    }

    /// <summary>
    /// Scores an episodic memory based on recency, relevance, and significance.
    /// Recency is calculated using exponential decay based on snapshot time (deterministic).
    /// Relevance uses hybrid scoring (keyword + semantic) when semantic retrieval is enabled.
    /// </summary>
    private float ScoreEpisodicMemory(EpisodicMemoryEntry memory, string playerInput, List<string> topics, long snapshotTimeUtcTicks, Dictionary<string, float>? semanticScores)
    {
      // Calculate recency score using exponential decay based on snapshot time
      // Formula: recency = 0.5 ^ (elapsedTime / halfLife)
      // This ensures deterministic recency scoring that doesn't depend on wall-clock time
      float recencyScore;
      var elapsedTicks = snapshotTimeUtcTicks - memory.CreatedAtTicks;
      if (elapsedTicks <= 0)
      {
        // Memory created at or after snapshot time - treat as fresh
        recencyScore = 1.0f;
      }
      else if (_config.RecencyHalfLifeTicks <= 0)
      {
        // No decay configured - treat all as fresh
        recencyScore = 1.0f;
      }
      else
      {
        // Calculate exponential decay: 0.5 ^ (elapsed / halfLife)
        var halfLives = (double)elapsedTicks / _config.RecencyHalfLifeTicks;
        recencyScore = (float)Math.Pow(0.5, halfLives);

        // Apply significance boost: higher significance resists decay
        // Boost formula: recency = baseRecency * (1.0 + significance * 0.5), capped at 1.0
        recencyScore = Math.Min(1.0f, recencyScore * (1.0f + memory.Significance * 0.5f));
      }

      // Relevance score (0-1) - use hybrid if available, otherwise keyword-only
      var relevanceScore = CalculateRelevanceWithHybrid(memory.Id, memory.Content, playerInput, topics, semanticScores);

      // Significance score (0-1)
      var significanceScore = memory.Significance;

      return (_config.RecencyWeight * recencyScore) +
             (_config.RelevanceWeight * relevanceScore) +
             (_config.SignificanceWeight * significanceScore);
    }

    /// <summary>
    /// Scores a belief based on relevance and confidence.
    /// Relevance uses hybrid scoring (keyword + semantic) when semantic retrieval is enabled.
    /// </summary>
    private float ScoreBelief(BeliefMemoryEntry belief, string playerInput, List<string> topics, Dictionary<string, float>? semanticScores)
    {
      // Relevance score (0-1) - use hybrid if available, otherwise keyword-only
      var relevanceScore = CalculateRelevanceWithHybrid(belief.Id, belief.Content, playerInput, topics, semanticScores);
      var confidenceScore = belief.Confidence;

      // Penalize contradicted beliefs
      if (belief.IsContradicted)
      {
        confidenceScore *= 0.5f;
      }

      return (relevanceScore * 0.6f) + (confidenceScore * 0.4f);
    }

    /// <summary>
    /// Calculates relevance using hybrid scoring if available, otherwise falls back to keyword-only.
    /// </summary>
    private float CalculateRelevanceWithHybrid(string memoryId, string content, string playerInput, List<string> topics, Dictionary<string, float>? semanticScores)
    {
      if (_hybridCalculator != null && semanticScores != null)
      {
        return _hybridCalculator.CalculateHybridRelevance(memoryId, content, playerInput, topics, semanticScores);
      }

      // Fall back to keyword-only relevance
      return CalculateRelevance(content, playerInput, topics);
    }

    /// <summary>
    /// Calculates relevance of content to input and topics.
    /// Simple keyword matching - could be enhanced with embeddings.
    /// </summary>
    private float CalculateRelevance(string content, string playerInput, List<string> topics)
    {
      if (string.IsNullOrEmpty(content)) return 0;

      var contentLower = content.ToLowerInvariant();
      var inputLower = playerInput.ToLowerInvariant();

      float score = 0;

      // Check for keyword overlap with input
      var inputWords = inputLower.Split(new[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Where(w => w.Length > 3) // Skip short words
                                  .ToHashSet();

      var contentWords = contentLower.Split(new[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Where(w => w.Length > 3)
                                      .ToHashSet();

      var overlap = inputWords.Intersect(contentWords).Count();
      if (inputWords.Count > 0)
      {
        score = (float)overlap / inputWords.Count;
      }

      // Boost if matches any topic
      if (topics.Count > 0 && IsRelevantToTopics(content, topics))
      {
        score = Math.Min(1.0f, score + 0.3f);
      }

      return score;
    }

    /// <summary>
    /// Checks if content is relevant to any of the given topics.
    /// </summary>
    private bool IsRelevantToTopics(string content, List<string> topics)
    {
      if (string.IsNullOrEmpty(content) || topics.Count == 0) return false;

      var contentLower = content.ToLowerInvariant();
      return topics.Any(t => contentLower.Contains(t.ToLowerInvariant()));
    }

    /// <summary>
    /// Formats a belief for prompt inclusion.
    /// </summary>
    private string FormatBelief(BeliefMemoryEntry belief)
    {
      if (belief.IsContradicted)
      {
        return $"[Uncertain] {belief.Content}";
      }
      return belief.Content;
    }

    private static string Truncate(string text, int maxLength)
    {
      if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
      return text.Substring(0, maxLength) + "...";
    }

    /// <summary>
    /// Helper class for scored items.
    /// </summary>
    private class ScoredItem<T>
    {
      public T Item { get; }
      public float Score { get; }

      public ScoredItem(T item, float score)
      {
        Item = item;
        Score = score;
      }
    }
  }

  /// <summary>
  /// Container for retrieved context from the memory system.
  /// </summary>
  public class RetrievedContext
  {
    /// <summary>
    /// Retrieved canonical facts.
    /// </summary>
    public List<string> CanonicalFacts { get; set; } = new List<string>();

    /// <summary>
    /// Retrieved world state entries.
    /// </summary>
    public List<string> WorldState { get; set; } = new List<string>();

    /// <summary>
    /// Retrieved episodic memories (scored and filtered).
    /// </summary>
    public List<string> EpisodicMemories { get; set; } = new List<string>();

    /// <summary>
    /// Retrieved beliefs (scored and filtered).
    /// </summary>
    public List<string> Beliefs { get; set; } = new List<string>();

    /// <summary>
    /// Total number of retrieved items.
    /// </summary>
    public int TotalCount => CanonicalFacts.Count + WorldState.Count +
                             EpisodicMemories.Count + Beliefs.Count;

    /// <summary>
    /// Whether any context was retrieved.
    /// </summary>
    public bool HasContent => TotalCount > 0;

    /// <summary>
    /// Applies this retrieved context to a StateSnapshotBuilder.
    /// </summary>
    /// <param name="builder">The builder to apply context to.</param>
    /// <returns>The builder for chaining.</returns>
    public StateSnapshotBuilder ApplyTo(StateSnapshotBuilder builder)
    {
      return builder
        .WithCanonicalFacts(CanonicalFacts)
        .WithWorldState(WorldState)
        .WithEpisodicMemories(EpisodicMemories)
        .WithBeliefs(Beliefs);
    }
  }
}
