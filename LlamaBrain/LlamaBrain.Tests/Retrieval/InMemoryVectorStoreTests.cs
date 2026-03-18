using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core.Retrieval;

namespace LlamaBrain.Tests.Retrieval
{
    /// <summary>
    /// Tests for InMemoryVectorStore.
    /// </summary>
    public class InMemoryVectorStoreTests
    {
        private const int TestDimension = 4;
        private InMemoryVectorStore _store = null!;

        [SetUp]
        public void SetUp()
        {
            _store = new InMemoryVectorStore(TestDimension);
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithValidDimension_CreatesStore()
        {
            var store = new InMemoryVectorStore(128);
            Assert.That(store.GetStatistics().EmbeddingDimension, Is.EqualTo(128));
        }

        [Test]
        public void Constructor_WithZeroDimension_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new InMemoryVectorStore(0));
        }

        [Test]
        public void Constructor_WithNegativeDimension_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new InMemoryVectorStore(-1));
        }

        #endregion

        #region Upsert Tests

        [Test]
        public void Upsert_AddsNewEntry()
        {
            var embedding = new float[] { 1, 0, 0, 0 };
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, embedding, 1);

            var stats = _store.GetStatistics();
            Assert.That(stats.TotalVectors, Is.EqualTo(1));
            Assert.That(stats.EpisodicVectors, Is.EqualTo(1));
        }

        [Test]
        public void Upsert_UpdatesExistingEntry()
        {
            var embedding1 = new float[] { 1, 0, 0, 0 };
            var embedding2 = new float[] { 0, 1, 0, 0 };

            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, embedding1, 1);
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Belief, embedding2, 2);

            var stats = _store.GetStatistics();
            Assert.That(stats.TotalVectors, Is.EqualTo(1));
            Assert.That(stats.EpisodicVectors, Is.EqualTo(0));
            Assert.That(stats.BeliefVectors, Is.EqualTo(1));
        }

        [Test]
        public void Upsert_WithNullMemoryId_Throws()
        {
            var embedding = new float[] { 1, 0, 0, 0 };
            Assert.Throws<ArgumentNullException>(() =>
                _store.Upsert(null!, "npc-1", MemoryVectorType.Episodic, embedding, 1));
        }

        [Test]
        public void Upsert_WithNullEmbedding_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, null!, 1));
        }

        [Test]
        public void Upsert_WithWrongDimension_Throws()
        {
            var wrongDimension = new float[] { 1, 0 };
            Assert.Throws<ArgumentException>(() =>
                _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, wrongDimension, 1));
        }

        [Test]
        public void Upsert_WithNullNpcId_AllowsSharedEntry()
        {
            var embedding = new float[] { 1, 0, 0, 0 };
            _store.Upsert("shared-1", null, MemoryVectorType.CanonicalFact, embedding, 1);

            var stats = _store.GetStatistics();
            Assert.That(stats.TotalVectors, Is.EqualTo(1));
            Assert.That(stats.CanonicalFactVectors, Is.EqualTo(1));
        }

        [Test]
        public void Upsert_CopiesEmbedding_PreventsMutation()
        {
            var embedding = new float[] { 1, 0, 0, 0 };
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, embedding, 1);

            // Mutate original
            embedding[0] = 999;

            // Search should still work with original value
            var query = new float[] { 1, 0, 0, 0 };
            var results = _store.FindSimilar(query, 1, "npc-1");
            Assert.That(results[0].Similarity, Is.EqualTo(1.0f).Within(0.001f));
        }

        #endregion

        #region Remove Tests

        [Test]
        public void Remove_ExistingEntry_ReturnsTrue()
        {
            var embedding = new float[] { 1, 0, 0, 0 };
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, embedding, 1);

            var result = _store.Remove("mem-1");
            Assert.That(result, Is.True);
            Assert.That(_store.GetStatistics().TotalVectors, Is.EqualTo(0));
        }

        [Test]
        public void Remove_NonExistentEntry_ReturnsFalse()
        {
            var result = _store.Remove("non-existent");
            Assert.That(result, Is.False);
        }

        #endregion

        #region FindSimilar Tests

        [Test]
        public void FindSimilar_ReturnsExactMatch()
        {
            var embedding = new float[] { 1, 0, 0, 0 };
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, embedding, 1);

            var results = _store.FindSimilar(embedding, 1, "npc-1");

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].MemoryId, Is.EqualTo("mem-1"));
            Assert.That(results[0].Similarity, Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void FindSimilar_OrdersBySimilarityDescending()
        {
            // Unit vectors at different angles
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("mem-2", "npc-1", MemoryVectorType.Episodic, new float[] { 0.707f, 0.707f, 0, 0 }, 2);
            _store.Upsert("mem-3", "npc-1", MemoryVectorType.Episodic, new float[] { 0, 1, 0, 0 }, 3);

            var query = new float[] { 1, 0, 0, 0 };
            var results = _store.FindSimilar(query, 3, "npc-1");

            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[0].MemoryId, Is.EqualTo("mem-1")); // Exact match
            Assert.That(results[1].MemoryId, Is.EqualTo("mem-2")); // 45 degrees
            Assert.That(results[2].MemoryId, Is.EqualTo("mem-3")); // 90 degrees
        }

        [Test]
        public void FindSimilar_RespectsKLimit()
        {
            for (int i = 0; i < 10; i++)
            {
                var embedding = new float[] { 1, 0, 0, 0 };
                _store.Upsert($"mem-{i}", "npc-1", MemoryVectorType.Episodic, embedding, i);
            }

            var query = new float[] { 1, 0, 0, 0 };
            var results = _store.FindSimilar(query, 3, "npc-1");

            Assert.That(results.Count, Is.EqualTo(3));
        }

        [Test]
        public void FindSimilar_FiltersMinSimilarity()
        {
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("mem-2", "npc-1", MemoryVectorType.Episodic, new float[] { 0, 1, 0, 0 }, 2);

            var query = new float[] { 1, 0, 0, 0 };
            var results = _store.FindSimilar(query, 10, "npc-1", minSimilarity: 0.5f);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].MemoryId, Is.EqualTo("mem-1"));
        }

        [Test]
        public void FindSimilar_FiltersByMemoryType()
        {
            _store.Upsert("episodic-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("belief-1", "npc-1", MemoryVectorType.Belief, new float[] { 1, 0, 0, 0 }, 2);

            var query = new float[] { 1, 0, 0, 0 };
            var results = _store.FindSimilar(query, 10, "npc-1", MemoryVectorType.Episodic);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].MemoryId, Is.EqualTo("episodic-1"));
        }

        [Test]
        public void FindSimilar_IncludesSharedEntries_WhenNpcIdProvided()
        {
            // NPC-specific entry
            _store.Upsert("npc-specific", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            // Shared entry (null NpcId)
            _store.Upsert("shared", null, MemoryVectorType.CanonicalFact, new float[] { 1, 0, 0, 0 }, 2);

            var query = new float[] { 1, 0, 0, 0 };
            var results = _store.FindSimilar(query, 10, "npc-1");

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.Select(r => r.MemoryId), Contains.Item("npc-specific"));
            Assert.That(results.Select(r => r.MemoryId), Contains.Item("shared"));
        }

        [Test]
        public void FindSimilar_ExcludesOtherNpcEntries()
        {
            _store.Upsert("npc1-mem", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("npc2-mem", "npc-2", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 2);

            var query = new float[] { 1, 0, 0, 0 };
            var results = _store.FindSimilar(query, 10, "npc-1");

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].MemoryId, Is.EqualTo("npc1-mem"));
        }

        [Test]
        public void FindSimilar_WithNullQueryEmbedding_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _store.FindSimilar(null!, 1, "npc-1"));
        }

        [Test]
        public void FindSimilar_WithWrongDimensionQuery_Throws()
        {
            var wrongDimension = new float[] { 1, 0 };
            Assert.Throws<ArgumentException>(() =>
                _store.FindSimilar(wrongDimension, 1, "npc-1"));
        }

        [Test]
        public void FindSimilar_WithZeroK_Throws()
        {
            var query = new float[] { 1, 0, 0, 0 };
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _store.FindSimilar(query, 0, "npc-1"));
        }

        #endregion

        #region Deterministic Ordering Tests

        [Test]
        public void FindSimilar_WithIdenticalSimilarity_OrdersBySequenceNumber()
        {
            // All same embedding = same similarity
            _store.Upsert("mem-3", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 3);
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("mem-2", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 2);

            var query = new float[] { 1, 0, 0, 0 };
            var results = _store.FindSimilar(query, 3, "npc-1");

            // Should be ordered by sequence number ascending (older first)
            Assert.That(results[0].MemoryId, Is.EqualTo("mem-1"));
            Assert.That(results[1].MemoryId, Is.EqualTo("mem-2"));
            Assert.That(results[2].MemoryId, Is.EqualTo("mem-3"));
        }

        [Test]
        public void FindSimilar_WithIdenticalSimilarityAndSequence_OrdersByMemoryIdOrdinal()
        {
            // Same embedding and sequence number
            _store.Upsert("mem-c", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("mem-a", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("mem-b", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);

            var query = new float[] { 1, 0, 0, 0 };
            var results = _store.FindSimilar(query, 3, "npc-1");

            // Should be ordered by memoryId ordinal ascending
            Assert.That(results[0].MemoryId, Is.EqualTo("mem-a"));
            Assert.That(results[1].MemoryId, Is.EqualTo("mem-b"));
            Assert.That(results[2].MemoryId, Is.EqualTo("mem-c"));
        }

        [Test]
        public void FindSimilar_ProducesDeterministicResults_AcrossMultipleCalls()
        {
            // Setup varied data
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("mem-2", "npc-1", MemoryVectorType.Episodic, new float[] { 0.9f, 0.1f, 0, 0 }, 2);
            _store.Upsert("mem-3", "npc-1", MemoryVectorType.Episodic, new float[] { 0.8f, 0.2f, 0, 0 }, 3);
            _store.Upsert("shared-1", null, MemoryVectorType.CanonicalFact, new float[] { 1, 0, 0, 0 }, 4);

            var query = new float[] { 1, 0, 0, 0 };

            // Execute multiple times
            var results1 = _store.FindSimilar(query, 10, "npc-1");
            var results2 = _store.FindSimilar(query, 10, "npc-1");
            var results3 = _store.FindSimilar(query, 10, "npc-1");

            // Results must be identical
            Assert.That(results1.Select(r => r.MemoryId), Is.EqualTo(results2.Select(r => r.MemoryId)));
            Assert.That(results2.Select(r => r.MemoryId), Is.EqualTo(results3.Select(r => r.MemoryId)));
        }

        #endregion

        #region Cosine Similarity Edge Cases

        [Test]
        public void FindSimilar_WithZeroVector_ReturnsZeroSimilarity()
        {
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);

            var zeroQuery = new float[] { 0, 0, 0, 0 };
            var results = _store.FindSimilar(zeroQuery, 1, "npc-1", minSimilarity: -1f);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Similarity, Is.EqualTo(0f));
        }

        [Test]
        public void FindSimilar_WithOppositeVectors_ReturnsNegativeSimilarity()
        {
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);

            var oppositeQuery = new float[] { -1, 0, 0, 0 };
            var results = _store.FindSimilar(oppositeQuery, 1, "npc-1", minSimilarity: -1f);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Similarity, Is.EqualTo(-1.0f).Within(0.001f));
        }

        [Test]
        public void FindSimilar_WithOrthogonalVectors_ReturnsZeroSimilarity()
        {
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);

            var orthogonalQuery = new float[] { 0, 1, 0, 0 };
            var results = _store.FindSimilar(orthogonalQuery, 1, "npc-1", minSimilarity: -1f);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Similarity, Is.EqualTo(0f).Within(0.001f));
        }

        #endregion

        #region Snapshot Tests

        [Test]
        public void CreateSnapshot_ReturnsCorrectData()
        {
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("mem-2", null, MemoryVectorType.CanonicalFact, new float[] { 0, 1, 0, 0 }, 2);

            var snapshot = _store.CreateSnapshot();

            Assert.That(snapshot.EmbeddingDimension, Is.EqualTo(TestDimension));
            Assert.That(snapshot.Entries.Count, Is.EqualTo(2));
        }

        [Test]
        public void CreateSnapshot_OrdersBySequenceThenId()
        {
            _store.Upsert("mem-c", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("mem-a", "npc-1", MemoryVectorType.Episodic, new float[] { 0, 1, 0, 0 }, 1);
            _store.Upsert("mem-b", "npc-1", MemoryVectorType.Episodic, new float[] { 0, 0, 1, 0 }, 2);

            var snapshot = _store.CreateSnapshot();

            // Same sequence (1): mem-a before mem-c (ordinal)
            // Different sequence: mem-b (seq=2) last
            Assert.That(snapshot.Entries[0].MemoryId, Is.EqualTo("mem-a"));
            Assert.That(snapshot.Entries[1].MemoryId, Is.EqualTo("mem-c"));
            Assert.That(snapshot.Entries[2].MemoryId, Is.EqualTo("mem-b"));
        }

        [Test]
        public void RestoreFromSnapshot_RestoresData()
        {
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            var snapshot = _store.CreateSnapshot();

            var newStore = new InMemoryVectorStore(TestDimension);
            newStore.RestoreFromSnapshot(snapshot);

            var stats = newStore.GetStatistics();
            Assert.That(stats.TotalVectors, Is.EqualTo(1));

            var query = new float[] { 1, 0, 0, 0 };
            var results = newStore.FindSimilar(query, 1, "npc-1");
            Assert.That(results[0].MemoryId, Is.EqualTo("mem-1"));
        }

        [Test]
        public void RestoreFromSnapshot_WithMismatchedDimension_Throws()
        {
            var snapshot = new VectorStoreSnapshot
            {
                EmbeddingDimension = 128, // Different from TestDimension
                Entries = new List<VectorEntry>()
            };

            Assert.Throws<ArgumentException>(() => _store.RestoreFromSnapshot(snapshot));
        }

        [Test]
        public void RestoreFromSnapshot_ClearsExistingData()
        {
            _store.Upsert("old-mem", "npc-1", MemoryVectorType.Episodic, new float[] { 0, 0, 1, 0 }, 1);

            var snapshot = new VectorStoreSnapshot
            {
                EmbeddingDimension = TestDimension,
                Entries = new List<VectorEntry>
                {
                    new VectorEntry { MemoryId = "new-mem", NpcId = "npc-1", MemoryType = MemoryVectorType.Belief, Embedding = new float[] { 1, 0, 0, 0 }, SequenceNumber = 1 }
                }
            };

            _store.RestoreFromSnapshot(snapshot);

            var stats = _store.GetStatistics();
            Assert.That(stats.TotalVectors, Is.EqualTo(1));
            Assert.That(stats.BeliefVectors, Is.EqualTo(1));
            Assert.That(stats.EpisodicVectors, Is.EqualTo(0));
        }

        #endregion

        #region Statistics Tests

        [Test]
        public void GetStatistics_CountsAllTypes()
        {
            _store.Upsert("ep-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("ep-2", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 2);
            _store.Upsert("belief-1", "npc-1", MemoryVectorType.Belief, new float[] { 0, 1, 0, 0 }, 3);
            _store.Upsert("canon-1", null, MemoryVectorType.CanonicalFact, new float[] { 0, 0, 1, 0 }, 4);
            _store.Upsert("world-1", null, MemoryVectorType.WorldState, new float[] { 0, 0, 0, 1 }, 5);

            var stats = _store.GetStatistics();

            Assert.That(stats.TotalVectors, Is.EqualTo(5));
            Assert.That(stats.EpisodicVectors, Is.EqualTo(2));
            Assert.That(stats.BeliefVectors, Is.EqualTo(1));
            Assert.That(stats.CanonicalFactVectors, Is.EqualTo(1));
            Assert.That(stats.WorldStateVectors, Is.EqualTo(1));
            Assert.That(stats.EmbeddingDimension, Is.EqualTo(TestDimension));
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_RemovesAllEntries()
        {
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("mem-2", "npc-1", MemoryVectorType.Belief, new float[] { 0, 1, 0, 0 }, 2);

            _store.Clear();

            Assert.That(_store.GetStatistics().TotalVectors, Is.EqualTo(0));
        }

        #endregion
    }
}
