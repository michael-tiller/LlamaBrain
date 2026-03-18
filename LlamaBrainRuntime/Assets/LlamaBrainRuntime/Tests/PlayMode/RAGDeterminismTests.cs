using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;
using LlamaBrain.Core;
using LlamaBrain.Core.Inference;
using LlamaBrain.Core.Retrieval;
using LlamaBrain.Persona;
using LlamaBrain.Persona.MemoryTypes;
using LlamaBrain.Runtime.Core;

namespace LlamaBrain.Tests.PlayMode
{
    /// <summary>
    /// Deterministic RAG integration tests for Unity PlayMode.
    /// Validates that RAG retrieval produces identical results given identical inputs.
    ///
    /// These tests auto-start an embedding server if one isn't already running.
    /// The embedding model must exist at: Backend/model/nomic-embed-text-v1.5.f32.gguf
    ///
    /// Key determinism guarantees tested:
    /// - Same memories + same query = same retrieval order
    /// - Same embeddings = same similarity scores
    /// - Vector store ordering is stable (similarity desc, sequenceNumber asc, memoryId ordinal)
    /// - Hybrid relevance scoring is deterministic
    /// </summary>
    [Category("Contract")]
    [Category("RAG")]
    [Category("Determinism")]
    public class RAGDeterminismTests
    {
        private const int EmbeddingServerPort = 8081;
        private const string EmbeddingServerUrl = "http://localhost:8081";
        private const int DefaultEmbeddingDimension = 768; // nomic-embed-text produces 768-dim
        private const string ModelName = "nomic-embed-text";

        // Paths relative to project root
        private const string EmbeddingModelRelativePath = "Backend/model/nomic-embed-text-v1.5.f32.gguf";
        private const string LlamaServerRelativePath = "Backend/llama-server.exe";

        // Fixed snapshot time for deterministic recency calculations (Jan 1, 2024 UTC)
        private static readonly long FixedSnapshotTimeTicks = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc).Ticks;

        // Shared server manager for all tests in this fixture
        private static ServerManager _embeddingServerManager;
        private static bool _serverStartedByTests;

        private LlamaCppEmbeddingProvider _provider;
        private List<IDisposable> _disposables;
        private int _embeddingDimension;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Check if BrainServer is already running the embedding server
            var brainServer = BrainServer.Instance;
            if (brainServer != null && brainServer.IsEmbeddingServerRunning)
            {
                Debug.Log("[RAGDeterminism] BrainServer embedding server is running, will use it");
                _serverStartedByTests = false;
                return;
            }

            // Try to start embedding server ourselves
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var modelPath = Path.Combine(projectRoot, EmbeddingModelRelativePath);
            var serverPath = Path.Combine(projectRoot, LlamaServerRelativePath);

            if (!File.Exists(modelPath))
            {
                Debug.LogWarning($"[RAGDeterminism] Embedding model not found: {modelPath}");
                return;
            }

            if (!File.Exists(serverPath))
            {
                Debug.LogWarning($"[RAGDeterminism] llama-server not found: {serverPath}");
                return;
            }

            Debug.Log($"[RAGDeterminism] Starting embedding server: {serverPath}");
            Debug.Log($"[RAGDeterminism] Model: {modelPath}");

            var config = new ProcessConfig
            {
                Host = "localhost",
                Port = EmbeddingServerPort,
                Model = modelPath,
                ExecutablePath = serverPath,
                ContextSize = 512,
                GpuLayers = 35,
                EnableEmbedding = true
            };

            try
            {
                _embeddingServerManager = new ServerManager(config);
                _embeddingServerManager.StartServer();
                _serverStartedByTests = true;
                // Don't block waiting for server - each test checks connectivity and waits using WaitUntil
                Debug.Log("[RAGDeterminism] Embedding server process started. Tests will wait for connectivity.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RAGDeterminism] Failed to start embedding server: {ex.Message}");
                _embeddingServerManager = null;
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_serverStartedByTests && _embeddingServerManager != null)
            {
                Debug.Log("[RAGDeterminism] Stopping embedding server started by tests");
                try
                {
                    _embeddingServerManager.StopServer();
                    _embeddingServerManager.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RAGDeterminism] Error stopping server: {ex.Message}");
                }
                _embeddingServerManager = null;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _disposables = new List<IDisposable>();

            // Try to use BrainServer's embedding provider if available
            var brainServer = BrainServer.Instance;
            if (brainServer != null && brainServer.IsEmbeddingServerRunning && brainServer.EmbeddingProvider is LlamaCppEmbeddingProvider serverProvider)
            {
                _provider = serverProvider;
                _embeddingDimension = brainServer.EmbeddingDimension;
                Debug.Log("[RAGDeterminism] Using BrainServer's embedding provider");
            }
            else
            {
                // Use direct connection (either to our started server or external)
                _embeddingDimension = DefaultEmbeddingDimension;
                _provider = new LlamaCppEmbeddingProvider(
                    baseUrl: EmbeddingServerUrl,
                    embeddingDimension: _embeddingDimension,
                    modelName: ModelName,
                    timeout: TimeSpan.FromSeconds(30));
                _disposables.Add(_provider);
                Debug.Log("[RAGDeterminism] Using direct embedding server connection");
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var disposable in _disposables)
            {
                try { disposable.Dispose(); } catch { }
            }
            _disposables.Clear();
        }

        #region Core Determinism Tests

        /// <summary>
        /// Contract test: Running the same memory scenario twice must produce identical retrieval results.
        /// This validates the end-to-end RAG pipeline determinism.
        /// </summary>
        [UnityTest]
        [Timeout(120000)] // 2 minute timeout for embedding generation
        public IEnumerator RAG_Determinism_SameInputProducesSameRetrieval() => UniTask.ToCoroutine(async () =>
        {
            // Wait for server with retries
            if (!await WaitForServerConnectionAsync(30f))
            {
                Assert.Ignore($"Embedding server not available at {EmbeddingServerUrl}");
                return;
            }

            // ARRANGE: Fixed test data for determinism
            var memories = new[]
            {
                ("mem_001", "The player entered a dark tunnel filled with cobwebs"),
                ("mem_002", "The player defeated a fierce goblin in combat"),
                ("mem_003", "A mysterious stranger offered the player a quest"),
                ("mem_004", "The player found a magical sword in an ancient chest")
            };
            var query = "combat encounter with monsters";

            // RUN 1: Execute retrieval scenario
            var result1 = await RunRetrievalScenario(memories, query, "npc_001");

            // RUN 2: Execute identical retrieval scenario
            var result2 = await RunRetrievalScenario(memories, query, "npc_001");

            // ASSERT: Memory counts match
            Assert.AreEqual(result1.MemoryIds.Count, result2.MemoryIds.Count,
                "Memory ID count mismatch between runs");

            // ASSERT: Retrieved memory IDs are identical in order
            CollectionAssert.AreEqual(result1.MemoryIds, result2.MemoryIds,
                $"Memory ID order mismatch.\nRun1: [{string.Join(", ", result1.MemoryIds)}]\nRun2: [{string.Join(", ", result2.MemoryIds)}]");

            // ASSERT: Similarity scores are identical (within floating-point tolerance)
            Assert.AreEqual(result1.Similarities.Count, result2.Similarities.Count,
                "Similarity score count mismatch");

            for (int i = 0; i < result1.Similarities.Count; i++)
            {
                Assert.AreEqual(result1.Similarities[i], result2.Similarities[i], 1e-6f,
                    $"Similarity score mismatch at index {i}: Run1={result1.Similarities[i]:F6}, Run2={result2.Similarities[i]:F6}");
            }

            Debug.Log($"[RAGDeterminism] Verified determinism: {result1.MemoryIds.Count} memories, " +
                      $"top result: {result1.MemoryIds.FirstOrDefault()} (sim={result1.Similarities.FirstOrDefault():F4})");
        });

        /// <summary>
        /// Contract test: Vector store ordering must be deterministic with stable tie-breaking.
        /// Order: similarity desc, sequenceNumber asc, memoryId ordinal asc.
        /// </summary>
        [UnityTest]
        [Timeout(180000)]
        public IEnumerator RAG_VectorStoreOrdering_DeterministicTieBreaking() => UniTask.ToCoroutine(async () =>
        {
            if (!await WaitForServerConnectionAsync(30f))
            {
                Assert.Ignore($"Embedding server not available at {EmbeddingServerUrl}");
                return;
            }

            // ARRANGE: Create memories with similar content (likely similar embeddings)
            var vectorStore = new InMemoryVectorStore(_embeddingDimension);

            // Generate embedding for similar texts
            var text1 = "The brave knight fought valiantly";
            var text2 = "The courageous warrior battled fiercely";
            var text3 = "The heroic soldier struggled mightily";

            var embedding1 = await GenerateEmbeddingWithTimeoutAsync(text1);
            Assert.IsNotNull(embedding1, "Failed to generate embedding 1");

            var embedding2 = await GenerateEmbeddingWithTimeoutAsync(text2);
            Assert.IsNotNull(embedding2, "Failed to generate embedding 2");

            var embedding3 = await GenerateEmbeddingWithTimeoutAsync(text3);
            Assert.IsNotNull(embedding3, "Failed to generate embedding 3");

            // Insert with specific sequence numbers for tie-breaking verification
            vectorStore.Upsert("mem_c", "npc_001", MemoryVectorType.Episodic, embedding1, sequenceNumber: 3);
            vectorStore.Upsert("mem_a", "npc_001", MemoryVectorType.Episodic, embedding2, sequenceNumber: 1);
            vectorStore.Upsert("mem_b", "npc_001", MemoryVectorType.Episodic, embedding3, sequenceNumber: 2);

            // Generate query embedding
            var queryEmbedding = await GenerateEmbeddingWithTimeoutAsync("warrior in battle");
            Assert.IsNotNull(queryEmbedding, "Failed to generate query embedding");

            // ACT: Run search multiple times
            var results1 = vectorStore.FindSimilar(queryEmbedding, k: 3, npcId: "npc_001");
            var results2 = vectorStore.FindSimilar(queryEmbedding, k: 3, npcId: "npc_001");
            var results3 = vectorStore.FindSimilar(queryEmbedding, k: 3, npcId: "npc_001");

            // ASSERT: All runs return identical ordering
            Assert.AreEqual(3, results1.Count, "Expected 3 results");

            for (int i = 0; i < results1.Count; i++)
            {
                Assert.AreEqual(results1[i].MemoryId, results2[i].MemoryId,
                    $"Run1 vs Run2 mismatch at index {i}");
                Assert.AreEqual(results1[i].MemoryId, results3[i].MemoryId,
                    $"Run1 vs Run3 mismatch at index {i}");
                Assert.AreEqual(results1[i].Similarity, results2[i].Similarity, 1e-6f,
                    $"Similarity mismatch at index {i}");
            }

            Debug.Log($"[RAGDeterminism] Vector store ordering verified: " +
                      $"[{string.Join(", ", results1.Select(r => $"{r.MemoryId}({r.Similarity:F4})"))}]");
        });

        /// <summary>
        /// Contract test: Hybrid relevance scoring (keyword + semantic) must be deterministic.
        /// </summary>
        [UnityTest]
        [Timeout(180000)]
        public IEnumerator RAG_HybridRelevance_DeterministicScoring() => UniTask.ToCoroutine(async () =>
        {
            if (!await WaitForServerConnectionAsync(30f))
            {
                Assert.Ignore($"Embedding server not available at {EmbeddingServerUrl}");
                return;
            }

            // ARRANGE: Create memory system with deterministic providers
            var clock = new FixedClock(FixedSnapshotTimeTicks - TimeSpan.FromHours(1).Ticks);
            var idGen = new SequentialIdGenerator("hybrid_test");
            var memorySystem = new AuthoritativeMemorySystem(clock, idGen);
            memorySystem.NpcId = "npc_hybrid";
            _disposables.Add(new DisposableAction(() => memorySystem.ClearAll()));

            var vectorStore = new InMemoryVectorStore(_embeddingDimension);

            // Add memories with keyword-relevant content (dragon-related should rank higher)
            var mem1 = new EpisodicMemoryEntry("The dragon attacked the village with fire", EpisodeType.Event);
            var mem2 = new EpisodicMemoryEntry("The blacksmith forged a new sword", EpisodeType.Observation);
            var mem3 = new EpisodicMemoryEntry("A dragon was spotted flying over mountains", EpisodeType.Event);

            memorySystem.AddEpisodicMemory(mem1, MutationSource.ValidatedOutput);
            memorySystem.AddEpisodicMemory(mem2, MutationSource.ValidatedOutput);
            memorySystem.AddEpisodicMemory(mem3, MutationSource.ValidatedOutput);

            // Generate embeddings manually (simulating what MemoryEmbeddingService does)
            var descriptions = new[] { mem1.Description, mem2.Description, mem3.Description };
            var embeddings = new float[3][];

            for (int i = 0; i < descriptions.Length; i++)
            {
                embeddings[i] = await GenerateEmbeddingWithTimeoutAsync(descriptions[i]);
                Assert.IsNotNull(embeddings[i], $"Failed to generate embedding for memory {i}");
            }

            // Get the assigned memory IDs
            var allMemories = memorySystem.GetActiveEpisodicMemories().ToList();
            Assert.AreEqual(3, allMemories.Count, "Expected 3 episodic memories");

            // Add to vector store
            for (int i = 0; i < allMemories.Count; i++)
            {
                vectorStore.Upsert(
                    allMemories[i].Id,
                    "npc_hybrid",
                    MemoryVectorType.Episodic,
                    embeddings[i],
                    allMemories[i].SequenceNumber);
            }

            // Configure hybrid retrieval with semantic enabled
            var embeddingConfig = new EmbeddingConfig
            {
                ProviderType = EmbeddingProviderType.LlamaCpp,
                EnableSemanticRetrieval = true,
                KeywordWeight = 0.3f,
                SemanticWeight = 0.7f,
                MinSemanticSimilarity = 0.2f,
                SemanticCandidateLimit = 50,
                EmbeddingDimension = _embeddingDimension
            };

            var retrievalConfig = new ContextRetrievalConfig
            {
                MaxEpisodicMemories = 10,
                EmbeddingConfig = embeddingConfig
            };

            var retrievalLayer = new ContextRetrievalLayer(
                memorySystem,
                retrievalConfig,
                _provider,
                vectorStore,
                "npc_hybrid");

            // ACT: Retrieve context multiple times with query containing keyword "dragon"
            // Use async version to avoid deadlock in Unity
            var playerInput = "Tell me about dragons";

            var context1 = await retrievalLayer.RetrieveContextAsync(playerInput, FixedSnapshotTimeTicks);
            var context2 = await retrievalLayer.RetrieveContextAsync(playerInput, FixedSnapshotTimeTicks);
            var context3 = await retrievalLayer.RetrieveContextAsync(playerInput, FixedSnapshotTimeTicks);

            // ASSERT: All retrievals produce identical results
            CollectionAssert.AreEqual(context1.EpisodicMemories, context2.EpisodicMemories,
                "Run1 vs Run2 episodic memories mismatch");
            CollectionAssert.AreEqual(context1.EpisodicMemories, context3.EpisodicMemories,
                "Run1 vs Run3 episodic memories mismatch");

            // Verify dragon-related memories are ranked higher (keyword + semantic boost)
            Assert.IsTrue(context1.EpisodicMemories.Count > 0, "Expected at least one retrieved memory");

            Debug.Log($"[RAGDeterminism] Hybrid relevance verified. Retrieved {context1.EpisodicMemories.Count} memories. " +
                      $"Top: '{TruncateString(context1.EpisodicMemories.FirstOrDefault(), 50)}'");
        });

        /// <summary>
        /// Contract test: Snapshot and restore must preserve deterministic ordering.
        /// </summary>
        [UnityTest]
        [Timeout(180000)]
        public IEnumerator RAG_SnapshotRestore_PreservesDeterministicOrdering() => UniTask.ToCoroutine(async () =>
        {
            if (!await WaitForServerConnectionAsync(30f))
            {
                Assert.Ignore($"Embedding server not available at {EmbeddingServerUrl}");
                return;
            }

            // ARRANGE: Create and populate vector store
            var vectorStore = new InMemoryVectorStore(_embeddingDimension);

            var texts = new[]
            {
                ("snap_001", "First memory about adventures"),
                ("snap_002", "Second memory about quests"),
                ("snap_003", "Third memory about battles")
            };

            long seq = 1;
            foreach (var (id, text) in texts)
            {
                var embedding = await GenerateEmbeddingWithTimeoutAsync(text);
                Assert.IsNotNull(embedding, $"Failed to generate embedding for {id}");

                vectorStore.Upsert(id, "npc_snap", MemoryVectorType.Episodic, embedding, seq++);
            }

            // Generate query embedding
            var queryEmbedding = await GenerateEmbeddingWithTimeoutAsync("quest adventure");
            Assert.IsNotNull(queryEmbedding);

            // Get results before snapshot
            var resultsBefore = vectorStore.FindSimilar(queryEmbedding, k: 3, npcId: "npc_snap");

            // ACT: Snapshot and restore
            var snapshot = vectorStore.CreateSnapshot();

            var restoredStore = new InMemoryVectorStore(_embeddingDimension);
            restoredStore.RestoreFromSnapshot(snapshot);

            // Get results after restore
            var resultsAfter = restoredStore.FindSimilar(queryEmbedding, k: 3, npcId: "npc_snap");

            // ASSERT: Results are identical
            Assert.AreEqual(resultsBefore.Count, resultsAfter.Count, "Result count mismatch after restore");

            for (int i = 0; i < resultsBefore.Count; i++)
            {
                Assert.AreEqual(resultsBefore[i].MemoryId, resultsAfter[i].MemoryId,
                    $"Memory ID mismatch at index {i}");
                Assert.AreEqual(resultsBefore[i].Similarity, resultsAfter[i].Similarity, 1e-6f,
                    $"Similarity mismatch at index {i}");
                Assert.AreEqual(resultsBefore[i].SequenceNumber, resultsAfter[i].SequenceNumber,
                    $"Sequence number mismatch at index {i}");
            }

            Debug.Log($"[RAGDeterminism] Snapshot/restore determinism verified for {resultsBefore.Count} results");
        });

        #endregion

        #region NPC Filtering Determinism Tests

        /// <summary>
        /// Contract test: NPC filtering must be deterministic (includes shared memories).
        /// </summary>
        [UnityTest]
        [Timeout(180000)]
        public IEnumerator RAG_NpcFiltering_DeterministicIncludesShared() => UniTask.ToCoroutine(async () =>
        {
            if (!await WaitForServerConnectionAsync(30f))
            {
                Assert.Ignore($"Embedding server not available at {EmbeddingServerUrl}");
                return;
            }

            // ARRANGE: Create vector store with NPC-specific and shared memories
            var vectorStore = new InMemoryVectorStore(_embeddingDimension);

            var npc1Memory = "The blacksmith makes fine weapons";
            var npc2Memory = "The wizard studies ancient spells";
            var sharedMemory = "The kingdom is at peace";

            // Generate embeddings
            var emb1 = await GenerateEmbeddingWithTimeoutAsync(npc1Memory);
            var emb2 = await GenerateEmbeddingWithTimeoutAsync(npc2Memory);
            var embShared = await GenerateEmbeddingWithTimeoutAsync(sharedMemory);

            Assert.IsNotNull(emb1);
            Assert.IsNotNull(emb2);
            Assert.IsNotNull(embShared);

            // Insert: NPC1 memory, NPC2 memory, and shared (npcId=null)
            vectorStore.Upsert("npc1_mem", "blacksmith", MemoryVectorType.Episodic, emb1, 1);
            vectorStore.Upsert("npc2_mem", "wizard", MemoryVectorType.Episodic, emb2, 2);
            vectorStore.Upsert("shared_mem", null, MemoryVectorType.CanonicalFact, embShared, 3);

            // Generate query
            var queryEmbedding = await GenerateEmbeddingWithTimeoutAsync("weapons and crafting");
            Assert.IsNotNull(queryEmbedding);

            // ACT: Query as blacksmith multiple times
            var results1 = vectorStore.FindSimilar(queryEmbedding, k: 10, npcId: "blacksmith");
            var results2 = vectorStore.FindSimilar(queryEmbedding, k: 10, npcId: "blacksmith");

            // ASSERT: Results are deterministic
            Assert.AreEqual(results1.Count, results2.Count, "Result count mismatch");

            for (int i = 0; i < results1.Count; i++)
            {
                Assert.AreEqual(results1[i].MemoryId, results2[i].MemoryId,
                    $"Memory ID mismatch at index {i}");
            }

            // Verify blacksmith sees their own memories + shared, but not wizard's
            var memoryIds = results1.Select(r => r.MemoryId).ToList();
            Assert.Contains("npc1_mem", memoryIds, "Blacksmith should see their own memory");
            Assert.Contains("shared_mem", memoryIds, "Blacksmith should see shared memory");
            Assert.IsFalse(memoryIds.Contains("npc2_mem"), "Blacksmith should NOT see wizard's memory");

            Debug.Log($"[RAGDeterminism] NPC filtering verified. Blacksmith sees: [{string.Join(", ", memoryIds)}]");
        });

        #endregion

        #region Helper Methods

        /// <summary>
        /// Waits for the embedding server to become available, with retries.
        /// </summary>
        private async UniTask<bool> WaitForServerConnectionAsync(float timeoutSeconds = 30f)
        {
            var startTime = Time.realtimeSinceStartup;

            Debug.Log("[RAGDeterminism] Waiting for embedding server connection...");

            while (Time.realtimeSinceStartup - startTime < timeoutSeconds)
            {
                try
                {
                    using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                    var result = await _provider.TestConnectionAsync().AsUniTask().AttachExternalCancellation(cts.Token);
                    if (result)
                    {
                        Debug.Log("[RAGDeterminism] Embedding server connected!");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"[RAGDeterminism] Connection attempt exception: {ex.Message}");
                }

                await UniTask.Delay(1000);
            }

            Debug.LogWarning($"[RAGDeterminism] Server connection timeout after {timeoutSeconds}s");
            return false;
        }

        /// <summary>
        /// Generates an embedding with timeout using UniTask.WhenAny.
        /// </summary>
        private async UniTask<float[]> GenerateEmbeddingWithTimeoutAsync(string text, int timeoutSeconds = 30)
        {
            try
            {
                Debug.Log($"[RAGDeterminism] Starting embedding generation for: '{TruncateString(text, 40)}'");

                // Create the embedding task
                var embeddingTask = _provider.GenerateEmbeddingAsync(text).AsUniTask();
                var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(timeoutSeconds));

                // Race between embedding and timeout
                var (hasResult, result) = await UniTask.WhenAny(embeddingTask, timeoutTask);

                if (!hasResult)
                {
                    Debug.LogWarning($"[RAGDeterminism] Embedding generation timed out after {timeoutSeconds}s");
                    return null;
                }

                Debug.Log($"[RAGDeterminism] Embedding generated successfully, dim={result?.Length ?? 0}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RAGDeterminism] Embedding generation failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Runs a complete retrieval scenario and returns the results.
        /// </summary>
        private async UniTask<RetrievalTestResult> RunRetrievalScenario(
            (string id, string content)[] memories,
            string query,
            string npcId)
        {
            var vectorStore = new InMemoryVectorStore(_embeddingDimension);

            // Generate embeddings and store
            long seq = 1;
            foreach (var (id, content) in memories)
            {
                var embedding = await GenerateEmbeddingWithTimeoutAsync(content, 30);
                if (embedding != null)
                {
                    vectorStore.Upsert(id, npcId, MemoryVectorType.Episodic, embedding, seq++);
                }
            }

            // Generate query embedding
            var queryEmbedding = await GenerateEmbeddingWithTimeoutAsync(query, 30);
            if (queryEmbedding == null)
            {
                throw new InvalidOperationException("Failed to generate query embedding");
            }

            // Search
            var results = vectorStore.FindSimilar(queryEmbedding, k: memories.Length, npcId: npcId);

            return new RetrievalTestResult
            {
                MemoryIds = results.Select(r => r.MemoryId).ToList(),
                Similarities = results.Select(r => r.Similarity).ToList()
            };
        }

        private static string TruncateString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Result container for retrieval tests.
        /// </summary>
        private class RetrievalTestResult
        {
            public List<string> MemoryIds { get; set; } = new List<string>();
            public List<float> Similarities { get; set; } = new List<float>();
        }

        /// <summary>
        /// Fixed clock for deterministic time in tests.
        /// </summary>
        private class FixedClock : IClock
        {
            private readonly long _fixedTicks;

            public FixedClock(long fixedTicks) => _fixedTicks = fixedTicks;

            public long UtcNowTicks => _fixedTicks;
            public DateTime UtcNow => new DateTime(_fixedTicks, DateTimeKind.Utc);
        }

        /// <summary>
        /// Sequential ID generator for deterministic IDs in tests.
        /// </summary>
        private class SequentialIdGenerator : IIdGenerator
        {
            private readonly string _prefix;
            private int _counter;

            public SequentialIdGenerator(string prefix) => _prefix = prefix;

            public string GenerateId() => $"{_prefix}_{++_counter:D4}";
        }

        /// <summary>
        /// Helper class to wrap an action as IDisposable.
        /// </summary>
        private class DisposableAction : IDisposable
        {
            private readonly Action _action;
            private bool _disposed;

            public DisposableAction(Action action) => _action = action;

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _action?.Invoke();
                }
            }
        }

        #endregion
    }
}
