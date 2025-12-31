using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Expectancy;

namespace LlamaBrain.Tests.Inference
{
  /// <summary>
  /// Tests for EphemeralWorkingMemory and WorkingMemoryConfig.
  /// </summary>
  public class EphemeralWorkingMemoryTests
  {
    private StateSnapshot _defaultSnapshot = null!;

    [SetUp]
    public void SetUp()
    {
      var context = new InteractionContext { NpcId = "npc-001" };
      var constraints = new ConstraintSet();
      
      _defaultSnapshot = new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt("You are a friendly shopkeeper.")
        .WithPlayerInput("Hello!")
        .WithCanonicalFacts(new[] { "The king is Arthur" })
        .WithWorldState(new[] { "door_status=open" })
        .WithEpisodicMemories(new[] { "Player said hello", "NPC greeted player" })
        .WithBeliefs(new[] { "Player is trustworthy" })
        .WithDialogueHistory(new[] { "Player: Hi", "NPC: Hello there!" })
        .Build();
    }

    #region WorkingMemoryConfig Tests

    [Test]
    public void WorkingMemoryConfig_Default_HasCorrectValues()
    {
      // Act
      var config = WorkingMemoryConfig.Default;

      // Assert
      Assert.That(config.MaxDialogueExchanges, Is.EqualTo(5));
      Assert.That(config.MaxEpisodicMemories, Is.EqualTo(5));
      Assert.That(config.MaxBeliefs, Is.EqualTo(3));
      Assert.That(config.MaxContextCharacters, Is.EqualTo(2000));
      Assert.That(config.AlwaysIncludeCanonicalFacts, Is.True);
      Assert.That(config.AlwaysIncludeWorldState, Is.True);
    }

    [Test]
    public void WorkingMemoryConfig_Minimal_HasCorrectValues()
    {
      // Act
      var config = WorkingMemoryConfig.Minimal;

      // Assert
      Assert.That(config.MaxDialogueExchanges, Is.EqualTo(2));
      Assert.That(config.MaxEpisodicMemories, Is.EqualTo(2));
      Assert.That(config.MaxBeliefs, Is.EqualTo(1));
      Assert.That(config.MaxContextCharacters, Is.EqualTo(1000));
    }

    [Test]
    public void WorkingMemoryConfig_Expanded_HasCorrectValues()
    {
      // Act
      var config = WorkingMemoryConfig.Expanded;

      // Assert
      Assert.That(config.MaxDialogueExchanges, Is.EqualTo(10));
      Assert.That(config.MaxEpisodicMemories, Is.EqualTo(10));
      Assert.That(config.MaxBeliefs, Is.EqualTo(5));
      Assert.That(config.MaxContextCharacters, Is.EqualTo(4000));
    }

    #endregion

    #region EphemeralWorkingMemory Creation Tests

    [Test]
    public void EphemeralWorkingMemory_CreatesFromSnapshot()
    {
      // Act
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);

      // Assert
      Assert.That(workingMemory.SourceSnapshot, Is.EqualTo(_defaultSnapshot));
      Assert.That(workingMemory.SystemPrompt, Is.EqualTo("You are a friendly shopkeeper."));
      Assert.That(workingMemory.PlayerInput, Is.EqualTo("Hello!"));
      Assert.That(workingMemory.CreatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));
    }

    [Test]
    public void EphemeralWorkingMemory_NullSnapshot_ThrowsException()
    {
      // Act & Assert
      Assert.Throws<ArgumentNullException>(() => new EphemeralWorkingMemory(null!));
    }

    [Test]
    public void EphemeralWorkingMemory_WithCustomConfig_UsesConfig()
    {
      // Arrange
      var config = new WorkingMemoryConfig { MaxDialogueExchanges = 2, MaxEpisodicMemories = 1 };

      // Act
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot, config);

      // Assert
      Assert.That(workingMemory.DialogueHistory.Count, Is.LessThanOrEqualTo(4)); // 2 exchanges * 2 lines
      Assert.That(workingMemory.EpisodicMemories.Count, Is.LessThanOrEqualTo(1));
    }

    #endregion

    #region Bounds Tests

    [Test]
    public void EphemeralWorkingMemory_MaxDialogueExchanges_EnforcesLimit()
    {
      // Arrange
      var snapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext())
        .WithConstraints(new ConstraintSet())
        .WithDialogueHistory(Enumerable.Range(0, 20).Select(i => $"Line {i}").ToArray())
        .Build();
      var config = new WorkingMemoryConfig { MaxDialogueExchanges = 3 };

      // Act
      var workingMemory = new EphemeralWorkingMemory(snapshot, config);

      // Assert
      // 3 exchanges = 6 lines max
      Assert.That(workingMemory.DialogueHistory.Count, Is.LessThanOrEqualTo(6));
    }

    [Test]
    public void EphemeralWorkingMemory_MaxEpisodicMemories_EnforcesLimit()
    {
      // Arrange
      var snapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext())
        .WithConstraints(new ConstraintSet())
        .WithEpisodicMemories(Enumerable.Range(0, 20).Select(i => $"Memory {i}").ToArray())
        .Build();
      var config = new WorkingMemoryConfig { MaxEpisodicMemories = 5 };

      // Act
      var workingMemory = new EphemeralWorkingMemory(snapshot, config);

      // Assert
      Assert.That(workingMemory.EpisodicMemories.Count, Is.EqualTo(5));
    }

    [Test]
    public void EphemeralWorkingMemory_MaxBeliefs_EnforcesLimit()
    {
      // Arrange
      var snapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext())
        .WithConstraints(new ConstraintSet())
        .WithBeliefs(Enumerable.Range(0, 10).Select(i => $"Belief {i}").ToArray())
        .Build();
      var config = new WorkingMemoryConfig { MaxBeliefs = 3 };

      // Act
      var workingMemory = new EphemeralWorkingMemory(snapshot, config);

      // Assert
      Assert.That(workingMemory.Beliefs.Count, Is.EqualTo(3));
    }

    [Test]
    public void EphemeralWorkingMemory_AlwaysIncludeCanonicalFacts_IncludesAllFacts()
    {
      // Arrange
      var facts = Enumerable.Range(0, 10).Select(i => $"Fact {i}").ToArray();
      var snapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext())
        .WithConstraints(new ConstraintSet())
        .WithCanonicalFacts(facts)
        .Build();
      var config = new WorkingMemoryConfig 
      { 
        AlwaysIncludeCanonicalFacts = true,
        MaxContextCharacters = 100 // Very small limit
      };

      // Act
      var workingMemory = new EphemeralWorkingMemory(snapshot, config);

      // Assert
      Assert.That(workingMemory.CanonicalFacts.Count, Is.EqualTo(10));
    }

    [Test]
    public void EphemeralWorkingMemory_AlwaysIncludeWorldState_IncludesAllState()
    {
      // Arrange
      var states = Enumerable.Range(0, 10).Select(i => $"State {i}").ToArray();
      var snapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext())
        .WithConstraints(new ConstraintSet())
        .WithWorldState(states)
        .Build();
      var config = new WorkingMemoryConfig 
      { 
        AlwaysIncludeWorldState = true,
        MaxContextCharacters = 100 // Very small limit
      };

      // Act
      var workingMemory = new EphemeralWorkingMemory(snapshot, config);

      // Assert
      Assert.That(workingMemory.WorldState.Count, Is.EqualTo(10));
    }

    [Test]
    public void EphemeralWorkingMemory_NotAlwaysIncludeCanonicalFacts_CanExcludeFacts()
    {
      // Arrange
      var facts = Enumerable.Range(0, 10).Select(i => $"Fact {i}").ToArray();
      var snapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext())
        .WithConstraints(new ConstraintSet())
        .WithCanonicalFacts(facts)
        .Build();
      var config = new WorkingMemoryConfig 
      { 
        AlwaysIncludeCanonicalFacts = false,
        MaxContextCharacters = 50 // Very small limit
      };

      // Act
      var workingMemory = new EphemeralWorkingMemory(snapshot, config);

      // Assert
      // Facts may be excluded if they don't fit
      Assert.That(workingMemory.CanonicalFacts.Count, Is.LessThanOrEqualTo(10));
    }

    #endregion

    #region Truncation Tests

    [Test]
    public void EphemeralWorkingMemory_CharacterLimit_TruncatesContent()
    {
      // Arrange
      var longContent = new string('x', 1000);
      var snapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext())
        .WithConstraints(new ConstraintSet())
        .WithEpisodicMemories(Enumerable.Range(0, 20).Select(i => $"{longContent} {i}").ToArray())
        .Build();
      var config = new WorkingMemoryConfig 
      { 
        MaxContextCharacters = 500,
        MaxEpisodicMemories = 20 // Allow all, but character limit will truncate
      };

      // Act
      var workingMemory = new EphemeralWorkingMemory(snapshot, config);

      // Assert
      Assert.That(workingMemory.WasTruncated, Is.True);
      Assert.That(workingMemory.TotalCharacterCount, Is.LessThanOrEqualTo(500));
    }

    [Test]
    public void EphemeralWorkingMemory_CharacterLimit_PreservesCanonicalFacts()
    {
      // Arrange
      var facts = Enumerable.Range(0, 5).Select(i => $"Fact {i}").ToArray();
      var longMemories = Enumerable.Range(0, 20).Select(i => new string('x', 200)).ToArray();
      var snapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext())
        .WithConstraints(new ConstraintSet())
        .WithCanonicalFacts(facts)
        .WithEpisodicMemories(longMemories)
        .Build();
      var config = new WorkingMemoryConfig 
      { 
        MaxContextCharacters = 1000,
        AlwaysIncludeCanonicalFacts = true
      };

      // Act
      var workingMemory = new EphemeralWorkingMemory(snapshot, config);

      // Assert
      Assert.That(workingMemory.CanonicalFacts.Count, Is.EqualTo(5)); // All facts preserved
    }

    [Test]
    public void EphemeralWorkingMemory_CharacterLimit_TruncatesDialogueFromEnd()
    {
      // Arrange
      var dialogue = Enumerable.Range(0, 20).Select(i => $"Dialogue line {i}").ToArray();
      var snapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext())
        .WithConstraints(new ConstraintSet())
        .WithDialogueHistory(dialogue)
        .Build();
      var config = new WorkingMemoryConfig 
      { 
        MaxContextCharacters = 200,
        MaxDialogueExchanges = 20 // Allow all, but character limit will truncate
      };

      // Act
      var workingMemory = new EphemeralWorkingMemory(snapshot, config);

      // Assert
      Assert.That(workingMemory.WasTruncated, Is.True);
      // Should keep most recent dialogue
      if (workingMemory.DialogueHistory.Count > 0)
      {
        Assert.That(workingMemory.DialogueHistory.Last(), Does.Contain("19")); // Most recent
      }
    }

    [Test]
    public void EphemeralWorkingMemory_CharacterLimit_AllocatesBudgetCorrectly()
    {
      // Arrange
      var longMemories = Enumerable.Range(0, 10).Select(i => new string('x', 100)).ToArray();
      var longBeliefs = Enumerable.Range(0, 10).Select(i => new string('y', 100)).ToArray();
      var snapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext())
        .WithConstraints(new ConstraintSet())
        .WithSystemPrompt("System")
        .WithPlayerInput("Input")
        .WithEpisodicMemories(longMemories)
        .WithBeliefs(longBeliefs)
        .Build();
      var config = new WorkingMemoryConfig 
      { 
        MaxContextCharacters = 500,
        MaxEpisodicMemories = 10,
        MaxBeliefs = 10
      };

      // Act
      var workingMemory = new EphemeralWorkingMemory(snapshot, config);

      // Assert
      Assert.That(workingMemory.WasTruncated, Is.True);
      Assert.That(workingMemory.TotalCharacterCount, Is.LessThanOrEqualTo(500));
    }

    #endregion

    #region Formatting Tests

    [Test]
    public void GetFormattedContext_FormatsAllMemoryTypes()
    {
      // Arrange
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);

      // Act
      var formatted = workingMemory.GetFormattedContext();

      // Assert
      Assert.That(formatted, Does.Contain("[Fact]"));
      Assert.That(formatted, Does.Contain("[State]"));
      Assert.That(formatted, Does.Contain("[Memory]"));
      Assert.That(formatted, Does.Contain("trustworthy")); // Belief without prefix
    }

    [Test]
    public void GetFormattedContext_EmptyMemory_ReturnsEmpty()
    {
      // Arrange
      var emptySnapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext())
        .WithConstraints(new ConstraintSet())
        .Build();
      var workingMemory = new EphemeralWorkingMemory(emptySnapshot);

      // Act
      var formatted = workingMemory.GetFormattedContext();

      // Assert
      Assert.That(formatted, Is.Empty);
    }

    [Test]
    public void GetFormattedDialogue_ReturnsDialogueHistory()
    {
      // Arrange
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);

      // Act
      var formatted = workingMemory.GetFormattedDialogue();

      // Assert
      Assert.That(formatted, Does.Contain("Player: Hi"));
      Assert.That(formatted, Does.Contain("NPC: Hello there!"));
    }

    [Test]
    public void GetFormattedDialogue_EmptyHistory_ReturnsEmpty()
    {
      // Arrange
      var emptySnapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext())
        .WithConstraints(new ConstraintSet())
        .Build();
      var workingMemory = new EphemeralWorkingMemory(emptySnapshot);

      // Act
      var formatted = workingMemory.GetFormattedDialogue();

      // Assert
      Assert.That(formatted, Is.Empty);
    }

    #endregion

    #region Stats Tests

    [Test]
    public void GetStats_ReturnsCorrectCounts()
    {
      // Arrange
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);

      // Act
      var stats = workingMemory.GetStats();

      // Assert
      Assert.That(stats.DialogueCount, Is.EqualTo(workingMemory.DialogueHistory.Count));
      Assert.That(stats.CanonicalFactCount, Is.EqualTo(workingMemory.CanonicalFacts.Count));
      Assert.That(stats.WorldStateCount, Is.EqualTo(workingMemory.WorldState.Count));
      Assert.That(stats.EpisodicMemoryCount, Is.EqualTo(workingMemory.EpisodicMemories.Count));
      Assert.That(stats.BeliefCount, Is.EqualTo(workingMemory.Beliefs.Count));
      Assert.That(stats.ConstraintCount, Is.EqualTo(workingMemory.Constraints.Count));
      Assert.That(stats.TotalCharacters, Is.EqualTo(workingMemory.TotalCharacterCount));
      Assert.That(stats.WasTruncated, Is.EqualTo(workingMemory.WasTruncated));
    }

    [Test]
    public void GetStats_TotalItems_CalculatesCorrectly()
    {
      // Arrange
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);

      // Act
      var stats = workingMemory.GetStats();

      // Assert
      var expected = stats.DialogueCount + stats.CanonicalFactCount + stats.WorldStateCount +
                     stats.EpisodicMemoryCount + stats.BeliefCount;
      Assert.That(stats.TotalItems, Is.EqualTo(expected));
    }

    [Test]
    public void GetStats_ToString_ReturnsFormattedString()
    {
      // Arrange
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);

      // Act
      var stats = workingMemory.GetStats();
      var str = stats.ToString();

      // Assert
      Assert.That(str, Does.Contain("WorkingMemoryStats"));
      Assert.That(str, Does.Contain("items"));
      Assert.That(str, Does.Contain("chars"));
    }

    #endregion

    #region Disposal Tests

    [Test]
    public void Dispose_ClearsAllReferences()
    {
      // Arrange
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);
      var originalDialogueCount = workingMemory.DialogueHistory.Count;
      var originalFactsCount = workingMemory.CanonicalFacts.Count;

      // Act
      workingMemory.Dispose();

      // Assert
      Assert.That(workingMemory.DialogueHistory, Is.Empty);
      Assert.That(workingMemory.CanonicalFacts, Is.Empty);
      Assert.That(workingMemory.WorldState, Is.Empty);
      Assert.That(workingMemory.EpisodicMemories, Is.Empty);
      Assert.That(workingMemory.Beliefs, Is.Empty);
      Assert.That(workingMemory.SystemPrompt, Is.Empty);
      Assert.That(workingMemory.PlayerInput, Is.Empty);
      Assert.That(workingMemory.Constraints.Count, Is.EqualTo(0));
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
      // Arrange
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);

      // Act & Assert
      Assert.DoesNotThrow(() =>
      {
        workingMemory.Dispose();
        workingMemory.Dispose();
        workingMemory.Dispose();
      });
    }

    [Test]
    public void Dispose_UsingStatement_DisposesCorrectly()
    {
      // Arrange
      EphemeralWorkingMemory? disposedMemory = null;

      // Act
      using (var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot))
      {
        disposedMemory = workingMemory;
        Assert.That(disposedMemory.DialogueHistory.Count, Is.GreaterThan(0));
      }

      // Assert
      Assert.That(disposedMemory!.DialogueHistory, Is.Empty);
    }

    #endregion

    #region ToString Tests

    [Test]
    public void ToString_ReturnsFormattedString()
    {
      // Arrange
      var workingMemory = new EphemeralWorkingMemory(_defaultSnapshot);

      // Act
      var str = workingMemory.ToString();

      // Assert
      Assert.That(str, Does.Contain("WorkingMemory["));
      Assert.That(str, Does.Contain("items"));
      Assert.That(str, Does.Contain("chars"));
    }

    [Test]
    public void ToString_WithTruncation_ShowsTruncated()
    {
      // Arrange
      var longContent = new string('x', 1000);
      var snapshot = new StateSnapshotBuilder()
        .WithContext(new InteractionContext())
        .WithConstraints(new ConstraintSet())
        .WithEpisodicMemories(Enumerable.Range(0, 20).Select(i => $"{longContent} {i}").ToArray())
        .Build();
      var config = new WorkingMemoryConfig { MaxContextCharacters = 500 };
      var workingMemory = new EphemeralWorkingMemory(snapshot, config);

      // Act
      var str = workingMemory.ToString();

      // Assert
      Assert.That(str, Does.Contain("truncated"));
    }

    #endregion
  }
}

