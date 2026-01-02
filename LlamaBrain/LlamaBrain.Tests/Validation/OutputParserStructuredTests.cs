using NUnit.Framework;
using LlamaBrain.Core.Validation;

namespace LlamaBrain.Tests.Validation
{
    /// <summary>
    /// Tests for OutputParser structured output parsing.
    /// </summary>
    public class OutputParserStructuredTests
    {
        private OutputParser parser = null!;

        [SetUp]
        public void SetUp()
        {
            parser = new OutputParser(OutputParserConfig.NativeStructured);
        }

        #region ParseStructured Basic

        [Test]
        public void ParseStructured_ValidJson_ReturnsSuccess()
        {
            // Arrange
            var json = @"{
                ""dialogueText"": ""Hello, traveler! How may I help you?""
            }";

            // Act
            var result = parser.ParseStructured(json);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.DialogueText, Is.EqualTo("Hello, traveler! How may I help you?"));
        }

        [Test]
        public void ParseStructured_EmptyString_ReturnsFailed()
        {
            // Act
            var result = parser.ParseStructured("");

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("empty"));
        }

        [Test]
        public void ParseStructured_NullString_ReturnsFailed()
        {
            // Act
            var result = parser.ParseStructured(null!);

            // Assert
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void ParseStructured_InvalidJson_FallsBackToRegex()
        {
            // Arrange
            var invalidJson = "Hello, this is not JSON but valid dialogue.";

            // Act
            var result = parser.ParseStructured(invalidJson);

            // Assert
            // Falls back to regex parsing - should succeed since it's valid dialogue text
            Assert.That(result.Success, Is.True);
            Assert.That(result.DialogueText, Does.Contain("Hello"));
        }

        [Test]
        public void ParseStructured_EmptyDialogueText_ReturnsFailed()
        {
            // Arrange
            var json = @"{ ""dialogueText"": """" }";

            // Act
            var result = parser.ParseStructured(json);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("empty"));
        }

        #endregion

        #region ParseStructured with Mutations

        [Test]
        public void ParseStructured_WithMutation_ExtractsMutation()
        {
            // Arrange
            var json = @"{
                ""dialogueText"": ""I remember you now!"",
                ""proposedMutations"": [
                    {
                        ""type"": ""AppendEpisodic"",
                        ""content"": ""Met the player at the tavern""
                    }
                ]
            }";

            // Act
            var result = parser.ParseStructured(json);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.ProposedMutations.Count, Is.EqualTo(1));
            Assert.That(result.ProposedMutations[0].Type, Is.EqualTo(MutationType.AppendEpisodic));
            Assert.That(result.ProposedMutations[0].Content, Is.EqualTo("Met the player at the tavern"));
        }

        [Test]
        public void ParseStructured_WithBeliefMutation_ExtractsWithConfidence()
        {
            // Arrange
            var json = @"{
                ""dialogueText"": ""I think I trust you now."",
                ""proposedMutations"": [
                    {
                        ""type"": ""TransformBelief"",
                        ""target"": ""trust_player"",
                        ""content"": ""The player seems trustworthy"",
                        ""confidence"": 0.8
                    }
                ]
            }";

            // Act
            var result = parser.ParseStructured(json);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.ProposedMutations.Count, Is.EqualTo(1));
            Assert.That(result.ProposedMutations[0].Type, Is.EqualTo(MutationType.TransformBelief));
            Assert.That(result.ProposedMutations[0].Target, Is.EqualTo("trust_player"));
            Assert.That(result.ProposedMutations[0].Confidence, Is.EqualTo(0.8f).Within(0.01f));
        }

        [Test]
        public void ParseStructured_WithMultipleMutations_ExtractsAll()
        {
            // Arrange
            var json = @"{
                ""dialogueText"": ""You have my gratitude!"",
                ""proposedMutations"": [
                    { ""type"": ""AppendEpisodic"", ""content"": ""Player helped with quest"" },
                    { ""type"": ""TransformRelationship"", ""target"": ""player"", ""content"": ""Grateful ally"" }
                ]
            }";

            // Act
            var result = parser.ParseStructured(json);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.ProposedMutations.Count, Is.EqualTo(2));
        }

        #endregion

        #region ParseStructured with Intents

        [Test]
        public void ParseStructured_WithIntent_ExtractsIntent()
        {
            // Arrange
            var json = @"{
                ""dialogueText"": ""Follow me, I'll show you the way."",
                ""worldIntents"": [
                    {
                        ""intentType"": ""follow_player"",
                        ""target"": ""player"",
                        ""priority"": 10
                    }
                ]
            }";

            // Act
            var result = parser.ParseStructured(json);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.WorldIntents.Count, Is.EqualTo(1));
            Assert.That(result.WorldIntents[0].IntentType, Is.EqualTo("follow_player"));
            Assert.That(result.WorldIntents[0].Target, Is.EqualTo("player"));
            Assert.That(result.WorldIntents[0].Priority, Is.EqualTo(10));
        }

        [Test]
        public void ParseStructured_WithIntentParameters_ExtractsParameters()
        {
            // Arrange
            var json = @"{
                ""dialogueText"": ""Here, take this sword."",
                ""worldIntents"": [
                    {
                        ""intentType"": ""give_item"",
                        ""target"": ""player"",
                        ""parameters"": {
                            ""item_id"": ""sword_01"",
                            ""quantity"": ""1""
                        }
                    }
                ]
            }";

            // Act
            var result = parser.ParseStructured(json);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.WorldIntents.Count, Is.EqualTo(1));
            Assert.That(result.WorldIntents[0].Parameters.ContainsKey("item_id"), Is.True);
            Assert.That(result.WorldIntents[0].Parameters["item_id"], Is.EqualTo("sword_01"));
        }

        #endregion

        #region ParseStructured Full Response

        [Test]
        public void ParseStructured_FullResponse_ExtractsAll()
        {
            // Arrange
            var json = @"{
                ""dialogueText"": ""Thank you for your help! I shall remember this."",
                ""proposedMutations"": [
                    {
                        ""type"": ""AppendEpisodic"",
                        ""content"": ""Player helped defeat the bandits""
                    },
                    {
                        ""type"": ""TransformBelief"",
                        ""target"": ""player_alignment"",
                        ""content"": ""Player is heroic"",
                        ""confidence"": 0.9
                    }
                ],
                ""worldIntents"": [
                    {
                        ""intentType"": ""give_reward"",
                        ""target"": ""player"",
                        ""parameters"": { ""gold"": ""100"" },
                        ""priority"": 5
                    }
                ]
            }";

            // Act
            var result = parser.ParseStructured(json);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.DialogueText, Is.EqualTo("Thank you for your help! I shall remember this."));
            Assert.That(result.ProposedMutations.Count, Is.EqualTo(2));
            Assert.That(result.WorldIntents.Count, Is.EqualTo(1));
            Assert.That(result.HasStructuredData, Is.True);
        }

        #endregion

        #region ParseAuto

        [Test]
        public void ParseAuto_WithStructuredFlag_UsesStructuredParsing()
        {
            // Arrange
            var json = @"{ ""dialogueText"": ""Hello from structured!"" }";

            // Act
            var result = parser.ParseAuto(json, isStructuredOutput: true);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.DialogueText, Is.EqualTo("Hello from structured!"));
            Assert.That(result.Metadata.ContainsKey("parse_mode"), Is.True);
            Assert.That(result.Metadata["parse_mode"], Is.EqualTo("structured"));
        }

        [Test]
        public void ParseAuto_WithoutStructuredFlag_FallsBackToRegex()
        {
            // Arrange
            var text = "Hello, this is regular dialogue.";
            var defaultParser = new OutputParser(); // Default config (no structured output)

            // Act
            var result = defaultParser.ParseAuto(text, isStructuredOutput: false);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Metadata.ContainsKey("parse_mode"), Is.False);
        }

        [Test]
        public void ParseAuto_InvalidJson_FallsBackToRegex()
        {
            // Arrange
            var invalidJson = "{ invalid json but valid dialogue }";

            // Act
            var result = parser.ParseAuto(invalidJson, isStructuredOutput: true);

            // Assert
            // Falls back to regex parsing
            Assert.That(result.Success, Is.True);
        }

        #endregion

        #region OutputParserConfig.NativeStructured

        [Test]
        public void NativeStructuredConfig_HasCorrectSettings()
        {
            // Act
            var config = OutputParserConfig.NativeStructured;

            // Assert
            Assert.That(config.EnforceSingleLine, Is.False);
            Assert.That(config.ExtractStructuredData, Is.False);
            Assert.That(config.TrimToCompleteSentence, Is.False);
            Assert.That(config.RemoveStageDirections, Is.False);
            Assert.That(config.RemoveScriptDirections, Is.False);
            Assert.That(config.RemoveSpeakerLabels, Is.False);
            Assert.That(config.UseStructuredOutput, Is.True);
        }

        #endregion

        #region Determinism

        [Test]
        public void ParseStructured_SameInput_ProducesSameOutput()
        {
            // Arrange
            var json = @"{
                ""dialogueText"": ""Determinism test."",
                ""proposedMutations"": [
                    { ""type"": ""AppendEpisodic"", ""content"": ""Test memory"" }
                ]
            }";

            // Act
            var result1 = parser.ParseStructured(json);
            var result2 = parser.ParseStructured(json);

            // Assert
            Assert.That(result1.Success, Is.EqualTo(result2.Success));
            Assert.That(result1.DialogueText, Is.EqualTo(result2.DialogueText));
            Assert.That(result1.ProposedMutations.Count, Is.EqualTo(result2.ProposedMutations.Count));
        }

        #endregion
    }
}
