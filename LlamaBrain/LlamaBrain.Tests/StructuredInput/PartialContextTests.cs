using System.Collections.Generic;
using LlamaBrain.Core.StructuredInput;
using LlamaBrain.Core.StructuredInput.Schemas;
using Newtonsoft.Json;
using NUnit.Framework;

namespace LlamaBrain.Tests.StructuredInput
{
    /// <summary>
    /// Tests for partial/optional structured context (F23.5).
    /// Validates that schemas can be serialized with only the provided sections.
    /// </summary>
    [TestFixture]
    public class PartialContextTests
    {
        #region Nullable Section Tests

        [Test]
        public void ContextJsonSchema_NullContext_SerializesWithoutContext()
        {
            var schema = new ContextJsonSchema
            {
                Context = null,
                Constraints = null,
                Dialogue = new DialogueSection
                {
                    PlayerInput = "Hello"
                }
            };

            var json = ContextSerializer.Serialize(schema);

            Assert.That(json, Does.Not.Contain("\"context\""));
            Assert.That(json, Does.Not.Contain("\"constraints\""));
            Assert.That(json, Does.Contain("\"dialogue\""));
        }

        [Test]
        public void ContextJsonSchema_NullDialogue_SerializesWithoutDialogue()
        {
            var schema = new ContextJsonSchema
            {
                Context = new ContextSection
                {
                    CanonicalFacts = new List<string> { "The sky is blue" }
                },
                Constraints = null,
                Dialogue = null
            };

            var json = ContextSerializer.Serialize(schema);

            Assert.That(json, Does.Contain("\"context\""));
            Assert.That(json, Does.Not.Contain("\"constraints\""));
            Assert.That(json, Does.Not.Contain("\"dialogue\""));
        }

        [Test]
        public void ContextJsonSchema_AllSectionsNull_SerializesMinimal()
        {
            var schema = new ContextJsonSchema
            {
                Context = null,
                Constraints = null,
                Dialogue = null
            };

            var json = ContextSerializer.Serialize(schema);

            Assert.That(json, Does.Contain("\"schemaVersion\""));
            Assert.That(json, Does.Not.Contain("\"context\""));
            Assert.That(json, Does.Not.Contain("\"constraints\""));
            Assert.That(json, Does.Not.Contain("\"dialogue\""));
        }

        [Test]
        public void ContextJsonSchema_EmptySchema_IsValid()
        {
            var schema = new ContextJsonSchema();
            schema.Context = null;
            schema.Constraints = null;
            schema.Dialogue = null;

            var json = ContextSerializer.Serialize(schema);
            var deserialized = ContextSerializer.Deserialize(json);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized!.SchemaVersion, Is.EqualTo("1.0"));
        }

        #endregion

        #region Nullable List Property Tests

        [Test]
        public void ContextSection_NullLists_SerializesWithoutLists()
        {
            var schema = new ContextJsonSchema
            {
                Context = new ContextSection
                {
                    CanonicalFacts = null,
                    WorldState = null,
                    EpisodicMemories = null,
                    Beliefs = new List<BeliefEntry>
                    {
                        new BeliefEntry { Id = "belief1", Content = "Test belief", Confidence = 0.8f }
                    }
                }
            };

            var json = ContextSerializer.Serialize(schema);

            Assert.That(json, Does.Not.Contain("\"canonicalFacts\""));
            Assert.That(json, Does.Not.Contain("\"worldState\""));
            Assert.That(json, Does.Not.Contain("\"episodicMemories\""));
            Assert.That(json, Does.Contain("\"beliefs\""));
        }

        [Test]
        public void ContextSection_OnlyBeliefs_SerializesOnlyBeliefs()
        {
            var schema = new ContextJsonSchema
            {
                Context = new ContextSection
                {
                    CanonicalFacts = null,
                    WorldState = null,
                    EpisodicMemories = null,
                    Beliefs = new List<BeliefEntry>
                    {
                        new BeliefEntry { Id = "b1", Content = "Player is friendly", Confidence = 0.9f }
                    }
                },
                Constraints = null,
                Dialogue = null
            };

            var json = ContextSerializer.Serialize(schema);
            var deserialized = ContextSerializer.Deserialize(json);

            Assert.That(deserialized!.Context!.Beliefs, Has.Count.EqualTo(1));
            Assert.That(deserialized.Context.Beliefs![0].Id, Is.EqualTo("b1"));
        }

        #endregion

        #region Deserialization Tests

        [Test]
        public void Deserialize_PartialJson_ReturnsPartialSchema()
        {
            var partialJson = @"{
                ""schemaVersion"": ""1.0"",
                ""context"": {
                    ""beliefs"": [
                        { ""id"": ""b1"", ""content"": ""Test"", ""confidence"": 0.5, ""sentiment"": 0.0 }
                    ]
                }
            }";

            var deserialized = ContextSerializer.Deserialize(partialJson);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized!.Context, Is.Not.Null);
            Assert.That(deserialized.Context!.Beliefs, Has.Count.EqualTo(1));
            Assert.That(deserialized.Constraints, Is.Null);
            Assert.That(deserialized.Dialogue, Is.Null);
        }

        [Test]
        public void Deserialize_MinimalJson_ReturnsMinimalSchema()
        {
            var minimalJson = @"{ ""schemaVersion"": ""1.0"" }";

            var deserialized = ContextSerializer.Deserialize(minimalJson);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized!.SchemaVersion, Is.EqualTo("1.0"));
            Assert.That(deserialized.Context, Is.Null);
            Assert.That(deserialized.Constraints, Is.Null);
            Assert.That(deserialized.Dialogue, Is.Null);
        }

        [Test]
        public void Deserialize_MixedPartialSections_WorksCorrectly()
        {
            var mixedJson = @"{
                ""schemaVersion"": ""1.0"",
                ""dialogue"": {
                    ""playerInput"": ""Hello world"",
                    ""history"": []
                }
            }";

            var deserialized = ContextSerializer.Deserialize(mixedJson);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized!.Context, Is.Null);
            Assert.That(deserialized.Constraints, Is.Null);
            Assert.That(deserialized.Dialogue, Is.Not.Null);
            Assert.That(deserialized.Dialogue!.PlayerInput, Is.EqualTo("Hello world"));
        }

        #endregion

        #region PartialContextBuilder Tests

        [Test]
        public void PartialContextBuilder_WithBeliefs_BuildsPartialContext()
        {
            var beliefs = new List<BeliefEntry>
            {
                new BeliefEntry { Id = "b1", Content = "Test", Confidence = 0.8f }
            };

            var context = new PartialContextBuilder()
                .WithBeliefs(beliefs)
                .Build();

            Assert.That(context.Context, Is.Not.Null);
            Assert.That(context.Context!.Beliefs, Has.Count.EqualTo(1));
            Assert.That(context.Constraints, Is.Null);
            Assert.That(context.Dialogue, Is.Null);
        }

        [Test]
        public void PartialContextBuilder_WithCanonicalFacts_BuildsPartialContext()
        {
            var facts = new List<string> { "Fact 1", "Fact 2" };

            var context = new PartialContextBuilder()
                .WithCanonicalFacts(facts)
                .Build();

            Assert.That(context.Context, Is.Not.Null);
            Assert.That(context.Context!.CanonicalFacts, Has.Count.EqualTo(2));
            Assert.That(context.Context.Beliefs, Is.Null);
        }

        [Test]
        public void PartialContextBuilder_WithDialogue_BuildsPartialContext()
        {
            var context = new PartialContextBuilder()
                .WithCurrentInput("Hello")
                .Build();

            Assert.That(context.Dialogue, Is.Not.Null);
            Assert.That(context.Dialogue!.PlayerInput, Is.EqualTo("Hello"));
            Assert.That(context.Context, Is.Null);
        }

        [Test]
        public void PartialContextBuilder_Empty_BuildsMinimalContext()
        {
            var context = new PartialContextBuilder().Build();

            Assert.That(context.Context, Is.Null);
            Assert.That(context.Constraints, Is.Null);
            Assert.That(context.Dialogue, Is.Null);
            Assert.That(context.SchemaVersion, Is.EqualTo("1.0"));
        }

        [Test]
        public void PartialContextBuilder_Mixed_BuildsCorrectContext()
        {
            var beliefs = new List<BeliefEntry>
            {
                new BeliefEntry { Id = "b1", Content = "Test", Confidence = 0.8f }
            };

            var context = new PartialContextBuilder()
                .WithBeliefs(beliefs)
                .WithCurrentInput("Player says hello")
                .Build();

            Assert.That(context.Context, Is.Not.Null);
            Assert.That(context.Context!.Beliefs, Has.Count.EqualTo(1));
            Assert.That(context.Context.CanonicalFacts, Is.Null);
            Assert.That(context.Dialogue, Is.Not.Null);
            Assert.That(context.Dialogue!.PlayerInput, Is.EqualTo("Player says hello"));
            Assert.That(context.Constraints, Is.Null);
        }

        [Test]
        public void PartialContextBuilder_WithEpisodicMemories_BuildsPartialContext()
        {
            var memories = new List<EpisodicMemoryEntry>
            {
                new EpisodicMemoryEntry { Content = "Memory 1", Recency = 0.9f, Importance = 0.5f }
            };

            var context = new PartialContextBuilder()
                .WithEpisodicMemories(memories)
                .Build();

            Assert.That(context.Context, Is.Not.Null);
            Assert.That(context.Context!.EpisodicMemories, Has.Count.EqualTo(1));
        }

        [Test]
        public void PartialContextBuilder_WithWorldState_BuildsPartialContext()
        {
            var worldState = new List<WorldStateEntry>
            {
                new WorldStateEntry { Key = "door", Value = "open" }
            };

            var context = new PartialContextBuilder()
                .WithWorldState(worldState)
                .Build();

            Assert.That(context.Context, Is.Not.Null);
            Assert.That(context.Context!.WorldState, Has.Count.EqualTo(1));
        }

        #endregion

        #region Serialization Roundtrip Tests

        [Test]
        public void SerializeDeserialize_PartialContext_Roundtrips()
        {
            var original = new PartialContextBuilder()
                .WithBeliefs(new List<BeliefEntry>
                {
                    new BeliefEntry { Id = "b1", Content = "Test", Confidence = 0.8f, Sentiment = 0.1f }
                })
                .WithCurrentInput("Hello")
                .Build();

            var json = ContextSerializer.Serialize(original);
            var deserialized = ContextSerializer.Deserialize(json);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized!.Context!.Beliefs, Has.Count.EqualTo(1));
            Assert.That(deserialized.Context.Beliefs![0].Id, Is.EqualTo("b1"));
            Assert.That(deserialized.Dialogue!.PlayerInput, Is.EqualTo("Hello"));
        }

        [Test]
        public void SerializeCompact_PartialContext_OmitsNullSections()
        {
            var context = new PartialContextBuilder()
                .WithCurrentInput("Hello")
                .Build();

            var json = ContextSerializer.SerializeCompact(context);

            Assert.That(json, Does.Not.Contain("context"));
            Assert.That(json, Does.Not.Contain("constraints"));
            Assert.That(json, Does.Contain("dialogue"));
        }

        #endregion
    }
}
