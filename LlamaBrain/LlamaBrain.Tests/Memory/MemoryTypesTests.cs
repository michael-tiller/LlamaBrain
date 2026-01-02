using NUnit.Framework;
using LlamaBrain.Persona.MemoryTypes;

namespace LlamaBrain.Tests.Memory
{
  /// <summary>
  /// Tests for individual memory types.
  /// </summary>
  public class MemoryTypesTests
  {
    #region CanonicalFact Tests

    [Test]
    public void CanonicalFact_Create_SetsPropertiesCorrectly()
    {
      // Act
      var fact = new CanonicalFact("The king's name is Arthur", "lore");

      // Assert
      Assert.That(fact.Fact, Is.EqualTo("The king's name is Arthur"));
      Assert.That(fact.Domain, Is.EqualTo("lore"));
      Assert.That(fact.Authority, Is.EqualTo(MemoryAuthority.Canonical));
      Assert.That(fact.Source, Is.EqualTo(MutationSource.Designer));
      Assert.That(fact.Content, Is.EqualTo("The king's name is Arthur"));
    }

    [Test]
    public void CanonicalFact_Create_WithId_SetsId()
    {
      // Act
      var fact = CanonicalFact.Create("king-name", "The king's name is Arthur");

      // Assert
      Assert.That(fact.Id, Is.EqualTo("king-name"));
    }

    [Test]
    public void CanonicalFact_HasHighestAuthority()
    {
      // Arrange
      var fact = new CanonicalFact("Test fact");

      // Assert
      Assert.That(fact.Authority, Is.EqualTo(MemoryAuthority.Canonical));
      Assert.That((int)fact.Authority, Is.EqualTo(100));
    }

    #endregion

    #region WorldState Tests

    [Test]
    public void WorldStateEntry_Create_SetsPropertiesCorrectly()
    {
      // Act
      var state = new WorldStateEntry("door_castle", "open");

      // Assert
      Assert.That(state.Key, Is.EqualTo("door_castle"));
      Assert.That(state.Value, Is.EqualTo("open"));
      Assert.That(state.Authority, Is.EqualTo(MemoryAuthority.WorldState));
      Assert.That(state.Content, Is.EqualTo("door_castle=open"));
    }

    [Test]
    public void WorldStateEntry_UpdateValue_WithSufficientAuthority_Succeeds()
    {
      // Arrange
      var state = new WorldStateEntry("door_castle", "open");

      // Act
      var result = state.UpdateValue("closed", MutationSource.GameSystem);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(state.Value, Is.EqualTo("closed"));
      Assert.That(state.ModificationCount, Is.EqualTo(1));
    }

    [Test]
    public void WorldStateEntry_UpdateValue_WithInsufficientAuthority_Fails()
    {
      // Arrange
      var state = new WorldStateEntry("door_castle", "open");

      // Act
      var result = state.UpdateValue("closed", MutationSource.LlmSuggestion);

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.FailureReason, Does.Contain("lacks authority"));
      Assert.That(state.Value, Is.EqualTo("open")); // Unchanged
    }

    [Test]
    public void WorldStateEntry_UpdateValue_ByDesigner_Succeeds()
    {
      // Arrange
      var state = new WorldStateEntry("door_castle", "open");

      // Act
      var result = state.UpdateValue("locked", MutationSource.Designer);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(state.Value, Is.EqualTo("locked"));
    }

    #endregion

    #region EpisodicMemory Tests

    [Test]
    public void EpisodicMemoryEntry_Create_SetsPropertiesCorrectly()
    {
      // Act
      var memory = new EpisodicMemoryEntry("Player said hello", EpisodeType.Dialogue);

      // Assert
      Assert.That(memory.Description, Is.EqualTo("Player said hello"));
      Assert.That(memory.EpisodeType, Is.EqualTo(EpisodeType.Dialogue));
      Assert.That(memory.Authority, Is.EqualTo(MemoryAuthority.Episodic));
      Assert.That(memory.Strength, Is.EqualTo(1.0f));
      Assert.That(memory.IsActive, Is.True);
    }

    [Test]
    public void EpisodicMemoryEntry_ApplyDecay_ReducesStrength()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 1.0f, Significance = 0f };

      // Act
      memory.ApplyDecay(0.2f);

      // Assert
      Assert.That(memory.Strength, Is.LessThan(1.0f));
      Assert.That(memory.Strength, Is.EqualTo(0.8f).Within(0.01f));
    }

    [Test]
    public void EpisodicMemoryEntry_ApplyDecay_SignificantMemoriesDecaySlower()
    {
      // Arrange
      var normalMemory = new EpisodicMemoryEntry("Normal") { Strength = 1.0f, Significance = 0f };
      var significantMemory = new EpisodicMemoryEntry("Significant") { Strength = 1.0f, Significance = 1.0f };

      // Act
      normalMemory.ApplyDecay(0.2f);
      significantMemory.ApplyDecay(0.2f);

      // Assert
      Assert.That(significantMemory.Strength, Is.GreaterThan(normalMemory.Strength));
    }

    [Test]
    public void EpisodicMemoryEntry_Reinforce_IncreasesStrength()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 0.5f };

      // Act
      memory.Reinforce(0.3f);

      // Assert
      Assert.That(memory.Strength, Is.EqualTo(0.8f).Within(0.01f));
    }

    [Test]
    public void EpisodicMemoryEntry_Reinforce_CapsAtOne()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 0.9f };

      // Act
      memory.Reinforce(0.5f);

      // Assert
      Assert.That(memory.Strength, Is.EqualTo(1.0f));
    }

    [Test]
    public void EpisodicMemoryEntry_FromDialogue_CreatesCorrectly()
    {
      // Act
      var memory = EpisodicMemoryEntry.FromDialogue("Player", "Hello there!", 0.7f);

      // Assert
      Assert.That(memory.EpisodeType, Is.EqualTo(EpisodeType.Dialogue));
      Assert.That(memory.Participant, Is.EqualTo("Player"));
      Assert.That(memory.Significance, Is.EqualTo(0.7f));
      Assert.That(memory.Content, Contains.Substring("Player: Hello there!"));
    }

    [Test]
    public void EpisodicMemoryEntry_IsActive_FalseWhenStrengthLow()
    {
      // Arrange
      var memory = new EpisodicMemoryEntry("Test") { Strength = 0.05f };

      // Assert
      Assert.That(memory.IsActive, Is.False);
    }

    #endregion

    #region BeliefMemory Tests

    [Test]
    public void BeliefMemoryEntry_Create_SetsPropertiesCorrectly()
    {
      // Act
      var belief = new BeliefMemoryEntry("Player", "is friendly", BeliefType.Opinion);

      // Assert
      Assert.That(belief.Subject, Is.EqualTo("Player"));
      Assert.That(belief.BeliefContent, Is.EqualTo("is friendly"));
      Assert.That(belief.BeliefType, Is.EqualTo(BeliefType.Opinion));
      Assert.That(belief.Authority, Is.EqualTo(MemoryAuthority.Belief));
      Assert.That(belief.Confidence, Is.EqualTo(0.5f));
    }

    [Test]
    public void BeliefMemoryEntry_Content_VariesByConfidence()
    {
      // Arrange & Act
      var certain = new BeliefMemoryEntry("X", "is true") { Confidence = 0.9f };
      var believe = new BeliefMemoryEntry("X", "is true") { Confidence = 0.6f };
      var think = new BeliefMemoryEntry("X", "is true") { Confidence = 0.4f };
      var unsure = new BeliefMemoryEntry("X", "is true") { Confidence = 0.2f };

      // Assert
      Assert.That(certain.Content, Does.StartWith("I know that"));
      Assert.That(believe.Content, Does.StartWith("I believe that"));
      Assert.That(think.Content, Does.StartWith("I think that"));
      Assert.That(unsure.Content, Does.StartWith("I'm not sure"));
    }

    [Test]
    public void BeliefMemoryEntry_UpdateBelief_WithSufficientAuthority_Succeeds()
    {
      // Arrange
      var belief = new BeliefMemoryEntry("Player", "is friendly");

      // Act
      var result = belief.UpdateBelief("is hostile", 0.8f, "They attacked me", MutationSource.ValidatedOutput);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(belief.BeliefContent, Is.EqualTo("is hostile"));
      Assert.That(belief.Confidence, Is.EqualTo(0.8f));
      Assert.That(belief.Evidence, Is.EqualTo("They attacked me"));
    }

    [Test]
    public void BeliefMemoryEntry_UpdateBelief_WithInsufficientAuthority_Fails()
    {
      // Arrange
      var belief = new BeliefMemoryEntry("Player", "is friendly");

      // Act
      var result = belief.UpdateBelief("is hostile", 0.8f, null, MutationSource.LlmSuggestion);

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(belief.BeliefContent, Is.EqualTo("is friendly")); // Unchanged
    }

    [Test]
    public void BeliefMemoryEntry_MarkContradicted_SetsFlag()
    {
      // Arrange
      var belief = new BeliefMemoryEntry("Player", "is a wizard") { Confidence = 0.8f };

      // Act
      belief.MarkContradicted("Canonical fact states player is a warrior");

      // Assert
      Assert.That(belief.IsContradicted, Is.True);
      Assert.That(belief.Confidence, Is.LessThanOrEqualTo(0.2f));
      Assert.That(belief.Evidence, Does.Contain("[CONTRADICTED]"));
    }

    [Test]
    public void BeliefMemoryEntry_AdjustSentiment_ModifiesValue()
    {
      // Arrange
      var belief = BeliefMemoryEntry.CreateRelationship("Player", "acquaintance", 0f);

      // Act
      belief.AdjustSentiment(0.3f);

      // Assert
      Assert.That(belief.Sentiment, Is.EqualTo(0.3f));
    }

    [Test]
    public void BeliefMemoryEntry_AdjustSentiment_ClampsBetweenMinusOneAndOne()
    {
      // Arrange
      var belief = BeliefMemoryEntry.CreateRelationship("Player", "enemy", -0.8f);

      // Act
      belief.AdjustSentiment(-0.5f);

      // Assert
      Assert.That(belief.Sentiment, Is.EqualTo(-1.0f));
    }

    [Test]
    public void BeliefMemoryEntry_CreateRelationship_SetsCorrectType()
    {
      // Act
      var relationship = BeliefMemoryEntry.CreateRelationship("Player", "trusted ally", 0.8f);

      // Assert
      Assert.That(relationship.BeliefType, Is.EqualTo(BeliefType.Relationship));
      Assert.That(relationship.Sentiment, Is.EqualTo(0.8f));
      Assert.That(relationship.Confidence, Is.EqualTo(0.7f));
    }

    #endregion

    #region MutationResult Tests

    [Test]
    public void MutationResult_Succeeded_CreatesSuccessResult()
    {
      // Arrange
      var entry = new CanonicalFact("Test");

      // Act
      var result = MutationResult.Succeeded(entry);

      // Assert
      Assert.That(result.Success, Is.True);
      Assert.That(result.AffectedEntry, Is.SameAs(entry));
      Assert.That(result.FailureReason, Is.Null);
    }

    [Test]
    public void MutationResult_Failed_CreatesFailureResult()
    {
      // Act
      var result = MutationResult.Failed("Test failure reason");

      // Assert
      Assert.That(result.Success, Is.False);
      Assert.That(result.FailureReason, Is.EqualTo("Test failure reason"));
      Assert.That(result.AffectedEntry, Is.Null);
    }

    #endregion
  }
}
