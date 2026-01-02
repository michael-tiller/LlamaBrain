namespace LlamaBrain.Core.StructuredOutput
{
    /// <summary>
    /// Configuration for structured output handling.
    /// Controls how the system requests and parses structured responses from the LLM.
    /// </summary>
    public sealed class StructuredOutputConfig
    {
        /// <summary>
        /// The preferred structured output format to use.
        /// Default is JsonSchema for maximum reliability.
        /// </summary>
        public StructuredOutputFormat PreferredFormat { get; set; } = StructuredOutputFormat.JsonSchema;

        /// <summary>
        /// Whether to fall back to prompt injection when native structured output
        /// is not available or fails. When true, the system will inject JSON
        /// instructions into the prompt and use regex parsing.
        /// </summary>
        public bool FallbackToPromptInjection { get; set; } = true;

        /// <summary>
        /// Whether to validate the JSON schema before sending to the LLM.
        /// When true, malformed schemas will throw an exception rather than
        /// being sent to the LLM.
        /// </summary>
        public bool ValidateSchema { get; set; } = true;

        /// <summary>
        /// Whether to strictly enforce schema compliance in parsed responses.
        /// When true, responses that don't match the schema will be rejected.
        /// When false, best-effort parsing is used.
        /// </summary>
        public bool StrictSchemaValidation { get; set; } = false;

        /// <summary>
        /// Default configuration with JsonSchema format and fallback enabled.
        /// </summary>
        public static StructuredOutputConfig Default => new StructuredOutputConfig();

        /// <summary>
        /// Configuration that disables native structured output and uses
        /// prompt injection with regex parsing (legacy behavior).
        /// </summary>
        public static StructuredOutputConfig PromptInjectionOnly
        {
            get
            {
                return new StructuredOutputConfig
                {
                    PreferredFormat = StructuredOutputFormat.None,
                    FallbackToPromptInjection = true
                };
            }
        }

        /// <summary>
        /// Strict configuration with JsonSchema format, schema validation,
        /// and no fallback. Will throw on any parsing or schema errors.
        /// </summary>
        public static StructuredOutputConfig Strict
        {
            get
            {
                return new StructuredOutputConfig
                {
                    PreferredFormat = StructuredOutputFormat.JsonSchema,
                    FallbackToPromptInjection = false,
                    ValidateSchema = true,
                    StrictSchemaValidation = true
                };
            }
        }
    }
}
