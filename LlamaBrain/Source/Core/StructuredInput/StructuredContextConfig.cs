namespace LlamaBrain.Core.StructuredInput
{
    /// <summary>
    /// Configuration for structured context handling.
    /// Controls how context is formatted and injected into prompts.
    /// </summary>
    public sealed class StructuredContextConfig
    {
        /// <summary>
        /// The preferred structured context format to use.
        /// Default is JsonContext for maximum compatibility.
        /// </summary>
        public StructuredContextFormat PreferredFormat { get; set; } = StructuredContextFormat.JsonContext;

        /// <summary>
        /// Whether to fall back to text-based prompt assembly when
        /// structured context fails or is not supported.
        /// </summary>
        public bool FallbackToTextAssembly { get; set; } = true;

        /// <summary>
        /// Whether to validate the context schema before injection.
        /// When true, malformed context will throw an exception.
        /// </summary>
        public bool ValidateSchema { get; set; } = true;

        /// <summary>
        /// Opening tag for context JSON block.
        /// </summary>
        public string ContextBlockOpenTag { get; set; } = "<context_json>";

        /// <summary>
        /// Closing tag for context JSON block.
        /// </summary>
        public string ContextBlockCloseTag { get; set; } = "</context_json>";

        /// <summary>
        /// Whether to use compact JSON (no indentation) to save tokens.
        /// </summary>
        public bool UseCompactJson { get; set; } = false;

        /// <summary>
        /// Default configuration with JsonContext format and fallback enabled.
        /// </summary>
        public static StructuredContextConfig Default => new StructuredContextConfig();

        /// <summary>
        /// Configuration that disables structured context and uses
        /// text-based prompt assembly (legacy behavior).
        /// </summary>
        public static StructuredContextConfig TextOnly
        {
            get
            {
                return new StructuredContextConfig
                {
                    PreferredFormat = StructuredContextFormat.None,
                    FallbackToTextAssembly = true
                };
            }
        }

        /// <summary>
        /// Strict configuration with JsonContext format, schema validation,
        /// and no fallback. Will throw on any context errors.
        /// </summary>
        public static StructuredContextConfig Strict
        {
            get
            {
                return new StructuredContextConfig
                {
                    PreferredFormat = StructuredContextFormat.JsonContext,
                    FallbackToTextAssembly = false,
                    ValidateSchema = true
                };
            }
        }
    }
}
