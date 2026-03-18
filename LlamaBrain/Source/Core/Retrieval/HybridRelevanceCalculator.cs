using System;
using System.Collections.Generic;
using System.Linq;

namespace LlamaBrain.Core.Retrieval
{
    /// <summary>
    /// Calculates hybrid relevance scores combining keyword matching and semantic similarity.
    /// Maintains determinism by using explicit ordering in all operations.
    /// </summary>
    public sealed class HybridRelevanceCalculator
    {
        private readonly EmbeddingConfig _config;
        private static readonly char[] WordSeparators = { ' ', '.', ',', '!', '?', ':', ';', '-', '"', '\'' };

        /// <summary>
        /// Creates a new hybrid relevance calculator.
        /// </summary>
        /// <param name="config">The embedding configuration.</param>
        public HybridRelevanceCalculator(EmbeddingConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Calculates hybrid relevance for a memory entry by combining keyword and semantic scores.
        /// </summary>
        /// <param name="memoryId">The memory's unique ID.</param>
        /// <param name="content">The memory content (for keyword matching).</param>
        /// <param name="playerInput">The player's input.</param>
        /// <param name="topics">Optional topics for boosting.</param>
        /// <param name="semanticScores">Pre-computed semantic scores from batch search (memoryId -> similarity).</param>
        /// <returns>Combined relevance score (0-1).</returns>
        public float CalculateHybridRelevance(
            string memoryId,
            string content,
            string playerInput,
            IReadOnlyList<string>? topics,
            IReadOnlyDictionary<string, float>? semanticScores)
        {
            // Calculate keyword score (existing logic)
            float keywordScore = CalculateKeywordRelevance(content, playerInput, topics);

            // If semantic retrieval is disabled or no scores available, return keyword only
            if (!_config.EnableSemanticRetrieval || semanticScores == null)
            {
                return keywordScore;
            }

            // Get semantic score if available
            float semanticScore = 0f;
            if (semanticScores.TryGetValue(memoryId, out var score))
            {
                semanticScore = score;
            }

            // Combine scores with configured weights
            return (_config.KeywordWeight * keywordScore) + (_config.SemanticWeight * semanticScore);
        }

        /// <summary>
        /// Calculates keyword-based relevance using word overlap.
        /// This is the same algorithm as the original ContextRetrievalLayer.CalculateRelevance().
        /// </summary>
        /// <param name="content">The memory content.</param>
        /// <param name="playerInput">The player's input.</param>
        /// <param name="topics">Optional topics for boosting.</param>
        /// <returns>Keyword relevance score (0-1).</returns>
        public float CalculateKeywordRelevance(string content, string playerInput, IReadOnlyList<string>? topics)
        {
            if (string.IsNullOrEmpty(content)) return 0;

            var contentLower = content.ToLowerInvariant();
            var inputLower = playerInput.ToLowerInvariant();

            float score = 0;

            // Check for keyword overlap with input
            var inputWords = inputLower.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries)
                                        .Where(w => w.Length > 3) // Skip short words
                                        .ToHashSet();

            var contentWords = contentLower.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries)
                                            .Where(w => w.Length > 3)
                                            .ToHashSet();

            var overlap = inputWords.Intersect(contentWords).Count();
            if (inputWords.Count > 0)
            {
                score = (float)overlap / inputWords.Count;
            }

            // Boost if matches any topic
            if (topics != null && topics.Count > 0 && IsRelevantToTopics(content, topics))
            {
                score = Math.Min(1.0f, score + 0.3f);
            }

            return score;
        }

        /// <summary>
        /// Checks if content is relevant to any of the given topics.
        /// </summary>
        /// <param name="content">The content to check.</param>
        /// <param name="topics">The topics to match against.</param>
        /// <returns>True if content contains any topic.</returns>
        public bool IsRelevantToTopics(string content, IReadOnlyList<string> topics)
        {
            if (string.IsNullOrEmpty(content) || topics == null || topics.Count == 0)
                return false;

            var contentLower = content.ToLowerInvariant();
            return topics.Any(t => contentLower.Contains(t.ToLowerInvariant()));
        }

        /// <summary>
        /// Extracts keywords from text for topic/keyword matching.
        /// </summary>
        /// <param name="text">The text to extract keywords from.</param>
        /// <returns>Set of lowercase keywords (length > 3).</returns>
        public HashSet<string> ExtractKeywords(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new HashSet<string>();

            return text.ToLowerInvariant()
                .Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3)
                .ToHashSet();
        }
    }
}
