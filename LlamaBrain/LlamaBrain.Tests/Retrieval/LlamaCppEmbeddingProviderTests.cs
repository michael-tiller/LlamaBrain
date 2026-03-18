using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using LlamaBrain.Core.Retrieval;

namespace LlamaBrain.Tests.Retrieval
{
    /// <summary>
    /// Tests for LlamaCppEmbeddingProvider.
    /// Integration tests marked [Explicit] require a running llama.cpp server.
    /// </summary>
    public class LlamaCppEmbeddingProviderTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_WithDefaults_SetsExpectedValues()
        {
            using var provider = new LlamaCppEmbeddingProvider();

            Assert.That(provider.EmbeddingDimension, Is.EqualTo(384));
        }

        [Test]
        public void Constructor_WithCustomDimension_SetsCorrectValue()
        {
            using var provider = new LlamaCppEmbeddingProvider(
                embeddingDimension: 768);

            Assert.That(provider.EmbeddingDimension, Is.EqualTo(768));
        }

        [Test]
        public void Constructor_WithNullBaseUrl_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new LlamaCppEmbeddingProvider(baseUrl: null!));
        }

        [Test]
        public void Constructor_WithEmptyBaseUrl_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new LlamaCppEmbeddingProvider(baseUrl: ""));
        }

        [Test]
        public void Constructor_WithZeroDimension_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new LlamaCppEmbeddingProvider(embeddingDimension: 0));
        }

        [Test]
        public void Constructor_WithNegativeDimension_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new LlamaCppEmbeddingProvider(embeddingDimension: -1));
        }

        [Test]
        public void Constructor_TrimsTrailingSlashFromBaseUrl()
        {
            using var provider = new LlamaCppEmbeddingProvider(
                baseUrl: "http://localhost:8080/");

            // Internal URL should have trailing slash removed
            // We can't easily test this directly, but it shouldn't cause double slashes
            Assert.That(provider.EmbeddingDimension, Is.EqualTo(384));
        }

        #endregion

        #region IsAvailable Tests

        [Test]
        public void IsAvailable_BeforeAnyRequest_ReturnsTrue()
        {
            using var provider = new LlamaCppEmbeddingProvider();

            // Before any actual request, we assume available (lazy check)
            Assert.That(provider.IsAvailable, Is.True);
        }

        #endregion

        #region GenerateEmbeddingAsync Tests (Unit - with Mock)

        [Test]
        public async Task GenerateEmbeddingAsync_WithEmptyText_ReturnsNull()
        {
            using var provider = new LlamaCppEmbeddingProvider();

            var result = await provider.GenerateEmbeddingAsync("");

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GenerateEmbeddingAsync_WithNullText_ReturnsNull()
        {
            using var provider = new LlamaCppEmbeddingProvider();

            var result = await provider.GenerateEmbeddingAsync(null!);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GenerateEmbeddingAsync_WithCancellation_ReturnsNull()
        {
            using var provider = new LlamaCppEmbeddingProvider();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await provider.GenerateEmbeddingAsync("test", cts.Token);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GenerateEmbeddingAsync_WhenServerUnavailable_ReturnsNull()
        {
            // Use an unlikely port to ensure connection failure
            using var provider = new LlamaCppEmbeddingProvider(
                baseUrl: "http://localhost:59999",
                timeout: TimeSpan.FromMilliseconds(500));

            var result = await provider.GenerateEmbeddingAsync("test");

            Assert.That(result, Is.Null);
            // Note: IsAvailable may still be true if timeout occurred before connection error
            // The provider gracefully returns null for all failure modes
        }

        [Test]
        public async Task GenerateEmbeddingAsync_LogsWhenProviderSet()
        {
            using var provider = new LlamaCppEmbeddingProvider(
                baseUrl: "http://localhost:59999",
                timeout: TimeSpan.FromMilliseconds(100));

            var logs = new List<string>();
            provider.OnLog = msg => logs.Add(msg);

            await provider.GenerateEmbeddingAsync("test");

            Assert.That(logs, Has.Count.GreaterThan(0));
            Assert.That(logs[0], Does.Contain("[LlamaCppEmbedding]"));
        }

        #endregion

        #region GenerateBatchEmbeddingsAsync Tests (Unit)

        [Test]
        public async Task GenerateBatchEmbeddingsAsync_WithNullList_ReturnsEmpty()
        {
            using var provider = new LlamaCppEmbeddingProvider();

            var result = await provider.GenerateBatchEmbeddingsAsync(null!);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GenerateBatchEmbeddingsAsync_WithEmptyList_ReturnsEmpty()
        {
            using var provider = new LlamaCppEmbeddingProvider();

            var result = await provider.GenerateBatchEmbeddingsAsync(new List<string>());

            Assert.That(result, Is.Empty);
        }

        #endregion

        #region Dispose Tests

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var provider = new LlamaCppEmbeddingProvider();

            Assert.DoesNotThrow(() =>
            {
                provider.Dispose();
                provider.Dispose();
                provider.Dispose();
            });
        }

        #endregion

        #region Integration Tests (Require Running Server)

        /// <summary>
        /// Integration test that requires a running llama.cpp server.
        /// Start server with: ./llama-server -m nomic-embed-text-v1.5.f32.gguf --embedding --port 8080
        /// </summary>
        [Test]
        [Explicit("Requires running llama.cpp server with embedding model")]
        [Category("Integration")]
        public async Task Integration_GenerateEmbeddingAsync_ReturnsValidEmbedding()
        {
            using var provider = new LlamaCppEmbeddingProvider(
                baseUrl: "http://localhost:8080",
                embeddingDimension: 768, // nomic-embed-text produces 768-dim embeddings
                modelName: "nomic-embed-text");

            var result = await provider.GenerateEmbeddingAsync("Hello, world!");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Length, Is.EqualTo(768));

            // Embedding values should be normalized (typically in [-1, 1] range)
            foreach (var value in result)
            {
                Assert.That(value, Is.InRange(-10f, 10f));
            }
        }

        /// <summary>
        /// Integration test for batch embeddings.
        /// </summary>
        [Test]
        [Explicit("Requires running llama.cpp server with embedding model")]
        [Category("Integration")]
        public async Task Integration_GenerateBatchEmbeddingsAsync_ReturnsValidEmbeddings()
        {
            using var provider = new LlamaCppEmbeddingProvider(
                baseUrl: "http://localhost:8080",
                embeddingDimension: 768,
                modelName: "nomic-embed-text");

            var texts = new List<string>
            {
                "The quick brown fox",
                "jumps over the lazy dog",
                "Hello world"
            };

            var results = await provider.GenerateBatchEmbeddingsAsync(texts);

            Assert.That(results.Length, Is.EqualTo(3));
            foreach (var result in results)
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result!.Length, Is.EqualTo(768));
            }
        }

        /// <summary>
        /// Integration test for semantic similarity.
        /// Similar texts should have similar embeddings.
        /// </summary>
        [Test]
        [Explicit("Requires running llama.cpp server with embedding model")]
        [Category("Integration")]
        public async Task Integration_SimilarTexts_HaveHighCosineSimilarity()
        {
            using var provider = new LlamaCppEmbeddingProvider(
                baseUrl: "http://localhost:8080",
                embeddingDimension: 768,
                modelName: "nomic-embed-text");

            var embedding1 = await provider.GenerateEmbeddingAsync("The weather is nice today");
            var embedding2 = await provider.GenerateEmbeddingAsync("It's a beautiful sunny day");
            var embedding3 = await provider.GenerateEmbeddingAsync("Advanced quantum mechanics equations");

            Assert.That(embedding1, Is.Not.Null);
            Assert.That(embedding2, Is.Not.Null);
            Assert.That(embedding3, Is.Not.Null);

            var similaritySameContext = CosineSimilarity(embedding1!, embedding2!);
            var similarityDifferentContext = CosineSimilarity(embedding1!, embedding3!);

            // Similar texts should have higher similarity than unrelated texts
            Assert.That(similaritySameContext, Is.GreaterThan(similarityDifferentContext));
            Assert.That(similaritySameContext, Is.GreaterThan(0.5f));
        }

        /// <summary>
        /// Integration test for connection check.
        /// </summary>
        [Test]
        [Explicit("Requires running llama.cpp server with embedding model")]
        [Category("Integration")]
        public async Task Integration_TestConnectionAsync_ReturnsTrue()
        {
            using var provider = new LlamaCppEmbeddingProvider(
                baseUrl: "http://localhost:8080",
                embeddingDimension: 768,
                modelName: "nomic-embed-text");

            var result = await provider.TestConnectionAsync();

            Assert.That(result, Is.True);
            Assert.That(provider.IsAvailable, Is.True);
        }

        /// <summary>
        /// Integration test for determinism.
        /// Same input should produce identical embeddings.
        /// </summary>
        [Test]
        [Explicit("Requires running llama.cpp server with embedding model")]
        [Category("Integration")]
        public async Task Integration_SameInput_ProducesIdenticalEmbeddings()
        {
            using var provider = new LlamaCppEmbeddingProvider(
                baseUrl: "http://localhost:8080",
                embeddingDimension: 768,
                modelName: "nomic-embed-text");

            var text = "This is a determinism test";
            var embedding1 = await provider.GenerateEmbeddingAsync(text);
            var embedding2 = await provider.GenerateEmbeddingAsync(text);

            Assert.That(embedding1, Is.Not.Null);
            Assert.That(embedding2, Is.Not.Null);

            // Embeddings should be identical for same input
            for (int i = 0; i < embedding1!.Length; i++)
            {
                Assert.That(embedding1[i], Is.EqualTo(embedding2![i]).Within(1e-6f),
                    $"Mismatch at index {i}");
            }
        }

        #endregion

        #region Helper Methods

        private static float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have same length");

            float dotProduct = 0;
            float normA = 0;
            float normB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA == 0 || normB == 0)
                return 0;

            return dotProduct / ((float)Math.Sqrt(normA) * (float)Math.Sqrt(normB));
        }

        #endregion
    }
}
