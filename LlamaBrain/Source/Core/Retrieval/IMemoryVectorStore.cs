using System;
using System.Collections.Generic;

namespace LlamaBrain.Core.Retrieval
{
    /// <summary>
    /// Types of memory entries that can be indexed in the vector store.
    /// </summary>
    public enum MemoryVectorType
    {
        /// <summary>Episodic memory (conversation/event history).</summary>
        Episodic = 0,

        /// <summary>Belief memory (NPC opinions/relationships).</summary>
        Belief = 1,

        /// <summary>Canonical fact (immutable world truth).</summary>
        CanonicalFact = 2,

        /// <summary>World state (mutable game state).</summary>
        WorldState = 3
    }

    /// <summary>
    /// Result of a vector similarity search.
    /// </summary>
    public sealed class VectorSearchResult
    {
        /// <summary>The unique ID of the matched memory entry.</summary>
        public string MemoryId { get; }

        /// <summary>The NPC that owns this memory, or null for shared memories.</summary>
        public string? NpcId { get; }

        /// <summary>The cosine similarity score (-1 to 1).</summary>
        public float Similarity { get; }

        /// <summary>The sequence number for deterministic ordering.</summary>
        public long SequenceNumber { get; }

        /// <summary>The type of memory entry.</summary>
        public MemoryVectorType MemoryType { get; }

        public VectorSearchResult(string memoryId, string? npcId, float similarity, long sequenceNumber, MemoryVectorType memoryType)
        {
            if (string.IsNullOrEmpty(memoryId))
                throw new ArgumentNullException(nameof(memoryId), "Memory ID cannot be null or empty.");
            if (npcId != null && string.IsNullOrWhiteSpace(npcId))
                throw new ArgumentException("NPC ID cannot be empty or whitespace when provided.", nameof(npcId));
            if (similarity < -1f || similarity > 1f)
                throw new ArgumentOutOfRangeException(nameof(similarity), similarity, "Similarity must be between -1 and 1.");
            if (sequenceNumber < 0)
                throw new ArgumentOutOfRangeException(nameof(sequenceNumber), sequenceNumber, "Sequence number cannot be negative.");

            MemoryId = memoryId;
            NpcId = npcId;
            Similarity = similarity;
            SequenceNumber = sequenceNumber;
            MemoryType = memoryType;
        }
    }

    /// <summary>
    /// Statistics about the vector store contents.
    /// </summary>
    public sealed class VectorStoreStatistics
    {
        /// <summary>Total number of vectors stored.</summary>
        public int TotalVectors { get; }

        /// <summary>Number of episodic memory vectors.</summary>
        public int EpisodicVectors { get; }

        /// <summary>Number of belief memory vectors.</summary>
        public int BeliefVectors { get; }

        /// <summary>Number of canonical fact vectors.</summary>
        public int CanonicalFactVectors { get; }

        /// <summary>Number of world state vectors.</summary>
        public int WorldStateVectors { get; }

        /// <summary>The dimension of stored embeddings.</summary>
        public int EmbeddingDimension { get; }

        public VectorStoreStatistics(int totalVectors, int episodicVectors, int beliefVectors, int canonicalFactVectors, int worldStateVectors, int embeddingDimension)
        {
            if (totalVectors < 0)
                throw new ArgumentOutOfRangeException(nameof(totalVectors), totalVectors, "Total vectors cannot be negative.");
            if (episodicVectors < 0)
                throw new ArgumentOutOfRangeException(nameof(episodicVectors), episodicVectors, "Episodic vectors cannot be negative.");
            if (beliefVectors < 0)
                throw new ArgumentOutOfRangeException(nameof(beliefVectors), beliefVectors, "Belief vectors cannot be negative.");
            if (canonicalFactVectors < 0)
                throw new ArgumentOutOfRangeException(nameof(canonicalFactVectors), canonicalFactVectors, "Canonical fact vectors cannot be negative.");
            if (worldStateVectors < 0)
                throw new ArgumentOutOfRangeException(nameof(worldStateVectors), worldStateVectors, "World state vectors cannot be negative.");
            if (embeddingDimension < 0)
                throw new ArgumentOutOfRangeException(nameof(embeddingDimension), embeddingDimension, "Embedding dimension cannot be negative.");

            TotalVectors = totalVectors;
            EpisodicVectors = episodicVectors;
            BeliefVectors = beliefVectors;
            CanonicalFactVectors = canonicalFactVectors;
            WorldStateVectors = worldStateVectors;
            EmbeddingDimension = embeddingDimension;
        }
    }

    /// <summary>
    /// Snapshot of the vector store for persistence.
    /// </summary>
    public sealed class VectorStoreSnapshot
    {
        /// <summary>
        /// The embedding dimension used by all vectors.
        /// </summary>
        public int EmbeddingDimension { get; set; }

        /// <summary>
        /// All stored vector entries.
        /// </summary>
        public List<VectorEntry> Entries { get; set; } = new List<VectorEntry>();
    }

    /// <summary>
    /// A single vector entry in the store.
    /// </summary>
    public sealed class VectorEntry
    {
        /// <summary>
        /// The unique ID of the memory entry.
        /// </summary>
        public string MemoryId { get; set; } = "";

        /// <summary>
        /// The NPC that owns this memory, or null for shared memories (canonical facts, world state).
        /// </summary>
        public string? NpcId { get; set; }

        /// <summary>
        /// The type of memory entry.
        /// </summary>
        public MemoryVectorType MemoryType { get; set; }

        /// <summary>
        /// The embedding vector.
        /// </summary>
        public float[] Embedding { get; set; } = Array.Empty<float>();

        /// <summary>
        /// The sequence number for deterministic ordering.
        /// </summary>
        public long SequenceNumber { get; set; }
    }

    /// <summary>
    /// Stores and retrieves embedding vectors for memory entries.
    /// Supports incremental updates, NPC-based filtering, and similarity search.
    ///
    /// <para>
    /// The vector store is shared across all NPCs. Entries with NpcId=null
    /// are considered shared (e.g., canonical facts, world state) and are
    /// included in queries for any NPC.
    /// </para>
    /// </summary>
    public interface IMemoryVectorStore
    {
        /// <summary>
        /// Adds or updates an embedding for a memory entry.
        /// </summary>
        /// <param name="memoryId">The unique ID of the memory entry.</param>
        /// <param name="npcId">The NPC that owns this memory, or null for shared memories.</param>
        /// <param name="memoryType">The type of memory (Episodic, Belief, etc.).</param>
        /// <param name="embedding">The embedding vector.</param>
        /// <param name="sequenceNumber">Sequence number for deterministic ordering.</param>
        void Upsert(string memoryId, string? npcId, MemoryVectorType memoryType, float[] embedding, long sequenceNumber);

        /// <summary>
        /// Removes an embedding by memory ID.
        /// </summary>
        /// <param name="memoryId">The memory ID to remove.</param>
        /// <returns>True if the entry was found and removed, false otherwise.</returns>
        bool Remove(string memoryId);

        /// <summary>
        /// Finds the k most similar memories to the query embedding.
        /// Results are returned in deterministic order (similarity desc, then sequenceNumber asc, then memoryId ordinal asc).
        /// </summary>
        /// <param name="queryEmbedding">The query embedding to search for.</param>
        /// <param name="k">Maximum number of results to return.</param>
        /// <param name="npcId">Filter by NPC ID. Entries with matching NpcId OR NpcId=null (shared) are included.</param>
        /// <param name="memoryType">Optional filter by memory type.</param>
        /// <param name="minSimilarity">Minimum similarity threshold (-1 to 1). Entries below this are excluded.</param>
        /// <returns>List of search results in deterministic order.</returns>
        IReadOnlyList<VectorSearchResult> FindSimilar(
            float[] queryEmbedding,
            int k,
            string? npcId,
            MemoryVectorType? memoryType = null,
            float minSimilarity = 0.0f);

        /// <summary>
        /// Gets statistics about the vector store.
        /// </summary>
        /// <returns>Statistics about stored vectors.</returns>
        VectorStoreStatistics GetStatistics();

        /// <summary>
        /// Clears all stored embeddings.
        /// </summary>
        void Clear();

        /// <summary>
        /// Creates a snapshot for persistence.
        /// </summary>
        /// <returns>A snapshot of all stored vectors.</returns>
        VectorStoreSnapshot CreateSnapshot();

        /// <summary>
        /// Restores the vector store from a snapshot.
        /// </summary>
        /// <param name="snapshot">The snapshot to restore from.</param>
        void RestoreFromSnapshot(VectorStoreSnapshot snapshot);
    }
}
