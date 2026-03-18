using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core.Retrieval;
using LlamaBrain.Persistence;

namespace LlamaBrain.Tests.Retrieval
{
    /// <summary>
    /// Tests for VectorStoreBinarySerializer and persistence.
    /// </summary>
    public class VectorStorePersistenceTests
    {
        private const int TestDimension = 4;

        #region Binary Serializer Write/Read Tests

        [Test]
        public void Write_CreatesValidBinaryStream()
        {
            var snapshot = CreateTestSnapshot();
            using var stream = new MemoryStream();

            VectorStoreBinarySerializer.Write(stream, snapshot);

            Assert.That(stream.Length, Is.GreaterThan(VectorStoreBinarySerializer.HeaderSize));
        }

        [Test]
        public void ReadWrite_RoundTrips()
        {
            var original = CreateTestSnapshot();
            using var stream = new MemoryStream();

            VectorStoreBinarySerializer.Write(stream, original);
            stream.Position = 0;
            var restored = VectorStoreBinarySerializer.Read(stream);

            Assert.That(restored.EmbeddingDimension, Is.EqualTo(original.EmbeddingDimension));
            Assert.That(restored.Entries.Count, Is.EqualTo(original.Entries.Count));
        }

        [Test]
        public void ReadWrite_PreservesEntryData()
        {
            var original = new VectorStoreSnapshot
            {
                EmbeddingDimension = TestDimension,
                Entries = new List<VectorEntry>
                {
                    new VectorEntry
                    {
                        MemoryId = "test-mem-1",
                        NpcId = "npc-wizard",
                        MemoryType = MemoryVectorType.Episodic,
                        Embedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f },
                        SequenceNumber = 42
                    }
                }
            };

            using var stream = new MemoryStream();
            VectorStoreBinarySerializer.Write(stream, original);
            stream.Position = 0;
            var restored = VectorStoreBinarySerializer.Read(stream);

            var entry = restored.Entries[0];
            Assert.That(entry.MemoryId, Is.EqualTo("test-mem-1"));
            Assert.That(entry.NpcId, Is.EqualTo("npc-wizard"));
            Assert.That(entry.MemoryType, Is.EqualTo(MemoryVectorType.Episodic));
            Assert.That(entry.SequenceNumber, Is.EqualTo(42));
            Assert.That(entry.Embedding, Is.EqualTo(new float[] { 0.1f, 0.2f, 0.3f, 0.4f }));
        }

        [Test]
        public void ReadWrite_PreservesNullNpcId()
        {
            var original = new VectorStoreSnapshot
            {
                EmbeddingDimension = TestDimension,
                Entries = new List<VectorEntry>
                {
                    new VectorEntry
                    {
                        MemoryId = "shared-mem",
                        NpcId = null,
                        MemoryType = MemoryVectorType.CanonicalFact,
                        Embedding = new float[] { 1, 0, 0, 0 },
                        SequenceNumber = 1
                    }
                }
            };

            using var stream = new MemoryStream();
            VectorStoreBinarySerializer.Write(stream, original);
            stream.Position = 0;
            var restored = VectorStoreBinarySerializer.Read(stream);

            Assert.That(restored.Entries[0].NpcId, Is.Null);
        }

        [Test]
        public void ReadWrite_PreservesAllMemoryTypes()
        {
            var original = new VectorStoreSnapshot
            {
                EmbeddingDimension = TestDimension,
                Entries = new List<VectorEntry>
                {
                    new VectorEntry { MemoryId = "ep", NpcId = "npc", MemoryType = MemoryVectorType.Episodic, Embedding = new float[] { 1, 0, 0, 0 }, SequenceNumber = 1 },
                    new VectorEntry { MemoryId = "bl", NpcId = "npc", MemoryType = MemoryVectorType.Belief, Embedding = new float[] { 0, 1, 0, 0 }, SequenceNumber = 2 },
                    new VectorEntry { MemoryId = "cf", NpcId = null, MemoryType = MemoryVectorType.CanonicalFact, Embedding = new float[] { 0, 0, 1, 0 }, SequenceNumber = 3 },
                    new VectorEntry { MemoryId = "ws", NpcId = null, MemoryType = MemoryVectorType.WorldState, Embedding = new float[] { 0, 0, 0, 1 }, SequenceNumber = 4 }
                }
            };

            using var stream = new MemoryStream();
            VectorStoreBinarySerializer.Write(stream, original);
            stream.Position = 0;
            var restored = VectorStoreBinarySerializer.Read(stream);

            Assert.That(restored.Entries.Select(e => e.MemoryType), Is.EquivalentTo(new[]
            {
                MemoryVectorType.Episodic,
                MemoryVectorType.Belief,
                MemoryVectorType.CanonicalFact,
                MemoryVectorType.WorldState
            }));
        }

        [Test]
        public void ReadWrite_PreservesUnicodeStrings()
        {
            var original = new VectorStoreSnapshot
            {
                EmbeddingDimension = TestDimension,
                Entries = new List<VectorEntry>
                {
                    new VectorEntry
                    {
                        MemoryId = "unicode-test-日本語",
                        NpcId = "npc-ñoño-🎮",
                        MemoryType = MemoryVectorType.Episodic,
                        Embedding = new float[] { 1, 0, 0, 0 },
                        SequenceNumber = 1
                    }
                }
            };

            using var stream = new MemoryStream();
            VectorStoreBinarySerializer.Write(stream, original);
            stream.Position = 0;
            var restored = VectorStoreBinarySerializer.Read(stream);

            Assert.That(restored.Entries[0].MemoryId, Is.EqualTo("unicode-test-日本語"));
            Assert.That(restored.Entries[0].NpcId, Is.EqualTo("npc-ñoño-🎮"));
        }

        [Test]
        public void ReadWrite_LargeEmbedding()
        {
            var dimension = 384; // Typical embedding size
            var embedding = new float[dimension];
            for (int i = 0; i < dimension; i++)
            {
                embedding[i] = (float)Math.Sin(i * 0.1);
            }

            var original = new VectorStoreSnapshot
            {
                EmbeddingDimension = dimension,
                Entries = new List<VectorEntry>
                {
                    new VectorEntry
                    {
                        MemoryId = "large-emb",
                        NpcId = "npc",
                        MemoryType = MemoryVectorType.Episodic,
                        Embedding = embedding,
                        SequenceNumber = 1
                    }
                }
            };

            using var stream = new MemoryStream();
            VectorStoreBinarySerializer.Write(stream, original);
            stream.Position = 0;
            var restored = VectorStoreBinarySerializer.Read(stream);

            Assert.That(restored.Entries[0].Embedding.Length, Is.EqualTo(dimension));
            for (int i = 0; i < dimension; i++)
            {
                Assert.That(restored.Entries[0].Embedding[i], Is.EqualTo(embedding[i]).Within(0.00001f));
            }
        }

        [Test]
        public void ReadWrite_ManyEntries()
        {
            var entries = new List<VectorEntry>();
            for (int i = 0; i < 1000; i++)
            {
                entries.Add(new VectorEntry
                {
                    MemoryId = $"mem-{i:D5}",
                    NpcId = i % 2 == 0 ? "npc-even" : null,
                    MemoryType = (MemoryVectorType)(i % 4),
                    Embedding = new float[] { i * 0.001f, 0, 0, 0 },
                    SequenceNumber = i
                });
            }

            var original = new VectorStoreSnapshot
            {
                EmbeddingDimension = TestDimension,
                Entries = entries
            };

            using var stream = new MemoryStream();
            VectorStoreBinarySerializer.Write(stream, original);
            stream.Position = 0;
            var restored = VectorStoreBinarySerializer.Read(stream);

            Assert.That(restored.Entries.Count, Is.EqualTo(1000));
            Assert.That(restored.Entries[500].MemoryId, Is.EqualTo("mem-00500"));
        }

        #endregion

        #region File Operations Tests

        [Test]
        public void WriteToFile_ReadFromFile_RoundTrips()
        {
            var tempPath = Path.GetTempFileName();
            try
            {
                var original = CreateTestSnapshot();

                VectorStoreBinarySerializer.WriteToFile(tempPath, original);
                var restored = VectorStoreBinarySerializer.ReadFromFile(tempPath);

                Assert.That(restored.EmbeddingDimension, Is.EqualTo(original.EmbeddingDimension));
                Assert.That(restored.Entries.Count, Is.EqualTo(original.Entries.Count));
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [Test]
        public void WriteToFile_WithNullPath_Throws()
        {
            var snapshot = CreateTestSnapshot();
            Assert.Throws<ArgumentNullException>(() =>
                VectorStoreBinarySerializer.WriteToFile(null!, snapshot));
        }

        [Test]
        public void ReadFromFile_WithNullPath_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                VectorStoreBinarySerializer.ReadFromFile(null!));
        }

        #endregion

        #region Header Validation Tests

        [Test]
        public void ValidateHeader_ValidFile_ReturnsValidInfo()
        {
            var snapshot = CreateTestSnapshot();
            using var stream = new MemoryStream();
            VectorStoreBinarySerializer.Write(stream, snapshot);
            stream.Position = 0;

            var info = VectorStoreBinarySerializer.ValidateHeader(stream);

            Assert.That(info.IsValid, Is.True);
            Assert.That(info.ErrorMessage, Is.Null);
            Assert.That(info.EmbeddingDimension, Is.EqualTo(TestDimension));
            Assert.That(info.VectorCount, Is.EqualTo(2));
            Assert.That(info.Version, Is.EqualTo(VectorStoreBinarySerializer.CurrentVersion));
        }

        [Test]
        public void ValidateHeader_InvalidMagic_ReturnsInvalid()
        {
            using var stream = new MemoryStream();
            stream.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 0, 16);
            stream.Position = 0;

            var info = VectorStoreBinarySerializer.ValidateHeader(stream);

            Assert.That(info.IsValid, Is.False);
            Assert.That(info.ErrorMessage, Does.Contain("magic"));
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Write_WithNullStream_Throws()
        {
            var snapshot = CreateTestSnapshot();
            Assert.Throws<ArgumentNullException>(() =>
                VectorStoreBinarySerializer.Write(null!, snapshot));
        }

        [Test]
        public void Write_WithNullSnapshot_Throws()
        {
            using var stream = new MemoryStream();
            Assert.Throws<ArgumentNullException>(() =>
                VectorStoreBinarySerializer.Write(stream, null!));
        }

        [Test]
        public void Read_WithNullStream_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                VectorStoreBinarySerializer.Read(null!));
        }

        [Test]
        public void Read_InvalidMagic_Throws()
        {
            using var stream = new MemoryStream();
            stream.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 0, 16);
            stream.Position = 0;

            Assert.Throws<InvalidDataException>(() => VectorStoreBinarySerializer.Read(stream));
        }

        [Test]
        public void Write_DimensionMismatch_Throws()
        {
            var snapshot = new VectorStoreSnapshot
            {
                EmbeddingDimension = 4,
                Entries = new List<VectorEntry>
                {
                    new VectorEntry
                    {
                        MemoryId = "bad-dim",
                        NpcId = "npc",
                        MemoryType = MemoryVectorType.Episodic,
                        Embedding = new float[] { 1, 0 }, // Wrong dimension
                        SequenceNumber = 1
                    }
                }
            };

            using var stream = new MemoryStream();
            Assert.Throws<InvalidDataException>(() =>
                VectorStoreBinarySerializer.Write(stream, snapshot));
        }

        #endregion

        #region File Size Estimation Tests

        [Test]
        public void EstimateFileSize_ReturnsReasonableEstimate()
        {
            var estimated = VectorStoreBinarySerializer.EstimateFileSize(100, 384);

            // Should be header + per-entry overhead + embeddings
            // ~16 + 100 * (1 + 8 + 1 + 4 + 1 + 8 + 384*4) = ~16 + 100 * 1559 = ~156k
            Assert.That(estimated, Is.GreaterThan(100000));
            Assert.That(estimated, Is.LessThan(200000));
        }

        [Test]
        public void EstimateFileSize_MatchesActualSize()
        {
            var snapshot = CreateTestSnapshot();
            using var stream = new MemoryStream();
            VectorStoreBinarySerializer.Write(stream, snapshot);

            var estimated = VectorStoreBinarySerializer.EstimateFileSize(
                snapshot.Entries.Count, snapshot.EmbeddingDimension);

            // Should be within 50% of actual size
            Assert.That(estimated, Is.InRange(stream.Length * 0.5, stream.Length * 1.5));
        }

        #endregion

        #region InMemoryVectorStore Integration Tests

        [Test]
        public void VectorStore_SnapshotRoundTrip_PreservesSearchBehavior()
        {
            var store = new InMemoryVectorStore(TestDimension);
            store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            store.Upsert("mem-2", "npc-1", MemoryVectorType.Episodic, new float[] { 0, 1, 0, 0 }, 2);
            store.Upsert("shared", null, MemoryVectorType.CanonicalFact, new float[] { 0.5f, 0.5f, 0, 0 }, 3);

            // Create snapshot and serialize
            var snapshot = store.CreateSnapshot();
            using var stream = new MemoryStream();
            VectorStoreBinarySerializer.Write(stream, snapshot);
            stream.Position = 0;

            // Deserialize and restore
            var restoredSnapshot = VectorStoreBinarySerializer.Read(stream);
            var restoredStore = new InMemoryVectorStore(TestDimension);
            restoredStore.RestoreFromSnapshot(restoredSnapshot);

            // Search should work the same
            var query = new float[] { 1, 0, 0, 0 };
            var originalResults = store.FindSimilar(query, 10, "npc-1");
            var restoredResults = restoredStore.FindSimilar(query, 10, "npc-1");

            Assert.That(restoredResults.Select(r => r.MemoryId),
                Is.EqualTo(originalResults.Select(r => r.MemoryId)));
            Assert.That(restoredResults.Select(r => r.Similarity),
                Is.EqualTo(originalResults.Select(r => r.Similarity)));
        }

        #endregion

        #region Determinism Tests

        [Test]
        public void Write_ProducesIdenticalBytes_ForSameInput()
        {
            var snapshot = CreateTestSnapshot();

            using var stream1 = new MemoryStream();
            using var stream2 = new MemoryStream();

            VectorStoreBinarySerializer.Write(stream1, snapshot);
            VectorStoreBinarySerializer.Write(stream2, snapshot);

            Assert.That(stream1.ToArray(), Is.EqualTo(stream2.ToArray()));
        }

        #endregion

        private VectorStoreSnapshot CreateTestSnapshot()
        {
            return new VectorStoreSnapshot
            {
                EmbeddingDimension = TestDimension,
                Entries = new List<VectorEntry>
                {
                    new VectorEntry
                    {
                        MemoryId = "mem-1",
                        NpcId = "npc-1",
                        MemoryType = MemoryVectorType.Episodic,
                        Embedding = new float[] { 1, 0, 0, 0 },
                        SequenceNumber = 1
                    },
                    new VectorEntry
                    {
                        MemoryId = "mem-2",
                        NpcId = null,
                        MemoryType = MemoryVectorType.CanonicalFact,
                        Embedding = new float[] { 0, 1, 0, 0 },
                        SequenceNumber = 2
                    }
                }
            };
        }
    }
}
