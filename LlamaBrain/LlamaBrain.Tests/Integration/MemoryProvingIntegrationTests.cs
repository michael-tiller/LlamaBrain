using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LlamaBrain.Core.Expectancy;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Retrieval;
using LlamaBrain.Core.Validation;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;

namespace LlamaBrain.Tests.Integration
{
    /// <summary>
    /// Memory Proving Integration Tests
    ///
    /// These tests prove that RAG retrieval influences generation through the full pipeline:
    ///   Memory → Recognition → Prompt Injection → (Mock LLM) → Validation
    ///
    /// Uses production code for all components except LLM output (which is simulated).
    /// This demonstrates that recognition queries correctly identify repetition,
    /// prompt assembly injects appropriate RECOGNITION blocks, and validation
    /// confirms recognition cues in output.
    /// </summary>
    [TestFixture]
    [Category("RAG")]
    [Category("Integration")]
    [Category("MemoryProving")]
    public class MemoryProvingIntegrationTests
    {
        private AuthoritativeMemorySystem _memorySystem = null!;
        private RecognitionQueryService _recognitionService = null!;
        private PromptAssembler _promptAssembler = null!;
        private ContextRetrievalLayer _contextRetrieval = null!;

        // Fixed time for determinism
        private const long FixedSnapshotTime = 638400000000000000L; // 2024-01-01 00:00:00 UTC

        [SetUp]
        public void SetUp()
        {
            // Use deterministic providers
            var clock = new AdvancingClock(FixedSnapshotTime, incrementTicks: TimeSpan.FromMinutes(1).Ticks);
            var idGen = new SequentialIdGenerator(1);

            // Production memory system
            _memorySystem = new AuthoritativeMemorySystem(clock, idGen);

            // Production recognition service (keyword-only for unit tests, no embedding server needed)
            _recognitionService = new RecognitionQueryService(
                _memorySystem,
                vectorStore: null,
                embeddingProvider: null);

            // Production prompt assembler
            _promptAssembler = new PromptAssembler(PromptAssemblerConfig.Default);

            // Production context retrieval
            _contextRetrieval = new ContextRetrievalLayer(_memorySystem);
        }

        #region Location Recognition → Prompt Injection → Validation

        [Test]
        public void LocationRecognition_FullFlow_FirstVisit_NoRecognitionBlock()
        {
            // Arrange: First visit to a location
            var locationEntry = EpisodicMemoryEntry.FromLocationEntry(
                locationId: "dark_tunnel",
                description: "Entered the Dark Tunnel");
            _memorySystem.AddEpisodicMemory(locationEntry, MutationSource.GameSystem);

            // Act: Query for recognition (first time = no recognition)
            var recognition = _recognitionService.QueryLocationRecognition(
                npcId: _memorySystem.NpcId,
                locationId: "dark_tunnel");

            // Assert: First visit should not be recognized
            Assert.That(recognition.Recognized, Is.False);
            Assert.That(recognition.RepeatCount, Is.EqualTo(0));

            // Verify prompt has no recognition block
            var snapshot = BuildTestSnapshot("What is this place?");
            var prompt = _promptAssembler.AssembleFromWorkingMemory(
                new EphemeralWorkingMemory(snapshot, new WorkingMemoryConfig()),
                recognition: recognition);

            Assert.That(prompt.Text, Does.Not.Contain("<RECOGNITION"));
            Assert.That(prompt.Recognition, Is.Not.Null);
            Assert.That(prompt.Recognition!.Recognized, Is.False);
        }

        [Test]
        public void LocationRecognition_FullFlow_SecondVisit_RecognitionBlockInjected()
        {
            // Arrange: Two visits to the same location
            var firstVisit = EpisodicMemoryEntry.FromLocationEntry(
                locationId: "dark_tunnel",
                description: "Entered the Dark Tunnel");
            _memorySystem.AddEpisodicMemory(firstVisit, MutationSource.GameSystem);

            var secondVisit = EpisodicMemoryEntry.FromLocationEntry(
                locationId: "dark_tunnel",
                description: "Entered the Dark Tunnel again");
            _memorySystem.AddEpisodicMemory(secondVisit, MutationSource.GameSystem);

            // Act: Query for recognition (second time = recognized)
            var recognition = _recognitionService.QueryLocationRecognition(
                npcId: _memorySystem.NpcId,
                locationId: "dark_tunnel");

            // Assert: Second visit should be recognized
            Assert.That(recognition.Recognized, Is.True);
            Assert.That(recognition.RecognitionType, Is.EqualTo(RecognitionType.Location));
            Assert.That(recognition.RepeatCount, Is.GreaterThanOrEqualTo(1));

            // Verify prompt contains recognition block
            var snapshot = BuildTestSnapshot("What is this place?");
            var prompt = _promptAssembler.AssembleFromWorkingMemory(
                new EphemeralWorkingMemory(snapshot, new WorkingMemoryConfig()),
                recognition: recognition);

            Assert.That(prompt.Text, Does.Contain("<RECOGNITION"));
            Assert.That(prompt.Text, Does.Contain("type=\"location\""));
            Assert.That(prompt.Text, Does.Contain("repeat_count="));
            Assert.That(prompt.Recognition, Is.Not.Null);
            Assert.That(prompt.Recognition!.Recognized, Is.True);
            Assert.That(prompt.Breakdown.Recognition, Is.GreaterThan(0));
        }

        [Test]
        public void LocationRecognition_FullFlow_OutputWithCue_ValidationPasses()
        {
            // Arrange: Multiple visits
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromLocationEntry("dark_tunnel", "Entered the Dark Tunnel"),
                MutationSource.GameSystem);
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromLocationEntry("dark_tunnel", "Entered the Dark Tunnel"),
                MutationSource.GameSystem);

            var recognition = _recognitionService.QueryLocationRecognition(
                npcId: _memorySystem.NpcId,
                locationId: "dark_tunnel");

            // Act: Simulate LLM output WITH recognition cue
            var llmOutput = "Ah, I remember this tunnel! We've been here before. The darkness feels familiar.";

            // Assert: Validation should find the cue
            var validationResult = RecognitionCueValidator.Validate(llmOutput, recognition);

            Assert.That(validationResult.CueExpected, Is.True);
            Assert.That(validationResult.CueFound, Is.True);
            Assert.That(validationResult.MatchedCue, Is.Not.Null);
            Assert.That(validationResult.Warning, Is.Null);
        }

        [Test]
        public void LocationRecognition_FullFlow_OutputWithoutCue_ValidationWarns()
        {
            // Arrange: Multiple visits
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromLocationEntry("dark_tunnel", "Entered the Dark Tunnel"),
                MutationSource.GameSystem);
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromLocationEntry("dark_tunnel", "Entered the Dark Tunnel"),
                MutationSource.GameSystem);

            var recognition = _recognitionService.QueryLocationRecognition(
                npcId: _memorySystem.NpcId,
                locationId: "dark_tunnel");

            // Act: Simulate LLM output WITHOUT recognition cue
            var llmOutput = "This is a dark tunnel. The air is damp and cold.";

            // Assert: Validation should warn about missing cue
            var validationResult = RecognitionCueValidator.Validate(llmOutput, recognition);

            Assert.That(validationResult.CueExpected, Is.True);
            Assert.That(validationResult.CueFound, Is.False);
            Assert.That(validationResult.Warning, Is.Not.Null);
            Assert.That(validationResult.Warning, Does.Contain("Location"));
        }

        #endregion

        #region Topic Recognition → Prompt Injection → Validation

        [Test]
        public void TopicRecognition_FullFlow_RepeatedTopic_RecognitionBlockInjected()
        {
            // Arrange: Multiple discussions about dragons
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromDialogue("Player", "Tell me about dragons", significance: 0.6f),
                MutationSource.ValidatedOutput);
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromDialogue("NPC", "Dragons are fearsome creatures", significance: 0.5f),
                MutationSource.ValidatedOutput);
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromDialogue("Player", "What else about dragons?", significance: 0.6f),
                MutationSource.ValidatedOutput);
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromDialogue("Player", "More dragon information please", significance: 0.6f),
                MutationSource.ValidatedOutput);

            // Act: Query for topic recognition (keyword-based since no embedding server)
            // Note: In production, this would use semantic search if available
            var recognition = _recognitionService.QueryTopicRecognitionKeywordOnly(
                npcId: _memorySystem.NpcId,
                playerInput: "Tell me more about dragons");

            // Assert: Topic should be recognized due to repeated mentions
            Assert.That(recognition.Recognized, Is.True);
            Assert.That(recognition.RecognitionType, Is.EqualTo(RecognitionType.Topic));

            // Verify prompt contains recognition block
            var snapshot = BuildTestSnapshot("Tell me more about dragons");
            var prompt = _promptAssembler.AssembleFromWorkingMemory(
                new EphemeralWorkingMemory(snapshot, new WorkingMemoryConfig()),
                recognition: recognition);

            Assert.That(prompt.Text, Does.Contain("<RECOGNITION"));
            Assert.That(prompt.Text, Does.Contain("type=\"topic\""));
        }

        [Test]
        public void TopicRecognition_FullFlow_OutputWithCue_ValidationPasses()
        {
            // Arrange: Repeated topic
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromDialogue("Player", "Tell me about dragons", significance: 0.6f),
                MutationSource.ValidatedOutput);
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromDialogue("Player", "More about dragons please", significance: 0.6f),
                MutationSource.ValidatedOutput);

            var recognition = _recognitionService.QueryTopicRecognitionKeywordOnly(
                npcId: _memorySystem.NpcId,
                playerInput: "Dragons again!");

            // Act: Simulate LLM output WITH topic cue
            var llmOutput = "We've already discussed dragons at length. As I mentioned before, they breathe fire.";

            // Assert: Validation should find the cue
            var validationResult = RecognitionCueValidator.Validate(llmOutput, recognition);

            Assert.That(validationResult.CueExpected, Is.True);
            Assert.That(validationResult.CueFound, Is.True);
        }

        #endregion

        #region Determinism Proofs

        [Test]
        public void Recognition_SameMemories_SameQuery_IdenticalResult()
        {
            // Arrange: Fixed memory state
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromLocationEntry("market_square", "Arrived at the Market Square"),
                MutationSource.GameSystem);
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromLocationEntry("market_square", "Returned to Market Square"),
                MutationSource.GameSystem);

            // Act: Query multiple times
            var result1 = _recognitionService.QueryLocationRecognition(_memorySystem.NpcId, "market_square");
            var result2 = _recognitionService.QueryLocationRecognition(_memorySystem.NpcId, "market_square");
            var result3 = _recognitionService.QueryLocationRecognition(_memorySystem.NpcId, "market_square");

            // Assert: Results are identical
            Assert.That(result1.Recognized, Is.EqualTo(result2.Recognized));
            Assert.That(result1.Recognized, Is.EqualTo(result3.Recognized));
            Assert.That(result1.RepeatCount, Is.EqualTo(result2.RepeatCount));
            Assert.That(result1.RepeatCount, Is.EqualTo(result3.RepeatCount));
            Assert.That(result1.RecognitionType, Is.EqualTo(result2.RecognitionType));
            Assert.That(result1.RecognitionType, Is.EqualTo(result3.RecognitionType));
        }

        [Test]
        public void PromptAssembly_SameRecognition_IdenticalBlock()
        {
            // Arrange: Fixed recognition
            var recognition = RecognitionResult.LocationRecognized(
                repeatCount: 3,
                lastVisitTicks: FixedSnapshotTime,
                matchedMemoryIds: new[] { "mem_001", "mem_002", "mem_003" });

            var snapshot = BuildTestSnapshot("Hello");

            // Act: Assemble multiple times
            var prompt1 = _promptAssembler.AssembleFromWorkingMemory(
                new EphemeralWorkingMemory(snapshot, new WorkingMemoryConfig()),
                recognition: recognition);
            var prompt2 = _promptAssembler.AssembleFromWorkingMemory(
                new EphemeralWorkingMemory(snapshot, new WorkingMemoryConfig()),
                recognition: recognition);

            // Assert: Recognition blocks are identical
            var block1 = ExtractRecognitionBlock(prompt1.Text);
            var block2 = ExtractRecognitionBlock(prompt2.Text);

            Assert.That(block1, Is.EqualTo(block2));
            Assert.That(prompt1.Breakdown.Recognition, Is.EqualTo(prompt2.Breakdown.Recognition));
        }

        [Test]
        public void Validation_SameOutput_SameRecognition_IdenticalResult()
        {
            // Arrange: Fixed inputs
            var recognition = RecognitionResult.LocationRecognized(
                repeatCount: 2,
                lastVisitTicks: FixedSnapshotTime,
                matchedMemoryIds: new[] { "mem_001" });
            var llmOutput = "Ah yes, I remember this place well.";

            // Act: Validate multiple times
            var result1 = RecognitionCueValidator.Validate(llmOutput, recognition);
            var result2 = RecognitionCueValidator.Validate(llmOutput, recognition);
            var result3 = RecognitionCueValidator.Validate(llmOutput, recognition);

            // Assert: Results are identical
            Assert.That(result1.CueFound, Is.EqualTo(result2.CueFound));
            Assert.That(result1.CueFound, Is.EqualTo(result3.CueFound));
            Assert.That(result1.MatchedCue, Is.EqualTo(result2.MatchedCue));
            Assert.That(result1.MatchedCue, Is.EqualTo(result3.MatchedCue));
        }

        #endregion

        #region End-to-End Pipeline

        [Test]
        public void EndToEnd_LocationRecognition_FullPipeline()
        {
            // === STEP 1: Memory Formation ===
            // Player enters the Dark Tunnel multiple times
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromLocationEntry("dark_tunnel", "Player entered the Dark Tunnel"),
                MutationSource.GameSystem);
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromDialogue("NPC", "Be careful in these tunnels", significance: 0.5f),
                MutationSource.ValidatedOutput);
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromLocationEntry("dark_tunnel", "Player returned to the Dark Tunnel"),
                MutationSource.GameSystem);

            // === STEP 2: Recognition Query ===
            var recognition = _recognitionService.QueryLocationRecognition(
                npcId: _memorySystem.NpcId,
                locationId: "dark_tunnel");

            Assert.That(recognition.Recognized, Is.True, "Should recognize repeated location");
            Assert.That(recognition.RecognitionType, Is.EqualTo(RecognitionType.Location));

            // === STEP 3: Prompt Assembly with Recognition ===
            var snapshot = BuildTestSnapshot("What is this place?");
            var prompt = _promptAssembler.AssembleFromWorkingMemory(
                new EphemeralWorkingMemory(snapshot, new WorkingMemoryConfig()),
                recognition: recognition);

            Assert.That(prompt.Text, Does.Contain("<RECOGNITION"), "Prompt should contain recognition block");
            Assert.That(prompt.Text, Does.Contain("type=\"location\""));
            Assert.That(prompt.Text, Does.Contain("Acknowledge"));

            // === STEP 4: Simulated LLM Response (would come from actual LLM in production) ===
            var llmResponse = "Ah, the Dark Tunnel... I remember warning you about this place before. " +
                              "The dangers haven't changed since your last visit.";

            // === STEP 5: Validation ===
            var validation = RecognitionCueValidator.Validate(llmResponse, recognition);

            Assert.That(validation.CueFound, Is.True, "Output should contain recognition cue");
            Assert.That(validation.Warning, Is.Null, "No warning when cue is present");

            // === PROOF: The pipeline proves retrieval influences generation ===
            // - Memory contains multiple visits to "dark_tunnel"
            // - RecognitionQueryService detects the repetition
            // - PromptAssembler injects RECOGNITION block constraining the LLM
            // - LLM (simulated) produces output acknowledging familiarity
            // - RecognitionCueValidator confirms the cue is present
        }

        [Test]
        public void EndToEnd_TopicRecognition_FullPipeline()
        {
            // === STEP 1: Memory Formation ===
            // Player asks about dragons repeatedly
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromDialogue("Player", "Tell me about dragons", significance: 0.6f),
                MutationSource.ValidatedOutput);
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromDialogue("NPC", "Dragons are ancient creatures of fire", significance: 0.5f),
                MutationSource.ValidatedOutput);
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromDialogue("Player", "What else do you know about dragons?", significance: 0.6f),
                MutationSource.ValidatedOutput);
            _memorySystem.AddEpisodicMemory(
                EpisodicMemoryEntry.FromDialogue("Player", "I want to hear more about dragons", significance: 0.6f),
                MutationSource.ValidatedOutput);

            // === STEP 2: Recognition Query ===
            var recognition = _recognitionService.QueryTopicRecognitionKeywordOnly(
                npcId: _memorySystem.NpcId,
                playerInput: "Dragons dragons dragons!");

            Assert.That(recognition.Recognized, Is.True, "Should recognize repeated topic");
            Assert.That(recognition.RecognitionType, Is.EqualTo(RecognitionType.Topic));

            // === STEP 3: Prompt Assembly with Recognition ===
            var snapshot = BuildTestSnapshot("Tell me everything about dragons!");
            var prompt = _promptAssembler.AssembleFromWorkingMemory(
                new EphemeralWorkingMemory(snapshot, new WorkingMemoryConfig()),
                recognition: recognition);

            Assert.That(prompt.Text, Does.Contain("<RECOGNITION"), "Prompt should contain recognition block");
            Assert.That(prompt.Text, Does.Contain("type=\"topic\""));

            // === STEP 4: Simulated LLM Response ===
            var llmResponse = "You've mentioned dragons several times now. As I've told you before, " +
                              "they are dangerous creatures. Perhaps we could discuss something else?";

            // === STEP 5: Validation ===
            var validation = RecognitionCueValidator.Validate(llmResponse, recognition);

            Assert.That(validation.CueFound, Is.True, "Output should contain recognition cue");
        }

        #endregion

        #region Helper Methods

        private StateSnapshot BuildTestSnapshot(string playerInput)
        {
            var context = new InteractionContext
            {
                TriggerReason = TriggerReason.PlayerUtterance,
                NpcId = _memorySystem.NpcId ?? "test_npc",
                PlayerInput = playerInput,
                GameTime = 0f
            };

            var constraints = new ConstraintSet();
            var retrievedContext = _contextRetrieval.RetrieveContext(playerInput, FixedSnapshotTime);

            return new StateSnapshotBuilder()
                .WithContext(context)
                .WithConstraints(constraints)
                .WithSystemPrompt("You are a helpful NPC in a fantasy game.")
                .WithPlayerInput(playerInput)
                .WithCanonicalFacts(retrievedContext.CanonicalFacts.ToArray())
                .WithWorldState(retrievedContext.WorldState.ToArray())
                .WithEpisodicMemories(retrievedContext.EpisodicMemories.ToArray())
                .WithBeliefs(retrievedContext.Beliefs.ToArray())
                .Build();
        }

        private static string? ExtractRecognitionBlock(string promptText)
        {
            const string startTag = "<RECOGNITION";
            const string endTag = "</RECOGNITION>";

            var startIndex = promptText.IndexOf(startTag, StringComparison.Ordinal);
            if (startIndex < 0) return null;

            var endIndex = promptText.IndexOf(endTag, startIndex, StringComparison.Ordinal);
            if (endIndex < 0) return null;

            return promptText.Substring(startIndex, endIndex - startIndex + endTag.Length);
        }

        #endregion
    }
}
