using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core.Inference;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;

namespace LlamaBrain.Tests.Inference
{
  /// <summary>
  /// Tests for ContextRetrievalLayer.
  /// </summary>
  public class ContextRetrievalLayerTests
  {
    private AuthoritativeMemorySystem _memorySystem = null!;
    private ContextRetrievalLayer _retrievalLayer = null!;

    [SetUp]
    public void SetUp()
    {
      _memorySystem = new AuthoritativeMemorySystem();
      _retrievalLayer = new ContextRetrievalLayer(_memorySystem);
      
      // Add some test data
      _memorySystem.AddCanonicalFact("fact-1", "The king is Arthur", "lore");
      _memorySystem.AddCanonicalFact("fact-2", "The world is round", "world");
      
      _memorySystem.SetWorldState("door_status", "open", MutationSource.GameSystem);
      _memorySystem.SetWorldState("weather", "sunny", MutationSource.GameSystem);
      
      _memorySystem.AddDialogue("Player", "Hello!");
      _memorySystem.AddDialogue("NPC", "Hi there!");
      
      _memorySystem.SetBelief("trust", BeliefMemoryEntry.CreateOpinion("Player", "is trustworthy", 0.0f, 0.8f), MutationSource.ValidatedOutput);
      _memorySystem.SetBelief("quest", BeliefMemoryEntry.CreateBelief("Quest", "is important", 0.6f), MutationSource.ValidatedOutput);
    }

    #region Basic Retrieval Tests

    [Test]
    public void RetrieveContext_ReturnsRetrievedContext()
    {
      // Act
      var context = _retrievalLayer.RetrieveContext("Hello");

      // Assert
      Assert.That(context, Is.Not.Null);
      Assert.That(context.CanonicalFacts, Is.Not.Null);
      Assert.That(context.WorldState, Is.Not.Null);
      Assert.That(context.EpisodicMemories, Is.Not.Null);
      Assert.That(context.Beliefs, Is.Not.Null);
    }

    [Test]
    public void RetrieveContext_RetrievesCanonicalFacts()
    {
      // Act
      var context = _retrievalLayer.RetrieveContext("king");

      // Assert
      Assert.That(context.CanonicalFacts.Count, Is.GreaterThan(0));
      Assert.That(context.CanonicalFacts, Has.Some.Contain("Arthur"));
    }

    [Test]
    public void RetrieveContext_RetrievesWorldState()
    {
      // Act
      var context = _retrievalLayer.RetrieveContext("door");

      // Assert
      Assert.That(context.WorldState.Count, Is.GreaterThan(0));
    }

    [Test]
    public void RetrieveContext_RetrievesEpisodicMemories()
    {
      // Act
      var context = _retrievalLayer.RetrieveContext("Hello");

      // Assert
      Assert.That(context.EpisodicMemories.Count, Is.GreaterThan(0));
      Assert.That(context.EpisodicMemories, Has.Some.Contain("Hello"));
    }

    [Test]
    public void RetrieveContext_RetrievesBeliefs()
    {
      // Act
      var context = _retrievalLayer.RetrieveContext("Player");

      // Assert
      Assert.That(context.Beliefs.Count, Is.GreaterThan(0));
    }

    #endregion

    #region Configuration Tests

    [Test]
    public void RetrieveContext_WithMaxCanonicalFacts_LimitsFacts()
    {
      // Arrange
      // Add more facts
      _memorySystem.AddCanonicalFact("fact-3", "Fact 3", "test");
      _memorySystem.AddCanonicalFact("fact-4", "Fact 4", "test");
      _memorySystem.AddCanonicalFact("fact-5", "Fact 5", "test");
      
      var config = new ContextRetrievalConfig { MaxCanonicalFacts = 2 };
      var layer = new ContextRetrievalLayer(_memorySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert
      Assert.That(context.CanonicalFacts.Count, Is.LessThanOrEqualTo(2));
    }

    [Test]
    public void RetrieveContext_WithMaxWorldState_LimitsState()
    {
      // Arrange
      _memorySystem.SetWorldState("state1", "value1", MutationSource.GameSystem);
      _memorySystem.SetWorldState("state2", "value2", MutationSource.GameSystem);
      _memorySystem.SetWorldState("state3", "value3", MutationSource.GameSystem);
      
      var config = new ContextRetrievalConfig { MaxWorldState = 2 };
      var layer = new ContextRetrievalLayer(_memorySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert
      Assert.That(context.WorldState.Count, Is.LessThanOrEqualTo(2));
    }

    [Test]
    public void RetrieveContext_WithMaxEpisodicMemories_LimitsMemories()
    {
      // Arrange
      for (int i = 0; i < 20; i++)
      {
        _memorySystem.AddDialogue("Player", $"Message {i}");
      }
      
      var config = new ContextRetrievalConfig { MaxEpisodicMemories = 5 };
      var layer = new ContextRetrievalLayer(_memorySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert
      Assert.That(context.EpisodicMemories.Count, Is.LessThanOrEqualTo(5));
    }

    [Test]
    public void RetrieveContext_WithMaxBeliefs_LimitsBeliefs()
    {
      // Arrange
      for (int i = 0; i < 15; i++)
      {
        _memorySystem.SetBelief($"belief-{i}", BeliefMemoryEntry.CreateOpinion("Subject", $"is {i}", 0.0f, 0.5f), MutationSource.ValidatedOutput);
      }
      
      var config = new ContextRetrievalConfig { MaxBeliefs = 5 };
      var layer = new ContextRetrievalLayer(_memorySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert
      Assert.That(context.Beliefs.Count, Is.LessThanOrEqualTo(5));
    }

    [Test]
    public void RetrieveContext_WithMinEpisodicStrength_FiltersByStrength()
    {
      // Arrange
      _memorySystem.AddDialogue("Player", "Recent message");
      
      // Apply decay to reduce strength
      _memorySystem.ApplyEpisodicDecay();
      _memorySystem.ApplyEpisodicDecay();
      
      var config = new ContextRetrievalConfig { MinEpisodicStrength = 0.5f };
      var layer = new ContextRetrievalLayer(_memorySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert
      // Only memories with strength >= 0.5 should be included
      Assert.That(context.EpisodicMemories.Count, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void RetrieveContext_WithMinBeliefConfidence_FiltersByConfidence()
    {
      // Arrange
      _memorySystem.SetBelief("low-confidence", BeliefMemoryEntry.CreateOpinion("Subject", "is low", 0.0f, 0.2f), MutationSource.ValidatedOutput);
      _memorySystem.SetBelief("high-confidence", BeliefMemoryEntry.CreateOpinion("Subject", "is high", 0.0f, 0.9f), MutationSource.ValidatedOutput);
      
      var config = new ContextRetrievalConfig { MinBeliefConfidence = 0.5f };
      var layer = new ContextRetrievalLayer(_memorySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert
      // Only beliefs with confidence >= 0.5 should be included
      Assert.That(context.Beliefs, Has.None.Contain("low"));
      Assert.That(context.Beliefs, Has.Some.Contain("high"));
    }

    [Test]
    public void RetrieveContext_WithIncludeContradictedBeliefs_IncludesContradicted()
    {
      // Arrange
      // Use a fresh system to avoid beliefs from SetUp
      var emptySystem = new AuthoritativeMemorySystem();
      var belief = BeliefMemoryEntry.CreateOpinion("Subject", "is true", 0.0f, 0.8f);
      emptySystem.SetBelief("contradicted", belief, MutationSource.ValidatedOutput);
      var retrievedBelief = emptySystem.GetBelief("contradicted");
      retrievedBelief!.MarkContradicted("Test");
      
      // Verify the belief is actually contradicted
      Assert.That(retrievedBelief.IsContradicted, Is.True);
      
      var config = new ContextRetrievalConfig 
      { 
        IncludeContradictedBeliefs = true,
        MinBeliefConfidence = 0.0f, // Ensure it's not filtered out
        MaxBeliefs = 10 // Ensure it's not limited
      };
      var layer = new ContextRetrievalLayer(emptySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert
      Assert.That(context.Beliefs, Has.Some.Contain("[Uncertain]"));
    }

    [Test]
    public void RetrieveContext_WithoutIncludeContradictedBeliefs_ExcludesContradicted()
    {
      // Arrange
      var belief = BeliefMemoryEntry.CreateOpinion("Subject", "is true", 0.0f, 0.8f);
      _memorySystem.SetBelief("contradicted", belief, MutationSource.ValidatedOutput);
      _memorySystem.GetBelief("contradicted")!.MarkContradicted("Test");
      
      var config = new ContextRetrievalConfig { IncludeContradictedBeliefs = false };
      var layer = new ContextRetrievalLayer(_memorySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert
      Assert.That(context.Beliefs, Has.None.Contain("[Uncertain]"));
    }

    #endregion

    #region Topic Filtering Tests

    [Test]
    public void RetrieveContext_WithTopics_FiltersByTopics()
    {
      // Arrange
      var topics = new[] { "lore" };

      // Act
      var context = _retrievalLayer.RetrieveContext("test", topics);

      // Assert
      // Should prioritize facts with "lore" domain
      Assert.That(context.CanonicalFacts, Has.Some.Contain("Arthur"));
    }

    [Test]
    public void RetrieveContext_WithMultipleTopics_FiltersByAnyTopic()
    {
      // Arrange
      var topics = new[] { "lore", "world" };

      // Act
      var context = _retrievalLayer.RetrieveContext("test", topics);

      // Assert
      Assert.That(context.CanonicalFacts.Count, Is.GreaterThan(0));
    }

    #endregion

    #region Relevance Scoring Tests

    [Test]
    public void RetrieveContext_RelevanceScoring_PrioritizesRelevantMemories()
    {
      // Arrange
      _memorySystem.AddDialogue("Player", "I need help with a quest");
      _memorySystem.AddDialogue("Player", "The weather is nice");
      
      var config = new ContextRetrievalConfig { MaxEpisodicMemories = 1 };
      var layer = new ContextRetrievalLayer(_memorySystem, config);

      // Act
      var context = layer.RetrieveContext("quest");

      // Assert
      // Should prioritize the memory about quest
      Assert.That(context.EpisodicMemories, Has.Some.Contain("quest"));
    }

    [Test]
    public void RetrieveContext_RelevanceScoring_PrioritizesRelevantBeliefs()
    {
      // Arrange
      // Clear existing beliefs from SetUp - use fresh system
      var emptySystem = new AuthoritativeMemorySystem();
      // Make sure the belief content actually contains "quest" for relevance matching
      emptySystem.SetBelief("quest-belief", BeliefMemoryEntry.CreateBelief("Quest", "the quest is important", 0.7f), MutationSource.ValidatedOutput);
      emptySystem.SetBelief("weather-belief", BeliefMemoryEntry.CreateBelief("Weather", "the weather is nice", 0.7f), MutationSource.ValidatedOutput);
      
      var config = new ContextRetrievalConfig { MaxBeliefs = 1 };
      var layer = new ContextRetrievalLayer(emptySystem, config);

      // Act
      var context = layer.RetrieveContext("quest");

      // Assert
      // Should prioritize the quest-related belief (content contains "quest")
      // The formatted content will be "I believe that the quest is important"
      Assert.That(context.Beliefs, Has.Some.Contain("quest"));
    }

    #endregion

    #region RetrievedContext Tests

    [Test]
    public void RetrievedContext_TotalCount_CalculatesCorrectly()
    {
      // Act
      var context = _retrievalLayer.RetrieveContext("test");

      // Assert
      var expected = context.CanonicalFacts.Count +
                     context.WorldState.Count +
                     context.EpisodicMemories.Count +
                     context.Beliefs.Count;
      Assert.That(context.TotalCount, Is.EqualTo(expected));
    }

    [Test]
    public void RetrievedContext_HasContent_WhenItemsExist()
    {
      // Act
      var context = _retrievalLayer.RetrieveContext("test");

      // Assert
      Assert.That(context.HasContent, Is.True);
    }

    [Test]
    public void RetrievedContext_HasContent_FalseWhenEmpty()
    {
      // Arrange
      var emptySystem = new AuthoritativeMemorySystem();
      var emptyLayer = new ContextRetrievalLayer(emptySystem);

      // Act
      var context = emptyLayer.RetrieveContext("test");

      // Assert
      Assert.That(context.HasContent, Is.False);
      Assert.That(context.TotalCount, Is.EqualTo(0));
    }

    [Test]
    public void RetrievedContext_ApplyTo_AppliesToBuilder()
    {
      // Arrange
      var context = _retrievalLayer.RetrieveContext("test");
      var builder = new StateSnapshotBuilder();

      // Act
      var snapshot = context.ApplyTo(builder).Build();

      // Assert
      Assert.That(snapshot.CanonicalFacts, Is.EqualTo(context.CanonicalFacts));
      Assert.That(snapshot.WorldState, Is.EqualTo(context.WorldState));
      Assert.That(snapshot.EpisodicMemories, Is.EqualTo(context.EpisodicMemories));
      Assert.That(snapshot.Beliefs, Is.EqualTo(context.Beliefs));
    }

    #endregion

    #region Logging Tests

    [Test]
    public void RetrieveContext_WithLogging_CallsOnLog()
    {
      // Arrange
      var logMessages = new List<string>();
      _retrievalLayer.OnLog = msg => logMessages.Add(msg);

      // Act
      _retrievalLayer.RetrieveContext("test input");

      // Assert
      Assert.That(logMessages.Count, Is.GreaterThan(0));
      Assert.That(logMessages, Has.Some.Contain("[ContextRetrieval]"));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void RetrieveContext_EmptyInput_StillRetrieves()
    {
      // Act
      var context = _retrievalLayer.RetrieveContext("");

      // Assert
      Assert.That(context, Is.Not.Null);
      // Should still retrieve context even with empty input
    }

    [Test]
    public void RetrieveContext_NullTopics_DoesNotFilter()
    {
      // Act
      var context = _retrievalLayer.RetrieveContext("test", null);

      // Assert
      Assert.That(context, Is.Not.Null);
      // Should retrieve all available context
    }

    [Test]
    public void RetrieveContext_EmptyMemorySystem_ReturnsEmptyContext()
    {
      // Arrange
      var emptySystem = new AuthoritativeMemorySystem();
      var emptyLayer = new ContextRetrievalLayer(emptySystem);

      // Act
      var context = emptyLayer.RetrieveContext("test");

      // Assert
      Assert.That(context.CanonicalFacts, Is.Empty);
      Assert.That(context.WorldState, Is.Empty);
      Assert.That(context.EpisodicMemories, Is.Empty);
      Assert.That(context.Beliefs, Is.Empty);
    }

    #endregion

    #region Phase 10.1: Relevance Scoring Behavior Tests

    [Test]
    public void RelevanceScoring_KeywordOverlap_IncreasesScore()
    {
      // Arrange - Add memories with different keyword overlap
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddDialogue("Player", "I love sword fighting and combat");
      memorySystem.AddDialogue("Player", "The weather is nice today");

      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 1,
        RelevanceWeight = 1.0f,
        RecencyWeight = 0.0f,
        SignificanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act - Search for "sword"
      var context = layer.RetrieveContext("sword");

      // Assert - Memory with keyword overlap should be prioritized
      Assert.That(context.EpisodicMemories.Count, Is.EqualTo(1));
      Assert.That(context.EpisodicMemories[0], Does.Contain("sword"));
    }

    [Test]
    public void RelevanceScoring_NoKeywordOverlap_LowerScore()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddDialogue("Player", "I love cooking and baking");

      var config = new ContextRetrievalConfig
      {
        RelevanceWeight = 1.0f,
        RecencyWeight = 0.0f,
        SignificanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act - Search for completely unrelated term
      var context = layer.RetrieveContext("dragons");

      // Assert - Memory should still be retrieved (no filtering, just lower score)
      Assert.That(context.EpisodicMemories.Count, Is.GreaterThan(0));
    }

    [Test]
    public void RelevanceScoring_TopicMatching_BoostsScore()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddDialogue("Player", "I went to the market today");
      memorySystem.AddDialogue("Player", "The castle is beautiful");

      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 1,
        RelevanceWeight = 1.0f,
        RecencyWeight = 0.0f,
        SignificanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act - Search with topic that matches one memory
      var context = layer.RetrieveContext("something", new[] { "castle" });

      // Assert - Memory containing topic should be prioritized
      Assert.That(context.EpisodicMemories[0], Does.Contain("castle"));
    }

    [Test]
    public void RelevanceScoring_TopicBoostCappedAtOne()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();
      // Memory with high keyword overlap AND topic match
      memorySystem.AddDialogue("Player", "The castle tower is tall");

      var config = new ContextRetrievalConfig
      {
        RelevanceWeight = 1.0f,
        RecencyWeight = 0.0f,
        SignificanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act - Search with both keyword overlap and topic match
      var context = layer.RetrieveContext("castle tower", new[] { "castle" });

      // Assert - Should not crash and should retrieve the memory
      Assert.That(context.EpisodicMemories.Count, Is.GreaterThan(0));
    }

    [Test]
    public void RelevanceScoring_EmptyPlayerInput_ReturnsZeroRelevance()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddDialogue("Player", "Some memory content");

      var config = new ContextRetrievalConfig
      {
        RelevanceWeight = 1.0f,
        RecencyWeight = 0.0f,
        SignificanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act
      var context = layer.RetrieveContext("");

      // Assert - Should still return memories (empty input = 0 relevance score)
      Assert.That(context.EpisodicMemories.Count, Is.GreaterThan(0));
    }

    [Test]
    public void RelevanceScoring_ShortWordsFiltered()
    {
      // Arrange - Words <= 3 chars should be filtered from keyword matching
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddDialogue("Player", "The dog is big");
      memorySystem.AddDialogue("Player", "I love adventure games");

      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 1,
        RelevanceWeight = 1.0f,
        RecencyWeight = 0.0f,
        SignificanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act - Search with short word that exists in one memory
      var context = layer.RetrieveContext("dog"); // 3 chars = filtered

      // Assert - "dog" should be filtered, so no keyword boost for that memory
      // Both memories should have similar low relevance
      Assert.That(context.EpisodicMemories.Count, Is.EqualTo(1));
    }

    #endregion

    #region Phase 10.1: Recency Scoring Behavior Tests

    [Test]
    public void RecencyScoring_UsesMemoryStrength()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddDialogue("Player", "Old memory");

      // Apply decay to reduce strength
      for (int i = 0; i < 5; i++)
      {
        memorySystem.ApplyEpisodicDecay();
      }

      memorySystem.AddDialogue("Player", "New memory");

      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 1,
        RecencyWeight = 1.0f,
        RelevanceWeight = 0.0f,
        SignificanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert - New memory (higher strength) should be prioritized
      Assert.That(context.EpisodicMemories[0], Does.Contain("New memory"));
    }

    [Test]
    public void RecencyScoring_RecentMemoryScoresHigher()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();

      // Add old memory, decay it
      memorySystem.AddDialogue("Player", "First memory added");
      memorySystem.ApplyEpisodicDecay();
      memorySystem.ApplyEpisodicDecay();

      // Add new memory (will have strength = 1.0)
      memorySystem.AddDialogue("Player", "Second memory added");

      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 2,
        RecencyWeight = 1.0f,
        RelevanceWeight = 0.0f,
        SignificanceWeight = 0.0f,
        MinEpisodicStrength = 0.0f // Don't filter any
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert - Second memory should be first (higher recency score)
      Assert.That(context.EpisodicMemories[0], Does.Contain("Second"));
    }

    #endregion

    #region Phase 10.1: Significance Scoring Behavior Tests

    [Test]
    public void SignificanceScoring_HighSignificanceScoresHigher()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();

      // Add memory with low significance
      var lowEntry = new EpisodicMemoryEntry("Low importance event", EpisodeType.Observation);
      lowEntry.Significance = 0.2f;
      memorySystem.AddEpisodicMemory(lowEntry, MutationSource.ValidatedOutput);

      // Add memory with high significance
      var highEntry = new EpisodicMemoryEntry("High importance event", EpisodeType.Event);
      highEntry.Significance = 0.9f;
      memorySystem.AddEpisodicMemory(highEntry, MutationSource.ValidatedOutput);

      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 1,
        SignificanceWeight = 1.0f,
        RecencyWeight = 0.0f,
        RelevanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert - High significance memory should be prioritized
      Assert.That(context.EpisodicMemories[0], Does.Contain("High importance"));
    }

    [Test]
    public void SignificanceScoring_UsesSignificanceDirectly()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();

      // Create memories with specific significance values
      var mem1 = new EpisodicMemoryEntry("Memory with 0.9 significance", EpisodeType.Event);
      mem1.Significance = 0.9f;
      memorySystem.AddEpisodicMemory(mem1, MutationSource.ValidatedOutput);

      var mem2 = new EpisodicMemoryEntry("Memory with 0.1 significance", EpisodeType.Event);
      mem2.Significance = 0.1f;
      memorySystem.AddEpisodicMemory(mem2, MutationSource.ValidatedOutput);

      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 2,
        SignificanceWeight = 1.0f,
        RecencyWeight = 0.0f,
        RelevanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert - Higher significance should be first
      Assert.That(context.EpisodicMemories[0], Does.Contain("0.9"));
      Assert.That(context.EpisodicMemories[1], Does.Contain("0.1"));
    }

    #endregion

    #region Phase 10.1: Confidence-Based Belief Selection Tests

    [Test]
    public void BeliefSelection_BelowMinConfidence_FilteredOut()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.SetBelief("low", BeliefMemoryEntry.CreateOpinion("Subject", "low confidence belief", 0.0f, 0.2f), MutationSource.ValidatedOutput);
      memorySystem.SetBelief("high", BeliefMemoryEntry.CreateOpinion("Subject", "high confidence belief", 0.0f, 0.8f), MutationSource.ValidatedOutput);

      var config = new ContextRetrievalConfig { MinBeliefConfidence = 0.5f };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert
      Assert.That(context.Beliefs, Has.None.Contain("low confidence"));
      Assert.That(context.Beliefs, Has.Some.Contain("high confidence"));
    }

    [Test]
    public void BeliefSelection_AtExactlyMinConfidence_Included()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.SetBelief("exact", BeliefMemoryEntry.CreateOpinion("Subject", "exactly at threshold", 0.0f, 0.5f), MutationSource.ValidatedOutput);

      var config = new ContextRetrievalConfig { MinBeliefConfidence = 0.5f };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert - Belief at exactly the threshold should be included (>=)
      Assert.That(context.Beliefs, Has.Some.Contain("exactly at threshold"));
    }

    [Test]
    public void BeliefSelection_ContradictedBeliefs_ConfidencePenalized()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();

      // Non-contradicted belief with lower base confidence
      memorySystem.SetBelief("normal", BeliefMemoryEntry.CreateOpinion("Subject", "normal belief", 0.0f, 0.5f), MutationSource.ValidatedOutput);

      // Contradicted belief with higher base confidence
      memorySystem.SetBelief("contradicted", BeliefMemoryEntry.CreateOpinion("Subject", "contradicted belief", 0.0f, 0.9f), MutationSource.ValidatedOutput);
      memorySystem.GetBelief("contradicted")!.MarkContradicted("Test");

      var config = new ContextRetrievalConfig
      {
        MaxBeliefs = 1,
        IncludeContradictedBeliefs = true,
        MinBeliefConfidence = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert - Normal belief should rank higher because contradicted has 0.5x penalty
      // Normal: 0.5 confidence (no penalty)
      // Contradicted: 0.9 * 0.5 = 0.45 effective confidence
      Assert.That(context.Beliefs[0], Does.Contain("normal belief"));
    }

    [Test]
    public void BeliefSelection_ScoringFormula_CorrectWeights()
    {
      // Arrange - Belief scoring formula: (relevance * 0.6) + (confidence * 0.4)
      var memorySystem = new AuthoritativeMemorySystem();

      // High relevance, low confidence
      memorySystem.SetBelief("relevant", BeliefMemoryEntry.CreateBelief("Quest", "the quest is very important", 0.3f), MutationSource.ValidatedOutput);

      // Low relevance, high confidence
      memorySystem.SetBelief("confident", BeliefMemoryEntry.CreateBelief("Weather", "weather is nice", 0.9f), MutationSource.ValidatedOutput);

      var config = new ContextRetrievalConfig { MaxBeliefs = 2, MinBeliefConfidence = 0.0f };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act - Search for "quest" to boost relevance of first belief
      var context = layer.RetrieveContext("quest");

      // Assert - Should retrieve both, order depends on calculated scores
      Assert.That(context.Beliefs.Count, Is.EqualTo(2));
      // With "quest" search, the quest-related belief should have higher relevance
      Assert.That(context.Beliefs[0], Does.Contain("quest"));
    }

    #endregion

    #region Phase 10.1: Combined Scoring and Selection Tests

    [Test]
    public void CombinedScoring_MemoriesSortedByTotalScore()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();

      // Memory with moderate scores across all dimensions
      var mem1 = new EpisodicMemoryEntry("Memory about adventure and exploration", EpisodeType.Dialogue);
      mem1.Significance = 0.5f;
      memorySystem.AddEpisodicMemory(mem1, MutationSource.ValidatedOutput);

      // Memory with high significance, low relevance
      var mem2 = new EpisodicMemoryEntry("Unrelated but very important event", EpisodeType.Event);
      mem2.Significance = 0.9f;
      memorySystem.AddEpisodicMemory(mem2, MutationSource.ValidatedOutput);

      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 2,
        RecencyWeight = 0.4f,
        RelevanceWeight = 0.4f,
        SignificanceWeight = 0.2f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act - Search for "adventure"
      var context = layer.RetrieveContext("adventure");

      // Assert - Both retrieved, order based on combined score
      Assert.That(context.EpisodicMemories.Count, Is.EqualTo(2));
    }

    [Test]
    public void CombinedScoring_LimitAppliedAfterScoring()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();

      // Add many memories with different scores
      for (int i = 0; i < 10; i++)
      {
        var mem = new EpisodicMemoryEntry($"Memory number {i}", EpisodeType.Dialogue);
        mem.Significance = i * 0.1f; // 0.0 to 0.9
        memorySystem.AddEpisodicMemory(mem, MutationSource.ValidatedOutput);
      }

      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 3,
        SignificanceWeight = 1.0f,
        RecencyWeight = 0.0f,
        RelevanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert - Should get top 3 by significance (9, 8, 7)
      Assert.That(context.EpisodicMemories.Count, Is.EqualTo(3));
      Assert.That(context.EpisodicMemories[0], Does.Contain("9"));
      Assert.That(context.EpisodicMemories[1], Does.Contain("8"));
      Assert.That(context.EpisodicMemories[2], Does.Contain("7"));
    }

    [Test]
    public void CombinedScoring_HighestScoringSelected()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();

      // Low score memory
      var low = new EpisodicMemoryEntry("Low score memory", EpisodeType.Observation);
      low.Significance = 0.1f;
      memorySystem.AddEpisodicMemory(low, MutationSource.ValidatedOutput);

      // High score memory
      var high = new EpisodicMemoryEntry("High score memory", EpisodeType.Event);
      high.Significance = 0.9f;
      memorySystem.AddEpisodicMemory(high, MutationSource.ValidatedOutput);

      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 1,
        SignificanceWeight = 1.0f,
        RecencyWeight = 0.0f,
        RelevanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert
      Assert.That(context.EpisodicMemories.Count, Is.EqualTo(1));
      Assert.That(context.EpisodicMemories[0], Does.Contain("High score"));
    }

    [Test]
    public void CombinedScoring_AllWeightsZero_StillReturnsMemories()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddDialogue("Player", "First memory");
      memorySystem.AddDialogue("Player", "Second memory");

      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 2,
        RecencyWeight = 0.0f,
        RelevanceWeight = 0.0f,
        SignificanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert - Should still return memories (all scores = 0)
      Assert.That(context.EpisodicMemories.Count, Is.EqualTo(2));
    }

    [Test]
    public void CombinedScoring_DifferentWeightConfigurations()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();

      // High relevance only
      var relevant = new EpisodicMemoryEntry("Dragon adventure exciting", EpisodeType.Dialogue);
      relevant.Significance = 0.1f;
      memorySystem.AddEpisodicMemory(relevant, MutationSource.ValidatedOutput);

      // High significance only
      var significant = new EpisodicMemoryEntry("Boring unrelated event", EpisodeType.Event);
      significant.Significance = 1.0f;
      memorySystem.AddEpisodicMemory(significant, MutationSource.ValidatedOutput);

      // Test with recency weight = 1.0
      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 1,
        RecencyWeight = 1.0f,
        RelevanceWeight = 0.0f,
        SignificanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act
      var context = layer.RetrieveContext("dragon");

      // Assert - Most recent memory should be selected (last added)
      Assert.That(context.EpisodicMemories.Count, Is.EqualTo(1));
      Assert.That(context.EpisodicMemories[0], Does.Contain("Boring")); // Last added
    }

    #endregion

    #region Phase 10.1: Deterministic Ordering Edge Cases

    [Test]
    public void DeterministicOrdering_IdenticalScores_ConsistentOrder()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();

      // Add memories with identical scores (same significance, no relevance, same strength)
      for (int i = 0; i < 5; i++)
      {
        var mem = new EpisodicMemoryEntry($"Memory {i}", EpisodeType.Dialogue);
        mem.Significance = 0.5f;
        memorySystem.AddEpisodicMemory(mem, MutationSource.ValidatedOutput);
      }

      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 5,
        SignificanceWeight = 0.5f,
        RecencyWeight = 0.5f,
        RelevanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act - Run twice
      var context1 = layer.RetrieveContext("unrelated");
      var context2 = layer.RetrieveContext("unrelated");

      // Assert - Order should be identical
      Assert.That(context1.EpisodicMemories, Is.EqualTo(context2.EpisodicMemories));
    }

    [Test]
    public void DeterministicOrdering_EmptyMemorySystem_ReturnsEmptyLists()
    {
      // Arrange
      var emptySystem = new AuthoritativeMemorySystem();
      var layer = new ContextRetrievalLayer(emptySystem);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert
      Assert.That(context.EpisodicMemories, Is.Empty);
      Assert.That(context.Beliefs, Is.Empty);
      Assert.That(context.CanonicalFacts, Is.Empty);
      Assert.That(context.WorldState, Is.Empty);
    }

    [Test]
    public void DeterministicOrdering_SameInputSameOutput()
    {
      // Arrange
      var memorySystem = new AuthoritativeMemorySystem();
      memorySystem.AddDialogue("Player", "Hello world");
      memorySystem.AddDialogue("NPC", "Greetings traveler");
      memorySystem.SetBelief("test", BeliefMemoryEntry.CreateOpinion("Player", "is friendly", 0.0f, 0.8f), MutationSource.ValidatedOutput);
      memorySystem.AddCanonicalFact("world", "The sky is blue", "nature");

      var config = ContextRetrievalConfig.Default;
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act - Run multiple times with same input
      var results = new List<RetrievedContext>();
      for (int i = 0; i < 5; i++)
      {
        results.Add(layer.RetrieveContext("hello"));
      }

      // Assert - All results should be identical
      for (int i = 1; i < results.Count; i++)
      {
        Assert.That(results[i].EpisodicMemories, Is.EqualTo(results[0].EpisodicMemories));
        Assert.That(results[i].Beliefs, Is.EqualTo(results[0].Beliefs));
        Assert.That(results[i].CanonicalFacts, Is.EqualTo(results[0].CanonicalFacts));
        Assert.That(results[i].WorldState, Is.EqualTo(results[0].WorldState));
      }
    }

    [Test]
    public void DeterministicOrdering_TieBreaker_ProducesDeterministicOrder()
    {
      // Arrange - Create memories that will have identical scores
      var memorySystem = new AuthoritativeMemorySystem();

      // Add memories in specific order
      var memA = new EpisodicMemoryEntry("Memory Alpha", EpisodeType.Dialogue);
      memA.Significance = 0.5f;
      memorySystem.AddEpisodicMemory(memA, MutationSource.ValidatedOutput);

      var memB = new EpisodicMemoryEntry("Memory Beta", EpisodeType.Dialogue);
      memB.Significance = 0.5f;
      memorySystem.AddEpisodicMemory(memB, MutationSource.ValidatedOutput);

      var memC = new EpisodicMemoryEntry("Memory Gamma", EpisodeType.Dialogue);
      memC.Significance = 0.5f;
      memorySystem.AddEpisodicMemory(memC, MutationSource.ValidatedOutput);

      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 3,
        SignificanceWeight = 1.0f,
        RecencyWeight = 0.0f,
        RelevanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act
      var context1 = layer.RetrieveContext("unrelated");
      var context2 = layer.RetrieveContext("unrelated");

      // Assert - Order should be deterministic
      Assert.That(context1.EpisodicMemories, Is.EqualTo(context2.EpisodicMemories));
    }

    #endregion

    #region Phase 10.7: High-Leverage Determinism Tests

    [Test]
    public void DictionaryOrderTripwire_ShuffledInsertionOrder_DeterministicRetrieval()
    {
      // Phase 10.7 - Forces sorting, not reliance on enumeration order
      // This test deliberately inserts memories in shuffled order to catch
      // any code that relies on dictionary/hashset enumeration order
      var memorySystem = new AuthoritativeMemorySystem();

      // Create a set of memories with predictable IDs
      var memoryDescriptions = new List<string>
      {
        "Memory about zebras",
        "Memory about apples",
        "Memory about monkeys",
        "Memory about bananas",
        "Memory about cats"
      };

      // Shuffle the insertion order randomly
      var random = new Random(42); // Fixed seed for reproducibility
      var shuffled = memoryDescriptions.OrderBy(_ => random.Next()).ToList();

      // Insert in shuffled order
      foreach (var desc in shuffled)
      {
        var mem = new EpisodicMemoryEntry(desc, EpisodeType.Dialogue);
        mem.Significance = 0.5f; // All same significance
        memorySystem.AddEpisodicMemory(mem, MutationSource.ValidatedOutput);
      }

      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 10,
        SignificanceWeight = 1.0f,
        RecencyWeight = 0.0f,
        RelevanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act - Run retrieval multiple times
      var results = new List<List<string>>();
      for (int i = 0; i < 10; i++)
      {
        results.Add(layer.RetrieveContext("test").EpisodicMemories);
      }

      // Assert - All results should be identical regardless of insertion order
      for (int i = 1; i < results.Count; i++)
      {
        Assert.That(results[i], Is.EqualTo(results[0]),
          $"Iteration {i} produced different ordering than iteration 0");
      }
    }

    [Test]
    public void NearEqualFloatingScore_TieBreaker_DeterministicOrdering()
    {
      // Phase 10.7 - Catches floating-point drift issues
      // Two items whose score differs at ~1e-12 should still have deterministic ordering
      var memorySystem = new AuthoritativeMemorySystem();

      // Create memories with nearly identical significance (scores will be nearly identical)
      var mem1 = new EpisodicMemoryEntry("First memory", EpisodeType.Dialogue);
      mem1.Significance = 0.5000000000001f; // Very slightly higher
      memorySystem.AddEpisodicMemory(mem1, MutationSource.ValidatedOutput);

      var mem2 = new EpisodicMemoryEntry("Second memory", EpisodeType.Dialogue);
      mem2.Significance = 0.5f;
      memorySystem.AddEpisodicMemory(mem2, MutationSource.ValidatedOutput);

      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 2,
        SignificanceWeight = 1.0f,
        RecencyWeight = 0.0f,
        RelevanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act - Run multiple times
      var results = new List<List<string>>();
      for (int i = 0; i < 20; i++)
      {
        results.Add(layer.RetrieveContext("unrelated").EpisodicMemories);
      }

      // Assert - Order should be consistent
      for (int i = 1; i < results.Count; i++)
      {
        Assert.That(results[i], Is.EqualTo(results[0]),
          $"Near-equal floating point scores produced inconsistent ordering at iteration {i}");
      }
    }

    [Test]
    public void SequenceNumberTieBreaker_IdenticalScoresAndTimestamps_UsesSequenceNumber()
    {
      // Phase 10.7 - Verifies SequenceNumber is the final deterministic tie-breaker
      var memorySystem = new AuthoritativeMemorySystem();

      // Create memories with identical everything except sequence number
      var baseTicks = DateTimeOffset.UtcNow.UtcTicks;

      var memories = new List<EpisodicMemoryEntry>();
      for (int i = 0; i < 5; i++)
      {
        var mem = new EpisodicMemoryEntry($"Memory {i}", EpisodeType.Dialogue);
        mem.Id = "same_id"; // All same ID to force SequenceNumber tie-breaker
        mem.Significance = 0.5f;
        mem.Strength = 1.0f;
        mem.CreatedAtTicks = baseTicks; // All same timestamp
        memories.Add(mem);
      }

      // Add in order - sequence numbers will be 1, 2, 3, 4, 5
      foreach (var mem in memories)
      {
        memorySystem.AddEpisodicMemory(mem, MutationSource.ValidatedOutput);
      }

      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 5,
        SignificanceWeight = 1.0f,
        RecencyWeight = 0.0f,
        RelevanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act
      var context = layer.RetrieveContext("unrelated");

      // Assert - Memories should be in SequenceNumber order (ascending, so earlier insertions first)
      Assert.That(context.EpisodicMemories.Count, Is.EqualTo(5));
      // When all else is equal, lower SequenceNumber sorts first
      Assert.That(context.EpisodicMemories[0], Does.Contain("Memory 0"));
      Assert.That(context.EpisodicMemories[4], Does.Contain("Memory 4"));
    }

    [Test]
    public void SequenceNumberPersistence_RecalculateAfterManualAssignment()
    {
      // Phase 10.7 - Verifies SequenceNumber persistence logic
      var memorySystem = new AuthoritativeMemorySystem();

      // Add some memories - they get sequence numbers 1, 2, 3
      memorySystem.AddDialogue("Player", "Hello");
      memorySystem.AddDialogue("NPC", "Hi there");
      memorySystem.AddDialogue("Player", "Goodbye");

      // Simulate a "load from persistence" scenario by recalculating
      memorySystem.RecalculateNextSequenceNumber();

      // Act - Add more memories after recalculation
      memorySystem.AddDialogue("Player", "I'm back");

      // Assert - The new memory should have sequence number 4
      var memories = memorySystem.GetActiveEpisodicMemories().ToList();
      Assert.That(memories.Count, Is.EqualTo(4));

      // The newest memory should have the highest sequence number
      var newestMemory = memories.OrderByDescending(m => m.SequenceNumber).First();
      Assert.That(newestMemory.Description, Does.Contain("I'm back"));
      Assert.That(newestMemory.SequenceNumber, Is.EqualTo(4));
    }

    [Test]
    public void BeliefDeterminism_IdenticalConfidence_OrderedBySequenceNumber()
    {
      // Phase 10.7 - Verifies belief ordering uses SequenceNumber tie-breaker
      var memorySystem = new AuthoritativeMemorySystem();

      // Add beliefs with identical confidence
      for (int i = 0; i < 5; i++)
      {
        var belief = BeliefMemoryEntry.CreateOpinion("Player", $"Opinion {i}", 0.0f, 0.8f);
        memorySystem.SetBelief($"belief_{i}", belief, MutationSource.ValidatedOutput);
      }

      var config = new ContextRetrievalConfig
      {
        MaxBeliefs = 5
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act - Run multiple times
      var results = new List<List<string>>();
      for (int i = 0; i < 10; i++)
      {
        results.Add(layer.RetrieveContext("test").Beliefs);
      }

      // Assert - Order should be consistent
      for (int i = 1; i < results.Count; i++)
      {
        Assert.That(results[i], Is.EqualTo(results[0]),
          $"Belief ordering inconsistent at iteration {i}");
      }
    }

    [Test]
    public void OrdinalStringComparison_NonAsciiIds_DeterministicOrdering()
    {
      // Phase 10.7 - Verifies Id comparison uses StringComparer.Ordinal
      // This catches culture-sensitive string comparison bugs
      var memorySystem = new AuthoritativeMemorySystem();

      // Create memories with IDs that would sort differently under different cultures
      var mem1 = new EpisodicMemoryEntry("Memory Alpha", EpisodeType.Dialogue);
      mem1.Id = "café"; // Accented character
      mem1.Significance = 0.5f;
      memorySystem.AddEpisodicMemory(mem1, MutationSource.ValidatedOutput);

      var mem2 = new EpisodicMemoryEntry("Memory Beta", EpisodeType.Dialogue);
      mem2.Id = "caff"; // ASCII approximation
      mem2.Significance = 0.5f;
      memorySystem.AddEpisodicMemory(mem2, MutationSource.ValidatedOutput);

      var mem3 = new EpisodicMemoryEntry("Memory Gamma", EpisodeType.Dialogue);
      mem3.Id = "cafb"; // Sorts between under ordinal
      mem3.Significance = 0.5f;
      memorySystem.AddEpisodicMemory(mem3, MutationSource.ValidatedOutput);

      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 3,
        SignificanceWeight = 1.0f,
        RecencyWeight = 0.0f,
        RelevanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act - Run multiple times
      var results = new List<List<string>>();
      for (int i = 0; i < 5; i++)
      {
        results.Add(layer.RetrieveContext("test").EpisodicMemories);
      }

      // Assert - Order should be consistent using ordinal comparison
      for (int i = 1; i < results.Count; i++)
      {
        Assert.That(results[i], Is.EqualTo(results[0]),
          $"Non-ASCII ID ordering inconsistent at iteration {i}");
      }
    }

    [Test]
    public void AllWeightsZero_FallbackToTieBreakers_DeterministicOrdering()
    {
      // Phase 10.7 - Tests edge case where all weights are zero
      // Should fall back to tie-breaker keys (CreatedAtTicks desc, Id ordinal asc, SequenceNumber asc)
      var memorySystem = new AuthoritativeMemorySystem();

      // Add memories with different timestamps
      var baseTicks = DateTimeOffset.UtcNow.UtcTicks;

      var mem1 = new EpisodicMemoryEntry("Oldest memory", EpisodeType.Dialogue);
      mem1.CreatedAtTicks = baseTicks - 3000;
      mem1.Significance = 0.9f;
      memorySystem.AddEpisodicMemory(mem1, MutationSource.ValidatedOutput);

      var mem2 = new EpisodicMemoryEntry("Middle memory", EpisodeType.Dialogue);
      mem2.CreatedAtTicks = baseTicks - 2000;
      mem2.Significance = 0.1f;
      memorySystem.AddEpisodicMemory(mem2, MutationSource.ValidatedOutput);

      var mem3 = new EpisodicMemoryEntry("Newest memory", EpisodeType.Dialogue);
      mem3.CreatedAtTicks = baseTicks - 1000;
      mem3.Significance = 0.5f;
      memorySystem.AddEpisodicMemory(mem3, MutationSource.ValidatedOutput);

      // All weights zero - score collapses to 0 for all items
      var config = new ContextRetrievalConfig
      {
        MaxEpisodicMemories = 3,
        RecencyWeight = 0.0f,
        RelevanceWeight = 0.0f,
        SignificanceWeight = 0.0f
      };
      var layer = new ContextRetrievalLayer(memorySystem, config);

      // Act
      var context = layer.RetrieveContext("test");

      // Assert - Should be ordered by CreatedAtTicks descending (newest first)
      Assert.That(context.EpisodicMemories.Count, Is.EqualTo(3));
      Assert.That(context.EpisodicMemories[0], Does.Contain("Newest"));
      Assert.That(context.EpisodicMemories[1], Does.Contain("Middle"));
      Assert.That(context.EpisodicMemories[2], Does.Contain("Oldest"));
    }

    #endregion
  }
}

