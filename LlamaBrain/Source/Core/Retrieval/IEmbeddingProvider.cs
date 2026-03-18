using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LlamaBrain.Core.Retrieval
{
    /// <summary>
    /// Provides embedding generation for semantic search.
    /// Implementations can use local models (llama.cpp) or external APIs (OpenAI).
    /// </summary>
    public interface IEmbeddingProvider
    {
        /// <summary>
        /// Generates an embedding vector for the given text.
        /// </summary>
        /// <param name="text">The text to embed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Embedding vector as float array, or null if generation fails.</returns>
        Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates embeddings for multiple texts in a batch.
        /// More efficient than calling GenerateEmbeddingAsync repeatedly.
        /// </summary>
        /// <param name="texts">The texts to embed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Array of embedding vectors, with null entries for failed generations.</returns>
        Task<float[]?[]> GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);

        /// <summary>
        /// The dimension of embeddings produced by this provider.
        /// </summary>
        int EmbeddingDimension { get; }

        /// <summary>
        /// Whether this provider is available and ready to generate embeddings.
        /// </summary>
        bool IsAvailable { get; }
    }
}
