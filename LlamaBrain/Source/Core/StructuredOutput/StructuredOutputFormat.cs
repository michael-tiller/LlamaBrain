namespace LlamaBrain.Core.StructuredOutput
{
    /// <summary>
    /// Specifies the format for structured LLM output.
    /// These correspond to llama.cpp server API parameters.
    /// </summary>
    public enum StructuredOutputFormat
    {
        /// <summary>
        /// No structured output enforcement. Falls back to prompt injection
        /// with "respond with JSON only" instructions and regex parsing.
        /// </summary>
        None = 0,

        /// <summary>
        /// Native JSON schema enforcement via llama.cpp json_schema parameter.
        /// Forces the LLM to generate output matching the provided JSON schema.
        /// This is the most reliable format for JSON output.
        /// </summary>
        JsonSchema = 1,

        /// <summary>
        /// GBNF grammar constraint via llama.cpp grammar parameter.
        /// More flexible than JSON schema, can enforce non-JSON formats.
        /// Requires generating a grammar string.
        /// </summary>
        Grammar = 2,

        /// <summary>
        /// Simple JSON mode via response_format: json_object.
        /// Less strict than JsonSchema but widely supported.
        /// Only guarantees valid JSON, not schema conformance.
        /// </summary>
        ResponseFormat = 3
    }
}
