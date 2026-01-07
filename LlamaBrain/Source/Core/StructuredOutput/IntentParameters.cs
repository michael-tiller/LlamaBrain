using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LlamaBrain.Core.StructuredOutput
{
    /// <summary>
    /// Typed parameters for give_item intent.
    /// </summary>
    [Serializable]
    public sealed class GiveItemParameters
    {
        /// <summary>
        /// The ID of the item to give.
        /// </summary>
        public string ItemId { get; set; } = "";

        /// <summary>
        /// The quantity to give (default: 1).
        /// </summary>
        public int Quantity { get; set; } = 1;

        /// <summary>
        /// The condition of the item (e.g., "new", "worn", "damaged").
        /// </summary>
        public string? Condition { get; set; }
    }

    /// <summary>
    /// Typed parameters for move_to intent.
    /// </summary>
    [Serializable]
    public sealed class MoveToParameters
    {
        /// <summary>
        /// The destination location.
        /// </summary>
        public string Location { get; set; } = "";

        /// <summary>
        /// Movement speed (e.g., "walk", "run", "sneak"). Default: "walk".
        /// </summary>
        public string Speed { get; set; } = "walk";

        /// <summary>
        /// Path type (e.g., "direct", "pathfind", "follow_road"). Default: "pathfind".
        /// </summary>
        public string PathType { get; set; } = "pathfind";
    }

    /// <summary>
    /// Typed parameters for interact intent.
    /// </summary>
    [Serializable]
    public sealed class InteractParameters
    {
        /// <summary>
        /// The target entity to interact with.
        /// </summary>
        public string TargetEntity { get; set; } = "";

        /// <summary>
        /// The type of interaction (e.g., "talk", "trade", "attack").
        /// </summary>
        public string InteractionType { get; set; } = "";

        /// <summary>
        /// Duration of the interaction in seconds (optional).
        /// </summary>
        public float? Duration { get; set; }
    }

    /// <summary>
    /// Extension methods for extracting typed parameters from intent parameter dictionaries.
    /// </summary>
    public static class IntentParameterExtensions
    {
        /// <summary>
        /// Extracts GiveItemParameters from a parameter dictionary.
        /// </summary>
        /// <param name="parameters">The parameters dictionary.</param>
        /// <returns>Typed parameters, or null if required fields are missing.</returns>
        public static GiveItemParameters? GetGiveItemParameters(Dictionary<string, object>? parameters)
        {
            if (parameters == null)
                return null;

            var itemId = GetValue<string>(parameters, "itemId");
            if (string.IsNullOrEmpty(itemId))
                return null;

            return new GiveItemParameters
            {
                ItemId = itemId,
                Quantity = GetValue(parameters, "quantity", 1),
                Condition = GetValue<string>(parameters, "condition")
            };
        }

        /// <summary>
        /// Extracts MoveToParameters from a parameter dictionary.
        /// </summary>
        /// <param name="parameters">The parameters dictionary.</param>
        /// <returns>Typed parameters, or null if required fields are missing.</returns>
        public static MoveToParameters? GetMoveToParameters(Dictionary<string, object>? parameters)
        {
            if (parameters == null)
                return null;

            var location = GetValue<string>(parameters, "location");
            if (string.IsNullOrEmpty(location))
                return null;

            return new MoveToParameters
            {
                Location = location,
                Speed = GetValue(parameters, "speed", "walk"),
                PathType = GetValue(parameters, "pathType", "pathfind")
            };
        }

        /// <summary>
        /// Extracts InteractParameters from a parameter dictionary.
        /// </summary>
        /// <param name="parameters">The parameters dictionary.</param>
        /// <returns>Typed parameters, or null if required fields are missing.</returns>
        public static InteractParameters? GetInteractParameters(Dictionary<string, object>? parameters)
        {
            if (parameters == null)
                return null;

            var targetEntity = GetValue<string>(parameters, "targetEntity");
            if (string.IsNullOrEmpty(targetEntity))
                return null;

            return new InteractParameters
            {
                TargetEntity = targetEntity,
                InteractionType = GetValue(parameters, "interactionType", ""),
                Duration = GetValue<float?>(parameters, "duration")
            };
        }

        /// <summary>
        /// Gets a typed value from a parameter dictionary.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="parameters">The parameters dictionary.</param>
        /// <param name="key">The key to look up.</param>
        /// <param name="defaultValue">Default value if key is missing or conversion fails.</param>
        /// <returns>The converted value or default.</returns>
        public static T GetValue<T>(Dictionary<string, object>? parameters, string key, T defaultValue = default!)
        {
            if (parameters == null || !parameters.TryGetValue(key, out var value) || value == null)
                return defaultValue;

            // Direct type match
            if (value is T typedValue)
                return typedValue;

            // Try conversion
            try
            {
                var targetType = typeof(T);
                var underlyingType = Nullable.GetUnderlyingType(targetType);

                if (underlyingType != null)
                {
                    // Handle nullable types
                    return (T)Convert.ChangeType(value, underlyingType);
                }

                return (T)Convert.ChangeType(value, targetType);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Gets an array value from a parameter dictionary.
        /// Handles T[], object[], IList, IEnumerable, and JArray from JSON deserialization.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="parameters">The parameters dictionary.</param>
        /// <param name="key">The key to look up.</param>
        /// <returns>The array or null if not found or conversion fails.</returns>
        public static T[]? GetArray<T>(Dictionary<string, object>? parameters, string key)
        {
            if (parameters == null || !parameters.TryGetValue(key, out var value) || value == null)
                return null;

            // Direct type match - already T[]
            if (value is T[] typedArray)
                return typedArray;

            try
            {
                // Handle JArray from Newtonsoft.Json deserialization
                if (value is JArray jArray)
                {
                    var result = new T[jArray.Count];
                    for (int i = 0; i < jArray.Count; i++)
                    {
                        result[i] = jArray[i].ToObject<T>()!;
                    }
                    return result;
                }

                // Handle JToken (could be an array)
                if (value is JToken jToken && jToken.Type == JTokenType.Array)
                {
                    var tokenArray = jToken.ToObject<T[]>();
                    return tokenArray;
                }

                // Handle object[] (common from some deserializers)
                if (value is object[] objArray)
                {
                    var result = new T[objArray.Length];
                    for (int i = 0; i < objArray.Length; i++)
                    {
                        result[i] = ConvertElement<T>(objArray[i]);
                    }
                    return result;
                }

                // Handle IList (includes List<T>, ArrayList, etc.)
                if (value is IList list)
                {
                    var result = new T[list.Count];
                    for (int i = 0; i < list.Count; i++)
                    {
                        result[i] = ConvertElement<T>(list[i]);
                    }
                    return result;
                }

                // Handle IEnumerable<object> or any IEnumerable (fallback)
                if (value is IEnumerable enumerable && !(value is string))
                {
                    var items = enumerable.Cast<object>().ToArray();
                    var result = new T[items.Length];
                    for (int i = 0; i < items.Length; i++)
                    {
                        result[i] = ConvertElement<T>(items[i]);
                    }
                    return result;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// Converts a single element to the target type.
        /// Handles JToken, nullable types, and standard conversions.
        /// </summary>
        private static T ConvertElement<T>(object? element)
        {
            if (element == null)
                return default!;

            // Already the right type
            if (element is T typedElement)
                return typedElement;

            // Handle JToken/JValue from Newtonsoft.Json
            if (element is JToken jToken)
            {
                return jToken.ToObject<T>()!;
            }

            // Standard conversion
            var targetType = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(targetType);

            if (underlyingType != null)
            {
                return (T)Convert.ChangeType(element, underlyingType);
            }

            return (T)Convert.ChangeType(element, targetType);
        }

        /// <summary>
        /// Gets a nested dictionary from a parameter dictionary.
        /// </summary>
        /// <param name="parameters">The parameters dictionary.</param>
        /// <param name="key">The key to look up.</param>
        /// <returns>The nested dictionary or null if not found.</returns>
        public static Dictionary<string, object>? GetNested(Dictionary<string, object>? parameters, string key)
        {
            if (parameters == null || !parameters.TryGetValue(key, out var value) || value == null)
                return null;

            return value as Dictionary<string, object>;
        }
    }
}
