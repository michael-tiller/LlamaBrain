using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using LlamaBrain.Core.Retrieval;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;

namespace LlamaBrain.Tests.Retrieval
{
    /// <summary>
    /// Tests for RecognitionQueryService.
    /// </summary>
    public class RecognitionQueryServiceTests
    {
        private AuthoritativeMemorySystem _memorySystem = null!;
        private RecognitionQueryService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _memorySystem = new AuthoritativeMemorySystem(
                new FixedClock(1000000),
                new SequentialIdGenerator());
            _service = new RecognitionQueryService(_memorySystem);
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithNullMemorySystem_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new RecognitionQueryService(null!));
        }

        [Test]
        public void Constructor_WithOnlyMemorySystem_CreatesService()
        {
            var service = new RecognitionQueryService(_memorySystem);
            Assert.That(service, Is.Not.Null);
            Assert.That(service.IsSemanticSearchAvailable, Is.False);
        }

        [Test]
        public void Constructor_WithVectorStoreAndProvider_EnablesSemantic()
        {
            var store = new InMemoryVectorStore(4);
            var provider = new DeterministicEmbeddingProvider(4);
            var service = new RecognitionQueryService(_memorySystem, store, provider);

            Assert.That(service.IsSemanticSearchAvailable, Is.True);
        }

        #endregion

        #region Location Recognition Tests

        [Test]
        public void QueryLocationRecognition_NoMatches_ReturnsNotRecognized()
        {
            var result = _service.QueryLocationRecognition("npc-1", "castle");

            Assert.That(result.Recognized, Is.False);
            Assert.That(result.RecognitionType, Is.EqualTo(RecognitionType.None));
        }

        [Test]
        public void QueryLocationRecognition_WithMatchingMemory_ReturnsRecognized()
        {
            // Recognition requires at least 2 visits
            _memorySystem.AddEpisodicMemory(
                new EpisodicMemoryEntry("Entered the castle", EpisodeType.Event),
                MutationSource.ValidatedOutput);
            _memorySystem.AddEpisodicMemory(
                new EpisodicMemoryEntry("Returned to the castle", EpisodeType.Event),
                MutationSource.ValidatedOutput);

            var result = _service.QueryLocationRecognition("npc-1", "castle");

            Assert.That(result.Recognized, Is.True);
            Assert.That(result.RecognitionType, Is.EqualTo(RecognitionType.Location));
            Assert.That(result.RepeatCount, Is.EqualTo(2));
        }

        [Test]
        public void QueryLocationRecognition_MultipleVisits_CountsAll()
        {
            _memorySystem.AddEpisodicMemory(
                new EpisodicMemoryEntry("Arrived at tavern", EpisodeType.Event),
                MutationSource.ValidatedOutput);
            _memorySystem.AddEpisodicMemory(
                new EpisodicMemoryEntry("Visited the tavern again", EpisodeType.Event),
                MutationSource.ValidatedOutput);
            _memorySystem.AddEpisodicMemory(
                new EpisodicMemoryEntry("Returned to tavern", EpisodeType.Event),
                MutationSource.ValidatedOutput);

            var result = _service.QueryLocationRecognition("npc-1", "tavern");

            Assert.That(result.RepeatCount, Is.EqualTo(3));
        }

        [Test]
        public void QueryLocationRecognition_CaseInsensitive()
        {
            // Recognition requires at least 2 visits
            _memorySystem.AddEpisodicMemory(
                new EpisodicMemoryEntry("Entered the CASTLE", EpisodeType.Event),
                MutationSource.ValidatedOutput);
            _memorySystem.AddEpisodicMemory(
                new EpisodicMemoryEntry("Left the castle", EpisodeType.Event),
                MutationSource.ValidatedOutput);

            var result = _service.QueryLocationRecognition("npc-1", "castle");

            Assert.That(result.Recognized, Is.True);
        }

        [Test]
        public void QueryLocationRecognition_WithLocationPattern()
        {
            // Recognition requires at least 2 visits
            _memorySystem.AddEpisodicMemory(
                new EpisodicMemoryEntry("location: blacksmith_shop", EpisodeType.Event),
                MutationSource.ValidatedOutput);
            _memorySystem.AddEpisodicMemory(
                new EpisodicMemoryEntry("visited blacksmith_shop again", EpisodeType.Event),
                MutationSource.ValidatedOutput);

            var result = _service.QueryLocationRecognition("npc-1", "blacksmith_shop");

            Assert.That(result.Recognized, Is.True);
        }

        [Test]
        public void QueryLocationRecognition_WithEmptyLocationId_ReturnsNotRecognized()
        {
            var result = _service.QueryLocationRecognition("npc-1", "");

            Assert.That(result.Recognized, Is.False);
        }

        [Test]
        public void QueryLocationRecognition_ReturnsMatchedMemoryIds()
        {
            // Recognition requires at least 2 visits
            _memorySystem.AddEpisodicMemory(
                new EpisodicMemoryEntry("Visited forest", EpisodeType.Event),
                MutationSource.ValidatedOutput);
            _memorySystem.AddEpisodicMemory(
                new EpisodicMemoryEntry("Returned to forest", EpisodeType.Event),
                MutationSource.ValidatedOutput);

            var result = _service.QueryLocationRecognition("npc-1", "forest");

            Assert.That(result.MatchedMemoryIds.Count, Is.GreaterThan(0));
        }

        #endregion

        #region Topic Recognition (Keyword) Tests

        [Test]
        public void QueryTopicRecognitionKeywordOnly_NoMatches_ReturnsNotRecognized()
        {
            var result = _service.QueryTopicRecognitionKeywordOnly("npc-1", "dragons");

            Assert.That(result.Recognized, Is.False);
        }

        [Test]
        public void QueryTopicRecognitionKeywordOnly_WithMatchingMemory_ReturnsRecognized()
        {
            _memorySystem.AddDialogue("Player", "Let's talk about dragons and magic");

            var result = _service.QueryTopicRecognitionKeywordOnly("npc-1", "Tell me about dragons");

            Assert.That(result.Recognized, Is.True);
            Assert.That(result.RecognitionType, Is.EqualTo(RecognitionType.Topic));
        }

        [Test]
        public void QueryTopicRecognitionKeywordOnly_PartialKeywordMatch()
        {
            _memorySystem.AddDialogue("Player", "The weather today is quite lovely");

            var result = _service.QueryTopicRecognitionKeywordOnly("npc-1", "weather forecast");

            Assert.That(result.Recognized, Is.True);
        }

        [Test]
        public void QueryTopicRecognitionKeywordOnly_NoOverlap_ReturnsNotRecognized()
        {
            _memorySystem.AddDialogue("Player", "Hello there friend");

            var result = _service.QueryTopicRecognitionKeywordOnly("npc-1", "dragons magic swords");

            Assert.That(result.Recognized, Is.False);
        }

        [Test]
        public void QueryTopicRecognitionKeywordOnly_EmptyInput_ReturnsNotRecognized()
        {
            var result = _service.QueryTopicRecognitionKeywordOnly("npc-1", "");

            Assert.That(result.Recognized, Is.False);
        }

        #endregion

        #region Async Topic Recognition Tests

        [Test]
        public async Task QueryTopicRecognitionAsync_WithoutSemantic_FallsBackToKeyword()
        {
            // Add memory with enough overlapping keywords to exceed 50% threshold
            // AddDialogue creates "Player: Dragons are powerful creatures"
            // Query needs to share enough words with the memory content
            _memorySystem.AddDialogue("Player", "Dragons are powerful creatures");

            // Use query that shares multiple words with memory
            var result = await _service.QueryTopicRecognitionAsync("npc-1", "Dragons are powerful");

            Assert.That(result.Recognized, Is.True);
            Assert.That(result.RecognitionType, Is.EqualTo(RecognitionType.Topic));
        }

        [Test]
        public async Task QueryTopicRecognitionAsync_EmptyInput_ReturnsNotRecognized()
        {
            var result = await _service.QueryTopicRecognitionAsync("npc-1", "");

            Assert.That(result.Recognized, Is.False);
        }

        #endregion

        #region Conversation Pattern Recognition Tests

        [Test]
        public async Task QueryConversationPatternAsync_ExactRepeat_Recognized()
        {
            // Use a lenient config with lower threshold for testing
            var lenientConfig = RecognitionConfig.Lenient();
            var lenientService = new RecognitionQueryService(_memorySystem, null, null, lenientConfig);

            // AddDialogue creates "Player: What is your name?"
            _memorySystem.AddDialogue("Player", "What is your name?");

            // Query the same phrase - should be recognized since input is contained in memory
            var result = await lenientService.QueryConversationPatternAsync("npc-1", "What is your name?");

            // Should recognize the repetition
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.RecognitionType, Is.EqualTo(RecognitionType.Conversation));
        }

        [Test]
        public async Task QueryConversationPatternAsync_DifferentInput_NotRecognized()
        {
            _memorySystem.AddDialogue("Player", "Hello there");

            var result = await _service.QueryConversationPatternAsync("npc-1", "Goodbye my friend");

            Assert.That(result.Recognized, Is.False);
        }

        [Test]
        public async Task QueryConversationPatternAsync_EmptyInput_ReturnsNotRecognized()
        {
            var result = await _service.QueryConversationPatternAsync("npc-1", "");

            Assert.That(result.Recognized, Is.False);
        }

        #endregion

        #region RecognitionResult Factory Tests

        [Test]
        public void RecognitionResult_NotRecognized_CorrectDefaults()
        {
            var result = RecognitionResult.NotRecognized();

            Assert.That(result.Recognized, Is.False);
            Assert.That(result.RecognitionType, Is.EqualTo(RecognitionType.None));
            Assert.That(result.RepeatCount, Is.EqualTo(0));
            Assert.That(result.EvidenceSummary, Is.Empty);
            Assert.That(result.MatchedMemoryIds, Is.Empty);
        }

        [Test]
        public void RecognitionResult_LocationRecognized_CorrectValues()
        {
            var ids = new List<string> { "mem-1", "mem-2" };
            var result = RecognitionResult.LocationRecognized(2, 12345, ids);

            Assert.That(result.Recognized, Is.True);
            Assert.That(result.RecognitionType, Is.EqualTo(RecognitionType.Location));
            Assert.That(result.RepeatCount, Is.EqualTo(2));
            Assert.That(result.LastOccurrenceTicks, Is.EqualTo(12345));
            Assert.That(result.MatchedMemoryIds.Count, Is.EqualTo(2));
            Assert.That(result.BestMatchSimilarity, Is.EqualTo(1.0f));
        }

        [Test]
        public void RecognitionResult_TopicRecognized_CorrectValues()
        {
            var ids = new List<string> { "mem-1" };
            var result = RecognitionResult.TopicRecognized(1, 9999, ids, 0.85f, "weather");

            Assert.That(result.Recognized, Is.True);
            Assert.That(result.RecognitionType, Is.EqualTo(RecognitionType.Topic));
            Assert.That(result.BestMatchSimilarity, Is.EqualTo(0.85f));
            Assert.That(result.EvidenceSummary, Does.Contain("weather"));
        }

        [Test]
        public void RecognitionResult_ConversationRecognized_CorrectValues()
        {
            var ids = new List<string> { "mem-1" };
            var result = RecognitionResult.ConversationRecognized(3, 5555, ids, 0.95f, "greeting");

            Assert.That(result.Recognized, Is.True);
            Assert.That(result.RecognitionType, Is.EqualTo(RecognitionType.Conversation));
            Assert.That(result.RepeatCount, Is.EqualTo(3));
        }

        #endregion

        #region RecognitionConfig Tests

        [Test]
        public void RecognitionConfig_Default_HasReasonableValues()
        {
            var config = RecognitionConfig.Default();

            Assert.That(config.TopicSimilarityThreshold, Is.InRange(0.5f, 0.8f));
            Assert.That(config.ConversationSimilarityThreshold, Is.InRange(0.6f, 0.9f));
            Assert.That(config.EnableKeywordFallback, Is.True);
        }

        [Test]
        public void RecognitionConfig_Strict_HigherThresholds()
        {
            var strict = RecognitionConfig.Strict();
            var defaultConfig = RecognitionConfig.Default();

            Assert.That(strict.TopicSimilarityThreshold, Is.GreaterThan(defaultConfig.TopicSimilarityThreshold));
            Assert.That(strict.ConversationSimilarityThreshold, Is.GreaterThan(defaultConfig.ConversationSimilarityThreshold));
        }

        [Test]
        public void RecognitionConfig_Lenient_LowerThresholds()
        {
            var lenient = RecognitionConfig.Lenient();
            var defaultConfig = RecognitionConfig.Default();

            Assert.That(lenient.TopicSimilarityThreshold, Is.LessThan(defaultConfig.TopicSimilarityThreshold));
        }

        #endregion

        #region Determinism Tests

        [Test]
        public void QueryLocationRecognition_SameInput_ProducesIdenticalResults()
        {
            _memorySystem.AddEpisodicMemory(
                new EpisodicMemoryEntry("Visited castle", EpisodeType.Event),
                MutationSource.ValidatedOutput);

            var result1 = _service.QueryLocationRecognition("npc-1", "castle");
            var result2 = _service.QueryLocationRecognition("npc-1", "castle");
            var result3 = _service.QueryLocationRecognition("npc-1", "castle");

            Assert.That(result1.Recognized, Is.EqualTo(result2.Recognized));
            Assert.That(result2.Recognized, Is.EqualTo(result3.Recognized));
            Assert.That(result1.RepeatCount, Is.EqualTo(result2.RepeatCount));
            Assert.That(result1.MatchedMemoryIds, Is.EqualTo(result2.MatchedMemoryIds));
        }

        [Test]
        public void QueryTopicRecognitionKeywordOnly_SameInput_ProducesIdenticalResults()
        {
            _memorySystem.AddDialogue("Player", "Dragons are dangerous creatures");

            var result1 = _service.QueryTopicRecognitionKeywordOnly("npc-1", "dragons");
            var result2 = _service.QueryTopicRecognitionKeywordOnly("npc-1", "dragons");

            Assert.That(result1.Recognized, Is.EqualTo(result2.Recognized));
            Assert.That(result1.BestMatchSimilarity, Is.EqualTo(result2.BestMatchSimilarity));
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Fixed clock for deterministic testing.
        /// </summary>
        private class FixedClock : IClock
        {
            private long _ticks;

            public FixedClock(long initialTicks)
            {
                _ticks = initialTicks;
            }

            public long UtcNowTicks
            {
                get
                {
                    _ticks += 1000; // Advance time slightly each call
                    return _ticks;
                }
            }
        }

        /// <summary>
        /// Sequential ID generator for deterministic testing.
        /// </summary>
        private class SequentialIdGenerator : IIdGenerator
        {
            private int _counter = 0;

            public string GenerateId()
            {
                return $"id-{_counter++:D8}";
            }
        }

        /// <summary>
        /// Deterministic embedding provider for testing.
        /// </summary>
        private class DeterministicEmbeddingProvider : IEmbeddingProvider
        {
            public int EmbeddingDimension { get; }
            public bool IsAvailable => true;

            public DeterministicEmbeddingProvider(int dimension)
            {
                EmbeddingDimension = dimension;
            }

            public Task<float[]?> GenerateEmbeddingAsync(string text, System.Threading.CancellationToken cancellationToken = default)
            {
                // Generate deterministic embedding using stable hash (FNV-1a)
                var hash = StableStringHash(text);
                var embedding = new float[EmbeddingDimension];
                for (int i = 0; i < EmbeddingDimension; i++)
                {
                    embedding[i] = (float)Math.Sin(hash + i * 0.1);
                }
                return Task.FromResult<float[]?>(embedding);
            }

            public async Task<float[]?[]> GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts, System.Threading.CancellationToken cancellationToken = default)
            {
                var tasks = new Task<float[]?>[texts.Count];
                for (int i = 0; i < texts.Count; i++)
                {
                    tasks[i] = GenerateEmbeddingAsync(texts[i], cancellationToken);
                }
                return await Task.WhenAll(tasks);
            }

            /// <summary>
            /// FNV-1a hash - deterministic across .NET versions and runs.
            /// </summary>
            private static uint StableStringHash(string text)
            {
                const uint fnvPrime = 16777619;
                const uint offsetBasis = 2166136261;

                uint hash = offsetBasis;
                foreach (char c in text)
                {
                    hash ^= c;
                    hash *= fnvPrime;
                }
                return hash;
            }
        }

        #endregion
    }
}
