using System;
using System.Collections.Generic;
using NUnit.Framework;
using LlamaBrain.Core.Retrieval;

namespace LlamaBrain.Tests.Retrieval
{
    /// <summary>
    /// Tests for HybridRelevanceCalculator.
    /// </summary>
    public class HybridRelevanceCalculatorTests
    {
        private EmbeddingConfig _defaultConfig = null!;
        private HybridRelevanceCalculator _calculator = null!;

        [SetUp]
        public void SetUp()
        {
            _defaultConfig = EmbeddingConfig.Default();
            _calculator = new HybridRelevanceCalculator(_defaultConfig);
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithNullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new HybridRelevanceCalculator(null!));
        }

        [Test]
        public void Constructor_WithValidConfig_Creates()
        {
            var calc = new HybridRelevanceCalculator(EmbeddingConfig.Default());
            Assert.That(calc, Is.Not.Null);
        }

        #endregion

        #region Keyword-Only Mode Tests

        [Test]
        public void CalculateHybridRelevance_KeywordOnlyMode_IgnoresSemanticScores()
        {
            var keywordOnlyConfig = EmbeddingConfig.KeywordOnly();
            var calc = new HybridRelevanceCalculator(keywordOnlyConfig);

            var semanticScores = new Dictionary<string, float> { { "mem-1", 1.0f } };
            var score = calc.CalculateHybridRelevance(
                "mem-1", "hello world", "hello", null, semanticScores);

            // Should only use keyword relevance, not semantic
            // "hello" matches, so should have non-zero keyword score
            Assert.That(score, Is.GreaterThan(0));
            Assert.That(score, Is.LessThanOrEqualTo(1.0f));
        }

        [Test]
        public void CalculateHybridRelevance_WithNullSemanticScores_UsesKeywordOnly()
        {
            var score = _calculator.CalculateHybridRelevance(
                "mem-1", "hello world", "hello", null, null);

            Assert.That(score, Is.GreaterThan(0));
        }

        #endregion

        #region Keyword Relevance Tests

        [Test]
        public void CalculateKeywordRelevance_ExactMatch_ReturnsOne()
        {
            var score = _calculator.CalculateKeywordRelevance(
                "hello world today", "hello world today", null);

            Assert.That(score, Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void CalculateKeywordRelevance_NoMatch_ReturnsZero()
        {
            var score = _calculator.CalculateKeywordRelevance(
                "hello world", "goodbye universe", null);

            Assert.That(score, Is.EqualTo(0f));
        }

        [Test]
        public void CalculateKeywordRelevance_PartialMatch_ReturnsPartialScore()
        {
            var score = _calculator.CalculateKeywordRelevance(
                "hello world today", "hello tomorrow", null);

            // "hello" matches, "tomorrow" doesn't, so partial score
            Assert.That(score, Is.GreaterThan(0));
            Assert.That(score, Is.LessThan(1.0f));
        }

        [Test]
        public void CalculateKeywordRelevance_IgnoresShortWords()
        {
            // Words <= 3 chars are ignored
            var score = _calculator.CalculateKeywordRelevance(
                "the cat sat on mat", "the a is an", null);

            // All words in input are <= 3 chars, so no matches
            Assert.That(score, Is.EqualTo(0f));
        }

        [Test]
        public void CalculateKeywordRelevance_CaseInsensitive()
        {
            var score = _calculator.CalculateKeywordRelevance(
                "HELLO WORLD", "hello world", null);

            Assert.That(score, Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void CalculateKeywordRelevance_EmptyContent_ReturnsZero()
        {
            var score = _calculator.CalculateKeywordRelevance("", "hello world", null);
            Assert.That(score, Is.EqualTo(0f));
        }

        [Test]
        public void CalculateKeywordRelevance_EmptyInput_ReturnsZero()
        {
            var score = _calculator.CalculateKeywordRelevance("hello world", "", null);
            Assert.That(score, Is.EqualTo(0f));
        }

        #endregion

        #region Topic Boost Tests

        [Test]
        public void CalculateKeywordRelevance_WithMatchingTopic_BoostsScore()
        {
            var topics = new List<string> { "weather", "king" };

            var scoreWithoutTopic = _calculator.CalculateKeywordRelevance(
                "the king is here", "random words", null);
            var scoreWithTopic = _calculator.CalculateKeywordRelevance(
                "the king is here", "random words", topics);

            Assert.That(scoreWithTopic, Is.GreaterThan(scoreWithoutTopic));
        }

        [Test]
        public void CalculateKeywordRelevance_WithNonMatchingTopics_NoBoost()
        {
            var topics = new List<string> { "weather", "moon" };

            var scoreWithoutTopic = _calculator.CalculateKeywordRelevance(
                "hello world", "hello", null);
            var scoreWithTopic = _calculator.CalculateKeywordRelevance(
                "hello world", "hello", topics);

            Assert.That(scoreWithTopic, Is.EqualTo(scoreWithoutTopic));
        }

        [Test]
        public void CalculateKeywordRelevance_TopicBoost_ClampedToOne()
        {
            var topics = new List<string> { "hello" };

            // Full keyword match + topic boost shouldn't exceed 1.0
            var score = _calculator.CalculateKeywordRelevance(
                "hello world", "hello world", topics);

            Assert.That(score, Is.LessThanOrEqualTo(1.0f));
        }

        #endregion

        #region Hybrid Score Tests

        [Test]
        public void CalculateHybridRelevance_CombinesScores()
        {
            var config = new EmbeddingConfig
            {
                EnableSemanticRetrieval = true,
                KeywordWeight = 0.3f,
                SemanticWeight = 0.7f
            };
            var calc = new HybridRelevanceCalculator(config);

            var semanticScores = new Dictionary<string, float> { { "mem-1", 0.8f } };
            var score = calc.CalculateHybridRelevance(
                "mem-1", "hello world", "hello world", null, semanticScores);

            // Keyword score = 1.0 (full match), semantic = 0.8
            // Expected: 0.3 * 1.0 + 0.7 * 0.8 = 0.3 + 0.56 = 0.86
            Assert.That(score, Is.EqualTo(0.86f).Within(0.05f));
        }

        [Test]
        public void CalculateHybridRelevance_MissingSemanticScore_UsesZero()
        {
            var config = new EmbeddingConfig
            {
                EnableSemanticRetrieval = true,
                KeywordWeight = 0.3f,
                SemanticWeight = 0.7f
            };
            var calc = new HybridRelevanceCalculator(config);

            var semanticScores = new Dictionary<string, float> { { "other-mem", 1.0f } };
            var score = calc.CalculateHybridRelevance(
                "mem-1", "hello world", "hello world", null, semanticScores);

            // Keyword score = 1.0, semantic = 0 (not in dict)
            // Expected: 0.3 * 1.0 + 0.7 * 0 = 0.3
            Assert.That(score, Is.EqualTo(0.3f).Within(0.01f));
        }

        [Test]
        public void CalculateHybridRelevance_SemanticOnly()
        {
            var config = new EmbeddingConfig
            {
                EnableSemanticRetrieval = true,
                KeywordWeight = 0f,
                SemanticWeight = 1.0f
            };
            var calc = new HybridRelevanceCalculator(config);

            var semanticScores = new Dictionary<string, float> { { "mem-1", 0.75f } };
            var score = calc.CalculateHybridRelevance(
                "mem-1", "no matching words", "completely different", null, semanticScores);

            // Should be purely semantic
            Assert.That(score, Is.EqualTo(0.75f).Within(0.001f));
        }

        #endregion

        #region IsRelevantToTopics Tests

        [Test]
        public void IsRelevantToTopics_MatchingTopic_ReturnsTrue()
        {
            var topics = new List<string> { "weather", "king" };
            var result = _calculator.IsRelevantToTopics("The king arrived", topics);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsRelevantToTopics_NoMatch_ReturnsFalse()
        {
            var topics = new List<string> { "weather", "moon" };
            var result = _calculator.IsRelevantToTopics("The king arrived", topics);

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsRelevantToTopics_CaseInsensitive()
        {
            var topics = new List<string> { "KING" };
            var result = _calculator.IsRelevantToTopics("the king arrived", topics);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsRelevantToTopics_EmptyContent_ReturnsFalse()
        {
            var topics = new List<string> { "king" };
            var result = _calculator.IsRelevantToTopics("", topics);

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsRelevantToTopics_EmptyTopics_ReturnsFalse()
        {
            var result = _calculator.IsRelevantToTopics("The king arrived", new List<string>());
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsRelevantToTopics_NullTopics_ReturnsFalse()
        {
            var result = _calculator.IsRelevantToTopics("The king arrived", null!);
            Assert.That(result, Is.False);
        }

        #endregion

        #region ExtractKeywords Tests

        [Test]
        public void ExtractKeywords_ExtractsLongWords()
        {
            var keywords = _calculator.ExtractKeywords("Hello world today");

            Assert.That(keywords, Contains.Item("hello"));
            Assert.That(keywords, Contains.Item("world"));
            Assert.That(keywords, Contains.Item("today"));
        }

        [Test]
        public void ExtractKeywords_IgnoresShortWords()
        {
            var keywords = _calculator.ExtractKeywords("The cat is here");

            Assert.That(keywords, Does.Not.Contain("the"));
            Assert.That(keywords, Does.Not.Contain("cat"));
            Assert.That(keywords, Does.Not.Contain("is"));
            Assert.That(keywords, Contains.Item("here"));
        }

        [Test]
        public void ExtractKeywords_RemovesDuplicates()
        {
            var keywords = _calculator.ExtractKeywords("hello hello hello");

            Assert.That(keywords.Count, Is.EqualTo(1));
            Assert.That(keywords, Contains.Item("hello"));
        }

        [Test]
        public void ExtractKeywords_HandlesPunctuation()
        {
            var keywords = _calculator.ExtractKeywords("Hello, world! How are you?");

            Assert.That(keywords, Contains.Item("hello"));
            Assert.That(keywords, Contains.Item("world"));
        }

        [Test]
        public void ExtractKeywords_EmptyString_ReturnsEmpty()
        {
            var keywords = _calculator.ExtractKeywords("");
            Assert.That(keywords, Is.Empty);
        }

        [Test]
        public void ExtractKeywords_NullString_ReturnsEmpty()
        {
            var keywords = _calculator.ExtractKeywords(null!);
            Assert.That(keywords, Is.Empty);
        }

        #endregion

        #region Config Factory Tests

        [Test]
        public void EmbeddingConfig_KeywordOnly_DisablesSemantic()
        {
            var config = EmbeddingConfig.KeywordOnly();
            Assert.That(config.EnableSemanticRetrieval, Is.False);
        }

        [Test]
        public void EmbeddingConfig_Default_EnablesSemantic()
        {
            var config = EmbeddingConfig.Default();
            Assert.That(config.EnableSemanticRetrieval, Is.True);
        }

        [Test]
        public void EmbeddingConfig_SemanticHeavy_HighSemanticWeight()
        {
            var config = EmbeddingConfig.SemanticHeavy();
            Assert.That(config.SemanticWeight, Is.GreaterThan(config.KeywordWeight));
        }

        [Test]
        public void EmbeddingConfig_Balanced_EqualWeights()
        {
            var config = EmbeddingConfig.Balanced();
            Assert.That(config.SemanticWeight, Is.EqualTo(config.KeywordWeight).Within(0.01f));
        }

        #endregion

        #region Determinism Tests

        [Test]
        public void CalculateHybridRelevance_SameInputs_ProducesIdenticalResults()
        {
            var semanticScores = new Dictionary<string, float> { { "mem-1", 0.75f } };

            var score1 = _calculator.CalculateHybridRelevance(
                "mem-1", "hello world", "hello there", null, semanticScores);
            var score2 = _calculator.CalculateHybridRelevance(
                "mem-1", "hello world", "hello there", null, semanticScores);
            var score3 = _calculator.CalculateHybridRelevance(
                "mem-1", "hello world", "hello there", null, semanticScores);

            Assert.That(score1, Is.EqualTo(score2));
            Assert.That(score2, Is.EqualTo(score3));
        }

        #endregion
    }
}
