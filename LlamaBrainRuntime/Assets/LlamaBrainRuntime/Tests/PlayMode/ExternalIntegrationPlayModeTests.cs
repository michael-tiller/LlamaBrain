#nullable enable

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LlamaBrain.Core;
using LlamaBrain.Runtime.Core;
using System.Collections;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.IO;

namespace LlamaBrain.Tests.PlayMode
{
    /// <summary>
    /// External Integration Tests: LLM Reproducibility Smoke Tests (PlayMode)
    ///
    /// These tests verify that seeded LLM generation produces reproducible output
    /// under controlled conditions (same server process, same model, same parameters).
    ///
    /// IMPORTANT LIMITATIONS:
    /// - These are NOT mathematical proofs of determinism
    /// - Seeded sampling is best-effort reproducibility, not a guarantee
    /// - Results may vary across: different hardware, GPU vs CPU, thread counts,
    ///   llama.cpp versions, CUDA versions, driver versions, etc.
    /// - True cross-session determinism would require: stop server, restart fresh,
    ///   reload model, then compare - which these tests do NOT do
    ///
    /// WHAT IS ACTUALLY PROVEN (in GovernancePlaneDeterminismTests.cs):
    /// - Prompt assembly is deterministic (byte-stable)
    /// - Validation/gating is deterministic
    /// - Memory mutation is deterministic
    /// - The pipeline AROUND the LLM is deterministic
    ///
    /// These tests run in Unity PlayMode with a real llama.cpp server.
    /// Category: ExternalIntegration (requires real server)
    /// </summary>
    [TestFixture]
    public class ExternalIntegrationPlayModeTests
    {
        private GameObject? serverObject;
        private BrainServer? server;
        private BrainSettings? settings;
        private ApiClient? apiClient;
        private List<IDisposable> disposables = new List<IDisposable>();
        private string? resolvedExePath;
        private string? resolvedModelPath;

        // Configurable paths from environment variables
        private static string GetExecutablePath() =>
            Environment.GetEnvironmentVariable("LLAMABRAIN_EXECUTABLE_PATH") ?? "Backend/llama-server.exe";

        private static string GetModelPath() =>
            Environment.GetEnvironmentVariable("LLAMABRAIN_MODEL_PATH") ?? "Backend/model/stablelm-zephyr-3b.Q4_0.gguf";

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
                return 5000;
            }
        }

        private static float GetServerTimeoutSeconds()
        {
            var timeoutEnv = Environment.GetEnvironmentVariable("LLAMABRAIN_SERVER_TIMEOUT_SECONDS");
            if (string.IsNullOrEmpty(timeoutEnv))
                return 30f;

            if (float.TryParse(timeoutEnv, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var timeout))
                return timeout;

            return 30f;
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
            apiClient = null;

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
            settings!.ContextSize = 2048;

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

        #region Seeded Reproducibility Smoke Tests

        /// <summary>
        /// Smoke test: Same seed + same prompt tends to produce identical output
        /// within the same server session.
        ///
        /// NOTE: This is NOT a determinism proof. Seeded sampling provides
        /// best-effort reproducibility under controlled conditions, but results
        /// may vary due to hardware, threading, GPU kernels, etc.
        /// </summary>
        [UnityTest]
        [Category("ExternalIntegration")]
        public IEnumerator PlayMode_SameSeedSamePrompt_ReproducibleOutput()
        {
            RequireExternal();
            SetUpExternalServer();
            yield return WaitForServerReady();

            const int seed = 42;
            const string prompt = "Complete this sentence: The quick brown fox";

            // First inference
            string output1;
            {
                var client1 = server!.CreateClient();
                Assert.That(client1, Is.Not.Null, "Failed to create client for request 1");
                disposables.Add(client1!);

                CompletionMetrics? result1 = null;
                yield return UniTask.ToCoroutine(async () =>
                {
                    result1 = await client1!.SendPromptWithMetricsAsync(
                        prompt,
                        maxTokens: 50,
                        temperature: 0.7f,
                        seed: seed,
                        cancellationToken: CancellationToken.None);
                });

                Assert.That(result1, Is.Not.Null, "Request 1 should return a result");
                output1 = result1!.Content;
                TestContext.Out.WriteLine($"Request 1 output: {output1}");

                Assert.That(output1, Does.Not.StartWith("Error:"),
                    $"Request 1 returned an error instead of LLM response: {output1}");
            }

            // Second inference with identical parameters
            string output2;
            {
                var client2 = server!.CreateClient();
                Assert.That(client2, Is.Not.Null, "Failed to create client for request 2");
                disposables.Add(client2!);

                CompletionMetrics? result2 = null;
                yield return UniTask.ToCoroutine(async () =>
                {
                    result2 = await client2!.SendPromptWithMetricsAsync(
                        prompt,
                        maxTokens: 50,
                        temperature: 0.7f,
                        seed: seed,
                        cancellationToken: CancellationToken.None);
                });

                Assert.That(result2, Is.Not.Null, "Request 2 should return a result");
                output2 = result2!.Content;
                TestContext.Out.WriteLine($"Request 2 output: {output2}");

                Assert.That(output2, Does.Not.StartWith("Error:"),
                    $"Request 2 returned an error instead of LLM response: {output2}");
            }

            // Assert: Outputs should match (best-effort reproducibility)
            Assert.That(output2, Is.EqualTo(output1),
                $"Seeded reproducibility check.\n" +
                $"Seed: {seed}\n" +
                $"Prompt: {prompt}\n" +
                $"Request 1: {output1}\n" +
                $"Request 2: {output2}\n" +
                $"NOTE: If this fails, it may indicate hardware/runtime non-determinism, not a bug.");

            TestContext.Out.WriteLine("Seeded reproducibility check passed (same session)");
        }

        /// <summary>
        /// Smoke test: Different seeds tend to produce different outputs.
        ///
        /// NOTE: This test is inherently probabilistic. Two different seeds
        /// CAN legitimately produce the same output for short completions.
        /// Repeated failures may indicate seed handling issues.
        /// </summary>
        [UnityTest]
        [Category("ExternalIntegration")]
        public IEnumerator PlayMode_DifferentSeeds_TendToProduceDifferentOutputs()
        {
            RequireExternal();
            SetUpExternalServer();
            yield return WaitForServerReady();

            apiClient = server!.CreateClient();
            Assert.That(apiClient, Is.Not.Null, "Failed to create client");
            disposables.Add(apiClient!);

            const string prompt = "Generate a random number:";

            CompletionMetrics? result1 = null;
            CompletionMetrics? result2 = null;

            yield return UniTask.ToCoroutine(async () =>
            {
                result1 = await apiClient!.SendPromptWithMetricsAsync(
                    prompt, maxTokens: 20, temperature: 1.0f, seed: 111);
                result2 = await apiClient!.SendPromptWithMetricsAsync(
                    prompt, maxTokens: 20, temperature: 1.0f, seed: 222);
            });

            Assert.That(result1, Is.Not.Null);
            Assert.That(result2, Is.Not.Null);

            Assert.That(result1!.Content, Does.Not.StartWith("Error:"),
                $"Seed 111 returned an error: {result1.Content}");
            Assert.That(result2!.Content, Does.Not.StartWith("Error:"),
                $"Seed 222 returned an error: {result2.Content}");

            TestContext.Out.WriteLine($"Seed 111: {result1.Content}");
            TestContext.Out.WriteLine($"Seed 222: {result2.Content}");

            // Different seeds should usually produce different outputs
            // but this is probabilistic, not guaranteed
            Assert.That(result2.Content, Is.Not.EqualTo(result1.Content),
                "Different seeds produced identical output. " +
                "This is statistically unlikely but not impossible. " +
                "If this fails repeatedly, check seed handling.");
        }

        /// <summary>
        /// Smoke test: InteractionCount as seed produces reproducible sequences
        /// within the same server session.
        ///
        /// This tests the Feature 14 pattern but does NOT prove true cross-session
        /// determinism (which would require server restart between sessions).
        /// </summary>
        [UnityTest]
        [Category("ExternalIntegration")]
        public IEnumerator PlayMode_InteractionCountAsSeed_ReproducibleSequence()
        {
            RequireExternal();
            SetUpExternalServer();
            yield return WaitForServerReady();

            const string basePrompt = "NPC responds to player greeting. Interaction #{0}. Player says: Hello!";

            var sequence1 = new string[3];
            var sequence2 = new string[3];

            // First sequence
            {
                var client = server!.CreateClient();
                Assert.That(client, Is.Not.Null, "Failed to create client for sequence 1");
                disposables.Add(client!);

                yield return UniTask.ToCoroutine(async () =>
                {
                    for (int interactionCount = 0; interactionCount < 3; interactionCount++)
                    {
                        var prompt = string.Format(basePrompt, interactionCount);
                        var result = await client!.SendPromptWithMetricsAsync(
                            prompt, maxTokens: 30, temperature: 0.7f, seed: interactionCount);
                        sequence1[interactionCount] = result.Content;
                    }
                });
            }

            // Second sequence (same server session)
            {
                var client = server!.CreateClient();
                Assert.That(client, Is.Not.Null, "Failed to create client for sequence 2");
                disposables.Add(client!);

                yield return UniTask.ToCoroutine(async () =>
                {
                    for (int interactionCount = 0; interactionCount < 3; interactionCount++)
                    {
                        var prompt = string.Format(basePrompt, interactionCount);
                        var result = await client!.SendPromptWithMetricsAsync(
                            prompt, maxTokens: 30, temperature: 0.7f, seed: interactionCount);
                        sequence2[interactionCount] = result.Content;
                    }
                });
            }

            // Assert: Both sequences should match
            for (int i = 0; i < 3; i++)
            {
                Assert.That(sequence1[i], Does.Not.StartWith("Error:"),
                    $"Sequence 1, interaction {i} returned an error: {sequence1[i]}");
                Assert.That(sequence2[i], Does.Not.StartWith("Error:"),
                    $"Sequence 2, interaction {i} returned an error: {sequence2[i]}");

                Assert.That(sequence2[i], Is.EqualTo(sequence1[i]),
                    $"Interaction {i} differs between sequences.\n" +
                    $"Sequence 1: {sequence1[i]}\n" +
                    $"Sequence 2: {sequence2[i]}");

                TestContext.Out.WriteLine($"Interaction {i}: {sequence1[i]}");
            }

            TestContext.Out.WriteLine("InteractionCount reproducibility check passed (same session)");
        }

        /// <summary>
        /// Smoke test: Temperature=0 (greedy decoding) produces reproducible output.
        ///
        /// NOTE: Even greedy decoding may not be 100% deterministic across
        /// all hardware/runtime configurations due to floating-point ordering.
        /// </summary>
        [UnityTest]
        [Category("ExternalIntegration")]
        public IEnumerator PlayMode_TemperatureZero_ReproducibleOutput()
        {
            RequireExternal();
            SetUpExternalServer();
            yield return WaitForServerReady();

            apiClient = server!.CreateClient();
            Assert.That(apiClient, Is.Not.Null, "Failed to create client");
            disposables.Add(apiClient!);

            const string prompt = "What is 2 + 2?";

            CompletionMetrics? result1 = null;
            CompletionMetrics? result2 = null;

            yield return UniTask.ToCoroutine(async () =>
            {
                result1 = await apiClient!.SendPromptWithMetricsAsync(
                    prompt, maxTokens: 10, temperature: 0.0f);
                result2 = await apiClient!.SendPromptWithMetricsAsync(
                    prompt, maxTokens: 10, temperature: 0.0f);
            });

            Assert.That(result1, Is.Not.Null);
            Assert.That(result2, Is.Not.Null);

            Assert.That(result1!.Content, Does.Not.StartWith("Error:"),
                $"First request returned an error: {result1.Content}");
            Assert.That(result2!.Content, Does.Not.StartWith("Error:"),
                $"Second request returned an error: {result2.Content}");

            Assert.That(result2.Content, Is.EqualTo(result1.Content),
                "Temperature=0 should produce reproducible output (greedy decoding).");

            TestContext.Out.WriteLine($"Temperature=0 output: {result1.Content}");
            TestContext.Out.WriteLine("Temperature=0 reproducibility check passed");
        }

        /// <summary>
        /// Smoke test: Structured output with same seed produces reproducible JSON.
        /// </summary>
        [UnityTest]
        [Category("ExternalIntegration")]
        public IEnumerator PlayMode_StructuredOutput_ReproducibleJson()
        {
            RequireExternal();
            SetUpExternalServer();
            yield return WaitForServerReady();

            const int seed = 12345;
            const string prompt = "Generate a person's name and age.";
            const string schema = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""name"": { ""type"": ""string"" },
                    ""age"": { ""type"": ""integer"" }
                },
                ""required"": [""name"", ""age""]
            }";

            // First request
            string json1;
            {
                var client1 = server!.CreateClient();
                Assert.That(client1, Is.Not.Null, "Failed to create client for request 1");
                disposables.Add(client1!);

                string? result = null;
                yield return UniTask.ToCoroutine(async () =>
                {
                    result = await client1!.SendStructuredPromptAsync(
                        prompt, schema, seed: seed);
                });

                Assert.That(result, Is.Not.Null, "Request 1 should return structured output");
                json1 = result!;
                TestContext.Out.WriteLine($"Request 1 JSON: {json1}");

                Assert.That(json1, Does.Not.StartWith("Error:"),
                    $"Request 1 returned an error: {json1}");
            }

            // Second request
            string json2;
            {
                var client2 = server!.CreateClient();
                Assert.That(client2, Is.Not.Null, "Failed to create client for request 2");
                disposables.Add(client2!);

                string? result = null;
                yield return UniTask.ToCoroutine(async () =>
                {
                    result = await client2!.SendStructuredPromptAsync(
                        prompt, schema, seed: seed);
                });

                Assert.That(result, Is.Not.Null, "Request 2 should return structured output");
                json2 = result!;
                TestContext.Out.WriteLine($"Request 2 JSON: {json2}");

                Assert.That(json2, Does.Not.StartWith("Error:"),
                    $"Request 2 returned an error: {json2}");
            }

            // Assert: JSON outputs should match
            Assert.That(json2, Is.EqualTo(json1),
                $"Structured output reproducibility check.\n" +
                $"Request 1: {json1}\n" +
                $"Request 2: {json2}");

            TestContext.Out.WriteLine("Structured output reproducibility check passed");
        }

        #endregion
    }
}
