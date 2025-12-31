using System;
using NUnit.Framework;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.Inference
{
  [TestFixture]
  public class ResponseValidatorTests
  {
    private ResponseValidator _validator;

    [SetUp]
    public void SetUp()
    {
      _validator = new ResponseValidator();
    }

    [Test]
    public void Validate_EmptyResponse_ReturnsInvalidFormat()
    {
      var constraints = new ConstraintSet();
      var result = _validator.Validate("", constraints);

      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.InvalidFormat));
      Assert.That(result.ErrorMessage, Does.Contain("empty"));
    }

    [Test]
    public void Validate_WhitespaceResponse_ReturnsInvalidFormat()
    {
      var constraints = new ConstraintSet();
      var result = _validator.Validate("   \n\t  ", constraints);

      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.InvalidFormat));
    }

    [Test]
    public void Validate_NoConstraints_ReturnsValid()
    {
      var constraints = new ConstraintSet();
      var result = _validator.Validate("Hello, how can I help you?", constraints);

      Assert.That(result.IsValid, Is.True);
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.Valid));
      Assert.That(result.Violations.Count, Is.EqualTo(0));
    }

    [Test]
    public void Validate_ProhibitionViolated_ReturnsProhibitionViolated()
    {
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibit("Do not say \"secret\"", "test"));

      var result = _validator.Validate("I'll tell you the secret password.", constraints);

      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
      Assert.That(result.Violations.Count, Is.EqualTo(1));
    }

    [Test]
    public void Validate_ProhibitionNotViolated_ReturnsValid()
    {
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibit("Do not say \"secret\"", "test"));

      var result = _validator.Validate("Hello, I'm here to help.", constraints);

      Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_RequirementMet_ReturnsValid()
    {
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Require("Must mention \"greeting\"", "test"));

      var result = _validator.Validate("Here's a friendly greeting for you!", constraints);

      Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_RequirementNotMet_ReturnsRequirementNotMet()
    {
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Require("Must mention \"hello\"", "test"));

      var result = _validator.Validate("How can I assist you today?", constraints);

      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.RequirementNotMet));
    }

    [Test]
    public void Validate_MultipleConstraints_AllMet()
    {
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibit("Do not say \"bad\"", "test"));
      constraints.Add(Constraint.Require("Must say \"good\"", "test"));

      var result = _validator.Validate("This is a good response!", constraints);

      Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_ProhibitionTakesPrecedence()
    {
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibit("Do not say \"bad\"", "test"));
      constraints.Add(Constraint.Require("Must say \"good\"", "test"));

      // Response has "good" (meets requirement) but also "bad" (violates prohibition)
      var result = _validator.Validate("This is good but also bad.", constraints);

      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Outcome, Is.EqualTo(ValidationOutcome.ProhibitionViolated));
    }

    [Test]
    public void Validate_CaseInsensitive()
    {
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibit("Do not say \"SECRET\"", "test"));

      var result = _validator.Validate("I know the secret!", constraints);

      Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_ExtractsQuotedPatterns()
    {
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibit("Never mention 'password' in any form", "test"));

      var result = _validator.Validate("Here's your password.", constraints);

      Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_ViolationIncludesContext()
    {
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibit("Do not say \"forbidden\"", "test"));

      var result = _validator.Validate("This is forbidden information.", constraints);

      Assert.That(result.Violations.Count, Is.EqualTo(1));
      Assert.That(result.Violations[0].ViolatingText, Does.Contain("forbidden"));
    }

    [Test]
    public void Validate_WithSnapshot_UsesSnapshotConstraints()
    {
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibit("No \"magic\"", "test"));

      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .WithPlayerInput("Tell me about magic")
        .Build();

      var result = _validator.Validate("Magic is real!", snapshot);

      Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_DescriptiveRequirement_PassesWithoutPatterns()
    {
      var constraints = new ConstraintSet();
      // This requirement has no quoted patterns to match
      constraints.Add(Constraint.Require("Be polite and helpful", "test"));

      var result = _validator.Validate("I'd be happy to assist you.", constraints);

      // Without specific patterns, descriptive requirements pass
      Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidationResult_ToString_FormatsCorrectly()
    {
      var constraints = new ConstraintSet();
      var validResult = _validator.Validate("Hello!", constraints);
      Assert.That(validResult.ToString(), Does.Contain("Valid"));

      constraints.Add(Constraint.Prohibit("No \"test\"", "test"));
      var invalidResult = _validator.Validate("This is a test.", constraints);
      Assert.That(invalidResult.ToString(), Does.Contain("ProhibitionViolated"));
    }
  }
}
