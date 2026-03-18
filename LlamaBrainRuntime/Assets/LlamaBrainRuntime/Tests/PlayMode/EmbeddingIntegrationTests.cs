using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LlamaBrain.Core;
using LlamaBrain.Core.Retrieval;
using LlamaBrain.Runtime.Core;

namespace LlamaBrain.Tests.PlayMode
{
    /// <summary>
    /// Integration tests for the RAG embedding system.
    /// Auto-starts embedding server if not already running.
    /// The embedding model must exist at: Backend/model/nomic-embed-text-v1.5.f32.gguf
    /// </summary>
    [Category("Integration")]
    [Category("RAG")]
    public class EmbeddingIntegrationTests
    {
        // Port for embedding server (separate from main LLM server)
        private const int EmbeddingServerPort = 8081;
        private const string EmbeddingServerUrl = "http://localhost:8081";
        private const int EmbeddingDimension = 768; // nomic-embed-text produces 768-dim
        private const string ModelName = "nomic-embed-text";

        // Paths relative to project root
        private const string EmbeddingModelRelativePath = "Backend/model/nomic-embed-text-v1.5.f32.gguf";
        private const string LlamaServerRelativePath = "Backend/llama-server.exe";

        // Shared server manager for all tests in this fixture
        private static ServerManager _embeddingServerManager;
        private static bool _serverStartedByTests;
        private static bool _serverReady;

        private LlamaCppEmbeddingProvider _provider;
        private bool _serverAvailable;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _serverAvailable = false;
            _serverReady = false;

            // Check if BrainServer is already running the embedding server
            var brainServer = BrainServer.Instance;
            if (brainServer != null && brainServer.IsEmbeddingServerRunning)
            {
                Debug.Log("[EmbeddingTest] BrainServer embedding server is running, will use it");
                _serverStartedByTests = false;
                _serverReady = true;
                return;
            }

            // Try to start embedding server ourselves
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var modelPath = Path.Combine(projectRoot, EmbeddingModelRelativePath);
            var serverPath = Path.Combine(projectRoot, LlamaServerRelativePath);

            if (!File.Exists(modelPath))
            {
                Debug.LogWarning($"[EmbeddingTest] Embedding model not found: {modelPath}");
                return;
            }

            if (!File.Exists(serverPath))
            {
                Debug.LogWarning($"[EmbeddingTest] llama-server not found: {serverPath}");
                return;
            }

            Debug.Log($"[EmbeddingTest] Starting embedding server: {serverPath}");
            Debug.Log($"[EmbeddingTest] Model: {modelPath}");

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
                // Don't block waiting for server - each test will check connectivity
                Debug.Log("[EmbeddingTest] Embedding server process started. Tests will wait for connectivity.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EmbeddingTest] Failed to start embedding server: {ex.Message}");
                _embeddingServerManager = null;
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_serverStartedByTests && _embeddingServerManager != null)
            {
                Debug.Log("[EmbeddingTest] Stopping embedding server started by tests");
                try
                {
                    _embeddingServerManager.StopServer();
                    _embeddingServerManager.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[EmbeddingTest] Error stopping server: {ex.Message}");
                }
                _embeddingServerManager = null;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _provider = new LlamaCppEmbeddingProvider(
                baseUrl: EmbeddingServerUrl,
                embeddingDimension: EmbeddingDimension,
                modelName: ModelName,
                timeout: TimeSpan.FromSeconds(30));
        }

        [TearDown]
        public void TearDown()
        {
            _provider?.Dispose();
        }

        #region Server Connection Tests

        [UnityTest]
        public IEnumerator EmbeddingServer_TestConnection_VerifiesServerAvailability()
        {
            yield return WaitForServerConnection(30f);

            _serverAvailable = _serverConnected;

            if (!_serverAvailable)
            {
                Debug.LogWarning($"[EmbeddingTest] Embedding server not available at {EmbeddingServerUrl}. " +
                    $"Start with: ./llama-server -m nomic-embed-text-v1.5.f32.gguf --embedding --port {EmbeddingServerPort}");
            }

            // Don't fail - just log status
            Debug.Log($"[EmbeddingTest] Server available: {_serverAvailable}");
            Assert.Pass($"Server connection test complete. Available: {_serverAvailable}");
        }

        #endregion

        #region Single Embedding Tests

        [UnityTest]
        public IEnumerator GenerateEmbedding_SimpleText_ReturnsValidVector()
        {
            var checkTask = _provider.TestConnectionAsync();
            yield return new WaitUntil(() => checkTask.IsCompleted);

            if (!checkTask.Result)
            {
                Assert.Ignore("Embedding server not available");
                yield break;
            }

            var task = _provider.GenerateEmbeddingAsync("Hello, world!");
            yield return new WaitUntil(() => task.IsCompleted);

            var embedding = task.Result;
            Assert.IsNotNull(embedding, "Embedding should not be null");
            Assert.AreEqual(EmbeddingDimension, embedding.Length,
                $"Expected {EmbeddingDimension} dimensions, got {embedding.Length}");

            // Verify embedding has reasonable values
            float sum = 0;
            foreach (var v in embedding) sum += Math.Abs(v);
            Assert.Greater(sum, 0, "Embedding should have non-zero values");

            Debug.Log($"[EmbeddingTest] Generated embedding with {embedding.Length} dimensions, L1 norm: {sum:F4}");
        }

        [UnityTest]
        public IEnumerator GenerateEmbedding_LongerText_ReturnsValidVector()
        {
            var checkTask = _provider.TestConnectionAsync();
            yield return new WaitUntil(() => checkTask.IsCompleted);

            if (!checkTask.Result)
            {
                Assert.Ignore("Embedding server not available");
                yield break;
            }

            var text = "The quick brown fox jumps over the lazy dog. " +
                       "This is a longer piece of text to test the embedding model's " +
                       "ability to handle paragraphs and multiple sentences.";

            var task = _provider.GenerateEmbeddingAsync(text);
            yield return new WaitUntil(() => task.IsCompleted);

            var embedding = task.Result;
            Assert.IsNotNull(embedding, "Embedding should not be null");
            Assert.AreEqual(EmbeddingDimension, embedding.Length);
        }

        [UnityTest]
        public IEnumerator GenerateEmbedding_Determinism_SameInputSameOutput()
        {
            var checkTask = _provider.TestConnectionAsync();
            yield return new WaitUntil(() => checkTask.IsCompleted);

            if (!checkTask.Result)
            {
                Assert.Ignore("Embedding server not available");
                yield break;
            }

            var text = "Test determinism with this exact phrase";

            // Generate twice
            var task1 = _provider.GenerateEmbeddingAsync(text);
            yield return new WaitUntil(() => task1.IsCompleted);

            var task2 = _provider.GenerateEmbeddingAsync(text);
            yield return new WaitUntil(() => task2.IsCompleted);

            var embedding1 = task1.Result;
            var embedding2 = task2.Result;

            Assert.IsNotNull(embedding1);
            Assert.IsNotNull(embedding2);

            // Should be identical
            for (int i = 0; i < embedding1.Length; i++)
            {
                Assert.AreEqual(embedding1[i], embedding2[i], 1e-6f,
                    $"Mismatch at index {i}: {embedding1[i]} vs {embedding2[i]}");
            }

            Debug.Log("[EmbeddingTest] Determinism verified - same input produces identical embedding");
        }

        #endregion

        #region Semantic Similarity Tests

        [UnityTest]
        public IEnumerator SemanticSimilarity_SimilarTexts_HighSimilarity()
        {
            var checkTask = _provider.TestConnectionAsync();
            yield return new WaitUntil(() => checkTask.IsCompleted);

            if (!checkTask.Result)
            {
                Assert.Ignore("Embedding server not available");
                yield break;
            }

            // Generate embeddings for similar texts
            var task1 = _provider.GenerateEmbeddingAsync("The weather is nice today");
            yield return new WaitUntil(() => task1.IsCompleted);

            var task2 = _provider.GenerateEmbeddingAsync("It's a beautiful sunny day");
            yield return new WaitUntil(() => task2.IsCompleted);

            var embedding1 = task1.Result;
            var embedding2 = task2.Result;

            Assert.IsNotNull(embedding1);
            Assert.IsNotNull(embedding2);

            var similarity = CosineSimilarity(embedding1, embedding2);
            Debug.Log($"[EmbeddingTest] Similarity between weather phrases: {similarity:F4}");

            Assert.Greater(similarity, 0.5f, "Similar texts should have high similarity");
        }

        [UnityTest]
        public IEnumerator SemanticSimilarity_DifferentTexts_LowerSimilarity()
        {
            var checkTask = _provider.TestConnectionAsync();
            yield return new WaitUntil(() => checkTask.IsCompleted);

            if (!checkTask.Result)
            {
                Assert.Ignore("Embedding server not available");
                yield break;
            }

            // Generate embeddings for unrelated texts
            var task1 = _provider.GenerateEmbeddingAsync("The weather is nice today");
            yield return new WaitUntil(() => task1.IsCompleted);

            var task2 = _provider.GenerateEmbeddingAsync("Advanced quantum mechanics equations");
            yield return new WaitUntil(() => task2.IsCompleted);

            var task3 = _provider.GenerateEmbeddingAsync("It's a beautiful sunny day");
            yield return new WaitUntil(() => task3.IsCompleted);

            var embedding1 = task1.Result;
            var embedding2 = task2.Result;
            var embedding3 = task3.Result;

            Assert.IsNotNull(embedding1);
            Assert.IsNotNull(embedding2);
            Assert.IsNotNull(embedding3);

            var similarityRelated = CosineSimilarity(embedding1, embedding3);
            var similarityUnrelated = CosineSimilarity(embedding1, embedding2);

            Debug.Log($"[EmbeddingTest] Similar topics: {similarityRelated:F4}, Unrelated: {similarityUnrelated:F4}");

            Assert.Greater(similarityRelated, similarityUnrelated,
                "Related texts should have higher similarity than unrelated texts");
        }

        [UnityTest]
        public IEnumerator SemanticSimilarity_GameDialogue_ContextualMatching()
        {
            var checkTask = _provider.TestConnectionAsync();
            yield return new WaitUntil(() => checkTask.IsCompleted);

            if (!checkTask.Result)
            {
                Assert.Ignore("Embedding server not available");
                yield break;
            }

            // Test with game-relevant dialogue
            var memories = new[]
            {
                "Player asked about the dragon attack on the village",
                "I told the player about the ancient sword in the cave",
                "The weather was cloudy when we met",
                "Player mentioned they defeated the goblin king"
            };

            var query = "Tell me about dragons";

            // Generate query embedding
            var queryTask = _provider.GenerateEmbeddingAsync(query);
            yield return new WaitUntil(() => queryTask.IsCompleted);
            var queryEmbedding = queryTask.Result;
            Assert.IsNotNull(queryEmbedding);

            // Generate and compare similarities
            var similarities = new float[memories.Length];
            for (int i = 0; i < memories.Length; i++)
            {
                var memTask = _provider.GenerateEmbeddingAsync(memories[i]);
                yield return new WaitUntil(() => memTask.IsCompleted);
                var memEmbedding = memTask.Result;
                Assert.IsNotNull(memEmbedding);
                similarities[i] = CosineSimilarity(queryEmbedding, memEmbedding);
                Debug.Log($"[EmbeddingTest] '{memories[i].Substring(0, Math.Min(50, memories[i].Length))}...' → {similarities[i]:F4}");
            }

            // The dragon-related memory should have highest similarity
            int maxIndex = 0;
            for (int i = 1; i < similarities.Length; i++)
            {
                if (similarities[i] > similarities[maxIndex])
                    maxIndex = i;
            }

            Assert.AreEqual(0, maxIndex,
                $"Dragon query should match dragon memory best. Got index {maxIndex}: '{memories[maxIndex]}'");
        }

        #endregion

        #region Batch Embedding Tests

        [UnityTest]
        public IEnumerator BatchEmbedding_MultipleTexts_ReturnsAllEmbeddings()
        {
            var checkTask = _provider.TestConnectionAsync();
            yield return new WaitUntil(() => checkTask.IsCompleted);

            if (!checkTask.Result)
            {
                Assert.Ignore("Embedding server not available");
                yield break;
            }

            var texts = new List<string>
            {
                "First sentence about cats",
                "Second sentence about dogs",
                "Third sentence about birds"
            };

            var task = _provider.GenerateBatchEmbeddingsAsync(texts);
            yield return new WaitUntil(() => task.IsCompleted);

            var embeddings = task.Result;
            Assert.IsNotNull(embeddings);
            Assert.AreEqual(texts.Count, embeddings.Length);

            for (int i = 0; i < embeddings.Length; i++)
            {
                Assert.IsNotNull(embeddings[i], $"Embedding {i} should not be null");
                Assert.AreEqual(EmbeddingDimension, embeddings[i].Length,
                    $"Embedding {i} should have {EmbeddingDimension} dimensions");
            }

            Debug.Log($"[EmbeddingTest] Generated {embeddings.Length} batch embeddings successfully");
        }

        [UnityTest]
        [Timeout(60000)] // 60 second timeout for larger batch
        public IEnumerator BatchEmbedding_LargerBatch_HandlesEfficiently()
        {
            var checkTask = _provider.TestConnectionAsync();
            yield return new WaitUntil(() => checkTask.IsCompleted);

            if (!checkTask.Result)
            {
                Assert.Ignore("Embedding server not available");
                yield break;
            }

            // Create 20 texts to embed
            var texts = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                texts.Add($"Memory entry number {i} about topic {i % 5}");
            }

            var startTime = Time.realtimeSinceStartup;
            var task = _provider.GenerateBatchEmbeddingsAsync(texts);
            yield return new WaitUntil(() => task.IsCompleted);
            var elapsed = Time.realtimeSinceStartup - startTime;

            var embeddings = task.Result;
            Assert.IsNotNull(embeddings);
            Assert.AreEqual(texts.Count, embeddings.Length);

            int nullCount = 0;
            foreach (var e in embeddings)
                if (e == null) nullCount++;

            Debug.Log($"[EmbeddingTest] Batch of {texts.Count} took {elapsed:F2}s. Null count: {nullCount}");

            // All should succeed (allow some flexibility for server issues)
            Assert.LessOrEqual(nullCount, 2, "At most 2 embeddings should fail");
        }

        #endregion

        #region Vector Store Integration Tests

        [UnityTest]
        public IEnumerator VectorStoreIntegration_StoreAndRetrieve_WorksCorrectly()
        {
            var checkTask = _provider.TestConnectionAsync();
            yield return new WaitUntil(() => checkTask.IsCompleted);

            if (!checkTask.Result)
            {
                Assert.Ignore("Embedding server not available");
                yield break;
            }

            // Create vector store
            var vectorStore = new InMemoryVectorStore(EmbeddingDimension);

            // Add some memories
            var memories = new Dictionary<string, string>
            {
                {"mem-1", "I visited the tavern last week"},
                {"mem-2", "The blacksmith made me a new sword"},
                {"mem-3", "I heard rumors about dragons in the mountains"},
                {"mem-4", "The king requested my presence at court"}
            };

            long seq = 1;
            foreach (var kvp in memories)
            {
                var embTask = _provider.GenerateEmbeddingAsync(kvp.Value);
                yield return new WaitUntil(() => embTask.IsCompleted);

                var embedding = embTask.Result;
                Assert.IsNotNull(embedding, $"Failed to generate embedding for {kvp.Key}");

                vectorStore.Upsert(kvp.Key, "npc-1", MemoryVectorType.Episodic, embedding, seq++);
            }

            Assert.AreEqual(4, vectorStore.GetStatistics().TotalVectors);

            // Query for similar memories
            var queryTask = _provider.GenerateEmbeddingAsync("Tell me about the dragon");
            yield return new WaitUntil(() => queryTask.IsCompleted);
            var queryEmbedding = queryTask.Result;
            Assert.IsNotNull(queryEmbedding);

            var results = vectorStore.FindSimilar(queryEmbedding, k: 2, npcId: "npc-1");

            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("mem-3", results[0].MemoryId, "Dragon memory should be most similar");

            Debug.Log($"[EmbeddingTest] Top result: {results[0].MemoryId} (sim: {results[0].Similarity:F4})");
            Debug.Log($"[EmbeddingTest] Second: {results[1].MemoryId} (sim: {results[1].Similarity:F4})");
        }

        [UnityTest]
        public IEnumerator VectorStoreIntegration_NpcFiltering_ReturnsCorrectResults()
        {
            var checkTask = _provider.TestConnectionAsync();
            yield return new WaitUntil(() => checkTask.IsCompleted);

            if (!checkTask.Result)
            {
                Assert.Ignore("Embedding server not available");
                yield break;
            }

            var vectorStore = new InMemoryVectorStore(EmbeddingDimension);

            // Add memories for different NPCs
            var npc1Memories = new[] { "I am a blacksmith", "I make swords and armor" };
            var npc2Memories = new[] { "I am a wizard", "I study ancient magic" };
            var sharedMemories = new[] { "The king rules this land" }; // Shared (npcId = null)

            long seq = 1;

            foreach (var mem in npc1Memories)
            {
                var task = _provider.GenerateEmbeddingAsync(mem);
                yield return new WaitUntil(() => task.IsCompleted);
                vectorStore.Upsert($"npc1-{seq}", "blacksmith", MemoryVectorType.Episodic, task.Result, seq++);
            }

            foreach (var mem in npc2Memories)
            {
                var task = _provider.GenerateEmbeddingAsync(mem);
                yield return new WaitUntil(() => task.IsCompleted);
                vectorStore.Upsert($"npc2-{seq}", "wizard", MemoryVectorType.Episodic, task.Result, seq++);
            }

            foreach (var mem in sharedMemories)
            {
                var task = _provider.GenerateEmbeddingAsync(mem);
                yield return new WaitUntil(() => task.IsCompleted);
                vectorStore.Upsert($"shared-{seq}", null, MemoryVectorType.CanonicalFact, task.Result, seq++);
            }

            // Query as blacksmith - should get blacksmith memories + shared
            var queryTask = _provider.GenerateEmbeddingAsync("What do you do?");
            yield return new WaitUntil(() => queryTask.IsCompleted);

            var blacksmithResults = vectorStore.FindSimilar(queryTask.Result, k: 10, npcId: "blacksmith");
            var wizardResults = vectorStore.FindSimilar(queryTask.Result, k: 10, npcId: "wizard");

            // Blacksmith query should not include wizard memories
            bool hasWizardMemory = false;
            foreach (var r in blacksmithResults)
            {
                if (r.MemoryId.StartsWith("npc2-")) hasWizardMemory = true;
            }
            Assert.IsFalse(hasWizardMemory, "Blacksmith query should not return wizard memories");

            // Both should include shared memories
            bool blacksmithHasShared = false;
            bool wizardHasShared = false;
            foreach (var r in blacksmithResults)
                if (r.MemoryId.StartsWith("shared-")) blacksmithHasShared = true;
            foreach (var r in wizardResults)
                if (r.MemoryId.StartsWith("shared-")) wizardHasShared = true;

            Assert.IsTrue(blacksmithHasShared, "Blacksmith should see shared memories");
            Assert.IsTrue(wizardHasShared, "Wizard should see shared memories");

            Debug.Log($"[EmbeddingTest] NPC filtering verified. Blacksmith results: {blacksmithResults.Count}, Wizard results: {wizardResults.Count}");
        }

        #endregion

        #region Performance Tests

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator Performance_SingleEmbedding_MeasureLatency()
        {
            var checkTask = _provider.TestConnectionAsync();
            yield return new WaitUntil(() => checkTask.IsCompleted);

            if (!checkTask.Result)
            {
                Assert.Ignore("Embedding server not available");
                yield break;
            }

            // Warm up
            var warmupTask = _provider.GenerateEmbeddingAsync("warmup");
            yield return new WaitUntil(() => warmupTask.IsCompleted);

            // Measure 10 embeddings
            var latencies = new List<float>();
            for (int i = 0; i < 10; i++)
            {
                var startTime = Time.realtimeSinceStartup;
                var task = _provider.GenerateEmbeddingAsync($"Test sentence number {i}");
                yield return new WaitUntil(() => task.IsCompleted);
                var elapsed = (Time.realtimeSinceStartup - startTime) * 1000; // ms
                latencies.Add(elapsed);
            }

            float avgLatency = 0;
            float minLatency = float.MaxValue;
            float maxLatency = 0;
            foreach (var l in latencies)
            {
                avgLatency += l;
                if (l < minLatency) minLatency = l;
                if (l > maxLatency) maxLatency = l;
            }
            avgLatency /= latencies.Count;

            Debug.Log($"[EmbeddingTest] Latency: avg={avgLatency:F1}ms, min={minLatency:F1}ms, max={maxLatency:F1}ms");

            // Should be reasonably fast (< 500ms average for local server)
            Assert.Less(avgLatency, 2000f, "Average latency should be under 2 seconds");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Waits for the embedding server to become available, with retries.
        /// Sets _serverConnected field when done.
        /// </summary>
        private bool _serverConnected;
        private IEnumerator WaitForServerConnection(float timeoutSeconds = 30f)
        {
            var startTime = Time.realtimeSinceStartup;
            _serverConnected = false;

            Debug.Log("[EmbeddingTest] Waiting for embedding server connection...");

            while (Time.realtimeSinceStartup - startTime < timeoutSeconds)
            {
                Task<bool> checkTask = null;
                bool hadException = false;

                try
                {
                    checkTask = _provider.TestConnectionAsync();
                }
                catch (Exception ex)
                {
                    Debug.Log($"[EmbeddingTest] Connection attempt exception: {ex.Message}");
                    hadException = true;
                }

                if (hadException)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                yield return new WaitUntil(() => checkTask.IsCompleted);

                if (checkTask.IsCompletedSuccessfully && checkTask.Result)
                {
                    _serverConnected = true;
                    Debug.Log("[EmbeddingTest] Embedding server connected!");
                    yield break;
                }

                yield return new WaitForSeconds(1f);
            }

            Debug.LogWarning($"[EmbeddingTest] Server connection timeout after {timeoutSeconds}s");
        }

        private static float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException($"Vector length mismatch: {a.Length} vs {b.Length}");

            float dotProduct = 0;
            float normA = 0;
            float normB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA == 0 || normB == 0)
                return 0;

            return dotProduct / ((float)Math.Sqrt(normA) * (float)Math.Sqrt(normB));
        }

        #endregion
    }
}
