using System;
using System.Diagnostics;
using NUnit.Framework;
using Newtonsoft.Json;
using LlamaBrain.Core.Audit;

namespace LlamaBrain.Tests.Audit
{
  /// <summary>
  /// Tests for DebugPackageExporter.
  /// </summary>
  [TestFixture]
  [Category("Audit")]
  public class DebugPackageExporterTests
  {
    #region Export from Recorder Tests

    [Test]
    public void Export_EmptyRecorder_CreatesValidPackage()
    {
      // Arrange
      var recorder = new AuditRecorder();
      var exporter = new DebugPackageExporter();

      // Act
      var package = exporter.Export(recorder);

      // Assert
      Assert.That(package, Is.Not.Null);
      Assert.That(package.Version, Is.EqualTo(DebugPackage.CurrentVersion));
      Assert.That(package.PackageId, Is.Not.Empty);
      Assert.That(package.Records, Is.Empty);
      Assert.That(package.TotalInteractions, Is.EqualTo(0));
    }

    [Test]
    public void Export_WithRecords_IncludesAllRecords()
    {
      // Arrange
      var recorder = new AuditRecorder();
      recorder.Record(CreateRecord("npc-1", "input-1"));
      recorder.Record(CreateRecord("npc-1", "input-2"));
      recorder.Record(CreateRecord("npc-2", "input-3"));

      var exporter = new DebugPackageExporter();

      // Act
      var package = exporter.Export(recorder);

      // Assert
      Assert.That(package.Records, Has.Count.EqualTo(3));
      Assert.That(package.NpcIds, Has.Count.EqualTo(2));
      Assert.That(package.TotalInteractions, Is.EqualTo(3));
    }

    [Test]
    public void Export_WithModelFingerprint_IncludesFingerprint()
    {
      // Arrange
      var recorder = new AuditRecorder();
      var fingerprint = new ModelFingerprint
      {
        ModelFileName = "test-model.gguf",
        ModelFileSizeBytes = 1234567890L
      };
      fingerprint.ComputeFingerprintHash();

      var exporter = new DebugPackageExporter();

      // Act
      var package = exporter.Export(recorder, fingerprint);

      // Assert
      Assert.That(package.ModelFingerprint.ModelFileName, Is.EqualTo("test-model.gguf"));
      Assert.That(package.ModelFingerprint.ModelFileSizeBytes, Is.EqualTo(1234567890L));
    }

    [Test]
    public void Export_WithOptions_IncludesMetadata()
    {
      // Arrange
      var recorder = new AuditRecorder();
      var options = new ExportOptions
      {
        GameVersion = "1.0.0",
        SceneName = "TestScene",
        CreatorNotes = "Bug report: NPC said something wrong"
      };

      var exporter = new DebugPackageExporter();

      // Act
      var package = exporter.Export(recorder, options: options);

      // Assert
      Assert.That(package.GameVersion, Is.EqualTo("1.0.0"));
      Assert.That(package.SceneName, Is.EqualTo("TestScene"));
      Assert.That(package.CreatorNotes, Is.EqualTo("Bug report: NPC said something wrong"));
    }

    [Test]
    public void Export_SetsCreatedAtUtcTicks()
    {
      // Arrange
      var recorder = new AuditRecorder();
      var exporter = new DebugPackageExporter();
      var beforeTicks = DateTimeOffset.UtcNow.UtcTicks;

      // Act
      var package = exporter.Export(recorder);
      var afterTicks = DateTimeOffset.UtcNow.UtcTicks;

      // Assert
      Assert.That(package.CreatedAtUtcTicks, Is.GreaterThanOrEqualTo(beforeTicks));
      Assert.That(package.CreatedAtUtcTicks, Is.LessThanOrEqualTo(afterTicks));
    }

    [Test]
    public void Export_ComputesIntegrityHash()
    {
      // Arrange
      var recorder = new AuditRecorder();
      recorder.Record(CreateRecord("npc-1", "test"));
      var exporter = new DebugPackageExporter();

      // Act
      var package = exporter.Export(recorder);

      // Assert
      Assert.That(package.PackageIntegrityHash, Is.Not.Empty);
      Assert.That(package.ValidateIntegrity(), Is.True);
    }

    [Test]
    public void Export_UpdatesStatistics()
    {
      // Arrange
      var recorder = new AuditRecorder();
      recorder.Record(new AuditRecord { NpcId = "npc-1", ValidationPassed = true });
      recorder.Record(new AuditRecord { NpcId = "npc-1", ValidationPassed = false });
      recorder.Record(new AuditRecord { NpcId = "npc-1", ValidationPassed = true, FallbackUsed = true });

      var exporter = new DebugPackageExporter();

      // Act
      var package = exporter.Export(recorder);

      // Assert
      Assert.That(package.TotalInteractions, Is.EqualTo(3));
      Assert.That(package.ValidationFailures, Is.EqualTo(1));
      Assert.That(package.FallbacksUsed, Is.EqualTo(1));
    }

    #endregion

    #region Export for Single NPC Tests

    [Test]
    public void ExportNpc_ExtractsOnlySpecifiedNpc()
    {
      // Arrange
      var recorder = new AuditRecorder();
      recorder.Record(CreateRecord("npc-1", "input-1"));
      recorder.Record(CreateRecord("npc-2", "input-2"));
      recorder.Record(CreateRecord("npc-1", "input-3"));

      var exporter = new DebugPackageExporter();

      // Act
      var package = exporter.ExportNpc(recorder, "npc-1");

      // Assert
      Assert.That(package.Records, Has.Count.EqualTo(2));
      Assert.That(package.NpcIds, Has.Count.EqualTo(1));
      Assert.That(package.NpcIds[0], Is.EqualTo("npc-1"));
    }

    [Test]
    public void ExportNpc_UnknownNpc_CreatesEmptyPackage()
    {
      // Arrange
      var recorder = new AuditRecorder();
      recorder.Record(CreateRecord("npc-1", "input-1"));

      var exporter = new DebugPackageExporter();

      // Act
      var package = exporter.ExportNpc(recorder, "unknown-npc");

      // Assert
      Assert.That(package.Records, Is.Empty);
      Assert.That(package.TotalInteractions, Is.EqualTo(0));
    }

    #endregion

    #region ToJson Tests

    [Test]
    public void ToJson_ProducesValidJson()
    {
      // Arrange
      var recorder = new AuditRecorder();
      recorder.Record(CreateRecord("npc-1", "test"));
      var exporter = new DebugPackageExporter();
      var package = exporter.Export(recorder);

      // Act
      var json = exporter.ToJson(package);

      // Assert
      Assert.That(json, Is.Not.Empty);
      Assert.DoesNotThrow(() => JsonConvert.DeserializeObject<DebugPackage>(json));
    }

    [Test]
    public void ToJson_WithIndentation_ProducesFormattedJson()
    {
      // Arrange
      var recorder = new AuditRecorder();
      recorder.Record(CreateRecord("npc-1", "test"));
      var exporter = new DebugPackageExporter();
      var package = exporter.Export(recorder);

      // Act
      var json = exporter.ToJson(package, indented: true);

      // Assert
      Assert.That(json, Does.Contain("\n")); // Has newlines
      Assert.That(json, Does.Contain("  ")); // Has indentation
    }

    [Test]
    public void ToJson_WithoutIndentation_ProducesCompactJson()
    {
      // Arrange
      var recorder = new AuditRecorder();
      recorder.Record(CreateRecord("npc-1", "test"));
      var exporter = new DebugPackageExporter();
      var package = exporter.Export(recorder);

      // Act
      var json = exporter.ToJson(package, indented: false);

      // Assert
      Assert.That(json, Does.Not.Contain("\n")); // No newlines
    }

    #endregion

    #region Performance Tests

    [Test]
    [Category("Performance")]
    public void Export_50Records_Under100ms()
    {
      // Arrange
      var recorder = new AuditRecorder(50);
      for (int i = 0; i < 50; i++)
        recorder.Record(CreateRecord("npc-1", $"input-{i}", generateLargeOutput: true));

      var exporter = new DebugPackageExporter();

      // Act
      var sw = Stopwatch.StartNew();
      var package = exporter.Export(recorder);
      var json = exporter.ToJson(package);
      sw.Stop();

      // Assert
      Assert.That(sw.ElapsedMilliseconds, Is.LessThan(100),
        $"Export took {sw.ElapsedMilliseconds}ms, expected < 100ms");
      Assert.That(package.Records, Has.Count.EqualTo(50));
      Assert.That(json.Length, Is.GreaterThan(0));
    }

    [Test]
    [Category("Performance")]
    public void Export_50Records_Under10MB()
    {
      // Arrange
      var recorder = new AuditRecorder(50);
      for (int i = 0; i < 50; i++)
        recorder.Record(CreateRecord("npc-1", $"input-{i}", generateLargeOutput: true));

      var exporter = new DebugPackageExporter();
      var package = exporter.Export(recorder);

      // Act
      var json = exporter.ToJson(package);
      var sizeBytes = System.Text.Encoding.UTF8.GetByteCount(json);
      var sizeMB = sizeBytes / (1024.0 * 1024.0);

      // Assert
      Assert.That(sizeMB, Is.LessThan(10.0),
        $"Package size is {sizeMB:F2}MB, expected < 10MB");
    }

    #endregion

    #region Helper Methods

    private static AuditRecord CreateRecord(string npcId, string playerInput, bool generateLargeOutput = false)
    {
      var output = generateLargeOutput
        ? new string('x', 1000) // 1KB of output to simulate realistic response
        : "Test output";

      return new AuditRecordBuilder()
        .WithNpcId(npcId)
        .WithPlayerInput(playerInput)
        .WithInteractionCount(1)
        .WithSeed(42)
        .WithOutput(output, output)
        .WithValidationOutcome(true, 0, 0)
        .Build();
    }

    #endregion
  }
}
