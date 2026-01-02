using System;
using LlamaBrain.Utilities;

namespace LlamaBrain.Core.StructuredOutput
{
    /// <summary>
    /// Structured output provider for llama.cpp server.
    /// Supports JsonSchema, Grammar, and ResponseFormat modes.
    /// </summary>
    public sealed class LlamaCppStructuredOutputProvider : IStructuredOutputProvider
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static LlamaCppStructuredOutputProvider Instance { get; } = new LlamaCppStructuredOutputProvider();

        private LlamaCppStructuredOutputProvider() { }

        /// <inheritdoc/>
        public bool SupportsFormat(StructuredOutputFormat format)
        {
            return format switch
            {
                StructuredOutputFormat.None => true,
                StructuredOutputFormat.JsonSchema => true,
                StructuredOutputFormat.Grammar => true,
                StructuredOutputFormat.ResponseFormat => true,
                _ => false
            };
        }

        /// <inheritdoc/>
        public StructuredOutputParameters BuildParameters(string jsonSchema, StructuredOutputFormat format)
        {
            if (format == StructuredOutputFormat.None)
            {
                return StructuredOutputParameters.None;
            }

            if (string.IsNullOrWhiteSpace(jsonSchema))
            {
                throw new ArgumentException("JSON schema cannot be null or empty", nameof(jsonSchema));
            }

            if (!ValidateSchema(jsonSchema, out var error))
            {
                throw new ArgumentException($"Invalid JSON schema: {error}", nameof(jsonSchema));
            }

            return format switch
            {
                StructuredOutputFormat.JsonSchema => StructuredOutputParameters.ForJsonSchema(jsonSchema),
                StructuredOutputFormat.Grammar => BuildGrammarParameters(jsonSchema),
                StructuredOutputFormat.ResponseFormat => StructuredOutputParameters.ForJsonMode(),
                _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format))
            };
        }

        /// <inheritdoc/>
        public bool ValidateSchema(string jsonSchema, out string? error)
        {
            if (string.IsNullOrWhiteSpace(jsonSchema))
            {
                error = "Schema cannot be null or empty";
                return false;
            }

            // Validate that the schema is valid JSON
            if (!JsonUtils.IsValidJson(jsonSchema))
            {
                error = "Schema is not valid JSON";
                return false;
            }

            // Basic structural validation - must be an object with at least a type property
            // More detailed validation could be added here
            if (!jsonSchema.Contains("\"type\""))
            {
                error = "Schema must contain a 'type' property";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Builds grammar parameters from a JSON schema.
        /// This converts the JSON schema to a GBNF grammar.
        /// </summary>
        private StructuredOutputParameters BuildGrammarParameters(string jsonSchema)
        {
            // For now, we use the JSON schema directly in grammar mode.
            // llama.cpp supports json_schema natively, so grammar mode is
            // primarily for non-JSON formats or when more control is needed.
            // A full JSON-to-GBNF converter could be implemented here.

            // Simple GBNF for JSON object - this is a basic fallback
            // In production, you'd want to generate this from the schema
            var grammar = GenerateJsonGrammarFromSchema(jsonSchema);
            return StructuredOutputParameters.ForGrammar(grammar);
        }

        /// <summary>
        /// Generates a basic GBNF grammar from a JSON schema.
        /// This is a simplified implementation - full schema-to-grammar
        /// conversion is complex and may not cover all cases.
        /// </summary>
        private string GenerateJsonGrammarFromSchema(string jsonSchema)
        {
            // Basic JSON object grammar that allows any valid JSON object
            // A more sophisticated implementation would parse the schema
            // and generate specific rules for required fields, types, etc.
            return @"
root   ::= object
value  ::= object | array | string | number | (""true"" | ""false"") | ""null""
object ::= ""{""  ws (string "":"" ws value ("","" ws string "":"" ws value)*)? ""}""  ws
array  ::= ""[""  ws (value ("","" ws value)*)? ""]""  ws
string ::= ""\"""" ([^""\\] | ""\\""  ([""\\/bfnrt] | ""u""  [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F]))* ""\""""  ws
number ::= (""-"" ?)  ([0-9] | [1-9] [0-9]*)  (""."" [0-9]+)?  ([eE] [""-+""]? [0-9]+)?  ws
ws     ::= ([ \t\n] ws)?
".Trim();
        }
    }
}
