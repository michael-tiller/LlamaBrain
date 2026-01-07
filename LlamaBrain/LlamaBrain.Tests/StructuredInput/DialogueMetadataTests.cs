using NUnit.Framework;
using Newtonsoft.Json;
using LlamaBrain.Core.StructuredInput.Schemas;
using System.Collections.Generic;

namespace LlamaBrain.Tests.StructuredInput
{
    /// <summary>
    /// Tests for DialogueMetadata and StructuredDialogueEntry timestamp/metadata fields.
    /// Feature 23.2: Timestamp/metadata for dialogue history.
    /// </summary>
    [TestFixture]
    public class DialogueMetadataTests
    {
        #region DialogueMetadata Tests

        [Test]
        public void DialogueMetadata_DefaultValues_AllNull()
        {
            var metadata = new DialogueMetadata();

            Assert.That(metadata.Emotion, Is.Null);
            Assert.That(metadata.Location, Is.Null);
            Assert.That(metadata.Trigger, Is.Null);
            Assert.That(metadata.TurnNumber, Is.Null);
        }

        [Test]
        public void DialogueMetadata_SetAllFields_PreservesValues()
        {
            var metadata = new DialogueMetadata
            {
                Emotion = "friendly",
                Location = "tavern",
                Trigger = "player_greeting",
                TurnNumber = 5
            };

            Assert.That(metadata.Emotion, Is.EqualTo("friendly"));
            Assert.That(metadata.Location, Is.EqualTo("tavern"));
            Assert.That(metadata.Trigger, Is.EqualTo("player_greeting"));
            Assert.That(metadata.TurnNumber, Is.EqualTo(5));
        }

        [Test]
        public void DialogueMetadata_JsonRoundTrip_PreservesAllFields()
        {
            var original = new DialogueMetadata
            {
                Emotion = "angry",
                Location = "dungeon_entrance",
                Trigger = "combat_end",
                TurnNumber = 12
            };

            var json = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<DialogueMetadata>(json);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored!.Emotion, Is.EqualTo("angry"));
            Assert.That(restored.Location, Is.EqualTo("dungeon_entrance"));
            Assert.That(restored.Trigger, Is.EqualTo("combat_end"));
            Assert.That(restored.TurnNumber, Is.EqualTo(12));
        }

        [Test]
        public void DialogueMetadata_NullValues_OmittedFromJson()
        {
            var metadata = new DialogueMetadata
            {
                Emotion = "neutral"
                // All other fields left null
            };

            var json = JsonConvert.SerializeObject(metadata);

            Assert.That(json, Does.Contain("emotion"));
            Assert.That(json, Does.Not.Contain("location"));
            Assert.That(json, Does.Not.Contain("trigger"));
            Assert.That(json, Does.Not.Contain("turnNumber"));
        }

        [Test]
        public void DialogueMetadata_PartialFields_DeserializesCorrectly()
        {
            var json = @"{""emotion"":""happy"",""turnNumber"":3}";

            var metadata = JsonConvert.DeserializeObject<DialogueMetadata>(json);

            Assert.That(metadata, Is.Not.Null);
            Assert.That(metadata!.Emotion, Is.EqualTo("happy"));
            Assert.That(metadata.TurnNumber, Is.EqualTo(3));
            Assert.That(metadata.Location, Is.Null);
            Assert.That(metadata.Trigger, Is.Null);
        }

        #endregion

        #region StructuredDialogueEntry Timestamp/Metadata Tests

        [Test]
        public void StructuredDialogueEntry_DefaultValues_TimestampAndMetadataNull()
        {
            var entry = new StructuredDialogueEntry();

            Assert.That(entry.Speaker, Is.EqualTo(string.Empty));
            Assert.That(entry.Text, Is.EqualTo(string.Empty));
            Assert.That(entry.Timestamp, Is.Null);
            Assert.That(entry.Metadata, Is.Null);
        }

        [Test]
        public void StructuredDialogueEntry_WithTimestamp_PreservesValue()
        {
            var entry = new StructuredDialogueEntry
            {
                Speaker = "Player",
                Text = "Hello!",
                Timestamp = 123.45f
            };

            Assert.That(entry.Timestamp, Is.EqualTo(123.45f));
        }

        [Test]
        public void StructuredDialogueEntry_WithMetadata_PreservesValue()
        {
            var entry = new StructuredDialogueEntry
            {
                Speaker = "NPC",
                Text = "Greetings, traveler!",
                Metadata = new DialogueMetadata
                {
                    Emotion = "welcoming",
                    Location = "village_square"
                }
            };

            Assert.That(entry.Metadata, Is.Not.Null);
            Assert.That(entry.Metadata!.Emotion, Is.EqualTo("welcoming"));
            Assert.That(entry.Metadata.Location, Is.EqualTo("village_square"));
        }

        [Test]
        public void StructuredDialogueEntry_JsonRoundTrip_PreservesAllFields()
        {
            var original = new StructuredDialogueEntry
            {
                Speaker = "Guard",
                Text = "Halt! Who goes there?",
                Timestamp = 500.0f,
                Metadata = new DialogueMetadata
                {
                    Emotion = "alert",
                    Location = "castle_gate",
                    Trigger = "zone_enter",
                    TurnNumber = 0
                }
            };

            var json = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<StructuredDialogueEntry>(json);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored!.Speaker, Is.EqualTo("Guard"));
            Assert.That(restored.Text, Is.EqualTo("Halt! Who goes there?"));
            Assert.That(restored.Timestamp, Is.EqualTo(500.0f));
            Assert.That(restored.Metadata, Is.Not.Null);
            Assert.That(restored.Metadata!.Emotion, Is.EqualTo("alert"));
            Assert.That(restored.Metadata.Location, Is.EqualTo("castle_gate"));
            Assert.That(restored.Metadata.Trigger, Is.EqualTo("zone_enter"));
            Assert.That(restored.Metadata.TurnNumber, Is.EqualTo(0));
        }

        [Test]
        public void StructuredDialogueEntry_NullTimestampAndMetadata_OmittedFromJson()
        {
            var entry = new StructuredDialogueEntry
            {
                Speaker = "Player",
                Text = "Hello"
                // Timestamp and Metadata left null
            };

            var json = JsonConvert.SerializeObject(entry);

            Assert.That(json, Does.Contain("speaker"));
            Assert.That(json, Does.Contain("text"));
            Assert.That(json, Does.Not.Contain("timestamp"));
            Assert.That(json, Does.Not.Contain("metadata"));
        }

        [Test]
        public void StructuredDialogueEntry_JsonPropertyOrder_IsCorrect()
        {
            var entry = new StructuredDialogueEntry
            {
                Speaker = "A",
                Text = "B",
                Timestamp = 1.0f,
                Metadata = new DialogueMetadata { Emotion = "C" }
            };

            var json = JsonConvert.SerializeObject(entry);

            // Verify order: speaker, text, timestamp, metadata
            var speakerIndex = json.IndexOf("speaker");
            var textIndex = json.IndexOf("text");
            var timestampIndex = json.IndexOf("timestamp");
            var metadataIndex = json.IndexOf("metadata");

            Assert.That(speakerIndex, Is.LessThan(textIndex), "speaker should come before text");
            Assert.That(textIndex, Is.LessThan(timestampIndex), "text should come before timestamp");
            Assert.That(timestampIndex, Is.LessThan(metadataIndex), "timestamp should come before metadata");
        }

        #endregion

        #region DialogueSection Integration Tests

        [Test]
        public void DialogueSection_WithTimestampedHistory_SerializesCorrectly()
        {
            var section = new DialogueSection
            {
                History = new List<StructuredDialogueEntry>
                {
                    new StructuredDialogueEntry
                    {
                        Speaker = "Player",
                        Text = "Hello!",
                        Timestamp = 0.0f,
                        Metadata = new DialogueMetadata { TurnNumber = 0 }
                    },
                    new StructuredDialogueEntry
                    {
                        Speaker = "NPC",
                        Text = "Greetings!",
                        Timestamp = 1.5f,
                        Metadata = new DialogueMetadata { TurnNumber = 1, Emotion = "friendly" }
                    }
                },
                PlayerInput = "What do you sell?"
            };

            var json = JsonConvert.SerializeObject(section, Formatting.Indented);

            Assert.That(json, Does.Contain("history"));
            Assert.That(json, Does.Contain("timestamp"));
            Assert.That(json, Does.Contain("metadata"));
            Assert.That(json, Does.Contain("turnNumber"));
            Assert.That(json, Does.Contain("friendly"));
        }

        [Test]
        public void DialogueSection_MixedTimestampedAndNot_SerializesCorrectly()
        {
            var section = new DialogueSection
            {
                History = new List<StructuredDialogueEntry>
                {
                    new StructuredDialogueEntry
                    {
                        Speaker = "Player",
                        Text = "Hi"
                        // No timestamp or metadata
                    },
                    new StructuredDialogueEntry
                    {
                        Speaker = "NPC",
                        Text = "Hello",
                        Timestamp = 10.0f
                        // Has timestamp but no metadata
                    }
                },
                PlayerInput = "Test"
            };

            var json = JsonConvert.SerializeObject(section, Formatting.Indented);
            var restored = JsonConvert.DeserializeObject<DialogueSection>(json);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored!.History, Has.Count.EqualTo(2));
            Assert.That(restored.History[0].Timestamp, Is.Null);
            Assert.That(restored.History[0].Metadata, Is.Null);
            Assert.That(restored.History[1].Timestamp, Is.EqualTo(10.0f));
            Assert.That(restored.History[1].Metadata, Is.Null);
        }

        [Test]
        public void DialogueSection_JsonRoundTrip_PreservesAllFields()
        {
            var original = new DialogueSection
            {
                History = new List<StructuredDialogueEntry>
                {
                    new StructuredDialogueEntry
                    {
                        Speaker = "Player",
                        Text = "First message",
                        Timestamp = 0.0f,
                        Metadata = new DialogueMetadata
                        {
                            Emotion = "curious",
                            Location = "market",
                            Trigger = "npc_interaction",
                            TurnNumber = 0
                        }
                    }
                },
                PlayerInput = "Second message"
            };

            var json = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<DialogueSection>(json);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored!.History, Has.Count.EqualTo(1));
            Assert.That(restored.History[0].Metadata, Is.Not.Null);
            Assert.That(restored.History[0].Metadata!.Emotion, Is.EqualTo("curious"));
            Assert.That(restored.History[0].Metadata.Location, Is.EqualTo("market"));
            Assert.That(restored.History[0].Metadata.Trigger, Is.EqualTo("npc_interaction"));
            Assert.That(restored.History[0].Metadata.TurnNumber, Is.EqualTo(0));
            Assert.That(restored.PlayerInput, Is.EqualTo("Second message"));
        }

        #endregion
    }
}
