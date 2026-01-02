using NUnit.Framework;
using LlamaBrain.Core.StructuredOutput;

namespace LlamaBrain.Tests.StructuredOutput
{
    /// <summary>
    /// Tests for JsonSchemaBuilder.
    /// </summary>
    public class JsonSchemaBuilderTests
    {
        #region ParsedOutput Schema

        [Test]
        public void BuildParsedOutputSchema_ReturnsValidJson()
        {
            // Act
            var schema = JsonSchemaBuilder.BuildParsedOutputSchema();

            // Assert
            Assert.That(schema, Is.Not.Null);
            Assert.That(schema, Is.Not.Empty);
            Assert.That(LlamaBrain.Utilities.JsonUtils.IsValidJson(schema), Is.True);
        }

        [Test]
        public void ParsedOutputSchema_ContainsDialogueText()
        {
            // Act
            var schema = JsonSchemaBuilder.ParsedOutputSchema;

            // Assert
            Assert.That(schema, Does.Contain("dialogueText"));
            Assert.That(schema, Does.Contain("\"type\": \"string\""));
        }

        [Test]
        public void ParsedOutputSchema_ContainsProposedMutations()
        {
            // Act
            var schema = JsonSchemaBuilder.ParsedOutputSchema;

            // Assert
            Assert.That(schema, Does.Contain("proposedMutations"));
            Assert.That(schema, Does.Contain("\"type\": \"array\""));
        }

        [Test]
        public void ParsedOutputSchema_ContainsWorldIntents()
        {
            // Act
            var schema = JsonSchemaBuilder.ParsedOutputSchema;

            // Assert
            Assert.That(schema, Does.Contain("worldIntents"));
        }

        [Test]
        public void ParsedOutputSchema_ContainsMutationTypeEnum()
        {
            // Act
            var schema = JsonSchemaBuilder.ParsedOutputSchema;

            // Assert
            Assert.That(schema, Does.Contain("AppendEpisodic"));
            Assert.That(schema, Does.Contain("TransformBelief"));
            Assert.That(schema, Does.Contain("TransformRelationship"));
            Assert.That(schema, Does.Contain("EmitWorldIntent"));
        }

        [Test]
        public void ParsedOutputSchema_DialogueTextIsRequired()
        {
            // Act
            var schema = JsonSchemaBuilder.ParsedOutputSchema;

            // Assert
            Assert.That(schema, Does.Contain("\"required\": [\"dialogueText\"]"));
        }

        #endregion

        #region DialogueOnly Schema

        [Test]
        public void DialogueOnlySchema_IsValidJson()
        {
            // Act
            var schema = JsonSchemaBuilder.DialogueOnlySchema;

            // Assert
            Assert.That(LlamaBrain.Utilities.JsonUtils.IsValidJson(schema), Is.True);
        }

        [Test]
        public void DialogueOnlySchema_ContainsDialogueTextAndEmotion()
        {
            // Act
            var schema = JsonSchemaBuilder.DialogueOnlySchema;

            // Assert
            Assert.That(schema, Does.Contain("dialogueText"));
            Assert.That(schema, Does.Contain("emotion"));
            Assert.That(schema, Does.Contain("neutral"));
            Assert.That(schema, Does.Contain("happy"));
        }

        #endregion

        #region Schema Validation

        [Test]
        public void ValidateSchema_ValidSchema_ReturnsTrue()
        {
            // Arrange
            var schema = @"{ ""type"": ""object"", ""properties"": { ""name"": { ""type"": ""string"" } } }";

            // Act
            var result = JsonSchemaBuilder.ValidateSchema(schema, out var error);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void ValidateSchema_EmptyString_ReturnsFalse()
        {
            // Act
            var result = JsonSchemaBuilder.ValidateSchema("", out var error);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null);
            Assert.That(error, Does.Contain("empty"));
        }

        [Test]
        public void ValidateSchema_InvalidJson_ReturnsFalse()
        {
            // Act
            var result = JsonSchemaBuilder.ValidateSchema("{ invalid json }", out var error);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(error, Does.Contain("not valid JSON"));
        }

        [Test]
        public void ValidateSchema_MissingType_ReturnsFalse()
        {
            // Arrange
            var schema = @"{ ""properties"": { ""name"": { ""value"": ""string"" } } }";

            // Act
            var result = JsonSchemaBuilder.ValidateSchema(schema, out var error);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(error, Does.Contain("type"));
        }

        #endregion

        #region CreateObjectSchema

        [Test]
        public void CreateObjectSchema_SimpleProperties_ReturnsValidSchema()
        {
            // Arrange
            var properties = new System.Collections.Generic.Dictionary<string, (string type, string description)>
            {
                { "name", ("string", "The user's name") },
                { "age", ("integer", "The user's age") }
            };

            // Act
            var schema = JsonSchemaBuilder.CreateObjectSchema(properties, new[] { "name" });

            // Assert
            Assert.That(LlamaBrain.Utilities.JsonUtils.IsValidJson(schema), Is.True);
            Assert.That(schema, Does.Contain("\"name\""));
            Assert.That(schema, Does.Contain("\"age\""));
            Assert.That(schema, Does.Contain("\"required\""));
        }

        #endregion

        #region BuildFromType

        [Test]
        public void BuildFromType_SimpleClass_ReturnsValidSchema()
        {
            // Act
            var schema = JsonSchemaBuilder.BuildFromType<SimpleTestClass>();

            // Assert
            Assert.That(LlamaBrain.Utilities.JsonUtils.IsValidJson(schema), Is.True);
            Assert.That(schema, Does.Contain("name"));
            Assert.That(schema, Does.Contain("count"));
        }

        [Test]
        public void BuildFromType_WithEnum_ContainsEnumValues()
        {
            // Act
            var schema = JsonSchemaBuilder.BuildFromType<ClassWithEnum>();

            // Assert
            Assert.That(schema, Does.Contain("status"));
            Assert.That(schema, Does.Contain("enum"));
        }

        #endregion

        #region Determinism

        [Test]
        public void ParsedOutputSchema_IsDeterministic()
        {
            // Act
            var schema1 = JsonSchemaBuilder.BuildParsedOutputSchema();
            var schema2 = JsonSchemaBuilder.BuildParsedOutputSchema();

            // Assert
            Assert.That(schema1, Is.EqualTo(schema2));
        }

        [Test]
        public void BuildFromType_IsDeterministic()
        {
            // Act
            var schema1 = JsonSchemaBuilder.BuildFromType<SimpleTestClass>();
            var schema2 = JsonSchemaBuilder.BuildFromType<SimpleTestClass>();

            // Assert
            Assert.That(schema1, Is.EqualTo(schema2));
        }

        #endregion

        #region Test Helper Classes

        private class SimpleTestClass
        {
            public string Name { get; set; } = "";
            public int Count { get; set; }
            public bool Active { get; set; }
        }

        private class ClassWithEnum
        {
            public TestStatus Status { get; set; }
        }

        private enum TestStatus
        {
            Pending,
            Active,
            Completed
        }

        #endregion
    }
}
