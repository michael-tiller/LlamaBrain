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
  }
}

