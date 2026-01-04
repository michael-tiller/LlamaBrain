using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NUnit.Framework;
using LlamaBrain.Core.Validation;
using LlamaBrain.Core.StructuredInput;
using LlamaBrain.Core.StructuredInput.Schemas;

namespace LlamaBrain.Tests.Performance
{
    /// <summary>
    /// Performance tests to prove parsing latency claims.
    ///
    /// Claim: "Sub-millisecond parsing for all paths" (ARCHITECTURE.md)
    ///
    /// These tests verify that parsing operations complete in under 1ms,
    /// providing test-based proof of the performance claim.
    /// </summary>
    [TestFixture]
    [Category("Performance")]
    public class ParsingPerformanceTests
    {
        private OutputParser _parser = null!;
        private OutputParser _structuredParser = null!;

        // Threshold in milliseconds - claim is "sub-millisecond"
        private const double MaxParseTimeMs = 1.0;

        // Number of iterations for reliable timing
        private const int WarmupIterations = 100;
        private const int TimedIterations = 1000;

        [SetUp]
        public void SetUp()
        {
            _parser = new OutputParser(OutputParserConfig.Default);
            _structuredParser = new OutputParser(OutputParserConfig.Structured);
        }

        #region OutputParser.Parse (Regex) Performance

        [Test]
        public void Parse_SimpleDialogue_SubMillisecond()
        {
            // Arrange
            const string input = @"[DIALOGUE]
Hello there, traveler! Welcome to our village.
[/DIALOGUE]";

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
                _parser.Parse(input);

            // Act
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TimedIterations; i++)
                _parser.Parse(input);
            sw.Stop();

            var avgMs = sw.Elapsed.TotalMilliseconds / TimedIterations;

            // Assert
            Assert.That(avgMs, Is.LessThan(MaxParseTimeMs),
                $"Parse (simple dialogue) took {avgMs:F4}ms avg, expected < {MaxParseTimeMs}ms");

            TestContext.WriteLine($"Parse (simple dialogue): {avgMs:F4}ms avg over {TimedIterations} iterations");
        }

        [Test]
        public void Parse_WithMutations_SubMillisecond()
        {
            // Arrange
            const string input = @"[DIALOGUE]
I remember you mentioned the king earlier. That's interesting.
[/DIALOGUE]

[MUTATIONS]
- ADD_BELIEF: Player is curious about royalty
- UPDATE_WORLD: player_interest = politics
[/MUTATIONS]

[INTERNAL]
The player seems interested in the political situation.
[/INTERNAL]";

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
                _parser.Parse(input);

            // Act
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TimedIterations; i++)
                _parser.Parse(input);
            sw.Stop();

            var avgMs = sw.Elapsed.TotalMilliseconds / TimedIterations;

            // Assert
            Assert.That(avgMs, Is.LessThan(MaxParseTimeMs),
                $"Parse (with mutations) took {avgMs:F4}ms avg, expected < {MaxParseTimeMs}ms");

            TestContext.WriteLine($"Parse (with mutations): {avgMs:F4}ms avg over {TimedIterations} iterations");
        }

        [Test]
        public void Parse_LongDialogue_SubMillisecond()
        {
            // Arrange - Generate a long dialogue (1000+ characters)
            var sb = new StringBuilder();
            sb.AppendLine("[DIALOGUE]");
            for (int i = 0; i < 20; i++)
            {
                sb.AppendLine($"This is line {i} of a longer dialogue to test parsing performance with substantial content.");
            }
            sb.AppendLine("[/DIALOGUE]");
            var input = sb.ToString();

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
                _parser.Parse(input);

            // Act
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TimedIterations; i++)
                _parser.Parse(input);
            sw.Stop();

            var avgMs = sw.Elapsed.TotalMilliseconds / TimedIterations;

            // Assert
            Assert.That(avgMs, Is.LessThan(MaxParseTimeMs),
                $"Parse (long dialogue, {input.Length} chars) took {avgMs:F4}ms avg, expected < {MaxParseTimeMs}ms");

            TestContext.WriteLine($"Parse (long dialogue, {input.Length} chars): {avgMs:F4}ms avg over {TimedIterations} iterations");
        }

        #endregion

        #region OutputParser.ParseStructured (JSON) Performance

        [Test]
        public void ParseStructured_ValidJson_SubMillisecond()
        {
            // Arrange
            const string json = @"{
                ""dialogue"": ""Hello there, traveler! Welcome to our village."",
                ""internal_thought"": ""This person seems friendly."",
                ""mutations"": []
            }";

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
                _structuredParser.ParseStructured(json);

            // Act
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TimedIterations; i++)
                _structuredParser.ParseStructured(json);
            sw.Stop();

            var avgMs = sw.Elapsed.TotalMilliseconds / TimedIterations;

            // Assert
            Assert.That(avgMs, Is.LessThan(MaxParseTimeMs),
                $"ParseStructured (valid JSON) took {avgMs:F4}ms avg, expected < {MaxParseTimeMs}ms");

            TestContext.WriteLine($"ParseStructured (valid JSON): {avgMs:F4}ms avg over {TimedIterations} iterations");
        }

        [Test]
        public void ParseStructured_WithMutations_SubMillisecond()
        {
            // Arrange
            const string json = @"{
                ""dialogue"": ""I see you've returned. The king will be pleased."",
                ""internal_thought"": ""This visitor has proven trustworthy."",
                ""mutations"": [
                    { ""type"": ""ADD_BELIEF"", ""content"": ""Player is trustworthy"" },
                    { ""type"": ""UPDATE_WORLD"", ""key"": ""player_reputation"", ""value"": ""good"" },
                    { ""type"": ""ADD_MEMORY"", ""content"": ""Player returned after helping with the quest"" }
                ]
            }";

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
                _structuredParser.ParseStructured(json);

            // Act
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TimedIterations; i++)
                _structuredParser.ParseStructured(json);
            sw.Stop();

            var avgMs = sw.Elapsed.TotalMilliseconds / TimedIterations;

            // Assert
            Assert.That(avgMs, Is.LessThan(MaxParseTimeMs),
                $"ParseStructured (with mutations) took {avgMs:F4}ms avg, expected < {MaxParseTimeMs}ms");

            TestContext.WriteLine($"ParseStructured (with mutations): {avgMs:F4}ms avg over {TimedIterations} iterations");
        }

        [Test]
        public void ParseStructured_ComplexJson_SubMillisecond()
        {
            // Arrange - Complex JSON with many fields
            var sb = new StringBuilder();
            sb.Append(@"{
                ""dialogue"": ""This is a complex response with many mutations and details."",
                ""internal_thought"": ""Processing complex game state."",
                ""mutations"": [");

            for (int i = 0; i < 10; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append($@"
                    {{ ""type"": ""ADD_BELIEF"", ""content"": ""Belief {i} about the world state"" }}");
            }
            sb.Append(@"
                ]
            }");
            var json = sb.ToString();

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
                _structuredParser.ParseStructured(json);

            // Act
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TimedIterations; i++)
                _structuredParser.ParseStructured(json);
            sw.Stop();

            var avgMs = sw.Elapsed.TotalMilliseconds / TimedIterations;

            // Assert
            Assert.That(avgMs, Is.LessThan(MaxParseTimeMs),
                $"ParseStructured (complex JSON, {json.Length} chars) took {avgMs:F4}ms avg, expected < {MaxParseTimeMs}ms");

            TestContext.WriteLine($"ParseStructured (complex JSON, {json.Length} chars): {avgMs:F4}ms avg over {TimedIterations} iterations");
        }

        #endregion

        #region OutputParser.ParseAuto Performance

        [Test]
        public void ParseAuto_DetectsRegex_SubMillisecond()
        {
            // Arrange
            const string input = @"[DIALOGUE]
Hello there!
[/DIALOGUE]";

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
                _parser.ParseAuto(input, isStructuredOutput: false);

            // Act
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TimedIterations; i++)
                _parser.ParseAuto(input, isStructuredOutput: false);
            sw.Stop();

            var avgMs = sw.Elapsed.TotalMilliseconds / TimedIterations;

            // Assert
            Assert.That(avgMs, Is.LessThan(MaxParseTimeMs),
                $"ParseAuto (regex path) took {avgMs:F4}ms avg, expected < {MaxParseTimeMs}ms");

            TestContext.WriteLine($"ParseAuto (regex path): {avgMs:F4}ms avg over {TimedIterations} iterations");
        }

        [Test]
        public void ParseAuto_DetectsJson_SubMillisecond()
        {
            // Arrange
            const string json = @"{ ""dialogue"": ""Hello!"", ""internal_thought"": ""Greeting."", ""mutations"": [] }";

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
                _structuredParser.ParseAuto(json, isStructuredOutput: true);

            // Act
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TimedIterations; i++)
                _structuredParser.ParseAuto(json, isStructuredOutput: true);
            sw.Stop();

            var avgMs = sw.Elapsed.TotalMilliseconds / TimedIterations;

            // Assert
            Assert.That(avgMs, Is.LessThan(MaxParseTimeMs),
                $"ParseAuto (JSON path) took {avgMs:F4}ms avg, expected < {MaxParseTimeMs}ms");

            TestContext.WriteLine($"ParseAuto (JSON path): {avgMs:F4}ms avg over {TimedIterations} iterations");
        }

        #endregion

        #region ContextSerializer Performance

        [Test]
        public void ContextSerializer_Serialize_SubMillisecond()
        {
            // Arrange
            var context = new ContextJsonSchema
            {
                SchemaVersion = "1.0",
                Context = new ContextSection
                {
                    CanonicalFacts = new List<string> { "The king is Arthur", "The kingdom is peaceful" },
                    WorldState = new List<WorldStateEntry>
                    {
                        new WorldStateEntry { Key = "location", Value = "Village square" },
                        new WorldStateEntry { Key = "time_of_day", Value = "morning" }
                    },
                    EpisodicMemories = new List<EpisodicMemoryEntry>
                    {
                        new EpisodicMemoryEntry { Content = "Player arrived yesterday", Recency = 0.9f }
                    }
                },
                Dialogue = new DialogueSection
                {
                    PlayerInput = "Hello, elder!"
                }
            };

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
                ContextSerializer.Serialize(context);

            // Act
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TimedIterations; i++)
                ContextSerializer.Serialize(context);
            sw.Stop();

            var avgMs = sw.Elapsed.TotalMilliseconds / TimedIterations;

            // Assert
            Assert.That(avgMs, Is.LessThan(MaxParseTimeMs),
                $"ContextSerializer.Serialize took {avgMs:F4}ms avg, expected < {MaxParseTimeMs}ms");

            TestContext.WriteLine($"ContextSerializer.Serialize: {avgMs:F4}ms avg over {TimedIterations} iterations");
        }

        [Test]
        public void ContextSerializer_Deserialize_SubMillisecond()
        {
            // Arrange
            const string json = @"{
                ""schema_version"": ""1.0"",
                ""persona"": {
                    ""name"": ""Elder Miriam"",
                    ""role"": ""Village elder"",
                    ""personality_traits"": [""wise"", ""patient""]
                },
                ""world_state"": {
                    ""location"": ""Village square"",
                    ""time_of_day"": ""morning""
                },
                ""interaction"": {
                    ""player_input"": ""Hello!"",
                    ""trigger_reason"": ""player_utterance""
                }
            }";

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
                ContextSerializer.Deserialize(json);

            // Act
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TimedIterations; i++)
                ContextSerializer.Deserialize(json);
            sw.Stop();

            var avgMs = sw.Elapsed.TotalMilliseconds / TimedIterations;

            // Assert
            Assert.That(avgMs, Is.LessThan(MaxParseTimeMs),
                $"ContextSerializer.Deserialize took {avgMs:F4}ms avg, expected < {MaxParseTimeMs}ms");

            TestContext.WriteLine($"ContextSerializer.Deserialize: {avgMs:F4}ms avg over {TimedIterations} iterations");
        }

        [Test]
        public void ContextSerializer_SerializeCompact_SubMillisecond()
        {
            // Arrange
            var context = new ContextJsonSchema
            {
                SchemaVersion = "1.0",
                Context = new ContextSection
                {
                    CanonicalFacts = new List<string> { "Test fact" }
                },
                Dialogue = new DialogueSection { PlayerInput = "Hello" }
            };

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
                ContextSerializer.SerializeCompact(context);

            // Act
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TimedIterations; i++)
                ContextSerializer.SerializeCompact(context);
            sw.Stop();

            var avgMs = sw.Elapsed.TotalMilliseconds / TimedIterations;

            // Assert
            Assert.That(avgMs, Is.LessThan(MaxParseTimeMs),
                $"ContextSerializer.SerializeCompact took {avgMs:F4}ms avg, expected < {MaxParseTimeMs}ms");

            TestContext.WriteLine($"ContextSerializer.SerializeCompact: {avgMs:F4}ms avg over {TimedIterations} iterations");
        }

        #endregion

        #region Summary Report

        [Test]
        [Order(int.MaxValue)] // Run last
        public void PerformanceSummary_AllPathsSubMillisecond()
        {
            // This test aggregates all parsing paths and reports a summary
            var results = new System.Collections.Generic.List<(string Name, double AvgMs)>();

            // Test each path
            results.Add(MeasureParsing("Parse (simple)",
                @"[DIALOGUE]Hello[/DIALOGUE]",
                s => _parser.Parse(s)));

            results.Add(MeasureParsing("ParseStructured (simple)",
                @"{""dialogue"":""Hello"",""internal_thought"":"""",""mutations"":[]}",
                s => _structuredParser.ParseStructured(s)));

            results.Add(MeasureParsing("ParseAuto (regex)",
                @"[DIALOGUE]Hello[/DIALOGUE]",
                s => _parser.ParseAuto(s, false)));

            results.Add(MeasureParsing("ParseAuto (JSON)",
                @"{""dialogue"":""Hello"",""internal_thought"":"""",""mutations"":[]}",
                s => _structuredParser.ParseAuto(s, true)));

            // Report
            TestContext.WriteLine("\n=== PARSING PERFORMANCE SUMMARY ===\n");
            TestContext.WriteLine($"{"Path",-30} {"Avg (ms)",-12} {"Status",-10}");
            TestContext.WriteLine(new string('-', 52));

            bool allPassed = true;
            foreach (var (name, avgMs) in results)
            {
                var status = avgMs < MaxParseTimeMs ? "PASS" : "FAIL";
                if (avgMs >= MaxParseTimeMs) allPassed = false;
                TestContext.WriteLine($"{name,-30} {avgMs,-12:F4} {status,-10}");
            }

            TestContext.WriteLine(new string('-', 52));
            TestContext.WriteLine($"\nClaim: Sub-millisecond parsing for all paths");
            TestContext.WriteLine($"Result: {(allPassed ? "VERIFIED" : "FAILED")}");

            Assert.That(allPassed, Is.True,
                "Not all parsing paths achieved sub-millisecond performance");
        }

        private (string Name, double AvgMs) MeasureParsing(string name, string input, Action<string> parseAction)
        {
            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
                parseAction(input);

            // Measure
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TimedIterations; i++)
                parseAction(input);
            sw.Stop();

            return (name, sw.Elapsed.TotalMilliseconds / TimedIterations);
        }

        #endregion
    }
}
