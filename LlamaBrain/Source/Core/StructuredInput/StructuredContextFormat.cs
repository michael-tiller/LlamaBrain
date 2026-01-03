namespace LlamaBrain.Core.StructuredInput
{
    /// <summary>
    /// Format for structured context injection into prompts.
    /// Explicit numeric values per determinism contract.
    /// </summary>
    public enum StructuredContextFormat
    {
        /// <summary>
        /// No structured context - use legacy text-based prompt assembly.
        /// </summary>
        None = 0,

        /// <summary>
        /// JSON context block injected into prompt with delimiters.
        /// Supported by all LLM backends.
        /// </summary>
        JsonContext = 1,

        /// <summary>
        /// Function calling / tool use for context injection.
        /// Only supported by OpenAI, Anthropic APIs (not llama.cpp).
        /// </summary>
        FunctionCalling = 2
    }
}
