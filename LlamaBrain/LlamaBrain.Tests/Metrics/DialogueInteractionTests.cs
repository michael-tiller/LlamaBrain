using System;
using System.Collections.Generic;
using NUnit.Framework;
using LlamaBrain.Core;
using LlamaBrain.Core.Metrics;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Validation;

namespace LlamaBrain.Tests.Metrics
{
  /// <summary>
  /// Tests for DialogueInteraction and DialogueMetricsCollection classes.
  /// </summary>
  public class DialogueInteractionTests
  {
    #region Test Helpers

    /// <summary>
    /// Mock implementation of ITriggerInfo for testing.
    /// </summary>
    private class MockTriggerInfo : ITriggerInfo
    {
      public Guid Id { get; set; } = Guid.NewGuid();
      public string Name { get; set; } = "TestTrigger";
      public string PromptText { get; set; } = "Hello NPC";
      public int PromptCount { get; set; } = 1;
    }

    /// <summary>
    /// Mock implementation of IAgentMetrics for testing.
    /// </summary>
    private class MockAgentMetrics : IAgentMetrics
    {
      public InferenceResultWithRetries? LastInferenceResult { get; set; }
      public GateResult? LastGateResult { get; set; }
      public ConstraintSet? LastConstraints { get; set; }
      public StateSnapshot? LastSnapshot { get; set; }
      public IFallbackStats? FallbackStats { get; set; }
    }

    private CompletionMetrics CreateTestMetrics(
      string content = "Hello there!",
      int promptTokens = 100,
      int generatedTokens = 20,
      long ttftMs = 50,
      long prefillMs = 100,
      long decodeMs = 200,
      long totalMs = 300,
      int cachedTokens = 10)
    {
      return new CompletionMetrics
      {
        Content = content,
        PromptTokenCount = promptTokens,
        GeneratedTokenCount = generatedTokens,
        TtftMs = ttftMs,
        PrefillTimeMs = prefillMs,
        DecodeTimeMs = decodeMs,
        TotalTimeMs = totalMs,
        CachedTokenCount = cachedTokens
      };
    }

    #endregion

    #region DialogueInteraction Default Values Tests

    [Test]
    public void DialogueInteraction_DefaultValues_AreCorrect()
    {
      // Act
      var interaction = new DialogueInteraction();

      // Assert - String properties default to empty
      Assert.That(interaction.TriggerId, Is.EqualTo(string.Empty));
      Assert.That(interaction.TriggerName, Is.EqualTo(string.Empty));
      Assert.That(interaction.PromptText, Is.EqualTo(string.Empty));
      Assert.That(interaction.NpcName, Is.EqualTo(string.Empty));
      Assert.That(interaction.ResponseText, Is.EqualTo(string.Empty));
      Assert.That(interaction.ConstraintViolationTypes, Is.EqualTo(string.Empty));
      Assert.That(interaction.FallbackTriggerReason, Is.EqualTo(string.Empty));

      // Assert - Numeric properties default to 0
      Assert.That(interaction.PromptCount, Is.EqualTo(0));
      Assert.That(interaction.ResponseLength, Is.EqualTo(0));
      Assert.That(interaction.TtftMs, Is.EqualTo(0));
      Assert.That(interaction.PrefillTimeMs, Is.EqualTo(0));
      Assert.That(interaction.DecodeTimeMs, Is.EqualTo(0));
      Assert.That(interaction.TotalTimeMs, Is.EqualTo(0));
      Assert.That(interaction.PromptTokenCount, Is.EqualTo(0));
      Assert.That(interaction.GeneratedTokenCount, Is.EqualTo(0));
      Assert.That(interaction.CachedTokenCount, Is.EqualTo(0));
      Assert.That(interaction.TokensPerSecond, Is.EqualTo(0));
      Assert.That(interaction.ValidationFailureCount, Is.EqualTo(0));
      Assert.That(interaction.RetryCount, Is.EqualTo(0));
      Assert.That(interaction.TotalAttempts, Is.EqualTo(0));
      Assert.That(interaction.ConstraintCount, Is.EqualTo(0));
      Assert.That(interaction.ProhibitionCount, Is.EqualTo(0));
      Assert.That(interaction.RequirementCount, Is.EqualTo(0));
      Assert.That(interaction.PermissionCount, Is.EqualTo(0));

      // Assert - Boolean properties default to false
      Assert.That(interaction.WasTruncated, Is.False);
      Assert.That(interaction.ValidationPassed, Is.False);
      Assert.That(interaction.HasCriticalFailure, Is.False);
      Assert.That(interaction.FallbackUsed, Is.False);
    }

    [Test]
    public void DialogueInteraction_InteractionId_IsAutomaticallyGenerated()
    {
      // Act
      var interaction1 = new DialogueInteraction();
      var interaction2 = new DialogueInteraction();

      // Assert
      Assert.That(interaction1.InteractionId, Is.Not.Null.And.Not.Empty);
      Assert.That(interaction2.InteractionId, Is.Not.Null.And.Not.Empty);
      Assert.That(interaction1.InteractionId, Is.Not.EqualTo(interaction2.InteractionId));
    }

    [Test]
    public void DialogueInteraction_Timestamp_IsSetToNow()
    {
      // Arrange
      var before = DateTime.Now.AddSeconds(-1);

      // Act
      var interaction = new DialogueInteraction();
      var after = DateTime.Now.AddSeconds(1);

      // Assert
      Assert.That(interaction.Timestamp, Is.GreaterThan(before));
      Assert.That(interaction.Timestamp, Is.LessThan(after));
    }

    #endregion

    #region DialogueInteraction Property Setting Tests

    [Test]
    public void DialogueInteraction_CanSetAllProperties()
    {
      // Arrange
      var now = DateTime.Now;
      var interaction = new DialogueInteraction
      {
        InteractionId = "test-id-123",
        Timestamp = now,
        TriggerId = "trigger-456",
        TriggerName = "TestTrigger",
        PromptText = "Hello there",
        PromptCount = 5,
        NpcName = "TestNPC",
        ResponseText = "Greetings, traveler!",
        ResponseLength = 20,
        TtftMs = 50,
        PrefillTimeMs = 100,
        DecodeTimeMs = 200,
        TotalTimeMs = 350,
        PromptTokenCount = 50,
        GeneratedTokenCount = 15,
        CachedTokenCount = 10,
        TokensPerSecond = 75.0,
        WasTruncated = true,
        ValidationPassed = true,
        ValidationFailureCount = 0,
        ConstraintViolationTypes = "Prohibition,Requirement",
        HasCriticalFailure = false,
        RetryCount = 2,
        TotalAttempts = 3,
        FallbackUsed = false,
        FallbackTriggerReason = "PlayerUtterance",
        ConstraintCount = 5,
        ProhibitionCount = 2,
        RequirementCount = 2,
        PermissionCount = 1
      };

      // Assert
      Assert.That(interaction.InteractionId, Is.EqualTo("test-id-123"));
      Assert.That(interaction.Timestamp, Is.EqualTo(now));
      Assert.That(interaction.TriggerId, Is.EqualTo("trigger-456"));
      Assert.That(interaction.TriggerName, Is.EqualTo("TestTrigger"));
      Assert.That(interaction.PromptText, Is.EqualTo("Hello there"));
      Assert.That(interaction.PromptCount, Is.EqualTo(5));
      Assert.That(interaction.NpcName, Is.EqualTo("TestNPC"));
      Assert.That(interaction.ResponseText, Is.EqualTo("Greetings, traveler!"));
      Assert.That(interaction.ResponseLength, Is.EqualTo(20));
      Assert.That(interaction.TtftMs, Is.EqualTo(50));
      Assert.That(interaction.PrefillTimeMs, Is.EqualTo(100));
      Assert.That(interaction.DecodeTimeMs, Is.EqualTo(200));
      Assert.That(interaction.TotalTimeMs, Is.EqualTo(350));
      Assert.That(interaction.PromptTokenCount, Is.EqualTo(50));
      Assert.That(interaction.GeneratedTokenCount, Is.EqualTo(15));
      Assert.That(interaction.CachedTokenCount, Is.EqualTo(10));
      Assert.That(interaction.TokensPerSecond, Is.EqualTo(75.0));
      Assert.That(interaction.WasTruncated, Is.True);
      Assert.That(interaction.ValidationPassed, Is.True);
      Assert.That(interaction.ValidationFailureCount, Is.EqualTo(0));
      Assert.That(interaction.ConstraintViolationTypes, Is.EqualTo("Prohibition,Requirement"));
      Assert.That(interaction.HasCriticalFailure, Is.False);
      Assert.That(interaction.RetryCount, Is.EqualTo(2));
      Assert.That(interaction.TotalAttempts, Is.EqualTo(3));
      Assert.That(interaction.FallbackUsed, Is.False);
      Assert.That(interaction.FallbackTriggerReason, Is.EqualTo("PlayerUtterance"));
      Assert.That(interaction.ConstraintCount, Is.EqualTo(5));
      Assert.That(interaction.ProhibitionCount, Is.EqualTo(2));
      Assert.That(interaction.RequirementCount, Is.EqualTo(2));
      Assert.That(interaction.PermissionCount, Is.EqualTo(1));
    }

    #endregion

    #region FromMetrics Tests

    [Test]
    public void FromMetrics_CreatesInteractionWithCorrectValues()
    {
      // Arrange
      var metrics = CreateTestMetrics(
        content: "Hello adventurer!",
        promptTokens: 150,
        generatedTokens: 25,
        ttftMs: 60,
        prefillMs: 120,
        decodeMs: 250,
        totalMs: 370,
        cachedTokens: 20);

      var trigger = new MockTriggerInfo
      {
        Id = Guid.Parse("12345678-1234-1234-1234-123456789abc"),
        Name = "GreetingTrigger",
        PromptText = "Greet the player",
        PromptCount = 3
      };

      // Act
      var interaction = DialogueInteraction.FromMetrics(metrics, trigger, "Village Elder");

      // Assert
      Assert.That(interaction.TriggerId, Is.EqualTo("12345678-1234-1234-1234-123456789abc"));
      Assert.That(interaction.TriggerName, Is.EqualTo("GreetingTrigger"));
      Assert.That(interaction.PromptText, Is.EqualTo("Greet the player"));
      Assert.That(interaction.PromptCount, Is.EqualTo(3));
      Assert.That(interaction.NpcName, Is.EqualTo("Village Elder"));
      Assert.That(interaction.ResponseText, Is.EqualTo("Hello adventurer!"));
      Assert.That(interaction.ResponseLength, Is.EqualTo(17));
      Assert.That(interaction.TtftMs, Is.EqualTo(60));
      Assert.That(interaction.PrefillTimeMs, Is.EqualTo(120));
      Assert.That(interaction.DecodeTimeMs, Is.EqualTo(250));
      Assert.That(interaction.TotalTimeMs, Is.EqualTo(370));
      Assert.That(interaction.PromptTokenCount, Is.EqualTo(150));
      Assert.That(interaction.GeneratedTokenCount, Is.EqualTo(25));
      Assert.That(interaction.CachedTokenCount, Is.EqualTo(20));
      Assert.That(interaction.WasTruncated, Is.False);
    }

    [Test]
    public void FromMetrics_WithNullContent_SetsEmptyResponse()
    {
      // Arrange
      var metrics = new CompletionMetrics { Content = null! };
      var trigger = new MockTriggerInfo();

      // Act
      var interaction = DialogueInteraction.FromMetrics(metrics, trigger, "TestNPC");

      // Assert
      Assert.That(interaction.ResponseText, Is.EqualTo(string.Empty));
      Assert.That(interaction.ResponseLength, Is.EqualTo(0));
    }

    [Test]
    public void FromMetrics_SetsTimestampToNow()
    {
      // Arrange
      var before = DateTime.Now.AddSeconds(-1);
      var metrics = CreateTestMetrics();
      var trigger = new MockTriggerInfo();

      // Act
      var interaction = DialogueInteraction.FromMetrics(metrics, trigger, "TestNPC");
      var after = DateTime.Now.AddSeconds(1);

      // Assert
      Assert.That(interaction.Timestamp, Is.GreaterThan(before));
      Assert.That(interaction.Timestamp, Is.LessThan(after));
    }

    [Test]
    public void FromMetrics_GeneratesUniqueInteractionId()
    {
      // Arrange
      var metrics = CreateTestMetrics();
      var trigger = new MockTriggerInfo();

      // Act
      var interaction1 = DialogueInteraction.FromMetrics(metrics, trigger, "NPC1");
      var interaction2 = DialogueInteraction.FromMetrics(metrics, trigger, "NPC2");

      // Assert
      Assert.That(interaction1.InteractionId, Is.Not.EqualTo(interaction2.InteractionId));
    }

    [Test]
    public void FromMetrics_CalculatesTokensPerSecond()
    {
      // Arrange
      var metrics = CreateTestMetrics(
        generatedTokens: 100,
        decodeMs: 500);
      var trigger = new MockTriggerInfo();

      // Act
      var interaction = DialogueInteraction.FromMetrics(metrics, trigger, "TestNPC");

      // Assert - 100 tokens / 0.5 seconds = 200 tokens/second
      Assert.That(interaction.TokensPerSecond, Is.EqualTo(200.0).Within(0.01));
    }

    #endregion

    #region PopulateArchitecturalMetrics Tests

    [Test]
    public void PopulateArchitecturalMetrics_WithNullAgentMetrics_DoesNothing()
    {
      // Arrange
      var interaction = new DialogueInteraction();

      // Act - Should not throw
      interaction.PopulateArchitecturalMetrics(null!);

      // Assert - Values should remain at defaults
      Assert.That(interaction.TotalAttempts, Is.EqualTo(0));
      Assert.That(interaction.RetryCount, Is.EqualTo(0));
      Assert.That(interaction.ValidationPassed, Is.False);
    }

    [Test]
    public void PopulateArchitecturalMetrics_WithEmptyAgentMetrics_DoesNothing()
    {
      // Arrange
      var interaction = new DialogueInteraction();
      var agentMetrics = new MockAgentMetrics();

      // Act
      interaction.PopulateArchitecturalMetrics(agentMetrics);

      // Assert - Values should remain at defaults
      Assert.That(interaction.TotalAttempts, Is.EqualTo(0));
      Assert.That(interaction.RetryCount, Is.EqualTo(0));
    }

    [Test]
    public void PopulateArchitecturalMetrics_WithConstraints_CountsCorrectly()
    {
      // Arrange
      var interaction = new DialogueInteraction();
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("p1", "No violence", "Do not mention violence"));
      constraints.Add(Constraint.Prohibition("p2", "No swearing", "Do not use profanity"));
      constraints.Add(Constraint.Requirement("r1", "Be helpful", "You must be helpful"));
      constraints.Add(Constraint.Permission("perm1", "May discuss weather", "You may discuss weather"));

      var agentMetrics = new MockAgentMetrics
      {
        LastConstraints = constraints
      };

      // Act
      interaction.PopulateArchitecturalMetrics(agentMetrics);

      // Assert
      Assert.That(interaction.ConstraintCount, Is.EqualTo(4));
      Assert.That(interaction.ProhibitionCount, Is.EqualTo(2));
      Assert.That(interaction.RequirementCount, Is.EqualTo(1));
      Assert.That(interaction.PermissionCount, Is.EqualTo(1));
    }

    [Test]
    public void PopulateArchitecturalMetrics_WithGateResultPassed_SetsValidationPassed()
    {
      // Arrange
      var interaction = new DialogueInteraction();
      var gateResult = new GateResult { Passed = true };

      var agentMetrics = new MockAgentMetrics
      {
        LastGateResult = gateResult
      };

      // Act
      interaction.PopulateArchitecturalMetrics(agentMetrics);

      // Assert
      Assert.That(interaction.ValidationPassed, Is.True);
      Assert.That(interaction.ValidationFailureCount, Is.EqualTo(0));
      Assert.That(interaction.HasCriticalFailure, Is.False);
    }

    [Test]
    public void PopulateArchitecturalMetrics_WithGateResultFailed_SetsValidationFailed()
    {
      // Arrange
      var interaction = new DialogueInteraction();
      var gateResult = new GateResult
      {
        Passed = false,
        Failures = new List<ValidationFailure>
        {
          new ValidationFailure { Reason = ValidationFailureReason.ProhibitionViolated },
          new ValidationFailure { Reason = ValidationFailureReason.RequirementNotMet }
        }
      };

      var agentMetrics = new MockAgentMetrics
      {
        LastGateResult = gateResult
      };

      // Act
      interaction.PopulateArchitecturalMetrics(agentMetrics);

      // Assert
      Assert.That(interaction.ValidationPassed, Is.False);
      Assert.That(interaction.ValidationFailureCount, Is.EqualTo(2));
      Assert.That(interaction.ConstraintViolationTypes, Does.Contain("ProhibitionViolated"));
      Assert.That(interaction.ConstraintViolationTypes, Does.Contain("RequirementNotMet"));
    }

    [Test]
    public void PopulateArchitecturalMetrics_WithCriticalFailure_SetsCriticalFlag()
    {
      // Arrange
      var interaction = new DialogueInteraction();
      var gateResult = new GateResult
      {
        Passed = false,
        Failures = new List<ValidationFailure>
        {
          new ValidationFailure
          {
            Reason = ValidationFailureReason.CanonicalFactContradiction,
            Severity = ConstraintSeverity.Critical // This makes HasCriticalFailure true
          }
        }
      };

      var agentMetrics = new MockAgentMetrics
      {
        LastGateResult = gateResult
      };

      // Act
      interaction.PopulateArchitecturalMetrics(agentMetrics);

      // Assert
      Assert.That(interaction.HasCriticalFailure, Is.True);
    }

    [Test]
    public void PopulateArchitecturalMetrics_WithFallbackCondition_SetsFallbackUsed()
    {
      // Arrange
      var interaction = new DialogueInteraction();

      // Create a minimal snapshot with context
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance
      };
      var snapshot = new StateSnapshotBuilder()
        .WithContext(context)
        .Build();

      // Create inference result that indicates failure after max retries using static factory
      var constraint = Constraint.Prohibition("test", "Test prohibition", "Do not test");
      var violation = new ConstraintViolation(constraint, "Violated");
      var inferenceResult = InferenceResult.FailedValidation(
        response: "",
        outcome: ValidationOutcome.ProhibitionViolated,
        violations: new List<ConstraintViolation> { violation },
        snapshot: snapshot,
        elapsedMs: 100);

      // Create 3 attempts (failure after max retries)
      var allAttempts = new List<InferenceResult> { inferenceResult, inferenceResult, inferenceResult };
      var resultWithRetries = new InferenceResultWithRetries(
        finalResult: inferenceResult,
        allAttempts: allAttempts,
        totalElapsedMs: 300);

      var agentMetrics = new MockAgentMetrics
      {
        LastInferenceResult = resultWithRetries,
        LastSnapshot = snapshot
      };

      // Act
      interaction.PopulateArchitecturalMetrics(agentMetrics);

      // Assert
      Assert.That(interaction.TotalAttempts, Is.EqualTo(3));
      Assert.That(interaction.RetryCount, Is.EqualTo(2));
      Assert.That(interaction.FallbackUsed, Is.True);
      Assert.That(interaction.FallbackTriggerReason, Is.EqualTo("PlayerUtterance"));
    }

    [Test]
    public void PopulateArchitecturalMetrics_WithSuccessfulInference_SetsCorrectMetrics()
    {
      // Arrange
      var interaction = new DialogueInteraction();

      var snapshot = new StateSnapshotBuilder().Build();
      var successResult = InferenceResult.Succeeded(
        response: "Hello!",
        snapshot: snapshot,
        elapsedMs: 100);

      var allAttempts = new List<InferenceResult> { successResult };
      var resultWithRetries = new InferenceResultWithRetries(
        finalResult: successResult,
        allAttempts: allAttempts,
        totalElapsedMs: 100);

      var agentMetrics = new MockAgentMetrics
      {
        LastInferenceResult = resultWithRetries
      };

      // Act
      interaction.PopulateArchitecturalMetrics(agentMetrics);

      // Assert
      Assert.That(interaction.TotalAttempts, Is.EqualTo(1));
      Assert.That(interaction.RetryCount, Is.EqualTo(0));
      Assert.That(interaction.ValidationPassed, Is.True);
      Assert.That(interaction.FallbackUsed, Is.False);
    }

    [Test]
    public void PopulateArchitecturalMetrics_WithViolations_CollectsViolationTypes()
    {
      // Arrange
      var interaction = new DialogueInteraction();

      var snapshot = new StateSnapshotBuilder().Build();
      var prohibitionConstraint = Constraint.Prohibition("p1", "No secrets", "Do not reveal secrets");
      var requirementConstraint = Constraint.Requirement("r1", "Be polite", "You must be polite");

      var violations = new List<ConstraintViolation>
      {
        new ConstraintViolation(prohibitionConstraint, "Revealed secret"),
        new ConstraintViolation(requirementConstraint, "Was rude")
      };

      var failResult = InferenceResult.FailedValidation(
        response: "Bad response",
        outcome: ValidationOutcome.ProhibitionViolated,
        violations: violations,
        snapshot: snapshot,
        elapsedMs: 100);

      var allAttempts = new List<InferenceResult> { failResult };
      var resultWithRetries = new InferenceResultWithRetries(
        finalResult: failResult,
        allAttempts: allAttempts,
        totalElapsedMs: 100);

      var agentMetrics = new MockAgentMetrics
      {
        LastInferenceResult = resultWithRetries
      };

      // Act
      interaction.PopulateArchitecturalMetrics(agentMetrics);

      // Assert
      Assert.That(interaction.ValidationPassed, Is.False);
      Assert.That(interaction.ValidationFailureCount, Is.EqualTo(2));
      Assert.That(interaction.ConstraintViolationTypes, Does.Contain("Prohibition"));
      Assert.That(interaction.ConstraintViolationTypes, Does.Contain("Requirement"));
    }

    [Test]
    public void PopulateArchitecturalMetrics_OnlyProhibitions_CountsCorrectly()
    {
      // Arrange
      var interaction = new DialogueInteraction();
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("p1", "No violence", "Do not be violent"));
      constraints.Add(Constraint.Prohibition("p2", "No swearing", "Do not use profanity"));
      constraints.Add(Constraint.Prohibition("p3", "No spoilers", "Do not reveal spoilers"));

      var agentMetrics = new MockAgentMetrics
      {
        LastConstraints = constraints
      };

      // Act
      interaction.PopulateArchitecturalMetrics(agentMetrics);

      // Assert
      Assert.That(interaction.ConstraintCount, Is.EqualTo(3));
      Assert.That(interaction.ProhibitionCount, Is.EqualTo(3));
      Assert.That(interaction.RequirementCount, Is.EqualTo(0));
      Assert.That(interaction.PermissionCount, Is.EqualTo(0));
    }

    [Test]
    public void PopulateArchitecturalMetrics_EmptyConstraintSet_SetsZeroCounts()
    {
      // Arrange
      var interaction = new DialogueInteraction();
      var constraints = new ConstraintSet();

      var agentMetrics = new MockAgentMetrics
      {
        LastConstraints = constraints
      };

      // Act
      interaction.PopulateArchitecturalMetrics(agentMetrics);

      // Assert
      Assert.That(interaction.ConstraintCount, Is.EqualTo(0));
      Assert.That(interaction.ProhibitionCount, Is.EqualTo(0));
      Assert.That(interaction.RequirementCount, Is.EqualTo(0));
      Assert.That(interaction.PermissionCount, Is.EqualTo(0));
    }

    #endregion

    #region DialogueMetricsCollection Default Values Tests

    [Test]
    public void DialogueMetricsCollection_DefaultValues_AreCorrect()
    {
      // Act
      var collection = new DialogueMetricsCollection();

      // Assert
      Assert.That(collection.SessionId, Is.Not.Null.And.Not.Empty);
      Assert.That(collection.SessionEnd, Is.Null);
      Assert.That(collection.Interactions, Is.Not.Null);
      Assert.That(collection.Interactions.Count, Is.EqualTo(0));
      Assert.That(collection.TotalInteractions, Is.EqualTo(0));
    }

    [Test]
    public void DialogueMetricsCollection_SessionStart_IsSetToNow()
    {
      // Arrange
      var before = DateTime.Now.AddSeconds(-1);

      // Act
      var collection = new DialogueMetricsCollection();
      var after = DateTime.Now.AddSeconds(1);

      // Assert
      Assert.That(collection.SessionStart, Is.GreaterThan(before));
      Assert.That(collection.SessionStart, Is.LessThan(after));
    }

    [Test]
    public void DialogueMetricsCollection_SessionId_IsAutomaticallyGenerated()
    {
      // Act
      var collection1 = new DialogueMetricsCollection();
      var collection2 = new DialogueMetricsCollection();

      // Assert
      Assert.That(collection1.SessionId, Is.Not.EqualTo(collection2.SessionId));
    }

    #endregion

    #region DialogueMetricsCollection AddInteraction Tests

    [Test]
    public void AddInteraction_AddsToCollection()
    {
      // Arrange
      var collection = new DialogueMetricsCollection();
      var interaction = new DialogueInteraction { NpcName = "TestNPC" };

      // Act
      collection.AddInteraction(interaction);

      // Assert
      Assert.That(collection.Interactions.Count, Is.EqualTo(1));
      Assert.That(collection.TotalInteractions, Is.EqualTo(1));
      Assert.That(collection.Interactions[0].NpcName, Is.EqualTo("TestNPC"));
    }

    [Test]
    public void AddInteraction_MultipleInteractions_AllAdded()
    {
      // Arrange
      var collection = new DialogueMetricsCollection();

      // Act
      for (int i = 0; i < 5; i++)
      {
        collection.AddInteraction(new DialogueInteraction { NpcName = $"NPC{i}" });
      }

      // Assert
      Assert.That(collection.Interactions.Count, Is.EqualTo(5));
      Assert.That(collection.TotalInteractions, Is.EqualTo(5));
    }

    [Test]
    public void AddInteraction_PreservesOrder()
    {
      // Arrange
      var collection = new DialogueMetricsCollection();

      // Act
      collection.AddInteraction(new DialogueInteraction { NpcName = "First" });
      collection.AddInteraction(new DialogueInteraction { NpcName = "Second" });
      collection.AddInteraction(new DialogueInteraction { NpcName = "Third" });

      // Assert
      Assert.That(collection.Interactions[0].NpcName, Is.EqualTo("First"));
      Assert.That(collection.Interactions[1].NpcName, Is.EqualTo("Second"));
      Assert.That(collection.Interactions[2].NpcName, Is.EqualTo("Third"));
    }

    #endregion

    #region DialogueMetricsCollection EndSession Tests

    [Test]
    public void EndSession_SetsSessionEnd()
    {
      // Arrange
      var collection = new DialogueMetricsCollection();
      var before = DateTime.Now.AddSeconds(-1);

      // Act
      collection.EndSession();
      var after = DateTime.Now.AddSeconds(1);

      // Assert
      Assert.That(collection.SessionEnd, Is.Not.Null);
      Assert.That(collection.SessionEnd!.Value, Is.GreaterThan(before));
      Assert.That(collection.SessionEnd!.Value, Is.LessThan(after));
    }

    [Test]
    public void EndSession_CalledTwice_UpdatesSessionEnd()
    {
      // Arrange
      var collection = new DialogueMetricsCollection();

      // Act
      collection.EndSession();
      var firstEnd = collection.SessionEnd;
      System.Threading.Thread.Sleep(10); // Small delay
      collection.EndSession();
      var secondEnd = collection.SessionEnd;

      // Assert
      Assert.That(secondEnd, Is.GreaterThanOrEqualTo(firstEnd));
    }

    #endregion

    #region DialogueMetricsCollection TotalInteractions Tests

    [Test]
    public void TotalInteractions_ReflectsCollectionCount()
    {
      // Arrange
      var collection = new DialogueMetricsCollection();

      // Assert initial
      Assert.That(collection.TotalInteractions, Is.EqualTo(0));

      // Act & Assert after adding
      collection.AddInteraction(new DialogueInteraction());
      Assert.That(collection.TotalInteractions, Is.EqualTo(1));

      collection.AddInteraction(new DialogueInteraction());
      Assert.That(collection.TotalInteractions, Is.EqualTo(2));

      collection.AddInteraction(new DialogueInteraction());
      Assert.That(collection.TotalInteractions, Is.EqualTo(3));
    }

    #endregion

    #region DialogueMetricsCollection Property Setting Tests

    [Test]
    public void DialogueMetricsCollection_CanSetAllProperties()
    {
      // Arrange
      var sessionStart = DateTime.Now.AddHours(-1);
      var sessionEnd = DateTime.Now;
      var interactions = new List<DialogueInteraction>
      {
        new DialogueInteraction { NpcName = "NPC1" },
        new DialogueInteraction { NpcName = "NPC2" }
      };

      // Act
      var collection = new DialogueMetricsCollection
      {
        SessionId = "custom-session-id",
        SessionStart = sessionStart,
        SessionEnd = sessionEnd,
        Interactions = interactions
      };

      // Assert
      Assert.That(collection.SessionId, Is.EqualTo("custom-session-id"));
      Assert.That(collection.SessionStart, Is.EqualTo(sessionStart));
      Assert.That(collection.SessionEnd, Is.EqualTo(sessionEnd));
      Assert.That(collection.Interactions.Count, Is.EqualTo(2));
      Assert.That(collection.TotalInteractions, Is.EqualTo(2));
    }

    #endregion

    #region Edge Cases Tests

    [Test]
    public void DialogueInteraction_WithEmptyTriggerInfo_HandlesGracefully()
    {
      // Arrange
      var metrics = CreateTestMetrics();
      var trigger = new MockTriggerInfo
      {
        Id = Guid.Empty,
        Name = "",
        PromptText = "",
        PromptCount = 0
      };

      // Act
      var interaction = DialogueInteraction.FromMetrics(metrics, trigger, "");

      // Assert
      Assert.That(interaction.TriggerId, Is.EqualTo(Guid.Empty.ToString()));
      Assert.That(interaction.TriggerName, Is.EqualTo(string.Empty));
      Assert.That(interaction.PromptText, Is.EqualTo(string.Empty));
      Assert.That(interaction.NpcName, Is.EqualTo(string.Empty));
    }

    [Test]
    public void DialogueInteraction_WithLargeValues_HandlesCorrectly()
    {
      // Arrange
      var interaction = new DialogueInteraction
      {
        ResponseLength = int.MaxValue,
        TotalTimeMs = long.MaxValue,
        TokensPerSecond = double.MaxValue,
        PromptCount = int.MaxValue
      };

      // Assert
      Assert.That(interaction.ResponseLength, Is.EqualTo(int.MaxValue));
      Assert.That(interaction.TotalTimeMs, Is.EqualTo(long.MaxValue));
      Assert.That(interaction.TokensPerSecond, Is.EqualTo(double.MaxValue));
      Assert.That(interaction.PromptCount, Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void DialogueMetricsCollection_WithNullInteractionsList_CanBeAssigned()
    {
      // Arrange
      var collection = new DialogueMetricsCollection();

      // Act
      collection.Interactions = null!;

      // Assert
      Assert.That(collection.Interactions, Is.Null);
    }

    [Test]
    public void DialogueInteraction_SerializableAttribute_IsPresent()
    {
      // Assert
      var hasAttribute = typeof(DialogueInteraction).IsDefined(typeof(System.SerializableAttribute), false);
      Assert.That(hasAttribute, Is.True);
    }

    [Test]
    public void DialogueMetricsCollection_SerializableAttribute_IsPresent()
    {
      // Assert
      var hasAttribute = typeof(DialogueMetricsCollection).IsDefined(typeof(System.SerializableAttribute), false);
      Assert.That(hasAttribute, Is.True);
    }

    #endregion
  }
}
