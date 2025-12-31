using System.Linq;
using NUnit.Framework;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;

namespace LlamaBrain.Tests.Memory
{
  /// <summary>
  /// Tests for PersonaMemoryStore using the structured memory system.
  /// </summary>
  public class PersonaMemoryStoreTests
  {
    private PersonaMemoryStore _store = null!;

    [SetUp]
    public void SetUp()
    {
      _store = new PersonaMemoryStore();
    }

    #region Convenience API Tests

    [Test]
    public void AddMemory_ByPersonaId_Works()
    {
      // Act
      _store.AddMemory("npc-001", "Player said hello");

      // Assert
      var memories = _store.GetMemory("npc-001");
      Assert.That(memories.Count, Is.GreaterThan(0));
      Assert.That(memories.Any(m => m.Contains("Player said hello")), Is.True);
    }

    [Test]
    public void AddMemory_ByProfile_Works()
    {
      // Arrange
      var profile = PersonaProfile.Create("npc-001", "Test NPC");

      // Act
      _store.AddMemory(profile, "Player said hello");

      // Assert
      var memories = _store.GetMemory(profile);
      Assert.That(memories.Count, Is.GreaterThan(0));
    }

    [Test]
    public void AddMemory_NullProfile_ThrowsException()
    {
      // Act & Assert
      Assert.Throws<System.ArgumentException>(() => _store.AddMemory((PersonaProfile)null!, "test"));
    }

    [Test]
    public void GetMemory_NonexistentPersona_ReturnsEmpty()
    {
      // Act
      var memories = _store.GetMemory("nonexistent");

      // Assert
      Assert.That(memories, Is.Empty);
    }

    [Test]
    public void ClearMemory_RemovesAllMemories()
    {
      // Arrange
      _store.AddMemory("npc-001", "Memory 1");
      _store.AddMemory("npc-001", "Memory 2");

      // Act
      _store.ClearMemory("npc-001");

      // Assert
      var memories = _store.GetMemory("npc-001");
      Assert.That(memories, Is.Empty);
    }

    [Test]
    public void ClearMemory_ByProfile_Works()
    {
      // Arrange
      var profile = PersonaProfile.Create("npc-001", "Test NPC");
      _store.AddMemory(profile, "Memory 1");

      // Act
      _store.ClearMemory(profile);

      // Assert
      var memories = _store.GetMemory(profile);
      Assert.That(memories, Is.Empty);
    }

    #endregion


    #region New Structured API Tests

    [Test]
    public void AddCanonicalFact_CreatesImmutableFact()
    {
      // Act
      var result = _store.AddCanonicalFact("npc-001", "king-name", "The king is Arthur");

      // Assert
      Assert.That(result.Success, Is.True);
      var system = _store.GetSystem("npc-001");
      Assert.That(system, Is.Not.Null);
      Assert.That(system!.GetCanonicalFact("king-name")!.Fact, Is.EqualTo("The king is Arthur"));
    }

    [Test]
    public void SetWorldState_CreatesStateEntry()
    {
      // Act
      var result = _store.SetWorldState("npc-001", "door_status", "open");

      // Assert
      Assert.That(result.Success, Is.True);
      var system = _store.GetSystem("npc-001");
      Assert.That(system!.GetWorldState("door_status")!.Value, Is.EqualTo("open"));
    }

    [Test]
    public void AddDialogue_CreatesEpisodicMemory()
    {
      // Act
      var result = _store.AddDialogue("npc-001", "Player", "Hello!");

      // Assert
      Assert.That(result.Success, Is.True);
      var memories = _store.GetMemory("npc-001");
      Assert.That(memories.Any(m => m.Contains("Hello!")), Is.True);
    }

    [Test]
    public void SetBelief_CreatesBeliefMemory()
    {
      // Act
      var result = _store.SetBelief("npc-001", "player-trust", "Player", "is trustworthy", 0.8f);

      // Assert
      Assert.That(result.Success, Is.True);
      var system = _store.GetSystem("npc-001");
      var belief = system!.GetBelief("player-trust");
      Assert.That(belief, Is.Not.Null);
      Assert.That(belief!.Confidence, Is.EqualTo(0.8f));
    }

    [Test]
    public void SetRelationship_CreatesRelationshipBelief()
    {
      // Act
      var result = _store.SetRelationship("npc-001", "player", "trusted friend", 0.7f);

      // Assert
      Assert.That(result.Success, Is.True);
      var system = _store.GetSystem("npc-001");
      var belief = system!.GetBelief("rel_player");
      Assert.That(belief, Is.Not.Null);
      Assert.That(belief!.BeliefType, Is.EqualTo(BeliefType.Relationship));
      Assert.That(belief.Sentiment, Is.EqualTo(0.7f));
    }

    [Test]
    public void GetStatistics_ReturnsMemoryStats()
    {
      // Arrange
      _store.AddCanonicalFact("npc-001", "f1", "Fact");
      _store.AddDialogue("npc-001", "Player", "Hello");

      // Act
      var stats = _store.GetStatistics("npc-001");

      // Assert
      Assert.That(stats, Is.Not.Null);
      Assert.That(stats!.CanonicalFactCount, Is.EqualTo(1));
      Assert.That(stats.EpisodicMemoryCount, Is.EqualTo(1));
    }

    [Test]
    public void ApplyDecay_ReducesEpisodicStrength()
    {
      // Arrange
      _store.AddDialogue("npc-001", "Player", "Hello");
      var system = _store.GetSystem("npc-001");
      var memoryBefore = system!.GetActiveEpisodicMemories().First();
      var strengthBefore = memoryBefore.Strength;

      // Act
      _store.ApplyDecay("npc-001");

      // Assert
      Assert.That(memoryBefore.Strength, Is.LessThan(strengthBefore));
    }

    [Test]
    public void ApplyDecayAll_AffectsAllPersonas()
    {
      // Arrange
      _store.AddDialogue("npc-001", "Player", "Hello");
      _store.AddDialogue("npc-002", "Player", "Hi");

      // Act
      _store.ApplyDecayAll();

      // Assert
      var system1 = _store.GetSystem("npc-001");
      var system2 = _store.GetSystem("npc-002");
      Assert.That(system1!.GetActiveEpisodicMemories().First().Strength, Is.LessThan(1.0f));
      Assert.That(system2!.GetActiveEpisodicMemories().First().Strength, Is.LessThan(1.0f));
    }

    #endregion

    #region GetOrCreateSystem Tests

    [Test]
    public void GetOrCreateSystem_CreatesNewSystem()
    {
      // Act
      var system = _store.GetOrCreateSystem("npc-001");

      // Assert
      Assert.That(system, Is.Not.Null);
    }

    [Test]
    public void GetOrCreateSystem_ReturnsSameInstance()
    {
      // Act
      var system1 = _store.GetOrCreateSystem("npc-001");
      var system2 = _store.GetOrCreateSystem("npc-001");

      // Assert
      Assert.That(system2, Is.SameAs(system1));
    }

    [Test]
    public void GetSystem_NonexistentPersona_ReturnsNull()
    {
      // Act
      var system = _store.GetSystem("nonexistent");

      // Assert
      Assert.That(system, Is.Null);
    }

    #endregion

    #region Mixed Memory Types in Prompt

    [Test]
    public void GetMemory_IncludesAllMemoryTypes()
    {
      // Arrange
      _store.AddCanonicalFact("npc-001", "f1", "The world is round");
      _store.SetWorldState("npc-001", "weather", "sunny");
      _store.AddDialogue("npc-001", "Player", "Hello");
      _store.SetBelief("npc-001", "b1", "Player", "is friendly");

      // Act
      var memories = _store.GetMemory("npc-001");

      // Assert
      Assert.That(memories.Count, Is.EqualTo(4));
      Assert.That(memories.Any(m => m.Contains("[Fact]")), Is.True);
      Assert.That(memories.Any(m => m.Contains("[State]")), Is.True);
      Assert.That(memories.Any(m => m.Contains("[Memory]")), Is.True);
    }

    #endregion
  }
}
