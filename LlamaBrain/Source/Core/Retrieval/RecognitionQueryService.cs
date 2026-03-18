using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LlamaBrain.Persona;

namespace LlamaBrain.Core.Retrieval
{
    /// <summary>
    /// Service for querying whether locations, topics, or conversation patterns
    /// have been encountered before. Supports both semantic search (RAG) and
    /// keyword fallback for robustness.
    /// </summary>
    public sealed class RecognitionQueryService
    {
        private readonly IMemoryVectorStore? _vectorStore;
        private readonly IEmbeddingProvider? _embeddingProvider;
        private readonly AuthoritativeMemorySystem _memorySystem;
        private readonly RecognitionConfig _config;
        private readonly HybridRelevanceCalculator? _relevanceCalculator;

        private static readonly char[] WordSeparators = { ' ', '.', ',', '!', '?', ':', ';', '-', '"', '\'' };

        /// <summary>
        /// Creates a new recognition query service.
        /// </summary>
        /// <param name="memorySystem">The memory system to query.</param>
        /// <param name="vectorStore">Optional vector store for semantic search.</param>
        /// <param name="embeddingProvider">Optional embedding provider for semantic search.</param>
        /// <param name="config">Optional recognition configuration.</param>
        public RecognitionQueryService(
            AuthoritativeMemorySystem memorySystem,
            IMemoryVectorStore? vectorStore = null,
            IEmbeddingProvider? embeddingProvider = null,
            RecognitionConfig? config = null)
        {
            _memorySystem = memorySystem ?? throw new ArgumentNullException(nameof(memorySystem));
            _vectorStore = vectorStore;
            _embeddingProvider = embeddingProvider;
            _config = config ?? RecognitionConfig.Default();

            // Create relevance calculator if we have embedding support
            if (_embeddingProvider != null && _embeddingProvider.IsAvailable)
            {
                var embeddingConfig = new EmbeddingConfig
                {
                    EnableSemanticRetrieval = true,
                    EmbeddingDimension = _embeddingProvider.EmbeddingDimension
                };
                _relevanceCalculator = new HybridRelevanceCalculator(embeddingConfig);
            }
        }

        /// <summary>
        /// Whether semantic search is available (vector store and embeddings configured).
        /// </summary>
        public bool IsSemanticSearchAvailable =>
            _vectorStore != null &&
            _embeddingProvider != null &&
            _embeddingProvider.IsAvailable;

        /// <summary>
        /// Queries for location recognition by searching episodic memories
        /// for previous visits to the specified location.
        /// </summary>
        /// <param name="npcId">The NPC whose memories to search (null includes shared only).</param>
        /// <param name="locationId">The location identifier or name to search for.</param>
        /// <returns>Recognition result indicating whether location was visited before.</returns>
        public RecognitionResult QueryLocationRecognition(string? npcId, string locationId)
        {
            if (string.IsNullOrEmpty(locationId))
            {
                return RecognitionResult.NotRecognized();
            }

            var locationLower = locationId.ToLowerInvariant();
            var matchedMemories = new List<(string id, long ticks)>();

            // Search episodic memories for location mentions
            foreach (var memory in _memorySystem.GetActiveEpisodicMemories())
            {
                // Check explicit LocationId property first (exact match, case-insensitive)
                if (!string.IsNullOrEmpty(memory.LocationId) &&
                    memory.LocationId.Equals(locationId, System.StringComparison.OrdinalIgnoreCase))
                {
                    matchedMemories.Add((memory.Id, memory.CreatedAtTicks));
                    continue;
                }

                var descLower = memory.Description.ToLowerInvariant();

                // Check for location in description (fallback for legacy memories)
                if (descLower.Contains(locationLower) ||
                    ContainsLocationPattern(descLower, locationLower))
                {
                    matchedMemories.Add((memory.Id, memory.CreatedAtTicks));
                }
            }

            // Recognition requires at least 2 visits (first visit = no recognition)
            if (matchedMemories.Count < 2)
            {
                return RecognitionResult.NotRecognized();
            }

            // Sort by time for deterministic ordering
            var sortedMatches = matchedMemories
                .OrderByDescending(m => m.ticks)
                .ThenBy(m => m.id, StringComparer.Ordinal)
                .ToList();

            var matchedIds = sortedMatches.Select(m => m.id).ToList();
            var lastOccurrence = sortedMatches.First().ticks;

            return RecognitionResult.LocationRecognized(
                repeatCount: matchedMemories.Count,
                lastVisitTicks: lastOccurrence,
                matchedMemoryIds: matchedIds);
        }

        /// <summary>
        /// Queries for topic recognition using semantic similarity search
        /// with keyword fallback.
        /// </summary>
        /// <param name="npcId">The NPC whose memories to search.</param>
        /// <param name="playerInput">The player's input to find similar topics for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Recognition result indicating whether topic was discussed before.</returns>
        public async Task<RecognitionResult> QueryTopicRecognitionAsync(
            string? npcId,
            string playerInput,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(playerInput))
            {
                return RecognitionResult.NotRecognized();
            }

            // Try semantic search first
            if (IsSemanticSearchAvailable)
            {
                var semanticResult = await QueryTopicSemanticAsync(npcId, playerInput, cancellationToken);
                if (semanticResult.Recognized)
                {
                    return semanticResult;
                }
            }

            // Fall back to keyword matching
            if (_config.EnableKeywordFallback)
            {
                return QueryTopicByKeyword(npcId, playerInput);
            }

            return RecognitionResult.NotRecognized();
        }

        /// <summary>
        /// Queries for conversation pattern recognition (e.g., repeated questions).
        /// Uses semantic similarity to find similar previous conversations.
        /// </summary>
        /// <param name="npcId">The NPC whose memories to search.</param>
        /// <param name="playerInput">The player's current input.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Recognition result indicating whether conversation pattern is recurring.</returns>
        public async Task<RecognitionResult> QueryConversationPatternAsync(
            string? npcId,
            string playerInput,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(playerInput))
            {
                return RecognitionResult.NotRecognized();
            }

            // Search for similar dialogue patterns
            if (IsSemanticSearchAvailable)
            {
                var semanticResult = await QueryConversationSemanticAsync(npcId, playerInput, cancellationToken);
                if (semanticResult.Recognized)
                {
                    return semanticResult;
                }
            }

            // Fall back to exact/near-exact matching
            if (_config.EnableKeywordFallback)
            {
                return QueryConversationByKeyword(npcId, playerInput);
            }

            return RecognitionResult.NotRecognized();
        }

        /// <summary>
        /// Synchronous topic recognition using keyword matching only.
        /// Use this when embeddings are unavailable or for deterministic testing.
        /// </summary>
        /// <param name="npcId">The NPC whose memories to search.</param>
        /// <param name="playerInput">The player's input to find similar topics for.</param>
        /// <returns>Recognition result based on keyword overlap.</returns>
        public RecognitionResult QueryTopicRecognitionKeywordOnly(string? npcId, string playerInput)
        {
            return QueryTopicByKeyword(npcId, playerInput);
        }

        private async Task<RecognitionResult> QueryTopicSemanticAsync(
            string? npcId,
            string playerInput,
            CancellationToken cancellationToken)
        {
            // Generate embedding for player input
            var queryEmbedding = await _embeddingProvider!.GenerateEmbeddingAsync(playerInput, cancellationToken);
            if (queryEmbedding == null)
            {
                return RecognitionResult.NotRecognized();
            }

            // Search vector store for similar memories
            var searchResults = _vectorStore!.FindSimilar(
                queryEmbedding,
                _config.TopicSearchLimit,
                npcId,
                MemoryVectorType.Episodic,
                _config.TopicSimilarityThreshold);

            if (searchResults.Count == 0)
            {
                return RecognitionResult.NotRecognized();
            }

            // Filter results above threshold and collect matched IDs
            var matches = searchResults
                .Where(r => r.Similarity >= _config.TopicSimilarityThreshold)
                .OrderByDescending(r => r.Similarity)
                .ThenBy(r => r.SequenceNumber)
                .ThenBy(r => r.MemoryId, StringComparer.Ordinal)
                .ToList();

            if (matches.Count == 0)
            {
                return RecognitionResult.NotRecognized();
            }

            var matchedIds = matches.Select(m => m.MemoryId).ToList();
            var bestMatch = matches.First();

            // Extract topic summary from input
            var topicSummary = ExtractTopicSummary(playerInput);

            return RecognitionResult.TopicRecognized(
                repeatCount: matches.Count,
                lastDiscussionTicks: GetLastOccurrenceTicks(matchedIds),
                matchedMemoryIds: matchedIds,
                similarity: bestMatch.Similarity,
                topicSummary: topicSummary);
        }

        private async Task<RecognitionResult> QueryConversationSemanticAsync(
            string? npcId,
            string playerInput,
            CancellationToken cancellationToken)
        {
            // Generate embedding for player input
            var queryEmbedding = await _embeddingProvider!.GenerateEmbeddingAsync(playerInput, cancellationToken);
            if (queryEmbedding == null)
            {
                return RecognitionResult.NotRecognized();
            }

            // Search for similar dialogues with higher threshold
            var searchResults = _vectorStore!.FindSimilar(
                queryEmbedding,
                _config.TopicSearchLimit,
                npcId,
                MemoryVectorType.Episodic,
                _config.ConversationSimilarityThreshold);

            if (searchResults.Count == 0)
            {
                return RecognitionResult.NotRecognized();
            }

            // Filter to very similar matches (conversation repetition)
            var matches = searchResults
                .Where(r => r.Similarity >= _config.ConversationSimilarityThreshold)
                .OrderByDescending(r => r.Similarity)
                .ThenBy(r => r.SequenceNumber)
                .ThenBy(r => r.MemoryId, StringComparer.Ordinal)
                .ToList();

            if (matches.Count == 0)
            {
                return RecognitionResult.NotRecognized();
            }

            var matchedIds = matches.Select(m => m.MemoryId).ToList();
            var bestMatch = matches.First();

            // Create pattern summary
            var patternSummary = playerInput.Length > 50
                ? playerInput.Substring(0, 47) + "..."
                : playerInput;

            return RecognitionResult.ConversationRecognized(
                repeatCount: matches.Count,
                lastOccurrenceTicks: GetLastOccurrenceTicks(matchedIds),
                matchedMemoryIds: matchedIds,
                similarity: bestMatch.Similarity,
                patternSummary: patternSummary);
        }

        private RecognitionResult QueryTopicByKeyword(string? npcId, string playerInput)
        {
            var inputWords = ExtractKeywords(playerInput);
            if (inputWords.Count == 0)
            {
                return RecognitionResult.NotRecognized();
            }

            var matchedMemories = new List<(string id, long ticks, float overlap)>();

            foreach (var memory in _memorySystem.GetActiveEpisodicMemories())
            {
                var memoryWords = ExtractKeywords(memory.Description);
                if (memoryWords.Count == 0) continue;

                var overlap = inputWords.Intersect(memoryWords).Count();
                var overlapRatio = (float)overlap / inputWords.Count;

                if (overlapRatio >= _config.KeywordFallbackThreshold)
                {
                    matchedMemories.Add((memory.Id, memory.CreatedAtTicks, overlapRatio));
                }
            }

            if (matchedMemories.Count == 0)
            {
                return RecognitionResult.NotRecognized();
            }

            // Sort by overlap (descending), then time, then ID for determinism
            var sortedMatches = matchedMemories
                .OrderByDescending(m => m.overlap)
                .ThenByDescending(m => m.ticks)
                .ThenBy(m => m.id, StringComparer.Ordinal)
                .ToList();

            var matchedIds = sortedMatches.Select(m => m.id).ToList();
            var bestOverlap = sortedMatches.First().overlap;
            var topicSummary = ExtractTopicSummary(playerInput);

            return RecognitionResult.TopicRecognized(
                repeatCount: matchedMemories.Count,
                lastDiscussionTicks: sortedMatches.First().ticks,
                matchedMemoryIds: matchedIds,
                similarity: bestOverlap,
                topicSummary: topicSummary);
        }

        private RecognitionResult QueryConversationByKeyword(string? npcId, string playerInput)
        {
            var inputLower = playerInput.ToLowerInvariant().Trim();
            var matchedMemories = new List<(string id, long ticks, float similarity)>();

            foreach (var memory in _memorySystem.GetActiveEpisodicMemories())
            {
                var memoryLower = memory.Description.ToLowerInvariant().Trim();

                // Check for near-exact match (high similarity for conversation recognition)
                float similarity = CalculateStringSimilarity(inputLower, memoryLower);

                if (similarity >= _config.ConversationSimilarityThreshold)
                {
                    matchedMemories.Add((memory.Id, memory.CreatedAtTicks, similarity));
                }
            }

            if (matchedMemories.Count == 0)
            {
                return RecognitionResult.NotRecognized();
            }

            var sortedMatches = matchedMemories
                .OrderByDescending(m => m.similarity)
                .ThenByDescending(m => m.ticks)
                .ThenBy(m => m.id, StringComparer.Ordinal)
                .ToList();

            var matchedIds = sortedMatches.Select(m => m.id).ToList();
            var bestSimilarity = sortedMatches.First().similarity;

            var patternSummary = playerInput.Length > 50
                ? playerInput.Substring(0, 47) + "..."
                : playerInput;

            return RecognitionResult.ConversationRecognized(
                repeatCount: matchedMemories.Count,
                lastOccurrenceTicks: sortedMatches.First().ticks,
                matchedMemoryIds: matchedIds,
                similarity: bestSimilarity,
                patternSummary: patternSummary);
        }

        private bool ContainsLocationPattern(string text, string locationId)
        {
            // Common patterns for location mentions
            var patterns = new[]
            {
                $"entered {locationId}",
                $"arrived at {locationId}",
                $"visited {locationId}",
                $"went to {locationId}",
                $"at the {locationId}",
                $"in {locationId}",
                $"location: {locationId}"
            };

            return patterns.Any(p => text.Contains(p));
        }

        private HashSet<string> ExtractKeywords(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new HashSet<string>();
            }

            return text.ToLowerInvariant()
                .Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3) // Skip short words
                .ToHashSet();
        }

        private string ExtractTopicSummary(string input)
        {
            // Extract first few meaningful words as topic summary
            var words = input.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .Take(5);

            var summary = string.Join(" ", words);
            return summary.Length > 30 ? summary.Substring(0, 27) + "..." : summary;
        }

        private float CalculateStringSimilarity(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            {
                return 0f;
            }

            // Exact match
            if (a == b)
            {
                return 1.0f;
            }

            // Check if one contains the other
            if (a.Contains(b) || b.Contains(a))
            {
                var shorter = a.Length < b.Length ? a : b;
                var longer = a.Length >= b.Length ? a : b;
                return (float)shorter.Length / longer.Length;
            }

            // Word overlap based similarity
            var wordsA = ExtractKeywords(a);
            var wordsB = ExtractKeywords(b);

            if (wordsA.Count == 0 || wordsB.Count == 0)
            {
                return 0f;
            }

            var intersection = wordsA.Intersect(wordsB).Count();
            var union = wordsA.Union(wordsB).Count();

            return union > 0 ? (float)intersection / union : 0f;
        }

        private long GetLastOccurrenceTicks(IReadOnlyList<string> memoryIds)
        {
            long maxTicks = 0;

            foreach (var id in memoryIds)
            {
                // Search through episodic memories to find the entry
                foreach (var memory in _memorySystem.GetActiveEpisodicMemories())
                {
                    if (memory.Id == id && memory.CreatedAtTicks > maxTicks)
                    {
                        maxTicks = memory.CreatedAtTicks;
                        break;
                    }
                }
            }

            return maxTicks;
        }
    }
}
