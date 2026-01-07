using System;
using System.Threading;
using System.Threading.Tasks;
using LlamaBrain.Core.Validation;
using LlamaBrain.Persona;
using LlamaBrain.Utilities;

namespace LlamaBrain.Core.StructuredOutput
{
    /// <summary>
    /// Unified pipeline for processing dialogue using structured output.
    /// Orchestrates the complete flow from LLM request through validation to mutation execution.
    /// </summary>
    /// <remarks>
    /// Pipeline flow:
    /// 1. Send prompt to LLM with json_schema constraint
    /// 2. Parse response with OutputParser.ParseStructured()
    /// 3. Validate with ValidationGate
    /// 4. Execute mutations with MemoryMutationController
    /// 5. Return unified result with metrics
    /// </remarks>
    public sealed class StructuredDialoguePipeline
    {
        private readonly BrainAgent _agent;
        private readonly ValidationGate _validationGate;
        private readonly MemoryMutationController? _mutationController;
        private readonly AuthoritativeMemorySystem? _memorySystem;
        private readonly OutputParser _structuredParser;
        private readonly OutputParser _regexParser;
        private readonly StructuredPipelineConfig _config;
        private readonly StructuredPipelineMetrics _metrics;

        /// <summary>
        /// Gets the pipeline configuration.
        /// </summary>
        public StructuredPipelineConfig Config => _config;

        /// <summary>
        /// Gets the pipeline metrics.
        /// </summary>
        public StructuredPipelineMetrics Metrics => _metrics;

        /// <summary>
        /// Creates a new structured dialogue pipeline.
        /// </summary>
        /// <param name="agent">The BrainAgent for LLM communication.</param>
        /// <param name="validationGate">The validation gate for output validation.</param>
        /// <param name="mutationController">Optional mutation controller for executing mutations.</param>
        /// <param name="memorySystem">Optional memory system for mutation execution (required if mutationController is provided).</param>
        /// <param name="config">Optional pipeline configuration (uses Default if not provided).</param>
        public StructuredDialoguePipeline(
            BrainAgent agent,
            ValidationGate validationGate,
            MemoryMutationController? mutationController = null,
            AuthoritativeMemorySystem? memorySystem = null,
            StructuredPipelineConfig? config = null)
        {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
            _validationGate = validationGate ?? throw new ArgumentNullException(nameof(validationGate));
            _mutationController = mutationController;
            _memorySystem = memorySystem;
            _config = config ?? StructuredPipelineConfig.Default;
            _metrics = new StructuredPipelineMetrics();

            // Validate that memory system is provided if mutation controller is provided
            if (_mutationController != null && _memorySystem == null)
            {
                throw new ArgumentException(
                    "memorySystem is required when mutationController is provided",
                    nameof(memorySystem));
            }

            // Create parsers for structured and regex modes
            _structuredParser = new OutputParser(OutputParserConfig.NativeStructured);
            _regexParser = new OutputParser(OutputParserConfig.Default);
        }

        /// <summary>
        /// Processes a player message through the complete dialogue pipeline.
        /// </summary>
        /// <param name="playerInput">The player's input message.</param>
        /// <param name="context">Optional validation context for constraint checking.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The complete pipeline result including dialogue, mutations, and metrics.</returns>
        public async Task<StructuredPipelineResult> ProcessDialogueAsync(
            string playerInput,
            ValidationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(playerInput))
            {
                return StructuredPipelineResult.Failed("Player input cannot be null or empty");
            }

            try
            {
                return await ProcessWithRetryAsync(playerInput, context, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Error($"Pipeline error: {ex.Message}");
                return StructuredPipelineResult.Failed($"Pipeline error: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes dialogue with retry logic for validation failures.
        /// </summary>
        private async Task<StructuredPipelineResult> ProcessWithRetryAsync(
            string playerInput,
            ValidationContext? context,
            CancellationToken cancellationToken)
        {
            int retryCount = 0;
            StructuredPipelineResult? lastResult = null;

            while (retryCount <= _config.MaxRetries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Generate and validate response
                var result = await GenerateAndValidateAsync(playerInput, context, cancellationToken);
                result.RetryCount = retryCount;

                // Success - return immediately
                if (result.Success)
                {
                    return result;
                }

                lastResult = result;

                // Check if we should retry
                bool shouldRetry = result.GateResult?.ShouldRetry == true &&
                                   retryCount < _config.MaxRetries;

                if (!shouldRetry)
                {
                    break;
                }

                // Retry
                retryCount++;
                _metrics.RecordRetry();
                Logger.Info($"Validation failed, retrying ({retryCount}/{_config.MaxRetries})...");
            }

            // Return last result (failed after all retries)
            return lastResult ?? StructuredPipelineResult.Failed("No result generated");
        }

        /// <summary>
        /// Generates a response and validates it (single attempt, no retry).
        /// </summary>
        private async Task<StructuredPipelineResult> GenerateAndValidateAsync(
            string playerInput,
            ValidationContext? context,
            CancellationToken cancellationToken)
        {
            // Try structured output first if enabled
            if (_config.UseStructuredOutput)
            {
                var result = await TryStructuredOutputAsync(playerInput, context, cancellationToken);

                if (result.Success)
                {
                    return result;
                }

                // Fall back to regex if configured
                if (_config.FallbackToRegex)
                {
                    _metrics.RecordStructuredFailure();
                    return await TryRegexFallbackAsync(playerInput, context, cancellationToken);
                }

                return result;
            }

            // Use regex mode directly
            _metrics.RecordRegexDirect();
            return await TryRegexParsingAsync(playerInput, context, cancellationToken);
        }

        /// <summary>
        /// Attempts to process using native structured output.
        /// </summary>
        private async Task<StructuredPipelineResult> TryStructuredOutputAsync(
            string playerInput,
            ValidationContext? context,
            CancellationToken cancellationToken)
        {
            try
            {
                // Send with native structured output
                var parsedOutput = await _agent.SendNativeDialogueAsync(
                    playerInput,
                    StructuredOutputFormat.JsonSchema,
                    seed: null,
                    cancellationToken);

                if (!parsedOutput.Success)
                {
                    return StructuredPipelineResult.Failed(
                        parsedOutput.ErrorMessage ?? "Structured output parsing failed",
                        parsedOutput.RawOutput,
                        ParseMode.Structured);
                }

                // Validate the output
                return await ValidateAndExecuteAsync(parsedOutput, context, ParseMode.Structured);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Structured output failed: {ex.Message}");
                return StructuredPipelineResult.Failed(
                    $"Structured output error: {ex.Message}",
                    "",
                    ParseMode.Structured);
            }
        }

        /// <summary>
        /// Falls back to regex parsing after structured output failure.
        /// </summary>
        private async Task<StructuredPipelineResult> TryRegexFallbackAsync(
            string playerInput,
            ValidationContext? context,
            CancellationToken cancellationToken)
        {
            try
            {
                _metrics.RecordFallbackToRegex();

                // Send without structured output constraint
                var response = await _agent.SendMessageAsync(playerInput, seed: null, cancellationToken);

                // Parse with regex
                var parsedOutput = _regexParser.Parse(response);

                if (!parsedOutput.Success)
                {
                    return StructuredPipelineResult.Failed(
                        parsedOutput.ErrorMessage ?? "Regex parsing failed",
                        response,
                        ParseMode.Fallback);
                }

                // Validate and execute
                return await ValidateAndExecuteAsync(parsedOutput, context, ParseMode.Fallback);
            }
            catch (Exception ex)
            {
                Logger.Error($"Regex fallback failed: {ex.Message}");
                return StructuredPipelineResult.Failed(
                    $"Fallback error: {ex.Message}",
                    "",
                    ParseMode.Fallback);
            }
        }

        /// <summary>
        /// Processes using regex parsing directly (legacy mode).
        /// </summary>
        private async Task<StructuredPipelineResult> TryRegexParsingAsync(
            string playerInput,
            ValidationContext? context,
            CancellationToken cancellationToken)
        {
            try
            {
                // Send without structured output constraint
                var response = await _agent.SendMessageAsync(playerInput, seed: null, cancellationToken);

                // Parse with regex
                var parsedOutput = _regexParser.Parse(response);

                if (!parsedOutput.Success)
                {
                    return StructuredPipelineResult.Failed(
                        parsedOutput.ErrorMessage ?? "Regex parsing failed",
                        response,
                        ParseMode.Regex);
                }

                // Validate and execute
                return await ValidateAndExecuteAsync(parsedOutput, context, ParseMode.Regex);
            }
            catch (Exception ex)
            {
                Logger.Error($"Regex parsing failed: {ex.Message}");
                return StructuredPipelineResult.Failed(
                    $"Regex error: {ex.Message}",
                    "",
                    ParseMode.Regex);
            }
        }

        /// <summary>
        /// Validates the parsed output and executes mutations if validation passes.
        /// </summary>
        private async Task<StructuredPipelineResult> ValidateAndExecuteAsync(
            ParsedOutput parsedOutput,
            ValidationContext? context,
            ParseMode parseMode)
        {
            // Pre-validate mutation schemas if enabled
            if (_config.ValidateMutationSchemas && parsedOutput.ProposedMutations.Count > 0)
            {
                var validMutations = StructuredSchemaValidator.FilterValidMutations(
                    parsedOutput.ProposedMutations,
                    (index, mutation, result) =>
                    {
                        Logger.Warn($"Schema validation failed for mutation {index}: {result.ErrorMessage}");
                        _metrics.RecordValidationFailure();
                    });

                // Replace with validated mutations only
                parsedOutput = parsedOutput.WithMutationsReplaced(validMutations);
            }

            // Pre-validate intent schemas if enabled
            if (_config.ValidateIntentSchemas && parsedOutput.WorldIntents.Count > 0)
            {
                var validIntents = StructuredSchemaValidator.FilterValidIntents(
                    parsedOutput.WorldIntents,
                    (index, intent, result) =>
                    {
                        Logger.Warn($"Schema validation failed for intent {index}: {result.ErrorMessage}");
                        _metrics.RecordValidationFailure();
                    });

                // Replace with validated intents only
                parsedOutput = parsedOutput.WithIntentsReplaced(validIntents);
            }

            // Run validation gate
            var gateResult = _validationGate.Validate(parsedOutput, context);

            if (!gateResult.Passed)
            {
                _metrics.RecordValidationFailure();
                return StructuredPipelineResult.ValidationFailed(gateResult, parsedOutput, parseMode);
            }

            // Execute mutations if controller is available
            MutationBatchResult? mutationResult = null;

            if (_mutationController != null && _memorySystem != null && gateResult.ApprovedMutations.Count > 0)
            {
                mutationResult = _mutationController.ExecuteMutations(gateResult, _memorySystem);
                _metrics.RecordMutationsExecuted(mutationResult.SuccessCount);
                _metrics.RecordIntentsEmitted(mutationResult.EmittedIntents.Count);
            }

            // Record success
            if (parseMode == ParseMode.Structured)
            {
                _metrics.RecordStructuredSuccess();
            }

            return StructuredPipelineResult.Succeeded(
                parsedOutput.DialogueText,
                parsedOutput,
                gateResult,
                mutationResult,
                parseMode);
        }

        /// <summary>
        /// Resets the pipeline metrics.
        /// </summary>
        public void ResetMetrics()
        {
            _metrics.Reset();
        }
    }
}
