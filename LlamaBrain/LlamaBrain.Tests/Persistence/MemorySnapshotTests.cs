using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;
using LlamaBrain.Persistence;
using LlamaBrain.Persistence.Dtos;

namespace LlamaBrain.Tests.Persistence
{
  /// <summary>
  /// Tests for MemorySnapshotBuilder and MemorySnapshotRestorer.
  /// Verifies deterministic round-trip serialization.
  /// </summary>
  [TestFixture]
  public class MemorySnapshotTests
  {
    private IClock _clock = null!;
    private IIdGenerator _idGenerator = null!;

    [SetUp]
    public void SetUp()
    {
      _clock = new FixedClock(1000000000L);
      _idGenerator = new SequentialIdGenerator();
    }

    #region Snapshot Builder Tests

    [Test]
    public void CreateSnapshot_EmptyMemory_ReturnsEmptySnapshot()
    {
      // Arrange
      var memory = new AuthoritativeMemorySystem(_clock, _idGenerator);

      // Act
      var snapshot = MemorySnapshotBuilder.CreateSnapshot(memory, "test-persona");

      // Assert
      Assert.That(snapshot.PersonaId, Is.EqualTo("test-persona"));
      Assert.That(snapshot.NextSequenceNumber, Is.EqualTo(1));
      Assert.That(snapshot.CanonicalFacts, Is.Empty);
      Assert.That(snapshot.WorldState, Is.Empty);
      Assert.That(snapshot.EpisodicMemories, Is.Empty);
      Assert.That(snapshot.Beliefs, Is.Empty);
    }

    [Test]
    public void CreateSnapshot_WithCanonicalFacts_CapturesAllFields()
    {
      // Arrange
      var memory = new AuthoritativeMemorySystem(_clock, _idGenerator);
      memory.AddCanonicalFact("king-name", "The king is named Arthur", "royalty");

      // Act
      var snapshot = MemorySnapshotBuilder.CreateSnapshot(memory, "test");

      // Assert
      Assert.That(snapshot.CanonicalFacts.Count, Is.EqualTo(1));
      var dto = snapshot.CanonicalFacts[0];
      Assert.That(dto.Id, Is.EqualTo("king-name"));
      Assert.That(dto.Fact, Is.EqualTo("The king is named Arthur"));
      Assert.That(dto.Domain, Is.EqualTo("royalty"));
      Assert.That(dto.SequenceNumber, Is.EqualTo(1));
      Assert.That(dto.Source, Is.EqualTo((int)MutationSource.Designer));
    }

    [Test]
    public void CreateSnapshot_WithWorldState_CapturesKeyAndAllFields()
    {
      // Arrange
      var memory = new AuthoritativeMemorySystem(_clock, _idGenerator);
      memory.SetWorldState("door_castle", "open", MutationSource.GameSystem);

      // Act
      var snapshot = MemorySnapshotBuilder.CreateSnapshot(memory, "test");

      // Assert
      Assert.That(snapshot.WorldState.Count, Is.EqualTo(1));
      var dto = snapshot.WorldState[0];
      Assert.That(dto.Key, Is.EqualTo("door_castle"));
      Assert.That(dto.Value, Is.EqualTo("open"));
      Assert.That(dto.Source, Is.EqualTo((int)MutationSource.GameSystem));
    }

    [Test]
    public void CreateSnapshot_WithEpisodicMemories_CapturesAllFields()
    {
      // Arrange
      var memory = new AuthoritativeMemorySystem(_clock, _idGenerator);
      memory.AddDialogue("Player", "Hello there", 0.8f, MutationSource.ValidatedOutput);

      // Act
      var snapshot = MemorySnapshotBuilder.CreateSnapshot(memory, "test");

      // Assert
      Assert.That(snapshot.EpisodicMemories.Count, Is.EqualTo(1));
      var dto = snapshot.EpisodicMemories[0];
      Assert.That(dto.Description, Is.EqualTo("Player: Hello there"));
      Assert.That(dto.Significance, Is.EqualTo(0.8f).Within(0.001f));
      Assert.That(dto.EpisodeType, Is.EqualTo((int)EpisodeType.Dialogue));
    }

    [Test]
    public void CreateSnapshot_WithBeliefs_CapturesKeyAndAllFields()
    {
      // Arrange
      var memory = new AuthoritativeMemorySystem(_clock, _idGenerator);
      var belief = BeliefMemoryEntry.CreateRelationship("Player", "is friendly", 0.7f);
      memory.SetBelief("player-relation", belief, MutationSource.ValidatedOutput);

      // Act
      var snapshot = MemorySnapshotBuilder.CreateSnapshot(memory, "test");

      // Assert
      Assert.That(snapshot.Beliefs.Count, Is.EqualTo(1));
      var dto = snapshot.Beliefs[0];
      Assert.That(dto.Key, Is.EqualTo("player-relation"));
      Assert.That(dto.Subject, Is.EqualTo("Player"));
      Assert.That(dto.BeliefContent, Is.EqualTo("is friendly"));
      Assert.That(dto.Sentiment, Is.EqualTo(0.7f).Within(0.001f));
    }

    [Test]
    public void CreateSnapshot_PreservesSequenceNumbers()
    {
      // Arrange
      var memory = new AuthoritativeMemorySystem(_clock, _idGenerator);
      memory.AddCanonicalFact("fact1", "First fact");
      memory.AddDialogue("Player", "Second thing", 0.5f);
      memory.AddCanonicalFact("fact2", "Third fact");

      // Act
      var snapshot = MemorySnapshotBuilder.CreateSnapshot(memory, "test");

      // Assert
      Assert.That(snapshot.NextSequenceNumber, Is.EqualTo(4));
      Assert.That(snapshot.CanonicalFacts[0].SequenceNumber, Is.EqualTo(1));
      Assert.That(snapshot.EpisodicMemories[0].SequenceNumber, Is.EqualTo(2));
      Assert.That(snapshot.CanonicalFacts[1].SequenceNumber, Is.EqualTo(3));
    }

    #endregion

    #region Snapshot Restorer Tests

    [Test]
    public void RestoreSnapshot_EmptySnapshot_ClearsMemory()
    {
      // Arrange
      var memory = new AuthoritativeMemorySystem(_clock, _idGenerator);
      memory.AddCanonicalFact("old", "Old fact");

      var emptySnapshot = new PersonaMemorySnapshot { PersonaId = "test", NextSequenceNumber = 1 };

      // Act
      MemorySnapshotRestorer.RestoreSnapshot(memory, emptySnapshot);

      // Assert
      Assert.That(memory.GetCanonicalFacts().Count(), Is.EqualTo(0));
      Assert.That(memory.NextSequenceNumber, Is.EqualTo(1));
    }

    [Test]
    public void RestoreSnapshot_WithCanonicalFacts_RestoresAllFields()
    {
      // Arrange
      var memory = new AuthoritativeMemorySystem(_clock, _idGenerator);
      var snapshot = new PersonaMemorySnapshot
      {
        PersonaId = "test",
        NextSequenceNumber = 2,
        CanonicalFacts = new List<CanonicalFactDto>
        {
          new CanonicalFactDto
          {
            Id = "king-name",
            Fact = "The king is Arthur",
            Domain = "royalty",
            CreatedAtTicks = 12345,
            SequenceNumber = 1,
            Source = (int)MutationSource.Designer
          }
        }
      };

      // Act
      MemorySnapshotRestorer.RestoreSnapshot(memory, snapshot);

      // Assert
      var fact = memory.GetCanonicalFact("king-name");
      Assert.That(fact, Is.Not.Null);
      Assert.That(fact!.Fact, Is.EqualTo("The king is Arthur"));
      Assert.That(fact.Domain, Is.EqualTo("royalty"));
      Assert.That(fact.CreatedAtTicks, Is.EqualTo(12345));
      Assert.That(fact.SequenceNumber, Is.EqualTo(1));
    }

    [Test]
    public void RestoreSnapshot_WithWorldState_RestoresKeyAndAllFields()
    {
      // Arrange
      var memory = new AuthoritativeMemorySystem(_clock, _idGenerator);
      var snapshot = new PersonaMemorySnapshot
      {
        PersonaId = "test",
        NextSequenceNumber = 2,
        WorldState = new List<WorldStateDto>
        {
          new WorldStateDto
          {
            Id = "ws-1",
            Key = "door_castle",
            Value = "open",
            CreatedAtTicks = 12345,
            ModifiedAtTicks = 12346,
            ModificationCount = 1,
            SequenceNumber = 1,
            Source = (int)MutationSource.GameSystem
          }
        }
      };

      // Act
      MemorySnapshotRestorer.RestoreSnapshot(memory, snapshot);

      // Assert
      var state = memory.GetWorldState("door_castle");
      Assert.That(state, Is.Not.Null);
      Assert.That(state!.Value, Is.EqualTo("open"));
      Assert.That(state.ModificationCount, Is.EqualTo(1));
    }

    [Test]
    public void RestoreSnapshot_RestoresNextSequenceNumber()
    {
      // Arrange
      var memory = new AuthoritativeMemorySystem(_clock, _idGenerator);
      var snapshot = new PersonaMemorySnapshot
      {
        PersonaId = "test",
        NextSequenceNumber = 42
      };

      // Act
      MemorySnapshotRestorer.RestoreSnapshot(memory, snapshot);

      // Assert
      Assert.That(memory.NextSequenceNumber, Is.EqualTo(42));
    }

    #endregion

    #region Round-Trip Determinism Tests

    [Test]
    public void RoundTrip_PreservesAllMemoryState()
    {
      // Arrange - create memory with all types
      var original = new AuthoritativeMemorySystem(_clock, _idGenerator);
      original.AddCanonicalFact("fact1", "The sky is blue", "world");
      original.SetWorldState("door", "closed", MutationSource.GameSystem);
      original.AddDialogue("Player", "Hello", 0.6f);
      var belief = BeliefMemoryEntry.CreateOpinion("Player", "seems friendly", 0.5f, 0.7f);
      original.SetBelief("player-opinion", belief, MutationSource.ValidatedOutput);

      // Act - snapshot and restore
      var snapshot = MemorySnapshotBuilder.CreateSnapshot(original, "test");
      var restored = new AuthoritativeMemorySystem(_clock, _idGenerator);
      MemorySnapshotRestorer.RestoreSnapshot(restored, snapshot);

      // Assert - verify all state matches
      Assert.That(restored.NextSequenceNumber, Is.EqualTo(original.NextSequenceNumber));

      var originalFacts = original.GetCanonicalFacts().ToList();
      var restoredFacts = restored.GetCanonicalFacts().ToList();
      Assert.That(restoredFacts.Count, Is.EqualTo(originalFacts.Count));
      Assert.That(restoredFacts[0].Fact, Is.EqualTo(originalFacts[0].Fact));

      var originalState = original.GetWorldState("door");
      var restoredState = restored.GetWorldState("door");
      Assert.That(restoredState?.Value, Is.EqualTo(originalState?.Value));
    }

    [Test]
    public void RoundTrip_PreservesSequenceNumbers()
    {
      // Arrange
      var original = new AuthoritativeMemorySystem(_clock, _idGenerator);
      original.AddCanonicalFact("f1", "Fact 1");
      original.AddDialogue("P", "M1", 0.5f);
      original.AddCanonicalFact("f2", "Fact 2");
      original.AddDialogue("P", "M2", 0.5f);

      // Act
      var snapshot = MemorySnapshotBuilder.CreateSnapshot(original, "test");
      var restored = new AuthoritativeMemorySystem(_clock, _idGenerator);
      MemorySnapshotRestorer.RestoreSnapshot(restored, snapshot);

      // Assert
      var originalFacts = original.GetCanonicalFacts().OrderBy(f => f.SequenceNumber).ToList();
      var restoredFacts = restored.GetCanonicalFacts().OrderBy(f => f.SequenceNumber).ToList();

      for (int i = 0; i < originalFacts.Count; i++)
      {
        Assert.That(restoredFacts[i].SequenceNumber, Is.EqualTo(originalFacts[i].SequenceNumber),
          $"Fact {i} sequence number mismatch");
      }
    }

    [Test]
    public void RoundTrip_PreservesCreatedAtTicks()
    {
      // Arrange
      var original = new AuthoritativeMemorySystem(_clock, _idGenerator);
      original.AddCanonicalFact("f1", "Fact 1");

      // Act
      var snapshot = MemorySnapshotBuilder.CreateSnapshot(original, "test");
      var restored = new AuthoritativeMemorySystem(_clock, _idGenerator);
      MemorySnapshotRestorer.RestoreSnapshot(restored, snapshot);

      // Assert
      var originalFact = original.GetCanonicalFact("f1");
      var restoredFact = restored.GetCanonicalFact("f1");
      Assert.That(restoredFact?.CreatedAtTicks, Is.EqualTo(originalFact?.CreatedAtTicks));
    }

    [Test]
    public void RoundTrip_MultipleSnapshots_AreDeterministic()
    {
      // Arrange
      var memory = new AuthoritativeMemorySystem(_clock, _idGenerator);
      memory.AddCanonicalFact("f1", "Fact");
      memory.AddDialogue("P", "Message", 0.5f);

      // Act - take multiple snapshots
      var snapshot1 = MemorySnapshotBuilder.CreateSnapshot(memory, "test");
      var snapshot2 = MemorySnapshotBuilder.CreateSnapshot(memory, "test");

      // Assert - snapshots should be identical
      Assert.That(snapshot1.NextSequenceNumber, Is.EqualTo(snapshot2.NextSequenceNumber));
      Assert.That(snapshot1.CanonicalFacts.Count, Is.EqualTo(snapshot2.CanonicalFacts.Count));
      Assert.That(snapshot1.CanonicalFacts[0].SequenceNumber,
        Is.EqualTo(snapshot2.CanonicalFacts[0].SequenceNumber));
    }

    #endregion
  }
}
