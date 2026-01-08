using System;
using System.Text;
using NUnit.Framework;
using LlamaBrain.Core.Audit;

namespace LlamaBrain.Tests.Audit
{
  /// <summary>
  /// Tests for debug package compression functionality.
  /// </summary>
  [TestFixture]
  [Category("Audit")]
  public class CompressionTests
  {
    private DebugPackageExporter _exporter = null!;
    private DebugPackageImporter _importer = null!;

    [SetUp]
    public void SetUp()
    {
      _exporter = new DebugPackageExporter();
      _importer = new DebugPackageImporter();
    }

    #region Magic Header Tests

    [Test]
    public void CompressedMagicHeader_IsLBPK()
    {
      var header = DebugPackageExporter.CompressedMagicHeader;

      Assert.That(header.Length, Is.EqualTo(4));
      Assert.That(header[0], Is.EqualTo(0x4C)); // 'L'
      Assert.That(header[1], Is.EqualTo(0x42)); // 'B'
      Assert.That(header[2], Is.EqualTo(0x50)); // 'P'
      Assert.That(header[3], Is.EqualTo(0x4B)); // 'K'
    }

    [Test]
    public void IsCompressed_WithValidHeader_ReturnsTrue()
    {
      var data = new byte[] { 0x4C, 0x42, 0x50, 0x4B, 0x00, 0x00 };

      Assert.That(DebugPackageExporter.IsCompressed(data), Is.True);
    }

    [Test]
    public void IsCompressed_WithJsonData_ReturnsFalse()
    {
      var json = "{\"PackageId\":\"test\"}";
      var data = Encoding.UTF8.GetBytes(json);

      Assert.That(DebugPackageExporter.IsCompressed(data), Is.False);
    }

    [Test]
    public void IsCompressed_NullData_ReturnsFalse()
    {
      Assert.That(DebugPackageExporter.IsCompressed(null!), Is.False);
    }

    [Test]
    public void IsCompressed_EmptyData_ReturnsFalse()
    {
      Assert.That(DebugPackageExporter.IsCompressed(Array.Empty<byte>()), Is.False);
    }

    [Test]
    public void IsCompressed_TooShortData_ReturnsFalse()
    {
      var data = new byte[] { 0x4C, 0x42, 0x50 }; // Only 3 bytes

      Assert.That(DebugPackageExporter.IsCompressed(data), Is.False);
    }

    #endregion

    #region Compression Export Tests

    [Test]
    public void ToCompressedBytes_CreatesValidCompressedPackage()
    {
      var package = CreateTestPackage(5);

      var compressed = _exporter.ToCompressedBytes(package);

      Assert.That(compressed, Is.Not.Null);
      Assert.That(compressed.Length, Is.GreaterThan(4)); // At least header + some data
      Assert.That(DebugPackageExporter.IsCompressed(compressed), Is.True);
    }

    [Test]
    public void ToCompressedBytes_ReducesSize()
    {
      var package = CreateTestPackage(20);
      var json = _exporter.ToJson(package);
      var uncompressedSize = Encoding.UTF8.GetBytes(json).Length;

      var compressed = _exporter.ToCompressedBytes(package);

      // Compressed should be smaller (JSON compresses well)
      Assert.That(compressed.Length, Is.LessThan(uncompressedSize));
    }

    [Test]
    public void ToCompressedBytes_NullPackage_ThrowsArgumentNull()
    {
      Assert.Throws<ArgumentNullException>(() => _exporter.ToCompressedBytes(null!));
    }

    [Test]
    public void ToCompressedBytes_InvalidCompressionLevel_ThrowsArgumentOutOfRange()
    {
      var package = CreateTestPackage(1);

      Assert.Throws<ArgumentOutOfRangeException>(() => _exporter.ToCompressedBytes(package, 0));
      Assert.Throws<ArgumentOutOfRangeException>(() => _exporter.ToCompressedBytes(package, 10));
    }

    [Test]
    public void ToCompressedBytes_DifferentLevels_AllSucceed()
    {
      var package = CreateTestPackage(5);

      for (int level = 1; level <= 9; level++)
      {
        var compressed = _exporter.ToCompressedBytes(package, level);
        Assert.That(DebugPackageExporter.IsCompressed(compressed), Is.True,
          $"Compression level {level} should produce valid output");
      }
    }

    [Test]
    public void ToCompressedBytes_HigherLevel_SmallerSize()
    {
      var package = CreateTestPackage(50);

      var fast = _exporter.ToCompressedBytes(package, 1);
      var best = _exporter.ToCompressedBytes(package, 9);

      // Best compression should be smaller or equal
      Assert.That(best.Length, Is.LessThanOrEqualTo(fast.Length));
    }

    #endregion

    #region Compression Import Tests

    [Test]
    public void FromCompressedBytes_DecompressesSuccessfully()
    {
      var original = CreateTestPackage(5);
      var compressed = _exporter.ToCompressedBytes(original);

      var result = _importer.FromCompressedBytes(compressed);

      Assert.That(result.Success, Is.True);
      Assert.That(result.Package, Is.Not.Null);
      Assert.That(result.Package!.PackageId, Is.EqualTo(original.PackageId));
      Assert.That(result.Package.Records.Count, Is.EqualTo(original.Records.Count));
    }

    [Test]
    public void FromCompressedBytes_SetsCompressionMetadata()
    {
      var package = CreateTestPackage(10);
      var compressed = _exporter.ToCompressedBytes(package);

      var result = _importer.FromCompressedBytes(compressed);

      Assert.That(result.WasCompressed, Is.True);
      Assert.That(result.CompressedSize, Is.EqualTo(compressed.Length));
      Assert.That(result.UncompressedSize, Is.GreaterThan(0));
      Assert.That(result.CompressionRatio, Is.GreaterThan(1.0)); // Ratio > 1 means compression worked
    }

    [Test]
    public void FromCompressedBytes_NullData_ReturnsFailed()
    {
      var result = _importer.FromCompressedBytes(null!);

      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("null or empty"));
    }

    [Test]
    public void FromCompressedBytes_InvalidHeader_ReturnsFailed()
    {
      var data = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 };

      var result = _importer.FromCompressedBytes(data);

      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("magic header"));
    }

    [Test]
    public void FromCompressedBytes_CorruptedData_ReturnsFailed()
    {
      // Valid header but garbage data
      var data = new byte[] { 0x4C, 0x42, 0x50, 0x4B, 0xFF, 0xFF, 0xFF };

      var result = _importer.FromCompressedBytes(data);

      Assert.That(result.Success, Is.False);
    }

    #endregion

    #region Auto-Detection Tests

    [Test]
    public void FromBytes_AutoDetectsCompressed()
    {
      var package = CreateTestPackage(5);
      var compressed = _exporter.ToCompressedBytes(package);

      var result = _importer.FromBytes(compressed);

      Assert.That(result.Success, Is.True);
      Assert.That(result.WasCompressed, Is.True);
      Assert.That(result.Package!.PackageId, Is.EqualTo(package.PackageId));
    }

    [Test]
    public void FromBytes_AutoDetectsUncompressed()
    {
      var package = CreateTestPackage(5);
      var json = _exporter.ToJson(package);
      var uncompressed = Encoding.UTF8.GetBytes(json);

      var result = _importer.FromBytes(uncompressed);

      Assert.That(result.Success, Is.True);
      Assert.That(result.WasCompressed, Is.False);
      Assert.That(result.Package!.PackageId, Is.EqualTo(package.PackageId));
    }

    [Test]
    public void FromBytes_NullData_ReturnsFailed()
    {
      var result = _importer.FromBytes(null!);

      Assert.That(result.Success, Is.False);
    }

    [Test]
    public void FromBytes_EmptyData_ReturnsFailed()
    {
      var result = _importer.FromBytes(Array.Empty<byte>());

      Assert.That(result.Success, Is.False);
    }

    #endregion

    #region ToBytes Tests

    [Test]
    public void ToBytes_WithoutCompression_ReturnsUtf8Json()
    {
      var package = CreateTestPackage(3);
      var options = new ExportOptions { UseCompression = false };

      var bytes = _exporter.ToBytes(package, options);
      var json = Encoding.UTF8.GetString(bytes);

      Assert.That(DebugPackageExporter.IsCompressed(bytes), Is.False);
      Assert.That(json, Does.StartWith("{"));
      Assert.That(json, Does.Contain("PackageId"));
    }

    [Test]
    public void ToBytes_WithCompression_ReturnsCompressed()
    {
      var package = CreateTestPackage(3);
      var options = new ExportOptions { UseCompression = true };

      var bytes = _exporter.ToBytes(package, options);

      Assert.That(DebugPackageExporter.IsCompressed(bytes), Is.True);
    }

    [Test]
    public void ToBytes_NullOptions_ReturnsUncompressed()
    {
      var package = CreateTestPackage(3);

      var bytes = _exporter.ToBytes(package, null);

      Assert.That(DebugPackageExporter.IsCompressed(bytes), Is.False);
    }

    #endregion

    #region Round-Trip Tests

    [Test]
    public void RoundTrip_Compressed_PreservesAllData()
    {
      var original = CreateTestPackage(10);
      original.CreatorNotes = "Test notes with special chars: <>&\"'";
      original.ComputeIntegrityHash();

      var compressed = _exporter.ToCompressedBytes(original);
      var result = _importer.FromCompressedBytes(compressed, validateIntegrity: true);

      Assert.That(result.Success, Is.True);
      Assert.That(result.IntegrityValid, Is.True);
      Assert.That(result.Package!.PackageId, Is.EqualTo(original.PackageId));
      Assert.That(result.Package.CreatorNotes, Is.EqualTo(original.CreatorNotes));
      Assert.That(result.Package.Records.Count, Is.EqualTo(original.Records.Count));
      Assert.That(result.Package.PackageIntegrityHash, Is.EqualTo(original.PackageIntegrityHash));
    }

    [Test]
    public void RoundTrip_LargePackage_CompressesWell()
    {
      var recorder = new AuditRecorder(100);
      for (int i = 0; i < 100; i++)
      {
        recorder.Record(new AuditRecordBuilder()
          .WithNpcId($"npc-{i % 5}")
          .WithInteractionCount(i)
          .WithPlayerInput($"This is a test input message number {i} with some repeated content.")
          .WithOutput($"This is a test output response number {i} with similar repeated content.", $"Dialogue {i}")
          .WithStateHashes($"memory-hash-{i}", $"prompt-hash-{i}", $"constraints-hash-{i}")
          .WithConstraints($"{{\"constraint_{i}\": \"value_{i}\"}}")
          .Build());
      }

      var exporter = new DebugPackageExporter();
      var package = exporter.Export(recorder);
      var json = exporter.ToJson(package);
      var uncompressedSize = Encoding.UTF8.GetBytes(json).Length;
      var compressed = exporter.ToCompressedBytes(package);

      // Should achieve at least 50% compression on repetitive data
      var compressionRatio = (double)uncompressedSize / compressed.Length;
      Assert.That(compressionRatio, Is.GreaterThan(2.0),
        $"Expected at least 2x compression, got {compressionRatio:F2}x");

      // Verify round-trip
      var result = _importer.FromCompressedBytes(compressed);
      Assert.That(result.Success, Is.True);
      Assert.That(result.Package!.Records.Count, Is.EqualTo(100));
    }

    #endregion

    #region ExportOptions Integration Tests

    [Test]
    public void ExportToCompressedBytes_UsesOptionsCompressionLevel()
    {
      var recorder = new AuditRecorder();
      recorder.Record(new AuditRecordBuilder()
        .WithNpcId("test")
        .WithPlayerInput("Hello")
        .WithOutput("World", "World")
        .Build());

      var options = new ExportOptions
      {
        UseCompression = true,
        CompressionLevel = 9
      };

      var compressed = _exporter.ExportToCompressedBytes(recorder, options);

      Assert.That(DebugPackageExporter.IsCompressed(compressed), Is.True);
    }

    #endregion

    #region Helper Methods

    private DebugPackage CreateTestPackage(int recordCount)
    {
      var recorder = new AuditRecorder(recordCount + 1);

      for (int i = 0; i < recordCount; i++)
      {
        recorder.Record(new AuditRecordBuilder()
          .WithNpcId($"npc-{i % 3}")
          .WithInteractionCount(i + 1)
          .WithSeed(1000 + i)
          .WithPlayerInput($"Test input {i}")
          .WithOutput($"Test output {i}", $"Dialogue {i}")
          .WithStateHashes($"mem-{i}", $"prompt-{i}", $"constraints-{i}")
          .WithValidationOutcome(true, 0, 0)
          .Build());
      }

      return _exporter.Export(recorder);
    }

    #endregion
  }
}
