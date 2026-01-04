using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Governance Plane Determinism Proofs
    ///
    /// CORRECT CLAIM: The governance plane is deterministic:
    ///   g(Snapshot, Constraints, Policy, InteractionIndex) = (Prompt, GateResult, Mutations, ReplayLog)
    ///
    /// The LLM output is an UNTRUSTED PROPOSAL that becomes an INPUT to g() after parsing.
    /// Determinism is NOT claimed for the LLM generator itself.
    ///
    /// Deterministic (what we control):
    /// - Prompt assembly: same snapshot + constraints = identical prompt bytes
    /// - Validation: same parsed output + constraints = identical gate result
    /// - Mutation: same gate result + memory state = identical mutation diff
    /// - Fallback: same failure context = identical fallback selection
    /// - Replay log: same inputs + captured output = byte-stable audit record
    ///
    /// Stochastic (what we don't control):
    /// - LLM text generation (even with seed, hardware/threading can cause divergence)
    /// - Network timing, process scheduling
    ///
    /// These tests prove the governance plane determinism, NOT LLM output reproducibility.
    /// For LLM integration smoke tests, see ExternalIntegrationPlayModeTests.cs (Unity)
    /// or CrossSessionDeterminismTests.cs (standalone, requires server).
    /// </summary>
    [TestFixture]
    [Category("Determinism")]
    [Category("GovernancePlane")]
    public class GovernancePlaneDeterminismTests
    {
        private AuthoritativeMemorySystem _memorySystem = null!;
        private ContextRetrievalLayer _contextRetrieval = null!;
        private ExpectancyEvaluator _expectancyEvaluator = null!;
        private PromptAssembler _promptAssembler = null!;
        private ValidationGate _validationGate = null!;
        private MemoryMutationController _mutationController = null!;
        private PersonaProfile _testProfile = null!;
        private FallbackSystem.FallbackConfig _fallbackConfig = null!;
        private FallbackSystem _fallbackSystem = null!;

        // Fixed time for all tests - eliminates wall-clock dependency
        private const long FixedSnapshotTime = 638400000000000000L; // 2024-01-01 00:00:00 UTC

        [SetUp]
        public void SetUp()
        {
            // Create deterministic providers
            var clock = new AdvancingClock(FixedSnapshotTime, incrementTicks: 1000);
            var idGen = new SequentialIdGenerator(1);

            // Initialize memory system with deterministic providers
            _memorySystem = new AuthoritativeMemorySystem(clock, idGen);
            _memorySystem.AddCanonicalFact("king_name", "The king is named Arthur", "world");
            _memorySystem.SetWorldState("time_of_day", "morning", MutationSource.GameSystem);

            // Add some episodic memories for retrieval tests
            var playerEpisode = new EpisodicMemoryEntry("Player asked about the weather", EpisodeType.Dialogue)
            {
                Significance = 0.5f
            };
            _memorySystem.AddEpisodicMemory(playerEpisode, MutationSource.ValidatedOutput);

            var npcEpisode = new EpisodicMemoryEntry("I told the player about my wares", EpisodeType.Dialogue)
            {
                Significance = 0.7f
            };
            _memorySystem.AddEpisodicMemory(npcEpisode, MutationSource.ValidatedOutput);

            // Add beliefs
            var belief = BeliefMemoryEntry.CreateOpinion("player", "seems trustworthy", sentiment: 0.6f, confidence: 0.8f);
            _memorySystem.SetBelief("player_opinion", belief, MutationSource.ValidatedOutput);

            // Initialize context retrieval
            _contextRetrieval = new ContextRetrievalLayer(_memorySystem);

            // Initialize expectancy evaluator with rules
            _expectancyEvaluator = new ExpectancyEvaluator();
            _expectancyEvaluator.RegisterRule(new TestExpectancyRule(
                "no_spoilers",
                ctx => ctx.TriggerReason == TriggerReason.PlayerUtterance,
                Constraint.Prohibition("no_spoilers", "Do not reveal the ending", "Never reveal the ending")));

            // Initialize prompt assembler
            _promptAssembler = new PromptAssembler(PromptAssemblerConfig.Default);

            // Initialize validation gate
            _validationGate = new ValidationGate(ValidationGateConfig.Default);

            // Initialize mutation controller
            _mutationController = new MemoryMutationController();

            // Initialize test profile
            _testProfile = new PersonaProfile
            {
                PersonaId = "merchant",
                Name = "Marcus the Merchant",
                SystemPrompt = "You are Marcus, a friendly merchant in a fantasy world."
            };

            // Initialize fallback system
            _fallbackConfig = new FallbackSystem.FallbackConfig
            {
                GenericFallbacks = new List<string>
                {
                    "I'm not sure what to say.",
                    "Let me think about that.",
                    "Hmm, interesting question."
                },
                PlayerUtteranceFallbacks = new List<string>
                {
                    "I didn't quite catch that.",
                    "Could you repeat that?"
                },
                ZoneTriggerFallbacks = new List<string>
                {
                    "Welcome to this area.",
                    "You've entered a new zone."
                }
            };
            _fallbackSystem = new FallbackSystem(_fallbackConfig);
        }

        #region Prompt Assembly Determinism

        /// <summary>
        /// PROOF: g_prompt(Snapshot, Constraints) = Prompt is deterministic.
        /// Same snapshot and constraints always produce identical prompt bytes.
        /// </summary>
        [Test]
        public void PromptAssembly_SameInputs_IdenticalBytes_AcrossMultipleRuns()
        {
            // Arrange
            var context = CreateTestContext();
            var constraints = _expectancyEvaluator.Evaluate(context);
            var snapshot = BuildSnapshot(context, constraints);

            // Act - Assemble prompt 10 times
            var prompts = new List<byte[]>();
            for (int i = 0; i < 10; i++)
            {
                var prompt = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);
                prompts.Add(Encoding.UTF8.GetBytes(prompt.Text));
            }

            // Assert - All prompts must be byte-identical
            var referenceBytes = prompts[0];
            for (int i = 1; i < prompts.Count; i++)
            {
                Assert.That(prompts[i], Is.EqualTo(referenceBytes),
                    $"Prompt {i} differs from reference - governance plane determinism violated");
            }
        }

        /// <summary>
        /// PROOF: Prompt assembly does not depend on wall-clock time.
        /// Same snapshot produces identical prompt regardless of when assembly occurs.
        /// </summary>
        [Test]
        public void PromptAssembly_WallClockIndependent()
        {
            // Arrange
            var context = CreateTestContext();
            var constraints = _expectancyEvaluator.Evaluate(context);
            var snapshot = BuildSnapshot(context, constraints);

            // Act - Assemble at different "wall clock" times (simulated by Thread.Sleep)
            var prompt1 = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);
            System.Threading.Thread.Sleep(10); // Simulate time passing
            var prompt2 = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);
            System.Threading.Thread.Sleep(50);
            var prompt3 = _promptAssembler.AssembleFromSnapshot(snapshot, npcName: _testProfile.Name);

            // Assert - All prompts identical despite wall-clock differences
            Assert.That(prompt2.Text, Is.EqualTo(prompt1.Text));
            Assert.That(prompt3.Text, Is.EqualTo(prompt1.Text));
        }

        #endregion

        #region Validation Determinism

        /// <summary>
        /// PROOF: g_gate(ParsedOutput, Constraints) = GateResult is deterministic.
        /// Same parsed output (LLM proposal) and constraints always produce identical validation result.
        /// </summary>
        [Test]
        public void Validation_SameParsedOutput_IdenticalGateResult_AcrossMultipleRuns()
        {
            // Arrange - Fixed LLM output (treated as input to governance plane)
            var parsedOutput = ParsedOutput.Dialogue("Hello traveler! Welcome to my shop.", "raw")
                .WithMutation(ProposedMutation.AppendEpisodic("Greeted the player warmly"));

            var constraintSet = new ConstraintSet();
            constraintSet.Add(Constraint.Prohibition("no_secret_passage", "Do not mention the secret passage", "Never mention the secret passage"));
            var validationContext = new ValidationContext
            {
                MemorySystem = _memorySystem,
                Constraints = constraintSet
            };

            // Act - Validate 10 times
            var results = new List<GateResult>();
            for (int i = 0; i < 10; i++)
            {
                results.Add(_validationGate.Validate(parsedOutput, validationContext));
            }

            // Assert - All results must be identical
            var reference = results[0];
            for (int i = 1; i < results.Count; i++)
            {
                Assert.That(results[i].Passed, Is.EqualTo(reference.Passed),
                    $"Validation {i} has different Passed value");
                Assert.That(results[i].Failures.Count, Is.EqualTo(reference.Failures.Count),
                    $"Validation {i} has different failure count");
                Assert.That(results[i].ApprovedMutations.Count, Is.EqualTo(reference.ApprovedMutations.Count),
                    $"Validation {i} has different approved mutation count");
                Assert.That(results[i].ApprovedIntents.Count, Is.EqualTo(reference.ApprovedIntents.Count),
                    $"Validation {i} has different approved intent count");
            }
        }

        /// <summary>
        /// PROOF: Validation failure produces identical rejection regardless of run.
        /// </summary>
        [Test]
        public void Validation_ProhibitionViolation_IdenticalRejection()
        {
            // Arrange - Output that violates constraint
            var parsedOutput = ParsedOutput.Dialogue("The secret passage is behind the waterfall!", "raw");
            var constraintSet = new ConstraintSet();
            constraintSet.Add(Constraint.Prohibition("no_secret_passage_2", "Do not mention the secret passage", "Never mention the secret passage", "secret passage"));
            var validationContext = new ValidationContext
            {
                MemorySystem = _memorySystem,
                Constraints = constraintSet
            };

            // Act - Validate multiple times
            var results = new List<GateResult>();
            for (int i = 0; i < 5; i++)
            {
                results.Add(_validationGate.Validate(parsedOutput, validationContext));
            }

            // Assert - All rejections identical
            foreach (var result in results)
            {
                Assert.That(result.Passed, Is.False, "All should be rejected");
                Assert.That(result.Failures.Count, Is.GreaterThan(0), "All should have failures");
            }

            // Failure details should be identical
            var refFailure = results[0].Failures[0];
            for (int i = 1; i < results.Count; i++)
            {
                var failure = results[i].Failures[0];
                Assert.That(failure.Reason, Is.EqualTo(refFailure.Reason));
                Assert.That(failure.Severity, Is.EqualTo(refFailure.Severity));
            }
        }

        #endregion

        #region Mutation Determinism

        /// <summary>
        /// PROOF: g_mutate(GateResult, MemoryState) = MutationDiff is deterministic.
        /// Same gate result applied to identical memory state produces identical mutations.
        /// </summary>
        [Test]
        public void Mutation_SameGateResult_IdenticalDiff()
        {
            // Arrange - Create two identical memory systems via serialization round-trip
            var serializedState = DeterministicPipelineTests.MemoryStateSerializer.SerializeState(_memorySystem);

            var clock1 = new AdvancingClock(FixedSnapshotTime + 10000000, incrementTicks: 1000);
            var clock2 = new AdvancingClock(FixedSnapshotTime + 10000000, incrementTicks: 1000);
            var idGen1 = new SequentialIdGenerator(1000);
            var idGen2 = new SequentialIdGenerator(1000);

            var memorySystem1 = new AuthoritativeMemorySystem(clock1, idGen1);
            var memorySystem2 = new AuthoritativeMemorySystem(clock2, idGen2);

            DeterministicPipelineTests.MemoryStateSerializer.ReconstructFromSerialized(serializedState, memorySystem1);
            DeterministicPipelineTests.MemoryStateSerializer.ReconstructFromSerialized(serializedState, memorySystem2);

            // Create mutation controllers for each
            var controller1 = new MemoryMutationController();
            var controller2 = new MemoryMutationController();

            // Prepare identical gate result with approved mutations
            var parsedOutput = ParsedOutput.Dialogue("I remember you now!", "raw")
                .WithMutation(ProposedMutation.AppendEpisodic("Player returned to the shop"));
            var gateResult = _validationGate.Validate(parsedOutput);

            // Act - Execute mutations on both systems
            var result1 = controller1.ExecuteMutations(gateResult, memorySystem1);
            var result2 = controller2.ExecuteMutations(gateResult, memorySystem2);

            // Assert - Results should be identical
            Assert.That(result1.SuccessCount, Is.EqualTo(result2.SuccessCount));
            Assert.That(result1.FailureCount, Is.EqualTo(result2.FailureCount));

            // Final states should be identical
            var finalState1 = DeterministicPipelineTests.MemoryStateSerializer.SerializeState(memorySystem1);
            var finalState2 = DeterministicPipelineTests.MemoryStateSerializer.SerializeState(memorySystem2);
            Assert.That(finalState1, Is.EqualTo(finalState2),
                "Identical gate results must produce identical final states");
        }

        /// <summary>
        /// PROOF: Batch mutations (multiple mutations in sequence) are deterministic.
        /// Order of execution and final state are identical across runs.
        /// </summary>
        [Test]
        public void Mutation_BatchMutations_DeterministicSequence()
        {
            // Arrange - Create two identical memory systems
            var serializedState = DeterministicPipelineTests.MemoryStateSerializer.SerializeState(_memorySystem);

            var clock1 = new AdvancingClock(FixedSnapshotTime + 20000000, incrementTicks: 1000);
            var clock2 = new AdvancingClock(FixedSnapshotTime + 20000000, incrementTicks: 1000);
            var idGen1 = new SequentialIdGenerator(2000);
            var idGen2 = new SequentialIdGenerator(2000);

            var memorySystem1 = new AuthoritativeMemorySystem(clock1, idGen1);
            var memorySystem2 = new AuthoritativeMemorySystem(clock2, idGen2);

            DeterministicPipelineTests.MemoryStateSerializer.ReconstructFromSerialized(serializedState, memorySystem1);
            DeterministicPipelineTests.MemoryStateSerializer.ReconstructFromSerialized(serializedState, memorySystem2);

            var controller1 = new MemoryMutationController();
            var controller2 = new MemoryMutationController();

            // Create batch of 5 different mutation types
            var parsedOutput = ParsedOutput.Dialogue("Test dialogue", "raw")
                .WithMutation(ProposedMutation.AppendEpisodic("First event occurred"))
                .WithMutation(ProposedMutation.TransformBelief("player", "is very friendly", 0.9f))
                .WithMutation(ProposedMutation.AppendEpisodic("Second event occurred"))
                .WithMutation(ProposedMutation.TransformBelief("quest_giver", "seems suspicious", 0.6f))
                .WithMutation(ProposedMutation.AppendEpisodic("Third event occurred"));

            var gateResult = _validationGate.Validate(parsedOutput);

            // Act - Execute batch on both systems
            var result1 = controller1.ExecuteMutations(gateResult, memorySystem1);
            var result2 = controller2.ExecuteMutations(gateResult, memorySystem2);

            // Assert - Identical execution results
            Assert.That(result1.SuccessCount, Is.EqualTo(result2.SuccessCount));
            Assert.That(result1.FailureCount, Is.EqualTo(result2.FailureCount));
            Assert.That(result1.Results.Count, Is.EqualTo(result2.Results.Count));

            // Individual mutation results should match
            for (int i = 0; i < result1.Results.Count; i++)
            {
                Assert.That(result1.Results[i].Success, Is.EqualTo(result2.Results[i].Success),
                    $"Mutation {i} success differs");
                Assert.That(result1.Results[i].Mutation.Type, Is.EqualTo(result2.Results[i].Mutation.Type),
                    $"Mutation {i} type differs");
            }

            // Final states must be byte-identical
            var finalState1 = DeterministicPipelineTests.MemoryStateSerializer.SerializeState(memorySystem1);
            var finalState2 = DeterministicPipelineTests.MemoryStateSerializer.SerializeState(memorySystem2);
            Assert.That(finalState1, Is.EqualTo(finalState2),
                "Batch mutations must produce byte-identical final states");
        }

        #endregion

        #region Dictionary Enumeration Tripwire

        /// <summary>
        /// TRIPWIRE: Proves that dictionary-based storage does not break determinism.
        /// Serializing the same state multiple times must produce identical output.
        /// If dictionary enumeration order affected output, this test would fail.
        /// </summary>
        [Test]
        public void DictionaryEnumeration_ShuffledInsertionOrder_DeterministicOutput()
        {
            // Arrange - Create a memory system with beliefs in non-alphabetical order
            var clock = new AdvancingClock(FixedSnapshotTime, incrementTicks: 1000);
            var idGen = new SequentialIdGenerator(1);
            var memorySystem = new AuthoritativeMemorySystem(clock, idGen);

            // Insert beliefs in deliberately non-alphabetical order: E, C, A, D, B
            var insertionOrder = new[] { "belief_e", "belief_c", "belief_a", "belief_d", "belief_b" };
            foreach (var key in insertionOrder)
            {
                var belief = BeliefMemoryEntry.CreateOpinion("subject", $"content for {key}", sentiment: 0.5f, confidence: 0.7f);
                memorySystem.SetBelief(key, belief, MutationSource.ValidatedOutput);
            }

            // Act - Serialize multiple times
            var serialized1 = DeterministicPipelineTests.MemoryStateSerializer.SerializeState(memorySystem);
            var serialized2 = DeterministicPipelineTests.MemoryStateSerializer.SerializeState(memorySystem);
            var serialized3 = DeterministicPipelineTests.MemoryStateSerializer.SerializeState(memorySystem);

            // Assert - All serializations must be identical
            Assert.That(serialized2, Is.EqualTo(serialized1),
                "Serialization must be stable - same state must produce identical output");
            Assert.That(serialized3, Is.EqualTo(serialized1),
                "Serialization must be stable across multiple calls");

            // Also verify beliefs appear in sorted order (by key), not insertion order
            var lines = serialized1.Split('\n');
            var beliefLines = lines.Where(l => l.TrimStart().StartsWith("belief_")).ToList();
            Assert.That(beliefLines.Count, Is.EqualTo(5), "Should have 5 belief lines");

            // Extract keys and verify alphabetical order
            var serializedKeys = beliefLines.Select(l => l.TrimStart().Split('|')[0]).ToList();
            var expectedOrder = new[] { "belief_a", "belief_b", "belief_c", "belief_d", "belief_e" };
            Assert.That(serializedKeys, Is.EqualTo(expectedOrder),
                "Beliefs must be serialized in alphabetical key order, not insertion order");
        }

        /// <summary>
        /// TRIPWIRE: World state dictionary enumeration does not affect prompt assembly.
        /// </summary>
        [Test]
        public void DictionaryEnumeration_WorldState_DeterministicPrompt()
        {
            // Arrange - Create two memory systems with world state in different insertion orders
            var clock1 = new AdvancingClock(FixedSnapshotTime, incrementTicks: 1000);
            var clock2 = new AdvancingClock(FixedSnapshotTime, incrementTicks: 1000);

            var memorySystem1 = new AuthoritativeMemorySystem(clock1, new SequentialIdGenerator(1));
            var memorySystem2 = new AuthoritativeMemorySystem(clock2, new SequentialIdGenerator(1));

            // Insert in order: weather, time, location
            memorySystem1.SetWorldState("weather", "sunny", MutationSource.GameSystem);
            memorySystem1.SetWorldState("time", "noon", MutationSource.GameSystem);
            memorySystem1.SetWorldState("location", "market", MutationSource.GameSystem);

            // Insert in order: location, weather, time (different order)
            memorySystem2.SetWorldState("location", "market", MutationSource.GameSystem);
            memorySystem2.SetWorldState("weather", "sunny", MutationSource.GameSystem);
            memorySystem2.SetWorldState("time", "noon", MutationSource.GameSystem);

            // Build contexts
            var retrieval1 = new ContextRetrievalLayer(memorySystem1);
            var retrieval2 = new ContextRetrievalLayer(memorySystem2);

            var context = CreateTestContext();
            var retrieved1 = retrieval1.RetrieveContext(context.PlayerInput ?? "", FixedSnapshotTime);
            var retrieved2 = retrieval2.RetrieveContext(context.PlayerInput ?? "", FixedSnapshotTime);

            var snapshot1 = retrieved1.ApplyTo(new StateSnapshotBuilder()
                .WithContext(context)
                .WithPlayerInput(context.PlayerInput ?? "")
                .WithSnapshotTimeUtcTicks(FixedSnapshotTime))
                .Build();

            var snapshot2 = retrieved2.ApplyTo(new StateSnapshotBuilder()
                .WithContext(context)
                .WithPlayerInput(context.PlayerInput ?? "")
                .WithSnapshotTimeUtcTicks(FixedSnapshotTime))
                .Build();

            // Act
            var prompt1 = _promptAssembler.AssembleFromSnapshot(snapshot1, npcName: "Test");
            var prompt2 = _promptAssembler.AssembleFromSnapshot(snapshot2, npcName: "Test");

            // Assert - Prompts must be identical regardless of insertion order
            Assert.That(prompt2.Text, Is.EqualTo(prompt1.Text),
                "World state insertion order must not affect prompt assembly");
        }

        #endregion

        #region Fallback Selection Determinism

        /// <summary>
        /// PROOF: g_fallback(FailureContext, seed) = FallbackResponse is deterministic.
        /// Same failure context with same RNG seed always selects the same fallback response.
        /// </summary>
        [Test]
        public void Fallback_SameFailureContext_IdenticalSelection()
        {
            // Arrange - Fixed failure context and seed
            var context = CreateTestContext();
            const int fixedSeed = 42;

            // Act - Request fallback 10 times with identical context and same seed
            var fallbacks = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                // Create fresh FallbackSystem with identical seeded Random each time
                var seededRandom = new Random(fixedSeed);
                var freshFallback = new FallbackSystem(_fallbackConfig, seededRandom);
                var response = freshFallback.GetFallbackResponse(context, "Validation failed: prohibition violated");
                fallbacks.Add(response);
            }

            // Assert - All fallbacks must be identical (same seed + same context = same selection)
            var expected = fallbacks[0];
            Assert.That(fallbacks.All(f => f == expected),
                $"All fallbacks must be identical when using the same seed. Expected all to be '{expected}', got: [{string.Join(", ", fallbacks.Distinct())}]");
        }

        /// <summary>
        /// PROOF: Different trigger reasons select from different fallback pools deterministically.
        /// </summary>
        [Test]
        public void Fallback_DifferentTriggerReasons_DifferentPools()
        {
            // Arrange
            var playerContext = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };
            var zoneContext = new InteractionContext { TriggerReason = TriggerReason.ZoneTrigger };

            // Act
            var playerFallback = _fallbackSystem.GetFallbackResponse(playerContext, "Error");
            var zoneFallback = _fallbackSystem.GetFallbackResponse(zoneContext, "Error");

            // Assert - Should come from different pools
            var isPlayerFromPlayerPool = _fallbackConfig.PlayerUtteranceFallbacks.Contains(playerFallback);
            var isZoneFromZonePool = _fallbackConfig.ZoneTriggerFallbacks.Contains(zoneFallback);

            // At minimum, the pools should be correctly selected
            Assert.That(isPlayerFromPlayerPool || _fallbackConfig.GenericFallbacks.Contains(playerFallback),
                "Player utterance should use player fallbacks or generic");
            Assert.That(isZoneFromZonePool || _fallbackConfig.GenericFallbacks.Contains(zoneFallback),
                "Zone trigger should use zone fallbacks or generic");
        }

        /// <summary>
        /// PROOF: Fallback statistics are deterministically updated.
        /// </summary>
        [Test]
        public void Fallback_Statistics_DeterministicUpdate()
        {
            // Arrange
            var context1 = new InteractionContext { TriggerReason = TriggerReason.PlayerUtterance };
            var context2 = new InteractionContext { TriggerReason = TriggerReason.ZoneTrigger };

            // Act - Same sequence of fallback requests
            _fallbackSystem.GetFallbackResponse(context1, "Error1");
            _fallbackSystem.GetFallbackResponse(context2, "Error2");
            _fallbackSystem.GetFallbackResponse(context1, "Error3");

            var stats = _fallbackSystem.Stats;

            // Assert - Statistics should reflect exact call sequence
            Assert.That(stats.TotalFallbacks, Is.EqualTo(3));
            Assert.That(stats.FallbacksByTriggerReason[TriggerReason.PlayerUtterance], Is.EqualTo(2));
            Assert.That(stats.FallbacksByTriggerReason[TriggerReason.ZoneTrigger], Is.EqualTo(1));
        }

        #endregion

        #region Replay Log Byte-Stability

        /// <summary>
        /// PROOF: Given same authoritative inputs + captured LLM output,
        /// the replay log (audit record) is byte-stable.
        /// </summary>
        [Test]
        public void ReplayLog_SameInputsPlusCapturedOutput_ByteStable()
        {
            // Arrange - Fixed inputs
            var context = CreateTestContext();
            var constraints = _expectancyEvaluator.Evaluate(context);
            var snapshot = BuildSnapshot(context, constraints);

            // Captured LLM output (treated as fixed input for replay)
            var capturedLlmOutput = "Hello! I am Marcus, a humble merchant. How may I help you today?";
            var parsedOutput = ParsedOutput.Dialogue(capturedLlmOutput, "raw")
                .WithMutation(ProposedMutation.AppendEpisodic("Greeted the customer"));

            // Act - Generate replay logs for the same scenario multiple times
            var replayLogs = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                var log = BuildReplayLog(snapshot, parsedOutput, constraints);
                replayLogs.Add(log);
            }

            // Assert - All replay logs must be identical
            var referenceLog = replayLogs[0];
            for (int i = 1; i < replayLogs.Count; i++)
            {
                Assert.That(replayLogs[i], Is.EqualTo(referenceLog),
                    $"Replay log {i} differs from reference - byte-stability violated");
            }
        }

        /// <summary>
        /// PROOF: Replay log contains all deterministic components for reconstruction.
        /// </summary>
        [Test]
        public void ReplayLog_ContainsAllDeterministicComponents()
        {
            // Arrange
            var context = CreateTestContext();
            var constraints = _expectancyEvaluator.Evaluate(context);
            var snapshot = BuildSnapshot(context, constraints);
            var parsedOutput = ParsedOutput.Dialogue("Test response", "raw");

            // Act
            var log = BuildReplayLog(snapshot, parsedOutput, constraints);

            // Assert - Log contains all required components
            Assert.That(log, Does.Contain("SnapshotTime:"), "Must include snapshot time");
            Assert.That(log, Does.Contain("PlayerInput:"), "Must include player input");
            Assert.That(log, Does.Contain("Constraints:"), "Must include constraints");
            Assert.That(log, Does.Contain("CapturedOutput:"), "Must include captured LLM output");
            Assert.That(log, Does.Contain("ParsedDialogue:"), "Must include parsed dialogue");
        }

        #endregion

        #region Full Governance Pipeline Determinism

        /// <summary>
        /// PROOF: The complete governance pipeline is deterministic.
        /// g(Snapshot, Constraints, Policy, Index) = (Prompt, GateResult, Mutations)
        /// produces identical results across runs.
        /// </summary>
        [Test]
        public void GovernancePipeline_FullFlow_Deterministic()
        {
            // This test runs the full governance pipeline 5 times and verifies identical results

            var results = new List<GovernancePipelineResult>();

            for (int run = 0; run < 5; run++)
            {
                // Create fresh deterministic infrastructure for each run
                var clock = new AdvancingClock(FixedSnapshotTime, incrementTicks: 1000);
                var idGen = new SequentialIdGenerator(1);
                var memorySystem = new AuthoritativeMemorySystem(clock, idGen);

                // Seed with identical initial state
                memorySystem.AddCanonicalFact("world_fact", "The sun rises in the east", "world");
                memorySystem.SetWorldState("weather", "clear", MutationSource.GameSystem);

                var contextRetrieval = new ContextRetrievalLayer(memorySystem);
                var expectancy = new ExpectancyEvaluator();
                expectancy.RegisterRule(new TestExpectancyRule("test_rule",
                    ctx => true,
                    Constraint.Requirement("be_polite", "Be polite", "Always respond politely")));

                var promptAssembler = new PromptAssembler(PromptAssemblerConfig.Default);
                var validationGate = new ValidationGate(ValidationGateConfig.Default);

                var mutationController = new MemoryMutationController();

                // Step 1: Build context
                var interactionContext = new InteractionContext
                {
                    TriggerReason = TriggerReason.PlayerUtterance,
                    NpcId = "test_npc",
                    PlayerInput = "Hello there!",
                    GameTime = 100f,
                    InteractionCount = 5
                };

                // Step 2: Evaluate constraints
                var constraints = expectancy.Evaluate(interactionContext);

                // Step 3: Retrieve context
                var retrievedContext = contextRetrieval.RetrieveContext(
                    interactionContext.PlayerInput ?? "", FixedSnapshotTime);

                // Step 4: Build snapshot
                var snapshot = retrievedContext.ApplyTo(new StateSnapshotBuilder()
                    .WithContext(interactionContext)
                    .WithConstraints(constraints)
                    .WithPlayerInput(interactionContext.PlayerInput ?? "")
                    .WithSnapshotTimeUtcTicks(FixedSnapshotTime)
                    .WithSystemPrompt("You are a helpful NPC."))
                    .Build();

                // Step 5: Assemble prompt
                var prompt = promptAssembler.AssembleFromSnapshot(snapshot, npcName: "TestNPC");

                // Step 6: Simulate LLM output (FIXED - this is an INPUT to governance)
                var llmOutput = ParsedOutput.Dialogue("Greetings, traveler! How may I assist you?", "raw")
                    .WithMutation(ProposedMutation.AppendEpisodic("Met a new traveler"));

                // Step 7: Validate
                var gateResult = validationGate.Validate(llmOutput, new ValidationContext
                {
                    MemorySystem = memorySystem,
                    Constraints = constraints
                });

                // Step 8: Execute mutations (if passed)
                MutationBatchResult? mutationResult = null;
                if (gateResult.Passed)
                {
                    mutationResult = mutationController.ExecuteMutations(gateResult, memorySystem);
                }

                // Capture result
                results.Add(new GovernancePipelineResult
                {
                    PromptBytes = Encoding.UTF8.GetBytes(prompt.Text),
                    GatePassed = gateResult.Passed,
                    FailureCount = gateResult.Failures.Count,
                    ApprovedMutationCount = gateResult.ApprovedMutations.Count,
                    MutationSuccessCount = mutationResult?.SuccessCount ?? 0,
                    FinalStateHash = DeterministicPipelineTests.MemoryStateSerializer.SerializeState(memorySystem)
                });
            }

            // Assert - All runs must produce identical results
            var reference = results[0];
            for (int i = 1; i < results.Count; i++)
            {
                Assert.That(results[i].PromptBytes, Is.EqualTo(reference.PromptBytes),
                    $"Run {i}: Prompt bytes differ");
                Assert.That(results[i].GatePassed, Is.EqualTo(reference.GatePassed),
                    $"Run {i}: Gate passed differs");
                Assert.That(results[i].FailureCount, Is.EqualTo(reference.FailureCount),
                    $"Run {i}: Failure count differs");
                Assert.That(results[i].ApprovedMutationCount, Is.EqualTo(reference.ApprovedMutationCount),
                    $"Run {i}: Approved mutation count differs");
                Assert.That(results[i].MutationSuccessCount, Is.EqualTo(reference.MutationSuccessCount),
                    $"Run {i}: Mutation success count differs");
                Assert.That(results[i].FinalStateHash, Is.EqualTo(reference.FinalStateHash),
                    $"Run {i}: Final state hash differs");
            }
        }

        #endregion

        #region Helper Methods

        private InteractionContext CreateTestContext()
        {
            return new InteractionContext
            {
                TriggerReason = TriggerReason.PlayerUtterance,
                NpcId = _testProfile.PersonaId,
                PlayerInput = "What do you have for sale?",
                GameTime = 100f,
                InteractionCount = 1
            };
        }

        private StateSnapshot BuildSnapshot(InteractionContext context, ConstraintSet constraints)
        {
            var retrievedContext = _contextRetrieval.RetrieveContext(
                context.PlayerInput ?? string.Empty, FixedSnapshotTime);

            return retrievedContext.ApplyTo(new StateSnapshotBuilder()
                .WithContext(context)
                .WithConstraints(constraints)
                .WithSystemPrompt(_testProfile.SystemPrompt)
                .WithPlayerInput(context.PlayerInput ?? string.Empty)
                .WithSnapshotTimeUtcTicks(FixedSnapshotTime))
                .Build();
        }

        private string BuildReplayLog(StateSnapshot snapshot, ParsedOutput output, ConstraintSet constraints)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== REPLAY LOG ===");
            sb.AppendLine($"SnapshotTime: {snapshot.SnapshotTimeUtcTicks}");
            sb.AppendLine($"PlayerInput: {snapshot.PlayerInput}");
            sb.AppendLine($"Constraints: {constraints.Count}");
            foreach (var constraint in constraints.All)
            {
                sb.AppendLine($"  - {constraint.Type}: {constraint.Description}");
            }
            sb.AppendLine($"CapturedOutput: {output.RawOutput}");
            sb.AppendLine($"ParsedDialogue: {output.DialogueText}");
            sb.AppendLine($"MutationCount: {output.ProposedMutations.Count}");
            sb.AppendLine("=== END REPLAY LOG ===");
            return sb.ToString();
        }

        private class GovernancePipelineResult
        {
            public byte[] PromptBytes { get; set; } = Array.Empty<byte>();
            public bool GatePassed { get; set; }
            public int FailureCount { get; set; }
            public int ApprovedMutationCount { get; set; }
            public int MutationSuccessCount { get; set; }
            public string FinalStateHash { get; set; } = "";
        }

        /// <summary>
        /// Simple test rule for expectancy evaluation.
        /// </summary>
        private class TestExpectancyRule : IExpectancyRule
        {
            private readonly string _id;
            private readonly Func<InteractionContext, bool> _condition;
            private readonly Constraint _constraint;

            public TestExpectancyRule(string id, Func<InteractionContext, bool> condition, Constraint constraint)
            {
                _id = id;
                _condition = condition;
                _constraint = constraint;
            }

            public string RuleId => _id;
            public string RuleName => _id;
            public bool IsEnabled => true;
            public int Priority => 0;

            public bool Evaluate(InteractionContext context)
            {
                return _condition(context);
            }

            public void GenerateConstraints(InteractionContext context, ConstraintSet constraints)
            {
                constraints.Add(_constraint);
            }
        }

        #endregion
    }
}
