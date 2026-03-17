using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Newtonsoft.Json;
using LlamaBrain.Core.Audit;

namespace LlamaBrain.Tests.Audit
{
  /// <summary>
  /// Tests for DebugPackageImporter.
  /// </summary>
  [TestFixture]
  [Category("Audit")]
  public class DebugPackageImporterTests
  {
    private DebugPackageExporter _exporter = null!;
    private DebugPackageImporter _importer = null!;

    [SetUp]
    public void SetUp()
    {
      _exporter = new DebugPackageExporter();
      _importer = new DebugPackageImporter();
    }

    #region FromJson Tests

    [Test]
    public void FromJson_ValidJson_ReturnsPackage()
    {
      // Arrange
      var original = CreateTestPackage();
      var json = _exporter.ToJson(original);

      // Act
      var result = _importer.FromJson(json);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.Package, Is.Not.Null);
      Assert.That(result.Package!.PackageId, Is.EqualTo(original.PackageId));
    }

    [Test]
    public void FromJson_InvalidJson_ReturnsFailure()
    {
      // Arrange
      var invalidJson = "{ invalid json }}}";

      // Act
      var result = _importer.FromJson(invalidJson);

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("Failed to parse"));
      Assert.That(result.Package, Is.Null);
    }

    [Test]
    public void FromJson_EmptyJson_ReturnsFailure()
    {
      // Act
      var result = _importer.FromJson("");

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("empty"));
    }

    [Test]
    public void FromJson_NullJson_ReturnsFailure()
    {
      // Act
      var result = _importer.FromJson(null!);

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("empty"));
    }

    [Test]
    public void FromJson_PreservesAllFields()
    {
      // Arrange
      var original = CreateTestPackage();
      original.CreatorNotes = "Test notes with unicode: 世界 🎮";
      original.Records.Add(new AuditRecord
      {
        RecordId = "rec-1",
        NpcId = "npc-1",
        PlayerInput = "Hello, NPC!",
        RawOutput = "Hello, Player!",
        OutputHash = "hash123",
        ValidationPassed = true,
        Seed = 42
      });
      original.UpdateStatistics();
      original.ComputeIntegrityHash();

      var json = _exporter.ToJson(original);

      // Act
      var result = _importer.FromJson(json);

      // Assert
      Assert.That(result.Success, Is.True);
      var package = result.Package!;
      Assert.That(package.Version, Is.EqualTo(original.Version));
      Assert.That(package.PackageId, Is.EqualTo(original.PackageId));
      Assert.That(package.CreatorNotes, Is.EqualTo(original.CreatorNotes));
      Assert.That(package.Records, Has.Count.EqualTo(1));
      Assert.That(package.Records[0].PlayerInput, Is.EqualTo("Hello, NPC!"));
      Assert.That(package.TotalInteractions, Is.EqualTo(1));
    }

    #endregion

    #region Integrity Validation Tests

    [Test]
    public void FromJson_ValidIntegrity_ReturnsSuccess()
    {
      // Arrange
      var original = CreateTestPackage();
      original.ComputeIntegrityHash();
      var json = _exporter.ToJson(original);

      // Act
      var result = _importer.FromJson(json, validateIntegrity: true);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.IntegrityValid, Is.True);
    }

    [Test]
    public void FromJson_TamperedPackage_ReturnsIntegrityFailure()
    {
      // Arrange
      var original = CreateTestPackage();
      original.ComputeIntegrityHash();
      var json = _exporter.ToJson(original);

      // Tamper with JSON
      json = json.Replace(original.PackageId, "tampered-id");

      // Act
      var result = _importer.FromJson(json, validateIntegrity: true);

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.IntegrityValid, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("integrity"));
    }

    [Test]
    public void FromJson_SkipIntegrityValidation_IgnoresTampering()
    {
      // Arrange
      var original = CreateTestPackage();
      original.ComputeIntegrityHash();
      var json = _exporter.ToJson(original);

      // Tamper with JSON
      json = json.Replace(original.PackageId, "tampered-id");

      // Act
      var result = _importer.FromJson(json, validateIntegrity: false);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.IntegrityValid, Is.Null); // Not checked
    }

    [Test]
    public void FromJson_NoIntegrityHash_FailsValidation()
    {
      // Arrange
      var original = CreateTestPackage();
      // Don't compute integrity hash
      var json = _exporter.ToJson(original);

      // Act
      var result = _importer.FromJson(json, validateIntegrity: true);

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.IntegrityValid, Is.False);
    }

    #endregion

    #region Version Validation Tests

    [Test]
    public void FromJson_CurrentVersion_ReturnsSuccess()
    {
      // Arrange
      var original = CreateTestPackage();
      original.Version = DebugPackage.CurrentVersion;
      var json = _exporter.ToJson(original);

      // Act
      var result = _importer.FromJson(json);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.VersionSupported, Is.True);
    }

    [Test]
    public void FromJson_FutureVersion_ReturnsWarning()
    {
      // Arrange
      var original = CreateTestPackage();
      original.Version = DebugPackage.CurrentVersion + 1;
      var json = _exporter.ToJson(original);

      // Act
      var result = _importer.FromJson(json);

      // Assert
      Assert.That(result.Success, Is.True); // Still succeeds
      Assert.That(result.VersionSupported, Is.False);
      Assert.That(result.Warnings, Has.Some.Contains("version"));
    }

    #endregion

    #region Model Fingerprint Validation Tests

    [Test]
    public void ValidateModelFingerprint_ExactMatch_ReturnsMatch()
    {
      // Arrange
      var fingerprint = new ModelFingerprint
      {
        ModelFileName = "model.gguf",
        ModelFileSizeBytes = 1234567890L
      };
      fingerprint.ComputeFingerprintHash();

      var package = CreateTestPackage();
      package.ModelFingerprint = fingerprint;

      // Act
      var result = _importer.ValidateModelFingerprint(package, fingerprint);

      // Assert
      Assert.That(result.IsExactMatch, Is.True);
      Assert.That(result.IsCompatible, Is.True);
    }

    [Test]
    public void ValidateModelFingerprint_Compatible_ReturnsCompatible()
    {
      // Arrange
      var packageFingerprint = new ModelFingerprint
      {
        ModelFileName = "model.gguf",
        ModelFileSizeBytes = 1234567890L,
        ContextLength = 4096
      };
      packageFingerprint.ComputeFingerprintHash();

      var currentFingerprint = new ModelFingerprint
      {
        ModelFileName = "model.gguf",
        ModelFileSizeBytes = 1234567890L,
        ContextLength = 8192 // Different config
      };
      currentFingerprint.ComputeFingerprintHash();

      var package = CreateTestPackage();
      package.ModelFingerprint = packageFingerprint;

      // Act
      var result = _importer.ValidateModelFingerprint(package, currentFingerprint);

      // Assert
      Assert.That(result.IsExactMatch, Is.False);
      Assert.That(result.IsCompatible, Is.True);
    }

    [Test]
    public void ValidateModelFingerprint_Incompatible_ReturnsIncompatible()
    {
      // Arrange
      var packageFingerprint = new ModelFingerprint
      {
        ModelFileName = "model-a.gguf",
        ModelFileSizeBytes = 1234567890L
      };
      packageFingerprint.ComputeFingerprintHash();

      var currentFingerprint = new ModelFingerprint
      {
        ModelFileName = "model-b.gguf", // Different model
        ModelFileSizeBytes = 9876543210L
      };
      currentFingerprint.ComputeFingerprintHash();

      var package = CreateTestPackage();
      package.ModelFingerprint = packageFingerprint;

      // Act
      var result = _importer.ValidateModelFingerprint(package, currentFingerprint);

      // Assert
      Assert.That(result.IsExactMatch, Is.False);
      Assert.That(result.IsCompatible, Is.False);
      Assert.That(result.MismatchDescription, Does.Contain("mismatch"));
    }

    #endregion

    #region Performance Tests

    [Test]
    [Category("Performance")]
    public void FromJson_50Records_Under500ms()
    {
      // Arrange
      var recorder = new AuditRecorder(50);
      for (int i = 0; i < 50; i++)
      {
        recorder.Record(new AuditRecordBuilder()
          .WithNpcId("npc-1")
          .WithPlayerInput($"Input {i}")
          .WithOutput(new string('x', 1000), "Dialogue")
          .Build());
      }

      var package = _exporter.Export(recorder);
      var json = _exporter.ToJson(package);

      // Act
      var sw = Stopwatch.StartNew();
      var result = _importer.FromJson(json, validateIntegrity: true);
      sw.Stop();

      // Assert
      Assert.That(sw.ElapsedMilliseconds, Is.LessThan(500),
        $"Import took {sw.ElapsedMilliseconds}ms, expected < 500ms");
      Assert.That(result.Success, Is.True);
      Assert.That(result.Package!.Records, Has.Count.EqualTo(50));
    }

    #endregion

    #region Round-Trip Tests

    [Test]
    public void RoundTrip_ExportImport_PreservesData()
    {
      // Arrange
      var recorder = new AuditRecorder();
      recorder.Record(new AuditRecordBuilder()
        .WithNpcId("npc-1")
        .WithInteractionCount(1)
        .WithSeed(12345)
        .WithPlayerInput("Hello!")
        .WithOutput("Hi there!", "Hi there!")
        .WithValidationOutcome(true, 0, 2)
        .Build());

      var fingerprint = new ModelFingerprint { ModelFileName = "test.gguf" };
      var options = new ExportOptions
      {
        GameVersion = "1.0.0",
        SceneName = "TestScene",
        CreatorNotes = "Round-trip test"
      };

      // Act
      var exported = _exporter.Export(recorder, fingerprint, options);
      var json = _exporter.ToJson(exported);
      var imported = _importer.FromJson(json, validateIntegrity: true);

      // Assert
      Assert.That(imported.Success, Is.True);
      Assert.That(imported.IntegrityValid, Is.True);

      var package = imported.Package!;
      Assert.That(package.GameVersion, Is.EqualTo("1.0.0"));
      Assert.That(package.SceneName, Is.EqualTo("TestScene"));
      Assert.That(package.CreatorNotes, Is.EqualTo("Round-trip test"));
      Assert.That(package.Records, Has.Count.EqualTo(1));

      var record = package.Records[0];
      Assert.That(record.NpcId, Is.EqualTo("npc-1"));
      Assert.That(record.Seed, Is.EqualTo(12345));
      Assert.That(record.PlayerInput, Is.EqualTo("Hello!"));
      Assert.That(record.DialogueText, Is.EqualTo("Hi there!"));
    }

    #endregion

    #region Helper Methods

    private static DebugPackage CreateTestPackage()
    {
      return new DebugPackage
      {
        PackageId = "test-package-" + Guid.NewGuid().ToString("N").Substring(0, 8),
        CreatedAtUtcTicks = DateTimeOffset.UtcNow.UtcTicks,
        GameVersion = "1.0.0",
        NpcIds = new List<string> { "npc-1" }
      };
    }

    #endregion
  }
}
