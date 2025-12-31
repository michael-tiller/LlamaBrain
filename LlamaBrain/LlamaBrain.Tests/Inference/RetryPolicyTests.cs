using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.Inference
{
  /// <summary>
  /// Tests for RetryPolicy.
  /// </summary>
  public class RetryPolicyTests
  {
    private StateSnapshot _defaultSnapshot = null!;

    [SetUp]
    public void SetUp()
    {
      var context = new InteractionContext { NpcId = "npc-001" };
      _defaultSnapshot = new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(new ConstraintSet())
        .Build();
    }

    #region Policy Creation Tests

    [Test]
    public void Default_CreatesDefaultPolicy()
    {
      // Act
      var policy = RetryPolicy.Default;

      // Assert
      Assert.That(policy.MaxRetries, Is.EqualTo(2));
      Assert.That(policy.Escalation, Is.EqualTo(ConstraintEscalation.AddSpecificProhibition));
      Assert.That(policy.IncludePreviousResponse, Is.True);
      Assert.That(policy.IncludeViolationFeedback, Is.True);
      Assert.That(policy.RetryDelayMs, Is.EqualTo(0));
      Assert.That(policy.MaxTotalTimeMs, Is.EqualTo(30000));
    }

    [Test]
    public void NoRetry_CreatesNoRetryPolicy()
    {
      // Act
      var policy = RetryPolicy.NoRetry;

      // Assert
      Assert.That(policy.MaxRetries, Is.EqualTo(0));
      Assert.That(policy.MaxAttempts, Is.EqualTo(1));
    }

    [Test]
    public void Aggressive_CreatesAggressivePolicy()
    {
      // Act
      var policy = RetryPolicy.Aggressive;

      // Assert
      Assert.That(policy.MaxRetries, Is.EqualTo(3));
      Assert.That(policy.Escalation, Is.EqualTo(ConstraintEscalation.Full));
      Assert.That(policy.IncludePreviousResponse, Is.True);
      Assert.That(policy.IncludeViolationFeedback, Is.True);
    }

    [Test]
    public void MaxAttempts_CalculatesCorrectly()
    {
      // Arrange
      var policy = new RetryPolicy { MaxRetries = 2 };

      // Act & Assert
      Assert.That(policy.MaxAttempts, Is.EqualTo(3)); // 1 initial + 2 retries
    }

    #endregion

    #region GenerateRetryConstraints Tests

    [Test]
    public void GenerateRetryConstraints_NoneEscalation_ReturnsEmpty()
    {
      // Arrange
      var policy = new RetryPolicy { Escalation = ConstraintEscalation.None };
      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(
          Constraint.Prohibition("test", "Test", "Do not test"),
          "Violation"
        )
      };

      // Act
      var constraints = policy.GenerateRetryConstraints(violations, 0);

      // Assert
      Assert.That(constraints.Count, Is.EqualTo(0));
    }

    [Test]
    public void GenerateRetryConstraints_AddSpecificProhibition_CreatesProhibition()
    {
      // Arrange
      var policy = new RetryPolicy { Escalation = ConstraintEscalation.AddSpecificProhibition };
      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(
          Constraint.Prohibition("test", "Test prohibition", "Do not test"),
          "Violation description",
          "violating text"
        )
      };

      // Act
      var constraints = policy.GenerateRetryConstraints(violations, 0);

      // Assert
      Assert.That(constraints.Count, Is.EqualTo(1));
      var prohibitions = constraints.Prohibitions.ToList();
      Assert.That(prohibitions.Count, Is.EqualTo(1));
      var prohibition = prohibitions[0];
      Assert.That(prohibition.Type, Is.EqualTo(ConstraintType.Prohibition));
      Assert.That(prohibition.Id, Does.Contain("RetryEscalation"));
    }

    [Test]
    public void GenerateRetryConstraints_WithViolatingText_IncludesPattern()
    {
      // Arrange
      var policy = new RetryPolicy { Escalation = ConstraintEscalation.AddSpecificProhibition };
      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(
          Constraint.Prohibition("test", "Test", "Do not test"),
          "Violation",
          "secret information"
        )
      };

      // Act
      var constraints = policy.GenerateRetryConstraints(violations, 1);

      // Assert
      var prohibitions = constraints.Prohibitions.ToList();
      Assert.That(prohibitions.Count, Is.EqualTo(1));
      var prohibition = prohibitions[0];
      Assert.That(prohibition.ValidationPatterns, Contains.Item("secret information"));
    }

    [Test]
    public void GenerateRetryConstraints_HardenRequirements_CreatesStrengthenedRequirement()
    {
      // Arrange
      var policy = new RetryPolicy { Escalation = ConstraintEscalation.HardenRequirements };
      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(
          Constraint.Requirement("req", "Must meet requirement", "Meet requirement"),
          "Requirement not met"
        )
      };

      // Act
      var constraints = policy.GenerateRetryConstraints(violations, 0);

      // Assert
      Assert.That(constraints.Count, Is.EqualTo(1));
      var requirements = constraints.Requirements.ToList();
      Assert.That(requirements.Count, Is.EqualTo(1));
      var requirement = requirements[0];
      Assert.That(requirement.Description, Does.Contain("MUST"));
    }

    [Test]
    public void GenerateRetryConstraints_FullEscalation_CreatesBoth()
    {
      // Arrange
      var policy = new RetryPolicy { Escalation = ConstraintEscalation.Full };
      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(
          Constraint.Prohibition("proh", "Prohibition", "Do not"),
          "Prohibition violated",
          "bad text"
        ),
        new ConstraintViolation(
          Constraint.Requirement("req", "Requirement", "Must"),
          "Requirement not met"
        )
      };

      // Act
      var constraints = policy.GenerateRetryConstraints(violations, 0);

      // Assert
      // Full escalation: prohibition violation creates 1 prohibition, requirement violation creates 1 prohibition + 1 requirement = 3 total
      Assert.That(constraints.Count, Is.EqualTo(3));
      Assert.That(constraints.Prohibitions.ToList().Count, Is.EqualTo(2)); // One from each violation
      Assert.That(constraints.Requirements.ToList().Count, Is.EqualTo(1)); // One from requirement violation
    }

    [Test]
    public void GenerateRetryConstraints_IncludesAttemptNumberInId()
    {
      // Arrange
      var policy = new RetryPolicy { Escalation = ConstraintEscalation.AddSpecificProhibition };
      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(
          Constraint.Prohibition("test", "Test", "Do not"),
          "Violation"
        )
      };

      // Act
      var constraints1 = policy.GenerateRetryConstraints(violations, 1);
      var constraints2 = policy.GenerateRetryConstraints(violations, 2);

      // Assert
      var prohibitions1 = constraints1.Prohibitions.ToList();
      var prohibitions2 = constraints2.Prohibitions.ToList();
      Assert.That(prohibitions1[0].Id, Does.Contain("_1"));
      Assert.That(prohibitions2[0].Id, Does.Contain("_2"));
    }

    [Test]
    public void GenerateRetryConstraints_WithoutViolatingText_StillCreatesProhibition()
    {
      // Arrange
      var policy = new RetryPolicy { Escalation = ConstraintEscalation.AddSpecificProhibition };
      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(
          Constraint.Prohibition("test", "Test prohibition", "Do not test"),
          "Violation description"
        )
      };

      // Act
      var constraints = policy.GenerateRetryConstraints(violations, 0);

      // Assert
      var prohibitions = constraints.Prohibitions.ToList();
      Assert.That(prohibitions.Count, Is.EqualTo(1));
      Assert.That(prohibitions[0].ValidationPatterns, Is.Empty);
    }

    #endregion

    #region GenerateRetryFeedback Tests

    [Test]
    public void GenerateRetryFeedback_IncludesRetryAttemptNumber()
    {
      // Arrange
      var policy = new RetryPolicy();
      var snapshot = new StateSnapshotBuilder().WithAttemptNumber(0).Build();
      var result = InferenceResult.FailedValidation(
        "Bad response",
        ValidationOutcome.ProhibitionViolated,
        new List<ConstraintViolation>(),
        snapshot,
        100
      );

      // Act
      var feedback = policy.GenerateRetryFeedback(result);

      // Assert
      Assert.That(feedback, Does.Contain("[RETRY ATTEMPT 2]"));
    }

    [Test]
    public void GenerateRetryFeedback_WithViolations_IncludesViolationDetails()
    {
      // Arrange
      var policy = new RetryPolicy { IncludeViolationFeedback = true };
      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(
          Constraint.Prohibition("test", "Test prohibition", "Do not test"),
          "First violation"
        ),
        new ConstraintViolation(
          Constraint.Requirement("req", "Test requirement", "Must test"),
          "Second violation"
        )
      };
      var result = InferenceResult.FailedValidation(
        "Bad response",
        ValidationOutcome.ProhibitionViolated,
        violations,
        _defaultSnapshot,
        100
      );

      // Act
      var feedback = policy.GenerateRetryFeedback(result);

      // Assert
      Assert.That(feedback, Does.Contain("Your previous response violated"));
      Assert.That(feedback, Does.Contain("First violation"));
      Assert.That(feedback, Does.Contain("Second violation"));
    }

    [Test]
    public void GenerateRetryFeedback_WithoutViolationFeedback_ExcludesViolations()
    {
      // Arrange
      var policy = new RetryPolicy { IncludeViolationFeedback = false };
      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(
          Constraint.Prohibition("test", "Test", "Do not"),
          "Violation"
        )
      };
      var result = InferenceResult.FailedValidation(
        "Bad response",
        ValidationOutcome.ProhibitionViolated,
        violations,
        _defaultSnapshot,
        100
      );

      // Act
      var feedback = policy.GenerateRetryFeedback(result);

      // Assert
      Assert.That(feedback, Does.Not.Contain("violated the following constraints"));
    }

    [Test]
    public void GenerateRetryFeedback_WithPreviousResponse_IncludesResponse()
    {
      // Arrange
      var policy = new RetryPolicy { IncludePreviousResponse = true };
      var result = InferenceResult.FailedValidation(
        "This is a bad response that should be included",
        ValidationOutcome.ProhibitionViolated,
        new List<ConstraintViolation>(),
        _defaultSnapshot,
        100
      );

      // Act
      var feedback = policy.GenerateRetryFeedback(result);

      // Assert
      Assert.That(feedback, Does.Contain("Previous response (rejected)"));
      Assert.That(feedback, Does.Contain("This is a bad response"));
    }

    [Test]
    public void GenerateRetryFeedback_WithoutPreviousResponse_ExcludesResponse()
    {
      // Arrange
      var policy = new RetryPolicy { IncludePreviousResponse = false };
      var result = InferenceResult.FailedValidation(
        "Bad response",
        ValidationOutcome.ProhibitionViolated,
        new List<ConstraintViolation>(),
        _defaultSnapshot,
        100
      );

      // Act
      var feedback = policy.GenerateRetryFeedback(result);

      // Assert
      Assert.That(feedback, Does.Not.Contain("Previous response"));
    }

    [Test]
    public void GenerateRetryFeedback_TruncatesLongResponse()
    {
      // Arrange
      var policy = new RetryPolicy { IncludePreviousResponse = true };
      var longResponse = new string('a', 300); // 300 characters
      var result = InferenceResult.FailedValidation(
        longResponse,
        ValidationOutcome.ProhibitionViolated,
        new List<ConstraintViolation>(),
        _defaultSnapshot,
        100
      );

      // Act
      var feedback = policy.GenerateRetryFeedback(result);

      // Assert
      Assert.That(feedback, Does.Contain("..."));
      // Should be truncated to 200 chars + "..."
    }

    [Test]
    public void GenerateRetryFeedback_IncludesFinalInstruction()
    {
      // Arrange
      var policy = new RetryPolicy();
      var result = InferenceResult.FailedValidation(
        "Bad response",
        ValidationOutcome.ProhibitionViolated,
        new List<ConstraintViolation>(),
        _defaultSnapshot,
        100
      );

      // Act
      var feedback = policy.GenerateRetryFeedback(result);

      // Assert
      Assert.That(feedback, Does.Contain("Please provide a new response that satisfies ALL constraints"));
    }

    #endregion
  }
}

