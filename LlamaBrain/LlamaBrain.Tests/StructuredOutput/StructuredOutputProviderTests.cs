using System;
using NUnit.Framework;
using LlamaBrain.Core.StructuredOutput;

namespace LlamaBrain.Tests.StructuredOutput
{
    /// <summary>
    /// Tests for LlamaCppStructuredOutputProvider.
    /// </summary>
    public class StructuredOutputProviderTests
    {
        private IStructuredOutputProvider provider = null!;

        [SetUp]
        public void SetUp()
        {
            provider = LlamaCppStructuredOutputProvider.Instance;
        }

        #region SupportsFormat

        [Test]
        public void SupportsFormat_None_ReturnsTrue()
        {
            // Act
            var result = provider.SupportsFormat(StructuredOutputFormat.None);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void SupportsFormat_JsonSchema_ReturnsTrue()
        {
            // Act
            var result = provider.SupportsFormat(StructuredOutputFormat.JsonSchema);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void SupportsFormat_Grammar_ReturnsTrue()
        {
            // Act
            var result = provider.SupportsFormat(StructuredOutputFormat.Grammar);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void SupportsFormat_ResponseFormat_ReturnsTrue()
        {
            // Act
            var result = provider.SupportsFormat(StructuredOutputFormat.ResponseFormat);

            // Assert
            Assert.That(result, Is.True);
        }

        #endregion

        #region BuildParameters

        [Test]
        public void BuildParameters_JsonSchema_ReturnsJsonSchemaParameters()
        {
            // Arrange
            var schema = @"{ ""type"": ""object"", ""properties"": { ""name"": { ""type"": ""string"" } } }";

            // Act
            var result = provider.BuildParameters(schema, StructuredOutputFormat.JsonSchema);

            // Assert
            Assert.That(result.Format, Is.EqualTo(StructuredOutputFormat.JsonSchema));
            Assert.That(result.JsonSchema, Is.EqualTo(schema));
            Assert.That(result.Grammar, Is.Null);
        }

        [Test]
        public void BuildParameters_Grammar_ReturnsGrammarParameters()
        {
            // Arrange
            var schema = @"{ ""type"": ""object"", ""properties"": { ""name"": { ""type"": ""string"" } } }";

            // Act
            var result = provider.BuildParameters(schema, StructuredOutputFormat.Grammar);

            // Assert
            Assert.That(result.Format, Is.EqualTo(StructuredOutputFormat.Grammar));
            Assert.That(result.Grammar, Is.Not.Null);
            Assert.That(result.Grammar, Is.Not.Empty);
        }

        [Test]
        public void BuildParameters_ResponseFormat_ReturnsJsonModeParameters()
        {
            // Arrange
            var schema = @"{ ""type"": ""object"", ""properties"": { ""name"": { ""type"": ""string"" } } }";

            // Act
            var result = provider.BuildParameters(schema, StructuredOutputFormat.ResponseFormat);

            // Assert
            Assert.That(result.Format, Is.EqualTo(StructuredOutputFormat.ResponseFormat));
            Assert.That(result.ResponseFormatType, Is.EqualTo("json_object"));
        }

        [Test]
        public void BuildParameters_None_ReturnsEmptyParameters()
        {
            // Arrange
            var schema = @"{ ""type"": ""object"" }";

            // Act
            var result = provider.BuildParameters(schema, StructuredOutputFormat.None);

            // Assert
            Assert.That(result.Format, Is.EqualTo(StructuredOutputFormat.None));
            Assert.That(result.JsonSchema, Is.Null);
            Assert.That(result.Grammar, Is.Null);
        }

        [Test]
        public void BuildParameters_EmptySchema_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                provider.BuildParameters("", StructuredOutputFormat.JsonSchema));
        }

        [Test]
        public void BuildParameters_InvalidJson_ThrowsArgumentException()
        {
            // Arrange
            var invalidSchema = "{ not valid json }";

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                provider.BuildParameters(invalidSchema, StructuredOutputFormat.JsonSchema));
        }

        [Test]
        public void BuildParameters_MissingType_ThrowsArgumentException()
        {
            // Arrange
            var schemaWithoutType = @"{ ""properties"": { ""name"": { ""value"": ""test"" } } }";

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                provider.BuildParameters(schemaWithoutType, StructuredOutputFormat.JsonSchema));
        }

        #endregion

        #region ValidateSchema

        [Test]
        public void ValidateSchema_ValidSchema_ReturnsTrue()
        {
            // Arrange
            var schema = @"{ ""type"": ""object"", ""properties"": {} }";

            // Act
            var result = provider.ValidateSchema(schema, out var error);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void ValidateSchema_EmptyString_ReturnsFalse()
        {
            // Act
            var result = provider.ValidateSchema("", out var error);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null);
        }

        [Test]
        public void ValidateSchema_NullString_ReturnsFalse()
        {
            // Act
            var result = provider.ValidateSchema(null!, out var error);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null);
        }

        [Test]
        public void ValidateSchema_InvalidJson_ReturnsFalse()
        {
            // Act
            var result = provider.ValidateSchema("{ invalid }", out var error);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(error, Does.Contain("not valid JSON"));
        }

        #endregion

        #region Singleton

        [Test]
        public void Instance_ReturnsSameInstance()
        {
            // Act
            var instance1 = LlamaCppStructuredOutputProvider.Instance;
            var instance2 = LlamaCppStructuredOutputProvider.Instance;

            // Assert
            Assert.That(instance1, Is.SameAs(instance2));
        }

        #endregion

        #region StructuredOutputParameters Factory Methods

        [Test]
        public void ForJsonSchema_CreatesCorrectParameters()
        {
            // Arrange
            var schema = @"{ ""type"": ""string"" }";

            // Act
            var result = StructuredOutputParameters.ForJsonSchema(schema);

            // Assert
            Assert.That(result.Format, Is.EqualTo(StructuredOutputFormat.JsonSchema));
            Assert.That(result.JsonSchema, Is.EqualTo(schema));
        }

        [Test]
        public void ForGrammar_CreatesCorrectParameters()
        {
            // Arrange
            var grammar = "root ::= string";

            // Act
            var result = StructuredOutputParameters.ForGrammar(grammar);

            // Assert
            Assert.That(result.Format, Is.EqualTo(StructuredOutputFormat.Grammar));
            Assert.That(result.Grammar, Is.EqualTo(grammar));
        }

        [Test]
        public void ForJsonMode_CreatesCorrectParameters()
        {
            // Act
            var result = StructuredOutputParameters.ForJsonMode();

            // Assert
            Assert.That(result.Format, Is.EqualTo(StructuredOutputFormat.ResponseFormat));
            Assert.That(result.ResponseFormatType, Is.EqualTo("json_object"));
        }

        [Test]
        public void None_CreatesEmptyParameters()
        {
            // Act
            var result = StructuredOutputParameters.None;

            // Assert
            Assert.That(result.Format, Is.EqualTo(StructuredOutputFormat.None));
            Assert.That(result.JsonSchema, Is.Null);
            Assert.That(result.Grammar, Is.Null);
            Assert.That(result.ResponseFormatType, Is.Null);
        }

        #endregion

        #region StructuredOutputConfig

        [Test]
        public void StructuredOutputConfig_Default_HasCorrectValues()
        {
            // Act
            var config = StructuredOutputConfig.Default;

            // Assert
            Assert.That(config.PreferredFormat, Is.EqualTo(StructuredOutputFormat.JsonSchema));
            Assert.That(config.FallbackToPromptInjection, Is.True);
            Assert.That(config.ValidateSchema, Is.True);
        }

        [Test]
        public void StructuredOutputConfig_PromptInjectionOnly_DisablesNativeOutput()
        {
            // Act
            var config = StructuredOutputConfig.PromptInjectionOnly;

            // Assert
            Assert.That(config.PreferredFormat, Is.EqualTo(StructuredOutputFormat.None));
            Assert.That(config.FallbackToPromptInjection, Is.True);
        }

        [Test]
        public void StructuredOutputConfig_Strict_HasNoFallback()
        {
            // Act
            var config = StructuredOutputConfig.Strict;

            // Assert
            Assert.That(config.PreferredFormat, Is.EqualTo(StructuredOutputFormat.JsonSchema));
            Assert.That(config.FallbackToPromptInjection, Is.False);
            Assert.That(config.ValidateSchema, Is.True);
            Assert.That(config.StrictSchemaValidation, Is.True);
        }

        #endregion
    }
}
