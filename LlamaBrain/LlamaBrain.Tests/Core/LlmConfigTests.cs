using NUnit.Framework;
using LlamaBrain.Core;

namespace LlamaBrain.Tests.Core
{
  /// <summary>
  /// Tests for LlmConfig configuration class.
  /// </summary>
  public class LlmConfigTests
  {
    #region Default Values Tests

    [Test]
    public void LlmConfig_DefaultValues_AreCorrect()
    {
      // Arrange & Act
      var config = new LlmConfig();

      // Assert
      Assert.That(config.MaxTokens, Is.EqualTo(32));
      Assert.That(config.Temperature, Is.EqualTo(0.7f));
      Assert.That(config.TopP, Is.EqualTo(0.9f));
      Assert.That(config.TopK, Is.EqualTo(40));
      Assert.That(config.RepeatPenalty, Is.EqualTo(1.1f));
      Assert.That(config.Seed, Is.Null);
      Assert.That(config.StopSequences, Is.Not.Null);
      Assert.That(config.StopSequences.Length, Is.GreaterThan(0));
    }

    #endregion

    #region MaxTokens Tests

    [Test]
    public void MaxTokens_ValidValue_SetsCorrectly()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.MaxTokens = 100;

      // Assert
      Assert.That(config.MaxTokens, Is.EqualTo(100));
    }

    [Test]
    public void MaxTokens_BelowMinimum_ClampsToMinimum()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.MaxTokens = 0;

      // Assert
      Assert.That(config.MaxTokens, Is.EqualTo(1));
    }

    [Test]
    public void MaxTokens_NegativeValue_ClampsToMinimum()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.MaxTokens = -10;

      // Assert
      Assert.That(config.MaxTokens, Is.EqualTo(1));
    }

    [Test]
    public void MaxTokens_AboveMaximum_ClampsToMaximum()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.MaxTokens = 3000;

      // Assert
      Assert.That(config.MaxTokens, Is.EqualTo(2048));
    }

    [Test]
    public void MaxTokens_AtMinimumBoundary_AcceptsValue()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.MaxTokens = 1;

      // Assert
      Assert.That(config.MaxTokens, Is.EqualTo(1));
    }

    [Test]
    public void MaxTokens_AtMaximumBoundary_AcceptsValue()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.MaxTokens = 2048;

      // Assert
      Assert.That(config.MaxTokens, Is.EqualTo(2048));
    }

    #endregion

    #region Temperature Tests

    [Test]
    public void Temperature_ValidValue_SetsCorrectly()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.Temperature = 1.0f;

      // Assert
      Assert.That(config.Temperature, Is.EqualTo(1.0f));
    }

    [Test]
    public void Temperature_BelowMinimum_ClampsToMinimum()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.Temperature = -1.0f;

      // Assert
      Assert.That(config.Temperature, Is.EqualTo(0.0f));
    }

    [Test]
    public void Temperature_AboveMaximum_ClampsToMaximum()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.Temperature = 3.0f;

      // Assert
      Assert.That(config.Temperature, Is.EqualTo(2.0f));
    }

    [Test]
    public void Temperature_AtMinimumBoundary_AcceptsValue()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.Temperature = 0.0f;

      // Assert
      Assert.That(config.Temperature, Is.EqualTo(0.0f));
    }

    [Test]
    public void Temperature_AtMaximumBoundary_AcceptsValue()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.Temperature = 2.0f;

      // Assert
      Assert.That(config.Temperature, Is.EqualTo(2.0f));
    }

    #endregion

    #region TopP Tests

    [Test]
    public void TopP_ValidValue_SetsCorrectly()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.TopP = 0.5f;

      // Assert
      Assert.That(config.TopP, Is.EqualTo(0.5f));
    }

    [Test]
    public void TopP_BelowMinimum_ClampsToMinimum()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.TopP = -0.5f;

      // Assert
      Assert.That(config.TopP, Is.EqualTo(0.0f));
    }

    [Test]
    public void TopP_AboveMaximum_ClampsToMaximum()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.TopP = 1.5f;

      // Assert
      Assert.That(config.TopP, Is.EqualTo(1.0f));
    }

    [Test]
    public void TopP_AtMinimumBoundary_AcceptsValue()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.TopP = 0.0f;

      // Assert
      Assert.That(config.TopP, Is.EqualTo(0.0f));
    }

    [Test]
    public void TopP_AtMaximumBoundary_AcceptsValue()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.TopP = 1.0f;

      // Assert
      Assert.That(config.TopP, Is.EqualTo(1.0f));
    }

    #endregion

    #region TopK Tests

    [Test]
    public void TopK_ValidValue_SetsCorrectly()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.TopK = 50;

      // Assert
      Assert.That(config.TopK, Is.EqualTo(50));
    }

    [Test]
    public void TopK_BelowMinimum_ClampsToMinimum()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.TopK = 0;

      // Assert
      Assert.That(config.TopK, Is.EqualTo(1));
    }

    [Test]
    public void TopK_NegativeValue_ClampsToMinimum()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.TopK = -5;

      // Assert
      Assert.That(config.TopK, Is.EqualTo(1));
    }

    [Test]
    public void TopK_AboveMaximum_ClampsToMaximum()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.TopK = 200;

      // Assert
      Assert.That(config.TopK, Is.EqualTo(100));
    }

    [Test]
    public void TopK_AtMinimumBoundary_AcceptsValue()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.TopK = 1;

      // Assert
      Assert.That(config.TopK, Is.EqualTo(1));
    }

    [Test]
    public void TopK_AtMaximumBoundary_AcceptsValue()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.TopK = 100;

      // Assert
      Assert.That(config.TopK, Is.EqualTo(100));
    }

    #endregion

    #region RepeatPenalty Tests

    [Test]
    public void RepeatPenalty_ValidValue_SetsCorrectly()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.RepeatPenalty = 1.5f;

      // Assert
      Assert.That(config.RepeatPenalty, Is.EqualTo(1.5f));
    }

    [Test]
    public void RepeatPenalty_BelowMinimum_ClampsToMinimum()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.RepeatPenalty = -0.5f;

      // Assert
      Assert.That(config.RepeatPenalty, Is.EqualTo(0.0f));
    }

    [Test]
    public void RepeatPenalty_AboveMaximum_ClampsToMaximum()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.RepeatPenalty = 3.0f;

      // Assert
      Assert.That(config.RepeatPenalty, Is.EqualTo(2.0f));
    }

    [Test]
    public void RepeatPenalty_AtMinimumBoundary_AcceptsValue()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.RepeatPenalty = 0.0f;

      // Assert
      Assert.That(config.RepeatPenalty, Is.EqualTo(0.0f));
    }

    [Test]
    public void RepeatPenalty_AtMaximumBoundary_AcceptsValue()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.RepeatPenalty = 2.0f;

      // Assert
      Assert.That(config.RepeatPenalty, Is.EqualTo(2.0f));
    }

    #endregion

    #region Seed Tests

    [Test]
    public void Seed_DefaultValue_IsNull()
    {
      // Arrange & Act
      var config = new LlmConfig();

      // Assert
      Assert.That(config.Seed, Is.Null);
    }

    [Test]
    public void Seed_NullValue_SetsCorrectly()
    {
      // Arrange
      var config = new LlmConfig { Seed = 42 };

      // Act
      config.Seed = null;

      // Assert
      Assert.That(config.Seed, Is.Null);
    }

    [Test]
    public void Seed_NegativeOne_SetsCorrectly()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.Seed = -1;

      // Assert
      Assert.That(config.Seed, Is.EqualTo(-1));
    }

    [Test]
    public void Seed_Zero_SetsCorrectly()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.Seed = 0;

      // Assert
      Assert.That(config.Seed, Is.EqualTo(0));
    }

    [Test]
    public void Seed_PositiveValue_SetsCorrectly()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.Seed = 12345;

      // Assert
      Assert.That(config.Seed, Is.EqualTo(12345));
    }

    [Test]
    public void Seed_LargeValue_SetsCorrectly()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.Seed = int.MaxValue;

      // Assert
      Assert.That(config.Seed, Is.EqualTo(int.MaxValue));
    }

    #endregion

    #region StopSequences Tests

    [Test]
    public void StopSequences_DefaultValue_IsNotNull()
    {
      // Arrange & Act
      var config = new LlmConfig();

      // Assert
      Assert.That(config.StopSequences, Is.Not.Null);
      Assert.That(config.StopSequences.Length, Is.GreaterThan(0));
    }

    [Test]
    public void StopSequences_CustomValue_SetsCorrectly()
    {
      // Arrange
      var config = new LlmConfig();
      var sequences = new string[] { "END", "STOP", "DONE" };

      // Act
      config.StopSequences = sequences;

      // Assert
      Assert.That(config.StopSequences, Is.EqualTo(sequences));
      Assert.That(config.StopSequences.Length, Is.EqualTo(3));
    }

    [Test]
    public void StopSequences_EmptyArray_AcceptsValue()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.StopSequences = new string[0];

      // Assert
      Assert.That(config.StopSequences, Is.Not.Null);
      Assert.That(config.StopSequences.Length, Is.EqualTo(0));
    }

    [Test]
    public void StopSequences_NullValue_AcceptsValue()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      config.StopSequences = null!;

      // Assert
      Assert.That(config.StopSequences, Is.Null);
    }

    #endregion

    #region Validate Tests

    [Test]
    public void Validate_ValidConfig_ReturnsNoErrors()
    {
      // Arrange
      var config = new LlmConfig
      {
        MaxTokens = 100,
        Temperature = 0.8f,
        TopP = 0.9f,
        TopK = 50,
        RepeatPenalty = 1.1f,
        StopSequences = new string[] { "END" }
      };

      // Act
      var errors = config.Validate();

      // Assert
      Assert.That(errors, Is.Not.Null);
      Assert.That(errors.Length, Is.EqualTo(0));
    }

    [Test]
    public void Validate_NullStopSequences_ReturnsError()
    {
      // Arrange
      var config = new LlmConfig
      {
        StopSequences = null!
      };

      // Act
      var errors = config.Validate();

      // Assert
      Assert.That(errors, Is.Not.Null);
      Assert.That(errors.Length, Is.EqualTo(1));
      Assert.That(errors[0], Does.Contain("StopSequences cannot be null"));
    }

    [Test]
    public void Validate_DefaultConfig_ReturnsNoErrors()
    {
      // Arrange
      var config = new LlmConfig();

      // Act
      var errors = config.Validate();

      // Assert
      Assert.That(errors, Is.Not.Null);
      Assert.That(errors.Length, Is.EqualTo(0));
    }

    [Test]
    public void Validate_AllPropertiesAtBoundaries_ReturnsNoErrors()
    {
      // Arrange
      var config = new LlmConfig
      {
        MaxTokens = 2048,
        Temperature = 2.0f,
        TopP = 1.0f,
        TopK = 100,
        RepeatPenalty = 2.0f,
        StopSequences = new string[] { "END" }
      };

      // Act
      var errors = config.Validate();

      // Assert
      Assert.That(errors, Is.Not.Null);
      Assert.That(errors.Length, Is.EqualTo(0));
    }

    [Test]
    public void Validate_AllPropertiesAtMinimumBoundaries_ReturnsNoErrors()
    {
      // Arrange
      var config = new LlmConfig
      {
        MaxTokens = 1,
        Temperature = 0.0f,
        TopP = 0.0f,
        TopK = 1,
        RepeatPenalty = 0.0f,
        StopSequences = new string[] { "END" }
      };

      // Act
      var errors = config.Validate();

      // Assert
      Assert.That(errors, Is.Not.Null);
      Assert.That(errors.Length, Is.EqualTo(0));
    }

    #endregion

    #region Clone Tests

    [Test]
    public void Clone_DefaultConfig_CreatesIdenticalCopy()
    {
      // Arrange
      var original = new LlmConfig();

      // Act
      var clone = original.Clone();

      // Assert
      Assert.That(clone, Is.Not.Null);
      Assert.That(clone, Is.Not.SameAs(original));
      Assert.That(clone.MaxTokens, Is.EqualTo(original.MaxTokens));
      Assert.That(clone.Temperature, Is.EqualTo(original.Temperature));
      Assert.That(clone.TopP, Is.EqualTo(original.TopP));
      Assert.That(clone.TopK, Is.EqualTo(original.TopK));
      Assert.That(clone.RepeatPenalty, Is.EqualTo(original.RepeatPenalty));
      Assert.That(clone.Seed, Is.EqualTo(original.Seed));
      Assert.That(clone.StopSequences, Is.Not.Null);
      Assert.That(clone.StopSequences, Is.Not.SameAs(original.StopSequences));
      Assert.That(clone.StopSequences, Is.EqualTo(original.StopSequences));
    }

    [Test]
    public void Clone_CustomConfig_CreatesIdenticalCopy()
    {
      // Arrange
      var original = new LlmConfig
      {
        MaxTokens = 256,
        Temperature = 0.8f,
        TopP = 0.95f,
        TopK = 50,
        RepeatPenalty = 1.2f,
        Seed = 42,
        StopSequences = new string[] { "END", "STOP", "DONE" }
      };

      // Act
      var clone = original.Clone();

      // Assert
      Assert.That(clone, Is.Not.Null);
      Assert.That(clone, Is.Not.SameAs(original));
      Assert.That(clone.MaxTokens, Is.EqualTo(256));
      Assert.That(clone.Temperature, Is.EqualTo(0.8f));
      Assert.That(clone.TopP, Is.EqualTo(0.95f));
      Assert.That(clone.TopK, Is.EqualTo(50));
      Assert.That(clone.RepeatPenalty, Is.EqualTo(1.2f));
      Assert.That(clone.Seed, Is.EqualTo(42));
      Assert.That(clone.StopSequences, Is.Not.Null);
      Assert.That(clone.StopSequences, Is.Not.SameAs(original.StopSequences));
      Assert.That(clone.StopSequences, Is.EqualTo(original.StopSequences));
      Assert.That(clone.StopSequences.Length, Is.EqualTo(3));
    }

    [Test]
    public void Clone_ModifiedClone_DoesNotAffectOriginal()
    {
      // Arrange
      var original = new LlmConfig
      {
        MaxTokens = 100,
        Temperature = 0.7f,
        Seed = 42
      };

      // Act
      var clone = original.Clone();
      clone.MaxTokens = 200;
      clone.Temperature = 0.9f;
      clone.Seed = 999;

      // Assert
      Assert.That(original.MaxTokens, Is.EqualTo(100));
      Assert.That(original.Temperature, Is.EqualTo(0.7f));
      Assert.That(original.Seed, Is.EqualTo(42));
      Assert.That(clone.MaxTokens, Is.EqualTo(200));
      Assert.That(clone.Temperature, Is.EqualTo(0.9f));
      Assert.That(clone.Seed, Is.EqualTo(999));
    }

    [Test]
    public void Clone_NullStopSequences_CreatesEmptyArray()
    {
      // Arrange
      var original = new LlmConfig
      {
        StopSequences = null!
      };

      // Act
      var clone = original.Clone();

      // Assert
      Assert.That(clone, Is.Not.Null);
      Assert.That(clone.StopSequences, Is.Not.Null);
      Assert.That(clone.StopSequences.Length, Is.EqualTo(0));
    }

    [Test]
    public void Clone_EmptyStopSequences_CreatesEmptyArray()
    {
      // Arrange
      var original = new LlmConfig
      {
        StopSequences = new string[0]
      };

      // Act
      var clone = original.Clone();

      // Assert
      Assert.That(clone, Is.Not.Null);
      Assert.That(clone.StopSequences, Is.Not.Null);
      Assert.That(clone.StopSequences.Length, Is.EqualTo(0));
      Assert.That(clone.StopSequences, Is.Not.SameAs(original.StopSequences));
    }

    #endregion
  }
}

