using System;
using System.Collections.Generic;
using System.Linq;

namespace LlamaBrain.Core.Retrieval
{
    /// <summary>
    /// In-memory vector store using Dictionary for storage.
    /// Suitable for memory sets under ~1000 entries.
    /// Provides O(n) search via brute-force cosine similarity.
    /// </summary>
    public sealed class InMemoryVectorStore : IMemoryVectorStore
    {
        private readonly Dictionary<string, StoredVectorEntry> _vectors = new Dictionary<string, StoredVectorEntry>();
        private readonly int _embeddingDimension;

        /// <summary>
        /// Creates a new in-memory vector store.
        /// </summary>
        /// <param name="embeddingDimension">The dimension of embeddings to store.</param>
        public InMemoryVectorStore(int embeddingDimension)
        {
            if (embeddingDimension < 1)
                throw new ArgumentOutOfRangeException(nameof(embeddingDimension), "Embedding dimension must be at least 1");

            _embeddingDimension = embeddingDimension;
        }

        /// <inheritdoc />
        public void Upsert(string memoryId, string? npcId, MemoryVectorType memoryType, float[] embedding, long sequenceNumber)
        {
            if (string.IsNullOrEmpty(memoryId))
                throw new ArgumentNullException(nameof(memoryId));

            if (embedding == null)
                throw new ArgumentNullException(nameof(embedding));

            if (embedding.Length != _embeddingDimension)
                throw new ArgumentException($"Embedding dimension mismatch: expected {_embeddingDimension}, got {embedding.Length}", nameof(embedding));

            // Store a copy of the embedding to prevent external modification
            var embeddingCopy = new float[embedding.Length];
            Array.Copy(embedding, embeddingCopy, embedding.Length);

            _vectors[memoryId] = new StoredVectorEntry(
                memoryId,
                npcId,
                memoryType,
                embeddingCopy,
                sequenceNumber);
        }

        /// <inheritdoc />
        public bool Remove(string memoryId)
        {
            return _vectors.Remove(memoryId);
        }

        /// <inheritdoc />
        public IReadOnlyList<VectorSearchResult> FindSimilar(
            float[] queryEmbedding,
            int k,
            string? npcId,
            MemoryVectorType? memoryType = null,
            float minSimilarity = 0.0f)
        {
            if (queryEmbedding == null)
                throw new ArgumentNullException(nameof(queryEmbedding));

            if (queryEmbedding.Length != _embeddingDimension)
                throw new ArgumentException($"Query embedding dimension mismatch: expected {_embeddingDimension}, got {queryEmbedding.Length}", nameof(queryEmbedding));

            if (k < 1)
                throw new ArgumentOutOfRangeException(nameof(k), "k must be at least 1");

            // Filter entries by NPC (include shared entries where NpcId is null)
            var filteredEntries = _vectors.Values
                .Where(entry => entry.NpcId == null || entry.NpcId == npcId);

            // Filter by memory type if specified
            if (memoryType.HasValue)
            {
                filteredEntries = filteredEntries.Where(entry => entry.MemoryType == memoryType.Value);
            }

            // Calculate similarity for each entry
            var scoredEntries = filteredEntries
                .Select(entry => new
                {
                    Entry = entry,
                    Similarity = CosineSimilarity(queryEmbedding, entry.Embedding)
                })
                .Where(x => x.Similarity >= minSimilarity);

            // Sort with deterministic tie-breaking:
            // 1. Similarity (descending) - highest similarity first
            // 2. SequenceNumber (ascending) - older entries first when similarity is equal
            // 3. MemoryId (ordinal ascending) - stable string comparison as final tie-breaker
            var sortedResults = scoredEntries
                .OrderByDescending(x => x.Similarity)
                .ThenBy(x => x.Entry.SequenceNumber)
                .ThenBy(x => x.Entry.MemoryId, StringComparer.Ordinal)
                .Take(k)
                .Select(x => new VectorSearchResult(
                    x.Entry.MemoryId,
                    x.Entry.NpcId,
                    x.Similarity,
                    x.Entry.SequenceNumber,
                    x.Entry.MemoryType))
                .ToList();

            return sortedResults;
        }

        /// <inheritdoc />
        public VectorStoreStatistics GetStatistics()
        {
            int episodic = 0, belief = 0, canonical = 0, worldState = 0;

            foreach (var entry in _vectors.Values)
            {
                switch (entry.MemoryType)
                {
                    case MemoryVectorType.Episodic:
                        episodic++;
                        break;
                    case MemoryVectorType.Belief:
                        belief++;
                        break;
                    case MemoryVectorType.CanonicalFact:
                        canonical++;
                        break;
                    case MemoryVectorType.WorldState:
                        worldState++;
                        break;
                }
            }

            return new VectorStoreStatistics(
                _vectors.Count,
                episodic,
                belief,
                canonical,
                worldState,
                _embeddingDimension);
        }

        /// <inheritdoc />
        public void Clear()
        {
            _vectors.Clear();
        }

        /// <inheritdoc />
        public VectorStoreSnapshot CreateSnapshot()
        {
            var snapshot = new VectorStoreSnapshot
            {
                EmbeddingDimension = _embeddingDimension,
                Entries = new List<VectorEntry>(_vectors.Count)
            };

            // Sort by SequenceNumber for deterministic snapshot ordering
            foreach (var entry in _vectors.Values.OrderBy(e => e.SequenceNumber).ThenBy(e => e.MemoryId, StringComparer.Ordinal))
            {
                // Copy the embedding to prevent external modification
                var embeddingCopy = new float[entry.Embedding.Length];
                Array.Copy(entry.Embedding, embeddingCopy, entry.Embedding.Length);

                snapshot.Entries.Add(new VectorEntry
                {
                    MemoryId = entry.MemoryId,
                    NpcId = entry.NpcId,
                    MemoryType = entry.MemoryType,
                    Embedding = embeddingCopy,
                    SequenceNumber = entry.SequenceNumber
                });
            }

            return snapshot;
        }

        /// <inheritdoc />
        public void RestoreFromSnapshot(VectorStoreSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            if (snapshot.EmbeddingDimension != _embeddingDimension)
                throw new ArgumentException($"Snapshot embedding dimension mismatch: expected {_embeddingDimension}, got {snapshot.EmbeddingDimension}", nameof(snapshot));

            _vectors.Clear();

            foreach (var entry in snapshot.Entries)
            {
                if (entry.Embedding.Length != _embeddingDimension)
                {
                    throw new ArgumentException($"Entry '{entry.MemoryId}' embedding dimension mismatch: expected {_embeddingDimension}, got {entry.Embedding.Length}", nameof(snapshot));
                }

                // Copy the embedding
                var embeddingCopy = new float[entry.Embedding.Length];
                Array.Copy(entry.Embedding, embeddingCopy, entry.Embedding.Length);

                _vectors[entry.MemoryId] = new StoredVectorEntry(
                    entry.MemoryId,
                    entry.NpcId,
                    entry.MemoryType,
                    embeddingCopy,
                    entry.SequenceNumber);
            }
        }

        /// <summary>
        /// Calculates the cosine similarity between two vectors.
        /// Returns a value between -1 and 1, where 1 is identical direction.
        /// </summary>
        /// <remarks>
        /// Handles edge cases:
        /// - Zero-magnitude vectors return 0.0
        /// - Uses double precision internally to avoid floating-point accumulation errors
        /// </remarks>
        private static float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have the same length");

            // Use double precision for accumulation to avoid floating-point errors
            double dotProduct = 0.0;
            double magnitudeA = 0.0;
            double magnitudeB = 0.0;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                magnitudeA += a[i] * a[i];
                magnitudeB += b[i] * b[i];
            }

            // Handle zero-magnitude vectors
            if (magnitudeA < double.Epsilon || magnitudeB < double.Epsilon)
            {
                return 0.0f;
            }

            double similarity = dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));

            // Clamp to [-1, 1] to handle floating-point rounding errors
            similarity = Math.Max(-1.0, Math.Min(1.0, similarity));

            return (float)similarity;
        }

        /// <summary>
        /// Internal storage entry for vectors.
        /// </summary>
        private sealed class StoredVectorEntry
        {
            public string MemoryId { get; }
            public string? NpcId { get; }
            public MemoryVectorType MemoryType { get; }
            public float[] Embedding { get; }
            public long SequenceNumber { get; }

            public StoredVectorEntry(string memoryId, string? npcId, MemoryVectorType memoryType, float[] embedding, long sequenceNumber)
            {
                MemoryId = memoryId;
                NpcId = npcId;
                MemoryType = memoryType;
                Embedding = embedding;
                SequenceNumber = sequenceNumber;
            }
        }
    }
}
