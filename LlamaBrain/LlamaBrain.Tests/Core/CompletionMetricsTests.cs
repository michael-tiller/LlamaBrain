using System;
using NUnit.Framework;
using LlamaBrain.Core;

namespace LlamaBrain.Tests.Core
{
  /// <summary>
  /// Tests for the CompletionMetrics performance tracking
  /// </summary>
  [TestFixture]
  public class CompletionMetricsTests
  {
    #region Constructor Tests

    [Test]
    public void CompletionMetrics_DefaultConstructor_InitializesWithDefaults()
    {
      // Arrange & Act
      var metrics = new CompletionMetrics();

      // Assert
      Assert.IsNotNull(metrics);
      Assert.AreEqual(string.Empty, metrics.Content);
      Assert.AreEqual(0, metrics.PromptTokenCount);
      Assert.AreEqual(0, metrics.PrefillTimeMs);
      Assert.AreEqual(0, metrics.DecodeTimeMs);
      Assert.AreEqual(0, metrics.TtftMs);
      Assert.AreEqual(0, metrics.GeneratedTokenCount);
      Assert.AreEqual(0, metrics.CachedTokenCount);
      Assert.AreEqual(0, metrics.TotalTimeMs);
      Assert.AreEqual(0, metrics.TokensPerSecond);
    }

    [Test]
    public void CompletionMetrics_ObjectInitializer_SetsAllProperties()
    {
      // Arrange & Act
      var metrics = new CompletionMetrics
      {
        Content = "Test response",
        PromptTokenCount = 100,
        PrefillTimeMs = 50,
        DecodeTimeMs = 200,
        TtftMs = 50,
        GeneratedTokenCount = 50,
        CachedTokenCount = 10,
        TotalTimeMs = 250
      };

      // Assert
      Assert.AreEqual("Test response", metrics.Content);
      Assert.AreEqual(100, metrics.PromptTokenCount);
      Assert.AreEqual(50, metrics.PrefillTimeMs);
      Assert.AreEqual(200, metrics.DecodeTimeMs);
      Assert.AreEqual(50, metrics.TtftMs);
      Assert.AreEqual(50, metrics.GeneratedTokenCount);
      Assert.AreEqual(10, metrics.CachedTokenCount);
      Assert.AreEqual(250, metrics.TotalTimeMs);
    }

    #endregion

    #region TokensPerSecond Calculation Tests

    [Test]
    public void TokensPerSecond_WithValidValues_CalculatesCorrectly()
    {
      // Arrange
      var metrics = new CompletionMetrics
      {
        GeneratedTokenCount = 100,
        DecodeTimeMs = 2000 // 2 seconds
      };

      // Act
      var tokensPerSecond = metrics.TokensPerSecond;

      // Assert
      // 100 tokens / 2 seconds = 50 tokens/second
      Assert.AreEqual(50.0, tokensPerSecond, 0.001);
    }

    [Test]
    public void TokensPerSecond_WithZeroDecodeTime_ReturnsZero()
    {
      // Arrange
      var metrics = new CompletionMetrics
      {
        GeneratedTokenCount = 100,
        DecodeTimeMs = 0
      };

      // Act
      var tokensPerSecond = metrics.TokensPerSecond;

      // Assert
      Assert.AreEqual(0, tokensPerSecond);
    }

    [Test]
    public void TokensPerSecond_WithZeroTokens_ReturnsZero()
    {
      // Arrange
      var metrics = new CompletionMetrics
      {
        GeneratedTokenCount = 0,
        DecodeTimeMs = 1000
      };

      // Act
      var tokensPerSecond = metrics.TokensPerSecond;

      // Assert
      Assert.AreEqual(0, tokensPerSecond);
    }

    [Test]
    public void TokensPerSecond_WithVeryFastGeneration_CalculatesHighRate()
    {
      // Arrange
      var metrics = new CompletionMetrics
      {
        GeneratedTokenCount = 1000,
        DecodeTimeMs = 100 // 0.1 seconds
      };

      // Act
      var tokensPerSecond = metrics.TokensPerSecond;

      // Assert
      // 1000 tokens / 0.1 seconds = 10000 tokens/second
      Assert.AreEqual(10000.0, tokensPerSecond, 0.001);
    }

    [Test]
    public void TokensPerSecond_WithSlowGeneration_CalculatesLowRate()
    {
      // Arrange
      var metrics = new CompletionMetrics
      {
        GeneratedTokenCount = 10,
        DecodeTimeMs = 10000 // 10 seconds
      };

      // Act
      var tokensPerSecond = metrics.TokensPerSecond;

      // Assert
      // 10 tokens / 10 seconds = 1 token/second
      Assert.AreEqual(1.0, tokensPerSecond, 0.001);
    }

    [Test]
    public void TokensPerSecond_WithFractionalResult_CalculatesPrecisely()
    {
      // Arrange
      var metrics = new CompletionMetrics
      {
        GeneratedTokenCount = 33,
        DecodeTimeMs = 1000 // 1 second
      };

      // Act
      var tokensPerSecond = metrics.TokensPerSecond;

      // Assert
      // 33 tokens / 1 second = 33 tokens/second
      Assert.AreEqual(33.0, tokensPerSecond, 0.001);
    }

    [Test]
    public void TokensPerSecond_WithOneMillisecond_CalculatesCorrectly()
    {
      // Arrange
      var metrics = new CompletionMetrics
      {
        GeneratedTokenCount = 50,
        DecodeTimeMs = 1 // 1 millisecond
      };

      // Act
      var tokensPerSecond = metrics.TokensPerSecond;

      // Assert
      // 50 tokens / 0.001 seconds = 50000 tokens/second
      Assert.AreEqual(50000.0, tokensPerSecond, 0.001);
    }

    [Test]
    public void TokensPerSecond_IsComputedProperty_UpdatesWhenValuesChange()
    {
      // Arrange
      var metrics = new CompletionMetrics
      {
        GeneratedTokenCount = 100,
        DecodeTimeMs = 1000
      };

      // Act & Assert - Initial calculation
      Assert.AreEqual(100.0, metrics.TokensPerSecond, 0.001);

      // Change values
      metrics.GeneratedTokenCount = 200;
      metrics.DecodeTimeMs = 2000;

      // Assert - Should recalculate
      Assert.AreEqual(100.0, metrics.TokensPerSecond, 0.001);
    }

    #endregion

    #region Property Setter/Getter Tests

    [Test]
    public void Content_CanBeSetAndRetrieved()
    {
      // Arrange
      var metrics = new CompletionMetrics();
      var testContent = "Hello, world!";

      // Act
      metrics.Content = testContent;

      // Assert
      Assert.AreEqual(testContent, metrics.Content);
    }

    [Test]
    public void Content_CanBeSetToNull()
    {
      // Arrange
      var metrics = new CompletionMetrics();

      // Act
      metrics.Content = null!;

      // Assert
      Assert.IsNull(metrics.Content);
    }

    [Test]
    public void Content_CanBeSetToEmptyString()
    {
      // Arrange
      var metrics = new CompletionMetrics();

      // Act
      metrics.Content = string.Empty;

      // Assert
      Assert.AreEqual(string.Empty, metrics.Content);
    }

    [Test]
    public void PromptTokenCount_CanBeSetAndRetrieved()
    {
      // Arrange
      var metrics = new CompletionMetrics();

      // Act
      metrics.PromptTokenCount = 500;

      // Assert
      Assert.AreEqual(500, metrics.PromptTokenCount);
    }

    [Test]
    public void PrefillTimeMs_CanBeSetAndRetrieved()
    {
      // Arrange
      var metrics = new CompletionMetrics();

      // Act
      metrics.PrefillTimeMs = 150;

      // Assert
      Assert.AreEqual(150, metrics.PrefillTimeMs);
    }

    [Test]
    public void DecodeTimeMs_CanBeSetAndRetrieved()
    {
      // Arrange
      var metrics = new CompletionMetrics();

      // Act
      metrics.DecodeTimeMs = 300;

      // Assert
      Assert.AreEqual(300, metrics.DecodeTimeMs);
    }

    [Test]
    public void TtftMs_CanBeSetAndRetrieved()
    {
      // Arrange
      var metrics = new CompletionMetrics();

      // Act
      metrics.TtftMs = 75;

      // Assert
      Assert.AreEqual(75, metrics.TtftMs);
    }

    [Test]
    public void GeneratedTokenCount_CanBeSetAndRetrieved()
    {
      // Arrange
      var metrics = new CompletionMetrics();

      // Act
      metrics.GeneratedTokenCount = 250;

      // Assert
      Assert.AreEqual(250, metrics.GeneratedTokenCount);
    }

    [Test]
    public void CachedTokenCount_CanBeSetAndRetrieved()
    {
      // Arrange
      var metrics = new CompletionMetrics();

      // Act
      metrics.CachedTokenCount = 50;

      // Assert
      Assert.AreEqual(50, metrics.CachedTokenCount);
    }

    [Test]
    public void TotalTimeMs_CanBeSetAndRetrieved()
    {
      // Arrange
      var metrics = new CompletionMetrics();

      // Act
      metrics.TotalTimeMs = 500;

      // Assert
      Assert.AreEqual(500, metrics.TotalTimeMs);
    }

    #endregion

    #region Edge Cases

    [Test]
    public void CompletionMetrics_WithAllZeroValues_HandlesGracefully()
    {
      // Arrange & Act
      var metrics = new CompletionMetrics
      {
        Content = "",
        PromptTokenCount = 0,
        PrefillTimeMs = 0,
        DecodeTimeMs = 0,
        TtftMs = 0,
        GeneratedTokenCount = 0,
        CachedTokenCount = 0,
        TotalTimeMs = 0
      };

      // Assert
      Assert.AreEqual(0, metrics.TokensPerSecond);
      Assert.AreEqual("", metrics.Content);
    }

    [Test]
    public void CompletionMetrics_WithLargeValues_HandlesGracefully()
    {
      // Arrange & Act
      var metrics = new CompletionMetrics
      {
        Content = new string('A', 10000), // Large content
        PromptTokenCount = int.MaxValue,
        PrefillTimeMs = long.MaxValue,
        DecodeTimeMs = long.MaxValue,
        TtftMs = long.MaxValue,
        GeneratedTokenCount = int.MaxValue,
        CachedTokenCount = int.MaxValue,
        TotalTimeMs = long.MaxValue
      };

      // Assert
      Assert.AreEqual(10000, metrics.Content.Length);
      Assert.AreEqual(int.MaxValue, metrics.PromptTokenCount);
      Assert.AreEqual(int.MaxValue, metrics.GeneratedTokenCount);
      // TokensPerSecond calculation with MaxValue should still work (though result may be large)
      Assert.IsTrue(metrics.TokensPerSecond >= 0);
    }

    [Test]
    public void CompletionMetrics_WithNegativeValues_AllowsButMayCauseIssues()
    {
      // Arrange & Act
      var metrics = new CompletionMetrics
      {
        PromptTokenCount = -10,
        PrefillTimeMs = -5,
        DecodeTimeMs = -20,
        GeneratedTokenCount = -15
      };

      // Assert
      // Properties allow negative values (no validation in the class itself)
      Assert.AreEqual(-10, metrics.PromptTokenCount);
      Assert.AreEqual(-5, metrics.PrefillTimeMs);
      Assert.AreEqual(-20, metrics.DecodeTimeMs);
      // TokensPerSecond with negative DecodeTimeMs will return 0 (due to division check)
      Assert.AreEqual(0, metrics.TokensPerSecond);
    }

    [Test]
    public void CompletionMetrics_WithVeryLongContent_HandlesGracefully()
    {
      // Arrange
      var longContent = new string('X', 100000); // 100k characters
      var metrics = new CompletionMetrics();

      // Act
      metrics.Content = longContent;

      // Assert
      Assert.AreEqual(100000, metrics.Content.Length);
      Assert.AreEqual(longContent, metrics.Content);
    }

    #endregion

    #region Real-World Scenarios

    [Test]
    public void CompletionMetrics_WithTypicalCompletion_CalculatesCorrectly()
    {
      // Arrange & Act - Simulating a typical completion
      var metrics = new CompletionMetrics
      {
        Content = "Hello! How can I help you today?",
        PromptTokenCount = 150,
        PrefillTimeMs = 80,
        DecodeTimeMs = 120,
        TtftMs = 80,
        GeneratedTokenCount = 12,
        CachedTokenCount = 0,
        TotalTimeMs = 200
      };

      // Assert
      Assert.AreEqual("Hello! How can I help you today?", metrics.Content);
      Assert.AreEqual(150, metrics.PromptTokenCount);
      Assert.AreEqual(80, metrics.PrefillTimeMs);
      Assert.AreEqual(120, metrics.DecodeTimeMs);
      Assert.AreEqual(80, metrics.TtftMs);
      Assert.AreEqual(12, metrics.GeneratedTokenCount);
      Assert.AreEqual(0, metrics.CachedTokenCount);
      Assert.AreEqual(200, metrics.TotalTimeMs);
      // 12 tokens / 0.12 seconds = 100 tokens/second
      Assert.AreEqual(100.0, metrics.TokensPerSecond, 0.001);
    }

    [Test]
    public void CompletionMetrics_WithCachedTokens_ReflectsCorrectly()
    {
      // Arrange & Act - Simulating a completion with cached tokens
      var metrics = new CompletionMetrics
      {
        Content = "This is a cached response.",
        PromptTokenCount = 200,
        PrefillTimeMs = 50, // Faster due to caching
        DecodeTimeMs = 100,
        TtftMs = 50,
        GeneratedTokenCount = 8,
        CachedTokenCount = 150, // Many tokens cached
        TotalTimeMs = 150
      };

      // Assert
      Assert.AreEqual(150, metrics.CachedTokenCount);
      Assert.AreEqual(50, metrics.PrefillTimeMs); // Lower prefill due to cache
      // 8 tokens / 0.1 seconds = 80 tokens/second
      Assert.AreEqual(80.0, metrics.TokensPerSecond, 0.001);
    }

    [Test]
    public void CompletionMetrics_WithErrorResponse_HandlesCorrectly()
    {
      // Arrange & Act - Simulating an error response (as created in ApiClient)
      var metrics = new CompletionMetrics
      {
        Content = "Error: Request was cancelled",
        TotalTimeMs = 5000
      };

      // Assert
      Assert.AreEqual("Error: Request was cancelled", metrics.Content);
      Assert.AreEqual(5000, metrics.TotalTimeMs);
      Assert.AreEqual(0, metrics.TokensPerSecond); // No tokens generated
      Assert.AreEqual(0, metrics.GeneratedTokenCount);
    }

    #endregion

    #region Integration Tests

    [Test]
    public void CompletionMetrics_CanBeUsedInMetricsCollection()
    {
      // Arrange
      var metrics1 = new CompletionMetrics
      {
        Content = "Response 1",
        GeneratedTokenCount = 10,
        DecodeTimeMs = 100,
        TotalTimeMs = 150
      };

      var metrics2 = new CompletionMetrics
      {
        Content = "Response 2",
        GeneratedTokenCount = 20,
        DecodeTimeMs = 200,
        TotalTimeMs = 250
      };

      // Act & Assert - Verify both metrics are independent
      Assert.AreEqual(100.0, metrics1.TokensPerSecond, 0.001);
      Assert.AreEqual(100.0, metrics2.TokensPerSecond, 0.001);
      Assert.AreNotSame(metrics1, metrics2);
    }

    [Test]
    public void CompletionMetrics_MultipleInstances_AreIndependent()
    {
      // Arrange
      var metrics1 = new CompletionMetrics { GeneratedTokenCount = 100, DecodeTimeMs = 1000 };
      var metrics2 = new CompletionMetrics { GeneratedTokenCount = 200, DecodeTimeMs = 2000 };

      // Act & Assert
      Assert.AreEqual(100.0, metrics1.TokensPerSecond, 0.001);
      Assert.AreEqual(100.0, metrics2.TokensPerSecond, 0.001);

      // Modify one, verify other is unchanged
      metrics1.GeneratedTokenCount = 50;
      Assert.AreEqual(50.0, metrics1.TokensPerSecond, 0.001);
      Assert.AreEqual(100.0, metrics2.TokensPerSecond, 0.001);
    }

    #endregion
  }
}

