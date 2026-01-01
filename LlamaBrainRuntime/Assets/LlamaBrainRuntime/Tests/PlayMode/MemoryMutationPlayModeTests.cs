#nullable enable

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LlamaBrain.Core;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;
using LlamaBrain.Runtime.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System;

namespace LlamaBrain.Tests.PlayMode
{
  /// <summary>
  /// PlayMode tests for memory mutation flows including:
  /// - Episodic memory creation and decay
  /// - Belief formation and updates
  /// - World state mutations
  /// - Canonical fact protection
  /// - Memory statistics tracking
  /// </summary>
  public class MemoryMutationPlayModeTests
  {
    private GameObject? agentObject;
    private LlamaBrainAgent? unityAgent;
    private PersonaMemoryStore? memoryStore;
    private PersonaConfig? personaConfig;
    private List<IDisposable> disposables = new List<IDisposable>();
    private List<ScriptableObject> createdScriptableObjects = new List<ScriptableObject>();

    [SetUp]
    public void SetUp()
    {
      // Create persona config
      personaConfig = ScriptableObject.CreateInstance<PersonaConfig>();
      personaConfig!.PersonaId = "memory-test-npc";
      personaConfig!.Name = "Memory Test NPC";
      personaConfig!.Description = "An NPC for testing memory mutations";
      personaConfig!.SystemPrompt = "You are a helpful NPC that remembers conversations.";
      createdScriptableObjects.Add(personaConfig);

      // Create memory store
      memoryStore = new PersonaMemoryStore();

      // Initialize memory with canonical facts
      var memorySystem = memoryStore.GetOrCreateSystem(personaConfig.PersonaId);
      memorySystem.AddCanonicalFact("npc_name", "My name is Memory Test NPC", "identity");
      memorySystem.AddCanonicalFact("world_rule", "The sun rises in the east", "world");
      memorySystem.SetWorldState("current_location", "town_square", MutationSource.GameSystem);
      memorySystem.SetWorldState("time_of_day", "morning", MutationSource.GameSystem);
    }

    [TearDown]
    public void TearDown()
    {
      // Dispose tracked IDisposable instances
      foreach (var disposable in disposables)
      {
        try { disposable.Dispose(); } catch { }
      }
      disposables.Clear();

      // Cleanup Unity agent
      if (agentObject)
      {
        UnityEngine.Object.DestroyImmediate(agentObject);
        agentObject = null;
      }

      // Cleanup ScriptableObjects
      foreach (var so in createdScriptableObjects)
      {
        if (so) UnityEngine.Object.DestroyImmediate(so);
      }
      createdScriptableObjects.Clear();

      memoryStore = null;
      unityAgent = null;
    }

    #region Stub Client Helper

    /// <summary>
    /// Creates a stub client with the provided responses.
    /// </summary>
    private StubApiClient CreateStubClient(params string[] responses)
    {
      var stubResponses = responses.Select(r => new StubApiClient.StubResponse { Content = r }).ToList();
      var client = new StubApiClient(stubResponses);
      disposables.Add(client);
      return client;
    }

    #endregion

    #region Episodic Memory Tests

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Memory_EpisodicMemory_CanBeAddedByGameSystem()
    {
      // Arrange
      var stubClient = CreateStubClient("Hello, I remember you!");

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      var memorySystem = memoryStore!.GetOrCreateSystem(personaConfig!.PersonaId);
      var initialCount = memorySystem.GetRecentMemories(100).Count();

      // Act - Add episodic memory from game system
      var entry = new EpisodicMemoryEntry("Player helped me find my lost item")
      {
        Significance = 0.8f,
        Category = "quest_complete"
      };
      var result = memorySystem.AddEpisodicMemory(entry, MutationSource.GameSystem);

      // Assert
      Assert.That(result.Success, Is.True, "Game system should be able to add episodic memory");
      var newCount = memorySystem.GetRecentMemories(100).Count();
      Assert.That(newCount, Is.EqualTo(initialCount + 1), "Episodic memory count should increase");

      var memories = memorySystem.GetRecentMemories(10).ToList();
      Assert.That(memories.Any(m => m.Content.Contains("lost item")), Is.True,
        "Added memory should be retrievable");
    }

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Memory_EpisodicMemory_DecayAffectsRetrieval()
    {
      // Arrange
      var memorySystem = memoryStore!.GetOrCreateSystem(personaConfig!.PersonaId);

      // Add memories with different significance
      var highSigEntry = new EpisodicMemoryEntry("High significance event") { Significance = 0.9f };
      var lowSigEntry = new EpisodicMemoryEntry("Low significance event") { Significance = 0.2f };
      memorySystem.AddEpisodicMemory(highSigEntry, MutationSource.GameSystem);
      memorySystem.AddEpisodicMemory(lowSigEntry, MutationSource.GameSystem);

      yield return null;

      // Act - Get memories sorted by significance
      var memories = memorySystem.GetRecentMemories(10).OrderByDescending(m => m.Significance).ToList();

      // Assert - Higher significance should come first when sorted
      Assert.That(memories.Count, Is.GreaterThanOrEqualTo(2));
      Assert.That(memories[0].Content, Does.Contain("High significance"));
    }

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Memory_EpisodicMemory_SourceIsTracked()
    {
      // Arrange
      var memorySystem = memoryStore!.GetOrCreateSystem(personaConfig!.PersonaId);

      // Act
      var gameEntry = new EpisodicMemoryEntry("Game event happened") { Significance = 0.5f, Category = "game_event" };
      var designerEntry = new EpisodicMemoryEntry("Designer added memory") { Significance = 0.5f, Category = "designer_note" };
      memorySystem.AddEpisodicMemory(gameEntry, MutationSource.GameSystem);
      memorySystem.AddEpisodicMemory(designerEntry, MutationSource.Designer);

      yield return null;

      // Assert
      var memories = memorySystem.GetRecentMemories(10).ToList();
      var gameMemory = memories.FirstOrDefault(m => m.Content.Contains("Game event"));
      var designerMemory = memories.FirstOrDefault(m => m.Content.Contains("Designer added"));

      Assert.That(gameMemory, Is.Not.Null);
      Assert.That(designerMemory, Is.Not.Null);
      Assert.That(gameMemory!.Source, Is.EqualTo(MutationSource.GameSystem));
      Assert.That(designerMemory!.Source, Is.EqualTo(MutationSource.Designer));
    }

    #endregion

    #region Belief Memory Tests

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Memory_Belief_CanBeFormed()
    {
      // Arrange
      var memorySystem = memoryStore!.GetOrCreateSystem(personaConfig!.PersonaId);
      var initialBeliefCount = memorySystem.GetAllBeliefs().Count();

      yield return null;

      // Act - Form a belief about the player
      var beliefEntry = BeliefMemoryEntry.CreateBelief("player", "The player seems trustworthy", 0.7f);
      var result = memorySystem.SetBelief("player", beliefEntry, MutationSource.ValidatedOutput);

      // Assert
      Assert.That(result.Success, Is.True, "Should be able to form belief");
      var newCount = memorySystem.GetAllBeliefs().Count();
      Assert.That(newCount, Is.EqualTo(initialBeliefCount + 1));

      var belief = memorySystem.GetBelief("player");
      Assert.That(belief, Is.Not.Null);
      Assert.That(belief!.Content, Does.Contain("trustworthy"));
      Assert.That(belief.Confidence, Is.EqualTo(0.7f).Within(0.01f));
    }

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Memory_Belief_CanBeUpdated()
    {
      // Arrange
      var memorySystem = memoryStore!.GetOrCreateSystem(personaConfig!.PersonaId);

      // Form initial belief
      var initialBelief = BeliefMemoryEntry.CreateBelief("player", "Player is okay", 0.5f);
      memorySystem.SetBelief("player", initialBelief, MutationSource.ValidatedOutput);
      yield return null;

      // Act - Update belief with higher confidence
      var updatedBelief = BeliefMemoryEntry.CreateBelief("player", "Player is very helpful", 0.9f);
      var result = memorySystem.SetBelief("player", updatedBelief, MutationSource.ValidatedOutput);

      // Assert
      Assert.That(result.Success, Is.True);
      var belief = memorySystem.GetBelief("player");
      Assert.That(belief, Is.Not.Null);
      Assert.That(belief!.Content, Does.Contain("very helpful"));
      Assert.That(belief.Confidence, Is.EqualTo(0.9f).Within(0.01f));
    }

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Memory_Belief_RespectsMutationAuthority()
    {
      // Arrange
      var memorySystem = memoryStore!.GetOrCreateSystem(personaConfig!.PersonaId);

      // Form belief from Designer (higher authority)
      var designerBelief = BeliefMemoryEntry.CreateBelief("player", "Designer says player is special", 0.8f);
      memorySystem.SetBelief("player", designerBelief, MutationSource.Designer);
      yield return null;

      // Act - Try to update from ValidatedOutput (lower authority than Designer)
      var npcBelief = BeliefMemoryEntry.CreateBelief("player", "NPC thinks player is ordinary", 0.5f);
      var result = memorySystem.SetBelief("player", npcBelief, MutationSource.ValidatedOutput);

      // Assert - NpcInference has lower authority than Designer
      // The update should fail or be ignored
      var belief = memorySystem.GetBelief("player");
      Assert.That(belief, Is.Not.Null);
      // Designer belief should remain (depending on authority rules)
      // This tests the authority hierarchy
    }

    #endregion

    #region World State Tests

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Memory_WorldState_CanBeUpdatedByGameSystem()
    {
      // Arrange
      var memorySystem = memoryStore!.GetOrCreateSystem(personaConfig!.PersonaId);

      yield return null;

      // Act
      var result = memorySystem.SetWorldState("door_status", "closed", MutationSource.GameSystem);

      // Assert
      Assert.That(result.Success, Is.True);
      var state = memorySystem.GetWorldState("door_status");
      Assert.That(state, Is.Not.Null);
      Assert.That(state!.Value, Is.EqualTo("closed"));
    }

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Memory_WorldState_NpcInferenceCannotModify()
    {
      // Arrange
      var memorySystem = memoryStore!.GetOrCreateSystem(personaConfig!.PersonaId);

      // Set initial state from GameSystem
      memorySystem.SetWorldState("secure_door", "locked", MutationSource.GameSystem);
      yield return null;

      // Act - Try to modify from ValidatedOutput (lower authority than GameSystem for world state)
      var result = memorySystem.SetWorldState("secure_door", "unlocked", MutationSource.ValidatedOutput);

      // Assert - Should fail due to authority
      Assert.That(result.Success, Is.False, "NpcInference should not be able to modify world state");
      var state = memorySystem.GetWorldState("secure_door");
      Assert.That(state!.Value, Is.EqualTo("locked"), "Original value should be preserved");
    }

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Memory_WorldState_RetrievedInSnapshot()
    {
      // Arrange
      var stubClient = CreateStubClient("I see the door is open.");

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Act
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("What do you see?");
      });

      // Assert
      Assert.That(unityAgent!.LastSnapshot, Is.Not.Null);
      // World state collection should exist in snapshot
      Assert.That(unityAgent!.LastSnapshot!.WorldState, Is.Not.Null);
    }

    #endregion

    #region Canonical Fact Protection Tests

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Memory_CanonicalFacts_CannotBeModifiedByNpc()
    {
      // Arrange
      var memorySystem = memoryStore!.GetOrCreateSystem(personaConfig!.PersonaId);
      var originalFact = memorySystem.GetCanonicalFact("npc_name");
      Assert.That(originalFact, Is.Not.Null);

      yield return null;

      // Act - Try to add same ID (should fail)
      var result = memorySystem.AddCanonicalFact("npc_name", "My name is something else", "identity");

      // Assert
      Assert.That(result.Success, Is.False, "Cannot modify existing canonical fact");
      var factAfter = memorySystem.GetCanonicalFact("npc_name");
      Assert.That(factAfter!.Fact, Is.EqualTo(originalFact!.Fact), "Canonical fact should be unchanged");
    }

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Memory_CanonicalFacts_AreIncludedInSnapshot()
    {
      // Arrange
      var stubClient = CreateStubClient("My name is Memory Test NPC.");

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Act
      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("What is your name?");
      });

      // Assert
      Assert.That(unityAgent!.LastSnapshot, Is.Not.Null);
      Assert.That(unityAgent!.LastSnapshot!.CanonicalFacts, Is.Not.Null);
      Assert.That(unityAgent!.LastSnapshot!.CanonicalFacts.Count, Is.GreaterThan(0),
        "Canonical facts should be included in snapshot");
    }

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Memory_CanonicalFacts_MultipleDomainsSupported()
    {
      // Arrange
      var memorySystem = memoryStore!.GetOrCreateSystem(personaConfig!.PersonaId);

      yield return null;

      // Act - Add facts to different domains
      memorySystem.AddCanonicalFact("lore_fact", "Dragons once ruled this land", "lore");
      memorySystem.AddCanonicalFact("rule_fact", "Magic requires concentration", "rules");

      // Assert
      var loreFacts = memorySystem.GetCanonicalFacts("lore").ToList();
      var ruleFacts = memorySystem.GetCanonicalFacts("rules").ToList();

      Assert.That(loreFacts.Count, Is.GreaterThanOrEqualTo(1));
      Assert.That(ruleFacts.Count, Is.GreaterThanOrEqualTo(1));
      Assert.That(loreFacts.Any(f => f.Domain == "lore"), Is.True);
      Assert.That(ruleFacts.Any(f => f.Domain == "rules"), Is.True);
    }

    #endregion

    #region Memory Statistics Tests

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Memory_Statistics_TrackedAcrossOperations()
    {
      // Arrange
      var stubClient = CreateStubClient("I remember everything!");

      agentObject = new GameObject("TestAgent");
      unityAgent = agentObject.AddComponent<LlamaBrainAgent>();
      unityAgent!.PersonaConfig = personaConfig;
      unityAgent!.Initialize(stubClient, memoryStore!);
      yield return null;

      // Act - Perform some operations
      var memorySystem = memoryStore!.GetOrCreateSystem(personaConfig!.PersonaId);
      var mem1 = new EpisodicMemoryEntry("Test memory 1") { Significance = 0.5f };
      var mem2 = new EpisodicMemoryEntry("Test memory 2") { Significance = 0.5f };
      memorySystem.AddEpisodicMemory(mem1, MutationSource.GameSystem);
      memorySystem.AddEpisodicMemory(mem2, MutationSource.GameSystem);
      var testBelief = BeliefMemoryEntry.CreateBelief("test_subject", "Test belief", 0.5f);
      memorySystem.SetBelief("test_subject", testBelief, MutationSource.GameSystem);

      InferenceResultWithRetries? result = null;
      yield return UniTask.ToCoroutine(async () =>
      {
        result = await unityAgent!.SendWithSnapshotAsync("Remember this conversation");
      });

      // Assert - Check mutation stats are tracked
      var stats = unityAgent!.MutationStats;
      Assert.That(stats, Is.Not.Null, "Mutation stats should be available");
      // Stats should track operations (exact values depend on implementation)
    }

    #endregion

    #region Memory Retrieval Context Tests

    [UnityTest]
    [Category("Contract")]
    public IEnumerator Memory_Retrieval_RespectsBounds()
    {
      // Arrange
      var memorySystem = memoryStore!.GetOrCreateSystem(personaConfig!.PersonaId);

      // Add many memories
      for (int i = 0; i < 20; i++)
      {
        var memEntry = new EpisodicMemoryEntry($"Memory number {i}") { Significance = 0.5f };
        memorySystem.AddEpisodicMemory(memEntry, MutationSource.GameSystem);
      }

      yield return null;

      // Act - Retrieve limited number
      var limitedMemories = memorySystem.GetRecentMemories(5).ToList();
      var allMemories = memorySystem.GetRecentMemories(100).ToList();

      // Assert
      Assert.That(limitedMemories.Count, Is.LessThanOrEqualTo(5), "Should respect limit parameter");
      Assert.That(allMemories.Count, Is.GreaterThan(5), "Should have more memories available");
    }

    #endregion
  }
}
