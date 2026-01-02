using System;
using System.Collections.Generic;
using System.Linq;
using LlamaBrain.Core.Validation;

namespace LlamaBrain.Core.StructuredOutput
{
    /// <summary>
    /// Validates structured mutations and intents against their expected schemas.
    /// Used by StructuredDialoguePipeline when ValidateMutationSchemas or ValidateIntentSchemas is enabled.
    /// </summary>
    public static class StructuredSchemaValidator
    {
        /// <summary>
        /// Valid mutation type names (case-insensitive).
        /// </summary>
        private static readonly HashSet<string> ValidMutationTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AppendEpisodic",
            "TransformBelief",
            "TransformRelationship",
            "EmitWorldIntent"
        };

        /// <summary>
        /// Result of schema validation.
        /// </summary>
        public sealed class SchemaValidationResult
        {
            /// <summary>
            /// Whether validation passed.
            /// </summary>
            public bool IsValid { get; }

            /// <summary>
            /// Error message if validation failed.
            /// </summary>
            public string? ErrorMessage { get; }

            /// <summary>
            /// The field that failed validation (if any).
            /// </summary>
            public string? FailedField { get; }

            private SchemaValidationResult(bool isValid, string? errorMessage = null, string? failedField = null)
            {
                IsValid = isValid;
                ErrorMessage = errorMessage;
                FailedField = failedField;
            }

            /// <summary>
            /// Creates a successful validation result.
            /// </summary>
            public static SchemaValidationResult Success() => new SchemaValidationResult(true);

            /// <summary>
            /// Creates a failed validation result.
            /// </summary>
            public static SchemaValidationResult Failure(string errorMessage, string? failedField = null)
                => new SchemaValidationResult(false, errorMessage, failedField);

            /// <inheritdoc/>
            public override string ToString()
                => IsValid ? "Valid" : $"Invalid: {ErrorMessage} (field: {FailedField ?? "unknown"})";
        }

        /// <summary>
        /// Validates a structured mutation against the expected schema.
        /// </summary>
        /// <param name="mutation">The mutation to validate.</param>
        /// <returns>Validation result with error details if invalid.</returns>
        public static SchemaValidationResult ValidateMutation(StructuredMutation? mutation)
        {
            if (mutation == null)
            {
                return SchemaValidationResult.Failure("Mutation is null", "mutation");
            }

            // Validate Type field
            if (string.IsNullOrWhiteSpace(mutation.Type))
            {
                return SchemaValidationResult.Failure("Mutation type is required", "type");
            }

            if (!ValidMutationTypes.Contains(mutation.Type))
            {
                return SchemaValidationResult.Failure(
                    $"Invalid mutation type '{mutation.Type}'. Valid types: {string.Join(", ", ValidMutationTypes)}",
                    "type");
            }

            // Validate Content field (required for all types)
            if (string.IsNullOrWhiteSpace(mutation.Content))
            {
                return SchemaValidationResult.Failure("Mutation content is required", "content");
            }

            // Type-specific validation
            var mutationType = mutation.Type.ToLowerInvariant();

            switch (mutationType)
            {
                case "transformbelief":
                    if (string.IsNullOrWhiteSpace(mutation.Target))
                    {
                        return SchemaValidationResult.Failure(
                            "TransformBelief requires a target (belief ID)", "target");
                    }
                    if (mutation.Confidence < 0 || mutation.Confidence > 1)
                    {
                        return SchemaValidationResult.Failure(
                            $"Confidence must be between 0 and 1 (got {mutation.Confidence})", "confidence");
                    }
                    break;

                case "transformrelationship":
                    if (string.IsNullOrWhiteSpace(mutation.Target))
                    {
                        return SchemaValidationResult.Failure(
                            "TransformRelationship requires a target (entity ID)", "target");
                    }
                    break;

                case "emitworldintent":
                    if (string.IsNullOrWhiteSpace(mutation.Target))
                    {
                        return SchemaValidationResult.Failure(
                            "EmitWorldIntent requires a target (intent type)", "target");
                    }
                    break;

                case "appendepisodic":
                    // Content already validated above
                    break;
            }

            return SchemaValidationResult.Success();
        }

        /// <summary>
        /// Validates a ProposedMutation against the expected schema.
        /// </summary>
        /// <param name="mutation">The mutation to validate.</param>
        /// <returns>Validation result with error details if invalid.</returns>
        public static SchemaValidationResult ValidateMutation(ProposedMutation? mutation)
        {
            if (mutation == null)
            {
                return SchemaValidationResult.Failure("Mutation is null", "mutation");
            }

            // Validate Content field (required for all types)
            if (string.IsNullOrWhiteSpace(mutation.Content))
            {
                return SchemaValidationResult.Failure("Mutation content is required", "content");
            }

            // Type-specific validation
            switch (mutation.Type)
            {
                case MutationType.TransformBelief:
                    if (string.IsNullOrWhiteSpace(mutation.Target))
                    {
                        return SchemaValidationResult.Failure(
                            "TransformBelief requires a target (belief ID)", "target");
                    }
                    if (mutation.Confidence < 0 || mutation.Confidence > 1)
                    {
                        return SchemaValidationResult.Failure(
                            $"Confidence must be between 0 and 1 (got {mutation.Confidence})", "confidence");
                    }
                    break;

                case MutationType.TransformRelationship:
                    if (string.IsNullOrWhiteSpace(mutation.Target))
                    {
                        return SchemaValidationResult.Failure(
                            "TransformRelationship requires a target (entity ID)", "target");
                    }
                    break;

                case MutationType.EmitWorldIntent:
                    if (string.IsNullOrWhiteSpace(mutation.Target))
                    {
                        return SchemaValidationResult.Failure(
                            "EmitWorldIntent requires a target (intent type)", "target");
                    }
                    break;

                case MutationType.AppendEpisodic:
                    // Content already validated above
                    break;

                default:
                    return SchemaValidationResult.Failure(
                        $"Unknown mutation type: {mutation.Type}", "type");
            }

            return SchemaValidationResult.Success();
        }

        /// <summary>
        /// Validates a structured intent against the expected schema.
        /// </summary>
        /// <param name="intent">The intent to validate.</param>
        /// <returns>Validation result with error details if invalid.</returns>
        public static SchemaValidationResult ValidateIntent(StructuredIntent? intent)
        {
            if (intent == null)
            {
                return SchemaValidationResult.Failure("Intent is null", "intent");
            }

            // Validate IntentType field (required)
            if (string.IsNullOrWhiteSpace(intent.IntentType))
            {
                return SchemaValidationResult.Failure("Intent type is required", "intentType");
            }

            // Validate Priority (must be non-negative)
            if (intent.Priority < 0)
            {
                return SchemaValidationResult.Failure(
                    $"Priority must be non-negative (got {intent.Priority})", "priority");
            }

            // Parameters dictionary should not be null (but can be empty)
            if (intent.Parameters == null)
            {
                return SchemaValidationResult.Failure("Parameters cannot be null", "parameters");
            }

            return SchemaValidationResult.Success();
        }

        /// <summary>
        /// Validates a WorldIntent against the expected schema.
        /// </summary>
        /// <param name="intent">The intent to validate.</param>
        /// <returns>Validation result with error details if invalid.</returns>
        public static SchemaValidationResult ValidateIntent(WorldIntent? intent)
        {
            if (intent == null)
            {
                return SchemaValidationResult.Failure("Intent is null", "intent");
            }

            // Validate IntentType field (required)
            if (string.IsNullOrWhiteSpace(intent.IntentType))
            {
                return SchemaValidationResult.Failure("Intent type is required", "intentType");
            }

            // Validate Priority (must be non-negative)
            if (intent.Priority < 0)
            {
                return SchemaValidationResult.Failure(
                    $"Priority must be non-negative (got {intent.Priority})", "priority");
            }

            // Parameters dictionary should not be null (but can be empty)
            if (intent.Parameters == null)
            {
                return SchemaValidationResult.Failure("Parameters cannot be null", "parameters");
            }

            return SchemaValidationResult.Success();
        }

        /// <summary>
        /// Validates all mutations in a parsed output.
        /// </summary>
        /// <param name="mutations">The mutations to validate.</param>
        /// <returns>List of validation failures (empty if all valid).</returns>
        public static List<(int Index, SchemaValidationResult Result)> ValidateAllMutations(
            IReadOnlyList<ProposedMutation>? mutations)
        {
            var failures = new List<(int, SchemaValidationResult)>();

            if (mutations == null || mutations.Count == 0)
            {
                return failures;
            }

            for (int i = 0; i < mutations.Count; i++)
            {
                var result = ValidateMutation(mutations[i]);
                if (!result.IsValid)
                {
                    failures.Add((i, result));
                }
            }

            return failures;
        }

        /// <summary>
        /// Validates all intents in a parsed output.
        /// </summary>
        /// <param name="intents">The intents to validate.</param>
        /// <returns>List of validation failures (empty if all valid).</returns>
        public static List<(int Index, SchemaValidationResult Result)> ValidateAllIntents(
            IReadOnlyList<WorldIntent>? intents)
        {
            var failures = new List<(int, SchemaValidationResult)>();

            if (intents == null || intents.Count == 0)
            {
                return failures;
            }

            for (int i = 0; i < intents.Count; i++)
            {
                var result = ValidateIntent(intents[i]);
                if (!result.IsValid)
                {
                    failures.Add((i, result));
                }
            }

            return failures;
        }

        /// <summary>
        /// Filters mutations, returning only those that pass schema validation.
        /// Invalid mutations are logged via the optional callback.
        /// </summary>
        /// <param name="mutations">The mutations to filter.</param>
        /// <param name="onInvalid">Optional callback for invalid mutations.</param>
        /// <returns>List of valid mutations.</returns>
        public static List<ProposedMutation> FilterValidMutations(
            IReadOnlyList<ProposedMutation>? mutations,
            Action<int, ProposedMutation, SchemaValidationResult>? onInvalid = null)
        {
            var valid = new List<ProposedMutation>();

            if (mutations == null)
            {
                return valid;
            }

            for (int i = 0; i < mutations.Count; i++)
            {
                var mutation = mutations[i];
                var result = ValidateMutation(mutation);

                if (result.IsValid)
                {
                    valid.Add(mutation);
                }
                else
                {
                    onInvalid?.Invoke(i, mutation, result);
                }
            }

            return valid;
        }

        /// <summary>
        /// Filters intents, returning only those that pass schema validation.
        /// Invalid intents are logged via the optional callback.
        /// </summary>
        /// <param name="intents">The intents to filter.</param>
        /// <param name="onInvalid">Optional callback for invalid intents.</param>
        /// <returns>List of valid intents.</returns>
        public static List<WorldIntent> FilterValidIntents(
            IReadOnlyList<WorldIntent>? intents,
            Action<int, WorldIntent, SchemaValidationResult>? onInvalid = null)
        {
            var valid = new List<WorldIntent>();

            if (intents == null)
            {
                return valid;
            }

            for (int i = 0; i < intents.Count; i++)
            {
                var intent = intents[i];
                var result = ValidateIntent(intent);

                if (result.IsValid)
                {
                    valid.Add(intent);
                }
                else
                {
                    onInvalid?.Invoke(i, intent, result);
                }
            }

            return valid;
        }
    }
}
