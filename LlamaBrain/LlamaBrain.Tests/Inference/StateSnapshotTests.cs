using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.Inference
{
  /// <summary>
  /// Tests for StateSnapshot and StateSnapshotBuilder.
  /// </summary>
  public class StateSnapshotTests
  {
    private InteractionContext _defaultContext = null!;
    private ConstraintSet _defaultConstraints = null!;

    [SetUp]
    public void SetUp()
    {
      _defaultContext = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = "npc-001",
        PlayerInput = "Hello"
      };

      _defaultConstraints = new ConstraintSet();
      _defaultConstraints.Add(Constraint.Prohibition("no-secrets", "Cannot reveal secrets", "Do not reveal secrets"));
    }

    #region StateSnapshotBuilder Tests

    [Test]
    public void Builder_WithContext_SetsContext()
    {
      // Arrange
      var context = new InteractionContext { NpcId = "test-npc" };
      var builder = new StateSnapshotBuilder();

      // Act
      var snapshot = builder.WithContext(context).Build();

      // Assert
      Assert.That(snapshot.Context, Is.EqualTo(context));
      Assert.That(snapshot.Context.NpcId, Is.EqualTo("test-npc"));
    }

    [Test]
    public void Builder_WithoutContext_CreatesDefaultContext()
    {
      // Arrange
      var builder = new StateSnapshotBuilder();

      // Act
      var snapshot = builder.Build();

      // Assert
      Assert.That(snapshot.Context, Is.Not.Null);
    }

    [Test]
    public void Builder_WithConstraints_SetsConstraints()
    {
      // Arrange
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Requirement("test-req", "Test requirement", "Must test"));
      var builder = new StateSnapshotBuilder();

      // Act
      var snapshot = builder.WithConstraints(constraints).Build();

      // Assert
      Assert.That(snapshot.Constraints.Count, Is.EqualTo(1));
      Assert.That(snapshot.Constraints.Requirements.Count, Is.EqualTo(1));
    }

    [Test]
    public void Builder_WithCanonicalFacts_AddsFacts()
    {
      // Arrange
      var facts = new[] { "The king is Arthur", "The world is round" };
      var builder = new StateSnapshotBuilder();

      // Act
      var snapshot = builder.WithCanonicalFacts(facts).Build();

      // Assert
      Assert.That(snapshot.CanonicalFacts.Count, Is.EqualTo(2));
      Assert.That(snapshot.CanonicalFacts, Contains.Item("The king is Arthur"));
      Assert.That(snapshot.CanonicalFacts, Contains.Item("The world is round"));
    }

    [Test]
    public void Builder_WithWorldState_AddsState()
    {
      // Arrange
      var state = new[] { "door_status=open", "weather=sunny" };
      var builder = new StateSnapshotBuilder();

      // Act
      var snapshot = builder.WithWorldState(state).Build();

      // Assert
      Assert.That(snapshot.WorldState.Count, Is.EqualTo(2));
      Assert.That(snapshot.WorldState, Contains.Item("door_status=open"));
    }

    [Test]
    public void Builder_WithEpisodicMemories_AddsMemories()
    {
      // Arrange
      var memories = new[] { "Player said hello", "NPC greeted player" };
      var builder = new StateSnapshotBuilder();

      // Act
      var snapshot = builder.WithEpisodicMemories(memories).Build();

      // Assert
      Assert.That(snapshot.EpisodicMemories.Count, Is.EqualTo(2));
      Assert.That(snapshot.EpisodicMemories, Contains.Item("Player said hello"));
    }

    [Test]
    public void Builder_WithBeliefs_AddsBeliefs()
    {
      // Arrange
      var beliefs = new[] { "Player is trustworthy", "The quest is important" };
      var builder = new StateSnapshotBuilder();

      // Act
      var snapshot = builder.WithBeliefs(beliefs).Build();

      // Assert
      Assert.That(snapshot.Beliefs.Count, Is.EqualTo(2));
      Assert.That(snapshot.Beliefs, Contains.Item("Player is trustworthy"));
    }

    [Test]
    public void Builder_WithDialogueHistory_AddsHistory()
    {
      // Arrange
      var history = new[] { "Player: Hello", "NPC: Hi there!" };
      var builder = new StateSnapshotBuilder();

      // Act
      var snapshot = builder.WithDialogueHistory(history).Build();

      // Assert
      Assert.That(snapshot.DialogueHistory.Count, Is.EqualTo(2));
      Assert.That(snapshot.DialogueHistory, Contains.Item("Player: Hello"));
    }

    [Test]
    public void Builder_WithSystemPrompt_SetsPrompt()
    {
      // Arrange
      var prompt = "You are a friendly shopkeeper.";
      var builder = new StateSnapshotBuilder();

      // Act
      var snapshot = builder.WithSystemPrompt(prompt).Build();

      // Assert
      Assert.That(snapshot.SystemPrompt, Is.EqualTo(prompt));
    }

    [Test]
    public void Builder_WithPlayerInput_SetsInput()
    {
      // Arrange
      var input = "Hello, shopkeeper!";
      var builder = new StateSnapshotBuilder();

      // Act
      var snapshot = builder.WithPlayerInput(input).Build();

      // Assert
      Assert.That(snapshot.PlayerInput, Is.EqualTo(input));
    }

    [Test]
    public void Builder_WithAttemptNumber_SetsAttempt()
    {
      // Arrange
      var builder = new StateSnapshotBuilder();

      // Act
      var snapshot = builder.WithAttemptNumber(2).Build();

      // Assert
      Assert.That(snapshot.AttemptNumber, Is.EqualTo(2));
    }

    [Test]
    public void Builder_WithMaxAttempts_SetsMaxAttempts()
    {
      // Arrange
      var builder = new StateSnapshotBuilder();

      // Act
      var snapshot = builder.WithMaxAttempts(5).Build();

      // Assert
      Assert.That(snapshot.MaxAttempts, Is.EqualTo(5));
    }

    [Test]
    public void Builder_WithMetadata_AddsMetadata()
    {
      // Arrange
      var builder = new StateSnapshotBuilder();

      // Act
      var snapshot = builder
        .WithMetadata("key1", "value1")
        .WithMetadata("key2", "value2")
        .Build();

      // Assert
      Assert.That(snapshot.Metadata.Count, Is.EqualTo(2));
      Assert.That(snapshot.Metadata["key1"], Is.EqualTo("value1"));
      Assert.That(snapshot.Metadata["key2"], Is.EqualTo("value2"));
    }

    [Test]
    public void Builder_FluentChaining_Works()
    {
      // Arrange
      var builder = new StateSnapshotBuilder();

      // Act
      var snapshot = builder
        .WithContext(_defaultContext)
        .WithConstraints(_defaultConstraints)
        .WithSystemPrompt("Test prompt")
        .WithPlayerInput("Test input")
        .WithCanonicalFacts(new[] { "Fact 1" })
        .Build();

      // Assert
      Assert.That(snapshot.Context, Is.EqualTo(_defaultContext));
      Assert.That(snapshot.Constraints.Count, Is.EqualTo(1));
      Assert.That(snapshot.SystemPrompt, Is.EqualTo("Test prompt"));
      Assert.That(snapshot.PlayerInput, Is.EqualTo("Test input"));
      Assert.That(snapshot.CanonicalFacts.Count, Is.EqualTo(1));
    }

    [Test]
    public void Builder_GeneratesUniqueSnapshotId()
    {
      // Arrange
      var builder1 = new StateSnapshotBuilder();
      var builder2 = new StateSnapshotBuilder();

      // Act
      var snapshot1 = builder1.Build();
      var snapshot2 = builder2.Build();

      // Assert
      Assert.That(snapshot1.SnapshotId, Is.Not.EqualTo(snapshot2.SnapshotId));
      Assert.That(snapshot1.SnapshotId, Is.Not.Empty);
      Assert.That(snapshot2.SnapshotId, Is.Not.Empty);
    }

    #endregion

    #region StateSnapshot Tests

    [Test]
    public void Snapshot_CreatedAt_IsSet()
    {
      // Arrange
      var before = DateTime.UtcNow;
      var builder = new StateSnapshotBuilder();

      // Act
      var snapshot = builder.Build();
      var after = DateTime.UtcNow;

      // Assert
      Assert.That(snapshot.CreatedAt, Is.GreaterThanOrEqualTo(before));
      Assert.That(snapshot.CreatedAt, Is.LessThanOrEqualTo(after));
    }

    [Test]
    public void Snapshot_TotalMemoryCount_CalculatesCorrectly()
    {
      // Arrange
      var builder = new StateSnapshotBuilder()
        .WithCanonicalFacts(new[] { "Fact 1", "Fact 2" })
        .WithWorldState(new[] { "State 1" })
        .WithEpisodicMemories(new[] { "Memory 1", "Memory 2", "Memory 3" })
        .WithBeliefs(new[] { "Belief 1" });

      // Act
      var snapshot = builder.Build();

      // Assert
      Assert.That(snapshot.TotalMemoryCount, Is.EqualTo(7)); // 2 + 1 + 3 + 1
    }

    [Test]
    public void Snapshot_CanRetry_WhenAttemptLessThanMax()
    {
      // Arrange
      var builder = new StateSnapshotBuilder()
        .WithAttemptNumber(0)
        .WithMaxAttempts(3);

      // Act
      var snapshot = builder.Build();

      // Assert
      Assert.That(snapshot.CanRetry, Is.True);
    }

    [Test]
    public void Snapshot_CanRetry_FalseWhenAtMax()
    {
      // Arrange
      var builder = new StateSnapshotBuilder()
        .WithAttemptNumber(2)
        .WithMaxAttempts(3);

      // Act
      var snapshot = builder.Build();

      // Assert
      Assert.That(snapshot.CanRetry, Is.False);
    }

    [Test]
    public void Snapshot_GetAllMemoryForPrompt_FormatsCorrectly()
    {
      // Arrange
      var builder = new StateSnapshotBuilder()
        .WithCanonicalFacts(new[] { "The king is Arthur" })
        .WithWorldState(new[] { "door_status=open" })
        .WithEpisodicMemories(new[] { "Player said hello" })
        .WithBeliefs(new[] { "Player is trustworthy" });

      // Act
      var snapshot = builder.Build();
      var formatted = snapshot.GetAllMemoryForPrompt().ToList();

      // Assert
      Assert.That(formatted.Count, Is.EqualTo(4));
      Assert.That(formatted, Contains.Item("[Fact] The king is Arthur"));
      Assert.That(formatted, Contains.Item("[State] door_status=open"));
      Assert.That(formatted, Contains.Item("[Memory] Player said hello"));
      Assert.That(formatted, Contains.Item("Player is trustworthy")); // Beliefs without prefix
    }

    [Test]
    public void Snapshot_ToString_ReturnsFormattedString()
    {
      // Arrange
      var builder = new StateSnapshotBuilder()
        .WithAttemptNumber(1)
        .WithMaxAttempts(3)
        .WithCanonicalFacts(new[] { "Fact 1", "Fact 2" })
        .WithConstraints(_defaultConstraints);

      // Act
      var snapshot = builder.Build();
      var result = snapshot.ToString();

      // Assert
      Assert.That(result, Does.Contain("StateSnapshot["));
      Assert.That(result, Does.Contain("Attempt 2/3"));
      Assert.That(result, Does.Contain("2 memories"));
      Assert.That(result, Does.Contain("1 constraints"));
    }

    #endregion

    #region ForRetry Tests

    [Test]
    public void ForRetry_CreatesNewSnapshotWithIncrementedAttempt()
    {
      // Arrange
      var snapshot = new StateSnapshotBuilder()
        .WithContext(_defaultContext)
        .WithConstraints(_defaultConstraints)
        .WithAttemptNumber(0)
        .WithMaxAttempts(3)
        .Build();

      // Act
      var retrySnapshot = snapshot.ForRetry();

      // Assert
      Assert.That(retrySnapshot.AttemptNumber, Is.EqualTo(1));
      Assert.That(retrySnapshot.MaxAttempts, Is.EqualTo(3));
      Assert.That(retrySnapshot.SnapshotId, Is.Not.EqualTo(snapshot.SnapshotId));
    }

    [Test]
    public void ForRetry_PreservesAllContext()
    {
      // Arrange
      var snapshot = new StateSnapshotBuilder()
        .WithContext(_defaultContext)
        .WithCanonicalFacts(new[] { "Fact 1" })
        .WithWorldState(new[] { "State 1" })
        .WithEpisodicMemories(new[] { "Memory 1" })
        .WithBeliefs(new[] { "Belief 1" })
        .WithDialogueHistory(new[] { "History 1" })
        .WithSystemPrompt("System prompt")
        .WithPlayerInput("Player input")
        .WithMetadata("key", "value")
        .Build();

      // Act
      var retrySnapshot = snapshot.ForRetry();

      // Assert
      Assert.That(retrySnapshot.Context, Is.EqualTo(snapshot.Context));
      Assert.That(retrySnapshot.CanonicalFacts, Is.EqualTo(snapshot.CanonicalFacts));
      Assert.That(retrySnapshot.WorldState, Is.EqualTo(snapshot.WorldState));
      Assert.That(retrySnapshot.EpisodicMemories, Is.EqualTo(snapshot.EpisodicMemories));
      Assert.That(retrySnapshot.Beliefs, Is.EqualTo(snapshot.Beliefs));
      Assert.That(retrySnapshot.DialogueHistory, Is.EqualTo(snapshot.DialogueHistory));
      Assert.That(retrySnapshot.SystemPrompt, Is.EqualTo(snapshot.SystemPrompt));
      Assert.That(retrySnapshot.PlayerInput, Is.EqualTo(snapshot.PlayerInput));
      Assert.That(retrySnapshot.Metadata, Is.EqualTo(snapshot.Metadata));
    }

    [Test]
    public void ForRetry_MergesConstraints()
    {
      // Arrange
      var originalConstraints = new ConstraintSet();
      originalConstraints.Add(Constraint.Prohibition("original", "Original prohibition", "Do not do original"));
      
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(originalConstraints)
        .Build();

      var additionalConstraints = new ConstraintSet();
      additionalConstraints.Add(Constraint.Requirement("additional", "Additional requirement", "Must do additional"));

      // Act
      var retrySnapshot = snapshot.ForRetry(additionalConstraints);

      // Assert
      Assert.That(retrySnapshot.Constraints.Count, Is.EqualTo(2));
      Assert.That(retrySnapshot.Constraints.Prohibitions.Count, Is.EqualTo(1));
      Assert.That(retrySnapshot.Constraints.Requirements.Count, Is.EqualTo(1));
    }

    [Test]
    public void ForRetry_WithoutAdditionalConstraints_PreservesOriginal()
    {
      // Arrange
      var originalConstraints = new ConstraintSet();
      originalConstraints.Add(Constraint.Prohibition("original", "Original", "Do not"));
      
      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(originalConstraints)
        .Build();

      // Act
      var retrySnapshot = snapshot.ForRetry();

      // Assert
      Assert.That(retrySnapshot.Constraints.Count, Is.EqualTo(1));
      Assert.That(retrySnapshot.Constraints.Prohibitions.Count, Is.EqualTo(1));
    }

    #endregion
  }
}

