using System;
using System.Collections.Generic;
using NUnit.Framework;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.Inference
{
  /// <summary>
  /// Tests for InferenceResult, InferenceResultWithRetries, and related classes.
  /// </summary>
  public class InferenceResultTests
  {
    private StateSnapshot _defaultSnapshot = null!;

    [SetUp]
    public void SetUp()
    {
      var context = new InteractionContext { NpcId = "npc-001" };
      var constraints = new ConstraintSet();
      
      _defaultSnapshot = new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt("Test prompt")
        .WithPlayerInput("Test input")
        .Build();
    }

    #region InferenceResult.Succeeded Tests

    [Test]
    public void Succeeded_CreatesSuccessfulResult()
    {
      // Act
      var result = InferenceResult.Succeeded("Hello, traveler!", _defaultSnapshot, 100);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.Response, Is.EqualTo("Hello, traveler!"));
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.Valid));
      Assert.That(result.Violations, Is.Empty);
      Assert.That(result.Snapshot, Is.EqualTo(_defaultSnapshot));
      Assert.That(result.ElapsedMilliseconds, Is.EqualTo(100));
      Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void Succeeded_WithTokenUsage_IncludesTokenUsage()
    {
      // Arrange
      var tokenUsage = new TokenUsage { PromptTokens = 50, CompletionTokens = 25 };

      // Act
      var result = InferenceResult.Succeeded("Response", _defaultSnapshot, 100, tokenUsage);

      // Assert
      Assert.That(result.TokenUsage, Is.Not.Null);
      Assert.That(result.TokenUsage!.PromptTokens, Is.EqualTo(50));
      Assert.That(result.TokenUsage.CompletionTokens, Is.EqualTo(25));
      Assert.That(result.TokenUsage.TotalTokens, Is.EqualTo(75));
    }

    #endregion

    #region InferenceResult.FailedValidation Tests

    [Test]
    public void FailedValidation_CreatesFailedResult()
    {
      // Arrange
      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(
          Constraint.Prohibition("test", "Test prohibition", "Do not test"),
          "Violation description"
        )
      };

      // Act
      var result = InferenceResult.FailedValidation(
        "Bad response",
        ValidationOutcome.ProhibitionViolated,
        violations,
        _defaultSnapshot,
        150
      );

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.Response, Is.EqualTo("Bad response"));
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
      Assert.That(result.Violations.Count, Is.EqualTo(1));
      Assert.That(result.Snapshot, Is.EqualTo(_defaultSnapshot));
      Assert.That(result.ElapsedMilliseconds, Is.EqualTo(150));
      Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void FailedValidation_WithTokenUsage_IncludesTokenUsage()
    {
      // Arrange
      var violations = new List<ConstraintViolation>();
      var tokenUsage = new TokenUsage { PromptTokens = 100, CompletionTokens = 50 };

      // Act
      var result = InferenceResult.FailedValidation(
        "Response",
        ValidationOutcome.RequirementNotMet,
        violations,
        _defaultSnapshot,
        200,
        tokenUsage
      );

      // Assert
      Assert.That(result.TokenUsage, Is.Not.Null);
      Assert.That(result.TokenUsage!.TotalTokens, Is.EqualTo(150));
    }

    #endregion

    #region InferenceResult.FailedError Tests

    [Test]
    public void FailedError_CreatesErrorResult()
    {
      // Act
      var result = InferenceResult.FailedError("Connection timeout", _defaultSnapshot, 5000);

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.Response, Is.Empty);
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.InvalidFormat));
      Assert.That(result.Violations, Is.Empty);
      Assert.That(result.ErrorMessage, Is.EqualTo("Connection timeout"));
      Assert.That(result.ElapsedMilliseconds, Is.EqualTo(5000));
      Assert.That(result.TokenUsage, Is.Null);
    }

    #endregion

    #region InferenceResult.ToString Tests

    [Test]
    public void ToString_SuccessfulResult_ReturnsFormattedString()
    {
      // Arrange
      var snapshot = new StateSnapshotBuilder()
        .WithAttemptNumber(0)
        .Build();
      var result = InferenceResult.Succeeded("Response", snapshot, 100);

      // Act
      var str = result.ToString();

      // Assert
      Assert.That(str, Does.Contain("InferenceResult[Success]"));
      Assert.That(str, Does.Contain("Attempt 1"));
      Assert.That(str, Does.Contain("100ms"));
    }

    [Test]
    public void ToString_FailedValidation_ReturnsFormattedString()
    {
      // Arrange
      var snapshot = new StateSnapshotBuilder()
        .WithAttemptNumber(1)
        .Build();
      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(
          Constraint.Prohibition("test", "Test", "Test"),
          "Violation"
        )
      };
      var result = InferenceResult.FailedValidation(
        "Response",
        ValidationOutcome.ProhibitionViolated,
        violations,
        snapshot,
        200
      );

      // Act
      var str = result.ToString();

      // Assert
      Assert.That(str, Does.Contain("InferenceResult[ProhibitionViolated]"));
      Assert.That(str, Does.Contain("1 violations"));
      Assert.That(str, Does.Contain("Attempt 2"));
    }

    [Test]
    public void ToString_FailedError_ReturnsFormattedString()
    {
      // Arrange
      var result = InferenceResult.FailedError("Error message", _defaultSnapshot, 1000);

      // Act
      var str = result.ToString();

      // Assert
      Assert.That(str, Does.Contain("InferenceResult[Error]"));
      Assert.That(str, Does.Contain("Error message"));
    }

    #endregion

    #region ConstraintViolation Tests

    [Test]
    public void ConstraintViolation_CreatesViolation()
    {
      // Arrange
      var constraint = Constraint.Prohibition("test", "Test prohibition", "Do not test");

      // Act
      var violation = new ConstraintViolation(constraint, "Violation description", "violating text");

      // Assert
      Assert.That(violation.Constraint, Is.EqualTo(constraint));
      Assert.That(violation.Description, Is.EqualTo("Violation description"));
      Assert.That(violation.ViolatingText, Is.EqualTo("violating text"));
    }

    [Test]
    public void ConstraintViolation_ToString_ReturnsFormattedString()
    {
      // Arrange
      var constraint = Constraint.Prohibition("test", "Test prohibition", "Do not test");
      var violation = new ConstraintViolation(constraint, "Violation description");

      // Act
      var str = violation.ToString();

      // Assert
      Assert.That(str, Does.Contain("[Prohibition]"));
      Assert.That(str, Does.Contain("Violation description"));
    }

    #endregion

    #region TokenUsage Tests

    [Test]
    public void TokenUsage_TotalTokens_CalculatesCorrectly()
    {
      // Arrange
      var usage = new TokenUsage
      {
        PromptTokens = 100,
        CompletionTokens = 50
      };

      // Act & Assert
      Assert.That(usage.TotalTokens, Is.EqualTo(150));
    }

    [Test]
    public void TokenUsage_ZeroTokens_ReturnsZero()
    {
      // Arrange
      var usage = new TokenUsage();

      // Act & Assert
      Assert.That(usage.TotalTokens, Is.EqualTo(0));
    }

    #endregion

    #region InferenceResultWithRetries Tests

    [Test]
    public void InferenceResultWithRetries_CreatesAggregatedResult()
    {
      // Arrange
      var finalResult = InferenceResult.Succeeded("Success", _defaultSnapshot, 200);
      var allAttempts = new List<InferenceResult>
      {
        InferenceResult.FailedValidation("Bad 1", ValidationOutcome.ProhibitionViolated, new List<ConstraintViolation>(), _defaultSnapshot, 100),
        InferenceResult.FailedValidation("Bad 2", ValidationOutcome.RequirementNotMet, new List<ConstraintViolation>(), _defaultSnapshot, 150),
        finalResult
      };

      // Act
      var aggregated = new InferenceResultWithRetries(finalResult, allAttempts, 450);

      // Assert
      Assert.That(aggregated.Success, Is.True);
      Assert.That(aggregated.FinalResult, Is.EqualTo(finalResult));
      Assert.That(aggregated.AllAttempts.Count, Is.EqualTo(3));
      Assert.That(aggregated.TotalElapsedMilliseconds, Is.EqualTo(450));
      Assert.That(aggregated.AttemptCount, Is.EqualTo(3));
    }

    [Test]
    public void InferenceResultWithRetries_GetTotalTokenUsage_CalculatesTotal()
    {
      // Arrange
      var snapshot1 = new StateSnapshotBuilder().Build();
      var snapshot2 = new StateSnapshotBuilder().Build();
      var snapshot3 = new StateSnapshotBuilder().Build();

      var allAttempts = new List<InferenceResult>
      {
        InferenceResult.Succeeded("Response 1", snapshot1, 100, new TokenUsage { PromptTokens = 50, CompletionTokens = 25 }),
        InferenceResult.Succeeded("Response 2", snapshot2, 150, new TokenUsage { PromptTokens = 60, CompletionTokens = 30 }),
        InferenceResult.Succeeded("Response 3", snapshot3, 200, new TokenUsage { PromptTokens = 70, CompletionTokens = 35 })
      };

      var aggregated = new InferenceResultWithRetries(allAttempts[2], allAttempts, 450);

      // Act
      var totalUsage = aggregated.GetTotalTokenUsage();

      // Assert
      Assert.That(totalUsage, Is.Not.Null);
      Assert.That(totalUsage!.PromptTokens, Is.EqualTo(180)); // 50 + 60 + 70
      Assert.That(totalUsage.CompletionTokens, Is.EqualTo(90)); // 25 + 30 + 35
      Assert.That(totalUsage.TotalTokens, Is.EqualTo(270));
    }

    [Test]
    public void InferenceResultWithRetries_GetTotalTokenUsage_WithMissingUsage_ReturnsNull()
    {
      // Arrange
      var allAttempts = new List<InferenceResult>
      {
        InferenceResult.Succeeded("Response 1", _defaultSnapshot, 100), // No token usage
        InferenceResult.Succeeded("Response 2", _defaultSnapshot, 150) // No token usage
      };

      var aggregated = new InferenceResultWithRetries(allAttempts[1], allAttempts, 250);

      // Act
      var totalUsage = aggregated.GetTotalTokenUsage();

      // Assert
      Assert.That(totalUsage, Is.Null);
    }

    [Test]
    public void InferenceResultWithRetries_GetTotalTokenUsage_WithPartialUsage_CalculatesAvailable()
    {
      // Arrange
      var snapshot1 = new StateSnapshotBuilder().Build();
      var snapshot2 = new StateSnapshotBuilder().Build();

      var allAttempts = new List<InferenceResult>
      {
        InferenceResult.Succeeded("Response 1", snapshot1, 100), // No token usage
        InferenceResult.Succeeded("Response 2", snapshot2, 150, new TokenUsage { PromptTokens = 50, CompletionTokens = 25 })
      };

      var aggregated = new InferenceResultWithRetries(allAttempts[1], allAttempts, 250);

      // Act
      var totalUsage = aggregated.GetTotalTokenUsage();

      // Assert
      Assert.That(totalUsage, Is.Not.Null);
      Assert.That(totalUsage!.PromptTokens, Is.EqualTo(50));
      Assert.That(totalUsage.CompletionTokens, Is.EqualTo(25));
    }

    [Test]
    public void InferenceResultWithRetries_ToString_ReturnsFormattedString()
    {
      // Arrange
      var finalResult = InferenceResult.Succeeded("Success", _defaultSnapshot, 200);
      var allAttempts = new List<InferenceResult> { finalResult };
      var aggregated = new InferenceResultWithRetries(finalResult, allAttempts, 200);

      // Act
      var str = aggregated.ToString();

      // Assert
      Assert.That(str, Does.Contain("InferenceWithRetries[Success]"));
      Assert.That(str, Does.Contain("1 attempts"));
      Assert.That(str, Does.Contain("200ms total"));
    }

    [Test]
    public void InferenceResultWithRetries_ToString_Failed_ReturnsFormattedString()
    {
      // Arrange
      var finalResult = InferenceResult.FailedValidation(
        "Failed",
        ValidationOutcome.ProhibitionViolated,
        new List<ConstraintViolation>(),
        _defaultSnapshot,
        200
      );
      var allAttempts = new List<InferenceResult> { finalResult };
      var aggregated = new InferenceResultWithRetries(finalResult, allAttempts, 200);

      // Act
      var str = aggregated.ToString();

      // Assert
      Assert.That(str, Does.Contain("InferenceWithRetries[Failed]"));
    }

    #endregion
  }
}

