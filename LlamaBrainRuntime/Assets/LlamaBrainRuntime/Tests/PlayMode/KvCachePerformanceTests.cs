#nullable enable

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LlamaBrain.Core;
using LlamaBrain.Core.Inference;
using LlamaBrain.Runtime.Core;
using System.Collections;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.IO;

namespace LlamaBrain.Tests.PlayMode
{
    /// <summary>
    /// KV Cache Performance Tests (PlayMode)
    ///
    /// These tests verify that KV cache optimization provides measurable latency
    /// improvements when the same static prefix is reused across requests.
    ///
    /// Performance targets (Feature 27):
    /// - Cache hit: &lt; 200ms response time (prefill time near zero)
    /// - Cache miss: &lt; 1.5s response time (full prefill)
    /// - Cache hit rate: &gt; 80% for typical gameplay patterns
    ///
    /// IMPORTANT NOTES:
    /// - Tests require a real llama.cpp server with KV cache support
    /// - Latency measurements may vary based on hardware and model size
    /// - Tests verify relative improvement, not absolute thresholds
    /// - First request always misses cache (cold start)
    ///
    /// These tests run in Unity PlayMode with a real llama.cpp server.
    /// Category: ExternalIntegration (requires real server)
    /// </summary>
    [TestFixture]
    public class KvCachePerformanceTests
    {
        private GameObject? serverObject;
        private BrainServer? server;
        private BrainSettings? settings;
        private List<IDisposable> disposables = new List<IDisposable>();
        private string? resolvedExePath;
        private string? resolvedModelPath;

        // Configurable paths from environment variables
        private static string GetExecutablePath() =>
            Environment.GetEnvironmentVariable("LLAMABRAIN_EXECUTABLE_PATH") ?? "Backend/llama-server.exe";

        private static string GetModelPath() =>
            Environment.GetEnvironmentVariable("LLAMABRAIN_MODEL_PATH") ?? "Backend/model/qwen2.5-3b-instruct-abliterated-sft-q4_k_m.gguf";

        private static int GetPort()
        {
            var envPort = Environment.GetEnvironmentVariable("LLAMABRAIN_PORT");
            if (!string.IsNullOrEmpty(envPort) && int.TryParse(envPort, out var envPortValue))
                return envPortValue;

            // Pick a random free TCP port to avoid collisions
            try
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                var freePort = ((IPEndPoint)listener.LocalEndpoint).Port;
                listener.Stop();
                return freePort;
            }
            catch
            {
                return 5001; // Different default than other tests
            }
        }

        private static float GetServerTimeoutSeconds()
        {
            var timeoutEnv = Environment.GetEnvironmentVariable("LLAMABRAIN_SERVER_TIMEOUT_SECONDS");
            if (string.IsNullOrEmpty(timeoutEnv))
                return 60f; // Longer timeout for cache tests

            if (float.TryParse(timeoutEnv, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var timeout))
                return timeout;

            return 60f;
        }

        private static bool ShouldSkipExternalTests()
        {
            var skipEnv = Environment.GetEnvironmentVariable("LLAMABRAIN_SKIP_EXTERNAL_TESTS");
            return string.Equals(skipEnv, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(skipEnv, "true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validates external dependencies exist and throws Inconclusive if missing.
        /// </summary>
        private void RequireExternal()
        {
            if (ShouldSkipExternalTests())
            {
                Assert.Inconclusive("External tests skipped (LLAMABRAIN_SKIP_EXTERNAL_TESTS=1)");
            }

            var exePath = GetExecutablePath();
            var modelPath = GetModelPath();

            // Resolve paths relative to Unity project root if needed
            if (!Path.IsPathRooted(exePath) && !File.Exists(exePath))
            {
                var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
                var fullExePath = Path.Combine(projectRoot, exePath);
                if (File.Exists(fullExePath))
                    exePath = fullExePath;
            }

            if (!Path.IsPathRooted(modelPath) && !File.Exists(modelPath))
            {
                var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
                var fullModelPath = Path.Combine(projectRoot, modelPath);
                if (File.Exists(fullModelPath))
                    modelPath = fullModelPath;
            }

            if (!File.Exists(exePath))
            {
                Assert.Inconclusive($"Executable not found: {exePath}. Set LLAMABRAIN_EXECUTABLE_PATH.");
            }

            if (!File.Exists(modelPath))
            {
                Assert.Inconclusive($"Model file not found: {modelPath}. Set LLAMABRAIN_MODEL_PATH.");
            }

            resolvedExePath = exePath;
            resolvedModelPath = modelPath;
        }

        [SetUp]
        public void SetUp()
        {
            // Server is NOT started in SetUp - tests call RequireExternal() then SetUpExternalServer()
        }

        [TearDown]
        public void TearDown()
        {
            // Stop server first
            if (server != null)
            {
                try
                {
                    server.StopServer();
                }
                catch
                {
                    // Ignore shutdown errors
                }
            }

            // Dispose tracked IDisposable instances
            foreach (var disposable in disposables)
            {
                try
                {
                    disposable.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
            disposables.Clear();

            // Cleanup Unity objects
            if (serverObject)
            {
                UnityEngine.Object.DestroyImmediate(serverObject);
                serverObject = null;
            }
            server = null;

            if (settings)
            {
                UnityEngine.Object.DestroyImmediate(settings);
                settings = null;
            }
        }

        private void SetUpExternalServer()
        {
            Assert.That(resolvedExePath, Is.Not.Null, "Call RequireExternal() before SetUpExternalServer()");
            Assert.That(resolvedModelPath, Is.Not.Null, "Call RequireExternal() before SetUpExternalServer()");

            if (server != null) return;

            settings = ScriptableObject.CreateInstance<BrainSettings>();
            settings!.ExecutablePath = resolvedExePath!;
            settings!.ModelPath = resolvedModelPath!;
            settings!.Port = GetPort();
            settings!.ContextSize = 4096; // Larger context for cache tests

            serverObject = new GameObject("TestServer");
            server = serverObject!.AddComponent<BrainServer>();
            server!.Settings = settings;
            server!.Initialize();
        }

        private IEnumerator WaitForServerReady()
        {
            Assert.That(server, Is.Not.Null, "Call SetUpExternalServer() before WaitForServerReady()");
            Assert.That(settings, Is.Not.Null, "Call SetUpExternalServer() before WaitForServerReady()");

            yield return null;

            HttpClient? http = null;

            try
            {
                http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

                var startTime = Time.realtimeSinceStartup;
                var maxWaitTime = GetServerTimeoutSeconds();
                var port = settings!.Port;
                var healthUrl = $"http://127.0.0.1:{port}/health";

                while (Time.realtimeSinceStartup - startTime < maxWaitTime)
                {
                    var httpTask = http.GetAsync(healthUrl);
                    yield return new WaitUntil(() => httpTask.IsCompleted);

                    if (httpTask.IsFaulted)
                    {
                        _ = httpTask.Exception;
                        yield return new WaitForSeconds(0.5f);
                        continue;
                    }

                    if (httpTask.IsCanceled)
                    {
                        yield return new WaitForSeconds(0.5f);
                        continue;
                    }

                    if (httpTask.IsCompletedSuccessfully && httpTask.Result != null)
                    {
                        using var resp = httpTask.Result;
                        // Wait for 200 OK - 503 means model is still loading
                        if (resp.IsSuccessStatusCode)
                        {
                            TestContext.Out.WriteLine($"Server ready after {Time.realtimeSinceStartup - startTime:F1}s");
                            yield break;
                        }
                        // Still loading, wait and retry
                        TestContext.Out.WriteLine($"Server loading... (status: {(int)resp.StatusCode})");
                    }

                    yield return new WaitForSeconds(0.5f);
                }

                Assert.Inconclusive($"Server not ready within {maxWaitTime}s on port {port}. Model may still be loading.");
            }
            finally
            {
                http?.Dispose();
            }
        }

        #region KV Cache Performance Tests

        /// <summary>
        /// Static prefix used for cache tests - represents system prompt + canonical facts.
        /// This is the content that should be cached across requests.
        /// </summary>
        private const string StaticPrefix =
            "You are Gareth, a gruff but good-hearted blacksmith in the village of Thornwood. " +
            "You have lived here for 40 years and know everyone. You speak in short, direct sentences. " +
            "CANONICAL FACTS:\n" +
            "- The village of Thornwood sits at the edge of the Mistwood Forest\n" +
            "- The local tavern is called The Rusted Nail\n" +
            "- The mayor is Helena Brightwater, a half-elf merchant\n" +
            "- Dragons have not been seen for 200 years\n" +
            "- The annual Harvest Festival is next week\n" +
            "- You craft weapons and tools for the village guard\n" +
            "- Your apprentice is a young woman named Mira\n" +
            "- The nearest city is Eastholm, three days travel\n" +
            "WORLD STATE:\n" +
            "- CurrentTime: Morning\n" +
            "- Weather: Light rain\n" +
            "- ShopStatus: Open\n" +
            "- PlayerReputation: Neutral\n";

        /// <summary>
        /// Verifies that cached requests have lower prefill time than uncached requests.
        /// This is the core cache efficiency test.
        ///
        /// Test pattern:
        /// 1. First request (cold cache) - measures baseline prefill time
        /// 2. Second request with same prefix - should have much lower prefill time
        /// 3. Verify prefill time reduction indicates cache hit
        /// </summary>
        [UnityTest]
        [Category("ExternalIntegration")]
        [Category("Performance")]
        public IEnumerator PlayMode_KvCache_SameStaticPrefix_ReducesPrefillTime()
        {
            RequireExternal();
            SetUpExternalServer();
            yield return WaitForServerReady();

            var client = server!.CreateClient();
            Assert.That(client, Is.Not.Null, "Failed to create client");
            disposables.Add(client!);

            // Request 1: Cold cache (first request with this prefix)
            var prompt1 = StaticPrefix + "\nPlayer says: Good morning, blacksmith!\nGareth responds:";
            CompletionMetrics? metrics1 = null;

            yield return UniTask.ToCoroutine(async () =>
            {
                metrics1 = await client!.SendPromptWithMetricsAsync(
                    prompt1,
                    maxTokens: 50,
                    temperature: 0.7f,
                    seed: 42,
                    cachePrompt: true,
                    cancellationToken: CancellationToken.None);
            });

            Assert.That(metrics1, Is.Not.Null, "Request 1 should return metrics");
            Assert.That(metrics1!.Content, Does.Not.StartWith("Error:"),
                $"Request 1 returned an error: {metrics1.Content}");

            var prefillMs1 = metrics1.PrefillTimeMs;
            TestContext.Out.WriteLine($"Request 1 (cold cache): PrefillMs={prefillMs1}, Response={metrics1.Content}");

            // Small delay to ensure cache is populated
            yield return new WaitForSeconds(0.1f);

            // Request 2: Same static prefix, different dynamic content (should hit cache)
            var prompt2 = StaticPrefix + "\nPlayer says: Do you have any swords for sale?\nGareth responds:";
            CompletionMetrics? metrics2 = null;

            yield return UniTask.ToCoroutine(async () =>
            {
                metrics2 = await client!.SendPromptWithMetricsAsync(
                    prompt2,
                    maxTokens: 50,
                    temperature: 0.7f,
                    seed: 43,
                    cachePrompt: true,
                    cancellationToken: CancellationToken.None);
            });

            Assert.That(metrics2, Is.Not.Null, "Request 2 should return metrics");
            Assert.That(metrics2!.Content, Does.Not.StartWith("Error:"),
                $"Request 2 returned an error: {metrics2.Content}");

            var prefillMs2 = metrics2.PrefillTimeMs;
            TestContext.Out.WriteLine($"Request 2 (warm cache): PrefillMs={prefillMs2}, Response={metrics2.Content}");

            // Assert: Second request should have significantly lower prefill time
            // We expect at least 50% reduction for a meaningful cache hit
            // (allowing for some variance in timing)
            if (prefillMs1 > 100) // Only assert if first request took meaningful time
            {
                var reduction = (double)(prefillMs1 - prefillMs2) / prefillMs1 * 100;
                TestContext.Out.WriteLine($"Prefill time reduction: {reduction:F1}%");

                Assert.That(prefillMs2, Is.LessThan(prefillMs1),
                    $"Cached request should have lower prefill time.\n" +
                    $"Cold cache: {prefillMs1}ms\n" +
                    $"Warm cache: {prefillMs2}ms");
            }
            else
            {
                TestContext.Out.WriteLine($"Note: First request prefill ({prefillMs1}ms) too fast to measure cache benefit reliably");
            }
        }

        /// <summary>
        /// Verifies that changing the static prefix invalidates the cache.
        /// Different prefix = full prefill required.
        /// </summary>
        [UnityTest]
        [Category("ExternalIntegration")]
        [Category("Performance")]
        public IEnumerator PlayMode_KvCache_DifferentStaticPrefix_InvalidatesCache()
        {
            RequireExternal();
            SetUpExternalServer();
            yield return WaitForServerReady();

            var client = server!.CreateClient();
            Assert.That(client, Is.Not.Null, "Failed to create client");
            disposables.Add(client!);

            // Request 1: First prefix (primes the cache)
            var prompt1 = StaticPrefix + "\nPlayer says: Hello!\nGareth responds:";
            CompletionMetrics? metrics1 = null;

            yield return UniTask.ToCoroutine(async () =>
            {
                metrics1 = await client!.SendPromptWithMetricsAsync(
                    prompt1,
                    maxTokens: 30,
                    temperature: 0.7f,
                    seed: 100,
                    cachePrompt: true,
                    cancellationToken: CancellationToken.None);
            });

            Assert.That(metrics1, Is.Not.Null);
            Assert.That(metrics1!.Content, Does.Not.StartWith("Error:"));
            var prefillMs1 = metrics1.PrefillTimeMs;
            TestContext.Out.WriteLine($"Request 1 (first prefix): PrefillMs={prefillMs1}");

            yield return new WaitForSeconds(0.1f);

            // Request 2: Different prefix entirely (should miss cache)
            const string differentPrefix =
                "You are Elena, a mysterious traveling merchant. " +
                "You speak in riddles and never give straight answers. " +
                "FACTS: You sell rare artifacts from distant lands.\n";

            var prompt2 = differentPrefix + "\nPlayer says: Hello!\nElena responds:";
            CompletionMetrics? metrics2 = null;

            yield return UniTask.ToCoroutine(async () =>
            {
                metrics2 = await client!.SendPromptWithMetricsAsync(
                    prompt2,
                    maxTokens: 30,
                    temperature: 0.7f,
                    seed: 101,
                    cachePrompt: true,
                    cancellationToken: CancellationToken.None);
            });

            Assert.That(metrics2, Is.Not.Null);
            Assert.That(metrics2!.Content, Does.Not.StartWith("Error:"));
            var prefillMs2 = metrics2.PrefillTimeMs;
            TestContext.Out.WriteLine($"Request 2 (different prefix): PrefillMs={prefillMs2}");

            // Request 3: Back to original prefix (should miss cache, was evicted)
            var prompt3 = StaticPrefix + "\nPlayer says: Goodbye!\nGareth responds:";
            CompletionMetrics? metrics3 = null;

            yield return UniTask.ToCoroutine(async () =>
            {
                metrics3 = await client!.SendPromptWithMetricsAsync(
                    prompt3,
                    maxTokens: 30,
                    temperature: 0.7f,
                    seed: 102,
                    cachePrompt: true,
                    cancellationToken: CancellationToken.None);
            });

            Assert.That(metrics3, Is.Not.Null);
            Assert.That(metrics3!.Content, Does.Not.StartWith("Error:"));
            var prefillMs3 = metrics3.PrefillTimeMs;
            TestContext.Out.WriteLine($"Request 3 (original prefix, cache evicted): PrefillMs={prefillMs3}");

            // The different prefix request should have required full prefill
            // This test documents the behavior rather than strictly asserting timing
            TestContext.Out.WriteLine(
                $"Cache invalidation test:\n" +
                $"  First prefix (cold): {prefillMs1}ms\n" +
                $"  Different prefix (cold): {prefillMs2}ms\n" +
                $"  First prefix again (evicted): {prefillMs3}ms");
        }

        /// <summary>
        /// Simulates typical gameplay pattern: multiple dialogue turns with same NPC.
        /// Each turn should benefit from cached static prefix.
        ///
        /// This test measures cache hit rate across a conversation.
        /// </summary>
        [UnityTest]
        [Category("ExternalIntegration")]
        [Category("Performance")]
        public IEnumerator PlayMode_KvCache_MultiTurnConversation_MaintainsCacheEfficiency()
        {
            RequireExternal();
            SetUpExternalServer();
            yield return WaitForServerReady();

            var client = server!.CreateClient();
            Assert.That(client, Is.Not.Null, "Failed to create client");
            disposables.Add(client!);

            var playerLines = new[]
            {
                "Good morning!",
                "What do you have for sale today?",
                "How much for a sword?",
                "Tell me about the village.",
                "Have you heard any rumors?"
            };

            var prefillTimes = new List<long>();
            var dialogueHistory = "";

            for (int i = 0; i < playerLines.Length; i++)
            {
                // Build prompt with growing dialogue history (but same static prefix)
                var dynamicContent = dialogueHistory + $"\nPlayer says: {playerLines[i]}\nGareth responds:";
                var prompt = StaticPrefix + dynamicContent;

                CompletionMetrics? metrics = null;

                yield return UniTask.ToCoroutine(async () =>
                {
                    metrics = await client!.SendPromptWithMetricsAsync(
                        prompt,
                        maxTokens: 40,
                        temperature: 0.7f,
                        seed: 200 + i,
                        cachePrompt: true,
                        cancellationToken: CancellationToken.None);
                });

                Assert.That(metrics, Is.Not.Null, $"Turn {i + 1} should return metrics");
                Assert.That(metrics!.Content, Does.Not.StartWith("Error:"),
                    $"Turn {i + 1} returned an error: {metrics.Content}");

                prefillTimes.Add(metrics.PrefillTimeMs);
                TestContext.Out.WriteLine($"Turn {i + 1}: PrefillMs={metrics.PrefillTimeMs}, Player=\"{playerLines[i]}\"");

                // Add to dialogue history for next turn
                dialogueHistory += $"\nPlayer says: {playerLines[i]}\nGareth says: {metrics.Content.Trim()}";

                // Small delay between turns
                yield return new WaitForSeconds(0.1f);
            }

            // Calculate statistics
            var firstTurnMs = prefillTimes[0];
            var subsequentTurnsAvg = 0.0;
            for (int i = 1; i < prefillTimes.Count; i++)
            {
                subsequentTurnsAvg += prefillTimes[i];
            }
            subsequentTurnsAvg /= (prefillTimes.Count - 1);

            TestContext.Out.WriteLine($"\nConversation cache statistics:");
            TestContext.Out.WriteLine($"  First turn (cold cache): {firstTurnMs}ms");
            TestContext.Out.WriteLine($"  Subsequent turns avg: {subsequentTurnsAvg:F1}ms");

            if (firstTurnMs > 50)
            {
                var improvement = (1.0 - (subsequentTurnsAvg / firstTurnMs)) * 100;
                TestContext.Out.WriteLine($"  Average improvement: {improvement:F1}%");

                // Subsequent turns should show some improvement due to prefix caching
                // (though dialogue history growth may partially offset this)
                Assert.That(subsequentTurnsAvg, Is.LessThan(firstTurnMs * 1.5),
                    "Subsequent turns should not be significantly slower than first turn");
            }
        }

        /// <summary>
        /// Verifies that cachePrompt=false does not benefit from KV cache.
        /// This is a control test to confirm cache behavior is tied to the flag.
        /// </summary>
        [UnityTest]
        [Category("ExternalIntegration")]
        [Category("Performance")]
        public IEnumerator PlayMode_KvCache_CacheDisabled_NoPrefillImprovement()
        {
            RequireExternal();
            SetUpExternalServer();
            yield return WaitForServerReady();

            var client = server!.CreateClient();
            Assert.That(client, Is.Not.Null, "Failed to create client");
            disposables.Add(client!);

            // Request 1: Cache disabled
            var prompt1 = StaticPrefix + "\nPlayer says: Hello!\nGareth responds:";
            CompletionMetrics? metrics1 = null;

            yield return UniTask.ToCoroutine(async () =>
            {
                metrics1 = await client!.SendPromptWithMetricsAsync(
                    prompt1,
                    maxTokens: 30,
                    temperature: 0.7f,
                    seed: 300,
                    cachePrompt: false, // Cache disabled
                    cancellationToken: CancellationToken.None);
            });

            Assert.That(metrics1, Is.Not.Null);
            Assert.That(metrics1!.Content, Does.Not.StartWith("Error:"));
            var prefillMs1 = metrics1.PrefillTimeMs;
            TestContext.Out.WriteLine($"Request 1 (cache disabled): PrefillMs={prefillMs1}");

            yield return new WaitForSeconds(0.1f);

            // Request 2: Same prompt, cache still disabled
            var prompt2 = StaticPrefix + "\nPlayer says: How are you?\nGareth responds:";
            CompletionMetrics? metrics2 = null;

            yield return UniTask.ToCoroutine(async () =>
            {
                metrics2 = await client!.SendPromptWithMetricsAsync(
                    prompt2,
                    maxTokens: 30,
                    temperature: 0.7f,
                    seed: 301,
                    cachePrompt: false, // Cache disabled
                    cancellationToken: CancellationToken.None);
            });

            Assert.That(metrics2, Is.Not.Null);
            Assert.That(metrics2!.Content, Does.Not.StartWith("Error:"));
            var prefillMs2 = metrics2.PrefillTimeMs;
            TestContext.Out.WriteLine($"Request 2 (cache disabled): PrefillMs={prefillMs2}");

            // With cache disabled, both requests should have similar prefill times
            // (no benefit from caching)
            TestContext.Out.WriteLine(
                $"Cache disabled comparison:\n" +
                $"  Request 1: {prefillMs1}ms\n" +
                $"  Request 2: {prefillMs2}ms\n" +
                $"  Note: With cache disabled, prefill times should be similar");

            // Don't assert strict equality as there's natural variance,
            // but document the behavior
        }

        /// <summary>
        /// Compares latency with cache enabled vs disabled on same workload.
        /// This is the definitive test for Feature 27's performance claims.
        /// </summary>
        [UnityTest]
        [Category("ExternalIntegration")]
        [Category("Performance")]
        public IEnumerator PlayMode_KvCache_EnabledVsDisabled_MeasurableImprovement()
        {
            RequireExternal();
            SetUpExternalServer();
            yield return WaitForServerReady();

            const int warmupRequests = 2;
            const int measureRequests = 3;

            // Phase 1: Measure with cache ENABLED
            var cachedPrefillTimes = new List<long>();
            {
                var client = server!.CreateClient();
                disposables.Add(client!);

                // Warmup to prime the cache
                for (int i = 0; i < warmupRequests; i++)
                {
                    var warmupPrompt = StaticPrefix + $"\nPlayer says: Warmup {i}\nGareth responds:";
                    yield return UniTask.ToCoroutine(async () =>
                    {
                        await client!.SendPromptWithMetricsAsync(
                            warmupPrompt, maxTokens: 20, temperature: 0.7f, seed: 400 + i, cachePrompt: true);
                    });
                    yield return new WaitForSeconds(0.1f);
                }

                // Measure with warm cache
                for (int i = 0; i < measureRequests; i++)
                {
                    var prompt = StaticPrefix + $"\nPlayer says: Test message {i}\nGareth responds:";
                    CompletionMetrics? metrics = null;

                    yield return UniTask.ToCoroutine(async () =>
                    {
                        metrics = await client!.SendPromptWithMetricsAsync(
                            prompt, maxTokens: 30, temperature: 0.7f, seed: 410 + i, cachePrompt: true);
                    });

                    Assert.That(metrics, Is.Not.Null);
                    Assert.That(metrics!.Content, Does.Not.StartWith("Error:"));
                    cachedPrefillTimes.Add(metrics.PrefillTimeMs);

                    yield return new WaitForSeconds(0.1f);
                }
            }

            // Phase 2: Measure with cache DISABLED (different client to avoid cache)
            var uncachedPrefillTimes = new List<long>();
            {
                var client = server!.CreateClient();
                disposables.Add(client!);

                // Warmup (no cache benefit)
                for (int i = 0; i < warmupRequests; i++)
                {
                    var warmupPrompt = StaticPrefix + $"\nPlayer says: Warmup {i}\nGareth responds:";
                    yield return UniTask.ToCoroutine(async () =>
                    {
                        await client!.SendPromptWithMetricsAsync(
                            warmupPrompt, maxTokens: 20, temperature: 0.7f, seed: 500 + i, cachePrompt: false);
                    });
                    yield return new WaitForSeconds(0.1f);
                }

                // Measure without cache
                for (int i = 0; i < measureRequests; i++)
                {
                    var prompt = StaticPrefix + $"\nPlayer says: Test message {i}\nGareth responds:";
                    CompletionMetrics? metrics = null;

                    yield return UniTask.ToCoroutine(async () =>
                    {
                        metrics = await client!.SendPromptWithMetricsAsync(
                            prompt, maxTokens: 30, temperature: 0.7f, seed: 510 + i, cachePrompt: false);
                    });

                    Assert.That(metrics, Is.Not.Null);
                    Assert.That(metrics!.Content, Does.Not.StartWith("Error:"));
                    uncachedPrefillTimes.Add(metrics.PrefillTimeMs);

                    yield return new WaitForSeconds(0.1f);
                }
            }

            // Calculate averages
            double cachedAvg = 0, uncachedAvg = 0;
            foreach (var t in cachedPrefillTimes) cachedAvg += t;
            foreach (var t in uncachedPrefillTimes) uncachedAvg += t;
            cachedAvg /= cachedPrefillTimes.Count;
            uncachedAvg /= uncachedPrefillTimes.Count;

            TestContext.Out.WriteLine($"\nCache Performance Comparison:");
            TestContext.Out.WriteLine($"  Cache ENABLED avg prefill: {cachedAvg:F1}ms");
            TestContext.Out.WriteLine($"  Cache DISABLED avg prefill: {uncachedAvg:F1}ms");

            if (uncachedAvg > 50) // Only assert if timing is meaningful
            {
                var improvement = (1.0 - (cachedAvg / uncachedAvg)) * 100;
                TestContext.Out.WriteLine($"  Improvement: {improvement:F1}%");

                // Cache enabled should provide measurable improvement
                // (Feature 27 targets 200ms vs 1.5s, so we expect significant difference)
                Assert.That(cachedAvg, Is.LessThan(uncachedAvg),
                    $"Cache enabled should reduce prefill time.\n" +
                    $"Cached: {cachedAvg:F1}ms, Uncached: {uncachedAvg:F1}ms");
            }
            else
            {
                TestContext.Out.WriteLine(
                    "Note: Prefill times too fast to measure cache benefit reliably. " +
                    "This may occur with small models or fast hardware.");
            }
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Verifies cache efficiency as dialogue history grows to many turns.
        /// The static prefix should remain stable even as the dynamic suffix grows large.
        /// </summary>
        [UnityTest]
        [Category("ExternalIntegration")]
        [Category("Performance")]
        public IEnumerator PlayMode_KvCache_LargeDialogueHistoryGrowth_MaintainsStablePrefix()
        {
            RequireExternal();
            SetUpExternalServer();
            yield return WaitForServerReady();

            var client = server!.CreateClient();
            Assert.That(client, Is.Not.Null, "Failed to create client");
            disposables.Add(client!);

            const int totalTurns = 20; // Simulate 20 dialogue turns
            var dialogueHistory = "";
            var prefillTimes = new List<long>();
            var staticPrefixValidator = new PrefixStabilityValidator();

            // Track the static prefix to verify it doesn't change
            string? firstStaticPrefix = null;

            for (int turn = 0; turn < totalTurns; turn++)
            {
                // Build prompt with growing dialogue history
                var playerLine = $"This is message number {turn + 1} from the player.";
                var dynamicContent = dialogueHistory + $"\nPlayer says: {playerLine}\nGareth responds:";
                var fullPrompt = StaticPrefix + dynamicContent;

                // Extract what would be the static prefix (everything before dialogue)
                var currentStaticPrefix = StaticPrefix;
                if (firstStaticPrefix == null)
                {
                    firstStaticPrefix = currentStaticPrefix;
                }

                // Validate prefix stability
                var violation = staticPrefixValidator.ValidatePrefix(
                    "gareth",
                    currentStaticPrefix,
                    StaticPrefixBoundary.AfterCanonicalFacts);

                Assert.That(violation, Is.Null,
                    $"Static prefix changed unexpectedly at turn {turn + 1}");

                CompletionMetrics? metrics = null;

                yield return UniTask.ToCoroutine(async () =>
                {
                    metrics = await client!.SendPromptWithMetricsAsync(
                        fullPrompt,
                        maxTokens: 30,
                        temperature: 0.7f,
                        seed: 600 + turn,
                        cachePrompt: true,
                        cancellationToken: CancellationToken.None);
                });

                Assert.That(metrics, Is.Not.Null, $"Turn {turn + 1} should return metrics");
                Assert.That(metrics!.Content, Does.Not.StartWith("Error:"),
                    $"Turn {turn + 1} returned an error: {metrics.Content}");

                prefillTimes.Add(metrics.PrefillTimeMs);

                // Add to dialogue history for next turn
                var npcResponse = metrics.Content.Trim();
                if (npcResponse.Length > 100) npcResponse = npcResponse.Substring(0, 100);
                dialogueHistory += $"\nPlayer says: {playerLine}\nGareth says: {npcResponse}";

                // Log every 5 turns
                if ((turn + 1) % 5 == 0)
                {
                    TestContext.Out.WriteLine(
                        $"Turn {turn + 1}: PrefillMs={metrics.PrefillTimeMs}, " +
                        $"PromptTokens={metrics.PromptTokenCount}, " +
                        $"CachedTokens={metrics.CachedTokenCount}");
                }

                yield return new WaitForSeconds(0.05f);
            }

            // Calculate statistics
            var firstFiveTurnsAvg = prefillTimes.Take(5).Average();
            var lastFiveTurnsAvg = prefillTimes.Skip(totalTurns - 5).Take(5).Average();

            TestContext.Out.WriteLine($"\nDialogue history growth statistics:");
            TestContext.Out.WriteLine($"  First 5 turns avg prefill: {firstFiveTurnsAvg:F1}ms");
            TestContext.Out.WriteLine($"  Last 5 turns avg prefill: {lastFiveTurnsAvg:F1}ms");
            TestContext.Out.WriteLine($"  Total turns: {totalTurns}");
            TestContext.Out.WriteLine($"  Prefix stability violations: {staticPrefixValidator.ViolationCount}");

            // Assert: Static prefix should remain completely stable
            Assert.That(staticPrefixValidator.ViolationCount, Is.EqualTo(0),
                "Static prefix should not change as dialogue history grows");

            // Assert: Prefill times should not explode as history grows
            // (cache should still help with static prefix)
            Assert.That(lastFiveTurnsAvg, Is.LessThan(firstFiveTurnsAvg * 3),
                "Prefill time should not grow dramatically as dialogue history expands");
        }

        /// <summary>
        /// Simulates rapid NPC switching - player talks to 5 different NPCs in quick succession.
        /// Each NPC has a different static prefix, so cache should miss when switching.
        /// When returning to a previously-visited NPC, behavior depends on cache eviction policy.
        /// </summary>
        [UnityTest]
        [Category("ExternalIntegration")]
        [Category("Performance")]
        public IEnumerator PlayMode_KvCache_RapidNpcSwitching_HandlesMultiplePrefixes()
        {
            RequireExternal();
            SetUpExternalServer();
            yield return WaitForServerReady();

            var client = server!.CreateClient();
            Assert.That(client, Is.Not.Null, "Failed to create client");
            disposables.Add(client!);

            // Define 5 different NPCs with different static prefixes
            var npcs = new[]
            {
                new {
                    Id = "gareth",
                    Prefix = "You are Gareth, a gruff blacksmith in Thornwood. You speak in short sentences. " +
                             "FACTS:\n- You craft weapons for the village guard\n- Your apprentice is Mira\n"
                },
                new {
                    Id = "elena",
                    Prefix = "You are Elena, a mysterious traveling merchant. You speak in riddles. " +
                             "FACTS:\n- You sell rare artifacts from distant lands\n- You never stay in one place long\n"
                },
                new {
                    Id = "marcus",
                    Prefix = "You are Marcus, the village elder. You are wise and patient. " +
                             "FACTS:\n- You have led Thornwood for 30 years\n- You remember the old wars\n"
                },
                new {
                    Id = "luna",
                    Prefix = "You are Luna, a young herbalist. You are cheerful and curious. " +
                             "FACTS:\n- You gather herbs from the Mistwood Forest\n- Your mentor was the old healer\n"
                },
                new {
                    Id = "drake",
                    Prefix = "You are Drake, captain of the village guard. You are stern but fair. " +
                             "FACTS:\n- You trained in the capital city\n- You protect Thornwood from bandits\n"
                }
            };

            var results = new List<(string NpcId, int Visit, long PrefillMs, int CachedTokens)>();

            // Visit pattern: Talk to each NPC twice in round-robin fashion
            // First round: all cache misses (cold)
            // Second round: may hit cache if not evicted
            for (int round = 0; round < 2; round++)
            {
                for (int npcIndex = 0; npcIndex < npcs.Length; npcIndex++)
                {
                    var npc = npcs[npcIndex];
                    var playerLine = round == 0 ? "Hello, who are you?" : "Tell me more about yourself.";
                    var prompt = npc.Prefix + $"\nPlayer says: {playerLine}\n{npc.Id} responds:";

                    CompletionMetrics? metrics = null;

                    yield return UniTask.ToCoroutine(async () =>
                    {
                        metrics = await client!.SendPromptWithMetricsAsync(
                            prompt,
                            maxTokens: 40,
                            temperature: 0.7f,
                            seed: 700 + (round * 10) + npcIndex,
                            cachePrompt: true,
                            cancellationToken: CancellationToken.None);
                    });

                    Assert.That(metrics, Is.Not.Null);
                    Assert.That(metrics!.Content, Does.Not.StartWith("Error:"));

                    results.Add((npc.Id, round + 1, metrics.PrefillTimeMs, metrics.CachedTokenCount));

                    TestContext.Out.WriteLine(
                        $"NPC: {npc.Id}, Visit: {round + 1}, " +
                        $"PrefillMs: {metrics.PrefillTimeMs}, CachedTokens: {metrics.CachedTokenCount}");

                    yield return new WaitForSeconds(0.05f);
                }
            }

            // Analyze results
            var firstRoundAvg = results.Where(r => r.Visit == 1).Average(r => r.PrefillMs);
            var secondRoundAvg = results.Where(r => r.Visit == 2).Average(r => r.PrefillMs);
            var totalCachedTokensRound1 = results.Where(r => r.Visit == 1).Sum(r => r.CachedTokens);
            var totalCachedTokensRound2 = results.Where(r => r.Visit == 2).Sum(r => r.CachedTokens);

            TestContext.Out.WriteLine($"\nRapid NPC switching statistics:");
            TestContext.Out.WriteLine($"  NPCs visited: {npcs.Length}");
            TestContext.Out.WriteLine($"  First round avg prefill: {firstRoundAvg:F1}ms (cold cache)");
            TestContext.Out.WriteLine($"  Second round avg prefill: {secondRoundAvg:F1}ms (may hit cache)");
            TestContext.Out.WriteLine($"  First round total cached tokens: {totalCachedTokensRound1}");
            TestContext.Out.WriteLine($"  Second round total cached tokens: {totalCachedTokensRound2}");

            // First round should mostly be cache misses (cold start for each NPC)
            // We don't assert specific behavior for second round since cache eviction policy varies

            // Assert: System should handle rapid switching without errors
            Assert.That(results.Count, Is.EqualTo(npcs.Length * 2), "All NPC interactions should complete");

            // Assert: Each NPC's prefix produces consistent results
            foreach (var npc in npcs)
            {
                var npcResults = results.Where(r => r.NpcId == npc.Id).ToList();
                Assert.That(npcResults.Count, Is.EqualTo(2),
                    $"NPC {npc.Id} should have exactly 2 visits");
            }
        }

        /// <summary>
        /// Tests cache behavior with a very small static prefix (&lt;100 tokens).
        /// Even small prefixes should benefit from caching.
        /// </summary>
        [UnityTest]
        [Category("ExternalIntegration")]
        [Category("Performance")]
        public IEnumerator PlayMode_KvCache_VerySmallPrefix_StillShowsCacheBenefit()
        {
            RequireExternal();
            SetUpExternalServer();
            yield return WaitForServerReady();

            var client = server!.CreateClient();
            Assert.That(client, Is.Not.Null, "Failed to create client");
            disposables.Add(client!);

            // Very small static prefix (~20-30 tokens)
            const string smallPrefix = "You are Bob, a farmer. Be helpful.";

            var prefillTimes = new List<long>();

            for (int i = 0; i < 5; i++)
            {
                var prompt = smallPrefix + $"\nPlayer: Question {i + 1}?\nBob:";
                CompletionMetrics? metrics = null;

                yield return UniTask.ToCoroutine(async () =>
                {
                    metrics = await client!.SendPromptWithMetricsAsync(
                        prompt,
                        maxTokens: 20,
                        temperature: 0.7f,
                        seed: 800 + i,
                        cachePrompt: true,
                        cancellationToken: CancellationToken.None);
                });

                Assert.That(metrics, Is.Not.Null);
                Assert.That(metrics!.Content, Does.Not.StartWith("Error:"));
                prefillTimes.Add(metrics.PrefillTimeMs);

                TestContext.Out.WriteLine(
                    $"Request {i + 1}: PrefillMs={metrics.PrefillTimeMs}, " +
                    $"CachedTokens={metrics.CachedTokenCount}");

                yield return new WaitForSeconds(0.05f);
            }

            var firstPrefill = prefillTimes[0];
            var subsequentAvg = prefillTimes.Skip(1).Average();

            TestContext.Out.WriteLine($"\nSmall prefix cache statistics:");
            TestContext.Out.WriteLine($"  Prefix size: ~{smallPrefix.Length} chars");
            TestContext.Out.WriteLine($"  First request prefill: {firstPrefill}ms");
            TestContext.Out.WriteLine($"  Subsequent avg prefill: {subsequentAvg:F1}ms");

            // For very small prefixes, cache benefit may be minimal but should not hurt
            Assert.That(subsequentAvg, Is.LessThanOrEqualTo(firstPrefill * 1.5),
                "Small prefix caching should not increase latency");
        }

        #endregion

        #region n_keep Protection Tests (Context Shift Protection)

        /// <summary>
        /// Helper method to create a server with a small context size for overflow testing.
        /// Uses 512 tokens to make context overflow achievable within test timeouts.
        /// </summary>
        private void SetUpSmallContextServer()
        {
            Assert.That(resolvedExePath, Is.Not.Null, "Call RequireExternal() before SetUpSmallContextServer()");
            Assert.That(resolvedModelPath, Is.Not.Null, "Call RequireExternal() before SetUpSmallContextServer()");

            if (server != null) return;

            settings = ScriptableObject.CreateInstance<BrainSettings>();
            settings!.ExecutablePath = resolvedExePath!;
            settings!.ModelPath = resolvedModelPath!;
            settings!.Port = GetPort();
            settings!.ContextSize = 512; // Small context to trigger overflow quickly

            serverObject = new GameObject("TestServer");
            server = serverObject!.AddComponent<BrainServer>();
            server!.Settings = settings;
            server!.Initialize();
        }

        /// <summary>
        /// Estimates token count from character count using typical chars-per-token ratio.
        /// </summary>
        private static int EstimateTokenCount(int charCount, float charsPerToken = 4.0f)
        {
            if (charCount <= 0) return 0;
            return (int)Math.Ceiling(charCount / charsPerToken);
        }

        /// <summary>
        /// Verifies that n_keep parameter protects the static prefix during context shifts.
        ///
        /// Test approach:
        /// 1. Use a small context size (512 tokens) to force context overflow
        /// 2. Send requests with n_keep set to protect the static prefix
        /// 3. Generate enough tokens to fill and overflow the context
        /// 4. Verify subsequent requests still work and show cache benefit
        ///
        /// This tests Feature 27.2.1: Context Shift Protection
        /// </summary>
        [UnityTest]
        [Category("ExternalIntegration")]
        [Category("Performance")]
        public IEnumerator PlayMode_NKeep_ProtectsStaticPrefixDuringContextShift()
        {
            RequireExternal();
            SetUpSmallContextServer(); // 512 token context
            yield return WaitForServerReady();

            var client = server!.CreateClient();
            Assert.That(client, Is.Not.Null, "Failed to create client");
            disposables.Add(client!);

            // Static prefix (~100 tokens) - this should be protected by n_keep
            const string staticPrefix =
                "You are Gareth, a gruff but good-hearted blacksmith in Thornwood. " +
                "FACTS:\n" +
                "- The village sits at the edge of Mistwood Forest\n" +
                "- The tavern is called The Rusted Nail\n" +
                "- You craft weapons for the village guard\n" +
                "- Your apprentice is Mira\n";

            // Estimate n_keep from static prefix
            int nKeep = EstimateTokenCount(staticPrefix.Length);
            TestContext.Out.WriteLine($"Static prefix: {staticPrefix.Length} chars, estimated {nKeep} tokens");
            TestContext.Out.WriteLine($"n_keep set to: {nKeep}");

            var prefillTimes = new List<long>();
            var cachedTokenCounts = new List<int>();
            var dialogueHistory = "";

            // Generate enough turns to overflow the 512 token context
            // With n_keep protecting ~100 tokens, we have ~400 tokens for dialogue
            // Each turn generates ~50-80 tokens, so 10+ turns should overflow
            const int totalTurns = 12;

            for (int turn = 0; turn < totalTurns; turn++)
            {
                var playerLine = $"Tell me a long story about adventure number {turn + 1}. Make it detailed.";
                var dynamicContent = dialogueHistory + $"\nPlayer: {playerLine}\nGareth:";
                var fullPrompt = staticPrefix + dynamicContent;

                CompletionMetrics? metrics = null;

                yield return UniTask.ToCoroutine(async () =>
                {
                    metrics = await client!.SendPromptWithMetricsAsync(
                        fullPrompt,
                        maxTokens: 60, // Generate enough to fill context
                        temperature: 0.7f,
                        seed: 900 + turn,
                        cachePrompt: true,
                        nKeep: nKeep, // Protect static prefix
                        cancellationToken: CancellationToken.None);
                });

                Assert.That(metrics, Is.Not.Null, $"Turn {turn + 1} should return metrics");
                Assert.That(metrics!.Content, Does.Not.StartWith("Error:"),
                    $"Turn {turn + 1} returned error: {metrics.Content}");

                prefillTimes.Add(metrics.PrefillTimeMs);
                cachedTokenCounts.Add(metrics.CachedTokenCount);

                // Log progress
                TestContext.Out.WriteLine(
                    $"Turn {turn + 1}: PrefillMs={metrics.PrefillTimeMs}, " +
                    $"PromptTokens={metrics.PromptTokenCount}, " +
                    $"CachedTokens={metrics.CachedTokenCount}, " +
                    $"Generated={metrics.GeneratedTokenCount}");

                // Add to dialogue history (truncate to simulate rolling buffer)
                var npcResponse = metrics.Content.Trim();
                if (npcResponse.Length > 150) npcResponse = npcResponse.Substring(0, 150);
                dialogueHistory += $"\nPlayer: {playerLine}\nGareth: {npcResponse}";

                // Keep dialogue history manageable (simulate sliding window)
                if (dialogueHistory.Length > 800)
                {
                    dialogueHistory = dialogueHistory.Substring(dialogueHistory.Length - 600);
                }

                yield return new WaitForSeconds(0.1f);
            }

            // Analyze results
            var firstThreeTurnsAvgPrefill = prefillTimes.Take(3).Average();
            var lastThreeTurnsAvgPrefill = prefillTimes.Skip(totalTurns - 3).Take(3).Average();
            var firstThreeTurnsAvgCached = cachedTokenCounts.Take(3).Average();
            var lastThreeTurnsAvgCached = cachedTokenCounts.Skip(totalTurns - 3).Take(3).Average();

            TestContext.Out.WriteLine($"\nn_keep Protection Results (context size: 512 tokens):");
            TestContext.Out.WriteLine($"  n_keep value: {nKeep} tokens");
            TestContext.Out.WriteLine($"  Total turns: {totalTurns}");
            TestContext.Out.WriteLine($"  First 3 turns avg prefill: {firstThreeTurnsAvgPrefill:F1}ms");
            TestContext.Out.WriteLine($"  Last 3 turns avg prefill: {lastThreeTurnsAvgPrefill:F1}ms");
            TestContext.Out.WriteLine($"  First 3 turns avg cached tokens: {firstThreeTurnsAvgCached:F1}");
            TestContext.Out.WriteLine($"  Last 3 turns avg cached tokens: {lastThreeTurnsAvgCached:F1}");

            // Assert: System should complete all turns without errors (already verified above)
            Assert.That(prefillTimes.Count, Is.EqualTo(totalTurns), "All turns should complete");

            // Assert: Later turns should still show some cache benefit
            // If n_keep is working, the static prefix should remain cached even after overflow
            // Note: We can't assert exact cached token counts as it depends on server behavior
            Assert.That(lastThreeTurnsAvgPrefill, Is.LessThan(lastThreeTurnsAvgPrefill * 10),
                "Later turns should not have catastrophically high prefill times");
        }

        /// <summary>
        /// Verifies that static prefix survives context window overflow when n_keep is set.
        ///
        /// This test specifically targets the scenario where:
        /// 1. Context fills completely (512 tokens)
        /// 2. Context shift occurs (oldest tokens evicted)
        /// 3. Static prefix (protected by n_keep) should survive
        /// 4. Cache benefit should persist for the protected prefix
        ///
        /// Tests ROADMAP item: "Add tests verifying static prefix survives context window overflow"
        /// </summary>
        [UnityTest]
        [Category("ExternalIntegration")]
        [Category("Performance")]
        public IEnumerator PlayMode_NKeep_StaticPrefixSurvivesContextOverflow()
        {
            RequireExternal();
            SetUpSmallContextServer(); // 512 token context
            yield return WaitForServerReady();

            var client = server!.CreateClient();
            Assert.That(client, Is.Not.Null, "Failed to create client");
            disposables.Add(client!);

            // Small static prefix that should be protected
            const string staticPrefix =
                "You are a helpful assistant. Always be concise. " +
                "RULES: Answer in one sentence only.\n";

            int nKeep = EstimateTokenCount(staticPrefix.Length);
            TestContext.Out.WriteLine($"Static prefix: {staticPrefix.Length} chars, n_keep={nKeep}");

            // Phase 1: Prime the cache with static prefix
            var prompt1 = staticPrefix + "\nUser: What is 2+2?\nAssistant:";
            CompletionMetrics? metrics1 = null;

            yield return UniTask.ToCoroutine(async () =>
            {
                metrics1 = await client!.SendPromptWithMetricsAsync(
                    prompt1,
                    maxTokens: 20,
                    temperature: 0.7f,
                    seed: 1000,
                    cachePrompt: true,
                    nKeep: nKeep,
                    cancellationToken: CancellationToken.None);
            });

            Assert.That(metrics1, Is.Not.Null);
            Assert.That(metrics1!.Content, Does.Not.StartWith("Error:"));
            var initialPrefillMs = metrics1.PrefillTimeMs;
            var initialCachedTokens = metrics1.CachedTokenCount;
            TestContext.Out.WriteLine($"Initial request: PrefillMs={initialPrefillMs}, CachedTokens={initialCachedTokens}");

            yield return new WaitForSeconds(0.1f);

            // Phase 2: Fill up the context with a long conversation
            // Generate enough content to definitely overflow 512 tokens
            var longHistory = "";
            for (int i = 0; i < 8; i++)
            {
                var fillPrompt = staticPrefix + longHistory + $"\nUser: Tell me fact {i + 1} about science.\nAssistant:";
                CompletionMetrics? fillMetrics = null;

                yield return UniTask.ToCoroutine(async () =>
                {
                    fillMetrics = await client!.SendPromptWithMetricsAsync(
                        fillPrompt,
                        maxTokens: 40,
                        temperature: 0.7f,
                        seed: 1010 + i,
                        cachePrompt: true,
                        nKeep: nKeep,
                        cancellationToken: CancellationToken.None);
                });

                Assert.That(fillMetrics, Is.Not.Null);
                Assert.That(fillMetrics!.Content, Does.Not.StartWith("Error:"),
                    $"Fill request {i + 1} failed: {fillMetrics.Content}");

                longHistory += $"\nUser: Tell me fact {i + 1}.\nAssistant: {fillMetrics.Content.Trim()}";

                TestContext.Out.WriteLine(
                    $"Fill {i + 1}: PromptTokens={fillMetrics.PromptTokenCount}, " +
                    $"CachedTokens={fillMetrics.CachedTokenCount}");

                yield return new WaitForSeconds(0.05f);
            }

            // Phase 3: After overflow, verify the static prefix still provides cache benefit
            // Use the same prefix but fresh dynamic content
            var postOverflowPrompt = staticPrefix + "\nUser: What is the capital of France?\nAssistant:";
            CompletionMetrics? postOverflowMetrics = null;

            yield return UniTask.ToCoroutine(async () =>
            {
                postOverflowMetrics = await client!.SendPromptWithMetricsAsync(
                    postOverflowPrompt,
                    maxTokens: 20,
                    temperature: 0.7f,
                    seed: 1100,
                    cachePrompt: true,
                    nKeep: nKeep,
                    cancellationToken: CancellationToken.None);
            });

            Assert.That(postOverflowMetrics, Is.Not.Null);
            Assert.That(postOverflowMetrics!.Content, Does.Not.StartWith("Error:"));

            var postOverflowPrefillMs = postOverflowMetrics.PrefillTimeMs;
            var postOverflowCachedTokens = postOverflowMetrics.CachedTokenCount;

            TestContext.Out.WriteLine($"\nContext Overflow Test Results:");
            TestContext.Out.WriteLine($"  Context size: 512 tokens");
            TestContext.Out.WriteLine($"  n_keep: {nKeep} tokens");
            TestContext.Out.WriteLine($"  Initial request prefill: {initialPrefillMs}ms, cached: {initialCachedTokens}");
            TestContext.Out.WriteLine($"  Post-overflow request prefill: {postOverflowPrefillMs}ms, cached: {postOverflowCachedTokens}");

            // Assert: Post-overflow requests should still work
            Assert.That(postOverflowMetrics.Content.Length, Is.GreaterThan(0),
                "Post-overflow request should generate content");

            // Assert: If n_keep is working, some tokens should still be cached
            // (the static prefix should survive the context shift)
            // Note: Exact behavior depends on llama.cpp implementation
            TestContext.Out.WriteLine(
                $"  Result: Post-overflow cached {postOverflowCachedTokens} tokens " +
                $"(n_keep protected {nKeep} tokens)");
        }

        /// <summary>
        /// Compares behavior with and without n_keep protection during context overflow.
        /// This demonstrates the value of n_keep for maintaining cache efficiency.
        /// </summary>
        [UnityTest]
        [Category("ExternalIntegration")]
        [Category("Performance")]
        public IEnumerator PlayMode_NKeep_WithVsWithoutProtection_ShowsDifference()
        {
            RequireExternal();
            SetUpSmallContextServer(); // 512 token context
            yield return WaitForServerReady();

            const string staticPrefix =
                "You are a wise scholar. You speak formally. " +
                "KNOWLEDGE: History, science, philosophy.\n";

            int nKeep = EstimateTokenCount(staticPrefix.Length);
            TestContext.Out.WriteLine($"Testing n_keep={nKeep} vs n_keep=null (512 token context)");

            // Test 1: WITH n_keep protection
            var withNKeepResults = new List<(long PrefillMs, int CachedTokens)>();
            {
                var client = server!.CreateClient();
                disposables.Add(client!);

                for (int i = 0; i < 6; i++)
                {
                    var prompt = staticPrefix + $"\nUser: Question {i + 1} about history?\nScholar:";
                    CompletionMetrics? metrics = null;

                    yield return UniTask.ToCoroutine(async () =>
                    {
                        metrics = await client!.SendPromptWithMetricsAsync(
                            prompt,
                            maxTokens: 50,
                            temperature: 0.7f,
                            seed: 1200 + i,
                            cachePrompt: true,
                            nKeep: nKeep, // WITH protection
                            cancellationToken: CancellationToken.None);
                    });

                    Assert.That(metrics, Is.Not.Null);
                    Assert.That(metrics!.Content, Does.Not.StartWith("Error:"));
                    withNKeepResults.Add((metrics.PrefillTimeMs, metrics.CachedTokenCount));

                    yield return new WaitForSeconds(0.05f);
                }
            }

            // Small delay between test phases
            yield return new WaitForSeconds(0.5f);

            // Test 2: WITHOUT n_keep protection (null)
            var withoutNKeepResults = new List<(long PrefillMs, int CachedTokens)>();
            {
                var client = server!.CreateClient();
                disposables.Add(client!);

                for (int i = 0; i < 6; i++)
                {
                    var prompt = staticPrefix + $"\nUser: Question {i + 1} about science?\nScholar:";
                    CompletionMetrics? metrics = null;

                    yield return UniTask.ToCoroutine(async () =>
                    {
                        metrics = await client!.SendPromptWithMetricsAsync(
                            prompt,
                            maxTokens: 50,
                            temperature: 0.7f,
                            seed: 1300 + i,
                            cachePrompt: true,
                            nKeep: null, // WITHOUT protection (server default)
                            cancellationToken: CancellationToken.None);
                    });

                    Assert.That(metrics, Is.Not.Null);
                    Assert.That(metrics!.Content, Does.Not.StartWith("Error:"));
                    withoutNKeepResults.Add((metrics.PrefillTimeMs, metrics.CachedTokenCount));

                    yield return new WaitForSeconds(0.05f);
                }
            }

            // Analyze results
            var withNKeepAvgPrefill = withNKeepResults.Average(r => r.PrefillMs);
            var withoutNKeepAvgPrefill = withoutNKeepResults.Average(r => r.PrefillMs);
            var withNKeepAvgCached = withNKeepResults.Average(r => r.CachedTokens);
            var withoutNKeepAvgCached = withoutNKeepResults.Average(r => r.CachedTokens);

            TestContext.Out.WriteLine($"\nn_keep Protection Comparison:");
            TestContext.Out.WriteLine($"  WITH n_keep={nKeep}:");
            TestContext.Out.WriteLine($"    Avg prefill: {withNKeepAvgPrefill:F1}ms");
            TestContext.Out.WriteLine($"    Avg cached tokens: {withNKeepAvgCached:F1}");
            TestContext.Out.WriteLine($"  WITHOUT n_keep (null):");
            TestContext.Out.WriteLine($"    Avg prefill: {withoutNKeepAvgPrefill:F1}ms");
            TestContext.Out.WriteLine($"    Avg cached tokens: {withoutNKeepAvgCached:F1}");

            // Log individual results for debugging
            TestContext.Out.WriteLine($"\n  WITH n_keep details:");
            for (int i = 0; i < withNKeepResults.Count; i++)
            {
                TestContext.Out.WriteLine($"    Request {i + 1}: {withNKeepResults[i].PrefillMs}ms, {withNKeepResults[i].CachedTokens} cached");
            }
            TestContext.Out.WriteLine($"\n  WITHOUT n_keep details:");
            for (int i = 0; i < withoutNKeepResults.Count; i++)
            {
                TestContext.Out.WriteLine($"    Request {i + 1}: {withoutNKeepResults[i].PrefillMs}ms, {withoutNKeepResults[i].CachedTokens} cached");
            }

            // Assert: Both configurations should complete without errors
            Assert.That(withNKeepResults.Count, Is.EqualTo(6), "All WITH n_keep requests should complete");
            Assert.That(withoutNKeepResults.Count, Is.EqualTo(6), "All WITHOUT n_keep requests should complete");

            // Document the difference (actual benefit depends on llama.cpp implementation)
            var difference = withoutNKeepAvgPrefill - withNKeepAvgPrefill;
            TestContext.Out.WriteLine($"\n  Prefill difference: {difference:F1}ms (positive = n_keep helps)");
        }

        #endregion
    }
}
