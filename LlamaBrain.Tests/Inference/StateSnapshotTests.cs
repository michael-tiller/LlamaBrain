using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.Inference
{
  [TestFixture]
  public class StateSnapshotTests
  {
    [Test]
    public void Builder_CreatesSnapshot_WithBasicProperties()
    {
      var snapshot = new StateSnapshotBuilder()
        .WithSystemPrompt("Test system prompt")
        .WithPlayerInput("Hello world")
        .Build();

      Assert.That(snapshot.SystemPrompt, Is.EqualTo("Test system prompt"));
      Assert.That(snapshot.PlayerInput, Is.EqualTo("Hello world"));
      Assert.That(snapshot.SnapshotId, Is.Not.Null.And.Not.Empty);
      Assert.That(snapshot.AttemptNumber, Is.EqualTo(0));
      Assert.That(snapshot.MaxAttempts, Is.EqualTo(3));
    }

    [Test]
    public void Builder_CreatesSnapshot_WithConstraints()
    {
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibit("No violence", "test"));
      constraints.Add(Constraint.Require("Be helpful", "test"));

      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .Build();

      Assert.That(snapshot.Constraints.Count, Is.EqualTo(2));
      Assert.That(snapshot.Constraints.Prohibitions.Count, Is.EqualTo(1));
      Assert.That(snapshot.Constraints.Requirements.Count, Is.EqualTo(1));
    }

    [Test]
    public void Builder_CreatesSnapshot_WithMemories()
    {
      var snapshot = new StateSnapshotBuilder()
        .WithCanonicalFacts(new[] { "The king's name is Arthur" })
        .WithWorldState(new[] { "door_castle: open" })
        .WithEpisodicMemories(new[] { "Player asked about the castle" })
        .WithBeliefs(new[] { "Player seems friendly" })
        .Build();

      Assert.That(snapshot.CanonicalFacts.Count, Is.EqualTo(1));
      Assert.That(snapshot.WorldState.Count, Is.EqualTo(1));
      Assert.That(snapshot.EpisodicMemories.Count, Is.EqualTo(1));
      Assert.That(snapshot.Beliefs.Count, Is.EqualTo(1));
      Assert.That(snapshot.TotalMemoryCount, Is.EqualTo(4));
    }

    [Test]
    public void Builder_CreatesSnapshot_WithDialogueHistory()
    {
      var history = new[] { "Player: Hello", "NPC: Hi there!" };
      var snapshot = new StateSnapshotBuilder()
        .WithDialogueHistory(history)
        .Build();

      Assert.That(snapshot.DialogueHistory.Count, Is.EqualTo(2));
      Assert.That(snapshot.DialogueHistory[0], Is.EqualTo("Player: Hello"));
    }

    [Test]
    public void Builder_CreatesSnapshot_WithMetadata()
    {
      var snapshot = new StateSnapshotBuilder()
        .WithMetadata("npc_id", "npc_001")
        .WithMetadata("game_time", "100.5")
        .Build();

      Assert.That(snapshot.Metadata.Count, Is.EqualTo(2));
      Assert.That(snapshot.Metadata["npc_id"], Is.EqualTo("npc_001"));
      Assert.That(snapshot.Metadata["game_time"], Is.EqualTo("100.5"));
    }

    [Test]
    public void Builder_CreatesSnapshot_WithContext()
    {
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.ZoneTrigger,
        TriggerId = "zone_1",
        PlayerInput = "test"
      };

      var snapshot = new StateSnapshotBuilder()
        .WithContext(context)
        .Build();

      Assert.That(snapshot.Context.TriggerReason, Is.EqualTo(TriggerReason.ZoneTrigger));
      Assert.That(snapshot.Context.TriggerId, Is.EqualTo("zone_1"));
    }

    [Test]
    public void Snapshot_CanRetry_WhenUnderMaxAttempts()
    {
      var snapshot = new StateSnapshotBuilder()
        .WithMaxAttempts(3)
        .WithAttemptNumber(0)
        .Build();

      Assert.That(snapshot.CanRetry, Is.True);
    }

    [Test]
    public void Snapshot_CannotRetry_WhenAtMaxAttempts()
    {
      var snapshot = new StateSnapshotBuilder()
        .WithMaxAttempts(3)
        .WithAttemptNumber(2) // Last attempt (0-indexed)
        .Build();

      Assert.That(snapshot.CanRetry, Is.False);
    }

    [Test]
    public void ForRetry_IncrementsAttemptNumber()
    {
      var snapshot = new StateSnapshotBuilder()
        .WithAttemptNumber(0)
        .WithMaxAttempts(3)
        .Build();

      var retry = snapshot.ForRetry();

      Assert.That(retry.AttemptNumber, Is.EqualTo(1));
      Assert.That(retry.SnapshotId, Is.Not.EqualTo(snapshot.SnapshotId));
    }

    [Test]
    public void ForRetry_MergesAdditionalConstraints()
    {
      var originalConstraints = new ConstraintSet();
      originalConstraints.Add(Constraint.Prohibit("No swearing", "test"));

      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(originalConstraints)
        .WithMaxAttempts(3)
        .Build();

      var additionalConstraints = new ConstraintSet();
      additionalConstraints.Add(Constraint.Prohibit("No violence", "retry"));

      var retry = snapshot.ForRetry(additionalConstraints);

      Assert.That(retry.Constraints.Count, Is.EqualTo(2));
      Assert.That(retry.Constraints.Prohibitions.Count, Is.EqualTo(2));
    }

    [Test]
    public void ForRetry_PreservesOriginalData()
    {
      var snapshot = new StateSnapshotBuilder()
        .WithSystemPrompt("System prompt")
        .WithPlayerInput("Hello")
        .WithCanonicalFacts(new[] { "Fact 1" })
        .WithMaxAttempts(3)
        .Build();

      var retry = snapshot.ForRetry();

      Assert.That(retry.SystemPrompt, Is.EqualTo(snapshot.SystemPrompt));
      Assert.That(retry.PlayerInput, Is.EqualTo(snapshot.PlayerInput));
      Assert.That(retry.CanonicalFacts.Count, Is.EqualTo(1));
      Assert.That(retry.MaxAttempts, Is.EqualTo(snapshot.MaxAttempts));
    }

    [Test]
    public void GetAllMemoryForPrompt_FormatsCorrectly()
    {
      var snapshot = new StateSnapshotBuilder()
        .WithCanonicalFacts(new[] { "The king is Arthur" })
        .WithWorldState(new[] { "door: open" })
        .WithEpisodicMemories(new[] { "Player arrived" })
        .WithBeliefs(new[] { "Player is friendly" })
        .Build();

      var memories = snapshot.GetAllMemoryForPrompt().ToList();

      Assert.That(memories.Count, Is.EqualTo(4));
      Assert.That(memories[0], Is.EqualTo("[Fact] The king is Arthur"));
      Assert.That(memories[1], Is.EqualTo("[State] door: open"));
      Assert.That(memories[2], Is.EqualTo("[Memory] Player arrived"));
      Assert.That(memories[3], Is.EqualTo("Player is friendly")); // Beliefs don't have prefix
    }

    [Test]
    public void ToString_ReturnsReadableFormat()
    {
      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibit("test", "test"));

      var snapshot = new StateSnapshotBuilder()
        .WithConstraints(constraints)
        .WithCanonicalFacts(new[] { "Fact" })
        .WithWorldState(new[] { "State" })
        .Build();

      var str = snapshot.ToString();

      Assert.That(str, Does.Contain("StateSnapshot"));
      Assert.That(str, Does.Contain("Attempt 1/3"));
      Assert.That(str, Does.Contain("2 memories"));
      Assert.That(str, Does.Contain("1 constraints"));
    }
  }
}
