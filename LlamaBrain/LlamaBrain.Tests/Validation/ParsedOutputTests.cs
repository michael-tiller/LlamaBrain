using NUnit.Framework;
using LlamaBrain.Core.Validation;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.Validation
{
  /// <summary>
  /// Tests for ParsedOutput and related types.
  /// </summary>
  public class ParsedOutputTests
  {
    #region ParsedOutput Tests

    [Test]
    public void ParsedOutput_Dialogue_CreatesSuccessfulResult()
    {
      // Act
      var result = ParsedOutput.Dialogue("Hello there!", "raw output");

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.DialogueText, Is.EqualTo("Hello there!"));
      Assert.That(result.RawOutput, Is.EqualTo("raw output"));
      Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void ParsedOutput_Failed_CreatesFailedResult()
    {
      // Act
      var result = ParsedOutput.Failed("Parse error occurred", "raw output");

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Is.EqualTo("Parse error occurred"));
      Assert.That(result.RawOutput, Is.EqualTo("raw output"));
    }

    [Test]
    public void ParsedOutput_WithMutation_AddsMutation()
    {
      // Act
      var result = ParsedOutput.Dialogue("Hello!", "raw")
        .WithMutation(ProposedMutation.AppendEpisodic("Test memory"));

      // Assert
      Assert.That(result.ProposedMutations.Count, Is.EqualTo(1));
      Assert.That(result.HasStructuredData, Is.True);
    }

    [Test]
    public void ParsedOutput_WithIntent_AddsIntent()
    {
      // Act
      var result = ParsedOutput.Dialogue("Follow me!", "raw")
        .WithIntent(WorldIntent.Create("follow", "player"));

      // Assert
      Assert.That(result.WorldIntents.Count, Is.EqualTo(1));
      Assert.That(result.HasStructuredData, Is.True);
    }

    [Test]
    public void ParsedOutput_WithMetadata_AddsMetadata()
    {
      // Act
      var result = ParsedOutput.Dialogue("Hello!", "raw")
        .WithMetadata("key", "value");

      // Assert
      Assert.That(result.Metadata["key"], Is.EqualTo("value"));
    }

    [Test]
    public void ParsedOutput_NoStructuredData_ReturnsFalse()
    {
      // Act
      var result = ParsedOutput.Dialogue("Hello!", "raw");

      // Assert
      Assert.That(result.HasStructuredData, Is.False);
    }

    [Test]
    public void ParsedOutput_ToString_ReturnsFormattedString()
    {
      // Arrange
      var success = ParsedOutput.Dialogue("Hello there, traveler!", "raw");
      var failed = ParsedOutput.Failed("Error", "raw");

      // Assert
      Assert.That(success.ToString(), Does.Contain("OK"));
      Assert.That(failed.ToString(), Does.Contain("Failed"));
    }

    #endregion

    #region ProposedMutation Tests

    [Test]
    public void ProposedMutation_AppendEpisodic_CreatesCorrectType()
    {
      // Act
      var mutation = ProposedMutation.AppendEpisodic("Memory content", "source");

      // Assert
      Assert.That(mutation.Type, Is.EqualTo(MutationType.AppendEpisodic));
      Assert.That(mutation.Content, Is.EqualTo("Memory content"));
      Assert.That(mutation.SourceText, Is.EqualTo("source"));
    }

    [Test]
    public void ProposedMutation_TransformBelief_CreatesCorrectType()
    {
      // Act
      var mutation = ProposedMutation.TransformBelief("belief-id", "New belief", 0.8f, "source");

      // Assert
      Assert.That(mutation.Type, Is.EqualTo(MutationType.TransformBelief));
      Assert.That(mutation.Target, Is.EqualTo("belief-id"));
      Assert.That(mutation.Content, Is.EqualTo("New belief"));
      Assert.That(mutation.Confidence, Is.EqualTo(0.8f));
    }

    [Test]
    public void ProposedMutation_TransformRelationship_CreatesCorrectType()
    {
      // Act
      var mutation = ProposedMutation.TransformRelationship("player", "friendly", "source");

      // Assert
      Assert.That(mutation.Type, Is.EqualTo(MutationType.TransformRelationship));
      Assert.That(mutation.Target, Is.EqualTo("player"));
      Assert.That(mutation.Content, Is.EqualTo("friendly"));
    }

    [Test]
    public void ProposedMutation_EmitWorldIntent_CreatesCorrectType()
    {
      // Act
      var mutation = ProposedMutation.EmitWorldIntent("follow", "Follow the player", "source");

      // Assert
      Assert.That(mutation.Type, Is.EqualTo(MutationType.EmitWorldIntent));
      Assert.That(mutation.Target, Is.EqualTo("follow"));
      Assert.That(mutation.Content, Is.EqualTo("Follow the player"));
    }

    [Test]
    public void ProposedMutation_ToString_ReturnsFormattedString()
    {
      // Arrange
      var mutation = ProposedMutation.AppendEpisodic("Test content");

      // Assert
      Assert.That(mutation.ToString(), Does.Contain("AppendEpisodic"));
      Assert.That(mutation.ToString(), Does.Contain("Test content"));
    }

    #endregion

    #region WorldIntent Tests

    [Test]
    public void WorldIntent_Create_SetsProperties()
    {
      // Act
      var intent = WorldIntent.Create("follow_player", "player1", 5);

      // Assert
      Assert.That(intent.IntentType, Is.EqualTo("follow_player"));
      Assert.That(intent.Target, Is.EqualTo("player1"));
      Assert.That(intent.Priority, Is.EqualTo(5));
    }

    [Test]
    public void WorldIntent_Parameters_CanBeAdded()
    {
      // Arrange
      var intent = WorldIntent.Create("give_item", "player");
      intent.Parameters["item_id"] = "health_potion";
      intent.Parameters["quantity"] = "3";

      // Assert
      Assert.That(intent.Parameters["item_id"], Is.EqualTo("health_potion"));
      Assert.That(intent.Parameters["quantity"], Is.EqualTo("3"));
    }

    [Test]
    public void WorldIntent_ToString_ReturnsFormattedString()
    {
      // Arrange
      var intent = WorldIntent.Create("follow", "player1", 10);

      // Assert
      Assert.That(intent.ToString(), Does.Contain("follow"));
      Assert.That(intent.ToString(), Does.Contain("player1"));
      Assert.That(intent.ToString(), Does.Contain("10"));
    }

    #endregion

    #region ValidationFailure Tests

    [Test]
    public void ValidationFailure_ProhibitionViolated_CreatesCorrectFailure()
    {
      // Act
      var failure = ValidationFailure.ProhibitionViolated("Description", "violating text", "rule-id");

      // Assert
      Assert.That(failure.Reason, Is.EqualTo(ValidationFailureReason.ProhibitionViolated));
      Assert.That(failure.Description, Is.EqualTo("Description"));
      Assert.That(failure.ViolatingText, Is.EqualTo("violating text"));
      Assert.That(failure.ViolatedRule, Is.EqualTo("rule-id"));
    }

    [Test]
    public void ValidationFailure_CanonicalContradiction_HasCriticalSeverity()
    {
      // Act
      var failure = ValidationFailure.CanonicalContradiction("fact-id", "Fact content", "violating");

      // Assert
      Assert.That(failure.Reason, Is.EqualTo(ValidationFailureReason.CanonicalFactContradiction));
      Assert.That(failure.Severity, Is.EqualTo(ConstraintSeverity.Critical));
    }

    [Test]
    public void ValidationFailure_CanonicalMutation_HasCriticalSeverity()
    {
      // Act
      var failure = ValidationFailure.CanonicalMutation("fact-id");

      // Assert
      Assert.That(failure.Reason, Is.EqualTo(ValidationFailureReason.CanonicalMutationAttempt));
      Assert.That(failure.Severity, Is.EqualTo(ConstraintSeverity.Critical));
    }

    #endregion

    #region GateResult Tests

    [Test]
    public void GateResult_Pass_CreatesPassedResult()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("Hello!", "raw")
        .WithMutation(ProposedMutation.AppendEpisodic("Memory"))
        .WithIntent(WorldIntent.Create("follow"));

      // Act
      var result = GateResult.Pass(output);

      // Assert
      Assert.That(result.Passed, Is.True);
      Assert.That(result.ValidatedOutput, Is.EqualTo(output));
      Assert.That(result.ApprovedMutations.Count, Is.EqualTo(1));
      Assert.That(result.ApprovedIntents.Count, Is.EqualTo(1));
    }

    [Test]
    public void GateResult_Fail_CreatesFailedResult()
    {
      // Act
      var result = GateResult.Fail(
        ValidationFailure.ProhibitionViolated("Test", "text"),
        ValidationFailure.RequirementNotMet("Test2")
      );

      // Assert
      Assert.That(result.Passed, Is.False);
      Assert.That(result.Failures.Count, Is.EqualTo(2));
    }

    [Test]
    public void GateResult_HasCriticalFailure_ReturnsTrueForCritical()
    {
      // Act
      var result = GateResult.Fail(ValidationFailure.CanonicalContradiction("id", "content"));

      // Assert
      Assert.That(result.HasCriticalFailure, Is.True);
      Assert.That(result.ShouldRetry, Is.False);
    }

    [Test]
    public void GateResult_ShouldRetry_ReturnsTrueForNonCritical()
    {
      // Act
      var result = GateResult.Fail(ValidationFailure.ProhibitionViolated("desc", "text"));

      // Assert
      Assert.That(result.ShouldRetry, Is.True);
    }

    #endregion
  }
}
