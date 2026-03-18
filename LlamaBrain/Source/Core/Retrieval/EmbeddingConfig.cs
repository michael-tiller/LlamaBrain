using System;

namespace LlamaBrain.Core.Retrieval
{
    /// <summary>
    /// Specifies which embedding provider implementation to use.
    /// </summary>
    public enum EmbeddingProviderType
    {
        /// <summary>
        /// No embedding provider - keyword-only mode.
        /// Uses NullEmbeddingProvider which always returns null.
        /// </summary>
        Null = 0,

        /// <summary>
        /// Local llama.cpp server with OpenAI-compatible /v1/embeddings endpoint.
        /// Requires llama.cpp server running with an embedding model loaded.
        /// </summary>
        LlamaCpp = 1,

        /// <summary>
        /// External OpenAI API (future implementation).
        /// </summary>
        OpenAI = 2
    }

    /// <summary>
    /// Configuration for the embedding and RAG retrieval system.
    /// </summary>
    public sealed class EmbeddingConfig
    {
        /// <summary>
        /// Which embedding provider to use.
        /// Default is Null (keyword-only mode) for backward compatibility.
        /// </summary>
        public EmbeddingProviderType ProviderType { get; set; } = EmbeddingProviderType.Null;

        /// <summary>
        /// Base URL for the llama.cpp embedding server.
        /// Only used when ProviderType is LlamaCpp.
        /// </summary>
        public string LlamaCppBaseUrl { get; set; } = "http://localhost:8080";

        /// <summary>
        /// Model name to send in llama.cpp requests.
        /// Some servers ignore this, but it's required by the OpenAI API format.
        /// </summary>
        public string LlamaCppModelName { get; set; } = "nomic-embed-text";

        /// <summary>
        /// Timeout for embedding requests in seconds.
        /// </summary>
        public int RequestTimeoutSeconds { get; set; } = 30;
        /// <summary>
        /// Whether RAG (semantic) retrieval is enabled.
        /// When false, only keyword matching is used (backward compatible).
        /// </summary>
        public bool EnableSemanticRetrieval { get; set; } = false;

        /// <summary>
        /// Weight for keyword-based relevance (0-1).
        /// Higher values give more importance to keyword matching.
        /// </summary>
        public float KeywordWeight { get; set; } = 0.3f;

        /// <summary>
        /// Weight for semantic similarity (0-1).
        /// Higher values give more importance to embedding-based similarity.
        /// KeywordWeight + SemanticWeight should sum to 1.0 for normalized scores.
        /// </summary>
        public float SemanticWeight { get; set; } = 0.7f;

        /// <summary>
        /// Minimum semantic similarity threshold (0-1).
        /// Memories below this threshold are not boosted by semantic score.
        /// </summary>
        public float MinSemanticSimilarity { get; set; } = 0.3f;

        /// <summary>
        /// Maximum number of candidates to consider from semantic search
        /// before combining with keyword scores.
        /// </summary>
        public int SemanticCandidateLimit { get; set; } = 50;

        /// <summary>
        /// The embedding dimension expected by the system.
        /// Must match the IEmbeddingProvider dimension.
        /// Common values: 384 (small models), 768 (base models), 1536 (large models).
        /// </summary>
        public int EmbeddingDimension { get; set; } = 384;

        /// <summary>
        /// Whether to automatically generate embeddings when memories are added.
        /// When false, embeddings must be generated explicitly via batch operations.
        /// </summary>
        public bool AutoGenerateEmbeddings { get; set; } = true;

        /// <summary>
        /// Batch size for embedding generation operations.
        /// Larger batches may be more efficient but use more memory.
        /// </summary>
        public int EmbeddingBatchSize { get; set; } = 10;

        /// <summary>
        /// Creates a keyword-only configuration (no semantic retrieval).
        /// This is the safest default for backward compatibility.
        /// </summary>
        public static EmbeddingConfig KeywordOnly() => new EmbeddingConfig
        {
            ProviderType = EmbeddingProviderType.Null,
            EnableSemanticRetrieval = false
        };

        /// <summary>
        /// Creates a default hybrid configuration with semantic retrieval enabled.
        /// Uses LlamaCpp provider pointing to localhost:8080.
        /// </summary>
        public static EmbeddingConfig Default() => new EmbeddingConfig
        {
            ProviderType = EmbeddingProviderType.LlamaCpp,
            EnableSemanticRetrieval = true,
            KeywordWeight = 0.3f,
            SemanticWeight = 0.7f,
            MinSemanticSimilarity = 0.3f,
            SemanticCandidateLimit = 50,
            EmbeddingDimension = 384,
            AutoGenerateEmbeddings = true,
            EmbeddingBatchSize = 10,
            LlamaCppBaseUrl = "http://localhost:8080",
            LlamaCppModelName = "nomic-embed-text",
            RequestTimeoutSeconds = 30
        };

        /// <summary>
        /// Creates a semantic-heavy configuration that prioritizes embedding similarity.
        /// Uses LlamaCpp provider pointing to localhost:8080.
        /// </summary>
        public static EmbeddingConfig SemanticHeavy() => new EmbeddingConfig
        {
            ProviderType = EmbeddingProviderType.LlamaCpp,
            EnableSemanticRetrieval = true,
            KeywordWeight = 0.1f,
            SemanticWeight = 0.9f,
            MinSemanticSimilarity = 0.4f,
            SemanticCandidateLimit = 100,
            EmbeddingDimension = 384,
            AutoGenerateEmbeddings = true,
            EmbeddingBatchSize = 10,
            LlamaCppBaseUrl = "http://localhost:8080",
            LlamaCppModelName = "nomic-embed-text",
            RequestTimeoutSeconds = 30
        };

        /// <summary>
        /// Creates a balanced configuration with equal keyword and semantic weights.
        /// Uses LlamaCpp provider pointing to localhost:8080.
        /// </summary>
        public static EmbeddingConfig Balanced() => new EmbeddingConfig
        {
            ProviderType = EmbeddingProviderType.LlamaCpp,
            EnableSemanticRetrieval = true,
            KeywordWeight = 0.5f,
            SemanticWeight = 0.5f,
            MinSemanticSimilarity = 0.3f,
            SemanticCandidateLimit = 50,
            EmbeddingDimension = 384,
            AutoGenerateEmbeddings = true,
            EmbeddingBatchSize = 10,
            LlamaCppBaseUrl = "http://localhost:8080",
            LlamaCppModelName = "nomic-embed-text",
            RequestTimeoutSeconds = 30
        };

        /// <summary>
        /// Validates the configuration and returns any errors.
        /// </summary>
        /// <returns>Array of error messages, or empty array if valid.</returns>
        public string[] Validate()
        {
            var errors = new System.Collections.Generic.List<string>();

            if (KeywordWeight < 0f || KeywordWeight > 1f)
                errors.Add($"KeywordWeight must be between 0 and 1, got {KeywordWeight}");

            if (SemanticWeight < 0f || SemanticWeight > 1f)
                errors.Add($"SemanticWeight must be between 0 and 1, got {SemanticWeight}");

            if (MinSemanticSimilarity < 0f || MinSemanticSimilarity > 1f)
                errors.Add($"MinSemanticSimilarity must be between 0 and 1, got {MinSemanticSimilarity}");

            if (SemanticCandidateLimit < 1)
                errors.Add($"SemanticCandidateLimit must be at least 1, got {SemanticCandidateLimit}");

            if (EmbeddingDimension < 1)
                errors.Add($"EmbeddingDimension must be at least 1, got {EmbeddingDimension}");

            if (EmbeddingBatchSize < 1)
                errors.Add($"EmbeddingBatchSize must be at least 1, got {EmbeddingBatchSize}");

            if (RequestTimeoutSeconds < 1)
                errors.Add($"RequestTimeoutSeconds must be at least 1, got {RequestTimeoutSeconds}");

            // Validate provider-specific settings
            if (ProviderType == EmbeddingProviderType.LlamaCpp)
            {
                if (string.IsNullOrWhiteSpace(LlamaCppBaseUrl))
                    errors.Add("LlamaCppBaseUrl is required when using LlamaCpp provider");

                if (!Uri.TryCreate(LlamaCppBaseUrl, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != "http" && uri.Scheme != "https"))
                {
                    errors.Add($"LlamaCppBaseUrl must be a valid HTTP(S) URL, got '{LlamaCppBaseUrl}'");
                }
            }

            // Warn if semantic retrieval enabled but using Null provider
            if (EnableSemanticRetrieval && ProviderType == EmbeddingProviderType.Null)
            {
                errors.Add("EnableSemanticRetrieval is true but ProviderType is Null - semantic retrieval will not function");
            }

            // Warn if weights don't sum to 1.0 (not an error, but unusual)
            var weightSum = KeywordWeight + SemanticWeight;
            if (EnableSemanticRetrieval && Math.Abs(weightSum - 1.0f) > 0.001f)
            {
                errors.Add($"KeywordWeight + SemanticWeight should sum to 1.0 for normalized scores, got {weightSum:F3}");
            }

            return errors.ToArray();
        }

        /// <summary>
        /// Creates a configuration for a specific llama.cpp server.
        /// </summary>
        /// <param name="baseUrl">Base URL of the llama.cpp server.</param>
        /// <param name="modelName">Model name for requests (optional).</param>
        /// <param name="embeddingDimension">Expected embedding dimension.</param>
        /// <param name="timeoutSeconds">Request timeout in seconds.</param>
        public static EmbeddingConfig ForLlamaCpp(
            string baseUrl,
            string modelName = "nomic-embed-text",
            int embeddingDimension = 384,
            int timeoutSeconds = 30) => new EmbeddingConfig
        {
            ProviderType = EmbeddingProviderType.LlamaCpp,
            EnableSemanticRetrieval = true,
            LlamaCppBaseUrl = baseUrl,
            LlamaCppModelName = modelName,
            EmbeddingDimension = embeddingDimension,
            RequestTimeoutSeconds = timeoutSeconds,
            KeywordWeight = 0.3f,
            SemanticWeight = 0.7f,
            MinSemanticSimilarity = 0.3f,
            SemanticCandidateLimit = 50,
            AutoGenerateEmbeddings = true,
            EmbeddingBatchSize = 10
        };
    }
}
