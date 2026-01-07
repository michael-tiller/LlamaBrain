using System.Collections.Generic;
using LlamaBrain.Core.StructuredInput.Schemas;
using LlamaBrain.Core.StructuredOutput;
using LlamaBrain.Core.Validation;
using NUnit.Framework;

namespace LlamaBrain.Tests.StructuredOutput
{
    /// <summary>
    /// Tests for RelationshipAuthorityValidator (F23.2).
    /// Validates owner-based and confidence threshold authority checks.
    /// </summary>
    [TestFixture]
    public class RelationshipAuthorityTests
    {
        #region CanModify Tests (Owner-Based)

        [Test]
        public void CanModify_MatchingNpcId_ReturnsTrue()
        {
            var relationship = RelationshipEntry.Create("npc_blacksmith", "player", "friend");

            var result = RelationshipAuthorityValidator.CanModify(relationship, "npc_blacksmith");

            Assert.That(result, Is.True);
        }

        [Test]
        public void CanModify_NonMatchingNpcId_ReturnsFalse()
        {
            var relationship = RelationshipEntry.Create("npc_blacksmith", "player", "friend");

            var result = RelationshipAuthorityValidator.CanModify(relationship, "npc_merchant");

            Assert.That(result, Is.False);
        }

        [Test]
        public void CanModify_CaseInsensitive_ReturnsTrue()
        {
            var relationship = RelationshipEntry.Create("NPC_Blacksmith", "player", "friend");

            var result = RelationshipAuthorityValidator.CanModify(relationship, "npc_blacksmith");

            Assert.That(result, Is.True);
        }

        [Test]
        public void CanModify_NullRelationship_ReturnsFalse()
        {
            var result = RelationshipAuthorityValidator.CanModify(null!, "npc_001");

            Assert.That(result, Is.False);
        }

        [Test]
        public void CanModify_EmptyNpcId_ReturnsFalse()
        {
            var relationship = RelationshipEntry.Create("npc_001", "player", "friend");

            var result = RelationshipAuthorityValidator.CanModify(relationship, "");

            Assert.That(result, Is.False);
        }

        #endregion

        #region MeetsConfidenceThreshold Tests

        [Test]
        public void MeetsConfidenceThreshold_AboveThreshold_ReturnsTrue()
        {
            var mutation = ProposedMutation.TransformRelationship("player", "Improved relationship", null);
            mutation.Confidence = 0.8f;

            var result = RelationshipAuthorityValidator.MeetsConfidenceThreshold(mutation, 0.5f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void MeetsConfidenceThreshold_EqualToThreshold_ReturnsTrue()
        {
            var mutation = ProposedMutation.TransformRelationship("player", "Improved relationship", null);
            mutation.Confidence = 0.5f;

            var result = RelationshipAuthorityValidator.MeetsConfidenceThreshold(mutation, 0.5f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void MeetsConfidenceThreshold_BelowThreshold_ReturnsFalse()
        {
            var mutation = ProposedMutation.TransformRelationship("player", "Improved relationship", null);
            mutation.Confidence = 0.3f;

            var result = RelationshipAuthorityValidator.MeetsConfidenceThreshold(mutation, 0.5f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void MeetsConfidenceThreshold_UsesDefaultThreshold()
        {
            var mutation = ProposedMutation.TransformRelationship("player", "Improved relationship", null);
            mutation.Confidence = 0.6f;

            var result = RelationshipAuthorityValidator.MeetsConfidenceThreshold(mutation);

            Assert.That(result, Is.True); // Default threshold is 0.5
        }

        [Test]
        public void MeetsConfidenceThreshold_NullMutation_ReturnsFalse()
        {
            var result = RelationshipAuthorityValidator.MeetsConfidenceThreshold(null!);

            Assert.That(result, Is.False);
        }

        #endregion

        #region ValidateRelationshipMutation Tests

        [Test]
        public void ValidateRelationshipMutation_ValidMutation_ReturnsAuthorized()
        {
            var mutation = ProposedMutation.TransformRelationship("player", "Improved", null);
            mutation.Confidence = 0.8f;

            var result = RelationshipAuthorityValidator.ValidateRelationshipMutation(mutation);

            Assert.That(result.IsAuthorized, Is.True);
        }

        [Test]
        public void ValidateRelationshipMutation_LowConfidence_ReturnsUnauthorized()
        {
            var mutation = ProposedMutation.TransformRelationship("player", "Improved", null);
            mutation.Confidence = 0.2f;

            var config = new RelationshipAuthorityValidator.AuthorityConfig
            {
                ConfidenceThreshold = 0.5f,
                EnforceConfidenceThreshold = true
            };

            var result = RelationshipAuthorityValidator.ValidateRelationshipMutation(mutation, config);

            Assert.That(result.IsAuthorized, Is.False);
            Assert.That(result.FailedRule, Is.EqualTo("confidence_threshold"));
        }

        [Test]
        public void ValidateRelationshipMutation_ConfidenceEnforcementDisabled_ReturnsAuthorized()
        {
            var mutation = ProposedMutation.TransformRelationship("player", "Improved", null);
            mutation.Confidence = 0.1f;

            var config = new RelationshipAuthorityValidator.AuthorityConfig
            {
                EnforceConfidenceThreshold = false
            };

            var result = RelationshipAuthorityValidator.ValidateRelationshipMutation(mutation, config);

            Assert.That(result.IsAuthorized, Is.True);
        }

        [Test]
        public void ValidateRelationshipMutation_NonRelationshipMutation_ReturnsAuthorized()
        {
            var mutation = ProposedMutation.AppendEpisodic("Player said hello", null);
            mutation.Confidence = 0.1f;

            var result = RelationshipAuthorityValidator.ValidateRelationshipMutation(mutation);

            Assert.That(result.IsAuthorized, Is.True); // Only validates TransformRelationship
        }

        [Test]
        public void ValidateRelationshipMutation_NullMutation_ReturnsUnauthorized()
        {
            var result = RelationshipAuthorityValidator.ValidateRelationshipMutation(null!);

            Assert.That(result.IsAuthorized, Is.False);
            Assert.That(result.FailedRule, Is.EqualTo("null_check"));
        }

        #endregion

        #region ValidateRelationshipModification Tests

        [Test]
        public void ValidateRelationshipModification_OwnerMatches_ReturnsAuthorized()
        {
            var relationship = RelationshipEntry.Create("npc_001", "player", "friend");
            var mutation = ProposedMutation.TransformRelationship("player", "Better friends", null);
            mutation.Confidence = 0.8f;

            var config = RelationshipAuthorityValidator.AuthorityConfig.ForNpc("npc_001");

            var result = RelationshipAuthorityValidator.ValidateRelationshipModification(
                relationship, mutation, config);

            Assert.That(result.IsAuthorized, Is.True);
        }

        [Test]
        public void ValidateRelationshipModification_OwnerMismatch_ReturnsUnauthorized()
        {
            var relationship = RelationshipEntry.Create("npc_001", "player", "friend");
            var mutation = ProposedMutation.TransformRelationship("player", "Better friends", null);
            mutation.Confidence = 0.8f;

            var config = RelationshipAuthorityValidator.AuthorityConfig.ForNpc("npc_002");

            var result = RelationshipAuthorityValidator.ValidateRelationshipModification(
                relationship, mutation, config);

            Assert.That(result.IsAuthorized, Is.False);
            Assert.That(result.FailedRule, Is.EqualTo("owner_check"));
        }

        [Test]
        public void ValidateRelationshipModification_OwnershipDisabled_IgnoresOwnerMismatch()
        {
            var relationship = RelationshipEntry.Create("npc_001", "player", "friend");
            var mutation = ProposedMutation.TransformRelationship("player", "Better friends", null);
            mutation.Confidence = 0.8f;

            var config = new RelationshipAuthorityValidator.AuthorityConfig
            {
                NpcId = "npc_002",
                EnforceOwnership = false
            };

            var result = RelationshipAuthorityValidator.ValidateRelationshipModification(
                relationship, mutation, config);

            Assert.That(result.IsAuthorized, Is.True);
        }

        [Test]
        public void ValidateRelationshipModification_BothChecks_RequiresBothToPass()
        {
            var relationship = RelationshipEntry.Create("npc_001", "player", "friend");
            var mutation = ProposedMutation.TransformRelationship("player", "Better friends", null);
            mutation.Confidence = 0.3f; // Below threshold

            var config = new RelationshipAuthorityValidator.AuthorityConfig
            {
                NpcId = "npc_001", // Matches owner
                ConfidenceThreshold = 0.5f,
                EnforceOwnership = true,
                EnforceConfidenceThreshold = true
            };

            var result = RelationshipAuthorityValidator.ValidateRelationshipModification(
                relationship, mutation, config);

            Assert.That(result.IsAuthorized, Is.False);
            Assert.That(result.FailedRule, Is.EqualTo("confidence_threshold"));
        }

        #endregion

        #region FilterAuthorizedMutations Tests

        [Test]
        public void FilterAuthorizedMutations_RemovesUnauthorized()
        {
            var mutations = new List<ProposedMutation>
            {
                CreateRelationshipMutation(0.8f),  // Above threshold
                CreateRelationshipMutation(0.3f),  // Below threshold
                CreateRelationshipMutation(0.6f)   // Above threshold
            };

            var config = new RelationshipAuthorityValidator.AuthorityConfig
            {
                ConfidenceThreshold = 0.5f
            };

            var unauthorizedCount = 0;
            var authorized = RelationshipAuthorityValidator.FilterAuthorizedMutations(
                mutations, config,
                (idx, mut, result) => unauthorizedCount++);

            Assert.That(authorized, Has.Count.EqualTo(2));
            Assert.That(unauthorizedCount, Is.EqualTo(1));
        }

        [Test]
        public void FilterAuthorizedMutations_NullList_ReturnsEmpty()
        {
            var result = RelationshipAuthorityValidator.FilterAuthorizedMutations(null);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void FilterAuthorizedMutations_AllAuthorized_ReturnsAll()
        {
            var mutations = new List<ProposedMutation>
            {
                CreateRelationshipMutation(0.8f),
                CreateRelationshipMutation(0.9f)
            };

            var authorized = RelationshipAuthorityValidator.FilterAuthorizedMutations(mutations);

            Assert.That(authorized, Has.Count.EqualTo(2));
        }

        [Test]
        public void FilterAuthorizedMutations_IncludesNonRelationshipMutations()
        {
            var mutations = new List<ProposedMutation>
            {
                ProposedMutation.AppendEpisodic("Memory", null),
                CreateRelationshipMutation(0.3f),  // Below threshold
                ProposedMutation.TransformBelief("belief_id", "New belief", 0.8f, null)
            };

            var config = new RelationshipAuthorityValidator.AuthorityConfig
            {
                ConfidenceThreshold = 0.5f
            };

            var authorized = RelationshipAuthorityValidator.FilterAuthorizedMutations(mutations, config);

            // Non-relationship mutations pass, low-confidence relationship mutation filtered
            Assert.That(authorized, Has.Count.EqualTo(2));
        }

        #endregion

        #region AuthorityConfig Tests

        [Test]
        public void AuthorityConfig_Default_HasExpectedValues()
        {
            var config = RelationshipAuthorityValidator.AuthorityConfig.Default;

            Assert.That(config.ConfidenceThreshold, Is.EqualTo(RelationshipAuthorityValidator.DefaultConfidenceThreshold));
            Assert.That(config.EnforceOwnership, Is.True);
            Assert.That(config.EnforceConfidenceThreshold, Is.True);
            Assert.That(config.NpcId, Is.Null);
        }

        [Test]
        public void AuthorityConfig_ForNpc_SetsNpcId()
        {
            var config = RelationshipAuthorityValidator.AuthorityConfig.ForNpc("npc_merchant");

            Assert.That(config.NpcId, Is.EqualTo("npc_merchant"));
        }

        #endregion

        #region AuthorityValidationResult Tests

        [Test]
        public void AuthorityValidationResult_Authorized_HasCorrectProperties()
        {
            var result = RelationshipAuthorityValidator.ValidateRelationshipMutation(
                CreateRelationshipMutation(0.8f));

            Assert.That(result.IsAuthorized, Is.True);
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.FailedRule, Is.Null);
            Assert.That(result.ToString(), Is.EqualTo("Authorized"));
        }

        [Test]
        public void AuthorityValidationResult_Unauthorized_HasCorrectProperties()
        {
            var config = new RelationshipAuthorityValidator.AuthorityConfig
            {
                ConfidenceThreshold = 0.9f
            };

            var result = RelationshipAuthorityValidator.ValidateRelationshipMutation(
                CreateRelationshipMutation(0.5f), config);

            Assert.That(result.IsAuthorized, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null);
            Assert.That(result.FailedRule, Is.EqualTo("confidence_threshold"));
            Assert.That(result.ToString(), Does.Contain("Unauthorized"));
        }

        #endregion

        #region Helper Methods

        private static ProposedMutation CreateRelationshipMutation(float confidence)
        {
            var mutation = ProposedMutation.TransformRelationship("player", "Updated relationship", null);
            mutation.Confidence = confidence;
            return mutation;
        }

        #endregion
    }
}
