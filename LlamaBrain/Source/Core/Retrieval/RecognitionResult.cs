using System;
using System.Collections.Generic;

namespace LlamaBrain.Core.Retrieval
{
    /// <summary>
    /// Types of recognition that can be detected.
    /// </summary>
    public enum RecognitionType
    {
        /// <summary>No recognition detected.</summary>
        None = 0,

        /// <summary>Location has been visited before.</summary>
        Location = 1,

        /// <summary>Topic has been discussed before.</summary>
        Topic = 2,

        /// <summary>Conversation pattern is recurring.</summary>
        Conversation = 3
    }

    /// <summary>
    /// Result of a recognition query indicating whether something has been encountered before.
    /// Used to enable NPCs to recognize repeated locations, topics, or conversation patterns.
    /// </summary>
    public sealed class RecognitionResult
    {
        /// <summary>
        /// Whether recognition was detected.
        /// </summary>
        public bool Recognized { get; }

        /// <summary>
        /// The type of recognition detected.
        /// </summary>
        public RecognitionType RecognitionType { get; }

        /// <summary>
        /// Number of times this has been encountered before (excluding current).
        /// </summary>
        public int RepeatCount { get; }

        /// <summary>
        /// Ticks of the last occurrence (for determining recency).
        /// </summary>
        public long LastOccurrenceTicks { get; }

        /// <summary>
        /// Human-readable summary of the evidence for recognition.
        /// Useful for debugging and prompt injection.
        /// </summary>
        public string EvidenceSummary { get; }

        /// <summary>
        /// IDs of memories that matched for this recognition.
        /// </summary>
        public IReadOnlyList<string> MatchedMemoryIds { get; }

        /// <summary>
        /// Similarity score of the best match (for topic/semantic recognition).
        /// Range -1 to 1, where 1 is exact match.
        /// </summary>
        public float BestMatchSimilarity { get; }

        /// <summary>
        /// Creates a new recognition result.
        /// </summary>
        public RecognitionResult(
            bool recognized,
            RecognitionType recognitionType,
            int repeatCount,
            long lastOccurrenceTicks,
            string evidenceSummary,
            IReadOnlyList<string> matchedMemoryIds,
            float bestMatchSimilarity = 0f)
        {
            Recognized = recognized;
            RecognitionType = recognitionType;
            RepeatCount = repeatCount;
            LastOccurrenceTicks = lastOccurrenceTicks;
            EvidenceSummary = evidenceSummary ?? "";
            MatchedMemoryIds = matchedMemoryIds ?? Array.Empty<string>();
            BestMatchSimilarity = bestMatchSimilarity;
        }

        /// <summary>
        /// Creates a result indicating no recognition.
        /// </summary>
        public static RecognitionResult NotRecognized()
        {
            return new RecognitionResult(
                recognized: false,
                recognitionType: RecognitionType.None,
                repeatCount: 0,
                lastOccurrenceTicks: 0,
                evidenceSummary: "",
                matchedMemoryIds: Array.Empty<string>(),
                bestMatchSimilarity: 0f);
        }

        /// <summary>
        /// Creates a location recognition result.
        /// </summary>
        /// <param name="repeatCount">Number of previous visits.</param>
        /// <param name="lastVisitTicks">Ticks of last visit.</param>
        /// <param name="matchedMemoryIds">IDs of matching episodic memories.</param>
        public static RecognitionResult LocationRecognized(int repeatCount, long lastVisitTicks, IReadOnlyList<string> matchedMemoryIds)
        {
            return new RecognitionResult(
                recognized: true,
                recognitionType: RecognitionType.Location,
                repeatCount: repeatCount,
                lastOccurrenceTicks: lastVisitTicks,
                evidenceSummary: $"Location visited {repeatCount} time(s) before",
                matchedMemoryIds: matchedMemoryIds,
                bestMatchSimilarity: 1.0f);
        }

        /// <summary>
        /// Creates a topic recognition result.
        /// </summary>
        /// <param name="repeatCount">Number of times topic was discussed.</param>
        /// <param name="lastDiscussionTicks">Ticks of last discussion.</param>
        /// <param name="matchedMemoryIds">IDs of matching memories.</param>
        /// <param name="similarity">Best match similarity score.</param>
        /// <param name="topicSummary">Brief description of the recognized topic.</param>
        public static RecognitionResult TopicRecognized(
            int repeatCount,
            long lastDiscussionTicks,
            IReadOnlyList<string> matchedMemoryIds,
            float similarity,
            string topicSummary)
        {
            return new RecognitionResult(
                recognized: true,
                recognitionType: RecognitionType.Topic,
                repeatCount: repeatCount,
                lastOccurrenceTicks: lastDiscussionTicks,
                evidenceSummary: $"Topic '{topicSummary}' discussed {repeatCount} time(s) before",
                matchedMemoryIds: matchedMemoryIds,
                bestMatchSimilarity: similarity);
        }

        /// <summary>
        /// Creates a conversation pattern recognition result.
        /// </summary>
        /// <param name="repeatCount">Number of times pattern occurred.</param>
        /// <param name="lastOccurrenceTicks">Ticks of last occurrence.</param>
        /// <param name="matchedMemoryIds">IDs of matching memories.</param>
        /// <param name="similarity">Best match similarity score.</param>
        /// <param name="patternSummary">Brief description of the pattern.</param>
        public static RecognitionResult ConversationRecognized(
            int repeatCount,
            long lastOccurrenceTicks,
            IReadOnlyList<string> matchedMemoryIds,
            float similarity,
            string patternSummary)
        {
            return new RecognitionResult(
                recognized: true,
                recognitionType: RecognitionType.Conversation,
                repeatCount: repeatCount,
                lastOccurrenceTicks: lastOccurrenceTicks,
                evidenceSummary: $"Conversation pattern '{patternSummary}' occurred {repeatCount} time(s) before",
                matchedMemoryIds: matchedMemoryIds,
                bestMatchSimilarity: similarity);
        }

        public override string ToString()
        {
            if (!Recognized)
            {
                return "RecognitionResult: Not recognized";
            }

            return $"RecognitionResult: {RecognitionType}, RepeatCount={RepeatCount}, Similarity={BestMatchSimilarity:F3}, {EvidenceSummary}";
        }
    }

    /// <summary>
    /// Configuration for recognition queries.
    /// </summary>
    public sealed class RecognitionConfig
    {
        /// <summary>
        /// Minimum similarity threshold for topic recognition (0-1).
        /// Higher values require closer semantic match.
        /// </summary>
        public float TopicSimilarityThreshold { get; set; } = 0.6f;

        /// <summary>
        /// Minimum similarity threshold for conversation pattern recognition (0-1).
        /// </summary>
        public float ConversationSimilarityThreshold { get; set; } = 0.7f;

        /// <summary>
        /// Maximum number of memories to search for topic recognition.
        /// </summary>
        public int TopicSearchLimit { get; set; } = 20;

        /// <summary>
        /// Whether to use keyword fallback when embeddings are unavailable.
        /// </summary>
        public bool EnableKeywordFallback { get; set; } = true;

        /// <summary>
        /// Minimum keyword overlap ratio for fallback recognition (0-1).
        /// </summary>
        public float KeywordFallbackThreshold { get; set; } = 0.5f;

        /// <summary>
        /// Creates default configuration.
        /// </summary>
        public static RecognitionConfig Default()
        {
            return new RecognitionConfig();
        }

        /// <summary>
        /// Creates strict configuration requiring higher similarity.
        /// </summary>
        public static RecognitionConfig Strict()
        {
            return new RecognitionConfig
            {
                TopicSimilarityThreshold = 0.75f,
                ConversationSimilarityThreshold = 0.8f,
                KeywordFallbackThreshold = 0.6f
            };
        }

        /// <summary>
        /// Creates lenient configuration with lower thresholds.
        /// </summary>
        public static RecognitionConfig Lenient()
        {
            return new RecognitionConfig
            {
                TopicSimilarityThreshold = 0.5f,
                ConversationSimilarityThreshold = 0.6f,
                KeywordFallbackThreshold = 0.4f
            };
        }
    }
}
