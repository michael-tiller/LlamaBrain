namespace LlamaBrain.Core.StructuredOutput
{
    /// <summary>
    /// Parameters for structured output that will be sent to the LLM API.
    /// These map directly to llama.cpp server API parameters.
    /// </summary>
    public sealed class StructuredOutputParameters
    {
        /// <summary>
        /// The structured output format being used.
        /// </summary>
        public StructuredOutputFormat Format { get; set; }

        /// <summary>
        /// JSON schema string for JsonSchema format.
        /// Maps to llama.cpp json_schema parameter.
        /// </summary>
        public string? JsonSchema { get; set; }

        /// <summary>
        /// GBNF grammar string for Grammar format.
        /// Maps to llama.cpp grammar parameter.
        /// </summary>
        public string? Grammar { get; set; }

        /// <summary>
        /// Response format type for ResponseFormat format.
        /// Maps to llama.cpp response_format parameter.
        /// </summary>
        public string? ResponseFormatType { get; set; }

        /// <summary>
        /// Creates parameters for JsonSchema format.
        /// </summary>
        public static StructuredOutputParameters ForJsonSchema(string jsonSchema)
        {
            return new StructuredOutputParameters
            {
                Format = StructuredOutputFormat.JsonSchema,
                JsonSchema = jsonSchema
            };
        }

        /// <summary>
        /// Creates parameters for Grammar format.
        /// </summary>
        public static StructuredOutputParameters ForGrammar(string grammar)
        {
            return new StructuredOutputParameters
            {
                Format = StructuredOutputFormat.Grammar,
                Grammar = grammar
            };
        }

        /// <summary>
        /// Creates parameters for ResponseFormat (JSON mode).
        /// </summary>
        public static StructuredOutputParameters ForJsonMode()
        {
            return new StructuredOutputParameters
            {
                Format = StructuredOutputFormat.ResponseFormat,
                ResponseFormatType = "json_object"
            };
        }

        /// <summary>
        /// Creates empty parameters (no structured output).
        /// </summary>
        public static StructuredOutputParameters None
        {
            get
            {
                return new StructuredOutputParameters
                {
                    Format = StructuredOutputFormat.None
                };
            }
        }
    }
}
