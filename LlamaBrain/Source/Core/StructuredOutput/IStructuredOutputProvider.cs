namespace LlamaBrain.Core.StructuredOutput
{
    /// <summary>
    /// Interface for structured output providers that convert JSON schemas
    /// into API-specific structured output parameters.
    /// </summary>
    public interface IStructuredOutputProvider
    {
        /// <summary>
        /// Checks whether this provider supports the specified format.
        /// </summary>
        /// <param name="format">The structured output format to check.</param>
        /// <returns>True if the format is supported, false otherwise.</returns>
        bool SupportsFormat(StructuredOutputFormat format);

        /// <summary>
        /// Builds structured output parameters from a JSON schema.
        /// </summary>
        /// <param name="jsonSchema">The JSON schema describing the expected output structure.</param>
        /// <param name="format">The structured output format to use.</param>
        /// <returns>Parameters that can be passed to the LLM API.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown when the schema is invalid or the format is not supported.
        /// </exception>
        StructuredOutputParameters BuildParameters(string jsonSchema, StructuredOutputFormat format);

        /// <summary>
        /// Validates that a JSON schema is well-formed and can be used for structured output.
        /// </summary>
        /// <param name="jsonSchema">The JSON schema to validate.</param>
        /// <param name="error">The error message if validation fails, null otherwise.</param>
        /// <returns>True if the schema is valid, false otherwise.</returns>
        bool ValidateSchema(string jsonSchema, out string? error);
    }
}
