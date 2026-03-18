using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LlamaBrain.Core.Retrieval
{
    /// <summary>
    /// Null implementation of IEmbeddingProvider that always returns null.
    /// Used when semantic retrieval is disabled or for testing keyword-only mode.
    /// </summary>
    public sealed class NullEmbeddingProvider : IEmbeddingProvider
    {
        /// <inheritdoc />
        public int EmbeddingDimension => 0;

        /// <inheritdoc />
        public bool IsAvailable => false;

        /// <inheritdoc />
        public Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<float[]?>(null);
        }

        /// <inheritdoc />
        public Task<float[]?[]> GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new float[]?[texts.Count]);
        }
    }
}
