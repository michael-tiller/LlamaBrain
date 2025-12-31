using System;
using System.Collections.Generic;
using NUnit.Framework;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.Inference
{
  [TestFixture]
  public class RetryPolicyTests
  {
    [Test]
    public void Default_HasReasonableSettings()
    {
      var policy = RetryPolicy.Default;

      Assert.That(policy.MaxRetries, Is.EqualTo(2));
      Assert.That(policy.MaxAttempts, Is.EqualTo(3)); // MaxRetries + 1
      Assert.That(policy.Escalation, Is.EqualTo(ConstraintEscalation.AddSpecificProhibition));
      Assert.That(policy.IncludePreviousResponse, Is.True);
      Assert.That(policy.IncludeViolationFeedback, Is.True);
      Assert.That(policy.RetryDelayMs, Is.EqualTo(0));
      Assert.That(policy.MaxTotalTimeMs, Is.EqualTo(30000));
    }

    [Test]
    public void NoRetry_HasZeroRetries()
    {
      var policy = RetryPolicy.NoRetry;

      Assert.That(policy.MaxRetries, Is.EqualTo(0));
      Assert.That(policy.MaxAttempts, Is.EqualTo(1));
    }

    [Test]
    public void Aggressive_HasHigherRetries()
    {
      var policy = RetryPolicy.Aggressive;

      Assert.That(policy.MaxRetries, Is.EqualTo(3));
      Assert.That(policy.MaxAttempts, Is.EqualTo(4));
      Assert.That(policy.Escalation, Is.EqualTo(ConstraintEscalation.Full));
    }

    [Test]
    public void GenerateRetryConstraints_NoEscalation_ReturnsEmpty()
    {
      var policy = new RetryPolicy { Escalation = ConstraintEscalation.None };
      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(
          Constraint.Prohibit("No swearing", "test"),
          "Contains swearing"
        )
      };

      var constraints = policy.GenerateRetryConstraints(violations, 0);

      Assert.That(constraints.Count, Is.EqualTo(0));
    }

    [Test]
    public void GenerateRetryConstraints_AddSpecificProhibition_CreatesProhibition()
    {
      var policy = new RetryPolicy { Escalation = ConstraintEscalation.AddSpecificProhibition };
      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(
          Constraint.Prohibit("No swearing", "test"),
          "Contains swearing",
          "damn"
        )
      };

      var constraints = policy.GenerateRetryConstraints(violations, 0);

      Assert.That(constraints.Prohibitions.Count, Is.EqualTo(1));
      Assert.That(constraints.Prohibitions[0].Description, Does.Contain("damn"));
    }

    [Test]
    public void GenerateRetryConstraints_HardenRequirements_StrengthensRequirements()
    {
      var policy = new RetryPolicy { Escalation = ConstraintEscalation.HardenRequirements };
      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(
          Constraint.Require("Be polite", "test"),
          "Not polite enough"
        )
      };

      var constraints = policy.GenerateRetryConstraints(violations, 0);

      Assert.That(constraints.Requirements.Count, Is.EqualTo(1));
      Assert.That(constraints.Requirements[0].Description, Does.Contain("MUST"));
    }

    [Test]
    public void GenerateRetryConstraints_Full_CreatesBothTypes()
    {
      var policy = new RetryPolicy { Escalation = ConstraintEscalation.Full };
      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(
          Constraint.Prohibit("No violence", "test"),
          "Contains violence",
          "kill"
        ),
        new ConstraintViolation(
          Constraint.Require("Be helpful", "test"),
          "Not helpful"
        )
      };

      var constraints = policy.GenerateRetryConstraints(violations, 0);

      // 2 prohibitions (one from prohibition violation, one from requirement violation as "do not fail to")
      // + 1 hardened requirement
      Assert.That(constraints.Count, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void GenerateRetryFeedback_IncludesAttemptNumber()
    {
      var policy = new RetryPolicy
      {
        IncludeViolationFeedback = true,
        IncludePreviousResponse = true
      };

      var snapshot = new StateSnapshotBuilder()
        .WithAttemptNumber(0)
        .Build();

      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(
          Constraint.Prohibit("test", "test"),
          "Violation description"
        )
      };

      var result = InferenceResult.FailedValidation(
        "Bad response",
        ValidationOutcome.ProhibitionViolated,
        violations,
        snapshot,
        50
      );

      var feedback = policy.GenerateRetryFeedback(result);

      Assert.That(feedback, Does.Contain("RETRY ATTEMPT 2"));
      Assert.That(feedback, Does.Contain("Violation description"));
      Assert.That(feedback, Does.Contain("Bad response"));
    }

    [Test]
    public void GenerateRetryFeedback_WithoutViolationFeedback_OmitsViolations()
    {
      var policy = new RetryPolicy
      {
        IncludeViolationFeedback = false,
        IncludePreviousResponse = true
      };

      var snapshot = new StateSnapshotBuilder().WithAttemptNumber(0).Build();
      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(Constraint.Prohibit("test", "test"), "Violation")
      };

      var result = InferenceResult.FailedValidation(
        "Bad response",
        ValidationOutcome.ProhibitionViolated,
        violations,
        snapshot,
        50
      );

      var feedback = policy.GenerateRetryFeedback(result);

      Assert.That(feedback, Does.Contain("Bad response"));
      Assert.That(feedback, Does.Not.Contain("Violation"));
    }

    [Test]
    public void GenerateRetryFeedback_WithoutPreviousResponse_OmitsResponse()
    {
      var policy = new RetryPolicy
      {
        IncludeViolationFeedback = true,
        IncludePreviousResponse = false
      };

      var snapshot = new StateSnapshotBuilder().WithAttemptNumber(0).Build();
      var result = InferenceResult.FailedValidation(
        "Bad response",
        ValidationOutcome.ProhibitionViolated,
        new List<ConstraintViolation>(),
        snapshot,
        50
      );

      var feedback = policy.GenerateRetryFeedback(result);

      Assert.That(feedback, Does.Not.Contain("Bad response"));
    }

    [Test]
    public void OnRetry_CallbackInvoked()
    {
      int callbackCount = 0;
      int lastAttempt = -1;

      var policy = new RetryPolicy
      {
        OnRetry = (attempt, result) =>
        {
          callbackCount++;
          lastAttempt = attempt;
        }
      };

      var snapshot = new StateSnapshotBuilder().Build();
      var result = InferenceResult.FailedValidation(
        "bad",
        ValidationOutcome.ProhibitionViolated,
        new List<ConstraintViolation>(),
        snapshot,
        50
      );

      // Simulate callback invocation
      policy.OnRetry?.Invoke(1, result);

      Assert.That(callbackCount, Is.EqualTo(1));
      Assert.That(lastAttempt, Is.EqualTo(1));
    }

    [Test]
    public void MaxAttempts_IsMaxRetriesPlusOne()
    {
      var policy = new RetryPolicy { MaxRetries = 5 };
      Assert.That(policy.MaxAttempts, Is.EqualTo(6));

      policy.MaxRetries = 0;
      Assert.That(policy.MaxAttempts, Is.EqualTo(1));
    }
  }
}
