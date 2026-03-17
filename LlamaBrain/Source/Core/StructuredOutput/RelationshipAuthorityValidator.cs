using System;
using System.Collections.Generic;
using LlamaBrain.Core.StructuredInput.Schemas;
using LlamaBrain.Core.Validation;

namespace LlamaBrain.Core.StructuredOutput
{
    /// <summary>
    /// Validates relationship mutations against authority rules.
    /// Implements owner-based and confidence threshold checks.
    /// </summary>
    public static class RelationshipAuthorityValidator
    {
        /// <summary>
        /// Default confidence threshold for relationship mutations.
        /// </summary>
        public const float DefaultConfidenceThreshold = 0.5f;

        /// <summary>
        /// Result of relationship authority validation.
        /// </summary>
        public sealed class AuthorityValidationResult
        {
            /// <summary>
            /// Whether the validation passed.
            /// </summary>
            public bool IsAuthorized { get; }

            /// <summary>
            /// Error message if validation failed.
            /// </summary>
            public string? ErrorMessage { get; }

            /// <summary>
            /// The rule that failed (if any).
            /// </summary>
            public string? FailedRule { get; }

            private AuthorityValidationResult(bool isAuthorized, string? errorMessage = null, string? failedRule = null)
            {
                IsAuthorized = isAuthorized;
                ErrorMessage = errorMessage;
                FailedRule = failedRule;
            }

            /// <summary>
            /// Creates an authorized result.
            /// </summary>
            /// <returns>An AuthorityValidationResult indicating authorization was granted.</returns>
            public static AuthorityValidationResult Authorized() => new AuthorityValidationResult(true);

            /// <summary>
            /// Creates an unauthorized result.
            /// </summary>
            /// <param name="errorMessage">The error message.</param>
            /// <param name="failedRule">The rule that failed.</param>
            /// <returns>An AuthorityValidationResult indicating authorization was denied.</returns>
            public static AuthorityValidationResult Unauthorized(string errorMessage, string failedRule)
                => new AuthorityValidationResult(false, errorMessage, failedRule);

            /// <summary>
            /// Returns a string representation of the result.
            /// </summary>
            /// <returns>A formatted string describing the authorization result.</returns>
            public override string ToString()
                => IsAuthorized ? "Authorized" : $"Unauthorized: {ErrorMessage} (rule: {FailedRule ?? "unknown"})";
        }

        /// <summary>
        /// Configuration for authority validation.
        /// </summary>
        public sealed class AuthorityConfig
        {
            /// <summary>
            /// The NPC ID that owns the current context.
            /// Used for owner-based authority checks.
            /// </summary>
            public string? NpcId { get; set; }

            /// <summary>
            /// Minimum confidence threshold for mutations (0-1).
            /// Mutations below this threshold are rejected.
            /// </summary>
            public float ConfidenceThreshold { get; set; } = DefaultConfidenceThreshold;

            /// <summary>
            /// Whether to enforce owner-based authority checks.
            /// </summary>
            public bool EnforceOwnership { get; set; } = true;

            /// <summary>
            /// Whether to enforce confidence threshold checks.
            /// </summary>
            public bool EnforceConfidenceThreshold { get; set; } = true;

            /// <summary>
            /// Creates a default config.
            /// </summary>
            public static AuthorityConfig Default => new AuthorityConfig();

            /// <summary>
            /// Creates a config with the specified NPC ID.
            /// </summary>
            /// <param name="npcId">The NPC ID.</param>
            /// <returns>An AuthorityConfig instance configured for the specified NPC.</returns>
            public static AuthorityConfig ForNpc(string npcId) => new AuthorityConfig { NpcId = npcId };
        }

        /// <summary>
        /// Checks if an NPC can modify a relationship entry.
        /// Owner-based: sourceEntity must match NPC ID.
        /// </summary>
        /// <param name="relationship">The relationship to check.</param>
        /// <param name="npcId">The NPC ID attempting the modification.</param>
        /// <returns>True if the NPC owns this relationship view.</returns>
        public static bool CanModify(RelationshipEntry relationship, string npcId)
        {
            if (relationship == null || string.IsNullOrEmpty(npcId))
                return false;

            return string.Equals(relationship.SourceEntity, npcId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a mutation meets the confidence threshold.
        /// </summary>
        /// <param name="mutation">The mutation to check.</param>
        /// <param name="threshold">The minimum confidence threshold (0-1).</param>
        /// <returns>True if the mutation meets the threshold.</returns>
        public static bool MeetsConfidenceThreshold(ProposedMutation mutation, float threshold = DefaultConfidenceThreshold)
        {
            if (mutation == null)
                return false;

            return mutation.Confidence >= threshold;
        }

        /// <summary>
        /// Validates a relationship mutation against authority rules.
        /// </summary>
        /// <param name="mutation">The mutation to validate.</param>
        /// <param name="config">The authority configuration.</param>
        /// <returns>The validation result.</returns>
        public static AuthorityValidationResult ValidateRelationshipMutation(
            ProposedMutation mutation,
            AuthorityConfig? config = null)
        {
            if (mutation == null)
                return AuthorityValidationResult.Unauthorized("Mutation is null", "null_check");

            config ??= AuthorityConfig.Default;

            // Only validate TransformRelationship mutations
            if (mutation.Type != MutationType.TransformRelationship)
                return AuthorityValidationResult.Authorized();

            // Check confidence threshold
            if (config.EnforceConfidenceThreshold && !MeetsConfidenceThreshold(mutation, config.ConfidenceThreshold))
            {
                return AuthorityValidationResult.Unauthorized(
                    $"Confidence {mutation.Confidence:F2} below threshold {config.ConfidenceThreshold:F2}",
                    "confidence_threshold");
            }

            // Note: Owner-based checks require knowing the target relationship.
            // For mutation-only validation, we can only check confidence.
            // Full owner validation requires the relationship entry itself.

            return AuthorityValidationResult.Authorized();
        }

        /// <summary>
        /// Validates a relationship modification against authority rules.
        /// </summary>
        /// <param name="relationship">The relationship being modified.</param>
        /// <param name="mutation">The mutation being applied.</param>
        /// <param name="config">The authority configuration.</param>
        /// <returns>The validation result.</returns>
        public static AuthorityValidationResult ValidateRelationshipModification(
            RelationshipEntry relationship,
            ProposedMutation mutation,
            AuthorityConfig config)
        {
            if (relationship == null)
                return AuthorityValidationResult.Unauthorized("Relationship is null", "null_check");

            if (mutation == null)
                return AuthorityValidationResult.Unauthorized("Mutation is null", "null_check");

            if (config == null)
                return AuthorityValidationResult.Unauthorized("Config is null", "null_check");

            // Check owner-based authority
            if (config.EnforceOwnership && !string.IsNullOrEmpty(config.NpcId))
            {
                if (!CanModify(relationship, config.NpcId))
                {
                    return AuthorityValidationResult.Unauthorized(
                        $"NPC '{config.NpcId}' cannot modify relationship owned by '{relationship.SourceEntity}'",
                        "owner_check");
                }
            }

            // Check confidence threshold
            if (config.EnforceConfidenceThreshold && !MeetsConfidenceThreshold(mutation, config.ConfidenceThreshold))
            {
                return AuthorityValidationResult.Unauthorized(
                    $"Confidence {mutation.Confidence:F2} below threshold {config.ConfidenceThreshold:F2}",
                    "confidence_threshold");
            }

            return AuthorityValidationResult.Authorized();
        }

        /// <summary>
        /// Filters mutations, returning only those that pass authority validation.
        /// </summary>
        /// <param name="mutations">The mutations to filter.</param>
        /// <param name="config">The authority configuration.</param>
        /// <param name="onUnauthorized">Optional callback for unauthorized mutations.</param>
        /// <returns>List of authorized mutations.</returns>
        public static List<ProposedMutation> FilterAuthorizedMutations(
            IReadOnlyList<ProposedMutation>? mutations,
            AuthorityConfig? config = null,
            Action<int, ProposedMutation, AuthorityValidationResult>? onUnauthorized = null)
        {
            var authorized = new List<ProposedMutation>();

            if (mutations == null)
                return authorized;

            config ??= AuthorityConfig.Default;

            for (int i = 0; i < mutations.Count; i++)
            {
                var mutation = mutations[i];
                var result = ValidateRelationshipMutation(mutation, config);

                if (result.IsAuthorized)
                {
                    authorized.Add(mutation);
                }
                else
                {
                    onUnauthorized?.Invoke(i, mutation, result);
                }
            }

            return authorized;
        }

        /// <summary>
        /// Validates a relationship entry's fields are within valid ranges.
        /// </summary>
        /// <param name="relationship">The relationship to validate.</param>
        /// <returns>Schema validation result.</returns>
        public static StructuredSchemaValidator.SchemaValidationResult ValidateRelationshipEntry(RelationshipEntry? relationship)
        {
            if (relationship == null)
                return StructuredSchemaValidator.SchemaValidationResult.Failure("Relationship is null", "relationship");

            // Required fields
            if (string.IsNullOrWhiteSpace(relationship.SourceEntity))
                return StructuredSchemaValidator.SchemaValidationResult.Failure("SourceEntity is required", "sourceEntity");

            if (string.IsNullOrWhiteSpace(relationship.TargetEntity))
                return StructuredSchemaValidator.SchemaValidationResult.Failure("TargetEntity is required", "targetEntity");

            if (string.IsNullOrWhiteSpace(relationship.RelationshipType))
                return StructuredSchemaValidator.SchemaValidationResult.Failure("RelationshipType is required", "relationshipType");

            // Range validations
            if (relationship.Affinity < -1f || relationship.Affinity > 1f)
                return StructuredSchemaValidator.SchemaValidationResult.Failure(
                    $"Affinity must be between -1 and 1 (got {relationship.Affinity})", "affinity");

            if (relationship.Trust < 0f || relationship.Trust > 1f)
                return StructuredSchemaValidator.SchemaValidationResult.Failure(
                    $"Trust must be between 0 and 1 (got {relationship.Trust})", "trust");

            if (relationship.Familiarity < 0f || relationship.Familiarity > 1f)
                return StructuredSchemaValidator.SchemaValidationResult.Failure(
                    $"Familiarity must be between 0 and 1 (got {relationship.Familiarity})", "familiarity");

            return StructuredSchemaValidator.SchemaValidationResult.Success();
        }

        /// <summary>
        /// Filters relationship entries, returning only those that pass validation.
        /// </summary>
        /// <param name="relationships">The relationships to filter.</param>
        /// <param name="onInvalid">Optional callback for invalid relationships.</param>
        /// <returns>List of valid relationships.</returns>
        public static List<RelationshipEntry> FilterValidRelationships(
            IReadOnlyList<RelationshipEntry>? relationships,
            Action<int, RelationshipEntry, StructuredSchemaValidator.SchemaValidationResult>? onInvalid = null)
        {
            var valid = new List<RelationshipEntry>();

            if (relationships == null)
                return valid;

            for (int i = 0; i < relationships.Count; i++)
            {
                var relationship = relationships[i];
                var result = ValidateRelationshipEntry(relationship);

                if (result.IsValid)
                {
                    valid.Add(relationship);
                }
                else
                {
                    onInvalid?.Invoke(i, relationship, result);
                }
            }

            return valid;
        }
    }
}
