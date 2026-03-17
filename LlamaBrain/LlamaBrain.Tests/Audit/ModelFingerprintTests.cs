using System;
using NUnit.Framework;
using Newtonsoft.Json;
using LlamaBrain.Core.Audit;

namespace LlamaBrain.Tests.Audit
{
  /// <summary>
  /// Tests for ModelFingerprint DTO and matching logic.
  /// </summary>
  [TestFixture]
  [Category("Audit")]
  public class ModelFingerprintTests
  {
    #region Default Value Tests

    [Test]
    public void Constructor_DefaultValues_AllPropertiesInitialized()
    {
      // Act
      var fingerprint = new ModelFingerprint();

      // Assert
      Assert.That(fingerprint.ModelPath, Is.EqualTo(""));
      Assert.That(fingerprint.ModelFileName, Is.EqualTo(""));
      Assert.That(fingerprint.ModelFileSizeBytes, Is.EqualTo(0L));
      Assert.That(fingerprint.ModelFileHashPrefix, Is.EqualTo(""));
      Assert.That(fingerprint.QuantizationType, Is.EqualTo(""));
      Assert.That(fingerprint.ContextLength, Is.EqualTo(0));
      Assert.That(fingerprint.GpuLayers, Is.EqualTo(0));
      Assert.That(fingerprint.BatchSize, Is.EqualTo(0));
      Assert.That(fingerprint.FingerprintHash, Is.EqualTo(""));
    }

    #endregion

    #region Property Assignment Tests

    [Test]
    public void Properties_CanBeSetAndRetrieved()
    {
      // Arrange
      var fingerprint = new ModelFingerprint
      {
        ModelPath = "/models/phi-3-mini.Q4_K_M.gguf",
        ModelFileName = "phi-3-mini.Q4_K_M.gguf",
        ModelFileSizeBytes = 2345678901L,
        ModelFileHashPrefix = "a1b2c3d4",
        QuantizationType = "Q4_K_M",
        ContextLength = 4096,
        GpuLayers = 35,
        BatchSize = 512,
        FingerprintHash = "abc123"
      };

      // Assert
      Assert.That(fingerprint.ModelPath, Is.EqualTo("/models/phi-3-mini.Q4_K_M.gguf"));
      Assert.That(fingerprint.ModelFileName, Is.EqualTo("phi-3-mini.Q4_K_M.gguf"));
      Assert.That(fingerprint.ModelFileSizeBytes, Is.EqualTo(2345678901L));
      Assert.That(fingerprint.ModelFileHashPrefix, Is.EqualTo("a1b2c3d4"));
      Assert.That(fingerprint.QuantizationType, Is.EqualTo("Q4_K_M"));
      Assert.That(fingerprint.ContextLength, Is.EqualTo(4096));
      Assert.That(fingerprint.GpuLayers, Is.EqualTo(35));
      Assert.That(fingerprint.BatchSize, Is.EqualTo(512));
      Assert.That(fingerprint.FingerprintHash, Is.EqualTo("abc123"));
    }

    #endregion

    #region Matches Tests

    [Test]
    public void Matches_SameFingerprintHash_ReturnsTrue()
    {
      // Arrange
      var fp1 = new ModelFingerprint { FingerprintHash = "abc123" };
      var fp2 = new ModelFingerprint { FingerprintHash = "abc123" };

      // Act & Assert
      Assert.That(fp1.Matches(fp2), Is.True);
      Assert.That(fp2.Matches(fp1), Is.True);
    }

    [Test]
    public void Matches_DifferentFingerprintHash_ReturnsFalse()
    {
      // Arrange
      var fp1 = new ModelFingerprint { FingerprintHash = "abc123" };
      var fp2 = new ModelFingerprint { FingerprintHash = "xyz789" };

      // Act & Assert
      Assert.That(fp1.Matches(fp2), Is.False);
    }

    [Test]
    public void Matches_EmptyHashes_ReturnsTrue()
    {
      // Arrange
      var fp1 = new ModelFingerprint();
      var fp2 = new ModelFingerprint();

      // Act & Assert
      Assert.That(fp1.Matches(fp2), Is.True);
    }

    #endregion

    #region IsCompatibleWith Tests

    [Test]
    public void IsCompatibleWith_SameFileNameAndSize_ReturnsTrue()
    {
      // Arrange
      var fp1 = new ModelFingerprint
      {
        ModelFileName = "model.gguf",
        ModelFileSizeBytes = 1234567890L,
        ContextLength = 4096 // Different config
      };
      var fp2 = new ModelFingerprint
      {
        ModelFileName = "model.gguf",
        ModelFileSizeBytes = 1234567890L,
        ContextLength = 8192 // Different config
      };

      // Act & Assert - Same model file, different server config is compatible
      Assert.That(fp1.IsCompatibleWith(fp2), Is.True);
    }

    [Test]
    public void IsCompatibleWith_DifferentFileName_ReturnsFalse()
    {
      // Arrange
      var fp1 = new ModelFingerprint { ModelFileName = "model-a.gguf", ModelFileSizeBytes = 1000 };
      var fp2 = new ModelFingerprint { ModelFileName = "model-b.gguf", ModelFileSizeBytes = 1000 };

      // Act & Assert
      Assert.That(fp1.IsCompatibleWith(fp2), Is.False);
    }

    [Test]
    public void IsCompatibleWith_DifferentFileSize_ReturnsFalse()
    {
      // Arrange
      var fp1 = new ModelFingerprint { ModelFileName = "model.gguf", ModelFileSizeBytes = 1000 };
      var fp2 = new ModelFingerprint { ModelFileName = "model.gguf", ModelFileSizeBytes = 2000 };

      // Act & Assert
      Assert.That(fp1.IsCompatibleWith(fp2), Is.False);
    }

    #endregion

    #region Serialization Tests

    [Test]
    public void Serialization_RoundTrip_PreservesAllProperties()
    {
      // Arrange
      var original = new ModelFingerprint
      {
        ModelPath = "/path/to/model.gguf",
        ModelFileName = "model.gguf",
        ModelFileSizeBytes = 9876543210L,
        ModelFileHashPrefix = "deadbeef",
        QuantizationType = "Q5_K_S",
        ContextLength = 8192,
        GpuLayers = 40,
        BatchSize = 256,
        FingerprintHash = "fingerprint123"
      };

      // Act
      var json = JsonConvert.SerializeObject(original);
      var deserialized = JsonConvert.DeserializeObject<ModelFingerprint>(json);

      // Assert
      Assert.That(deserialized, Is.Not.Null);
      Assert.That(deserialized!.ModelPath, Is.EqualTo(original.ModelPath));
      Assert.That(deserialized.ModelFileName, Is.EqualTo(original.ModelFileName));
      Assert.That(deserialized.ModelFileSizeBytes, Is.EqualTo(original.ModelFileSizeBytes));
      Assert.That(deserialized.ModelFileHashPrefix, Is.EqualTo(original.ModelFileHashPrefix));
      Assert.That(deserialized.QuantizationType, Is.EqualTo(original.QuantizationType));
      Assert.That(deserialized.ContextLength, Is.EqualTo(original.ContextLength));
      Assert.That(deserialized.GpuLayers, Is.EqualTo(original.GpuLayers));
      Assert.That(deserialized.BatchSize, Is.EqualTo(original.BatchSize));
      Assert.That(deserialized.FingerprintHash, Is.EqualTo(original.FingerprintHash));
    }

    #endregion

    #region ComputeFingerprintHash Tests

    [Test]
    public void ComputeFingerprintHash_SetsHashFromProperties()
    {
      // Arrange
      var fingerprint = new ModelFingerprint
      {
        ModelFileName = "model.gguf",
        ModelFileSizeBytes = 1234567890L,
        QuantizationType = "Q4_K_M",
        ContextLength = 4096
      };

      // Act
      fingerprint.ComputeFingerprintHash();

      // Assert
      Assert.That(fingerprint.FingerprintHash, Is.Not.Empty);
    }

    [Test]
    public void ComputeFingerprintHash_SameProperties_SameHash()
    {
      // Arrange
      var fp1 = new ModelFingerprint
      {
        ModelFileName = "model.gguf",
        ModelFileSizeBytes = 1234567890L,
        QuantizationType = "Q4_K_M",
        ContextLength = 4096
      };
      var fp2 = new ModelFingerprint
      {
        ModelFileName = "model.gguf",
        ModelFileSizeBytes = 1234567890L,
        QuantizationType = "Q4_K_M",
        ContextLength = 4096
      };

      // Act
      fp1.ComputeFingerprintHash();
      fp2.ComputeFingerprintHash();

      // Assert
      Assert.That(fp1.FingerprintHash, Is.EqualTo(fp2.FingerprintHash));
    }

    [Test]
    public void ComputeFingerprintHash_DifferentProperties_DifferentHash()
    {
      // Arrange
      var fp1 = new ModelFingerprint { ModelFileName = "model-a.gguf" };
      var fp2 = new ModelFingerprint { ModelFileName = "model-b.gguf" };

      // Act
      fp1.ComputeFingerprintHash();
      fp2.ComputeFingerprintHash();

      // Assert
      Assert.That(fp1.FingerprintHash, Is.Not.EqualTo(fp2.FingerprintHash));
    }

    #endregion
  }
}
