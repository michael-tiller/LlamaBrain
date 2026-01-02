using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using LlamaBrain.Core.Validation;
using LlamaBrain.Utilities;

namespace LlamaBrain.Core.StructuredOutput
{
    /// <summary>
    /// Builder for JSON schemas used in structured output.
    /// Provides pre-built schemas for common types and dynamic schema generation.
    /// </summary>
    public static class JsonSchemaBuilder
    {
        /// <summary>
        /// Pre-built JSON schema for ParsedOutput structure.
        /// This is the primary schema for dialogue responses with mutations and intents.
        /// </summary>
        public static readonly string ParsedOutputSchema = @"{
  ""type"": ""object"",
  ""properties"": {
    ""dialogueText"": {
      ""type"": ""string"",
      ""description"": ""The NPC's dialogue response to display to the player""
    },
    ""proposedMutations"": {
      ""type"": ""array"",
      ""description"": ""Memory mutations to apply after validation"",
      ""items"": {
        ""type"": ""object"",
        ""properties"": {
          ""type"": {
            ""type"": ""string"",
            ""enum"": [""AppendEpisodic"", ""TransformBelief"", ""TransformRelationship"", ""EmitWorldIntent""],
            ""description"": ""The type of memory mutation""
          },
          ""target"": {
            ""type"": ""string"",
            ""description"": ""The target of the mutation (belief ID, relationship target, etc.)""
          },
          ""content"": {
            ""type"": ""string"",
            ""description"": ""The content/value of the mutation""
          },
          ""confidence"": {
            ""type"": ""number"",
            ""minimum"": 0,
            ""maximum"": 1,
            ""description"": ""Confidence level for belief mutations (0-1)""
          }
        },
        ""required"": [""type"", ""content""]
      }
    },
    ""worldIntents"": {
      ""type"": ""array"",
      ""description"": ""World intents representing NPC actions/desires"",
      ""items"": {
        ""type"": ""object"",
        ""properties"": {
          ""intentType"": {
            ""type"": ""string"",
            ""description"": ""The type of intent (e.g., follow_player, give_item)""
          },
          ""target"": {
            ""type"": ""string"",
            ""description"": ""The target of the intent""
          },
          ""parameters"": {
            ""type"": ""object"",
            ""description"": ""Additional parameters for the intent"",
            ""additionalProperties"": { ""type"": ""string"" }
          },
          ""priority"": {
            ""type"": ""integer"",
            ""description"": ""Priority of this intent (higher = more urgent)""
          }
        },
        ""required"": [""intentType""]
      }
    }
  },
  ""required"": [""dialogueText""]
}";

        /// <summary>
        /// Simplified schema for dialogue-only responses (no mutations/intents).
        /// Use this when you only need the dialogue text.
        /// </summary>
        public static readonly string DialogueOnlySchema = @"{
  ""type"": ""object"",
  ""properties"": {
    ""dialogueText"": {
      ""type"": ""string"",
      ""description"": ""The NPC's dialogue response to display to the player""
    },
    ""emotion"": {
      ""type"": ""string"",
      ""enum"": [""neutral"", ""happy"", ""sad"", ""angry"", ""surprised"", ""fearful""],
      ""description"": ""The emotional tone of the response""
    }
  },
  ""required"": [""dialogueText""]
}";

        /// <summary>
        /// Schema for analysis/decision-making responses.
        /// </summary>
        public static readonly string AnalysisSchema = @"{
  ""type"": ""object"",
  ""properties"": {
    ""decision"": {
      ""type"": ""string"",
      ""description"": ""The decision or conclusion reached""
    },
    ""reasoning"": {
      ""type"": ""string"",
      ""description"": ""The reasoning behind the decision""
    },
    ""confidence"": {
      ""type"": ""number"",
      ""minimum"": 0,
      ""maximum"": 1,
      ""description"": ""Confidence level in the decision (0-1)""
    },
    ""alternatives"": {
      ""type"": ""array"",
      ""items"": { ""type"": ""string"" },
      ""description"": ""Alternative decisions considered""
    }
  },
  ""required"": [""decision"", ""reasoning""]
}";

        /// <summary>
        /// Builds the pre-defined ParsedOutput schema.
        /// </summary>
        /// <returns>The JSON schema string for ParsedOutput.</returns>
        public static string BuildParsedOutputSchema()
        {
            return ParsedOutputSchema;
        }

        /// <summary>
        /// Builds a JSON schema from a C# type using reflection.
        /// Supports basic types, enums, arrays, and nested objects.
        /// </summary>
        /// <typeparam name="T">The type to generate a schema for.</typeparam>
        /// <returns>The JSON schema string for the type.</returns>
        public static string BuildFromType<T>()
        {
            return BuildFromType(typeof(T));
        }

        /// <summary>
        /// Builds a JSON schema from a C# type using reflection.
        /// </summary>
        /// <param name="type">The type to generate a schema for.</param>
        /// <returns>The JSON schema string for the type.</returns>
        public static string BuildFromType(Type type)
        {
            var schema = BuildSchemaForType(type, new HashSet<Type>());
            return FormatJson(schema);
        }

        /// <summary>
        /// Validates that a schema is well-formed JSON Schema.
        /// </summary>
        /// <param name="schema">The JSON schema to validate.</param>
        /// <param name="error">The error message if validation fails.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateSchema(string schema, out string? error)
        {
            if (string.IsNullOrWhiteSpace(schema))
            {
                error = "Schema cannot be null or empty";
                return false;
            }

            if (!JsonUtils.IsValidJson(schema))
            {
                error = "Schema is not valid JSON";
                return false;
            }

            // Basic structural validation
            if (!schema.Contains("\"type\""))
            {
                error = "Schema must contain a 'type' property";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Creates a simple object schema with the given properties.
        /// </summary>
        /// <param name="properties">Dictionary of property name to type description.</param>
        /// <param name="required">Array of required property names.</param>
        /// <returns>The JSON schema string.</returns>
        public static string CreateObjectSchema(
            Dictionary<string, (string type, string description)> properties,
            string[]? required = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"type\": \"object\",");
            sb.AppendLine("  \"properties\": {");

            var propList = properties.ToList();
            for (int i = 0; i < propList.Count; i++)
            {
                var prop = propList[i];
                var comma = i < propList.Count - 1 ? "," : "";
                sb.AppendLine($"    \"{prop.Key}\": {{");
                sb.AppendLine($"      \"type\": \"{prop.Value.type}\",");
                sb.AppendLine($"      \"description\": \"{EscapeJsonString(prop.Value.description)}\"");
                sb.AppendLine($"    }}{comma}");
            }

            sb.AppendLine("  }");

            if (required != null && required.Length > 0)
            {
                var requiredJson = string.Join(", ", required.Select(r => $"\"{r}\""));
                sb.AppendLine($"  ,\"required\": [{requiredJson}]");
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        /// Builds schema object for a C# type.
        /// </summary>
        private static string BuildSchemaForType(Type type, HashSet<Type> visitedTypes)
        {
            // Prevent infinite recursion for circular references
            if (visitedTypes.Contains(type))
            {
                return "{ \"type\": \"object\" }";
            }

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                type = underlyingType;
            }

            // Handle primitive types
            if (type == typeof(string))
                return "{ \"type\": \"string\" }";
            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
                return "{ \"type\": \"integer\" }";
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
                return "{ \"type\": \"number\" }";
            if (type == typeof(bool))
                return "{ \"type\": \"boolean\" }";

            // Handle enums
            if (type.IsEnum)
            {
                var values = Enum.GetNames(type);
                var enumValues = string.Join(", ", values.Select(v => $"\"{v}\""));
                return $"{{ \"type\": \"string\", \"enum\": [{enumValues}] }}";
            }

            // Handle arrays and lists
            if (type.IsArray)
            {
                var elementType = type.GetElementType()!;
                visitedTypes.Add(type);
                var itemSchema = BuildSchemaForType(elementType, visitedTypes);
                return $"{{ \"type\": \"array\", \"items\": {itemSchema} }}";
            }

            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>) || genericDef == typeof(IList<>) ||
                    genericDef == typeof(IEnumerable<>) || genericDef == typeof(IReadOnlyList<>))
                {
                    var elementType = type.GetGenericArguments()[0];
                    visitedTypes.Add(type);
                    var itemSchema = BuildSchemaForType(elementType, visitedTypes);
                    return $"{{ \"type\": \"array\", \"items\": {itemSchema} }}";
                }

                if (genericDef == typeof(Dictionary<,>) || genericDef == typeof(IDictionary<,>))
                {
                    var genericArgs = type.GetGenericArguments();
                    if (genericArgs.Length >= 2)
                    {
                        // Get the value type (second generic argument)
                        var valueType = genericArgs[1];
                        visitedTypes.Add(type);
                        try
                        {
                            // Recursively generate schema for the value type
                            var valueSchema = BuildSchemaForType(valueType, visitedTypes);
                            return $"{{ \"type\": \"object\", \"additionalProperties\": {valueSchema} }}";
                        }
                        catch
                        {
                            // Fallback to generic object schema if value type cannot be resolved
                            return "{ \"type\": \"object\", \"additionalProperties\": {} }";
                        }
                    }
                    else
                    {
                        // Fallback to generic object schema if generic arguments cannot be resolved
                        return "{ \"type\": \"object\", \"additionalProperties\": {} }";
                    }
                }
            }

            // Handle complex objects
            visitedTypes.Add(type);
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"type\": \"object\",");
            sb.AppendLine("  \"properties\": {");

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToList();

            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];
                var propName = ToCamelCase(prop.Name);
                var propSchema = BuildSchemaForType(prop.PropertyType, new HashSet<Type>(visitedTypes));
                var comma = i < properties.Count - 1 ? "," : "";
                sb.AppendLine($"    \"{propName}\": {propSchema}{comma}");
            }

            sb.AppendLine("  }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// Converts a PascalCase string to camelCase.
        /// </summary>
        private static string ToCamelCase(string str)
        {
            if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
                return str;

            return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        /// <summary>
        /// Escapes a string for JSON.
        /// </summary>
        private static string EscapeJsonString(string str)
        {
            return str
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        /// <summary>
        /// Formats JSON with proper indentation.
        /// </summary>
        private static string FormatJson(string json)
        {
            // Simple pass-through for now - the builder already formats
            return json.Trim();
        }
    }

    /// <summary>
    /// DTO for structured dialogue response that maps to ParsedOutput.
    /// Use this type with JsonSchemaBuilder.BuildFromType for automatic schema generation.
    /// </summary>
    [Serializable]
    public class StructuredDialogueResponse
    {
        /// <summary>
        /// The NPC's dialogue response text.
        /// </summary>
        public string DialogueText { get; set; } = "";

        /// <summary>
        /// Proposed memory mutations.
        /// </summary>
        public List<StructuredMutation> ProposedMutations { get; set; } = new List<StructuredMutation>();

        /// <summary>
        /// World intents for game actions.
        /// </summary>
        public List<StructuredIntent> WorldIntents { get; set; } = new List<StructuredIntent>();
    }

    /// <summary>
    /// DTO for structured mutation in JSON responses.
    /// </summary>
    [Serializable]
    public class StructuredMutation
    {
        /// <summary>
        /// The type of mutation.
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// The target of the mutation.
        /// </summary>
        public string? Target { get; set; }

        /// <summary>
        /// The content of the mutation.
        /// </summary>
        public string Content { get; set; } = "";

        /// <summary>
        /// Confidence level for belief mutations.
        /// </summary>
        public float Confidence { get; set; } = 1.0f;

        /// <summary>
        /// Converts to ProposedMutation.
        /// </summary>
        /// <returns>A ProposedMutation instance with the parsed mutation type and content.</returns>
        public ProposedMutation ToProposedMutation()
        {
            if (!Enum.TryParse<MutationType>(Type, true, out var mutationType))
            {
                mutationType = MutationType.AppendEpisodic;
            }

            return new ProposedMutation
            {
                Type = mutationType,
                Target = Target,
                Content = Content,
                Confidence = Confidence
            };
        }
    }

    /// <summary>
    /// DTO for structured world intent in JSON responses.
    /// </summary>
    [Serializable]
    public class StructuredIntent
    {
        /// <summary>
        /// The type of intent.
        /// </summary>
        public string IntentType { get; set; } = "";

        /// <summary>
        /// The target of the intent.
        /// </summary>
        public string? Target { get; set; }

        /// <summary>
        /// Additional parameters.
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Priority of the intent.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Converts to WorldIntent.
        /// </summary>
        /// <returns>A WorldIntent instance with the intent type, target, parameters, and priority.</returns>
        public WorldIntent ToWorldIntent()
        {
            return new WorldIntent
            {
                IntentType = IntentType,
                Target = Target,
                Parameters = new Dictionary<string, string>(Parameters),
                Priority = Priority
            };
        }
    }
}
