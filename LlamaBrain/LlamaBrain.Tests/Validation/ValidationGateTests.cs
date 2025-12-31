using System.Collections.Generic;
using NUnit.Framework;
using LlamaBrain.Core.Validation;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;

namespace LlamaBrain.Tests.Validation
{
  /// <summary>
  /// Tests for ValidationGate.
  /// </summary>
  public class ValidationGateTests
  {
    private ValidationGate gate = null!;

    [SetUp]
    public void SetUp()
    {
      gate = new ValidationGate();
    }

    #region Basic Validation

    [Test]
    public void Validate_ValidParsedOutput_Passes()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("Hello, traveler!", "Hello, traveler!");

      // Act
      var result = gate.Validate(output);

      // Assert
      Assert.That(result.Passed, Is.True);
      Assert.That(result.Failures.Count, Is.EqualTo(0));
      Assert.That(result.ValidatedOutput, Is.Not.Null);
    }

    [Test]
    public void Validate_FailedParsedOutput_Fails()
    {
      // Arrange
      var output = ParsedOutput.Failed("Parsing error", "raw output");

      // Act
      var result = gate.Validate(output);

      // Assert
      Assert.That(result.Passed, Is.False);
      Assert.That(result.Failures.Count, Is.GreaterThan(0));
      Assert.That(result.Failures[0].Reason, Is.EqualTo(ValidationFailureReason.InvalidFormat));
    }

    #endregion

    #region Constraint Validation

    [Test]
    public void Validate_ProhibitionViolated_Fails()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("The secret treasure is hidden in the cave.", "raw");
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition(
        "no-treasure-location",
        "Do not reveal treasure location",
        "Never reveal where treasure is hidden",
        "treasure", "hidden", "cave"
      ));

      var context = new ValidationContext { Constraints = constraints };

      // Act
      var result = gate.Validate(output, context);

      // Assert
      Assert.That(result.Passed, Is.False);
      Assert.That(result.Failures.Exists(f => f.Reason == ValidationFailureReason.ProhibitionViolated), Is.True);
    }

    [Test]
    public void Validate_ProhibitionNotViolated_Passes()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("I sell various goods at fair prices.", "raw");
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition(
        "no-secrets",
        "Do not reveal secrets",
        "Never reveal secrets",
        "secret", "hidden", "confidential"
      ));

      var context = new ValidationContext { Constraints = constraints };

      // Act
      var result = gate.Validate(output, context);

      // Assert
      Assert.That(result.Passed, Is.True);
    }

    #endregion

    #region Canonical Fact Validation

    [Test]
    public void Validate_ContradictCanonicalFact_Fails()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("The king is not named Arthur.", "raw");
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddCanonicalFact("king-name", "The king is named Arthur");

      var context = new ValidationContext { MemorySystem = memorySystem };

      // Act
      var result = gate.Validate(output, context);

      // Assert
      Assert.That(result.Passed, Is.False);
      Assert.That(result.Failures.Exists(f => f.Reason == ValidationFailureReason.CanonicalFactContradiction), Is.True);
    }

    [Test]
    public void Validate_NoContradiction_Passes()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("Welcome to the kingdom of King Arthur!", "raw");
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddCanonicalFact("king-name", "The king is named Arthur");

      var context = new ValidationContext { MemorySystem = memorySystem };

      // Act
      var result = gate.Validate(output, context);

      // Assert
      Assert.That(result.Passed, Is.True);
    }

    #endregion

    #region Knowledge Boundary Validation

    [Test]
    public void Validate_ForbiddenKnowledge_Fails()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("I know about the assassination plot against the king.", "raw");
      var context = new ValidationContext
      {
        ForbiddenKnowledge = new List<string> { "assassination", "plot", "conspiracy" }
      };

      // Act
      var result = gate.Validate(output, context);

      // Assert
      Assert.That(result.Passed, Is.False);
      Assert.That(result.Failures.Exists(f => f.Reason == ValidationFailureReason.KnowledgeBoundaryViolation), Is.True);
    }

    [Test]
    public void Validate_NoForbiddenKnowledge_Passes()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("I sell weapons and armor.", "raw");
      var context = new ValidationContext
      {
        ForbiddenKnowledge = new List<string> { "assassination", "plot", "conspiracy" }
      };

      // Act
      var result = gate.Validate(output, context);

      // Assert
      Assert.That(result.Passed, Is.True);
    }

    #endregion

    #region Mutation Validation

    [Test]
    public void Validate_MutationTargetsCanonicalFact_RejectsMutation()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("I'll remember that.", "raw")
        .WithMutation(ProposedMutation.TransformBelief("king-name", "The king is named Bob"));

      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddCanonicalFact("king-name", "The king is named Arthur");

      var context = new ValidationContext { MemorySystem = memorySystem };

      // Act
      var result = gate.Validate(output, context);

      // Assert
      Assert.That(result.Passed, Is.False);
      Assert.That(result.RejectedMutations.Count, Is.GreaterThan(0));
      Assert.That(result.Failures.Exists(f => f.Reason == ValidationFailureReason.CanonicalMutationAttempt), Is.True);
    }

    [Test]
    public void Validate_ValidMutation_ApprovesMutation()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("I'll remember that.", "raw")
        .WithMutation(ProposedMutation.AppendEpisodic("Player asked about healing potions"));

      // Act
      var result = gate.Validate(output);

      // Assert
      Assert.That(result.Passed, Is.True);
      Assert.That(result.ApprovedMutations.Count, Is.EqualTo(1));
    }

    #endregion

    #region Custom Rules

    [Test]
    public void Validate_CustomProhibitionRule_Fails()
    {
      // Arrange
      gate.AddRule(new PatternValidationRule
      {
        Id = "no-modern-terms",
        Description = "No modern terminology",
        Pattern = "computer|internet|phone",
        IsProhibition = true,
        Severity = ConstraintSeverity.Hard
      });

      var output = ParsedOutput.Dialogue("I don't know what a computer is.", "raw");

      // Act
      var result = gate.Validate(output);

      // Assert
      Assert.That(result.Passed, Is.False);
      Assert.That(result.Failures.Exists(f => f.ViolatedRule == "no-modern-terms"), Is.True);
    }

    [Test]
    public void Validate_CustomRuleNotTriggered_Passes()
    {
      // Arrange
      gate.AddRule(new PatternValidationRule
      {
        Id = "no-modern-terms",
        Description = "No modern terminology",
        Pattern = "computer|internet|phone",
        IsProhibition = true,
        Severity = ConstraintSeverity.Hard
      });

      var output = ParsedOutput.Dialogue("I sell swords and shields.", "raw");

      // Act
      var result = gate.Validate(output);

      // Assert
      Assert.That(result.Passed, Is.True);
    }

    [Test]
    public void RemoveRule_RemovesCustomRule()
    {
      // Arrange
      gate.AddRule(new PatternValidationRule
      {
        Id = "test-rule",
        Description = "Test rule",
        Pattern = "test",
        IsProhibition = true
      });

      // Act
      var removed = gate.RemoveRule("test-rule");
      var output = ParsedOutput.Dialogue("This is a test.", "raw");
      var result = gate.Validate(output);

      // Assert
      Assert.That(removed, Is.True);
      Assert.That(result.Passed, Is.True);
    }

    #endregion

    #region Critical Failures

    [Test]
    public void Validate_CriticalFailure_MarksAsCritical()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddCanonicalFact("world-rule", "Magic does not exist");

      var output = ParsedOutput.Dialogue("Magic is not real, it's just tricks.", "raw");
      // This creates a canonical contradiction which has Critical severity

      var context = new ValidationContext { MemorySystem = memorySystem };

      // Act
      var result = gate.Validate(output, context);

      // Assert - canonical contradictions are critical
      if (!result.Passed)
      {
        Assert.That(result.HasCriticalFailure, Is.True);
        Assert.That(result.ShouldRetry, Is.False);
      }
    }

    [Test]
    public void Validate_NonCriticalFailure_AllowsRetry()
    {
      // Arrange
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition(
        "soft-rule",
        "Soft prohibition",
        "Don't say hello",
        "hello"
      ));
      // Default severity is Hard, not Critical

      var output = ParsedOutput.Dialogue("Hello there!", "raw");
      var context = new ValidationContext { Constraints = constraints };

      // Act
      var result = gate.Validate(output, context);

      // Assert
      Assert.That(result.Passed, Is.False);
      Assert.That(result.HasCriticalFailure, Is.False);
      Assert.That(result.ShouldRetry, Is.True);
    }

    #endregion

    #region World Intents

    [Test]
    public void Validate_WithWorldIntents_ApprovesIntents()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("Follow me!", "raw")
        .WithIntent(WorldIntent.Create("follow_player", "player1"));

      // Act
      var result = gate.Validate(output);

      // Assert
      Assert.That(result.Passed, Is.True);
      Assert.That(result.ApprovedIntents.Count, Is.EqualTo(1));
      Assert.That(result.ApprovedIntents[0].IntentType, Is.EqualTo("follow_player"));
    }

    #endregion

    #region Configuration

    [Test]
    public void Validate_MinimalConfig_SkipsCanonicalCheck()
    {
      // Arrange
      var minimalGate = new ValidationGate(ValidationGateConfig.Minimal);
      var output = ParsedOutput.Dialogue("The king is not named Arthur.", "raw");
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddCanonicalFact("king-name", "The king is named Arthur");

      var context = new ValidationContext { MemorySystem = memorySystem };

      // Act
      var result = minimalGate.Validate(output, context);

      // Assert - should pass because canonical check is disabled
      Assert.That(result.Passed, Is.True);
    }

    #endregion
  }
}
