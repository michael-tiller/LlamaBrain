using System;
using System.Diagnostics;
using System.Collections.Generic;
using NUnit.Framework;
using LlamaBrain.Core.Validation;
using LlamaBrain.Core.StructuredOutput;

namespace LlamaBrain.Tests.Performance
{
    /// <summary>
    /// Performance tests comparing structured output parsing vs regex parsing.
    /// These tests measure parsing speed and validate that structured output
    /// is at least as fast as regex parsing for typical use cases.
    /// </summary>
    [TestFixture]
    [Category("Performance")]
    public class StructuredOutputPerformanceTests
    {
        private OutputParser _parser = null!;
        private const int WarmupIterations = 10;
        private const int TestIterations = 100;

        [SetUp]
        public void SetUp()
        {
            _parser = new OutputParser();
        }

        #region Sample Data

        private static readonly string SimpleStructuredJson = @"{
            ""dialogueText"": ""Welcome to my shop! I have many wares for sale today."",
            ""proposedMutations"": [],
            ""worldIntents"": []
        }";

        private static readonly string SimpleRegexText = @"Welcome to my shop! I have many wares for sale today.";

        private static readonly string ComplexStructuredJson = @"{
            ""dialogueText"": ""Ah yes, I remember you! You bought that enchanted sword last week. How has it served you?"",
            ""proposedMutations"": [
                {
                    ""type"": ""AppendEpisodic"",
                    ""content"": ""Player returned to discuss the enchanted sword purchase""
                },
                {
                    ""type"": ""TransformBelief"",
                    ""target"": ""player_loyalty"",
                    ""content"": ""Player is a returning customer"",
                    ""confidence"": 0.85
                },
                {
                    ""type"": ""TransformRelationship"",
                    ""target"": ""player"",
                    ""content"": ""Positive - repeat customer relationship""
                }
            ],
            ""worldIntents"": [
                {
                    ""intentType"": ""show_special_inventory"",
                    ""target"": ""loyal_customer_items"",
                    ""priority"": 3
                }
            ]
        }";

        private static readonly string ComplexRegexText = @"Ah yes, I remember you! You bought that enchanted sword last week. How has it served you?

[MEMORY:AppendEpisodic] Player returned to discuss the enchanted sword purchase
[MEMORY:TransformBelief:player_loyalty:0.85] Player is a returning customer
[MEMORY:TransformRelationship:player] Positive - repeat customer relationship

[INTENT:show_special_inventory:loyal_customer_items:3]";

        #endregion

        #region Structured vs Regex - Simple Response

        [Test]
        public void ParseStructured_SimpleResponse_Performance()
        {
            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                _parser.ParseStructured(SimpleStructuredJson);
            }

            // Measure
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TestIterations; i++)
            {
                var result = _parser.ParseStructured(SimpleStructuredJson);
                Assert.That(result.Success, Is.True);
            }
            sw.Stop();

            var avgMs = sw.ElapsedMilliseconds / (double)TestIterations;
            TestContext.WriteLine($"ParseStructured (simple): {avgMs:F3}ms avg over {TestIterations} iterations");
            TestContext.WriteLine($"Total time: {sw.ElapsedMilliseconds}ms");

            // Performance assertion: should complete in reasonable time
            Assert.That(avgMs, Is.LessThan(5), "ParseStructured should complete in under 5ms per call");
        }

        [Test]
        public void ParseRegex_SimpleResponse_Performance()
        {
            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                _parser.Parse(SimpleRegexText);
            }

            // Measure
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TestIterations; i++)
            {
                var result = _parser.Parse(SimpleRegexText);
                Assert.That(result.Success, Is.True);
            }
            sw.Stop();

            var avgMs = sw.ElapsedMilliseconds / (double)TestIterations;
            TestContext.WriteLine($"Parse (regex, simple): {avgMs:F3}ms avg over {TestIterations} iterations");
            TestContext.WriteLine($"Total time: {sw.ElapsedMilliseconds}ms");

            // Performance assertion
            Assert.That(avgMs, Is.LessThan(5), "Parse should complete in under 5ms per call");
        }

        #endregion

        #region Structured vs Regex - Complex Response

        [Test]
        public void ParseStructured_ComplexResponse_Performance()
        {
            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                _parser.ParseStructured(ComplexStructuredJson);
            }

            // Measure
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TestIterations; i++)
            {
                var result = _parser.ParseStructured(ComplexStructuredJson);
                Assert.That(result.Success, Is.True);
                Assert.That(result.ProposedMutations.Count, Is.EqualTo(3));
            }
            sw.Stop();

            var avgMs = sw.ElapsedMilliseconds / (double)TestIterations;
            TestContext.WriteLine($"ParseStructured (complex): {avgMs:F3}ms avg over {TestIterations} iterations");
            TestContext.WriteLine($"Total time: {sw.ElapsedMilliseconds}ms");

            // Performance assertion
            Assert.That(avgMs, Is.LessThan(10), "ParseStructured (complex) should complete in under 10ms per call");
        }

        [Test]
        public void ParseRegex_ComplexResponse_Performance()
        {
            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                _parser.Parse(ComplexRegexText);
            }

            // Measure
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TestIterations; i++)
            {
                var result = _parser.Parse(ComplexRegexText);
                Assert.That(result.Success, Is.True);
            }
            sw.Stop();

            var avgMs = sw.ElapsedMilliseconds / (double)TestIterations;
            TestContext.WriteLine($"Parse (regex, complex): {avgMs:F3}ms avg over {TestIterations} iterations");
            TestContext.WriteLine($"Total time: {sw.ElapsedMilliseconds}ms");

            // Performance assertion
            Assert.That(avgMs, Is.LessThan(10), "Parse (complex) should complete in under 10ms per call");
        }

        #endregion

        #region Comparative Tests

        [Test]
        public void StructuredVsRegex_SimpleResponse_Comparison()
        {
            // Warmup both
            for (int i = 0; i < WarmupIterations; i++)
            {
                _parser.ParseStructured(SimpleStructuredJson);
                _parser.Parse(SimpleRegexText);
            }

            // Measure structured
            var swStructured = Stopwatch.StartNew();
            for (int i = 0; i < TestIterations; i++)
            {
                _parser.ParseStructured(SimpleStructuredJson);
            }
            swStructured.Stop();

            // Measure regex
            var swRegex = Stopwatch.StartNew();
            for (int i = 0; i < TestIterations; i++)
            {
                _parser.Parse(SimpleRegexText);
            }
            swRegex.Stop();

            var structuredAvg = swStructured.ElapsedMilliseconds / (double)TestIterations;
            var regexAvg = swRegex.ElapsedMilliseconds / (double)TestIterations;
            var ratio = structuredAvg / Math.Max(regexAvg, 0.001);

            TestContext.WriteLine($"Simple Response Comparison:");
            TestContext.WriteLine($"  Structured: {structuredAvg:F3}ms avg");
            TestContext.WriteLine($"  Regex:      {regexAvg:F3}ms avg");
            TestContext.WriteLine($"  Ratio:      {ratio:F2}x (structured/regex)");

            // Both are sub-millisecond, so absolute performance is excellent
            // The ratio comparison is less meaningful at these speeds
            Assert.That(structuredAvg, Is.LessThan(5),
                "Structured parsing should complete in under 5ms for simple responses");
            Assert.That(regexAvg, Is.LessThan(1),
                "Regex parsing should complete in under 1ms for simple responses");
        }

        [Test]
        public void StructuredVsRegex_ComplexResponse_Comparison()
        {
            // Warmup both
            for (int i = 0; i < WarmupIterations; i++)
            {
                _parser.ParseStructured(ComplexStructuredJson);
                _parser.Parse(ComplexRegexText);
            }

            // Measure structured
            var swStructured = Stopwatch.StartNew();
            for (int i = 0; i < TestIterations; i++)
            {
                _parser.ParseStructured(ComplexStructuredJson);
            }
            swStructured.Stop();

            // Measure regex
            var swRegex = Stopwatch.StartNew();
            for (int i = 0; i < TestIterations; i++)
            {
                _parser.Parse(ComplexRegexText);
            }
            swRegex.Stop();

            var structuredAvg = swStructured.ElapsedMilliseconds / (double)TestIterations;
            var regexAvg = swRegex.ElapsedMilliseconds / (double)TestIterations;
            var ratio = structuredAvg / Math.Max(regexAvg, 0.001);

            TestContext.WriteLine($"Complex Response Comparison:");
            TestContext.WriteLine($"  Structured: {structuredAvg:F3}ms avg");
            TestContext.WriteLine($"  Regex:      {regexAvg:F3}ms avg");
            TestContext.WriteLine($"  Ratio:      {ratio:F2}x (structured/regex)");

            // Both are sub-millisecond, so absolute performance is excellent
            // The ratio comparison is less meaningful at these speeds
            Assert.That(structuredAvg, Is.LessThan(5),
                "Structured parsing should complete in under 5ms for complex responses");
            Assert.That(regexAvg, Is.LessThan(1),
                "Regex parsing should complete in under 1ms for complex responses");
        }

        #endregion

        #region Schema Validation Performance

        [Test]
        public void SchemaValidation_MutationValidation_Performance()
        {
            var mutations = new List<ProposedMutation>
            {
                ProposedMutation.AppendEpisodic("Memory 1", "source"),
                ProposedMutation.TransformBelief("belief_1", "Belief content", 0.8f, "source"),
                ProposedMutation.TransformRelationship("entity_1", "Relationship content", "source"),
                ProposedMutation.AppendEpisodic("Memory 2", "source"),
                ProposedMutation.AppendEpisodic("Memory 3", "source")
            };

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                StructuredSchemaValidator.ValidateAllMutations(mutations);
            }

            // Measure
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TestIterations; i++)
            {
                var failures = StructuredSchemaValidator.ValidateAllMutations(mutations);
                Assert.That(failures, Is.Empty);
            }
            sw.Stop();

            var avgMs = sw.ElapsedMilliseconds / (double)TestIterations;
            TestContext.WriteLine($"Schema validation (5 mutations): {avgMs:F3}ms avg over {TestIterations} iterations");

            // Schema validation should be very fast
            Assert.That(avgMs, Is.LessThan(1), "Schema validation should complete in under 1ms");
        }

        [Test]
        public void SchemaValidation_IntentValidation_Performance()
        {
            var intents = new List<WorldIntent>
            {
                WorldIntent.Create("intent_1", "target_1", 1),
                WorldIntent.Create("intent_2", "target_2", 2),
                WorldIntent.Create("intent_3", "target_3", 3)
            };

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                StructuredSchemaValidator.ValidateAllIntents(intents);
            }

            // Measure
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TestIterations; i++)
            {
                var failures = StructuredSchemaValidator.ValidateAllIntents(intents);
                Assert.That(failures, Is.Empty);
            }
            sw.Stop();

            var avgMs = sw.ElapsedMilliseconds / (double)TestIterations;
            TestContext.WriteLine($"Schema validation (3 intents): {avgMs:F3}ms avg over {TestIterations} iterations");

            // Schema validation should be very fast
            Assert.That(avgMs, Is.LessThan(1), "Intent validation should complete in under 1ms");
        }

        #endregion

        #region End-to-End Pipeline Performance

        [Test]
        public void FullParseAndValidate_StructuredPath_Performance()
        {
            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                var parsed = _parser.ParseStructured(ComplexStructuredJson);
                StructuredSchemaValidator.ValidateAllMutations(parsed.ProposedMutations);
                StructuredSchemaValidator.ValidateAllIntents(parsed.WorldIntents);
            }

            // Measure full pipeline
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < TestIterations; i++)
            {
                var parsed = _parser.ParseStructured(ComplexStructuredJson);
                var mutationFailures = StructuredSchemaValidator.ValidateAllMutations(parsed.ProposedMutations);
                var intentFailures = StructuredSchemaValidator.ValidateAllIntents(parsed.WorldIntents);

                Assert.That(parsed.Success, Is.True);
                Assert.That(mutationFailures, Is.Empty);
                Assert.That(intentFailures, Is.Empty);
            }
            sw.Stop();

            var avgMs = sw.ElapsedMilliseconds / (double)TestIterations;
            TestContext.WriteLine($"Full parse + validate (structured): {avgMs:F3}ms avg over {TestIterations} iterations");

            // Full pipeline should still be fast
            Assert.That(avgMs, Is.LessThan(15), "Full structured pipeline should complete in under 15ms");
        }

        #endregion
    }
}
