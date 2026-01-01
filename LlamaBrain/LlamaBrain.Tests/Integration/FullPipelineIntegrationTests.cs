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
  /// Integration tests for the full 9-component architectural pipeline.
  /// Tests all components working together from InteractionContext through validated output and memory mutation.
  /// </summary>
  [TestFixture]
  public class FullPipelineIntegrationTests
  {
    private PersonaProfile _testProfile = null!;
    private PersonaMemoryStore _memoryStore = null!;
    private IApiClient _mockApiClient = null!;
    private ExpectancyEvaluator _expectancyEvaluator = null!;
    private ContextRetrievalLayer _contextRetrieval = null!;
    private PromptAssembler _promptAssembler = null!;
    private OutputParser _outputParser = null!;
    private ValidationGate _validationGate = null!;
    private MemoryMutationController _mutationController = null!;
    private FallbackSystem _fallbackSystem = null!;
    private AuthoritativeMemorySystem _memorySystem = null!;

    [SetUp]
    public void SetUp()
    {
      // Create test persona profile
      _testProfile = PersonaProfile.Create("test-npc", "Test NPC");
      _testProfile.Description = "A friendly shopkeeper";
      _testProfile.SystemPrompt = "You are a helpful shopkeeper. Be friendly and helpful.";

      // Initialize memory store
      _memoryStore = new PersonaMemoryStore();
      _memorySystem = _memoryStore.GetOrCreateSystem(_testProfile.PersonaId);

      // Add canonical facts (immutable)
      _memorySystem.AddCanonicalFact("king_name", "The king is named Arthur", "world_lore");
      _memorySystem.AddCanonicalFact("magic_exists", "Magic is real in this world", "world_lore");

      // Add world state (mutable)
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

      // Initialize components
      _mockApiClient = Substitute.For<IApiClient>();
      _expectancyEvaluator = new ExpectancyEvaluator();
      _contextRetrieval = new ContextRetrievalLayer(_memorySystem);
      _promptAssembler = new PromptAssembler(PromptAssemblerConfig.Default);
      _outputParser = new OutputParser();
      _validationGate = new ValidationGate();
      _mutationController = new MemoryMutationController();
      _fallbackSystem = new FallbackSystem();
    }

    #region Happy Path Tests

    [Test]
    public async Task FullPipeline_ValidInput_CompletesSuccessfully()
    {
      // Arrange - Component 1: Interaction Context
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Hello, can you tell me about the king?",
        GameTime = 100f
      };

      // Component 2: Determinism Layer - Evaluate rules (no rules, so empty constraints)
      var constraints = _expectancyEvaluator.Evaluate(context);

      // Component 3 & 4: Authoritative State Snapshot
      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput);
      var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput))
        .WithAttemptNumber(0)
        .WithMaxAttempts(3)
        .Build();

      // Component 5: Ephemeral Working Memory - Assemble prompt
      var assembledPrompt = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);

      // Component 6: Mocked Stateless Inference Core
      var mockResponse = "Hello! The king is named Arthur. He's a wise ruler.";
      _mockApiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(mockResponse));

      var llmResponse = await _mockApiClient.SendPromptAsync(assembledPrompt.Text, null, null, CancellationToken.None);

      // Component 7: Output Parsing & Validation
      var parsedOutput = _outputParser.Parse(llmResponse);
      var validationContext = new ValidationContext
      {
        Constraints = constraints,
        MemorySystem = _memorySystem,
        Snapshot = snapshot
      };
      var gateResult = _validationGate.Validate(parsedOutput, validationContext);

      // Assert - Validation should pass
      Assert.That(gateResult.Passed, Is.True, "Validation should pass for valid output");
      Assert.That(gateResult.ValidatedOutput, Is.Not.Null);
      Assert.That(gateResult.ValidatedOutput!.DialogueText, Contains.Substring("Arthur"));

      // Component 8: Memory Mutation - Execute approved mutations
      if (gateResult.Passed && gateResult.ApprovedMutations.Count > 0)
      {
        var mutationResult = _mutationController.ExecuteMutations(gateResult, _memorySystem, _testProfile.PersonaId);
        Assert.That(mutationResult.AllSucceeded, Is.True, "All mutations should succeed");
      }
    }

    #endregion

    #region Constraint Enforcement Tests

    [Test]
    public async Task FullPipeline_WithConstraints_EnforcesConstraints()
    {
      // Arrange - Create a rule that prohibits revealing secrets
      var rule = new TestExpectancyRule
      {
        RuleId = "no-secrets",
        RuleName = "No Secrets",
        IsEnabled = true,
        Priority = 100,
        ShouldMatch = true
      };
      rule.OnGenerateConstraints = (ctx, constraints) =>
      {
        constraints.Add(Constraint.Prohibition(
          "no-secrets",
          "Cannot reveal secrets",
          "Do not reveal any secrets",
          "secret", "hidden", "confidential"
        ));
      };

      _expectancyEvaluator.RegisterRule(rule);

      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Tell me a secret"
      };

      var constraints = _expectancyEvaluator.Evaluate(context);
      Assert.That(constraints.Count, Is.GreaterThan(0), "Constraints should be generated");

      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput);
      var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput))
        .Build();

      var assembledPrompt = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);

      // Mock LLM response that violates constraint
      var violatingResponse = "I'll tell you a secret: the treasure is hidden in the cave.";
      _mockApiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(violatingResponse));

      var llmResponse = await _mockApiClient.SendPromptAsync(assembledPrompt.Text, null, null, CancellationToken.None);

      // Parse and validate
      var parsedOutput = _outputParser.Parse(llmResponse);
      var validationContext = new ValidationContext
      {
        Constraints = constraints,
        MemorySystem = _memorySystem,
        Snapshot = snapshot
      };
      var gateResult = _validationGate.Validate(parsedOutput, validationContext);

      // Assert - Validation should fail due to constraint violation
      Assert.That(gateResult.Passed, Is.False, "Validation should fail when constraint is violated");
      Assert.That(gateResult.Failures.Count, Is.GreaterThan(0));
      Assert.That(gateResult.Failures.Any(f => f.Reason == ValidationFailureReason.ProhibitionViolated), Is.True);
    }

    #endregion

    #region Canonical Fact Protection Tests

    [Test]
    public async Task FullPipeline_WithCanonicalFacts_ProtectsFacts()
    {
      // Arrange
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "What's the king's name?"
      };

      var constraints = _expectancyEvaluator.Evaluate(context);
      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput);
      var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput))
        .Build();

      var assembledPrompt = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);

      // Mock LLM response that contradicts canonical fact
      // Must match the validator's negation pattern: "the king is not named arthur" or "the king isn't named arthur"
      var contradictingResponse = "The king is not named Arthur. His name is Bob.";
      _mockApiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(contradictingResponse));

      var llmResponse = await _mockApiClient.SendPromptAsync(assembledPrompt.Text, null, null, CancellationToken.None);

      // Parse and validate
      var parsedOutput = _outputParser.Parse(llmResponse);
      var validationContext = new ValidationContext
      {
        Constraints = constraints,
        MemorySystem = _memorySystem,
        Snapshot = snapshot
      };
      var gateResult = _validationGate.Validate(parsedOutput, validationContext);

      // Assert - Validation should fail due to canonical fact contradiction
      Assert.That(gateResult.Passed, Is.False, "Validation should fail when canonical fact is contradicted");
      Assert.That(gateResult.Failures.Count, Is.GreaterThan(0));
      Assert.That(gateResult.Failures.Any(f => f.Reason == ValidationFailureReason.CanonicalFactContradiction), Is.True);

      // Verify canonical fact is still intact
      var facts = _memorySystem.GetCanonicalFacts();
      var kingFact = facts.FirstOrDefault(f => f.Id == "king_name");
      Assert.That(kingFact, Is.Not.Null);
      Assert.That(kingFact!.Fact, Contains.Substring("Arthur"));
    }

    #endregion

    #region Retry Logic Tests

    [Test]
    public async Task FullPipeline_InvalidOutput_TriggersRetry()
    {
      // Arrange - Create rule with prohibition
      var rule = new TestExpectancyRule
      {
        RuleId = "no-swearing",
        RuleName = "No Swearing",
        IsEnabled = true,
        Priority = 100,
        ShouldMatch = true
      };
      rule.OnGenerateConstraints = (ctx, constraints) =>
      {
        constraints.Add(Constraint.Prohibition("no-swearing", "No swearing", "Do not use profanity", "damn", "hell"));
      };

      _expectancyEvaluator.RegisterRule(rule);

      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Hello"
      };

      var constraints = _expectancyEvaluator.Evaluate(context);
      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput);
      var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput)
        .WithAttemptNumber(0)
        .WithMaxAttempts(3))
        .Build();

      var assembledPrompt = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);

      // Mock first response that violates constraint
      var violatingResponse = "What the hell do you want?";
      _mockApiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(violatingResponse));

      var llmResponse = await _mockApiClient.SendPromptAsync(assembledPrompt.Text, null, null, CancellationToken.None);
      var parsedOutput = _outputParser.Parse(llmResponse);

      var validationContext = new ValidationContext
      {
        Constraints = constraints,
        MemorySystem = _memorySystem,
        Snapshot = snapshot
      };
      var gateResult = _validationGate.Validate(parsedOutput, validationContext);

      // Assert - First attempt should fail
      Assert.That(gateResult.Passed, Is.False, "First attempt should fail");

      // Create retry snapshot with stricter constraints
      var escalatedConstraints = new ConstraintSet();
      escalatedConstraints.Add(Constraint.Prohibition("no-swearing-hard", "No swearing (strict)", "You MUST NOT use any profanity", "damn", "hell", "crap"));
      var retrySnapshot = snapshot.ForRetry(escalatedConstraints);

      // Mock second response that passes (must not contain "hell" as substring - "Hello" contains "hell"!)
      var validResponse = "Hi there! How can I help you today?";
      _mockApiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(validResponse));

      var retryResponse = await _mockApiClient.SendPromptAsync(assembledPrompt.Text, null, null, CancellationToken.None);
      var retryParsed = _outputParser.Parse(retryResponse);

      // Assert - Parsed output should be successful
      Assert.That(retryParsed.Success, Is.True, $"Parsed output should be successful. Error: {retryParsed.ErrorMessage}");

      // Use the merged constraints from the retry snapshot (original + escalated)
      var retryValidationContext = new ValidationContext
      {
        Constraints = retrySnapshot.Constraints,
        MemorySystem = _memorySystem,
        Snapshot = retrySnapshot
      };
      var retryGateResult = _validationGate.Validate(retryParsed, retryValidationContext);

      // Assert - Retry should succeed
      if (!retryGateResult.Passed)
      {
        var failureMessages = string.Join("; ", retryGateResult.Failures.Select(f => $"{f.Reason}: {f.Description}"));
        Assert.Fail($"Retry validation failed with {retryGateResult.Failures.Count} failures: {failureMessages}");
      }
      Assert.That(retryGateResult.Passed, Is.True, "Retry should succeed with valid response");
    }

    #endregion

    #region Fallback System Tests

    [Test]
    public async Task FullPipeline_MaxRetriesExceeded_UsesFallback()
    {
      // Arrange
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Hello"
      };

      var constraints = _expectancyEvaluator.Evaluate(context);
      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput);
      var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput)
        .WithAttemptNumber(3) // Max attempts reached
        .WithMaxAttempts(3))
        .Build();

      // Mock response that fails parsing (contains meta-text pattern)
      var invalidResponse = "Example answer: This is how you should respond";
      _mockApiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(invalidResponse));

      var llmResponse = await _mockApiClient.SendPromptAsync("test", null, null, CancellationToken.None);
      var parsedOutput = _outputParser.Parse(llmResponse);

      // Assert - Parsing should fail (meta-text detected)
      Assert.That(parsedOutput.Success, Is.False, "Parsing should fail due to meta-text");

      var validationContext = new ValidationContext
      {
        Constraints = constraints,
        MemorySystem = _memorySystem,
        Snapshot = snapshot
      };
      var gateResult = _validationGate.Validate(parsedOutput, validationContext);

      // Assert - Validation should fail (because parsing failed)
      Assert.That(gateResult.Passed, Is.False, "Validation should fail when parsing fails");

      // Use fallback system
      var fallbackResponse = _fallbackSystem.GetFallbackResponse(context, "Validation failed after max retries");

      // Assert - Fallback should provide a response
      Assert.That(fallbackResponse, Is.Not.Null);
      Assert.That(fallbackResponse, Is.Not.Empty);
      Assert.That(_fallbackSystem.Stats.TotalFallbacks, Is.EqualTo(1));
    }

    #endregion

    #region Memory Mutation Tests

    [Test]
    public async Task FullPipeline_MemoryMutation_ExecutesApprovedMutations()
    {
      // Arrange
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "I saved your life!"
      };

      var constraints = _expectancyEvaluator.Evaluate(context);
      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput);
      var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput))
        .Build();

      var assembledPrompt = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);

      // Mock response with proposed mutations (in structured format)
      var responseWithMutations = @"Player saved my life. I'm very grateful.
[MUTATION: AppendEpisodic] Player saved my life
[MUTATION: TransformBelief] player is a hero (confidence: 0.95)";
      _mockApiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(responseWithMutations));

      var llmResponse = await _mockApiClient.SendPromptAsync(assembledPrompt.Text, null, null, CancellationToken.None);
      var parsedOutput = _outputParser.Parse(llmResponse);

      var validationContext = new ValidationContext
      {
        Constraints = constraints,
        MemorySystem = _memorySystem,
        Snapshot = snapshot
      };
      var gateResult = _validationGate.Validate(parsedOutput, validationContext);

      // Assert - Validation should pass
      Assert.That(gateResult.Passed, Is.True, "Validation should pass");

      // Execute mutations
      if (gateResult.ApprovedMutations.Count > 0)
      {
        var mutationResult = _mutationController.ExecuteMutations(gateResult, _memorySystem, _testProfile.PersonaId);

        // Assert - Mutations should execute
        Assert.That(mutationResult.TotalAttempted, Is.GreaterThan(0));
        Assert.That(mutationResult.AllSucceeded, Is.True);

        // Verify memory was updated
        var memories = _memorySystem.GetRecentMemories(10);
        Assert.That(memories.Any(m => m.Content.Contains("saved my life")), Is.True);
      }
    }

    #endregion

    #region Context Retrieval Tests

    [Test]
    public void FullPipeline_ContextRetrieval_IncludesRelevantMemories()
    {
      // Arrange
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "Tell me about the king"
      };

      // Retrieve context
      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput);

      // Build snapshot
      var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput))
        .Build();

      // Assert - Canonical facts should be included
      Assert.That(snapshot.CanonicalFacts.Count, Is.GreaterThan(0));
      Assert.That(snapshot.CanonicalFacts.Any(f => f.Contains("Arthur")), Is.True);

      // Assert - World state should be included
      Assert.That(snapshot.WorldState.Count, Is.GreaterThan(0));

      // Assert - Episodic memories should be included
      Assert.That(snapshot.EpisodicMemories.Count, Is.GreaterThan(0));
    }

    #endregion

    #region World Intent Tests

    [Test]
    public async Task FullPipeline_WorldIntent_DispatchesIntent()
    {
      // Arrange
      var context = new InteractionContext
      {
        TriggerReason = TriggerReason.PlayerUtterance,
        NpcId = _testProfile.PersonaId,
        PlayerInput = "I want to buy something"
      };

      var constraints = _expectancyEvaluator.Evaluate(context);
      var retrievedContext = _contextRetrieval.RetrieveContext(context.PlayerInput);
      var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
        .WithContext(context)
        .WithConstraints(constraints)
        .WithSystemPrompt(_testProfile.SystemPrompt)
        .WithPlayerInput(context.PlayerInput))
        .Build();

      var assembledPrompt = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);

      // Mock response with world intent
      // OutputParser expects [INTENT: ...] or [ACTION: ...] format
      var responseWithIntent = @"I'll help you with that!
[INTENT: OpenShop] Open the shop interface";
      _mockApiClient.SendPromptAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<float?>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(responseWithIntent));

      var llmResponse = await _mockApiClient.SendPromptAsync(assembledPrompt.Text, null, null, CancellationToken.None);
      var parsedOutput = _outputParser.Parse(llmResponse);

      var validationContext = new ValidationContext
      {
        Constraints = constraints,
        MemorySystem = _memorySystem,
        Snapshot = snapshot
      };
      var gateResult = _validationGate.Validate(parsedOutput, validationContext);

      // Assert - Validation should pass
      Assert.That(gateResult.Passed, Is.True);

      // Assert - World intents should be approved
      Assert.That(gateResult.ApprovedIntents.Count, Is.GreaterThan(0));
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Test implementation of IExpectancyRule for testing.
    /// </summary>
    private class TestExpectancyRule : IExpectancyRule
    {
      public string RuleId { get; set; } = "";
      public string RuleName { get; set; } = "";
      public bool IsEnabled { get; set; } = true;
      public int Priority { get; set; } = 100;
      public bool ShouldMatch { get; set; } = true;
      public Action<InteractionContext, ConstraintSet>? OnGenerateConstraints { get; set; }

      public bool Evaluate(InteractionContext context)
      {
        return ShouldMatch;
      }

      public void GenerateConstraints(InteractionContext context, ConstraintSet constraintSet)
      {
        OnGenerateConstraints?.Invoke(context, constraintSet);
      }
    }

    #endregion
  }
}
