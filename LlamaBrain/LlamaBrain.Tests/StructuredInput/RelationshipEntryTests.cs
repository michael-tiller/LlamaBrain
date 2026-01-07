using System.Collections.Generic;
using LlamaBrain.Core.StructuredInput;
using LlamaBrain.Core.StructuredInput.Schemas;
using LlamaBrain.Core.StructuredOutput;
using Newtonsoft.Json;
using NUnit.Framework;

namespace LlamaBrain.Tests.StructuredInput
{
    /// <summary>
    /// Tests for RelationshipEntry (F23.2).
    /// Validates rich relationship data structure.
    /// </summary>
    [TestFixture]
    public class RelationshipEntryTests
    {
        #region Creation Tests

        [Test]
        public void Create_ReturnsEntryWithDefaults()
        {
            var entry = RelationshipEntry.Create("npc_001", "player", "acquaintance");

            Assert.That(entry.SourceEntity, Is.EqualTo("npc_001"));
            Assert.That(entry.TargetEntity, Is.EqualTo("player"));
            Assert.That(entry.RelationshipType, Is.EqualTo("acquaintance"));
            Assert.That(entry.Affinity, Is.EqualTo(0f));
            Assert.That(entry.Trust, Is.EqualTo(0.5f));
            Assert.That(entry.Familiarity, Is.EqualTo(0f));
        }

        [Test]
        public void CreateFriendly_ReturnsFriendlyEntry()
        {
            var entry = RelationshipEntry.CreateFriendly("npc_001", "player", 0.7f);

            Assert.That(entry.RelationshipType, Is.EqualTo("friend"));
            Assert.That(entry.Affinity, Is.EqualTo(0.7f));
            Assert.That(entry.Trust, Is.EqualTo(0.6f));
            Assert.That(entry.Familiarity, Is.EqualTo(0.4f));
        }

        [Test]
        public void CreateHostile_ReturnsHostileEntry()
        {
            var entry = RelationshipEntry.CreateHostile("npc_001", "player", -0.8f);

            Assert.That(entry.RelationshipType, Is.EqualTo("rival"));
            Assert.That(entry.Affinity, Is.EqualTo(-0.8f));
            Assert.That(entry.Trust, Is.EqualTo(0.1f));
            Assert.That(entry.Familiarity, Is.EqualTo(0.2f));
        }

        #endregion

        #region Fluent Builder Tests

        [Test]
        public void WithHistory_AddsHistoryEntry()
        {
            var entry = RelationshipEntry.Create("npc_001", "player")
                .WithHistory("First meeting at the tavern")
                .WithHistory("Helped player defeat bandits");

            Assert.That(entry.History, Is.Not.Null);
            Assert.That(entry.History, Has.Count.EqualTo(2));
            Assert.That(entry.History![0], Is.EqualTo("First meeting at the tavern"));
        }

        [Test]
        public void WithTag_AddsTag()
        {
            var entry = RelationshipEntry.Create("npc_001", "player")
                .WithTag("ally")
                .WithTag("trusted");

            Assert.That(entry.Tags, Is.Not.Null);
            Assert.That(entry.Tags, Has.Count.EqualTo(2));
            Assert.That(entry.Tags, Does.Contain("ally"));
        }

        [Test]
        public void WithTag_DoesNotAddDuplicates()
        {
            var entry = RelationshipEntry.Create("npc_001", "player")
                .WithTag("ally")
                .WithTag("ally");

            Assert.That(entry.Tags, Has.Count.EqualTo(1));
        }

        #endregion

        #region Serialization Tests

        [Test]
        public void RelationshipEntry_SerializesToJson()
        {
            var entry = new RelationshipEntry
            {
                SourceEntity = "npc_blacksmith",
                TargetEntity = "player",
                RelationshipType = "friend",
                Affinity = 0.7f,
                Trust = 0.8f,
                Familiarity = 0.6f,
                History = new List<string> { "Met at forge" },
                Tags = new List<string> { "ally" },
                LastInteractionTimestamp = 12345678L
            };

            var json = JsonConvert.SerializeObject(entry);
            var deserialized = JsonConvert.DeserializeObject<RelationshipEntry>(json);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized!.SourceEntity, Is.EqualTo("npc_blacksmith"));
            Assert.That(deserialized.TargetEntity, Is.EqualTo("player"));
            Assert.That(deserialized.Affinity, Is.EqualTo(0.7f));
            Assert.That(deserialized.History, Has.Count.EqualTo(1));
        }

        [Test]
        public void RelationshipEntry_InContextSection_SerializesCorrectly()
        {
            var context = new PartialContextBuilder()
                .WithRelationships(new List<RelationshipEntry>
                {
                    RelationshipEntry.CreateFriendly("npc_001", "player", 0.6f)
                })
                .Build();

            var json = ContextSerializer.Serialize(context);
            var deserialized = ContextSerializer.Deserialize(json);

            Assert.That(deserialized!.Context!.Relationships, Has.Count.EqualTo(1));
            Assert.That(deserialized.Context.Relationships![0].SourceEntity, Is.EqualTo("npc_001"));
        }

        [Test]
        public void RelationshipEntry_NullOptionalFields_OmittedFromJson()
        {
            var entry = RelationshipEntry.Create("npc_001", "player");

            var json = JsonConvert.SerializeObject(entry, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            Assert.That(json, Does.Not.Contain("history"));
            Assert.That(json, Does.Not.Contain("tags"));
            Assert.That(json, Does.Not.Contain("lastInteractionTimestamp"));
        }

        #endregion

        #region Validation Tests

        [Test]
        public void ValidateRelationshipEntry_Valid_ReturnsSuccess()
        {
            var entry = RelationshipEntry.Create("npc_001", "player", "friend");

            var result = RelationshipAuthorityValidator.ValidateRelationshipEntry(entry);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateRelationshipEntry_NullEntry_ReturnsFailure()
        {
            var result = RelationshipAuthorityValidator.ValidateRelationshipEntry(null);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("relationship"));
        }

        [Test]
        public void ValidateRelationshipEntry_EmptySourceEntity_ReturnsFailure()
        {
            var entry = new RelationshipEntry
            {
                SourceEntity = "",
                TargetEntity = "player",
                RelationshipType = "friend"
            };

            var result = RelationshipAuthorityValidator.ValidateRelationshipEntry(entry);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("sourceEntity"));
        }

        [Test]
        public void ValidateRelationshipEntry_EmptyTargetEntity_ReturnsFailure()
        {
            var entry = new RelationshipEntry
            {
                SourceEntity = "npc_001",
                TargetEntity = "",
                RelationshipType = "friend"
            };

            var result = RelationshipAuthorityValidator.ValidateRelationshipEntry(entry);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("targetEntity"));
        }

        [Test]
        public void ValidateRelationshipEntry_EmptyRelationshipType_ReturnsFailure()
        {
            var entry = new RelationshipEntry
            {
                SourceEntity = "npc_001",
                TargetEntity = "player",
                RelationshipType = ""
            };

            var result = RelationshipAuthorityValidator.ValidateRelationshipEntry(entry);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("relationshipType"));
        }

        [Test]
        public void ValidateRelationshipEntry_AffinityTooLow_ReturnsFailure()
        {
            var entry = new RelationshipEntry
            {
                SourceEntity = "npc_001",
                TargetEntity = "player",
                RelationshipType = "friend",
                Affinity = -1.5f
            };

            var result = RelationshipAuthorityValidator.ValidateRelationshipEntry(entry);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("affinity"));
        }

        [Test]
        public void ValidateRelationshipEntry_AffinityTooHigh_ReturnsFailure()
        {
            var entry = new RelationshipEntry
            {
                SourceEntity = "npc_001",
                TargetEntity = "player",
                RelationshipType = "friend",
                Affinity = 1.5f
            };

            var result = RelationshipAuthorityValidator.ValidateRelationshipEntry(entry);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("affinity"));
        }

        [Test]
        public void ValidateRelationshipEntry_TrustOutOfRange_ReturnsFailure()
        {
            var entry = new RelationshipEntry
            {
                SourceEntity = "npc_001",
                TargetEntity = "player",
                RelationshipType = "friend",
                Trust = -0.1f
            };

            var result = RelationshipAuthorityValidator.ValidateRelationshipEntry(entry);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("trust"));
        }

        [Test]
        public void ValidateRelationshipEntry_FamiliarityOutOfRange_ReturnsFailure()
        {
            var entry = new RelationshipEntry
            {
                SourceEntity = "npc_001",
                TargetEntity = "player",
                RelationshipType = "friend",
                Familiarity = 1.5f
            };

            var result = RelationshipAuthorityValidator.ValidateRelationshipEntry(entry);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("familiarity"));
        }

        [TestCase(-1f)]
        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        public void ValidateRelationshipEntry_ValidAffinityRange_ReturnsSuccess(float affinity)
        {
            var entry = RelationshipEntry.Create("npc_001", "player", "friend");
            entry.Affinity = affinity;

            var result = RelationshipAuthorityValidator.ValidateRelationshipEntry(entry);

            Assert.That(result.IsValid, Is.True);
        }

        #endregion

        #region Filter Tests

        [Test]
        public void FilterValidRelationships_RemovesInvalid()
        {
            var relationships = new List<RelationshipEntry>
            {
                RelationshipEntry.Create("npc_001", "player", "friend"),
                new RelationshipEntry { SourceEntity = "", TargetEntity = "player", RelationshipType = "enemy" },
                RelationshipEntry.Create("npc_002", "player", "ally")
            };

            var invalidCount = 0;
            var valid = RelationshipAuthorityValidator.FilterValidRelationships(
                relationships,
                (idx, rel, result) => invalidCount++);

            Assert.That(valid, Has.Count.EqualTo(2));
            Assert.That(invalidCount, Is.EqualTo(1));
        }

        [Test]
        public void FilterValidRelationships_NullList_ReturnsEmpty()
        {
            var valid = RelationshipAuthorityValidator.FilterValidRelationships(null);

            Assert.That(valid, Is.Empty);
        }

        #endregion

        #region ToString Tests

        [Test]
        public void ToString_ReturnsFormattedString()
        {
            var entry = RelationshipEntry.CreateFriendly("npc_001", "player", 0.75f);

            var str = entry.ToString();

            Assert.That(str, Does.Contain("npc_001"));
            Assert.That(str, Does.Contain("player"));
            Assert.That(str, Does.Contain("friend"));
            Assert.That(str, Does.Contain("0.75"));
        }

        #endregion
    }
}
