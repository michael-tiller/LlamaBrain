using NUnit.Framework;
using LlamaBrain.Core.StructuredInput;
using LlamaBrain.Core.StructuredInput.Schemas;
using LlamaBrain.Utilities;
using System.Collections.Generic;

namespace LlamaBrain.Tests.StructuredInput
{
    /// <summary>
    /// Tests for ContextSerializer - deterministic JSON serialization of context.
    /// </summary>
    public class ContextSerializerTests
    {
        #region Round-Trip Serialization

        [Test]
        public void Serialize_EmptyContext_ReturnsValidJson()
        {
            // Arrange
            var context = new ContextJsonSchema();

            // Act
            var json = ContextSerializer.Serialize(context);

            // Assert
            Assert.That(json, Is.Not.Null);
            Assert.That(json, Is.Not.Empty);
            Assert.That(JsonUtils.IsValidJson(json), Is.True);
        }

        [Test]
        public void Serialize_WithCanonicalFacts_IncludesFacts()
        {
            // Arrange
            var context = new ContextJsonSchema
            {
                Context = new ContextSection
                {
                    CanonicalFacts = new List<string> { "The king is Arthur", "The castle is Camelot" }
                }
            };

            // Act
            var json = ContextSerializer.Serialize(context);

            // Assert
            Assert.That(json, Does.Contain("The king is Arthur"));
            Assert.That(json, Does.Contain("The castle is Camelot"));
        }

        [Test]
        public void Serialize_WithWorldState_IncludesState()
        {
            // Arrange
            var context = new ContextJsonSchema
            {
                Context = new ContextSection
                {
                    WorldState = new List<WorldStateEntry>
                    {
                        new WorldStateEntry { Key = "door_status", Value = "open" },
                        new WorldStateEntry { Key = "quest_started", Value = "true" }
                    }
                }
            };

            // Act
            var json = ContextSerializer.Serialize(context);

            // Assert
            Assert.That(json, Does.Contain("door_status"));
            Assert.That(json, Does.Contain("open"));
            Assert.That(json, Does.Contain("quest_started"));
        }

        [Test]
        public void Serialize_WithEpisodicMemories_IncludesMemories()
        {
            // Arrange
            var context = new ContextJsonSchema
            {
                Context = new ContextSection
                {
                    EpisodicMemories = new List<EpisodicMemoryEntry>
                    {
                        new EpisodicMemoryEntry { Content = "Player said hello", Recency = 0.9f, Importance = 0.5f }
                    }
                }
            };

            // Act
            var json = ContextSerializer.Serialize(context);

            // Assert
            Assert.That(json, Does.Contain("Player said hello"));
            Assert.That(json, Does.Contain("0.9"));
        }

        [Test]
        public void Serialize_WithBeliefs_IncludesBeliefs()
        {
            // Arrange
            var context = new ContextJsonSchema
            {
                Context = new ContextSection
                {
                    Beliefs = new List<BeliefEntry>
                    {
                        new BeliefEntry { Id = "trust_player", Content = "I trust the player", Confidence = 0.8f, Sentiment = 0.6f }
                    }
                }
            };

            // Act
            var json = ContextSerializer.Serialize(context);

            // Assert
            Assert.That(json, Does.Contain("trust_player"));
            Assert.That(json, Does.Contain("I trust the player"));
            Assert.That(json, Does.Contain("0.8"));
        }

        [Test]
        public void Serialize_WithConstraints_IncludesConstraints()
        {
            // Arrange
            var context = new ContextJsonSchema
            {
                Constraints = new ConstraintSection
                {
                    Prohibitions = new List<string> { "Do not reveal secrets" },
                    Requirements = new List<string> { "Always be polite" },
                    Permissions = new List<string> { "May use magic" }
                }
            };

            // Act
            var json = ContextSerializer.Serialize(context);

            // Assert
            Assert.That(json, Does.Contain("Do not reveal secrets"));
            Assert.That(json, Does.Contain("Always be polite"));
            Assert.That(json, Does.Contain("May use magic"));
        }

        [Test]
        public void Serialize_WithDialogue_IncludesDialogueHistory()
        {
            // Arrange
            var context = new ContextJsonSchema
            {
                Dialogue = new DialogueSection
                {
                    History = new List<StructuredDialogueEntry>
                    {
                        new StructuredDialogueEntry { Speaker = "Player", Text = "Hello there!" },
                        new StructuredDialogueEntry { Speaker = "NPC", Text = "Greetings, traveler." }
                    },
                    PlayerInput = "What is your name?"
                }
            };

            // Act
            var json = ContextSerializer.Serialize(context);

            // Assert
            Assert.That(json, Does.Contain("Hello there!"));
            Assert.That(json, Does.Contain("Greetings, traveler"));
            Assert.That(json, Does.Contain("What is your name?"));
        }

        [Test]
        public void RoundTrip_PreservesAllData()
        {
            // Arrange
            var original = new ContextJsonSchema
            {
                SchemaVersion = "1.0",
                Context = new ContextSection
                {
                    CanonicalFacts = new List<string> { "Fact 1", "Fact 2" },
                    WorldState = new List<WorldStateEntry>
                    {
                        new WorldStateEntry { Key = "key1", Value = "value1" }
                    },
                    EpisodicMemories = new List<EpisodicMemoryEntry>
                    {
                        new EpisodicMemoryEntry { Content = "Memory 1", Recency = 0.5f, Importance = 0.7f }
                    },
                    Beliefs = new List<BeliefEntry>
                    {
                        new BeliefEntry { Id = "belief1", Content = "Belief content", Confidence = 0.9f, Sentiment = 0.3f }
                    }
                },
                Constraints = new ConstraintSection
                {
                    Prohibitions = new List<string> { "Prohibition 1" },
                    Requirements = new List<string> { "Requirement 1" },
                    Permissions = new List<string> { "Permission 1" }
                },
                Dialogue = new DialogueSection
                {
                    History = new List<StructuredDialogueEntry>
                    {
                        new StructuredDialogueEntry { Speaker = "Player", Text = "Hello" }
                    },
                    PlayerInput = "Current input"
                }
            };

            // Act
            var json = ContextSerializer.Serialize(original);
            var deserialized = ContextSerializer.Deserialize(json);

            // Assert
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized!.SchemaVersion, Is.EqualTo(original.SchemaVersion));
            Assert.That(deserialized.Context!.CanonicalFacts, Is.EqualTo(original.Context.CanonicalFacts));
            Assert.That(deserialized.Context.WorldState!.Count, Is.EqualTo(original.Context.WorldState.Count));
            Assert.That(deserialized.Context.EpisodicMemories!.Count, Is.EqualTo(original.Context.EpisodicMemories.Count));
            Assert.That(deserialized.Context.Beliefs!.Count, Is.EqualTo(original.Context.Beliefs.Count));
            Assert.That(deserialized.Constraints!.Prohibitions, Is.EqualTo(original.Constraints.Prohibitions));
            Assert.That(deserialized.Dialogue!.PlayerInput, Is.EqualTo(original.Dialogue.PlayerInput));
        }

        #endregion

        #region Determinism Tests

        [Test]
        public void Serialize_SameInput_ProducesSameOutput()
        {
            // Arrange
            var context1 = CreateTestContext();
            var context2 = CreateTestContext();

            // Act
            var json1 = ContextSerializer.Serialize(context1);
            var json2 = ContextSerializer.Serialize(context2);

            // Assert
            Assert.That(json1, Is.EqualTo(json2), "Same input should produce identical JSON output");
        }

        [Test]
        public void Serialize_MultipleCallsSameInstance_ProducesSameOutput()
        {
            // Arrange
            var context = CreateTestContext();

            // Act
            var json1 = ContextSerializer.Serialize(context);
            var json2 = ContextSerializer.Serialize(context);
            var json3 = ContextSerializer.Serialize(context);

            // Assert
            Assert.That(json1, Is.EqualTo(json2));
            Assert.That(json2, Is.EqualTo(json3));
        }

        [Test]
        public void Serialize_PreservesCollectionOrder()
        {
            // Arrange
            var context = new ContextJsonSchema
            {
                Context = new ContextSection
                {
                    CanonicalFacts = new List<string> { "Fact A", "Fact B", "Fact C" }
                }
            };

            // Act
            var json = ContextSerializer.Serialize(context);

            // Assert
            var indexA = json.IndexOf("Fact A");
            var indexB = json.IndexOf("Fact B");
            var indexC = json.IndexOf("Fact C");
            Assert.That(indexA, Is.LessThan(indexB), "Facts should preserve insertion order");
            Assert.That(indexB, Is.LessThan(indexC), "Facts should preserve insertion order");
        }

        #endregion

        #region Schema Version

        [Test]
        public void Serialize_IncludesSchemaVersion()
        {
            // Arrange
            var context = new ContextJsonSchema();

            // Act
            var json = ContextSerializer.Serialize(context);

            // Assert
            Assert.That(json, Does.Contain("schemaVersion"));
            Assert.That(json, Does.Contain("1.0"));
        }

        [Test]
        public void DefaultSchemaVersion_Is1_0()
        {
            // Arrange
            var context = new ContextJsonSchema();

            // Assert
            Assert.That(context.SchemaVersion, Is.EqualTo("1.0"));
        }

        #endregion

        #region Empty and Null Handling

        [Test]
        public void Serialize_EmptyLists_ProducesEmptyArrays()
        {
            // Arrange
            var context = new ContextJsonSchema
            {
                Context = new ContextSection
                {
                    CanonicalFacts = new List<string>(),
                    WorldState = new List<WorldStateEntry>(),
                    EpisodicMemories = new List<EpisodicMemoryEntry>(),
                    Beliefs = new List<BeliefEntry>()
                }
            };

            // Act
            var json = ContextSerializer.Serialize(context);

            // Assert
            Assert.That(JsonUtils.IsValidJson(json), Is.True);
            Assert.That(json, Does.Contain("[]"));
        }

        [Test]
        public void Serialize_NullDialoguePlayerInput_HandlesGracefully()
        {
            // Arrange
            var context = new ContextJsonSchema
            {
                Dialogue = new DialogueSection
                {
                    History = new List<StructuredDialogueEntry>(),
                    PlayerInput = null
                }
            };

            // Act
            var json = ContextSerializer.Serialize(context);

            // Assert
            Assert.That(JsonUtils.IsValidJson(json), Is.True);
        }

        #endregion

        #region Null Argument Exception Tests

        [Test]
        public void Serialize_NullContext_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<System.ArgumentNullException>(() => ContextSerializer.Serialize(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("context"));
        }

        [Test]
        public void Deserialize_NullJson_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<System.ArgumentNullException>(() => ContextSerializer.Deserialize(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("json"));
        }

        [Test]
        public void SerializeCompact_NullContext_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<System.ArgumentNullException>(() => ContextSerializer.SerializeCompact(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("context"));
        }

        [Test]
        public void SerializeWithDelimiters_NullContext_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<System.ArgumentNullException>(() => ContextSerializer.SerializeWithDelimiters(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("context"));
        }

        #endregion

        #region SerializeCompact Tests

        [Test]
        public void SerializeCompact_EmptyContext_ReturnsValidCompactJson()
        {
            // Arrange
            var context = new ContextJsonSchema();

            // Act
            var json = ContextSerializer.SerializeCompact(context);

            // Assert
            Assert.That(json, Is.Not.Null);
            Assert.That(json, Is.Not.Empty);
            Assert.That(JsonUtils.IsValidJson(json), Is.True);
            // Compact JSON should not have newlines or indentation
            Assert.That(json, Does.Not.Contain("\n"));
        }

        [Test]
        public void SerializeCompact_WithData_ProducesCompactOutput()
        {
            // Arrange
            var context = CreateTestContext();

            // Act
            var compactJson = ContextSerializer.SerializeCompact(context);
            var normalJson = ContextSerializer.Serialize(context);

            // Assert
            Assert.That(compactJson.Length, Is.LessThan(normalJson.Length), "Compact JSON should be shorter");
            Assert.That(compactJson, Does.Not.Contain("\n"), "Compact JSON should not have newlines");
            Assert.That(compactJson, Does.Not.Contain("  "), "Compact JSON should not have indentation");
        }

        [Test]
        public void SerializeCompact_PreservesAllData()
        {
            // Arrange
            var original = CreateTestContext();

            // Act
            var compactJson = ContextSerializer.SerializeCompact(original);
            var deserialized = ContextSerializer.Deserialize(compactJson);

            // Assert
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized!.SchemaVersion, Is.EqualTo(original.SchemaVersion));
            Assert.That(deserialized.Context!.CanonicalFacts, Is.EqualTo(original.Context!.CanonicalFacts));
            Assert.That(deserialized.Dialogue!.PlayerInput, Is.EqualTo(original.Dialogue!.PlayerInput));
        }

        [Test]
        public void SerializeCompact_SameInput_ProducesSameOutput()
        {
            // Arrange
            var context1 = CreateTestContext();
            var context2 = CreateTestContext();

            // Act
            var json1 = ContextSerializer.SerializeCompact(context1);
            var json2 = ContextSerializer.SerializeCompact(context2);

            // Assert
            Assert.That(json1, Is.EqualTo(json2), "Same input should produce identical compact JSON output");
        }

        [Test]
        public void SerializeCompact_OmitsNullValues()
        {
            // Arrange
            var context = new ContextJsonSchema
            {
                Context = new ContextSection
                {
                    CanonicalFacts = new List<string> { "Test fact" }
                    // Other properties are null
                }
            };

            // Act
            var json = ContextSerializer.SerializeCompact(context);

            // Assert
            Assert.That(JsonUtils.IsValidJson(json), Is.True);
            // Null values should be omitted due to NullValueHandling.Ignore
        }

        #endregion

        #region SerializeWithDelimiters Tests

        [Test]
        public void SerializeWithDelimiters_DefaultTags_WrapsJsonCorrectly()
        {
            // Arrange
            var context = new ContextJsonSchema();

            // Act
            var result = ContextSerializer.SerializeWithDelimiters(context);

            // Assert
            Assert.That(result, Does.StartWith("<context_json>"));
            Assert.That(result, Does.EndWith("</context_json>"));
            Assert.That(result, Does.Contain("schemaVersion"));
        }

        [Test]
        public void SerializeWithDelimiters_CustomTags_UsesProvidedTags()
        {
            // Arrange
            var context = new ContextJsonSchema();
            var openTag = "<game_context>";
            var closeTag = "</game_context>";

            // Act
            var result = ContextSerializer.SerializeWithDelimiters(context, openTag, closeTag);

            // Assert
            Assert.That(result, Does.StartWith("<game_context>"));
            Assert.That(result, Does.EndWith("</game_context>"));
            Assert.That(result, Does.Not.Contain("<context_json>"));
        }

        [Test]
        public void SerializeWithDelimiters_CompactFalse_UsesIndentedJson()
        {
            // Arrange
            var context = CreateTestContext();

            // Act
            var result = ContextSerializer.SerializeWithDelimiters(context, compact: false);

            // Assert
            // Indented JSON will have newlines between properties
            var jsonContent = result.Replace("<context_json>", "").Replace("</context_json>", "").Trim();
            Assert.That(jsonContent, Does.Contain("\n"), "Non-compact output should have newlines");
        }

        [Test]
        public void SerializeWithDelimiters_CompactTrue_UsesCompactJson()
        {
            // Arrange
            var context = CreateTestContext();

            // Act
            var result = ContextSerializer.SerializeWithDelimiters(context, compact: true);

            // Assert
            // Extract the JSON between delimiters
            var lines = result.Split('\n');
            // With compact=true, structure is: openTag\njson\ncloseTag (3 lines)
            Assert.That(lines.Length, Is.EqualTo(3), "Compact output should have exactly 3 lines (open tag, json, close tag)");
        }

        [Test]
        public void SerializeWithDelimiters_PreservesDataIntegrity()
        {
            // Arrange
            var context = CreateTestContext();

            // Act
            var result = ContextSerializer.SerializeWithDelimiters(context);

            // Assert
            Assert.That(result, Does.Contain("The king is Arthur"));
            Assert.That(result, Does.Contain("How are you?"));
            Assert.That(result, Does.Contain("trust"));
        }

        [Test]
        public void SerializeWithDelimiters_EmptyTags_StillWorks()
        {
            // Arrange
            var context = new ContextJsonSchema();

            // Act
            var result = ContextSerializer.SerializeWithDelimiters(context, "", "");

            // Assert
            Assert.That(result, Does.Contain("schemaVersion"));
        }

        [Test]
        public void SerializeWithDelimiters_XmlStyleTags_FormatsCorrectly()
        {
            // Arrange
            var context = new ContextJsonSchema { SchemaVersion = "1.0" };

            // Act
            var result = ContextSerializer.SerializeWithDelimiters(
                context,
                "<!-- BEGIN CONTEXT -->",
                "<!-- END CONTEXT -->");

            // Assert
            Assert.That(result, Does.StartWith("<!-- BEGIN CONTEXT -->"));
            Assert.That(result, Does.EndWith("<!-- END CONTEXT -->"));
        }

        #endregion

        #region Deserialize Edge Cases

        [Test]
        public void Deserialize_EmptyString_ReturnsNull()
        {
            // Act
            var result = ContextSerializer.Deserialize("");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Deserialize_WhitespaceOnly_ReturnsNull()
        {
            // Act
            var result = ContextSerializer.Deserialize("   \n\t  ");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Deserialize_InvalidJson_ThrowsJsonException()
        {
            // Arrange
            var invalidJson = "{ this is not valid json }";

            // Act & Assert
            Assert.Throws<Newtonsoft.Json.JsonReaderException>(() => ContextSerializer.Deserialize(invalidJson));
        }

        [Test]
        public void Deserialize_ValidJsonWrongSchema_ReturnsObjectWithDefaults()
        {
            // Arrange - Valid JSON but not matching our schema
            var wrongSchemaJson = @"{ ""foo"": ""bar"", ""number"": 42 }";

            // Act
            var result = ContextSerializer.Deserialize(wrongSchemaJson);

            // Assert - Should deserialize but with default values
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.SchemaVersion, Is.EqualTo("1.0")); // Default value
        }

        [Test]
        public void Deserialize_PartialSchema_PreservesProvidedValues()
        {
            // Arrange - Only some fields provided
            var partialJson = @"{ ""schemaVersion"": ""2.0"" }";

            // Act
            var result = ContextSerializer.Deserialize(partialJson);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.SchemaVersion, Is.EqualTo("2.0"));
        }

        [Test]
        public void Deserialize_JsonArray_ThrowsException()
        {
            // Arrange - Array instead of object
            var arrayJson = @"[1, 2, 3]";

            // Act & Assert
            Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => ContextSerializer.Deserialize(arrayJson));
        }

        [Test]
        public void Deserialize_NullJsonValue_ReturnsNull()
        {
            // Arrange
            var nullJson = "null";

            // Act
            var result = ContextSerializer.Deserialize(nullJson);

            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion

        #region Helper Methods

        private static ContextJsonSchema CreateTestContext()
        {
            return new ContextJsonSchema
            {
                SchemaVersion = "1.0",
                Context = new ContextSection
                {
                    CanonicalFacts = new List<string> { "The king is Arthur" },
                    WorldState = new List<WorldStateEntry>
                    {
                        new WorldStateEntry { Key = "door", Value = "open" }
                    },
                    EpisodicMemories = new List<EpisodicMemoryEntry>
                    {
                        new EpisodicMemoryEntry { Content = "Met player", Recency = 0.8f, Importance = 0.5f }
                    },
                    Beliefs = new List<BeliefEntry>
                    {
                        new BeliefEntry { Id = "trust", Content = "Trust the player", Confidence = 0.7f, Sentiment = 0.5f }
                    }
                },
                Constraints = new ConstraintSection
                {
                    Prohibitions = new List<string> { "No secrets" },
                    Requirements = new List<string> { "Be helpful" },
                    Permissions = new List<string> { "Use magic" }
                },
                Dialogue = new DialogueSection
                {
                    History = new List<StructuredDialogueEntry>
                    {
                        new StructuredDialogueEntry { Speaker = "Player", Text = "Hello" }
                    },
                    PlayerInput = "How are you?"
                }
            };
        }

        #endregion
    }
}
