using System;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.Inference
{
  [TestFixture]
  public class EphemeralWorkingMemoryTests
  {
    private StateSnapshot CreateTestSnapshot(
      int dialogueCount = 10,
      int factCount = 2,
      int stateCount = 2,
      int episodicCount = 10,
      int beliefCount = 5)
    {
      var builder = new StateSnapshotBuilder()
        .WithSystemPrompt("You are a helpful NPC.")
        .WithPlayerInput("Hello, how are you?");

      // Add dialogue history
      var dialogue = Enumerable.Range(0, dialogueCount)
        .Select(i => i % 2 == 0 ? $"Player: Message {i}" : $"NPC: Response {i}")
        .ToList();
      builder.WithDialogueHistory(dialogue);

      // Add canonical facts
      var facts = Enumerable.Range(0, factCount)
        .Select(i => $"Canonical fact {i}")
        .ToList();
      builder.WithCanonicalFacts(facts);

      // Add world state
      var states = Enumerable.Range(0, stateCount)
        .Select(i => $"state_{i}: value_{i}")
        .ToList();
      builder.WithWorldState(states);

      // Add episodic memories
      var memories = Enumerable.Range(0, episodicCount)
        .Select(i => $"Episodic memory {i}")
        .ToList();
      builder.WithEpisodicMemories(memories);

      // Add beliefs
      var beliefs = Enumerable.Range(0, beliefCount)
        .Select(i => $"Belief {i}")
        .ToList();
      builder.WithBeliefs(beliefs);

      return builder.Build();
    }

    [Test]
    public void Constructor_CreatesFromSnapshot()
    {
      var snapshot = CreateTestSnapshot();
      var memory = new EphemeralWorkingMemory(snapshot);

      Assert.That(memory.SourceSnapshot, Is.SameAs(snapshot));
      Assert.That(memory.SystemPrompt, Is.EqualTo(snapshot.SystemPrompt));
      Assert.That(memory.PlayerInput, Is.EqualTo(snapshot.PlayerInput));
      Assert.That(memory.CreatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));
    }

    [Test]
    public void Constructor_ThrowsOnNullSnapshot()
    {
      Assert.Throws<ArgumentNullException>(() => new EphemeralWorkingMemory(null));
    }

    [Test]
    public void DialogueHistory_BoundedByConfig()
    {
      var snapshot = CreateTestSnapshot(dialogueCount: 20);
      var config = new WorkingMemoryConfig { MaxDialogueExchanges = 3 }; // 6 lines max

      var memory = new EphemeralWorkingMemory(snapshot, config);

      // Should have at most 6 lines (3 exchanges * 2)
      Assert.That(memory.DialogueHistory.Count, Is.LessThanOrEqualTo(6));
    }

    [Test]
    public void DialogueHistory_KeepsMostRecent()
    {
      var snapshot = CreateTestSnapshot(dialogueCount: 10);
      var config = new WorkingMemoryConfig { MaxDialogueExchanges = 2 }; // 4 lines max

      var memory = new EphemeralWorkingMemory(snapshot, config);

      // Should keep the most recent messages
      if (memory.DialogueHistory.Count > 0)
      {
        var lastMessage = memory.DialogueHistory[memory.DialogueHistory.Count - 1];
        Assert.That(lastMessage, Does.Contain("9") | Does.Contain("8"));
      }
    }

    [Test]
    public void EpisodicMemories_BoundedByConfig()
    {
      var snapshot = CreateTestSnapshot(episodicCount: 20);
      var config = new WorkingMemoryConfig { MaxEpisodicMemories = 5 };

      var memory = new EphemeralWorkingMemory(snapshot, config);

      Assert.That(memory.EpisodicMemories.Count, Is.LessThanOrEqualTo(5));
    }

    [Test]
    public void Beliefs_BoundedByConfig()
    {
      var snapshot = CreateTestSnapshot(beliefCount: 10);
      var config = new WorkingMemoryConfig { MaxBeliefs = 2 };

      var memory = new EphemeralWorkingMemory(snapshot, config);

      Assert.That(memory.Beliefs.Count, Is.LessThanOrEqualTo(2));
    }

    [Test]
    public void CanonicalFacts_AlwaysIncluded_WhenConfigured()
    {
      var snapshot = CreateTestSnapshot(factCount: 5);
      var config = new WorkingMemoryConfig
      {
        AlwaysIncludeCanonicalFacts = true,
        MaxContextCharacters = 100 // Very small limit
      };

      var memory = new EphemeralWorkingMemory(snapshot, config);

      Assert.That(memory.CanonicalFacts.Count, Is.EqualTo(5));
    }

    [Test]
    public void WorldState_AlwaysIncluded_WhenConfigured()
    {
      var snapshot = CreateTestSnapshot(stateCount: 3);
      var config = new WorkingMemoryConfig
      {
        AlwaysIncludeWorldState = true,
        MaxContextCharacters = 100
      };

      var memory = new EphemeralWorkingMemory(snapshot, config);

      Assert.That(memory.WorldState.Count, Is.EqualTo(3));
    }

    [Test]
    public void TotalCharacterCount_IsCalculated()
    {
      var snapshot = CreateTestSnapshot();
      var memory = new EphemeralWorkingMemory(snapshot);

      Assert.That(memory.TotalCharacterCount, Is.GreaterThan(0));
    }

    [Test]
    public void WasTruncated_SetWhenExceedingLimit()
    {
      var snapshot = CreateTestSnapshot(dialogueCount: 100, episodicCount: 100);
      var config = new WorkingMemoryConfig
      {
        MaxContextCharacters = 200,
        AlwaysIncludeCanonicalFacts = false,
        AlwaysIncludeWorldState = false
      };

      var memory = new EphemeralWorkingMemory(snapshot, config);

      Assert.That(memory.WasTruncated, Is.True);
    }

    [Test]
    public void GetFormattedContext_FormatsCorrectly()
    {
      var snapshot = CreateTestSnapshot(factCount: 1, stateCount: 1, episodicCount: 1, beliefCount: 1);
      var memory = new EphemeralWorkingMemory(snapshot);

      var context = memory.GetFormattedContext();

      Assert.That(context, Does.Contain("[Fact]"));
      Assert.That(context, Does.Contain("[State]"));
      Assert.That(context, Does.Contain("[Memory]"));
    }

    [Test]
    public void GetFormattedDialogue_ReturnsDialogueLines()
    {
      var snapshot = CreateTestSnapshot(dialogueCount: 4);
      var memory = new EphemeralWorkingMemory(snapshot);

      var dialogue = memory.GetFormattedDialogue();

      Assert.That(dialogue, Does.Contain("Player:"));
      Assert.That(dialogue, Does.Contain("NPC:"));
    }

    [Test]
    public void GetStats_ReturnsCorrectCounts()
    {
      var snapshot = CreateTestSnapshot(
        dialogueCount: 6,
        factCount: 2,
        stateCount: 3,
        episodicCount: 4,
        beliefCount: 5);

      var memory = new EphemeralWorkingMemory(snapshot);
      var stats = memory.GetStats();

      Assert.That(stats.CanonicalFactCount, Is.EqualTo(2));
      Assert.That(stats.WorldStateCount, Is.EqualTo(3));
      Assert.That(stats.TotalCharacters, Is.GreaterThan(0));
    }

    [Test]
    public void Dispose_ClearsAllReferences()
    {
      var snapshot = CreateTestSnapshot();
      var memory = new EphemeralWorkingMemory(snapshot);

      memory.Dispose();

      Assert.That(memory.DialogueHistory.Count, Is.EqualTo(0));
      Assert.That(memory.CanonicalFacts.Count, Is.EqualTo(0));
      Assert.That(memory.WorldState.Count, Is.EqualTo(0));
      Assert.That(memory.EpisodicMemories.Count, Is.EqualTo(0));
      Assert.That(memory.Beliefs.Count, Is.EqualTo(0));
      Assert.That(memory.SystemPrompt, Is.Empty);
      Assert.That(memory.PlayerInput, Is.Empty);
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
      var snapshot = CreateTestSnapshot();
      var memory = new EphemeralWorkingMemory(snapshot);

      memory.Dispose();
      Assert.DoesNotThrow(() => memory.Dispose());
    }

    [Test]
    public void ToString_ReturnsUsefulInfo()
    {
      var snapshot = CreateTestSnapshot();
      var memory = new EphemeralWorkingMemory(snapshot);

      var str = memory.ToString();

      Assert.That(str, Does.Contain("WorkingMemory"));
      Assert.That(str, Does.Contain("items"));
      Assert.That(str, Does.Contain("chars"));
    }
  }

  [TestFixture]
  public class WorkingMemoryConfigTests
  {
    [Test]
    public void Default_HasReasonableValues()
    {
      var config = WorkingMemoryConfig.Default;

      Assert.That(config.MaxDialogueExchanges, Is.EqualTo(5));
      Assert.That(config.MaxEpisodicMemories, Is.EqualTo(5));
      Assert.That(config.MaxBeliefs, Is.EqualTo(3));
      Assert.That(config.MaxContextCharacters, Is.EqualTo(2000));
      Assert.That(config.AlwaysIncludeCanonicalFacts, Is.True);
      Assert.That(config.AlwaysIncludeWorldState, Is.True);
    }

    [Test]
    public void Minimal_HasSmallerValues()
    {
      var config = WorkingMemoryConfig.Minimal;

      Assert.That(config.MaxDialogueExchanges, Is.LessThan(WorkingMemoryConfig.Default.MaxDialogueExchanges));
      Assert.That(config.MaxContextCharacters, Is.LessThan(WorkingMemoryConfig.Default.MaxContextCharacters));
    }

    [Test]
    public void Expanded_HasLargerValues()
    {
      var config = WorkingMemoryConfig.Expanded;

      Assert.That(config.MaxDialogueExchanges, Is.GreaterThan(WorkingMemoryConfig.Default.MaxDialogueExchanges));
      Assert.That(config.MaxContextCharacters, Is.GreaterThan(WorkingMemoryConfig.Default.MaxContextCharacters));
    }
  }
}
