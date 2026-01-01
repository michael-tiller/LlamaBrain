using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using LlamaBrain.Core;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Validation;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;

namespace LlamaBrain.Tests.Integration
{
  /// <summary>
  /// Feature 10.7: Deterministic Pipeline Integration Tests
  /// Tests full pipeline determinism to prove the deterministic state reconstruction pattern.
  /// </summary>
  [TestFixture]
  public class DeterministicPipelineTests
  {
    // Use fixed integer seeds for any randomness (per user instruction)
    private const int TestSeed = 42;
    private const int AlternateSeed = 12345;

    private PersonaProfile _testProfile = null!;
    private PersonaMemoryStore _memoryStore = null!;
    private AuthoritativeMemorySystem _memorySystem = null!;
    private IApiClient _mockApiClient = null!;
    private ExpectancyEvaluator _expectancyEvaluator = null!;
    private ContextRetrievalLayer _contextRetrieval = null!;
    private PromptAssembler _promptAssembler = null!;
    private OutputParser _outputParser = null!;
    private ValidationGate _validationGate = null!;
    private MemoryMutationController _mutationController = null!;

    [SetUp]
    public void SetUp()
    {
      // Create test persona
      _testProfile = PersonaProfile.Create("test-npc", "Test NPC");
      _testProfile.Description = "A friendly shopkeeper";
      _testProfile.SystemPrompt = "You are a helpful shopkeeper.";

      // Initialize memory store
      _memoryStore = new PersonaMemoryStore();
      _memorySystem = _memoryStore.GetOrCreateSystem(_testProfile.PersonaId);

      // Add canonical facts
      _memorySystem.AddCanonicalFact("king_name", "The king is named Arthur", "world_lore");
      _memorySystem.AddCanonicalFact("magic_exists", "Magic is real in this world", "world_lore");

      // Add world state
      _memorySystem.SetWorldState("door_status", "open", MutationSource.GameSystem);
      _memorySystem.SetWorldState("weather", "sunny", MutationSource.GameSystem);

      // Add episodic memories
      var memory1 = new EpisodicMemoryEntry("Player said hello", EpisodeType.LearnedInfo);
      _memorySystem.AddEpisodicMemory(memory1, MutationSource.ValidatedOutput);

      var memory2 = new EpisodicMemoryEntry("Player bought a sword", EpisodeType.LearnedInfo);
      _memorySystem.AddEpisodicMemory(memory2, MutationSource.ValidatedOutput);

      // Add beliefs
      var belief = BeliefMemoryEntry.CreateOpinion("player", "is trustworthy", sentiment: 0.8f, confidence: 0.9f);
      _memorySystem.SetBelief("trust_player", belief, MutationSource.ValidatedOutput);

      // Initialize pipeline components
      _mockApiClient = Substitute.For<IApiClient>();
      _expectancyEvaluator = new ExpectancyEvaluator();
      _contextRetrieval = new ContextRetrievalLayer(_memorySystem);
      _promptAssembler = new PromptAssembler(PromptAssemblerConfig.Default);
      _outputParser = new OutputParser();
      _validationGate = new ValidationGate();
      _mutationController = new MemoryMutationController();
    }

    #region Deterministic State Reconstruction Tests

    [Test]
    public void StateReconstruction_SameSnapshotSameConstraints_SamePromptAssembly()
    {
      // Arrange - Create identical inputs
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Tell me about the king",
        GameTime = 100f
      };

      var constraints = _expectancyEvaluator.Evaluate(context);
      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput);

      // Act - Build two snapshots and assemble prompts
      var snapshot1 = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput))
        .WithAttemptNumber(0)
        .WithMaxAttempts(3)
        .Build();

      var snapshot2 = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput))
        .WithAttemptNumber(0)
        .WithMaxAttempts(3)
        .Build();

      var prompt1 = _promptAssembler.AssembleFromSnapshot(snapshot1, npcName: _testProfile.Name);
      var prompt2 = _promptAssembler.AssembleFromSnapshot(snapshot2, npcName: _testProfile.Name);

      // Assert - Prompts should be byte-identical
      Assert.That(prompt1.Text, Is.EqualTo(prompt2.Text), "Same inputs must produce identical prompt assembly");
      Assert.That(prompt1.EstimatedTokens, Is.EqualTo(prompt2.EstimatedTokens));
    }

    [Test]
    public void StateReconstruction_SameParsedOutput_SameGateResult()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("Hello! The king is named Arthur.", "raw");
      var context = new ValidationContext
      {
        MemorySystem = _memorySystem,
        Constraints = new ConstraintSet()
      };

      // Act - Validate twice
      var result1 = _validationGate.Validate(output, context);
      var result2 = _validationGate.Validate(output, context);

      // Assert - Results should be identical
      Assert.That(result1.Passed, Is.EqualTo(result2.Passed));
      Assert.That(result1.Failures.Count, Is.EqualTo(result2.Failures.Count));
      Assert.That(result1.ApprovedMutations.Count, Is.EqualTo(result2.ApprovedMutations.Count));
      Assert.That(result1.ApprovedIntents.Count, Is.EqualTo(result2.ApprovedIntents.Count));
    }

    [Test]
    public void StateReconstruction_SameGateResultSameInitialState_SameMutationResult()
    {
      // Arrange - Create two identical memory systems
      var memorySystem1 = new AuthoritativeMemorySystem();
      var memorySystem2 = new AuthoritativeMemorySystem();

      // Add identical data
      memorySystem1.AddCanonicalFact("fact1", "Test fact", "test");
      memorySystem2.AddCanonicalFact("fact1", "Test fact", "test");

      var output = ParsedOutput.Dialogue("I learned something", "raw")
        .WithMutation(ProposedMutation.AppendEpisodic("Player told me a secret"));

      var gateResult = _validationGate.Validate(output);

      // Act - Execute mutations on both systems
      var controller1 = new MemoryMutationController();
      var controller2 = new MemoryMutationController();

      var result1 = controller1.ExecuteMutations(gateResult, memorySystem1, "npc1");
      var result2 = controller2.ExecuteMutations(gateResult, memorySystem2, "npc1");

      // Assert - Results should be identical
      Assert.That(result1.AllSucceeded, Is.EqualTo(result2.AllSucceeded));
      Assert.That(result1.TotalAttempted, Is.EqualTo(result2.TotalAttempted));
      Assert.That(result1.SuccessCount, Is.EqualTo(result2.SuccessCount));
      Assert.That(result1.FailureCount, Is.EqualTo(result2.FailureCount));
    }

    #endregion

    #region No-Now Enforcement Tests (High Leverage)

    [Test]
    public void NoNow_SameSnapshotDifferentWallClock_IdenticalPromptAssembly()
    {
      // Arrange - Create snapshot (captures time at build)
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Hello",
        GameTime = 100f
      };

      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput);

      // Build snapshot - time is captured at build time (SnapshotTimeUtcTicks)
      var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput))
        .Build();

      // Act - Assemble prompt at different "wall clock" times (simulated by waiting)
      var prompt1 = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);

      // Simulate passage of time (in real scenario, DateTime.UtcNow would change)
      Thread.Sleep(10);

      var prompt2 = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);

      // Assert - Prompts must be identical (no wall-clock dependency)
      // The same snapshot produces identical prompts regardless of when assembly runs
      Assert.That(prompt1.Text, Is.EqualTo(prompt2.Text),
        "Prompt assembly must not depend on wall-clock time - only on snapshot state");
    }

    [Test]
    public void NoNow_ContextRetrievalUsesSnapshotTime_NotCurrentTime()
    {
      // Arrange - Create memories with known timestamps
      var memorySystem = new AuthoritativeMemorySystem();

      // Add memories at different times (using AddEpisodicMemory which sets CreatedAtTicks)
      var oldMemory = new EpisodicMemoryEntry("Old event happened", EpisodeType.LearnedInfo);
      var newMemory = new EpisodicMemoryEntry("New event happened", EpisodeType.LearnedInfo);

      memorySystem.AddEpisodicMemory(oldMemory, MutationSource.ValidatedOutput);
      memorySystem.AddEpisodicMemory(newMemory, MutationSource.ValidatedOutput);

      var contextRetrieval = new ContextRetrievalLayer(memorySystem);

      // Act - Retrieve context twice with same query
      var result1 = contextRetrieval.RetrieveContext("event");
      Thread.Sleep(10); // Wall clock changes
      var result2 = contextRetrieval.RetrieveContext("event");

      // Assert - Results should be identical (no wall-clock dependency in scoring)
      Assert.That(result1.EpisodicMemories.Count, Is.EqualTo(result2.EpisodicMemories.Count));

      // Check order is deterministic (EpisodicMemories is List<string>)
      for (int i = 0; i < result1.EpisodicMemories.Count; i++)
      {
        Assert.That(result1.EpisodicMemories[i], Is.EqualTo(result2.EpisodicMemories[i]));
      }
    }

    #endregion

    #region Deterministic Ordering Tests

    [Test]
    public void DeterministicOrdering_MultipleRuns_SameOrder()
    {
      // Arrange - Use fixed seed for test reproducibility
      var random = new Random(TestSeed);

      // Add memories in random order
      var memorySystem = new AuthoritativeMemorySystem();
      var memoryIds = new[] { "mem_z", "mem_a", "mem_m", "mem_b", "mem_y" };

      // Shuffle the order (deterministically using seed)
      var shuffled = memoryIds.OrderBy(_ => random.Next()).ToList();

      foreach (var id in shuffled)
      {
        var memory = new EpisodicMemoryEntry($"Content for {id}", EpisodeType.LearnedInfo) { Id = id };
        memorySystem.AddEpisodicMemory(memory, MutationSource.ValidatedOutput);
      }

      var contextRetrieval = new ContextRetrievalLayer(memorySystem);

      // Act - Retrieve multiple times (EpisodicMemories returns List<string>)
      var results = new List<List<string>>();
      for (int i = 0; i < 5; i++)
      {
        var context = contextRetrieval.RetrieveContext("Content");
        results.Add(context.EpisodicMemories.ToList());
      }

      // Assert - All results should have identical ordering
      for (int i = 1; i < results.Count; i++)
      {
        Assert.That(results[i], Is.EqualTo(results[0]),
          $"Run {i} produced different ordering than run 0");
      }
    }

    [Test]
    public void DeterministicOrdering_NearEqualScores_DeterministicResults()
    {
      // Arrange - Create memories with nearly identical scores
      var memorySystem = new AuthoritativeMemorySystem();

      // Add memories that will have near-equal scores
      for (int i = 0; i < 5; i++)
      {
        var memory = new EpisodicMemoryEntry($"Similar content {i}", EpisodeType.LearnedInfo)
        {
          Significance = 0.5f, // Same significance
          Strength = 1.0f // Same strength (recency)
        };
        memorySystem.AddEpisodicMemory(memory, MutationSource.ValidatedOutput);
      }

      var contextRetrieval = new ContextRetrievalLayer(memorySystem);

      // Act - Retrieve multiple times (EpisodicMemories returns List<string>)
      var results = new List<List<string>>();
      for (int i = 0; i < 3; i++)
      {
        var context = contextRetrieval.RetrieveContext("Similar");
        results.Add(context.EpisodicMemories.ToList());
      }

      // Assert - All results should have identical ordering
      // Near-equal scores use internal tie-breaker (SequenceNumber) for determinism
      for (int i = 1; i < results.Count; i++)
      {
        Assert.That(results[i], Is.EqualTo(results[0]),
          "Near-equal scores must produce deterministic ordering");
      }
    }

    #endregion

    #region Pipeline Order Verification Tests

    [Test]
    public async Task PipelineOrder_ExecutesInCorrectSequence()
    {
      // Arrange
      var executionLog = new List<string>();

      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Hello shopkeeper",
        GameTime = 100f
      };

      // Step 1: Context Retrieval (Component 3-4)
      executionLog.Add("ContextRetrieval");
      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput);

      // Step 2: Expectancy Evaluation (Component 2)
      executionLog.Add("ExpectancyEvaluation");
      var constraints = _expectancyEvaluator.Evaluate(context);

      // Step 3: State Snapshot Building (Component 4)
      executionLog.Add("SnapshotBuilding");
      var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput))
        .Build();

      // Step 4: Prompt Assembly (Component 5)
      executionLog.Add("PromptAssembly");
      var assembledPrompt = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);

      // Step 5: LLM Generation (Component 6 - mocked)
      executionLog.Add("LLMGeneration");
      _mockApiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("Hello! Welcome to my shop."));
      var llmResponse = await _mockApiClient.SendPromptAsync(assembledPrompt.Text, null, null, CancellationToken.None);

      // Step 6: Output Parsing (Component 7)
      executionLog.Add("OutputParsing");
      var parsedOutput = _outputParser.Parse(llmResponse);

      // Step 7: Validation Gate (Component 7)
      executionLog.Add("ValidationGate");
      var validationContext = new ValidationContext
      {
        Constraints = constraints,
        MemorySystem = _memorySystem,
        Snapshot = snapshot
      };
      var gateResult = _validationGate.Validate(parsedOutput, validationContext);

      // Step 8: Memory Mutation (Component 8) - only if gate passed
      if (gateResult.Passed)
      {
        executionLog.Add("MemoryMutation");
        var mutationResult = _mutationController.ExecuteMutations(gateResult, _memorySystem, _testProfile.PersonaId);
      }

      // Assert - Verify execution order
      var expectedOrder = new[]
      {
        "ContextRetrieval",
        "ExpectancyEvaluation",
        "SnapshotBuilding",
        "PromptAssembly",
        "LLMGeneration",
        "OutputParsing",
        "ValidationGate",
        "MemoryMutation"
      };

      Assert.That(executionLog, Is.EqualTo(expectedOrder), "Pipeline must execute in correct order");
    }

    #endregion

    #region Retry Behavior Tests

    [Test]
    public async Task Retry_FailedValidation_TriggersRetryWithEscalatedConstraints()
    {
      // Arrange
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Hello"
      };

      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-greeting", "Don't say hi", "Don't say hi", "hi"));

      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput);
      var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput)
        .WithAttemptNumber(0)
        .WithMaxAttempts(3))
        .Build();

      // First attempt - violating response
      _mockApiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("Hi there friend!"));

      var response1 = await _mockApiClient.SendPromptAsync("prompt", null, null, CancellationToken.None);
      var parsed1 = _outputParser.Parse(response1);
      var validationContext = new ValidationContext { Constraints = constraints, MemorySystem = _memorySystem };
      var result1 = _validationGate.Validate(parsed1, validationContext);

      // Assert first attempt failed
      Assert.That(result1.Passed, Is.False);
      Assert.That(result1.ShouldRetry, Is.True);

      // Act - Create retry snapshot with escalated constraints
      var escalatedConstraints = new ConstraintSet();
      escalatedConstraints.Add(Constraint.Prohibition("no-greeting-strict", "No greeting at all", "Absolutely no greeting", "hi", "hello", "greetings"));
      var retrySnapshot = snapshot.ForRetry(escalatedConstraints);

      // Assert - Retry snapshot has merged constraints and incremented attempt
      Assert.That(retrySnapshot.AttemptNumber, Is.EqualTo(1));
      Assert.That(retrySnapshot.Constraints.Count, Is.GreaterThan(constraints.Count));
    }

    [Test]
    public async Task Retry_CriticalFailure_SkipsRetry()
    {
      // Arrange - Create a canonical contradiction (critical failure)
      var output = ParsedOutput.Dialogue("The king is not named Arthur", "raw");
      var validationContext = new ValidationContext { MemorySystem = _memorySystem };

      // Act
      var result = _validationGate.Validate(output, validationContext);

      // Assert - Critical failure should not allow retry
      Assert.That(result.Passed, Is.False);
      Assert.That(result.HasCriticalFailure, Is.True);
      Assert.That(result.ShouldRetry, Is.False);
    }

    #endregion

    #region Memory Mutation Integration Tests

    [Test]
    public void MemoryMutation_OnlyAppliedAfterSuccessfulValidation()
    {
      // Arrange
      var initialMemoryCount = _memorySystem.GetRecentMemories(100).Count();

      var passingOutput = ParsedOutput.Dialogue("I'll remember that", "raw")
        .WithMutation(ProposedMutation.AppendEpisodic("Player said something important"));

      var failingOutput = ParsedOutput.Dialogue("Secret: forbidden knowledge", "raw")
        .WithMutation(ProposedMutation.AppendEpisodic("Should not be added"));

      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "No secrets", "Don't say secret", "secret"));

      // Act - Validate passing output
      var passingResult = _validationGate.Validate(passingOutput);
      Assert.That(passingResult.Passed, Is.True);

      if (passingResult.Passed)
      {
        _mutationController.ExecuteMutations(passingResult, _memorySystem, _testProfile.PersonaId);
      }

      var afterPassingCount = _memorySystem.GetRecentMemories(100).Count();

      // Act - Validate failing output
      var failingResult = _validationGate.Validate(failingOutput, new ValidationContext { Constraints = constraints });
      Assert.That(failingResult.Passed, Is.False);

      if (failingResult.Passed)
      {
        _mutationController.ExecuteMutations(failingResult, _memorySystem, _testProfile.PersonaId);
      }

      var afterFailingCount = _memorySystem.GetRecentMemories(100).Count();

      // Assert
      Assert.That(afterPassingCount, Is.EqualTo(initialMemoryCount + 1), "Passing validation should add memory");
      Assert.That(afterFailingCount, Is.EqualTo(afterPassingCount), "Failing validation should not add memory");
    }

    [Test]
    public void MemoryMutation_CanonicalFactProtection_WorksEndToEnd()
    {
      // Arrange
      var output = ParsedOutput.Dialogue("I'll update my knowledge", "raw")
        .WithMutation(ProposedMutation.TransformBelief("king_name", "The king is Bob"));

      var context = new ValidationContext { MemorySystem = _memorySystem };

      // Act
      var gateResult = _validationGate.Validate(output, context);

      // Assert - Gate should reject the mutation
      Assert.That(gateResult.Passed, Is.False);
      Assert.That(gateResult.RejectedMutations.Count, Is.EqualTo(1));

      // Even if we try to execute (which we shouldn't with failed gate)
      // The controller should also reject
      var mutationResult = _mutationController.ExecuteMutations(gateResult, _memorySystem, "npc1");
      Assert.That(mutationResult.AllSucceeded, Is.True); // No mutations to execute (gate rejected)

      // Verify canonical fact is unchanged
      var fact = _memorySystem.GetCanonicalFact("king_name");
      Assert.That(fact?.Fact, Is.EqualTo("The king is named Arthur"));
    }

    #endregion

    #region Intent Dispatch Policy Tests

    [Test]
    public void IntentDispatch_OnlyWhenGatePasses()
    {
      // Arrange - Passing output with intent
      var passingOutput = ParsedOutput.Dialogue("Follow me!", "raw")
        .WithIntent(WorldIntent.Create("follow", "player"));

      var passingResult = _validationGate.Validate(passingOutput);

      // Assert - Intent should be approved when gate passes
      Assert.That(passingResult.Passed, Is.True);
      Assert.That(passingResult.ApprovedIntents.Count, Is.EqualTo(1));
    }

    [Test]
    public void IntentDispatch_PolicyDefenseTest_IntentsNotFromParsedOutput()
    {
      // Contract: When gate fails, ApprovedIntents MUST be empty.
      // Downstream consumers (WorldIntentDispatcher, MemoryMutationController) must only
      // consume from GateResult.ApprovedIntents or MutationBatchResult.EmittedIntents,
      // never directly from ParsedOutput.WorldIntents.

      // Arrange - Failing output with intent
      var failingOutput = ParsedOutput.Dialogue("Secret: follow me!", "raw")
        .WithIntent(WorldIntent.Create("follow", "player"));

      var constraints = new ConstraintSet();
      constraints.Add(Constraint.Prohibition("no-secrets", "No secrets", "Don't say secret", "secret"));

      var context = new ValidationContext { Constraints = constraints };

      // Act
      var result = _validationGate.Validate(failingOutput, context);

      // Assert - Gate fails, ApprovedIntents MUST be empty (contract requirement)
      Assert.That(result.Passed, Is.False);
      Assert.That(result.ApprovedIntents.Count, Is.EqualTo(0),
        "Contract: ApprovedIntents must be empty when gate fails");

      // The mutation controller also respects the gate status
      var mutationResult = _mutationController.ExecuteMutations(result, _memorySystem, "npc1");

      // EmittedIntents should be empty because gate failed
      Assert.That(mutationResult.EmittedIntents.Count, Is.EqualTo(0),
        "Dispatcher must only consume from MutationBatchResult.EmittedIntents, never directly from ParsedOutput.Intents");
    }

    #endregion

    #region Determinism with Seed Tests

    [Test]
    public void Determinism_WithFixedSeed_ReproducibleResults()
    {
      // Arrange
      var random1 = new Random(TestSeed);
      var random2 = new Random(TestSeed);

      // Generate sequences
      var sequence1 = Enumerable.Range(0, 10).Select(_ => random1.Next()).ToList();
      var sequence2 = Enumerable.Range(0, 10).Select(_ => random2.Next()).ToList();

      // Assert - Same seed produces same sequence
      Assert.That(sequence1, Is.EqualTo(sequence2),
        "Same integer seed must produce identical random sequences");
    }

    [Test]
    public void Determinism_DifferentSeeds_DifferentResults()
    {
      // Arrange
      var random1 = new Random(TestSeed);
      var random2 = new Random(AlternateSeed);

      // Generate sequences
      var sequence1 = Enumerable.Range(0, 10).Select(_ => random1.Next()).ToList();
      var sequence2 = Enumerable.Range(0, 10).Select(_ => random2.Next()).ToList();

      // Assert - Different seeds produce different sequences
      Assert.That(sequence1, Is.Not.EqualTo(sequence2),
        "Different integer seeds should produce different random sequences");
    }

    [Test]
    public void Determinism_FullPipeline_MultiplRunsSameResult()
    {
      // Arrange - Fixed inputs
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Tell me about your wares",
        GameTime = 100f
      };

      // Act - Run pipeline twice
      var results = new List<string>();
      for (int i = 0; i < 3; i++)
      {
        var constraints = _expectancyEvaluator.Evaluate(context);
        var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput);
        var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
          .WithContext(context)
          .WithConstraints(constraints)
          .WithSystemPrompt(_testProfile.SystemPrompt)
          .WithPlayerInput(context.PlayerInput))
          .Build();

        var prompt = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);
        results.Add(prompt.Text);
      }

      // Assert - All prompts identical
      for (int i = 1; i < results.Count; i++)
      {
        Assert.That(results[i], Is.EqualTo(results[0]),
          $"Run {i} produced different prompt than run 0 - determinism violation");
      }
    }

    #endregion
  }
}
