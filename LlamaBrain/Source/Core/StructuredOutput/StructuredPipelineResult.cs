using System;
using LlamaBrain.Core.Validation;
using LlamaBrain.Persona;

namespace LlamaBrain.Core.StructuredOutput
{
    /// <summary>
    /// The mode used to parse the LLM response.
    /// </summary>
    public enum ParseMode
    {
        /// <summary>Native structured output (json_schema) was used.</summary>
        Structured = 0,

        /// <summary>Regex parsing was used (legacy mode).</summary>
        Regex = 1,

        /// <summary>Structured output failed and regex fallback was used.</summary>
        Fallback = 2
    }

    /// <summary>
    /// Result of processing dialogue through the structured pipeline.
    /// Contains the complete output including validation, mutations, and metrics.
    /// </summary>
    [Serializable]
    public sealed class StructuredPipelineResult
    {
        /// <summary>
        /// Whether the pipeline completed successfully.
        /// True if parsing succeeded and validation passed (or retry succeeded).
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The dialogue text to display to the player.
        /// </summary>
        public string DialogueText { get; set; } = "";

        /// <summary>
        /// The raw response from the LLM.
        /// </summary>
        public string RawResponse { get; set; } = "";

        /// <summary>
        /// The parsed output from the LLM response.
        /// Contains dialogue, mutations, and intents.
        /// </summary>
        public ParsedOutput? ParsedOutput { get; set; }

        /// <summary>
        /// The result of validation gate processing.
        /// Contains approved/rejected mutations and intents.
        /// </summary>
        public GateResult? GateResult { get; set; }

        /// <summary>
        /// The result of mutation execution (if mutations were approved).
        /// </summary>
        public MutationBatchResult? MutationResult { get; set; }

        /// <summary>
        /// The parsing mode that was used.
        /// </summary>
        public ParseMode ParseMode { get; set; } = ParseMode.Structured;

        /// <summary>
        /// Error message if the pipeline failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Number of retries that were attempted.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Whether fallback to regex parsing was used.
        /// </summary>
        public bool UsedFallback => ParseMode == ParseMode.Fallback;

        /// <summary>
        /// Whether validation passed (all gates cleared).
        /// </summary>
        public bool ValidationPassed => GateResult?.Passed ?? false;

        /// <summary>
        /// Number of mutations that were executed.
        /// </summary>
        public int MutationsExecuted => MutationResult?.SuccessCount ?? 0;

        /// <summary>
        /// Number of intents that were emitted.
        /// </summary>
        public int IntentsEmitted => MutationResult?.EmittedIntents?.Count ?? 0;

        /// <summary>
        /// Creates a successful pipeline result.
        /// </summary>
        /// <param name="dialogueText">The dialogue text.</param>
        /// <param name="parsedOutput">The parsed output.</param>
        /// <param name="gateResult">The validation gate result.</param>
        /// <param name="mutationResult">The mutation execution result.</param>
        /// <param name="parseMode">The parsing mode used.</param>
        /// <returns>A successful pipeline result.</returns>
        public static StructuredPipelineResult Succeeded(
            string dialogueText,
            ParsedOutput parsedOutput,
            GateResult gateResult,
            MutationBatchResult? mutationResult = null,
            ParseMode parseMode = ParseMode.Structured)
        {
            return new StructuredPipelineResult
            {
                Success = true,
                DialogueText = dialogueText,
                RawResponse = parsedOutput.RawOutput,
                ParsedOutput = parsedOutput,
                GateResult = gateResult,
                MutationResult = mutationResult,
                ParseMode = parseMode
            };
        }

        /// <summary>
        /// Creates a failed pipeline result.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="rawResponse">The raw LLM response (if available).</param>
        /// <param name="parseMode">The parsing mode that was attempted.</param>
        /// <returns>A failed pipeline result.</returns>
        public static StructuredPipelineResult Failed(
            string errorMessage,
            string rawResponse = "",
            ParseMode parseMode = ParseMode.Structured)
        {
            return new StructuredPipelineResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                RawResponse = rawResponse,
                ParseMode = parseMode
            };
        }

        /// <summary>
        /// Creates a result indicating validation failed.
        /// </summary>
        /// <param name="gateResult">The gate result with failures.</param>
        /// <param name="parsedOutput">The parsed output that failed validation.</param>
        /// <param name="parseMode">The parsing mode used.</param>
        /// <returns>A pipeline result indicating validation failure.</returns>
        public static StructuredPipelineResult ValidationFailed(
            GateResult gateResult,
            ParsedOutput parsedOutput,
            ParseMode parseMode = ParseMode.Structured)
        {
            return new StructuredPipelineResult
            {
                Success = false,
                DialogueText = parsedOutput.DialogueText,
                RawResponse = parsedOutput.RawOutput,
                ParsedOutput = parsedOutput,
                GateResult = gateResult,
                ParseMode = parseMode,
                ErrorMessage = $"Validation failed: {gateResult.Failures.Count} failures"
            };
        }

        /// <inheritdoc/>
        /// <returns>A string representation of the pipeline result including success status and dialogue text.</returns>
        public override string ToString()
        {
            if (Success)
            {
                return $"PipelineResult[OK] Mode={ParseMode}, Retries={RetryCount}, " +
                       $"Mutations={MutationsExecuted}, Intents={IntentsEmitted}";
            }
            return $"PipelineResult[FAIL] Mode={ParseMode}, Error={ErrorMessage}";
        }
    }
}
