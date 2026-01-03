using System;
using LlamaBrain.Core.StructuredInput.Schemas;
using Newtonsoft.Json;

namespace LlamaBrain.Core.StructuredInput
{
    /// <summary>
    /// Pure static serializer for structured context JSON.
    /// Provides deterministic serialization/deserialization of context data.
    /// </summary>
    public static class ContextSerializer
    {
        /// <summary>
        /// JSON serializer settings for deterministic output.
        /// Uses consistent formatting and property ordering.
        /// </summary>
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include,
            // Ensure deterministic property order via JsonProperty Order attributes
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver()
        };

        /// <summary>
        /// Serializes a context schema to JSON string.
        /// Produces deterministic output for identical inputs.
        /// </summary>
        /// <param name="context">The context schema to serialize.</param>
        /// <returns>JSON string representation of the context.</returns>
        /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
        public static string Serialize(ContextJsonSchema context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return JsonConvert.SerializeObject(context, _serializerSettings);
        }

        /// <summary>
        /// Deserializes a JSON string to context schema.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized context schema, or null if deserialization fails.</returns>
        /// <exception cref="ArgumentNullException">Thrown when json is null.</exception>
        public static ContextJsonSchema? Deserialize(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            return JsonConvert.DeserializeObject<ContextJsonSchema>(json, _serializerSettings);
        }

        /// <summary>
        /// Serializes context to a compact JSON string (no indentation).
        /// Useful for reducing token count when sending to LLM.
        /// </summary>
        /// <param name="context">The context schema to serialize.</param>
        /// <returns>Compact JSON string representation.</returns>
        public static string SerializeCompact(ContextJsonSchema context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var compactSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include
            };

            return JsonConvert.SerializeObject(context, compactSettings);
        }

        /// <summary>
        /// Wraps serialized context JSON in XML-style delimiters for prompt injection.
        /// </summary>
        /// <param name="context">The context schema to serialize.</param>
        /// <param name="openTag">Opening tag (default: &lt;context_json&gt;).</param>
        /// <param name="closeTag">Closing tag (default: &lt;/context_json&gt;).</param>
        /// <param name="compact">Whether to use compact JSON (no indentation).</param>
        /// <returns>JSON wrapped in delimiter tags.</returns>
        public static string SerializeWithDelimiters(
            ContextJsonSchema context,
            string openTag = "<context_json>",
            string closeTag = "</context_json>",
            bool compact = false)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var json = compact ? SerializeCompact(context) : Serialize(context);
            return $"{openTag}\n{json}\n{closeTag}";
        }
    }
}
