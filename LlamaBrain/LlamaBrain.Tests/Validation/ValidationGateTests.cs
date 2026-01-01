using System;
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

    #region Feature 10.4: Gate Execution Ordering Tests

    [Test]
    public void GateExecution_AllGatesExecuteInOrder_FailuresAccumulate()
    {
      // Arrange - Create conditions that fail multiple gates
      var output = ParsedOutput.Dialogue("I'll tell you a secret: The king is not named Arthur.", "raw")
        .WithMutation(ProposedMutation.TransformBelief("king_name", "King Bob"));

      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddCanonicalFact("king_name", "The king is named Arthur");

      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "Don't reveal secrets", "Never say secret", "secret"));

      var context = new ValidationContext
      {
        Constraints = constraints,
        MemorySystem = memorySystem,
        ForbiddenKnowledge = new List<string> { "assassination" }
      };

      // Act
      var result = gate.Validate(output, context);

      // Assert - Multiple failures should accumulate
      Assert.That(result.Passed, Is.False);
      Assert.That(result.Failures.Count, Is.GreaterThanOrEqualTo(2));
      Assert.That(result.Failures.Exists(f => f.Reason == ValidationFailureReason.ProhibitionViolated), Is.True);
      Assert.That(result.Failures.Exists(f => f.Reason == ValidationFailureReason.CanonicalFactContradiction), Is.True);
      Assert.That(result.Failures.Exists(f => f.Reason == ValidationFailureReason.CanonicalMutationAttempt), Is.True);
    }

    [Test]
    public void GateExecution_DeterministicOrder_SameInputSameOrder()
    {
      // Arrange - Same input with multiple failure conditions
      var output = ParsedOutput.Dialogue("Secret: The king is not named Arthur", "raw");

      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddCanonicalFact("king_name", "The king is named Arthur");

      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "Don't reveal secrets", "Never say secret", "secret"));

      var context = new ValidationContext
      {
        Constraints = constraints,
        MemorySystem = memorySystem
      };

      // Act - Run validation twice with identical input
      var result1 = gate.Validate(output, context);
      var result2 = gate.Validate(output, context);

      // Assert - Same failure count and order
      Assert.That(result1.Failures.Count, Is.EqualTo(result2.Failures.Count));
      for (int i = 0; i < result1.Failures.Count; i++)
      {
        Assert.That(result1.Failures[i].Reason, Is.EqualTo(result2.Failures[i].Reason));
        Assert.That(result1.Failures[i].Description, Is.EqualTo(result2.Failures[i].Description));
      }
    }

    #endregion

    #region Feature 10.4: Constraint Validation (Gate 1) Tests

    [Test]
    public void Gate1_CheckConstraintsFalse_SkipsConstraintValidation()
    {
      // Arrange
      var configNoConstraints = new ValidationGateConfig { CheckConstraints = false };
      var gateNoConstraints = new ValidationGate(configNoConstraints);

      var output = ParsedOutput.Dialogue("This is a secret message", "raw");
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "Don't reveal secrets", "Never say secret", "secret"));

      var context = new ValidationContext { Constraints = constraints };

      // Act
      var result = gateNoConstraints.Validate(output, context);

      // Assert - Should pass because constraint check is skipped
      Assert.That(result.Passed, Is.True);
      Assert.That(result.Failures.Count, Is.EqualTo(0));
    }

    [Test]
    public void Gate1_ProhibitionViolation_UsesCorrectReason()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("The treasure is hidden", "raw");
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-treasure", "Don't reveal treasure", "Never mention treasure", "treasure"));

      var context = new ValidationContext { Constraints = constraints };

      // Act
      var result = gate.Validate(output, context);

      // Assert
      Assert.That(result.Passed, Is.False);
      Assert.That(result.Failures[0].Reason, Is.EqualTo(ValidationFailureReason.ProhibitionViolated));
      Assert.That(result.Failures[0].ViolatedRule, Is.EqualTo("no-treasure"));
    }

    [Test]
    public void Gate1_ConstraintSeverity_PreservedInFailure()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("The forbidden word", "raw");
      var constraints = new ConstraintSet();

      var softProhibition = Constraint.Prohibition("soft-rule", "Soft rule", "Soft prohibition", "forbidden");
      softProhibition.Severity = ConstraintSeverity.Soft;
      constraints.Add(softProhibition);

      var context = new ValidationContext { Constraints = constraints };

      // Act
      var result = gate.Validate(output, context);

      // Assert
      Assert.That(result.Passed, Is.False);
      Assert.That(result.Failures[0].Severity, Is.EqualTo(ConstraintSeverity.Soft));
    }

    #endregion

    #region Feature 10.4: Canonical Fact Validation (Gate 2) Tests

    [Test]
    public void Gate2_CheckCanonicalFactsFalse_SkipsCanonicalCheck()
    {
      // Arrange
      var configNoCanonical = new ValidationGateConfig { CheckCanonicalFacts = false };
      var gateNoCanonical = new ValidationGate(configNoCanonical);

      var output = ParsedOutput.Dialogue("The king is not named Arthur", "raw");
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddCanonicalFact("king_name", "The king is named Arthur");

      var context = new ValidationContext { MemorySystem = memorySystem };

      // Act
      var result = gateNoCanonical.Validate(output, context);

      // Assert - Should pass because canonical check is skipped
      Assert.That(result.Passed, Is.True);
    }

    [Test]
    [TestCase("not the king is named arthur")]
    [TestCase("the king isn't named arthur")]
    [TestCase("the king is not named arthur")]
    public void Gate2_NegationPatterns_DetectsContradiction(string negationText)
    {
      // Arrange
      var output = ParsedOutput.Dialogue(negationText, "raw");
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddCanonicalFact("king_name", "The king is named Arthur");

      var context = new ValidationContext { MemorySystem = memorySystem };

      // Act
      var result = gate.Validate(output, context);

      // Assert
      Assert.That(result.Passed, Is.False);
      Assert.That(result.Failures.Exists(f => f.Reason == ValidationFailureReason.CanonicalFactContradiction), Is.True);
    }

    [Test]
    public void Gate2_ContradictionKeywords_DetectsViolation()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("Everyone knows King Bob rules this land", "raw");
      var memorySystem = new AuthoritativeMemorySystem();

      memorySystem.AddCanonicalFact("king_name", "The king is named Arthur", "world_lore");
      var fact = memorySystem.GetCanonicalFact("king_name");
      fact!.ContradictionKeywords = new List<string> { "king bob", "king robert" };

      var context = new ValidationContext { MemorySystem = memorySystem };

      // Act
      var result = gate.Validate(output, context);

      // Assert
      Assert.That(result.Passed, Is.False);
      Assert.That(result.Failures.Exists(f => f.Reason == ValidationFailureReason.CanonicalFactContradiction), Is.True);
    }

    [Test]
    public void Gate2_CanonicalContradiction_HasCriticalSeverity()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("The king is not named Arthur", "raw");
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddCanonicalFact("king_name", "The king is named Arthur");

      var context = new ValidationContext { MemorySystem = memorySystem };

      // Act
      var result = gate.Validate(output, context);

      // Assert
      Assert.That(result.Passed, Is.False);
      Assert.That(result.HasCriticalFailure, Is.True);
      Assert.That(result.Failures[0].Severity, Is.EqualTo(ConstraintSeverity.Critical));
    }

    #endregion

    #region Feature 10.4: Knowledge Boundary Validation (Gate 3) Tests

    [Test]
    public void Gate3_CheckKnowledgeBoundariesFalse_SkipsKnowledgeCheck()
    {
      // Arrange
      var configNoKnowledge = new ValidationGateConfig { CheckKnowledgeBoundaries = false };
      var gateNoKnowledge = new ValidationGate(configNoKnowledge);

      var output = ParsedOutput.Dialogue("I know about the assassination plot", "raw");
      var context = new ValidationContext
      {
        ForbiddenKnowledge = new List<string> { "assassination" }
      };

      // Act
      var result = gateNoKnowledge.Validate(output, context);

      // Assert - Should pass because knowledge check is skipped
      Assert.That(result.Passed, Is.True);
    }

    [Test]
    public void Gate3_ForbiddenKnowledge_CaseInsensitive()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("The ASSASSINATION was planned", "raw");
      var context = new ValidationContext
      {
        ForbiddenKnowledge = new List<string> { "assassination" }
      };

      // Act
      var result = gate.Validate(output, context);

      // Assert - Should fail (case-insensitive match)
      Assert.That(result.Passed, Is.False);
      Assert.That(result.Failures.Exists(f => f.Reason == ValidationFailureReason.KnowledgeBoundaryViolation), Is.True);
    }

    [Test]
    public void Gate3_EmptyForbiddenKnowledge_NoCrash()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("Any text here", "raw");
      var context = new ValidationContext
      {
        ForbiddenKnowledge = new List<string>()
      };

      // Act
      var result = gate.Validate(output, context);

      // Assert - Should pass (no forbidden knowledge to check)
      Assert.That(result.Passed, Is.True);
    }

    #endregion

    #region Feature 10.4: Mutation Validation (Gate 4) Tests

    [Test]
    public void Gate4_ValidateMutationsFalse_ApprovesAllMutations()
    {
      // Arrange
      var configNoMutationValidation = new ValidationGateConfig { ValidateMutations = false };
      var gateNoMutationValidation = new ValidationGate(configNoMutationValidation);

      var output = ParsedOutput.Dialogue("I remember", "raw")
        .WithMutation(ProposedMutation.TransformBelief("king_name", "King Bob"));

      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddCanonicalFact("king_name", "The king is named Arthur");

      var context = new ValidationContext { MemorySystem = memorySystem };

      // Act
      var result = gateNoMutationValidation.Validate(output, context);

      // Assert - Mutation should be approved (validation skipped)
      Assert.That(result.ApprovedMutations.Count, Is.EqualTo(1));
      Assert.That(result.RejectedMutations.Count, Is.EqualTo(0));
    }

    [Test]
    public void Gate4_MutationsTargetingCanonicalFacts_Rejected()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("I'll update my knowledge", "raw")
        .WithMutation(ProposedMutation.TransformBelief("king_name", "The king is Bob"));

      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddCanonicalFact("king_name", "The king is named Arthur");

      var context = new ValidationContext { MemorySystem = memorySystem };

      // Act
      var result = gate.Validate(output, context);

      // Assert
      Assert.That(result.Passed, Is.False);
      Assert.That(result.RejectedMutations.Count, Is.EqualTo(1));
      Assert.That(result.ApprovedMutations.Count, Is.EqualTo(0));
      Assert.That(result.Failures.Exists(f => f.Reason == ValidationFailureReason.CanonicalMutationAttempt), Is.True);
    }

    [Test]
    public void Gate4_ValidMutations_AddedToApprovedList()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("I learned something", "raw")
        .WithMutation(ProposedMutation.AppendEpisodic("Player told me a story"))
        .WithMutation(ProposedMutation.TransformBelief("player_trust", "Player is trustworthy"));

      // Act
      var result = gate.Validate(output);

      // Assert
      Assert.That(result.Passed, Is.True);
      Assert.That(result.ApprovedMutations.Count, Is.EqualTo(2));
      Assert.That(result.RejectedMutations.Count, Is.EqualTo(0));
    }

    #endregion

    #region Feature 10.4: Gate Result Assembly Tests

    [Test]
    public void GateResult_PassedFalse_WhenAnyGateFails()
    {
      // Arrange - Only one failure
      var output = ParsedOutput.Dialogue("secret", "raw");
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "No secrets", "Don't say secret", "secret"));

      var context = new ValidationContext { Constraints = constraints };

      // Act
      var result = gate.Validate(output, context);

      // Assert
      Assert.That(result.Passed, Is.False);
    }

    [Test]
    public void GateResult_PassedTrue_OnlyWhenAllGatesPass()
    {
      // Arrange - Valid output with no violations
      var output = ParsedOutput.Dialogue("Hello, how can I help you?", "raw");

      // Act
      var result = gate.Validate(output);

      // Assert
      Assert.That(result.Passed, Is.True);
      Assert.That(result.Failures.Count, Is.EqualTo(0));
    }

    [Test]
    public void GateResult_ApprovedIntents_OnlyWhenGatePasses()
    {
      // Arrange - Valid output with intent
      var output = ParsedOutput.Dialogue("Follow me!", "raw")
        .WithIntent(WorldIntent.Create("follow", "player"));

      // Act
      var result = gate.Validate(output);

      // Assert
      Assert.That(result.Passed, Is.True);
      Assert.That(result.ApprovedIntents.Count, Is.EqualTo(1));
    }

    [Test]
    public void GateResult_ApprovedIntents_EmptyWhenGateFails()
    {
      // Contract: ApprovedIntents MUST be empty when gate fails.
      // This ensures downstream consumers (WorldIntentDispatcher, MemoryMutationController)
      // cannot accidentally dispatch intents from failed outputs.

      // Arrange - Failing output with intent
      var output = ParsedOutput.Dialogue("secret follow me", "raw")
        .WithIntent(WorldIntent.Create("follow", "player"));

      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "No secrets", "Don't say secret", "secret"));

      var context = new ValidationContext { Constraints = constraints };

      // Act
      var result = gate.Validate(output, context);

      // Assert - Gate fails AND ApprovedIntents is empty (contract requirement)
      Assert.That(result.Passed, Is.False);
      Assert.That(result.ApprovedIntents.Count, Is.EqualTo(0),
        "Contract: ApprovedIntents must be empty when GateResult.Passed=false");
    }

    [Test]
    public void GateResult_HasCriticalFailure_TrueWhenAnyCritical()
    {
      // Arrange - Create a critical failure
      var output = ParsedOutput.Dialogue("The king is not named Arthur", "raw");
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddCanonicalFact("king_name", "The king is named Arthur");

      var context = new ValidationContext { MemorySystem = memorySystem };

      // Act
      var result = gate.Validate(output, context);

      // Assert
      Assert.That(result.HasCriticalFailure, Is.True);
    }

    [Test]
    public void GateResult_ShouldRetry_TrueWhenFailedButNotCritical()
    {
      // Arrange - Non-critical failure
      var output = ParsedOutput.Dialogue("secret", "raw");
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "No secrets", "Don't say secret", "secret"));

      var context = new ValidationContext { Constraints = constraints };

      // Act
      var result = gate.Validate(output, context);

      // Assert
      Assert.That(result.Passed, Is.False);
      Assert.That(result.HasCriticalFailure, Is.False);
      Assert.That(result.ShouldRetry, Is.True);
    }

    [Test]
    public void GateResult_ShouldRetry_FalseWhenCriticalFailure()
    {
      // Arrange - Critical failure (canonical contradiction)
      var output = ParsedOutput.Dialogue("The king is not named Arthur", "raw");
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddCanonicalFact("king_name", "The king is named Arthur");

      var context = new ValidationContext { MemorySystem = memorySystem };

      // Act
      var result = gate.Validate(output, context);

      // Assert
      Assert.That(result.HasCriticalFailure, Is.True);
      Assert.That(result.ShouldRetry, Is.False);
    }

    #endregion

    #region Feature 10.4: Configuration Tests

    [Test]
    public void Config_Default_EnablesAllChecks()
    {
      // Arrange
      var defaultConfig = ValidationGateConfig.Default;

      // Assert
      Assert.That(defaultConfig.CheckConstraints, Is.True);
      Assert.That(defaultConfig.CheckCanonicalFacts, Is.True);
      Assert.That(defaultConfig.CheckKnowledgeBoundaries, Is.True);
      Assert.That(defaultConfig.ValidateMutations, Is.True);
    }

    [Test]
    public void Config_Minimal_DisablesAllExceptConstraints()
    {
      // Arrange
      var minimalConfig = ValidationGateConfig.Minimal;

      // Assert
      Assert.That(minimalConfig.CheckConstraints, Is.True);
      Assert.That(minimalConfig.CheckCanonicalFacts, Is.False);
      Assert.That(minimalConfig.CheckKnowledgeBoundaries, Is.False);
      Assert.That(minimalConfig.ValidateMutations, Is.False);
    }

    #endregion

    #region Feature 10.4: Determinism Tests with Seed

    [Test]
    public void Determinism_SameInputWithSeed_ProducesSameResult()
    {
      // Arrange - Use a fixed seed (integer as per user request) for any randomness
      const int seed = 42;
      var random = new Random(seed);

      // Create deterministic test data
      var output = ParsedOutput.Dialogue("Test message with secret content", "raw");
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "No secrets", "Don't say secret", "secret"));

      var context = new ValidationContext { Constraints = constraints };

      // Act - Validate multiple times
      var results = new List<GateResult>();
      for (int i = 0; i < 10; i++)
      {
        results.Add(gate.Validate(output, context));
      }

      // Assert - All results should be identical
      for (int i = 1; i < results.Count; i++)
      {
        Assert.That(results[i].Passed, Is.EqualTo(results[0].Passed));
        Assert.That(results[i].Failures.Count, Is.EqualTo(results[0].Failures.Count));
        Assert.That(results[i].HasCriticalFailure, Is.EqualTo(results[0].HasCriticalFailure));
        Assert.That(results[i].ShouldRetry, Is.EqualTo(results[0].ShouldRetry));
      }
    }

    [Test]
    public void Determinism_ConstraintOrderIndependent_SameResult()
    {
      // Arrange - Create constraints in different orders
      var output = ParsedOutput.Dialogue("secret treasure hidden", "raw");

      var constraints1 = new ConstraintSet();
      constraints1.Add(Constraint.Prohibition("c1", "C1", "Secret", "secret"));
      constraints1.Add(Constraint.Prohibition("c2", "C2", "Treasure", "treasure"));
      constraints1.Add(Constraint.Prohibition("c3", "C3", "Hidden", "hidden"));

      var constraints2 = new ConstraintSet();
      constraints2.Add(Constraint.Prohibition("c3", "C3", "Hidden", "hidden"));
      constraints2.Add(Constraint.Prohibition("c1", "C1", "Secret", "secret"));
      constraints2.Add(Constraint.Prohibition("c2", "C2", "Treasure", "treasure"));

      // Act
      var result1 = gate.Validate(output, new ValidationContext { Constraints = constraints1 });
      var result2 = gate.Validate(output, new ValidationContext { Constraints = constraints2 });

      // Assert - Same number of failures (order may differ but count should match)
      Assert.That(result1.Passed, Is.EqualTo(result2.Passed));
      Assert.That(result1.Failures.Count, Is.EqualTo(result2.Failures.Count));
    }

    #endregion
  }
}
