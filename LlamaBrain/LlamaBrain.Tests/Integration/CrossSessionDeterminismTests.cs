using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using LlamaBrain.Core;

namespace LlamaBrain.Tests.Integration
{
    /// <summary>
    /// Feature 14: Cross-Session Determinism Proof Tests
    ///
    /// These tests prove that the same seed + same prompt produces identical output
    /// across independent sessions, completing the deterministic state reconstruction pattern.
    ///
    /// IMPORTANT: These tests require a running llama.cpp server.
    /// They are skipped by default in CI. Run manually with:
    ///   dotnet test --filter "Category=RequiresLlamaServer"
    ///
    /// To run locally:
    ///   1. Start llama.cpp server: ./llama-server -m model.gguf --port 8080
    ///   2. Set environment variable: LLAMA_SERVER_URL=http://localhost:8080
    ///   3. Run tests: dotnet test --filter "Category=RequiresLlamaServer"
    /// </summary>
    [TestFixture]
    [Category("RequiresLlamaServer")]
    public class CrossSessionDeterminismTests
    {
        private string? _serverUrl;
        private bool _serverAvailable;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            _serverUrl = Environment.GetEnvironmentVariable("LLAMA_SERVER_URL")
                ?? "http://localhost:8080";

            // Check if server is available
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var response = await client.GetAsync($"{_serverUrl}/health");
                _serverAvailable = response.IsSuccessStatusCode;
            }
            catch
            {
                _serverAvailable = false;
            }

            if (!_serverAvailable)
            {
                Assert.Ignore("llama.cpp server not available. Set LLAMA_SERVER_URL environment variable.");
            }
        }

        /// <summary>
        /// PROOF TEST: Same seed + same prompt = identical output across sessions.
        ///
        /// This test proves Feature 14's core claim:
        /// "f(Prompt, Context, InteractionCount) = Output" is a pure function.
        /// </summary>
        [Test]
        public async Task SameSeedSamePrompt_ProducesIdenticalOutput_AcrossSessions()
        {
            // Arrange
            var uri = new Uri(_serverUrl!);
            const int deterministicSeed = 42;
            const string prompt = "Complete this sentence: The quick brown fox";

            // Session 1: First inference
            string output1;
            {
                using var client1 = new ApiClient(uri.Host, uri.Port, "test");
                var result1 = await client1.SendPromptWithMetricsAsync(
                    prompt,
                    maxTokens: 50,
                    temperature: 0.7f,
                    seed: deterministicSeed,
                    cancellationToken: CancellationToken.None);
                output1 = result1.Content;
            }
            // Client disposed - simulates end of session

            // Session 2: Second inference with identical parameters
            string output2;
            {
                using var client2 = new ApiClient(uri.Host, uri.Port, "test");
                var result2 = await client2.SendPromptWithMetricsAsync(
                    prompt,
                    maxTokens: 50,
                    temperature: 0.7f,
                    seed: deterministicSeed,
                    cancellationToken: CancellationToken.None);
                output2 = result2.Content;
            }

            // Assert: Outputs must be byte-for-byte identical
            Assert.That(output2, Is.EqualTo(output1),
                $"Cross-session determinism FAILED.\n" +
                $"Seed: {deterministicSeed}\n" +
                $"Prompt: {prompt}\n" +
                $"Session 1 output: {output1}\n" +
                $"Session 2 output: {output2}");
        }

        /// <summary>
        /// Proves different seeds produce different outputs (sanity check).
        /// </summary>
        [Test]
        public async Task DifferentSeeds_ProduceDifferentOutputs()
        {
            // Arrange
            var uri = new Uri(_serverUrl!);
            const string prompt = "Generate a random number:";

            using var client = new ApiClient(uri.Host, uri.Port, "test");

            // Act
            var result1 = await client.SendPromptWithMetricsAsync(
                prompt, maxTokens: 20, temperature: 1.0f, seed: 111);
            var result2 = await client.SendPromptWithMetricsAsync(
                prompt, maxTokens: 20, temperature: 1.0f, seed: 222);

            // Assert: Different seeds should (almost always) produce different outputs
            // Note: There's a tiny chance they could match by coincidence
            Assert.That(result2.Content, Is.Not.EqualTo(result1.Content),
                "Different seeds produced identical output - this is statistically unlikely. " +
                "If this fails repeatedly, there may be an issue with seed handling.");
        }

        /// <summary>
        /// Proves determinism holds with structured output (JSON schema).
        /// </summary>
        [Test]
        public async Task SameSeedWithStructuredOutput_ProducesIdenticalJson()
        {
            // Arrange
            var uri = new Uri(_serverUrl!);
            const int deterministicSeed = 12345;
            const string prompt = "Generate a person's name and age.";
            const string schema = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""name"": { ""type"": ""string"" },
                    ""age"": { ""type"": ""integer"" }
                },
                ""required"": [""name"", ""age""]
            }";

            // Session 1
            string json1;
            {
                using var client1 = new ApiClient(uri.Host, uri.Port, "test");
                json1 = await client1.SendStructuredPromptAsync(
                    prompt, schema, seed: deterministicSeed);
            }

            // Session 2
            string json2;
            {
                using var client2 = new ApiClient(uri.Host, uri.Port, "test");
                json2 = await client2.SendStructuredPromptAsync(
                    prompt, schema, seed: deterministicSeed);
            }

            // Assert
            Assert.That(json2, Is.EqualTo(json1),
                $"Structured output determinism FAILED.\n" +
                $"Session 1: {json1}\n" +
                $"Session 2: {json2}");
        }

        /// <summary>
        /// Proves determinism with InteractionCount as seed (the Feature 14 pattern).
        /// </summary>
        [Test]
        public async Task InteractionCountAsSeed_ProducesDeterministicSequence()
        {
            // Arrange
            var uri = new Uri(_serverUrl!);
            const string basePrompt = "NPC responds to player greeting. Interaction #{0}. Player says: Hello!";

            // Simulate game replay: same interaction sequence
            var sequence1 = new string[3];
            var sequence2 = new string[3];

            // First playthrough
            {
                using var client = new ApiClient(uri.Host, uri.Port, "test");
                for (int interactionCount = 0; interactionCount < 3; interactionCount++)
                {
                    var prompt = string.Format(basePrompt, interactionCount);
                    var result = await client.SendPromptWithMetricsAsync(
                        prompt, maxTokens: 30, temperature: 0.7f, seed: interactionCount);
                    sequence1[interactionCount] = result.Content;
                }
            }

            // Second playthrough (new session, same interaction counts)
            {
                using var client = new ApiClient(uri.Host, uri.Port, "test");
                for (int interactionCount = 0; interactionCount < 3; interactionCount++)
                {
                    var prompt = string.Format(basePrompt, interactionCount);
                    var result = await client.SendPromptWithMetricsAsync(
                        prompt, maxTokens: 30, temperature: 0.7f, seed: interactionCount);
                    sequence2[interactionCount] = result.Content;
                }
            }

            // Assert: Both playthroughs produce identical dialogue
            for (int i = 0; i < 3; i++)
            {
                Assert.That(sequence2[i], Is.EqualTo(sequence1[i]),
                    $"Interaction {i} differs between playthroughs.\n" +
                    $"Playthrough 1: {sequence1[i]}\n" +
                    $"Playthrough 2: {sequence2[i]}");
            }
        }

        /// <summary>
        /// Proves that temperature=0 also produces deterministic output (alternative to seed).
        /// </summary>
        [Test]
        public async Task TemperatureZero_ProducesDeterministicOutput()
        {
            // Arrange
            var uri = new Uri(_serverUrl!);
            const string prompt = "What is 2 + 2?";

            using var client = new ApiClient(uri.Host, uri.Port, "test");

            // Act: temperature=0 should be greedy decoding (deterministic)
            var result1 = await client.SendPromptWithMetricsAsync(
                prompt, maxTokens: 10, temperature: 0.0f);
            var result2 = await client.SendPromptWithMetricsAsync(
                prompt, maxTokens: 10, temperature: 0.0f);

            // Assert
            Assert.That(result2.Content, Is.EqualTo(result1.Content),
                "Temperature=0 should produce deterministic output.");
        }
    }
}
