using System;
using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json;
using LlamaBrain.Core.Audit;

namespace LlamaBrain.Tests.Audit
{
  /// <summary>
  /// Tests for DebugPackage DTO and export functionality.
  /// </summary>
  [TestFixture]
  [Category("Audit")]
  public class DebugPackageTests
  {
    #region Constant Tests

    [Test]
    public void CurrentVersion_IsOne()
    {
      // Assert
      Assert.That(DebugPackage.CurrentVersion, Is.EqualTo(1));
    }

    #endregion

    #region Default Value Tests

    [Test]
    public void Constructor_DefaultValues_AllPropertiesInitialized()
    {
      // Act
      var package = new DebugPackage();

      // Assert
      Assert.That(package.Version, Is.EqualTo(DebugPackage.CurrentVersion));
      Assert.That(package.PackageId, Is.EqualTo(""));
      Assert.That(package.CreatedAtUtcTicks, Is.EqualTo(0L));
      Assert.That(package.CreatorNotes, Is.EqualTo(""));
      Assert.That(package.ModelFingerprint, Is.Not.Null);
      Assert.That(package.GameVersion, Is.EqualTo(""));
      Assert.That(package.SceneName, Is.EqualTo(""));
      Assert.That(package.NpcIds, Is.Not.Null);
      Assert.That(package.NpcIds, Is.Empty);
      Assert.That(package.Records, Is.Not.Null);
      Assert.That(package.Records, Is.Empty);
      Assert.That(package.TotalInteractions, Is.EqualTo(0));
      Assert.That(package.ValidationFailures, Is.EqualTo(0));
      Assert.That(package.FallbacksUsed, Is.EqualTo(0));
      Assert.That(package.PackageIntegrityHash, Is.EqualTo(""));
    }

    #endregion

    #region Property Assignment Tests

    [Test]
    public void Properties_CanBeSetAndRetrieved()
    {
      // Arrange
      var fingerprint = new ModelFingerprint { ModelFileName = "model.gguf" };
      var records = new List<AuditRecord>
      {
        new AuditRecord { RecordId = "rec-1" },
        new AuditRecord { RecordId = "rec-2" }
      };

      var package = new DebugPackage
      {
        Version = 1,
        PackageId = "pkg-123",
        CreatedAtUtcTicks = 638000000000000000L,
        CreatorNotes = "Test package",
        ModelFingerprint = fingerprint,
        GameVersion = "1.0.0",
        SceneName = "TestScene",
        NpcIds = new List<string> { "npc-1", "npc-2" },
        Records = records,
        TotalInteractions = 10,
        ValidationFailures = 2,
        FallbacksUsed = 1,
        PackageIntegrityHash = "abc123"
      };

      // Assert
      Assert.That(package.Version, Is.EqualTo(1));
      Assert.That(package.PackageId, Is.EqualTo("pkg-123"));
      Assert.That(package.CreatedAtUtcTicks, Is.EqualTo(638000000000000000L));
      Assert.That(package.CreatorNotes, Is.EqualTo("Test package"));
      Assert.That(package.ModelFingerprint.ModelFileName, Is.EqualTo("model.gguf"));
      Assert.That(package.GameVersion, Is.EqualTo("1.0.0"));
      Assert.That(package.SceneName, Is.EqualTo("TestScene"));
      Assert.That(package.NpcIds, Has.Count.EqualTo(2));
      Assert.That(package.Records, Has.Count.EqualTo(2));
      Assert.That(package.TotalInteractions, Is.EqualTo(10));
      Assert.That(package.ValidationFailures, Is.EqualTo(2));
      Assert.That(package.FallbacksUsed, Is.EqualTo(1));
      Assert.That(package.PackageIntegrityHash, Is.EqualTo("abc123"));
    }

    #endregion

    #region Serialization Tests

    [Test]
    public void Serialization_RoundTrip_PreservesAllProperties()
    {
      // Arrange
      var fingerprint = new ModelFingerprint
      {
        ModelFileName = "test-model.gguf",
        ModelFileSizeBytes = 1234567890L
      };

      var records = new List<AuditRecord>
      {
        new AuditRecord
        {
          RecordId = "rec-1",
          NpcId = "npc-1",
          PlayerInput = "Hello",
          RawOutput = "Hi there",
          OutputHash = "hash123"
        }
      };

      var original = new DebugPackage
      {
        Version = 1,
        PackageId = "test-pkg",
        CreatedAtUtcTicks = 638000000000000000L,
        CreatorNotes = "Serialization test",
        ModelFingerprint = fingerprint,
        GameVersion = "2.0.0",
        SceneName = "SerializationScene",
        NpcIds = new List<string> { "npc-1" },
        Records = records,
        TotalInteractions = 5,
        ValidationFailures = 1,
        FallbacksUsed = 0,
        PackageIntegrityHash = "integrity123"
      };

      // Act
      var json = JsonConvert.SerializeObject(original, Formatting.Indented);
      var deserialized = JsonConvert.DeserializeObject<DebugPackage>(json);

      // Assert
      Assert.That(deserialized, Is.Not.Null);
      Assert.That(deserialized!.Version, Is.EqualTo(original.Version));
      Assert.That(deserialized.PackageId, Is.EqualTo(original.PackageId));
      Assert.That(deserialized.CreatedAtUtcTicks, Is.EqualTo(original.CreatedAtUtcTicks));
      Assert.That(deserialized.CreatorNotes, Is.EqualTo(original.CreatorNotes));
      Assert.That(deserialized.ModelFingerprint.ModelFileName, Is.EqualTo(fingerprint.ModelFileName));
      Assert.That(deserialized.GameVersion, Is.EqualTo(original.GameVersion));
      Assert.That(deserialized.SceneName, Is.EqualTo(original.SceneName));
      Assert.That(deserialized.NpcIds, Has.Count.EqualTo(1));
      Assert.That(deserialized.Records, Has.Count.EqualTo(1));
      Assert.That(deserialized.Records[0].PlayerInput, Is.EqualTo("Hello"));
      Assert.That(deserialized.TotalInteractions, Is.EqualTo(original.TotalInteractions));
      Assert.That(deserialized.ValidationFailures, Is.EqualTo(original.ValidationFailures));
      Assert.That(deserialized.FallbacksUsed, Is.EqualTo(original.FallbacksUsed));
      Assert.That(deserialized.PackageIntegrityHash, Is.EqualTo(original.PackageIntegrityHash));
    }

    [Test]
    public void Serialization_EmptyPackage_ProducesValidJson()
    {
      // Arrange
      var package = new DebugPackage();

      // Act
      var json = JsonConvert.SerializeObject(package);

      // Assert
      Assert.That(json, Is.Not.Empty);
      Assert.DoesNotThrow(() => JsonConvert.DeserializeObject<DebugPackage>(json));
    }

    [Test]
    public void Serialization_WithUnicodeContent_PreservesContent()
    {
      // Arrange
      var package = new DebugPackage
      {
        CreatorNotes = "Notes with unicode: 世界 🎮",
        Records = new List<AuditRecord>
        {
          new AuditRecord { PlayerInput = "Hello, 世界!" }
        }
      };

      // Act
      var json = JsonConvert.SerializeObject(package);
      var deserialized = JsonConvert.DeserializeObject<DebugPackage>(json);

      // Assert
      Assert.That(deserialized!.CreatorNotes, Is.EqualTo(package.CreatorNotes));
      Assert.That(deserialized.Records[0].PlayerInput, Is.EqualTo("Hello, 世界!"));
    }

    #endregion

    #region ComputeIntegrityHash Tests

    [Test]
    public void ComputeIntegrityHash_SetsHashFromContent()
    {
      // Arrange
      var package = new DebugPackage
      {
        PackageId = "test-pkg",
        Records = new List<AuditRecord>
        {
          new AuditRecord { RecordId = "rec-1", OutputHash = "out1" },
          new AuditRecord { RecordId = "rec-2", OutputHash = "out2" }
        }
      };

      // Act
      package.ComputeIntegrityHash();

      // Assert
      Assert.That(package.PackageIntegrityHash, Is.Not.Empty);
    }

    [Test]
    public void ComputeIntegrityHash_SameContent_SameHash()
    {
      // Arrange
      var pkg1 = new DebugPackage
      {
        PackageId = "pkg-1",
        Records = new List<AuditRecord>
        {
          new AuditRecord { RecordId = "rec-1", OutputHash = "hash1" }
        }
      };
      var pkg2 = new DebugPackage
      {
        PackageId = "pkg-1",
        Records = new List<AuditRecord>
        {
          new AuditRecord { RecordId = "rec-1", OutputHash = "hash1" }
        }
      };

      // Act
      pkg1.ComputeIntegrityHash();
      pkg2.ComputeIntegrityHash();

      // Assert
      Assert.That(pkg1.PackageIntegrityHash, Is.EqualTo(pkg2.PackageIntegrityHash));
    }

    [Test]
    public void ComputeIntegrityHash_DifferentContent_DifferentHash()
    {
      // Arrange
      var pkg1 = new DebugPackage { PackageId = "pkg-1" };
      var pkg2 = new DebugPackage { PackageId = "pkg-2" };

      // Act
      pkg1.ComputeIntegrityHash();
      pkg2.ComputeIntegrityHash();

      // Assert
      Assert.That(pkg1.PackageIntegrityHash, Is.Not.EqualTo(pkg2.PackageIntegrityHash));
    }

    #endregion

    #region ValidateIntegrity Tests

    [Test]
    public void ValidateIntegrity_ValidHash_ReturnsTrue()
    {
      // Arrange
      var package = new DebugPackage { PackageId = "test-pkg" };
      package.ComputeIntegrityHash();

      // Act
      var isValid = package.ValidateIntegrity();

      // Assert
      Assert.That(isValid, Is.True);
    }

    [Test]
    public void ValidateIntegrity_TamperedContent_ReturnsFalse()
    {
      // Arrange
      var package = new DebugPackage { PackageId = "test-pkg" };
      package.ComputeIntegrityHash();

      // Tamper with content
      package.PackageId = "tampered-pkg";

      // Act
      var isValid = package.ValidateIntegrity();

      // Assert
      Assert.That(isValid, Is.False);
    }

    [Test]
    public void ValidateIntegrity_EmptyHash_ReturnsFalse()
    {
      // Arrange
      var package = new DebugPackage { PackageId = "test-pkg" };
      // Don't compute hash

      // Act
      var isValid = package.ValidateIntegrity();

      // Assert
      Assert.That(isValid, Is.False);
    }

    #endregion

    #region Statistics Calculation Tests

    [Test]
    public void UpdateStatistics_CalculatesCorrectCounts()
    {
      // Arrange
      var package = new DebugPackage
      {
        Records = new List<AuditRecord>
        {
          new AuditRecord { ValidationPassed = true, FallbackUsed = false },
          new AuditRecord { ValidationPassed = false, FallbackUsed = false },
          new AuditRecord { ValidationPassed = true, FallbackUsed = true },
          new AuditRecord { ValidationPassed = false, FallbackUsed = true },
          new AuditRecord { ValidationPassed = true, FallbackUsed = false }
        },
        NpcIds = new List<string> { "npc-1", "npc-2" }
      };

      // Act
      package.UpdateStatistics();

      // Assert
      Assert.That(package.TotalInteractions, Is.EqualTo(5));
      Assert.That(package.ValidationFailures, Is.EqualTo(2));
      Assert.That(package.FallbacksUsed, Is.EqualTo(2));
    }

    #endregion
  }
}
