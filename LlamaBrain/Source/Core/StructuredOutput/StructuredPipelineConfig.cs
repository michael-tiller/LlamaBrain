using System;

namespace LlamaBrain.Core.StructuredOutput
{
    /// <summary>
    /// Configuration for the structured dialogue pipeline.
    /// Controls how dialogue processing uses structured output with fallback options.
    /// </summary>
    [Serializable]
    public sealed class StructuredPipelineConfig
    {
        /// <summary>
        /// Whether to use native structured output (json_schema) for LLM requests.
        /// When true, the pipeline uses llama.cpp json_schema parameter.
        /// When false, uses legacy regex parsing.
        /// </summary>
        public bool UseStructuredOutput { get; set; } = true;

        /// <summary>
        /// Whether to fall back to regex parsing if structured output fails.
        /// Recommended to keep true for maximum reliability.
        /// </summary>
        public bool FallbackToRegex { get; set; } = true;

        /// <summary>
        /// Maximum number of retries when validation fails.
        /// Each retry escalates constraints via StateSnapshot.ForRetry().
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Whether to track metrics for structured output success/failure rates.
        /// </summary>
        public bool TrackMetrics { get; set; } = true;

        /// <summary>
        /// Whether to validate mutation schemas before execution.
        /// When true, malformed mutations are rejected before reaching MemoryMutationController.
        /// </summary>
        public bool ValidateMutationSchemas { get; set; } = true;

        /// <summary>
        /// Whether to validate intent schemas before dispatch.
        /// When true, malformed intents are rejected before reaching WorldIntentDispatcher.
        /// </summary>
        public bool ValidateIntentSchemas { get; set; } = true;

        /// <summary>
        /// The underlying structured output configuration for the LLM API.
        /// </summary>
        public StructuredOutputConfig StructuredOutputConfig { get; set; } = StructuredOutputConfig.Default;

        /// <summary>
        /// Default configuration: structured output with regex fallback.
        /// Recommended for production use.
        /// </summary>
        public static StructuredPipelineConfig Default => new StructuredPipelineConfig();

        /// <summary>
        /// Strict configuration: structured output only, no fallback.
        /// Use when you need guaranteed structured output or explicit failure.
        /// </summary>
        public static StructuredPipelineConfig StructuredOnly
        {
            get
            {
                return new StructuredPipelineConfig
                {
                    UseStructuredOutput = true,
                    FallbackToRegex = false,
                    StructuredOutputConfig = StructuredOutputConfig.Strict
                };
            }
        }

        /// <summary>
        /// Legacy configuration: regex parsing only, no structured output.
        /// Use for backward compatibility or when structured output is not available.
        /// </summary>
        public static StructuredPipelineConfig RegexOnly
        {
            get
            {
                return new StructuredPipelineConfig
                {
                    UseStructuredOutput = false,
                    FallbackToRegex = true,
                    StructuredOutputConfig = StructuredOutputConfig.PromptInjectionOnly
                };
            }
        }
    }
}
