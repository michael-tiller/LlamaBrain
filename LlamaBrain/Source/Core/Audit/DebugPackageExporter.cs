using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace LlamaBrain.Core.Audit
{
  /// <summary>
  /// Exports audit records from an AuditRecorder to a DebugPackage.
  /// The package can be serialized to JSON for sharing bug reports.
  /// Supports optional GZip compression for large packages.
  /// </summary>
  /// <remarks>
  /// Performance targets:
  /// - Export 50 turns: &lt; 100ms
  /// - Package size: &lt; 10MB for 50 turns
  ///
  /// Compression:
  /// - Compressed packages use GZip format
  /// - File format: Magic header (4 bytes) + GZip stream
  /// - Magic header: "LBPK" (0x4C, 0x42, 0x50, 0x4B)
  /// - Typical compression ratio: 70-90% reduction
  /// </remarks>
  public sealed class DebugPackageExporter
  {
    /// <summary>
    /// Magic header bytes for compressed packages: "LBPK" (LlamaBrain PacKage).
    /// </summary>
    public static readonly byte[] CompressedMagicHeader = { 0x4C, 0x42, 0x50, 0x4B };

    /// <summary>
    /// Exports all records from the recorder to a debug package.
    /// </summary>
    /// <param name="recorder">The audit recorder containing records to export.</param>
    /// <param name="modelFingerprint">Optional model fingerprint for replay validation.</param>
    /// <param name="options">Optional export options for metadata.</param>
    /// <returns>A DebugPackage containing all records.</returns>
    public DebugPackage Export(
      IAuditRecorder recorder,
      ModelFingerprint? modelFingerprint = null,
      ExportOptions? options = null)
    {
      if (recorder == null)
        throw new ArgumentNullException(nameof(recorder));

      var package = new DebugPackage
      {
        PackageId = GeneratePackageId(),
        CreatedAtUtcTicks = DateTimeOffset.UtcNow.UtcTicks,
        ModelFingerprint = modelFingerprint ?? new ModelFingerprint(),
        GameVersion = options?.GameVersion ?? "",
        SceneName = options?.SceneName ?? "",
        CreatorNotes = options?.CreatorNotes ?? ""
      };

      // Collect all records from all NPCs
      var allRecords = recorder.GetAllRecords();
      package.Records = allRecords.ToList();

      // Collect unique NPC IDs
      package.NpcIds = allRecords
        .Select(r => r.NpcId)
        .Distinct()
        .OrderBy(id => id, StringComparer.Ordinal)
        .ToList();

      // Update statistics
      package.UpdateStatistics();

      // Compute integrity hash
      package.ComputeIntegrityHash();

      return package;
    }

    /// <summary>
    /// Exports records for a specific NPC to a debug package.
    /// </summary>
    /// <param name="recorder">The audit recorder containing records.</param>
    /// <param name="npcId">The NPC ID to export records for.</param>
    /// <param name="modelFingerprint">Optional model fingerprint for replay validation.</param>
    /// <param name="options">Optional export options for metadata.</param>
    /// <returns>A DebugPackage containing records for the specified NPC.</returns>
    public DebugPackage ExportNpc(
      IAuditRecorder recorder,
      string npcId,
      ModelFingerprint? modelFingerprint = null,
      ExportOptions? options = null)
    {
      if (recorder == null)
        throw new ArgumentNullException(nameof(recorder));

      if (npcId == null)
        throw new ArgumentNullException(nameof(npcId));

      if (string.IsNullOrWhiteSpace(npcId))
        throw new ArgumentException("NPC ID cannot be empty or whitespace.", nameof(npcId));

      var package = new DebugPackage
      {
        PackageId = GeneratePackageId(),
        CreatedAtUtcTicks = DateTimeOffset.UtcNow.UtcTicks,
        ModelFingerprint = modelFingerprint ?? new ModelFingerprint(),
        GameVersion = options?.GameVersion ?? "",
        SceneName = options?.SceneName ?? "",
        CreatorNotes = options?.CreatorNotes ?? ""
      };

      // Get records for specific NPC
      var records = recorder.GetRecords(npcId);
      package.Records = records.ToList();

      // Set NPC IDs
      if (records.Length > 0)
      {
        package.NpcIds = new List<string> { npcId };
      }

      // Update statistics
      package.UpdateStatistics();

      // Compute integrity hash
      package.ComputeIntegrityHash();

      return package;
    }

    /// <summary>
    /// Serializes a debug package to JSON.
    /// </summary>
    /// <param name="package">The package to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>JSON string representation of the package.</returns>
    public string ToJson(DebugPackage package, bool indented = false)
    {
      if (package == null)
        throw new ArgumentNullException(nameof(package));

      var formatting = indented ? Formatting.Indented : Formatting.None;
      return JsonConvert.SerializeObject(package, formatting);
    }

    /// <summary>
    /// Exports a debug package to compressed bytes using GZip.
    /// </summary>
    /// <param name="package">The package to export.</param>
    /// <param name="compressionLevel">Compression level (1-9, default 6).</param>
    /// <returns>Compressed byte array with magic header.</returns>
    public byte[] ToCompressedBytes(DebugPackage package, int compressionLevel = 6)
    {
      if (package == null)
        throw new ArgumentNullException(nameof(package));

      if (compressionLevel < 1 || compressionLevel > 9)
        throw new ArgumentOutOfRangeException(nameof(compressionLevel), "Compression level must be between 1 and 9.");

      // Serialize to JSON
      var json = JsonConvert.SerializeObject(package, Formatting.None);
      var jsonBytes = Encoding.UTF8.GetBytes(json);

      // Compress with GZip
      using (var outputStream = new MemoryStream())
      {
        // Write magic header
        outputStream.Write(CompressedMagicHeader, 0, CompressedMagicHeader.Length);

        // Map compression level (1-9) to CompressionLevel enum
        // Note: .NET Standard 2.1 only has Fastest, Optimal, NoCompression
        // Higher levels use Optimal (the best available in .NET Standard 2.1)
        var level = compressionLevel <= 3 ? CompressionLevel.Fastest : CompressionLevel.Optimal;

        using (var gzipStream = new GZipStream(outputStream, level, leaveOpen: true))
        {
          gzipStream.Write(jsonBytes, 0, jsonBytes.Length);
        }

        return outputStream.ToArray();
      }
    }

    /// <summary>
    /// Exports all records from the recorder to compressed bytes.
    /// </summary>
    /// <param name="recorder">The audit recorder containing records to export.</param>
    /// <param name="options">Export options including compression settings.</param>
    /// <returns>Compressed byte array with magic header.</returns>
    public byte[] ExportToCompressedBytes(IAuditRecorder recorder, ExportOptions? options = null)
    {
      var package = Export(recorder, options?.ModelFingerprint, options);
      var compressionLevel = options?.CompressionLevel ?? 6;
      return ToCompressedBytes(package, compressionLevel);
    }

    /// <summary>
    /// Exports a debug package to bytes (compressed or uncompressed based on options).
    /// </summary>
    /// <param name="package">The package to export.</param>
    /// <param name="options">Export options controlling compression.</param>
    /// <returns>Byte array (compressed with header if UseCompression is true, otherwise UTF8 JSON).</returns>
    public byte[] ToBytes(DebugPackage package, ExportOptions? options = null)
    {
      if (package == null)
        throw new ArgumentNullException(nameof(package));

      if (options?.UseCompression == true)
      {
        return ToCompressedBytes(package, options.CompressionLevel);
      }
      else
      {
        var json = ToJson(package, indented: false);
        return Encoding.UTF8.GetBytes(json);
      }
    }

    /// <summary>
    /// Checks if the given bytes represent a compressed package by checking the magic header.
    /// </summary>
    /// <param name="data">The byte array to check.</param>
    /// <returns>True if the data starts with the compressed magic header.</returns>
    public static bool IsCompressed(byte[] data)
    {
      if (data == null || data.Length < CompressedMagicHeader.Length)
        return false;

      for (int i = 0; i < CompressedMagicHeader.Length; i++)
      {
        if (data[i] != CompressedMagicHeader[i])
          return false;
      }

      return true;
    }

    /// <summary>
    /// Generates a unique package ID.
    /// </summary>
    private static string GeneratePackageId()
    {
      var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
      var guid = Guid.NewGuid().ToString("N").Substring(0, 8);
      return $"debug-{timestamp}-{guid}";
    }
  }
}
