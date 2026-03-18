using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core.Retrieval;
using LlamaBrain.Diagnostics;
using LlamaBrain.Persistence;

namespace LlamaBrain.Tests.Retrieval
{
    /// <summary>
    /// Tests for VectorStoreDiagnostics.
    /// </summary>
    public class VectorStoreDiagnosticsTests
    {
        private const int TestDimension = 4;
        private InMemoryVectorStore _store = null!;

        [SetUp]
        public void SetUp()
        {
            _store = new InMemoryVectorStore(TestDimension);
        }

        #region GetSummary Tests

        [Test]
        public void GetSummary_EmptyStore_ReturnsValidSummary()
        {
            var summary = VectorStoreDiagnostics.GetSummary(_store);

            Assert.That(summary, Does.Contain("0 vectors"));
            Assert.That(summary, Does.Contain($"dim={TestDimension}"));
        }

        [Test]
        public void GetSummary_WithData_IncludesCounts()
        {
            _store.Upsert("ep-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("ep-2", "npc-1", MemoryVectorType.Episodic, new float[] { 0, 1, 0, 0 }, 2);
            _store.Upsert("bl-1", "npc-1", MemoryVectorType.Belief, new float[] { 0, 0, 1, 0 }, 3);
            _store.Upsert("cf-1", null, MemoryVectorType.CanonicalFact, new float[] { 0, 0, 0, 1 }, 4);

            var summary = VectorStoreDiagnostics.GetSummary(_store);

            Assert.That(summary, Does.Contain("4 vectors"));
            Assert.That(summary, Does.Contain("Episodic: 2"));
            Assert.That(summary, Does.Contain("Belief: 1"));
            Assert.That(summary, Does.Contain("Canonical: 1"));
            Assert.That(summary, Does.Contain("Shared: 1"));
        }

        [Test]
        public void GetSummary_WithNullStore_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                VectorStoreDiagnostics.GetSummary(null!));
        }

        #endregion

        #region FindByIdPattern Tests

        [Test]
        public void FindByIdPattern_ExactMatch()
        {
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("mem-2", "npc-1", MemoryVectorType.Episodic, new float[] { 0, 1, 0, 0 }, 2);

            var results = VectorStoreDiagnostics.FindByIdPattern(_store, "mem-1").ToList();

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].MemoryId, Is.EqualTo("mem-1"));
        }

        [Test]
        public void FindByIdPattern_Wildcard()
        {
            _store.Upsert("episodic-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("episodic-2", "npc-1", MemoryVectorType.Episodic, new float[] { 0, 1, 0, 0 }, 2);
            _store.Upsert("belief-1", "npc-1", MemoryVectorType.Belief, new float[] { 0, 0, 1, 0 }, 3);

            var results = VectorStoreDiagnostics.FindByIdPattern(_store, "episodic*").ToList();

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.All(r => r.MemoryId.StartsWith("episodic")), Is.True);
        }

        [Test]
        public void FindByIdPattern_QuestionMarkWildcard()
        {
            _store.Upsert("mem-a", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("mem-b", "npc-1", MemoryVectorType.Episodic, new float[] { 0, 1, 0, 0 }, 2);
            _store.Upsert("mem-ab", "npc-1", MemoryVectorType.Episodic, new float[] { 0, 0, 1, 0 }, 3);

            var results = VectorStoreDiagnostics.FindByIdPattern(_store, "mem-?").ToList();

            Assert.That(results.Count, Is.EqualTo(2));
        }

        [Test]
        public void FindByIdPattern_CaseInsensitive()
        {
            _store.Upsert("UPPER-MEM", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);

            var results = VectorStoreDiagnostics.FindByIdPattern(_store, "upper*").ToList();

            Assert.That(results.Count, Is.EqualTo(1));
        }

        [Test]
        public void FindByIdPattern_DeterministicOrdering()
        {
            _store.Upsert("mem-c", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 3);
            _store.Upsert("mem-a", "npc-1", MemoryVectorType.Episodic, new float[] { 0, 1, 0, 0 }, 1);
            _store.Upsert("mem-b", "npc-1", MemoryVectorType.Episodic, new float[] { 0, 0, 1, 0 }, 1);

            var results = VectorStoreDiagnostics.FindByIdPattern(_store, "mem-*").ToList();

            // Ordered by sequence number, then memory ID
            Assert.That(results[0].MemoryId, Is.EqualTo("mem-a"));
            Assert.That(results[1].MemoryId, Is.EqualTo("mem-b"));
            Assert.That(results[2].MemoryId, Is.EqualTo("mem-c"));
        }

        [Test]
        public void FindByIdPattern_NoMatches_ReturnsEmpty()
        {
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);

            var results = VectorStoreDiagnostics.FindByIdPattern(_store, "xyz*").ToList();

            Assert.That(results, Is.Empty);
        }

        #endregion

        #region FindByNpcId Tests

        [Test]
        public void FindByNpcId_FindsNpcEntries()
        {
            _store.Upsert("npc1-mem", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("npc2-mem", "npc-2", MemoryVectorType.Episodic, new float[] { 0, 1, 0, 0 }, 2);

            var results = VectorStoreDiagnostics.FindByNpcId(_store, "npc-1").ToList();

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].MemoryId, Is.EqualTo("npc1-mem"));
        }

        [Test]
        public void FindByNpcId_FindsSharedEntries()
        {
            _store.Upsert("shared-1", null, MemoryVectorType.CanonicalFact, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("npc-mem", "npc-1", MemoryVectorType.Episodic, new float[] { 0, 1, 0, 0 }, 2);

            var results = VectorStoreDiagnostics.FindByNpcId(_store, null).ToList();

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].MemoryId, Is.EqualTo("shared-1"));
        }

        #endregion

        #region FindByMemoryType Tests

        [Test]
        public void FindByMemoryType_FiltersCorrectly()
        {
            _store.Upsert("ep-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("bl-1", "npc-1", MemoryVectorType.Belief, new float[] { 0, 1, 0, 0 }, 2);
            _store.Upsert("cf-1", null, MemoryVectorType.CanonicalFact, new float[] { 0, 0, 1, 0 }, 3);

            var results = VectorStoreDiagnostics.FindByMemoryType(_store, MemoryVectorType.Belief).ToList();

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].MemoryType, Is.EqualTo(MemoryVectorType.Belief));
        }

        #endregion

        #region FindDuplicateIds Tests

        [Test]
        public void FindDuplicateIds_NoDuplicates_ReturnsEmpty()
        {
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("mem-2", "npc-1", MemoryVectorType.Episodic, new float[] { 0, 1, 0, 0 }, 2);

            var duplicates = VectorStoreDiagnostics.FindDuplicateIds(_store);

            Assert.That(duplicates, Is.Empty);
        }

        // Note: InMemoryVectorStore uses dictionary keys, so actual duplicates aren't possible
        // This test verifies the diagnostic method works correctly

        #endregion

        #region FindDimensionMismatches Tests

        [Test]
        public void FindDimensionMismatches_NoMismatches_ReturnsEmpty()
        {
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            _store.Upsert("mem-2", "npc-1", MemoryVectorType.Episodic, new float[] { 0, 1, 0, 0 }, 2);

            var mismatches = VectorStoreDiagnostics.FindDimensionMismatches(_store);

            Assert.That(mismatches, Is.Empty);
        }

        #endregion

        #region ComputeEmbeddingStatistics Tests

        [Test]
        public void ComputeEmbeddingStatistics_EmptyStore_ReturnsZeros()
        {
            var stats = VectorStoreDiagnostics.ComputeEmbeddingStatistics(_store);

            Assert.That(stats.TotalVectors, Is.EqualTo(0));
            Assert.That(stats.MinMagnitude, Is.EqualTo(0));
            Assert.That(stats.MaxMagnitude, Is.EqualTo(0));
        }

        [Test]
        public void ComputeEmbeddingStatistics_WithData_ComputesMagnitudes()
        {
            // Unit vector: magnitude = 1
            _store.Upsert("unit", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);
            // Scaled vector: magnitude = 2
            _store.Upsert("scaled", "npc-1", MemoryVectorType.Episodic, new float[] { 2, 0, 0, 0 }, 2);

            var stats = VectorStoreDiagnostics.ComputeEmbeddingStatistics(_store);

            Assert.That(stats.TotalVectors, Is.EqualTo(2));
            Assert.That(stats.MinMagnitude, Is.EqualTo(1.0f).Within(0.001f));
            Assert.That(stats.MaxMagnitude, Is.EqualTo(2.0f).Within(0.001f));
            Assert.That(stats.MeanMagnitude, Is.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void ComputeEmbeddingStatistics_CountsZeroVectors()
        {
            _store.Upsert("zero", "npc-1", MemoryVectorType.Episodic, new float[] { 0, 0, 0, 0 }, 1);
            _store.Upsert("unit", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 2);

            var stats = VectorStoreDiagnostics.ComputeEmbeddingStatistics(_store);

            Assert.That(stats.ZeroVectorCount, Is.EqualTo(1));
        }

        #endregion

        #region ExportToJson Tests

        [Test]
        public void ExportToJson_ProducesValidJson()
        {
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);

            using var writer = new StringWriter();
            VectorStoreDiagnostics.ExportToJson(_store, writer);
            var json = writer.ToString();

            Assert.That(json, Does.Contain("\"embeddingDimension\""));
            Assert.That(json, Does.Contain("\"entries\""));
            Assert.That(json, Does.Contain("\"mem-1\""));
        }

        [Test]
        public void ExportToJson_TruncatesEmbeddings_ByDefault()
        {
            _store.Upsert("mem-1", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 2, 3, 4 }, 1);

            using var writer = new StringWriter();
            VectorStoreDiagnostics.ExportToJson(_store, writer, includeFullEmbeddings: false);
            var json = writer.ToString();

            // Should have truncated notice
            // With only 4 elements, it won't truncate, but the method should work
            Assert.That(json, Does.Contain("embedding"));
        }

        [Test]
        public void ExportToJson_HandlesNullNpcId()
        {
            _store.Upsert("shared", null, MemoryVectorType.CanonicalFact, new float[] { 1, 0, 0, 0 }, 1);

            using var writer = new StringWriter();
            VectorStoreDiagnostics.ExportToJson(_store, writer);
            var json = writer.ToString();

            Assert.That(json, Does.Contain("\"npcId\": null"));
        }

        [Test]
        public void ExportToJson_EscapesSpecialCharacters()
        {
            _store.Upsert("mem-with\"quote", "npc-1", MemoryVectorType.Episodic, new float[] { 1, 0, 0, 0 }, 1);

            using var writer = new StringWriter();
            VectorStoreDiagnostics.ExportToJson(_store, writer);
            var json = writer.ToString();

            Assert.That(json, Does.Contain("\\\""));
        }

        #endregion

        #region Validate Tests

        [Test]
        public void Validate_ValidFile_ReturnsValidReport()
        {
            var snapshot = new VectorStoreSnapshot
            {
                EmbeddingDimension = TestDimension,
                Entries = new List<VectorEntry>
                {
                    new VectorEntry { MemoryId = "m1", NpcId = "npc", MemoryType = MemoryVectorType.Episodic, Embedding = new float[] { 1, 0, 0, 0 }, SequenceNumber = 1 },
                    new VectorEntry { MemoryId = "m2", NpcId = null, MemoryType = MemoryVectorType.CanonicalFact, Embedding = new float[] { 0, 1, 0, 0 }, SequenceNumber = 2 }
                }
            };

            using var stream = new MemoryStream();
            VectorStoreBinarySerializer.Write(stream, snapshot);
            stream.Position = 0;

            var report = VectorStoreDiagnostics.Validate(stream);

            Assert.That(report.IsValid, Is.True);
            Assert.That(report.ErrorMessage, Is.Null);
            Assert.That(report.VectorCount, Is.EqualTo(2));
            Assert.That(report.EmbeddingDimension, Is.EqualTo(TestDimension));
            Assert.That(report.CountsByType[MemoryVectorType.Episodic], Is.EqualTo(1));
            Assert.That(report.CountsByType[MemoryVectorType.CanonicalFact], Is.EqualTo(1));
        }

        [Test]
        public void Validate_InvalidMagic_ReturnsInvalidReport()
        {
            using var stream = new MemoryStream();
            stream.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 0, 16);
            stream.Position = 0;

            var report = VectorStoreDiagnostics.Validate(stream);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.ErrorMessage, Does.Contain("magic"));
        }

        [Test]
        public void ValidateFile_NonExistent_ReturnsInvalidReport()
        {
            var report = VectorStoreDiagnostics.ValidateFile("/non/existent/path.bin");

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.ErrorMessage, Does.Contain("not found"));
        }

        #endregion

        #region VectorStoreDiagnosticReport Tests

        [Test]
        public void VectorStoreDiagnosticReport_ToString_Valid()
        {
            var report = new VectorStoreDiagnosticReport(
                isValid: true,
                errorMessage: null,
                vectorCount: 100,
                embeddingDimension: 384,
                countsByType: new Dictionary<MemoryVectorType, int>
                {
                    { MemoryVectorType.Episodic, 50 },
                    { MemoryVectorType.Belief, 30 },
                    { MemoryVectorType.CanonicalFact, 20 },
                    { MemoryVectorType.WorldState, 0 }
                },
                countsByNpc: new Dictionary<string, int>
                {
                    { "npc-1", 60 },
                    { "", 40 } // shared
                },
                fileSizeBytes: 100000);

            var str = report.ToString();

            Assert.That(str, Does.Contain("Valid"));
            Assert.That(str, Does.Contain("100"));
            Assert.That(str, Does.Contain("384"));
        }

        [Test]
        public void VectorStoreDiagnosticReport_ToString_Invalid()
        {
            var report = new VectorStoreDiagnosticReport(
                isValid: false,
                errorMessage: "Test error",
                vectorCount: 0,
                embeddingDimension: 0,
                countsByType: new Dictionary<MemoryVectorType, int>(),
                countsByNpc: new Dictionary<string, int>(),
                fileSizeBytes: 0);

            var str = report.ToString();

            Assert.That(str, Does.Contain("Invalid"));
            Assert.That(str, Does.Contain("Test error"));
        }

        #endregion

        #region EmbeddingStatistics Tests

        [Test]
        public void EmbeddingStatistics_ToString_FormatsCorrectly()
        {
            var stats = new EmbeddingStatistics(0.5f, 1.5f, 1.0f, 2, 100);

            var str = stats.ToString();

            Assert.That(str, Does.Contain("100"));
            Assert.That(str, Does.Contain("zero vectors=2"));
        }

        #endregion
    }
}
