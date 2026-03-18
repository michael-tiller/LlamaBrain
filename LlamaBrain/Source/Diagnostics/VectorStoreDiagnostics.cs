using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LlamaBrain.Core.Retrieval;
using LlamaBrain.Persistence;

namespace LlamaBrain.Diagnostics
{
    /// <summary>
    /// Diagnostic utilities for inspecting and validating vector store data.
    /// Useful for debugging since binary format is not human-readable.
    /// </summary>
    public static class VectorStoreDiagnostics
    {
        /// <summary>
        /// Validates a binary vector store file and returns diagnostic information.
        /// Does not fully load the file - only reads header and validates structure.
        /// </summary>
        /// <param name="stream">The stream to validate.</param>
        /// <returns>Diagnostic report with validation results.</returns>
        public static VectorStoreDiagnosticReport Validate(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            long fileSize = 0;
            try
            {
                fileSize = stream.Length;
            }
            catch
            {
                // Stream doesn't support Length
            }

            // Validate header first
            var headerInfo = VectorStoreBinarySerializer.ValidateHeader(stream);
            if (!headerInfo.IsValid)
            {
                return new VectorStoreDiagnosticReport(
                    isValid: false,
                    errorMessage: headerInfo.ErrorMessage,
                    vectorCount: 0,
                    embeddingDimension: 0,
                    countsByType: new Dictionary<MemoryVectorType, int>(),
                    countsByNpc: new Dictionary<string, int>(),
                    fileSizeBytes: fileSize);
            }

            // Reset stream to read full data for detailed analysis
            stream.Position = 0;

            try
            {
                var snapshot = VectorStoreBinarySerializer.Read(stream);

                // Build counts by type
                var countsByType = new Dictionary<MemoryVectorType, int>
                {
                    { MemoryVectorType.Episodic, 0 },
                    { MemoryVectorType.Belief, 0 },
                    { MemoryVectorType.CanonicalFact, 0 },
                    { MemoryVectorType.WorldState, 0 }
                };

                // Build counts by NPC (null key = shared)
                var countsByNpc = new Dictionary<string, int>();

                foreach (var entry in snapshot.Entries)
                {
                    // Count by type
                    if (countsByType.ContainsKey(entry.MemoryType))
                    {
                        countsByType[entry.MemoryType]++;
                    }

                    // Count by NPC (use empty string for null to avoid dictionary issues)
                    var npcKey = entry.NpcId ?? "";
                    if (!countsByNpc.ContainsKey(npcKey))
                    {
                        countsByNpc[npcKey] = 0;
                    }
                    countsByNpc[npcKey]++;
                }

                return new VectorStoreDiagnosticReport(
                    isValid: true,
                    errorMessage: null,
                    vectorCount: snapshot.Entries.Count,
                    embeddingDimension: snapshot.EmbeddingDimension,
                    countsByType: countsByType,
                    countsByNpc: countsByNpc,
                    fileSizeBytes: fileSize);
            }
            catch (Exception ex)
            {
                return new VectorStoreDiagnosticReport(
                    isValid: false,
                    errorMessage: $"Failed to read vector store: {ex.Message}",
                    vectorCount: (int)headerInfo.VectorCount,
                    embeddingDimension: headerInfo.EmbeddingDimension,
                    countsByType: new Dictionary<MemoryVectorType, int>(),
                    countsByNpc: new Dictionary<string, int>(),
                    fileSizeBytes: fileSize);
            }
        }

        /// <summary>
        /// Validates a binary vector store file by path.
        /// </summary>
        /// <param name="filePath">Path to the binary file.</param>
        /// <returns>Diagnostic report with validation results.</returns>
        public static VectorStoreDiagnosticReport ValidateFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
            {
                return new VectorStoreDiagnosticReport(
                    isValid: false,
                    errorMessage: $"File not found: {filePath}",
                    vectorCount: 0,
                    embeddingDimension: 0,
                    countsByType: new Dictionary<MemoryVectorType, int>(),
                    countsByNpc: new Dictionary<string, int>(),
                    fileSizeBytes: 0);
            }

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Validate(stream);
            }
        }

        /// <summary>
        /// Exports vector store contents to human-readable JSON for debugging.
        /// Embeddings are truncated to first 5 values for readability.
        /// </summary>
        /// <param name="store">The vector store to export.</param>
        /// <param name="writer">The text writer to write JSON to.</param>
        /// <param name="includeFullEmbeddings">If true, includes all embedding values. Default truncates to first 5.</param>
        public static void ExportToJson(IMemoryVectorStore store, TextWriter writer, bool includeFullEmbeddings = false)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            var snapshot = store.CreateSnapshot();
            var stats = store.GetStatistics();

            writer.WriteLine("{");
            writer.WriteLine($"  \"embeddingDimension\": {snapshot.EmbeddingDimension},");
            writer.WriteLine($"  \"totalVectors\": {stats.TotalVectors},");
            writer.WriteLine($"  \"statistics\": {{");
            writer.WriteLine($"    \"episodic\": {stats.EpisodicVectors},");
            writer.WriteLine($"    \"belief\": {stats.BeliefVectors},");
            writer.WriteLine($"    \"canonicalFact\": {stats.CanonicalFactVectors},");
            writer.WriteLine($"    \"worldState\": {stats.WorldStateVectors}");
            writer.WriteLine("  },");
            writer.WriteLine("  \"entries\": [");

            bool first = true;
            foreach (var entry in snapshot.Entries.OrderBy(e => e.SequenceNumber).ThenBy(e => e.MemoryId, StringComparer.Ordinal))
            {
                if (!first)
                {
                    writer.WriteLine(",");
                }
                first = false;

                writer.WriteLine("    {");
                writer.WriteLine($"      \"memoryId\": \"{EscapeJson(entry.MemoryId)}\",");
                writer.WriteLine($"      \"npcId\": {(entry.NpcId != null ? $"\"{EscapeJson(entry.NpcId)}\"" : "null")},");
                writer.WriteLine($"      \"memoryType\": \"{entry.MemoryType}\",");
                writer.WriteLine($"      \"sequenceNumber\": {entry.SequenceNumber},");

                // Embedding - truncate for readability unless full requested
                var embeddingStr = FormatEmbedding(entry.Embedding, includeFullEmbeddings);
                writer.WriteLine($"      \"embedding\": {embeddingStr}");

                writer.Write("    }");
            }

            writer.WriteLine();
            writer.WriteLine("  ]");
            writer.WriteLine("}");
        }

        /// <summary>
        /// Exports vector store contents to a JSON file.
        /// </summary>
        /// <param name="store">The vector store to export.</param>
        /// <param name="filePath">Path to write JSON to.</param>
        /// <param name="includeFullEmbeddings">If true, includes all embedding values.</param>
        public static void ExportToJsonFile(IMemoryVectorStore store, string filePath, bool includeFullEmbeddings = false)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                ExportToJson(store, writer, includeFullEmbeddings);
            }
        }

        /// <summary>
        /// Gets a concise summary string for logging or display.
        /// </summary>
        /// <param name="store">The vector store to summarize.</param>
        /// <returns>Summary string suitable for logging.</returns>
        public static string GetSummary(IMemoryVectorStore store)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            var stats = store.GetStatistics();
            var snapshot = store.CreateSnapshot();

            // Count shared vs NPC-specific
            int sharedCount = 0;
            var npcCounts = new Dictionary<string, int>();

            foreach (var entry in snapshot.Entries)
            {
                if (entry.NpcId == null)
                {
                    sharedCount++;
                }
                else
                {
                    if (!npcCounts.ContainsKey(entry.NpcId))
                    {
                        npcCounts[entry.NpcId] = 0;
                    }
                    npcCounts[entry.NpcId]++;
                }
            }

            var sb = new StringBuilder();
            sb.Append($"VectorStore: {stats.TotalVectors:N0} vectors (dim={stats.EmbeddingDimension})");
            sb.Append($" | Episodic: {stats.EpisodicVectors:N0}");
            sb.Append($", Belief: {stats.BeliefVectors:N0}");
            sb.Append($", Canonical: {stats.CanonicalFactVectors:N0}");
            sb.Append($", WorldState: {stats.WorldStateVectors:N0}");
            sb.Append($" | Shared: {sharedCount:N0}");

            if (npcCounts.Count > 0)
            {
                sb.Append(" | NPCs: ");
                sb.Append(string.Join(", ", npcCounts.OrderBy(kvp => kvp.Key, StringComparer.Ordinal).Select(kvp => $"{kvp.Key}={kvp.Value:N0}")));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Finds entries by memory ID pattern (supports * and ? wildcards).
        /// </summary>
        /// <param name="store">The vector store to search.</param>
        /// <param name="pattern">Wildcard pattern to match (e.g., "episodic_*" or "belief_wizard_?").</param>
        /// <returns>Matching entries in deterministic order.</returns>
        public static IEnumerable<VectorEntry> FindByIdPattern(IMemoryVectorStore store, string pattern)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));
            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentNullException(nameof(pattern));

            var snapshot = store.CreateSnapshot();

            // Convert wildcard pattern to regex
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

            return snapshot.Entries
                .Where(e => regex.IsMatch(e.MemoryId))
                .OrderBy(e => e.SequenceNumber)
                .ThenBy(e => e.MemoryId, StringComparer.Ordinal);
        }

        /// <summary>
        /// Finds entries by NPC ID.
        /// </summary>
        /// <param name="store">The vector store to search.</param>
        /// <param name="npcId">NPC ID to find, or null for shared entries.</param>
        /// <returns>Matching entries in deterministic order.</returns>
        public static IEnumerable<VectorEntry> FindByNpcId(IMemoryVectorStore store, string? npcId)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            var snapshot = store.CreateSnapshot();

            return snapshot.Entries
                .Where(e => e.NpcId == npcId)
                .OrderBy(e => e.SequenceNumber)
                .ThenBy(e => e.MemoryId, StringComparer.Ordinal);
        }

        /// <summary>
        /// Finds entries by memory type.
        /// </summary>
        /// <param name="store">The vector store to search.</param>
        /// <param name="memoryType">Memory type to find.</param>
        /// <returns>Matching entries in deterministic order.</returns>
        public static IEnumerable<VectorEntry> FindByMemoryType(IMemoryVectorStore store, MemoryVectorType memoryType)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            var snapshot = store.CreateSnapshot();

            return snapshot.Entries
                .Where(e => e.MemoryType == memoryType)
                .OrderBy(e => e.SequenceNumber)
                .ThenBy(e => e.MemoryId, StringComparer.Ordinal);
        }

        /// <summary>
        /// Gets duplicate memory IDs if any exist (indicates data corruption).
        /// </summary>
        /// <param name="store">The vector store to check.</param>
        /// <returns>List of duplicate memory IDs.</returns>
        public static IReadOnlyList<string> FindDuplicateIds(IMemoryVectorStore store)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            var snapshot = store.CreateSnapshot();

            return snapshot.Entries
                .GroupBy(e => e.MemoryId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Checks for embedding dimension mismatches (indicates data corruption).
        /// </summary>
        /// <param name="store">The vector store to check.</param>
        /// <returns>List of entry IDs with mismatched dimensions.</returns>
        public static IReadOnlyList<string> FindDimensionMismatches(IMemoryVectorStore store)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            var snapshot = store.CreateSnapshot();
            var expectedDimension = snapshot.EmbeddingDimension;

            return snapshot.Entries
                .Where(e => e.Embedding.Length != expectedDimension)
                .Select(e => e.MemoryId)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Computes embedding statistics (min, max, mean magnitude).
        /// </summary>
        /// <param name="store">The vector store to analyze.</param>
        /// <returns>Embedding statistics.</returns>
        public static EmbeddingStatistics ComputeEmbeddingStatistics(IMemoryVectorStore store)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            var snapshot = store.CreateSnapshot();

            if (snapshot.Entries.Count == 0)
            {
                return new EmbeddingStatistics(0, 0, 0, 0, 0);
            }

            double minMagnitude = double.MaxValue;
            double maxMagnitude = double.MinValue;
            double sumMagnitude = 0;
            int zeroVectorCount = 0;

            foreach (var entry in snapshot.Entries)
            {
                double magnitude = ComputeMagnitude(entry.Embedding);

                if (magnitude < double.Epsilon)
                {
                    zeroVectorCount++;
                }

                minMagnitude = Math.Min(minMagnitude, magnitude);
                maxMagnitude = Math.Max(maxMagnitude, magnitude);
                sumMagnitude += magnitude;
            }

            double meanMagnitude = sumMagnitude / snapshot.Entries.Count;

            return new EmbeddingStatistics(
                minMagnitude: (float)minMagnitude,
                maxMagnitude: (float)maxMagnitude,
                meanMagnitude: (float)meanMagnitude,
                zeroVectorCount: zeroVectorCount,
                totalVectors: snapshot.Entries.Count);
        }

        private static double ComputeMagnitude(float[] embedding)
        {
            double sumSquares = 0;
            for (int i = 0; i < embedding.Length; i++)
            {
                sumSquares += embedding[i] * embedding[i];
            }
            return Math.Sqrt(sumSquares);
        }

        private static string FormatEmbedding(float[] embedding, bool full)
        {
            if (embedding == null || embedding.Length == 0)
            {
                return "[]";
            }

            if (full)
            {
                return "[" + string.Join(", ", embedding.Select(v => v.ToString("G6"))) + "]";
            }

            // Truncate to first 5 values
            var preview = embedding.Take(5).Select(v => v.ToString("G6")).ToList();
            if (embedding.Length > 5)
            {
                return "[" + string.Join(", ", preview) + $", ... ({embedding.Length - 5} more)]";
            }
            return "[" + string.Join(", ", preview) + "]";
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }

    /// <summary>
    /// Diagnostic report for vector store validation.
    /// </summary>
    public sealed class VectorStoreDiagnosticReport
    {
        /// <summary>Whether the vector store is valid.</summary>
        public bool IsValid { get; }

        /// <summary>Error message if invalid, null otherwise.</summary>
        public string? ErrorMessage { get; }

        /// <summary>Total number of vectors.</summary>
        public int VectorCount { get; }

        /// <summary>Embedding dimension.</summary>
        public int EmbeddingDimension { get; }

        /// <summary>Count of vectors by memory type.</summary>
        public IReadOnlyDictionary<MemoryVectorType, int> CountsByType { get; }

        /// <summary>Count of vectors by NPC ID (empty string key = shared/null NPC).</summary>
        public IReadOnlyDictionary<string, int> CountsByNpc { get; }

        /// <summary>File size in bytes (0 if stream doesn't support Length).</summary>
        public long FileSizeBytes { get; }

        public VectorStoreDiagnosticReport(
            bool isValid,
            string? errorMessage,
            int vectorCount,
            int embeddingDimension,
            Dictionary<MemoryVectorType, int> countsByType,
            Dictionary<string, int> countsByNpc,
            long fileSizeBytes)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
            VectorCount = vectorCount;
            EmbeddingDimension = embeddingDimension;
            CountsByType = countsByType;
            CountsByNpc = countsByNpc;
            FileSizeBytes = fileSizeBytes;
        }

        /// <summary>
        /// Gets a formatted summary string.
        /// </summary>
        public override string ToString()
        {
            if (!IsValid)
            {
                return $"Invalid: {ErrorMessage}";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Valid: {VectorCount:N0} vectors, dim={EmbeddingDimension}");

            if (FileSizeBytes > 0)
            {
                sb.AppendLine($"File size: {FormatFileSize(FileSizeBytes)}");
            }

            sb.AppendLine("By type:");
            foreach (var kvp in CountsByType.OrderBy(k => k.Key))
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value:N0}");
            }

            sb.AppendLine("By NPC:");
            foreach (var kvp in CountsByNpc.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var npcName = string.IsNullOrEmpty(kvp.Key) ? "(shared)" : kvp.Key;
                sb.AppendLine($"  {npcName}: {kvp.Value:N0}");
            }

            return sb.ToString();
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }

    /// <summary>
    /// Statistics about embedding vectors.
    /// </summary>
    public sealed class EmbeddingStatistics
    {
        /// <summary>Minimum embedding magnitude (L2 norm).</summary>
        public float MinMagnitude { get; }

        /// <summary>Maximum embedding magnitude (L2 norm).</summary>
        public float MaxMagnitude { get; }

        /// <summary>Mean embedding magnitude (L2 norm).</summary>
        public float MeanMagnitude { get; }

        /// <summary>Number of zero vectors (potential issues).</summary>
        public int ZeroVectorCount { get; }

        /// <summary>Total number of vectors analyzed.</summary>
        public int TotalVectors { get; }

        public EmbeddingStatistics(float minMagnitude, float maxMagnitude, float meanMagnitude, int zeroVectorCount, int totalVectors)
        {
            MinMagnitude = minMagnitude;
            MaxMagnitude = maxMagnitude;
            MeanMagnitude = meanMagnitude;
            ZeroVectorCount = zeroVectorCount;
            TotalVectors = totalVectors;
        }

        public override string ToString()
        {
            return $"Embeddings: {TotalVectors:N0} vectors, magnitude min={MinMagnitude:F4} max={MaxMagnitude:F4} mean={MeanMagnitude:F4}, zero vectors={ZeroVectorCount}";
        }
    }
}
