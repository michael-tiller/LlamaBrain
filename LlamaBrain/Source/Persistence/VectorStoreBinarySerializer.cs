using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LlamaBrain.Core.Retrieval;

namespace LlamaBrain.Persistence
{
    /// <summary>
    /// Binary serializer for vector store snapshots.
    ///
    /// <para>
    /// Binary format is significantly faster and more compact than JSON for float arrays:
    /// - JSON: ~10 bytes per float (e.g., "[0.123456, 0.234567, ...]")
    /// - Binary: 4 bytes per float (direct IEEE 754)
    /// - Result: ~60% size reduction, ~10x faster load
    /// </para>
    ///
    /// <para>
    /// Format specification:
    /// <code>
    /// Header (16 bytes):
    ///   - Magic: "LBVS" (4 bytes) - LlamaBrain Vector Store
    ///   - Version: uint16 (2 bytes)
    ///   - EmbeddingDimension: uint16 (2 bytes)
    ///   - VectorCount: uint32 (4 bytes)
    ///   - Reserved: 4 bytes (for future use)
    ///
    /// Per Entry:
    ///   - MemoryIdLength: uint8 (1 byte)
    ///   - MemoryId: UTF-8 bytes (variable)
    ///   - NpcIdLength: uint8 (1 byte, 0 = null)
    ///   - NpcId: UTF-8 bytes (variable, omitted if length=0)
    ///   - MemoryType: uint8 (1 byte)
    ///   - SequenceNumber: int64 (8 bytes)
    ///   - Embedding: float32[] (dimension * 4 bytes)
    /// </code>
    /// </para>
    /// </summary>
    public static class VectorStoreBinarySerializer
    {
        /// <summary>
        /// Magic bytes identifying a LlamaBrain Vector Store file.
        /// </summary>
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("LBVS");

        /// <summary>
        /// Current binary format version.
        /// </summary>
        public const ushort CurrentVersion = 1;

        /// <summary>
        /// Header size in bytes.
        /// </summary>
        public const int HeaderSize = 16;

        /// <summary>
        /// Writes a vector store snapshot to a binary stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="snapshot">The snapshot to serialize.</param>
        /// <exception cref="ArgumentNullException">If stream or snapshot is null.</exception>
        /// <exception cref="ArgumentException">If embedding dimension exceeds uint16 max.</exception>
        public static void Write(Stream stream, VectorStoreSnapshot snapshot)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.EmbeddingDimension > ushort.MaxValue)
                throw new ArgumentException($"Embedding dimension {snapshot.EmbeddingDimension} exceeds maximum {ushort.MaxValue}", nameof(snapshot));
            // Note: Entries.Count is int, which always fits in uint32, so no validation needed

            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                // Write header
                writer.Write(Magic);
                writer.Write(CurrentVersion);
                writer.Write((ushort)snapshot.EmbeddingDimension);
                writer.Write((uint)snapshot.Entries.Count);
                writer.Write((uint)0); // Reserved

                // Write entries
                foreach (var entry in snapshot.Entries)
                {
                    WriteEntry(writer, entry, snapshot.EmbeddingDimension);
                }
            }
        }

        /// <summary>
        /// Reads a vector store snapshot from a binary stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>The deserialized snapshot.</returns>
        /// <exception cref="ArgumentNullException">If stream is null.</exception>
        /// <exception cref="InvalidDataException">If the stream contains invalid data.</exception>
        public static VectorStoreSnapshot Read(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                // Read and validate header
                var magic = reader.ReadBytes(4);
                if (!ByteArraysEqual(magic, Magic))
                {
                    throw new InvalidDataException($"Invalid magic bytes. Expected 'LBVS', got '{Encoding.ASCII.GetString(magic)}'");
                }

                var version = reader.ReadUInt16();
                if (version > CurrentVersion)
                {
                    throw new InvalidDataException($"Unsupported version {version}. Maximum supported version is {CurrentVersion}");
                }

                var embeddingDimension = reader.ReadUInt16();
                var vectorCount = reader.ReadUInt32();
                var reserved = reader.ReadUInt32(); // Reserved, ignored for now

                // Create snapshot
                var snapshot = new VectorStoreSnapshot
                {
                    EmbeddingDimension = embeddingDimension,
                    Entries = new List<VectorEntry>((int)vectorCount)
                };

                // Read entries
                for (uint i = 0; i < vectorCount; i++)
                {
                    var entry = ReadEntry(reader, embeddingDimension);
                    snapshot.Entries.Add(entry);
                }

                return snapshot;
            }
        }

        /// <summary>
        /// Writes a vector store snapshot to a file.
        /// </summary>
        /// <param name="filePath">The file path to write to.</param>
        /// <param name="snapshot">The snapshot to serialize.</param>
        public static void WriteToFile(string filePath, VectorStoreSnapshot snapshot)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Write(stream, snapshot);
            }
        }

        /// <summary>
        /// Reads a vector store snapshot from a file.
        /// </summary>
        /// <param name="filePath">The file path to read from.</param>
        /// <returns>The deserialized snapshot.</returns>
        public static VectorStoreSnapshot ReadFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Read(stream);
            }
        }

        /// <summary>
        /// Validates a binary vector store file without fully loading it.
        /// Returns information about the file if valid, or throws if invalid.
        /// </summary>
        /// <param name="stream">The stream to validate.</param>
        /// <returns>Validation result with file information.</returns>
        public static VectorStoreFileInfo ValidateHeader(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                // Read and validate header
                var magic = reader.ReadBytes(4);
                if (!ByteArraysEqual(magic, Magic))
                {
                    return new VectorStoreFileInfo(
                        isValid: false,
                        errorMessage: $"Invalid magic bytes. Expected 'LBVS', got '{Encoding.ASCII.GetString(magic)}'",
                        version: 0,
                        embeddingDimension: 0,
                        vectorCount: 0);
                }

                var version = reader.ReadUInt16();
                if (version > CurrentVersion)
                {
                    return new VectorStoreFileInfo(
                        isValid: false,
                        errorMessage: $"Unsupported version {version}. Maximum supported version is {CurrentVersion}",
                        version: version,
                        embeddingDimension: 0,
                        vectorCount: 0);
                }

                var embeddingDimension = reader.ReadUInt16();
                var vectorCount = reader.ReadUInt32();
                reader.ReadUInt32(); // Reserved bytes - must consume to leave stream at correct position

                return new VectorStoreFileInfo(
                    isValid: true,
                    errorMessage: null,
                    version: version,
                    embeddingDimension: embeddingDimension,
                    vectorCount: vectorCount);
            }
        }

        /// <summary>
        /// Estimates the file size for a snapshot without actually serializing.
        /// </summary>
        /// <param name="vectorCount">Number of vectors.</param>
        /// <param name="embeddingDimension">Dimension of each embedding.</param>
        /// <param name="avgIdLength">Average memory ID length (default 8).</param>
        /// <param name="avgNpcIdLength">Average NPC ID length (default 8, or 0 if mostly null).</param>
        /// <returns>Estimated file size in bytes.</returns>
        public static long EstimateFileSize(int vectorCount, int embeddingDimension, int avgIdLength = 8, int avgNpcIdLength = 4)
        {
            // Header: 16 bytes
            // Per entry: 1 + memoryIdLen + 1 + npcIdLen + 1 + 8 + (dim * 4)
            long perEntryOverhead = 1 + avgIdLength + 1 + avgNpcIdLength + 1 + 8;
            long embeddingSize = embeddingDimension * 4;
            return HeaderSize + (vectorCount * (perEntryOverhead + embeddingSize));
        }

        private static void WriteEntry(BinaryWriter writer, VectorEntry entry, int expectedDimension)
        {
            // Validate embedding dimension
            if (entry.Embedding.Length != expectedDimension)
            {
                throw new InvalidDataException($"Entry '{entry.MemoryId}' has embedding dimension {entry.Embedding.Length}, expected {expectedDimension}");
            }

            // Write MemoryId
            var memoryIdBytes = Encoding.UTF8.GetBytes(entry.MemoryId);
            if (memoryIdBytes.Length > 255)
            {
                throw new InvalidDataException($"MemoryId '{entry.MemoryId}' exceeds maximum length of 255 bytes when UTF-8 encoded");
            }
            writer.Write((byte)memoryIdBytes.Length);
            writer.Write(memoryIdBytes);

            // Write NpcId (null = length 0)
            if (entry.NpcId == null)
            {
                writer.Write((byte)0);
            }
            else
            {
                var npcIdBytes = Encoding.UTF8.GetBytes(entry.NpcId);
                if (npcIdBytes.Length > 255)
                {
                    throw new InvalidDataException($"NpcId '{entry.NpcId}' exceeds maximum length of 255 bytes when UTF-8 encoded");
                }
                writer.Write((byte)npcIdBytes.Length);
                writer.Write(npcIdBytes);
            }

            // Write MemoryType
            writer.Write((byte)entry.MemoryType);

            // Write SequenceNumber
            writer.Write(entry.SequenceNumber);

            // Write Embedding (as raw float32 bytes)
            foreach (var value in entry.Embedding)
            {
                writer.Write(value);
            }
        }

        private static VectorEntry ReadEntry(BinaryReader reader, int embeddingDimension)
        {
            // Read MemoryId
            var memoryIdLength = reader.ReadByte();
            var memoryIdBytes = reader.ReadBytes(memoryIdLength);
            var memoryId = Encoding.UTF8.GetString(memoryIdBytes);

            // Read NpcId
            var npcIdLength = reader.ReadByte();
            string? npcId = null;
            if (npcIdLength > 0)
            {
                var npcIdBytes = reader.ReadBytes(npcIdLength);
                npcId = Encoding.UTF8.GetString(npcIdBytes);
            }

            // Read MemoryType
            var memoryType = (MemoryVectorType)reader.ReadByte();

            // Read SequenceNumber
            var sequenceNumber = reader.ReadInt64();

            // Read Embedding
            var embedding = new float[embeddingDimension];
            for (int i = 0; i < embeddingDimension; i++)
            {
                embedding[i] = reader.ReadSingle();
            }

            return new VectorEntry
            {
                MemoryId = memoryId,
                NpcId = npcId,
                MemoryType = memoryType,
                SequenceNumber = sequenceNumber,
                Embedding = embedding
            };
        }

        private static bool ByteArraysEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Information about a vector store binary file from header validation.
    /// </summary>
    public sealed class VectorStoreFileInfo
    {
        /// <summary>Whether the file header is valid.</summary>
        public bool IsValid { get; }

        /// <summary>Error message if invalid, null otherwise.</summary>
        public string? ErrorMessage { get; }

        /// <summary>Binary format version.</summary>
        public ushort Version { get; }

        /// <summary>Embedding dimension.</summary>
        public int EmbeddingDimension { get; }

        /// <summary>Number of vectors in the file.</summary>
        public uint VectorCount { get; }

        public VectorStoreFileInfo(bool isValid, string? errorMessage, ushort version, int embeddingDimension, uint vectorCount)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
            Version = version;
            EmbeddingDimension = embeddingDimension;
            VectorCount = vectorCount;
        }
    }
}
