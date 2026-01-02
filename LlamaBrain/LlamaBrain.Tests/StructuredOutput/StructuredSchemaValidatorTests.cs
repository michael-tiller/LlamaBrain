using System.Collections.Generic;
using LlamaBrain.Core.StructuredOutput;
using LlamaBrain.Core.Validation;
using NUnit.Framework;

namespace LlamaBrain.Tests.StructuredOutput
{
    /// <summary>
    /// Tests for StructuredSchemaValidator.
    /// </summary>
    [TestFixture]
    public class StructuredSchemaValidatorTests
    {
        #region Mutation Validation Tests

        [Test]
        public void ValidateMutation_NullMutation_ReturnsFailure()
        {
            var result = StructuredSchemaValidator.ValidateMutation((StructuredMutation?)null);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("mutation"));
            Assert.That(result.ErrorMessage, Does.Contain("null"));
        }

        [Test]
        public void ValidateMutation_EmptyType_ReturnsFailure()
        {
            var mutation = new StructuredMutation
            {
                Type = "",
                Content = "Some content"
            };

            var result = StructuredSchemaValidator.ValidateMutation(mutation);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("type"));
        }

        [Test]
        public void ValidateMutation_InvalidType_ReturnsFailure()
        {
            var mutation = new StructuredMutation
            {
                Type = "InvalidType",
                Content = "Some content"
            };

            var result = StructuredSchemaValidator.ValidateMutation(mutation);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("type"));
            Assert.That(result.ErrorMessage, Does.Contain("InvalidType"));
        }

        [Test]
        public void ValidateMutation_EmptyContent_ReturnsFailure()
        {
            var mutation = new StructuredMutation
            {
                Type = "AppendEpisodic",
                Content = ""
            };

            var result = StructuredSchemaValidator.ValidateMutation(mutation);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("content"));
        }

        [TestCase("AppendEpisodic")]
        [TestCase("appendepisodic")]
        [TestCase("APPENDEPISODIC")]
        public void ValidateMutation_AppendEpisodic_Valid_ReturnsSuccess(string typeName)
        {
            var mutation = new StructuredMutation
            {
                Type = typeName,
                Content = "Player mentioned they like cats"
            };

            var result = StructuredSchemaValidator.ValidateMutation(mutation);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateMutation_TransformBelief_NoTarget_ReturnsFailure()
        {
            var mutation = new StructuredMutation
            {
                Type = "TransformBelief",
                Content = "Believes player is friendly",
                Target = null
            };

            var result = StructuredSchemaValidator.ValidateMutation(mutation);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("target"));
        }

        [Test]
        public void ValidateMutation_TransformBelief_InvalidConfidence_ReturnsFailure()
        {
            var mutation = new StructuredMutation
            {
                Type = "TransformBelief",
                Content = "Believes player is friendly",
                Target = "player_friendly",
                Confidence = 1.5f
            };

            var result = StructuredSchemaValidator.ValidateMutation(mutation);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("confidence"));
        }

        [Test]
        public void ValidateMutation_TransformBelief_Valid_ReturnsSuccess()
        {
            var mutation = new StructuredMutation
            {
                Type = "TransformBelief",
                Content = "Believes player is friendly",
                Target = "player_friendly",
                Confidence = 0.8f
            };

            var result = StructuredSchemaValidator.ValidateMutation(mutation);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateMutation_TransformRelationship_NoTarget_ReturnsFailure()
        {
            var mutation = new StructuredMutation
            {
                Type = "TransformRelationship",
                Content = "Improved relationship",
                Target = null
            };

            var result = StructuredSchemaValidator.ValidateMutation(mutation);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("target"));
        }

        [Test]
        public void ValidateMutation_TransformRelationship_Valid_ReturnsSuccess()
        {
            var mutation = new StructuredMutation
            {
                Type = "TransformRelationship",
                Content = "Improved relationship",
                Target = "player"
            };

            var result = StructuredSchemaValidator.ValidateMutation(mutation);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateMutation_EmitWorldIntent_NoTarget_ReturnsFailure()
        {
            var mutation = new StructuredMutation
            {
                Type = "EmitWorldIntent",
                Content = "give_item",
                Target = null
            };

            var result = StructuredSchemaValidator.ValidateMutation(mutation);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("target"));
        }

        [Test]
        public void ValidateMutation_EmitWorldIntent_Valid_ReturnsSuccess()
        {
            var mutation = new StructuredMutation
            {
                Type = "EmitWorldIntent",
                Content = "give_item",
                Target = "give_item"
            };

            var result = StructuredSchemaValidator.ValidateMutation(mutation);

            Assert.That(result.IsValid, Is.True);
        }

        #endregion

        #region ProposedMutation Validation Tests

        [Test]
        public void ValidateProposedMutation_Null_ReturnsFailure()
        {
            var result = StructuredSchemaValidator.ValidateMutation((ProposedMutation?)null);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("mutation"));
        }

        [Test]
        public void ValidateProposedMutation_EmptyContent_ReturnsFailure()
        {
            var mutation = new ProposedMutation
            {
                Type = MutationType.AppendEpisodic,
                Content = ""
            };

            var result = StructuredSchemaValidator.ValidateMutation(mutation);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("content"));
        }

        [Test]
        public void ValidateProposedMutation_AppendEpisodic_Valid_ReturnsSuccess()
        {
            var mutation = ProposedMutation.AppendEpisodic("Player said hello", "source");

            var result = StructuredSchemaValidator.ValidateMutation(mutation);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateProposedMutation_TransformBelief_Valid_ReturnsSuccess()
        {
            var mutation = ProposedMutation.TransformBelief("player_trust", "High trust", 0.9f, "source");

            var result = StructuredSchemaValidator.ValidateMutation(mutation);

            Assert.That(result.IsValid, Is.True);
        }

        #endregion

        #region Intent Validation Tests

        [Test]
        public void ValidateIntent_NullIntent_ReturnsFailure()
        {
            var result = StructuredSchemaValidator.ValidateIntent((StructuredIntent?)null);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("intent"));
        }

        [Test]
        public void ValidateIntent_EmptyIntentType_ReturnsFailure()
        {
            var intent = new StructuredIntent
            {
                IntentType = ""
            };

            var result = StructuredSchemaValidator.ValidateIntent(intent);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("intentType"));
        }

        [Test]
        public void ValidateIntent_NegativePriority_ReturnsFailure()
        {
            var intent = new StructuredIntent
            {
                IntentType = "follow_player",
                Priority = -1
            };

            var result = StructuredSchemaValidator.ValidateIntent(intent);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("priority"));
        }

        [Test]
        public void ValidateIntent_Valid_ReturnsSuccess()
        {
            var intent = new StructuredIntent
            {
                IntentType = "follow_player",
                Target = "player",
                Priority = 5,
                Parameters = new Dictionary<string, string> { { "speed", "walk" } }
            };

            var result = StructuredSchemaValidator.ValidateIntent(intent);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateIntent_MinimalValid_ReturnsSuccess()
        {
            var intent = new StructuredIntent
            {
                IntentType = "idle"
            };

            var result = StructuredSchemaValidator.ValidateIntent(intent);

            Assert.That(result.IsValid, Is.True);
        }

        #endregion

        #region WorldIntent Validation Tests

        [Test]
        public void ValidateWorldIntent_Null_ReturnsFailure()
        {
            var result = StructuredSchemaValidator.ValidateIntent((WorldIntent?)null);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailedField, Is.EqualTo("intent"));
        }

        [Test]
        public void ValidateWorldIntent_Valid_ReturnsSuccess()
        {
            var intent = WorldIntent.Create("give_item", "player", 5);

            var result = StructuredSchemaValidator.ValidateIntent(intent);

            Assert.That(result.IsValid, Is.True);
        }

        #endregion

        #region Batch Validation Tests

        [Test]
        public void ValidateAllMutations_NullList_ReturnsEmptyList()
        {
            var failures = StructuredSchemaValidator.ValidateAllMutations(null);

            Assert.That(failures, Is.Empty);
        }

        [Test]
        public void ValidateAllMutations_EmptyList_ReturnsEmptyList()
        {
            var failures = StructuredSchemaValidator.ValidateAllMutations(new List<ProposedMutation>());

            Assert.That(failures, Is.Empty);
        }

        [Test]
        public void ValidateAllMutations_AllValid_ReturnsEmptyList()
        {
            var mutations = new List<ProposedMutation>
            {
                ProposedMutation.AppendEpisodic("Memory 1", "source"),
                ProposedMutation.AppendEpisodic("Memory 2", "source")
            };

            var failures = StructuredSchemaValidator.ValidateAllMutations(mutations);

            Assert.That(failures, Is.Empty);
        }

        [Test]
        public void ValidateAllMutations_MixedValidity_ReturnsOnlyFailures()
        {
            var mutations = new List<ProposedMutation>
            {
                ProposedMutation.AppendEpisodic("Valid memory", "source"),
                new ProposedMutation { Type = MutationType.TransformBelief, Content = "No target", Target = null },
                ProposedMutation.AppendEpisodic("Another valid", "source")
            };

            var failures = StructuredSchemaValidator.ValidateAllMutations(mutations);

            Assert.That(failures, Has.Count.EqualTo(1));
            Assert.That(failures[0].Index, Is.EqualTo(1));
            Assert.That(failures[0].Result.IsValid, Is.False);
        }

        [Test]
        public void ValidateAllIntents_AllValid_ReturnsEmptyList()
        {
            var intents = new List<WorldIntent>
            {
                WorldIntent.Create("intent1", "target1"),
                WorldIntent.Create("intent2", "target2")
            };

            var failures = StructuredSchemaValidator.ValidateAllIntents(intents);

            Assert.That(failures, Is.Empty);
        }

        #endregion

        #region Filter Tests

        [Test]
        public void FilterValidMutations_RemovesInvalid()
        {
            var mutations = new List<ProposedMutation>
            {
                ProposedMutation.AppendEpisodic("Valid 1", "source"),
                new ProposedMutation { Type = MutationType.TransformBelief, Content = "Invalid - no target" },
                ProposedMutation.AppendEpisodic("Valid 2", "source")
            };

            var invalidCount = 0;
            var valid = StructuredSchemaValidator.FilterValidMutations(
                mutations,
                (idx, mut, result) => invalidCount++);

            Assert.That(valid, Has.Count.EqualTo(2));
            Assert.That(invalidCount, Is.EqualTo(1));
        }

        [Test]
        public void FilterValidIntents_RemovesInvalid()
        {
            var intents = new List<WorldIntent>
            {
                WorldIntent.Create("valid", "target"),
                new WorldIntent { IntentType = "", Target = "invalid" },
                WorldIntent.Create("also_valid", "target")
            };

            var invalidCount = 0;
            var valid = StructuredSchemaValidator.FilterValidIntents(
                intents,
                (idx, intent, result) => invalidCount++);

            Assert.That(valid, Has.Count.EqualTo(2));
            Assert.That(invalidCount, Is.EqualTo(1));
        }

        [Test]
        public void FilterValidMutations_NullList_ReturnsEmptyList()
        {
            var valid = StructuredSchemaValidator.FilterValidMutations(null);

            Assert.That(valid, Is.Empty);
        }

        #endregion

        #region SchemaValidationResult Tests

        [Test]
        public void SchemaValidationResult_Success_HasCorrectProperties()
        {
            var result = StructuredSchemaValidator.ValidateMutation(
                ProposedMutation.AppendEpisodic("content", "source"));

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.FailedField, Is.Null);
            Assert.That(result.ToString(), Is.EqualTo("Valid"));
        }

        [Test]
        public void SchemaValidationResult_Failure_HasCorrectProperties()
        {
            var result = StructuredSchemaValidator.ValidateMutation(
                new StructuredMutation { Type = "", Content = "test" });

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null);
            Assert.That(result.FailedField, Is.EqualTo("type"));
            Assert.That(result.ToString(), Does.Contain("Invalid"));
        }

        #endregion
    }
}
