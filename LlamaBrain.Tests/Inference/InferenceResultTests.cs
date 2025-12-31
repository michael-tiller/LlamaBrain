using System;
using System.Collections.Generic;
using NUnit.Framework;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.Inference
{
  [TestFixture]
  public class InferenceResultTests
  {
    private StateSnapshot CreateTestSnapshot(int attemptNumber = 0)
    {
      return new StateSnapshotBuilder()
        .WithSystemPrompt("Test")
        .WithPlayerInput("Hello")
        .WithAttemptNumber(attemptNumber)
        .Build();
    }

    [Test]
    public void Succeeded_CreatesSuccessfulResult()
    {
      var snapshot = CreateTestSnapshot();
      var result = InferenceResult.Succeeded(
        response: "Hello there!",
        snapshot: snapshot,
        elapsedMs: 100
      );

      Assert.That(result.Success, Is.True);
      Assert.That(result.Response, Is.EqualTo("Hello there!"));
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.Valid));
      Assert.That(result.Violations.Count, Is.EqualTo(0));
      Assert.That(result.ElapsedMilliseconds, Is.EqualTo(100));
      Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void Succeeded_IncludesTokenUsage()
    {
      var snapshot = CreateTestSnapshot();
      var tokenUsage = new TokenUsage { PromptTokens = 50, CompletionTokens = 10 };

      var result = InferenceResult.Succeeded(
        response: "Response",
        snapshot: snapshot,
        elapsedMs: 100,
        tokenUsage: tokenUsage
      );

      Assert.That(result.TokenUsage, Is.Not.Null);
      Assert.That(result.TokenUsage.PromptTokens, Is.EqualTo(50));
      Assert.That(result.TokenUsage.CompletionTokens, Is.EqualTo(10));
      Assert.That(result.TokenUsage.TotalTokens, Is.EqualTo(60));
    }

    [Test]
    public void FailedValidation_CreatesFailedResult()
    {
      var snapshot = CreateTestSnapshot();
      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(
          Constraint.Prohibit("No swearing", "test"),
          "Response contains prohibited content",
          "damn"
        )
      };

      var result = InferenceResult.FailedValidation(
        response: "Well damn!",
        outcome: ValidationOutcome.ProhibitionViolated,
        violations: violations,
        snapshot: snapshot,
        elapsedMs: 50
      );

      Assert.That(result.Success, Is.False);
      Assert.That(result.Response, Is.EqualTo("Well damn!"));
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
      Assert.That(result.Violations.Count, Is.EqualTo(1));
      Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void FailedError_CreatesErrorResult()
    {
      var snapshot = CreateTestSnapshot();

      var result = InferenceResult.FailedError(
        errorMessage: "Connection timeout",
        snapshot: snapshot,
        elapsedMs: 5000
      );

      Assert.That(result.Success, Is.False);
      Assert.That(result.Response, Is.Empty);
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.InvalidFormat));
      Assert.That(result.ErrorMessage, Is.EqualTo("Connection timeout"));
      Assert.That(result.Violations.Count, Is.EqualTo(0));
    }

    [Test]
    public void ConstraintViolation_StoresAllData()
    {
      var constraint = Constraint.Prohibit("No violence", "combat");
      var violation = new ConstraintViolation(
        constraint,
        "Response contains violent content",
        "I will kill you"
      );

      Assert.That(violation.Constraint, Is.EqualTo(constraint));
      Assert.That(violation.Description, Is.EqualTo("Response contains violent content"));
      Assert.That(violation.ViolatingText, Is.EqualTo("I will kill you"));
    }

    [Test]
    public void ConstraintViolation_ToString_FormatsCorrectly()
    {
      var violation = new ConstraintViolation(
        Constraint.Prohibit("No swearing", "test"),
        "Contains prohibited word"
      );

      var str = violation.ToString();
      Assert.That(str, Does.Contain("[Prohibition]"));
      Assert.That(str, Does.Contain("Contains prohibited word"));
    }
  }

  [TestFixture]
  public class InferenceResultWithRetriesTests
  {
    private StateSnapshot CreateTestSnapshot(int attemptNumber = 0)
    {
      return new StateSnapshotBuilder()
        .WithSystemPrompt("Test")
        .WithAttemptNumber(attemptNumber)
        .Build();
    }

    [Test]
    public void Success_WhenFinalAttemptSucceeds()
    {
      var attempts = new List<InferenceResult>
      {
        InferenceResult.FailedValidation(
          "bad response",
          ValidationOutcome.ProhibitionViolated,
          new List<ConstraintViolation>(),
          CreateTestSnapshot(0),
          50
        ),
        InferenceResult.Succeeded("good response", CreateTestSnapshot(1), 100)
      };

      var result = new InferenceResultWithRetries(
        attempts[1],
        attempts,
        150
      );

      Assert.That(result.Success, Is.True);
      Assert.That(result.AttemptCount, Is.EqualTo(2));
      Assert.That(result.TotalElapsedMilliseconds, Is.EqualTo(150));
    }

    [Test]
    public void Failed_WhenAllAttemptsFail()
    {
      var attempts = new List<InferenceResult>
      {
        InferenceResult.FailedValidation(
          "bad1",
          ValidationOutcome.ProhibitionViolated,
          new List<ConstraintViolation>(),
          CreateTestSnapshot(0),
          50
        ),
        InferenceResult.FailedValidation(
          "bad2",
          ValidationOutcome.ProhibitionViolated,
          new List<ConstraintViolation>(),
          CreateTestSnapshot(1),
          50
        ),
        InferenceResult.FailedValidation(
          "bad3",
          ValidationOutcome.RequirementNotMet,
          new List<ConstraintViolation>(),
          CreateTestSnapshot(2),
          50
        )
      };

      var result = new InferenceResultWithRetries(
        attempts[2],
        attempts,
        150
      );

      Assert.That(result.Success, Is.False);
      Assert.That(result.AttemptCount, Is.EqualTo(3));
    }

    [Test]
    public void GetTotalTokenUsage_AggregatesAllAttempts()
    {
      var attempts = new List<InferenceResult>
      {
        InferenceResult.Succeeded(
          "response1",
          CreateTestSnapshot(0),
          50,
          new TokenUsage { PromptTokens = 100, CompletionTokens = 10 }
        ),
        InferenceResult.Succeeded(
          "response2",
          CreateTestSnapshot(1),
          50,
          new TokenUsage { PromptTokens = 100, CompletionTokens = 15 }
        )
      };

      var result = new InferenceResultWithRetries(attempts[1], attempts, 100);
      var totalUsage = result.GetTotalTokenUsage();

      Assert.That(totalUsage, Is.Not.Null);
      Assert.That(totalUsage.PromptTokens, Is.EqualTo(200));
      Assert.That(totalUsage.CompletionTokens, Is.EqualTo(25));
      Assert.That(totalUsage.TotalTokens, Is.EqualTo(225));
    }

    [Test]
    public void GetTotalTokenUsage_ReturnsNull_WhenNoUsageData()
    {
      var attempts = new List<InferenceResult>
      {
        InferenceResult.Succeeded("response", CreateTestSnapshot(), 50)
      };

      var result = new InferenceResultWithRetries(attempts[0], attempts, 50);
      var totalUsage = result.GetTotalTokenUsage();

      Assert.That(totalUsage, Is.Null);
    }

    [Test]
    public void ToString_IncludesRelevantInfo()
    {
      var attempts = new List<InferenceResult>
      {
        InferenceResult.Succeeded("response", CreateTestSnapshot(), 50)
      };

      var result = new InferenceResultWithRetries(attempts[0], attempts, 50);
      var str = result.ToString();

      Assert.That(str, Does.Contain("Success"));
      Assert.That(str, Does.Contain("1 attempts"));
      Assert.That(str, Does.Contain("50ms"));
    }
  }
}
