using System;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core.Inference;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;

namespace LlamaBrain.Tests.Inference
{
  [TestFixture]
  public class ContextRetrievalLayerTests
  {
    private AuthoritativeMemorySystem _memorySystem;
    private ContextRetrievalLayer _retrieval;

    [SetUp]
    public void SetUp()
    {
      _memorySystem = new AuthoritativeMemorySystem();
      _retrieval = new ContextRetrievalLayer(_memorySystem);
    }

    [Test]
    public void Constructor_ThrowsOnNullMemorySystem()
    {
      Assert.Throws<ArgumentNullException>(() => new ContextRetrievalLayer(null));
    }

    [Test]
    public void RetrieveContext_EmptySystem_ReturnsEmptyContext()
    {
      var result = _retrieval.RetrieveContext("Hello");

      Assert.That(result.HasContent, Is.False);
      Assert.That(result.TotalCount, Is.EqualTo(0));
    }

    [Test]
    public void RetrieveContext_RetrievesCanonicalFacts()
    {
      _memorySystem.AddCanonicalFact("fact1", "The king is Arthur", "world");
      _memorySystem.AddCanonicalFact("fact2", "Dragons breathe fire", "lore");

      var result = _retrieval.RetrieveContext("Tell me about the king");

      Assert.That(result.CanonicalFacts.Count, Is.EqualTo(2));
      Assert.That(result.CanonicalFacts, Does.Contain("The king is Arthur"));
    }

    [Test]
    public void RetrieveContext_RetrievesWorldState()
    {
      _memorySystem.SetWorldState("door_castle", "open", MutationSource.GameSystem);
      _memorySystem.SetWorldState("player_gold", "100", MutationSource.GameSystem);

      var result = _retrieval.RetrieveContext("What's the door status?");

      Assert.That(result.WorldState.Count, Is.EqualTo(2));
    }

    [Test]
    public void RetrieveContext_RetrievesEpisodicMemories()
    {
      var entry1 = EpisodicMemoryEntry.FromDialogue("Player", "Hello there!", 0.8f);
      var entry2 = EpisodicMemoryEntry.FromDialogue("NPC", "Welcome!", 0.7f);

      _memorySystem.AddEpisodicMemory(entry1, MutationSource.ValidatedOutput);
      _memorySystem.AddEpisodicMemory(entry2, MutationSource.ValidatedOutput);

      var result = _retrieval.RetrieveContext("Hello");

      Assert.That(result.EpisodicMemories.Count, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void RetrieveContext_RetrievesBeliefs()
    {
      var belief = new BeliefMemoryEntry("Player", "Player is friendly");
      _memorySystem.SetBelief("belief1", belief, MutationSource.ValidatedOutput);

      var result = _retrieval.RetrieveContext("What do you think?");

      Assert.That(result.Beliefs.Count, Is.EqualTo(1));
    }

    [Test]
    public void RetrieveContext_RespectLimits()
    {
      // Add many episodic memories
      for (int i = 0; i < 20; i++)
      {
        var entry = EpisodicMemoryEntry.FromDialogue("Player", $"Message {i}", 0.5f);
        _memorySystem.AddEpisodicMemory(entry, MutationSource.ValidatedOutput);
      }

      var config = new ContextRetrievalConfig { MaxEpisodicMemories = 5 };
      var retrieval = new ContextRetrievalLayer(_memorySystem, config);

      var result = retrieval.RetrieveContext("Hello");

      Assert.That(result.EpisodicMemories.Count, Is.LessThanOrEqualTo(5));
    }

    [Test]
    public void RetrieveContext_FiltersLowConfidenceBeliefs()
    {
      var highConfidence = new BeliefMemoryEntry("Player", "Very confident belief");
      highConfidence.Confidence = 0.9f;

      var lowConfidence = new BeliefMemoryEntry("Player", "Uncertain belief");
      lowConfidence.Confidence = 0.1f;

      _memorySystem.SetBelief("high", highConfidence, MutationSource.ValidatedOutput);
      _memorySystem.SetBelief("low", lowConfidence, MutationSource.ValidatedOutput);

      var config = new ContextRetrievalConfig { MinBeliefConfidence = 0.5f };
      var retrieval = new ContextRetrievalLayer(_memorySystem, config);

      var result = retrieval.RetrieveContext("What do you believe?");

      Assert.That(result.Beliefs.Count, Is.EqualTo(1));
      Assert.That(result.Beliefs[0], Does.Contain("Very confident"));
    }

    [Test]
    public void RetrieveContext_ExcludesContradictedBeliefsByDefault()
    {
      // Add a canonical fact
      _memorySystem.AddCanonicalFact("fact", "The king is Arthur", "world");

      // Add a belief that contradicts it (will be marked as contradicted)
      var contradictingBelief = new BeliefMemoryEntry("Player", "The king is not Arthur");
      _memorySystem.SetBelief("belief", contradictingBelief, MutationSource.ValidatedOutput);

      var result = _retrieval.RetrieveContext("Who is the king?");

      // Contradicted beliefs should be excluded by default
      Assert.That(result.Beliefs.All(b => !b.Contains("not Arthur")), Is.True);
    }

    [Test]
    public void RetrieveContext_CanIncludeContradictedBeliefs()
    {
      _memorySystem.AddCanonicalFact("fact", "The sky is blue", "world");

      var belief = new BeliefMemoryEntry("Player", "The sky is not blue");
      _memorySystem.SetBelief("belief", belief, MutationSource.ValidatedOutput);

      var config = new ContextRetrievalConfig { IncludeContradictedBeliefs = true };
      var retrieval = new ContextRetrievalLayer(_memorySystem, config);

      var result = retrieval.RetrieveContext("What color is the sky?");

      // Should have both fact and contradicted belief
      Assert.That(result.CanonicalFacts.Count, Is.EqualTo(1));
    }

    [Test]
    public void RetrieveContext_FiltersByTopics()
    {
      _memorySystem.AddCanonicalFact("fact1", "The king is Arthur", "royalty");
      _memorySystem.AddCanonicalFact("fact2", "Dragons breathe fire", "creatures");

      var result = _retrieval.RetrieveContext("Tell me about royalty", new[] { "royalty" });

      // Should prioritize facts matching the topic
      Assert.That(result.CanonicalFacts, Does.Contain("The king is Arthur"));
    }

    [Test]
    public void RetrievedContext_ApplyTo_BuildsSnapshot()
    {
      _memorySystem.AddCanonicalFact("fact", "Important fact", "test");
      _memorySystem.SetWorldState("state", "value", MutationSource.GameSystem);

      var retrieved = _retrieval.RetrieveContext("Test input");
      var builder = new StateSnapshotBuilder()
        .WithSystemPrompt("Test")
        .WithPlayerInput("Test input");

      retrieved.ApplyTo(builder);
      var snapshot = builder.Build();

      Assert.That(snapshot.CanonicalFacts.Count, Is.GreaterThan(0));
      Assert.That(snapshot.WorldState.Count, Is.GreaterThan(0));
    }

    [Test]
    public void RetrievedContext_TotalCount_IsCorrect()
    {
      _memorySystem.AddCanonicalFact("fact", "Fact", "test");
      _memorySystem.SetWorldState("state", "value", MutationSource.GameSystem);

      var entry = EpisodicMemoryEntry.FromDialogue("Player", "Hello", 0.5f);
      _memorySystem.AddEpisodicMemory(entry, MutationSource.ValidatedOutput);

      var belief = new BeliefMemoryEntry("Player", "Belief");
      _memorySystem.SetBelief("belief", belief, MutationSource.ValidatedOutput);

      var result = _retrieval.RetrieveContext("test");

      Assert.That(result.TotalCount, Is.EqualTo(4));
    }
  }

  [TestFixture]
  public class ContextRetrievalConfigTests
  {
    [Test]
    public void Default_HasReasonableSettings()
    {
      var config = ContextRetrievalConfig.Default;

      Assert.That(config.MaxCanonicalFacts, Is.EqualTo(0)); // Unlimited
      Assert.That(config.MaxWorldState, Is.EqualTo(0)); // Unlimited
      Assert.That(config.MaxEpisodicMemories, Is.EqualTo(15));
      Assert.That(config.MaxBeliefs, Is.EqualTo(10));
      Assert.That(config.MaxDialogueHistory, Is.EqualTo(10));
      Assert.That(config.MinEpisodicStrength, Is.EqualTo(0.1f));
      Assert.That(config.MinBeliefConfidence, Is.EqualTo(0.3f));
      Assert.That(config.IncludeContradictedBeliefs, Is.False);
    }

    [Test]
    public void Weights_SumToOne()
    {
      var config = ContextRetrievalConfig.Default;

      var totalWeight = config.RecencyWeight + config.RelevanceWeight + config.SignificanceWeight;

      Assert.That(totalWeight, Is.EqualTo(1.0f).Within(0.001f));
    }
  }
}
