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

        #region AnalysisSchema Tests

        [Test]
        public void AnalysisSchema_IsValidJson()
        {
            // Act
            var schema = JsonSchemaBuilder.AnalysisSchema;

            // Assert
            Assert.That(LlamaBrain.Utilities.JsonUtils.IsValidJson(schema), Is.True);
        }

        [Test]
        public void AnalysisSchema_ContainsRequiredProperties()
        {
            // Act
            var schema = JsonSchemaBuilder.AnalysisSchema;

            // Assert
            Assert.That(schema, Does.Contain("decision"));
            Assert.That(schema, Does.Contain("reasoning"));
            Assert.That(schema, Does.Contain("confidence"));
            Assert.That(schema, Does.Contain("alternatives"));
        }

        [Test]
        public void AnalysisSchema_HasCorrectRequiredFields()
        {
            // Act
            var schema = JsonSchemaBuilder.AnalysisSchema;

            // Assert
            Assert.That(schema, Does.Contain("\"required\": [\"decision\", \"reasoning\"]"));
        }

        #endregion

        #region BuildFromType - Additional Type Coverage

        [Test]
        public void BuildFromType_WithArray_ReturnsArraySchema()
        {
            // Act
            var schema = JsonSchemaBuilder.BuildFromType<ClassWithArray>();

            // Assert
            Assert.That(LlamaBrain.Utilities.JsonUtils.IsValidJson(schema), Is.True);
            Assert.That(schema, Does.Contain("array"));
            Assert.That(schema, Does.Contain("items"));
        }

        [Test]
        public void BuildFromType_WithList_ReturnsArraySchema()
        {
            // Act
            var schema = JsonSchemaBuilder.BuildFromType<ClassWithList>();

            // Assert
            Assert.That(LlamaBrain.Utilities.JsonUtils.IsValidJson(schema), Is.True);
            Assert.That(schema, Does.Contain("array"));
        }

        [Test]
        public void BuildFromType_WithDictionary_ReturnsObjectWithAdditionalProperties()
        {
            // Act
            var schema = JsonSchemaBuilder.BuildFromType<ClassWithDictionary>();

            // Assert
            Assert.That(LlamaBrain.Utilities.JsonUtils.IsValidJson(schema), Is.True);
            Assert.That(schema, Does.Contain("additionalProperties"));
        }

        [Test]
        public void BuildFromType_WithNullableInt_HandlesNullable()
        {
            // Act
            var schema = JsonSchemaBuilder.BuildFromType<ClassWithNullable>();

            // Assert
            Assert.That(LlamaBrain.Utilities.JsonUtils.IsValidJson(schema), Is.True);
            Assert.That(schema, Does.Contain("integer"));
        }

        [Test]
        public void BuildFromType_WithLong_ReturnsInteger()
        {
            // Act
            var schema = JsonSchemaBuilder.BuildFromType<ClassWithLong>();

            // Assert
            Assert.That(schema, Does.Contain("integer"));
        }

        [Test]
        public void BuildFromType_WithFloat_ReturnsNumber()
        {
            // Act
            var schema = JsonSchemaBuilder.BuildFromType<ClassWithFloat>();

            // Assert
            Assert.That(schema, Does.Contain("number"));
        }

        [Test]
        public void BuildFromType_WithDecimal_ReturnsNumber()
        {
            // Act
            var schema = JsonSchemaBuilder.BuildFromType<ClassWithDecimal>();

            // Assert
            Assert.That(schema, Does.Contain("number"));
        }

        [Test]
        public void BuildFromType_WithNestedClass_HandlesNesting()
        {
            // Act
            var schema = JsonSchemaBuilder.BuildFromType<ClassWithNested>();

            // Assert
            Assert.That(LlamaBrain.Utilities.JsonUtils.IsValidJson(schema), Is.True);
            Assert.That(schema, Does.Contain("nested"));
        }

        [Test]
        public void BuildFromType_WithCircularReference_DoesNotCrash()
        {
            // Act - Should handle circular reference without infinite loop
            var schema = JsonSchemaBuilder.BuildFromType<CircularClass>();

            // Assert
            Assert.That(LlamaBrain.Utilities.JsonUtils.IsValidJson(schema), Is.True);
        }

        [Test]
        public void BuildFromType_StructuredDialogueResponse_ReturnsValidSchema()
        {
            // Act
            var schema = JsonSchemaBuilder.BuildFromType<StructuredDialogueResponse>();

            // Assert
            Assert.That(LlamaBrain.Utilities.JsonUtils.IsValidJson(schema), Is.True);
            Assert.That(schema, Does.Contain("dialogueText"));
            Assert.That(schema, Does.Contain("proposedMutations"));
            Assert.That(schema, Does.Contain("worldIntents"));
        }

        #endregion

        #region CreateObjectSchema - Edge Cases

        [Test]
        public void CreateObjectSchema_NoRequiredProperties_OmitsRequired()
        {
            // Arrange
            var properties = new System.Collections.Generic.Dictionary<string, (string type, string description)>
            {
                { "name", ("string", "The name") }
            };

            // Act
            var schema = JsonSchemaBuilder.CreateObjectSchema(properties, null);

            // Assert
            Assert.That(LlamaBrain.Utilities.JsonUtils.IsValidJson(schema), Is.True);
            Assert.That(schema, Does.Not.Contain("\"required\""));
        }

        [Test]
        public void CreateObjectSchema_EmptyRequiredArray_OmitsRequired()
        {
            // Arrange
            var properties = new System.Collections.Generic.Dictionary<string, (string type, string description)>
            {
                { "name", ("string", "The name") }
            };

            // Act
            var schema = JsonSchemaBuilder.CreateObjectSchema(properties, System.Array.Empty<string>());

            // Assert
            Assert.That(LlamaBrain.Utilities.JsonUtils.IsValidJson(schema), Is.True);
            Assert.That(schema, Does.Not.Contain("\"required\""));
        }

        [Test]
        public void CreateObjectSchema_WithSpecialCharsInDescription_EscapesCorrectly()
        {
            // Arrange
            var properties = new System.Collections.Generic.Dictionary<string, (string type, string description)>
            {
                { "text", ("string", "Contains \"quotes\" and \\backslash") }
            };

            // Act
            var schema = JsonSchemaBuilder.CreateObjectSchema(properties);

            // Assert
            Assert.That(LlamaBrain.Utilities.JsonUtils.IsValidJson(schema), Is.True);
        }

        [Test]
        public void CreateObjectSchema_WithNewlineInDescription_EscapesCorrectly()
        {
            // Arrange
            var properties = new System.Collections.Generic.Dictionary<string, (string type, string description)>
            {
                { "text", ("string", "Line1\nLine2") }
            };

            // Act
            var schema = JsonSchemaBuilder.CreateObjectSchema(properties);

            // Assert
            Assert.That(LlamaBrain.Utilities.JsonUtils.IsValidJson(schema), Is.True);
        }

        [Test]
        public void CreateObjectSchema_MultipleRequiredProperties_IncludesAll()
        {
            // Arrange
            var properties = new System.Collections.Generic.Dictionary<string, (string type, string description)>
            {
                { "first", ("string", "First prop") },
                { "second", ("string", "Second prop") },
                { "third", ("string", "Third prop") }
            };

            // Act
            var schema = JsonSchemaBuilder.CreateObjectSchema(properties, new[] { "first", "second" });

            // Assert
            Assert.That(schema, Does.Contain("\"first\""));
            Assert.That(schema, Does.Contain("\"second\""));
            Assert.That(schema, Does.Contain("\"required\""));
        }

        #endregion

        #region ValidateSchema - Edge Cases

        [Test]
        public void ValidateSchema_NullInput_ReturnsFalse()
        {
            // Act
            var result = JsonSchemaBuilder.ValidateSchema(null!, out var error);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null);
        }

        [Test]
        public void ValidateSchema_WhitespaceOnly_ReturnsFalse()
        {
            // Act
            var result = JsonSchemaBuilder.ValidateSchema("   ", out var error);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(error, Does.Contain("empty"));
        }

        [Test]
        public void ValidateSchema_PrebuiltParsedOutputSchema_IsValid()
        {
            // Act
            var result = JsonSchemaBuilder.ValidateSchema(JsonSchemaBuilder.ParsedOutputSchema, out var error);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void ValidateSchema_PrebuiltDialogueOnlySchema_IsValid()
        {
            // Act
            var result = JsonSchemaBuilder.ValidateSchema(JsonSchemaBuilder.DialogueOnlySchema, out var error);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void ValidateSchema_PrebuiltAnalysisSchema_IsValid()
        {
            // Act
            var result = JsonSchemaBuilder.ValidateSchema(JsonSchemaBuilder.AnalysisSchema, out var error);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(error, Is.Null);
        }

        #endregion

        #region StructuredMutation Tests

        [Test]
        public void StructuredMutation_ToProposedMutation_ValidType_Converts()
        {
            // Arrange
            var mutation = new StructuredMutation
            {
                Type = "AppendEpisodic",
                Content = "Test content",
                Target = "target1",
                Confidence = 0.9f
            };

            // Act
            var result = mutation.ToProposedMutation();

            // Assert
            Assert.That(result.Type, Is.EqualTo(LlamaBrain.Core.Validation.MutationType.AppendEpisodic));
            Assert.That(result.Content, Is.EqualTo("Test content"));
            Assert.That(result.Target, Is.EqualTo("target1"));
            Assert.That(result.Confidence, Is.EqualTo(0.9f));
        }

        [Test]
        public void StructuredMutation_ToProposedMutation_InvalidType_FallsBackToAppendEpisodic()
        {
            // Arrange
            var mutation = new StructuredMutation
            {
                Type = "InvalidMutationType",
                Content = "Test content"
            };

            // Act
            var result = mutation.ToProposedMutation();

            // Assert - Should fall back to AppendEpisodic
            Assert.That(result.Type, Is.EqualTo(LlamaBrain.Core.Validation.MutationType.AppendEpisodic));
        }

        [Test]
        public void StructuredMutation_ToProposedMutation_TransformBelief_Converts()
        {
            // Arrange
            var mutation = new StructuredMutation
            {
                Type = "TransformBelief",
                Content = "New belief",
                Target = "belief_id",
                Confidence = 0.75f
            };

            // Act
            var result = mutation.ToProposedMutation();

            // Assert
            Assert.That(result.Type, Is.EqualTo(LlamaBrain.Core.Validation.MutationType.TransformBelief));
        }

        #endregion

        #region StructuredIntent Tests

        [Test]
        public void StructuredIntent_ToWorldIntent_Converts()
        {
            // Arrange
            var intent = new StructuredIntent
            {
                IntentType = "follow_player",
                Target = "player1",
                Priority = 5,
                Parameters = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "speed", "fast" }
                }
            };

            // Act
            var result = intent.ToWorldIntent();

            // Assert
            Assert.That(result.IntentType, Is.EqualTo("follow_player"));
            Assert.That(result.Target, Is.EqualTo("player1"));
            Assert.That(result.Priority, Is.EqualTo(5));
            Assert.That(result.Parameters["speed"], Is.EqualTo("fast"));
        }

        [Test]
        public void StructuredIntent_ToWorldIntent_CopiesParameters()
        {
            // Arrange
            var intent = new StructuredIntent
            {
                IntentType = "test",
                Parameters = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "key", "value" }
                }
            };

            // Act
            var result = intent.ToWorldIntent();

            // Modify original
            intent.Parameters["key"] = "modified";

            // Assert - Should be a copy, not reference
            Assert.That(result.Parameters["key"], Is.EqualTo("value"));
        }

        #endregion

        #region StructuredFunctionCall Tests

        [Test]
        public void StructuredFunctionCall_ToFunctionCall_Converts()
        {
            // Arrange
            var call = new StructuredFunctionCall
            {
                FunctionName = "get_memories",
                CallId = "call_123",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "limit", 10 }
                }
            };

            // Act
            var result = call.ToFunctionCall();

            // Assert
            Assert.That(result.FunctionName, Is.EqualTo("get_memories"));
            Assert.That(result.CallId, Is.EqualTo("call_123"));
            Assert.That(result.Arguments["limit"], Is.EqualTo(10));
        }

        [Test]
        public void StructuredFunctionCall_ToFunctionCall_CopiesArguments()
        {
            // Arrange
            var call = new StructuredFunctionCall
            {
                FunctionName = "test",
                Arguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "key", "value" }
                }
            };

            // Act
            var result = call.ToFunctionCall();

            // Modify original
            call.Arguments["key"] = "modified";

            // Assert - Should be a copy
            Assert.That(result.Arguments["key"], Is.EqualTo("value"));
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

        private class ClassWithArray
        {
            public string[] Tags { get; set; } = System.Array.Empty<string>();
        }

        private class ClassWithList
        {
            public System.Collections.Generic.List<int> Numbers { get; set; } = new System.Collections.Generic.List<int>();
        }

        private class ClassWithDictionary
        {
            public System.Collections.Generic.Dictionary<string, string> Metadata { get; set; } = new System.Collections.Generic.Dictionary<string, string>();
        }

        private class ClassWithNullable
        {
            public int? OptionalValue { get; set; }
        }

        private class ClassWithLong
        {
            public long BigNumber { get; set; }
        }

        private class ClassWithFloat
        {
            public float Ratio { get; set; }
        }

        private class ClassWithDecimal
        {
            public decimal Price { get; set; }
        }

        private class ClassWithNested
        {
            public SimpleTestClass Nested { get; set; } = new SimpleTestClass();
        }

        private class CircularClass
        {
            public string Name { get; set; } = "";
            public CircularClass? Self { get; set; }
        }

        #endregion
    }
}
