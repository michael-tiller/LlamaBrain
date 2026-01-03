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
            Assert.That(deserialized.Context.CanonicalFacts, Is.EqualTo(original.Context.CanonicalFacts));
            Assert.That(deserialized.Context.WorldState.Count, Is.EqualTo(original.Context.WorldState.Count));
            Assert.That(deserialized.Context.EpisodicMemories.Count, Is.EqualTo(original.Context.EpisodicMemories.Count));
            Assert.That(deserialized.Context.Beliefs.Count, Is.EqualTo(original.Context.Beliefs.Count));
            Assert.That(deserialized.Constraints.Prohibitions, Is.EqualTo(original.Constraints.Prohibitions));
            Assert.That(deserialized.Dialogue.PlayerInput, Is.EqualTo(original.Dialogue.PlayerInput));
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
