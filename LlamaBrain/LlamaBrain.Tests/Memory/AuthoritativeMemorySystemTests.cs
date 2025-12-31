using System.Linq;
using NUnit.Framework;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;

namespace LlamaBrain.Tests.Memory
{
  /// <summary>
  /// Tests for the AuthoritativeMemorySystem.
  /// </summary>
  public class AuthoritativeMemorySystemTests
  {
    private AuthoritativeMemorySystem _system = null!;

    [SetUp]
    public void SetUp()
    {
      _system = new AuthoritativeMemorySystem();
    }

    #region Canonical Facts Tests

    [Test]
    public void AddCanonicalFact_Succeeds()
    {
      // Act
      var result = _system.AddCanonicalFact("king-name", "The king's name is Arthur", "lore");

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(_system.GetCanonicalFact("king-name"), Is.Not.Null);
      Assert.That(_system.GetCanonicalFact("king-name")!.Fact, Is.EqualTo("The king's name is Arthur"));
    }

    [Test]
    public void AddCanonicalFact_DuplicateId_Fails()
    {
      // Arrange
      _system.AddCanonicalFact("king-name", "The king's name is Arthur");

      // Act
      var result = _system.AddCanonicalFact("king-name", "The king's name is Bob");

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.FailureReason, Does.Contain("already exists"));
      Assert.That(_system.GetCanonicalFact("king-name")!.Fact, Is.EqualTo("The king's name is Arthur")); // Unchanged
    }

    [Test]
    public void GetCanonicalFacts_FiltersByDomain()
    {
      // Arrange
      _system.AddCanonicalFact("f1", "Fact 1", "lore");
      _system.AddCanonicalFact("f2", "Fact 2", "world");
      _system.AddCanonicalFact("f3", "Fact 3", "lore");

      // Act
      var loreFacts = _system.GetCanonicalFacts("lore").ToList();

      // Assert
      Assert.That(loreFacts.Count, Is.EqualTo(2));
      Assert.That(loreFacts.All(f => f.Domain == "lore"), Is.True);
    }

    #endregion

    #region World State Tests

    [Test]
    public void SetWorldState_NewKey_CreatesEntry()
    {
      // Act
      var result = _system.SetWorldState("door_castle", "open", MutationSource.GameSystem);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(_system.GetWorldState("door_castle")!.Value, Is.EqualTo("open"));
    }

    [Test]
    public void SetWorldState_ExistingKey_UpdatesValue()
    {
      // Arrange
      _system.SetWorldState("door_castle", "open", MutationSource.GameSystem);

      // Act
      var result = _system.SetWorldState("door_castle", "closed", MutationSource.GameSystem);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(_system.GetWorldState("door_castle")!.Value, Is.EqualTo("closed"));
    }

    [Test]
    public void SetWorldState_InsufficientAuthority_Fails()
    {
      // Act
      var result = _system.SetWorldState("door_castle", "open", MutationSource.LlmSuggestion);

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(_system.GetWorldState("door_castle"), Is.Null);
    }

    [Test]
    public void GetAllWorldState_ReturnsAllEntries()
    {
      // Arrange
      _system.SetWorldState("door_castle", "open", MutationSource.GameSystem);
      _system.SetWorldState("player_gold", "100", MutationSource.GameSystem);

      // Act
      var allState = _system.GetAllWorldState().ToList();

      // Assert
      Assert.That(allState.Count, Is.EqualTo(2));
    }

    #endregion

    #region Episodic Memory Tests

    [Test]
    public void AddEpisodicMemory_WithValidAuthority_Succeeds()
    {
      // Arrange
      var entry = new EpisodicMemoryEntry("Player said hello");

      // Act
      var result = _system.AddEpisodicMemory(entry, MutationSource.ValidatedOutput);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(_system.GetActiveEpisodicMemories().Count(), Is.EqualTo(1));
    }

    [Test]
    public void AddEpisodicMemory_WithInsufficientAuthority_Fails()
    {
      // Arrange
      var entry = new EpisodicMemoryEntry("Player said hello");

      // Act
      var result = _system.AddEpisodicMemory(entry, MutationSource.LlmSuggestion);

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(_system.GetActiveEpisodicMemories().Count(), Is.EqualTo(0));
    }

    [Test]
    public void AddDialogue_CreatesEpisodicMemory()
    {
      // Act
      var result = _system.AddDialogue("Player", "Hello there!", 0.7f);

      // Assert
      Assert.That(result.Success, Is.True);
      var memories = _system.GetActiveEpisodicMemories().ToList();
      Assert.That(memories.Count, Is.EqualTo(1));
      Assert.That(memories[0].Participant, Is.EqualTo("Player"));
    }

    [Test]
    public void GetRecentMemories_ReturnsInChronologicalOrder()
    {
      // Arrange
      _system.AddDialogue("Player", "First");
      _system.AddDialogue("Player", "Second");
      _system.AddDialogue("Player", "Third");

      // Act
      var recent = _system.GetRecentMemories(2).ToList();

      // Assert
      Assert.That(recent.Count, Is.EqualTo(2));
      Assert.That(recent[0].Content, Does.Contain("Third")); // Most recent first
    }

    [Test]
    public void ApplyEpisodicDecay_ReducesMemoryStrength()
    {
      // Arrange
      _system.AddDialogue("Player", "Test");
      var memoryBefore = _system.GetActiveEpisodicMemories().First();
      var strengthBefore = memoryBefore.Strength;

      // Act
      _system.ApplyEpisodicDecay();

      // Assert
      Assert.That(memoryBefore.Strength, Is.LessThan(strengthBefore));
    }

    [Test]
    public void EpisodicMemories_PrunedWhenOverLimit()
    {
      // Arrange
      _system.MaxEpisodicMemories = 5;
      for (int i = 0; i < 10; i++)
      {
        _system.AddDialogue("Player", $"Message {i}");
      }

      // Assert
      Assert.That(_system.GetActiveEpisodicMemories().Count(), Is.LessThanOrEqualTo(5));
    }

    #endregion

    #region Belief Tests

    [Test]
    public void SetBelief_WithValidAuthority_Succeeds()
    {
      // Arrange
      var belief = BeliefMemoryEntry.CreateOpinion("Player", "is trustworthy");

      // Act
      var result = _system.SetBelief("player-trust", belief, MutationSource.ValidatedOutput);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(_system.GetBelief("player-trust"), Is.Not.Null);
    }

    [Test]
    public void SetBelief_ContradictingCanonicalFact_IsMarkedContradicted()
    {
      // Arrange
      _system.AddCanonicalFact("king-name", "The king's name is Arthur");
      var belief = BeliefMemoryEntry.CreateBelief("king", "The king's name is not Arthur");

      // Act
      _system.SetBelief("wrong-king", belief, MutationSource.ValidatedOutput);

      // Assert
      var savedBelief = _system.GetBelief("wrong-king");
      Assert.That(savedBelief!.IsContradicted, Is.True);
    }

    [Test]
    public void GetBeliefsAbout_FiltersCorrectly()
    {
      // Arrange
      _system.SetBelief("b1", BeliefMemoryEntry.CreateOpinion("Player", "is friendly"), MutationSource.ValidatedOutput);
      _system.SetBelief("b2", BeliefMemoryEntry.CreateOpinion("Guard", "is suspicious"), MutationSource.ValidatedOutput);
      _system.SetBelief("b3", BeliefMemoryEntry.CreateOpinion("Player", "is helpful"), MutationSource.ValidatedOutput);

      // Act
      var playerBeliefs = _system.GetBeliefsAbout("Player").ToList();

      // Assert
      Assert.That(playerBeliefs.Count, Is.EqualTo(2));
      Assert.That(playerBeliefs.All(b => b.Subject == "Player"), Is.True);
    }

    [Test]
    public void GetActiveBeliefs_ExcludesContradicted()
    {
      // Arrange
      _system.AddCanonicalFact("truth", "The sky is blue");
      _system.SetBelief("b1", BeliefMemoryEntry.CreateBelief("sky", "is green"), MutationSource.ValidatedOutput);
      _system.SetBelief("b2", BeliefMemoryEntry.CreateBelief("grass", "is green"), MutationSource.ValidatedOutput);

      // Mark b1 as contradicted manually (since our simple check won't catch it)
      _system.GetBelief("b1")!.MarkContradicted("Testing");

      // Act
      var active = _system.GetActiveBeliefs().ToList();

      // Assert
      Assert.That(active.Count, Is.EqualTo(1));
      Assert.That(active[0].Subject, Is.EqualTo("grass"));
    }

    #endregion

    #region Authority Validation Tests

    [Test]
    public void ValidateMutation_DesignerCanModifyAll()
    {
      // Assert
      Assert.That(_system.ValidateMutation(MemoryAuthority.Canonical, MutationSource.Designer), Is.True);
      Assert.That(_system.ValidateMutation(MemoryAuthority.WorldState, MutationSource.Designer), Is.True);
      Assert.That(_system.ValidateMutation(MemoryAuthority.Episodic, MutationSource.Designer), Is.True);
      Assert.That(_system.ValidateMutation(MemoryAuthority.Belief, MutationSource.Designer), Is.True);
    }

    [Test]
    public void ValidateMutation_GameSystemCannotModifyCanonical()
    {
      // Assert
      Assert.That(_system.ValidateMutation(MemoryAuthority.Canonical, MutationSource.GameSystem), Is.False);
      Assert.That(_system.ValidateMutation(MemoryAuthority.WorldState, MutationSource.GameSystem), Is.True);
    }

    [Test]
    public void ValidateMutation_LlmSuggestionHasLimitedAccess()
    {
      // Assert
      Assert.That(_system.ValidateMutation(MemoryAuthority.Canonical, MutationSource.LlmSuggestion), Is.False);
      Assert.That(_system.ValidateMutation(MemoryAuthority.WorldState, MutationSource.LlmSuggestion), Is.False);
      Assert.That(_system.ValidateMutation(MemoryAuthority.Episodic, MutationSource.LlmSuggestion), Is.False);
      Assert.That(_system.ValidateMutation(MemoryAuthority.Belief, MutationSource.LlmSuggestion), Is.True);
    }

    #endregion

    #region Unified Access Tests

    [Test]
    public void GetAllMemoriesForPrompt_ReturnsFormattedMemories()
    {
      // Arrange
      _system.AddCanonicalFact("f1", "The king is wise", "lore");
      _system.SetWorldState("door", "open", MutationSource.GameSystem);
      _system.AddDialogue("Player", "Hello");
      _system.SetBelief("b1", BeliefMemoryEntry.CreateOpinion("Player", "is friendly"), MutationSource.ValidatedOutput);

      // Act
      var memories = _system.GetAllMemoriesForPrompt().ToList();

      // Assert
      Assert.That(memories.Count, Is.EqualTo(4));
      Assert.That(memories.Any(m => m.Contains("[Fact]")), Is.True);
      Assert.That(memories.Any(m => m.Contains("[State]")), Is.True);
      Assert.That(memories.Any(m => m.Contains("[Memory]")), Is.True);
    }

    [Test]
    public void GetStatistics_ReturnsCorrectCounts()
    {
      // Arrange
      _system.AddCanonicalFact("f1", "Fact 1");
      _system.AddCanonicalFact("f2", "Fact 2");
      _system.SetWorldState("s1", "value", MutationSource.GameSystem);
      _system.AddDialogue("Player", "Hello");
      _system.AddDialogue("Player", "Goodbye");
      _system.SetBelief("b1", BeliefMemoryEntry.CreateOpinion("X", "Y"), MutationSource.ValidatedOutput);

      // Act
      var stats = _system.GetStatistics();

      // Assert
      Assert.That(stats.CanonicalFactCount, Is.EqualTo(2));
      Assert.That(stats.WorldStateCount, Is.EqualTo(1));
      Assert.That(stats.EpisodicMemoryCount, Is.EqualTo(2));
      Assert.That(stats.BeliefCount, Is.EqualTo(1));
    }

    [Test]
    public void ClearAll_RemovesAllMemories()
    {
      // Arrange
      _system.AddCanonicalFact("f1", "Fact");
      _system.SetWorldState("s1", "value", MutationSource.GameSystem);
      _system.AddDialogue("Player", "Hello");
      _system.SetBelief("b1", BeliefMemoryEntry.CreateOpinion("X", "Y"), MutationSource.ValidatedOutput);

      // Act
      _system.ClearAll();

      // Assert
      var stats = _system.GetStatistics();
      Assert.That(stats.CanonicalFactCount, Is.EqualTo(0));
      Assert.That(stats.WorldStateCount, Is.EqualTo(0));
      Assert.That(stats.EpisodicMemoryCount, Is.EqualTo(0));
      Assert.That(stats.BeliefCount, Is.EqualTo(0));
    }

    #endregion
  }
}
